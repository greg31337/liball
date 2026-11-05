using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpInProcessClientServerTests
{
    private string RootPath = "";
    private X509Certificate2 Certificate = null!;

    private void RequireCertificate()
    {
        if (Certificate != null)
        {
            return;
        }
        Certificate = FtpTlsStreamTests.CreateCertificate();
    }

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-in-process-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(Path.Combine(RootPath, "docs"));
        File.WriteAllText(
            Path.Combine(RootPath, "root.txt"),
            "root-content",
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(RootPath, "docs", "item.txt"),
            "nested-content",
            Encoding.UTF8);
    }

    [TearDown]
    public void TearDown()
    {
        Certificate?.Dispose();
        Certificate = null!;
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void Plain_IPv4_passive_runs_every_listing_path_and_round_trip_transfers()
    {
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Passive);
            try
            {
                FtpListing adaptive = client.ListDirectory("/");
                FtpListing machine = client.ListMachineDirectory("/");
                FtpListing human = client.ListHumanDirectory("/");
                FtpListing names = client.ListNames("/");
                FtpListing status = client.ListStatus("/");
                FtpPathProbeResult metadata = client.GetMetadata("/root.txt");
                AssertRoundTrip(client, "passive");

                Assert.Multiple(() =>
                {
                    AssertListingContains(adaptive, "root.txt");
                    AssertListingContains(machine, "root.txt");
                    AssertListingContains(human, "root.txt");
                    AssertListingContains(names, "root.txt");
                    AssertListingContains(status, "root.txt");
                    Assert.That(adaptive.MachineReadable, Is.True);
                    Assert.That(machine.MachineReadable, Is.True);
                    Assert.That(human.MachineReadable, Is.False);
                    Assert.That(names.FormatName, Is.EqualTo("Names"));
                    Assert.That(metadata.Succeeded, Is.True);
                    Assert.That(metadata.Entry, Is.Not.Null);
                    Assert.That(metadata.Entry!.Name, Is.EqualTo("/root.txt"));
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Plain_IPv4_active_runs_upload_and_download_against_the_library_server()
    {
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Active);
            try
            {
                AssertRoundTrip(client, "active");
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Plain_IPv6_active_runs_a_round_trip_when_IPv6_is_available()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are unavailable on this platform.");
        }
        IPAddress address = IPAddress.IPv6Loopback;
        int controlPort;
        try
        {
            controlPort = ReserveAvailablePort(address);
        }
        catch (SocketException)
        {
            Assert.Ignore("IPv6 loopback binding is unavailable on this platform.");
            return;
        }
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Active);
            try
            {
                AssertRoundTrip(client, "ipv6");
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Explicit_FTPS_switches_PROT_P_and_C_then_clears_the_control_channel()
    {
        RequireCertificate();
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(
            FtpSecurityMode.ExplicitTlsRequired);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.ExplicitTlsRequired));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.ExplicitTlsRequired,
                FtpDataConnectionMode.Passive);
            try
            {
                FtpCommandResult privateProtection = client.SetDataProtection(
                    FtpDataProtectionLevel.Private);
                FtpTransferResult privateUpload = Upload(
                    client, "/private.bin", "private-data");
                FtpCommandResult clearProtection = client.SetDataProtection(
                    FtpDataProtectionLevel.Clear);
                string clearDownload = Download(client, "/private.bin");
                FtpCommandResult clearControl = client.ClearControlTls();
                FtpCommandResult probe = client.SendNoOperation();

                Assert.Multiple(() =>
                {
                    Assert.That(privateProtection.Succeeded, Is.True);
                    Assert.That(privateUpload.Succeeded, Is.True);
                    Assert.That(clearProtection.Succeeded, Is.True);
                    Assert.That(clearDownload, Is.EqualTo("private-data"));
                    Assert.That(clearControl.Succeeded, Is.True);
                    Assert.That(probe.Succeeded, Is.True);
                    Assert.That(client.State.Connection!.ControlProtected,
                        Is.False);
                    Assert.That(client.GetSessionState(),
                        Is.EqualTo(FtpSessionState.Authenticated));
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Implicit_FTPS_uses_private_passive_data_for_listing_and_transfer()
    {
        RequireCertificate();
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.ImplicitTls);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.ImplicitTls));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.ImplicitTls,
                FtpDataConnectionMode.Passive);
            try
            {
                FtpCommandResult protection = client.SetDataProtection(
                    FtpDataProtectionLevel.Private);
                FtpListing listing = client.ListMachineDirectory("/");
                AssertRoundTrip(client, "implicit");

                Assert.Multiple(() =>
                {
                    Assert.That(protection.Succeeded, Is.True);
                    AssertListingContains(listing, "root.txt");
                    Assert.That(listing.MachineReadable, Is.True);
                    Assert.That(client.State.Connection!.ControlProtected,
                        Is.True);
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Round_trips_zero_text_binary_record_page_and_compressed_content()
    {
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Passive);
            try
            {
                FtpTransferOptions image = CreateTransferOptions(
                    FtpTransferType.Image,
                    FtpFileStructure.File,
                    FtpTransferMode.Stream);
                FtpTransferOptions text = CreateTransferOptions(
                    FtpTransferType.Ascii,
                    FtpFileStructure.File,
                    FtpTransferMode.Stream);
                FtpTransferOptions record = CreateTransferOptions(
                    FtpTransferType.Ascii,
                    FtpFileStructure.Record,
                    FtpTransferMode.Compressed);
                FtpTransferOptions page = CreateTransferOptions(
                    FtpTransferType.Image,
                    FtpFileStructure.Page,
                    FtpTransferMode.Block);
                FtpTransferOptions deflate = CreateTransferOptions(
                    FtpTransferType.Image,
                    FtpFileStructure.File,
                    FtpTransferMode.Deflate);
                byte[] binary = Enumerable.Range(0, 256)
                    .Select(value => (byte)value)
                    .Concat(new byte[] { 0, 255, 0, 255 })
                    .ToArray();
                byte[] textBytes = Encoding.ASCII.GetBytes(
                    "one\ntwo\rthree\n");
                byte[] records = Encoding.ASCII.GetBytes(
                    "first\nsecond\n");
                byte[] pages = new byte[]
                {
                    4, 1, 3, 1, 10, 20, 30,
                    4, 0, 0, 0,
                };
                byte[] compressible = Enumerable.Repeat((byte)65, 8192)
                    .Concat(binary)
                    .ToArray();

                AssertRoundTripBytes(
                    client, "/zero.bin", Array.Empty<byte>(), image);
                AssertRoundTripBytes(
                    client, "/text.txt", textBytes, text);
                AssertRoundTripBytes(
                    client, "/binary.bin", binary, image);
                AssertRoundTripBytes(
                    client, "/records.txt", records, record);
                AssertRoundTripBytes(
                    client, "/pages.bin", pages, page);
                AssertRoundTripBytes(
                    client, "/deflate.bin", compressible, deflate);
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Large_sparse_offset_resumes_upload_and_download_without_truncation()
    {
        long offset = (long)int.MaxValue + 4096;
        byte[] tail = Encoding.ASCII.GetBytes("large-offset-tail");
        string sourcePath = Path.Combine(RootPath, "sparse-source.local");
        string remotePath = Path.Combine(RootPath, "large.remote");
        string destinationPath =
            Path.Combine(RootPath, "sparse-download.local");
        CreateSparseFile(sourcePath, offset, tail);
        CreateSparseFile(remotePath, offset, Array.Empty<byte>());
        CreateSparseFile(destinationPath, offset, Array.Empty<byte>());
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Passive);
            try
            {
                FtpTransferRequest upload = new FtpTransferRequest();
                upload.Direction = FtpTransferDirection.Upload;
                upload.RemotePath = "/large.remote";
                upload.LocalPath = sourcePath;
                upload.HasRestartOffset = true;
                upload.RestartOffset = offset;
                upload.Options.ResumePolicy = FtpResumePolicy.Required;
                upload.Options.VerificationMode = FtpVerificationMode.None;
                FtpTransferResult uploaded = client.Transfer(upload);

                FtpTransferRequest download = new FtpTransferRequest();
                download.Direction = FtpTransferDirection.Download;
                download.RemotePath = "/large.remote";
                download.LocalPath = destinationPath;
                download.HasRestartOffset = true;
                download.RestartOffset = offset;
                download.Options.ResumePolicy = FtpResumePolicy.Required;
                download.Options.VerificationMode = FtpVerificationMode.None;
                FtpTransferResult downloaded = client.Transfer(download);

                Assert.Multiple(() =>
                {
                    Assert.That(uploaded.Succeeded, Is.True,
                        uploaded.FailureMessage);
                    Assert.That(uploaded.RestartMarker,
                        Is.EqualTo(offset.ToString()));
                    Assert.That(downloaded.Succeeded, Is.True,
                        downloaded.FailureMessage);
                    Assert.That(downloaded.RestartMarker,
                        Is.EqualTo(offset.ToString()));
                    Assert.That(new FileInfo(remotePath).Length,
                        Is.EqualTo(offset + tail.Length));
                    Assert.That(new FileInfo(destinationPath).Length,
                        Is.EqualTo(offset + tail.Length));
                    Assert.That(ReadTail(remotePath, offset, tail.Length),
                        Is.EqualTo(tail));
                    Assert.That(ReadTail(
                        destinationPath, offset, tail.Length),
                        Is.EqualTo(tail));
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Atomic_verified_and_recursive_transfers_commit_complete_trees()
    {
        string localSource = Path.Combine(RootPath, "local-tree-source");
        string localDestination =
            Path.Combine(RootPath, "local-tree-download");
        Directory.CreateDirectory(Path.Combine(localSource, "nested"));
        Directory.CreateDirectory(Path.Combine(localSource, "empty"));
        File.WriteAllText(
            Path.Combine(localSource, "root.bin"), "root-tree-content");
        File.WriteAllText(
            Path.Combine(localSource, "nested", "child.bin"),
            "child-tree-content");
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Passive);
            try
            {
                byte[] atomicContent =
                    Encoding.UTF8.GetBytes("atomic-verified-content");
                using MemoryStream atomicSource =
                    new MemoryStream(atomicContent);
                FtpTransferRequest atomic = new FtpTransferRequest();
                atomic.Direction = FtpTransferDirection.Upload;
                atomic.RemotePath = "/atomic.bin";
                atomic.Content = atomicSource;
                atomic.Options.AtomicUpload = true;
                atomic.Options.VerificationMode = FtpVerificationMode.Size;
                FtpTransferResult atomicResult = client.Transfer(atomic);

                FtpDirectoryTransferRequest uploadTree =
                    new FtpDirectoryTransferRequest();
                uploadTree.Direction = FtpTransferDirection.Upload;
                uploadTree.LocalPath = localSource;
                uploadTree.RemotePath = "/round-tree";
                uploadTree.Options.Transfer.VerificationMode =
                    FtpVerificationMode.None;
                FtpDirectoryTransferResult uploadedTree =
                    client.TransferDirectory(uploadTree);

                FtpDirectoryTransferRequest downloadTree =
                    new FtpDirectoryTransferRequest();
                downloadTree.Direction = FtpTransferDirection.Download;
                downloadTree.LocalPath = localDestination;
                downloadTree.RemotePath = "/round-tree";
                downloadTree.Options.Transfer.VerificationMode =
                    FtpVerificationMode.None;
                FtpDirectoryTransferResult downloadedTree =
                    client.TransferDirectory(downloadTree);

                Assert.Multiple(() =>
                {
                    Assert.That(atomicResult.Succeeded, Is.True,
                        atomicResult.FailureMessage);
                    Assert.That(atomicResult.VerificationAttempted, Is.True);
                    Assert.That(atomicResult.Verified, Is.True);
                    Assert.That(File.ReadAllBytes(
                        Path.Combine(RootPath, "atomic.bin")),
                        Is.EqualTo(atomicContent));
                    Assert.That(Directory.GetFiles(RootPath, "*.tmp"),
                        Is.Empty);
                    Assert.That(uploadedTree.Succeeded, Is.True,
                        uploadedTree.FailureMessage);
                    Assert.That(downloadedTree.Succeeded, Is.True,
                        downloadedTree.FailureMessage);
                    Assert.That(File.ReadAllText(Path.Combine(
                        localDestination, "root.bin")),
                        Is.EqualTo("root-tree-content"));
                    Assert.That(File.ReadAllText(Path.Combine(
                        localDestination, "nested", "child.bin")),
                        Is.EqualTo("child-tree-content"));
                    Assert.That(Directory.Exists(Path.Combine(
                        localDestination, "empty")), Is.True);
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Failed_and_cancelled_transfers_report_outcomes_and_release_resources()
    {
        IPAddress address = IPAddress.Loopback;
        int controlPort = ReserveAvailablePort(address);
        int passivePort = ReserveDifferentPort(address, controlPort);
        FtpServer server = CreateServer(FtpSecurityMode.Plain);

        try
        {
            server.StartServer(CreateServerOptions(
                address,
                controlPort,
                passivePort,
                FtpSecurityMode.Plain));
            FtpClient client = ConnectClient(
                address,
                controlPort,
                FtpSecurityMode.Plain,
                FtpDataConnectionMode.Passive);
            using CancellationTokenSource cancellation =
                new CancellationTokenSource();
            try
            {
                using MemoryStream missingDestination = new MemoryStream();
                FtpTransferRequest missing = new FtpTransferRequest();
                missing.Direction = FtpTransferDirection.Download;
                missing.RemotePath = "/missing.bin";
                missing.Content = missingDestination;
                missing.Options.VerificationMode = FtpVerificationMode.None;
                FtpTransferResult failed = client.Transfer(missing);

                using MemoryStream upload =
                    new MemoryStream(new byte[1024 * 1024]);
                FtpTransferRequest canceled = new FtpTransferRequest();
                canceled.Direction = FtpTransferDirection.Upload;
                canceled.RemotePath = "/cancelled.bin";
                canceled.Content = upload;
                canceled.Options.BufferBytes = 1024;
                canceled.Options.VerificationMode = FtpVerificationMode.None;
                canceled.Options.Timeouts.CancellationToken =
                    cancellation.Token;
                canceled.Observer = new CancelingTransferObserver(cancellation);
                OperationCanceledException? cancellationFailure =
                    Assert.Throws<OperationCanceledException>(() =>
                        client.Transfer(canceled));

                Assert.Multiple(() =>
                {
                    Assert.That(failed.Succeeded, Is.False);
                    Assert.That(failed.FailureMessage,
                        Does.Contain("RETR"));
                    Assert.That(missingDestination.Length, Is.Zero);
                    Assert.That(cancellationFailure, Is.Not.Null);
                    Assert.That(cancellation.IsCancellationRequested, Is.True);
                    Assert.That(client.GetSessionState(),
                        Is.EqualTo(FtpSessionState.Closed));
                    Assert.That(SpinWait.SpinUntil(() =>
                        server.Runtime.Limits!.GetTransferCount() == 0,
                        3000), Is.True);
                    Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                        Is.Zero);
                });
            }
            finally
            {
                client.Disconnect();
            }
        }
        finally
        {
            server.StopServer();
        }
    }

    private FtpServer CreateServer(FtpSecurityMode securityMode)
    {
        FtpFixedUserOptions users = CreateUsers();
        IFtpCertificateProvider certificateProvider;
        if (securityMode == FtpSecurityMode.Plain)
        {
            certificateProvider = new UnavailableCertificateProvider();
        }
        else
        {
            FtpTlsOptions certificateOptions = new FtpTlsOptions();
            certificateOptions.ServerCertificate = Certificate;
            certificateProvider =
                new DefaultFtpCertificateProvider(certificateOptions);
        }
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new FixedUserFtpAuthenticator(users),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            certificateProvider,
            new NullFtpEventSink());
    }

    private FtpServerOptions CreateServerOptions(
        IPAddress address,
        int controlPort,
        int passivePort,
        FtpSecurityMode securityMode)
    {
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = address.ToString();
        listener.Endpoint.Port = controlPort;
        listener.Endpoint.AddressFamily = address.AddressFamily ==
            AddressFamily.InterNetwork ?
                FtpAddressFamily.IPv4 : FtpAddressFamily.IPv6;
        listener.SecurityMode = securityMode;
        listener.DualStack = false;
        listener.AllowPlaintextCredentials =
            securityMode == FtpSecurityMode.Plain;
        if (securityMode != FtpSecurityMode.Plain)
        {
            listener.Tls.ServerCertificate = Certificate;
        }
        listener.Tls.CheckCertificateRevocation = false;
        listener.Tls.AllowClearCommandChannel = true;
        listener.Tls.RequireDataSessionReuse = false;
        FtpServerOptions options = new FtpServerOptions();
        options.Listeners = new[] { listener };
        if (securityMode != FtpSecurityMode.Plain)
        {
            options.Tls.ServerCertificate = Certificate;
        }
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.AllowClearCommandChannel = true;
        options.Tls.RequireDataSessionReuse = false;
        options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
        options.DataConnection.PassivePortStart = passivePort;
        options.DataConnection.PassivePortEnd = passivePort;
        options.EnableModeZ = true;
        return options;
    }

    private static FtpFixedUserOptions CreateUsers()
    {
        byte[] salt = Encoding.UTF8.GetBytes("in-process-salt");
        byte[] password = Encoding.UTF8.GetBytes("secret");
        byte[] salted = new byte[salt.Length + password.Length];
        Buffer.BlockCopy(salt, 0, salted, 0, salt.Length);
        Buffer.BlockCopy(password, 0, salted, salt.Length, password.Length);
        FtpUserRecord user = new FtpUserRecord();
        user.UserName = "alice";
        user.Salt = salt;
        user.PasswordVerifier = SHA256.HashData(salted);
        user.HomeDirectory = "/";
        user.Disabled = false;
        user.AllowWrite = true;
        FtpFixedUserOptions users = new FtpFixedUserOptions();
        users.Users = new[] { user };
        users.RequireProtectedControl = false;
        users.FailureDelayMilliseconds = 0;
        return users;
    }

    private static FtpClient ConnectClient(
        IPAddress address,
        int port,
        FtpSecurityMode securityMode,
        FtpDataConnectionMode dataMode)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = address.ToString();
        options.Endpoint.Port = port;
        options.Endpoint.AddressFamily = address.AddressFamily ==
            AddressFamily.InterNetwork ?
                FtpAddressFamily.IPv4 : FtpAddressFamily.IPv6;
        options.SecurityMode = securityMode;
        options.AllowPlaintextCredentials =
            securityMode == FtpSecurityMode.Plain;
        options.Credentials.UserName = "alice";
        options.Credentials.Password = "secret";
        options.DataConnection.Mode = dataMode;
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.AllowClearCommandChannel = true;
        options.Tls.RequireDataSessionReuse = false;
        FtpClient client = new FtpClient(
            securityMode == FtpSecurityMode.Plain ?
                null : new AcceptingCertificateValidator());
        client.Connect(options);
        Assert.That(client.GetSessionState(),
            Is.EqualTo(FtpSessionState.Authenticated));
        return client;
    }

    private static void AssertRoundTrip(FtpClient client, string name)
    {
        string content = name + "-round-trip";
        FtpTransferResult upload = Upload(
            client, "/" + name + ".bin", content);
        string downloaded = Download(client, "/" + name + ".bin");
        Assert.Multiple(() =>
        {
            Assert.That(upload.Succeeded, Is.True);
            Assert.That(downloaded, Is.EqualTo(content));
        });
    }

    private static FtpTransferOptions CreateTransferOptions(
        FtpTransferType type,
        FtpFileStructure structure,
        FtpTransferMode mode)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.Structure = structure;
        options.Mode = mode;
        options.VerificationMode = FtpVerificationMode.None;
        options.BufferBytes = 257;
        return options;
    }

    private static void AssertRoundTripBytes(
        FtpClient client,
        string remotePath,
        byte[] content,
        FtpTransferOptions options)
    {
        using MemoryStream source = new MemoryStream(content);
        FtpTransferRequest upload = new FtpTransferRequest();
        upload.Direction = FtpTransferDirection.Upload;
        upload.RemotePath = remotePath;
        upload.Content = source;
        upload.Options = options;
        FtpTransferResult uploaded = client.Transfer(upload);
        using MemoryStream destination = new MemoryStream();
        FtpTransferRequest download = new FtpTransferRequest();
        download.Direction = FtpTransferDirection.Download;
        download.RemotePath = remotePath;
        download.Content = destination;
        download.Options = options;
        FtpTransferResult downloaded = client.Transfer(download);

        Assert.Multiple(() =>
        {
            Assert.That(uploaded.Succeeded, Is.True,
                uploaded.FailureMessage);
            Assert.That(downloaded.Succeeded, Is.True,
                downloaded.FailureMessage);
            Assert.That(destination.ToArray(), Is.EqualTo(content));
        });
    }

    private static void CreateSparseFile(
        string path,
        long offset,
        byte[] tail)
    {
        using FileStream stream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.Read);
        stream.SetLength(offset);
        stream.Seek(offset, SeekOrigin.Begin);
        stream.Write(tail, 0, tail.Length);
    }

    private static byte[] ReadTail(string path, long offset, int count)
    {
        byte[] tail = new byte[count];
        using FileStream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Seek(offset, SeekOrigin.Begin);
        int read = stream.Read(tail, 0, tail.Length);
        Assert.That(read, Is.EqualTo(tail.Length));
        return tail;
    }

    private static FtpTransferResult Upload(
        FtpClient client,
        string remotePath,
        string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        MemoryStream source = new MemoryStream(bytes);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = remotePath;
        request.Content = source;
        request.Options.VerificationMode = FtpVerificationMode.None;
        FtpTransferResult result = client.Transfer(request);
        source.Dispose();
        return result;
    }

    private static string Download(FtpClient client, string remotePath)
    {
        using MemoryStream destination = new MemoryStream();
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Download;
        request.RemotePath = remotePath;
        request.Content = destination;
        request.Options.VerificationMode = FtpVerificationMode.None;
        FtpTransferResult result = client.Transfer(request);
        Assert.That(result.Succeeded, Is.True, result.FailureMessage);
        return Encoding.UTF8.GetString(destination.ToArray());
    }

    private static void AssertListingContains(
        FtpListing listing,
        string name)
    {
        Assert.That(listing.Complete, Is.True, listing.FailureMessage);
        Assert.That(listing.Entries.Select(entry => entry.Name),
            Does.Contain(name));
    }

    private static int ReserveAvailablePort(IPAddress address)
    {
        using TcpListener reservation = new TcpListener(address, 0);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            reservation.Server.DualMode = false;
        }
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static int ReserveDifferentPort(IPAddress address, int excluded)
    {
        int port = ReserveAvailablePort(address);
        while (port == excluded)
        {
            port = ReserveAvailablePort(address);
        }
        return port;
    }

    private sealed class AcceptingCertificateValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            FtpCertificateValidationResult result =
                new FtpCertificateValidationResult();
            result.Handled = true;
            result.Accepted = request.Certificate != null;
            return result;
        }
    }

    private sealed class CancelingTransferObserver : IFtpTransferObserver
    {
        public CancellationTokenSource Cancellation;

        public CancelingTransferObserver(
            CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
        }

        public void ObserveTransferStarted(FtpTransferProgress progress)
        {
        }

        public void ObserveTransferProgress(FtpTransferProgress progress)
        {
            if (progress.LogicalBytes != 0)
            {
                Cancellation.Cancel();
            }
        }

        public void ObserveTransferCompleted(FtpTransferResult result)
        {
        }
    }

    private sealed class UnavailableCertificateProvider : IFtpCertificateProvider
    {
        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            FtpCertificateResult result = new FtpCertificateResult();
            result.Handled = true;
            result.Succeeded = false;
            result.FailureMessage =
                "The plain FTP test does not provide a TLS certificate.";
            return result;
        }
    }
}
