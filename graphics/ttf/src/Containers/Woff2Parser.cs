namespace ttf;

internal static class Woff2Parser
{
    const uint Signature = 0x774F4632U;
    const uint CollectionFlavor = 0x74746366U;

    internal static Woff2Font Parse(FontData fontData, FontLoadOptions options)
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
            throw FontLimitException.Create("The WOFF 2 file exceeds the configured input limit.");
        }

        BigEndianReader reader = new BigEndianReader(fontData.GetSlice(0, fileLength));

        if (reader.ReadUInt32() != Signature)
        {
            throw FontFormatException.Create("The WOFF 2 signature is invalid.");
        }

        uint flavor = reader.ReadUInt32();
        uint declaredLengthValue = reader.ReadUInt32();
        int tableCount = reader.ReadUInt16();
        int reserved = reader.ReadUInt16();
        uint totalSfntSize = reader.ReadUInt32();
        uint compressedLengthValue = reader.ReadUInt32();
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        uint metadataOffsetValue = reader.ReadUInt32();
        uint metadataLengthValue = reader.ReadUInt32();
        uint metadataOriginalLengthValue = reader.ReadUInt32();
        uint privateOffsetValue = reader.ReadUInt32();
        uint privateLengthValue = reader.ReadUInt32();

        ValidateHeader(
            flavor,
            declaredLengthValue,
            fileLength,
            tableCount,
            metadataOffsetValue,
            metadataLengthValue,
            metadataOriginalLengthValue,
            privateOffsetValue,
            privateLengthValue,
            limits);

        Woff2DirectoryEntry[] entries = new Woff2DirectoryEntry[tableCount];
        long decompressedLengthValue = 0;
        int index = 0;

        while (index < entries.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int flags = reader.ReadUInt8();
            int tagIndex = flags & 0x3F;
            int transformVersion = flags >> 6;
            bool customTag = tagIndex == 63;
            int tag = customTag ? reader.ReadTag() : Woff2KnownTags.GetTag(tagIndex);
            bool transformed = IsTransformed(tag, transformVersion);
            long originalLength = Woff2Integer.ReadUIntBase128(reader);
            long transformLength = transformed
                ? Woff2Integer.ReadUIntBase128(reader)
                : 0;

            if (tag == FontTags.Loca && transformed && transformLength != 0)
            {
                throw FontFormatException.Create(
                    "A transformed WOFF 2 loca table must have zero transform length.");
            }

            long storedLength = transformed ? transformLength : originalLength;

            if (storedLength > int.MaxValue ||
                decompressedLengthValue > int.MaxValue - storedLength)
            {
                throw FontLimitException.Create(
                    "The WOFF 2 decompressed table data exceeds the supported range.");
            }

            decompressedLengthValue += storedLength;

            if (decompressedLengthValue > limits.GetMaxDecompressedBytes())
            {
                throw FontLimitException.Create(
                    "The WOFF 2 decompressed table data exceeds the configured limit.");
            }

            entries[index] = new Woff2DirectoryEntry(
                index,
                flags,
                tagIndex,
                tag,
                transformVersion,
                customTag,
                transformed,
                originalLength,
                transformLength,
                (int)(decompressedLengthValue - storedLength),
                (int)storedLength);
            index += 1;
        }

        Woff2Collection collection;

        if (flavor == CollectionFlavor)
        {
            collection = ParseCollection(reader, entries, limits, cancellationToken);
            ValidateCollectionTablePairs(entries);
        }
        else
        {
            collection = new Woff2Collection(
                false,
                0,
                System.Array.Empty<Woff2CollectionFace>());
            ValidateScalarTableDirectory(entries);
        }

        int compressedDataOffset = reader.GetPosition();
        ValidateRange(
            compressedDataOffset,
            compressedLengthValue,
            fileLength,
            "The WOFF 2 compressed font data");
        int compressedDataLength = ConvertFileLength(
            compressedLengthValue,
            "The WOFF 2 compressed font-data length exceeds the supported range.");
        long nextBlockOffset = compressedDataOffset + compressedLengthValue;
        bool hasMetadata = metadataOffsetValue != 0;
        bool hasPrivateData = privateOffsetValue != 0;
        long metadataOffset = 0;
        int metadataLength = 0;
        int metadataOriginalLength = 0;
        long privateOffset = 0;
        int privateLength = 0;

        if (hasMetadata)
        {
            metadataOffset = metadataOffsetValue;
            ValidateRange(
                metadataOffset,
                metadataLengthValue,
                fileLength,
                "The WOFF 2 metadata");
            metadataLength = ConvertFileLength(
                metadataLengthValue,
                "The WOFF 2 metadata length exceeds the supported range.");
            metadataOriginalLength = ConvertDecompressedLength(
                metadataOriginalLengthValue,
                limits,
                "The WOFF 2 metadata exceeds the configured decompression limit.");
            long alignedOffset = AlignFour(nextBlockOffset);
            ValidateZeroPadding(fontData, nextBlockOffset, alignedOffset);

            if (metadataOffset != alignedOffset)
            {
                throw FontFormatException.Create(
                    "The WOFF 2 metadata does not immediately follow the compressed font data.");
            }

            nextBlockOffset = metadataOffset + metadataLength;
        }

        if (hasPrivateData)
        {
            privateOffset = privateOffsetValue;
            ValidateRange(
                privateOffset,
                privateLengthValue,
                fileLength,
                "The WOFF 2 private data");
            privateLength = ConvertFileLength(
                privateLengthValue,
                "The WOFF 2 private-data length exceeds the supported range.");
            long alignedOffset = AlignFour(nextBlockOffset);
            ValidateZeroPadding(fontData, nextBlockOffset, alignedOffset);

            if (privateOffset != alignedOffset)
            {
                throw FontFormatException.Create(
                    "The WOFF 2 private data does not immediately follow preceding data.");
            }

            nextBlockOffset = privateOffset + privateLength;
        }

        if (nextBlockOffset != fileLength)
        {
            throw FontFormatException.Create("The WOFF 2 file contains extraneous trailing data.");
        }

        if (decompressedLengthValue + metadataOriginalLength >
            limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create(
                "WOFF 2 decompression exceeds the configured cumulative limit.");
        }

        byte[] decompressedBytes = BoundedBrotliInflater.Inflate(
            fontData.GetMemory().Span.Slice(compressedDataOffset, compressedDataLength),
            (int)decompressedLengthValue,
            limits,
            cancellationToken);
        FontData decompressedData = FontData.Create(decompressedBytes);
        Woff2Table[] tables = CreateTables(entries, decompressedData);
        byte[] metadataBytes = hasMetadata
            ? BoundedBrotliInflater.Inflate(
                fontData.GetMemory().Span.Slice((int)metadataOffset, metadataLength),
                metadataOriginalLength,
                limits,
                cancellationToken)
            : System.Array.Empty<byte>();
        byte[] privateBytes = hasPrivateData
            ? fontData.GetSlice((int)privateOffset, privateLength).CopyBytes()
            : System.Array.Empty<byte>();

        return new Woff2Font(
            flavor,
            (int)declaredLengthValue,
            reserved,
            totalSfntSize,
            majorVersion,
            minorVersion,
            compressedDataOffset,
            compressedDataLength,
            decompressedData,
            tables,
            collection,
            new Woff2Metadata(
                hasMetadata,
                metadataOffset,
                metadataLength,
                metadataOriginalLength,
                metadataBytes),
            new Woff2PrivateData(hasPrivateData, privateOffset, privateBytes));
    }

    static void ValidateHeader(
        uint flavor,
        uint declaredLength,
        int fileLength,
        int tableCount,
        uint metadataOffset,
        uint metadataLength,
        uint metadataOriginalLength,
        uint privateOffset,
        uint privateLength,
        FontLimits limits)
    {
        if (declaredLength != (uint)fileLength)
        {
            throw FontFormatException.Create("The WOFF 2 declared file length is invalid.");
        }

        if (tableCount == 0)
        {
            throw FontFormatException.Create("A WOFF 2 file must contain at least one table.");
        }

        long maximumTableCount = limits.GetMaxTablesPerFace();

        if (flavor == CollectionFlavor)
        {
            maximumTableCount *= limits.GetMaxFaces();
        }

        if (tableCount > maximumTableCount)
        {
            throw FontLimitException.Create("The WOFF 2 table count exceeds the configured limit.");
        }

        bool metadataAbsent = metadataOffset == 0 &&
            metadataLength == 0 &&
            metadataOriginalLength == 0;
        bool metadataPresent = metadataOffset != 0 &&
            metadataLength != 0 &&
            metadataOriginalLength != 0;

        if (!metadataAbsent && !metadataPresent)
        {
            throw FontFormatException.Create("The WOFF 2 metadata fields are inconsistent.");
        }

        bool privateAbsent = privateOffset == 0 && privateLength == 0;
        bool privatePresent = privateOffset != 0 && privateLength != 0;

        if (!privateAbsent && !privatePresent)
        {
            throw FontFormatException.Create("The WOFF 2 private-data fields are inconsistent.");
        }
    }

    static bool IsTransformed(int tag, int transformVersion)
    {
        if (tag == FontTags.Glyf || tag == FontTags.Loca)
        {
            if (transformVersion == 0)
            {
                return true;
            }

            if (transformVersion == 3)
            {
                return false;
            }

            throw FontFormatException.Create(
                "A WOFF 2 glyf or loca table uses an unknown transform version.");
        }

        if (tag == FontTags.Hmtx)
        {
            if (transformVersion == 0)
            {
                return false;
            }

            if (transformVersion == 1)
            {
                return true;
            }

            throw FontFormatException.Create(
                "A WOFF 2 hmtx table uses an unknown transform version.");
        }

        if (transformVersion != 0)
        {
            throw FontFormatException.Create(
                "A WOFF 2 table uses an unknown transform version.");
        }

        return false;
    }

    static Woff2Collection ParseCollection(
        BigEndianReader reader,
        Woff2DirectoryEntry[] entries,
        FontLimits limits,
        System.Threading.CancellationToken cancellationToken)
    {
        uint version = reader.ReadUInt32();

        if (version != 0x00010000U && version != 0x00020000U)
        {
            throw FontFormatException.Create("The WOFF 2 collection version is not supported.");
        }

        int faceCount = Woff2Integer.Read255UInt16(reader);

        if (faceCount == 0)
        {
            throw FontFormatException.Create("A WOFF 2 collection must contain at least one font.");
        }

        if (faceCount > limits.GetMaxFaces())
        {
            throw FontLimitException.Create("The WOFF 2 collection face count exceeds the configured limit.");
        }

        Woff2CollectionFace[] faces = new Woff2CollectionFace[faceCount];
        bool[] referencedEntries = new bool[entries.Length];
        int faceIndex = 0;

        while (faceIndex < faces.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int faceTableCount = Woff2Integer.Read255UInt16(reader);

            if (faceTableCount == 0)
            {
                throw FontFormatException.Create(
                    "A WOFF 2 collection font must contain at least one table.");
            }

            if (faceTableCount > limits.GetMaxTablesPerFace())
            {
                throw FontLimitException.Create(
                    "A WOFF 2 collection font table count exceeds the configured limit.");
            }

            uint faceFlavor = reader.ReadUInt32();
            int[] tableIndices = new int[faceTableCount];
            System.Collections.Generic.HashSet<int> faceEntryIndices =
                new System.Collections.Generic.HashSet<int>();
            System.Collections.Generic.HashSet<int> faceTags =
                new System.Collections.Generic.HashSet<int>();
            int tableIndex = 0;

            while (tableIndex < tableIndices.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int entryIndex = Woff2Integer.Read255UInt16(reader);

                if (entryIndex >= entries.Length)
                {
                    throw FontFormatException.Create(
                        "A WOFF 2 collection table index is outside the table directory.");
                }

                if (!faceEntryIndices.Add(entryIndex) ||
                    !faceTags.Add(entries[entryIndex].GetTag()))
                {
                    throw FontFormatException.Create(
                        "A WOFF 2 collection font contains a duplicate table.");
                }

                tableIndices[tableIndex] = entryIndex;
                referencedEntries[entryIndex] = true;
                tableIndex += 1;
            }

            ValidateCollectionFacePairs(entries, tableIndices, faceEntryIndices);
            faces[faceIndex] = new Woff2CollectionFace(
                faceIndex,
                faceFlavor,
                tableIndices);
            faceIndex += 1;
        }

        int referenceIndex = 0;

        while (referenceIndex < referencedEntries.Length)
        {
            if (!referencedEntries[referenceIndex])
            {
                throw FontFormatException.Create(
                    "The WOFF 2 table directory contains an unreferenced collection table.");
            }

            referenceIndex += 1;
        }

        return new Woff2Collection(true, version, faces);
    }

    static void ValidateScalarTableDirectory(Woff2DirectoryEntry[] entries)
    {
        System.Collections.Generic.HashSet<int> tags =
            new System.Collections.Generic.HashSet<int>();
        int index = 0;

        while (index < entries.Length)
        {
            if (!tags.Add(entries[index].GetTag()))
            {
                throw FontFormatException.Create(
                    "A scalar WOFF 2 table directory contains a duplicate tag.");
            }

            index += 1;
        }

        int glyfIndex = FindTag(entries, FontTags.Glyf);
        int locaIndex = FindTag(entries, FontTags.Loca);

        if ((glyfIndex < 0) != (locaIndex < 0))
        {
            throw FontFormatException.Create(
                "WOFF 2 glyf and loca tables must be present as a pair.");
        }

        if (glyfIndex >= 0)
        {
            if (locaIndex < glyfIndex)
            {
                throw FontFormatException.Create(
                    "The WOFF 2 loca table must follow the glyf table.");
            }

            if (entries[glyfIndex].IsTransformed() != entries[locaIndex].IsTransformed())
            {
                throw FontFormatException.Create(
                    "WOFF 2 glyf and loca tables must use matching transform forms.");
            }
        }
    }

    static void ValidateCollectionTablePairs(Woff2DirectoryEntry[] entries)
    {
        int index = 0;

        while (index < entries.Length)
        {
            int tag = entries[index].GetTag();

            if (tag == FontTags.Glyf)
            {
                if (index + 1 >= entries.Length ||
                    entries[index + 1].GetTag() != FontTags.Loca ||
                    entries[index].IsTransformed() != entries[index + 1].IsTransformed())
                {
                    throw FontFormatException.Create(
                        "WOFF 2 collection glyf and loca tables must form adjacent matching pairs.");
                }
            }

            if (tag == FontTags.Loca &&
                (index == 0 || entries[index - 1].GetTag() != FontTags.Glyf))
            {
                throw FontFormatException.Create(
                    "A WOFF 2 collection loca table does not follow its glyf table.");
            }

            index += 1;
        }
    }

    static void ValidateCollectionFacePairs(
        Woff2DirectoryEntry[] entries,
        int[] tableIndices,
        System.Collections.Generic.HashSet<int> faceEntryIndices)
    {
        int index = 0;

        while (index < tableIndices.Length)
        {
            int entryIndex = tableIndices[index];
            int tag = entries[entryIndex].GetTag();

            if (tag == FontTags.Glyf)
            {
                if (!faceEntryIndices.Contains(entryIndex + 1))
                {
                    throw FontFormatException.Create(
                        "A WOFF 2 collection font references glyf without its paired loca table.");
                }
            }

            if (tag == FontTags.Loca)
            {
                if (entryIndex == 0 || !faceEntryIndices.Contains(entryIndex - 1))
                {
                    throw FontFormatException.Create(
                        "A WOFF 2 collection font references loca without its paired glyf table.");
                }
            }

            index += 1;
        }
    }

    static int FindTag(Woff2DirectoryEntry[] entries, int tag)
    {
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetTag() == tag)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static void ValidateRange(long offset, long length, int fileLength, string description)
    {
        if (offset > fileLength || length > fileLength - offset)
        {
            throw FontFormatException.Create(description + " extends outside the WOFF 2 file.");
        }
    }

    static int ConvertFileLength(uint value, string message)
    {
        if (value > int.MaxValue)
        {
            throw FontLimitException.Create(message);
        }

        return (int)value;
    }

    static int ConvertDecompressedLength(uint value, FontLimits limits, string message)
    {
        if (value > int.MaxValue || value > limits.GetMaxDecompressedBytes())
        {
            throw FontLimitException.Create(message);
        }

        return (int)value;
    }

    static long AlignFour(long value)
    {
        if (value > long.MaxValue - 3)
        {
            throw FontFormatException.Create("WOFF 2 alignment arithmetic overflowed.");
        }

        return (value + 3) & -4L;
    }

    static void ValidateZeroPadding(FontData fontData, long start, long end)
    {
        ReadOnlySpan<byte> bytes = fontData.GetMemory().Span;
        long index = start;

        while (index < end)
        {
            if (index >= bytes.Length || bytes[(int)index] != 0)
            {
                throw FontFormatException.Create(
                    "WOFF 2 block alignment padding must contain only zero bytes.");
            }

            index += 1;
        }
    }

    static Woff2Table[] CreateTables(
        Woff2DirectoryEntry[] entries,
        FontData decompressedData)
    {
        Woff2Table[] tables = new Woff2Table[entries.Length];
        int index = 0;

        while (index < entries.Length)
        {
            Woff2DirectoryEntry entry = entries[index];
            tables[index] = new Woff2Table(
                entry.GetDirectoryIndex(),
                entry.GetFlags(),
                entry.GetTagIndex(),
                entry.GetTag(),
                entry.GetTransformVersion(),
                entry.UsesCustomTag(),
                entry.IsTransformed(),
                entry.GetOriginalLength(),
                entry.GetTransformLength(),
                entry.GetDataOffset(),
                decompressedData.GetSlice(entry.GetDataOffset(), entry.GetDataLength()));
            index += 1;
        }

        return tables;
    }
}

