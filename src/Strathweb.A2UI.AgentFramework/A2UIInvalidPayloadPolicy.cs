namespace Strathweb.A2UI.AgentFramework;

/// <summary>What happens to an A2UI payload the model wrote that is still invalid after every repair.</summary>
public enum A2UIInvalidPayloadPolicy
{
    /// <summary>Drop the payload and log a warning. The model's prose still reaches the user.</summary>
    Drop = 1,

    /// <summary>Fail the run with an <see cref="Validation.A2UIValidationException"/>.</summary>
    Throw = 2,
}
