namespace uri;

internal class Utf8EncodeResult
{
    internal bool Success;
    internal byte[] Bytes;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal Utf8EncodeResult(bool success, byte[] bytes, int errorOffset, int errorLength)
    {
        Success = success;
        Bytes = bytes;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
