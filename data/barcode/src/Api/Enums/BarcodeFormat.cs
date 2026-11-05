namespace barcode;

/// <summary>
/// Identifies a barcode symbology or application profile.
/// </summary>
public enum BarcodeFormat
{
    /// <summary>
    /// Selects a compatible format automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Identifies Code 128.
    /// </summary>
    Code128 = 1,

    /// <summary>
    /// Identifies the GS1-128 application profile.
    /// </summary>
    Gs1128 = 2,

    /// <summary>
    /// Identifies EAN-8.
    /// </summary>
    Ean8 = 3,

    /// <summary>
    /// Identifies EAN-13.
    /// </summary>
    Ean13 = 4,

    /// <summary>
    /// Identifies UPC-A.
    /// </summary>
    UpcA = 5,

    /// <summary>
    /// Identifies UPC-E.
    /// </summary>
    UpcE = 6,

    /// <summary>
    /// Identifies a two-digit EAN/UPC add-on.
    /// </summary>
    EanUpcAddon2 = 7,

    /// <summary>
    /// Identifies a five-digit EAN/UPC add-on.
    /// </summary>
    EanUpcAddon5 = 8,

    /// <summary>
    /// Identifies Code 39.
    /// </summary>
    Code39 = 9,

    /// <summary>
    /// Identifies Interleaved 2 of 5.
    /// </summary>
    Itf = 10,

    /// <summary>
    /// Identifies the ITF-14 application profile.
    /// </summary>
    Itf14 = 11,

    /// <summary>
    /// Identifies Codabar.
    /// </summary>
    Codabar = 12,

    /// <summary>
    /// Identifies Data Matrix ECC 200.
    /// </summary>
    DataMatrix = 13,

    /// <summary>
    /// Identifies the GS1 DataMatrix application profile.
    /// </summary>
    Gs1DataMatrix = 14,

    /// <summary>
    /// Identifies standard PDF417.
    /// </summary>
    Pdf417 = 15,

    /// <summary>
    /// Identifies Compact PDF417.
    /// </summary>
    CompactPdf417 = 16,

    /// <summary>
    /// Identifies Macro PDF417.
    /// </summary>
    MacroPdf417 = 17
}
