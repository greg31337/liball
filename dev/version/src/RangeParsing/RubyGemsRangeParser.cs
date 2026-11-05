namespace version;

internal class RubyGemsRangeParser
{
    internal string source;
    internal int index;
    internal bool failed;
    internal VersionRangeParseResult? failureResult;

    internal RubyGemsRangeParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failed = false;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string text)
    {
        RubyGemsRangeParser parser = new RubyGemsRangeParser(text);
        return parser.ParseRequirement();
    }

    internal VersionRangeParseResult ParseRequirement()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(RubyGemsRequirementDiagnosticCodes.MissingRequirement, "Expected RubyGems requirement.", index);
        }

        System.Collections.Generic.List<VersionComparator> comparators = new System.Collections.Generic.List<VersionComparator>();

        while (!IsAtEnd())
        {
            ReadComparatorInto(comparators);
            if (failed)
            {
                return GetFailureResult();
            }

            SkipWhitespace();
            if (IsAtEnd())
            {
                break;
            }

            if (Peek() != ',')
            {
                return Failure(RubyGemsRequirementDiagnosticCodes.InvalidOperator, "Expected comma between RubyGems requirements.", index);
            }

            Advance();
            SkipWhitespace();

            if (IsAtEnd() || Peek() == ',')
            {
                return Failure(RubyGemsRequirementDiagnosticCodes.MissingRequirement, "Expected RubyGems requirement after comma.", index);
            }
        }

        VersionComparatorSet set = new VersionComparatorSet(comparators.ToArray());
        return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.RubyGems, VersionRange.CreateSingleSet(set));
    }

    internal void ReadComparatorInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (Peek() == '~')
        {
            ReadPessimisticRequirementInto(comparators);
            return;
        }

        int operatorStartIndex = index;
        RangeOperator rangeOperator = ReadOperator();

        if (failed)
        {
            return;
        }

        if (index == operatorStartIndex && !VersionChar.IsDigit(Peek()))
        {
            Fail(RubyGemsRequirementDiagnosticCodes.InvalidOperator, "Expected RubyGems requirement operator or version.", operatorStartIndex);
            return;
        }

        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(RubyGemsRequirementDiagnosticCodes.MissingVersion, "Expected RubyGems requirement version.", versionStartIndex);
            return;
        }

        VersionParseResult versionResult = RubyGemsVersionParser.ParseUniversal(versionText);
        if (versionResult.IsFailure())
        {
            Fail(RubyGemsRequirementDiagnosticCodes.InvalidVersion, "Invalid RubyGems requirement version.", versionStartIndex);
            return;
        }

        comparators.Add(CreateComparator(rangeOperator, versionResult.GetVersion()));
    }

    internal void ReadPessimisticRequirementInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        int operatorStartIndex = index;
        Advance();
        if (Peek() != '>')
        {
            Fail(RubyGemsRequirementDiagnosticCodes.InvalidOperator, "Expected RubyGems pessimistic requirement operator.", operatorStartIndex);
            return;
        }

        Advance();
        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(RubyGemsRequirementDiagnosticCodes.MissingVersion, "Expected RubyGems pessimistic requirement version.", versionStartIndex);
            return;
        }

        RubyGemsVersionParseResult rubyGemsVersionResult = RubyGemsVersionParser.Parse(versionText);
        if (rubyGemsVersionResult.IsFailure())
        {
            Fail(RubyGemsRequirementDiagnosticCodes.InvalidVersion, "Invalid RubyGems pessimistic requirement version.", versionStartIndex);
            return;
        }

        VersionParseResult lowerResult = RubyGemsVersionParser.ParseUniversal(versionText);
        VersionParseResult upperResult = RubyGemsVersionParser.ParseUniversal(GetPessimisticUpperVersionText(rubyGemsVersionResult.GetVersion()));

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(lowerResult.GetVersion(), VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(upperResult.GetVersion(), VersionComparisonMode.Precedence));
    }

    internal RangeOperator ReadOperator()
    {
        if (Peek() == '<')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return RangeOperator.LessThanOrEqual;
            }

            return RangeOperator.LessThan;
        }

        if (Peek() == '>')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return RangeOperator.GreaterThanOrEqual;
            }

            return RangeOperator.GreaterThan;
        }

        if (Peek() == '=')
        {
            Advance();
            return RangeOperator.Equal;
        }

        if (Peek() == '!')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return RangeOperator.NotEqual;
            }

            Fail(RubyGemsRequirementDiagnosticCodes.InvalidOperator, "Unsupported RubyGems requirement operator.", index - 1);
            return RangeOperator.Equal;
        }

        return RangeOperator.Equal;
    }

    internal VersionComparator CreateComparator(RangeOperator rangeOperator, UniversalVersion version)
    {
        if (rangeOperator == RangeOperator.Equal)
        {
            return VersionComparator.CreateEqual(version, VersionComparisonMode.Precedence);
        }

        if (rangeOperator == RangeOperator.NotEqual)
        {
            return VersionComparator.CreateNotEqual(version, VersionComparisonMode.Precedence);
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            return VersionComparator.CreateGreaterThan(version, VersionComparisonMode.Precedence);
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            return VersionComparator.CreateGreaterThanOrEqual(version, VersionComparisonMode.Precedence);
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            return VersionComparator.CreateLessThan(version, VersionComparisonMode.Precedence);
        }

        return VersionComparator.CreateLessThanOrEqual(version, VersionComparisonMode.Precedence);
    }

    internal string ReadVersionText()
    {
        int startIndex = index;
        int endIndex = index;

        while (!IsAtEnd() && !VersionChar.IsWhitespace(Peek()) && Peek() != ',')
        {
            Advance();
            endIndex = index;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(source[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return source.Substring(startIndex, endIndex - startIndex);
    }

    internal string GetPessimisticUpperVersionText(RubyGemsVersion version)
    {
        RubyGemsVersionSegment[] segments = version.GetSegments();
        int numericCount = CountLeadingNumericSegments(segments);
        string[] upperSegments = new string[GetUpperSegmentCount(numericCount)];
        int index = 0;

        while (index < upperSegments.Length)
        {
            if (index == upperSegments.Length - 1)
            {
                upperSegments[index] = IncrementNumericText(segments[index].GetText());
            }
            else
            {
                upperSegments[index] = segments[index].GetText();
            }

            index = index + 1;
        }

        return JoinDotSeparated(upperSegments);
    }

    internal int CountLeadingNumericSegments(RubyGemsVersionSegment[] segments)
    {
        int count = 0;

        while (count < segments.Length && segments[count].IsNumeric())
        {
            count = count + 1;
        }

        return count;
    }

    internal int GetUpperSegmentCount(int numericCount)
    {
        if (numericCount > 1)
        {
            return numericCount - 1;
        }

        return 1;
    }

    internal string JoinDotSeparated(string[] segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < segments.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(segments[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string IncrementNumericText(string text)
    {
        string normalized = VersionFormatter.NormalizeNumericText(text);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int carry = 1;
        int index = normalized.Length - 1;

        while (index >= 0)
        {
            int digit = (normalized[index] - '0') + carry;
            if (digit == 10)
            {
                builder.Insert(0, '0');
                carry = 1;
            }
            else
            {
                builder.Insert(0, (char)('0' + digit));
                carry = 0;
            }

            index = index - 1;
        }

        if (carry == 1)
        {
            builder.Insert(0, '1');
        }

        return builder.ToString();
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
            RangeSyntax.RubyGems,
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
