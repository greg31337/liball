namespace barcode;

/// <summary>
/// Identifies a supported GS1 data syntax.
/// </summary>
public enum Gs1Syntax
{
    /// <summary>
    /// Uses bracketed application identifier syntax.
    /// </summary>
    Bracketed = 0,

    /// <summary>
    /// Uses an unbracketed element string.
    /// </summary>
    ElementString = 1,

    /// <summary>
    /// Uses a barcode message with separator semantics.
    /// </summary>
    BarcodeMessage = 2,

    /// <summary>
    /// Uses scanner data with a symbology identifier.
    /// </summary>
    ScannerData = 3,

    /// <summary>
    /// Uses human-readable interpretation text.
    /// </summary>
    HumanReadableInterpretation = 4,

    /// <summary>
    /// Uses a GS1 Digital Link URI.
    /// </summary>
    DigitalLinkUri = 5
}
