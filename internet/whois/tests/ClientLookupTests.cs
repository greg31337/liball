using whois;

namespace whois.Tests;

public class ClientLookupTests
{
    [Test]
    public void ReturnsCompletedLookupResult()
    {
        FakeTransport transport = CreateTransport(string.Empty);
        WhoisClient client = CreateClient(transport);

        WhoisLookupResult result = client.Lookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
    }

    [Test]
    public void ThrowsTypedSelectionFailureWithClassifiedQuery()
    {
        FakeTransport transport = CreateTransport(string.Empty);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();
        request.Text = "domain example.test";

        WhoisServerNotFoundException exception = Assert.Throws<WhoisServerNotFoundException>(() => client.Lookup(request))!;

        Assert.That(exception.Query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
        Assert.That(exception.Query.OriginalText, Is.EqualTo("domain example.test"));
        Assert.That(exception.Query.NormalizedText, Is.EqualTo("domain example.test"));
        Assert.That(exception.InnerException, Is.TypeOf<WhoisServerNotFoundException>());
        Assert.That(transport.Requests, Is.Empty);
    }

    [Test]
    public void ThrowsTransportFailureWithPartialRawContext()
    {
        FakeTransport transport = new FakeTransport();
        IOException error = new IOException("partial read failed");
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.57");
        WhoisRawResponse partial = new WhoisRawResponse();
        partial.ApprovedAddress = address;
        partial.Body = new byte[] { 112, 97, 114, 116 };
        partial.Status = WhoisExchangeStatus.ReadFailed;
        partial.Stage = WhoisOperationStage.Read;
        partial.Error = error;
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            partial.Request = exchangeRequest;
            return partial;
        };
        WhoisClient client = CreateClient(transport);

        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Lookup(CreateRequest()))!;

        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(exception.Endpoint, Is.SameAs(partial.Request.Endpoint));
        Assert.That(exception.ApprovedAddress, Is.SameAs(address));
        Assert.That(exception.PartialResponse, Is.SameAs(partial));
        Assert.That(exception.PartialResponse.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(exception.InnerException, Is.SameAs(error));
    }

    [Test]
    public void ThrowsResponseFailureWithRawAndDecodeContext()
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse raw = new WhoisRawResponse();
            raw.Request = exchangeRequest;
            raw.Body = new byte[] { 195, 40 };
            raw.Status = WhoisExchangeStatus.Complete;
            return raw;
        };
        WhoisClient client = CreateClient(transport);

        WhoisResponseException exception = Assert.Throws<WhoisResponseException>(() => client.Lookup(CreateRequest()))!;

        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Decode));
        Assert.That(exception.RawResponse.Body, Is.EqualTo(new byte[] { 195, 40 }));
        Assert.That(exception.TextResponse, Is.Not.Null);
        Assert.That(exception.TextResponse!.RawResponse, Is.SameAs(exception.RawResponse));
        Assert.That(exception.TextResponse.IsDecoded, Is.False);
        Assert.That(exception.InnerException, Is.SameAs(exception.TextResponse.Error));
    }

    [Test]
    public void ThrowsResponseFailureWithParsedTextContext()
    {
        FakeTransport transport = CreateTransport("first: one\r\nsecond: two\r\n");
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateRequest();
        request.Options.Limits.MaximumLines = 1;

        WhoisResponseException exception = Assert.Throws<WhoisResponseException>(() => client.Lookup(request))!;

        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Parse));
        Assert.That(exception.RawResponse.Body, Is.Not.Empty);
        Assert.That(exception.TextResponse, Is.Not.Null);
        Assert.That(exception.TextResponse!.Text, Is.EqualTo("first: one\r\nsecond: two\r\n"));
        Assert.That(exception.TextResponse.IsDecoded, Is.True);
        Assert.That(exception.InnerException, Is.Not.Null);
    }

    [Test]
    public void ThrowsReferralFailureWithSelectedReferralAndPartialLookup()
    {
        FakeTransport transport = CreateTransport("refer: whois.iana.org\r\n");
        WhoisClient client = CreateClient(transport);

        WhoisReferralException exception = Assert.Throws<WhoisReferralException>(() => client.Lookup(CreateRequest()))!;

        Assert.That(exception.Reason, Is.EqualTo("Loop"));
        Assert.That(exception.Referral, Is.Not.Null);
        Assert.That(exception.Referral!.Endpoint!.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(exception.PartialResult.IsComplete, Is.False);
        Assert.That(exception.PartialResult.FailedStage, Is.EqualTo(WhoisOperationStage.Refer));
        Assert.That(exception.PartialResult.Hops, Has.Length.EqualTo(1));
        Assert.That(exception.PartialResult.Hops[0].SelectedReferral, Is.SameAs(exception.Referral));
        Assert.That(exception.InnerException, Is.SameAs(exception.PartialResult.Error));
    }

    private static FakeTransport CreateTransport(string responseText)
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse raw = new WhoisRawResponse();
            raw.Request = exchangeRequest;
            raw.Body = System.Text.Encoding.UTF8.GetBytes(responseText);
            raw.Status = WhoisExchangeStatus.Complete;
            return raw;
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
