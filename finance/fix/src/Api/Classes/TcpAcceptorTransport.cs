using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace fix;

/// <summary>Provides a bounded single-connection TCP listening transport for an acceptor session.</summary>
public class TcpAcceptorTransport : ISessionTransport
{
    internal int ConnectTimeoutMilliseconds;
    internal Socket? ListenerSocket;
    internal Socket? CurrentSocket;
    internal TransportState CurrentState = TransportState.Disconnected;
    internal bool HasAcceptedConnection;
    internal SessionEndpoint BoundEndpoint = new SessionEndpoint();
    internal SessionEndpoint PeerEndpoint = new SessionEndpoint();

    /// <summary>Creates a TCP acceptor using the configured connection timeout.</summary>
    /// <param name="settings">The session transport settings.</param>
    /// <exception cref="FixTransportException">Thrown when settings are missing or the connection timeout is not positive.</exception>
    public TcpAcceptorTransport(SessionSettings settings)
    {
        if (settings == null)
        {
            throw new FixTransportException("The TCP acceptor settings are missing.");
        }
        if (settings.ConnectTimeoutMilliseconds <= 0)
        {
            throw new FixTransportException("The TCP accept timeout must be positive.");
        }
        ConnectTimeoutMilliseconds = settings.ConnectTimeoutMilliseconds;
    }

