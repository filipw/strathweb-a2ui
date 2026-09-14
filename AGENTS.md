# AGENTS.md

House rules for working in this repository.

## Ground truth

- `spec/` is vendored from `a2ui-project/a2ui` at the SHA in `spec/SPEC_VERSION`. Never hand-edit it.
  Re-vendor with `scripts/sync-spec.sh`.
- The vendored schemas and conformance YAML win over anything written here or in `docs/`.
- Never invent protocol fields, message names, metadata keys or error codes. If it is not in the
  vendored schema or conformance YAML, it does not exist.

## Project discipline

- `Strathweb.A2UI.Protocol` has zero non-BCL package references. Enforced by a test.
- No shipping project references `Microsoft.Agents.AI.Hosting.*`, `Microsoft.AspNetCore.*`, `OpenAI`
  or `AGUI.*`. Those appear only under `samples/` and `tests/`.
- Central Package Management: versions go in `Directory.Packages.props`, never on a `PackageReference`.

## Code style

- Nullable reference types enabled, warnings as errors.
- One public type per file; file name matches the type.
- Public types `sealed` unless designed for inheritance.
- Sealed classes over records for public wire types: records leak `with`, equality and `ToString` into
  the public contract.
- XML docs on every public member, one line where possible. Internal comments only where the reasoning
  is non-obvious. No essays.
- Argument validation at every public entry point.
- `System.Text.Json` only. No reflection-based serialization in a path the trim/AOT test touches.

## Testing

- xUnit v3 on Microsoft.Testing.Platform: `dotnet test --solution A2UI.slnx`.
- Test names read as sentences: `Validate_EmptyComponentId_ReportsMissingIdNotDuplicate`.
- Protocol code is pure functions. Test it directly, no mocking frameworks.
- Every bug fix starts with a failing test.
- Conformance skips need a reason in `skip-list.yaml` or a rule in `ConformanceSkips.cs`. A case that
  is neither run nor accounted for fails the run. Keep `docs/conformance.md` in step.
- If a `HostConstraintTests` assertion fails after a package bump, stop. Do not adjust the test to
  match new behaviour without working out what changed on the wire.

## Commits

- Imperative subject under 72 chars; body explains why.

## Things that will bite you

- Components are a flat adjacency list, not a tree. Exactly one has `id == "root"`.
- `child` (singular) and `children` (plural, array *and* template form) are both child references.
  Validating only `children` lets dangling singular refs through.
- An empty-string component id is a *missing* id, not a duplicate. Adding every string to the
  duplicate set makes two empty ids flag each other.
- Cycle detection must be iterative. Generated input nests deeply enough to overflow the stack.
- A property is a component reference only if its catalog schema `$ref`s
  `common_types.json#/$defs/ComponentId` or `#/$defs/ChildList`. There is no `format: componentRef`
  marker. `"$289"` and `"card_1"` are indistinguishable by shape, so never guess.
- Metadata keys are version-dependent: `a2uiClientCapabilities` (v0.9.1) vs `a2uiRendererCapabilities`
  (v1.0). The wrong key is a silent no-op.
- `data` in an A2A part is always an array, even for one message.
- `RawRepresentation` must be set in `A2UIContent`'s constructor. Making it lazy breaks A2A transport
  silently.
- `updateDataModel` with an *omitted* `value` deletes the key; an explicit `value: null` writes null.
  A `JsonNode?` cannot express that, hence the presence flag.
- Tool return strings are prompt surface. A tool that shows a UI must say so, or the model describes
  the form in prose as well.
- An `AsyncLocal` set inside an async iterator is reverted at every `yield`. `A2UIAgent` re-enters its
  scopes around each `MoveNextAsync` of the inner run; a scope opened once at the top of
  `RunCoreStreamingAsync` is gone by the time a tool runs after the first update.
- `AIContent` is polymorphic over a closed set of types and the framework's default
  `JsonSerializerOptions` are read-only. A custom content type that reaches the chat history the
  wrapped agent stores breaks every session store on the next save. Surfaces travel on a message of
  their own that the inner agent never sees; inbound actions become `TextContent` plus
  `A2UIRunContext.Actions`.
- The scripted test doubles run their callback before the first yield and keep no chat history.
  Anything that touches streaming or sessions also needs a test on `ToolCallingChatClient`, which
  streams text before the function call the way real providers do.
