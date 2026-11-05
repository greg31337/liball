using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLoopCountEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesForeverLoopFromOptions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 0;

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.LoopCount, Is.EqualTo(0));
        Assert.That(parsed.Document.Metadata.LoopCount, Is.EqualTo(0));
        Assert.That(parsed.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.ApplicationExtensions[0].Identifier, Is.EqualTo("NETSCAPE"));
        Assert.That(parsed.Document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(parsed.Document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 1, 0, 0 }));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0xFF, 11 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesLoopCountFromAnimation()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.LoopCount = 5;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette)),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette))
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(encoded.Document.LoopCount, Is.EqualTo(5));
        Assert.That(encoded.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.LoopCount, Is.EqualTo(5));
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 1, 5, 0 }));
    }

    [Test]
    public void EncodeIndexedAnimation_OptionsLoopCountOverridesAnimationLoopCount()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.LoopCount = 2;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette))
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 7;

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LoopCount, Is.EqualTo(7));
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 1, 7, 0 }));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesLoopCountThroughQuantization()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.LoopCount = 3;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 })),
            CreateFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }))
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.LoopCount, Is.EqualTo(3));
        Assert.That(parsed.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 1, 3, 0 }));
    }

    [Test]
    public void EncodeIndexedAnimation_RejectsInvalidLoopCount()
    {
        GifPalette palette = CreatePalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette))
        };
        GifEncodeOptions negative = new GifEncodeOptions();
        GifEncodeOptions tooLarge = new GifEncodeOptions();
        negative.LoopCount = -2;
        tooLarge.LoopCount = 65536;

        GifEncodeResult negativeResult = Gif.EncodeIndexedAnimation(animation, negative);
        GifEncodeResult tooLargeResult = Gif.EncodeIndexedAnimation(animation, tooLarge);

        Assert.That(negativeResult.Success, Is.False);
        Assert.That(negativeResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(negativeResult.Error.Position.BlockName, Is.EqualTo("ApplicationExtension"));
        Assert.That(negativeResult.Data, Is.Empty);
        Assert.That(tooLargeResult.Success, Is.False);
        Assert.That(tooLargeResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(tooLargeResult.Error.Position.BlockName, Is.EqualTo("ApplicationExtension"));
        Assert.That(tooLargeResult.Data, Is.Empty);
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
