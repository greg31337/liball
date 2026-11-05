using System.Collections.Generic;
using System.Text;

namespace png;

internal class PngMetadataEncodingProcessor
{
    internal static PngMetadata? Prepare(PngMetadata? metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(options);
        if (metadata == null)
        {
            return null;
        }

        if (!PngEncoder.ShouldPreserveMetadata(options))
        {
            return new PngMetadata();
        }

        ArgumentNullException.ThrowIfNull(metadata.Chunks);
        ArgumentNullException.ThrowIfNull(metadata.UnknownChunks);
        ArgumentNullException.ThrowIfNull(metadata.Latin1TextEntries);
        ArgumentNullException.ThrowIfNull(metadata.CompressedTextEntries);
        ArgumentNullException.ThrowIfNull(metadata.InternationalTextEntries);
        ArgumentNullException.ThrowIfNull(metadata.TextEntries);
        ArgumentNullException.ThrowIfNull(metadata.SuggestedPalettes);

        List<PngChunk> chunks = new List<PngChunk>();
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            PngMetadataEditor.ValidateChunk(metadata.Chunks[index]);
            chunks.Add(PngEncoder.CopyChunk(metadata.Chunks[index]));
            index += 1;
        }

        index = 0;
        while (index < metadata.UnknownChunks.Length)
        {
            PngChunk unknownChunk = metadata.UnknownChunks[index];
            PngMetadataEditor.ValidateChunk(unknownChunk);
            if (!ContainsEquivalentSourceChunk(metadata.Chunks, unknownChunk))
            {
                chunks.Add(PngEncoder.CopyChunk(unknownChunk));
            }

            index += 1;
        }

        AddColorMetadata(chunks, metadata, options);
        AddTransparency(chunks, metadata, ihdr, options);
        AddSignificantBits(chunks, metadata, ihdr, options);
        AddHdrMetadata(chunks, metadata, options);
        AddBackground(chunks, metadata, ihdr, options);
        AddPaletteHistogram(chunks, metadata, ihdr, options);
        AddPhysicalPixels(chunks, metadata, options);
        AddSuggestedPalettes(chunks, metadata, options);
        AddExifProfile(chunks, metadata, options);
        AddTime(chunks, metadata, options);
        AddTextMetadata(chunks, metadata, options);

