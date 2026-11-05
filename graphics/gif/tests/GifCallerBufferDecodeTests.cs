using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCallerBufferDecodeTests
{
    [Test]
    public void RawFrameSpanOperationsWriteSelectedIndexedPixelsAndReportCounts()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        byte[] byteDestination = CreateFilledBytes(8, 91);
        byte[] memoryDestination = CreateFilledBytes(6, 92);
        byte[] streamDestination = CreateFilledBytes(8, 93);

        int byteCount = decoder.DecodeFrameBytesToSpan(source, 1, byteDestination.AsSpan(2, 4), null);
        int memoryCount = decoder.DecodeFrameMemoryToSpan(CreatePaddedSlice(source), 2, memoryDestination.AsSpan(1, 2), null);
        MemoryStream stream = new MemoryStream(source);
        int streamCount = decoder.DecodeFrameStreamToSpan(stream, 1, streamDestination.AsSpan(3, 4), null);

        Assert.That(byteCount, Is.EqualTo(4));
        Assert.That(byteDestination, Is.EqualTo(new byte[] { 91, 91, 0, 1, 2, 3, 91, 91 }));
        Assert.That(memoryCount, Is.EqualTo(2));
        Assert.That(memoryDestination, Is.EqualTo(new byte[] { 92, 0, 3, 92, 92, 92 }));
        Assert.That(streamCount, Is.EqualTo(4));
        Assert.That(streamDestination, Is.EqualTo(new byte[] { 93, 93, 93, 0, 1, 2, 3, 93 }));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void CompositedFrameSpanOperationsWriteRgbaPixelsAndReportCounts()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;
        byte[] source = GifFixtures.ComplexValidGif89a();
        byte[] expected = new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 255, 255, 255, 0, 255,
            0, 0, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255
        };
        byte[] byteDestination = CreateFilledBytes(28, 71);
        byte[] memoryDestination = CreateFilledBytes(26, 72);
        byte[] streamDestination = CreateFilledBytes(25, 73);

        int byteCount = decoder.DecodeCompositedFrameBytesToSpan(source, 1, byteDestination.AsSpan(2, 24), options);
        int memoryCount = decoder.DecodeCompositedFrameMemoryToSpan(CreatePaddedSlice(source), 1, memoryDestination.AsSpan(1, 24), options);
        MemoryStream stream = new MemoryStream(source);
        int streamCount = decoder.DecodeCompositedFrameStreamToSpan(stream, 1, streamDestination.AsSpan(0, 24), options);

        Assert.That(byteCount, Is.EqualTo(24));
        Assert.That(byteDestination.AsSpan(2, 24).ToArray(), Is.EqualTo(expected));
        Assert.That(byteDestination[0], Is.EqualTo(71));
        Assert.That(byteDestination[1], Is.EqualTo(71));
        Assert.That(byteDestination[26], Is.EqualTo(71));
        Assert.That(byteDestination[27], Is.EqualTo(71));
        Assert.That(memoryCount, Is.EqualTo(24));
        Assert.That(memoryDestination.AsSpan(1, 24).ToArray(), Is.EqualTo(expected));
        Assert.That(memoryDestination[0], Is.EqualTo(72));
        Assert.That(memoryDestination[25], Is.EqualTo(72));
        Assert.That(streamCount, Is.EqualTo(24));
        Assert.That(streamDestination.AsSpan(0, 24).ToArray(), Is.EqualTo(expected));
        Assert.That(streamDestination[24], Is.EqualTo(73));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void RawFrameSpanRejectsShortDestinationWithoutPartialWrite()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] destination = CreateFilledBytes(3, 61);

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeFrameBytesToSpan(GifFixtures.ComplexValidGif89a(), 1, destination, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(exception.FrameIndex, Is.EqualTo(1));
        Assert.That(exception.BlockName, Is.EqualTo("RawFrameDestination"));
        Assert.That(exception.Message, Does.Contain("at least 4 bytes"));
        Assert.That(destination, Is.EqualTo(new byte[] { 61, 61, 61 }));
    }

    [Test]
    public void CompositedFrameSpanRejectsShortDestinationWithoutPartialWrite()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] destination = CreateFilledBytes(23, 62);

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeCompositedFrameBytesToSpan(GifFixtures.ComplexValidGif89a(), 1, destination, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(exception.FrameIndex, Is.EqualTo(1));
        Assert.That(exception.BlockName, Is.EqualTo("CompositedFrameDestination"));
        Assert.That(exception.Message, Does.Contain("at least 24 bytes"));
        Assert.That(destination, Is.EqualTo(CreateFilledBytes(23, 62)));
    }

    static ReadOnlyMemory<byte> CreatePaddedSlice(byte[] source)
    {
        byte[] padded = new byte[source.Length + 4];
        padded[0] = 9;
        padded[1] = 8;
        Array.Copy(source, 0, padded, 2, source.Length);
        padded[padded.Length - 2] = 7;
        padded[padded.Length - 1] = 6;
        return new ReadOnlyMemory<byte>(padded, 2, source.Length);
    }

    static byte[] CreateFilledBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index++;
        }

        return data;
    }
}
