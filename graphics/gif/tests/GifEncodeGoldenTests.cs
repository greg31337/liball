using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifEncodeGoldenTests
{
    [Test]
    public void EncodeIndexedStill_MinimalFixturesMatchEncodeGoldens()
    {
        GifEncodeResult gif87a = Gif.EncodeIndexedStill(CreateBlackBitmap());
        GifEncodeOptions gif89aOptions = new GifEncodeOptions();
        gif89aOptions.Version = GifVersion.Gif89a;
        GifEncodeResult gif89a = Gif.EncodeIndexedStill(CreateBlackBitmap(), gif89aOptions);

        Assert.That(gif87a.Success, Is.True);
        Assert.That(gif89a.Success, Is.True);
        Assert.That(gif87a.Data, Is.EqualTo(WithByte(GifFixtures.MinimalGif87a(), 10, 240)));
        Assert.That(gif89a.Data, Is.EqualTo(WithByte(GifFixtures.MinimalGif89a(), 10, 240)));
    }

    [Test]
    public void EncodeIndexedStill_TransparencyAndLocalColorTableFixturesMatchEncodeGoldens()
    {
        GifEncodeResult transparent = Gif.EncodeIndexedStill(CreateBlackBitmap(), true, 0);
        GifEncodeOptions localOptions = new GifEncodeOptions();
        localOptions.Version = GifVersion.Gif89a;
        localOptions.ColorTableMode = GifColorTableMode.Local;
        GifEncodeResult local = Gif.EncodeIndexedStill(CreateBlueLocalBitmap(), localOptions);

        Assert.That(transparent.Success, Is.True);
        Assert.That(local.Success, Is.True);
        Assert.That(transparent.Data, Is.EqualTo(WithByte(GifFixtures.TransparentGif89a(), 10, 240)));
        Assert.That(local.Data, Is.EqualTo(WithByte(GifFixtures.LocalColorTableGif89a(), 10, 112)));
    }

    [Test]
    public void EncodeIndexedStill_MetadataExtensionFixturesMatchEncodeGoldens()
    {
        GifEncodeResult loop = Gif.EncodeIndexedStill(CreateBlackBitmap(), CreateLoopOptions());
        GifEncodeResult comment = Gif.EncodeIndexedStill(CreateBlackBitmap(), CreateCommentOptions());
        GifEncodeResult application = Gif.EncodeIndexedStill(CreateBlackBitmap(), CreateApplicationOptions());
        GifEncodeResult plainText = Gif.EncodeIndexedStill(CreateBlackBitmap(), CreatePlainTextOptions());

        Assert.That(loop.Success, Is.True);
        Assert.That(comment.Success, Is.True);
        Assert.That(application.Success, Is.True);
        Assert.That(plainText.Success, Is.True);
        Assert.That(loop.Data, Is.EqualTo(WithByte(GifFixtures.NetscapeLoopGif89a(), 10, 240)));
        Assert.That(comment.Data, Is.EqualTo(WithByte(GifFixtures.CommentGif89a(), 10, 240)));
        Assert.That(application.Data, Is.EqualTo(WithByte(GifFixtures.ApplicationExtensionGif89a(), 10, 240)));
        Assert.That(plainText.Data, Is.EqualTo(WithByte(GifFixtures.PlainTextGif89a(), 10, 240)));
    }

    [Test]
    public void EncodeIndexedStill_InterlacedFixtureMatchesEncodeGolden()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.Version = GifVersion.Gif89a;
        options.Interlace = true;
        GifEncodeResult encoded = Gif.EncodeIndexedStill(CreateInterlacedBitmap(), options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Data, Is.EqualTo(InterlacedEncodeGolden()));
    }

    [Test]
    public void EncodeIndexedAnimation_DisposalFixturesMatchEncodeGoldens()
    {
        AssertDisposalEncodeGolden(GifDisposalMethod.Unspecified, WithByte(GifFixtures.AnimatedDisposalMethod0Gif89a(), 10, 241));
        AssertDisposalEncodeGolden(GifDisposalMethod.DoNotDispose, WithByte(GifFixtures.AnimatedDisposalMethod1Gif89a(), 10, 241));
        AssertDisposalEncodeGolden(GifDisposalMethod.RestoreToBackground, WithByte(GifFixtures.AnimatedDisposalMethod2Gif89a(), 10, 241));
        AssertDisposalEncodeGolden(GifDisposalMethod.RestoreToPrevious, WithByte(GifFixtures.AnimatedDisposalMethod3Gif89a(), 10, 241));
    }

    static void AssertDisposalEncodeGolden(GifDisposalMethod middleDisposal, byte[] expected)
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 2;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateAnimationFrame(0, new byte[] { 1 }, GifDisposalMethod.DoNotDispose),
            CreateAnimationFrame(0, new byte[] { 2 }, middleDisposal),
            CreateAnimationFrame(1, new byte[] { 3 }, GifDisposalMethod.DoNotDispose)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.Version = GifVersion.Gif89a;

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation, options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Data, Is.EqualTo(expected));
    }

    static GifBitmap CreateBlackBitmap()
    {
        return GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, CreateBlackWhitePalette());
    }

    static GifBitmap CreateBlueLocalBitmap()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(255, 0, 0),
            new GifColor(0, 0, 255)
        });
        return GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette);
    }

    static GifBitmap CreateInterlacedBitmap()
    {
        return GifBitmap.CreateIndexed(2, 4, new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3
        }, CreateFourColorPalette());
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
            new GifColor(0, 0, 255),
            new GifColor(0, 255, 0)
        });
    }

    static GifEncodeOptions CreateLoopOptions()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 5;
        return options;
    }

    static GifEncodeOptions CreateCommentOptions()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.Comments = new GifCommentExtension[] { new GifCommentExtension("fixture") };
        return options;
    }

    static GifEncodeOptions CreateApplicationOptions()
    {
        GifApplicationExtension application = new GifApplicationExtension();
        application.Identifier = "TESTAPP1";
        application.AuthenticationCode = "001";
        application.Data = new byte[] { 9, 8, 7 };
        GifEncodeOptions options = new GifEncodeOptions();
        options.ApplicationExtensions = new GifApplicationExtension[] { application };
        return options;
    }

    static GifEncodeOptions CreatePlainTextOptions()
    {
        GifPlainTextExtension plainText = new GifPlainTextExtension();
        plainText.TextGridWidth = 1;
        plainText.TextGridHeight = 1;
        plainText.CharacterCellWidth = 8;
        plainText.CharacterCellHeight = 8;
        plainText.ForegroundColorIndex = 1;
        plainText.BackgroundColorIndex = 0;
        plainText.Text = "plain";
        GifEncodeOptions options = new GifEncodeOptions();
        options.PlainTextExtensions = new GifPlainTextExtension[] { plainText };
        return options;
    }

    static GifAnimationFrame CreateAnimationFrame(int left, byte[] indexedPixels, GifDisposalMethod disposalMethod)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = GifBitmap.CreateIndexed(1, 1, indexedPixels, CreateFourColorPalette());
        frame.SourceLeft = left;
        frame.SourceTop = 0;
        frame.SourceWidth = 1;
        frame.SourceHeight = 1;
        frame.DelayMilliseconds = 10;
        frame.DisposalMethod = disposalMethod;
        return frame;
    }

    static byte[] WithByte(byte[] data, int index, byte value)
    {
        byte[] result = new byte[data.Length];
        int offset = 0;
        while (offset < data.Length)
        {
            result[offset] = data[offset];
            offset = offset + 1;
        }

        result[index] = value;
        return result;
    }

    static byte[] InterlacedEncodeGolden()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 4, 0, 241, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 0, 255, 0, 255, 0,
            44, 0, 0, 0, 0, 2, 0, 4, 0, 64,
            2, 5, 4, 36, 17, 51, 5, 0,
            59
        };
    }
}
