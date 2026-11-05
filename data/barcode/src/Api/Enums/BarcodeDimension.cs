namespace barcode;

/// <summary>
/// Classifies the dimensional organization of a barcode.
/// </summary>
public enum BarcodeDimension
{
    /// <summary>
    /// Represents a linear barcode.
    /// </summary>
    Linear = 0,

    /// <summary>
    /// Represents a matrix barcode.
    /// </summary>
    Matrix = 1,

    /// <summary>
    /// Represents a stacked-linear barcode.
    /// </summary>
    StackedLinear = 2
}
