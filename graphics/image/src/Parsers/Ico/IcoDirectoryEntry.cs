namespace image;

internal class IcoDirectoryEntry
{
    internal int Index;
    internal int Width;
    internal int Height;
    internal int ColorCount;
    internal int Reserved;
    internal ushort PlanesOrHotspotX;
    internal ushort BitCountOrHotspotY;
    internal uint DataSize;
    internal uint DataOffset;

    internal IcoDirectoryEntry()
    {
        Index = 0;
        Width = 0;
        Height = 0;
        ColorCount = 0;
        Reserved = 0;
        PlanesOrHotspotX = 0;
        BitCountOrHotspotY = 0;
        DataSize = 0;
        DataOffset = 0;
    }
}
