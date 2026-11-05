using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dns;

internal class DnsTcpServerListener
{
    internal Socket? Socket;
    internal IPEndPoint? Endpoint;
}

internal class DnsTcpServerClient
{
    internal Socket? Socket;
    internal IPEndPoint? ClientEndpoint;
    internal IPEndPoint? ServerEndpoint;
    internal DnsClockValue AcceptedAt = new DnsClockValue();
    internal DnsClockValue LastActivity = new DnsClockValue();
    internal byte[] LengthPrefix = new byte[DnsProtocolLimits.TcpLengthPrefixLength];
    internal int LengthPrefixReceived;
    internal byte[] MessageBytes = [];
    internal int MessageBytesReceived;
    internal byte[] ResponseFrame = [];
    internal int ResponseFrameSent;
    internal List<byte[]> ResponseMessages = new List<byte[]>();
    internal int ResponseMessageIndex;
    internal int RequestCount;
    internal bool CurrentRequestCounted;
    internal bool Closed;
}

internal static class TcpServerOperations
{
    const int StopPollMilliseconds = 10;

    internal static DnsServerRunResult Run(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        ValidateOptions(server, options, services);
        DnsServerRunResult result = new DnsServerRunResult();
        result.StartedAt = TransportTime.ReadClock();
        result.Status = DnsServerStatus.Running;
        if (IsStopRequested(server, options))
        {
            CompleteCancellation(result);
            return result;
        }

        List<DnsTcpServerListener> listeners = BindListeners(
            options,
            result);
        List<DnsTcpServerClient> clients =
            new List<DnsTcpServerClient>();
        RunLoop(
            server,
            options,
            services,
            listeners,
            clients,
            result);
        DisposeClients(clients);
        DisposeListeners(listeners);
        result.CompletedAt = TransportTime.ReadClock();
        return result;
    }

    static void ValidateOptions(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        if (server == null || options == null ||
            services == null)
        {
            throw new ArgumentNullException("server");
        }

        if (options.Authoritative == null ||
            options.Authoritative.Authorization == null ||
            options.Authoritative.Security == null ||
            options.Endpoints == null || options.TsigKeys == null)
        {
            throw new ArgumentException(
                "Complete authoritative server options are required.");
        }

        if (options.MaximumRequestLength < DnsProtocolLimits.HeaderLength ||
            options.MaximumRequestLength >
                DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumConcurrentRequests < 1 ||
            options.TcpListenBacklog < 1 ||
            options.TcpIdleTimeoutMilliseconds < 0 ||
            options.TcpConnectionLifetimeMilliseconds < 0 ||
            options.RequestTimeoutMilliseconds < 0 ||
            options.MaximumTcpRequestsPerConnection < 1 ||
            options.Authoritative.MaximumTcpResponseLength <
                DnsProtocolLimits.HeaderLength ||
            options.Authoritative.MaximumTcpResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        if (options.Endpoints.Count == 0)
        {
            throw new ArgumentException(
                "At least one TCP server endpoint is required.");
        }
    }

    static List<DnsTcpServerListener> BindListeners(
        DnsServerOptions options,
        DnsServerRunResult result)
    {
        List<DnsTcpServerListener> listeners =
            new List<DnsTcpServerListener>();
        int endpointIndex = 0;
        while (endpointIndex < options.Endpoints.Count)
        {
            List<IPEndPoint> endpoints = EndpointOperations.Resolve(
                options.Endpoints[endpointIndex]);
            int addressIndex = 0;
            while (addressIndex < endpoints.Count)
            {
                IPEndPoint endpoint = endpoints[addressIndex];
                Socket socket = new Socket(
                    endpoint.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    socket.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.IPv6Only,
                        true);
                }

                socket.Bind(endpoint);
                socket.Listen(options.TcpListenBacklog);
                EndPoint? localObject = socket.LocalEndPoint;
                if (localObject == null)
                {
                    socket.Dispose();
                    throw new InvalidOperationException(
                        "The TCP listener did not expose its bound endpoint.");
                }

                IPEndPoint localEndpoint = (IPEndPoint)localObject;
                DnsTcpServerListener listener =
                    new DnsTcpServerListener();
                listener.Socket = socket;
                listener.Endpoint = localEndpoint;
                listeners.Add(listener);
                result.BoundEndpoints.Add(
                    EndpointOperations.ToDnsEndpoint(localEndpoint));
                addressIndex = addressIndex + 1;
            }

            endpointIndex = endpointIndex + 1;
        }

        return listeners;
    }

