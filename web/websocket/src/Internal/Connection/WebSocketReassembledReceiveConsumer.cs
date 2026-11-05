using System;

namespace websocket;

internal class WebSocketReassemblyBuffer
{
    int MaximumCount;
    byte[] Buffer = [];
    int Count;

    internal WebSocketReassemblyBuffer(int maximumCount)
    {
        if (maximumCount <= 0)
        {
            throw new WebSocketLimitException();
        }

        MaximumCount = maximumCount;
    }

    internal bool AppendMessageBytes(
        byte[] source,
        int sourceOffset,
        int sourceCount)
    {
        if (source == null ||
            sourceOffset < 0 ||
            sourceOffset > source.Length ||
            sourceCount < 0 ||
            sourceCount > source.Length - sourceOffset)
        {
            throw new WebSocketStateException();
        }

        if (sourceCount > MaximumCount - Count)
        {
            return false;
        }

        int requiredCount = Count + sourceCount;
        if (requiredCount > Buffer.Length)
        {
            int capacity = Buffer.Length;
            if (capacity == 0)
            {
                capacity = 1;
            }

            while (capacity < requiredCount)
            {
                if (capacity > MaximumCount / 2)
                {
                    capacity = MaximumCount;
                }
                else
                {
                    capacity = capacity * 2;
                }
            }

            byte[] grown = new byte[capacity];
            Buffer.AsSpan(0, Count).CopyTo(grown);
            Buffer = grown;
        }

        source.AsSpan(sourceOffset, sourceCount).CopyTo(
            Buffer.AsSpan(Count));
        Count = requiredCount;
        return true;
    }

    internal byte[] TakeMessageBytes()
    {
        byte[] message = new byte[Count];
        Buffer.AsSpan(0, Count).CopyTo(message);
        Buffer = [];
        Count = 0;
        return message;
    }

    internal int GetMessageByteCount()
    {
        return Count;
    }

    internal int GetMessageByteCapacity()
    {
        return Buffer.Length;
    }

    internal void ClearMessageBytes()
    {
        Buffer = [];
        Count = 0;
    }
}

