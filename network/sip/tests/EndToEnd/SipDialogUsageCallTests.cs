namespace sip.Tests;

public class SipDialogUsageCallTests
{
    [Test]
    public void ReliableInviteCompletesOfferAnswerPrackAndSessionRefresh()
    {
        using SipInProcessHarness harness = new SipInProcessHarness();
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Invite("dialog-usage", 42, Offer());
        command.Options.ReliableProvisional = true;
        command.Options.SessionExpiresSeconds = 120;
        command.Options.MinSessionExpiresSeconds = 90;

        SipInviteEvent started = session.StartInvite(command);
        DrainInviteEvents(session);
        SipMessage receivedInvite = SendRequest(harness, started.Message!,
            SipTransportKind.Udp, "invite");
        SipMessage progress = ReturnResponse(harness,
            Response(receivedInvite, 183, "Session Progress", "remote-tag",
                "Contact: <sip:bob@uas.test>\r\n"
                    + "Require: 100rel\r\nRSeq: 1\r\n", Answer()),
            SipTransportKind.Udp, "progress");
        SipInviteEvent early = session.ReceiveInviteMessage(progress);
        SipMessage prack = PollInviteMessage(session, SipMethod.Prack);
        SipMessage receivedPrack = SendRequest(harness, prack,
            SipTransportKind.Udp, "prack");
        session.ReceiveInviteMessage(ReturnResponse(harness,
            Response(receivedPrack, 200, "OK", "remote-tag", null,
                new byte[0]), SipTransportKind.Udp, "prack-ok"));
        DrainInviteEvents(session);

        SipMessage accepted = ReturnResponse(harness,
            Response(receivedInvite, 200, "OK", "remote-tag",
                "Contact: <sip:bob@uas.test>\r\n"
                    + "Require: timer\r\n"
                    + "Allow: INVITE, ACK, BYE, UPDATE\r\n"
                    + "Session-Expires: 120;refresher=uac\r\n",
                new byte[0]), SipTransportKind.Udp, "invite-ok");
        SipInviteEvent confirmed = session.ReceiveInviteMessage(accepted);
        SipMessage ack = PollInviteMessage(session, SipMethod.Ack);
        SipMessage receivedAck = SendRequest(harness, ack,
            SipTransportKind.Udp, "ack");
        DrainInviteEvents(session);

        session.AdvanceInviteTime(60000);
        SipMessage update = PollInviteMessage(session, SipMethod.Update);
        SipMessage receivedUpdate = SendRequest(harness, update,
            SipTransportKind.Udp, "refresh");
        SipMessage updateOk = ReturnResponse(harness,
            Response(receivedUpdate, 200, "OK", "remote-tag",
                "Contact: <sip:bob@uas.test>\r\n"
                    + "Session-Expires: 120;refresher=uac\r\n",
                new byte[0]), SipTransportKind.Udp, "refresh-ok");
        SipInviteEvent refreshed = session.ReceiveInviteMessage(updateOk);

        Assert.Multiple(() =>
        {
            Assert.That(early.State, Is.EqualTo(SipInviteSessionState.Early));
            Assert.That(early.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(early.OfferAnswer.NegotiatedBody, Is.EqualTo(Answer()));
            Assert.That(Header(prack, SipHeaderNames.RAck),
                Is.EqualTo("1 42 INVITE"));
            Assert.That(confirmed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(receivedAck.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(update.RequestUri!.Host, Is.EqualTo("uas.test"));
            Assert.That(Header(update, SipHeaderNames.SessionExpires),
                Is.EqualTo("120;refresher=uac"));
            Assert.That(refreshed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
        });
    }

    [Test]
    public void MessageInfoAndReferWithReplacesCrossTheDialogTransport()
    {
        using SipInProcessHarness messageHarness = new SipInProcessHarness();
        SipMessageClient messageClient = new SipMessageClient();
        SipInstantMessageRequest messageRequest = new SipInstantMessageRequest();
        messageRequest.Sender = Uri("sip:alice@example.com");
        messageRequest.Target = Uri("sip:bob@uas.test");
        messageRequest.Body = Bytes("hello in dialog");
        messageRequest.ContentType = MediaType("text", "plain");
        SipInstantMessageEvent messageSent =
            messageClient.SendInstantMessage(messageRequest);
        SipMessage receivedMessage = SendRequest(messageHarness,
            messageSent.Message!, SipTransportKind.Tcp, "message");
        SipInstantMessageEvent messageAccepted = new SipMessageServer()
            .ReceiveInstantMessage(receivedMessage,
                new SipInstantMessageServerOptions());
        SipMessage messageResponse = ReturnResponse(messageHarness,
            messageAccepted.Response!, SipTransportKind.Tcp, "message-ok");
        SipInstantMessageEvent delivered =
            messageClient.ReceiveInstantMessageResponse(messageResponse);

        using SipInProcessHarness infoHarness = new SipInProcessHarness();
        RecordingEndToEndInfoPackage handler =
            new RecordingEndToEndInfoPackage();
        SipInfoPackageFramework info = new SipInfoPackageFramework();
        info.RegisterInfoPackage("dtmf", handler,
            new SipMediaType[] { MediaType("application", "dtmf-relay") });
        SipDialogId dialog = DialogId("usage-dialog", "bob-tag", "alice-tag");
        SipMessage sentInfo = InDialogRequest("INFO", "usage-dialog", 43,
            "Info-Package: dtmf\r\n"
                + "Content-Disposition: info-package\r\n"
                + "Content-Type: application/dtmf-relay\r\n",
            "Signal=5\r\nDuration=160");
        SipMessage receivedInfo = SendRequest(infoHarness, sentInfo,
            SipTransportKind.Tcp, "info");
        SipInfoResult infoAccepted = info.ReceiveInfo(dialog,
            receivedInfo, new SipInfoOptions());
        SipMessage infoResponse = ReturnResponse(infoHarness,
            infoAccepted.Response!, SipTransportKind.Tcp, "info-ok");

        using SipInProcessHarness referHarness = new SipInProcessHarness();
        SipReferClient referClient = new SipReferClient();
        SipReferRequest referRequest = ReferRequest();
        SipReferEvent referSent = referClient.SendRefer(referRequest);
        SipMessage receivedRefer = SendRequest(referHarness,
            referSent.Message!, SipTransportKind.Tcp, "refer");
        SipReferServer referServer = new SipReferServer();
        SipReferEvent referAccepted = referServer.ReceiveRefer(
            null, receivedRefer, new SipReferServerOptions());
        SipMessage referResponse = ReturnResponse(referHarness,
            referAccepted.Response!, SipTransportKind.Tcp, "refer-accepted");
        SipReferEvent pending = referClient.ReceiveReferResponse(referResponse);
        SipMessage notify = SendReverseRequest(referHarness,
            referAccepted.Notification!, SipTransportKind.Tcp, "refer-notify");
        SipReferEvent notified = referClient.ReceiveReferNotification(notify);
        ReturnReverseResponse(referHarness, notified.Response!,
            SipTransportKind.Tcp, "refer-notify-ok");

        Assert.Multiple(() =>
        {
            Assert.That(delivered.Kind,
                Is.EqualTo(SipInstantMessageEventKind.Delivered));
            Assert.That(messageAccepted.Message!.Body,
                Is.EqualTo(Bytes("hello in dialog")));
            Assert.That(infoAccepted.Accepted, Is.True,
                infoAccepted.ErrorMessage);
            Assert.That(infoResponse.StatusCode, Is.EqualTo(200));
            Assert.That(handler.Body,
                Is.EqualTo(Bytes("Signal=5\r\nDuration=160")));
            Assert.That(Header(receivedRefer, SipHeaderNames.ReferTo),
                Does.Contain("Replaces="));
            Assert.That(pending.State, Is.EqualTo(SipReferState.Accepted));
            Assert.That(notified.State, Is.EqualTo(SipReferState.Pending));
            Assert.That(notified.StatusCode, Is.EqualTo(100));
        });
    }

    [Test]
    public void PresencePublishSubscribeAndNotifyCompleteEndToEnd()
    {
        using SipInProcessHarness publishHarness = new SipInProcessHarness();
        SipPublicationClient publisher = new SipPublicationClient();
        SipPublicationRequest publishRequest = new SipPublicationRequest();
        publishRequest.Sender = Uri("sip:alice@example.com");
        publishRequest.Target = Uri("sip:alice@uas.test");
        publishRequest.EventPackage = "presence";
        publishRequest.ExpiresSeconds = 300;
        publishRequest.Body = Pidf("open");
        publishRequest.ContentType = MediaType("application", "pidf+xml");
        SipPublicationEvent publishSent =
            publisher.CreatePublication(publishRequest);
        SipMessage receivedPublish = SendRequest(publishHarness,
            publishSent.Message!, SipTransportKind.Tcp, "publish");
        SipPublicationRequest serverPublish = new SipPublicationRequest();
        serverPublish.Message = receivedPublish;
        SipPublicationServer publicationServer = new SipPublicationServer();
        SipPublicationEvent stored =
            publicationServer.ProcessPublication(serverPublish);
        SipMessage publishResponse = ReturnResponse(publishHarness,
            stored.Response!, SipTransportKind.Tcp, "publish-ok");
        SipPublicationEvent published =
            publisher.ReceivePublicationResponse(publishResponse);

        using SipInProcessHarness subscribeHarness = new SipInProcessHarness();
        SipSubscriptionClient subscriber = new SipSubscriptionClient();
        SipSubscriptionRequest subscribeRequest = new SipSubscriptionRequest();
        subscribeRequest.Sender = Uri("sip:bob@example.net");
        subscribeRequest.Target = Uri("sip:alice@uas.test");
        subscribeRequest.Contact = Uri("sip:bob@caller.example.net");
        subscribeRequest.EventPackage = "presence";
        subscribeRequest.ExpiresSeconds = 300;
        SipSubscriptionEvent subscribeSent =
            subscriber.CreateSubscription(subscribeRequest);
        SipMessage receivedSubscribe = SendRequest(subscribeHarness,
            subscribeSent.Message!, SipTransportKind.Tcp, "subscribe");
        SipSubscriptionRequest serverSubscribe = new SipSubscriptionRequest();
        serverSubscribe.Message = receivedSubscribe;
        SipSubscriptionServer subscriptionServer = new SipSubscriptionServer();
        SipSubscriptionEvent accepted =
            subscriptionServer.ProcessSubscription(serverSubscribe);
        SipMessage subscribeResponse = ReturnResponse(subscribeHarness,
            accepted.Response!, SipTransportKind.Tcp, "subscribe-ok");
        SipSubscriptionEvent response =
            subscriber.ReceiveSubscriptionResponse(subscribeResponse);
        SipMessage receivedNotify = SendReverseRequest(subscribeHarness,
            accepted.Notification!, SipTransportKind.Tcp, "notify");
        SipSubscriptionEvent active = subscriber.ConsumeNotify(receivedNotify);
        SipMessage notifyResponse = ReturnReverseResponse(subscribeHarness,
            active.Response!, SipTransportKind.Tcp, "notify-ok");
        SipSubscriptionEvent notified =
            subscriptionServer.ReceiveNotifyResponse(notifyResponse);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Accepted, Is.True, stored.ErrorMessage);
            Assert.That(stored.AggregationRequired, Is.True);
            Assert.That(publicationServer.GetPublications()[0].Body,
                Is.EqualTo(Pidf("open")));
            Assert.That(published.State,
                Is.EqualTo(SipPublicationState.Published));
            Assert.That(published.EntityTag, Is.Not.Empty);
            Assert.That(response.State,
                Is.EqualTo(SipSubscriptionState.Pending));
            Assert.That(active.Accepted, Is.True);
            Assert.That(active.State, Is.EqualTo(SipSubscriptionState.Active));
            Assert.That(notified.Accepted, Is.True);
            Assert.That(Header(receivedNotify, SipHeaderNames.Event),
                Is.EqualTo("presence"));
        });
    }

    [Test]
    public void BackToBackMessageMappingKeepsTransportLegsIsolated()
    {
        using SipInProcessHarness leftHarness = new SipInProcessHarness();
        using SipInProcessHarness rightHarness = new SipInProcessHarness();
        SipMessage leftInvite = SendRequest(leftHarness, BackToBackInvite(
            "left-usage", 10, "caller.example.com", "left-branch",
            "Caller <sip:caller@example.com>;tag=caller-tag",
            "Service <sip:service@example.com>",
            "sip:service@relay-left.example.com",
            "sip:caller@caller.example.com", "left offer"),
            SipTransportKind.Udp, "left-invite");
        SipInviteRequest right = new SipInviteRequest();
        right.Message = BackToBackInvite(
            "right-usage", 70, "relay-right.example.com", "right-branch",
            "Relay <sip:relay@example.com>;tag=relay-tag",
            "Callee <sip:callee@example.com>",
            "sip:callee@callee.example.com",
            "sip:relay@relay-right.example.com", "right offer");
        right.AuthenticationContextId = "right-auth";
        right.CorrelationId = "bridge-usage";
        SipBackToBackSession session = new SipBackToBackSession();

        SipBackToBackEvent created = session.CreateBackToBackSession(
            BackToBackLeft(leftInvite), right);
        SipMessage rightInvite = SendRequest(rightHarness,
            created.RightMessage!, SipTransportKind.Tcp, "right-invite");
        SipMessage rightOk = ReturnResponse(rightHarness,
            Response(rightInvite, 200, "OK", "callee-tag",
                "Contact: <sip:callee@callee.example.com>\r\n",
                Bytes("right answer")),
            SipTransportKind.Tcp, "right-ok");
        SipMessage mappedLeftOk = Response(leftInvite, 200, "OK",
            "relay-left-tag",
            "Contact: <sip:service@relay-left.example.com>\r\n",
            Bytes("left answer"));
        SipBackToBackEvent confirmed = session.ApplyBackToBackMapping(
            Mapping(SipBackToBackLeg.Right, rightOk, mappedLeftOk,
                "mapped-answer"));
        SipMessage leftOk = ReturnResponse(leftHarness,
            confirmed.LeftMessage!, SipTransportKind.Udp, "left-ok");

        SipMessage leftAck = BackToBackInDialogRequest(
            "ACK", "left-usage", 10, "caller.example.com", "left-ack",
            "Caller <sip:caller@example.com>;tag=caller-tag",
            "Service <sip:service@example.com>;tag=relay-left-tag",
            "sip:service@relay-left.example.com", "", null);
        session.MapBackToBackMessage(SendRequest(leftHarness, leftAck,
            SipTransportKind.Udp, "left-ack"));
        SipMessage leftMessage = BackToBackInDialogRequest(
            "MESSAGE", "left-usage", 11, "caller.example.com",
            "left-message",
            "Caller <sip:caller@example.com>;tag=caller-tag",
            "Service <sip:service@example.com>;tag=relay-left-tag",
            "sip:service@relay-left.example.com", "left private body",
            "text/plain");
        SipMessage receivedLeftMessage = SendRequest(leftHarness, leftMessage,
            SipTransportKind.Tcp, "left-message");
        SipMessage rightMessage = BackToBackInDialogRequest(
            "MESSAGE", "right-usage", 71, "relay-right.example.com",
            "right-message", "Relay <sip:relay@example.com>;tag=relay-tag",
            "Callee <sip:callee@example.com>;tag=callee-tag",
            "sip:callee@callee.example.com", "mapped public body",
            "text/plain");
        SipBackToBackEvent mapped = session.ApplyBackToBackMapping(
            Mapping(SipBackToBackLeg.Left, receivedLeftMessage, rightMessage,
                "mapped-message"));
        SipMessage receivedRightMessage = SendRequest(rightHarness,
            mapped.RightMessage!, SipTransportKind.Tcp, "right-message");
        SipBackToBackEvent current = session.GetBackToBackSession();

        Assert.Multiple(() =>
        {
            Assert.That(Header(leftOk, SipHeaderNames.CallId),
                Is.EqualTo("left-usage"));
            Assert.That(confirmed.State,
                Is.EqualTo(SipBackToBackState.Confirmed));
            Assert.That(confirmed.LeftDialog!.Id!.CallId,
                Is.EqualTo("left-usage"));
            Assert.That(confirmed.RightDialog!.Id!.CallId,
                Is.EqualTo("right-usage"));
            Assert.That(confirmed.LeftMessage!.Body,
                Is.EqualTo(Bytes("left answer")));
            Assert.That(confirmed.RightMessage!.Body,
                Is.EqualTo(Bytes("right answer")));
            Assert.That(Header(receivedRightMessage, SipHeaderNames.CallId),
                Is.EqualTo("right-usage"));
            Assert.That(receivedRightMessage.CSeq!.SequenceNumber,
                Is.EqualTo(71));
            Assert.That(receivedRightMessage.Body,
                Is.EqualTo(Bytes("mapped public body")));
            Assert.That(mapped.LeftMessage!.Body,
                Is.EqualTo(Bytes("left private body")));
            Assert.That(current.LeftInvite!.AuthenticationContextId,
                Is.EqualTo("left-auth"));
            Assert.That(current.RightInvite!.AuthenticationContextId,
                Is.EqualTo("right-auth"));
            Assert.That(current.CorrelationId, Is.EqualTo("mapped-message"));
        });
    }

    static SipInviteRequest BackToBackLeft(SipMessage invite)
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Message = invite;
        request.AuthenticationContextId = "left-auth";
        return request;
    }

