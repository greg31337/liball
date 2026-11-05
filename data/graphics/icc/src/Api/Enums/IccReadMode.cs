namespace icc;

/// <summary>
/// Specifies how profile input errors are handled while reading.
/// </summary>
public enum IccReadMode
{
    /// <summary>
    /// Rejects every structural or conformance error.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Accepts recoverable representation details while enforcing profile conformance.
    /// </summary>
    Conforming = 1,

    /// <summary>
    /// Preserves readable data from malformed profiles and reports recoverable errors.
    /// </summary>
    Recovery = 2
}
