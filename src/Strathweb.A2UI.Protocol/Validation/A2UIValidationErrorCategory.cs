namespace Strathweb.A2UI.Validation;

/// <summary>The kind of problem an <see cref="A2UIValidationError"/> reports.</summary>
public enum A2UIValidationErrorCategory
{
    /// <summary>The payload does not match the message schema: a missing, extra or mistyped field.</summary>
    Validation = 1,

    /// <summary>The component graph does not hang together: duplicate ids, no root, a dangling reference, an orphan.</summary>
    Integrity = 2,

    /// <summary>Something is nested too deeply, or refers to itself directly or through a cycle.</summary>
    Recursion = 3,

    /// <summary>The payload names something the catalog does not define.</summary>
    Catalog = 4,
}
