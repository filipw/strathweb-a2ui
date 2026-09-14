namespace Strathweb.A2UI.Rendering;

/// <summary>One child to draw: which component, and the data scope it reads from.</summary>
public sealed class A2UIChildBinding
{
    internal A2UIChildBinding(string componentId, A2UIDataScope scope, string key)
    {
        ComponentId = componentId;
        Scope = scope;
        Key = key;
    }

    /// <summary>The component to draw.</summary>
    public string ComponentId { get; }

    /// <summary>The scope its bindings resolve in. The item scope for a template child.</summary>
    public A2UIDataScope Scope { get; }

    /// <summary>A key stable across re-renders: the component id, or the item path for a template child.</summary>
    public string Key { get; }
}
