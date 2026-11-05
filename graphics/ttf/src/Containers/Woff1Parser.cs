namespace ttf;

internal static class Woff1Parser
{
    const uint Signature = 0x774F4646U;

    internal static Woff1Font Parse(FontData fontData, FontLoadOptions options)
    {
        if (fontData == null)
        {
            throw new System.ArgumentNullException("fontData");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        System.Threading.CancellationToken cancellationToken = options.GetCancellationToken();
        cancellationToken.ThrowIfCancellationRequested();
        FontLimits limits = options.GetLimits();
        int fileLength = fontData.GetLength();

        if (fileLength > limits.GetMaxInputBytes())
        {
            throw FontLimitException.Create("The WOFF file exceeds the configured input limit.");
        }

        BigEndianReader reader = new BigEndianReader(fontData.GetSlice(0, fileLength));

        if (reader.ReadUInt32() != Signature)
        {
            throw FontFormatException.Create("The WOFF 1 signature is invalid.");
        }

        uint flavor = reader.ReadUInt32();
        uint declaredLength = reader.ReadUInt32();
        int tableCount = reader.ReadUInt16();
        int reserved = reader.ReadUInt16();
        uint declaredSfntSize = reader.ReadUInt32();
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        uint metadataOffsetValue = reader.ReadUInt32();
        uint metadataLengthValue = reader.ReadUInt32();
        uint metadataOriginalLengthValue = reader.ReadUInt32();
        uint privateOffsetValue = reader.ReadUInt32();
        uint privateLengthValue = reader.ReadUInt32();

        ValidateHeader(
            declaredLength,
            fileLength,
            tableCount,
            reserved,
            declaredSfntSize,
            metadataOffsetValue,
            metadataLengthValue,
            metadataOriginalLengthValue,
            privateOffsetValue,
            privateLengthValue,
            limits);

        int directoryEnd = 44 + tableCount * 20;
        Woff1DirectoryEntry[] entries = new Woff1DirectoryEntry[tableCount];
        long computedSfntSize = 12L + tableCount * 16L;
        int index = 0;

        while (index < tableCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int tag = reader.ReadTag();
            uint offsetValue = reader.ReadUInt32();
            uint compressedLengthValue = reader.ReadUInt32();
            uint originalLengthValue = reader.ReadUInt32();
            uint checksum = reader.ReadUInt32();

            if (index > 0 && CheckedFontMath.ReinterpretInt32AsUInt32(
                entries[index - 1].GetTag()) >=
                CheckedFontMath.ReinterpretInt32AsUInt32(tag))
            {
                throw FontFormatException.Create("The WOFF table directory is not strictly sorted by tag.");
            }

            long offset = offsetValue;

            if (compressedLengthValue > originalLengthValue)
            {
                throw FontFormatException.Create("A WOFF compressed table is larger than its original table.");
            }

            ValidateAlignedRange(offset, compressedLengthValue, fileLength, "WOFF table");
            int compressedLength = ConvertBoundedLength(
                compressedLengthValue,
                int.MaxValue,
                "A WOFF compressed table length exceeds the supported range.");
            int originalLength = ConvertBoundedLength(
                originalLengthValue,
                limits.GetMaxDecompressedBytes(),
                "A WOFF table exceeds the configured decompression limit.");
            computedSfntSize = AddPaddedLength(computedSfntSize, originalLength);
            entries[index] = new Woff1DirectoryEntry(
                tag,
                offset,
                compressedLength,
                originalLength,
                checksum,
                44L + index * 20L + 16L);
            index += 1;
        }

        if (computedSfntSize != declaredSfntSize || computedSfntSize % 4L != 0)
        {
            throw FontFormatException.Create("The WOFF total SFNT size is invalid.");
        }

        if (computedSfntSize > limits.GetMaxOutputBytes() ||
            computedSfntSize > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create("The reconstructed SFNT exceeds a configured output limit.");
        }

        Woff1DirectoryEntry[] physicalEntries = entries.AsSpan().ToArray();
        SortByOffset(physicalEntries);
        long nextBlockOffset = directoryEnd;
        index = 0;

        while (index < physicalEntries.Length)
        {
            Woff1DirectoryEntry entry = physicalEntries[index];

            if (entry.GetOffset() != nextBlockOffset)
            {
                throw FontFormatException.Create("WOFF tables overlap or contain an extraneous gap.");
            }

            long tableEnd = entry.GetOffset() + entry.GetCompressedLength();
            long paddedEnd = AlignFour(tableEnd);
            ValidateZeroPadding(fontData, tableEnd, paddedEnd);
            nextBlockOffset = paddedEnd;
            index += 1;
        }

        bool hasMetadata = metadataOffsetValue != 0;
        bool hasPrivateData = privateOffsetValue != 0;
        int metadataLength = 0;
        int metadataOriginalLength = 0;
        long metadataOffset = 0;
        int privateLength = 0;
        long privateOffset = 0;

        if (hasMetadata)
        {
            metadataOffset = metadataOffsetValue;
            ValidateAlignedRange(
                metadataOffset,
                metadataLengthValue,
                fileLength,
                "WOFF metadata");
            metadataLength = ConvertBoundedLength(
                metadataLengthValue,
                int.MaxValue,
                "The WOFF metadata length exceeds the supported range.");
            metadataOriginalLength = ConvertBoundedLength(
                metadataOriginalLengthValue,
                limits.GetMaxDecompressedBytes(),
                "The WOFF metadata exceeds the configured decompression limit.");
            if (metadataOffset != nextBlockOffset)
            {
                throw FontFormatException.Create("The WOFF metadata does not immediately follow the tables.");
            }

            nextBlockOffset = metadataOffset + metadataLength;
        }

        if (hasPrivateData)
        {
            privateOffset = privateOffsetValue;
            ValidateAlignedRange(
                privateOffset,
                privateLengthValue,
                fileLength,
                "WOFF private data");
            privateLength = ConvertBoundedLength(
                privateLengthValue,
                int.MaxValue,
                "The WOFF private-data length exceeds the supported range.");
            long alignedBlockOffset = AlignFour(nextBlockOffset);
            ValidateZeroPadding(fontData, nextBlockOffset, alignedBlockOffset);

            if (privateOffset != alignedBlockOffset)
            {
                throw FontFormatException.Create("The WOFF private data does not immediately follow preceding data.");
            }

            nextBlockOffset = privateOffset + privateLength;
        }

        if (nextBlockOffset != fileLength)
        {
            throw FontFormatException.Create("The WOFF file contains extraneous trailing data.");
        }

        long totalDecompressed = computedSfntSize + metadataOriginalLength;

        if (totalDecompressed > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create("WOFF decompression exceeds the configured cumulative limit.");
        }

        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        DecodeAndVerifyTables(fontData, entries, options, diagnostics);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] metadataBytes = DecodeMetadata(
            fontData,
            metadataOffset,
            metadataLength,
            metadataOriginalLength,
            hasMetadata,
            limits);
        byte[] privateBytes = hasPrivateData
            ? fontData.GetSlice((int)privateOffset, privateLength).CopyBytes()
            : System.Array.Empty<byte>();
        FontData reconstructedSfnt = ReconstructSfnt(
            flavor,
            (int)declaredSfntSize,
            entries,
            physicalEntries,
            limits);
        VerifyWholeFont(reconstructedSfnt, entries, options, diagnostics);
        Woff1Table[] tables = CreateTables(entries, reconstructedSfnt);

        return new Woff1Font(
            flavor,
            majorVersion,
            minorVersion,
            tables,
            new Woff1Metadata(
                hasMetadata,
                metadataOffset,
                metadataLength,
                metadataOriginalLength,
                metadataBytes),
            new Woff1PrivateData(hasPrivateData, privateOffset, privateBytes),
            reconstructedSfnt,
            diagnostics.ToArray());
    }

