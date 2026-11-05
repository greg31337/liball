namespace version;

internal class SemVerParser
{
    internal string originalSource;
    internal string source;
    internal int index;
    internal int diagnosticOffset;
    internal bool failed;
    internal VersionParseResult? failureResult;
    internal VersionDialect dialect;
    internal bool leadingVPrefix;
    internal bool rejectCoreLeadingZeroes;
    internal bool rejectPrereleaseLeadingZeroes;

    internal SemVerParser(
        string sourceValue,
        string originalSourceValue,
        int diagnosticOffsetValue,
        VersionDialect dialectValue,
        bool leadingVPrefixValue,
        bool rejectCoreLeadingZeroesValue,
        bool rejectPrereleaseLeadingZeroesValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);
        System.ArgumentNullException.ThrowIfNull(originalSourceValue);

        originalSource = originalSourceValue;
        source = sourceValue;
        index = 0;
        diagnosticOffset = diagnosticOffsetValue;
        failed = false;
        failureResult = null;
        dialect = dialectValue;
        leadingVPrefix = leadingVPrefixValue;
        rejectCoreLeadingZeroes = rejectCoreLeadingZeroesValue;
        rejectPrereleaseLeadingZeroes = rejectPrereleaseLeadingZeroesValue;
    }

    internal static VersionParseResult ParseStrict(string text)
    {
        SemVerParser parser = new SemVerParser(text, text, 0, VersionDialect.SemVer, false, true, true);
        return parser.ParseStrictVersion();
    }

    internal static VersionParseResult ParseLoose(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

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

        int versionStartIndex = startIndex;
        bool foundLeadingVPrefix = false;

        if (versionStartIndex < endIndex && text[versionStartIndex] == '=')
        {
            versionStartIndex = versionStartIndex + 1;
        }

        while (versionStartIndex < endIndex && VersionChar.IsWhitespace(text[versionStartIndex]))
        {
            versionStartIndex = versionStartIndex + 1;
        }

        if (versionStartIndex < endIndex && VersionChar.IsV(text[versionStartIndex]))
        {
            foundLeadingVPrefix = true;
            versionStartIndex = versionStartIndex + 1;
        }

        while (versionStartIndex < endIndex && VersionChar.IsWhitespace(text[versionStartIndex]))
        {
            versionStartIndex = versionStartIndex + 1;
        }

        string parseText = text.Substring(versionStartIndex, endIndex - versionStartIndex);
        SemVerParser parser = new SemVerParser(parseText, text, versionStartIndex, VersionDialect.NodeSemVer, foundLeadingVPrefix, false, false);
        return parser.ParseStrictVersion();
    }

    internal static VersionParseResult Coerce(string text)
    {
        return SemVerCoercer.Coerce(text);
    }

    internal static VersionParseResult CoerceRightToLeft(string text)
    {
        return SemVerCoercer.CoerceRightToLeft(text);
    }

    internal static VersionParseResult ParsePartial(string text)
    {
        return SemVerPartialParser.Parse(text);
    }

    internal VersionParseResult ParseStrictVersion()
    {
        string major = ReadCoreNumericComponent(SemVerDiagnosticCodes.MissingMajor, "Expected major version component.");
        if (failed)
        {
            return GetFailureResult();
        }

        ExpectDot(SemVerDiagnosticCodes.MissingMinor, "Expected minor version component.");
        if (failed)
        {
            return GetFailureResult();
        }

        string minor = ReadCoreNumericComponent(SemVerDiagnosticCodes.MissingMinor, "Expected minor version component.");
        if (failed)
        {
            return GetFailureResult();
        }

        ExpectDot(SemVerDiagnosticCodes.MissingPatch, "Expected patch version component.");
        if (failed)
        {
            return GetFailureResult();
        }

        string patch = ReadCoreNumericComponent(SemVerDiagnosticCodes.MissingPatch, "Expected patch version component.");
        if (failed)
        {
            return GetFailureResult();
        }

        VersionIdentifier[] prereleaseIdentifiers = new VersionIdentifier[0];
        VersionIdentifier[] buildIdentifiers = new VersionIdentifier[0];

        if (Peek() == '-')
        {
            Advance();
            prereleaseIdentifiers = ReadIdentifierSequence(rejectPrereleaseLeadingZeroes, true);
            if (failed)
            {
                return GetFailureResult();
            }
        }

        if (Peek() == '+')
        {
            Advance();
            buildIdentifiers = ReadIdentifierSequence(false, false);
            if (failed)
            {
                return GetFailureResult();
            }
        }

        if (!IsAtEnd())
        {
            Fail(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after SemVer version.", index);
            return GetFailureResult();
        }

        UniversalVersion version = new UniversalVersion(
            originalSource,
            dialect,
            new string[] { major, minor, patch },
            prereleaseIdentifiers,
            buildIdentifiers,
            0,
            leadingVPrefix,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(originalSource, dialect, version);
    }

    internal string ReadCoreNumericComponent(string code, string message)
    {
        int startIndex = index;

        if (!VersionChar.IsDigit(Peek()))
        {
            if (IsAtEnd())
            {
                Fail(code, message, index);
                return "";
            }

            Fail(SemVerDiagnosticCodes.IllegalCharacter, "Expected numeric version component but found an illegal character.", index);
            return "";
        }

        while (VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        if (rejectCoreLeadingZeroes && index - startIndex > 1 && source[startIndex] == '0')
        {
            Fail(SemVerDiagnosticCodes.NumericComponentLeadingZero, "Numeric version components must not contain leading zeroes.", startIndex);
            return "";
        }

        return source.Substring(startIndex, index - startIndex);
    }

    internal VersionIdentifier[] ReadIdentifierSequence(bool rejectNumericLeadingZeroes, bool prerelease)
    {
        System.Collections.Generic.List<VersionIdentifier> identifiers = new System.Collections.Generic.List<VersionIdentifier>();
        bool expectIdentifier = true;

        if (IsAtEnd())
        {
            FailMalformedSection(prerelease, index);
            return new VersionIdentifier[0];
        }

        while (!IsAtEnd())
        {
            if (Peek() == '+')
            {
                if (expectIdentifier)
                {
                    FailEmptyIdentifier(prerelease, index);
                    return new VersionIdentifier[0];
                }

                return identifiers.ToArray();
            }

            if (Peek() == '.')
            {
                if (expectIdentifier)
                {
                    FailEmptyIdentifier(prerelease, index);
                    return new VersionIdentifier[0];
                }

                Advance();
                expectIdentifier = true;
            }
            else if (IsIdentifierCharacter(Peek()))
            {
                identifiers.Add(ReadIdentifier(rejectNumericLeadingZeroes));
                if (failed)
                {
                    return new VersionIdentifier[0];
                }

                expectIdentifier = false;
            }
            else
            {
                Fail(SemVerDiagnosticCodes.IllegalCharacter, "Identifier contains an illegal character.", index);
                return new VersionIdentifier[0];
            }
        }

        if (expectIdentifier)
        {
            if (identifiers.Count == 0)
            {
                FailMalformedSection(prerelease, index);
                return new VersionIdentifier[0];
            }

            FailEmptyIdentifier(prerelease, index);
            return new VersionIdentifier[0];
        }

        return identifiers.ToArray();
    }

    internal VersionIdentifier ReadIdentifier(bool rejectNumericLeadingZeroes)
    {
        int startIndex = index;
        bool numeric = true;

        while (IsIdentifierCharacter(Peek()))
        {
            if (!VersionChar.IsDigit(Peek()))
            {
                numeric = false;
            }

            Advance();
        }

        if (index == startIndex)
        {
            Fail(SemVerDiagnosticCodes.IllegalCharacter, "Expected identifier but found an illegal character.", index);
            return VersionIdentifier.CreateText("invalid");
        }

        string text = source.Substring(startIndex, index - startIndex);

        if (numeric && rejectNumericLeadingZeroes && text.Length > 1 && text[0] == '0')
        {
            Fail(SemVerDiagnosticCodes.PrereleaseLeadingZero, "Numeric prerelease identifiers must not contain leading zeroes.", startIndex);
            return VersionIdentifier.CreateText("invalid");
        }

        if (numeric)
        {
            return VersionIdentifier.CreateNumeric(text);
        }

        return VersionIdentifier.CreateText(text);
    }

    internal bool IsIdentifierCharacter(char value)
    {
        return VersionChar.IsAlphaNumeric(value) || VersionChar.IsHyphen(value);
    }

    internal void ExpectDot(string missingComponentCode, string missingComponentMessage)
    {
        if (Peek() == '.')
        {
            Advance();
            return;
        }

        if (IsAtEnd())
        {
            Fail(missingComponentCode, missingComponentMessage, index);
            return;
        }

        Fail(SemVerDiagnosticCodes.ExpectedDot, "Expected dot between numeric version components.", index);
    }

    internal void FailEmptyIdentifier(bool prerelease, int diagnosticIndex)
    {
        if (prerelease)
        {
            Fail(SemVerDiagnosticCodes.EmptyPrereleaseIdentifier, "Prerelease identifiers cannot be empty.", diagnosticIndex);
            return;
        }

        Fail(SemVerDiagnosticCodes.EmptyBuildIdentifier, "Build metadata identifiers cannot be empty.", diagnosticIndex);
    }

    internal void FailMalformedSection(bool prerelease, int diagnosticIndex)
    {
        if (prerelease)
        {
            Fail(SemVerDiagnosticCodes.MalformedPrerelease, "Prerelease section must contain at least one identifier.", diagnosticIndex);
            return;
        }

        Fail(SemVerDiagnosticCodes.MalformedBuild, "Build metadata section must contain at least one identifier.", diagnosticIndex);
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

    internal void Fail(string code, string message, int diagnosticIndex)
    {
        if (failed)
        {
            return;
        }

        failed = true;
        failureResult = VersionParseResult.CreateFailure(
            originalSource,
            dialect,
            VersionDiagnostic.FromIndex(code, message, originalSource, diagnosticOffset + diagnosticIndex));
    }

    internal VersionParseResult GetFailureResult()
    {
        if (failureResult == null)
        {
            throw new System.InvalidOperationException("Parser failed without a diagnostic.");
        }

        return failureResult;
    }
}
