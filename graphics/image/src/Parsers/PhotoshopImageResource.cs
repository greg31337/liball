namespace image;

internal class PhotoshopImageResource
{
    internal string Signature;
    internal int ResourceId;
    internal string Name;
    internal byte[] Data;
    internal long ResourceOffset;
    internal long DataOffset;
    internal long ResourceLength;
    internal uint DeclaredDataLength;
    internal int NameStorageLength;
    internal bool Truncated;

    internal PhotoshopImageResource()
    {
        Signature = "";
        ResourceId = 0;
        Name = "";
        Data = new byte[0];
        ResourceOffset = -1;
        DataOffset = -1;
        ResourceLength = 0;
        DeclaredDataLength = 0;
        NameStorageLength = 0;
        Truncated = false;
    }
}
