namespace Strathweb.A2UI.Surfaces;

/// <summary>A dividing line.</summary>
public sealed class DividerBuilder : A2UIComponentBuilder<DividerBuilder>
{
    internal DividerBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Divider")
    {
    }

    /// <summary>Sets which way the divider runs.</summary>
    /// <param name="axis">The axis.</param>
    /// <returns>This builder.</returns>
    public DividerBuilder Axis(DividerAxis axis) => Set("axis", axis.ToWireString());
}
