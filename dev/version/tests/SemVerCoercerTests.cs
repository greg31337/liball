using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerCoercerTests
{
    [Test]
    public void Coerce_ExtractsFirstVersionTupleFromSurroundingText()
    {
        string text = "release version v2.03.004-alpha+build";
        VersionParseResult result = SemVerCoercer.Coerce(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(text));
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetOriginalText(), Is.EqualTo(text));
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(version.HasLeadingVPrefix(), Is.True);
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("2"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("3"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("4"));
        Assert.That(version.GetMissingComponentCount(), Is.EqualTo(0));
        Assert.That(version.IsPrerelease(), Is.False);
        Assert.That(version.HasBuildMetadata(), Is.False);
        Assert.That(version.ToNormalizedString(), Is.EqualTo("2.3.4"));
    }

    [Test]
    public void Coerce_FillsMissingMinorAndPatchComponents()
    {
        AssertCoerced("pkg 42", "42.0.0", 2);
        AssertCoerced("pkg 42.6", "42.6.0", 1);
        AssertCoerced("pkg 42.6.7", "42.6.7", 0);
    }

    [Test]
    public void Coerce_IgnoresExtraNumericAndTextSuffixes()
    {
        AssertCoerced("build 42.6.7.9", "42.6.7", 0);
        AssertCoerced("build 42.6.x", "42.6.0", 1);
        AssertCoerced("build 42.6.7-rc.1+meta", "42.6.7", 0);
    }

    [Test]
    public void Coerce_CanBeReachedThroughSemVerParserFacade()
    {
        VersionParseResult result = SemVerParser.Coerce("version 0001.0002.0003");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void Coerce_FailsWhenInputContainsNoDigits()
    {
        AssertCoerceFailure("");
        AssertCoerceFailure("no version here");
    }

    static void AssertCoerced(string text, string normalizedText, int missingComponentCount)
    {
        VersionParseResult result = SemVerCoercer.Coerce(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo(normalizedText));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(missingComponentCount));
    }

    static void AssertCoerceFailure(string text)
    {
        VersionParseResult result = SemVerCoercer.Coerce(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.NoCoercibleVersion));
    }
}
