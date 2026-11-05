using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionInspectionTests
{
    [Test]
    public void Inspection_ExposesVersionStateThroughThePublicContract()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion version = Parse(service, "v1.2.3-alpha.7+build.9", VersionDialect.Go);

        Assert.That(inspection.GetOriginalText(version), Is.EqualTo("v1.2.3-alpha.7+build.9"));
        Assert.That(inspection.GetDialect(version), Is.EqualTo(VersionDialect.Go));
        Assert.That(inspection.IsStable(version), Is.False);
        Assert.That(inspection.IsPrerelease(version), Is.True);
        Assert.That(inspection.HasBuildMetadata(version), Is.True);
        Assert.That(inspection.GetNumericComponentCount(version), Is.EqualTo(3));
        Assert.That(inspection.GetNumericComponent(version, 1), Is.EqualTo("2"));
        Assert.That(inspection.GetNumericComponents(version), Is.EqualTo(new string[] { "1", "2", "3" }));
        Assert.That(inspection.GetPrereleaseIdentifierCount(version), Is.EqualTo(2));
        Assert.That(inspection.GetPrereleaseIdentifier(version, 0).text, Is.EqualTo("alpha"));
        Assert.That(inspection.GetPrereleaseIdentifier(version, 1).numeric, Is.True);
        Assert.That(inspection.GetBuildIdentifierCount(version), Is.EqualTo(2));
        Assert.That(inspection.GetBuildIdentifier(version, 0).text, Is.EqualTo("build"));
        Assert.That(inspection.GetMissingComponentCount(version), Is.EqualTo(0));
        Assert.That(inspection.HasLeadingVPrefix(version), Is.True);
        Assert.That(inspection.HasRevisionComponent(version), Is.False);
        Assert.That(inspection.GetRevisionComponent(version), Is.Empty);
        Assert.That(inspection.HasPythonVersion(version), Is.False);
    }

    [Test]
    public void Inspection_ExposesShorthandRevisionAndPythonMetadata()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion shorthand = Parse(service, "v4", VersionDialect.Go);
        UniversalVersion missing = Parse(service, "1.2", VersionDialect.NuGet);
        UniversalVersion revision = Parse(service, "1.2.3.0004", VersionDialect.NuGet);
        UniversalVersion python = Parse(service, "2!01.0.dev3+Linux.1", VersionDialect.Python);

        Assert.That(inspection.IsStable(shorthand), Is.True);
        Assert.That(inspection.GetMissingComponentCount(shorthand), Is.EqualTo(2));
        Assert.That(inspection.GetNumericComponents(shorthand), Is.EqualTo(new string[] { "4", "0", "0" }));
        Assert.That(inspection.HasLeadingVPrefix(shorthand), Is.True);
        Assert.That(inspection.GetMissingComponentCount(missing), Is.EqualTo(1));
        Assert.That(inspection.GetNumericComponents(missing), Is.EqualTo(new string[] { "1", "2" }));
        Assert.That(inspection.HasLeadingVPrefix(missing), Is.False);
        Assert.That(inspection.HasRevisionComponent(missing), Is.False);
        Assert.That(inspection.GetRevisionComponent(missing), Is.Empty);
        Assert.That(inspection.HasRevisionComponent(revision), Is.True);
        Assert.That(inspection.GetRevisionComponent(revision), Is.EqualTo("0004"));
        Assert.That(inspection.GetMissingComponentCount(revision), Is.EqualTo(0));
        Assert.That(inspection.GetNumericComponents(revision), Is.EqualTo(new string[] { "1", "2", "3" }));
        Assert.That(inspection.HasPythonVersion(python), Is.True);
        Assert.That(inspection.IsPrerelease(python), Is.True);
        Assert.That(inspection.GetPythonVersion(python).epoch, Is.EqualTo("2"));
        Assert.That(inspection.GetPythonVersion(python).developmentReleaseNumber, Is.EqualTo("3"));
    }

    [Test]
    public void Inspection_ArrayAccessorsReturnDefensiveCopies()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion version = Parse(service, "1.2.3-alpha.1+build.2", VersionDialect.SemVer);

        string[] components = inspection.GetNumericComponents(version);
        VersionIdentifier[] prerelease = inspection.GetPrereleaseIdentifiers(version);
        VersionIdentifier[] build = inspection.GetBuildIdentifiers(version);

        Assert.That(inspection.GetNumericComponents(version), Is.Not.SameAs(components));
        Assert.That(inspection.GetPrereleaseIdentifiers(version), Is.Not.SameAs(prerelease));
        Assert.That(inspection.GetBuildIdentifiers(version), Is.Not.SameAs(build));

        components[0] = "9";
        prerelease[0] = build[0];
        build[0] = prerelease[1];

        Assert.That(inspection.GetNumericComponent(version, 0), Is.EqualTo("1"));
        Assert.That(inspection.GetPrereleaseIdentifier(version, 0).text, Is.EqualTo("alpha"));
        Assert.That(inspection.GetBuildIdentifier(version, 0).text, Is.EqualTo("build"));
    }

    [Test]
    public void Inspection_ProvidesExplicitEqualityModes()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion buildOne = Parse(service, "1.2.3-alpha+build.1", VersionDialect.SemVer);
        UniversalVersion buildTwo = Parse(service, "1.2.3-alpha+build.2", VersionDialect.SemVer);
        UniversalVersion differentPrerelease = Parse(service, "1.2.3-beta+build.1", VersionDialect.SemVer);

        Assert.That(inspection.EqualsByPrecedence(buildOne, buildTwo), Is.True);
        Assert.That(inspection.EqualsByIdentity(buildOne, buildTwo), Is.False);
        Assert.That(inspection.EqualsByNormalizedIdentity(buildOne, buildTwo), Is.True);
        Assert.That(inspection.EqualsByPrecedence(buildOne, differentPrerelease), Is.False);
        Assert.That(inspection.EqualsByIdentity(buildOne, buildOne), Is.True);
    }

    [Test]
    public void Inspection_PreservesVeryLargeNumericComponentsAndIdentifiers()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        string hugeMajor = new string('9', 600);
        string hugePrerelease = new string('8', 700);
        string firstText = hugeMajor + ".2.3-" + hugePrerelease + "+build.1";
        string secondText = hugeMajor + ".2.3-" + hugePrerelease + "+build.2";
        UniversalVersion first = Parse(service, firstText, VersionDialect.SemVer);
        UniversalVersion second = Parse(service, secondText, VersionDialect.SemVer);
        UniversalVersion different = Parse(service, new string('1', 601) + ".2.3-" + hugePrerelease, VersionDialect.SemVer);

        Assert.That(inspection.GetOriginalText(first), Is.EqualTo(firstText));
        Assert.That(inspection.GetNumericComponent(first, 0), Is.EqualTo(hugeMajor));
        Assert.That(inspection.GetPrereleaseIdentifier(first, 0).numeric, Is.True);
        Assert.That(inspection.GetPrereleaseIdentifier(first, 0).text, Is.EqualTo(hugePrerelease));
        Assert.That(inspection.EqualsByPrecedence(first, second), Is.True);
        Assert.That(inspection.EqualsByIdentity(first, second), Is.False);
        Assert.That(inspection.EqualsByNormalizedIdentity(first, second), Is.True);
        Assert.That(inspection.EqualsByPrecedence(first, different), Is.False);
    }

    [Test]
    public void Inspection_ExposesCompletePythonMetadata()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        string text = "0002!01.0RC002.post-003.dev04+Ubuntu-001.foo0100";
        UniversalVersion version = Parse(service, text, VersionDialect.Python);
        PythonVersion python = inspection.GetPythonVersion(version);

        Assert.That(inspection.GetDialect(version), Is.EqualTo(VersionDialect.Python));
        Assert.That(inspection.IsStable(version), Is.False);
        Assert.That(inspection.IsPrerelease(version), Is.True);
        Assert.That(inspection.HasBuildMetadata(version), Is.False);
        Assert.That(inspection.HasPythonVersion(version), Is.True);
        Assert.That(python.originalText, Is.EqualTo(text));
        Assert.That(python.epoch, Is.EqualTo("2"));
        Assert.That(python.releaseSegments, Is.EqualTo(new string[] { "1", "0" }));
        Assert.That(python.preReleasePresent, Is.True);
        Assert.That(python.preReleaseLabel, Is.EqualTo("rc"));
        Assert.That(python.preReleaseNumber, Is.EqualTo("2"));
        Assert.That(python.postReleasePresent, Is.True);
        Assert.That(python.postReleaseNumber, Is.EqualTo("3"));
        Assert.That(python.developmentReleasePresent, Is.True);
        Assert.That(python.developmentReleaseNumber, Is.EqualTo("4"));
        Assert.That(python.localSegments.Length, Is.EqualTo(3));
        Assert.That(python.localSegments[0].text, Is.EqualTo("ubuntu"));
        Assert.That(python.localSegments[0].numeric, Is.False);
        Assert.That(python.localSegments[1].text, Is.EqualTo("001"));
        Assert.That(python.localSegments[1].numeric, Is.True);
        Assert.That(python.localSegments[2].text, Is.EqualTo("foo0100"));
    }

    [Test]
    public void Formatting_ExposesEveryModeThroughThePublicContract()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        IVersionFormatting formatting = service;
        string text = "v01.002RC0003.post-0004.dev05+Ubuntu-001.foo0100";
        UniversalVersion version = Parse(service, text, VersionDialect.Python);

        Assert.That(formatting.FormatOriginal(version), Is.EqualTo(text));
        Assert.That(formatting.FormatDisplay(version), Is.EqualTo(text));
        Assert.That(formatting.FormatNormalized(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(formatting.FormatCanonical(version), Is.EqualTo("1.2rc3.post4.dev5+ubuntu.1.foo0100"));
        Assert.That(inspection.GetOriginalText(version), Is.EqualTo(formatting.FormatOriginal(version)));
    }

    [Test]
    public void Inspection_ReportsEmptyCollectionsAndUnavailableMetadata()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion version = Parse(service, "1.2.3", VersionDialect.SemVer);

        Assert.That(inspection.IsStable(version), Is.True);
        Assert.That(inspection.IsPrerelease(version), Is.False);
        Assert.That(inspection.HasBuildMetadata(version), Is.False);
        Assert.That(inspection.GetPrereleaseIdentifierCount(version), Is.EqualTo(0));
        Assert.That(inspection.GetPrereleaseIdentifiers(version), Is.Empty);
        Assert.That(inspection.GetBuildIdentifierCount(version), Is.EqualTo(0));
        Assert.That(inspection.GetBuildIdentifiers(version), Is.Empty);
        Assert.That(inspection.HasPythonVersion(version), Is.False);
        Assert.That(
            delegate
            {
                inspection.GetPythonVersion(version);
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void Inspection_RejectsOutOfRangeComponentAndIdentifierIndices()
    {
        VersionService service = new VersionService();
        IVersionInspection inspection = service;
        UniversalVersion version = Parse(service, "1.2.3-alpha+build", VersionDialect.SemVer);

        Assert.That(
            delegate
            {
                inspection.GetNumericComponent(version, 3);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(
            delegate
            {
                inspection.GetPrereleaseIdentifier(version, -1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(
            delegate
            {
                inspection.GetBuildIdentifier(version, 1);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    static UniversalVersion Parse(IVersionParsing parsing, string text, VersionDialect dialect)
    {
        VersionParseOptions options = new VersionParseOptions(
            dialect,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            PrereleasePolicy.Default);

        return parsing.Parse(text, options);
    }
}
