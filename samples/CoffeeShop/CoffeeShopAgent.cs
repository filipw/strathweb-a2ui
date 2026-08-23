using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Strathweb.A2UI.AgentFramework;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Surfaces;

namespace CoffeeShop;

/// <summary>
/// A deterministic agent, so the sample runs without a model key.
/// </summary>
/// <remarks>
/// It reacts to the actions the renderer sends instead of asking a model what to do. The surface
/// building, emission and inbound handling are exactly what a model-backed agent's tools would do;
/// see samples/SurveyAgent for that shape.
/// </remarks>
internal sealed class CoffeeShopAgent : AIAgent
{
    public override string Name => "coffee-shop";

    public override string Description => "Takes a coffee order using a form rather than a conversation.";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        new(new CoffeeShopSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(session.StateBag.Serialize());

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(new CoffeeShopSession(AgentSessionStateBag.Deserialize(serializedSession)));

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

    /// <summary>Decides what to show, from the action the renderer sent.</summary>
    private static string Handle()
    {
        var actions = A2UIRunContext.Actions;
        var action = actions.Count == 0 ? null : actions[actions.Count - 1];

        return action?.Name switch
        {
            "add_drink" => ChangeCart(cart =>
            {
                if (Menu.Find((string?)action.Context["id"]) is { } drink)
                {
                    cart.Add(drink);
                }
            }),
            "remove_drink" => ChangeCart(cart => cart.RemoveAt(Index(action.Context["index"]))),
            "place_order" => PlaceOrder(),
            "refresh_status" => AdvanceStatus(),
            "start_over" => StartOver(),
            _ => ShowMenu(),
        };
    }

    private static string ShowMenu()
    {
        A2UIEmitter.Emit(CoffeeShopSurfaces.Menu());
        return "The menu is on your screen.";
    }

    /// <summary>
    /// Reads the basket out of the data model the renderer reported, changes it, and writes back only
    /// what moved. The components stay where they are.
    /// </summary>
    private static string ChangeCart(Action<Cart> change)
    {
        var cart = Cart.From(A2UIRunContext.GetSurfaceData(CoffeeShopSurfaces.MenuSurfaceId));
        change(cart);

        A2UIEmitter.Emit(A2UISurfaceUpdate.For(CoffeeShopSurfaces.MenuSurfaceId, CoffeeShopSurfaces.Catalog)
            .SetData("/cart", cart.ToJson())
            .SetData("/cartHeading", JsonValue.Create(cart.Heading))
            .SetData("/total", JsonValue.Create(cart.TotalText)));

        return $"{cart.Heading}, {cart.TotalText.ToLowerInvariant()}.";
    }

    private static string PlaceOrder()
    {
        var cart = Cart.From(A2UIRunContext.GetSurfaceData(CoffeeShopSurfaces.MenuSurfaceId));

        if (cart.Items.Count == 0)
        {
            return "Nothing in the basket yet.";
        }

        // The menu comes off the screen and the confirmation replaces it.
        A2UIEmitter.Delete(CoffeeShopSurfaces.MenuSurfaceId);
        A2UIEmitter.Emit(CoffeeShopSurfaces.Order(cart.Summary, Menu.Money(cart.Total)));

        return "Order placed.";
    }

    private static readonly string[] Statuses =
        ["Grinding beans", "Pulling the shot", "Steaming milk", "Ready at the counter"];

    private static string AdvanceStatus()
    {
        var current = (string?)A2UIRunContext.GetSurfaceData(CoffeeShopSurfaces.OrderSurfaceId)?["status"];
        var next = Statuses[Math.Min(Array.IndexOf(Statuses, current) + 1, Statuses.Length - 1)];

        A2UIEmitter.Emit([UpdateDataModelMessage.Set(
            CoffeeShopSurfaces.OrderSurfaceId,
            "/status",
            JsonValue.Create(next))]);

        return next;
    }

    private static string StartOver()
    {
        A2UIEmitter.Delete(CoffeeShopSurfaces.OrderSurfaceId);
        return ShowMenu();
    }

    private static int Index(JsonNode? value) =>
        int.TryParse((string?)value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            ? index
            : -1;
}

/// <summary>An in-memory session. The A2A host keeps one per conversation.</summary>
internal sealed class CoffeeShopSession : AgentSession
{
    internal CoffeeShopSession()
    {
    }

    internal CoffeeShopSession(AgentSessionStateBag stateBag)
        : base(stateBag)
    {
    }
}