    static SipBackToBackMapping Mapping(SipBackToBackLeg sourceLeg,
        SipMessage source, SipMessage mapped, string correlationId)
    {
        SipBackToBackMapping mapping = new SipBackToBackMapping();
        mapping.SourceLeg = sourceLeg;
        mapping.SourceMessage = source;
        mapping.MappedMessage = mapped;
        mapping.CorrelationId = correlationId;
        return mapping;
    }

    static SipMessage SendRequest(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUac(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas).First(value =>
            value.Kind == SipStackEventKind.IncomingRequest).Message!;
    }

    static SipMessage ReturnResponse(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUas(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac).First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null
            && value.TransactionEvent.Message.Kind == SipMessageKind.Response)
            .TransactionEvent!.Message!;
    }

    static SipMessage SendReverseRequest(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUas(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uas);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uac).First(value =>
            value.Kind == SipStackEventKind.IncomingRequest).Message!;
    }

    static SipMessage ReturnReverseResponse(SipInProcessHarness harness,
        SipMessage message, SipTransportKind transport, string correlationId)
    {
        SipSendResult sent = harness.SendFromUac(
            message, transport, correlationId);
        Assert.That(sent.Success, Is.True, sent.ErrorMessage);
        SipInProcessHarness.Drain(harness.Uac);
        harness.DeliverPackets();
        return SipInProcessHarness.Drain(harness.Uas).First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.TransactionEvent!.Message != null
            && value.TransactionEvent.Message.Kind == SipMessageKind.Response)
            .TransactionEvent!.Message!;
    }

    static SipMessage PollInviteMessage(SipInviteSession session,
        SipMethod method)
    {
        SipInviteEvent value = session.PollInviteEvent();
        while (value.State != SipInviteSessionState.None)
        {
            if (value.Message != null && value.Message.Method == method)
            {
                return value.Message;
            }
            value = session.PollInviteEvent();
        }
        Assert.Fail("The expected invite-usage message was not queued.");
        return new SipMessage();
    }

    static void DrainInviteEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static SipMessage Invite(string callId, int cseq, byte[] body)
    {
        return Parse("INVITE sip:bob@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch=z9hG4bK-invite\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@example.com>\r\n"
            + "Call-ID: " + callId + "@example.com\r\n"
            + "CSeq: " + cseq + " INVITE\r\n"
            + "Contact: <sip:alice@caller.example.com>\r\n"
            + BodyHeaders("application/sdp", body));
    }

    static SipMessage BackToBackInvite(string callId, int cseq,
        string viaHost, string branch, string from, string to,
        string requestUri, string contact, string body)
    {
        byte[] bytes = Bytes(body);
        return Parse("INVITE " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP " + viaHost + ";branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: " + from + "\r\nTo: " + to + "\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq
            + " INVITE\r\nContact: <" + contact + ">\r\n"
            + BodyHeaders("application/sdp", bytes));
    }

    static SipMessage BackToBackInDialogRequest(string method, string callId,
        int cseq, string viaHost, string branch, string from, string to,
        string requestUri, string body, string? contentType)
    {
        byte[] bytes = Bytes(body);
        return Parse(method + " " + requestUri + " SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP " + viaHost + ";branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: " + from + "\r\nTo: " + to + "\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq + " "
            + method + "\r\n" + BodyHeaders(contentType, bytes));
    }

    static SipMessage Response(SipMessage request, int status, string reason,
        string toTag, string? extraHeaders, byte[] body)
    {
        string? contentType = body.Length == 0 ? null : "application/sdp";
        return Parse("SIP/2.0 " + status + " " + reason + "\r\n"
            + "Via: " + Header(request, SipHeaderNames.Via) + "\r\n"
            + "From: " + Header(request, SipHeaderNames.From) + "\r\n"
            + "To: " + Header(request, SipHeaderNames.To)
            + (Header(request, SipHeaderNames.To).Contains(";tag=")
                ? "" : ";tag=" + toTag) + "\r\n"
            + "Call-ID: " + Header(request, SipHeaderNames.CallId) + "\r\n"
            + "CSeq: " + Header(request, SipHeaderNames.CSeq) + "\r\n"
            + (extraHeaders ?? "") + BodyHeaders(contentType, body));
    }

    static SipMessage InDialogRequest(string method, string callId, int cseq,
        string extraHeaders, string body)
    {
        byte[] bytes = Bytes(body);
        return Parse(method + " sip:bob@uas.test SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP caller.example.com;branch=z9hG4bK-" + method
            + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=alice-tag\r\n"
            + "To: Bob <sip:bob@example.com>;tag=bob-tag\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq + " " + method + "\r\n"
            + extraHeaders + "Content-Length: " + bytes.Length
            + "\r\n\r\n" + body);
    }

    static SipReferRequest ReferRequest()
    {
        SipReferRequest request = new SipReferRequest();
        request.Sender = Uri("sip:alice@example.com");
        request.Target = Uri("sip:bob@uas.test");
        request.Contact = Uri("sip:alice@caller.example.com");
        request.ReferTo = new SipNameAddress();
        request.ReferTo.Uri = Uri("sip:carol@example.net?Replaces="
            + "replacement-call%3Bto-tag%3Dremote%3Bfrom-tag%3Dlocal");
        return request;
    }

    static SipDialogId DialogId(string callId, string localTag,
        string remoteTag)
    {
        SipDialogId id = new SipDialogId();
        id.CallId = callId;
        id.LocalTag = localTag;
        id.RemoteTag = remoteTag;
        return id;
    }

    static string BodyHeaders(string? contentType, byte[] body)
    {
        return (contentType == null ? ""
                : "Content-Type: " + contentType + "\r\n")
            + "Content-Length: " + body.Length + "\r\n\r\n"
            + System.Text.Encoding.ASCII.GetString(body);
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static byte[] Offer()
    {
        return Bytes("v=0\r\no=offer 1 1 IN IP4 192.0.2.10\r\n");
    }

    static byte[] Answer()
    {
        return Bytes("v=0\r\no=answer 1 1 IN IP4 192.0.2.20\r\n");
    }

    static byte[] Pidf(string basic)
    {
        return Bytes("<?xml version=\"1.0\"?>"
            + "<presence xmlns=\"urn:ietf:params:xml:ns:pidf\" "
            + "entity=\"pres:alice@example.com\"><tuple id=\"one\">"
            + "<status><basic>" + basic + "</basic></status>"
            + "</tuple></presence>");
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Header(SipMessage message, string name)
    {
        return message.Headers.First(value => string.Equals(
            value.Name, name, StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }
}

class RecordingEndToEndInfoPackage : ISipInfoPackage
{
    internal byte[] Body = new byte[0];

    public SipInfoResult ProcessInfo(SipInfoRequest request)
    {
        Body = request.Body.ToArray();
        SipInfoResult result = new SipInfoResult();
        result.Accepted = true;
        result.StatusCode = SipStatusCodes.Ok;
        return result;
    }
}
