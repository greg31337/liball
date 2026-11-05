using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifApplicationExtensionEncodeTests
{
    [Test]
    public void EncodeIndexedStill_WritesApplicationExtensionsFromOptions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions options = new GifEncodeOptions();
        options.ApplicationExtensions = new GifApplicationExtension[]
        {
            CreateApplication("TESTAPP1", "001", new byte[] { 9, 8, 7 }),
            CreateApplication("EMPTYAPP", "000", new byte[0])
        };

        GifEncodeResult encoded = Gif.EncodeIndexedStill(bitmap, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(encoded.Document.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(encoded.Document.Metadata.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.Metadata.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.ApplicationExtensions[0].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(parsed.Document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("001"));
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(parsed.Document.ApplicationExtensions[1].Identifier, Is.EqualTo("EMPTYAPP"));
        Assert.That(parsed.Document.ApplicationExtensions[1].AuthenticationCode, Is.EqualTo("000"));
        Assert.That(parsed.Document.ApplicationExtensions[1].Data, Is.Empty);
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
        Assert.That(FindSequence(encoded.Data, new byte[] { 0x21, 0xFF, 11 }), Is.LessThan(FindSequence(encoded.Data, new byte[] { 0x2C })));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesApplicationWithLoopWithoutDuplicatingLoopExtension()
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
        options.LoopCount = 4;
        options.ApplicationExtensions = new GifApplicationExtension[]
        {
            CreateApplication("TESTAPP1", "001", new byte[] { 3, 2, 1 })
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.LoopCount, Is.EqualTo(4));
        Assert.That(parsed.Document.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(parsed.Document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(parsed.Document.ApplicationExtensions[0].LoopCount, Is.EqualTo(4));
        Assert.That(parsed.Document.ApplicationExtensions[1].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(parsed.Document.ApplicationExtensions[1].Data, Is.EqualTo(new byte[] { 3, 2, 1 }));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesApplicationExtensionsThroughQuantization()
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
        options.ApplicationExtensions = new GifApplicationExtension[]
        {
            CreateApplication("RGBAAPP1", "A01", new byte[] { 5, 6 })
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation, options);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(parsed.Document.ApplicationExtensions[0].Identifier, Is.EqualTo("RGBAAPP1"));
        Assert.That(parsed.Document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("A01"));
        Assert.That(parsed.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 5, 6 }));
    }

    [Test]
    public void EncodeIndexedStill_RejectsInvalidApplicationExtensions()
    {
        GifPalette palette = CreatePalette();
        GifBitmap bitmap = GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette);
        GifEncodeOptions missing = new GifEncodeOptions();
        GifEncodeOptions longIdentifier = new GifEncodeOptions();
        missing.ApplicationExtensions = new GifApplicationExtension[] { null! };
        longIdentifier.ApplicationExtensions = new GifApplicationExtension[]
        {
            CreateApplication("TOO-LONG-ID", "001", new byte[] { 1 })
        };

        GifEncodeResult missingResult = Gif.EncodeIndexedStill(bitmap, missing);
        GifEncodeResult longIdentifierResult = Gif.EncodeIndexedStill(bitmap, longIdentifier);

        Assert.That(missingResult.Success, Is.False);
        Assert.That(missingResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(missingResult.Error.Position.BlockName, Is.EqualTo("ApplicationExtension"));
        Assert.That(missingResult.Data, Is.Empty);
        Assert.That(longIdentifierResult.Success, Is.False);
        Assert.That(longIdentifierResult.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(longIdentifierResult.Error.Position.BlockName, Is.EqualTo("ApplicationExtension"));
        Assert.That(longIdentifierResult.Data, Is.Empty);
    }

    static GifApplicationExtension CreateApplication(string identifier, string authenticationCode, byte[] data)
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = identifier;
        application.AuthenticationCode = authenticationCode;
        application.Data = data;
        return application;
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
