using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifSelectedFrameDecodeTests
{
    [Test]
    public void DecodeFrameBytesReturnsAllMetadataAndOnlySelectedRawPixels()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = true;
        options.FirstFrameOnly = true;

        GifDocument document = decoder.DecodeFrameBytes(GifFixtures.ComplexValidGif89a(), 1, options);

        Assert.That(document.Frames.Length, Is.EqualTo(3));
        Assert.That(document.LoopCount, Is.EqualTo(2));
        Assert.That(document.Comments[0].Text, Is.EqualTo("complex"));
        Assert.That(document.Frames[0].CompressedImageData, Is.Empty);
        Assert.That(document.Frames[0].IndexedPixels, Is.Empty);
        Assert.That(document.Frames[1].CompressedImageData, Is.EqualTo(new byte[] { 68, 52, 5 }));
        Assert.That(document.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[1].LocalColorTable.Count, Is.EqualTo(4));
        Assert.That(document.Frames[2].CompressedImageData, Is.Empty);
        Assert.That(document.Frames[2].IndexedPixels, Is.Empty);
        Assert.That(options.MetadataOnly, Is.True);
        Assert.That(options.FirstFrameOnly, Is.True);
    }

    [Test]
    public void DecodeFrameMemoryUsesSliceAndMatchesRawGolden()
    {
        IGifDecoder decoder = new GifCodec();
        ReadOnlyMemory<byte> memory = CreatePaddedSlice(GifFixtures.ComplexValidGif89a());

        GifDocument document = decoder.DecodeFrameMemory(memory, 2, null);

        Assert.That(document.Frames.Length, Is.EqualTo(3));
        Assert.That(document.Frames[0].IndexedPixels, Is.Empty);
        Assert.That(document.Frames[1].IndexedPixels, Is.Empty);
        Assert.That(document.Frames[2].CompressedImageData, Is.EqualTo(new byte[] { 196, 10 }));
        Assert.That(document.Frames[2].IndexedPixels, Is.EqualTo(new byte[] { 0, 3 }));
    }

    [Test]
    public void DecodeFrameStreamSeeksFromInitialPositionAndLeavesStreamOpen()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.ComplexValidGif89a();
        byte[] padded = CreatePaddedBytes(source);
        MemoryStream stream = new MemoryStream(padded);
        stream.Position = 2;

        GifDocument document = decoder.DecodeFrameStream(stream, 1, null);

        Assert.That(document.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.Position, Is.GreaterThan(2));
        Assert.That(stream.Position, Is.LessThanOrEqualTo(2 + source.Length));
    }

    [Test]
    public void DecodeCompositedFrameBytesMatchesSelectedFullAnimationFrame()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;
        byte[] source = GifFixtures.ComplexValidGif89a();

        GifAnimation full = decoder.DecodeAnimationBytes(source, options);
        GifAnimation selected = decoder.DecodeCompositedFrameBytes(source, 2, options);

        Assert.That(selected.Width, Is.EqualTo(full.Width));
        Assert.That(selected.Height, Is.EqualTo(full.Height));
        Assert.That(selected.LoopCount, Is.EqualTo(full.LoopCount));
        Assert.That(selected.Frames.Length, Is.EqualTo(1));
        Assert.That(selected.Frames[0].FrameIndex, Is.EqualTo(2));
        Assert.That(selected.Frames[0].DelayMilliseconds, Is.EqualTo(110));
        Assert.That(selected.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(full.Frames[2].Bitmap.RgbaPixels));
        Assert.That(selected.TotalDurationMilliseconds, Is.EqualTo(110));
    }

    [Test]
    public void DecodeCompositedFrameMemoryUsesSliceAndRetainsOnlyRequestedOutput()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;
        ReadOnlyMemory<byte> memory = CreatePaddedSlice(GifFixtures.ComplexValidGif89a());

        GifAnimation selected = decoder.DecodeCompositedFrameMemory(memory, 1, options);

        Assert.That(selected.Frames.Length, Is.EqualTo(1));
        Assert.That(selected.Frames[0].FrameIndex, Is.EqualTo(1));
        Assert.That(selected.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 255, 255, 255, 0, 255,
            0, 0, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255
        }));
    }

    [Test]
    public void DecodeCompositedFrameStreamMatchesBytePath()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.AnimatedDisposalMethod3Gif89a();
        GifAnimation fromBytes = decoder.DecodeCompositedFrameBytes(source, 2, null);
        MemoryStream stream = new MemoryStream(source);

        GifAnimation fromStream = decoder.DecodeCompositedFrameStream(stream, 2, null);

        Assert.That(fromStream.Frames.Length, Is.EqualTo(1));
        Assert.That(fromStream.Frames[0].FrameIndex, Is.EqualTo(2));
        Assert.That(fromStream.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(fromBytes.Frames[0].Bitmap.RgbaPixels));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void SelectedFrameOperationsRejectOutOfRangeIndexWithContext()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.MinimalGif89a();

        GifException rawException = Assert.Throws<GifException>(() => decoder.DecodeFrameBytes(source, 1, null));
        GifException compositedException = Assert.Throws<GifException>(() => decoder.DecodeCompositedFrameMemory(new ReadOnlyMemory<byte>(source), -1, null));

        Assert.That(rawException.ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(rawException.FrameIndex, Is.EqualTo(1));
        Assert.That(rawException.BlockName, Is.EqualTo("FrameSelection"));
        Assert.That(compositedException.ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(compositedException.FrameIndex, Is.EqualTo(-1));
        Assert.That(compositedException.BlockName, Is.EqualTo("FrameSelection"));
    }

    [Test]
    public void SelectedFrameStreamOperationsRequireSeekableSource()
    {
        IGifDecoder decoder = new GifCodec();
        NonSeekableReadStream stream = new NonSeekableReadStream(GifFixtures.MinimalGif89a());

        GifException exception = Assert.Throws<GifException>(() => decoder.DecodeFrameStream(stream, 0, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidOperationState));
        Assert.That(exception.FrameIndex, Is.EqualTo(0));
        Assert.That(exception.BlockName, Is.EqualTo("FrameSelection"));
        Assert.That(stream.CanRead, Is.True);
    }

    static ReadOnlyMemory<byte> CreatePaddedSlice(byte[] source)
    {
        byte[] padded = CreatePaddedBytes(source);
        return new ReadOnlyMemory<byte>(padded, 2, source.Length);
    }

    static byte[] CreatePaddedBytes(byte[] source)
    {
        byte[] padded = new byte[source.Length + 4];
        padded[0] = 9;
        padded[1] = 8;
        Array.Copy(source, 0, padded, 2, source.Length);
        padded[padded.Length - 2] = 7;
        padded[padded.Length - 1] = 6;
        return padded;
    }

    class NonSeekableReadStream : Stream
    {
        MemoryStream Source;

        public NonSeekableReadStream(byte[] data)
        {
            Source = new MemoryStream(data);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return Source.Length; }
        }

        public override long Position
        {
            get { return Source.Position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
