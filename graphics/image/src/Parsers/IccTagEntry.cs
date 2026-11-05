namespace image;

internal class IccTagEntry
{
    internal string Signature;
    internal int Offset;
    internal int Size;
    internal string Type;

    internal IccTagEntry()
    {
        Signature = "";
        Offset = 0;
        Size = 0;
        Type = "";
    }
}
