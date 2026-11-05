namespace version;

internal class SemVerPartialParser
{
    internal string source;
    internal int index;

    internal SemVerPartialParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
    }

    internal static VersionParseResult Parse(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        string trimmed = TrimOuterWhitespace(text);
        SemVerPartialParser parser = new SemVerPartialParser(trimmed);
        return parser.ParsePartialVersion();
    }

    internal VersionParseResult ParsePartialVersion()
    {
        if (IsAtEnd())
        {
            return Failure(SemVerDiagnosticCodes.MissingMajor, "Expected partial SemVer major component.", index);
        }

        if (IsWildcard(Peek()))
        {
            Advance();
            if (!IsAtEnd())
            {
                return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after wildcard partial version.", index);
            }

            return Success(new string[] { "0", "0", "0" }, 3, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        string major = ReadNumericComponent();
        if (major.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.IllegalCharacter, "Expected numeric major component or wildcard.", index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, "0", "0" }, 2, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() != '.')
        {
            return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after partial major component.", index);
        }

        Advance();

        if (IsWildcard(Peek()))
        {
            Advance();
            if (!IsAtEnd())
            {
                return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after partial minor wildcard.", index);
            }

            return Success(new string[] { major, "0", "0" }, 2, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        string minor = ReadNumericComponent();
        if (minor.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingMinor, "Expected partial SemVer minor component.", index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, minor, "0" }, 1, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() != '.')
        {
            return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after partial minor component.", index);
        }

        Advance();

        if (IsWildcard(Peek()))
        {
            Advance();
            if (!IsAtEnd())
            {
                return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after partial patch wildcard.", index);
            }

            return Success(new string[] { major, minor, "0" }, 1, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        string patch = ReadNumericComponent();
        if (patch.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingPatch, "Expected partial SemVer patch component.", index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, minor, patch }, 0, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() == '-' || Peek() == '+')
        {
            return ParseFullVersionAsPartial();
        }

        return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after partial patch component.", index);
    }

    internal VersionParseResult ParseFullVersionAsPartial()
    {
        VersionParseResult strictResult = SemVerParser.ParseStrict(source);
        if (strictResult.IsFailure())
        {
            return strictResult;
        }

        UniversalVersion strictVersion = strictResult.GetVersion();
        return Success(
            strictVersion.GetNumericComponents(),
            0,
            strictVersion.GetPrereleaseIdentifiers(),
            strictVersion.GetBuildIdentifiers());
    }

    internal VersionParseResult Success(string[] components, int missingComponentCount, VersionIdentifier[] prereleaseIdentifiers, VersionIdentifier[] buildIdentifiers)
    {
        UniversalVersion version = new UniversalVersion(
            source,
            VersionDialect.NodeSemVer,
            components,
            prereleaseIdentifiers,
            buildIdentifiers,
            missingComponentCount,
            false,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(source, VersionDialect.NodeSemVer, version);
    }

    internal VersionParseResult Failure(string code, string message, int diagnosticIndex)
    {
        return VersionParseResult.CreateFailure(
            source,
            VersionDialect.NodeSemVer,
            VersionDiagnostic.FromIndex(code, message, source, diagnosticIndex));
    }

    internal string ReadNumericComponent()
    {
        int startIndex = index;

        while (VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        if (index == startIndex)
        {
            return "";
        }

        if (index - startIndex > 1 && source[startIndex] == '0')
        {
            index = startIndex;
            return "";
        }

        return source.Substring(startIndex, index - startIndex);
    }

    internal bool IsWildcard(char value)
    {
        return VersionChar.IsStar(value) || VersionChar.IsX(value);
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

    internal static string TrimOuterWhitespace(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(startIndex, endIndex - startIndex);
    }
}
