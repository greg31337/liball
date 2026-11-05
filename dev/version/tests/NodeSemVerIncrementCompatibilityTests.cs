using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerIncrementCompatibilityTests
{
    [Test]
    public void IncrementCompatibility_CoversZeroMajorBehavior()
    {
        Assert.That(NodeSemVer.Major("0.1.2"), Is.EqualTo("1.0.0"));
        Assert.That(NodeSemVer.Minor("0.1.2"), Is.EqualTo("0.2.0"));
        Assert.That(NodeSemVer.Patch("0.1.2"), Is.EqualTo("0.1.3"));
        Assert.That(NodeSemVer.Premajor("0.1.2"), Is.EqualTo("1.0.0-0"));
        Assert.That(NodeSemVer.Preminor("0.1.2"), Is.EqualTo("0.2.0-0"));
        Assert.That(NodeSemVer.Prepatch("0.1.2"), Is.EqualTo("0.1.3-0"));
    }

    [Test]
    public void IncrementCompatibility_CoversZeroMajorPrereleasePromotion()
    {
        Assert.That(NodeSemVer.Major("0.0.0-alpha.1"), Is.EqualTo("0.0.0"));
        Assert.That(NodeSemVer.Major("0.1.0-alpha.1"), Is.EqualTo("1.0.0"));
        Assert.That(NodeSemVer.Minor("0.1.0-alpha.1"), Is.EqualTo("0.1.0"));
        Assert.That(NodeSemVer.Patch("0.1.2-alpha.1"), Is.EqualTo("0.1.2"));
        Assert.That(NodeSemVer.Release("0.1.2-alpha.1"), Is.EqualTo("0.1.2"));
    }

    [Test]
    public void IncrementCompatibility_CoversIdentifierNumbering()
    {
        NodeSemVerIncrementOptions betaOne = NodeSemVerIncrementOptions.CreateWithIdentifierBase(NodeSemVerIncrementReleaseType.Prerelease, "beta", 1);
        NodeSemVerIncrementOptions betaNoBase = NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Preminor, "beta");

        betaNoBase.DisableIdentifierBase();

        Assert.That(NodeSemVer.Prerelease("1.2.3-beta.0", "beta"), Is.EqualTo("1.2.3-beta.1"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-beta.9", "beta"), Is.EqualTo("1.2.3-beta.10"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-beta.9", "rc"), Is.EqualTo("1.2.3-rc.0"));
        Assert.That(NodeSemVer.Increment("1.2.3", betaOne), Is.EqualTo("1.2.4-beta.1"));
        Assert.That(NodeSemVer.Increment("1.2.3", betaNoBase), Is.EqualTo("1.3.0-beta"));
    }

    [Test]
    public void IncrementCompatibility_RemovesBuildMetadataForEveryReleaseShape()
    {
        Assert.That(NodeSemVer.Major("1.2.3+build.1"), Is.EqualTo("2.0.0"));
        Assert.That(NodeSemVer.Minor("1.2.3+build.1"), Is.EqualTo("1.3.0"));
        Assert.That(NodeSemVer.Patch("1.2.3+build.1"), Is.EqualTo("1.2.4"));
        Assert.That(NodeSemVer.Prepatch("1.2.3+build.1"), Is.EqualTo("1.2.4-0"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-alpha.1+build.1"), Is.EqualTo("1.2.3-alpha.2"));
        Assert.That(NodeSemVer.Release("1.2.3-alpha.1+build.1"), Is.EqualTo("1.2.3"));
    }

    [Test]
    public void IncrementCompatibility_RejectsInvalidReleaseTypes()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.Increment("1.2.3", (NodeSemVerIncrementReleaseType)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        NodeSemVerIncrementOptions options = NodeSemVerIncrementOptions.CreateDefault();
        Assert.That(
            delegate
            {
                options.SetReleaseType((NodeSemVerIncrementReleaseType)999);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void IncrementCompatibility_ReturnsNullForInvalidStartingVersions()
    {
        Assert.That(NodeSemVer.Major(""), Is.Null);
        Assert.That(NodeSemVer.Minor("1"), Is.Null);
        Assert.That(NodeSemVer.Patch("1.2"), Is.Null);
        Assert.That(NodeSemVer.Prepatch("version 1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Prerelease("v1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Release("1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Release(null), Is.Null);
    }
}
