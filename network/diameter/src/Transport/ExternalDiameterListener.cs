using System.Threading;

namespace diameter;

internal class ExternalDiameterListener : IDiameterListener
{
    internal DiameterExternalTransportFactory Owner;
    internal IDiameterListener OwnedListener;
    internal DiameterTransportConfiguration Transport;
    internal DiameterSecurityConfiguration Security;
    internal DiameterListenerMetadata Metadata;
    internal object StateLock = new object();
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closing;
    internal bool Closed;

    internal ExternalDiameterListener(
        DiameterExternalTransportFactory owner,
        IDiameterListener listener,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterListenerMetadata metadata)
    {
        Owner = owner;
        OwnedListener = listener;
        Transport = transport;
        Security = security;
        Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
    }

    /// <summary>Accepts and validates one host-provided connection.</summary>
    /// <param name="timeoutMilliseconds">The positive accept timeout.</param>
    /// <returns>The validated accepted connection outcome.</returns>
    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return Owner.CreateConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive external accept timeout is required.");
        }

        lock (StateLock)
        {
            if (Closing || Closed)
            {
                return Owner.CreateConnectionFailure(
                    DiameterTransportStatus.Closed,
                    "listener-closed",
                    "The external listener is closed.");
            }
        }

        DiameterConnectionResult? supplied =
            OwnedListener.AcceptDiameterConnection(timeoutMilliseconds);
        lock (StateLock)
        {
            if (Closing || Closed)
            {
                if (supplied != null && supplied.Connection != null)
                {
                    supplied.Connection.CloseDiameterConnection();
                }

                return Owner.CreateConnectionFailure(
                    DiameterTransportStatus.Closed,
                    "listener-closed",
                    "The external listener is closed.");
            }
        }

        return Owner.IntegrateConnectionResult(
            supplied,
            Transport,
            Security,
            null,
            DiameterConnectionDirection.Inbound);
    }

    /// <summary>Reads an independent metadata snapshot from the adapter.</summary>
    /// <returns>The current external listener metadata.</returns>
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

    /// <summary>Closes the owned host listener exactly once.</summary>
    /// <returns>The stable host close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterListener()
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
            OwnedListener.CloseDiameterListener();
        lock (StateLock)
        {
            if (supplied == null)
            {
                CloseResult.Status = DiameterTransportStatus.Failed;
                CloseResult.Code = "external-close-result-invalid";
                CloseResult.Message = "The host transport returned no listener close result.";
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
