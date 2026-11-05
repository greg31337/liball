using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetPackageIdentityModelTests
{
    [Test]
    public void PackageId_NormalizesCaseAndPreservesOriginalText()
    {
        NuGetPackageId packageId = new NuGetPackageId("Newtonsoft.Json");

        Assert.That(packageId.GetValue(), Is.EqualTo("Newtonsoft.Json"));
        Assert.That(packageId.GetNormalizedValue(), Is.EqualTo("newtonsoft.json"));
        Assert.That(packageId.EqualsId(new NuGetPackageId("newtonsoft.json")), Is.True);
        Assert.That(packageId.MatchesText("NEWTONSOFT.JSON"), Is.True);
    }

    [Test]
    public void PackageId_RejectsNullEmptyTooLongAndInvalidCharacters()
    {
        Assert.That(
            delegate
            {
                new NuGetPackageId(null!);
            },
            Throws.TypeOf<ArgumentNullException>());

        Assert.That(
            delegate
            {
                new NuGetPackageId("");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new NuGetPackageId(new string('a', NuGetPackageId.MaxLength + 1));
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new NuGetPackageId("bad/package");
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void PackageVersionIdentity_UsesNormalizedNuGetVersionForIdentity()
    {
        NuGetPackageVersionIdentity left = NuGetPackageVersionIdentity.Create("Package.Id", "01.002.0003.0000+build.1");
        NuGetPackageVersionIdentity right = NuGetPackageVersionIdentity.Create("package.id", "1.2.3+build.2");

        Assert.That(left.GetPackageId().GetNormalizedValue(), Is.EqualTo("package.id"));
        Assert.That(left.GetNormalizedVersionText(), Is.EqualTo("1.2.3"));
        Assert.That(left.GetIdentityText(), Is.EqualTo("package.id 1.2.3"));
        Assert.That(left.HasSameIdentity(right), Is.True);
    }

    [Test]
    public void PackageVersionIdentity_RejectsInvalidOrNonNuGetVersions()
    {
        UniversalVersion semver = SemVerParser.ParseStrict("1.2.3").GetVersion();

        Assert.That(
            delegate
            {
                NuGetPackageVersionIdentity.Create("Package.Id", "bad");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new NuGetPackageVersionIdentity(new NuGetPackageId("Package.Id"), semver);
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SourceIdentity_NormalizesNameAndSourceCase()
    {
        NuGetSourceIdentity source = new NuGetSourceIdentity("NuGet.org", "HTTPS://API.NUGET.ORG/V3/INDEX.JSON");
        NuGetSourceIdentity equivalent = new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json");

        Assert.That(source.GetName(), Is.EqualTo("NuGet.org"));
        Assert.That(source.GetSource(), Is.EqualTo("HTTPS://API.NUGET.ORG/V3/INDEX.JSON"));
        Assert.That(source.GetNormalizedName(), Is.EqualTo("nuget.org"));
        Assert.That(source.GetNormalizedSource(), Is.EqualTo("https://api.nuget.org/v3/index.json"));
        Assert.That(source.EqualsSource(equivalent), Is.True);
    }

    [Test]
    public void MetadataEnums_ExposeNuGetStatesReasonsAndSeverityLevels()
    {
        Assert.That(Enum.GetNames(typeof(NuGetListedState)), Is.EqualTo(new string[]
        {
            "Unknown",
            "Listed",
            "Unlisted"
        }));

        Assert.That(Enum.GetNames(typeof(NuGetPackageDeprecationReason)), Is.EqualTo(new string[]
        {
            "Other",
            "Legacy",
            "CriticalBugs"
        }));

        Assert.That(Enum.GetNames(typeof(NuGetPackageVulnerabilitySeverity)), Is.EqualTo(new string[]
        {
            "Unknown",
            "Low",
            "Moderate",
            "High",
            "Critical"
        }));
    }

    [Test]
    public void DeprecationMetadata_CarriesReasonsMessageAndAlternatePackage()
    {
        NuGetPackageVersionIdentity alternate = NuGetPackageVersionIdentity.Create("Replacement", "2.0.0");
        NuGetPackageDeprecationReason[] reasons = new NuGetPackageDeprecationReason[]
        {
            NuGetPackageDeprecationReason.Legacy,
            NuGetPackageDeprecationReason.CriticalBugs
        };

        NuGetPackageDeprecationMetadata metadata = NuGetPackageDeprecationMetadata.CreateDeprecated(reasons, "Use Replacement.", alternate);
        reasons[0] = NuGetPackageDeprecationReason.Other;

        Assert.That(metadata.IsDeprecated(), Is.True);
        Assert.That(metadata.GetReasonCount(), Is.EqualTo(2));
        Assert.That(metadata.GetReason(0), Is.EqualTo(NuGetPackageDeprecationReason.Legacy));
        Assert.That(metadata.HasMessage(), Is.True);
        Assert.That(metadata.GetMessage(), Is.EqualTo("Use Replacement."));
        Assert.That(metadata.HasAlternatePackage(), Is.True);
        Assert.That(metadata.GetAlternatePackage().HasSameIdentity(alternate), Is.True);

        NuGetPackageDeprecationReason[] returned = metadata.GetReasons();
        returned[1] = NuGetPackageDeprecationReason.Other;

        Assert.That(metadata.GetReason(1), Is.EqualTo(NuGetPackageDeprecationReason.CriticalBugs));
    }

    [Test]
    public void DeprecationMetadata_NotDeprecatedRejectsDeprecationPayload()
    {
        NuGetPackageDeprecationMetadata notDeprecated = NuGetPackageDeprecationMetadata.CreateNotDeprecated();

        Assert.That(notDeprecated.IsDeprecated(), Is.False);
        Assert.That(notDeprecated.GetReasonCount(), Is.EqualTo(0));
        Assert.That(notDeprecated.HasMessage(), Is.False);
        Assert.That(notDeprecated.HasAlternatePackage(), Is.False);
        Assert.That(
            delegate
            {
                notDeprecated.GetAlternatePackage();
            },
            Throws.TypeOf<InvalidOperationException>());

        Assert.That(
            delegate
            {
                new NuGetPackageDeprecationMetadata(false, new NuGetPackageDeprecationReason[] { NuGetPackageDeprecationReason.Other }, "", null);
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void VulnerabilityMetadata_CarriesSeverityAndAdvisoryUrl()
    {
        NuGetPackageVulnerabilityMetadata metadata = new NuGetPackageVulnerabilityMetadata(
            NuGetPackageVulnerabilitySeverity.High,
            "https://github.com/advisories/GHSA-1234");

        Assert.That(metadata.GetSeverity(), Is.EqualTo(NuGetPackageVulnerabilitySeverity.High));
        Assert.That(metadata.GetAdvisoryUrl(), Is.EqualTo("https://github.com/advisories/GHSA-1234"));

        Assert.That(
            delegate
            {
                new NuGetPackageVulnerabilityMetadata((NuGetPackageVulnerabilitySeverity)99, "https://example.test");
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void DependencyAndGroups_CarryNuGetRangeAndDefensivelyCopyDependencies()
    {
        NuGetDependency dependency = NuGetDependency.Create("Newtonsoft.Json", "[13.0.1,14.0.0)");
        NuGetDependency[] dependencies = new NuGetDependency[] { dependency };
        NuGetDependencyGroup group = NuGetDependencyGroup.Create("net8.0", dependencies);

        dependencies[0] = NuGetDependency.CreateAnyVersion("Other.Package");

        Assert.That(dependency.GetPackageId().GetNormalizedValue(), Is.EqualTo("newtonsoft.json"));
        Assert.That(dependency.GetVersionRangeText(), Is.EqualTo("[13.0.1,14.0.0)"));
        Assert.That(dependency.AllowsVersionText("13.0.1"), Is.True);
        Assert.That(dependency.AllowsVersionText("14.0.0"), Is.False);
        Assert.That(group.HasTargetFramework(), Is.True);
        Assert.That(group.GetTargetFramework(), Is.EqualTo("net8.0"));
        Assert.That(group.GetDependencyCount(), Is.EqualTo(1));
        Assert.That(group.GetDependency(0).GetPackageId().MatchesText("newtonsoft.json"), Is.True);

        NuGetDependency[] returned = group.GetDependencies();
        returned[0] = NuGetDependency.CreateAnyVersion("Other.Package");

        Assert.That(group.GetDependency(0).GetPackageId().MatchesText("newtonsoft.json"), Is.True);
    }

    [Test]
    public void DependencyGroups_RepresentAnyFrameworkAndEmptyGroups()
    {
        NuGetDependencyGroup group = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[0]);

        Assert.That(group.HasTargetFramework(), Is.False);
        Assert.That(group.GetTargetFramework(), Is.EqualTo(""));
        Assert.That(group.IsEmpty(), Is.True);

        Assert.That(
            delegate
            {
                new NuGetDependencyGroup(null, new NuGetDependency[] { null! });
            },
            Throws.TypeOf<ArgumentException>());
    }
}
