using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCommentFixtureTests
{
    [Test]
    public void CommentGif89aFixture_HasExpectedBytes()
    {
        byte[] data = GifFixtures.CommentGif89a();

        Assert.That(data.Length, Is.EqualTo(46));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[19], Is.EqualTo(0x21));
        Assert.That(data[20], Is.EqualTo(0xFE));
        Assert.That(data[21], Is.EqualTo(7));
        Assert.That(data[22], Is.EqualTo(102));
        Assert.That(data[28], Is.EqualTo(101));
        Assert.That(data[29], Is.EqualTo(0));
        Assert.That(data[30], Is.EqualTo(0x2C));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    [Test]
    public void CommentGif89aFixture_IdentifiesCommentMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.CommentGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.HasComments, Is.True);
    }

    [Test]
    public void CommentGif89aFixture_DecodesComment()
    {
        GifDocument document = Gif.Decode(GifFixtures.CommentGif89a());

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.Comments.Length, Is.EqualTo(1));
        Assert.That(document.Comments[0].Text, Is.EqualTo("fixture"));
        Assert.That(document.Comments[0].Data, Is.EqualTo(new byte[] { 102, 105, 120, 116, 117, 114, 101 }));
        Assert.That(document.Metadata.Comments.Length, Is.EqualTo(1));
        Assert.That(document.Metadata.Comments[0].Text, Is.EqualTo("fixture"));
        Assert.That(document.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void CommentGif89aFixture_DecodesCoalescedImage()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.CommentGif89a());

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    [Test]
    public void CommentGif89aFixture_RoundTripsExactly()
    {
        byte[] data = GifFixtures.CommentGif89a();
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
