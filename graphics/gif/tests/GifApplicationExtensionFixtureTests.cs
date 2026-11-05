using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifApplicationExtensionFixtureTests
{
    [Test]
    public void ApplicationExtensionGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.ApplicationExtensionGif89a();

        Assert.That(data.Length, Is.EqualTo(54));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[19], Is.EqualTo(0x21));
        Assert.That(data[20], Is.EqualTo(0xFF));
        Assert.That(data[21], Is.EqualTo(11));
        Assert.That(data[22], Is.EqualTo(84));
        Assert.That(data[29], Is.EqualTo(49));
        Assert.That(data[30], Is.EqualTo(48));
        Assert.That(data[31], Is.EqualTo(48));
        Assert.That(data[32], Is.EqualTo(49));
        Assert.That(data[33], Is.EqualTo(3));
        Assert.That(data[34], Is.EqualTo(9));
        Assert.That(data[35], Is.EqualTo(8));
        Assert.That(data[36], Is.EqualTo(7));
        Assert.That(data[37], Is.EqualTo(0));
        Assert.That(data[38], Is.EqualTo(0x2C));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void ApplicationExtensionGif89aFixture_IdentifiesApplicationMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.ApplicationExtensionGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.LoopCount, Is.EqualTo(-1));
        Assert.That(info.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "TESTAPP1" }));
    }

    [Test]
    public void ApplicationExtensionGif89aFixture_DecodesApplicationExtension()
    {
        GifDocument document = Gif.Decode(GifFixtures.ApplicationExtensionGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LoopCount, Is.EqualTo(-1));
        Assert.That(document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("001"));
        Assert.That(document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(document.ApplicationExtensions[0].IsLoopExtension, Is.False);
        Assert.That(document.ApplicationExtensions[0].LoopCount, Is.EqualTo(-1));
        Assert.That(document.Metadata.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(document.Metadata.ApplicationExtensions[0].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void ApplicationExtensionGif89aFixture_DecodesCoalescedImage()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.ApplicationExtensionGif89a());

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.LoopCount, Is.EqualTo(-1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    [Test]
    public void ApplicationExtensionGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.ApplicationExtensionGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
