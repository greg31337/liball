namespace sip.Tests;

public class SipIdentityServiceTests
{
    const string Passport =
        "eyJhbGciOiJFUzI1NiJ9.eyJvcmlnIjp7InRuIjoiMTIwMjU1NTAxMDAifX0.c2ln";

    [Test]
    public void InspectsEveryIdentityFamilyAndPreservesExtensions()
    {
        SipMessage message = ParseMessage(RequestText(
            "Privacy: id;history;x-vendor\r\n"
            + "P-Asserted-Identity: <sip:alice@example.com>, <tel:+12025550100>\r\n"
            + "P-Preferred-Identity: <sip:alias@example.com>, <tel:+12025550101>\r\n"
            + "History-Info: <sip:old@example.com>;index=1.1;future=kept\r\n"
            + "Session-ID: 1234567890abcdef1234567890abcdef;remote=abcdef1234567890abcdef1234567890;future=kept\r\n"
            + "Geolocation: <cid:target@example.com>;routing-allowed=yes;future=kept\r\n"
            + "Geolocation-Routing: yes\r\n"
            + "Geolocation-Error: 424 Bad location;future=kept\r\n"
            + "Resource-Priority: dsn.flash, ets.0\r\n"
            + "Accept-Resource-Priority: dsn.flash-override, ets.1\r\n"
            + "Identity: " + Passport
            + ";info=<https://cert.example>;alg=ES256;ppt=shaken\r\n"
            + "Identity-Info: <https://legacy.example>;alg=rsa-sha1\r\n"));

        SipIdentityService service = new SipIdentityService();
        SipIdentityResult result = service.InspectIdentityMessage(message);
        result.Metadata!.AssertedIdentities[0].Address!.Uri!.User = "changed";
        SipIdentityResult repeated = service.InspectIdentityMessage(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(result.Metadata.Privacy,
                Is.EqualTo(new string[] { "id", "history", "x-vendor" }));
            Assert.That(result.Metadata.AssertedIdentities, Has.Length.EqualTo(2));
            Assert.That(result.Metadata.PreferredIdentities, Has.Length.EqualTo(2));
            Assert.That(result.Metadata.ConnectedIdentity!.Uri!.User,
                Is.EqualTo("alice"));
            Assert.That(result.Metadata.HistoryInformation[0].Parameters
                .Single(parameter => parameter.Name == "future").Value,
                Is.EqualTo("kept"));
            Assert.That(result.Metadata.SessionIdentifier!.Parameters,
                Has.Length.EqualTo(2));
            Assert.That(result.Metadata.Geolocations[0].Address!.Uri!.SchemeToken,
                Is.EqualTo("cid"));
            Assert.That(result.Metadata.GeolocationRouting, Is.EqualTo("yes"));
            Assert.That(result.Metadata.GeolocationErrors[0].Value,
                Is.EqualTo("424 Bad location;future=kept"));
            Assert.That(result.Metadata.ResourcePriorities, Has.Length.EqualTo(2));
            Assert.That(result.Metadata.AcceptedResourcePriorities,
                Has.Length.EqualTo(2));
            Assert.That(result.Metadata.IdentityHeaders.Select(header => header.Name),
                Is.EqualTo(new string[] { "Identity", "Identity-Info" }));
            Assert.That(result.Metadata.Passports,
                Is.EqualTo(new string[] { Passport }));
            Assert.That(repeated.Metadata!.AssertedIdentities[0].Address!.Uri!.User,
                Is.EqualTo("alice"));
        });
    }

    [Test]
    public void ResponseConnectedIdentityComesFromTo()
    {
        SipMessage response = ParseMessage(
            "SIP/2.0 180 Ringing\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-id\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: \"Connected Bob\" <sip:bob@device.example.com>;tag=to\r\n"
            + "Call-ID: identity@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");

        SipIdentityResult result =
            new SipIdentityService().InspectIdentityMessage(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Metadata!.ConnectedIdentity!.DisplayName,
                Is.EqualTo("Connected Bob"));
            Assert.That(result.Metadata.ConnectedIdentity.Uri!.Host,
                Is.EqualTo("device.example.com"));
        });
    }

    [TestCase("Privacy: none;id\r\n", "Privacy")]
    [TestCase("Privacy: id;ID\r\n", "Privacy")]
    [TestCase("History-Info: <sip:old@example.com>\r\n", "History-Info")]
    [TestCase("History-Info: <sip:old@example.com>;index=2\r\n", "History-Info")]
    [TestCase("Session-ID: abcdef;remote=abcdef\r\n", "Session-ID")]
    [TestCase("P-Asserted-Identity: <sip:a@example.com>, <sips:b@example.com>\r\n", "identity")]
    [TestCase("Geolocation-Routing: yes\r\nGeolocation-Routing: no\r\n", "Geolocation-Routing")]
    public void RejectsInvalidIdentityMetadata(string headers, string errorText)
    {
        SipIdentityResult result = new SipIdentityService()
            .InspectIdentityMessage(ParseMessage(RequestText(headers)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.ErrorMessage, Does.Contain(errorText).IgnoreCase);
        });
    }

    [Test]
    public void AddsPreservedMetadataToDetachedMessage()
    {
        SipIdentityService service = new SipIdentityService();
        SipIdentityMetadata metadata = service.InspectIdentityMessage(
            ParseMessage(RequestText(
                "Privacy: id;x-vendor\r\n"
                + "P-Asserted-Identity: <sip:alice@example.com>\r\n"
                + "P-Preferred-Identity: <sip:alias@example.com>, <tel:+12025550101>\r\n"
                + "History-Info: <sip:old@example.com>;index=1;future=kept\r\n"
                + "Session-ID: 1234567890abcdef1234567890abcdef;future=kept\r\n"
                + "Geolocation: <cid:target@example.com>;future=kept\r\n"
                + "Geolocation-Routing: vendor-route\r\n"
                + "Geolocation-Error: 424 Bad location;future=kept\r\n"
                + "Resource-Priority: dsn.flash\r\n"
                + "Accept-Resource-Priority: ets.0\r\n"
                + "Identity: " + Passport + ";alg=ES256\r\n")))
            .Metadata!;
        SipMessage target = ParseMessage(RequestText("X-Trace: keep\r\n"));

        SipIdentityResult result = service.AddIdentityHeaders(target, metadata);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.Privacy),
                Is.EqualTo(new string[] { "id;x-vendor" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.HistoryInfo)[0],
                Does.Contain("future=kept"));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.GeolocationRouting),
                Is.EqualTo(new string[] { "vendor-route" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.GeolocationError),
                Is.EqualTo(new string[] { "424 Bad location;future=kept" }));
            Assert.That(HeaderValues(result.Message!,
                SipHeaderNames.AcceptResourcePriority),
                Is.EqualTo(new string[] { "ets.0" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.Identity)[0],
                Does.StartWith(Passport));
            Assert.That(result.Metadata!.Passports,
                Is.EqualTo(new string[] { Passport }));
            Assert.That(HeaderValues(target, SipHeaderNames.Privacy), Is.Empty);
            Assert.That(HeaderValues(result.Message!, "X-Trace"),
                Is.EqualTo(new string[] { "keep" }));
        });
    }

    [Test]
    public void RejectsUnsafeOpaqueMetadataWithoutMutatingMessage()
    {
        SipMessage target = ParseMessage(RequestText("X-Trace: keep\r\n"));
        SipIdentityMetadata metadata = new SipIdentityMetadata();
        SipHeader unsafeHeader = new SipHeader();
        unsafeHeader.Name = SipHeaderNames.Identity;
        unsafeHeader.Value = Passport + "\r\nInjected: true";
        metadata.IdentityHeaders = new SipHeader[] { unsafeHeader };

        SipIdentityResult result = new SipIdentityService()
            .AddIdentityHeaders(target, metadata);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.Null);
            Assert.That(HeaderValues(target, SipHeaderNames.Identity), Is.Empty);
            Assert.That(HeaderValues(target, "Injected"), Is.Empty);
        });
    }

    [Test]
    public void PrivacyFilteringHonorsTrustTokensAndForwardingRules()
    {
        SipIdentityService service = new SipIdentityService();
        string fields =
            "P-Asserted-Identity: <sip:alice@example.com>\r\n"
            + "P-Preferred-Identity: <sip:alias@example.com>\r\n"
            + "History-Info: <sip:old@example.com>;index=1\r\n"
            + "Identity: " + Passport + ";alg=ES256\r\n";
        SipMessage selective = ParseMessage(RequestText(
            "Privacy: id;history\r\n" + fields));
        SipMessage headerPrivate = ParseMessage(RequestText(
            "Privacy: header\r\n" + fields));
        SipMessage none = ParseMessage(RequestText(
            "Privacy: none\r\n" + fields));
        SipMessage unsupportedCritical = ParseMessage(RequestText(
            "Privacy: critical;x-unknown\r\n" + fields));

        SipIdentityResult selectiveResult =
            service.ApplyIdentityPrivacy(selective, false);
        SipIdentityResult headerResult =
            service.ApplyIdentityPrivacy(headerPrivate, false);
        SipIdentityResult noneResult = service.ApplyIdentityPrivacy(none, false);
        SipIdentityResult trustedResult =
            service.ApplyIdentityPrivacy(selective, true);
        SipIdentityResult criticalResult =
            service.ApplyIdentityPrivacy(unsupportedCritical, false);

        Assert.Multiple(() =>
        {
            Assert.That(selectiveResult.Success, Is.True,
                selectiveResult.ErrorMessage);
            Assert.That(HeaderValues(selectiveResult.Message!,
                SipHeaderNames.PAssertedIdentity), Is.Empty);
            Assert.That(HeaderValues(selectiveResult.Message!,
                SipHeaderNames.PPreferredIdentity), Is.Empty);
            Assert.That(HeaderValues(selectiveResult.Message!,
                SipHeaderNames.HistoryInfo), Is.Empty);
            Assert.That(HeaderValues(selectiveResult.Message!,
                SipHeaderNames.Identity), Has.Length.EqualTo(1));
            Assert.That(HeaderValues(headerResult.Message!,
                SipHeaderNames.Identity), Is.Empty);
            Assert.That(HeaderValues(headerResult.Message!,
                SipHeaderNames.PAssertedIdentity), Is.Empty);
            Assert.That(HeaderValues(noneResult.Message!,
                SipHeaderNames.PAssertedIdentity), Has.Length.EqualTo(1));
            Assert.That(HeaderValues(noneResult.Message!,
                SipHeaderNames.PPreferredIdentity), Is.Empty);
            Assert.That(HeaderValues(noneResult.Message!,
                SipHeaderNames.HistoryInfo), Has.Length.EqualTo(1));
            Assert.That(HeaderValues(trustedResult.Message!,
                SipHeaderNames.PAssertedIdentity), Has.Length.EqualTo(1));
            Assert.That(HeaderValues(trustedResult.Message!,
                SipHeaderNames.PPreferredIdentity), Is.Empty);
            Assert.That(HeaderValues(selective,
                SipHeaderNames.PAssertedIdentity), Has.Length.EqualTo(1));
            Assert.That(criticalResult.Success, Is.False);
            Assert.That(criticalResult.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
        });
    }

    [Test]
    public void UpdatesConnectedIdentityAndRetainsDialogTag()
    {
        SipIdentityService service = new SipIdentityService();
        SipMessage request = ParseMessage(RequestText(""));
        SipNameAddress requestIdentity = ParseNameAddress(
            "From: \"New Alice\" <sips:alice@secure.example.com>");
        SipMessage response = ParseMessage(
            "SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-id\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>;tag=to\r\n"
            + "Call-ID: identity@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipNameAddress responseIdentity = ParseNameAddress(
            "To: \"Connected Bob\" <sip:bob@device.example.com>");

        SipIdentityResult requestResult = service.UpdateConnectedIdentity(
            request, requestIdentity);
        SipIdentityResult responseResult = service.UpdateConnectedIdentity(
            response, responseIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(requestResult.Success, Is.True, requestResult.ErrorMessage);
            Assert.That(requestResult.Message!.From!.DisplayName,
                Is.EqualTo("New Alice"));
            Assert.That(requestResult.Message.From.Parameters.Single().Value,
                Is.EqualTo("from"));
            Assert.That(responseResult.Message!.To!.DisplayName,
                Is.EqualTo("Connected Bob"));
            Assert.That(responseResult.Message.To.Parameters.Single().Value,
                Is.EqualTo("to"));
            Assert.That(request.From!.Uri!.Host, Is.EqualTo("example.com"));
            Assert.That(response.To!.Uri!.Host, Is.EqualTo("example.com"));
        });
    }

    [Test]
    public void VerificationPolicyReceivesDetachedOpaqueAssertions()
    {
        SipMessage message = ParseMessage(RequestText(
            "Identity: " + Passport
            + ";info=<https://cert.example>;alg=ES256;ppt=shaken\r\n"
            + "Identity-Info: <https://legacy.example>;alg=rsa-sha1\r\n"));
        RecordingIdentityPolicy policy = new RecordingIdentityPolicy();
        policy.Decision.Accepted = true;
        policy.Decision.Verified = true;
        policy.Decision.Identity = "+12025550100";

        SipIdentityVerificationResult result = new SipIdentityService()
            .VerifyMessageIdentity(message, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyEvaluated, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Verified, Is.True);
            Assert.That(result.Identity, Is.EqualTo("+12025550100"));
            Assert.That(policy.Request!.Passports,
                Is.EqualTo(new string[] { Passport }));
            Assert.That(policy.Request.IdentityHeaders, Has.Length.EqualTo(2));
            Assert.That(result.Metadata!.IdentityHeaders[0].Value,
                Does.StartWith(Passport));
            Assert.That(message.From!.Uri!.User, Is.EqualTo("alice"));
            Assert.That(HeaderValues(message, SipHeaderNames.Identity)[0],
                Does.StartWith(Passport));
        });
    }

    [Test]
    public void VerificationRequiresARequestPolicyAndDecision()
    {
        SipIdentityService service = new SipIdentityService();
        SipMessage request = ParseMessage(RequestText(""));
        SipMessage response = ParseMessage(
            "SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-id\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>;tag=to\r\n"
            + "Call-ID: identity@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");

        SipIdentityVerificationResult missingPolicy =
            service.VerifyMessageIdentity(request, null!);
        SipIdentityVerificationResult missingDecision =
            service.VerifyMessageIdentity(request, new NullIdentityPolicy());
        SipIdentityVerificationResult wrongKind =
            service.VerifyMessageIdentity(response,
                new RecordingIdentityPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(missingPolicy.PolicyEvaluated, Is.False);
            Assert.That(missingPolicy.StatusCode,
                Is.EqualTo(SipStatusCodes.InvalidIdentityHeader));
            Assert.That(missingDecision.PolicyEvaluated, Is.False);
            Assert.That(wrongKind.PolicyEvaluated, Is.False);
            Assert.That(wrongKind.Reason, Does.Contain("request"));
        });
    }

    [Test]
    public void VerificationReceivesFullAndCompactPassportsInWireOrder()
    {
        const string FullPassport = "header.payload.signature";
        const string CompactPassport = "..compact-signature";
        SipMessage message = ParseMessage(RequestText(
            "Identity: " + FullPassport + ";info=<https://full.example>\r\n"
            + "y: header..missing-payload;info=<https://invalid.example>\r\n"
            + "Identity: " + CompactPassport
            + ";info=<https://compact.example>\r\n"));
        RecordingIdentityPolicy policy = new RecordingIdentityPolicy();
        policy.Decision.Accepted = true;

        SipIdentityVerificationResult result = new SipIdentityService()
            .VerifyMessageIdentity(message, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyEvaluated, Is.True);
            Assert.That(policy.Request!.IdentityHeaders, Has.Length.EqualTo(3));
            Assert.That(policy.Request.Passports,
                Is.EqualTo(new string[] { FullPassport, CompactPassport }));
            Assert.That(result.Metadata!.Passports,
                Is.EqualTo(new string[] { FullPassport, CompactPassport }));
            Assert.That(HeaderValues(message, SipHeaderNames.Identity),
                Has.Length.EqualTo(2));
            Assert.That(HeaderValues(message, SipHeaderNames.CompactIdentity),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void UnknownHeadersSurviveIdentityInspectionAdditionAndPrivacy()
    {
        SipIdentityService service = new SipIdentityService();
        SipMessage source = ParseMessage(RequestText(
            "Privacy: id\r\n"
            + "P-Asserted-Identity: <sip:alice@example.com>\r\n"
            + "X-Identity-Future: opaque-value\r\n"));
        SipIdentityMetadata metadata = service.InspectIdentityMessage(source)
            .Metadata!;

        SipIdentityResult added = service.AddIdentityHeaders(
            ParseMessage(RequestText("X-Identity-Future: retained\r\n")), metadata);
        SipIdentityResult filtered = service.ApplyIdentityPrivacy(source, false);

        Assert.Multiple(() =>
        {
            Assert.That(added.Success, Is.True, added.ErrorMessage);
            Assert.That(HeaderValues(added.Message!, "X-Identity-Future"),
                Is.EqualTo(new string[] { "retained" }));
            Assert.That(filtered.Success, Is.True, filtered.ErrorMessage);
            Assert.That(HeaderValues(filtered.Message!, "X-Identity-Future"),
                Is.EqualTo(new string[] { "opaque-value" }));
            Assert.That(HeaderValues(filtered.Message!,
                SipHeaderNames.PAssertedIdentity), Is.Empty);
        });
    }

    static SipNameAddress ParseNameAddress(string text)
    {
        SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.NameAddress!;
    }

    static SipMessage ParseMessage(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string RequestText(string extraHeaders)
    {
        return "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-id\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: identity@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + extraHeaders
            + "Content-Length: 0\r\n\r\n";
    }

    static string[] HeaderValues(SipMessage message, string name)
    {
        return message.Headers
            .Where(header => string.Equals(header.Name, name,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value!)
            .ToArray();
    }

    sealed class RecordingIdentityPolicy : ISipIdentityVerificationPolicy
    {
        public SipIdentityVerificationRequest? Request;
        public SipIdentityVerificationResult Decision =
            new SipIdentityVerificationResult();

        public SipIdentityVerificationResult VerifyIdentityAssertion(
            SipIdentityVerificationRequest request)
        {
            Request = request;
            request.Message!.From!.Uri!.User = "mutated";
            request.IdentityHeaders[0].Value = "mutated";
            return Decision;
        }
    }

    sealed class NullIdentityPolicy : ISipIdentityVerificationPolicy
    {
        public SipIdentityVerificationResult VerifyIdentityAssertion(
            SipIdentityVerificationRequest request)
        {
            return null!;
        }
    }
}