    static void ValidateHeader(
        uint declaredLength,
        int fileLength,
        int tableCount,
        int reserved,
        uint declaredSfntSize,
        uint metadataOffset,
        uint metadataLength,
        uint metadataOriginalLength,
        uint privateOffset,
        uint privateLength,
        FontLimits limits)
    {
        if (declaredLength != (uint)fileLength)
        {
            throw FontFormatException.Create("The WOFF declared file length is invalid.");
        }

        if (tableCount == 0)
        {
            throw FontFormatException.Create("A WOFF file must contain at least one table.");
        }

        if (tableCount > limits.GetMaxTablesPerFace())
        {
            throw FontLimitException.Create("The WOFF table count exceeds the configured limit.");
        }

        if (44L + tableCount * 20L > fileLength)
        {
            throw FontFormatException.Create("The WOFF table directory is truncated.");
        }

        if (reserved != 0)
        {
            throw FontFormatException.Create("The WOFF reserved field must be zero.");
        }

        if (declaredSfntSize > int.MaxValue)
        {
            throw FontLimitException.Create("The WOFF total SFNT size exceeds the supported range.");
        }

        if (declaredSfntSize % 4U != 0)
        {
            throw FontFormatException.Create("The WOFF total SFNT size is invalid.");
        }

        bool metadataAbsent = metadataOffset == 0 &&
            metadataLength == 0 &&
            metadataOriginalLength == 0;
        bool metadataPresent = metadataOffset != 0 &&
            metadataLength != 0 &&
            metadataOriginalLength != 0;

        if (!metadataAbsent && !metadataPresent)
        {
            throw FontFormatException.Create("The WOFF metadata fields are inconsistent.");
        }

        bool privateAbsent = privateOffset == 0 && privateLength == 0;
        bool privatePresent = privateOffset != 0 && privateLength != 0;

        if (!privateAbsent && !privatePresent)
        {
            throw FontFormatException.Create("The WOFF private-data fields are inconsistent.");
        }
    }

