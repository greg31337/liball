using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetVersionParserTests
{
    [Test]
    public void Parse_AcceptsOneToFourNumericComponents()
    {
        AssertNuGetVersion("1", new string[] { "1" }, 2, false, "");
        AssertNuGetVersion("1.2", new string[] { "1", "2" }, 1, false, "");
        AssertNuGetVersion("1.2.3", new string[] { "1", "2", "3" }, 0, false, "");
        AssertNuGetVersion("1.2.3.4", new string[] { "1", "2", "3" }, 0, true, "4");
    }

    [Test]
    public void Parse_PreservesLeadingZeroNumericComponentsForNormalizationStage()
    {
        VersionParseResult result = NuGetVersionParser.Parse("01.002.0003.0004");

        Assert.That(result.IsSuccess(), Is.True);

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("01"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("002"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("0003"));
        Assert.That(version.HasRevisionComponent(), Is.True);
        Assert.That(version.GetRevisionComponent(), Is.EqualTo("0004"));
    }

    [Test]
    public void Parse_AcceptsPrereleaseAndBuildMetadata()
    {
        VersionParseResult result = NuGetVersionParser.Parse("1.2.3-alpha.1+meta.0007");

        Assert.That(result.IsSuccess(), Is.True);

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.NuGet));
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.GetPrereleaseIdentifier(1).GetText(), Is.EqualTo("1"));
        Assert.That(version.HasBuildMetadata(), Is.True);
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("meta"));
        Assert.That(version.GetBuildIdentifier(1).GetText(), Is.EqualTo("0007"));
    }

    [Test]
    public void Parse_AcceptsPrereleaseOnShortNumericVersions()
    {
        VersionParseResult result = NuGetVersionParser.Parse("1-beta");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().GetNumericComponentCount(), Is.EqualTo(1));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(2));
        Assert.That(result.GetVersion().GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("beta"));
    }

    [Test]
    public void Parse_RejectsMalformedNuGetVersions()
    {
        AssertNuGetFailure("");
        AssertNuGetFailure("v1.2.3");
        AssertNuGetFailure("1.");
        AssertNuGetFailure("1.2.3.4.5");
        AssertNuGetFailure("1.2.3-");
        AssertNuGetFailure("1.2.3-alpha..1");
        AssertNuGetFailure("1.2.3+");
        AssertNuGetFailure("1.2.3+meta..1");
        AssertNuGetFailure("1.2.3-alpha_1");
    }

    static void AssertNuGetVersion(string text, string[] components, int missingComponentCount, bool revisionPresent, string revision)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.NuGet));
        Assert.That(result.GetVersion().GetNumericComponentCount(), Is.EqualTo(components.Length));
        Assert.That(result.GetVersion().GetMissingComponentCount(), Is.EqualTo(missingComponentCount));
        Assert.That(result.GetVersion().HasRevisionComponent(), Is.EqualTo(revisionPresent));
        Assert.That(result.GetVersion().GetRevisionComponent(), Is.EqualTo(revision));

        int index = 0;
        while (index < components.Length)
        {
            Assert.That(result.GetVersion().GetNumericComponent(index), Is.EqualTo(components[index]));
            index = index + 1;
        }
    }

    static void AssertNuGetFailure(string text)
    {
        VersionParseResult result = NuGetVersionParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
    }
}