internal class Woff2DirectoryEntry
{
    int directoryIndex;
    int flags;
    int tagIndex;
    int tag;
    int transformVersion;
    bool customTag;
    bool transformed;
    long originalLength;
    long transformLength;
    int dataOffset;
    int dataLength;

    internal Woff2DirectoryEntry(
        int directoryIndex,
        int flags,
        int tagIndex,
        int tag,
        int transformVersion,
        bool customTag,
        bool transformed,
        long originalLength,
        long transformLength,
        int dataOffset,
        int dataLength)
    {
        this.directoryIndex = directoryIndex;
        this.flags = flags;
        this.tagIndex = tagIndex;
        this.tag = tag;
        this.transformVersion = transformVersion;
        this.customTag = customTag;
        this.transformed = transformed;
        this.originalLength = originalLength;
        this.transformLength = transformLength;
        this.dataOffset = dataOffset;
        this.dataLength = dataLength;
    }

    internal int GetDirectoryIndex()
    {
        return directoryIndex;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal int GetTagIndex()
    {
        return tagIndex;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal int GetTransformVersion()
    {
        return transformVersion;
    }

    internal bool UsesCustomTag()
    {
        return customTag;
    }

    internal bool IsTransformed()
    {
        return transformed;
    }

    internal long GetOriginalLength()
    {
        return originalLength;
    }

    internal long GetTransformLength()
    {
        return transformLength;
    }

    internal int GetDataOffset()
    {
        return dataOffset;
    }

    internal int GetDataLength()
    {
        return dataLength;
    }
}
