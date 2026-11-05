using System.Net.Sockets;
using websocket;

namespace websocket.Tests.Autobahn;

internal class AutobahnOpenedConnection
{
    internal NetworkStream Stream = null!;
    internal WebSocketConnection Connection = null!;
    internal IWebSocketFrameCodec BoundaryCodec = null!;
    internal byte[] TrailingBytes = [];
}

internal class AutobahnHandshakeRead
{
    internal WebSocketHttp1ReadResult Result = new WebSocketHttp1ReadResult();
    internal byte[] TrailingBytes = [];
}

internal static class AutobahnHttp1Transport
{
    internal static AutobahnOpenedConnection OpenClient(string url)
    {
        Uri uri = new Uri(url);
        TcpClient tcpClient = ConnectWithRetry(uri.Host, uri.Port);
        tcpClient.NoDelay = true;
        tcpClient.ReceiveTimeout = 60000;
        tcpClient.SendTimeout = 60000;
        NetworkStream stream = tcpClient.GetStream();

        PerMessageDeflateOptions compression =
            AutobahnAdapterOptions.CreateCompressionOptions();
        PerMessageDeflateExtension extension =
            new PerMessageDeflateExtension(compression);
        IWebSocketClientHandshake handshaker = new WebSocketClientHandshake();
        WebSocketClientHandshakeOptions handshakeOptions =
            new WebSocketClientHandshakeOptions
            {
                Uri = url,
                Transport = WebSocketHandshakeTransport.Http1Upgrade,
                ExtensionHandlers = [extension],
                HasUserAgent = true,
                UserAgent = "libs11-websocket-autobahn"
            };
        WebSocketHandshakeResult created =
            handshaker.CreateHttp1Request(handshakeOptions);
        IWebSocketHttp1Codec codec = CreateCodec();
        WriteClientRequest(stream, codec, created.Request);

        AutobahnHandshakeRead parsed = ReadServerResponse(stream, codec);
        WebSocketHandshakeResult accepted =
            handshaker.ValidateHttp1Response(parsed.Result.Response);
        if (accepted.Outcome != WebSocketHandshakeOutcome.Accepted)
        {
            throw new InvalidOperationException(
                "Autobahn server rejected the client opening handshake: " +
                accepted.Error.Identifier);
        }

        WebSocketConnectionOptions connectionOptions =
            AutobahnAdapterOptions.CreateConnectionTemplate(
                extension,
                compression,
                true,
                accepted.Extensions.Length != 0);
        connectionOptions = handshaker.CreateClientConnectionOptions(
            accepted,
            connectionOptions);
        return new AutobahnOpenedConnection
        {
            Stream = stream,
            Connection = new WebSocketConnection(connectionOptions, accepted),
            BoundaryCodec = new WebSocketFrameCodec(
                WebSocketRole.Client,
                connectionOptions.MaximumFramePayloadLength,
                accepted.Extensions),
            TrailingBytes = parsed.TrailingBytes
        };
    }

