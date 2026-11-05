using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SessionStateTests
{
    [TestCase(SessionRole.Initiator)]
    [TestCase(SessionRole.Acceptor)]
    public void BothRolesFollowConnectionLogonAndLogoutLifecycle(SessionRole role)
    {
        SessionStateMachine machine = new SessionStateMachine(role);

        AssertTransition(machine.StartSession(), SessionState.Stopped, SessionState.Disconnected, SessionEventKind.Started, "session-started");
        AssertTransition(machine.BeginConnection(), SessionState.Disconnected, SessionState.Connecting, SessionEventKind.None, "connection-started");
        AssertTransition(machine.CompleteConnection(), SessionState.Connecting, SessionState.Connected, SessionEventKind.Connected, "transport-connected");
        AssertTransition(machine.BeginLogon(), SessionState.Connected, SessionState.LogonPending, SessionEventKind.None, "logon-pending");
        AssertTransition(machine.CompleteLogon(), SessionState.LogonPending, SessionState.Active, SessionEventKind.Logon, "session-active");
        AssertTransition(machine.BeginLogout(), SessionState.Active, SessionState.LogoutPending, SessionEventKind.None, "logout-pending");
        AssertTransition(machine.CompleteLogout(), SessionState.LogoutPending, SessionState.Disconnected, SessionEventKind.Logout, "session-logged-out");
    }

    [TestCase(SessionRole.Initiator)]
    [TestCase(SessionRole.Acceptor)]
    public void BothRolesEnterAndCompleteRecoveryOnlyWhileActive(SessionRole role)
    {
        SessionStateMachine machine = ActiveMachine(role);

        SessionStateTransitionResult started = machine.BeginRecovery();
        SessionStateTransitionResult repeated = machine.BeginRecovery();
        SessionStateTransitionResult completed = machine.CompleteRecovery();

        Assert.Multiple(() =>
        {
            AssertTransition(started, SessionState.Active, SessionState.Recovering, SessionEventKind.RecoveryStarted, "recovery-started");
            AssertRejected(repeated, SessionState.Recovering, "invalid-session-state-transition");
            AssertTransition(completed, SessionState.Recovering, SessionState.Active, SessionEventKind.RecoveryCompleted, "recovery-completed");
        });
    }

    [TestCase(SessionState.Connecting)]
    [TestCase(SessionState.Connected)]
    [TestCase(SessionState.LogonPending)]
    [TestCase(SessionState.Active)]
    [TestCase(SessionState.LogoutPending)]
    [TestCase(SessionState.Recovering)]
    public void TransportLossReturnsEveryConnectedPhaseToDisconnected(SessionState state)
    {
        SessionStateMachine machine = MachineInState(SessionRole.Initiator, state);

        SessionStateTransitionResult result = machine.DisconnectTransport();

        AssertTransition(result, state, SessionState.Disconnected, SessionEventKind.Disconnected, "transport-disconnected");
    }

    [Test]
    public void OnlyInitiatorCanScheduleReconnectAndReconnect()
    {
        SessionStateMachine initiator = new SessionStateMachine(SessionRole.Initiator);
        initiator.StartSession();
        SessionStateMachine acceptor = new SessionStateMachine(SessionRole.Acceptor);
        acceptor.StartSession();

        SessionStateTransitionResult scheduled = initiator.ScheduleReconnect();
        SessionStateTransitionResult connecting = initiator.BeginConnection();
        SessionStateTransitionResult rejected = acceptor.ScheduleReconnect();

        Assert.Multiple(() =>
        {
            AssertTransition(scheduled, SessionState.Disconnected, SessionState.Reconnecting, SessionEventKind.None, "reconnect-scheduled");
            AssertTransition(connecting, SessionState.Reconnecting, SessionState.Connecting, SessionEventKind.None, "connection-started");
            AssertRejected(rejected, SessionState.Disconnected, "reconnect-not-supported-for-acceptor");
        });
    }

    [Test]
    public void LogoutCanInterruptRecovery()
    {
        SessionStateMachine machine = ActiveMachine(SessionRole.Acceptor);
        machine.BeginRecovery();

        SessionStateTransitionResult result = machine.BeginLogout();

        AssertTransition(result, SessionState.Recovering, SessionState.LogoutPending, SessionEventKind.None, "logout-pending");
    }

    [Test]
    public void InvalidTransitionsAreRejectedWithoutMutation()
    {
        SessionStateMachine machine = new SessionStateMachine(SessionRole.Initiator);

        SessionStateTransitionResult connect = machine.BeginConnection();
        SessionStateTransitionResult logon = machine.CompleteLogon();
        SessionStateTransitionResult recovery = machine.BeginRecovery();
        SessionStateTransitionResult disconnect = machine.DisconnectTransport();

        Assert.Multiple(() =>
        {
            AssertRejected(connect, SessionState.Stopped, "invalid-session-state-transition");
            AssertRejected(logon, SessionState.Stopped, "invalid-session-state-transition");
            AssertRejected(recovery, SessionState.Stopped, "invalid-session-state-transition");
            AssertRejected(disconnect, SessionState.Stopped, "invalid-session-state-transition");
            Assert.That(machine.State, Is.EqualTo(SessionState.Stopped));
        });
    }

    [Test]
    public void FaultStopAndRestartHaveDeterministicSemantics()
    {
        SessionStateMachine machine = ActiveMachine(SessionRole.Initiator);

        SessionStateTransitionResult faulted = machine.FaultSession();
        SessionStateTransitionResult repeatedFault = machine.FaultSession();
        SessionStateTransitionResult stopped = machine.StopSession();
        SessionStateTransitionResult repeatedStop = machine.StopSession();
        SessionStateTransitionResult restarted = machine.StartSession();

        Assert.Multiple(() =>
        {
            AssertTransition(faulted, SessionState.Active, SessionState.Faulted, SessionEventKind.Error, "session-faulted");
            AssertRejected(repeatedFault, SessionState.Faulted, "invalid-session-state-transition");
            AssertTransition(stopped, SessionState.Faulted, SessionState.Stopped, SessionEventKind.Stopped, "session-stopped");
            Assert.That(repeatedStop.Accepted, Is.True);
            Assert.That(repeatedStop.Changed, Is.False);
            Assert.That(repeatedStop.State, Is.EqualTo(SessionState.Stopped));
            Assert.That(repeatedStop.Code, Is.EqualTo("session-already-stopped"));
            AssertTransition(restarted, SessionState.Stopped, SessionState.Disconnected, SessionEventKind.Started, "session-started");
        });
    }

    [Test]
    public void ConstructorRejectsUnknownRole()
    {
        Assert.Throws<FixSessionException>(() => new SessionStateMachine((SessionRole)99));
    }

    private static SessionStateMachine ActiveMachine(SessionRole role)
    {
        return MachineInState(role, SessionState.Active);
    }

    private static SessionStateMachine MachineInState(SessionRole role, SessionState target)
    {
        SessionStateMachine machine = new SessionStateMachine(role);
        machine.StartSession();
        if (target == SessionState.Disconnected)
        {
            return machine;
        }
        machine.BeginConnection();
        if (target == SessionState.Connecting)
        {
            return machine;
        }
        machine.CompleteConnection();
        if (target == SessionState.Connected)
        {
            return machine;
        }
        machine.BeginLogon();
        if (target == SessionState.LogonPending)
        {
            return machine;
        }
        machine.CompleteLogon();
        if (target == SessionState.Active)
        {
            return machine;
        }
        if (target == SessionState.LogoutPending)
        {
            machine.BeginLogout();
            return machine;
        }
        if (target == SessionState.Recovering)
        {
            machine.BeginRecovery();
            return machine;
        }
        Assert.Fail($"Unsupported test state {target}.");
        return machine;
    }

    private static void AssertTransition(SessionStateTransitionResult result, SessionState previous, SessionState current, SessionEventKind eventKind, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.PreviousState, Is.EqualTo(previous));
            Assert.That(result.State, Is.EqualTo(current));
            Assert.That(result.EventKind, Is.EqualTo(eventKind));
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.Text, Is.Not.Empty);
        });
    }

    private static void AssertRejected(SessionStateTransitionResult result, SessionState state, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.PreviousState, Is.EqualTo(state));
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.EventKind, Is.EqualTo(SessionEventKind.None));
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.Text, Is.Not.Empty);
        });
    }
}
