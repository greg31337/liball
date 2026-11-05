using whois;

namespace whois.Tests;

public class PublicApiBehaviorTests
{
    [Test]
    public void EveryPublicDataAndExceptionTypeComposesIntoLosslessFailureProvenance()
    {
        WhoisTimeoutOptions timeouts = new WhoisTimeoutOptions();
        WhoisLimits limits = new WhoisLimits();
        WhoisEncodingOptions encoding = new WhoisEncodingOptions();
        WhoisClientOptions clientOptions = new WhoisClientOptions();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        WhoisRequest exchangeRequest = new WhoisRequest();
        WhoisResolution resolution = new WhoisResolution();
        WhoisConnection connection = new WhoisConnection();
        WhoisRawResponse raw = new WhoisRawResponse();
        WhoisTextResponse text = new WhoisTextResponse();
        WhoisLine line = new WhoisLine();
        WhoisField field = new WhoisField();
        WhoisRecord record = new WhoisRecord();
        WhoisParsedResponse parsed = new WhoisParsedResponse();
        WhoisReferral referral = new WhoisReferral();
        WhoisServerSelection selection = new WhoisServerSelection();
        WhoisLookupHop hop = new WhoisLookupHop();
        WhoisQuery query = new WhoisQuery();
        WhoisLookupRequest lookupRequest = new WhoisLookupRequest();
        WhoisLookupResult lookupResult = new WhoisLookupResult();
        IOException failure = new IOException("public API provenance failure");
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.65");
        MemoryStream stream = new MemoryStream();

        clientOptions.Timeouts = timeouts;
        clientOptions.Limits = limits;
        clientOptions.Encoding = encoding;
        clientOptions.AddressPreference = WhoisAddressPreference.IPv6First;
        encoding.DecodingMode = WhoisDecodingMode.Latin1Fallback;
        endpoint.Scheme = WhoisReferralScheme.Whois;
        endpoint.Host = WhoisDefaults.IanaHost;
        endpoint.Port = WhoisDefaults.WhoisPort;
        endpoint.AdvertisedValue = WhoisDefaults.IanaHost;
        exchangeRequest.Endpoint = endpoint;
        exchangeRequest.Source = WhoisEndpointSource.InitialSelection;
        exchangeRequest.QueryText = "example.com";
        exchangeRequest.Encoding = encoding;
        exchangeRequest.Timeouts = timeouts;
        exchangeRequest.Limits = limits;
        resolution.Addresses = new[] { address };
        resolution.Status = WhoisExchangeStatus.Complete;
        connection.ApprovedAddress = address;
        connection.Stream = stream;
        connection.Status = WhoisExchangeStatus.Complete;
        raw.Request = exchangeRequest;
        raw.ApprovedAddress = address;
        raw.Body = System.Text.Encoding.UTF8.GetBytes("refer: whois.registry.example\r\n");
        raw.Status = WhoisExchangeStatus.ReadFailed;
        raw.Stage = WhoisOperationStage.Read;
        raw.Error = failure;
        text.RawResponse = raw;
        text.Text = "refer: whois.registry.example\r\n";
        text.EncodingName = "utf-8";
        text.IsDecoded = true;
        line.LineNumber = 1;
        line.RawContent = "refer: whois.registry.example";
        line.NormalizedContent = line.RawContent;
        line.Kind = WhoisLineKind.Field;
        line.FieldIndex = 0;
        field.Index = 0;
        field.RecordIndex = 0;
        field.SourceLine = 1;
        field.OriginalName = WhoisFieldNames.Refer;
        field.CanonicalName = WhoisFieldNames.Refer;
        field.RawValue = "whois.registry.example";
        field.Value = field.RawValue;
        record.Index = 0;
        record.Fields = new[] { field };
        record.Lines = new[] { line };
        parsed.TextResponse = text;
        parsed.Lines = new[] { line };
        parsed.Fields = new[] { field };
        parsed.Records = new[] { record };
        parsed.IsParsed = true;
        referral.SourceField = WhoisFieldNames.Refer;
        referral.SourceLine = 1;
        referral.AdvertisedValue = "whois.registry.example";
        referral.Scheme = WhoisReferralScheme.Whois;
        referral.Kind = WhoisReferralKind.Iana;
        referral.FollowPriority = 0;
        referral.Endpoint = new WhoisEndpoint();
        referral.Endpoint.Host = "whois.registry.example";
        referral.IsValid = true;
        selection.Endpoint = endpoint;
        selection.Source = WhoisEndpointSource.InitialSelection;
        selection.Reason = "Public API selection provenance.";
        hop.Selection = selection;
        hop.RawResponse = raw;
        hop.TextResponse = text;
        hop.ParsedResponse = parsed;
        hop.Referrals = new[] { referral };
        hop.SelectedReferral = referral;
        query.OriginalText = "Example.COM.";
        query.NormalizedText = "example.com";
        query.RoutingKey = "domain:example.com";
        query.Kind = WhoisQueryKind.Domain;
        query.IsValid = true;
        lookupRequest.Text = query.OriginalText;
        lookupRequest.Kind = WhoisQueryKind.Domain;
        lookupRequest.Options = clientOptions;
        lookupRequest.EndpointOverride = endpoint;
        lookupResult.Query = query;
        lookupResult.Hops = new[] { hop };
        lookupResult.FailedStage = WhoisOperationStage.Read;
        lookupResult.Error = failure;

        WhoisQueryException queryException = new WhoisQueryException(
            "Invalid query.",
            "QueryText",
            query.OriginalText,
            query.Kind,
            endpoint,
            failure);
        WhoisServerNotFoundException serverException = new WhoisServerNotFoundException(
            "No server.",
            query,
            failure);
        WhoisTransportException transportException = new WhoisTransportException(
            "Read failed.",
            WhoisOperationStage.Read,
            endpoint,
            address,
            raw,
            failure);
        WhoisResponseException responseException = new WhoisResponseException(
            "Parse failed.",
            WhoisOperationStage.Parse,
            raw,
            text,
            failure);
        WhoisReferralException referralException = new WhoisReferralException(
            "Referral failed.",
            referral,
            "Policy",
            lookupResult,
            failure);

        Assert.That(timeouts.Resolve, Is.EqualTo(WhoisDefaults.ResolveTimeout));
        Assert.That(timeouts.Connect, Is.EqualTo(WhoisDefaults.ConnectTimeout));
        Assert.That(timeouts.Write, Is.EqualTo(WhoisDefaults.WriteTimeout));
        Assert.That(timeouts.ReadIdle, Is.EqualTo(WhoisDefaults.ReadIdleTimeout));
        Assert.That(timeouts.Overall, Is.EqualTo(WhoisDefaults.OverallTimeout));
        Assert.That(limits.MaximumEncodedQueryBytes, Is.EqualTo(WhoisDefaults.MaximumEncodedQueryBytes));
        Assert.That(limits.MaximumResponseBytes, Is.EqualTo(WhoisDefaults.MaximumResponseBytes));
        Assert.That(limits.MaximumReferralHops, Is.EqualTo(WhoisDefaults.MaximumReferralHops));
        Assert.That(limits.MaximumLines, Is.EqualTo(WhoisDefaults.MaximumLines));
        Assert.That(limits.MaximumFields, Is.EqualTo(WhoisDefaults.MaximumFields));
        Assert.That(WhoisDefaults.RWhoisPort, Is.EqualTo(4321));
        Assert.That(new[]
        {
            WhoisFieldNames.Refer,
            WhoisFieldNames.Whois,
            WhoisFieldNames.WhoisServer,
            WhoisFieldNames.RegistrarWhoisServer,
            WhoisFieldNames.ReferralServer,
            WhoisFieldNames.ReferralServerWithSpace,
            WhoisFieldNames.ReferralDirective
        }, Is.EqualTo(new[]
        {
            "refer",
            "whois",
            "Whois Server",
            "Registrar WHOIS Server",
            "ReferralServer",
            "Referral Server",
            "%referral"
        }));
        Assert.That(lookupRequest.Options, Is.SameAs(clientOptions));
        Assert.That(lookupResult.Query, Is.SameAs(query));
        Assert.That(lookupResult.Hops[0], Is.SameAs(hop));
        Assert.That(hop.Selection, Is.SameAs(selection));
        Assert.That(hop.RawResponse, Is.SameAs(raw));
        Assert.That(hop.TextResponse, Is.SameAs(text));
        Assert.That(hop.ParsedResponse, Is.SameAs(parsed));
        Assert.That(parsed.Records[0], Is.SameAs(record));
        Assert.That(record.Lines[0], Is.SameAs(line));
        Assert.That(record.Fields[0], Is.SameAs(field));
        Assert.That(hop.SelectedReferral, Is.SameAs(referral));
        Assert.That(resolution.Addresses, Is.EqualTo(new[] { address }));
        Assert.That(connection.Stream, Is.SameAs(stream));
        Assert.That(queryException.Endpoint, Is.SameAs(endpoint));
        Assert.That(serverException.Query, Is.SameAs(query));
        Assert.That(transportException.PartialResponse, Is.SameAs(raw));
        Assert.That(responseException.TextResponse, Is.SameAs(text));
        Assert.That(referralException.PartialResult, Is.SameAs(lookupResult));

        stream.Dispose();
    }

