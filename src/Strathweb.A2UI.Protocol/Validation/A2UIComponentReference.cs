namespace Strathweb.A2UI.Validation;

/// <summary>One reference from a component to another, and the property it was found in.</summary>
public readonly struct A2UIComponentReference : IEquatable<A2UIComponentReference>
{
    internal A2UIComponentReference(string componentId, string propertyPath)
    {
        ComponentId = componentId;
        PropertyPath = propertyPath;
    }

    /// <summary>The id being referenced.</summary>
    public string ComponentId { get; }

    /// <summary>
    /// Where the reference sits, for example <c>child</c>, <c>children</c>, <c>tabs[1].child</c> or
    /// <c>children.componentId</c>.
    /// </summary>
    public string PropertyPath { get; }

    /// <inheritdoc />
    public bool Equals(A2UIComponentReference other) =>
        string.Equals(ComponentId, other.ComponentId, StringComparison.Ordinal) &&
        string.Equals(PropertyPath, other.PropertyPath, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is A2UIComponentReference other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(ComponentId) * 397 ^
        StringComparer.Ordinal.GetHashCode(PropertyPath);

    /// <summary>Compares two references.</summary>
    public static bool operator ==(A2UIComponentReference left, A2UIComponentReference right) => left.Equals(right);

    /// <summary>Compares two references.</summary>
    public static bool operator !=(A2UIComponentReference left, A2UIComponentReference right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"{PropertyPath} -> {ComponentId}";
}
