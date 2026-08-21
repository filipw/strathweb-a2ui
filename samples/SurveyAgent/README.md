# SurveyAgent

An ASP.NET Core agent served over A2A that answers with a form instead of a paragraph.

`AskForUserSatisfaction` is an ordinary agent tool. It calls `A2UIEmitter.Emit(...)` with a surface
defined in [SatisfactionSurvey.cs](SatisfactionSurvey.cs) and returns a sentence telling the model a
UI was shown. The agent decorator attaches the surface to the response and the A2A hosting layer puts
it on the wire as an `application/a2ui+json` data part.

## Running

```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project samples/SurveyAgent
dotnet run --project samples/SurveyAgent
```

| Route | Description |
|---|---|
| `POST /a2a` | A2A JSON-RPC endpoint. Requires a model. |
| `GET /.well-known/agent-card.json` | Agent card, advertising the A2UI extension. |
| `GET /surfaces/satisfaction` | The surface on its own, no model required. |

`GET /surfaces/satisfaction` returns exactly the message array a renderer applies. Paste it into the
A2UI Composer at [a2ui.org](https://a2ui.org) to see the form without wiring up a client.

```bash
curl -s localhost:5000/surfaces/satisfaction
```
