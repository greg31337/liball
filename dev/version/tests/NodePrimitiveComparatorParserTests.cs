using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodePrimitiveComparatorParserTests
{
    [Test]
    public void Parse_MapsPrimitiveOperators()
    {
        AssertComparator("<1.2.3", RangeOperator.LessThan, "1.2.3");
        AssertComparator("<=1.2.3", RangeOperator.LessThanOrEqual, "1.2.3");
        AssertComparator(">1.2.3", RangeOperator.GreaterThan, "1.2.3");
        AssertComparator(">=1.2.3", RangeOperator.GreaterThanOrEqual, "1.2.3");
        AssertComparator("=1.2.3", RangeOperator.Equal, "1.2.3");
    }

    [Test]
    public void Parse_AssumesEqualityWhenOperatorIsMissing()
    {
        VersionComparator comparator = ParseComparator("1.2.3");

        Assert.That(comparator.GetOperator(), Is.EqualTo(RangeOperator.Equal));
        Assert.That(comparator.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void Parse_AllowsWhitespaceAndNodeVersionPrefixes()
    {
        VersionComparator comparator = ParseComparator("  >=  v1.2.3  ");

        Assert.That(comparator.GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(comparator.GetVersion().GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(comparator.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(comparator.GetVersion().HasLeadingVPrefix(), Is.True);
    }

    [Test]
    public void ParsedComparator_ContainsExpectedVersions()
    {
        VersionComparator comparator = ParseComparator(">=1.2.7");

        Assert.That(comparator.ContainsVersion(ParseNodeVersion("1.2.6")), Is.False);
        Assert.That(comparator.ContainsVersion(ParseNodeVersion("1.2.7")), Is.True);
        Assert.That(comparator.ContainsVersion(ParseNodeVersion("1.2.8")), Is.True);
        Assert.That(comparator.ContainsVersion(ParseNodeVersion("2.5.3")), Is.True);
    }

    [Test]
    public void Parse_PreservesPrereleaseAndBuildMetadata()
    {
        VersionComparator comparator = ParseComparator("<1.2.3-alpha.1+build.5");

        Assert.That(comparator.GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(comparator.GetVersion().IsPrerelease(), Is.True);
        Assert.That(comparator.GetVersion().HasBuildMetadata(), Is.True);
        Assert.That(comparator.GetVersion().GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(comparator.GetVersion().GetPrereleaseIdentifier(1).GetText(), Is.EqualTo("1"));
        Assert.That(comparator.GetVersion().GetBuildIdentifier(0).GetText(), Is.EqualTo("build"));
        Assert.That(comparator.GetVersion().GetBuildIdentifier(1).GetText(), Is.EqualTo("5"));
    }

    [Test]
    public void Parse_RejectsMalformedPrimitiveComparators()
    {
        AssertFailure("", NodeRangeDiagnosticCodes.MissingComparator);
        AssertFailure("   ", NodeRangeDiagnosticCodes.MissingComparator);
        AssertFailure(">=", NodeRangeDiagnosticCodes.MissingVersion);
        AssertFailure("!=1.2.3", NodeRangeDiagnosticCodes.InvalidOperator);
        AssertFailure("=>1.2.3", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure(">=bad", NodeRangeDiagnosticCodes.InvalidVersion);
        AssertFailure(">=1.2", NodeRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void FailureResult_DoesNotExposeComparator()
    {
        VersionComparatorParseResult result = NodePrimitiveComparatorParser.Parse("bad");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasComparator(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(
            delegate
            {
                result.GetComparator();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void Parse_RejectsNullText()
    {
        VersionComparatorParseResult result = NodePrimitiveComparatorParser.Parse(null);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasComparator(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.NullInput));
    }

    static void AssertComparator(string text, RangeOperator expectedOperator, string expectedVersion)
    {
        VersionComparator comparator = ParseComparator(text);

        Assert.That(comparator.GetOperator(), Is.EqualTo(expectedOperator));
        Assert.That(comparator.GetVersion().ToNormalizedString(), Is.EqualTo(expectedVersion));
        Assert.That(comparator.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Precedence));
    }

    static VersionComparator ParseComparator(string text)
    {
        VersionComparatorParseResult result = NodePrimitiveComparatorParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(result.HasComparator(), Is.True);
        return result.GetComparator();
    }

    static UniversalVersion ParseNodeVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionComparatorParseResult result = NodePrimitiveComparatorParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasComparator(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode));
    }
}
