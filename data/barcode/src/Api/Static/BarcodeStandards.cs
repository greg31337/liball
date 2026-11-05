namespace barcode;

/// <summary>
/// Provides the governing standard editions and data snapshots used by the library.
/// </summary>
public static class BarcodeStandards
{
    /// <summary>
    /// The base Code 128 standard edition.
    /// </summary>
    public const string Code128 = "ISO/IEC 15417:2007";

    /// <summary>
    /// The Code 128 amendment applied in addition to the base edition.
    /// </summary>
    public const string Code128Amendment = "ISO/IEC 15417:2007/Amd 1:2026";

    /// <summary>
    /// The EAN and UPC standard edition.
    /// </summary>
    public const string EanUpc = "ISO/IEC 15420:2025";

    /// <summary>
    /// The Code 39 standard edition.
    /// </summary>
    public const string Code39 = "ISO/IEC 16388:2023";

    /// <summary>
    /// The Data Matrix standard edition.
    /// </summary>
    public const string DataMatrix = "ISO/IEC 16022:2024";

    /// <summary>
    /// The PDF417 standard edition.
    /// </summary>
    public const string Pdf417 = "ISO/IEC 15438:2015";

    /// <summary>
    /// The Interleaved 2 of 5 standard edition.
    /// </summary>
    public const string Itf = "ISO/IEC 16390:2007";

    /// <summary>
    /// The symbology-identifier standard edition.
    /// </summary>
    public const string SymbologyIdentifiers = "ISO/IEC 15424:2025";

    /// <summary>
    /// The Rationalized Codabar specification.
    /// </summary>
    public const string Codabar = "AIM BC3-2000 / USS Codabar";

    /// <summary>
    /// The GS1 General Specifications edition.
    /// </summary>
    public const string Gs1GeneralSpecifications = "GS1 General Specifications 26.0";

    /// <summary>
    /// The checked-in GS1 Barcode Syntax Dictionary snapshot.
    /// </summary>
    public const string Gs1SyntaxDictionarySnapshot = "GS1 Barcode Syntax Dictionary, 2026-08-26 snapshot";

    /// <summary>
    /// The GS1 Digital Link URI Syntax edition.
    /// </summary>
    public const string Gs1DigitalLinkUriSyntax = "GS1 Digital Link URI Syntax 1.7.0";
}
