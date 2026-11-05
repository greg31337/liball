namespace fix;

internal class GroupStructureResult
{
    internal bool Success;
    internal DecodeStatus Status = DecodeStatus.Invalid;
    internal int ErrorWireIndex = -1;
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
