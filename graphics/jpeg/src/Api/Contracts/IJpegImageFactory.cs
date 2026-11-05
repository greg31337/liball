namespace jpeg;

/// <summary>Creates JPEG image data objects from packed pixel buffers.</summary>
public interface IJpegImageFactory
{
    /// <summary>Creates an image by copying a tightly packed pixel buffer.</summary>
    /// <param name="pixels">Packed pixel bytes in row-major order with no row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The channel layout of <c>pixels</c>.</param>
    /// <returns>An image containing a private copy of the supplied pixels.</returns>
    JpegImage CreateImage(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat);

    /// <summary>Creates an interleaved eight-bit image by copying a strided read-only sample span.</summary>
    /// <param name="samples">Interleaved row-major samples, including any row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="componentCount">The number of samples stored for each pixel.</param>
    /// <param name="rowStride">The number of byte samples between adjacent row starts.</param>
    /// <param name="pixelFormat">The logical packed pixel layout.</param>
    /// <param name="colorSpace">The interpreted color space.</param>
    /// <returns>An image containing a library-owned copy of the supplied samples.</returns>
    JpegImage CreatePackedImageFromByteSamples(System.ReadOnlySpan<byte> samples, int width, int height, int componentCount, int rowStride, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace);

    /// <summary>Creates an interleaved twelve-bit or sixteen-bit image by copying a strided read-only sample span.</summary>
    /// <param name="samples">Interleaved row-major samples, including any row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="componentCount">The number of samples stored for each pixel.</param>
    /// <param name="rowStride">The number of integer samples between adjacent row starts.</param>
    /// <param name="precision">The meaningful sample precision, either 12 or 16 bits.</param>
    /// <param name="colorSpace">The interpreted color space.</param>
    /// <returns>An image containing a library-owned copy of the supplied samples.</returns>
    JpegImage CreatePackedImageFromIntSamples(System.ReadOnlySpan<int> samples, int width, int height, int componentCount, int rowStride, int precision, JpegColorSpace colorSpace);

    /// <summary>Creates an eight-bit planar image by copying concatenated component planes from a read-only span.</summary>
    /// <remarks>The span contains each component plane in layout order, including <c>Stride</c> padding through <c>PaddedHeight</c>.</remarks>
    /// <param name="samples">The concatenated component-plane samples.</param>
    /// <param name="layout">The image, component, sampling, dimension, and stride description. Its sample arrays are ignored.</param>
    /// <returns>An image containing library-owned copies of the supplied samples and layout.</returns>
    JpegImage CreatePlanarImageFromByteSamples(System.ReadOnlySpan<byte> samples, JpegPlanarImageBuffer layout);

    /// <summary>Creates a twelve-bit or sixteen-bit planar image by copying concatenated component planes from a read-only span.</summary>
    /// <remarks>The span contains each component plane in layout order, including <c>Stride</c> padding through <c>PaddedHeight</c>.</remarks>
    /// <param name="samples">The concatenated component-plane samples.</param>
    /// <param name="layout">The image, component, sampling, dimension, precision, and stride description. Its sample arrays are ignored.</param>
    /// <returns>An image containing library-owned copies of the supplied samples and layout.</returns>
    JpegImage CreatePlanarImageFromIntSamples(System.ReadOnlySpan<int> samples, JpegPlanarImageBuffer layout);
}
