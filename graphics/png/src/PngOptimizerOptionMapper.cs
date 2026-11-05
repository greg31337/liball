namespace png;

internal class PngOptimizerOptionMapper
{
    internal static PngEncoderOptions CreateEncoderOptions(PngImage image, PngOptimizerOptions options, int compressionLevel)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(options);

        PngEncoderOptions encoderOptions = new PngEncoderOptions();
        encoderOptions.Limits = options.Limits;
        encoderOptions.MetadataPolicy = options.MetadataPolicy;
        encoderOptions.TextCompressionMode = options.TextCompressionMode;
        encoderOptions.TextCompressionThreshold = options.TextCompressionThreshold;
        encoderOptions.UnknownChunkDisposition = options.UnknownChunkDisposition;
        encoderOptions.MetadataChunkTypesToKeep = options.MetadataChunkTypesToKeep;
        encoderOptions.MetadataChunkTypesToRemove = options.MetadataChunkTypesToRemove;
        encoderOptions.UnknownChunkReplacements = options.UnknownChunkReplacements;
        encoderOptions.PixelDataChanged = options.PixelDataChanged;
        encoderOptions.FixedFilterType = options.FixedFilterType;
        encoderOptions.RowFilterStrategy = options.RowFilterStrategy;
        encoderOptions.UseFixedFilter = options.UseFixedFilter || !options.TryFilters || options.RowFilterStrategy == PngRowFilterStrategy.Fixed;
        encoderOptions.CompressionLevel = compressionLevel;
        encoderOptions.UseAutomaticColorType = options.AllowColorTypeReduction;
        encoderOptions.UseAutomaticBitDepth = options.AllowBitDepthReduction;
        encoderOptions.AllowPaletteReduction = options.AllowPaletteReduction;
        encoderOptions.EncodeAnimation = HasAnimation(image);
        if (!encoderOptions.UseAutomaticColorType && image.Pixels.Format != PngPixelFormat.Unknown)
        {
            encoderOptions.ColorType = PngEncoder.GetColorType(image.Pixels.Format);
        }

        if (!encoderOptions.UseAutomaticBitDepth && image.Pixels.Format != PngPixelFormat.Unknown)
        {
            encoderOptions.BitDepth = PngEncoder.GetBitDepth(image.Pixels.Format);
        }

        if (encoderOptions.RowFilterStrategy == PngRowFilterStrategy.Exhaustive)
        {
            encoderOptions.RowFilterStrategy = PngRowFilterStrategy.Adaptive;
        }

        return encoderOptions;
    }

    internal static bool HasAnimation(PngImage image)
    {
        if (image.Animation == null || image.Animation.Frames == null)
        {
            return false;
        }

        return image.Animation.Frames.Length > 0;
    }

    internal static PngOptimizerOptions CopyOptions(PngOptimizerOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        PngOptimizerOptions copy = new PngOptimizerOptions();
        copy.Limits = source.Limits;
        copy.MetadataPolicy = source.MetadataPolicy;
        copy.TextCompressionMode = source.TextCompressionMode;
        copy.TextCompressionThreshold = source.TextCompressionThreshold;
        copy.UnknownChunkDisposition = source.UnknownChunkDisposition;
        copy.MetadataChunkTypesToKeep = source.MetadataChunkTypesToKeep;
        copy.MetadataChunkTypesToRemove = source.MetadataChunkTypesToRemove;
        copy.UnknownChunkReplacements = source.UnknownChunkReplacements;
        copy.PixelDataChanged = source.PixelDataChanged;
        copy.FixedFilterType = source.FixedFilterType;
        copy.RowFilterStrategy = source.RowFilterStrategy;
        copy.MinimumCompressionLevel = source.MinimumCompressionLevel;
        copy.MaximumCompressionLevel = source.MaximumCompressionLevel;
        copy.TryFilters = source.TryFilters;
        copy.TryCompressionLevels = source.TryCompressionLevels;
        copy.UseFixedFilter = source.UseFixedFilter;
        copy.AllowColorTypeReduction = source.AllowColorTypeReduction;
        copy.AllowBitDepthReduction = source.AllowBitDepthReduction;
        copy.AllowPaletteReduction = source.AllowPaletteReduction;
        copy.UseEarlyBailout = source.UseEarlyBailout;
        copy.BestKnownByteCount = source.BestKnownByteCount;
        return copy;
    }
}
