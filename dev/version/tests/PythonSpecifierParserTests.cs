using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonSpecifierParserTests
{
    [Test]
    public void Parse_CommaSeparatedSpecifiersCreateIntersection()
    {
        VersionRange range = ParseRange(">=1.0, <2.0, !=1.5");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.0", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.4.9", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.5", VersionDialect.Python), Is.False);
        Assert.That(range.ContainsText("2.0", VersionDialect.Python), Is.False);
    }

    [Test]
    public void Parse_CompatibleReleaseDesugarsToLowerAndUpperBounds()
    {
        AssertMatches("~=1.4", new string[] { "1.4", "1.9.9" }, new string[] { "1.3.9", "2.0" });
        AssertMatches("~=1.4.5", new string[] { "1.4.5", "1.4.99" }, new string[] { "1.4.4", "1.5.0" });
        AssertMatches("~=1.4.5.0", new string[] { "1.4.5", "1.4.5.9" }, new string[] { "1.4.4", "1.4.6" });
    }

    [Test]
    public void Parse_EqualityAndInequalityUsePythonVersionOrdering()
    {
        AssertMatches("==1.0", new string[] { "1.0", "1.0.0" }, new string[] { "1.0.1" });
        AssertMatches("!=1.0", new string[] { "1.0.1" }, new string[] { "1.0", "1.0.0", "1.0a1" });
    }

    [Test]
    public void Parse_WildcardEqualityMatchesReleasePrefix()
    {
        AssertMatches("==1.2.*", new string[] { "1.2", "1.2.5", "1.2.post1" }, new string[] { "1.1.9", "1.3.0" });
        AssertMatches("!=1.2.*", new string[] { "1.1.9", "1.3.0" }, new string[] { "1.2", "1.2.5" });
    }

    [Test]
    public void Parse_DefaultPrereleasePolicyExcludesPrereleases()
    {
        VersionRange range = ParseRange(">=1.0");

        Assert.That(range.ContainsText("1.1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.1a1", VersionDialect.Python), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.1a1", VersionDialect.Python, true), Is.True);
    }

    [Test]
    public void Parse_ExplicitPrereleaseSpecifierAllowsPrereleasesByDefault()
    {
        VersionRange range = ParseRange(">=1.0a1");

        Assert.That(range.ContainsText("1.0a1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.1a1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.1", VersionDialect.Python), Is.True);
    }

    [Test]
    public void Parse_ResultCarriesPythonSyntaxAndRange()
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(">=1.0");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Python));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.0", VersionDialect.Python), Is.True);
    }

    [Test]
    public void Parse_RejectsInvalidSpecifiers()
    {
        AssertFailure("", PythonSpecifierDiagnosticCodes.MissingSpecifier);
        AssertFailure("1.0", PythonSpecifierDiagnosticCodes.InvalidOperator);
        AssertFailure(">=", PythonSpecifierDiagnosticCodes.MissingVersion);
        AssertFailure(">=bad", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertFailure("~=1", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertFailure(">1.0.*", PythonSpecifierDiagnosticCodes.InvalidOperator);
        AssertFailure("==1.*.2", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertFailure(">=1.0,", PythonSpecifierDiagnosticCodes.MissingSpecifier);
    }

    [Test]
    public void Parse_RejectsNullText()
    {
        Assert.That(
            delegate
            {
                PythonSpecifierParser.Parse(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertMatches(string specifierText, string[] matchingVersions, string[] rejectedVersions)
    {
        VersionRange range = ParseRange(specifierText);
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                range.ContainsText(matchingVersions[index], VersionDialect.Python),
                Is.True,
                specifierText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.Python),
                Is.False,
                specifierText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Python), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
