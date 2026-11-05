namespace sip.Tests;

public class SipPublicationClientTests
{
    [Test]
    public void InitialPublicationBuildsRequiredRequestAndQueuesDetachedEvent()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationRequest request = Request("initial-state");
        request.CorrelationId = "publication-one";

        SipPublicationEvent created = client.CreatePublication(request);
        request.Body[0] = (byte)'X';
        SipPublicationEvent queued = client.PollPublicationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(created.State, Is.EqualTo(SipPublicationState.Publishing));
            Assert.That(created.Message!.Method, Is.EqualTo(SipMethod.Publish));
            Assert.That(Header(created.Message, SipHeaderNames.Event), Is.EqualTo("presence"));
            Assert.That(Header(created.Message, SipHeaderNames.Expires), Is.EqualTo("3600"));
            Assert.That(Header(created.Message, SipHeaderNames.SipIfMatch), Is.Null);
            Assert.That(created.Message.Body, Is.EqualTo(Bytes("initial-state")));
            Assert.That(created.PublicationId, Is.Not.Empty);
            Assert.That(created.CorrelationId, Is.EqualTo("publication-one"));
            Assert.That(queued.Request!.Body, Is.EqualTo(Bytes("initial-state")));
        });
    }

    [Test]
    public void SuccessfulResponseStoresEntityTagAndSchedulesRefreshAndExpiry()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationRequest request = Request("online");
        request.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        SipPublicationEvent created = client.CreatePublication(request);
        Drain(client);

        SipPublicationEvent published = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: tag-one\r\nExpires: 120\r\n"));
        SipPublicationEvent queued = client.PollPublicationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(published.State, Is.EqualTo(SipPublicationState.Published));
            Assert.That(published.Accepted, Is.True);
            Assert.That(published.Final, Is.True);
            Assert.That(published.EntityTag, Is.EqualTo("tag-one"));
            Assert.That(published.ExpiresSeconds, Is.EqualTo(120));
            Assert.That(published.RefreshAtMilliseconds, Is.EqualTo(100000));
            Assert.That(published.ExpiresAtMilliseconds, Is.EqualTo(120000));
            Assert.That(queued.EntityTag, Is.EqualTo("tag-one"));
        });
    }

    [Test]
    public void ModifyRefreshAndRemoveUseConditionalOperationBodyRules()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("one"));
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: tag-one\r\nExpires: 300\r\n"));
        Drain(client);

        SipPublicationRequest modifyRequest = Conditional(active, "two");
        SipPublicationEvent modify = client.ModifyPublication(modifyRequest);
        SipPublicationEvent modified = client.ReceivePublicationResponse(
            Response(modify.Message!, 200, "SIP-ETag: tag-two\r\nExpires: 240\r\n"));
        Drain(client);
        SipPublicationEvent refresh = client.RefreshPublication(Conditional(modified, null));
        SipPublicationEvent refreshed = client.ReceivePublicationResponse(
            Response(refresh.Message!, 200, "SIP-ETag: tag-three\r\nExpires: 180\r\n"));
        Drain(client);
        SipPublicationEvent remove = client.RemovePublication(Conditional(refreshed, null));
        SipPublicationEvent removed = client.ReceivePublicationResponse(
            Response(remove.Message!, 200, "SIP-ETag: tag-four\r\nExpires: 0\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(Header(modify.Message!, SipHeaderNames.SipIfMatch), Is.EqualTo("tag-one"));
            Assert.That(modify.Message!.Body, Is.EqualTo(Bytes("two")));
            Assert.That(modified.EntityTag, Is.EqualTo("tag-two"));
            Assert.That(Header(refresh.Message!, SipHeaderNames.SipIfMatch), Is.EqualTo("tag-two"));
            Assert.That(refresh.Message!.Body, Is.Empty);
            Assert.That(refresh.Message.ContentType, Is.Null);
            Assert.That(refreshed.EntityTag, Is.EqualTo("tag-three"));
            Assert.That(Header(remove.Message!, SipHeaderNames.SipIfMatch), Is.EqualTo("tag-three"));
            Assert.That(Header(remove.Message!, SipHeaderNames.Expires), Is.EqualTo("0"));
            Assert.That(remove.Message!.Body, Is.Empty);
            Assert.That(removed.State, Is.EqualTo(SipPublicationState.Terminated));
            Assert.That(removed.Accepted, Is.True);
        });
    }

    [Test]
    public void ConditionalFailureRetriesAsInitialPublicationWithLatestState()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("one"));
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: stale\r\nExpires: 300\r\n"));
        Drain(client);
        SipPublicationEvent modify = client.ModifyPublication(Conditional(active, "latest"));

        SipPublicationEvent retry = client.ReceivePublicationResponse(
            Response(modify.Message!, SipStatusCodes.ConditionalRequestFailed, ""));

        Assert.Multiple(() =>
        {
            Assert.That(retry.State, Is.EqualTo(SipPublicationState.Publishing));
            Assert.That(retry.Final, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("conditional-retry"));
            Assert.That(retry.EntityTag, Is.Null);
            Assert.That(Header(retry.Message!, SipHeaderNames.SipIfMatch), Is.Null);
            Assert.That(retry.Message!.Body, Is.EqualTo(Bytes("latest")));
            Assert.That(retry.Message.CSeq!.SequenceNumber,
                Is.EqualTo(modify.Message!.CSeq!.SequenceNumber + 1));
        });
    }

    [Test]
    public void RefreshConditionalFailureRepublishesCachedState()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("cached"));
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: stale\r\nExpires: 300\r\n"));
        Drain(client);
        SipPublicationEvent refresh = client.RefreshPublication(Conditional(active, null));

        SipPublicationEvent retry = client.ReceivePublicationResponse(
            Response(refresh.Message!, SipStatusCodes.ConditionalRequestFailed, ""));

        Assert.Multiple(() =>
        {
            Assert.That(Header(retry.Message!, SipHeaderNames.SipIfMatch), Is.Null);
            Assert.That(retry.Message!.Body, Is.EqualTo(Bytes("cached")));
            Assert.That(retry.Message.ContentType!.Subtype, Is.EqualTo("pidf+xml"));
        });
    }

    [Test]
    public void RemoveConditionalFailureTreatsAlreadyAbsentStateAsRemoved()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("state"));
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: gone\r\nExpires: 300\r\n"));
        Drain(client);
        SipPublicationEvent remove = client.RemovePublication(Conditional(active, null));

        SipPublicationEvent result = client.ReceivePublicationResponse(
            Response(remove.Message!, SipStatusCodes.ConditionalRequestFailed, ""));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SipPublicationState.Terminated));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Final, Is.True);
            Assert.That(result.EntityTag, Is.Null);
            Assert.That(result.Reason, Is.EqualTo("already-absent"));
        });
    }

    [Test]
    public void IntervalTooBriefRetriesWithMinimumExpiryAndIncreasingCSeq()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationRequest request = Request("state");
        request.ExpiresSeconds = 120;
        SipPublicationEvent created = client.CreatePublication(request);

        SipPublicationEvent retry = client.ReceivePublicationResponse(
            Response(created.Message!, SipStatusCodes.IntervalTooBrief,
                "Min-Expires: 600\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(retry.Final, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("minimum-expiry"));
            Assert.That(Header(retry.Message!, SipHeaderNames.Expires), Is.EqualTo("600"));
            Assert.That(retry.Message!.CSeq!.SequenceNumber,
                Is.EqualTo(created.Message!.CSeq!.SequenceNumber + 1));
        });
    }

    [Test]
    public void AuthenticationChallengeCreatesAuthorizedCorrelatedRetry()
    {
        RecordingAuthenticationClient authentication =
            new RecordingAuthenticationClient();
        SipPublicationClient client = new SipPublicationClient();
        client.ConfigurePublicationAuthenticationClient(authentication);
        SipPublicationRequest request = Request("protected");
        request.AuthenticationContextId = "publish-auth";
        SipPublicationEvent created = client.CreatePublication(request);
        SipMessage challenge = Response(created.Message!, SipStatusCodes.Unauthorized,
            "WWW-Authenticate: Digest realm=\"example.com\", nonce=\"one\", algorithm=SHA-256\r\n");

        SipPublicationEvent retry = client.ReceivePublicationResponse(challenge);
        SipPublicationEvent published = client.ReceivePublicationResponse(
            Response(retry.Message!, 200, "SIP-ETag: secure\r\nExpires: 300\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(retry.AuthenticationAttemptCount, Is.EqualTo(1));
            Assert.That(retry.Message!.CSeq!.SequenceNumber, Is.EqualTo(2));
            Assert.That(Header(retry.Message, SipHeaderNames.Authorization),
                Is.EqualTo("Digest test-response"));
            Assert.That(authentication.AuthorizationCorrelation, Is.EqualTo("publish-auth"));
            Assert.That(authentication.VerificationCount, Is.EqualTo(1));
            Assert.That(published.Accepted, Is.True);
        });
    }

    [Test]
    public void AutomaticRefreshUsesCurrentEntityTagAndExpiryTerminatesWithoutResponse()
    {
        SipPublicationClient refreshClient = new SipPublicationClient();
        SipPublicationRequest refreshRequest = Request("refresh-state");
        refreshRequest.ExpiresSeconds = 120;
        refreshRequest.Options.RefreshBeforeSeconds = 20;
        SipPublicationEvent created = refreshClient.CreatePublication(refreshRequest);
        Drain(refreshClient);
        refreshClient.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: timed\r\nExpires: 120\r\n"));
        Drain(refreshClient);

        refreshClient.AdvancePublicationTime(100000);
        SipPublicationEvent refresh = refreshClient.PollPublicationEvent();

        SipPublicationClient expiryClient = new SipPublicationClient();
        SipPublicationRequest expiryRequest = Request("expiry-state");
        expiryRequest.ExpiresSeconds = 10;
        expiryRequest.Options.MinExpiresSeconds = 0;
        expiryRequest.Options.RefreshBeforeSeconds = 0;
        SipPublicationEvent expiryCreated = expiryClient.CreatePublication(expiryRequest);
        Drain(expiryClient);
        expiryClient.ReceivePublicationResponse(
            Response(expiryCreated.Message!, 200, "SIP-ETag: expires\r\nExpires: 10\r\n"));
        Drain(expiryClient);
        expiryClient.AdvancePublicationTime(10000);
        SipPublicationEvent expired = expiryClient.PollPublicationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(refresh.State, Is.EqualTo(SipPublicationState.Refreshing));
            Assert.That(refresh.Reason, Is.EqualTo("automatic-refresh"));
            Assert.That(Header(refresh.Message!, SipHeaderNames.SipIfMatch), Is.EqualTo("timed"));
            Assert.That(refresh.Message!.Body, Is.Empty);
            Assert.That(expired.State, Is.EqualTo(SipPublicationState.Terminated));
            Assert.That(expired.Final, Is.True);
            Assert.That(expired.Reason, Is.EqualTo("expired"));
            Assert.That(expired.ExpiresSeconds, Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsInvalidOperationBodyCombinationsAndMalformedSuccessResponses()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("state"));
        Drain(client);
        SipPublicationEvent failed = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "Expires: 300\r\n"));
        SipPublicationRequest initialWithoutBody = Request("");
        SipPublicationRequest conditionalWithBody = Request("not-allowed");
        conditionalWithBody.EntityTag = "tag";

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipPublicationState.Failed));
            Assert.That(failed.ErrorMessage, Does.Contain("SIP-ETag"));
            Assert.That(() => new SipPublicationClient().CreatePublication(initialWithoutBody),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new SipPublicationClient().RefreshPublication(conditionalWithBody),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void EntityTagLookupIsScopedByTargetAndEventPackage()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationRequest firstRequest = Request("first");
        SipPublicationRequest secondRequest = Request("second");
        secondRequest.Sender = Uri("sip:bob@example.net");
        secondRequest.Target = Uri("sip:bob@example.net");
        SipPublicationEvent firstCreated = client.CreatePublication(firstRequest);
        SipPublicationEvent secondCreated = client.CreatePublication(secondRequest);
        Drain(client);
        SipPublicationEvent first = client.ReceivePublicationResponse(
            Response(firstCreated.Message!, 200, "SIP-ETag: shared\r\nExpires: 300\r\n"));
        SipPublicationEvent second = client.ReceivePublicationResponse(
            Response(secondCreated.Message!, 200, "SIP-ETag: shared\r\nExpires: 300\r\n"));
        Drain(client);

        SipPublicationRequest secondRefresh = Conditional(second, null);
        secondRefresh.Sender = Uri("sip:bob@example.net");
        secondRefresh.Target = Uri("sip:bob@example.net");
        SipPublicationEvent refresh = client.RefreshPublication(secondRefresh);

        Assert.Multiple(() =>
        {
            Assert.That(first.PublicationId, Is.Not.EqualTo(second.PublicationId));
            Assert.That(refresh.PublicationId, Is.EqualTo(second.PublicationId));
            Assert.That(refresh.Message!.RequestUri!.Host, Is.EqualTo("example.net"));
        });
    }

    [Test]
    public void FailedModificationRestoresLastAcknowledgedBody()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationEvent created = client.CreatePublication(Request("acknowledged"));
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: current\r\nExpires: 300\r\n"));
        Drain(client);
        SipPublicationEvent modify = client.ModifyPublication(
            Conditional(active, "rejected"));
        SipPublicationEvent failed = client.ReceivePublicationResponse(
            Response(modify.Message!, SipStatusCodes.ServerInternalError, ""));
        Drain(client);
        SipPublicationEvent refresh = client.RefreshPublication(Conditional(failed, null));

        SipPublicationEvent recovery = client.ReceivePublicationResponse(
            Response(refresh.Message!, SipStatusCodes.ConditionalRequestFailed, ""));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipPublicationState.Failed));
            Assert.That(recovery.Reason, Is.EqualTo("conditional-retry"));
            Assert.That(recovery.Message!.Body, Is.EqualTo(Bytes("acknowledged")));
        });
    }

    [Test]
    public void FailedOperationPreservesAutomaticRefreshForPublishedState()
    {
        SipPublicationClient client = new SipPublicationClient();
        SipPublicationRequest request = Request("acknowledged");
        request.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        SipPublicationEvent created = client.CreatePublication(request);
        Drain(client);
        SipPublicationEvent active = client.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: current\r\nExpires: 120\r\n"));
        Drain(client);
        SipPublicationEvent modify = client.ModifyPublication(
            Conditional(active, "rejected"));

        SipPublicationEvent failed = client.ReceivePublicationResponse(
            Response(modify.Message!, SipStatusCodes.ServerInternalError, ""));
        Drain(client);
        client.AdvancePublicationTime(100000);
        SipPublicationEvent refresh = client.PollPublicationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipPublicationState.Failed));
            Assert.That(failed.RefreshAtMilliseconds, Is.EqualTo(100000));
            Assert.That(refresh.State, Is.EqualTo(SipPublicationState.Refreshing));
            Assert.That(refresh.Reason, Is.EqualTo("automatic-refresh"));
            Assert.That(refresh.Message!.Body, Is.Empty);
            Assert.That(Header(refresh.Message, SipHeaderNames.SipIfMatch),
                Is.EqualTo("current"));
        });
    }

    [Test]
    public void ProxyChallengeUsesProxyAuthenticationScope()
    {
        RecordingAuthenticationClient authentication =
            new RecordingAuthenticationClient();
        SipPublicationClient client = new SipPublicationClient();
        client.ConfigurePublicationAuthenticationClient(authentication);
        SipPublicationEvent created = client.CreatePublication(Request("protected"));

        SipPublicationEvent retry = client.ReceivePublicationResponse(
            Response(created.Message!, SipStatusCodes.ProxyAuthenticationRequired,
                "Proxy-Authenticate: Digest realm=\"proxy.example.com\", "
                + "nonce=\"proxy-one\", algorithm=SHA-256\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(authentication.AuthorizationScope,
                Is.EqualTo(SipCredentialScope.Proxy));
            Assert.That(Header(retry.Message!, SipHeaderNames.ProxyAuthorization),
                Is.EqualTo("Digest test-response"));
            Assert.That(Header(retry.Message!, SipHeaderNames.Authorization), Is.Null);
            Assert.That(retry.AuthenticationAttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RecoveryFailuresAreFinalAndDoNotLoop()
    {
        SipPublicationClient conditionalClient = new SipPublicationClient();
        SipPublicationEvent created = conditionalClient.CreatePublication(Request("one"));
        Drain(conditionalClient);
        SipPublicationEvent active = conditionalClient.ReceivePublicationResponse(
            Response(created.Message!, 200, "SIP-ETag: stale\r\nExpires: 300\r\n"));
        Drain(conditionalClient);
        SipPublicationEvent modify = conditionalClient.ModifyPublication(
            Conditional(active, "two"));
        SipPublicationEvent retry = conditionalClient.ReceivePublicationResponse(
            Response(modify.Message!, SipStatusCodes.ConditionalRequestFailed, ""));
        SipPublicationEvent repeated = conditionalClient.ReceivePublicationResponse(
            Response(retry.Message!, SipStatusCodes.ConditionalRequestFailed, ""));

        SipPublicationClient expiryClient = new SipPublicationClient();
        SipPublicationRequest expiryRequest = Request("brief");
        expiryRequest.ExpiresSeconds = 120;
        SipPublicationEvent expiryCreated = expiryClient.CreatePublication(expiryRequest);
        SipPublicationEvent invalidMinimum = expiryClient.ReceivePublicationResponse(
            Response(expiryCreated.Message!, SipStatusCodes.IntervalTooBrief,
                "Min-Expires: 90000\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Final, Is.True);
            Assert.That(repeated.State, Is.EqualTo(SipPublicationState.Failed));
            Assert.That(repeated.ErrorMessage, Does.Contain("recovery failed"));
            Assert.That(invalidMinimum.Final, Is.True);
            Assert.That(invalidMinimum.State, Is.EqualTo(SipPublicationState.Failed));
            Assert.That(invalidMinimum.ErrorMessage, Does.Contain("Min-Expires"));
        });
    }

    static SipPublicationRequest Request(string body)
    {
        SipPublicationRequest request = new SipPublicationRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri("sip:alice@example.com");
        request.EventPackage = "presence";
        request.Body = Bytes(body);
        request.ContentType = MediaType("application", "pidf+xml");
        return request;
    }

    static SipPublicationRequest Conditional(SipPublicationEvent active, string? body)
    {
        SipPublicationRequest request = Request(body == null ? "" : body);
        request.EntityTag = active.EntityTag;
        request.ExpiresSeconds = active.ExpiresSeconds;
        if (body == null) request.ContentType = null;
        return request;
    }

    static SipMessage Response(SipMessage request, int status, string extraHeaders)
    {
        return Parse("SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To) + ";tag=server\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders + "Content-Length: 0\r\n\r\n");
    }

    static void Drain(SipPublicationClient client)
    {
        while (client.PollPublicationEvent().State != SipPublicationState.None)
        {
        }
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            Bytes(text), new SipParseOptions());
        return parsed.Uri!;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            Bytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
        return parsed.Message!;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    class RecordingAuthenticationClient : ISipAuthenticationClient
    {
        public string? AuthorizationCorrelation;
        public SipCredentialScope AuthorizationScope;
        public int VerificationCount;

        public SipAuthenticationResult AuthorizeRequest(SipAuthenticationRequest request)
        {
            AuthorizationCorrelation = request.CorrelationId;
            AuthorizationScope = request.Scope;
            SipMessage message = SipReferCore.CopyMessage(request.Message!);
            string headerName = request.Scope == SipCredentialScope.Proxy
                ? SipHeaderNames.ProxyAuthorization : SipHeaderNames.Authorization;
            SipReferCore.AppendHeader(message, headerName,
                "Digest test-response");
            SipAuthenticationResult result = new SipAuthenticationResult();
            result.Status = SipAuthenticationStatus.Success;
            result.RetryAllowed = true;
            result.Message = message;
            return result;
        }

        public SipAuthenticationResult VerifyAuthenticationInfo(
            SipAuthenticationRequest request)
        {
            VerificationCount++;
            SipAuthenticationResult result = new SipAuthenticationResult();
            result.Status = SipAuthenticationStatus.Success;
            return result;
        }
    }
}
