namespace png;

internal class PngUnknownChunk
{
    internal PngChunk Chunk = new PngChunk();
    internal string Type = "";
    internal int OriginalIndex;
    internal long OriginalOffset;
    internal bool IsAncillary;
    internal bool IsSafeToCopy;
    internal PngChunkDisposition Disposition = PngChunkDisposition.Preserve;
    internal byte[] ReplacementData = new byte[0];
}

internal class PngUnknownChunkPreservation
{
    internal static PngUnknownChunk Create(PngChunk chunk, int originalIndex)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (originalIndex < 0)
        {
            throw new ArgumentOutOfRangeException("originalIndex");
        }

        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunk.Type);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.Errors[0].Message);
        }

        if (PngKnownChunkTypes.IsKnown(chunk.Type))
        {
            throw new ArgumentException("Known chunk types cannot be stored as unknown chunks.");
        }

        PngChunkType chunkType = PngChunkTypeParser.Parse(
            (byte)chunk.Type[0],
            (byte)chunk.Type[1],
            (byte)chunk.Type[2],
            (byte)chunk.Type[3]);
        PngUnknownChunk unknownChunk = new PngUnknownChunk();
        unknownChunk.Chunk = chunk;
        unknownChunk.Type = chunk.Type;
        unknownChunk.OriginalIndex = originalIndex;
        unknownChunk.OriginalOffset = chunk.Offset;
        unknownChunk.IsAncillary = PngChunkTypeFlags.IsAncillary(chunkType);
        unknownChunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
        unknownChunk.Disposition = PngChunkDisposition.Preserve;
        unknownChunk.ReplacementData = new byte[0];
        return unknownChunk;
    }

    internal static bool ShouldWrite(PngUnknownChunk unknownChunk)
    {
        ArgumentNullException.ThrowIfNull(unknownChunk);

        switch (unknownChunk.Disposition)
        {
            case PngChunkDisposition.Preserve:
                return true;
            case PngChunkDisposition.Drop:
                return false;
            case PngChunkDisposition.Replace:
                return true;
            default:
                throw new ArgumentOutOfRangeException("Disposition");
        }
    }

    internal static PngChunk CreateOutputChunk(PngUnknownChunk unknownChunk)
    {
        ArgumentNullException.ThrowIfNull(unknownChunk);

        if (!ShouldWrite(unknownChunk))
        {
            throw new InvalidOperationException("Dropped unknown chunks do not produce output chunks.");
        }

        if (unknownChunk.Disposition == PngChunkDisposition.Replace)
        {
            ArgumentNullException.ThrowIfNull(unknownChunk.ReplacementData);
            return CreateChunk(unknownChunk.Type, unknownChunk.ReplacementData, unknownChunk.OriginalOffset);
        }

        ArgumentNullException.ThrowIfNull(unknownChunk.Chunk);
        ArgumentNullException.ThrowIfNull(unknownChunk.Chunk.Data);
        return CreateChunk(unknownChunk.Chunk.Type, unknownChunk.Chunk.Data, unknownChunk.OriginalOffset);
    }

    internal static PngChunk CreateChunk(string chunkTypeName, byte[] chunkData, long offset)
    {
        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunkTypeName);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.Errors[0].Message);
        }

        byte[] dataCopy = new byte[chunkData.Length];
        int index = 0;
        while (index < chunkData.Length)
        {
            dataCopy[index] = chunkData[index];
            index += 1;
        }

        PngChunkType chunkType = PngChunkTypeParser.Parse(
            (byte)chunkTypeName[0],
            (byte)chunkTypeName[1],
            (byte)chunkTypeName[2],
            (byte)chunkTypeName[3]);
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkTypeName;
        chunk.Data = dataCopy;
        chunk.Crc = PngCrc32.CalculateChunkCrc(chunkTypeName, dataCopy);
        chunk.Offset = offset;
        chunk.Length = (uint)dataCopy.Length;
        chunk.IsKnown = false;
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
        return chunk;
    }
}
