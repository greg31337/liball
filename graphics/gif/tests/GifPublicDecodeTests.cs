using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicDecodeTests
{
    [Test]
    public void Decode_FromBytesReturnsDocumentWithFramesAndMetadata()
    {
        GifEncodeResult encoded = CreateEncodedAnimation();

        GifDocument document = Gif.Decode(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.Frames.Length, Is.EqualTo(2));
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(document.LoopCount, Is.EqualTo(2));
        Assert.That(document.Comments.Length, Is.EqualTo(1));
        Assert.That(document.Comments[0].Text, Is.EqualTo("decode"));
        Assert.That(document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(document.DecodeStatistics.FramesRead, Is.EqualTo(2));
    }

    [Test]
    public void Decode_FromStreamUsesDefaultOptions()
    {
        GifEncodeResult encoded = CreateEncodedAnimation();
        MemoryStream stream = new MemoryStream(encoded.Data);

        GifDocument document = Gif.Decode(stream);

        Assert.That(encoded.Success, Is.True);
        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.Frames.Length, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
    }

    [Test]
    public void Decode_HonorsFirstFrameOnlyOption()
    {
        GifEncodeResult encoded = CreateEncodedAnimation();
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;

        GifDocument document = Gif.Decode(encoded.Data, options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.DecodeStatistics.FramesRead, Is.EqualTo(1));
    }

    [Test]
    public void Decode_StreamHonorsUnknownExtensionPreservationOption()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions encodeOptions = new GifEncodeOptions();
        encodeOptions.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(0x88, new byte[] { 1, 2 }, "CustomUnknown")
        };
        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, encodeOptions);
        GifDecodeOptions decodeOptions = new GifDecodeOptions();
        decodeOptions.PreserveUnknownExtensions = false;

        GifDocument document = Gif.Decode(new MemoryStream(encoded.Data), decodeOptions);

        Assert.That(encoded.Success, Is.True);
        Assert.That(document.UnknownExtensions, Is.Empty);
        Assert.That(document.Metadata.UnknownExtensions, Is.Empty);
        Assert.That(document.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void Decode_InvalidBytesReturnEmptyDocument()
    {
        GifDocument document = Gif.Decode(new byte[] { 1, 2, 3 });

        Assert.That(document.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.DecodeStatistics.BytesRead, Is.EqualTo(0));
    }

    static GifEncodeResult CreateEncodedAnimation()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 2;
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("decode") };
        return Gif.EncodeIndexedAnimation(animation, options);
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
