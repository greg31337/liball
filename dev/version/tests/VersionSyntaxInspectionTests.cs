using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionSyntaxInspectionTests
{
    [TestCase("", VersionTokenKind.End)]
    [TestCase("42", VersionTokenKind.NumericIdentifier)]
    [TestCase("alpha9", VersionTokenKind.TextIdentifier)]
    [TestCase(".", VersionTokenKind.Separator)]
    [TestCase(">=", VersionTokenKind.Operator)]
    [TestCase("*", VersionTokenKind.Wildcard)]
    [TestCase("[", VersionTokenKind.RangeDelimiter)]
    [TestCase("\t", VersionTokenKind.Whitespace)]
    [TestCase("@", VersionTokenKind.Unknown)]
    public void Tokenize_ClassifiesEveryPublicTokenKind(string text, VersionTokenKind expectedKind)
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();

        VersionToken[] tokens = inspection.Tokenize(text);

        Assert.That(tokens[0].kind, Is.EqualTo(expectedKind));
        Assert.That(tokens[tokens.Length - 1].kind, Is.EqualTo(VersionTokenKind.End));
    }

    [Test]
    public void Tokenize_ExposesTokensAndSourceLocationsThroughPublicData()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();

        VersionToken[] tokens = inspection.Tokenize("1.x\r\n>=2");

        Assert.That(tokens.Length, Is.EqualTo(7));
        AssertToken(tokens[0], VersionTokenKind.NumericIdentifier, "1", 0, 1, 1, 1);
        AssertToken(tokens[1], VersionTokenKind.Separator, ".", 1, 2, 1, 2);
        AssertToken(tokens[2], VersionTokenKind.Wildcard, "x", 2, 3, 1, 3);
        AssertToken(tokens[3], VersionTokenKind.Whitespace, "\r\n", 3, 5, 1, 4);
        AssertToken(tokens[4], VersionTokenKind.Operator, ">=", 5, 7, 2, 1);
        AssertToken(tokens[5], VersionTokenKind.NumericIdentifier, "2", 7, 8, 2, 3);
        AssertToken(tokens[6], VersionTokenKind.End, "", 8, 8, 2, 4);
    }

    [Test]
    public void Tokenize_TracksSpaceTabAndEveryNewlineForm()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();

        VersionToken[] tokens = inspection.Tokenize(" \tA\nB\rC\r\nD");

        Assert.That(tokens.Length, Is.EqualTo(9));
        AssertToken(tokens[0], VersionTokenKind.Whitespace, " \t", 0, 2, 1, 1);
        AssertToken(tokens[1], VersionTokenKind.TextIdentifier, "A", 2, 3, 1, 3);
        AssertToken(tokens[2], VersionTokenKind.Whitespace, "\n", 3, 4, 1, 4);
        AssertToken(tokens[3], VersionTokenKind.TextIdentifier, "B", 4, 5, 2, 1);
        AssertToken(tokens[4], VersionTokenKind.Whitespace, "\r", 5, 6, 2, 2);
        AssertToken(tokens[5], VersionTokenKind.TextIdentifier, "C", 6, 7, 3, 1);
        AssertToken(tokens[6], VersionTokenKind.Whitespace, "\r\n", 7, 9, 3, 2);
        AssertToken(tokens[7], VersionTokenKind.TextIdentifier, "D", 9, 10, 4, 1);
        AssertToken(tokens[8], VersionTokenKind.End, "", 10, 10, 4, 2);
    }

    [Test]
    public void Tokenize_ClassifiesWildcardVariantsOperatorsDelimitersAndUnknownText()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();

        VersionToken[] tokens = inspection.Tokenize("x X * < <= > >= = == ! != | || [ ] ( ) , .-+_ ?");

        Assert.That(TokenTexts(tokens, VersionTokenKind.Wildcard), Is.EqualTo(new string[] { "x", "X", "*" }));
        Assert.That(TokenTexts(tokens, VersionTokenKind.Operator), Is.EqualTo(new string[] { "<", "<=", ">", ">=", "=", "==", "!", "!=", "|", "||" }));
        Assert.That(TokenTexts(tokens, VersionTokenKind.RangeDelimiter), Is.EqualTo(new string[] { "[", "]", "(", ")", "," }));
        Assert.That(TokenTexts(tokens, VersionTokenKind.Separator), Is.EqualTo(new string[] { ".", "-", "+", "_" }));
        Assert.That(TokenTexts(tokens, VersionTokenKind.Unknown), Is.EqualTo(new string[] { "?" }));
    }

    [Test]
    public void ConvertInterval_ExposesEquivalentComparatorsThroughPublicData()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();
        UniversalVersion lower = Parse("1.0.0");
        UniversalVersion upper = Parse("2.0.0");
        VersionInterval interval = new VersionInterval(
            BoundKind.Exclusive,
            lower,
            BoundKind.Inclusive,
            upper,
            VersionComparisonMode.Identity);

        VersionComparatorSet set = inspection.ConvertInterval(interval);

        Assert.That(set.comparators.Length, Is.EqualTo(2));
        Assert.That(set.comparators[0].rangeOperator, Is.EqualTo(RangeOperator.GreaterThan));
        Assert.That(set.comparators[0].version, Is.SameAs(lower));
        Assert.That(set.comparators[0].comparisonMode, Is.EqualTo(VersionComparisonMode.Identity));
        Assert.That(set.comparators[1].rangeOperator, Is.EqualTo(RangeOperator.LessThanOrEqual));
        Assert.That(set.comparators[1].version, Is.SameAs(upper));
        Assert.That(set.comparators[1].comparisonMode, Is.EqualTo(VersionComparisonMode.Identity));
    }

    [TestCase(BoundKind.Inclusive, RangeOperator.GreaterThanOrEqual, RangeOperator.LessThanOrEqual)]
    [TestCase(BoundKind.Exclusive, RangeOperator.GreaterThan, RangeOperator.LessThan)]
    public void ConvertInterval_MapsInclusiveAndExclusiveBounds(
        BoundKind boundKind,
        RangeOperator expectedLowerOperator,
        RangeOperator expectedUpperOperator)
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();
        UniversalVersion lower = Parse("1.0.0");
        UniversalVersion upper = Parse("2.0.0");
        VersionInterval interval = new VersionInterval(
            boundKind,
            lower,
            boundKind,
            upper,
            VersionComparisonMode.NormalizedIdentity);

        VersionComparatorSet set = inspection.ConvertInterval(interval);

        AssertComparator(set.comparators[0], expectedLowerOperator, lower, VersionComparisonMode.NormalizedIdentity);
        AssertComparator(set.comparators[1], expectedUpperOperator, upper, VersionComparisonMode.NormalizedIdentity);
    }

    [Test]
    public void ConvertInterval_HandlesOneSidedAndFullyUnboundedIntervals()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();
        UniversalVersion lower = Parse("1.0.0");
        UniversalVersion upper = Parse("2.0.0");
        VersionInterval lowerOnly = new VersionInterval(
            BoundKind.Inclusive,
            lower,
            BoundKind.Unbounded,
            null,
            VersionComparisonMode.Precedence);
        VersionInterval upperOnly = new VersionInterval(
            BoundKind.Unbounded,
            null,
            BoundKind.Exclusive,
            upper,
            VersionComparisonMode.Precedence);
        VersionInterval unbounded = new VersionInterval(
            BoundKind.Unbounded,
            null,
            BoundKind.Unbounded,
            null,
            VersionComparisonMode.Precedence);

        VersionComparatorSet lowerSet = inspection.ConvertInterval(lowerOnly);
        VersionComparatorSet upperSet = inspection.ConvertInterval(upperOnly);
        VersionComparatorSet universalSet = inspection.ConvertInterval(unbounded);

        Assert.That(lowerSet.comparators.Length, Is.EqualTo(1));
        AssertComparator(lowerSet.comparators[0], RangeOperator.GreaterThanOrEqual, lower, VersionComparisonMode.Precedence);
        Assert.That(upperSet.comparators.Length, Is.EqualTo(1));
        AssertComparator(upperSet.comparators[0], RangeOperator.LessThan, upper, VersionComparisonMode.Precedence);
        Assert.That(universalSet.comparators, Is.Empty);
    }

    [Test]
    public void ConvertInterval_RepresentsAnInclusiveExactBoundary()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();
        UniversalVersion exact = Parse("1.2.3");
        VersionInterval interval = new VersionInterval(
            BoundKind.Inclusive,
            exact,
            BoundKind.Inclusive,
            exact,
            VersionComparisonMode.Precedence);

        VersionComparatorSet set = inspection.ConvertInterval(interval);

        Assert.That(set.comparators.Length, Is.EqualTo(2));
        AssertComparator(set.comparators[0], RangeOperator.GreaterThanOrEqual, exact, VersionComparisonMode.Precedence);
        AssertComparator(set.comparators[1], RangeOperator.LessThanOrEqual, exact, VersionComparisonMode.Precedence);
    }

    [Test]
    public void ConvertInterval_OutputComposesWithAPublicExclusionHole()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();
        UniversalVersion lower = Parse("1.0.0");
        UniversalVersion upper = Parse("2.0.0");
        UniversalVersion excluded = Parse("1.5.0");
        VersionInterval interval = new VersionInterval(
            BoundKind.Inclusive,
            lower,
            BoundKind.Exclusive,
            upper,
            VersionComparisonMode.Precedence);
        VersionComparatorSet bounds = inspection.ConvertInterval(interval);
        VersionComparatorSet withHole = new VersionComparatorSet(new VersionComparator[]
        {
            bounds.comparators[0],
            bounds.comparators[1],
            new VersionComparator(RangeOperator.NotEqual, excluded, VersionComparisonMode.Precedence)
        });

        Assert.That(withHole.comparators.Length, Is.EqualTo(3));
        AssertComparator(withHole.comparators[0], RangeOperator.GreaterThanOrEqual, lower, VersionComparisonMode.Precedence);
        AssertComparator(withHole.comparators[1], RangeOperator.LessThan, upper, VersionComparisonMode.Precedence);
        AssertComparator(withHole.comparators[2], RangeOperator.NotEqual, excluded, VersionComparisonMode.Precedence);
    }

    [Test]
    public void IntervalConstruction_RejectsMalformedAndEmptyBoundCombinations()
    {
        UniversalVersion lower = Parse("1.0.0");
        UniversalVersion upper = Parse("2.0.0");

        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Unbounded, lower, BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Inclusive, null, BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Inclusive, upper, BoundKind.Inclusive, lower, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Exclusive, lower, BoundKind.Inclusive, lower, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                new VersionInterval((BoundKind)99, lower, BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void PublicOperations_RejectNullInputs()
    {
        IVersionSyntaxInspection inspection = new VersionSyntaxInspection();

        Assert.That(
            delegate
            {
                inspection.Tokenize(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                inspection.ConvertInterval(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static void AssertToken(
        VersionToken token,
        VersionTokenKind kind,
        string text,
        int startIndex,
        int endIndex,
        int lineNumber,
        int columnNumber)
    {
        Assert.That(token.kind, Is.EqualTo(kind));
        Assert.That(token.text, Is.EqualTo(text));
        Assert.That(token.startIndex, Is.EqualTo(startIndex));
        Assert.That(token.endIndex, Is.EqualTo(endIndex));
        Assert.That(token.lineNumber, Is.EqualTo(lineNumber));
        Assert.That(token.columnNumber, Is.EqualTo(columnNumber));
    }

    static void AssertComparator(
        VersionComparator comparator,
        RangeOperator rangeOperator,
        UniversalVersion version,
        VersionComparisonMode comparisonMode)
    {
        Assert.That(comparator.rangeOperator, Is.EqualTo(rangeOperator));
        Assert.That(comparator.version, Is.SameAs(version));
        Assert.That(comparator.comparisonMode, Is.EqualTo(comparisonMode));
    }

    static string[] TokenTexts(VersionToken[] tokens, VersionTokenKind kind)
    {
        System.Collections.Generic.List<string> texts = new System.Collections.Generic.List<string>();
        int index = 0;

        while (index < tokens.Length)
        {
            if (tokens[index].kind == kind)
            {
                texts.Add(tokens[index].text);
            }

            index = index + 1;
        }

        return texts.ToArray();
    }

    static UniversalVersion Parse(string text)
    {
        IVersionParsing parsing = new VersionService();
        VersionParseOptions options = new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            PrereleasePolicy.Default);

        return parsing.Parse(text, options);
    }
}
