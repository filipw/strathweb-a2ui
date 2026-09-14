using System.Globalization;
using System.Text.Json.Nodes;
using Strathweb.A2UI.Components;
using Strathweb.A2UI.Messages;
using Strathweb.A2UI.State;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Rendering;

/// <summary>
/// One surface as a renderer sees it: the components and data the agent sent, plus what a UI needs on
/// top of them: resolved bindings, expanded templates, evaluated checks, and actions ready to send.
/// </summary>
public sealed class A2UIRenderSurface
{
    private readonly A2UISurfaceState state;
    private readonly CultureInfo culture;

    /// <summary>Creates a surface from its first message's identity.</summary>
    /// <param name="surfaceId">The surface's identifier.</param>
    /// <param name="catalogId">The catalog its components come from, when known.</param>
    /// <param name="culture">The culture used when formatting values. Defaults to the invariant culture.</param>
    public A2UIRenderSurface(string surfaceId, string? catalogId = null, CultureInfo? culture = null)
        : this(new A2UISurfaceState(surfaceId, catalogId), culture)
    {
    }

    /// <summary>Wraps surface state that already exists.</summary>
    /// <param name="state">The state to render.</param>
    /// <param name="culture">The culture used when formatting values. Defaults to the invariant culture.</param>
    public A2UIRenderSurface(A2UISurfaceState state, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        this.state = state;
        this.culture = culture ?? CultureInfo.InvariantCulture;
    }

    /// <summary>Raised after any message or local edit changed the surface.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when an action asks the host to open a URL. Only http and https URLs are raised.</summary>
    public event EventHandler<A2UIOpenUrlEventArgs>? OpenUrlRequested;

    /// <summary>The surface's identifier.</summary>
    public string SurfaceId => state.SurfaceId;

    /// <summary>The catalog its components come from.</summary>
    public string? CatalogId => state.CatalogId;

    /// <summary>Whether the agent asked for this surface's data model back on every message.</summary>
    public bool SendDataModel => state.SendDataModel;

    /// <summary>Whether the agent deleted this surface.</summary>
    public bool IsDeleted => state.IsDeleted;

    /// <summary>The components on the surface, keyed by id.</summary>
    public IReadOnlyDictionary<string, A2UIComponent> Components => state.Components;

    /// <summary>The root component, or <see langword="null"/> while the surface is incomplete.</summary>
    public A2UIComponent? Root => state.Root;

    /// <summary>The data model. Read through <see cref="GetData"/>; write through <see cref="TrySetValue"/>.</summary>
    public JsonNode? DataModel => state.DataModel;

    /// <summary>The underlying protocol state.</summary>
    public A2UISurfaceState State => state;

