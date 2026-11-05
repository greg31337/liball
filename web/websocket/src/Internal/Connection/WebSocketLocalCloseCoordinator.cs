using System;

namespace websocket;

internal class WebSocketLocalCloseResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal bool Changed;
    internal bool CloseQueued;
    internal bool CloseSent;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketLocalCloseCoordinator
{
    WebSocketRole Role;
    int MaximumFramePayloadLength;
    IWebSocketMaskKeySource? MaskKeySource;
    IWebSocketCloseCodec CloseCodec;
    IWebSocketFrameCodec FrameCodec;
    WebSocketSendScheduler SendScheduler;
    WebSocketConnectionStateMachine StateMachine;
    WebSocketHeartbeatManager? HeartbeatManager;
    bool LocalCloseStarted;
    bool LocalCloseQueued;
    bool LocalCloseSent;
    WebSocketCloseInfo LocalClose = new WebSocketCloseInfo();
    byte[] EncodedCloseFrame = [];

    internal WebSocketLocalCloseCoordinator(
        WebSocketConnectionOptions options,
        WebSocketSendScheduler? sendScheduler,
        WebSocketConnectionStateMachine? stateMachine,
        WebSocketHeartbeatManager? heartbeatManager)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (sendScheduler == null ||
            stateMachine == null ||
            stateMachine.Role != options.Role)
        {
            throw new WebSocketStateException();
        }

        Role = options.Role;
        MaximumFramePayloadLength = options.MaximumFramePayloadLength;
        MaskKeySource = options.MaskKeySource;
        if (Role == WebSocketRole.Client && MaskKeySource == null)
        {
            MaskKeySource = new CryptographicWebSocketMaskKeySource();
        }

        CloseCodec = new WebSocketCloseCodec();
        FrameCodec = new WebSocketFrameCodec(
            Role,
            options.MaximumFramePayloadLength,
            options.NegotiatedExtensions);
        SendScheduler = sendScheduler;
        StateMachine = stateMachine;
        HeartbeatManager = heartbeatManager;
    }

    internal WebSocketLocalCloseResult StartLocalClose(
        WebSocketCloseInfo? close)
    {
        WebSocketLocalCloseResult validation = ValidateLocalClose(close);
        if (validation.HasError)
        {
            return validation;
        }

        if (close == null)
        {
            return CreateStateFailure(
                "local-close-invalid",
                "Local close information is required.");
        }

        WebSocketCloseInfo requested = CopyRequestedClose(close);
        if (LocalCloseStarted)
        {
            if (!CloseRequestsEqual(LocalClose, requested))
            {
                return CreateStateFailure(
                    "local-close-details-conflict",
                    "Local closing already started with different details.");
            }

            if (LocalCloseQueued || LocalCloseSent)
            {
                WebSocketLocalCloseResult duplicate =
                    new WebSocketLocalCloseResult();
                duplicate.CloseSent = LocalCloseSent;
                return duplicate;
            }

            return TryQueuePreparedClose(false);
        }

        if (StateMachine.State != WebSocketConnectionState.Open &&
            (StateMachine.State != WebSocketConnectionState.Closing ||
                StateMachine.LocalCloseStarted))
        {
            return CreateStateFailure(
                "local-close-state-invalid",
                "The connection state does not permit a local Close frame.");
        }

        int payloadLength = CloseCodec.MeasureClosePayload(requested);
        if (payloadLength > MaximumFramePayloadLength)
        {
            return CreateLimitFailure(
                "local-close-frame-limit-exceeded",
                "The local Close payload exceeds the configured frame limit.");
        }

        int encodedCount = payloadLength + 2;
        if (Role == WebSocketRole.Client)
        {
            encodedCount = encodedCount + 4;
        }

        if (encodedCount > SendScheduler.GetPendingByteCapacity())
        {
            return CreateLimitFailure(
                "local-close-output-limit-exceeded",
                "The encoded local Close frame exceeds the output buffer limit.");
        }

        if (encodedCount >
            SendScheduler.GetPendingByteAvailableCapacity())
        {
            WebSocketLocalCloseResult blocked =
                new WebSocketLocalCloseResult();
            blocked.Status = WebSocketParseStatus.NeedOutputCapacity;
            blocked.Accepted = false;
            return blocked;
        }

        EncodedCloseFrame = EncodeCloseFrame(requested, payloadLength);

        WebSocketConnectionTransitionResult transition =
            StateMachine.StartLocalClose();
        if (transition.HasError && !StateMachine.LocalCloseStarted)
        {
            EncodedCloseFrame = [];
            return CreateTransitionFailure(transition);
        }

        LocalCloseStarted = true;
        LocalClose = requested;
        LocalClose.InitiatedLocally =
            StateMachine.LocalInitiatedClosing;
        SendScheduler.StopDataFrames();
        if (HeartbeatManager != null)
        {
            HeartbeatManager.StopHeartbeat();
        }

        return TryQueuePreparedClose(true);
    }

    internal WebSocketLocalCloseResult SynchronizeLocalCloseSent()
    {
        WebSocketLocalCloseResult result =
            new WebSocketLocalCloseResult();
        if (!LocalCloseStarted || LocalCloseSent ||
            !SendScheduler.HasCompletedCloseFrame())
        {
            result.CloseSent = LocalCloseSent;
            return result;
        }

        WebSocketConnectionTransitionResult transition =
            StateMachine.RecordLocalCloseSent();
        if (transition.HasError && !StateMachine.LocalCloseSent)
        {
            return CreateTransitionFailure(transition);
        }

        LocalCloseSent = true;
        LocalCloseQueued = false;
        LocalClose.Sent = true;
        EncodedCloseFrame = [];
        result.Changed = true;
        result.CloseSent = true;
        return result;
    }

    internal bool CanSendApplicationData()
    {
        return !LocalCloseStarted &&
            !SendScheduler.GetDataSendsStopped();
    }

    internal bool HasLocalCloseInfo()
    {
        return LocalCloseStarted;
    }

    internal WebSocketCloseInfo GetLocalCloseInfo()
    {
        return CopyCloseInfo(LocalClose);
    }

    internal void ResetLocalCloseCoordinator()
    {
        LocalCloseStarted = false;
        LocalCloseQueued = false;
        LocalCloseSent = false;
        LocalClose = new WebSocketCloseInfo();
        Array.Clear(EncodedCloseFrame);
        EncodedCloseFrame = [];
    }

    WebSocketLocalCloseResult TryQueuePreparedClose(bool changed)
    {
        if (!SendScheduler.TryQueueCloseFrame(EncodedCloseFrame))
        {
            WebSocketLocalCloseResult blocked =
                new WebSocketLocalCloseResult();
            blocked.Status = WebSocketParseStatus.NeedOutputCapacity;
            blocked.Accepted = false;
            blocked.Changed = changed;
            return blocked;
        }

        LocalCloseQueued = true;
        WebSocketLocalCloseResult queued =
            new WebSocketLocalCloseResult();
        queued.Changed = changed;
        queued.CloseQueued = true;
        return queued;
    }

    WebSocketLocalCloseResult ValidateLocalClose(
        WebSocketCloseInfo? close)
    {
        if (close == null || close.Reason == null)
        {
            return CreateStateFailure(
                "local-close-invalid",
                "Local close information is incomplete.");
        }

        if (!close.HasCode)
        {
            if (close.HasReason)
            {
                return CreateStateFailure(
                    "local-close-reason-without-code",
                    "A local Close reason requires a status code.");
            }

            return new WebSocketLocalCloseResult();
        }

        CloseCodeClassification classification =
            CloseCodeClassifier.Classify(close.Code);
        if (!classification.Valid)
        {
            return CreateStateFailure(
                "local-close-code-invalid",
                "The local Close code is not valid on the wire.");
        }

        if (close.HasReason)
        {
            CloseReasonValidationResult reason =
                ClosePayloadValidator.ValidateReason(close.Reason);
            if (reason.TooLong)
            {
                return CreateLimitFailure(
                    "local-close-reason-limit-exceeded",
                    "The local Close reason exceeds 123 UTF-8 bytes.");
            }

            if (!reason.Valid)
            {
                return CreateStateFailure(
                    "local-close-reason-invalid",
                    "The local Close reason contains invalid UTF-16.");
            }
        }

        return new WebSocketLocalCloseResult();
    }

    byte[] EncodeCloseFrame(
        WebSocketCloseInfo close,
        int payloadLength)
    {
        byte[] payload = new byte[payloadLength];
        int payloadWritten = CloseCodec.WriteClosePayload(close, payload);
        if (payloadWritten != payloadLength)
        {
            throw new WebSocketStateException();
        }

        WebSocketFrameHeader header = new WebSocketFrameHeader();
        header.Final = true;
        header.Opcode = WebSocketOpcode.Close;
        header.Masked = Role == WebSocketRole.Client;
        header.PayloadLength = payloadLength;
        WebSocketFrameWriteRequest request =
            new WebSocketFrameWriteRequest();
        request.Header = header;
        request.Payload = payload;
        request.PayloadCount = payloadLength;
        request.HasMaskKeySource = header.Masked;
        if (header.Masked)
        {
            request.MaskKeySource = MaskKeySource;
        }

        int encodedLength = FrameCodec.MeasureFrame(request);
        byte[] encoded = new byte[encodedLength];
        int frameWritten = FrameCodec.WriteFrame(request, encoded);
        if (frameWritten != encodedLength || request.PayloadCount != 0)
        {
            throw new WebSocketStateException();
        }

        return encoded;
    }

    static WebSocketCloseInfo CopyRequestedClose(
        WebSocketCloseInfo source)
    {
        WebSocketCloseInfo copy = new WebSocketCloseInfo();
        copy.HasCode = source.HasCode;
        copy.Code = source.Code;
        if (source.HasCode)
        {
            CloseCodeClassification classification =
                CloseCodeClassifier.Classify(source.Code);
            copy.HasKnownCode = classification.HasKnownCode;
            copy.KnownCode = classification.KnownCode;
        }

        copy.HasReason = source.HasReason;
        if (source.HasReason)
        {
            copy.Reason = source.Reason;
        }

        return copy;
    }

    static bool CloseRequestsEqual(
        WebSocketCloseInfo first,
        WebSocketCloseInfo second)
    {
        if (first.HasCode != second.HasCode ||
            first.HasReason != second.HasReason)
        {
            return false;
        }

        if (first.HasCode && first.Code != second.Code)
        {
            return false;
        }

        return !first.HasReason || first.Reason == second.Reason;
    }

    static WebSocketCloseInfo CopyCloseInfo(WebSocketCloseInfo source)
    {
        WebSocketCloseInfo copy = new WebSocketCloseInfo();
        copy.HasCode = source.HasCode;
        copy.Code = source.Code;
        copy.HasKnownCode = source.HasKnownCode;
        copy.KnownCode = source.KnownCode;
        copy.HasReason = source.HasReason;
        copy.Reason = source.Reason;
        copy.InitiatedLocally = source.InitiatedLocally;
        copy.Sent = source.Sent;
        copy.Received = source.Received;
        copy.Clean = source.Clean;
        return copy;
    }

    static WebSocketLocalCloseResult CreateTransitionFailure(
        WebSocketConnectionTransitionResult transition)
    {
        WebSocketLocalCloseResult result =
            new WebSocketLocalCloseResult();
        result.Status = transition.Status;
        result.Accepted = false;
        result.HasError = transition.HasError;
        result.Error = WebSocketFrameCodec.CopyError(transition.Error);
        return result;
    }

    static WebSocketLocalCloseResult CreateStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailure(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    static WebSocketLocalCloseResult CreateLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateFailure(
            WebSocketParseStatus.LimitRejected,
            error);
    }

    static WebSocketLocalCloseResult CreateFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        WebSocketLocalCloseResult result =
            new WebSocketLocalCloseResult();
        result.Status = status;
        result.Accepted = false;
        result.HasError = true;
        result.Error = error;
        return result;
    }
}
