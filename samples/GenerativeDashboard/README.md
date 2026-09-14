# GenerativeDashboard

Prompt-first A2UI. The model writes the dashboard itself as A2UI JSON inside `<a2ui-json>` tags;
the agent parses the blocks as they stream, validates them against the Basic Catalog, asks the model
to repair an invalid one, and hands the result to the Blazor renderer.

```bash
dotnet run --project samples/GenerativeDashboard
```

Open the printed URL. Without an API key a scripted stand-in writes the JSON, including a deliberately
broken version on request; set `OpenAI:ApiKey` (user secrets) or `OPENAI_API_KEY` to have a real model
write it from the prompt. `GET /prompt` shows that prompt.

## What it demonstrates

- **The system prompt.** `A2UISystemPrompt.Generate` builds it from a role, a UI description and the
  catalog, with the message schema, common types and catalog schema embedded.
- **Parsing while streaming.** `A2UIAgentOptions.PromptFirst` turns the model's blocks into surfaces
  as each block closes; the prose around them is what the user sees, the tags and JSON are not.
- **Repair.** Ask for a broken dashboard: the first block has an invented property and an unknown
  component. The library validates, logs the errors, sends them back to the model, and the corrected
  block is what renders. Sloppy JSON such as trailing commas is fixed without a round trip.
- **Updating a live surface.** Refresh makes the model write an `updateDataModel` block; the numbers
  change, the components do not.

## Layout

| File | Contents |
|---|---|
| `Dashboard.cs` | The role and UI description for the prompt, and the JSON the scripted model writes |
| `DashboardScript.cs` | The scripted model: valid, broken, repaired, refreshed |
| `Program.cs` | Prompt generation, prompt-first options, agent over A2A, Blazor host |
