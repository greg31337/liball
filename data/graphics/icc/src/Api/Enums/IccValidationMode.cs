namespace icc;

/// <summary>
/// Identifies the policy applied while validating a profile.
/// </summary>
public enum IccValidationMode
{
    /// <summary>
    /// Reports defects while accepting recoverable legacy representations.
    /// </summary>
    Tolerant = 0,

    /// <summary>
    /// Enforces the applicable ICC specification requirements.
    /// </summary>
    Conforming = 1,

    /// <summary>
    /// Applies conformance and strict security-oriented structural requirements.
    /// </summary>
    SecurityStrict = 2
}
