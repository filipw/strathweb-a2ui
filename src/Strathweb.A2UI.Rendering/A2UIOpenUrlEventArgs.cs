namespace Strathweb.A2UI.Rendering;

/// <summary>A request from a surface to open a URL, raised by the <c>openUrl</c> catalog function.</summary>
public sealed class A2UIOpenUrlEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="url">The URL to open. Always http or https.</param>
    public A2UIOpenUrlEventArgs(Uri url)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
    }

    /// <summary>The URL to open.</summary>
    public Uri Url { get; }
}
