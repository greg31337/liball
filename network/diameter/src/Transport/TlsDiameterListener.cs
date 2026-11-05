using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace diameter;

internal class TlsDiameterListener : IDiameterListener
{
    internal const int AuthenticationTimeoutMilliseconds = 10000;
    internal IDiameterListener OwnedListener;
    internal DiameterSecurityConfiguration Security;
    internal X509Certificate2 LocalCertificate;
    internal X509Certificate2? TrustAnchor;
    internal DiameterListenerMetadata Metadata;
    internal int QueueCapacity;
    internal Queue<DiameterConnectionResult> AcceptedConnections =
        new Queue<DiameterConnectionResult>();
    internal object StateLock = new object();
    internal Thread Worker;
    internal IDiameterConnection? AuthenticatingConnection;
    internal DiameterTransportOperationResult CloseResult =
        new DiameterTransportOperationResult();
    internal bool Closing;
    internal bool Closed;

    internal TlsDiameterListener(
        IDiameterListener listener,
        DiameterSecurityConfiguration security,
        X509Certificate2 localCertificate,
        X509Certificate2? trustAnchor,
        DiameterListenerMetadata metadata,
        int queueCapacity)
    {
        OwnedListener = listener;
        Security = security;
        LocalCertificate = localCertificate;
        TrustAnchor = trustAnchor;
        Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
        Metadata.SecurityMode = DiameterSecurityMode.Tls;
        Metadata.LocalEndpoint.Security = DiameterSecurityMode.Tls;
        QueueCapacity = queueCapacity;
        Worker = new Thread(() => RunTlsAcceptWorker());
        Worker.IsBackground = true;
        Worker.Name = "Diameter TLS accept worker";
        Worker.Start();
    }

    /// <summary>Waits for and transfers ownership of one authenticated TLS connection.</summary>
    /// <param name="timeoutMilliseconds">The positive accept timeout.</param>
    /// <returns>The authenticated connection, timeout, closure, or failure outcome.</returns>
    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            return CreateTlsAcceptFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "invalid-timeout",
                "A positive TLS accept timeout is required.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (StateLock)
        {
            while (AcceptedConnections.Count == 0 && !Closing && !Closed)
            {
                int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
                if (remaining <= 0)
                {
                    return CreateTlsAcceptFailure(
                        DiameterTransportStatus.TimedOut,
                        "accept-timeout",
                        "No authenticated TLS connection arrived before the timeout.");
                }

                Monitor.Wait(StateLock, remaining);
            }

            if (AcceptedConnections.Count > 0)
            {
                DiameterConnectionResult result = AcceptedConnections.Dequeue();
                Monitor.PulseAll(StateLock);
                return result;
            }

            return CreateTlsAcceptFailure(
                DiameterTransportStatus.Closed,
                "listener-closed",
                "The TLS listener is closed.");
        }
    }

    /// <summary>Reads an independent TLS listener metadata snapshot.</summary>
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

    /// <summary>Closes the listener, authentication worker, and unclaimed connections.</summary>
    /// <returns>The idempotent close outcome.</returns>
    public DiameterTransportOperationResult CloseDiameterListener()
    {
        lock (StateLock)
        {
            if (Closing || Closed)
            {
                while (!Closed)
                {
                    Monitor.Wait(StateLock);
                }

                return QueuedDiameterConnection.CloneOperationResult(CloseResult);
            }

            Closing = true;
            if (AuthenticatingConnection != null)
            {
                AuthenticatingConnection.CloseDiameterConnection();
            }

            Monitor.PulseAll(StateLock);
        }

        DiameterTransportOperationResult listenerClose =
            OwnedListener.CloseDiameterListener();
        Worker.Join();
        LocalCertificate.Dispose();
        if (TrustAnchor != null)
        {
            TrustAnchor.Dispose();
        }

        lock (StateLock)
        {
            while (AcceptedConnections.Count > 0)
            {
                DiameterConnectionResult accepted = AcceptedConnections.Dequeue();
                if (accepted.Connection != null)
                {
                    accepted.Connection.CloseDiameterConnection();
                }
            }

            CloseResult = QueuedDiameterConnection.CloneOperationResult(listenerClose);
            Closed = true;
            Metadata.Open = false;
            Monitor.PulseAll(StateLock);
            return QueuedDiameterConnection.CloneOperationResult(CloseResult);
        }
    }

    internal void RunTlsAcceptWorker()
    {
        while (true)
        {
            lock (StateLock)
            {
                if (Closing)
                {
                    return;
                }
            }

            DiameterConnectionResult accepted = OwnedListener.AcceptDiameterConnection(100);
            if (accepted.Status == DiameterTransportStatus.TimedOut)
            {
                continue;
            }

            if (accepted.Status != DiameterTransportStatus.Success ||
                accepted.Connection == null)
            {
                lock (StateLock)
                {
                    if (Closing || accepted.Status == DiameterTransportStatus.Closed)
                    {
                        return;
                    }
                }

                EnqueueTlsAcceptResult(accepted);
                continue;
            }

            lock (StateLock)
            {
                if (Closing)
                {
                    accepted.Connection.CloseDiameterConnection();
                    return;
                }

                AuthenticatingConnection = accepted.Connection;
            }

            DiameterConnectionResult authenticated =
                TlsCertificateSupport.AuthenticateTlsServer(
                    accepted.Connection,
                    accepted.Metadata,
                    Security,
                    LocalCertificate,
                    TrustAnchor,
                    AuthenticationTimeoutMilliseconds);
            lock (StateLock)
            {
                AuthenticatingConnection = null;
                if (Closing)
                {
                    if (authenticated.Connection != null)
                    {
                        authenticated.Connection.CloseDiameterConnection();
                    }

                    return;
                }
            }

            if (!EnqueueTlsAcceptResult(authenticated))
            {
                if (authenticated.Connection != null)
                {
                    authenticated.Connection.CloseDiameterConnection();
                }

                return;
            }
        }
    }

    internal bool EnqueueTlsAcceptResult(DiameterConnectionResult result)
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

            AcceptedConnections.Enqueue(result);
            Monitor.PulseAll(StateLock);
            return true;
        }
    }

    internal DiameterConnectionResult CreateTlsAcceptFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        result.Metadata.Security.Mode = DiameterSecurityMode.Tls;
        return result;
    }
}
