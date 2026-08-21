using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>Builds what a component does when the user interacts with it.</summary>
public static class Act
{
    /// <summary>Dispatches an event to the agent.</summary>
    /// <param name="name">The action name the agent will receive.</param>
    /// <returns>The action.</returns>
    public static A2UIAction Event(string name) => A2UIAction.FromEvent(new A2UIEvent(name));

    /// <summary>Dispatches an event to the agent, with context.</summary>
    /// <param name="name">The action name.</param>
    /// <param name="context">
    /// The values sent with the event. Bind the ones the user can change; leave the rest literal.
    /// </param>
    /// <returns>The action.</returns>
    public static A2UIAction Event(string name, params (string Key, DynamicValue Value)[] context)
    {
        Throw.IfNull(context, nameof(context));

        var values = new Dictionary<string, DynamicValue>(context.Length, StringComparer.Ordinal);
        foreach (var entry in context)
        {
            values[Throw.IfNullOrEmpty(entry.Key, nameof(context))] = entry.Value;
        }

        return A2UIAction.FromEvent(new A2UIEvent(name) { Context = values });
    }

    /// <summary>Runs a catalog function in the renderer, without involving the agent.</summary>
    /// <param name="function">The function name.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The action.</returns>
    public static A2UIAction Call(string function, params (string Key, DynamicValue Value)[] args)
    {
        Throw.IfNull(args, nameof(args));

        var values = new Dictionary<string, DynamicValue>(args.Length, StringComparer.Ordinal);
        foreach (var entry in args)
        {
            values[Throw.IfNullOrEmpty(entry.Key, nameof(args))] = entry.Value;
        }

        return A2UIAction.FromFunctionCall(new FunctionCall(function) { Args = values });
    }

    /// <summary>Opens a URL in the renderer.</summary>
    /// <param name="url">The URL, or a binding to one.</param>
    /// <returns>The action.</returns>
    public static A2UIAction OpenUrl(DynamicValue url) => Call("openUrl", ("url", url));
}
