using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.IO.Compression;
using System.Text;

namespace ftp.Tests;

public class FtpServerFileTransferCommandTests
{
    private string RootPath = "";
    private List<string> TransferControlReplies = new List<string>();
    private FtpResourceLimits UserLimits = new FtpResourceLimits();

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-transfer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        TransferControlReplies.Clear();
        UserLimits = new FtpResourceLimits();
    }

    [Test]
    public void Matching_PRET_allows_the_announced_transfer_after_passive_setup()
    {
        File.WriteAllText(
            Path.Combine(RootPath, "file name.bin"), "prepared");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.EnablePret = true;

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream controlStream = control.GetStream();
            using StreamReader reader = CreateReader(controlStream);
            Login(controlStream, reader);
            WriteText(controlStream, "TYPE I\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            WriteText(controlStream, "PRET RETR file name.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));

            byte[] downloaded = DownloadPassive(
                controlStream, reader, "RETR file name.bin");
            WriteText(controlStream, "QUIT\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("221 "));

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(downloaded),
                    Is.EqualTo("prepared"));
                Assert.That(authorizer.Requests, Has.Count.EqualTo(1));
                Assert.That(authorizer.Requests[0].Path,
                    Is.EqualTo("/file name.bin"));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void MODE_Z_advertises_negotiates_wraps_data_and_restarts_at_logical_offsets()
    {
        File.WriteAllText(
            Path.Combine(RootPath, "resume-read.bin"),
            "prefix repeated payload");
        File.WriteAllText(
            Path.Combine(RootPath, "resume-write.bin"),
            "prefix old-data");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.EnableModeZ = true;

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "FEAT\r\n");
            List<string> features = new List<string>();
            string? feature = reader.ReadLine();
            while (feature != null)
            {
                features.Add(feature);
                if (feature == "211 End")
                {
                    break;
                }
                feature = reader.ReadLine();
            }
            WriteText(stream,
                "OPTS MODE Z\r\n" +
                "OPTS MODE Z LEVEL 9\r\n" +
                "MODE Z\r\n");
            string? defaults = reader.ReadLine();
            string? unsupported = reader.ReadLine();
            string? mode = reader.ReadLine();

            int downloadPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST 7\r\n");
            string? restart = reader.ReadLine();
            byte[] downloadWire = DownloadConfiguredPassiveWire(
                stream, reader, downloadPort, "RETR resume-read.bin");
            int uploadPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST 7\r\n");
            string? uploadRestart = reader.ReadLine();
            UploadConfiguredPassiveWire(
                stream,
                reader,
                uploadPort,
                "STOR resume-write.bin",
                CompressZlib(Encoding.ASCII.GetBytes("replacement")));
            byte[] listingWire = DownloadPassiveWire(
                stream, reader, "NLST /");
            WriteText(stream, "MODE S\r\nQUIT\r\n");
            string? streamMode = reader.ReadLine();
            reader.ReadLine();

            string listing = Encoding.ASCII.GetString(
                DecompressZlib(listingWire));
            Assert.Multiple(() =>
            {
                Assert.That(features, Does.Contain(" MODE Z"));
                Assert.That(defaults,
                    Is.EqualTo("200 MODE Z default ZLIB settings restored."));
                Assert.That(unsupported, Does.StartWith("501 "));
                Assert.That(mode,
                    Is.EqualTo("200 Transfer mode accepted."));
                Assert.That(restart,
                    Is.EqualTo("350 Restart marker accepted."));
                Assert.That(uploadRestart,
                    Is.EqualTo("350 Restart marker accepted."));
                Assert.That(Encoding.ASCII.GetString(
                    DecompressZlib(downloadWire)),
                    Is.EqualTo("repeated payload"));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "resume-write.bin")),
                    Is.EqualTo("prefix replacement"));
                Assert.That(listing, Does.Contain("resume-read.bin"));
                Assert.That(listing, Does.Contain("resume-write.bin"));
                Assert.That(streamMode,
                    Is.EqualTo("200 Transfer mode accepted."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void MODE_Z_is_not_advertised_or_negotiated_when_disabled()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "FEAT\r\n");
            List<string> features = new List<string>();
            string? feature = reader.ReadLine();
            while (feature != null)
            {
                features.Add(feature);
                if (feature == "211 End")
                {
                    break;
                }
                feature = reader.ReadLine();
            }
            WriteText(stream, "OPTS MODE Z\r\nMODE Z\r\nQUIT\r\n");
            string? option = reader.ReadLine();
            string? mode = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features, Does.Not.Contain(" MODE Z"));
                Assert.That(option, Does.StartWith("504 "));
                Assert.That(mode, Does.StartWith("504 "));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void Passive_RETR_STOR_APPE_and_STOU_transfer_content_and_report_final_replies()
    {
        File.WriteAllBytes(
            Path.Combine(RootPath, "download.bin"),
            Encoding.ASCII.GetBytes("abcdef"));
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream controlStream = control.GetStream();
            using StreamReader reader = CreateReader(controlStream);
            Login(controlStream, reader);
            WriteText(controlStream, "TYPE I\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));

            byte[] downloaded = DownloadPassive(
                controlStream, reader, "RETR download.bin");
            UploadPassive(
                controlStream,
                reader,
                "STOR upload.bin",
                Encoding.ASCII.GetBytes("new"));
            UploadPassive(
                controlStream,
                reader,
                "APPE upload.bin",
                Encoding.ASCII.GetBytes("+tail"));
            string uniquePreliminary = UploadPassive(
                controlStream,
                reader,
                "STOU",
                Encoding.ASCII.GetBytes("unique"));
            string? uniquePath = FtpClientUploadOperation.ExtractUniqueName(
                uniquePreliminary.Substring(4));
            WriteText(controlStream, "QUIT\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("221 "));

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(downloaded),
                    Is.EqualTo("abcdef"));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "upload.bin")),
                    Is.EqualTo("new+tail"));
                Assert.That(uniquePath, Is.Not.Null);
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, uniquePath!.Substring(1))),
                    Is.EqualTo("unique"));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.Read,
                        FtpAuthorizationOperation.Write,
                        FtpAuthorizationOperation.Append,
                        FtpAuthorizationOperation.Write
                    }));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Path), Is.EqualTo(new[]
                    {
                        "/download.bin",
                        "/upload.bin",
                        "/upload.bin",
                        "/"
                    }));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Per_user_transfer_and_storage_ceilings_return_552_and_release_resources()
    {
        File.WriteAllText(Path.Combine(RootPath, "download.bin"), "abcdef");
        File.WriteAllText(Path.Combine(RootPath, "stored.bin"), "1234");
        UserLimits.HasTransferByteCeiling = true;
        UserLimits.TransferByteCeiling = 3;
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "RETR download.bin\r\n");
            string? transferFailure = reader.ReadLine();

            UserLimits.HasTransferByteCeiling = false;
            UserLimits.TransferByteCeiling = 0;
            UserLimits.HasStorageQuotaBytes = true;
            UserLimits.StorageQuotaBytes = 10;
            int dataPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "APPE stored.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));
            using TcpClient data = Connect(dataPort);
            NetworkStream dataStream = data.GetStream();
            byte[] excess = Encoding.ASCII.GetBytes("1234567");
            dataStream.Write(excess, 0, excess.Length);
            dataStream.Flush();
            data.Client.Shutdown(SocketShutdown.Send);
            string? quotaFailure = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(transferFailure,
                    Is.EqualTo("552 The configured storage or transfer limit was exceeded."));
                Assert.That(quotaFailure,
                    Is.EqualTo("552 Transfer aborted; configured resource limit exceeded."));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "stored.bin")), Is.EqualTo("1234"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(), Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Per_user_read_and_write_bandwidth_ceilings_throttle_server_transfers()
    {
        File.WriteAllText(Path.Combine(RootPath, "read.bin"), "12");
        UserLimits.HasReadBytesPerSecond = true;
        UserLimits.ReadBytesPerSecond = 100;
        UserLimits.HasWriteBytesPerSecond = true;
        UserLimits.WriteBytesPerSecond = 100;
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            System.Diagnostics.Stopwatch readElapsed =
                System.Diagnostics.Stopwatch.StartNew();
            byte[] downloaded = DownloadPassive(
                stream, reader, "RETR read.bin");
            readElapsed.Stop();
            System.Diagnostics.Stopwatch writeElapsed =
                System.Diagnostics.Stopwatch.StartNew();
            UploadPassive(
                stream,
                reader,
                "STOR write.bin",
                Encoding.ASCII.GetBytes("34"));
            writeElapsed.Stop();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(downloaded),
                    Is.EqualTo("12"));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "write.bin")), Is.EqualTo("34"));
                Assert.That(readElapsed.ElapsedMilliseconds,
                    Is.GreaterThanOrEqualTo(15));
                Assert.That(writeElapsed.ElapsedMilliseconds,
                    Is.GreaterThanOrEqualTo(15));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Stream_restart_resumes_RETR_and_STOR_and_rejects_non_image_resume()
    {
        File.WriteAllText(Path.Combine(RootPath, "read.bin"), "abcdef");
        File.WriteAllText(Path.Combine(RootPath, "write.bin"), "abcdef");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "TYPE I\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            int readDataPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST 2\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("350 Restart marker accepted."));
            WriteText(stream, "RETR read.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));
            byte[] resumedDownload;
            using (TcpClient readData = Connect(readDataPort))
            using (MemoryStream received = new MemoryStream())
            {
                readData.GetStream().CopyTo(received);
                resumedDownload = received.ToArray();
            }
            Assert.That(reader.ReadLine(), Does.StartWith("226 "));

            int writeDataPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST 2\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("350 Restart marker accepted."));
            WriteText(stream, "STOR write.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));
            using (TcpClient writeData = Connect(writeDataPort))
            {
                NetworkStream writeStream = writeData.GetStream();
                byte[] replacement = Encoding.ASCII.GetBytes("XY");
                writeStream.Write(replacement, 0, replacement.Length);
                writeStream.Flush();
                writeData.Client.Shutdown(SocketShutdown.Send);
            }
            Assert.That(reader.ReadLine(), Does.StartWith("226 "));

            WriteText(stream, "TYPE A\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST 1\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("350 Restart marker accepted."));
            WriteText(stream, "RETR read.bin\r\n");
            string? rejected = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(resumedDownload),
                    Is.EqualTo("cdef"));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "write.bin")),
                    Is.EqualTo("abXYef"));
                Assert.That(rejected,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Transfer_requires_data_setup_and_authorization_denial_does_not_open_files()
    {
        File.WriteAllText(Path.Combine(RootPath, "kept.bin"), "kept");
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = false;
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "RETR kept.bin\r\n");
            string? noSetup = reader.ReadLine();
            WriteText(stream, "EPSV\r\n");
            string? epsv = reader.ReadLine();
            WriteText(stream, "STOR kept.bin\r\n");
            string? denied = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(noSetup, Does.StartWith("425 "));
                Assert.That(epsv, Does.StartWith("229 "));
                Assert.That(denied,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "kept.bin")),
                    Is.EqualTo("kept"));
                Assert.That(authorizer.Requests, Has.Count.EqualTo(1));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Simultaneous_transfer_limit_rejects_second_session_and_releases_slot()
    {
        File.WriteAllText(Path.Combine(RootPath, "content.bin"), "content");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumSimultaneousTransfers = 1;

        try
        {
            server.StartServer(options);
            using TcpClient first = Connect(port);
            using NetworkStream firstStream = first.GetStream();
            using StreamReader firstReader = CreateReader(firstStream);
            Login(firstStream, firstReader);
            int firstDataPort = EnterExtendedPassive(firstStream, firstReader);
            WriteText(firstStream, "RETR content.bin\r\n");
            Assert.That(firstReader.ReadLine(), Does.StartWith("150 "));

            using TcpClient second = Connect(port);
            using NetworkStream secondStream = second.GetStream();
            using StreamReader secondReader = CreateReader(secondStream);
            Login(secondStream, secondReader);
            EnterExtendedPassive(secondStream, secondReader);
            WriteText(secondStream, "RETR content.bin\r\n");
            string? limited = secondReader.ReadLine();

            using TcpClient data = Connect(firstDataPort);
            using MemoryStream content = new MemoryStream();
            data.GetStream().CopyTo(content);
            string? completed = firstReader.ReadLine();
            WriteText(firstStream, "QUIT\r\n");
            firstReader.ReadLine();
            WriteText(secondStream, "QUIT\r\n");
            secondReader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(limited,
                    Is.EqualTo("450 No server transfer slot is available."));
                Assert.That(completed, Does.StartWith("226 "));
                Assert.That(Encoding.ASCII.GetString(content.ToArray()),
                    Is.EqualTo("content"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Active_RETR_connects_to_the_validated_PORT_endpoint()
    {
        File.WriteAllText(Path.Combine(RootPath, "active.bin"), "active");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            FtpServerOptions options = CreateOptions(port);
            options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            using TcpListener dataListener =
                new TcpListener(IPAddress.Loopback, 0);
            dataListener.Start();
            int dataPort = ((IPEndPoint)dataListener.LocalEndpoint).Port;
            string portArgument = "127,0,0,1," +
                (dataPort / 256).ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "," +
                (dataPort % 256).ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            WriteText(stream,
                "TYPE I\r\nPORT " + portArgument + "\r\nRETR active.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 PORT command successful"));
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));
            using Socket dataSocket = dataListener.AcceptSocket();
            using NetworkStream dataStream =
                new NetworkStream(dataSocket, false);
            using MemoryStream received = new MemoryStream();
            dataStream.CopyTo(received);
            string? completed = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(completed, Does.StartWith("226 "));
                Assert.That(Encoding.ASCII.GetString(received.ToArray()),
                    Is.EqualTo("active"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void ABOR_cancels_a_transfer_waiting_for_passive_data_and_emits_one_terminal_sequence()
    {
        File.WriteAllText(Path.Combine(RootPath, "waiting.bin"), "waiting");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "RETR waiting.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));

            WriteText(stream, "ABOR\r\n");
            string? aborted = reader.ReadLine();
            string? completedAbort = reader.ReadLine();
            WriteText(stream, "NOOP\r\n");
            string? noOperation = reader.ReadLine();
            bool released = SpinWait.SpinUntil(
                () => server.Runtime.Limits!.GetTransferCount() == 0 &&
                    server.Runtime.PassivePortPool!.ReservedCount == 0,
                5000);
            WriteText(stream, "QUIT\r\n");
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(aborted,
                    Is.EqualTo(
                        "426 Data connection closed; transfer aborted."));
                Assert.That(completedAbort,
                    Is.EqualTo("226 Abort command successful."));
                Assert.That(noOperation,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(released, Is.True);
                Assert.That(quit, Does.StartWith("221 "));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Control_commands_continue_while_RETR_waits_and_final_reply_follows_data_completion()
    {
        File.WriteAllText(Path.Combine(RootPath, "ordered.bin"), "ordered");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            int dataPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "RETR ordered.bin\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));

            WriteText(stream, "NOOP\r\n");
            string? noOperation = reader.ReadLine();
            byte[] content;
            using (TcpClient data = Connect(dataPort))
            using (MemoryStream received = new MemoryStream())
            {
                data.GetStream().CopyTo(received);
                content = received.ToArray();
            }
            string? completed = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(noOperation,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(completed,
                    Is.EqualTo(
                        "226 Closing data connection; transfer complete"));
                Assert.That(Encoding.ASCII.GetString(content),
                    Is.EqualTo("ordered"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Every_representation_format_structure_and_mode_combination_negotiates()
    {
        string[] representations =
        {
            "A N", "A T", "A C", "E N", "E T", "E C", "I", "L 8"
        };
        string[] structures = { "F", "R", "P" };
        string[] modes = { "S", "B", "C", "Z" };
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.EnableModeZ = true;
        options.MaximumCommandsPerMinute = 1000;
        int combinations = 0;

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            int representationIndex = 0;
            while (representationIndex < representations.Length)
            {
                int structureIndex = 0;
                while (structureIndex < structures.Length)
                {
                    bool record = structures[structureIndex] == "R";
                    bool binary = representations[representationIndex] == "I" ||
                        representations[representationIndex].StartsWith(
                            "L ", StringComparison.Ordinal);
                    if (!record || !binary)
                    {
                        int modeIndex = 0;
                        while (modeIndex < modes.Length)
                        {
                            WriteText(stream,
                                "TYPE A N\r\n" +
                                "STRU F\r\n" +
                                "TYPE " + representations[representationIndex] +
                                    "\r\n" +
                                "STRU " + structures[structureIndex] + "\r\n" +
                                "MODE " + modes[modeIndex] + "\r\n");
                            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
                            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
                            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
                            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
                            Assert.That(reader.ReadLine(), Does.StartWith("200 "),
                                representations[representationIndex] + "/" +
                                structures[structureIndex] + "/" +
                                modes[modeIndex]);
                            combinations++;
                            modeIndex++;
                        }
                    }
                    structureIndex++;
                }
                representationIndex++;
            }
            WriteText(stream, "QUIT\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("221 "));

            Assert.That(combinations, Is.EqualTo(88));
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void ALLO_applies_once_after_upload_path_resolution_and_reports_provider_failure()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        TransferRecordingFileSystem fileSystem =
            new TransferRecordingFileSystem(RootPath);
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, fileSystem);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "TYPE I\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));

            int storedPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "ALLO 12 R 4\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 Allocation request accepted."));
            UploadConfiguredPassiveWire(
                stream,
                reader,
                storedPort,
                "STOR allocated.bin",
                Encoding.ASCII.GetBytes("one"));

            int uniquePort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "ALLO 8\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 Allocation request accepted."));
            UploadConfiguredPassiveWire(
                stream,
                reader,
                uniquePort,
                "STOU",
                Encoding.ASCII.GetBytes("two"));

            int failedPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "ALLO 16\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 Allocation request accepted."));
            fileSystem.FailAllocation = true;
            WriteText(stream, "STOR rejected.bin\r\n");
            string? allocationFailure = reader.ReadLine();
            fileSystem.FailAllocation = false;

            UploadPassiveWire(
                stream,
                reader,
                "STOR no-allocation.bin",
                Encoding.ASCII.GetBytes("three"));
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(fileSystem.AllocationPaths,
                    Has.Count.EqualTo(3));
                Assert.That(fileSystem.AllocationPaths[0],
                    Is.EqualTo("/allocated.bin"));
                Assert.That(fileSystem.AllocationLengths,
                    Is.EqualTo(new long[] { 12, 8, 16 }));
                Assert.That(fileSystem.AllocationPaths[1],
                    Is.EqualTo(fileSystem.UniquePaths.Single()));
                Assert.That(allocationFailure, Is.EqualTo(
                    "452 Insufficient storage information is available."));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "allocated.bin")),
                    Is.EqualTo("one"));
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "rejected.bin")), Is.False);
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "no-allocation.bin")),
                    Is.EqualTo("three"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
                Assert.That(failedPort, Is.GreaterThan(0));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Provider_open_unique_and_disk_path_failures_are_isolated()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        TransferRecordingFileSystem fileSystem =
            new TransferRecordingFileSystem(RootPath);
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, fileSystem);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);

            fileSystem.FailOpenRead = true;
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "RETR content.bin\r\n");
            string? readFailure = reader.ReadLine();
            fileSystem.FailOpenRead = false;

            fileSystem.ThrowOpenWrite = true;
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "STOR provider.bin\r\n");
            string? writeFault = reader.ReadLine();
            fileSystem.ThrowOpenWrite = false;

            fileSystem.FailUnique = true;
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "STOU\r\n");
            string? uniqueFailure = reader.ReadLine();
            fileSystem.FailUnique = false;

            EnterExtendedPassive(stream, reader);
            WriteText(stream, "STOR missing/child.bin\r\n");
            string? diskPathFailure = reader.ReadLine();
            WriteText(stream, "NOOP\r\nQUIT\r\n");
            string? probe = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(readFailure, Is.EqualTo(
                    "550 The requested file action was not taken."));
                Assert.That(writeFault, Is.EqualTo(
                    "550 The requested file action was not taken."));
                Assert.That(uniqueFailure, Is.EqualTo(
                    "550 The requested file action was not taken."));
                Assert.That(diskPathFailure, Is.EqualTo(
                    "550 The requested file action was not taken."));
                Assert.That(probe,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "provider.bin")), Is.False);
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Server_applies_text_local_byte_record_block_and_compressed_processing()
    {
        byte[] ascii = Encoding.ASCII.GetBytes("one\ntwo\n");
        byte[] ebcdic = Encoding.UTF8.GetBytes("ABC\n");
        byte[] local = new byte[] { 5, 3 };
        byte[] records = Encoding.ASCII.GetBytes("first\nsecond\n");
        byte[] upload = Encoding.ASCII.GetBytes("stored\nrecords\n");
        File.WriteAllBytes(Path.Combine(RootPath, "ascii.txt"), ascii);
        File.WriteAllBytes(Path.Combine(RootPath, "ebcdic.txt"), ebcdic);
        File.WriteAllBytes(Path.Combine(RootPath, "local.bin"), local);
        File.WriteAllBytes(Path.Combine(RootPath, "records.txt"), records);
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);

            WriteText(stream, "TYPE A N\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            byte[] asciiWire = DownloadPassiveWire(
                stream, reader, "RETR ascii.txt");

            WriteText(stream, "TYPE E N\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            byte[] ebcdicWire = DownloadPassiveWire(
                stream, reader, "RETR ebcdic.txt");

            WriteText(stream, "TYPE L 3\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            byte[] localWire = DownloadPassiveWire(
                stream, reader, "RETR local.bin");

            WriteText(stream, "TYPE A N\r\nSTRU R\r\nMODE B\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            byte[] recordWire = DownloadPassiveWire(
                stream, reader, "RETR records.txt");

            WriteText(stream, "MODE C\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            FtpTransferOptions compressedOptions = CreateTransferOptions(
                FtpTransferType.Ascii,
                FtpFileStructure.Record,
                FtpTransferMode.Compressed,
                0);
            byte[] compressedWire = TransformTransferBytes(
                compressedOptions, true, upload);
            UploadPassiveWire(
                stream, reader, "STOR compressed.txt", compressedWire);
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            FtpTransferOptions blockOptions = CreateTransferOptions(
                FtpTransferType.Ascii,
                FtpFileStructure.Record,
                FtpTransferMode.Block,
                0);
            Assert.Multiple(() =>
            {
                Assert.That(asciiWire,
                    Is.EqualTo(Encoding.ASCII.GetBytes("one\r\ntwo\r\n")));
                Assert.That(ebcdicWire,
                    Is.EqualTo(new byte[] { 0xC1, 0xC2, 0xC3, 0x15 }));
                Assert.That(localWire, Is.EqualTo(new byte[] { 0xAC }));
                Assert.That(TransformTransferBytes(
                    blockOptions, false, recordWire), Is.EqualTo(records));
                Assert.That(File.ReadAllBytes(
                    Path.Combine(RootPath, "compressed.txt")),
                    Is.EqualTo(upload));
                Assert.That(TransferControlReplies,
                    Has.Some.StartsWith("110 MARK F1:"));
                Assert.That(server.Runtime.Limits!.GetTransferCount(),
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Block_mode_restart_marker_resumes_server_downloads_and_uploads()
    {
        File.WriteAllText(Path.Combine(RootPath, "resume-read.bin"), "abcdef");
        File.WriteAllText(Path.Combine(RootPath, "resume-write.bin"), "abcdef");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpTransferOptions blockOptions = CreateTransferOptions(
            FtpTransferType.Image,
            FtpFileStructure.File,
            FtpTransferMode.Block,
            0);
        string marker = "F1:3:0:0:0:0";

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream, "TYPE I\r\nMODE B\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));
            Assert.That(reader.ReadLine(), Does.StartWith("200 "));

            int downloadPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST " + marker + "\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("350 "));
            byte[] downloadWire = DownloadConfiguredPassiveWire(
                stream, reader, downloadPort, "RETR resume-read.bin");

            int uploadPort = EnterExtendedPassive(stream, reader);
            WriteText(stream, "REST " + marker + "\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("350 "));
            byte[] uploadWire = TransformTransferBytes(
                blockOptions,
                true,
                Encoding.ASCII.GetBytes("XYZ"));
            UploadConfiguredPassiveWire(
                stream,
                reader,
                uploadPort,
                "STOR resume-write.bin",
                uploadWire);
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(
                    TransformTransferBytes(
                        blockOptions, false, downloadWire)),
                    Is.EqualTo("def"));
                Assert.That(File.ReadAllText(
                    Path.Combine(RootPath, "resume-write.bin")),
                    Is.EqualTo("abcXYZ"));
                Assert.That(TransferControlReplies.Count(reply =>
                    reply.StartsWith("110 MARK F1:",
                        StringComparison.Ordinal)),
                    Is.GreaterThanOrEqualTo(2));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private byte[] DownloadPassive(
        Stream controlStream,
        StreamReader reader,
        string command)
    {
        int dataPort = EnterExtendedPassive(controlStream, reader);
        WriteText(controlStream, command + "\r\n");
        Assert.That(reader.ReadLine(), Does.StartWith("150 "));
        using TcpClient data = Connect(dataPort);
        using MemoryStream received = new MemoryStream();
        data.GetStream().CopyTo(received);
        Assert.That(reader.ReadLine(),
            Is.EqualTo("226 Closing data connection; transfer complete"));
        return received.ToArray();
    }

    private byte[] DownloadPassiveWire(
        Stream controlStream,
        StreamReader reader,
        string command)
    {
        int dataPort = EnterExtendedPassive(controlStream, reader);
        return DownloadConfiguredPassiveWire(
            controlStream, reader, dataPort, command);
    }

    private byte[] DownloadConfiguredPassiveWire(
        Stream controlStream,
        StreamReader reader,
        int dataPort,
        string command)
    {
        WriteText(controlStream, command + "\r\n");
        Assert.That(reader.ReadLine(), Does.StartWith("150 "));
        using TcpClient data = Connect(dataPort);
        using MemoryStream received = new MemoryStream();
        data.GetStream().CopyTo(received);
        ReadTransferCompletion(reader);
        return received.ToArray();
    }

    private void UploadPassiveWire(
        Stream controlStream,
        StreamReader reader,
        string command,
        byte[] wire)
    {
        int dataPort = EnterExtendedPassive(controlStream, reader);
        UploadConfiguredPassiveWire(
            controlStream, reader, dataPort, command, wire);
    }

    private void UploadConfiguredPassiveWire(
        Stream controlStream,
        StreamReader reader,
        int dataPort,
        string command,
        byte[] wire)
    {
        WriteText(controlStream, command + "\r\n");
        Assert.That(reader.ReadLine(), Does.StartWith("150 "));
        using TcpClient data = Connect(dataPort);
        NetworkStream dataStream = data.GetStream();
        dataStream.Write(wire, 0, wire.Length);
        dataStream.Flush();
        data.Client.Shutdown(SocketShutdown.Send);
        ReadTransferCompletion(reader);
    }

    private void ReadTransferCompletion(StreamReader reader)
    {
        string? reply = reader.ReadLine();
        while (reply != null && reply.StartsWith(
            "110 ", StringComparison.Ordinal))
        {
            TransferControlReplies.Add(reply);
            reply = reader.ReadLine();
        }
        if (reply != null)
        {
            TransferControlReplies.Add(reply);
        }
        Assert.That(reply, Does.StartWith("226 "));
    }

    private static byte[] TransformTransferBytes(
        FtpTransferOptions options,
        bool encoding,
        byte[] input)
    {
        FtpTransferPipelineTransform transform =
            new FtpTransferPipelineTransform(options, encoding, null, 0);
        List<byte> output = new List<byte>();
        FtpTransferPumpChunk[] data = transform.TransformChunk(
            input, input.Length, false);
        AppendTransferChunks(output, data);
        FtpTransferPumpChunk[] final = transform.TransformChunk(
            Array.Empty<byte>(), 0, true);
        AppendTransferChunks(output, final);
        return output.ToArray();
    }

    private static byte[] CompressZlib(byte[] content)
    {
        MemoryStream output = new MemoryStream();
        using (ZLibStream compression = new ZLibStream(
            output, CompressionLevel.Optimal, true))
        {
            compression.Write(content, 0, content.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressZlib(byte[] wire)
    {
        using ZLibStream decompression = new ZLibStream(
            new MemoryStream(wire), CompressionMode.Decompress);
        MemoryStream output = new MemoryStream();
        decompression.CopyTo(output);
        return output.ToArray();
    }

    private static void AppendTransferChunks(
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

    private static FtpTransferOptions CreateTransferOptions(
        FtpTransferType type,
        FtpFileStructure structure,
        FtpTransferMode mode,
        int localByteSize)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.Structure = structure;
        options.Mode = mode;
        options.VerificationMode = FtpVerificationMode.None;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = localByteSize;
        }
        return options;
    }

    private string UploadPassive(
        Stream controlStream,
        StreamReader reader,
        string command,
        byte[] content)
    {
        int dataPort = EnterExtendedPassive(controlStream, reader);
        WriteText(controlStream, command + "\r\n");
        string? preliminary = reader.ReadLine();
        Assert.That(preliminary, Does.StartWith("150 "));
        using TcpClient data = Connect(dataPort);
        NetworkStream dataStream = data.GetStream();
        dataStream.Write(content, 0, content.Length);
        dataStream.Flush();
        data.Client.Shutdown(SocketShutdown.Send);
        Assert.That(reader.ReadLine(),
            Is.EqualTo("226 Closing data connection; transfer complete"));
        return preliminary == null ? "" : preliminary;
    }

    private int EnterExtendedPassive(
        Stream controlStream,
        StreamReader reader)
    {
        WriteText(controlStream, "EPSV\r\n");
        string? reply = reader.ReadLine();
        Assert.That(reply, Does.StartWith("229 "));
        string[] fields = reply == null ?
            new string[0] : reply.Split('|');
        Assert.That(fields, Has.Length.GreaterThan(3));
        return int.Parse(fields[3],
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private void Login(Stream stream, StreamReader reader)
    {
        Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
        WriteText(stream, "USER alice\r\nPASS secret\r\nACCT billing\r\n");
        Assert.That(reader.ReadLine(), Does.StartWith("331 "));
        Assert.That(reader.ReadLine(), Does.StartWith("332 "));
        Assert.That(reader.ReadLine(), Does.StartWith("230 "));
    }

    private RecordingFtpAuthorizer CreateAllowedAuthorizer()
    {
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = true;
        return authorizer;
    }

    private FtpServer CreateServer(IFtpAuthorizer authorizer)
    {
        return CreateServer(
            authorizer, new LocalFtpFileSystem(RootPath));
    }

    private FtpServer CreateServer(
        IFtpAuthorizer authorizer,
        IFtpFileSystem fileSystem)
    {
        ScriptedServerAuthenticator authenticator =
            new ScriptedServerAuthenticator();
        authenticator.ResourceLimits = UserLimits;
        return new FtpServer(
            fileSystem,
            authenticator,
            authorizer,
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink());
    }

    private static FtpServerOptions CreateOptions(int port)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new FtpListenerOptions[] { listener };
        return options;
    }

    private static TcpClient Connect(int port)
    {
        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        client.Connect(IPAddress.Loopback, port);
        return client;
    }

    private static StreamReader CreateReader(Stream stream)
    {
        stream.ReadTimeout = 5000;
        return new StreamReader(
            stream, Encoding.ASCII, false, 1024, true);
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation =
            new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerLoginProviderProxy>();
    }
}

public class TransferRecordingFileSystem : IFtpFileSystem
{
    public IFtpFileSystem Inner;
    public List<string> AllocationPaths;
    public List<long> AllocationLengths;
    public List<string> UniquePaths;
    public bool FailAllocation;
    public bool FailOpenRead;
    public bool ThrowOpenWrite;
    public bool FailUnique;

    public TransferRecordingFileSystem(string rootPath)
    {
        Inner = new LocalFtpFileSystem(rootPath);
        AllocationPaths = new List<string>();
        AllocationLengths = new List<long>();
        UniquePaths = new List<string>();
        FailAllocation = false;
        FailOpenRead = false;
        ThrowOpenWrite = false;
        FailUnique = false;
    }

    public FtpFileSystemResult ResolveEntry(FtpFileSystemRequest request)
    {
        return Inner.ResolveEntry(request);
    }

    public FtpFileSystemResult EnumerateDirectory(
        FtpFileSystemRequest request)
    {
        return Inner.EnumerateDirectory(request);
    }

    public FtpFileOpenResult OpenFileRead(FtpFileSystemRequest request)
    {
        if (FailOpenRead)
        {
            return new FtpFileOpenResult();
        }
        return Inner.OpenFileRead(request);
    }

    public FtpFileOpenResult OpenFileWrite(FtpFileSystemRequest request)
    {
        if (ThrowOpenWrite)
        {
            throw new IOException("Injected write-open provider fault.");
        }
        return Inner.OpenFileWrite(request);
    }

    public FtpFileSystemResult CreateDirectoryEntry(
        FtpFileSystemRequest request)
    {
        return Inner.CreateDirectoryEntry(request);
    }

    public FtpFileSystemResult DeleteFileEntry(FtpFileSystemRequest request)
    {
        return Inner.DeleteFileEntry(request);
    }

    public FtpFileSystemResult DeleteDirectoryEntry(
        FtpFileSystemRequest request)
    {
        return Inner.DeleteDirectoryEntry(request);
    }

    public FtpFileSystemResult RenameEntry(FtpFileSystemRequest request)
    {
        return Inner.RenameEntry(request);
    }

    public FtpFileSystemResult CreateUniqueEntry(
        FtpFileSystemRequest request)
    {
        if (FailUnique)
        {
            return new FtpFileSystemResult();
        }
        FtpFileSystemResult result = Inner.CreateUniqueEntry(request);
        if (result.Succeeded && result.Path != null)
        {
            UniquePaths.Add(result.Path);
        }
        return result;
    }

    public FtpFileSystemResult AllocateEntry(FtpFileSystemRequest request)
    {
        AllocationPaths.Add(request.Path);
        AllocationLengths.Add(request.Length);
        if (FailAllocation)
        {
            return new FtpFileSystemResult();
        }
        return Inner.AllocateEntry(request);
    }

    public FtpFileSystemResult SetModificationTime(
        FtpFileSystemRequest request)
    {
        return Inner.SetModificationTime(request);
    }

    public FtpFileSystemResult SetPermissions(FtpFileSystemRequest request)
    {
        return Inner.SetPermissions(request);
    }

    public FtpFileSystemResult QuerySpace(FtpFileSystemRequest request)
    {
        return Inner.QuerySpace(request);
    }

    public FtpFileSystemResult MountStructure(FtpFileSystemRequest request)
    {
        return Inner.MountStructure(request);
    }
}
