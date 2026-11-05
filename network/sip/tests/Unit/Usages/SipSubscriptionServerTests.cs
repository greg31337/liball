namespace sip.Tests;

public class SipSubscriptionServerTests
{
    [Test]
    public void InitialSubscriptionIsAuthorizedAndImmediatelyNotified()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        RecordingSubscriptionPolicy policy =
            new RecordingSubscriptionPolicy();
        policy.Decision.Allowed = true;
        SipSubscriptionServer server = new SipSubscriptionServer();
        server.ConfigureSubscriptionAuthorizationPolicy(policy);
        SipSubscriptionRequest operation = ServerRequest(sent.Message!);
        operation.CorrelationId = "server-subscription";

        SipSubscriptionEvent accepted = server.ProcessSubscription(operation);
        SipSubscriptionEvent active = client.ConsumeNotify(
            accepted.Notification!);
        SipSubscriptionEvent delivered = server.ReceiveNotifyResponse(
            active.Response!);
        SipSubscriptionEvent response = client.ReceiveSubscriptionResponse(
            accepted.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(accepted.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(accepted.Version, Is.EqualTo(1));
            Assert.That(accepted.NotifyCSeq, Is.EqualTo(1));
            Assert.That(Header(accepted.Notification!, SipHeaderNames.Event),
                Is.EqualTo("presence"));
            Assert.That(Header(accepted.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("active;expires=3600"));
            Assert.That(active.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(delivered.Accepted, Is.True);
            Assert.That(response.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(policy.Context!.EventPackage, Is.EqualTo("presence"));
            Assert.That(policy.Context.Refresh, Is.False);
            Assert.That(policy.Context.CorrelationId,
                Is.EqualTo("server-subscription"));
        });
    }

    [Test]
    public void IntervalTooBriefReturnsMinimumWithoutCreatingNotify()
    {
        SipSubscriptionRequest clientRequest = ClientRequest();
        clientRequest.ExpiresSeconds = 60;
        SipSubscriptionEvent sent = new SipSubscriptionClient()
            .CreateSubscription(clientRequest);
        SipSubscriptionRequest operation = ServerRequest(sent.Message!);
        operation.ServerOptions.MinExpiresSeconds = 300;

        SipSubscriptionEvent rejected = new SipSubscriptionServer()
            .ProcessSubscription(operation);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.IntervalTooBrief));
            Assert.That(Header(rejected.Response!, SipHeaderNames.MinExpires),
                Is.EqualTo("300"));
            Assert.That(rejected.Notification, Is.Null);
        });
    }

    [Test]
    public void ApplicationCanDenySubscription()
    {
        SipSubscriptionEvent sent = new SipSubscriptionClient()
            .CreateSubscription(ClientRequest());
        RecordingSubscriptionPolicy policy =
            new RecordingSubscriptionPolicy();
        policy.Decision.Allowed = false;
        policy.Decision.StatusCode = SipStatusCodes.Forbidden;
        policy.Decision.Reason = "Watcher is not permitted.";
        SipSubscriptionServer server = new SipSubscriptionServer();
        server.ConfigureSubscriptionAuthorizationPolicy(policy);

        SipSubscriptionEvent rejected = server.ProcessSubscription(
            ServerRequest(sent.Message!));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(403));
            Assert.That(rejected.ErrorMessage,
                Is.EqualTo("Watcher is not permitted."));
            Assert.That(rejected.Authorization!.Allowed, Is.False);
            Assert.That(rejected.Notification, Is.Null);
        });
    }

    [Test]
    public void RefreshUpdatesLifetimeAndProducesNextVersion()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        client.ReceiveSubscriptionResponse(initial.Response!);
        SipSubscriptionRequest refreshRequest = ClientRequest();
        refreshRequest.DialogId = active.DialogId;
        refreshRequest.ExpiresSeconds = 600;
        SipSubscriptionEvent refresh = client.RefreshSubscription(
            refreshRequest);

        SipSubscriptionEvent refreshed = server.ProcessSubscription(
            ServerRequest(refresh.Message!));

        Assert.Multiple(() =>
        {
            Assert.That(refreshed.Accepted, Is.True);
            Assert.That(refreshed.ExpiresSeconds, Is.EqualTo(600));
            Assert.That(refreshed.Version, Is.EqualTo(2));
            Assert.That(refreshed.NotifyCSeq, Is.EqualTo(2));
            Assert.That(Header(refreshed.Response!, SipHeaderNames.Expires),
                Is.EqualTo("600"));
            Assert.That(Header(refreshed.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("active;expires=600"));
        });
    }

    [Test]
    public void UnsubscribeCreatesFinalNotifyAndRemovesAfterDelivery()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        client.ReceiveSubscriptionResponse(initial.Response!);
        DrainClient(client);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = active.SubscriptionId;
        client.TerminateSubscription(cancellation);
        SipSubscriptionEvent unsubscribe = client.PollSubscriptionEvent();

        SipSubscriptionEvent terminated = server.ProcessSubscription(
            ServerRequest(unsubscribe.Message!));
        SipSubscriptionEvent clientFinal = client.ConsumeNotify(
            terminated.Notification!);
        SipSubscriptionEvent delivery = server.ReceiveNotifyResponse(
            clientFinal.Response!);

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Final, Is.True);
            Assert.That(terminated.Reason, Is.EqualTo("timeout"));
            Assert.That(Header(terminated.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("terminated;reason=timeout"));
            Assert.That(delivery.Final, Is.True);
            Assert.That(() => server.SendNotify(Update(
                terminated.SubscriptionId!, SipSubscriptionState.Active,
                60)), Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void StateUpdateCarriesBodyReasonRetryAndMonotonicVersion()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        SipSubscriptionEvent update = Update(initial.SubscriptionId!,
            SipSubscriptionState.Terminated, 0);
        update.Reason = "probation";
        update.RetryAfterSeconds = 30;
        update.Body = System.Text.Encoding.UTF8.GetBytes("offline");
        update.ContentType = MediaType("text", "plain");

        SipSubscriptionEvent notified = server.SendNotify(update);

        Assert.Multiple(() =>
        {
            Assert.That(notified.Version, Is.EqualTo(2));
            Assert.That(notified.NotifyCSeq, Is.EqualTo(2));
            Assert.That(notified.Body,
                Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("offline")));
            Assert.That(Header(notified.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("terminated;reason=probation;retry-after=30"));
            Assert.That(Header(notified.Notification!,
                SipHeaderNames.ContentType), Is.EqualTo("text/plain"));
        });
    }

    [Test]
    public void ExpiryQueuesFinalTimeoutNotifyAfterInitialDelivery()
    {
        SipSubscriptionRequest clientRequest = ClientRequest();
        clientRequest.ExpiresSeconds = 60;
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(clientRequest);
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        DrainServer(server);

        server.ExpireSubscriptions(60000);
        SipSubscriptionEvent expired = server.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(expired.State,
                Is.EqualTo(SipSubscriptionState.Terminated));
            Assert.That(expired.Final, Is.True);
            Assert.That(expired.Reason, Is.EqualTo("timeout"));
            Assert.That(expired.Version, Is.EqualTo(2));
            Assert.That(expired.Notification, Is.Not.Null);
        });
    }

    [Test]
    public void ReliableDeliverySerializesNotifyAndHandlesFailure()
    {
        SipSubscriptionEvent sent = new SipSubscriptionClient()
            .CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));

        Assert.That(() => server.SendNotify(Update(
            initial.SubscriptionId!, SipSubscriptionState.Active, 300)),
            Throws.TypeOf<InvalidOperationException>());

        SipMessage failedResponse = Response(
            initial.Notification!, SipStatusCodes.CallOrTransactionDoesNotExist,
            null, "");
        SipSubscriptionEvent failed = server.ReceiveNotifyResponse(
            failedResponse);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Final, Is.True);
            Assert.That(failed.Accepted, Is.False);
            Assert.That(failed.Reason, Is.EqualTo("delivery-failed"));
            Assert.That(failed.ErrorMessage, Does.Contain("rejected"));
        });
    }

    [Test]
    public void NonForkableEventRejectsRoutingMarkedFork()
    {
        SipSubscriptionEvent sent = new SipSubscriptionClient()
            .CreateSubscription(ClientRequest());
        SipSubscriptionRequest operation = ServerRequest(sent.Message!);
        operation.Forked = true;
        operation.ServerOptions.AllowForking = false;

        SipSubscriptionEvent rejected = new SipSubscriptionServer()
            .ProcessSubscription(operation);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(403));
            Assert.That(rejected.Forked, Is.True);
            Assert.That(rejected.ErrorMessage, Does.Contain("Forked"));
        });
    }

    [Test]
    public void RefreshWithDifferentEventIdentifierIsRejected()
    {
        SipSubscriptionRequest clientRequest = ClientRequest();
        clientRequest.EventId = "one";
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(clientRequest);
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        client.ReceiveSubscriptionResponse(initial.Response!);
        SipSubscriptionRequest refreshRequest = ClientRequest();
        refreshRequest.EventId = "one";
        refreshRequest.DialogId = active.DialogId;
        SipSubscriptionEvent refresh = client.RefreshSubscription(
            refreshRequest);
        SetHeader(refresh.Message!, SipHeaderNames.Event,
            "presence;id=two");

        SipSubscriptionEvent rejected = server.ProcessSubscription(
            ServerRequest(refresh.Message!));

        Assert.That(rejected.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
    }

    [Test]
    public void ExpiryWaitsForOutstandingNotifyThenQueuesTermination()
    {
        SipSubscriptionRequest clientRequest = ClientRequest();
        clientRequest.ExpiresSeconds = 60;
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(clientRequest);
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        DrainServer(server);

        server.ExpireSubscriptions(60000);
        SipSubscriptionEvent deferred = server.PollSubscriptionEvent();
        SipSubscriptionEvent received = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(received.Response!);
        SipSubscriptionEvent delivered = server.PollSubscriptionEvent();
        SipSubscriptionEvent expired = server.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(deferred.State,
                Is.EqualTo(SipSubscriptionState.None));
            Assert.That(delivered.Accepted, Is.True);
            Assert.That(delivered.Final, Is.False);
            Assert.That(expired.State,
                Is.EqualTo(SipSubscriptionState.Terminated));
            Assert.That(expired.Final, Is.True);
            Assert.That(expired.Reason, Is.EqualTo("timeout"));
            Assert.That(Header(expired.Notification!,
                SipHeaderNames.SubscriptionState),
                Is.EqualTo("terminated;reason=timeout"));
        });
    }

    [Test]
    public void InconsistentSubscribeMethodTokensReturnMethodFailure()
    {
        SipSubscriptionEvent sent = new SipSubscriptionClient()
            .CreateSubscription(ClientRequest());
        sent.Message!.MethodToken = SipMethods.Options;

        SipSubscriptionEvent rejected = new SipSubscriptionServer()
            .ProcessSubscription(ServerRequest(sent.Message));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Final, Is.True);
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.MethodNotAllowed));
        });
    }

    [Test]
    public void InconsistentNotifyResponseTokenPreservesOutstandingDelivery()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        SipMessage inconsistent = SipSnapshotCopy.CopyMessageModel(
            active.Response)!;
        inconsistent.CSeq!.MethodToken = SipMethods.Info;

        Assert.That(() => server.ReceiveNotifyResponse(inconsistent),
            Throws.TypeOf<ArgumentException>());
        SipSubscriptionEvent delivered = server.ReceiveNotifyResponse(
            active.Response!);

        Assert.That(delivered.Accepted, Is.True);
    }

    [Test]
    public void RefreshOrderingRejectsOlderAndReplaysDuplicateRequest()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(ClientRequest());
        SipSubscriptionServer server = new SipSubscriptionServer();
        SipSubscriptionEvent initial = server.ProcessSubscription(
            ServerRequest(sent.Message!));
        SipSubscriptionEvent active = client.ConsumeNotify(
            initial.Notification!);
        server.ReceiveNotifyResponse(active.Response!);
        client.ReceiveSubscriptionResponse(initial.Response!);
        SipSubscriptionRequest refreshRequest = ClientRequest();
        refreshRequest.DialogId = active.DialogId;
        SipSubscriptionEvent refresh = client.RefreshSubscription(
            refreshRequest);
        SipMessage older = SipSnapshotCopy.CopyMessageModel(
            refresh.Message)!;
        older.CSeq!.SequenceNumber = 0;
        SetHeader(older, SipHeaderNames.CSeq, "0 SUBSCRIBE");
        SipSubscriptionEvent rejected = server.ProcessSubscription(
            ServerRequest(older));

        SipSubscriptionEvent refreshed = server.ProcessSubscription(
            ServerRequest(refresh.Message!));
        SipSubscriptionEvent retransmitted = server.ProcessSubscription(
            ServerRequest(refresh.Message!));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(rejected.ErrorMessage, Does.Contain("out of order"));
            Assert.That(refreshed.Accepted, Is.True);
            Assert.That(retransmitted.Accepted, Is.True);
            Assert.That(retransmitted.Reason, Is.EqualTo("retransmission"));
            Assert.That(retransmitted.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
        });
    }

    static SipSubscriptionRequest ClientRequest()
    {
        SipSubscriptionRequest request = new SipSubscriptionRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri("sip:bob@example.com");
        request.Contact = Uri("sip:alice@device.example.com");
        request.EventPackage = "presence";
        return request;
    }

    static SipSubscriptionRequest ServerRequest(SipMessage message)
    {
        SipSubscriptionRequest request = new SipSubscriptionRequest();
        request.Message = message;
        request.Target = Uri("sip:bob@server.example.com");
        request.EventPackage = "presence";
        return request;
    }

    static SipSubscriptionEvent Update(
        string subscriptionId, SipSubscriptionState state, int expires)
    {
        SipSubscriptionEvent value = new SipSubscriptionEvent();
        value.SubscriptionId = subscriptionId;
        value.State = state;
        value.ExpiresSeconds = expires;
        return value;
    }

    static SipMessage Response(
        SipMessage request, int status, string? remoteTag,
        string extraHeaders)
    {
        string to = Header(request, SipHeaderNames.To) ?? "";
        if (remoteTag != null && !to.Contains(";tag=", StringComparison.Ordinal))
        {
            to = to + ";tag=" + remoteTag;
        }
        return Parse("SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + to + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders + "Content-Length: 0\r\n\r\n");
    }

    static void DrainClient(SipSubscriptionClient client)
    {
        while (client.PollSubscriptionEvent().State
            != SipSubscriptionState.None)
        {
        }
    }

    static void DrainServer(SipSubscriptionServer server)
    {
        while (server.PollSubscriptionEvent().State
            != SipSubscriptionState.None)
        {
        }
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType value = new SipMediaType();
        value.Type = type;
        value.Subtype = subtype;
        return value;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
        return parsed.Uri!;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string? Header(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header =>
            string.Equals(header.Name, name,
                StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static void SetHeader(SipMessage message, string name, string value)
    {
        SipHeader header = message.Headers.First(item =>
            string.Equals(item.Name, name,
                StringComparison.OrdinalIgnoreCase));
        header.Value = value;
    }

    sealed class RecordingSubscriptionPolicy
        : ISipSubscriptionAuthorizationPolicy
    {
        internal SipAuthorizationDecision Decision =
            new SipAuthorizationDecision();
        internal SipSubscriptionAuthorizationContext? Context;

        public SipAuthorizationDecision AuthorizeSubscription(
            SipSubscriptionAuthorizationContext context)
        {
            Context = context;
            return Decision;
        }
    }
}
