using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>An embedded video.</summary>
public sealed class VideoBuilder : A2UIComponentBuilder<VideoBuilder>
{
    internal VideoBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Video")
    {
    }

    /// <summary>Sets the video URL.</summary>
    /// <param name="url">The URL, or a binding to one.</param>
    /// <returns>This builder.</returns>
    public VideoBuilder Url(DynamicValue url) => Set("url", url);
}
