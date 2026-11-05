using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientMetadataMutationOperationTests
{
    [Test]
    public void Advertised_MFMT_CHMOD_and_UMASK_are_formatted_and_executed()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            WriteText(stream, "220 Ready\r\n");
            Receive(
                stream,
                commands,
                "211-Extensions:\r\n" +
                " MFMT\r\n" +
                " SITE CHMOD\r\n" +
                " SITE UMASK\r\n" +
                "211 End\r\n");
            AuthenticateAfterGreeting(stream, commands);
            Receive(stream, commands, "213 Modification time changed\r\n");
            Receive(stream, commands, "200 Permissions changed\r\n");
            Receive(stream, commands, "200 Umask changed\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, true);
        FtpTimestamp timestamp =
            FtpTimestampCodec.ParseTimestampValue("20240229123456.125");

        FtpCommandResult modified =
            client.SetModificationTime("/pub/report.txt", timestamp);
        FtpCommandResult permissions =
            client.SetPermissions("/pub/report.txt", 420);
        FtpCommandResult umask = client.SetUmask(18);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "FEAT", "USER alice", "PASS secret",
                "MFMT 20240229123456.125 /pub/report.txt",
                "SITE CHMOD 644 /pub/report.txt", "SITE UMASK 022",
            }));
            Assert.That(client.State.FeatureState.ModificationTime, Is.True);
            Assert.That(client.State.FeatureState.SiteChmod, Is.True);
            Assert.That(client.State.FeatureState.SiteUmask, Is.True);
            Assert.That(modified.Succeeded, Is.True);
            Assert.That(permissions.Succeeded, Is.True);
            Assert.That(umask.Succeeded, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Negotiated_absence_blocks_optional_metadata_extensions()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            WriteText(stream, "220 Ready\r\n");
            Receive(stream, commands, "211 No extensions\r\n");
            AuthenticateAfterGreeting(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(listener, true);
        server.GetAwaiter().GetResult();
        FtpTimestamp timestamp =
            FtpTimestampCodec.ParseTimestampValue("20260102030405");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                client.SetModificationTime("/file", timestamp));
            Assert.Throws<FtpConfigurationException>(() =>
                client.SetPermissions("/file", 420));
            Assert.Throws<FtpConfigurationException>(() => client.SetUmask(18));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "FEAT", "USER alice", "PASS secret",
            }));
        });

        client.AbortConnection();
    }

    [Test]
    public void Disabled_negotiation_allows_explicit_attempts_and_returns_failures()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            WriteText(stream, "220 Ready\r\n");
            AuthenticateAfterGreeting(stream, commands);
            Receive(stream, commands, "500 MFMT unavailable\r\n");
            Receive(stream, commands, "550 CHMOD denied\r\n");
            Receive(stream, commands, "500 UMASK unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener, false);
        FtpTimestamp unavailable = new FtpTimestamp();
        FtpTimestamp timestamp =
            FtpTimestampCodec.ParseTimestampValue("20260102030405");

        Assert.Throws<FtpProtocolException>(() =>
            client.SetModificationTime("/file", unavailable));
        Assert.Throws<FtpConfigurationException>(() =>
            client.SetModificationTime("bad\npath", timestamp));
        Assert.Throws<FtpConfigurationException>(() =>
            client.SetPermissions("/file", 4096));
        Assert.Throws<FtpConfigurationException>(() => client.SetUmask(512));
        FtpCommandResult modified = client.SetModificationTime("/file", timestamp);
        FtpCommandResult permissions = client.SetPermissions("/file", 511);
        FtpCommandResult umask = client.SetUmask(0);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MFMT 20260102030405 /file",
                "SITE CHMOD 777 /file", "SITE UMASK 000",
            }));
            Assert.That(modified.Succeeded, Is.False);
            Assert.That(modified.FinalReply!.Code, Is.EqualTo(500));
            Assert.That(permissions.Succeeded, Is.False);
            Assert.That(permissions.FinalReply!.Code, Is.EqualTo(550));
            Assert.That(umask.Succeeded, Is.False);
        });

        client.AbortConnection();
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
        bool negotiateFeatures)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = negotiateFeatures;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        FtpAuthenticationResult authentication = client.AuthenticateUser(credentials);
        Assert.That(authentication.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void AuthenticateAfterGreeting(
        NetworkStream stream,
        List<string> commands)
    {
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

    private static void WriteText(NetworkStream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
