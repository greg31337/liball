namespace cookies.Tests;

public class CookiePathMatcherTests
{
    [TestCase("https://example.test", "/")]
    [TestCase("https://example.test/", "/")]
    [TestCase("https://example.test/account", "/account")]
    [TestCase("https://example.test/account/page", "/account/page")]
    [TestCase("https://example.test/account/page?query=/ignored", "/account/page")]
    [TestCase("https://example.test/account/page#fragment/ignored", "/account/page")]
    public void RequestPathExtractionExcludesAuthorityQueryAndFragment(string uriText, string expected)
    {
        CookiePathResult result = CookiePathMatcher.ExtractRequestPath(new Uri(uriText));

        AssertSuccess(result, expected, false);
    }

    [TestCase("https://example.test/caf%C3%A9/menu", "/caf%C3%A9/menu")]
    [TestCase("https://example.test/café/menu", "/caf%C3%A9/menu")]
    [TestCase("https://example.test/a%2Fb/menu", "/a%2Fb/menu")]
    public void RequestPathExtractionUsesTheEscapedUriRepresentation(string uriText, string expected)
    {
        CookiePathResult result = CookiePathMatcher.ExtractRequestPath(new Uri(uriText));

        AssertSuccess(result, expected, false);
    }

    [TestCase("", "/")]
    [TestCase("relative", "/")]
    [TestCase("/", "/")]
    [TestCase("/account", "/")]
    [TestCase("/account/", "/account")]
    [TestCase("/account/page", "/account")]
    [TestCase("/account/page/", "/account/page")]
    [TestCase("/account//page", "/account/")]
    public void DefaultPathAlgorithmUsesTheRightmostSlash(string requestPath, string expected)
    {
        string result = CookiePathMatcher.ComputeDefaultFromEscapedPath(requestPath);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void DefaultPathAlgorithmHandlesNullInput()
    {
        Assert.That(CookiePathMatcher.ComputeDefaultFromEscapedPath(null!), Is.EqualTo("/"));
    }

    [TestCase("https://example.test", "/")]
    [TestCase("https://example.test/account", "/")]
    [TestCase("https://example.test/account/page", "/account")]
    [TestCase("https://example.test/café/menu", "/caf%C3%A9")]
    [TestCase("https://example.test/a%2Fb/menu", "/a%2Fb")]
    public void UriDefaultPathUsesTheEscapedRequestPath(string uriText, string expected)
    {
        CookiePathResult result = CookiePathMatcher.ComputeDefaultPath(new Uri(uriText));

        AssertSuccess(result, expected, true);
    }

    [TestCase("/foo", "/foo", true)]
    [TestCase("/foo/", "/foo", true)]
    [TestCase("/foo/bar", "/foo", true)]
    [TestCase("/foo/bar", "/foo/", true)]
    [TestCase("/foobar", "/foo", false)]
    [TestCase("/foo-bar", "/foo", false)]
    [TestCase("/foo", "/foo/", false)]
    [TestCase("/anything/here", "/", true)]
    public void PathMatchingRequiresExactPrefixOrSlashBoundary(
        string requestPath,
        string cookiePath,
        bool expected)
    {
        bool result = CookiePathMatcher.PathMatches(requestPath, cookiePath);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void EmptyRequestPathIsTreatedAsRoot()
    {
        Assert.That(CookiePathMatcher.PathMatches("", "/"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches(null!, "/"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches("", "/account"), Is.False);
    }

    [TestCase("relative", "/")]
    [TestCase("/path", "")]
    [TestCase("/path", "relative")]
    public void InvalidMatchInputsReturnFalse(string requestPath, string cookiePath)
    {
        Assert.That(CookiePathMatcher.PathMatches(requestPath, cookiePath), Is.False);
    }

    [Test]
    public void MatchingDoesNotDecodeEscapedPathSeparators()
    {
        string requestPath = CookiePathMatcher.ExtractRequestPath(
            new Uri("https://example.test/foo%2Fbar/item")).Path;

        Assert.That(requestPath, Is.EqualTo("/foo%2Fbar/item"));
        Assert.That(CookiePathMatcher.PathMatches(requestPath, "/foo"), Is.False);
        Assert.That(CookiePathMatcher.PathMatches(requestPath, "/foo%2Fbar"), Is.True);
        Assert.That(CookiePathMatcher.PathMatches(requestPath, "/foo/bar"), Is.False);
        Assert.That(CookiePathMatcher.PathMatches(requestPath, "/foo%2fbar"), Is.False);
    }

    [Test]
    public void AbsentEmptyAndRelativeSetCookiePathsUseTheDefault()
    {
        Uri origin = new Uri("https://example.test/account/page");
        SetCookie absent = new SetCookie { HasPath = false, Path = "/ignored" };
        SetCookie empty = new SetCookie { HasPath = true, Path = "" };
        SetCookie relative = new SetCookie { HasPath = true, Path = "account" };

        CookiePathResult absentResult = CookiePathMatcher.NormalizeSetPath(absent, origin);
        CookiePathResult emptyResult = CookiePathMatcher.NormalizeSetPath(empty, origin);
        CookiePathResult relativeResult = CookiePathMatcher.NormalizeSetPath(relative, origin);

        AssertSuccess(absentResult, "/account", true);
        AssertSuccess(emptyResult, "/account", true);
        AssertSuccess(relativeResult, "/account", true);
    }

    [Test]
    public void ValidSetCookiePathIsRetainedExactly()
    {
        SetCookie cookie = new SetCookie
        {
            HasPath = true,
            Path = "/a%2Fb/resource"
        };

        CookiePathResult result = CookiePathMatcher.NormalizeSetPath(
            cookie,
            new Uri("https://example.test/other/page"));

        AssertSuccess(result, "/a%2Fb/resource", false);
    }

    [TestCase("/path\0value", CookieErrorCode.HeaderInjection)]
    [TestCase("/path\rvalue", CookieErrorCode.HeaderInjection)]
    [TestCase("/path\nvalue", CookieErrorCode.HeaderInjection)]
    [TestCase("/path;value", CookieErrorCode.InvalidPath)]
    [TestCase("/café", CookieErrorCode.InvalidPath)]
    public void UnsafeSetCookiePathsFailExplicitly(string path, CookieErrorCode expected)
    {
        SetCookie cookie = new SetCookie
        {
            HasPath = true,
            Path = path
        };

        CookiePathResult result = CookiePathMatcher.NormalizeSetPath(
            cookie,
            new Uri("https://example.test/account/page"));

        AssertFailure(result, expected);
    }

    [TestCase("ftp://example.test/path", CookieErrorCode.UnsupportedScheme)]
    [TestCase("file:///path", CookieErrorCode.InvalidHost)]
    public void InvalidOriginsPropagateHostExtractionFailures(string uriText, CookieErrorCode expected)
    {
        CookiePathResult extracted = CookiePathMatcher.ExtractRequestPath(new Uri(uriText));
        CookiePathResult defaultPath = CookiePathMatcher.ComputeDefaultPath(new Uri(uriText));

        AssertFailure(extracted, expected);
        AssertFailure(defaultPath, expected);
    }

    [Test]
    public void NullRelativeAndNullModelInputsFailDeterministically()
    {
        CookiePathResult nullUri = CookiePathMatcher.ExtractRequestPath(null!);
        CookiePathResult relativeUri = CookiePathMatcher.ComputeDefaultPath(
            new Uri("relative", UriKind.Relative));
        CookiePathResult nullCookie = CookiePathMatcher.NormalizeSetPath(
            null!,
            new Uri("https://example.test/path"));

        AssertFailure(nullUri, CookieErrorCode.InvalidHost);
        AssertFailure(relativeUri, CookieErrorCode.InvalidHost);
        AssertFailure(nullCookie, CookieErrorCode.InvalidPath);
    }

    static void AssertSuccess(CookiePathResult result, string expected, bool usedDefault)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Path, Is.EqualTo(expected));
        Assert.That(result.UsedDefault, Is.EqualTo(usedDefault));
    }

    static void AssertFailure(CookiePathResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Path, Is.Empty);
        Assert.That(result.UsedDefault, Is.False);
    }
}
