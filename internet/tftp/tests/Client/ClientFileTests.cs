using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientFileTests
{
    [Test]
    public void DownloadPublishesNewFileOnlyAfterFinalBlock()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string destinationPath = Path.Combine(directory.Path, "download.bin");
        byte[] firstBlock = new byte[TftpProtocolValues.DefaultBlockSize];
        firstBlock[0] = 1;
        byte[] expected = new byte[firstBlock.Length + 1];
        firstBlock.CopyTo(expected, 0);
        expected[^1] = 2;
        using Socket server = CreateServerSocket();
        ManualResetEventSlim firstAcknowledged = new ManualResetEventSlim(false);
        ManualResetEventSlim allowFinalBlock = new ManualResetEventSlim(false);
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateData(1, firstBlock), client);
            TftpPacket firstAck = ReceivePacket(server, ref client);
            Assert.That(firstAck.BlockNumber, Is.EqualTo(1));
            firstAcknowledged.Set();
            Assert.That(allowFinalBlock.Wait(5000), Is.True);
            SendPacket(server, CreateData(2, new byte[] { 2 }), client);
            TftpPacket finalAck = ReceivePacket(server, ref client);
            Assert.That(finalAck.BlockNumber, Is.EqualTo(2));
        });
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = CreateReadRequest(server);
        request.Options.DallyDurationSeconds = 0;

        Task<TftpTransferResult> transfer = Task.Run(() =>
            client.DownloadToFile(
                request,
                destinationPath,
                CancellationToken.None));
        Assert.That(firstAcknowledged.Wait(5000), Is.True);
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(destinationPath), Is.False);
                Assert.That(Directory.GetFiles(directory.Path), Has.Length.EqualTo(1));
                Assert.That(Path.GetDirectoryName(
                    Directory.GetFiles(directory.Path)[0]),
                    Is.EqualTo(directory.Path));
                Assert.That(Directory.GetFiles(directory.Path)[0],
                    Does.EndWith(".tftp.tmp"));
            });
        }
        finally
        {
            allowFinalBlock.Set();
        }

        TftpTransferResult result = transfer.GetAwaiter().GetResult();
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(expected));
            Assert.That(Directory.GetFiles(directory.Path), Has.Length.EqualTo(1));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.BytesTransferred, Is.EqualTo(expected.Length));
        });
    }

    [Test]
    public void SuccessfulDownloadAtomicallyReplacesExistingDestination()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string destinationPath = Path.Combine(directory.Path, "existing.bin");
        File.WriteAllBytes(destinationPath, new byte[] { 9, 9, 9 });
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateData(1, new byte[] { 1, 2 }), client);
            ReceivePacket(server, ref client);
        });
        TftpReadRequest request = CreateReadRequest(server);
        request.Options.DallyDurationSeconds = 0;

        new TftpClient(null).DownloadToFile(
            request,
            destinationPath,
            CancellationToken.None);
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(destinationPath),
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(Directory.GetFiles(directory.Path), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void FailedDownloadPreservesDestinationAndRemovesTemporaryFile()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string destinationPath = Path.Combine(directory.Path, "existing.bin");
        byte[] original = { 7, 8, 9 };
        File.WriteAllBytes(destinationPath, original);
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(
                server,
                CreateData(
                    1,
                    new byte[TftpProtocolValues.DefaultBlockSize]),
                client);
            TftpPacket acknowledgement = ReceivePacket(server, ref client);
            Assert.That(acknowledgement.BlockNumber, Is.EqualTo(1));
            SendPacket(server, CreateError(
                TftpErrorCode.AccessViolation,
                "denied"), client);
        });

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            new TftpClient(null).DownloadToFile(
                CreateReadRequest(server),
                destinationPath,
                CancellationToken.None));
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory.Path),
                Is.EqualTo(new[] { destinationPath }));
        });
    }

    [Test]
    public void CancelledDownloadPreservesDestinationAndRemovesTemporaryFile()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string destinationPath = Path.Combine(directory.Path, "existing.bin");
        byte[] original = { 3, 2, 1 };
        File.WriteAllBytes(destinationPath, original);
        using Socket server = CreateServerSocket();
        ManualResetEventSlim requestReceived = new ManualResetEventSlim(false);
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            requestReceived.Set();
        });
        CancellationTokenSource cancellation = new CancellationTokenSource();
        Task<TftpTransferResult> transfer = Task.Run(() =>
            new TftpClient(null).DownloadToFile(
                CreateReadRequest(server),
                destinationPath,
                cancellation.Token));
        Assert.That(requestReceived.Wait(5000), Is.True);

        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            transfer.GetAwaiter().GetResult());
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory.Path),
                Is.EqualTo(new[] { destinationPath }));
        });
    }

    [Test]
    public void UploadReadsFileSafelyAndReleasesHandleAfterCompletion()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string sourcePath = Path.Combine(directory.Path, "upload.bin");
        byte[] content = { 4, 5, 6 };
        File.WriteAllBytes(sourcePath, content);
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            TftpPacket requestPacket = ReceivePacket(server, ref client);
            Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            SendPacket(server, CreateAck(0), client);
            TftpPacket data = ReceivePacket(server, ref client);
            Assert.That(data.Payload, Is.EqualTo(content));
            SendPacket(server, CreateAck(1), client);
        });

        TftpTransferResult result = new TftpClient(null).UploadFromFile(
            CreateWriteRequest(server),
            sourcePath,
            CancellationToken.None);
        peer.GetAwaiter().GetResult();
        byte[] retainedContent = File.ReadAllBytes(sourcePath);
        using FileStream writable = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.BytesTransferred, Is.EqualTo(content.Length));
            Assert.That(retainedContent, Is.EqualTo(content));
            Assert.That(writable.CanWrite, Is.True);
        });
    }

    [Test]
    public void FailedUploadPreservesSourceReleasesHandleAndExposesRemoteError()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string sourcePath = Path.Combine(directory.Path, "upload.bin");
        byte[] content = { 7, 6, 5 };
        File.WriteAllBytes(sourcePath, content);
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            TftpPacket requestPacket = ReceivePacket(server, ref client);
            Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            SendPacket(server, CreateError(
                TftpErrorCode.FileAlreadyExists,
                "destination exists"), client);
        });

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            new TftpClient(null).UploadFromFile(
                CreateWriteRequest(server),
                sourcePath,
                CancellationToken.None));
        peer.GetAwaiter().GetResult();
        byte[] retainedContent = File.ReadAllBytes(sourcePath);
        using FileStream writable = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode,
                Is.EqualTo(TftpErrorCode.FileAlreadyExists));
            Assert.That(exception.RemoteMessage, Is.EqualTo("destination exists"));
            Assert.That(retainedContent, Is.EqualTo(content));
            Assert.That(writable.CanWrite, Is.True);
        });
    }

    [Test]
    public void CancelledUploadPreservesSourceAndReleasesHandle()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        string sourcePath = Path.Combine(directory.Path, "upload.bin");
        byte[] content = { 3, 4, 5 };
        File.WriteAllBytes(sourcePath, content);
        using Socket server = CreateServerSocket();
        using ManualResetEventSlim dataReceived = new ManualResetEventSlim(false);
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateAck(0), client);
            ReceivePacket(server, ref client);
            dataReceived.Set();
        });
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Task<TftpTransferResult> transfer = Task.Run(() =>
            new TftpClient(null).UploadFromFile(
                CreateWriteRequest(server),
                sourcePath,
                cancellation.Token));
        Assert.That(dataReceived.Wait(5000), Is.True);

        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            transfer.GetAwaiter().GetResult());
        peer.GetAwaiter().GetResult();
        byte[] retainedContent = File.ReadAllBytes(sourcePath);
        using FileStream writable = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None);

        Assert.Multiple(() =>
        {
            Assert.That(retainedContent, Is.EqualTo(content));
            Assert.That(writable.CanWrite, Is.True);
        });
    }

    [Test]
    public void FileOperationsExposePathAndInputFailuresWithoutArtifacts()
    {
        using ClientFileTestDirectory directory = new ClientFileTestDirectory();
        TftpClient client = new TftpClient(null);
        string missingDirectory = Path.Combine(
            directory.Path,
            "missing",
            "download.bin");
        string missingSource = Path.Combine(directory.Path, "missing.bin");

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToFile(
                    new TftpReadRequest(), "", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromFile(
                    new TftpWriteRequest(), " ", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToFile(
                    null!, missingDirectory, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromFile(
                    null!, missingSource, CancellationToken.None));
            Assert.Throws<DirectoryNotFoundException>(() =>
                client.DownloadToFile(
                    ValidReadRequest(), missingDirectory, CancellationToken.None));
            Assert.Throws<FileNotFoundException>(() =>
                client.UploadFromFile(
                    ValidWriteRequest(), missingSource, CancellationToken.None));
            Assert.Throws<UnauthorizedAccessException>(() =>
                client.UploadFromFile(
                    ValidWriteRequest(), directory.Path, CancellationToken.None));
        });

        Assert.That(Directory.GetFiles(directory.Path), Is.Empty);
    }

    private static Socket CreateServerSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.ReceiveTimeout = 5000;
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }

    private static TftpReadRequest CreateReadRequest(Socket server)
    {
        TftpReadRequest request = ValidReadRequest();
        request.Endpoint.Port = ((IPEndPoint)server.LocalEndPoint!).Port;
        return request;
    }

    private static TftpWriteRequest CreateWriteRequest(Socket server)
    {
        TftpWriteRequest request = ValidWriteRequest();
        request.Endpoint.Port = ((IPEndPoint)server.LocalEndPoint!).Port;
        return request;
    }

    private static TftpReadRequest ValidReadRequest()
    {
        TftpReadRequest request = new TftpReadRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.RemoteFilename = "remote.bin";
        return request;
    }

    private static TftpWriteRequest ValidWriteRequest()
    {
        TftpWriteRequest request = new TftpWriteRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.RemoteFilename = "remote.bin";
        return request;
    }

    private static TftpPacket ReceivePacket(Socket socket, ref EndPoint remote)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        int length = socket.ReceiveFrom(buffer, ref remote);
        return new TftpPacketCodec().Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
    }

    private static void SendPacket(
        Socket socket,
        TftpPacket packet,
        EndPoint remote)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        socket.SendTo(encoded, remote);
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    private static TftpPacket CreateAck(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
        return packet;
    }

    private static TftpPacket CreateError(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }
}

internal class ClientFileTestDirectory : IDisposable
{
    internal string Path;

    internal ClientFileTestDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "tftp-client-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}
