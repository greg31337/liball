using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class DialectNativeFormattingApiTests
{
    [Test]
    public void SharedFormatting_UsesMavenDialectNativeVersionText()
    {
        UniversalVersion version = RequireVersion(MavenVersions.Parse("01.000-cr_000"));

        Assert.That(version.ToOriginalString(), Is.EqualTo("01.000-cr_000"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("01.000-cr_000"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.0-rc"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.0-rc"));
        Assert.That(VersionFormatter.FormatNormalized(version), Is.EqualTo("1.0-rc"));
        Assert.That(VersionFormatter.FormatCanonical(version), Is.EqualTo("1.0-rc"));
        Assert.That(MavenVersions.FormatNormalized(version), Is.EqualTo("1.0-rc"));
        Assert.That(MavenVersions.FormatCanonical(version), Is.EqualTo("1.0-rc"));
        Assert.That(MavenVersions.FormatCanonical("1.0.0-FINAL"), Is.EqualTo("1"));
    }

    [Test]
    public void SharedFormatting_UsesRubyGemsDialectNativeVersionText()
    {
        UniversalVersion version = RequireVersion(RubyGemsVersions.Parse(" 001.000.a10.000 "));

        Assert.That(version.ToOriginalString(), Is.EqualTo("001.000.a10.000"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("001.000.a10.000"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.0.a.10.0"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("1.a.10"));
        Assert.That(VersionFormatter.FormatNormalized(version), Is.EqualTo("1.0.a.10.0"));
        Assert.That(VersionFormatter.FormatCanonical(version), Is.EqualTo("1.a.10"));
        Assert.That(RubyGemsVersions.FormatNormalized(version), Is.EqualTo("1.0.a.10.0"));
        Assert.That(RubyGemsVersions.FormatCanonical(version), Is.EqualTo("1.a.10"));
        Assert.That(RubyGemsVersions.FormatCanonical("01.0.0"), Is.EqualTo("1"));
    }

    [Test]
    public void SharedFormatting_UsesPythonDialectNativeVersionText()
    {
        UniversalVersion version = RequireVersion(PythonVersions.Parse("0002!01.0PREVIEW-0004.post_0005.dev-0006+LOCAL_0007"));

        Assert.That(version.ToOriginalString(), Is.EqualTo("0002!01.0PREVIEW-0004.post_0005.dev-0006+LOCAL_0007"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("0002!01.0PREVIEW-0004.post_0005.dev-0006+LOCAL_0007"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(PythonVersions.FormatNormalized(version), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(PythonVersions.FormatCanonical(version), Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
    }

    [Test]
    public void FacadeFormatting_RejectsInvalidTextInputs()
    {
        Assert.That(
            delegate
            {
                MavenVersions.FormatCanonical("[bad]");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsVersions.FormatCanonical("1..0");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static UniversalVersion RequireVersion(VersionParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetSourceText());
        Assert.That(result.HasVersion(), Is.True, result.GetSourceText());
        return result.GetVersion();
    }
}
