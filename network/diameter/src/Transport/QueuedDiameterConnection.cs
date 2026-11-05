using System;
using System.Collections.Generic;
using System.Threading;

namespace diameter;

internal class QueuedDiameterConnection : IDiameterConnection
{
    internal IDiameterConnection OwnedConnection;
    internal int IngressCapacity;
    internal int EgressCapacity;
    internal int ReceiveBufferSize;
    internal int MaximumFrameSize;
    internal object StateLock = new object();
    internal Queue<ConnectionReadItem> IngressQueue = new Queue<ConnectionReadItem>();
    internal Queue<ConnectionWriteItem> EgressQueue = new Queue<ConnectionWriteItem>();
    internal Thread IngressWorker;
    internal Thread EgressWorker;
    internal DiameterConnectionMetadata Metadata;
    internal DiameterTransportIoResult? TerminalReceiveResult;
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closing;
    internal bool Closed;

    internal QueuedDiameterConnection(
        IDiameterConnection connection,
        int ingressCapacity,
        int egressCapacity,
        int receiveBufferSize,
        int maximumFrameSize)
    {
        if (connection == null)
        {
            throw new ArgumentException("A queued connection requires an owned connection.");
        }

        if (ingressCapacity <= 0 || egressCapacity <= 0 || receiveBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException("Connection queue and buffer sizes");
        }

        if (maximumFrameSize < DiameterProtocolConstants.MinimumMessageLength ||
            maximumFrameSize > DiameterProtocolConstants.MaximumMessageLength ||
            maximumFrameSize % DiameterProtocolConstants.Alignment != 0)
        {
            throw new ArgumentOutOfRangeException("maximumFrameSize");
        }

        OwnedConnection = connection;
        IngressCapacity = ingressCapacity;
        EgressCapacity = egressCapacity;
        ReceiveBufferSize = receiveBufferSize;
        MaximumFrameSize = maximumFrameSize;
        Metadata = CloneConnectionMetadata(connection.ReadDiameterConnectionMetadata());
        Metadata.Open = true;
        IngressWorker = new Thread(() => RunIngressWorker());
        EgressWorker = new Thread(() => RunEgressWorker());
        IngressWorker.IsBackground = true;
        EgressWorker.IsBackground = true;
        IngressWorker.Name = "Diameter ingress worker";
        EgressWorker.Name = "Diameter egress worker";
        IngressWorker.Start();
        EgressWorker.Start();
    }

