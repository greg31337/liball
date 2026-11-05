using whois;

namespace whois.Tests;

public class DirectQueryBehaviorTests
{
    [Test]
    public void DirectOperationsPreserveExactInputAndBypassTheAutomaticPipeline()
    {
        ForbiddenPipeline forbidden = new ForbiddenPipeline();
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(forbidden, transport);
        WhoisRequest request = CreateCustomRequest();
        transport.ExchangeHandler = (copy) =>
        {
            WhoisRawResponse complete = new WhoisRawResponse();
            complete.Request = copy;
            complete.Body = new byte[] { 111, 107 };
            complete.Status = WhoisExchangeStatus.Complete;
            return complete;
        };

        WhoisRawResponse statusBearing = client.TryQuery(request);
        WhoisRawResponse throwing = client.Query(request);

        Assert.That(statusBearing.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(throwing.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
        AssertDirectCopy(transport.Requests[0], request);
        AssertDirectCopy(transport.Requests[1], request);
    }

    [TestCase(WhoisExchangeStatus.Cancelled, WhoisOperationStage.Resolve)]
    [TestCase(WhoisExchangeStatus.TimedOut, WhoisOperationStage.Connect)]
    [TestCase(WhoisExchangeStatus.ResponseLimitReached, WhoisOperationStage.Read)]
    [TestCase(WhoisExchangeStatus.ResolveFailed, WhoisOperationStage.Resolve)]
    [TestCase(WhoisExchangeStatus.ConnectFailed, WhoisOperationStage.Connect)]
    [TestCase(WhoisExchangeStatus.WriteFailed, WhoisOperationStage.Write)]
    [TestCase(WhoisExchangeStatus.ReadFailed, WhoisOperationStage.Read)]
    public void TryQueryReturnsAndQueryThrowsForEveryExpectedFailure(WhoisExchangeStatus status, WhoisOperationStage stage)
    {
        ForbiddenPipeline forbidden = new ForbiddenPipeline();
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(forbidden, transport);
        InvalidOperationException error = new InvalidOperationException("expected transport failure");
        WhoisRawResponse partial = new WhoisRawResponse();
        partial.Status = status;
        partial.Stage = stage;
        partial.Body = new byte[] { 112, 97, 114, 116 };
        partial.IsTruncated = status == WhoisExchangeStatus.ResponseLimitReached;
        partial.Error = error;
        transport.ExchangeHandler = (request) =>
        {
            partial.Request = request;
            return partial;
        };
        WhoisRequest directRequest = CreateCustomRequest();

        WhoisRawResponse statusBearing = client.TryQuery(directRequest);
        WhoisTransportException exception = Assert.Throws<WhoisTransportException>(() => client.Query(directRequest))!;

        Assert.That(statusBearing, Is.SameAs(partial));
        Assert.That(statusBearing.Status, Is.EqualTo(status));
        Assert.That(statusBearing.Stage, Is.EqualTo(stage));
        Assert.That(statusBearing.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(statusBearing.Error, Is.SameAs(error));
        Assert.That(exception.Stage, Is.EqualTo(stage));
        Assert.That(exception.PartialResponse, Is.SameAs(partial));
        Assert.That(exception.InnerException, Is.SameAs(error));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
        Assert.That(transport.Requests[0].QueryText, Is.EqualTo(directRequest.QueryText));
        Assert.That(transport.Requests[1].QueryText, Is.EqualTo(directRequest.QueryText));
    }

    private static void AssertDirectCopy(WhoisRequest copy, WhoisRequest original)
    {
        Assert.That(copy, Is.Not.SameAs(original));
        Assert.That(copy.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(copy.QueryText, Is.EqualTo("  handle = Exact/Case:Value  "));
        Assert.That(copy.Endpoint, Is.Not.SameAs(original.Endpoint));
        Assert.That(copy.Endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(copy.Endpoint.Host, Is.EqualTo("Whois.Custom.Example"));
        Assert.That(copy.Endpoint.Port, Is.EqualTo(1043));
        Assert.That(copy.Endpoint.AdvertisedValue, Is.EqualTo("custom direct endpoint"));
        Assert.That(copy.Encoding, Is.Not.SameAs(original.Encoding));
        Assert.That(copy.Encoding.RequestEncoding, Is.Not.SameAs(original.Encoding.RequestEncoding));
        Assert.That(copy.Encoding.RequestEncoding.WebName, Is.EqualTo("utf-32"));
        Assert.That(copy.Encoding.ResponseEncoding!.WebName, Is.EqualTo("utf-16"));
        Assert.That(copy.Encoding.DetectByteOrderMark, Is.False);
        Assert.That(copy.Encoding.DecodingMode, Is.EqualTo(WhoisDecodingMode.Latin1Fallback));
        Assert.That(copy.Timeouts, Is.Not.SameAs(original.Timeouts));
        Assert.That(copy.Timeouts.Resolve, Is.EqualTo(TimeSpan.FromSeconds(6)));
        Assert.That(copy.Timeouts.Connect, Is.EqualTo(TimeSpan.FromSeconds(7)));
        Assert.That(copy.Timeouts.Write, Is.EqualTo(TimeSpan.FromSeconds(8)));
        Assert.That(copy.Timeouts.ReadIdle, Is.EqualTo(TimeSpan.FromSeconds(9)));
        Assert.That(copy.Timeouts.Overall, Is.EqualTo(TimeSpan.FromSeconds(40)));
    }

    private static WhoisClient CreateClient(ForbiddenPipeline forbidden, IWhoisTransport transport)
    {
        return new WhoisClient(forbidden, forbidden, forbidden, transport, forbidden, forbidden, forbidden);
    }

    private static WhoisRequest CreateCustomRequest()
    {
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Scheme = WhoisReferralScheme.Whois;
        request.Endpoint.Host = "Whois.Custom.Example";
        request.Endpoint.Port = 1043;
        request.Endpoint.AdvertisedValue = "custom direct endpoint";
        request.Source = WhoisEndpointSource.InitialSelection;
        request.QueryText = "  handle = Exact/Case:Value  ";
        request.Encoding.RequestEncoding = new System.Text.UTF32Encoding(false, false, true);
        request.Encoding.ResponseEncoding = new System.Text.UnicodeEncoding(false, true, true);
        request.Encoding.DetectByteOrderMark = false;
        request.Encoding.DecodingMode = WhoisDecodingMode.Latin1Fallback;
        request.Timeouts.Resolve = TimeSpan.FromSeconds(6);
        request.Timeouts.Connect = TimeSpan.FromSeconds(7);
        request.Timeouts.Write = TimeSpan.FromSeconds(8);
        request.Timeouts.ReadIdle = TimeSpan.FromSeconds(9);
        request.Timeouts.Overall = TimeSpan.FromSeconds(40);
        return request;
    }

    private class ForbiddenPipeline : IWhoisQueryClassifier, IWhoisQueryFormatter, IWhoisServerSelector, IWhoisResponseDecoder, IWhoisResponseParser, IWhoisReferralParser
    {
        public WhoisQuery Classify(WhoisLookupRequest request)
        {
            throw new InvalidOperationException("Direct queries must not classify input.");
        }

        public string FormatQuery(WhoisQuery query, WhoisServerSelection selection)
        {
            throw new InvalidOperationException("Direct queries must not format input.");
        }

        public WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request)
        {
            throw new InvalidOperationException("Direct queries must not select a server.");
        }

        public WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options)
        {
            throw new InvalidOperationException("Direct queries must not decode responses.");
        }

        public WhoisParsedResponse ParseResponse(WhoisTextResponse response, WhoisLimits limits)
        {
            throw new InvalidOperationException("Direct queries must not parse responses.");
        }

        public WhoisField[] FindFields(WhoisParsedResponse response, string name)
        {
            throw new InvalidOperationException("Direct queries must not inspect fields.");
        }

        public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
        {
            throw new InvalidOperationException("Direct queries must not parse referrals.");
        }
    }
}
