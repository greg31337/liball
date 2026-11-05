using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngEncoderPaletteColor
{
    internal byte Red;
    internal byte Green;
    internal byte Blue;
    internal byte Alpha;
}

internal class PngEncoderPaletteData
{
    internal PngPalette Palette = new PngPalette();
    internal PngIndexedTransparency? Transparency;
}

internal class PngEncoderCandidate
{
    internal PngPixelFormat Format;
    internal PngPixelBuffer[] Buffers = new PngPixelBuffer[0];
    internal PngPalette? Palette;
    internal PngIndexedTransparency? Transparency;
    internal int BitsPerPixel;
    internal int Rank;
    internal long EstimatedByteCount;
}

internal class PngEncoderContentAnalyzer
{
    static readonly PngPixelFormat[] candidateFormats = new PngPixelFormat[]
    {
        PngPixelFormat.Grayscale1,
        PngPixelFormat.Indexed1,
        PngPixelFormat.Grayscale2,
        PngPixelFormat.Indexed2,
        PngPixelFormat.Grayscale4,
        PngPixelFormat.Indexed4,
        PngPixelFormat.Grayscale8,
        PngPixelFormat.Indexed8,
        PngPixelFormat.GrayscaleAlpha8,
        PngPixelFormat.Rgb8,
        PngPixelFormat.Rgba8,
        PngPixelFormat.Grayscale16,
        PngPixelFormat.GrayscaleAlpha16,
        PngPixelFormat.Rgb16,
        PngPixelFormat.Rgba16
    };

    internal static PngImage Analyze(PngImage image, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(image.Pixels.Data);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(options);

        ValidateExplicitChoices(options);
        if (!options.UseAutomaticColorType && !options.UseAutomaticBitDepth)
        {
            return image;
        }

        PngPixelBuffer[] sources = CollectSourceBuffers(image, options);
        ValidateSourceSizes(sources);
        bool preserveIndexedColor = SourceFormatsAreIndexed(sources);
        PngPixelBuffer[] canonicalBuffers = CreateCanonicalBuffers(sources, image.Metadata);
        PngEncoderPaletteData? paletteData;
        if (preserveIndexedColor)
        {
            paletteData = CreatePreservedPaletteData(sources, image.Metadata, options.AllowPaletteReduction);
        }
        else
        {
            paletteData = CreateExactPalette(canonicalBuffers);
        }
        PngEncoderCandidate? best = null;

        int formatIndex = 0;
        while (formatIndex < candidateFormats.Length)
        {
            PngPixelFormat format = candidateFormats[formatIndex];
            if (IsCandidateAllowed(format, options, preserveIndexedColor) && CanUsePalette(format, paletteData))
            {
                PngEncoderCandidate candidate = CreateCandidate(format, canonicalBuffers, paletteData);
                if (CandidateIsExact(candidate, canonicalBuffers) && IsBetter(candidate, best))
                {
                    best = candidate;
                }
            }

            formatIndex += 1;
        }

        if (best == null)
        {
            throw new InvalidDataException("The requested PNG color type and bit depth cannot represent the pixels losslessly.");
        }

        return CreateAnalyzedImage(image, options, best);
    }

    internal static void ValidateExplicitChoices(PngEncoderOptions options)
    {
        if (!options.UseAutomaticColorType)
        {
            PngEncoder.GetColorType(PngIhdrValidator.GetPixelFormat(options.ColorType, PngBitDepth.Eight));
        }

        if (!options.UseAutomaticBitDepth)
        {
            PngSizeCalculator.GetBitDepthValue(options.BitDepth);
        }

        if (!options.UseAutomaticColorType && !options.UseAutomaticBitDepth && !PngIhdrValidator.IsBitDepthAllowedForColorType(options.BitDepth, options.ColorType))
        {
            throw new ArgumentOutOfRangeException("BitDepth");
        }
    }

