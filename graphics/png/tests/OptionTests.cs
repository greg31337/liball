using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class OptionTests
{
    [Test]
    public void OptionClasses_UseVisibilityThatMatchesTheirApiStatus()
    {
        Type[] optionTypes = new Type[]
        {
            typeof(PngSecurityLimits),
            typeof(PngChunkWriteOptions),
            typeof(PngPixelConversionOptions),
            typeof(PngDecoderOptions),
            typeof(PngEncoderOptions),
            typeof(PngTransformOptions),
            typeof(PngOptimizerOptions)
        };

        foreach (Type optionType in optionTypes)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            bool isPublicApi = optionType != typeof(PngTransformOptions);

            Assert.That(optionType.IsPublic, Is.EqualTo(isPublicApi), optionType.Name);
            Assert.That(optionType.GetProperties(flags), Is.Empty, optionType.Name);
            Assert.That(optionType.GetEvents(flags), Is.Empty, optionType.Name);

            FieldInfo[] fields = optionType.GetFields(flags);
            Assert.That(fields, Is.Not.Empty, optionType.Name);
            foreach (FieldInfo field in fields)
            {
                Assert.That(field.IsPublic, Is.EqualTo(isPublicApi), optionType.Name + "." + field.Name);
                Assert.That(field.IsAssembly, Is.EqualTo(!isPublicApi), optionType.Name + "." + field.Name);
            }
        }
    }

    [Test]
    public void ChunkWriteOptions_DefaultToPreservedPlacementWithLimits()
    {
        PngChunkWriteOptions options = new PngChunkWriteOptions();

        Assert.That(options.PlacementPolicy, Is.EqualTo(PngChunkPlacementPolicy.Preserve));
        Assert.That(options.Limits, Is.Not.Null);
    }

    [Test]
    public void PixelConversionOptions_DefaultToRgba8WithoutMetadata()
    {
        PngPixelConversionOptions options = new PngPixelConversionOptions();

        Assert.That(options.TargetFormat, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(options.Palette, Is.Null);
        Assert.That(options.IndexedTransparency, Is.Null);
        Assert.That(options.GrayscaleTransparency, Is.Null);
        Assert.That(options.TruecolorTransparency, Is.Null);
    }

    [Test]
    public void DecoderOptions_DefaultToStrictRgbaDecodeWithLimits()
    {
        PngDecoderOptions options = new PngDecoderOptions();

        Assert.That(options.ErrorPolicy, Is.EqualTo(PngErrorPolicy.Strict));
        Assert.That(options.PreferredPixelFormat, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(options.ValidateCrc, Is.True);
        Assert.That(options.PreserveUnknownChunks, Is.True);
        Assert.That(options.DecodeAnimation, Is.True);
        Assert.That(options.IgnoreTrailingBytes, Is.False);
        Assert.That(options.Limits, Is.Not.Null);
    }

    [Test]
    public void EncoderOptions_DefaultToRgba8PngSettingsWithPreservation()
    {
        PngEncoderOptions options = new PngEncoderOptions();

        Assert.That(options.ColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(options.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(options.CompressionMethod, Is.EqualTo(PngCompressionMethod.Deflate));
        Assert.That(options.FilterMethod, Is.EqualTo(PngFilterMethod.Adaptive));
        Assert.That(options.InterlaceMethod, Is.EqualTo(PngInterlaceMethod.None));
        Assert.That(options.FixedFilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(options.RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Adaptive));
        Assert.That(options.MetadataPolicy, Is.EqualTo(PngMetadataPolicy.Preserve));
        Assert.That(options.TextCompressionMode, Is.EqualTo(PngTextCompressionMode.Preserve));
        Assert.That(options.TextCompressionThreshold, Is.EqualTo(1024));
        Assert.That(options.UnknownChunkDisposition, Is.EqualTo(PngChunkDisposition.Preserve));
        Assert.That(options.MetadataChunkTypesToKeep, Is.Empty);
        Assert.That(options.MetadataChunkTypesToRemove, Is.Empty);
        Assert.That(options.UnknownChunkReplacements, Is.Empty);
        Assert.That(options.PixelDataChanged, Is.True);
        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.MaxIdatChunkBytes, Is.EqualTo(0));
        Assert.That(options.UseAutomaticColorType, Is.True);
        Assert.That(options.UseAutomaticBitDepth, Is.True);
        Assert.That(options.UseFixedFilter, Is.False);
        Assert.That(options.EncodeAnimation, Is.False);
    }

    [Test]
    public void TransformOptions_DefaultToRgbaOutputWithoutTransforms()
    {
        PngTransformOptions options = new PngTransformOptions();

        Assert.That(options.OutputPixelFormat, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(options.ExpandPalette, Is.False);
        Assert.That(options.ExpandTransparency, Is.False);
        Assert.That(options.StripAlpha, Is.False);
        Assert.That(options.AddOpaqueAlpha, Is.False);
        Assert.That(options.PremultiplyAlpha, Is.False);
        Assert.That(options.UnpremultiplyAlpha, Is.False);
        Assert.That(options.ClearTransparentPixels, Is.False);
        Assert.That(options.InvertAlpha, Is.False);
        Assert.That(options.InvertMonochrome, Is.False);
        Assert.That(options.SwapRgbBgr, Is.False);
        Assert.That(options.SwapRgbaArgb, Is.False);
        Assert.That(options.ByteSwap16BitSamples, Is.False);
        Assert.That(options.FlipVertical, Is.False);
        Assert.That(options.Crop, Is.False);
        Assert.That(options.CropX, Is.EqualTo(0));
        Assert.That(options.CropY, Is.EqualTo(0));
        Assert.That(options.CropWidth, Is.EqualTo(0));
        Assert.That(options.CropHeight, Is.EqualTo(0));
        Assert.That(options.PadCanvas, Is.False);
        Assert.That(options.PadLeft, Is.EqualTo(0));
        Assert.That(options.PadTop, Is.EqualTo(0));
        Assert.That(options.PadRight, Is.EqualTo(0));
        Assert.That(options.PadBottom, Is.EqualTo(0));
        Assert.That(options.ResizeNearestNeighbor, Is.False);
        Assert.That(options.ResizeBilinear, Is.False);
        Assert.That(options.ResizeWidth, Is.EqualTo(0));
        Assert.That(options.ResizeHeight, Is.EqualTo(0));
    }

    [Test]
    public void OptimizerOptions_DefaultToLosslessTrialsWithPreservation()
    {
        PngOptimizerOptions options = new PngOptimizerOptions();

        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.MetadataPolicy, Is.EqualTo(PngMetadataPolicy.Preserve));
        Assert.That(options.TextCompressionMode, Is.EqualTo(PngTextCompressionMode.Preserve));
        Assert.That(options.TextCompressionThreshold, Is.EqualTo(1024));
        Assert.That(options.UnknownChunkDisposition, Is.EqualTo(PngChunkDisposition.Preserve));
        Assert.That(options.MetadataChunkTypesToKeep, Is.Empty);
        Assert.That(options.MetadataChunkTypesToRemove, Is.Empty);
        Assert.That(options.UnknownChunkReplacements, Is.Empty);
        Assert.That(options.PixelDataChanged, Is.True);
        Assert.That(options.FixedFilterType, Is.EqualTo(PngFilterType.None));
        Assert.That(options.RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Exhaustive));
        Assert.That(options.TryFilters, Is.True);
        Assert.That(options.TryCompressionLevels, Is.True);
        Assert.That(options.UseFixedFilter, Is.False);
        Assert.That(options.AllowColorTypeReduction, Is.True);
        Assert.That(options.AllowBitDepthReduction, Is.True);
        Assert.That(options.AllowPaletteReduction, Is.True);
        Assert.That(options.UseEarlyBailout, Is.True);
        Assert.That(options.BestKnownByteCount, Is.EqualTo(0));
    }

    [Test]
    public void SecurityLimits_DefaultToBoundedValues()
    {
        PngSecurityLimits limits = new PngSecurityLimits();

        Assert.That(limits.MaxWidth, Is.EqualTo(100000));
        Assert.That(limits.MaxHeight, Is.EqualTo(100000));
        Assert.That(limits.MaxPixelCount, Is.EqualTo(268435456));
        Assert.That(limits.MaxChunkCount, Is.EqualTo(100000));
        Assert.That(limits.MaxMetadataBytes, Is.EqualTo(67108864));
        Assert.That(limits.MaxTextBytes, Is.EqualTo(16777216));
        Assert.That(limits.MaxTextEntries, Is.EqualTo(10000));
        Assert.That(limits.MaxIdatBytes, Is.EqualTo(1073741824));
        Assert.That(limits.MaxDecompressedBytes, Is.EqualTo(2147483648));
        Assert.That(limits.MaxAnimationFrames, Is.EqualTo(10000));
    }

    [Test]
    public void SecurityLimitConstants_MatchDefaultFieldValues()
    {
        PngSecurityLimits limits = new PngSecurityLimits();

        Assert.That(limits.MaxWidth, Is.EqualTo(PngSecurityLimits.DefaultMaxWidth));
        Assert.That(limits.MaxHeight, Is.EqualTo(PngSecurityLimits.DefaultMaxHeight));
        Assert.That(limits.MaxPixelCount, Is.EqualTo(PngSecurityLimits.DefaultMaxPixelCount));
        Assert.That(limits.MaxChunkCount, Is.EqualTo(PngSecurityLimits.DefaultMaxChunkCount));
        Assert.That(limits.MaxMetadataBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxMetadataBytes));
        Assert.That(limits.MaxTextBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxTextBytes));
        Assert.That(limits.MaxTextEntries, Is.EqualTo(PngSecurityLimits.DefaultMaxTextEntries));
        Assert.That(limits.MaxIdatBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxIdatBytes));
        Assert.That(limits.MaxDecompressedBytes, Is.EqualTo(PngSecurityLimits.DefaultMaxDecompressedBytes));
        Assert.That(limits.MaxAnimationFrames, Is.EqualTo(PngSecurityLimits.DefaultMaxAnimationFrames));
    }

    [Test]
    public void OptionClasses_CreateIndependentSecurityLimits()
    {
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        PngEncoderOptions encoderOptions = new PngEncoderOptions();
        PngOptimizerOptions optimizerOptions = new PngOptimizerOptions();

        Assert.That(decoderOptions.Limits, Is.Not.SameAs(encoderOptions.Limits));
        Assert.That(decoderOptions.Limits, Is.Not.SameAs(optimizerOptions.Limits));
        Assert.That(encoderOptions.Limits, Is.Not.SameAs(optimizerOptions.Limits));
    }
}
