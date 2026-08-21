using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>
/// Creates the Basic Catalog's components. Every component in a surface comes from here, which is
/// how ids get assigned without anyone writing one.
/// </summary>
public sealed class BasicComponents
{
    private readonly A2UISurfaceBuilder surface;

    internal BasicComponents(A2UISurfaceBuilder surface)
    {
        this.surface = surface;
    }

    /// <summary>Creates a text component.</summary>
    /// <param name="text">The text, a binding, or a formatting call.</param>
    /// <returns>The builder.</returns>
    public TextBuilder Text(DynamicValue text) =>
        new TextBuilder(surface).Text(Throw.IfNull(text, nameof(text)));

    /// <summary>Creates a text component with literal text.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The builder.</returns>
    public TextBuilder Text(string text) => Text(DynamicValue.FromString(Throw.IfNull(text, nameof(text))));

    /// <summary>Creates an image.</summary>
    /// <param name="url">The image URL, or a binding to one.</param>
    /// <returns>The builder.</returns>
    public ImageBuilder Image(DynamicValue url) => new ImageBuilder(surface).Url(Throw.IfNull(url, nameof(url)));

    /// <summary>Creates an image.</summary>
    /// <param name="url">The image URL.</param>
    /// <returns>The builder.</returns>
    public ImageBuilder Image(string url) => Image(DynamicValue.FromString(Throw.IfNull(url, nameof(url))));

    /// <summary>Creates an icon.</summary>
    /// <param name="name">A name from <see cref="A2UIIcons"/>.</param>
    /// <returns>The builder.</returns>
    public IconBuilder Icon(string name) => new IconBuilder(surface).Name(name);

    /// <summary>Creates a video.</summary>
    /// <param name="url">The video URL, or a binding to one.</param>
    /// <returns>The builder.</returns>
    public VideoBuilder Video(DynamicValue url) => new VideoBuilder(surface).Url(Throw.IfNull(url, nameof(url)));

    /// <summary>Creates an audio player.</summary>
    /// <param name="url">The audio URL, or a binding to one.</param>
    /// <returns>The builder.</returns>
    public AudioPlayerBuilder AudioPlayer(DynamicValue url) =>
        new AudioPlayerBuilder(surface).Url(Throw.IfNull(url, nameof(url)));

    /// <summary>Creates a horizontal layout.</summary>
    /// <param name="children">The children, in order.</param>
    /// <returns>The builder.</returns>
    public RowBuilder Row(params A2UIComponentBuilder[] children) => new RowBuilder(surface).Children(children);

    /// <summary>Creates a vertical layout.</summary>
    /// <param name="children">The children, in order.</param>
    /// <returns>The builder.</returns>
    public ColumnBuilder Column(params A2UIComponentBuilder[] children) =>
        new ColumnBuilder(surface).Children(children);

    /// <summary>Creates a list.</summary>
    /// <param name="children">The children, in order.</param>
    /// <returns>The builder.</returns>
    public ListBuilder List(params A2UIComponentBuilder[] children) => new ListBuilder(surface).Children(children);

    /// <summary>Creates a card around a single child.</summary>
    /// <param name="child">The component inside the card.</param>
    /// <returns>The builder.</returns>
    public CardBuilder Card(A2UIComponentBuilder child) => new CardBuilder(surface).Child(child);

    /// <summary>Creates a tab set. Add tabs with <see cref="TabsBuilder.Tab(string, A2UIComponentBuilder)"/>.</summary>
    /// <returns>The builder.</returns>
    public TabsBuilder Tabs() => new(surface);

    /// <summary>Creates a modal.</summary>
    /// <param name="trigger">The component that opens it.</param>
    /// <param name="content">What it shows.</param>
    /// <returns>The builder.</returns>
    public ModalBuilder Modal(A2UIComponentBuilder trigger, A2UIComponentBuilder content) =>
        new ModalBuilder(surface).Trigger(trigger).Content(content);

    /// <summary>Creates a dividing line.</summary>
    /// <returns>The builder.</returns>
    public DividerBuilder Divider() => new(surface);

    /// <summary>Creates a button.</summary>
    /// <param name="child">The label component, usually a <see cref="Text(string)"/>.</param>
    /// <returns>The builder.</returns>
    public ButtonBuilder Button(A2UIComponentBuilder child) => new ButtonBuilder(surface).Child(child);

    /// <summary>Creates a button with a literal label.</summary>
    /// <param name="label">The button's text.</param>
    /// <returns>The builder.</returns>
    public ButtonBuilder Button(string label) => Button(Text(label));

    /// <summary>Creates a text input.</summary>
    /// <param name="label">The field's label, which the catalog requires.</param>
    /// <returns>The builder.</returns>
    public TextFieldBuilder TextField(DynamicValue label) =>
        new TextFieldBuilder(surface).Label(Throw.IfNull(label, nameof(label)));

    /// <summary>Creates a text input.</summary>
    /// <param name="label">The field's label.</param>
    /// <returns>The builder.</returns>
    public TextFieldBuilder TextField(string label) =>
        TextField(DynamicValue.FromString(Throw.IfNull(label, nameof(label))));

    /// <summary>Creates a checkbox.</summary>
    /// <param name="label">The label.</param>
    /// <param name="value">Where the checkbox's state lives, usually <see cref="Bind.Path"/>.</param>
    /// <returns>The builder.</returns>
    public CheckBoxBuilder CheckBox(string label, DynamicValue value) =>
        new CheckBoxBuilder(surface)
            .Label(DynamicValue.FromString(Throw.IfNull(label, nameof(label))))
            .Value(value);

    /// <summary>Creates a choice picker.</summary>
    /// <returns>The builder.</returns>
    public ChoicePickerBuilder ChoicePicker() => new(surface);

    /// <summary>Creates a slider.</summary>
    /// <param name="value">Where the slider's value lives, usually <see cref="Bind.Path"/>.</param>
    /// <param name="max">The highest selectable value, which the catalog requires.</param>
    /// <returns>The builder.</returns>
    public SliderBuilder Slider(DynamicValue value, double max) =>
        new SliderBuilder(surface).Value(value).Max(max);

    /// <summary>Creates a date and/or time picker.</summary>
    /// <param name="value">Where the picked value lives, usually <see cref="Bind.Path"/>.</param>
    /// <returns>The builder.</returns>
    public DateTimeInputBuilder DateTimeInput(DynamicValue value) => new DateTimeInputBuilder(surface).Value(value);
}
