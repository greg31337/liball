using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientMachineListingTests
{
    [Test]
    public void PRET_announces_the_exact_MLSD_before_passive_setup()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            commands.Add(ReadCommand(controlStream));
            WriteReply(controlStream, "200 PRET accepted\r\n");
            commands.Add(ReadCommand(controlStream));
            WriteReply(controlStream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteReply(controlStream, "150 Opening machine listing\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteReply(dataStream, "Type=file;Size=1; item.txt\r\n");
            }
            WriteReply(controlStream, "226 Complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener, true, true);

        FtpListing listing = client.ListMachineDirectory("/folder name");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret",
                "PRET MLSD /folder name", "EPSV", "MLSD /folder name",
            }));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.Entries.Select(item => item.Name),
                Is.EqualTo(new[] { "item.txt" }));
        });

        client.AbortConnection();
    }

    [Test]
    public void MLST_returns_one_entry_with_selected_and_unknown_raw_facts()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            commands.Add(ReadCommand(stream));
            WriteReply(stream,
                "250-Listing /reports/report.txt\r\n" +
                " Type=file;Size=42;Modify=20240229123456;X.Owner=Alice; /reports/report.txt\r\n" +
                "250 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);

        FtpPathProbeResult result = client.GetMetadata("/reports/report.txt");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MLST /reports/report.txt",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Commands, Has.Length.EqualTo(1));
            Assert.That(result.Commands[0].Succeeded, Is.True);
            Assert.That(result.Entry, Is.Not.Null);
            Assert.That(result.Entry!.Name, Is.EqualTo("/reports/report.txt"));
            Assert.That(result.Entry.FullPath, Is.EqualTo("/reports/report.txt"));
            Assert.That(result.Entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(result.Entry.Size, Is.EqualTo(42));
            Assert.That(result.Entry.Facts.Select(fact => fact.Name),
                Is.EqualTo(new[] { "type", "size", "modify", "x.owner" }));
            Assert.That(result.Entry.Facts[3].Standard, Is.False);
            Assert.That(result.Entry.Facts[3].RawText, Is.EqualTo("X.Owner=Alice"));
            Assert.That(result.Entry.RawLine, Is.EqualTo(
                " Type=file;Size=42;Modify=20240229123456;X.Owner=Alice; /reports/report.txt"));
        });

        client.AbortConnection();
    }

    [Test]
    public void MLSD_reads_the_data_connection_and_preserves_entries_and_raw_lines()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream controlStream = control.GetStream();
            Authenticate(controlStream, commands);
            commands.Add(ReadCommand(controlStream));
            WriteReply(controlStream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(controlStream));
            WriteReply(controlStream, "150 Opening machine listing\r\n");
            using (NetworkStream dataStream = data.GetStream())
            {
                WriteReply(dataStream,
                    "Type=file;Size=12;Modify=20240102030405;X.Owner=Alice; report.txt\r\n" +
                    "Type=dir;Unique=folder-7; archive\r\n");
            }
            WriteReply(controlStream, "226 Machine listing complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.FeatureState.MlstFacts = new[] { "type", "size", "modify" };

        FtpListing listing = client.ListMachineDirectory("/reports");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "MLSD /reports",
            }));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.MachineReadable, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("MLSD"));
            Assert.That(listing.CommandResult!.Succeeded, Is.True);
            Assert.That(listing.CommandResult.PreliminaryReply!.Code, Is.EqualTo(150));
            Assert.That(listing.CommandResult.FinalReply!.Code, Is.EqualTo(226));
            Assert.That(listing.Entries, Has.Length.EqualTo(2));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("report.txt"));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(12));
            Assert.That(listing.Entries[0].Facts.Select(fact => fact.Name),
                Is.EqualTo(new[] { "type", "size", "modify", "x.owner" }));
            Assert.That(listing.Entries[0].Facts[3].RawText,
                Is.EqualTo("X.Owner=Alice"));
            Assert.That(listing.Entries[1].Type, Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.RawLines, Is.EqualTo(new[]
            {
                "Type=file;Size=12;Modify=20240102030405;X.Owner=Alice; report.txt",
                "Type=dir;Unique=folder-7; archive",
            }));
            Assert.That(client.State.FeatureState.MlstFacts,
                Is.EqualTo(new[] { "type", "size", "modify" }));
            Assert.That(client.State.DataStrategy, Is.Null);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Failed_MLSD_applies_the_configured_fallback_policy(
        bool allowFallback,
        bool expectedMachineListing)
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            commands.Add(ReadCommand(stream));
            WriteReply(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
            WriteReply(stream, "500 MLSD is unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener, allowFallback);
        client.State.FeatureState.MachineListing = true;

        FtpListing listing = client.ListMachineDirectory("");
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "MLSD",
            }));
            Assert.That(listing.Complete, Is.False);
            Assert.That(listing.Entries, Is.Empty);
            Assert.That(listing.RawLines, Is.Empty);
            Assert.That(listing.CommandResult!.Succeeded, Is.False);
            Assert.That(listing.CommandResult.FinalReply!.Code, Is.EqualTo(500));
            Assert.That(listing.FailureMessage, Does.Contain("reply 500"));
            Assert.That(client.State.FeatureState.MachineListing,
                Is.EqualTo(expectedMachineListing));
            Assert.That(client.State.DataStrategy, Is.Null);
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
        bool allowFallback = true,
        bool enablePret = false)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.EnablePret = enablePret;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        options.Quirks.AllowMlsdFallback = allowFallback;
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
        WriteReply(stream, "220 Ready\r\n");
        commands.Add(ReadCommand(stream));
        WriteReply(stream, "331 Password required\r\n");
        commands.Add(ReadCommand(stream));
        WriteReply(stream, "230 Logged in\r\n");
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
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static void WriteReply(NetworkStream stream, string reply)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(reply);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
