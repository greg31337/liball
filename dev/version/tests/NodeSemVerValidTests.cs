using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerValidTests
{
    [Test]
    public void Valid_ReturnsNormalizedTextForStrictSemVer()
    {
        Assert.That(NodeSemVer.Valid("1.2.3"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Valid("1.2.3-alpha.1+build.5"), Is.EqualTo("1.2.3-alpha.1+build.5"));
    }

    [Test]
    public void Valid_RejectsLooseOnlyAndMalformedInputsWithoutThrowing()
    {
        string? nullText = null;

        Assert.That(NodeSemVer.Valid("v1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Valid("= 1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Valid(" 1.2.3 "), Is.Null);
        Assert.That(NodeSemVer.Valid("1.2"), Is.Null);
        Assert.That(NodeSemVer.Valid("1.2.3-alpha.01"), Is.Null);
        Assert.That(NodeSemVer.Valid(nullText), Is.Null);
    }

    [Test]
    public void ValidLoose_ReturnsNormalizedTextForNodeCompatibleLooseInputs()
    {
        Assert.That(NodeSemVer.ValidLoose("v1.2.3"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.ValidLoose("= v1.2.3"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.ValidLoose(" \t= v01.002.0003-alpha.01+build.0007 \r\n"), Is.EqualTo("1.2.3-alpha.1+build.7"));
    }

    [Test]
    public void ValidLoose_RejectsPartialAndEmbeddedVersionsWithoutCoercion()
    {
        Assert.That(NodeSemVer.ValidLoose("1.2"), Is.Null);
        Assert.That(NodeSemVer.ValidLoose("release 1.2.3"), Is.Null);
    }

    [Test]
    public void ValidResult_ReturnsVersionTextForStrictSuccess()
    {
        NodeSemVerValidResult result = NodeSemVer.ValidResult("1.2.3+build.7");

        Assert.That(result.IsValid(), Is.True);
        Assert.That(result.IsInvalid(), Is.False);
        Assert.That(result.HasVersion(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo("1.2.3+build.7"));
        Assert.That(result.GetVersionText(), Is.EqualTo("1.2.3+build.7"));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));
    }

    [Test]
    public void ValidResult_ReturnsNullEquivalentInvalidResultWithDiagnostics()
    {
        NodeSemVerValidResult result = NodeSemVer.ValidResult("v1.2.3");

        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetSourceText(), Is.EqualTo("v1.2.3"));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.IllegalCharacter));
        Assert.That(
            delegate
            {
                result.GetVersionText();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void ValidLooseResult_ReturnsDiagnosticsForMalformedLooseInput()
    {
        NodeSemVerValidResult result = NodeSemVer.ValidLooseResult("release 1.2.3");

        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo("release 1.2.3"));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.IllegalCharacter));
    }

    [Test]
    public void ValidResult_HandlesNullAsInvalidInput()
    {
        string? nullText = null;
        NodeSemVerValidResult result = NodeSemVer.ValidResult(nullText);

        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(""));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetMessage(), Is.EqualTo("Version text cannot be null."));
    }

    [Test]
    public void ValidResult_DefensivelyCopiesDiagnostics()
    {
        NodeSemVerValidResult result = NodeSemVer.ValidResult("1.2");
        VersionDiagnostic[] diagnostics = result.GetDiagnostics();

        diagnostics[0] = VersionDiagnostic.FromIndex("changed", "changed", "changed", 0);

        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.MissingPatch));
    }
}
