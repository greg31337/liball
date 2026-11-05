namespace version;

internal class CargoRangeParser
{
    internal string source;
    internal int index;
    internal bool failed;
    internal VersionRangeParseResult? failureResult;

    internal CargoRangeParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failed = false;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string text)
    {
        CargoRangeParser parser = new CargoRangeParser(text);
        return parser.ParseRange();
    }

    internal VersionRangeParseResult ParseRange()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(CargoRangeDiagnosticCodes.MissingComparator, "Expected Cargo version requirement.", index);
        }

        System.Collections.Generic.List<VersionComparator> comparators = new System.Collections.Generic.List<VersionComparator>();

        while (!IsAtEnd())
        {
            ReadRequirementInto(comparators);
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
                return Failure(CargoRangeDiagnosticCodes.InvalidOperator, "Expected comma between Cargo version requirements.", index);
            }

            Advance();
            SkipWhitespace();

            if (IsAtEnd() || Peek() == ',')
            {
                return Failure(CargoRangeDiagnosticCodes.MissingComparator, "Expected Cargo version requirement after comma.", index);
            }
        }

        VersionComparatorSet set = new VersionComparatorSet(comparators.ToArray());
        return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.Cargo, VersionRange.CreateSingleSet(set));
    }

    internal void ReadRequirementInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (Peek() == '^')
        {
            ReadDefaultRequirementInto(comparators, true);
            return;
        }

        if (Peek() == '~')
        {
            ReadTildeRequirementInto(comparators);
            return;
        }

        if (VersionChar.IsDigit(Peek()) || IsWildcardCharacter(Peek()))
        {
            ReadDefaultRequirementInto(comparators, false);
            return;
        }

        ReadComparatorInto(comparators);
    }

    internal void ReadDefaultRequirementInto(System.Collections.Generic.List<VersionComparator> comparators, bool leadingCaret)
    {
        if (leadingCaret)
        {
            Advance();
            SkipWhitespace();
        }

        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(CargoRangeDiagnosticCodes.MissingVersion, "Expected Cargo default requirement version.", versionStartIndex);
            return;
        }

        CargoPartialVersion partial = CargoPartialVersion.Parse(versionText);
        if (!partial.IsValid())
        {
            Fail(CargoRangeDiagnosticCodes.InvalidVersion, "Invalid Cargo default requirement version.", versionStartIndex);
            return;
        }

        if (partial.IsWildcard())
        {
            if (leadingCaret)
            {
                Fail(CargoRangeDiagnosticCodes.InvalidVersion, "Invalid Cargo caret requirement version.", versionStartIndex);
                return;
            }

            AddWildcardComparators(partial, comparators);
            return;
        }

        UniversalVersion lowerVersion = CreateCargoVersionFromComponents(
            versionText,
            partial.GetMajor(),
            partial.GetMinor(),
            partial.GetPatch(),
            partial.GetPrereleaseIdentifiers(),
            partial.GetBuildIdentifiers(),
            partial.GetMissingComponentCount());
        UniversalVersion upperVersion = CreateCargoVersion(GetDefaultUpperVersionText(partial));

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(lowerVersion, VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(upperVersion, VersionComparisonMode.Precedence));
    }

    internal void ReadTildeRequirementInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        Advance();
        SkipWhitespace();

        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(CargoRangeDiagnosticCodes.MissingVersion, "Expected Cargo tilde requirement version.", versionStartIndex);
            return;
        }

        CargoPartialVersion partial = CargoPartialVersion.Parse(versionText);
        if (!partial.IsValid() || partial.IsWildcard())
        {
            Fail(CargoRangeDiagnosticCodes.InvalidVersion, "Invalid Cargo tilde requirement version.", versionStartIndex);
            return;
        }

        UniversalVersion lowerVersion = CreateCargoVersionFromComponents(
            versionText,
            partial.GetMajor(),
            partial.GetMinor(),
            partial.GetPatch(),
            partial.GetPrereleaseIdentifiers(),
            partial.GetBuildIdentifiers(),
            partial.GetMissingComponentCount());
        UniversalVersion upperVersion = CreateCargoVersion(GetTildeUpperVersionText(partial));

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(lowerVersion, VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(upperVersion, VersionComparisonMode.Precedence));
    }

    internal void ReadComparatorInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        int comparatorStartIndex = index;
        RangeOperator rangeOperator = ReadOperator();

        if (rangeOperator == RangeOperator.NotEqual)
        {
            Fail(CargoRangeDiagnosticCodes.InvalidOperator, "Unsupported Cargo comparator operator.", comparatorStartIndex);
            return;
        }

        if (index == comparatorStartIndex)
        {
            Fail(CargoRangeDiagnosticCodes.InvalidOperator, "Expected Cargo comparator operator.", comparatorStartIndex);
            return;
        }

        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadVersionText();

        if (versionText.Length == 0)
        {
            Fail(CargoRangeDiagnosticCodes.MissingVersion, "Expected Cargo comparator version.", versionStartIndex);
            return;
        }

        CargoPartialVersion partial = CargoPartialVersion.Parse(versionText);
        if (!partial.IsValid() || partial.IsWildcard())
        {
            Fail(CargoRangeDiagnosticCodes.InvalidVersion, "Invalid Cargo comparator version.", versionStartIndex);
            return;
        }

        AddComparisonComparators(rangeOperator, versionText, partial, comparators);
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
            return RangeOperator.NotEqual;
        }

        return RangeOperator.Equal;
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

    internal string GetDefaultUpperVersionText(CargoPartialVersion partial)
    {
        if (!IsZeroNumericText(partial.GetMajor()))
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0";
        }

        if (partial.GetMissingComponentCount() == 2)
        {
            return "1.0.0";
        }

        if (!IsZeroNumericText(partial.GetMinor()))
        {
            return "0." + IncrementNumericText(partial.GetMinor()) + ".0";
        }

        if (partial.GetMissingComponentCount() == 1)
        {
            return "0.1.0";
        }

        return "0.0." + IncrementNumericText(partial.GetPatch());
    }

    internal string GetTildeUpperVersionText(CargoPartialVersion partial)
    {
        if (partial.GetMissingComponentCount() == 2)
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0";
        }

        return partial.GetMajor() + "." + IncrementNumericText(partial.GetMinor()) + ".0";
    }

    internal void AddWildcardComparators(CargoPartialVersion partial, System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (partial.IsAny())
        {
            comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateCargoVersion("0.0.0"), VersionComparisonMode.Precedence));
            return;
        }

        UniversalVersion lowerVersion = CreateCargoVersion(
            partial.GetMajor() + "." + partial.GetMinor() + "." + partial.GetPatch());
        UniversalVersion upperVersion = CreateCargoVersion(GetWildcardUpperVersionText(partial));

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(lowerVersion, VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(upperVersion, VersionComparisonMode.Precedence));
    }

    internal string GetWildcardUpperVersionText(CargoPartialVersion partial)
    {
        if (partial.GetComponentCount() == 2)
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0";
        }

        return partial.GetMajor() + "." + IncrementNumericText(partial.GetMinor()) + ".0";
    }

    internal void AddComparisonComparators(
        RangeOperator rangeOperator,
        string versionText,
        CargoPartialVersion partial,
        System.Collections.Generic.List<VersionComparator> comparators)
    {
        UniversalVersion version = CreateCargoVersionFromComponents(
            versionText,
            partial.GetMajor(),
            partial.GetMinor(),
            partial.GetPatch(),
            partial.GetPrereleaseIdentifiers(),
            partial.GetBuildIdentifiers(),
            partial.GetMissingComponentCount());

        if (rangeOperator == RangeOperator.Equal)
        {
            AddEqualComparators(version, partial, comparators);
            return;
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            AddGreaterThanComparators(version, partial, comparators);
            return;
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            comparators.Add(VersionComparator.CreateGreaterThanOrEqual(version, VersionComparisonMode.Precedence));
            return;
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            comparators.Add(VersionComparator.CreateLessThan(version, VersionComparisonMode.Precedence));
            return;
        }

        AddLessThanOrEqualComparators(version, partial, comparators);
    }

    internal void AddEqualComparators(
        UniversalVersion version,
        CargoPartialVersion partial,
        System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (partial.GetMissingComponentCount() == 0)
        {
            comparators.Add(VersionComparator.CreateEqual(version, VersionComparisonMode.Precedence));
            return;
        }

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(version, VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(CreateCargoVersion(GetPartialUpperVersionText(partial)), VersionComparisonMode.Precedence));
    }

    internal void AddGreaterThanComparators(
        UniversalVersion version,
        CargoPartialVersion partial,
        System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (partial.GetMissingComponentCount() == 0)
        {
            comparators.Add(VersionComparator.CreateGreaterThan(version, VersionComparisonMode.Precedence));
            return;
        }

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateCargoVersion(GetPartialUpperVersionText(partial)), VersionComparisonMode.Precedence));
    }

    internal void AddLessThanOrEqualComparators(
        UniversalVersion version,
        CargoPartialVersion partial,
        System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (partial.GetMissingComponentCount() == 0)
        {
            comparators.Add(VersionComparator.CreateLessThanOrEqual(version, VersionComparisonMode.Precedence));
            return;
        }

        comparators.Add(VersionComparator.CreateLessThan(CreateCargoVersion(GetPartialUpperVersionText(partial)), VersionComparisonMode.Precedence));
    }

    internal string GetPartialUpperVersionText(CargoPartialVersion partial)
    {
        if (partial.GetMissingComponentCount() == 2)
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0";
        }

        return partial.GetMajor() + "." + IncrementNumericText(partial.GetMinor()) + ".0";
    }

    internal UniversalVersion CreateCargoVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException("Internal Cargo range desugaring produced an invalid version.");
        }

        UniversalVersion version = result.GetVersion();
        return CreateCargoVersionFromComponents(
            text,
            version.GetNumericComponent(0),
            version.GetNumericComponent(1),
            version.GetNumericComponent(2),
            version.GetPrereleaseIdentifiers(),
            version.GetBuildIdentifiers(),
            0);
    }

    internal UniversalVersion CreateCargoVersionFromComponents(
        string originalText,
        string major,
        string minor,
        string patch,
        VersionIdentifier[] prereleaseIdentifiers,
        VersionIdentifier[] buildIdentifiers,
        int missingComponentCount)
    {
        return new UniversalVersion(
            originalText,
            VersionDialect.Cargo,
            new string[] { major, minor, patch },
            prereleaseIdentifiers,
            buildIdentifiers,
            missingComponentCount,
            false,
            false,
            "",
            null);
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

    internal bool IsZeroNumericText(string text)
    {
        return VersionFormatter.NormalizeNumericText(text) == "0";
    }

    internal void SkipWhitespace()
    {
        while (!IsAtEnd() && VersionChar.IsWhitespace(Peek()))
        {
            Advance();
        }
    }

    internal bool IsWildcardCharacter(char value)
    {
        return VersionChar.IsStar(value) || VersionChar.IsX(value);
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
            RangeSyntax.Cargo,
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
