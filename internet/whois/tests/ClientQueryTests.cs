using whois;

namespace whois.Tests;

public class ClientQueryTests
{
    [Test]
    public void ReturnsCompleteTransportResponseUnchanged()
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(transport);
        WhoisRawResponse complete = new WhoisRawResponse();
        complete.Status = WhoisExchangeStatus.Complete;
        transport.ExchangeHandler = (request) =>
        {
            complete.Request = request;
            return complete;
        };

        WhoisRawResponse response = client.Query(CreateRequest());

        Assert.That(response, Is.SameAs(complete));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
        Assert.That(response.Request.Source, Is.EqualTo(WhoisEndpointSource.Direct));
    }

    [TestCase(WhoisExchangeStatus.Cancelled, WhoisOperationStage.Read, "The WHOIS exchange was cancelled.")]
    [TestCase(WhoisExchangeStatus.TimedOut, WhoisOperationStage.Connect, "The WHOIS exchange timed out.")]
    [TestCase(WhoisExchangeStatus.ResponseLimitReached, WhoisOperationStage.Read, "The WHOIS response exceeded the configured byte limit.")]
    [TestCase(WhoisExchangeStatus.ResolveFailed, WhoisOperationStage.Resolve, "WHOIS host resolution failed.")]
    [TestCase(WhoisExchangeStatus.ConnectFailed, WhoisOperationStage.Connect, "WHOIS connection failed.")]
    [TestCase(WhoisExchangeStatus.WriteFailed, WhoisOperationStage.Write, "WHOIS query write failed.")]
    [TestCase(WhoisExchangeStatus.ReadFailed, WhoisOperationStage.Read, "WHOIS response read failed.")]
    public void ConvertsEveryFailureStatusToTransportException(WhoisExchangeStatus status, WhoisOperationStage stage, string expectedMessage)
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(transport);
        WhoisRawResponse partial = new WhoisRawResponse();
        IOException error = new IOException("transport failed");
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.53");
        partial.ApprovedAddress = address;
        partial.Body = new byte[] { 112, 97, 114, 116 };
        partial.Status = status;
        partial.Stage = stage;
        partial.IsTruncated = status == WhoisExchangeStatus.ResponseLimitReached;
        partial.Error = error;
        transport.ExchangeHandler = (request) =>
        {
            partial.Request = request;
            return partial;
        };
        WhoisRequest original = CreateRequest();

        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Query(original))!;

        Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        Assert.That(exception.Stage, Is.EqualTo(stage));
        Assert.That(exception.Endpoint, Is.SameAs(partial.Request.Endpoint));
        Assert.That(exception.Endpoint, Is.Not.SameAs(original.Endpoint));
        Assert.That(exception.Endpoint.Host, Is.EqualTo("whois.failure.example"));
        Assert.That(exception.ApprovedAddress, Is.SameAs(address));
        Assert.That(exception.PartialResponse, Is.SameAs(partial));
        Assert.That(exception.PartialResponse.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
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

    private static WhoisRequest CreateRequest()
    {
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.failure.example";
        request.Endpoint.Port = 4343;
        request.QueryText = "example.test";
        return request;
    }
}
