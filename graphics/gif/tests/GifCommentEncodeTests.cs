using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCommentEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesCommentsFromOptions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifCommentExtension raw = new GifCommentExtension(null);
        raw.Data = new byte[] { 1, 2, 3 };
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[]
        {
            new GifCommentExtension("hello"),
            raw
        };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(encoded.Document.Comments.Length, Is.EqualTo(2));
        Assert.That(encoded.Document.Metadata.Comments.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Comments.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Metadata.Comments.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Comments[0].Text, Is.EqualTo("hello"));
        Assert.That(parsed.Document.Comments[0].Data, Is.EqualTo(new byte[] { 104, 101, 108, 108, 111 }));
        Assert.That(parsed.Document.Comments[1].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0xFE }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesCommentsBeforeFrames()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("anim") };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Comments.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.Comments[0].Text, Is.EqualTo("anim"));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0xFE }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesCommentsThroughQuantization()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 })),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("rgba") };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Comments.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.Comments[0].Text, Is.EqualTo("rgba"));
    }

    [Test]
    public void EncodeIndexedStill_RejectsMissingComment()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { null! };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(encoded.Error.Position.BlockName, Is.EqualTo("CommentExtension"));
        Assert.That(encoded.Data, Is.Empty);
    }

    static int FindSequence(byte[] data, byte[] sequence)
    {
        int index = 0;
        while (index <= data.Length - sequence.Length)
        {
            bool match = true;
            int sequenceIndex = 0;
            while (sequenceIndex < sequence.Length)
            {
                if (data[index + sequenceIndex] != sequence[sequenceIndex])
                {
                    match = false;
                }

                sequenceIndex = sequenceIndex + 1;
            }

            if (match)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
