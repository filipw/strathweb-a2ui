using System.Text.Json;
using A2A;
using Strathweb.A2UI.A2A;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Rendering;

namespace Strathweb.A2UI.Samples;

/// <summary>
/// One conversation with an A2UI-speaking agent over A2A, as a browser page sees it: a transcript,
/// the live surfaces, and the wire log. Speaks <c>message/stream</c>, so text arrives token by token
/// and surfaces paint the moment their messages arrive.
/// </summary>
public sealed class A2UIChatSession : IDisposable
{
    private readonly Func<A2AClient> clientFactory;
    private readonly string contextId = Guid.NewGuid().ToString("N");
    private readonly A2UIVersionProfile profile = A2UIVersionProfile.V0_9_1;
    private A2AClient? client;
    private ChatEntry? current;

    /// <summary>Creates a session.</summary>
    /// <param name="clientFactory">Creates the A2A client on first use, once the page knows its own address.</param>
    public A2UIChatSession(Func<A2AClient> clientFactory)
    {
        this.clientFactory = clientFactory;
        Renderer.SurfaceCreated += (_, surface) => (current ?? Transcript.LastOrDefault(e => e.Role == "agent"))?.Surfaces.Add(surface);
        Renderer.SurfaceDeleted += (_, surface) =>
        {
            foreach (var entry in Transcript)
            {
                entry.Surfaces.Remove(surface);
            }
        };
        Renderer.SurfaceChanged += (_, _) => Changed?.Invoke();
    }

    /// <summary>Raised whenever the transcript, a surface or the log changed.</summary>
    public event Action? Changed;

    /// <summary>The renderer-side state: every live surface and the data models to send back.</summary>
    public A2UIRendererSession Renderer { get; } = new();

    /// <summary>The conversation so far.</summary>
    public List<ChatEntry> Transcript { get; } = [];

    /// <summary>What went over the wire and what the library reported, newest first.</summary>
    public List<WireLogEntry> Wire { get; } = [];

    /// <summary>Whether a request is in flight.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>The last error, if the last request failed.</summary>
    public string? Error { get; private set; }

    /// <summary>Sends what the user typed.</summary>
    /// <param name="text">The text.</param>
    public Task SendTextAsync(string text) =>
        SendAsync(Part.FromText(text), $"text \"{text}\"", new ChatEntry("user", text));

    /// <summary>Sends an action the user performed on a surface.</summary>
    /// <param name="action">The action, built by the renderer.</param>
    public Task SendActionAsync(ActionMessage action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return SendAsync(A2UIParts.Create(action), $"action {action.Name} on {action.SurfaceId}", null);
    }

    /// <summary>Adds a line to the log panel.</summary>
    /// <param name="kind"><c>out</c>, <c>in</c>, <c>log</c> or <c>err</c>.</param>
    /// <param name="text">The line.</param>
    public void Log(string kind, string text)
    {
        Wire.Insert(0, new WireLogEntry(kind, text, DateTimeOffset.Now));
        if (Wire.Count > 60)
        {
            Wire.RemoveAt(Wire.Count - 1);
        }

        Changed?.Invoke();
    }

    private async Task SendAsync(Part part, string description, ChatEntry? userEntry)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Error = null;
        if (userEntry is not null)
        {
            Transcript.Add(userEntry);
        }

        current = new ChatEntry("agent");
        Transcript.Add(current);
        Log("out", description);

        try
        {
            client ??= clientFactory();
            var request = new SendMessageRequest { Message = BuildMessage(part) };

            await foreach (var chunk in client.SendStreamingMessageAsync(request).ConfigureAwait(false))
            {
                foreach (var received in PartsOf(chunk))
                {
                    Receive(received);
                }

                Changed?.Invoke();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Error = ex.Message;
            Log("err", ex.Message);
        }
        finally
        {
            if (current.IsEmpty)
            {
                Transcript.Remove(current);
            }

            current = null;
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// The A2A message, with the renderer's capabilities and the data models of surfaces that asked
    /// for them in the metadata, the way the A2UI extension specifies.
    /// </summary>
    private Message BuildMessage(Part part)
    {
        var message = new Message
        {
            Role = Role.User,
            MessageId = Guid.NewGuid().ToString("N"),
            ContextId = contextId,
            Parts = [part],
            Metadata = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };

        A2UIMetadata.WriteCapabilities(message.Metadata, new A2UIRendererCapabilities(Renderer.SupportedCatalogIds), profile);

        var models = Renderer.DataModels();
        if (models.Count > 0)
        {
            A2UIMetadata.WriteDataModel(message.Metadata, new A2UIRendererDataModel(profile.EmitVersion, models), profile);
        }

        return message;
    }

    private void Receive(Part part)
    {
        if (A2UIParts.TryReadTolerant(part, out var messages, out var errors))
        {
            foreach (var error in errors)
            {
                Log("err", error);
            }

            var ignored = Renderer.Apply(messages);
            Log("in", string.Join(", ", messages.Select(m => m.WireKey)) + (ignored > 0 ? $" ({ignored} for unknown surfaces)" : string.Empty));
            return;
        }

        if (part.Text is { Length: > 0 } text)
        {
            current?.Append(text);
        }
    }

    private static IEnumerable<Part> PartsOf(StreamResponse chunk)
    {
        foreach (var part in chunk.Message?.Parts ?? [])
        {
            yield return part;
        }

        foreach (var part in chunk.ArtifactUpdate?.Artifact?.Parts ?? [])
        {
            yield return part;
        }

        foreach (var part in chunk.StatusUpdate?.Status?.Message?.Parts ?? [])
        {
            yield return part;
        }

        if (chunk.Task is { } task)
        {
            foreach (var part in task.Status?.Message?.Parts ?? [])
            {
                yield return part;
            }

            foreach (var artifact in task.Artifacts ?? [])
            {
                foreach (var part in artifact.Parts ?? [])
                {
                    yield return part;
                }
            }
        }
    }

    public void Dispose() => client?.Dispose();
}
