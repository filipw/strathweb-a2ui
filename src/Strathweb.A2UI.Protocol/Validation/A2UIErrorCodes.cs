namespace Strathweb.A2UI.Validation;

/// <summary>Codes reported by this library's validation.</summary>
public static class A2UIErrorCodes
{
    /// <summary>A property the schema requires is absent.</summary>
    public const string MissingField = "missing_field";

    /// <summary>A property is present but its value is not one the schema permits.</summary>
    public const string InvalidValue = "invalid_value";

    /// <summary>A property is present but of the wrong JSON type.</summary>
    public const string TypeMismatch = "type_mismatch";

    /// <summary>A property the schema forbids is present.</summary>
    public const string ExtraField = "extra_field";

    /// <summary>A data binding path is not a well-formed JSON Pointer.</summary>
    public const string InvalidPointer = "invalid_pointer";

    /// <summary>Two components in the same surface share an id.</summary>
    public const string DuplicateId = "duplicate_id";

    /// <summary>A component has no id, or an empty one.</summary>
    public const string MissingId = "missing_id";

    /// <summary>No component in a newly created surface has the root id.</summary>
    public const string MissingRoot = "missing_root";

    /// <summary>A component references an id that no component defines.</summary>
    public const string UnresolvedReference = "unresolved_reference";

    /// <summary>A component references itself.</summary>
    public const string SelfReference = "self_reference";

    /// <summary>The child-reference graph contains a cycle.</summary>
    public const string CircularReference = "circular_reference";

    /// <summary>A component exists but nothing reaches it from the root.</summary>
    public const string OrphanComponent = "orphan_component";

    /// <summary>Nesting exceeded the depth limit.</summary>
    public const string RecursionLimit = "recursion_limit";

    /// <summary>Function calls were nested past their own, tighter, depth limit.</summary>
    public const string FunctionCallRecursionLimit = "function_call_recursion_limit";

    /// <summary>The payload names a component the catalog does not define.</summary>
    public const string UnknownComponent = "unknown_component";

    /// <summary>The payload sets a property the catalog does not define for that component.</summary>
    public const string UnknownProperty = "unknown_property";
}
