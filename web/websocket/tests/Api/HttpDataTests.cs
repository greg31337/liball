using websocket;

namespace websocket.Tests;

public class HttpDataTests
{
    [Test]
    public void HeaderFieldsPreserveOrderedDuplicates()
    {
        var first = new WebSocketHeaderField();
        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.Empty);
            Assert.That(first.Value, Is.Empty);
        });

        first.Name = "Cookie";
        first.Value = "first=1";
        var second = new WebSocketHeaderField();
        second.Name = "Cookie";
        second.Value = "second=2";
        var fields = new[] { first, second };
        var request = new WebSocketHttpRequest();
        request.Fields = fields;

        Assert.Multiple(() =>
        {
            Assert.That(request.Fields, Is.SameAs(fields));
            Assert.That(request.Fields.Select(field => field.Name), Is.EqualTo(new[] { "Cookie", "Cookie" }));
            Assert.That(request.Fields.Select(field => field.Value), Is.EqualTo(new[] { "first=1", "second=2" }));
        });
    }

    [Test]
    public void HttpRequestCarriesHttp1AndExtendedConnectMetadata()
    {
        var request = new WebSocketHttpRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(request.Method, Is.Empty);
            Assert.That(request.HasRequestTarget, Is.False);
            Assert.That(request.RequestTarget, Is.Empty);
            Assert.That(request.HasHttpVersion, Is.False);
            Assert.That(request.HttpVersion, Is.Empty);
            Assert.That(request.HasScheme, Is.False);
            Assert.That(request.Scheme, Is.Empty);
            Assert.That(request.HasAuthority, Is.False);
            Assert.That(request.Authority, Is.Empty);
            Assert.That(request.HasPath, Is.False);
            Assert.That(request.Path, Is.Empty);
            Assert.That(request.HasProtocol, Is.False);
            Assert.That(request.Protocol, Is.Empty);
            Assert.That(request.Fields, Is.Empty);
        });

        request.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        request.Method = "CONNECT";
        request.HasRequestTarget = true;
        request.RequestTarget = "/chat?room=1";
        request.HasHttpVersion = true;
        request.HttpVersion = "HTTP/1.1";
        request.HasScheme = true;
        request.Scheme = "https";
        request.HasAuthority = true;
        request.Authority = "example.com";
        request.HasPath = true;
        request.Path = "/chat?room=1";
        request.HasProtocol = true;
        request.Protocol = "websocket";

        Assert.Multiple(() =>
        {
            Assert.That(request.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(request.Method, Is.EqualTo("CONNECT"));
            Assert.That(request.HasRequestTarget, Is.True);
            Assert.That(request.RequestTarget, Is.EqualTo("/chat?room=1"));
            Assert.That(request.HasHttpVersion, Is.True);
            Assert.That(request.HttpVersion, Is.EqualTo("HTTP/1.1"));
            Assert.That(request.HasScheme, Is.True);
            Assert.That(request.Scheme, Is.EqualTo("https"));
            Assert.That(request.HasAuthority, Is.True);
            Assert.That(request.Authority, Is.EqualTo("example.com"));
            Assert.That(request.HasPath, Is.True);
            Assert.That(request.Path, Is.EqualTo("/chat?room=1"));
            Assert.That(request.HasProtocol, Is.True);
            Assert.That(request.Protocol, Is.EqualTo("websocket"));
        });
    }

    [Test]
    public void HttpResponseUsesExplicitFlagsForOptionalMetadata()
    {
        var response = new WebSocketHttpResponse();
        var fields = new[]
        {
            new WebSocketHeaderField
            {
                Name = "Content-Type",
                Value = "text/plain"
            }
        };
        byte[] body = [1, 2, 3];

        Assert.Multiple(() =>
        {
            Assert.That(response.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(response.StatusCode, Is.Zero);
            Assert.That(response.HasReasonPhrase, Is.False);
            Assert.That(response.ReasonPhrase, Is.Empty);
            Assert.That(response.Fields, Is.Empty);
            Assert.That(response.HasBody, Is.False);
            Assert.That(response.Body, Is.Empty);
        });

        response.Transport = WebSocketHandshakeTransport.Http1Upgrade;
        response.StatusCode = 403;
        response.HasReasonPhrase = true;
        response.ReasonPhrase = "Forbidden";
        response.Fields = fields;
        response.HasBody = true;
        response.Body = body;

        Assert.Multiple(() =>
        {
            Assert.That(response.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(response.StatusCode, Is.EqualTo(403));
            Assert.That(response.HasReasonPhrase, Is.True);
            Assert.That(response.ReasonPhrase, Is.EqualTo("Forbidden"));
            Assert.That(response.Fields, Is.SameAs(fields));
            Assert.That(response.Fields[0].Name, Is.EqualTo("Content-Type"));
            Assert.That(response.HasBody, Is.True);
            Assert.That(response.Body, Is.SameAs(body));
            Assert.That(response.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void HttpReadResultDistinguishesRequestResponseAndErrorPresence()
    {
        var result = new WebSocketHttp1ReadResult();
        var second = new WebSocketHttp1ReadResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.HasRequest, Is.False);
            Assert.That(result.HasResponse, Is.False);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Request, Is.Not.SameAs(second.Request));
            Assert.That(result.Response, Is.Not.SameAs(second.Response));
            Assert.That(result.Error, Is.Not.SameAs(second.Error));
        });

        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = 87;
        result.HasRequest = true;
        result.Request.Method = "GET";
        result.HasResponse = false;
        result.Response.StatusCode = 101;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.Handshake;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(87));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.HasResponse, Is.False);
            Assert.That(result.Response.StatusCode, Is.EqualTo(101));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
        });
    }

    [Test]
    public void ErrorCarriesRawKnownCloseAndHttpStatusDetails()
    {
        var error = new WebSocketError();

        Assert.Multiple(() =>
        {
            Assert.That(error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(error.HasCloseCode, Is.False);
            Assert.That(error.CloseCode, Is.Zero);
            Assert.That(error.HasKnownCloseCode, Is.False);
            Assert.That((int)error.KnownCloseCode, Is.Zero);
            Assert.That(error.HasHttpStatusCode, Is.False);
            Assert.That(error.HttpStatusCode, Is.Zero);
            Assert.That(error.Identifier, Is.Empty);
            Assert.That(error.Diagnostic, Is.Empty);
        });

        error.Kind = WebSocketErrorKind.Handshake;
        error.HasCloseCode = true;
        error.CloseCode = 1002;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.ProtocolError;
        error.HasHttpStatusCode = true;
        error.HttpStatusCode = 400;
        error.Identifier = "invalid-upgrade";
        error.Diagnostic = "The Upgrade token is missing.";

        Assert.Multiple(() =>
        {
            Assert.That(error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(error.HasCloseCode, Is.True);
            Assert.That(error.CloseCode, Is.EqualTo(1002));
            Assert.That(error.HasKnownCloseCode, Is.True);
            Assert.That(error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.ProtocolError));
            Assert.That(error.HasHttpStatusCode, Is.True);
            Assert.That(error.HttpStatusCode, Is.EqualTo(400));
            Assert.That(error.Identifier, Is.EqualTo("invalid-upgrade"));
            Assert.That(error.Diagnostic, Is.EqualTo("The Upgrade token is missing."));
        });
    }

    [Test]
    public void DefaultHttpContainersHaveEmptyCollectionsAndIndependentObjects()
    {
        var firstRequest = new WebSocketHttpRequest();
        var secondRequest = new WebSocketHttpRequest();
        var firstResponse = new WebSocketHttpResponse();
        var secondResponse = new WebSocketHttpResponse();
        var firstResult = new WebSocketHttp1ReadResult();
        var secondResult = new WebSocketHttp1ReadResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstRequest.Fields, Is.Empty);
            Assert.That(secondRequest.Fields, Is.Empty);
            Assert.That(firstResponse.Fields, Is.Empty);
            Assert.That(secondResponse.Fields, Is.Empty);
            Assert.That(firstResponse.Body, Is.Empty);
            Assert.That(secondResponse.Body, Is.Empty);
            Assert.That(firstResult.Request, Is.Not.SameAs(secondResult.Request));
            Assert.That(firstResult.Response, Is.Not.SameAs(secondResult.Response));
            Assert.That(firstResult.Error, Is.Not.SameAs(secondResult.Error));
        });
    }
}