    /// <summary>Binds, listens, and accepts one connection within the configured timeout.</summary>
    /// <param name="endpoint">The local endpoint, updated with the address and port selected by the socket.</param>
    /// <param name="tls">The TLS settings reserved for a wrapping TLS transport.</param>
    /// <returns>The connected state or a stable bind, resolution, timeout, or lifecycle failure.</returns>
    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        if (HasAcceptedConnection)
        {
            if (CurrentState == TransportState.Connected && CurrentSocket != null && CurrentSocket.Connected)
            {
                return CreateWriteSuccess(0);
            }
            return CreateWriteFailure(CurrentState, "acceptor-connection-consumed", "This TCP acceptor instance has already accepted its connection.");
        }
        ReleaseListenerSocket();
        ReleaseCurrentSocket();
        if (!IsValidBindEndpoint(endpoint))
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "invalid-tcp-bind-endpoint", "The TCP acceptor endpoint has an invalid host or port.");
        }
        long deadline = Environment.TickCount64 + ConnectTimeoutMilliseconds;
        Task<IPAddress[]> resolution = Dns.GetHostAddressesAsync(endpoint.Host);
        WaitForTask(resolution, deadline);
        if (!resolution.IsCompleted)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tcp-accept-timeout", "TCP endpoint name resolution exceeded the accept timeout.");
        }
        if (!resolution.IsCompletedSuccessfully || resolution.Result.Length == 0)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tcp-bind-name-resolution-failed", "The TCP bind host could not be resolved.");
        }
        IPAddress address = resolution.Result[0];
        Socket listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.ExclusiveAddressUse = true;
        listener.Bind(new IPEndPoint(address, endpoint.Port));
        listener.Listen(1);
        ListenerSocket = listener;
        CaptureBoundEndpoint(endpoint, listener);
        CurrentState = TransportState.Listening;
        Task<Socket> accept = listener.AcceptAsync(CancellationToken.None).AsTask();
        WaitForTask(accept, deadline);
        if (!accept.IsCompleted)
        {
            ReleaseListenerSocket();
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tcp-accept-timeout", "No TCP peer connected before the accept timeout.");
        }
        if (!accept.IsCompletedSuccessfully)
        {
            ReleaseListenerSocket();
            if (CurrentState == TransportState.Closed)
            {
                return CreateWriteFailure(CurrentState, "transport-closed", "The TCP acceptor was closed while listening.");
            }
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "tcp-accept-failed", "The TCP listener failed to accept a connection.");
        }
        Socket connected = accept.Result;
        ReleaseListenerSocket();
        TcpInitiatorTransport.ConfigureSocket(connected);
        CurrentSocket = connected;
        CapturePeerEndpoint(endpoint, connected);
        HasAcceptedConnection = true;
        CurrentState = TransportState.Connected;
        return CreateWriteSuccess(0);
    }

    /// <summary>Receives one available synchronous chunk from the accepted connection.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The received byte count, EOF indication, and resulting state.</returns>
    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        Socket? socket = CurrentSocket;
        if (CurrentState != TransportState.Connected || socket == null)
        {
            if (CurrentState == TransportState.Closed) return CreateReadFailure(CurrentState, "transport-closed", "The TCP acceptor is closed.");
            if (CurrentState == TransportState.Faulted) return CreateReadFailure(CurrentState, "transport-faulted", "The TCP acceptor is faulted.");
            return CreateReadFailure(CurrentState, "transport-not-connected", "The TCP acceptor has no connected peer.");
        }
        TransportReadResult result = new TransportReadResult();
        result.State = TransportState.Connected;
        if (destination.Length == 0)
        {
            return result;
        }
        result.BytesRead = socket.Receive(destination, SocketFlags.None);
        if (result.BytesRead == 0)
        {
            ReleaseCurrentSocket();
            CurrentState = TransportState.Closed;
            result.State = TransportState.Closed;
            result.EndOfStream = true;
        }
        return result;
    }

    /// <summary>Sends one synchronous socket chunk to the accepted connection.</summary>
    /// <param name="source">The source bytes.</param>
    /// <returns>The partial consumed byte count and resulting state.</returns>
    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        Socket? socket = CurrentSocket;
        if (CurrentState != TransportState.Connected || socket == null)
        {
            if (CurrentState == TransportState.Closed) return CreateWriteFailure(CurrentState, "transport-closed", "The TCP acceptor is closed.");
            if (CurrentState == TransportState.Faulted) return CreateWriteFailure(CurrentState, "transport-faulted", "The TCP acceptor is faulted.");
            return CreateWriteFailure(CurrentState, "transport-not-connected", "The TCP acceptor has no connected peer.");
        }
        int written = 0;
        if (source.Length != 0)
        {
            written = socket.Send(source, SocketFlags.None);
        }
        return CreateWriteSuccess(written);
    }

    /// <summary>Closes the listener and accepted socket without permitting a second accepted connection.</summary>
    /// <returns>The closed transport outcome.</returns>
    public TransportWriteResult CloseTransport()
    {
        CurrentState = TransportState.Closed;
        ReleaseListenerSocket();
        ReleaseCurrentSocket();
        TransportWriteResult result = new TransportWriteResult();
        result.State = TransportState.Closed;
        return result;
    }

    internal static bool IsValidBindEndpoint(SessionEndpoint endpoint)
    {
        return endpoint != null && endpoint.Host != null && endpoint.Host.Length != 0 && endpoint.Port >= 0 && endpoint.Port <= 65535;
    }

    internal void CaptureBoundEndpoint(SessionEndpoint endpoint, Socket listener)
    {
        IPEndPoint fallback = new IPEndPoint(IPAddress.None, endpoint.Port);
        IPEndPoint bound = (IPEndPoint)(listener.LocalEndPoint ?? fallback);
        endpoint.Host = bound.Address.ToString();
        endpoint.Port = bound.Port;
        BoundEndpoint = CloneEndpoint(endpoint);
    }

    internal void CapturePeerEndpoint(SessionEndpoint local, Socket connected)
    {
        PeerEndpoint = new SessionEndpoint();
        PeerEndpoint.UseTls = local.UseTls;
        EndPoint? remote = connected.RemoteEndPoint;
        if (remote != null)
        {
            IPEndPoint peer = (IPEndPoint)remote;
            PeerEndpoint.Host = peer.Address.ToString();
            PeerEndpoint.Port = peer.Port;
        }
    }

    internal static SessionEndpoint CloneEndpoint(SessionEndpoint source)
    {
        SessionEndpoint copy = new SessionEndpoint();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Priority = source.Priority;
        copy.UseTls = source.UseTls;
        return copy;
    }

    internal static void WaitForTask(Task task, long deadline)
    {
        while (!task.IsCompleted && Environment.TickCount64 < deadline)
        {
            Thread.Sleep(1);
        }
    }

    internal void ReleaseListenerSocket()
    {
        Socket? listener = ListenerSocket;
        ListenerSocket = null;
        if (listener != null)
        {
            listener.Close();
        }
    }

    internal void ReleaseCurrentSocket()
    {
        Socket? socket = CurrentSocket;
        CurrentSocket = null;
        if (socket != null)
        {
            socket.Close();
        }
    }

    internal TransportWriteResult CreateWriteSuccess(int bytesWritten)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = CurrentState;
        result.BytesWritten = bytesWritten;
        return result;
    }

    internal static TransportWriteResult CreateWriteFailure(TransportState state, string code, string message)
    {
        TransportWriteResult result = new TransportWriteResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static TransportReadResult CreateReadFailure(TransportState state, string code, string message)
    {
        TransportReadResult result = new TransportReadResult();
        result.State = state;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
