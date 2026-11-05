namespace version;

internal class PythonVersionParser
{
    internal string source;
    internal string normalizedSource;
    internal int index;

    internal PythonVersionParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        normalizedSource = sourceValue.ToLowerInvariant();
        index = 0;
    }

    internal static VersionParseResult Parse(string text)
    {
        PythonVersionParser parser = new PythonVersionParser(text);
        PythonVersion? version = parser.ParsePythonVersion();
        if (version == null)
        {
            return VersionParseResult.CreateFailure(
                text,
                VersionDialect.Python,
                VersionDiagnostic.FromIndex(PythonDiagnosticCodes.InvalidVersion, "Malformed Python version.", text, parser.GetDiagnosticIndex()));
        }

        UniversalVersion universalVersion = new UniversalVersion(
            version.GetOriginalText(),
            VersionDialect.Python,
            version.GetReleaseSegments(),
            GetUniversalPrereleaseIdentifiers(version),
            new VersionIdentifier[0],
            0,
            false,
            false,
            "",
            version);

        return VersionParseResult.CreateSuccess(text, VersionDialect.Python, universalVersion);
    }

    internal static PythonVersion ParseVersion(string text)
    {
        PythonVersionParser parser = new PythonVersionParser(text);
        PythonVersion? version = parser.ParsePythonVersion();
        if (version == null)
        {
            throw new System.ArgumentException("Malformed Python version.");
        }

        return version;
    }

    internal PythonVersion? ParsePythonVersion()
    {
        if (IsAtEnd())
        {
            return null;
        }

        if (Peek() == 'v')
        {
            Advance();
        }

        string epoch = "0";
        int epochStartIndex = index;
        string firstNumber = ReadDigits();
        if (firstNumber.Length == 0)
        {
            return null;
        }

        if (Peek() == '!')
        {
            epoch = VersionFormatter.NormalizeNumericText(firstNumber);
            Advance();
            firstNumber = ReadDigits();
            if (firstNumber.Length == 0)
            {
                index = epochStartIndex;
                return null;
            }
        }

        System.Collections.Generic.List<string> releaseSegments = new System.Collections.Generic.List<string>();
        releaseSegments.Add(VersionFormatter.NormalizeNumericText(firstNumber));

        while (Peek() == '.')
        {
            int separatorIndex = index;
            Advance();
            string segment = ReadDigits();
            if (segment.Length == 0)
            {
                index = separatorIndex;
                break;
            }

            releaseSegments.Add(VersionFormatter.NormalizeNumericText(segment));
        }

        bool preReleasePresent = false;
        string preReleaseLabel = "";
        string preReleaseNumber = "";
        bool postReleasePresent = false;
        string postReleaseNumber = "";
        bool developmentReleasePresent = false;
        string developmentReleaseNumber = "";

        if (TryReadPreRelease())
        {
            preReleasePresent = true;
            preReleaseLabel = pendingLabel;
            preReleaseNumber = pendingNumber;
        }

        if (TryReadPostRelease())
        {
            postReleasePresent = true;
            postReleaseNumber = pendingNumber;
        }

        if (TryReadDevelopmentRelease())
        {
            developmentReleasePresent = true;
            developmentReleaseNumber = pendingNumber;
        }

        PythonLocalVersionSegment[] localSegments = new PythonLocalVersionSegment[0];
        if (Peek() == '+')
        {
            Advance();
            localSegments = ReadLocalSegments();
            if (localSegments.Length == 0)
            {
                return null;
            }
        }

        if (!IsAtEnd())
        {
            return null;
        }

        return new PythonVersion(
            source,
            epoch,
            releaseSegments.ToArray(),
            preReleasePresent,
            preReleaseLabel,
            preReleaseNumber,
            postReleasePresent,
            postReleaseNumber,
            developmentReleasePresent,
            developmentReleaseNumber,
            localSegments);
    }

    internal string pendingLabel = "";
    internal string pendingNumber = "";

    internal bool TryReadPreRelease()
    {
        int startIndex = index;
        SkipOptionalSeparator();
        string label = ReadLetters();
        string normalizedLabel = NormalizePreReleaseLabel(label);
        if (normalizedLabel.Length == 0)
        {
            index = startIndex;
            return false;
        }

        SkipOptionalSeparator();
        string number = ReadDigits();
        if (number.Length == 0)
        {
            number = "0";
        }

        pendingLabel = normalizedLabel;
        pendingNumber = VersionFormatter.NormalizeNumericText(number);
        return true;
    }

    internal bool TryReadPostRelease()
    {
        int startIndex = index;

        if (Peek() == '-')
        {
            Advance();
            string implicitNumber = ReadDigits();
            if (implicitNumber.Length > 0)
            {
                pendingNumber = VersionFormatter.NormalizeNumericText(implicitNumber);
                return true;
            }

            index = startIndex;
        }

        SkipOptionalSeparator();
        string label = ReadLetters();
        if (!IsPostReleaseLabel(label))
        {
            index = startIndex;
            return false;
        }

        SkipOptionalSeparator();
        string number = ReadDigits();
        if (number.Length == 0)
        {
            number = "0";
        }

        pendingNumber = VersionFormatter.NormalizeNumericText(number);
        return true;
    }

    internal bool TryReadDevelopmentRelease()
    {
        int startIndex = index;
        SkipOptionalSeparator();
        string label = ReadLetters();
        if (label != "dev")
        {
            index = startIndex;
            return false;
        }

        SkipOptionalSeparator();
        string number = ReadDigits();
        if (number.Length == 0)
        {
            number = "0";
        }

        pendingNumber = VersionFormatter.NormalizeNumericText(number);
        return true;
    }

    internal PythonLocalVersionSegment[] ReadLocalSegments()
    {
        System.Collections.Generic.List<PythonLocalVersionSegment> segments = new System.Collections.Generic.List<PythonLocalVersionSegment>();

        while (!IsAtEnd())
        {
            string segment = ReadAlphaNumericText();
            if (segment.Length == 0)
            {
                return new PythonLocalVersionSegment[0];
            }

            segments.Add(new PythonLocalVersionSegment(segment, ContainsOnlyDigits(segment)));

            if (IsAtEnd())
            {
                break;
            }

            if (Peek() != '.' && Peek() != '-' && Peek() != '_')
            {
                return new PythonLocalVersionSegment[0];
            }

            Advance();
        }

        return segments.ToArray();
    }

    internal void SkipOptionalSeparator()
    {
        if (Peek() == '.' || Peek() == '-' || Peek() == '_')
        {
            Advance();
        }
    }

    internal string ReadDigits()
    {
        int startIndex = index;
        while (!IsAtEnd() && VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        return normalizedSource.Substring(startIndex, index - startIndex);
    }

    internal string ReadLetters()
    {
        int startIndex = index;
        while (!IsAtEnd() && VersionChar.IsLetter(Peek()))
        {
            Advance();
        }

        return normalizedSource.Substring(startIndex, index - startIndex);
    }

    internal string ReadAlphaNumericText()
    {
        int startIndex = index;
        while (!IsAtEnd() && VersionChar.IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        return normalizedSource.Substring(startIndex, index - startIndex);
    }

    internal string NormalizePreReleaseLabel(string label)
    {
        if (label == "a" || label == "alpha")
        {
            return "a";
        }

        if (label == "b" || label == "beta")
        {
            return "b";
        }

        if (label == "rc" || label == "c" || label == "pre" || label == "preview")
        {
            return "rc";
        }

        return "";
    }

    internal bool IsPostReleaseLabel(string label)
    {
        return label == "post" || label == "rev" || label == "r";
    }

    internal int GetDiagnosticIndex()
    {
        if (index < 0)
        {
            return 0;
        }

        if (index > source.Length)
        {
            return source.Length;
        }

        return index;
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

    internal static VersionIdentifier[] GetUniversalPrereleaseIdentifiers(PythonVersion version)
    {
        if (!version.IsPrerelease())
        {
            return new VersionIdentifier[0];
        }

        VersionIdentifier[] identifiers = new VersionIdentifier[1];
        identifiers[0] = VersionIdentifier.CreateText("prerelease");
        return identifiers;
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
