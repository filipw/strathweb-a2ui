using Strathweb.A2UI.Catalogs;

namespace Strathweb.A2UI.AgentFramework;

/// <summary>
/// Prompt-first generation: the model writes A2UI JSON in <c>&lt;a2ui-json&gt;</c> blocks and the
/// agent turns them into surfaces. Pair this with a system prompt from
/// <see cref="Prompting.A2UISystemPrompt"/> in the wrapped agent's instructions.
/// </summary>
public sealed class A2UIPromptFirstOptions
{
    /// <summary>
    /// The catalog generated payloads are validated against. Defaults to the Basic Catalog for the
    /// agent's version.
    /// </summary>
    public A2UICatalog? Catalog { get; set; }

    /// <summary>
    /// Whether each block is run through <see cref="Parsing.A2UIPayloadRepair"/> before parsing, so
    /// trailing commas, typographic quotes and a missing array wrapper do not cost a model round trip.
    /// </summary>
    public bool RepairPayloads { get; set; } = true;

    /// <summary>
    /// How many times an invalid payload is sent back to the model with the validation errors, asking
    /// for a corrected block. Zero turns model repair off. Defaults to one.
    /// </summary>
    public int MaxModelRepairs { get; set; } = 1;

    /// <summary>What happens when a payload is still invalid after every repair.</summary>
    public A2UIInvalidPayloadPolicy OnInvalid { get; set; } = A2UIInvalidPayloadPolicy.Drop;

    /// <summary>
    /// The message sent to the model when a payload is invalid. <c>{errors}</c> is replaced with the
    /// validation errors, one per line.
    /// </summary>
    public string RepairInstruction { get; set; } =
        "Your previous response was invalid. The A2UI payload in it could not be shown:\n{errors}\n" +
        "You MUST generate a valid response that strictly follows the A2UI JSON schema and the catalog. " +
        "The response MUST be a JSON list of A2UI messages wrapped in <a2ui-json> and </a2ui-json> tags. " +
        "Reply with only the corrected <a2ui-json> block, and nothing else.";
}
