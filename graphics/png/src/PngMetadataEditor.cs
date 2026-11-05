namespace png;

/// <summary>
/// Returns metadata copies with selected standardized chunks added, replaced, or removed.
/// </summary>
internal static class PngMetadataEditor
{
    /// <summary>
    /// Returns metadata with its ICC profile chunk replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="profile">The ICC profile to store.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetIccProfile(PngMetadata metadata, PngIccProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        PngChunk chunk = PngIccProfileWriter.WriteChunk(profile);
        PngMetadata result = SetMetadataChunk(metadata, PngConstants.ChunkIccProfile, chunk);
        result.IccProfile = PngIccProfileParser.Parse(chunk);
        PngTypedMetadataEditor.UpdateColorMetadata(result);
        return result;
    }

    /// <summary>
    /// Returns metadata with its coding-independent color information replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="colorPrimaries">The color-primary code point.</param>
    /// <param name="transferCharacteristics">The transfer-function code point.</param>
    /// <param name="matrixCoefficients">The matrix-coefficient code point.</param>
    /// <param name="videoFullRangeFlag">The full-range flag, which must be zero or one.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetCodingIndependentCodePoints(PngMetadata metadata, byte colorPrimaries, byte transferCharacteristics, byte matrixCoefficients, byte videoFullRangeFlag)
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = colorPrimaries;
        codePoints.TransferCharacteristics = transferCharacteristics;
        codePoints.MatrixCoefficients = matrixCoefficients;
        codePoints.VideoFullRangeFlag = videoFullRangeFlag;
        return SetCodingIndependentCodePoints(metadata, codePoints);
    }

    /// <summary>
    /// Returns metadata with its coding-independent color information replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="codePoints">The coding-independent color information to store.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetCodingIndependentCodePoints(PngMetadata metadata, PngCodingIndependentCodePoints codePoints)
    {
        ArgumentNullException.ThrowIfNull(codePoints);
        PngChunk chunk = PngCodingIndependentCodePointsWriter.WriteChunk(codePoints);
        PngMetadata result = SetMetadataChunk(metadata, PngConstants.ChunkCodingIndependentCodePoints, chunk);
        result.CodingIndependentCodePoints = PngCodingIndependentCodePointsParser.Parse(chunk);
        PngTypedMetadataEditor.UpdateColorMetadata(result);
        return result;
    }

    /// <summary>
    /// Returns metadata with its image-gamma chunk replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="gammaValue">The PNG gamma integer scaled by 100000.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetGamma(PngMetadata metadata, uint gammaValue)
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = gammaValue;
        return SetGamma(metadata, gamma);
    }

    /// <summary>
    /// Returns metadata with its image-gamma chunk replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="gamma">The PNG image-gamma value.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetGamma(PngMetadata metadata, PngGamma gamma)
    {
        ArgumentNullException.ThrowIfNull(gamma);
        PngChunk chunk = PngGammaWriter.WriteChunk(gamma);
        PngMetadata result = SetMetadataChunk(metadata, PngConstants.ChunkGamma, chunk);
        result.Gamma = PngGammaParser.Parse(chunk);
        PngTypedMetadataEditor.UpdateColorMetadata(result);
        return result;
    }

    /// <summary>
    /// Returns metadata with its standard-RGB chunk replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="renderingIntent">The PNG standard-RGB rendering-intent value.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetStandardRgb(PngMetadata metadata, byte renderingIntent)
    {
        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = renderingIntent;
        return SetStandardRgb(metadata, standardRgb);
    }

    /// <summary>
    /// Returns metadata with its standard-RGB chunk replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="standardRgb">The standard-RGB rendering intent to store.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetStandardRgb(PngMetadata metadata, PngStandardRgb standardRgb)
    {
        ArgumentNullException.ThrowIfNull(standardRgb);
        PngChunk chunk = PngStandardRgbWriter.WriteChunk(standardRgb);
        PngMetadata result = SetMetadataChunk(metadata, PngConstants.ChunkStandardRgb, chunk);
        result.StandardRgb = PngStandardRgbParser.Parse(chunk);
        PngTypedMetadataEditor.UpdateColorMetadata(result);
        return result;
    }

    /// <summary>
    /// Returns metadata with its physical pixel dimensions replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="pixelsPerUnitX">The horizontal pixels per unit.</param>
    /// <param name="pixelsPerUnitY">The vertical pixels per unit.</param>
    /// <param name="unitSpecifier">Zero for an unknown unit or one for metres.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetPhysicalDimensions(PngMetadata metadata, uint pixelsPerUnitX, uint pixelsPerUnitY, byte unitSpecifier)
    {
        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = pixelsPerUnitX;
        physicalPixels.PixelsPerUnitY = pixelsPerUnitY;
        physicalPixels.UnitSpecifier = unitSpecifier;
        return SetPhysicalDimensions(metadata, physicalPixels);
    }

    /// <summary>
    /// Returns metadata with its physical pixel dimensions replaced.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="physicalPixels">The physical pixel dimensions to store.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata SetPhysicalDimensions(PngMetadata metadata, PngPhysicalPixels physicalPixels)
    {
        ArgumentNullException.ThrowIfNull(physicalPixels);
        PngChunk chunk = PngPhysicalPixelsWriter.WriteChunk(physicalPixels);
        PngMetadata result = SetPhysicalDimensionsChunk(metadata, chunk);
        result.PhysicalPixels = PngPhysicalPixelsParser.Parse(chunk);
        return result;
    }

    /// <summary>
    /// Returns metadata with all standard text chunks replaced by the supplied entries.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="textChunks">The replacement text entries or prebuilt text chunks.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata ReplaceTextChunks(PngMetadata metadata, PngText[] textChunks)
    {
        ArgumentNullException.ThrowIfNull(textChunks);

        return ReplaceTextChunks(metadata, WriteTextChunks(textChunks));
    }

    /// <summary>
    /// Returns metadata with all standard text chunks replaced by the supplied entries.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="textChunks">The replacement text entries or prebuilt text chunks.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata ReplaceTextChunks(PngMetadata metadata, PngChunk[] textChunks)
    {
        ValidateMetadata(metadata);
        ArgumentNullException.ThrowIfNull(textChunks);
        ValidateTextChunks(textChunks);

        PngMetadata result = new PngMetadata();
        result.Chunks = ReplaceTextChunksInList(metadata.Chunks, textChunks);
        result.UnknownChunks = CopyNonTextChunks(metadata.UnknownChunks);
        result.Palette = CopyPalette(metadata.Palette);
        CopyTypedFields(metadata, result);
        PngTypedMetadataEditor.RebuildTextFields(result);
        UpdateSummary(result);
        return result;
    }

    internal static PngMetadata SetPhysicalDimensionsChunk(PngMetadata metadata, PngChunk physicalPixelsChunk)
    {
        return SetMetadataChunk(metadata, PngConstants.ChunkPhysicalPixels, physicalPixelsChunk);
    }

    internal static PngMetadata SetMetadataChunk(PngMetadata metadata, string chunkType, PngChunk replacement)
    {
        ValidateMetadata(metadata);

        PngMetadata result = new PngMetadata();
        result.Chunks = ReplaceChunkInList(metadata.Chunks, chunkType, replacement);
        result.UnknownChunks = CopyFilteredChunks(metadata.UnknownChunks, new string[] { chunkType });
        result.Palette = CopyPalette(metadata.Palette);
        CopyTypedFields(metadata, result);
        UpdateSummary(result);
        return result;
    }

    internal static PngMetadata AddMetadataChunk(PngMetadata metadata, PngChunk addition)
    {
        ValidateMetadata(metadata);
        ValidateChunk(addition);

        PngMetadata result = new PngMetadata();
        result.Chunks = AppendChunk(metadata.Chunks, addition);
        result.UnknownChunks = CopyFilteredChunks(metadata.UnknownChunks, new string[0]);
        result.Palette = CopyPalette(metadata.Palette);
        CopyTypedFields(metadata, result);
        UpdateSummary(result);
        return result;
    }

    internal static PngMetadata ReplaceMetadataChunks(PngMetadata metadata, string chunkType, PngChunk[] replacements)
    {
        ValidateMetadata(metadata);
        ArgumentNullException.ThrowIfNull(replacements);
        int replacementIndex = 0;
        while (replacementIndex < replacements.Length)
        {
            ValidateChunk(replacements[replacementIndex]);
            if (replacements[replacementIndex].Type != chunkType)
            {
                throw new ArgumentException("Replacement chunk type does not match the requested metadata family.");
            }

            replacementIndex += 1;
        }

        PngMetadata result = new PngMetadata();
        result.Chunks = ReplaceChunkTypeInList(metadata.Chunks, chunkType, replacements);
        result.UnknownChunks = CopyFilteredChunks(metadata.UnknownChunks, new string[] { chunkType });
        result.Palette = CopyPalette(metadata.Palette);
        CopyTypedFields(metadata, result);
        UpdateSummary(result);
        return result;
    }

    /// <summary>
    /// Returns metadata with every matching chunk type removed.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="chunkType">The four-character chunk type to remove.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata RemoveChunks(PngMetadata metadata, string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return RemoveChunks(metadata, new string[] { chunkType });
    }

    /// <summary>
    /// Returns metadata with every matching chunk type removed.
    /// </summary>
    /// <param name="metadata">The metadata whose ordered chunk list will be copied and edited.</param>
    /// <param name="chunkTypes">The four-character chunk types to remove.</param>
    /// <returns>The resulting metadata object.</returns>
    public static PngMetadata RemoveChunks(PngMetadata metadata, string[] chunkTypes)
    {
        ValidateMetadata(metadata);
        ArgumentNullException.ThrowIfNull(chunkTypes);

        ValidateChunkTypes(chunkTypes);

        PngMetadata result = new PngMetadata();
        result.Chunks = CopyFilteredChunks(metadata.Chunks, chunkTypes);
        result.UnknownChunks = CopyFilteredChunks(metadata.UnknownChunks, chunkTypes);
        result.Palette = CopyPalette(metadata.Palette);
        CopyTypedFields(metadata, result);
        PngTypedMetadataEditor.ClearRemovedFields(result, chunkTypes);
        UpdateSummary(result);
        return result;
    }

    internal static PngChunk[] AppendChunk(PngChunk[] chunks, PngChunk addition)
    {
        PngChunk[] result = new PngChunk[chunks.Length + 1];
        int index = 0;
        while (index < chunks.Length)
        {
            ValidateChunk(chunks[index]);
            result[index] = CopyChunk(chunks[index]);
            index += 1;
        }

        result[chunks.Length] = CopyChunk(addition);
        return result;
    }

    internal static PngChunk[] ReplaceChunkTypeInList(PngChunk[] chunks, string chunkType, PngChunk[] replacements)
    {
        int count = CountKeptChunks(chunks, new string[] { chunkType }) + replacements.Length;
        PngChunk[] result = new PngChunk[count];
        bool inserted = false;
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < chunks.Length)
        {
            PngChunk chunk = chunks[sourceIndex];
            ValidateChunk(chunk);
            if (chunk.Type == chunkType)
            {
                if (!inserted)
                {
                    resultIndex = CopyReplacementTextChunks(replacements, result, resultIndex);
                    inserted = true;
                }
            }
            else
            {
                result[resultIndex] = CopyChunk(chunk);
                resultIndex += 1;
            }

            sourceIndex += 1;
        }

        if (!inserted)
        {
            CopyReplacementTextChunks(replacements, result, resultIndex);
        }

        return result;
    }

    internal static void ValidateMetadata(PngMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Chunks);
        ArgumentNullException.ThrowIfNull(metadata.UnknownChunks);
        ArgumentNullException.ThrowIfNull(metadata.Palette);
    }

    internal static PngChunk[] WriteTextChunks(PngText[] textEntries)
    {
        PngChunk[] chunks = new PngChunk[textEntries.Length];

        int index = 0;
        while (index < textEntries.Length)
        {
            chunks[index] = PngTextWriter.WriteChunk(textEntries[index]);
            index += 1;
        }

        return chunks;
    }

    internal static void ValidateTextChunks(PngChunk[] textChunks)
    {
        int index = 0;
        while (index < textChunks.Length)
        {
            PngChunk chunk = textChunks[index];
            ValidateChunk(chunk);
            if (!IsTextChunk(chunk.Type))
            {
                throw new ArgumentException("Replacement chunk must be a PNG text chunk.");
            }

            index += 1;
        }
    }

    internal static void ValidateChunkTypes(string[] chunkTypes)
    {
        int index = 0;
        while (index < chunkTypes.Length)
        {
            string chunkType = chunkTypes[index];
            ArgumentNullException.ThrowIfNull(chunkType);

            PngValidationResult validation = PngChunkTypeValidator.Validate(chunkType);
            if (!validation.IsValid)
            {
                throw new ArgumentException("PNG chunk type is invalid.");
            }

            index += 1;
        }
    }

    internal static PngChunk[] CopyFilteredChunks(PngChunk[] chunks, string[] chunkTypes)
    {
        int keptCount = CountKeptChunks(chunks, chunkTypes);
        PngChunk[] filtered = new PngChunk[keptCount];

        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < chunks.Length)
        {
            PngChunk chunk = chunks[sourceIndex];
            ValidateChunk(chunk);
            if (!ShouldRemoveChunk(chunk, chunkTypes))
            {
                filtered[resultIndex] = CopyChunk(chunk);
                resultIndex += 1;
            }

            sourceIndex += 1;
        }

        return filtered;
    }

    internal static PngChunk[] ReplaceChunkInList(PngChunk[] chunks, string chunkType, PngChunk replacement)
    {
        int count = CountKeptChunks(chunks, new string[] { chunkType }) + 1;
        PngChunk[] replaced = new PngChunk[count];

        bool inserted = false;
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < chunks.Length)
        {
            PngChunk chunk = chunks[sourceIndex];
            ValidateChunk(chunk);
            if (chunk.Type == chunkType)
            {
                if (!inserted)
                {
                    replaced[resultIndex] = CopyChunk(replacement);
                    resultIndex += 1;
                    inserted = true;
                }
            }
            else
            {
                replaced[resultIndex] = CopyChunk(chunk);
                resultIndex += 1;
            }

            sourceIndex += 1;
        }

        if (!inserted)
        {
            replaced[resultIndex] = CopyChunk(replacement);
        }

        return replaced;
    }

    internal static PngChunk[] ReplaceTextChunksInList(PngChunk[] chunks, PngChunk[] textChunks)
    {
        int count = CountNonTextChunks(chunks) + textChunks.Length;
        PngChunk[] replaced = new PngChunk[count];

        bool inserted = false;
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < chunks.Length)
        {
            PngChunk chunk = chunks[sourceIndex];
            ValidateChunk(chunk);
            if (IsTextChunk(chunk.Type))
            {
                if (!inserted)
                {
                    resultIndex = CopyReplacementTextChunks(textChunks, replaced, resultIndex);
                    inserted = true;
                }
            }
            else
            {
                replaced[resultIndex] = CopyChunk(chunk);
                resultIndex += 1;
            }

            sourceIndex += 1;
        }

        if (!inserted)
        {
            CopyReplacementTextChunks(textChunks, replaced, resultIndex);
        }

        return replaced;
    }

    internal static PngChunk[] CopyNonTextChunks(PngChunk[] chunks)
    {
        int count = CountNonTextChunks(chunks);
        PngChunk[] copied = new PngChunk[count];

        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < chunks.Length)
        {
            PngChunk chunk = chunks[sourceIndex];
            ValidateChunk(chunk);
            if (!IsTextChunk(chunk.Type))
            {
                copied[resultIndex] = CopyChunk(chunk);
                resultIndex += 1;
            }

            sourceIndex += 1;
        }

        return copied;
    }

    internal static int CountNonTextChunks(PngChunk[] chunks)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunk(chunk);
            if (!IsTextChunk(chunk.Type))
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    internal static int CopyReplacementTextChunks(PngChunk[] textChunks, PngChunk[] destination, int offset)
    {
        int index = 0;
        while (index < textChunks.Length)
        {
            destination[offset + index] = CopyChunk(textChunks[index]);
            index += 1;
        }

        return offset + textChunks.Length;
    }

    internal static int CountKeptChunks(PngChunk[] chunks, string[] chunkTypes)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunk(chunk);
            if (!ShouldRemoveChunk(chunk, chunkTypes))
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    internal static bool ShouldRemoveChunk(PngChunk chunk, string[] chunkTypes)
    {
        return ContainsChunkType(chunkTypes, chunk.Type);
    }

    internal static bool ContainsChunkType(string[] chunkTypes, string chunkType)
    {
        int index = 0;
        while (index < chunkTypes.Length)
        {
            if (chunkTypes[index] == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static PngChunk CopyChunk(PngChunk source)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = source.Type;
        chunk.Data = CopyBytes(source.Data);
        chunk.Crc = source.Crc;
        chunk.Offset = source.Offset;
        chunk.Length = source.Length;
        chunk.IsKnown = source.IsKnown;
        chunk.IsCritical = source.IsCritical;
        chunk.IsPublic = source.IsPublic;
        chunk.IsReservedBitValid = source.IsReservedBitValid;
        chunk.IsSafeToCopy = source.IsSafeToCopy;
        chunk.Placement = source.Placement;
        chunk.PreservationState = source.PreservationState;
        return chunk;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index += 1;
        }

        return copy;
    }

    internal static PngPalette CopyPalette(PngPalette source)
    {
        ArgumentNullException.ThrowIfNull(source.Entries);

        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[source.Entries.Length];

        int index = 0;
        while (index < source.Entries.Length)
        {
            PngPaletteEntry sourceEntry = source.Entries[index];
            ArgumentNullException.ThrowIfNull(sourceEntry);

            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = sourceEntry.Red;
            entry.Green = sourceEntry.Green;
            entry.Blue = sourceEntry.Blue;
            palette.Entries[index] = entry;

            index += 1;
        }

        return palette;
    }

    internal static void CopyTypedFields(PngMetadata source, PngMetadata destination)
    {
        destination.IndexedTransparency = source.IndexedTransparency;
        destination.GrayscaleTransparency = source.GrayscaleTransparency;
        destination.TruecolorTransparency = source.TruecolorTransparency;
        destination.Chromaticities = source.Chromaticities;
        destination.Gamma = source.Gamma;
        destination.IccProfile = source.IccProfile;
        destination.SignificantBits = source.SignificantBits;
        destination.StandardRgb = source.StandardRgb;
        destination.CodingIndependentCodePoints = source.CodingIndependentCodePoints;
        destination.ColorMetadata = source.ColorMetadata;
        destination.MasteringDisplayColorVolume = source.MasteringDisplayColorVolume;
        destination.ContentLightLevelInformation = source.ContentLightLevelInformation;
        destination.TextEntries = source.TextEntries;
        destination.Latin1TextEntries = source.Latin1TextEntries;
        destination.CompressedTextEntries = source.CompressedTextEntries;
        destination.InternationalTextEntries = source.InternationalTextEntries;
        destination.BackgroundColor = source.BackgroundColor;
        destination.PaletteHistogram = source.PaletteHistogram;
        destination.PhysicalPixels = source.PhysicalPixels;
        destination.SuggestedPalettes = source.SuggestedPalettes;
        destination.ExifProfile = source.ExifProfile;
        destination.Time = source.Time;
        destination.AnimationControl = source.AnimationControl;
        destination.FrameControls = source.FrameControls;
        destination.FrameData = source.FrameData;
    }

    internal static void UpdateSummary(PngMetadata metadata)
    {
        metadata.MetadataBytes = 0;
        metadata.TextBytes = 0;
        metadata.TextEntryCount = 0;

        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            PngChunk chunk = metadata.Chunks[index];
            ValidateChunk(chunk);

            if (PngChunkTypeFlags.IsAncillary(chunk.Type) && chunk.Type != PngConstants.ChunkFrameData)
            {
                metadata.MetadataBytes += chunk.Length;
            }

            if (IsTextChunk(chunk.Type))
            {
                metadata.TextBytes += GetTextLength(chunk);
                metadata.TextEntryCount += 1;
            }

            index += 1;
        }
    }

    internal static long GetTextLength(PngChunk chunk)
    {
        if (!PngMetadataReader.CanParseTextChunk(chunk))
        {
            return chunk.Length;
        }

        if (chunk.Type == PngConstants.ChunkText)
        {
            return PngTextParser.Parse(chunk).Text.Length;
        }

        if (chunk.Type == PngConstants.ChunkCompressedText)
        {
            return PngCompressedTextParser.Parse(chunk).Text.Length;
        }

        return System.Text.Encoding.UTF8.GetByteCount(PngInternationalTextParser.Parse(chunk).Text);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
    }

    internal static bool IsTextChunk(string chunkType)
    {
        return chunkType == PngConstants.ChunkText
            || chunkType == PngConstants.ChunkCompressedText
            || chunkType == PngConstants.ChunkInternationalText;
    }
}