    static int ConvertBoundedLength(uint value, int maximum, string message)
    {
        if (value > int.MaxValue)
        {
            throw FontLimitException.Create(message);
        }

        int length = (int)value;

        if (length > maximum)
        {
            throw FontLimitException.Create(message);
        }

        return length;
    }

    static void ValidateAlignedRange(long offset, long length, int fileLength, string description)
    {
        if (offset % 4L != 0)
        {
            throw FontFormatException.Create(description + " is not four-byte aligned.");
        }

        if (offset > fileLength || length > fileLength - offset)
        {
            throw FontFormatException.Create(description + " extends outside the WOFF file.");
        }
    }

    static long AddPaddedLength(long total, int length)
    {
        long paddedLength = AlignFour(length);

        if (total > int.MaxValue - paddedLength)
        {
            throw FontLimitException.Create("The reconstructed SFNT size exceeds the supported range.");
        }

        return total + paddedLength;
    }

    static long AlignFour(long value)
    {
        if (value > long.MaxValue - 3L)
        {
            throw FontFormatException.Create("WOFF alignment arithmetic overflowed.");
        }

        return (value + 3L) & -4L;
    }

    static void ValidateZeroPadding(FontData fontData, long start, long end)
    {
        ReadOnlySpan<byte> bytes = fontData.GetMemory().Span;
        long index = start;

        while (index < end)
        {
            if (index >= bytes.Length || bytes[(int)index] != 0)
            {
                throw FontFormatException.Create("WOFF alignment padding must contain only zero bytes.");
            }

            index += 1;
        }
    }

    static void SortByOffset(Woff1DirectoryEntry[] entries)
    {
        int index = 1;

        while (index < entries.Length)
        {
            Woff1DirectoryEntry current = entries[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                entries[insertionIndex - 1].GetOffset() > current.GetOffset())
            {
                entries[insertionIndex] = entries[insertionIndex - 1];
                insertionIndex -= 1;
            }

            entries[insertionIndex] = current;
            index += 1;
        }
    }