    static void RunLoop(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        List<DnsTcpServerListener> listeners,
        List<DnsTcpServerClient> clients,
        DnsServerRunResult result)
    {
        while (!IsStopRequested(server, options) &&
            result.Status == DnsServerStatus.Running)
        {
            bool processed = AcceptConnections(
                server,
                options,
                listeners,
                clients,
                result);
            if (ProcessClients(
                server,
                options,
                services,
                clients,
                result))
            {
                processed = true;
            }

            if (!processed && result.Status == DnsServerStatus.Running &&
                !IsStopRequested(server, options))
            {
                Thread.Sleep(StopPollMilliseconds);
            }
        }

        if (result.Status == DnsServerStatus.Running)
        {
            CompleteCancellation(result);
        }
        else if (result.Status == DnsServerStatus.Failed)
        {
            server.StopServer();
        }
    }

    static bool AcceptConnections(
        DnsServer server,
        DnsServerOptions options,
        List<DnsTcpServerListener> listeners,
        List<DnsTcpServerClient> clients,
        DnsServerRunResult result)
    {
        bool processed = false;
        int index = 0;
        while (index < listeners.Count &&
            !IsStopRequested(server, options))
        {
            Socket? listener = listeners[index].Socket;
            if (listener != null &&
                listener.Poll(0, SelectMode.SelectError))
            {
                result.Status = DnsServerStatus.Failed;
                AddDiagnostic(
                    result,
                    DnsDiagnosticCode.ResponseMismatch,
                    "tcpListener",
                    "A TCP server listener entered an error state.");
            }
            else if (listener != null &&
                listener.Poll(0, SelectMode.SelectRead))
            {
                Socket socket = listener.Accept();
                if (clients.Count >= options.MaximumConcurrentRequests)
                {
                    socket.Dispose();
                }
                else
                {
                    AcceptConnection(socket, clients);
                }

                processed = true;
            }

            index = index + 1;
        }

        return processed;
    }

    static void AcceptConnection(
        Socket socket,
        List<DnsTcpServerClient> clients)
    {
        EndPoint? remoteObject = socket.RemoteEndPoint;
        EndPoint? localObject = socket.LocalEndPoint;
        if (remoteObject == null || localObject == null)
        {
            socket.Dispose();
            return;
        }

        socket.SetSocketOption(
            SocketOptionLevel.Tcp,
            SocketOptionName.NoDelay,
            true);
        DnsClockValue acceptedAt = TransportTime.ReadClock();
        DnsTcpServerClient client = new DnsTcpServerClient();
        client.Socket = socket;
        client.ClientEndpoint = (IPEndPoint)remoteObject;
        client.ServerEndpoint = (IPEndPoint)localObject;
        client.AcceptedAt = acceptedAt;
        client.LastActivity = acceptedAt;
        clients.Add(client);
    }

    static bool ProcessClients(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        List<DnsTcpServerClient> clients,
        DnsServerRunResult result)
    {
        bool processed = false;
        int index = clients.Count - 1;
        while (index >= 0)
        {
            DnsTcpServerClient client = clients[index];
            if (client.Closed)
            {
                clients.RemoveAt(index);
            }
            else
            {
                if (ProcessClient(
                    server,
                    options,
                services,
                    client,
                    result))
                {
                    processed = true;
                }

                if (client.Closed)
                {
                    clients.RemoveAt(index);
                }
            }

            index = index - 1;
        }

        return processed;
    }

    static bool ProcessClient(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        DnsTcpServerClient client,
        DnsServerRunResult result)
    {
        Socket? socket = client.Socket;
        if (socket == null)
        {
            client.Closed = true;
            return false;
        }

        DnsClockValue now = TransportTime.ReadClock();
        long idle = TransportTime.ElapsedMilliseconds(
            client.LastActivity,
            now);
        long lifetime = TransportTime.ElapsedMilliseconds(
            client.AcceptedAt,
            now);
        if (idle >= options.TcpIdleTimeoutMilliseconds ||
            lifetime >= options.TcpConnectionLifetimeMilliseconds)
        {
            if (client.LengthPrefixReceived > 0 ||
                client.MessageBytesReceived > 0 ||
                client.ResponseFrameSent < client.ResponseFrame.Length)
            {
                RecordFailure(
                    result,
                    DnsDiagnosticCode.InvalidTcpFrame,
                    "tcpFrame",
                    "The TCP connection expired during a request or response frame.");
            }

            CloseClient(client);
            return true;
        }

        if (socket.Poll(0, SelectMode.SelectError))
        {
            CloseClient(client);
            return true;
        }

        if (client.ResponseFrameSent < client.ResponseFrame.Length)
        {
            if (socket.Poll(0, SelectMode.SelectWrite))
            {
                SendClient(client);
                return true;
            }

            return false;
        }

        if (!client.CurrentRequestCounted &&
            client.RequestCount >=
                options.MaximumTcpRequestsPerConnection)
        {
            CloseClient(client);
            return true;
        }

        if (!socket.Poll(0, SelectMode.SelectRead))
        {
            return false;
        }

        ReceiveClient(
            server,
            options,
            services,
            client,
            result);
        return true;
    }

