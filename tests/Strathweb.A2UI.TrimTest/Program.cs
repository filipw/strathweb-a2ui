// Trim/AOT smoke test. Exercises the protocol paths a consumer touches so that ILCompiler reports
// any reflection-based serialization that has crept in. The build treats such a warning as an error.
using System.Text.Json.Nodes;
using Strathweb.A2UI;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Parsing;
using Strathweb.A2UI.State;
using Strathweb.A2UI.Validation;
using Strathweb.A2UI.Values;

var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

List<A2UIMessage> messages =
[
    new CreateSurfaceMessage("trim_surface", catalog.CatalogId) { SendDataModel = true },
    new UpdateComponentsMessage("trim_surface",
    [
        new A2UIComponent("root", "Column").Set("children", ChildList.Of("prompt", "submit").ToJson()),
        new A2UIComponent("prompt", "Text").Set("text", DynamicValue.FromPath("/question")),
        new A2UIComponent("submit", "Button")
            .Set("child", "submit_label")
            .Set("action", A2UIAction.FromEvent(new A2UIEvent("submit")).ToJson()),
        new A2UIComponent("submit_label", "Text").Set("text", DynamicValue.FromString("Submit")),
    ]),
    UpdateDataModelMessage.Replace("trim_surface", new JsonObject { ["question"] = "Trim test?" }),
    UpdateDataModelMessage.Remove("trim_surface", "/question"),
    new DeleteSurfaceMessage("trim_surface"),
];

var json = A2UIJson.Serialize(messages);
var round = A2UIJson.DeserializeList(json);

if (round.Count != messages.Count)
{
    Console.Error.WriteLine($"Round trip lost messages: wrote {messages.Count}, read {round.Count}.");
    return 1;
}

if (A2UIJson.Serialize(round) != json)
{
    Console.Error.WriteLine("Round trip changed the payload.");
    return 1;
}

var action = A2UIJson.Deserialize(A2UIJson.Serialize(new ActionMessage(
    "submit",
    "trim_surface",
    "submit",
    DateTimeOffset.UnixEpoch,
    new JsonObject { ["rating"] = 5 })));

if (action is not ActionMessage { Name: "submit" })
{
    Console.Error.WriteLine("Action message did not round-trip.");
    return 1;
}

var validation = new A2UIValidator(new A2UIValidationOptions { Catalog = catalog })
    .Validate(messages.Take(2));

if (!validation.IsValid)
{
    Console.Error.WriteLine($"Validation failed unexpectedly:{Environment.NewLine}{validation}");
    return 1;
}

var streamParser = new A2UIStreamParser();
var streamed = streamParser.Feed($"Here you go. <a2ui-json>{json}</a2ui-json>").ToList();
streamed.AddRange(streamParser.Complete());

if (streamed.Count(p => p.IsA2UI) != 1)
{
    Console.Error.WriteLine("The stream parser did not produce exactly one A2UI part.");
    return 1;
}

var surfaces = new A2UISurfaceSet();
surfaces.Apply(messages.Take(3));

if (!surfaces.TryGet("trim_surface", out var surface) || surface.Components.Count != 4)
{
    Console.Error.WriteLine("Replaying the messages did not produce the expected surface.");
    return 1;
}

var profile = A2UIVersionProfile.For(A2UIVersion.V0_9_1);
Console.WriteLine(
    $"A2UI trim test OK: {round.Count} messages, {catalog.Components.Count} components, " +
    $"extension {profile.ExtensionUri}");
return 0;
