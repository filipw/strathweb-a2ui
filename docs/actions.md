# Actions

## What the renderer sends

An ordinary A2A message with a data part carrying an `action`:

```json
{
  "version": "v0.9.1",
  "action": {
    "name": "submit_satisfaction",
    "surfaceId": "survey_01j8x9",
    "sourceComponentId": "button_1",
    "timestamp": "2026-08-21T09:30:00Z",
    "context": { "rating": "5", "comment": "fast fix" }
  }
}
```

`name` and the `context` keys are whatever the surface author chose in `Act.Event(...)`. The renderer
resolves bindings before sending, so the values are concrete.

## What the agent sees

`WithA2UI()` replaces each inbound A2UI part with a `TextContent` for the model reading *The user
performed the "submit_satisfaction" action on surface survey_01j8x9, with rating="5", comment="fast
fix".* The structured `ActionMessage` is on `A2UIRunContext.Actions` for code.

A raw JSON payload left in the conversation changes how the model writes, so the part itself is gone.
Nothing but text goes into the message: the wrapped agent stores that message in the chat history it
keeps in the session, and a content type the framework cannot serialize there breaks every session
store on the next save.

```csharp
foreach (var action in A2UIRunContext.Actions)
{
    if (action.Name == "submit_satisfaction")
    {
        var rating = (string?)action.Context["rating"];
    }
}
```

## Tools do not block

A tool that shows a surface returns immediately. The answer arrives on a later turn as a fresh user
message, not as that tool call's result. The tool's return value tells the model a UI is on screen
and that it should stop.

## Detecting an A2UI part

By the time an inbound data part reaches the agent it is a `DataContent` labelled `application/json`;
the A2A conversion does not promote the A2UI MIME type onto the content. The real type is in the
part's metadata:

```csharp
content.RawRepresentation is Part part && A2UIParts.IsA2UI(part)
```

Matching on `DataContent.MediaType` compiles and never matches.

## Renderer errors

A renderer that cannot display a surface sends an `error`. Those are normalized into a sentence too
and collected on `A2UIRunContext.Current.Errors`. A `VALIDATION_FAILED` error carries a JSON Pointer
to the offending field.

## Data models

`.SendDataModel()` asks the renderer to return the surface's whole data model in the metadata of every
message it sends. Off by default: it ships every field the user has typed on every message.

```csharp
var data = A2UIRunContext.GetSurfaceData("survey_01j8x9");
```

Per the specification a data model goes only to the agent that created the surface. Data for a surface
this session did not create is dropped before it reaches your code, and the surface id is listed on
`A2UIRunContext.Current.IgnoredSurfaceData`.

## The renderer is not trusted

Everything in an action's `context` and in a reported data model was produced by the client. Treat it
like a form post: resolve prices, permissions and identifiers on the server, never from what came
back. The CoffeeShop sample reads drink ids out of the reported basket and looks the prices up in its
own menu.

Inbound payloads are capped by `A2UIAgentOptions.InboundLimits`. A data part above `MaxPartBytes`
(256 KiB) is reported to the model as ignored rather than read, a data model payload above
`MaxDataModelBytes` (256 KiB) is dropped whole, and each value in the sentence the model reads is cut
at `MaxDescribedValueLength` characters (500).

## Logging

A2UI fails silently by design: a surface from a catalog the renderer does not know renders blank, and
a data model for a surface the agent does not recognise is discarded. `A2UIAgent` logs both, along
with every surface created or deleted, every action and renderer error received, and every inbound
part it could not read. Logging goes through `A2UIAgentOptions.LoggerFactory`, or the
`ILoggerFactory` the wrapped agent exposes through `GetService`, under the category
`Strathweb.A2UI.AgentFramework.A2UIAgent`.

`A2UIAgentOptions.UnsupportedCatalogPolicy` decides what happens when a tool emits a surface from a
catalog the renderer did not list in `a2uiClientCapabilities`. `Warn`, the default, sends it and logs.
`Drop` keeps it off the wire. `Throw` makes `A2UIEmitter.Emit` throw `InvalidOperationException`, so
the tool can fall back to text. Nothing is checked on a turn where the renderer sent no capabilities.

## Session state is required for any of this

Which surfaces this session created is tracked in the agent session, so anything that spans turns
needs the host to persist sessions. `AddA2AServer` falls back to `NoopAgentSessionStore` when no
store is registered for the agent, which hands the agent a fresh session on every turn. Nothing
fails; surface tracking and data-model reconciliation simply stop working.

```csharp
builder.Services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore());
builder.Services.AddA2AServer(agent);
```

A store serializes the session after every turn, including the chat history the wrapped agent keeps
in it. That is why the surfaces and actions this library handles never enter that history as custom
content.

If `IgnoredSurfaceData` lists surfaces you know you created, this is why, and the warning logged for
each of them says so.
