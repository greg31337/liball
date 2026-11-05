using System.Collections.Generic;

namespace png;

internal class PngChunkDuplicateValidator
{
    internal static PngValidationResult Validate(PngChunkListResult chunkList)
    {
        ArgumentNullException.ThrowIfNull(chunkList);
        return Validate(chunkList.Chunks);
    }

    internal static PngValidationResult Validate(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        List<PngError> errors = new List<PngError>();
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            if (MustAppearAtMostOnce(chunk.Type) && HasPreviousChunk(chunks, index, chunk.Type))
            {
                AddError(errors, chunk);
            }

            index += 1;
        }

        PngValidationResult result = new PngValidationResult();
        result.IsValid = errors.Count == 0;
        result.Errors = errors.ToArray();
        return result;
    }

    internal static bool MustAppearAtMostOnce(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return chunkType == PngConstants.ChunkIHDR
            || chunkType == PngConstants.ChunkPLTE
            || chunkType == PngConstants.ChunkIEND
            || chunkType == PngConstants.ChunkTransparency
            || chunkType == PngConstants.ChunkChromaticities
            || chunkType == PngConstants.ChunkGamma
            || chunkType == PngConstants.ChunkIccProfile
            || chunkType == PngConstants.ChunkSignificantBits
            || chunkType == PngConstants.ChunkStandardRgb
            || chunkType == PngConstants.ChunkCodingIndependentCodePoints
            || chunkType == PngConstants.ChunkMasteringDisplayColorVolume
            || chunkType == PngConstants.ChunkContentLightLevelInformation
            || chunkType == PngConstants.ChunkBackgroundColor
            || chunkType == PngConstants.ChunkPaletteHistogram
            || chunkType == PngConstants.ChunkPhysicalPixels
            || chunkType == PngConstants.ChunkExif
            || chunkType == PngConstants.ChunkTime
            || chunkType == PngConstants.ChunkAnimationControl;
    }

    internal static bool HasPreviousChunk(PngChunk[] chunks, int beforeIndex, string chunkType)
    {
        int index = 0;
        while (index < beforeIndex)
        {
            PngChunk previous = chunks[index];
            ArgumentNullException.ThrowIfNull(previous);
            ArgumentNullException.ThrowIfNull(previous.Type);

            if (previous.Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void AddError(List<PngError> errors, PngChunk chunk)
    {
        PngError error = new PngError();
        error.Code = "PNG_CHUNK_DUPLICATE";
        error.Message = "PNG chunk type must not appear more than once.";
        error.ChunkType = chunk.Type;
        error.Offset = chunk.Offset;
        error.IsFatal = true;
        errors.Add(error);
    }
}