internal class WebSocketReassembledReceiveConsumer :
    IWebSocketInboundFrameConsumer
{
    WebSocketStreamingReceiveConsumer StreamingConsumer;
    WebSocketEventQueue Events;
    WebSocketReassemblyBuffer MessageBuffer;
    int MaximumOwnedPayloadBytes;
    int OwnedPayloadCount;
    bool MessageActive;
    WebSocketMessageType MessageType;
    WebSocketInboundFrameChunk? PendingAcceptedChunk;
    WebSocketEvent? PendingCompletedEvent;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    WebSocketError FailureError = new WebSocketError();

    internal WebSocketReassembledReceiveConsumer(
        WebSocketConnectionOptions options,
        WebSocketExtensionPipeline? extensionPipeline)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (options.DeliveryMode != WebSocketMessageDeliveryMode.Reassembled ||
            extensionPipeline == null)
        {
            throw new WebSocketStateException();
        }

        WebSocketConnectionOptions streamingOptions =
            CreateStreamingOptions(options);
        StreamingConsumer = new WebSocketStreamingReceiveConsumer(
            streamingOptions,
            extensionPipeline);
        Events = new WebSocketEventQueue(options.MaximumQueuedEvents);
        MaximumOwnedPayloadBytes = options.MaximumBufferedInputBytes;
        int maximumMessageBufferCount =
            options.MaximumDecompressedMessagePayloadLength;
        if (maximumMessageBufferCount > MaximumOwnedPayloadBytes)
        {
            maximumMessageBufferCount = MaximumOwnedPayloadBytes;
        }

        MessageBuffer = new WebSocketReassemblyBuffer(
            maximumMessageBufferCount);
    }

    public WebSocketInboundFrameCapacity GetInboundFrameCapacity(
        WebSocketFrameHeader header,
        bool firstChunk)
    {
        if (Failed)
        {
            return CreateFailureCapacity();
        }

        if (header == null)
        {
            SetStateFailure(
                "reassembly-frame-header-invalid",
                "The reassembly receiver requires a valid frame header.");
            return CreateFailureCapacity();
        }

        if (PendingAcceptedChunk != null)
        {
            WebSocketInboundFrameCapacity pending =
                new WebSocketInboundFrameCapacity();
            pending.Status = WebSocketParseStatus.NeedEventCapacity;
            return pending;
        }

        SynchronizeStreamingCapacity();

        if (IsStandardDataOpcode(header.Opcode) &&
            !StreamingConsumer.GetApplicationDataSuppressed() &&
            header.PayloadLength != 0 &&
            OwnedPayloadCount >= MaximumOwnedPayloadBytes)
        {
            if (Events.GetEventCount() != 0 ||
                PendingCompletedEvent != null)
            {
                WebSocketInboundFrameCapacity blocked =
                    new WebSocketInboundFrameCapacity();
                blocked.Status = WebSocketParseStatus.NeedEventCapacity;
                return blocked;
            }

            SetLimitFailure(
                "reassembly-input-buffer-limit-exceeded",
                "The reassembled message exceeds the configured input buffer limit.");
            return CreateFailureCapacity();
        }

        bool publicEventRequired =
            header.Opcode == WebSocketOpcode.Ping ||
            header.Opcode == WebSocketOpcode.Pong ||
            header.Opcode == WebSocketOpcode.Close ||
            (IsStandardDataOpcode(header.Opcode) &&
                header.Final &&
                !StreamingConsumer.GetApplicationDataSuppressed());
        if (publicEventRequired &&
            Events.GetEventAvailableCapacity() == 0)
        {
            WebSocketInboundFrameCapacity blocked =
                new WebSocketInboundFrameCapacity();
            blocked.Status = WebSocketParseStatus.NeedEventCapacity;
            return blocked;
        }

        return StreamingConsumer.GetInboundFrameCapacity(header, firstChunk);
    }

    public WebSocketInboundFrameConsumeResult ConsumeInboundFrameChunk(
        WebSocketInboundFrameChunk chunk)
    {
        if (Failed)
        {
            return CreateFailureConsumeResult();
        }

        if (PendingAcceptedChunk != null)
        {
            if (!object.ReferenceEquals(PendingAcceptedChunk, chunk) ||
                PendingCompletedEvent == null)
            {
                SetStateFailure(
                    "reassembly-pending-chunk-invalid",
                    "A different frame chunk was supplied while message delivery was pending.");
                return CreateFailureConsumeResult();
            }

            if (!Events.TryEnqueueEvent(PendingCompletedEvent))
            {
                return CreateBackpressureResult();
            }

            PendingAcceptedChunk = null;
            PendingCompletedEvent = null;
            return new WebSocketInboundFrameConsumeResult();
        }

        SynchronizeStreamingCapacity();

        while (true)
        {
            WebSocketInboundFrameConsumeResult streamed =
                StreamingConsumer.ConsumeInboundFrameChunk(chunk);
            if (StreamingConsumer.GetApplicationDataSuppressed())
            {
                CancelReassembledApplicationData();
            }

            int streamingEventCount =
                StreamingConsumer.GetStreamingEventCount();
            if (!ProcessStreamingEvents())
            {
                return CreateFailureConsumeResult();
            }

            SynchronizeStreamingCapacity();

            if (streamed.HasError)
            {
                SetFailure(streamed.Status, streamed.Error);
                return CreateFailureConsumeResult();
            }

            if (PendingCompletedEvent != null)
            {
                if (streamed.Accepted)
                {
                    PendingAcceptedChunk = chunk;
                }

                return CreateBackpressureResult();
            }

            if (streamed.Accepted)
            {
                return streamed;
            }

            if (streamed.Status ==
                WebSocketParseStatus.NeedOutputCapacity)
            {
                return streamed;
            }

            if (streamed.Status != WebSocketParseStatus.NeedEventCapacity)
            {
                SetStateFailure(
                    "reassembly-stream-status-invalid",
                    "The validated streaming receiver returned an inconsistent status.");
                return CreateFailureConsumeResult();
            }

            if (Events.GetEventAvailableCapacity() == 0)
            {
                return streamed;
            }

            if (OwnedPayloadCount >= MaximumOwnedPayloadBytes &&
                streamingEventCount == 0)
            {
                SetLimitFailure(
                    "reassembly-input-buffer-limit-exceeded",
                    "The reassembled message exceeds the configured input buffer limit.");
                return CreateFailureConsumeResult();
            }
        }
    }

    public void ResetInboundFrameConsumer()
    {
        StreamingConsumer.ResetInboundFrameConsumer();
        Events.ClearEvents();
        MessageBuffer.ClearMessageBytes();
        MessageActive = false;
        MessageType = WebSocketMessageType.Text;
        PendingAcceptedChunk = null;
        PendingCompletedEvent = null;
        OwnedPayloadCount = 0;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureError = new WebSocketError();
    }

    public void HandleInboundFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        StreamingConsumer.HandleInboundFailure(status, error);
        ProcessStreamingEvents();
        SetFailure(
            status,
            StreamingConsumer.GetStreamingFailureHandler()
                .GetConnectionFailureError());
    }

    internal WebSocketEvent? ReadReassembledEvent()
    {
        StreamingConsumer.GetStreamingFailureHandler()
            .SynchronizeFailureHandling();
        ProcessStreamingEvents();
        SynchronizeStreamingCapacity();
        SynchronizePendingCompletedEvent();
        WebSocketEvent? connectionEvent = Events.DequeueEvent();
        if (connectionEvent != null)
        {
            ReleaseOwnedEventPayload(connectionEvent);
        }

        SynchronizeStreamingCapacity();
        SynchronizePendingCompletedEvent();
        StreamingConsumer.GetStreamingFailureHandler()
            .SynchronizeFailureHandling();
        ProcessStreamingEvents();
        return connectionEvent;
    }

    internal int GetReassembledEventCount()
    {
        StreamingConsumer.GetStreamingFailureHandler()
            .SynchronizeFailureHandling();
        ProcessStreamingEvents();
        SynchronizeStreamingCapacity();
        SynchronizePendingCompletedEvent();
        return Events.GetEventCount();
    }

    internal bool TryQueueReassembledTransportEndEvent(
        WebSocketEvent? connectionEvent)
    {
        StreamingConsumer.GetStreamingFailureHandler()
            .SynchronizeFailureHandling();
        ProcessStreamingEvents();
        SynchronizeStreamingCapacity();
        SynchronizePendingCompletedEvent();
        return Events.TryEnqueueEvent(connectionEvent);
    }

    internal int GetOwnedPayloadCount()
    {
        SynchronizeStreamingCapacity();
        return StreamingConsumer.GetOwnedPayloadCount();
    }

    internal int GetBufferedMessageByteCount()
    {
        return MessageBuffer.GetMessageByteCount();
    }

    internal int GetBufferedMessageByteCapacity()
    {
        return MessageBuffer.GetMessageByteCapacity();
    }

    internal WebSocketSendScheduler GetReassembledSendScheduler()
    {
        return StreamingConsumer.GetStreamingSendScheduler();
    }

    internal WebSocketEventQueue GetReassembledStreamingEventQueue()
    {
        return StreamingConsumer.GetStreamingEventQueue();
    }

    internal WebSocketPingPongHandler GetReassembledPingPongHandler()
    {
        return StreamingConsumer.GetStreamingPingPongHandler();
    }

    internal WebSocketHeartbeatManager GetReassembledHeartbeatManager()
    {
        return StreamingConsumer.GetStreamingHeartbeatManager();
    }

    internal WebSocketConnectionStateMachine GetReassembledStateMachine()
    {
        return StreamingConsumer.GetStreamingStateMachine();
    }

    internal WebSocketLocalCloseCoordinator GetReassembledLocalCloseCoordinator()
    {
        return StreamingConsumer.GetStreamingLocalCloseCoordinator();
    }

    internal WebSocketRemoteCloseHandler GetReassembledRemoteCloseHandler()
    {
        return StreamingConsumer.GetStreamingRemoteCloseHandler();
    }

    internal WebSocketConnectionFailureHandler GetReassembledFailureHandler()
    {
        return StreamingConsumer.GetStreamingFailureHandler();
    }

    internal long GetCompletedReassembledMessageCount()
    {
        return StreamingConsumer.GetCompletedStreamingMessageCount();
    }

    bool ProcessStreamingEvents()
    {
        WebSocketEvent? connectionEvent =
            StreamingConsumer.ReadStreamingEvent();
        while (connectionEvent != null)
        {
            if (!ProcessStreamingEvent(connectionEvent))
            {
                return false;
            }

            connectionEvent = StreamingConsumer.ReadStreamingEvent();
        }

        return true;
    }

    bool ProcessStreamingEvent(WebSocketEvent connectionEvent)
    {
        if (connectionEvent.Type == WebSocketEventType.Ping ||
            connectionEvent.Type == WebSocketEventType.Pong ||
            connectionEvent.Type == WebSocketEventType.PingAcknowledged)
        {
            RetainOwnedEventPayload(connectionEvent);
            if (!Events.TryEnqueueEvent(connectionEvent))
            {
                PendingCompletedEvent = connectionEvent;
            }

            return true;
        }

        if (connectionEvent.Type == WebSocketEventType.Close)
        {
            RetainOwnedEventPayload(connectionEvent);
            if (!Events.TryEnqueueEvent(connectionEvent))
            {
                PendingCompletedEvent = connectionEvent;
            }

            return true;
        }

        if (connectionEvent.Type == WebSocketEventType.Error)
        {
            RetainOwnedEventPayload(connectionEvent);
            if (!Events.TryEnqueueEvent(connectionEvent))
            {
                PendingCompletedEvent = connectionEvent;
            }

            return true;
        }

        if (connectionEvent.Type == WebSocketEventType.MessageStart)
        {
            if (MessageActive || !connectionEvent.HasMessageType)
            {
                SetStateFailure(
                    "reassembly-message-start-invalid",
                    "The reassembly receiver observed an invalid message start.");
                return false;
            }

            MessageBuffer.ClearMessageBytes();
            MessageActive = true;
            MessageType = connectionEvent.MessageType;
            return true;
        }

        if (connectionEvent.Type == WebSocketEventType.MessageData)
        {
            if (!MessageActive ||
                !connectionEvent.HasMessageType ||
                connectionEvent.MessageType != MessageType)
            {
                SetStateFailure(
                    "reassembly-message-data-invalid",
                    "The reassembly receiver observed invalid message data.");
                return false;
            }

            if (!MessageBuffer.AppendMessageBytes(
                connectionEvent.Payload,
                connectionEvent.PayloadOffset,
                connectionEvent.PayloadCount))
            {
                SetLimitFailure(
                    "reassembly-message-payload-limit-exceeded",
                    "The reassembled message exceeds the configured payload limit.");
                return false;
            }

            OwnedPayloadCount =
                OwnedPayloadCount + connectionEvent.PayloadCount;

            return true;
        }

        if (connectionEvent.Type == WebSocketEventType.MessageEnd)
        {
            if (!MessageActive ||
                !connectionEvent.HasMessageType ||
                connectionEvent.MessageType != MessageType)
            {
                SetStateFailure(
                    "reassembly-message-end-invalid",
                    "The reassembly receiver observed an invalid message end.");
                return false;
            }

            WebSocketEvent completed = new WebSocketEvent();
            completed.Type = WebSocketEventType.MessageData;
            completed.HasMessageType = true;
            completed.MessageType = MessageType;
            completed.Payload = MessageBuffer.TakeMessageBytes();
            completed.PayloadOffset = 0;
            completed.PayloadCount = completed.Payload.Length;
            completed.First = true;
            completed.Final = true;
            MessageActive = false;
            if (!Events.TryEnqueueEvent(completed))
            {
                PendingCompletedEvent = completed;
            }

            return true;
        }

        SetStateFailure(
            "reassembly-event-type-invalid",
            "The validated streaming receiver returned a non-message event.");
        return false;
    }

    void CancelReassembledApplicationData()
    {
        OwnedPayloadCount = OwnedPayloadCount -
            MessageBuffer.GetMessageByteCount();
        MessageBuffer.ClearMessageBytes();
        MessageActive = false;
        MessageType = WebSocketMessageType.Text;
    }

    void SynchronizePendingCompletedEvent()
    {
        if (PendingCompletedEvent != null &&
            PendingAcceptedChunk == null &&
            Events.TryEnqueueEvent(PendingCompletedEvent))
        {
            PendingCompletedEvent = null;
        }
    }

    void SynchronizeStreamingCapacity()
    {
        StreamingConsumer.SetExternalOwnedPayloadCount(
            OwnedPayloadCount);
    }

    void RetainOwnedEventPayload(WebSocketEvent connectionEvent)
    {
        if (connectionEvent.PayloadCount >
            MaximumOwnedPayloadBytes - OwnedPayloadCount)
        {
            throw new WebSocketStateException();
        }

        OwnedPayloadCount =
            OwnedPayloadCount + connectionEvent.PayloadCount;
    }

    void ReleaseOwnedEventPayload(WebSocketEvent connectionEvent)
    {
        OwnedPayloadCount =
            OwnedPayloadCount - connectionEvent.PayloadCount;
        if (OwnedPayloadCount < 0)
        {
            throw new WebSocketStateException();
        }
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
        ReportReassemblyFailure(
            WebSocketParseStatus.LimitRejected,
            error);
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
        ReportReassemblyFailure(
            WebSocketParseStatus.ProtocolFailure,
            error);
    }

    void ReportReassemblyFailure(
        WebSocketParseStatus status,
        WebSocketError error)
    {
        SetFailure(status, error);
        StreamingConsumer.HandleInboundFailure(status, error);
        WebSocketEvent? connectionEvent =
            StreamingConsumer.ReadStreamingEvent();
        while (connectionEvent != null)
        {
            if (connectionEvent.Type == WebSocketEventType.Error)
            {
                RetainOwnedEventPayload(connectionEvent);
                if (!Events.TryEnqueueEvent(connectionEvent))
                {
                    PendingCompletedEvent = connectionEvent;
                }
            }

            connectionEvent = StreamingConsumer.ReadStreamingEvent();
        }
    }

    void SetFailure(WebSocketParseStatus status, WebSocketError error)
    {
        Failed = true;
        FailureStatus = status;
        FailureError = WebSocketFrameCodec.CopyError(error);
        StreamingConsumer.GetStreamingPingPongHandler()
            .ClearTrackedPingState();
        OwnedPayloadCount = OwnedPayloadCount -
            MessageBuffer.GetMessageByteCount();
        MessageBuffer.ClearMessageBytes();
        MessageActive = false;
        if (PendingCompletedEvent == null ||
            PendingCompletedEvent.Type != WebSocketEventType.Error)
        {
            if (PendingCompletedEvent != null)
            {
                ReleaseOwnedEventPayload(PendingCompletedEvent);
            }

            PendingAcceptedChunk = null;
            PendingCompletedEvent = null;
        }
        else
        {
            PendingAcceptedChunk = null;
        }
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

    static WebSocketConnectionOptions CreateStreamingOptions(
        WebSocketConnectionOptions source)
    {
        WebSocketConnectionOptions copy = new WebSocketConnectionOptions();
        copy.Role = source.Role;
        copy.Transport = source.Transport;
        copy.DeliveryMode = WebSocketMessageDeliveryMode.Streaming;
        copy.HasSelectedSubprotocol = source.HasSelectedSubprotocol;
        copy.SelectedSubprotocol = source.SelectedSubprotocol;
        copy.Extensions = source.Extensions;
        copy.NegotiatedExtensions = source.NegotiatedExtensions;
        copy.HasPerMessageDeflate = source.HasPerMessageDeflate;
        copy.PerMessageDeflate = source.PerMessageDeflate;
        copy.MaximumFramePayloadLength = source.MaximumFramePayloadLength;
        copy.MaximumMessagePayloadLength = source.MaximumMessagePayloadLength;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        copy.MaximumFragmentCount = source.MaximumFragmentCount;
        copy.MaximumBufferedInputBytes = source.MaximumBufferedInputBytes;
        copy.MaximumBufferedOutputBytes = source.MaximumBufferedOutputBytes;
        copy.MaximumQueuedEvents = source.MaximumQueuedEvents;
        copy.AutomaticFramePayloadLength = source.AutomaticFramePayloadLength;
        copy.AutomaticPong = source.AutomaticPong;
        copy.AutomaticCloseResponse = source.AutomaticCloseResponse;
        copy.CloseTimeoutMilliseconds = source.CloseTimeoutMilliseconds;
        copy.Heartbeat = source.Heartbeat;
        copy.HasMaskKeySource = source.HasMaskKeySource;
        copy.MaskKeySource = source.MaskKeySource;
        return copy;
    }

    static bool IsStandardDataOpcode(WebSocketOpcode opcode)
    {
        return opcode == WebSocketOpcode.Continuation ||
            opcode == WebSocketOpcode.Text ||
            opcode == WebSocketOpcode.Binary;
    }
}
