# CoffeeShop

An agent that takes a coffee order, and a browser front end that draws it.

Where [SurveyAgent](../SurveyAgent) shows the server side, this one closes the loop: the surfaces are
rendered by [`@a2ui/lit`](https://www.npmjs.com/package/@a2ui/lit), the official A2UI renderer, and
clicks come back to the agent as A2UI actions.

## Running

```bash
dotnet run --project samples/CoffeeShop
```

Open the printed URL. No API key: the agent is deterministic and reacts to the actions the renderer
sends rather than asking a model what to do. Surface building, emission and inbound handling are the
same code a model-backed agent's tools would run.

The renderer is loaded from esm.sh, so the page needs internet access but the sample needs no npm
install or bundler.

## What it demonstrates

- **Templates.** The menu and the basket are each one component repeated over a data model list, with
  relative paths (`Bind.Path("name")`) resolving per item.
- **`sendDataModel`.** The menu asks the renderer to return its data model with every message, so the
  basket lives on the client and the agent reads it back with
  `A2UIRunContext.GetSurfaceData(...)` instead of keeping a copy.
- **Partial updates.** Adding a drink sends `updateDataModel` for three paths. The components are
  never resent.
- **Multiple surfaces and deletion.** Placing an order deletes the menu surface and creates the
  confirmation.
- **Capability negotiation.** The client sends `a2uiClientCapabilities` in message metadata.
- **Session state.** `Program.cs` registers an `InMemoryAgentSessionStore`. Without one the A2A host
  hands the agent a fresh session every turn, and the basket silently resets to a single item.

The wire log on the right of the page lists every message in both directions.

## Layout

| File | Contents |
|---|---|
| `CoffeeShopSurfaces.cs` | The two surfaces, written once in C# |
| `CoffeeShopAgent.cs` | Reacts to actions and emits surfaces or updates |
| `Cart.cs` | Reads the basket out of the reported data model and writes it back |
| `wwwroot/app.js` | A2A transport and the renderer wiring; knows nothing about coffee |
