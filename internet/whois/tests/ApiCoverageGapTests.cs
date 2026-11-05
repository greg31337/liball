using whois;

namespace whois.Tests;

public class ApiCoverageGapTests
{
    [Test]
    public void SelectorRejectsInvalidClassificationsAndNonWhoisOverrides()
    {
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        WhoisQuery invalidQuery = new WhoisQuery();
        invalidQuery.OriginalText = "example.com";
        invalidQuery.NormalizedText = "example.com";
        invalidQuery.Kind = WhoisQueryKind.Domain;
        invalidQuery.IsValid = false;
        WhoisLookupRequest automaticRequest = new WhoisLookupRequest();
        automaticRequest.Text = "example.com";
        WhoisQueryException schemeError;
        WhoisLookupRequest overrideRequest = new WhoisLookupRequest();
        overrideRequest.Text = "example.com";
        overrideRequest.EndpointOverride = new WhoisEndpoint();
        overrideRequest.EndpointOverride.Host = "web.example";
        overrideRequest.EndpointOverride.Port = 443;
        overrideRequest.EndpointOverride.Scheme = WhoisReferralScheme.Https;

        WhoisServerNotFoundException noServer = Assert.Throws<WhoisServerNotFoundException>(() =>
            selector.SelectServer(invalidQuery, automaticRequest))!;
        schemeError = Assert.Throws<WhoisQueryException>(() =>
            selector.SelectServer(invalidQuery, overrideRequest))!;

        Assert.That(noServer.Query, Is.SameAs(invalidQuery));
        Assert.That(schemeError.InvalidParameter, Is.EqualTo("Endpoint.Scheme"));
        Assert.That(schemeError.Endpoint, Is.SameAs(overrideRequest.EndpointOverride));
    }

    [Test]
    public void ResolverExercisesThePublicDnsHostnamePathWithoutExternalNetwork()
    {
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "localhost";

        WhoisResolution result = resolver.ResolveHost(
            endpoint,
            WhoisAddressPreference.IPv6First,
            default(CancellationToken));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(result.Addresses, Is.Not.Empty);
        Assert.That(result.Addresses, Has.All.Matches<System.Net.IPAddress>((address) => System.Net.IPAddress.IsLoopback(address)));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void DecoderRejectsInvalidMutableInputsAndDecodesBothUtf32BomOrders()
    {
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        WhoisRawResponse nullBody = new WhoisRawResponse();
        nullBody.Body = null!;
        WhoisEncodingOptions validOptions = new WhoisEncodingOptions();
        WhoisEncodingOptions invalidOptions = new WhoisEncodingOptions();
        invalidOptions.DecodingMode = (WhoisDecodingMode)99;
        WhoisRawResponse ordinary = new WhoisRawResponse();
        System.Text.Encoding bigEndian = new System.Text.UTF32Encoding(true, true, true);
        System.Text.Encoding littleEndian = new System.Text.UTF32Encoding(false, true, true);
        WhoisRawResponse bigEndianRaw = new WhoisRawResponse();
        WhoisRawResponse littleEndianRaw = new WhoisRawResponse();
        bigEndianRaw.Body = Join(bigEndian.GetPreamble(), bigEndian.GetBytes("BE"));
        littleEndianRaw.Body = Join(littleEndian.GetPreamble(), littleEndian.GetBytes("LE"));

        Assert.Throws<InvalidOperationException>(() => decoder.DecodeResponse(nullBody, validOptions));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeResponse(ordinary, invalidOptions));
        WhoisTextResponse bigEndianText = decoder.DecodeResponse(bigEndianRaw, validOptions);
        WhoisTextResponse littleEndianText = decoder.DecodeResponse(littleEndianRaw, validOptions);

        Assert.That(bigEndianText.Text, Is.EqualTo("BE"));
        Assert.That(bigEndianText.ByteOrderMarkDetected, Is.True);
        Assert.That(bigEndianText.IsDecoded, Is.True);
        Assert.That(littleEndianText.Text, Is.EqualTo("LE"));
        Assert.That(littleEndianText.ByteOrderMarkDetected, Is.True);
        Assert.That(littleEndianText.IsDecoded, Is.True);
    }

