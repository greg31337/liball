using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerRangeAlgebraFacadeTests
{
    [Test]
    public void Intersects_UsesComparatorAlgebraForNodeRanges()
    {
        Assert.That(NodeSemVer.Intersects("^1.2.3", ">=1.5.0 <2.0.0"), Is.True);
        Assert.That(NodeSemVer.Intersects("^1.2.3", ">=2.0.0 <3.0.0"), Is.False);
        Assert.That(NodeSemVer.Intersects("<1.0.0 || >=2.0.0", "1.5.x"), Is.False);
        Assert.That(NodeSemVer.Intersects("<1.0.0 || >=2.0.0", "2.1.x"), Is.True);
    }

    [Test]
    public void Intersects_ReturnsFalseForInvalidInputs()
    {
        Assert.That(NodeSemVer.Intersects(null, ">=1.0.0"), Is.False);
        Assert.That(NodeSemVer.Intersects(">=1.0.0", null), Is.False);
        Assert.That(NodeSemVer.Intersects(">=bad", ">=1.0.0"), Is.False);
        Assert.That(NodeSemVer.Intersects(">=1.0.0", ">=bad"), Is.False);
    }

    [Test]
    public void Subset_UsesComparatorAlgebraForNodeRanges()
    {
        Assert.That(NodeSemVer.Subset(">=1.2.3 <2.0.0", ">=1.0.0 <3.0.0"), Is.True);
        Assert.That(NodeSemVer.Subset(">=1.0.0 <3.0.0", ">=1.2.3 <2.0.0"), Is.False);
        Assert.That(NodeSemVer.Subset(">=1.2.0 <1.3.0", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(NodeSemVer.Subset(">=1.0.0 <2.0.0", ">=1.2.0 <1.3.0"), Is.False);
    }

    [Test]
    public void Subset_ReturnsFalseForInvalidInputs()
    {
        Assert.That(NodeSemVer.Subset(null, ">=1.0.0"), Is.False);
        Assert.That(NodeSemVer.Subset(">=1.0.0", null), Is.False);
        Assert.That(NodeSemVer.Subset(">=bad", ">=1.0.0"), Is.False);
        Assert.That(NodeSemVer.Subset(">=1.0.0", ">=bad"), Is.False);
    }

    [Test]
    public void SimplifyRange_ReturnsUnionOfMatchingFiniteCandidates()
    {
        string[] candidates = new string[]
        {
            "1.0.0",
            "1.2.0",
            "1.2.1",
            "2.0.0"
        };

        Assert.That(NodeSemVer.SimplifyRange(candidates, "1.2.x"), Is.EqualTo("=1.2.0 || =1.2.1"));
        Assert.That(NodeSemVer.SimplifyRange("1.2.x", candidates), Is.EqualTo("=1.2.0 || =1.2.1"));
    }

    [Test]
    public void SimplifyRange_DeduplicatesEquivalentCandidatesAndReturnsEmptyWhenNoneMatch()
    {
        string[] duplicateCandidates = new string[]
        {
            "1.2.0",
            "1.2.0+build.1",
            "1.2.1"
        };
        string[] noMatches = new string[]
        {
            "1.0.0",
            "2.0.0"
        };

        Assert.That(NodeSemVer.SimplifyRange(duplicateCandidates, "1.2.x"), Is.EqualTo("=1.2.0 || =1.2.1"));
        Assert.That(NodeSemVer.SimplifyRange(noMatches, "1.2.x"), Is.EqualTo(""));
    }

    [Test]
    public void SimplifyRange_ReturnsNullForInvalidRangeOrCandidate()
    {
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "1.2.0" }, ">=bad"), Is.Null);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "bad" }, "1.2.x"), Is.Null);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { null! }, "1.2.x"), Is.Null);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "1.2.0" }, null), Is.Null);
    }

    [Test]
    public void SimplifyRange_RejectsNullCandidateArrays()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.SimplifyRange(null!, "1.2.x");
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                NodeSemVer.SimplifyRange("1.2.x", null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }
}
