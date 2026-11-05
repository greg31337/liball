namespace sip.Tests;

public class SipTransactionMatchingTests
{
    [Test]
    public void CreateKeyCapturesStableModernTransactionIdentity()
    {
        SipMessage original = Request("OPTIONS", "sip:service@example.com",
            "Client.Example.com:5070", ";branch=z9hG4bK-one", "from-one", null,
            "call-one@example.com", 41);
        SipTransactionKey key = SipTransactionMatching.CreateKey(original,
            SipTransactionRole.Server);

        original.RequestUri!.Host = "changed.example.com";
        original.From!.Parameters[0].Value = "changed";
        original.ViaValues[0].Host = "changed.example.com";
        SipMessage retransmission = Request("OPTIONS", "sip:service@example.com",
            "client.example.com:5070", ";branch=z9hG4bK-one", "from-one", null,
            "call-one@example.com", 41);

        Assert.Multiple(() =>
        {
            Assert.That(key.Branch, Is.EqualTo("z9hG4bK-one"));
            Assert.That(key.SentBy, Is.EqualTo("client.example.com:5070"));
            Assert.That(key.Method, Is.EqualTo(SipMethod.Options));
            Assert.That(key.MethodToken, Is.EqualTo("OPTIONS"));
            Assert.That(key.CallId, Is.EqualTo("call-one@example.com"));
            Assert.That(key.CSeq, Is.EqualTo(41));
            Assert.That(key.Role, Is.EqualTo(SipTransactionRole.Server));
            Assert.That(SipTransactionMatching.MatchRequest(key, retransmission, null, true),
                Is.True);
        });
    }

