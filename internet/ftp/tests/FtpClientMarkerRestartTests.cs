using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientMarkerRestartTests
{
    [Test]
    public void Block_upload_reports_server_marker_and_reuses_it_for_STOR_restart()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener firstDataListener = StartListener();
        using TcpListener secondDataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Mode selected\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream,
                firstDataListener,
                commands,
                "110 MARK F1:3:3:1:0:0 = SERVER-THREE\r\n" +
                    "226 Transfer complete\r\n"));
            uploads.Add(ReceiveResumedUpload(
                stream,
                secondDataListener,
                commands,
                "SERVER-THREE",
                "226 Resume complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream firstContent = new MemoryStream(
            Encoding.ASCII.GetBytes("abcdef"));
        FtpTransferRequest first = CreateUploadRequest(firstContent);

        FtpTransferResult firstResult = client.Transfer(first);

        using MemoryStream resumedContent = new MemoryStream(
            Encoding.ASCII.GetBytes("abcdef"));
        FtpTransferRequest resumed = CreateUploadRequest(resumedContent);
        resumed.Options.CollisionPolicy = FtpCollisionPolicy.Resume;
        resumed.Options.ResumePolicy = FtpResumePolicy.Required;
        FtpTransferResult resumedResult = client.Transfer(resumed);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MODE B", "TYPE I",
                "EPSV", "STOR /file.bin", "EPSV", "REST SERVER-THREE",
                "STOR /file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "abcdef", "def" }));
            Assert.That(firstResult.RestartMarker, Is.EqualTo("SERVER-THREE"));
            Assert.That(resumedResult.RestartMarker, Is.EqualTo("SERVER-THREE"));
            Assert.That(resumedResult.Progress.LogicalBytes, Is.EqualTo(6));
            Assert.That(client.GetLatestRestartMarker(), Is.EqualTo("SERVER-THREE"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Page_compressed_download_uses_local_checkpoint_and_server_marker()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener firstDataListener = StartListener();
        using TcpListener secondDataListener = StartListener();
        List<string> commands = new List<string>();
        byte[] pageContent = CreatePageContent();
        byte[] lastPage = new byte[] { 4, 0, 0, 0 };
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Page structure selected\r\n");
            Receive(stream, commands, "200 Compressed mode selected\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(
                stream,
                firstDataListener,
                commands,
                pageContent,
                "110 MARK F1:10:18:1:0:0 = PAGE-SERVER-10\r\n" +
                    "226 Transfer complete\r\n");
            SendResumedDownload(
                stream,
                secondDataListener,
                commands,
                "PAGE-SERVER-10",
                lastPage,
                "226 Resume complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest first = CreatePageDownloadRequest(content);

        FtpTransferResult firstResult = client.Transfer(first);

        FtpTransferRequest resumed = CreatePageDownloadRequest(content);
        resumed.Options.CollisionPolicy = FtpCollisionPolicy.Resume;
        resumed.Options.ResumePolicy = FtpResumePolicy.Required;
        FtpTransferResult resumedResult = client.Transfer(resumed);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "STRU P", "MODE C", "TYPE I",
                "EPSV", "RETR /page.bin", "EPSV", "REST PAGE-SERVER-10",
                "RETR /page.bin",
            }));
            Assert.That(content.ToArray(), Is.EqualTo(pageContent));
            Assert.That(firstResult.RestartMarker,
                Is.EqualTo("PAGE-SERVER-10"));
            Assert.That(resumedResult.RestartMarker,
                Is.EqualTo("PAGE-SERVER-10"));
            Assert.That(resumedResult.ActualSize,
                Is.EqualTo(pageContent.Length));
        });

        client.AbortConnection();
    }

    private static FtpTransferRequest CreateUploadRequest(Stream content)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/file.bin";
        request.Content = content;
        request.Options.Mode = FtpTransferMode.Block;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static FtpTransferRequest CreatePageDownloadRequest(Stream content)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = "/page.bin";
        request.Content = content;
        request.Options.Structure = FtpFileStructure.Page;
        request.Options.Mode = FtpTransferMode.Compressed;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static string ReceiveUpload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string finalReplies)
    {
        PreparePassive(controlStream, dataListener, commands);
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening data\r\n");
        string content = ReadBlockData(data);
        WriteText(controlStream, finalReplies);
        return content;
    }

    private static string ReceiveResumedUpload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string marker,
        string finalReply)
    {
        PreparePassive(controlStream, dataListener, commands);
        using TcpClient data = dataListener.AcceptTcpClient();
        Receive(controlStream, commands, "350 Marker accepted\r\n");
        Assert.That(commands[^1], Is.EqualTo("REST " + marker));
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening resumed data\r\n");
        string content = ReadBlockData(data);
        WriteText(controlStream, finalReply);
        return content;
    }

    private static void SendDownload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        byte[] content,
        string finalReplies)
    {
        PreparePassive(controlStream, dataListener, commands);
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening data\r\n");
        using (NetworkStream dataStream = data.GetStream())
        {
            WriteBytes(dataStream, TransformTransferBytes(
                CreatePageOptions(), true, content));
        }
        WriteText(controlStream, finalReplies);
    }

    private static void SendResumedDownload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string marker,
        byte[] content,
        string finalReply)
    {
        PreparePassive(controlStream, dataListener, commands);
        using TcpClient data = dataListener.AcceptTcpClient();
        Receive(controlStream, commands, "350 Marker accepted\r\n");
        Assert.That(commands[^1], Is.EqualTo("REST " + marker));
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening resumed data\r\n");
        using (NetworkStream dataStream = data.GetStream())
        {
            WriteBytes(dataStream, TransformTransferBytes(
                CreatePageOptions(), true, content));
        }
        WriteText(controlStream, finalReply);
    }

    private static string ReadBlockData(TcpClient data)
    {
        using NetworkStream dataStream = data.GetStream();
        using MemoryStream buffer = new MemoryStream();
        dataStream.CopyTo(buffer);
        return Encoding.ASCII.GetString(TransformTransferBytes(
            CreateBlockOptions(), false, buffer.ToArray()));
    }

    private static byte[] TransformTransferBytes(
        FtpTransferOptions options,
        bool encoding,
        byte[] input)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        List<byte> output = new List<byte>();
        AppendChunks(output, transform.TransformChunk(
            input, input.Length, false));
        AppendChunks(output, transform.TransformChunk(
            Array.Empty<byte>(), 0, true));
        return output.ToArray();
    }

    private static void AppendChunks(
        List<byte> output,
        FtpTransferPumpChunk[] chunks)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            FtpTransferPumpChunk chunk = chunks[index];
            byte[] data = chunk.Data ?? Array.Empty<byte>();
            int byteIndex = 0;
            while (byteIndex < chunk.Count)
            {
                output.Add(data[byteIndex]);
                byteIndex++;
            }
            index++;
        }
    }

    private static FtpTransferOptions CreateBlockOptions()
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Mode = FtpTransferMode.Block;
        options.VerificationMode = FtpVerificationMode.None;
        return options;
    }

    private static FtpTransferOptions CreatePageOptions()
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Structure = FtpFileStructure.Page;
        options.Mode = FtpTransferMode.Compressed;
        options.VerificationMode = FtpVerificationMode.None;
        return options;
    }

    private static byte[] CreatePageContent()
    {
        return new byte[]
        {
            4, 1, 6, 1,
            (byte)'a', (byte)'b', (byte)'c',
            (byte)'d', (byte)'e', (byte)'f',
            4, 0, 0, 0
        };
    }

    private static void WriteBytes(Stream stream, byte[] bytes)
    {
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static void PreparePassive(
        NetworkStream stream,
        TcpListener listener,
        List<string> commands)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, "229 Entering Extended Passive Mode (|||" +
            GetPort(listener).ToString() + "|)\r\n");
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
