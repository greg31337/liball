using System.Collections.Generic;

namespace png;

internal class PngValidationDatastreamReadResult
{
    internal PngChunkListResult ChunkList = new PngChunkListResult();
    internal PngError[] Errors = new PngError[0];
}

internal class PngValidationDatastreamReader
{
    internal static PngValidationDatastreamReadResult Read(byte[] data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<PngChunk> chunks = new List<PngChunk>();
        List<PngError> errors = new List<PngError>();
        int currentOffset = offset;
        bool foundIend = false;
        while (currentOffset < data.Length && !foundIend)
        {
            if (limits.MaxChunkCount > 0 && chunks.Count >= limits.MaxChunkCount)
            {
                throw new OverflowException();
            }

            if (data.Length - currentOffset < 12)
            {
                AddError(errors, "PNG_CHUNK_FRAMING_TRUNCATED", "PNG chunk framing must contain a length, type, payload, and CRC.", "", currentOffset);
                currentOffset = data.Length;
                continue;
            }

            uint length = PngBinaryReader.ReadUInt32(data, currentOffset);
            PngChunkType chunkType = PngChunkTypeParser.Parse(data, currentOffset + 4);
            PngValidationResult typeResult = PngChunkTypeValidator.Validate(chunkType);
            AddTypeErrors(errors, typeResult.Errors, currentOffset + 4);
            long availablePayload = data.Length - currentOffset - 12;
            if (length > availablePayload || length > int.MaxValue)
            {
                AddError(errors, "PNG_CHUNK_PAYLOAD_TRUNCATED", "PNG chunk payload or CRC extends past the end of the datastream.", chunkType.Name, currentOffset);
                currentOffset = data.Length;
                continue;
            }

            EnforceChunkLimit(chunkType, length, limits);
            byte[] chunkData = CopyBytes(data, currentOffset + 8, (int)length);
            uint crc = PngBinaryReader.ReadUInt32(data, currentOffset + 8 + (int)length);
            PngChunk chunk = CreateChunk(chunkType, chunkData, crc, currentOffset);
            chunks.Add(chunk);
            currentOffset += 12 + (int)length;
            foundIend = chunk.Type == PngConstants.ChunkIEND;
        }

        byte[] trailingBytes = new byte[0];
        if (foundIend && currentOffset < data.Length)
        {
            trailingBytes = CopyBytes(data, currentOffset, data.Length - currentOffset);
        }

        PngChunkListResult chunkList = new PngChunkListResult();
        chunkList.Chunks = chunks.ToArray();
        chunkList.TrailingBytes = trailingBytes;
        chunkList.FoundIend = foundIend;
        PngChunkPlacementProcessor.AnnotatePreservedChunks(chunkList.Chunks);
        PngValidationDatastreamReadResult result = new PngValidationDatastreamReadResult();
        result.ChunkList = chunkList;
        result.Errors = errors.ToArray();
        return result;
    }

    internal static void EnforceChunkLimit(PngChunkType chunkType, uint length, PngSecurityLimits limits)
    {
        long limit = PngRawChunkReader.GetChunkDataLimit(chunkType, limits);
        PngSizeCalculator.CalculateChunkAllocation(length, limit);
    }

    internal static byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = data[offset + index];
            index += 1;
        }

        return result;
    }

    internal static PngChunk CreateChunk(PngChunkType chunkType, byte[] data, uint crc, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType.Name;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.Crc = crc;
        chunk.Offset = offset;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunk.Type);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
        return chunk;
    }

    internal static void AddErrors(List<PngError> errors, PngError[] newErrors)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            errors.Add(newErrors[index]);
            index += 1;
        }
    }

    internal static void AddTypeErrors(List<PngError> errors, PngError[] newErrors, long typeOffset)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            PngError source = newErrors[index];
            AddError(errors, source.Code, source.Message, source.ChunkType, typeOffset + source.Offset);
            index += 1;
        }
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
}
