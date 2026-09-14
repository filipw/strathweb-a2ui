# A2UI for .NET

Send [A2UI](https://a2ui.org) surfaces from a [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
agent hosted over the [A2A protocol](https://a2a-protocol.org), and read the user's answers back. No
changes to the agent framework, no fork.

A2UI is an open declarative UI format. Instead of a bespoke JSON payload per widget with a matching
handler in the front end, the agent sends components from a catalog the client already knows, and an
off-the-shelf renderer draws them.

Targets A2UI **v0.9.1**. v1.0 is a release candidate upstream and is not implemented; see
[docs/versioning.md](docs/versioning.md). Pre-release.

## Packages

| Package | Contents | Dependencies |
|---|---|---|
| `Strathweb.A2UI.Protocol` | Messages, components, catalogs, validator, parser, surface builder | BCL only |
| `Strathweb.A2UI.A2A` | Data parts, agent card advertisement, capability metadata | `A2A` |
| `Strathweb.A2UI.AgentFramework` | Emit surfaces from tools, read actions back, prompt-first generation | `Microsoft.Agents.AI.Abstractions` |
| `Strathweb.A2UI.Rendering` | Renderer-side state: bindings, templates, catalog functions, checks, actions | BCL only |
| `Strathweb.A2UI.Blazor` | `A2UISurfaceView`, a Blazor component that draws a surface | `Microsoft.AspNetCore.Components.Web` |

## Usage

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

    // The return value is what the model reads. Without this it describes the form it just showed.
    return "A satisfaction survey is now on the user's screen. Wait for their answer.";
}

var agent = chatClient
    .AsAIAgent(instructions: "...", tools: [AIFunctionFactory.Create(AskForUserSatisfaction)])
    .WithA2UI();

builder.Services.AddA2AServer(agent);
app.MapA2AJsonRpc(agent, "/a2a");
```

The surface leaves as an `application/a2ui+json` data part. The user's answer arrives on a later turn
as an `ActionMessage` on `A2UIRunContext.Actions`, and as a plain sentence for the model.

Or let the model write the surface itself. `A2UISystemPrompt.Generate` builds the system prompt from
the catalog, and `PromptFirst` turns the `<a2ui-json>` blocks in the reply into surfaces, validated
against the catalog and sent back to the model for repair when they are wrong. See
[prompt-first generation](docs/prompt-first.md).

On the other side of the wire, `Strathweb.A2UI.Blazor` draws the surfaces in a Blazor page. See
[rendering in .NET](docs/rendering.md).

## What it handles

- Component ids. A surface is a flat adjacency list; one wrong id renders blank with no error. You
  write an object graph, the builder writes the ids.
- Validation at `Build()`: duplicate ids, dangling references, cycles, unreachable components,
  malformed binding paths, unknown component types.
- Inbound actions become a sentence for the model rather than a JSON blob, and nothing is added to
  the chat history that a session store cannot serialize.
- Silent failures are logged: a surface from a catalog the renderer did not advertise, a data model
  for a surface this session did not create, an inbound part that could not be read or was too large.
- No reflection-based serialization; the protocol package has no dependencies.

## Conformance

The specification and its conformance suite are vendored under [`spec/`](spec/) at a pinned commit
and are the source of truth for this repository.

58 of the 206 vendored cases run and pass: every runnable v0.9 validator case, every `parse_full`,
`fix_payload`, `select_catalog` and `load_catalog` case, the v0.9 `generate_prompt` cases, and the A2A
extension cases for parts and negotiation. The other 148 are skipped with a stated reason, and the
test run fails if a case is neither run nor accounted for. The largest groups are 86 v0.8 cases and 38
`process_chunk` cases (progressive rendering that exists in the reference SDK rather than the
specification). CI publishes the breakdown as an artifact.

Every emitted message is additionally validated against the vendored JSON Schemas in tests, including
all 24 worked examples the specification ships.

## Documentation

- [Getting started](docs/getting-started.md)
- [Authoring surfaces](docs/surfaces.md)
- [Actions](docs/actions.md)
- [Prompt-first generation](docs/prompt-first.md)
- [Rendering in .NET](docs/rendering.md)
- [Protocol versions](docs/versioning.md)
- [Conformance and scope](docs/conformance.md)
- [samples/CoffeeShop](samples/CoffeeShop), an agent plus a browser front end using the official
  `@a2ui/lit` renderer
- [samples/SurveyAgent](samples/SurveyAgent), the server side on its own
- [samples/StreamingChat](samples/StreamingChat), a chat page over `message/stream` drawn by the Blazor
  renderer, with a form painted mid-reply
- [samples/BookingWizard](samples/BookingWizard), a three-step form on one surface
- [samples/GenerativeDashboard](samples/GenerativeDashboard), the model writes the surface itself

## Building

Requires the .NET 10 SDK.

```bash
dotnet build A2UI.slnx
dotnet test --solution A2UI.slnx
```

`dotnet test` needs the `--solution` form: xUnit v3 runs on Microsoft.Testing.Platform.

Re-vendor the specification and check nothing has hand-edited it:

```bash
./scripts/sync-spec.sh <sha>
./scripts/verify-spec.sh
```

## License

MIT. See [LICENSE](LICENSE). The vendored material under `spec/` carries its own upstream Apache-2.0
license.
