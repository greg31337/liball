namespace version;

internal class PythonSpecifierParser
{
    internal string source;
    internal int index;
    internal bool failed;
    internal bool prereleasesAllowedByDefault;
    internal VersionRangeParseResult? failureResult;

    internal PythonSpecifierParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failed = false;
        prereleasesAllowedByDefault = false;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string text)
    {
        PythonSpecifierParser parser = new PythonSpecifierParser(text);
        return parser.ParseSpecifierSet();
    }

    internal VersionRangeParseResult ParseSpecifierSet()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(PythonSpecifierDiagnosticCodes.MissingSpecifier, "Expected Python version specifier.", index);
        }

        VersionRange range = VersionRange.CreateUniversal();

        while (!IsAtEnd())
        {
            VersionRange specifierRange = ReadSpecifier();
            if (failed)
            {
                return GetFailureResult();
            }

            range = IntersectRanges(range, specifierRange);
            SkipWhitespace();

            if (IsAtEnd())
            {
                break;
            }

            if (Peek() != ',')
            {
                return Failure(PythonSpecifierDiagnosticCodes.InvalidOperator, "Expected comma between Python specifiers.", index);
            }

            Advance();
            SkipWhitespace();

            if (IsAtEnd() || Peek() == ',')
            {
                return Failure(PythonSpecifierDiagnosticCodes.MissingSpecifier, "Expected Python specifier after comma.", index);
            }
        }

        return VersionRangeParseResult.CreateSuccess(
            source,
            RangeSyntax.Python,
            VersionRange.CreateWithPrereleaseAdmission(range.GetSets(), prereleasesAllowedByDefault));
    }

    internal VersionRange ReadSpecifier()
    {
        int operatorStartIndex = index;
        string operatorText = ReadOperator();

        if (operatorText.Length == 0)
        {
            Fail(PythonSpecifierDiagnosticCodes.InvalidOperator, "Expected Python specifier operator.", operatorStartIndex);
            return VersionRange.CreateEmpty();
        }

        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(PythonSpecifierDiagnosticCodes.MissingVersion, "Expected Python specifier version.", versionStartIndex);
            return VersionRange.CreateEmpty();
        }

        if (ContainsStar(versionText))
        {
            return ReadWildcardSpecifier(operatorText, versionText, versionStartIndex);
        }

        VersionParseResult versionResult = PythonVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            Fail(PythonSpecifierDiagnosticCodes.InvalidVersion, "Invalid Python specifier version.", versionStartIndex);
            return VersionRange.CreateEmpty();
        }

        if (versionResult.GetVersion().IsPrerelease())
        {
            prereleasesAllowedByDefault = true;
        }

        if (operatorText == "~=")
        {
            return CreateCompatibleReleaseRange(versionText, versionStartIndex);
        }

        return VersionRange.CreateSingleComparator(CreateComparator(operatorText, versionResult.GetVersion()));
    }

    internal VersionRange ReadWildcardSpecifier(string operatorText, string versionText, int versionStartIndex)
    {
        if (operatorText != "==" && operatorText != "!=")
        {
            Fail(PythonSpecifierDiagnosticCodes.InvalidOperator, "Python wildcard specifiers require == or !=.", versionStartIndex);
            return VersionRange.CreateEmpty();
        }

        string[] prefixSegments = ParseWildcardPrefix(versionText);
        if (prefixSegments.Length == 0)
        {
            Fail(PythonSpecifierDiagnosticCodes.InvalidVersion, "Invalid Python wildcard specifier version.", versionStartIndex);
            return VersionRange.CreateEmpty();
        }

        UniversalVersion lowerVersion = ParseRequiredVersion(JoinDotSeparated(prefixSegments));
        UniversalVersion upperVersion = ParseRequiredVersion(GetWildcardUpperVersionText(prefixSegments));

        VersionComparator lowerComparator = VersionComparator.CreateGreaterThanOrEqual(lowerVersion, VersionComparisonMode.Precedence);
        VersionComparator upperComparator = VersionComparator.CreateLessThan(upperVersion, VersionComparisonMode.Precedence);

        if (operatorText == "==")
        {
            return VersionRange.CreateSingleSet(new VersionComparatorSet(new VersionComparator[] { lowerComparator, upperComparator }));
        }

        VersionComparatorSet lowerSet = VersionComparatorSet.CreateSingle(VersionComparator.CreateLessThan(lowerVersion, VersionComparisonMode.Precedence));
        VersionComparatorSet upperSet = VersionComparatorSet.CreateSingle(VersionComparator.CreateGreaterThanOrEqual(upperVersion, VersionComparisonMode.Precedence));
        return new VersionRange(new VersionComparatorSet[] { lowerSet, upperSet });
    }

    internal VersionRange CreateCompatibleReleaseRange(string versionText, int versionStartIndex)
    {
        PythonVersion version = PythonVersionParser.ParseVersion(versionText);
        if (version.GetReleaseSegmentCount() < 2 || version.HasLocalVersion())
        {
            Fail(PythonSpecifierDiagnosticCodes.InvalidVersion, "Invalid Python compatible release version.", versionStartIndex);
            return VersionRange.CreateEmpty();
        }

        UniversalVersion lowerVersion = ParseRequiredVersion(versionText);
        UniversalVersion upperVersion = ParseRequiredVersion(GetCompatibleUpperVersionText(version));

        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(lowerVersion, VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(upperVersion, VersionComparisonMode.Precedence)
        });

        return VersionRange.CreateSingleSet(set);
    }

    internal VersionRange IntersectRanges(VersionRange left, VersionRange right)
    {
        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();
        int leftIndex = 0;

        while (leftIndex < left.GetSetCount())
        {
            int rightIndex = 0;
            while (rightIndex < right.GetSetCount())
            {
                sets.Add(VersionComparatorSet.Intersect(left.GetSet(leftIndex), right.GetSet(rightIndex)));
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return new VersionRange(sets.ToArray());
    }

    internal VersionComparator CreateComparator(string operatorText, UniversalVersion version)
    {
        if (operatorText == "==")
        {
            return VersionComparator.CreateEqual(version, VersionComparisonMode.Precedence);
        }

        if (operatorText == "!=")
        {
            return VersionComparator.CreateNotEqual(version, VersionComparisonMode.Precedence);
        }

        if (operatorText == ">")
        {
            return VersionComparator.CreateGreaterThan(version, VersionComparisonMode.Precedence);
        }

        if (operatorText == ">=")
        {
            return VersionComparator.CreateGreaterThanOrEqual(version, VersionComparisonMode.Precedence);
        }

        if (operatorText == "<")
        {
            return VersionComparator.CreateLessThan(version, VersionComparisonMode.Precedence);
        }

        return VersionComparator.CreateLessThanOrEqual(version, VersionComparisonMode.Precedence);
    }

    internal string ReadOperator()
    {
        if (Peek() == '~')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return "~=";
            }

            return "";
        }

        if (Peek() == '=')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return "==";
            }

            return "";
        }

        if (Peek() == '!')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return "!=";
            }

            return "";
        }

        if (Peek() == '<')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return "<=";
            }

            return "<";
        }

        if (Peek() == '>')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
                return ">=";
            }

            return ">";
        }

        return "";
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

        return source.Substring(startIndex, endIndex - startIndex);
    }

    internal string[] ParseWildcardPrefix(string versionText)
    {
        string lower = versionText.ToLowerInvariant();
        int starIndex = lower.IndexOf('*');
        if (starIndex < 0 || starIndex != lower.Length - 1)
        {
            return new string[0];
        }

        if (starIndex < 2 || lower[starIndex - 1] != '.')
        {
            return new string[0];
        }

        string prefixText = lower.Substring(0, starIndex - 1);
        string[] prefixSegments = prefixText.Split('.');
        int index = 0;

        while (index < prefixSegments.Length)
        {
            if (prefixSegments[index].Length == 0 || !ContainsOnlyDigits(prefixSegments[index]))
            {
                return new string[0];
            }

            prefixSegments[index] = VersionFormatter.NormalizeNumericText(prefixSegments[index]);
            index = index + 1;
        }

        return prefixSegments;
    }

    internal string GetWildcardUpperVersionText(string[] prefixSegments)
    {
        string[] upperSegments = new string[prefixSegments.Length];
        int index = 0;

        while (index < prefixSegments.Length)
        {
            if (index == prefixSegments.Length - 1)
            {
                upperSegments[index] = IncrementNumericText(prefixSegments[index]);
            }
            else
            {
                upperSegments[index] = prefixSegments[index];
            }

            index = index + 1;
        }

        return JoinDotSeparated(upperSegments);
    }

    internal string GetCompatibleUpperVersionText(PythonVersion version)
    {
        string[] upperSegments = new string[version.GetReleaseSegmentCount() - 1];
        int index = 0;

        while (index < upperSegments.Length)
        {
            if (index == upperSegments.Length - 1)
            {
                upperSegments[index] = IncrementNumericText(version.GetReleaseSegment(index));
            }
            else
            {
                upperSegments[index] = version.GetReleaseSegment(index);
            }

            index = index + 1;
        }

        return JoinDotSeparated(upperSegments);
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

    internal UniversalVersion ParseRequiredVersion(string versionText)
    {
        VersionParseResult result = PythonVersionParser.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException("Internal Python specifier desugaring produced an invalid version.");
        }

        return result.GetVersion();
    }

    internal bool ContainsStar(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '*')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsOnlyDigits(string text)
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
            RangeSyntax.Python,
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
