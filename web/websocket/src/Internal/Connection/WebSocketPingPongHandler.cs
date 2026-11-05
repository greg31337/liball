using System;

namespace websocket;

internal class WebSocketControlSendResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal bool Accepted = true;
    internal int EncodedCount;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}

internal class WebSocketPingPongHandler
{
    WebSocketRole Role;
    bool AutomaticPong;
    int MaximumOwnedPayloadBytes;
    IWebSocketMaskKeySource? MaskKeySource;
    IWebSocketFrameCodec FrameCodec;
    WebSocketSendScheduler SendScheduler;
    WebSocketEventQueue Events;
    WebSocketReassemblyBuffer PayloadBuffer;
    WebSocketPingTracker PingTracker;
    long CurrentElapsedMilliseconds;
    bool FrameActive;
    WebSocketFrameHeader FrameHeader = new WebSocketFrameHeader();
    long FramePayloadCount;
    int OwnedPayloadCount;
    WebSocketInboundFrameChunk? PendingAcceptedChunk;
    WebSocketEvent? PendingEvent;
    bool PendingPongRequired;
    bool PendingPongQueued;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketPingPongHandler(
        WebSocketConnectionOptions options,
        WebSocketSendScheduler? sendScheduler,
        WebSocketEventQueue? events)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (sendScheduler == null || events == null)
        {
            throw new WebSocketStateException();
        }

        Role = options.Role;
        AutomaticPong = options.AutomaticPong;
        MaximumOwnedPayloadBytes = options.MaximumBufferedInputBytes;
        MaskKeySource = options.MaskKeySource;
        if (Role == WebSocketRole.Client && MaskKeySource == null)
        {
            MaskKeySource = new CryptographicWebSocketMaskKeySource();
        }

