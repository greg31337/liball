namespace version;

internal class NuGetVersionParser
{
    internal string source;
    internal int index;

    internal NuGetVersionParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
    }

    internal static VersionParseResult Parse(string text)
    {
        NuGetVersionParser parser = new NuGetVersionParser(text);
        return parser.ParseVersion();
    }

    internal VersionParseResult ParseVersion()
    {
        System.Collections.Generic.List<string> numericComponents = new System.Collections.Generic.List<string>();

        string major = ReadNumericComponent();
        if (major.Length == 0)
        {
            return Failure(SemVerDiagnosticCodes.MissingMajor, "Expected NuGet major version component.", index);
        }

        numericComponents.Add(major);

        while (Peek() == '.' && numericComponents.Count < 4)
        {
            Advance();
            string component = ReadNumericComponent();
            if (component.Length == 0)
            {
                return Failure(SemVerDiagnosticCodes.IllegalCharacter, "Expected NuGet numeric version component.", index);
            }

            numericComponents.Add(component);
        }

        VersionIdentifier[] prereleaseIdentifiers = new VersionIdentifier[0];
        VersionIdentifier[] buildIdentifiers = new VersionIdentifier[0];

        if (Peek() == '-')
        {
            Advance();
            prereleaseIdentifiers = ReadIdentifierSequence();
            if (prereleaseIdentifiers.Length == 0)
            {
                return Failure(SemVerDiagnosticCodes.MalformedPrerelease, "Prerelease section must contain at least one identifier.", index);
            }
        }

        if (Peek() == '+')
        {
            Advance();
            buildIdentifiers = ReadIdentifierSequence();
            if (buildIdentifiers.Length == 0)
            {
                return Failure(SemVerDiagnosticCodes.MalformedBuild, "Build metadata section must contain at least one identifier.", index);
            }
        }

        if (!IsAtEnd())
        {
            return Failure(SemVerDiagnosticCodes.TrailingContent, "Unexpected character after NuGet version.", index);
        }

        return Success(numericComponents.ToArray(), prereleaseIdentifiers, buildIdentifiers);
    }

    internal VersionParseResult Success(string[] parsedNumericComponents, VersionIdentifier[] prereleaseIdentifiers, VersionIdentifier[] buildIdentifiers)
    {
        int missingComponentCount = 0;
        bool revisionPresent = false;
        string revision = "";
        string[] numericComponents = parsedNumericComponents;

        if (parsedNumericComponents.Length < 3)
        {
            missingComponentCount = 3 - parsedNumericComponents.Length;
        }

        if (parsedNumericComponents.Length == 4)
        {
            revisionPresent = true;
            revision = parsedNumericComponents[3];
            numericComponents = new string[] { parsedNumericComponents[0], parsedNumericComponents[1], parsedNumericComponents[2] };
        }

        UniversalVersion version = new UniversalVersion(
            source,
            VersionDialect.NuGet,
            numericComponents,
            prereleaseIdentifiers,
            buildIdentifiers,
            missingComponentCount,
            false,
            revisionPresent,
            revision,
            null);

        return VersionParseResult.CreateSuccess(source, VersionDialect.NuGet, version);
    }

    internal VersionIdentifier[] ReadIdentifierSequence()
    {
        System.Collections.Generic.List<VersionIdentifier> identifiers = new System.Collections.Generic.List<VersionIdentifier>();
        bool expectIdentifier = true;

        while (!IsAtEnd())
        {
            if (Peek() == '+')
            {
                if (expectIdentifier)
                {
                    return new VersionIdentifier[0];
                }

                return identifiers.ToArray();
            }

            if (Peek() == '.')
            {
                if (expectIdentifier)
                {
                    return new VersionIdentifier[0];
                }

                Advance();
                expectIdentifier = true;
            }
            else if (IsIdentifierCharacter(Peek()))
            {
                identifiers.Add(ReadIdentifier());
                expectIdentifier = false;
            }
            else
            {
                return new VersionIdentifier[0];
            }
        }

        if (expectIdentifier)
        {
            return new VersionIdentifier[0];
        }

        return identifiers.ToArray();
    }

    internal VersionIdentifier ReadIdentifier()
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

        string text = source.Substring(startIndex, index - startIndex);

        if (numeric)
        {
            return VersionIdentifier.CreateNumeric(text);
        }

        return VersionIdentifier.CreateText(text);
    }

    internal string ReadNumericComponent()
    {
        int startIndex = index;

        while (VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        return source.Substring(startIndex, index - startIndex);
    }

    internal bool IsIdentifierCharacter(char value)
    {
        return VersionChar.IsAlphaNumeric(value) || VersionChar.IsHyphen(value);
    }

    internal VersionParseResult Failure(string code, string message, int diagnosticIndex)
    {
        return VersionParseResult.CreateFailure(
            source,
            VersionDialect.NuGet,
            VersionDiagnostic.FromIndex(code, message, source, diagnosticIndex));
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
}
