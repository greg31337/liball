using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetSourceMetadataModelTests
{
    [Test]
    public void SourceMetadataEnums_ExposeAvailabilityStates()
    {
        Assert.That(Enum.GetNames(typeof(NuGetPackageAvailability)), Is.EqualTo(new string[]
        {
            "Unknown",
            "Available",
            "Unavailable"
        }));
    }

    [Test]
    public void SourcePriority_OrdersEnabledSourcesBeforeDisabledAndLowerPriorityFirst()
    {
        NuGetPackageSourcePriority primary = NuGetPackageSourcePriority.CreateEnabled(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            0);
        NuGetPackageSourcePriority secondary = NuGetPackageSourcePriority.CreateEnabled(
            new NuGetSourceIdentity("internal", "https://packages.example.test/v3/index.json"),
            1);
        NuGetPackageSourcePriority disabled = NuGetPackageSourcePriority.CreateDisabled(
            new NuGetSourceIdentity("disabled", "https://disabled.example.test/v3/index.json"),
            0);

        Assert.That(primary.GetPriority(), Is.EqualTo(0));
        Assert.That(primary.IsEnabled(), Is.True);
        Assert.That(disabled.IsEnabled(), Is.False);
        Assert.That(primary.ComesBefore(secondary), Is.True);
        Assert.That(secondary.ComesBefore(primary), Is.False);
        Assert.That(primary.ComesBefore(disabled), Is.True);
        Assert.That(disabled.ComesBefore(primary), Is.False);
    }

    [Test]
    public void SourcePriority_UsesNormalizedSourceAsStableTieBreaker()
    {
        NuGetPackageSourcePriority a = NuGetPackageSourcePriority.CreateEnabled(
            new NuGetSourceIdentity("A", "https://a.example.test/index.json"),
            0);
        NuGetPackageSourcePriority b = NuGetPackageSourcePriority.CreateEnabled(
            new NuGetSourceIdentity("B", "https://b.example.test/index.json"),
            0);

        Assert.That(a.ComesBefore(b), Is.True);
        Assert.That(b.ComesBefore(a), Is.False);
    }

    [Test]
    public void SourcePackageMetadata_CarriesSourceLocalAvailabilityListedStateAndMetadata()
    {
        NuGetSourceIdentity source = new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json");
        NuGetPackageMetadata packageMetadata = CreatePackageMetadata("Example.Package", "1.0.0");
        NuGetSourcePackageMetadata sourceMetadata = NuGetSourcePackageMetadata.CreateAvailable(
            source,
            packageMetadata,
            NuGetListedState.Listed);

        Assert.That(sourceMetadata.GetSource().EqualsSource(source), Is.True);
        Assert.That(sourceMetadata.GetIdentity().HasSameIdentity(packageMetadata.GetIdentity()), Is.True);
        Assert.That(sourceMetadata.GetListedState(), Is.EqualTo(NuGetListedState.Listed));
        Assert.That(sourceMetadata.IsListed(), Is.True);
        Assert.That(sourceMetadata.GetAvailability(), Is.EqualTo(NuGetPackageAvailability.Available));
        Assert.That(sourceMetadata.IsAvailable(), Is.True);
        Assert.That(sourceMetadata.GetDeprecationMetadata().IsDeprecated(), Is.False);
        Assert.That(sourceMetadata.HasVulnerabilities(), Is.False);
    }

    [Test]
    public void SourcePackageMetadata_DefensivelyCopiesVulnerabilities()
    {
        NuGetPackageVulnerabilityMetadata high = new NuGetPackageVulnerabilityMetadata(
            NuGetPackageVulnerabilitySeverity.High,
            "https://example.test/high");
        NuGetPackageVulnerabilityMetadata low = new NuGetPackageVulnerabilityMetadata(
            NuGetPackageVulnerabilitySeverity.Low,
            "https://example.test/low");
        NuGetPackageVulnerabilityMetadata[] vulnerabilities = new NuGetPackageVulnerabilityMetadata[] { high };

        NuGetSourcePackageMetadata metadata = new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            CreatePackageMetadata("Example.Package", "1.0.0"),
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            vulnerabilities);

        vulnerabilities[0] = low;
        NuGetPackageVulnerabilityMetadata[] returned = metadata.GetVulnerabilities();
        returned[0] = low;

        Assert.That(metadata.HasVulnerabilities(), Is.True);
        Assert.That(metadata.GetVulnerabilityCount(), Is.EqualTo(1));
        Assert.That(metadata.GetVulnerability(0).GetSeverity(), Is.EqualTo(NuGetPackageVulnerabilitySeverity.High));
    }

    [Test]
    public void RejectionPolicy_DefaultRejectsUnavailableButAllowsUnlistedDeprecatedAndVulnerablePackages()
    {
        NuGetPackageRejectionPolicy policy = NuGetPackageRejectionPolicy.CreateDefault();

        Assert.That(policy.ShouldRejectUnavailablePackages(), Is.True);
        Assert.That(policy.ShouldRejectUnlistedPackages(), Is.False);
        Assert.That(policy.ShouldRejectDeprecatedPackages(), Is.False);
        Assert.That(policy.ShouldRejectVulnerablePackages(), Is.False);

        Assert.That(policy.ShouldReject(CreateUnavailablePackage()), Is.True);
        Assert.That(policy.ShouldReject(CreateUnlistedPackage()), Is.False);
        Assert.That(policy.ShouldReject(CreateDeprecatedPackage()), Is.False);
        Assert.That(policy.ShouldReject(CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity.Critical)), Is.False);
    }

    [Test]
    public void RejectionPolicy_StrictRejectsUnlistedDeprecatedAndVulnerablePackages()
    {
        NuGetPackageRejectionPolicy policy = NuGetPackageRejectionPolicy.CreateStrict();

        Assert.That(CreateUnlistedPackage().IsRejectedBy(policy), Is.True);
        Assert.That(CreateDeprecatedPackage().IsRejectedBy(policy), Is.True);
        Assert.That(CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity.Low).IsRejectedBy(policy), Is.True);
    }

    [Test]
    public void RejectionPolicy_UsesMinimumVulnerabilitySeverityThreshold()
    {
        NuGetPackageRejectionPolicy policy = NuGetPackageRejectionPolicy.CreateDefault();
        policy.SetRejectVulnerablePackages(true);
        policy.SetMinimumRejectedVulnerabilitySeverity(NuGetPackageVulnerabilitySeverity.High);

        Assert.That(policy.ShouldReject(CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity.Moderate)), Is.False);
        Assert.That(policy.ShouldReject(CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity.High)), Is.True);
        Assert.That(policy.ShouldReject(CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity.Critical)), Is.True);
    }

    [Test]
    public void SourceMetadataModels_RejectInvalidEnumValuesAndNullArrays()
    {
        Assert.That(
            delegate
            {
                new NuGetPackageSourcePriority(new NuGetSourceIdentity("source", "https://example.test"), -1, true);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new NuGetSourcePackageMetadata(
                    new NuGetSourceIdentity("source", "https://example.test"),
                    CreatePackageMetadata("Example.Package", "1.0.0"),
                    (NuGetListedState)99,
                    NuGetPackageAvailability.Available,
                    NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
                    new NuGetPackageVulnerabilityMetadata[0]);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new NuGetSourcePackageMetadata(
                    new NuGetSourceIdentity("source", "https://example.test"),
                    CreatePackageMetadata("Example.Package", "1.0.0"),
                    NuGetListedState.Listed,
                    (NuGetPackageAvailability)99,
                    NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
                    new NuGetPackageVulnerabilityMetadata[0]);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new NuGetSourcePackageMetadata(
                    new NuGetSourceIdentity("source", "https://example.test"),
                    CreatePackageMetadata("Example.Package", "1.0.0"),
                    NuGetListedState.Listed,
                    NuGetPackageAvailability.Available,
                    NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
                    new NuGetPackageVulnerabilityMetadata[] { null! });
            },
            Throws.TypeOf<ArgumentException>());
    }

    static NuGetPackageMetadata CreatePackageMetadata(string packageId, string version)
    {
        return new NuGetPackageMetadata(
            NuGetPackageVersionIdentity.Create(packageId, version),
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            false,
            false,
            new NuGetDependencyGroup[0]);
    }

    static NuGetSourcePackageMetadata CreateUnavailablePackage()
    {
        return new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            CreatePackageMetadata("Example.Package", "1.0.0"),
            NuGetListedState.Listed,
            NuGetPackageAvailability.Unavailable,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0]);
    }

    static NuGetSourcePackageMetadata CreateUnlistedPackage()
    {
        return new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            CreatePackageMetadata("Example.Package", "1.0.0"),
            NuGetListedState.Unlisted,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0]);
    }

    static NuGetSourcePackageMetadata CreateDeprecatedPackage()
    {
        return new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            CreatePackageMetadata("Example.Package", "1.0.0"),
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateDeprecated(
                new NuGetPackageDeprecationReason[] { NuGetPackageDeprecationReason.Legacy },
                "Use a newer package.",
                null),
            new NuGetPackageVulnerabilityMetadata[0]);
    }

    static NuGetSourcePackageMetadata CreateVulnerablePackage(NuGetPackageVulnerabilitySeverity severity)
    {
        return new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("nuget.org", "https://api.nuget.org/v3/index.json"),
            CreatePackageMetadata("Example.Package", "1.0.0"),
            NuGetListedState.Listed,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[]
            {
                new NuGetPackageVulnerabilityMetadata(severity, "https://example.test/advisory")
            });
    }
}
