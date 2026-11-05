using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientQuirkTests
{
    [Test]
    public void Lenient_reply_quirk_accepts_a_compact_LF_only_greeting()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220ready\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.Quirks.LenientReplies = true;
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Options!.StrictReplyParsing, Is.False);
            Assert.That(client.State.Connection!.Greeting!.Code,
                Is.EqualTo(FtpReplyCodes.ServiceReady));
            Assert.That(client.State.Connection.Greeting.Text, Is.EqualTo("ready"));
        });
        client.AbortConnection();
    }

    [Test]
    public void Lenient_reply_quirk_accepts_a_common_extended_FEAT_terminator()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            string command = ReadCommand(stream);
            if (command != "FEAT")
            {
                throw new InvalidDataException(
                    "The client did not request its feature inventory.");
            }
            WriteText(stream,
                "211-Features:\r\n SIZE\r\n211 End FEAT.\r\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.NegotiateFeatures = true;
        options.Quirks.LenientReplies = true;
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Features.Negotiated, Is.True);
            Assert.That(client.State.Features.Features.Select(
                feature => feature.Name), Is.EqualTo(new[] { "SIZE" }));
            Assert.That(client.State.FeatureState.FileSize, Is.True);
        });
        client.AbortConnection();
    }

    [Test]
    public void Broken_feature_claims_remain_diagnostic_but_are_not_selected()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream,
                "211-Features:\r\n" +
                " UTF8\r\n" +
                " MLST Type*;Size;Modify;\r\n" +
                " SIZE\r\n" +
                " MDTM\r\n" +
                "211 End\r\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.NegotiateFeatures = true;
        options.Quirks.IgnoreUtf8FeatureClaim = true;
        options.Quirks.IgnoreMlstFeatureClaim = true;
        options.Quirks.IgnoreSizeFeatureClaim = true;
        options.Quirks.IgnoreMdtmFeatureClaim = true;
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "FEAT" }));
            Assert.That(client.State.Features.Features.Select(feature => feature.Name),
                Is.EqualTo(new[] { "UTF8", "MLST", "SIZE", "MDTM" }));
            Assert.That(client.State.FeatureState.Utf8Advertised, Is.False);
            Assert.That(client.State.FeatureState.MlstAdvertised, Is.False);
            Assert.That(client.State.FeatureState.MachineListing, Is.False);
            Assert.That(client.State.FeatureState.FileSize, Is.False);
            Assert.That(client.State.FeatureState.ModificationTimeQuery, Is.False);
            Assert.That(client.State.Connection!.Utf8Enabled, Is.False);
        });
        client.AbortConnection();
    }

    [Test]
    public void Compatibility_commands_run_immediately_before_and_after_AUTH()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            Receive(stream, commands, "200 CLNT accepted\r\n");
            Receive(stream, commands, "502 TLS unavailable\r\n");
            Receive(stream, commands, "200 NOOP accepted\r\n");
        });
        FtpClientOptions options = CreateOptions(listener);
        options.SecurityMode = FtpSecurityMode.ExplicitTlsOpportunistic;
        options.AllowPlaintextCredentials = true;
        FtpCommand before = new FtpCommand();
        before.Kind = FtpCommandKind.Clnt;
        before.Argument = "quirk-client";
        FtpCommand after = new FtpCommand();
        after.Kind = FtpCommandKind.Noop;
        options.Quirks.CommandsBeforeAuthorization = new[] { before };
        options.Quirks.CommandsAfterAuthorization = new[] { after };
        FtpClient client = new FtpClient(new AcceptingCertificateValidator());

        client.Connect(options);
        before.Argument = "mutated";
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "CLNT quirk-client", "AUTH TLS", "NOOP",
            }));
            Assert.That(client.State.Connection!.PlaintextFallback, Is.True);
            Assert.That(client.State.Options!.Quirks.CommandsBeforeAuthorization[0].Argument,
                Is.EqualTo("quirk-client"));
            Assert.That(client.State.Options.Quirks.CommandsBeforeAuthorization,
                Is.Not.SameAs(options.Quirks.CommandsBeforeAuthorization));
        });
        client.AbortConnection();
    }

    [Test]
    public void Passive_address_and_operation_fallback_quirks_apply_only_when_enabled()
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = "127.0.0.1";
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.DataConnection.RequireDataPeerMatch = false;
        options.DataConnection.RejectPassiveAddressMismatch = false;
        options.Quirks.HonorPassiveAdvertisedAddress = true;
        options.Quirks.AllowHistoricAliasFallback = true;
        FtpClientOptions snapshot = FtpClientOptionsSnapshot.CreateValidated(
            options, null);
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, snapshot.DataConnection);
        FtpEndpoint advertised = new FtpEndpoint();
        advertised.Host = "127.0.0.2";
        advertised.Port = 50000;
        advertised.AddressFamily = FtpAddressFamily.IPv4;
        FtpClientQuirkPolicy enabled = new FtpClientQuirkPolicy(snapshot.Quirks);
        FtpReply failure = CreateReply(550, "operation unavailable");
        FtpReply success = CreateReply(200, "operation okay");
        FtpClientQuirkOptions disabledOptions =
            FtpClientOptionsSnapshot.CopyQuirks(snapshot.Quirks);
        disabledOptions.AllowMlsdFallback = false;
        disabledOptions.AllowSizeFallback = false;
        disabledOptions.AllowMdtmFallback = false;
        FtpClientQuirkPolicy disabled =
            new FtpClientQuirkPolicy(disabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.DataConnection.IgnorePassiveAdvertisedAddress,
                Is.False);
            Assert.That(connector.SelectPassiveAddress(advertised),
                Is.EqualTo(IPAddress.Parse("127.0.0.2")));
            Assert.That(enabled.ShouldFallbackFromMlsd(failure), Is.True);
            Assert.That(enabled.ShouldFallbackFromSize(failure), Is.True);
            Assert.That(enabled.ShouldFallbackFromMdtm(failure), Is.True);
            Assert.That(enabled.ShouldFallbackToHistoricAlias(
                FtpCommandKind.Cwd,
                CreateReply(502, "unsupported")), Is.True);
            Assert.That(enabled.ShouldFallbackToHistoricAlias(
                FtpCommandKind.Cwd, failure), Is.False);
            Assert.That(enabled.ShouldFallbackToHistoricAlias(
                FtpCommandKind.Dele,
                CreateReply(502, "unsupported")), Is.False);
            Assert.That(snapshot.Quirks.AllowHistoricAliasFallback, Is.True);
            Assert.That(enabled.ShouldFallbackFromMlsd(success), Is.False);
            Assert.That(disabled.ShouldFallbackFromMlsd(failure), Is.False);
            Assert.That(disabled.ShouldFallbackFromSize(failure), Is.False);
            Assert.That(disabled.ShouldFallbackFromMdtm(failure), Is.False);
        });
    }

    [Test]
    public void AUTH_compatibility_commands_reject_security_and_transfer_operations()
    {
        FtpClientOptions authentication = CreateOptionsWithoutListener();
        FtpCommand auth = new FtpCommand();
        auth.Kind = FtpCommandKind.Auth;
        auth.Argument = "TLS";
        authentication.Quirks.CommandsBeforeAuthorization = new[] { auth };
        FtpClientOptions transfer = CreateOptionsWithoutListener();
        FtpCommand retr = new FtpCommand();
        retr.Kind = FtpCommandKind.Retr;
        retr.Argument = "/file";
        transfer.Quirks.CommandsAfterAuthorization = new[] { retr };

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                FtpClientOptionsSnapshot.CreateValidated(authentication, null));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpClientOptionsSnapshot.CreateValidated(transfer, null));
        });
    }

    private static FtpClientOptions CreateOptions(TcpListener listener)
    {
        FtpClientOptions options = CreateOptionsWithoutListener();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return options;
    }

    private static FtpClientOptions CreateOptionsWithoutListener()
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = FtpPorts.Control;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static void Receive(
        Stream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, reply);
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
                throw new EndOfStreamException();
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException();
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

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        FtpReplyClassifier.ApplyClassification(reply);
        return reply;
    }

    private sealed class AcceptingCertificateValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            FtpCertificateValidationResult result =
                new FtpCertificateValidationResult();
            result.Handled = true;
            result.Accepted = true;
            return result;
        }
    }
}
