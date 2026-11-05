using System.Text;

namespace pop3.Tests;

public class Pop3ClientCapabilityLifecycleTests
{
    [Test]
    public void ConnectRefreshesAndInterpretsAllAuthorizationCapabilities()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            AuthorizationCapabilities());
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.Capabilities.Entries, Has.Length.EqualTo(14));
        Assert.That(snapshot.Capabilities.Entries[0].Name, Is.EqualTo("top"));
        Assert.That(snapshot.Capabilities.Entries[13].Name, Is.EqualTo("X-Vendor"));
        Assert.That(snapshot.Capabilities.Entries[13].Parameters, Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(snapshot.Capabilities.SupportsTop, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.True);
        Assert.That(snapshot.Capabilities.SaslMechanisms, Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(snapshot.Capabilities.SupportsResponseCodes, Is.True);
        Assert.That(snapshot.Capabilities.HasLoginDelay, Is.True);
        Assert.That(snapshot.Capabilities.LoginDelaySeconds, Is.EqualTo(900));
        Assert.That(snapshot.Capabilities.LoginDelayPerUser, Is.True);
        Assert.That(snapshot.Capabilities.SupportsPipelining, Is.True);
        Assert.That(snapshot.Capabilities.HasExpire, Is.True);
        Assert.That(snapshot.Capabilities.ExpireNever, Is.True);
        Assert.That(snapshot.Capabilities.ExpireDays, Is.Zero);
        Assert.That(snapshot.Capabilities.ExpirePerUser, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.True);
        Assert.That(snapshot.Capabilities.Implementation, Is.EqualTo("Example POP3 Server"));
        Assert.That(snapshot.Capabilities.SupportsStls, Is.True);
        Assert.That(snapshot.Capabilities.SupportsAuthenticationResponseCode, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUtf8, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUtf8User, Is.True);
        Assert.That(snapshot.Capabilities.SupportsLang, Is.True);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("authorization capabilities"));

        engine.CloseConnection();
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void ExplicitRefreshReplacesTheCompleteCapabilityView()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            AuthorizationCapabilities(),
            "+OK refreshed\r\nTOP\r\nLOGIN-DELAY 60 USER\r\nEXPIRE 30 USER\r\nIMPLEMENTATION Refreshed\r\nUTF8\r\n.\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Pop3CapabilitySet refreshed = engine.RefreshCapabilities(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(refreshed.SupportsTop, Is.True);
        Assert.That(refreshed.SupportsUser, Is.False);
        Assert.That(refreshed.SaslMechanisms, Is.Empty);
        Assert.That(refreshed.LoginDelaySeconds, Is.EqualTo(60));
        Assert.That(refreshed.LoginDelayPerUser, Is.True);
        Assert.That(refreshed.ExpireNever, Is.False);
        Assert.That(refreshed.ExpireDays, Is.EqualTo(30));
        Assert.That(refreshed.ExpirePerUser, Is.True);
        Assert.That(refreshed.Implementation, Is.EqualTo("Refreshed"));
        Assert.That(refreshed.SupportsUtf8, Is.True);
        Assert.That(refreshed.SupportsUtf8User, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(snapshot.Capabilities.Entries, Has.Length.EqualTo(5));
        Assert.That(snapshot.Capabilities, Is.Not.SameAs(refreshed));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("refreshed"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void CompletedAuthenticationRefreshesTransactionCapabilities()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            AuthorizationCapabilities(),
            TransactionCapabilities());
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Pop3CapabilitySet refreshed = engine.CompleteAuthentication(
            "alice",
            CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.IsAuthenticated, Is.True);
        Assert.That(snapshot.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(snapshot.Capabilities.Entries, Has.Length.EqualTo(12));
        Assert.That(snapshot.Capabilities.SupportsTop, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.Capabilities.SaslMechanisms, Is.EqualTo(new[] { "OAUTHBEARER" }));
        Assert.That(snapshot.Capabilities.SupportsResponseCodes, Is.True);
        Assert.That(snapshot.Capabilities.HasLoginDelay, Is.True);
        Assert.That(snapshot.Capabilities.LoginDelaySeconds, Is.EqualTo(60));
        Assert.That(snapshot.Capabilities.LoginDelayPerUser, Is.False);
        Assert.That(snapshot.Capabilities.SupportsPipelining, Is.True);
        Assert.That(snapshot.Capabilities.HasExpire, Is.True);
        Assert.That(snapshot.Capabilities.ExpireNever, Is.False);
        Assert.That(snapshot.Capabilities.ExpireDays, Is.EqualTo(30));
        Assert.That(snapshot.Capabilities.ExpirePerUser, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.True);
        Assert.That(snapshot.Capabilities.Implementation, Is.EqualTo("Post Auth"));
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(snapshot.Capabilities.SupportsAuthenticationResponseCode, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUtf8, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUtf8User, Is.False);
        Assert.That(snapshot.Capabilities.SupportsLang, Is.True);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("transaction capabilities"));
        Assert.That(refreshed.Entries, Has.Length.EqualTo(12));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void NegativeInitialCapaKeepsAConnectedConservativeEmptyView()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(snapshot.IsConnected, Is.True);
        Assert.That(snapshot.Capabilities.Entries, Is.Empty);
        Assert.That(snapshot.Capabilities.SupportsTop, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUidl, Is.False);
        Assert.That(snapshot.Capabilities.SupportsPipelining, Is.False);
        Assert.That(snapshot.Capabilities.SaslMechanisms, Is.Empty);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUtf8, Is.False);
        Assert.That(snapshot.Capabilities.SupportsLang, Is.False);
        Assert.That(engine.IsCapaAvailable(), Is.False);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.False);
        Assert.That(engine.CanAttemptTop(), Is.False);
        Assert.That(engine.CanAttemptUidl(), Is.False);
        Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(snapshot.LastResponse.Text, Is.EqualTo("CAPA unavailable"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public void UnavailableCapaEnablesOnlyConfiguredLegacyProbes(
        bool probeUser,
        bool probeTop,
        bool probeUidl)
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n");
        Pop3ClientOptions options = CreateOptions(true);
        options.ProbeUserWhenCapaUnavailable = probeUser;
        options.ProbeTopWhenCapaUnavailable = probeTop;
        options.ProbeUidlWhenCapaUnavailable = probeUidl;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, options, CancellationToken.None);

        Pop3CapabilitySet advertised = engine.GetSnapshot().Capabilities;
        Assert.That(engine.IsCapaAvailable(), Is.False);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.EqualTo(probeUser));
        Assert.That(engine.CanAttemptTop(), Is.EqualTo(probeTop));
        Assert.That(engine.CanAttemptUidl(), Is.EqualTo(probeUidl));
        Assert.That(advertised.SupportsUser, Is.False);
        Assert.That(advertised.SupportsTop, Is.False);
        Assert.That(advertised.SupportsUidl, Is.False);
        Assert.That(advertised.SupportsPipelining, Is.False);
        Assert.That(advertised.SaslMechanisms, Is.Empty);
        Assert.That(advertised.SupportsStls, Is.False);
        Assert.That(advertised.SupportsAuthenticationResponseCode, Is.False);
        Assert.That(advertised.SupportsUtf8, Is.False);
        Assert.That(advertised.SupportsLang, Is.False);

        engine.CloseConnection();
        Assert.That(engine.CanAttemptUserAuthentication(), Is.False);
        Assert.That(engine.CanAttemptTop(), Is.False);
        Assert.That(engine.CanAttemptUidl(), Is.False);
        stream.Dispose();
    }

    [Test]
    public void AvailableEmptyCapaDoesNotEnableConfiguredProbes()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "+OK no extensions\r\n.\r\n");
        Pop3ClientOptions options = CreateOptions(true);
        options.ProbeUserWhenCapaUnavailable = true;
        options.ProbeTopWhenCapaUnavailable = true;
        options.ProbeUidlWhenCapaUnavailable = true;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, options, CancellationToken.None);

        Assert.That(engine.IsCapaAvailable(), Is.True);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.False);
        Assert.That(engine.CanAttemptTop(), Is.False);
        Assert.That(engine.CanAttemptUidl(), Is.False);

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void AdvertisedLegacyCommandsAreUsableWithoutProbeOptions()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "+OK extensions\r\nUSER\r\nTOP\r\nUIDL\r\n.\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Assert.That(engine.IsCapaAvailable(), Is.True);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.True);
        Assert.That(engine.CanAttemptTop(), Is.True);
        Assert.That(engine.CanAttemptUidl(), Is.True);

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void ExplicitRefreshReevaluatesWhetherFallbackProbesAreAllowed()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "-ERR CAPA unavailable\r\n",
            "+OK authoritative empty list\r\n.\r\n",
            "-ERR CAPA unavailable again\r\n");
        Pop3ClientOptions options = CreateOptions(true);
        options.ProbeUserWhenCapaUnavailable = true;
        options.ProbeTopWhenCapaUnavailable = true;
        options.ProbeUidlWhenCapaUnavailable = true;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.True);

        engine.RefreshCapabilities(CancellationToken.None);

        Assert.That(engine.IsCapaAvailable(), Is.True);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.False);
        Assert.That(engine.CanAttemptTop(), Is.False);
        Assert.That(engine.CanAttemptUidl(), Is.False);

        engine.RefreshCapabilities(CancellationToken.None);

        Assert.That(engine.IsCapaAvailable(), Is.False);
        Assert.That(engine.CanAttemptUserAuthentication(), Is.True);
        Assert.That(engine.CanAttemptTop(), Is.True);
        Assert.That(engine.CanAttemptUidl(), Is.True);
        Assert.That(engine.GetSnapshot().Capabilities.Entries, Is.Empty);

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void MalformedInitialCapabilitiesCloseAndHonorStreamOwnership(
        bool leaveOpen,
        bool expectedDisposed)
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            "+OK malformed follows\r\nTOP unexpected\r\n.\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.ConnectStream(stream, CreateOptions(leaveOpen), CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("CAPA"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [Test]
    public void PreCanceledExplicitRefreshDoesNotWriteOrCloseTheSession()
    {
        CapabilitySequenceStream stream = new CapabilitySequenceStream(
            "+OK ready\r\n",
            AuthorizationCapabilities(),
            "+OK unused\r\n.\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.RefreshCapabilities(cancellation.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().IsConnected, Is.True);

        engine.CloseConnection();
        stream.Dispose();
    }

    private static Pop3ClientOptions CreateOptions(bool leaveOpen)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.example.test";
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 512;
        options.LeaveStreamOpen = leaveOpen;
        return options;
    }

    private static string AuthorizationCapabilities()
    {
        return "+OK authorization capabilities\r\n" +
            "top\r\n" +
            "USER\r\n" +
            "SASL SCRAM-SHA-256 PLAIN\r\n" +
            "RESP-CODES\r\n" +
            "LOGIN-DELAY 900 USER\r\n" +
            "PIPELINING\r\n" +
            "EXPIRE NEVER USER\r\n" +
            "UIDL\r\n" +
            "IMPLEMENTATION Example POP3 Server\r\n" +
            "STLS\r\n" +
            "AUTH-RESP-CODE\r\n" +
            "UTF8 USER\r\n" +
            "LANG\r\n" +
            "X-Vendor alpha beta\r\n" +
            ".\r\n";
    }

    private static string TransactionCapabilities()
    {
        return "+OK transaction capabilities\r\n" +
            "TOP\r\n" +
            "SASL OAUTHBEARER\r\n" +
            "RESP-CODES\r\n" +
            "LOGIN-DELAY 60\r\n" +
            "PIPELINING\r\n" +
            "EXPIRE 30\r\n" +
            "UIDL\r\n" +
            "IMPLEMENTATION Post Auth\r\n" +
            "AUTH-RESP-CODE\r\n" +
            "UTF8\r\n" +
            "LANG\r\n" +
            "X-TRANSACTION current\r\n" +
            ".\r\n";
    }

    private class CapabilitySequenceStream : Stream
    {
        string[] CapabilityResponses;
        int CapabilityResponseIndex;
        byte[] ActiveReadData;
        int ActiveReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public CapabilitySequenceStream(string greeting, params string[] capabilityResponses)
        {
            ActiveReadData = Encoding.ASCII.GetBytes(greeting);
            CapabilityResponses = capabilityResponses;
        }

        public override bool CanRead
        {
            get { return !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.ASCII.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadCore(buffer, offset, count);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadCore(buffer, offset, count));
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
            string command = Encoding.ASCII.GetString(buffer, offset, count);
            if (command == "CAPA\r\n")
            {
                if (CapabilityResponseIndex < CapabilityResponses.Length)
                {
                    ActiveReadData = Encoding.ASCII.GetBytes(
                        CapabilityResponses[CapabilityResponseIndex]);
                    CapabilityResponseIndex++;
                }
                else
                {
                    ActiveReadData = new byte[0];
                }
                ActiveReadPosition = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
            {
                Written.Dispose();
            }
            base.Dispose(disposing);
        }

        int ReadCore(byte[] buffer, int offset, int count)
        {
            int remaining = ActiveReadData.Length - ActiveReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(ActiveReadData, ActiveReadPosition, buffer, offset, readCount);
            ActiveReadPosition += readCount;
            return readCount;
        }
    }
}
