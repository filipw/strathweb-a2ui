using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.AgentFramework;

namespace BookingWizard;

/// <summary>
/// Drives the wizard from the actions the renderer sends. Deterministic, so the sample runs without a
/// model; a model-backed agent's tools would emit exactly the same messages.
/// </summary>
internal sealed class BookingAgent : AIAgent
{
    public override string Name => "table-booking";

    public override string Description => "Books a table through a three-step form.";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        new(new BookingSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(session.StateBag.Serialize());

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(new BookingSession(AgentSessionStateBag.Deserialize(serializedSession)));

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, Handle())));

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var reply = Handle();
        await Task.Yield();
        yield return new AgentResponseUpdate(ChatRole.Assistant, reply);
    }

    private static string Handle()
    {
        var actions = A2UIRunContext.Actions;
        var action = actions.Count == 0 ? null : actions[^1];
        var step = CurrentStep();

        switch (action?.Name)
        {
            case "next":
                A2UIEmitter.Emit(BookingSurfaces.GoToStep(Math.Min(step + 1, 3)));
                return string.Empty;

            case "back":
                A2UIEmitter.Emit(BookingSurfaces.GoToStep(Math.Max(step - 1, 1)));
                return string.Empty;

            case "confirm":
                A2UIEmitter.Emit(BookingSurfaces.Confirmed(Summary(action.Context)));
                return "Table booked.";

            case "start_over":
            case null when A2UIRunContext.GetSurfaceData(BookingSurfaces.SurfaceId) is not null:
                A2UIEmitter.Delete(BookingSurfaces.SurfaceId);
                A2UIEmitter.Emit(BookingSurfaces.Start());
                return string.Empty;

            case "noop":
                return string.Empty;

            default:
                A2UIEmitter.Emit(BookingSurfaces.Start());
                return "Let's get you a table. Three quick steps.";
        }
    }

    /// <summary>The step the renderer says it is on, read from the data model it sent back.</summary>
    private static int CurrentStep()
    {
        var data = A2UIRunContext.GetSurfaceData(BookingSurfaces.SurfaceId);
        return data?["step"] is JsonValue value && value.TryGetValue<double>(out var step) ? (int)step : 1;
    }

    private static string Summary(JsonObject context)
    {
        var time = context["time"] is JsonArray times && times.Count > 0 ? (string?)times[0] : "an unspecified time";
        var window = context["window"] is JsonValue w && w.TryGetValue<bool>(out var wantsWindow) && wantsWindow
            ? ", by the window if we can"
            : string.Empty;
        var notes = (string?)context["notes"] is { Length: > 0 } n ? $" Notes: {n}" : string.Empty;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{context["name"]}, {context["guests"]} guests, {context["date"]} at {time}{window}.{notes}");
    }
}

/// <summary>An in-memory session. The A2A host keeps one per conversation.</summary>
internal sealed class BookingSession : AgentSession
{
    internal BookingSession()
    {
    }

    internal BookingSession(AgentSessionStateBag stateBag)
        : base(stateBag)
    {
    }
}
