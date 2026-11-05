namespace barcode;

/// <summary>
/// Holds requested logical, raster, and physical barcode sizing constraints.
/// </summary>
public class BarcodeSizing
{
    /// <summary>
    /// The requested symbol width in modules, or zero when automatic.
    /// </summary>
    public int RequestedWidthModules;

    /// <summary>
    /// The requested symbol height in modules, or zero when automatic.
    /// </summary>
    public int RequestedHeightModules;

    /// <summary>
    /// The requested output width in pixels, or zero when automatic.
    /// </summary>
    public int RequestedWidthPixels;

    /// <summary>
    /// The requested output height in pixels, or zero when automatic.
    /// </summary>
    public int RequestedHeightPixels;

    /// <summary>
    /// The requested physical width in millimetres, or zero when unspecified.
    /// </summary>
    public double RequestedWidthMillimetres;

    /// <summary>
    /// The requested physical height in millimetres, or zero when unspecified.
    /// </summary>
    public double RequestedHeightMillimetres;

    /// <summary>
    /// The requested output resolution in dots per inch, or zero when unspecified.
    /// </summary>
    public double DotsPerInch;

    /// <summary>
    /// The requested module or X dimension in millimetres, or zero when automatic.
    /// </summary>
    public double ModuleSizeMillimetres;

    /// <summary>
    /// The requested linear-bar height in millimetres, or zero when automatic.
    /// </summary>
    public double BarHeightMillimetres;

    /// <summary>
    /// The preferred width-to-height ratio, or zero when automatic.
    /// </summary>
    public double TargetAspectRatio;

    /// <summary>
    /// The requested quiet-zone dimensions when explicitly supplied.
    /// </summary>
    public BarcodeQuietZone? QuietZone;
}