        FrameCodec = new WebSocketFrameCodec(
            Role,
            options.MaximumFramePayloadLength,
            options.NegotiatedExtensions);
        SendScheduler = sendScheduler;
        Events = events;
        PayloadBuffer = new WebSocketReassemblyBuffer(
            WebSocketProtocolConstants.ControlPayloadLengthMaximum);
        PingTracker = new WebSocketPingTracker(
            options.MaximumQueuedEvents);
    }

    internal WebSocketControlSendResult SendPing(
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        return SendTrackedPing(
            payload,
            payloadOffset,
            payloadCount,
            CurrentElapsedMilliseconds);
    }

    internal WebSocketControlSendResult SendPong(
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        return SendControlFrame(
            WebSocketOpcode.Pong,
            payload,
            payloadOffset,
            payloadCount);
    }

    internal WebSocketControlSendResult SendTrackedPing(
        byte[]? payload,
        int payloadOffset,
        int payloadCount,
        long sentElapsedMilliseconds)
    {
        WebSocketControlSendResult validation = ValidateSend(
            payload,
            payloadOffset,
            payloadCount);
        if (validation.HasError)
        {
            return validation;
        }

        if (sentElapsedMilliseconds < CurrentElapsedMilliseconds)
        {
            return CreateSendStateFailure(
                "ping-elapsed-time-regressed",
                "The tracked Ping send time cannot move backwards.");
        }

        WebSocketPingTrackingResult tracking = PingTracker.TrackPingToken(
            payload,
            payloadOffset,
            payloadCount,
            sentElapsedMilliseconds);
        if (tracking.HasError)
        {
            return CreateSendTrackingFailure(tracking);
        }

        WebSocketControlSendResult sent = SendControlFrame(
            WebSocketOpcode.Ping,
            payload,
            payloadOffset,
            payloadCount);
        if (!sent.Accepted)
        {
            PingTracker.RemovePingToken(
                payload,
                payloadOffset,
                payloadCount);
            return sent;
        }

        CurrentElapsedMilliseconds = sentElapsedMilliseconds;
        return sent;
    }

    internal WebSocketControlSendResult SetPingElapsedMilliseconds(
        long elapsedMilliseconds)
    {
        if (elapsedMilliseconds < CurrentElapsedMilliseconds)
        {
            return CreateSendStateFailure(
                "ping-elapsed-time-regressed",
                "The Ping tracking time cannot move backwards.");
        }

        CurrentElapsedMilliseconds = elapsedMilliseconds;
        return new WebSocketControlSendResult();
    }

    internal WebSocketInboundFrameCapacity GetInboundControlCapacity(
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
            capacity.Status = GetPendingStatus();
            return capacity;
        }

        if (Events.GetEventAvailableCapacity() == 0)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        if (firstChunk && header.PayloadLength > MaximumOwnedPayloadBytes)
        {
            SetLimitFailure(
                "control-receive-buffer-limit-exceeded",
                "The control payload exceeds the configured input buffer limit.");
            return CreateFailureCapacity();
        }

        if (availableOwnedPayloadCount <= 0 && header.PayloadLength != 0)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        if (firstChunk &&
            header.Opcode == WebSocketOpcode.Ping &&
            AutomaticPong)
        {
            int pongCount = MeasureControlFrame(
                WebSocketOpcode.Pong,
                (int)header.PayloadLength);
            if (pongCount > SendScheduler.GetPendingByteCapacity())
            {
                SetLimitFailure(
                    "control-send-buffer-limit-exceeded",
                    "The automatic Pong exceeds the configured output buffer limit.");
                return CreateFailureCapacity();
            }

            if (pongCount >
                SendScheduler.GetPendingByteAvailableCapacity())
            {
                capacity.Status = WebSocketParseStatus.NeedOutputCapacity;
                return capacity;
            }
        }

        long remaining = header.PayloadLength - FramePayloadCount;
        capacity.MaximumPayloadCount = (int)remaining;
        if (capacity.MaximumPayloadCount > availableOwnedPayloadCount)
        {
            capacity.MaximumPayloadCount = availableOwnedPayloadCount;
        }

        return capacity;
    }

    internal WebSocketInboundFrameConsumeResult ConsumeInboundControlChunk(
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
                    "control-pending-chunk-invalid",
                    "A different control chunk was supplied while delivery was pending.");
                return CreateFailureConsumeResult();
            }

            return CompletePendingControl();
        }

        if (!ValidateChunk(chunk))
        {
            SetStateFailure(
                "control-frame-chunk-invalid",
                "The inbound Ping or Pong frame chunk is inconsistent.");
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
                "control-frame-payload-length-invalid",
                "The Ping or Pong chunks do not match the declared payload length.");
            return CreateFailureConsumeResult();
        }

        if (!PayloadBuffer.AppendMessageBytes(
            chunk.Payload,
            chunk.PayloadOffset,
            chunk.PayloadCount))
        {
            SetLimitFailure(
                "control-payload-limit-exceeded",
                "The Ping or Pong payload exceeds 125 bytes.");
            return CreateFailureConsumeResult();
        }

        if (chunk.PayloadCount >
            MaximumOwnedPayloadBytes - OwnedPayloadCount)
        {
            SetLimitFailure(
                "control-receive-buffer-limit-exceeded",
                "The control payload exceeds the configured input buffer limit.");
            return CreateFailureConsumeResult();
        }

        FramePayloadCount = FramePayloadCount + chunk.PayloadCount;
        OwnedPayloadCount = OwnedPayloadCount + chunk.PayloadCount;
        if (!chunk.EndOfFrame)
        {
            return new WebSocketInboundFrameConsumeResult();
        }

        byte[] payload = PayloadBuffer.TakeMessageBytes();
        PendingEvent = CreateControlEvent(FrameHeader.Opcode, payload);
        if (FrameHeader.Opcode == WebSocketOpcode.Pong)
        {
            WebSocketPingTrackingResult tracking =
                PingTracker.MatchPongToken(
                    payload,
                    0,
                    payload.Length,
                    CurrentElapsedMilliseconds);
            if (tracking.HasError)
            {
                SetStateFailure(
                    tracking.Error.Identifier,
                    tracking.Error.Diagnostic);
                return CreateFailureConsumeResult();
            }

            if (tracking.Matched && tracking.HasAcknowledgementEvent)
            {
                PendingEvent = tracking.AcknowledgementEvent;
            }
        }
        PendingPongRequired = false;
        PendingPongQueued = false;
        if (FrameHeader.Opcode == WebSocketOpcode.Ping && AutomaticPong)
        {
            PendingPongRequired = true;
        }

        PendingAcceptedChunk = chunk;
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        return CompletePendingControl();
    }

    internal void ReleaseInboundControlEvent(WebSocketEvent? connectionEvent)
    {
        if (connectionEvent != null &&
            (connectionEvent.Type == WebSocketEventType.Ping ||
                connectionEvent.Type == WebSocketEventType.Pong ||
                connectionEvent.Type ==
                    WebSocketEventType.PingAcknowledged))
        {
            OwnedPayloadCount =
                OwnedPayloadCount - connectionEvent.PayloadCount;
            if (OwnedPayloadCount < 0)
            {
                OwnedPayloadCount = 0;
            }
        }
    }

    internal int GetOwnedControlPayloadCount()
    {
        return OwnedPayloadCount;
    }

    internal int GetOutstandingTrackedPingCount()
    {
        return PingTracker.GetOutstandingPingCount();
    }

    internal bool ContainsTrackedPingToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        return PingTracker.ContainsPingToken(
            token,
            tokenOffset,
            tokenCount);
    }

    internal bool RemoveTrackedPingToken(
        byte[]? token,
        int tokenOffset,
        int tokenCount)
    {
        return PingTracker.RemovePingToken(
            token,
            tokenOffset,
            tokenCount);
    }

    internal void ClearTrackedPingState()
    {
        PingTracker.ClearOutstandingPings();
    }

    internal void ResetTrackedPingState()
    {
        PingTracker.ClearOutstandingPings();
        CurrentElapsedMilliseconds = 0;
    }

    internal void ResetPingPongState()
    {
        PayloadBuffer.ClearMessageBytes();
        FrameActive = false;
        FrameHeader = new WebSocketFrameHeader();
        FramePayloadCount = 0;
        OwnedPayloadCount = 0;
        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingPongRequired = false;
        PendingPongQueued = false;
        PingTracker.ClearOutstandingPings();
        CurrentElapsedMilliseconds = 0;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    WebSocketControlSendResult SendControlFrame(
        WebSocketOpcode opcode,
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        WebSocketControlSendResult validation = ValidateSend(
            payload,
            payloadOffset,
            payloadCount);
        if (validation.HasError)
        {
            return validation;
        }

        if (payload == null)
        {
            return CreateSendStateFailure(
                "control-send-payload-invalid",
                "The control send payload is required.");
        }

        byte[] encoded = EncodeControlFrame(
            opcode,
            payload,
            payloadOffset,
            payloadCount);
        if (encoded.Length > SendScheduler.GetPendingByteCapacity())
        {
            Array.Clear(encoded);
            return CreateSendLimitFailure(
                "control-send-buffer-limit-exceeded",
                "The Ping or Pong exceeds the configured output buffer limit.");
        }

        if (!SendScheduler.TryQueueControlFrame(encoded))
        {
            WebSocketControlSendResult blocked =
                new WebSocketControlSendResult();
            blocked.Status = WebSocketParseStatus.NeedOutputCapacity;
            blocked.Accepted = false;
            return blocked;
        }

        WebSocketControlSendResult result =
            new WebSocketControlSendResult();
        result.EncodedCount = encoded.Length;
        return result;
    }

    WebSocketControlSendResult ValidateSend(
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        if (payload == null ||
            payloadOffset < 0 ||
            payloadOffset > payload.Length ||
            payloadCount < 0 ||
            payloadCount > payload.Length - payloadOffset)
        {
            return CreateSendStateFailure(
                "control-send-payload-range-invalid",
                "The control send payload range is invalid.");
        }

        if (payloadCount >
            WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            return CreateSendLimitFailure(
                "control-send-payload-limit-exceeded",
                "The Ping or Pong payload exceeds 125 bytes.");
        }

        return new WebSocketControlSendResult();
    }

    byte[] EncodeControlFrame(
        WebSocketOpcode opcode,
        byte[] payload,
        int payloadOffset,
        int payloadCount)
    {
        WebSocketFrameWriteRequest request = CreateFrameRequest(
            opcode,
            payload,
            payloadOffset,
            payloadCount);
        int encodedCount = FrameCodec.MeasureFrame(request);
        byte[] encoded = new byte[encodedCount];
        int written = FrameCodec.WriteFrame(request, encoded);
        if (written != encodedCount || request.PayloadCount != 0)
        {
            throw new WebSocketStateException();
        }

        return encoded;
    }

    int MeasureControlFrame(WebSocketOpcode opcode, int payloadCount)
    {
        byte[] payload = new byte[payloadCount];
        WebSocketFrameWriteRequest request = CreateFrameRequest(
            opcode,
            payload,
            0,
            payloadCount);
        return FrameCodec.MeasureFrame(request);
    }

    WebSocketFrameWriteRequest CreateFrameRequest(
        WebSocketOpcode opcode,
        byte[] payload,
        int payloadOffset,
        int payloadCount)
    {
        WebSocketFrameHeader header = new WebSocketFrameHeader();
        header.Final = true;
        header.Opcode = opcode;
        header.Masked = Role == WebSocketRole.Client;
        header.PayloadLength = payloadCount;
        WebSocketFrameWriteRequest request =
            new WebSocketFrameWriteRequest();
        request.Header = header;
        request.Payload = payload;
        request.PayloadOffset = payloadOffset;
        request.PayloadCount = payloadCount;
        request.HasMaskKeySource = header.Masked;
        if (header.Masked)
        {
            request.MaskKeySource = MaskKeySource;
        }

        return request;
    }

    WebSocketInboundFrameConsumeResult CompletePendingControl()
    {
        if (PendingEvent == null || PendingAcceptedChunk == null)
        {
            SetStateFailure(
                "control-pending-state-invalid",
                "The pending Ping or Pong delivery state is inconsistent.");
            return CreateFailureConsumeResult();
        }

        if (Events.GetEventAvailableCapacity() == 0)
        {
            return CreateBackpressureConsumeResult(
                WebSocketParseStatus.NeedEventCapacity);
        }

        if (PendingPongRequired && !PendingPongQueued)
        {
            int encodedCount = GetPendingPongEncodedCount();
            if (encodedCount > SendScheduler.GetPendingByteCapacity())
            {
                SetLimitFailure(
                    "control-send-buffer-limit-exceeded",
                    "The automatic Pong exceeds the configured output buffer limit.");
                return CreateFailureConsumeResult();
            }

            if (encodedCount >
                SendScheduler.GetPendingByteAvailableCapacity())
            {
                return CreateBackpressureConsumeResult(
                    WebSocketParseStatus.NeedOutputCapacity);
            }

            if (PendingEvent == null)
            {
                SetStateFailure(
                    "control-pending-state-invalid",
                    "The pending Ping response state is inconsistent.");
                return CreateFailureConsumeResult();
            }

            byte[] encoded = EncodeControlFrame(
                WebSocketOpcode.Pong,
                PendingEvent.Payload,
                PendingEvent.PayloadOffset,
                PendingEvent.PayloadCount);
            if (!SendScheduler.TryQueueControlFrame(encoded))
            {
                Array.Clear(encoded);
                return CreateBackpressureConsumeResult(
                    WebSocketParseStatus.NeedOutputCapacity);
            }

            Array.Clear(encoded);
            PendingPongQueued = true;
        }

        if (!Events.TryEnqueueEvent(PendingEvent))
        {
            return CreateBackpressureConsumeResult(
                WebSocketParseStatus.NeedEventCapacity);
        }

        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingPongRequired = false;
        PendingPongQueued = false;
        return new WebSocketInboundFrameConsumeResult();
    }

    WebSocketParseStatus GetPendingStatus()
    {
        if (Events.GetEventAvailableCapacity() == 0)
        {
            return WebSocketParseStatus.NeedEventCapacity;
        }

        if (PendingPongRequired &&
            !PendingPongQueued &&
            GetPendingPongEncodedCount() >
                SendScheduler.GetPendingByteAvailableCapacity())
        {
            return WebSocketParseStatus.NeedOutputCapacity;
        }

        return WebSocketParseStatus.Complete;
    }

    int GetPendingPongEncodedCount()
    {
        if (PendingEvent == null)
        {
            return 0;
        }

        return MeasureControlFrame(
            WebSocketOpcode.Pong,
            PendingEvent.PayloadCount);
    }

    bool ValidateCapacityRequest(
        WebSocketFrameHeader header,
        bool firstChunk,
        int availableOwnedPayloadCount)
    {
        if (header == null ||
            availableOwnedPayloadCount < 0 ||
            !IsPingPong(header.Opcode) ||
            !header.Final ||
            header.PayloadLength < 0 ||
            header.PayloadLength >
                WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            SetStateFailure(
                "control-frame-header-invalid",
                "The inbound Ping or Pong frame header is invalid.");
            return false;
        }

        if (firstChunk)
        {
            if (FrameActive || PendingAcceptedChunk != null)
            {
                SetStateFailure(
                    "control-frame-overlap",
                    "A Ping or Pong frame started before the prior control frame completed.");
                return false;
            }
        }
        else if (!FrameActive || !HeadersMatch(FrameHeader, header))
        {
            SetStateFailure(
                "control-frame-continuation-invalid",
                "The Ping or Pong payload does not continue the active control frame.");
            return false;
        }

        return true;
    }

    bool ValidateChunk(WebSocketInboundFrameChunk chunk)
    {
        if (chunk == null ||
            chunk.Header == null ||
            chunk.Payload == null ||
            chunk.PayloadOffset < 0 ||
            chunk.PayloadOffset > chunk.Payload.Length ||
            chunk.PayloadCount < 0 ||
            chunk.PayloadCount >
                chunk.Payload.Length - chunk.PayloadOffset ||
            !IsPingPong(chunk.Header.Opcode) ||
            !chunk.Header.Final ||
            chunk.Header.PayloadLength < 0 ||
            chunk.Header.PayloadLength >
                WebSocketProtocolConstants.ControlPayloadLengthMaximum)
        {
            return false;
        }

        if (chunk.FirstChunk)
        {
            return !FrameActive;
        }

        return FrameActive && HeadersMatch(FrameHeader, chunk.Header);
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

    void SetFailure(WebSocketParseStatus status, WebSocketError error)
    {
        Failed = true;
        FailureStatus = status;
        FailureError = WebSocketFrameCodec.CopyError(error);
        PingTracker.ClearOutstandingPings();
        PayloadBuffer.ClearMessageBytes();
        FrameActive = false;
        PendingAcceptedChunk = null;
        PendingEvent = null;
        PendingPongRequired = false;
        PendingPongQueued = false;
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

    static WebSocketInboundFrameConsumeResult CreateBackpressureConsumeResult(
        WebSocketParseStatus status)
    {
        WebSocketInboundFrameConsumeResult result =
            new WebSocketInboundFrameConsumeResult();
        result.Status = status;
        result.Accepted = false;
        return result;
    }

    static WebSocketControlSendResult CreateSendStateFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.State;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateSendFailure(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    static WebSocketControlSendResult CreateSendLimitFailure(
        string identifier,
        string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Limit;
        error.Identifier = identifier;
        error.Diagnostic = diagnostic;
        return CreateSendFailure(
            WebSocketParseStatus.LimitRejected,
            error);
    }

    static WebSocketControlSendResult CreateSendTrackingFailure(
        WebSocketPingTrackingResult tracking)
    {
        WebSocketControlSendResult result =
            new WebSocketControlSendResult();
        result.Status = tracking.Status;
        result.Accepted = false;
        result.HasError = tracking.HasError;
        result.Error = WebSocketFrameCodec.CopyError(tracking.Error);
        return result;
    }

    static WebSocketControlSendResult CreateSendFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        WebSocketControlSendResult result =
            new WebSocketControlSendResult();
        result.Status = status;
        result.Accepted = false;
        result.HasError = true;
        result.Error = error;
        return result;
    }

    static WebSocketEvent CreateControlEvent(
        WebSocketOpcode opcode,
        byte[] payload)
    {
        WebSocketEvent connectionEvent = new WebSocketEvent();
        connectionEvent.Type = WebSocketEventType.Ping;
        if (opcode == WebSocketOpcode.Pong)
        {
            connectionEvent.Type = WebSocketEventType.Pong;
        }

        connectionEvent.Payload = payload;
        connectionEvent.PayloadOffset = 0;
        connectionEvent.PayloadCount = payload.Length;
        connectionEvent.First = true;
        connectionEvent.Final = true;
        return connectionEvent;
    }

    static bool IsPingPong(WebSocketOpcode opcode)
    {
        return opcode == WebSocketOpcode.Ping ||
            opcode == WebSocketOpcode.Pong;
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
        copy.PayloadLength = source.PayloadLength;
        return copy;
    }
}