    [Test]
    public void ParserRejectsNullTextAndSuppliesMissingUndecodedErrorContext()
    {
        IWhoisResponseParser parser = new WhoisResponseParser();
        WhoisTextResponse nullText = new WhoisTextResponse();
        nullText.Text = null!;
        WhoisTextResponse undecoded = new WhoisTextResponse();

        Assert.Throws<InvalidOperationException>(() => parser.ParseResponse(nullText, new WhoisLimits()));
        WhoisParsedResponse result = parser.ParseResponse(undecoded, new WhoisLimits());

        Assert.That(result.IsParsed, Is.False);
        Assert.That(result.TextResponse, Is.SameAs(undecoded));
        Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(result.Error!.Message, Is.EqualTo("The response must be decoded before parsing."));
    }

    [Test]
    public void AutomaticClassifierFallsBackForNonDomainPunctuation()
    {
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "name_with_underscore";

        WhoisQuery result = classifier.Classify(request);

        Assert.That(result.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
        Assert.That(result.OriginalText, Is.EqualTo("name_with_underscore"));
        Assert.That(result.NormalizedText, Is.EqualTo("name_with_underscore"));
    }

    [Test]
    public void LookupThrowingApiMapsMaximumHopFailureReason()
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (request) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Status = WhoisExchangeStatus.Complete;
            if (request.Endpoint.Host == WhoisDefaults.IanaHost)
            {
                response.Body = System.Text.Encoding.UTF8.GetBytes("refer: first.example\r\n");
            }
            else
            {
                response.Body = System.Text.Encoding.UTF8.GetBytes("refer: second.example\r\n");
            }
            return response;
        };
        WhoisClient client = CreateClient(
            new IanaWhoisServerSelector(),
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        request.Options.Limits.MaximumReferralHops = 1;

        WhoisReferralException exception = Assert.Throws<WhoisReferralException>(() => client.Lookup(request))!;

        Assert.That(exception.Reason, Is.EqualTo("HopLimit"));
        Assert.That(exception.Referral, Is.Not.Null);
        Assert.That(exception.Referral!.Endpoint!.Host, Is.EqualTo("second.example"));
        Assert.That(exception.PartialResult.Hops, Has.Length.EqualTo(2));
    }

    [Test]
    public void ClientRejectsNullResultsFromEveryCustomLookupHook()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        FakeServerSelector nullSelector = new FakeServerSelector();
        nullSelector.Result = null!;
        FakeServerSelector nullEndpointSelector = new FakeServerSelector();
        nullEndpointSelector.Result.Endpoint = null!;
        FakeTransport nullTransport = new FakeTransport();
        nullTransport.ExchangeHandler = (exchangeRequest) => null!;
        FakeTransport completeTransport = CreateCompleteTransport();

