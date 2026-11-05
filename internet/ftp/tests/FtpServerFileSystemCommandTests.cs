using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerFileSystemCommandTests
{
    private string RootPath = "";

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(), "ftp-server-fs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
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
    public void Built_in_file_commands_normalize_paths_mutate_entries_and_enforce_rename_sequence()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "workspace"));
        Directory.CreateDirectory(Path.Combine(RootPath, "empty"));
        File.WriteAllText(Path.Combine(RootPath, "delete.txt"), "delete");
        File.WriteAllText(Path.Combine(RootPath, "rename.txt"), "rename");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream,
                "USER alice\r\n" +
                "PASS secret\r\n" +
                "ACCT billing\r\n" +
                "PWD\r\n" +
                "CWD workspace\r\n" +
                "PWD\r\n" +
                "CDUP\r\n" +
                "PWD\r\n" +
                "SMNT workspace\r\n" +
                "MKD made\"dir\r\n" +
                "DELE delete.txt\r\n" +
                "RMD empty\r\n" +
                "RNTO premature.txt\r\n" +
                "RNFR rename.txt\r\n" +
                "NOOP\r\n" +
                "RNTO interrupted.txt\r\n" +
                "RNFR rename.txt\r\n" +
                "RNTO renamed.txt\r\n" +
                "CWD renamed.txt\r\n" +
                "QUIT\r\n");

            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? root = reader.ReadLine();
            string? cwd = reader.ReadLine();
            string? workspace = reader.ReadLine();
            string? parent = reader.ReadLine();
            string? parentPath = reader.ReadLine();
            string? mount = reader.ReadLine();
            string? create = reader.ReadLine();
            string? delete = reader.ReadLine();
            string? remove = reader.ReadLine();
            string? premature = reader.ReadLine();
            string? renameFrom = reader.ReadLine();
            string? interrupt = reader.ReadLine();
            string? interrupted = reader.ReadLine();
            string? secondRenameFrom = reader.ReadLine();
            string? renameTo = reader.ReadLine();
            string? fileAsDirectory = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(root,
                    Is.EqualTo("257 \"/\" is the current directory."));
                Assert.That(cwd,
                    Is.EqualTo("250 Directory successfully changed."));
                Assert.That(workspace,
                    Is.EqualTo(
                        "257 \"/workspace\" is the current directory."));
                Assert.That(parent,
                    Is.EqualTo("200 Directory changed to parent."));
                Assert.That(parentPath,
                    Is.EqualTo("257 \"/\" is the current directory."));
                Assert.That(mount,
                    Is.EqualTo("250 Structure mount accepted."));
                Assert.That(create,
                    Is.EqualTo(
                        "257 \"/made\"\"dir\" directory created."));
                Assert.That(delete, Is.EqualTo("250 File deleted."));
                Assert.That(remove, Is.EqualTo("250 Directory removed."));
                Assert.That(premature, Does.StartWith("503 "));
                Assert.That(renameFrom,
                    Is.EqualTo(
                        "350 File exists; ready for destination name."));
                Assert.That(interrupt,
                    Is.EqualTo("200 NOOP command successful."));
                Assert.That(interrupted, Does.StartWith("503 "));
                Assert.That(secondRenameFrom,
                    Is.EqualTo(
                        "350 File exists; ready for destination name."));
                Assert.That(renameTo, Is.EqualTo("250 Rename successful."));
                Assert.That(fileAsDirectory, Does.StartWith("550 "));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "made\"dir")), Is.True);
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "delete.txt")), Is.False);
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "empty")), Is.False);
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "rename.txt")), Is.False);
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "renamed.txt")), Is.True);
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.CreateDirectory,
                        FtpAuthorizationOperation.DeleteFile,
                        FtpAuthorizationOperation.DeleteDirectory,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.Rename,
                        FtpAuthorizationOperation.ReadMetadata
                    }));
                Assert.That(authorizer.Requests[11].Path,
                    Is.EqualTo("/rename.txt"));
                Assert.That(authorizer.Requests[11].DestinationPath,
                    Is.EqualTo("/renamed.txt"));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Authorization_denial_prevents_file_system_mutation_and_returns_generic_failures()
    {
        File.WriteAllText(Path.Combine(RootPath, "keep.txt"), "keep");
        RecordingFtpAuthorizer authorizer = new RecordingFtpAuthorizer();
        authorizer.Decision.Handled = true;
        authorizer.Decision.Allowed = false;
        authorizer.Decision.Reason = "Private policy detail.";
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "MKD denied\r\n" +
                "RNFR keep.txt\r\n" +
                "RNTO moved.txt\r\n" +
                "DELE keep.txt\r\n" +
                "PWD\r\n" +
                "QUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? create = reader.ReadLine();
            string? renameFrom = reader.ReadLine();
            string? renameTo = reader.ReadLine();
            string? delete = reader.ReadLine();
            string? pwd = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(create,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(renameFrom,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(renameTo, Does.StartWith("503 "));
                Assert.That(delete,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(pwd,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "denied")), Is.False);
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "keep.txt")), Is.True);
                Assert.That(File.Exists(
                    Path.Combine(RootPath, "moved.txt")), Is.False);
                Assert.That(authorizer.Requests, Has.Count.EqualTo(4));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Missing_entries_and_invalid_directory_targets_return_550_without_state_changes()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "nonempty"));
        File.WriteAllText(Path.Combine(RootPath, "nonempty", "item"), "x");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "CWD missing\r\n" +
                "PWD\r\n" +
                "RNFR missing\r\n" +
                "RMD nonempty\r\n" +
                "QUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? cwd = reader.ReadLine();
            string? pwd = reader.ReadLine();
            string? rename = reader.ReadLine();
            string? remove = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(cwd, Does.StartWith("550 "));
                Assert.That(pwd,
                    Is.EqualTo("257 \"/\" is the current directory."));
                Assert.That(rename, Does.StartWith("550 "));
                Assert.That(remove, Does.StartWith("550 "));
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "nonempty")), Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Historic_aliases_are_disabled_by_default()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "workspace"));
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "XCWD workspace\r\nPWD\r\nQUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? alias = reader.ReadLine();
            string? directory = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(alias,
                    Is.EqualTo("502 Historic FTP command aliases are disabled."));
                Assert.That(directory,
                    Is.EqualTo("257 \"/\" is the current directory."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Enabled_historic_aliases_route_to_standard_handlers_and_obsolete_commands_are_rejected()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "workspace"));
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.EnableHistoricAliases = true;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "XPWD\r\nXMKD made\r\nXCWD workspace\r\nXPWD\r\n" +
                "XCUP\r\nXRMD made\r\n" +
                "LPRT obsolete\r\nLPSV\r\nALGS endpoint\r\nQUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? root = reader.ReadLine();
            string? created = reader.ReadLine();
            string? changed = reader.ReadLine();
            string? workspace = reader.ReadLine();
            string? parent = reader.ReadLine();
            string? removed = reader.ReadLine();
            string? lprt = reader.ReadLine();
            string? lpsv = reader.ReadLine();
            string? algs = reader.ReadLine();
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(root,
                    Is.EqualTo("257 \"/\" is the current directory."));
                Assert.That(created,
                    Is.EqualTo("257 \"/made\" directory created."));
                Assert.That(changed,
                    Is.EqualTo("250 Directory successfully changed."));
                Assert.That(workspace,
                    Is.EqualTo("257 \"/workspace\" is the current directory."));
                Assert.That(parent,
                    Is.EqualTo("200 Directory changed to parent."));
                Assert.That(removed, Is.EqualTo("250 Directory removed."));
                Assert.That(lprt,
                    Is.EqualTo("502 The obsolete FTP long-address command is not supported."));
                Assert.That(lpsv,
                    Is.EqualTo("502 The obsolete FTP long-address command is not supported."));
                Assert.That(algs,
                    Is.EqualTo("502 ALGS is not applicable to this FTP endpoint."));
                Assert.That(Directory.Exists(
                    Path.Combine(RootPath, "made")), Is.False);
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.CreateDirectory,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.DeleteDirectory,
                    }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void File_system_provider_fault_is_isolated_audited_and_control_session_survives()
    {
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            new ScriptedServerAuthenticator(),
            authorizer,
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            events);
        int port = ReserveAvailablePort();

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            reader.ReadLine();
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n" +
                "CWD unavailable\r\nPWD\r\nQUIT\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            string? cwd = reader.ReadLine();
            string? pwd = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(cwd,
                    Is.EqualTo(
                        "550 The requested file action was not taken."));
                Assert.That(pwd,
                    Is.EqualTo("257 \"/\" is the current directory."));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(events.GetEvents().Any(eventData =>
                    eventData.Kind == FtpEventKind.Fault &&
                    eventData.Message ==
                        "The isolated FTP file-system provider operation failed."),
                    Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Traversal_and_unicode_separator_ambiguity_cannot_escape_or_alias_paths()
    {
        string outsideName =
            "outside-" + Guid.NewGuid().ToString("N") + ".txt";
        string outsidePath = Path.Combine(
            Directory.GetParent(RootPath)!.FullName, outsideName);
        string nestedPath = Path.Combine(RootPath, "nested");
        string nestedFile = Path.Combine(nestedPath, "target.txt");
        string ambiguousName = "nested\u2215target.txt";
        string ambiguousPath = Path.Combine(RootPath, ambiguousName);
        File.WriteAllText(outsidePath, "outside");
        Directory.CreateDirectory(nestedPath);
        File.WriteAllText(nestedFile, "nested");
        File.WriteAllText(ambiguousPath, "ambiguous");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);

        try
        {
            server.StartServer(CreateOptions(port));
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteText(stream,
                "USER alice\r\nPASS secret\r\nACCT billing\r\n");
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            WriteUtf8Text(stream,
                "DELE ../" + outsideName + "\r\n" +
                "DELE " + ambiguousName + "\r\n" +
                "RMD /\r\n" +
                "QUIT\r\n");
            string? traversal = reader.ReadLine();
            string? ambiguous = reader.ReadLine();
            string? root = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(traversal, Does.StartWith("550 "));
                Assert.That(ambiguous, Is.EqualTo("250 File deleted."));
                Assert.That(root, Does.StartWith("550 "));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(File.ReadAllText(outsidePath),
                    Is.EqualTo("outside"));
                Assert.That(File.ReadAllText(nestedFile),
                    Is.EqualTo("nested"));
                Assert.That(File.Exists(ambiguousPath), Is.False);
                Assert.That(Directory.Exists(RootPath), Is.True);
            });
        }
        finally
        {
            server.StopServer();
            if (File.Exists(outsidePath))
            {
                File.Delete(outsidePath);
            }
        }
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
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new ScriptedServerAuthenticator(),
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

    private static void WriteUtf8Text(Stream stream, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
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
