namespace ttf;

internal class Woff1Font
{
    uint flavor;
    int majorVersion;
    int minorVersion;
    Woff1Table[] tables;
    Woff1Metadata metadata;
    Woff1PrivateData privateData;
    FontData reconstructedSfnt;
    FontDiagnostic[] diagnostics;

    internal Woff1Font(
        uint flavor,
        int majorVersion,
        int minorVersion,
        Woff1Table[] tables,
        Woff1Metadata metadata,
        Woff1PrivateData privateData,
        FontData reconstructedSfnt,
        FontDiagnostic[] diagnostics)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (privateData == null)
        {
            throw new System.ArgumentNullException("privateData");
        }

        if (reconstructedSfnt == null)
        {
            throw new System.ArgumentNullException("reconstructedSfnt");
        }

        if (diagnostics == null)
        {
            throw new System.ArgumentNullException("diagnostics");
        }

        this.flavor = flavor;
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.tables = tables;
        this.metadata = metadata;
        this.privateData = privateData;
        this.reconstructedSfnt = reconstructedSfnt;
        this.diagnostics = diagnostics;
    }

    internal uint GetFlavor()
    {
        return flavor;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetTableCount()
    {
        return tables.Length;
    }

    internal Woff1Table GetTable(int index)
    {
        if (index < 0 || index >= tables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tables[index];
    }

    internal Woff1Table? FindTable(int tag)
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

    internal Woff1Metadata GetMetadata()
    {
        return metadata;
    }

    internal Woff1PrivateData GetPrivateData()
    {
        return privateData;
    }

    internal int GetReconstructedSfntLength()
    {
        return reconstructedSfnt.GetLength();
    }

    internal byte[] CopyReconstructedSfnt()
    {
        return reconstructedSfnt.CopyBytes();
    }

    internal FontData GetReconstructedSfntData()
    {
        return reconstructedSfnt;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }
}

internal class Woff1Table
{
    int tag;
    long woffOffset;
    int compressedLength;
    int originalLength;
    uint originalChecksum;
    long sfntOffset;
    FontDataSlice data;

    internal Woff1Table(
        int tag,
        long woffOffset,
        int compressedLength,
        int originalLength,
        uint originalChecksum,
        long sfntOffset,
        FontDataSlice data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.tag = tag;
        this.woffOffset = woffOffset;
        this.compressedLength = compressedLength;
        this.originalLength = originalLength;
        this.originalChecksum = originalChecksum;
        this.sfntOffset = sfntOffset;
        this.data = data;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal long GetWoffOffset()
    {
        return woffOffset;
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

    internal long GetSfntOffset()
    {
        return sfntOffset;
    }

    internal bool IsCompressed()
    {
        return compressedLength < originalLength;
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }
}

internal class Woff1Metadata
{
    bool present;
    long compressedOffset;
    int compressedLength;
    int originalLength;
    byte[] data;

    internal Woff1Metadata(
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
            throw new System.InvalidOperationException("The WOFF file does not contain metadata.");
        }

        return data.AsSpan().ToArray();
    }
}

internal class Woff1PrivateData
{
    bool present;
    long offset;
    byte[] data;

    internal Woff1PrivateData(bool present, long offset, byte[] data)
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
            throw new System.InvalidOperationException("The WOFF file does not contain private data.");
        }

        return data.AsSpan().ToArray();
    }
}
