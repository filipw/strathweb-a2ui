# Conformance and scope

The A2UI specification and its conformance suite are vendored under [`spec/`](../spec) at the commit
in `spec/SPEC_VERSION`, and are the source of truth for this repository. `scripts/verify-spec.sh`
fails if anything there has been hand-edited.

58 of the 206 vendored cases run against this implementation and pass:

| Action | Cases | Runs against |
|---|---|---|
| `validate` | 19 | `A2UIValidator` |
| `parse_full` | 9 | `A2UIResponseParser` |
| `fix_payload` | 7 | `A2UIPayloadRepair` |
| `select_catalog` | 8 | `A2UICatalogSelector` |
| `load_catalog` | 3 | `A2UICatalog.Load`, `WithoutStrictValidation` |
| `generate_prompt` | 2 | `A2UISystemPrompt` |
| `has_parts` | 3 | `A2UIResponseParser.ContainsA2UIBlock` |
| `try_activate`, `try_activate_extension` | 4 | `A2UIExtensionUris` |
| `create_a2ui_part`, `is_a2ui_part`, `select_newest` | 3 | `A2UIParts`, `A2UIExtensionUris` |

The other 148 are skipped with a stated reason. A case that is neither run nor accounted for fails the
test run, and the full breakdown is written to `conformance-coverage.md` and published by CI.

Every message the library emits is additionally validated against the vendored JSON Schemas in tests,
including all 24 worked examples the specification ships.

## What is not implemented

**v0.8** (86 cases). Not a supported protocol version. This includes the six `generate_prompt` and
three `get_extension` cases whose `args.version` is 0.8.

**v1.0** (4 cases). A release candidate upstream. `A2UIVersionProfile` exists so it can be added
without restructuring; see [versioning](versioning.md).

**`process_chunk`** (38 cases). These expect the streaming parser to synthesise placeholder components
while a tree arrives, withhold components until they are reachable from the root, and drop orphans.
That is progressive-rendering behaviour with naming conventions private to the reference SDK; it does
not appear in the specification. `A2UIStreamParser` separates prose from A2UI blocks and yields each
message as its closing brace arrives, at any chunk boundary, which is what the `parse_full` cases
describe.

**Reference-SDK internals** (`prune`, `render`, `convert_event`, `execute_tool`, `handle_rpc`,
`verify_cuttable_keys`). Schema pruning, ADK event conversion and the reference RPC handler have no
counterpart here.

## Deliberate limits

**Catalog conformance is structural.** The validator checks that a component type exists and that
every property it sets is one the catalog declares. It does not evaluate the component's JSON Schema,
so `"text": 123` passes. Full schema conformance needs a schema engine, and
`Strathweb.A2UI.Protocol` references nothing outside the BCL. Layer it above where a dependency is
acceptable; the schemas are in `spec/`, and the unit tests do exactly that with `JsonSchema.Net`.

**Recursion limits are configurable.** The suite asserts a logical depth limit of 50 and a
function-call depth limit of 5 without stating them anywhere in the specification; both come from the
reference implementation and are settable on `A2UIValidationOptions`. The limits are not what makes
the validator safe: the graph walk and the JSON walk both use an explicit stack, so raising or
removing them does not turn a deep payload into a stack overflow.

**The deprecated MIME type is never written.** `application/json+a2ui` is accepted on read for v0.8
and early v0.9 peers. Parts this library produces always carry `application/a2ui+json`.

**Capabilities are read under either v0.9 key.** `client_capabilities.json` requires the key `v0.9`,
but a renderer configured for v0.9.1 sends `v0.9.1` instead: `@a2ui/lit` 0.10.3 does exactly that.
Accepting only the schema-correct key would silently ignore those renderers, so both are read. This
library writes `v0.9`.
