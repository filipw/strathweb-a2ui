using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;

namespace Strathweb.A2UI.Surfaces;

/// <summary>A named icon from the catalog's set.</summary>
public sealed class IconBuilder : A2UIComponentBuilder<IconBuilder>
{
    internal IconBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Icon")
    {
    }

    /// <summary>Sets which icon to draw.</summary>
    /// <param name="name">A name from <see cref="A2UIIcons"/>.</param>
    /// <returns>This builder.</returns>
    public IconBuilder Name(string name) => Set("name", JsonValue.Create(Throw.IfNullOrEmpty(name, nameof(name))));
}
