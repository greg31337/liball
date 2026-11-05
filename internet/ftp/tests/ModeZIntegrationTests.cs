using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class ModeZIntegrationTests
{
    [Test]
    public void Client_negotiates_MODE_Z_only_when_FEAT_advertises_it()
    {
        using TcpListener advertisedListener = StartListener();
        List<string> advertisedCommands = new List<string>();
        Task advertisedServer = Task.Run(() =>
        {
            using TcpClient accepted = advertisedListener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            advertisedCommands.Add(ReadCommand(stream));
            WriteText(stream,
                "211-Extensions:\r\n MODE Z\r\n211 End\r\n");
            advertisedCommands.Add(ReadCommand(stream));
            WriteText(stream, "331 Password required\r\n");
            advertisedCommands.Add(ReadCommand(stream));
            WriteText(stream, "230 Logged in\r\n");
            advertisedCommands.Add(ReadCommand(stream));
            WriteText(stream, "200 MODE Z accepted\r\n");
        });
        FtpClient advertisedClient = new FtpClient(null);
        advertisedClient.Connect(CreateClientOptions(advertisedListener));
        advertisedClient.AuthenticateUser(CreateCredentials());

        FtpCommandResult acceptedMode = advertisedClient.SetTransferMode(
            FtpTransferMode.Deflate);
        advertisedServer.GetAwaiter().GetResult();

        using TcpListener absentListener = StartListener();
        List<string> absentCommands = new List<string>();
        Task absentServer = Task.Run(() =>
        {
            using TcpClient accepted = absentListener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Ready\r\n");
            absentCommands.Add(ReadCommand(stream));
            WriteText(stream,
                "211-Extensions:\r\n UTF8\r\n211 End\r\n");
            absentCommands.Add(ReadCommand(stream));
            WriteText(stream, "200 UTF8 accepted\r\n");
            absentCommands.Add(ReadCommand(stream));
            WriteText(stream, "331 Password required\r\n");
            absentCommands.Add(ReadCommand(stream));
            WriteText(stream, "230 Logged in\r\n");
            WaitForPeerClose(stream);
        });
        FtpClient absentClient = new FtpClient(null);
        absentClient.Connect(CreateClientOptions(absentListener));
        absentClient.AuthenticateUser(CreateCredentials());

        Assert.Throws<FtpConfigurationException>(() =>
            absentClient.SetTransferMode(FtpTransferMode.Deflate));
        advertisedClient.AbortConnection();
        absentClient.AbortConnection();
        absentServer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(advertisedCommands,
                Is.EqualTo(new[]
                {
                    "FEAT", "USER alice", "PASS secret", "MODE Z"
                }));
            Assert.That(acceptedMode.Succeeded, Is.True);
            Assert.That(advertisedClient.State.FeatureState.ModeZ, Is.True);
            Assert.That(absentCommands,
                Is.EqualTo(new[]
                {
                    "FEAT", "OPTS UTF8 ON", "USER alice", "PASS secret"
                }));
            Assert.That(absentClient.State.FeatureState.ModeZ, Is.False);
        });
    }

    [TestCase(FtpFileStructure.File)]
    [TestCase(FtpFileStructure.Record)]
    [TestCase(FtpFileStructure.Page)]
    public void ZLIB_wraps_the_complete_transfer_pipeline_for_every_structure(
        FtpFileStructure structure)
    {
        FtpTransferOptions options = CreateTransferOptions(structure);
        byte[] local = CreateContent(structure);
        byte[] framed = TransformPipeline(options, true, local, 1);
        MemoryStream compressed = new MemoryStream();

        long encodedBytes = ModeZCodec.EncodeStream(
            CreateCodecRequest(options, new MemoryStream(framed), compressed),
            true);
        MemoryStream inflated = new MemoryStream();
        long decodedBytes = ModeZCodec.DecodeStream(
            CreateCodecRequest(
                options, new MemoryStream(compressed.ToArray()), inflated),
            true);
        byte[] decoded = TransformPipeline(
            options, false, inflated.ToArray(), 1);

        Assert.Multiple(() =>
        {
            Assert.That(encodedBytes, Is.EqualTo(framed.Length), structure.ToString());
            Assert.That(decodedBytes, Is.EqualTo(framed.Length), structure.ToString());
            Assert.That(compressed.ToArray()[0], Is.EqualTo(0x78), structure.ToString());
            Assert.That(decoded, Is.EqualTo(local), structure.ToString());
        });
    }

    [Test]
    public void Corrupted_headers_payloads_checksums_and_invalid_codes_are_rejected()
    {
        FtpTransferOptions options = CreateTransferOptions(
            FtpFileStructure.File);
        byte[] content = Enumerable.Repeat((byte)65, 4096)
            .Concat(Enumerable.Range(0, 256).Select(value => (byte)value))
            .ToArray();
        MemoryStream encoded = new MemoryStream();
        ModeZCodec.EncodeStream(CreateCodecRequest(
            options, new MemoryStream(content), encoded), true);
        byte[] valid = encoded.ToArray();
        byte[][] corrupted =
        {
            Mutate(valid, 0, 0xFF),
            Mutate(valid, valid.Length / 2, 0x80),
            Mutate(valid, valid.Length - 1, 0x01),
            new byte[] { 0x78, 0x9C, 0x07 }
        };

        for (int index = 0; index < corrupted.Length; index++)
        {
            FtpTransferCodecRequest request = CreateCodecRequest(
                options, new MemoryStream(corrupted[index]), new MemoryStream());
            FtpTransferException? failure = Assert.Throws<FtpTransferException>(() =>
                ModeZCodec.DecodeStream(request, true),
                "corruption case " + index);
            Assert.That(failure!.InnerException,
                Is.TypeOf<InvalidDataException>(), "corruption case " + index);
        }
    }

    [Test]
    public void Cancellation_aborts_an_active_MODE_Z_data_stream()
    {
        using SocketPair pair = CreateSocketPair();
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpClearDataStream transfer = new FtpClearDataStream(coordinator);
        CancelingReadStream source = new CancelingReadStream(
            new byte[8192], cancellation);
        FtpClearDataStreamRequest request = new FtpClearDataStreamRequest();
        request.CommandKind = FtpCommandKind.Retr;
        request.LocalStream = source;
        request.Direction = FtpTransferDirection.Download;
        request.ConnectionTimeoutMilliseconds = 5000;
        request.TransferTimeoutMilliseconds = 5000;
        request.LeaveLocalStreamOpen = true;
        request.Options!.Mode = FtpTransferMode.Deflate;
        request.Options.BufferBytes = 128;
        request.Options.Timeouts.CancellationToken = cancellation.Token;

        transfer.BeginSend(request);
        OperationCanceledException? failure =
            Assert.Throws<OperationCanceledException>(() =>
                transfer.ExecuteSendData());
        pair.Client = null;

        Assert.Multiple(() =>
        {
            Assert.That(failure!.CancellationToken,
                Is.EqualTo(cancellation.Token));
            Assert.That(source.ReadCalls, Is.EqualTo(1));
            Assert.That(coordinator.Aborted, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(transfer.DataComplete, Is.False);
            Assert.That(transfer.Result.FinalReply, Is.Null);
            Assert.Throws<FtpProtocolException>(() =>
                transfer.CreateFinalReply());
        });
    }

    [Test]
    public void MODE_Z_requires_the_explicit_extension_and_never_aliases_MODE_C()
    {
        FtpTransferOptions deflate = CreateTransferOptions(
            FtpFileStructure.File);
        FtpTransferCodecRequest disabled = CreateCodecRequest(
            deflate, new MemoryStream(new byte[] { 1 }), new MemoryStream());
        FtpTransferOptions compressed = CreateTransferOptions(
            FtpFileStructure.File);
        compressed.Mode = FtpTransferMode.Compressed;
        FtpTransferCodecRequest modeC = CreateCodecRequest(
            compressed, new MemoryStream(new byte[] { 1 }), new MemoryStream());

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                ModeZCodec.EncodeStream(disabled, false));
            Assert.Throws<FtpConfigurationException>(() =>
                ModeZCodec.EncodeStream(modeC, true));
            Assert.Throws<FtpConfigurationException>(() =>
                ModeZCodec.DecodeStream(modeC, true));
        });
    }

    private static FtpClientOptions CreateClientOptions(
        TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port =
            ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = true;
        options.AllowPlaintextCredentials = true;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static FtpCredentials CreateCredentials()
    {
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        return credentials;
    }

    private static FtpTransferOptions CreateTransferOptions(
        FtpFileStructure structure)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = structure == FtpFileStructure.Record ?
            FtpTransferType.Ascii : FtpTransferType.Image;
        options.Structure = structure;
        options.Mode = FtpTransferMode.Deflate;
        options.BufferBytes = 31;
        options.VerificationMode = FtpVerificationMode.None;
        return options;
    }

    private static byte[] CreateContent(FtpFileStructure structure)
    {
        if (structure == FtpFileStructure.Record)
        {
            return Encoding.ASCII.GetBytes(
                "alpha" + Environment.NewLine +
                "beta" + Environment.NewLine);
        }
        if (structure == FtpFileStructure.Page)
        {
            return new byte[]
            {
                4, 1, 4, 1, 10, 20, 30, 40,
                4, 0, 0, 0
            };
        }
        return Enumerable.Repeat((byte)7, 2048)
            .Concat(Enumerable.Range(0, 256).Select(value => (byte)value))
            .ToArray();
    }

    private static FtpTransferCodecRequest CreateCodecRequest(
        FtpTransferOptions options,
        Stream source,
        Stream destination)
    {
        FtpTransferCodecRequest request = new FtpTransferCodecRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.LeaveSourceOpen = true;
        request.LeaveDestinationOpen = true;
        return request;
    }

    private static byte[] TransformPipeline(
        FtpTransferOptions options,
        bool encoding,
        byte[] input,
        int chunkBytes)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        List<byte> output = new List<byte>();
        int offset = 0;
        while (offset < input.Length)
        {
            int count = Math.Min(chunkBytes, input.Length - offset);
            byte[] chunk = Slice(input, offset, count);
            Append(output, transform.TransformChunk(chunk, count, false));
            offset += count;
        }
        Append(output, transform.TransformChunk(
            Array.Empty<byte>(), 0, true));
        return output.ToArray();
    }

    private static void Append(
        List<byte> output,
        FtpTransferPumpChunk[] chunks)
    {
        for (int index = 0; index < chunks.Length; index++)
        {
            FtpTransferPumpChunk chunk = chunks[index];
            byte[] data = chunk.Data ?? Array.Empty<byte>();
            for (int byteIndex = 0; byteIndex < chunk.Count; byteIndex++)
            {
                output.Add(data[byteIndex]);
            }
        }
    }

    private static byte[] Mutate(byte[] source, int index, byte mask)
    {
        byte[] result = Slice(source, 0, source.Length);
        result[index] = (byte)(result[index] ^ mask);
        return result;
    }

    private static byte[] Slice(byte[] source, int offset, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(source, offset, result, 0, count);
        return result;
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
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

    private static void WaitForPeerClose(Stream stream)
    {
        while (stream.ReadByte() >= 0)
        {
        }
    }

    private static SocketPair CreateSocketPair()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Socket client = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(IPAddress.Loopback, port);
        Socket server = listener.AcceptSocket();
        listener.Stop();
        return new SocketPair(client, server);
    }

    private class CancelingReadStream : MemoryStream
    {
        private readonly CancellationTokenSource Cancellation;

        internal int ReadCalls;

        internal CancelingReadStream(
            byte[] content,
            CancellationTokenSource cancellation)
            : base(content)
        {
            Cancellation = cancellation;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            int read = base.Read(buffer, offset, count);
            Cancellation.Cancel();
            return read;
        }
    }

    private class SocketPair : IDisposable
    {
        internal Socket? Client;
        internal Socket Server;

        internal SocketPair(Socket client, Socket server)
        {
            Client = client;
            Server = server;
        }

        public void Dispose()
        {
            if (Client != null)
            {
                Client.Dispose();
            }
            Server.Dispose();
        }
    }
}
