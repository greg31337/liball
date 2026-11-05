using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifHeaderWriterTests
{
    [Test]
    public void Create_WritesGif87aHeaderBytes()
    {
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Create(GifVersion.Gif87a);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(result.Header, Is.EqualTo("GIF87a"));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'7', (byte)'a' }));
    }

    [Test]
    public void Create_WritesGif89aHeaderBytes()
    {
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Create(GifVersion.Gif89a);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(result.Header, Is.EqualTo("GIF89a"));
        Assert.That(result.BytesWritten, Is.EqualTo(6));
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }));
    }

    [Test]
    public void Write_WritesHeaderToProvidedStreamAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Write(byteWriter, GifVersion.Gif89a);

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(6));
    }

    [Test]
    public void Create_RejectsUnknownVersionWithoutBytes()
    {
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Create(GifVersion.Unknown);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(result.Header, Is.EqualTo(string.Empty));
        Assert.That(result.EncodedData, Is.Empty);
        Assert.That(result.BytesWritten, Is.EqualTo(0));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnsupportedVersion));
    }

    [Test]
    public void CreateHeaderBytes_RejectsUnsupportedEnumValue()
    {
        GifHeaderWriter writer = new GifHeaderWriter();

        byte[] bytes = writer.CreateHeaderBytes((GifVersion)1234);

        Assert.That(bytes, Is.Empty);
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Write(null!, GifVersion.Gif87a);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Header, Is.EqualTo("GIF87a"));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[6], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Write(byteWriter, GifVersion.Gif89a);

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
