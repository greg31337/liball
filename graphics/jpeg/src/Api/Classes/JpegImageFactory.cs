namespace jpeg;

/// <summary>Default service for creating JPEG image data objects from packed pixels.</summary>
public class JpegImageFactory : IJpegImageFactory
{
    /// <summary>Creates an independent JPEG image factory.</summary>
    public JpegImageFactory()
    {
    }

    /// <summary>Creates an image containing a private copy of packed pixels.</summary>
    /// <param name="pixels">Packed pixel bytes in row-major order.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The packed pixel format.</param>
    /// <returns>A newly allocated image.</returns>
    public JpegImage CreateImage(byte[] pixels, int width, int height, JpegPixelFormat pixelFormat)
    {
        return Jpeg.CreateImageFromPixels(pixels, width, height, pixelFormat);
    }

    /// <summary>Creates an interleaved eight-bit image containing a private copy of strided samples.</summary>
    /// <param name="samples">Interleaved row-major samples, including row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="componentCount">The number of samples per pixel.</param>
    /// <param name="rowStride">The number of byte samples between row starts.</param>
    /// <param name="pixelFormat">The packed pixel layout.</param>
    /// <param name="colorSpace">The interpreted color space.</param>
    /// <returns>A newly allocated image.</returns>
    public JpegImage CreatePackedImageFromByteSamples(System.ReadOnlySpan<byte> samples, int width, int height, int componentCount, int rowStride, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace)
    {
        return new JpegImageFactoryCore().CreatePackedImageFromByteSamples(samples, width, height, componentCount, rowStride, pixelFormat, colorSpace);
    }

    /// <summary>Creates an interleaved wide-sample image containing a private copy of strided samples.</summary>
    /// <param name="samples">Interleaved row-major samples, including row padding.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="componentCount">The number of samples per pixel.</param>
    /// <param name="rowStride">The number of integer samples between row starts.</param>
    /// <param name="precision">The meaningful sample precision, either 12 or 16 bits.</param>
    /// <param name="colorSpace">The interpreted color space.</param>
    /// <returns>A newly allocated image.</returns>
    public JpegImage CreatePackedImageFromIntSamples(System.ReadOnlySpan<int> samples, int width, int height, int componentCount, int rowStride, int precision, JpegColorSpace colorSpace)
    {
        return new JpegImageFactoryCore().CreatePackedImageFromIntSamples(samples, width, height, componentCount, rowStride, precision, colorSpace);
    }

    /// <summary>Creates an eight-bit planar image containing private copies of concatenated component samples.</summary>
    /// <param name="samples">The component planes concatenated in layout order.</param>
    /// <param name="layout">The planar image and component layout.</param>
    /// <returns>A newly allocated image.</returns>
    public JpegImage CreatePlanarImageFromByteSamples(System.ReadOnlySpan<byte> samples, JpegPlanarImageBuffer layout)
    {
        return new JpegImageFactoryCore().CreatePlanarImageFromByteSamples(samples, layout);
    }

    /// <summary>Creates a wide-sample planar image containing private copies of concatenated component samples.</summary>
    /// <param name="samples">The component planes concatenated in layout order.</param>
    /// <param name="layout">The planar image and component layout.</param>
    /// <returns>A newly allocated image.</returns>
    public JpegImage CreatePlanarImageFromIntSamples(System.ReadOnlySpan<int> samples, JpegPlanarImageBuffer layout)
    {
        return new JpegImageFactoryCore().CreatePlanarImageFromIntSamples(samples, layout);
    }
}
