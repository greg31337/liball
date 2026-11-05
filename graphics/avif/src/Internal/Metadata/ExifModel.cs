using System.Collections.Generic;

namespace avif;

internal class ExifParseResult
{
    internal AvifResultCode Code;
    internal ExifParsedData? Parsed;

    internal ExifParseResult(AvifResultCode code)
    {
        Code = code;
        Parsed = null;
    }
}

internal class ExifParsedData
{
    internal byte[] Data;
    internal int TiffOffset;
    internal bool LittleEndian;
    internal uint FirstIfdOffset;
    internal List<ExifIfd> Ifds;
    internal List<ExifTag> Tags;

    internal ExifParsedData()
    {
        Data = System.Array.Empty<byte>();
        Ifds = new List<ExifIfd>();
        Tags = new List<ExifTag>();
    }
}

internal class ExifIfd
{
    internal uint RelativeOffset;
    internal int AbsoluteOffset;
    internal int Depth;
    internal int TagStartIndex;
    internal int TagCount;
    internal uint NextIfdOffset;

    internal ExifIfd(
        uint relativeOffset,
        int absoluteOffset,
        int depth,
        int tagStartIndex)
    {
        RelativeOffset = relativeOffset;
        AbsoluteOffset = absoluteOffset;
        Depth = depth;
        TagStartIndex = tagStartIndex;
    }
}

internal class ExifTag
{
    internal ushort Identifier;
    internal ushort Type;
    internal uint Count;
    internal int IfdIndex;
    internal int EntryOffset;
    internal int ValueOffset;
    internal int ValueLength;
    internal bool Inline;

    internal ExifTag(
        ushort identifier,
        ushort type,
        uint count,
        int ifdIndex,
        int entryOffset,
        int valueOffset,
        int valueLength,
        bool inline)
    {
        Identifier = identifier;
        Type = type;
        Count = count;
        IfdIndex = ifdIndex;
        EntryOffset = entryOffset;
        ValueOffset = valueOffset;
        ValueLength = valueLength;
        Inline = inline;
    }
}