    /// <summary>Applies a message from the agent.</summary>
    /// <param name="message">The message. Must target this surface.</param>
    public void Apply(A2UIMessage message)
    {
        state.Apply(message);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Applies messages from the agent, in order.</summary>
    /// <param name="messages">The messages.</param>
    public void Apply(IEnumerable<A2UIMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        foreach (var message in messages)
        {
            state.Apply(message);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Looks up a component.</summary>
    /// <param name="componentId">The component's id.</param>
    /// <returns>The component, or <see langword="null"/> when the surface has none with that id.</returns>
    public A2UIComponent? GetComponent(string componentId)
    {
        ArgumentNullException.ThrowIfNull(componentId);
        return state.Components.TryGetValue(componentId, out var component) ? component : null;
    }

    /// <summary>Reads a value out of the data model.</summary>
    /// <param name="path">An absolute JSON Pointer.</param>
    /// <returns>A copy of the value, or <see langword="null"/> when nothing is there.</returns>
    public JsonNode? GetData(string path) => state.GetData(path)?.DeepClone();

    /// <summary>A copy of the whole data model, for sending back to the agent.</summary>
    /// <returns>The copy, or <see langword="null"/> when the surface has no data.</returns>
    public JsonNode? DataModelSnapshot() => state.DataModel?.DeepClone();

    /// <summary>Resolves a dynamic value: a literal as is, a path from the data model, a call by running it.</summary>
    /// <param name="value">The value.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <returns>Concrete JSON, or <see langword="null"/>.</returns>
    public JsonNode? Resolve(DynamicValue? value, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return value?.Kind switch
        {
            null => null,
            DynamicValueKind.Literal => value.Literal,
            DynamicValueKind.Path => GetData(scope.Absolute(value.Path!)),
            DynamicValueKind.FunctionCall => A2UIFunctions.Invoke(value.Call!, v => Resolve(v, scope), culture),
            _ => null,
        };
    }

    /// <summary>Resolves a component property.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <returns>Concrete JSON, or <see langword="null"/> when the property is absent or resolves to nothing.</returns>
    public JsonNode? ResolveProperty(A2UIComponent component, string property, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);

        return component.Properties.TryGetPropertyValue(property, out var raw) && raw is not null
            ? Resolve(DynamicValue.FromJson(raw), scope)
            : null;
    }

    /// <summary>Resolves a property as text.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <param name="fallback">What to return when the property is absent or null.</param>
    /// <returns>The text.</returns>
    public string ResolveString(A2UIComponent component, string property, A2UIDataScope scope, string fallback = "")
    {
        var value = ResolveProperty(component, property, scope);
        return value is null ? fallback : A2UIFunctions.Display(value, culture);
    }

    /// <summary>Resolves a property as a boolean. Anything but <c>true</c> is <see langword="false"/>.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <returns>The boolean.</returns>
    public bool ResolveBoolean(A2UIComponent component, string property, A2UIDataScope scope) =>
        ResolveProperty(component, property, scope) is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    /// <summary>Resolves a property as a number.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <returns>The number, or <see langword="null"/> when the property is absent or not numeric.</returns>
    public double? ResolveNumber(A2UIComponent component, string property, A2UIDataScope scope)
    {
        var value = ResolveProperty(component, property, scope);
        if (A2UIFunctions.TryGetNumber(value, out var number))
        {
            return number;
        }

        return value is JsonValue scalar && scalar.TryGetValue<string>(out var text) &&
               double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>Resolves a property as a list of strings, as a <c>ChoicePicker</c> value is.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <param name="scope">The scope relative paths resolve in.</param>
    /// <returns>The strings; a single string becomes a one-element list.</returns>
    public IReadOnlyList<string> ResolveStrings(A2UIComponent component, string property, A2UIDataScope scope) =>
        ResolveProperty(component, property, scope) switch
        {
            JsonArray array => array.Select(item => A2UIFunctions.Display(item, culture)).ToList(),
            JsonValue scalar when scalar.TryGetValue<string>(out var text) && text.Length > 0 => [text],
            _ => [],
        };

    /// <summary>The id a single-child property such as <c>child</c>, <c>trigger</c> or <c>content</c> points at.</summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name.</param>
    /// <returns>The child id, or <see langword="null"/>.</returns>
    public static string? ChildId(A2UIComponent component, string property)
    {
        ArgumentNullException.ThrowIfNull(component);
        return component.Properties[property] is JsonValue value && value.TryGetValue<string>(out var id) ? id : null;
    }

    /// <summary>
    /// The children a list property draws: a fixed list of ids, or a template repeated once per item
    /// of a data model list, each item getting its own scope.
    /// </summary>
    /// <param name="component">The component.</param>
    /// <param name="property">The property name, usually <c>children</c>.</param>
    /// <param name="scope">The scope the template's path resolves in.</param>
    /// <returns>The children to draw, in order.</returns>
    public IReadOnlyList<A2UIChildBinding> Children(A2UIComponent component, string property, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(scope);

        if (component.Properties[property] is not { } raw)
        {
            return [];
        }

        ChildList list;
        try
        {
            list = ChildList.FromJson(raw);
        }
        catch (A2UIValueException)
        {
            return [];
        }

        if (!list.IsTemplate)
        {
            return list.ComponentIds!.Select(id => new A2UIChildBinding(id, scope, id)).ToList();
        }

        var listPointer = scope.Absolute(list.TemplatePath!);
        if (state.GetData(listPointer) is not JsonArray items)
        {
            return [];
        }

        var children = new List<A2UIChildBinding>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var itemPointer = listPointer + "/" + i.ToString(CultureInfo.InvariantCulture);
            children.Add(new A2UIChildBinding(list.TemplateComponentId!, A2UIDataScope.ForItem(itemPointer), itemPointer));
        }

        return children;
    }

    /// <summary>
    /// Writes a value the user entered back to the data model, through the binding a property holds.
    /// A property that is a literal rather than a binding cannot be written to.
    /// </summary>
    /// <param name="component">The input component.</param>
    /// <param name="property">The bound property, usually <c>value</c>.</param>
    /// <param name="value">The new value.</param>
    /// <param name="scope">The scope the binding resolves in.</param>
    /// <returns><see langword="true"/> when the data model changed.</returns>
    public bool TrySetValue(A2UIComponent component, string property, JsonNode? value, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(scope);

        if (component.Properties[property] is not { } raw || DynamicValue.FromJson(raw) is not { Kind: DynamicValueKind.Path } binding)
        {
            return false;
        }

        // A local edit is a data update like any other, so the same code path applies it.
        state.Apply(UpdateDataModelMessage.Set(SurfaceId, scope.Absolute(binding.Path!), value));
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Evaluates a component's <c>checks</c>.</summary>
    /// <param name="component">The component.</param>
    /// <param name="scope">The scope its bindings resolve in.</param>
    /// <returns>The message of every check that failed. Empty when all pass or there are none.</returns>
    public IReadOnlyList<string> Check(A2UIComponent component, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(scope);

        if (component.Properties["checks"] is not JsonArray checks)
        {
            return [];
        }

        var failures = new List<string>();
        foreach (var item in checks)
        {
            CheckRule rule;
            try
            {
                rule = CheckRule.FromJson(item);
            }
            catch (A2UIValueException)
            {
                continue;
            }

            var passed = Resolve(rule.Condition, scope) is JsonValue scalar && scalar.TryGetValue<bool>(out var ok) && ok;
            if (!passed)
            {
                failures.Add(rule.Message);
            }
        }

        return failures;
    }

    /// <summary>
    /// Evaluates the checks of every component reachable from the root, with template children in
    /// their item scopes. The renderer runs this before dispatching an action.
    /// </summary>
    /// <returns>Failures keyed by child key: the component id, or the item path for a template child.</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> CheckAll()
    {
        var failures = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (Root is null)
        {
            return failures;
        }

        var stack = new Stack<A2UIChildBinding>();
        stack.Push(new A2UIChildBinding(Root.Id, A2UIDataScope.Root, Root.Id));
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (stack.Count > 0)
        {
            var binding = stack.Pop();
            if (!seen.Add(binding.Key) || GetComponent(binding.ComponentId) is not { } component)
            {
                continue;
            }

            var result = Check(component, binding.Scope);
            if (result.Count > 0)
            {
                failures[binding.Key] = result;
            }

            foreach (var child in AllChildren(component, binding.Scope))
            {
                stack.Push(child);
            }
        }

        return failures;
    }

    /// <summary>
    /// Builds the action a component dispatches. An event becomes an <see cref="ActionMessage"/> with
    /// its context resolved; a function call runs locally and returns <see langword="null"/>.
    /// </summary>
    /// <param name="component">The component the user interacted with.</param>
    /// <param name="scope">The scope its bindings resolve in.</param>
    /// <returns>The message to send to the agent, or <see langword="null"/> when nothing goes to the agent.</returns>
    public ActionMessage? CreateAction(A2UIComponent component, A2UIDataScope scope)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(scope);

        if (component.Properties["action"] is not { } raw)
        {
            return null;
        }

        A2UIAction action;
        try
        {
            action = A2UIAction.FromJson(raw);
        }
        catch (A2UIValueException)
        {
            return null;
        }

        if (action.FunctionCall is { } call)
        {
            RunLocally(call, scope);
            return null;
        }

        var context = new JsonObject();
        foreach (var pair in action.Event!.Context ?? new Dictionary<string, DynamicValue>(StringComparer.Ordinal))
        {
            context[pair.Key] = Resolve(pair.Value, scope)?.DeepClone();
        }

        return new ActionMessage(action.Event.Name, SurfaceId, component.Id, DateTimeOffset.UtcNow, context);
    }

    private void RunLocally(FunctionCall call, A2UIDataScope scope)
    {
        var result = A2UIFunctions.Invoke(call, v => Resolve(v, scope), culture);

        if (string.Equals(call.Call, A2UIFunctions.OpenUrlFunction, StringComparison.Ordinal) &&
            result is JsonValue value && value.TryGetValue<string>(out var url) &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            OpenUrlRequested?.Invoke(this, new A2UIOpenUrlEventArgs(uri));
        }
    }

    /// <summary>Every child a component draws, from its single-child and list properties.</summary>
    private IEnumerable<A2UIChildBinding> AllChildren(A2UIComponent component, A2UIDataScope scope)
    {
        foreach (var property in component.Properties)
        {
            switch (property.Value)
            {
                case JsonValue value when value.TryGetValue<string>(out var id) && IsChildProperty(property.Key) && Components.ContainsKey(id):
                    yield return new A2UIChildBinding(id, scope, id);
                    break;

                case JsonArray when property.Key == "children":
                case JsonObject when property.Key == "children":
                    foreach (var child in Children(component, property.Key, scope))
                    {
                        yield return child;
                    }

                    break;

                case JsonArray tabs when property.Key == "tabs":
                    foreach (var tab in tabs.OfType<JsonObject>())
                    {
                        if (tab["child"] is JsonValue tabChild && tabChild.TryGetValue<string>(out var tabId))
                        {
                            yield return new A2UIChildBinding(tabId, scope, tabId);
                        }
                    }

                    break;
            }
        }
    }

    private static bool IsChildProperty(string name) => name is "child" or "trigger" or "content";
}
