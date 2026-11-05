namespace barcode;

/// <summary>
/// Holds output format, scaling, rotation, color, quiet-zone, and human-readable text options.
/// </summary>
public class BarcodeRenderOptions
{
    /// <summary>
    /// The requested output format.
    /// </summary>
    public BarcodeRenderFormat Format;

    /// <summary>
    /// The requested logical, raster, and physical sizing constraints.
    /// </summary>
    public BarcodeSizing? Sizing;

    /// <summary>
    /// The quiet-zone policy applied by the render plan.
    /// </summary>
    public BarcodeQuietZonePolicy QuietZonePolicy;

    /// <summary>
    /// The custom quiet-zone dimensions when supplied.
    /// </summary>
    public BarcodeQuietZone? QuietZone;

    /// <summary>
    /// The clockwise right-angle output rotation.
    /// </summary>
    public BarcodeRotation Rotation;

    /// <summary>
    /// The requested integral number of output pixels per module, or zero when automatic.
    /// </summary>
    public int ModuleScale;

    /// <summary>
    /// The foreground color packed as red, green, blue, and alpha bytes.
    /// </summary>
    public uint ForegroundRgba;

    /// <summary>
    /// The background color packed as red, green, blue, and alpha bytes.
    /// </summary>
    public uint BackgroundRgba;

    /// <summary>
    /// Indicates whether a transparent background is requested when the format supports it.
    /// </summary>
    public bool TransparentBackground;

    /// <summary>
    /// The human-readable text mode.
    /// </summary>
    public BarcodeTextMode TextMode;

    /// <summary>
    /// The caller-supplied human-readable text when custom text is selected.
    /// </summary>
    public string? CustomText;

    /// <summary>
    /// Indicates whether SVG output may use escaped text instead of built-in glyph paths.
    /// </summary>
    public bool UseSvgText;
}
