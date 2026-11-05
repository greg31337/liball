namespace sip.Tests;

public class SipTransactionAdmissionTests
{
    [Test]
    public void AdmissionStoresDetachedRequestAndTracksBoundedCount()
    {
        SipTransactionRegistry registry = Registry(2, true);
        SipMessage request = Request("sip:one@example.com", "z9hG4bK-one", 41,
            "OPTIONS", null);

        SipTransactionAdmissionResult result = registry.AdmitTransaction("one", request,
            SipTransactionRole.Server);
        request.RequestUri!.Host = "changed.example.com";
        SipMessage stored = registry.FindTransactionRequest("one")!;
        stored.RequestUri!.Host = "also-changed.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Existing, Is.False);
            Assert.That(result.StatusCode, Is.Zero);
            Assert.That(result.TransactionId, Is.EqualTo("one"));
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(1));
            Assert.That(registry.FindTransactionRequest("one")!.RequestUri!.Host,
                Is.EqualTo("example.com"));
        });
    }

    [Test]
    public void RetransmissionReturnsExistingTransactionBeforeCapacityCheck()
    {
        SipTransactionRegistry registry = Registry(1, true);
        SipMessage original = Request("sip:one@example.com", "z9hG4bK-same", 41,
            "OPTIONS", null);
        registry.AdmitTransaction("original", original, SipTransactionRole.Server);
        SipMessage retransmission = Request("sip:different@example.com", "z9hG4bK-same", 41,
            "OPTIONS", null);

        SipTransactionAdmissionResult result = registry.AdmitTransaction("other-id",
            retransmission, SipTransactionRole.Server);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Existing, Is.True);
            Assert.That(result.Overloaded, Is.False);
            Assert.That(result.TransactionId, Is.EqualTo("original"));
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ForkedRequestWithSameCoreIdentityIsRejectedAsMerged()
    {
        SipTransactionRegistry registry = Registry(4, true);
        registry.AdmitTransaction("original",
            Request("sip:one@example.com", "z9hG4bK-original", 41, "OPTIONS", null),
            SipTransactionRole.Server);

        SipTransactionAdmissionResult result = registry.AdmitTransaction("fork",
            Request("sip:two@example.com", "z9hG4bK-fork", 41, "OPTIONS", null),
            SipTransactionRole.Server);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Merged, Is.True);
            Assert.That(result.Malformed, Is.False);
            Assert.That(result.Overloaded, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.LoopDetected));
            Assert.That(result.TransactionId, Is.EqualTo("original"));
            Assert.That(result.ErrorMessage, Does.Contain("merged"));
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void DialogAndDifferentCoreIdentityRequestsAreNotMerged()
    {
        SipTransactionRegistry registry = Registry(8, true);
        registry.AdmitTransaction("original",
            Request("sip:one@example.com", "z9hG4bK-original", 41, "OPTIONS", null),
            SipTransactionRole.Server);

        SipTransactionAdmissionResult differentCseq = registry.AdmitTransaction("cseq",
            Request("sip:two@example.com", "z9hG4bK-cseq", 42, "OPTIONS", null),
            SipTransactionRole.Server);
        SipTransactionAdmissionResult differentMethod = registry.AdmitTransaction("method",
            Request("sip:two@example.com", "z9hG4bK-method", 41, "BYE", null),
            SipTransactionRole.Server);
        SipTransactionAdmissionResult dialog = registry.AdmitTransaction("dialog",
            Request("sip:two@example.com", "z9hG4bK-dialog", 41, "OPTIONS", "remote-tag"),
            SipTransactionRole.Server);

        Assert.Multiple(() =>
        {
            Assert.That(differentCseq.Accepted, Is.True);
            Assert.That(differentMethod.Accepted, Is.True);
            Assert.That(dialog.Accepted, Is.True);
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void CapacityReturnsOverloadAndRemovalAllowsLaterAdmission()
    {
        SipTransactionRegistry registry = Registry(1, true);
        registry.AdmitTransaction("one",
            Request("sip:one@example.com", "z9hG4bK-one", 41, "OPTIONS", null),
            SipTransactionRole.Client);

        SipTransactionAdmissionResult overloaded = registry.AdmitTransaction("two",
            Request("sip:two@example.com", "z9hG4bK-two", 42, "OPTIONS", null),
            SipTransactionRole.Client);
        bool removed = registry.RemoveTransaction("one");
        SipTransactionAdmissionResult accepted = registry.AdmitTransaction("two",
            Request("sip:two@example.com", "z9hG4bK-two", 42, "OPTIONS", null),
            SipTransactionRole.Client);

        Assert.Multiple(() =>
        {
            Assert.That(overloaded.Overloaded, Is.True);
            Assert.That(overloaded.Accepted, Is.False);
            Assert.That(overloaded.StatusCode,
                Is.EqualTo(SipStatusCodes.ServiceUnavailable));
            Assert.That(overloaded.ErrorMessage, Does.Contain("limit"));
            Assert.That(removed, Is.True);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(1));
            Assert.That(registry.RemoveTransaction("missing"), Is.False);
        });
    }

    [Test]
    public void MalformedInputsAndConflictingIdentifiersReturnBadRequest()
    {
        SipTransactionRegistry registry = Registry(4, true);
        SipMessage malformed = Request("sip:one@example.com", "z9hG4bK-one", 41,
            "OPTIONS", null);
        malformed.Headers = new SipHeader[0];
        SipMessage ack = Request("sip:one@example.com", "z9hG4bK-ack", 41,
            "ACK", null);
        registry.AdmitTransaction("assigned",
            Request("sip:one@example.com", "z9hG4bK-assigned", 41, "OPTIONS", null),
            SipTransactionRole.Server);

        SipTransactionAdmissionResult nullRequest = registry.AdmitTransaction("null", null,
            SipTransactionRole.Server);
        SipTransactionAdmissionResult invalidId = registry.AdmitTransaction("", ack,
            SipTransactionRole.Server);
        SipTransactionAdmissionResult invalidRole = registry.AdmitTransaction("role", ack,
            SipTransactionRole.Unknown);
        SipTransactionAdmissionResult malformedRequest = registry.AdmitTransaction("bad",
            malformed, SipTransactionRole.Server);
        SipTransactionAdmissionResult ackRequest = registry.AdmitTransaction("ack", ack,
            SipTransactionRole.Server);
        SipTransactionAdmissionResult collision = registry.AdmitTransaction("assigned",
            Request("sip:other@example.com", "z9hG4bK-other", 42, "OPTIONS", null),
            SipTransactionRole.Server);

        Assert.Multiple(() =>
        {
            AssertBadRequest(nullRequest);
            AssertBadRequest(invalidId);
            AssertBadRequest(invalidRole);
            AssertBadRequest(malformedRequest);
            AssertBadRequest(ackRequest);
            AssertBadRequest(collision);
            Assert.That(registry.GetTransactionCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void LegacyMatchingPolicyRejectsNonCookieBranchWithoutMutatingCount()
    {
        SipTransactionRegistry registry = Registry(2, false);

        SipTransactionAdmissionResult rejected = registry.AdmitTransaction("legacy",
            Request("sip:one@example.com", "legacy-branch", 41, "OPTIONS", null),
            SipTransactionRole.Server);

        Assert.Multiple(() =>
        {
            AssertBadRequest(rejected);
            Assert.That(rejected.ErrorMessage, Does.Contain("magic-cookie"));
            Assert.That(registry.GetTransactionCount(), Is.Zero);
        });
    }

    [Test]
    public void InvalidRegistryLimitIsRejected()
    {
        SipTransactionOptions options = new SipTransactionOptions();
        options.MaxTransactionCount = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SipTransactionRegistry(options));
    }

    static void AssertBadRequest(SipTransactionAdmissionResult result)
    {
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.Malformed, Is.True);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    static SipTransactionRegistry Registry(int maximum, bool legacy)
    {
        SipTransactionOptions options = new SipTransactionOptions();
        options.MaxTransactionCount = maximum;
        options.AllowLegacyMatching = legacy;
        return new SipTransactionRegistry(options);
    }

    static SipMessage Request(string uri, string branch, int cseq, string method,
        string? toTag)
    {
        string to = toTag == null ? "" : ";tag=" + toTag;
        return Parse(method + " " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=" + branch + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>" + to + "\r\n"
            + "Call-ID: admission-call@example.com\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\nContent-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
