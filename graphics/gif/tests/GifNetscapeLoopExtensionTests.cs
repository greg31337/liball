using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifNetscapeLoopExtensionTests
{
    [Test]
    public void Parse_RecognizesNetscapeLoopExtension()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 5, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.Identifier, Is.EqualTo("NETSCAPE"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 5, 0 }));
        Assert.That(result.Application.IsLoopExtension, Is.True);
        Assert.That(result.Application.LoopCount, Is.EqualTo(5));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
    }

    [Test]
    public void Read_FromStreamRecognizesNetscapeLoopPayloadSplitAcrossSubBlocks()
    {
        byte[] data = new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 2, 1, 44, 1, 1, 0 };
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(data));
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.IsLoopExtension, Is.True);
        Assert.That(result.Application.LoopCount, Is.EqualTo(300));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 44, 1 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(20));
    }

    [Test]
    public void Parse_RejectsMalformedNetscapeLoopPayload()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult shortPayload = parser.Parse(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 2, 1, 0, 0 });
        GifApplicationExtensionReadResult wrongSubBlockId = parser.Parse(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 2, 0, 0, 0 });

        Assert.That(shortPayload.Success, Is.False);
        Assert.That(shortPayload.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(shortPayload.Application.Data, Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(wrongSubBlockId.Success, Is.False);
        Assert.That(wrongSubBlockId.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(wrongSubBlockId.Application.Data, Is.EqualTo(new byte[] { 2, 0, 0 }));
    }

    [Test]
    public void CreateNetscapeLoopExtension_WritesCanonicalForeverLoopBytes()
    {
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.CreateNetscapeLoopExtension(0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 }));
        Assert.That(result.BlockSize, Is.EqualTo(11));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(result.BytesWritten, Is.EqualTo(19));
    }

    [Test]
    public void Create_WritesCanonicalNetscapeLoopWhenApplicationHasLoopCount()
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.LoopCount = 65535;
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult result = writer.Create(application);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 255, 255, 0 }));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
    }

    [Test]
    public void CreateNetscapeLoopExtension_RoundTripsThroughParser()
    {
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionWriteResult writeResult = writer.CreateNetscapeLoopExtension(42);
        GifApplicationExtensionReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.Application.IsLoopExtension, Is.True);
        Assert.That(readResult.Application.LoopCount, Is.EqualTo(42));
        Assert.That(readResult.Application.Data, Is.EqualTo(new byte[] { 1, 42, 0 }));
    }

    [Test]
    public void CreateNetscapeLoopExtension_RejectsInvalidLoopCount()
    {
        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();

        GifApplicationExtensionWriteResult negative = writer.CreateNetscapeLoopExtension(-1);
        GifApplicationExtensionWriteResult tooLarge = writer.CreateNetscapeLoopExtension(65536);

        Assert.That(negative.Success, Is.False);
        Assert.That(negative.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(tooLarge.Success, Is.False);
        Assert.That(tooLarge.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }
}
