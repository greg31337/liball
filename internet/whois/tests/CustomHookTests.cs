using whois;

namespace whois.Tests;

public class CustomHookTests
{
    [Test]
    public void CustomClassifierControlsQueryPassedToLaterLookupStages()
    {
        RecordingClassifier classifier = new RecordingClassifier();
        classifier.Result.OriginalText = "classifier original";
        classifier.Result.NormalizedText = "classifier-normalized";
        classifier.Result.RoutingKey = "classifier-routing-key";
        classifier.Result.Kind = WhoisQueryKind.AutonomousSystem;
        classifier.Result.IsValid = true;
        FakeServerSelector selector = CreateSelector();
        FakeTransport transport = CreateCompleteTransport(Array.Empty<byte>());
        WhoisClient client = CreateClient(
            classifier,
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        WhoisLookupRequest request = CreateLookupRequest();

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(classifier.Requests, Has.Count.EqualTo(1));
        Assert.That(classifier.Requests[0], Is.Not.SameAs(request));
        Assert.That(classifier.Requests[0].Text, Is.EqualTo(request.Text));
        Assert.That(result.Query, Is.SameAs(classifier.Result));
        Assert.That(selector.Queries, Is.EqualTo(new[] { classifier.Result }));
        Assert.That(transport.Requests[0].QueryText, Is.EqualTo("classifier-normalized"));
    }

    [Test]
    public void CustomFormatterControlsExactTransportQueryText()
    {
        RecordingFormatter formatter = new RecordingFormatter();
        formatter.Result = "CUSTOM FORMAT example.com";
        FakeServerSelector selector = CreateSelector();
        FakeTransport transport = CreateCompleteTransport(Array.Empty<byte>());
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            formatter,
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(formatter.Queries, Has.Count.EqualTo(1));
        Assert.That(formatter.Queries[0].NormalizedText, Is.EqualTo("example.com"));
        Assert.That(formatter.Selections, Is.EqualTo(new[] { result.Hops[0].Selection }));
        Assert.That(transport.Requests[0].QueryText, Is.EqualTo("CUSTOM FORMAT example.com"));
    }

    [Test]
    public void CustomSelectorControlsEndpointSourceAndQueryOverride()
    {
        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = "whois.selector.example";
        selector.Result.Endpoint.Port = 4343;
        selector.Result.Source = WhoisEndpointSource.Direct;
        selector.Result.QueryOverride = "SELECTOR OVERRIDE";
        selector.Result.Reason = "Chosen by custom selector.";
        FakeTransport transport = CreateCompleteTransport(Array.Empty<byte>());
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(selector.Queries, Has.Count.EqualTo(1));
        Assert.That(selector.Requests, Has.Count.EqualTo(1));
        Assert.That(result.Hops[0].Selection.Endpoint.Host, Is.EqualTo("whois.selector.example"));
        Assert.That(result.Hops[0].Selection.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(result.Hops[0].Selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(result.Hops[0].Selection.Reason, Is.EqualTo("Chosen by custom selector."));
        Assert.That(transport.Requests[0].Endpoint.Host, Is.EqualTo("whois.selector.example"));
        Assert.That(transport.Requests[0].Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(transport.Requests[0].QueryText, Is.EqualTo("SELECTOR OVERRIDE"));
    }

    [Test]
    public void CustomTransportControlsDirectClientResponse()
    {
        FakeTransport transport = new FakeTransport();
        WhoisRawResponse expected = new WhoisRawResponse();
        expected.Body = new byte[] { 99, 117, 115, 116, 111, 109 };
        expected.Status = WhoisExchangeStatus.Complete;
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            expected.Request = exchangeRequest;
            return expected;
        };
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            CreateSelector(),
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.transport.example";
        request.QueryText = "custom transport query";

        WhoisRawResponse response = client.TryQuery(request);

        Assert.That(response, Is.SameAs(expected));
        Assert.That(response.Body, Is.EqualTo(new byte[] { 99, 117, 115, 116, 111, 109 }));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
        Assert.That(transport.Requests[0], Is.Not.SameAs(request));
        Assert.That(transport.Requests[0].Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(transport.Requests[0].QueryText, Is.EqualTo("custom transport query"));
    }

    [Test]
    public void CustomDecoderControlsTextPassedToTheParser()
    {
        byte[] rawBody = new byte[] { 255, 254, 253 };
        FakeTransport transport = CreateCompleteTransport(rawBody);
        RecordingDecoder decoder = new RecordingDecoder();
        decoder.Result.Text = "decoded by custom hook";
        decoder.Result.EncodingName = "custom-encoding";
        decoder.Result.IsDecoded = true;
        RecordingParser parser = new RecordingParser();
        parser.Result.IsParsed = true;
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            CreateSelector(),
            transport,
            decoder,
            parser,
            new WhoisReferralParser());

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(decoder.Responses, Has.Count.EqualTo(1));
        Assert.That(decoder.Responses[0].Body, Is.EqualTo(rawBody));
        Assert.That(decoder.Options, Has.Count.EqualTo(1));
        Assert.That(result.Hops[0].TextResponse, Is.SameAs(decoder.Result));
        Assert.That(result.Hops[0].TextResponse.Text, Is.EqualTo("decoded by custom hook"));
        Assert.That(result.Hops[0].TextResponse.EncodingName, Is.EqualTo("custom-encoding"));
        Assert.That(parser.Responses, Is.EqualTo(new[] { decoder.Result }));
    }

    [Test]
    public void CustomParserControlsParsedResponsePassedToReferralParsing()
    {
        FakeTransport transport = CreateCompleteTransport(System.Text.Encoding.ASCII.GetBytes("ignored by custom parser"));
        RecordingParser parser = new RecordingParser();
        WhoisField customField = new WhoisField();
        customField.OriginalName = "Custom Field";
        customField.Value = "custom value";
        parser.Result.Fields = new[] { customField };
        parser.Result.IsParsed = true;
        RecordingReferralParser referralParser = new RecordingReferralParser();
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            CreateSelector(),
            transport,
            new WhoisResponseDecoder(),
            parser,
            referralParser);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(parser.Responses, Has.Count.EqualTo(1));
        Assert.That(parser.Limits, Has.Count.EqualTo(1));
        Assert.That(result.Hops[0].ParsedResponse, Is.SameAs(parser.Result));
        Assert.That(result.Hops[0].ParsedResponse.Fields, Is.EqualTo(new[] { customField }));
        Assert.That(referralParser.Responses, Is.EqualTo(new[] { parser.Result }));
    }

    [Test]
    public void CustomReferralParserControlsRetainedReferralMetadata()
    {
        FakeTransport transport = CreateCompleteTransport(Array.Empty<byte>());
        RecordingReferralParser referralParser = new RecordingReferralParser();
        WhoisReferral customReferral = new WhoisReferral();
        customReferral.SourceField = "custom-referral";
        customReferral.AdvertisedValue = "https://custom.example/lookup";
        customReferral.Scheme = WhoisReferralScheme.Https;
        customReferral.Kind = WhoisReferralKind.Web;
        customReferral.FollowPriority = -1;
        customReferral.IsValid = true;
        customReferral.Endpoint = new WhoisEndpoint();
        customReferral.Endpoint.Scheme = WhoisReferralScheme.Https;
        customReferral.Endpoint.Host = "custom.example";
        customReferral.Endpoint.Port = 443;
        referralParser.Result = new[] { customReferral };
        WhoisClient client = CreateClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            CreateSelector(),
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            referralParser);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(referralParser.Responses, Has.Count.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Is.SameAs(referralParser.Result));
        Assert.That(result.Hops[0].Referrals, Is.EqualTo(new[] { customReferral }));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void CustomResolverControlsAddressGivenToPolicyAndConnectionFactory()
    {
        System.Net.IPAddress customAddress = System.Net.IPAddress.Parse("198.51.100.62");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { customAddress };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Stream = new MemoryStream();
        WhoisTcpTransport transport = CreateTransport(resolver, policy, factory);
        WhoisRequest request = CreateExchangeRequest();

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.ApprovedAddress, Is.SameAs(customAddress));
        Assert.That(resolver.Endpoints, Is.EqualTo(new[] { request.Endpoint }));
        Assert.That(resolver.Preferences, Is.EqualTo(new[] { WhoisAddressPreference.IPv6First }));
        Assert.That(policy.Addresses, Is.EqualTo(new[] { customAddress }));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { customAddress }));
    }

    [Test]
    public void CustomEndpointPolicyControlsWhichResolvedAddressCanConnect()
    {
        System.Net.IPAddress deniedAddress = System.Net.IPAddress.Parse("203.0.113.1");
        System.Net.IPAddress allowedAddress = System.Net.IPAddress.Parse("203.0.113.2");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { deniedAddress, allowedAddress };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        policy.AllowHandler = (endpoint, address, source) => address.Equals(allowedAddress);
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Stream = new MemoryStream();
        WhoisTcpTransport transport = CreateTransport(resolver, policy, factory);
        WhoisRequest request = CreateExchangeRequest();

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(policy.Endpoints, Is.EqualTo(new[] { request.Endpoint, request.Endpoint }));
        Assert.That(policy.Addresses, Is.EqualTo(new[] { deniedAddress, allowedAddress }));
        Assert.That(policy.Sources, Is.EqualTo(new[] { WhoisEndpointSource.Referral, WhoisEndpointSource.Referral }));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { allowedAddress }));
        Assert.That(response.ApprovedAddress, Is.SameAs(allowedAddress));
    }

    [Test]
    public void CustomConnectionFactoryControlsConnectionFailureOutcome()
    {
        System.Net.IPAddress address = System.Net.IPAddress.Parse("203.0.113.62");
        IOException customError = new IOException("custom connection hook refused the endpoint");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { address };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeEndpointPolicy policy = new FakeEndpointPolicy();
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Status = WhoisExchangeStatus.ConnectFailed;
        factory.Error = customError;
        factory.Duration = TimeSpan.FromMilliseconds(62);
        WhoisTcpTransport transport = CreateTransport(resolver, policy, factory);
        WhoisRequest request = CreateExchangeRequest();

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Error, Is.SameAs(customError));
        Assert.That(factory.Endpoints, Is.EqualTo(new[] { request.Endpoint }));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { address }));
        Assert.That(factory.TimeoutOptions, Is.EqualTo(new[] { request.Timeouts }));
        Assert.That(factory.CancellationTokens, Has.Count.EqualTo(1));
    }

    private static FakeServerSelector CreateSelector()
    {
        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = "whois.hook.example";
        selector.Result.Endpoint.Port = WhoisDefaults.WhoisPort;
        selector.Result.Source = WhoisEndpointSource.InitialSelection;
        return selector;
    }

    private static FakeTransport CreateCompleteTransport(byte[] body)
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = exchangeRequest;
            response.Body = body;
            response.Status = WhoisExchangeStatus.Complete;
            return response;
        };
        return transport;
    }

    private static WhoisTcpTransport CreateTransport(FakeHostResolver resolver, FakeEndpointPolicy policy, FakeConnectionFactory factory)
    {
        return new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            policy,
            policy,
            factory,
            WhoisAddressPreference.IPv6First);
    }

    private static WhoisClient CreateClient(
        IWhoisQueryClassifier classifier,
        IWhoisQueryFormatter formatter,
        IWhoisServerSelector selector,
        IWhoisTransport transport,
        IWhoisResponseDecoder decoder,
        IWhoisResponseParser parser,
        IWhoisReferralParser referralParser)
    {
        return new WhoisClient(classifier, formatter, selector, transport, decoder, parser, referralParser);
    }

    private static WhoisLookupRequest CreateLookupRequest()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        return request;
    }

    private static WhoisRequest CreateExchangeRequest()
    {
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.transport-hook.example";
        request.Endpoint.Port = WhoisDefaults.WhoisPort;
        request.Source = WhoisEndpointSource.Referral;
        request.QueryText = "example.com";
        return request;
    }

    private class RecordingClassifier : IWhoisQueryClassifier
    {
        public WhoisQuery Result = new WhoisQuery();
        public List<WhoisLookupRequest> Requests = new List<WhoisLookupRequest>();

        public WhoisQuery Classify(WhoisLookupRequest request)
        {
            Requests.Add(request);
            return Result;
        }
    }

    private class RecordingFormatter : IWhoisQueryFormatter
    {
        public string Result = string.Empty;
        public List<WhoisQuery> Queries = new List<WhoisQuery>();
        public List<WhoisServerSelection> Selections = new List<WhoisServerSelection>();

        public string FormatQuery(WhoisQuery query, WhoisServerSelection selection)
        {
            Queries.Add(query);
            Selections.Add(selection);
            return Result;
        }
    }

    private class RecordingDecoder : IWhoisResponseDecoder
    {
        public WhoisTextResponse Result = new WhoisTextResponse();
        public List<WhoisRawResponse> Responses = new List<WhoisRawResponse>();
        public List<WhoisEncodingOptions> Options = new List<WhoisEncodingOptions>();

        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            Responses.Add(response);
            Options.Add(options);
            Result.RawResponse = response;
            return Result;
        }
    }

    private class RecordingParser : IWhoisResponseParser
    {
        public WhoisParsedResponse Result = new WhoisParsedResponse();
        public WhoisField[] FindResult = Array.Empty<WhoisField>();
        public List<WhoisTextResponse> Responses = new List<WhoisTextResponse>();
        public List<WhoisLimits> Limits = new List<WhoisLimits>();

        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            Responses.Add(response);
            Limits.Add(limits);
            Result.TextResponse = response;
            return Result;
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            return FindResult;
        }
    }

    private class RecordingReferralParser : IWhoisReferralParser
    {
        public WhoisReferral[] Result = Array.Empty<WhoisReferral>();
        public List<WhoisParsedResponse> Responses = new List<WhoisParsedResponse>();

        public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
        {
            Responses.Add(response);
            return Result;
        }
    }
}
