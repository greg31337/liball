using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpDataConnectionCoordinatorTests
{
    [Test]
    public void Preopened_connection_selects_125_and_is_returned_without_reopening()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);

        FtpReply reply = coordinator.BeginTransfer();
        Socket connection = coordinator.OpenDataConnection(
            5000, default(CancellationToken));

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code,
                Is.EqualTo(FtpReplyCodes.DataConnectionAlreadyOpen));
            Assert.That(reply.Classification.Category,
                Is.EqualTo(FtpReplyCategory.PositivePreliminary));
            Assert.That(connection, Is.SameAs(pair.Client));
            Assert.That(coordinator.Preopened, Is.True);
            Assert.That(coordinator.ConnectionAcquired, Is.True);
        });

        coordinator.CompleteTransfer();
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(coordinator.TransferInProgress, Is.False);
        });
    }

    [Test]
    public void On_demand_connection_selects_150_and_opens_only_when_requested()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        int factoryCalls = 0;
        int receivedTimeout = 0;
        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            factoryCalls++;
            receivedTimeout = timeoutMilliseconds;
            return pair.Client!;
        }, null);

        FtpReply reply = coordinator.BeginTransfer();
        int callsBeforeOpen = factoryCalls;
        Socket connection = coordinator.OpenDataConnection(
            5000, default(CancellationToken));

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code,
                Is.EqualTo(FtpReplyCodes.FileStatusOkayOpeningDataConnection));
            Assert.That(callsBeforeOpen, Is.Zero);
            Assert.That(factoryCalls, Is.EqualTo(1));
            Assert.That(receivedTimeout, Is.InRange(1, 5000));
            Assert.That(connection, Is.SameAs(pair.Client));
            Assert.That(coordinator.Preopened, Is.False);
            Assert.That(coordinator.ConnectionAcquired, Is.True);
        });

        coordinator.CompleteTransfer();
        pair.Client = null;
    }

    [Test]
    public void On_demand_setup_cleanup_runs_once_after_successful_acquisition()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        int cleanupCalls = 0;
        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            return pair.Client!;
        }, () =>
        {
            cleanupCalls++;
        });
        coordinator.BeginTransfer();

        coordinator.OpenDataConnection(5000, default(CancellationToken));
        coordinator.CompleteTransfer();
        pair.Client = null;

        Assert.That(cleanupCalls, Is.EqualTo(1));
    }

    [Test]
    public void Immediate_timeout_and_cancellation_close_preopened_connections()
    {
        using SocketPair timedOutPair = CreateSocketPair();
        FtpDataConnectionCoordinator timedOut = new FtpDataConnectionCoordinator();
        timedOut.SetPreopenedConnection(timedOutPair.Client);
        timedOut.BeginTransfer();

        Assert.Throws<TimeoutException>(() =>
            timedOut.OpenDataConnection(0, default(CancellationToken)));
        Assert.That(timedOutPair.Client!.SafeHandle.IsClosed, Is.True);
        timedOutPair.Client = null;

        using SocketPair canceledPair = CreateSocketPair();
        FtpDataConnectionCoordinator canceled = new FtpDataConnectionCoordinator();
        canceled.SetPreopenedConnection(canceledPair.Client);
        canceled.BeginTransfer();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                canceled.OpenDataConnection(5000, cancellation.Token));
            Assert.That(canceledPair.Client!.SafeHandle.IsClosed, Is.True);
            Assert.That(canceled.HasDataSetup(), Is.False);
        });
        canceledPair.Client = null;
    }

    [Test]
    public void Write_shutdown_half_closes_the_socket_and_is_idempotent()
    {
        using SocketPair pair = CreateSocketPair();
        pair.Server.ReceiveTimeout = 5000;
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        coordinator.BeginTransfer();
        Socket connection = coordinator.OpenDataConnection(
            5000, default(CancellationToken));
        connection.Send(new byte[] { 1, 2, 3 });

        coordinator.ShutdownWriteSide();
        coordinator.ShutdownWriteSide();
        byte[] buffer = new byte[8];
        int bytesRead = pair.Server.Receive(buffer);
        int endOfStream = pair.Server.Receive(buffer);

        Assert.Multiple(() =>
        {
            Assert.That(bytesRead, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[1], Is.EqualTo(2));
            Assert.That(buffer[2], Is.EqualTo(3));
            Assert.That(endOfStream, Is.Zero);
            Assert.That(coordinator.WriteShutdown, Is.True);
        });

        coordinator.CompleteTransfer();
        pair.Client = null;
    }

    [Test]
    public void ABOR_closes_pending_setup_and_reports_whether_transfer_started()
    {
        FtpDataConnectionCoordinator pending = new FtpDataConnectionCoordinator();
        int pendingCleanupCalls = 0;
        int pendingFactoryCalls = 0;
        pending.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            pendingFactoryCalls++;
            throw new InvalidOperationException("The pending factory must not run.");
        }, () =>
        {
            pendingCleanupCalls++;
        });

        bool pendingTransfer = pending.AbortTransfer();

        FtpDataConnectionCoordinator active = new FtpDataConnectionCoordinator();
        int activeCleanupCalls = 0;
        active.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            throw new InvalidOperationException("The active factory must not run.");
        }, () =>
        {
            activeCleanupCalls++;
        });
        active.BeginTransfer();

        bool activeTransfer = active.AbortTransfer();

        Assert.Multiple(() =>
        {
            Assert.That(pendingTransfer, Is.False);
            Assert.That(activeTransfer, Is.True);
            Assert.That(pendingFactoryCalls, Is.Zero);
            Assert.That(pendingCleanupCalls, Is.EqualTo(1));
            Assert.That(activeCleanupCalls, Is.EqualTo(1));
            Assert.That(pending.Aborted, Is.True);
            Assert.That(active.Aborted, Is.True);
            Assert.That(active.TransferInProgress, Is.False);
        });
    }

    [Test]
    public void ABOR_closes_an_acquired_connection()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        Socket ownedConnection = pair.Client!;
        coordinator.SetPreopenedConnection(ownedConnection);
        coordinator.BeginTransfer();
        coordinator.OpenDataConnection(5000, default(CancellationToken));

        bool transferWasInProgress = coordinator.AbortTransfer();

        Assert.Multiple(() =>
        {
            Assert.That(transferWasInProgress, Is.True);
            Assert.That(ownedConnection.SafeHandle.IsClosed, Is.True);
            Assert.That(coordinator.Aborted, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
        });
        pair.Client = null;
    }

    [Test]
    public void Failed_on_demand_open_cleans_setup_and_resets_transfer_state()
    {
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        int cleanupCalls = 0;
        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            throw new SocketException((int)SocketError.ConnectionRefused);
        }, () =>
        {
            cleanupCalls++;
        });
        coordinator.BeginTransfer();

        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            coordinator.OpenDataConnection(5000, default(CancellationToken)));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException, Is.TypeOf<SocketException>());
            Assert.That(cleanupCalls, Is.EqualTo(1));
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(coordinator.TransferInProgress, Is.False);
            Assert.That(coordinator.ConnectionAcquired, Is.False);
        });
    }

    [Test]
    public void Replacing_and_reset_consume_owned_resources_once()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        int cleanupCalls = 0;
        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            throw new InvalidOperationException("The replaced factory must not run.");
        }, () =>
        {
            cleanupCalls++;
        });

        coordinator.SetPreopenedConnection(pair.Client);

        Assert.Multiple(() =>
        {
            Assert.That(cleanupCalls, Is.EqualTo(1));
            Assert.That(coordinator.Preopened, Is.True);
            Assert.That(coordinator.Connection, Is.SameAs(pair.Client));
        });

        coordinator.ResetSession();
        coordinator.ResetSession();
        Assert.Multiple(() =>
        {
            Assert.That(pair.Client!.SafeHandle.IsClosed, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(coordinator.Aborted, Is.False);
        });
        pair.Client = null;
    }

    [Test]
    public void Invalid_coordinator_sequences_are_rejected()
    {
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                coordinator.SetPreopenedConnection(null));
            Assert.Throws<FtpConfigurationException>(() =>
                coordinator.SetOnDemandConnection(null, null));
            Assert.Throws<FtpConfigurationException>(() => coordinator.BeginTransfer());
            Assert.Throws<FtpProtocolException>(() =>
                coordinator.OpenDataConnection(5000, default(CancellationToken)));
            Assert.Throws<FtpProtocolException>(() => coordinator.ShutdownWriteSide());
            Assert.Throws<FtpProtocolException>(() => coordinator.CompleteTransfer());
        });

        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            throw new InvalidOperationException("The incomplete transfer must not open.");
        }, null);
        coordinator.BeginTransfer();
        Assert.Throws<FtpProtocolException>(() => coordinator.CompleteTransfer());
        coordinator.AbortTransfer();
    }

    private static SocketPair CreateSocketPair()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Socket client = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(IPAddress.Loopback, port);
        Socket server = listener.AcceptSocket();
        listener.Stop();
        return new SocketPair(client, server);
    }

    private class SocketPair : IDisposable
    {
        public Socket? Client;
        public Socket Server;

        public SocketPair(Socket client, Socket server)
        {
            Client = client;
            Server = server;
        }

        public void Dispose()
        {
            if (Client != null)
            {
                Client.Dispose();
            }

            Server.Dispose();
        }
    }
}
