namespace sip;

internal class SipDecodedUriComponent
{
    internal bool Success;
    internal string? Value;
    internal string? ComparisonValue;
    internal int ErrorOffset;

    internal SipDecodedUriComponent(bool success, string? value, string? comparisonValue, int errorOffset)
    {
        Success = success;
        Value = value;
        ComparisonValue = comparisonValue;
        ErrorOffset = errorOffset;
    }
}
