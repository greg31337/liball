namespace barcode;

/// <summary>
/// Holds Code 128 code-set and function-character options.
/// </summary>
public class Code128Options
{
    /// <summary>
    /// The requested code set or automatic code-set planning mode.
    /// </summary>
    public Code128CodeSet CodeSet;

    /// <summary>
    /// Indicates whether FNC1 markers in the payload are permitted.
    /// </summary>
    public bool AllowFnc1;

    /// <summary>
    /// Indicates whether FNC2 markers in the payload are permitted.
    /// </summary>
    public bool AllowFnc2;

    /// <summary>
    /// Indicates whether FNC3 markers in the payload are permitted.
    /// </summary>
    public bool AllowFnc3;

    /// <summary>
    /// Indicates whether FNC4 upper-shift behavior is permitted.
    /// </summary>
    public bool AllowFnc4;

    /// <summary>
    /// Indicates whether ISO-8859-1 upper-shift planning is enabled.
    /// </summary>
    public bool UseLatin1UpperShift;
}
