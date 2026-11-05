using System.Security.Cryptography.X509Certificates;

namespace ftp.Tests;

public class FtpClearControlCoordinatorTests
{
    [Test]
    public void Client_requires_explicit_opt_in_before_sending_CCC()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        FtpTlsOptions options = new FtpTlsOptions();
        RecordingEventSink events = new RecordingEventSink();
        FtpTlsClientStream tlsStream = CreateClientStream(options, false);
        FtpClientClearControlCoordinator coordinator =
            new FtpClientClearControlCoordinator(options, session, tlsStream, events);

        Assert.Throws<FtpConfigurationException>(() => coordinator.BeginClearControl());

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.Rejected, Is.True);
            Assert.That(coordinator.State.CommandPending, Is.False);
            Assert.That(tlsStream.Closed, Is.False);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(events.Events, Has.Count.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(FtpEventKind.PolicyRejection));
            Assert.That(events.Events[0].CommandKind, Is.EqualTo(FtpCommandKind.Ccc));
        });
    }

    [Test]
    public void Server_rejects_CCC_by_default_and_records_the_policy_decision()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        FtpTlsOptions options = new FtpTlsOptions();
        RecordingEventSink events = new RecordingEventSink();
        FtpTlsServerStream tlsStream = CreateServerStream(options, false);
        FtpServerClearControlCoordinator coordinator =
            new FtpServerClearControlCoordinator(options, session, tlsStream, events);

        FtpReply reply = coordinator.HandleClearControlCommand(CreateCccCommand());

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.RequestDeniedByPolicy));
            Assert.That(coordinator.State.Rejected, Is.True);
            Assert.That(coordinator.State.ReplyWritePending, Is.False);
            Assert.That(tlsStream.Closed, Is.False);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(events.Events, Has.Count.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(FtpEventKind.PolicyRejection));
            Assert.That(events.Events[0].Reply, Is.Not.SameAs(reply));
            Assert.That(events.Events[0].Reply!.Code, Is.EqualTo(reply.Code));
        });
    }

    [Test]
    public void Client_restores_clear_control_only_after_orderly_TLS_shutdown()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        SelectPrivateProtection(session);
        FtpTlsOptions options = CreateClearControlOptions();
        RecordingEventSink events = new RecordingEventSink();
        FtpTlsClientStream tlsStream = CreateClientStream(options, true);
        FtpClientClearControlCoordinator coordinator =
            new FtpClientClearControlCoordinator(options, session, tlsStream, events);

        FtpCommand command = coordinator.BeginClearControl();
        FtpReply reply = CreateReply(FtpReplyCodes.CommandOkay, "Continue without TLS");
        coordinator.ApplyClearControlReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Ccc));
            Assert.That(coordinator.State.TlsShutdownPending, Is.True);
            Assert.That(tlsStream.Closed, Is.False);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(session.State.ClearCommandChannelCompleted, Is.False);
        });

        coordinator.CompleteClearControl();

        AssertSuccessfulDowngrade(coordinator.State, session, tlsStream.Closed, events);
    }

    [Test]
    public void Server_waits_for_the_protected_CCC_reply_before_TLS_shutdown()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        SelectPrivateProtection(session);
        FtpTlsOptions options = CreateClearControlOptions();
        RecordingEventSink events = new RecordingEventSink();
        FtpTlsServerStream tlsStream = CreateServerStream(options, true);
        FtpServerClearControlCoordinator coordinator =
            new FtpServerClearControlCoordinator(options, session, tlsStream, events);

        FtpReply reply = coordinator.HandleClearControlCommand(CreateCccCommand());

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(coordinator.State.ReplyWritePending, Is.True);
            Assert.That(coordinator.State.TlsShutdownPending, Is.False);
            Assert.That(tlsStream.Closed, Is.False);
            Assert.That(session.State.ControlProtected, Is.True);
        });

        coordinator.CompleteClearControlAfterReplyWrite();

        AssertSuccessfulDowngrade(coordinator.State, session, tlsStream.Closed, events);
    }

    [Test]
    public void Rejected_client_CCC_keeps_TLS_and_the_existing_data_protection_level()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        SelectPrivateProtection(session);
        FtpTlsOptions options = CreateClearControlOptions();
        RecordingEventSink events = new RecordingEventSink();
        FtpTlsClientStream tlsStream = CreateClientStream(options, true);
        FtpClientClearControlCoordinator coordinator =
            new FtpClientClearControlCoordinator(options, session, tlsStream, events);
        coordinator.BeginClearControl();

        coordinator.ApplyClearControlReply(
            CreateReply(FtpReplyCodes.RequestDeniedByPolicy, "CCC disabled"));

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.Rejected, Is.True);
            Assert.That(coordinator.State.TlsShutdownPending, Is.False);
            Assert.That(coordinator.State.ConnectionMustClose, Is.False);
            Assert.That(tlsStream.Closed, Is.False);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(session.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(events.Events, Is.Empty);
        });
    }

    [Test]
    public void CCC_requires_a_TLS_wrapper_that_preserves_the_control_transport()
    {
        FtpSessionStateMachine session = CreateTlsSession();
        FtpTlsOptions options = CreateClearControlOptions();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpClientClearControlCoordinator(
                    options, session, CreateClientStream(options, false), null));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerClearControlCoordinator(
                    options, session, CreateServerStream(options, false), null));
        });
    }

    private static void AssertSuccessfulDowngrade(
        FtpClearControlState state, FtpSessionStateMachine session,
        bool tlsClosed, RecordingEventSink events)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.TlsShutdownPending, Is.False);
            Assert.That(state.TlsShutdownComplete, Is.True);
            Assert.That(state.ClearControlRestored, Is.True);
            Assert.That(state.AuditRecorded, Is.True);
            Assert.That(tlsClosed, Is.True);
            Assert.That(session.State.ControlProtected, Is.False);
            Assert.That(session.State.ClearCommandChannelCompleted, Is.True);
            Assert.That(session.State.SecurityExchangeComplete, Is.True);
            Assert.That(session.State.DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(FtpSequenceValidator.InspectCommand(
                CreateCommand(FtpCommandKind.Prot, "C"), session.State).ReplyCode,
                Is.EqualTo(FtpReplyCodes.BadCommandSequence));
            Assert.That(events.Events, Has.Count.EqualTo(1));
            Assert.That(events.Events[0].Kind, Is.EqualTo(FtpEventKind.Tls));
            Assert.That(events.Events[0].HasCommandKind, Is.True);
            Assert.That(events.Events[0].CommandKind, Is.EqualTo(FtpCommandKind.Ccc));
            Assert.That(events.Events[0].DataProtectionLevel,
                Is.EqualTo(FtpDataProtectionLevel.Private));
            Assert.That(events.Events[0].Timestamp.HasValue, Is.True);
            Assert.That(events.Events[0].Redacted, Is.True);
        });
    }

    private static FtpTlsOptions CreateClearControlOptions()
    {
        FtpTlsOptions options = new FtpTlsOptions();
        options.AllowClearCommandChannel = true;
        return options;
    }

    private static FtpSessionStateMachine CreateTlsSession()
    {
        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        session.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Auth, "TLS"),
            CreateReply(FtpReplyCodes.SecurityExchangeComplete, "Begin TLS"));
        session.CompleteSecurityExchange(true);
        return session;
    }

    private static void SelectPrivateProtection(FtpSessionStateMachine session)
    {
        session.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Pbsz, "0"),
            CreateReply(FtpReplyCodes.CommandOkay, "PBSZ set"));
        session.ApplyCommandReply(
            CreateCommand(FtpCommandKind.Prot, "P"),
            CreateReply(FtpReplyCodes.CommandOkay, "Private"));
    }

    private static FtpTlsClientStream CreateClientStream(
        FtpTlsOptions options, bool leaveTransportOpen)
    {
        FtpTlsClientStreamRequest request = new FtpTlsClientStreamRequest();
        request.TransportStream = new MemoryStream();
        request.Options = options;
        request.TargetHost = "localhost";
        request.LeaveTransportOpen = leaveTransportOpen;
        return new FtpTlsClientStream(request);
    }

    private static FtpTlsServerStream CreateServerStream(
        FtpTlsOptions options, bool leaveTransportOpen)
    {
        FtpTlsServerStreamRequest request = new FtpTlsServerStreamRequest();
        request.TransportStream = new MemoryStream();
        request.Options = options;
        request.LeaveTransportOpen = leaveTransportOpen;
        request.CertificateSelection = (sender, name) =>
            throw new InvalidOperationException("The test must not begin a TLS handshake.");
        return new FtpTlsServerStream(request);
    }

    private static FtpCommand CreateCccCommand()
    {
        return CreateCommand(FtpCommandKind.Ccc, null);
    }

    private static FtpCommand CreateCommand(FtpCommandKind kind, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
        command.RawText = argument == null ? command.Verb : command.Verb + " " + argument;
        return command;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }

    private class RecordingEventSink : IFtpEventSink
    {
        internal List<FtpEvent> Events = new List<FtpEvent>();

        public void RecordEvent(FtpEvent eventData)
        {
            Events.Add(eventData);
        }
    }
}
