namespace uri;

internal class Utf8DecodeResult
{
    internal bool Success;
    internal string Text;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal Utf8DecodeResult(bool success, string text, int errorOffset, int errorLength)
    {
        Success = success;
        Text = text;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
