namespace ttf;

internal static class BoundedBrotliInflater
{
    internal static byte[] Inflate(
        ReadOnlySpan<byte> compressedData,
        int expectedLength,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expectedLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("expectedLength");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (expectedLength > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create("Decompressed data exceeds the configured size limit.");
        }

        if (compressedData.Length == 0)
        {
            throw FontFormatException.Create("Compressed Brotli data is empty.");
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
            using (System.IO.Compression.BrotliStream inflater =
                new System.IO.Compression.BrotliStream(
                    input,
                    System.IO.Compression.CompressionMode.Decompress,
                    false))
            {
                int outputOffset = 0;

                while (outputOffset < output.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                        "Brotli data does not expand to the declared length.");
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return output;
    }
}
