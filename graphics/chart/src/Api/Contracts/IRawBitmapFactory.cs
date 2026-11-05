namespace chart;
/// <summary>Defines substitutable raw bitmap factory operations.</summary>
public interface IRawBitmapFactory
{
    /// <summary>
    /// Creates a raw bitmap from the supplied width, height, stride, and pixel format.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "stride">The number of bytes between the start of adjacent bitmap rows.</param>
    /// <param name = "pixelFormat">The byte ordering and number of channels stored for each pixel.</param>
    /// <returns>A raw bitmap with a newly allocated buffer.</returns>
    RawBitmap CreateWithStride(int width, int height, int stride, ChartPixelFormat pixelFormat);
    /// <summary>
    /// Wraps the supplied buffer as a raw bitmap with the supplied dimensions, stride, and pixel format.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "stride">The number of bytes between the start of adjacent bitmap rows.</param>
    /// <param name = "pixelFormat">The byte ordering and number of channels stored for each pixel.</param>
    /// <param name = "buffer">The caller-owned pixel buffer, which must be large enough for height times stride bytes.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>A raw bitmap that uses the supplied buffer directly.</returns>
    RawBitmap Wrap(int width, int height, int stride, ChartPixelFormat pixelFormat, byte[] buffer);
    /// <summary>Creates an indexed bitmap with explicit row stride and palette.</summary>
    /// <param name = "width">The positive pixel width.</param>
    /// <param name = "height">The positive pixel height.</param>
    /// <param name = "stride">The number of bytes between adjacent rows.</param>
    /// <param name = "palette">One through 256 palette colors.</param>
    /// <returns>A newly allocated indexed bitmap.</returns>
    RawBitmap CreateIndexedWithStride(int width, int height, int stride, ChartColor[] palette);
    /// <summary>Wraps caller-owned indexed bytes with an explicit palette.</summary>
    /// <param name = "width">The positive pixel width.</param>
    /// <param name = "height">The positive pixel height.</param>
    /// <param name = "stride">The number of bytes between adjacent rows.</param>
    /// <param name = "buffer">The caller-owned indexed pixel buffer.</param>
    /// <param name = "palette">One through 256 palette colors.</param>
    /// <returns>An indexed bitmap using the supplied buffer directly.</returns>
    RawBitmap WrapIndexed(int width, int height, int stride, byte[] buffer, ChartColor[] palette);
    /// <summary>Converts a bitmap to the requested pixel format.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "pixelFormat">The destination pixel format.</param>
    /// <returns>A tightly packed converted bitmap.</returns>
    RawBitmap Convert(RawBitmap bitmap, ChartPixelFormat pixelFormat);
    /// <summary>Converts a bitmap to indexed storage using an explicit palette.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "palette">One through 256 destination colors.</param>
    /// <returns>A tightly packed indexed bitmap.</returns>
    RawBitmap ConvertToIndexed(RawBitmap bitmap, ChartColor[] palette);
    /// <summary>
    /// Calculates the tightly packed number of bytes in one row for a width and pixel format.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "pixelFormat">The byte ordering and number of channels stored for each pixel.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    int CalculateStride(int width, ChartPixelFormat pixelFormat);
    /// <summary>
    /// Applies the bytes per pixel operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "pixelFormat">The byte ordering and number of channels stored for each pixel.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    int BytesPerPixel(ChartPixelFormat pixelFormat);
}
