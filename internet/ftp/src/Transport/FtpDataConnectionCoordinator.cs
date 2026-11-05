using System;
using System.Net.Sockets;
using System.Threading;

namespace ftp;

internal class FtpDataConnectionCoordinator
{
    internal Socket? Connection;
    internal Func<int, CancellationToken, Socket>? OnDemandConnection;
    internal Action? SetupCleanup;
    internal bool Preopened;
    internal bool TransferInProgress;
    internal bool ConnectionAcquired;
    internal bool WriteShutdown;
    internal bool Aborted;

    internal FtpDataConnectionCoordinator()
    {
        Connection = null;
        OnDemandConnection = null;
        SetupCleanup = null;
        Preopened = false;
        TransferInProgress = false;
        ConnectionAcquired = false;
        WriteShutdown = false;
        Aborted = false;
    }

    internal void SetPreopenedConnection(Socket? connection)
    {
        if (connection == null || !connection.Connected)
        {
            throw new FtpConfigurationException(
                "A connected pre-opened data socket is required.", null);
        }

        PrepareReplacement();
        Connection = connection;
        Preopened = true;
        ConnectionAcquired = true;
    }

    internal void SetOnDemandConnection(
        Func<int, CancellationToken, Socket>? connectionFactory,
        Action? setupCleanup)
    {
        if (connectionFactory == null)
        {
            throw new FtpConfigurationException(
                "An on-demand data connection factory is required.", null);
        }

        PrepareReplacement();
        OnDemandConnection = connectionFactory;
        SetupCleanup = setupCleanup;
        Preopened = false;
        ConnectionAcquired = false;
    }

    internal FtpReply BeginTransfer()
    {
        if (TransferInProgress)
        {
            throw new FtpProtocolException(
                "An FTP data transfer is already in progress.", null);
        }

        if (!HasDataSetup())
        {
            throw new FtpConfigurationException(
                "A pre-opened or on-demand data connection is required.", null);
        }

        TransferInProgress = true;
        WriteShutdown = false;
        Aborted = false;
        if (Connection != null)
        {
            ConnectionAcquired = true;
            return FtpEndpointCodec.CreateEndpointReply(
                FtpReplyCodes.DataConnectionAlreadyOpen,
                "Data connection already open; transfer starting");
        }

        ConnectionAcquired = false;
        return FtpEndpointCodec.CreateEndpointReply(
            FtpReplyCodes.FileStatusOkayOpeningDataConnection,
            "File status okay; opening data connection");
    }

