using websocket;

namespace websocket.Tests;

public class WebSocketUriValidatorTests
{
    [Test]
    public void NormalizesDefaultCleartextUri()
    {
        var result = WebSocketUriValidator.Validate("ws://example.com");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Secure, Is.False);
            Assert.That(result.Scheme, Is.EqualTo("ws"));
            Assert.That(result.Host, Is.EqualTo("example.com"));
            Assert.That(result.HostIsIpv4, Is.False);
            Assert.That(result.HostIsIpv6, Is.False);
            Assert.That(result.HasExplicitPort, Is.False);
            Assert.That(result.Port, Is.EqualTo(80));
            Assert.That(result.Authority, Is.EqualTo("example.com"));
            Assert.That(result.RequestTarget, Is.EqualTo("/"));
            Assert.That(result.NormalizedUri, Is.EqualTo("ws://example.com/"));
            Assert.That(result.Error.Identifier, Is.Empty);
        });
    }

    [Test]
    public void NormalizesSecureSchemeHostCasePathAndQuery()
    {
        var result = WebSocketUriValidator.Validate("WSS://EXAMPLE.COM/chat?room=blue");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Secure, Is.True);
            Assert.That(result.Scheme, Is.EqualTo("wss"));
            Assert.That(result.Host, Is.EqualTo("example.com"));
            Assert.That(result.Port, Is.EqualTo(443));
            Assert.That(result.HasExplicitPort, Is.False);
            Assert.That(result.Authority, Is.EqualTo("example.com"));
            Assert.That(result.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(result.NormalizedUri, Is.EqualTo("wss://example.com/chat?room=blue"));
        });
    }

    [TestCase("ws://example.com:8080/a", 8080, "example.com:8080", "ws://example.com:8080/a")]
    [TestCase("ws://example.com:80/", 80, "example.com:80", "ws://example.com:80/")]
    [TestCase("wss://example.com:443/socket", 443, "example.com:443", "wss://example.com:443/socket")]
    [TestCase("ws://example.com:00080/", 80, "example.com:80", "ws://example.com:80/")]
    [TestCase("wss://example.com:65535/", 65535, "example.com:65535", "wss://example.com:65535/")]
    public void PreservesExplicitPortIntentAndCanonicalizesDigits(
        string uri,
        int expectedPort,
        string expectedAuthority,
        string expectedNormalized)
    {
        var result = WebSocketUriValidator.Validate(uri);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.HasExplicitPort, Is.True);
            Assert.That(result.Port, Is.EqualTo(expectedPort));
            Assert.That(result.Authority, Is.EqualTo(expectedAuthority));
            Assert.That(result.NormalizedUri, Is.EqualTo(expectedNormalized));
        });
    }

    [Test]
    public void AcceptsStrictDottedDecimalIpv4()
    {
        var result = WebSocketUriValidator.Validate("ws://192.0.2.10:8000/socket");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(result.HostIsIpv4, Is.True);
            Assert.That(result.HostIsIpv6, Is.False);
            Assert.That(result.Authority, Is.EqualTo("192.0.2.10:8000"));
            Assert.That(result.RequestTarget, Is.EqualTo("/socket"));
        });
    }

    [Test]
    public void CanonicalizesBracketedIpv6AndRetainsBracketsInAuthority()
    {
        var result = WebSocketUriValidator.Validate(
            "wss://[2001:0DB8:0:0:0:0:0:1]:8443/chat?x=1");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(result.HostIsIpv4, Is.False);
            Assert.That(result.HostIsIpv6, Is.True);
            Assert.That(result.Port, Is.EqualTo(8443));
            Assert.That(result.Authority, Is.EqualTo("[2001:db8::1]:8443"));
            Assert.That(result.NormalizedUri, Is.EqualTo("wss://[2001:db8::1]:8443/chat?x=1"));
        });
    }

    [Test]
    public void ConvertsInternationalHostNameToAsciiIdna()
    {
        var result = WebSocketUriValidator.Validate("wss://BÜCHER.example/chat");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Host, Is.EqualTo("xn--bcher-kva.example"));
            Assert.That(result.Authority, Is.EqualTo("xn--bcher-kva.example"));
            Assert.That(result.NormalizedUri, Is.EqualTo("wss://xn--bcher-kva.example/chat"));
        });
    }

    [Test]
    public void EscapesUnicodePathAndQueryUsingUriNormalization()
    {
        var result = WebSocketUriValidator.Validate(
            "ws://example.com/Grüße?q=naïve");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.RequestTarget,
                Is.EqualTo("/Gr%C3%BC%C3%9Fe?q=na%C3%AFve"));
            Assert.That(result.NormalizedUri,
                Is.EqualTo("ws://example.com/Gr%C3%BC%C3%9Fe?q=na%C3%AFve"));
        });
    }

    [TestCase("ws://example.com?x=1", "/?x=1", "ws://example.com/?x=1")]
    [TestCase("ws://example.com/a/../b?x=%7e", "/b?x=~", "ws://example.com/b?x=~")]
    [TestCase("ws://example.com/a%20b", "/a%20b", "ws://example.com/a%20b")]
    [TestCase("ws://example.com/?", "/?", "ws://example.com/?")]
    public void NormalizesOriginFormPathAndQuery(
        string uri,
        string expectedTarget,
        string expectedNormalized)
    {
        var result = WebSocketUriValidator.Validate(uri);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.RequestTarget, Is.EqualTo(expectedTarget));
            Assert.That(result.NormalizedUri, Is.EqualTo(expectedNormalized));
        });
    }

    [Test]
    public void NormalizedUriIsIdempotentIncludingExplicitDefaultPort()
    {
        var first = WebSocketUriValidator.Validate(
            "WSS://BÜCHER.example:0443/a/../chat?q=naïve");

        var second = WebSocketUriValidator.Validate(first.NormalizedUri);

        Assert.Multiple(() =>
        {
            Assert.That(first.Valid, Is.True);
            Assert.That(second.Valid, Is.True);
            Assert.That(second.Scheme, Is.EqualTo(first.Scheme));
            Assert.That(second.Host, Is.EqualTo(first.Host));
            Assert.That(second.HasExplicitPort, Is.True);
            Assert.That(second.Port, Is.EqualTo(first.Port));
            Assert.That(second.Authority, Is.EqualTo(first.Authority));
            Assert.That(second.RequestTarget, Is.EqualTo(first.RequestTarget));
            Assert.That(second.NormalizedUri, Is.EqualTo(first.NormalizedUri));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("example.com/chat")]
    [TestCase("/chat")]
    [TestCase("http://example.com/")]
    [TestCase("https://example.com/")]
    [TestCase("ftp://example.com/")]
    [TestCase("ws:/example.com/")]
    [TestCase("ws:///chat")]
    [TestCase("ws://")]
    public void RejectsMissingRelativeOrUnsupportedSchemeUris(string? uri)
    {
        AssertInvalid(uri!);
    }

    [TestCase("ws://user@example.com/")]
    [TestCase("wss://user:password@example.com/")]
    [TestCase("ws://example.com/#fragment")]
    [TestCase("ws://example.com/#")]
    [TestCase("ws://example.com/chat?x=1#fragment")]
    public void RejectsUserInfoAndFragments(string uri)
    {
        AssertInvalid(uri);
    }

    [TestCase("ws://example.com:/")]
    [TestCase("ws://example.com:0/")]
    [TestCase("ws://example.com:65536/")]
    [TestCase("ws://example.com:-1/")]
    [TestCase("ws://example.com:+80/")]
    [TestCase("ws://example.com:8x/")]
    [TestCase("ws://example.com:80:90/")]
    public void RejectsInvalidExplicitPorts(string uri)
    {
        AssertInvalid(uri);
    }

    [TestCase("ws://127.1/")]
    [TestCase("ws://127.0.0/")]
    [TestCase("ws://127.00.0.1/")]
    [TestCase("ws://256.0.0.1/")]
    [TestCase("ws://1.2.3.999/")]
    [TestCase("ws://1..2.3/")]
    public void RejectsNonCanonicalOrOutOfRangeIpv4(string uri)
    {
        AssertInvalid(uri);
    }

    [TestCase("ws://2001:db8::1/")]
    [TestCase("ws://[2001:db8::1/")]
    [TestCase("ws://2001:db8::1]/")]
    [TestCase("ws://[]/")]
    [TestCase("ws://[v1.example]/")]
    [TestCase("ws://[fe80::1%25eth0]/")]
    public void RejectsUnbracketedOrUnsupportedIpLiterals(string uri)
    {
        AssertInvalid(uri);
    }

    [TestCase("ws://-bad.example/")]
    [TestCase("ws://bad-.example/")]
    [TestCase("ws://bad..example/")]
    [TestCase("ws://bad_name.example/")]
    [TestCase("ws://123/")]
    [TestCase("ws://%65xample.com/")]
    public void RejectsInvalidDnsOrEncodedHostNames(string uri)
    {
        AssertInvalid(uri);
    }

    [TestCase(" ws://example.com/")]
    [TestCase("ws://example.com/ ")]
    [TestCase("ws://example.com/a b")]
    [TestCase("ws://example.com\\chat")]
    [TestCase("ws://example.com/\u007f")]
    [TestCase("ws://example.com/\r\nInjected")]
    public void RejectsUnsafeOrAmbiguousUriText(string uri)
    {
        AssertInvalid(uri);
    }

    [Test]
    public void AcceptsDnsNameWithTrailingRootDot()
    {
        var result = WebSocketUriValidator.Validate("ws://Example.COM./");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Host, Is.EqualTo("example.com."));
            Assert.That(result.Authority, Is.EqualTo("example.com."));
            Assert.That(result.NormalizedUri, Is.EqualTo("ws://example.com./"));
        });
    }

    static void AssertInvalid(string uri)
    {
        var result = WebSocketUriValidator.Validate(uri);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Scheme, Is.Empty);
            Assert.That(result.Host, Is.Empty);
            Assert.That(result.Authority, Is.Empty);
            Assert.That(result.RequestTarget, Is.Empty);
            Assert.That(result.NormalizedUri, Is.Empty);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-uri-invalid"));
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
        });
    }
}
