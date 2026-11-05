using websocket;

namespace websocket.Tests;

public class HandshakeDataTests
{
    [Test]
    public void ClientOptionsHaveBoundedDefaultsAndEmptyCollections()
    {
        var first = new WebSocketClientHandshakeOptions();
        var second = new WebSocketClientHandshakeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.Uri, Is.Empty);
            Assert.That(first.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(first.HasOrigin, Is.False);
            Assert.That(first.Origin, Is.Empty);
            Assert.That(first.HasUserAgent, Is.False);
            Assert.That(first.UserAgent, Is.Empty);
            Assert.That(first.PeerEnabledExtendedConnect, Is.False);
            Assert.That(first.MaximumHeaderBytes, Is.EqualTo(WebSocketDefaults.MaximumHttpHeaderBytes));
            Assert.That(first.MaximumHeaderCount, Is.EqualTo(WebSocketDefaults.MaximumHttpHeaderCount));
            Assert.That(first.MaximumLineLength, Is.EqualTo(WebSocketDefaults.MaximumHttpLineLength));
            Assert.That(first.Subprotocols, Is.Empty);
            Assert.That(second.Subprotocols, Is.Empty);
            Assert.That(first.Extensions, Is.Empty);
            Assert.That(second.Extensions, Is.Empty);
            Assert.That(first.ExtensionHandlers, Is.Empty);
            Assert.That(second.ExtensionHandlers, Is.Empty);
            Assert.That(first.ExtraFields, Is.Empty);
            Assert.That(second.ExtraFields, Is.Empty);
        });
    }

    [Test]
    public void ClientOptionsCarryOrderedProtocolsExtensionsAndExtraFields()
    {
        var parameter = new WebSocketExtensionParameter();
        parameter.Name = "client_max_window_bits";
        parameter.HasValue = true;
        parameter.Value = "12";
        var extension = new WebSocketExtensionOffer();
        extension.Name = "permessage-deflate";
        extension.Parameters = [parameter];
        var extraField = new WebSocketHeaderField();
        extraField.Name = "Authorization";
        extraField.Value = "Bearer opaque";
        var protocols = new[] { "chat", "superchat" };
        var extensions = new[] { extension };
        var handlers = new IWebSocketExtension[] { new DataTestExtension() };
        var extraFields = new[] { extraField };
        var options = new WebSocketClientHandshakeOptions();
        options.Uri = "wss://example.com/chat";
        options.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        options.HasOrigin = true;
        options.Origin = "https://example.com";
        options.Subprotocols = protocols;
        options.Extensions = extensions;
        options.ExtensionHandlers = handlers;
        options.ExtraFields = extraFields;
        options.HasUserAgent = true;
        options.UserAgent = "websocket-tests";
        options.PeerEnabledExtendedConnect = true;
        options.MaximumHeaderBytes = 12000;
        options.MaximumHeaderCount = 80;
        options.MaximumLineLength = 6000;

        Assert.Multiple(() =>
        {
            Assert.That(options.Uri, Is.EqualTo("wss://example.com/chat"));
            Assert.That(options.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http3ExtendedConnect));
            Assert.That(options.HasOrigin, Is.True);
            Assert.That(options.Origin, Is.EqualTo("https://example.com"));
            Assert.That(options.Subprotocols, Is.SameAs(protocols));
            Assert.That(options.Subprotocols, Is.EqualTo(new[] { "chat", "superchat" }));
            Assert.That(options.Extensions, Is.SameAs(extensions));
            Assert.That(options.Extensions[0].Name, Is.EqualTo("permessage-deflate"));
            Assert.That(options.Extensions[0].Parameters[0].HasValue, Is.True);
            Assert.That(options.Extensions[0].Parameters[0].Value, Is.EqualTo("12"));
            Assert.That(options.ExtensionHandlers, Is.SameAs(handlers));
            Assert.That(options.ExtraFields, Is.SameAs(extraFields));
            Assert.That(options.HasUserAgent, Is.True);
            Assert.That(options.UserAgent, Is.EqualTo("websocket-tests"));
            Assert.That(options.PeerEnabledExtendedConnect, Is.True);
            Assert.That(options.MaximumHeaderBytes, Is.EqualTo(12000));
            Assert.That(options.MaximumHeaderCount, Is.EqualTo(80));
            Assert.That(options.MaximumLineLength, Is.EqualTo(6000));
        });
    }

    [Test]
    public void ServerOptionsDefaultToHttp1AndSafeRejectionMetadata()
    {
        var first = new WebSocketServerHandshakeOptions();
        var second = new WebSocketServerHandshakeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(first.SupportedTransports, Is.EqualTo(new[] { WebSocketHandshakeTransport.Http1Upgrade }));
            Assert.That(first.ExtendedConnectEnabled, Is.False);
            Assert.That(first.HasOriginPolicy, Is.False);
            Assert.That(first.OriginPolicy, Is.Null);
            Assert.That(first.HasSubprotocolSelector, Is.False);
            Assert.That(first.SubprotocolSelector, Is.Null);
            Assert.That(first.RejectionStatusCode, Is.EqualTo(403));
            Assert.That(first.HasRejectionBody, Is.False);
            Assert.That(first.RejectionBody, Is.Empty);
            Assert.That(first.MaximumHeaderBytes, Is.EqualTo(WebSocketDefaults.MaximumHttpHeaderBytes));
            Assert.That(first.MaximumHeaderCount, Is.EqualTo(WebSocketDefaults.MaximumHttpHeaderCount));
            Assert.That(first.MaximumLineLength, Is.EqualTo(WebSocketDefaults.MaximumHttpLineLength));
            Assert.That(first.SupportedTransports, Is.Not.SameAs(second.SupportedTransports));
            Assert.That(first.SupportedSubprotocols, Is.Empty);
            Assert.That(second.SupportedSubprotocols, Is.Empty);
            Assert.That(first.SupportedExtensions, Is.Empty);
            Assert.That(second.SupportedExtensions, Is.Empty);
            Assert.That(first.ExtensionHandlers, Is.Empty);
            Assert.That(second.ExtensionHandlers, Is.Empty);
            Assert.That(first.ExtraFields, Is.Empty);
            Assert.That(second.ExtraFields, Is.Empty);
            Assert.That(first.RejectionFields, Is.Empty);
            Assert.That(second.RejectionFields, Is.Empty);
            Assert.That(first.RejectionBody, Is.Empty);
            Assert.That(second.RejectionBody, Is.Empty);
        });
    }

    [Test]
    public void ServerOptionsCarryPoliciesPreferencesAndRejectionMetadata()
    {
        var originPolicy = new TestOriginPolicy();
        var selector = new TestSubprotocolSelector();
        var body = new byte[] { 1, 2, 3 };
        var supportedExtension = new WebSocketExtensionOffer
        {
            Name = "x-test"
        };
        var handler = new DataTestExtension();
        var extraField = new WebSocketHeaderField
        {
            Name = "X-Server",
            Value = "yes"
        };
        var rejectionField = new WebSocketHeaderField
        {
            Name = "Retry-After",
            Value = "30"
        };
        var options = new WebSocketServerHandshakeOptions();
        options.SupportedTransports =
        [
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            WebSocketHandshakeTransport.Http3ExtendedConnect
        ];
        options.ExtendedConnectEnabled = true;
        options.HasOriginPolicy = true;
        options.OriginPolicy = originPolicy;
        options.HasSubprotocolSelector = true;
        options.SubprotocolSelector = selector;
        options.SupportedSubprotocols = ["superchat", "chat"];
        options.SupportedExtensions = [supportedExtension];
        options.ExtensionHandlers = [handler];
        options.ExtraFields = [extraField];
        options.RejectionStatusCode = 404;
        options.RejectionFields = [rejectionField];
        options.HasRejectionBody = true;
        options.RejectionBody = body;
        options.MaximumHeaderBytes = 12000;
        options.MaximumHeaderCount = 80;
        options.MaximumLineLength = 6000;

        Assert.Multiple(() =>
        {
            Assert.That(options.SupportedTransports, Is.EqualTo(new[]
            {
                WebSocketHandshakeTransport.Http2ExtendedConnect,
                WebSocketHandshakeTransport.Http3ExtendedConnect
            }));
            Assert.That(options.ExtendedConnectEnabled, Is.True);
            Assert.That(options.HasOriginPolicy, Is.True);
            Assert.That(options.OriginPolicy, Is.SameAs(originPolicy));
            Assert.That(options.HasSubprotocolSelector, Is.True);
            Assert.That(options.SubprotocolSelector, Is.SameAs(selector));
            Assert.That(options.SupportedSubprotocols, Is.EqualTo(new[] { "superchat", "chat" }));
            Assert.That(options.SupportedExtensions[0], Is.SameAs(supportedExtension));
            Assert.That(options.ExtensionHandlers[0], Is.SameAs(handler));
            Assert.That(options.ExtraFields[0], Is.SameAs(extraField));
            Assert.That(options.RejectionStatusCode, Is.EqualTo(404));
            Assert.That(options.RejectionFields[0], Is.SameAs(rejectionField));
            Assert.That(options.HasRejectionBody, Is.True);
            Assert.That(options.RejectionBody, Is.SameAs(body));
            Assert.That(options.MaximumHeaderBytes, Is.EqualTo(12000));
            Assert.That(options.MaximumHeaderCount, Is.EqualTo(80));
            Assert.That(options.MaximumLineLength, Is.EqualTo(6000));
        });
    }

    [Test]
    public void HandshakeResultDefaultsToIncompleteWithoutOptionalValues()
    {
        var first = new WebSocketHandshakeResult();
        var second = new WebSocketHandshakeResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Incomplete));
            Assert.That(first.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(first.HasRequest, Is.False);
            Assert.That(first.HasResponse, Is.False);
            Assert.That(first.HasSelectedSubprotocol, Is.False);
            Assert.That(first.SelectedSubprotocol, Is.Empty);
            Assert.That(first.HasClientKey, Is.False);
            Assert.That(first.ClientKey, Is.Empty);
            Assert.That(first.HasError, Is.False);
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.Response, Is.Not.SameAs(second.Response));
            Assert.That(first.Extensions, Is.Empty);
            Assert.That(second.Extensions, Is.Empty);
            Assert.That(first.Error, Is.Not.SameAs(second.Error));
        });
    }

    [Test]
    public void HandshakeResultCarriesAcceptedNegotiationMetadata()
    {
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Status = WebSocketExtensionStatus.Negotiated;
        var result = new WebSocketHandshakeResult();
        result.Outcome = WebSocketHandshakeOutcome.Accepted;
        result.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        result.HasRequest = true;
        result.Request.Method = "GET";
        result.HasResponse = true;
        result.Response.StatusCode = 101;
        result.HasSelectedSubprotocol = true;
        result.SelectedSubprotocol = "chat";
        result.Extensions = [negotiation];
        result.HasClientKey = true;
        result.ClientKey = "dGhlIHNhbXBsZSBub25jZQ==";
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(101));
            Assert.That(result.HasSelectedSubprotocol, Is.True);
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(result.Extensions[0].Status, Is.EqualTo(WebSocketExtensionStatus.Negotiated));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.EqualTo("dGhlIHNhbXBsZSBub25jZQ=="));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
        });
    }
}

internal class TestOriginPolicy : IWebSocketOriginPolicy
{
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        return request.Method == "GET";
    }
}

internal class TestSubprotocolSelector : IWebSocketSubprotocolSelector
{
    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        if (clientProtocols.Length == 0 || serverProtocols.Length == 0)
        {
            return "";
        }

        return clientProtocols[0];
    }
}
