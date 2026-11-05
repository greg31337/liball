namespace websocket;

/// <summary>
/// Constructs client opening handshakes and validates the corresponding server responses.
/// </summary>
public class WebSocketClientHandshake : IWebSocketClientHandshake
{
    internal Http1ClientHandshakeState Http1State;
    internal WebSocketHttpRequest? PendingHttp1Request;
    internal ExtendedConnectClientHandshakeState ExtendedState;
    internal WebSocketHttpRequest? PendingExtendedRequest;

    /// <summary>
    /// Initializes an empty client opening-handshake state.
    /// </summary>
    public WebSocketClientHandshake()
    {
        Http1State = new Http1ClientHandshakeState();
        ExtendedState = new ExtendedConnectClientHandshakeState();
    }

    /// <summary>
    /// Creates an HTTP/1 Upgrade request and retains an independent validation snapshot.
    /// </summary>
    /// <param name="options">The client handshake options.</param>
    /// <returns>The request construction result.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the options cannot produce a safe HTTP/1 request.</exception>
    public WebSocketHandshakeResult CreateHttp1Request(WebSocketClientHandshakeOptions options)
    {
        PendingHttp1Request = null;
        WebSocketHandshakeResult result = Http1State.CreateRequest(options);
        PendingHttp1Request = CopyRequestMetadata(result.Request);
        return result;
    }

    /// <summary>
    /// Validates an HTTP/1 Upgrade response against retained request state.
    /// </summary>
    /// <param name="response">The server response metadata.</param>
    /// <returns>The response result with the retained request and complete negotiation metadata.</returns>
    /// <exception cref="WebSocketStateException">Thrown when no HTTP/1 request is awaiting a response.</exception>
    public WebSocketHandshakeResult ValidateHttp1Response(WebSocketHttpResponse response)
    {
        if (PendingHttp1Request == null)
        {
            throw new WebSocketStateException();
        }

        WebSocketHttpRequest retainedRequest = CopyRequestMetadata(PendingHttp1Request);
        WebSocketHandshakeResult result = Http1State.ValidateResponse(response);
        result.HasRequest = true;
        result.Request = retainedRequest;
        if (result.Outcome != WebSocketHandshakeOutcome.Incomplete)
        {
            PendingHttp1Request = null;
        }

        return result;
    }

    /// <summary>
    /// Creates an HTTP/2 or HTTP/3 Extended CONNECT request after peer setting enablement.
    /// </summary>
    /// <param name="options">The client handshake options.</param>
    /// <returns>The request construction result.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the options are invalid or the peer setting for the selected transport is disabled.</exception>
    public WebSocketHandshakeResult CreateExtendedConnectRequest(
        WebSocketClientHandshakeOptions options)
    {
        PendingExtendedRequest = null;
        WebSocketHandshakeResult result = ExtendedState.CreateRequest(options);
        PendingExtendedRequest = CopyRequestMetadata(result.Request);
        return result;
    }

    /// <summary>
    /// Validates an HTTP/2 or HTTP/3 Extended CONNECT response.
    /// </summary>
    /// <param name="response">The server response metadata.</param>
    /// <returns>The response result with the retained request and complete negotiation metadata.</returns>
    /// <exception cref="WebSocketStateException">Thrown when no Extended CONNECT request is awaiting a response.</exception>
    public WebSocketHandshakeResult ValidateExtendedConnectResponse(WebSocketHttpResponse response)
    {
        if (PendingExtendedRequest == null)
        {
            throw new WebSocketStateException();
        }

        WebSocketHttpRequest retainedRequest = CopyRequestMetadata(PendingExtendedRequest);
        WebSocketHandshakeResult result = ExtendedState.ValidateResponse(response);
        result.HasRequest = true;
        result.Request = retainedRequest;
        if (result.Outcome != WebSocketHandshakeOutcome.Incomplete)
        {
            PendingExtendedRequest = null;
        }

        return result;
    }

    /// <summary>
    /// Creates an independent client connection-option snapshot from an accepted handshake.
    /// </summary>
    /// <param name="handshake">The accepted client handshake result.</param>
    /// <param name="template">The caller-selected connection behavior, collaborators, and limits.</param>
    /// <returns>The frozen client connection options.</returns>
    /// <exception cref="WebSocketStateException">Thrown when the handshake or connection template is not valid for transfer.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a copied connection limit is unsafe.</exception>
    /// <exception cref="WebSocketExtensionException">Thrown when copied compression settings are invalid.</exception>
    public WebSocketConnectionOptions CreateClientConnectionOptions(
        WebSocketHandshakeResult handshake,
        WebSocketConnectionOptions template)
    {
        return HandshakeConnectionOptionsFactory.CreateOptions(
            WebSocketRole.Client,
            handshake,
            template);
    }

    static WebSocketHttpRequest CopyRequestMetadata(WebSocketHttpRequest source)
    {
        WebSocketHttpRequest copy = new WebSocketHttpRequest();
        copy.Transport = source.Transport;
        copy.Method = source.Method;
        copy.HasRequestTarget = source.HasRequestTarget;
        copy.RequestTarget = source.RequestTarget;
        copy.HasHttpVersion = source.HasHttpVersion;
        copy.HttpVersion = source.HttpVersion;
        copy.HasScheme = source.HasScheme;
        copy.Scheme = source.Scheme;
        copy.HasAuthority = source.HasAuthority;
        copy.Authority = source.Authority;
        copy.HasPath = source.HasPath;
        copy.Path = source.Path;
        copy.HasProtocol = source.HasProtocol;
        copy.Protocol = source.Protocol;
        WebSocketHeaderField[] fields = new WebSocketHeaderField[source.Fields.Length];
        int fieldIndex = 0;
        while (fieldIndex < source.Fields.Length)
        {
            WebSocketHeaderField field = new WebSocketHeaderField();
            field.Name = source.Fields[fieldIndex].Name;
            field.Value = source.Fields[fieldIndex].Value;
            fields[fieldIndex] = field;
            fieldIndex = fieldIndex + 1;
        }

        copy.Fields = fields;
        return copy;
    }
}
