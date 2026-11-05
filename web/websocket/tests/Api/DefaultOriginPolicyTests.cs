using websocket;

namespace websocket.Tests;

public class DefaultOriginPolicyTests
{
    [Test]
    public void AllowsRequestsWithoutAnOriginField()
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        var request = Http1Request("example.com", null);

        Assert.That(policy.IsOriginAllowed(request), Is.True);
    }

    [TestCase("EXAMPLE.com", "http://example.COM", true)]
    [TestCase("example.com:80", "http://example.com", true)]
    [TestCase("example.com", "http://example.com:80", true)]
    [TestCase("example.com:8080", "http://example.com:8080", true)]
    [TestCase("example.com:8080", "http://example.com", false)]
    [TestCase("example.com", "http://other.example", false)]
    [TestCase("BÜCHER.example", "http://xn--bcher-kva.example", true)]
    [TestCase("xn--bcher-kva.example", "http://BÜCHER.example", true)]
    [TestCase("192.0.2.10", "http://192.0.2.10:80", true)]
    [TestCase("192.0.2.10:8080", "http://192.0.2.10", false)]
    [TestCase("[2001:db8::1]", "http://[2001:0DB8:0:0:0:0:0:1]:80", true)]
    [TestCase("[2001:db8::1]:8080", "http://[2001:0DB8::1]:8080", true)]
    [TestCase("[2001:db8::1]:8080", "http://[2001:db8::2]:8080", false)]
    [TestCase("example.com:443", "https://example.com", true)]
    public void AppliesNormalizedSameAuthorityPolicyToHttp1(
        string host,
        string origin,
        bool expected)
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        var request = Http1Request(host, origin);

        Assert.That(policy.IsOriginAllowed(request), Is.EqualTo(expected));
    }

    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void ExtendedConnectRequiresTheSameSchemeHostAndPort(
        WebSocketHandshakeTransport transport)
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        var same = ExtendedRequest(transport, "https", "EXAMPLE.com:443", "https://example.com");
        var wrongScheme = ExtendedRequest(transport, "https", "example.com", "http://example.com");
        var wrongHost = ExtendedRequest(transport, "https", "example.com", "https://other.example");
        var wrongPort = ExtendedRequest(transport, "https", "example.com:8443", "https://example.com");

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(same), Is.True);
            Assert.That(policy.IsOriginAllowed(wrongScheme), Is.False);
            Assert.That(policy.IsOriginAllowed(wrongHost), Is.False);
            Assert.That(policy.IsOriginAllowed(wrongPort), Is.False);
        });
    }

    [TestCase("https", "192.0.2.10", "https://192.0.2.10:443", true)]
    [TestCase("http", "192.0.2.10:8080", "http://192.0.2.10:8080", true)]
    [TestCase("https", "[2001:db8::1]", "https://[2001:0DB8:0:0:0:0:0:1]", true)]
    [TestCase("https", "BÜCHER.example:443", "https://xn--bcher-kva.example", true)]
    [TestCase("https", "xn--bcher-kva.example", "https://BÜCHER.example:8443", false)]
    [TestCase("https", "example.com:8443", "https://example.com", false)]
    public void NormalizesIpIdnaAndPortVariantsForBothExtendedConnectTransports(
        string scheme,
        string authority,
        string origin,
        bool expected)
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        WebSocketHandshakeTransport[] transports =
        [
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            WebSocketHandshakeTransport.Http3ExtendedConnect
        ];

        foreach (WebSocketHandshakeTransport transport in transports)
        {
            var request = ExtendedRequest(transport, scheme, authority, origin);

            Assert.That(policy.IsOriginAllowed(request), Is.EqualTo(expected),
                $"transport {transport}");
        }
    }

    [Test]
    public void ExactAllowListAddsNormalizedCrossOriginTuples()
    {
        var policy = new DefaultWebSocketOriginPolicy(
            ["HTTPS://TRUSTED.example:443", "https://trusted.example:8443"]);
        var defaultPort = Http1Request("service.example", "https://trusted.example");
        var alternatePort = Http1Request("service.example", "https://TRUSTED.example:8443");
        var wrongPort = Http1Request("service.example", "https://trusted.example:9443");
        var wrongScheme = Http1Request("service.example", "http://trusted.example");

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(defaultPort), Is.True);
            Assert.That(policy.IsOriginAllowed(alternatePort), Is.True);
            Assert.That(policy.IsOriginAllowed(wrongPort), Is.False);
            Assert.That(policy.IsOriginAllowed(wrongScheme), Is.False);
        });
    }

    [Test]
    public void AllowListNormalizesIpv4Ipv6AndIdnaOrigins()
    {
        var policy = new DefaultWebSocketOriginPolicy(
        [
            "HTTPS://192.0.2.10:443",
            "https://[2001:0DB8:0:0:0:0:0:1]:8443",
            "https://BÜCHER.example"
        ]);
        var ipv4 = Http1Request("service.example", "https://192.0.2.10");
        var ipv6 = Http1Request("service.example", "https://[2001:db8::1]:8443");
        var idna = Http1Request("service.example", "https://xn--bcher-kva.example:443");
        var wrongIpv6Port = Http1Request("service.example", "https://[2001:db8::1]");

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(ipv4), Is.True);
            Assert.That(policy.IsOriginAllowed(ipv6), Is.True);
            Assert.That(policy.IsOriginAllowed(idna), Is.True);
            Assert.That(policy.IsOriginAllowed(wrongIpv6Port), Is.False);
        });
    }

    [Test]
    public void AllowListConfigurationIsCopied()
    {
        var allowed = new[] { "https://trusted.example" };
        var policy = new DefaultWebSocketOriginPolicy(allowed);
        allowed[0] = "https://mutated.example";

        var retained = Http1Request("service.example", "https://trusted.example");
        var mutated = Http1Request("service.example", "https://mutated.example");

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(retained), Is.True);
            Assert.That(policy.IsOriginAllowed(mutated), Is.False);
        });
    }

    [Test]
    public void ConstructorRejectsInvalidOpaqueOrDuplicateAllowListEntries()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy(null!));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy([""]));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy([null!]));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy(["null"]));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy(["https://example.com/path"]));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new DefaultWebSocketOriginPolicy(
                    ["https://EXAMPLE.com", "https://example.com:443"]));
        });
    }

    [Test]
    public void ClientOptionsRequireAParsedOriginWhenPresent()
    {
        var tuple = new WebSocketClientHandshakeOptions();
        tuple.Uri = "wss://example.com/";
        tuple.HasOrigin = true;
        tuple.Origin = "https://example.com";
        var opaque = new WebSocketClientHandshakeOptions();
        opaque.Uri = "wss://example.com/";
        opaque.HasOrigin = true;
        opaque.Origin = "null";
        var malformed = new WebSocketClientHandshakeOptions();
        malformed.Uri = "wss://example.com/";
        malformed.HasOrigin = true;
        malformed.Origin = "https://example.com/path";

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                WebSocketOptionValidator.ValidateClientHandshakeOptions(tuple));
            Assert.DoesNotThrow(() =>
                WebSocketOptionValidator.ValidateClientHandshakeOptions(opaque));
            Assert.Throws<WebSocketHandshakeException>(() =>
                WebSocketOptionValidator.ValidateClientHandshakeOptions(malformed));
        });
    }

    [TestCase("null")]
    [TestCase("")]
    [TestCase("https://example.com/path")]
    [TestCase("https://one.example https://two.example")]
    [TestCase("https://user@example.com")]
    public void RejectsOpaqueEmptyMalformedOrMultipleOrigins(string origin)
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        var request = Http1Request("example.com", origin);

        Assert.That(policy.IsOriginAllowed(request), Is.False);
    }

    [Test]
    public void RejectsRepeatedOriginOrMissingTargetAuthority()
    {
        var policy = new DefaultWebSocketOriginPolicy([]);
        var repeated = Http1Request("example.com", "http://example.com");
        repeated.Fields =
        [
            Field("Host", "example.com"),
            Field("Origin", "http://example.com"),
            Field("origin", "http://example.com")
        ];
        var duplicateHost = Http1Request("example.com", "http://example.com");
        duplicateHost.Fields =
        [
            Field("Host", "example.com"),
            Field("host", "example.com"),
            Field("Origin", "http://example.com")
        ];
        var missingExtendedAuthority = ExtendedRequest(
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            "https",
            "example.com",
            "https://example.com");
        missingExtendedAuthority.HasAuthority = false;

        Assert.Multiple(() =>
        {
            Assert.That(policy.IsOriginAllowed(repeated), Is.False);
            Assert.That(policy.IsOriginAllowed(duplicateHost), Is.False);
            Assert.That(policy.IsOriginAllowed(missingExtendedAuthority), Is.False);
            Assert.That(policy.IsOriginAllowed(null!), Is.False);
        });
    }

    [Test]
    public void DefaultAndCustomPoliciesHandleAbsentOpaqueAndCrossOriginsDistinctly()
    {
        IWebSocketServerHandshake defaultHandshake = new WebSocketServerHandshake();
        var opaqueByDefault = defaultHandshake.EvaluateHttp1Request(
            ValidUpgradeRequest("null"),
            new WebSocketServerHandshakeOptions());

        var allowingPolicy = new RecordingOriginDecisionPolicy(true);
        var allowingOptions = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = allowingPolicy
        };
        IWebSocketServerHandshake allowingHandshake = new WebSocketServerHandshake();
        var absent = allowingHandshake.EvaluateHttp1Request(
            ValidUpgradeRequest(null),
            allowingOptions);
        var opaque = allowingHandshake.EvaluateHttp1Request(
            ValidUpgradeRequest("null"),
            allowingOptions);
        var cross = allowingHandshake.EvaluateHttp1Request(
            ValidUpgradeRequest("https://other.example"),
            allowingOptions);

        var denyingPolicy = new RecordingOriginDecisionPolicy(false);
        var denyingOptions = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = denyingPolicy
        };
        var deniedAbsent = new WebSocketServerHandshake().EvaluateHttp1Request(
            ValidUpgradeRequest(null),
            denyingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(opaqueByDefault.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(opaqueByDefault.Error.Identifier,
                Is.EqualTo("websocket-origin-rejected"));
            Assert.That(absent.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(opaque.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(cross.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(allowingPolicy.CallCount, Is.EqualTo(3));
            Assert.That(deniedAbsent.Outcome,
                Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(deniedAbsent.Error.HttpStatusCode, Is.EqualTo(403));
            Assert.That(denyingPolicy.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedOriginFailsBeforeCustomPolicyIsInvoked()
    {
        var policy = new RecordingOriginDecisionPolicy(true);
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = policy
        };

        var result = new WebSocketServerHandshake().EvaluateHttp1Request(
            ValidUpgradeRequest("https://example.com/path"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("websocket-request-origin-invalid"));
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(400));
            Assert.That(policy.CallCount, Is.Zero);
        });
    }

    static WebSocketHttpRequest Http1Request(string host, string? origin)
    {
        var fields = new List<WebSocketHeaderField>
        {
            Field("Host", host)
        };
        if (origin != null)
        {
            fields.Add(Field("Origin", origin));
        }

        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Fields = fields.ToArray()
        };
    }

    static WebSocketHttpRequest ExtendedRequest(
        WebSocketHandshakeTransport transport,
        string scheme,
        string authority,
        string origin)
    {
        return new WebSocketHttpRequest
        {
            Transport = transport,
            HasScheme = true,
            Scheme = scheme,
            HasAuthority = true,
            Authority = authority,
            Fields = [Field("Origin", origin)]
        };
    }

    static WebSocketHttpRequest ValidUpgradeRequest(string? origin)
    {
        var fields = new List<WebSocketHeaderField>
        {
            Field("Host", "example.com"),
            Field("Upgrade", "websocket"),
            Field("Connection", "Upgrade"),
            Field("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ=="),
            Field("Sec-WebSocket-Version", "13")
        };
        if (origin != null)
        {
            fields.Add(Field("Origin", origin));
        }

        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/chat",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1",
            Fields = fields.ToArray()
        };
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }
}

internal class RecordingOriginDecisionPolicy : IWebSocketOriginPolicy
{
    readonly bool allowed;

    internal int CallCount;

    internal RecordingOriginDecisionPolicy(bool allowedValue)
    {
        allowed = allowedValue;
    }

    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        CallCount++;
        return allowed;
    }
}
