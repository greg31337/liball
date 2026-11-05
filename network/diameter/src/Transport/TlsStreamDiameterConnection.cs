using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal class TlsStreamDiameterConnection : IDiameterConnection
{
    internal IDiameterConnection OwnedConnection;
    internal SslStream OwnedStream;
    internal DiameterConnectionMetadata Metadata;
    internal object StateLock = new object();
    internal object SendLock = new object();
    internal object ReceiveLock = new object();
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closed;

    internal TlsStreamDiameterConnection(
        IDiameterConnection connection,
        SslStream stream,
        DiameterConnectionMetadata metadata)
    {
        OwnedConnection = connection;
        OwnedStream = stream;
        Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
    }

    /// <summary>Sends every supplied byte through the authenticated TLS stream.</summary>
    /// <param name="source">The bytes to send.</param>
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
                "A positive TLS send timeout is required.");
        }

        byte[] data = new byte[source.Length];
        source.CopyTo(data);
        lock (SendLock)
        {
            lock (StateLock)
            {
                if (Closed)
                {
                    return CreateClosedTlsIoResult();
                }
            }

            if (data.Length == 0)
            {
                return new DiameterTransportIoResult();
            }

            Task writeTask = OwnedStream.WriteAsync(
                new ReadOnlyMemory<byte>(data),
                CancellationToken.None).AsTask();
            bool completed = SpinWait.SpinUntil(
                () => writeTask.IsCompleted,
                timeoutMilliseconds);
            if (!completed)
            {
                CloseDiameterConnection();
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "send-timeout",
                    "The TLS send did not complete before its timeout.");
            }

            if (!writeTask.IsCompletedSuccessfully)
            {
                bool closed;
                lock (StateLock)
                {
                    closed = Closed;
                }

                if (closed)
                {
                    return CreateClosedTlsIoResult();
                }

                DiameterTransportIoResult failure =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "send-failed",
                        ReadTlsTaskFailure(writeTask, "The TLS send failed."));
                CloseDiameterConnection();
                return failure;
            }

            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = data.Length;
            return result;
        }
    }

    /// <summary>Receives one decrypted byte segment from the authenticated TLS stream.</summary>
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
                "A positive TLS receive timeout is required.");
        }

        lock (ReceiveLock)
        {
            lock (StateLock)
            {
                if (Closed)
                {
                    return CreateClosedTlsIoResult();
                }
            }

            if (destination.Length == 0)
            {
                return new DiameterTransportIoResult();
            }

            byte[] data = new byte[destination.Length];
            Task<int> readTask = OwnedStream.ReadAsync(
                new Memory<byte>(data),
                CancellationToken.None).AsTask();
            bool completed = SpinWait.SpinUntil(
                () => readTask.IsCompleted,
                timeoutMilliseconds);
            if (!completed)
            {
                CloseDiameterConnection();
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.TimedOut,
                    "receive-timeout",
                    "The TLS receive did not complete before its timeout.");
            }

            if (!readTask.IsCompletedSuccessfully)
            {
                bool closed;
                lock (StateLock)
                {
                    closed = Closed;
                }

                if (closed)
                {
                    return CreateClosedTlsIoResult();
                }

                DiameterTransportIoResult failure =
                    QueuedDiameterConnection.CreateIoFailure(
                        DiameterTransportStatus.Failed,
                        "receive-failed",
                        ReadTlsTaskFailure(readTask, "The TLS receive failed."));
                CloseDiameterConnection();
                return failure;
            }

            int received = readTask.Result;
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
                    "The TLS stream returned an invalid receive count.");
            }

            data.AsSpan(0, received).CopyTo(destination);
            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = received;
            return result;
        }
    }

    /// <summary>Reads an independent authenticated TLS connection metadata snapshot.</summary>
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

    /// <summary>Closes the TLS stream and owned TCP connection idempotently.</summary>
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
        }

        Task closeStream = Task.Run(() => OwnedStream.Close());
        SpinWait.SpinUntil(() => closeStream.IsCompleted, 1000);
        DiameterTransportOperationResult closeConnection =
            OwnedConnection.CloseDiameterConnection();
        lock (StateLock)
        {
            CloseResult = QueuedDiameterConnection.CloneOperationResult(closeConnection);
            return QueuedDiameterConnection.CloneOperationResult(CloseResult);
        }
    }

    internal static DiameterTransportIoResult CreateClosedTlsIoResult()
    {
        return QueuedDiameterConnection.CreateIoFailure(
            DiameterTransportStatus.Closed,
            "connection-closed",
            "The TLS connection is closed.");
    }

    internal static string ReadTlsTaskFailure(Task task, string fallback)
    {
        AggregateException? failure = task.Exception;
        if (failure == null)
        {
            return fallback;
        }

        return failure.GetBaseException().Message;
    }
}
