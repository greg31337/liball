using System.Collections.Generic;

namespace jpeg;

/// <summary>Describes a row-major interleaved sample buffer for any positive component count.</summary>
/// <remarks>Eight-bit samples use <c>Pixels</c>. Twelve-bit and sixteen-bit samples use <c>IntSamples</c>. <c>Stride</c> is measured in storage elements, not bytes.</remarks>
public class JpegInterleavedPixelBuffer
{
    /// <summary>Creates an empty interleaved buffer with initialized sample arrays.</summary>
    public JpegInterleavedPixelBuffer()
    {
    }

    /// <summary>The image width in pixels.</summary>
    public int Width;
    /// <summary>The image height in pixels.</summary>
    public int Height;
    /// <summary>The number of meaningful bits in each sample: 8, 12, or 16.</summary>
    public int Precision;
    /// <summary>The number of components stored for each pixel.</summary>
    public int ComponentCount;
    /// <summary>The logical packed pixel interpretation.</summary>
    public JpegPixelFormat PixelFormat;
    /// <summary>The number of sample elements stored for each pixel.</summary>
    public int SamplesPerPixel;
    /// <summary>The byte count for each pixel in an eight-bit buffer, or zero for integer storage.</summary>
    public int BytesPerPixel;
    /// <summary>The number of storage elements between the starts of adjacent rows.</summary>
    public int Stride;
    /// <summary>The managed element type containing the samples.</summary>
    public JpegSampleStorage SampleStorage;
    /// <summary>The ownership relationship for the active sample array.</summary>
    public JpegBufferOwnership Ownership;
    /// <summary>The interleaved eight-bit samples, including any row padding.</summary>
    public byte[] Pixels = new byte[0];
    /// <summary>The interleaved twelve-bit or sixteen-bit samples, including any row padding.</summary>
    public int[] IntSamples = new int[0];
}

/// <summary>Describes one planar JPEG component and its sampling relationship.</summary>
/// <remarks>Eight-bit samples use <c>Samples</c>. Twelve-bit and sixteen-bit samples use <c>IntSamples</c>. <c>Stride</c> is measured in storage elements.</remarks>
public class JpegPlanarComponentBuffer
{
    /// <summary>Creates an empty component buffer with initialized sample arrays.</summary>
    public JpegPlanarComponentBuffer()
    {
    }

    /// <summary>The zero-based component position.</summary>
    public int ComponentIndex;
    /// <summary>The component identifier recorded by the JPEG frame.</summary>
    public int Identifier;
    /// <summary>The number of meaningful bits in each sample: 8, 12, or 16.</summary>
    public int Precision;
    /// <summary>The horizontal sampling factor from one through four.</summary>
    public int HorizontalSamplingFactor;
    /// <summary>The vertical sampling factor from one through four.</summary>
    public int VerticalSamplingFactor;
    /// <summary>The unpadded component width in samples.</summary>
    public int Width;
    /// <summary>The unpadded component height in samples.</summary>
    public int Height;
    /// <summary>The padded component width in samples.</summary>
    public int PaddedWidth;
    /// <summary>The padded component height in samples.</summary>
    public int PaddedHeight;
    /// <summary>The number of storage elements between the starts of adjacent rows.</summary>
    public int Stride;
    /// <summary>The managed element type containing the samples.</summary>
    public JpegSampleStorage SampleStorage;
    /// <summary>The ownership relationship for the active sample array.</summary>
    public JpegBufferOwnership Ownership;
    /// <summary>The planar eight-bit samples, including any row padding.</summary>
    public byte[] Samples = new byte[0];
    /// <summary>The planar twelve-bit or sixteen-bit samples, including any row padding.</summary>
    public int[] IntSamples = new int[0];
}

/// <summary>Collects separate component sample planes for an image.</summary>
public class JpegPlanarImageBuffer
{
    /// <summary>Creates an empty planar image buffer with an initialized component collection.</summary>
    public JpegPlanarImageBuffer()
    {
    }

    /// <summary>The full image width in pixels.</summary>
    public int Width;
    /// <summary>The full image height in pixels.</summary>
    public int Height;
    /// <summary>The number of meaningful bits in each component sample: 8, 12, or 16.</summary>
    public int Precision;
    /// <summary>The number of component planes.</summary>
    public int ComponentCount;
    /// <summary>The interpreted color space of the component planes.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>The named sampling relationship, or <see cref="JpegSubsampling.Custom"/> for an arbitrary legal layout.</summary>
    public JpegSubsampling Subsampling;
    /// <summary>The ownership relationship common to library-allocated component planes.</summary>
    public JpegBufferOwnership Ownership;
    /// <summary>The component planes in frame order.</summary>
    public List<JpegPlanarComponentBuffer> Components = new List<JpegPlanarComponentBuffer>();
}
