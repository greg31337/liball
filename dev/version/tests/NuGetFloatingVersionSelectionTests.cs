using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetFloatingVersionSelectionTests
{
    [Test]
    public void SelectHighestMatchingText_StarChoosesHighestStableVersion()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.1.0",
            "1.1.1",
            "1.2.0",
            "1.3.0-alpha"
        });

        Assert.That(selected, Is.EqualTo("1.2.0"));
    }

    [Test]
    public void SelectHighestMatchingText_NumericStablePatternChoosesHighestStableMatch()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("1.1.*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.1.0",
            "1.1.1",
            "1.1.2-alpha",
            "1.2.0-alpha"
        });

        Assert.That(selected, Is.EqualTo("1.1.1"));
    }

    [Test]
    public void SelectHighestMatchingText_DashStarChoosesHighestIncludingPrerelease()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("*-*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.1.0",
            "1.1.1",
            "1.1.2-alpha",
            "1.3.0-beta"
        });

        Assert.That(selected, Is.EqualTo("1.3.0-beta"));
    }

    [Test]
    public void SelectHighestMatchingText_NumericDashStarChoosesHighestPrereleaseWithinPattern()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("1.1.*-*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.1.0",
            "1.1.1",
            "1.1.2-alpha",
            "1.1.2-beta",
            "1.3.0-beta"
        });

        Assert.That(selected, Is.EqualTo("1.1.2-beta"));
    }

    [Test]
    public void SelectHighestMatchingText_PrereleaseLabelFloatingAllowsStableCore()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("1.2.0-rc.*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.1.0",
            "1.2.0-rc.1",
            "1.2.0-rc.2",
            "1.2.0"
        });

        Assert.That(selected, Is.EqualTo("1.2.0"));
    }

    [Test]
    public void SelectHighestMatchingVersion_ReturnsHighestMatchingParsedVersion()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("2.*");

        UniversalVersion? selected = floatingVersion.SelectHighestMatchingVersion(new UniversalVersion[]
        {
            ParseNuGet("1.9.9"),
            ParseNuGet("2.0.0"),
            ParseNuGet("2.5.0"),
            ParseNuGet("3.0.0")
        });

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.ToNormalizedString(), Is.EqualTo("2.5.0"));
    }

    [Test]
    public void SelectHighestMatchingText_ReturnsEmptyStringWhenNoCandidateMatches()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("3.*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "1.0.0",
            "2.0.0",
            "3.0.0-alpha"
        });

        Assert.That(selected, Is.EqualTo(""));
    }

    [Test]
    public void SelectHighestMatchingText_IgnoresInvalidCandidateText()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("1.*");

        string selected = floatingVersion.SelectHighestMatchingText(new string[]
        {
            "not-a-version",
            "1.0.0",
            "1.2.0"
        });

        Assert.That(selected, Is.EqualTo("1.2.0"));
    }

    [Test]
    public void SelectHighestMatchingText_RejectsNullInputs()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("*");

        Assert.That(
            delegate
            {
                floatingVersion.SelectHighestMatchingText(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                floatingVersion.SelectHighestMatchingText(new string[] { "1.0.0", null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void SelectHighestMatchingVersion_RejectsNullInputs()
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("*");

        Assert.That(
            delegate
            {
                floatingVersion.SelectHighestMatchingVersion(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                floatingVersion.SelectHighestMatchingVersion(new UniversalVersion[] { ParseNuGet("1.0.0"), null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static NuGetFloatingVersion ParseFloatingVersion(string text)
    {
        NuGetFloatingVersion floatingVersion = NuGetFloatingVersion.Parse(text);

        Assert.That(floatingVersion.IsValid(), Is.True, text);
        return floatingVersion;
    }

    static UniversalVersion ParseNuGet(string text)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        return result.GetVersion();
    }
}
