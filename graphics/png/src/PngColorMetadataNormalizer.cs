using System.Collections.Generic;

namespace png;

internal class PngColorMetadataNormalizer
{
    internal const double StandardRgbGamma = 0.45455;
    internal const string IccProfileTransformUnavailableCode = "IccProfileTransformUnavailable";
    internal const string UnsupportedCicpTransformCode = "UnsupportedCicpTransform";
    internal const string ChromaticitiesTransformUnavailableCode = "ChromaticitiesTransformUnavailable";
    internal const string GammaMetadataUnavailableCode = "GammaMetadataUnavailable";
    internal const string BackgroundUnavailableCode = "BackgroundUnavailable";

    internal static PngColorNormalizationResult Normalize(PngImage image, PngColorNormalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngColorMetadata colorMetadata = PngColorMetadataPrecedenceResolver.Resolve(image.Metadata, options.Limits);
        PngPixelBuffer pixels = PreparePixels(image.Pixels, image.Metadata);
        List<PngWarning> warnings = new List<PngWarning>();
        bool appliedSignificantBits = false;
        if (options.ApplySignificantBits && image.Metadata.SignificantBits != null)
        {
            PngSignificantBits bits = PrepareSignificantBits(image.Metadata.SignificantBits, pixels.Format);
            pixels = PngSampleTransformer.DownshiftToSignificantBits(pixels, bits);
            pixels = PngSampleTransformer.UpscaleFromSignificantBits(pixels, bits);
            appliedSignificantBits = true;
        }

        bool appliedBackground = false;
        if (options.CompositeOverBackground && HasAlpha(pixels.Format))
        {
            PngBackgroundColor? background = options.Background;
            if (background == null)
            {
                background = image.Metadata.BackgroundColor;
            }

            if (background == null)
            {
                warnings.Add(CreateWarning(BackgroundUnavailableCode, "Background compositing was requested, but neither an explicit background nor bKGD metadata was available.", PngConstants.ChunkBackgroundColor));
            }
            else
            {
                PngBackgroundColor effectiveBackground = ResolveBackground(image.Pixels.Format, background, image.Metadata);
                pixels = PngAlphaTransformer.CompositeOverBackground(pixels, effectiveBackground);
                appliedBackground = true;
            }
        }

        PngColorTransformResult colorResult = ApplyColorMetadata(pixels, colorMetadata);
        AddWarnings(warnings, colorResult.Warnings);

        PngColorNormalizationResult result = new PngColorNormalizationResult();
        result.Pixels = colorResult.Pixels;
        result.ColorMetadata = colorMetadata;
        result.Warnings = warnings.ToArray();
        result.AppliedSignificantBits = appliedSignificantBits;
        result.AppliedGammaTransform = colorResult.AppliedGammaTransform;
        result.AppliedBackgroundComposite = appliedBackground;
        result.IsSrgbLike = colorResult.IsSrgbLike;
        return result;
    }

    internal static PngPixelBuffer PreparePixels(PngPixelBuffer pixels, PngMetadata metadata)
    {
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = GetNormalizationFormat(pixels.Format, metadata.IndexedTransparency != null, metadata.GrayscaleTransparency != null, metadata.TruecolorTransparency != null);
        options.Palette = metadata.Palette;
        options.IndexedTransparency = metadata.IndexedTransparency;
        options.GrayscaleTransparency = metadata.GrayscaleTransparency;
        options.TruecolorTransparency = metadata.TruecolorTransparency;
        return PngPixelFormatConverter.Convert(pixels, options);
    }

    internal static PngPixelFormat GetNormalizationFormat(PngPixelFormat format, bool hasIndexedTransparency, bool hasGrayscaleTransparency, bool hasTruecolorTransparency)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
                if (hasGrayscaleTransparency)
                {
                    return PngPixelFormat.GrayscaleAlpha8;
                }

                return PngPixelFormat.Grayscale8;
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                if (hasIndexedTransparency)
                {
                    return PngPixelFormat.Rgba8;
                }

                return PngPixelFormat.Rgb8;
            case PngPixelFormat.Bgr8:
                if (hasTruecolorTransparency)
                {
                    return PngPixelFormat.Rgba8;
                }

                return PngPixelFormat.Rgb8;
            case PngPixelFormat.Bgr16:
                if (hasTruecolorTransparency)
                {
                    return PngPixelFormat.Rgba16;
                }

