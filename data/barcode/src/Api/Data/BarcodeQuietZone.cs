namespace barcode;

/// <summary>
/// Holds four-sided quiet-zone dimensions in modules and output pixels.
/// </summary>
public class BarcodeQuietZone
{
    /// <summary>
    /// The left quiet zone in modules.
    /// </summary>
    public int LeftModules;

    /// <summary>
    /// The top quiet zone in modules.
    /// </summary>
    public int TopModules;

    /// <summary>
    /// The right quiet zone in modules.
    /// </summary>
    public int RightModules;

    /// <summary>
    /// The bottom quiet zone in modules.
    /// </summary>
    public int BottomModules;

    /// <summary>
    /// The left quiet zone in output pixels.
    /// </summary>
    public int LeftPixels;

    /// <summary>
    /// The top quiet zone in output pixels.
    /// </summary>
    public int TopPixels;

    /// <summary>
    /// The right quiet zone in output pixels.
    /// </summary>
    public int RightPixels;

    /// <summary>
    /// The bottom quiet zone in output pixels.
    /// </summary>
    public int BottomPixels;
}
