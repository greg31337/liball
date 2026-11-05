using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class TcpSocketDiameterConnection : IDiameterConnection
{
    internal Socket OwnedSocket;
    internal DiameterConnectionMetadata Metadata;
    internal object StateLock = new object();
    internal object SendLock = new object();
    internal object ReceiveLock = new object();
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal int MaximumSendChunkSize = 65536;
    internal int SendOperationCount;
    internal bool Closed;

    internal TcpSocketDiameterConnection(
        Socket socket,
        DiameterConnectionMetadata metadata)
    {
        OwnedSocket = socket;
        Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
    }

    /// <summary>Sends every supplied byte over the owned TCP socket.</summary>
    /// <param name="source">The bytes to send.</param>
    /// <param name="timeoutMilliseconds">The positive total send timeout.</param>
    /// <returns>The send outcome and total byte count.</returns>
    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive TCP send timeout is required.");
        }

        byte[] data = new byte[source.Length];
        source.CopyTo(data);
        lock (SendLock)
        {
            lock (StateLock)
            {
                if (Closed)
                {
                    return CreateClosedTcpIoResult(0);
                }
            }

            if (data.Length == 0)
            {
                return new DiameterTransportIoResult();
            }

            int offset = 0;
            long deadline = Environment.TickCount64 + timeoutMilliseconds;
            while (offset < data.Length)
            {
                Task<int>? sendTask = null;
                int requested = Math.Min(MaximumSendChunkSize, data.Length - offset);
                lock (StateLock)
                {
                    if (Closed)
                    {
                        return CreateClosedTcpIoResult(offset);
                    }

                    ReadOnlyMemory<byte> remaining = new ReadOnlyMemory<byte>(
                        data,
                        offset,
                        requested);
                    sendTask = OwnedSocket.SendAsync(
                        remaining,
                        SocketFlags.None,
                        CancellationToken.None).AsTask();
                    SendOperationCount = SendOperationCount + 1;
                }

                int timeout = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
                bool completed = timeout > 0 &&
                    SpinWait.SpinUntil(() => sendTask.IsCompleted, timeout);
                if (!completed)
                {
                    CloseDiameterConnection();
                    DiameterTransportIoResult timedOut =
                        QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.TimedOut,
                            "send-timeout",
                            "The TCP send did not complete before its timeout.");
                    timedOut.TransferredCount = offset;
                    return timedOut;
                }

                if (!sendTask.IsCompletedSuccessfully)
                {
                    bool closed;
                    lock (StateLock)
                    {
                        closed = Closed;
                    }

                    DiameterTransportIoResult failure;
                    if (closed)
                    {
                        failure = CreateClosedTcpIoResult(offset);
                    }
                    else
                    {
                        failure = QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.Failed,
                            "send-failed",
                            ReadSocketTaskFailure(sendTask, "The TCP send failed."));
                        failure.TransferredCount = offset;
                        CloseDiameterConnection();
                    }

                    return failure;
                }

                int sent = sendTask.Result;
                if (sent <= 0 || sent > requested)
                {
                    CloseDiameterConnection();
                    DiameterTransportIoResult invalid =
                        QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.Failed,
                            "invalid-send-count",
                            "The TCP socket returned an invalid send count.");
                    invalid.TransferredCount = offset;
                    return invalid;
                }

                offset = offset + sent;
            }

            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = offset;
            return result;
        }
    }

    /// <summary>Receives one available TCP byte segment.</summary>
    /// <param name="destination">The destination for received bytes.</param>
    /// <param name="timeoutMilliseconds">The positive receive timeout.</param>
    /// <returns>The receive, timeout, closure, or end-of-stream outcome.</returns>
    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive TCP receive timeout is required.");
        }

        lock (ReceiveLock)
        {
            if (destination.Length == 0)
            {
                lock (StateLock)
                {
                    if (Closed)
                    {
                        return CreateClosedTcpIoResult(0);
                    }
                }

                return new DiameterTransportIoResult();
            }

            byte[] data = new byte[destination.Length];
            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                Task<int>? receiveTask = null;
                lock (StateLock)
                {
                    if (Closed)
                    {
                        return CreateClosedTcpIoResult(0);
                    }

                    receiveTask = OwnedSocket.ReceiveAsync(
                        new Memory<byte>(data),
                        SocketFlags.None,
                        cancellation.Token).AsTask();
                }

                bool completed = SpinWait.SpinUntil(
                    () => receiveTask.IsCompleted,
                    timeoutMilliseconds);
                if (!completed)
                {
                    cancellation.Cancel();
                    completed = SpinWait.SpinUntil(
                        () => receiveTask.IsCompleted,
                        1000);
                    if (!completed)
                    {
                        CloseDiameterConnection();
                        return QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.TimedOut,
                            "receive-timeout",
                            "The TCP receive did not complete before its timeout.");
                    }

                    if (!receiveTask.IsCompletedSuccessfully)
                    {
                        return QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.TimedOut,
                            "receive-timeout",
                            "The TCP receive did not complete before its timeout.");
                    }
                }

                if (receiveTask.IsCanceled)
                {
                    return QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.TimedOut,
                        "receive-timeout",
                        "The TCP receive did not complete before its timeout.");
                }

                if (!receiveTask.IsCompletedSuccessfully)
                {
                    bool closed;
                    lock (StateLock)
                    {
                        closed = Closed;
                    }

                    if (closed)
                    {
                        return CreateClosedTcpIoResult(0);
                    }

                    DiameterTransportIoResult failure =
                        QueuedDiameterConnection.CreateIoFailure(
                            DiameterTransportStatus.Failed,
                            "receive-failed",
                            ReadSocketTaskFailure(receiveTask, "The TCP receive failed."));
                    CloseDiameterConnection();
                    return failure;
                }

                int received = receiveTask.Result;
                if (received == 0)
                {
                    CloseDiameterConnection();
                    DiameterTransportIoResult end = new DiameterTransportIoResult();
                    end.Status = DiameterTransportStatus.EndOfStream;
                    end.EndOfStream = true;
                    return end;
                }

                if (received < 0 || received > data.Length)
                {
                    CloseDiameterConnection();
                    return QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "invalid-receive-count",
                        "The TCP socket returned an invalid receive count.");
                }

                data.AsSpan(0, received).CopyTo(destination);
                DiameterTransportIoResult result = new DiameterTransportIoResult();
                result.TransferredCount = received;
                return result;
            }
        }
    }

    /// <summary>Reads an independent TCP connection metadata snapshot.</summary>
    /// <returns>The connection metadata.</returns>
    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        lock (StateLock)
        {
            DiameterConnectionMetadata result =
                QueuedDiameterConnection.CloneConnectionMetadata(Metadata);
            result.Open = !Closed;
            return result;
        }
    }

    /// <summary>Closes the owned TCP socket idempotently.</summary>
    /// <returns>The close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        lock (StateLock)
        {
            if (Closed)
            {
                return QueuedDiameterConnection.CloneOperationResult(CloseResult);
            }

            Closed = true;
            Metadata.Open = false;
            OwnedSocket.Close();
            return QueuedDiameterConnection.CloneOperationResult(CloseResult);
        }
    }

    internal DiameterTransportIoResult CreateClosedTcpIoResult(int transferredCount)
    {
        DiameterTransportIoResult result = QueuedDiameterConnection.CreateClosedIoResult();
        result.TransferredCount = transferredCount;
        return result;
    }

    internal string ReadSocketTaskFailure(Task task, string fallback)
    {
        AggregateException? aggregate = task.Exception;
        if (aggregate == null)
        {
            return fallback;
        }

        Exception failure = aggregate.GetBaseException();
        if (string.IsNullOrEmpty(failure.Message))
        {
            return fallback;
        }

        return failure.Message;
    }
}

internal class TcpAddressResolution
{
    internal DiameterTransportStatus Status;
    internal List<System.Net.IPAddress> Addresses = new List<System.Net.IPAddress>();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}
