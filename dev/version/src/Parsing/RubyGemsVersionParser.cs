namespace version;

internal class RubyGemsVersionParser
{
    internal string source;
    internal string normalizedSource;
    internal int index;

    internal RubyGemsVersionParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        normalizedSource = TrimOuterWhitespace(sourceValue);
        if (normalizedSource.Length == 0)
        {
            normalizedSource = "0";
        }

        index = 0;
    }

    internal static RubyGemsVersionParseResult Parse(string text)
    {
        RubyGemsVersionParser parser = new RubyGemsVersionParser(text);
        return parser.ParseVersion();
    }

    internal static VersionParseResult ParseUniversal(string text)
    {
        RubyGemsVersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            return VersionParseResult.CreateFailure(text, VersionDialect.RubyGems, result.GetDiagnostic(0));
        }

        RubyGemsVersion rubyGemsVersion = result.GetVersion();
        UniversalVersion version = new UniversalVersion(
            rubyGemsVersion.GetOriginalText(),
            VersionDialect.RubyGems,
            new string[] { "0" },
            GetUniversalPrereleaseIdentifiers(rubyGemsVersion),
            new VersionIdentifier[0],
            0,
            false,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(text, VersionDialect.RubyGems, version);
    }

    internal RubyGemsVersionParseResult ParseVersion()
    {
        if (!VersionChar.IsDigit(Peek()))
        {
            return Failure(0);
        }

        System.Collections.Generic.List<RubyGemsVersionSegment> segments = new System.Collections.Generic.List<RubyGemsVersionSegment>();
        bool expectSegment = true;

        while (!IsAtEnd())
        {
            if (Peek() == '.')
            {
                if (expectSegment)
                {
                    return Failure(index);
                }

                Advance();
                expectSegment = true;
            }
            else if (VersionChar.IsAlphaNumeric(Peek()))
            {
                segments.Add(ReadSegment());
                expectSegment = false;
            }
            else
            {
                return Failure(index);
            }
        }

        if (expectSegment)
        {
            return Failure(index);
        }

        RubyGemsVersion version = new RubyGemsVersion(normalizedSource, segments.ToArray());
        return RubyGemsVersionParseResult.CreateSuccess(source, version);
    }

    internal RubyGemsVersionSegment ReadSegment()
    {
        int startIndex = index;
        bool numeric = VersionChar.IsDigit(Peek());

        while (!IsAtEnd() && VersionChar.IsAlphaNumeric(Peek()) && VersionChar.IsDigit(Peek()) == numeric)
        {
            Advance();
        }

        return new RubyGemsVersionSegment(normalizedSource.Substring(startIndex, index - startIndex), numeric);
    }

    internal RubyGemsVersionParseResult Failure(int diagnosticIndex)
    {
        return RubyGemsVersionParseResult.CreateFailure(
            source,
            VersionDiagnostic.FromIndex(
                RubyGemsDiagnosticCodes.InvalidVersion,
                "Malformed RubyGems version.",
                source,
                diagnosticIndex));
    }

    internal char Peek()
    {
        if (IsAtEnd())
        {
            return VersionChar.EndOfInput;
        }

        return normalizedSource[index];
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
        return index >= normalizedSource.Length;
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

    internal static VersionIdentifier[] GetUniversalPrereleaseIdentifiers(RubyGemsVersion version)
    {
        if (!version.IsPrerelease())
        {
            return new VersionIdentifier[0];
        }

        VersionIdentifier[] identifiers = new VersionIdentifier[1];
        identifiers[0] = VersionIdentifier.CreateText("prerelease");
        return identifiers;
    }
}
