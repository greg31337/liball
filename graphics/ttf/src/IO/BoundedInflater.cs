namespace ttf;

internal static class BoundedInflater
{
    internal static byte[] InflateGzip(
        ReadOnlySpan<byte> compressedData,
        FontLimits limits)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (compressedData.Length < 18 ||
            compressedData[0] != 0x1F ||
            compressedData[1] != 0x8B ||
            compressedData[2] != 0x08)
        {
            throw FontFormatException.Create(
                "An SVG gzip document has an invalid header.");
        }

        int trailerOffset = compressedData.Length - 4;
        uint expandedLengthValue =
            compressedData[trailerOffset] |
            ((uint)compressedData[trailerOffset + 1] << 8) |
            ((uint)compressedData[trailerOffset + 2] << 16) |
            ((uint)compressedData[trailerOffset + 3] << 24);

        if (expandedLengthValue > int.MaxValue ||
            expandedLengthValue > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create(
                "Decompressed gzip data exceeds the configured size limit.");
        }

        int expandedLength = (int)expandedLengthValue;
        long ratioLimit = (long)compressedData.Length *
            limits.GetMaxDecompressionRatio();

        if (expandedLength > ratioLimit)
        {
            throw FontLimitException.Create(
                "Decompressed gzip data exceeds the configured ratio limit.");
        }

        byte[] output = new byte[expandedLength];
        byte[] inputBytes = compressedData.ToArray();

        using (System.IO.MemoryStream input =
            new System.IO.MemoryStream(inputBytes, false))
        {
            using (System.IO.Compression.GZipStream inflater =
                new System.IO.Compression.GZipStream(
                    input,
                    System.IO.Compression.CompressionMode.Decompress,
                    false))
            {
                int outputOffset = 0;

                while (outputOffset < output.Length)
                {
                    int read = inflater.Read(
                        output,
                        outputOffset,
                        output.Length - outputOffset);

                    if (read == 0)
                    {
                        break;
                    }

                    outputOffset += read;
                }

                if (outputOffset != output.Length || inflater.ReadByte() != -1)
                {
                    throw FontFormatException.Create(
                        "Gzip data does not match its declared expanded length.");
                }
            }
        }

        return output;
    }

    internal static byte[] InflateZlib(
        ReadOnlySpan<byte> compressedData,
        int expectedLength,
        FontLimits limits)
    {
        if (expectedLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("expectedLength");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (expectedLength > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create("Decompressed data exceeds the configured size limit.");
        }

        if (compressedData.Length == 0)
        {
            throw FontFormatException.Create("Compressed data is empty.");
        }

        long ratioLimit = (long)compressedData.Length * limits.GetMaxDecompressionRatio();

        if (expectedLength > ratioLimit)
        {
            throw FontLimitException.Create("Decompressed data exceeds the configured ratio limit.");
        }

        byte[] output = new byte[expectedLength];
        byte[] inputBytes = compressedData.ToArray();

        using (System.IO.MemoryStream input = new System.IO.MemoryStream(inputBytes, false))
        {
            using (System.IO.Compression.ZLibStream inflater =
                new System.IO.Compression.ZLibStream(
                    input,
                    System.IO.Compression.CompressionMode.Decompress,
                    false))
            {
                int outputOffset = 0;

                while (outputOffset < output.Length)
                {
                    int read = inflater.Read(
                        output,
                        outputOffset,
                        output.Length - outputOffset);

                    if (read == 0)
                    {
                        break;
                    }

                    outputOffset += read;
                }

                if (outputOffset != output.Length || inflater.ReadByte() != -1)
                {
                    throw FontFormatException.Create(
                        "Compressed data does not expand to the declared length.");
                }
            }
        }

        return output;
    }
}
