namespace ttf;

internal static class Woff2NullTransformReconstructor
{
    internal static Woff2Reconstruction Reconstruct(
        Woff2Font font,
        FontLoadOptions options)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();
        EnsureAllTablesUseNullTransforms(font);
        EnsureEncodableFaceTableCounts(font);

        if (font.GetCollection().IsPresent())
        {
            return ReconstructCollection(font, options);
        }

        return ReconstructScalar(font, options);
    }

    static void EnsureAllTablesUseNullTransforms(Woff2Font font)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            if (font.GetTable(index).IsTransformed())
            {
                throw new System.InvalidOperationException(
                    "Null-transform reconstruction cannot process transformed WOFF 2 tables.");
            }

            index += 1;
        }
    }

    static void EnsureEncodableFaceTableCounts(Woff2Font font)
    {
        if (!font.GetCollection().IsPresent())
        {
            EnsureEncodableTableCount(font.GetTableCount());
            return;
        }

        Woff2Collection collection = font.GetCollection();
        int faceIndex = 0;

        while (faceIndex < collection.GetFaceCount())
        {
            EnsureEncodableTableCount(collection.GetFace(faceIndex).GetTableCount());
            faceIndex += 1;
        }
    }

    static void EnsureEncodableTableCount(int tableCount)
    {
        if (tableCount > 4095)
        {
            throw FontFormatException.Create(
                "An SFNT offset table cannot encode search fields for more than 4095 tables.");
        }
    }

    static Woff2Reconstruction ReconstructScalar(
        Woff2Font font,
        FontLoadOptions options)
    {
        int tableCount = font.GetTableCount();
        int totalLength = CalculateScalarLength(font, options.GetLimits());
        int[] physicalOffsets = new int[tableCount];
        int nextOffset = 12 + tableCount * 16;
        int index = 0;

        while (index < tableCount)
        {
            physicalOffsets[index] = nextOffset;
            nextOffset = AddPaddedLength(
                nextOffset,
                font.GetTable(index).GetDataLength(),
                options.GetLimits());
            index += 1;
        }

        int[] sortedIndices = CreateSortedTableIndices(font);
        uint[] tableChecksums = CalculateTableChecksums(font, options);
        BigEndianWriter writer = BigEndianWriter.Create(
            totalLength,
            options.GetLimits().GetMaxOutputBytes());
        WriteOffsetTable(writer, font.GetFlavor(), tableCount);
        index = 0;

        while (index < sortedIndices.Length)
        {
            int tableIndex = sortedIndices[index];
            Woff2Table table = font.GetTable(tableIndex);
            writer.WriteTag(table.GetTag());
            writer.WriteUInt32(tableChecksums[tableIndex]);
            writer.WriteOffset32(physicalOffsets[tableIndex]);
            writer.WriteOffset32(table.GetDataLength());
            index += 1;
        }

        WritePhysicalTables(writer, font, physicalOffsets, options);

        if (writer.GetLength() != totalLength)
        {
            throw FontFormatException.Create(
                "The reconstructed SFNT length is inconsistent.");
        }

        byte[] output = writer.CopyBytes();
        int headIndex = FindTableIndex(font, FontTags.Head);

        if (headIndex >= 0)
        {
            int headOffset = physicalOffsets[headIndex];
            uint adjustment = FontChecksum.CalculateChecksumAdjustment(output, headOffset);
            WriteUInt32(output, headOffset + 8, adjustment);
        }

        return new Woff2Reconstruction(
            false,
            FontData.Create(output),
            new int[] { 0 });
    }

    static Woff2Reconstruction ReconstructCollection(
        Woff2Font font,
        FontLoadOptions options)
    {
        Woff2Collection collection = font.GetCollection();
        int faceCount = collection.GetFaceCount();
        int[] faceOffsets = new int[faceCount];
        int headerLength = 12 + faceCount * 4;

        if (collection.GetVersion() == 0x00020000U)
        {
            headerLength += 12;
        }

        int nextOffset = headerLength;
        int faceIndex = 0;

        while (faceIndex < faceCount)
        {
            faceOffsets[faceIndex] = nextOffset;
            int faceDirectoryLength = 12 +
                collection.GetFace(faceIndex).GetTableCount() * 16;
            nextOffset = AddOutputLength(
                nextOffset,
                faceDirectoryLength,
                options.GetLimits());
            faceIndex += 1;
        }

        int[] tableOffsets = new int[font.GetTableCount()];
        int tableIndex = 0;

        while (tableIndex < font.GetTableCount())
        {
            tableOffsets[tableIndex] = nextOffset;
            nextOffset = AddPaddedLength(
                nextOffset,
                font.GetTable(tableIndex).GetDataLength(),
                options.GetLimits());
            tableIndex += 1;
        }

        uint[] tableChecksums = CalculateTableChecksums(font, options);

        BigEndianWriter writer = BigEndianWriter.Create(
            nextOffset,
            options.GetLimits().GetMaxOutputBytes());
        writer.WriteUInt32(0x74746366U);
        writer.WriteUInt32(collection.GetVersion());
        writer.WriteUInt32((uint)faceCount);
        faceIndex = 0;

        while (faceIndex < faceOffsets.Length)
        {
            writer.WriteOffset32(faceOffsets[faceIndex]);
            faceIndex += 1;
        }

        if (collection.GetVersion() == 0x00020000U)
        {
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
        }

        faceIndex = 0;

        while (faceIndex < faceCount)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            Woff2CollectionFace face = collection.GetFace(faceIndex);

            if (writer.GetLength() != faceOffsets[faceIndex])
            {
                throw FontFormatException.Create(
                    "The reconstructed TTC face-directory layout is inconsistent.");
            }

            WriteCollectionFaceDirectory(
                writer,
                font,
                face,
                tableOffsets,
                tableChecksums);
            faceIndex += 1;
        }

        WritePhysicalTables(writer, font, tableOffsets, options);

        if (writer.GetLength() != nextOffset)
        {
            throw FontFormatException.Create(
                "The reconstructed TTC length is inconsistent.");
        }

        byte[] output = writer.CopyBytes();
        ZeroCollectionHeadAdjustments(output, font, tableOffsets);
        return new Woff2Reconstruction(true, FontData.Create(output), faceOffsets);
    }

    static int CalculateScalarLength(Woff2Font font, FontLimits limits)
    {
        long length = 12L + font.GetTableCount() * 16L;
        int index = 0;

        while (index < font.GetTableCount())
        {
            length = AddPaddedLengthLong(
                length,
                font.GetTable(index).GetDataLength(),
                limits);
            index += 1;
        }

        return (int)length;
    }

    static int AddOutputLength(int total, int additionalLength, FontLimits limits)
    {
        if (additionalLength < 0 || additionalLength > int.MaxValue - total)
        {
            throw FontLimitException.Create("The reconstructed WOFF 2 output is too large.");
        }

        int result = total + additionalLength;

        if (result > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "The reconstructed WOFF 2 output exceeds the configured limit.");
        }

        return result;
    }

    static int AddPaddedLength(int total, int length, FontLimits limits)
    {
        int paddedLength = CheckedFontMath.AlignFour(length);
        return AddOutputLength(total, paddedLength, limits);
    }

    static long AddPaddedLengthLong(long total, int length, FontLimits limits)
    {
        long paddedLength = ((long)length + 3L) & -4L;

        if (total > int.MaxValue - paddedLength)
        {
            throw FontLimitException.Create("The reconstructed WOFF 2 output is too large.");
        }

        long result = total + paddedLength;

        if (result > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "The reconstructed WOFF 2 output exceeds the configured limit.");
        }

        return result;
    }

    static void WriteOffsetTable(BigEndianWriter writer, uint flavor, int tableCount)
    {
        int searchRange = CalculateSearchRange(tableCount);
        writer.WriteUInt32(flavor);
        writer.WriteUInt16((ushort)tableCount);
        writer.WriteUInt16((ushort)searchRange);
        writer.WriteUInt16((ushort)CalculateEntrySelector(tableCount));
        writer.WriteUInt16((ushort)(tableCount * 16 - searchRange));
    }

    static void WriteCollectionFaceDirectory(
        BigEndianWriter writer,
        Woff2Font font,
        Woff2CollectionFace face,
        int[] tableOffsets,
        uint[] tableChecksums)
    {
        int[] sortedFaceIndices = CreateSortedFaceTableIndices(font, face);
        WriteOffsetTable(writer, face.GetFlavor(), sortedFaceIndices.Length);
        int index = 0;

        while (index < sortedFaceIndices.Length)
        {
            int tableIndex = sortedFaceIndices[index];
            Woff2Table table = font.GetTable(tableIndex);
            writer.WriteTag(table.GetTag());
            writer.WriteUInt32(tableChecksums[tableIndex]);
            writer.WriteOffset32(tableOffsets[tableIndex]);
            writer.WriteOffset32(table.GetDataLength());
            index += 1;
        }
    }

    static uint CalculateTableChecksum(Woff2Table table)
    {
        ReadOnlySpan<byte> data = table.GetDataSlice().GetMemory().Span;

        if (table.GetTag() == FontTags.Head)
        {
            return FontChecksum.CalculateHeadTable(data);
        }

        return FontChecksum.CalculateTable(data);
    }

    static uint[] CalculateTableChecksums(
        Woff2Font font,
        FontLoadOptions options)
    {
        uint[] checksums = new uint[font.GetTableCount()];
        int index = 0;

        while (index < checksums.Length)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            checksums[index] = CalculateTableChecksum(font.GetTable(index));
            index += 1;
        }

        return checksums;
    }

    static void WritePhysicalTables(
        BigEndianWriter writer,
        Woff2Font font,
        int[] tableOffsets,
        FontLoadOptions options)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();

            if (writer.GetLength() != tableOffsets[index])
            {
                throw FontFormatException.Create(
                    "The reconstructed WOFF 2 table layout is inconsistent.");
            }

            writer.WriteMemory(font.GetTable(index).GetDataSlice().GetMemory());
            writer.WritePaddingToFourBytes();
            index += 1;
        }
    }

    static int[] CreateSortedTableIndices(Woff2Font font)
    {
        int[] indices = new int[font.GetTableCount()];
        int index = 0;

        while (index < indices.Length)
        {
            indices[index] = index;
            index += 1;
        }

        SortIndicesByTag(indices, font);
        return indices;
    }

    static int[] CreateSortedFaceTableIndices(
        Woff2Font font,
        Woff2CollectionFace face)
    {
        int[] indices = new int[face.GetTableCount()];
        int index = 0;

        while (index < indices.Length)
        {
            indices[index] = face.GetTableIndex(index);
            index += 1;
        }

        SortIndicesByTag(indices, font);
        return indices;
    }

    static void SortIndicesByTag(int[] indices, Woff2Font font)
    {
        int index = 1;

        while (index < indices.Length)
        {
            int current = indices[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    font.GetTable(indices[insertionIndex - 1]).GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    font.GetTable(current).GetTag()))
            {
                indices[insertionIndex] = indices[insertionIndex - 1];
                insertionIndex -= 1;
            }

            indices[insertionIndex] = current;
            index += 1;
        }
    }

    static int CalculateSearchRange(int tableCount)
    {
        int maximumPowerOfTwo = 1;

        while (maximumPowerOfTwo <= tableCount / 2)
        {
            maximumPowerOfTwo *= 2;
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

    static int FindTableIndex(Woff2Font font, int tag)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            if (font.GetTable(index).GetTag() == tag)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static void ZeroCollectionHeadAdjustments(
        byte[] output,
        Woff2Font font,
        int[] tableOffsets)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            Woff2Table table = font.GetTable(index);

            if (table.GetTag() == FontTags.Head)
            {
                if (table.GetDataLength() < 12)
                {
                    throw FontFormatException.Create(
                        "A reconstructed WOFF 2 head table is truncated.");
                }

                WriteUInt32(output, tableOffsets[index] + 8, 0);
            }

            index += 1;
        }
    }

    static void WriteUInt32(byte[] output, int offset, uint value)
    {
        output[offset] = (byte)((value >> 24) & 0xFFU);
        output[offset + 1] = (byte)((value >> 16) & 0xFFU);
        output[offset + 2] = (byte)((value >> 8) & 0xFFU);
        output[offset + 3] = (byte)(value & 0xFFU);
    }
}

internal class Woff2Reconstruction
{
    bool collection;
    FontData data;
    int[] faceOffsets;

    internal Woff2Reconstruction(bool collection, FontData data, int[] faceOffsets)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (faceOffsets == null)
        {
            throw new System.ArgumentNullException("faceOffsets");
        }

        this.collection = collection;
        this.data = data;
        this.faceOffsets = faceOffsets;
    }

    internal bool IsCollection()
    {
        return collection;
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }

    internal FontData GetData()
    {
        return data;
    }

    internal int GetFaceCount()
    {
        return faceOffsets.Length;
    }

    internal int GetFaceOffset(int index)
    {
        if (index < 0 || index >= faceOffsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return faceOffsets[index];
    }
}
