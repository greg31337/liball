namespace fix;

internal class MessageEncodingResult
{
    internal bool Success;
    internal EncodeStatus Status = EncodeStatus.Invalid;
    internal byte[] Bytes = [];
    internal int Required;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
