namespace sip.Tests;

public class SipPublicationServerTests
{
    [Test]
    public void InitialPublicationStoresDetachedStateAndQueuesAggregationTrigger()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest request = Request(Pidf("ready"));
        request.CorrelationId = "initial-publication";

        SipPublicationEvent accepted = server.ProcessPublication(request);
        request.Body[0] = (byte)'X';
        SipPublication[] stored = server.GetPublications();
        stored[0].Body[0] = (byte)'Y';
        SipPublicationEvent queued = server.PollPublicationEvent();
        SipPublication[] second = server.GetPublications();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State, Is.EqualTo(SipPublicationState.Published));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(accepted.EntityTag, Is.Not.Empty);
            Assert.That(accepted.PublicationId, Is.Not.Empty);
            Assert.That(accepted.AggregationRequired, Is.True);
            Assert.That(accepted.ExpiresSeconds, Is.EqualTo(300));
            Assert.That(accepted.ExpiresAtMilliseconds, Is.EqualTo(300000));
            Assert.That(queued.CorrelationId, Is.EqualTo("initial-publication"));
            Assert.That(second[0].Body, Is.EqualTo(Bytes(Pidf("ready"))));
        });
    }

    [Test]
    public void MessageRequestProducesRequiredSuccessResponseHeaders()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest operation = new SipPublicationRequest();
        operation.Message = PublishMessage(Pidf("online"), null, 180, "presence");

        SipPublicationEvent accepted = server.ProcessPublication(operation);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True, accepted.ErrorMessage);
            Assert.That(accepted.Response, Is.Not.Null);
            Assert.That(accepted.Response!.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(Header(accepted.Response, SipHeaderNames.SipETag),
                Is.EqualTo(accepted.EntityTag));
            Assert.That(Header(accepted.Response, SipHeaderNames.Expires), Is.EqualTo("180"));
            Assert.That(accepted.Request!.Target!.Host, Is.EqualTo("example.com"));
            Assert.That(accepted.Request.Sender!.User, Is.EqualTo("alice"));
            Assert.That(accepted.CorrelationId, Is.EqualTo("publish-call"));
        });
    }

    [Test]
    public void ConditionalModificationReplacesStateAndRejectsStaleTag()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        Drain(server);
        SipPublicationRequest modification = Conditional(initial, Pidf("two"), 240);

        SipPublicationEvent modified = server.ProcessPublication(modification);
        SipPublicationEvent stale = server.ProcessPublication(modification);
        SipPublication[] stored = server.GetPublications();

        Assert.Multiple(() =>
        {
            Assert.That(modified.Accepted, Is.True, modified.ErrorMessage);
            Assert.That(modified.PublicationId, Is.EqualTo(initial.PublicationId));
            Assert.That(modified.EntityTag, Is.Not.EqualTo(initial.EntityTag));
            Assert.That(modified.AggregationRequired, Is.True);
            Assert.That(stored, Has.Length.EqualTo(1));
            Assert.That(stored[0].Body, Is.EqualTo(Bytes(Pidf("two"))));
            Assert.That(stored[0].ExpiresAtMilliseconds, Is.EqualTo(240000));
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.StatusCode,
                Is.EqualTo(SipStatusCodes.ConditionalRequestFailed));
        });
    }

    [Test]
    public void RefreshRotatesTagWithoutTriggeringAggregationOrChangingBody()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        Drain(server);
        SipPublicationRequest refresh = Conditional(initial, null, 600);

        SipPublicationEvent refreshed = server.ProcessPublication(refresh);
        SipPublication stored = server.GetPublications()[0];

        Assert.Multiple(() =>
        {
            Assert.That(refreshed.Accepted, Is.True);
            Assert.That(refreshed.Reason, Is.EqualTo("refreshed"));
            Assert.That(refreshed.AggregationRequired, Is.False);
            Assert.That(refreshed.EntityTag, Is.Not.EqualTo(initial.EntityTag));
            Assert.That(stored.Body, Is.EqualTo(Bytes(Pidf("one"))));
            Assert.That(stored.ExpiresAtMilliseconds, Is.EqualTo(600000));
        });
    }

    [Test]
    public void RemovalDeletesStateAndTriggersAggregation()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        Drain(server);
        SipPublicationRequest remove = Conditional(initial, null, 0);

        SipPublicationEvent removed = server.ProcessPublication(remove);

        Assert.Multiple(() =>
        {
            Assert.That(removed.State, Is.EqualTo(SipPublicationState.Terminated));
            Assert.That(removed.Accepted, Is.True);
            Assert.That(removed.AggregationRequired, Is.True);
            Assert.That(removed.ExpiresSeconds, Is.EqualTo(0));
            Assert.That(removed.Publication, Is.Null);
            Assert.That(server.GetPublications(), Is.Empty);
        });
    }

    [Test]
    public void TooBriefMessageRequestReturns423WithMinimum()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest operation = new SipPublicationRequest();
        operation.Message = PublishMessage(Pidf("brief"), null, 10, "presence");
        operation.ServerOptions.MinExpiresSeconds = 90;

        SipPublicationEvent rejected = server.ProcessPublication(operation);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(SipStatusCodes.IntervalTooBrief));
            Assert.That(rejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.IntervalTooBrief));
            Assert.That(Header(rejected.Response, SipHeaderNames.MinExpires), Is.EqualTo("90"));
            Assert.That(server.GetPublications(), Is.Empty);
        });
    }

    [Test]
    public void RejectsUnknownPackagesMalformedBodiesAndInvalidOperationMatrix()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest unknown = Request("opaque");
        unknown.EventPackage = "unknown";
        unknown.ContentType = MediaType("application", "opaque");
        SipPublicationRequest malformed = Request("<presence>");
        SipPublicationRequest refreshWithoutTag = Request("");
        refreshWithoutTag.ContentType = null;

        SipPublicationEvent unknownResult = server.ProcessPublication(unknown);
        SipPublicationEvent malformedResult = server.ProcessPublication(malformed);
        SipPublicationEvent matrixResult = server.ProcessPublication(refreshWithoutTag);

        Assert.Multiple(() =>
        {
            Assert.That(unknownResult.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
            Assert.That(malformedResult.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(matrixResult.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(server.GetPublications(), Is.Empty);
        });
    }

    [Test]
    public void AuthorizationPolicyReceivesOperationContextAndCanDeny()
    {
        RecordingAuthorizationPolicy policy = new RecordingAuthorizationPolicy();
        SipPublicationServer server = new SipPublicationServer();
        server.ConfigurePublicationAuthorizationPolicy(policy);
        SipPublicationRequest request = Request(Pidf("private"));
        request.ServerOptions.RequireAuthorization = true;
        policy.Allowed = false;
        policy.StatusCode = SipStatusCodes.Forbidden;
        policy.Reason = "not permitted";

        SipPublicationEvent denied = server.ProcessPublication(request);
        policy.Allowed = true;
        SipPublicationEvent allowed = server.ProcessPublication(request);

        Assert.Multiple(() =>
        {
            Assert.That(denied.StatusCode, Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(denied.Authorization!.Allowed, Is.False);
            Assert.That(denied.ErrorMessage, Is.EqualTo("not permitted"));
            Assert.That(allowed.Accepted, Is.True, allowed.ErrorMessage);
            Assert.That(policy.Context!.Initial, Is.True);
            Assert.That(policy.Context.Request!.Body, Is.EqualTo(Bytes(Pidf("private"))));
        });
    }

    [Test]
    public void RequiredAuthorizationRejectsWhenNoPolicyIsConfigured()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest request = Request(Pidf("private"));
        request.ServerOptions.RequireAuthorization = true;

        SipPublicationEvent denied = server.ProcessPublication(request);

        Assert.Multiple(() =>
        {
            Assert.That(denied.StatusCode, Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(denied.Accepted, Is.False);
            Assert.That(server.GetPublications(), Is.Empty);
        });
    }

    [Test]
    public void ExpirationRemovesStoredStateAndQueuesAggregationTrigger()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest request = Request(Pidf("temporary"));
        request.ExpiresSeconds = 60;
        server.ProcessPublication(request);
        Drain(server);

        server.ExpirePublications(59999);
        Assert.That(server.GetPublications(), Has.Length.EqualTo(1));
        server.ExpirePublications(60000);
        SipPublicationEvent expired = server.PollPublicationEvent();

        Assert.Multiple(() =>
        {
            Assert.That(server.GetPublications(), Is.Empty);
            Assert.That(expired.State, Is.EqualTo(SipPublicationState.Terminated));
            Assert.That(expired.Reason, Is.EqualTo("expired"));
            Assert.That(expired.AggregationRequired, Is.True);
            Assert.That(expired.Final, Is.True);
        });
    }

    [Test]
    public void ConfiguredStoreReceivesCreatesUpdatesAndDeletes()
    {
        RecordingStore store = new RecordingStore();
        SipPublicationServer server = new SipPublicationServer();
        server.ConfigurePublicationStore(store);
        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        SipPublicationEvent modified = server.ProcessPublication(
            Conditional(initial, Pidf("two"), 300));
        server.ProcessPublication(Conditional(modified, null, 0));

        Assert.Multiple(() =>
        {
            Assert.That(store.StoreCount, Is.EqualTo(2));
            Assert.That(store.RemoveCount, Is.EqualTo(1));
            Assert.That(store.GetPublications(), Is.Empty);
        });
    }

    [Test]
    public void MessageConditionalFailureProduces412Response()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest operation = new SipPublicationRequest();
        operation.Message = PublishMessage("", "missing", 300, "presence");

        SipPublicationEvent rejected = server.ProcessPublication(operation);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.ConditionalRequestFailed));
            Assert.That(rejected.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ConditionalRequestFailed));
        });
    }

    [Test]
    public void ErrorResponsesAdvertiseEventPackagesAndAcceptedBodyTypes()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest unknown = new SipPublicationRequest();
        unknown.Message = PublishMessage(Pidf("state"), null, 300, "unknown");
        SipPublicationRequest wrongType = new SipPublicationRequest();
        wrongType.Message = PublishMessageWithType(
            "plain", null, 300, "presence", "text/plain");

        SipPublicationEvent badEvent = server.ProcessPublication(unknown);
        SipPublicationEvent unsupported = server.ProcessPublication(wrongType);

        Assert.Multiple(() =>
        {
            Assert.That(badEvent.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
            Assert.That(Header(badEvent.Response!, SipHeaderNames.AllowEvents),
                Does.Contain("presence"));
            Assert.That(unsupported.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedMediaType));
            Assert.That(Header(unsupported.Response!, SipHeaderNames.Accept),
                Does.Contain(SipMediaTypes.Pidf));
        });
    }

    [Test]
    public async Task ConcurrentConditionalUpdatesAcceptOnlyOneCurrentEntityTag()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        Drain(server);
        CoordinatedAuthorizationPolicy policy =
            new CoordinatedAuthorizationPolicy();
        server.ConfigurePublicationAuthorizationPolicy(policy);
        SipPublicationRequest firstRequest = Conditional(initial, Pidf("first"), 300);
        SipPublicationRequest secondRequest = Conditional(initial, Pidf("second"), 300);

        Task<SipPublicationEvent> firstTask = Task.Run(
            () => server.ProcessPublication(firstRequest));
        Assert.That(policy.FirstEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Task<SipPublicationEvent> secondTask = Task.Run(
            () => server.ProcessPublication(secondRequest));
        SipPublicationEvent second = await secondTask;
        policy.ReleaseFirst.Set();
        SipPublicationEvent first = await firstTask;
        SipPublication stored = server.GetPublications()[0];

        Assert.Multiple(() =>
        {
            Assert.That(second.Accepted, Is.True, second.ErrorMessage);
            Assert.That(first.Accepted, Is.False);
            Assert.That(first.StatusCode,
                Is.EqualTo(SipStatusCodes.ConditionalRequestFailed));
            Assert.That(stored.Body, Is.EqualTo(Bytes(Pidf("second"))));
            Assert.That(stored.EntityTag, Is.EqualTo(second.EntityTag));
        });
    }

    [Test]
    public void AuthorizationReceivesEveryPublicationOperation()
    {
        OperationAuthorizationPolicy policy = new OperationAuthorizationPolicy();
        SipPublicationServer server = new SipPublicationServer();
        server.ConfigurePublicationAuthorizationPolicy(policy);

        SipPublicationEvent initial = server.ProcessPublication(Request(Pidf("one")));
        SipPublicationEvent modified = server.ProcessPublication(
            Conditional(initial, Pidf("two"), 240));
        SipPublicationEvent refreshed = server.ProcessPublication(
            Conditional(modified, null, 180));
        SipPublicationEvent removed = server.ProcessPublication(
            Conditional(refreshed, null, 0));

        Assert.Multiple(() =>
        {
            Assert.That(policy.Contexts, Has.Count.EqualTo(4));
            Assert.That(policy.Contexts[0].Initial, Is.True);
            Assert.That(policy.Contexts[1].Modify, Is.True);
            Assert.That(policy.Contexts[1].CurrentPublication!.Body,
                Is.EqualTo(Bytes(Pidf("one"))));
            Assert.That(policy.Contexts[2].Refresh, Is.True);
            Assert.That(policy.Contexts[2].CurrentPublication!.Body,
                Is.EqualTo(Bytes(Pidf("two"))));
            Assert.That(policy.Contexts[3].Remove, Is.True);
            Assert.That(removed.Accepted, Is.True);
        });
    }

    [Test]
    public void MalformedMessageRequestsReturnSpecificProtocolErrors()
    {
        SipPublicationServer server = new SipPublicationServer();
        SipPublicationRequest wrongMethod = new SipPublicationRequest();
        wrongMethod.Message = PublishMessage(Pidf("state"), null, 300, "presence");
        wrongMethod.Message.Method = SipMethod.Options;
        wrongMethod.Message.MethodToken = SipMethods.Options;
        SipPublicationRequest duplicateEvent = new SipPublicationRequest();
        duplicateEvent.Message = PublishMessage(Pidf("state"), null, 300, "presence");
        SipReferCore.AppendHeader(
            duplicateEvent.Message, SipHeaderNames.Event, "presence");
        SipPublicationRequest duplicateTag = new SipPublicationRequest();
        duplicateTag.Message = PublishMessage("", "first", 300, "presence");
        SipReferCore.AppendHeader(
            duplicateTag.Message, SipHeaderNames.SipIfMatch, "second");

        SipPublicationEvent methodResult = server.ProcessPublication(wrongMethod);
        SipPublicationEvent eventResult = server.ProcessPublication(duplicateEvent);
        SipPublicationEvent tagResult = server.ProcessPublication(duplicateTag);

        Assert.Multiple(() =>
        {
            Assert.That(methodResult.StatusCode,
                Is.EqualTo(SipStatusCodes.MethodNotAllowed));
            Assert.That(Header(methodResult.Response!, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Publish));
            Assert.That(eventResult.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
            Assert.That(tagResult.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(server.GetPublications(), Is.Empty);
        });
    }

    static SipPublicationRequest Request(string body)
    {
        SipPublicationRequest request = new SipPublicationRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri("sip:alice@example.com");
        request.EventPackage = "presence";
        request.ExpiresSeconds = 300;
        request.Body = Bytes(body);
        request.ContentType = MediaType("application", "pidf+xml");
        return request;
    }

    static SipPublicationRequest Conditional(
        SipPublicationEvent current, string? body, int expires)
    {
        SipPublicationRequest request = Request(body == null ? "" : body);
        request.EntityTag = current.EntityTag;
        request.ExpiresSeconds = expires;
        if (body == null) request.ContentType = null;
        return request;
    }

    static SipMessage PublishMessage(
        string body, string? entityTag, int expires, string eventPackage)
    {
        return PublishMessageWithType(
            body, entityTag, expires, eventPackage, "application/pidf+xml");
    }

    static SipMessage PublishMessageWithType(
        string body, string? entityTag, int expires,
        string eventPackage, string contentType)
    {
        byte[] bodyBytes = Bytes(body);
        string headers = "PUBLISH sip:alice@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-publish\r\n"
            + "Max-Forwards: 70\r\nFrom: <sip:alice@example.com>;tag=publisher\r\n"
            + "To: <sip:alice@example.com>\r\nCall-ID: publish-call\r\n"
            + "CSeq: 1 PUBLISH\r\nEvent: " + eventPackage + "\r\n"
            + "Expires: " + expires + "\r\n"
            + (entityTag == null ? "" : "SIP-If-Match: " + entityTag + "\r\n")
            + (bodyBytes.Length == 0 ? "" : "Content-Type: " + contentType + "\r\n")
            + "Content-Length: " + bodyBytes.Length + "\r\n\r\n";
        byte[] headerBytes = Bytes(headers);
        byte[] wire = new byte[headerBytes.Length + bodyBytes.Length];
        Array.Copy(headerBytes, wire, headerBytes.Length);
        Array.Copy(bodyBytes, 0, wire, headerBytes.Length, bodyBytes.Length);
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            wire, new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string Pidf(string note)
    {
        return "<?xml version=\"1.0\"?><presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\"><note>" + note + "</note></presence>";
    }

    static void Drain(SipPublicationServer server)
    {
        while (server.PollPublicationEvent().State != SipPublicationState.None)
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

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    class RecordingAuthorizationPolicy : ISipPublicationAuthorizationPolicy
    {
        public bool Allowed;
        public int StatusCode;
        public string? Reason;
        public SipPublicationAuthorizationContext? Context;

        public SipAuthorizationDecision AuthorizePublication(
            SipPublicationAuthorizationContext context)
        {
            Context = context;
            SipAuthorizationDecision decision = new SipAuthorizationDecision();
            decision.Allowed = Allowed;
            decision.StatusCode = StatusCode;
            decision.Reason = Reason;
            return decision;
        }
    }

    class RecordingStore : ISipPublicationStore
    {
        SipMemoryPublicationStore inner = new SipMemoryPublicationStore();
        public int StoreCount;
        public int RemoveCount;

        public SipPublication[] GetPublications()
        {
            return inner.GetPublications();
        }

        public void StorePublication(SipPublication publication)
        {
            StoreCount++;
            inner.StorePublication(publication);
        }

        public bool RemovePublication(string publicationId)
        {
            RemoveCount++;
            return inner.RemovePublication(publicationId);
        }
    }

    class CoordinatedAuthorizationPolicy : ISipPublicationAuthorizationPolicy
    {
        public ManualResetEventSlim FirstEntered = new ManualResetEventSlim(false);
        public ManualResetEventSlim ReleaseFirst = new ManualResetEventSlim(false);
        int authorizationCount;

        public SipAuthorizationDecision AuthorizePublication(
            SipPublicationAuthorizationContext context)
        {
            int count = Interlocked.Increment(ref authorizationCount);
            if (count == 1)
            {
                FirstEntered.Set();
                if (!ReleaseFirst.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The concurrent update was not released.");
            }
            SipAuthorizationDecision decision = new SipAuthorizationDecision();
            decision.Allowed = true;
            return decision;
        }
    }

    class OperationAuthorizationPolicy : ISipPublicationAuthorizationPolicy
    {
        public List<SipPublicationAuthorizationContext> Contexts =
            new List<SipPublicationAuthorizationContext>();

        public SipAuthorizationDecision AuthorizePublication(
            SipPublicationAuthorizationContext context)
        {
            Contexts.Add(context);
            SipAuthorizationDecision decision = new SipAuthorizationDecision();
            decision.Allowed = true;
            return decision;
        }
    }
}
