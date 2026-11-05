namespace version;

internal class SemVerCoercer
{
    internal string source;
    internal int index;

    internal SemVerCoercer(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
    }

    internal static VersionParseResult Coerce(string text)
    {
        SemVerCoercer coercer = new SemVerCoercer(text);
        return coercer.CoerceVersion();
    }

    internal static VersionParseResult CoerceRightToLeft(string text)
    {
        SemVerCoercer coercer = new SemVerCoercer(text);
        return coercer.CoerceRightToLeftVersion();
    }

    internal VersionParseResult CoerceVersion()
    {
        MoveToFirstDigit();

        if (IsAtEnd())
        {
            return CreateNoCoercibleVersionFailure();
        }

        return CoerceAtCurrentIndex();
    }

    internal VersionParseResult CoerceRightToLeftVersion()
    {
        int candidateStart = -1;
        int candidateEnd = -1;
        int searchIndex = 0;

        while (searchIndex < source.Length)
        {
            if (IsCandidateStart(searchIndex))
            {
                int currentEnd = FindCandidateEnd(searchIndex);
                if (currentEnd > candidateEnd
                    || (currentEnd == candidateEnd && (candidateStart < 0 || searchIndex < candidateStart)))
                {
                    candidateStart = searchIndex;
                    candidateEnd = currentEnd;
                }
            }

            searchIndex = searchIndex + 1;
        }

        if (candidateStart < 0)
        {
            return CreateNoCoercibleVersionFailure();
        }

        index = candidateStart;
        return CoerceAtCurrentIndex();
    }

    internal VersionParseResult CoerceAtCurrentIndex()
    {

        bool foundLeadingVPrefix = false;
        if (index > 0 && VersionChar.IsV(source[index - 1]))
        {
            foundLeadingVPrefix = true;
        }

        string major = VersionFormatter.NormalizeNumericText(ReadNumericText());
        string minor = "0";
        string patch = "0";
        int missingComponentCount = 2;

        if (Peek() == '.' && VersionChar.IsDigit(PeekAt(1)))
        {
            Advance();
            minor = VersionFormatter.NormalizeNumericText(ReadNumericText());
            missingComponentCount = 1;

            if (Peek() == '.' && VersionChar.IsDigit(PeekAt(1)))
            {
                Advance();
                patch = VersionFormatter.NormalizeNumericText(ReadNumericText());
                missingComponentCount = 0;
            }
        }

        UniversalVersion version = new UniversalVersion(
            source,
            VersionDialect.NodeSemVer,
            new string[] { major, minor, patch },
            new VersionIdentifier[0],
            new VersionIdentifier[0],
            missingComponentCount,
            foundLeadingVPrefix,
            false,
            "",
            null);

        return VersionParseResult.CreateSuccess(source, VersionDialect.NodeSemVer, version);
    }

    internal bool IsCandidateStart(int candidateIndex)
    {
        if (!VersionChar.IsDigit(source[candidateIndex]))
        {
            return false;
        }

        return candidateIndex == 0 || !VersionChar.IsDigit(source[candidateIndex - 1]);
    }

    internal int FindCandidateEnd(int candidateIndex)
    {
        int candidateEnd = candidateIndex;

        while (candidateEnd < source.Length && VersionChar.IsDigit(source[candidateEnd]))
        {
            candidateEnd = candidateEnd + 1;
        }

        int additionalComponentCount = 0;
        while (additionalComponentCount < 2
            && candidateEnd + 1 < source.Length
            && source[candidateEnd] == '.'
            && VersionChar.IsDigit(source[candidateEnd + 1]))
        {
            candidateEnd = candidateEnd + 1;
            while (candidateEnd < source.Length && VersionChar.IsDigit(source[candidateEnd]))
            {
                candidateEnd = candidateEnd + 1;
            }

            additionalComponentCount = additionalComponentCount + 1;
        }

        return candidateEnd;
    }

    internal VersionParseResult CreateNoCoercibleVersionFailure()
    {
        return VersionParseResult.CreateFailure(
            source,
            VersionDialect.NodeSemVer,
            VersionDiagnostic.FromIndex(
                SemVerDiagnosticCodes.NoCoercibleVersion,
                "Input does not contain a coercible SemVer version.",
                source,
                source.Length));
    }

    internal void MoveToFirstDigit()
    {
        while (!IsAtEnd() && !VersionChar.IsDigit(Peek()))
        {
            Advance();
        }
    }

    internal string ReadNumericText()
    {
        int startIndex = index;

        while (VersionChar.IsDigit(Peek()))
        {
            Advance();
        }

        return source.Substring(startIndex, index - startIndex);
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
        int targetIndex = index + offset;
        if (targetIndex < 0 || targetIndex >= source.Length)
        {
            return VersionChar.EndOfInput;
        }

        return source[targetIndex];
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