    static void ReceiveClient(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        DnsTcpServerClient client,
        DnsServerRunResult result)
    {
        Socket? socket = client.Socket;
        if (socket == null)
        {
            client.Closed = true;
            return;
        }

        if (client.LengthPrefixReceived < client.LengthPrefix.Length)
        {
            int received = socket.Receive(
                client.LengthPrefix,
                client.LengthPrefixReceived,
                client.LengthPrefix.Length - client.LengthPrefixReceived,
                SocketFlags.None);
            if (received == 0)
            {
                if (client.LengthPrefixReceived > 0)
                {
                    RecordFailure(
                        result,
                        DnsDiagnosticCode.InvalidTcpFrame,
                        "tcpFrame.lengthPrefix",
                        "The TCP connection closed during a length prefix.");
                }

                CloseClient(client);
                return;
            }

            client.LengthPrefixReceived =
                client.LengthPrefixReceived + received;
            CountRequest(client, result);
            client.LastActivity = TransportTime.ReadClock();
            if (client.LengthPrefixReceived < client.LengthPrefix.Length)
            {
                return;
            }

            int messageLength =
                (client.LengthPrefix[0] << 8) | client.LengthPrefix[1];
            if (messageLength < DnsProtocolLimits.HeaderLength ||
                messageLength > options.MaximumRequestLength)
            {
                RecordFailure(
                    result,
                    DnsDiagnosticCode.InvalidTcpFrame,
                    "tcpFrame.messageLength",
                    "The TCP request length is zero, too short, or exceeds the configured limit.");
                CloseClient(client);
                return;
            }

            client.MessageBytes = new byte[messageLength];
            return;
        }

        if (client.MessageBytesReceived < client.MessageBytes.Length)
        {
            int received = socket.Receive(
                client.MessageBytes,
                client.MessageBytesReceived,
                client.MessageBytes.Length - client.MessageBytesReceived,
                SocketFlags.None);
            if (received == 0)
            {
                RecordFailure(
                    result,
                    DnsDiagnosticCode.InvalidTcpFrame,
                    "tcpFrame.message",
                    "The TCP connection closed during a request message.");
                CloseClient(client);
                return;
            }

            client.MessageBytesReceived =
                client.MessageBytesReceived + received;
            client.LastActivity = TransportTime.ReadClock();
            if (client.MessageBytesReceived < client.MessageBytes.Length)
            {
                return;
            }
        }

        ProcessFrame(
            server,
            options,
            services,
            client,
            result);
    }

