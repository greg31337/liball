using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientFaultOperationTests
{
    [Test]
    public void Malformed_authenticated_reply_fails_closed_without_command_replay()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 malformed\n");
        });
        FtpClient client = ConnectAuthenticated(listener, 3);

        Assert.Throws<FtpProtocolException>(() => client.SendNoOperation());
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "NOOP",
            }));
            Assert.That(commands.Count(command => command == "NOOP"), Is.EqualTo(1));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Premature_control_close_during_STOR_does_not_replay_upload()
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
            commands.Add(ReadCommand(stream));
            WriteText(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
        });
        FtpClient client = ConnectAuthenticated(controlListener, 3);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("must-not-replay"));
        FtpTransferRequest request = CreateUploadRequest(content);

        Assert.Throws<FtpTransferException>(() => client.Transfer(request));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "STOR /upload.bin",
            }));
            Assert.That(commands.Count(command =>
                command == "STOR /upload.bin"), Is.EqualTo(1));
            Assert.That(content.Position, Is.Zero);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Premature_data_reset_fails_download_and_closes_both_channels()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        using ManualResetEventSlim releaseServer = new ManualResetEventSlim(false);
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
            WriteText(stream, "150 Opening data\r\n");
            WriteText(data.GetStream(), "part");
            data.Client.LingerState = new LingerOption(true, 0);
            data.Close();
            releaseServer.Wait();
        });
        FtpClient client = ConnectAuthenticated(controlListener, 3);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/download.bin";
        request.Content = content;
        request.HasExpectedSize = true;
        request.ExpectedSize = 1024;
        request.Options.VerificationMode = FtpVerificationMode.None;
        request.Options.Timeouts.ControlReadTimeoutMilliseconds = 250;
        request.Options.Timeouts.IdleTimeoutMilliseconds = 1000;

        FtpTransferException? error;
        try
        {
            error = Assert.Throws<FtpTransferException>(() =>
                client.Transfer(request));
        }
        finally
        {
            releaseServer.Set();
        }
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Not.Null);
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "RETR /download.bin",
            }));
            Assert.That(commands.Count(command =>
                command == "RETR /download.bin"), Is.EqualTo(1));
            Assert.That(content.Length, Is.LessThan(1024));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Completion_cancellation_wins_the_final_reply_race_without_replaying_STOR()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<byte> uploaded = new List<byte>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveUpload(stream, dataListener, commands, uploaded);
        });
        FtpClient client = ConnectAuthenticated(controlListener, 3);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CompletionCancelingObserver observer =
            new CompletionCancelingObserver(cancellation);
        byte[] bytes = Encoding.ASCII.GetBytes("complete upload");
        using MemoryStream content = new MemoryStream(bytes);
        FtpTransferRequest request = CreateUploadRequest(content);
        request.Options.BufferBytes = 2;
        request.Options.Timeouts.CancellationToken = cancellation.Token;
        request.Observer = observer;

        Assert.Throws<OperationCanceledException>(() => client.Transfer(request));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.IsCancellationRequested, Is.True);
            Assert.That(observer.CompletedProgressCount, Is.EqualTo(1));
            Assert.That(observer.CompletedResultCount, Is.Zero);
            Assert.That(uploaded.ToArray(), Is.EqualTo(bytes));
            Assert.That(commands.Count(command =>
                command == "STOR /upload.bin"), Is.EqualTo(1));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    private static FtpTransferRequest CreateUploadRequest(Stream content)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/upload.bin";
        request.Content = content;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static void ReceiveUpload(
        NetworkStream control,
        TcpListener dataListener,
        List<string> commands,
        List<byte> uploaded)
    {
        commands.Add(ReadCommand(control));
        WriteText(control, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(control));
        WriteText(control, "150 Opening data\r\n");
        int value = data.GetStream().ReadByte();
        while (value >= 0)
        {
            uploaded.Add((byte)value);
            value = data.GetStream().ReadByte();
        }
        WriteText(control, "226 Transfer complete\r\n");
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

    private static FtpClient ConnectAuthenticated(
        TcpListener listener,
        int retryCount)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        options.RetryCount = retryCount;
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

    private class CompletionCancelingObserver : IFtpTransferObserver
    {
        public CancellationTokenSource Cancellation;
        public int CompletedProgressCount;
        public int CompletedResultCount;

        public CompletionCancelingObserver(CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
            CompletedProgressCount = 0;
            CompletedResultCount = 0;
        }

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
            if (progress.Completed)
            {
                CompletedProgressCount++;
                Cancellation.Cancel();
            }
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
            CompletedResultCount++;
        }
    }
}
