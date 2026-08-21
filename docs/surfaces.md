# Authoring surfaces

A surface is a flat list of components referencing each other by id, plus a data model they bind to.

```csharp
var s = A2UISurface.Create(A2UISurfaceId.New("survey"), A2UICatalogs.Basic(A2UIVersion.V0_9_1));
var ui = s.Components;

var surface = s
    .Root(ui.Card(ui.Column(
        ui.Text("How satisfied were you?").Variant(TextVariant.H3),
        ui.ChoicePicker()
            .Label("Your rating")
            .MutuallyExclusive()
            .Options(("Very", "5"), ("Somewhat", "4"), ("Not at all", "1"))
            .Value(Bind.Path("/rating"))
            .Checks(Check.Required(Bind.Path("/rating"), "Please pick a rating.")),
        ui.TextField("Anything else?").Value(Bind.Path("/comment")),
        ui.Button("Submit").Primary().OnClick(Act.Event(
            "submit_satisfaction",
            ("rating", Bind.Path("/rating")),
            ("comment", Bind.Path("/comment")))))))
    .WithData(data =>
    {
        data["rating"] = null;
        data["comment"] = string.Empty;
    })
    .Build();
```

## Ids, enums and validation

The builder assigns component ids (`text_1`, `choicePicker_1`) and wires references from the object
graph. Use `.WithId("status_line")` only when you need to address a component later.

Catalog properties like `variant`, `justify`, `align` and `fit` are closed sets. A value outside the
set renders as nothing rather than failing, so the builders take enums: `TextVariant.H3`, not
`"heading"`.

`Build()` validates against the catalog and throws. Duplicate ids, dangling references, cycles,
unreachable components and malformed binding paths all show up on screen as an empty rectangle and
nowhere else, so none of them leave the process.

## Bindings and templates

`Bind.Path("/rating")` points at the data model. Inside a template a relative path addresses the
current item:

```csharp
var row = ui.Text(Bind.Path("title"));
s.Root(ui.Column().ChildrenFrom(row, "/restaurants"));
```

That renders one `Text` per entry in `/restaurants`, each reading its own `title`.

## Updating a live surface

```csharp
A2UIEmitter.Emit(A2UISurfaceUpdate.For(surfaceId, catalog)
    .SetData("/status", JsonValue.Create("On its way")));
```

A component sent in an update replaces the one with the same id, which requires that you gave it one
with `.WithId(...)`. Components without an explicit id get a fresh random one.

`A2UIEmitter.Delete(surfaceId)` removes a surface.

## Surface ids

An id must be unique for the renderer's whole lifetime, not just the current turn: reusing one
replaces whatever is on screen. `A2UISurfaceId.New("survey")` generates them.

## Escape hatch

The typed builders cover the Basic Catalog. Anything else goes through `.Set(name, value)`, and
`A2UIComponent` is public if you would rather build the list yourself.
