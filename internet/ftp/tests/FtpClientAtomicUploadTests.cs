using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

namespace ftp.Tests;

public class FtpClientAtomicUploadTests
{
    [TestCase(FtpVerificationMode.ModifiedTime, false)]
    [TestCase(FtpVerificationMode.SizeAndModifiedTime, true)]
    public void Atomic_upload_applies_the_selected_size_and_time_policy(
        FtpVerificationMode mode,
        bool expectSize)
    {
        string directory = Path.Combine(
            Path.GetTempPath(), "ftp-atomic-time-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string localPath = Path.Combine(directory, "source.bin");
        File.WriteAllText(localPath, "complete");
        File.SetLastWriteTimeUtc(
            localPath, new DateTime(2024, 8, 30, 12, 34, 56, DateTimeKind.Utc));
        DateTime modified = File.GetLastWriteTimeUtc(localPath);
        string wireTime = modified.ToString(
            "yyyyMMddHHmmss.fffffff", CultureInfo.InvariantCulture);
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
            if (expectSize)
            {
                Receive(stream, commands, "213 8\r\n");
            }
            Receive(stream, commands, "213 " + wireTime + "\r\n");
            Receive(stream, commands, "350 Ready for destination\r\n");
            Receive(stream, commands, "250 Rename complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/incoming/file.bin";
        request.LocalPath = localPath;
        request.Options.AtomicUpload = true;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.VerificationMode = mode;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        string temporaryPath = GetArgument(commands[4]);
        List<string> expected = new List<string>();
        expected.Add("STOR " + temporaryPath);
        if (expectSize)
        {
            expected.Add("SIZE " + temporaryPath);
        }
        expected.Add("MDTM " + temporaryPath);
        expected.Add("RNFR " + temporaryPath);
        expected.Add("RNTO /incoming/file.bin");
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(4), Is.EqualTo(expected));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.VerificationMode, Is.EqualTo(mode));
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.True);
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Atomic_upload_verifies_temporary_size_before_rename_commit()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(stream, dataListener, commands));
            Receive(stream, commands, "213 8\r\n");
            Receive(stream, commands, "350 Ready for destination\r\n");
            Receive(stream, commands, "250 Rename complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("complete"));
        FtpTransferRequest request = CreateAtomicRequest(content);

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        string temporaryPath = GetArgument(commands[4]);
        Assert.Multiple(() =>
        {
            Assert.That(temporaryPath,
                Does.Match("^/incoming/\\.file\\.bin\\.upload-[0-9a-f]{32}\\.tmp$"));
            Assert.That(temporaryPath, Is.Not.EqualTo(request.RemotePath));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "STOR " + temporaryPath, "SIZE " + temporaryPath,
                "RNFR " + temporaryPath, "RNTO /incoming/file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "complete" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type, FtpCommandKind.Stor,
                    FtpCommandKind.Size, FtpCommandKind.Rnfr,
                    FtpCommandKind.Rnto,
                }));
        });

        client.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Atomic_size_verification_failure_applies_the_cleanup_policy(
        bool cleanupPartial)
    {
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
            Receive(stream, commands, "213 7\r\n");
            if (cleanupPartial)
            {
                Receive(stream, commands, "250 Temporary file deleted\r\n");
            }
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("complete"));
        FtpTransferRequest request = CreateAtomicRequest(content);
        request.Options.CleanupPartialOnFailure = cleanupPartial;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        string temporaryPath = GetArgument(commands[4]);
        List<string> expected = new List<string>();
        expected.Add("STOR " + temporaryPath);
        expected.Add("SIZE " + temporaryPath);
        if (cleanupPartial)
        {
            expected.Add("DELE " + temporaryPath);
        }
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(4), Is.EqualTo(expected));
            Assert.That(commands.Any(item => item.StartsWith("RNFR ")), Is.False);
            Assert.That(commands.Any(item => item.StartsWith("DELE ")),
                Is.EqualTo(cleanupPartial));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("size verification"));
            Assert.That(result.Commands[^1].Command.Kind,
                Is.EqualTo(cleanupPartial ?
                    FtpCommandKind.Dele : FtpCommandKind.Size));
        });

        client.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Atomic_rename_failure_applies_the_cleanup_policy(
        bool cleanupPartial)
    {
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
            Receive(stream, commands, "213 8\r\n");
            Receive(stream, commands, "350 Ready for destination\r\n");
            Receive(stream, commands, "553 Destination rejected\r\n");
            if (cleanupPartial)
            {
                Receive(stream, commands, "250 Temporary file deleted\r\n");
            }
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("complete"));
        FtpTransferRequest request = CreateAtomicRequest(content);
        request.Options.CleanupPartialOnFailure = cleanupPartial;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        string temporaryPath = GetArgument(commands[4]);
        List<string> expected = new List<string>();
        expected.Add("SIZE " + temporaryPath);
        expected.Add("RNFR " + temporaryPath);
        expected.Add("RNTO /incoming/file.bin");
        if (cleanupPartial)
        {
            expected.Add("DELE " + temporaryPath);
        }
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(5), Is.EqualTo(expected));
            Assert.That(commands.Any(item => item.StartsWith("DELE ")),
                Is.EqualTo(cleanupPartial));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.VerificationAttempted, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("RNTO failed"));
            Assert.That(result.Commands[^1].Command.Kind,
                Is.EqualTo(cleanupPartial ?
                    FtpCommandKind.Dele : FtpCommandKind.Rnto));
        });

        client.AbortConnection();
    }

    [Test]
    public void Atomic_rename_disconnect_leaves_cleanup_to_recovery()
    {
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
            Receive(stream, commands, "213 8\r\n");
            Receive(stream, commands, "350 Ready for destination\r\n");
            Receive(stream, commands, "421 Service unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("complete"));
        FtpTransferRequest request = CreateAtomicRequest(content);

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        string temporaryPath = GetArgument(commands[4]);
        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(5), Is.EqualTo(new[]
            {
                "SIZE " + temporaryPath, "RNFR " + temporaryPath,
                "RNTO /incoming/file.bin",
            }));
            Assert.That(commands.Any(item => item.StartsWith("DELE ")), Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.FailureMessage, Does.Contain("RNTO failed"));
            Assert.That(result.Commands[^1].Command.Kind,
                Is.EqualTo(FtpCommandKind.Rnto));
            Assert.That(result.Commands[^1].ConnectionClosed, Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Closed));
        });
    }

    private static FtpTransferRequest CreateAtomicRequest(Stream content)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/incoming/file.bin";
        request.Content = content;
        request.Options.AtomicUpload = true;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.VerificationMode = FtpVerificationMode.Size;
        return request;
    }

    private static string ReceiveUpload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands)
    {
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening data\r\n");
        string content;
        using (NetworkStream dataStream = data.GetStream())
        using (MemoryStream buffer = new MemoryStream())
        {
            dataStream.CopyTo(buffer);
            content = Encoding.ASCII.GetString(buffer.ToArray());
        }
        WriteText(controlStream, "226 Transfer complete\r\n");
        return content;
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
        FtpAuthenticationResult authentication = client.AuthenticateUser(credentials);
        Assert.That(authentication.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(NetworkStream stream, List<string> commands)
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

    private static string ReadCommand(NetworkStream stream)
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
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