    static void DecodeAndVerifyTables(
        FontData fontData,
        Woff1DirectoryEntry[] entries,
        FontLoadOptions options,
        System.Collections.Generic.List<FontDiagnostic> diagnostics)
    {
        int index = 0;

        while (index < entries.Length)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            Woff1DirectoryEntry entry = entries[index];
            ReadOnlySpan<byte> storedData = fontData.GetMemory().Span.Slice(
                (int)entry.GetOffset(),
                entry.GetCompressedLength());
            byte[] decodedData = entry.IsCompressed()
                ? BoundedInflater.InflateZlib(
                    storedData,
                    entry.GetOriginalLength(),
                    options.GetLimits())
                : storedData.ToArray();
            entry.SetDecodedData(decodedData);

            if (options.GetChecksumPolicy() != FontChecksumPolicy.Ignore)
            {
                bool valid = entry.GetTag() == FontTags.Head
                    ? FontChecksum.VerifyHeadTable(decodedData, entry.GetOriginalChecksum())
                    : FontChecksum.VerifyTable(decodedData, entry.GetOriginalChecksum());

                if (!valid)
                {
                    HandleChecksumMismatch(
                        options,
                        diagnostics,
                        FontDiagnosticCode.TableChecksumMismatch,
                        "A WOFF table checksum does not match its directory entry.",
                        entry.GetTag(),
                        entry.GetChecksumFieldOffset(),
                        "woff1.tableDirectory.origChecksum");
                }
            }

            index += 1;
        }
    }

    static byte[] DecodeMetadata(
        FontData fontData,
        long offset,
        int compressedLength,
        int originalLength,
        bool present,
        FontLimits limits)
    {
        if (!present)
        {
            return System.Array.Empty<byte>();
        }

        return BoundedInflater.InflateZlib(
            fontData.GetMemory().Span.Slice((int)offset, compressedLength),
            originalLength,
            limits);
    }

    static FontData ReconstructSfnt(
        uint flavor,
        int totalLength,
        Woff1DirectoryEntry[] entries,
        Woff1DirectoryEntry[] physicalEntries,
        FontLimits limits)
    {
        int tableCount = entries.Length;
        int dataOffset = 12 + tableCount * 16;
        int index = 0;

        while (index < physicalEntries.Length)
        {
            physicalEntries[index].SetSfntOffset(dataOffset);
            dataOffset = (int)AddPaddedLength(dataOffset, physicalEntries[index].GetOriginalLength());
            index += 1;
        }

        if (dataOffset != totalLength)
        {
            throw FontFormatException.Create("The reconstructed SFNT size is inconsistent.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(totalLength, limits.GetMaxOutputBytes());
        writer.WriteUInt32(flavor);
        writer.WriteUInt16((ushort)tableCount);
        writer.WriteUInt16((ushort)CalculateSearchRange(tableCount));
        writer.WriteUInt16((ushort)CalculateEntrySelector(tableCount));
        writer.WriteUInt16((ushort)(tableCount * 16 - CalculateSearchRange(tableCount)));
        index = 0;

        while (index < entries.Length)
        {
            Woff1DirectoryEntry entry = entries[index];
            writer.WriteTag(entry.GetTag());
            writer.WriteUInt32(entry.GetOriginalChecksum());
            writer.WriteOffset32(entry.GetSfntOffset());
            writer.WriteOffset32(entry.GetOriginalLength());
            index += 1;
        }

        index = 0;

        while (index < physicalEntries.Length)
        {
            Woff1DirectoryEntry entry = physicalEntries[index];

            if (writer.GetLength() != entry.GetSfntOffset())
            {
                throw FontFormatException.Create("The reconstructed SFNT table layout is inconsistent.");
            }

            writer.WriteBytes(entry.GetDecodedData());
            writer.WritePaddingToFourBytes();
            index += 1;
        }

        return FontData.Create(writer.CopyBytes());
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

    static void VerifyWholeFont(
        FontData sfnt,
        Woff1DirectoryEntry[] entries,
        FontLoadOptions options,
        System.Collections.Generic.List<FontDiagnostic> diagnostics)
    {
        if (options.GetChecksumPolicy() == FontChecksumPolicy.Ignore ||
            FindEntry(entries, FontTags.Head) == null)
        {
            return;
        }

        if (!FontChecksum.VerifyFont(sfnt.GetMemory().Span))
        {
            Woff1DirectoryEntry? head = FindEntry(entries, FontTags.Head);
            System.ArgumentNullException.ThrowIfNull(head);
            HandleChecksumMismatch(
                options,
                diagnostics,
                FontDiagnosticCode.FontChecksumMismatch,
                "The reconstructed SFNT whole-font checksum is invalid.",
                FontTags.Head,
                head.GetOffset() + 8L,
                "woff1.tables.head.checkSumAdjustment");
        }
    }

    static Woff1DirectoryEntry? FindEntry(Woff1DirectoryEntry[] entries, int tag)
    {
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetTag() == tag)
            {
                return entries[index];
            }

            index += 1;
        }

        return null;
    }

    static void HandleChecksumMismatch(
        FontLoadOptions options,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontDiagnosticCode code,
        string message,
        int tag,
        long offset,
        string path)
    {
        if (options.GetChecksumPolicy() == FontChecksumPolicy.RequireValid)
        {
            throw FontFormatException.Create(message);
        }

        if (diagnostics.Count >= options.GetLimits().GetMaxDiagnostics())
        {
            throw FontLimitException.Create("The configured diagnostic count limit was exceeded.");
        }

        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            code,
            message,
            0,
            tag,
            offset,
            path));
    }

    static Woff1Table[] CreateTables(Woff1DirectoryEntry[] entries, FontData sfnt)
    {
        Woff1Table[] tables = new Woff1Table[entries.Length];
        int index = 0;

        while (index < entries.Length)
        {
            Woff1DirectoryEntry entry = entries[index];
            tables[index] = new Woff1Table(
                entry.GetTag(),
                entry.GetOffset(),
                entry.GetCompressedLength(),
                entry.GetOriginalLength(),
                entry.GetOriginalChecksum(),
                entry.GetSfntOffset(),
                sfnt.GetSlice(entry.GetSfntOffset(), entry.GetOriginalLength()));
            index += 1;
        }

        return tables;
    }
}

