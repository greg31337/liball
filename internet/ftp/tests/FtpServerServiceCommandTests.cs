using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ftp.Tests;

public class FtpServerServiceCommandTests
{
    private string RootPath = "";

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-service-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    [Test]
    public void Enabled_CLNT_and_MFMT_advertise_parse_authorize_and_mutate()
    {
        string path = Path.Combine(RootPath, "file name.txt");
        File.WriteAllText(path, "content");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.EnableClnt = true;
        options.EnableMfmt = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "CLNT test client/1.0\r\n" +
                "MFMT 20240229123456 file name.txt\r\n" +
                "MFMT 20230229123456 file name.txt\r\n" +
                "MFMT 20240229123456  file name.txt\r\n" +
                "MFMT 20240229123456 missing.txt\r\n" +
                "QUIT\r\n");
            string? clientName = reader.ReadLine();
            string? modified = reader.ReadLine();
            string? invalidDate = reader.ReadLine();
            string? invalidSeparator = reader.ReadLine();
            string? missing = reader.ReadLine();
            string? quit = reader.ReadLine();

            DateTime stored = File.GetLastWriteTimeUtc(path);
            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Clnt));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Mfmt));
                Assert.That(clientName,
                    Is.EqualTo("200 Client identification accepted."));
                Assert.That(modified,
                    Is.EqualTo("213 Modify=20240229123456; /file name.txt"));
                Assert.That(invalidDate, Does.StartWith("501 "));
                Assert.That(invalidSeparator, Does.StartWith("501 "));
                Assert.That(missing, Does.StartWith("550 "));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(stored,
                    Is.EqualTo(new DateTime(
                        2024, 2, 29, 12, 34, 56, DateTimeKind.Utc)));
                Assert.That(authorizer.Requests.Select(item => item.Operation),
                    Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.WriteMetadata,
                        FtpAuthorizationOperation.WriteMetadata,
                    }));
                Assert.That(authorizer.Requests.Select(item => item.Path),
                    Is.EqualTo(new[] { "/file name.txt", "/missing.txt" }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Disabled_CLNT_and_MFMT_are_not_advertised_or_executed()
    {
        File.WriteAllText(Path.Combine(RootPath, "file.txt"), "content");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "CLNT test-client\r\n" +
                "MFMT 20240229123456 file.txt\r\n" +
                "QUIT\r\n");
            string? clientName = reader.ReadLine();
            string? modified = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Clnt));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Mfmt));
                Assert.That(clientName, Does.StartWith("502 "));
                Assert.That(modified, Does.StartWith("502 "));
                Assert.That(authorizer.Requests, Is.Empty);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Compatibility_handlers_precede_enabled_built_ins_and_HELP_tracks_availability()
    {
        string filePath = Path.Combine(RootPath, "file.bin");
        string treePath = Path.Combine(RootPath, "tree");
        File.WriteAllText(filePath, "unchanged");
        Directory.CreateDirectory(treePath);
        File.WriteAllText(Path.Combine(treePath, "item.txt"), "unchanged");
        DateTime originalModified = File.GetLastWriteTimeUtc(filePath);
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        RecordingExtensionHandler handler = new RecordingExtensionHandler();
        handler.Result.Disposition = FtpExtensionDisposition.Handled;
        handler.Result.Replies = new[]
        {
            FtpServerSession.CreateReply(
                FtpReplyCodes.CommandOkay,
                "Compatibility extension handled")
        };
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.ExtensionHandlers = new IFtpExtensionHandler[] { handler };
        options.EnableHistoricAliases = true;
        options.EnablePret = true;
        options.EnableClnt = true;
        options.EnableMfmt = true;
        options.EnableModeZ = true;
        options.EnableHash = true;
        options.EnableMd5 = true;
        options.EnableXmd5 = true;
        options.EnableXsha1 = true;
        options.EnableXsha256 = true;
        options.EnableXsha512 = true;
        options.EnableXcrc = true;
        options.EnableCompatibilityExtensions = true;
        string[] commands = new[]
        {
            "XCWD /folder",
            "XCUP",
            "XPWD",
            "XMKD /made",
            "XRMD /tree",
            "PRET RETR /file.bin",
            "CLNT client/1.0",
            "MFMT 20240203040506 /file.bin",
            "HASH /file.bin",
            "MD5 /file.bin",
            "XMD5 /file.bin",
            "XSHA1 /file.bin",
            "XSHA256 /file.bin",
            "XSHA512 /file.bin",
            "XCRC /file.bin",
            "MODE Z",
            "SITE CHMOD 600 /file.bin",
            "SITE UMASK 077",
            "SITE MKDIR /made/child",
            "SITE RMDIR /tree",
            "RMDA /tree",
            "OPTS HASH SHA-1"
        };

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            WriteText(stream, "HELP\r\n");
            FtpReply help = ReadMultilineReply(reader);
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                string.Join("\r\n", commands) + "\r\nQUIT\r\n");
            List<string?> replies = new List<string?>();
            int index = 0;
            while (index < commands.Length)
            {
                replies.Add(reader.ReadLine());
                index++;
            }
            string? quit = reader.ReadLine();

            string[] helpCommands = help.Lines[1].RawText.Split(
                ' ', StringSplitOptions.RemoveEmptyEntries);
            string[] featureNames = features.Features.Select(feature =>
                feature.Name).ToArray();
            FtpExtensionContext[] contexts = handler.GetContexts();
            Assert.Multiple(() =>
            {
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Pret));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Clnt));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Mfmt));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.ModeZ));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Hash));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Md5));
                Assert.That(featureNames, Does.Contain(FtpFeatureNames.Rmda));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Xcwd));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Pret));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Clnt));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Mfmt));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Hash));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Xcrc));
                Assert.That(helpCommands, Does.Contain(FtpCommandNames.Rmda));
                Assert.That(replies, Has.All.EqualTo(
                    "200 Compatibility extension handled"));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(contexts.Select(context => context.Command.RawText),
                    Is.EqualTo(commands));
                Assert.That(File.ReadAllText(filePath), Is.EqualTo("unchanged"));
                Assert.That(File.GetLastWriteTimeUtc(filePath),
                    Is.EqualTo(originalModified));
                Assert.That(Directory.Exists(treePath), Is.True);
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "made")), Is.False);
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.All.EqualTo(
                        FtpAuthorizationOperation.Site));
                Assert.That(authorizer.Requests, Has.Count.EqualTo(4));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Enabled_checksum_commands_advertise_negotiate_and_hash_files()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("123456789");
        File.WriteAllBytes(Path.Combine(RootPath, "file name.bin"), bytes);
        Directory.CreateDirectory(Path.Combine(RootPath, "folder"));
        string md5 = Convert.ToHexString(MD5.HashData(bytes));
        string sha1 = Convert.ToHexString(SHA1.HashData(bytes));
        string sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        string sha512 = Convert.ToHexString(SHA512.HashData(bytes));
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.EnableHash = true;
        options.EnableMd5 = true;
        options.EnableXmd5 = true;
        options.EnableXsha1 = true;
        options.EnableXsha256 = true;
        options.EnableXsha512 = true;
        options.EnableXcrc = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet initial = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            LoginAfterGreeting(stream, reader);
            WriteText(stream, "OPTS HASH\r\n");
            string? initialOption = reader.ReadLine();
            WriteText(stream, "OPTS HASH sha1\r\n");
            string? selectedOption = reader.ReadLine();
            WriteText(stream, "OPTS HASH SHA-224\r\n");
            string? invalidOption = reader.ReadLine();
            WriteText(stream, "OPTS HASH\r\n");
            string? retainedOption = reader.ReadLine();
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet changed = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            WriteText(stream,
                "HASH file name.bin\r\n" +
                "MD5 file name.bin\r\n" +
                "XMD5 file name.bin\r\n" +
                "XSHA1 file name.bin\r\n" +
                "XSHA256 file name.bin\r\n" +
                "XSHA512 file name.bin\r\n" +
                "XCRC file name.bin\r\n" +
                "HASH missing.bin\r\n" +
                "HASH folder\r\n" +
                "QUIT\r\n");
            string? hash = reader.ReadLine();
            string? md5Reply = reader.ReadLine();
            string? xmd5Reply = reader.ReadLine();
            string? sha1Reply = reader.ReadLine();
            string? sha256Reply = reader.ReadLine();
            string? sha512Reply = reader.ReadLine();
            string? crcReply = reader.ReadLine();
            string? missingReply = reader.ReadLine();
            string? directoryReply = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(FindFeature(initial, FtpFeatureNames.Hash).Parameters,
                    Is.EqualTo("SHA-256*;SHA-512;SHA-1;MD5;CRC32;"));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Md5));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Xmd5));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Xsha1));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Xsha256));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Xsha512));
                Assert.That(initial.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Xcrc));
                Assert.That(initialOption, Is.EqualTo("200 SHA-256"));
                Assert.That(selectedOption, Is.EqualTo("200 SHA-1"));
                Assert.That(invalidOption, Does.StartWith("501 "));
                Assert.That(retainedOption, Is.EqualTo("200 SHA-1"));
                Assert.That(FindFeature(changed, FtpFeatureNames.Hash).Parameters,
                    Is.EqualTo("SHA-256;SHA-512;SHA-1*;MD5;CRC32;"));
                Assert.That(hash, Is.EqualTo(
                    "213 SHA-1 0-8 " + sha1.ToLowerInvariant() +
                    " /file name.bin"));
                Assert.That(md5Reply, Is.EqualTo(
                    "251 " + md5.ToLowerInvariant() + " /file name.bin"));
                Assert.That(xmd5Reply, Is.EqualTo(
                    "250 " + md5.ToLowerInvariant() + " /file name.bin"));
                Assert.That(sha1Reply, Is.EqualTo(
                    "250 " + sha1.ToLowerInvariant() + " /file name.bin"));
                Assert.That(sha256Reply, Is.EqualTo(
                    "250 " + sha256.ToLowerInvariant() + " /file name.bin"));
                Assert.That(sha512Reply, Is.EqualTo(
                    "250 " + sha512.ToLowerInvariant() + " /file name.bin"));
                Assert.That(crcReply,
                    Is.EqualTo("250 cbf43926 /file name.bin"));
                Assert.That(missingReply, Does.StartWith("550 "));
                Assert.That(directoryReply, Does.StartWith("553 "));
                Assert.That(authorizer.Requests.Select(item => item.Operation),
                    Is.All.EqualTo(FtpAuthorizationOperation.Read));
                Assert.That(authorizer.Requests.Select(item => item.Path),
                    Is.EqualTo(new[]
                    {
                        "/file name.bin", "/file name.bin", "/file name.bin",
                        "/file name.bin", "/file name.bin", "/file name.bin",
                        "/file name.bin", "/missing.bin", "/folder"
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Disabled_checksum_commands_are_not_advertised_or_executed()
    {
        File.WriteAllText(Path.Combine(RootPath, "file.bin"), "content");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            WriteText(stream, "HELP\r\n");
            FtpReply help = ReadMultilineReply(reader);
            WriteText(stream, "HELP HASH\r\nHELP RMDA\r\n");
            string? hashHelp = reader.ReadLine();
            string? rmdaHelp = reader.ReadLine();
            WriteText(stream, "OPTS HASH SHA-256\r\n");
            string? option = reader.ReadLine();
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "HASH file.bin\r\nMD5 file.bin\r\nXCRC file.bin\r\nQUIT\r\n");
            string? hash = reader.ReadLine();
            string? md5 = reader.ReadLine();
            string? crc = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                string[] helpCommands = help.Lines[1].RawText.Split(
                    ' ', StringSplitOptions.RemoveEmptyEntries);
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Hash));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Md5));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Xcrc));
                Assert.That(helpCommands,
                    Does.Not.Contain(FtpCommandNames.Hash));
                Assert.That(helpCommands,
                    Does.Not.Contain(FtpCommandNames.Rmda));
                Assert.That(hashHelp, Does.StartWith("502 "));
                Assert.That(rmdaHelp, Does.StartWith("502 "));
                Assert.That(option, Does.StartWith("504 "));
                Assert.That(hash, Does.StartWith("502 "));
                Assert.That(md5, Does.StartWith("502 "));
                Assert.That(crc, Does.StartWith("502 "));
                Assert.That(authorizer.Requests, Is.Empty);
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
    public void Metadata_commands_return_authorized_size_and_UTC_modification_time()
    {
        string path = Path.Combine(RootPath, "report.bin");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
        DateTime modified = new DateTime(
            2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, modified);
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            WriteText(stream,
                "SIZE report.bin\r\n" +
                "MDTM report.bin\r\n" +
                "SIZE missing.bin\r\n" +
                "QUIT\r\n");
            string? size = reader.ReadLine();
            string? timestamp = reader.ReadLine();
            string? missing = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(size, Is.EqualTo("213 4"));
                Assert.That(timestamp, Is.EqualTo("213 20240203040506"));
                Assert.That(missing,
                    Is.EqualTo(
                        "550 The requested metadata was not available."));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata
                    }));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Path), Is.EqualTo(new[]
                    {
                        "/report.bin", "/report.bin", "/missing.bin"
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void FEAT_OPTS_and_LANG_use_active_host_capabilities_and_session_state()
    {
        File.WriteAllText(Path.Combine(RootPath, "item.txt"), "seven!!");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        FtpVirtualHostOptions host = CreateCapableHost();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, host);
        FtpServerOptions options = CreateOptions(port, host);

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 Capable ready"));

            WriteText(stream, "FEAT\r\n");
            FtpReply initialFeatures = ReadMultilineReply(reader);
            WriteText(stream, "LANG fr\r\n");
            string? language = reader.ReadLine();
            WriteText(stream, "FEAT\r\n");
            FtpReply changedFeatures = ReadMultilineReply(reader);
            WriteText(stream, "LANG de\r\n");
            string? unsupportedLanguage = reader.ReadLine();
            WriteText(stream, "OPTS MLST size;type;unknown;\r\n");
            string? mlstOptions = reader.ReadLine();
            WriteText(stream, "OPTS UTF8 OFF\r\n");
            string? utf8Off = reader.ReadLine();
            WriteText(stream, "OPTS UTF8 ON\r\n");
            string? utf8On = reader.ReadLine();
            LoginAfterGreeting(stream, reader);
            WriteText(stream, "MLST item.txt\r\n");
            FtpReply metadata = ReadMultilineReply(reader);
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            FtpFeatureSet initial =
                FtpFeatureCodec.ParseFeatureReply(initialFeatures);
            FtpFeatureSet changed =
                FtpFeatureCodec.ParseFeatureReply(changedFeatures);
            FtpEntry entry = FtpMlsxCodec.ParseControlReply(metadata);

            Assert.Multiple(() =>
            {
                Assert.That(initial.Features.Select(feature => feature.Name),
                    Is.EqualTo(new[]
                    {
                        "UTF8", "LANG", "MDTM", "SIZE", "REST STREAM",
                        "TVFS", "MLST", "HOST", "X-CUSTOM"
                    }));
                Assert.That(initial.Features.Select(feature => feature.Name),
                    Does.Not.Contain("AUTH"));
                Assert.That(initial.Features.Select(feature => feature.Name),
                    Does.Not.Contain("MODE Z"));
                Assert.That(FindFeature(initial, "LANG").Parameters,
                    Is.EqualTo("en*;fr"));
                Assert.That(language, Is.EqualTo("200 Language set to fr."));
                Assert.That(FindFeature(changed, "LANG").Parameters,
                    Is.EqualTo("en;fr*"));
                Assert.That(unsupportedLanguage,
                    Is.EqualTo("504 The requested language is not supported."));
                Assert.That(mlstOptions,
                    Is.EqualTo("200 MLST OPTS type;size;"));
                Assert.That(utf8Off, Is.EqualTo("200 UTF8 OFF"));
                Assert.That(utf8On, Is.EqualTo("200 UTF8 ON"));
                Assert.That(entry.Facts.Select(fact => fact.Name),
                    Is.EqualTo(new[] { "type", "size" }));
                Assert.That(entry.Size, Is.EqualTo(7));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void SYST_HELP_NOOP_and_SITE_report_capabilities_and_enforce_SITE_authorization()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "SYST\r\nHELP NOOP\r\nHELP\r\nNOOP\r\n");
            string? system = reader.ReadLine();
            string? topicHelp = reader.ReadLine();
            FtpReply help = ReadMultilineReply(reader);
            string? noop = reader.ReadLine();
            LoginAfterGreeting(stream, reader);
            WriteText(stream, "SITE UNKNOWN\r\nQUIT\r\n");
            string? site = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(system, Does.StartWith("215 "));
                Assert.That(topicHelp,
                    Is.EqualTo("214 NOOP is supported by this FTP server."));
                Assert.That(help.Code, Is.EqualTo(214));
                Assert.That(help.Lines[1].RawText,
                    Does.Contain("MDTM SIZE SYST HELP SITE NOOP FEAT OPTS LANG"));
                Assert.That(noop, Is.EqualTo("200 NOOP command successful."));
                Assert.That(site,
                    Is.EqualTo(
                        "502 The requested SITE parameter is not implemented."));
                Assert.That(authorizer.Requests, Has.Count.EqualTo(1));
                Assert.That(authorizer.Requests[0].Operation,
                    Is.EqualTo(FtpAuthorizationOperation.Site));
                Assert.That(authorizer.Requests[0].Path,
                    Is.EqualTo("UNKNOWN"));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Enabled_SITE_compatibility_mutates_modes_and_directory_trees_with_root_safety()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Unix permission mutation is unavailable on Windows.");
        }
        string chmodPath = Path.Combine(RootPath, "chmod.txt");
        File.WriteAllText(chmodPath, "content");
        string siteTree = Path.Combine(RootPath, "site-tree", "child");
        Directory.CreateDirectory(siteTree);
        File.WriteAllText(Path.Combine(siteTree, "item.txt"), "site");
        string rmdaTree = Path.Combine(RootPath, "rmda-tree", "child");
        Directory.CreateDirectory(rmdaTree);
        File.WriteAllText(Path.Combine(rmdaTree, "item.txt"), "rmda");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.EnableCompatibilityExtensions = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "SITE UMASK\r\n" +
                "SITE UMASK 027\r\n" +
                "SITE MKDIR created/child\r\n" +
                "SITE CHMOD 640 chmod.txt\r\n" +
                "SITE RMDIR site-tree\r\n" +
                "RMDA rmda-tree\r\n" +
                "SITE UMASK 888\r\n" +
                "SITE CHMOD 999 chmod.txt\r\n" +
                "SITE RMDIR /\r\n" +
                "RMDA /\r\n" +
                "QUIT\r\n");
            string? initialUmask = reader.ReadLine();
            string? changedUmask = reader.ReadLine();
            string? mkdir = reader.ReadLine();
            string? chmod = reader.ReadLine();
            string? siteRmdir = reader.ReadLine();
            string? rmda = reader.ReadLine();
            string? invalidUmask = reader.ReadLine();
            string? invalidMode = reader.ReadLine();
            string? siteRoot = reader.ReadLine();
            string? rmdaRoot = reader.ReadLine();
            reader.ReadLine();
            int createdMode = 0;
            int chmodMode = 0;
            if (!OperatingSystem.IsWindows())
            {
                createdMode = (int)File.GetUnixFileMode(Path.Combine(
                    RootPath, "created", "child"));
                chmodMode = (int)File.GetUnixFileMode(chmodPath);
            }

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.SiteChmod));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.SiteUmask));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Contain(FtpFeatureNames.Rmda));
                Assert.That(initialUmask,
                    Is.EqualTo("200 Current UMASK is 022."));
                Assert.That(changedUmask,
                    Is.EqualTo("200 UMASK changed from 022 to 027."));
                Assert.That(mkdir, Does.StartWith("257 "));
                Assert.That(chmod,
                    Is.EqualTo("200 Permissions changed."));
                Assert.That(siteRmdir,
                    Is.EqualTo("250 Directory tree removed."));
                Assert.That(rmda,
                    Is.EqualTo("250 Directory tree removed."));
                Assert.That(invalidUmask, Does.StartWith("501 "));
                Assert.That(invalidMode, Does.StartWith("501 "));
                Assert.That(siteRoot, Does.StartWith("550 "));
                Assert.That(rmdaRoot, Does.StartWith("550 "));
                Assert.That(Directory.Exists(Path.Combine(
                    RootPath, "created", "child")), Is.True);
                Assert.That(createdMode, Is.EqualTo(488));
                Assert.That(chmodMode, Is.EqualTo(416));
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "site-tree")), Is.False);
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "rmda-tree")), Is.False);
                Assert.That(Directory.Exists(RootPath), Is.True);
                Assert.That(authorizer.Requests.Select(item => item.Operation),
                    Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.CreateDirectory,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.WriteMetadata,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.DeleteDirectory,
                        FtpAuthorizationOperation.DeleteDirectory,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Disabled_SITE_compatibility_is_not_advertised_or_executed()
    {
        string tree = Path.Combine(RootPath, "tree");
        Directory.CreateDirectory(tree);
        File.WriteAllText(Path.Combine(tree, "item.txt"), "content");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "SITE UMASK 027\r\n" +
                "SITE RMDIR tree\r\n" +
                "RMDA tree\r\n" +
                "QUIT\r\n");
            string? umask = reader.ReadLine();
            string? siteRmdir = reader.ReadLine();
            string? rmda = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.SiteChmod));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.SiteUmask));
                Assert.That(features.Features.Select(item => item.Name),
                    Does.Not.Contain(FtpFeatureNames.Rmda));
                Assert.That(umask, Does.StartWith("502 "));
                Assert.That(siteRmdir, Does.StartWith("502 "));
                Assert.That(rmda, Does.StartWith("502 "));
                Assert.That(Directory.Exists(tree), Is.True);
                Assert.That(authorizer.Requests.Select(item => item.Operation),
                    Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void SITE_and_RMDA_authorization_denial_prevents_mutation()
    {
        string file = Path.Combine(RootPath, "file.txt");
        File.WriteAllText(file, "content");
        string tree = Path.Combine(RootPath, "tree");
        Directory.CreateDirectory(tree);
        File.WriteAllText(Path.Combine(tree, "item.txt"), "content");
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = false;
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.EnableCompatibilityExtensions = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "SITE CHMOD 600 file.txt\r\n" +
                "SITE MKDIR denied/child\r\n" +
                "RMDA tree\r\n" +
                "QUIT\r\n");
            string? chmod = reader.ReadLine();
            string? mkdir = reader.ReadLine();
            string? rmda = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(chmod, Does.StartWith("550 "));
                Assert.That(mkdir, Does.StartWith("550 "));
                Assert.That(rmda, Does.StartWith("550 "));
                Assert.That(File.Exists(file), Is.True);
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "denied")), Is.False);
                Assert.That(Directory.Exists(tree), Is.True);
                Assert.That(authorizer.Requests.Select(item => item.Operation),
                    Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.Site,
                        FtpAuthorizationOperation.DeleteDirectory
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Derived_default_capabilities_and_authorization_denial_return_stable_results()
    {
        File.WriteAllText(Path.Combine(RootPath, "private.txt"), "private");
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = false;
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);

        try
        {
            server.StartServer(CreateOptions(port, null));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpReply featureReply = ReadMultilineReply(reader);
            WriteText(stream, "LANG fr\r\nOPTS UTF8 ON\r\n");
            string? language = reader.ReadLine();
            string? option = reader.ReadLine();
            LoginAfterGreeting(stream, reader);
            WriteText(stream,
                "SIZE private.txt\r\nSITE DENIED\r\nQUIT\r\n");
            string? metadata = reader.ReadLine();
            string? site = reader.ReadLine();
            reader.ReadLine();
            FtpFeatureSet features =
                FtpFeatureCodec.ParseFeatureReply(featureReply);

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Select(feature => feature.Name),
                    Is.EqualTo(new[]
                    {
                        "UTF8", "MDTM", "SIZE", "REST STREAM", "TVFS",
                        "MLST", "HOST"
                    }));
                Assert.That(features.Features.Select(feature => feature.Name),
                    Does.Not.Contain("AUTH"));
                Assert.That(language,
                    Is.EqualTo("502 Language negotiation is not available."));
                Assert.That(option,
                    Is.EqualTo("200 UTF8 ON"));
                Assert.That(metadata,
                    Is.EqualTo(
                        "550 The requested metadata was not available."));
                Assert.That(site,
                    Is.EqualTo("550 The SITE operation was not authorized."));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.Site
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void FEAT_advertises_TLS_commands_only_for_a_TLS_capable_listener()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer, null);
        FtpServerOptions options = CreateOptions(port, null);
        options.Listeners[0].SecurityMode =
            FtpSecurityMode.ExplicitTlsOpportunistic;
        options.Listeners[0].AllowPlaintextCredentials = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream, "FEAT\r\n");
            FtpFeatureSet features = FtpFeatureCodec.ParseFeatureReply(
                ReadMultilineReply(reader));
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(features.Features.Take(3).Select(feature =>
                    feature.Name), Is.EqualTo(new[]
                    {
                        "AUTH", "PBSZ", "PROT"
                    }));
                Assert.That(FindFeature(features, "AUTH").Parameters,
                    Is.EqualTo("TLS"));
                Assert.That(features.Features.Select(feature => feature.Name),
                    Does.Contain("UTF8"));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private FtpVirtualHostOptions CreateCapableHost()
    {
        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = "ftp.capable";
        host.Greeting = "Capable ready";
        host.RootPath = "/";
        host.Features.Features = new[]
        {
            CreateFeature("UTF8", null),
            CreateFeature("MLST", "type*;size;modify*;"),
            CreateFeature("LANG", "en*;fr"),
            CreateFeature("MDTM", null),
            CreateFeature("SIZE", null),
            CreateFeature("AUTH", "TLS"),
            CreateFeature("MODE Z", null),
            CreateFeature("X-CUSTOM", "enabled")
        };
        return host;
    }

    private static FtpFeature CreateFeature(
        string name,
        string? parameters)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = name;
        feature.Parameters = parameters;
        feature.RawLine = parameters == null ?
            " " + name : " " + name + " " + parameters;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    private static FtpFeature FindFeature(
        FtpFeatureSet features,
        string name)
    {
        return features.Features.Single(feature =>
            string.Equals(feature.Name, name, StringComparison.Ordinal));
    }

    private FtpServer CreateServer(
        IFtpAuthorizer authorizer,
        FtpVirtualHostOptions? host)
    {
        FtpVirtualHostOptions[] hosts = host == null ?
            Array.Empty<FtpVirtualHostOptions>() : new[] { host };
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new ScriptedServerAuthenticator(),
            authorizer,
            new DefaultFtpHostResolver(hosts),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink());
    }

    private static FtpServerOptions CreateOptions(
        int port,
        FtpVirtualHostOptions? host)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
        if (host != null)
        {
            options.VirtualHosts = new[] { host };
            options.DefaultHostName = host.HostName;
        }
        return options;
    }

    private static FtpReply ReadMultilineReply(StreamReader reader)
    {
        string? opening = reader.ReadLine();
        Assert.That(opening, Is.Not.Null);
        Assert.That(opening![3], Is.EqualTo('-'));
        string code = opening.Substring(0, 3);
        StringBuilder text = new StringBuilder();
        text.Append(opening);
        text.Append("\r\n");
        string? line = reader.ReadLine();
        while (line != null && !line.StartsWith(
            code + " ", StringComparison.Ordinal))
        {
            text.Append(line);
            text.Append("\r\n");
            line = reader.ReadLine();
        }
        Assert.That(line, Is.Not.Null);
        text.Append(line);
        text.Append("\r\n");
        return new FtpReplyParser(
            FtpDefaults.MaximumReplyBytes,
            FtpDefaults.MaximumReplyLines,
            true).ParseReplyText(text.ToString());
    }

    private void Login(Stream stream, StreamReader reader)
    {
        Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
        LoginAfterGreeting(stream, reader);
    }

    private static void LoginAfterGreeting(
        Stream stream,
        StreamReader reader)
    {
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
            stream, new UTF8Encoding(false, true), false, 1024, true);
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
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
