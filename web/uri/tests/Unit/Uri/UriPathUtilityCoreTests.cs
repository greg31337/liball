namespace uri.Tests;

public class UriPathUtilityCoreTests
{
    [Test]
    public void SplitPreservesRootsTrailingAndRepeatedEmptySegments()
    {
        AssertSegments("", new string[0]);
        AssertSegments("/", new string[] { "", "" });
        AssertSegments("a/b", new string[] { "a", "b" });
        AssertSegments("/a//b/", new string[] { "", "a", "", "b", "" });
        AssertSegments("//a///", new string[] { "", "", "a", "", "", "" });
    }

    [Test]
    public void SplitTreatsEncodedReservedCharactersAsSegmentData()
    {
        AssertSegments(
            "/a%2Fb/%3F/%2f",
            new string[] { "", "a%2Fb", "%3F", "%2f" });
    }

    [Test]
    public void JoinCombinesTokensWithOneSharedSeamSeparator()
    {
        AssertJoin(new string[0], "");
        AssertJoin(new string[] { "" }, "");
        AssertJoin(new string[] { "/" }, "/");
        AssertJoin(new string[] { "/a/" }, "/a/");
        AssertJoin(new string[] { "/a/b", "/c", "d", "/e" }, "/a/b/c/d/e");
        AssertJoin(new string[] { "", "a", "" }, "/a/");
        AssertJoin(new string[] { "a", "b" }, "a/b");
        AssertJoin(new string[] { "a/", "b" }, "a/b");
        AssertJoin(new string[] { "a", "/b" }, "a/b");
        AssertJoin(new string[] { "/", "/a" }, "/a");
    }

    [Test]
    public void JoinPreservesRepeatedAndEncodedSeparatorsOutsideTheSharedSeam()
    {
        AssertJoin(new string[] { "/a//", "//b" }, "/a///b");
        AssertJoin(new string[] { "a//b", "c///d" }, "a//b/c///d");
        AssertJoin(new string[] { "/a%2Fb", "%3F/c" }, "/a%2Fb/%3F/c");
    }

    [TestCase("/foo/bar/baz.html", "/foo/bar/world.html", "/foo/bar/")]
    [TestCase("/foo/bar/baz.html", "/foo/bazz/world.html", "/foo/")]
    [TestCase("/foo/bar/baz.html", "/other/world.html", "/")]
    [TestCase("/foo", "bar", "")]
    [TestCase("foo/a", "foo/b", "foo/")]
    [TestCase("foo", "foo", "")]
    [TestCase("/foo/bar/", "/foo/bar/baz", "/foo/bar/")]
    [TestCase("/a//b/x", "/a//b/y", "/a//b/")]
    [TestCase("", "", "")]
    public void CommonBaseReturnsTheLongestSharedDirectory(
        string first,
        string second,
        string expected)
    {
        string result = UriPathUtilityCore.FindCommonBasePath(first, second);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CommonBaseDoesNotDecodeEncodedSeparators()
    {
        string encoded = UriPathUtilityCore.FindCommonBasePath(
            "/a%2Fb/c",
            "/a%2Fb/d");
        string encodedQuestion = UriPathUtilityCore.FindCommonBasePath(
            "/a%3Fb/c",
            "/a%3Fb/d");

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo("/a%2Fb/"));
            Assert.That(encodedQuestion, Is.EqualTo("/a%3Fb/"));
        });
    }

    [TestCase("/a/./b/../c", "/a/c")]
    [TestCase("a/b/../../c", "/c")]
    [TestCase("/a//./b/../c/", "/a//c/")]
    [TestCase("/a/%2E%2E/b", "/a/%2E%2E/b")]
    [TestCase("/a%2Fb/../c", "/c")]
    [TestCase("/a%3Fb/./c", "/a%3Fb/c")]
    public void StandaloneDotRemovalUsesOnlyLiteralDotAndSlashSegments(
        string path,
        string expected)
    {
        string result = UriPathUtilityCore.RemoveDotSegments(path);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void InvalidNullInputsAreRejectedWithoutPartialResults()
    {
        string[] invalidPaths = new string[1];
        invalidPaths[0] = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.RemoveDotSegments(null));
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.SplitPathSegments(null));
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.JoinPaths(null));
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.JoinPaths(invalidPaths));
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.FindCommonBasePath(null, "/a"));
            Assert.Throws<System.ArgumentException>(() =>
                UriPathUtilityCore.FindCommonBasePath("/a", null));
        });
    }

    private static void AssertSegments(string path, string[] expected)
    {
        string[] result = UriPathUtilityCore.SplitPathSegments(path);

        Assert.That(result, Is.EqualTo(expected), path);
    }

    private static void AssertJoin(string[] paths, string expected)
    {
        string result = UriPathUtilityCore.JoinPaths(paths);

        Assert.That(result, Is.EqualTo(expected));
    }
}
