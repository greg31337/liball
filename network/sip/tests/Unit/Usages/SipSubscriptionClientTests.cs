namespace sip.Tests;

public class SipSubscriptionClientTests
{
    [Test]
    public void InitialSubscriptionBuildsRequiredHeadersAndQueuesEvent()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.EventId = "line-one";
        request.CorrelationId = "subscription-one";

        SipSubscriptionEvent sent = client.CreateSubscription(request);
        SipSubscriptionEvent queued = client.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(sent.State, Is.EqualTo(SipSubscriptionState.Pending));
            Assert.That(sent.Message!.Method, Is.EqualTo(SipMethod.Subscribe));
            Assert.That(Header(sent.Message, SipHeaderNames.Event),
                Is.EqualTo("presence;id=line-one"));
            Assert.That(Header(sent.Message, SipHeaderNames.Expires),
                Is.EqualTo("3600"));
            Assert.That(sent.Message.ContactValues, Has.Length.EqualTo(1));
            Assert.That(sent.SubscriptionId, Is.Not.Empty);
            Assert.That(sent.CorrelationId, Is.EqualTo("subscription-one"));
            Assert.That(queued.SubscriptionId, Is.EqualTo(sent.SubscriptionId));
        });
    }

    [Test]
    public void NotifyBeforeSuccessResponseEstablishesSubscription()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());
        SipMessage notify = Notify(sent.Message!, "remote-one", 1,
            "presence", null, "active;expires=300");

        SipSubscriptionEvent active = client.ConsumeNotify(notify);
        SipMessage response = Response(sent.Message!, 200, "remote-one", "");
        SipSubscriptionEvent accepted = client.ReceiveSubscriptionResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(active.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(active.Accepted, Is.True);
            Assert.That(active.Response!.StatusCode, Is.EqualTo(200));
            Assert.That(active.Version, Is.EqualTo(1));
            Assert.That(active.NotifyCSeq, Is.EqualTo(1));
            Assert.That(active.ExpiresAtMilliseconds, Is.EqualTo(300000));
            Assert.That(accepted.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(accepted.DialogId!.RemoteTag, Is.EqualTo("remote-one"));
        });
    }

    [Test]
    public void ForkedNotifiesCreateIndependentBranches()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.EventId = "watcher";
        SipSubscriptionEvent sent = client.CreateSubscription(request);

        SipSubscriptionEvent first = client.ConsumeNotify(Notify(
            sent.Message!, "remote-one", 1, "presence", "watcher",
            "pending;expires=120"));
        SipSubscriptionEvent second = client.ConsumeNotify(Notify(
            sent.Message!, "remote-two", 1, "presence", "watcher",
            "active;expires=240"));

        Assert.Multiple(() =>
        {
            Assert.That(first.DialogId!.RemoteTag, Is.EqualTo("remote-one"));
            Assert.That(second.DialogId!.RemoteTag, Is.EqualTo("remote-two"));
            Assert.That(second.Forked, Is.True);
            Assert.That(second.SubscriptionId, Is.Not.EqualTo(first.SubscriptionId));
            Assert.That(first.ExpiresSeconds, Is.EqualTo(120));
            Assert.That(second.ExpiresSeconds, Is.EqualTo(240));
        });
    }

    [Test]
    public void EventIdentifierMismatchDoesNotCorrelate()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.EventId = "expected";
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        SipMessage notify = Notify(sent.Message!, "remote-one", 1,
            "presence", "different", "active;expires=300");

        Assert.That(() => client.ConsumeNotify(notify),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void DuplicateNotifyIsRejectedWithoutAdvancingVersion()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());
        SipMessage notify = Notify(sent.Message!, "remote-one", 7,
            "presence", null, "active;expires=300");
        SipSubscriptionEvent first = client.ConsumeNotify(notify);

        SipSubscriptionEvent duplicate = client.ConsumeNotify(notify);

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(duplicate.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.ServerInternalError));
            Assert.That(duplicate.Version, Is.EqualTo(1));
            Assert.That(duplicate.ErrorMessage, Does.Contain("out of order"));
        });
    }

    [Test]
    public void IntervalTooBriefRetriesWithMinimumAndIncreasingCSeq()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.ExpiresSeconds = 120;
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        SipMessage response = Response(sent.Message!, 423, null,
            "Min-Expires: 600\r\n");

        SipSubscriptionEvent retry = client.ReceiveSubscriptionResponse(response);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Final, Is.False);
            Assert.That(retry.Message!.CSeq!.SequenceNumber,
                Is.EqualTo(sent.Message!.CSeq!.SequenceNumber + 1));
            Assert.That(Header(retry.Message, SipHeaderNames.Expires),
                Is.EqualTo("600"));
            Assert.That(retry.Reason, Is.EqualTo("minimum-expiry"));
        });
    }

    [Test]
    public void RefreshTimerCreatesInDialogSubscribeWithIncreasingCSeq()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.ExpiresSeconds = 120;
        request.Options.RefreshBeforeSeconds = 20;
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        SipSubscriptionEvent active = client.ConsumeNotify(Notify(
            sent.Message!, "remote-one", 1, "presence", null,
            "active;expires=120"));
        Drain(client);

        client.AdvanceSubscriptionTime(100000);
        SipSubscriptionEvent refresh = client.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(active.RefreshAtMilliseconds, Is.EqualTo(100000));
            Assert.That(refresh.Reason, Is.EqualTo("refresh"));
            Assert.That(refresh.Message!.CSeq!.SequenceNumber,
                Is.EqualTo(sent.Message!.CSeq!.SequenceNumber + 1));
            Assert.That(SipReferCore.ReadTag(refresh.Message.To),
                Is.EqualTo("remote-one"));
        });
    }

    [Test]
    public void TerminatedNotifySchedulesAndExecutesRetry()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());
        SipSubscriptionEvent terminated = client.ConsumeNotify(Notify(
            sent.Message!, "remote-one", 1, "presence", null,
            "terminated;reason=probation;retry-after=10"));
        Drain(client);

        client.AdvanceSubscriptionTime(9999);
        SipSubscriptionEvent early = client.PollSubscriptionEvent();
        client.AdvanceSubscriptionTime(10000);
        SipSubscriptionEvent retry = client.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Final, Is.True);
            Assert.That(terminated.RetryAtMilliseconds, Is.EqualTo(10000));
            Assert.That(early.State, Is.EqualTo(SipSubscriptionState.None));
            Assert.That(retry.State, Is.EqualTo(SipSubscriptionState.Pending));
            Assert.That(retry.Message!.Method, Is.EqualTo(SipMethod.Subscribe));
            Assert.That(retry.Reason, Is.EqualTo("retry"));
        });
    }

    [Test]
    public void ApplicationTerminationSendsZeroExpirySubscribe()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());
        SipSubscriptionEvent active = client.ConsumeNotify(Notify(
            sent.Message!, "remote-one", 1, "presence", null,
            "active;expires=300"));
        Drain(client);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = active.SubscriptionId;
        cancellation.Reason = "application";

        client.TerminateSubscription(cancellation);
        SipSubscriptionEvent terminating = client.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(Header(terminating.Message!, SipHeaderNames.Expires),
                Is.EqualTo("0"));
            Assert.That(terminating.Reason, Is.EqualTo("application"));
            Assert.That(terminating.Final, Is.False);
        });
    }

    [Test]
    public void ExplicitInDialogSubscriptionUsesDialogRouting()
    {
        SipDialogLayer layer = new SipDialogLayer();
        SipDialogSnapshot dialog = CreateDialog(layer);
        SipSubscriptionClient client = new SipSubscriptionClient();
        client.ConfigureSubscriptionDialogLayer(layer);
        SipSubscriptionRequest request = Request();
        request.DialogId = dialog.Id;
        request.Sender = null;
        request.Target = null;
        request.Contact = null;

        SipSubscriptionEvent sent = client.CreateSubscription(request);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Message!.RequestUri!.Host,
                Is.EqualTo("device.example.com"));
            Assert.That(sent.Message.CSeq!.SequenceNumber, Is.EqualTo(11));
            Assert.That(Header(sent.Message, SipHeaderNames.CallId),
                Is.EqualTo("subscription-dialog"));
            Assert.That(Header(sent.Message, SipHeaderNames.Event),
                Is.EqualTo("presence"));
        });
    }

    [TestCase("deactivated", "", -1, 0L)]
    [TestCase("probation", ";retry-after=11", 11, 11000L)]
    [TestCase("rejected", "", -1, -1L)]
    [TestCase("timeout", "", -1, 0L)]
    [TestCase("giveup", ";retry-after=11", 11, 11000L)]
    [TestCase("noresource", "", -1, -1L)]
    [TestCase("invariant", "", -1, -1L)]
    public void TerminationReasonsApplyRequiredRetryPolicy(
        string reason, string parameters, int retryAfter,
        long retryAt)
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());

        SipSubscriptionEvent terminated = client.ConsumeNotify(Notify(
            sent.Message!, "remote-one", 1, "presence", null,
            "terminated;reason=" + reason + parameters));

        Assert.Multiple(() =>
        {
            Assert.That(terminated.State,
                Is.EqualTo(SipSubscriptionState.Terminated));
            Assert.That(terminated.Final, Is.True);
            Assert.That(terminated.Reason, Is.EqualTo(reason));
            Assert.That(terminated.RetryAfterSeconds,
                Is.EqualTo(retryAfter));
            Assert.That(terminated.RetryAtMilliseconds,
                Is.EqualTo(retryAt));
        });
    }

    [Test]
    public void ForkLimitRejectsAdditionalRemoteBranch()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.Options.MaxForks = 1;
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        client.ConsumeNotify(Notify(sent.Message!, "remote-one", 1,
            "presence", null, "active;expires=300"));

        Assert.That(() => client.ConsumeNotify(Notify(
            sent.Message!, "remote-two", 1, "presence", null,
            "active;expires=300")),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void NonStrictOrderingAcceptsRepeatedNotifyCSeq()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.Options.StrictNotifyOrdering = false;
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        SipMessage notify = Notify(sent.Message!, "remote-one", 7,
            "presence", null, "active;expires=300");
        SipSubscriptionEvent first = client.ConsumeNotify(notify);

        SipSubscriptionEvent repeated = client.ConsumeNotify(notify);

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(repeated.Accepted, Is.True);
            Assert.That(repeated.StatusCode, Is.EqualTo(200));
            Assert.That(repeated.Version, Is.EqualTo(2));
            Assert.That(repeated.NotifyCSeq, Is.EqualTo(7));
        });
    }

    [Test]
    public void InvalidMinimumExpiryTerminatesOutstandingRequest()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.ExpiresSeconds = 120;
        request.Options.MaxExpiresSeconds = 300;
        SipSubscriptionEvent sent = client.CreateSubscription(request);

        SipSubscriptionEvent failed =
            client.ReceiveSubscriptionResponse(Response(
                sent.Message!, SipStatusCodes.IntervalTooBrief, null,
                "Min-Expires: 301\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(SipSubscriptionState.Terminated));
            Assert.That(failed.Final, Is.True);
            Assert.That(failed.ErrorMessage,
                Does.Contain("acceptable Min-Expires"));
            Assert.That(() => client.ReceiveSubscriptionResponse(Response(
                sent.Message!, SipStatusCodes.Ok, "remote-one", "")),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void InconsistentResponseAndNotifyMethodTokensAreRejected()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionEvent sent = client.CreateSubscription(Request());
        SipMessage response = Response(
            sent.Message!, SipStatusCodes.Ok, "remote-one", "");
        response.CSeq!.MethodToken = SipMethods.Info;
        SipMessage notify = Notify(sent.Message!, "remote-one", 1,
            "presence", null, "active;expires=300");
        notify.MethodToken = SipMethods.Options;

        Assert.Multiple(() =>
        {
            Assert.That(() => client.ReceiveSubscriptionResponse(response),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => client.ConsumeNotify(notify),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void ExpiryWithoutRefreshLeadTerminatesClientBranch()
    {
        SipSubscriptionClient client = new SipSubscriptionClient();
        SipSubscriptionRequest request = Request();
        request.ExpiresSeconds = 60;
        request.Options.RefreshBeforeSeconds = 0;
        SipSubscriptionEvent sent = client.CreateSubscription(request);
        client.ConsumeNotify(Notify(sent.Message!, "remote-one", 1,
            "presence", null, "active;expires=60"));
        client.ReceiveSubscriptionResponse(Response(
            sent.Message!, SipStatusCodes.Ok, "remote-one",
            "Expires: 60\r\n"));
        Drain(client);

        client.AdvanceSubscriptionTime(60000);
        SipSubscriptionEvent expired = client.PollSubscriptionEvent();

        Assert.Multiple(() =>
        {
            Assert.That(expired.State,
                Is.EqualTo(SipSubscriptionState.Terminated));
            Assert.That(expired.Final, Is.True);
            Assert.That(expired.Reason, Is.EqualTo("timeout"));
            Assert.That(expired.ErrorMessage, Does.Contain("expired"));
        });
    }

    static SipSubscriptionRequest Request()
    {
        SipSubscriptionRequest request = new SipSubscriptionRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri("sip:bob@example.com");
        request.Contact = Uri("sip:alice@device.example.com");
        request.EventPackage = "presence";
        return request;
    }

    static SipMessage Response(
        SipMessage request, int status, string? remoteTag,
        string extraHeaders)
    {
        return Parse("SIP/2.0 " + status + " Result\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + (remoteTag == null ? "" : ";tag=" + remoteTag) + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + extraHeaders + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage Notify(
        SipMessage subscribe, string remoteTag, int cseq,
        string eventPackage, string? eventId, string state)
    {
        string localTag = SipReferCore.ReadTag(subscribe.From)!;
        string eventValue = eventPackage
            + (eventId == null ? "" : ";id=" + eventId);
        return Parse("NOTIFY sip:alice@device.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP server.example.com;branch=z9hG4bK-notify-"
            + remoteTag + "-" + cseq + "\r\nMax-Forwards: 70\r\n"
            + "From: <sip:bob@example.com>;tag=" + remoteTag + "\r\n"
            + "To: <sip:alice@example.com>;tag=" + localTag + "\r\n"
            + "Call-ID: " + Header(subscribe, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + cseq + " NOTIFY\r\n"
            + "Contact: <sip:bob@server.example.com>\r\n"
            + "Event: " + eventValue + "\r\n"
            + "Subscription-State: " + state + "\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipDialogSnapshot CreateDialog(SipDialogLayer layer)
    {
        SipMessage invite = Parse("INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-sub\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: subscription-dialog\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:alice@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipMessage response = Parse("SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-sub\r\n"
            + "From: <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: <sip:bob@example.com>;tag=remote-tag\r\n"
            + "Call-ID: subscription-dialog\r\nCSeq: 10 INVITE\r\n"
            + "Contact: <sip:bob@device.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return layer.CreateDialog(invite, response, new SipDialogOptions());
    }

    static void Drain(SipSubscriptionClient client)
    {
        while (client.PollSubscriptionEvent().State
            != SipSubscriptionState.None)
        {
        }
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
}
