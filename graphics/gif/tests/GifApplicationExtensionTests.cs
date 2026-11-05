using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifApplicationExtensionTests
{
    [Test]
    public void Read_FromMemoryParsesApplicationHeaderAndData()
    {
        byte[] data = new byte[] { 0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 3, 1, 2, 3, 0 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockSize, Is.EqualTo(11));
        Assert.That(result.Application.Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("001"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(19));
    }

    [Test]
    public void Read_FromStreamParsesMultipleDataSubBlocks()
    {
        byte[] data = new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 2, 1, 0, 1, 5, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.Identifier, Is.EqualTo("NETSCAPE"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 0, 5 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(20));
    }

    [Test]
    public void Parse_AcceptsEmptyApplicationData()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 11, 69, 77, 80, 84, 89, 65, 80, 80, 48, 48, 48, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.Identifier, Is.EqualTo("EMPTYAPP"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("000"));
        Assert.That(result.Application.Data, Is.Empty);
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Parse_RejectsInvalidIntroducerOrLabel()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult invalidIntroducer = parser.Parse(new byte[] { 0x20, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 0 });
        GifApplicationExtensionReadResult invalidLabel = parser.Parse(new byte[] { 0x21, 0xFE, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 0 });

        Assert.That(invalidIntroducer.Success, Is.False);
        Assert.That(invalidIntroducer.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(invalidLabel.Success, Is.False);
        Assert.That(invalidLabel.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Parse_RejectsInvalidApplicationBlockSize()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 10, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Read_FromMemoryRejectsShortHeaderWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x21, 0xFF, 11 });
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Parse_PropagatesUnterminatedSubBlocksAndPreservesCompletedPayload()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 1, 77 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(result.Application.Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("001"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 77 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(1));
    }

    [Test]
    public void Create_WritesApplicationExtensionBytes()
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "TESTAPP1";
        application.AuthenticationCode = "001";
        application.Data = new byte[] { 1, 2, 3 };
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Create(application);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockSize, Is.EqualTo(11));
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 3, 1, 2, 3, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(19));
    }

    [Test]
    public void Create_PadsShortIdentifierAndAuthenticationWithZeros()
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "ABC";
        application.AuthenticationCode = "1";
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Create(application);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFF, 11, 65, 66, 67, 0, 0, 0, 0, 0, 49, 0, 0, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Create_ChunksPayloadAtMaximumSubBlockSize()
    {
        byte[] payload = new byte[256];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)(index & 255);
            index = index + 1;
        }

        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "TESTAPP1";
        application.AuthenticationCode = "001";
        application.Data = payload;
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Create(application);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(256));
        Assert.That(result.BytesWritten, Is.EqualTo(273));
        Assert.That(result.EncodedData.Length, Is.EqualTo(273));
        Assert.That(result.EncodedData[14], Is.EqualTo(255));
        Assert.That(result.EncodedData[270], Is.EqualTo(1));
        Assert.That(result.EncodedData[271], Is.EqualTo(255));
        Assert.That(result.EncodedData[272], Is.EqualTo(0));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "TESTAPP1";
        application.AuthenticationCode = "001";
        application.Data = new byte[] { 9, 8, 7 };
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionWriteResult writeResult = writer.Create(application);
        GifApplicationExtensionReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Application.Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(readResult.Application.AuthenticationCode, Is.EqualTo("001"));
        Assert.That(readResult.Application.Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    [Test]
    public void Create_RejectsUnencodableFixedHeaderText()
    {
        GifApplicationExtension longIdentifier = new GifApplicationExtension();
        GifApplicationExtension longAuthentication = new GifApplicationExtension();
        longIdentifier.Identifier = "TOO-LONG-ID";
        longAuthentication.Identifier = "TESTAPP1";
        longAuthentication.AuthenticationCode = "LONG";
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        Assert.That(writer.Create(longIdentifier).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(writer.Create(longAuthentication).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Write(null!, new GifApplicationExtension());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureForMissingApplication()
    {
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Write(new GifStreamByteWriter(new MemoryStream()), null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[32], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Write(byteWriter, new GifApplicationExtension());

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
