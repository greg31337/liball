using System.Collections.Generic;

namespace png;

internal class PngTypedMetadataEditor
{
    internal static PngMetadata SetIndexedTransparency(PngMetadata metadata, PngIndexedTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        PngChunk chunk = PngTransparencyWriter.WriteIndexedChunk(transparency);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkTransparency, chunk);
        result.IndexedTransparency = PngTransparencyParser.ParseIndexed(chunk);
        result.GrayscaleTransparency = null;
        result.TruecolorTransparency = null;
        return result;
    }

    internal static PngMetadata SetGrayscaleTransparency(PngMetadata metadata, PngGrayscaleTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        PngChunk chunk = PngTransparencyWriter.WriteGrayscaleChunk(transparency);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkTransparency, chunk);
        result.IndexedTransparency = null;
        result.GrayscaleTransparency = PngTransparencyParser.ParseGrayscale(chunk);
        result.TruecolorTransparency = null;
        return result;
    }

    internal static PngMetadata SetTruecolorTransparency(PngMetadata metadata, PngTruecolorTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        PngChunk chunk = PngTransparencyWriter.WriteTruecolorChunk(transparency);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkTransparency, chunk);
        result.IndexedTransparency = null;
        result.GrayscaleTransparency = null;
        result.TruecolorTransparency = PngTransparencyParser.ParseTruecolor(chunk);
        return result;
    }

    internal static PngMetadata SetChromaticities(PngMetadata metadata, PngChromaticities chromaticities)
    {
        ArgumentNullException.ThrowIfNull(chromaticities);
        PngChunk chunk = PngChromaticitiesWriter.WriteChunk(chromaticities);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkChromaticities, chunk);
        result.Chromaticities = PngChromaticitiesParser.Parse(chunk);
        UpdateColorMetadata(result);
        return result;
    }

    internal static PngMetadata SetGrayscaleSignificantBits(PngMetadata metadata, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        PngChunk chunk = PngSignificantBitsWriter.WriteGrayscaleChunk(bits);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkSignificantBits, chunk);
        result.SignificantBits = PngSignificantBitsParser.ParseGrayscale(chunk);
        return result;
    }

    internal static PngMetadata SetGrayscaleAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        PngChunk chunk = PngSignificantBitsWriter.WriteGrayscaleAlphaChunk(bits);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkSignificantBits, chunk);
        result.SignificantBits = PngSignificantBitsParser.ParseGrayscaleAlpha(chunk);
        return result;
    }

    internal static PngMetadata SetIndexedSignificantBits(PngMetadata metadata, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        PngChunk chunk = PngSignificantBitsWriter.WriteIndexedChunk(bits);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkSignificantBits, chunk);
        result.SignificantBits = PngSignificantBitsParser.ParseIndexed(chunk);
        return result;
    }

    internal static PngMetadata SetTruecolorSignificantBits(PngMetadata metadata, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        PngChunk chunk = PngSignificantBitsWriter.WriteTruecolorChunk(bits);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkSignificantBits, chunk);
        result.SignificantBits = PngSignificantBitsParser.ParseTruecolor(chunk);
        return result;
    }

    internal static PngMetadata SetTruecolorAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        PngChunk chunk = PngSignificantBitsWriter.WriteTruecolorAlphaChunk(bits);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkSignificantBits, chunk);
        result.SignificantBits = PngSignificantBitsParser.ParseTruecolorAlpha(chunk);
        return result;
    }

    internal static PngMetadata SetMasteringDisplayColorVolume(PngMetadata metadata, PngMasteringDisplayColorVolume volume)
    {
        ArgumentNullException.ThrowIfNull(volume);
        PngChunk chunk = PngMasteringDisplayColorVolumeWriter.WriteChunk(volume);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkMasteringDisplayColorVolume, chunk);
        result.MasteringDisplayColorVolume = PngMasteringDisplayColorVolumeParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata SetContentLightLevelInformation(PngMetadata metadata, PngContentLightLevelInformation lightLevels)
    {
        ArgumentNullException.ThrowIfNull(lightLevels);
        PngChunk chunk = PngContentLightLevelInformationWriter.WriteChunk(lightLevels);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkContentLightLevelInformation, chunk);
        result.ContentLightLevelInformation = PngContentLightLevelInformationParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata AddLatin1Text(PngMetadata metadata, PngText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngTextWriter.WriteChunk(text));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata AddCompressedText(PngMetadata metadata, PngCompressedText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngCompressedTextWriter.WriteChunk(text));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata AddInternationalText(PngMetadata metadata, PngInternationalText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngInternationalTextWriter.WriteChunk(text));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata ReplaceLatin1Text(PngMetadata metadata, PngText[] entries)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkText, WriteLatin1TextChunks(entries));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata ReplaceCompressedText(PngMetadata metadata, PngCompressedText[] entries)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkCompressedText, WriteCompressedTextChunks(entries));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata ReplaceInternationalText(PngMetadata metadata, PngInternationalText[] entries)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkInternationalText, WriteInternationalTextChunks(entries));
        RebuildTextFields(result);
        return result;
    }

    internal static PngMetadata SetIndexedBackgroundColor(PngMetadata metadata, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(background);
        PngChunk chunk = PngBackgroundColorWriter.WriteIndexedChunk(background);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkBackgroundColor, chunk);
        result.BackgroundColor = PngBackgroundColorParser.ParseIndexed(chunk);
        return result;
    }

    internal static PngMetadata SetGrayscaleBackgroundColor(PngMetadata metadata, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(background);
        PngChunk chunk = PngBackgroundColorWriter.WriteGrayscaleChunk(background);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkBackgroundColor, chunk);
        result.BackgroundColor = PngBackgroundColorParser.ParseGrayscale(chunk);
        return result;
    }

    internal static PngMetadata SetTruecolorBackgroundColor(PngMetadata metadata, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(background);
        PngChunk chunk = PngBackgroundColorWriter.WriteTruecolorChunk(background);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkBackgroundColor, chunk);
        result.BackgroundColor = PngBackgroundColorParser.ParseTruecolor(chunk);
        return result;
    }

    internal static PngMetadata SetPaletteHistogram(PngMetadata metadata, PngPaletteHistogram histogram)
    {
        ArgumentNullException.ThrowIfNull(histogram);
        PngChunk chunk = PngPaletteHistogramWriter.WriteChunk(histogram);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkPaletteHistogram, chunk);
        result.PaletteHistogram = PngPaletteHistogramParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata AddSuggestedPalette(PngMetadata metadata, PngSuggestedPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngSuggestedPaletteWriter.WriteChunk(palette));
        RebuildSuggestedPalettes(result);
        return result;
    }

    internal static PngMetadata ReplaceSuggestedPalettes(PngMetadata metadata, PngSuggestedPalette[] palettes)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkSuggestedPalette, WriteSuggestedPaletteChunks(palettes));
        RebuildSuggestedPalettes(result);
        return result;
    }

    internal static PngMetadata SetExifProfile(PngMetadata metadata, PngExifProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        PngChunk chunk = PngExifProfileWriter.WriteChunk(profile);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkExif, chunk);
        result.ExifProfile = PngExifProfileParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata SetTime(PngMetadata metadata, PngTime time)
    {
        ArgumentNullException.ThrowIfNull(time);
        PngChunk chunk = PngTimeWriter.WriteChunk(time);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkTime, chunk);
        result.Time = PngTimeParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata SetAnimationControl(PngMetadata metadata, PngAnimationControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        PngChunk chunk = PngAnimationControlWriter.WriteChunk(control);
        PngMetadata result = PngMetadataEditor.SetMetadataChunk(metadata, PngConstants.ChunkAnimationControl, chunk);
        result.AnimationControl = PngAnimationControlParser.Parse(chunk);
        return result;
    }

    internal static PngMetadata AddFrameControl(PngMetadata metadata, PngFrameControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngFrameControlWriter.WriteChunk(control));
        RebuildFrameControls(result);
        return result;
    }

    internal static PngMetadata ReplaceFrameControls(PngMetadata metadata, PngFrameControl[] controls)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkFrameControl, WriteFrameControlChunks(controls));
        RebuildFrameControls(result);
        return result;
    }

    internal static PngMetadata AddFrameData(PngMetadata metadata, PngFrameData frameData)
    {
        ArgumentNullException.ThrowIfNull(frameData);
        PngMetadata result = PngMetadataEditor.AddMetadataChunk(metadata, PngFrameDataWriter.WriteChunk(frameData));
        RebuildFrameData(result);
        return result;
    }

    internal static PngMetadata ReplaceFrameData(PngMetadata metadata, PngFrameData[] frameData)
    {
        PngMetadata result = PngMetadataEditor.ReplaceMetadataChunks(metadata, PngConstants.ChunkFrameData, WriteFrameDataChunks(frameData));
        RebuildFrameData(result);
        return result;
    }

    internal static PngMetadata RemoveTransparency(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkTransparency); }
    internal static PngMetadata RemoveChromaticities(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkChromaticities); }
    internal static PngMetadata RemoveGamma(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkGamma); }
    internal static PngMetadata RemoveIccProfile(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkIccProfile); }
    internal static PngMetadata RemoveSignificantBits(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkSignificantBits); }
    internal static PngMetadata RemoveStandardRgb(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkStandardRgb); }
    internal static PngMetadata RemoveCodingIndependentCodePoints(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkCodingIndependentCodePoints); }
    internal static PngMetadata RemoveMasteringDisplayColorVolume(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkMasteringDisplayColorVolume); }
    internal static PngMetadata RemoveContentLightLevelInformation(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkContentLightLevelInformation); }
    internal static PngMetadata RemoveLatin1Text(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkText); }
    internal static PngMetadata RemoveCompressedText(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkCompressedText); }
    internal static PngMetadata RemoveInternationalText(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkInternationalText); }
    internal static PngMetadata RemoveText(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, new string[] { PngConstants.ChunkText, PngConstants.ChunkCompressedText, PngConstants.ChunkInternationalText }); }
    internal static PngMetadata RemoveBackgroundColor(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkBackgroundColor); }
    internal static PngMetadata RemovePaletteHistogram(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkPaletteHistogram); }
    internal static PngMetadata RemovePhysicalDimensions(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkPhysicalPixels); }
    internal static PngMetadata RemoveSuggestedPalettes(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkSuggestedPalette); }
    internal static PngMetadata RemoveExifProfile(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkExif); }
    internal static PngMetadata RemoveTime(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkTime); }
    internal static PngMetadata RemoveAnimationControl(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkAnimationControl); }
    internal static PngMetadata RemoveFrameControls(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkFrameControl); }
    internal static PngMetadata RemoveFrameData(PngMetadata metadata) { return PngMetadataEditor.RemoveChunks(metadata, PngConstants.ChunkFrameData); }

    internal static void RebuildTextFields(PngMetadata metadata)
    {
        List<PngTextEntry> entries = new List<PngTextEntry>();
        List<PngText> latin1Entries = new List<PngText>();
        List<PngCompressedText> compressedEntries = new List<PngCompressedText>();
        List<PngInternationalText> internationalEntries = new List<PngInternationalText>();
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            PngChunk chunk = metadata.Chunks[index];
            if (PngMetadataReader.CanParseTextChunk(chunk))
            {
                if (chunk.Type == PngConstants.ChunkText)
                {
                    PngText text = PngTextParser.Parse(chunk);
                    latin1Entries.Add(text);
                    entries.Add(PngTextModelConverter.CreateLatin1Entry(text, index));
                }
                else if (chunk.Type == PngConstants.ChunkCompressedText)
                {
                    PngCompressedText text = PngCompressedTextParser.Parse(chunk);
                    compressedEntries.Add(text);
                    entries.Add(PngTextModelConverter.CreateCompressedLatin1Entry(text, index));
                }
                else if (chunk.Type == PngConstants.ChunkInternationalText)
                {
                    PngInternationalText text = PngInternationalTextParser.Parse(chunk);
                    internationalEntries.Add(text);
                    entries.Add(PngTextModelConverter.CreateInternationalEntry(text, index));
                }
            }

            index += 1;
        }

        metadata.TextEntries = entries.ToArray();
        metadata.Latin1TextEntries = latin1Entries.ToArray();
        metadata.CompressedTextEntries = compressedEntries.ToArray();
        metadata.InternationalTextEntries = internationalEntries.ToArray();
    }

    internal static void RebuildSuggestedPalettes(PngMetadata metadata)
    {
        List<PngSuggestedPalette> palettes = new List<PngSuggestedPalette>();
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            if (metadata.Chunks[index].Type == PngConstants.ChunkSuggestedPalette)
            {
                palettes.Add(PngSuggestedPaletteParser.Parse(metadata.Chunks[index]));
            }

            index += 1;
        }

        metadata.SuggestedPalettes = palettes.ToArray();
    }

    internal static void RebuildFrameControls(PngMetadata metadata)
    {
        List<PngFrameControl> controls = new List<PngFrameControl>();
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            if (metadata.Chunks[index].Type == PngConstants.ChunkFrameControl)
            {
                controls.Add(PngFrameControlParser.Parse(metadata.Chunks[index]));
            }

            index += 1;
        }

        metadata.FrameControls = controls.ToArray();
    }

    internal static void RebuildFrameData(PngMetadata metadata)
    {
        List<PngFrameData> frameData = new List<PngFrameData>();
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            if (metadata.Chunks[index].Type == PngConstants.ChunkFrameData)
            {
                frameData.Add(PngFrameDataParser.Parse(metadata.Chunks[index]));
            }

            index += 1;
        }

        metadata.FrameData = frameData.ToArray();
    }

    internal static void ClearRemovedFields(PngMetadata metadata, string[] chunkTypes)
    {
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkTransparency))
        {
            metadata.IndexedTransparency = null;
            metadata.GrayscaleTransparency = null;
            metadata.TruecolorTransparency = null;
        }

        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkChromaticities)) { metadata.Chromaticities = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkGamma)) { metadata.Gamma = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkIccProfile)) { metadata.IccProfile = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkSignificantBits)) { metadata.SignificantBits = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkStandardRgb)) { metadata.StandardRgb = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkCodingIndependentCodePoints)) { metadata.CodingIndependentCodePoints = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkMasteringDisplayColorVolume)) { metadata.MasteringDisplayColorVolume = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkContentLightLevelInformation)) { metadata.ContentLightLevelInformation = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkBackgroundColor)) { metadata.BackgroundColor = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkPaletteHistogram)) { metadata.PaletteHistogram = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkPhysicalPixels)) { metadata.PhysicalPixels = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkExif)) { metadata.ExifProfile = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkTime)) { metadata.Time = null; }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkAnimationControl)) { metadata.AnimationControl = null; }

        if (ContainsTextType(chunkTypes)) { RebuildTextFields(metadata); }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkSuggestedPalette)) { RebuildSuggestedPalettes(metadata); }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkFrameControl)) { RebuildFrameControls(metadata); }
        if (PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkFrameData)) { RebuildFrameData(metadata); }
        if (ContainsColorType(chunkTypes)) { UpdateColorMetadata(metadata); }
    }

    internal static bool ContainsTextType(string[] chunkTypes)
    {
        return PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkText)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkCompressedText)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkInternationalText);
    }

    internal static bool ContainsColorType(string[] chunkTypes)
    {
        return PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkChromaticities)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkGamma)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkIccProfile)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkStandardRgb)
            || PngMetadataEditor.ContainsChunkType(chunkTypes, PngConstants.ChunkCodingIndependentCodePoints);
    }

    internal static void UpdateColorMetadata(PngMetadata metadata)
    {
        metadata.ColorMetadata = PngColorMetadataPrecedenceResolver.Resolve(metadata.Chunks);
    }

    internal static PngChunk[] WriteLatin1TextChunks(PngText[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        PngChunk[] chunks = new PngChunk[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            chunks[index] = PngTextWriter.WriteChunk(entries[index]);
            index += 1;
        }

        return chunks;
    }

    internal static PngChunk[] WriteCompressedTextChunks(PngCompressedText[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        PngChunk[] chunks = new PngChunk[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            chunks[index] = PngCompressedTextWriter.WriteChunk(entries[index]);
            index += 1;
        }

        return chunks;
    }

    internal static PngChunk[] WriteInternationalTextChunks(PngInternationalText[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        PngChunk[] chunks = new PngChunk[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            chunks[index] = PngInternationalTextWriter.WriteChunk(entries[index]);
            index += 1;
        }

        return chunks;
    }

    internal static PngChunk[] WriteSuggestedPaletteChunks(PngSuggestedPalette[] palettes)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        PngChunk[] chunks = new PngChunk[palettes.Length];
        int index = 0;
        while (index < palettes.Length)
        {
            chunks[index] = PngSuggestedPaletteWriter.WriteChunk(palettes[index]);
            index += 1;
        }

        return chunks;
    }

    internal static PngChunk[] WriteFrameControlChunks(PngFrameControl[] controls)
    {
        ArgumentNullException.ThrowIfNull(controls);
        PngChunk[] chunks = new PngChunk[controls.Length];
        int index = 0;
        while (index < controls.Length)
        {
            chunks[index] = PngFrameControlWriter.WriteChunk(controls[index]);
            index += 1;
        }

        return chunks;
    }

    internal static PngChunk[] WriteFrameDataChunks(PngFrameData[] frameData)
    {
        ArgumentNullException.ThrowIfNull(frameData);
        PngChunk[] chunks = new PngChunk[frameData.Length];
        int index = 0;
        while (index < frameData.Length)
        {
            chunks[index] = PngFrameDataWriter.WriteChunk(frameData[index]);
            index += 1;
        }

        return chunks;
    }
}
