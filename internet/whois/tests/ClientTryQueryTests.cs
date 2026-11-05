using whois;

namespace whois.Tests;

public class ClientTryQueryTests
{
    [Test]
    public void PerformsOneDirectExchangeWithAnIndependentRequestCopy()
    {
        FakeTransport transport = new FakeTransport();
        FakeServerSelector selector = new FakeServerSelector();
        WhoisClient client = CreateClient(selector, transport);
        WhoisRequest request = CreateCustomizedRequest();
        WhoisRawResponse expected = new WhoisRawResponse();
        transport.ExchangeHandler = (copiedRequest) =>
        {
            expected.Request = copiedRequest;
            expected.Body = new byte[] { 111, 107 };
            expected.Status = WhoisExchangeStatus.Complete;
            return expected;
        };

        WhoisRawResponse response = client.TryQuery(request);

        Assert.That(response, Is.SameAs(expected));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
        WhoisRequest copy = transport.Requests[0];
        Assert.That(copy, Is.Not.SameAs(request));
        Assert.That(copy.Endpoint, Is.Not.SameAs(request.Endpoint));
        Assert.That(copy.Encoding, Is.Not.SameAs(request.Encoding));
        Assert.That(copy.Encoding.RequestEncoding, Is.Not.SameAs(request.Encoding.RequestEncoding));
        Assert.That(copy.Encoding.ResponseEncoding, Is.Not.SameAs(request.Encoding.ResponseEncoding));
        Assert.That(copy.Timeouts, Is.Not.SameAs(request.Timeouts));
        Assert.That(copy.Limits, Is.Not.SameAs(request.Limits));
        Assert.That(copy.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(copy.Endpoint.Host, Is.EqualTo("whois.direct.example"));
        Assert.That(copy.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(copy.Endpoint.AdvertisedValue, Is.EqualTo("caller supplied"));
        Assert.That(copy.QueryText, Is.EqualTo("=Example.COM"));
        Assert.That(copy.Encoding.RequestEncoding.WebName, Is.EqualTo(request.Encoding.RequestEncoding.WebName));
        Assert.That(copy.Encoding.ResponseEncoding!.WebName, Is.EqualTo(request.Encoding.ResponseEncoding!.WebName));
        Assert.That(copy.Encoding.DetectByteOrderMark, Is.False);
        Assert.That(copy.Encoding.DecodingMode, Is.EqualTo(WhoisDecodingMode.ReplaceInvalidUtf8));
        Assert.That(copy.Timeouts.Resolve, Is.EqualTo(TimeSpan.FromSeconds(2)));
        Assert.That(copy.Timeouts.Connect, Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(copy.Timeouts.Write, Is.EqualTo(TimeSpan.FromSeconds(4)));
        Assert.That(copy.Timeouts.ReadIdle, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(copy.Timeouts.Overall, Is.EqualTo(TimeSpan.FromSeconds(20)));
        Assert.That(copy.Limits.MaximumEncodedQueryBytes, Is.EqualTo(101));
        Assert.That(copy.Limits.MaximumResponseBytes, Is.EqualTo(202));
        Assert.That(copy.Limits.MaximumReferralHops, Is.EqualTo(3));
        Assert.That(copy.Limits.MaximumLines, Is.EqualTo(404));
        Assert.That(copy.Limits.MaximumFields, Is.EqualTo(505));
        Assert.That(copy.CancellationToken, Is.EqualTo(request.CancellationToken));
        Assert.That(selector.Requests, Is.Empty);
    }

    [Test]
    public void LaterCallerMutationDoesNotChangeTransportRequestMetadata()
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(new FakeServerSelector(), transport);
        WhoisRequest request = CreateCustomizedRequest();
        transport.ExchangeHandler = (copiedRequest) =>
        {
            WhoisRawResponse result = new WhoisRawResponse();
            result.Request = copiedRequest;
            return result;
        };

        WhoisRawResponse response = client.TryQuery(request);
        request.Endpoint.Host = "changed.example";
        request.Encoding.DetectByteOrderMark = true;
        request.Timeouts.Resolve = TimeSpan.FromSeconds(99);
        request.Limits.MaximumResponseBytes = 999;

        Assert.That(response.Request.Endpoint.Host, Is.EqualTo("whois.direct.example"));
        Assert.That(response.Request.Encoding.DetectByteOrderMark, Is.False);
        Assert.That(response.Request.Timeouts.Resolve, Is.EqualTo(TimeSpan.FromSeconds(2)));
        Assert.That(response.Request.Limits.MaximumResponseBytes, Is.EqualTo(202));
    }

    [Test]
    public void ReturnsExpectedTransportFailureWithoutThrowingOrRetrying()
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(new FakeServerSelector(), transport);
        WhoisRawResponse failure = new WhoisRawResponse();
        IOException error = new IOException("read failed");
        failure.Status = WhoisExchangeStatus.ReadFailed;
        failure.Stage = WhoisOperationStage.Read;
        failure.Body = new byte[] { 112, 97, 114, 116 };
        failure.Error = error;
        transport.Result = failure;

        WhoisRawResponse response = client.TryQuery(CreateCustomizedRequest());

        Assert.That(response, Is.SameAs(failure));
        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(response.Error, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void RejectsNullRequestAndNestedConfigurationBeforeTransport()
    {
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(new FakeServerSelector(), transport);
        WhoisRequest missingEndpoint = CreateCustomizedRequest();
        WhoisRequest missingEncoding = CreateCustomizedRequest();
        WhoisRequest missingTimeouts = CreateCustomizedRequest();
        WhoisRequest missingLimits = CreateCustomizedRequest();
        missingEndpoint.Endpoint = null!;
        missingEncoding.Encoding = null!;
        missingTimeouts.Timeouts = null!;
        missingLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => client.TryQuery(null!));
        Assert.Throws<ArgumentNullException>(() => client.TryQuery(missingEndpoint));
        Assert.Throws<ArgumentNullException>(() => client.TryQuery(missingEncoding));
        Assert.Throws<ArgumentNullException>(() => client.TryQuery(missingTimeouts));
        Assert.Throws<ArgumentNullException>(() => client.TryQuery(missingLimits));
        Assert.That(transport.Requests, Is.Empty);
    }

    private static WhoisClient CreateClient(IWhoisServerSelector selector, IWhoisTransport transport)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
    }

    private static WhoisRequest CreateCustomizedRequest()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Scheme = WhoisReferralScheme.Whois;
        request.Endpoint.Host = "whois.direct.example";
        request.Endpoint.Port = 4343;
        request.Endpoint.AdvertisedValue = "caller supplied";
        request.Source = WhoisEndpointSource.Referral;
        request.QueryText = "=Example.COM";
        request.Encoding.RequestEncoding = new System.Text.UTF8Encoding(false, true);
        request.Encoding.ResponseEncoding = System.Text.Encoding.GetEncoding(28591);
        request.Encoding.DetectByteOrderMark = false;
        request.Encoding.DecodingMode = WhoisDecodingMode.ReplaceInvalidUtf8;
        request.Timeouts.Resolve = TimeSpan.FromSeconds(2);
        request.Timeouts.Connect = TimeSpan.FromSeconds(3);
        request.Timeouts.Write = TimeSpan.FromSeconds(4);
        request.Timeouts.ReadIdle = TimeSpan.FromSeconds(5);
        request.Timeouts.Overall = TimeSpan.FromSeconds(20);
        request.Limits.MaximumEncodedQueryBytes = 101;
        request.Limits.MaximumResponseBytes = 202;
        request.Limits.MaximumReferralHops = 3;
        request.Limits.MaximumLines = 404;
        request.Limits.MaximumFields = 505;
        request.CancellationToken = cancellation.Token;
        return request;
    }
}
