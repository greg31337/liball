namespace barcode;

/// <summary>
/// Holds positioned human-readable text and the area reserved for it.
/// </summary>
public class BarcodeTextLayout
{
    /// <summary>
    /// The positioned text runs in display order.
    /// </summary>
    public BarcodeTextRun[]? Runs;

    /// <summary>
    /// The dark bar extensions that descend into the reserved text area.
    /// </summary>
    public BarcodeRectangle[]? BarExtensions;

    /// <summary>
    /// The total height reserved for human-readable text in layout units.
    /// </summary>
    public double ReservedHeight;

    /// <summary>
    /// The bounds of the reserved text area.
    /// </summary>
    public BarcodeRectangle? Bounds;

    /// <summary>
    /// The complete size of the layout containing the text.
    /// </summary>
    public BarcodeSize? Size;
}
