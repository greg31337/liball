using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifOptionsTests
{
    [Test]
    public void DecodeOptions_DefaultsUseSafetyLimitsAndPreserveUnknownExtensions()
    {
        GifDecodeOptions options = new GifDecodeOptions();

        Assert.That(options.Strictness, Is.EqualTo(GifStrictness.Normal));
        Assert.That(options.MaxWidth, Is.EqualTo(GifConstants.DefaultMaxWidth));
        Assert.That(options.MaxHeight, Is.EqualTo(GifConstants.DefaultMaxHeight));
        Assert.That(options.MaxFrames, Is.EqualTo(GifConstants.DefaultMaxFrames));
        Assert.That(options.MaxDecodedPixels, Is.EqualTo(GifConstants.DefaultMaxDecodedPixels));
        Assert.That(options.MaxBytes, Is.EqualTo(GifConstants.DefaultMaxBytes));
        Assert.That(options.MaxCompressedBytes, Is.EqualTo(GifConstants.DefaultMaxCompressedBytes));
        Assert.That(options.MaxSubBlocks, Is.EqualTo(GifConstants.DefaultMaxSubBlocks));
        Assert.That(options.MaxSubBlockBytes, Is.EqualTo(GifConstants.DefaultMaxSubBlockBytes));
        Assert.That(options.MaxMemoryBytes, Is.EqualTo(GifConstants.DefaultMaxMemoryBytes));
        Assert.That(options.MetadataOnly, Is.False);
        Assert.That(options.FirstFrameOnly, Is.False);
        Assert.That(options.PreserveUnknownExtensions, Is.True);
        Assert.That(options.TolerateMalformedStreams, Is.False);
        Assert.That(options.DefaultGlobalColorTable, Is.Empty);
        Assert.That(options.DisposalFallback, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(options.MinimumFrameDelayMilliseconds, Is.EqualTo(0));
        Assert.That(options.BackgroundMode, Is.EqualTo(GifBackgroundMode.LogicalScreen));
    }

    [Test]
    public void EncodeOptions_DefaultsAvoidOptionalTransformations()
    {
        GifEncodeOptions options = new GifEncodeOptions();

        Assert.That(options.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(options.ColorTableMode, Is.EqualTo(GifColorTableMode.Auto));
        Assert.That(options.QuantizerMode, Is.EqualTo(GifQuantizerMode.Exact));
        Assert.That(options.DitherMode, Is.EqualTo(GifDitherMode.None));
        Assert.That(options.OptimizationMode, Is.EqualTo(GifOptimizationMode.None));
        Assert.That(options.RemoveZeroDelayFrames, Is.False);
        Assert.That(options.RemoveDuplicateFrames, Is.False);
        Assert.That(options.NormalizeFrameDelays, Is.False);
        Assert.That(options.MinimumFrameDelayMilliseconds, Is.EqualTo(0));
        Assert.That(options.DeconstructFrames, Is.False);
        Assert.That(options.RecalculateTimeline, Is.True);
        Assert.That(options.LzwClearPolicy, Is.EqualTo(GifLzwClearPolicy.ClearOnFullDictionary));
        Assert.That(options.LoopCount, Is.EqualTo(-1));
        Assert.That(options.Comments, Is.Empty);
        Assert.That(options.ApplicationExtensions, Is.Empty);
        Assert.That(options.PlainTextExtensions, Is.Empty);
        Assert.That(options.UnknownExtensions, Is.Empty);
        Assert.That(options.BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(options.TransparentColorMode, Is.EqualTo(GifTransparentColorMode.None));
        Assert.That(options.Interlace, Is.False);
        Assert.That(options.PreserveMetadata, Is.True);
        Assert.That(options.ValidateBeforeWrite, Is.True);
        Assert.That(options.MaxBytes, Is.EqualTo(GifConstants.DefaultMaxBytes));
        Assert.That(options.MaxMemoryBytes, Is.EqualTo(GifConstants.DefaultMaxMemoryBytes));
    }

    [Test]
    public void AnimationOptions_DefaultToSinglePlaybackWithoutTransparency()
    {
        GifAnimationOptions options = new GifAnimationOptions();

        Assert.That(options.DelaysMilliseconds, Is.Empty);
        Assert.That(options.DefaultDelayMilliseconds, Is.EqualTo(100));
        Assert.That(options.DisposalMethods, Is.Empty);
        Assert.That(options.DefaultDisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(options.LoopCount, Is.EqualTo(-1));
        Assert.That(options.FrameLefts, Is.Empty);
        Assert.That(options.FrameTops, Is.Empty);
        Assert.That(options.UseTransparency, Is.False);
        Assert.That(options.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(options.BackgroundMode, Is.EqualTo(GifBackgroundMode.LogicalScreen));
    }

    [Test]
    public void QuantizeOptions_DefaultToExactUnditheredMaximumGifPalette()
    {
        GifQuantizeOptions options = new GifQuantizeOptions();

        Assert.That(options.MaxColors, Is.EqualTo(GifConstants.MaximumColorTableSize));
        Assert.That(options.QuantizerMode, Is.EqualTo(GifQuantizerMode.Exact));
        Assert.That(options.DitherMode, Is.EqualTo(GifDitherMode.None));
        Assert.That(options.SamplingStep, Is.EqualTo(1));
        Assert.That(options.TransparentAlphaThreshold, Is.EqualTo(0));
        Assert.That(options.PaletteMode, Is.EqualTo(GifColorTableMode.Auto));
        Assert.That(options.PreservePaletteOrder, Is.True);
    }

    [Test]
    public void OptionFields_AreMutableForCallers()
    {
        GifDecodeOptions decodeOptions = new GifDecodeOptions();
        GifEncodeOptions encodeOptions = new GifEncodeOptions();
        GifAnimationOptions animationOptions = new GifAnimationOptions();
        GifQuantizeOptions quantizeOptions = new GifQuantizeOptions();

        decodeOptions.MetadataOnly = true;
        encodeOptions.Interlace = true;
        animationOptions.LoopCount = 0;
        quantizeOptions.MaxColors = 16;

        Assert.That(decodeOptions.MetadataOnly, Is.True);
        Assert.That(encodeOptions.Interlace, Is.True);
        Assert.That(animationOptions.LoopCount, Is.EqualTo(0));
        Assert.That(quantizeOptions.MaxColors, Is.EqualTo(16));
    }
}
