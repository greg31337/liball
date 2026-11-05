namespace sdp;

internal class SdpNumberParseResult
{
    internal bool Success = false;
    internal bool Invalid = false;
    internal bool Overflow = false;
    internal int Int32Value = 0;
    internal uint UInt32Value = 0;
    internal long SignedValue = 0;
    internal ulong UnsignedValue = 0;
    internal string DiagnosticCode = string.Empty;
}
