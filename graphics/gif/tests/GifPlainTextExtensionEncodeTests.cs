using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPlainTextExtensionEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesPlainTextExtensionsFromOptions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.PlainTextExtensions = new GifPlainTextExtension[]
        {
            CreatePlainText("hello")
        };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(encoded.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(encoded.Document.Metadata.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.Metadata.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextGridLeft, Is.EqualTo(1));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextGridTop, Is.EqualTo(2));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextGridWidth, Is.EqualTo(10));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextGridHeight, Is.EqualTo(20));
        Assert.That(parsed.Document.PlainTextExtensions[0].CharacterCellWidth, Is.EqualTo(8));
        Assert.That(parsed.Document.PlainTextExtensions[0].CharacterCellHeight, Is.EqualTo(9));
        Assert.That(parsed.Document.PlainTextExtensions[0].ForegroundColorIndex, Is.EqualTo(1));
        Assert.That(parsed.Document.PlainTextExtensions[0].BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(parsed.Document.PlainTextExtensions[0].Text, Is.EqualTo("hello"));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextData, Is.EqualTo(new byte[] { 104, 101, 108, 108, 111 }));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(1));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0x01, 12 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesPlainTextBeforeFrames()
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
        GifPlainTextExtension plainText = CreatePlainText(string.Empty);
        plainText.TextData = new byte[] { 1, 2, 3 };
        GifEncodeOptions options = new GifEncodeOptions();
        options.PlainTextExtensions = new GifPlainTextExtension[] { plainText };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.PlainTextExtensions[0].TextData, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0x01, 12 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesPlainTextThroughQuantization()
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
        options.PlainTextExtensions = new GifPlainTextExtension[] { CreatePlainText("rgba") };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.PlainTextExtensions[0].Text, Is.EqualTo("rgba"));
    }

    [Test]
    public void EncodeIndexedStill_RejectsInvalidPlainTextExtensions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions missing = new GifEncodeOptions();
        GifEncodeOptions invalidGrid = new GifEncodeOptions();
        GifPlainTextExtension tooLarge = CreatePlainText("bad");
        tooLarge.TextGridWidth = 65536;
        missing.PlainTextExtensions = new GifPlainTextExtension[] { null! };
        invalidGrid.PlainTextExtensions = new GifPlainTextExtension[] { tooLarge };

        GifEncodeResult missingResult = Gif.EncodeIndexedStill(bitmap, missing);
        GifEncodeResult invalidGridResult = Gif.EncodeIndexedStill(bitmap, invalidGrid);

        Assert.That(missingResult.Success, Is.False);
        Assert.That(missingResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(missingResult.Error.Position.BlockName, Is.EqualTo("PlainTextExtension"));
        Assert.That(missingResult.Data, Is.Empty);
        Assert.That(invalidGridResult.Success, Is.False);
        Assert.That(invalidGridResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(invalidGridResult.Error.Position.BlockName, Is.EqualTo("PlainTextExtension"));
        Assert.That(invalidGridResult.Data, Is.Empty);
    }

    static GifPlainTextExtension CreatePlainText(string text)
    {
        GifPlainTextExtension plainText = new GifPlainTextExtension();
        plainText.TextGridLeft = 1;
        plainText.TextGridTop = 2;
        plainText.TextGridWidth = 10;
        plainText.TextGridHeight = 20;
        plainText.CharacterCellWidth = 8;
        plainText.CharacterCellHeight = 9;
        plainText.ForegroundColorIndex = 1;
        plainText.BackgroundColorIndex = 0;
        plainText.Text = text;
        return plainText;
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
