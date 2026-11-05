namespace sip.Tests;

public class SipRequestRoutingTests
{
    [Test]
    public void AddsOrDecrementsMaxForwardsAndRejectsExhaustion()
    {
        SipMessage existing = ParseRequest("sip:service@example.com", "branch-one", 2, "");
        SipMessage missing = ParseRequest("sip:service@example.com", "branch-two", 70, "");
        missing.Headers = missing.Headers.Where(header =>
            !header.Name!.Equals(SipHeaderNames.MaxForwards,
                System.StringComparison.OrdinalIgnoreCase)).ToArray();
        SipMessage exhausted = ParseRequest("sip:service@example.com", "branch-three", 0, "");

        int decremented = SipRequestRouting.PrepareMaxForwards(existing);
        int inserted = SipRequestRouting.PrepareMaxForwards(missing);
        SipProtocolException error = Assert.Throws<SipProtocolException>(() =>
            SipRequestRouting.PrepareMaxForwards(exhausted))!;

        Assert.Multiple(() =>
        {
            Assert.That(decremented, Is.EqualTo(1));
            Assert.That(HeaderValue(existing, SipHeaderNames.MaxForwards), Is.EqualTo("1"));
            Assert.That(inserted, Is.EqualTo(SipDefaults.MaxForwards));
            Assert.That(HeaderValue(missing, SipHeaderNames.MaxForwards), Is.EqualTo("70"));
            Assert.That(missing.Headers[1].Name, Is.EqualTo(SipHeaderNames.MaxForwards));
            Assert.That(error.StatusCode, Is.EqualTo(SipStatusCodes.TooManyHops));
        });
    }

