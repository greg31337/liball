using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicEncodeWriteTests
{
    [Test]
    public void Encode_FromDocumentReturnsGifBytes()
    {
        GifDocument document = CreateEncodedDocument();

        byte[] data = Gif.Encode(document);
        GifInfo info = Gif.Identify(data);
        GifDocument parsed = Gif.Decode(data);

        Assert.That(data.Length, Is.GreaterThan(0));
        Assert.That(info.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(parsed.Comments.Length, Is.EqualTo(1));
        Assert.That(parsed.Comments[0].Text, Is.EqualTo("public encode"));
    }

    [Test]
    public void Encode_UsesOptions()
    {
        GifDocument document = CreateEncodedDocument();
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxBytes = 1;

        byte[] data = Gif.Encode(document, options);

        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Encode_NullDocumentReturnsEmptyBytes()
    {
        byte[] data = Gif.Encode(null);

        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Write_ToStreamWritesEncodedDocument()
    {
        GifDocument document = CreateEncodedDocument();
        byte[] expected = Gif.Encode(document);
        MemoryStream stream = new MemoryStream();

        Gif.Write(stream, document);

        Assert.That(stream.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void Write_StreamHonorsOptions()
    {
        GifDocument document = CreateEncodedDocument();
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxBytes = 1;
        MemoryStream stream = new MemoryStream();

        Gif.Write(stream, document, options);

        Assert.That(stream.ToArray(), Is.Empty);
    }

    [Test]
    public void Write_NullStreamDoesNotEncode()
    {
        Gif.Write(null, CreateEncodedDocument());

        Assert.Pass();
    }

    static GifDocument CreateEncodedDocument()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("public encode") };
        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);

        return encoded.Document;
    }
}
