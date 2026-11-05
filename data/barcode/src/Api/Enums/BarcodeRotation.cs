namespace barcode;

/// <summary>
/// Identifies a clockwise right-angle rotation.
/// </summary>
public enum BarcodeRotation
{
    /// <summary>
    /// Applies no rotation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Rotates clockwise by 90 degrees.
    /// </summary>
    Clockwise90 = 1,

    /// <summary>
    /// Rotates clockwise by 180 degrees.
    /// </summary>
    Clockwise180 = 2,

    /// <summary>
    /// Rotates clockwise by 270 degrees.
    /// </summary>
    Clockwise270 = 3
}
