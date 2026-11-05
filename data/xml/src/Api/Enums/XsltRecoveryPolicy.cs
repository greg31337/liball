namespace xml;

/// <summary>
/// Specifies XSLT recovery behavior where recovery is permitted.
/// </summary>
public enum XsltRecoveryPolicy
{
    /// <summary>
    /// Does not recover from an error.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Uses specification-permitted recovery and reports a diagnostic.
    /// </summary>
    Recover = 1
}
