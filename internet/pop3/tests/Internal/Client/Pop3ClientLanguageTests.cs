using System.Text;

namespace pop3.Tests;

public class Pop3ClientLanguageTests
{
    [Test]
    public void ListsLanguagesInServerOrderWithUtf8Descriptions()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "+OK languages follow\r\n" +
            "en English\r\n" +
            "de Deutsch – Österreich\r\n" +
            "x-private\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Pop3Language[] languages = engine.GetLanguages(CancellationToken.None);

        Assert.That(languages.Select(language => language.Tag),
            Is.EqualTo(new[] { "en", "de", "x-private" }));
        Assert.That(languages.Select(language => language.Description),
            Is.EqualTo(new[] { "English", "Deutsch – Österreich", "" }));
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.Null);
        Assert.That(engine.GetSnapshot().LastResponse!.IsMultiline, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nLANG\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void LanguageSelectionSurvivesAuthenticationAndWorksInTransaction()
    {
        LanguageSequenceStream stream = CreateTransactionStream(
            "+OK selected\r\n" +
            "+OK languages follow\r\n" +
            "en English\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        engine.SelectLanguage("en-US", CancellationToken.None);

        engine.CompleteAuthentication("alice", CancellationToken.None);
        Pop3Language[] languages = engine.GetLanguages(CancellationToken.None);

        Assert.That(languages.Length, Is.EqualTo(1));
        Assert.That(languages[0].Tag, Is.EqualTo("en"));
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.EqualTo("en-US"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nLANG en-US\r\nCAPA\r\nLANG\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void SelectionAndWildcardResetBothWorkInTransaction()
    {
        LanguageSequenceStream stream = new LanguageSequenceStream(Encoding.UTF8.GetBytes(
            "+OK ready\r\n" +
            CapabilityResponse(true) +
            CapabilityResponse(true) +
            "+OK selected\r\n" +
            "+OK default restored\r\n"));
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        engine.CompleteAuthentication("alice", CancellationToken.None);

        engine.SelectLanguage("de-AT", CancellationToken.None);
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.EqualTo("de-AT"));
        engine.ResetLanguage(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.SelectedLanguage, Is.Null);
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("default restored"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLANG de-AT\r\nLANG *\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void PostAuthenticationCapabilityLossPreservesSelectionButBlocksCommands()
    {
        LanguageSequenceStream stream = new LanguageSequenceStream(Encoding.UTF8.GetBytes(
            "+OK ready\r\n" +
            CapabilityResponse(true) +
            "+OK selected\r\n" +
            CapabilityResponse(false)));
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        engine.SelectLanguage("fr", CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        string writesBefore = stream.WrittenText();

        Assert.Throws<InvalidOperationException>(() =>
            engine.GetLanguages(CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.SelectLanguage("de", CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.ResetLanguage(CancellationToken.None));

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Capabilities.SupportsLang, Is.False);
        Assert.That(snapshot.SelectedLanguage, Is.EqualTo("fr"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(writesBefore));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void WildcardResetClearsSelectedLanguageAfterServerAcceptance()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "+OK selected\r\n" +
            "+OK default restored\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        engine.SelectLanguage("fr", CancellationToken.None);
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.EqualTo("fr"));
        engine.ResetLanguage(CancellationToken.None);

        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.Null);
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("default restored"));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nLANG fr\r\nLANG *\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void RejectedSelectionAndResetPreserveThePreviousLanguage()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "+OK selected\r\n" +
            "-ERR unknown language\r\n" +
            "-ERR reset unavailable\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        engine.SelectLanguage("fr", CancellationToken.None);

        Pop3CommandException selectionFailure = Assert.Throws<Pop3CommandException>(() =>
            engine.SelectLanguage("de", CancellationToken.None))!;
        Pop3CommandException resetFailure = Assert.Throws<Pop3CommandException>(() =>
            engine.ResetLanguage(CancellationToken.None))!;

        Assert.That(selectionFailure.Command.Arguments, Is.EqualTo(new[] { "de" }));
        Assert.That(selectionFailure.Response.Text, Is.EqualTo("unknown language"));
        Assert.That(resetFailure.Command.Arguments, Is.EqualTo(new[] { "*" }));
        Assert.That(resetFailure.Response.Text, Is.EqualTo("reset unavailable"));
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.EqualTo("fr"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nLANG fr\r\nLANG de\r\nLANG *\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void MissingLangCapabilityPreventsEveryLanguageCommand()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(false, "");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Assert.Throws<InvalidOperationException>(() =>
            engine.GetLanguages(CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.SelectLanguage("en", CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.ResetLanguage(CancellationToken.None));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void InvalidSelectionTagsAreRejectedBeforeWriting()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(true, "");
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        string longTag = "en-" + string.Join(
            "-",
            Enumerable.Repeat("abcdefgh", 28));

        Assert.Throws<ArgumentNullException>(() =>
            engine.SelectLanguage(null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage("", CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage("*", CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage("1n", CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage("en_US", CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage("français", CancellationToken.None));
        Assert.Throws<ArgumentException>(() =>
            engine.SelectLanguage(longTag, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void MalformedUtf8DescriptionInvalidatesTheSession()
    {
        byte[] prefix = Encoding.UTF8.GetBytes(
            "+OK ready\r\n" +
            "+OK capabilities\r\n" +
            "LANG\r\n" +
            ".\r\n" +
            "+OK languages follow\r\n" +
            "en ");
        byte[] suffix = Encoding.ASCII.GetBytes("\r\n.\r\n");
        byte[] responses = new byte[prefix.Length + 1 + suffix.Length];
        Array.Copy(prefix, responses, prefix.Length);
        responses[prefix.Length] = 195;
        Array.Copy(suffix, 0, responses, prefix.Length + 1, suffix.Length);
        LanguageSequenceStream stream = new LanguageSequenceStream(responses);
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetLanguages(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LANG response"));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nLANG\r\n"));
        stream.Dispose();
    }

    [TestCase("1n Invalid tag\r\n")]
    [TestCase("en  Invalid spacing\r\n")]
    [TestCase("en English\r\nEN Duplicate\r\n")]
    public void MalformedLanguageListingsInvalidateTheSession(string listing)
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "+OK languages follow\r\n" +
            listing +
            ".\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetLanguages(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("LANG response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nLANG\r\n"));
        stream.Dispose();
    }

    [Test]
    public void RejectedListingIsRetryableWithoutChangingSelection()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "-ERR listing unavailable\r\n" +
            "+OK languages follow\r\n" +
            "en English\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetLanguages(CancellationToken.None))!;
        Pop3Language[] languages = engine.GetLanguages(CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Lang));
        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(exception.Response.Text, Is.EqualTo("listing unavailable"));
        Assert.That(languages[0].Tag, Is.EqualTo("en"));
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.Null);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nLANG\r\nLANG\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void IncompleteLanguageListingClosesWithEndOfStreamContext()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(
            true,
            "+OK languages follow\r\n" +
            "en English\r\n");
        Pop3ClientEngine engine = ConnectAuthorization(stream);

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.GetLanguages(CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Lang));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void PreCancelledLanguageOperationsDoNotWriteOrChangeState()
    {
        LanguageSequenceStream stream = CreateAuthorizationStream(true, "");
        Pop3ClientEngine engine = ConnectAuthorization(stream);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.GetLanguages(source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                engine.SelectLanguage("en", source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                engine.ResetLanguage(source.Token));
        }

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(engine.GetSnapshot().SelectedLanguage, Is.Null);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.Abort();
        stream.Dispose();
    }

    static Pop3ClientEngine ConnectAuthorization(LanguageSequenceStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(), CancellationToken.None);
        return engine;
    }

    static LanguageSequenceStream CreateAuthorizationStream(
        bool supportsLang,
        string responses)
    {
        return new LanguageSequenceStream(Encoding.UTF8.GetBytes(
            "+OK ready\r\n" + CapabilityResponse(supportsLang) + responses));
    }

    static LanguageSequenceStream CreateTransactionStream(string responses)
    {
        return new LanguageSequenceStream(Encoding.UTF8.GetBytes(
            "+OK ready\r\n" + CapabilityResponse(true) + responses.Insert(
                responses.IndexOf("+OK languages follow", StringComparison.Ordinal),
                CapabilityResponse(true))));
    }

    static string CapabilityResponse(bool supportsLang)
    {
        if (supportsLang)
        {
            return "+OK capabilities\r\nLANG\r\n.\r\n";
        }
        return "+OK capabilities\r\nUIDL\r\n.\r\n";
    }

    static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.LeaveStreamOpen = true;
        return options;
    }

    class LanguageSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public LanguageSequenceStream(byte[] responses)
        {
            Responses = responses;
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
            get { return Responses.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
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
            int remaining = Responses.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(Responses, ReadPosition, buffer, offset, readCount);
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
