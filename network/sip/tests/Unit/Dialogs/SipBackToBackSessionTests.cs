namespace sip.Tests;

public class SipBackToBackSessionTests
{
    [Test]
    public void CreatesDetachedIncomingAndOutgoingLegsWithIndependentState()
    {
        SipInviteRequest left = LeftRequest();
        SipInviteRequest right = RightRequest();
        SipBackToBackSession session = new SipBackToBackSession();

        SipBackToBackEvent created = session.CreateBackToBackSession(left, right);
        left.Message!.Body[0] = (byte)'X';
        right.Message!.RequestUri!.Host = "changed.example.com";
        created.LeftInvite!.Invite!.Body[0] = (byte)'Y';
        SipBackToBackEvent current = session.GetBackToBackSession();

        Assert.Multiple(() =>
        {
            Assert.That(created.State,
                Is.EqualTo(SipBackToBackState.Establishing));
            Assert.That(created.SourceLeg, Is.EqualTo(SipBackToBackLeg.None));
            Assert.That(created.LeftInvite.Role, Is.EqualTo(SipDialogRole.Uas));
            Assert.That(created.RightInvite!.Role, Is.EqualTo(SipDialogRole.Uac));
            Assert.That(created.LeftInvite.State,
                Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(created.RightInvite.State,
                Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(created.LeftInvite.Invite!.CSeq!.SequenceNumber,
                Is.EqualTo(10));
            Assert.That(created.RightInvite.Invite!.CSeq!.SequenceNumber,
                Is.EqualTo(42));
            Assert.That(created.LeftInvite.Invite.ViaValues[0].Branch,
                Is.EqualTo("z9hG4bK-left"));
            Assert.That(created.RightInvite.Invite.ViaValues[0].Branch,
                Is.EqualTo("z9hG4bK-right"));
            Assert.That(created.LeftTransaction!.Role,
                Is.EqualTo(SipTransactionRole.Server));
            Assert.That(created.LeftTransaction.State,
                Is.EqualTo(SipTransactionState.Proceeding));
            Assert.That(created.RightTransaction!.Role,
                Is.EqualTo(SipTransactionRole.Client));
            Assert.That(created.RightTransaction.State,
                Is.EqualTo(SipTransactionState.Calling));
            Assert.That(created.LeftTransaction.Key!.CallId,
                Is.EqualTo("left-call"));
            Assert.That(created.RightTransaction.Key!.CallId,
                Is.EqualTo("right-call"));
            Assert.That(created.LeftInvite.AuthenticationContextId,
                Is.EqualTo("left-auth"));
            Assert.That(created.RightInvite.AuthenticationContextId,
                Is.EqualTo("right-auth"));
            Assert.That(current.LeftInvite!.Invite!.Body,
                Is.EqualTo(Ascii("left-offer")));
            Assert.That(current.RightInvite!.Invite!.RequestUri!.Host,
                Is.EqualTo("callee.example.com"));
            Assert.That(current.CorrelationId, Is.EqualTo("b2b-call"));
        });
    }

    [Test]
    public void AdvancesEachLegDeadlineWithoutSharingCancellationState()
    {
        SipInviteRequest right = RightRequest();
        right.DeadlineMilliseconds = 25;
        SipBackToBackSession session = new SipBackToBackSession();
        session.CreateBackToBackSession(LeftRequest(), right);
        Drain(session);

        session.AdvanceBackToBackTime(24);
        SipBackToBackEvent before = session.GetBackToBackSession();
        session.AdvanceBackToBackTime(25);
        SipBackToBackEvent expired = session.GetBackToBackSession();

        Assert.Multiple(() =>
        {
            Assert.That(before.State,
                Is.EqualTo(SipBackToBackState.Establishing));
            Assert.That(before.LeftInvite!.CancellationPending, Is.False);
            Assert.That(before.RightInvite!.CancellationPending, Is.False);
            Assert.That(expired.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(expired.LeftInvite!.State,
                Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(expired.LeftInvite.CancellationPending, Is.False);
            Assert.That(expired.RightInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(expired.RightInvite.CancellationPending, Is.True);
            Assert.That(expired.RightInvite.CancellationId,
                Is.EqualTo("deadline"));
        });
    }

    [Test]
    public void AppliesProvisionalAndFinalMappingsWithoutSharingLegIdentity()
    {
        SipBackToBackSession session = StartedSession();
        SipBackToBackEvent early = session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            RightResponse(180, "Ringing", new byte[0]),
            LeftResponse(183, "Session Progress", new byte[0]), "early"));
        SipBackToBackEvent confirmed = session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            RightResponse(200, "OK", Ascii("right-answer")),
            LeftResponse(200, "OK", Ascii("mapped-left-answer")), "answer"));

        Assert.Multiple(() =>
        {
            Assert.That(early.Mapped, Is.True);
            Assert.That(early.SourceLeg, Is.EqualTo(SipBackToBackLeg.Right));
            Assert.That(early.LeftDialog!.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(early.RightDialog!.State, Is.EqualTo(SipDialogState.Early));
            Assert.That(early.LeftDialog.RemoteTarget!.Host,
                Is.EqualTo("caller.example.com"));
            Assert.That(early.LeftDialog.LocalTarget!.Host,
                Is.EqualTo("relay-left.example.com"));
            Assert.That(early.RightDialog.LocalTarget!.Host,
                Is.EqualTo("relay-right.example.com"));
            Assert.That(early.RightDialog.RemoteTarget!.Host,
                Is.EqualTo("callee-dialog.example.com"));
            Assert.That(early.LeftDialog.RouteSet[0].Address!.Uri!.Host,
                Is.EqualTo("left-proxy.example.com"));
            Assert.That(early.RightDialog.RouteSet[0].Address!.Uri!.Host,
                Is.EqualTo("right-proxy.example.com"));
            Assert.That(confirmed.State,
                Is.EqualTo(SipBackToBackState.Confirmed));
            Assert.That(confirmed.LeftDialog!.RemoteCSeq, Is.EqualTo(10));
            Assert.That(confirmed.RightDialog!.LocalCSeq, Is.EqualTo(42));
            Assert.That(confirmed.LeftTransaction!.State,
                Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(confirmed.RightTransaction!.State,
                Is.EqualTo(SipTransactionState.Accepted));
            Assert.That(confirmed.LeftMessage!.Body,
                Is.EqualTo(Ascii("mapped-left-answer")));
            Assert.That(confirmed.RightMessage!.Body,
                Is.EqualTo(Ascii("right-answer")));
            Assert.That(CallId(confirmed.LeftMessage), Is.EqualTo("left-call"));
            Assert.That(CallId(confirmed.RightMessage), Is.EqualTo("right-call"));
            Assert.That(confirmed.CorrelationId, Is.EqualTo("answer"));
        });
    }

    [Test]
    public void RecordsApplicationControlledBodiesForNonInviteMessages()
    {
        SipBackToBackSession session = StartedSession();
        SipMessage received = InDialogMessage(
            "MESSAGE", "left-call", 11, "source body", true);
        SipMessage mapped = InDialogMessage(
            "MESSAGE", "right-call", 43, "mapped body", false);

        SipBackToBackEvent result = session.ApplyBackToBackMapping(
            Mapping(SipBackToBackLeg.Left, received, mapped, "message-map"));
        received.Body[0] = 0;
        mapped.Body[0] = 0;
        SipBackToBackEvent current = session.GetBackToBackSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.Mapped, Is.True);
            Assert.That(result.State,
                Is.EqualTo(SipBackToBackState.Establishing));
            Assert.That(result.LeftMessage!.Body,
                Is.EqualTo(Ascii("source body")));
            Assert.That(result.RightMessage!.Body,
                Is.EqualTo(Ascii("mapped body")));
            Assert.That(result.LeftMessage.Body,
                Is.Not.EqualTo(result.RightMessage.Body));
            Assert.That(current.LeftMessage!.Body,
                Is.EqualTo(Ascii("source body")));
            Assert.That(current.RightMessage!.Body,
                Is.EqualTo(Ascii("mapped body")));
        });
    }

