using System.Text.Json;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Catalogs;

/// <summary>The set of components and functions a renderer knows how to render, keyed by name.</summary>
public sealed class A2UICatalog
{
    private A2UICatalog(JsonObject source, string catalogId)
    {
        Source = source;
        CatalogId = catalogId;
    }

    /// <summary>Schema documents this catalog's components refer to by absolute URI, keyed by that URI.</summary>
    public IReadOnlyDictionary<string, JsonObject> ReferenceDocuments { get; private init; } =
        new Dictionary<string, JsonObject>(StringComparer.Ordinal);

    /// <summary>Returns a copy that can resolve <c>$ref</c>s into another schema document.</summary>
    /// <param name="documentUri">The URI the refs use, without any fragment.</param>
    /// <param name="document">The document.</param>
    /// <returns>A new catalog over the same source.</returns>
    public A2UICatalog WithReferenceDocument(string documentUri, JsonObject document)
    {
        Throw.IfNullOrEmpty(documentUri, nameof(documentUri));
        Throw.IfNull(document, nameof(document));

        var documents = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var pair in ReferenceDocuments)
        {
            documents[pair.Key] = pair.Value;
        }

        documents[documentUri] = document;

        return new A2UICatalog(Source, CatalogId)
        {
            Instructions = Instructions,
            ReferenceDocuments = documents,
        };
    }

    /// <summary>The catalog's unique identifier, conventionally a URL on a domain its author owns.</summary>
    public string CatalogId { get; }

    /// <summary>The catalog title, when the document declares one.</summary>
    public string? Title => ReadString("title");

    /// <summary>A human-readable description, when the document declares one.</summary>
    public string? Description => ReadString("description");

    /// <summary>
    /// Markdown design guidance for this catalog, when the document declares it. Formalised in v1.0;
    /// carried here for any version that supplies it.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>Component definitions, keyed by component type name. Each value is a JSON Schema.</summary>
    public JsonObject Components => Source["components"] as JsonObject ?? [];

    /// <summary>Function definitions, keyed by function name. Each value is a JSON Schema.</summary>
    public JsonObject Functions => Source["functions"] as JsonObject ?? [];

    /// <summary>
    /// The theme property schema, from the document's <c>$defs.theme</c> or top-level <c>theme</c>.
    /// </summary>
    public JsonObject? Theme =>
        (Source["$defs"] as JsonObject)?["theme"] as JsonObject ?? Source["theme"] as JsonObject;

    /// <summary>The catalog document exactly as it was read.</summary>
    public JsonObject Source { get; }

    /// <summary>
    /// Which properties of each component hold references to other components, worked out from the
    /// component schemas. Components with no reference properties are absent from the map.
    /// </summary>
    public IReadOnlyDictionary<string, A2UIComponentReferenceFields> ReferenceFields =>
        referenceFields ??= CatalogReferenceFieldReader.Read(Source);

    private IReadOnlyDictionary<string, A2UIComponentReferenceFields>? referenceFields;

    /// <summary>Gets a component's reference properties.</summary>
    /// <param name="componentName">The component type name.</param>
    /// <returns>The reference properties, or <see langword="null"/> when the component has none.</returns>
    public A2UIComponentReferenceFields? GetReferenceFields(string componentName) =>
        ReferenceFields.TryGetValue(Throw.IfNull(componentName, nameof(componentName)), out var fields)
            ? fields
            : null;

    /// <summary>Whether this catalog defines a component.</summary>
    /// <param name="componentName">The component type name, for example <c>Card</c>.</param>
    /// <returns><see langword="true"/> when the component is defined.</returns>
    public bool HasComponent(string componentName) =>
        Components.ContainsKey(Throw.IfNull(componentName, nameof(componentName)));

    /// <summary>Gets a component's JSON Schema.</summary>
    /// <param name="componentName">The component type name.</param>
    /// <returns>The schema, or <see langword="null"/> when the catalog does not define it.</returns>
    public JsonObject? GetComponent(string componentName) =>
        Components[Throw.IfNull(componentName, nameof(componentName))] as JsonObject;

    /// <summary>Whether this catalog defines a function.</summary>
    /// <param name="functionName">The function name, for example <c>required</c>.</param>
    /// <returns><see langword="true"/> when the function is defined.</returns>
    public bool HasFunction(string functionName) =>
        Functions.ContainsKey(Throw.IfNull(functionName, nameof(functionName)));

    /// <summary>Reads a catalog from a parsed document.</summary>
    /// <param name="node">The catalog document.</param>
    /// <returns>The catalog.</returns>
    /// <exception cref="A2UICatalogException">The document is not an object with a string <c>catalogId</c>.</exception>
    public static A2UICatalog FromJson(JsonNode? node)
    {
        if (node is not JsonObject obj || obj["catalogId"] is not JsonValue idValue ||
            !idValue.TryGetValue<string>(out var catalogId))
        {
            throw new A2UICatalogException("A catalog must be an object with a string 'catalogId' property.");
        }

        var source = (JsonObject)obj.DeepClone();
        NormalizeFunctions(source);

        return new A2UICatalog(source, catalogId)
        {
            Instructions = (source["instructions"] as JsonValue)?.TryGetValue<string>(out var instructions) == true
                ? instructions
                : null,
        };
    }

    /// <summary>Reads a catalog from a <c>catalog.json</c> stream.</summary>
    /// <param name="stream">The stream to read. Not disposed.</param>
    /// <returns>The catalog.</returns>
    /// <exception cref="A2UICatalogException">The stream does not contain a catalog document.</exception>
    public static A2UICatalog Load(Stream stream)
    {
        Throw.IfNull(stream, nameof(stream));

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(stream);
        }
        catch (JsonException ex)
        {
            throw new A2UICatalogException("The stream does not contain valid JSON.", ex);
        }

        return FromJson(node);
    }

    /// <summary>
    /// Returns a copy carrying different design guidance. Useful for catalogs whose instructions live
    /// beside the document rather than inside it, as the Basic Catalog's do.
    /// </summary>
    /// <param name="instructions">The guidance, or <see langword="null"/> to drop it.</param>
    /// <returns>A new catalog over the same document.</returns>
    public A2UICatalog WithInstructions(string? instructions) =>
        new(Source, CatalogId) { Instructions = instructions, ReferenceDocuments = ReferenceDocuments };

    /// <summary>Writes the catalog document.</summary>
    /// <returns>A copy of the source document.</returns>
    public JsonObject ToJson() => (JsonObject)Source.DeepClone();

    /// <summary>
    /// Returns a copy that also defines another catalog's components. A component defined in both is
    /// taken from <paramref name="other"/>. The id, functions and everything else stay this catalog's.
    /// </summary>
    /// <param name="other">The catalog whose components to add, typically one a renderer sent inline.</param>
    /// <returns>A new catalog.</returns>
    public A2UICatalog WithComponentsFrom(A2UICatalog other)
    {
        Throw.IfNull(other, nameof(other));

        var source = (JsonObject)Source.DeepClone();
        var components = source["components"] as JsonObject ?? [];
        source["components"] = components;

        foreach (var pair in other.Components)
        {
            components[pair.Key] = pair.Value?.DeepClone();
        }

        return Derive(source);
    }

    /// <summary>
    /// Returns a copy that defines only the named components, for offering a model a smaller set than
    /// the catalog has. Names the catalog does not define are ignored.
    /// </summary>
    /// <param name="componentNames">The components to keep.</param>
    /// <returns>A new catalog.</returns>
    public A2UICatalog WithOnlyComponents(IEnumerable<string> componentNames)
    {
        Throw.IfNull(componentNames, nameof(componentNames));

        var keep = new HashSet<string>(componentNames, StringComparer.Ordinal);
        var source = (JsonObject)Source.DeepClone();
        var components = new JsonObject();

        foreach (var pair in Components)
        {
            if (keep.Contains(pair.Key))
            {
                components[pair.Key] = pair.Value?.DeepClone();
            }
        }

        source["components"] = components;
        return Derive(source);
    }

    /// <summary>
    /// Returns a copy with every <c>additionalProperties: false</c> and
    /// <c>unevaluatedProperties: false</c> removed, so a schema-driven validator tolerates properties
    /// the catalog does not declare. Structural validation in this library is unaffected.
    /// </summary>
    /// <returns>A new catalog.</returns>
    public A2UICatalog WithoutStrictValidation()
    {
        var source = (JsonObject)Source.DeepClone();
        RemoveStrictKeywords(source);
        return Derive(source);
    }

    private A2UICatalog Derive(JsonObject source) =>
        new(source, CatalogId) { Instructions = Instructions, ReferenceDocuments = ReferenceDocuments };

    private static void RemoveStrictKeywords(JsonNode? node)
    {
        // Iterative: catalogs nest deeply and a generated one could nest deeper.
        var stack = new Stack<JsonNode>();
        if (node is not null)
        {
            stack.Push(node);
        }

        while (stack.Count > 0)
        {
            switch (stack.Pop())
            {
                case JsonObject obj:
                    foreach (var keyword in StrictKeywords)
                    {
                        if (obj[keyword] is JsonValue value && value.TryGetValue<bool>(out var strict) && !strict)
                        {
                            obj.Remove(keyword);
                        }
                    }

                    foreach (var pair in obj)
                    {
                        if (pair.Value is not null)
                        {
                            stack.Push(pair.Value);
                        }
                    }

                    break;

                case JsonArray array:
                    foreach (var item in array)
                    {
                        if (item is not null)
                        {
                            stack.Push(item);
                        }
                    }

                    break;
            }
        }
    }

    private static readonly string[] StrictKeywords = ["additionalProperties", "unevaluatedProperties"];

    /// <summary>
    /// Rewrites the array-of-objects <c>functions</c> encoding used by inline catalogs in A2A
    /// capabilities into the name-keyed map a catalog document uses, so callers see one shape.
    /// </summary>
    private static void NormalizeFunctions(JsonObject source)
    {
        if (source["functions"] is not JsonArray array)
        {
            return;
        }

        var map = new JsonObject();
        foreach (var item in array)
        {
            if (item is not JsonObject function || function["name"] is not JsonValue nameValue ||
                !nameValue.TryGetValue<string>(out var name))
            {
                throw new A2UICatalogException(
                    "An inline catalog's 'functions' array must contain objects with a string 'name'.");
            }

            var definition = (JsonObject)function.DeepClone();
            definition.Remove("name");
            map[name] = definition;
        }

        source["functions"] = map;
    }

    private string? ReadString(string name) =>
        Source[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}
