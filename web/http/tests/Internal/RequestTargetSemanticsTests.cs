using System.Text;

namespace http.Tests;

public class RequestTargetSemanticsTests
{
    [Test]
    public void DeterminesAllFourRequestTargetForms()
    {
        HttpRequest origin = Request(HttpMethods.Get, "/items?q=one", "example.com");
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "http://example.com/items?q=one",
            "ignored.example");
        HttpRequest authority = Request(HttpMethods.Connect, "example.com:443", "example.com");
        HttpRequest asterisk = Request(HttpMethods.Options, "*", "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(RequestTargetSemantics.Determine(origin),
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(RequestTargetSemantics.Determine(absolute),
                Is.EqualTo(HttpRequestTargetForm.Absolute));
            Assert.That(RequestTargetSemantics.Determine(authority),
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(RequestTargetSemantics.Determine(asterisk),
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
        });
    }

    [Test]
    public void EnforcesMethodSpecificTargetForms()
    {
        HttpRequest getAsterisk = Request(HttpMethods.Get, "*", "example.com");
        HttpRequest connectOrigin = Request(HttpMethods.Connect, "/", "example.com");
        HttpRequest connectAbsolute = Request(
            HttpMethods.Connect,
            "https://example.com/",
            "example.com");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(getAsterisk));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(connectOrigin));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(connectAbsolute));
        });
    }

    [Test]
    public void RejectsMalformedTargetsAndStrictConnectAuthorities()
    {
        string[] malformedOrigins = ["", "relative/path", "/bad path", "/bad%2", "/path#part"];
        string[] malformedConnect =
        [
            "example.com",
            "example.com:",
            "example.com:65536",
            "user@example.com:443",
            "[::1",
            ":443"
        ];

        Assert.Multiple(() =>
        {
            foreach (string target in malformedOrigins)
            {
                Assert.Throws<HttpProtocolException>(() =>
                    RequestTargetSemantics.Determine(Request(HttpMethods.Get, target, "example.com")),
                    target);
            }

            foreach (string target in malformedConnect)
            {
                Assert.Throws<HttpProtocolException>(() =>
                    RequestTargetSemantics.Determine(
                        Request(HttpMethods.Connect, target, "example.com")),
                    target);
            }
        });
    }

    [Test]
    public void Http11RequiresExactlyOneValidHostField()
    {
        HttpRequest missing = new HttpRequest();
        missing.ProtocolVersion = HttpProtocolVersion.Http11;
        missing.Method = HttpMethods.Get;
        missing.Target = "/";
        HttpRequest duplicate = Request(HttpMethods.Get, "/", "one.example");
        AddHost(duplicate, "two.example");
        HttpRequest malformed = Request(HttpMethods.Get, "/", "bad host");
        HttpRequest absoluteMissing = new HttpRequest();
        absoluteMissing.ProtocolVersion = HttpProtocolVersion.Http11;
        absoluteMissing.Method = HttpMethods.Get;
        absoluteMissing.Target = "http://example.com/";

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => RequestTargetSemantics.Determine(missing));
            Assert.Throws<HttpProtocolException>(() => RequestTargetSemantics.Determine(duplicate));
            Assert.Throws<HttpProtocolException>(() => RequestTargetSemantics.Determine(malformed));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(absoluteMissing));
        });
    }

    [Test]
    public void AuthorityComparisonUsesHttpNormalization()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "EXAMPLE.com",
                "example.com:80",
                "http"), Is.True);
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "example.com:",
                "example.com:080",
                "HTTP"), Is.True);
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "%65xample.com",
                "Example.com",
                "https"), Is.True);
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "[2001:DB8::1]:443",
                "[2001:db8::1]",
                "https"), Is.True);
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "example.com:81",
                "example.com",
                "http"), Is.False);
            Assert.That(RequestTargetSemantics.AreAuthoritiesEquivalent(
                "user@example.com",
                "example.com",
                "http"), Is.False);
        });
    }

    [Test]
    public void Http2AuthorityAndHostMustIdentifyTheSameEntity()
    {
        HttpRequest matching = Request(HttpMethods.Get, "/", "example.com:443");
        matching.ProtocolVersion = HttpProtocolVersion.Http2;
        matching.Scheme = "https";
        matching.Authority = "EXAMPLE.com";
        HttpRequest conflicting = Request(HttpMethods.Get, "/", "other.example");
        conflicting.ProtocolVersion = HttpProtocolVersion.Http2;
        conflicting.Scheme = "https";
        conflicting.Authority = "example.com";

        Assert.Multiple(() =>
        {
            Assert.That(RequestTargetSemantics.Determine(matching),
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(conflicting));
        });
    }

    [Test]
    public void AbsoluteFormUsesTargetAuthorityRatherThanHost()
    {
        HttpRequest request = Request(
            HttpMethods.Get,
            "http://target.example/resource",
            "different.example");
        request.Scheme = "https";

        Assert.That(
            RequestTargetSemantics.BuildEffectiveUri(request),
            Is.EqualTo("http://target.example/resource"));
    }

    [Test]
    public void BuildsEffectiveUrisForNonAbsoluteForms()
    {
        HttpRequest origin = Request(HttpMethods.Get, "/path?", "example.com:8443");
        origin.Scheme = "https";
        HttpRequest authority = Request(HttpMethods.Connect, "example.com:443", "example.com");
        authority.Scheme = "https";
        HttpRequest asterisk = Request(HttpMethods.Options, "*", "example.com");
        asterisk.Scheme = "http";

        Assert.Multiple(() =>
        {
            Assert.That(RequestTargetSemantics.BuildEffectiveUri(origin),
                Is.EqualTo("https://example.com:8443/path?"));
            Assert.That(RequestTargetSemantics.BuildEffectiveUri(authority),
                Is.EqualTo("https://example.com:443"));
            Assert.That(RequestTargetSemantics.BuildEffectiveUri(asterisk),
                Is.EqualTo("http://example.com"));
        });
    }

    [Test]
    public void RejectsUserinfoMalformedAuthorityAndMissingEffectiveContext()
    {
        HttpRequest absoluteUserInfo = Request(
            HttpMethods.Get,
            "http://user@example.com/",
            "example.com");
        HttpRequest authorityUserInfo = Request(HttpMethods.Get, "/", "example.com");
        authorityUserInfo.Authority = "user@example.com";
        HttpRequest badPort = Request(HttpMethods.Get, "/", "example.com:99999");
        HttpRequest noScheme = Request(HttpMethods.Get, "/", "example.com");
        noScheme.Scheme = null;
        HttpRequest emptyHttpAuthority = Request(HttpMethods.Get, "/", "");
        emptyHttpAuthority.Scheme = "http";

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(absoluteUserInfo));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.Determine(authorityUserInfo));
            Assert.Throws<HttpProtocolException>(() => RequestTargetSemantics.Determine(badPort));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.BuildEffectiveUri(noScheme));
            Assert.Throws<HttpProtocolException>(() =>
                RequestTargetSemantics.BuildEffectiveUri(emptyHttpAuthority));
        });
    }

    static HttpRequest Request(string method, string target, string host)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        request.Scheme = "http";
        AddHost(request, host);
        return request;
    }

    static void AddHost(HttpRequest request, string value)
    {
        HttpHeaderField host = new HttpHeaderField();
        host.Name = HttpHeaderNames.Host;
        host.Value = Encoding.ASCII.GetBytes(value);
        request.Headers.Fields.Add(host);
    }
}
