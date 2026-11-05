using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRawDecodeGoldenTests
{
    [Test]
    public void DecodeFrame_SinglePixelFixturesMatchRawGoldens()
    {
        AssertSinglePixelRawGolden(GifFixtures.MinimalGif87a());
        AssertSinglePixelRawGolden(GifFixtures.MinimalGif89a());
        AssertSinglePixelRawGolden(GifFixtures.TransparentGif89a());
        AssertSinglePixelRawGolden(GifFixtures.NetscapeLoopGif89a());
        AssertSinglePixelRawGolden(GifFixtures.CommentGif89a());
        AssertSinglePixelRawGolden(GifFixtures.ApplicationExtensionGif89a());
        AssertSinglePixelRawGolden(GifFixtures.PlainTextGif89a());
    }

    [Test]
    public void DecodeFrame_LocalColorTableFixtureMatchesRawGolden()
    {
        GifDocument document = Gif.Decode(GifFixtures.LocalColorTableGif89a());
        GifFrame frame = document.Frames[0];

        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(decoded.Success, Is.True);
        Assert.That(frame.Width, Is.EqualTo(1));
        Assert.That(frame.Height, Is.EqualTo(1));
        Assert.That(frame.HasLocalColorTable, Is.True);
        Assert.That(frame.LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(frame.LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(frame.CompressedImageData, Is.EqualTo(new byte[] { 76, 1 }));
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(frame.IndexedPixels, Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void DecodeFrame_InterlacedFixtureMatchesRowMajorRawGolden()
    {
        GifDocument document = Gif.Decode(GifFixtures.InterlacedGif89a());
        GifFrame frame = document.Frames[0];

        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(decoded.Success, Is.True);
        Assert.That(frame.Width, Is.EqualTo(2));
        Assert.That(frame.Height, Is.EqualTo(4));
        Assert.That(frame.Interlace, Is.True);
        Assert.That(frame.LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(frame.CompressedImageData, Is.EqualTo(new byte[] { 4, 65, 81, 12, 195, 113, 5 }));
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3
        }));
        Assert.That(frame.IndexedPixels, Is.EqualTo(decoded.IndexedPixels));
    }

    [Test]
    public void DecodeFrame_AnimatedDisposalFixtureMatchesRawGoldens()
    {
        GifDocument document = Gif.Decode(GifFixtures.AnimatedDisposalMethod3Gif89a());
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult first = decoder.DecodeFrame(document.Frames[0]);
        GifRawFrameDecodeResult second = decoder.DecodeFrame(document.Frames[1]);
        GifRawFrameDecodeResult third = decoder.DecodeFrame(document.Frames[2]);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(third.Success, Is.True);
        Assert.That(document.Frames[0].Left, Is.EqualTo(0));
        Assert.That(document.Frames[1].Left, Is.EqualTo(0));
        Assert.That(document.Frames[2].Left, Is.EqualTo(1));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(document.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 76, 1 }));
        Assert.That(document.Frames[1].CompressedImageData, Is.EqualTo(new byte[] { 84, 1 }));
        Assert.That(document.Frames[2].CompressedImageData, Is.EqualTo(new byte[] { 92, 1 }));
        Assert.That(first.IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(second.IndexedPixels, Is.EqualTo(new byte[] { 2 }));
        Assert.That(third.IndexedPixels, Is.EqualTo(new byte[] { 3 }));
    }

    static void AssertSinglePixelRawGolden(byte[] data)
    {
        GifDocument document = Gif.Decode(data);
        GifFrame frame = document.Frames[0];

        GifRawFrameDecodeResult decoded = new GifRawFrameDecoder().DecodeFrame(frame);

        Assert.That(decoded.Success, Is.True);
        Assert.That(frame.Width, Is.EqualTo(1));
        Assert.That(frame.Height, Is.EqualTo(1));
        Assert.That(frame.LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(frame.CompressedImageData, Is.EqualTo(new byte[] { 68, 1 }));
        Assert.That(decoded.IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(frame.IndexedPixels, Is.EqualTo(new byte[] { 0 }));
    }
}
