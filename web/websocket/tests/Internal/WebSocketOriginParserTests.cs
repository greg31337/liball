using websocket;

namespace websocket.Tests;

public class WebSocketOriginParserTests
{
    [TestCase(
        "HTTP://EXAMPLE.COM",
        "http",
        "example.com",
        80,
        "http://example.com")]
    [TestCase(
        "http://example.com:00080",
        "http",
        "example.com",
        80,
        "http://example.com")]
    [TestCase(
        "HTTPS://BÜCHER.example:443",
        "https",
        "xn--bcher-kva.example",
        443,
        "https://xn--bcher-kva.example")]
    [TestCase(
        "https://example.com:8443",
        "https",
        "example.com",
        8443,
        "https://example.com:8443")]
    [TestCase(
        "http://192.0.2.10:8080",
        "http",
        "192.0.2.10",
        8080,
        "http://192.0.2.10:8080")]
    public void ParsesAndNormalizesTupleOrigins(
        string origin,
        string expectedScheme,
        string expectedHost,
        int expectedPort,
        string expectedSerialized)
    {
        var result = WebSocketOriginParser.ParseOrigin(origin);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Opaque, Is.False);
            Assert.That(result.Scheme, Is.EqualTo(expectedScheme));
            Assert.That(result.Host, Is.EqualTo(expectedHost));
            Assert.That(result.Port, Is.EqualTo(expectedPort));
            Assert.That(result.SerializedOrigin, Is.EqualTo(expectedSerialized));
        });
    }

    [Test]
    public void NormalizesBracketedIpv6AndRetainsBracketsOnlyInSerialization()
    {
        var result = WebSocketOriginParser.ParseOrigin(
            "HTTPS://[2001:0DB8:0:0:0:0:0:1]:0443");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(result.HostIsIpv4, Is.False);
            Assert.That(result.HostIsIpv6, Is.True);
            Assert.That(result.Port, Is.EqualTo(443));
            Assert.That(result.SerializedOrigin, Is.EqualTo("https://[2001:db8::1]"));
        });
    }

    [Test]
    public void RecognizesOpaqueNullOriginWithoutTreatingItAsATuple()
    {
        var result = WebSocketOriginParser.ParseOrigin("null");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Opaque, Is.True);
            Assert.That(result.Scheme, Is.Empty);
            Assert.That(result.Host, Is.Empty);
            Assert.That(result.Port, Is.Zero);
            Assert.That(result.SerializedOrigin, Is.EqualTo("null"));
        });
    }

    [TestCase("")]
    [TestCase("NULL")]
    [TestCase("example.com")]
    [TestCase("ws://example.com")]
    [TestCase("ftp://example.com")]
    [TestCase("http://")]
    [TestCase("http://user@example.com")]
    [TestCase("http://example.com/")]
    [TestCase("http://example.com/path")]
    [TestCase("http://example.com?query")]
    [TestCase("http://example.com#fragment")]
    [TestCase("http://one.example https://two.example")]
    [TestCase(" http://example.com")]
    [TestCase("http://example.com ")]
    [TestCase("http://example.com:0")]
    [TestCase("http://example.com:65536")]
    [TestCase("http://127.1")]
    [TestCase("http://bad_name.example")]
    [TestCase("http://%65xample.com")]
    public void RejectsNonSerializedOrUnsafeOrigins(string origin)
    {
        var result = WebSocketOriginParser.ParseOrigin(origin);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Opaque, Is.False);
            Assert.That(result.SerializedOrigin, Is.Empty);
        });
    }

    [Test]
    public void ParsesAuthoritiesUsingTheSuppliedHttpScheme()
    {
        var cleartext = WebSocketOriginParser.ParseAuthority("http", "Example.com");
        var secure = WebSocketOriginParser.ParseAuthority("https", "Example.com:443");
        var unsupported = WebSocketOriginParser.ParseAuthority("ws", "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(cleartext.SerializedOrigin, Is.EqualTo("http://example.com"));
            Assert.That(cleartext.Port, Is.EqualTo(80));
            Assert.That(secure.SerializedOrigin, Is.EqualTo("https://example.com"));
            Assert.That(secure.Port, Is.EqualTo(443));
            Assert.That(unsupported.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [Test]
    public void OriginEqualityUsesNormalizedSchemeHostAndEffectivePort()
    {
        var normalized = WebSocketOriginParser.ParseOrigin("HTTPS://BÜCHER.example:443");
        var equivalent = WebSocketOriginParser.ParseOrigin("https://xn--bcher-kva.example");
        var otherScheme = WebSocketOriginParser.ParseOrigin("http://xn--bcher-kva.example");
        var otherPort = WebSocketOriginParser.ParseOrigin("https://xn--bcher-kva.example:8443");
        var opaque = WebSocketOriginParser.ParseOrigin("null");

        Assert.Multiple(() =>
        {
            Assert.That(WebSocketOriginParser.AreSame(normalized, equivalent), Is.True);
            Assert.That(WebSocketOriginParser.AreSame(normalized, otherScheme), Is.False);
            Assert.That(WebSocketOriginParser.AreSame(normalized, otherPort), Is.False);
            Assert.That(WebSocketOriginParser.AreSame(opaque, opaque), Is.False);
        });
    }
}
