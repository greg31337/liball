using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifUnknownExtensionTests
{
    [Test]
    public void Read_FromMemoryParsesUnknownExtensionAndPreservesData()
    {
        byte[] data = new byte[] { 0x21, 0x99, 2, 1, 2, 1, 3, 0 };
        GifMemoryByteReader byteReader = new GifMemoryByteReader(data);
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UnknownExtension.Label, Is.EqualTo(0x99));
        Assert.That(result.UnknownExtension.BlockName, Is.EqualTo("UnknownExtension0x99"));
        Assert.That(result.UnknownExtension.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Read_FromStreamParsesEmptyUnknownExtension()
    {
        byte[] data = new byte[] { 0x21, 0x7F, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UnknownExtension.Label, Is.EqualTo(0x7F));
        Assert.That(result.UnknownExtension.BlockName, Is.EqualTo("UnknownExtension0x7F"));
        Assert.That(result.UnknownExtension.Data, Is.Empty);
        Assert.That(result.BlockCount, Is.EqualTo(0));
        Assert.That(result.TotalBytes, Is.EqualTo(0));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
    }

    [Test]
    public void Parse_RejectsInvalidIntroducer()
    {
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult result = parser.Parse(new byte[] { 0x20, 0x99, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Parse_RejectsKnownExtensionLabels()
    {
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult graphicControl = parser.Parse(new byte[] { 0x21, 0xF9, 0 });
        GifUnknownExtensionReadResult comment = parser.Parse(new byte[] { 0x21, 0xFE, 0 });
        GifUnknownExtensionReadResult plainText = parser.Parse(new byte[] { 0x21, 0x01, 0 });
        GifUnknownExtensionReadResult application = parser.Parse(new byte[] { 0x21, 0xFF, 0 });

        Assert.That(graphicControl.Success, Is.False);
        Assert.That(graphicControl.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(comment.Success, Is.False);
        Assert.That(comment.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(plainText.Success, Is.False);
        Assert.That(plainText.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(application.Success, Is.False);
        Assert.That(application.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
    }

    [Test]
    public void Read_FromMemoryRejectsShortHeaderWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 0x21 });
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Parse_PropagatesUnterminatedSubBlocksAndPreservesCompletedPayload()
    {
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        GifUnknownExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0x88, 1, 77 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(result.UnknownExtension.Label, Is.EqualTo(0x88));
        Assert.That(result.UnknownExtension.BlockName, Is.EqualTo("UnknownExtension0x88"));
        Assert.That(result.UnknownExtension.Data, Is.EqualTo(new byte[] { 77 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(1));
    }

    [Test]
    public void Helpers_ClassifyLabelsAndCreateStableBlockNames()
    {
        GifUnknownExtensionParser parser = new GifUnknownExtensionParser();

        Assert.That(parser.IsUnknownExtensionLabel(0x88), Is.True);
        Assert.That(parser.IsUnknownExtensionLabel(GifConstants.GraphicControlExtensionLabel), Is.False);
        Assert.That(parser.IsUnknownExtensionLabel(GifConstants.CommentExtensionLabel), Is.False);
        Assert.That(parser.IsUnknownExtensionLabel(GifConstants.PlainTextExtensionLabel), Is.False);
        Assert.That(parser.IsUnknownExtensionLabel(GifConstants.ApplicationExtensionLabel), Is.False);
        Assert.That(parser.CreateBlockName(0x05), Is.EqualTo("UnknownExtension0x05"));
    }

    [Test]
    public void UnknownExtensionModel_PreservesNullDataAsEmptyPayload()
    {
        GifUnknownExtension unknownExtension = new GifUnknownExtension(0x77, null, "CustomUnknown");

        Assert.That(unknownExtension.Label, Is.EqualTo(0x77));
        Assert.That(unknownExtension.Data, Is.Empty);
        Assert.That(unknownExtension.BlockName, Is.EqualTo("CustomUnknown"));
    }

    [Test]
    public void Create_WritesUnknownExtensionBytes()
    {
        GifUnknownExtension unknownExtension = new GifUnknownExtension(0x88, new byte[] { 1, 2, 3 }, "CustomUnknown");
        GifUnknownExtensionWriter writer = new GifUnknownExtensionWriter();

        GifUnknownExtensionWriteResult result = writer.Create(unknownExtension);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0x88, 3, 1, 2, 3, 0 }));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(7));
    }

    [Test]
    public void Create_RejectsKnownExtensionLabel()
    {
        GifUnknownExtension unknownExtension = new GifUnknownExtension(GifConstants.CommentExtensionLabel, new byte[] { 1 }, "NotUnknown");
        GifUnknownExtensionWriter writer = new GifUnknownExtensionWriter();

        GifUnknownExtensionWriteResult result = writer.Create(unknownExtension);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }
}
