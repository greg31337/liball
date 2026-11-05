using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class TcpSocketDiameterListener : IDiameterListener
{
    internal List<Socket> ListeningSockets;
    internal List<Thread> Workers = new List<Thread>();
    internal Queue<TcpSocketDiameterConnection> AcceptedConnections =
        new Queue<TcpSocketDiameterConnection>();
    internal object StateLock = new object();
    internal DiameterListenerMetadata Metadata;
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal DiameterTransportIoResult? TerminalAcceptResult;
    internal int QueueCapacity;
    internal int ReceiveBufferSize;
    internal int SendBufferSize;
    internal bool NoDelay;
    internal bool KeepAlive;
    internal DiameterSecurityMode SecurityMode;
    internal int StoppedWorkerCount;
    internal int AcceptWaiterCount;
    internal int TransientFailureCount;
    internal bool Closing;
    internal bool Closed;

    internal TcpSocketDiameterListener(
        List<Socket> sockets,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterListenerMetadata metadata)
    {
        if (sockets == null || sockets.Count == 0)
        {
            throw new ArgumentException("At least one bound TCP listener socket is required.");
        }

        ListeningSockets = sockets;
        QueueCapacity = transport.ListenBacklog;
        ReceiveBufferSize = transport.ReceiveBufferSize;
        SendBufferSize = transport.SendBufferSize;
        NoDelay = transport.NoDelay;
        KeepAlive = transport.KeepAlive;
        SecurityMode = security.Mode;
        Metadata = CloneListenerMetadata(metadata);
        int index = 0;
        while (index < ListeningSockets.Count)
        {
            Socket socket = ListeningSockets[index];
            Thread worker = new Thread(() => RunAcceptWorker(socket));
            worker.IsBackground = true;
            worker.Name = "Diameter TCP accept worker";
            Workers.Add(worker);
            index = index + 1;
        }

        index = 0;
        while (index < Workers.Count)
        {
            Workers[index].Start();
            index = index + 1;
        }
    }

    /// <summary>Waits for and transfers ownership of one accepted TCP connection.</summary>
    /// <param name="timeoutMilliseconds">The positive accept timeout.</param>
    /// <returns>The accepted connection, timeout, closure, or failure outcome.</returns>
    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return CreateAcceptFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive TCP accept timeout is required.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return CreateAcceptFailure(
                    DiameterTransportStatus.Closed,
                    "listener-closed",
                    "The TCP listener is closed.");
            }

            AcceptWaiterCount = AcceptWaiterCount + 1;
            while (AcceptedConnections.Count == 0 && !Closing && !Closed &&
                TerminalAcceptResult == null)
            {
                int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
                if (remaining <= 0)
                {
                    AcceptWaiterCount = AcceptWaiterCount - 1;
                    return CreateAcceptFailure(
                        DiameterTransportStatus.TimedOut,
                        "accept-timeout",
                        "No TCP connection arrived before the accept timeout.");
                }

                Monitor.Wait(StateLock, remaining);
            }

            AcceptWaiterCount = AcceptWaiterCount - 1;
            if (Closing || Closed)
            {
                return CreateAcceptFailure(
                    DiameterTransportStatus.Closed,
                    "listener-closed",
                    "The TCP listener is closed.");
            }

            if (AcceptedConnections.Count > 0)
            {
                TcpSocketDiameterConnection connection = AcceptedConnections.Dequeue();
                Monitor.PulseAll(StateLock);
                DiameterConnectionResult result = new DiameterConnectionResult();
                result.Connection = connection;
                result.Metadata = connection.ReadDiameterConnectionMetadata();
                return result;
            }

            if (TerminalAcceptResult != null)
            {
                return CreateAcceptFailure(
                    TerminalAcceptResult.Status,
                    TerminalAcceptResult.Code,
                    TerminalAcceptResult.Message);
            }

            return CreateAcceptFailure(
                DiameterTransportStatus.Failed,
                "accept-failed",
                "The TCP accept operation failed.");
        }
    }

    /// <summary>Reads an independent TCP listener metadata snapshot.</summary>
    /// <returns>The listener metadata.</returns>
    public DiameterListenerMetadata ReadDiameterListenerMetadata()
    {
        lock (StateLock)
        {
            DiameterListenerMetadata result = CloneListenerMetadata(Metadata);
            result.Open = !Closing && !Closed;
            return result;
        }
    }

    /// <summary>Closes all listener sockets, workers, and unclaimed connections.</summary>
    /// <returns>The idempotent close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterListener()
    {
        bool ownsClose = false;
        lock (StateLock)
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                ownsClose = true;
                int socketIndex = 0;
                while (socketIndex < ListeningSockets.Count)
                {
                    ListeningSockets[socketIndex].Close();
                    socketIndex = socketIndex + 1;
                }

                while (AcceptedConnections.Count > 0)
                {
                    AcceptedConnections.Dequeue().CloseDiameterConnection();
                }

                Monitor.PulseAll(StateLock);
            }

            if (!ownsClose)
            {
                while (!Closed)
                {
                    Monitor.Wait(StateLock);
                }

                return QueuedDiameterConnection.CloneOperationResult(CloseResult);
            }
        }

        int workerIndex = 0;
        while (workerIndex < Workers.Count)
        {
            Workers[workerIndex].Join();
            workerIndex = workerIndex + 1;
        }

        lock (StateLock)
        {
            Closed = true;
            Metadata.Open = false;
            Monitor.PulseAll(StateLock);
            return QueuedDiameterConnection.CloneOperationResult(CloseResult);
        }
    }

    internal void RunAcceptWorker(Socket listeningSocket)
    {
        while (true)
        {
            Task<Socket>? acceptTask = null;
            lock (StateLock)
            {
                if (Closing)
                {
                    CompleteAcceptWorker();
                    return;
                }

                acceptTask = listeningSocket.AcceptAsync(CancellationToken.None).AsTask();
            }

            bool stop = false;
            while (!acceptTask.IsCompleted && !stop)
            {
                SpinWait.SpinUntil(() => acceptTask.IsCompleted, 100);
                lock (StateLock)
                {
                    stop = Closing;
                }
            }

            if (stop && !acceptTask.IsCompleted)
            {
                CompleteAcceptWorker();
                return;
            }

            if (!acceptTask.IsCompletedSuccessfully)
            {
                bool closing;
                lock (StateLock)
                {
                    closing = Closing;
                }

                if (closing)
                {
                    CompleteAcceptWorker();
                    return;
                }

                AggregateException? aggregate = acceptTask.Exception;
                if (aggregate == null)
                {
                    CompleteFailedAcceptWorker(
                        "accept-failed",
                        "The TCP accept operation failed.");
                    return;
                }

                Exception failure = aggregate.GetBaseException();
                SocketException socketFailure = (SocketException)failure;
                if (IsTransientTcpAcceptError(socketFailure.SocketErrorCode))
                {
                    lock (StateLock)
                    {
                        TransientFailureCount = TransientFailureCount + 1;
                    }

                    Thread.Sleep(10);
                    continue;
                }

                CompleteFailedAcceptWorker("accept-failed", failure.Message);
                return;
            }

            Socket accepted = acceptTask.Result;
            ConfigureAcceptedSocket(accepted);
            DiameterConnectionMetadata metadata = CreateAcceptedConnectionMetadata(accepted);
            TcpSocketDiameterConnection connection =
                new TcpSocketDiameterConnection(accepted, metadata);
            if (!EnqueueAcceptedConnection(connection))
            {
                connection.CloseDiameterConnection();
                CompleteAcceptWorker();
                return;
            }
        }
    }

    internal bool EnqueueAcceptedConnection(TcpSocketDiameterConnection connection)
    {
        lock (StateLock)
        {
            while (AcceptedConnections.Count >= QueueCapacity && !Closing)
            {
                Monitor.Wait(StateLock);
            }

            if (Closing)
            {
                return false;
            }

            AcceptedConnections.Enqueue(connection);
            Monitor.PulseAll(StateLock);
            return true;
        }
    }

    internal void ConfigureAcceptedSocket(Socket socket)
    {
        socket.NoDelay = NoDelay;
        socket.ReceiveBufferSize = ReceiveBufferSize;
        socket.SendBufferSize = SendBufferSize;
        socket.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            KeepAlive);
    }

    internal DiameterConnectionMetadata CreateAcceptedConnectionMetadata(Socket socket)
    {
        EndPoint? localValue = socket.LocalEndPoint;
        EndPoint? remoteValue = socket.RemoteEndPoint;
        if (localValue == null || remoteValue == null)
        {
            throw new DiameterTransportException(
                "An accepted TCP socket requires local and remote endpoints.");
        }

        IPEndPoint local = (IPEndPoint)localValue;
        IPEndPoint remote = (IPEndPoint)remoteValue;
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = $"{local.Address}:{local.Port}-{remote.Address}:{remote.Port}";
        metadata.LocalEndpoint.Host = local.Address.ToString();
        metadata.LocalEndpoint.Address = local.Address;
        metadata.LocalEndpoint.Port = local.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        metadata.LocalEndpoint.Security = SecurityMode;
        metadata.RemoteEndpoint.Host = remote.Address.ToString();
        metadata.RemoteEndpoint.Address = remote.Address;
        metadata.RemoteEndpoint.Port = remote.Port;
        metadata.RemoteEndpoint.Transport = DiameterTransportKind.Tcp;
        metadata.RemoteEndpoint.Security = SecurityMode;
        metadata.Direction = DiameterConnectionDirection.Inbound;
        metadata.Transport.Kind = DiameterTransportKind.Tcp;
        metadata.Transport.SupportsActiveConnections = true;
        metadata.Transport.SupportsListeners = true;
        metadata.Transport.SupportsIpv4 = Socket.OSSupportsIPv4;
        metadata.Transport.SupportsIpv6 = Socket.OSSupportsIPv6;
        metadata.Security.Mode = SecurityMode;
        if (SecurityMode == DiameterSecurityMode.Ipsec)
        {
            metadata.Security.Authenticated = true;
            metadata.Security.Encrypted = true;
            metadata.Security.Protocol = "IPsec";
        }

        metadata.EstablishedAt = DateTimeOffset.UtcNow;
        metadata.Open = true;
        return metadata;
    }

    internal void CompleteFailedAcceptWorker(string code, string message)
    {
        lock (StateLock)
        {
            StoppedWorkerCount = StoppedWorkerCount + 1;
            if (StoppedWorkerCount == Workers.Count)
            {
                TerminalAcceptResult = QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.Failed,
                    code,
                    message);
            }

            Monitor.PulseAll(StateLock);
        }
    }

    internal void CompleteAcceptWorker()
    {
        lock (StateLock)
        {
            StoppedWorkerCount = StoppedWorkerCount + 1;
            Monitor.PulseAll(StateLock);
        }
    }

    internal bool IsTransientTcpAcceptError(SocketError error)
    {
        return error == SocketError.Interrupted ||
            error == SocketError.WouldBlock ||
            error == SocketError.TryAgain ||
            error == SocketError.NoBufferSpaceAvailable ||
            error == SocketError.TimedOut ||
            error == SocketError.ConnectionAborted ||
            error == SocketError.NetworkDown ||
            error == SocketError.NetworkReset;
    }

    internal DiameterConnectionResult CreateAcceptFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        result.Metadata.Direction = DiameterConnectionDirection.Inbound;
        return result;
    }

    internal static DiameterListenerMetadata CloneListenerMetadata(
        DiameterListenerMetadata source)
    {
        DiameterListenerMetadata result = new DiameterListenerMetadata();
        result.LocalEndpoint = QueuedDiameterConnection.CloneEndpoint(source.LocalEndpoint);
        result.Transport = QueuedDiameterConnection.CloneTransportMetadata(source.Transport);
        result.SecurityMode = source.SecurityMode;
        result.Open = source.Open;
        return result;
    }
}
