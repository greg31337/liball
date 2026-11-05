using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CoreModelTests
{
    [Test]
    public void VersionIdentifier_RepresentsNumericAndTextIdentifiers()
    {
        VersionIdentifier numeric = VersionIdentifier.CreateNumeric("001");
        VersionIdentifier text = VersionIdentifier.CreateText("alpha");

        Assert.That(numeric.GetText(), Is.EqualTo("001"));
        Assert.That(numeric.GetLength(), Is.EqualTo(3));
        Assert.That(numeric.IsNumeric(), Is.True);
        Assert.That(numeric.IsText(), Is.False);
        Assert.That(numeric.HasLeadingZero(), Is.True);

        Assert.That(text.GetText(), Is.EqualTo("alpha"));
        Assert.That(text.IsNumeric(), Is.False);
        Assert.That(text.IsText(), Is.True);
        Assert.That(text.HasLeadingZero(), Is.False);
    }

    [Test]
    public void UniversalVersion_CarriesConstructorControlledState()
    {
        VersionIdentifier prerelease = VersionIdentifier.CreateText("alpha");
        VersionIdentifier prereleaseNumber = VersionIdentifier.CreateNumeric("1");
        VersionIdentifier build = VersionIdentifier.CreateText("build");

        UniversalVersion version = new UniversalVersion(
            "v1.2.3-alpha.1+build",
            VersionDialect.SemVer,
            new string[] { "1", "2", "3" },
            new VersionIdentifier[] { prerelease, prereleaseNumber },
            new VersionIdentifier[] { build },
            0,
            true,
            false,
            "",
            null);

        Assert.That(version.GetOriginalText(), Is.EqualTo("v1.2.3-alpha.1+build"));
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(version.GetNumericComponentCount(), Is.EqualTo(3));
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("1"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("2"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("3"));
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.GetPrereleaseIdentifierCount(), Is.EqualTo(2));
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.GetPrereleaseIdentifier(1).GetText(), Is.EqualTo("1"));
        Assert.That(version.HasBuildMetadata(), Is.True);
        Assert.That(version.GetBuildIdentifierCount(), Is.EqualTo(1));
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("build"));
        Assert.That(version.GetMissingComponentCount(), Is.EqualTo(0));
        Assert.That(version.HasLeadingVPrefix(), Is.True);
        Assert.That(version.HasRevisionComponent(), Is.False);
        Assert.That(version.GetRevisionComponent(), Is.EqualTo(""));
    }

    [Test]
    public void UniversalVersion_DefensivelyCopiesArrays()
    {
        VersionIdentifier alpha = VersionIdentifier.CreateText("alpha");
        VersionIdentifier beta = VersionIdentifier.CreateText("beta");
        string[] numericComponents = new string[] { "1", "2" };
        VersionIdentifier[] prereleaseIdentifiers = new VersionIdentifier[] { alpha };
        VersionIdentifier[] buildIdentifiers = new VersionIdentifier[] { beta };

        UniversalVersion version = new UniversalVersion(
            "1.2-alpha+beta",
            VersionDialect.NodeSemVer,
            numericComponents,
            prereleaseIdentifiers,
            buildIdentifiers,
            1,
            false,
            false,
            "",
            null);

        numericComponents[0] = "9";
        prereleaseIdentifiers[0] = beta;
        buildIdentifiers[0] = alpha;

        string[] returnedNumericComponents = version.GetNumericComponents();
        VersionIdentifier[] returnedPrereleaseIdentifiers = version.GetPrereleaseIdentifiers();
        VersionIdentifier[] returnedBuildIdentifiers = version.GetBuildIdentifiers();

        returnedNumericComponents[1] = "9";
        returnedPrereleaseIdentifiers[0] = beta;
        returnedBuildIdentifiers[0] = alpha;

        Assert.That(version.GetNumericComponent(0), Is.EqualTo("1"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("2"));
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("beta"));
    }

    [Test]
    public void UniversalVersion_CanRepresentRevisionComponents()
    {
        UniversalVersion version = new UniversalVersion(
            "1.2.3.4",
            VersionDialect.NuGet,
            new string[] { "1", "2", "3" },
            new VersionIdentifier[0],
            new VersionIdentifier[0],
            0,
            false,
            true,
            "4",
            null);

        Assert.That(version.HasRevisionComponent(), Is.True);
        Assert.That(version.GetRevisionComponent(), Is.EqualTo("4"));
        Assert.That(version.IsPrerelease(), Is.False);
        Assert.That(version.HasBuildMetadata(), Is.False);
    }

    [Test]
    public void VersionToken_RejectsInvalidSpanAndLocation()
    {
        Assert.That(
            delegate
            {
                new VersionToken(VersionTokenKind.TextIdentifier, "bad", -1, 3, 1, 1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new VersionToken(VersionTokenKind.TextIdentifier, "bad", 3, 2, 1, 1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new VersionToken(VersionTokenKind.TextIdentifier, "bad", 0, 3, 0, 1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }
}
