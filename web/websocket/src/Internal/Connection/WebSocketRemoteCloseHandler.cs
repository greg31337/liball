using System;

namespace websocket;

internal class WebSocketRemoteCloseHandler
{
    bool AutomaticCloseResponse;
    int MaximumOwnedPayloadBytes;
    IWebSocketCloseCodec CloseCodec;
    WebSocketSendScheduler SendScheduler;
    WebSocketEventQueue Events;
    WebSocketConnectionStateMachine StateMachine;
    WebSocketLocalCloseCoordinator LocalCloseCoordinator;
    WebSocketHeartbeatManager HeartbeatManager;
    WebSocketReassemblyBuffer PayloadBuffer;
    bool FrameActive;
    WebSocketFrameHeader FrameHeader = new WebSocketFrameHeader();
    long FramePayloadCount;
    WebSocketInboundFrameChunk? PendingAcceptedChunk;
    WebSocketEvent? PendingEvent;
    bool PendingEchoRequired;
    WebSocketParseStatus PendingStatus =
        WebSocketParseStatus.NeedEventCapacity;
    bool RemoteCloseRecorded;
    WebSocketCloseInfo RemoteClose = new WebSocketCloseInfo();
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketRemoteCloseHandler(
        WebSocketConnectionOptions options,
        WebSocketSendScheduler? sendScheduler,
        WebSocketEventQueue? events,
        WebSocketConnectionStateMachine? stateMachine,
        WebSocketLocalCloseCoordinator? localCloseCoordinator,
        WebSocketHeartbeatManager? heartbeatManager)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (sendScheduler == null ||
            events == null ||
            stateMachine == null ||
            localCloseCoordinator == null ||
            heartbeatManager == null ||
            stateMachine.Role != options.Role)
        {
            throw new WebSocketStateException();
        }

