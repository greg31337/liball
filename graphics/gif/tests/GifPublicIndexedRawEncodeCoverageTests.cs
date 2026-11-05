using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicIndexedRawEncodeCoverageTests
{
    [Test]
    public void PublicIndexedStillWritesFullCanvasGlobalPaletteAndStatistics()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifPalette palette = CreateFourColorPalette();
        byte[] pixels = new byte[] { 0, 1, 2, 3, 2, 1 };
        GifBitmap bitmap = codec.CreateIndexedBitmap(3, 2, pixels, palette);

        GifEncodeResult encoded = encoder.EncodeIndexedStill(bitmap, null, false, -1);
        GifDocument decoded = decoder.DecodeFrameBytes(encoded.Data, 0, null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(decoded.LogicalScreen.Width, Is.EqualTo(3));
        Assert.That(decoded.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(decoded.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(decoded.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(decoded.Frames, Has.Length.EqualTo(1));
        Assert.That(decoded.Frames[0].Left, Is.EqualTo(0));
        Assert.That(decoded.Frames[0].Top, Is.EqualTo(0));
        Assert.That(decoded.Frames[0].Width, Is.EqualTo(3));
        Assert.That(decoded.Frames[0].Height, Is.EqualTo(2));
        Assert.That(decoded.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(decoded.Frames[0].IndexedPixels, Is.EqualTo(pixels));
        Assert.That(encoded.Document.EncodeStatistics.BytesWritten, Is.EqualTo(encoded.Data.Length));
        Assert.That(encoded.Document.EncodeStatistics.CompressedBytesWritten, Is.GreaterThan(0));
        Assert.That(encoded.Document.EncodeStatistics.IndexedPixelCount, Is.EqualTo(6));
        Assert.That(encoded.Document.EncodeStatistics.FramesWritten, Is.EqualTo(1));
        Assert.That(encoded.Document.EncodeStatistics.ExtensionCount, Is.EqualTo(0));
        Assert.That(encoded.Document.EncodeStatistics.WarningCount, Is.EqualTo(0));
    }

    [Test]
    public void PublicIndexedAnimationPreservesFullAndOffsetInterlacedFramesWithAutomaticPalettes()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifPalette firstPalette = CreateFourColorPalette();
        GifPalette secondPalette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 255, 255),
            new GifColor(255, 255, 0),
            new GifColor(255, 0, 255),
            new GifColor(0, 255, 255)
        });
        GifAnimation animation = new GifAnimation();
        animation.Width = 3;
        animation.Height = 4;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(codec.CreateIndexedBitmap(3, 4, new byte[]
            {
                0, 1, 2,
                1, 2, 3,
                2, 3, 0,
                3, 0, 1
            }, firstPalette), 0, 0, 10),
            CreateFrame(codec.CreateIndexedBitmap(1, 2, new byte[] { 3, 1 }, secondPalette), 2, 1, 20)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ColorTableMode = GifColorTableMode.Auto;
        options.Interlace = true;

        GifEncodeResult encoded = encoder.EncodeIndexedAnimation(animation, options);
        GifDocument decoded = decoder.DecodeBytes(encoded.Data, null);
        GifDocument decodedFirst = decoder.DecodeFrameBytes(encoded.Data, 0, null);
        GifDocument decodedSecond = decoder.DecodeFrameBytes(encoded.Data, 1, null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(decoded.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(decoded.Frames, Has.Length.EqualTo(2));
        Assert.That(decoded.Frames[0].Left, Is.EqualTo(0));
        Assert.That(decoded.Frames[0].Top, Is.EqualTo(0));
        Assert.That(decoded.Frames[0].Width, Is.EqualTo(3));
        Assert.That(decoded.Frames[0].Height, Is.EqualTo(4));
        Assert.That(decoded.Frames[1].Left, Is.EqualTo(2));
        Assert.That(decoded.Frames[1].Top, Is.EqualTo(1));
        Assert.That(decoded.Frames[1].Width, Is.EqualTo(1));
        Assert.That(decoded.Frames[1].Height, Is.EqualTo(2));
        Assert.That(decoded.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(decoded.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(decoded.Frames[0].Interlace, Is.True);
        Assert.That(decoded.Frames[1].Interlace, Is.True);
        Assert.That(decodedFirst.Frames[0].IndexedPixels, Is.EqualTo(animation.Frames[0].Bitmap.IndexedPixels));
        Assert.That(decodedSecond.Frames[1].IndexedPixels, Is.EqualTo(animation.Frames[1].Bitmap.IndexedPixels));
    }

    [Test]
    public void PublicIndexedEncodingHonorsExplicitGlobalAndLocalPaletteModes()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifPalette palette = CreateFourColorPalette();
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(codec.CreateIndexedBitmap(1, 1, new byte[] { 1 }, palette), 0, 0, 0),
            CreateFrame(codec.CreateIndexedBitmap(1, 1, new byte[] { 2 }, palette), 0, 0, 0)
        };
        GifEncodeOptions globalOptions = new GifEncodeOptions();
        globalOptions.ColorTableMode = GifColorTableMode.Global;
        GifEncodeOptions localOptions = new GifEncodeOptions();
        localOptions.ColorTableMode = GifColorTableMode.Local;

        GifEncodeResult global = encoder.EncodeIndexedAnimation(animation, globalOptions);
        GifEncodeResult local = encoder.EncodeIndexedAnimation(animation, localOptions);
        GifDocument globalDocument = decoder.DecodeBytes(global.Data, null);
        GifDocument localDocument = decoder.DecodeBytes(local.Data, null);

        Assert.That(global.Success, Is.True);
        Assert.That(globalDocument.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(globalDocument.Frames[0].HasLocalColorTable, Is.False);
        Assert.That(globalDocument.Frames[1].HasLocalColorTable, Is.False);
        Assert.That(local.Success, Is.True);
        Assert.That(localDocument.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(localDocument.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(localDocument.Frames[1].HasLocalColorTable, Is.True);
    }

    [Test]
    public void PublicRawDocumentEncodePreservesExactFrameAndMetadataBytes()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        byte[][] sources = new byte[][]
        {
            GifFixtures.MinimalGif87a(),
            GifFixtures.TransparentGif89a(),
            GifFixtures.LocalColorTableGif89a(),
            GifFixtures.InterlacedGif89a(),
            GifFixtures.CommentGif89a(),
            GifFixtures.ApplicationExtensionGif89a(),
            GifFixtures.PlainTextGif89a(),
            GifFixtures.AnimatedDisposalMethod3Gif89a()
        };

        int index = 0;
        while (index < sources.Length)
        {
            GifDocument document = decoder.DecodeBytes(sources[index], null);
            byte[][] compressedFrames = CopyCompressedFrames(document);

            byte[] encoded = encoder.Encode(document, null);

            Assert.That(encoded, Is.EqualTo(sources[index]));
            AssertCompressedFrames(document, compressedFrames);
            index = index + 1;
        }
    }

    [Test]
    public void PublicIndexedEncodingHonorsForcedAndAutomaticallySelectedVersions()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifBitmap bitmap = codec.CreateIndexedBitmap(1, 1, new byte[] { 0 }, CreateBlackWhitePalette());
        GifEncodeOptions forced87Options = new GifEncodeOptions();
        forced87Options.Version = GifVersion.Gif87a;
        GifEncodeOptions forced89Options = new GifEncodeOptions();
        forced89Options.Version = GifVersion.Gif89a;
        GifEncodeOptions automatic89Options = new GifEncodeOptions();
        automatic89Options.Comments = new GifCommentExtension[] { new GifCommentExtension("automatic 89a") };

        GifEncodeResult forced87 = encoder.EncodeIndexedStill(bitmap, forced87Options, false, -1);
        GifEncodeResult forced89 = encoder.EncodeIndexedStill(bitmap, forced89Options, false, -1);
        GifEncodeResult automatic87 = encoder.EncodeIndexedStill(bitmap, null, false, -1);
        GifEncodeResult automatic89 = encoder.EncodeIndexedStill(bitmap, automatic89Options, false, -1);

        Assert.That(decoder.DecodeBytes(forced87.Data, null).Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(decoder.DecodeBytes(forced89.Data, null).Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(decoder.DecodeBytes(automatic87.Data, null).Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(decoder.DecodeBytes(automatic89.Data, null).Version, Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void PublicIndexedEncodingWritesEveryMetadataExtensionAndCountsStatistics()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifBitmap bitmap = codec.CreateIndexedBitmap(1, 1, new byte[] { 1 }, CreateBlackWhitePalette());
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "TESTAPP1";
        application.AuthenticationCode = "001";
        application.Data = new byte[] { 9, 8, 7 };
        GifPlainTextExtension plainText = new GifPlainTextExtension();
        plainText.TextGridWidth = 1;
        plainText.TextGridHeight = 1;
        plainText.CharacterCellWidth = 8;
        plainText.CharacterCellHeight = 8;
        plainText.ForegroundColorIndex = 1;
        plainText.BackgroundColorIndex = 0;
        plainText.Text = "text";
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 3;
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("comment") };
        options.ApplicationExtensions = new GifApplicationExtension[] { application };
        options.PlainTextExtensions = new GifPlainTextExtension[] { plainText };
        options.UnknownExtensions = new GifUnknownExtension[]
        {
            new GifUnknownExtension(0x99, new byte[] { 6, 5, 4 }, "UnknownExtension0x99")
        };

        GifEncodeResult encoded = encoder.EncodeIndexedStill(bitmap, options, false, -1);
        GifDocument decoded = decoder.DecodeBytes(encoded.Data, null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(decoded.LoopCount, Is.EqualTo(3));
        Assert.That(decoded.Comments, Has.Length.EqualTo(1));
        Assert.That(decoded.Comments[0].Text, Is.EqualTo("comment"));
        Assert.That(decoded.ApplicationExtensions, Has.Length.EqualTo(2));
        Assert.That(decoded.ApplicationExtensions[1].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(decoded.PlainTextExtensions, Has.Length.EqualTo(1));
        Assert.That(decoded.PlainTextExtensions[0].Text, Is.EqualTo("text"));
        Assert.That(decoded.UnknownExtensions, Has.Length.EqualTo(1));
        Assert.That(decoded.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 6, 5, 4 }));
        Assert.That(decoded.DecodeStatistics.ExtensionCount, Is.EqualTo(5));
        Assert.That(encoded.Document.EncodeStatistics.ExtensionCount, Is.EqualTo(5));
        Assert.That(encoded.Document.EncodeStatistics.WarningCount, Is.EqualTo(encoded.Document.Warnings.Length));
    }

    [Test]
    public void PublicIndexedEncodingSupportsBothLzwClearPolicies()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        byte[] pixels = CreateCompressionPixels();
        GifBitmap bitmap = codec.CreateIndexedBitmap(32, 16, pixels, CreateFourColorPalette());
        GifEncodeOptions fullDictionaryOptions = new GifEncodeOptions();
        fullDictionaryOptions.LzwClearPolicy = GifLzwClearPolicy.ClearOnFullDictionary;
        GifEncodeOptions earlyClearOptions = new GifEncodeOptions();
        earlyClearOptions.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;

        GifEncodeResult fullDictionary = encoder.EncodeIndexedStill(bitmap, fullDictionaryOptions, false, -1);
        GifEncodeResult earlyClear = encoder.EncodeIndexedStill(bitmap, earlyClearOptions, false, -1);
        GifDocument fullDictionaryDocument = decoder.DecodeFrameBytes(fullDictionary.Data, 0, null);
        GifDocument earlyClearDocument = decoder.DecodeFrameBytes(earlyClear.Data, 0, null);

        Assert.That(fullDictionary.Success, Is.True);
        Assert.That(earlyClear.Success, Is.True);
        Assert.That(fullDictionary.Data, Is.Not.EqualTo(earlyClear.Data));
        Assert.That(fullDictionaryDocument.Frames[0].IndexedPixels, Is.EqualTo(pixels));
        Assert.That(earlyClearDocument.Frames[0].IndexedPixels, Is.EqualTo(pixels));
    }

    [Test]
    public void PublicRawDocumentStreamWriteValidationAndLimitsAreAtomic()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        IGifDecoder decoder = codec;
        GifDocument document = decoder.DecodeBytes(GifFixtures.InterlacedGif89a(), null);
        byte[] expected = encoder.Encode(document, null);
        MemoryStream destination = new MemoryStream();

        encoder.Write(destination, document, null);
        destination.WriteByte(0xAA);

        Assert.That(destination.ToArray()[..expected.Length], Is.EqualTo(expected));
        Assert.That(destination.ToArray()[expected.Length], Is.EqualTo(0xAA));

        GifDocument invalid = decoder.DecodeBytes(GifFixtures.MinimalGif87a(), null);
        invalid.Frames[0].LzwMinimumCodeSize = 1;
        MemoryStream invalidDestination = new MemoryStream();
        GifException invalidException = Assert.Throws<GifException>(() => encoder.Write(invalidDestination, invalid, null));
        Assert.That(invalidException.ErrorCode, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(invalidDestination.Length, Is.EqualTo(0));

        GifEncodeOptions byteLimit = new GifEncodeOptions();
        byteLimit.MaxBytes = expected.Length - 1;
        GifException byteLimitException = Assert.Throws<GifException>(() => encoder.Encode(document, byteLimit));
        Assert.That(byteLimitException.ErrorCode, Is.EqualTo(GifErrorCode.ByteLimitExceeded));

        GifEncodeOptions memoryLimit = new GifEncodeOptions();
        memoryLimit.MaxMemoryBytes = 5;
        GifBitmap oversized = codec.CreateIndexedBitmap(3, 2, new byte[] { 0, 1, 2, 3, 0, 1 }, CreateFourColorPalette());
        GifEncodeResult memoryLimited = encoder.EncodeIndexedStill(oversized, memoryLimit, false, -1);
        Assert.That(memoryLimited.Success, Is.False);
        Assert.That(memoryLimited.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(memoryLimited.Data, Is.Empty);
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int left, int top, int delayMilliseconds)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = left;
        frame.SourceTop = top;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.DelayMilliseconds = delayMilliseconds;
        return frame;
    }

    static GifPalette CreateBlackWhitePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static GifPalette CreateFourColorPalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255)
        });
    }

    static byte[][] CopyCompressedFrames(GifDocument document)
    {
        byte[][] copies = new byte[document.Frames.Length][];
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            byte[] source = document.Frames[frameIndex].CompressedImageData;
            copies[frameIndex] = new byte[source.Length];
            System.Array.Copy(source, copies[frameIndex], source.Length);
            frameIndex = frameIndex + 1;
        }

        return copies;
    }

    static void AssertCompressedFrames(GifDocument document, byte[][] expected)
    {
        Assert.That(document.Frames, Has.Length.EqualTo(expected.Length));
        int frameIndex = 0;
        while (frameIndex < expected.Length)
        {
            Assert.That(document.Frames[frameIndex].CompressedImageData, Is.EqualTo(expected[frameIndex]));
            frameIndex = frameIndex + 1;
        }
    }

    static byte[] CreateCompressionPixels()
    {
        byte[] pixels = new byte[512];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = (byte)(((index * 3) + (index / 7)) % 4);
            index = index + 1;
        }

        return pixels;
    }
}