    internal static PngPixelBuffer[] CollectSourceBuffers(PngImage image, PngEncoderOptions options)
    {
        PngFrame[] frames = new PngFrame[0];
        if (options.EncodeAnimation && image.Animation != null && image.Animation.Frames != null)
        {
            frames = image.Animation.Frames;
        }

        PngPixelBuffer[] sources = new PngPixelBuffer[frames.Length + 1];
        sources[0] = image.Pixels;
        int index = 0;
        while (index < frames.Length)
        {
            PngFrame frame = frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(frame.Pixels);
            ArgumentNullException.ThrowIfNull(frame.Pixels.Data);
            sources[index + 1] = frame.Pixels;
            index += 1;
        }

        return sources;
    }

    internal static bool SourceFormatsAreIndexed(PngPixelBuffer[] sources)
    {
        int index = 0;
        while (index < sources.Length)
        {
            if (!PngPaletteIndexPixelConverter.IsIndexedFormat(sources[index].Format))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static void ValidateSourceSizes(PngPixelBuffer[] sources)
    {
        int index = 0;
        while (index < sources.Length)
        {
            PngPixelBuffer buffer = sources[index];
            long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(buffer.Width, buffer.Format);
            long imageBytes = PngSizeCalculator.CalculateFrameBytes(buffer.Width, buffer.Height, buffer.Format);
            if (rowBytes > int.MaxValue || imageBytes > int.MaxValue)
            {
                throw new OverflowException();
            }

            index += 1;
        }
    }

    internal static PngPixelBuffer[] CreateCanonicalBuffers(PngPixelBuffer[] sources, PngMetadata metadata)
    {
        PngPixelBuffer[] canonicalBuffers = new PngPixelBuffer[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            PngPixelConversionOptions conversion = new PngPixelConversionOptions();
            conversion.TargetFormat = PngPixelFormat.Rgba16;
            conversion.Palette = metadata.Palette;
            conversion.IndexedTransparency = metadata.IndexedTransparency;
            conversion.GrayscaleTransparency = metadata.GrayscaleTransparency;
            conversion.TruecolorTransparency = metadata.TruecolorTransparency;
            canonicalBuffers[index] = PngPixelFormatConverter.Convert(sources[index], conversion);
            index += 1;
        }

        return canonicalBuffers;
    }

    internal static PngEncoderPaletteData? CreateExactPalette(PngPixelBuffer[] canonicalBuffers)
    {
        List<PngEncoderPaletteColor> colors = new List<PngEncoderPaletteColor>();
        int bufferIndex = 0;
        while (bufferIndex < canonicalBuffers.Length)
        {
            PngPixelBuffer buffer = canonicalBuffers[bufferIndex];
            int y = 0;
            while (y < buffer.Height)
            {
                int offset = y * buffer.Stride;
                int x = 0;
                while (x < buffer.Width)
                {
                    if (!PixelIsEightBitRepresentable(buffer.Data, offset))
                    {
                        return null;
                    }

                    byte red = buffer.Data[offset];
                    byte green = buffer.Data[offset + 2];
                    byte blue = buffer.Data[offset + 4];
                    byte alpha = buffer.Data[offset + 6];
                    if (!AddPaletteColor(colors, red, green, blue, alpha))
                    {
                        return null;
                    }

                    offset += 8;
                    x += 1;
                }

                y += 1;
            }

            bufferIndex += 1;
        }

        return CreatePaletteData(colors);
    }

    internal static PngEncoderPaletteData CreatePreservedPaletteData(PngPixelBuffer[] sources, PngMetadata metadata, bool allowReduction)
    {
        ArgumentNullException.ThrowIfNull(metadata.Palette);
        ArgumentNullException.ThrowIfNull(metadata.Palette.Entries);
        int highestIndex = 0;
        int bufferIndex = 0;
        while (bufferIndex < sources.Length)
        {
            PngPixelBuffer indices = PngPaletteIndexPixelConverter.ToIndexed8(sources[bufferIndex]);
            int bufferHighestIndex = PngPaletteTrimmer.FindHighestIndex(indices);
            if (bufferHighestIndex > highestIndex)
            {
                highestIndex = bufferHighestIndex;
            }

            bufferIndex += 1;
        }

        int entryCount = metadata.Palette.Entries.Length;
        if (allowReduction)
        {
            entryCount = highestIndex + 1;
        }
        if (entryCount > metadata.Palette.Entries.Length)
        {
            throw new InvalidDataException("A palette index does not have a corresponding palette entry.");
        }

        PngEncoderPaletteData data = new PngEncoderPaletteData();
        data.Palette = PngPaletteTrimmer.CopyEntries(metadata.Palette, entryCount);
        if (metadata.IndexedTransparency != null)
        {
            ArgumentNullException.ThrowIfNull(metadata.IndexedTransparency.Alpha);
            int alphaCount = metadata.IndexedTransparency.Alpha.Length;
            if (alphaCount > entryCount)
            {
                alphaCount = entryCount;
            }

            while (allowReduction && alphaCount > 0 && metadata.IndexedTransparency.Alpha[alphaCount - 1] == byte.MaxValue)
            {
                alphaCount -= 1;
            }

            if (alphaCount > 0)
            {
                PngIndexedTransparency transparency = new PngIndexedTransparency();
                transparency.Alpha = new byte[alphaCount];
                int index = 0;
                while (index < alphaCount)
                {
                    transparency.Alpha[index] = metadata.IndexedTransparency.Alpha[index];
                    index += 1;
                }

                data.Transparency = transparency;
            }
        }

        return data;
    }

    internal static bool PixelIsEightBitRepresentable(byte[] data, int offset)
    {
        return data[offset] == data[offset + 1]
            && data[offset + 2] == data[offset + 3]
            && data[offset + 4] == data[offset + 5]
            && data[offset + 6] == data[offset + 7];
    }

    internal static bool AddPaletteColor(List<PngEncoderPaletteColor> colors, byte red, byte green, byte blue, byte alpha)
    {
        int index = 0;
        while (index < colors.Count)
        {
            PngEncoderPaletteColor color = colors[index];
            if (color.Red == red && color.Green == green && color.Blue == blue && color.Alpha == alpha)
            {
                return true;
            }

            index += 1;
        }

        if (colors.Count == 256)
        {
            return false;
        }

        PngEncoderPaletteColor added = new PngEncoderPaletteColor();
        added.Red = red;
        added.Green = green;
        added.Blue = blue;
        added.Alpha = alpha;
        colors.Add(added);
        return true;
    }

    internal static PngEncoderPaletteData CreatePaletteData(List<PngEncoderPaletteColor> colors)
    {
        PngEncoderPaletteData data = new PngEncoderPaletteData();
        data.Palette.Entries = new PngPaletteEntry[colors.Count];
        int lastTransparentIndex = -1;
        int index = 0;
        while (index < colors.Count)
        {
            PngEncoderPaletteColor color = colors[index];
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = color.Red;
            entry.Green = color.Green;
            entry.Blue = color.Blue;
            data.Palette.Entries[index] = entry;
            if (color.Alpha != byte.MaxValue)
            {
                lastTransparentIndex = index;
            }

            index += 1;
        }

        if (lastTransparentIndex >= 0)
        {
            PngIndexedTransparency transparency = new PngIndexedTransparency();
            transparency.Alpha = new byte[lastTransparentIndex + 1];
            index = 0;
            while (index <= lastTransparentIndex)
            {
                transparency.Alpha[index] = colors[index].Alpha;
                index += 1;
            }

            data.Transparency = transparency;
        }

        return data;
    }

    internal static bool IsCandidateAllowed(PngPixelFormat format, PngEncoderOptions options, bool preserveIndexedColor)
    {
        if (preserveIndexedColor && PngEncoder.GetColorType(format) != PngColorType.IndexedColor)
        {
            return false;
        }

        if (!options.UseAutomaticColorType && PngEncoder.GetColorType(format) != options.ColorType)
        {
            return false;
        }

        if (!options.UseAutomaticBitDepth && PngEncoder.GetBitDepth(format) != options.BitDepth)
        {
            return false;
        }

        return true;
    }

    internal static bool CanUsePalette(PngPixelFormat format, PngEncoderPaletteData? paletteData)
    {
        if (!PngPaletteIndexPixelConverter.IsIndexedFormat(format))
        {
            return true;
        }

        if (paletteData == null)
        {
            return false;
        }

        int capacity = 1 << PngSizeCalculator.GetPixelFormatBitsPerPixel(format);
        return paletteData.Palette.Entries.Length <= capacity;
    }

    internal static PngEncoderCandidate CreateCandidate(PngPixelFormat format, PngPixelBuffer[] canonicalBuffers, PngEncoderPaletteData? paletteData)
    {
        PngEncoderCandidate candidate = new PngEncoderCandidate();
        candidate.Format = format;
        candidate.BitsPerPixel = PngSizeCalculator.GetPixelFormatBitsPerPixel(format);
        candidate.Rank = GetCandidateRank(format);
        if (PngPaletteIndexPixelConverter.IsIndexedFormat(format))
        {
            candidate.Palette = paletteData == null ? null : paletteData.Palette;
            candidate.Transparency = paletteData == null ? null : paletteData.Transparency;
        }

        candidate.Buffers = new PngPixelBuffer[canonicalBuffers.Length];
        int index = 0;
        while (index < canonicalBuffers.Length)
        {
            PngPixelConversionOptions conversion = new PngPixelConversionOptions();
            conversion.TargetFormat = format;
            conversion.Palette = candidate.Palette;
            conversion.IndexedTransparency = candidate.Transparency;
            candidate.Buffers[index] = PngPixelFormatConverter.Convert(canonicalBuffers[index], conversion);
            long bufferBytes = PngSizeCalculator.CalculateFrameBytes(candidate.Buffers[index].Width, candidate.Buffers[index].Height, format);
            if (candidate.EstimatedByteCount > long.MaxValue - bufferBytes)
            {
                throw new OverflowException();
            }

            candidate.EstimatedByteCount += bufferBytes;
            index += 1;
        }

        if (candidate.Palette != null)
        {
            candidate.EstimatedByteCount += 12 + candidate.Palette.Entries.Length * 3;
            if (candidate.Transparency != null)
            {
                candidate.EstimatedByteCount += 12 + candidate.Transparency.Alpha.Length;
            }
        }

        return candidate;
    }

    internal static bool CandidateIsExact(PngEncoderCandidate candidate, PngPixelBuffer[] canonicalBuffers)
    {
        int index = 0;
        while (index < candidate.Buffers.Length)
        {
            PngPixelConversionOptions conversion = new PngPixelConversionOptions();
            conversion.TargetFormat = PngPixelFormat.Rgba16;
            conversion.Palette = candidate.Palette;
            conversion.IndexedTransparency = candidate.Transparency;
            PngPixelBuffer roundTrip = PngPixelFormatConverter.Convert(candidate.Buffers[index], conversion);
            if (!BuffersEqual(roundTrip, canonicalBuffers[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool BuffersEqual(PngPixelBuffer first, PngPixelBuffer second)
    {
        if (first.Format != second.Format || first.Width != second.Width || first.Height != second.Height)
        {
            return false;
        }

        int rowBytes = (int)PngSizeCalculator.CalculateFrameRowBytes(first.Width, first.Format);
        int y = 0;
        while (y < first.Height)
        {
            int x = 0;
            while (x < rowBytes)
            {
                if (first.Data[y * first.Stride + x] != second.Data[y * second.Stride + x])
                {
                    return false;
                }

                x += 1;
            }

            y += 1;
        }

        return true;
    }

    internal static bool IsBetter(PngEncoderCandidate candidate, PngEncoderCandidate? best)
    {
        if (best == null)
        {
            return true;
        }

        if (candidate.EstimatedByteCount != best.EstimatedByteCount)
        {
            return candidate.EstimatedByteCount < best.EstimatedByteCount;
        }

        if (candidate.BitsPerPixel != best.BitsPerPixel)
        {
            return candidate.BitsPerPixel < best.BitsPerPixel;
        }

        return candidate.Rank < best.Rank;
    }

    internal static int GetCandidateRank(PngPixelFormat format)
    {
        PngColorType colorType = PngEncoder.GetColorType(format);
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return 0;
            case PngColorType.IndexedColor:
                return 1;
            case PngColorType.GrayscaleAlpha:
                return 2;
            case PngColorType.Truecolor:
                return 3;
            case PngColorType.TruecolorAlpha:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("format");
        }
    }

    internal static PngImage CreateAnalyzedImage(PngImage source, PngEncoderOptions options, PngEncoderCandidate candidate)
    {
        PngImage result = new PngImage();
        result.Info = source.Info;
        result.Pixels = candidate.Buffers[0];
        result.Metadata = CopyMetadata(source.Metadata);
        result.Warnings = source.Warnings;
        result.Errors = source.Errors;
        if (candidate.Palette != null)
        {
            result.Metadata.Palette = candidate.Palette;
            result.Metadata.IndexedTransparency = candidate.Transparency;
        }
        else
        {
            result.Metadata.IndexedTransparency = null;
        }

        result.Metadata.GrayscaleTransparency = null;
        result.Metadata.TruecolorTransparency = null;
        result.Animation = CopyAnimation(source.Animation, options, candidate.Buffers);
        return result;
    }

    internal static PngAnimation CopyAnimation(PngAnimation animation, PngEncoderOptions options, PngPixelBuffer[] buffers)
    {
        PngAnimation result = new PngAnimation();
        if (!options.EncodeAnimation || animation == null || animation.Frames == null || animation.Frames.Length == 0)
        {
            return result;
        }

        result.FrameCount = animation.FrameCount;
        result.PlayCount = animation.PlayCount;
        result.Frames = new PngFrame[animation.Frames.Length];
        int index = 0;
        while (index < animation.Frames.Length)
        {
            PngFrame source = animation.Frames[index];
            PngFrame frame = new PngFrame();
            frame.Width = source.Width;
            frame.Height = source.Height;
            frame.XOffset = source.XOffset;
            frame.YOffset = source.YOffset;
            frame.DelayNumerator = source.DelayNumerator;
            frame.DelayDenominator = source.DelayDenominator;
            frame.BlendOperation = source.BlendOperation;
            frame.DisposeOperation = source.DisposeOperation;
            frame.Pixels = buffers[index + 1];
            frame.UsesDefaultImage = source.UsesDefaultImage;
            result.Frames[index] = frame;
            index += 1;
        }

        return result;
    }

    internal static PngMetadata CopyMetadata(PngMetadata source)
    {
        PngMetadata copy = new PngMetadata();
        copy.Chunks = CopyMetadataChunks(source.Chunks);
        copy.UnknownChunks = source.UnknownChunks;
        copy.Palette = source.Palette;
        copy.IndexedTransparency = source.IndexedTransparency;
        copy.GrayscaleTransparency = source.GrayscaleTransparency;
        copy.TruecolorTransparency = source.TruecolorTransparency;
        copy.Chromaticities = source.Chromaticities;
        copy.Gamma = source.Gamma;
        copy.IccProfile = source.IccProfile;
        copy.SignificantBits = source.SignificantBits;
        copy.StandardRgb = source.StandardRgb;
        copy.CodingIndependentCodePoints = source.CodingIndependentCodePoints;
        copy.ColorMetadata = source.ColorMetadata;
        copy.MasteringDisplayColorVolume = source.MasteringDisplayColorVolume;
        copy.ContentLightLevelInformation = source.ContentLightLevelInformation;
        copy.TextEntries = source.TextEntries;
        copy.Latin1TextEntries = source.Latin1TextEntries;
        copy.CompressedTextEntries = source.CompressedTextEntries;
        copy.InternationalTextEntries = source.InternationalTextEntries;
        copy.BackgroundColor = source.BackgroundColor;
        copy.PaletteHistogram = source.PaletteHistogram;
        copy.PhysicalPixels = source.PhysicalPixels;
        copy.SuggestedPalettes = source.SuggestedPalettes;
        copy.ExifProfile = source.ExifProfile;
        copy.Time = source.Time;
        copy.AnimationControl = source.AnimationControl;
        copy.FrameControls = source.FrameControls;
        copy.FrameData = source.FrameData;
        copy.MetadataBytes = source.MetadataBytes;
        copy.TextBytes = source.TextBytes;
        copy.TextEntryCount = source.TextEntryCount;
        return copy;
    }

    internal static PngChunk[] CopyMetadataChunks(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        List<PngChunk> retained = new List<PngChunk>();
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            if (chunk.Type != PngConstants.ChunkTransparency
                && chunk.Type != PngConstants.ChunkPaletteHistogram
                && chunk.Type != PngConstants.ChunkSignificantBits)
            {
                retained.Add(chunk);
            }

            index += 1;
        }

        return retained.ToArray();
    }
}
