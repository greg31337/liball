using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifInterlacedFixtureTests
{
    [Test]
    public void InterlacedGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.InterlacedGif89a();

        Assert.That(data.Length, Is.EqualTo(46));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[34], Is.EqualTo(0x40));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void InterlacedGif89aFixture_IdentifiesMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.InterlacedGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(4));
        Assert.That(info.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void InterlacedGif89aFixture_DecodesRawInterlaceMetadata()
    {
        GifDocument document = Gif.Decode(GifFixtures.InterlacedGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(4));
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
        Assert.That(document.Frames[0].Interlace, Is.True);
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 4, 65, 81, 12, 195, 113, 5 }));
    }

    [Test]
    public void InterlacedGif89aFixture_RawFrameDecoderDeinterlacesToRowMajorPixels()
    {
        GifDocument document = Gif.Decode(GifFixtures.InterlacedGif89a());

        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(document.Frames[0]);

        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0, 0, 1, 1, 2, 2, 3, 3 }));
    }

    [Test]
    public void InterlacedGif89aFixture_DecodesCoalescedRows()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.InterlacedGif89a());

        Assert.That(animation.Width, Is.EqualTo(2));
        Assert.That(animation.Height, Is.EqualTo(4));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255, 0, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255,
            0, 255, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 0, 0, 255, 255
        }));
    }

    [Test]
    public void InterlacedGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.InterlacedGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
