using whois;

namespace whois.Tests;

public class ClientLookupFailureTests
{
    [Test]
    public void InitialNoServerIsReturnedAndThrownWithClassifiedQueryContext()
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();
        request.Text = "domain example.test";

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisServerNotFoundException exception = Assert.Throws<WhoisServerNotFoundException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Select));
        Assert.That(statusBearing.Query.OriginalText, Is.EqualTo("domain example.test"));
        Assert.That(statusBearing.Query.NormalizedText, Is.EqualTo("domain example.test"));
        Assert.That(statusBearing.Query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
        Assert.That(statusBearing.Hops, Is.Empty);
        Assert.That(statusBearing.Error, Is.TypeOf<WhoisServerNotFoundException>());
        Assert.That(exception.Query.OriginalText, Is.EqualTo(statusBearing.Query.OriginalText));
        Assert.That(exception.Query.NormalizedText, Is.EqualTo(statusBearing.Query.NormalizedText));
        Assert.That(exception.Query.Kind, Is.EqualTo(statusBearing.Query.Kind));
        Assert.That(exception.InnerException, Is.TypeOf<WhoisServerNotFoundException>());
        Assert.That(transport.Requests, Is.Empty);
    }

    [Test]
    public void FirstHopResolutionFailureIsReturnedAndThrownWithRawContext()
    {
        IOException error = new IOException("initial resolution failed");
        FakeTransport transport = CreateFailureTransport(
            WhoisExchangeStatus.ResolveFailed,
            WhoisOperationStage.Resolve,
            Array.Empty<byte>(),
            false,
            error);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(statusBearing.Error, Is.SameAs(error));
        Assert.That(statusBearing.Hops, Has.Length.EqualTo(1));
        Assert.That(statusBearing.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(statusBearing.Hops[0].RawResponse.Body, Is.Empty);
        Assert.That(statusBearing.Hops[0].TextResponse.RawResponse, Is.SameAs(statusBearing.Hops[0].RawResponse));
        Assert.That(statusBearing.Hops[0].TextResponse.IsDecoded, Is.False);
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(exception.PartialResponse.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(exception.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void LaterHopPartialReadFailurePreservesSuccessfulAndFailedHopContext()
    {
        byte[] partialBody = System.Text.Encoding.ASCII.GetBytes("partial registrar response");
        IOException error = new IOException("registrar read failed");
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = exchangeRequest;
            if (exchangeRequest.Endpoint.Host == WhoisDefaults.IanaHost)
            {
                response.Body = System.Text.Encoding.ASCII.GetBytes("refer: whois.registry.example\r\n");
                response.Status = WhoisExchangeStatus.Complete;
            }
            else
            {
                response.Body = partialBody;
                response.Status = WhoisExchangeStatus.ReadFailed;
                response.Stage = WhoisOperationStage.Read;
                response.Error = error;
            }
            return response;
        };
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(statusBearing.Error, Is.SameAs(error));
        Assert.That(statusBearing.Hops, Has.Length.EqualTo(2));
        Assert.That(statusBearing.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(statusBearing.Hops[0].TextResponse.IsDecoded, Is.True);
        Assert.That(statusBearing.Hops[0].ParsedResponse.IsParsed, Is.True);
        Assert.That(statusBearing.Hops[0].SelectedReferral, Is.SameAs(statusBearing.Hops[0].Referrals[0]));
        Assert.That(statusBearing.Hops[1].Selection.Endpoint.Host, Is.EqualTo("whois.registry.example"));
        Assert.That(statusBearing.Hops[1].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(statusBearing.Hops[1].RawResponse.Body, Is.EqualTo(partialBody));
        Assert.That(statusBearing.Hops[1].TextResponse.RawResponse, Is.SameAs(statusBearing.Hops[1].RawResponse));
        Assert.That(statusBearing.Hops[1].TextResponse.IsDecoded, Is.False);
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(exception.Endpoint.Host, Is.EqualTo("whois.registry.example"));
        Assert.That(exception.PartialResponse.Body, Is.EqualTo(partialBody));
        Assert.That(exception.PartialResponse.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(4));
    }

    [Test]
    public void ResponseLimitIsReturnedAndThrownWithTruncatedPrefix()
    {
        byte[] retainedPrefix = new byte[] { 100, 111, 109, 97, 105, 110 };
        InvalidOperationException error = new InvalidOperationException("response byte limit reached");
        FakeTransport transport = CreateFailureTransport(
            WhoisExchangeStatus.ResponseLimitReached,
            WhoisOperationStage.Read,
            retainedPrefix,
            true,
            error);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(statusBearing.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.ResponseLimitReached));
        Assert.That(statusBearing.Hops[0].RawResponse.Body, Is.EqualTo(retainedPrefix));
        Assert.That(statusBearing.Hops[0].RawResponse.IsTruncated, Is.True);
        Assert.That(exception.Message, Is.EqualTo("The WHOIS response exceeded the configured byte limit."));
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(exception.PartialResponse.Body, Is.EqualTo(retainedPrefix));
        Assert.That(exception.PartialResponse.IsTruncated, Is.True);
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void DecodeFailureIsReturnedAndThrownWithLosslessRawBytes()
    {
        byte[] malformedUtf8 = new byte[] { 195, 40 };
        FakeTransport transport = CreateCompleteTransport(malformedUtf8);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisResponseException exception = Assert.Throws<WhoisResponseException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Decode));
        Assert.That(statusBearing.Error, Is.SameAs(statusBearing.Hops[0].TextResponse.Error));
        Assert.That(statusBearing.Hops, Has.Length.EqualTo(1));
        Assert.That(statusBearing.Hops[0].RawResponse.Body, Is.EqualTo(malformedUtf8));
        Assert.That(statusBearing.Hops[0].TextResponse.RawResponse, Is.SameAs(statusBearing.Hops[0].RawResponse));
        Assert.That(statusBearing.Hops[0].TextResponse.IsDecoded, Is.False);
        Assert.That(statusBearing.Hops[0].ParsedResponse.TextResponse, Is.SameAs(statusBearing.Hops[0].TextResponse));
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Decode));
        Assert.That(exception.RawResponse.Body, Is.EqualTo(malformedUtf8));
        Assert.That(exception.TextResponse, Is.Not.Null);
        Assert.That(exception.TextResponse!.RawResponse, Is.SameAs(exception.RawResponse));
        Assert.That(exception.TextResponse.IsDecoded, Is.False);
        Assert.That(exception.InnerException, Is.SameAs(exception.TextResponse.Error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParserFailureIsReturnedAndThrownWithDecodedTextContext()
    {
        string responseText = "first: one\r\nsecond: two\r\n";
        FakeTransport transport = CreateCompleteTransport(System.Text.Encoding.UTF8.GetBytes(responseText));
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();
        request.Options.Limits.MaximumLines = 1;

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisResponseException exception = Assert.Throws<WhoisResponseException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Parse));
        Assert.That(statusBearing.Error, Is.SameAs(statusBearing.Hops[0].ParsedResponse.Error));
        Assert.That(statusBearing.Hops, Has.Length.EqualTo(1));
        Assert.That(statusBearing.Hops[0].TextResponse.Text, Is.EqualTo(responseText));
        Assert.That(statusBearing.Hops[0].TextResponse.IsDecoded, Is.True);
        Assert.That(statusBearing.Hops[0].ParsedResponse.TextResponse, Is.SameAs(statusBearing.Hops[0].TextResponse));
        Assert.That(statusBearing.Hops[0].ParsedResponse.IsParsed, Is.False);
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Parse));
        Assert.That(exception.RawResponse.Body, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(responseText)));
        Assert.That(exception.TextResponse, Is.Not.Null);
        Assert.That(exception.TextResponse!.Text, Is.EqualTo(responseText));
        Assert.That(exception.TextResponse.IsDecoded, Is.True);
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void CallerCancellationIsReturnedAndThrownAtTheReportedStage()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException error = new OperationCanceledException(cancellation.Token);
        FakeTransport transport = CreateFailureTransport(
            WhoisExchangeStatus.Cancelled,
            WhoisOperationStage.Resolve,
            Array.Empty<byte>(),
            false,
            error);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();
        request.CancellationToken = cancellation.Token;

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(statusBearing.Error, Is.SameAs(error));
        Assert.That(statusBearing.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(statusBearing.Hops[0].RawResponse.Request.CancellationToken, Is.EqualTo(cancellation.Token));
        Assert.That(exception.Message, Is.EqualTo("The WHOIS exchange was cancelled."));
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Resolve));
        Assert.That(exception.PartialResponse.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(exception.PartialResponse.Request.CancellationToken, Is.EqualTo(cancellation.Token));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void TimeoutIsReturnedAndThrownAtTheReportedStage()
    {
        TimeoutException error = new TimeoutException("connection timed out");
        FakeTransport transport = CreateFailureTransport(
            WhoisExchangeStatus.TimedOut,
            WhoisOperationStage.Connect,
            Array.Empty<byte>(),
            false,
            error);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();

        WhoisLookupResult statusBearing = client.TryLookup(request);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(request))!;

        Assert.That(statusBearing.IsComplete, Is.False);
        Assert.That(statusBearing.FailedStage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(statusBearing.Error, Is.SameAs(error));
        Assert.That(statusBearing.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(exception.Message, Is.EqualTo("The WHOIS exchange timed out."));
        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(exception.PartialResponse.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    private static FakeTransport CreateFailureTransport(
        WhoisExchangeStatus status,
        WhoisOperationStage stage,
        byte[] body,
        bool isTruncated,
        Exception error)
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = exchangeRequest;
            response.Body = body;
            response.Status = status;
            response.Stage = stage;
            response.IsTruncated = isTruncated;
            response.Error = error;
            return response;
        };
        return transport;
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

    private static WhoisClient CreateClient(IWhoisTransport transport)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            new IanaWhoisServerSelector(),
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
    }

    private static WhoisLookupRequest CreateRequest()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        return request;
    }
}
