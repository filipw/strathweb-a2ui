using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Catalogs;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.Validation;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Assembles a surface from typed component builders.</summary>
public sealed class A2UISurfaceBuilder
{
    private readonly List<A2UIComponentBuilder> created = [];
    private readonly A2UICatalog catalog;
    private readonly string surfaceId;

    private A2UIComponentBuilder? root;
    private JsonNode? dataModel;
    private JsonObject? theme;
    private bool sendDataModel;
    private A2UIVersion version = A2UIVersion.V0_9_1;

    private readonly Action<A2UIComponentBuilder>? register;

    internal A2UISurfaceBuilder(string surfaceId, A2UICatalog catalog)
    {
        this.surfaceId = surfaceId;
        this.catalog = catalog;
        Components = new BasicComponents(this);
    }

    /// <summary>
    /// Used by <see cref="A2UISurfaceUpdate"/>, which needs the component factory without the rest of
    /// a surface.
    /// </summary>
    internal A2UISurfaceBuilder(string surfaceId, A2UICatalog catalog, Action<A2UIComponentBuilder> register)
        : this(surfaceId, catalog)
    {
        this.register = register;
    }

    /// <summary>The component factory. Every component you create comes from here.</summary>
    public BasicComponents Components { get; }

    /// <summary>Marks a component as the surface's root.</summary>
    /// <param name="component">The component to render at the top level.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder Root(A2UIComponentBuilder component)
    {
        root = Throw.IfNull(component, nameof(component));
        return this;
    }

    /// <summary>Sets the surface's initial data model.</summary>
    /// <param name="value">The data. Bindings in the surface read from here.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder WithData(JsonNode? value)
    {
        dataModel = value;
        return this;
    }

    /// <summary>Builds the surface's initial data model.</summary>
    /// <param name="configure">Fills in the data.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder WithData(Action<JsonObject> configure)
    {
        Throw.IfNull(configure, nameof(configure));

        var data = dataModel as JsonObject ?? [];
        configure(data);
        dataModel = data;
        return this;
    }

    /// <summary>Sets theme parameters, which the catalog's theme schema constrains.</summary>
    /// <param name="value">The theme.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder WithTheme(JsonObject value)
    {
        theme = Throw.IfNull(value, nameof(value));
        return this;
    }

    /// <summary>Asks the renderer to send this surface's whole data model back with every message.</summary>
    /// <param name="value">Whether to ask.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder SendDataModel(bool value = true)
    {
        sendDataModel = value;
        return this;
    }

    /// <summary>Sets the protocol version to emit.</summary>
    /// <param name="value">The version.</param>
    /// <returns>This builder.</returns>
    public A2UISurfaceBuilder WithVersion(A2UIVersion value)
    {
        version = value;
        return this;
    }

    /// <summary>Assigns ids, materialises the components, and validates the result.</summary>
    /// <returns>The finished surface.</returns>
    /// <exception cref="InvalidOperationException">No root component was set.</exception>
    /// <exception cref="A2UIValidationException">The surface would not render.</exception>
    public A2UISurface Build()
    {
        if (root is null)
        {
            throw new InvalidOperationException(
                "A surface needs a root component. Call Root(...) with the component to render at the top.");
        }

        AssignIds();

        var components = new List<A2UIComponent>(created.Count);
        foreach (var builder in created)
        {
            components.Add(builder.ToComponent());
        }

        var messages = new List<A2UIMessage>(3)
        {
            new CreateSurfaceMessage(surfaceId, catalog.CatalogId)
            {
                Version = version,
                Theme = theme,
                SendDataModel = sendDataModel ? true : null,
            },
            new UpdateComponentsMessage(surfaceId, components) { Version = version },
        };

        if (dataModel is not null)
        {
            messages.Add(UpdateDataModelMessage.Replace(surfaceId, dataModel, version));
        }

        new A2UIValidator(new A2UIValidationOptions { Catalog = catalog, Profile = A2UIVersionProfile.For(version) })
            .Validate(messages)
            .ThrowIfInvalid();

        return new A2UISurface(surfaceId, catalog.CatalogId, messages);
    }

    internal void Register(A2UIComponentBuilder component)
    {
        if (register is not null)
        {
            register(component);
            return;
        }

        created.Add(component);
    }

    /// <summary>
    /// Gives the root the id the specification reserves for it, and everything else a generated one.
    /// Ids are derived from component type and creation order, so a surface built twice is identical.
    /// </summary>
    private void AssignIds()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var component in created)
        {
            if (component.RequestedId is { } requested && !used.Add(requested))
            {
                throw new InvalidOperationException($"Two components were given the id '{requested}'.");
            }
        }

        root!.Id = "root";
        if (root.RequestedId is { } rootRequested && rootRequested != "root")
        {
            throw new InvalidOperationException(
                $"The root component was given the id '{rootRequested}', but the root must be called 'root'.");
        }

        used.Add("root");

        foreach (var component in created)
        {
            if (ReferenceEquals(component, root))
            {
                continue;
            }

            if (component.RequestedId is { } requested)
            {
                component.Id = requested;
                continue;
            }

            var prefix = Camel(component.ComponentType);
            string candidate;
            do
            {
                counters.TryGetValue(prefix, out var next);
                counters[prefix] = next + 1;
                candidate = prefix + "_" + (next + 1).ToString(CultureInfo.InvariantCulture);
            }
            while (!used.Add(candidate));

            component.Id = candidate;
        }
    }

    private static string Camel(string componentType) =>
        componentType.Length == 0
            ? componentType
            : char.ToLowerInvariant(componentType[0]) + componentType.Substring(1);
}
