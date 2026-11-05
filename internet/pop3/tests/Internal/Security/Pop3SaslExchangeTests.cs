using System.Text;

namespace pop3.Tests;

public class Pop3SaslExchangeTests
{
    [Test]
    public void InitialResponseAndMultipleChallengesCompleteTheExchange()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Ascii("initial"), true, null),
            Step(Pop3SaslStepStatus.Continue, Ascii("middle"), true, null),
            Step(Pop3SaslStepStatus.Success, Ascii("final"), true, "alice"));
        ExchangeHarness harness = new ExchangeHarness(
            "+ Y2hhbGxlbmdlLTE=\r\n+ Y2hhbGxlbmdlLTI=\r\n+OK authenticated\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            Pop3Defaults.DefaultMaximumSaslLineOctets,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(result.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(result.Response!.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(result.Response.Text, Is.EqualTo("authenticated"));
        Assert.That(client.Challenges.Select(Encoding.ASCII.GetString), Is.EqualTo(new[]
        {
            "challenge-1",
            "challenge-2"
        }));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo(
            "AUTH TEST aW5pdGlhbA==\r\nbWlkZGxl\r\nZmluYWw=\r\n"));
        Assert.That(client.CancelCount, Is.Zero);
    }

    [Test]
    public void InitialResponseCanBeOmittedUntilTheServerChallenges()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(Pop3SaslStepStatus.Success, Ascii("answer"), true, "bob"));
        ExchangeHarness harness = new ExchangeHarness("+ cXVlc3Rpb24=\r\n+OK done\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(client.Challenges.Single(), Is.EqualTo(Ascii("question")));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\nYW5zd2Vy\r\n"));
    }

    [Test]
    public void ExplicitlyEmptyInitialResponseUsesThePop3Marker()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "EXTERNAL",
            Step(Pop3SaslStepStatus.Success, Array.Empty<byte>(), true, "external"));
        ExchangeHarness harness = new ExchangeHarness("+OK done\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH EXTERNAL =\r\n"));
    }

    [Test]
    public void OversizedInitialResponseFallsBackToAnEmptyChallengeRoundTrip()
    {
        byte[] initial = new byte[190];
        int index = 0;
        while (index < initial.Length)
        {
            initial[index] = (byte)index;
            index++;
        }
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Success, initial, true, "large"));
        ExchangeHarness harness = new ExchangeHarness("+ \r\n+OK done\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo(
            "AUTH TEST\r\n" + Convert.ToBase64String(initial) + "\r\n"));
    }

    [Test]
    public void EmptyChallengesAndResponsesArePreservedAcrossMultipleSteps()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), true, null),
            Step(Pop3SaslStepStatus.Success, new byte[] { 2 }, true, "alice"));
        ExchangeHarness harness = new ExchangeHarness("+ \r\n+ AQ==\r\n+OK done\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(client.Challenges[0], Is.Empty);
        Assert.That(client.Challenges[1], Is.EqualTo(new byte[] { 1 }));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n\r\nAg==\r\n"));
    }

    [Test]
    public void LargeChallengeUsesTheConfiguredSaslLimitInsteadOfTheNormalResponseLimit()
    {
        byte[] challenge = new byte[4096];
        int index = 0;
        while (index < challenge.Length)
        {
            challenge[index] = (byte)(index % 251);
            index++;
        }
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(Pop3SaslStepStatus.Success, Ascii("done"), true, "alice"));
        ExchangeHarness harness = new ExchangeHarness(
            "+ " + Convert.ToBase64String(challenge) + "\r\n+OK done\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            8192,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(client.Challenges.Single(), Is.EqualTo(challenge));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\nZG9uZQ==\r\n"));
    }

    [Test]
    public void NegativeAuthResponseLeavesTheWireSynchronizedForRetry()
    {
        ExchangeHarness harness = new ExchangeHarness("-ERR rejected\r\n+OK accepted\r\n");
        ScriptedSaslClient rejectedClient = OneShotClient("first", "alice");
        ScriptedSaslClient acceptedClient = OneShotClient("second", "alice");

        Pop3SaslExchangeResult rejected = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            rejectedClient,
            1024,
            CancellationToken.None);
        Pop3SaslExchangeResult accepted = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            acceptedClient,
            1024,
            CancellationToken.None);

        Assert.That(rejected.Status, Is.EqualTo(Pop3SaslStepStatus.Rejected));
        Assert.That(rejected.Response!.Text, Is.EqualTo("rejected"));
        Assert.That(rejectedClient.CancelCount, Is.EqualTo(1));
        Assert.That(accepted.Status, Is.EqualTo(Pop3SaslStepStatus.Success));
        Assert.That(acceptedClient.CancelCount, Is.Zero);
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo(
            "AUTH TEST Zmlyc3Q=\r\nAUTH TEST c2Vjb25k\r\n"));
    }

    [TestCase(Pop3SaslStepStatus.Cancelled)]
    [TestCase(Pop3SaslStepStatus.Rejected)]
    public void LocalTerminationSendsCancellationAndConsumesTheRejection(Pop3SaslStepStatus status)
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(status, Array.Empty<byte>(), false, null));
        ExchangeHarness harness = new ExchangeHarness("+ Y2hhbGxlbmdl\r\n-ERR cancelled\r\n");

        Pop3SaslExchangeResult result = harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Response!.Text, Is.EqualTo("cancelled"));
        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [TestCase("Z g==")]
    [TestCase("A")]
    [TestCase("A===")]
    [TestCase("=AAA")]
    [TestCase("Zm=8")]
    public void MalformedChallengeIsCancelledBeforeItIsReported(string challenge)
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null));
        ExchangeHarness harness = new ExchangeHarness(
            "+ " + challenge + "\r\n-ERR cancelled\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None));

        Assert.That(client.ContinueCount, Is.Zero);
        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [Test]
    public void OversizedChallengeResponseIsCancelledBeforeItIsWritten()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(Pop3SaslStepStatus.Success, new byte[16], true, "alice"));
        ExchangeHarness harness = new ExchangeHarness("+ YQ==\r\n-ERR cancelled\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            16,
            CancellationToken.None));

        Assert.That(client.ContinueCount, Is.EqualTo(1));
        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [Test]
    public void DeferredInitialResponseMustFitTheConfiguredSaslLineLimit()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Success, new byte[190], true, "alice"));
        ExchangeHarness harness = new ExchangeHarness("+ \r\n-ERR cancelled\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            32,
            CancellationToken.None));

        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [Test]
    public void ServerCannotReportSuccessWhileTheClientNeedsMoreData()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Ascii("first"), true, null));
        ExchangeHarness harness = new ExchangeHarness("+OK premature\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None));

        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST Zmlyc3Q=\r\n"));
    }

    [Test]
    public void Pop3AdditionalSuccessDataChallengeIsRejectedWithCancellation()
    {
        ScriptedSaslClient client = OneShotClient("final", "alice");
        ExchangeHarness harness = new ExchangeHarness("+ ZXh0cmE=\r\n-ERR cancelled\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None));

        Assert.That(client.ContinueCount, Is.Zero);
        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST ZmluYWw=\r\n*\r\n"));
    }

    [Test]
    public void OversizedInitialFallbackRequiresAnEmptyChallenge()
    {
        ScriptedSaslClient client = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Success, new byte[190], true, "alice"));
        ExchangeHarness harness = new ExchangeHarness("+ bm90LWVtcHR5\r\n-ERR cancelled\r\n");

        Assert.Throws<InvalidDataException>(() => harness.Exchange.AuthenticateClient(
            harness.Reader,
            harness.Writer,
            client,
            1024,
            CancellationToken.None));

        Assert.That(client.CancelCount, Is.EqualTo(1));
        Assert.That(harness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [Test]
    public void PreCancelledCallDoesNotInvokeTheMechanismOrWrite()
    {
        ScriptedSaslClient client = OneShotClient("unused", "alice");
        ExchangeHarness harness = new ExchangeHarness("+OK unused\r\n");
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => harness.Exchange.AuthenticateClient(
                harness.Reader,
                harness.Writer,
                client,
                1024,
                cancellation.Token));
        }

        Assert.That(client.BeginCount, Is.Zero);
        Assert.That(harness.Stream.WrittenText(), Is.Empty);
    }

    [Test]
    public void InvalidMechanismStepsAreRejectedBeforeWriting()
    {
        ExchangeHarness missingDataHarness = new ExchangeHarness("+OK unused\r\n");
        Pop3SaslStep missingData = Step(Pop3SaslStepStatus.Continue, Ascii("hidden"), false, null);
        ScriptedSaslClient missingDataClient = new ScriptedSaslClient("TEST", missingData);

        Assert.Throws<InvalidDataException>(() => missingDataHarness.Exchange.AuthenticateClient(
            missingDataHarness.Reader,
            missingDataHarness.Writer,
            missingDataClient,
            1024,
            CancellationToken.None));
        Assert.That(missingDataHarness.Stream.WrittenText(), Is.Empty);

        ExchangeHarness missingResponseHarness = new ExchangeHarness("+ YQ==\r\n-ERR cancelled\r\n");
        ScriptedSaslClient missingResponseClient = new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null),
            Step(Pop3SaslStepStatus.Continue, Array.Empty<byte>(), false, null));

        Assert.Throws<InvalidDataException>(() => missingResponseHarness.Exchange.AuthenticateClient(
            missingResponseHarness.Reader,
            missingResponseHarness.Writer,
            missingResponseClient,
            1024,
            CancellationToken.None));
        Assert.That(missingResponseClient.CancelCount, Is.EqualTo(1));
        Assert.That(missingResponseHarness.Stream.WrittenText(), Is.EqualTo("AUTH TEST\r\n*\r\n"));
    }

    [Test]
    public void ClientAndServerEntryPointsValidateDependenciesBeforeExchangeWork()
    {
        ExchangeHarness harness = new ExchangeHarness("+OK unused\r\n");
        Pop3SaslExchange exchange = harness.Exchange;
        ScriptedSaslClient client = OneShotClient("response", "alice");
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Auth;
        command.Arguments = new[] { "TEST" };
        FakeSaslServerFactory factory = new FakeSaslServerFactory();
        IPop3SaslServerFactory[] factories = new IPop3SaslServerFactory[] { factory };
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = harness.Stream;

        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateClient(
            null!, harness.Writer, client, 1024, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateClient(
            harness.Reader, null!, client, 1024, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateClient(
            harness.Reader, harness.Writer, null!, 1024, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => exchange.AuthenticateClient(
            harness.Reader, harness.Writer, client, 3, CancellationToken.None));

        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateServer(
            null!, harness.Writer, command, factories, connection, 1024,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateServer(
            harness.Reader, null!, command, factories, connection, 1024,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateServer(
            harness.Reader, harness.Writer, null!, factories, connection, 1024,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateServer(
            harness.Reader, harness.Writer, command, null!, connection, 1024,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => exchange.AuthenticateServer(
            harness.Reader, harness.Writer, command, factories, null!, 1024,
            CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => exchange.AuthenticateServer(
            harness.Reader, harness.Writer, command, factories, connection, 3,
            CancellationToken.None));
        Assert.Throws<ArgumentException>(() => exchange.AuthenticateServer(
            harness.Reader,
            harness.Writer,
            command,
            new IPop3SaslServerFactory[] { null! },
            connection,
            1024,
            CancellationToken.None));

        Assert.That(client.BeginCount, Is.Zero);
        Assert.That(factory.CreateSessionCallCount, Is.Zero);
        Assert.That(harness.Stream.WrittenText(), Is.Empty);
    }

    private static ScriptedSaslClient OneShotClient(string response, string identity)
    {
        return new ScriptedSaslClient(
            "TEST",
            Step(Pop3SaslStepStatus.Success, Ascii(response), true, identity));
    }

    private static Pop3SaslStep Step(
        Pop3SaslStepStatus status,
        byte[] data,
        bool hasData,
        string? identity)
    {
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = status;
        step.Data = data;
        step.HasData = hasData;
        step.AuthenticatedIdentity = identity;
        return step;
    }

    private static byte[] Ascii(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    private class ExchangeHarness
    {
        public ScriptedDuplexStream Stream;
        public Pop3WireReader Reader;
        public Pop3WireWriter Writer;
        public Pop3SaslExchange Exchange = new Pop3SaslExchange();

        public ExchangeHarness(string responses)
        {
            Stream = new ScriptedDuplexStream(responses);
            Reader = new Pop3WireReader(Stream, 128);
            Reader.IsFirstResponse = false;
            Writer = new Pop3WireWriter(Stream, 128);
        }
    }

    private class ScriptedSaslClient : IPop3SaslClient
    {
        string Mechanism;
        Queue<Pop3SaslStep> Steps;
        public List<byte[]> Challenges = new List<byte[]>();
        public int BeginCount;
        public int ContinueCount;
        public int CancelCount;

        public ScriptedSaslClient(string mechanism, params Pop3SaslStep[] steps)
        {
            Mechanism = mechanism;
            Steps = new Queue<Pop3SaslStep>(steps);
        }

        public string GetMechanism()
        {
            return Mechanism;
        }

        public Pop3SaslStep BeginAuthentication()
        {
            BeginCount++;
            return Steps.Dequeue();
        }

        public Pop3SaslStep ContinueAuthentication(byte[] challenge)
        {
            ContinueCount++;
            Challenges.Add(challenge);
            return Steps.Dequeue();
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

    private class ScriptedDuplexStream : Stream
    {
        byte[] ReadData;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public ScriptedDuplexStream(string responses)
        {
            ReadData = Encoding.ASCII.GetBytes(responses);
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
            return Encoding.ASCII.GetString(Written.ToArray());
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
