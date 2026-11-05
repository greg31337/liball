using System.Net;
using System.Net.Sockets;
using System.IO.Compression;

namespace ftp.Tests;

public class FtpClearDataStreamTests
{
    [TestCase(FtpCommandKind.List)]
    [TestCase(FtpCommandKind.Nlst)]
    [TestCase(FtpCommandKind.Mlsd)]
    [TestCase(FtpCommandKind.Retr)]
    [TestCase(FtpCommandKind.Stor)]
    [TestCase(FtpCommandKind.Stou)]
    [TestCase(FtpCommandKind.Appe)]
    public void Every_clear_data_command_can_begin(FtpCommandKind commandKind)
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        FtpClearDataStreamRequest request = CreateRequest(
            commandKind, new MemoryStream(new byte[] { 1 }), true);

        FtpReply preliminary = transfer.BeginSend(request);

        Assert.Multiple(() =>
        {
            Assert.That(preliminary.Code,
                Is.EqualTo(FtpReplyCodes.DataConnectionAlreadyOpen));
            Assert.That(transfer.Started, Is.True);
            Assert.That(transfer.Sending, Is.True);
        });

        coordinator.AbortTransfer();
        pair.Client = null;
    }

    [Test]
    public void Send_flushes_half_closes_and_closes_data_before_creating_226()
    {
        using SocketPair pair = CreateSocketPair();
        pair.Server.ReceiveTimeout = 5000;
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        byte[] content = new byte[] { 1, 2, 3, 4, 5 };
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Retr, new MemoryStream(content), true);

        FtpReply preliminary = transfer.BeginSend(request);
        Assert.Throws<FtpProtocolException>(() => transfer.CreateFinalReply());
        FtpTransferPumpResult pump = transfer.ExecuteSendData();
        byte[] received = ReceiveAll(pair.Server);
        FtpReply finalReply = transfer.CreateFinalReply();
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(preliminary.Code,
                Is.EqualTo(FtpReplyCodes.DataConnectionAlreadyOpen));
            Assert.That(received, Is.EqualTo(content));
            Assert.That(pump.SourceBytes, Is.EqualTo(content.Length));
            Assert.That(pump.DestinationBytes, Is.EqualTo(content.Length));
            Assert.That(transfer.DataComplete, Is.True);
            Assert.That(transfer.Result.DataConnectionClosed, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(finalReply.Code,
                Is.EqualTo(FtpReplyCodes.ClosingDataConnection));
            Assert.That(transfer.Result.Succeeded, Is.True);
            Assert.Throws<FtpProtocolException>(() => transfer.CreateFinalReply());
        });
    }

    [Test]
    public void Receive_reads_until_peer_EOF_flushes_local_output_and_then_accepts_final_reply()
    {
        using SocketPair pair = CreateSocketPair();
        byte[] content = new byte[] { 10, 20, 30, 40 };
        pair.Server.Send(content);
        pair.Server.Shutdown(SocketShutdown.Send);
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        TrackingWriteStream destination = new TrackingWriteStream();
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Stor, destination, false);

        transfer.BeginReceive(request);
        Assert.Throws<FtpProtocolException>(() =>
            transfer.ApplyFinalReply(CreateReply(226)));
        FtpTransferPumpResult pump = transfer.ExecuteReceiveData();
        Assert.Throws<FtpProtocolException>(() =>
            transfer.ApplyFinalReply(CreateReply(200)));
        FtpReply finalReply = CreateReply(226);
        transfer.ApplyFinalReply(finalReply);
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.False);
            Assert.That(pump.SourceBytes, Is.EqualTo(content.Length));
            Assert.That(pump.DestinationBytes, Is.EqualTo(content.Length));
            Assert.That(transfer.Result.FinalReply, Is.SameAs(finalReply));
            Assert.That(transfer.Result.Succeeded, Is.True);
            Assert.That(transfer.Result.DataConnectionClosed, Is.True);
        });
    }

    [Test]
    public void MODE_Z_send_wraps_wire_data_in_a_complete_ZLIB_stream()
    {
        using SocketPair pair = CreateSocketPair();
        pair.Server.ReceiveTimeout = 5000;
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        byte[] content = Enumerable.Repeat((byte)65, 4096).ToArray();
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Retr, new MemoryStream(content), true);
        request.Options!.Mode = FtpTransferMode.Deflate;

        transfer.BeginSend(request);
        transfer.ExecuteSendData();
        byte[] wire = ReceiveAll(pair.Server);
        byte[] decoded = DecompressZlib(wire);
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(wire[0], Is.EqualTo(0x78));
            Assert.That(wire.Length, Is.LessThan(content.Length));
            Assert.That(decoded, Is.EqualTo(content));
        });
    }

    [Test]
    public void MODE_Z_receive_inflates_ZLIB_data_before_the_transfer_pipeline()
    {
        using SocketPair pair = CreateSocketPair();
        byte[] content = Enumerable.Repeat((byte)90, 2048).ToArray();
        byte[] wire = CompressZlib(content);
        pair.Server.Send(wire);
        pair.Server.Shutdown(SocketShutdown.Send);
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        MemoryStream destination = new MemoryStream();
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Stor, destination, false);
        request.Options!.Mode = FtpTransferMode.Deflate;

        transfer.BeginReceive(request);
        transfer.ExecuteReceiveData();
        pair.Client = null;

        Assert.That(destination.ToArray(), Is.EqualTo(content));
    }

    [Test]
    public void On_demand_connection_is_not_opened_until_after_150_is_available()
    {
        using SocketPair pair = CreateSocketPair();
        pair.Server.ReceiveTimeout = 5000;
        int factoryCalls = 0;
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetOnDemandConnection((timeoutMilliseconds, cancellationToken) =>
        {
            factoryCalls++;
            return pair.Client!;
        }, null);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.List,
            new MemoryStream(new byte[] { 65, 13, 10 }),
            true);

        FtpReply preliminary = transfer.BeginSend(request);
        int callsBeforeExecution = factoryCalls;
        transfer.ExecuteSendData();
        byte[] received = ReceiveAll(pair.Server);
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(preliminary.Code,
                Is.EqualTo(FtpReplyCodes.FileStatusOkayOpeningDataConnection));
            Assert.That(callsBeforeExecution, Is.Zero);
            Assert.That(factoryCalls, Is.EqualTo(1));
            Assert.That(received, Is.EqualTo(new byte[] { 65, 13, 10 }));
            Assert.That(transfer.Result.FinalReply, Is.Null);
        });
    }

    [Test]
    public void Terminal_failure_reply_is_retained_only_after_data_cleanup()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Appe, new MemoryStream(new byte[0]), true);
        transfer.BeginSend(request);
        transfer.ExecuteSendData();
        FtpReply failure = CreateReply(
            FtpReplyCodes.RequestedActionAbortedLocalError);

        transfer.ApplyFinalReply(failure);
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(transfer.Result.FinalReply, Is.SameAs(failure));
            Assert.That(transfer.Result.Succeeded, Is.False);
            Assert.That(transfer.FinalReplyApplied, Is.True);
        });
    }

    [Test]
    public void Data_stream_shutdown_runs_before_stream_disposal()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        TrackingDataStream data = new TrackingDataStream();
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Retr,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            true);
        request.DataStreamFactory = socket => data;
        request.DataStreamShutdown = () => data.Shutdown();

        transfer.BeginSend(request);
        transfer.ExecuteSendData();
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(data.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(data.ShutdownCalled, Is.True);
            Assert.That(data.DisposedAtShutdown, Is.False);
            Assert.That(data.Disposed, Is.True);
            Assert.That(transfer.DataComplete, Is.True);
        });
    }

    [Test]
    public void Pump_failure_aborts_and_closes_the_data_connection_without_final_reply()
    {
        using SocketPair pair = CreateSocketPair();
        pair.Server.ReceiveTimeout = 5000;
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        FailingReadStream source = new FailingReadStream();
        FtpClearDataStreamRequest request = CreateRequest(
            FtpCommandKind.Stou, source, true);
        request.LeaveLocalStreamOpen = false;
        transfer.BeginSend(request);

        FtpTransferException? failure = Assert.Throws<FtpTransferException>(() =>
            transfer.ExecuteSendData());
        byte[] received = ReceiveAll(pair.Server);
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException, Is.TypeOf<FtpTransferException>());
            Assert.That(received, Is.Empty);
            Assert.That(source.Disposed, Is.True);
            Assert.That(coordinator.Aborted, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(transfer.DataComplete, Is.False);
            Assert.That(transfer.Result.FinalReply, Is.Null);
            Assert.Throws<FtpProtocolException>(() => transfer.CreateFinalReply());
        });
    }

    [Test]
    public void Invalid_requests_and_direction_mismatches_are_rejected()
    {
        using SocketPair pair = CreateSocketPair();
        FtpDataConnectionCoordinator coordinator = new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        FtpClearDataStreamRequest invalidCommand = CreateRequest(
            FtpCommandKind.Noop, new MemoryStream(), true);
        FtpClearDataStreamRequest hiddenExpected = CreateRequest(
            FtpCommandKind.Retr, new MemoryStream(), true);
        hiddenExpected.ExpectedBytes = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                transfer.BeginSend(null));
            Assert.Throws<FtpConfigurationException>(() =>
                transfer.BeginSend(invalidCommand));
            Assert.Throws<FtpConfigurationException>(() =>
                transfer.BeginSend(hiddenExpected));
            Assert.Throws<FtpProtocolException>(() => transfer.ExecuteSendData());
            Assert.Throws<FtpProtocolException>(() => transfer.ExecuteReceiveData());
        });

        FtpClearDataStreamRequest valid = CreateRequest(
            FtpCommandKind.Nlst, new MemoryStream(new byte[0]), true);
        transfer.BeginSend(valid);
        Assert.Throws<FtpProtocolException>(() => transfer.ExecuteReceiveData());
        coordinator.AbortTransfer();
        pair.Client = null;
    }

    private static FtpClearDataStreamRequest CreateRequest(
        FtpCommandKind commandKind,
        Stream localStream,
        bool sending)
    {
        FtpClearDataStreamRequest request = new FtpClearDataStreamRequest();
        request.CommandKind = commandKind;
        request.LocalStream = localStream;
        request.Direction = sending ?
            FtpTransferDirection.Upload : FtpTransferDirection.Download;
        request.ConnectionTimeoutMilliseconds = 5000;
        request.TransferTimeoutMilliseconds = 5000;
        request.LeaveLocalStreamOpen = true;
        return request;
    }

    private static FtpReply CreateReply(int code)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = "test reply";
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }

    private static byte[] ReceiveAll(Socket socket)
    {
        MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[32];
        int count = socket.Receive(buffer);
        while (count != 0)
        {
            output.Write(buffer, 0, count);
            count = socket.Receive(buffer);
        }

        return output.ToArray();
    }

    private static byte[] CompressZlib(byte[] content)
    {
        MemoryStream output = new MemoryStream();
        using (ZLibStream compression = new ZLibStream(
            output, CompressionLevel.Optimal, true))
        {
            compression.Write(content, 0, content.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressZlib(byte[] wire)
    {
        using ZLibStream decompression = new ZLibStream(
            new MemoryStream(wire), CompressionMode.Decompress);
        MemoryStream output = new MemoryStream();
        decompression.CopyTo(output);
        return output.ToArray();
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

    private class TrackingWriteStream : MemoryStream
    {
        public int FlushCalls;
        public bool Disposed;

        public TrackingWriteStream()
        {
            FlushCalls = 0;
            Disposed = false;
        }

        public override void Flush()
        {
            FlushCalls++;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class FailingReadStream : MemoryStream
    {
        public bool Disposed;

        public FailingReadStream()
        {
            Disposed = false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("read failed");
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class TrackingDataStream : MemoryStream
    {
        public bool ShutdownCalled;
        public bool DisposedAtShutdown;
        public bool Disposed;

        public void Shutdown()
        {
            ShutdownCalled = true;
            DisposedAtShutdown = Disposed;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
