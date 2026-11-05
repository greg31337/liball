namespace version;

internal class GoVersionParser
{
    internal string source;
    internal string body;
    internal int index;

    internal GoVersionParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        body = "";
        index = 0;
    }

    internal static VersionParseResult Parse(string text)
    {
        GoVersionParser parser = new GoVersionParser(text);
        return parser.ParseVersion();
    }

    internal VersionParseResult ParseVersion()
    {
        if (source.Length == 0 || source[0] != 'v')
        {
            return Failure(GoDiagnosticCodes.MissingLeadingV, "Go module versions must start with a lowercase v.", 0);
        }

        body = source.Substring(1);
        index = 0;

        if (body.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingMajor, "Expected Go module major version after v.", 1);
        }

        string major = ReadNumericComponent();
        if (major.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.IllegalCharacter, "Expected numeric Go module major version after v.", 1 + index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, "0", "0" }, 2, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() != '.')
        {
            return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after Go module major version.", 1 + index);
        }

        Advance();

        string minor = ReadNumericComponent();
        if (minor.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingMinor, "Expected Go module minor version.", 1 + index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, minor, "0" }, 1, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() != '.')
        {
            return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after Go module minor version.", 1 + index);
        }

        Advance();

        string patch = ReadNumericComponent();
        if (patch.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingPatch, "Expected Go module patch version.", 1 + index);
        }

        if (IsAtEnd())
        {
            return Success(new string[] { major, minor, patch }, 0, new VersionIdentifier[0], new VersionIdentifier[0]);
        }

        if (Peek() == '-' || Peek() == '+')
        {
            return ParseStrictBodyAsGoVersion();
        }

        return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after Go module patch version.", 1 + index);
    }

    internal VersionParseResult ParseStrictBodyAsGoVersion()
    {
        VersionParseResult strictResult = SemVerParser.ParseStrict(body);
        if (strictResult.IsFailure())
        {
            VersionDiagnostic diagnostic = strictResult.GetDiagnostic(0);
            return Failure(diagnostic.GetCode(), diagnostic.GetMessage(), 1 + diagnostic.GetIndex());
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
            VersionDialect.Go,
            components,
            prereleaseIdentifiers,
            buildIdentifiers,
            missingComponentCount,
            true,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(source, VersionDialect.Go, version);
    }

    internal VersionParseResult Failure(string code, string message, int diagnosticIndex)
    {
        return VersionParseResult.CreateFailure(
            source,
            VersionDialect.Go,
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

        if (index - startIndex > 1 && body[startIndex] == '0')
        {
            index = startIndex;
            return "";
        }

        return body.Substring(startIndex, index - startIndex);
    }

    internal char Peek()
    {
        if (IsAtEnd())
        {
            return VersionChar.EndOfInput;
        }

        return body[index];
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
        return index >= body.Length;
    }
}
