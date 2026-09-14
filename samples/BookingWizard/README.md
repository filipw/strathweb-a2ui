# BookingWizard

A three-step table booking on a single A2UI surface, rendered by the Blazor renderer and driven by
a deterministic agent. No model, no API key.

```bash
dotnet run --project samples/BookingWizard
```

## What it demonstrates

- **Steps by `updateComponents`.** Moving between steps replaces the `step` and `nav` components by
  id. The card, the heading and everything the user typed stay where they are.
- **Client-side checks.** `Check.Required` on the name, date and time fields. The renderer evaluates
  them before dispatching an action, so Next does nothing until the step is complete and the failing
  fields say why.
- **The rest of the Basic Catalog.** `Slider`, `DateTimeInput`, `CheckBox`, `ChoicePicker` as chips,
  `Tabs`, `Modal`, and `formatString` bindings on the review step that read the live data model.
- **`sendDataModel`.** The agent never keeps its own copy of the form. It reads the current step and
  the answers out of the data model the renderer returns with every message.
- **Deletion and recreation.** Booking another table deletes the surface and creates it again.

## Layout

| File | Contents |
|---|---|
| `BookingSurfaces.cs` | The surface, the three steps, the navigation rows, the confirmation |
| `BookingAgent.cs` | Reacts to `next`, `back`, `confirm` and `start_over` |
| `Program.cs` | Agent over A2A, session store, Blazor host, agent card |
