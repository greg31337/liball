using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPlainTextFixtureTests
{
    [Test]
    public void PlainTextGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.PlainTextGif89a();

        Assert.That(data.Length, Is.EqualTo(57));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[19], Is.EqualTo(0x21));
        Assert.That(data[20], Is.EqualTo(0x01));
        Assert.That(data[21], Is.EqualTo(12));
        Assert.That(data[26], Is.EqualTo(1));
        Assert.That(data[28], Is.EqualTo(1));
        Assert.That(data[30], Is.EqualTo(8));
        Assert.That(data[31], Is.EqualTo(8));
        Assert.That(data[32], Is.EqualTo(1));
        Assert.That(data[34], Is.EqualTo(5));
        Assert.That(data[35], Is.EqualTo(112));
        Assert.That(data[39], Is.EqualTo(110));
        Assert.That(data[40], Is.EqualTo(0));
        Assert.That(data[41], Is.EqualTo(0x2C));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void PlainTextGif89aFixture_IdentifiesFrameMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.PlainTextGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void PlainTextGif89aFixture_DecodesPlainTextExtension()
    {
        GifDocument document = Gif.Decode(GifFixtures.PlainTextGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].TextGridLeft, Is.EqualTo(0));
        Assert.That(document.PlainTextExtensions[0].TextGridTop, Is.EqualTo(0));
        Assert.That(document.PlainTextExtensions[0].TextGridWidth, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].TextGridHeight, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].CharacterCellWidth, Is.EqualTo(8));
        Assert.That(document.PlainTextExtensions[0].CharacterCellHeight, Is.EqualTo(8));
        Assert.That(document.PlainTextExtensions[0].ForegroundColorIndex, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(document.PlainTextExtensions[0].TextData, Is.EqualTo(new byte[] { 112, 108, 97, 105, 110 }));
        Assert.That(document.PlainTextExtensions[0].Text, Is.EqualTo("plain"));
        Assert.That(document.Metadata.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void PlainTextGif89aFixture_BlockReaderCountsPlainTextExtension()
    {
        GifBlockReadResult result = new GifBlockReader().Read(GifFixtures.PlainTextGif89a());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.Document.PlainTextExtensions.Length, Is.EqualTo(1));
    }

    [Test]
    public void PlainTextGif89aFixture_DecodesCoalescedImage()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.PlainTextGif89a());

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    [Test]
    public void PlainTextGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.PlainTextGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
