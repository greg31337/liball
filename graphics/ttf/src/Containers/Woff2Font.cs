namespace ttf;

internal class Woff2Font
{
    uint flavor;
    int declaredLength;
    int reserved;
    uint totalSfntSize;
    int majorVersion;
    int minorVersion;
    int compressedDataOffset;
    int compressedDataLength;
    FontData decompressedData;
    Woff2Table[] tables;
    Woff2Collection collection;
    Woff2Metadata metadata;
    Woff2PrivateData privateData;

    internal Woff2Font(
        uint flavor,
        int declaredLength,
        int reserved,
        uint totalSfntSize,
        int majorVersion,
        int minorVersion,
        int compressedDataOffset,
        int compressedDataLength,
        FontData decompressedData,
        Woff2Table[] tables,
        Woff2Collection collection,
        Woff2Metadata metadata,
        Woff2PrivateData privateData)
    {
        if (decompressedData == null)
        {
            throw new System.ArgumentNullException("decompressedData");
        }

        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (collection == null)
        {
            throw new System.ArgumentNullException("collection");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (privateData == null)
        {
            throw new System.ArgumentNullException("privateData");
        }

        this.flavor = flavor;
        this.declaredLength = declaredLength;
        this.reserved = reserved;
        this.totalSfntSize = totalSfntSize;
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.compressedDataOffset = compressedDataOffset;
        this.compressedDataLength = compressedDataLength;
        this.decompressedData = decompressedData;
        this.tables = tables;
        this.collection = collection;
        this.metadata = metadata;
        this.privateData = privateData;
    }

    internal uint GetFlavor()
    {
        return flavor;
    }

    internal int GetDeclaredLength()
    {
        return declaredLength;
    }

    internal int GetReservedValue()
    {
        return reserved;
    }

    internal uint GetTotalSfntSize()
    {
        return totalSfntSize;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetCompressedDataOffset()
    {
        return compressedDataOffset;
    }

    internal int GetCompressedDataLength()
    {
        return compressedDataLength;
    }

    internal int GetDecompressedDataLength()
    {
        return decompressedData.GetLength();
    }

    internal byte[] CopyDecompressedData()
    {
        return decompressedData.CopyBytes();
    }

    internal int GetTableCount()
    {
        return tables.Length;
    }

    internal Woff2Table GetTable(int index)
    {
        if (index < 0 || index >= tables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tables[index];
    }

    internal Woff2Table? FindTable(int tag)
    {
        int index = 0;

        while (index < tables.Length)
        {
            if (tables[index].GetTag() == tag)
            {
                return tables[index];
            }

            index += 1;
        }

        return null;
    }

    internal Woff2Collection GetCollection()
    {
        return collection;
    }

    internal Woff2Metadata GetMetadata()
    {
        return metadata;
    }

    internal Woff2PrivateData GetPrivateData()
    {
        return privateData;
    }
}

internal class Woff2Table
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
    FontDataSlice data;

    internal Woff2Table(
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
        FontDataSlice data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

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
        this.data = data;
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

    internal bool HasTransformLength()
    {
        return transformed;
    }

    internal long GetTransformLength()
    {
        if (!transformed)
        {
            throw new System.InvalidOperationException("The WOFF 2 table uses a null transform.");
        }

        return transformLength;
    }

    internal int GetDataOffset()
    {
        return dataOffset;
    }

    internal int GetDataLength()
    {
        return data.GetLength();
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }

    internal FontDataSlice GetDataSlice()
    {
        return data;
    }
}

internal class Woff2Collection
{
    bool present;
    uint version;
    Woff2CollectionFace[] faces;

    internal Woff2Collection(bool present, uint version, Woff2CollectionFace[] faces)
    {
        if (faces == null)
        {
            throw new System.ArgumentNullException("faces");
        }

        this.present = present;
        this.version = version;
        this.faces = faces;
    }

    internal bool IsPresent()
    {
        return present;
    }

    internal uint GetVersion()
    {
        if (!present)
        {
            throw new System.InvalidOperationException("The WOFF 2 file is not a collection.");
        }

        return version;
    }

    internal int GetFaceCount()
    {
        return faces.Length;
    }

    internal Woff2CollectionFace GetFace(int index)
    {
        if (index < 0 || index >= faces.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return faces[index];
    }
}

internal class Woff2CollectionFace
{
    int faceIndex;
    uint flavor;
    int[] tableIndices;

    internal Woff2CollectionFace(int faceIndex, uint flavor, int[] tableIndices)
    {
        if (faceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        if (tableIndices == null)
        {
            throw new System.ArgumentNullException("tableIndices");
        }

        this.faceIndex = faceIndex;
        this.flavor = flavor;
        this.tableIndices = tableIndices;
    }

    internal int GetFaceIndex()
    {
        return faceIndex;
    }

    internal uint GetFlavor()
    {
        return flavor;
    }

    internal int GetTableCount()
    {
        return tableIndices.Length;
    }

    internal int GetTableIndex(int index)
    {
        if (index < 0 || index >= tableIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tableIndices[index];
    }
}

internal class Woff2Metadata
{
    bool present;
    long compressedOffset;
    int compressedLength;
    int originalLength;
    byte[] data;

    internal Woff2Metadata(
        bool present,
        long compressedOffset,
        int compressedLength,
        int originalLength,
        byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.present = present;
        this.compressedOffset = compressedOffset;
        this.compressedLength = compressedLength;
        this.originalLength = originalLength;
        this.data = data;
    }

    internal bool IsPresent()
    {
        return present;
    }

    internal long GetCompressedOffset()
    {
        return compressedOffset;
    }

    internal int GetCompressedLength()
    {
        return compressedLength;
    }

    internal int GetOriginalLength()
    {
        return originalLength;
    }

    internal byte[] CopyData()
    {
        if (!present)
        {
            throw new System.InvalidOperationException("The WOFF 2 file does not contain metadata.");
        }

        return data.AsSpan().ToArray();
    }
}

internal class Woff2PrivateData
{
    bool present;
    long offset;
    byte[] data;

    internal Woff2PrivateData(bool present, long offset, byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.present = present;
        this.offset = offset;
        this.data = data;
    }

    internal bool IsPresent()
    {
        return present;
    }

    internal long GetOffset()
    {
        return offset;
    }

    internal int GetLength()
    {
        return data.Length;
    }

    internal byte[] CopyData()
    {
        if (!present)
        {
            throw new System.InvalidOperationException("The WOFF 2 file does not contain private data.");
        }

        return data.AsSpan().ToArray();
    }
}
