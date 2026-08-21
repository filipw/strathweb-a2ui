using System.Text.Json.Nodes;
using Strathweb.A2UI.Internal;
using Strathweb.A2UI.Values;

namespace Strathweb.A2UI.Surfaces;

/// <summary>
/// One option in a <see cref="ChoicePickerBuilder"/>: what the user sees, and the value that reaches
/// the agent.
/// </summary>
public readonly struct A2UIChoice : IEquatable<A2UIChoice>
{
    /// <summary>Creates a choice.</summary>
    /// <param name="label">What the user sees.</param>
    /// <param name="value">The stable value sent back when the choice is selected.</param>
    public A2UIChoice(DynamicValue label, string value)
    {
        Label = Throw.IfNull(label, nameof(label));
        Value = Throw.IfNull(value, nameof(value));
    }

    /// <summary>What the user sees.</summary>
    public DynamicValue Label { get; }

    /// <summary>The value sent back when this choice is selected.</summary>
    public string Value { get; }

    /// <summary>Creates a choice from a literal label.</summary>
    /// <param name="label">What the user sees.</param>
    /// <param name="value">The value sent back.</param>
    /// <returns>The choice.</returns>
    public static A2UIChoice Of(string label, string value) =>
        new(DynamicValue.FromString(Throw.IfNull(label, nameof(label))), value);

    /// <summary>Lets a <c>(label, value)</c> tuple stand in for a choice.</summary>
    /// <param name="choice">The tuple.</param>
    public static implicit operator A2UIChoice((string Label, string Value) choice) =>
        Of(choice.Label, choice.Value);

    /// <summary>Writes the choice as its wire JSON object.</summary>
    /// <returns>A new object.</returns>
    public JsonObject ToJson() => new()
    {
        ["label"] = Label.ToJson(),
        ["value"] = Value,
    };

    /// <inheritdoc />
    public bool Equals(A2UIChoice other) =>
        ReferenceEquals(Label, other.Label) && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is A2UIChoice other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <summary>Compares two choices.</summary>
    public static bool operator ==(A2UIChoice left, A2UIChoice right) => left.Equals(right);

    /// <summary>Compares two choices.</summary>
    public static bool operator !=(A2UIChoice left, A2UIChoice right) => !left.Equals(right);
}
