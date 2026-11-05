namespace cookies.Tests;

public class CookiePathConformanceTests
{
    [TestCase("", "/")]
    [TestCase("relative", "/")]
    [TestCase("/", "/")]
    [TestCase("/one", "/")]
    [TestCase("//", "/")]
    [TestCase("/one/", "/one")]
    [TestCase("/one/two", "/one")]
    [TestCase("/one/two/", "/one/two")]
    [TestCase("/one//two", "/one/")]
    public void DefaultPathCoversEveryRfcBranch(
        string requestPath,
        string expected)
    {
        string result = CookiePathMatcher.ComputeDefaultFromEscapedPath(
            requestPath);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("https://example.test/caf%C3%A9/menu", "/caf%C3%A9/menu", "/caf%C3%A9")]
    [TestCase("https://example.test/café/menu", "/caf%C3%A9/menu", "/caf%C3%A9")]
    [TestCase("https://example.test/a%2Fb/menu", "/a%2Fb/menu", "/a%2Fb")]
    public void RequestAndDefaultPathsRetainEscapedRepresentation(
        string uriText,
        string expectedRequestPath,
        string expectedDefaultPath)
    {
        Uri uri = new Uri(uriText);

        CookiePathResult request = CookiePathMatcher.ExtractRequestPath(uri);
        CookiePathResult defaultPath = CookiePathMatcher.ComputeDefaultPath(uri);

        Assert.That(request.Success, Is.True);
        Assert.That(request.Path, Is.EqualTo(expectedRequestPath));
        Assert.That(request.UsedDefault, Is.False);
        Assert.That(defaultPath.Success, Is.True);
        Assert.That(defaultPath.Path, Is.EqualTo(expectedDefaultPath));
        Assert.That(defaultPath.UsedDefault, Is.True);
    }

    [TestCase("/foo", "/foo", true)]
    [TestCase("/foo/", "/foo", true)]
    [TestCase("/foo/bar", "/foo", true)]
    [TestCase("/foo/bar", "/foo/", true)]
    [TestCase("/foobar", "/foo", false)]
    [TestCase("/foo-bar", "/foo", false)]
    [TestCase("/foo", "/foo/", false)]
    [TestCase("/anything", "/", true)]
    public void PathMatchRequiresExactPrefixOrSlashBoundary(
        string requestPath,
        string cookiePath,
        bool expected)
    {
        Assert.That(
            CookiePathMatcher.PathMatches(requestPath, cookiePath),
            Is.EqualTo(expected));
    }

    [Test]
    public void EmptyRequestPathsMapToRootWhileNonSlashPathsAreInvalid()
    {
        Assert.That(CookiePathMatcher.PathMatches("", "/"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches(null!, "/"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches("", "/account"), Is.False);
        Assert.That(CookiePathMatcher.PathMatches("relative", "/"), Is.False);
        Assert.That(CookiePathMatcher.PathMatches("/account", "relative"), Is.False);
        Assert.That(CookiePathMatcher.ComputeDefaultFromEscapedPath(null!),
            Is.EqualTo("/"));
    }

    [Test]
    public void EscapedSeparatorsAreMatchedWithoutDecodingOrCaseFolding()
    {
        CookiePathResult request = CookiePathMatcher.ExtractRequestPath(
            new Uri("https://example.test/foo%2Fbar/item"));

        Assert.That(request.Path, Is.EqualTo("/foo%2Fbar/item"));
        Assert.That(CookiePathMatcher.PathMatches(
            request.Path, "/foo%2Fbar"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches(
            request.Path, "/foo/bar"), Is.False);
        Assert.That(CookiePathMatcher.PathMatches(
            request.Path, "/foo%2fbar"), Is.False);
    }

    [TestCase("id=1", false)]
    [TestCase("id=1; Path=", false)]
    [TestCase("id=1; Path=relative", false)]
    public void AbsentOrInvalidParsedPathFallsBackToTheOriginDefault(
        string fieldValue,
        bool expectedHasPath)
    {
        SetCookieParseResult parsed = SetCookieParserCore.Parse(
            fieldValue.AsSpan(), new CookieParseOptions());

        CookiePathResult normalized = CookiePathMatcher.NormalizeSetPath(
            parsed.Cookie!,
            new Uri("https://example.test/account/page"));

        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Cookie!.HasPath, Is.EqualTo(expectedHasPath));
        Assert.That(normalized.Success, Is.True);
        Assert.That(normalized.Path, Is.EqualTo("/account"));
        Assert.That(normalized.UsedDefault, Is.True);
    }

    [Test]
    public void ValidEscapedSetPathIsRetainedAndMatchesOnlyItsEncodedPrefix()
    {
        SetCookieParseResult parsed = SetCookieParserCore.Parse(
            "id=1; Path=/a%2Fb".AsSpan(),
            new CookieParseOptions());

        CookiePathResult normalized = CookiePathMatcher.NormalizeSetPath(
            parsed.Cookie!,
            new Uri("https://example.test/other/page"));

        Assert.That(normalized.Success, Is.True);
        Assert.That(normalized.Path, Is.EqualTo("/a%2Fb"));
        Assert.That(normalized.UsedDefault, Is.False);
        Assert.That(CookiePathMatcher.PathMatches(
            "/a%2Fb/resource", normalized.Path), Is.True);
        Assert.That(CookiePathMatcher.PathMatches(
            "/a/b/resource", normalized.Path), Is.False);
    }

    [Test]
    public void IncludedCookiesAreOrderedByDescendingPathLength()
    {
        DateTimeOffset creation = new DateTimeOffset(
            2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        CookieMatchResult[] matches =
        [
            Match("root", "/", creation, 0),
            Match("account", "/account", creation, 1),
            Match("deep", "/account/deep", creation, 2)
        ];

        CookieJar.SortIncludedMatches(matches);

        Assert.That(matches.Select(match => match.Cookie!.Name), Is.EqualTo(
            new[] { "deep", "account", "root" }));
    }

    static CookieMatchResult Match(
        string name,
        string path,
        DateTimeOffset creation,
        long sequence)
    {
        return new CookieMatchResult
        {
            Status = CookieMatchStatus.Included,
            ErrorCode = CookieErrorCode.None,
            Cookie = new StoredCookie
            {
                Name = name,
                Value = name,
                Domain = "example.test",
                Path = path,
                CreationTime = creation,
                LastAccessTime = creation,
                SequenceNumber = sequence
            },
            Pair = new CookiePair { Name = name, Value = name }
        };
    }
}
