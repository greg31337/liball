using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCommentExtensionTests
{
    [Test]
    public void Read_FromMemoryParsesSingleSubBlock()
    {
        byte[] data = new byte[] { 0x21, 0xFE, 3, 97, 98, 99, 0 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Comment.Data, Is.EqualTo(new byte[] { 97, 98, 99 }));
        Assert.That(result.Comment.Text, Is.EqualTo("abc"));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }

    [Test]
    public void Read_FromStreamParsesMultipleSubBlocks()
    {
        byte[] data = new byte[] { 0x21, 0xFE, 2, 104, 105, 1, 33, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Comment.Data, Is.EqualTo(new byte[] { 104, 105, 33 }));
        Assert.That(result.Comment.Text, Is.EqualTo("hi!"));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Parse_AcceptsEmptyComment()
    {
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFE, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Comment.Data, Is.Empty);
        Assert.That(result.Comment.Text, Is.EqualTo(string.Empty));
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Parse_RejectsInvalidIntroducerOrLabel()
    {
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult invalidIntroducer = parser.Parse(new byte[] { 0x20, 0xFE, 0 });
        GifCommentExtensionReadResult invalidLabel = parser.Parse(new byte[] { 0x21, 0xF9, 0 });

        Assert.That(invalidIntroducer.Success, Is.False);
        Assert.That(invalidIntroducer.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(invalidLabel.Success, Is.False);
        Assert.That(invalidLabel.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Read_FromMemoryRejectsShortHeaderWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x21 });
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Parse_PropagatesUnterminatedSubBlocksAndPreservesCompletedPayload()
    {
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFE, 1, 97 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(result.Comment.Data, Is.EqualTo(new byte[] { 97 }));
        Assert.That(result.Comment.Text, Is.EqualTo("a"));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(1));
    }

    [Test]
    public void Create_WritesRawDataComment()
    {
        GifCommentExtension comment = new GifCommentExtension(null);
        comment.Data = new byte[] { 1, 2, 3 };
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Create(comment);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFE, 3, 1, 2, 3, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
    }

    [Test]
    public void Create_WritesTextWhenRawDataIsEmpty()
    {
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Create(new GifCommentExtension("hi"));

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFE, 2, 104, 105, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
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

        GifCommentExtension comment = new GifCommentExtension(null);
        comment.Data = payload;
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Create(comment);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(256));
        Assert.That(result.BytesWritten, Is.EqualTo(261));
        Assert.That(result.EncodedData.Length, Is.EqualTo(261));
        Assert.That(result.EncodedData[0], Is.EqualTo(0x21));
        Assert.That(result.EncodedData[1], Is.EqualTo(0xFE));
        Assert.That(result.EncodedData[2], Is.EqualTo(255));
        Assert.That(result.EncodedData[258], Is.EqualTo(1));
        Assert.That(result.EncodedData[259], Is.EqualTo(255));
        Assert.That(result.EncodedData[260], Is.EqualTo(0));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifCommentExtension comment = new GifCommentExtension("hello");
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();
        GifCommentExtensionParser parser = new GifCommentExtensionParser();

        GifCommentExtensionWriteResult writeResult = writer.Create(comment);
        GifCommentExtensionReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Comment.Data, Is.EqualTo(new byte[] { 104, 101, 108, 108, 111 }));
        Assert.That(readResult.Comment.Text, Is.EqualTo("hello"));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Write(null!, new GifCommentExtension("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureForMissingComment()
    {
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Write(new GifStreamByteWriter(new MemoryStream()), null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[8], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifCommentExtensionWriter writer = new GifCommentExtensionWriter();

        GifCommentExtensionWriteResult result = writer.Write(byteWriter, new GifCommentExtension("x"));

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
