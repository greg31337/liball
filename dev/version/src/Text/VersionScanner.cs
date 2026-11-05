namespace version;

internal class VersionScanner
{
    internal string source;
    internal int index;
    internal int lineNumber;
    internal int columnNumber;
    internal int previousWasCarriageReturn;

    internal VersionScanner(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        lineNumber = 1;
        columnNumber = 1;
        previousWasCarriageReturn = 0;
    }

    internal string GetSource()
    {
        return source;
    }

    internal int GetIndex()
    {
        return index;
    }

    internal int GetLength()
    {
        return source.Length;
    }

    internal int GetLineNumber()
    {
        return lineNumber;
    }

    internal int GetColumnNumber()
    {
        return columnNumber;
    }

    internal bool IsAtEnd()
    {
        return index >= source.Length;
    }

    internal char Peek()
    {
        if (IsAtEnd())
        {
            return VersionChar.EndOfInput;
        }

        return source[index];
    }

    internal char PeekAt(int offset)
    {
        int target = index + offset;
        if (target < 0 || target >= source.Length)
        {
            return VersionChar.EndOfInput;
        }

        return source[target];
    }

    internal char Advance()
    {
        if (IsAtEnd())
        {
            return VersionChar.EndOfInput;
        }

        char current = source[index];
        index = index + 1;

        if (current == '\r')
        {
            lineNumber = lineNumber + 1;
            columnNumber = 1;
            previousWasCarriageReturn = 1;
            return current;
        }

        if (current == '\n')
        {
            if (previousWasCarriageReturn == 0)
            {
                lineNumber = lineNumber + 1;
            }

            columnNumber = 1;
            previousWasCarriageReturn = 0;
            return current;
        }

        previousWasCarriageReturn = 0;
        columnNumber = columnNumber + 1;
        return current;
    }

    internal void AdvanceBy(int count)
    {
        int moved = 0;
        while (moved < count && !IsAtEnd())
        {
            Advance();
            moved = moved + 1;
        }
    }

    internal void SkipWhitespace()
    {
        while (VersionChar.IsWhitespace(Peek()))
        {
            Advance();
        }
    }

    internal VersionToken CreateToken(VersionTokenKind kind, int startIndex, int startLineNumber, int startColumnNumber)
    {
        if (startIndex < 0 || startIndex > index)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        string tokenText = source.Substring(startIndex, index - startIndex);
        return new VersionToken(kind, tokenText, startIndex, index, startLineNumber, startColumnNumber);
    }

    internal VersionToken ReadToken()
    {
        int startIndex = index;
        int startLineNumber = lineNumber;
        int startColumnNumber = columnNumber;
        char current = Peek();

        if (current == VersionChar.EndOfInput)
        {
            return new VersionToken(VersionTokenKind.End, "", startIndex, index, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsDigit(current))
        {
            return ReadNumericIdentifier(startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsLetter(current))
        {
            return ReadTextOrWildcard(startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsStar(current))
        {
            Advance();
            return CreateToken(VersionTokenKind.Wildcard, startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsSeparator(current))
        {
            Advance();
            return CreateToken(VersionTokenKind.Separator, startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsComparisonOperator(current) || VersionChar.IsPipe(current))
        {
            return ReadOperator(startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsRangeDelimiter(current))
        {
            Advance();
            return CreateToken(VersionTokenKind.RangeDelimiter, startIndex, startLineNumber, startColumnNumber);
        }

        if (VersionChar.IsWhitespace(current))
        {
            return ReadWhitespace(startIndex, startLineNumber, startColumnNumber);
        }

        Advance();
        return CreateToken(VersionTokenKind.Unknown, startIndex, startLineNumber, startColumnNumber);
    }

    internal VersionToken ReadNumericIdentifier(int startIndex, int startLineNumber, int startColumnNumber)
    {
        while (VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        return CreateToken(VersionTokenKind.NumericIdentifier, startIndex, startLineNumber, startColumnNumber);
    }

    internal VersionToken ReadTextOrWildcard(int startIndex, int startLineNumber, int startColumnNumber)
    {
        while (VersionChar.IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        if (index - startIndex == 1 && VersionChar.IsX(source[startIndex]))
        {
            return CreateToken(VersionTokenKind.Wildcard, startIndex, startLineNumber, startColumnNumber);
        }

        return CreateToken(VersionTokenKind.TextIdentifier, startIndex, startLineNumber, startColumnNumber);
    }

    internal VersionToken ReadOperator(int startIndex, int startLineNumber, int startColumnNumber)
    {
        char current = Peek();
        Advance();

        if ((current == '<' || current == '>' || current == '!' || current == '=') && Peek() == '=')
        {
            Advance();
            return CreateToken(VersionTokenKind.Operator, startIndex, startLineNumber, startColumnNumber);
        }

        if (current == '|' && Peek() == '|')
        {
            Advance();
            return CreateToken(VersionTokenKind.Operator, startIndex, startLineNumber, startColumnNumber);
        }

        return CreateToken(VersionTokenKind.Operator, startIndex, startLineNumber, startColumnNumber);
    }

    internal VersionToken ReadWhitespace(int startIndex, int startLineNumber, int startColumnNumber)
    {
        while (VersionChar.IsWhitespace(Peek()))
        {
            Advance();
        }

        return CreateToken(VersionTokenKind.Whitespace, startIndex, startLineNumber, startColumnNumber);
    }
}
