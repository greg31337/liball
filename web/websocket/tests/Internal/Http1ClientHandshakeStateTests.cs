using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1ClientHandshakeStateTests
{
    [Test]
    public void CreatesMinimalUpgradeRequestAndRetainsCryptographicState()
    {
        var state = new Http1ClientHandshakeState();
        var options = Options("ws://Example.COM/chat?room=blue");

        var result = state.CreateRequest(options);
        var request = result.Request;
        var nonce = new byte[WebSocketProtocolConstants.ClientNonceLength];
        var nonceDecode = Base64Codec.DecodeClientNonce(state.ClientKey, nonce);
        var expectedAccept = WebSocketHandshakeCryptography.ComputeAccept(state.ClientKey);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.EqualTo(state.ClientKey));
            Assert.That(result.HasError, Is.False);
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.HasRequestTarget, Is.True);
            Assert.That(request.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(request.HasHttpVersion, Is.True);
            Assert.That(request.HttpVersion, Is.EqualTo("HTTP/1.1"));
            Assert.That(request.HasScheme, Is.False);
            Assert.That(request.HasAuthority, Is.False);
            Assert.That(request.HasPath, Is.False);
            Assert.That(request.HasProtocol, Is.False);
            Assert.That(request.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "Host",
                "Upgrade",
                "Connection",
                "Sec-WebSocket-Key",
                "Sec-WebSocket-Version"
            }));
            Assert.That(request.Fields.Select(field => field.Value), Is.EqualTo(new[]
            {
                "example.com",
                "websocket",
                "Upgrade",
                state.ClientKey,
                "13"
            }));
            Assert.That(state.HasPendingRequest, Is.True);
            Assert.That(nonceDecode.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(state.ExpectedAccept, Is.EqualTo(expectedAccept.Value));
            Assert.That(state.OfferedSubprotocols, Is.Empty);
            Assert.That(state.OfferedExtensions, Is.Empty);
            Assert.That(Http1Writer.MeasureRequest(request).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void SerializesRequiredRequestUsingTheHttp1Writer()
    {
        var state = new Http1ClientHandshakeState();
        var result = state.CreateRequest(Options("wss://example.com:8443/socket"));
        var measurement = Http1Writer.MeasureRequest(result.Request);
        var bytes = new byte[measurement.RequiredLength];

        var write = Http1Writer.WriteRequest(result.Request, bytes);
        var text = Encoding.ASCII.GetString(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(text, Is.EqualTo(
                "GET /socket HTTP/1.1\r\n" +
                "Host: example.com:8443\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Key: {state.ClientKey}\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "\r\n"));
        });
    }

    [Test]
    public void AddsNormalizedOptionalFieldsBeforeCopiedExtraFields()
    {
        var state = new Http1ClientHandshakeState();
        var options = Options("wss://BÜCHER.example:443/a/../chat?q=naïve");
        options.HasOrigin = true;
        options.Origin = "HTTPS://BÜCHER.example:443";
        options.Subprotocols = ["chat", "Chat.v2"];
        options.Extensions =
        [
            Offer(
                "permessage-deflate",
                Parameter("client_no_context_takeover", null),
                Parameter("client_max_window_bits", "12"))
        ];
        options.HasUserAgent = true;
        options.UserAgent = "websocket-tests/1.0";
        options.ExtraFields =
        [
            Field("Cookie", "session=abc"),
            Field("Authorization", "Bearer token"),
            Field("X-Trace", "one")
        ];

        var result = state.CreateRequest(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request.RequestTarget, Is.EqualTo("/chat?q=na%C3%AFve"));
            Assert.That(result.Request.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "Host",
                "Upgrade",
                "Connection",
                "Sec-WebSocket-Key",
                "Sec-WebSocket-Version",
                "Origin",
                "Sec-WebSocket-Protocol",
                "Sec-WebSocket-Extensions",
                "User-Agent",
                "Cookie",
                "Authorization",
                "X-Trace"
            }));
            Assert.That(Value(result.Request, "Host"),
                Is.EqualTo("xn--bcher-kva.example:443"));
            Assert.That(Value(result.Request, "Origin"),
                Is.EqualTo("https://xn--bcher-kva.example"));
            Assert.That(Value(result.Request, "Sec-WebSocket-Protocol"),
                Is.EqualTo("chat, Chat.v2"));
            Assert.That(Value(result.Request, "Sec-WebSocket-Extensions"), Is.EqualTo(
                "permessage-deflate; client_no_context_takeover; client_max_window_bits=12"));
            Assert.That(Value(result.Request, "User-Agent"), Is.EqualTo("websocket-tests/1.0"));
            Assert.That(Value(result.Request, "Cookie"), Is.EqualTo("session=abc"));
            Assert.That(Value(result.Request, "Authorization"), Is.EqualTo("Bearer token"));
        });
    }

    [Test]
    public void RetainedOffersAndRequestFieldsDoNotShareMutableOptionObjects()
    {
        var state = new Http1ClientHandshakeState();
        var protocolArray = new[] { "chat" };
        var parameter = Parameter("mode", "fast");
        var offer = Offer("x-test", parameter);
        var extra = Field("X-Test", "original");
        var options = Options("ws://example.com/");
        options.Subprotocols = protocolArray;
        options.Extensions = [offer];
        options.ExtraFields = [extra];

        var result = state.CreateRequest(options);
        protocolArray[0] = "mutated";
        offer.Name = "mutated";
        parameter.Name = "mutated";
        parameter.Value = "mutated";
        extra.Name = "X-Mutated";
        extra.Value = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(state.OfferedSubprotocols, Is.EqualTo(new[] { "chat" }));
            Assert.That(state.OfferedExtensions[0], Is.Not.SameAs(offer));
            Assert.That(state.OfferedExtensions[0].Name, Is.EqualTo("x-test"));
            Assert.That(state.OfferedExtensions[0].Parameters[0], Is.Not.SameAs(parameter));
            Assert.That(state.OfferedExtensions[0].Parameters[0].Name, Is.EqualTo("mode"));
            Assert.That(state.OfferedExtensions[0].Parameters[0].Value, Is.EqualTo("fast"));
            Assert.That(Value(result.Request, "X-Test"), Is.EqualTo("original"));
            Assert.That(result.Request.Fields.Any(field => field.Name == "X-Mutated"), Is.False);
        });
    }

    [Test]
    public void RepeatedConstructionReplacesStateWithAFreshClientKey()
    {
        var state = new Http1ClientHandshakeState();
        var first = state.CreateRequest(Options("ws://first.example/"));
        var firstKey = state.ClientKey;
        var firstAccept = state.ExpectedAccept;
        var secondOptions = Options("ws://second.example/");
        secondOptions.Subprotocols = ["second"];

        var second = state.CreateRequest(secondOptions);

        Assert.Multiple(() =>
        {
            Assert.That(second.ClientKey, Is.Not.EqualTo(firstKey));
            Assert.That(state.ClientKey, Is.EqualTo(second.ClientKey));
            Assert.That(state.ExpectedAccept, Is.Not.EqualTo(firstAccept));
            Assert.That(state.OfferedSubprotocols, Is.EqualTo(new[] { "second" }));
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
        });
    }

    [TestCase("Host")]
    [TestCase("Upgrade")]
    [TestCase("Connection")]
    [TestCase("Origin")]
    [TestCase("Sec-WebSocket-Key")]
    [TestCase("Sec-WebSocket-Accept")]
    [TestCase("Sec-WebSocket-Version")]
    [TestCase("Sec-WebSocket-Protocol")]
    [TestCase("Sec-WebSocket-Extensions")]
    [TestCase("Content-Length")]
    [TestCase("Transfer-Encoding")]
    public void RejectsExtraFieldsThatCouldOverrideHandshakeSemantics(string fieldName)
    {
        var state = new Http1ClientHandshakeState();
        var options = Options("ws://example.com/");
        options.ExtraFields = [Field(fieldName.ToLowerInvariant(), "value")];

        Assert.Throws<WebSocketHandshakeException>(() => state.CreateRequest(options));
        AssertCleared(state);
    }

    [Test]
    public void AllowsNonCriticalUserAgentAsAnExtraOnlyWhenDedicatedValueIsAbsent()
    {
        var extraOnlyState = new Http1ClientHandshakeState();
        var extraOnly = Options("ws://example.com/");
        extraOnly.ExtraFields = [Field("User-Agent", "extra-agent")];
        var duplicateState = new Http1ClientHandshakeState();
        var duplicate = Options("ws://example.com/");
        duplicate.HasUserAgent = true;
        duplicate.UserAgent = "dedicated-agent";
        duplicate.ExtraFields = [Field("user-agent", "extra-agent")];

        var result = extraOnlyState.CreateRequest(extraOnly);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Request, "User-Agent"), Is.EqualTo("extra-agent"));
            Assert.Throws<WebSocketHandshakeException>(() => duplicateState.CreateRequest(duplicate));
        });
    }

    [TestCase("not a uri")]
    [TestCase("http://example.com/")]
    [TestCase("ws://user@example.com/")]
    public void RejectsInvalidWebSocketUrisAndClearsEarlierState(string uri)
    {
        var state = new Http1ClientHandshakeState();
        state.CreateRequest(Options("ws://valid.example/"));

        Assert.Throws<WebSocketHandshakeException>(() => state.CreateRequest(Options(uri)));
        AssertCleared(state);
    }

    [Test]
    public void RejectsWrongTransportUnsafeFieldsAndInvalidExtensions()
    {
        var wrongTransport = Options("wss://example.com/");
        wrongTransport.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        wrongTransport.PeerEnabledExtendedConnect = true;
        var unsafeUserAgent = Options("ws://example.com/");
        unsafeUserAgent.HasUserAgent = true;
        unsafeUserAgent.UserAgent = " bad";
        var unsafeExtra = Options("ws://example.com/");
        unsafeExtra.ExtraFields = [Field("X-Test", "bad\rvalue")];
        var invalidExtension = Options("ws://example.com/");
        invalidExtension.Extensions = [Offer("bad name")];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ClientHandshakeState().CreateRequest(wrongTransport));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ClientHandshakeState().CreateRequest(unsafeUserAgent));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ClientHandshakeState().CreateRequest(unsafeExtra));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new Http1ClientHandshakeState().CreateRequest(invalidExtension));
        });
    }

    static WebSocketClientHandshakeOptions Options(string uri)
    {
        var options = new WebSocketClientHandshakeOptions();
        options.Uri = uri;
        options.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        return options;
    }

    static string Value(WebSocketHttpRequest request, string fieldName)
    {
        return request.Fields.Single(field =>
            string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static WebSocketExtensionOffer Offer(
        string name,
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = name,
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Parameter(string name, string? value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = value != null,
            Value = value ?? ""
        };
    }

    static void AssertCleared(Http1ClientHandshakeState state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.HasPendingRequest, Is.False);
            Assert.That(state.ClientKey, Is.Empty);
            Assert.That(state.ExpectedAccept, Is.Empty);
            Assert.That(state.OfferedSubprotocols, Is.Empty);
            Assert.That(state.OfferedExtensions, Is.Empty);
        });
    }
}
