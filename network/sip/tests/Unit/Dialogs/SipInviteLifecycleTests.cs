namespace sip.Tests;

public class SipInviteLifecycleTests
{
    [Test]
    public void SnapshotHistoryAndCurrentSnapshotAreDetached()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand();
        command.DeadlineMilliseconds = 500;
        command.AuthenticationContextId = "auth-attempt-one";
        command.CorrelationId = "invite-operation";

        SipInviteEvent started = session.StartInvite(command);
        SipInviteSnapshot queued = session.PollInviteSnapshot();
        SipInviteSnapshot current = session.GetInviteSnapshot();
        started.Snapshot!.Invite!.RequestUri!.Host = "changed.example.com";
        queued.Invite!.RequestUri!.Host = "queued-change.example.com";
        current.TimerNames[0] = "changed";
        SipInviteSnapshot unchanged = session.GetInviteSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(started.Snapshot.State,
                Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(queued.State, Is.EqualTo(SipInviteSessionState.Calling));
            Assert.That(queued.AuthenticationContextId,
                Is.EqualTo("auth-attempt-one"));
            Assert.That(queued.CorrelationId, Is.EqualTo("invite-operation"));
            Assert.That(queued.TimerNames, Is.EqualTo(new string[] { "operation" }));
            Assert.That(queued.TimerDeadlines, Is.EqualTo(new long[] { 500 }));
            Assert.That(queued.NextDeadlineMilliseconds, Is.EqualTo(500));
            Assert.That(unchanged.Invite!.RequestUri!.Host,
                Is.EqualTo("callee.example.com"));
            Assert.That(unchanged.TimerNames[0], Is.EqualTo("operation"));
            Assert.That(session.PollInviteSnapshot().State,
                Is.EqualTo(SipInviteSessionState.None));
        });
    }

    [Test]
    public void DeadlineRequestsCancellationAndRetainsCancellationIdentity()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand();
        command.DeadlineMilliseconds = 100;
        session.StartInvite(command);
        DrainEvents(session);

        session.AdvanceInviteTime(99);
        SipInviteEvent before = session.PollInviteEvent();
        session.AdvanceInviteTime(100);
        SipInviteEvent expired = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(before.State, Is.EqualTo(SipInviteSessionState.None));
            Assert.That(expired.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(expired.Message, Is.Null);
            Assert.That(expired.Reason, Does.Contain("deadline"));
            Assert.That(expired.Snapshot!.CancellationPending, Is.True);
            Assert.That(expired.Snapshot.CancellationId, Is.EqualTo("deadline"));
            Assert.That(expired.Snapshot.NextDeadlineMilliseconds, Is.EqualTo(-1));
        });
    }

    [Test]
    public void DeadlineInEarlyStateImmediatelyQueuesCancel()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand();
        command.DeadlineMilliseconds = 100;
        session.StartInvite(command);
        DrainEvents(session);
        session.ReceiveInviteMessage(Response(180, "Ringing", true));
        DrainEvents(session);

        session.AdvanceInviteTime(100);
        SipInviteEvent expired = session.PollInviteEvent();
        SipInviteEvent cancel = session.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(expired.State,
                Is.EqualTo(SipInviteSessionState.Terminating));
            Assert.That(expired.Reason, Does.Contain("deadline"));
            Assert.That(cancel.Message!.Method, Is.EqualTo(SipMethod.Cancel));
            Assert.That(cancel.Message.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(cancel.Message.ViaValues[0].Branch,
                Is.EqualTo("z9hG4bK-lifecycle"));
            Assert.That(session.GetInviteSnapshot().CancellationPending, Is.True);
        });
    }

    [Test]
    public void AuthenticationContextAssociatesWithEarlyDialogAndSurvivesFailure()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteEvent initial = session.StartInvite(OutgoingCommand());
        DrainEvents(session);
        session.ReceiveInviteMessage(Response(180, "Ringing", true));
        DrainEvents(session);

        session.AssociateInviteAuthenticationContext("digest-retry-one");
        SipInviteEvent associated = session.PollInviteEvent();
        SipInviteEvent failed = session.ReceiveInviteMessage(
            Response(401, "Unauthorized", false));
        SipInviteRequest retryCommand = OutgoingCommand();
        retryCommand.AuthenticationContextId = "digest-retry-one";
        SipInviteEvent retry = session.StartInvite(retryCommand);

        Assert.Multiple(() =>
        {
            Assert.That(associated.Message, Is.Null);
            Assert.That(associated.Dialog!.AuthenticationContextId,
                Is.EqualTo("digest-retry-one"));
            Assert.That(associated.Snapshot!.AuthenticationContextId,
                Is.EqualTo("digest-retry-one"));
            Assert.That(failed.State, Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.Snapshot!.AuthenticationContextId,
                Is.EqualTo("digest-retry-one"));
            Assert.That(failed.Snapshot.Invite, Is.Null);
            Assert.That(failed.Snapshot.LastResponse!.StatusCode, Is.EqualTo(401));
            Assert.That(retry.Reason, Does.Contain("Authenticated"));
            Assert.That(retry.LocalSessionId, Is.EqualTo(initial.LocalSessionId));
            Assert.That(retry.Snapshot!.AuthenticationRetryCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void TransportFailureTerminatesDialogAndClearsPendingState()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand();
        command.DeadlineMilliseconds = 800;
        command.AuthenticationContextId = "authenticated-dialog";
        session.StartInvite(command);
        session.ReceiveInviteMessage(Response(200, "OK", true));
        DrainEvents(session);

        session.ReportInviteTransportError("connection reset by peer");
        SipInviteEvent failure = session.PollInviteEvent();
        SipInviteSnapshot current = session.GetInviteSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(failure.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failure.ErrorMessage,
                Is.EqualTo("connection reset by peer"));
            Assert.That(failure.Snapshot!.Dialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
            Assert.That(failure.Snapshot.Invite, Is.Null);
            Assert.That(failure.Snapshot.TimerNames, Is.Empty);
            Assert.That(failure.Snapshot.NextDeadlineMilliseconds, Is.EqualTo(-1));
            Assert.That(current.State, Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(current.AuthenticationContextId,
                Is.EqualTo("authenticated-dialog"));
            Assert.Throws<InvalidOperationException>(() =>
                session.ReportInviteTransportError("second failure"));
        });
    }

    [Test]
    public void TransportFailureTerminatesOutgoingEarlyAndIncomingProceedingStates()
    {
        SipInviteSession outgoing = new SipInviteSession();
        outgoing.StartInvite(OutgoingCommand());
        outgoing.ReceiveInviteMessage(Response(180, "Ringing", true));
        DrainEvents(outgoing);
        outgoing.ReportInviteTransportError("early transport failure");
        SipInviteEvent outgoingFailure = outgoing.PollInviteEvent();

        SipInviteSession incoming = new SipInviteSession();
        incoming.ReceiveInviteMessage(OutgoingCommand().Message!);
        DrainEvents(incoming);
        incoming.ReportInviteTransportError("incoming transport failure");
        SipInviteEvent incomingFailure = incoming.PollInviteEvent();

        Assert.Multiple(() =>
        {
            Assert.That(outgoingFailure.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(outgoingFailure.Dialog!.State,
                Is.EqualTo(SipDialogState.Terminated));
            Assert.That(outgoingFailure.ErrorMessage,
                Is.EqualTo("early transport failure"));
            Assert.That(incomingFailure.State,
                Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(incomingFailure.Dialog, Is.Null);
            Assert.That(incomingFailure.ErrorMessage,
                Is.EqualTo("incoming transport failure"));
        });
    }

    [Test]
    public void AuthenticationRetryPolicyRejectsRelatedRetryWhenDisabled()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest initial = OutgoingCommand();
        initial.Options.AllowAuthenticationRetry = false;
        session.StartInvite(initial);
        session.AssociateInviteAuthenticationContext("disabled-retry");
        session.ReceiveInviteMessage(Response(401, "Unauthorized", false));
        SipInviteRequest retry = OutgoingCommand();
        retry.AuthenticationContextId = "disabled-retry";

        Assert.Throws<InvalidOperationException>(() =>
            session.StartInvite(retry));
    }

    [Test]
    public void FinalClientFailureQueuesCleanTerminalSnapshot()
    {
        SipInviteSession session = new SipInviteSession();
        SipInviteRequest command = OutgoingCommand();
        command.DeadlineMilliseconds = 900;
        session.StartInvite(command);
        DrainEvents(session);

        SipInviteEvent failed = session.ReceiveInviteMessage(
            Response(486, "Busy Here", false));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(SipInviteSessionState.Terminated));
            Assert.That(failed.Snapshot!.Invite, Is.Null);
            Assert.That(failed.Snapshot.TimerNames, Is.Empty);
            Assert.That(failed.Snapshot.NextDeadlineMilliseconds, Is.EqualTo(-1));
            Assert.That(failed.Snapshot.LastResponse!.StatusCode, Is.EqualTo(486));
            Assert.That(failed.Snapshot.OfferAnswer!.State,
                Is.EqualTo(SipOfferAnswerState.Stable));
        });
    }

    static SipInviteRequest OutgoingCommand()
    {
        SipInviteRequest command = new SipInviteRequest();
        command.Message = Parse(
            "INVITE sip:bob@callee.example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-lifecycle\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>\r\n"
            + "Call-ID: lifecycle-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + "Content-Length: 0\r\n\r\n");
        return command;
    }

    static SipMessage Response(int status, string reason, bool contact)
    {
        return Parse("SIP/2.0 " + status.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + " " + reason + "\r\n"
            + "Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-lifecycle\r\n"
            + "From: Alice <sip:alice@alice.example.com>;tag=local-tag\r\n"
            + "To: Bob <sip:bob@bob.example.com>;tag=remote-tag\r\n"
            + "Call-ID: lifecycle-call@example.com\r\n"
            + "CSeq: 42 INVITE\r\n"
            + (contact ? "Contact: <sip:bob@target.example.com>\r\n" : "")
            + "Content-Length: 0\r\n\r\n");
    }

    static void DrainEvents(SipInviteSession session)
    {
        while (session.PollInviteEvent().State != SipInviteSessionState.None)
        {
        }
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
