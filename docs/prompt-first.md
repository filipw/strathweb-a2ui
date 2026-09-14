# Prompt-first generation

There are two ways to get a surface out of an agent. A **tool** can build one with the typed builders
and emit it; that is what [getting started](getting-started.md) shows, and it is deterministic. Or the
**model can write the A2UI JSON itself**, guided by a system prompt that carries the catalog. That is
the mode A2UI's own announcements lead with, and it is what `A2UIAgentOptions.PromptFirst` turns on.
Both can be on at once.

## The prompt

```csharp
var catalog = A2UICatalogs.Basic(A2UIVersion.V0_9_1);

var instructions = A2UISystemPrompt.Generate(catalog, new A2UIPromptOptions
{
    RoleDescription = "You are a sales analyst. Answer questions with a dashboard and one sentence.",
    UiDescription = "A Card with a heading, a Row of headline metrics, and a List of regions.",
    IncludeSchema = true,
});

var agent = chatClient
    .AsAIAgent(instructions: instructions, name: "analyst")
    .WithA2UI(o => o.PromptFirst = new A2UIPromptFirstOptions { Catalog = catalog });
```

`A2UISystemPrompt.Generate` writes the same sections the reference Python SDK writes, so material
written for one reads the same to a model here: the role, a workflow description with the rules the
response must follow, the catalog's own instructions, an optional UI description, and with
`IncludeSchema` the message schema, the common types and the catalog schema between
`---BEGIN A2UI JSON SCHEMA---` and `---END A2UI JSON SCHEMA---`. `AllowedComponents` reduces the
catalog schema to a subset; `InlineCatalogs` merges components a renderer sent; `Examples` adds
worked examples, loadable from a directory with `A2UIPromptExample.FromDirectory`.

The schemas are large. Leave `IncludeSchema` off for a model that already knows A2UI, or restrict
`AllowedComponents` to what the agent needs.

## What the agent does with the reply

The model wraps each payload in `<a2ui-json>` and `</a2ui-json>` tags. `A2UIAgent` takes them out of
the reply and turns them into surfaces:

1. **Parses** each block. With `RepairPayloads` (the default) a trailing comma, a typographic quote, a
   markdown fence or a single message outside its array are fixed first, without a round trip. In a
   streaming run the block is read as it arrives; the prose around it is passed through as soon as it
   cannot be the start of a tag, and the surface is emitted the moment the block closes.
2. **Validates** the messages against the catalog: envelope shape, unique ids, a root, no dangling
   references, no cycles, known components and properties. A block that only updates a surface the
   renderer already holds is allowed to refer to components it does not restate.
3. **Sends invalid blocks back.** Up to `MaxModelRepairs` times (default one), the model receives a
   user message with the validation errors and `RepairInstruction`, and its answer is read the same
   way. The prose of a repair round is not shown to the user; the surface is.
4. **Applies `OnInvalid`** when a block is still wrong: `Drop` logs a warning and keeps the model's
   prose; `Throw` fails the run with `A2UIValidationException`.

The surfaces reach the renderer exactly as tool-built ones do, as `A2UIContent` on a message of its
own, and are recorded in the session's surface registry. The model's own text, with the blocks
removed, is what the user reads.

## Catalog negotiation

A renderer says which catalogs it can draw in `a2uiClientCapabilities`. `A2UICatalogSelector.Select`
picks the one to generate from, in the renderer's order of preference, and merges any inline catalogs
the renderer sent when the agent accepts them. Pass the result as `PromptFirst.Catalog` and into the
prompt. The vendored conformance cases for `select_catalog`, `generate_prompt` and `fix_payload` run
against these types; see [conformance](conformance.md).

## Limits

- The stream parser tracks strings with straight double quotes. A typographic quote used as a string
  delimiter inside a streamed block is repaired only once the block is complete.
- Progressive rendering of a partially received component tree, as the reference renderers do, is
  not implemented; a surface appears when its block closes.
- Repair rounds cost a model call each and are recorded in the session history like any other turn.
