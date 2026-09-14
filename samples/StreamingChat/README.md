# StreamingChat

A chat page over A2A `message/stream`, drawn end to end in .NET: the agent streams its reply, a
tool puts a form on the screen mid-reply, and the Blazor renderer paints it while the text is still
arriving. The form comes back as an A2UI action.

```bash
dotnet run --project samples/StreamingChat
```

Open the printed URL. Without an API key the model is a scripted stand-in that calls the tool and
plans from the answers; set `OpenAI:ApiKey` (user secrets) or `OPENAI_API_KEY` for a real one.

## What it demonstrates

- **Streaming.** The page uses `A2AClient.SendStreamingMessageAsync`; each streamed message is applied
  as it arrives. Text tokens append to the transcript; A2UI parts go to the renderer.
- **A surface mid-reply.** `StreamSurfacesAsTheyAppear` is on, so the form is on the wire the moment
  the tool emits it, before the model's follow-up sentence finishes.
- **The Blazor renderer.** `A2UISurfaceView` from `Strathweb.A2UI.Blazor` draws the form, binds the
  inputs to the data model, runs the client-side `checks`, and dispatches the action.
- **Capabilities and data models.** Every request carries `a2uiClientCapabilities`; surfaces created
  with `sendDataModel` would carry their data back too.
- **The library's own log.** The right-hand panel merges the wire traffic with what the A2UI library
  logs: surfaces created, actions received, anything dropped.

## Layout

| File | Contents |
|---|---|
| `TripPlanner.cs` | The agent's instructions and the `plan_trip` tool that builds the form |
| `TripPlannerScript.cs` | The scripted model used when no key is configured |
| `Program.cs` | Agent over A2A, session store, Blazor host, agent card |
| `Components/Pages/Home.razor` | The page: `ChatPanel` and `WireLog` from the shared sample library |
