namespace uri.Tests;

public class PathAlgorithmTests
{
    [TestCase("/a/b/c/./../../g", "/a/g")]
    [TestCase("mid/content=5/../6", "mid/6")]
    public void RfcSectionFiveExamplesMatchTheTwoBufferAlgorithm(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [TestCase("../a", "a")]
    [TestCase("./a", "a")]
    [TestCase("../../a", "a")]
    [TestCase("./../a", "a")]
    [TestCase(".././../a", "a")]
    public void LeadingRelativeDotPrefixesAreRemovedByRuleA(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [TestCase("/./a", "/a")]
    [TestCase("/a/./b", "/a/b")]
    [TestCase("/a/.", "/a/")]
    [TestCase("a/.", "a/")]
    [TestCase("/./", "/")]
    public void CurrentDirectorySegmentsAreReplacedByRuleB(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [TestCase("/../a", "/a")]
    [TestCase("/a/../b", "/b")]
    [TestCase("/a/b/..", "/a/")]
    [TestCase("a/b/..", "a/")]
    [TestCase("a/..", "/")]
    [TestCase("/../", "/")]
    [TestCase("/../../a", "/a")]
    [TestCase("/a/b/../../../c", "/c")]
    public void ParentDirectorySegmentsRemoveOneOutputSegmentByRuleC(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [TestCase(".")]
    [TestCase("..")]
    [TestCase("./")]
    [TestCase("../")]
    [TestCase("../..")]
    public void PathsConsistingOnlyOfDotSegmentsBecomeEmpty(string path)
    {
        AssertRemoval(path, "");
    }

    [TestCase("")]
    [TestCase("a")]
    [TestCase("/a/b")]
    [TestCase("a:b@c")]
    [TestCase("/a//b///c/")]
    [TestCase("a//b///c/")]
    [TestCase("//a//b")]
    public void PathsWithoutDotSegmentsRemainCharacterForCharacter(string path)
    {
        AssertRemoval(path, path);
    }

    [TestCase("/a//./b", "/a//b")]
    [TestCase("/a//../b", "/a/b")]
    [TestCase("/a///../b", "/a//b")]
    [TestCase("a//./b", "a//b")]
    [TestCase("a//../b", "a/b")]
    [TestCase("//a/./b", "//a/b")]
    [TestCase("///../a", "//a")]
    public void NonDotEmptySegmentsArePreserved(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [TestCase("%2e")]
    [TestCase("%2E")]
    [TestCase("%2e%2e")]
    [TestCase(".%2e")]
    [TestCase("%2e.")]
    [TestCase("/%2e/%2E/%2e%2e/.%2e/%2e./a")]
    public void PercentEncodedDotsAreOrdinarySegmentData(string path)
    {
        AssertRemoval(path, path);
    }

    [TestCase("...")]
    [TestCase(".a")]
    [TestCase("a.")]
    [TestCase("..a")]
    [TestCase("a..")]
    [TestCase("/.../..a/.a/a./")]
    public void DotCharactersInsideNonDotSegmentsRemainData(string path)
    {
        AssertRemoval(path, path);
    }

    [TestCase("a/b/./c/../d", "a/b/d")]
    [TestCase("a/b/../c", "a/c")]
    [TestCase("../a/b", "a/b")]
    [TestCase("a/../../b", "/b")]
    [TestCase("a/b/../", "a/")]
    [TestCase("a/./", "a/")]
    public void RelativePathsFollowTheSameRfcBufferSemantics(
        string path,
        string expected)
    {
        AssertRemoval(path, expected);
    }

    [Test]
    public void EncodedSlashDoesNotCreateAPathSegmentBoundary()
    {
        AssertRemoval("a%2Fb/../c", "/c");
    }

    [Test]
    public void RemovalConsumesOnlyTheProvidedSpan()
    {
        const string path = "/a/b/../c";
        string padded = "before" + path + "after";

        string result = PathAlgorithm.RemoveDotSegments(
            padded.AsSpan(6, path.Length));

        Assert.That(result, Is.EqualTo("/a/c"));
    }

    [TestCase("/a/b/c/./../../g")]
    [TestCase("mid/content=5/../6")]
    [TestCase("/a//./b")]
    [TestCase("a/../../b")]
    [TestCase("/%2e/../a")]
    public void RemovalIsIdempotent(string path)
    {
        string once = PathAlgorithm.RemoveDotSegments(path.AsSpan());
        string twice = PathAlgorithm.RemoveDotSegments(once.AsSpan());

        Assert.That(twice, Is.EqualTo(once), path);
    }

    private static void AssertRemoval(string path, string expected)
    {
        string original = path;

        string result = PathAlgorithm.RemoveDotSegments(path.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expected), path);
            Assert.That(result.Length, Is.LessThanOrEqualTo(path.Length), path);
            Assert.That(path, Is.EqualTo(original), path);
        });
    }
}
