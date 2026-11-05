using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetResolverDiagnosticTests
{
    [Test]
    public void Resolve_ReportsMissingPackageMetadataDiagnostic()
    {
        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[] { NuGetDependency.Create("Missing", "[1.0.0]") },
            new NuGetSourcePackageMetadata[0]);

        AssertConflict(result, "Missing", NuGetResolverDiagnosticCodes.MissingPackageMetadata);
    }

    [Test]
    public void Resolve_ReportsUnsatisfiedConstraintsDiagnostic()
    {
        NuGetSourceIdentity source = Source("nuget");

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[2.0.0]") },
            new NuGetSourcePackageMetadata[] { Package("A", "1.0.0", source, new NuGetDependencyGroup[0]) });

        AssertConflict(result, "A", NuGetResolverDiagnosticCodes.UnsatisfiedConstraints);
    }

    [Test]
    public void Resolve_ReportsCyclicDependencyDiagnostic()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            source,
            new NuGetDependencyGroup[] { NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("B") }) });
        NuGetSourcePackageMetadata packageB = Package(
            "B",
            "1.0.0",
            source,
            new NuGetDependencyGroup[] { NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("A") }) });

        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA, packageB });

        AssertConflict(result, "A", NuGetResolverDiagnosticCodes.CyclicDependency);
    }

    [Test]
    public void Resolve_ReportsIncompatibleFrameworkDiagnostic()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            source,
            new NuGetDependencyGroup[]
            {
                NuGetDependencyGroup.Create("net9.0", new NuGetDependency[] { Dependency("FutureOnly") })
            });

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA },
            "net8.0");

        AssertConflict(result, "A", NuGetResolverDiagnosticCodes.IncompatibleFramework);
    }

    [Test]
    public void Resolve_ReportsAmbiguousSourceMatchesDiagnostic()
    {
        NuGetSourceIdentity nuget = Source("nuget");
        NuGetSourceIdentity mirror = Source("mirror");

        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[]
            {
                Package("A", "1.0.0", nuget, new NuGetDependencyGroup[0]),
                Package("A", "1.0.0", mirror, new NuGetDependencyGroup[0])
            });

        AssertConflict(result, "A", NuGetResolverDiagnosticCodes.AmbiguousSourceMatches);
    }

    [Test]
    public void DependencyConflict_DefaultConstructorUsesUnsatisfiedConstraintsDiagnostic()
    {
        NuGetDependencyConflict conflict = NuGetDependencyConflict.Create(
            NuGetPackageId.Create("A"),
            0,
            "No package satisfies the requested range.");

        Assert.That(conflict.GetDiagnosticCode(), Is.EqualTo(NuGetResolverDiagnosticCodes.UnsatisfiedConstraints));
        Assert.That(conflict.GetMessage(), Is.EqualTo("No package satisfies the requested range."));
    }

    static void AssertConflict(NuGetDependencyResolutionResult result, string packageId, string diagnosticCode)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetPackageId().MatchesText(packageId), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(diagnosticCode));
        Assert.That(result.GetConflict(0).GetMessage(), Is.Not.Empty);
    }

    static NuGetDependency Dependency(string packageId)
    {
        return NuGetDependency.CreateAnyVersion(packageId);
    }

    static NuGetSourcePackageMetadata Package(
        string packageId,
        string version,
        NuGetSourceIdentity source,
        NuGetDependencyGroup[] dependencyGroups)
    {
        NuGetPackageMetadata packageMetadata = new NuGetPackageMetadata(
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
            dependencyGroups);

        return NuGetSourcePackageMetadata.CreateAvailable(source, packageMetadata, NuGetListedState.Listed);
    }

    static NuGetSourceIdentity Source(string name)
    {
        return new NuGetSourceIdentity(name, "https://" + name + ".example.test/v3/index.json");
    }
}
