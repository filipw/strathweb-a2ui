using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>An image.</summary>
public sealed class ImageBuilder : A2UIComponentBuilder<ImageBuilder>
{
    internal ImageBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Image")
    {
    }

    /// <summary>Sets the image URL.</summary>
    /// <param name="url">The URL, or a binding to one.</param>
    /// <returns>This builder.</returns>
    public ImageBuilder Url(DynamicValue url) => Set("url", url);

    /// <summary>Sets the alternative text.</summary>
    /// <param name="description">What the image shows.</param>
    /// <returns>This builder.</returns>
    public ImageBuilder Description(DynamicValue description) => Set("description", description);

    /// <summary>Sets how the image fills its space.</summary>
    /// <param name="fit">The fit.</param>
    /// <returns>This builder.</returns>
    public ImageBuilder Fit(ImageFit fit) => Set("fit", fit.ToWireString());

    /// <summary>Sets the role the image plays, which decides how large it is drawn.</summary>
    /// <param name="variant">The role.</param>
    /// <returns>This builder.</returns>
    public ImageBuilder Variant(ImageVariant variant) => Set("variant", variant.ToWireString());
}
