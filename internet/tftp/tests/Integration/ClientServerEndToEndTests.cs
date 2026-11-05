using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class ClientServerEndToEndTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void DownloadsOptionlessOctetContentBoundaries(int contentLength)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content = Content(contentLength);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "download.bin"),
            content);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = ReadRequest(
            harness.ListenerEndPoint,
            "download.bin",
            TftpTransferMode.Octet,
            0);
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult result = client.DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(result.Direction,
                Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(result.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EncodedBytesTransferred,
                Is.EqualTo(contentLength));
            Assert.That(result.NegotiatedOptions, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void UploadsOptionlessOctetContentBoundaries(int contentLength)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content = Content(contentLength);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        TftpClient client = new TftpClient(null);
        TftpWriteRequest request = WriteRequest(
            harness.ListenerEndPoint,
            "upload.bin",
            TftpTransferMode.Octet,
            0,
            content.Length);
        using MemoryStream source = new MemoryStream(content);

        TftpTransferResult result = client.UploadFromStream(
            request,
            source,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "upload.bin")),
                Is.EqualTo(content));
            Assert.That(source.CanRead, Is.True);
            Assert.That(result.Direction,
                Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(result.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EncodedBytesTransferred,
                Is.EqualTo(contentLength));
            Assert.That(result.NegotiatedOptions, Is.Empty);
        });
    }

    [TestCase(TftpTransferDirection.Read, 0)]
    [TestCase(TftpTransferDirection.Read, 1)]
    [TestCase(TftpTransferDirection.Read, 2)]
    [TestCase(TftpTransferDirection.Write, 0)]
    [TestCase(TftpTransferDirection.Write, 1)]
    [TestCase(TftpTransferDirection.Write, 2)]
    public void TransfersWithNoPartialAndFullOptionSets(
        TftpTransferDirection direction,
        int optionLevel)
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content = Content(19);
        string filename = "options.bin";
        if (direction == TftpTransferDirection.Read)
        {
            File.WriteAllBytes(
                Path.Combine(directory.RootPath, filename),
                content);
        }

        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        TftpClient client = new TftpClient(null);
        TftpTransferResult result;
        byte[] transferredContent = [];
        if (direction == TftpTransferDirection.Read)
        {
            TftpReadRequest request = ReadRequest(
                harness.ListenerEndPoint,
                filename,
                TftpTransferMode.Octet,
                optionLevel);
            using MemoryStream destination = new MemoryStream();
            result = client.DownloadToStream(
                request,
                destination,
                CancellationToken.None);
            transferredContent = destination.ToArray();
        }
        else
        {
            TftpWriteRequest request = WriteRequest(
                harness.ListenerEndPoint,
                filename,
                TftpTransferMode.Octet,
                optionLevel,
                content.Length);
            using MemoryStream source = new MemoryStream(content);
            result = client.UploadFromStream(
                request,
                source,
                CancellationToken.None);
        }

        harness.WaitForCompletedTransfers(1);
        if (direction == TftpTransferDirection.Write)
        {
            transferredContent = File.ReadAllBytes(
                Path.Combine(directory.RootPath, filename));
        }

        harness.StopGracefully();
        int expectedOptionCount = 0;
        int expectedBlockSize = TftpProtocolValues.DefaultBlockSize;
        if (optionLevel == 1)
        {
            expectedOptionCount = 1;
            expectedBlockSize = 8;
        }
        else if (optionLevel == 2)
        {
            expectedOptionCount = 3;
            expectedBlockSize = 8;
        }

        Assert.Multiple(() =>
        {
            Assert.That(transferredContent, Is.EqualTo(content));
            Assert.That(result.Direction, Is.EqualTo(direction));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(expectedBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(1));
            Assert.That(result.NegotiatedOptions,
                Has.Count.EqualTo(expectedOptionCount));
            Assert.That(result.HasTransferSize,
                Is.EqualTo(optionLevel == 2));
            Assert.That(result.TransferSize,
                Is.EqualTo(optionLevel == 2 ? content.Length : 0));
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RoundTripsNetasciiByteForByteAcrossBlockBoundaries()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content =
            [65, 66, 67, 68, 69, 70, 71, 10, 72, 13, 73, 13, 10];
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "download.txt"),
            content);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        TftpClient client = new TftpClient(null);
        TftpReadRequest readRequest = ReadRequest(
            harness.ListenerEndPoint,
            "download.txt",
            TftpTransferMode.Netascii,
            1);
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult readResult = client.DownloadToStream(
            readRequest,
            destination,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        TftpWriteRequest writeRequest = WriteRequest(
            harness.ListenerEndPoint,
            "upload.txt",
            TftpTransferMode.Netascii,
            1,
            content.Length);
        using MemoryStream source = new MemoryStream(content);
        TftpTransferResult writeResult = client.UploadFromStream(
            writeRequest,
            source,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(2);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "upload.txt")),
                Is.EqualTo(content));
            Assert.That(readResult.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(writeResult.Mode,
                Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(readResult.BytesTransferred,
                Is.EqualTo(content.Length));
            Assert.That(writeResult.BytesTransferred,
                Is.EqualTo(content.Length));
            Assert.That(readResult.EncodedBytesTransferred,
                Is.GreaterThan(content.Length));
            Assert.That(writeResult.EncodedBytesTransferred,
                Is.EqualTo(readResult.EncodedBytesTransferred));
        });
    }

    [Test]
    public void TransfersBothDirectionsOverIpv6WhenSupported()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are unavailable on this host.");
        }

        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = Content(33);
        byte[] uploadContent = Content(47);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "ipv6-download.bin"),
            downloadContent);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.IPv6Loopback);
        TftpClient client = new TftpClient(null);
        TftpReadRequest readRequest = ReadRequest(
            harness.ListenerEndPoint,
            "ipv6-download.bin",
            TftpTransferMode.Octet,
            2);
        using MemoryStream destination = new MemoryStream();
        TftpTransferResult readResult = client.DownloadToStream(
            readRequest,
            destination,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        TftpWriteRequest writeRequest = WriteRequest(
            harness.ListenerEndPoint,
            "ipv6-upload.bin",
            TftpTransferMode.Octet,
            2,
            uploadContent.Length);
        using MemoryStream source = new MemoryStream(uploadContent);
        TftpTransferResult writeResult = client.UploadFromStream(
            writeRequest,
            source,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(2);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(downloadContent));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "ipv6-upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(readResult.FinalEndpoint.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(writeResult.FinalEndpoint.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(readResult.NegotiatedOptions, Has.Count.EqualTo(3));
            Assert.That(writeResult.NegotiatedOptions, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void TransfersBothDirectionsByteForByteAcrossBlockRollover()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        int contentLength = TftpProtocolValues.MinimumBlockSize * 65536;
        byte[] content = Content(contentLength);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "rollover-download.bin"),
            content);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        TftpClient client = new TftpClient(null);
        TftpReadRequest readRequest = ReadRequest(
            harness.ListenerEndPoint,
            "rollover-download.bin",
            TftpTransferMode.Octet,
            1);
        readRequest.Options.MaximumTransferDurationSeconds = 60;
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult readResult = client.DownloadToStream(
            readRequest,
            destination,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        TftpWriteRequest writeRequest = WriteRequest(
            harness.ListenerEndPoint,
            "rollover-upload.bin",
            TftpTransferMode.Octet,
            1,
            content.Length);
        writeRequest.Options.MaximumTransferDurationSeconds = 60;
        using MemoryStream source = new MemoryStream(content);
        TftpTransferResult writeResult = client.UploadFromStream(
            writeRequest,
            source,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(2);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "rollover-upload.bin")),
                Is.EqualTo(content));
            Assert.That(readResult.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(writeResult.BytesTransferred,
                Is.EqualTo(contentLength));
            Assert.That(readResult.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.MinimumBlockSize));
            Assert.That(writeResult.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.MinimumBlockSize));
        });
    }

    private static TftpReadRequest ReadRequest(
        IPEndPoint serverEndPoint,
        string filename,
        TftpTransferMode mode,
        int optionLevel)
    {
        TftpReadRequest request = new TftpReadRequest();
        SetEndpoint(request.Endpoint, serverEndPoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions(mode, optionLevel);
        return request;
    }

    private static TftpWriteRequest WriteRequest(
        IPEndPoint serverEndPoint,
        string filename,
        TftpTransferMode mode,
        int optionLevel,
        long sourceSize)
    {
        TftpWriteRequest request = new TftpWriteRequest();
        SetEndpoint(request.Endpoint, serverEndPoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions(mode, optionLevel);
        if (optionLevel == 2)
        {
            request.HasKnownSourceSize = true;
            request.KnownSourceSize = sourceSize;
        }

        return request;
    }

    private static TftpTransferOptions TransferOptions(
        TftpTransferMode mode,
        int optionLevel)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = mode;
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 2;
        options.MaximumTransferDurationSeconds = 30;
        options.DallyDurationSeconds = 0;
        if (optionLevel >= 1)
        {
            options.RequestBlockSize = true;
            options.BlockSize = TftpProtocolValues.MinimumBlockSize;
        }

        if (optionLevel == 2)
        {
            options.RequestTimeout = true;
            options.TimeoutSeconds = 1;
            options.RequestTransferSize = true;
        }

        return options;
    }

    private static void SetEndpoint(
        TftpEndpoint endpoint,
        IPEndPoint serverEndPoint)
    {
        endpoint.Host = serverEndPoint.Address.ToString();
        endpoint.Port = serverEndPoint.Port;
        endpoint.AddressFamily = serverEndPoint.AddressFamily;
        if (serverEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            endpoint.LocalAddress = IPAddress.IPv6Loopback;
        }
        else
        {
            endpoint.LocalAddress = IPAddress.Loopback;
        }
    }

    private static byte[] Content(int length)
    {
        return Enumerable.Range(0, length)
            .Select(value => (byte)(value % 251))
            .ToArray();
    }
}

