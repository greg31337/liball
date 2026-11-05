namespace barcode;

/// <summary>
/// Holds EAN, UPC, and supplemental add-on encoding options.
/// </summary>
public class EanUpcOptions
{
    /// <summary>
    /// The check-digit calculation and validation policy.
    /// </summary>
    public BarcodeChecksumMode ChecksumMode;

    /// <summary>
    /// The two- or five-digit supplemental add-on text when present.
    /// </summary>
    public string? AddOn;

    /// <summary>
    /// The allowed add-on lengths.
    /// </summary>
    public int[]? AllowedAddOnLengths;

    /// <summary>
    /// Indicates whether an add-on is required.
    /// </summary>
    public bool RequireAddOn;

    /// <summary>
    /// The UPC-E number system, restricted to zero or one when explicitly selected.
    /// </summary>
    public int UpcENumberSystem;

    /// <summary>
    /// Indicates whether a compressible UPC-A payload should be encoded as UPC-E.
    /// </summary>
    public bool CompressToUpcE;

    /// <summary>
    /// Indicates whether guard bars extend into the human-readable text area.
    /// </summary>
    public bool ExtendGuardBars;

    /// <summary>
    /// Indicates whether quiet-zone indicator marks are included in the layout.
    /// </summary>
    public bool ShowQuietZoneIndicators;
}
