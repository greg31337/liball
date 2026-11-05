using whois;

namespace whois.Tests;

public class ApiExceptionTests
{
    [Test]
    public void QueryExceptionRetainsInvalidCallerInput()
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        FormatException inner = new FormatException("invalid port");
        endpoint.Host = "whois.example";
        endpoint.Port = 0;

        WhoisQueryException exception = new WhoisQueryException("Port must be between 1 and 65535.", "Endpoint.Port", "example.com", WhoisQueryKind.Domain, endpoint, inner);

        Assert.That(exception.Message, Is.EqualTo("Port must be between 1 and 65535."));
        Assert.That(exception.InvalidParameter, Is.EqualTo("Endpoint.Port"));
        Assert.That(exception.QueryText, Is.EqualTo("example.com"));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(exception.Endpoint, Is.SameAs(endpoint));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void ServerNotFoundExceptionRetainsNormalizedQuery()
    {
        WhoisQuery query = new WhoisQuery();
        query.OriginalText = "custom search";
        query.NormalizedText = "custom search";
        query.RoutingKey = "free:custom search";
        query.Kind = WhoisQueryKind.FreeForm;
        query.IsValid = true;

        WhoisServerNotFoundException exception = new WhoisServerNotFoundException("No automatic server is available.", query, null);

        Assert.That(exception.Message, Is.EqualTo("No automatic server is available."));
        Assert.That(exception.Query, Is.SameAs(query));
        Assert.That(exception.Query.OriginalText, Is.EqualTo("custom search"));
        Assert.That(exception.Query.NormalizedText, Is.EqualTo("custom search"));
        Assert.That(exception.Query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void TransportExceptionRetainsEndpointAddressAndPartialBytes()
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.43");
        WhoisRawResponse partial = new WhoisRawResponse();
        IOException inner = new IOException("connection reset");
        endpoint.Host = "whois.example";
        partial.ApprovedAddress = address;
        partial.Body = new byte[] { 112, 97, 114, 116 };
        partial.Status = WhoisExchangeStatus.ReadFailed;
        partial.Stage = WhoisOperationStage.Read;
        partial.Error = inner;

        WhoisTransportException exception = new WhoisTransportException("WHOIS response read failed.", WhoisOperationStage.Read, endpoint, address, partial, inner);

        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(exception.Endpoint, Is.SameAs(endpoint));
        Assert.That(exception.ApprovedAddress, Is.SameAs(address));
        Assert.That(exception.PartialResponse, Is.SameAs(partial));
        Assert.That(exception.PartialResponse.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void ResponseExceptionRetainsRawAndDecodedViews()
    {
        WhoisRawResponse raw = new WhoisRawResponse();
        WhoisTextResponse text = new WhoisTextResponse();
        FormatException inner = new FormatException("field limit reached");
        raw.Body = System.Text.Encoding.UTF8.GetBytes("name: value");
        text.RawResponse = raw;
        text.Text = "name: value";
        text.IsDecoded = true;

        WhoisResponseException exception = new WhoisResponseException("Generic response parsing failed.", WhoisOperationStage.Parse, raw, text, inner);

        Assert.That(exception.Stage, Is.EqualTo(WhoisOperationStage.Parse));
        Assert.That(exception.RawResponse, Is.SameAs(raw));
        Assert.That(exception.TextResponse, Is.SameAs(text));
        Assert.That(exception.TextResponse?.RawResponse, Is.SameAs(raw));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void ReferralExceptionRetainsRejectedReferralAndPartialHops()
    {
        WhoisReferral referral = new WhoisReferral();
        WhoisLookupHop hop = new WhoisLookupHop();
        WhoisLookupResult partial = new WhoisLookupResult();
        InvalidOperationException inner = new InvalidOperationException("loop detected");
        referral.AdvertisedValue = "whois.loop.example";
        referral.Scheme = WhoisReferralScheme.Whois;
        referral.Kind = WhoisReferralKind.Generic;
        referral.IsValid = true;
        hop.Referrals = new[] { referral };
        hop.SelectedReferral = referral;
        partial.Hops = new[] { hop };
        partial.FailedStage = WhoisOperationStage.Refer;
        partial.Error = inner;

        WhoisReferralException exception = new WhoisReferralException("Referral loop detected.", referral, "Loop", partial, inner);

        Assert.That(exception.Message, Is.EqualTo("Referral loop detected."));
        Assert.That(exception.Referral, Is.SameAs(referral));
        Assert.That(exception.Reason, Is.EqualTo("Loop"));
        Assert.That(exception.PartialResult, Is.SameAs(partial));
        Assert.That(exception.PartialResult.Hops, Has.Length.EqualTo(1));
        Assert.That(exception.PartialResult.Hops[0].SelectedReferral, Is.SameAs(referral));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }
}
