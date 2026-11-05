namespace sip.Tests;

public class SipStatelessTransactionTests
{
    [Test]
    public void StatelessSendReturnsDetachedRequestsAndResponsesWithoutCreatingState()
    {
        SipMessage request = Request("z9hG4bK-send", 70);
        SipMessage response = Response(true);

        SipStatelessResult sentRequest = SipStatelessTransaction.SendRequest(request);
        SipStatelessResult sentResponse = SipStatelessTransaction.SendResponse(response);
        request.RequestUri!.Host = "changed.example.com";
        response.StatusCode = 500;

        Assert.Multiple(() =>
        {
            Assert.That(sentRequest.Success, Is.True);
            Assert.That(sentRequest.Forwarded, Is.False);
            Assert.That(sentRequest.OutboundMessages.Single().RequestUri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(sentResponse.Success, Is.True);
            Assert.That(sentResponse.OutboundMessages.Single().StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void StatelessRequestForwardingDecrementsCopyAndLeavesInputUntouched()
    {
        SipMessage request = Request("z9hG4bK-forward", 3);

        SipStatelessResult result = SipStatelessTransaction.ForwardRequest(request);
        result.OutboundMessages[0].RequestUri!.Host = "changed.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Forwarded, Is.True);
            Assert.That(result.DeliveredLocally, Is.False);
            Assert.That(MaxForwards(result.OutboundMessages.Single()), Is.EqualTo("2"));
            Assert.That(MaxForwards(request), Is.EqualTo("3"));
            Assert.That(request.RequestUri!.Host, Is.EqualTo("service.example.com"));
        });
    }

    [Test]
    public void ExhaustedStatelessRequestIsRejectedWithTooManyHops()
    {
        SipStatelessResult result = SipStatelessTransaction.ForwardRequest(
            Request("z9hG4bK-exhausted", 0));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejected, Is.True);
            Assert.That(result.Malformed, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.TooManyHops));
            Assert.That(result.OutboundMessages, Is.Empty);
        });
    }

    [Test]
    public void StatelessResponseForwardingRemovesOnlyMatchingTopViaFromCopy()
    {
        SipMessage response = Response(true);

        SipStatelessResult result = SipStatelessTransaction.ForwardResponse(response,
            "proxy.example.com", 5060, "z9hG4bK-proxy");
        SipMessage forwarded = result.OutboundMessages.Single();
        forwarded.ViaValues[0].Host = "changed.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Forwarded, Is.True);
            Assert.That(result.DeliveredLocally, Is.False);
            Assert.That(forwarded.ViaValues.Length, Is.EqualTo(1));
            Assert.That(forwarded.ViaValues[0].Branch, Is.EqualTo("z9hG4bK-client"));
            Assert.That(HeaderValues(forwarded, SipHeaderNames.Via).Length, Is.EqualTo(1));
            Assert.That(response.ViaValues.Length, Is.EqualTo(2));
            Assert.That(response.ViaValues[0].Host, Is.EqualTo("proxy.example.com"));
        });
    }

    [Test]
    public void RemovingLastResponseViaReturnsMessageForLocalDelivery()
    {
        SipMessage response = Response(false);

        SipStatelessResult result = SipStatelessTransaction.ForwardResponse(response,
            "proxy.example.com", 5060, "z9hG4bK-proxy");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Forwarded, Is.False);
            Assert.That(result.DeliveredLocally, Is.True);
            Assert.That(result.OutboundMessages, Is.Empty);
            Assert.That(result.DeliveredMessages.Single().ViaValues, Is.Empty);
            Assert.That(response.ViaValues.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void MismatchedTopViaAndMalformedMessagesReturnBadRequestResults()
    {
        SipMessage malformedRequest = Request("z9hG4bK-malformed", 4);
        malformedRequest.Headers = new SipHeader[0];
        SipMessage malformedResponse = Response(true);
        malformedResponse.Headers = new SipHeader[0];

        SipStatelessResult badRequest = SipStatelessTransaction.SendRequest(malformedRequest);
        SipStatelessResult wrongKind = SipStatelessTransaction.SendRequest(Response(true));
        SipStatelessResult badResponse = SipStatelessTransaction.SendResponse(malformedResponse);
        SipStatelessResult mismatch = SipStatelessTransaction.ForwardResponse(Response(true),
            "other.example.com", 5060, "z9hG4bK-proxy");
        SipStatelessResult badIdentity = SipStatelessTransaction.ForwardResponse(Response(true),
            "", 5060, "z9hG4bK-proxy");

        Assert.Multiple(() =>
        {
            AssertBadRequest(badRequest);
            AssertBadRequest(wrongKind);
            AssertBadRequest(badResponse);
            AssertBadRequest(mismatch);
            AssertBadRequest(badIdentity);
        });
    }

    static void AssertBadRequest(SipStatelessResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Malformed, Is.True);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
        Assert.That(result.OutboundMessages, Is.Empty);
    }

    static SipMessage Request(string branch, int maxForwards)
    {
        return Parse("OPTIONS sip:service@service.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: " + maxForwards.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: stateless-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Response(bool secondVia)
    {
        string previous = secondVia
            ? "Via: SIP/2.0/UDP client.example.com:5070;branch=z9hG4bK-client\r\n" : "";
        return Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP proxy.example.com:5060;branch=z9hG4bK-proxy\r\n"
            + previous
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>;tag=to-tag\r\n"
            + "Call-ID: stateless-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static string MaxForwards(SipMessage message)
    {
        return HeaderValues(message, SipHeaderNames.MaxForwards).Single();
    }

    static string[] HeaderValues(SipMessage message, string name)
    {
        return message.Headers.Where(header =>
            string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value ?? "").ToArray();
    }
}
