namespace ftp.Tests;

public class FtpAuthTlsCoordinatorTests
{
    [TestCase("TLS", "TLS")]
    [TestCase("tls-c", "TLS-C")]
    public void Client_accepts_TLS_mechanisms_and_requires_FEAT_after_the_handshake(
        string mechanism, string expectedMechanism)
    {
        FtpSessionStateMachine session = CreateOpenSession();
        session.State.SelectedHost = "files.example.test";
        session.State.SessionState = FtpSessionState.HostSelected;
        FtpClientAuthTlsCoordinator coordinator = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsRequired, false, session);

        FtpCommand authorization = coordinator.BeginAuthorization(mechanism);
        coordinator.ApplyAuthorizationReply(CreateReply(234, "Proceed with TLS"));

        Assert.Multiple(() =>
        {
            Assert.That(authorization.Kind, Is.EqualTo(FtpCommandKind.Auth));
            Assert.That(authorization.Argument, Is.EqualTo(expectedMechanism));
            Assert.That(coordinator.State.HandshakePending, Is.True);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.False);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticating));
            Assert.That(session.State.SecurityExchangeInProgress, Is.True);
        });

        coordinator.CompleteHandshake();

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.ControlProtected, Is.True);
            Assert.That(coordinator.State.FeatureRefreshRequired, Is.True);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.False);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.HostSelected));
            Assert.That(session.State.ControlProtected, Is.True);
        });

        FtpCommand feature = coordinator.BeginFeatureRefresh();
        coordinator.ApplyFeatureRefreshReply(CreateReply(211, "Features"));

        Assert.Multiple(() =>
        {
            Assert.That(feature.Kind, Is.EqualTo(FtpCommandKind.Feat));
            Assert.That(coordinator.State.FeatureRefreshRequired, Is.False);
            Assert.That(coordinator.State.FeatureRefreshPending, Is.False);
            Assert.That(coordinator.State.FeatureRefreshComplete, Is.True);
            Assert.That(coordinator.State.FeatureRefreshSucceeded, Is.True);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.True);
        });
    }

    [Test]
    public void Required_TLS_terminates_after_rejection_even_when_plaintext_was_opted_in()
    {
        FtpSessionStateMachine session = CreateOpenSession();
        FtpClientAuthTlsCoordinator coordinator = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsRequired, true, session);
        coordinator.BeginAuthorization("TLS");

        coordinator.ApplyAuthorizationReply(CreateReply(534, "TLS denied"));

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.TlsRejected, Is.True);
            Assert.That(coordinator.State.PlaintextFallback, Is.False);
            Assert.That(coordinator.State.Failed, Is.True);
            Assert.That(coordinator.State.ConnectionMustClose, Is.True);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.False);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
        });
    }

    [Test]
    public void Opportunistic_TLS_falls_back_only_after_a_clean_negative_AUTH_reply()
    {
        Assert.Throws<FtpConfigurationException>(() =>
            new FtpClientAuthTlsCoordinator(
                FtpSecurityMode.ExplicitTlsOpportunistic, false, CreateOpenSession()));

        FtpSessionStateMachine rejectedSession = CreateOpenSession();
        FtpClientAuthTlsCoordinator rejected = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsOpportunistic, true, rejectedSession);
        rejected.BeginAuthorization("TLS");
        rejected.ApplyAuthorizationReply(CreateReply(504, "TLS unsupported"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State.PlaintextFallback, Is.True);
            Assert.That(rejected.State.ApplicationCommandsAllowed, Is.True);
            Assert.That(rejected.State.ConnectionMustClose, Is.False);
            Assert.That(rejectedSession.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
        });

        FtpSessionStateMachine failedSession = CreateOpenSession();
        FtpClientAuthTlsCoordinator failed = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsOpportunistic, true, failedSession);
        failed.BeginAuthorization("TLS");
        failed.ApplyAuthorizationReply(CreateReply(234, "Proceed"));
        failed.FailHandshake();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.PlaintextFallback, Is.False);
            Assert.That(failed.State.ConnectionMustClose, Is.True);
            Assert.That(failedSession.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
        });
    }

    [Test]
    public void Client_transport_and_malformed_reply_failures_terminate_the_connection()
    {
        FtpSessionStateMachine transportSession = CreateOpenSession();
        FtpClientAuthTlsCoordinator transport = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsOpportunistic, true, transportSession);
        transport.BeginAuthorization("TLS");
        transport.FailAuthorizationExchange();

        FtpSessionStateMachine malformedSession = CreateOpenSession();
        FtpClientAuthTlsCoordinator malformed = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsOpportunistic, true, malformedSession);
        malformed.BeginAuthorization("TLS");

        Assert.Multiple(() =>
        {
            Assert.That(transport.State.ConnectionMustClose, Is.True);
            Assert.That(transport.State.PlaintextFallback, Is.False);
            Assert.Throws<FtpProtocolException>(() =>
                malformed.ApplyAuthorizationReply(CreateReply(334, "Continue")));
            Assert.That(malformed.State.ConnectionMustClose, Is.True);
            Assert.That(malformed.State.PlaintextFallback, Is.False);
            Assert.That(malformedSession.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
        });
    }

    [Test]
    public void Negative_FEAT_reply_completes_refresh_without_claiming_feature_support()
    {
        FtpSessionStateMachine session = CreateOpenSession();
        FtpClientAuthTlsCoordinator coordinator = new FtpClientAuthTlsCoordinator(
            FtpSecurityMode.ExplicitTlsRequired, false, session);
        coordinator.BeginAuthorization("TLS");
        coordinator.ApplyAuthorizationReply(CreateReply(234, "Proceed"));
        coordinator.CompleteHandshake();
        coordinator.BeginFeatureRefresh();

        coordinator.ApplyFeatureRefreshReply(CreateReply(502, "FEAT unavailable"));

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.FeatureRefreshComplete, Is.True);
            Assert.That(coordinator.State.FeatureRefreshSucceeded, Is.False);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.True);
            Assert.That(coordinator.State.ControlProtected, Is.True);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
        });
    }

    [TestCase("TLS")]
    [TestCase("tls-c")]
    public void Server_accepts_TLS_and_TLS_C_then_protects_the_control_session(string mechanism)
    {
        FtpSessionStateMachine session = CreateOpenSession();
        FtpServerAuthTlsCoordinator coordinator =
            new FtpServerAuthTlsCoordinator(true, session);

        FtpReply reply = coordinator.HandleAuthorizationCommand(
            CreateCommand(FtpCommandKind.Auth, mechanism));

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.SecurityExchangeComplete));
            Assert.That(coordinator.State.HandshakePending, Is.True);
            Assert.That(coordinator.State.Mechanism,
                Is.EqualTo(mechanism.Equals("tls-c", StringComparison.OrdinalIgnoreCase) ?
                    "TLS-C" : "TLS"));
            Assert.That(session.State.SecurityExchangeInProgress, Is.True);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Authenticating));
        });

        coordinator.CompleteHandshake();

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.State.ControlProtected, Is.True);
            Assert.That(coordinator.State.ApplicationCommandsAllowed, Is.True);
            Assert.That(session.State.SecurityExchangeComplete, Is.True);
            Assert.That(session.State.ControlProtected, Is.True);
            Assert.That(session.State.SessionState, Is.EqualTo(FtpSessionState.Connected));
        });
    }

    [Test]
    public void Server_rejects_unavailable_mechanisms_and_closes_after_handshake_failure()
    {
        FtpSessionStateMachine unavailableSession = CreateOpenSession();
        FtpServerAuthTlsCoordinator unavailable =
            new FtpServerAuthTlsCoordinator(false, unavailableSession);

        FtpReply unknownReply = unavailable.HandleAuthorizationCommand(
            CreateCommand(FtpCommandKind.Auth, "GSSAPI"));
        FtpReply unavailableReply = unavailable.HandleAuthorizationCommand(
            CreateCommand(FtpCommandKind.Auth, "TLS"));

        Assert.Multiple(() =>
        {
            Assert.That(unknownReply.Code,
                Is.EqualTo(FtpReplyCodes.CommandParameterNotImplemented));
            Assert.That(unavailableReply.Code,
                Is.EqualTo(FtpReplyCodes.RequestDeniedByPolicy));
            Assert.That(unavailable.State.HandshakePending, Is.False);
            Assert.That(unavailableSession.State.SessionState,
                Is.EqualTo(FtpSessionState.Connected));
        });

        FtpSessionStateMachine failedSession = CreateOpenSession();
        FtpServerAuthTlsCoordinator failed =
            new FtpServerAuthTlsCoordinator(true, failedSession);
        failed.HandleAuthorizationCommand(CreateCommand(FtpCommandKind.Auth, "TLS"));
        failed.FailHandshake();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.Failed, Is.True);
            Assert.That(failed.State.ConnectionMustClose, Is.True);
            Assert.That(failed.State.ControlProtected, Is.False);
            Assert.That(failedSession.State.SessionState, Is.EqualTo(FtpSessionState.Closed));
            Assert.That(failed.HandleAuthorizationCommand(
                CreateCommand(FtpCommandKind.Auth, "TLS")).Code,
                Is.EqualTo(FtpReplyCodes.ServiceUnavailable));
        });
    }

    private static FtpSessionStateMachine CreateOpenSession()
    {
        FtpSessionStateMachine session = new FtpSessionStateMachine();
        session.OpenConnection();
        return session;
    }

    private static FtpCommand CreateCommand(FtpCommandKind kind, string? argument)
    {
        FtpCommand command = new FtpCommand();
        command.Kind = kind;
        command.Verb = FtpCommandCatalog.ResolveVerb(kind);
        command.Argument = argument;
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
}
