namespace uri;

internal class TextOutputResult
{
    internal bool Success;
    internal int WrittenOffset;
    internal int WrittenLength;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal TextOutputResult(
        bool success,
        int writtenOffset,
        int writtenLength,
        int errorOffset,
        int errorLength)
    {
        Success = success;
        WrittenOffset = writtenOffset;
        WrittenLength = writtenLength;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
