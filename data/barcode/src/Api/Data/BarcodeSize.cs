namespace barcode;

/// <summary>
/// Holds the logical, raster, and physical dimensions selected for a barcode.
/// </summary>
public class BarcodeSize
{
    /// <summary>
    /// The selected symbol width in modules.
    /// </summary>
    public int WidthModules;

    /// <summary>
    /// The selected symbol height in modules.
    /// </summary>
    public int HeightModules;

    /// <summary>
    /// The selected output width in pixels.
    /// </summary>
    public int WidthPixels;

    /// <summary>
    /// The selected output height in pixels.
    /// </summary>
    public int HeightPixels;

    /// <summary>
    /// The selected physical width in millimetres.
    /// </summary>
    public double WidthMillimetres;

    /// <summary>
    /// The selected physical height in millimetres.
    /// </summary>
    public double HeightMillimetres;

    /// <summary>
    /// The output resolution in dots per inch.
    /// </summary>
    public double DotsPerInch;

    /// <summary>
    /// The selected module or X dimension in millimetres.
    /// </summary>
    public double ModuleSizeMillimetres;

    /// <summary>
    /// The selected quiet-zone dimensions.
    /// </summary>
    public BarcodeQuietZone? QuietZone;
}
