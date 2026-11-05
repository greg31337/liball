using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifAnimextsLoopExtensionTests
{
    [Test]
    public void Parse_RecognizesAnimextsLoopExtension()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 49, 46, 48, 3, 1, 7, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.Identifier, Is.EqualTo("ANIMEXTS"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("1.0"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 7, 0 }));
        Assert.That(result.Application.IsLoopExtension, Is.True);
        Assert.That(result.Application.LoopCount, Is.EqualTo(7));
        Assert.That(result.BlockCount, Is.EqualTo(1));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
    }

    [Test]
    public void Read_FromStreamRecognizesAnimextsLoopPayloadSplitAcrossSubBlocks()
    {
        byte[] data = new byte[] { 0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 49, 46, 48, 1, 1, 2, 44, 1, 0 };
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
    public void Parse_RejectsMalformedAnimextsLoopPayload()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult shortPayload = parser.Parse(new byte[] { 0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 49, 46, 48, 2, 1, 0, 0 });
        GifApplicationExtensionReadResult wrongSubBlockId = parser.Parse(new byte[] { 0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 49, 46, 48, 3, 2, 0, 0, 0 });

        Assert.That(shortPayload.Success, Is.False);
        Assert.That(shortPayload.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(shortPayload.Application.Data, Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(wrongSubBlockId.Success, Is.False);
        Assert.That(wrongSubBlockId.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(wrongSubBlockId.Application.Data, Is.EqualTo(new byte[] { 2, 0, 0 }));
    }

    [Test]
    public void Parse_KeepsSimilarApplicationExtensionGeneric()
    {
        GifApplicationExtensionParser parser = new GifApplicationExtensionParser();

        GifApplicationExtensionReadResult result = parser.Parse(new byte[] { 0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 50, 46, 48, 3, 1, 2, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Application.Identifier, Is.EqualTo("ANIMEXTS"));
        Assert.That(result.Application.AuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(result.Application.Data, Is.EqualTo(new byte[] { 1, 2, 0 }));
        Assert.That(result.Application.IsLoopExtension, Is.False);
        Assert.That(result.Application.LoopCount, Is.EqualTo(-1));
    }
}
