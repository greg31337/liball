namespace barcode;

/// <summary>
/// Provides ISO/IEC 15424 symbology identifiers for supported barcode carriers and profiles.
/// </summary>
public static class BarcodeSymbologyIdentifiers
{
    /// <summary>
    /// The Code 128 identifier without GS1 semantics.
    /// </summary>
    public const string Code128 = "]C0";

    /// <summary>
    /// The GS1-128 identifier.
    /// </summary>
    public const string Gs1128 = "]C1";

    /// <summary>
    /// The base EAN and UPC identifier.
    /// </summary>
    public const string EanUpc = "]E0";

    /// <summary>
    /// The Code 39 identifier without optional processing modifiers.
    /// </summary>
    public const string Code39 = "]A0";

    /// <summary>
    /// The Interleaved 2 of 5 identifier without GS1 check-digit semantics.
    /// </summary>
    public const string Itf = "]I0";

    /// <summary>
    /// The ITF-14 identifier with GS1 check-digit semantics.
    /// </summary>
    public const string Itf14 = "]I1";

    /// <summary>
    /// The Codabar identifier without optional processing modifiers.
    /// </summary>
    public const string Codabar = "]F0";

    /// <summary>
    /// The Data Matrix ECC 200 identifier without GS1 semantics.
    /// </summary>
    public const string DataMatrix = "]d1";

    /// <summary>
    /// The GS1 DataMatrix identifier.
    /// </summary>
    public const string Gs1DataMatrix = "]d2";

    /// <summary>
    /// The base PDF417 identifier.
    /// </summary>
    public const string Pdf417 = "]L0";
}
