using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class SctpSocketDiameterConnection : IDiameterConnection
{
    internal Socket OwnedSocket;
    internal DiameterConnectionMetadata Metadata;
    internal int OutboundStreamCount;
    internal int NextOutboundStream;
    internal object StateLock = new object();
    internal object SendLock = new object();
    internal object ReceiveLock = new object();
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closed;

    internal SctpSocketDiameterConnection(
        Socket socket,
        DiameterConnectionMetadata metadata,
        int outboundStreamCount)
    {
        OwnedSocket = socket;
        Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
        OutboundStreamCount = outboundStreamCount;
    }

    /// <summary>Sends one ordered Diameter SCTP user message with payload identifier 46.</summary>
    /// <param name="source">The complete Diameter message bytes.</param>
    /// <param name="timeoutMilliseconds">The positive total send timeout.</param>
    /// <returns>The send outcome and transferred byte count.</returns>
    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive SCTP send timeout is required.");
        }

        byte[] data = new byte[source.Length];
        source.CopyTo(data);
        lock (SendLock)
        {
            lock (StateLock)
            {
                if (Closed)
                {
                    return CreateClosedSctpIoResult();
                }
            }

            if (data.Length == 0)
            {
                return new DiameterTransportIoResult();
            }

            long deadline = Environment.TickCount64 + timeoutMilliseconds;
            int stream = NextOutboundStream;
            Task configuration = Task.Run(() =>
                SctpSocketSupport.ConfigureSctpDefaultSend(
                    OwnedSocket,
                    stream,
                    DiameterProtocolConstants.SctpPayloadProtocolIdentifier));
            int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
            bool configured = remaining > 0 && SpinWait.SpinUntil(
                () => configuration.IsCompleted,
                remaining);
            if (!configured)
            {
                CloseDiameterConnection();
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "send-timeout",
                    "The SCTP send policy did not complete before its timeout.");
            }

            if (!configuration.IsCompletedSuccessfully)
            {
                DiameterTransportIoResult failure =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "sctp-send-policy-failed",
                        ReadSctpTaskFailure(
                            configuration,
                            "The SCTP stream and payload policy could not be applied."));
                CloseDiameterConnection();
                return failure;
            }

            Task<int> sendTask = OwnedSocket.SendAsync(
                new ReadOnlyMemory<byte>(data),
                SocketFlags.None,
                CancellationToken.None).AsTask();
            remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
            bool completed = remaining > 0 && SpinWait.SpinUntil(
                () => sendTask.IsCompleted,
                remaining);
            if (!completed)
            {
                CloseDiameterConnection();
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "send-timeout",
                    "The SCTP send did not complete before its timeout.");
            }

            if (!sendTask.IsCompletedSuccessfully)
            {
                bool closed;
                lock (StateLock)
                {
                    closed = Closed;
                }

                if (closed)
                {
                    return CreateClosedSctpIoResult();
                }

                DiameterTransportIoResult failure =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "send-failed",
                        ReadSctpTaskFailure(sendTask, "The SCTP send failed."));
                CloseDiameterConnection();
                return failure;
            }

            int sent = sendTask.Result;
            if (sent != data.Length)
            {
                DiameterTransportIoResult partial =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "partial-sctp-message",
                        "The SCTP socket did not send the complete user message atomically.");
                partial.TransferredCount = sent;
                CloseDiameterConnection();
                return partial;
            }

            lock (StateLock)
            {
                Metadata.OutboundStream = stream;
            }

            NextOutboundStream = (NextOutboundStream + 1) % OutboundStreamCount;
            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = sent;
            return result;
        }
    }

    /// <summary>Receives one available ordered SCTP byte segment.</summary>
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
                "A positive SCTP receive timeout is required.");
        }

        lock (ReceiveLock)
        {
            lock (StateLock)
            {
                if (Closed)
                {
                    return CreateClosedSctpIoResult();
                }
            }

            if (destination.Length == 0)
            {
                return new DiameterTransportIoResult();
            }

            byte[] data = new byte[destination.Length];
            Task<int> receiveTask = OwnedSocket.ReceiveAsync(
                new Memory<byte>(data),
                SocketFlags.None,
                CancellationToken.None).AsTask();
            bool completed = SpinWait.SpinUntil(
                () => receiveTask.IsCompleted,
                timeoutMilliseconds);
            if (!completed)
            {
                CloseDiameterConnection();
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "receive-timeout",
                    "The SCTP receive did not complete before its timeout.");
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
                    return CreateClosedSctpIoResult();
                }

                DiameterTransportIoResult failure =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "receive-failed",
                        ReadSctpTaskFailure(receiveTask, "The SCTP receive failed."));
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
                    "The SCTP socket returned an invalid receive count.");
            }

            data.AsSpan(0, received).CopyTo(destination);
            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = received;
            return result;
        }
    }

    /// <summary>Reads an independent SCTP connection metadata snapshot.</summary>
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

    /// <summary>Closes the owned SCTP socket idempotently.</summary>
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

    internal DiameterTransportIoResult CreateClosedSctpIoResult()
    {
        return QueuedDiameterConnection.CreateClosedIoResult();
    }

    internal string ReadSctpTaskFailure(Task task, string fallback)
    {
        return ReadStaticSctpTaskFailure(task, fallback);
    }

    internal static string ReadStaticSctpTaskFailure(Task task, string fallback)
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
