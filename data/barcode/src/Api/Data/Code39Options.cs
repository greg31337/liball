namespace barcode;

/// <summary>
/// Holds Code 39 character, check, append, and layout options.
/// </summary>
public class Code39Options
{
    /// <summary>
    /// The standard or Full ASCII character repertoire.
    /// </summary>
    public Code39CharacterMode CharacterMode;

    /// <summary>
    /// The wide-to-narrow element ratio.
    /// </summary>
    public double WideToNarrowRatio;

    /// <summary>
    /// The intercharacter gap in narrow modules.
    /// </summary>
    public int IntercharacterGapModules;

    /// <summary>
    /// The modulo-43 check-character policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// Indicates whether a verified check character is retained in transmitted data.
    /// </summary>
    public bool TransmitCheckCharacter;

    /// <summary>
    /// Indicates whether Code 39 append metadata is enabled.
    /// </summary>
    public bool EnableAppend;
}
