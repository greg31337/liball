using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class FormattingTests
{
    [Test]
    public void VersionFormatter_FormatsOriginalNormalizedCanonicalAndDisplayStrings()
    {
        UniversalVersion version = new UniversalVersion(
            "v01.002.0003-alpha.01+build.0007",
            VersionDialect.NodeSemVer,
            new string[] { "01", "002", "0003" },
            new VersionIdentifier[] { VersionIdentifier.CreateText("alpha"), VersionIdentifier.CreateNumeric("01") },
            new VersionIdentifier[] { VersionIdentifier.CreateText("build"), VersionIdentifier.CreateNumeric("0007") },
            0,
            true,
            false,
            "",
            null);

        Assert.That(VersionFormatter.FormatOriginal(version), Is.EqualTo("v01.002.0003-alpha.01+build.0007"));
        Assert.That(VersionFormatter.FormatNormalized(version), Is.EqualTo("1.2.3-alpha.1+build.7"));
        Assert.That(VersionFormatter.FormatCanonical(version), Is.EqualTo("v1.2.3-alpha.1+build.7"));
        Assert.That(VersionFormatter.FormatDisplay(version), Is.EqualTo("v01.002.0003-alpha.01+build.0007"));

        Assert.That(version.ToOriginalString(), Is.EqualTo("v01.002.0003-alpha.01+build.0007"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.7"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("v1.2.3-alpha.1+build.7"));
        Assert.That(version.ToDisplayString(), Is.EqualTo("v01.002.0003-alpha.01+build.0007"));
    }

    [Test]
    public void VersionFormatter_DisplayFallsBackToCanonicalWhenOriginalIsEmpty()
    {
        UniversalVersion version = new UniversalVersion(
            "",
            VersionDialect.SemVer,
            new string[] { "0001", "02", "3" },
            new VersionIdentifier[0],
            new VersionIdentifier[0],
            0,
            false,
            false,
            "",
            null);

        Assert.That(version.ToDisplayString(), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void VersionFormatter_CanonicalUsesDialectRulesForGoAndNuGet()
    {
        UniversalVersion goVersion = new UniversalVersion(
            "v1.2.3+build",
            VersionDialect.Go,
            new string[] { "1", "2", "3" },
            new VersionIdentifier[0],
            new VersionIdentifier[] { VersionIdentifier.CreateText("build") },
            0,
            true,
            false,
            "",
            null);

        UniversalVersion nugetVersion = new UniversalVersion(
            "1.2.3.0+build",
            VersionDialect.NuGet,
            new string[] { "1", "2", "3" },
            new VersionIdentifier[0],
            new VersionIdentifier[] { VersionIdentifier.CreateText("build") },
            0,
            false,
            true,
            "0",
            null);

        Assert.That(goVersion.ToNormalizedString(), Is.EqualTo("1.2.3+build"));
        Assert.That(goVersion.ToCanonicalString(), Is.EqualTo("v1.2.3"));
        Assert.That(nugetVersion.ToNormalizedString(), Is.EqualTo("1.2.3"));
        Assert.That(nugetVersion.ToCanonicalString(), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void VersionFormatter_JoinsComponentsAndIdentifiers()
    {
        Assert.That(VersionFormatter.JoinNumericComponents(new string[] { "000", "01", "20" }), Is.EqualTo("0.1.20"));
        Assert.That(
            VersionFormatter.JoinIdentifiers(new VersionIdentifier[]
            {
                VersionIdentifier.CreateText("rc"),
                VersionIdentifier.CreateNumeric("0002")
            }),
            Is.EqualTo("rc.2"));
        Assert.That(VersionFormatter.NormalizeNumericText("0000"), Is.EqualTo("0"));
        Assert.That(VersionFormatter.NormalizeNumericText("0012"), Is.EqualTo("12"));
    }
}
