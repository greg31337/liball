using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifTrailerTests
{
    [Test]
    public void Read_FromMemoryAcceptsTrailerByte()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x3B });
        GifTrailerValidator validator = new GifTrailerValidator();

        GifTrailerReadResult result = validator.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.TrailerByte, Is.EqualTo(0x3B));
        Assert.That(result.HasExtraData, Is.False);
        Assert.That(byteReader.ByteOffset, Is.EqualTo(1));
    }

    [Test]
    public void Parse_ReportsExtraDataAfterTrailerAsWarning()
    {
        GifTrailerValidator validator = new GifTrailerValidator();

        GifTrailerReadResult result = validator.Parse(new byte[] { 0x3B, 1, 2, 3 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasExtraData, Is.True);
        Assert.That(result.ExtraDataLength, Is.EqualTo(3));
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
        Assert.That(result.Warning.Position.ByteOffset, Is.EqualTo(1));
    }

    [Test]
    public void Read_ReportsMissingTrailer()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[0]);
        GifTrailerValidator validator = new GifTrailerValidator();

        GifTrailerReadResult result = validator.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Trailer"));
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.MissingTrailer));
    }

    [Test]
    public void ValidateTrailerByte_RejectsNonTrailerByte()
    {
        GifTrailerValidator validator = new GifTrailerValidator();

        GifTrailerReadResult result = validator.ValidateTrailerByte(0x2C, 12);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(12));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Trailer"));
    }

    [Test]
    public void Create_WritesTrailerByte()
    {
        GifTrailerWriter writer = new GifTrailerWriter();

        GifTrailerWriteResult result = writer.Create();

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x3B }));
        Assert.That(result.BytesWritten, Is.EqualTo(1));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifTrailerWriter writer = new GifTrailerWriter();

        GifTrailerWriteResult result = writer.Write(null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[1], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifTrailerWriter writer = new GifTrailerWriter();

        GifTrailerWriteResult result = writer.Write(byteWriter);

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }

    [Test]
    public void BlockReader_ReportsExtraDataAfterTrailerWarning()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x3B, 1, 2
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.Document.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Document.Warnings[0].Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
        Assert.That(result.Document.DecodeStatistics.WarningCount, Is.EqualTo(1));
    }

    [Test]
    public void BlockReader_ReportsMissingTrailerWarningOnFailure()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Document.Warnings.Length, Is.EqualTo(1));
        Assert.That(result.Document.Warnings[0].Code, Is.EqualTo(GifWarningCode.MissingTrailer));
        Assert.That(result.Document.DecodeStatistics.WarningCount, Is.EqualTo(1));
    }
}
