using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class SctpSocketDiameterListener : IDiameterListener
{
    internal List<Socket> ListeningSockets;
    internal List<Thread> Workers = new List<Thread>();
    internal Queue<SctpSocketDiameterConnection> AcceptedConnections =
        new Queue<SctpSocketDiameterConnection>();
    internal object StateLock = new object();
    internal DiameterListenerMetadata Metadata;
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal DiameterTransportIoResult? TerminalAcceptResult;
    internal int QueueCapacity;
    internal int ReceiveBufferSize;
    internal int SendBufferSize;
    internal int StreamCount;
    internal bool NoDelay;
    internal DiameterSecurityMode SecurityMode;
    internal int StoppedWorkerCount;
    internal bool Closing;
    internal bool Closed;

    internal SctpSocketDiameterListener(
        List<Socket> sockets,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterListenerMetadata metadata)
    {
        if (sockets == null || sockets.Count == 0)
        {
            throw new ArgumentException("At least one bound SCTP listener socket is required.");
        }

        ListeningSockets = sockets;
        QueueCapacity = transport.ListenBacklog;
        ReceiveBufferSize = transport.ReceiveBufferSize;
        SendBufferSize = transport.SendBufferSize;
        StreamCount = transport.SctpStreamCount;
        NoDelay = transport.NoDelay;
        SecurityMode = security.Mode;
        Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
        int index = 0;
        while (index < ListeningSockets.Count)
        {
            Socket socket = ListeningSockets[index];
            Thread worker = new Thread(() => RunSctpAcceptWorker(socket));
            worker.IsBackground = true;
            worker.Name = "Diameter SCTP accept worker";
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

    /// <summary>Waits for and transfers ownership of one accepted SCTP connection.</summary>
    /// <param name="timeoutMilliseconds">The positive accept timeout.</param>
    /// <returns>The accepted connection, timeout, closure, or failure outcome.</returns>
    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return CreateSctpAcceptFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive SCTP accept timeout is required.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (StateLock)
        {
            while (AcceptedConnections.Count == 0 && !Closing && !Closed &&
                TerminalAcceptResult == null)
            {
                int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
                if (remaining <= 0)
                {
                    return CreateSctpAcceptFailure(
                        DiameterTransportStatus.TimedOut,
                        "accept-timeout",
                        "No SCTP connection arrived before the accept timeout.");
                }

                Monitor.Wait(StateLock, remaining);
            }

            if (AcceptedConnections.Count > 0)
            {
                SctpSocketDiameterConnection connection = AcceptedConnections.Dequeue();
                Monitor.PulseAll(StateLock);
                DiameterConnectionResult result = new DiameterConnectionResult();
                result.Connection = connection;
                result.Metadata = connection.ReadDiameterConnectionMetadata();
                return result;
            }

            if (TerminalAcceptResult != null)
            {
                return CreateSctpAcceptFailure(
                    TerminalAcceptResult.Status,
                    TerminalAcceptResult.Code,
                    TerminalAcceptResult.Message);
            }

            return CreateSctpAcceptFailure(
                DiameterTransportStatus.Closed,
                "listener-closed",
                "The SCTP listener is closed.");
        }
    }

    /// <summary>Reads an independent SCTP listener metadata snapshot.</summary>
    /// <returns>The current listener metadata.</returns>
    public DiameterListenerMetadata ReadDiameterListenerMetadata()
    {
        lock (StateLock)
        {
            DiameterListenerMetadata result =
                TcpSocketDiameterListener.CloneListenerMetadata(Metadata);
            result.Open = !Closing && !Closed;
            return result;
        }
    }

    /// <summary>Closes all listener sockets, workers, and unclaimed SCTP connections.</summary>
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

    internal void RunSctpAcceptWorker(Socket listeningSocket)
    {
        while (true)
        {
            lock (StateLock)
            {
                if (Closing)
                {
                    CompleteSctpAcceptWorker();
                    return;
                }
            }

            Task<Socket> acceptTask = listeningSocket.AcceptAsync(
                CancellationToken.None).AsTask();
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
                CompleteSctpAcceptWorker();
                return;
            }

            if (!acceptTask.IsCompletedSuccessfully)
            {
                lock (StateLock)
                {
                    if (Closing)
                    {
                        CompleteSctpAcceptWorker();
                        return;
                    }
                }

                CompleteFailedSctpAcceptWorker(
                    "accept-failed",
                    SctpSocketDiameterConnection.ReadStaticSctpTaskFailure(
                        acceptTask,
                        "The SCTP accept operation failed."));
                return;
            }

            Socket accepted = acceptTask.Result;
            ConfigureAcceptedSctpSocket(accepted);
            DiameterConnectionMetadata metadata = CreateAcceptedSctpMetadata(accepted);
            SctpSocketDiameterConnection connection =
                new SctpSocketDiameterConnection(accepted, metadata, StreamCount);
            if (!EnqueueAcceptedSctpConnection(connection))
            {
                connection.CloseDiameterConnection();
                CompleteSctpAcceptWorker();
                return;
            }
        }
    }

    internal bool EnqueueAcceptedSctpConnection(
        SctpSocketDiameterConnection connection)
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

    internal void ConfigureAcceptedSctpSocket(Socket socket)
    {
        socket.ReceiveBufferSize = ReceiveBufferSize;
        socket.SendBufferSize = SendBufferSize;
        SctpSocketSupport.ConfigureSctpNoDelay(socket, NoDelay);
        SctpSocketSupport.ConfigureSctpDefaultSend(
            socket,
            0,
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier);
    }

    internal DiameterConnectionMetadata CreateAcceptedSctpMetadata(Socket socket)
    {
        EndPoint? localValue = socket.LocalEndPoint;
        EndPoint? remoteValue = socket.RemoteEndPoint;
        if (localValue == null || remoteValue == null)
        {
            throw new DiameterTransportException(
                "An accepted SCTP socket requires local and remote endpoints.");
        }

        IPEndPoint local = (IPEndPoint)localValue;
        IPEndPoint remote = (IPEndPoint)remoteValue;
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = $"{local.Address}:{local.Port}-{remote.Address}:{remote.Port}";
        metadata.LocalEndpoint.Host = local.Address.ToString();
        metadata.LocalEndpoint.Address = local.Address;
        metadata.LocalEndpoint.Port = local.Port;
        metadata.LocalEndpoint.Transport = DiameterTransportKind.Sctp;
        metadata.LocalEndpoint.Security = SecurityMode;
        metadata.RemoteEndpoint.Host = remote.Address.ToString();
        metadata.RemoteEndpoint.Address = remote.Address;
        metadata.RemoteEndpoint.Port = remote.Port;
        metadata.RemoteEndpoint.Transport = DiameterTransportKind.Sctp;
        metadata.RemoteEndpoint.Security = SecurityMode;
        metadata.Direction = DiameterConnectionDirection.Inbound;
        PopulateSctpTransportMetadata(metadata.Transport);
        PopulateSctpSecurityMetadata(metadata.Security);
        metadata.EstablishedAt = DateTimeOffset.UtcNow;
        metadata.Open = true;
        return metadata;
    }

    internal void PopulateSctpTransportMetadata(DiameterTransportMetadata metadata)
    {
        metadata.Kind = DiameterTransportKind.Sctp;
        metadata.SupportsActiveConnections = true;
        metadata.SupportsListeners = true;
        metadata.SupportsIpv4 = Metadata.Transport.SupportsIpv4;
        metadata.SupportsIpv6 = Metadata.Transport.SupportsIpv6;
        metadata.MessageOriented = true;
        metadata.MaximumInboundStreams = StreamCount;
        metadata.MaximumOutboundStreams = StreamCount;
        metadata.PayloadProtocolIdentifier =
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
    }

    internal void PopulateSctpSecurityMetadata(DiameterSecurityMetadata metadata)
    {
        metadata.Mode = SecurityMode;
        if (SecurityMode == DiameterSecurityMode.Ipsec)
        {
            metadata.Authenticated = true;
            metadata.Encrypted = true;
            metadata.Protocol = "IPsec";
        }
    }

    internal void CompleteFailedSctpAcceptWorker(string code, string message)
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

    internal void CompleteSctpAcceptWorker()
    {
        lock (StateLock)
        {
            StoppedWorkerCount = StoppedWorkerCount + 1;
            Monitor.PulseAll(StateLock);
        }
    }

    internal DiameterConnectionResult CreateSctpAcceptFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Sctp;
        result.Metadata.Direction = DiameterConnectionDirection.Inbound;
        return result;
    }
}
