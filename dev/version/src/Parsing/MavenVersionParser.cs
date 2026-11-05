namespace version;

internal class MavenVersionParser
{
    internal string source;
    internal int index;
    internal char pendingSeparator;

    internal MavenVersionParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        pendingSeparator = VersionChar.EndOfInput;
    }

    internal static MavenVersionToken[] ParseTokens(string text)
    {
        MavenVersionParser parser = new MavenVersionParser(text);
        return parser.Tokenize();
    }

    internal static VersionParseResult Parse(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return VersionParseResult.CreateFailure(
                text,
                VersionDialect.Maven,
                VersionDiagnostic.FromIndex(
                    MavenDiagnosticCodes.MissingVersion,
                    "Expected Maven version.",
                    text,
                    0));
        }

        int index = 0;
        while (index < text.Length)
        {
            if (IsIllegalVersionCharacter(text[index]))
            {
                return VersionParseResult.CreateFailure(
                    text,
                    VersionDialect.Maven,
                    VersionDiagnostic.FromIndex(
                        MavenDiagnosticCodes.InvalidVersion,
                        "Invalid character in Maven version.",
                        text,
                        index));
            }

            index = index + 1;
        }

        UniversalVersion version = new UniversalVersion(
            text,
            VersionDialect.Maven,
            new string[] { "0" },
            new VersionIdentifier[0],
            new VersionIdentifier[0],
            0,
            false,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(text, VersionDialect.Maven, version);
    }

    internal MavenVersionToken[] Tokenize()
    {
        System.Collections.Generic.List<MavenVersionToken> tokens = new System.Collections.Generic.List<MavenVersionToken>();

        while (!IsAtEnd())
        {
            if (IsSeparator(Peek()))
            {
                pendingSeparator = Peek();
                Advance();
            }
            else
            {
                tokens.Add(ReadToken());
            }
        }

        return tokens.ToArray();
    }

    internal MavenVersionToken ReadToken()
    {
        int startIndex = index;
        bool numeric = VersionChar.IsDigit(Peek());
        char separatorBefore = pendingSeparator;
        pendingSeparator = VersionChar.EndOfInput;

        while (!IsAtEnd() && !IsSeparator(Peek()) && VersionChar.IsDigit(Peek()) == numeric)
        {
            Advance();
        }

        return new MavenVersionToken(source.Substring(startIndex, index - startIndex), numeric, separatorBefore);
    }

    internal bool IsSeparator(char value)
    {
        return value == '.' || value == '-' || value == '_';
    }

    internal char Peek()
    {
        if (IsAtEnd())
        {
            return VersionChar.EndOfInput;
        }

        return source[index];
    }

    internal void Advance()
    {
        if (!IsAtEnd())
        {
            index = index + 1;
        }
    }

    internal bool IsAtEnd()
    {
        return index >= source.Length;
    }

    internal static bool IsIllegalVersionCharacter(char value)
    {
        return VersionChar.IsWhitespace(value)
            || value == '['
            || value == ']'
            || value == '('
            || value == ')'
            || value == ',';
    }
}
