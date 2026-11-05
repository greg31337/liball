namespace xml;

internal class XdmNumericParseResult
{
    internal bool Success;
    internal XdmAtomicValue? Value;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal XdmNumericParseResult(bool success, XdmAtomicValue? value, string? errorCode, string? errorMessage)
    {
        Success = success;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
