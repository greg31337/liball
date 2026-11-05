using System;
using System.Threading;

namespace diameter;

internal class ExternalDiameterConnection : IDiameterConnection
{
    internal IDiameterConnection OwnedConnection;
    internal DiameterConnectionMetadata Metadata;
    internal object StateLock = new object();
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closing;
    internal bool Closed;

    internal ExternalDiameterConnection(
        IDiameterConnection connection,
        DiameterConnectionMetadata metadata)
    {
        OwnedConnection = connection;
        Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
    }

    /// <summary>Sends bytes through the validated host connection.</summary>
    /// <param name="source">The bytes to send.</param>
    /// <param name="timeoutMilliseconds">The positive send timeout.</param>
    /// <returns>The validated host send outcome.</returns>
    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive external send timeout is required.");
        }

        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.Closed,
                    "connection-closed",
                    "The external connection is closed.");
            }
        }

        DiameterTransportIoResult? supplied =
            OwnedConnection.SendDiameterConnectionBytes(source, timeoutMilliseconds);
        if (supplied == null || supplied.TransferredCount < 0 ||
            supplied.TransferredCount > source.Length || supplied.EndOfStream ||
            (supplied.Status == DiameterTransportStatus.Success &&
            source.Length > 0 && supplied.TransferredCount == 0))
        {
            CloseDiameterConnection();
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.Failed,
                "external-io-result-invalid",
                "The host transport returned an invalid send result.");
        }

        return QueuedDiameterConnection.CloneIoResult(supplied);
    }

    /// <summary>Receives bytes through the validated host connection.</summary>
    /// <param name="destination">The destination for received bytes.</param>
    /// <param name="timeoutMilliseconds">The positive receive timeout.</param>
    /// <returns>The validated host receive outcome.</returns>
    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive external receive timeout is required.");
        }

        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return QueuedDiameterConnection.CreateIoFailure(
                    DiameterTransportStatus.Closed,
                    "connection-closed",
                    "The external connection is closed.");
            }
        }

        DiameterTransportIoResult? supplied =
            OwnedConnection.ReceiveDiameterConnectionBytes(
                destination,
                timeoutMilliseconds);
        if (supplied == null || supplied.TransferredCount < 0 ||
            supplied.TransferredCount > destination.Length ||
            (supplied.Status == DiameterTransportStatus.Success &&
            destination.Length > 0 && supplied.TransferredCount == 0) ||
            (supplied.EndOfStream &&
            supplied.Status != DiameterTransportStatus.EndOfStream) ||
            (!supplied.EndOfStream &&
            supplied.Status == DiameterTransportStatus.EndOfStream))
        {
            CloseDiameterConnection();
            return QueuedDiameterConnection.CreateIoFailure(
                DiameterTransportStatus.Failed,
                "external-io-result-invalid",
                "The host transport returned an invalid receive result.");
        }

        return QueuedDiameterConnection.CloneIoResult(supplied);
    }

    /// <summary>Reads an independent metadata snapshot from the adapter.</summary>
    /// <returns>The current external connection metadata.</returns>
    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        lock (StateLock)
        {
            DiameterConnectionMetadata result =
                QueuedDiameterConnection.CloneConnectionMetadata(Metadata);
            result.Open = !Closing && !Closed;
            return result;
        }
    }

    /// <summary>Closes the owned host connection exactly once.</summary>
    /// <returns>The stable host close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        bool ownsClose = false;
        lock (StateLock)
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                ownsClose = true;
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

        DiameterTransportOperationResult? supplied =
            OwnedConnection.CloseDiameterConnection();
        lock (StateLock)
        {
            if (supplied == null)
            {
                CloseResult.Status = DiameterTransportStatus.Failed;
                CloseResult.Code = "external-close-result-invalid";
                CloseResult.Message = "The host transport returned no close result.";
            }
            else
            {
                CloseResult = QueuedDiameterConnection.CloneOperationResult(supplied);
            }

            Closed = true;
            Metadata.Open = false;
            Monitor.PulseAll(StateLock);
            return QueuedDiameterConnection.CloneOperationResult(CloseResult);
        }
    }
}