        Assert.That(() => CreateClient(nullSelector, completeTransport, new WhoisResponseDecoder(), new WhoisResponseParser(), new WhoisReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The server selector returned no endpoint selection."));
        Assert.That(() => CreateClient(nullEndpointSelector, completeTransport, new WhoisResponseDecoder(), new WhoisResponseParser(), new WhoisReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The server selector returned no endpoint selection."));
        Assert.That(() => CreateClient(new IanaWhoisServerSelector(), nullTransport, new WhoisResponseDecoder(), new WhoisResponseParser(), new WhoisReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The WHOIS transport returned no response."));
        Assert.That(() => CreateClient(new IanaWhoisServerSelector(), completeTransport, new NullDecoder(), new WhoisResponseParser(), new WhoisReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The response decoder returned no result."));
        Assert.That(() => CreateClient(new IanaWhoisServerSelector(), completeTransport, new SuccessfulDecoder(), new NullParser(), new WhoisReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The response parser returned no result."));
        Assert.That(() => CreateClient(new IanaWhoisServerSelector(), completeTransport, new SuccessfulDecoder(), new SuccessfulParser(), new NullReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The referral parser returned no result."));
        Assert.That(() => CreateClient(new IanaWhoisServerSelector(), completeTransport, new SuccessfulDecoder(), new SuccessfulParser(), new NullElementReferralParser()).TryLookup(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The referral parser returned a null referral."));
    }

    [Test]
    public void ClientSuppliesErrorsWhenCustomFailureResultsOmitThem()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        FakeTransport failedTransport = new FakeTransport();
        failedTransport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = exchangeRequest;
            response.Status = WhoisExchangeStatus.ReadFailed;
            response.Stage = WhoisOperationStage.Read;
            return response;
        };

        WhoisLookupResult transportResult = CreateClient(
            new IanaWhoisServerSelector(),
            failedTransport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser()).TryLookup(request);
        WhoisLookupResult decodeResult = CreateClient(
            new IanaWhoisServerSelector(),
            CreateCompleteTransport(),
            new FailedDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser()).TryLookup(request);
        WhoisLookupResult parseResult = CreateClient(
            new IanaWhoisServerSelector(),
            CreateCompleteTransport(),
            new SuccessfulDecoder(),
            new FailedParser(),
            new WhoisReferralParser()).TryLookup(request);

        Assert.That(transportResult.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(transportResult.Error!.Message, Is.EqualTo("The WHOIS exchange failed without error details."));
        Assert.That(decodeResult.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(decodeResult.Error!.Message, Is.EqualTo("WHOIS response decoding failed without error details."));
        Assert.That(parseResult.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(parseResult.Error!.Message, Is.EqualTo("WHOIS response parsing failed without error details."));
    }

    [Test]
    public void TransportRejectsInvalidResolverAndConnectionFactoryResults()
    {
        WhoisRequest request = CreateExchangeRequest();
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        FakeConnectionFactory unusedFactory = new FakeConnectionFactory();
        FakeHostResolver nullResolver = new FakeHostResolver();
        nullResolver.ResolveHandler = (endpoint, preference, cancellationToken) => null!;
        FakeHostResolver invalidStatusResolver = CreateResolver();
        invalidStatusResolver.Result.Status = WhoisExchangeStatus.ReadFailed;
        FakeHostResolver nullAddressesResolver = CreateResolver();
        nullAddressesResolver.Result.Addresses = null!;
        FakeHostResolver successfulWithErrorResolver = CreateResolver();
        successfulWithErrorResolver.Result.Error = new IOException("unexpected success error");
        FakeHostResolver nullCandidateResolver = CreateResolver();
        nullCandidateResolver.Result.Addresses = new System.Net.IPAddress[] { null! };
        FakeHostResolver validResolver = CreateResolver();
        FakeConnectionFactory nullFactory = new FakeConnectionFactory();
        nullFactory.OpenHandler = (endpoint, address, timeouts, cancellationToken) => null!;
        FakeConnectionFactory invalidStatusFactory = new FakeConnectionFactory();
        invalidStatusFactory.Status = WhoisExchangeStatus.ReadFailed;
        FakeConnectionFactory wrongAddressFactory = new FakeConnectionFactory();
        wrongAddressFactory.OpenHandler = (endpoint, address, timeouts, cancellationToken) =>
        {
            WhoisConnection connection = new WhoisConnection();
            connection.ApprovedAddress = System.Net.IPAddress.Parse("203.0.113.99");
            connection.Status = WhoisExchangeStatus.ConnectFailed;
            return connection;
        };
        FakeConnectionFactory missingStreamFactory = new FakeConnectionFactory();
        FakeConnectionFactory successfulWithErrorFactory = new FakeConnectionFactory();
        successfulWithErrorFactory.Stream = new MemoryStream();
        successfulWithErrorFactory.Error = new IOException("unexpected connection success error");

        AssertInvariant(nullResolver, policy, unusedFactory, request, "The host resolver returned no result.");
        AssertInvariant(invalidStatusResolver, policy, unusedFactory, request, "The host resolver returned an invalid status.");
        AssertInvariant(nullAddressesResolver, policy, unusedFactory, request, "The host resolver returned a null address array.");
        AssertInvariant(successfulWithErrorResolver, policy, unusedFactory, request, "The host resolver returned an error for a successful result.");
        AssertInvariant(nullCandidateResolver, policy, unusedFactory, request, "The host resolver returned a null address candidate.");
        AssertInvariant(validResolver, policy, nullFactory, request, "The connection factory returned no result.");
        AssertInvariant(validResolver, policy, invalidStatusFactory, request, "The connection factory returned an invalid status.");
        AssertInvariant(validResolver, policy, wrongAddressFactory, request, "The connection factory returned a result for a different approved address.");
        AssertInvariant(validResolver, policy, missingStreamFactory, request, "The connection factory returned an invalid successful connection.");
        AssertInvariant(validResolver, policy, successfulWithErrorFactory, request, "The connection factory returned an invalid successful connection.");
    }

    [Test]
    public void TransportSuppliesMissingResolutionAndConnectionErrors()
    {
        WhoisRequest request = CreateExchangeRequest();
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        FakeConnectionFactory unusedFactory = new FakeConnectionFactory();
        FakeHostResolver timedOutResolver = new FakeHostResolver();
        timedOutResolver.Result.Status = WhoisExchangeStatus.TimedOut;
        WhoisRawResponse timedOutResolution = CreateTransport(timedOutResolver, policy, unusedFactory).Exchange(request);
        FakeHostResolver validResolver = CreateResolver();
        FakeConnectionFactory failedFactory = new FakeConnectionFactory();
        failedFactory.Status = WhoisExchangeStatus.ConnectFailed;
        WhoisRawResponse failedConnection = CreateTransport(validResolver, policy, failedFactory).Exchange(request);
        FakeConnectionFactory timedOutFactory = new FakeConnectionFactory();
        timedOutFactory.Status = WhoisExchangeStatus.TimedOut;
        WhoisRawResponse timedOutConnection = CreateTransport(validResolver, policy, timedOutFactory).Exchange(request);

        Assert.That(timedOutResolution.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(timedOutResolution.Error, Is.TypeOf<TimeoutException>());
        Assert.That(failedConnection.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(failedConnection.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(timedOutConnection.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(timedOutConnection.Error, Is.TypeOf<TimeoutException>());
    }

    [Test]
    public void DirectClientAndTransportRejectInvalidCustomOutcomes()
    {
        WhoisRequest directRequest = CreateExchangeRequest();
        FakeTransport nullTransport = new FakeTransport();
        nullTransport.ExchangeHandler = (request) => null!;
        WhoisClient nullClient = CreateClient(
            new IanaWhoisServerSelector(),
            nullTransport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        FakeTransport unsupportedTransport = new FakeTransport();
        unsupportedTransport.ExchangeHandler = (request) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Status = (WhoisExchangeStatus)99;
            return response;
        };
        WhoisClient unsupportedClient = CreateClient(
            new IanaWhoisServerSelector(),
            unsupportedTransport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        FakeHostResolver resolver = CreateResolver();
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        FakeConnectionFactory factory = new FakeConnectionFactory();
        directRequest.Source = (WhoisEndpointSource)99;

        Assert.That(() => nullClient.Query(directRequest),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The WHOIS transport returned no response."));
        Assert.That(() => unsupportedClient.Query(directRequest),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("The WHOIS transport returned an unsupported status."));
        Assert.That(() => CreateTransport(resolver, policy, factory).Exchange(directRequest),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("request.Source"));
    }

    [Test]
    public void TransportObservesCancellationTriggeredByEachCustomNetworkHook()
    {
        using CancellationTokenSource resolverCancellation = new CancellationTokenSource();
        WhoisRequest resolverRequest = CreateExchangeRequest();
        resolverRequest.CancellationToken = resolverCancellation.Token;
        FakeHostResolver cancellingResolver = CreateResolver();
        cancellingResolver.ResolveHandler = (endpoint, preference, cancellationToken) =>
        {
            resolverCancellation.Cancel();
            return cancellingResolver.Result;
        };
        WhoisRawResponse resolverResult = CreateTransport(
            cancellingResolver,
            new FakeEndpointPolicy(),
            new FakeConnectionFactory()).Exchange(resolverRequest);

        using CancellationTokenSource policyCancellation = new CancellationTokenSource();
        WhoisRequest policyRequest = CreateExchangeRequest();
        policyRequest.CancellationToken = policyCancellation.Token;
        FakeEndpointPolicy cancellingPolicy = new FakeEndpointPolicy();
        cancellingPolicy.AllowHandler = (endpoint, address, source) =>
        {
            policyCancellation.Cancel();
            return true;
        };
        WhoisRawResponse policyResult = CreateTransport(
            CreateResolver(),
            cancellingPolicy,
            new FakeConnectionFactory()).Exchange(policyRequest);

        using CancellationTokenSource factoryCancellation = new CancellationTokenSource();
        WhoisRequest factoryRequest = CreateExchangeRequest();
        factoryRequest.CancellationToken = factoryCancellation.Token;
        TrackingStream stream = new TrackingStream();
        FakeConnectionFactory cancellingFactory = new FakeConnectionFactory();
        cancellingFactory.OpenHandler = (endpoint, address, timeouts, cancellationToken) =>
        {
            factoryCancellation.Cancel();
            WhoisConnection connection = new WhoisConnection();
            connection.ApprovedAddress = address;
            connection.Stream = stream;
            connection.Status = WhoisExchangeStatus.Complete;
            return connection;
        };
        WhoisRawResponse factoryResult = CreateTransport(
            CreateResolver(),
            new FakeEndpointPolicy(),
            cancellingFactory).Exchange(factoryRequest);

        Assert.That(resolverResult.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(resolverResult.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(policyResult.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(policyResult.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(factoryResult.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(factoryResult.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(stream.Disposed, Is.True);
    }

    [Test]
    public void ReferralParserIgnoresDirectiveNamesShorterThanReferral()
    {
        WhoisRawResponse raw = new WhoisRawResponse();
        raw.Body = System.Text.Encoding.UTF8.GetBytes("%ref\r\n");
        raw.Status = WhoisExchangeStatus.Complete;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();
        IWhoisReferralParser referralParser = new WhoisReferralParser();

        WhoisTextResponse text = decoder.DecodeResponse(raw, new WhoisEncodingOptions());
        WhoisParsedResponse parsed = parser.ParseResponse(text, new WhoisLimits());
        WhoisReferral[] referrals = referralParser.ParseReferrals(parsed);

        Assert.That(parsed.Lines, Has.Length.EqualTo(1));
        Assert.That(parsed.Lines[0].Kind, Is.EqualTo(WhoisLineKind.Comment));
        Assert.That(referrals, Is.Empty);
    }

    private static byte[] Join(byte[] prefix, byte[] body)
    {
        byte[] result = new byte[prefix.Length + body.Length];
        Array.Copy(prefix, 0, result, 0, prefix.Length);
        Array.Copy(body, 0, result, prefix.Length, body.Length);
        return result;
    }

    private static FakeTransport CreateCompleteTransport()
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (request) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Status = WhoisExchangeStatus.Complete;
            return response;
        };
        return transport;
    }

    private static WhoisClient CreateClient(
        IWhoisServerSelector selector,
        IWhoisTransport transport,
        IWhoisResponseDecoder decoder,
        IWhoisResponseParser parser,
        IWhoisReferralParser referralParser)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            decoder,
            parser,
            referralParser);
    }

    private static WhoisRequest CreateExchangeRequest()
    {
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.coverage.example";
        request.QueryText = "example.com";
        request.Source = WhoisEndpointSource.Referral;
        return request;
    }

    private static FakeHostResolver CreateResolver()
    {
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { System.Net.IPAddress.Parse("203.0.113.66") };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        return resolver;
    }

    private static WhoisTcpTransport CreateTransport(
        IWhoisHostResolver resolver,
        IWhoisEndpointPolicy policy,
        IWhoisConnectionFactory factory)
    {
        return new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            policy,
            policy,
            factory,
            WhoisAddressPreference.System);
    }

    private static void AssertInvariant(
        IWhoisHostResolver resolver,
        IWhoisEndpointPolicy policy,
        IWhoisConnectionFactory factory,
        WhoisRequest request,
        string message)
    {
        Assert.That(() => CreateTransport(resolver, policy, factory).Exchange(request),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(message));
    }

    private class NullDecoder : IWhoisResponseDecoder
    {
        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            return null!;
        }
    }

    private class SuccessfulDecoder : IWhoisResponseDecoder
    {
        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            WhoisTextResponse result = new WhoisTextResponse();
            result.RawResponse = response;
            result.IsDecoded = true;
            return result;
        }
    }

    private class FailedDecoder : IWhoisResponseDecoder
    {
        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            WhoisTextResponse result = new WhoisTextResponse();
            result.RawResponse = response;
            result.IsDecoded = false;
            return result;
        }
    }

    private class NullParser : IWhoisResponseParser
    {
        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            return null!;
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            return Array.Empty<WhoisField>();
        }
    }

    private class SuccessfulParser : IWhoisResponseParser
    {
        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            WhoisParsedResponse result = new WhoisParsedResponse();
            result.TextResponse = response;
            result.IsParsed = true;
            return result;
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            return Array.Empty<WhoisField>();
        }
    }

    private class FailedParser : IWhoisResponseParser
    {
        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            WhoisParsedResponse result = new WhoisParsedResponse();
            result.TextResponse = response;
            result.IsParsed = false;
            return result;
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            return Array.Empty<WhoisField>();
        }
    }

    private class NullReferralParser : IWhoisReferralParser
    {
        public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
        {
            return null!;
        }
    }

    private class NullElementReferralParser : IWhoisReferralParser
    {
        public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
        {
            return new WhoisReferral[] { null! };
        }
    }

    private class TrackingStream : MemoryStream
    {
        public bool Disposed;

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
