namespace raw;

internal enum TiffLayoutReferenceKind
{
    Ifd = 0,
    Block = 1
}

internal enum TiffLayoutBlockKind
{
    Strip = 0,
    Tile = 1,
    Opaque = 2
}

internal class TiffLayoutReference
{
    internal int Index;
    internal TiffLayoutReferenceKind Kind;
    internal string Target;

    internal TiffLayoutReference(int index, TiffLayoutReferenceKind kind, string target)
    {
        Index = index;
        Kind = kind;
        Target = target;
    }
}

internal class TiffLayoutEntryInput
{
    internal ushort Tag;
    internal TiffFieldType FieldType;
    internal ulong Count;
    internal byte[] ValueBytes;
    internal TiffLayoutReference[]? References;

    internal TiffLayoutEntryInput(ushort tag, TiffFieldType fieldType, ulong count, byte[] valueBytes, TiffLayoutReference[]? references)
    {
        Tag = tag;
        FieldType = fieldType;
        Count = count;
        ValueBytes = valueBytes;
        References = references;
    }
}

internal class TiffLayoutIfdInput
{
    internal string Path;
    internal TiffLayoutEntryInput[] Entries;
    internal string? NextIfdPath;

    internal TiffLayoutIfdInput(string path, TiffLayoutEntryInput[] entries, string? nextIfdPath)
    {
        Path = path;
        Entries = entries;
        NextIfdPath = nextIfdPath;
    }
}

internal class TiffLayoutBlockInput
{
    internal string Name;
    internal TiffLayoutBlockKind Kind;
    internal long Length;
    internal byte[]? Bytes;

    internal TiffLayoutBlockInput(string name, TiffLayoutBlockKind kind, long length, byte[]? bytes)
    {
        Name = name;
        Kind = kind;
        Length = length;
        Bytes = bytes;
    }
}

internal class TiffLayoutRequest
{
    internal bool BigTiff;
    internal RawByteOrder ByteOrder;
    internal byte[] HeaderExtraBytes;
    internal string FirstIfdPath;
    internal TiffLayoutIfdInput[] Ifds;
    internal TiffLayoutBlockInput[] Blocks;

    internal TiffLayoutRequest(bool bigTiff, RawByteOrder byteOrder, string firstIfdPath, TiffLayoutIfdInput[] ifds, TiffLayoutBlockInput[] blocks)
    {
        BigTiff = bigTiff;
        ByteOrder = byteOrder;
        HeaderExtraBytes = System.Array.Empty<byte>();
        FirstIfdPath = firstIfdPath;
        Ifds = ifds;
        Blocks = blocks;
    }

    internal static TiffLayoutRequest CreateWithHeaderExtraBytes(bool bigTiff, RawByteOrder byteOrder, byte[] headerExtraBytes, string firstIfdPath, TiffLayoutIfdInput[] ifds, TiffLayoutBlockInput[] blocks)
    {
        TiffLayoutRequest request = new TiffLayoutRequest(bigTiff, byteOrder, firstIfdPath, ifds, blocks);
        request.HeaderExtraBytes = headerExtraBytes;
        return request;
    }
}

internal class TiffLayoutEntryPlan
{
    internal ushort Tag;
    internal TiffFieldType FieldType;
    internal ulong Count;
    internal long EntryOffset;
    internal bool Inline;
    internal ulong ValueOffset;
    internal long ValueLength;
    internal byte[]? ValueBytes;
    internal TiffLayoutReference[]? References;
}

internal class TiffLayoutIfdPlan
{
    internal string? Path;
    internal ulong Offset;
    internal long Length;
    internal ulong NextIfdOffset;
    internal string? NextIfdPath;
    internal TiffLayoutEntryPlan[]? Entries;
}

internal class TiffLayoutBlockPlan
{
    internal string? Name;
    internal TiffLayoutBlockKind Kind;
    internal ulong Offset;
    internal long Length;
    internal byte[]? Bytes;
}

internal class TiffLayoutPlan
{
    internal bool BigTiff;
    internal RawByteOrder ByteOrder;
    internal int Alignment;
    internal int HeaderLength;
    internal byte[]? HeaderExtraBytes;
    internal ulong FirstIfdOffset;
    internal long PredictedSize;
    internal TiffLayoutIfdPlan[]? Ifds;
    internal TiffLayoutBlockPlan[]? Blocks;
}
