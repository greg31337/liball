namespace version;

internal class NodeRangeParser
{
    internal string source;
    internal int index;
    internal bool failed;
    internal VersionRangeParseResult? failureResult;

    internal NodeRangeParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
        failed = false;
        failureResult = null;
    }

    internal static VersionRangeParseResult Parse(string? text)
    {
        if (text == null)
        {
            return NodeSemVerInputGuard.CreateRangeNullFailure();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerInputGuard.CreateRangeLengthFailure(text);
        }

        NodeRangeParser parser = new NodeRangeParser(text);
        return parser.ParseRange();
    }

    internal VersionRangeParseResult ParseRange()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.NodeSemVer, VersionRange.CreateUniversal());
        }

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();

        while (!IsAtEnd())
        {
            VersionComparatorSet set = ParseComparatorSet();
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

            if (!IsPipePipe())
            {
                return Failure(NodeRangeDiagnosticCodes.InvalidOperator, "Expected npm range disjunction operator.", index);
            }

            Advance();
            Advance();
            SkipWhitespace();

            if (IsAtEnd())
            {
                return Failure(NodeRangeDiagnosticCodes.MissingComparator, "Expected npm comparator after disjunction.", index);
            }
        }

        return VersionRangeParseResult.CreateSuccess(source, RangeSyntax.NodeSemVer, new VersionRange(sets.ToArray()));
    }

    internal VersionComparatorSet ParseComparatorSet()
    {
        System.Collections.Generic.List<VersionComparator> comparators = new System.Collections.Generic.List<VersionComparator>();
        bool matchedComparator = false;

        while (!IsAtEnd() && !IsPipePipe())
        {
            SkipWhitespace();

            if (IsAtEnd() || IsPipePipe())
            {
                break;
            }

            if (ReadCaretRangeInto(comparators))
            {
                matchedComparator = true;

                if (failed)
                {
                    return new VersionComparatorSet(new VersionComparator[0]);
                }

                SkipWhitespace();
                continue;
            }

            if (ReadTildeRangeInto(comparators))
            {
                matchedComparator = true;

                if (failed)
                {
                    return new VersionComparatorSet(new VersionComparator[0]);
                }

                SkipWhitespace();
                continue;
            }

            if (ReadHyphenRangeInto(comparators))
            {
                matchedComparator = true;

                if (failed)
                {
                    return new VersionComparatorSet(new VersionComparator[0]);
                }

                SkipWhitespace();
                continue;
            }

            int comparatorStartIndex = index;
            string comparatorText = ReadComparatorText();

            if (AddPartialRangeComparators(comparatorText, comparatorStartIndex, comparators))
            {
                matchedComparator = true;

                if (failed)
                {
                    return new VersionComparatorSet(new VersionComparator[0]);
                }

                SkipWhitespace();
                continue;
            }

            VersionComparatorParseResult comparatorResult = NodePrimitiveComparatorParser.Parse(comparatorText);
            if (comparatorResult.IsFailure())
            {
                VersionDiagnostic diagnostic = comparatorResult.GetDiagnostic(0);
                Fail(diagnostic.GetCode(), diagnostic.GetMessage(), comparatorStartIndex + diagnostic.GetIndex());
                return new VersionComparatorSet(new VersionComparator[0]);
            }

            comparators.Add(comparatorResult.GetComparator());
            matchedComparator = true;
            SkipWhitespace();
        }

        if (comparators.Count == 0 && !matchedComparator)
        {
            Fail(NodeRangeDiagnosticCodes.MissingComparator, "Expected npm comparator.", index);
            return new VersionComparatorSet(new VersionComparator[0]);
        }

        return new VersionComparatorSet(comparators.ToArray());
    }

    internal bool ReadCaretRangeInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (Peek() != '^')
        {
            return false;
        }

        Advance();
        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadTokenText();

        if (versionText.Length == 0)
        {
            Fail(NodeRangeDiagnosticCodes.MissingVersion, "Expected npm caret range version.", versionStartIndex);
            return true;
        }

        NodePartialVersion partial = NodePartialVersion.Parse(versionText);
        if (partial.IsRange())
        {
            if (!partial.IsValid())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm caret range version.", versionStartIndex);
                return true;
            }

            if (!partial.IsAny())
            {
                comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetLowerVersionText(partial)), VersionComparisonMode.Precedence));
                comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetCaretUpperPartialVersionText(partial)), VersionComparisonMode.Precedence));
            }

            return true;
        }

        VersionParseResult versionResult = SemVerParser.ParseLoose(versionText);
        if (versionResult.IsFailure())
        {
            Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm caret range version.", versionStartIndex);
            return true;
        }

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(versionResult.GetVersion(), VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetCaretUpperVersionText(versionResult.GetVersion())), VersionComparisonMode.Precedence));
        return true;
    }

    internal bool ReadTildeRangeInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (Peek() != '~')
        {
            return false;
        }

        Advance();
        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadTokenText();

        if (versionText.Length == 0)
        {
            Fail(NodeRangeDiagnosticCodes.MissingVersion, "Expected npm tilde range version.", versionStartIndex);
            return true;
        }

        NodePartialVersion partial = NodePartialVersion.Parse(versionText);
        if (partial.IsRange())
        {
            if (!partial.IsValid())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm tilde range version.", versionStartIndex);
                return true;
            }

            if (!partial.IsAny())
            {
                comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetLowerVersionText(partial)), VersionComparisonMode.Precedence));
                comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetUpperVersionText(partial)), VersionComparisonMode.Precedence));
            }

            return true;
        }

        VersionParseResult versionResult = SemVerParser.ParseLoose(versionText);
        if (versionResult.IsFailure())
        {
            Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm tilde range version.", versionStartIndex);
            return true;
        }

        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(versionResult.GetVersion(), VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetTildeUpperVersionText(versionResult.GetVersion())), VersionComparisonMode.Precedence));
        return true;
    }

    internal bool AddPartialRangeComparators(string comparatorText, int diagnosticOffset, System.Collections.Generic.List<VersionComparator> comparators)
    {
        int[] textIndex = new int[] { 0 };
        SkipTextWhitespace(comparatorText, textIndex);

        RangeOperator rangeOperator = ReadTextOperator(comparatorText, textIndex);
        if (rangeOperator == RangeOperator.NotEqual)
        {
            return false;
        }

        SkipTextWhitespace(comparatorText, textIndex);
        int versionStartIndex = textIndex[0];
        string versionText = TrimTextEnd(comparatorText.Substring(versionStartIndex));

        if (versionText.Length == 0)
        {
            return false;
        }

        NodePartialVersion partial = NodePartialVersion.Parse(versionText);
        if (!partial.IsRange())
        {
            return false;
        }

        if (!partial.IsValid())
        {
            Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm partial range version.", diagnosticOffset + versionStartIndex);
            return true;
        }

        AddPartialComparators(rangeOperator, partial, comparators);
        return true;
    }

    internal void AddPartialComparators(RangeOperator rangeOperator, NodePartialVersion partial, System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (partial.IsAny())
        {
            AddAnyComparators(rangeOperator, comparators);
            return;
        }

        if (rangeOperator == RangeOperator.Equal)
        {
            AddEqualPartialComparators(partial, comparators);
            return;
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetLowerVersionText(partial)), VersionComparisonMode.Precedence));
            return;
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetUpperVersionText(partial)), VersionComparisonMode.Precedence));
            return;
        }

        if (rangeOperator == RangeOperator.LessThanOrEqual)
        {
            comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetUpperVersionText(partial)), VersionComparisonMode.Precedence));
            return;
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetLowerVersionText(partial) + "-0"), VersionComparisonMode.Precedence));
        }
    }

    internal void AddAnyComparators(RangeOperator rangeOperator, System.Collections.Generic.List<VersionComparator> comparators)
    {
        if (rangeOperator == RangeOperator.GreaterThan || rangeOperator == RangeOperator.LessThan)
        {
            comparators.Add(VersionComparator.CreateGreaterThan(CreateNodeVersion("0.0.0"), VersionComparisonMode.Precedence));
            comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion("0.0.0"), VersionComparisonMode.Precedence));
        }
    }

    internal void AddEqualPartialComparators(NodePartialVersion partial, System.Collections.Generic.List<VersionComparator> comparators)
    {
        comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetLowerVersionText(partial)), VersionComparisonMode.Precedence));
        comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetUpperVersionText(partial)), VersionComparisonMode.Precedence));
    }

    internal string GetLowerVersionText(NodePartialVersion partial)
    {
        if (partial.IsMajorOnly())
        {
            return partial.GetMajor() + ".0.0";
        }

        if (partial.IsMinorOnly())
        {
            return partial.GetMajor() + "." + partial.GetMinor() + ".0";
        }

        return partial.GetMajor() + "." + partial.GetMinor() + "." + partial.GetPatch();
    }

    internal string GetUpperVersionText(NodePartialVersion partial)
    {
        if (partial.IsMajorOnly())
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0-0";
        }

        return partial.GetMajor() + "." + IncrementNumericText(partial.GetMinor()) + ".0-0";
    }

    internal string GetTildeUpperVersionText(UniversalVersion version)
    {
        return version.GetNumericComponent(0) + "." + IncrementNumericText(version.GetNumericComponent(1)) + ".0-0";
    }

    internal string GetCaretUpperPartialVersionText(NodePartialVersion partial)
    {
        if (!IsZeroNumericText(partial.GetMajor()))
        {
            return IncrementNumericText(partial.GetMajor()) + ".0.0-0";
        }

        if (partial.IsMajorOnly())
        {
            return "1.0.0-0";
        }

        if (!IsZeroNumericText(partial.GetMinor()))
        {
            return "0." + IncrementNumericText(partial.GetMinor()) + ".0-0";
        }

        if (partial.IsMinorOnly())
        {
            return "0.1.0-0";
        }

        return "0.0." + IncrementNumericText(partial.GetPatch()) + "-0";
    }

    internal string GetCaretUpperVersionText(UniversalVersion version)
    {
        if (!IsZeroNumericText(version.GetNumericComponent(0)))
        {
            return IncrementNumericText(version.GetNumericComponent(0)) + ".0.0-0";
        }

        if (!IsZeroNumericText(version.GetNumericComponent(1)))
        {
            return "0." + IncrementNumericText(version.GetNumericComponent(1)) + ".0-0";
        }

        return "0.0." + IncrementNumericText(version.GetNumericComponent(2)) + "-0";
    }

    internal bool ReadHyphenRangeInto(System.Collections.Generic.List<VersionComparator> comparators)
    {
        int startIndex = index;
        int lowerStartIndex = index;
        string lowerText = ReadTokenText();

        if (lowerText.Length == 0)
        {
            index = startIndex;
            return false;
        }

        int afterLowerIndex = index;
        SkipWhitespace();

        if (afterLowerIndex == index)
        {
            index = startIndex;
            return false;
        }

        if (Peek() != '-')
        {
            index = startIndex;
            return false;
        }

        Advance();

        if (IsAtEnd() || !VersionChar.IsWhitespace(Peek()))
        {
            index = startIndex;
            return false;
        }

        SkipWhitespace();
        int upperStartIndex = index;
        string upperText = ReadTokenText();

        if (upperText.Length == 0)
        {
            Fail(NodeRangeDiagnosticCodes.MissingVersion, "Expected upper npm hyphen range version.", upperStartIndex);
            return true;
        }

        NodePartialVersion lowerPartial = NodePartialVersion.Parse(lowerText);
        if (lowerPartial.IsRange())
        {
            if (!lowerPartial.IsValid())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid lower npm hyphen range version.", lowerStartIndex);
                return true;
            }

            if (!lowerPartial.IsAny())
            {
                comparators.Add(VersionComparator.CreateGreaterThanOrEqual(CreateNodeVersion(GetLowerVersionText(lowerPartial)), VersionComparisonMode.Precedence));
            }
        }
        else
        {
            VersionParseResult lowerResult = SemVerParser.ParseLoose(lowerText);
            if (lowerResult.IsFailure())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid lower npm hyphen range version.", lowerStartIndex);
                return true;
            }

            comparators.Add(VersionComparator.CreateGreaterThanOrEqual(lowerResult.GetVersion(), VersionComparisonMode.Precedence));
        }

        NodePartialVersion upperPartial = NodePartialVersion.Parse(upperText);
        if (upperPartial.IsRange())
        {
            if (!upperPartial.IsValid())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid upper npm hyphen range version.", upperStartIndex);
                return true;
            }

            if (!upperPartial.IsAny())
            {
                comparators.Add(VersionComparator.CreateLessThan(CreateNodeVersion(GetUpperVersionText(upperPartial)), VersionComparisonMode.Precedence));
            }
        }
        else
        {
            VersionParseResult upperResult = SemVerParser.ParseLoose(upperText);
            if (upperResult.IsFailure())
            {
                Fail(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid upper npm hyphen range version.", upperStartIndex);
                return true;
            }

            comparators.Add(VersionComparator.CreateLessThanOrEqual(upperResult.GetVersion(), VersionComparisonMode.Precedence));
        }

        return true;
    }

    internal string ReadComparatorText()
    {
        int startIndex = index;

        if (Peek() == '<' || Peek() == '>' || Peek() == '=')
        {
            Advance();
            if (Peek() == '=')
            {
                Advance();
            }

            SkipWhitespace();
        }

        while (!IsAtEnd() && !VersionChar.IsWhitespace(Peek()) && Peek() != '|')
        {
            Advance();
        }

        return source.Substring(startIndex, index - startIndex);
    }

    internal string ReadTokenText()
    {
        int startIndex = index;

        while (!IsAtEnd() && !VersionChar.IsWhitespace(Peek()) && Peek() != '|')
        {
            Advance();
        }

        return source.Substring(startIndex, index - startIndex);
    }

    internal RangeOperator ReadTextOperator(string text, int[] textIndex)
    {
        if (textIndex[0] >= text.Length)
        {
            return RangeOperator.Equal;
        }

        if (text[textIndex[0]] == '<')
        {
            textIndex[0] = textIndex[0] + 1;
            if (textIndex[0] < text.Length && text[textIndex[0]] == '=')
            {
                textIndex[0] = textIndex[0] + 1;
                return RangeOperator.LessThanOrEqual;
            }

            return RangeOperator.LessThan;
        }

        if (text[textIndex[0]] == '>')
        {
            textIndex[0] = textIndex[0] + 1;
            if (textIndex[0] < text.Length && text[textIndex[0]] == '=')
            {
                textIndex[0] = textIndex[0] + 1;
                return RangeOperator.GreaterThanOrEqual;
            }

            return RangeOperator.GreaterThan;
        }

        if (text[textIndex[0]] == '=')
        {
            textIndex[0] = textIndex[0] + 1;
            return RangeOperator.Equal;
        }

        if (text[textIndex[0]] == '!')
        {
            return RangeOperator.NotEqual;
        }

        return RangeOperator.Equal;
    }

    internal void SkipTextWhitespace(string text, int[] textIndex)
    {
        while (textIndex[0] < text.Length && VersionChar.IsWhitespace(text[textIndex[0]]))
        {
            textIndex[0] = textIndex[0] + 1;
        }
    }

    internal string TrimTextEnd(string text)
    {
        int endIndex = text.Length;

        while (endIndex > 0 && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(0, endIndex);
    }

    internal UniversalVersion CreateNodeVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException("Internal npm range desugaring produced an invalid version.");
        }

        return result.GetVersion();
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

    internal bool IsPipePipe()
    {
        if (index + 1 >= source.Length)
        {
            return false;
        }

        return source[index] == '|' && source[index + 1] == '|';
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
            RangeSyntax.NodeSemVer,
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
