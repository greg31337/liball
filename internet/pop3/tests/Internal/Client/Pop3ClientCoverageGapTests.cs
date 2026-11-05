using System.Text;

namespace pop3.Tests;

public class Pop3ClientCoverageGapTests
{
    [Test]
    public void SequentialRetrieveAndTopBatchesPreserveOrderAndDestinations()
    {
        ScriptedDuplexStream retrieveStream = TransactionStream(
            "UIDL\r\n",
            ScriptedStreamEvent.ExpectAscii("RETR 1\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK first\r\none\r\n.\r\n"),
            ScriptedStreamEvent.ExpectAscii("RETR 2\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK second\r\ntwo\r\n.\r\n"));
        Pop3ClientEngine retrieveEngine = ConnectTransaction(retrieveStream);
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        long[] retrieveCounts = retrieveEngine.RetrieveMessagesToStreams(
            new[] { 1, 2 },
            new Stream[] { first, second },
            CancellationToken.None);

        Assert.That(retrieveCounts, Is.EqualTo(new long[] { 5, 5 }));
        Assert.That(Encoding.ASCII.GetString(first.ToArray()), Is.EqualTo("one\r\n"));
        Assert.That(Encoding.ASCII.GetString(second.ToArray()), Is.EqualTo("two\r\n"));
        Assert.That(retrieveStream.IsComplete, Is.True);
        retrieveEngine.CloseConnection();
        retrieveStream.Dispose();
        first.Dispose();
        second.Dispose();

        ScriptedDuplexStream topStream = TransactionStream(
            "TOP\r\n",
            ScriptedStreamEvent.ExpectAscii("TOP 1 0\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK first\r\nH: one\r\n\r\n.\r\n"),
            ScriptedStreamEvent.ExpectAscii("TOP 2 1\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK second\r\nH: two\r\n\r\nbody\r\n.\r\n"));
        Pop3ClientEngine topEngine = ConnectTransaction(topStream);
        MemoryStream firstTop = new MemoryStream();
        MemoryStream secondTop = new MemoryStream();

        long[] topCounts = topEngine.RetrieveTopBatchToStreams(
            new[] { 1, 2 },
            new[] { 0, 1 },
            new Stream[] { firstTop, secondTop },
            CancellationToken.None);

        Assert.That(topCounts, Is.EqualTo(new long[] { 10, 16 }));
        Assert.That(Encoding.ASCII.GetString(firstTop.ToArray()),
            Is.EqualTo("H: one\r\n\r\n"));
        Assert.That(Encoding.ASCII.GetString(secondTop.ToArray()),
            Is.EqualTo("H: two\r\n\r\nbody\r\n"));
        Assert.That(topStream.IsComplete, Is.True);
        topEngine.CloseConnection();
        topStream.Dispose();
        firstTop.Dispose();
        secondTop.Dispose();
    }

    [Test]
    public void PipelinedRetrieveBatchReturnsEveryTransferredOctetCount()
    {
        ScriptedDuplexStream stream = TransactionStream(
            "PIPELINING\r\n",
            ScriptedStreamEvent.ExpectAscii("RETR 1\r\n"),
            ScriptedStreamEvent.ExpectAscii("RETR 2\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK first\r\none\r\n.\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK second\r\ntwo\r\n.\r\n"));
        Pop3ClientOptions options = Options();
        options.EnablePipelining = true;
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        long[] counts = engine.RetrieveMessagesToStreams(
            new[] { 1, 2 },
            new Stream[] { first, second },
            CancellationToken.None);

        Assert.That(counts, Is.EqualTo(new long[] { 5, 5 }));
        Assert.That(Encoding.ASCII.GetString(first.ToArray()), Is.EqualTo("one\r\n"));
        Assert.That(Encoding.ASCII.GetString(second.ToArray()), Is.EqualTo("two\r\n"));
        Assert.That(stream.IsComplete, Is.True);
        engine.CloseConnection();
        stream.Dispose();
        first.Dispose();
        second.Dispose();
    }

