namespace asn1;

/// <summary>
/// Identifies one application-provided PER instruction registration.
/// </summary>
public class AsnPerInstructionRegistration
{
    /// <summary>
    /// Contains the identifying instruction keyword.
    /// </summary>
    public string Keyword = "";

    /// <summary>
    /// Contains the stable application handler identity.
    /// </summary>
    public string HandlerId = "";

    /// <summary>
    /// Indicates whether the handler supports aligned PER.
    /// </summary>
    public bool SupportsAligned;

    /// <summary>
    /// Indicates whether the handler supports unaligned PER.
    /// </summary>
    public bool SupportsUnaligned;

    /// <summary>
    /// Indicates whether the handler contributes reversible encode/decode steps.
    /// </summary>
    public bool Reversible = true;

    /// <summary>
    /// Contains identifying keywords that cannot be applied with this registration.
    /// </summary>
    public string[] ConflictingKeywords = [];
}
