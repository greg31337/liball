namespace uri;

internal class Utf8ScalarResult
{
    internal bool Success;
    internal int ScalarValue;
    internal int ByteLength;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal Utf8ScalarResult(
        bool success,
        int scalarValue,
        int byteLength,
        int errorOffset,
        int errorLength)
    {
        Success = success;
        ScalarValue = scalarValue;
        ByteLength = byteLength;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
