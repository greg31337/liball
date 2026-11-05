namespace xml;

internal class XdmStringParseResult
{
    internal bool Success;
    internal XdmAtomicValue? Value;
    internal string? ErrorMessage;

    internal XdmStringParseResult(bool success, XdmAtomicValue? value, string? errorMessage)
    {
        Success = success;
        Value = value;
        ErrorMessage = errorMessage;
    }
}
