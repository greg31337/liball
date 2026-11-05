using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPlainTextDecodePolicyTests
{
    [Test]
    public void DefaultPolicyPreservesPlainTextForExactRoundTripWithoutExposingGraphics()
    {
        GifCodec codec = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        byte[] source = GifFixtures.PlainTextGif89a();

        GifDocument document = codec.DecodeBytes(source, options);
        byte[] encoded = codec.Encode(document, null);

        Assert.That(options.PlainTextExtensionPolicy, Is.EqualTo(GifPlainTextExtensionPolicy.Preserve));
        Assert.That(document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.Metadata.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.PlainTextGraphics, Is.Empty);
        Assert.That(document.Warnings, Is.Empty);
        Assert.That(encoded, Is.EqualTo(source));
    }

    [Test]
    public void IgnorePolicyDiscardsPlainTextAndReportsWarningThroughRawAndAnimationDecode()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Ignore;
        byte[] source = GifFixtures.PlainTextGif89a();

        GifDocument document = decoder.DecodeMemory(new ReadOnlyMemory<byte>(source), options);
        GifAnimation animation = decoder.DecodeAnimationBytes(source, options);

        Assert.That(document.PlainTextExtensions, Is.Empty);
        Assert.That(document.Metadata.PlainTextExtensions, Is.Empty);
        Assert.That(document.PlainTextGraphics, Is.Empty);
        Assert.That(document.DecodeStatistics.ExtensionCount, Is.EqualTo(1));
        AssertIgnoredWarning(document.Warnings, 19);
        Assert.That(animation.PlainTextGraphics, Is.Empty);
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        AssertIgnoredWarning(animation.Warnings, 19);
    }

    [Test]
    public void IgnorePolicyConsumesGraphicControlScopedToPlainText()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Ignore;

        GifDocument document = decoder.DecodeBytes(AddGraphicControlBeforePlainText(GifFixtures.PlainTextGif89a()), options);

        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(0));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(document.Frames[0].HasTransparency, Is.False);
        Assert.That(document.Frames[0].TransparentColorIndex, Is.EqualTo(-1));
        AssertIgnoredWarning(document.Warnings, 27);
    }

    [Test]
    public void ExposePolicyPublishesStructuredGraphicsWithoutRasterizingThem()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = new GifDecodeOptions();
        options.PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Expose;
        byte[] source = AddGraphicControlBeforePlainText(GifFixtures.PlainTextGif89a());
        MemoryStream stream = new MemoryStream(source);

        GifDocument document = decoder.DecodeStream(stream, options);
        GifAnimation animation = decoder.DecodeCompositedFrameMemory(new ReadOnlyMemory<byte>(source), 0, options);

        Assert.That(document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.PlainTextGraphics.Length, Is.EqualTo(1));
        Assert.That(document.PlainTextGraphics[0], Is.SameAs(document.PlainTextExtensions[0]));
        AssertPlainTextGraphic(document.PlainTextGraphics[0]);
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(0));
        Assert.That(stream.CanRead, Is.True);
        Assert.That(animation.PlainTextGraphics.Length, Is.EqualTo(1));
        AssertPlainTextGraphic(animation.PlainTextGraphics[0]);
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    static void AssertIgnoredWarning(GifWarningDiagnostic[] warnings, long byteOffset)
    {
        Assert.That(warnings.Length, Is.EqualTo(1));
        Assert.That(warnings[0].Code, Is.EqualTo(GifWarningCode.IgnoredPlainTextExtension));
        Assert.That(warnings[0].Position.ByteOffset, Is.EqualTo(byteOffset));
        Assert.That(warnings[0].Position.FrameIndex, Is.EqualTo(0));
        Assert.That(warnings[0].Position.BlockName, Is.EqualTo("PlainTextExtension"));
    }

    static void AssertPlainTextGraphic(GifPlainTextExtension graphic)
    {
        Assert.That(graphic.TextGridLeft, Is.EqualTo(0));
        Assert.That(graphic.TextGridTop, Is.EqualTo(0));
        Assert.That(graphic.TextGridWidth, Is.EqualTo(1));
        Assert.That(graphic.TextGridHeight, Is.EqualTo(1));
        Assert.That(graphic.CharacterCellWidth, Is.EqualTo(8));
        Assert.That(graphic.CharacterCellHeight, Is.EqualTo(8));
        Assert.That(graphic.ForegroundColorIndex, Is.EqualTo(1));
        Assert.That(graphic.BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(graphic.TextData, Is.EqualTo(new byte[] { 112, 108, 97, 105, 110 }));
        Assert.That(graphic.Text, Is.EqualTo("plain"));
        Assert.That(graphic.GraphicControl.DelayHundredths, Is.EqualTo(7));
        Assert.That(graphic.GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(graphic.GraphicControl.HasTransparency, Is.True);
        Assert.That(graphic.GraphicControl.TransparentColorIndex, Is.EqualTo(1));
    }

    static byte[] AddGraphicControlBeforePlainText(byte[] source)
    {
        byte[] control = new byte[] { 0x21, 0xF9, 4, 0x05, 7, 0, 1, 0 };
        byte[] result = new byte[source.Length + control.Length];
        Array.Copy(source, 0, result, 0, 19);
        Array.Copy(control, 0, result, 19, control.Length);
        Array.Copy(source, 19, result, 19 + control.Length, source.Length - 19);
        return result;
    }
}
