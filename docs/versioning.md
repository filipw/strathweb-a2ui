# Protocol versions

This library targets A2UI v0.9.1. v1.0 is a release candidate upstream and is not implemented;
`A2UIVersionProfile` exists so it can be added without restructuring. v0.8 is not supported.

Verified against `spec/` at the SHA in `spec/SPEC_VERSION`. Re-check on every spec bump.

| Aspect | v0.9.1 | v1.0 (not implemented) |
|---|---|---|
| Terminology | client / server | renderer / agent |
| Agent to client schema | `server_to_client.json` | `agent_to_renderer.json` |
| Client to agent schema | `client_to_server.json` | `renderer_to_agent.json` |
| List schema | `server_to_client_list.json` | `agent_to_renderer_list.json` |
| Capabilities schemas | `client_capabilities.json`, `server_capabilities.json` | `renderer_capabilities.json`, `agent_capabilities.json` |
| Data model schema | `client_data_model.json` | `renderer_data_model.json` |
| Catalog schema | inside each catalog's `catalog.json` | `catalog_definition.json` |
| Capabilities metadata key | `a2uiClientCapabilities` | `a2uiRendererCapabilities` |
| Data model metadata key | `a2uiClientDataModel` | `a2uiRendererDataModel` |
| Capabilities version key | `"v0.9"` | `"v1.0"` |
| `version` on each envelope | required, `["v0.9", "v0.9.1"]` | `"v1.0"` |
| `createSurface.catalogId` | required | optional |
| `createSurface.theme` | present | removed |
| Inline components/data in `createSurface` | not allowed | allowed |
| Function-call messages | none | four |
| A2A extension URI | `https://a2ui.org/a2a-extension/a2ui/v0.9.1` | `.../v1.0` |
| MIME type | `application/a2ui+json` | `application/a2ui+json` |
| Legacy MIME | `application/json+a2ui`, accepted on read only | n/a |

## Version skew

An agent emitting a version the renderer does not support produces a blank surface and no error. The
renderer advertises what it supports in message metadata under the version-appropriate capabilities
key. A renderer advertising a version the surface was not built for is logged rather than ignored.
