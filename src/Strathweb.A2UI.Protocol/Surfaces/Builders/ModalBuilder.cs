namespace Strathweb.A2UI.Surfaces;

/// <summary>A dialog, opened by one component and containing another.</summary>
public sealed class ModalBuilder : A2UIComponentBuilder<ModalBuilder>
{
    internal ModalBuilder(A2UISurfaceBuilder surface)
        : base(surface, "Modal")
    {
    }

    /// <summary>Sets the component that opens the modal.</summary>
    /// <param name="trigger">Usually a button.</param>
    /// <returns>This builder.</returns>
    public ModalBuilder Trigger(A2UIComponentBuilder trigger) => Reference("trigger", trigger);

    /// <summary>Sets what the modal shows.</summary>
    /// <param name="content">The component inside the modal.</param>
    /// <returns>This builder.</returns>
    public ModalBuilder Content(A2UIComponentBuilder content) => Reference("content", content);
}
