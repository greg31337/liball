using System;

namespace uri;

internal class SpanCursor
{
    internal int Offset;

    internal SpanCursor(int offset)
    {
        Offset = offset;
    }

    internal bool HasRemaining(ReadOnlySpan<char> text)
    {
        return Offset >= 0 && Offset < text.Length;
    }

    internal int GetRemainingLength(ReadOnlySpan<char> text)
    {
        if (Offset < 0 || Offset >= text.Length)
        {
            return 0;
        }

        return text.Length - Offset;
    }

    internal SpanCursorResult PeekCharacter(ReadOnlySpan<char> text)
    {
        if (Offset < 0)
        {
            return CreateFailure(0, 0);
        }

        if (Offset >= text.Length)
        {
            return CreateFailure(text.Length, 0);
        }

        return new SpanCursorResult(true, text[Offset], Offset, Offset, 0, 0);
    }

    internal SpanCursorResult ReadCharacter(ReadOnlySpan<char> text)
    {
        SpanCursorResult result = PeekCharacter(text);
        if (result.Success)
        {
            Offset = Offset + 1;
            result.NextOffset = Offset;
        }

        return result;
    }

    internal SpanCursorResult Advance(ReadOnlySpan<char> text, int count)
    {
        if (Offset < 0)
        {
            return CreateFailure(0, 0);
        }

        if (Offset > text.Length)
        {
            return CreateFailure(text.Length, 0);
        }

        if (count < 0)
        {
            return CreateFailure(Offset, 0);
        }

        if (count > text.Length - Offset)
        {
            return CreateFailure(text.Length, 0);
        }

        int startOffset = Offset;
        Offset = Offset + count;
        return new SpanCursorResult(true, '\0', startOffset, Offset, 0, 0);
    }

    internal SpanCursorResult ConsumeCharacter(ReadOnlySpan<char> text, char expected)
    {
        SpanCursorResult result = PeekCharacter(text);
        if (!result.Success)
        {
            return result;
        }

        if (result.Character != expected)
        {
            return CreateFailure(Offset, 1);
        }

        Offset = Offset + 1;
        result.NextOffset = Offset;
        return result;
    }

    internal SpanCursorResult ConsumeText(ReadOnlySpan<char> text, ReadOnlySpan<char> expected)
    {
        if (Offset < 0)
        {
            return CreateFailure(0, 0);
        }

        if (Offset > text.Length)
        {
            return CreateFailure(text.Length, 0);
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (index >= text.Length - Offset)
            {
                return CreateFailure(text.Length, 0);
            }

            if (text[Offset + index] != expected[index])
            {
                return CreateFailure(Offset + index, 1);
            }

            index = index + 1;
        }

        int startOffset = Offset;
        Offset = Offset + expected.Length;
        return new SpanCursorResult(true, '\0', startOffset, Offset, 0, 0);
    }

    internal SpanCursorResult CreateFailure(int errorOffset, int errorLength)
    {
        return new SpanCursorResult(false, '\0', Offset, Offset, errorOffset, errorLength);
    }
}
