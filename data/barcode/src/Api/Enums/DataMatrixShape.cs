namespace barcode;

/// <summary>
/// Selects the permitted shape of a Data Matrix symbol.
/// </summary>
public enum DataMatrixShape
{
    /// <summary>
    /// Selects a supported shape automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Requires a square ECC 200 symbol.
    /// </summary>
    Square = 1,

    /// <summary>
    /// Requires a standard rectangular ECC 200 symbol.
    /// </summary>
    StandardRectangular = 2
}