    [Test]
    public void EveryBuiltInImplementationDispatchesThroughItsPublicContract()
    {
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisQueryFormatter formatter = new WhoisQueryFormatter();
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        IWhoisEndpointPolicy allowAllPolicy = new WhoisAllowAllEndpointPolicy();
        IWhoisEndpointPolicy publicPolicy = new WhoisPublicEndpointPolicy();
        IWhoisConnectionFactory connectionFactory = new WhoisTcpConnectionFactory();
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();
        IWhoisReferralParser referralParser = new WhoisReferralParser();
        WhoisLookupRequest lookupRequest = new WhoisLookupRequest();
        lookupRequest.Text = "Example.COM.";

        WhoisQuery query = classifier.Classify(lookupRequest);
        WhoisServerSelection selection = selector.SelectServer(query, lookupRequest);
        string formattedQuery = formatter.FormatQuery(query, selection);
        WhoisRequest encodedRequest = new WhoisRequest();
        encodedRequest.Endpoint = selection.Endpoint;
        encodedRequest.Source = selection.Source;
        encodedRequest.QueryText = formattedQuery;
        byte[] encoded = codec.EncodeQuery(encodedRequest);
        WhoisEndpoint literalEndpoint = new WhoisEndpoint();
        literalEndpoint.Host = System.Net.IPAddress.Loopback.ToString();
        WhoisResolution literalResolution = resolver.ResolveHost(
            literalEndpoint,
            WhoisAddressPreference.IPv4First,
            default(CancellationToken));
        bool trustedAllowed = allowAllPolicy.AllowEndpoint(
            literalEndpoint,
            System.Net.IPAddress.Loopback,
            WhoisEndpointSource.Direct);
        bool publicAllowed = publicPolicy.AllowEndpoint(
            selection.Endpoint,
            System.Net.IPAddress.Parse("8.8.8.8"),
            WhoisEndpointSource.InitialSelection);

        System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        literalEndpoint.Port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        WhoisConnection connection = connectionFactory.OpenConnection(
            literalEndpoint,
            System.Net.IPAddress.Loopback,
            new WhoisTimeoutOptions(),
            default(CancellationToken));
        using System.Net.Sockets.Socket accepted = listener.AcceptSocket();
        connection.Stream!.Dispose();
        listener.Stop();

        byte[] wireBody = System.Text.Encoding.UTF8.GetBytes("refer: whois.registry.example\r\n");
        using LoopbackWhoisServer server = new LoopbackWhoisServer(new[] { wireBody }, TimeSpan.Zero, false);
        IWhoisTransport tcpTransport = new WhoisTcpTransport(
            codec,
            resolver,
            allowAllPolicy,
            allowAllPolicy,
            connectionFactory,
            WhoisAddressPreference.System);
        WhoisRequest wireRequest = new WhoisRequest();
        wireRequest.Endpoint = server.Endpoint;
        wireRequest.Source = WhoisEndpointSource.Direct;
        wireRequest.QueryText = "example.com";
        WhoisRawResponse raw = tcpTransport.Exchange(wireRequest);
        server.Completion.GetAwaiter().GetResult();
        WhoisTextResponse text = decoder.DecodeResponse(raw, wireRequest.Encoding);
        WhoisParsedResponse parsed = parser.ParseResponse(text, wireRequest.Limits);
        WhoisField[] found = parser.FindFields(parsed, WhoisFieldNames.Refer);
        WhoisReferral[] referrals = referralParser.ParseReferrals(parsed);

        FakeTransport clientTransport = new FakeTransport();
        clientTransport.ExchangeHandler = (request) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Body = System.Text.Encoding.UTF8.GetBytes("domain: example.com\r\n");
            response.Status = WhoisExchangeStatus.Complete;
            return response;
        };
        IWhoisClient client = new WhoisClient(
            classifier,
            formatter,
            selector,
            clientTransport,
            decoder,
            parser,
            referralParser);
        WhoisRequest directRequest = new WhoisRequest();
        directRequest.Endpoint.Host = "whois.direct.example";
        directRequest.QueryText = "example.com";
        WhoisRawResponse queryResponse = client.Query(directRequest);
        WhoisRawResponse tryQueryResponse = client.TryQuery(directRequest);
        WhoisLookupResult lookupResponse = client.Lookup(lookupRequest);
        WhoisLookupResult tryLookupResponse = client.TryLookup(lookupRequest);

        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(selection.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(formattedQuery, Is.EqualTo("example.com"));
        Assert.That(encoded, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(literalResolution.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(literalResolution.Addresses, Is.EqualTo(new[] { System.Net.IPAddress.Loopback }));
        Assert.That(trustedAllowed, Is.True);
        Assert.That(publicAllowed, Is.True);
        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(raw.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(raw.Body, Is.EqualTo(wireBody));
        Assert.That(text.Text, Is.EqualTo("refer: whois.registry.example\r\n"));
        Assert.That(parsed.IsParsed, Is.True);
        Assert.That(found, Has.Length.EqualTo(1));
        Assert.That(referrals, Has.Length.EqualTo(1));
        Assert.That(referrals[0].Kind, Is.EqualTo(WhoisReferralKind.Iana));
        Assert.That(queryResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(tryQueryResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(lookupResponse.IsComplete, Is.True);
        Assert.That(tryLookupResponse.IsComplete, Is.True);
        Assert.That(clientTransport.Requests, Has.Count.EqualTo(4));
        Assert.That(server.Error, Is.Null);
    }
}
