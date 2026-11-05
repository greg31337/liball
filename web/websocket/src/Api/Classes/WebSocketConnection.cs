using System;

namespace websocket;

/// <summary>
/// Composes negotiated framing, extensions, messages, controls, closing, and connection snapshots.
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
    internal WebSocketConnectionOptions AppliedOptions;
    internal WebSocketExtensionPipeline ExtensionPipeline;
    internal IWebSocketInboundFrameConsumer InboundConsumer;
    internal WebSocketStreamingReceiveConsumer? StreamingConsumer;
    internal WebSocketReassembledReceiveConsumer? ReassembledConsumer;
    internal WebSocketInboundFramePump InboundPump;
    internal WebSocketWholeMessageWriter WholeMessageWriter;
    internal WebSocketStreamingMessageWriter StreamingMessageWriter;
    internal WebSocketSendScheduler SendScheduler;
    internal WebSocketPingPongHandler PingPongHandler;
    internal WebSocketHeartbeatManager HeartbeatManager;
    internal WebSocketConnectionStateMachine StateMachine;
    internal WebSocketLocalCloseCoordinator LocalCloseCoordinator;
    internal WebSocketRemoteCloseHandler RemoteCloseHandler;
    internal WebSocketConnectionFailureHandler FailureHandler;
    internal WebSocketConnectionInformationProvider InformationProvider;
    internal WebSocketSendRequest? PendingWholeRequest;
    internal byte[][] PendingWholeFrames = [];
    internal int PendingWholeFrameIndex;
    internal WebSocketSendChunk? PendingStreamingChunk;
    internal byte[] PendingStreamingFrame = [];
    internal bool PendingStreamingMessageEnded;
    internal long ObservedReceivedMessageCount;
    internal bool TransportEndEventCreated;
    internal WebSocketEvent? PendingTransportEndEvent;

    /// <summary>
    /// Initializes an open connection from an accepted handshake and connection options.
    /// </summary>
    /// <param name="options">The role, negotiated extensions, behavior, and finite connection limits.</param>
    /// <param name="handshake">The accepted opening-handshake result.</param>
    /// <exception cref="WebSocketStateException">Thrown when the handshake and options cannot initialize one connection.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a configured connection limit is unsafe.</exception>
    /// <exception cref="WebSocketExtensionException">Thrown when copied compression settings are invalid.</exception>
    public WebSocketConnection(
        WebSocketConnectionOptions options,
        WebSocketHandshakeResult handshake)
    {
        if (options == null || handshake == null)
        {
            throw new WebSocketStateException();
        }

        AppliedOptions = HandshakeConnectionOptionsFactory.CreateOptions(
            options.Role,
            handshake,
            options);
        ExtensionPipeline = new WebSocketExtensionPipeline(
            AppliedOptions.Extensions,
            AppliedOptions.NegotiatedExtensions);

        WebSocketEventQueue streamingEvents;
        if (AppliedOptions.DeliveryMode ==
            WebSocketMessageDeliveryMode.Streaming)
        {
            StreamingConsumer = new WebSocketStreamingReceiveConsumer(
                AppliedOptions,
                ExtensionPipeline);
            InboundConsumer = StreamingConsumer;
            streamingEvents = StreamingConsumer.GetStreamingEventQueue();
            SendScheduler =
                StreamingConsumer.GetStreamingSendScheduler();
            PingPongHandler =
                StreamingConsumer.GetStreamingPingPongHandler();
            HeartbeatManager =
                StreamingConsumer.GetStreamingHeartbeatManager();
            StateMachine =
                StreamingConsumer.GetStreamingStateMachine();
            LocalCloseCoordinator =
                StreamingConsumer.GetStreamingLocalCloseCoordinator();
            RemoteCloseHandler =
                StreamingConsumer.GetStreamingRemoteCloseHandler();
            FailureHandler =
                StreamingConsumer.GetStreamingFailureHandler();
        }
        else
        {
            ReassembledConsumer =
                new WebSocketReassembledReceiveConsumer(
                    AppliedOptions,
                    ExtensionPipeline);
            InboundConsumer = ReassembledConsumer;
            streamingEvents = ReassembledConsumer
                .GetReassembledStreamingEventQueue();
            SendScheduler =
                ReassembledConsumer.GetReassembledSendScheduler();
            PingPongHandler =
                ReassembledConsumer.GetReassembledPingPongHandler();
            HeartbeatManager =
                ReassembledConsumer.GetReassembledHeartbeatManager();
            StateMachine =
                ReassembledConsumer.GetReassembledStateMachine();
            LocalCloseCoordinator = ReassembledConsumer
                .GetReassembledLocalCloseCoordinator();
            RemoteCloseHandler = ReassembledConsumer
                .GetReassembledRemoteCloseHandler();
            FailureHandler =
                ReassembledConsumer.GetReassembledFailureHandler();
        }

        WebSocketFrameCodec frameCodec = new WebSocketFrameCodec(
            AppliedOptions.Role,
            AppliedOptions.MaximumFramePayloadLength,
            AppliedOptions.NegotiatedExtensions);
        InboundPump = new WebSocketInboundFramePump(
            frameCodec,
            InboundConsumer);
        WholeMessageWriter = new WebSocketWholeMessageWriter(
            AppliedOptions,
            ExtensionPipeline);
        StreamingMessageWriter = new WebSocketStreamingMessageWriter(
            AppliedOptions,
            ExtensionPipeline);
        InformationProvider =
            new WebSocketConnectionInformationProvider(
                AppliedOptions,
                handshake,
                StateMachine,
                SendScheduler,
                streamingEvents,
                HeartbeatManager,
                LocalCloseCoordinator,
                RemoteCloseHandler);
    }

    /// <summary>
    /// Queues one complete application message for sending.
    /// </summary>
    /// <param name="request">The complete message send request.</param>
    /// <returns>The send status, including output-capacity pressure or state rejection.</returns>
    public WebSocketParseStatus SendWholeMessage(
        WebSocketSendRequest request)
    {
        FailureHandler.SynchronizeFailureHandling();
        if (PendingWholeRequest != null)
        {
            if (!object.ReferenceEquals(PendingWholeRequest, request))
            {
                return WebSocketParseStatus.NeedOutputCapacity;
            }

            return ResumePendingWholeMessage();
        }

        if (PendingStreamingChunk != null)
        {
            return WebSocketParseStatus.NeedOutputCapacity;
        }

        if (StateMachine.State != WebSocketConnectionState.Open ||
            !LocalCloseCoordinator.CanSendApplicationData() ||
            StreamingMessageWriter.HasActiveStreamingMessage())
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        WebSocketWholeMessageWriteResult encoded =
            WholeMessageWriter.EncodeWholeMessage(request);
        if (!encoded.Accepted)
        {
            HandleWholeWriterFailure(encoded);
            return encoded.Status;
        }

        PendingWholeRequest = request;
        PendingWholeFrames = encoded.EncodedFrames;
        PendingWholeFrameIndex = 0;
        return ResumePendingWholeMessage();
    }

    /// <summary>
    /// Queues one chunk of a streaming application message for sending.
    /// </summary>
    /// <param name="chunk">The streaming message chunk.</param>
    /// <returns>The send status, including output-capacity pressure or state rejection.</returns>
    public WebSocketParseStatus SendMessageChunk(WebSocketSendChunk chunk)
    {
        FailureHandler.SynchronizeFailureHandling();
        if (PendingStreamingChunk != null)
        {
            if (!object.ReferenceEquals(PendingStreamingChunk, chunk))
            {
                return WebSocketParseStatus.NeedOutputCapacity;
            }

            return ResumePendingStreamingMessage();
        }

        if (PendingWholeRequest != null)
        {
            return WebSocketParseStatus.NeedOutputCapacity;
        }

        if (StateMachine.State != WebSocketConnectionState.Open ||
            !LocalCloseCoordinator.CanSendApplicationData())
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        WebSocketStreamingMessageWriteResult encoded =
            StreamingMessageWriter.EncodeMessageChunk(chunk);
        if (!encoded.Accepted)
        {
            HandleStreamingWriterFailure(encoded);
            return encoded.Status;
        }

        PendingStreamingChunk = chunk;
        PendingStreamingFrame = encoded.EncodedFrame;
        PendingStreamingMessageEnded = encoded.MessageEnded;
        return ResumePendingStreamingMessage();
    }

    /// <summary>
    /// Reads the next queued connection event when one is available.
    /// </summary>
    /// <returns>The next event, or <c>null</c> when the event queue is empty.</returns>
    public WebSocketEvent? ReadNextEvent()
    {
        SynchronizeTransportEndEvent();
        WebSocketEvent? connectionEvent;
        if (ReassembledConsumer != null)
        {
            connectionEvent =
                ReassembledConsumer.ReadReassembledEvent();
        }
        else if (StreamingConsumer != null)
        {
            connectionEvent = StreamingConsumer.ReadStreamingEvent();
        }
        else
        {
            throw new WebSocketStateException();
        }

        SynchronizeReceivedMessageCount();
        SynchronizeTransportEndEvent();
        return connectionEvent;
    }

    /// <summary>
    /// Gets an independent snapshot of current connection information.
    /// </summary>
    /// <returns>The connection information snapshot.</returns>
    public WebSocketConnectionInfo GetConnectionInfo()
    {
        FailureHandler.SynchronizeFailureHandling();
        SynchronizeTransportEndEvent();
        SynchronizeReceivedMessageCount();
        WebSocketConnectionInfo information =
            InformationProvider.GetConnectionInformation();
        if (ReassembledConsumer != null)
        {
            information.PendingEventCount =
                ReassembledConsumer.GetReassembledEventCount();
        }

        return information;
    }

    internal WebSocketInboundFramePumpResult ReceiveConnectionBytes(
        ReadOnlySpan<byte> source)
    {
        WebSocketInboundFramePumpResult result =
            InboundPump.PumpInboundFrames(source);
        InformationProvider.RecordInboundProgress(
            result.FrameCount,
            0);
        SynchronizeReceivedMessageCount();
        FailureHandler.SynchronizeFailureHandling();
        return result;
    }

    internal int WriteConnectionBytes(Span<byte> destination)
    {
        int written = SendScheduler.WritePendingBytes(destination);
        LocalCloseCoordinator.SynchronizeLocalCloseSent();
        FailureHandler.SynchronizeFailureHandling();
        return written;
    }

    internal int GetPendingConnectionWriteCount()
    {
        return SendScheduler.GetPendingByteCount();
    }

    internal int GetPendingConnectionEventCount()
    {
        SynchronizeTransportEndEvent();
        if (ReassembledConsumer != null)
        {
            return ReassembledConsumer.GetReassembledEventCount();
        }

        if (StreamingConsumer != null)
        {
            return StreamingConsumer.GetStreamingEventCount();
        }

        throw new WebSocketStateException();
    }

    internal WebSocketControlSendResult SendConnectionPing(
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        if (StateMachine.State != WebSocketConnectionState.Open)
        {
            return CreateControlStateRejection();
        }

        return PingPongHandler.SendPing(
            payload,
            payloadOffset,
            payloadCount);
    }

    internal WebSocketControlSendResult SendConnectionPong(
        byte[]? payload,
        int payloadOffset,
        int payloadCount)
    {
        if (StateMachine.State != WebSocketConnectionState.Open)
        {
            return CreateControlStateRejection();
        }

        return PingPongHandler.SendPong(
            payload,
            payloadOffset,
            payloadCount);
    }

    internal WebSocketLocalCloseResult StartConnectionClose(
        WebSocketCloseInfo? close)
    {
        ClearPendingApplicationSend();
        return LocalCloseCoordinator.StartLocalClose(close);
    }

    internal WebSocketParseStatus AdvanceConnectionTime(
        long elapsedMilliseconds)
    {
        WebSocketConnectionTransitionResult transition =
            StateMachine.AdvanceElapsedMilliseconds(
                elapsedMilliseconds);
        if (transition.HasError)
        {
            if (StateMachine.HasFailure)
            {
                FailureHandler.HandleConnectionFailure(
                    transition.Status,
                    transition.Error);
                if (StateMachine.State ==
                    WebSocketConnectionState.Failed)
                {
                    InboundPump.LatchConnectionFailure(
                        transition.Status,
                        transition.Error);
                }
            }

            return transition.Status;
        }

        WebSocketHeartbeatAdvanceResult heartbeat =
            HeartbeatManager.AdvanceHeartbeatTime(
                elapsedMilliseconds);
        if (heartbeat.HasError)
        {
            FailureHandler.HandleConnectionFailure(
                heartbeat.Status,
                heartbeat.Error);
        }

        return heartbeat.Status;
    }

    internal WebSocketConnectionTransitionResult EndConnectionInput()
    {
        WebSocketConnectionTransitionResult result =
            StateMachine.EndTransport();
        HeartbeatManager.StopHeartbeat();
        HandleTerminalTransition(result);
        return result;
    }

    internal WebSocketConnectionTransitionResult ResetConnectionTransport()
    {
        WebSocketConnectionTransitionResult result =
            StateMachine.ResetTransport();
        HeartbeatManager.StopHeartbeat();
        SendScheduler.ClearPendingBytes();
        HandleTerminalTransition(result);
        return result;
    }

    internal WebSocketConnectionTransitionResult AbortConnectionTransport()
    {
        WebSocketConnectionTransitionResult result =
            StateMachine.AbortConnection();
        HeartbeatManager.StopHeartbeat();
        SendScheduler.ClearPendingBytes();
        HandleTerminalTransition(result);
        return result;
    }

    internal WebSocketParseStatus ResumePendingWholeMessage()
    {
        if (PendingWholeRequest == null ||
            StateMachine.State != WebSocketConnectionState.Open ||
            !LocalCloseCoordinator.CanSendApplicationData())
        {
            ClearPendingApplicationSend();
            return WebSocketParseStatus.ProtocolFailure;
        }

        while (PendingWholeFrameIndex < PendingWholeFrames.Length)
        {
            if (!SendScheduler.TryQueueDataFrame(
                PendingWholeFrames[PendingWholeFrameIndex]))
            {
                return WebSocketParseStatus.NeedOutputCapacity;
            }

            InformationProvider.RecordOutboundProgress(1, 0);
            PendingWholeFrameIndex = PendingWholeFrameIndex + 1;
        }

        InformationProvider.RecordOutboundProgress(0, 1);
        PendingWholeRequest = null;
        PendingWholeFrames = [];
        PendingWholeFrameIndex = 0;
        return WebSocketParseStatus.Complete;
    }

    internal WebSocketParseStatus ResumePendingStreamingMessage()
    {
        if (PendingStreamingChunk == null ||
            StateMachine.State != WebSocketConnectionState.Open ||
            !LocalCloseCoordinator.CanSendApplicationData())
        {
            ClearPendingApplicationSend();
            return WebSocketParseStatus.ProtocolFailure;
        }

        if (!SendScheduler.TryQueueDataFrame(PendingStreamingFrame))
        {
            return WebSocketParseStatus.NeedOutputCapacity;
        }

        int messageCount = 0;
        if (PendingStreamingMessageEnded)
        {
            messageCount = 1;
        }

        InformationProvider.RecordOutboundProgress(1, messageCount);
        PendingStreamingChunk = null;
        PendingStreamingFrame = [];
        PendingStreamingMessageEnded = false;
        return WebSocketParseStatus.Complete;
    }

    internal void SynchronizeReceivedMessageCount()
    {
        long completedCount;
        if (ReassembledConsumer != null)
        {
            completedCount = ReassembledConsumer
                .GetCompletedReassembledMessageCount();
        }
        else if (StreamingConsumer != null)
        {
            completedCount = StreamingConsumer
                .GetCompletedStreamingMessageCount();
        }
        else
        {
            throw new WebSocketStateException();
        }

        long remaining = completedCount - ObservedReceivedMessageCount;
        if (remaining < 0)
        {
            throw new WebSocketStateException();
        }

        while (remaining > int.MaxValue)
        {
            InformationProvider.RecordInboundProgress(0, int.MaxValue);
            ObservedReceivedMessageCount =
                ObservedReceivedMessageCount + int.MaxValue;
            remaining = remaining - int.MaxValue;
        }

        if (remaining != 0)
        {
            InformationProvider.RecordInboundProgress(
                0,
                (int)remaining);
            ObservedReceivedMessageCount = completedCount;
        }
    }

    internal void HandleWholeWriterFailure(
        WebSocketWholeMessageWriteResult result)
    {
        if (WholeMessageWriter.HasWholeMessageWriterFailed())
        {
            FailureHandler.HandleConnectionFailure(
                result.Status,
                WholeMessageWriter.GetWholeMessageWriterError());
            ClearPendingApplicationSend();
        }
    }

    internal void HandleStreamingWriterFailure(
        WebSocketStreamingMessageWriteResult result)
    {
        if (StreamingMessageWriter.HasStreamingMessageWriterFailed())
        {
            FailureHandler.HandleConnectionFailure(
                result.Status,
                StreamingMessageWriter.GetStreamingMessageWriterError());
            ClearPendingApplicationSend();
        }
    }

    internal void HandleTerminalTransition(
        WebSocketConnectionTransitionResult result)
    {
        ClearPendingApplicationSend();
        ExtensionPipeline.ReleasePipeline();
        if (result.HasError && StateMachine.HasFailure)
        {
            FailureHandler.HandleConnectionFailure(
                result.Status,
                result.Error);
            InboundPump.LatchConnectionFailure(
                result.Status,
                result.Error);
        }

        if (!TransportEndEventCreated)
        {
            WebSocketEvent connectionEvent = new WebSocketEvent();
            connectionEvent.Type = WebSocketEventType.TransportEnd;
            connectionEvent.First = true;
            connectionEvent.Final = true;
            PendingTransportEndEvent = connectionEvent;
            TransportEndEventCreated = true;
        }

        SynchronizeTransportEndEvent();
    }

    internal bool SynchronizeTransportEndEvent()
    {
        if (PendingTransportEndEvent == null)
        {
            return false;
        }

        bool queued;
        if (ReassembledConsumer != null)
        {
            queued = ReassembledConsumer
                .TryQueueReassembledTransportEndEvent(
                    PendingTransportEndEvent);
        }
        else if (StreamingConsumer != null)
        {
            queued = StreamingConsumer
                .TryQueueStreamingTransportEndEvent(
                    PendingTransportEndEvent);
        }
        else
        {
            throw new WebSocketStateException();
        }

        if (queued)
        {
            PendingTransportEndEvent = null;
        }

        return queued;
    }

    internal void ClearPendingApplicationSend()
    {
        PendingWholeRequest = null;
        PendingWholeFrames = [];
        PendingWholeFrameIndex = 0;
        PendingStreamingChunk = null;
        PendingStreamingFrame = [];
        PendingStreamingMessageEnded = false;
    }

    internal static WebSocketControlSendResult CreateControlStateRejection()
    {
        WebSocketControlSendResult result =
            new WebSocketControlSendResult();
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.Accepted = false;
        result.HasError = true;
        result.Error.Kind = WebSocketErrorKind.State;
        result.Error.Identifier = "connection-control-state-invalid";
        result.Error.Diagnostic =
            "The connection state does not permit an explicit control send.";
        return result;
    }
}
