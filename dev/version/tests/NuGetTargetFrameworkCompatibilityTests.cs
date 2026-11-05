using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetTargetFrameworkCompatibilityTests
{
    [Test]
    public void IsCompatible_AppliesSameFamilyVersionRules()
    {
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0", "net8.0"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0", "net6.0"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0", "net9.0"), Is.False);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net48", "net472"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net472", "net48"), Is.False);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("netcoreapp3.1", "netcoreapp3.0"), Is.True);
    }

    [Test]
    public void IsCompatible_HandlesUnboundedNumericVersionParts()
    {
        string lower = "net9999999999999999999999999999999999999998.0";
        string higher = "net9999999999999999999999999999999999999999.0";

        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible(higher, higher), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible(higher, lower), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible(lower, higher), Is.False);
    }

    [Test]
    public void IsCompatible_AppliesNetStandardSupportBands()
    {
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0", "netstandard2.1"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("netcoreapp3.1", "netstandard2.1"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("netcoreapp2.1", "netstandard2.1"), Is.False);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net472", "netstandard2.0"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net45", "netstandard2.0"), Is.False);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net45", "netstandard1.1"), Is.True);
    }

    [Test]
    public void IsCompatible_RequiresCompatibleModernNetPlatform()
    {
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0-windows10.0.19041", "net8.0"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0-windows10.0.19041", "net8.0-windows10.0"), Is.True);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0", "net8.0-windows"), Is.False);
        Assert.That(NuGetTargetFrameworkCompatibility.IsCompatible("net8.0-android", "net8.0-windows"), Is.False);
    }

    [Test]
    public void IsCompatible_PortableProfileRequiresCandidateFrameworkSubset()
    {
        Assert.That(
            NuGetTargetFrameworkCompatibility.IsCompatible(
                "portable-net45+win8+wpa81",
                "portable-net45+win8"),
            Is.True);
        Assert.That(
            NuGetTargetFrameworkCompatibility.IsCompatible(
                "portable-net45+win8",
                "portable-net45+win8+wpa81"),
            Is.False);
    }

    [Test]
    public void IsCompatible_UsesFallbackFrameworkListAfterDirectCompatibilityFails()
    {
        Assert.That(NuGetVersions.IsTargetFrameworkCompatible("net8.0", "net48"), Is.False);
        Assert.That(NuGetVersions.IsTargetFrameworkCompatible("net8.0", "net48", "net48;netstandard2.0"), Is.True);
        Assert.That(NuGetVersions.IsTargetFrameworkCompatible("net8.0", "net48", "netstandard2.0"), Is.False);
        Assert.That(NuGetVersions.IsTargetFrameworkCompatible("not-a-framework", "net8.0"), Is.False);
    }

    [Test]
    public void SelectBestDependencyGroup_ChoosesExactOverCompatibleAndUntargetedGroups()
    {
        NuGetDependencyGroup any = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("Any") });
        NuGetDependencyGroup netstandard = NuGetDependencyGroup.Create("netstandard2.0", new NuGetDependency[] { Dependency("Standard") });
        NuGetDependencyGroup exact = NuGetDependencyGroup.Create("net8.0", new NuGetDependency[] { Dependency("Exact") });

        NuGetDependencyGroup? selected = NuGetVersions.SelectBestDependencyGroup(
            "net8.0",
            new NuGetDependencyGroup[] { any, netstandard, exact });

        AssertSelected(selected, "net8.0", "Exact");
    }

    [Test]
    public void SelectBestDependencyGroup_ChoosesNearestSameFamilyBeforeNetStandard()
    {
        NuGetDependencyGroup any = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("Any") });
        NuGetDependencyGroup netstandard = NuGetDependencyGroup.Create("netstandard2.0", new NuGetDependency[] { Dependency("Standard") });
        NuGetDependencyGroup net6 = NuGetDependencyGroup.Create("net6.0", new NuGetDependency[] { Dependency("Net6") });

        NuGetDependencyGroup? selected = NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            Parse("net7.0"),
            new NuGetDependencyGroup[] { any, netstandard, net6 });

        AssertSelected(selected, "net6.0", "Net6");
    }

    [Test]
    public void SelectBestDependencyGroup_FallsBackToUntargetedWhenNoTargetedGroupIsCompatible()
    {
        NuGetDependencyGroup any = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("Any") });
        NuGetDependencyGroup future = NuGetDependencyGroup.Create("net9.0", new NuGetDependency[] { Dependency("Future") });

        NuGetDependencyGroup? selected = NuGetVersions.SelectBestDependencyGroup(
            "net8.0",
            new NuGetDependencyGroup[] { future, any });

        Assert.That(selected, Is.SameAs(any));
    }

    [Test]
    public void SelectBestDependencyGroup_IgnoresInvalidTargetFrameworkGroups()
    {
        NuGetDependencyGroup invalid = NuGetDependencyGroup.Create("xamarin.ios10", new NuGetDependency[] { Dependency("Invalid") });
        NuGetDependencyGroup compatible = NuGetDependencyGroup.Create("netstandard2.0", new NuGetDependency[] { Dependency("Standard") });

        NuGetDependencyGroup? selected = NuGetVersions.SelectBestDependencyGroup(
            "net8.0",
            new NuGetDependencyGroup[] { invalid, compatible });

        AssertSelected(selected, "netstandard2.0", "Standard");
        Assert.That(NuGetVersions.SelectBestDependencyGroup("bad", new NuGetDependencyGroup[] { compatible }), Is.Null);
    }

    [Test]
    public void SelectBestDependencyGroup_CanSelectFromPackageMetadata()
    {
        NuGetDependencyGroup any = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("Any") });
        NuGetDependencyGroup selectedGroup = NuGetDependencyGroup.Create("netstandard2.0", new NuGetDependency[] { Dependency("Standard") });
        NuGetPackageMetadata metadata = Metadata(new NuGetDependencyGroup[] { any, selectedGroup });

        NuGetDependencyGroup? selected = NuGetVersions.SelectBestDependencyGroup("net472", metadata);

        Assert.That(selected, Is.SameAs(selectedGroup));
    }

    static NuGetTargetFramework Parse(string text)
    {
        NuGetTargetFrameworkParseResult result = NuGetTargetFrameworkParser.Parse(text);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetFramework();
    }

    static NuGetDependency Dependency(string id)
    {
        return NuGetDependency.CreateAnyVersion(id);
    }

    static NuGetPackageMetadata Metadata(NuGetDependencyGroup[] dependencyGroups)
    {
        return new NuGetPackageMetadata(
            NuGetPackageVersionIdentity.Create("Package", "1.0.0"),
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
    }

    static void AssertSelected(NuGetDependencyGroup? selected, string targetFramework, string dependencyId)
    {
        System.ArgumentNullException.ThrowIfNull(selected);
        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetTargetFramework(), Is.EqualTo(targetFramework));
        Assert.That(selected.GetDependencyCount(), Is.EqualTo(1));
        Assert.That(selected.GetDependency(0).GetPackageId().GetValue(), Is.EqualTo(dependencyId));
    }
}