    internal Socket OpenDataConnection(
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (!TransferInProgress)
        {
            throw new FtpProtocolException(
                "An FTP transfer must begin before opening its data connection.", null);
        }

        FtpDeadline deadline = FtpTimeout.StartDeadline(timeoutMilliseconds);
        if (Connection != null)
        {
            return ValidatePreopenedConnection(cancellationToken, deadline);
        }

        Func<int, CancellationToken, Socket>? connectionFactory = OnDemandConnection;
        if (connectionFactory == null)
        {
            throw new FtpConfigurationException(
                "The on-demand data connection setup was already consumed.", null);
        }

        FtpFaultIsolationResult openResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TransferPump.CheckOperationState(cancellationToken, deadline);
            int remainingMilliseconds = FtpTimeout.GetRemainingWaitMilliseconds(deadline);
            Socket socket = connectionFactory.Invoke(
                remainingMilliseconds, cancellationToken);
            Connection = socket;
            if (!socket.Connected)
            {
                throw new FtpConnectionException(
                    "The on-demand data connection is not connected.", null);
            }

            TransferPump.CheckOperationState(cancellationToken, deadline);
            return null;
        });

        Action? setupCleanup = SetupCleanup;
        OnDemandConnection = null;
        SetupCleanup = null;
        FtpFaultIsolationResult setupCleanupResult =
            RunSetupCleanup(setupCleanup);
        bool canceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = FtpTimeout.HasDeadlineExpired(deadline);
        if (openResult.Succeeded && setupCleanupResult.Succeeded &&
            !canceled && !timedOut && Connection != null)
        {
            ConnectionAcquired = true;
            return Connection;
        }

        Exception? cleanupFailure = ReleaseOwnedResources();
        ThrowOpenFailure(
            openResult,
            setupCleanupResult,
            cleanupFailure,
            cancellationToken,
            deadline);
        throw new FtpConnectionException(
            "The FTP data connection did not reach a terminal result.", null);
    }

    internal void ShutdownWriteSide()
    {
        if (!TransferInProgress || !ConnectionAcquired || Connection == null)
        {
            throw new FtpProtocolException(
                "An acquired data connection is required before write shutdown.", null);
        }

        if (WriteShutdown)
        {
            return;
        }

        Socket socket = Connection;
        FtpFaultIsolationResult shutdownResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            socket.Shutdown(SocketShutdown.Send);
            return null;
        });
        if (!shutdownResult.Succeeded)
        {
            Exception? cleanupFailure = ReleaseOwnedResources();
            Exception? failure = shutdownResult.Failure == null ?
                cleanupFailure : shutdownResult.Failure;
            throw new FtpConnectionException(
                "The FTP data connection write side could not be shut down.", failure);
        }

        WriteShutdown = true;
    }

    internal void CompleteTransfer()
    {
        if (!TransferInProgress || !ConnectionAcquired || Connection == null)
        {
            throw new FtpProtocolException(
                "No acquired FTP data connection is ready for completion.", null);
        }

        Exception? cleanupFailure = ReleaseOwnedResources();
        if (cleanupFailure != null)
        {
            throw new FtpConnectionException(
                "The completed FTP data connection could not be closed.", cleanupFailure);
        }
    }

    internal bool AbortTransfer()
    {
        bool transferWasInProgress = TransferInProgress;
        Exception? cleanupFailure = ReleaseOwnedResources();
        Aborted = true;
        if (cleanupFailure != null)
        {
            throw new FtpConnectionException(
                "The aborted FTP data setup could not be closed.", cleanupFailure);
        }

        return transferWasInProgress;
    }

    internal void ResetSession()
    {
        Exception? cleanupFailure = ReleaseOwnedResources();
        Aborted = false;
        if (cleanupFailure != null)
        {
            throw new FtpConnectionException(
                "The FTP session data setup could not be reset.", cleanupFailure);
        }
    }

    internal bool HasDataSetup()
    {
        return Connection != null || OnDemandConnection != null;
    }

    internal Socket ValidatePreopenedConnection(
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        Socket? socket = Connection;
        FtpFaultIsolationResult validationResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            TransferPump.CheckOperationState(cancellationToken, deadline);
            if (socket == null || !socket.Connected)
            {
                throw new FtpConnectionException(
                    "The pre-opened FTP data connection is no longer connected.", null);
            }

            return null;
        });
        bool canceled = FtpCancellation.IsCancellationRequested(cancellationToken);
        bool timedOut = FtpTimeout.HasDeadlineExpired(deadline);
        if (validationResult.Succeeded && !canceled && !timedOut && socket != null)
        {
            ConnectionAcquired = true;
            return socket;
        }

        Exception? cleanupFailure = ReleaseOwnedResources();
        FtpFaultIsolationResult emptyCleanup = RunSetupCleanup(null);
        ThrowOpenFailure(
            validationResult,
            emptyCleanup,
            cleanupFailure,
            cancellationToken,
            deadline);
        throw new FtpConnectionException(
            "The pre-opened FTP data connection did not reach a terminal result.", null);
    }

    internal void PrepareReplacement()
    {
        if (TransferInProgress)
        {
            throw new FtpProtocolException(
                "An active data setup cannot be replaced during a transfer.", null);
        }

        Exception? cleanupFailure = ReleaseOwnedResources();
        Aborted = false;
        if (cleanupFailure != null)
        {
            throw new FtpConnectionException(
                "The previous FTP data setup could not be replaced.", cleanupFailure);
        }
    }

    internal Exception? ReleaseOwnedResources()
    {
        Socket? socket = Connection;
        Action? setupCleanup = SetupCleanup;
        Connection = null;
        OnDemandConnection = null;
        SetupCleanup = null;
        Preopened = false;
        TransferInProgress = false;
        ConnectionAcquired = false;
        WriteShutdown = false;

        FtpResourceLifetime connectionLifetime = new FtpResourceLifetime(socket);
        FtpFaultIsolationResult connectionCleanupResult =
            connectionLifetime.DisposeResource();
        FtpFaultIsolationResult setupCleanupResult =
            RunSetupCleanup(setupCleanup);
        if (!connectionCleanupResult.Succeeded)
        {
            return connectionCleanupResult.Failure;
        }

        if (!setupCleanupResult.Succeeded)
        {
            return setupCleanupResult.Failure;
        }

        return null;
    }

    internal FtpFaultIsolationResult RunSetupCleanup(Action? setupCleanup)
    {
        return FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (setupCleanup != null)
            {
                setupCleanup.Invoke();
            }

            return null;
        });
    }

    internal void ThrowOpenFailure(
        FtpFaultIsolationResult openResult,
        FtpFaultIsolationResult setupCleanupResult,
        Exception? cleanupFailure,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The FTP data connection setup was canceled.",
                openResult.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException(
                "The FTP data connection setup timed out.", openResult.Failure);
        }

        if (!openResult.Succeeded)
        {
            throw new FtpConnectionException(
                "The FTP data connection setup failed.", openResult.Failure);
        }

        if (!setupCleanupResult.Succeeded)
        {
            throw new FtpConnectionException(
                "The consumed FTP data setup could not be closed.",
                setupCleanupResult.Failure);
        }

        if (cleanupFailure != null)
        {
            throw new FtpConnectionException(
                "The failed FTP data connection could not be closed.", cleanupFailure);
        }
    }
}
