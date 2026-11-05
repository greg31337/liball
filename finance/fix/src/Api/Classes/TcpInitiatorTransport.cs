using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace fix;

/// <summary>Provides reconnectable synchronous TCP client transport with ordered endpoint failover.</summary>
public class TcpInitiatorTransport : ISessionTransport
{
    internal List<SessionEndpoint> ConfiguredEndpoints = [];
    internal int ConnectTimeoutMilliseconds;
    internal Socket? CurrentSocket;
    internal TransportState CurrentState = TransportState.Disconnected;
    internal string LastOpenErrorCode = "";
    internal string LastOpenErrorMessage = "";

    /// <summary>Creates a TCP initiator from session endpoints and connection timeout settings.</summary>
    /// <param name="settings">The session transport settings.</param>
    /// <exception cref="FixTransportException">Thrown when settings are missing or the connection timeout is not positive.</exception>
    public TcpInitiatorTransport(SessionSettings settings)
    {
        if (settings == null)
        {
            throw new FixTransportException("The TCP initiator settings are missing.");
        }
        if (settings.ConnectTimeoutMilliseconds <= 0)
        {
            throw new FixTransportException("The TCP connection timeout must be positive.");
        }
        ConnectTimeoutMilliseconds = settings.ConnectTimeoutMilliseconds;
        for (int index = 0; index < settings.Endpoints.Count; index++)
        {
            ConfiguredEndpoints.Add(CloneEndpoint(settings.Endpoints[index]));
        }
    }

    /// <summary>Connects to the first reachable endpoint in stable priority order.</summary>
    /// <param name="endpoint">The fallback endpoint used when no endpoints were configured.</param>
    /// <param name="tls">The TLS settings reserved for a wrapping TLS transport.</param>
    /// <returns>The connection state and a stable failure code when no endpoint is reachable.</returns>
    public TransportWriteResult OpenTransport(SessionEndpoint endpoint, SessionTlsSettings tls)
    {
        if (CurrentState == TransportState.Connected && CurrentSocket != null && CurrentSocket.Connected)
        {
            return CreateWriteSuccess(0);
        }
        ReleaseCurrentSocket();
        CurrentState = TransportState.Connecting;
        LastOpenErrorCode = "";
        LastOpenErrorMessage = "";
        List<SessionEndpoint> endpoints = CreateOrderedEndpoints(endpoint);
        if (endpoints.Count == 0)
        {
            CurrentState = TransportState.Faulted;
            return CreateWriteFailure(CurrentState, "missing-tcp-endpoint", "No TCP initiator endpoint was configured.");
        }
        for (int index = 0; index < endpoints.Count; index++)
        {
            SessionEndpoint candidate = endpoints[index];
            if (!IsValidEndpoint(candidate))
            {
                LastOpenErrorCode = "invalid-tcp-endpoint";
                LastOpenErrorMessage = "A TCP initiator endpoint has an invalid host or port.";
                continue;
            }
            Socket? connected = ConnectEndpoint(candidate);
            if (connected != null)
            {
                CurrentSocket = connected;
                CurrentState = TransportState.Connected;
                return CreateWriteSuccess(0);
            }
        }
        CurrentState = TransportState.Faulted;
        if (LastOpenErrorCode.Length == 0)
        {
            LastOpenErrorCode = "tcp-connect-failed";
            LastOpenErrorMessage = "No TCP initiator endpoint could be connected.";
        }
        return CreateWriteFailure(CurrentState, LastOpenErrorCode, LastOpenErrorMessage);
    }

