namespace cbor;

internal class CddlPrintfPart
{
    internal string Literal = "";
    internal char Specifier;
    internal bool LeftAligned;
    internal bool ShowSign;
    internal bool SpaceSign;
    internal bool Alternate;
    internal bool ZeroPadded;
    internal int Width = -1;
    internal int Precision = -1;
    internal int ArgumentIndex = -1;
}
