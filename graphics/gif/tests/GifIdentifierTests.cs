using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifIdentifierTests
{
    [Test]
    public void Identify_FromBytesPopulatesMetadataWithoutPixelDecode()
    {
        byte[] data = CreateIdentifiableGif();
        GifIdentifier identifier = new GifIdentifier();

        GifInfo info = identifier.IdentifyBytes(data);

        Assert.That(info.Success, Is.True);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(2));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasTransparency, Is.True);
        Assert.That(info.LoopCount, Is.EqualTo(5));
        Assert.That(info.HasComments, Is.True);
        Assert.That(info.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "NETSCAPE" }));
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(12));
        Assert.That(info.Warnings, Is.Empty);
    }

    [Test]
    public void Identify_FromStreamReportsTrailerWarnings()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x3B, 1, 2
        };
        GifIdentifier identifier = new GifIdentifier();

        GifInfo info = identifier.IdentifyStream(new MemoryStream(data));

        Assert.That(info.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(0));
        Assert.That(info.Warnings.Length, Is.EqualTo(1));
        Assert.That(info.Warnings[0].Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
    }

    [Test]
    public void Identify_InvalidHeaderReturnsDiagnostic()
    {
        GifIdentifier identifier = new GifIdentifier();

        GifInfo info = identifier.IdentifyBytes(new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(info.FrameCount, Is.EqualTo(0));
    }

    [Test]
    public void Identify_DoesNotMutateCallerOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = false;
        options.PreserveUnknownExtensions = false;
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3 };
        GifIdentifier identifier = new GifIdentifier();

        GifInfo info = identifier.IdentifyBytesWithOptions(CreateIdentifiableGif(), options);

        Assert.That(info.Success, Is.True);
        Assert.That(options.MetadataOnly, Is.False);
        Assert.That(options.PreserveUnknownExtensions, Is.False);
        Assert.That(options.DefaultGlobalColorTable, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void BlockReader_MetadataOnlySkipsCompressedImagePayloads()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = true;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateIdentifiableGif(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(3));
        Assert.That(result.ImageDataBytes, Is.EqualTo(6));
        Assert.That(result.Document.Frames[0].LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.Empty);
        Assert.That(result.Document.Frames[1].CompressedImageData, Is.Empty);
        Assert.That(result.Document.DecodeStatistics.CompressedBytesRead, Is.EqualTo(6));
    }

    [Test]
    public void ImageDataReader_ReadMetadataOnlyCountsSubBlocksWithoutKeepingPayload()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 8, 2, 1, 2, 1, 3, 0 });
        GifImageDataReader reader = new GifImageDataReader();

        GifImageDataReadResult result = reader.ReadMetadataOnly(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LzwMinimumCodeSize, Is.EqualTo(8));
        Assert.That(result.CompressedData, Is.Empty);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void ImageDataReader_ReadMetadataOnlyFromStreamCountsSubBlocksWithoutKeepingPayload()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 8, 2, 1, 2, 1, 3, 0 }));
        GifImageDataReader reader = new GifImageDataReader();

        GifImageDataReadResult result = reader.ReadMetadataOnly(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LzwMinimumCodeSize, Is.EqualTo(8));
        Assert.That(result.CompressedData, Is.Empty);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }

    static byte[] CreateIdentifiableGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 2, 79, 75, 0,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 5, 0, 0,
            0x21, 0xF9, 4, 1, 10, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 3, 1, 2, 3, 2, 4, 5, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 6, 0,
            0x3B
        };
    }
}
