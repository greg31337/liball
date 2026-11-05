using System;

namespace websocket;

internal class WebSocketStreamingReceiveConsumer :
    IWebSocketInboundFrameConsumer
{
    WebSocketRole Role;
    int MaximumFramePayloadLength;
    int MaximumMessagePayloadLength;
    int MaximumDecompressedMessagePayloadLength;
    int MaximumOwnedPayloadBytes;
    int ExternalOwnedPayloadCount;
    WebSocketInboundMessageTracker MessageTracker;
    WebSocketExtensionPipeline ExtensionPipeline;
    WebSocketEventQueue Events;
    WebSocketSendScheduler SendScheduler;
    WebSocketPingPongHandler PingPongHandler;
    WebSocketHeartbeatManager HeartbeatManager;
    WebSocketConnectionStateMachine StateMachine;
    WebSocketLocalCloseCoordinator LocalCloseCoordinator;
    WebSocketRemoteCloseHandler RemoteCloseHandler;
    WebSocketConnectionFailureHandler FailureHandler;
    IWebSocketUtf8Validator Utf8Validator;
    long RawMessagePayloadCount;
    long TransformedMessagePayloadCount;
    bool MessageDataDelivered;
    int QueuedPayloadCount;
    WebSocketInboundFrameChunk? PendingChunk;
    WebSocketInboundMessageTrackResult? PendingTracking;
    int PendingSourceOffset;
    int PendingTransformMaximumOutputCount;
    bool PendingTransformComplete;
    bool PendingStartRequired;
    bool PendingEndRequired;
    byte[] PendingOutput = [];
    bool PendingOutputFinal;
    bool ApplicationDataSuppressed;
    long CompletedMessageCount;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketStreamingReceiveConsumer(
        WebSocketConnectionOptions options,
        WebSocketExtensionPipeline? extensionPipeline)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (options.DeliveryMode != WebSocketMessageDeliveryMode.Streaming ||
            extensionPipeline == null)
        {
            throw new WebSocketStateException();
        }

        Role = options.Role;
        MaximumFramePayloadLength = options.MaximumFramePayloadLength;
        MaximumMessagePayloadLength = options.MaximumMessagePayloadLength;
        MaximumDecompressedMessagePayloadLength =
            options.MaximumDecompressedMessagePayloadLength;
        MaximumOwnedPayloadBytes = options.MaximumBufferedInputBytes;
        MessageTracker = new WebSocketInboundMessageTracker(
            options.MaximumFragmentCount);
        ExtensionPipeline = extensionPipeline;
        Events = new WebSocketEventQueue(options.MaximumQueuedEvents);
        SendScheduler = new WebSocketSendScheduler(
            options.MaximumBufferedOutputBytes);
        PingPongHandler = new WebSocketPingPongHandler(
            options,
            SendScheduler,
            Events);
        HeartbeatManager = new WebSocketHeartbeatManager(
            options,
            PingPongHandler);
        StateMachine = new WebSocketConnectionStateMachine(
            options.Role,
            options.Transport,
            options.CloseTimeoutMilliseconds);
        StateMachine.OpenConnection();
        LocalCloseCoordinator = new WebSocketLocalCloseCoordinator(
            options,
            SendScheduler,
            StateMachine,
            HeartbeatManager);
        RemoteCloseHandler = new WebSocketRemoteCloseHandler(
            options,
            SendScheduler,
            Events,
            StateMachine,
            LocalCloseCoordinator,
            HeartbeatManager);
        FailureHandler = new WebSocketConnectionFailureHandler(
            SendScheduler,
            Events,
            StateMachine,
            LocalCloseCoordinator,
            HeartbeatManager);
        Utf8Validator = new WebSocketUtf8Validator();
    }

    public WebSocketInboundFrameCapacity GetInboundFrameCapacity(
        WebSocketFrameHeader header,
        bool firstChunk)
    {
        if (Failed)
        {
            return CreateFailureCapacity();
        }

        if (header == null || header.PayloadLength < 0)
        {
            SetStateFailure(
                "stream-frame-header-invalid",
                "The streaming receiver requires a valid frame header.");
            return CreateFailureCapacity();
        }

        if (PendingChunk != null)
        {
            WebSocketInboundFrameCapacity pending =
                new WebSocketInboundFrameCapacity();
            pending.Status = WebSocketParseStatus.NeedEventCapacity;
            return pending;
        }

        if (IsPingPongOpcode(header.Opcode))
        {
            if (RemoteCloseHandler.HasRemoteCloseInfo())
            {
                WebSocketInboundFrameCapacity ignoredControlCapacity =
                    new WebSocketInboundFrameCapacity();
                ignoredControlCapacity.MaximumPayloadCount =
                    MaximumFramePayloadLength;
                return ignoredControlCapacity;
            }

            WebSocketInboundFrameCapacity controlCapacity =
                PingPongHandler.GetInboundControlCapacity(
                header,
                firstChunk,
                GetAvailablePayloadCapacity());
            if (controlCapacity.HasError)
            {
                SetFailure(controlCapacity.Status, controlCapacity.Error);
                return CreateFailureCapacity();
            }

            return controlCapacity;
        }

        if (header.Opcode == WebSocketOpcode.Close)
        {
            WebSocketInboundFrameCapacity closeCapacity =
                RemoteCloseHandler.GetInboundCloseCapacity(
                header,
                firstChunk,
                GetAvailablePayloadCapacity());
            if (closeCapacity.HasError)
            {
                SetFailure(closeCapacity.Status, closeCapacity.Error);
                return CreateFailureCapacity();
            }

            return closeCapacity;
        }

        WebSocketInboundFrameCapacity capacity =
            new WebSocketInboundFrameCapacity();
        bool dataFrame = IsStandardDataOpcode(header.Opcode);
        if (dataFrame && ApplicationDataSuppressed)
        {
            capacity.MaximumPayloadCount = MaximumFramePayloadLength;
            return capacity;
        }

        if (!dataFrame)
        {
            capacity.MaximumPayloadCount = MaximumOwnedPayloadBytes;
            return capacity;
        }

        if (Events.GetEventAvailableCapacity() == 0)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        if (header.PayloadLength == 0)
        {
            capacity.MaximumPayloadCount = 0;
            return capacity;
        }

        int availablePayloadCount = GetAvailablePayloadCapacity();
        if (availablePayloadCount == 0)
        {
            capacity.Status = WebSocketParseStatus.NeedEventCapacity;
            return capacity;
        }

        capacity.MaximumPayloadCount = availablePayloadCount;
        if (capacity.MaximumPayloadCount > MaximumFramePayloadLength)
        {
            capacity.MaximumPayloadCount = MaximumFramePayloadLength;
        }

        return capacity;
    }

    public WebSocketInboundFrameConsumeResult ConsumeInboundFrameChunk(
        WebSocketInboundFrameChunk chunk)
    {
        if (Failed)
        {
            return CreateFailureConsumeResult();
        }

        if (chunk == null)
        {
            SetStateFailure(
                "stream-frame-chunk-invalid",
                "The streaming receiver requires a frame chunk.");
            return CreateFailureConsumeResult();
        }

        if (chunk.Header != null &&
            IsPingPongOpcode(chunk.Header.Opcode))
        {
            if (RemoteCloseHandler.HasRemoteCloseInfo())
            {
                return new WebSocketInboundFrameConsumeResult();
            }

            WebSocketInboundFrameConsumeResult control =
                PingPongHandler.ConsumeInboundControlChunk(chunk);
            if (!control.HasError)
            {
                HeartbeatManager.RecordHeartbeatActivity();
                HeartbeatManager.SynchronizeHeartbeatAcknowledgement();
            }
            else
            {
                SetFailure(control.Status, control.Error);
                return CreateFailureConsumeResult();
            }

            return control;
        }

        if (chunk.Header != null &&
            chunk.Header.Opcode == WebSocketOpcode.Close)
        {
            bool hadRemoteClose =
                RemoteCloseHandler.HasRemoteCloseInfo();
            WebSocketInboundFrameConsumeResult close =
                RemoteCloseHandler.ConsumeInboundCloseChunk(chunk);
            if (!hadRemoteClose &&
                RemoteCloseHandler.HasRemoteCloseInfo())
            {
                CancelInboundApplicationData();
            }

            if (close.HasError)
            {
                SetFailure(close.Status, close.Error);
                return CreateFailureConsumeResult();
            }

            return close;
        }

        if (chunk.Header != null &&
            ApplicationDataSuppressed &&
            IsStandardDataOpcode(chunk.Header.Opcode))
        {
            return new WebSocketInboundFrameConsumeResult();
        }

        if (PendingChunk == null)
        {
            WebSocketInboundMessageTrackResult tracking =
                MessageTracker.TrackInboundFrameChunk(chunk);
            if (tracking.HasError)
            {
                SetFailure(tracking.Status, tracking.Error);
                return CreateFailureConsumeResult();
            }

            if (!tracking.DataChunk)
            {
                return new WebSocketInboundFrameConsumeResult();
            }

            InitializePendingChunk(chunk, tracking);
            if (Failed)
            {
                return CreateFailureConsumeResult();
            }
        }
        else if (!object.ReferenceEquals(PendingChunk, chunk))
        {
            SetStateFailure(
                "stream-pending-chunk-invalid",
                "A different frame chunk was supplied while delivery was pending.");
            return CreateFailureConsumeResult();
        }

        if (PendingOutput.Length != 0 ||
            PendingStartRequired ||
            PendingEndRequired)
        {
            if (!QueuePendingEvents())
            {
                return CreateBackpressureResult();
            }
        }

        if (!PendingTransformComplete)
        {
            if (Events.GetEventAvailableCapacity() == 0 ||
                GetAvailablePayloadCapacity() == 0)
            {
                return CreateBackpressureResult();
            }

            if (!AdvancePendingTransform())
            {
                return CreateFailureConsumeResult();
            }

            if (!QueuePendingEvents())
            {
                return CreateBackpressureResult();
            }
        }

        if (!PendingTransformComplete ||
            PendingOutput.Length != 0 ||
            PendingStartRequired ||
            PendingEndRequired)
        {
            return CreateBackpressureResult();
        }

        HeartbeatManager.RecordHeartbeatActivity();
        ClearPendingChunk();
        return new WebSocketInboundFrameConsumeResult();
    }

    public void ResetInboundFrameConsumer()
    {
        MessageTracker.ResetInboundMessageTracker();
        ExtensionPipeline.ResetPipeline();
        Events.ClearEvents();
        SendScheduler.ClearPendingBytes();
        HeartbeatManager.ResetHeartbeatState();
        PingPongHandler.ResetPingPongState();
        StateMachine.ResetConnectionStateMachine();
        StateMachine.OpenConnection();
        LocalCloseCoordinator.ResetLocalCloseCoordinator();
        RemoteCloseHandler.ResetRemoteCloseHandler();
        FailureHandler.ResetConnectionFailureHandler();
        Utf8Validator.ResetValidation();
        RawMessagePayloadCount = 0;
        TransformedMessagePayloadCount = 0;
        MessageDataDelivered = false;
        QueuedPayloadCount = 0;
        ExternalOwnedPayloadCount = 0;
        ClearPendingChunk();
        ApplicationDataSuppressed = false;
        CompletedMessageCount = 0;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    public void HandleInboundFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        SetFailure(status, error);
    }

    internal WebSocketEvent? ReadStreamingEvent()
    {
        FailureHandler.SynchronizeFailureHandling();
        WebSocketEvent? connectionEvent = Events.DequeueEvent();
        if (connectionEvent != null)
        {
            PingPongHandler.ReleaseInboundControlEvent(connectionEvent);
            if (connectionEvent.Type == WebSocketEventType.MessageData)
            {
                QueuedPayloadCount =
                    QueuedPayloadCount - connectionEvent.PayloadCount;
            }
        }

        FailureHandler.SynchronizeFailureHandling();

        return connectionEvent;
    }

    internal int GetStreamingEventCount()
    {
        FailureHandler.SynchronizeFailureHandling();
        return Events.GetEventCount();
    }

    internal bool TryQueueStreamingTransportEndEvent(
        WebSocketEvent? connectionEvent)
    {
        FailureHandler.SynchronizeFailureHandling();
        return Events.TryEnqueueEvent(connectionEvent);
    }

    internal int GetOwnedPayloadCount()
    {
        return ExternalOwnedPayloadCount + QueuedPayloadCount +
            PendingOutput.Length +
            PingPongHandler.GetOwnedControlPayloadCount() +
            RemoteCloseHandler.GetOwnedClosePayloadCount();
    }

    internal void SetExternalOwnedPayloadCount(int ownedPayloadCount)
    {
        if (ownedPayloadCount < 0 ||
            ownedPayloadCount > MaximumOwnedPayloadBytes)
        {
            throw new WebSocketStateException();
        }

        ExternalOwnedPayloadCount = ownedPayloadCount;
    }

    internal WebSocketSendScheduler GetStreamingSendScheduler()
    {
        return SendScheduler;
    }

    internal WebSocketEventQueue GetStreamingEventQueue()
    {
        return Events;
    }

    internal WebSocketPingPongHandler GetStreamingPingPongHandler()
    {
        return PingPongHandler;
    }

    internal WebSocketHeartbeatManager GetStreamingHeartbeatManager()
    {
        return HeartbeatManager;
    }

    internal WebSocketConnectionStateMachine GetStreamingStateMachine()
    {
        return StateMachine;
    }

    internal WebSocketLocalCloseCoordinator GetStreamingLocalCloseCoordinator()
    {
        return LocalCloseCoordinator;
    }

    internal WebSocketRemoteCloseHandler GetStreamingRemoteCloseHandler()
    {
        return RemoteCloseHandler;
    }

    internal WebSocketConnectionFailureHandler GetStreamingFailureHandler()
    {
        return FailureHandler;
    }

    internal bool GetApplicationDataSuppressed()
    {
        return ApplicationDataSuppressed;
    }

    internal long GetCompletedStreamingMessageCount()
    {
        return CompletedMessageCount;
    }

    void InitializePendingChunk(
        WebSocketInboundFrameChunk chunk,
        WebSocketInboundMessageTrackResult tracking)
    {
        PendingChunk = chunk;
        PendingTracking = tracking;
        PendingSourceOffset = 0;
        PendingTransformMaximumOutputCount = 0;
        PendingTransformComplete = false;
        PendingStartRequired = tracking.MessageStarted;
        PendingEndRequired = false;
        PendingOutput = [];
        PendingOutputFinal = false;
        if (tracking.MessageStarted)
        {
            RawMessagePayloadCount = 0;
            TransformedMessagePayloadCount = 0;
            MessageDataDelivered = false;
            if (tracking.MessageType == WebSocketMessageType.Text)
            {
                Utf8Validator.ResetValidation();
            }
        }

        RawMessagePayloadCount =
            RawMessagePayloadCount + tracking.PayloadCount;
        if (RawMessagePayloadCount > MaximumMessagePayloadLength)
        {
            SetLimitFailure(
                "stream-message-payload-limit-exceeded",
                "The streamed message exceeds the configured payload limit.");
            return;
        }

        AdvancePendingTransform();
    }

    bool AdvancePendingTransform()
    {
        if (PendingChunk == null || PendingTracking == null)
        {
            SetStateFailure(
                "stream-pending-state-invalid",
                "The streaming receiver has inconsistent pending state.");
            return false;
        }

        while (true)
        {
            int availablePayloadCount = GetAvailablePayloadCapacity();
            if (availablePayloadCount <= 0)
            {
                return true;
            }

            PendingTransformMaximumOutputCount = availablePayloadCount;

            WebSocketExtensionContext context = CreateTransformContext(
                PendingTracking,
                PendingTransformMaximumOutputCount);
            int remainingSourceCount =
                PendingTracking.PayloadCount - PendingSourceOffset;
            ReadOnlySpan<byte> source = PendingTracking.Payload.AsSpan(
                PendingTracking.PayloadOffset + PendingSourceOffset,
                remainingSourceCount);
            WebSocketTransformResult transformed =
                ExtensionPipeline.TransformIncoming(context, source);
            if (transformed.HasError)
            {
                SetFailure(transformed.Status, transformed.Error);
                return false;
            }

            if (transformed.Status != WebSocketParseStatus.Complete &&
                transformed.Status != WebSocketParseStatus.NeedOutputCapacity)
            {
                SetStateFailure(
                    "stream-transform-status-invalid",
                    "The inbound extension transform did not make deliverable progress.");
                return false;
            }

            PendingSourceOffset =
                PendingSourceOffset + transformed.ConsumedCount;
            if (transformed.Status == WebSocketParseStatus.Complete)
            {
                if (PendingSourceOffset != PendingTracking.PayloadCount)
                {
                    SetStateFailure(
                        "stream-transform-consumption-invalid",
                        "The inbound extension transform did not consume the frame chunk.");
                    return false;
                }

                PendingTransformComplete = true;
                PendingEndRequired = PendingTracking.MessageEnded;
            }

            byte[] output = CopyTransformOutput(transformed);
            if (!ValidateTransformedOutput(PendingTracking, output))
            {
                return false;
            }

            if (output.Length != 0)
            {
                PendingOutput = output;
                PendingOutputFinal = PendingTransformComplete &&
                    PendingTracking.MessageEnded;
                return true;
            }

            if (PendingTransformComplete)
            {
                if (PendingTracking.MessageEnded &&
                    PendingTracking.MessageType == WebSocketMessageType.Text &&
                    !Utf8Validator.CompleteValidation())
                {
                    SetUtf8Failure(
                        "stream-text-utf8-incomplete",
                        "The streamed text message ends with incomplete UTF-8.");
                    return false;
                }

                return true;
            }

            if (transformed.ConsumedCount == 0)
            {
                SetStateFailure(
                    "stream-transform-progress-invalid",
                    "The inbound extension transform made no progress.");
                return false;
            }
        }
    }

    bool ValidateTransformedOutput(
        WebSocketInboundMessageTrackResult tracking,
        byte[] output)
    {
        if (output.Length >
            MaximumDecompressedMessagePayloadLength -
                TransformedMessagePayloadCount)
        {
            SetLimitFailure(
                "stream-decompressed-payload-limit-exceeded",
                "The transformed message exceeds the configured payload limit.");
            return false;
        }

        if (tracking.MessageType == WebSocketMessageType.Text &&
            output.Length != 0 &&
            !Utf8Validator.ValidateChunk(output))
        {
            SetUtf8Failure(
                "stream-text-utf8-invalid",
                "The transformed text chunk contains invalid UTF-8.");
            return false;
        }

        TransformedMessagePayloadCount =
            TransformedMessagePayloadCount + output.Length;
        if (tracking.MessageEnded &&
            PendingTransformComplete &&
            output.Length != 0 &&
            tracking.MessageType == WebSocketMessageType.Text &&
            !Utf8Validator.CompleteValidation())
        {
            SetUtf8Failure(
                "stream-text-utf8-incomplete",
                "The streamed text message ends with incomplete UTF-8.");
            return false;
        }

        return true;
    }

    bool QueuePendingEvents()
    {
        if (PendingTracking == null)
        {
            SetStateFailure(
                "stream-pending-state-invalid",
                "The streaming receiver has inconsistent pending state.");
            return false;
        }

        if (PendingStartRequired)
        {
            WebSocketEvent started = CreateBoundaryEvent(
                WebSocketEventType.MessageStart,
                PendingTracking.MessageType,
                true,
                false);
            if (!Events.TryEnqueueEvent(started))
            {
                return false;
            }

            PendingStartRequired = false;
        }

        if (PendingOutput.Length != 0)
        {
            if (Events.GetEventAvailableCapacity() == 0 ||
                PendingOutput.Length >
                    MaximumOwnedPayloadBytes - QueuedPayloadCount)
            {
                return false;
            }

            WebSocketEvent data = new WebSocketEvent();
            data.Type = WebSocketEventType.MessageData;
            data.HasMessageType = true;
            data.MessageType = PendingTracking.MessageType;
            data.Payload = PendingOutput;
            data.PayloadOffset = 0;
            data.PayloadCount = PendingOutput.Length;
            data.First = !MessageDataDelivered;
            data.Final = PendingOutputFinal;
            if (!Events.TryEnqueueEvent(data))
            {
                return false;
            }

            QueuedPayloadCount = QueuedPayloadCount + PendingOutput.Length;
            MessageDataDelivered = true;
            PendingOutput = [];
            PendingOutputFinal = false;
        }

        if (PendingEndRequired)
        {
            WebSocketEvent ended = CreateBoundaryEvent(
                WebSocketEventType.MessageEnd,
                PendingTracking.MessageType,
                false,
                true);
            if (!Events.TryEnqueueEvent(ended))
            {
                return false;
            }

            PendingEndRequired = false;
            CompletedMessageCount = CompletedMessageCount + 1;
        }

        return true;
    }

    WebSocketExtensionContext CreateTransformContext(
        WebSocketInboundMessageTrackResult tracking,
        int maximumOutputCount)
    {
        WebSocketExtensionContext context = new WebSocketExtensionContext();
        context.Role = Role;
        context.Outbound = false;
        context.Rsv1 = tracking.Header.Rsv1;
        context.Rsv2 = tracking.Header.Rsv2;
        context.Rsv3 = tracking.Header.Rsv3;
        context.HasOpcode = true;
        context.Opcode = tracking.Header.Opcode;
        context.MessageType = tracking.MessageType;
        context.FirstChunk = tracking.MessageStarted;
        context.FinalChunk = tracking.MessageEnded;
        context.FirstFragment =
            tracking.Header.Opcode == WebSocketOpcode.Text ||
            tracking.Header.Opcode == WebSocketOpcode.Binary;
        context.FinalFragment = tracking.Header.Final;
        context.MaximumFramePayloadLength = MaximumFramePayloadLength;
        context.MaximumMessagePayloadLength = MaximumMessagePayloadLength;
        context.MaximumDecompressedMessagePayloadLength =
            MaximumDecompressedMessagePayloadLength;
        context.MaximumBufferedOutputBytes = maximumOutputCount;
        return context;
    }

    void ClearPendingChunk()
    {
        PendingChunk = null;
        PendingTracking = null;
        PendingSourceOffset = 0;
        PendingTransformMaximumOutputCount = 0;
        PendingTransformComplete = false;
        PendingStartRequired = false;
        PendingEndRequired = false;
        PendingOutput = [];
        PendingOutputFinal = false;
    }

    void CancelInboundApplicationData()
    {
        ApplicationDataSuppressed = true;
        MessageTracker.ResetInboundMessageTracker();
        ExtensionPipeline.ResetPipeline();
        Utf8Validator.ResetValidation();
        RawMessagePayloadCount = 0;
        TransformedMessagePayloadCount = 0;
        MessageDataDelivered = false;
        ClearPendingChunk();
    }

    int GetAvailablePayloadCapacity()
    {
        int pendingCount = PendingOutput.Length;
        int controlPayloadCount =
            PingPongHandler.GetOwnedControlPayloadCount();
        int closePayloadCount =
            RemoteCloseHandler.GetOwnedClosePayloadCount();
        int available = MaximumOwnedPayloadBytes -
            ExternalOwnedPayloadCount - QueuedPayloadCount -
            controlPayloadCount - closePayloadCount;
        if (pendingCount < available)
        {
            available = available - pendingCount;
        }
        else
        {
            available = 0;
        }

        return available;
    }

    void SetUtf8Failure(string identifier, string diagnostic)
    {
        WebSocketError error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Utf8;
        error.HasCloseCode = true;
        error.CloseCode = WebSocketCloseCodes.InvalidFramePayloadData;
        error.HasKnownCloseCode = true;
        error.KnownCloseCode = WebSocketCloseCode.InvalidFramePayloadData;
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
        FailureHandler.HandleConnectionFailure(status, error);
        Failed = true;
        FailureStatus = status;
        FailureError = FailureHandler.GetConnectionFailureError();
        HeartbeatManager.StopHeartbeat();
        PingPongHandler.ClearTrackedPingState();
        ApplicationDataSuppressed = true;
        MessageTracker.ResetInboundMessageTracker();
        ExtensionPipeline.ResetPipeline();
        Utf8Validator.ResetValidation();
        ClearPendingChunk();
        PendingOutput = [];
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

    static WebSocketInboundFrameConsumeResult CreateBackpressureResult()
    {
        WebSocketInboundFrameConsumeResult result =
            new WebSocketInboundFrameConsumeResult();
        result.Status = WebSocketParseStatus.NeedEventCapacity;
        result.Accepted = false;
        return result;
    }

    static WebSocketEvent CreateBoundaryEvent(
        WebSocketEventType type,
        WebSocketMessageType messageType,
        bool first,
        bool final)
    {
        WebSocketEvent connectionEvent = new WebSocketEvent();
        connectionEvent.Type = type;
        connectionEvent.HasMessageType = true;
        connectionEvent.MessageType = messageType;
        connectionEvent.First = first;
        connectionEvent.Final = final;
        return connectionEvent;
    }

    static byte[] CopyTransformOutput(WebSocketTransformResult transformed)
    {
        byte[] output = new byte[transformed.ProducedCount];
        transformed.Output.AsSpan(0, transformed.ProducedCount).CopyTo(output);
        return output;
    }

    static bool IsStandardDataOpcode(WebSocketOpcode opcode)
    {
        return opcode == WebSocketOpcode.Continuation ||
            opcode == WebSocketOpcode.Text ||
            opcode == WebSocketOpcode.Binary;
    }

    static bool IsPingPongOpcode(WebSocketOpcode opcode)
    {
        return opcode == WebSocketOpcode.Ping ||
            opcode == WebSocketOpcode.Pong;
    }
}
