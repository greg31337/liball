using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpServerLoginCommandTests
{
    [Test]
    public void Built_in_login_sequence_handles_password_account_relogin_rein_and_quit()
    {
        int port = ReserveAvailablePort();
        ScriptedServerAuthenticator authenticator =
            new ScriptedServerAuthenticator();
        RecordingHostResolver resolver = new RecordingHostResolver();
        resolver.Host.Authenticator = new RejectingServerAuthenticator();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(
            authenticator,
            CreateProxy<IFtpCertificateProvider>(),
            events,
            resolver);
        FtpServerOptions options = CreateOptions(
            port, FtpSecurityMode.Plain);
        options.MaximumAuthenticationAttempts = 10;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);

            Assert.That(reader.ReadLine(),
                Is.EqualTo("220 FTP service ready"));
            WriteText(stream,
                "PASS early\r\n" +
                "USER alice\r\n" +
                "PASS secret\r\n" +
                "ACCT billing\r\n" +
                "USER alice\r\n" +
                "PASS wrong\r\n" +
                "REIN\r\n" +
                "HOST unused.example\r\n" +
                "REIN\r\n" +
                "USER alice\r\n" +
                "AUTH GSSAPI\r\n" +
                "AUTH TLS\r\n" +
                "QUIT\r\n");

            string? earlyPass = reader.ReadLine();
            string? user = reader.ReadLine();
            string? password = reader.ReadLine();
            string? account = reader.ReadLine();
            string? reloginUser = reader.ReadLine();
            string? badPassword = reader.ReadLine();
            string? reinitialize = reader.ReadLine();
            string? host = reader.ReadLine();
            string? hostReinitialize = reader.ReadLine();
            string? postHostUser = reader.ReadLine();
            string? unknownSecurity = reader.ReadLine();
            string? unavailableTls = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 1,
                3000), Is.True);
            FtpAuthenticationRequest[] requests =
                authenticator.Requests.ToArray();
            FtpEvent[] recorded = events.GetEvents();
            Assert.Multiple(() =>
            {
                Assert.That(earlyPass,
                    Does.StartWith("503 "));
                Assert.That(user,
                    Is.EqualTo("331 User name accepted; password required."));
                Assert.That(password,
                    Is.EqualTo("332 Account information required."));
                Assert.That(account,
                    Is.EqualTo("230 User logged in; proceed."));
                Assert.That(reloginUser,
                    Is.EqualTo("331 User name accepted; password required."));
                Assert.That(badPassword,
                    Is.EqualTo("530 Authentication failed."));
                Assert.That(reinitialize,
                    Is.EqualTo(
                        "220 FTP session reinitialized; ready for a new user."));
                Assert.That(host, Is.EqualTo("220 FTP service ready"));
                Assert.That(hostReinitialize,
                    Is.EqualTo(
                        "220 FTP session reinitialized; ready for a new user."));
                Assert.That(postHostUser,
                    Is.EqualTo("331 User name accepted; password required."));
                Assert.That(unknownSecurity,
                    Does.StartWith("504 "));
                Assert.That(unavailableTls,
                    Does.StartWith("534 "));
                Assert.That(quit,
                    Is.EqualTo("221 FTP service closing control connection."));
                Assert.That(requests, Has.Length.EqualTo(6));
                Assert.That(requests.Select(request => request.AttemptNumber),
                    Is.EqualTo(new[] { 1, 2, 3, 4, 5, 1 }));
                Assert.That(requests.All(request =>
                    !request.ControlProtected), Is.True);
                Assert.That(requests[2].Account, Is.EqualTo("billing"));
                Assert.That(requests[2].Principal!.Authenticated, Is.True);
                Assert.That(recorded.Count(eventData =>
                    eventData.Kind == FtpEventKind.Authentication),
                    Is.EqualTo(6));
                Assert.That(recorded.Any(eventData =>
                    eventData.Kind == FtpEventKind.Authentication &&
                    eventData.AuthenticationStatus ==
                        FtpAuthenticationStatus.Success), Is.True);
                Assert.That(GetLimits(server).GetPendingLoginCount(), Is.Zero);
                Assert.That(GetLimits(server).GetUserSessionCount("alice"),
                    Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Login_attempt_limit_closes_after_uniform_failures()
    {
        int port = ReserveAvailablePort();
        RejectingServerAuthenticator authenticator =
            new RejectingServerAuthenticator();
        FtpServer server = CreateServer(
            authenticator,
            CreateProxy<IFtpCertificateProvider>(),
            new RecordingServerEventSink(),
            new DefaultFtpHostResolver(
                new FtpVirtualHostOptions[0]));
        FtpServerOptions options = CreateOptions(
            port, FtpSecurityMode.Plain);
        options.MaximumAuthenticationAttempts = 3;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(),
                Is.EqualTo("220 FTP service ready"));

            WriteText(stream,
                "USER one\r\nUSER two\r\nUSER three\r\n");
            string? first = reader.ReadLine();
            string? second = reader.ReadLine();
            string? final = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(first,
                    Is.EqualTo("530 Authentication failed."));
                Assert.That(second,
                    Is.EqualTo("530 Authentication failed."));
                Assert.That(final,
                    Is.EqualTo("421 Too many FTP authentication attempts."));
                Assert.That(authenticator.Attempts,
                    Is.EqualTo(new[] { 1, 2, 3 }));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Explicit_listener_rejects_clear_credentials_and_security_sequence_errors()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer(
            new ScriptedServerAuthenticator(),
            CreateProxy<IFtpCertificateProvider>(),
            new RecordingServerEventSink(),
            new DefaultFtpHostResolver(
                new FtpVirtualHostOptions[0]));
        FtpServerOptions options = CreateOptions(
            port, FtpSecurityMode.ExplicitTlsRequired);
        options.Listeners[0].AllowPlaintextCredentials = false;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(),
                Is.EqualTo("220 FTP service ready"));

            WriteText(stream,
                "USER alice\r\nPBSZ 0\r\nPROT P\r\nCCC\r\nQUIT\r\n");

            Assert.Multiple(() =>
            {
                Assert.That(reader.ReadLine(), Does.StartWith("534 "));
                Assert.That(reader.ReadLine(), Does.StartWith("503 "));
                Assert.That(reader.ReadLine(), Does.StartWith("503 "));
                Assert.That(reader.ReadLine(), Does.StartWith("503 "));
                Assert.That(reader.ReadLine(), Does.StartWith("221 "));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void TLS_command_handler_enforces_AUTH_PBSZ_PROT_and_CCC_state()
    {
        FtpServerOptions options = CreateOptions(
            ReserveAvailablePort(),
            FtpSecurityMode.ExplicitTlsRequired);
        options.Listeners[0].Tls.AllowClearCommandChannel = true;
        FtpServerLimitManager limits =
            new FtpServerLimitManager(options);
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        FtpServerSessionContext connection =
            new FtpServerSessionContext(
                socket, options.Listeners[0], limits);
        FtpServerSession session = new FtpServerSession(
            connection,
            options,
            CreateProxy<IFtpFileSystem>(),
            new ScriptedServerAuthenticator(),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(
                new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new RecordingServerEventSink(),
            context => FtpServerLoginCommandHandler.HandleCommand(context));
        session.State.OpenConnection();

        FtpCommand nonTls = CreateCommand(
            FtpCommandKind.Auth, "GSSAPI");
        FtpCommandResult rejected =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, nonTls));
        FtpCommand authorization = CreateCommand(
            FtpCommandKind.Auth, "TLS-C");
        FtpCommandResult accepted =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, authorization));
        session.State.ApplyCommandReply(
            authorization, accepted.FinalReply!);
        session.State.CompleteSecurityExchange(true);
        FtpCommand repeatedCommand = CreateCommand(
            FtpCommandKind.Auth, "TLS");
        FtpCommandResult repeated =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, repeatedCommand));
        FtpCommand pbszCommand = CreateCommand(
            FtpCommandKind.Pbsz, "0");
        FtpCommandResult pbsz =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, pbszCommand));
        session.State.ApplyCommandReply(pbszCommand, pbsz.FinalReply!);
        FtpCommand badProtectionCommand = CreateCommand(
            FtpCommandKind.Prot, "S");
        FtpCommandResult badProtection =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(
                    session, badProtectionCommand));
        FtpCommand protectionCommand = CreateCommand(
            FtpCommandKind.Prot, "P");
        FtpCommandResult protection =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, protectionCommand));
        session.State.ApplyCommandReply(
            protectionCommand, protection.FinalReply!);
        FtpCommand clearCommand = CreateCommand(
            FtpCommandKind.Ccc, null);
        FtpCommandResult clear =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, clearCommand));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandParameterNotImplemented));
            Assert.That(accepted.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.SecurityExchangeComplete));
            Assert.That(repeated.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(pbsz.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(badProtection.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.ProtectionLevelNotSupported));
            Assert.That(protection.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(session.State.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(clear.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandOkay));
        });
    }

    [Test]
    public void Explicit_TLS_upgrade_negotiates_protection_and_CCC_returns_to_clear_control()
    {
        using X509Certificate2 certificate =
            FtpTlsStreamTests.CreateCertificate();
        int port = ReserveAvailablePort();
        FtpTlsOptions providerOptions = new FtpTlsOptions();
        providerOptions.ServerCertificate = certificate;
        providerOptions.CheckCertificateRevocation = false;
        DefaultFtpCertificateProvider certificateProvider =
            new DefaultFtpCertificateProvider(providerOptions);
        ScriptedServerAuthenticator authenticator =
            new ScriptedServerAuthenticator();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(
            authenticator,
            certificateProvider,
            events,
            new DefaultFtpHostResolver(
                new FtpVirtualHostOptions[0]));
        FtpServerOptions options = CreateOptions(
            port, FtpSecurityMode.ExplicitTlsRequired);
        options.Listeners[0].Tls.Protocols = SslProtocols.Tls12;
        options.Listeners[0].Tls.CheckCertificateRevocation = false;
        options.Listeners[0].Tls.AllowClearCommandChannel = true;
        options.Listeners[0].AllowPlaintextCredentials = false;

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream transport = client.GetStream();
            using (StreamReader clearReader = CreateReader(transport))
            {
                Assert.That(clearReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteText(transport, "AUTH TLS\r\n");
                Assert.That(clearReader.ReadLine(),
                    Is.EqualTo(
                        "234 AUTH TLS accepted; proceed with TLS negotiation."));
            }

            FtpTlsClientStreamRequest request =
                new FtpTlsClientStreamRequest();
            request.TransportStream = transport;
            request.TargetHost = "localhost";
            request.LeaveTransportOpen = true;
            request.HandshakeTimeoutMilliseconds = 5000;
            request.CertificateValidation =
                (sender, remote, chain, errors) => true;
            request.Options!.Protocols = SslProtocols.Tls12;
            request.Options.CheckCertificateRevocation = false;
            FtpTlsClientStream tls = new FtpTlsClientStream(request);
            FtpTlsStreamResult secured = tls.UpgradeExplicitClient(
                FtpServerSession.CreateReply(
                    FtpReplyCodes.SecurityExchangeComplete,
                    "AUTH TLS accepted; proceed with TLS negotiation."));
            using (StreamReader secureReader =
                new StreamReader(
                    secured.Stream!, Encoding.ASCII, false, 1024, true))
            {
                WriteText(secured.Stream!,
                    "PBSZ 0\r\n" +
                    "PROT P\r\n" +
                    "USER alice\r\n" +
                    "PASS secret\r\n" +
                    "ACCT billing\r\n" +
                    "CCC\r\n");
                Assert.That(secureReader.ReadLine(), Does.StartWith("200 "));
                Assert.That(secureReader.ReadLine(), Does.StartWith("200 "));
                Assert.That(secureReader.ReadLine(), Does.StartWith("331 "));
                Assert.That(secureReader.ReadLine(), Does.StartWith("332 "));
                Assert.That(secureReader.ReadLine(), Does.StartWith("230 "));
                Assert.That(secureReader.ReadLine(), Does.StartWith("200 "));
            }
            tls.ShutdownClientTlsForClearControl();

            using StreamReader returnedClearReader = CreateReader(transport);
            WriteText(transport, "USER alice\r\nQUIT\r\n");
            string? clearCredential = returnedClearReader.ReadLine();
            string? quit = returnedClearReader.ReadLine();

            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 1,
                3000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(clearCredential, Does.StartWith("534 "));
                Assert.That(quit, Does.StartWith("221 "));
                Assert.That(authenticator.Requests,
                    Has.All.Matches<FtpAuthenticationRequest>(requestData =>
                        requestData.ControlProtected));
                Assert.That(events.GetEvents().Any(eventData =>
                    eventData.Kind == FtpEventKind.Tls &&
                    eventData.TlsDiagnostics!.ExplicitUpgrade), Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Built_in_users_enforce_accounts_host_permissions_uniform_delay_and_redaction()
    {
        const string password = "correct horse";
        const string account = "billing-secret";
        const string anonymousPassword = "guest-secret@example.test";
        string rootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-server-security-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        byte[] salt = Encoding.ASCII.GetBytes("server-security-salt");
        FtpUserRecord user = new FtpUserRecord();
        user.UserName = "alice";
        user.Salt = salt;
        user.PasswordVerifier = DeriveVerifier(salt, password);
        user.Account = account;
        user.Disabled = false;
        user.AllowWrite = true;
        FtpFixedUserOptions fixedUsers = new FtpFixedUserOptions();
        fixedUsers.Users = new[] { user };
        fixedUsers.AllowAnonymous = true;
        fixedUsers.RequireProtectedControl = false;
        fixedUsers.MaximumAttempts = 12;
        fixedUsers.FailureDelayMilliseconds = 75;

        int port = ReserveAvailablePort();
        FtpServerOptions options = CreateOptions(port, FtpSecurityMode.Plain);
        options.FixedUsers = fixedUsers;
        options.MaximumAuthenticationAttempts = 12;
        LocalFtpFileSystem fileSystem = new LocalFtpFileSystem(rootPath);
        FtpVirtualHostOptions primary = new FtpVirtualHostOptions();
        primary.HostName = "primary.example";
        primary.Greeting = "Primary FTP ready";
        primary.RootPath = "/";
        primary.FileSystem = fileSystem;
        primary.AllowAnonymous = true;
        primary.AllowAnonymousWrites = false;
        RejectingServerAuthenticator isolatedAuthenticator =
            new RejectingServerAuthenticator();
        FtpVirtualHostOptions isolated = new FtpVirtualHostOptions();
        isolated.HostName = "isolated.example";
        isolated.Greeting = "Isolated FTP ready";
        isolated.RootPath = "/";
        isolated.FileSystem = fileSystem;
        isolated.Authenticator = isolatedAuthenticator;
        options.VirtualHosts = new[] { primary, isolated };
        DefaultFtpHostResolver resolver =
            new DefaultFtpHostResolver(options.VirtualHosts);
        RejectingServerAuthenticator injectedAuthenticator =
            new RejectingServerAuthenticator();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = new FtpServer(
            fileSystem,
            injectedAuthenticator,
            new AllowAllFtpAuthorizer(),
            resolver,
            CreateProxy<IFtpCertificateProvider>(),
            events);

        try
        {
            server.StartServer(options);
            using (TcpClient fixedClient = Connect(port))
            using (NetworkStream fixedStream = fixedClient.GetStream())
            using (StreamReader fixedReader = CreateReader(fixedStream))
            {
                Assert.That(fixedReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteText(fixedStream,
                    "HOST primary.example\r\n" +
                    "USER alice\r\n" +
                    "PASS " + password + "\r\n" +
                    "ACCT " + account + "\r\n" +
                    "MKD fixed-directory\r\n" +
                    "QUIT\r\n");
                Assert.That(fixedReader.ReadLine(),
                    Is.EqualTo("220 Primary FTP ready"));
                Assert.That(fixedReader.ReadLine(), Does.StartWith("331 "));
                Assert.That(fixedReader.ReadLine(), Does.StartWith("332 "));
                Assert.That(fixedReader.ReadLine(), Does.StartWith("230 "));
                Assert.That(fixedReader.ReadLine(), Does.StartWith("257 "));
                Assert.That(fixedReader.ReadLine(), Does.StartWith("221 "));
            }

            using (TcpClient anonymousClient = Connect(port))
            using (NetworkStream anonymousStream = anonymousClient.GetStream())
            using (StreamReader anonymousReader = CreateReader(anonymousStream))
            {
                Assert.That(anonymousReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteText(anonymousStream,
                    "HOST primary.example\r\n" +
                    "USER anonymous\r\n" +
                    "PASS " + anonymousPassword + "\r\n" +
                    "MLST /\r\n");
                Assert.That(anonymousReader.ReadLine(),
                    Is.EqualTo("220 Primary FTP ready"));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith("331 "));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith("230 "));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith("250-"));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith(" "));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith("250 "));
                WriteText(anonymousStream,
                    "MKD anonymous-directory\r\nQUIT\r\n");
                Assert.That(anonymousReader.ReadLine(),
                    Is.EqualTo("550 The requested file action was not taken."));
                Assert.That(anonymousReader.ReadLine(), Does.StartWith("221 "));
            }

            string? unknownFailure;
            string? wrongPasswordFailure;
            long delayedMilliseconds;
            using (TcpClient failureClient = Connect(port))
            using (NetworkStream failureStream = failureClient.GetStream())
            using (StreamReader failureReader = CreateReader(failureStream))
            {
                Assert.That(failureReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteText(failureStream,
                    "HOST primary.example\r\nUSER missing\r\n");
                Assert.That(failureReader.ReadLine(),
                    Is.EqualTo("220 Primary FTP ready"));
                Assert.That(failureReader.ReadLine(), Does.StartWith("331 "));
                Stopwatch delay = Stopwatch.StartNew();
                WriteText(failureStream, "PASS wrong-secret\r\n");
                unknownFailure = failureReader.ReadLine();
                delay.Stop();
                delayedMilliseconds = delay.ElapsedMilliseconds;
                WriteText(failureStream,
                    "USER alice\r\nPASS wrong-secret\r\nQUIT\r\n");
                Assert.That(failureReader.ReadLine(), Does.StartWith("331 "));
                wrongPasswordFailure = failureReader.ReadLine();
                Assert.That(failureReader.ReadLine(), Does.StartWith("221 "));
            }

            using (TcpClient isolatedClient = Connect(port))
            using (NetworkStream isolatedStream = isolatedClient.GetStream())
            using (StreamReader isolatedReader = CreateReader(isolatedStream))
            {
                Assert.That(isolatedReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteText(isolatedStream,
                    "HOST isolated.example\r\nUSER alice\r\nQUIT\r\n");
                Assert.That(isolatedReader.ReadLine(),
                    Is.EqualTo("220 Isolated FTP ready"));
                Assert.That(isolatedReader.ReadLine(),
                    Is.EqualTo("530 Authentication failed."));
                Assert.That(isolatedReader.ReadLine(), Does.StartWith("221 "));
            }

            FtpEvent[] recorded = events.GetEvents();
            string eventText = FlattenEventText(recorded);
            Assert.Multiple(() =>
            {
                Assert.That(Directory.Exists(
                    Path.Combine(rootPath, "fixed-directory")), Is.True);
                Assert.That(Directory.Exists(
                    Path.Combine(rootPath, "anonymous-directory")), Is.False);
                Assert.That(unknownFailure,
                    Is.EqualTo("530 Authentication failed."));
                Assert.That(wrongPasswordFailure, Is.EqualTo(unknownFailure));
                Assert.That(delayedMilliseconds, Is.GreaterThanOrEqualTo(40));
                Assert.That(injectedAuthenticator.Attempts, Is.Empty);
                Assert.That(isolatedAuthenticator.Attempts,
                    Is.EqualTo(new[] { 1 }));
                Assert.That(recorded.Any(eventData =>
                    eventData.Kind == FtpEventKind.Command &&
                    eventData.CommandKind == FtpCommandKind.Pass), Is.True);
                Assert.That(recorded.Where(eventData =>
                    eventData.Command != null &&
                    (eventData.Command.Kind == FtpCommandKind.Pass ||
                     eventData.Command.Kind == FtpCommandKind.Acct)).All(
                        eventData => eventData.Command!.Argument ==
                            FtpTranscriptRedactor.RedactedText), Is.True);
                Assert.That(eventText, Does.Not.Contain(password));
                Assert.That(eventText, Does.Not.Contain(account));
                Assert.That(eventText, Does.Not.Contain(anonymousPassword));
                Assert.That(eventText, Does.Not.Contain("wrong-secret"));
                Assert.That(recorded.Where(eventData =>
                    eventData.Principal != null).All(eventData =>
                        eventData.Principal!.Account == null &&
                        eventData.Principal.CertificateIdentity == null &&
                        eventData.Principal.HomeDirectory == null), Is.True);
            });
        }
        finally
        {
            server.StopServer();
            Directory.Delete(rootPath, true);
        }
    }

    [Test]
    public void TLS_identity_reaches_authentication_and_CCC_remains_opt_in()
    {
        FtpServerOptions options = CreateOptions(
            ReserveAvailablePort(), FtpSecurityMode.ExplicitTlsRequired);
        FtpServerLimitManager limits = new FtpServerLimitManager(options);
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        FtpServerSessionContext connection =
            new FtpServerSessionContext(
                socket, options.Listeners[0], limits);
        ScriptedServerAuthenticator authenticator =
            new ScriptedServerAuthenticator();
        FtpServerSession session = new FtpServerSession(
            connection,
            options,
            CreateProxy<IFtpFileSystem>(),
            authenticator,
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            CreateProxy<IFtpCertificateProvider>(),
            new RecordingServerEventSink(),
            context => FtpServerLoginCommandHandler.HandleCommand(context));
        session.State.OpenConnection();
        FtpCommand authorization = CreateCommand(FtpCommandKind.Auth, "TLS");
        FtpReply authorizationReply = FtpServerSession.CreateReply(
            FtpReplyCodes.SecurityExchangeComplete, "Begin TLS");
        session.State.ApplyCommandReply(authorization, authorizationReply);
        session.State.CompleteSecurityExchange(true);
        session.CertificateIdentity = "SHA256:client-certificate-fingerprint";

        FtpCommand user = CreateCommand(FtpCommandKind.User, "alice");
        FtpCommandResult userResult =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, user));
        session.State.ApplyCommandReply(user, userResult.FinalReply!);
        FtpCommand pbsz = CreateCommand(FtpCommandKind.Pbsz, "0");
        FtpCommandResult pbszResult =
            FtpServerLoginCommandHandler.HandleCommand(
                new FtpServerCommandContext(session, pbsz));
        session.State.ApplyCommandReply(pbsz, pbszResult.FinalReply!);
        FtpCommand ccc = CreateCommand(FtpCommandKind.Ccc, null);
        FtpCommandResult denied = FtpServerLoginCommandHandler.HandleCommand(
            new FtpServerCommandContext(session, ccc));
        options.Listeners[0].Tls.AllowClearCommandChannel = true;
        FtpCommandResult allowed = FtpServerLoginCommandHandler.HandleCommand(
            new FtpServerCommandContext(session, ccc));

        Assert.Multiple(() =>
        {
            Assert.That(authenticator.Requests, Has.Count.EqualTo(1));
            Assert.That(authenticator.Requests[0].CertificateIdentity,
                Is.EqualTo("SHA256:client-certificate-fingerprint"));
            Assert.That(authenticator.Requests[0].ControlProtected, Is.True);
            Assert.That(denied.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.RequestDeniedByPolicy));
            Assert.That(allowed.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandOkay));
        });
    }

    private static FtpServer CreateServer(
        IFtpAuthenticator authenticator,
        IFtpCertificateProvider certificateProvider,
        IFtpEventSink eventSink,
        IFtpHostResolver hostResolver)
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            authenticator,
            new AllowAllFtpAuthorizer(),
            hostResolver,
            certificateProvider,
            eventSink);
    }

    private static FtpServerOptions CreateOptions(
        int port,
        FtpSecurityMode securityMode)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = securityMode;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
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

    private static FtpCommand CreateCommand(
        FtpCommandKind kind,
        string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
        command.RawText = argument == null ?
            command.Verb : command.Verb + " " + argument;
        return command;
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation =
            new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static byte[] DeriveVerifier(byte[] salt, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
        Buffer.BlockCopy(
            passwordBytes,
            0,
            saltedPassword,
            salt.Length,
            passwordBytes.Length);
        return SHA256.HashData(saltedPassword);
    }

    private static string FlattenEventText(FtpEvent[] events)
    {
        StringBuilder text = new StringBuilder();
        foreach (FtpEvent eventData in events)
        {
            text.Append(eventData.Message);
            if (eventData.Command != null)
            {
                text.Append(eventData.Command.Argument);
                text.Append(eventData.Command.RawText);
            }
            if (eventData.Reply != null)
            {
                text.Append(eventData.Reply.Text);
                text.Append(eventData.Reply.RawText);
                foreach (FtpReplyLine line in eventData.Reply.Lines)
                {
                    text.Append(line.Text);
                    text.Append(line.RawText);
                }
            }
            if (eventData.Principal != null)
            {
                text.Append(eventData.Principal.Account);
                text.Append(eventData.Principal.CertificateIdentity);
                text.Append(eventData.Principal.HomeDirectory);
            }
        }
        return text.ToString();
    }

    private static FtpServerSessionDispatcher GetDispatcher(FtpServer server)
    {
        FtpServerSessionDispatcher? dispatcher =
            server.Runtime.SessionDispatcher;
        Assert.That(dispatcher, Is.Not.Null);
        return dispatcher!;
    }

    private static FtpServerLimitManager GetLimits(FtpServer server)
    {
        FtpServerLimitManager? limits = server.Runtime.Limits;
        Assert.That(limits, Is.Not.Null);
        return limits!;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerLoginProviderProxy>();
    }
}

