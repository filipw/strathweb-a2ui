using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Base for the input components the Basic Catalog lets the renderer validate on the client.</summary>
/// <typeparam name="TSelf">The concrete builder type.</typeparam>
public abstract class A2UICheckableComponentBuilder<TSelf> : A2UIComponentBuilder<TSelf>
    where TSelf : A2UICheckableComponentBuilder<TSelf>
{
    private protected A2UICheckableComponentBuilder(A2UISurfaceBuilder surface, string componentType)
        : base(surface, componentType)
    {
    }

    /// <summary>
    /// Adds client-side validation rules. The renderer evaluates them before dispatching an action, so
    /// the user is told what is wrong without a round trip to the agent.
    /// </summary>
    /// <param name="rules">The rules, built with <see cref="Check"/>.</param>
    /// <returns>This builder.</returns>
    public TSelf Checks(params CheckRule[] rules)
    {
        Throw.IfNull(rules, nameof(rules));

        var array = new JsonArray();
        foreach (var rule in rules)
        {
            array.Add((JsonNode)Throw.IfNull(rule, nameof(rules)).ToJson());
        }

        return Set("checks", array);
    }
}
