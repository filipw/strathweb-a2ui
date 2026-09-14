# Getting started

## Install

```bash
dotnet add package Strathweb.A2UI.AgentFramework
```

That brings in `Strathweb.A2UI.Protocol` and `Strathweb.A2UI.A2A`. Take the protocol package alone if
you only want the message types, validator and parser; it depends on nothing but the BCL.

## Show a surface from a tool

```csharp
[Description("Ask the user how satisfied they were with the resolution.")]
static string AskForUserSatisfaction(string question)
{
    var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);
    var s = A2UISurface.Create(A2UISurfaceId.New("survey"), catalog);
    var ui = s.Components;

    A2UIEmitter.Emit(s
        .Root(ui.Card(ui.Column(
            ui.Text(question).Variant(TextVariant.H3),
            ui.ChoicePicker()
                .Label("Your rating")
                .Options(("Very", "5"), ("Somewhat", "4"), ("Not at all", "1"))
                .Value(Bind.Path("/rating")),
            ui.Button("Submit").Primary().OnClick(Act.Event(
                "submit_satisfaction",
                ("rating", Bind.Path("/rating")))))))
        .WithData(data => data["rating"] = null)
        .Build());

    return "A satisfaction survey is now on the user's screen. Wait for their answer.";
}

var agent = chatClient
    .AsAIAgent(instructions: "...", tools: [AIFunctionFactory.Create(AskForUserSatisfaction)])
    .WithA2UI();

builder.Services.AddA2AServer(agent);
app.MapA2AJsonRpc(agent, "/a2a");
```

The tool's return string is prompt surface. Without it the model describes the form it has just shown
and the user is asked everything twice.

## Advertise A2UI on the agent card

```csharp
card.AddA2UIExtension(
    A2UIVersion.V0_9_1,
    supportedCatalogIds: [A2UICatalogs.Basic(A2UIVersion.V0_9_1).CatalogId]);
```

Optional. A renderer can simply look for A2UI data parts coming back, but advertising tells it to.

## Next

- [Authoring surfaces](surfaces.md)
- [Actions](actions.md)
- [Prompt-first generation](prompt-first.md), when the model should write the surface itself
- [Rendering in .NET](rendering.md), for the Blazor side of the wire
- [samples/SurveyAgent](../samples/SurveyAgent), which runs without a model key and serves the surface
  on its own for pasting into a renderer.
