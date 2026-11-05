using whois;

namespace whois.Tests;

public class TcpTransportTests
{
    [Test]
    public void SendsExactEncodedQueryWithOneCrlfOverLoopback()
    {
        using LoopbackWhoisServer server = new LoopbackWhoisServer(Array.Empty<byte[]>(), TimeSpan.Zero, false);
        IWhoisEndpointPolicy policy = new WhoisAllowAllEndpointPolicy();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), new WhoisDnsResolver(), policy, policy, new WhoisTcpConnectionFactory(), WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;
        request.QueryText = "münich.example";
        request.Encoding.RequestEncoding = new System.Text.UTF8Encoding(false, true);

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(server.ReceivedQuery, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("münich.example\r\n")));
        Assert.That(server.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void SendsCompletePayloadWhenTheStreamAcceptsOnlyOneBytePerWrite()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        OneByteWriteStream stream = new OneByteWriteStream();
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { address };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Stream = stream;
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, policy, policy, factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.QueryText = "domain example.test";
        byte[] expected = System.Text.Encoding.ASCII.GetBytes("domain example.test\r\n");

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(stream.GetWrittenBytes(), Is.EqualTo(expected));
        Assert.That(stream.RequestedWriteCounts, Has.Count.EqualTo(expected.Length));
        Assert.That(stream.RequestedWriteCounts, Is.All.EqualTo(1));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void PreservesMultilineBinaryResponseAndUnterminatedFinalLine()
    {
        byte[] expected = new byte[] { 102, 105, 114, 115, 116, 13, 10, 0, 255, 10, 102, 105, 110, 97, 108 };
        using LoopbackWhoisServer server = new LoopbackWhoisServer(new[] { expected }, TimeSpan.Zero, false);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(response.Body, Is.EqualTo(expected));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.Null);
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void EmptyRemoteResponseCompletesAtEof()
    {
        using LoopbackWhoisServer server = new LoopbackWhoisServer(Array.Empty<byte[]>(), TimeSpan.Zero, false);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.Error, Is.Null);
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void ContinuesPastNewlinesThroughFragmentedDelayedChunksUntilEof()
    {
        byte[][] chunks = new byte[][]
        {
            System.Text.Encoding.ASCII.GetBytes("first line\r\n"),
            System.Text.Encoding.ASCII.GetBytes("second line\n"),
            System.Text.Encoding.ASCII.GetBytes("unterminated tail")
        };
        byte[] expected = System.Text.Encoding.ASCII.GetBytes("first line\r\nsecond line\nunterminated tail");
        using LoopbackWhoisServer server = new LoopbackWhoisServer(chunks, TimeSpan.FromMilliseconds(20), false);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Body, Is.EqualTo(expected));
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void ReadsLargeResponseAcrossMultipleNetworkReads()
    {
        byte[] expected = new byte[131089];
        int index = 0;
        while (index < expected.Length)
        {
            expected[index] = (byte)(index % 251);
            index = index + 1;
        }
        using LoopbackWhoisServer server = new LoopbackWhoisServer(new[] { expected }, TimeSpan.Zero, false);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;
        request.Limits.MaximumResponseBytes = expected.Length + 1;

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Body, Is.EqualTo(expected));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void ConnectionResetReturnsBytesReadBeforeTheFailure()
    {
        byte[] partial = System.Text.Encoding.ASCII.GetBytes("partial response\r\n");
        byte[][] chunks = new byte[][] { partial, Array.Empty<byte>() };
        using LoopbackWhoisServer server = new LoopbackWhoisServer(chunks, TimeSpan.FromMilliseconds(100), true);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(partial));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.Not.Null);
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void ExchangesThroughAllowedAddressesInDeterministicOrderUntilEof()
    {
        System.Net.IPAddress denied = System.Net.IPAddress.Parse("192.0.2.1");
        System.Net.IPAddress failed = System.Net.IPAddress.Parse("2001:db8::1");
        System.Net.IPAddress connected = System.Net.IPAddress.Parse("192.0.2.2");
        FixedResolver resolver = new FixedResolver(new[] { denied, failed, connected });
        SelectivePolicy automaticPolicy = new SelectivePolicy(denied);
        ScriptedStream stream = new ScriptedStream(System.Text.Encoding.ASCII.GetBytes("alpha\r\nbeta"), 2, false);
        RecordingConnectionFactory factory = new RecordingConnectionFactory(connected, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new ThrowingPolicy(), automaticPolicy, factory, WhoisAddressPreference.IPv6First);
        WhoisRequest request = CreateRequest();
        request.Source = WhoisEndpointSource.Referral;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(resolver.Preference, Is.EqualTo(WhoisAddressPreference.IPv6First));
        Assert.That(automaticPolicy.Seen, Is.EqualTo(new[] { denied, failed, connected }));
        Assert.That(factory.Attempts, Is.EqualTo(new[] { failed, connected }));
        Assert.That(stream.GetWrittenBytes(), Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(response.Body, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("alpha\r\nbeta")));
        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(response.ApprovedAddress, Is.SameAs(connected));
        Assert.That(response.Request, Is.SameAs(request));
        Assert.That(response.StartedAt, Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.Null);
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void RetainsConfiguredPrefixWhenResponseLimitIsExceeded()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        ScriptedStream stream = new ScriptedStream(new byte[] { 1, 2, 3, 4, 5 }, 5, false);
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new SelectivePolicy(null), factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Limits.MaximumResponseBytes = 4;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ResponseLimitReached));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(response.IsTruncated, Is.True);
        Assert.That(response.Error, Is.Not.Null);
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void ExactResponseLimitCompletesWithoutTruncation()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        byte[] exact = new byte[] { 1, 2, 3, 4 };
        ScriptedStream stream = new ScriptedStream(exact, 2, false);
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Limits.MaximumResponseBytes = exact.Length;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(response.Body, Is.EqualTo(exact));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.Null);
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void ReadIdleDeadlineReturnsTimedOutPartialResponse()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        byte[] partial = new byte[] { 1, 2, 3 };
        ScriptedStream stream = new ScriptedStream(partial, partial.Length, false);
        stream.BlockAfterResponse = true;
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new SelectivePolicy(null), factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Timeouts.ReadIdle = TimeSpan.FromMilliseconds(20);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(partial));
        Assert.That(response.Error, Is.TypeOf<TimeoutException>());
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void ResolveDeadlineReturnsTimedOutResolveMetadata()
    {
        WaitingResolver resolver = new WaitingResolver();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new ThrowingPolicy(), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Timeouts.Resolve = TimeSpan.FromMilliseconds(20);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.Null);
        Assert.That(response.Error, Is.Not.Null);
    }

    [Test]
    public void ConnectDeadlineReturnsTimedOutConnectMetadata()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { address };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.OpenHandler = (endpoint, approvedAddress, timeouts, cancellationToken) =>
        {
            cancellationToken.WaitHandle.WaitOne();
            WhoisConnection connection = new WhoisConnection();
            connection.ApprovedAddress = approvedAddress;
            connection.Status = WhoisExchangeStatus.Cancelled;
            connection.Error = new OperationCanceledException(cancellationToken);
            return connection;
        };
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, policy, policy, factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Timeouts.Connect = TimeSpan.FromMilliseconds(20);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.Not.Null);
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { address }));
    }

    [Test]
    public void WriteDeadlineReturnsTimedOutWriteMetadataAndDisposesStream()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        ScriptedStream stream = new ScriptedStream(Array.Empty<byte>(), 1, false);
        stream.BlockWrites = true;
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Timeouts.Write = TimeSpan.FromMilliseconds(20);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Write));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.TypeOf<TimeoutException>());
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void OverallDeadlineIsNotExtendedBySuccessfulReadIdleResets()
    {
        byte[][] chunks = new byte[][]
        {
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4 }
        };
        using LoopbackWhoisServer server = new LoopbackWhoisServer(chunks, TimeSpan.FromMilliseconds(50), false);
        IWhoisTransport transport = CreateLoopbackTransport();
        WhoisRequest request = CreateRequest();
        request.Endpoint = server.Endpoint;
        request.Timeouts.Resolve = TimeSpan.FromMilliseconds(80);
        request.Timeouts.Connect = TimeSpan.FromMilliseconds(80);
        request.Timeouts.Write = TimeSpan.FromMilliseconds(80);
        request.Timeouts.ReadIdle = TimeSpan.FromMilliseconds(80);
        request.Timeouts.Overall = TimeSpan.FromMilliseconds(120);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.Not.Empty);
        Assert.That(response.Body.Length, Is.LessThan(4));
        Assert.That(response.Error, Is.TypeOf<TimeoutException>());
    }

    [Test]
    public void CallerCancellationDuringReadReturnsPartialBytesAndDisposesStream()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        byte[] partial = new byte[] { 5, 6, 7 };
        ScriptedStream stream = new ScriptedStream(partial, partial.Length, false);
        stream.CancelAtEnd = cancellation;
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.CancellationToken = cancellation.Token;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(partial));
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.TypeOf<OperationCanceledException>());
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void OverallDeadlineCanExpireDuringResolution()
    {
        WaitingResolver resolver = new WaitingResolver();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new ThrowingPolicy(), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.Timeouts.Resolve = TimeSpan.FromMilliseconds(20);
        request.Timeouts.Connect = TimeSpan.FromMilliseconds(20);
        request.Timeouts.Write = TimeSpan.FromMilliseconds(20);
        request.Timeouts.ReadIdle = TimeSpan.FromMilliseconds(20);
        request.Timeouts.Overall = TimeSpan.FromMilliseconds(20);

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(response.ApprovedAddress, Is.Null);
        Assert.That(response.Error, Is.Not.Null);
    }

    [Test]
    public void PreCancelledRequestStopsBeforeResolution()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FixedResolver resolver = new FixedResolver(new[] { System.Net.IPAddress.Loopback });
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new ThrowingPolicy(), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        cancellation.Cancel();
        request.CancellationToken = cancellation.Token;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(response.Error, Is.TypeOf<OperationCanceledException>());
        Assert.That(resolver.Calls, Is.Zero);
    }

    [Test]
    public void ResolutionFailureIsReturnedWithItsStageAndError()
    {
        IOException error = new IOException("DNS failed.");
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), new FailedResolver(error), new ThrowingPolicy(), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);

        WhoisRawResponse response = transport.Exchange(CreateRequest());

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.Null);
        Assert.That(response.Error, Is.SameAs(error));
    }

    [Test]
    public void ConnectionFailureRetainsTheLastApprovedAddressAndError()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        IOException error = new IOException("Connection refused.");
        FixedResolver resolver = new FixedResolver(new[] { address });
        FailedConnectionFactory factory = new FailedConnectionFactory(error, null);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);

        WhoisRawResponse response = transport.Exchange(CreateRequest());

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.SameAs(error));
    }

    [Test]
    public void AllAddressFailuresAreAggregatedInAttemptOrder()
    {
        System.Net.IPAddress first = System.Net.IPAddress.Parse("192.0.2.1");
        System.Net.IPAddress second = System.Net.IPAddress.Parse("2001:db8::1");
        System.Net.IPAddress third = System.Net.IPAddress.Parse("192.0.2.2");
        Exception[] errors = new Exception[]
        {
            new IOException("First connection failed."),
            new IOException("Second connection failed."),
            new IOException("Third connection failed.")
        };
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { first, second, third };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.OpenHandler = (endpoint, approvedAddress, timeouts, cancellationToken) =>
        {
            int attemptIndex = factory.ApprovedAddresses.Count - 1;
            WhoisConnection connection = new WhoisConnection();
            connection.ApprovedAddress = approvedAddress;
            connection.Status = WhoisExchangeStatus.ConnectFailed;
            connection.Error = errors[attemptIndex];
            return connection;
        };
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, policy, policy, factory, WhoisAddressPreference.System);

        WhoisRawResponse response = transport.Exchange(CreateRequest());

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(response.ApprovedAddress, Is.SameAs(third));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { first, second, third }));
        Assert.That(response.Error, Is.TypeOf<AggregateException>());
        AggregateException aggregate = (AggregateException)response.Error!;
        Assert.That(aggregate.InnerExceptions, Is.EqualTo(errors));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.IsTruncated, Is.False);
    }

    [Test]
    public void WriteFailureIsReturnedAndDisposesTheSelectedStream()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        IOException error = new IOException("Write failed.");
        ScriptedStream stream = new ScriptedStream(System.Array.Empty<byte>(), 1, false);
        stream.WriteError = error;
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);

        WhoisRawResponse response = transport.Exchange(CreateRequest());

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.WriteFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Write));
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.SameAs(error));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void ReadFailureReturnsBytesReceivedBeforeTheError()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        IOException error = new IOException("Connection reset.");
        ScriptedStream stream = new ScriptedStream(new byte[] { 1, 2, 3 }, 3, false);
        stream.ReadError = error;
        FixedResolver resolver = new FixedResolver(new[] { address });
        RecordingConnectionFactory factory = new RecordingConnectionFactory(address, stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);

        WhoisRawResponse response = transport.Exchange(CreateRequest());

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.SameAs(error));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void CancellationDuringARejectedPolicyDecisionIsReturned()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FixedResolver resolver = new FixedResolver(new[] { System.Net.IPAddress.Parse("192.0.2.1") });
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new CancellingPolicy(cancellation), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);
        WhoisRequest request = CreateRequest();
        request.CancellationToken = cancellation.Token;

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(response.ApprovedAddress, Is.Null);
        Assert.That(response.Error, Is.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void SuccessfulResolutionWithoutAddressesIsRejectedAsAnInvariantFailure()
    {
        FixedResolver resolver = new FixedResolver(System.Array.Empty<System.Net.IPAddress>());
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new ThrowingPolicy(), new ThrowingPolicy(), new ThrowingConnectionFactory(), WhoisAddressPreference.System);

        Assert.That(() => transport.Exchange(CreateRequest()), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void FailedConnectionReturningAStreamIsRejectedAndDisposed()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.1");
        ScriptedStream stream = new ScriptedStream(System.Array.Empty<byte>(), 1, false);
        FixedResolver resolver = new FixedResolver(new[] { address });
        FailedConnectionFactory factory = new FailedConnectionFactory(new IOException("Connection failed."), stream);
        IWhoisTransport transport = new WhoisTcpTransport(new WhoisQueryCodec(), resolver, new SelectivePolicy(null), new ThrowingPolicy(), factory, WhoisAddressPreference.System);

        Assert.That(() => transport.Exchange(CreateRequest()), Throws.TypeOf<InvalidOperationException>());
        Assert.That(stream.Disposed, Is.True);
    }

    private static WhoisRequest CreateRequest()
    {
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.example";
        request.QueryText = "example.com";
        return request;
    }

    private static IWhoisTransport CreateLoopbackTransport()
    {
        IWhoisEndpointPolicy policy = new WhoisAllowAllEndpointPolicy();
        return new WhoisTcpTransport(new WhoisQueryCodec(), new WhoisDnsResolver(), policy, policy, new WhoisTcpConnectionFactory(), WhoisAddressPreference.System);
    }

    private class FixedResolver : IWhoisHostResolver
    {
        private System.Net.IPAddress[] addresses;
        public int Calls;
        public WhoisAddressPreference Preference;

        public FixedResolver(System.Net.IPAddress[] suppliedAddresses)
        {
            addresses = suppliedAddresses;
        }

        public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, CancellationToken cancellationToken)
        {
            Calls = Calls + 1;
            Preference = preference;
            WhoisResolution result = new WhoisResolution();
            result.Addresses = addresses;
            result.Status = WhoisExchangeStatus.Complete;
            return result;
        }
    }

    private class WaitingResolver : IWhoisHostResolver
    {
        public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, CancellationToken cancellationToken)
        {
            cancellationToken.WaitHandle.WaitOne();
            WhoisResolution result = new WhoisResolution();
            result.Status = WhoisExchangeStatus.Cancelled;
            result.Error = new OperationCanceledException(cancellationToken);
            return result;
        }
    }

    private class FailedResolver : IWhoisHostResolver
    {
        private Exception error;

        public FailedResolver(Exception failure)
        {
            error = failure;
        }

        public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, CancellationToken cancellationToken)
        {
            WhoisResolution result = new WhoisResolution();
            result.Status = WhoisExchangeStatus.ResolveFailed;
            result.Error = error;
            return result;
        }
    }

    private class SelectivePolicy : IWhoisEndpointPolicy
    {
        private System.Net.IPAddress? denied;
        public List<System.Net.IPAddress> Seen = new List<System.Net.IPAddress>();

        public SelectivePolicy(System.Net.IPAddress? deniedAddress)
        {
            denied = deniedAddress;
        }

        public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
        {
            Seen.Add(address);
            return denied == null || !denied.Equals(address);
        }
    }

    private class ThrowingPolicy : IWhoisEndpointPolicy
    {
        public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
        {
            throw new InvalidOperationException("Unexpected endpoint policy call.");
        }
    }

    private class CancellingPolicy : IWhoisEndpointPolicy
    {
        private CancellationTokenSource cancellation;

        public CancellingPolicy(CancellationTokenSource cancellationSource)
        {
            cancellation = cancellationSource;
        }

        public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
        {
            cancellation.Cancel();
            return false;
        }
    }

    private class RecordingConnectionFactory : IWhoisConnectionFactory
    {
        private System.Net.IPAddress successfulAddress;
        private Stream successfulStream;
        public List<System.Net.IPAddress> Attempts = new List<System.Net.IPAddress>();

        public RecordingConnectionFactory(System.Net.IPAddress address, Stream stream)
        {
            successfulAddress = address;
            successfulStream = stream;
        }

        public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, CancellationToken cancellationToken)
        {
            Attempts.Add(approvedAddress);
            WhoisConnection result = new WhoisConnection();
            result.ApprovedAddress = approvedAddress;
            if (approvedAddress.Equals(successfulAddress))
            {
                result.Stream = successfulStream;
                result.Status = WhoisExchangeStatus.Complete;
            }
            else
            {
                result.Status = WhoisExchangeStatus.ConnectFailed;
                result.Error = new IOException("Connection refused.");
            }
            return result;
        }
    }

    private class ThrowingConnectionFactory : IWhoisConnectionFactory
    {
        public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Unexpected connection factory call.");
        }
    }

    private class FailedConnectionFactory : IWhoisConnectionFactory
    {
        private Exception error;
        private Stream? stream;

        public FailedConnectionFactory(Exception failure, Stream? returnedStream)
        {
            error = failure;
            stream = returnedStream;
        }

        public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, CancellationToken cancellationToken)
        {
            WhoisConnection result = new WhoisConnection();
            result.ApprovedAddress = approvedAddress;
            result.Stream = stream;
            result.Status = WhoisExchangeStatus.ConnectFailed;
            result.Error = error;
            return result;
        }
    }

    private class ScriptedStream : Stream
    {
        private byte[] response;
        private int responseOffset;
        private int maximumRead;
        private bool blockReads;
        private MemoryStream written = new MemoryStream();
        private TaskCompletionSource<int> blockedRead = new TaskCompletionSource<int>();
        private TaskCompletionSource blockedWrite = new TaskCompletionSource();
        public bool Disposed;
        public Exception? WriteError;
        public Exception? ReadError;
        public bool BlockAfterResponse;
        public bool BlockWrites;
        public CancellationTokenSource? CancelAtEnd;

        public ScriptedStream(byte[] responseBytes, int readSize, bool shouldBlockReads)
        {
            response = responseBytes;
            maximumRead = readSize;
            blockReads = shouldBlockReads;
        }

        public byte[] GetWrittenBytes()
        {
            return written.ToArray();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = response.Length - responseOffset;
            if (remaining == 0)
            {
                return 0;
            }
            int readCount = Math.Min(count, Math.Min(maximumRead, remaining));
            Array.Copy(response, responseOffset, buffer, offset, readCount);
            responseOffset = responseOffset + readCount;
            return readCount;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (blockReads)
            {
                return blockedRead.Task;
            }
            if (responseOffset == response.Length && CancelAtEnd != null)
            {
                CancelAtEnd.Cancel();
                return blockedRead.Task;
            }
            if (responseOffset == response.Length && BlockAfterResponse)
            {
                return blockedRead.Task;
            }
            if (ReadError != null && responseOffset == response.Length)
            {
                return Task.FromException<int>(ReadError);
            }
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            written.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (BlockWrites)
            {
                return blockedWrite.Task;
            }
            if (WriteError != null)
            {
                return Task.FromException(WriteError);
            }
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class OneByteWriteStream : Stream
    {
        private MemoryStream written = new MemoryStream();
        public List<int> RequestedWriteCounts = new List<int>();
        public bool Disposed;

        public byte[] GetWrittenBytes()
        {
            return written.ToArray();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            RequestedWriteCounts.Add(count);
            int accepted = Math.Min(1, count);
            written.Write(buffer, offset, accepted);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
