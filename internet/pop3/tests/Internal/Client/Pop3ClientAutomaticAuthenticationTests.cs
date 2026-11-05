using System.Text;

namespace pop3.Tests;

public class Pop3ClientAutomaticAuthenticationTests
{
    [Test]
    public void CallerSaslOrderWinsOverServerOrderAndLogsRemainRedacted()
    {
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = SaslSuccessStream(
            null,
            "SASL SECOND FIRST\r\nUSER");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, logger));
        ScriptedSaslClient first = new ScriptedSaslClient("FIRST", "first-secret", "alice");
        ScriptedSaslClient second = new ScriptedSaslClient("SECOND", "second-secret", "alice");
        Pop3AuthenticationRequest request = Request("alice", "password");
        request.SaslClients = new IPop3SaslClient[] { first, second };

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(first.BeginCount, Is.EqualTo(1));
        Assert.That(second.BeginCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH FIRST Zmlyc3Qtc2VjcmV0\r\nCAPA\r\n"));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "AUTH FIRST [redacted]",
            "+OK authenticated"
        }));
        Assert.That(logger.Entries[0].IsRedacted, Is.True);
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("first-secret"));
    }

    [Test]
    public void UnsupportedHooksAreSkippedWithoutBeingStarted()
    {
        AuthenticationSequenceStream stream = SaslSuccessStream(null, "SASL SUPPORTED");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient unsupported = new ScriptedSaslClient("OTHER", "unused", "alice");
        ScriptedSaslClient supported = new ScriptedSaslClient("SUPPORTED", "selected", "alice");
        Pop3AuthenticationRequest request = Request("alice", "password");
        request.SaslClients = new IPop3SaslClient[] { unsupported, supported };

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(unsupported.BeginCount, Is.Zero);
        Assert.That(supported.BeginCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH SUPPORTED c2VsZWN0ZWQ=\r\nCAPA\r\n"));
    }

    [Test]
    public void SingleWeakSaslHookUsesExplicitPlaintextPermission()
    {
        AuthenticationSequenceStream stream = SaslSuccessStream(null, "SASL PLAIN");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient plain = new ScriptedSaslClient("PLAIN", "permitted", "alice");
        Pop3AuthenticationRequest request = Request("alice", "password");
        request.SaslClient = plain;
        request.AllowUnprotectedCredentials = true;

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(plain.BeginCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH PLAIN cGVybWl0dGVk\r\nCAPA\r\n"));
    }

    [Test]
    public void WeakSaslIsSkippedOnPlaintextButAllowedOnProtectedTransport()
    {
        AuthenticationSequenceStream plaintextStream = SaslSuccessStream(
            null,
            "SASL PLAIN SCRAM-SHA-256");
        Pop3ClientEngine plaintextEngine = Connect(
            plaintextStream,
            Options(false, false, null));
        ScriptedSaslClient plain = new ScriptedSaslClient("PLAIN", "plain", "alice");
        ScriptedSaslClient scram = new ScriptedSaslClient("SCRAM-SHA-256", "scram", "alice");
        Pop3AuthenticationRequest plaintextRequest = Request("alice", "password");
        plaintextRequest.SaslClients = new IPop3SaslClient[] { plain, scram };

        Pop3AuthenticationResult plaintextResult = plaintextEngine.AuthenticateAutomatic(
            plaintextRequest,
            CancellationToken.None);

        Assert.That(plaintextResult.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(plain.BeginCount, Is.Zero);
        Assert.That(scram.BeginCount, Is.EqualTo(1));
        Assert.That(plaintextStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH SCRAM-SHA-256 c2NyYW0=\r\nCAPA\r\n"));

        AuthenticationSequenceStream protectedStream = SaslSuccessStream(null, "SASL PLAIN");
        Pop3ClientEngine protectedEngine = Connect(
            protectedStream,
            Options(true, false, null));
        ScriptedSaslClient protectedPlain = new ScriptedSaslClient(
            "PLAIN",
            "protected",
            "alice");
        Pop3AuthenticationRequest protectedRequest = Request("alice", "password");
        protectedRequest.SaslClients = new IPop3SaslClient[] { protectedPlain };

        Pop3AuthenticationResult protectedResult = protectedEngine.AuthenticateAutomatic(
            protectedRequest,
            CancellationToken.None);

        Assert.That(protectedResult.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(protectedEngine.GetSnapshot().IsSecure, Is.True);
        Assert.That(protectedPlain.BeginCount, Is.EqualTo(1));
        Assert.That(protectedStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH PLAIN cHJvdGVjdGVk\r\nCAPA\r\n"));
    }

    [TestCase("PLAIN")]
    [TestCase("LOGIN")]
    [TestCase("OAUTHBEARER")]
    [TestCase("XOAUTH2")]
    public void EveryCleartextCredentialMechanismIsSkippedForASafeAlternative(
        string weakMechanism)
    {
        AuthenticationSequenceStream stream = SaslSuccessStream(
            null,
            "SASL " + weakMechanism + " SAFE");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient weak = new ScriptedSaslClient(
            weakMechanism,
            "weak-secret",
            "alice");
        ScriptedSaslClient safe = new ScriptedSaslClient("SAFE", "proof", "alice");
        Pop3AuthenticationRequest request = Request("alice", "password");
        request.SaslClients = new IPop3SaslClient[] { weak, safe };

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(weak.BeginCount, Is.Zero);
        Assert.That(safe.BeginCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH SAFE cHJvb2Y=\r\nCAPA\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("weak-secret"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void NoCleartextCredentialMethodIsUsedWithoutExplicitPermission()
    {
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nSASL PLAIN\r\nUSER\r\n.\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient plain = new ScriptedSaslClient("PLAIN", "secret", "alice");
        Pop3AuthenticationRequest request = Request("alice", "password");
        request.SaslClients = new IPop3SaslClient[] { plain };

        Assert.Throws<InvalidOperationException>(() => engine.AuthenticateAutomatic(
            request,
            CancellationToken.None));

        Assert.That(plain.BeginCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void OptionalApopPrecedesUserPasswordFallback()
    {
        const string timestamp = "<automatic@example.test>";
        AuthenticationSequenceStream apopStream = AuthenticationSuccessStream(
            timestamp,
            "USER",
            "+OK authenticated\r\n");
        Pop3ClientEngine apopEngine = Connect(apopStream, Options(false, false, null));
        Pop3AuthenticationRequest apopRequest = Request("alice", "secret");
        apopRequest.AllowApop = true;

        Pop3AuthenticationResult apopResult = apopEngine.AuthenticateAutomatic(
            apopRequest,
            CancellationToken.None);

        Assert.That(apopResult.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(apopStream.WrittenText(), Does.StartWith("CAPA\r\nAPOP alice "));
        Assert.That(apopStream.WrittenText(), Does.Not.Contain("USER alice"));

        AuthenticationSequenceStream userStream = AuthenticationSuccessStream(
            timestamp,
            "USER",
            "+OK user accepted\r\n+OK password accepted\r\n");
        Pop3ClientEngine userEngine = Connect(userStream, Options(false, false, null));
        Pop3AuthenticationRequest userRequest = Request("alice", "secret");
        userRequest.AllowUnprotectedCredentials = true;

        Pop3AuthenticationResult userResult = userEngine.AuthenticateAutomatic(
            userRequest,
            CancellationToken.None);

        Assert.That(userResult.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(userStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
        Assert.That(userStream.WrittenText(), Does.Not.Contain("APOP"));
    }

    [Test]
    public void UnsupportedHookFallsThroughToOptedInApop()
    {
        const string timestamp = "<unsupported@example.test>";
        AuthenticationSequenceStream stream = AuthenticationSuccessStream(
            timestamp,
            "USER",
            "+OK authenticated\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient unsupported = new ScriptedSaslClient(
            "UNSUPPORTED",
            "unused",
            "alice");
        Pop3AuthenticationRequest request = Request("alice", "secret");
        request.SaslClients = new IPop3SaslClient[] { unsupported };
        request.AllowApop = true;

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(unsupported.MechanismCount, Is.EqualTo(1));
        Assert.That(unsupported.BeginCount, Is.Zero);
        Assert.That(stream.WrittenText(), Does.StartWith("CAPA\r\nAPOP alice "));
        Assert.That(stream.WrittenText(), Does.Not.Contain("AUTH UNSUPPORTED"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("USER alice"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MissingApopChallengeFallsThroughToPermittedUserPassword()
    {
        AuthenticationSequenceStream stream = AuthenticationSuccessStream(
            null,
            "USER",
            "+OK user accepted\r\n+OK password accepted\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        Pop3AuthenticationRequest request = Request("alice", "secret");
        request.AllowApop = true;
        request.AllowUnprotectedCredentials = true;

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("APOP"));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void AuthenticationRejectionDoesNotDowngradeToApopOrUserPassword()
    {
        const string timestamp = "<downgrade@example.test>";
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nSASL SCRAM-SHA-256\r\nUSER\r\n.\r\n" +
            "-ERR rejected\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient sasl = new ScriptedSaslClient(
            "SCRAM-SHA-256",
            "proof",
            "alice");
        Pop3AuthenticationRequest request = Request("alice", "secret");
        request.SaslClients = new IPop3SaslClient[] { sasl };
        request.AllowApop = true;
        request.AllowUnprotectedCredentials = true;

        Pop3AuthenticationException exception = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateAutomatic(request, CancellationToken.None))!;

        Assert.That(exception.Method, Is.EqualTo(Pop3AuthenticationMethod.Sasl));
        Assert.That(exception.Result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(exception.Result.FailureText, Is.EqualTo("rejected"));
        Assert.That(exception.Response.Text, Is.EqualTo("rejected"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH SCRAM-SHA-256 cHJvb2Y=\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("APOP"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("USER alice"));
    }

    [Test]
    public void LocalSaslRejectionDoesNotDowngradeToApopOrUserPassword()
    {
        const string timestamp = "<local-rejection@example.test>";
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready " + timestamp + "\r\n" +
            "+OK capabilities\r\nSASL TEST\r\nUSER\r\n.\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        RejectingSaslClient rejecting = new RejectingSaslClient();
        Pop3AuthenticationRequest request = Request("alice", "secret");
        request.SaslClients = new IPop3SaslClient[] { rejecting };
        request.AllowApop = true;
        request.AllowUnprotectedCredentials = true;

        Pop3AuthenticationResult result = engine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Rejected));
        Assert.That(result.FailureText, Does.Contain("did not complete"));
        Assert.That(rejecting.BeginCount, Is.EqualTo(1));
        Assert.That(rejecting.CancelCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("APOP"));
        Assert.That(stream.WrittenText(), Does.Not.Contain("USER alice"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void UserFallbackRequiresBothPolicyPermissionAndCapabilityPermission()
    {
        AuthenticationSequenceStream deniedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n-ERR CAPA unavailable\r\n");
        Pop3ClientEngine deniedEngine = Connect(deniedStream, Options(false, false, null));
        Pop3AuthenticationRequest request = Request("alice", "secret");
        request.AllowUnprotectedCredentials = true;

        Assert.Throws<InvalidOperationException>(() => deniedEngine.AuthenticateAutomatic(
            request,
            CancellationToken.None));
        Assert.That(deniedStream.WrittenText(), Is.EqualTo("CAPA\r\n"));

        AuthenticationSequenceStream allowedStream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "+OK user accepted\r\n" +
            "+OK password accepted\r\n" +
            "-ERR transaction CAPA unavailable\r\n");
        Pop3ClientEngine allowedEngine = Connect(allowedStream, Options(false, true, null));

        Pop3AuthenticationResult result = allowedEngine.AuthenticateAutomatic(
            request,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(allowedStream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUSER alice\r\nPASS secret\r\nCAPA\r\n"));
    }

    [Test]
    public void AutomaticValidationAndCancellationOccurBeforeAuthenticationWrites()
    {
        AuthenticationSequenceStream stream = SaslSuccessStream(null, "SASL TEST");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        Pop3AuthenticationRequest invalidMethod = Request("alice", "secret");
        invalidMethod.Method = Pop3AuthenticationMethod.Sasl;
        Pop3AuthenticationRequest nullClients = Request("alice", "secret");
        nullClients.SaslClients = null!;
        Pop3AuthenticationRequest nullEntry = Request("alice", "secret");
        nullEntry.SaslClients = new IPop3SaslClient[] { null! };
        ScriptedSaslClient invalidClient = new ScriptedSaslClient("plain", "unused", "alice");
        Pop3AuthenticationRequest invalidMechanism = Request("alice", "secret");
        invalidMechanism.SaslClients = new IPop3SaslClient[] { invalidClient };

        Assert.Throws<ArgumentNullException>(() => engine.AuthenticateAutomatic(
            null!,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateAutomatic(
            invalidMethod,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateAutomatic(
            nullClients,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateAutomatic(
            nullEntry,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.AuthenticateAutomatic(
            invalidMechanism,
            CancellationToken.None));
        Assert.That(invalidClient.BeginCount, Is.Zero);

        ScriptedSaslClient cancelledClient = new ScriptedSaslClient("TEST", "unused", "alice");
        Pop3AuthenticationRequest cancelledRequest = Request("alice", "secret");
        cancelledRequest.SaslClients = new IPop3SaslClient[] { cancelledClient };
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => engine.AuthenticateAutomatic(
                cancelledRequest,
                cancellation.Token));
        }

        Assert.That(cancelledClient.MechanismCount, Is.Zero);
        Assert.That(cancelledClient.BeginCount, Is.Zero);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
    }

    [Test]
    public void ExplicitSaslCanProbeAnUnadvertisedCallerMechanism()
    {
        AuthenticationSequenceStream stream = SaslSuccessStream(null, "USER");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient client = new ScriptedSaslClient("TEST", "explicit", "alice");

        Pop3AuthenticationResult result = engine.AuthenticateSasl(
            client,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH TEST ZXhwbGljaXQ=\r\nCAPA\r\n"));
    }

    [Test]
    public void BuiltInPlainAuthenticatesThroughTheClientAndClearsCredentialBuffers()
    {
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = SaslSuccessStream(null, "SASL PLAIN");
        Pop3ClientEngine engine = Connect(stream, Options(true, false, logger));
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "alice";
        credentials.Password = "secret";
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);

        Pop3AuthenticationResult result = engine.AuthenticateSasl(
            client,
            CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.IsAuthenticated, Is.True);
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH PLAIN AGFsaWNlAHNlY3JldA==\r\nCAPA\r\n"));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "AUTH PLAIN [redacted]",
            "+OK authenticated"
        }));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain("secret"));
        Assert.That(client.AuthorizationIdentityBytes, Is.Empty);
        Assert.That(client.AuthenticationIdentityBytes, Is.Empty);
        Assert.That(client.PasswordBytes, Is.Empty);
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void OversizedBuiltInPlainInitialResponseUsesTheDeferredExchange()
    {
        string password = new string('s', 190);
        string response = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("\0alice\0" + password));
        RecordingLogger logger = new RecordingLogger();
        AuthenticationSequenceStream stream = AuthenticationSuccessStream(
            null,
            "SASL PLAIN",
            "+ \r\n+OK authenticated\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(true, false, logger));
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "alice";
        credentials.Password = password;
        Pop3PlainSaslClient client = new Pop3PlainSaslClient(credentials, true, false);

        Pop3AuthenticationResult result = engine.AuthenticateSasl(
            client,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nAUTH PLAIN\r\n" + response + "\r\nCAPA\r\n"));
        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "AUTH PLAIN",
            "+ [redacted]",
            "SASL [redacted]",
            "+OK authenticated"
        }));
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain(password));
        Assert.That(client.AuthorizationIdentityBytes, Is.Empty);
        Assert.That(client.AuthenticationIdentityBytes, Is.Empty);
        Assert.That(client.PasswordBytes, Is.Empty);
        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("AUTH/account", Pop3AuthenticationStatus.Rejected, -1)]
    [TestCase("LOGIN-DELAY/vendor", Pop3AuthenticationStatus.LoginDelay, 60)]
    [TestCase("IN-USE/maildrop", Pop3AuthenticationStatus.InUse, -1)]
    [TestCase("SYS/TEMP/backend", Pop3AuthenticationStatus.TemporaryFailure, -1)]
    [TestCase("SYS/PERM/storage", Pop3AuthenticationStatus.PermanentFailure, -1)]
    public void PeerResponseCodesProduceClassifiedExceptionsWithCompleteResponses(
        string responseCode,
        Pop3AuthenticationStatus expectedStatus,
        int expectedRetryAfterSeconds)
    {
        string responseText = "[" + responseCode + "] authentication detail";
        AuthenticationSequenceStream stream = new AuthenticationSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nSASL TEST\r\nLOGIN-DELAY 60\r\n.\r\n" +
            "-ERR " + responseText + "\r\n");
        Pop3ClientEngine engine = Connect(stream, Options(false, false, null));
        ScriptedSaslClient client = new ScriptedSaslClient("TEST", "proof", "alice");

        Pop3AuthenticationException exception = Assert.Throws<Pop3AuthenticationException>(() =>
            engine.AuthenticateSasl(client, CancellationToken.None))!;

        Assert.That(exception.Method, Is.EqualTo(Pop3AuthenticationMethod.Sasl));
        Assert.That(exception.Result.Status, Is.EqualTo(expectedStatus));
        Assert.That(exception.Result.RetryAfterSeconds, Is.EqualTo(expectedRetryAfterSeconds));
        Assert.That(exception.Result.FailureText, Is.EqualTo(responseText));
        Assert.That(exception.Response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(exception.Response.Text, Is.EqualTo(responseText));
        Assert.That(exception.Response.ResponseCodeLevels, Is.EqualTo(responseCode.Split('/')));
        Assert.That(exception.Response.IsMultiline, Is.False);
        Assert.That(exception.Response.AdvisoryOctetCount, Is.EqualTo(-1));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo(responseText));
        Assert.That(engine.GetSnapshot().LastResponse, Is.Not.SameAs(exception.Response));
    }

    private static AuthenticationSequenceStream SaslSuccessStream(
        string? timestamp,
        string capabilities)
    {
        return AuthenticationSuccessStream(
            timestamp,
            capabilities,
            "+OK authenticated\r\n");
    }

    private static AuthenticationSequenceStream AuthenticationSuccessStream(
        string? timestamp,
        string capabilities,
        string authenticationResponses)
    {
        string greeting = "+OK ready";
        if (timestamp != null)
        {
            greeting += " " + timestamp;
        }
        return new AuthenticationSequenceStream(
            greeting + "\r\n" +
            "+OK capabilities\r\n" + capabilities + "\r\n.\r\n" +
            authenticationResponses +
            "+OK transaction capabilities\r\nUIDL\r\n.\r\n");
    }

    private static Pop3ClientEngine Connect(
        AuthenticationSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        return engine;
    }

    private static Pop3ClientOptions Options(
        bool implicitTls,
        bool probeUser,
        IPop3ProtocolLogger? logger)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 128;
        options.LeaveStreamOpen = true;
        options.ProbeUserWhenCapaUnavailable = probeUser;
        options.Logger = logger;
        if (implicitTls)
        {
            options.SecurityMode = Pop3SecurityMode.ImplicitTls;
            options.TlsProvider = new IdentityTlsProvider();
        }
        return options;
    }

    private static Pop3AuthenticationRequest Request(string username, string password)
    {
        Pop3AuthenticationRequest request = new Pop3AuthenticationRequest();
        request.Credentials.Username = username;
        request.Credentials.Password = password;
        return request;
    }

    private class ScriptedSaslClient : IPop3SaslClient
    {
        string Mechanism;
        byte[] InitialResponse;
        string Identity;
        public int MechanismCount;
        public int BeginCount;
        public int CancelCount;

        public ScriptedSaslClient(string mechanism, string initialResponse, string identity)
        {
            Mechanism = mechanism;
            InitialResponse = Encoding.ASCII.GetBytes(initialResponse);
            Identity = identity;
        }

        public string GetMechanism()
        {
            MechanismCount++;
            return Mechanism;
        }

        public Pop3SaslStep BeginAuthentication()
        {
            BeginCount++;
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Success;
            step.Data = InitialResponse;
            step.HasData = true;
            step.AuthenticatedIdentity = Identity;
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            throw new InvalidOperationException("No challenge was expected.");
        }

        public void CancelAuthentication()
        {
            CancelCount++;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
        }
    }

    private class RejectingSaslClient : IPop3SaslClient
    {
        public int BeginCount;
        public int CancelCount;

        public string GetMechanism()
        {
            return "TEST";
        }

        public Pop3SaslStep BeginAuthentication()
        {
            BeginCount++;
            Pop3SaslStep step = new Pop3SaslStep();
            step.Status = Pop3SaslStepStatus.Rejected;
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            throw new InvalidOperationException("No challenge was expected.");
        }

        public void CancelAuthentication()
        {
            CancelCount++;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            throw new InvalidOperationException("No security layer was expected.");
        }
    }

    private class RecordingLogger : IPop3ProtocolLogger
    {
        public List<Pop3LogEntry> Entries = new List<Pop3LogEntry>();

        public void Log(Pop3LogEntry entry)
        {
            Entries.Add(entry);
        }

        public string[] Texts()
        {
            return Entries.Select(entry => entry.Text).ToArray();
        }
    }

    private class IdentityTlsProvider : IPop3ClientTlsProvider
    {
        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return stream;
        }
    }

    private class AuthenticationSequenceStream : Stream
    {
        byte[] ReadData;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public AuthenticationSequenceStream(string responses)
        {
            ReadData = Encoding.UTF8.GetBytes(responses);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return ReadData.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.UTF8.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = ReadData.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(ReadData, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Written.Write(buffer, offset, count);
        }
    }
}
