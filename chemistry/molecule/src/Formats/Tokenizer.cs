namespace molecule;

internal enum TextTokenKind
{
    End = 0,
    Whitespace = 1,
    Word = 2,
    Number = 3,
    Symbol = 4
}

internal class TextToken
{
    internal TextTokenKind Kind;
    internal string Text;
    internal int StartIndex;
    internal int EndIndex;
    internal int LineNumber;
    internal int ColumnNumber;
    internal DiagnosticLocation Location;

    internal TextToken(TextTokenKind kind, string text, int startIndex, int endIndex, int lineNumber, int columnNumber, string sourceName)
    {
        Kind = kind;
        Text = TextCursor.NormalizeString(text);
        StartIndex = startIndex;
        EndIndex = endIndex;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        Location = new DiagnosticLocation(sourceName, lineNumber, columnNumber, startIndex, "");
    }
}

internal class Tokenizer
{
    internal TextCursor Cursor;
    internal bool SkipWhitespace;

    internal static Tokenizer Create(string text)
    {
        return new Tokenizer(text, "");
    }

    internal Tokenizer(string text, string sourceName)
    {
        Cursor = new TextCursor(text, sourceName);
        SkipWhitespace = true;
    }

    internal TextToken NextToken()
    {
        if (SkipWhitespace)
        {
            SkipWhitespaceCharacters();
        }

        if (Cursor.IsEnd())
        {
            return CreateToken(TextTokenKind.End, "", Cursor.Index, Cursor.Index, Cursor.LineNumber, Cursor.ColumnNumber);
        }

        char current = Cursor.CurrentChar();
        if (char.IsWhiteSpace(current))
        {
            return ReadWhitespace();
        }

        if (IsNumberStart())
        {
            return ReadNumber();
        }

        if (IsWordStart(current))
        {
            return ReadWord();
        }

        return ReadSymbol();
    }

    internal void SkipWhitespaceCharacters()
    {
        while (!Cursor.IsEnd() && char.IsWhiteSpace(Cursor.CurrentChar()))
        {
            Cursor.Advance();
        }
    }

    internal TextToken ReadWhitespace()
    {
        int startIndex = Cursor.Index;
        int lineNumber = Cursor.LineNumber;
        int columnNumber = Cursor.ColumnNumber;

        while (!Cursor.IsEnd() && char.IsWhiteSpace(Cursor.CurrentChar()))
        {
            Cursor.Advance();
        }

        string text = Cursor.Text.Substring(startIndex, Cursor.Index - startIndex);
        return CreateToken(TextTokenKind.Whitespace, text, startIndex, Cursor.Index, lineNumber, columnNumber);
    }

    internal TextToken ReadWord()
    {
        int startIndex = Cursor.Index;
        int lineNumber = Cursor.LineNumber;
        int columnNumber = Cursor.ColumnNumber;

        while (!Cursor.IsEnd() && IsWordPart(Cursor.CurrentChar()))
        {
            Cursor.Advance();
        }

        string text = Cursor.Text.Substring(startIndex, Cursor.Index - startIndex);
        return CreateToken(TextTokenKind.Word, text, startIndex, Cursor.Index, lineNumber, columnNumber);
    }

    internal TextToken ReadNumber()
    {
        int startIndex = Cursor.Index;
        int lineNumber = Cursor.LineNumber;
        int columnNumber = Cursor.ColumnNumber;

        if (Cursor.CurrentChar() == '+' || Cursor.CurrentChar() == '-')
        {
            Cursor.Advance();
        }

        while (!Cursor.IsEnd() && char.IsDigit(Cursor.CurrentChar()))
        {
            Cursor.Advance();
        }

        if (Cursor.CurrentChar() == '.')
        {
            Cursor.Advance();
            while (!Cursor.IsEnd() && char.IsDigit(Cursor.CurrentChar()))
            {
                Cursor.Advance();
            }
        }

        if (Cursor.CurrentChar() == 'e' || Cursor.CurrentChar() == 'E')
        {
            Cursor.Advance();
            if (Cursor.CurrentChar() == '+' || Cursor.CurrentChar() == '-')
            {
                Cursor.Advance();
            }

            while (!Cursor.IsEnd() && char.IsDigit(Cursor.CurrentChar()))
            {
                Cursor.Advance();
            }
        }

        string text = Cursor.Text.Substring(startIndex, Cursor.Index - startIndex);
        return CreateToken(TextTokenKind.Number, text, startIndex, Cursor.Index, lineNumber, columnNumber);
    }

    internal TextToken ReadSymbol()
    {
        int startIndex = Cursor.Index;
        int lineNumber = Cursor.LineNumber;
        int columnNumber = Cursor.ColumnNumber;
        Cursor.Advance();
        string text = Cursor.Text.Substring(startIndex, Cursor.Index - startIndex);
        return CreateToken(TextTokenKind.Symbol, text, startIndex, Cursor.Index, lineNumber, columnNumber);
    }

    internal bool IsNumberStart()
    {
        char current = Cursor.CurrentChar();
        char next = Cursor.PeekChar(1);

        if (char.IsDigit(current))
        {
            return true;
        }

        if (current == '.' && char.IsDigit(next))
        {
            return true;
        }

        if ((current == '+' || current == '-') && char.IsDigit(next))
        {
            return true;
        }

        if ((current == '+' || current == '-') && next == '.' && char.IsDigit(Cursor.PeekChar(2)))
        {
            return true;
        }

        return false;
    }

    internal bool IsWordStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    internal bool IsWordPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    internal TextToken CreateToken(TextTokenKind kind, string text, int startIndex, int endIndex, int lineNumber, int columnNumber)
    {
        return new TextToken(kind, text, startIndex, endIndex, lineNumber, columnNumber, Cursor.SourceName);
    }
}