                return PngPixelFormat.Rgb16;
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return PngPixelFormat.Rgba8;
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return PngPixelFormat.Rgba16;
            case PngPixelFormat.Grayscale8:
                if (hasGrayscaleTransparency)
                {
                    return PngPixelFormat.GrayscaleAlpha8;
                }

                return format;
            case PngPixelFormat.Grayscale16:
                if (hasGrayscaleTransparency)
                {
                    return PngPixelFormat.GrayscaleAlpha16;
                }

                return format;
            case PngPixelFormat.Rgb8:
                if (hasTruecolorTransparency)
                {
                    return PngPixelFormat.Rgba8;
                }

                return format;
            case PngPixelFormat.Rgb16:
                if (hasTruecolorTransparency)
                {
                    return PngPixelFormat.Rgba16;
                }

                return format;
            default:
                return format;
        }
    }

    internal static PngSignificantBits PrepareSignificantBits(PngSignificantBits source, PngPixelFormat format)
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = source.Gray;
        bits.Red = source.Red;
        bits.Green = source.Green;
        bits.Blue = source.Blue;
        bits.Alpha = source.Alpha;
        if ((format == PngPixelFormat.GrayscaleAlpha8 || format == PngPixelFormat.Rgba8) && bits.Alpha == 0)
        {
            bits.Alpha = 8;
        }

        if ((format == PngPixelFormat.GrayscaleAlpha16 || format == PngPixelFormat.Rgba16) && bits.Alpha == 0)
        {
            bits.Alpha = 16;
        }

        return bits;
    }

    internal static PngBackgroundColor ResolveBackground(PngPixelFormat sourceFormat, PngBackgroundColor background, PngMetadata metadata)
    {
        if (!PngDecoderPixelConverter.IsIndexedFormat(sourceFormat))
        {
            return background;
        }

        ArgumentNullException.ThrowIfNull(metadata.Palette);
        ArgumentNullException.ThrowIfNull(metadata.Palette.Entries);
        int paletteIndex = background.PaletteIndex;
        if (paletteIndex >= metadata.Palette.Entries.Length)
        {
            throw new ArgumentOutOfRangeException("PaletteIndex");
        }

        PngPaletteEntry entry = metadata.Palette.Entries[paletteIndex];
        ArgumentNullException.ThrowIfNull(entry);
        PngBackgroundColor resolved = new PngBackgroundColor();
        resolved.Red = entry.Red;
        resolved.Green = entry.Green;
        resolved.Blue = entry.Blue;
        return resolved;
    }

    internal static PngColorTransformResult ApplyColorMetadata(PngPixelBuffer pixels, PngColorMetadata metadata)
    {
        if (!metadata.HasColorMetadata)
        {
            return CreateColorResult(pixels, false, false, new PngWarning[0]);
        }

        if (metadata.HasCodingIndependentCodePoints)
        {
            return ApplyCodingIndependentCodePoints(pixels, metadata.CodingIndependentCodePoints);
        }

        if (metadata.HasIccProfile)
        {
            PngWarning warning = CreateWarning(IccProfileTransformUnavailableCode, "The selected iCCP profile cannot be transformed to sRGB with built-in .NET facilities; pixels remain in the embedded profile color space.", PngConstants.ChunkIccProfile);
            return CreateColorResult(pixels, false, false, new PngWarning[] { warning });
        }

        if (metadata.HasStandardRgb)
        {
            return CreateColorResult(pixels, false, true, new PngWarning[0]);
        }

        return ApplyChromaticitiesGamma(pixels, metadata);
    }

    internal static PngColorTransformResult ApplyCodingIndependentCodePoints(PngPixelBuffer pixels, PngCodingIndependentCodePoints codePoints)
    {
        if (codePoints.MatrixCoefficients != 0 || codePoints.VideoFullRangeFlag != 1 || codePoints.ColorPrimaries != 1)
        {
            PngWarning warning = CreateWarning(UnsupportedCicpTransformCode, "The selected cICP color space requires a range, transfer, or primary conversion that is not available in the sRGB-like normalizer.", PngConstants.ChunkCodingIndependentCodePoints);
            return CreateColorResult(pixels, false, false, new PngWarning[] { warning });
        }

        if (codePoints.TransferCharacteristics == 13)
        {
            return CreateColorResult(pixels, false, true, new PngWarning[0]);
        }

        if (codePoints.TransferCharacteristics == 8)
        {
            PngPixelBuffer transformed = ApplyGamma(pixels, 1.0);
            return CreateColorResult(transformed, true, true, new PngWarning[0]);
        }

        if (codePoints.TransferCharacteristics == 1)
        {
            PngPixelBuffer transformed = ApplyGamma(pixels, 0.45);
            return CreateColorResult(transformed, true, true, new PngWarning[0]);
        }

        PngWarning unsupported = CreateWarning(UnsupportedCicpTransformCode, "The selected cICP transfer characteristic is not available in the sRGB-like normalizer.", PngConstants.ChunkCodingIndependentCodePoints);
        return CreateColorResult(pixels, false, false, new PngWarning[] { unsupported });
    }

    internal static PngColorTransformResult ApplyChromaticitiesGamma(PngPixelBuffer pixels, PngColorMetadata metadata)
    {
        List<PngWarning> warnings = new List<PngWarning>();
        bool chromaticitiesSupported = true;
        if (metadata.HasChromaticities && !IsStandardRgbChromaticities(metadata.Chromaticities))
        {
            chromaticitiesSupported = false;
            warnings.Add(CreateWarning(ChromaticitiesTransformUnavailableCode, "The selected cHRM primaries are not sRGB primaries; gamma was handled but a primary-color transform was not applied.", PngConstants.ChunkChromaticities));
        }

        if (!metadata.HasGamma)
        {
            warnings.Add(CreateWarning(GammaMetadataUnavailableCode, "The selected cHRM/gAMA tier does not provide a gAMA value, so its transfer function cannot be normalized.", PngConstants.ChunkGamma));
            return CreateColorResult(pixels, false, false, warnings.ToArray());
        }

        double imageGamma = metadata.Gamma.Value / PngGammaCorrection.PngGammaScale;
        PngPixelBuffer transformed = ApplyGamma(pixels, imageGamma);
        return CreateColorResult(transformed, imageGamma != StandardRgbGamma, chromaticitiesSupported, warnings.ToArray());
    }

    internal static PngPixelBuffer ApplyGamma(PngPixelBuffer pixels, double imageGamma)
    {
        if (Is16BitFormat(pixels.Format))
        {
            ushort[] table = PngGammaCorrection.Build16BitTable(imageGamma, 1.0, StandardRgbGamma);
            return PngGammaCorrection.Apply16BitTable(pixels, table);
        }

        byte[] table8 = PngGammaCorrection.Build8BitTable(imageGamma, 1.0, StandardRgbGamma);
        return PngGammaCorrection.Apply8BitTable(pixels, table8);
    }

    internal static bool IsStandardRgbChromaticities(PngChromaticities chromaticities)
    {
        return chromaticities.WhitePointX == 31270
            && chromaticities.WhitePointY == 32900
            && chromaticities.RedX == 64000
            && chromaticities.RedY == 33000
            && chromaticities.GreenX == 30000
            && chromaticities.GreenY == 60000
            && chromaticities.BlueX == 15000
            && chromaticities.BlueY == 6000;
    }

    internal static bool HasAlpha(PngPixelFormat format)
    {
        return format == PngPixelFormat.GrayscaleAlpha8
            || format == PngPixelFormat.GrayscaleAlpha16
            || format == PngPixelFormat.Rgba8
            || format == PngPixelFormat.Rgba16;
    }

    internal static bool Is16BitFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale16
            || format == PngPixelFormat.GrayscaleAlpha16
            || format == PngPixelFormat.Rgb16
            || format == PngPixelFormat.Rgba16;
    }

    internal static PngWarning CreateWarning(string code, string message, string chunkType)
    {
        PngWarning warning = new PngWarning();
        warning.Code = code;
        warning.Message = message;
        warning.ChunkType = chunkType;
        return warning;
    }

    internal static PngColorTransformResult CreateColorResult(PngPixelBuffer pixels, bool appliedGammaTransform, bool isSrgbLike, PngWarning[] warnings)
    {
        PngColorTransformResult result = new PngColorTransformResult();
        result.Pixels = pixels;
        result.AppliedGammaTransform = appliedGammaTransform;
        result.IsSrgbLike = isSrgbLike;
        result.Warnings = warnings;
        return result;
    }

    internal static void AddWarnings(List<PngWarning> warnings, PngWarning[] additions)
    {
        int index = 0;
        while (index < additions.Length)
        {
            warnings.Add(additions[index]);
            index += 1;
        }
    }
}

internal class PngColorTransformResult
{
    internal PngPixelBuffer Pixels = new PngPixelBuffer();
    internal PngWarning[] Warnings = new PngWarning[0];
    internal bool AppliedGammaTransform;
    internal bool IsSrgbLike;
}
