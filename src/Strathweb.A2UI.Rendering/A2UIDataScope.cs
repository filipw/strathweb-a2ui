namespace Strathweb.A2UI.Rendering;

/// <summary>
/// Where relative binding paths resolve from. At surface level every path is absolute; inside a
/// template each repeated child sees one list item, and a path without a leading slash is relative to
/// that item.
/// </summary>
public sealed class A2UIDataScope
{
    private A2UIDataScope(string? itemPath)
    {
        ItemPath = itemPath;
    }

    /// <summary>The scope for a surface's own components.</summary>
    public static A2UIDataScope Root { get; } = new(null);

    /// <summary>
    /// The absolute JSON Pointer of the list item this scope is bound to, or <see langword="null"/> at
    /// surface level.
    /// </summary>
    public string? ItemPath { get; }

    /// <summary>Whether this scope is inside a template.</summary>
    public bool IsItem => ItemPath is not null;

    /// <summary>Creates the scope for one item of a template.</summary>
    /// <param name="itemPath">The absolute pointer of the item, such as <c>/drinks/2</c>.</param>
    /// <returns>The scope.</returns>
    public static A2UIDataScope ForItem(string itemPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(itemPath);
        return new A2UIDataScope(itemPath);
    }

    /// <summary>Turns a binding path into an absolute JSON Pointer.</summary>
    /// <param name="path">A pointer such as <c>/rating</c>, or a relative path such as <c>name</c>.</param>
    /// <returns>The absolute pointer.</returns>
    public string Absolute(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Length == 0)
        {
            return ItemPath ?? string.Empty;
        }

        if (path[0] == '/')
        {
            return path;
        }

        return ItemPath is null ? "/" + path : ItemPath + "/" + path;
    }
}
