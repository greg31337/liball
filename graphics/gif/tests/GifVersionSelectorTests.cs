using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifVersionSelectorTests
{
    [Test]
    public void Select_DefaultsToGif87aWhenNoGif89aFeaturesArePresent()
    {
        GifVersionSelector selector = new GifVersionSelector();

        GifVersion version = selector.Select(null!, null!);

        Assert.That(version, Is.EqualTo(GifVersion.Gif87a));
    }

    [Test]
    public void Select_HonorsExplicitEncodeVersionOptions()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifEncodeOptions options87 = new GifEncodeOptions();
        GifEncodeOptions options89 = new GifEncodeOptions();
        options87.Version = GifVersion.Gif87a;
        options89.Version = GifVersion.Gif89a;
        GifDocument document = new GifDocument();
        document.Comments = new GifCommentExtension[] { new GifCommentExtension("comment") };

        Assert.That(selector.Select(document, options87), Is.EqualTo(GifVersion.Gif87a));
        Assert.That(selector.Select(new GifDocument(), options89), Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void Select_UsesGif87aForPlainStillDocument()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(1, 1);
        document.GlobalColorTable = new GifPalette(new GifColor[] { new GifColor(0, 0, 0), new GifColor(255, 255, 255) });
        document.Frames = new GifFrame[] { new GifFrame(0, 0, 1, 1) };

        GifVersion version = selector.Select(document, new GifEncodeOptions());

        Assert.That(version, Is.EqualTo(GifVersion.Gif87a));
    }

    [Test]
    public void Select_UsesGif89aForDocumentMetadataExtensionsAndLooping()
    {
        GifVersionSelector selector = new GifVersionSelector();

        GifDocument commentDocument = new GifDocument();
        commentDocument.Comments = new GifCommentExtension[] { new GifCommentExtension("hello") };
        GifDocument applicationDocument = new GifDocument();
        applicationDocument.ApplicationExtensions = new GifApplicationExtension[] { new GifApplicationExtension() };
        GifDocument plainTextDocument = new GifDocument();
        plainTextDocument.PlainTextExtensions = new GifPlainTextExtension[] { new GifPlainTextExtension() };
        GifDocument unknownDocument = new GifDocument();
        unknownDocument.UnknownExtensions = new GifUnknownExtension[] { new GifUnknownExtension(0, null, null) };
        GifDocument loopDocument = new GifDocument();
        loopDocument.LoopCount = 0;

        Assert.That(selector.Select(commentDocument, null!), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(applicationDocument, null!), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(plainTextDocument, null!), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(unknownDocument, null!), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(loopDocument, null!), Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void Select_UsesGif89aForMetadataContainerFeatures()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifDocument document = new GifDocument();
        document.Metadata.LoopCount = 2;

        GifVersion version = selector.Select(document, null!);

        Assert.That(version, Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void Select_UsesGif89aForFrameGraphicControlFeatures()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifFrame delayFrame = new GifFrame(0, 0, 0, 0);
        delayFrame.DelayHundredths = 5;
        GifFrame disposalFrame = new GifFrame(0, 0, 0, 0);
        disposalFrame.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        GifFrame transparentFrame = new GifFrame(0, 0, 0, 0);
        transparentFrame.HasTransparency = true;
        GifFrame userInputFrame = new GifFrame(0, 0, 0, 0);
        userInputFrame.UserInputFlag = true;

        Assert.That(selector.FrameRequiresGif89a(delayFrame), Is.True);
        Assert.That(selector.FrameRequiresGif89a(disposalFrame), Is.True);
        Assert.That(selector.FrameRequiresGif89a(transparentFrame), Is.True);
        Assert.That(selector.FrameRequiresGif89a(userInputFrame), Is.True);
    }

    [Test]
    public void Select_UsesGif89aForNestedGraphicControl()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifFrame frame = new GifFrame(0, 0, 0, 0);
        frame.GraphicControl.DelayHundredths = 10;
        GifDocument document = new GifDocument();
        document.Frames = new GifFrame[] { frame };

        GifVersion version = selector.Select(document, null!);

        Assert.That(version, Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void Select_UsesGif89aForEncodeOptionsTransparencyOrLooping()
    {
        GifVersionSelector selector = new GifVersionSelector();
        GifEncodeOptions transparentOptions = new GifEncodeOptions();
        GifEncodeOptions loopOptions = new GifEncodeOptions();
        GifEncodeOptions commentOptions = new GifEncodeOptions();
        GifEncodeOptions applicationOptions = new GifEncodeOptions();
        GifEncodeOptions plainTextOptions = new GifEncodeOptions();
        GifEncodeOptions unknownOptions = new GifEncodeOptions();
        transparentOptions.TransparentColorMode = GifTransparentColorMode.AlphaThreshold;
        loopOptions.LoopCount = 0;
        commentOptions.Comments = new GifCommentExtension[] { new GifCommentExtension("hello") };
        applicationOptions.ApplicationExtensions = new GifApplicationExtension[] { new GifApplicationExtension() };
        plainTextOptions.PlainTextExtensions = new GifPlainTextExtension[] { new GifPlainTextExtension() };
        unknownOptions.UnknownExtensions = new GifUnknownExtension[] { new GifUnknownExtension(0x99, new byte[] { 1 }, "UnknownExtension0x99") };

        Assert.That(selector.Select(null!, transparentOptions), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(null!, loopOptions), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(null!, commentOptions), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(null!, applicationOptions), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(null!, plainTextOptions), Is.EqualTo(GifVersion.Gif89a));
        Assert.That(selector.Select(null!, unknownOptions), Is.EqualTo(GifVersion.Gif89a));
    }
}
