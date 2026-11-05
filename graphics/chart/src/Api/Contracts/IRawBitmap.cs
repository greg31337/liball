namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "RawBitmap"/>.</summary>
public interface IRawBitmap
{
    /// <summary>
    /// Applies the bytes per pixel operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting int.</returns>
    int BytesPerPixel();
    /// <summary>
    /// Returns the requested byte offset.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting int.</returns>
    int GetByteOffset(int x, int y);
    /// <summary>
    /// Determines whether this value contains pixel.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsPixel(int x, int y);
    /// <summary>
    /// Returns the requested pixel.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartColor GetPixel(int x, int y);
    /// <summary>
    /// Sets the pixel configuration.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetPixel(int x, int y, ChartColor color);
    /// <summary>
    /// Creates an independent copy of the buffer.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    byte[] CopyBuffer();
    /// <summary>Returns a mutable zero-copy view of the complete bitmap buffer, including row padding.</summary>
    /// <returns>A span backed by this bitmap's buffer.</returns>
    System.Span<byte> GetBufferSpan();
    /// <summary>Returns a mutable zero-copy view of one complete bitmap row, including row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested bitmap row.</returns>
    System.Span<byte> GetRowSpan(int row);
    /// <summary>Returns a read-only zero-copy view of the complete bitmap buffer.</summary>
    /// <returns>A read-only span backed by this bitmap's buffer.</returns>
    System.ReadOnlySpan<byte> GetReadOnlyBufferSpan();
    /// <summary>Returns a mutable zero-copy view of the pixel bytes in one row, excluding row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested row's pixel bytes.</returns>
    System.Span<byte> GetPixelRowSpan(int row);
    /// <summary>Returns a read-only zero-copy view of one complete bitmap row, including row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A read-only span backed by the requested bitmap row.</returns>
    System.ReadOnlySpan<byte> GetReadOnlyRowSpan(int row);
    /// <summary>Returns a read-only zero-copy view of the pixel bytes in one row, excluding row padding.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A read-only span backed by the requested row's pixel bytes.</returns>
    System.ReadOnlySpan<byte> GetReadOnlyPixelRowSpan(int row);
    /// <summary>Returns an independent copy of this bitmap, including stride, buffer, and indexed palette.</summary>
    /// <returns>An independently mutable raw bitmap.</returns>
    RawBitmap Copy();
    /// <summary>Converts every pixel to the requested storage format.</summary>
    /// <param name = "pixelFormat">The destination pixel format.</param>
    /// <returns>A tightly packed converted bitmap.</returns>
    RawBitmap ConvertTo(ChartPixelFormat pixelFormat);
    /// <summary>Converts every pixel to indexed storage using the supplied palette.</summary>
    /// <param name = "palette">One through 256 colors used for indexed pixels.</param>
    /// <returns>A tightly packed indexed bitmap.</returns>
    RawBitmap ConvertToIndexed(ChartColor[] palette);
    /// <summary>Replaces the palette used by an indexed bitmap.</summary>
    /// <param name = "palette">One through 256 colors copied into this bitmap.</param>
    void SetPalette(ChartColor[] palette);
    /// <summary>Returns an independent copy of the indexed palette.</summary>
    /// <returns>The configured palette colors, or an empty array for a direct-color bitmap.</returns>
    ChartColor[] GetPalette();
    /// <summary>Returns the number of colors in the indexed palette.</summary>
    /// <returns>The palette color count.</returns>
    int GetPaletteCount();
    /// <summary>Returns an independent copy of one indexed palette color.</summary>
    /// <param name = "index">The zero-based palette index.</param>
    /// <returns>The requested palette color.</returns>
    ChartColor GetPaletteColor(int index);
    /// <summary>Replaces one indexed palette color.</summary>
    /// <param name = "index">The zero-based palette index.</param>
    /// <param name = "color">The replacement palette color.</param>
    void SetPaletteColor(int index, ChartColor color);
    /// <summary>Returns the stored palette index for one indexed pixel.</summary>
    /// <param name = "x">The pixel x coordinate.</param>
    /// <param name = "y">The pixel y coordinate.</param>
    /// <returns>The stored palette index.</returns>
    int GetPaletteIndex(int x, int y);
    /// <summary>Stores a palette index for one indexed pixel.</summary>
    /// <param name = "x">The pixel x coordinate.</param>
    /// <param name = "y">The pixel y coordinate.</param>
    /// <param name = "paletteIndex">The zero-based palette index.</param>
    void SetPaletteIndex(int x, int y, int paletteIndex);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    int GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(int value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    int GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(int value);
    /// <summary>Returns the current stride.</summary>
    /// <returns>The current field value.</returns>
    int GetStride();
    /// <summary>Replaces the current stride.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStride(int value);
    /// <summary>Returns the current pixel format.</summary>
    /// <returns>The current field value.</returns>
    ChartPixelFormat GetPixelFormat();
    /// <summary>Replaces the current pixel format.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPixelFormat(ChartPixelFormat value);
    /// <summary>Returns the current buffer.</summary>
    /// <returns>The current field value.</returns>
    byte[] GetBuffer();
    /// <summary>Replaces the current buffer.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBuffer(byte[] value);
}
