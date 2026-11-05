namespace whois;

/// <summary>
/// Specifies how response bytes that are not valid strict UTF-8 are handled.
/// </summary>
public enum WhoisDecodingMode
{
    /// <summary>
    /// Reports a decoding failure for invalid input.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Replaces invalid UTF-8 byte sequences while decoding.
    /// </summary>
    ReplaceInvalidUtf8 = 1,

    /// <summary>
    /// Falls back to ISO-8859-1 so every byte is preserved as a character.
    /// </summary>
    Latin1Fallback = 2
}