public class ScriptedServerAuthenticator : IFtpAuthenticator
{
    public List<FtpAuthenticationRequest> Requests;
    public FtpResourceLimits ResourceLimits;

    public ScriptedServerAuthenticator()
    {
        Requests = new List<FtpAuthenticationRequest>();
        ResourceLimits = new FtpResourceLimits();
    }

    public FtpAuthenticationResult AuthenticateIdentity(
        FtpAuthenticationRequest request)
    {
        Requests.Add(request);
        if (request.Password == null && request.Account == null)
        {
            FtpPrincipal principal = new FtpPrincipal();
            principal.Name = request.UserName;
            principal.ResourceLimits = ResourceLimits;
            FtpAuthenticationResult challenge =
                new FtpAuthenticationResult();
            challenge.Status =
                FtpAuthenticationStatus.AdditionalInformationRequired;
            challenge.Principal = principal;
            return challenge;
        }
        if (request.Password == "secret")
        {
            FtpPrincipal principal = new FtpPrincipal();
            principal.Name = request.UserName;
            principal.Authenticated = true;
            principal.ResourceLimits = ResourceLimits;
            FtpAuthenticationResult account =
                new FtpAuthenticationResult();
            account.Status =
                FtpAuthenticationStatus.AdditionalInformationRequired;
            account.Principal = principal;
            return account;
        }
        if (request.Account == "billing" &&
            request.Principal != null &&
            request.Principal.Authenticated)
        {
            FtpAuthenticationResult success =
                new FtpAuthenticationResult();
            success.Status = FtpAuthenticationStatus.Success;
            success.Principal = request.Principal;
            success.Principal.Account = request.Account;
            return success;
        }
        FtpAuthenticationResult failure = new FtpAuthenticationResult();
        failure.Status = FtpAuthenticationStatus.Failure;
        failure.FailureMessage = "Authentication failed.";
        return failure;
    }
}

public class RejectingServerAuthenticator : IFtpAuthenticator
{
    public List<int> Attempts;

    public RejectingServerAuthenticator()
    {
        Attempts = new List<int>();
    }

    public FtpAuthenticationResult AuthenticateIdentity(
        FtpAuthenticationRequest request)
    {
        Attempts.Add(request.AttemptNumber);
        FtpAuthenticationResult result = new FtpAuthenticationResult();
        result.Status = FtpAuthenticationStatus.Failure;
        result.FailureMessage = "Private provider detail.";
        return result;
    }
}

public class FtpServerLoginProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Server login tests do not invoke unrelated providers.");
    }
}
