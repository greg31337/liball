namespace barcode;

/// <summary>
/// Selects a Data Matrix high-level encodation mode.
/// </summary>
public enum DataMatrixEncodingMode
{
    /// <summary>
    /// Selects encodation modes automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Uses ASCII encodation.
    /// </summary>
    Ascii = 1,

    /// <summary>
    /// Uses C40 encodation.
    /// </summary>
    C40 = 2,

    /// <summary>
    /// Uses Text encodation.
    /// </summary>
    Text = 3,

    /// <summary>
    /// Uses ANSI X12 encodation.
    /// </summary>
    X12 = 4,

    /// <summary>
    /// Uses EDIFACT encodation.
    /// </summary>
    Edifact = 5,

    /// <summary>
    /// Uses Base 256 encodation.
    /// </summary>
    Base256 = 6
}
