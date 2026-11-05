namespace png;

/// <summary>
/// Calculates the CRC-32 values used to protect PNG chunk type and data bytes.
/// </summary>
internal static class PngCrc32
{
    internal const uint Polynomial = 0xEDB88320;
    internal const uint InitialValue = 0xFFFFFFFF;
    internal const uint FinalXorValue = 0xFFFFFFFF;

    internal static uint[] CreateTable()
    {
        uint[] table = new uint[256];
        int index = 0;
        while (index < 256)
        {
            uint value = (uint)index;
            int bit = 0;
            while (bit < 8)
            {
                if ((value & 1u) == 1u)
                {
                    value = Polynomial ^ (value >> 1);
                }
                else
                {
                    value = value >> 1;
                }

                bit += 1;
            }

            table[index] = value;
            index += 1;
        }

        return table;
    }

    internal static uint CalculateSpan(ReadOnlySpan<byte> data)
    {
        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    internal static uint CalculateChunkCrcSpan(string chunkType, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        if (chunkType.Length != 4)
        {
            throw new ArgumentException("PNG chunk type must contain exactly 4 characters.");
        }

        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = 0;
        while (index < 4)
        {
            char character = chunkType[index];
            if (character > 255)
            {
                throw new ArgumentException("PNG chunk type characters must fit in one byte.");
            }

            crc = UpdateCrc(crc, (byte)character, table);
            index += 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    internal static uint CalculateChunkCrcSpans(ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> data)
    {
        if (chunkType.Length != 4)
        {
            throw new ArgumentException("PNG chunk type must contain exactly 4 bytes.");
        }

        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = 0;
        while (index < chunkType.Length)
        {
            crc = UpdateCrc(crc, chunkType[index], table);
            index += 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    /// <summary>
    /// Calculates a PNG-compatible CRC-32 over the requested data bytes.
    /// </summary>
    /// <param name="data">The bytes to include in the CRC.</param>
    /// <returns>The finalized unsigned CRC-32 value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    public static uint Calculate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Calculate(data, 0, data.Length);
    }

    /// <summary>
    /// Calculates a PNG-compatible CRC-32 over the requested data bytes.
    /// </summary>
    /// <param name="data">The array containing the bytes to include.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <param name="count">The number of bytes to include.</param>
    /// <returns>The finalized unsigned CRC-32 value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offset or count does not describe a valid array range.</exception>
    public static uint Calculate(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (count < 0 || offset > data.Length - count)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    /// <summary>
    /// Calculates the CRC-32 over a four-byte chunk type followed by its data bytes.
    /// </summary>
    /// <param name="chunkType">The case-sensitive four-character chunk type.</param>
    /// <param name="data">The chunk payload bytes.</param>
    /// <returns>The finalized unsigned CRC-32 value stored in PNG chunk framing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when chunkType or data is null.</exception>
    /// <exception cref="ArgumentException">Thrown when chunkType is not exactly four single-byte characters.</exception>
    public static uint CalculateChunkCrc(string chunkType, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(chunkType);
        ArgumentNullException.ThrowIfNull(data);

        if (chunkType.Length != 4)
        {
            throw new ArgumentException("PNG chunk type must contain exactly 4 characters.");
        }

        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = 0;
        while (index < 4)
        {
            char character = chunkType[index];
            if (character > 255)
            {
                throw new ArgumentException("PNG chunk type characters must fit in one byte.");
            }

            crc = UpdateCrc(crc, (byte)character, table);
            index += 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    /// <summary>
    /// Calculates the CRC-32 over a four-byte chunk type followed by its data bytes.
    /// </summary>
    /// <param name="chunkType">Exactly four chunk-type bytes.</param>
    /// <param name="data">The chunk payload bytes.</param>
    /// <returns>The finalized unsigned CRC-32 value stored in PNG chunk framing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when chunkType or data is null.</exception>
    /// <exception cref="ArgumentException">Thrown when chunkType does not contain exactly four bytes.</exception>
    public static uint CalculateChunkCrc(byte[] chunkType, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(chunkType);
        ArgumentNullException.ThrowIfNull(data);

        if (chunkType.Length != 4)
        {
            throw new ArgumentException("PNG chunk type must contain exactly 4 bytes.");
        }

        uint[] table = CreateTable();
        uint crc = InitialValue;
        int index = 0;
        while (index < 4)
        {
            crc = UpdateCrc(crc, chunkType[index], table);
            index += 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index], table);
            index += 1;
        }

        return crc ^ FinalXorValue;
    }

    internal static uint UpdateCrc(uint crc, byte value, uint[] table)
    {
        int tableIndex = (int)((crc ^ value) & 0xFF);
        return table[tableIndex] ^ (crc >> 8);
    }
}
