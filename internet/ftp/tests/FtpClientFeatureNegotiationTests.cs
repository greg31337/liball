using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientFeatureNegotiationTests
{
    [Test]
    public void Advertised_CLNT_sends_the_configured_identifier_after_FEAT()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "211-Extensions:\r\n CLNT\r\n211 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 Client accepted\r\n");
        });
        FtpClientOptions options = CreateOptions(listener, true);
        options.ClientName = "ftp library/1.0";
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "FEAT", "CLNT ftp library/1.0",
            }));
            Assert.That(client.State.FeatureState.ClientName, Is.True);
            Assert.That(client.State.Options!.ClientName,
                Is.EqualTo("ftp library/1.0"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Client_name_requires_strict_bounded_text()
    {
        using TcpListener listener = StartListener();
        FtpClientOptions options = CreateOptions(listener, true);
        FtpClient client = new FtpClient(null);

        options.ClientName = " surrounding ";
        Assert.Throws<FtpConfigurationException>(() => client.Connect(options));
        options.ClientName = "embedded\tcontrol";
        Assert.Throws<FtpConfigurationException>(() => client.Connect(options));
        options.ClientName = "non-ASCII café";
        Assert.Throws<FtpConfigurationException>(() => client.Connect(options));
        options.ClientName = new string('a', FtpDefaults.MaximumControlLineBytes);
        Assert.Throws<FtpConfigurationException>(() => client.Connect(options));
    }

    [Test]
    public void Disabled_feature_negotiation_explicitly_attempts_configured_CLNT()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "500 CLNT unavailable\r\n");
        });
        FtpClientOptions options = CreateOptions(listener, false);
        options.ClientName = "explicit-client";
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.That(commands, Is.EqualTo(new[] { "CLNT explicit-client" }));
        client.AbortConnection();
    }

    [Test]
    public void Negotiated_CLNT_absence_suppresses_client_identification()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "211-Extensions:\r\n UTF8\r\n211 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 UTF8 accepted\r\n");
        });
        FtpClientOptions options = CreateOptions(listener, true);
        options.ClientName = "feature-directed-client";
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.That(commands, Is.EqualTo(new[]
        {
            "FEAT", "OPTS UTF8 ON",
        }));
        client.AbortConnection();
    }

    [Test]
    public void Connect_normalizes_FEAT_and_adapts_UTF8_MLST_and_capability_flags()
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
                "211-Extensions:\r\n" +
                " UTF8\r\n" +
                " MLST Type*;Size;Modify*;\r\n" +
                " REST STREAM\r\n" +
                " EPSV\r\n" +
                " EPRT\r\n" +
                " AUTH TLS\r\n" +
                " HOST\r\n" +
                " PRET\r\n" +
                " MODE Z\r\n" +
                " CLNT\r\n" +
                " MFMT\r\n" +
                " HASH SHA-256\r\n" +
                " X-VENDOR alpha\r\n" +
                "211 End\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 UTF8 ON\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 MLST OPTS type;size;modify;\r\n");
            commands.Add(ReadUtf8Command(stream));
            WriteText(stream, "200 XMODE OPTS\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "200 UTF8 OFF\r\n");
        });
        FtpClient client = new FtpClient(null);

        client.Connect(CreateOptions(listener, true));
        FtpOptionRequest extension = new FtpOptionRequest();
        extension.CommandName = "XMODE";
        extension.Value = "café";
        FtpCommandResult extensionResult = client.ApplyOption(extension);
        FtpOptionRequest disableUtf8 = new FtpOptionRequest();
        disableUtf8.CommandName = FtpFeatureNames.Utf8;
        disableUtf8.Value = "OFF";
        FtpCommandResult disableResult = client.ApplyOption(disableUtf8);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "FEAT",
                "OPTS UTF8 ON",
                "OPTS MLST type;size;modify;",
                "OPTS XMODE café",
                "OPTS UTF8 OFF",
            }));
            Assert.That(client.State.Features.Negotiated, Is.True);
            Assert.That(client.State.Features.Features.Select(feature => feature.Name),
                Is.EqualTo(new[]
                {
                    "UTF8", "MLST", "REST STREAM", "EPSV", "EPRT", "AUTH",
                    "HOST", "PRET", "MODE Z", "CLNT", "MFMT", "HASH", "X-VENDOR",
                }));
            Assert.That(client.State.FeatureState.Utf8Advertised, Is.True);
            Assert.That(client.State.FeatureState.Utf8Enabled, Is.False);
            Assert.That(client.State.Connection!.Utf8Enabled, Is.False);
            Assert.That(client.State.FeatureState.MlstAdvertised, Is.True);
            Assert.That(client.State.FeatureState.MlstFacts,
                Is.EqualTo(new[] { "type", "size", "modify" }));
            Assert.That(client.State.FeatureState.RestStream, Is.True);
            Assert.That(client.State.FeatureState.ExtendedPassive, Is.True);
            Assert.That(client.State.FeatureState.ExtendedActive, Is.True);
            Assert.That(client.State.FeatureState.AuthTls, Is.True);
            Assert.That(client.State.FeatureState.Host, Is.True);
            Assert.That(client.State.FeatureState.Pret, Is.True);
            Assert.That(client.State.FeatureState.ModeZ, Is.True);
            Assert.That(client.State.FeatureState.ClientName, Is.True);
            Assert.That(client.State.FeatureState.ModificationTime, Is.True);
            Assert.That(client.State.FeatureState.Hash, Is.True);
            Assert.That(extensionResult.Succeeded, Is.True);
            Assert.That(disableResult.Succeeded, Is.True);
            Assert.That(client.State.FeatureState.OptionProcessor!.State.ExtensionOptions,
                Has.Length.EqualTo(1));
        });

        client.AbortConnection();
    }

    [Test]
    public void Refresh_replaces_stale_capabilities_and_encoding_state()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            RequireCommand(stream, "FEAT");
            WriteText(stream,
                "211-First:\r\n UTF8\r\n REST STREAM\r\n211 End\r\n");
            RequireCommand(stream, "OPTS UTF8 ON");
            WriteText(stream, "200 UTF8 ON\r\n");
            RequireCommand(stream, "FEAT");
            WriteText(stream,
                "211-Second:\r\n HOST\r\n EPSV\r\n211 End\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener, false));

        FtpFeatureSet first = client.NegotiateFeatures();
        Assert.Multiple(() =>
        {
            Assert.That(first.Negotiated, Is.True);
            Assert.That(client.State.FeatureState.Utf8Enabled, Is.True);
            Assert.That(client.State.Connection!.Utf8Enabled, Is.True);
            Assert.That(client.State.FeatureState.RestStream, Is.True);
        });

        FtpFeatureSet second = client.RefreshFeatures();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(second.Negotiated, Is.True);
            Assert.That(second.Features.Select(feature => feature.Name),
                Is.EqualTo(new[] { "HOST", "EPSV" }));
            Assert.That(client.State.Features, Is.SameAs(second));
            Assert.That(client.State.FeatureState.Utf8Advertised, Is.False);
            Assert.That(client.State.FeatureState.Utf8Enabled, Is.False);
            Assert.That(client.State.Connection!.Utf8Enabled, Is.False);
            Assert.That(client.State.FeatureState.RestStream, Is.False);
            Assert.That(client.State.FeatureState.Host, Is.True);
            Assert.That(client.State.FeatureState.ExtendedPassive, Is.True);
            Assert.That(client.State.FeatureState.ExtendedActive, Is.False);
        });

        client.AbortConnection();
    }

    [Test]
    public void Negative_FEAT_and_OPTS_replies_remain_inspectable_without_false_support()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            RequireCommand(stream, "FEAT");
            WriteText(stream, "502 FEAT unavailable\r\n");
            RequireCommand(stream, "OPTS UTF8 ON");
            WriteText(stream, "501 UTF8 option rejected\r\n");
        });
        FtpClient client = new FtpClient(null);
        client.Connect(CreateOptions(listener, true));
        FtpOptionRequest request = new FtpOptionRequest();
        request.CommandName = FtpFeatureNames.Utf8;
        request.Value = "ON";

        FtpCommandResult optionResult = client.ApplyOption(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Features.Negotiated, Is.False);
            Assert.That(client.State.Features.SourceReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandNotImplemented));
            Assert.That(client.State.FeatureState.Utf8Advertised, Is.False);
            Assert.That(optionResult.Succeeded, Is.False);
            Assert.That(optionResult.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.SyntaxErrorInParameters));
            Assert.That(optionResult.FailureMessage, Does.Contain("501"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
        });

        client.AbortConnection();
    }

    [Test]
    public void Malformed_successful_FEAT_aborts_automatic_connection_setup()
    {
        using TcpListener listener = StartListener();
        Task server = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            RequireCommand(stream, "FEAT");
            WriteText(stream,
                "211-Features:\r\n UTF8\r\n211 Not-End\r\n");
        });
        FtpClient client = new FtpClient(null);

        Assert.Throws<FtpProtocolException>(() =>
            client.Connect(CreateOptions(listener, true)));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClientOptions CreateOptions(
        TcpListener listener,
        bool negotiateFeatures)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = negotiateFeatures;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static void RequireCommand(Stream stream, string expected)
    {
        string actual = ReadCommand(stream, Encoding.ASCII);
        if (actual != expected)
        {
            throw new InvalidDataException(
                "Expected command '" + expected + "' but received '" + actual + "'.");
        }
    }

    private static string ReadCommand(Stream stream)
    {
        return ReadCommand(stream, Encoding.ASCII);
    }

    private static string ReadUtf8Command(Stream stream)
    {
        return ReadCommand(stream, Encoding.UTF8);
    }

    private static string ReadCommand(Stream stream, Encoding encoding)
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
                return encoding.GetString(bytes.ToArray());
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
