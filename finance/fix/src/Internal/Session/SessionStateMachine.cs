namespace fix;

internal class SessionStateMachine
{
    internal SessionRole Role;
    internal SessionState State;

    internal SessionStateMachine(SessionRole role)
    {
        if (role != SessionRole.Initiator && role != SessionRole.Acceptor)
        {
            throw new FixSessionException("The session role is invalid.");
        }
        Role = role;
        State = SessionState.Stopped;
    }

    internal SessionStateTransitionResult StartSession()
    {
        lock (this)
        {
            if (State != SessionState.Stopped)
            {
                return RejectTransition("start-session");
            }
            return ChangeState(SessionState.Disconnected, SessionEventKind.Started, "session-started", "The session started.");
        }
    }

    internal SessionStateTransitionResult BeginConnection()
    {
        lock (this)
        {
            if (State != SessionState.Disconnected && State != SessionState.Reconnecting)
            {
                return RejectTransition("begin-connection");
            }
            return ChangeState(SessionState.Connecting, SessionEventKind.None, "connection-started", "Transport connection work started.");
        }
    }

    internal SessionStateTransitionResult CompleteConnection()
    {
        lock (this)
        {
            if (State != SessionState.Connecting)
            {
                return RejectTransition("complete-connection");
            }
            return ChangeState(SessionState.Connected, SessionEventKind.Connected, "transport-connected", "The transport connected.");
        }
    }

    internal SessionStateTransitionResult BeginLogon()
    {
        lock (this)
        {
            if (State != SessionState.Connected)
            {
                return RejectTransition("begin-logon");
            }
            return ChangeState(SessionState.LogonPending, SessionEventKind.None, "logon-pending", "The session is awaiting logon completion.");
        }
    }

    internal SessionStateTransitionResult CompleteLogon()
    {
        lock (this)
        {
            if (State != SessionState.LogonPending)
            {
                return RejectTransition("complete-logon");
            }
            return ChangeState(SessionState.Active, SessionEventKind.Logon, "session-active", "The logon exchange completed.");
        }
    }

    internal SessionStateTransitionResult BeginLogout()
    {
        lock (this)
        {
            if (State != SessionState.Active && State != SessionState.Recovering)
            {
                return RejectTransition("begin-logout");
            }
            return ChangeState(SessionState.LogoutPending, SessionEventKind.None, "logout-pending", "The session is awaiting logout completion.");
        }
    }

    internal SessionStateTransitionResult CompleteLogout()
    {
        lock (this)
        {
            if (State != SessionState.LogoutPending)
            {
                return RejectTransition("complete-logout");
            }
            return ChangeState(SessionState.Disconnected, SessionEventKind.Logout, "session-logged-out", "The logout exchange completed.");
        }
    }

    internal SessionStateTransitionResult BeginRecovery()
    {
        lock (this)
        {
            if (State != SessionState.Active)
            {
                return RejectTransition("begin-recovery");
            }
            return ChangeState(SessionState.Recovering, SessionEventKind.RecoveryStarted, "recovery-started", "Sequence recovery started.");
        }
    }

    internal SessionStateTransitionResult CompleteRecovery()
    {
        lock (this)
        {
            if (State != SessionState.Recovering)
            {
                return RejectTransition("complete-recovery");
            }
            return ChangeState(SessionState.Active, SessionEventKind.RecoveryCompleted, "recovery-completed", "Sequence recovery completed.");
        }
    }

    internal SessionStateTransitionResult DisconnectTransport()
    {
        lock (this)
        {
            if (State != SessionState.Connecting && State != SessionState.Connected && State != SessionState.LogonPending && State != SessionState.Active && State != SessionState.LogoutPending && State != SessionState.Reconnecting && State != SessionState.Recovering)
            {
                return RejectTransition("disconnect-transport");
            }
            return ChangeState(SessionState.Disconnected, SessionEventKind.Disconnected, "transport-disconnected", "The transport disconnected.");
        }
    }

    internal SessionStateTransitionResult ScheduleReconnect()
    {
        lock (this)
        {
            if (Role != SessionRole.Initiator)
            {
                return RejectTransitionWithCode("schedule-reconnect", "reconnect-not-supported-for-acceptor", "An acceptor cannot schedule an outbound reconnect.");
            }
            if (State != SessionState.Disconnected)
            {
                return RejectTransition("schedule-reconnect");
            }
            return ChangeState(SessionState.Reconnecting, SessionEventKind.None, "reconnect-scheduled", "The initiator is waiting to reconnect.");
        }
    }

    internal SessionStateTransitionResult FaultSession()
    {
        lock (this)
        {
            if (State == SessionState.Stopped || State == SessionState.Faulted)
            {
                return RejectTransition("fault-session");
            }
            return ChangeState(SessionState.Faulted, SessionEventKind.Error, "session-faulted", "The session entered the faulted state.");
        }
    }

    internal SessionStateTransitionResult StopSession()
    {
        lock (this)
        {
            if (State == SessionState.Stopped)
            {
                return UnchangedTransition("session-already-stopped", "The session is already stopped.");
            }
            return ChangeState(SessionState.Stopped, SessionEventKind.Stopped, "session-stopped", "The session stopped.");
        }
    }

    internal SessionStateTransitionResult ChangeState(SessionState nextState, SessionEventKind eventKind, string code, string text)
    {
        SessionStateTransitionResult result = new SessionStateTransitionResult();
        result.Accepted = true;
        result.Changed = State != nextState;
        result.PreviousState = State;
        result.State = nextState;
        result.EventKind = eventKind;
        result.Code = code;
        result.Text = text;
        State = nextState;
        return result;
    }

    internal SessionStateTransitionResult RejectTransition(string operation)
    {
        return RejectTransitionWithCode(operation, "invalid-session-state-transition", $"Operation '{operation}' is not valid while the session is in state '{State}'.");
    }

    internal SessionStateTransitionResult RejectTransitionWithCode(string operation, string code, string text)
    {
        SessionStateTransitionResult result = new SessionStateTransitionResult();
        result.Accepted = false;
        result.Changed = false;
        result.PreviousState = State;
        result.State = State;
        result.EventKind = SessionEventKind.None;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal SessionStateTransitionResult UnchangedTransition(string code, string text)
    {
        SessionStateTransitionResult result = new SessionStateTransitionResult();
        result.Accepted = true;
        result.Changed = false;
        result.PreviousState = State;
        result.State = State;
        result.EventKind = SessionEventKind.None;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
