namespace fix;

internal class GroupParseResult
{
    internal bool Success;
    internal DecodeStatus Status = DecodeStatus.Invalid;
    internal FixGroup? Group;
    internal int NextIndex;
    internal int ErrorWireIndex = -1;
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
