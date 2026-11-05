using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifGraphicControlExtensionParserTests
{
    [Test]
    public void Read_FromMemoryParsesGraphicControlFields()
    {
        byte[] data = new byte[] { 0x21, 0xF9, 4, 0x0F, 0x34, 0x12, 7, 0 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PackedFields, Is.EqualTo(0x0F));
        Assert.That(result.GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(result.GraphicControl.UserInputFlag, Is.True);
        Assert.That(result.GraphicControl.HasTransparency, Is.True);
        Assert.That(result.GraphicControl.DelayHundredths, Is.EqualTo(0x1234));
        Assert.That(result.GraphicControl.TransparentColorIndex, Is.EqualTo(7));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Read_FromStreamParsesReservedDisposalAndPreservesTransparentIndexByte()
    {
        byte[] data = new byte[] { 0x21, 0xF9, 4, 0x1C, 0, 0, 9, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.Reserved7));
        Assert.That(result.GraphicControl.UserInputFlag, Is.False);
        Assert.That(result.GraphicControl.HasTransparency, Is.False);
        Assert.That(result.GraphicControl.TransparentColorIndex, Is.EqualTo(9));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Parse_RejectsInvalidIntroducerOrLabel()
    {
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult invalidIntroducer = parser.Parse(new byte[] { 0x20, 0xF9, 4, 0, 0, 0, 0, 0 });
        GifGraphicControlExtensionReadResult invalidLabel = parser.Parse(new byte[] { 0x21, 0xFE, 4, 0, 0, 0, 0, 0 });

        Assert.That(invalidIntroducer.Success, Is.False);
        Assert.That(invalidIntroducer.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(invalidLabel.Success, Is.False);
        Assert.That(invalidLabel.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Parse_RejectsInvalidBlockSizeOrMissingTerminator()
    {
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult invalidSize = parser.Parse(new byte[] { 0x21, 0xF9, 3, 0, 0, 0, 0, 0 });
        GifGraphicControlExtensionReadResult invalidTerminator = parser.Parse(new byte[] { 0x21, 0xF9, 4, 0, 0, 0, 0, 1 });

        Assert.That(invalidSize.Success, Is.False);
        Assert.That(invalidSize.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(invalidTerminator.Success, Is.False);
        Assert.That(invalidTerminator.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Read_FromMemoryRejectsShortExtensionWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x21, 0xF9, 4 });
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromStreamRejectsShortExtensionAfterPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { 0x21, 0xF9, 4 }));
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }

    [Test]
    public void ParseExtension_RejectsNullExtension()
    {
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionReadResult result = parser.ParseExtension(null!, 5);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(5));
    }
}
