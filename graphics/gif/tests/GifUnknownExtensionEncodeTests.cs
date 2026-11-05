using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifUnknownExtensionEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesUnknownExtensionsFromOptions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(0x88, new byte[] { 1, 2, 3 }, "CustomUnknown"),
            new GifUnknownExtension(0x7F, new byte[0], "EmptyUnknown")
        };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(encoded.Document.UnknownExtensions.Length, Is.EqualTo(2));
        Assert.That(encoded.Document.Metadata.UnknownExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.UnknownExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Metadata.UnknownExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.UnknownExtensions[0].Label, Is.EqualTo(0x88));
        Assert.That(parsed.Document.UnknownExtensions[0].BlockName, Is.EqualTo("UnknownExtension0x88"));
        Assert.That(parsed.Document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(parsed.Document.UnknownExtensions[1].Label, Is.EqualTo(0x7F));
        Assert.That(parsed.Document.UnknownExtensions[1].Data, Is.Empty);
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0x88 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesUnknownExtensionsBeforeFrames()
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
        options.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(0x99, new byte[] { 42 }, "CustomUnknown")
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.UnknownExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.UnknownExtensions[0].Label, Is.EqualTo(0x99));
        Assert.That(parsed.Document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 42 }));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0x99 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesUnknownExtensionsThroughQuantization()
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
        options.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(0x9A, new byte[] { 5, 6 }, "CustomUnknown")
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.UnknownExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.UnknownExtensions[0].Label, Is.EqualTo(0x9A));
        Assert.That(parsed.Document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 5, 6 }));
    }

    [Test]
    public void EncodeIndexedStill_RejectsInvalidUnknownExtensions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions missing = new GifEncodeOptions();
        GifEncodeOptions knownLabel = new GifEncodeOptions();
        missing.UnknownExtensions = new GifUnknownExtension[] { null! };
        knownLabel.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(GifConstants.CommentExtensionLabel, new byte[] { 1 }, "NotUnknown")
        };

        GifEncodeResult missingResult = Gif.EncodeIndexedStill(bitmap, missing);
        GifEncodeResult knownLabelResult = Gif.EncodeIndexedStill(bitmap, knownLabel);

        Assert.That(missingResult.Success, Is.False);
        Assert.That(missingResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(missingResult.Error.Position.BlockName, Is.EqualTo("UnknownExtension"));
        Assert.That(missingResult.Data, Is.Empty);
        Assert.That(knownLabelResult.Success, Is.False);
        Assert.That(knownLabelResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(knownLabelResult.Error.Position.BlockName, Is.EqualTo("UnknownExtension"));
        Assert.That(knownLabelResult.Data, Is.Empty);
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
