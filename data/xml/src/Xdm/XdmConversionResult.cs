namespace xml;

internal class XdmConversionResult
{
    internal bool Success;
    internal bool TypeError;
    internal XdmAtomicValue? Value;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal XdmConversionResult(bool success, bool typeError, XdmAtomicValue? value, string? errorCode, string? errorMessage)
    {
        Success = success;
        TypeError = typeError;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
