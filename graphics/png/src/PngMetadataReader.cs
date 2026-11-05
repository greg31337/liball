using System.Collections.Generic;
using System.Text;

namespace png;

internal class PngMetadataReader
{
    internal static PngMetadata Read(PngChunk[] chunks, PngChunk[] preservedChunks, PngColorType colorType, PngDecoderOptions options, bool tolerateMalformedText)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(preservedChunks);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngMetadata metadata = new PngMetadata();
        metadata.Chunks = preservedChunks;
        List<PngChunk> unknownChunks = new List<PngChunk>();
        List<PngTextEntry> textEntries = new List<PngTextEntry>();
        List<PngText> latin1TextEntries = new List<PngText>();
        List<PngCompressedText> compressedTextEntries = new List<PngCompressedText>();
        List<PngInternationalText> internationalTextEntries = new List<PngInternationalText>();
        List<PngSuggestedPalette> suggestedPalettes = new List<PngSuggestedPalette>();
        List<PngFrameControl> frameControls = new List<PngFrameControl>();
        List<PngFrameData> frameData = new List<PngFrameData>();

        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);
            ArgumentNullException.ThrowIfNull(chunk.Data);

            if (!chunk.IsKnown && options.PreserveUnknownChunks)
            {
                unknownChunks.Add(chunk);
            }

            if (PngChunkTypeFlags.IsAncillary(chunk.Type) && chunk.Type != PngConstants.ChunkFrameData)
            {
                metadata.MetadataBytes = PngDecoder.AddLimited(metadata.MetadataBytes, chunk.Data.Length, options.Limits.MaxMetadataBytes);
            }

            if (chunk.Type == PngConstants.ChunkPLTE)
            {
                metadata.Palette = PngPlteParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkTransparency)
            {
                ReadTransparency(metadata, chunk, colorType);
            }
            else if (chunk.Type == PngConstants.ChunkChromaticities)
            {
                metadata.Chromaticities = PngChromaticitiesParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkGamma)
            {
                metadata.Gamma = PngGammaParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkIccProfile)
            {
                metadata.IccProfile = PngIccProfileParser.Parse(chunk, options.Limits);
            }
            else if (chunk.Type == PngConstants.ChunkSignificantBits)
            {
                metadata.SignificantBits = ReadSignificantBits(chunk, colorType);
            }
            else if (chunk.Type == PngConstants.ChunkStandardRgb)
            {
                metadata.StandardRgb = PngStandardRgbParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkCodingIndependentCodePoints)
            {
                metadata.CodingIndependentCodePoints = PngCodingIndependentCodePointsParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkMasteringDisplayColorVolume)
            {
                metadata.MasteringDisplayColorVolume = PngMasteringDisplayColorVolumeParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkContentLightLevelInformation)
            {
                metadata.ContentLightLevelInformation = PngContentLightLevelInformationParser.Parse(chunk);
            }
            else if (PngIdentifier.IsTextChunk(chunk.Type))
            {
                ReadTextChunk(metadata, chunk, index, options.Limits, tolerateMalformedText, textEntries, latin1TextEntries, compressedTextEntries, internationalTextEntries);
            }
            else if (chunk.Type == PngConstants.ChunkBackgroundColor)
            {
                metadata.BackgroundColor = ReadBackgroundColor(chunk, colorType);
            }
            else if (chunk.Type == PngConstants.ChunkPaletteHistogram)
            {
                metadata.PaletteHistogram = PngPaletteHistogramParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkPhysicalPixels)
            {
                metadata.PhysicalPixels = PngPhysicalPixelsParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkSuggestedPalette)
            {
                suggestedPalettes.Add(PngSuggestedPaletteParser.Parse(chunk));
            }
            else if (chunk.Type == PngConstants.ChunkExif)
            {
                metadata.ExifProfile = PngExifProfileParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkTime)
            {
                metadata.Time = PngTimeParser.Parse(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkAnimationControl)
            {
                if (!tolerateMalformedText || CanParseAnimationControl(chunk))
                {
                    metadata.AnimationControl = PngAnimationControlParser.Parse(chunk);
                    PngApngAnimationLimitEnforcer.EnforceFrameCount(metadata.AnimationControl.FrameCount, options.Limits);
                }
            }
            else if (chunk.Type == PngConstants.ChunkFrameControl)
            {
                frameControls.Add(PngFrameControlParser.Parse(chunk));
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                frameData.Add(PngFrameDataParser.Parse(chunk));
            }

            index += 1;
        }

        metadata.UnknownChunks = unknownChunks.ToArray();
        metadata.TextEntries = textEntries.ToArray();
        metadata.Latin1TextEntries = latin1TextEntries.ToArray();
        metadata.CompressedTextEntries = compressedTextEntries.ToArray();
        metadata.InternationalTextEntries = internationalTextEntries.ToArray();
        metadata.SuggestedPalettes = suggestedPalettes.ToArray();
        metadata.FrameControls = frameControls.ToArray();
        metadata.FrameData = frameData.ToArray();
        metadata.ColorMetadata = PngColorMetadataPrecedenceResolver.Resolve(chunks, options.Limits);
        return metadata;
    }

    internal static void ReadTransparency(PngMetadata metadata, PngChunk chunk, PngColorType colorType)
    {
        if (colorType == PngColorType.IndexedColor)
        {
            metadata.IndexedTransparency = PngTransparencyParser.ParseIndexed(chunk);
        }
        else if (colorType == PngColorType.Grayscale)
        {
            metadata.GrayscaleTransparency = PngTransparencyParser.ParseGrayscale(chunk);
        }
        else if (colorType == PngColorType.Truecolor)
        {
            metadata.TruecolorTransparency = PngTransparencyParser.ParseTruecolor(chunk);
        }
    }

    internal static PngSignificantBits ReadSignificantBits(PngChunk chunk, PngColorType colorType)
    {
        if (colorType == PngColorType.Grayscale)
        {
            return PngSignificantBitsParser.ParseGrayscale(chunk);
        }

        if (colorType == PngColorType.GrayscaleAlpha)
        {
            return PngSignificantBitsParser.ParseGrayscaleAlpha(chunk);
        }

        if (colorType == PngColorType.IndexedColor)
        {
            return PngSignificantBitsParser.ParseIndexed(chunk);
        }

        if (colorType == PngColorType.Truecolor)
        {
            return PngSignificantBitsParser.ParseTruecolor(chunk);
        }

        return PngSignificantBitsParser.ParseTruecolorAlpha(chunk);
    }

    internal static PngBackgroundColor ReadBackgroundColor(PngChunk chunk, PngColorType colorType)
    {
        if (colorType == PngColorType.IndexedColor)
        {
            return PngBackgroundColorParser.ParseIndexed(chunk);
        }

        if (colorType == PngColorType.Grayscale || colorType == PngColorType.GrayscaleAlpha)
        {
            return PngBackgroundColorParser.ParseGrayscale(chunk);
        }

        return PngBackgroundColorParser.ParseTruecolor(chunk);
    }

    internal static void ReadTextChunk(PngMetadata metadata, PngChunk chunk, int originalChunkIndex, PngSecurityLimits limits, bool tolerateMalformedText, List<PngTextEntry> textEntries, List<PngText> latin1TextEntries, List<PngCompressedText> compressedTextEntries, List<PngInternationalText> internationalTextEntries)
    {
        metadata.TextEntryCount += 1;
        if (limits.MaxTextEntries > 0 && metadata.TextEntryCount > limits.MaxTextEntries)
        {
            throw new OverflowException();
        }

        if (tolerateMalformedText && !CanParseTextChunk(chunk))
        {
            metadata.TextBytes = PngDecoder.AddLimited(metadata.TextBytes, chunk.Data.Length, limits.MaxTextBytes);
            return;
        }

        PngSecurityLimits textLimits = CreateTextLimits(limits, metadata.TextBytes);
        if (chunk.Type == PngConstants.ChunkText)
        {
            PngText text = PngTextParser.Parse(chunk);
            latin1TextEntries.Add(text);
            textEntries.Add(PngTextModelConverter.CreateLatin1Entry(text, originalChunkIndex));
            metadata.TextBytes = PngDecoder.AddLimited(metadata.TextBytes, text.Text.Length, limits.MaxTextBytes);
        }
        else if (chunk.Type == PngConstants.ChunkCompressedText)
        {
            PngCompressedText text = PngCompressedTextParser.Parse(chunk, textLimits);
            compressedTextEntries.Add(text);
            textEntries.Add(PngTextModelConverter.CreateCompressedLatin1Entry(text, originalChunkIndex));
            metadata.TextBytes = PngDecoder.AddLimited(metadata.TextBytes, text.Text.Length, limits.MaxTextBytes);
        }
        else
        {
            PngInternationalText text = PngInternationalTextParser.Parse(chunk, textLimits);
            internationalTextEntries.Add(text);
            textEntries.Add(PngTextModelConverter.CreateInternationalEntry(text, originalChunkIndex));
            metadata.TextBytes = PngDecoder.AddLimited(metadata.TextBytes, Encoding.UTF8.GetByteCount(text.Text), limits.MaxTextBytes);
        }
    }

    internal static PngSecurityLimits CreateTextLimits(PngSecurityLimits source, long existingTextBytes)
    {
        PngSecurityLimits result = PngDecoder.CopyLimits(source);
        if (source.MaxTextBytes > 0)
        {
            if (existingTextBytes >= source.MaxTextBytes)
            {
                result.MaxDecompressedBytes = 1;
            }
            else
            {
                result.MaxDecompressedBytes = source.MaxTextBytes - existingTextBytes;
            }
        }

        return result;
    }

    internal static bool CanParseTextChunk(PngChunk chunk)
    {
        int separator = FindNullByte(chunk.Data, 0);
        if (separator < 1 || separator > PngTextKeywordValidator.MaximumKeywordLength)
        {
            return false;
        }

        if (chunk.Type == PngConstants.ChunkText)
        {
            return true;
        }

        if (chunk.Type == PngConstants.ChunkCompressedText)
        {
            return separator + 1 < chunk.Data.Length;
        }

        int languageStart = separator + 3;
        if (languageStart > chunk.Data.Length)
        {
            return false;
        }

        int languageSeparator = FindNullByte(chunk.Data, languageStart);
        if (languageSeparator < 0)
        {
            return false;
        }

        int translatedSeparator = FindNullByte(chunk.Data, languageSeparator + 1);
        return translatedSeparator >= 0;
    }

    internal static int FindNullByte(byte[] data, int offset)
    {
        int index = offset;
        while (index < data.Length)
        {
            if (data[index] == 0)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static bool CanParseAnimationControl(PngChunk chunk)
    {
        if (chunk.Data.Length != 8)
        {
            return false;
        }

        return PngBinaryReader.ReadUInt32(chunk.Data, 0) != 0;
    }
}
