using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifFrameDisposalEncodeTests
{
    [Test]
    public void EncodeIndexedAnimation_WritesDisposalMethodsAsGraphicControlExtensions()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette), GifDisposalMethod.RestoreToBackground),
            CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette), GifDisposalMethod.RestoreToPrevious)
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(parsed.Document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(parsed.Document.Frames[0].GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(parsed.Document.Frames[1].GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesDisposalMethodsThroughQuantization()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }), GifDisposalMethod.DoNotDispose),
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }), GifDisposalMethod.RestoreToPrevious)
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(parsed.Document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
    }

    [Test]
    public void EncodeIndexedAnimation_RejectsInvalidDisposalMethod()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifAnimationFrame frame = CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette), GifDisposalMethod.Unspecified);
        frame.DisposalMethod = (GifDisposalMethod)8;
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[] { frame };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(encoded.Data, Is.Empty);
    }

    [Test]
    public void ValidateDisposalMethod_AcceptsReservedGifRange()
    {
        GifFrameDisposalEncoder encoder = new GifFrameDisposalEncoder();

        GifErrorDiagnostic reserved = encoder.ValidateDisposalMethod(GifDisposalMethod.Reserved7, 0);
        GifErrorDiagnostic invalid = encoder.ValidateDisposalMethod((GifDisposalMethod)(-1), 0);

        Assert.That(reserved.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(invalid.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    static GifAnimationFrame CreateIndexedFrame(GifBitmap bitmap, GifDisposalMethod disposalMethod)
    {
        return CreateFrame(bitmap, disposalMethod);
    }

    static GifAnimationFrame CreateRgbaFrame(GifBitmap bitmap, GifDisposalMethod disposalMethod)
    {
        return CreateFrame(bitmap, disposalMethod);
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, GifDisposalMethod disposalMethod)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.DisposalMethod = disposalMethod;
        return frame;
    }
}
