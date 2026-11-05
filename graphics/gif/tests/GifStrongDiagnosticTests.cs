using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifStrongDiagnosticTests
{
    [Test]
    public void StrictDecode_ReportsMalformedPackedFieldForTableMetadataWithoutTable()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = GifStrictness.Strict;

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeBytes(CreateImageGif(0x01, true), options));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MalformedPackedField));
        Assert.That(exception.FrameIndex, Is.EqualTo(0));
        Assert.That(exception.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    [Test]
    public void StrictDecode_ReportsReservedFieldValueForGraphicControlAndImageBits()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = GifStrictness.Strict;

        GifException graphicControl = Assert.Throws<GifException>(() => new GifCodec().DecodeBytes(CreateReservedGraphicControlGif(), options));
        GifException image = Assert.Throws<GifException>(() => new GifCodec().DecodeBytes(CreateImageGif(0x08, true), options));

        Assert.That(graphicControl.ErrorCode, Is.EqualTo(GifErrorCode.ReservedFieldValue));
        Assert.That(graphicControl.BlockName, Is.EqualTo("GraphicControlExtension"));
        Assert.That(image.ErrorCode, Is.EqualTo(GifErrorCode.ReservedFieldValue));
        Assert.That(image.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    [Test]
    public void NormalDecode_PreservesCompatibilityForReservedAndIgnoredPackedBits()
    {
        GifCodec codec = new GifCodec();

        GifDocument malformed = codec.DecodeBytes(CreateImageGif(0x01, true), null);
        GifDocument reserved = codec.DecodeBytes(CreateImageGif(0x08, true), null);

        Assert.That(malformed.Frames, Has.Length.EqualTo(1));
        Assert.That(reserved.Frames, Has.Length.EqualTo(1));
    }

    [Test]
    public void StrictDecode_ReportsMissingTrailerInsteadOfGenericEndOfStream()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = GifStrictness.Strict;

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeBytes(CreateImageGif(0, false), options));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MissingTrailer));
        Assert.That(exception.BlockName, Is.EqualTo("Trailer"));
    }

    [Test]
    public void MissingDestinationReportsInvalidOperationState()
    {
        IGifEncoder encoder = new GifCodec();

        GifException exception = Assert.Throws<GifException>(() => encoder.Write(null, new GifDocument(), null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidOperationState));
        Assert.That(exception.BlockName, Is.EqualTo("DestinationStream"));
    }

    [Test]
    public void BlockWriterFailureReportsDestinationWriteFailureInsteadOfByteLimit()
    {
        MemoryStream stream = new MemoryStream(new byte[0], false);
        GifStreamByteWriter destination = new GifStreamByteWriter(stream);

        GifHeaderWriteResult result = new GifHeaderWriter().Write(destination, GifVersion.Gif89a);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DestinationWriteFailed));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Header"));
    }

    static byte[] CreateReservedGraphicControlGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 0x20, 0, 0, 0, 0,
            0x3B
        };
    }

    static byte[] CreateImageGif(byte packedFields, bool includeTrailer)
    {
        byte[] trailer = includeTrailer ? new byte[] { 0x3B } : new byte[0];
        byte[] result = new byte[34 + trailer.Length];
        byte[] prefix = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, packedFields,
            2, 2, 68, 1, 0
        };
        System.Array.Copy(prefix, 0, result, 0, prefix.Length);
        if (includeTrailer)
        {
            result[result.Length - 1] = 0x3B;
        }

        return result;
    }
}
