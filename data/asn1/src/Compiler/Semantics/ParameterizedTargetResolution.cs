namespace asn1;

internal class ParameterizedTargetResolution
{
    internal SymbolTableEntry? Target;
    internal string ModuleName = "";
    internal string ReferenceName = "";
    internal int StartIndex = -1;
    internal int NameIndex = -1;
    internal int BraceIndex = -1;
}
