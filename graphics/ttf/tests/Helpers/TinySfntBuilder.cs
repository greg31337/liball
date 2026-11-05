using System.Buffers.Binary;

namespace ttf.Tests;

internal sealed class TinySfntBuilder
{
    private readonly List<TinySfntTable> tables = new List<TinySfntTable>();
    private uint scalerType = 0x00010000U;

    internal TinySfntBuilder SetScalerType(uint value)
    {
        scalerType = value;
        return this;
    }

    internal TinySfntBuilder AddTable(string tag, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        uint encodedTag = TestSfntEncoding.EncodeTag(tag);

        if (tables.Any(table => table.Tag == encodedTag))
        {
            throw new ArgumentException("A table with the same tag already exists.", nameof(tag));
        }

        tables.Add(new TinySfntTable(encodedTag, data.ToArray()));
        return this;
    }

    internal byte[] Build()
    {
        if (tables.Count > 4095)
        {
            throw new InvalidOperationException("A valid test SFNT cannot contain more than 4095 tables.");
        }

        List<TinySfntTable> ordered = tables
            .OrderBy(table => table.Tag)
            .Select(table => new TinySfntTable(table.Tag, table.Data.ToArray()))
            .ToList();

        int directoryLength = System.Convert.ToInt32(
            12L + ordered.Count * 16L);
        int nextOffset = directoryLength;

        foreach (TinySfntTable table in ordered)
        {
            table.Offset = nextOffset;
            nextOffset = System.Convert.ToInt32(
                (long)nextOffset + AlignFour(table.Data.Length));
        }

        byte[] font = new byte[nextOffset];
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(0, 4), scalerType);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(4, 2), (ushort)ordered.Count);
        WriteSearchFields(font, ordered.Count);

        TinySfntTable? headTable = null;

        for (int index = 0; index < ordered.Count; index += 1)
        {
            TinySfntTable table = ordered[index];
            int recordOffset = 12 + index * 16;
            bool isHead = table.Tag == TestSfntEncoding.EncodeTag("head") && table.Data.Length >= 12;
            uint checksum = CalculateChecksum(table.Data, isHead ? 8 : -1, isHead ? 12 : -1);

            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset, 4), table.Tag);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 4, 4), checksum);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 8, 4), (uint)table.Offset);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 12, 4), (uint)table.Data.Length);
            table.Data.CopyTo(font, table.Offset);

            if (isHead)
            {
                headTable = table;
            }
        }

        if (headTable != null)
        {
            font.AsSpan(headTable.Offset + 8, 4).Clear();
            uint checksum = CalculateChecksum(font, -1, -1);
            uint adjustment = (uint)(((ulong)0xB1B0AFBAU + 0x100000000UL - checksum) & 0xFFFFFFFFUL);
            BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(headTable.Offset + 8, 4), adjustment);
        }

        return font;
    }

    private static int AlignFour(int value)
    {
        return System.Convert.ToInt32(((long)value + 3L) / 4L * 4L);
    }

    private static void WriteSearchFields(byte[] font, int tableCount)
    {
        int maximumPowerOfTwo = 0;
        int entrySelector = 0;

        if (tableCount > 0)
        {
            maximumPowerOfTwo = 1;

            while (maximumPowerOfTwo <= tableCount / 2)
            {
                maximumPowerOfTwo *= 2;
                entrySelector += 1;
            }
        }

        int searchRange = maximumPowerOfTwo * 16;
        int rangeShift = tableCount * 16 - searchRange;
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(6, 2), (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(8, 2), (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(10, 2), (ushort)rangeShift);
    }

    private static uint CalculateChecksum(byte[] data, int zeroStart, int zeroEnd)
    {
        ulong sum = 0;

        for (int offset = 0; offset < data.Length; offset += 4)
        {
            uint word = 0;

            for (int index = 0; index < 4; index += 1)
            {
                int dataIndex = offset + index;
                byte value = 0;

                if (dataIndex < data.Length && (dataIndex < zeroStart || dataIndex >= zeroEnd))
                {
                    value = data[dataIndex];
                }

                word |= (uint)value << ((3 - index) * 8);
            }

            sum = (sum + word) & 0xFFFFFFFFUL;
        }

        return (uint)sum;
    }

    private sealed class TinySfntTable
    {
        internal TinySfntTable(uint tag, byte[] data)
        {
            Tag = tag;
            Data = data;
        }

        internal uint Tag { get; }

        internal byte[] Data { get; }

        internal int Offset { get; set; }
    }
}

internal static class TestSfntEncoding
{
    internal static uint EncodeTag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != 4)
        {
            throw new ArgumentException("An SFNT tag must contain exactly four characters.", nameof(value));
        }

        uint result = 0;

        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];

            if (character < 0x20 || character > 0x7E)
            {
                throw new ArgumentException("An SFNT tag must contain printable ASCII characters.", nameof(value));
            }

            result = (result << 8) | character;
        }

        return result;
    }
}
