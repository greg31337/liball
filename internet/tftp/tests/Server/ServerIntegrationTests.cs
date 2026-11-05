using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class ServerIntegrationTests
{
    [Test]
    public void ServesReadsAndCommitsWritesWithDirectoryStorage()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = [10, 20, 30, 40, 50];
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "download.bin"),
            downloadContent);
        RecordingServerObserver observer = new RecordingServerObserver();
        TftpServer server = new TftpServer(null, observer);
        TftpServerOptions options = CreateDirectoryOptions(directory.RootPath);
        Task runTask = Task.Run(() => server.Run(
            options,
            CancellationToken.None));

        try
        {
            IPEndPoint listenerEndPoint = WaitForListener(server);
            using Socket client = CreateClientSocket();

            TftpPacket readRequest = new TftpPacket();
            readRequest.Opcode = TftpOpcode.ReadRequest;
            readRequest.Filename = "download.bin";
            SendPacket(client, listenerEndPoint, readRequest);

            IPEndPoint readTransferEndPoint;
            TftpPacket data = ReceivePacket(
                client,
                out readTransferEndPoint);
            Assert.Multiple(() =>
            {
                Assert.That(data.Opcode, Is.EqualTo(TftpOpcode.Data));
                Assert.That(data.BlockNumber, Is.EqualTo(1));
                Assert.That(data.Payload, Is.EqualTo(downloadContent));
                Assert.That(readTransferEndPoint.Port,
                    Is.Not.EqualTo(listenerEndPoint.Port));
            });

            TftpPacket acknowledgement = new TftpPacket();
            acknowledgement.Opcode = TftpOpcode.Acknowledgement;
            acknowledgement.BlockNumber = 1;
            SendPacket(client, readTransferEndPoint, acknowledgement);
            Assert.That(SpinWait.SpinUntil(
                () => server.GetStatus().CompletedTransferCount == 1,
                2000),
                Is.True);

            TftpPacket writeRequest = new TftpPacket();
            writeRequest.Opcode = TftpOpcode.WriteRequest;
            writeRequest.Filename = "upload.bin";
            SendPacket(client, listenerEndPoint, writeRequest);

            IPEndPoint writeTransferEndPoint;
            TftpPacket acknowledgementZero = ReceivePacket(
                client,
                out writeTransferEndPoint);
            Assert.Multiple(() =>
            {
                Assert.That(acknowledgementZero.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(acknowledgementZero.BlockNumber, Is.Zero);
                Assert.That(writeTransferEndPoint.Port,
                    Is.Not.EqualTo(listenerEndPoint.Port));
            });

            byte[] uploadContent = [60, 70, 80];
            TftpPacket uploadData = new TftpPacket();
            uploadData.Opcode = TftpOpcode.Data;
            uploadData.BlockNumber = 1;
            uploadData.Payload = uploadContent;
            SendPacket(client, writeTransferEndPoint, uploadData);
            IPEndPoint finalAcknowledgementEndPoint;
            TftpPacket finalAcknowledgement = ReceivePacket(
                client,
                out finalAcknowledgementEndPoint);
            Assert.Multiple(() =>
            {
                Assert.That(finalAcknowledgement.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(finalAcknowledgement.BlockNumber,
                    Is.EqualTo(1));
                Assert.That(finalAcknowledgementEndPoint,
                    Is.EqualTo(writeTransferEndPoint));
            });

            Assert.That(SpinWait.SpinUntil(
                () => server.GetStatus().CompletedTransferCount == 2,
                2000),
                Is.True);
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(
                Directory.GetFiles(directory.RootPath, "*.tftp.tmp"),
                Is.Empty);

            server.Stop(TftpServerStopMode.Graceful);
            Assert.That(SpinWait.SpinUntil(() => runTask.IsCompleted, 2000),
                Is.True);
            runTask.GetAwaiter().GetResult();

            TftpServerStatus status = server.GetStatus();
            TftpTransferEvent readCompleted = observer.Events.Single(
                transferEvent =>
                    transferEvent.State == TftpTransferState.Completed &&
                    transferEvent.Direction == TftpTransferDirection.Read);
            TftpTransferEvent writeCompleted = observer.Events.Single(
                transferEvent =>
                    transferEvent.State == TftpTransferState.Completed &&
                    transferEvent.Direction == TftpTransferDirection.Write);
            Assert.Multiple(() =>
            {
                Assert.That(status.IsRunning, Is.False);
                Assert.That(status.IsDraining, Is.False);
                Assert.That(status.ActiveTransferCount, Is.Zero);
                Assert.That(status.AcceptedTransferCount, Is.EqualTo(2));
                Assert.That(status.CompletedTransferCount, Is.EqualTo(2));
                Assert.That(status.FailedTransferCount, Is.Zero);
                Assert.That(observer.Events, Has.Count.EqualTo(4));
                Assert.That(readCompleted.BytesTransferred,
                    Is.EqualTo(downloadContent.Length));
                Assert.That(readCompleted.ServerRequest!.Filename,
                    Is.EqualTo("download.bin"));
                Assert.That(writeCompleted.BytesTransferred,
                    Is.EqualTo(uploadContent.Length));
                Assert.That(writeCompleted.ServerRequest!.Filename,
                    Is.EqualTo("upload.bin"));
            });
        }
        finally
        {
            server.Stop(TftpServerStopMode.Immediate);
            SpinWait.SpinUntil(() => runTask.IsCompleted, 2000);
        }
    }

    [Test]
    public void ImmediateStopCancelsAnActiveDirectoryRead()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "large.bin"),
            new byte[1024]);
        TftpServer server = new TftpServer(null, null);
        TftpServerOptions options = CreateDirectoryOptions(directory.RootPath);
        Task runTask = Task.Run(() => server.Run(
            options,
            CancellationToken.None));

        try
        {
            IPEndPoint listenerEndPoint = WaitForListener(server);
            using Socket client = CreateClientSocket();
            TftpPacket readRequest = new TftpPacket();
            readRequest.Opcode = TftpOpcode.ReadRequest;
            readRequest.Filename = "large.bin";
            SendPacket(client, listenerEndPoint, readRequest);

            IPEndPoint transferEndPoint;
            TftpPacket data = ReceivePacket(client, out transferEndPoint);
            Assert.That(data.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(data.Payload, Has.Length.EqualTo(
                TftpProtocolValues.DefaultBlockSize));
            Assert.That(SpinWait.SpinUntil(
                () => server.GetStatus().ActiveTransferCount == 1,
                1000),
                Is.True);

            server.Stop(TftpServerStopMode.Immediate);
            Assert.That(SpinWait.SpinUntil(() => runTask.IsCompleted, 2000),
                Is.True);
            runTask.GetAwaiter().GetResult();

            TftpServerStatus status = server.GetStatus();
            Assert.Multiple(() =>
            {
                Assert.That(status.IsRunning, Is.False);
                Assert.That(status.ActiveTransferCount, Is.Zero);
                Assert.That(status.AcceptedTransferCount, Is.EqualTo(1));
                Assert.That(status.CompletedTransferCount, Is.Zero);
                Assert.That(status.FailedTransferCount, Is.EqualTo(1));
            });
        }
        finally
        {
            server.Stop(TftpServerStopMode.Immediate);
            SpinWait.SpinUntil(() => runTask.IsCompleted, 2000);
        }
    }

    [Test]
    public void RejectsMissingStorageAndConcurrentRunsAndReturnsSnapshots()
    {
        TftpServer missingStorageServer = new TftpServer(null, null);
        TftpServerOptions missingStorageOptions = CreateListenerOptions();
        Assert.Throws<TftpStorageException>(() => missingStorageServer.Run(
            missingStorageOptions,
            CancellationToken.None));

        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        TftpServer server = new TftpServer(null, null);
        TftpServerOptions options = CreateDirectoryOptions(directory.RootPath);
        Task runTask = Task.Run(() => server.Run(
            options,
            CancellationToken.None));

        try
        {
            IPEndPoint listenerEndPoint = WaitForListener(server);
            TftpServerStatus firstStatus = server.GetStatus();
            firstStatus.BoundEndpoints.Clear();
            TftpServerStatus secondStatus = server.GetStatus();

            Assert.Multiple(() =>
            {
                Assert.That(secondStatus.IsRunning, Is.True);
                Assert.That(secondStatus.BoundEndpoints, Has.Count.EqualTo(1));
                Assert.That(secondStatus.BoundEndpoints[0].Port,
                    Is.EqualTo(listenerEndPoint.Port));
                Assert.Throws<TftpProtocolException>(() => server.Run(
                    options,
                    CancellationToken.None));
                Assert.Throws<TftpProtocolException>(() => server.Stop(
                    (TftpServerStopMode)99));
            });

            server.Stop(TftpServerStopMode.Graceful);
            Assert.That(SpinWait.SpinUntil(() => runTask.IsCompleted, 2000),
                Is.True);
            runTask.GetAwaiter().GetResult();
            Assert.That(server.GetStatus().IsRunning, Is.False);
        }
        finally
        {
            server.Stop(TftpServerStopMode.Immediate);
            SpinWait.SpinUntil(() => runTask.IsCompleted, 2000);
        }
    }

    private static Socket CreateClientSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        socket.ReceiveTimeout = 2000;
        return socket;
    }

    private static TftpServerOptions CreateDirectoryOptions(string rootPath)
    {
        TftpServerOptions options = CreateListenerOptions();
        options.DallyDurationSeconds = 0;
        TftpDirectoryStorageOptions directoryOptions =
            new TftpDirectoryStorageOptions();
        directoryOptions.RootPath = rootPath;
        directoryOptions.AllowRead = true;
        directoryOptions.AllowWrite = true;
        directoryOptions.AllowCreate = true;
        directoryOptions.AllowOverwrite = true;
        options.DirectoryStorage = directoryOptions;
        return options;
    }

    private static TftpServerOptions CreateListenerOptions()
    {
        TftpServerOptions options = new TftpServerOptions();
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = IPAddress.Loopback;
        binding.Port = 0;
        options.Bindings.Add(binding);
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 1;
        options.MaximumSessionDurationSeconds = 5;
        return options;
    }

    private static IPEndPoint WaitForListener(TftpServer server)
    {
        Assert.That(SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = server.GetStatus();
            return status.IsRunning && status.BoundEndpoints.Count == 1;
        }, 2000), Is.True);
        TftpServerBinding binding = server.GetStatus().BoundEndpoints[0];
        return new IPEndPoint(binding.Address, binding.Port);
    }

    private static void SendPacket(
        Socket socket,
        IPEndPoint endPoint,
        TftpPacket packet)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] buffer = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, buffer.AsSpan());
        socket.SendTo(buffer, endPoint);
    }

    private static TftpPacket ReceivePacket(
        Socket socket,
        out IPEndPoint sourceEndPoint)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        EndPoint source = new IPEndPoint(IPAddress.Any, 0);
        int length = socket.ReceiveFrom(buffer, ref source);
        sourceEndPoint = (IPEndPoint)source;
        TftpPacketCodec codec = new TftpPacketCodec();
        return codec.Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
    }
}
