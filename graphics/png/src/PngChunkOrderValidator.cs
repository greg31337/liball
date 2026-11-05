using System.Collections.Generic;

namespace png;

internal class PngChunkOrderValidator
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
        bool seenIhdr = false;
        bool seenPlte = false;
        bool seenIdat = false;
        bool idatClosed = false;
        bool seenIend = false;
        int index = 0;

        if (chunks.Length == 0)
        {
            AddError(errors, "PNG_CHUNK_ORDER_EMPTY", "PNG chunk list is empty.", "", 0);
            return CreateResult(errors);
        }

        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            if (seenIend)
            {
                AddError(errors, "PNG_CHUNK_AFTER_IEND", "PNG chunks must not appear after IEND.", chunk.Type, chunk.Offset);
            }

            if (index == 0 && chunk.Type != PngConstants.ChunkIHDR)
            {
                AddError(errors, "PNG_IHDR_NOT_FIRST", "IHDR must be the first PNG chunk.", chunk.Type, chunk.Offset);
            }

            if (chunk.Type == PngConstants.ChunkIHDR)
            {
                seenIhdr = true;
                if (index != 0)
                {
                    AddError(errors, "PNG_IHDR_NOT_FIRST", "IHDR must be the first PNG chunk.", chunk.Type, chunk.Offset);
                }
            }
            else if (chunk.Type == PngConstants.ChunkPLTE)
            {
                if (!seenIhdr)
                {
                    AddError(errors, "PNG_PLTE_BEFORE_IHDR", "PLTE must appear after IHDR.", chunk.Type, chunk.Offset);
                }

                if (seenIdat)
                {
                    AddError(errors, "PNG_PLTE_AFTER_IDAT", "PLTE must appear before IDAT.", chunk.Type, chunk.Offset);
                }

                seenPlte = true;
                if (seenIdat)
                {
                    idatClosed = true;
                }
            }
            else if (chunk.Type == PngConstants.ChunkIDAT)
            {
                if (!seenIhdr)
                {
                    AddError(errors, "PNG_IDAT_BEFORE_IHDR", "IDAT must appear after IHDR.", chunk.Type, chunk.Offset);
                }

                if (idatClosed)
                {
                    AddError(errors, "PNG_IDAT_NOT_CONSECUTIVE", "IDAT chunks must be consecutive.", chunk.Type, chunk.Offset);
                }

                seenIdat = true;
            }
            else if (chunk.Type == PngConstants.ChunkIEND)
            {
                seenIend = true;
                if (index != chunks.Length - 1)
                {
                    AddError(errors, "PNG_IEND_NOT_LAST", "IEND must be the final PNG chunk.", chunk.Type, chunk.Offset);
                }
            }
            else
            {
                ValidateAncillaryPlacement(errors, chunk, seenPlte, seenIdat);
                if (seenIdat)
                {
                    idatClosed = true;
                }
            }

            index += 1;
        }

        if (!seenIhdr)
        {
            AddError(errors, "PNG_IHDR_MISSING", "PNG chunk list must contain IHDR.", "", 0);
        }

        if (!seenIdat)
        {
            AddError(errors, "PNG_IDAT_MISSING", "PNG chunk list must contain IDAT.", "", 0);
        }

        if (!seenIend)
        {
            AddError(errors, "PNG_IEND_MISSING", "PNG chunk list must contain IEND.", "", 0);
        }

        return CreateResult(errors);
    }

    internal static void ValidateAncillaryPlacement(List<PngError> errors, PngChunk chunk, bool seenPlte, bool seenIdat)
    {
        if (MustAppearBeforePlteAndIdat(chunk.Type))
        {
            if (seenPlte)
            {
                AddError(errors, "PNG_ANCILLARY_AFTER_PLTE", "This ancillary chunk must appear before PLTE.", chunk.Type, chunk.Offset);
            }

            if (seenIdat)
            {
                AddError(errors, "PNG_ANCILLARY_AFTER_IDAT", "This ancillary chunk must appear before IDAT.", chunk.Type, chunk.Offset);
            }
        }
        else if (MustAppearBeforeIdat(chunk.Type))
        {
            if (seenIdat)
            {
                AddError(errors, "PNG_ANCILLARY_AFTER_IDAT", "This ancillary chunk must appear before IDAT.", chunk.Type, chunk.Offset);
            }
        }
    }

    internal static bool MustAppearBeforePlteAndIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkChromaticities
            || chunkType == PngConstants.ChunkGamma
            || chunkType == PngConstants.ChunkIccProfile
            || chunkType == PngConstants.ChunkSignificantBits
            || chunkType == PngConstants.ChunkStandardRgb
            || chunkType == PngConstants.ChunkCodingIndependentCodePoints
            || chunkType == PngConstants.ChunkMasteringDisplayColorVolume
            || chunkType == PngConstants.ChunkContentLightLevelInformation;
    }

    internal static bool MustAppearBeforeIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkTransparency
            || chunkType == PngConstants.ChunkBackgroundColor
            || chunkType == PngConstants.ChunkPaletteHistogram
            || chunkType == PngConstants.ChunkPhysicalPixels
            || chunkType == PngConstants.ChunkSuggestedPalette
            || chunkType == PngConstants.ChunkExif
            || chunkType == PngConstants.ChunkAnimationControl;
    }

    internal static void AddError(List<PngError> errors, string code, string message, string chunkType, long offset)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        errors.Add(error);
    }

    internal static PngValidationResult CreateResult(List<PngError> errors)
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = errors.Count == 0;
        result.Errors = errors.ToArray();
        return result;
    }
}
