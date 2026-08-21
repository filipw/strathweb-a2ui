using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Changes a surface the renderer already holds, without rebuilding it.</summary>
public sealed class A2UISurfaceUpdate
{
    private readonly List<A2UIComponentBuilder> created = [];
    private readonly A2UICatalog catalog;
    private readonly string surfaceId;
    private readonly List<A2UIMessage> dataChanges = [];

    private A2UIVersion version = A2UIVersion.V0_9_1;

    private A2UISurfaceUpdate(string surfaceId, A2UICatalog catalog)
    {
        this.surfaceId = surfaceId;
        this.catalog = catalog;
        Components = new BasicComponents(new A2UISurfaceBuilder(surfaceId, catalog, Register));
    }

    /// <summary>The component factory, the same one a full surface uses.</summary>
    public BasicComponents Components { get; }

    /// <summary>Starts an update against a live surface.</summary>
    /// <param name="surfaceId">The surface to change.</param>
    /// <param name="catalog">The catalog its components come from.</param>
    /// <returns>The update.</returns>
    public static A2UISurfaceUpdate For(string surfaceId, A2UICatalog catalog) =>
        new(Throw.IfNullOrEmpty(surfaceId, nameof(surfaceId)), Throw.IfNull(catalog, nameof(catalog)));

    /// <summary>Writes a value into the surface's data model.</summary>
    /// <param name="path">A JSON Pointer, such as <c>/rating</c>.</param>
    /// <param name="value">The value. <see langword="null"/> writes JSON null; to delete, use <see cref="RemoveData"/>.</param>
    /// <returns>This update.</returns>
    public A2UISurfaceUpdate SetData(string path, JsonNode? value)
    {
        dataChanges.Add(UpdateDataModelMessage.Set(surfaceId, path, value, version));
        return this;
    }

    /// <summary>Deletes whatever is at a path in the data model.</summary>
    /// <param name="path">A JSON Pointer.</param>
    /// <returns>This update.</returns>
    public A2UISurfaceUpdate RemoveData(string path)
    {
        dataChanges.Add(UpdateDataModelMessage.Remove(surfaceId, path, version));
        return this;
    }

    /// <summary>Replaces the whole data model.</summary>
    /// <param name="value">The new data model.</param>
    /// <returns>This update.</returns>
    public A2UISurfaceUpdate ReplaceData(JsonNode? value)
    {
        dataChanges.Add(UpdateDataModelMessage.Replace(surfaceId, value, version));
        return this;
    }

    /// <summary>Sets the protocol version to emit.</summary>
    /// <param name="value">The version.</param>
    /// <returns>This update.</returns>
    public A2UISurfaceUpdate WithVersion(A2UIVersion value)
    {
        version = value;
        return this;
    }

    /// <summary>Assigns ids, materialises the components, and validates the result.</summary>
    /// <returns>The messages to send.</returns>
    /// <exception cref="InvalidOperationException">The update changes nothing.</exception>
    /// <exception cref="A2UIValidationException">The update would not apply.</exception>
    public IReadOnlyList<A2UIMessage> Build()
    {
        if (created.Count == 0 && dataChanges.Count == 0)
        {
            throw new InvalidOperationException(
                "This update changes nothing. Add a component or a data change before building it.");
        }

        var messages = new List<A2UIMessage>(dataChanges.Count + 1);

        if (created.Count > 0)
        {
            AssignIds();

            var components = new List<A2UIComponent>(created.Count);
            foreach (var builder in created)
            {
                components.Add(builder.ToComponent());
            }

            messages.Add(new UpdateComponentsMessage(surfaceId, components) { Version = version });
        }

        messages.AddRange(dataChanges);

        new A2UIValidator(new A2UIValidationOptions
        {
            Catalog = catalog,
            Profile = A2UIVersionProfile.For(version),

            // The renderer holds the rest of the tree; an update that had to restate it would not be
            // an update.
            AllowDanglingReferences = true,
            AllowOrphanComponents = true,
            AllowMissingRoot = true,
        })
            .Validate(messages)
            .ThrowIfInvalid();

        return messages;
    }

    private void Register(A2UIComponentBuilder component) => created.Add(component);

    private void AssignIds()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var component in created)
        {
            if (component.RequestedId is { } requested && !used.Add(requested))
            {
                throw new InvalidOperationException($"Two components were given the id '{requested}'.");
            }
        }

        foreach (var component in created)
        {
            if (component.RequestedId is { } requested)
            {
                component.Id = requested;
                continue;
            }

            // Random rather than sequential: the renderer already holds ids this process has never
            // seen, and a counter would eventually land on one of them.
            string candidate;
            do
            {
                candidate = A2UIComponentId.New(component.ComponentType);
            }
            while (!used.Add(candidate));

            component.Id = candidate;
        }
    }
}
