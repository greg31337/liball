using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifMinimalGif89aFixtureTests
{
    [Test]
    public void MinimalGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.MinimalGif89a();

        Assert.That(data.Length, Is.EqualTo(35));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void MinimalGif89aFixture_IdentifiesMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.MinimalGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasTransparency, Is.False);
        Assert.That(info.HasComments, Is.False);
    }

    [Test]
    public void MinimalGif89aFixture_DecodesRawDocument()
    {
        GifDocument document = Gif.Decode(GifFixtures.MinimalGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(document.GlobalColorTable.Colors[0].Red, Is.EqualTo(0));
        Assert.That(document.GlobalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 68, 1 }));
        Assert.That(document.Frames[0].IndexedPixels, Is.Empty);
    }

    [Test]
    public void MinimalGif89aFixture_DecodesCoalescedBlackPixel()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.MinimalGif89a());

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    [Test]
    public void MinimalGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.MinimalGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