internal class Woff1DirectoryEntry
{
    int tag;
    long offset;
    int compressedLength;
    int originalLength;
    uint originalChecksum;
    long checksumFieldOffset;
    int sfntOffset;
    byte[] decodedData;

    internal Woff1DirectoryEntry(
        int tag,
        long offset,
        int compressedLength,
        int originalLength,
        uint originalChecksum,
        long checksumFieldOffset)
    {
        this.tag = tag;
        this.offset = offset;
        this.compressedLength = compressedLength;
        this.originalLength = originalLength;
        this.originalChecksum = originalChecksum;
        this.checksumFieldOffset = checksumFieldOffset;
        sfntOffset = 0;
        decodedData = System.Array.Empty<byte>();
    }

    internal int GetTag()
    {
        return tag;
    }

    internal long GetOffset()
    {
        return offset;
    }

    internal int GetCompressedLength()
    {
        return compressedLength;
    }

    internal int GetOriginalLength()
    {
        return originalLength;
    }

    internal uint GetOriginalChecksum()
    {
        return originalChecksum;
    }

    internal long GetChecksumFieldOffset()
    {
        return checksumFieldOffset;
    }

    internal bool IsCompressed()
    {
        return compressedLength < originalLength;
    }

    internal void SetDecodedData(byte[] value)
    {
        decodedData = value;
    }

    internal byte[] GetDecodedData()
    {
        return decodedData;
    }

    internal void SetSfntOffset(int value)
    {
        sfntOffset = value;
    }

    internal int GetSfntOffset()
    {
        return sfntOffset;
    }
}
