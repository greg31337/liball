namespace version;

internal class NodePrimitiveComparatorParser
{
    internal string source;
    internal int index;

    internal NodePrimitiveComparatorParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        index = 0;
    }

    internal static VersionComparatorParseResult Parse(string? text)
    {
        if (text == null)
        {
            return NodeSemVerInputGuard.CreateComparatorNullFailure();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerInputGuard.CreateComparatorLengthFailure(text);
        }

        NodePrimitiveComparatorParser parser = new NodePrimitiveComparatorParser(text);
        return parser.ParseComparator();
    }

    internal VersionComparatorParseResult ParseComparator()
    {
        SkipWhitespace();

        if (IsAtEnd())
        {
            return Failure(NodeRangeDiagnosticCodes.MissingComparator, "Expected npm comparator.", index);
        }

        RangeOperator rangeOperator = ReadOperator();
        if (rangeOperator == RangeOperator.NotEqual)
        {
            return Failure(NodeRangeDiagnosticCodes.InvalidOperator, "Unsupported npm comparator operator.", index);
        }

        SkipWhitespace();
        int versionStartIndex = index;
        string versionText = ReadRemainingTrimmedText();

        if (versionText.Length == 0)
        {
            return Failure(NodeRangeDiagnosticCodes.MissingVersion, "Expected npm comparator version.", versionStartIndex);
        }

        VersionParseResult versionResult = SemVerParser.ParseLoose(versionText);
        if (versionResult.IsFailure())
        {
            return Failure(NodeRangeDiagnosticCodes.InvalidVersion, "Invalid npm comparator version.", versionStartIndex);
        }

        VersionComparator comparator = new VersionComparator(rangeOperator, versionResult.GetVersion(), VersionComparisonMode.Precedence);
        return VersionComparatorParseResult.CreateSuccess(source, RangeSyntax.NodeSemVer, comparator);
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

    internal string ReadRemainingTrimmedText()
    {
        int startIndex = index;
        int endIndex = source.Length;

        while (endIndex > startIndex && VersionChar.IsWhitespace(source[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        index = source.Length;
        return source.Substring(startIndex, endIndex - startIndex);
    }

    internal void SkipWhitespace()
    {
        while (!IsAtEnd() && VersionChar.IsWhitespace(Peek()))
        {
            Advance();
        }
    }

    internal VersionComparatorParseResult Failure(string code, string message, int diagnosticIndex)
    {
        return VersionComparatorParseResult.CreateFailure(
            source,
            RangeSyntax.NodeSemVer,
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
