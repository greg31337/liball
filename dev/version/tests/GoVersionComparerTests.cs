using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoVersionComparerTests
{
    [Test]
    public void Canonicalize_FillsMissingComponentsAndDropsBuildMetadata()
    {
        Assert.That(GoVersionComparer.Canonicalize("v1"), Is.EqualTo("v1.0.0"));
        Assert.That(GoVersionComparer.Canonicalize("v1.2"), Is.EqualTo("v1.2.0"));
        Assert.That(GoVersionComparer.Canonicalize("v1.2.3+build.5"), Is.EqualTo("v1.2.3"));
        Assert.That(GoVersionComparer.Canonicalize("v1.2.3-alpha.1+build.5"), Is.EqualTo("v1.2.3-alpha.1"));
        Assert.That(GoVersionComparer.Canonicalize("1.2.3"), Is.EqualTo(""));
    }

    [Test]
    public void ExtractionHelpers_ReturnGoVersionSuffixes()
    {
        Assert.That(GoVersionComparer.GetMajor("v2.1.0"), Is.EqualTo("v2"));
        Assert.That(GoVersionComparer.GetMajorMinor("v2.1.0"), Is.EqualTo("v2.1"));
        Assert.That(GoVersionComparer.GetPrerelease("v2.1.0-pre.1+meta.001"), Is.EqualTo("-pre.1"));
        Assert.That(GoVersionComparer.GetBuild("v2.1.0-pre.1+meta.001"), Is.EqualTo("+meta.001"));
        Assert.That(GoVersionComparer.GetMajor("2.1.0"), Is.EqualTo(""));
        Assert.That(GoVersionComparer.GetPrerelease("bad"), Is.EqualTo(""));
        Assert.That(GoVersionComparer.GetBuild("v2.1.0"), Is.EqualTo(""));
    }

    [Test]
    public void Compare_UsesSemVerPrecedenceWithGoInvalidOrdering()
    {
        Assert.That(GoVersionComparer.Compare("bad", "v1.0.0"), Is.EqualTo(-1));
        Assert.That(GoVersionComparer.Compare("v1.0.0", "bad"), Is.EqualTo(1));
        Assert.That(GoVersionComparer.Compare("bad", "also-bad"), Is.EqualTo(0));
        Assert.That(GoVersionComparer.Compare("v1.0.0-rc.1", "v1.0.0"), Is.EqualTo(-1));
        Assert.That(GoVersionComparer.Compare("v1.0.0+build.2", "v1.0.0+build.1"), Is.EqualTo(0));
    }

    [Test]
    public void SortAscending_OrdersInvalidThenSemanticVersionsWithOrdinalTieBreaks()
    {
        string[] sorted = GoVersionComparer.SortAscending(new string[]
        {
            "v1.0.0+build.2",
            "v1.0.0",
            "bad",
            "v0.9.0",
            "v1.0.0-rc.1",
            "v1.0.0+build.1"
        });

        Assert.That(sorted, Is.EqualTo(new string[]
        {
            "bad",
            "v0.9.0",
            "v1.0.0-rc.1",
            "v1.0.0",
            "v1.0.0+build.1",
            "v1.0.0+build.2"
        }));
    }

    [Test]
    public void SortDescending_ReversesAscendingOrder()
    {
        string[] sorted = GoVersionComparer.SortDescending(new string[]
        {
            "v1.0.0",
            "bad",
            "v2.0.0"
        });

        Assert.That(sorted, Is.EqualTo(new string[]
        {
            "v2.0.0",
            "v1.0.0",
            "bad"
        }));
    }
}
