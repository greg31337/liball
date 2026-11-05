using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicDecoderPathTests
{
    [Test]
    public void ByteMemoryAndStreamEntryPointsHaveRawAnimationAndIdentifyParity()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedMemory(source);

        GifDocument bytesDocument = decoder.DecodeBytes(source, null);
        GifDocument memoryDocument = decoder.DecodeMemory(memory, null);
        GifDocument streamDocument = decoder.DecodeStream(new MemoryStream(source), null);
        GifAnimation bytesAnimation = decoder.DecodeAnimationBytes(source, null);
        GifAnimation memoryAnimation = decoder.DecodeAnimationMemory(memory, null);
        GifAnimation streamAnimation = decoder.DecodeAnimationStream(new MemoryStream(source), null);
        GifInfo bytesInfo = decoder.IdentifyBytes(source, null);
        GifInfo memoryInfo = decoder.IdentifyMemory(memory, null);
        GifInfo streamInfo = decoder.IdentifyStream(new MemoryStream(source), null);

        AssertDocumentsMatch(bytesDocument, memoryDocument);
        AssertDocumentsMatch(bytesDocument, streamDocument);
        AssertAnimationsMatch(bytesAnimation, memoryAnimation);
        AssertAnimationsMatch(bytesAnimation, streamAnimation);
        AssertInfosMatch(bytesInfo, memoryInfo);
        AssertInfosMatch(bytesInfo, streamInfo);
    }

    [Test]
    public void StreamDecoderOperationsNeverCloseCallerOwnedStreams()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        TrackingReadStream decode = new TrackingReadStream(source);
        TrackingReadStream animation = new TrackingReadStream(source);
        TrackingReadStream identify = new TrackingReadStream(source);
        TrackingReadStream rawFrame = new TrackingReadStream(source);
        TrackingReadStream rawBuffer = new TrackingReadStream(source);
        TrackingReadStream compositedFrame = new TrackingReadStream(source);
        TrackingReadStream compositedBuffer = new TrackingReadStream(source);

        decoder.DecodeStream(decode, null);
        decoder.DecodeAnimationStream(animation, null);
        decoder.IdentifyStream(identify, null);
        decoder.DecodeFrameStream(rawFrame, 1, null);
        decoder.DecodeFrameStreamToSpan(rawBuffer, 1, new byte[4], null);
        decoder.DecodeCompositedFrameStream(compositedFrame, 1, null);
        decoder.DecodeCompositedFrameStreamToSpan(compositedBuffer, 1, new byte[24], null);

        AssertStreamOpen(decode);
        AssertStreamOpen(animation);
        AssertStreamOpen(identify);
        AssertStreamOpen(rawFrame);
        AssertStreamOpen(rawBuffer);
        AssertStreamOpen(compositedFrame);
        AssertStreamOpen(compositedBuffer);
    }

    [Test]
    public void MetadataOnlyAndFirstFrameOptionsHaveEntryPointParity()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedMemory(source);
        GifDecodeOptions metadataOptions = new GifDecodeOptions();
        metadataOptions.MetadataOnly = true;
        GifDecodeOptions firstFrameOptions = new GifDecodeOptions();
        firstFrameOptions.FirstFrameOnly = true;

        GifDocument metadataBytes = decoder.DecodeBytes(source, metadataOptions);
        GifDocument metadataMemory = decoder.DecodeMemory(memory, metadataOptions);
        GifDocument metadataStream = decoder.DecodeStream(new MemoryStream(source), metadataOptions);
        GifDocument firstBytes = decoder.DecodeBytes(source, firstFrameOptions);
        GifDocument firstMemory = decoder.DecodeMemory(memory, firstFrameOptions);
        GifDocument firstStream = decoder.DecodeStream(new MemoryStream(source), firstFrameOptions);

        AssertMetadataOnly(metadataBytes);
        AssertMetadataOnly(metadataMemory);
        AssertMetadataOnly(metadataStream);
        Assert.That(firstBytes.Frames, Has.Length.EqualTo(1));
        AssertDocumentsMatch(firstBytes, firstMemory);
        AssertDocumentsMatch(firstBytes, firstStream);
        Assert.That(firstBytes.DecodeStatistics.FramesRead, Is.EqualTo(1));
        Assert.That(metadataOptions.MetadataOnly, Is.True);
        Assert.That(firstFrameOptions.FirstFrameOnly, Is.True);
    }

    [Test]
    public void MaximumFrameLimitFailsConsistentlyAcrossEntryPoints()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxFrames = 2;
        TrackingReadStream stream = new TrackingReadStream(source);

        GifException bytes = Assert.Throws<GifException>(() => decoder.DecodeBytes(source, options));
        GifException memory = Assert.Throws<GifException>(() => decoder.DecodeMemory(CreatePaddedMemory(source), options));
        GifException streamFailure = Assert.Throws<GifException>(() => decoder.DecodeStream(stream, options));

        AssertFrameLimit(bytes);
        AssertFrameLimit(memory);
        AssertFrameLimit(streamFailure);
        AssertStreamOpen(stream);
        Assert.That(options.MaxFrames, Is.EqualTo(2));
    }

    [Test]
    public void OnDemandRawAndCompositedFramesHaveEntryPointParity()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedMemory(source);

        GifDocument rawBytes = decoder.DecodeFrameBytes(source, 1, null);
        GifDocument rawMemory = decoder.DecodeFrameMemory(memory, 1, null);
        GifDocument rawStream = decoder.DecodeFrameStream(new MemoryStream(source), 1, null);
        GifAnimation compositedBytes = decoder.DecodeCompositedFrameBytes(source, 1, null);
        GifAnimation compositedMemory = decoder.DecodeCompositedFrameMemory(memory, 1, null);
        GifAnimation compositedStream = decoder.DecodeCompositedFrameStream(new MemoryStream(source), 1, null);

        AssertDocumentsMatch(rawBytes, rawMemory);
        AssertDocumentsMatch(rawBytes, rawStream);
        Assert.That(rawBytes.Frames[0].IndexedPixels, Is.Empty);
        Assert.That(rawBytes.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(rawBytes.Frames[2].IndexedPixels, Is.Empty);
        AssertAnimationsMatch(compositedBytes, compositedMemory);
        AssertAnimationsMatch(compositedBytes, compositedStream);
        Assert.That(compositedBytes.Frames, Has.Length.EqualTo(1));
        Assert.That(compositedBytes.Frames[0].FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void CallerBuffersHaveByteMemoryAndStreamParity()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedMemory(source);
        byte[] rawBytes = new byte[4];
        byte[] rawMemory = new byte[4];
        byte[] rawStream = new byte[4];
        byte[] rgbaBytes = new byte[24];
        byte[] rgbaMemory = new byte[24];
        byte[] rgbaStream = new byte[24];

        int rawByteCount = decoder.DecodeFrameBytesToSpan(source, 1, rawBytes, null);
        int rawMemoryCount = decoder.DecodeFrameMemoryToSpan(memory, 1, rawMemory, null);
        int rawStreamCount = decoder.DecodeFrameStreamToSpan(new MemoryStream(source), 1, rawStream, null);
        int rgbaByteCount = decoder.DecodeCompositedFrameBytesToSpan(source, 1, rgbaBytes, null);
        int rgbaMemoryCount = decoder.DecodeCompositedFrameMemoryToSpan(memory, 1, rgbaMemory, null);
        int rgbaStreamCount = decoder.DecodeCompositedFrameStreamToSpan(new MemoryStream(source), 1, rgbaStream, null);

        Assert.That(new int[] { rawByteCount, rawMemoryCount, rawStreamCount }, Is.EqualTo(new int[] { 4, 4, 4 }));
        Assert.That(rawMemory, Is.EqualTo(rawBytes));
        Assert.That(rawStream, Is.EqualTo(rawBytes));
        Assert.That(new int[] { rgbaByteCount, rgbaMemoryCount, rgbaStreamCount }, Is.EqualTo(new int[] { 24, 24, 24 }));
        Assert.That(rgbaMemory, Is.EqualTo(rgbaBytes));
        Assert.That(rgbaStream, Is.EqualTo(rgbaBytes));
    }

    [Test]
    public void StreamWithNoImageFramesDecodesAndIdentifiesSuccessfully()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = CreateNoImageGif();
        TrackingReadStream decodeStream = new TrackingReadStream(source);
        TrackingReadStream identifyStream = new TrackingReadStream(source);

        GifDocument document = decoder.DecodeStream(decodeStream, null);
        GifInfo info = decoder.IdentifyStream(identifyStream, null);

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.DecodeStatistics.FramesRead, Is.EqualTo(0));
        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(0));
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(0));
        AssertStreamOpen(decodeStream);
        AssertStreamOpen(identifyStream);
    }

    [Test]
    public void SuppliedGlobalPaletteEnablesPaletteLessAnimationAcrossEntryPoints()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = CreatePaletteLessImageGif();
        GifDecodeOptions options = new GifDecodeOptions();
        options.DefaultGlobalColorTable = new byte[] { 10, 20, 30, 40, 50, 60 };

        GifAnimation bytes = decoder.DecodeAnimationBytes(source, options);
        GifAnimation memory = decoder.DecodeAnimationMemory(CreatePaddedMemory(source), options);
        TrackingReadStream input = new TrackingReadStream(source);
        GifAnimation stream = decoder.DecodeAnimationStream(input, options);

        Assert.That(bytes.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
        AssertAnimationsMatch(bytes, memory);
        AssertAnimationsMatch(bytes, stream);
        Assert.That(options.DefaultGlobalColorTable, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        AssertStreamOpen(input);
    }

    static void AssertDocumentsMatch(GifDocument expected, GifDocument actual)
    {
        Assert.That(actual.Version, Is.EqualTo(expected.Version));
        Assert.That(actual.LogicalScreen.Width, Is.EqualTo(expected.LogicalScreen.Width));
        Assert.That(actual.LogicalScreen.Height, Is.EqualTo(expected.LogicalScreen.Height));
        Assert.That(actual.LoopCount, Is.EqualTo(expected.LoopCount));
        Assert.That(actual.Comments.Length, Is.EqualTo(expected.Comments.Length));
        Assert.That(actual.Frames.Length, Is.EqualTo(expected.Frames.Length));
        int index = 0;
        while (index < expected.Frames.Length)
        {
            Assert.That(actual.Frames[index].Left, Is.EqualTo(expected.Frames[index].Left));
            Assert.That(actual.Frames[index].Top, Is.EqualTo(expected.Frames[index].Top));
            Assert.That(actual.Frames[index].Width, Is.EqualTo(expected.Frames[index].Width));
            Assert.That(actual.Frames[index].Height, Is.EqualTo(expected.Frames[index].Height));
            Assert.That(actual.Frames[index].CompressedImageData, Is.EqualTo(expected.Frames[index].CompressedImageData));
            Assert.That(actual.Frames[index].IndexedPixels, Is.EqualTo(expected.Frames[index].IndexedPixels));
            index = index + 1;
        }
    }

    static void AssertAnimationsMatch(GifAnimation expected, GifAnimation actual)
    {
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        Assert.That(actual.LoopCount, Is.EqualTo(expected.LoopCount));
        Assert.That(actual.TotalDurationMilliseconds, Is.EqualTo(expected.TotalDurationMilliseconds));
        Assert.That(actual.Frames.Length, Is.EqualTo(expected.Frames.Length));
        int index = 0;
        while (index < expected.Frames.Length)
        {
            Assert.That(actual.Frames[index].FrameIndex, Is.EqualTo(expected.Frames[index].FrameIndex));
            Assert.That(actual.Frames[index].DelayMilliseconds, Is.EqualTo(expected.Frames[index].DelayMilliseconds));
            Assert.That(actual.Frames[index].Bitmap.RgbaPixels, Is.EqualTo(expected.Frames[index].Bitmap.RgbaPixels));
            index = index + 1;
        }
    }

    static void AssertInfosMatch(GifInfo expected, GifInfo actual)
    {
        Assert.That(actual.Success, Is.EqualTo(expected.Success));
        Assert.That(actual.Version, Is.EqualTo(expected.Version));
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        Assert.That(actual.FrameCount, Is.EqualTo(expected.FrameCount));
        Assert.That(actual.LoopCount, Is.EqualTo(expected.LoopCount));
        Assert.That(actual.EstimatedDecodedBytes, Is.EqualTo(expected.EstimatedDecodedBytes));
        Assert.That(actual.ApplicationExtensionIdentifiers, Is.EqualTo(expected.ApplicationExtensionIdentifiers));
    }

    static void AssertMetadataOnly(GifDocument document)
    {
        Assert.That(document.Frames, Has.Length.EqualTo(3));
        Assert.That(document.DecodeStatistics.CompressedBytesRead, Is.GreaterThan(0));
        int index = 0;
        while (index < document.Frames.Length)
        {
            Assert.That(document.Frames[index].CompressedImageData, Is.Empty);
            Assert.That(document.Frames[index].IndexedPixels, Is.Empty);
            index = index + 1;
        }
    }

    static void AssertFrameLimit(GifException exception)
    {
        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.FrameLimitExceeded));
        Assert.That(exception.FrameIndex, Is.EqualTo(2));
        Assert.That(exception.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    static void AssertStreamOpen(TrackingReadStream stream)
    {
        Assert.That(stream.WasDisposed, Is.False);
        Assert.That(stream.CanRead, Is.True);
    }

    static ReadOnlyMemory<byte> CreatePaddedMemory(byte[] source)
    {
        byte[] padded = new byte[source.Length + 4];
        padded[0] = 9;
        padded[1] = 8;
        Array.Copy(source, 0, padded, 2, source.Length);
        padded[padded.Length - 2] = 7;
        padded[padded.Length - 1] = 6;
        return new ReadOnlyMemory<byte>(padded, 2, source.Length);
    }

    static byte[] CreateNoImageGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x3B
        };
    }

    static byte[] CreatePaletteLessImageGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    class TrackingReadStream : MemoryStream
    {
        public bool WasDisposed;

        public TrackingReadStream(byte[] data)
            : base(data)
        {
            WasDisposed = false;
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
