using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicCreateTests
{
    [Test]
    public void CreateStill_FromIndexedBitmapReturnsWritableDocument()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("create still") };

        GifDocument document = Gif.CreateStill(CreateIndexedBitmap(1), options);
        byte[] data = Gif.Encode(document);
        GifInfo info = Gif.Identify(data);

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].CompressedImageData.Length, Is.GreaterThan(0));
        Assert.That(document.Comments.Length, Is.EqualTo(1));
        Assert.That(document.Comments[0].Text, Is.EqualTo("create still"));
        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void CreateStill_FromRgbaBitmapReturnsWritableDocument()
    {
        GifBitmap bitmap = new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 });

        GifDocument document = Gif.CreateStill(bitmap);
        GifAnimation animation = Gif.DecodeAnimation(Gif.Encode(document));

        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].CompressedImageData.Length, Is.GreaterThan(0));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
    }

    [Test]
    public void CreateStill_NullBitmapReturnsEmptyDocument()
    {
        GifDocument document = Gif.CreateStill(null);

        Assert.That(document.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(document.Frames, Is.Empty);
    }

    [Test]
    public void CreateAnimation_FromIndexedBitmapsAppliesAnimationOptions()
    {
        GifAnimationOptions animationOptions = new GifAnimationOptions();
        animationOptions.DelaysMilliseconds = new int[] { 50, 70 };
        animationOptions.DisposalMethods = new GifDisposalMethod[]
        {
            GifDisposalMethod.RestoreToBackground,
            GifDisposalMethod.RestoreToPrevious
        };
        animationOptions.LoopCount = 4;
        animationOptions.FrameLefts = new int[] { 0, 1 };
        animationOptions.FrameTops = new int[] { 0, 0 };
        animationOptions.UseTransparency = true;
        animationOptions.TransparentColorIndex = 0;

        GifDocument document = Gif.CreateAnimation(new GifBitmap[] { CreateIndexedBitmap(0), CreateIndexedBitmap(1) }, animationOptions, new GifEncodeOptions());
        GifInfo info = Gif.Identify(Gif.Encode(document));

        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.LoopCount, Is.EqualTo(4));
        Assert.That(document.Frames.Length, Is.EqualTo(2));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(document.Frames[0].HasTransparency, Is.True);
        Assert.That(document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[1].Left, Is.EqualTo(1));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(7));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(document.Frames[1].CompressedImageData.Length, Is.GreaterThan(0));
        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void CreateAnimation_FromRgbaBitmapsReturnsWritableDocument()
    {
        GifBitmap first = new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 });
        GifBitmap second = new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 });
        GifAnimationOptions animationOptions = new GifAnimationOptions();
        animationOptions.DelaysMilliseconds = new int[] { 30, 40 };
        animationOptions.LoopCount = 1;

        GifDocument document = Gif.CreateAnimation(new GifBitmap[] { first, second }, animationOptions, new GifEncodeOptions());
        GifAnimation animation = Gif.DecodeAnimation(Gif.Encode(document));

        Assert.That(document.Frames.Length, Is.EqualTo(2));
        Assert.That(document.Frames[0].CompressedImageData.Length, Is.GreaterThan(0));
        Assert.That(animation.LoopCount, Is.EqualTo(1));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(70));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
    }

    [Test]
    public void CreateAnimation_NullFramesReturnEmptyDocument()
    {
        GifDocument document = Gif.CreateAnimation(null, new GifAnimationOptions(), new GifEncodeOptions());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(document.Frames, Is.Empty);
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