    [Test]
    public void MagicCookieRequestMatchingUsesBranchSentByAndMethod()
    {
        SipTransactionKey key = SipTransactionMatching.CreateKey(Request("OPTIONS",
            "sip:service@example.com", "client.example.com:5060", ";branch=z9hG4bK-one",
            "from-one", null, "call-one@example.com", 41), SipTransactionRole.Server);
        SipMessage matching = Request("OPTIONS", "sip:different@example.net",
            "CLIENT.EXAMPLE.COM:5060", ";branch=z9hG4bK-one", "other-from", "other-to",
            "other-call@example.net", 99);
        SipMessage differentBranch = Request("OPTIONS", "sip:service@example.com",
            "client.example.com:5060", ";branch=z9hG4bK-two", "from-one", null,
            "call-one@example.com", 41);
        SipMessage differentSentBy = Request("OPTIONS", "sip:service@example.com",
            "client.example.com:5061", ";branch=z9hG4bK-one", "from-one", null,
            "call-one@example.com", 41);
        SipMessage differentMethod = Request("BYE", "sip:service@example.com",
            "client.example.com:5060", ";branch=z9hG4bK-one", "from-one", null,
            "call-one@example.com", 41);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.HasMagicCookie("z9hG4bK-one"), Is.True);
            Assert.That(SipTransactionMatching.HasMagicCookie("Z9HG4BK-one"), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, matching, null, false), Is.True);
            Assert.That(SipTransactionMatching.MatchRequest(key, differentBranch, null, true), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, differentSentBy, null, true), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, differentMethod, null, true), Is.False);
        });
    }

    [Test]
    public void MagicCookieAckMatchesInviteAndCancelTargetsOriginalTransaction()
    {
        SipMessage invite = Request("INVITE", "sip:bob@example.com", "client.example.com",
            ";branch=z9hG4bK-shared", "from-one", null, "invite@example.com", 8);
        SipTransactionKey inviteKey = SipTransactionMatching.CreateKey(invite,
            SipTransactionRole.Server);
        SipTransactionKey optionsKey = SipTransactionMatching.CreateKey(Request("OPTIONS",
            "sip:bob@example.com", "client.example.com", ";branch=z9hG4bK-shared",
            "from-one", null, "invite@example.com", 8), SipTransactionRole.Server);
        SipMessage ack = Request("ACK", "sip:changed@example.net", "client.example.com",
            ";branch=z9hG4bK-shared", "different", "response-tag", "different@example.net", 8);
        SipMessage cancel = Request("CANCEL", "sip:bob@example.com", "client.example.com",
            ";branch=z9hG4bK-shared", "from-one", null, "invite@example.com", 8);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchRequest(inviteKey, ack, "ignored", false), Is.True);
            Assert.That(SipTransactionMatching.MatchRequest(optionsKey, ack, "ignored", false), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(inviteKey, cancel, null, false), Is.False);
            Assert.That(SipTransactionMatching.MatchCancel(inviteKey, cancel, false), Is.True);
            Assert.That(SipTransactionMatching.MatchCancel(optionsKey, cancel, false), Is.True);
        });
    }

    [Test]
    public void ResponsesUseBranchAndCSeqMethodToSeparateCancel()
    {
        SipTransactionKey inviteKey = SipTransactionMatching.CreateKey(Request("INVITE",
            "sip:bob@example.com", "client.example.com", ";branch=z9hG4bK-shared",
            "from-one", null, "call@example.com", 8), SipTransactionRole.Client);
        SipTransactionKey cancelKey = SipTransactionMatching.CreateKey(Request("CANCEL",
            "sip:bob@example.com", "client.example.com", ";branch=z9hG4bK-shared",
            "from-one", null, "call@example.com", 8), SipTransactionRole.Client);
        SipMessage inviteResponse = Response("INVITE", "client.example.com",
            ";branch=z9hG4bK-shared", "from-one", "to-one", "call@example.com", 8);
        SipMessage cancelResponse = Response("CANCEL", "client.example.com",
            ";branch=z9hG4bK-shared", "from-one", "to-one", "call@example.com", 8);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchResponse(inviteKey, inviteResponse, false), Is.True);
            Assert.That(SipTransactionMatching.MatchResponse(cancelKey, inviteResponse, false), Is.False);
            Assert.That(SipTransactionMatching.MatchResponse(cancelKey, cancelResponse, false), Is.True);
            Assert.That(SipTransactionMatching.MatchResponse(inviteKey, cancelResponse, false), Is.False);
        });
    }

    [Test]
    public void ForkedModernResponsesMatchClientBranchDespiteDialogDifferences()
    {
        SipTransactionKey key = SipTransactionMatching.CreateKey(Request("INVITE",
            "sip:bob@example.com", "client.example.com", ";branch=z9hG4bK-forked",
            "from-one", null, "call@example.com", 8), SipTransactionRole.Client);
        SipMessage first = Response("INVITE", "edge-one.example.net",
            ";branch=z9hG4bK-forked", "different-from", "dialog-one",
            "different-one@example.net", 8);
        SipMessage second = Response("INVITE", "edge-two.example.net",
            ";branch=z9hG4bK-forked", "another-from", "dialog-two",
            "different-two@example.net", 8);
        SipMessage otherBranch = Response("INVITE", "edge-one.example.net",
            ";branch=z9hG4bK-other", "different-from", "dialog-one",
            "different-one@example.net", 8);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchResponse(key, first, false), Is.True);
            Assert.That(SipTransactionMatching.MatchResponse(key, second, false), Is.True);
            Assert.That(SipTransactionMatching.MatchResponse(key, otherBranch, false), Is.False);
        });
    }

    [Test]
    public void MalformedTransactionKeysFailClosedForEveryMatcher()
    {
        SipMessage request = Request("OPTIONS", "sip:service@example.com",
            "client.example.com", ";branch=z9hG4bK-key", "from-one", null,
            "key@example.com", 21);
        SipMessage response = Response("OPTIONS", "client.example.com",
            ";branch=z9hG4bK-key", "from-one", "to-one", "key@example.com", 21);
        SipMessage cancel = Request("CANCEL", "sip:service@example.com",
            "client.example.com", ";branch=z9hG4bK-key", "from-one", null,
            "key@example.com", 21);
        SipTransactionKey clientKey = SipTransactionMatching.CreateKey(request,
            SipTransactionRole.Client);
        SipTransactionKey serverKey = SipTransactionMatching.CreateKey(request,
            SipTransactionRole.Server);

        Assert.That(SipTransactionMatching.MatchResponse(null, response, true), Is.False);
        Assert.That(SipTransactionMatching.MatchRequest(null, request, null, true), Is.False);
        Assert.That(SipTransactionMatching.MatchCancel(null, cancel, true), Is.False);

        clientKey.Role = SipTransactionRole.Unknown;
        Assert.That(SipTransactionMatching.MatchResponse(clientKey, response, true), Is.False);
        clientKey.Role = SipTransactionRole.Client;
        clientKey.MethodToken = "not a token";
        Assert.That(SipTransactionMatching.MatchResponse(clientKey, response, true), Is.False);

        serverKey.CSeq = -1;
        Assert.That(SipTransactionMatching.MatchRequest(serverKey, request, null, true), Is.False);
        Assert.That(SipTransactionMatching.MatchCancel(serverKey, cancel, true), Is.False);
        serverKey.CSeq = 21;
        serverKey.Branch = null;
        Assert.That(SipTransactionMatching.MatchRequest(serverKey, request, null, false), Is.False);
        Assert.That(SipTransactionMatching.MatchCancel(serverKey, cancel, false), Is.False);
    }

    [Test]
    public void LegacyRequestMatchingUsesRfc2543TupleAndHeaderComparisonRules()
    {
        SipTransactionKey key = SipTransactionMatching.CreateKey(Request("INVITE",
            "sip:bob@Example.com;transport=udp", "client.example.com:5060",
            ";foo=bar;ttl=4", "from-one", null, "legacy@example.com", 12),
            SipTransactionRole.Server);
        SipMessage matching = Request("INVITE", "sip:bob@example.COM;Transport=UDP",
            "CLIENT.EXAMPLE.COM:5060", ";ttl=4;foo=bar", "from-one", null,
            "legacy@example.com", 12);
        SipMessage wrongUri = Request("INVITE", "sip:alice@example.com;transport=udp",
            "client.example.com:5060", ";foo=bar;ttl=4", "from-one", null,
            "legacy@example.com", 12);
        SipMessage wrongVia = Request("INVITE", "sip:bob@example.com;transport=udp",
            "client.example.com:5060", ";foo=different;ttl=4", "from-one", null,
            "legacy@example.com", 12);
        SipMessage wrongTuple = Request("INVITE", "sip:bob@example.com;transport=udp",
            "client.example.com:5060", ";foo=bar;ttl=4", "from-two", null,
            "legacy@example.com", 12);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchRequest(key, matching, null, true), Is.True);
            Assert.That(SipTransactionMatching.MatchRequest(key, matching, null, false), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, wrongUri, null, true), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, wrongVia, null, true), Is.False);
            Assert.That(SipTransactionMatching.MatchRequest(key, wrongTuple, null, true), Is.False);
        });
    }

    [Test]
    public void LegacyAckUsesResponseToTagAndLegacyCancelIgnoresMethod()
    {
        SipMessage invite = Request("INVITE", "sip:bob@example.com", "client.example.com",
            ";foo=bar", "from-one", null, "legacy@example.com", 12);
        SipTransactionKey key = SipTransactionMatching.CreateKey(invite,
            SipTransactionRole.Server);
        SipMessage ack = Request("ACK", "sip:bob@example.com", "client.example.com",
            ";foo=bar", "from-one", "response-tag", "legacy@example.com", 12);
        SipMessage cancel = Request("CANCEL", "sip:bob@example.com", "client.example.com",
            ";foo=bar", "from-one", null, "legacy@example.com", 12);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchRequest(key, ack, "response-tag", true), Is.True);
            Assert.That(SipTransactionMatching.MatchRequest(key, ack, "other-tag", true), Is.False);
            Assert.That(SipTransactionMatching.MatchCancel(key, cancel, true), Is.True);
            Assert.That(SipTransactionMatching.MatchCancel(key, cancel, false), Is.False);
        });
    }

    [Test]
    public void LegacyResponseUsesBoundedCompatibilityTuple()
    {
        SipTransactionKey key = SipTransactionMatching.CreateKey(Request("OPTIONS",
            "sip:service@example.com", "client.example.com", ";branch=old-one;foo=bar",
            "from-one", null, "legacy@example.com", 33), SipTransactionRole.Client);
        SipMessage response = Response("OPTIONS", "client.example.com",
            ";branch=old-one;foo=bar;received=192.0.2.10", "from-one", "server-tag",
            "legacy@example.com", 33);
        SipMessage wrongCallId = Response("OPTIONS", "client.example.com",
            ";branch=old-one;foo=bar", "from-one", "server-tag", "other@example.com", 33);

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchResponse(key, response, true), Is.True);
            Assert.That(SipTransactionMatching.MatchResponse(key, response, false), Is.False);
            Assert.That(SipTransactionMatching.MatchResponse(key, wrongCallId, true), Is.False);
        });
    }

    [Test]
    public void LegacyCompatibilityRejectsModelsOutsideResourceBounds()
    {
        SipMessage original = Request("OPTIONS", "sip:service@example.com",
            "client.example.com", ";foo=bar", "from-one", null, "legacy@example.com", 33);
        SipTransactionKey key = SipTransactionMatching.CreateKey(original,
            SipTransactionRole.Server);
        SipMessage oversized = Request("OPTIONS", "sip:service@example.com",
            "client.example.com", ";foo=bar", "from-one", null, "legacy@example.com", 33);
        oversized.ViaValues[0].Parameters = new SipUriParameter[SipDefaults.MaxParameterCount + 1];

        Assert.Multiple(() =>
        {
            Assert.That(SipTransactionMatching.MatchRequest(key, oversized, null, true), Is.False);
            Assert.Throws<System.ArgumentException>(() => SipTransactionMatching.CreateKey(
                oversized, SipTransactionRole.Server));
            Assert.Throws<System.ArgumentException>(() => SipTransactionMatching.CreateKey(
                original, SipTransactionRole.Unknown));
        });
    }

    [Test]
    public void TransactionSnapshotCopyDetachesLegacyMatchingIdentity()
    {
        SipMessage original = Request("OPTIONS", "sip:%61lice@example.com",
            "client.example.com", ";foo=bar", "from-one", null, "legacy@example.com", 33);
        SipTransactionSnapshot snapshot = new SipTransactionSnapshot();
        snapshot.Key = SipTransactionMatching.CreateKey(original, SipTransactionRole.Server);
        SipTransactionSnapshot copy = SipSnapshotCopy.CopyTransactionSnapshot(snapshot)!;

        snapshot.Key.LegacyRequestUri!.Host = "changed.example.com";
        snapshot.Key.LegacyFrom!.Parameters[0].Value = "changed";
        snapshot.Key.LegacyTopVia!.Parameters[0].Value = "changed";
        SipMessage retransmission = Request("OPTIONS", "sip:alice@example.com",
            "client.example.com", ";foo=bar", "from-one", null, "legacy@example.com", 33);

        Assert.Multiple(() =>
        {
            Assert.That(copy.Key, Is.Not.SameAs(snapshot.Key));
            Assert.That(SipTransactionMatching.MatchRequest(copy.Key, retransmission, null, true),
                Is.True);
            Assert.That(SipTransactionMatching.MatchRequest(snapshot.Key, retransmission, null, true),
                Is.False);
        });
    }

    static SipMessage Request(string method, string uri, string sentBy, string viaParameters,
        string fromTag, string? toTag, string callId, int cseq)
    {
        string toParameters = toTag == null ? "" : ";tag=" + toTag;
        string contact = method == "INVITE" ? "Contact: <sip:alice@client.example.com>\r\n" : "";
        return Parse(method + " " + uri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP " + sentBy + viaParameters + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=" + fromTag + "\r\n"
            + "To: <sip:bob@example.com>" + toParameters + "\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n" + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Response(string method, string sentBy, string viaParameters,
        string fromTag, string toTag, string callId, int cseq)
    {
        string contact = method == "INVITE" ? "Contact: <sip:bob@server.example.com>\r\n" : "";
        return Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP " + sentBy + viaParameters + "\r\n"
            + "From: <sip:alice@example.com>;tag=" + fromTag + "\r\n"
            + "To: <sip:bob@example.com>;tag=" + toTag + "\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + method + "\r\n" + contact + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }
}