    [TestCase(Pop3Commands.Stat)]
    [TestCase(Pop3Commands.Rset)]
    [TestCase(Pop3Commands.Noop)]
    [TestCase(Pop3Commands.Quit)]
    public void TransactionCommandsCloseOnUnexpectedContinuationResponses(
        string keyword)
    {
        ScriptedDuplexStream stream = TransactionStream(
            "",
            ScriptedStreamEvent.ExpectAscii(keyword + "\r\n"),
            ScriptedStreamEvent.ReturnAscii("+ unexpected\r\n"));
        Pop3ClientEngine engine = ConnectTransaction(stream);

        Assert.Throws<Pop3ProtocolException>(() => ExecuteTransactionCommand(
            engine, keyword));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsComplete, Is.True);
        stream.Dispose();
    }

    [Test]
    public void LanguageSelectionClosesOnUnexpectedContinuationResponse()
    {
        ScriptedDuplexStream stream = AuthorizationStream(
            "LANG\r\n",
            ScriptedStreamEvent.ExpectAscii("LANG fr\r\n"),
            ScriptedStreamEvent.ReturnAscii("+ unexpected\r\n"));
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Assert.Throws<Pop3ProtocolException>(() =>
            engine.SelectLanguage("fr", CancellationToken.None));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsComplete, Is.True);
        stream.Dispose();
    }

    [Test]
    public void TlsUpgradeClosesOnUnexpectedContinuationResponse()
    {
        ScriptedDuplexStream stream = AuthorizationStream(
            "STLS\r\n",
            ScriptedStreamEvent.ExpectAscii("STLS\r\n"),
            ScriptedStreamEvent.ReturnAscii("+ unexpected\r\n"));
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Assert.Throws<Pop3ProtocolException>(() =>
            engine.UpgradeToTls(CancellationToken.None));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsComplete, Is.True);
        stream.Dispose();
    }

    [Test]
    public void PublicClientForwardsTlsUpgradeAndAutomaticAuthentication()
    {
        ScriptedDuplexStream tlsStream = AuthorizationStream(
            "STLS\r\n",
            ScriptedStreamEvent.ExpectAscii("STLS\r\n"),
            ScriptedStreamEvent.ReturnAscii("+ unexpected\r\n"));
        Pop3Client tlsClient = new Pop3Client();
        tlsClient.ConnectStream(tlsStream, Options(), CancellationToken.None);

        Assert.Throws<Pop3ProtocolException>(() =>
            tlsClient.UpgradeToTls(CancellationToken.None));
        Assert.That(tlsClient.GetSnapshot().State,
            Is.EqualTo(Pop3SessionState.Closed));
        tlsStream.Dispose();

        ScriptedDuplexStream authenticationStream = AuthorizationStream(
            "USER\r\n",
            ScriptedStreamEvent.ExpectAscii("USER alice\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK user accepted\r\n"),
            ScriptedStreamEvent.ExpectAscii("PASS secret\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK authenticated\r\n"),
            ScriptedStreamEvent.ExpectAscii("CAPA\r\n"),
            ScriptedStreamEvent.ReturnAscii(
                "+OK transaction capabilities\r\nUSER\r\n.\r\n"));
        Pop3Client authenticationClient = new Pop3Client();
        authenticationClient.ConnectStream(
            authenticationStream, Options(), CancellationToken.None);
        Pop3AuthenticationRequest request = new Pop3AuthenticationRequest();
        request.Method = Pop3AuthenticationMethod.Automatic;
        request.AllowUnprotectedCredentials = true;
        request.Credentials.Username = "alice";
        request.Credentials.Password = "secret";

        Pop3AuthenticationResult result = authenticationClient.AuthenticateAutomatic(
            request, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(authenticationClient.GetSnapshot().State,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(authenticationStream.IsComplete, Is.True);
        authenticationClient.Abort();
        authenticationStream.Dispose();
    }

    [Test]
    public void PublicClientCompletesAnExplicitTlsUpgradeAndRefreshesCapabilities()
    {
        ScriptedDuplexStream stream = AuthorizationStream(
            "STLS\r\n",
            ScriptedStreamEvent.ExpectAscii("STLS\r\n"),
            ScriptedStreamEvent.ReturnAscii("+OK begin TLS\r\n"),
            ScriptedStreamEvent.ExpectAscii("CAPA\r\n"),
            ScriptedStreamEvent.ReturnAscii(
                "+OK protected capabilities\r\nUSER\r\n.\r\n"));
        FakeTlsProvider provider = new FakeTlsProvider();
        Pop3ClientOptions options = Options();
        options.TlsProvider = provider;
        Pop3Client client = new Pop3Client();
        client.ConnectStream(stream, options, CancellationToken.None);

        client.UpgradeToTls(CancellationToken.None);

        Pop3ClientSnapshot snapshot = client.GetSnapshot();
        Assert.That(snapshot.IsSecure, Is.True);
        Assert.That(snapshot.Capabilities.SupportsStls, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUser, Is.True);
        Assert.That(provider.ClientCallCount, Is.EqualTo(1));
        Assert.That(provider.LastClientInputStream, Is.SameAs(stream));
        Assert.That(stream.IsComplete, Is.True);
        client.Abort();
        stream.Dispose();
    }

    static void ExecuteTransactionCommand(Pop3ClientEngine engine, string keyword)
    {
        switch (keyword)
        {
            case Pop3Commands.Stat:
                engine.GetMailboxStatus(CancellationToken.None);
                break;
            case Pop3Commands.Rset:
                engine.ResetDeletions(CancellationToken.None);
                break;
            case Pop3Commands.Noop:
                engine.NoOperation(CancellationToken.None);
                break;
            default:
                engine.QuitAndCommit(CancellationToken.None);
                break;
        }
    }

    static Pop3ClientEngine ConnectTransaction(ScriptedDuplexStream stream)
    {
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    static Pop3ClientEngine ConnectAuthorization(ScriptedDuplexStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, Options(), CancellationToken.None);
        return engine;
    }

    static ScriptedDuplexStream TransactionStream(
        string capabilities,
        params ScriptedStreamEvent[] commandEvents)
    {
        List<ScriptedStreamEvent> events = new List<ScriptedStreamEvent>();
        events.Add(ScriptedStreamEvent.ReturnAscii("+OK ready\r\n"));
        AddCapabilityExchange(events, capabilities);
        AddCapabilityExchange(events, capabilities);
        events.AddRange(commandEvents);
        return new ScriptedDuplexStream(events.ToArray(), 7);
    }

    static ScriptedDuplexStream AuthorizationStream(
        string capabilities,
        params ScriptedStreamEvent[] commandEvents)
    {
        List<ScriptedStreamEvent> events = new List<ScriptedStreamEvent>();
        events.Add(ScriptedStreamEvent.ReturnAscii("+OK ready\r\n"));
        AddCapabilityExchange(events, capabilities);
        events.AddRange(commandEvents);
        return new ScriptedDuplexStream(events.ToArray(), 7);
    }

    static void AddCapabilityExchange(
        List<ScriptedStreamEvent> events,
        string capabilities)
    {
        events.Add(ScriptedStreamEvent.ExpectAscii("CAPA\r\n"));
        events.Add(ScriptedStreamEvent.ReturnAscii(
            "+OK capabilities\r\n" + capabilities + ".\r\n"));
    }

    static Pop3ClientOptions Options()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.LeaveStreamOpen = true;
        options.EnablePipelining = false;
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.Tls.TargetHost = "localhost";
        return options;
    }
}
