using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicValidateTests
{
    [Test]
    public void Validate_DocumentReturnsNoneForWritableDocument()
    {
        GifDocument document = Gif.CreateStill(CreateIndexedBitmap(0));

        GifErrorDiagnostic diagnostic = Gif.Validate(document);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Validate_DocumentHonorsEncodeOptions()
    {
        GifDocument document = Gif.CreateStill(CreateIndexedBitmap(0));
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxBytes = 1;

        GifErrorDiagnostic diagnostic = Gif.Validate(document, options);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Validate_NullDocumentReturnsDiagnostic()
    {
        GifErrorDiagnostic diagnostic = Gif.Validate((GifDocument?)null);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.MissingLogicalScreenDescriptor));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo("Validate"));
    }

    [Test]
    public void Validate_IndexedBitmapReturnsNoneForValidBitmap()
    {
        GifErrorDiagnostic diagnostic = Gif.Validate(CreateIndexedBitmap(1));

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Validate_IndexedBitmapReturnsPaletteIndexDiagnostic()
    {
        GifErrorDiagnostic diagnostic = Gif.Validate(CreateIndexedBitmap(9));

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
    }

    [Test]
    public void Validate_RgbaBitmapReturnsNoneForValidBitmap()
    {
        GifBitmap bitmap = new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 });

        GifErrorDiagnostic diagnostic = Gif.Validate(bitmap);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Validate_NullBitmapReturnsDiagnostic()
    {
        GifErrorDiagnostic diagnostic = Gif.Validate((GifBitmap?)null);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo("Validate"));
    }

    [Test]
    public void Validate_IndexedAnimationReturnsNoneForValidAnimation()
    {
        GifAnimation animation = CreateIndexedAnimation(0, 1);

        GifErrorDiagnostic diagnostic = Gif.Validate(animation);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Validate_IndexedAnimationReturnsFrameDiagnostic()
    {
        GifAnimation animation = CreateIndexedAnimation(0, 9);

        GifErrorDiagnostic diagnostic = Gif.Validate(animation);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.InvalidPaletteIndex));
        Assert.That(diagnostic.Position.FrameIndex, Is.EqualTo(1));
    }

    [Test]
    public void Validate_RgbaAnimationReturnsNoneForValidAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }), 30, 0),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }), 40, 1)
        };

        GifErrorDiagnostic diagnostic = Gif.Validate(animation);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
    }

    [Test]
    public void Validate_NullAnimationReturnsDiagnostic()
    {
        GifErrorDiagnostic diagnostic = Gif.Validate((GifAnimation?)null);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
    }

    static GifAnimation CreateIndexedAnimation(int firstIndex, int secondIndex)
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(CreateIndexedBitmap(firstIndex), 30, 0),
            CreateFrame(CreateIndexedBitmap(secondIndex), 40, 1)
        };
        return animation;
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.DelayMilliseconds = delayMilliseconds;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static GifBitmap CreateIndexedBitmap(int index)
    {
        return GifBitmap.CreateIndexed(1, 1, new byte[] { (byte)index }, CreatePalette());
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }
}
