using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerComparisonHelperTests
{
    [Test]
    public void CompareHelpers_UseStrictSemVerPrecedence()
    {
        Assert.That(NodeSemVer.Compare("1.2.3", "1.2.4"), Is.EqualTo(-1));
        Assert.That(NodeSemVer.RCompare("1.2.3", "1.2.4"), Is.EqualTo(1));
        Assert.That(NodeSemVer.Eq("1.2.3+build.1", "1.2.3+build.2"), Is.True);
        Assert.That(NodeSemVer.Neq("1.2.3", "1.2.4"), Is.True);
        Assert.That(NodeSemVer.Gt("1.2.4", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Gte("1.2.3", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Lt("1.2.3-alpha.1", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Lte("1.2.3", "1.2.3"), Is.True);
    }

    [Test]
    public void Compare_RejectsLooseOnlyInputsWhileCompareLooseAcceptsThem()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.Compare("= v1.2.3", "1.2.4");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(NodeSemVer.CompareLoose("= v1.2.3", "v1.2.4"), Is.EqualTo(-1));
    }

    [Test]
    public void Cmp_AppliesNodeSemVerComparisonOperators()
    {
        Assert.That(NodeSemVer.Cmp("1.2.3", "=", "1.2.3+build.1"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "==", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "===", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "!=", "1.2.4"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "!==", "1.2.4"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.4", ">", "1.2.3"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.4", ">=", "1.2.4"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "<", "1.2.4"), Is.True);
        Assert.That(NodeSemVer.Cmp("1.2.3", "<=", "1.2.3"), Is.True);
    }

    [Test]
    public void Cmp_RejectsInvalidOperators()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.Cmp("1.2.3", "~", "1.2.4");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NodeSemVer.Cmp("1.2.3", null!, "1.2.4");
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    [Test]
    public void CompareBuild_UsesBuildMetadataAfterPrecedence()
    {
        Assert.That(NodeSemVer.Compare("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo(0));
        Assert.That(NodeSemVer.CompareBuild("1.2.3+build.1", "1.2.3+build.2"), Is.EqualTo(-1));
        Assert.That(NodeSemVer.CompareBuild("1.2.4+build.0", "1.2.3+build.z"), Is.EqualTo(1));
    }

    [Test]
    public void ComparisonHelpers_RejectInvalidVersionInputs()
    {
        Assert.That(
            delegate
            {
                NodeSemVer.Eq("1.2", "1.2.0");
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NodeSemVer.CompareLoose("release 1.2.3", "1.2.3");
            },
            Throws.TypeOf<System.ArgumentException>());
    }
}
