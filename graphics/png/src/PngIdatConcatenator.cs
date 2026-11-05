namespace png;

internal class PngIdatConcatenator
{
    internal static byte[] Concatenate(PngChunkListResult chunkList)
    {
        return Concatenate(chunkList, new PngSecurityLimits());
    }

    internal static byte[] Concatenate(PngChunkListResult chunkList, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunkList);
        ArgumentNullException.ThrowIfNull(chunkList.Chunks);

        return Concatenate(chunkList.Chunks, limits);
    }

    internal static byte[] Concatenate(PngChunk[] chunks)
    {
        return Concatenate(chunks, new PngSecurityLimits());
    }

    internal static byte[] Concatenate(PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(limits);

        long totalLength = CalculateTotalIdatBytes(chunks, limits);
        if (totalLength > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] result = new byte[(int)totalLength];
        int outputOffset = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunkReference(chunk);
            if (chunk.Type == PngConstants.ChunkIDAT)
            {
                ArgumentNullException.ThrowIfNull(chunk.Data);
                CopyChunkData(chunk.Data, result, outputOffset);
                outputOffset += chunk.Data.Length;
            }

            index += 1;
        }

        return result;
    }

    internal static long CalculateTotalIdatBytes(PngChunk[] chunks, PngSecurityLimits limits)
    {
        long totalLength = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunkReference(chunk);
            if (chunk.Type == PngConstants.ChunkIDAT)
            {
                ArgumentNullException.ThrowIfNull(chunk.Data);
                if (totalLength > long.MaxValue - chunk.Data.Length)
                {
                    throw new OverflowException();
                }

                totalLength += chunk.Data.Length;
                if (limits.MaxIdatBytes > 0 && totalLength > limits.MaxIdatBytes)
                {
                    throw new OverflowException();
                }
            }

            index += 1;
        }

        return totalLength;
    }

    internal static void ValidateChunkReference(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
    }

    internal static void CopyChunkData(byte[] source, byte[] destination, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index += 1;
        }
    }
}
