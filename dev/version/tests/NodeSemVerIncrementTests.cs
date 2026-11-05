using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerIncrementTests
{
    [Test]
    public void Increment_BumpsStableMajorMinorAndPatchVersions()
    {
        Assert.That(NodeSemVer.Major("1.2.3"), Is.EqualTo("2.0.0"));
        Assert.That(NodeSemVer.Minor("1.2.3"), Is.EqualTo("1.3.0"));
        Assert.That(NodeSemVer.Patch("1.2.3"), Is.EqualTo("1.2.4"));

        Assert.That(NodeSemVer.Increment("1.2.3", NodeSemVerIncrementReleaseType.Major), Is.EqualTo("2.0.0"));
        Assert.That(NodeSemVer.Increment("1.2.3", NodeSemVerIncrementReleaseType.Minor), Is.EqualTo("1.3.0"));
        Assert.That(NodeSemVer.Increment("1.2.3", NodeSemVerIncrementReleaseType.Patch), Is.EqualTo("1.2.4"));
    }

    [Test]
    public void Increment_PromotesPrereleaseVersionsForMajorMinorPatchAndRelease()
    {
        Assert.That(NodeSemVer.Major("1.0.0-alpha.1"), Is.EqualTo("1.0.0"));
        Assert.That(NodeSemVer.Major("1.2.3-alpha.1"), Is.EqualTo("2.0.0"));
        Assert.That(NodeSemVer.Minor("1.2.0-alpha.1"), Is.EqualTo("1.2.0"));
        Assert.That(NodeSemVer.Minor("1.2.3-alpha.1"), Is.EqualTo("1.3.0"));
        Assert.That(NodeSemVer.Patch("1.2.3-alpha.1"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Release("1.2.3-alpha.1"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Release("1.2.3"), Is.Null);
    }

    [Test]
    public void Increment_PreReleaseHelpersCreateInitialNumericPrerelease()
    {
        Assert.That(NodeSemVer.Premajor("1.2.3"), Is.EqualTo("2.0.0-0"));
        Assert.That(NodeSemVer.Preminor("1.2.3"), Is.EqualTo("1.3.0-0"));
        Assert.That(NodeSemVer.Prepatch("1.2.3"), Is.EqualTo("1.2.4-0"));
        Assert.That(NodeSemVer.Prerelease("1.2.3"), Is.EqualTo("1.2.4-0"));
    }

    [Test]
    public void Increment_PrereleaseBumpsExistingNumericIdentifier()
    {
        Assert.That(NodeSemVer.Prerelease("1.2.3-0"), Is.EqualTo("1.2.3-1"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-alpha.9"), Is.EqualTo("1.2.3-alpha.10"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-alpha"), Is.EqualTo("1.2.3-alpha.0"));
    }

    [Test]
    public void Increment_CustomPrereleaseIdentifiersCanBeProvidedDirectly()
    {
        Assert.That(NodeSemVer.Premajor("1.2.3", "beta"), Is.EqualTo("2.0.0-beta.0"));
        Assert.That(NodeSemVer.Preminor("1.2.3", "beta"), Is.EqualTo("1.3.0-beta.0"));
        Assert.That(NodeSemVer.Prepatch("1.2.3", "beta"), Is.EqualTo("1.2.4-beta.0"));
        Assert.That(NodeSemVer.Prerelease("1.2.3", "beta"), Is.EqualTo("1.2.4-beta.0"));
        Assert.That(NodeSemVer.Prerelease("1.2.4-beta.0", "beta"), Is.EqualTo("1.2.4-beta.1"));
        Assert.That(NodeSemVer.Prerelease("1.2.4-beta.1", "rc"), Is.EqualTo("1.2.4-rc.0"));
    }

    [Test]
    public void Increment_OptionsControlLooseModeIdentifierAndIdentifierBase()
    {
        NodeSemVerIncrementOptions loosePatch = NodeSemVerIncrementOptions.CreateLoose(NodeSemVerIncrementReleaseType.Patch);
        NodeSemVerIncrementOptions betaOne = NodeSemVerIncrementOptions.CreateWithIdentifierBase(NodeSemVerIncrementReleaseType.Prerelease, "beta", 1);
        NodeSemVerIncrementOptions betaNoBase = NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prepatch, "beta");

        betaNoBase.DisableIdentifierBase();

        Assert.That(NodeSemVer.Increment("v01.002.0003", loosePatch), Is.EqualTo("1.2.4"));
        Assert.That(NodeSemVer.Increment("1.2.3", betaOne), Is.EqualTo("1.2.4-beta.1"));
        Assert.That(NodeSemVer.Increment("1.2.3", betaNoBase), Is.EqualTo("1.2.4-beta"));
    }

    [Test]
    public void Increment_RemovesBuildMetadataFromIncrementedVersions()
    {
        Assert.That(NodeSemVer.Patch("1.2.3+build.5"), Is.EqualTo("1.2.4"));
        Assert.That(NodeSemVer.Release("1.2.3-alpha.1+build.5"), Is.EqualTo("1.2.3"));
        Assert.That(NodeSemVer.Prerelease("1.2.3-alpha.1+build.5"), Is.EqualTo("1.2.3-alpha.2"));
    }

    [Test]
    public void Increment_ReturnsNullForInvalidStartingVersions()
    {
        Assert.That(NodeSemVer.Patch("1.2"), Is.Null);
        Assert.That(NodeSemVer.Patch("v1.2.3"), Is.Null);
        Assert.That(NodeSemVer.Patch(null), Is.Null);

        string tooLongVersion = "1.2.3+" + new string('a', NodeSemVerInputGuard.MaxInputLength + 1 - "1.2.3+".Length);
        Assert.That(NodeSemVer.Patch(tooLongVersion), Is.Null);
    }

    [Test]
    public void Increment_RejectsNullOptions()
    {
        NodeSemVerIncrementOptions? options = null;

        Assert.That(
            delegate
            {
                NodeSemVer.Increment("1.2.3", options!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }
}
