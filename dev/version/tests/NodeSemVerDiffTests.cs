using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerDiffTests
{
    [Test]
    public void Diff_ReportsCoreComponentChanges()
    {
        Assert.That(NodeSemVer.Diff("1.2.3", "2.0.0"), Is.EqualTo("major"));
        Assert.That(NodeSemVer.Diff("1.2.3", "1.3.0"), Is.EqualTo("minor"));
        Assert.That(NodeSemVer.Diff("1.2.3", "1.2.4"), Is.EqualTo("patch"));

        Assert.That(NodeSemVer.DiffKind("1.2.3", "2.0.0"), Is.EqualTo(NodeSemVerDiffKind.Major));
        Assert.That(NodeSemVer.DiffKind("1.2.3", "1.3.0"), Is.EqualTo(NodeSemVerDiffKind.Minor));
        Assert.That(NodeSemVer.DiffKind("1.2.3", "1.2.4"), Is.EqualTo(NodeSemVerDiffKind.Patch));
    }

    [Test]
    public void Diff_PrefersCoreComponentChangesOverPrereleaseChanges()
    {
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1", "2.0.0-beta.1"), Is.EqualTo("major"));
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1", "1.3.0-beta.1"), Is.EqualTo("minor"));
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1", "1.2.4-beta.1"), Is.EqualTo("patch"));
    }

    [Test]
    public void Diff_ReportsPrereleaseChangesWhenCoreComponentsMatch()
    {
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1", "1.2.3-alpha.2"), Is.EqualTo("prerelease"));
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1", "1.2.3"), Is.EqualTo("prerelease"));
        Assert.That(NodeSemVer.DiffKind("1.2.3-alpha.1", "1.2.3"), Is.EqualTo(NodeSemVerDiffKind.Prerelease));
    }

    [Test]
    public void Diff_ReportsBuildOnlyChangesWhenPrecedenceIsEqual()
    {
        Assert.That(NodeSemVer.Compare("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo(0));
        Assert.That(NodeSemVer.CompareBuild("1.2.3+build.1", "1.2.3+build.2"), Is.Not.EqualTo(0));
        Assert.That(NodeSemVer.Diff("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo("build"));
        Assert.That(NodeSemVer.DiffKind("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo(NodeSemVerDiffKind.Build));
    }

    [Test]
    public void Diff_ReturnsNullAndNoneWhenIdentityMatches()
    {
        Assert.That(NodeSemVer.Diff("1.2.3-alpha.1+build.5", "1.2.3-alpha.1+build.5"), Is.Null);
        Assert.That(NodeSemVer.DiffKind("1.2.3-alpha.1+build.5", "1.2.3-alpha.1+build.5"), Is.EqualTo(NodeSemVerDiffKind.None));
    }

    [Test]
    public void Diff_RejectsLooseOrMalformedInput()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.Diff("= v1.2.3", "1.2.3");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NodeSemVer.Diff("1.2", "1.2.3");
            },
            Throws.TypeOf<System.ArgumentException>());
    }
}
