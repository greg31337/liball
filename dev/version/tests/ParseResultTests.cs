using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class ParseResultTests
{
    [Test]
    public void VersionDiagnostic_FromIndex_ComputesLineAndColumn()
    {
        VersionDiagnostic diagnostic = VersionDiagnostic.FromIndex("SEMVER001", "Expected major version.", "one\r\ntwo\nthree", 5);

        Assert.That(diagnostic.GetCode(), Is.EqualTo("SEMVER001"));
        Assert.That(diagnostic.GetMessage(), Is.EqualTo("Expected major version."));
        Assert.That(diagnostic.GetSourceText(), Is.EqualTo("one\r\ntwo\nthree"));
        Assert.That(diagnostic.GetIndex(), Is.EqualTo(5));
        Assert.That(diagnostic.GetLineNumber(), Is.EqualTo(2));
        Assert.That(diagnostic.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void VersionParseResult_CreateSuccess_CarriesDialectAndNoDiagnostics()
    {
        VersionParseResult result = VersionParseResult.CreateSuccess("1.2.3", VersionDialect.SemVer);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetSourceText(), Is.EqualTo("1.2.3"));
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void VersionParseResult_CreateFailure_DefensivelyCopiesDiagnostics()
    {
        VersionDiagnostic first = VersionDiagnostic.FromIndex("E001", "First.", "bad", 0);
        VersionDiagnostic second = VersionDiagnostic.FromIndex("E002", "Second.", "bad", 1);
        VersionDiagnostic[] diagnostics = new VersionDiagnostic[] { first, second };

        VersionParseResult result = VersionParseResult.CreateFailure("bad", VersionDialect.SemVer, diagnostics);
        diagnostics[0] = second;
        VersionDiagnostic[] returnedDiagnostics = result.GetDiagnostics();
        returnedDiagnostics[1] = first;

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo("E001"));
        Assert.That(result.GetDiagnostic(1).GetCode(), Is.EqualTo("E002"));
    }

    [Test]
    public void VersionRangeParseResult_CreateSuccess_CarriesSyntaxAndNoDiagnostics()
    {
        VersionRangeParseResult result = VersionRangeParseResult.CreateSuccess("^1.2.3", RangeSyntax.Cargo);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsFailure(), Is.False);
        Assert.That(result.GetSourceText(), Is.EqualTo("^1.2.3"));
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));
        Assert.That(result.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void VersionRangeParseResult_CreateFailure_DefensivelyCopiesDiagnostics()
    {
        VersionDiagnostic diagnostic = VersionDiagnostic.FromIndex("RANGE001", "Expected comparator.", "=>1.0", 0);

        VersionRangeParseResult result = VersionRangeParseResult.CreateFailure("=>1.0", RangeSyntax.NodeSemVer, diagnostic);
        VersionDiagnostic[] returnedDiagnostics = result.GetDiagnostics();
        returnedDiagnostics[0] = VersionDiagnostic.FromIndex("OTHER", "Other.", "=>1.0", 1);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NodeSemVer));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo("RANGE001"));
        Assert.That(result.GetDiagnostic(0).GetLineNumber(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetColumnNumber(), Is.EqualTo(1));
    }
}
