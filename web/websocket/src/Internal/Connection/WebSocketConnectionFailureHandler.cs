namespace websocket;

internal class WebSocketConnectionFailureResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal bool Changed;
    internal bool CloseQueued;
    internal bool ClosePending;
    internal bool ErrorEventQueued;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketConnectionFailureHandler
{
    WebSocketSendScheduler SendScheduler;
    WebSocketEventQueue Events;
    WebSocketConnectionStateMachine StateMachine;
    WebSocketLocalCloseCoordinator LocalCloseCoordinator;
    WebSocketHeartbeatManager HeartbeatManager;
    bool FailureHandled;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();
    WebSocketCloseInfo FailureClose = new WebSocketCloseInfo();
    bool FailureCloseStarted;
    bool FailureClosePending;
    bool FailureCloseFailed;
    WebSocketEvent? PendingErrorEvent;

    internal WebSocketConnectionFailureHandler(
        WebSocketSendScheduler? sendScheduler,
        WebSocketEventQueue? events,
        WebSocketConnectionStateMachine? stateMachine,
        WebSocketLocalCloseCoordinator? localCloseCoordinator,
        WebSocketHeartbeatManager? heartbeatManager)
    {
        if (sendScheduler == null ||
            events == null ||
            stateMachine == null ||
            localCloseCoordinator == null ||
            heartbeatManager == null)
        {
            throw new WebSocketStateException();
        }

        SendScheduler = sendScheduler;
        Events = events;
        StateMachine = stateMachine;
        LocalCloseCoordinator = localCloseCoordinator;
        HeartbeatManager = heartbeatManager;
    }

    internal WebSocketConnectionFailureResult HandleConnectionFailure(
        WebSocketParseStatus status,
        WebSocketError? error)
    {
        if ((status != WebSocketParseStatus.ProtocolFailure &&
                status != WebSocketParseStatus.LimitRejected) ||
            error == null)
        {
            throw new WebSocketStateException();
        }

        if (FailureHandled)
        {
            WebSocketConnectionFailureResult repeated =
                SynchronizeFailureHandling();
            repeated.Changed = false;
            return repeated;
        }

        FailureHandled = true;
        FailureStatus = status;
        FailureError = NormalizeFailureError(error);
        PendingErrorEvent = CreateErrorEvent(FailureError);
        SendScheduler.StopDataFrames();
        HeartbeatManager.StopHeartbeat();

        bool shouldSendClose = ShouldSendFailureClose(FailureError.Kind);
        if (shouldSendClose)
        {
            int closeCode = FailureError.CloseCode;
            FailureClose = new WebSocketCloseInfo();
            FailureClose.HasCode = true;
            FailureClose.Code = closeCode;
            if (LocalCloseCoordinator.HasLocalCloseInfo())
            {
                BeginFailureClosingOrFail();
            }
            else
            {
                FailureCloseStarted = true;
                WebSocketLocalCloseResult started =
                    LocalCloseCoordinator.StartLocalClose(FailureClose);
                if (started.HasError)
                {
                    FailureCloseFailed = true;
                    StateMachine.FailConnection(FailureError);
                }
                else
                {
                    FailureClosePending = !started.Accepted;
                    BeginFailureClosingOrFail();
                }
            }
        }
        else
        {
            FailureCloseFailed = true;
            StateMachine.FailConnection(FailureError);
        }

        WebSocketConnectionFailureResult result =
            SynchronizeFailureHandling();
        result.Changed = true;
        return result;
    }

    internal WebSocketConnectionFailureResult SynchronizeFailureHandling()
    {
        if (!FailureHandled)
        {
            return new WebSocketConnectionFailureResult();
        }

        bool closeQueued = false;
        if (FailureClosePending && !FailureCloseFailed)
        {
            WebSocketLocalCloseResult retried =
                LocalCloseCoordinator.StartLocalClose(FailureClose);
            if (retried.HasError)
            {
                FailureClosePending = false;
                FailureCloseFailed = true;
                StateMachine.FailConnection(FailureError);
            }
            else if (retried.Accepted)
            {
                FailureClosePending = false;
                closeQueued = true;
            }
        }

        bool errorQueued = false;
        if (PendingErrorEvent != null &&
            Events.TryEnqueueEvent(PendingErrorEvent))
        {
            PendingErrorEvent = null;
            errorQueued = true;
        }

        WebSocketConnectionFailureResult result =
            new WebSocketConnectionFailureResult();
        result.Status = FailureStatus;
        result.CloseQueued = FailureCloseStarted &&
            (closeQueued ||
                (LocalCloseCoordinator.HasLocalCloseInfo() &&
                    !FailureClosePending && !FailureCloseFailed));
        result.ClosePending = FailureClosePending;
        result.ErrorEventQueued = errorQueued;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    internal bool HasHandledConnectionFailure()
    {
        return FailureHandled;
    }

    internal bool HasPendingFailureClose()
    {
        return FailureClosePending;
    }

    internal bool HasFailedFailureClose()
    {
        return FailureCloseFailed;
    }

    internal bool HasPendingErrorEvent()
    {
        return PendingErrorEvent != null;
    }

    internal WebSocketError GetConnectionFailureError()
    {
        return WebSocketFrameCodec.CopyError(FailureError);
    }

    internal void ResetConnectionFailureHandler()
    {
        FailureHandled = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
        FailureClose = new WebSocketCloseInfo();
        FailureCloseStarted = false;
        FailureClosePending = false;
        FailureCloseFailed = false;
        PendingErrorEvent = null;
    }

    void BeginFailureClosingOrFail()
    {
        StateMachine.BeginFailureClosing(FailureError);
        if (!StateMachine.HasFailure ||
            StateMachine.State != WebSocketConnectionState.Closing)
        {
            FailureClosePending = false;
            FailureCloseFailed = true;
            StateMachine.FailConnection(FailureError);
        }
    }

    static bool ShouldSendFailureClose(WebSocketErrorKind kind)
    {
        return kind == WebSocketErrorKind.Protocol ||
            kind == WebSocketErrorKind.Utf8 ||
            kind == WebSocketErrorKind.Compression ||
            kind == WebSocketErrorKind.Limit;
    }

    static WebSocketError NormalizeFailureError(WebSocketError source)
    {
        WebSocketError copy = WebSocketFrameCodec.CopyError(source);
        int closeCode = 0;
        WebSocketCloseCode knownCode = WebSocketCloseCode.NormalClosure;
        if (copy.Kind == WebSocketErrorKind.Protocol ||
            copy.Kind == WebSocketErrorKind.Compression)
        {
            closeCode = WebSocketCloseCodes.ProtocolError;
            knownCode = WebSocketCloseCode.ProtocolError;
        }
        else if (copy.Kind == WebSocketErrorKind.Utf8)
        {
            closeCode = WebSocketCloseCodes.InvalidFramePayloadData;
            knownCode = WebSocketCloseCode.InvalidFramePayloadData;
        }
        else if (copy.Kind == WebSocketErrorKind.Limit)
        {
            closeCode = WebSocketCloseCodes.MessageTooBig;
            knownCode = WebSocketCloseCode.MessageTooBig;
        }

        if (closeCode != 0)
        {
            copy.HasCloseCode = true;
            copy.CloseCode = closeCode;
            copy.HasKnownCloseCode = true;
            copy.KnownCloseCode = knownCode;
        }

        return copy;
    }

    static WebSocketEvent CreateErrorEvent(WebSocketError error)
    {
        WebSocketEvent connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.Error;
        connectionEvent.HasError = true;
        connectionEvent.Error = WebSocketFrameCodec.CopyError(error);
        connectionEvent.First = true;
        connectionEvent.Final = true;
        return connectionEvent;
    }
}
