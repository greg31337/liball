using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifHeaderParserTests
{
    [Test]
    public void Read_FromMemoryParsesGif87aHeader()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'7', (byte)'a', 0 });
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(result.Header, Is.EqualTo("GIF87a"));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Read_FromStreamParsesGif89aHeader()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0 }));
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(result.Header, Is.EqualTo("GIF89a"));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Parse_RejectsInvalidSignature()
    {
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Parse(new byte[] { (byte)'B', (byte)'A', (byte)'D', (byte)'8', (byte)'9', (byte)'a' });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(result.Header, Is.EqualTo("BAD89a"));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Header"));
    }

    [Test]
    public void Parse_RejectsUnsupportedGifVersion()
    {
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Parse(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'9', (byte)'0', (byte)'a' });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(result.Header, Is.EqualTo("GIF90a"));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnsupportedVersion));
    }

    [Test]
    public void Read_FromMemoryRejectsShortHeaderWithoutAdvancing()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { (byte)'G', (byte)'I', (byte)'F' });
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(0));
        Assert.That(byteReader.EndOfData, Is.True);
    }

    [Test]
    public void Read_FromStreamRejectsShortHeaderAfterPartialConsumption()
    {
        GifStreamByteReader byteReader = new GifStreamByteReader(new MemoryStream(new byte[] { (byte)'G', (byte)'I', (byte)'F' }));
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.Read(byteReader);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(3));
        Assert.That(byteReader.EndOfStream, Is.True);
    }

    [Test]
    public void ParseHeader_RejectsNullHeader()
    {
        GifHeaderParser parser = new GifHeaderParser();

        GifHeaderReadResult result = parser.ParseHeader(null!, 12);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(12));
    }
}
