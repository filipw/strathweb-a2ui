using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>An audio player.</summary>
public sealed class AudioPlayerBuilder : A2UIComponentBuilder<AudioPlayerBuilder>
{
    internal AudioPlayerBuilder(A2UISurfaceBuilder surface)
        : base(surface, "AudioPlayer")
    {
    }

    /// <summary>Sets the audio URL.</summary>
    /// <param name="url">The URL, or a binding to one.</param>
    /// <returns>This builder.</returns>
    public AudioPlayerBuilder Url(DynamicValue url) => Set("url", url);

    /// <summary>Describes the audio for assistive technologies.</summary>
    /// <param name="description">What the audio is.</param>
    /// <returns>This builder.</returns>
    public AudioPlayerBuilder Description(DynamicValue description) => Set("description", description);
}
