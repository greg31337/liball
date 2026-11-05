namespace uri;

internal class SpanCursorResult
{
    internal bool Success;
    internal char Character;
    internal int StartOffset;
    internal int NextOffset;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal SpanCursorResult(
        bool success,
        char character,
        int startOffset,
        int nextOffset,
        int errorOffset,
        int errorLength)
    {
        Success = success;
        Character = character;
        StartOffset = startOffset;
        NextOffset = nextOffset;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
