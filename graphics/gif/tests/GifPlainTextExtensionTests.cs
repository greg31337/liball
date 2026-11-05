using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPlainTextExtensionTests
{
    [Test]
    public void Read_FromMemoryParsesPlainTextFieldsAndData()
    {
        byte[] data = new byte[] { 0x21, 0x01, 12, 1, 0, 2, 0, 10, 0, 20, 0, 8, 9, 3, 4, 5, 104, 101, 108, 108, 111, 0 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockSize, Is.EqualTo(12));
        Assert.That(result.PlainText.TextGridLeft, Is.EqualTo(1));
        Assert.That(result.PlainText.TextGridTop, Is.EqualTo(2));
        Assert.That(result.PlainText.TextGridWidth, Is.EqualTo(10));
        Assert.That(result.PlainText.TextGridHeight, Is.EqualTo(20));
        Assert.That(result.PlainText.CharacterCellWidth, Is.EqualTo(8));
        Assert.That(result.PlainText.CharacterCellHeight, Is.EqualTo(9));
        Assert.That(result.PlainText.ForegroundColorIndex, Is.EqualTo(3));
        Assert.That(result.PlainText.BackgroundColorIndex, Is.EqualTo(4));
        Assert.That(result.PlainText.TextData, Is.EqualTo(new byte[] { 104, 101, 108, 108, 111 }));
        Assert.That(result.PlainText.Text, Is.EqualTo("hello"));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(5));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(22));
    }

    [Test]
    public void Read_FromStreamParsesMultipleTextSubBlocks()
    {
        byte[] data = new byte[] { 0x21, 0x01, 12, 0, 0, 0, 0, 2, 0, 3, 0, 6, 7, 1, 0, 2, 104, 105, 1, 33, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PlainText.TextGridWidth, Is.EqualTo(2));
        Assert.That(result.PlainText.TextGridHeight, Is.EqualTo(3));
        Assert.That(result.PlainText.TextData, Is.EqualTo(new byte[] { 104, 105, 33 }));
        Assert.That(result.PlainText.Text, Is.EqualTo("hi!"));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(21));
    }

    [Test]
    public void Parse_AcceptsEmptyPlainTextData()
    {
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0x01, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.PlainText.TextData, Is.Empty);
        Assert.That(result.PlainText.Text, Is.EqualTo(string.Empty));
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
    }

    [Test]
    public void Parse_RejectsInvalidIntroducerOrLabel()
    {
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult invalidIntroducer = parser.Parse(new byte[] { 0x20, 0x01, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        GifPlainTextExtensionReadResult invalidLabel = parser.Parse(new byte[] { 0x21, 0xFE, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        Assert.That(invalidIntroducer.Success, Is.False);
        Assert.That(invalidIntroducer.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(invalidLabel.Success, Is.False);
        Assert.That(invalidLabel.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Parse_RejectsInvalidPlainTextBlockSize()
    {
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0x01, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Read_FromMemoryRejectsShortHeaderWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x21, 0x01, 12 });
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Parse_PropagatesUnterminatedSubBlocksAndPreservesCompletedPayload()
    {
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0x01, 12, 1, 0, 2, 0, 3, 0, 4, 0, 5, 6, 7, 8, 1, 97 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(result.PlainText.TextGridLeft, Is.EqualTo(1));
        Assert.That(result.PlainText.TextData, Is.EqualTo(new byte[] { 97 }));
        Assert.That(result.PlainText.Text, Is.EqualTo("a"));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(1));
    }

    [Test]
    public void Create_WritesRawDataPlainTextExtension()
    {
        GifPlainTextExtension plainText = CreateSamplePlainText();
        plainText.TextData = new byte[] { 1, 2, 3 };
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Create(plainText);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0x01, 12, 1, 0, 2, 0, 10, 0, 20, 0, 8, 9, 3, 4, 3, 1, 2, 3, 0 }));
        Assert.That(result.BlockSize, Is.EqualTo(12));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(20));
    }

    [Test]
    public void Create_WritesTextWhenRawDataIsEmpty()
    {
        GifPlainTextExtension plainText = CreateSamplePlainText();
        plainText.Text = "hi";
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Create(plainText);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0x01, 12, 1, 0, 2, 0, 10, 0, 20, 0, 8, 9, 3, 4, 2, 104, 105, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(2));
        Assert.That(result.BytesWritten, Is.EqualTo(19));
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

        GifPlainTextExtension plainText = CreateSamplePlainText();
        plainText.TextData = payload;
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Create(plainText);

        Assert.That(result.Success, Is.True);
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(256));
        Assert.That(result.BytesWritten, Is.EqualTo(274));
        Assert.That(result.EncodedData.Length, Is.EqualTo(274));
        Assert.That(result.EncodedData[15], Is.EqualTo(255));
        Assert.That(result.EncodedData[271], Is.EqualTo(1));
        Assert.That(result.EncodedData[272], Is.EqualTo(255));
        Assert.That(result.EncodedData[273], Is.EqualTo(0));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifPlainTextExtension plainText = CreateSamplePlainText();
        plainText.Text = "plain";
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();
        GifPlainTextExtensionParser parser = new GifPlainTextExtensionParser();

        GifPlainTextExtensionWriteResult writeResult = writer.Create(plainText);
        GifPlainTextExtensionReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.PlainText.TextGridLeft, Is.EqualTo(1));
        Assert.That(readResult.PlainText.TextGridTop, Is.EqualTo(2));
        Assert.That(readResult.PlainText.TextGridWidth, Is.EqualTo(10));
        Assert.That(readResult.PlainText.TextGridHeight, Is.EqualTo(20));
        Assert.That(readResult.PlainText.CharacterCellWidth, Is.EqualTo(8));
        Assert.That(readResult.PlainText.CharacterCellHeight, Is.EqualTo(9));
        Assert.That(readResult.PlainText.ForegroundColorIndex, Is.EqualTo(3));
        Assert.That(readResult.PlainText.BackgroundColorIndex, Is.EqualTo(4));
        Assert.That(readResult.PlainText.TextData, Is.EqualTo(new byte[] { 112, 108, 97, 105, 110 }));
        Assert.That(readResult.PlainText.Text, Is.EqualTo("plain"));
    }

    [Test]
    public void Create_RejectsUnencodableGridValues()
    {
        GifPlainTextExtension negative = CreateSamplePlainText();
        GifPlainTextExtension tooLarge = CreateSamplePlainText();
        negative.TextGridLeft = -1;
        tooLarge.TextGridHeight = 65536;
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        Assert.That(writer.Create(negative).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(writer.Create(tooLarge).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Write(null!, new GifPlainTextExtension());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureForMissingPlainText()
    {
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Write(new GifStreamByteWriter(new MemoryStream()), null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[32], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();

        GifPlainTextExtensionWriteResult result = writer.Write(byteWriter, CreateSamplePlainText());

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }

    static GifPlainTextExtension CreateSamplePlainText()
    {
        GifPlainTextExtension plainText = new GifPlainTextExtension();
        plainText.TextGridLeft = 1;
        plainText.TextGridTop = 2;
        plainText.TextGridWidth = 10;
        plainText.TextGridHeight = 20;
        plainText.CharacterCellWidth = 8;
        plainText.CharacterCellHeight = 9;
        plainText.ForegroundColorIndex = 3;
        plainText.BackgroundColorIndex = 4;
        return plainText;
    }
}
