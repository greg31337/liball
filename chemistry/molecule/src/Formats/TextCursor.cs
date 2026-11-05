namespace molecule;

internal class TextCursor
{
    internal string Text;
    internal string SourceName;
    internal int Index;
    internal int LineNumber;
    internal int ColumnNumber;

    internal static TextCursor CreateEmpty()
    {
        return new TextCursor("", "");
    }

    internal static TextCursor Create(string text)
    {
        return new TextCursor(text, "");
    }

    internal TextCursor(string text, string sourceName)
    {
        Text = NormalizeString(text);
        SourceName = NormalizeString(sourceName);
        Index = 0;
        LineNumber = 1;
        ColumnNumber = 1;
    }

    internal bool IsEnd()
    {
        return Index >= Text.Length;
    }

    internal bool HasMore()
    {
        return Index < Text.Length;
    }

    internal char CurrentChar()
    {
        if (IsEnd())
        {
            return '\0';
        }

        return Text[Index];
    }

    internal char PeekChar(int offset)
    {
        int peekIndex = Index + offset;
        if (peekIndex < 0 || peekIndex >= Text.Length)
        {
            return '\0';
        }

        return Text[peekIndex];
    }

    internal void Advance()
    {
        if (IsEnd())
        {
            return;
        }

        char current = Text[Index];
        if (current == '\r')
        {
            Index = Index + 1;
            if (!IsEnd() && Text[Index] == '\n')
            {
                Index = Index + 1;
            }

            LineNumber = LineNumber + 1;
            ColumnNumber = 1;
            return;
        }

        if (current == '\n')
        {
            Index = Index + 1;
            LineNumber = LineNumber + 1;
            ColumnNumber = 1;
            return;
        }

        Index = Index + 1;
        ColumnNumber = ColumnNumber + 1;
    }

    internal void AdvanceCount(int count)
    {
        int index = 0;
        while (index < count)
        {
            Advance();
            index = index + 1;
        }
    }

    internal bool ConsumeIf(char value)
    {
        if (CurrentChar() == value)
        {
            Advance();
            return true;
        }

        return false;
    }

    internal bool Match(string text)
    {
        string expected = NormalizeString(text);
        int index = 0;
        while (index < expected.Length)
        {
            if (PeekChar(index) != expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string ReadToEnd()
    {
        int start = Index;
        while (!IsEnd())
        {
            Advance();
        }

        return Text.Substring(start, Index - start);
    }

    internal DiagnosticLocation GetLocation()
    {
        return new DiagnosticLocation(SourceName, LineNumber, ColumnNumber, Index, "");
    }

    internal static string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
