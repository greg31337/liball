using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class InvalidInputParserTests
{
    [Test]
    public void Parsers_RejectEmptyComponents()
    {
        AssertVersionFailure(SemVerParser.ParseStrict("1..3"), SemVerDiagnosticCodes.IllegalCharacter);
        AssertVersionFailure(NuGetVersionParser.Parse("1..3"), SemVerDiagnosticCodes.IllegalCharacter);
        AssertRubyGemsFailure(RubyGemsVersionParser.Parse("1..0"), RubyGemsDiagnosticCodes.InvalidVersion);
        AssertVersionFailure(PythonVersionParser.Parse("1..0"), PythonDiagnosticCodes.InvalidVersion);
        AssertRangeFailure(NodeRangeParser.Parse("1..3"), NodeRangeDiagnosticCodes.InvalidVersion);
        AssertRangeFailure(CargoRangeParser.Parse("1..3"), CargoRangeDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void RangeParsers_RejectMalformedOperators()
    {
        AssertComparatorFailure(NodePrimitiveComparatorParser.Parse("!=1.2.3"), NodeRangeDiagnosticCodes.InvalidOperator);
        AssertRangeFailure(CargoRangeParser.Parse("!=1.2.3"), CargoRangeDiagnosticCodes.InvalidOperator);
        AssertRangeFailure(RubyGemsRangeParser.Parse("! 1.2.3"), RubyGemsRequirementDiagnosticCodes.InvalidOperator);
        AssertRangeFailure(PythonSpecifierParser.Parse(">1.0.*"), PythonSpecifierDiagnosticCodes.InvalidOperator);
    }

    [Test]
    public void Parsers_RejectInvalidIdentifiers()
    {
        AssertVersionFailure(SemVerParser.ParseStrict("1.2.3-alpha_1"), SemVerDiagnosticCodes.IllegalCharacter);
        AssertVersionFailure(NuGetVersionParser.Parse("1.2.3-alpha_1"), SemVerDiagnosticCodes.MalformedPrerelease);
        AssertRubyGemsFailure(RubyGemsVersionParser.Parse("1.0-rc1"), RubyGemsDiagnosticCodes.InvalidVersion);
        AssertVersionFailure(PythonVersionParser.Parse("1.0+bad!"), PythonDiagnosticCodes.InvalidVersion);
    }

    [Test]
    public void RangeParsers_RejectUnexpectedDelimiters()
    {
        AssertRangeFailure(MavenRangeParser.Parse("[1.0,2.0"), MavenRangeDiagnosticCodes.InvalidDelimiter);
        AssertRangeFailure(MavenRangeParser.Parse("[1.0,2.0) trailing"), MavenRangeDiagnosticCodes.InvalidDelimiter);
        AssertRangeFailure(NuGetRangeParser.Parse("[1.0,2.0"), NuGetRangeDiagnosticCodes.InvalidDelimiter);
        AssertRangeFailure(NuGetRangeParser.Parse("[1.0,2.0) trailing"), NuGetRangeDiagnosticCodes.InvalidDelimiter);
        AssertRangeFailure(NodeRangeParser.Parse("1.2.3 | 2.0.0"), NodeRangeDiagnosticCodes.MissingComparator);
        AssertRangeFailure(CargoRangeParser.Parse(">=1.0.0 <2.0.0"), CargoRangeDiagnosticCodes.InvalidOperator);
    }

    [Test]
    public void Parsers_RejectHugeMalformedNumericComponentsWithoutOverflow()
    {
        string hugeLeadingZero = "0" + RepeatCharacter('9', 256);

        AssertVersionFailure(SemVerParser.ParseStrict("1." + hugeLeadingZero + ".3"), SemVerDiagnosticCodes.NumericComponentLeadingZero);
        AssertVersionFailure(GoVersionParser.Parse("v" + hugeLeadingZero + ".2.3"), SemVerDiagnosticCodes.IllegalCharacter);
        AssertRangeFailure(CargoRangeParser.Parse("^" + hugeLeadingZero + ".2.3"), CargoRangeDiagnosticCodes.InvalidVersion);
        AssertVersionFailure(PythonVersionParser.Parse("1." + RepeatCharacter('9', 256) + "."), PythonDiagnosticCodes.InvalidVersion);
    }

    static string RepeatCharacter(char value, int count)
    {
        return new string(value, count);
    }

    static void AssertVersionFailure(VersionParseResult result, string expectedCode)
    {
        Assert.That(result.IsFailure(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.False, result.GetSourceText());
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), result.GetSourceText());
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), result.GetSourceText());
    }

    static void AssertRangeFailure(VersionRangeParseResult result, string expectedCode)
    {
        Assert.That(result.IsFailure(), Is.True, result.GetSourceText());
        Assert.That(result.HasRange(), Is.False, result.GetSourceText());
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), result.GetSourceText());
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), result.GetSourceText());
    }

    static void AssertComparatorFailure(VersionComparatorParseResult result, string expectedCode)
    {
        Assert.That(result.IsFailure(), Is.True, result.GetSourceText());
        Assert.That(result.HasComparator(), Is.False, result.GetSourceText());
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), result.GetSourceText());
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), result.GetSourceText());
    }

    static void AssertRubyGemsFailure(RubyGemsVersionParseResult result, string expectedCode)
    {
        Assert.That(result.IsFailure(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.False, result.GetSourceText());
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), result.GetSourceText());
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), result.GetSourceText());
    }
}
