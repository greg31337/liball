namespace barcode;

/// <summary>
/// Holds Interleaved 2 of 5 and ITF-14 encoding options.
/// </summary>
public class ItfOptions
{
    /// <summary>
    /// The wide-to-narrow element ratio.
    /// </summary>
    public double WideToNarrowRatio;

    /// <summary>
    /// The permitted payload lengths.
    /// </summary>
    public int[]? AllowedLengths;

    /// <summary>
    /// The check-digit calculation and validation policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// Indicates whether bearer bars are included.
    /// </summary>
    public bool UseBearerBars;

    /// <summary>
    /// Indicates whether bearer bars surround all four sides instead of only the top and bottom.
    /// </summary>
    public bool UseBearerBox;

    /// <summary>
    /// The bearer-bar thickness in narrow modules.
    /// </summary>
    public int BearerBarThicknessModules;

    /// <summary>
    /// Indicates whether the corrugated-printing sizing profile is requested.
    /// </summary>
    public bool UseCorrugatedProfile;
}
