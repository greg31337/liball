namespace uri;

/// <summary>
/// Selects how percent-decoded octets are converted to Unicode text.
/// </summary>
public enum PercentDecodeMode
{
    /// <summary>Rejects any octet sequence that is not legal strict UTF-8.</summary>
    StrictUtf8 = 0,

    /// <summary>Replaces each ill-formed UTF-8 maximal subpart with U+FFFD.</summary>
    ReplaceInvalidUtf8 = 1
}
