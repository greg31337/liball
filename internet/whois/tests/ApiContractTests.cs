using whois;

namespace whois.Tests;

public class ApiContractTests
{
    [Test]
    public void ClassificationFormattingAndCodecContractsDispatch()
    {
        ContractImplementation implementation = new ContractImplementation();
        IWhoisQueryClassifier classifier = implementation;
        IWhoisQueryFormatter formatter = implementation;
        IWhoisQueryCodec codec = implementation;
        WhoisLookupRequest lookupRequest = new WhoisLookupRequest();
        WhoisServerSelection selection = new WhoisServerSelection();
        WhoisRequest request = new WhoisRequest();

        lookupRequest.Text = "Example.COM";
        lookupRequest.Kind = WhoisQueryKind.Domain;
        WhoisQuery query = classifier.Classify(lookupRequest);
        selection.QueryOverride = "domain example.com";
        string formatted = formatter.FormatQuery(query, selection);
        request.QueryText = formatted;
        byte[] payload = codec.EncodeQuery(request);

        Assert.That(query.OriginalText, Is.EqualTo("Example.COM"));
        Assert.That(query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(query.IsValid, Is.True);
        Assert.That(formatted, Is.EqualTo("domain example.com"));
        Assert.That(payload, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("domain example.com\r\n")));
    }

    [Test]
    public void ResolverPolicyAndConnectionContractsDispatch()
    {
        ContractImplementation implementation = new ContractImplementation();
        IWhoisHostResolver resolver = implementation;
        IWhoisEndpointPolicy policy = implementation;
        IWhoisConnectionFactory connectionFactory = implementation;
        WhoisEndpoint endpoint = new WhoisEndpoint();

        endpoint.Host = "whois.example";
        WhoisResolution resolution = resolver.ResolveHost(endpoint, WhoisAddressPreference.IPv4First, default(CancellationToken));
        bool directAllowed = policy.AllowEndpoint(endpoint, resolution.Addresses[0], WhoisEndpointSource.Direct);
        bool referralAllowed = policy.AllowEndpoint(endpoint, resolution.Addresses[0], WhoisEndpointSource.Referral);
        WhoisConnection connection = connectionFactory.OpenConnection(endpoint, resolution.Addresses[0], new WhoisTimeoutOptions(), default(CancellationToken));

        Assert.That(resolution.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(resolution.Addresses, Is.EqualTo(new[] { System.Net.IPAddress.Loopback }));
        Assert.That(directAllowed, Is.True);
        Assert.That(referralAllowed, Is.False);
        Assert.That(connection.ApprovedAddress, Is.SameAs(resolution.Addresses[0]));
        Assert.That(connection.Stream, Is.TypeOf<MemoryStream>());
        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Complete));