    static void ProcessFrame(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        DnsTcpServerClient client,
        DnsServerRunResult result)
    {
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        decodeOptions.MaximumMessageLength = options.MaximumRequestLength;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            client.MessageBytes,
            decodeOptions);
        if (!decoded.Success || decoded.Message == null)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
            CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
            QueueResponse(client, CreateFormatError(client.MessageBytes));
            ResetFrame(client);
            return;
        }

        IPEndPoint? clientEndpoint = client.ClientEndpoint;
        IPEndPoint? serverEndpoint = client.ServerEndpoint;
        if (clientEndpoint == null || serverEndpoint == null)
        {
            RecordFailure(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "tcpConnection",
                "The TCP connection endpoints are unavailable.");
            CloseClient(client);
            return;
        }

        DnsServerRequestContext context = new DnsServerRequestContext();
        context.Message = decoded.Message;
        context.MessageBytes = client.MessageBytes;
        context.ClientEndpoint = EndpointOperations.ToDnsEndpoint(
            clientEndpoint);
        context.ServerEndpoint = EndpointOperations.ToDnsEndpoint(
            serverEndpoint);
        context.Transport = DnsTransportKind.Tcp;
        context.CancellationRequested = IsStopRequested(server, options);
        DnsServerDispatchResult dispatch =
            DnsServerRequestOperations.Dispatch(
                context,
                options,
                services);
        CopyDiagnostics(dispatch.Diagnostics, result.Diagnostics);
        if (dispatch.Responses.Count == 0)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
            CloseClient(client);
            return;
        }

        int responseIndex = 0;
        while (responseIndex < dispatch.Responses.Count)
        {
            if (dispatch.Responses[responseIndex].Length >
                options.Authoritative.MaximumTcpResponseLength)
            {
                RecordFailure(
                    result,
                    DnsDiagnosticCode.ResourceLimitExceeded,
                    "responseBytes",
                    "The TCP response exceeds the configured response limit.");
                CloseClient(client);
                return;
            }

            responseIndex = responseIndex + 1;
        }

        QueueResponseSet(client, dispatch.Responses);
        if (dispatch.Failed)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
        }

        ResetFrame(client);
    }

    static byte[] CreateFormatError(byte[] requestBytes)
    {
        byte[] response = new byte[DnsProtocolLimits.HeaderLength];
        response[0] = requestBytes[0];
        response[1] = requestBytes[1];
        response[2] = (byte)(128 | (requestBytes[2] & 121));
        response[3] = (byte)DnsResponseCode.FormatError;
        return response;
    }

    static void QueueResponse(
        DnsTcpServerClient client,
        byte[] responseBytes)
    {
        client.ResponseMessages.Clear();
        client.ResponseMessages.Add(responseBytes);
        client.ResponseMessageIndex = 0;
        ActivateResponse(client);
    }

    static void QueueResponseSet(
        DnsTcpServerClient client,
        List<byte[]> responseMessages)
    {
        client.ResponseMessages.Clear();
        int index = 0;
        while (index < responseMessages.Count)
        {
            client.ResponseMessages.Add(responseMessages[index]);
            index = index + 1;
        }

        client.ResponseMessageIndex = 0;
        ActivateResponse(client);
    }

    static void ActivateResponse(DnsTcpServerClient client)
    {
        byte[] responseBytes =
            client.ResponseMessages[client.ResponseMessageIndex];
        byte[] frame = new byte[
            DnsProtocolLimits.TcpLengthPrefixLength + responseBytes.Length];
        frame[0] = (byte)(responseBytes.Length >> 8);
        frame[1] = (byte)(responseBytes.Length & 255);
        Array.Copy(
            responseBytes,
            0,
            frame,
            DnsProtocolLimits.TcpLengthPrefixLength,
            responseBytes.Length);
        client.ResponseFrame = frame;
        client.ResponseFrameSent = 0;
    }

    static void SendClient(DnsTcpServerClient client)
    {
        Socket? socket = client.Socket;
        if (socket == null)
        {
            client.Closed = true;
            return;
        }

        int sent = socket.Send(
            client.ResponseFrame,
            client.ResponseFrameSent,
            client.ResponseFrame.Length - client.ResponseFrameSent,
            SocketFlags.None);
        if (sent == 0)
        {
            CloseClient(client);
            return;
        }

        client.ResponseFrameSent = client.ResponseFrameSent + sent;
        client.LastActivity = TransportTime.ReadClock();
        if (client.ResponseFrameSent == client.ResponseFrame.Length)
        {
            client.ResponseMessageIndex = client.ResponseMessageIndex + 1;
            if (client.ResponseMessageIndex <
                client.ResponseMessages.Count)
            {
                ActivateResponse(client);
            }
            else
            {
                client.ResponseFrame = [];
                client.ResponseFrameSent = 0;
                client.ResponseMessages.Clear();
                client.ResponseMessageIndex = 0;
            }
        }
    }

    static void ResetFrame(DnsTcpServerClient client)
    {
        client.LengthPrefix =
            new byte[DnsProtocolLimits.TcpLengthPrefixLength];
        client.LengthPrefixReceived = 0;
        client.MessageBytes = [];
        client.MessageBytesReceived = 0;
        client.CurrentRequestCounted = false;
    }

    static void CountRequest(
        DnsTcpServerClient client,
        DnsServerRunResult result)
    {
        if (!client.CurrentRequestCounted)
        {
            result.RequestCount = result.RequestCount + 1;
            result.TcpRequestCount = result.TcpRequestCount + 1;
            client.RequestCount = client.RequestCount + 1;
            client.CurrentRequestCounted = true;
        }
    }

    static void CloseClient(DnsTcpServerClient client)
    {
        Socket? socket = client.Socket;
        if (socket != null)
        {
            socket.Dispose();
        }

        client.Socket = null;
        client.Closed = true;
    }

    static void DisposeListeners(List<DnsTcpServerListener> listeners)
    {
        int index = 0;
        while (index < listeners.Count)
        {
            Socket? socket = listeners[index].Socket;
            if (socket != null)
            {
                socket.Dispose();
            }

            index = index + 1;
        }
    }

    static void DisposeClients(List<DnsTcpServerClient> clients)
    {
        int index = 0;
        while (index < clients.Count)
        {
            CloseClient(clients[index]);
            index = index + 1;
        }
    }

    static bool IsStopRequested(
        DnsServer server,
        DnsServerOptions options)
    {
        return options.StopRequested || server.ReadStopRequested();
    }

    static void CompleteCancellation(DnsServerRunResult result)
    {
        result.Status = DnsServerStatus.Cancelled;
        result.Cancelled = true;
        result.CompletedAt = TransportTime.ReadClock();
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void RecordFailure(
        DnsServerRunResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        result.FailedRequestCount = result.FailedRequestCount + 1;
        AddDiagnostic(result, code, field, reason);
    }

    static void AddDiagnostic(
        DnsServerRunResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }
}
