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

`WithA2UI()` turns each inbound A2UI part into two things: an `A2UIActionContent` for code, and a
`TextContent` for the model reading *The user performed the "submit_satisfaction" action on surface
survey_01j8x9, with rating="5", comment="fast fix".*

Both matter. A raw JSON payload left in the conversation changes how the model writes.

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

## Session state is required for any of this

Which surfaces this session created is tracked in the agent session, so anything that spans turns
needs the host to persist sessions. `AddA2AServer` falls back to `NoopAgentSessionStore` when no
store is registered for the agent, which hands the agent a fresh session on every turn. Nothing
fails; surface tracking and data-model reconciliation simply stop working.

```csharp
builder.Services.AddKeyedSingleton<AgentSessionStore>(agent.Name, new InMemoryAgentSessionStore());
builder.Services.AddA2AServer(agent);
```

If `IgnoredSurfaceData` lists surfaces you know you created, this is why.
