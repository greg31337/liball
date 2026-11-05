namespace barcode;

/// <summary>
/// Holds the four image-space corners and bounds of a barcode region.
/// </summary>
public class BarcodeQuadrilateral
{
    /// <summary>
    /// The top-left corner.
    /// </summary>
    public BarcodePoint? TopLeft;

    /// <summary>
    /// The top-right corner.
    /// </summary>
    public BarcodePoint? TopRight;

    /// <summary>
    /// The bottom-right corner.
    /// </summary>
    public BarcodePoint? BottomRight;

    /// <summary>
    /// The bottom-left corner.
    /// </summary>
    public BarcodePoint? BottomLeft;

    /// <summary>
    /// The axis-aligned bounds enclosing the corners.
    /// </summary>
    public BarcodeRectangle? Bounds;
}