        connection.Stream?.Dispose();
    }

    [Test]
    public void TransportDecoderParserAndReferralContractsDispatch()
    {
        ContractImplementation implementation = new ContractImplementation();
        IWhoisTransport transport = implementation;
        IWhoisResponseDecoder decoder = implementation;
        IWhoisResponseParser parser = implementation;
        IWhoisReferralParser referralParser = implementation;
        WhoisRequest request = new WhoisRequest();

        request.QueryText = "example.com";
        WhoisRawResponse raw = transport.Exchange(request);
        WhoisTextResponse text = decoder.DecodeResponse(raw, request.Encoding);
        WhoisParsedResponse parsed = parser.ParseResponse(text, request.Limits);
        WhoisField[] fields = parser.FindFields(parsed, "query");
        WhoisReferral[] referrals = referralParser.ParseReferrals(parsed);

        Assert.That(raw.Request, Is.SameAs(request));
        Assert.That(raw.Body, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("query: example.com")));
        Assert.That(text.RawResponse, Is.SameAs(raw));
        Assert.That(text.Text, Is.EqualTo("query: example.com"));
        Assert.That(text.IsDecoded, Is.True);
        Assert.That(parsed.TextResponse, Is.SameAs(text));
        Assert.That(parsed.IsParsed, Is.True);
        Assert.That(fields, Has.Length.EqualTo(1));
        Assert.That(fields[0].CanonicalName, Is.EqualTo("query"));
        Assert.That(referrals, Has.Length.EqualTo(1));
        Assert.That(referrals[0].Kind, Is.EqualTo(WhoisReferralKind.Generic));
    }

    [Test]
    public void SelectorAndClientContractsDispatchAllOperations()
    {
        ContractImplementation implementation = new ContractImplementation();
        IWhoisServerSelector selector = implementation;
        IWhoisClient client = implementation;
        WhoisLookupRequest lookupRequest = new WhoisLookupRequest();
        WhoisQuery query = new WhoisQuery();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        WhoisRequest directRequest = new WhoisRequest();

        endpoint.Host = "whois.override.example";
        lookupRequest.Text = "example.com";
        lookupRequest.EndpointOverride = endpoint;
        query.NormalizedText = "example.com";
        WhoisServerSelection selection = selector.SelectServer(query, lookupRequest);
        WhoisRawResponse queryResponse = client.Query(directRequest);
        WhoisRawResponse tryQueryResponse = client.TryQuery(directRequest);
        WhoisLookupResult lookupResult = client.Lookup(lookupRequest);
        WhoisLookupResult tryLookupResult = client.TryLookup(lookupRequest);

        Assert.That(selection.Endpoint, Is.SameAs(endpoint));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(queryResponse.Request, Is.SameAs(directRequest));
        Assert.That(queryResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(tryQueryResponse.Request, Is.SameAs(directRequest));
        Assert.That(tryQueryResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(lookupResult.Query.OriginalText, Is.EqualTo("example.com"));
        Assert.That(lookupResult.IsComplete, Is.True);
        Assert.That(tryLookupResult.Query.OriginalText, Is.EqualTo("example.com"));
        Assert.That(tryLookupResult.IsComplete, Is.True);
    }

    private class ContractImplementation : IWhoisQueryClassifier, IWhoisQueryFormatter, IWhoisQueryCodec, IWhoisHostResolver, IWhoisEndpointPolicy, IWhoisConnectionFactory, IWhoisTransport, IWhoisResponseDecoder, IWhoisResponseParser, IWhoisReferralParser, IWhoisServerSelector, IWhoisClient
    {
        public WhoisQuery Classify(WhoisLookupRequest request)
        {
            WhoisQuery query = new WhoisQuery();
            query.OriginalText = request.Text;
            query.NormalizedText = request.Text.ToLowerInvariant();
            query.RoutingKey = query.NormalizedText;
            query.Kind = request.Kind;
            query.IsValid = true;
            return query;
        }

        public string FormatQuery(WhoisQuery query, WhoisServerSelection selection)
        {
            return selection.QueryOverride ?? query.NormalizedText;
        }

        public byte[] EncodeQuery(WhoisRequest request)
        {
            return request.Encoding.RequestEncoding.GetBytes(request.QueryText + "\r\n");
        }

        public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, CancellationToken cancellationToken)
        {
            WhoisResolution resolution = new WhoisResolution();
            resolution.Addresses = new[] { System.Net.IPAddress.Loopback };
            resolution.Status = WhoisExchangeStatus.Complete;
            return resolution;
        }

        public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
        {
            return source == WhoisEndpointSource.Direct;
        }

        public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, CancellationToken cancellationToken)
        {
            WhoisConnection connection = new WhoisConnection();
            connection.ApprovedAddress = approvedAddress;
            connection.Stream = new MemoryStream();
            connection.Status = WhoisExchangeStatus.Complete;
            return connection;
        }

        public WhoisRawResponse Exchange(WhoisRequest request)
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Body = System.Text.Encoding.UTF8.GetBytes("query: " + request.QueryText);
            response.Status = WhoisExchangeStatus.Complete;
            return response;
        }

        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            WhoisTextResponse text = new WhoisTextResponse();
            text.RawResponse = response;
            text.Text = System.Text.Encoding.UTF8.GetString(response.Body);
            text.EncodingName = "utf-8";
            text.IsDecoded = true;
            return text;
        }

        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            WhoisField field = new WhoisField();
            WhoisParsedResponse parsed = new WhoisParsedResponse();
            field.Index = 0;
            field.SourceLine = 1;
            field.OriginalName = "query";
            field.CanonicalName = "query";
            field.RawValue = response.Text;
            field.Value = response.Text;
            parsed.TextResponse = response;
            parsed.Fields = new[] { field };
            parsed.IsParsed = true;
            return parsed;
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            return response.Fields;
        }

        public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
        {
            WhoisReferral referral = new WhoisReferral();
            referral.Kind = WhoisReferralKind.Generic;
            return new[] { referral };
        }

        public WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request)
        {
            WhoisServerSelection selection = new WhoisServerSelection();
            if (request.EndpointOverride == null)
            {
                selection.Endpoint.Host = WhoisDefaults.IanaHost;
                selection.Source = WhoisEndpointSource.InitialSelection;
            }
            else
            {
                selection.Endpoint = request.EndpointOverride;
                selection.Source = WhoisEndpointSource.Direct;
            }
            return selection;
        }

        public WhoisRawResponse Query(WhoisRequest request)
        {
            return Exchange(request);
        }

        public WhoisRawResponse TryQuery(WhoisRequest request)
        {
            return Exchange(request);
        }

        public WhoisLookupResult Lookup(WhoisLookupRequest request)
        {
            WhoisLookupResult result = new WhoisLookupResult();
            result.Query = Classify(request);
            result.IsComplete = true;
            return result;
        }

        public WhoisLookupResult TryLookup(WhoisLookupRequest request)
        {
            return Lookup(request);
        }
    }
}
