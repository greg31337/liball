using websocket;

namespace websocket.Tests;

public class WebSocketConnectionStateMachineTests
{
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http1Upgrade)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void OpensFromConnectingAndRetainsRoleAndTransport(
        WebSocketRole role,
        WebSocketHandshakeTransport transport)
    {
        var state = new WebSocketConnectionStateMachine(role, transport, 1000);

        WebSocketConnectionTransitionResult opened = state.OpenConnection();
        WebSocketConnectionTransitionResult repeated = state.OpenConnection();

        Assert.Multiple(() =>
        {
            Assert.That(opened.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(opened.State, Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(opened.Changed, Is.True);
            Assert.That(state.Role, Is.EqualTo(role));
            Assert.That(state.Transport, Is.EqualTo(transport));
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.Error.Kind, Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(state.State, Is.EqualTo(WebSocketConnectionState.Open));
        });
    }

    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http1Upgrade, false)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http1Upgrade, true)]
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http2ExtendedConnect, false)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http2ExtendedConnect, true)]
    [TestCase(WebSocketRole.Client, WebSocketHandshakeTransport.Http3ExtendedConnect, false)]
    [TestCase(WebSocketRole.Server, WebSocketHandshakeTransport.Http3ExtendedConnect, true)]
    public void LocalCloseCompletesCleanlyAfterPeerCloseAndTransportEnd(
        WebSocketRole role,
        WebSocketHandshakeTransport transport,
        bool shouldEndTransport)
    {
        var state = OpenState(role, transport, 1000);
        state.AdvanceElapsedMilliseconds(25);

        WebSocketConnectionTransitionResult started = state.StartLocalClose();
        WebSocketConnectionTransitionResult duplicate = state.StartLocalClose();
        WebSocketConnectionTransitionResult sent = state.RecordLocalCloseSent();
        WebSocketConnectionTransitionResult received = state.RecordRemoteClose();

        Assert.Multiple(() =>
        {
            Assert.That(started.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(started.Changed, Is.True);
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(sent.Changed, Is.True);
            Assert.That(received.Changed, Is.True);
            Assert.That(state.LocalInitiatedClosing, Is.True);
            Assert.That(state.HasClosingStartedMilliseconds, Is.True);
            Assert.That(state.ClosingStartedMilliseconds, Is.EqualTo(25));
            Assert.That(state.LocalCloseSent, Is.True);
            Assert.That(state.RemoteCloseReceived, Is.True);
            Assert.That(state.ShouldEndTransport, Is.EqualTo(shouldEndTransport));
        });

        WebSocketConnectionTransitionResult ended = state.EndTransport();

        Assert.Multiple(() =>
        {
            Assert.That(ended.Status, Is.EqualTo(WebSocketParseStatus.End));
            Assert.That(ended.State, Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(state.TransportEnded, Is.True);
            Assert.That(state.CleanClose, Is.True);
            Assert.That(state.HasFailure, Is.False);
            Assert.That(state.ShouldEndTransport, Is.True);
        });
    }

    [Test]
    public void RemoteInitiatedCloseCanBeAnsweredWithoutChangingInitiator()
    {
        var state = OpenState(WebSocketRole.Server, 1000);

        WebSocketConnectionTransitionResult received = state.RecordRemoteClose();
        WebSocketConnectionTransitionResult response = state.StartLocalClose();
        WebSocketConnectionTransitionResult sent = state.RecordLocalCloseSent();
        WebSocketConnectionTransitionResult ended = state.EndTransport();

        Assert.Multiple(() =>
        {
            Assert.That(received.Changed, Is.True);
            Assert.That(response.Changed, Is.True);
            Assert.That(sent.Changed, Is.True);
            Assert.That(state.LocalInitiatedClosing, Is.False);
            Assert.That(state.LocalCloseStarted, Is.True);
            Assert.That(state.LocalCloseSent, Is.True);
            Assert.That(state.RemoteCloseReceived, Is.True);
            Assert.That(ended.State, Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(state.CleanClose, Is.True);
        });
    }

    [Test]
    public void SimultaneousClosePreservesLocalInitiationAndIsIdempotent()
    {
        var state = OpenState(WebSocketRole.Client, 1000);

        state.StartLocalClose();
        state.RecordLocalCloseSent();
        WebSocketConnectionTransitionResult received = state.RecordRemoteClose();
        WebSocketConnectionTransitionResult duplicate = state.RecordRemoteClose();

        Assert.Multiple(() =>
        {
            Assert.That(received.Changed, Is.True);
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(state.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(state.LocalInitiatedClosing, Is.True);
            Assert.That(state.CleanClose, Is.False);
        });
    }

    [Test]
    public void PrematureTransportEndFailsWithAbnormalClosure()
    {
        var open = OpenState(WebSocketRole.Client, 1000);
        var closing = OpenState(WebSocketRole.Server, 1000);
        closing.StartLocalClose();
        closing.RecordLocalCloseSent();

        WebSocketConnectionTransitionResult openEnd = open.EndTransport();
        WebSocketConnectionTransitionResult closingEnd = closing.EndTransport();

        Assert.Multiple(() =>
        {
            AssertTerminalTransportFailure(
                open,
                openEnd,
                "connection-transport-ended-before-close");
            AssertTerminalTransportFailure(
                closing,
                closingEnd,
                "connection-transport-ended-before-close");
            Assert.That(open.TransportReset, Is.False);
            Assert.That(closing.CleanClose, Is.False);
            Assert.That(open.ShouldEndTransport, Is.True);
            Assert.That(closing.ShouldEndTransport, Is.True);
        });
    }

    [TestCase(
        WebSocketHandshakeTransport.Http1Upgrade,
        "connection-transport-ended-before-close")]
    [TestCase(
        WebSocketHandshakeTransport.Http2ExtendedConnect,
        "connection-http2-end-stream-before-close")]
    [TestCase(
        WebSocketHandshakeTransport.Http3ExtendedConnect,
        "connection-http3-fin-before-close")]
    public void PrematureInputEndUsesTransportSpecificMapping(
        WebSocketHandshakeTransport transport,
        string identifier)
    {
        var state = OpenState(WebSocketRole.Client, transport, 1000);

        WebSocketConnectionTransitionResult ended = state.EndTransport();

        Assert.Multiple(() =>
        {
            AssertTerminalTransportFailure(state, ended, identifier);
            Assert.That(ended.Error.HasCloseCode, Is.True);
            Assert.That(ended.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(ended.Error.HasKnownCloseCode, Is.True);
            Assert.That(ended.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.AbnormalClosure));
            Assert.That(state.RemoteCloseReceived, Is.False);
        });
    }

    [Test]
    public void TransportResetFailsOnceAndRecordsResetFacts()
    {
        var state = OpenState(WebSocketRole.Client, 1000);

        WebSocketConnectionTransitionResult reset = state.ResetTransport();
        WebSocketConnectionTransitionResult duplicate = state.ResetTransport();

        Assert.Multiple(() =>
        {
            AssertTerminalTransportFailure(
                state,
                reset,
                "connection-transport-reset");
            Assert.That(state.TransportEnded, Is.True);
            Assert.That(state.TransportReset, Is.True);
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(duplicate.Error.Identifier,
                Is.EqualTo("connection-transport-reset"));
            Assert.That(state.ShouldEndTransport, Is.False);
        });
    }

    [TestCase(
        WebSocketHandshakeTransport.Http1Upgrade,
        "connection-transport-reset")]
    [TestCase(
        WebSocketHandshakeTransport.Http2ExtendedConnect,
        "connection-http2-stream-reset")]
    [TestCase(
        WebSocketHandshakeTransport.Http3ExtendedConnect,
        "connection-http3-stream-reset")]
    public void ResetUsesTransportSpecificMapping(
        WebSocketHandshakeTransport transport,
        string identifier)
    {
        var state = OpenState(WebSocketRole.Server, transport, 1000);

        WebSocketConnectionTransitionResult reset = state.ResetTransport();

        Assert.Multiple(() =>
        {
            AssertTerminalTransportFailure(state, reset, identifier);
            Assert.That(state.TransportEnded, Is.True);
            Assert.That(state.TransportReset, Is.True);
            Assert.That(state.ShouldEndTransport, Is.False);
        });
    }

    [Test]
    public void ProtocolFailureIsCopiedAndRemainsTheFirstTerminalFailure()
    {
        var state = OpenState(WebSocketRole.Server, 1000);
        var error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Protocol;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.ProtocolError;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.ProtocolError;
        error.Identifier = "frame-invalid";
        error.Diagnostic = "The frame is invalid.";

        WebSocketConnectionTransitionResult failed =
            state.FailConnection(error);
        error.Identifier = "mutated";
        WebSocketConnectionTransitionResult aborted = state.AbortConnection();
        WebSocketConnectionTransitionResult ended = state.EndTransport();

        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(failed.Error.Identifier, Is.EqualTo("frame-invalid"));
            Assert.That(state.FailureError.Identifier, Is.EqualTo("frame-invalid"));
            Assert.That(aborted.Changed, Is.True);
            Assert.That(aborted.Error.Identifier, Is.EqualTo("frame-invalid"));
            Assert.That(state.ShouldEndTransport, Is.False);
            Assert.That(ended.Error.Identifier, Is.EqualTo("frame-invalid"));
            Assert.That(state.TransportEnded, Is.True);
        });
    }

    [Test]
    public void CloseTimeoutUsesMonotonicTimeAndExactBoundary()
    {
        var state = OpenState(WebSocketRole.Client, 100);
        state.AdvanceElapsedMilliseconds(50);
        state.StartLocalClose();

        WebSocketConnectionTransitionResult before =
            state.AdvanceElapsedMilliseconds(149);
        WebSocketConnectionTransitionResult backwards =
            state.AdvanceElapsedMilliseconds(148);
        WebSocketConnectionTransitionResult boundary =
            state.AdvanceElapsedMilliseconds(150);

        Assert.Multiple(() =>
        {
            Assert.That(before.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(backwards.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(backwards.Error.Identifier,
                Is.EqualTo("connection-elapsed-time-invalid"));
            Assert.That(state.ElapsedMilliseconds, Is.EqualTo(150));
            Assert.That(boundary.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(boundary.Error.Identifier,
                Is.EqualTo("connection-close-timeout"));
            Assert.That(boundary.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(state.ShouldEndTransport, Is.True);
        });
    }

    [Test]
    public void ConnectionInformationCopiesTransportCloseFactsAndError()
    {
        var state = OpenState(
            WebSocketRole.Client,
            WebSocketHandshakeTransport.Http2ExtendedConnect,
            100);
        state.AdvanceElapsedMilliseconds(10);
        state.StartLocalClose();
        state.AdvanceElapsedMilliseconds(110);

        WebSocketConnectionInfo first = state.GetConnectionInformation();
        first.Error.Identifier = "mutated";
        WebSocketConnectionInfo second = state.GetConnectionInformation();

        Assert.Multiple(() =>
        {
            Assert.That(first.Role, Is.EqualTo(WebSocketRole.Client));
            Assert.That(first.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(first.State,
                Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(first.ElapsedMilliseconds, Is.EqualTo(110));
            Assert.That(first.CleanClose, Is.False);
            Assert.That(first.TransportEnded, Is.False);
            Assert.That(first.TransportReset, Is.False);
            Assert.That(first.ShouldEndTransport, Is.True);
            Assert.That(first.HasError, Is.True);
            Assert.That(first.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
            Assert.That(second.Error.Identifier,
                Is.EqualTo("connection-close-timeout"));
            Assert.That(second.Error, Is.Not.SameAs(first.Error));
        });
    }

    [TestCase(WebSocketRole.Client, false)]
    [TestCase(WebSocketRole.Server, true)]
    public void ConnectionInformationExposesRoleCorrectCloseGuidance(
        WebSocketRole role,
        bool expectedBeforePeerEnd)
    {
        var state = OpenState(
            role,
            WebSocketHandshakeTransport.Http3ExtendedConnect,
            1000);
        state.StartLocalClose();
        state.RecordLocalCloseSent();
        state.RecordRemoteClose();

        WebSocketConnectionInfo beforePeerEnd =
            state.GetConnectionInformation();
        state.EndTransport();
        WebSocketConnectionInfo afterPeerEnd =
            state.GetConnectionInformation();

        Assert.Multiple(() =>
        {
            Assert.That(beforePeerEnd.State,
                Is.EqualTo(WebSocketConnectionState.Closing));
            Assert.That(beforePeerEnd.TransportEnded, Is.False);
            Assert.That(beforePeerEnd.ShouldEndTransport,
                Is.EqualTo(expectedBeforePeerEnd));
            Assert.That(afterPeerEnd.State,
                Is.EqualTo(WebSocketConnectionState.Closed));
            Assert.That(afterPeerEnd.TransportEnded, Is.True);
            Assert.That(afterPeerEnd.CleanClose, Is.True);
            Assert.That(afterPeerEnd.ShouldEndTransport, Is.True);
            Assert.That(afterPeerEnd.HasError, Is.False);
        });
    }

    [TestCase(WebSocketConnectionState.Connecting)]
    [TestCase(WebSocketConnectionState.Open)]
    [TestCase(WebSocketConnectionState.Closing)]
    public void AbortFailsEveryNonterminalState(
        WebSocketConnectionState initialState)
    {
        var state = new WebSocketConnectionStateMachine(
            WebSocketRole.Client,
            WebSocketHandshakeTransport.Http1Upgrade,
            1000);
        if (initialState != WebSocketConnectionState.Connecting)
        {
            state.OpenConnection();
        }

        if (initialState == WebSocketConnectionState.Closing)
        {
            state.StartLocalClose();
        }

        WebSocketConnectionTransitionResult result = state.AbortConnection();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(WebSocketConnectionState.Failed));
            Assert.That(result.Error.Identifier, Is.EqualTo("connection-aborted"));
            Assert.That(state.TransportEnded, Is.True);
            Assert.That(state.TransportReset, Is.False);
        });
    }

    [Test]
    public void IllegalTransitionsDoNotCorruptConnectingOrOpenState()
    {
        var connecting = new WebSocketConnectionStateMachine(
            WebSocketRole.Server,
            WebSocketHandshakeTransport.Http1Upgrade,
            1000);
        WebSocketConnectionTransitionResult earlyClose =
            connecting.StartLocalClose();
        WebSocketConnectionTransitionResult earlyRemote =
            connecting.RecordRemoteClose();
        var open = OpenState(WebSocketRole.Client, 1000);
        WebSocketConnectionTransitionResult unscheduledSend =
            open.RecordLocalCloseSent();

        Assert.Multiple(() =>
        {
            Assert.That(earlyClose.Error.Identifier,
                Is.EqualTo("connection-local-close-state-invalid"));
            Assert.That(earlyRemote.Error.Identifier,
                Is.EqualTo("connection-remote-close-state-invalid"));
            Assert.That(connecting.State,
                Is.EqualTo(WebSocketConnectionState.Connecting));
            Assert.That(unscheduledSend.Error.Identifier,
                Is.EqualTo("connection-close-send-state-invalid"));
            Assert.That(open.State, Is.EqualTo(WebSocketConnectionState.Open));
            Assert.That(open.HasFailure, Is.False);
        });
    }

    [Test]
    public void ConstructorRejectsUnknownVocabularyAndInvalidTimeout()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketConnectionStateMachine(
                    (WebSocketRole)99,
                    WebSocketHandshakeTransport.Http1Upgrade,
                    1));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketConnectionStateMachine(
                    WebSocketRole.Client,
                    (WebSocketHandshakeTransport)99,
                    1));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketConnectionStateMachine(
                    WebSocketRole.Client,
                    WebSocketHandshakeTransport.Http1Upgrade,
                    0));
        });
    }

    static WebSocketConnectionStateMachine OpenState(
        WebSocketRole role,
        int timeout)
    {
        return OpenState(
            role,
            WebSocketHandshakeTransport.Http1Upgrade,
            timeout);
    }

    static WebSocketConnectionStateMachine OpenState(
        WebSocketRole role,
        WebSocketHandshakeTransport transport,
        int timeout)
    {
        var state = new WebSocketConnectionStateMachine(
            role,
            transport,
            timeout);
        state.OpenConnection();
        return state;
    }

    static void AssertTerminalTransportFailure(
        WebSocketConnectionStateMachine state,
        WebSocketConnectionTransitionResult result,
        string identifier)
    {
        Assert.That(result.Status,
            Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        Assert.That(result.State, Is.EqualTo(WebSocketConnectionState.Failed));
        Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Transport));
        Assert.That(result.Error.CloseCode,
            Is.EqualTo(WebSocketCloseCodes.AbnormalClosure));
        Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        Assert.That(state.HasFailure, Is.True);
        Assert.That(state.CleanClose, Is.False);
    }
}