        PngMetadata prepared = new PngMetadata();
        prepared.Chunks = chunks.ToArray();
        PngSecurityLimitEnforcer.EnforceChunkAggregates(prepared.Chunks, options.Limits);
        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.Limits = options.Limits;
        PngMetadataReader.Read(prepared.Chunks, prepared.Chunks, ihdr.ColorType, decoderOptions, false);
        return prepared;
    }

    internal static void AddColorMetadata(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (ContainsSelectedColorChunk(chunks, options))
        {
            return;
        }

        if (metadata.CodingIndependentCodePoints != null && CanMaterialize(PngConstants.ChunkCodingIndependentCodePoints, options))
        {
            chunks.Add(PngCodingIndependentCodePointsWriter.WriteChunk(metadata.CodingIndependentCodePoints));
            return;
        }

        if (metadata.IccProfile != null && CanMaterialize(PngConstants.ChunkIccProfile, options))
        {
            chunks.Add(PngIccProfileWriter.WriteChunk(metadata.IccProfile));
            return;
        }

        if (metadata.StandardRgb != null && CanMaterialize(PngConstants.ChunkStandardRgb, options))
        {
            chunks.Add(PngStandardRgbWriter.WriteChunk(metadata.StandardRgb));
            return;
        }

        if (metadata.Chromaticities != null && CanMaterialize(PngConstants.ChunkChromaticities, options))
        {
            chunks.Add(PngChromaticitiesWriter.WriteChunk(metadata.Chromaticities));
        }

        if (metadata.Gamma != null && CanMaterialize(PngConstants.ChunkGamma, options))
        {
            chunks.Add(PngGammaWriter.WriteChunk(metadata.Gamma));
        }
    }

    internal static bool ContainsSelectedColorChunk(List<PngChunk> chunks, PngEncoderOptions options)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (PngColorMetadataPrecedenceResolver.IsColorMetadataChunk(chunks[index].Type)
                && PngEncoder.IsMetadataChunkSelected(chunks[index].Type, options))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void AddTransparency(List<PngChunk> chunks, PngMetadata metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        if (!CanMaterialize(PngConstants.ChunkTransparency, options) || ContainsChunk(chunks, PngConstants.ChunkTransparency))
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.Grayscale && metadata.GrayscaleTransparency != null)
        {
            chunks.Add(PngTransparencyWriter.WriteGrayscaleChunk(metadata.GrayscaleTransparency));
        }
        else if (ihdr.ColorType == PngColorType.Truecolor && metadata.TruecolorTransparency != null)
        {
            chunks.Add(PngTransparencyWriter.WriteTruecolorChunk(metadata.TruecolorTransparency));
        }
    }

    internal static void AddSignificantBits(List<PngChunk> chunks, PngMetadata metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        if (!CanMaterialize(PngConstants.ChunkSignificantBits, options) || metadata.SignificantBits == null || ContainsChunk(chunks, PngConstants.ChunkSignificantBits))
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.Grayscale)
        {
            chunks.Add(PngSignificantBitsWriter.WriteGrayscaleChunk(metadata.SignificantBits));
        }
        else if (ihdr.ColorType == PngColorType.GrayscaleAlpha)
        {
            chunks.Add(PngSignificantBitsWriter.WriteGrayscaleAlphaChunk(metadata.SignificantBits));
        }
        else if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            chunks.Add(PngSignificantBitsWriter.WriteIndexedChunk(metadata.SignificantBits));
        }
        else if (ihdr.ColorType == PngColorType.Truecolor)
        {
            chunks.Add(PngSignificantBitsWriter.WriteTruecolorChunk(metadata.SignificantBits));
        }
        else
        {
            chunks.Add(PngSignificantBitsWriter.WriteTruecolorAlphaChunk(metadata.SignificantBits));
        }
    }

    internal static void AddHdrMetadata(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (CanMaterialize(PngConstants.ChunkMasteringDisplayColorVolume, options) && metadata.MasteringDisplayColorVolume != null && !ContainsChunk(chunks, PngConstants.ChunkMasteringDisplayColorVolume))
        {
            chunks.Add(PngMasteringDisplayColorVolumeWriter.WriteChunk(metadata.MasteringDisplayColorVolume));
        }

        if (CanMaterialize(PngConstants.ChunkContentLightLevelInformation, options) && metadata.ContentLightLevelInformation != null && !ContainsChunk(chunks, PngConstants.ChunkContentLightLevelInformation))
        {
            chunks.Add(PngContentLightLevelInformationWriter.WriteChunk(metadata.ContentLightLevelInformation));
        }
    }

    internal static void AddBackground(List<PngChunk> chunks, PngMetadata metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        if (!CanMaterialize(PngConstants.ChunkBackgroundColor, options) || metadata.BackgroundColor == null || ContainsChunk(chunks, PngConstants.ChunkBackgroundColor))
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            chunks.Add(PngBackgroundColorWriter.WriteIndexedChunk(metadata.BackgroundColor));
        }
        else if (ihdr.ColorType == PngColorType.Grayscale || ihdr.ColorType == PngColorType.GrayscaleAlpha)
        {
            chunks.Add(PngBackgroundColorWriter.WriteGrayscaleChunk(metadata.BackgroundColor));
        }
        else
        {
            chunks.Add(PngBackgroundColorWriter.WriteTruecolorChunk(metadata.BackgroundColor));
        }
    }

    internal static void AddPaletteHistogram(List<PngChunk> chunks, PngMetadata metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        if (CanMaterialize(PngConstants.ChunkPaletteHistogram, options) && ihdr.ColorType == PngColorType.IndexedColor && metadata.PaletteHistogram != null && !ContainsChunk(chunks, PngConstants.ChunkPaletteHistogram))
        {
            chunks.Add(PngPaletteHistogramWriter.WriteChunk(metadata.PaletteHistogram));
        }
    }

    internal static void AddPhysicalPixels(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (CanMaterialize(PngConstants.ChunkPhysicalPixels, options) && metadata.PhysicalPixels != null && !ContainsChunk(chunks, PngConstants.ChunkPhysicalPixels))
        {
            chunks.Add(PngPhysicalPixelsWriter.WriteChunk(metadata.PhysicalPixels));
        }
    }

    internal static void AddExifProfile(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (CanMaterialize(PngConstants.ChunkExif, options) && metadata.ExifProfile != null && !ContainsChunk(chunks, PngConstants.ChunkExif))
        {
            chunks.Add(PngExifProfileWriter.WriteChunk(metadata.ExifProfile));
        }
    }

    internal static void AddTime(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (CanMaterialize(PngConstants.ChunkTime, options) && metadata.Time != null && !ContainsChunk(chunks, PngConstants.ChunkTime))
        {
            chunks.Add(PngTimeWriter.WriteChunk(metadata.Time));
        }
    }

    internal static void AddSuggestedPalettes(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (!CanMaterialize(PngConstants.ChunkSuggestedPalette, options) || ContainsChunk(chunks, PngConstants.ChunkSuggestedPalette))
        {
            return;
        }

        int index = 0;
        while (index < metadata.SuggestedPalettes.Length)
        {
            chunks.Add(PngSuggestedPaletteWriter.WriteChunk(metadata.SuggestedPalettes[index]));
            index += 1;
        }
    }

    internal static void AddTextMetadata(List<PngChunk> chunks, PngMetadata metadata, PngEncoderOptions options)
    {
        if (ContainsSelectedTextChunk(chunks, options))
        {
            return;
        }

        if (metadata.TextEntries.Length > 0)
        {
            int orderedIndex = 0;
            while (orderedIndex < metadata.TextEntries.Length)
            {
                PngTextEntry entry = metadata.TextEntries[orderedIndex];
                string chunkType = GetTextEntryChunkType(entry);
                if (CanMaterialize(chunkType, options))
                {
                    chunks.Add(WriteTextEntry(entry));
                }
                orderedIndex += 1;
            }

            return;
        }

        int index = 0;
        while (index < metadata.Latin1TextEntries.Length)
        {
            if (CanMaterialize(PngConstants.ChunkText, options))
            {
                chunks.Add(PngTextWriter.WriteChunk(metadata.Latin1TextEntries[index]));
            }
            index += 1;
        }

        index = 0;
        while (index < metadata.CompressedTextEntries.Length)
        {
            if (CanMaterialize(PngConstants.ChunkCompressedText, options))
            {
                chunks.Add(PngCompressedTextWriter.WriteChunk(metadata.CompressedTextEntries[index]));
            }
            index += 1;
        }

        index = 0;
        while (index < metadata.InternationalTextEntries.Length)
        {
            if (CanMaterialize(PngConstants.ChunkInternationalText, options))
            {
                chunks.Add(PngInternationalTextWriter.WriteChunk(metadata.InternationalTextEntries[index]));
            }
            index += 1;
        }
    }

    internal static PngChunk WriteTextEntry(PngTextEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Kind == PngTextKind.Latin1)
        {
            PngText text = new PngText();
            text.Keyword = entry.Keyword;
            text.Text = entry.Text;
            return PngTextWriter.WriteChunk(text);
        }

        if (entry.Kind == PngTextKind.CompressedLatin1)
        {
            PngCompressedText text = new PngCompressedText();
            text.Keyword = entry.Keyword;
            text.CompressionMethod = entry.CompressionMethod;
            text.Text = entry.Text;
            return PngCompressedTextWriter.WriteChunk(text);
        }

        if (entry.Kind == PngTextKind.InternationalUtf8)
        {
            PngInternationalText text = new PngInternationalText();
            text.Keyword = entry.Keyword;
            text.CompressionFlag = entry.CompressionFlag;
            text.CompressionMethod = entry.CompressionMethod;
            text.LanguageTag = entry.LanguageTag;
            text.TranslatedKeyword = entry.TranslatedKeyword;
            text.Text = entry.Text;
            return PngInternationalTextWriter.WriteChunk(text);
        }

        throw new ArgumentOutOfRangeException("Kind");
    }

    internal static string GetTextEntryChunkType(PngTextEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Kind == PngTextKind.Latin1)
        {
            return PngConstants.ChunkText;
        }

        if (entry.Kind == PngTextKind.CompressedLatin1)
        {
            return PngConstants.ChunkCompressedText;
        }

        if (entry.Kind == PngTextKind.InternationalUtf8)
        {
            return PngConstants.ChunkInternationalText;
        }

        throw new ArgumentOutOfRangeException("Kind");
    }

    internal static PngChunk TransformTextChunk(PngChunk chunk, PngEncoderOptions options)
    {
        if (options.TextCompressionMode == PngTextCompressionMode.Preserve || !PngMetadataEditor.IsTextChunk(chunk.Type))
        {
            return chunk;
        }

        if (chunk.Type == PngConstants.ChunkInternationalText)
        {
            return TransformInternationalText(chunk, options);
        }

        string keyword;
        string value;
        if (chunk.Type == PngConstants.ChunkText)
        {
            PngText parsed = PngTextParser.Parse(chunk);
            keyword = parsed.Keyword;
            value = parsed.Text;
        }
        else
        {
            PngCompressedText parsed = PngCompressedTextParser.Parse(chunk, options.Limits);
            keyword = parsed.Keyword;
            value = parsed.Text;
        }

        if (ShouldCompressText(value.Length, options))
        {
            PngCompressedText compressed = new PngCompressedText();
            compressed.Keyword = keyword;
            compressed.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
            compressed.Text = value;
            return PngCompressedTextWriter.WriteChunk(compressed, options.Limits);
        }

        PngText uncompressed = new PngText();
        uncompressed.Keyword = keyword;
        uncompressed.Text = value;
        return PngTextWriter.WriteChunk(uncompressed);
    }

    internal static PngChunk TransformInternationalText(PngChunk chunk, PngEncoderOptions options)
    {
        PngInternationalText parsed = PngInternationalTextParser.Parse(chunk, options.Limits);
        PngInternationalText transformed = new PngInternationalText();
        transformed.Keyword = parsed.Keyword;
        transformed.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        transformed.LanguageTag = parsed.LanguageTag;
        transformed.TranslatedKeyword = parsed.TranslatedKeyword;
        transformed.Text = parsed.Text;
        transformed.CompressionFlag = PngInternationalTextParser.CompressionFlagUncompressed;
        if (ShouldCompressText(Encoding.UTF8.GetByteCount(parsed.Text), options))
        {
            transformed.CompressionFlag = PngInternationalTextParser.CompressionFlagCompressed;
        }

        return PngInternationalTextWriter.WriteChunk(transformed, options.Limits);
    }

    internal static bool ShouldCompressText(int encodedLength, PngEncoderOptions options)
    {
        if (options.TextCompressionMode == PngTextCompressionMode.Compressed)
        {
            return true;
        }

        if (options.TextCompressionMode == PngTextCompressionMode.Uncompressed)
        {
            return false;
        }

        return encodedLength >= options.TextCompressionThreshold;
    }

    internal static PngChunk[] SortCanonical(PngChunk[] chunks, int position)
    {
        int index = 1;
        while (index < chunks.Length)
        {
            PngChunk value = chunks[index];
            int rank = GetCanonicalRank(value.Type, position);
            int insertionIndex = index;
            while (insertionIndex > 0 && GetCanonicalRank(chunks[insertionIndex - 1].Type, position) > rank)
            {
                chunks[insertionIndex] = chunks[insertionIndex - 1];
                insertionIndex -= 1;
            }

            chunks[insertionIndex] = value;
            index += 1;
        }

        return chunks;
    }

    internal static int GetCanonicalRank(string chunkType, int position)
    {
        if (position == PngEncoder.MetadataChunkPositionBeforePlte)
        {
            if (chunkType == PngConstants.ChunkCodingIndependentCodePoints) { return 0; }
            if (chunkType == PngConstants.ChunkIccProfile) { return 1; }
            if (chunkType == PngConstants.ChunkStandardRgb) { return 2; }
            if (chunkType == PngConstants.ChunkChromaticities) { return 3; }
            if (chunkType == PngConstants.ChunkGamma) { return 4; }
            if (chunkType == PngConstants.ChunkSignificantBits) { return 5; }
            if (chunkType == PngConstants.ChunkMasteringDisplayColorVolume) { return 6; }
            if (chunkType == PngConstants.ChunkContentLightLevelInformation) { return 7; }
            return 100;
        }

        if (position == PngEncoder.MetadataChunkPositionBeforeIdat)
        {
            if (chunkType == PngConstants.ChunkTransparency) { return 0; }
            if (chunkType == PngConstants.ChunkBackgroundColor) { return 1; }
            if (chunkType == PngConstants.ChunkPaletteHistogram) { return 2; }
            if (chunkType == PngConstants.ChunkPhysicalPixels) { return 3; }
            if (chunkType == PngConstants.ChunkSuggestedPalette) { return 4; }
            if (chunkType == PngConstants.ChunkExif) { return 5; }
            return 100;
        }

        if (chunkType == PngConstants.ChunkTime) { return 0; }
        if (PngMetadataEditor.IsTextChunk(chunkType)) { return 1; }
        return 100;
    }

    internal static bool ContainsSelectedTextChunk(List<PngChunk> chunks, PngEncoderOptions options)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (PngMetadataEditor.IsTextChunk(chunks[index].Type)
                && PngEncoder.IsMetadataChunkSelected(chunks[index].Type, options))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool ContainsChunk(List<PngChunk> chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool ContainsEquivalentSourceChunk(PngChunk[] chunks, PngChunk candidate)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == candidate.Type && chunk.Offset == candidate.Offset && BytesEqual(chunk.Data, candidate.Data))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool CanMaterialize(string chunkType, PngEncoderOptions options)
    {
        return PngEncoder.IsMetadataChunkSelected(chunkType, options);
    }
}
