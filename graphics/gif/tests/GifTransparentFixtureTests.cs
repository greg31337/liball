using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifTransparentFixtureTests
{
    [Test]
    public void TransparentGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.TransparentGif89a();

        Assert.That(data.Length, Is.EqualTo(43));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[19], Is.EqualTo(0x21));
        Assert.That(data[20], Is.EqualTo(0xF9));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void TransparentGif89aFixture_IdentifiesTransparency()
    {
        GifInfo info = Gif.Identify(GifFixtures.TransparentGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.HasTransparency, Is.True);
    }

    [Test]
    public void TransparentGif89aFixture_DecodesGraphicControlMetadata()
    {
        GifDocument document = Gif.Decode(GifFixtures.TransparentGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].HasTransparency, Is.True);
        Assert.That(document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[0].GraphicControl.HasTransparency, Is.True);
        Assert.That(document.Frames[0].GraphicControl.TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[0].GraphicControl.DelayHundredths, Is.EqualTo(0));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 68, 1 }));
    }

    [Test]
    public void TransparentGif89aFixture_DecodesCoalescedTransparentPixel()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.TransparentGif89a(), options);

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void TransparentGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.TransparentGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
