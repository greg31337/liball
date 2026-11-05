namespace raw;

internal enum TiffIfdLinkKind
{
    Next = 0,
    SubIfd = 1,
    Exif = 2,
    Gps = 3,
    Interop = 4,
    Unknown = 5,
    None = 6
}

internal class TiffIfd
{
    internal string Path;
    internal ulong Offset;
    internal long AbsoluteOffset;
    internal int Depth;
    internal TiffEntry[] Entries;
    internal ulong NextIfdOffset;

    internal TiffIfd(string path, ulong offset, long absoluteOffset, int depth, TiffEntry[] entries, ulong nextIfdOffset)
    {
        Path = path;
        Offset = offset;
        AbsoluteOffset = absoluteOffset;
        Depth = depth;
        Entries = entries;
        NextIfdOffset = nextIfdOffset;
    }

    internal TiffEntry? FindEntry(ushort tag)
    {
        for (int index = 0; index < Entries.Length; index++)
        {
            if (Entries[index].Tag == tag)
            {
                return Entries[index];
            }
        }
        return null;
    }
}

internal class TiffIfdLink
{
    internal string SourcePath;
    internal string Path;
    internal string TargetPath;
    internal ulong TargetOffset;
    internal TiffIfdLinkKind Kind;
    internal ushort Tag;
    internal int Index;
    internal bool Alias;

    internal TiffIfdLink(string sourcePath, string path, string targetPath, ulong targetOffset, TiffIfdLinkKind kind, ushort tag, int index, bool alias)
    {
        SourcePath = sourcePath;
        Path = path;
        TargetPath = targetPath;
        TargetOffset = targetOffset;
        Kind = kind;
        Tag = tag;
        Index = index;
        Alias = alias;
    }
}

internal class TiffIfdGraph
{
    internal TiffHeader Header;
    internal TiffIfd[] Ifds;
    internal TiffIfdLink[] Links;
    internal TiffImageDescriptor[] Images;
    internal long ContainerStart;
    internal long ContainerLength;

    internal TiffIfdGraph(TiffHeader header, TiffIfd[] ifds, TiffIfdLink[] links)
    {
        Header = header;
        Ifds = ifds;
        Links = links;
        Images = new TiffImageDescriptor[0];
        ContainerStart = header.BaseOffset;
        ContainerLength = 0;
    }

    internal TiffIfd? FindIfd(string? path)
    {
        for (int index = 0; index < Ifds.Length; index++)
        {
            if (Ifds[index].Path == path)
            {
                return Ifds[index];
            }
        }
        return null;
    }
}
