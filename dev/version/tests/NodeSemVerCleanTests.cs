using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerCleanTests
{
    [Test]
    public void Clean_TrimsWhitespaceAndRemovesLeadingEqualsAndV()
    {
        Assert.That(NodeSemVer.Clean("1.2.3"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Clean(" \t= v1.2.3 \r\n"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Clean("= V1.2.3"), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void Clean_PreservesPrereleaseAndBuildMetadata()
    {
        Assert.That(NodeSemVer.Clean(" = v1.2.3-alpha.1+build.5 "), Is.EqualTo("1.2.3-alpha.1+build.5"));
    }

    [Test]
    public void Clean_UsesStrictSemVerRulesAfterPrefixCleaning()
    {
        Assert.That(NodeSemVer.Clean("v01.2.3"), Is.Null);
        Assert.That(NodeSemVer.Clean("v1.02.3"), Is.Null);
        Assert.That(NodeSemVer.Clean("v1.2.03"), Is.Null);
        Assert.That(NodeSemVer.Clean("v1.2.3-alpha.01"), Is.Null);
    }

    [Test]
    public void CleanLoose_NormalizesLooseNumericIdentifiers()
    {
        Assert.That(NodeSemVer.CleanLoose(" \t= v01.002.0003-alpha.01+build.0007 \r\n"), Is.EqualTo("1.2.3-alpha.1+build.7"));
    }

    [Test]
    public void CleanLoose_RejectsPartialAndEmbeddedVersionsWithoutCoercion()
    {
        Assert.That(NodeSemVer.CleanLoose("1.2"), Is.Null);
        Assert.That(NodeSemVer.CleanLoose("release 1.2.3"), Is.Null);
    }

    [Test]
    public void CleanResult_ReturnsSourceTextVersionTextAndNoDiagnosticsForSuccess()
    {
        NodeSemVerValidResult result = NodeSemVer.CleanResult(" = v1.2.3+build.7 ");

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(" = v1.2.3+build.7 "));
        Assert.That(result.GetVersionText(), Is.EqualTo("1.2.3+build.7"));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));
    }

    [Test]
    public void CleanResult_ReturnsDiagnosticsForInvalidInput()
    {
        NodeSemVerValidResult result = NodeSemVer.CleanResult(" = v1.2 ");

        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetSourceText(), Is.EqualTo(" = v1.2 "));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.MissingPatch));
        Assert.That(
            delegate
            {
                result.GetVersionText();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void CleanLooseResult_ReturnsDiagnosticsForMalformedLooseInput()
    {
        NodeSemVerValidResult result = NodeSemVer.CleanLooseResult("release 1.2.3");

        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo("release 1.2.3"));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.IllegalCharacter));
    }

    [Test]
    public void Clean_HandlesNullAsInvalidInput()
    {
        string? nullText = null;

        Assert.That(NodeSemVer.Clean(nullText), Is.Null);
        Assert.That(NodeSemVer.CleanLoose(nullText), Is.Null);

        NodeSemVerValidResult result = NodeSemVer.CleanResult(nullText);
        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetDiagnostic(0).GetMessage(), Is.EqualTo("Version text cannot be null."));
    }
}