    [Test]
    public void AuthenticationAndTransportFailureRemainLegLocal()
    {
        SipBackToBackSession session = StartedSession();

        session.AssociateBackToBackAuthenticationContext(
            SipBackToBackLeg.Right, "right-retry-auth");
        SipBackToBackEvent associated = session.GetBackToBackSession();
        session.ReportBackToBackTransportError(
            SipBackToBackLeg.Right, "right transport failed");
        SipBackToBackEvent failed = session.GetBackToBackSession();

        Assert.Multiple(() =>
        {
            Assert.That(associated.LeftInvite!.AuthenticationContextId,
                Is.EqualTo("left-auth"));
            Assert.That(associated.RightInvite!.AuthenticationContextId,
                Is.EqualTo("right-retry-auth"));
            Assert.That(failed.State, Is.EqualTo(SipBackToBackState.Failed));
            Assert.That(failed.RightInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.RightInvite.ErrorMessage,
                Is.EqualTo("right transport failed"));
            Assert.That(failed.LeftInvite!.State,
                Is.EqualTo(SipInviteSessionState.Proceeding));
            Assert.That(failed.LeftInvite.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void TerminatesConfirmedLegsWithIndependentByeTransactions()
    {
        SipBackToBackSession session = ConfirmedSession();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "hangup-1";
        cancellation.Reason = "Application hangup.";

        session.TerminateBackToBackSession(cancellation);
        SipBackToBackEvent terminating = session.GetBackToBackSession();
        cancellation.Reason = "changed";
        SipMessage leftBye = terminating.LeftMessage!;
        SipMessage rightBye = terminating.RightMessage!;
        session.MapBackToBackMessage(SipTransactionMessageFactory
            .CreateRequestResponse(leftBye, 200, "OK",
                SipReferCore.ReadTag(leftBye.To)));
        SipBackToBackEvent terminated = session.MapBackToBackMessage(
            SipTransactionMessageFactory.CreateRequestResponse(
                rightBye, 200, "OK", SipReferCore.ReadTag(rightBye.To)));

        Assert.Multiple(() =>
        {
            Assert.That(terminating.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(leftBye.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(rightBye.Method, Is.EqualTo(SipMethod.Bye));
            Assert.That(CallId(leftBye), Is.EqualTo("left-call"));
            Assert.That(CallId(rightBye), Is.EqualTo("right-call"));
            Assert.That(leftBye.CSeq!.SequenceNumber,
                Is.Not.EqualTo(rightBye.CSeq!.SequenceNumber));
            Assert.That(terminating.LeftTransaction!.Kind,
                Is.EqualTo(SipTransactionKind.NonInvite));
            Assert.That(terminating.RightTransaction!.Kind,
                Is.EqualTo(SipTransactionKind.NonInvite));
            Assert.That(terminating.LeftTransaction.Key!.Branch,
                Is.Not.EqualTo(terminating.RightTransaction.Key!.Branch));
            Assert.That(terminating.Cancellation!.Reason,
                Is.EqualTo("Application hangup."));
            Assert.That(terminated.State,
                Is.EqualTo(SipBackToBackState.Terminated));
            Assert.That(terminated.LeftDialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
            Assert.That(terminated.RightDialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
        });
    }

    [Test]
    public void RejectsSharedOrUnmatchedLegIdentitiesAndIncompleteMappings()
    {
        SipInviteRequest shared = RightRequest();
        shared.Message = ReplaceCallId(shared.Message!, "left-call");
        SipBackToBackSession invalid = new SipBackToBackSession();
        Assert.That(() => invalid.CreateBackToBackSession(
            LeftRequest(), shared), Throws.TypeOf<ArgumentException>());

        SipBackToBackSession session = StartedSession();
        SipMessage unmatched = ReplaceCallId(
            RightResponse(180, "Ringing", new byte[0]), "other-call");
        SipBackToBackMapping reversed = Mapping(
            SipBackToBackLeg.Left,
            RightResponse(180, "Ringing", new byte[0]),
            LeftResponse(180, "Ringing", new byte[0]), "bad");

        Assert.Multiple(() =>
        {
            Assert.That(() => session.MapBackToBackMessage(unmatched),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => session.ApplyBackToBackMapping(reversed),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => new SipBackToBackSession()
                .GetBackToBackSession(),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void CancelRaceCompletesBothLegsWithoutBecomingFailure()
    {
        SipBackToBackSession session = StartedSession();
        session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            RightResponse(180, "Ringing", new byte[0]),
            LeftResponse(180, "Ringing", new byte[0]), "early"));
        Drain(session);
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "cancel-race";
        cancellation.Reason = "Caller cancelled.";

        session.TerminateBackToBackSession(cancellation);
        SipBackToBackEvent terminating = session.GetBackToBackSession();
        SipMessage rightCancel = terminating.RightMessage!;
        SipBackToBackEvent cancelAccepted = session.MapBackToBackMessage(
            SipTransactionMessageFactory.CreateRequestResponse(
                rightCancel, SipStatusCodes.Ok, SipReasonPhrases.Ok,
                "remote-right"));
        SipBackToBackEvent terminated = session.MapBackToBackMessage(
            RightResponse(SipStatusCodes.RequestTerminated,
                SipReasonPhrases.RequestTerminated, new byte[0]));

        Assert.Multiple(() =>
        {
            Assert.That(terminating.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(terminating.LeftMessage!.StatusCode,
                Is.EqualTo(SipStatusCodes.RequestTerminated));
            Assert.That(rightCancel.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancelAccepted.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(terminated.State,
                Is.EqualTo(SipBackToBackState.Terminated));
            Assert.That(terminated.LeftInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(terminated.RightInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
        });
    }

    [Test]
    public void FinalInviteFailureRemainsDistinctFromCancellation()
    {
        SipBackToBackSession session = StartedSession();

        SipBackToBackEvent failed = session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            RightResponse(SipStatusCodes.BusyHere,
                SipReasonPhrases.BusyHere, new byte[0]),
            LeftResponse(SipStatusCodes.BusyHere,
                SipReasonPhrases.BusyHere, new byte[0]), "busy"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipBackToBackState.Failed));
            Assert.That(failed.LeftInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.RightInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.LeftTransaction!.State,
                Is.EqualTo(SipTransactionState.Completed));
            Assert.That(failed.RightTransaction!.State,
                Is.EqualTo(SipTransactionState.Completed));
            Assert.That(failed.CorrelationId, Is.EqualTo("busy"));
        });
    }

    [Test]
    public void BidirectionalMappingsKeepTransactionIdentityAndCSeqIndependent()
    {
        SipBackToBackSession session = ConfirmedSession();
        SipBackToBackEvent leftMapped = session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Left,
            InDialogMessage("INFO", "left-call", 11, "left-info", true),
            InDialogMessage("INFO", "right-call", 50, "right-info", false),
            "left-to-right"));
        SipBackToBackEvent rightMapped = session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            InDialogMessage("MESSAGE", "right-call", 51,
                "right-message", false),
            InDialogMessage("MESSAGE", "left-call", 12,
                "left-message", true),
            "right-to-left"));

        Assert.Multiple(() =>
        {
            Assert.That(leftMapped.LeftTransaction!.Role,
                Is.EqualTo(SipTransactionRole.Server));
            Assert.That(leftMapped.RightTransaction!.Role,
                Is.EqualTo(SipTransactionRole.Client));
            Assert.That(leftMapped.LeftTransaction.Key!.CSeq, Is.EqualTo(11));
            Assert.That(leftMapped.RightTransaction.Key!.CSeq, Is.EqualTo(50));
            Assert.That(leftMapped.LeftTransaction.Key.CallId,
                Is.EqualTo("left-call"));
            Assert.That(leftMapped.RightTransaction.Key.CallId,
                Is.EqualTo("right-call"));
            Assert.That(rightMapped.LeftTransaction!.Key!.CSeq, Is.EqualTo(12));
            Assert.That(rightMapped.RightTransaction!.Key!.CSeq, Is.EqualTo(51));
            Assert.That(rightMapped.LeftMessage!.Body,
                Is.EqualTo(Ascii("left-message")));
            Assert.That(rightMapped.RightMessage!.Body,
                Is.EqualTo(Ascii("right-message")));
            Assert.That(rightMapped.CorrelationId, Is.EqualTo("right-to-left"));
        });
    }

    [Test]
    public void FailedByeStillCleansUpBothConfirmedLegs()
    {
        SipBackToBackSession session = ConfirmedSession();
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        cancellation.Id = "bye-cleanup";
        session.TerminateBackToBackSession(cancellation);
        SipBackToBackEvent terminating = session.GetBackToBackSession();
        SipMessage leftBye = terminating.LeftMessage!;
        SipMessage rightBye = terminating.RightMessage!;

        SipBackToBackEvent leftFailed = session.MapBackToBackMessage(
            SipTransactionMessageFactory.CreateRequestResponse(
                leftBye, SipStatusCodes.CallOrTransactionDoesNotExist,
                SipReasonPhrases.CallOrTransactionDoesNotExist,
                SipReferCore.ReadTag(leftBye.To)));
        SipBackToBackEvent terminated = session.MapBackToBackMessage(
            SipTransactionMessageFactory.CreateRequestResponse(
                rightBye, SipStatusCodes.Ok, SipReasonPhrases.Ok,
                SipReferCore.ReadTag(rightBye.To)));

        Assert.Multiple(() =>
        {
            Assert.That(leftFailed.State,
                Is.EqualTo(SipBackToBackState.Terminating));
            Assert.That(leftFailed.LeftInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(leftFailed.RightInvite!.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(terminated.State,
                Is.EqualTo(SipBackToBackState.Terminated));
            Assert.That(terminated.LeftDialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
            Assert.That(terminated.RightDialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
        });
    }

    static SipBackToBackSession StartedSession()
    {
        SipBackToBackSession session = new SipBackToBackSession();
        session.CreateBackToBackSession(LeftRequest(), RightRequest());
        Drain(session);
        return session;
    }

    static SipBackToBackSession ConfirmedSession()
    {
        SipBackToBackSession session = StartedSession();
        session.ApplyBackToBackMapping(Mapping(
            SipBackToBackLeg.Right,
            RightResponse(200, "OK", Ascii("right-answer")),
            LeftResponse(200, "OK", Ascii("left-answer")), "confirmed"));
        session.MapBackToBackMessage(LeftAck());
        Drain(session);
        return session;
    }

    static SipInviteRequest LeftRequest()
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Message = Parse("INVITE sip:service@relay-left.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch=z9hG4bK-left\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Caller <sip:caller@example.com>;tag=remote-left\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: left-call\r\n"
            + "CSeq: 10 INVITE\r\n"
            + "Record-Route: <sip:left-proxy.example.com;lr>\r\n"
            + "Contact: <sip:caller@caller.example.com>\r\n"
            + BodyHeaders(Ascii("left-offer"))
            + "left-offer");
        request.AuthenticationContextId = "left-auth";
        request.CorrelationId = "left-correlation";
        return request;
    }

    static SipInviteRequest RightRequest()
    {
        SipInviteRequest request = new SipInviteRequest();
        request.Message = Parse("INVITE sip:callee@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP relay-right.example.com;branch=z9hG4bK-right\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Relay <sip:relay@example.com>;tag=local-right\r\n"
            + "To: Callee <sip:callee@example.com>\r\n"
            + "Call-ID: right-call\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:relay@relay-right.example.com>\r\n"
            + BodyHeaders(Ascii("right-offer"))
            + "right-offer");
        request.AuthenticationContextId = "right-auth";
        request.CorrelationId = "b2b-call";
        return request;
    }

    static SipMessage RightResponse(int status, string reason, byte[] body)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP relay-right.example.com;branch=z9hG4bK-right\r\n"
            + "From: Relay <sip:relay@example.com>;tag=local-right\r\n"
            + "To: Callee <sip:callee@example.com>;tag=remote-right\r\n"
            + "Call-ID: right-call\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Record-Route: <sip:right-proxy.example.com;lr>\r\n"
            + "Contact: <sip:callee@callee-dialog.example.com>\r\n"
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipMessage LeftResponse(int status, string reason, byte[] body)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch=z9hG4bK-left\r\n"
            + "From: Caller <sip:caller@example.com>;tag=remote-left\r\n"
            + "To: Service <sip:service@example.com>;tag=local-left\r\n"
            + "Call-ID: left-call\r\n"
            + "CSeq: 10 INVITE\r\n"
            + "Record-Route: <sip:left-proxy.example.com;lr>\r\n"
            + "Contact: <sip:service@relay-left.example.com>\r\n"
            + BodyHeaders(body) + System.Text.Encoding.ASCII.GetString(body));
    }

    static SipMessage LeftAck()
    {
        return Parse("ACK sip:service@relay-left.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP caller.example.com;branch=z9hG4bK-left-ack\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Caller <sip:caller@example.com>;tag=remote-left\r\n"
            + "To: Service <sip:service@example.com>;tag=local-left\r\n"
            + "Call-ID: left-call\r\n"
            + "CSeq: 10 ACK\r\n"
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage InDialogMessage(string method, string callId,
        int cseq, string body, bool left)
    {
        string from = left
            ? "Caller <sip:caller@example.com>;tag=remote-left"
            : "Relay <sip:relay@example.com>;tag=local-right";
        string to = left
            ? "Service <sip:service@example.com>;tag=local-left"
            : "Callee <sip:callee@example.com>;tag=remote-right";
        return Parse(method + " sip:target@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP source.example.com;branch=z9hG4bK-" + cseq + "\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: " + from + "\r\n"
            + "To: " + to + "\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq + " " + method + "\r\n"
            + "Content-Type: text/plain\r\n"
            + "Content-Length: " + body.Length + "\r\n\r\n" + body);
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

    static SipMessage ReplaceCallId(SipMessage source, string callId)
    {
        SipMessage result = SipSnapshotCopy.CopyMessageModel(source)!;
        SipHeader header = result.Headers.First(value =>
            string.Equals(value.Name, SipHeaderNames.CallId,
                StringComparison.OrdinalIgnoreCase));
        header.Value = callId;
        result.OriginalBytes = new byte[0];
        return result;
    }

    static string CallId(SipMessage message)
    {
        return message.Headers.First(value =>
            string.Equals(value.Name, SipHeaderNames.CallId,
                StringComparison.OrdinalIgnoreCase)).Value!;
    }

    static string BodyHeaders(byte[] body)
    {
        return "Content-Type: application/sdp\r\n"
            + "Content-Length: " + body.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + "\r\n\r\n";
    }

    static byte[] Ascii(string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static void Drain(SipBackToBackSession session)
    {
        while (session.PollBackToBackEvent().State
            != SipBackToBackState.None)
        {
        }
    }
}