    internal static AutobahnOpenedConnection AcceptServer(TcpClient tcpClient)
    {
        tcpClient.NoDelay = true;
        tcpClient.ReceiveTimeout = 60000;
        tcpClient.SendTimeout = 60000;
        NetworkStream stream = tcpClient.GetStream();
        IWebSocketHttp1Codec codec = CreateCodec();
        AutobahnHandshakeRead parsed = ReadClientRequest(stream, codec);

        PerMessageDeflateOptions compression =
            AutobahnAdapterOptions.CreateCompressionOptions();
        PerMessageDeflateExtension extension =
            new PerMessageDeflateExtension(compression);
        IWebSocketServerHandshake handshaker = new WebSocketServerHandshake();
        WebSocketServerHandshakeOptions handshakeOptions =
            new WebSocketServerHandshakeOptions
            {
                HasOriginPolicy = true,
                OriginPolicy = new AllowAllAutobahnOrigins(),
                ExtensionHandlers = [extension]
            };
        WebSocketHandshakeResult accepted = handshaker.EvaluateHttp1Request(
            parsed.Result.Request,
            handshakeOptions);
        WriteServerResponse(stream, codec, accepted.Response);
        if (accepted.Outcome != WebSocketHandshakeOutcome.Accepted)
        {
            throw new InvalidOperationException(
                "Autobahn client opening handshake was rejected: " +
                accepted.Error.Identifier);
        }

        WebSocketConnectionOptions connectionOptions =
            AutobahnAdapterOptions.CreateConnectionTemplate(
                extension,
                compression,
                false,
                accepted.Extensions.Length != 0);
        connectionOptions = handshaker.CreateServerConnectionOptions(
            accepted,
            connectionOptions);
        return new AutobahnOpenedConnection
        {
            Stream = stream,
            Connection = new WebSocketConnection(connectionOptions, accepted),
            BoundaryCodec = new WebSocketFrameCodec(
                WebSocketRole.Server,
                connectionOptions.MaximumFramePayloadLength,
                accepted.Extensions),
            TrailingBytes = parsed.TrailingBytes
        };
    }

    static IWebSocketHttp1Codec CreateCodec()
    {
        return new WebSocketHttp1Codec(
            WebSocketDefaults.MaximumHttpHeaderBytes,
            WebSocketDefaults.MaximumHttpHeaderCount,
            WebSocketDefaults.MaximumHttpLineLength);
    }

    static AutobahnHandshakeRead ReadClientRequest(
        NetworkStream stream,
        IWebSocketHttp1Codec codec)
    {
        return ReadHandshake(stream, codec, true);
    }

    static AutobahnHandshakeRead ReadServerResponse(
        NetworkStream stream,
        IWebSocketHttp1Codec codec)
    {
        return ReadHandshake(stream, codec, false);
    }

    static AutobahnHandshakeRead ReadHandshake(
        NetworkStream stream,
        IWebSocketHttp1Codec codec,
        bool request)
    {
        byte[] input = new byte[16384];
        while (true)
        {
            int count = stream.Read(input, 0, input.Length);
            if (count == 0)
            {
                throw new IOException("Transport ended during the opening handshake.");
            }

            WebSocketHttp1ReadResult result = request
                ? codec.ReadClientRequest(input.AsSpan(0, count))
                : codec.ReadServerResponse(input.AsSpan(0, count));
            if (result.Status == WebSocketParseStatus.Complete)
            {
                return new AutobahnHandshakeRead
                {
                    Result = result,
                    TrailingBytes = input
                        .AsSpan(result.ConsumedCount, count - result.ConsumedCount)
                        .ToArray()
                };
            }

            if (result.Status != WebSocketParseStatus.NeedInput)
            {
                throw new InvalidOperationException(
                    "Opening handshake parse failed: " + result.Error.Identifier);
            }
        }
    }

    static void WriteClientRequest(
        NetworkStream stream,
        IWebSocketHttp1Codec codec,
        WebSocketHttpRequest request)
    {
        byte[] bytes = new byte[codec.MeasureClientRequest(request)];
        int count = codec.WriteClientRequest(request, bytes);
        stream.Write(bytes, 0, count);
    }

    static void WriteServerResponse(
        NetworkStream stream,
        IWebSocketHttp1Codec codec,
        WebSocketHttpResponse response)
    {
        byte[] bytes = new byte[codec.MeasureServerResponse(response)];
        int count = codec.WriteServerResponse(response, bytes);
        stream.Write(bytes, 0, count);
    }

    static TcpClient ConnectWithRetry(string host, int port)
    {
        int attempt = 0;
        while (true)
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(host, port);
                return client;
            }
            catch (SocketException)
            {
                client.Dispose();
                attempt++;
                if (attempt >= 80)
                {
                    throw;
                }

                Thread.Sleep(250);
            }
        }
    }
}
