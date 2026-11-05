using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenDependencyModelTests
{
    [Test]
    public void Dependency_CarriesScopeOptionalVersionRangeAndExclusions()
    {
        MavenDependencyExclusion exclusion = MavenDependencyExclusion.Create("com.example", "excluded");
        MavenDependencyExclusion[] exclusions = new MavenDependencyExclusion[] { exclusion };
        MavenDependency dependency = new MavenDependency(
            MavenArtifactCoordinate.Create("com.example", "runtime-lib", "jar", "tests"),
            "[1.0,2.0)",
            MavenDependencyScope.Runtime,
            true,
            exclusions);

        exclusions[0] = MavenDependencyExclusion.Create("com.example", "other");
        MavenDependencyExclusion[] returned = dependency.GetExclusions();
        returned[0] = MavenDependencyExclusion.Create("com.example", "third");

        Assert.That(dependency.GetGroupId().MatchesText("com.example"), Is.True);
        Assert.That(dependency.GetArtifactId().MatchesText("runtime-lib"), Is.True);
        Assert.That(dependency.GetPackaging(), Is.EqualTo("jar"));
        Assert.That(dependency.HasClassifier(), Is.True);
        Assert.That(dependency.GetClassifier(), Is.EqualTo("tests"));
        Assert.That(dependency.HasVersionRange(), Is.True);
        Assert.That(dependency.GetVersionRangeText(), Is.EqualTo("[1.0,2.0)"));
        Assert.That(dependency.AllowsVersionText("1.5"), Is.True);
        Assert.That(dependency.AllowsVersionText("2.0"), Is.False);
        Assert.That(dependency.GetScope(), Is.EqualTo(MavenDependencyScope.Runtime));
        Assert.That(dependency.IsOptional(), Is.True);
        Assert.That(dependency.GetExclusionCount(), Is.EqualTo(1));
        Assert.That(dependency.Excludes(MavenArtifactCoordinate.CreateJar("com.example", "excluded")), Is.True);
        Assert.That(dependency.Excludes(MavenArtifactCoordinate.CreateJar("com.example", "other")), Is.False);
    }

    [Test]
    public void Dependency_CreateHelpersUseCompileScopeAndAllowUnversionedDependencies()
    {
        MavenDependency versioned = MavenDependency.Create("com.example", "api", "[1.0]");
        MavenDependency unversioned = MavenDependency.CreateUnversioned("com.example", "managed");

        Assert.That(versioned.GetScope(), Is.EqualTo(MavenDependencyScope.Compile));
        Assert.That(versioned.IsOptional(), Is.False);
        Assert.That(versioned.GetPackaging(), Is.EqualTo("jar"));
        Assert.That(unversioned.HasVersionRange(), Is.False);
        Assert.That(unversioned.GetVersionRangeText(), Is.EqualTo(""));
        Assert.That(unversioned.AllowsVersionText("999.0"), Is.True);
        Assert.That(
            delegate
            {
                unversioned.GetVersionRange();
            },
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void DependencyManagement_CarriesManagedDependenciesAndImportedBomsDefensively()
    {
        MavenDependency managed = MavenDependency.Create("com.example", "managed", "[1.0]");
        MavenImportedBomConstraint bom = MavenImportedBomConstraint.Create("com.example", "bom", "1.0.0");
        MavenDependency[] managedDependencies = new MavenDependency[] { managed };
        MavenImportedBomConstraint[] boms = new MavenImportedBomConstraint[] { bom };
        MavenDependencyManagement management = new MavenDependencyManagement(managedDependencies, boms);

        managedDependencies[0] = MavenDependency.Create("com.example", "other", "[2.0]");
        boms[0] = MavenImportedBomConstraint.Create("com.example", "other-bom", "2.0.0");
        MavenDependency[] returnedManaged = management.GetManagedDependencies();
        MavenImportedBomConstraint[] returnedBoms = management.GetImportedBoms();
        returnedManaged[0] = MavenDependency.Create("com.example", "third", "[3.0]");
        returnedBoms[0] = MavenImportedBomConstraint.Create("com.example", "third-bom", "3.0.0");

        Assert.That(management.GetManagedDependencyCount(), Is.EqualTo(1));
        Assert.That(management.GetManagedDependency(0).GetArtifactId().MatchesText("managed"), Is.True);
        Assert.That(management.GetImportedBomCount(), Is.EqualTo(1));
        Assert.That(management.GetImportedBom(0).GetArtifactId().MatchesText("bom"), Is.True);
        Assert.That(management.GetImportedBom(0).GetIdentity().GetPackaging(), Is.EqualTo("pom"));
    }

    [Test]
    public void ParentReferenceAndDependencyModel_CarryInheritanceInputs()
    {
        MavenParentReference defaultParent = MavenParentReference.Create("com.example", "parent", "1.0.0");
        MavenParentReference explicitParent = MavenParentReference.CreateWithRelativePath(
            "com.example",
            "parent",
            "1.0.0",
            "../build/parent.xml");
        MavenDependency direct = MavenDependency.Create("com.example", "api", "[1.0]");
        MavenDependencyManagement management = new MavenDependencyManagement(
            new MavenDependency[] { MavenDependency.Create("com.example", "managed", "[2.0]") },
            new MavenImportedBomConstraint[] { MavenImportedBomConstraint.Create("com.example", "bom", "1.0.0") });
        MavenDependencyModel model = new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create("com.example", "app", "jar", null, "1.0.0"),
            explicitParent,
            new MavenDependency[] { direct },
            management);

        Assert.That(defaultParent.HasExplicitRelativePath(), Is.False);
        Assert.That(defaultParent.GetRelativePath(), Is.EqualTo("../pom.xml"));
        Assert.That(explicitParent.HasExplicitRelativePath(), Is.True);
        Assert.That(explicitParent.GetRelativePath(), Is.EqualTo("../build/parent.xml"));
        Assert.That(model.GetIdentity().GetArtifactId().MatchesText("app"), Is.True);
        Assert.That(model.HasParentReference(), Is.True);
        Assert.That(model.GetParentReference().GetArtifactId().MatchesText("parent"), Is.True);
        Assert.That(model.GetDependencyCount(), Is.EqualTo(1));
        Assert.That(model.GetDependency(0).GetArtifactId().MatchesText("api"), Is.True);
        Assert.That(model.GetDependencyManagement().GetManagedDependency(0).GetArtifactId().MatchesText("managed"), Is.True);
        Assert.That(model.GetDependencyManagement().GetImportedBom(0).GetArtifactId().MatchesText("bom"), Is.True);
    }

    [Test]
    public void DependencyModel_CreateUsesEmptyManagementAndNoParent()
    {
        MavenDependency direct = MavenDependency.Create("com.example", "api", "[1.0]");
        MavenDependency[] dependencies = new MavenDependency[] { direct };
        MavenDependencyModel model = MavenDependencyModel.Create(
            MavenArtifactVersionIdentity.Create("com.example", "app", "1.0.0"),
            dependencies);

        dependencies[0] = MavenDependency.Create("com.example", "other", "[2.0]");
        MavenDependency[] returned = model.GetDependencies();
        returned[0] = MavenDependency.Create("com.example", "third", "[3.0]");

        Assert.That(model.HasParentReference(), Is.False);
        Assert.That(model.GetDependencyManagement().GetManagedDependencyCount(), Is.EqualTo(0));
        Assert.That(model.GetDependencyManagement().GetImportedBomCount(), Is.EqualTo(0));
        Assert.That(model.GetDependency(0).GetArtifactId().MatchesText("api"), Is.True);
        Assert.That(
            delegate
            {
                model.GetParentReference();
            },
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Models_RejectInvalidScopesRangesArraysAndPomSpecificReferences()
    {
        Assert.That(
            delegate
            {
                new MavenDependency(
                    MavenArtifactCoordinate.CreateJar("com.example", "api"),
                    "[bad version]",
                    MavenDependencyScope.Compile,
                    false,
                    new MavenDependencyExclusion[0]);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenDependency(
                    MavenArtifactCoordinate.CreateJar("com.example", "api"),
                    "[1.0]",
                    (MavenDependencyScope)99,
                    false,
                    new MavenDependencyExclusion[0]);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new MavenDependency(
                    MavenArtifactCoordinate.CreateJar("com.example", "api"),
                    "[1.0]",
                    MavenDependencyScope.Compile,
                    false,
                    new MavenDependencyExclusion[] { null! });
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenDependencyManagement(new MavenDependency[] { null! }, new MavenImportedBomConstraint[0]);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenImportedBomConstraint(MavenArtifactVersionIdentity.Create("com.example", "bom", "jar", null, "1.0.0"));
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenParentReference(MavenArtifactVersionIdentity.Create("com.example", "parent", "jar", null, "1.0.0"), null);
            },
            Throws.TypeOf<ArgumentException>());
    }
}
