namespace barcode;

/// <summary>
/// Selects the human-readable text displayed with a barcode.
/// </summary>
public enum BarcodeTextMode
{
    /// <summary>
    /// Hides human-readable text.
    /// </summary>
    Hidden = 0,

    /// <summary>
    /// Displays the standard human-readable interpretation.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Displays the raw payload.
    /// </summary>
    RawPayload = 2,

    /// <summary>
    /// Displays caller-supplied text.
    /// </summary>
    Custom = 3
}
