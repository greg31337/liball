using System.Collections.Generic;

namespace png;

internal class PngApngSequenceValidator
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
        uint expectedSequenceNumber = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            if (IsSequenceChunk(chunk.Type))
            {
                ArgumentNullException.ThrowIfNull(chunk.Data);
                if (HasReadableSequenceNumber(chunk))
                {
                    uint sequenceNumber = PngBinaryReader.ReadUInt32(chunk.Data, 0);
                    if (sequenceNumber != expectedSequenceNumber)
                    {
                        AddSequenceError(errors, chunk);
                    }

                    expectedSequenceNumber += 1;
                }
                else
                {
                    AddLengthError(errors, chunk);
                }
            }

            index += 1;
        }

        return CreateResult(errors);
    }

    internal static bool IsSequenceChunk(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return chunkType == PngConstants.ChunkFrameControl
            || chunkType == PngConstants.ChunkFrameData;
    }

    internal static bool HasReadableSequenceNumber(PngChunk chunk)
    {
        if (chunk.Type == PngConstants.ChunkFrameControl)
        {
            return chunk.Data.Length == PngFrameControlParser.DataLength;
        }

        return chunk.Data.Length >= PngFrameDataParser.SequenceNumberLength;
    }

    internal static void AddSequenceError(List<PngError> errors, PngChunk chunk)
    {
        AddError(errors, "PNG_APNG_SEQUENCE_NUMBER_INVALID", "APNG fcTL and fdAT sequence numbers must be consecutive starting at 0.", chunk.Type, chunk.Offset);
    }

    internal static void AddLengthError(List<PngError> errors, PngChunk chunk)
    {
        AddError(errors, "PNG_APNG_SEQUENCE_CHUNK_LENGTH_INVALID", "APNG sequence chunk payload is too short or malformed.", chunk.Type, chunk.Offset);
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
