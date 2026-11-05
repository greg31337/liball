namespace uri.Tests;

public class Rfc3986DotSegmentTests
{
    [TestCase("http://a/b/c/./g", "http://a/b/c/g")]
    [TestCase("http://a/b/c/.", "http://a/b/c/")]
    [TestCase("http://a/b/c/./", "http://a/b/c/")]
    [TestCase("http://a/b/c/..", "http://a/b/")]
    [TestCase("http://a/b/c/../", "http://a/b/")]
    [TestCase("http://a/b/c/../g", "http://a/b/g")]
    [TestCase("http://a/b/c/../..", "http://a/")]
    [TestCase("http://a/b/c/../../", "http://a/")]
    [TestCase("http://a/b/c/../../g", "http://a/g")]
    public void SectionFiveNormalMergedPathsRemoveDotSegments(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    [TestCase("http://a/b/c/../../../g", "http://a/g")]
    [TestCase("http://a/b/c/../../../../g", "http://a/g")]
    [TestCase("http://a/./g", "http://a/g")]
    [TestCase("http://a/../g", "http://a/g")]
    [TestCase("http://a/b/c/./../g", "http://a/b/g")]
    [TestCase("http://a/b/c/./g/.", "http://a/b/c/g/")]
    [TestCase("http://a/b/c/g/./h", "http://a/b/c/g/h")]
    [TestCase("http://a/b/c/g/../h", "http://a/b/c/h")]
    [TestCase("http://a/b/c/g;x=1/./y", "http://a/b/c/g;x=1/y")]
    [TestCase("http://a/b/c/g;x=1/../y", "http://a/b/c/y")]
    public void SectionFiveAbnormalMergedPathsRemoveOnlyCompleteDotSegments(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    [TestCase("http://a/b/c/g.")]
    [TestCase("http://a/b/c/.g")]
    [TestCase("http://a/b/c/g..")]
    [TestCase("http://a/b/c/..g")]
    public void SectionFiveAbnormalDotLookalikesRemainPathData(string input)
    {
        AssertNormalization(input, input);
    }

    [TestCase("http://a/b/c/g?y/./x")]
    [TestCase("http://a/b/c/g?y/../x")]
    [TestCase("http://a/b/c/g#s/./x")]
    [TestCase("http://a/b/c/g#s/../x")]
    [TestCase("http://a/b/c/g?y/../x#s/./z")]
    public void DotSegmentsInQueryAndFragmentAreNotTreatedAsPathSegments(
        string input)
    {
        AssertNormalization(input, input);
    }

    [TestCase("x:/a//./b", "x:/a//b")]
    [TestCase("x:/a//../b", "x:/a/b")]
    [TestCase("x:/a///../b", "x:/a//b")]
    [TestCase("x:/a/././b", "x:/a/b")]
    [TestCase("x:/a//b/../c", "x:/a//c")]
    [TestCase("x:/a///b/../../c", "x:/a//c")]
    [TestCase("x:////a/./b", "x:////a/b")]
    public void RemovingDotsPreservesOrdinaryEmptySegments(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    [TestCase("x:/.", "x:/")]
    [TestCase("x:/..", "x:/")]
    [TestCase("x:/../g", "x:/g")]
    [TestCase("x:/../../g", "x:/g")]
    [TestCase("x:/a/../../g", "x:/g")]
    [TestCase("x:/a/b/../../../g", "x:/g")]
    [TestCase("x:/a/..", "x:/")]
    [TestCase("x:/a/b/../..", "x:/")]
    public void ParentSegmentsCannotMoveAnAbsolutePathAboveItsRoot(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    [TestCase("./a", "a")]
    [TestCase("../a", "a")]
    [TestCase("../../a", "a")]
    [TestCase("a/./b", "a/b")]
    [TestCase("a/b/../c", "a/c")]
    [TestCase("a/b/..", "a/")]
    [TestCase("a/..", "/")]
    [TestCase("a/../../b", "/b")]
    [TestCase(".././../a", "a")]
    [TestCase(".", "")]
    [TestCase("..", "")]
    public void RelativePathsFollowTheRfcTwoBufferAlgorithm(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    [TestCase("x:/a/%2e/b", "x:/a/b")]
    [TestCase("x:/a/%2E%2e/b", "x:/b")]
    [TestCase("x:/a/.%2e/b", "x:/b")]
    [TestCase("x:/a/%2e./b", "x:/b")]
    [TestCase("x:/a/%2eX/b", "x:/a/.X/b")]
    [TestCase("x:/a/X%2e/b", "x:/a/X./b")]
    [TestCase("x:/a/%2e%2eX/b", "x:/a/..X/b")]
    [TestCase("x:/a/%252e/b", "x:/a/%252e/b")]
    [TestCase("x:/a%2F../b", "x:/a%2F../b")]
    public void PercentNormalizationCreatesDotsButNotEncodedSeparators(
        string input,
        string expected)
    {
        AssertNormalization(input, expected);
    }

    private static void AssertNormalization(string input, string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        string actual = normalizer.NormalizeSyntax(
            input,
            new UriNormalizationOptions());

        Assert.That(actual, Is.EqualTo(expected), input);
    }
}
