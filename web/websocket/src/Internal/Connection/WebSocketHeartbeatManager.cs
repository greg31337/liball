using System;

namespace websocket;

internal class WebSocketHeartbeatAdvanceResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Changed;
    internal bool PingQueued;
    internal bool TimedOut;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketHeartbeatManager
{
    bool Enabled;
    int IdleIntervalMilliseconds;
    int PongTimeoutMilliseconds;
    int TokenLength;
    WebSocketPingPongHandler PingPongHandler;
    long ElapsedMilliseconds;
    long LastActivityElapsedMilliseconds;
    bool PingOutstanding;
    byte[] OutstandingToken = [];
    long PingSentElapsedMilliseconds;
    bool HasLastRoundTripMilliseconds;
    long LastRoundTripMilliseconds;
    bool Stopped;
    bool Failed;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketHeartbeatManager(
        WebSocketConnectionOptions options,
        WebSocketPingPongHandler? pingPongHandler)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (pingPongHandler == null)
        {
            throw new WebSocketStateException();
        }

        Enabled = options.Heartbeat.Enabled;
        IdleIntervalMilliseconds =
            options.Heartbeat.IdleIntervalMilliseconds;
        PongTimeoutMilliseconds =
            options.Heartbeat.PongTimeoutMilliseconds;
        TokenLength = options.Heartbeat.TokenLength;
        PingPongHandler = pingPongHandler;
    }

    internal WebSocketHeartbeatAdvanceResult AdvanceHeartbeatTime(
        long elapsedMilliseconds)
    {
        if (Failed)
        {
            return CreateLatchedFailure(false);
        }

        if (elapsedMilliseconds < 0 ||
            elapsedMilliseconds < ElapsedMilliseconds)
        {
            return CreateStateFailure(
                "heartbeat-elapsed-time-invalid",
                "Heartbeat elapsed time must be nonnegative and monotonic.");
        }

        bool changed = elapsedMilliseconds != ElapsedMilliseconds;
        WebSocketControlSendResult updated =
            PingPongHandler.SetPingElapsedMilliseconds(
                elapsedMilliseconds);
        if (updated.HasError)
        {
            return CreateStateFailure(
                updated.Error.Identifier,
                updated.Error.Diagnostic);
        }

        ElapsedMilliseconds = elapsedMilliseconds;
        SynchronizeHeartbeatAcknowledgement();
        if (!Enabled || Stopped)
        {
            return CreateAdvanceResult(changed, false);
        }

        if (PingOutstanding)
        {
            if (elapsedMilliseconds - PingSentElapsedMilliseconds >=
                PongTimeoutMilliseconds)
            {
                return FailHeartbeatTimeout(changed);
            }

            return CreateAdvanceResult(changed, false);
        }

        if (elapsedMilliseconds - LastActivityElapsedMilliseconds <
            IdleIntervalMilliseconds)
        {
            return CreateAdvanceResult(changed, false);
        }

        byte[] token = new byte[TokenLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(token);
        WebSocketControlSendResult sent =
            PingPongHandler.SendTrackedPing(
                token,
                0,
                token.Length,
                elapsedMilliseconds);
        if (!sent.Accepted)
        {
            if (sent.Status == WebSocketParseStatus.NeedOutputCapacity &&
                !sent.HasError)
            {
                WebSocketHeartbeatAdvanceResult blocked =
                    CreateAdvanceResult(changed, false);
                blocked.Status = WebSocketParseStatus.NeedOutputCapacity;
                return blocked;
            }

            return CreateSendFailure(changed, sent);
        }

        OutstandingToken = token;
        PingOutstanding = true;
        PingSentElapsedMilliseconds = elapsedMilliseconds;
        return CreateAdvanceResult(changed, true);
    }

    internal void RecordHeartbeatActivity()
    {
        if (!Failed && !Stopped)
        {
            LastActivityElapsedMilliseconds = ElapsedMilliseconds;
        }
    }

    internal bool SynchronizeHeartbeatAcknowledgement()
    {
        if (!PingOutstanding ||
            PingPongHandler.ContainsTrackedPingToken(
                OutstandingToken,
                0,
                OutstandingToken.Length))
        {
            return false;
        }

        PingOutstanding = false;
        LastRoundTripMilliseconds =
            ElapsedMilliseconds - PingSentElapsedMilliseconds;
        HasLastRoundTripMilliseconds = true;
        LastActivityElapsedMilliseconds = ElapsedMilliseconds;
        Array.Clear(OutstandingToken);
        OutstandingToken = [];
        PingSentElapsedMilliseconds = 0;
        return true;
    }

    internal long GetHeartbeatElapsedMilliseconds()
    {
        return ElapsedMilliseconds;
    }

    internal long GetLastHeartbeatActivityMilliseconds()
    {
        return LastActivityElapsedMilliseconds;
    }

    internal bool HasOutstandingHeartbeatPing()
    {
        return PingOutstanding;
    }

    internal bool HasHeartbeatPingSentMilliseconds()
    {
        return PingOutstanding;
    }

    internal long GetHeartbeatPingSentMilliseconds()
    {
        return PingSentElapsedMilliseconds;
    }

    internal bool HasHeartbeatRoundTripMilliseconds()
    {
        return HasLastRoundTripMilliseconds;
    }

    internal long GetHeartbeatRoundTripMilliseconds()
    {
        return LastRoundTripMilliseconds;
    }

    internal bool HasHeartbeatFailed()
    {
        return Failed;
    }

    internal void StopHeartbeat()
    {
        PingPongHandler.ClearTrackedPingState();
        RemoveOutstandingHeartbeatPing();
        Stopped = true;
    }

    internal void ResetHeartbeatState()
    {
        PingPongHandler.ResetTrackedPingState();
        RemoveOutstandingHeartbeatPing();
        ElapsedMilliseconds = 0;
        LastActivityElapsedMilliseconds = 0;
        PingSentElapsedMilliseconds = 0;
        HasLastRoundTripMilliseconds = false;
        LastRoundTripMilliseconds = 0;
        Stopped = false;
        Failed = false;
        FailureError = new WebSocketError();
    }

    void RemoveOutstandingHeartbeatPing()
    {
        if (PingOutstanding)
        {
            PingPongHandler.RemoveTrackedPingToken(
                OutstandingToken,
                0,
                OutstandingToken.Length);
        }

        PingOutstanding = false;
        Array.Clear(OutstandingToken);
        OutstandingToken = [];
        PingSentElapsedMilliseconds = 0;
    }

    WebSocketHeartbeatAdvanceResult FailHeartbeatTimeout(bool changed)
    {
        Failed = true;
        FailureError = new WebSocketError();
        FailureError.Kind = WebSocketErrorKind.Transport;
        FailureError.HasCloseCode = true;
        FailureError.CloseCode = WebSocketCloseCodes.AbnormalClosure;
        FailureError.HasKnownCloseCode = true;
        FailureError.KnownCloseCode = WebSocketCloseCode.AbnormalClosure;
        FailureError.Identifier = "heartbeat-pong-timeout";
        FailureError.Diagnostic =
            "The peer did not acknowledge the heartbeat Ping before its deadline.";
        PingPongHandler.ClearTrackedPingState();
        RemoveOutstandingHeartbeatPing();
        WebSocketHeartbeatAdvanceResult result =
            CreateLatchedFailure(changed);
        result.TimedOut = true;
        return result;
    }

    WebSocketHeartbeatAdvanceResult CreateLatchedFailure(bool changed)
    {
        WebSocketHeartbeatAdvanceResult result =
            new WebSocketHeartbeatAdvanceResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.Changed = changed;
        result.HasError = true;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    static WebSocketHeartbeatAdvanceResult CreateAdvanceResult(
        bool changed,
        bool pingQueued)
    {
        WebSocketHeartbeatAdvanceResult result =
            new WebSocketHeartbeatAdvanceResult();
        result.Changed = changed;
        result.PingQueued = pingQueued;
        return result;
    }

    static WebSocketHeartbeatAdvanceResult CreateStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketHeartbeatAdvanceResult result =
            new WebSocketHeartbeatAdvanceResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.State;
        result.Error.Identifier = identifier;
        result.Error.Diagnostic = diagnostic;
        return result;
    }

    static WebSocketHeartbeatAdvanceResult CreateSendFailure(
        bool changed,
        WebSocketControlSendResult sent)
    {
        WebSocketHeartbeatAdvanceResult result =
            new WebSocketHeartbeatAdvanceResult();
        result.Status = sent.Status;
        result.Changed = changed;
        result.HasError = sent.HasError;
        result.Error = WebSocketFrameCodec.CopyError(sent.Error);
        return result;
    }
}
