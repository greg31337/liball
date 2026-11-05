namespace ttf;

internal class SfntWriteTable
{
    int tag;
    byte[] data;

    internal SfntWriteTable(int tag, byte[]? data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.tag = tag;
        this.data = Copy(data);
    }

    internal int GetTag()
    {
        return tag;
    }

    internal byte[] CopyData()
    {
        return Copy(data);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] output = new byte[value.Length];
        System.Array.Copy(value, output, value.Length);
        return output;
    }
}

internal static class SfntWriter
{
    internal static byte[] Write(
        uint scalerType,
        SfntWriteTable[] tables,
        FontLimits limits)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (tables.Length > limits.GetMaxTablesPerFace())
        {
            throw FontLimitException.Create(
                "The standalone SFNT table count exceeds the configured per-face limit.");
        }

        if (tables.Length > 4095)
        {
            throw FontFormatException.Create(
                "An SFNT offset table cannot encode search fields for more than 4095 tables.");
        }

        SfntWriteTable[] ordered = CopyAndSort(tables);
        byte[][] tableData = new byte[ordered.Length][];
        int[] offsets = new int[ordered.Length];
        long totalLength = 12L + ordered.Length * 16L;
        int headIndex = -1;
        int index = 0;

        if (totalLength > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "The standalone SFNT directory exceeds the configured output limit.");
        }

        while (index < ordered.Length)
        {
            tableData[index] = ordered[index].CopyData();

            if (ordered[index].GetTag() == FontTags.Head)
            {
                if (tableData[index].Length < 12)
                {
                    throw FontFormatException.Create(
                        "The head table is truncated before checksumAdjustment.");
                }

                ZeroChecksumAdjustment(tableData[index]);
                headIndex = index;
            }

            long paddedLength = (tableData[index].Length + 3L) & -4L;

            if (totalLength > limits.GetMaxOutputBytes() - paddedLength)
            {
                throw FontLimitException.Create(
                    "The standalone SFNT exceeds the configured output limit.");
            }

            totalLength += paddedLength;
            index += 1;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            (int)totalLength,
            limits.GetMaxOutputBytes());
        WriteOffsetTable(writer, scalerType, ordered.Length);
        int nextOffset = 12 + ordered.Length * 16;
        index = 0;

        while (index < ordered.Length)
        {
            offsets[index] = nextOffset;
            uint checksum = ordered[index].GetTag() == FontTags.Head
                ? FontChecksum.CalculateHeadTable(tableData[index])
                : FontChecksum.CalculateTable(tableData[index]);
            writer.WriteTag(ordered[index].GetTag());
            writer.WriteUInt32(checksum);
            writer.WriteOffset32(offsets[index]);
            writer.WriteOffset32(tableData[index].Length);
            nextOffset += (tableData[index].Length + 3) & -4;
            index += 1;
        }

        index = 0;

        while (index < ordered.Length)
        {
            if (writer.GetLength() != offsets[index])
            {
                throw FontFormatException.Create(
                    "The standalone SFNT table layout is inconsistent.");
            }

            writer.WriteBytes(tableData[index]);
            writer.WritePaddingToFourBytes();
            index += 1;
        }

        if (writer.GetLength() != totalLength)
        {
            throw FontFormatException.Create(
                "The standalone SFNT length is inconsistent.");
        }

        byte[] result = writer.CopyBytes();

        if (headIndex >= 0)
        {
            uint adjustment = FontChecksum.CalculateChecksumAdjustment(
                result,
                offsets[headIndex]);
            WriteUInt32(result, offsets[headIndex] + 8, adjustment);
        }

        return result;
    }

    static SfntWriteTable[] CopyAndSort(SfntWriteTable[] tables)
    {
        SfntWriteTable[] ordered = new SfntWriteTable[tables.Length];
        int index = 0;

        while (index < tables.Length)
        {
            if (tables[index] == null)
            {
                throw new System.ArgumentNullException("tables");
            }

            ordered[index] = tables[index];
            index += 1;
        }

        index = 1;

        while (index < ordered.Length)
        {
            SfntWriteTable current = ordered[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    ordered[insertionIndex - 1].GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()))
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex -= 1;
            }

            ordered[insertionIndex] = current;
            index += 1;
        }

        index = 1;

        while (index < ordered.Length)
        {
            if (ordered[index - 1].GetTag() == ordered[index].GetTag())
            {
                throw FontFormatException.Create(
                    "An SFNT cannot contain duplicate table tags.");
            }

            index += 1;
        }

        return ordered;
    }

    static void WriteOffsetTable(
        BigEndianWriter writer,
        uint scalerType,
        int tableCount)
    {
        int searchRange = CalculateSearchRange(tableCount);
        writer.WriteUInt32(scalerType);
        writer.WriteUInt16((ushort)tableCount);
        writer.WriteUInt16((ushort)searchRange);
        writer.WriteUInt16((ushort)CalculateEntrySelector(tableCount));
        writer.WriteUInt16((ushort)(tableCount * 16 - searchRange));
    }

    static int CalculateSearchRange(int tableCount)
    {
        int maximumPowerOfTwo = 0;

        if (tableCount > 0)
        {
            maximumPowerOfTwo = 1;

            while (maximumPowerOfTwo <= tableCount / 2)
            {
                maximumPowerOfTwo *= 2;
            }
        }

        return maximumPowerOfTwo * 16;
    }

    static int CalculateEntrySelector(int tableCount)
    {
        int maximumPowerOfTwo = 1;
        int selector = 0;

        while (maximumPowerOfTwo <= tableCount / 2)
        {
            maximumPowerOfTwo *= 2;
            selector += 1;
        }

        return selector;
    }

    static void ZeroChecksumAdjustment(byte[] data)
    {
        data[8] = 0;
        data[9] = 0;
        data[10] = 0;
        data[11] = 0;
    }

    static void WriteUInt32(byte[] output, int offset, uint value)
    {
        output[offset] = (byte)((value >> 24) & 0xFFU);
        output[offset + 1] = (byte)((value >> 16) & 0xFFU);
        output[offset + 2] = (byte)((value >> 8) & 0xFFU);
        output[offset + 3] = (byte)(value & 0xFFU);
    }
}
