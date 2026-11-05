using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerListingCommandTests
{
    private string RootPath = "";
    private FtpResourceLimits UserLimits = new FtpResourceLimits();

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-listing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        UserLimits = new FtpResourceLimits();
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
    public void Listing_commands_emit_parseable_UTF8_data_and_control_replies()
    {
        string unicodeName = "résumé.txt";
        File.WriteAllText(
            Path.Combine(RootPath, unicodeName), "content", Encoding.UTF8);
        Directory.CreateDirectory(Path.Combine(RootPath, "folder"));
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

            string names = DownloadPassive(stream, reader, "NLST");
            string human = DownloadPassive(stream, reader, "LIST");
            string machine = DownloadPassive(stream, reader, "MLSD");

            WriteText(stream, "STAT\r\n");
            FtpReply status = ReadMultilineReply(reader);
            WriteText(stream, "MLST " + unicodeName + "\r\n");
            FtpReply metadata = ReadMultilineReply(reader);
            WriteText(stream, "QUIT\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("221 "));

            FtpListing humanListing = FtpHumanListingParser.ParseListing(
                SplitListingLines(human));
            FtpListing machineListing =
                FtpMlsxCodec.ParseDirectoryText(machine, true);
            FtpEntry metadataEntry =
                FtpMlsxCodec.ParseControlReply(metadata);

            Assert.Multiple(() =>
            {
                Assert.That(SplitListingLines(names),
                    Is.EqualTo(new[] { "folder", unicodeName }));
                Assert.That(humanListing.FormatName, Is.EqualTo("Unix"));
                Assert.That(humanListing.Entries.Select(entry => entry.Name),
                    Is.EqualTo(new[] { "folder", unicodeName }));
                Assert.That(machineListing.Entries.Select(entry => entry.Name),
                    Is.EqualTo(new[] { "folder", unicodeName }));
                Assert.That(machineListing.Entries,
                    Has.All.Matches<FtpEntry>(entry => entry.Facts.Length > 0));
                Assert.That(status.Code, Is.EqualTo(212));
                Assert.That(status.Lines.Skip(1).Take(2)
                    .Select(line => FtpUnixListingParser.ParseLine(line.RawText))
                    .All(parsed => parsed.Matched), Is.True);
                Assert.That(metadata.Code, Is.EqualTo(250));
                Assert.That(metadataEntry.Name,
                    Is.EqualTo("/" + unicodeName));
                Assert.That(metadataEntry.Type, Is.EqualTo(FtpEntryType.File));
                Assert.That(authorizer.Requests.Count(request =>
                    request.Operation == FtpAuthorizationOperation.List),
                    Is.EqualTo(12));
                Assert.That(authorizer.Requests.Count(request =>
                    request.Operation ==
                        FtpAuthorizationOperation.ReadMetadata),
                    Is.EqualTo(1));
                Assert.That(authorizer.Requests.Count(request =>
                    request.Entry != null), Is.EqualTo(8));
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
    public void Listings_preserve_selected_facts_permissions_timestamps_and_symlinks()
    {
        string unicodeName = "résumé report.txt";
        string filePath = Path.Combine(RootPath, unicodeName);
        string linkName = "shortcut";
        string linkPath = Path.Combine(RootPath, linkName);
        File.WriteAllText(filePath, "content", Encoding.UTF8);
        DateTime modified = new DateTime(
            2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(filePath, modified);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                filePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        try
        {
            File.CreateSymbolicLink(linkPath, filePath);
        }
        catch (Exception exception) when (
            exception is PlatformNotSupportedException ||
            exception is UnauthorizedAccessException ||
            exception is IOException)
        {
            Assert.Ignore(
                "Symbolic links are unavailable: " + exception.Message);
        }
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
            WriteText(stream,
                "OPTS MLST type;size;modify;perm;\r\n");
            Assert.That(reader.ReadLine(),
                Is.EqualTo("200 MLST OPTS type;size;modify;perm;"));
            string names = DownloadPassive(stream, reader, "NLST");
            string human = DownloadPassive(stream, reader, "LIST");
            string machine = DownloadPassive(stream, reader, "MLSD");
            WriteText(stream, "STAT\r\n");
            FtpReply status = ReadMultilineReply(reader);
            WriteText(stream, "MLST " + unicodeName + "\r\n");
            FtpEntry metadata = FtpMlsxCodec.ParseControlReply(
                ReadMultilineReply(reader));
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            FtpListing humanListing = FtpHumanListingParser.ParseListing(
                SplitListingLines(human));
            FtpListing machineListing =
                FtpMlsxCodec.ParseDirectoryText(machine, true);
            FtpEntry humanFile = humanListing.Entries.Single(entry =>
                entry.Name == unicodeName);
            FtpEntry humanLink = humanListing.Entries.Single(entry =>
                entry.Name == linkName);
            FtpEntry machineFile = machineListing.Entries.Single(entry =>
                entry.Name == unicodeName);
            FtpEntry machineLink = machineListing.Entries.Single(entry =>
                entry.Name == linkName);
            string[] statusBody = status.Lines.Skip(1)
                .Take(status.Lines.Length - 2)
                .Select(line => line.RawText)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(SplitListingLines(names),
                    Is.EqualTo(new[] { unicodeName, linkName }));
                Assert.That(humanFile.RawLine,
                    Does.Contain("Feb 03 2024 " + unicodeName));
                Assert.That(humanFile.UnixMode,
                    Is.EqualTo(OperatingSystem.IsWindows() ? 420 : 384));
                Assert.That(humanLink.Type,
                    Is.EqualTo(FtpEntryType.SymbolicLink));
                Assert.That(humanLink.LinkTarget, Is.Null);
                Assert.That(humanLink.RawLine, Does.Not.Contain(" -> "));
                Assert.That(statusBody, Has.Some.Contains(unicodeName));
                Assert.That(statusBody.Single(line =>
                    line.EndsWith(linkName, StringComparison.Ordinal)),
                    Does.Not.Contain(" -> "));
                Assert.That(machineFile.Facts.Select(fact => fact.Name),
                    Is.EqualTo(new[] { "type", "size", "modify", "perm" }));
                Assert.That(machineFile.Modified.ValueUtc,
                    Is.EqualTo(modified));
                Assert.That(machineFile.Permissions,
                    Does.Contain(FtpPermission.Read));
                Assert.That(machineFile.Permissions,
                    Does.Contain(FtpPermission.Write));
                Assert.That(machineLink.Type,
                    Is.EqualTo(FtpEntryType.SymbolicLink));
                Assert.That(machineLink.Facts.Select(fact => fact.Name),
                    Is.EqualTo(new[] { "type" }));
                Assert.That(metadata.Name, Is.EqualTo("/" + unicodeName));
                Assert.That(metadata.Facts.Select(fact => fact.Name),
                    Is.EqualTo(new[] { "type", "size", "modify", "perm" }));
                Assert.That(metadata.Modified.ValueUtc, Is.EqualTo(modified));
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
    public void Per_entry_authorization_filters_every_listing_before_limits()
    {
        File.WriteAllText(Path.Combine(RootPath, "public.txt"), "public");
        File.WriteAllText(Path.Combine(RootPath, "secret.txt"), "secret");
        FilteringListingAuthorizer authorizer =
            new FilteringListingAuthorizer("secret.txt");
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumListingEntryCount = 1;

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            string names = DownloadPassive(stream, reader, "NLST");
            string human = DownloadPassive(stream, reader, "LIST");
            string machine = DownloadPassive(stream, reader, "MLSD");
            WriteText(stream, "STAT\r\n");
            FtpReply status = ReadMultilineReply(reader);
            WriteText(stream, "MLST secret.txt\r\n");
            string? metadataDenied = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(names, Is.EqualTo("public.txt\r\n"));
                Assert.That(human, Does.Contain("public.txt"));
                Assert.That(human, Does.Not.Contain("secret.txt"));
                Assert.That(machine, Does.Contain(" public.txt\r\n"));
                Assert.That(machine, Does.Not.Contain("secret.txt"));
                Assert.That(status.Lines.Select(line => line.RawText),
                    Has.Some.Contains("public.txt"));
                Assert.That(status.Lines.Select(line => line.RawText),
                    Has.None.Contains("secret.txt"));
                Assert.That(metadataDenied, Is.EqualTo(
                    "550 The requested listing was not available."));
                Assert.That(authorizer.Requests.Count(request =>
                    request.Entry != null &&
                    request.Entry.Name == "secret.txt"), Is.EqualTo(4));
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
    public void Configured_entry_limit_rejects_data_and_control_listings_before_framing()
    {
        File.WriteAllText(Path.Combine(RootPath, "one.txt"), "1");
        File.WriteAllText(Path.Combine(RootPath, "two.txt"), "2");
        RecordingFtpAuthorizer authorizer = CreateAllowedAuthorizer();
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(authorizer);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumListingEntryCount = 1;

        try
        {
            server.StartServer(options);
            using TcpClient control = Connect(port);
            using NetworkStream stream = control.GetStream();
            using StreamReader reader = CreateReader(stream);
            Login(stream, reader);
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "MLSD\r\n");
            string? dataFailure = reader.ReadLine();
            WriteText(stream, "STAT\r\n");
            string? controlFailure = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(dataFailure,
                    Is.EqualTo("552 The configured directory listing limit was exceeded."));
                Assert.That(controlFailure,
                    Is.EqualTo("552 The configured directory listing limit was exceeded."));
                Assert.That(server.Runtime.PassivePortPool!.ReservedCount,
                    Is.Zero);
                Assert.That(server.Runtime.Limits!.GetTransferCount(), Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Per_user_entry_and_byte_caps_return_explicit_resource_limit_replies()
    {
        File.WriteAllText(Path.Combine(RootPath, "one.txt"), "1");
        File.WriteAllText(Path.Combine(RootPath, "two.txt"), "2");
        UserLimits.HasListingEntryCap = true;
        UserLimits.ListingEntryCap = 1;
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
            WriteText(stream, "MLSD\r\n");
            string? entryFailure = reader.ReadLine();

            UserLimits.HasListingEntryCap = false;
            UserLimits.ListingEntryCap = 0;
            UserLimits.HasListingByteCap = true;
            UserLimits.ListingByteCap = 1;
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "MLSD\r\n");
            string? byteFailure = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(entryFailure,
                    Is.EqualTo("552 The configured directory listing limit was exceeded."));
                Assert.That(byteFailure,
                    Is.EqualTo("552 The configured directory listing limit was exceeded."));
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
    public void Authorization_denial_prevents_MLST_and_MLSD_and_releases_data_setup()
    {
        File.WriteAllText(Path.Combine(RootPath, "kept.txt"), "kept");
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
            WriteText(stream, "MLST kept.txt\r\n");
            string? metadataFailure = reader.ReadLine();
            EnterExtendedPassive(stream, reader);
            WriteText(stream, "MLSD\r\n");
            string? listingFailure = reader.ReadLine();
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(metadataFailure,
                    Is.EqualTo("550 The requested listing was not available."));
                Assert.That(listingFailure,
                    Is.EqualTo("550 The requested listing was not available."));
                Assert.That(authorizer.Requests.Select(request =>
                    request.Operation), Is.EqualTo(new[]
                    {
                        FtpAuthorizationOperation.ReadMetadata,
                        FtpAuthorizationOperation.List
                    }));
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
    public void Machine_formatter_applies_explicit_fact_selection_in_provider_order()
    {
        FtpEntry entry = FtpMlsxCodec.ParseEntry(
            "type=file;size=7;modify=20240809101112; sample.txt");

        string selected = FtpServerListingFormatter.FormatMachineEntry(
            entry, entry.Name, new[] { "SIZE", "TYPE" }, true);
        string pathnameOnly = FtpServerListingFormatter.FormatMachineEntry(
            entry, entry.Name, Array.Empty<string>(), true);

        Assert.Multiple(() =>
        {
            Assert.That(selected,
                Is.EqualTo("type=file;size=7; sample.txt"));
            Assert.That(pathnameOnly, Is.EqualTo(" sample.txt"));
        });
    }

    [Test]
    public void ABOR_cancels_a_listing_waiting_for_its_passive_connection()
    {
        File.WriteAllText(Path.Combine(RootPath, "entry.txt"), "entry");
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
            WriteText(stream, "MLSD\r\n");
            Assert.That(reader.ReadLine(), Does.StartWith("150 "));

            WriteText(stream, "ABOR\r\n");
            string? aborted = reader.ReadLine();
            string? completedAbort = reader.ReadLine();
            bool released = SpinWait.SpinUntil(
                () => server.Runtime.Limits!.GetTransferCount() == 0 &&
                    server.Runtime.PassivePortPool!.ReservedCount == 0,
                5000);
            WriteText(stream, "QUIT\r\n");
            reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(aborted, Does.StartWith("426 "));
                Assert.That(completedAbort,
                    Is.EqualTo("226 Abort command successful."));
                Assert.That(released, Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private string DownloadPassive(
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
        return new UTF8Encoding(false, true).GetString(received.ToArray());
    }

    private int EnterExtendedPassive(
        Stream controlStream,
        StreamReader reader)
    {
        WriteText(controlStream, "EPSV\r\n");
        string? reply = reader.ReadLine();
        Assert.That(reply, Does.StartWith("229 "));
        string[] fields = reply == null ? Array.Empty<string>() :
            reply.Split('|');
        return int.Parse(fields[3],
            System.Globalization.CultureInfo.InvariantCulture);
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
        while (line != null && !line.StartsWith(code + " ",
            StringComparison.Ordinal))
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

    private static string[] SplitListingLines(string listing)
    {
        if (listing.Length == 0)
        {
            return Array.Empty<string>();
        }
        Assert.That(listing, Does.EndWith("\r\n"));
        return listing.Substring(0, listing.Length - 2)
            .Split("\r\n", StringSplitOptions.None);
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
        ScriptedServerAuthenticator authenticator =
            new ScriptedServerAuthenticator();
        authenticator.ResourceLimits = UserLimits;
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
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

public class FilteringListingAuthorizer : IFtpAuthorizer
{
    public string DeniedName;
    public List<FtpAuthorizationRequest> Requests;

    public FilteringListingAuthorizer(string deniedName)
    {
        DeniedName = deniedName;
        Requests = new List<FtpAuthorizationRequest>();
    }

    public FtpAuthorizationResult AuthorizeOperation(
        FtpAuthorizationRequest request)
    {
        Requests.Add(request);
        bool deniedEntry = request.Entry != null &&
            request.Entry.Name == DeniedName;
        bool deniedMetadata =
            request.Operation == FtpAuthorizationOperation.ReadMetadata &&
            request.Path.EndsWith(
                "/" + DeniedName, StringComparison.Ordinal);
        FtpAuthorizationResult result = new FtpAuthorizationResult();
        result.Handled = true;
        result.Allowed = !deniedEntry && !deniedMetadata;
        return result;
    }
}
