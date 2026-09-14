# Rendering in .NET

Two packages draw A2UI on the client side.

| Package | Contents | Dependencies |
|---|---|---|
| `Strathweb.A2UI.Rendering` | Surface state, binding resolution, templates, the Basic Catalog functions, checks, action dispatch | `Strathweb.A2UI.Protocol` |
| `Strathweb.A2UI.Blazor` | `A2UISurfaceView`, a Blazor component that draws a surface and its inputs | `Strathweb.A2UI.Rendering`, `Microsoft.AspNetCore.Components.Web` |

## The renderer core

`A2UIRendererSession` holds every live surface for one conversation and routes the agent's messages:

```csharp
var renderer = new A2UIRendererSession();

renderer.SurfaceCreated += (_, surface) => Show(surface);
renderer.SurfaceDeleted += (_, surface) => Hide(surface);

if (A2UIParts.TryRead(part, out var messages))
{
    renderer.Apply(messages);
}
```

Each `A2UIRenderSurface` wraps the protocol's `A2UISurfaceState` and adds what a UI needs:

- `Resolve` turns a `DynamicValue` into concrete JSON: a literal as is, a path from the data model, a
  function call by running it. `ResolveString`, `ResolveNumber`, `ResolveBoolean` and
  `ResolveStrings` read a component property that way.
- `Children` expands a `children` property: a fixed list of ids, or a template repeated once per item
  of a data model list, each child carrying an `A2UIDataScope` in which relative paths such as `name`
  resolve against that item.
- `TrySetValue` writes what the user typed back through the binding an input holds. A local edit is
  applied as an `updateDataModel` to the same state, so the two paths cannot disagree.
- `Check` and `CheckAll` evaluate the `checks` on input components; the renderer runs them before
  dispatching an action, so the user is told what is wrong without a round trip.
- `CreateAction` builds the `ActionMessage` for a component's `event`, with the context resolved in
  the right scope. A `functionCall` action runs locally instead; `openUrl` raises `OpenUrlRequested`
  for http and https URLs only.

`A2UIFunctions` implements the catalog's functions: `required`, `regex`, `email`, `length`,
`numeric`, `and`, `or`, `not`, `formatString` with `${...}` interpolation of paths, literals and nested
calls, `formatNumber`, `formatCurrency`, `formatDate` with the TR35 tokens the catalog documents,
`pluralize`, and `openUrl`.

What goes back to the agent is the renderer's business: `SupportedCatalogIds` for
`a2uiClientCapabilities`, and `DataModels()` for `a2uiClientDataModel`, which returns the data model
of every surface created with `sendDataModel`. `A2UIMetadata` in the A2A package writes both.

## The Blazor component

```razor
<link rel="stylesheet" href="_content/Strathweb.A2UI.Blazor/a2ui.css" />

<A2UISurfaceView Surface="surface" OnAction="SendAsync" OnOpenUrl="Open" />
```

`A2UISurfaceView` re-renders when the surface changes, from the agent or from the user. Inputs write
through to the data model on change. A button runs every check on the surface first; if one fails the
messages appear under the offending fields and nothing is sent. Otherwise `OnAction` receives the
`ActionMessage` to post to the agent, and the buttons are disabled until it returns.

Every Basic Catalog component renders: `Text` with inline Markdown (emphasis, strong, code, line
breaks, HTML-encoded first), `Image`, `Icon` as a text glyph, `Video`, `AudioPlayer`, `Row`, `Column`,
`List`, `Card`, `Tabs`, `Modal`, `Divider`, `Button`, `TextField` in its four variants, `CheckBox`,
`ChoicePicker` as a list or as chips with an optional filter, `Slider`, `DateTimeInput`. A component
the catalog does not define renders as a labelled placeholder rather than nothing.

The stylesheet is neutral and driven by CSS variables (`--a2ui-accent`, `--a2ui-bg`, `--a2ui-radius`
and so on) on `.a2ui-surface`, so a host can restyle it without touching markup. Icons are text glyphs
by default; a host with an icon font can target `.a2ui-icon[data-icon]`.

## Samples

- [samples/StreamingChat](../samples/StreamingChat): a chat page over `message/stream` with surfaces
  painted mid-reply.
- [samples/BookingWizard](../samples/BookingWizard): a three-step form on one surface.
- [samples/GenerativeDashboard](../samples/GenerativeDashboard): the model writes the surface itself.

All three share [samples/Shared](../samples/Shared), a small library with the A2A chat session, the
transcript and the wire log.