    /// <summary>Sends an owned copy through the bounded egress queue.</summary>
    /// <param name="source">The bytes to send.</param>
    /// <param name="timeoutMilliseconds">The positive send timeout in milliseconds.</param>
    /// <returns>The completed send outcome.</returns>
    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive send timeout is required.");
        }

        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return CreateClosedIoResult();
            }

            if (source.Length == 0)
            {
                return new DiameterTransportIoResult();
            }
        }

        ConnectionWriteItem item = new ConnectionWriteItem();
        item.Data = new byte[source.Length];
        source.CopyTo(item.Data);
        item.Deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return CreateClosedIoResult();
            }

            if (EgressQueue.Count >= EgressCapacity)
            {
                return CreateIoFailure(
                    DiameterTransportStatus.QueueFull,
                    "egress-queue-full",
                    "The bounded connection egress queue is full.");
            }

            EgressQueue.Enqueue(item);
            Monitor.PulseAll(StateLock);
        }

        lock (item.Sync)
        {
            while (!item.Complete)
            {
                int remaining = ReadRemainingMilliseconds(item.Deadline);
                if (remaining <= 0)
                {
                    item.Cancelled = true;
                    return CreateIoFailure(
                        DiameterTransportStatus.TimedOut,
                        "send-timeout",
                        "The queued send did not complete before its timeout.");
                }

                Monitor.Wait(item.Sync, remaining);
            }

            return CloneIoResult(item.Result);
        }
    }

    /// <summary>Receives bytes from the bounded ingress queue.</summary>
    /// <param name="destination">The destination for received bytes.</param>
    /// <param name="timeoutMilliseconds">The positive receive timeout in milliseconds.</param>
    /// <returns>The receive outcome.</returns>
    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive receive timeout is required.");
        }

        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return CreateClosedIoResult();
            }

            if (destination.Length == 0)
            {
                return new DiameterTransportIoResult();
            }
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return CreateClosedIoResult();
            }

            while (IngressQueue.Count == 0 && !Closing && !Closed &&
                TerminalReceiveResult == null)
            {
                int remaining = ReadRemainingMilliseconds(deadline);
                if (remaining <= 0)
                {
                    return CreateIoFailure(
                        DiameterTransportStatus.TimedOut,
                        "receive-timeout",
                        "No received data became available before the timeout.");
                }

                Monitor.Wait(StateLock, remaining);
            }

            if (IngressQueue.Count > 0)
            {
                ConnectionReadItem item = IngressQueue.Peek();
                int available = item.Data.Length - item.Offset;
                int count = Math.Min(destination.Length, available);
                item.Data.AsSpan(item.Offset, count).CopyTo(destination);
                item.Offset = item.Offset + count;
                if (item.Offset == item.Data.Length)
                {
                    IngressQueue.Dequeue();
                    Monitor.PulseAll(StateLock);
                }

                DiameterTransportIoResult success = new DiameterTransportIoResult();
                success.TransferredCount = count;
                return success;
            }

            if (TerminalReceiveResult != null)
            {
                return CloneIoResult(TerminalReceiveResult);
            }

            return CreateClosedIoResult();
        }
    }

    /// <summary>Reads an independent queued connection metadata snapshot.</summary>
    /// <returns>The connection metadata.</returns>
    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        lock (StateLock)
        {
            DiameterConnectionMetadata result = CloneConnectionMetadata(Metadata);
            result.Open = !Closing && !Closed;
            return result;
        }
    }

    /// <summary>Closes the owned connection and both workers idempotently.</summary>
    /// <returns>The close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        bool ownsClose = false;
        lock (StateLock)
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                ownsClose = true;
                CompleteQueuedWritesAsClosed();
                IngressQueue.Clear();
                TerminalReceiveResult = null;
                Monitor.PulseAll(StateLock);
            }

            if (!ownsClose)
            {
                while (!Closed)
                {
                    Monitor.Wait(StateLock);
                }

                return CloneOperationResult(CloseResult);
            }
        }

        DiameterTransportOperationResult closeResult =
            OwnedConnection.CloseDiameterConnection();
        IngressWorker.Join();
        EgressWorker.Join();
        lock (StateLock)
        {
            CloseResult = CloneOperationResult(closeResult);
            Closed = true;
            Metadata.Open = false;
            Monitor.PulseAll(StateLock);
        }

        return CloneOperationResult(CloseResult);
    }

    internal void RunEgressWorker()
    {
        while (true)
        {
            ConnectionWriteItem? item = null;
            lock (StateLock)
            {
                while (EgressQueue.Count == 0 && !Closing)
                {
                    Monitor.Wait(StateLock);
                }

                if (Closing)
                {
                    return;
                }

                item = EgressQueue.Dequeue();
                Monitor.PulseAll(StateLock);
            }

            bool cancelled;
            lock (item.Sync)
            {
                cancelled = item.Cancelled;
                item.Started = !cancelled;
                if (cancelled)
                {
                    item.Complete = true;
                    Monitor.PulseAll(item.Sync);
                }
            }

            if (!cancelled)
            {
                DiameterTransportIoResult result = SendQueuedConnectionItem(item);

                lock (item.Sync)
                {
                    item.Result = CloneIoResult(result);
                    item.Complete = true;
                    Monitor.PulseAll(item.Sync);
                }
            }
        }
    }

    internal DiameterTransportIoResult SendQueuedConnectionItem(
        ConnectionWriteItem item)
    {
        int transferred = 0;
        while (transferred < item.Data.Length)
        {
            lock (StateLock)
            {
                if (Closing || Closed)
                {
                    return CreateClosedIoResult();
                }
            }

            lock (item.Sync)
            {
                if (item.Cancelled)
                {
                    return CreateIoFailure(
                        DiameterTransportStatus.TimedOut,
                        "send-timeout",
                        "The queued send did not complete before its timeout.");
                }
            }

            int remainingTime = ReadRemainingMilliseconds(item.Deadline);
            if (remainingTime <= 0)
            {
                return CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "send-timeout",
                    "The queued send did not complete before its timeout.");
            }

            int remainingBytes = item.Data.Length - transferred;
            DiameterTransportIoResult? progress =
                OwnedConnection.SendDiameterConnectionBytes(
                    item.Data.AsSpan(transferred, remainingBytes),
                    remainingTime);
            if (progress == null || progress.TransferredCount < 0 ||
                progress.TransferredCount > remainingBytes)
            {
                return CreateIoFailure(
                    DiameterTransportStatus.Failed,
                    "invalid-send-count",
                    "The owned connection returned an invalid send count.");
            }

            if (progress.Status != DiameterTransportStatus.Success)
            {
                DiameterTransportIoResult failure = CloneIoResult(progress);
                failure.TransferredCount = transferred + progress.TransferredCount;
                return failure;
            }

            if (progress.TransferredCount == 0)
            {
                return CreateIoFailure(
                    DiameterTransportStatus.Failed,
                    "send-no-progress",
                    "The owned connection reported a successful send without progress.");
            }

            transferred = transferred + progress.TransferredCount;
        }

        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.TransferredCount = transferred;
        return result;
    }

    internal void RunIngressWorker()
    {
        byte[] buffer = new byte[ReceiveBufferSize];
        byte[] retained = new byte[0];
        while (true)
        {
            lock (StateLock)
            {
                if (Closing)
                {
                    return;
                }
            }

            DiameterTransportIoResult result =
                OwnedConnection.ReceiveDiameterConnectionBytes(buffer, 100);
            if (result.Status == DiameterTransportStatus.TimedOut)
            {
                continue;
            }

            if (result.TransferredCount < 0 || result.TransferredCount > buffer.Length)
            {
                SetTerminalReceiveResult(CreateIoFailure(
                    DiameterTransportStatus.Failed,
                    "invalid-receive-count",
                    "The owned connection returned an invalid receive count."));
                return;
            }

            bool endOfStream = result.EndOfStream ||
                result.Status == DiameterTransportStatus.EndOfStream;
            if (result.Status == DiameterTransportStatus.Success &&
                result.TransferredCount == 0 && !endOfStream)
            {
                Thread.Yield();
                continue;
            }

            if (result.Status == DiameterTransportStatus.Success ||
                result.Status == DiameterTransportStatus.EndOfStream)
            {
                DiameterFramingResult framing = StreamFramer.FrameStreamData(
                    retained,
                    buffer.AsSpan(0, result.TransferredCount),
                    endOfStream,
                    MaximumFrameSize);
                int frameIndex = 0;
                while (frameIndex < framing.Frames.Count)
                {
                    if (!EnqueueReceivedData(framing.Frames[frameIndex]))
                    {
                        return;
                    }

                    frameIndex = frameIndex + 1;
                }

                retained = framing.Remainder;
                if (framing.Status == DiameterCodecStatus.Success)
                {
                    if (framing.CleanEndOfStream)
                    {
                        DiameterTransportIoResult terminal = CreateIoFailure(
                            DiameterTransportStatus.EndOfStream,
                            "clean-end-of-stream",
                            "The remote endpoint ended the stream at a frame boundary.");
                        terminal.EndOfStream = true;
                        SetTerminalReceiveResult(terminal);
                        return;
                    }

                    continue;
                }

                if (framing.Status == DiameterCodecStatus.Incomplete && !endOfStream)
                {
                    continue;
                }

                DiameterTransportIoResult terminalFailure = CreateFramingIoFailure(framing);
                SetTerminalReceiveResult(terminalFailure);
                return;
            }

            SetTerminalReceiveResult(result);
            return;
        }
    }

    internal static DiameterTransportIoResult CreateFramingIoFailure(
        DiameterFramingResult framing)
    {
        string code = "frame-invalid";
        string message = "The received Diameter frame is invalid.";
        if (framing.Issues.Count > 0)
        {
            code = framing.Issues[0].Code;
            message = framing.Issues[0].Message;
        }

        DiameterTransportIoResult result = CreateIoFailure(
            DiameterTransportStatus.Failed,
            code,
            message);
        result.EndOfStream = framing.EndOfStream;
        return result;
    }

    internal bool EnqueueReceivedData(byte[] data)
    {
        lock (StateLock)
        {
            while (IngressQueue.Count >= IngressCapacity && !Closing)
            {
                Monitor.Wait(StateLock);
            }

            if (Closing)
            {
                return false;
            }

            ConnectionReadItem item = new ConnectionReadItem();
            item.Data = data;
            IngressQueue.Enqueue(item);
            Monitor.PulseAll(StateLock);
            return true;
        }
    }

    internal void SetTerminalReceiveResult(DiameterTransportIoResult result)
    {
        lock (StateLock)
        {
            if (!Closing)
            {
                TerminalReceiveResult = CloneIoResult(result);
                Monitor.PulseAll(StateLock);
            }
        }
    }

    internal void CompleteQueuedWritesAsClosed()
    {
        while (EgressQueue.Count > 0)
        {
            ConnectionWriteItem item = EgressQueue.Dequeue();
            lock (item.Sync)
            {
                item.Result = CreateClosedIoResult();
                item.Complete = true;
                Monitor.PulseAll(item.Sync);
            }
        }
    }

    internal static int ReadRemainingMilliseconds(long deadline)
    {
        long remaining = deadline - Environment.TickCount64;
        if (remaining <= 0L)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining;
    }

    internal static DiameterTransportIoResult CreateClosedIoResult()
    {
        return CreateIoFailure(
            DiameterTransportStatus.Closed,
            "connection-closed",
            "The connection is closed.");
    }

    internal static DiameterTransportIoResult CreateIoFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal static DiameterTransportIoResult CloneIoResult(
        DiameterTransportIoResult source)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.Status = source.Status;
        result.TransferredCount = source.TransferredCount;
        result.EndOfStream = source.EndOfStream;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterTransportOperationResult CloneOperationResult(
        DiameterTransportOperationResult source)
    {
        DiameterTransportOperationResult result = new DiameterTransportOperationResult();
        result.Status = source.Status;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterConnectionMetadata CloneConnectionMetadata(
        DiameterConnectionMetadata source)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = source.ConnectionId;
        result.LocalEndpoint = CloneEndpoint(source.LocalEndpoint);
        result.RemoteEndpoint = CloneEndpoint(source.RemoteEndpoint);
        result.Direction = source.Direction;
        result.Transport = CloneTransportMetadata(source.Transport);
        result.Security = CloneSecurityMetadata(source.Security);
        result.InboundStream = source.InboundStream;
        result.OutboundStream = source.OutboundStream;
        result.EstablishedAt = source.EstablishedAt;
        result.Open = source.Open;
        return result;
    }

    internal static DiameterEndpoint CloneEndpoint(DiameterEndpoint source)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = source.Identity.Value;
        result.Host = source.Host;
        result.Address = source.Address;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Security = source.Security;
        return result;
    }

    internal static DiameterTransportMetadata CloneTransportMetadata(
        DiameterTransportMetadata source)
    {
        DiameterTransportMetadata result = new DiameterTransportMetadata();
        result.Kind = source.Kind;
        result.SupportsActiveConnections = source.SupportsActiveConnections;
        result.SupportsListeners = source.SupportsListeners;
        result.SupportsIpv4 = source.SupportsIpv4;
        result.SupportsIpv6 = source.SupportsIpv6;
        result.MessageOriented = source.MessageOriented;
        result.MaximumInboundStreams = source.MaximumInboundStreams;
        result.MaximumOutboundStreams = source.MaximumOutboundStreams;
        result.PayloadProtocolIdentifier = source.PayloadProtocolIdentifier;
        return result;
    }

    internal static DiameterSecurityMetadata CloneSecurityMetadata(
        DiameterSecurityMetadata source)
    {
        DiameterSecurityMetadata result = new DiameterSecurityMetadata();
        result.Mode = source.Mode;
        result.Authenticated = source.Authenticated;
        result.Encrypted = source.Encrypted;
        result.MutuallyAuthenticated = source.MutuallyAuthenticated;
        result.Protocol = source.Protocol;
        result.CipherSuite = source.CipherSuite;
        result.LocalCertificateSubject = source.LocalCertificateSubject;
        result.PeerCertificateSubject = source.PeerCertificateSubject;
        result.PeerCertificateIssuer = source.PeerCertificateIssuer;
        result.PeerCertificateThumbprint = source.PeerCertificateThumbprint;
        result.PeerCertificateNotBefore = source.PeerCertificateNotBefore;
        result.PeerCertificateNotAfter = source.PeerCertificateNotAfter;
        int index = 0;
        while (index < source.PeerCertificateDnsNames.Count)
        {
            result.PeerCertificateDnsNames.Add(source.PeerCertificateDnsNames[index]);
            index = index + 1;
        }

        return result;
    }
}

internal class ConnectionReadItem
{
    internal byte[] Data = new byte[0];
    internal int Offset;
}

internal class ConnectionWriteItem
{
    internal byte[] Data = new byte[0];
    internal long Deadline;
    internal object Sync = new object();
    internal DiameterTransportIoResult Result = new DiameterTransportIoResult();
    internal bool Started;
    internal bool Cancelled;
    internal bool Complete;
}