    [Test]
    public void RejectsRepeatedOrInvalidMaxForwards()
    {
        SipMessage repeated = ParseRequest("sip:service@example.com", "branch-one", 5, "");
        repeated.Headers = repeated.Headers.Concat(new SipHeader[]
        {
            Header(SipHeaderNames.MaxForwards, "4")
        }).ToArray();
        SipMessage invalid = ParseRequest("sip:service@example.com", "branch-two", 5, "");
        invalid.Headers.Single(header => header.Name == SipHeaderNames.MaxForwards).Value = "bad";

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<SipProtocolException>(() =>
                SipRequestRouting.PrepareMaxForwards(repeated))!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(Assert.Throws<SipProtocolException>(() =>
                SipRequestRouting.PrepareMaxForwards(invalid))!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
        });
    }

    [Test]
    public void DistinguishesLoopsFromRoutingSpirals()
    {
        SipMessage original = ParseRequest("sip:service@EXAMPLE.com", "branch-loop", 70,
            "Proxy-Require: foo\r\n" + AuthorizationHeader());
        SipMessage loop = ParseRequest("sip:service@example.com", "branch-loop", 12,
            "Proxy-Require: foo\r\n" + AuthorizationHeader());
        SipMessage spiral = ParseRequest("sip:service@next.example.com", "branch-loop", 11,
            "Proxy-Require: foo\r\n" + AuthorizationHeader());
        SipMessage changedPolicy = ParseRequest("sip:service@example.com", "branch-loop", 10,
            "Proxy-Require: bar\r\n" + AuthorizationHeader());
        SipMessage changedRoute = ParseRequest("sip:service@example.com", "branch-loop", 9,
            "Route: <sip:next.example.com;lr>\r\n"
            + "Proxy-Require: foo\r\n" + AuthorizationHeader());

        string fingerprint = SipRequestRouting.CreateLoopFingerprint(original);

        Assert.Multiple(() =>
        {
            Assert.That(fingerprint, Has.Length.EqualTo(64));
            Assert.That(SipRequestRouting.RequestsFormLoop(original, loop), Is.True);
            Assert.That(SipRequestRouting.RequestsFormSpiral(original, loop), Is.False);
            Assert.That(SipRequestRouting.RequestsFormLoop(original, spiral), Is.False);
            Assert.That(SipRequestRouting.RequestsFormSpiral(original, spiral), Is.True);
            Assert.That(SipRequestRouting.RequestsFormLoop(original, changedPolicy), Is.False);
            Assert.That(SipRequestRouting.RequestsFormLoop(original, changedRoute), Is.False);
            Assert.That(SipRequestRouting.RequestsFormSpiral(original, changedRoute), Is.True);
        });
    }

    [Test]
    public void DetectsMergedRequestsButNotRetransmissions()
    {
        SipMessage existing = ParseRequest("sip:one@example.com", "branch-original", 70, "");
        SipMessage merged = ParseRequest("sip:two@example.com", "branch-fork", 69, "");
        SipMessage retransmission = ParseRequest("sip:two@example.com", "branch-original", 68, "");
        SipMessage dialogRequest = ParseRequest("sip:one@example.com", "branch-dialog", 70, "");
        dialogRequest.To!.Parameters = new SipUriParameter[] { Parameter("tag", "remote-tag") };

        Assert.Multiple(() =>
        {
            Assert.That(SipRequestRouting.CreateMergedRequestKey(existing),
                Is.EqualTo(SipRequestRouting.CreateMergedRequestKey(merged)));
            Assert.That(SipRequestRouting.IsMergedRequest(existing, merged), Is.True);
            Assert.That(SipRequestRouting.IsMergedRequest(existing, retransmission), Is.False);
            Assert.That(SipRequestRouting.IsMergedRequest(dialogRequest, merged), Is.False);
            Assert.Throws<System.ArgumentException>(() =>
                SipRequestRouting.CreateMergedRequestKey(dialogRequest));
        });
    }

    [Test]
    public void RewritesRequestTargetWithAnIndependentHeaderFreeCopy()
    {
        SipMessage request = ParseRequest("sip:old@example.com", "branch-rewrite", 70, "");
        SipUri target = ParseUri("sip:new@target.example.com;transport=tcp?Subject=hello");

        SipRequestRouting.RewriteRequestTarget(request, target);
        target.Host = "changed.example.com";
        target.Parameters[0].Value = "udp";

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestUri!.Host, Is.EqualTo("target.example.com"));
            Assert.That(request.RequestUri.Parameters[0].Value, Is.EqualTo("tcp"));
            Assert.That(request.RequestUri.Headers, Is.Empty);
            Assert.That(request.RequestUri, Is.Not.SameAs(target));
            Assert.That(request.OriginalBytes, Is.Empty);
        });
    }

    [Test]
    public void PreventsSecureTargetDowngradeAndValidatesRoutingIdentity()
    {
        SipMessage secure = ParseRequest("sips:service@example.com", "branch-secure", 70, "");
        SipMessage missingFromTag = ParseRequest("sip:service@example.com", "branch-tag", 70, "");
        missingFromTag.From!.Parameters = new SipUriParameter[0];
        SipMessage badBranch = ParseRequest("sip:service@example.com", "branch-valid", 70, "");
        badBranch.ViaValues[0].Branch = "invalid branch";

        Assert.Multiple(() =>
        {
            Assert.Throws<SipProtocolException>(() => SipRequestRouting.RewriteRequestTarget(
                secure, ParseUri("sip:insecure.example.com")));
            Assert.Throws<System.ArgumentException>(() =>
                SipRequestRouting.ValidateRoutingRequest(missingFromTag));
            Assert.Throws<System.ArgumentException>(() =>
                SipRequestRouting.ValidateRoutingRequest(badBranch));
        });
    }

    static SipMessage ParseRequest(string uri, string branch, int maxForwards,
        string additionalHeaders)
    {
        return ParseMessage("OPTIONS " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-" + branch + "\r\n"
            + "Max-Forwards: " + maxForwards.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: routing-call@example.com\r\n"
            + "CSeq: 41 OPTIONS\r\n"
            + additionalHeaders
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ParseMessage(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Uri!;
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static SipUriParameter Parameter(string name, string value)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = true;
        return parameter;
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        return message.Headers.Single(header => header.Name == name).Value;
    }

    static string AuthorizationHeader()
    {
        return "Proxy-Authorization: Digest username=\"a\",realm=\"r\",nonce=\"n\","
            + "uri=\"sip:service@example.com\","
            + "response=\"0123456789abcdef0123456789abcdef\"\r\n";
    }
}
