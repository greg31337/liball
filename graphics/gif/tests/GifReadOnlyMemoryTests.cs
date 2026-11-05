using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifReadOnlyMemoryTests
{
    [Test]
    public void MemoryReaderUsesCallerStorageWithoutCopying()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(source, 1, 1);
        GifMemoryByteReader reader = new GifMemoryByteReader(memory);

        source[1] = 9;

        Assert.That(reader.ReadByte(), Is.EqualTo(9));
        Assert.That(reader.GetByteOffset(), Is.EqualTo(1));
    }

    [Test]
    public void IdentifyMemoryUsesSelectedSliceAndMatchesByteArrayPath()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.NetscapeLoopGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedSlice(source);

        GifInfo fromBytes = decoder.IdentifyBytes(source, null);
        GifInfo fromMemory = decoder.IdentifyMemory(memory, null);

        Assert.That(fromMemory.Success, Is.True);
        Assert.That(fromMemory.Version, Is.EqualTo(fromBytes.Version));
        Assert.That(fromMemory.Width, Is.EqualTo(fromBytes.Width));
        Assert.That(fromMemory.Height, Is.EqualTo(fromBytes.Height));
        Assert.That(fromMemory.FrameCount, Is.EqualTo(fromBytes.FrameCount));
        Assert.That(fromMemory.LoopCount, Is.EqualTo(fromBytes.LoopCount));
        Assert.That(fromMemory.ApplicationExtensionIdentifiers, Is.EqualTo(fromBytes.ApplicationExtensionIdentifiers));
        Assert.That(fromMemory.Error.Code, Is.EqualTo(fromBytes.Error.Code));
    }

    [Test]
    public void DecodeMemoryUsesSelectedSliceAndMatchesByteArrayPath()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.CommentGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedSlice(source);

        GifDocument fromBytes = decoder.DecodeBytes(source, null);
        GifDocument fromMemory = decoder.DecodeMemory(memory, null);

        Assert.That(fromMemory.Version, Is.EqualTo(fromBytes.Version));
        Assert.That(fromMemory.LogicalScreen.Width, Is.EqualTo(fromBytes.LogicalScreen.Width));
        Assert.That(fromMemory.LogicalScreen.Height, Is.EqualTo(fromBytes.LogicalScreen.Height));
        Assert.That(fromMemory.Frames.Length, Is.EqualTo(fromBytes.Frames.Length));
        Assert.That(fromMemory.Frames[0].CompressedImageData, Is.EqualTo(fromBytes.Frames[0].CompressedImageData));
        Assert.That(fromMemory.Comments.Length, Is.EqualTo(1));
        Assert.That(fromMemory.Comments[0].Text, Is.EqualTo("fixture"));
        Assert.That(fromMemory.DecodeStatistics.BytesRead, Is.EqualTo(source.Length));
        Assert.That(fromMemory.Warnings, Is.Empty);
    }

    [Test]
    public void DecodeAnimationMemoryUsesSelectedSliceAndMatchesByteArrayPath()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.TransparentGif89a();
        ReadOnlyMemory<byte> memory = CreatePaddedSlice(source);

        GifAnimation fromBytes = decoder.DecodeAnimationBytes(source, null);
        GifAnimation fromMemory = decoder.DecodeAnimationMemory(memory, null);

        Assert.That(fromMemory.Width, Is.EqualTo(fromBytes.Width));
        Assert.That(fromMemory.Height, Is.EqualTo(fromBytes.Height));
        Assert.That(fromMemory.Frames.Length, Is.EqualTo(fromBytes.Frames.Length));
        Assert.That(fromMemory.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(fromBytes.Frames[0].Bitmap.RgbaPixels));
        Assert.That(fromMemory.TotalDurationMilliseconds, Is.EqualTo(fromBytes.TotalDurationMilliseconds));
        Assert.That(fromMemory.Warnings.Length, Is.EqualTo(fromBytes.Warnings.Length));
    }

    [Test]
    public void MemoryOperationsPreserveFailureContractAndDiagnosticLocation()
    {
        IGifDecoder decoder = new GifCodec();
        ReadOnlyMemory<byte> invalid = new ReadOnlyMemory<byte>(new byte[] { 8, 7, 6, 5, 4, 3 });

        GifInfo info = decoder.IdentifyMemory(invalid, null);
        GifException decodeException = Assert.Throws<GifException>(() => decoder.DecodeMemory(invalid, null));
        GifException animationException = Assert.Throws<GifException>(() => decoder.DecodeAnimationMemory(invalid, null));

        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(info.Error.Position.ByteOffset, Is.EqualTo(6));
        Assert.That(info.Error.Position.BlockName, Is.EqualTo("Header"));
        Assert.That(decodeException.ErrorCode, Is.EqualTo(info.Error.Code));
        Assert.That(decodeException.ByteOffset, Is.EqualTo(info.Error.Position.ByteOffset));
        Assert.That(decodeException.BlockName, Is.EqualTo(info.Error.Position.BlockName));
        Assert.That(animationException.ErrorCode, Is.EqualTo(info.Error.Code));
        Assert.That(animationException.ByteOffset, Is.EqualTo(info.Error.Position.ByteOffset));
        Assert.That(animationException.BlockName, Is.EqualTo(info.Error.Position.BlockName));
    }

    [Test]
    public void IdentifyMemoryHonorsOptionsWithoutMutatingCallerOptions()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = false;
        options.MaxFrames = 0;
        ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(GifFixtures.MinimalGif89a());

        GifInfo info = decoder.IdentifyMemory(memory, options);

        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.FrameLimitExceeded));
        Assert.That(info.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(options.MetadataOnly, Is.False);
        Assert.That(options.MaxFrames, Is.EqualTo(0));
    }

    static ReadOnlyMemory<byte> CreatePaddedSlice(byte[] source)
    {
        byte[] padded = new byte[source.Length + 5];
        padded[0] = 9;
        padded[1] = 8;
        Array.Copy(source, 0, padded, 2, source.Length);
        padded[padded.Length - 3] = 7;
        padded[padded.Length - 2] = 6;
        padded[padded.Length - 1] = 5;
        return new ReadOnlyMemory<byte>(padded, 2, source.Length);
    }
}
