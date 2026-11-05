using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ftp.Tests;

public class FtpClientChecksumOperationTests
{
    [TestCase("MD5", FtpCommandKind.Md5, "MD5", "25F9E794323B453885F5181F1B624D0B")]
    [TestCase("SHA-1", FtpCommandKind.Xsha1, "XSHA1", "F7C3BC1D808E04732ADF679965CCC34CA7AE3441")]
    [TestCase("SHA-256", FtpCommandKind.Xsha256, "XSHA256", "15E2B0D3C33891EBB0F1EF609EC419420C20E320CE94C65FBC8C3312448EB225")]
    [TestCase("SHA-512", FtpCommandKind.Xsha512, "XSHA512", "D9E6762DD1C8EAF6D61B3C6192FC408D4D6D5F1176D0C29169BC24E71C3F274AD27FCD5811B313D681F7E55EC02D73D499C95455B6B5BB503ACF574FBA8FFE85")]
    [TestCase("CRC32", FtpCommandKind.Xcrc, "XCRC", "CBF43926")]
    public void VerifyTransfer_uses_each_compatible_checksum_algorithm(
        string algorithm,
        FtpCommandKind commandKind,
        string commandName,
        string checksum)
    {
        byte[] bytes = Encoding.ASCII.GetBytes("123456789");
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands,
                "213 " + checksum.ToLowerInvariant() + " /remote.bin\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.Hash = false;
        using MemoryStream content = new MemoryStream(bytes);
        FtpTransferRequest request = CreateChecksumRequest(
            "/remote.bin", content, algorithm, checksum);

        FtpTransferResult result = client.VerifyTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", commandName + " /remote.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.ChecksumAlgorithm, Is.EqualTo(algorithm));
            Assert.That(result.ActualChecksum, Is.EqualTo(checksum));
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[] { commandKind }));
        });

        client.AbortConnection();
    }

    [Test]
    public void VerifyTransfer_negotiates_HASH_algorithm_and_compares_local_digest()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("checksum content");
        string checksum = Convert.ToHexString(SHA256.HashData(bytes));
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 HASH algorithm selected\r\n");
            Receive(stream, commands,
                "213 SHA-256 0-" + (bytes.Length - 1).ToString() + " " +
                checksum.ToLowerInvariant() + " /remote.bin\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        AdvertiseHash(client, "SHA-1;SHA-256*;");
        using MemoryStream content = new MemoryStream(bytes);
        content.Position = 3;
        FtpTransferRequest request = CreateChecksumRequest(
            "/remote.bin", content, "sha256", checksum.ToLowerInvariant());

        FtpTransferResult result = client.VerifyTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "OPTS HASH SHA-256",
                "HASH /remote.bin",
            }));
            Assert.That(content.Position, Is.EqualTo(3));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.ChecksumAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(result.ExpectedChecksum, Is.EqualTo(checksum));
            Assert.That(result.ActualChecksum, Is.EqualTo(checksum));
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Opts, FtpCommandKind.Hash }));
        });

        client.AbortConnection();
    }

    [Test]
    public void VerifyTransfer_falls_back_to_XMD5_and_detects_local_mismatch()
    {
        byte[] expectedBytes = Encoding.ASCII.GetBytes("expected");
        string expected = Convert.ToHexString(MD5.HashData(expectedBytes));
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 MD5 unavailable\r\n");
            Receive(stream, commands,
                "250 /remote.bin " + expected.ToLowerInvariant() + "\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.Hash = false;
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("different"));
        FtpTransferRequest request = CreateChecksumRequest(
            "/remote.bin", content, "MD5", expected);

        FtpTransferResult result = client.VerifyTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MD5 /remote.bin",
                "XMD5 /remote.bin",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Verified, Is.False);
            Assert.That(result.ActualChecksum, Is.EqualTo(expected));
            Assert.That(result.FailureMessage, Does.Contain("local content"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Download_runs_checksum_verification_after_RETR()
    {
        const string downloaded = "verified download";
        string checksum = Convert.ToHexString(
            SHA1.HashData(Encoding.ASCII.GetBytes(downloaded)));
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(stream, dataListener, commands, downloaded);
            Receive(stream, commands, "200 HASH algorithm selected\r\n");
            Receive(stream, commands,
                "213 SHA-1 0-" + (downloaded.Length - 1).ToString() + " " +
                checksum + " /download.bin\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = CreateChecksumRequest(
            "/download.bin", content, "SHA-1", checksum);
        request.Direction = FtpTransferDirection.Download;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "RETR /download.bin", "OPTS HASH SHA-1", "HASH /download.bin",
            }));
            Assert.That(Encoding.ASCII.GetString(content.ToArray()),
                Is.EqualTo(downloaded));
            Assert.That(content.Position, Is.EqualTo(downloaded.Length));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type, FtpCommandKind.Retr,
                    FtpCommandKind.Opts, FtpCommandKind.Hash,
                }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Atomic_upload_verifies_temporary_path_before_rename()
    {
        const string uploaded = "atomic checksum";
        string checksum = Convert.ToHexString(
            MD5.HashData(Encoding.ASCII.GetBytes(uploaded)));
        string directory = Path.Combine(
            Path.GetTempPath(), "ftp-checksum-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string localPath = Path.Combine(directory, "source.bin");
        File.WriteAllText(localPath, uploaded);
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveUpload(stream, dataListener, commands);
            Receive(stream, commands, "200 HASH algorithm selected\r\n");
            string temporaryPath = GetArgument(commands[4]);
            Receive(stream, commands,
                "213 MD5 0-" + (uploaded.Length - 1).ToString() + " " +
                checksum + " " + temporaryPath + "\r\n");
            Receive(stream, commands, "350 Ready for destination\r\n");
            Receive(stream, commands, "250 Rename complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/incoming/file.bin";
        request.LocalPath = localPath;
        request.Options.VerificationMode = FtpVerificationMode.Checksum;
        request.Options.ExpectedChecksumAlgorithm = "MD5";
        request.Options.ExpectedChecksum = checksum;
        request.Direction = FtpTransferDirection.Upload;
        request.Options.AtomicUpload = true;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();
        string temporaryPath = GetArgument(commands[4]);

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(4), Is.EqualTo(new[]
            {
                "STOR " + temporaryPath,
                "OPTS HASH MD5",
                "HASH " + temporaryPath,
                "RNFR " + temporaryPath,
                "RNTO /incoming/file.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Verified, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void VerifyTransfer_rejects_malformed_HASH_and_normalizes_vendor_digest()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("strict hash reply");
        string checksum = Convert.ToHexString(SHA256.HashData(bytes));
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 SHA-256\r\n");
            Receive(stream, commands,
                "213 SHA-256 " + checksum + " /remote.bin\r\n");
            Receive(stream, commands,
                "250 " + checksum.ToLowerInvariant() + " /remote.bin\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        AdvertiseHash(client, "SHA-256*;");
        using MemoryStream content = new MemoryStream(bytes);
        FtpTransferRequest request = CreateChecksumRequest(
            "/remote.bin", content, "SHA-256", checksum);

        FtpTransferResult result = client.VerifyTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "OPTS HASH SHA-256",
                "HASH /remote.bin", "XSHA256 /remote.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.ActualChecksum, Is.EqualTo(checksum));
        });

        client.AbortConnection();
    }

    [Test]
    public void VerifyTransfer_returns_an_unverified_result_when_the_algorithm_is_unavailable()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("unsupported remote algorithm");
        string checksum = Convert.ToHexString(SHA512.HashData(bytes));
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "502 XSHA512 unsupported\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        AdvertiseHash(client, "SHA-1*;");
        using MemoryStream content = new MemoryStream(bytes);
        FtpTransferRequest request = CreateChecksumRequest(
            "/remote.bin", content, "SHA-512", checksum);

        FtpTransferResult result = client.VerifyTransfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "XSHA512 /remote.bin",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.False);
            Assert.That(result.ActualChecksum, Is.Null);
            Assert.That(result.FailureMessage,
                Does.Contain("could not provide the requested remote checksum"));
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Xsha512 }));
        });

        client.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Download_checksum_mismatch_applies_cleanup_without_retrying(
        bool cleanupPartial)
    {
        const string downloaded = "mismatched download";
        string expected = Convert.ToHexString(
            SHA256.HashData(Encoding.ASCII.GetBytes(downloaded)));
        string remote = new string('0', expected.Length);
        string directory = Path.Combine(
            Path.GetTempPath(), "ftp-checksum-mismatch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string localPath = Path.Combine(directory, "download.bin");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(stream, dataListener, commands, downloaded);
            Receive(stream, commands, "200 HASH algorithm selected\r\n");
            Receive(stream, commands,
                "213 SHA-256 0-" + (downloaded.Length - 1).ToString() + " " +
                remote + " /download.bin\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        AdvertiseHash(client, "SHA-256*;");
        client.State.FeatureState.ExtendedPassive = true;
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Download;
        request.RemotePath = "/download.bin";
        request.LocalPath = localPath;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.CleanupPartialOnFailure = cleanupPartial;
        request.Options.VerificationMode = FtpVerificationMode.Checksum;
        request.Options.ExpectedChecksumAlgorithm = "SHA-256";
        request.Options.ExpectedChecksum = expected;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Count(item => item == "RETR /download.bin"),
                Is.EqualTo(1));
            Assert.That(commands.Count(item => item == "HASH /download.bin"),
                Is.EqualTo(1));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.False);
            Assert.That(result.FailureMessage,
                Does.Contain("remote checksum does not match"));
            Assert.That(File.Exists(localPath), Is.EqualTo(!cleanupPartial));
            if (!cleanupPartial)
            {
                Assert.That(File.ReadAllText(localPath), Is.EqualTo(downloaded));
            }
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    private static FtpTransferRequest CreateChecksumRequest(
        string remotePath,
        Stream content,
        string algorithm,
        string checksum)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = remotePath;
        request.Content = content;
        request.Options.VerificationMode = FtpVerificationMode.Checksum;
        request.Options.ExpectedChecksumAlgorithm = algorithm;
        request.Options.ExpectedChecksum = checksum;
        return request;
    }

    private static void AdvertiseHash(FtpClient client, string parameters)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = FtpFeatureNames.Hash;
        feature.Parameters = parameters;
        feature.Support = FtpFeatureSupport.Supported;
        client.State.Features.Features = new[] { feature };
        client.State.Features.Negotiated = true;
        client.State.FeatureState.Hash = true;
    }

    private static void SendDownload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        string content)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(control));
        WriteText(control, "150 Opening data\r\n");
        WriteText(data.GetStream(), content);
        data.Client.Shutdown(SocketShutdown.Send);
        WriteText(control, "226 Transfer complete\r\n");
    }

    private static void ReceiveUpload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(control));
        WriteText(control, "150 Opening data\r\n");
        data.GetStream().CopyTo(Stream.Null);
        WriteText(control, "226 Transfer complete\r\n");
    }

    private static string GetArgument(string command)
    {
        int separator = command.IndexOf(' ');
        Assert.That(separator, Is.GreaterThan(0));
        return command.Substring(separator + 1);
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static int GetPort(TcpListener listener)
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static FtpClient ConnectAuthenticated(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        FtpAuthenticationResult authentication =
            client.AuthenticateUser(credentials);
        Assert.That(authentication.Status,
            Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(
        NetworkStream stream,
        List<string> commands)
    {
        WriteText(stream, "220 Ready\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void Receive(
        NetworkStream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, reply);
    }

    private static string ReadCommand(Stream stream)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }
            if (value == '\n')
            {
                break;
            }
            if (value != '\r')
            {
                bytes.Add((byte)value);
            }
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
