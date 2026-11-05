namespace ttf;

/// <summary>Identifies the payload stored for one bitmap glyph.</summary>
public enum ColorBitmapPayloadKind
{
    /// <summary>The payload format is preserved but not interpreted.</summary>
    Unsupported = 0,
    /// <summary>A raw monochrome or grayscale bitmap.</summary>
    RawBitmap = 1,
    /// <summary>A composite assembled from other bitmap glyphs.</summary>
    CompositeBitmap = 2,
    /// <summary>A complete PNG image.</summary>
    Png = 3,
    /// <summary>A complete JPEG image.</summary>
    Jpeg = 4,
    /// <summary>A complete TIFF image.</summary>
    Tiff = 5
}
