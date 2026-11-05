namespace barcode;

/// <summary>
/// Holds an encoded logical barcode symbol, layout, payload, and encoder metadata.
/// </summary>
public class BarcodeSymbol
{
    /// <summary>
    /// The exact encoded format or application profile.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The dimensional organization of the encoded symbol.
    /// </summary>
    public BarcodeDimension Dimension;

    /// <summary>
    /// The logical symbol width in modules, excluding external rendering space.
    /// </summary>
    public int WidthModules;

    /// <summary>
    /// The logical symbol height in modules, excluding external rendering space.
    /// </summary>
    public int HeightModules;

    /// <summary>
    /// The row-major logical modules, where zero is light and a nonzero value is dark.
    /// </summary>
    public byte[]? Modules;

    /// <summary>
    /// The payload represented by the symbol.
    /// </summary>
    public BarcodePayload? Payload;

    /// <summary>
    /// The selected logical, raster, and physical size.
    /// </summary>
    public BarcodeSize? Size;

    /// <summary>
    /// The quiet zone required by the encoded format.
    /// </summary>
    public BarcodeQuietZone? RequiredQuietZone;

    /// <summary>
    /// The positioned human-readable text layout.
    /// </summary>
    public BarcodeTextLayout? TextLayout;

    /// <summary>
    /// The bearer-bar rectangles required by profiles such as ITF-14.
    /// </summary>
    public BarcodeRectangle[]? BearerBars;

    /// <summary>
    /// The ISO/IEC 15424 symbology identifier.
    /// </summary>
    public string? SymbologyIdentifier;

    /// <summary>
    /// The format-specific encoded codewords when available.
    /// </summary>
    public int[]? Codewords;

    /// <summary>
    /// Indicates whether a check value is present in <see cref="ChecksumValue"/>.
    /// </summary>
    public bool HasChecksum;

    /// <summary>
    /// The calculated check value when one is present.
    /// </summary>
    public int ChecksumValue;
}
