using System.Text.RegularExpressions;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetUriTests
{
    static Regex EncodedComponent = new Regex(
        "^(?:[A-Za-z0-9._~-]|%[0-9A-F]{2})*$",
        RegexOptions.CultureInvariant);

    [TestCase("", "")]
    [TestCase("AZaz09-._~", "AZaz09-._~")]
    [TestCase(" ", "%20")]
    [TestCase("%20", "%2520")]
    [TestCase(":/?#[]@!$&'()*+,;=", "%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D")]
    [TestCase("À", "%C3%80")]
    [TestCase("ア", "%E3%82%A2")]
    [TestCase("😀", "%F0%9F%98%80")]
    public void ComponentEncodingUsesUtf8UppercaseRfc3986Escapes(string value, string expected)
    {
        Assert.That(Fake.Create(8001).Internet().EncodeUriComponent(value), Is.EqualTo(expected));
    }

    [Test]
    public void ComponentEncodingRejectsNullMalformedUnicodeAndOversizedOutput()
    {
        InternetProvider internet = Fake.Create(8002).Internet();
        FakeOptions options = FakeOptions.Create(8002);
        options.MaximumOutputSize = 5;
        InternetProvider limited = Fake.Create(options).Internet();

        Assert.That(
            Assert.Throws<ArgumentNullException>(() => internet.EncodeUriComponent(null)).ParamName,
            Is.EqualTo("value"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.EncodeUriComponent("bad\ud800")).ParamName,
            Is.EqualTo("value"));
        Assert.That(limited.EncodeUriComponent("abcde"), Is.EqualTo("abcde"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => limited.EncodeUriComponent("é")).ParamName,
            Is.EqualTo("value"));
    }

    [Test]
    public void GeneratedComponentsAreDeterministicAndDelimiterSafe()
    {
        InternetProvider first = Fake.Create(8003).Internet();
        InternetProvider second = Fake.Create(8003).Internet();

        int index = 0;
        while (index < 256)
        {
            string userInfo = first.UserInfo();
            string segment = first.PathSegment();
            string path = first.Path();
            string query = first.Query();
            string fragment = first.Fragment();

            Assert.That(userInfo, Is.EqualTo(second.UserInfo()));
            Assert.That(segment, Is.EqualTo(second.PathSegment()));
            Assert.That(path, Is.EqualTo(second.Path()));
            Assert.That(query, Is.EqualTo(second.Query()));
            Assert.That(fragment, Is.EqualTo(second.Fragment()));
            Assert.That(EncodedComponent.IsMatch(userInfo), Is.True);
            Assert.That(userInfo, Does.Not.Contain(":"));
            Assert.That(userInfo, Does.Not.Contain("@"));
            Assert.That(EncodedComponent.IsMatch(segment), Is.True);
            Assert.That(segment, Does.Not.Contain("/"));
            AssertPath(path);
            AssertQuery(query);
            Assert.That(EncodedComponent.IsMatch(fragment), Is.True);
            Assert.That(fragment, Does.Not.Contain("#"));
            index++;
        }
    }

    [Test]
    public void ExactCountComponentsSupportEmptyAndConfiguredBounds()
    {
        InternetProvider internet = Fake.Create(8004).Internet();
        string path = internet.PathFromSegmentCount(5);
        string query = internet.QueryFromParameterCount(5);
        FakeOptions options = FakeOptions.Create(8004);
        options.MaximumCount = 4;
        InternetProvider limited = Fake.Create(options).Internet();

        Assert.That(internet.PathFromSegmentCount(0), Is.Empty);
        Assert.That(internet.QueryFromParameterCount(0), Is.Empty);
        Assert.That(path.Split('/', StringSplitOptions.None), Has.Length.EqualTo(6));
        Assert.That(query.Split('&', StringSplitOptions.None), Has.Length.EqualTo(5));
        AssertPath(path);
        AssertQuery(query);
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => limited.PathFromSegmentCount(5)).ParamName,
            Is.EqualTo("segmentCount"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => limited.QueryFromParameterCount(5)).ParamName,
            Is.EqualTo("parameterCount"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => internet.PathFromSegmentCount(-1)).ParamName,
            Is.EqualTo("segmentCount"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => internet.QueryFromParameterCount(-1)).ParamName,
            Is.EqualTo("parameterCount"));
    }

    [Test]
    public void DefaultUrlIsAbsoluteReservedAndOmitsUserInformation()
    {
        InternetProvider internet = Fake.Create(8005).Internet();

        int index = 0;
        while (index < 256)
        {
            string value = internet.Url();
            Uri parsed = new Uri(value, UriKind.Absolute);

            Assert.That(Uri.IsWellFormedUriString(value, UriKind.Absolute), Is.True, value);
            Assert.That(parsed.Scheme, Is.EqualTo("https"));
            Assert.That(parsed.UserInfo, Is.Empty);
            Assert.That(IsReservedHost(parsed.Host), Is.True);
            Assert.That(parsed.AbsolutePath, Is.Not.EqualTo("/"));
            Assert.That(parsed.Query, Does.StartWith("?"));
            Assert.That(parsed.Fragment, Does.StartWith("#"));
            Assert.That(value, Does.Not.Contain(" "));
            index++;
        }
    }

    [Test]
    public void EveryOptionalComponentCombinationProducesTheRequestedShape()
    {
        InternetProvider internet = Fake.Create(8006).Internet();

        int mask = 0;
        while (mask < 16)
        {
            bool includeUserInfo = (mask & 1) != 0;
            bool includePath = (mask & 2) != 0;
            bool includeQuery = (mask & 4) != 0;
            bool includeFragment = (mask & 8) != 0;
            string value = internet.UrlFromIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(
                includeUserInfo,
                includePath,
                includeQuery,
                includeFragment);
            Uri parsed = new Uri(value, UriKind.Absolute);
            int authorityStart = value.IndexOf("://", StringComparison.Ordinal) + 3;
            int pathIndex = value.IndexOf('/', authorityStart);

            Assert.That(Uri.IsWellFormedUriString(value, UriKind.Absolute), Is.True, value);
            Assert.That(parsed.UserInfo.Length != 0, Is.EqualTo(includeUserInfo));
            Assert.That(pathIndex >= 0, Is.EqualTo(includePath));
            Assert.That(parsed.Query.Length != 0, Is.EqualTo(includeQuery));
            Assert.That(parsed.Fragment.Length != 0, Is.EqualTo(includeFragment));
            Assert.That(IsReservedHost(parsed.Host), Is.True);
            mask++;
        }
    }

    [Test]
    public void ExplicitSchemeIsValidatedAndCanonicalizedBeforeGeneration()
    {
        InternetProvider internet = Fake.Create(8007).Internet();

        string value = internet.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment("Git+SSH", false, true, false, false);

        Assert.That(value, Does.StartWith("git+ssh://"));
        Assert.That(new Uri(value, UriKind.Absolute).Scheme, Is.EqualTo("git+ssh"));
        Assert.That(
            Assert.Throws<ArgumentNullException>(
                () => internet.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(null, false, false, false, false)).ParamName,
            Is.EqualTo("scheme"));
        Assert.That(
            Assert.Throws<ArgumentException>(
                () => internet.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment("", false, false, false, false)).ParamName,
            Is.EqualTo("scheme"));
        Assert.That(
            Assert.Throws<ArgumentException>(
                () => internet.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment("1http", false, false, false, false)).ParamName,
            Is.EqualTo("scheme"));
        Assert.That(
            Assert.Throws<ArgumentException>(
                () => internet.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment("ht_tp", false, false, false, false)).ParamName,
            Is.EqualTo("scheme"));
    }

    [Test]
    public void UriMethodsMatchUrlTextAtTheSameSeedAndOptions()
    {
        int mask = 0;
        while (mask < 16)
        {
            bool includeUserInfo = (mask & 1) != 0;
            bool includePath = (mask & 2) != 0;
            bool includeQuery = (mask & 4) != 0;
            bool includeFragment = (mask & 8) != 0;
            InternetProvider textProvider = Fake.Create(8100 + mask).Internet();
            InternetProvider uriProvider = Fake.Create(8100 + mask).Internet();
            string text = textProvider.UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(
                "https",
                includeUserInfo,
                includePath,
                includeQuery,
                includeFragment);
            Uri value = uriProvider.UriFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(
                "https",
                includeUserInfo,
                includePath,
                includeQuery,
                includeFragment);

            Assert.That(value.IsAbsoluteUri, Is.True);
            Assert.That(value.OriginalString, Is.EqualTo(text));
            mask++;
        }

        InternetProvider defaultText = Fake.Create(8200).Internet();
        InternetProvider defaultUri = Fake.Create(8200).Internet();
        Assert.That(defaultUri.Uri().OriginalString, Is.EqualTo(defaultText.Url()));
    }

    [Test]
    public void InvalidFixturesExposeEachRequestedDefectDeterministically()
    {
        InternetProvider internet = Fake.Create(8008).Internet();

        foreach (UriInvalidKind kind in Enum.GetValues<UriInvalidKind>())
        {
            string value = internet.InvalidUrlFromKind(kind);
            Assert.That(IsValidLibraryUrl(value), Is.False, kind.ToString());
        }

        InternetProvider first = Fake.Create(8009).Internet();
        InternetProvider second = Fake.Create(8009).Internet();
        HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < 256)
        {
            string value = first.InvalidUrl();
            Assert.That(value, Is.EqualTo(second.InvalidUrl()));
            Assert.That(IsValidLibraryUrl(value), Is.False);
            selected.Add(value);
            index++;
        }
        Assert.That(selected, Has.Count.EqualTo(6));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => internet.InvalidUrlFromKind((UriInvalidKind)999)).ParamName,
            Is.EqualTo("kind"));
    }

    [Test]
    public void UrlAndInvalidFixturesHonorConfiguredOutputLimit()
    {
        FakeOptions options = FakeOptions.Create(8010);
        options.MaximumOutputSize = 10;
        InternetProvider internet = Fake.Create(options).Internet();

        Assert.Throws<ArgumentOutOfRangeException>(() => internet.Url());
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => internet.InvalidUrlFromKind(UriInvalidKind.Scheme)).ParamName,
            Is.EqualTo("url"));
    }

    static void AssertPath(string value)
    {
        Assert.That(value, Does.StartWith("/"));
        string[] segments = value.Substring(1).Split('/');
        Assert.That(segments, Is.Not.Empty);
        foreach (string segment in segments)
        {
            Assert.That(segment, Is.Not.Empty);
            Assert.That(EncodedComponent.IsMatch(segment), Is.True);
        }
    }

    static void AssertQuery(string value)
    {
        string[] parameters = value.Split('&');
        Assert.That(parameters, Is.Not.Empty);
        foreach (string parameter in parameters)
        {
            string[] parts = parameter.Split('=');
            Assert.That(parts, Has.Length.EqualTo(2));
            Assert.That(parts[0], Is.Not.Empty);
            Assert.That(parts[1], Is.Not.Empty);
            Assert.That(EncodedComponent.IsMatch(parts[0]), Is.True);
            Assert.That(EncodedComponent.IsMatch(parts[1]), Is.True);
        }
    }

    static bool IsReservedHost(string host)
    {
        return host.EndsWith(".example.com", StringComparison.Ordinal) ||
            host.EndsWith(".example.net", StringComparison.Ordinal) ||
            host.EndsWith(".example.org", StringComparison.Ordinal);
    }

    static bool IsValidLibraryUrl(string value)
    {
        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute) ||
            value.Any(char.IsWhiteSpace) ||
            Regex.IsMatch(value, "%(?![0-9A-Fa-f]{2})"))
        {
            return false;
        }
        Uri parsed = new Uri(value, UriKind.Absolute);
        if (parsed.Host.Length == 0 || parsed.UserInfo.Contains('@'))
        {
            return false;
        }
        foreach (string label in parsed.Host.Split('.'))
        {
            if (!Regex.IsMatch(label, "^[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?$"))
            {
                return false;
            }
        }
        return true;
    }
}