    /// <summary>Receives one available synchronous chunk from the connected socket.</summary>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The received byte count, EOF indication, and resulting state.</returns>
    public TransportReadResult ReadTransport(Span<byte> destination)
    {
        Socket? socket = CurrentSocket;
        if (CurrentState != TransportState.Connected || socket == null)
        {
            if (CurrentState == TransportState.Closed) return CreateReadFailure(CurrentState, "transport-closed", "The TCP initiator is closed.");
            if (CurrentState == TransportState.Faulted) return CreateReadFailure(CurrentState, "transport-faulted", "The TCP initiator is faulted.");
            return CreateReadFailure(CurrentState, "transport-not-connected", "The TCP initiator is not connected.");
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

    /// <summary>Sends one synchronous socket chunk and reports its partial consumed count.</summary>
    /// <param name="source">The source bytes.</param>
    /// <returns>The consumed byte count and resulting state.</returns>
    public TransportWriteResult WriteTransport(ReadOnlySpan<byte> source)
    {
        Socket? socket = CurrentSocket;
        if (CurrentState != TransportState.Connected || socket == null)
        {
            if (CurrentState == TransportState.Closed) return CreateWriteFailure(CurrentState, "transport-closed", "The TCP initiator is closed.");
            if (CurrentState == TransportState.Faulted) return CreateWriteFailure(CurrentState, "transport-faulted", "The TCP initiator is faulted.");
            return CreateWriteFailure(CurrentState, "transport-not-connected", "The TCP initiator is not connected.");
        }
        int written = 0;
        if (source.Length != 0)
        {
            written = socket.Send(source, SocketFlags.None);
        }
        return CreateWriteSuccess(written);
    }

    /// <summary>Closes the current socket while retaining configuration for a later reconnect.</summary>
    /// <returns>The closed transport outcome.</returns>
    public TransportWriteResult CloseTransport()
    {
        ReleaseCurrentSocket();
        CurrentState = TransportState.Closed;
        TransportWriteResult result = new TransportWriteResult();
        result.State = TransportState.Closed;
        return result;
    }

    internal Socket? ConnectEndpoint(SessionEndpoint endpoint)
    {
        long deadline = Environment.TickCount64 + ConnectTimeoutMilliseconds;
        Task<IPAddress[]> resolution = Dns.GetHostAddressesAsync(endpoint.Host);
        WaitForTask(resolution, deadline);
        if (!resolution.IsCompleted)
        {
            LastOpenErrorCode = "tcp-connect-timeout";
            LastOpenErrorMessage = "TCP endpoint name resolution exceeded the connection timeout.";
            return null;
        }
        if (!resolution.IsCompletedSuccessfully)
        {
            LastOpenErrorCode = "tcp-name-resolution-failed";
            LastOpenErrorMessage = "The TCP endpoint host could not be resolved.";
            return null;
        }
        IPAddress[] addresses = resolution.Result;
        for (int index = 0; index < addresses.Length; index++)
        {
            if (Environment.TickCount64 >= deadline)
            {
                LastOpenErrorCode = "tcp-connect-timeout";
                LastOpenErrorMessage = "The TCP endpoint connection attempt timed out.";
                return null;
            }
            Socket socket = new Socket(addresses[index].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ConfigureSocket(socket);
            Task connection = socket.ConnectAsync(new IPEndPoint(addresses[index], endpoint.Port), CancellationToken.None).AsTask();
            WaitForTask(connection, deadline);
            if (!connection.IsCompleted)
            {
                socket.Close();
                LastOpenErrorCode = "tcp-connect-timeout";
                LastOpenErrorMessage = "The TCP endpoint connection attempt timed out.";
                return null;
            }
            if (connection.IsCompletedSuccessfully && socket.Connected)
            {
                return socket;
            }
            socket.Close();
            LastOpenErrorCode = "tcp-connect-failed";
            LastOpenErrorMessage = "The TCP endpoint refused or failed the connection attempt.";
        }
        if (addresses.Length == 0)
        {
            LastOpenErrorCode = "tcp-name-resolution-failed";
            LastOpenErrorMessage = "The TCP endpoint host did not resolve to an address.";
        }
        return null;
    }

    internal static void WaitForTask(Task task, long deadline)
    {
        while (!task.IsCompleted && Environment.TickCount64 < deadline)
        {
            Thread.Sleep(1);
        }
    }

    internal static void ConfigureSocket(Socket socket)
    {
        socket.NoDelay = true;
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    internal List<SessionEndpoint> CreateOrderedEndpoints(SessionEndpoint fallback)
    {
        List<SessionEndpoint> endpoints = [];
        for (int index = 0; index < ConfiguredEndpoints.Count; index++)
        {
            endpoints.Add(CloneEndpoint(ConfiguredEndpoints[index]));
        }
        if (endpoints.Count == 0 && fallback != null)
        {
            endpoints.Add(CloneEndpoint(fallback));
        }
        for (int index = 1; index < endpoints.Count; index++)
        {
            SessionEndpoint selected = endpoints[index];
            int position = index;
            while (position > 0 && endpoints[position - 1].Priority > selected.Priority)
            {
                endpoints[position] = endpoints[position - 1];
                position--;
            }
            endpoints[position] = selected;
        }
        return endpoints;
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

    internal static bool IsValidEndpoint(SessionEndpoint endpoint)
    {
        return endpoint != null && endpoint.Host != null && endpoint.Host.Length != 0 && endpoint.Port > 0 && endpoint.Port <= 65535;
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
