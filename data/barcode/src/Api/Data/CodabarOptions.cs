namespace barcode;

/// <summary>
/// Holds Codabar terminal, alias, check, transmission, and layout options.
/// </summary>
public class CodabarOptions
{
    /// <summary>
    /// The requested start character or accepted alias.
    /// </summary>
    public string? StartCharacter;

    /// <summary>
    /// The requested stop character or accepted alias.
    /// </summary>
    public string? StopCharacter;

    /// <summary>
    /// Indicates whether common start and stop aliases are accepted.
    /// </summary>
    public bool AcceptAliases;

    /// <summary>
    /// The wide-to-narrow element ratio.
    /// </summary>
    public double WideToNarrowRatio;

    /// <summary>
    /// The intercharacter gap in narrow modules.
    /// </summary>
    public int IntercharacterGapModules;

    /// <summary>
    /// The modulo-16 check-character policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// Indicates whether start and stop characters are retained in transmitted data.
    /// </summary>
    public bool TransmitStartStop;

    /// <summary>
    /// Indicates whether a verified check character is retained in transmitted data.
    /// </summary>
    public bool TransmitCheckCharacter;
}
