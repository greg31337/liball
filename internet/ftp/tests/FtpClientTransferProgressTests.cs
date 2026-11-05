using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientTransferProgressTests
{
    [Test]
    public void Upload_reports_expected_progress_and_isolates_observer_faults()
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
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FaultingObserver observer = new FaultingObserver();
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("seven77"));
        FtpTransferRequest request = CreateUploadRequest(content);
        request.HasExpectedSize = true;
        request.ExpectedSize = 7;
        request.Options.BufferBytes = 2;
        request.Observer = observer;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Progress.HasExpectedBytes, Is.True);
            Assert.That(result.Progress.ExpectedBytes, Is.EqualTo(7));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(7));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(7));
            Assert.That(result.Progress.Completed, Is.True);
            Assert.That(observer.Started, Has.Count.EqualTo(1));
            Assert.That(observer.Started[0].LogicalBytes, Is.Zero);
            Assert.That(observer.Started[0].HasExpectedBytes, Is.True);
            Assert.That(observer.Progress, Has.Count.GreaterThanOrEqualTo(4));
            Assert.That(observer.Progress[^1].Completed, Is.True);
            Assert.That(observer.Completed, Has.Count.EqualTo(1));
            Assert.That(observer.Completed[0], Is.SameAs(result));
        });

        client.AbortConnection();
    }

    [Test]
    public void Download_sends_periodic_NOOP_and_retains_byte_accounting()
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
            SendDownloadWithKeepAlive(stream, dataListener, commands);
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/slow.bin";
        request.Content = content;
        request.HasExpectedSize = true;
        request.ExpectedSize = 1;
        request.Options.VerificationMode = FtpVerificationMode.None;
        request.Options.EnableKeepAlive = true;
        request.Options.KeepAliveIntervalMilliseconds = 25;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "RETR /slow.bin", "NOOP",
            }));
            Assert.That(content.ToArray(), Is.EqualTo(new byte[] { 0x5A }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Progress.ExpectedBytes, Is.EqualTo(1));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(1));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(1));
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type, FtpCommandKind.Retr,
                    FtpCommandKind.Noop,
                }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Observer_cancellation_aborts_upload_and_propagates_cancellation()
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
            ReceiveCanceledUpload(
                stream, dataListener, commands, uploaded);
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        CancelingObserver observer = new CancelingObserver(cancellation);
        using MemoryStream content = new MemoryStream(new byte[64]);
        FtpTransferRequest request = CreateUploadRequest(content);
        request.Options.BufferBytes = 2;
        request.Options.Timeouts.CancellationToken = cancellation.Token;
        request.Observer = observer;

        Assert.Throws<OperationCanceledException>(() => client.Transfer(request));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.IsCancellationRequested, Is.True);
            Assert.That(uploaded.Count, Is.EqualTo(2));
            Assert.That(observer.StartedCount, Is.EqualTo(1));
            Assert.That(observer.ProgressCount, Is.EqualTo(1));
            Assert.That(observer.CompletedCount, Is.Zero);
            Assert.That(content.CanRead, Is.True);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
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

    private static void SendDownloadWithKeepAlive(
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
        Thread.Sleep(75);
        data.GetStream().WriteByte(0x5A);
        data.GetStream().Flush();
        Receive(control, commands, "200 NOOP okay\r\n");
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

    private static void ReceiveCanceledUpload(
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
        byte[] buffer = new byte[16];
        try
        {
            int count = data.GetStream().Read(buffer, 0, buffer.Length);
            while (count != 0)
            {
                int index = 0;
                while (index < count)
                {
                    uploaded.Add(buffer[index]);
                    index++;
                }
                count = data.GetStream().Read(buffer, 0, buffer.Length);
            }
        }
        catch (IOException)
        {
        }
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

    private class FaultingObserver : IFtpTransferObserver
    {
        public List<FtpTransferProgress> Started =
            new List<FtpTransferProgress>();
        public List<FtpTransferProgress> Progress =
            new List<FtpTransferProgress>();
        public List<FtpTransferResult> Completed =
            new List<FtpTransferResult>();

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
            Started.Add(progress);
            throw new InvalidOperationException("started observer failure");
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
            Progress.Add(progress);
            throw new InvalidOperationException("progress observer failure");
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
            Completed.Add(result);
            throw new InvalidOperationException("completed observer failure");
        }
    }

    private class CancelingObserver : IFtpTransferObserver
    {
        public CancellationTokenSource Cancellation;
        public int StartedCount;
        public int ProgressCount;
        public int CompletedCount;

        public CancelingObserver(CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
            StartedCount = 0;
            ProgressCount = 0;
            CompletedCount = 0;
        }

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
            StartedCount++;
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
            ProgressCount++;
            if (progress.LogicalBytes != 0)
            {
                Cancellation.Cancel();
            }
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
            CompletedCount++;
        }
    }
}
