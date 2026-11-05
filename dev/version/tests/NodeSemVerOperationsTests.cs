using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerOperationsTests
{
    [Test]
    public void ValidationCleaningParsingAndCoercion_AreAvailableThroughTheContract()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions strict = new NodeSemVerCleanOptions(false);
        NodeSemVerCleanOptions loose = new NodeSemVerCleanOptions(true);

        NodeSemVerValidResult strictValid = operations.Valid("1.2.3-alpha.1", strict);
        NodeSemVerValidResult strictInvalid = operations.Valid("v1.2.3", strict);
        NodeSemVerValidResult looseValid = operations.Valid("= v01.002.0003", loose);
        NodeSemVerValidResult cleaned = operations.Clean(" = v1.2.3+build.5 ", strict);
        VersionParseResult parsed = operations.Parse("= v1.2.3");
        VersionParseResult coerced = operations.Coerce("release 4.05");

        Assert.That(strictValid.valid, Is.True);
        Assert.That(strictValid.versionText, Is.EqualTo("1.2.3-alpha.1"));
        Assert.That(strictInvalid.valid, Is.False);
        Assert.That(strictInvalid.diagnostics, Has.Length.EqualTo(1));
        Assert.That(looseValid.valid, Is.True);
        Assert.That(looseValid.versionText, Is.EqualTo("1.2.3"));
        Assert.That(cleaned.versionText, Is.EqualTo("1.2.3+build.5"));
        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(parsed.version!.leadingVPrefix, Is.True);
        Assert.That(coerced.success, Is.True);
        Assert.That(coerced.version!.numericComponents, Is.EqualTo(new string[] { "4", "5", "0" }));
    }

    [Test]
    public void Accessors_ExposeStrictVersionComponentsAndIdentifiers()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string text = "12345678901234567890.2.3-alpha.1+build.05";

        Assert.That(operations.GetMajor(text), Is.EqualTo("12345678901234567890"));
        Assert.That(operations.GetMinor(text), Is.EqualTo("2"));
        Assert.That(operations.GetPatch(text), Is.EqualTo("3"));
        Assert.That(operations.GetPrerelease(text), Is.EqualTo(new string[] { "alpha", "1" }));
        Assert.That(operations.GetPrereleaseText(text), Is.EqualTo("alpha.1"));
        Assert.That(operations.GetBuildMetadata(text), Is.EqualTo(new string[] { "build", "05" }));
        Assert.That(operations.GetBuildMetadataText(text), Is.EqualTo("build.05"));
        Assert.That(operations.GetMajor("v1.2.3"), Is.Null);
    }

    [Test]
    public void ComparisonAndDiffHelpers_DistinguishPrecedenceAndBuildIdentity()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Equal("1.2.3+one", "1.2.3+two"), Is.True);
        Assert.That(operations.NotEqual("1.2.3", "1.2.4"), Is.True);
        Assert.That(operations.GreaterThan("1.2.4", "1.2.3"), Is.True);
        Assert.That(operations.GreaterThanOrEqual("1.2.3", "1.2.3"), Is.True);
        Assert.That(operations.LessThan("1.2.3-alpha", "1.2.3"), Is.True);
        Assert.That(operations.LessThanOrEqual("1.2.3", "1.2.3"), Is.True);
        Assert.That(operations.CompareByOperator("1.2.4", ">", "1.2.3"), Is.True);
        Assert.That(operations.Compare("1.2.3", "1.2.4"), Is.LessThan(0));
        Assert.That(operations.ReverseCompare("1.2.3", "1.2.4"), Is.GreaterThan(0));
        Assert.That(operations.CompareLoose("= v1.2.3", "v1.2.4"), Is.LessThan(0));
        Assert.That(operations.CompareBuild("1.2.3+one", "1.2.3+two"), Is.Not.Zero);
        Assert.That(operations.Diff("1.2.3+one", "1.2.3+two"), Is.EqualTo("build"));
        Assert.That(
            operations.DiffKind("1.2.3-alpha", "1.2.3"),
            Is.EqualTo(NodeSemVerDiffKind.Prerelease));
    }

    [Test]
    public void IncrementHelpers_CoverEveryReleaseMode()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.IncrementMajor("1.2.3"), Is.EqualTo("2.0.0"));
        Assert.That(operations.IncrementMinor("1.2.3"), Is.EqualTo("1.3.0"));
        Assert.That(operations.IncrementPatch("1.2.3"), Is.EqualTo("1.2.4"));
        Assert.That(operations.IncrementPremajor("1.2.3"), Is.EqualTo("2.0.0-0"));
        Assert.That(operations.IncrementPreminor("1.2.3"), Is.EqualTo("1.3.0-0"));
        Assert.That(operations.IncrementPrepatch("1.2.3"), Is.EqualTo("1.2.4-0"));
        Assert.That(operations.IncrementPrerelease("1.2.3-alpha.1"), Is.EqualTo("1.2.3-alpha.2"));
        Assert.That(operations.IncrementRelease("1.2.3-alpha.1"), Is.EqualTo("1.2.3"));

        NodeSemVerIncrementOptions options = new NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType.Preminor);
        options.identifier = "beta";
        options.identifierBase = 1;
        Assert.That(operations.Increment("1.2.3", options), Is.EqualTo("1.3.0-beta.1"));
    }

    [Test]
    public void RangeHelpers_ParseNormalizeExpandAndEvaluateRanges()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        VersionRangeParseResult parsed = operations.ParseRange("^1.2.3 || ~2.0");
        string[][] comparators = operations.ToComparators("^1.2.3 || ~2.0");

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.range, Is.Not.Null);
        Assert.That(operations.Satisfies("1.5.0", "^1.2.3"), Is.True);
        Assert.That(operations.Satisfies("2.0.0", "^1.2.3"), Is.False);
        Assert.That(comparators, Has.Length.EqualTo(2));
        Assert.That(comparators[0], Is.EqualTo(new string[] { ">=1.2.3", "<2.0.0-0" }));
        Assert.That(comparators[1], Is.EqualTo(new string[] { ">=2.0.0", "<2.1.0-0" }));
        Assert.That(operations.MinVersion(">1.2.3 <2.0.0"), Is.EqualTo("1.2.4"));
        Assert.That(operations.ValidRange("^1.2.3"), Is.EqualTo(">=1.2.3 <2.0.0-0"));
    }

    [Test]
    public void RangeRelationsAndSimplification_AreAvailableThroughTheContract()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Outside("2.0.0", ">=1.0.0 <2.0.0", ">"), Is.True);
        Assert.That(operations.GreaterThanRange("2.0.0", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(operations.LessThanRange("0.9.9", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(operations.Intersects("^1.2.3", ">=1.5.0 <2.0.0"), Is.True);
        Assert.That(operations.Subset("1.2.x", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(
            operations.SimplifyRange(
                "1.2.x",
                new string[] { "1.0.0", "1.2.0", "1.2.1", "2.0.0" }),
            Is.EqualTo("=1.2.0 || =1.2.1"));
    }

    [Test]
    public void SatisfiesWithOptions_ControlsPrereleaseAdmission()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;

        Assert.That(operations.Satisfies("1.5.0-beta.1", ">=1.0.0"), Is.False);
        Assert.That(
            operations.SatisfiesWithOptions("1.5.0-beta.1", ">=1.0.0", include),
            Is.True);
    }

    [Test]
    public void Sorting_ReturnsDefensiveStableAscendingAndDescendingArrays()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] input = new string[]
        {
            "1.0.0+z",
            "2.0.0",
            "1.0.0-alpha",
            "1.0.0+a"
        };

        string[] ascending = operations.Sort(input);
        string[] descending = operations.ReverseSort(input);

        Assert.That(ascending, Is.EqualTo(new string[]
        {
            "1.0.0-alpha",
            "1.0.0+z",
            "1.0.0+a",
            "2.0.0"
        }));
        Assert.That(descending, Is.EqualTo(new string[]
        {
            "2.0.0",
            "1.0.0+z",
            "1.0.0+a",
            "1.0.0-alpha"
        }));
        Assert.That(input, Is.EqualTo(new string[]
        {
            "1.0.0+z",
            "2.0.0",
            "1.0.0-alpha",
            "1.0.0+a"
        }));
        Assert.That(ascending, Is.Not.SameAs(input));
        Assert.That(() => operations.Sort(new string[] { "1.0.0", "bad" }), Throws.ArgumentException);
    }

    [Test]
    public void FiniteCandidateSelection_SelectsMinAndMaxAndIgnoresInvalidCandidates()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] candidates = new string[]
        {
            "bad",
            "1.9.0",
            "1.2.3",
            "2.0.0",
            "1.5.0"
        };

        Assert.That(operations.MinSatisfying(candidates, "^1.2.0"), Is.EqualTo("1.2.3"));
        Assert.That(operations.MaxSatisfying(candidates, "^1.2.0"), Is.EqualTo("1.9.0"));
        Assert.That(operations.MaxSatisfying(candidates, ">=3.0.0"), Is.Null);
        Assert.That(operations.MinSatisfying(candidates, ">=bad"), Is.Null);
    }

    [Test]
    public void FiniteCandidateSelection_UsesExplicitPrereleaseOptions()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] candidates = new string[] { "1.5.0-alpha.1", "1.5.0-beta.1" };
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;

        Assert.That(operations.MinSatisfying(candidates, ">=1.0.0"), Is.Null);
        Assert.That(
            operations.MinSatisfyingWithOptions(candidates, ">=1.0.0", include),
            Is.EqualTo("1.5.0-alpha.1"));
        Assert.That(
            operations.MaxSatisfyingWithOptions(candidates, ">=1.0.0", include),
            Is.EqualTo("1.5.0-beta.1"));
    }
}
