namespace websocket;

internal class WebSocketConnectionTransitionResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal WebSocketConnectionState State;
    internal bool Changed;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketConnectionStateMachine
{
    internal WebSocketRole Role;
    internal WebSocketHandshakeTransport Transport;
    internal WebSocketConnectionState State = WebSocketConnectionState.Connecting;
    internal int CloseTimeoutMilliseconds;
    internal long ElapsedMilliseconds;
    internal bool HasClosingStartedMilliseconds;
    internal long ClosingStartedMilliseconds;
    internal bool LocalCloseStarted;
    internal bool LocalCloseSent;
    internal bool RemoteCloseReceived;
    internal bool LocalInitiatedClosing;
    internal bool ShouldEndTransport;
    internal bool TransportEnded;
    internal bool TransportReset;
    internal bool CleanClose;
    internal bool HasFailure;
    internal WebSocketError FailureError = new WebSocketError();

    internal WebSocketConnectionStateMachine(
        WebSocketRole role,
        WebSocketHandshakeTransport transport,
        int closeTimeoutMilliseconds)
    {
        if ((role != WebSocketRole.Client && role != WebSocketRole.Server) ||
            (transport != WebSocketHandshakeTransport.Http1Upgrade &&
                transport != WebSocketHandshakeTransport.Http2ExtendedConnect &&
                transport != WebSocketHandshakeTransport.Http3ExtendedConnect))
        {
            throw new WebSocketStateException();
        }

        if (closeTimeoutMilliseconds <= 0)
        {
            throw new WebSocketLimitException();
        }

        Role = role;
        Transport = transport;
        CloseTimeoutMilliseconds = closeTimeoutMilliseconds;
    }

    internal WebSocketConnectionTransitionResult OpenConnection()
    {
        if (State != WebSocketConnectionState.Connecting)
        {
            return CreateStateRejection("connection-open-state-invalid");
        }

        State = WebSocketConnectionState.Open;
        return CreateCurrentResult(true);
    }

    internal WebSocketConnectionTransitionResult StartLocalClose()
    {
        if (State == WebSocketConnectionState.Open)
        {
            BeginClosing();
            LocalCloseStarted = true;
            LocalInitiatedClosing = true;
            return CreateCurrentResult(true);
        }

        if (State == WebSocketConnectionState.Closing)
        {
            if (LocalCloseStarted)
            {
                return CreateCurrentResult(false);
            }

            LocalCloseStarted = true;
            UpdateTransportGuidance();
            return CreateCurrentResult(true);
        }

        if (State == WebSocketConnectionState.Closed ||
            State == WebSocketConnectionState.Failed)
        {
            return CreateCurrentResult(false);
        }

        return CreateStateRejection("connection-local-close-state-invalid");
    }

    internal WebSocketConnectionTransitionResult RecordLocalCloseSent()
    {
        if (State == WebSocketConnectionState.Closed ||
            State == WebSocketConnectionState.Failed)
        {
            return CreateCurrentResult(false);
        }

        if (State != WebSocketConnectionState.Closing ||
            !LocalCloseStarted)
        {
            return CreateStateRejection("connection-close-send-state-invalid");
        }

        if (LocalCloseSent)
        {
            return CreateCurrentResult(false);
        }

        LocalCloseSent = true;
        UpdateTransportGuidance();
        return CreateCurrentResult(true);
    }

    internal WebSocketConnectionTransitionResult RecordRemoteClose()
    {
        if (State == WebSocketConnectionState.Open)
        {
            BeginClosing();
            RemoteCloseReceived = true;
            UpdateTransportGuidance();
            return CreateCurrentResult(true);
        }

        if (State == WebSocketConnectionState.Closing)
        {
            if (RemoteCloseReceived)
            {
                return CreateCurrentResult(false);
            }

            RemoteCloseReceived = true;
            UpdateTransportGuidance();
            return CreateCurrentResult(true);
        }

        if (State == WebSocketConnectionState.Closed ||
            State == WebSocketConnectionState.Failed)
        {
            return CreateCurrentResult(false);
        }

        return CreateStateRejection("connection-remote-close-state-invalid");
    }

    internal WebSocketConnectionTransitionResult EndTransport()
    {
        if (State == WebSocketConnectionState.Closed)
        {
            WebSocketConnectionTransitionResult repeated =
                CreateCurrentResult(false);
            repeated.Status = WebSocketParseStatus.End;
            return repeated;
        }

        if (State == WebSocketConnectionState.Failed)
        {
            bool changed = !TransportEnded;
            if (changed)
            {
                TransportEnded = true;
                ShouldEndTransport = true;
            }

            return CreateCurrentResult(changed);
        }

        if (State == WebSocketConnectionState.Closing && HasFailure)
        {
            State = WebSocketConnectionState.Failed;
            TransportEnded = true;
            CleanClose = false;
            ShouldEndTransport = true;
            return CreateCurrentResult(true);
        }

        if (State == WebSocketConnectionState.Closing &&
            LocalCloseSent &&
            RemoteCloseReceived)
        {
            State = WebSocketConnectionState.Closed;
            TransportEnded = true;
            CleanClose = true;
            ShouldEndTransport = true;
            WebSocketConnectionTransitionResult ended =
                CreateCurrentResult(true);
            ended.Status = WebSocketParseStatus.End;
            return ended;
        }

        TransportEnded = true;
        if (Transport == WebSocketHandshakeTransport.Http2ExtendedConnect)
        {
            WebSocketConnectionTransitionResult result = FailTerminal(
                WebSocketErrorKind.Transport,
                WebSocketCloseCodes.AbnormalClosure,
                "connection-http2-end-stream-before-close",
                "The HTTP/2 stream ended before the closing handshake completed.");
            ShouldEndTransport = true;
            return result;
        }

        if (Transport == WebSocketHandshakeTransport.Http3ExtendedConnect)
        {
            WebSocketConnectionTransitionResult result = FailTerminal(
                WebSocketErrorKind.Transport,
                WebSocketCloseCodes.AbnormalClosure,
                "connection-http3-fin-before-close",
                "The HTTP/3 stream ended before the closing handshake completed.");
            ShouldEndTransport = true;
            return result;
        }

        WebSocketConnectionTransitionResult tcpResult = FailTerminal(
            WebSocketErrorKind.Transport,
            WebSocketCloseCodes.AbnormalClosure,
            "connection-transport-ended-before-close",
            "The transport ended before the closing handshake completed.");
        ShouldEndTransport = true;
        return tcpResult;
    }

    internal WebSocketConnectionTransitionResult ResetTransport()
    {
        if (State == WebSocketConnectionState.Closed)
        {
            return CreateCurrentResult(false);
        }

        if (State == WebSocketConnectionState.Failed)
        {
            bool changed = !TransportReset || !TransportEnded;
            TransportReset = true;
            TransportEnded = true;
            ShouldEndTransport = false;
            return CreateCurrentResult(changed);
        }

        TransportReset = true;
        TransportEnded = true;
        if (Transport == WebSocketHandshakeTransport.Http2ExtendedConnect)
        {
            return FailTerminal(
                WebSocketErrorKind.Transport,
                WebSocketCloseCodes.AbnormalClosure,
                "connection-http2-stream-reset",
                "The HTTP/2 WebSocket stream was reset.");
        }

        if (Transport == WebSocketHandshakeTransport.Http3ExtendedConnect)
        {
            return FailTerminal(
                WebSocketErrorKind.Transport,
                WebSocketCloseCodes.AbnormalClosure,
                "connection-http3-stream-reset",
                "The HTTP/3 WebSocket stream was reset.");
        }

        return FailTerminal(
            WebSocketErrorKind.Transport,
            WebSocketCloseCodes.AbnormalClosure,
            "connection-transport-reset",
            "The WebSocket transport was reset.");
    }

    internal WebSocketConnectionTransitionResult FailConnection(
        WebSocketError error)
    {
        if (State == WebSocketConnectionState.Closed ||
            State == WebSocketConnectionState.Failed)
        {
            return CreateCurrentResult(false);
        }

        if (error == null)
        {
            return CreateStateRejection("connection-failure-error-invalid");
        }

        State = WebSocketConnectionState.Failed;
        HasFailure = true;
        FailureError = CopyError(error);
        ShouldEndTransport = true;
        return CreateCurrentResult(true);
    }

    internal WebSocketConnectionTransitionResult BeginFailureClosing(
        WebSocketError error)
    {
        if (State == WebSocketConnectionState.Closed ||
            State == WebSocketConnectionState.Failed)
        {
            return CreateCurrentResult(false);
        }

        if (error == null)
        {
            return CreateStateRejection("connection-failure-error-invalid");
        }

        if (HasFailure)
        {
            return CreateCurrentResult(false);
        }

        if (State == WebSocketConnectionState.Open)
        {
            BeginClosing();
        }
        else if (State != WebSocketConnectionState.Closing)
        {
            return CreateStateRejection(
                "connection-failure-close-state-invalid");
        }

        HasFailure = true;
        FailureError = CopyError(error);
        CleanClose = false;
        ShouldEndTransport = false;
        return CreateCurrentResult(true);
    }

    internal WebSocketConnectionTransitionResult AdvanceElapsedMilliseconds(
        long elapsedMilliseconds)
    {
        if (elapsedMilliseconds < 0 ||
            elapsedMilliseconds < ElapsedMilliseconds)
        {
            return CreateStateRejection("connection-elapsed-time-invalid");
        }

        bool changed = elapsedMilliseconds != ElapsedMilliseconds;
        ElapsedMilliseconds = elapsedMilliseconds;
        if (State == WebSocketConnectionState.Closing &&
            HasClosingStartedMilliseconds &&
            elapsedMilliseconds - ClosingStartedMilliseconds >=
                CloseTimeoutMilliseconds)
        {
            return FailTerminal(
                WebSocketErrorKind.Transport,
                WebSocketCloseCodes.AbnormalClosure,
                "connection-close-timeout",
                "The closing handshake exceeded its configured timeout.");
        }

        return CreateCurrentResult(changed);
    }

    internal WebSocketConnectionTransitionResult AbortConnection()
    {
        if (State == WebSocketConnectionState.Closed)
        {
            return CreateCurrentResult(false);
        }

        if (State == WebSocketConnectionState.Failed)
        {
            bool changed = !TransportEnded;
            TransportEnded = true;
            ShouldEndTransport = false;
            return CreateCurrentResult(changed);
        }

        TransportEnded = true;
        return FailTerminal(
            WebSocketErrorKind.Transport,
            WebSocketCloseCodes.AbnormalClosure,
            "connection-aborted",
            "The WebSocket connection was aborted locally.");
    }

    internal WebSocketConnectionInfo GetConnectionInformation()
    {
        WebSocketConnectionInfo information =
            new WebSocketConnectionInfo();
        information.Role = Role;
        information.State = State;
        information.Transport = Transport;
        information.ElapsedMilliseconds = ElapsedMilliseconds;
        information.CleanClose = CleanClose;
        information.TransportEnded = TransportEnded;
        information.TransportReset = TransportReset;
        information.ShouldEndTransport = ShouldEndTransport;
        if (HasFailure)
        {
            information.HasError = true;
            information.Error = CopyError(FailureError);
        }

        return information;
    }

    internal void ResetConnectionStateMachine()
    {
        State = WebSocketConnectionState.Connecting;
        ElapsedMilliseconds = 0;
        HasClosingStartedMilliseconds = false;
        ClosingStartedMilliseconds = 0;
        LocalCloseStarted = false;
        LocalCloseSent = false;
        RemoteCloseReceived = false;
        LocalInitiatedClosing = false;
        ShouldEndTransport = false;
        TransportEnded = false;
        TransportReset = false;
        CleanClose = false;
        HasFailure = false;
        FailureError = new WebSocketError();
    }

    void BeginClosing()
    {
        State = WebSocketConnectionState.Closing;
        HasClosingStartedMilliseconds = true;
        ClosingStartedMilliseconds = ElapsedMilliseconds;
    }

    void UpdateTransportGuidance()
    {
        ShouldEndTransport = Role == WebSocketRole.Server &&
            LocalCloseSent &&
            RemoteCloseReceived;
    }

    WebSocketConnectionTransitionResult FailTerminal(
        WebSocketErrorKind kind,
        int closeCode,
        string identifier,
        string diagnostic)
    {
        State = WebSocketConnectionState.Failed;
        if (!HasFailure)
        {
            HasFailure = true;
            FailureError = CreateError(
                kind,
                closeCode,
                identifier,
                diagnostic);
        }

        CleanClose = false;
        ShouldEndTransport = !TransportEnded;
        return CreateCurrentResult(true);
    }

    WebSocketConnectionTransitionResult CreateStateRejection(
        string identifier)
    {
        WebSocketConnectionTransitionResult result =
            new WebSocketConnectionTransitionResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.State = State;
        result.HasError = true;
        result.Error = CreateError(
            WebSocketErrorKind.State,
            WebSocketCloseCodes.InternalError,
            identifier,
            "The requested connection transition is invalid for the current state.");
        return result;
    }

    WebSocketConnectionTransitionResult CreateCurrentResult(bool changed)
    {
        WebSocketConnectionTransitionResult result =
            new WebSocketConnectionTransitionResult();
        result.State = State;
        result.Changed = changed;
        if (HasFailure)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.HasError = true;
            result.Error = CopyError(FailureError);
        }

        return result;
    }

    static WebSocketError CreateError(
        WebSocketErrorKind kind,
        int closeCode,
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = kind;
        error.HasCloseCode = true;
        error.CloseCode = closeCode;
        CloseCodeClassification classification =
            CloseCodeClassifier.Classify(closeCode);
        error.HasKnownCloseCode = classification.HasKnownCode;
        error.KnownCloseCode = classification.KnownCode;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return error;
    }

    static WebSocketError CopyError(WebSocketError source)
    {
        WebSocketError copy = new WebSocketError();
        copy.Kind = source.Kind;
        copy.HasCloseCode = source.HasCloseCode;
        copy.CloseCode = source.CloseCode;
        copy.HasKnownCloseCode = source.HasKnownCloseCode;
        copy.KnownCloseCode = source.KnownCloseCode;
        copy.HasHttpStatusCode = source.HasHttpStatusCode;
        copy.HttpStatusCode = source.HttpStatusCode;
        copy.Identifier = source.Identifier;
        copy.Diagnostic = source.Diagnostic;
        return copy;
    }
}
