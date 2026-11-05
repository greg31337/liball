namespace fix;

internal class ChecksumFieldResult
{
    internal bool Success;
    internal int Start;
    internal int DeclaredChecksum;
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
}
