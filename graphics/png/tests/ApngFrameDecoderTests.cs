using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameDecoderTests
{
    [Test]
    public void Decode_UsesFrameControlDimensionsForFrameLocalBuffer()
    {
        byte[] filteredFrame = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        byte[] compressedFrame = PngZlibCompressor.Compress(filteredFrame);

        PngPixelBuffer buffer = PngApngFrameDecoder.Decode(CreateFrameControl(2, 1), CreateIhdr(10, 10, PngInterlaceMethod.None), compressedFrame);

        AssertBuffer(buffer, PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    [Test]
    public void Decode_ConcatenatesFrameDataChunks()
    {
        byte[] filteredFrame = new byte[] { 0, 10, 20, 30 };
        byte[] compressedFrame = PngZlibCompressor.Compress(filteredFrame);
        PngFrameData[] chunks = SplitFrameData(compressedFrame);

        PngPixelBuffer buffer = PngApngFrameDecoder.Decode(CreateFrameControl(3, 1), CreateGrayscaleIhdr(20, 20), chunks);

        AssertBuffer(buffer, PngPixelFormat.Grayscale8, 3, 1, 3, new byte[] { 10, 20, 30 });
        Assert.That(PngApngFrameDecoder.ConcatenateFrameData(chunks), Is.EqualTo(compressedFrame));
    }

    [Test]
    public void Decode_DecodesAdam7FrameData()
    {
        PngIhdr frameIhdr = CreateIhdr(1, 1, PngInterlaceMethod.Adam7);
        byte[] imageData = new byte[] { 1, 2, 3, 4 };
        byte[] filteredFrame = PngAdam7Encoder.Encode(imageData, frameIhdr);
        byte[] compressedFrame = PngZlibCompressor.Compress(filteredFrame);

        PngPixelBuffer buffer = PngApngFrameDecoder.Decode(CreateFrameControl(1, 1), CreateIhdr(4, 4, PngInterlaceMethod.Adam7), compressedFrame);

        AssertBuffer(buffer, PngPixelFormat.Rgba8, 1, 1, 4, imageData);
    }

    [Test]
    public void Decode_EnforcesDecompressedByteLimit()
    {
        byte[] filteredFrame = new byte[] { 0, 1, 2, 3, 4 };
        byte[] compressedFrame = PngZlibCompressor.Compress(filteredFrame);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 4;

        Assert.Throws<OverflowException>(() => PngApngFrameDecoder.Decode(CreateFrameControl(1, 1), CreateIhdr(1, 1, PngInterlaceMethod.None), compressedFrame, limits));
    }

    [Test]
    public void Decode_RejectsInvalidInputs()
    {
        PngFrameControl control = CreateFrameControl(1, 1);
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.None);
        byte[] compressedFrame = PngZlibCompressor.Compress(new byte[] { 0, 1, 2, 3, 4 });
        PngFrameData[] chunks = SplitFrameData(compressedFrame);
        PngFrameData nullData = new PngFrameData();
        nullData.Data = null!;
        PngFrameControl tooWide = CreateFrameControl(1, 1);
        tooWide.Width = (uint)int.MaxValue + 1;
        PngFrameControl tooHigh = CreateFrameControl(1, 1);
        tooHigh.Height = (uint)int.MaxValue + 1;

        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(null!, ihdr, compressedFrame));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(control, null!, compressedFrame));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(control, ihdr, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(control, ihdr, compressedFrame, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(control, ihdr, (PngFrameData[])null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.Decode(control, ihdr, chunks, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.ConcatenateFrameData(null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.ConcatenateFrameData(new PngFrameData[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameDecoder.ConcatenateFrameData(new PngFrameData[] { nullData }));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDecoder.Decode(tooWide, ihdr, compressedFrame));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameDecoder.Decode(tooHigh, ihdr, compressedFrame));
        Assert.Throws<InvalidDataException>(() => PngApngFrameDecoder.Decode(control, ihdr, new byte[] { 1, 2, 3 }));
    }

    static PngFrameControl CreateFrameControl(uint width, uint height)
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = 0;
        control.Width = width;
        control.Height = height;
        control.XOffset = 0;
        control.YOffset = 0;
        control.DelayNumerator = 1;
        control.DelayDenominator = 24;
        control.DisposeOperation = PngApngDisposeOperation.None;
        control.BlendOperation = PngApngBlendOperation.Source;
        return control;
    }

    static PngIhdr CreateIhdr(int width, int height, PngInterlaceMethod interlaceMethod)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = interlaceMethod;
        return ihdr;
    }

    static PngIhdr CreateGrayscaleIhdr(int width, int height)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.Grayscale;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngFrameData[] SplitFrameData(byte[] compressedFrame)
    {
        int firstLength = compressedFrame.Length / 2;
        int secondLength = compressedFrame.Length - firstLength;
        PngFrameData first = new PngFrameData();
        first.SequenceNumber = 1;
        first.Data = new byte[firstLength];
        PngFrameData second = new PngFrameData();
        second.SequenceNumber = 2;
        second.Data = new byte[secondLength];
        CopyBytes(compressedFrame, 0, first.Data, 0, firstLength);
        CopyBytes(compressedFrame, firstLength, second.Data, 0, secondLength);
        return new PngFrameData[] { first, second };
    }

    static void AssertBuffer(PngPixelBuffer buffer, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(format));
        Assert.That(buffer.Width, Is.EqualTo(width));
        Assert.That(buffer.Height, Is.EqualTo(height));
        Assert.That(buffer.Stride, Is.EqualTo(stride));
        Assert.That(buffer.Data, Is.EqualTo(data));
    }

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
