namespace fix;

internal class DataFieldScanResult
{
    internal bool Success;
    internal int LengthTag;
    internal int DataTag;
    internal int DeclaredLength;
    internal int ValueStart;
    internal byte[] Value = [];
    internal int Consumed;
    internal int Required;
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
}
