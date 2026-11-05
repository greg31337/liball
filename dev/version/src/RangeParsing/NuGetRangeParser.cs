namespace version;

internal class NuGetRangeParser
{
    internal string source;
    internal int index;
    internal VersionRangeParseResult? failureResult;

    internal NuGetRangeParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string text)
    {
        NuGetRangeParser parser = new NuGetRangeParser(text);
        return parser.ParseRange();
    }

    internal VersionRangeParseResult ParseRange()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(NuGetRangeDiagnosticCodes.MissingVersion, "Expected NuGet version range.", index);
        }

        if (Peek() == '[' || Peek() == '(')
        {
            return ParseIntervalRange();
        }

        if (ContainsStar(GetTrimmedText(index, source.Length)))
        {
            return ParseFloatingRange();
        }

        return ParseBareMinimumRange();
    }

    internal VersionRangeParseResult ParseFloatingRange()
    {
        int versionStartIndex = index;
        int versionEndIndex = source.Length;
        string versionText = GetTrimmedText(versionStartIndex, versionEndIndex);
        int diagnosticIndex = GetTrimmedStartIndex(versionStartIndex, versionEndIndex);

        NuGetFloatingVersion floatingVersion = NuGetFloatingVersion.Parse(versionText);
        if (!floatingVersion.IsValid())
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidVersion, "Invalid NuGet floating version pattern.", diagnosticIndex);
        }

        return VersionRangeParseResult.CreateSuccess(
            source,
            RangeSyntax.NuGet,
            VersionRange.CreateNuGetFloating(floatingVersion));
    }

    internal VersionRangeParseResult ParseBareMinimumRange()
    {
        int versionStartIndex = index;
        int versionEndIndex = source.Length;
        string versionText = GetTrimmedText(versionStartIndex, versionEndIndex);
        int diagnosticIndex = GetTrimmedStartIndex(versionStartIndex, versionEndIndex);

        if (versionText.Length == 0)
        {
            return Failure(NuGetRangeDiagnosticCodes.MissingVersion, "Expected NuGet minimum version.", diagnosticIndex);
        }

        VersionParseResult versionResult = NuGetVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidVersion, "Invalid NuGet minimum version.", diagnosticIndex);
        }

        VersionComparator comparator = VersionComparator.CreateGreaterThanOrEqual(
            versionResult.GetVersion(),
            VersionComparisonMode.Precedence);
        return VersionRangeParseResult.CreateSuccess(
            source,
            RangeSyntax.NuGet,
            VersionRange.CreateSingleComparator(comparator));
    }

    internal VersionRangeParseResult ParseIntervalRange()
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
            return Failure(NuGetRangeDiagnosticCodes.InvalidDelimiter, "Expected NuGet range closing delimiter.", index);
        }

        int firstEndIndex = index;

        if (Peek() == ',')
        {
            return ParseBoundedOrOpenInterval(openDelimiter, lowerKind, firstStartIndex, firstEndIndex);
        }

        return ParseExactInterval(openDelimiter, openDelimiterIndex, firstStartIndex, firstEndIndex);
    }

    internal VersionRangeParseResult ParseExactInterval(char openDelimiter, int openDelimiterIndex, int versionStartIndex, int versionEndIndex)
    {
        char closeDelimiter = Peek();
        int closeDelimiterIndex = index;
        Advance();
        SkipWhitespace();

        if (!IsAtEnd())
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidDelimiter, "Unexpected content after NuGet range.", index);
        }

        if (openDelimiter != '[' || closeDelimiter != ']')
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "NuGet exact ranges must use inclusive square brackets.", openDelimiterIndex);
        }

        string versionText = GetTrimmedText(versionStartIndex, versionEndIndex);
        int diagnosticIndex = GetTrimmedStartIndex(versionStartIndex, versionEndIndex);
        if (versionText.Length == 0)
        {
            return Failure(NuGetRangeDiagnosticCodes.MissingVersion, "Expected NuGet exact range version.", closeDelimiterIndex);
        }

        VersionParseResult versionResult = NuGetVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidVersion, "Invalid NuGet exact range version.", diagnosticIndex);
        }

        VersionComparator comparator = VersionComparator.CreateEqual(
            versionResult.GetVersion(),
            VersionComparisonMode.Precedence);
        return VersionRangeParseResult.CreateSuccess(
            source,
            RangeSyntax.NuGet,
            VersionRange.CreateSingleComparator(comparator));
    }

    internal VersionRangeParseResult ParseBoundedOrOpenInterval(
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
            return Failure(NuGetRangeDiagnosticCodes.InvalidDelimiter, "Expected NuGet range closing delimiter.", index);
        }

        int upperEndIndex = index;
        char closeDelimiter = Peek();
        int closeDelimiterIndex = index;
        BoundKind upperKind = GetBoundKind(closeDelimiter);
        Advance();
        SkipWhitespace();

        if (!IsAtEnd())
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidDelimiter, "Unexpected content after NuGet range.", index);
        }

        string lowerText = GetTrimmedText(lowerStartIndex, lowerEndIndex);
        string upperText = GetTrimmedText(upperStartIndex, upperEndIndex);
        int lowerDiagnosticIndex = GetTrimmedStartIndex(lowerStartIndex, lowerEndIndex);
        int upperDiagnosticIndex = GetTrimmedStartIndex(upperStartIndex, upperEndIndex);

        if (lowerText.Length == 0 && upperText.Length == 0)
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "NuGet range must contain at least one bound.", closeDelimiterIndex);
        }

        if (lowerText.Length == 0 && openDelimiter == '[')
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "Unbounded NuGet lower ranges must use an exclusive lower delimiter.", lowerDiagnosticIndex);
        }

        if (upperText.Length == 0 && closeDelimiter == ']')
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "Unbounded NuGet upper ranges must use an exclusive upper delimiter.", closeDelimiterIndex);
        }

        UniversalVersion? lowerVersion = null;
        UniversalVersion? upperVersion = null;

        if (lowerText.Length > 0)
        {
            VersionParseResult lowerResult = NuGetVersionParser.Parse(lowerText);
            if (lowerResult.IsFailure())
            {
                return Failure(NuGetRangeDiagnosticCodes.InvalidVersion, "Invalid NuGet lower range version.", lowerDiagnosticIndex);
            }

            lowerVersion = lowerResult.GetVersion();
        }

        if (upperText.Length > 0)
        {
            VersionParseResult upperResult = NuGetVersionParser.Parse(upperText);
            if (upperResult.IsFailure())
            {
                return Failure(NuGetRangeDiagnosticCodes.InvalidVersion, "Invalid NuGet upper range version.", upperDiagnosticIndex);
            }

            upperVersion = upperResult.GetVersion();
        }

        return CreateIntervalRange(lowerKind, lowerVersion, upperKind, upperVersion, closeDelimiterIndex);
    }

    internal VersionRangeParseResult CreateIntervalRange(
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
                return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "NuGet range must contain at least one bound.", diagnosticIndex);
            }

            VersionInterval upperInterval = VersionInterval.CreateUpperBound(upperKind, upperVersion, comparisonMode);
            return CreateSuccessFromInterval(upperInterval);
        }

        if (upperVersion == null)
        {
            VersionInterval lowerInterval = VersionInterval.CreateLowerBound(lowerKind, lowerVersion, comparisonMode);
            return CreateSuccessFromInterval(lowerInterval);
        }

        int comparison = lowerVersion.CompareToVersion(upperVersion, comparisonMode);
        if (comparison > 0)
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "NuGet range lower bound cannot be greater than upper bound.", diagnosticIndex);
        }

        if (comparison == 0 && (lowerKind == BoundKind.Exclusive || upperKind == BoundKind.Exclusive))
        {
            return Failure(NuGetRangeDiagnosticCodes.InvalidRange, "NuGet range excludes its only possible bound version.", diagnosticIndex);
        }

        VersionInterval interval = VersionInterval.CreateBounded(
            lowerKind,
            lowerVersion,
            upperKind,
            upperVersion,
            comparisonMode);
        return CreateSuccessFromInterval(interval);
    }

    internal VersionRangeParseResult CreateSuccessFromInterval(VersionInterval interval)
    {
        return VersionRangeParseResult.CreateSuccess(
            source,
            RangeSyntax.NuGet,
            VersionRange.CreateSingleSet(interval.ToComparatorSet()));
    }

    internal BoundKind GetBoundKind(char delimiter)
    {
        if (delimiter == '[' || delimiter == ']')
        {
            return BoundKind.Inclusive;
        }

        return BoundKind.Exclusive;
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

    internal bool ContainsStar(string text)
    {
        int textIndex = 0;

        while (textIndex < text.Length)
        {
            if (text[textIndex] == '*')
            {
                return true;
            }

            textIndex = textIndex + 1;
        }

        return false;
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
        failureResult = VersionRangeParseResult.CreateFailure(
            source,
            RangeSyntax.NuGet,
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
