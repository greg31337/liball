namespace sip.Tests;

public class SipInviteSessionTimerTests
{
    [Test]
    public void NegotiatedUacTimerGeneratesUpdateAtHalfInterval()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand(
            120, 90, "Proxy-Require: timer-proxy\r\n");
        SipInviteEvent started = session.StartInvite(command);
        SipMessage initial = started.Message!;
        DrainEvents(session);

        session.ReceiveInviteMessage(InitialResponse(
            initial, 200, "OK", InitialAnswer(),
            "120;refresher=uac;proxy=edge", null, true, true));
        DrainEvents(session);
        session.AdvanceInviteTime(59999);
        SipInviteEvent early = session.PollInviteEvent();
        session.AdvanceInviteTime(60000);
        SipInviteEvent refresh = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(initial, SipHeaderNames.Supported),
                Does.Contain(SipOptionTags.Timer));
            Assert.That(HeaderValue(initial, SipHeaderNames.SessionExpires),
                Is.EqualTo("120"));
            Assert.That(HeaderValue(initial, SipHeaderNames.MinSE),
                Is.EqualTo("90"));
            Assert.That(early.State, Is.EqualTo(SipInviteSessionState.None));
            Assert.That(refresh.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(refresh.Message!.Method, Is.EqualTo(SipMethod.Update));
            Assert.That(refresh.Reason, Does.Contain("refresh timer"));
            Assert.That(HeaderValue(refresh.Message,
                SipHeaderNames.SessionExpires),
                Is.EqualTo("120;proxy=edge;refresher=uac"));
            Assert.That(HeaderValue(refresh.Message, SipHeaderNames.MinSE),
                Is.EqualTo("90"));
            Assert.That(HeaderValue(refresh.Message,
                SipHeaderNames.ProxyRequire), Is.EqualTo("timer-proxy"));
        });

        session.ReceiveInviteMessage(RefreshResponse(
            refresh.Message!, 491, "Request Pending", null,
            null, null, "5"));
        DrainEvents(session);
        session.AdvanceInviteTime(64999);
        Assert.That(session.PollInviteEvent().State,
            Is.EqualTo(SipInviteSessionState.None));
        session.AdvanceInviteTime(65000);
        Assert.That(session.PollInviteEvent().Message!.Method,
            Is.EqualTo(SipMethod.Update));
    }

    [Test]
    public void Initial422RetriesWithLargerMinSeAndFreshTransaction()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand(
            100, 90, "Session-Expires: 100;origin=caller\r\n"
                + "Proxy-Require: timer-proxy\r\n");
        SipMessage first = session.StartInvite(command).Message!;
        DrainEvents(session);

        SipInviteEvent rejected = session.ReceiveInviteMessage(
            InitialResponse(first, 422, "Session Interval Too Small", null,
                null, "120;proxy=edge", false, false));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();
        SipInviteEvent retryEvent = session.PollInviteEvent();
        SipMessage retry = retryEvent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Reason, Does.Contain("increased Min-SE"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(422));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(retry.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(retry.CSeq!.SequenceNumber,
                Is.EqualTo(first.CSeq!.SequenceNumber + 1));
            Assert.That(retry.ViaValues[0].Branch,
                Is.Not.EqualTo(first.ViaValues[0].Branch));
            Assert.That(HeaderValue(retry, SipHeaderNames.SessionExpires),
                Is.EqualTo("120;origin=caller"));
            Assert.That(HeaderValue(retry, SipHeaderNames.MinSE),
                Is.EqualTo("120;proxy=edge"));
            Assert.That(HeaderValue(retry, SipHeaderNames.ProxyRequire),
                Is.EqualTo("timer-proxy"));
            Assert.That(retry.Body, Is.EqualTo(InitialOffer()));
        });

        DrainEvents(session);
        SipInviteEvent confirmed = session.ReceiveInviteMessage(
            InitialResponse(retry, 200, "OK", InitialAnswer(),
                "120;refresher=uac", null, true, true));
        Assert.That(confirmed.State,
            Is.EqualTo(SipInviteSessionState.Confirmed));
    }

    [Test]
    public void InDialog422RetriesUpdateWithoutRollingBackOfferAnswer()
    {
        SipInviteSession session = ConfirmedOutgoing(
            120, "uac", true);
        SipInviteEvent started = session.StartInvite(
            RefreshCommand(SipMethods.Update, HoldOffer()));
        SipMessage update = started.Message!;
        DrainEvents(session);

        session.ReceiveInviteMessage(RefreshResponse(
            update, 422, "Session Interval Too Small", null,
            null, "180;proxy=edge", null));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent retryEvent = session.PollInviteEvent();
        SipMessage retry = retryEvent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(422));
            Assert.That(retry.Method, Is.EqualTo(SipMethod.Update));
            Assert.That(retry.CSeq!.SequenceNumber,
                Is.EqualTo(update.CSeq!.SequenceNumber + 1));
            Assert.That(retry.Body, Is.EqualTo(HoldOffer()));
            Assert.That(retryEvent.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.LocalOfferPending));
            Assert.That(HeaderValue(retry, SipHeaderNames.SessionExpires),
                Is.EqualTo("180;refresher=uac"));
            Assert.That(HeaderValue(retry, SipHeaderNames.MinSE),
                Is.EqualTo("180;proxy=edge"));
        });

        DrainEvents(session);
        SipInviteEvent completed = session.ReceiveInviteMessage(
            RefreshResponse(retry, 200, "OK", HoldAnswer(),
                "180;refresher=uac", null, null));
        Assert.Multiple(() =>
        {
            Assert.That(completed.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(completed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(completed.OfferAnswer.NegotiatedBody,
                Is.EqualTo(HoldAnswer()));
        });
    }

    [Test]
    public void InDialog422RetriesReinviteWithAckAndFreshTransaction()
    {
        SipInviteSession session = ConfirmedOutgoing(
            120, "uac", true);
        SipMessage reinvite = session.StartInvite(
            RefreshCommand(SipMethods.Invite, HoldOffer())).Message!;
        DrainEvents(session);

        SipInviteEvent rejected = session.ReceiveInviteMessage(RefreshResponse(
            reinvite, 422, "Session Interval Too Small", null,
            null, "180;proxy=edge", null));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent ackEvent = session.PollInviteEvent();
        SipInviteEvent retryEvent = session.PollInviteEvent();
        SipMessage retry = retryEvent.Message!;

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Reason, Does.Contain("increased Min-SE"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(422));
            Assert.That(ackEvent.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(ackEvent.Message.CSeq!.SequenceNumber,
                Is.EqualTo(reinvite.CSeq!.SequenceNumber));
            Assert.That(retry.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(retry.CSeq!.SequenceNumber,
                Is.EqualTo(reinvite.CSeq.SequenceNumber + 1));
            Assert.That(retry.ViaValues[0].Branch,
                Is.Not.EqualTo(reinvite.ViaValues[0].Branch));
            Assert.That(retry.Body, Is.EqualTo(HoldOffer()));
            Assert.That(HeaderValue(retry, SipHeaderNames.SessionExpires),
                Is.EqualTo("180;refresher=uac"));
            Assert.That(HeaderValue(retry, SipHeaderNames.MinSE),
                Is.EqualTo("180;proxy=edge"));
        });

        DrainEvents(session);
        SipInviteEvent completed = session.ReceiveInviteMessage(
            RefreshResponse(retry, 200, "OK", HoldAnswer(),
                "180;refresher=uac", null, null));
        SipInviteEvent completedEvent = session.PollInviteEvent();
        SipInviteEvent completedAck = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(completed.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Completed));
            Assert.That(completedEvent.Message!.StatusCode, Is.EqualTo(200));
            Assert.That(completedAck.Message!.Method, Is.EqualTo(SipMethod.Ack));
            Assert.That(completedAck.Message.CSeq!.SequenceNumber,
                Is.EqualTo(retry.CSeq!.SequenceNumber));
        });
    }

    [Test]
    public void RemoteRefresherExpiryQueuesEventAndBye()
    {
        SipInviteSession session = ConfirmedOutgoing(
            90, "uas", true);

        session.AdvanceInviteTime(89999);
        SipInviteEvent early = session.PollInviteEvent();
        session.AdvanceInviteTime(90000);
        SipInviteEvent expired = session.PollInviteEvent();
        SipInviteEvent bye = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(early.State, Is.EqualTo(SipInviteSessionState.None));
            Assert.That(expired.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(expired.Message, Is.Null);
            Assert.That(expired.Reason, Does.Contain("expired"));
            Assert.That(bye.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(bye.Reason, Does.Contain("timer expired"));
        });
    }

    [Test]
    public void SuccessfulIncomingRefreshRestartsRemoteRefresherDeadline()
    {
        SipInviteSession session = ConfirmedOutgoing(
            90, "uas", true);
        session.AdvanceInviteTime(60000);
        SipMessage update = IncomingDialogUpdate();
        session.ReceiveInviteMessage(update);
        DrainEvents(session);

        SipInviteEvent accepted = session.StartInvite(
            IncomingDialogResponseCommand(update));
        DrainEvents(session);
        session.AdvanceInviteTime(90000);
        SipInviteEvent oldDeadline = session.PollInviteEvent();
        session.AdvanceInviteTime(149999);
        SipInviteEvent beforeNewDeadline = session.PollInviteEvent();
        session.AdvanceInviteTime(150000);
        SipInviteEvent expired = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.State,
                Is.EqualTo(SipInviteSessionState.Confirmed));
            Assert.That(HeaderValue(accepted.Message!,
                SipHeaderNames.SessionExpires),
                Is.EqualTo("90;refresher=uas"));
            Assert.That(oldDeadline.State,
                Is.EqualTo(SipInviteSessionState.None));
            Assert.That(beforeNewDeadline.State,
                Is.EqualTo(SipInviteSessionState.None));
            Assert.That(expired.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(expired.Reason, Does.Contain("expired"));
        });
    }

    [Test]
    public void IncomingNegotiationSelectsUasAndGeneratesReinviteRefresh()
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage invite = IncomingInvite(
            "100;refresher=uas", true, false);
        session.ReceiveInviteMessage(invite);
        DrainEvents(session);

        SipInviteEvent accepted = session.StartInvite(
            IncomingResponseCommand(invite));
        SipMessage response = accepted.Message!;
        DrainEvents(session);
        session.ReceiveInviteMessage(IncomingAck(invite));
        DrainEvents(session);
        session.AdvanceInviteTime(50000);
        SipInviteEvent refresh = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(HeaderValue(response,
                SipHeaderNames.SessionExpires),
                Is.EqualTo("100;refresher=uas"));
            Assert.That(HeaderValue(response, SipHeaderNames.Require),
                Does.Contain(SipOptionTags.Timer));
            Assert.That(refresh.Message!.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(refresh.Message.CSeq!.SequenceNumber, Is.EqualTo(1));
            Assert.That(refresh.Message.Body, Is.EqualTo(InitialAnswer()));
            Assert.That(HeaderValue(refresh.Message,
                SipHeaderNames.SessionExpires),
                Is.EqualTo("100;refresher=uas"));
        });
    }

    [TestCase(408)]
    [TestCase(481)]
    public void FailedAutomaticRefreshTerminatesWithBye(int status)
    {
        SipInviteSession session = ConfirmedOutgoing(
            90, "uac", true);
        session.AdvanceInviteTime(45000);
        SipMessage refresh = session.PollInviteEvent().Message!;

        SipInviteEvent failed = session.ReceiveInviteMessage(
            RefreshResponse(refresh, status, "Refresh Failed", null,
                null, null, null));
        SipInviteEvent responseEvent = session.PollInviteEvent();
        SipInviteEvent byeEvent = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(failed.Reason, Does.Contain("terminated"));
            Assert.That(responseEvent.Message!.StatusCode, Is.EqualTo(status));
            Assert.That(byeEvent.Message!.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(byeEvent.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
        });
    }

    [Test]
    public void TooSmallIncomingIntervalProduces422WithConfiguredMinSe()
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage invite = IncomingInvite(
            "60;refresher=uas", true, false);
        session.ReceiveInviteMessage(invite);
        DrainEvents(session);

        SipInviteRequest responseCommand = IncomingResponseCommand(invite);
        responseCommand.Options.MinSessionExpiresSeconds = 120;
        responseCommand.Options.SessionExpiresSeconds = 180;
        SipInviteEvent rejected = session.StartInvite(responseCommand);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(rejected.Message!.StatusCode, Is.EqualTo(422));
            Assert.That(HeaderValue(rejected.Message, SipHeaderNames.MinSE),
                Is.EqualTo("120"));
            Assert.That(rejected.Message.Body, Is.Empty);
        });
    }

    [Test]
    public void RequiredTimerIsRejectedWhenIncomingPolicyDisablesIt()
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage invite = IncomingInvite(
            "100;refresher=uas", true, true);
        session.ReceiveInviteMessage(invite);
        DrainEvents(session);

        SipInviteRequest responseCommand = IncomingResponseCommand(invite);
        responseCommand.Options.SessionTimers = false;
        SipInviteEvent rejected = session.StartInvite(responseCommand);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Message!.StatusCode, Is.EqualTo(420));
            Assert.That(HeaderValue(rejected.Message,
                SipHeaderNames.Unsupported), Is.EqualTo("timer"));
            Assert.That(rejected.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
        });
    }

    static SipInviteSession ConfirmedOutgoing(
        int interval, string refresher, bool allowUpdate)
    {
        SipInviteSession session = new SipInviteSession();
        SipMessage request = session.StartInvite(
            OutgoingCommand(interval, 90, "")).Message!;
        DrainEvents(session);
        session.ReceiveInviteMessage(InitialResponse(
            request, 200, "OK", InitialAnswer(),
            interval.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + ";refresher=" + refresher,
            null, true, allowUpdate));
        DrainEvents(session);
        return session;
    }

    static SipInviteRequest OutgoingCommand(
        int interval, int minimum, string extraHeaders)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Options.SessionExpiresSeconds = interval;
        command.Options.MinSessionExpiresSeconds = minimum;
        command.Message = Parse(
            "INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-timer\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: timer-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + extraHeaders + BodyHeaders(InitialOffer())
            + System.Text.Encoding.ASCII.GetString(InitialOffer()));
        return command;
    }

    static SipMessage InitialResponse(
        SipMessage request, int status, string reason, byte[]? body,
        string? sessionExpires, string? minimum, bool requireTimer,
        bool allowUpdate)
    {
        byte[] actualBody = body ?? new byte[0];
        string contact = status >= 200 && status < 300
            ? "Contact: <sip:bob@peer.example.com>\r\n" : "";
        string timer = sessionExpires == null ? ""
            : "Session-Expires: " + sessionExpires + "\r\n";
        string min = minimum == null ? ""
            : "Min-SE: " + minimum + "\r\n";
        string required = requireTimer ? "Require: timer\r\n" : "";
        string allow = allowUpdate
            ? "Allow: INVITE, ACK, BYE, UPDATE\r\n" : "";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: timer-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " INVITE\r\n" + contact + timer + min + required + allow
            + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipInviteRequest RefreshCommand(string method, byte[] body)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse(method
            + " sip:bob@template.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP template.example.com;branch=z9hG4bK-template\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:template@example.com>;tag=template-from\r\n"
            + "To: <sip:peer@example.com>;tag=template-to\r\n"
            + "Call-ID: template@example.com\r\n"
            + "CSeq: 1 " + method + "\r\n"
            + (method == SipMethods.Invite
                ? "Contact: <sip:template@client.example.com>\r\n" : "")
            + BodyHeaders(body)
            + System.Text.Encoding.ASCII.GetString(body));
        return command;
    }

    static SipMessage RefreshResponse(
        SipMessage request, int status, string reason, byte[]? body,
        string? sessionExpires, string? minimum, string? retryAfter)
    {
        byte[] actualBody = body ?? new byte[0];
        string contact = status >= 200 && status < 300
            ? "Contact: <sip:bob@peer.example.com>\r\n" : "";
        string timer = sessionExpires == null ? ""
            : "Session-Expires: " + sessionExpires + "\r\n";
        string min = minimum == null ? ""
            : "Min-SE: " + minimum + "\r\n";
        string retry = retryAfter == null ? ""
            : "Retry-After: " + retryAfter + "\r\n";
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP dialog.invalid;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: timer-call@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + request.MethodToken + "\r\n"
            + contact + timer + min + retry + BodyHeaders(actualBody)
            + System.Text.Encoding.ASCII.GetString(actualBody));
    }

    static SipMessage IncomingInvite(
        string sessionExpires, bool supported, bool required)
    {
        return Parse("INVITE sip:alice@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch=z9hG4bK-incoming-timer\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>\r\n"
            + "Call-ID: incoming-timer@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:bob@peer.example.com>\r\n"
            + (supported ? "Supported: timer\r\n" : "")
            + (required ? "Require: timer\r\n" : "")
            + "Session-Expires: " + sessionExpires + "\r\n"
            + BodyHeaders(InitialOffer())
            + System.Text.Encoding.ASCII.GetString(InitialOffer()));
    }

    static SipMessage IncomingDialogUpdate()
    {
        return Parse("UPDATE sip:alice@client.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch=z9hG4bK-remote-refresh\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: timer-call@example.com\r\n"
            + "CSeq: 70 UPDATE\r\n"
            + "Contact: <sip:bob@peer.example.com>\r\n"
            + "Supported: timer\r\n"
            + "Allow: INVITE, ACK, BYE, UPDATE\r\n"
            + "Session-Expires: 90;refresher=uas\r\n"
            + "Min-SE: 90\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipInviteRequest IncomingDialogResponseCommand(SipMessage request)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Options.SessionExpiresSeconds = 90;
        command.Options.MinSessionExpiresSeconds = 90;
        command.Message = Parse(
            "SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: timer-call@example.com\r\n"
            + "CSeq: 70 UPDATE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return command;
    }

    static SipInviteRequest IncomingResponseCommand(SipMessage request)
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse(
            "SIP/2.0 200 OK\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch="
            + request.ViaValues[0].Branch + "\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-timer@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@server.example.com>\r\n"
            + BodyHeaders(InitialAnswer())
            + System.Text.Encoding.ASCII.GetString(InitialAnswer()));
        return command;
    }

    static SipMessage IncomingAck(SipMessage request)
    {
        return Parse("ACK sip:alice@server.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP peer.example.com;branch=z9hG4bK-timer-ack\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "To: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "Call-ID: incoming-timer@example.com\r\n"
            + "CSeq: " + request.CSeq!.SequenceNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " ACK\r\nContent-Length: 0\r\n\r\n");
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        int index = 0;
        while (index < message.Headers.Length)
        {
            if (string.Equals(message.Headers[index].Name, name,
                StringComparison.OrdinalIgnoreCase))
            {
                return message.Headers[index].Value;
            }
            index++;
        }
        return null;
    }

    static string BodyHeaders(byte[] body)
    {
        return (body.Length == 0 ? "" : "Content-Type: application/sdp\r\n")
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n";
    }

    static void DrainEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
    }

    static byte[] InitialOffer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\no=timer-offer\r\n");
    }

    static byte[] InitialAnswer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\no=timer-answer\r\n");
    }

    static byte[] HoldOffer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=sendonly\r\n");
    }

    static byte[] HoldAnswer()
    {
        return System.Text.Encoding.ASCII.GetBytes(
            "v=0\r\na=recvonly\r\n");
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }
}
