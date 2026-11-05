using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLocalColorTableFixtureTests
{
    [Test]
    public void LocalColorTableGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.LocalColorTableGif89a();

        Assert.That(data.Length, Is.EqualTo(35));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[10], Is.EqualTo(0));
        Assert.That(data[22], Is.EqualTo(0x80));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void LocalColorTableGif89aFixture_IdentifiesMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.LocalColorTableGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void LocalColorTableGif89aFixture_DecodesRawLocalColorTable()
    {
        GifDocument document = Gif.Decode(GifFixtures.LocalColorTableGif89a());

        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(document.Frames[0].LocalColorTable.Colors[0].Red, Is.EqualTo(255));
        Assert.That(document.Frames[0].LocalColorTable.Colors[0].Blue, Is.EqualTo(0));
        Assert.That(document.Frames[0].LocalColorTable.Colors[1].Red, Is.EqualTo(0));
        Assert.That(document.Frames[0].LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 76, 1 }));
    }

    [Test]
    public void LocalColorTableGif89aFixture_UsesLocalColorTableAsActivePalette()
    {
        GifDocument document = Gif.Decode(GifFixtures.LocalColorTableGif89a());

        GifActiveColorTableResult active = new GifActiveColorTableResolver().Resolve(document, 0);

        Assert.That(active.Success, Is.True);
        Assert.That(active.UsedLocalColorTable, Is.True);
        Assert.That(active.UsedGlobalColorTable, Is.False);
        Assert.That(active.Palette.Colors[1].Blue, Is.EqualTo(255));
    }

    [Test]
    public void LocalColorTableGif89aFixture_DecodesCoalescedBluePixel()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.LocalColorTableGif89a(), options);

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
    }

    [Test]
    public void LocalColorTableGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.LocalColorTableGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
