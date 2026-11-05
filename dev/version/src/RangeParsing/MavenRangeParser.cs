namespace version;

internal class MavenRangeParser
{
    internal string source;
    internal int index;
    internal bool failed;
    internal VersionRangeParseResult? failureResult;

    internal MavenRangeParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failed = false;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string text)
    {
        MavenRangeParser parser = new MavenRangeParser(text);
        return parser.ParseRange();
    }

    internal VersionRangeParseResult ParseRange()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(MavenRangeDiagnosticCodes.MissingRange, "Expected Maven version range.", index);
        }

        if (!IsOpenDelimiter(Peek()))
        {
            return ParseSoftRequirement();
        }

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();

        while (!IsAtEnd())
        {
            if (!IsOpenDelimiter(Peek()))
            {
                return Failure(MavenRangeDiagnosticCodes.InvalidDelimiter, "Expected Maven interval range.", index);
            }

            VersionComparatorSet set = ParseIntervalSet();
            if (failed)
            {
                return GetFailureResult();
            }

            sets.Add(set);
            SkipWhitespace();

            if (IsAtEnd())
            {
                break;
            }

            if (Peek() != ',')
            {
                return Failure(MavenRangeDiagnosticCodes.InvalidDelimiter, "Expected comma between Maven interval ranges.", index);
            }

            Advance();
            SkipWhitespace();

            if (IsAtEnd())
            {
                return Failure(MavenRangeDiagnosticCodes.MissingRange, "Expected Maven interval range after comma.", index);
            }
        }

        return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.Maven, new VersionRange(sets.ToArray()));
    }

    internal VersionRangeParseResult ParseSoftRequirement()
    {
        int versionStartIndex = index;
        string versionText = GetTrimmedText(versionStartIndex, source.Length);
        int diagnosticIndex = GetTrimmedStartIndex(versionStartIndex, source.Length);

        VersionParseResult versionResult = MavenVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            return Failure(MavenRangeDiagnosticCodes.InvalidVersion, "Invalid Maven soft requirement version.", diagnosticIndex);
        }

        return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.Maven, VersionRange.CreateUniversal());
    }

    internal VersionComparatorSet ParseIntervalSet()
    {
        char openDelimiter = Peek();
        int openDelimiterIndex = index;
        BoundKind lowerKind = GetBoundKind(openDelimiter);
        Advance();

        int firstStartIndex = index;
        while (!IsAtEnd() && Peek() != ',' && Peek() != ']' && Peek() != ')')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            Fail(MavenRangeDiagnosticCodes.InvalidDelimiter, "Expected Maven range closing delimiter.", index);
            return VersionComparatorSet.CreateUniversal();
        }

        int firstEndIndex = index;

        if (Peek() == ',')
        {
            return ParseBoundedOrOpenIntervalSet(openDelimiter, lowerKind, firstStartIndex, firstEndIndex);
        }

        return ParseExactIntervalSet(openDelimiter, openDelimiterIndex, firstStartIndex, firstEndIndex);
    }

    internal VersionComparatorSet ParseExactIntervalSet(char openDelimiter, int openDelimiterIndex, int versionStartIndex, int versionEndIndex)
    {
        char closeDelimiter = Peek();
        int closeDelimiterIndex = index;
        Advance();

        if (openDelimiter != '[' || closeDelimiter != ']')
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Maven exact ranges must use inclusive square brackets.", openDelimiterIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        string versionText = GetTrimmedText(versionStartIndex, versionEndIndex);
        int diagnosticIndex = GetTrimmedStartIndex(versionStartIndex, versionEndIndex);
        if (versionText.Length == 0)
        {
            Fail(MavenRangeDiagnosticCodes.MissingVersion, "Expected Maven exact range version.", closeDelimiterIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        VersionParseResult versionResult = MavenVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            Fail(MavenRangeDiagnosticCodes.InvalidVersion, "Invalid Maven exact range version.", diagnosticIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        return VersionComparatorSet.CreateSingle(
            VersionComparator.CreateEqual(versionResult.GetVersion(), VersionComparisonMode.Precedence));
    }

    internal VersionComparatorSet ParseBoundedOrOpenIntervalSet(
        char openDelimiter,
        BoundKind lowerKind,
        int lowerStartIndex,
        int lowerEndIndex)
    {
        Advance();
        int upperStartIndex = index;

        while (!IsAtEnd() && Peek() != ']' && Peek() != ')')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            Fail(MavenRangeDiagnosticCodes.InvalidDelimiter, "Expected Maven range closing delimiter.", index);
            return VersionComparatorSet.CreateUniversal();
        }

        int upperEndIndex = index;
        char closeDelimiter = Peek();
        int closeDelimiterIndex = index;
        BoundKind upperKind = GetBoundKind(closeDelimiter);
        Advance();

        string lowerText = GetTrimmedText(lowerStartIndex, lowerEndIndex);
        string upperText = GetTrimmedText(upperStartIndex, upperEndIndex);
        int lowerDiagnosticIndex = GetTrimmedStartIndex(lowerStartIndex, lowerEndIndex);
        int upperDiagnosticIndex = GetTrimmedStartIndex(upperStartIndex, upperEndIndex);

        if (lowerText.Length == 0 && upperText.Length == 0)
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Maven range must contain at least one bound.", closeDelimiterIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        if (lowerText.Length == 0 && openDelimiter == '[')
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Unbounded Maven lower ranges must use an exclusive lower delimiter.", lowerDiagnosticIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        if (upperText.Length == 0 && closeDelimiter == ']')
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Unbounded Maven upper ranges must use an exclusive upper delimiter.", closeDelimiterIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        UniversalVersion? lowerVersion = null;
        UniversalVersion? upperVersion = null;

        if (lowerText.Length > 0)
        {
            VersionParseResult lowerResult = MavenVersionParser.Parse(lowerText);
            if (lowerResult.IsFailure())
            {
                Fail(MavenRangeDiagnosticCodes.InvalidVersion, "Invalid Maven lower range version.", lowerDiagnosticIndex);
                return VersionComparatorSet.CreateUniversal();
            }

            lowerVersion = lowerResult.GetVersion();
        }

        if (upperText.Length > 0)
        {
            VersionParseResult upperResult = MavenVersionParser.Parse(upperText);
            if (upperResult.IsFailure())
            {
                Fail(MavenRangeDiagnosticCodes.InvalidVersion, "Invalid Maven upper range version.", upperDiagnosticIndex);
                return VersionComparatorSet.CreateUniversal();
            }

            upperVersion = upperResult.GetVersion();
        }

        return CreateIntervalSet(lowerKind, lowerVersion, upperKind, upperVersion, closeDelimiterIndex);
    }

    internal VersionComparatorSet CreateIntervalSet(
        BoundKind lowerKind,
        UniversalVersion? lowerVersion,
        BoundKind upperKind,
        UniversalVersion? upperVersion,
        int diagnosticIndex)
    {
        VersionComparisonMode comparisonMode = VersionComparisonMode.Precedence;

        if (lowerVersion == null)
        {
            if (upperVersion == null)
            {
                Fail(MavenRangeDiagnosticCodes.InvalidRange, "Maven range must contain at least one bound.", diagnosticIndex);
                return VersionComparatorSet.CreateUniversal();
            }

            return VersionInterval.CreateUpperBound(upperKind, upperVersion, comparisonMode).ToComparatorSet();
        }

        if (upperVersion == null)
        {
            return VersionInterval.CreateLowerBound(lowerKind, lowerVersion, comparisonMode).ToComparatorSet();
        }

        int comparison = lowerVersion.CompareToVersion(upperVersion, comparisonMode);
        if (comparison > 0)
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Maven range lower bound cannot be greater than upper bound.", diagnosticIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        if (comparison == 0 && (lowerKind == BoundKind.Exclusive || upperKind == BoundKind.Exclusive))
        {
            Fail(MavenRangeDiagnosticCodes.InvalidRange, "Maven range excludes its only possible bound version.", diagnosticIndex);
            return VersionComparatorSet.CreateUniversal();
        }

        return VersionInterval.CreateBounded(
            lowerKind,
            lowerVersion,
            upperKind,
            upperVersion,
            comparisonMode).ToComparatorSet();
    }

    internal BoundKind GetBoundKind(char delimiter)
    {
        if (delimiter == '[' || delimiter == ']')
        {
            return BoundKind.Inclusive;
        }

        return BoundKind.Exclusive;
    }

    internal bool IsOpenDelimiter(char value)
    {
        return value == '[' || value == '(';
    }

    internal string GetTrimmedText(int startIndex, int endIndex)
    {
        int trimStartIndex = GetTrimmedStartIndex(startIndex, endIndex);
        int trimEndIndex = GetTrimmedEndIndex(trimStartIndex, endIndex);
        return source.Substring(trimStartIndex, trimEndIndex - trimStartIndex);
    }

    internal int GetTrimmedStartIndex(int startIndex, int endIndex)
    {
        int trimStartIndex = startIndex;

        while (trimStartIndex < endIndex && VersionChar.IsWhitespace(source[trimStartIndex]))
        {
            trimStartIndex = trimStartIndex + 1;
        }

        return trimStartIndex;
    }

    internal int GetTrimmedEndIndex(int startIndex, int endIndex)
    {
        int trimEndIndex = endIndex;

        while (trimEndIndex > startIndex && VersionChar.IsWhitespace(source[trimEndIndex - 1]))
        {
            trimEndIndex = trimEndIndex - 1;
        }

        return trimEndIndex;
    }

    internal void SkipWhitespace()
    {
        while (!IsAtEnd() && VersionChar.IsWhitespace(Peek()))
        {
            Advance();
        }
    }

    internal VersionRangeParseResult Failure(string code, string message, int diagnosticIndex)
    {
        Fail(code, message, diagnosticIndex);
        return GetFailureResult();
    }

    internal void Fail(string code, string message, int diagnosticIndex)
    {
        failed = true;
        failureResult = VersionRangeParseResult.CreateFailure(
            source,
            RangeSyntax.Maven,
            VersionDiagnostic.FromIndex(code, message, source, diagnosticIndex));
    }

    internal VersionRangeParseResult GetFailureResult()
    {
        if (failureResult == null)
        {
            throw new System.InvalidOperationException("Parser is not in a failed state.");
        }

        return failureResult;
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
