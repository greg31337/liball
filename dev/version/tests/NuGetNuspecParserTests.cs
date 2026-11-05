using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetNuspecParserTests
{
    [Test]
    public void ParsePackageMetadata_ReadsNamespacedNuspecPackageAttributes()
    {
        NuGetPackageMetadata metadata = NuGetNuspecParser.ParsePackageMetadata(CreateNamespacedNuspec());

        Assert.That(metadata.GetIdentity().GetPackageId().GetValue(), Is.EqualTo("Example.Package"));
        Assert.That(metadata.GetIdentity().GetPackageId().GetNormalizedValue(), Is.EqualTo("example.package"));
        Assert.That(metadata.GetIdentity().GetNormalizedVersionText(), Is.EqualTo("1.2.0"));
        Assert.That(metadata.GetTitle(), Is.EqualTo("Example title"));
        Assert.That(metadata.GetAuthors(), Is.EqualTo("Alice;Bob"));
        Assert.That(metadata.GetOwners(), Is.EqualTo("Example Owners"));
        Assert.That(metadata.GetDescription(), Is.EqualTo("Example description"));
        Assert.That(metadata.GetTags(), Is.EqualTo("json test"));
        Assert.That(metadata.GetProjectUrl(), Is.EqualTo("https://example.test/project"));
        Assert.That(metadata.GetLicenseUrl(), Is.EqualTo("https://example.test/license"));
        Assert.That(metadata.GetMinClientVersion(), Is.EqualTo("3.5"));
        Assert.That(metadata.RequiresLicenseAcceptance(), Is.True);
        Assert.That(metadata.IsDevelopmentDependency(), Is.False);
    }

    [Test]
    public void ParsePackageMetadata_ReadsDirectAndGroupedDependencies()
    {
        NuGetPackageMetadata metadata = NuGetNuspecParser.ParsePackageMetadata(CreateNuspecWithDependencyGroups());

        Assert.That(metadata.GetDependencyGroupCount(), Is.EqualTo(3));

        NuGetDependencyGroup direct = metadata.GetDependencyGroup(0);
        Assert.That(direct.HasTargetFramework(), Is.False);
        Assert.That(direct.GetDependencyCount(), Is.EqualTo(2));
        Assert.That(direct.GetDependency(0).GetPackageId().MatchesText("Dependency.A"), Is.True);
        Assert.That(direct.GetDependency(0).AllowsVersionText("1.5.0"), Is.True);
        Assert.That(direct.GetDependency(0).AllowsVersionText("2.0.0"), Is.False);
        Assert.That(direct.GetDependency(1).GetPackageId().MatchesText("Dependency.B"), Is.True);
        Assert.That(direct.GetDependency(1).AllowsVersionText("999.0.0"), Is.True);

        NuGetDependencyGroup net8 = metadata.GetDependencyGroup(1);
        Assert.That(net8.HasTargetFramework(), Is.True);
        Assert.That(net8.GetTargetFramework(), Is.EqualTo("net8.0"));
        Assert.That(net8.GetDependencyCount(), Is.EqualTo(1));
        Assert.That(net8.GetDependency(0).GetPackageId().MatchesText("Dependency.C"), Is.True);
        Assert.That(net8.GetDependency(0).AllowsVersionText("2.0.0"), Is.True);

        NuGetDependencyGroup netstandard = metadata.GetDependencyGroup(2);
        Assert.That(netstandard.HasTargetFramework(), Is.True);
        Assert.That(netstandard.GetTargetFramework(), Is.EqualTo("netstandard2.0"));
        Assert.That(netstandard.IsEmpty(), Is.True);
    }

    [Test]
    public void ParseDependencyGroups_FacadeReturnsOnlyDependencyGroups()
    {
        NuGetDependencyGroup[] groups = NuGetVersions.ParseDependencyGroups(CreateNuspecWithDependencyGroups());

        Assert.That(groups.Length, Is.EqualTo(3));
        Assert.That(groups[0].HasTargetFramework(), Is.False);
        Assert.That(groups[1].GetTargetFramework(), Is.EqualTo("net8.0"));
        Assert.That(groups[2].GetTargetFramework(), Is.EqualTo("netstandard2.0"));

        groups[0] = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[0]);
        NuGetDependencyGroup[] groupsAgain = NuGetVersions.ParseDependencyGroups(CreateNuspecWithDependencyGroups());

        Assert.That(groupsAgain[0].GetDependencyCount(), Is.EqualTo(2));
    }

    [Test]
    public void ParsePackageMetadata_FacadeExposesNuspecMetadataParser()
    {
        NuGetPackageMetadata metadata = NuGetVersions.ParsePackageMetadata(CreateNamespacedNuspec());

        Assert.That(metadata.GetIdentity().GetIdentityText(), Is.EqualTo("example.package 1.2.0"));
    }

    [Test]
    public void PackageMetadata_DefensivelyCopiesDependencyGroups()
    {
        NuGetDependency dependency = NuGetDependency.Create("Dependency.A", "[1.0.0,2.0.0)");
        NuGetDependencyGroup[] groups = new NuGetDependencyGroup[]
        {
            NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { dependency })
        };
        NuGetPackageMetadata metadata = new NuGetPackageMetadata(
            NuGetPackageVersionIdentity.Create("Example.Package", "1.0.0"),
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
            groups);

        groups[0] = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[0]);
        NuGetDependencyGroup[] returned = metadata.GetDependencyGroups();
        returned[0] = NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[0]);

        Assert.That(metadata.GetDependencyGroup(0).GetDependencyCount(), Is.EqualTo(1));
    }

    [Test]
    public void ParsePackageMetadata_RejectsMissingRequiredMetadataAndInvalidDependencyRanges()
    {
        Assert.That(
            delegate
            {
                NuGetNuspecParser.ParsePackageMetadata("<package><metadata><version>1.0.0</version></metadata></package>");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetNuspecParser.ParsePackageMetadata("<package><metadata><id>Example</id><version>bad</version></metadata></package>");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetNuspecParser.ParsePackageMetadata("<package><metadata><id>Example</id><version>1.0.0</version><dependencies><dependency id=\"Dependency.A\" version=\"[2.0.0,1.0.0]\" /></dependencies></metadata></package>");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void ParsePackageMetadata_RejectsInvalidBooleanValuesAndMalformedXml()
    {
        Assert.That(
            delegate
            {
                NuGetNuspecParser.ParsePackageMetadata("<package><metadata><id>Example</id><version>1.0.0</version><requireLicenseAcceptance>yes</requireLicenseAcceptance></metadata></package>");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetNuspecParser.ParsePackageMetadata("<package><metadata></package>");
            },
            Throws.TypeOf<System.Xml.XmlException>());
    }

    static string CreateNamespacedNuspec()
    {
        return @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"">
  <metadata minClientVersion=""3.5"">
    <id> Example.Package </id>
    <version>01.002.0000+build.5</version>
    <title>Example title</title>
    <authors>Alice;Bob</authors>
    <owners>Example Owners</owners>
    <description>Example description</description>
    <tags>json test</tags>
    <projectUrl>https://example.test/project</projectUrl>
    <licenseUrl>https://example.test/license</licenseUrl>
    <requireLicenseAcceptance>TRUE</requireLicenseAcceptance>
    <developmentDependency>false</developmentDependency>
  </metadata>
</package>";
    }

    static string CreateNuspecWithDependencyGroups()
    {
        return @"<package>
  <metadata>
    <id>Example.Package</id>
    <version>1.0.0</version>
    <dependencies>
      <dependency id=""Dependency.A"" version=""[1.0.0,2.0.0)"" />
      <dependency id=""Dependency.B"" />
      <group targetFramework=""net8.0"">
        <dependency id=""Dependency.C"" version=""2.0.0"" />
      </group>
      <group targetFramework=""netstandard2.0"" />
    </dependencies>
  </metadata>
</package>";
    }
}