        AutomaticCloseResponse = options.AutomaticCloseResponse;
        MaximumOwnedPayloadBytes = options.MaximumBufferedInputBytes;
        CloseCodec = new WebSocketCloseCodec();
        SendScheduler = sendScheduler;
        Events = events;
        StateMachine = stateMachine;
        LocalCloseCoordinator = localCloseCoordinator;
        HeartbeatManager = heartbeatManager;
        PayloadBuffer = new WebSocketReassemblyBuffer(
            WebSocketProtocolConstants.ControlPayloadLengthMaximum);
    }

    internal WebSocketInboundFrameCapacity GetInboundCloseCapacity(
        WebSocketFrameHeader header,
        bool firstChunk,
        int availableOwnedPayloadCount)
    {
        if (Failed)
        {
            return CreateFailureCapacity();
        }

        if (!ValidateCapacityRequest(
            header,
            firstChunk,
            availableOwnedPayloadCount))
        {
            return CreateFailureCapacity();
        }

        WebSocketInboundFrameCapacity capacity =
            new WebSocketInboundFrameCapacity();
        if (PendingAcceptedChunk != null)
        {
            capacity.Status = PendingStatus;
            return capacity;
        }

        if (!RemoteCloseRecorded &&
            Events.GetEventAvailableCapacity() == 0)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        if (firstChunk &&
            header.PayloadLength > MaximumOwnedPayloadBytes)
        {
            SetLimitFailure(
                "close-receive-buffer-limit-exceeded",
                "The Close payload exceeds the configured input buffer limit.");
            return CreateFailureCapacity();
        }

        if (availableOwnedPayloadCount <= 0 &&
            header.PayloadLength != FramePayloadCount)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        long remaining = header.PayloadLength - FramePayloadCount;
        capacity.MaximumPayloadCount = (int)remaining;
        if (capacity.MaximumPayloadCount > availableOwnedPayloadCount)
        {
            capacity.MaximumPayloadCount = availableOwnedPayloadCount;
        }

        return capacity;
    }

    internal WebSocketInboundFrameConsumeResult ConsumeInboundCloseChunk(
        WebSocketInboundFrameChunk chunk)
    {
        if (Failed)
        {
            return CreateFailureConsumeResult();
        }

        if (PendingAcceptedChunk != null)
        {
            if (!object.ReferenceEquals(PendingAcceptedChunk, chunk))
            {
                SetStateFailure(
                    "close-pending-chunk-invalid",
                    "A different Close chunk was supplied while delivery was pending.");
                return CreateFailureConsumeResult();
            }

            return CompletePendingRemoteClose();
        }

        if (!ValidateChunk(chunk))
        {
            SetStateFailure(
                "close-frame-chunk-invalid",
                "The inbound Close frame chunk is inconsistent.");
            return CreateFailureConsumeResult();
        }

        if (chunk.FirstChunk)
        {
            FrameActive = true;
            FrameHeader = CopyHeader(chunk.Header);
            FramePayloadCount = 0;
            PayloadBuffer.ClearMessageBytes();
        }

        long remaining = FrameHeader.PayloadLength - FramePayloadCount;
        if (chunk.PayloadCount > remaining ||
            (chunk.EndOfFrame && chunk.PayloadCount != remaining) ||
            (!chunk.EndOfFrame && chunk.PayloadCount == remaining))
        {
            SetStateFailure(
                "close-frame-payload-length-invalid",
                "The Close chunks do not match the declared payload length.");
            return CreateFailureConsumeResult();
        }

        if (!PayloadBuffer.AppendMessageBytes(
            chunk.Payload,
            chunk.PayloadOffset,
            chunk.PayloadCount))
        {
            SetProtocolFailure(
                "close-payload-too-large",
                "The Close payload exceeds 125 bytes.");
            return CreateFailureConsumeResult();
        }

        if (PayloadBuffer.GetMessageByteCount() >
            MaximumOwnedPayloadBytes)
        {
            SetLimitFailure(
                "close-receive-buffer-limit-exceeded",
                "The Close payload exceeds the configured input buffer limit.");
            return CreateFailureConsumeResult();
        }

        FramePayloadCount = FramePayloadCount + chunk.PayloadCount;
        if (!chunk.EndOfFrame)
        {
            return new WebSocketInboundFrameConsumeResult();
        }

        byte[] payload = PayloadBuffer.TakeMessageBytes();
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        WebSocketCloseReadResult parsed =
            CloseCodec.ReadClosePayload(payload);
        Array.Clear(payload);
        if (parsed.HasError || !parsed.Valid)
        {
            WebSocketError error = parsed.Error;
            if (!parsed.HasError)
            {
                error = CreateProtocolError(
                    "close-payload-invalid",
                    "The Close payload is invalid.");
            }

            SetFailure(WebSocketParseStatus.ProtocolFailure, error);
            return CreateFailureConsumeResult();
        }

        if (RemoteCloseRecorded)
        {
            return new WebSocketInboundFrameConsumeResult();
        }

        if (StateMachine.State != WebSocketConnectionState.Open &&
            StateMachine.State != WebSocketConnectionState.Closing)
        {
            SetStateFailure(
                "remote-close-state-invalid",
                "The connection state does not permit a remote Close frame.");
            return CreateFailureConsumeResult();
        }

        WebSocketConnectionTransitionResult transition =
            StateMachine.RecordRemoteClose();
        if (transition.HasError)
        {
            SetFailure(transition.Status, transition.Error);
            return CreateFailureConsumeResult();
        }

        RemoteClose = CopyRemoteClose(parsed.Close);
        RemoteCloseRecorded = true;
        SendScheduler.StopDataFrames();
        HeartbeatManager.StopHeartbeat();
        PendingEvent = CreateCloseEvent(RemoteClose);
        PendingEchoRequired = AutomaticCloseResponse &&
            !LocalCloseCoordinator.HasLocalCloseInfo();
        PendingAcceptedChunk = chunk;
        PendingStatus = WebSocketParseStatus.NeedEventCapacity;
        return CompletePendingRemoteClose();
    }

    internal bool HasRemoteCloseInfo()
    {
        return RemoteCloseRecorded;
    }

    internal WebSocketCloseInfo GetRemoteCloseInfo()
    {
        return CopyCloseInfo(RemoteClose);
    }

    internal int GetOwnedClosePayloadCount()
    {
        return PayloadBuffer.GetMessageByteCount();
    }

    internal void ResetRemoteCloseHandler()
    {
        PayloadBuffer.ClearMessageBytes();
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingEchoRequired = false;
        PendingStatus = WebSocketParseStatus.NeedEventCapacity;
        RemoteCloseRecorded = false;
        RemoteClose = new WebSocketCloseInfo();
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    WebSocketInboundFrameConsumeResult CompletePendingRemoteClose()
    {
        if (PendingAcceptedChunk == null || PendingEvent == null)
        {
            SetStateFailure(
                "close-pending-state-invalid",
                "The pending Close delivery state is inconsistent.");
            return CreateFailureConsumeResult();
        }

        if (PendingEchoRequired)
        {
            WebSocketLocalCloseResult echoed =
                LocalCloseCoordinator.StartLocalClose(RemoteClose);
            if (echoed.HasError)
            {
                SetFailure(echoed.Status, echoed.Error);
                return CreateFailureConsumeResult();
            }

            if (!echoed.Accepted)
            {
                PendingStatus = echoed.Status;
                return CreateBackpressureResult(echoed.Status);
            }

            PendingEchoRequired = false;
        }

        if (!Events.TryEnqueueEvent(PendingEvent))
        {
            PendingStatus = WebSocketParseStatus.NeedEventCapacity;
            return CreateBackpressureResult(
                WebSocketParseStatus.NeedEventCapacity);
        }

        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingStatus = WebSocketParseStatus.NeedEventCapacity;
        return new WebSocketInboundFrameConsumeResult();
    }

    bool ValidateCapacityRequest(
        WebSocketFrameHeader header,
        bool firstChunk,
        int availableOwnedPayloadCount)
    {
        if (header == null ||
            header.Opcode != WebSocketOpcode.Close ||
            !header.Final ||
            header.PayloadLength < 0 ||
            header.PayloadLength >
                WebSocketProtocolConstants.ControlPayloadLengthMaximum ||
            availableOwnedPayloadCount < 0 ||
            (firstChunk && FrameActive) ||
            (!firstChunk &&
                (!FrameActive || !HeadersMatch(FrameHeader, header))))
        {
            SetStateFailure(
                "close-capacity-request-invalid",
                "The inbound Close capacity request is inconsistent.");
            return false;
        }

        return true;
    }

    bool ValidateChunk(WebSocketInboundFrameChunk chunk)
    {
        if (chunk == null ||
            chunk.Header == null ||
            chunk.Header.Opcode != WebSocketOpcode.Close ||
            !chunk.Header.Final ||
            chunk.Header.PayloadLength < 0 ||
            chunk.Header.PayloadLength >
                WebSocketProtocolConstants.ControlPayloadLengthMaximum ||
            chunk.Payload == null ||
            chunk.PayloadOffset < 0 ||
            chunk.PayloadOffset > chunk.Payload.Length ||
            chunk.PayloadCount < 0 ||
            chunk.PayloadCount >
                chunk.Payload.Length - chunk.PayloadOffset ||
            (chunk.FirstChunk && FrameActive) ||
            (!chunk.FirstChunk &&
                (!FrameActive ||
                    !HeadersMatch(FrameHeader, chunk.Header))))
        {
            return false;
        }

        return chunk.EndOfFrame || chunk.PayloadCount > 0;
    }

    void SetProtocolFailure(string identifier, string diagnostic)
    {
        SetFailure(
            WebSocketParseStatus.ProtocolFailure,
            CreateProtocolError(identifier, diagnostic));
    }

    void SetLimitFailure(string identifier, string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.MessageTooBig;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.MessageTooBig;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        SetFailure(WebSocketParseStatus.LimitRejected, error);
    }

    void SetStateFailure(string identifier, string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.InternalError;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.InternalError;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        SetFailure(WebSocketParseStatus.ProtocolFailure, error);
    }

    void SetFailure(WebSocketParseStatus status, WebSocketError error)
    {
        Failed = true;
        FailureStatus = status;
        FailureError = WebSocketFrameCodec.CopyError(error);
        HeartbeatManager.StopHeartbeat();
        PayloadBuffer.ClearMessageBytes();
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingEchoRequired = false;
    }

    WebSocketInboundFrameCapacity CreateFailureCapacity()
    {
        WebSocketInboundFrameCapacity capacity =
            new WebSocketInboundFrameCapacity();
        capacity.Status = FailureStatus;
        capacity.HasError = true;
        capacity.Error = WebSocketFrameCodec.CopyError(FailureError);
        return capacity;
    }

    WebSocketInboundFrameConsumeResult CreateFailureConsumeResult()
    {
        WebSocketInboundFrameConsumeResult result =
            new WebSocketInboundFrameConsumeResult();
        result.Status = FailureStatus;
        result.Accepted = false;
        result.HasError = true;
        result.Error = WebSocketFrameCodec.CopyError(FailureError);
        return result;
    }

    static WebSocketInboundFrameConsumeResult CreateBackpressureResult(
        WebSocketParseStatus status)
    {
        WebSocketInboundFrameConsumeResult result =
            new WebSocketInboundFrameConsumeResult();
        result.Status = status;
        result.Accepted = false;
        return result;
    }

    static WebSocketEvent CreateCloseEvent(WebSocketCloseInfo close)
    {
        WebSocketEvent connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.Close;
        connectionEvent.HasClose = true;
        connectionEvent.Close = CopyCloseInfo(close);
        connectionEvent.First = true;
        connectionEvent.Final = true;
        return connectionEvent;
    }

    static WebSocketCloseInfo CopyRemoteClose(WebSocketCloseInfo source)
    {
        WebSocketCloseInfo copy = CopyCloseInfo(source);
        copy.InitiatedLocally = false;
        copy.Received = true;
        return copy;
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

    static WebSocketError CreateProtocolError(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Protocol;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.ProtocolError;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.ProtocolError;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return error;
    }

    static bool HeadersMatch(
        WebSocketFrameHeader first,
        WebSocketFrameHeader second)
    {
        return first.Final == second.Final &&
            first.Rsv1 == second.Rsv1 &&
            first.Rsv2 == second.Rsv2 &&
            first.Rsv3 == second.Rsv3 &&
            first.Opcode == second.Opcode &&
            first.Masked == second.Masked &&
            first.MaskingKey == second.MaskingKey &&
            first.PayloadLength == second.PayloadLength;
    }

    static WebSocketFrameHeader CopyHeader(WebSocketFrameHeader source)
    {
        WebSocketFrameHeader copy = new WebSocketFrameHeader();
        copy.Final = source.Final;
        copy.Rsv1 = source.Rsv1;
        copy.Rsv2 = source.Rsv2;
        copy.Rsv3 = source.Rsv3;
        copy.Opcode = source.Opcode;
        copy.Masked = source.Masked;
        copy.MaskingKey = source.MaskingKey;
        copy.PayloadLength = source.PayloadLength;
        return copy;
    }
}
