namespace uri;

internal class UnicodeScalarResult
{
    internal bool Success;
    internal int ScalarValue;
    internal int CodeUnitLength;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal UnicodeScalarResult(
        bool success,
        int scalarValue,
        int codeUnitLength,
        int errorOffset,
        int errorLength)
    {
        Success = success;
        ScalarValue = scalarValue;
        CodeUnitLength = codeUnitLength;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
