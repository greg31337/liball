using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientAbortTransferTests
{
    [Test]
    public void AbortTransfer_without_a_pending_transfer_consumes_the_225_reply()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "230 User logged in\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "225 No transfer in progress\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener));

        FtpCommandResult result = client.AbortTransfer();
        server.GetAwaiter().GetResult();
        string operation = client.State.LastOperation;
        FtpSessionState state = client.GetSessionState();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "USER alice", "ABOR" }));
            Assert.That(result.Command.Kind, Is.EqualTo(FtpCommandKind.Abor));
            Assert.That(result.Replies, Has.Length.EqualTo(1));
            Assert.That(result.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.DataConnectionOpenNoTransfer));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ConnectionClosed, Is.False);
            Assert.That(state, Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(operation, Is.EqualTo("AbortTransfer"));
        });

        client.AbortConnection();
    }

    [Test]
    public void AbortTransfer_consumes_the_RFC_426_then_226_sequence()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "230 User logged in\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "426 Connection closed; transfer aborted\r\n" +
                "226 Abort successful\r\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.AllowPlaintextCredentials = true;
        options.Credentials.UserName = "alice";
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        client.State.Session.State.TransferInProgress = true;
        client.State.Session.State.SessionState = FtpSessionState.Transferring;

        FtpCommandResult result = client.AbortTransfer();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "USER alice", "ABOR" }));
            Assert.That(result.Replies.Select(reply => reply.Code),
                Is.EqualTo(new[]
                {
                    FtpReplyCodes.ConnectionClosedTransferAborted,
                    FtpReplyCodes.ClosingDataConnection
                }));
            Assert.That(result.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.ClosingDataConnection));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(client.State.Session.State.TransferInProgress, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void AbortTransfer_returns_negative_replies_without_closing_the_session()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            ReadCommand(stream);
            WriteText(stream, "230 User logged in\r\n");
            ReadCommand(stream);
            WriteText(stream, "500 ABOR unavailable\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener));

        FtpCommandResult result = client.AbortTransfer();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.SyntaxError));
            Assert.That(result.ConnectionClosed, Is.False);
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void AbortTransfer_closes_on_421_and_rejects_a_disconnected_client()
    {
        FtpClient disconnected = new FtpClient(null);
        FtpProtocolException unavailable = Assert.Throws<FtpProtocolException>(() =>
            disconnected.AbortTransfer())!;

        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            ReadCommand(stream);
            WriteText(stream, "230 User logged in\r\n");
            ReadCommand(stream);
            WriteText(stream, "421 Service closing\r\n");
        });
        FtpClient connected = new FtpClient(null);
        connected.Connect(CreateOptions(listener));

        FtpCommandResult closure = connected.AbortTransfer();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Message, Does.Contain("AbortTransfer"));
            Assert.That(disconnected.State.LastOperation,
                Is.EqualTo("AbortTransfer"));
            Assert.That(closure.Succeeded, Is.False);
            Assert.That(closure.ConnectionClosed, Is.True);
            Assert.That(closure.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.ServiceUnavailable));
            Assert.That(connected.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(connected.State.Connection, Is.Null);
        });
    }

    [Test]
    public void AbortTransfer_rejects_an_unauthenticated_session_without_wire_output()
    {
        using TcpListener listener = StartListener();
        int received = 0;
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            received = stream.ReadByte();
        });
        FtpClientOptions options = CreateOptions(listener);
        options.Credentials.UserName = "";
        FtpClient client = new FtpClient(null);
        client.Connect(options);

        FtpProtocolException failure = Assert.Throws<FtpProtocolException>(() =>
            client.AbortTransfer())!;
        FtpSessionState state = client.GetSessionState();
        client.AbortConnection();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.StartWith("530"));
            Assert.That(state, Is.EqualTo(FtpSessionState.Connected));
            Assert.That(received, Is.EqualTo(-1));
        });
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClientOptions CreateOptions(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.AllowPlaintextCredentials = true;
        options.Credentials.UserName = "alice";
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static string ReadCommand(Stream stream)
    {
        using MemoryStream bytes = new MemoryStream();
        bool carriageReturn = false;
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException(
                    "The client closed before sending the expected command.");
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException(
                        "The client command did not use CRLF.");
                }
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
            if (value == 13)
            {
                carriageReturn = true;
            }
            else
            {
                bytes.WriteByte((byte)value);
            }
        }
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