internal class EndToEndServerHarness : IDisposable
{
    internal TftpServer Server;
    internal Task RunTask;
    internal IPEndPoint ListenerEndPoint;

    internal EndToEndServerHarness(string rootPath, IPAddress address)
    {
        TftpDirectoryStorageOptions directoryOptions =
            new TftpDirectoryStorageOptions();
        directoryOptions.RootPath = rootPath;
        directoryOptions.AllowRead = true;
        directoryOptions.AllowWrite = true;
        directoryOptions.AllowCreate = true;
        directoryOptions.AllowOverwrite = true;
        TftpServerOptions options = new TftpServerOptions();
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = address;
        binding.Port = 0;
        options.Bindings.Add(binding);
        options.DirectoryStorage = directoryOptions;
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 2;
        options.MaximumSessionDurationSeconds = 60;
        options.DallyDurationSeconds = 0;
        Server = new TftpServer(null, null);
        RunTask = Task.Run(() => Server.Run(
            options,
            CancellationToken.None));
        bool started = SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = Server.GetStatus();
            return status.IsRunning && status.BoundEndpoints.Count == 1;
        }, 3000);
        if (!started)
        {
            Server.Stop(TftpServerStopMode.Immediate);
            throw new AssertionException("The end-to-end server did not start.");
        }

        TftpServerBinding listener = Server.GetStatus().BoundEndpoints[0];
        ListenerEndPoint = new IPEndPoint(listener.Address, listener.Port);
    }

    internal void WaitForCompletedTransfers(long expectedCount)
    {
        Assert.That(SpinWait.SpinUntil(
            () => Server.GetStatus().CompletedTransferCount == expectedCount,
            10000),
            Is.True);
    }

    internal void StopGracefully()
    {
        Server.Stop(TftpServerStopMode.Graceful);
        Assert.That(SpinWait.SpinUntil(() => RunTask.IsCompleted, 10000),
            Is.True);
        RunTask.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Server.Stop(TftpServerStopMode.Immediate);
        bool stopped = SpinWait.SpinUntil(() => RunTask.IsCompleted, 10000);
        if (!stopped)
        {
            throw new AssertionException("The end-to-end server did not stop.");
        }

        RunTask.GetAwaiter().GetResult();
    }
}
