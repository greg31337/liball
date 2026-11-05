using System;
using System.Collections.Generic;

namespace http;

internal class Http2ConnectionState
{
    internal bool initialized;
    internal bool terminal;
    internal bool sentPreface;
    internal bool receivedClientMagic;
    internal bool receivedPreface;
    internal bool receivedInitialSettings;
    internal bool initialSettingsAcknowledged;
    internal bool ready;
    internal bool unexpectedHttp1;
    internal bool hasUnexpectedHttpVersion;
    internal HttpProtocolVersion unexpectedHttpVersion;
    internal Http2EndpointRole endpointRole;
    internal int maximumBufferedSize;
    internal byte[] bufferedInput = Array.Empty<byte>();
    internal Http2Settings localSettings = new Http2Settings();
    internal Http2Settings remoteSettings = new Http2Settings();
    internal Http2FrameCodec frameCodec = new Http2FrameCodec();
    internal Http2StreamStateMachine streamState =
        new Http2StreamStateMachine();
    internal Http2HeaderBlockState headerBlockState =
        new Http2HeaderBlockState();
    internal Http2MessageCodec messageCodec = new Http2MessageCodec();
    internal Http2FlowControl flowControl = new Http2FlowControl();
    internal List<Http2Settings> pendingLocalSettings =
        new List<Http2Settings>();
    internal List<byte[]> pendingLocalPings = new List<byte[]>();
    internal bool initialLocalSettingsPending;
    internal bool sentGoAway;
    internal bool receivedGoAway;
    internal int sentLastStreamId;
    internal int receivedLastStreamId;
    internal bool hasSentGoAwayErrorCode;
    internal Http2ErrorCode sentGoAwayErrorCode;
    internal uint sentGoAwayErrorCodeValue;
    internal bool isKnownSentGoAwayErrorCode;
    internal byte[] sentGoAwayDebugData = Array.Empty<byte>();
    internal bool hasReceivedGoAwayErrorCode;
    internal Http2ErrorCode receivedGoAwayErrorCode;
    internal uint receivedGoAwayErrorCodeValue;
    internal bool isKnownReceivedGoAwayErrorCode;
    internal byte[] receivedGoAwayDebugData = Array.Empty<byte>();
    internal int gracefulGoAwayStage;
    internal bool discardedInboundHeaderActive;
    internal int discardedInboundHeaderStreamId;
    internal int configuredMaximumFrameSize =
        Http2Constants.DefaultMaximumFrameSize;
    internal bool allowServerPush;
    internal bool acknowledgedLocalPushEnabled = true;
    internal bool enablePriorityUpdate;
    internal bool enableOrigin;
    internal bool enableExtendedConnect;
    internal bool preserveUnknownFrames;
    internal string? initialOrigin;
    internal bool originSetInitialized;
    internal List<string> origins = new List<string>();
    internal int lastProcessedRemoteStreamId;
    internal List<int> outboundFinalResponseStreams = new List<int>();

    internal Http2ConnectionResult Reset(Http2ConnectionOptions options)
    {
        Http2ConnectionResult invalid = ValidateOptions(options);
        if (invalid.Status != HttpOperationStatus.Done)
        {
            return invalid;
        }

        initialized = true;
        terminal = false;
        sentPreface = true;
        receivedClientMagic = false;
        receivedPreface = false;
        receivedInitialSettings = false;
        initialSettingsAcknowledged = false;
        ready = false;
        unexpectedHttp1 = false;
        hasUnexpectedHttpVersion = false;
        unexpectedHttpVersion = HttpProtocolVersion.Http10;
        endpointRole = options.EndpointRole;
        allowServerPush = options.AllowServerPush;
        acknowledgedLocalPushEnabled = true;
        enablePriorityUpdate = options.EnablePriorityUpdate;
        enableOrigin = options.EnableOrigin;
        enableExtendedConnect = options.EnableExtendedConnect;
        preserveUnknownFrames = options.PreserveUnknownFrames;
        initialOrigin = options.InitialOrigin;
        originSetInitialized = false;
        origins = new List<string>();
        lastProcessedRemoteStreamId = 0;
        outboundFinalResponseStreams = new List<int>();
        maximumBufferedSize = options.Limits.MaximumBufferedSize;
        bufferedInput = Array.Empty<byte>();
        streamState.ResetStreams(
            endpointRole, options.MaximumRetainedClosedStreams);
        flowControl.ResetFlowControl();
        headerBlockState.ResetHeaderBlocks(
            endpointRole,
            options.EncoderOptions,
            options.DecoderOptions,
            options.Limits);
        messageCodec = new Http2MessageCodec();
        messageCodec.limits = CopyLimits(options.Limits);
        Http2Frame localFrame = new Http2Frame();
        localFrame.FrameType = Http2FrameType.Settings;
        localFrame.Settings = options.LocalSettings;
        Http2FrameResult normalizedLocal =
            new Http2FrameCodec().SerializeHttp2Frame(localFrame);
        Http2Settings normalizedLocalSettings = options.LocalSettings;
        if (normalizedLocal.Frame != null &&
            normalizedLocal.Frame.Settings != null)
        {
            normalizedLocalSettings = normalizedLocal.Frame.Settings;
        }

        localSettings = CopySettings(normalizedLocalSettings);
        remoteSettings = new Http2Settings();
        streamState.ApplyLocalConcurrencySettings(localSettings);
        pendingLocalSettings = new List<Http2Settings>();
        pendingLocalSettings.Add(CopySettings(localSettings));
        pendingLocalPings = new List<byte[]>();
        initialLocalSettingsPending = true;
        sentGoAway = false;
        receivedGoAway = false;
        sentLastStreamId = 0;
        receivedLastStreamId = 0;
        hasSentGoAwayErrorCode = false;
        sentGoAwayErrorCode = Http2ErrorCode.NoError;
        sentGoAwayErrorCodeValue = 0u;
        isKnownSentGoAwayErrorCode = false;
        sentGoAwayDebugData = Array.Empty<byte>();
        hasReceivedGoAwayErrorCode = false;
        receivedGoAwayErrorCode = Http2ErrorCode.NoError;
        receivedGoAwayErrorCodeValue = 0u;
        isKnownReceivedGoAwayErrorCode = false;
        receivedGoAwayDebugData = Array.Empty<byte>();
        gracefulGoAwayStage = 0;
        discardedInboundHeaderActive = false;
        discardedInboundHeaderStreamId = 0;
        streamState.ApplyLocalWindowSettings(localSettings);
        frameCodec = new Http2FrameCodec();
        frameCodec.directionalMaximumFrameSizes = true;
        configuredMaximumFrameSize = options.Limits.MaximumFrameSize;
        frameCodec.configuredMaximumFrameSize = localSettings.HasMaxFrameSize ?
            localSettings.MaxFrameSize :
            Http2Constants.DefaultMaximumFrameSize;
        frameCodec.peerMaximumFrameSize =
            Http2Constants.DefaultMaximumFrameSize;
        UpdatePriorityIgnoreState();

        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (endpointRole == Http2EndpointRole.Client)
        {
            result.Actions.Add(CreateClientPrefaceAction());
        }

        result.Actions.Add(CreateInitialSettingsAction());
        result.Actions.Add(CreateSettingsTimeoutAction(
            true, localSettings));
        return CompleteResult(result);
    }

    internal Http2ConnectionResult Process(Http2ConnectionInput input)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Input = input;
        if (input == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-connection-input";
            result.ErrorMessage = "An HTTP/2 connection input is required.";
            return CompleteResult(result);
        }

        if (!initialized)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-connection-not-initialized";
            result.ErrorMessage =
                "ResetHttp2Connection must initialize the connection first.";
            return CompleteResult(result);
        }

        if (terminal)
        {
            result.Status = HttpOperationStatus.Terminal;
            result.ErrorCodeName = "http2-connection-terminal";
            result.ErrorMessage = "The HTTP/2 connection is terminal.";
            return CompleteResult(result);
        }

        int sourceCount = 0;
        if (input.HasBytes)
        {
            sourceCount = sourceCount + 1;
        }

        if (input.Frame != null)
        {
            sourceCount = sourceCount + 1;
        }

        if (input.Action != null || input.Request != null ||
            input.Response != null || input.HasStreamId)
        {
            sourceCount = sourceCount + 1;
        }

        if (sourceCount > 1)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "ambiguous-http2-connection-input";
            result.ErrorMessage =
                "One HTTP/2 connection input source can be processed at a time.";
            return CompleteResult(result);
        }

        if (input.Action != null || input.Request != null ||
            input.Response != null || input.HasStreamId)
        {
            return ProcessOutboundInput(result, input);
        }

        if (input.HasBytes)
        {
            if (input.Bytes == null)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "invalid-http2-connection-bytes";
                result.ErrorMessage = "Inbound HTTP/2 bytes are required.";
                return CompleteResult(result);
            }

            if ((long)bufferedInput.Length + input.Bytes.Length >
                maximumBufferedSize)
            {
                result.Consumed = input.Bytes.Length;
                return FailConnection(
                    result,
                    Http2ErrorCode.EnhanceYourCalm,
                    "http2-connection-buffer-limit",
                    "Buffered HTTP/2 connection input exceeds the configured limit.",
                    false);
            }

            AppendBytes(input.Bytes);
            result.Consumed = input.Bytes.Length;
            ProcessBufferedInput(result);
        }
        else if (input.Frame != null)
        {
            ProcessInboundFrame(result, input.Frame);
        }
        else if (bufferedInput.Length != 0)
        {
            ProcessBufferedInput(result);
        }
        else
        {
            result.Status = ready ?
                HttpOperationStatus.Done : HttpOperationStatus.NeedInput;
        }

        if (input.EndOfInput && headerBlockState.inboundActive && !terminal)
        {
            return FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "incomplete-http2-header-block",
                "The peer ended input before completing the active HTTP/2 header block.",
                false);
        }

        if (input.EndOfInput && !ready && !terminal)
        {
            return FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "incomplete-http2-connection-preface",
                "The peer ended input before completing its HTTP/2 connection preface.",
                true);
        }

        if (input.EndOfInput && bufferedInput.Length != 0 && !terminal)
        {
            return FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "incomplete-http2-frame",
                "The peer ended input before completing an HTTP/2 frame.",
                false);
        }

        return CompleteResult(result);
    }

    internal Http2ConnectionResult ProcessOutboundInput(
        Http2ConnectionResult result,
        Http2ConnectionInput input)
    {
        if (input.Request != null && input.Response != null ||
            input.Action != null &&
                (input.Request != null || input.Response != null))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "ambiguous-http2-outbound-operation";
            result.ErrorMessage =
                "Provide one action, request, or response operation at a time.";
            return CompleteResult(result);
        }

        Http2Action? action = input.Action;
        if (input.Request != null || input.Response != null)
        {
            Http2MessageResult message = new Http2MessageResult();
            message.StreamId = input.HasStreamId ? input.StreamId : 0;
            message.EndStream = input.EndStream;
            if (input.Request != null)
            {
                message.MessageKind = HttpMessageKind.Request;
                message.Request = input.Request;
            }
            else
            {
                message.MessageKind = HttpMessageKind.Response;
                message.Response = input.Response;
            }

            action = new Http2Action();
            action.ActionKind = Http2ActionKind.SendMessage;
            action.Message = message;
            action.HasStreamId = input.HasStreamId;
            action.StreamId = input.StreamId;
        }

        if (action == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-outbound-operation";
            result.ErrorMessage =
                "An outbound request, response, or action is required.";
            return CompleteResult(result);
        }

        if (input.HasStreamId)
        {
            if (action.HasStreamId && action.StreamId != input.StreamId)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "http2-action-stream-id-mismatch";
                result.ErrorMessage =
                    "The input and action stream identifiers must match.";
                return CompleteResult(result);
            }

            action.HasStreamId = true;
            action.StreamId = input.StreamId;
        }

        Http2ConnectionResult queued = Queue(action);
        queued.Input = input;
        return queued;
    }

    internal Http2ConnectionResult PrepareOutboundAction(Http2Action action)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (action.ActionKind == Http2ActionKind.SendMessage)
        {
            return PrepareSendMessageAction(action);
        }

        if (action.ActionKind == Http2ActionKind.InitiatePush)
        {
            return PreparePushAction(action);
        }

        if (action.ActionKind == Http2ActionKind.ResetStream)
        {
            return PrepareResetAction(action);
        }

        if (action.ActionKind == Http2ActionKind.SendFrame &&
            action.Frame == null && action.Settings != null)
        {
            Http2Frame settingsFrame = new Http2Frame();
            settingsFrame.FrameType = Http2FrameType.Settings;
            settingsFrame.Settings = action.Settings;
            action.Frame = settingsFrame;
        }

        return result;
    }

    internal Http2ConnectionResult PrepareSendMessageAction(
        Http2Action action)
    {
        if (!ready)
        {
            return RejectOutboundAction(
                "http2-connection-not-ready",
                "HTTP/2 messages cannot be sent before the peer preface is complete.");
        }

        if (action.Frame != null || action.Message == null)
        {
            return RejectOutboundAction(
                "invalid-http2-message-action",
                "A message action requires message data and no prebuilt frame.");
        }

        Http2MessageResult message = action.Message;
        if (message.Request != null && message.Response != null)
        {
            return RejectOutboundAction(
                "ambiguous-http2-message-action",
                "A message action cannot contain both a request and a response.");
        }

        int streamId = ResolveOutboundActionStream(action, message);
        if (streamId < 0 ||
            streamId > Http2Constants.MaximumStreamIdentifier)
        {
            return RejectOutboundAction(
                "invalid-http2-message-stream-id",
                "A message action requires a valid HTTP/2 stream identifier.");
        }

        if (message.Request != null)
        {
            return PrepareRequestHeadAction(action, message, streamId);
        }

        if (message.Response != null)
        {
            return PrepareResponseHeadAction(action, message, streamId);
        }

        if (message.MessageKind == HttpMessageKind.Trailers)
        {
            return PrepareTrailerAction(action, message, streamId);
        }

        if (message.Headers != null && message.Headers.Fields != null &&
            message.Headers.Fields.Count != 0)
        {
            return PrepareEncodedHeadAction(action, message, streamId);
        }

        return PrepareContentAction(action, message, streamId);
    }

    internal Http2ConnectionResult PrepareRequestHeadAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        HttpRequest? request = message.Request;
        if (request == null)
        {
            return RejectOutboundAction(
                "invalid-http2-request",
                "A request message action requires request data.");
        }

        if (endpointRole != Http2EndpointRole.Client)
        {
            return RejectOutboundAction(
                "http2-server-cannot-send-request",
                "A server can send a request only through server push.");
        }

        if (streamId != 0 && streamState.FindStream(streamId) != null)
        {
            return RejectOutboundAction(
                "http2-request-stream-already-exists",
                "A new request requires an unused client stream identifier.");
        }

        if (request.ConnectProtocol != null &&
            (!enableExtendedConnect ||
                !remoteSettings.HasEnableConnectProtocol ||
                !remoteSettings.EnableConnectProtocol))
        {
            return RejectOutboundAction(
                "http2-extended-connect-not-negotiated",
                "Extended CONNECT requires peer SETTINGS_ENABLE_CONNECT_PROTOCOL.");
        }

        Http2MessageResult encoded = messageCodec.EncodeHttp2RequestHeaders(
            streamId, request, message.EndStream);
        if (encoded.Status != HttpOperationStatus.Done)
        {
            return RejectEncodedMessage(encoded);
        }

        SetHeaderMessageAction(action, encoded, streamId);
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PrepareResponseHeadAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        HttpResponse? response = message.Response;
        if (response == null)
        {
            return RejectOutboundAction(
                "invalid-http2-response",
                "A response message action requires response data.");
        }

        if (endpointRole != Http2EndpointRole.Server)
        {
            return RejectOutboundAction(
                "http2-client-cannot-send-response",
                "Only a server can send an HTTP response head.");
        }

        if (streamId <= 0 || streamState.FindStream(streamId) == null)
        {
            return RejectOutboundAction(
                "http2-response-stream-missing",
                "A response requires an existing request or pushed stream.");
        }

        if (ContainsOutboundFinalResponse(streamId))
        {
            return RejectOutboundAction(
                "http2-final-response-already-sent",
                "The stream has already sent its final response head.");
        }

        Http2MessageResult encoded = messageCodec.EncodeHttp2ResponseHeaders(
            streamId, response, message.EndStream);
        if (encoded.Status != HttpOperationStatus.Done)
        {
            return RejectEncodedMessage(encoded);
        }

        SetHeaderMessageAction(action, encoded, streamId);
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PrepareEncodedHeadAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        if (message.MessageKind == HttpMessageKind.Request)
        {
            if (endpointRole != Http2EndpointRole.Client || streamId != 0 &&
                streamState.FindStream(streamId) != null)
            {
                return RejectOutboundAction(
                    "invalid-http2-request-stream",
                    "An encoded request head requires a new client stream.");
            }

            Http2MessageResult decoded = Http2RequestSemantics.Decode(
                streamId,
                message.Headers,
                message.EndStream,
                enableExtendedConnect &&
                    remoteSettings.HasEnableConnectProtocol &&
                    remoteSettings.EnableConnectProtocol,
                messageCodec.limits);
            if (decoded.Status != HttpOperationStatus.Done)
            {
                return RejectEncodedMessage(decoded);
            }

            message.Request = decoded.Request;
        }
        else if (message.MessageKind == HttpMessageKind.Response ||
            message.MessageKind == HttpMessageKind.InformationalResponse)
        {
            if (endpointRole != Http2EndpointRole.Server || streamId <= 0 ||
                streamState.FindStream(streamId) == null ||
                ContainsOutboundFinalResponse(streamId))
            {
                return RejectOutboundAction(
                    "invalid-http2-response-stream",
                    "An encoded response requires an active server response stream.");
            }

            Http2MessageResult decoded = new Http2ResponseSemantics().Decode(
                streamId,
                message.Headers,
                message.EndStream,
                messageCodec.limits);
            if (decoded.Status != HttpOperationStatus.Done)
            {
                return RejectEncodedMessage(decoded);
            }

            message.MessageKind = decoded.MessageKind;
            message.Response = decoded.Response;
        }
        else
        {
            return RejectOutboundAction(
                "invalid-http2-message-kind",
                "Encoded message fields require a request or response kind.");
        }

        message.Status = HttpOperationStatus.Done;
        SetHeaderMessageAction(action, message, streamId);
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PrepareTrailerAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        Http2StreamSnapshot? stream = streamState.FindStream(streamId);
        if (streamId <= 0 || stream == null || !stream.SentHeaders ||
            endpointRole == Http2EndpointRole.Server &&
                !ContainsOutboundFinalResponse(streamId))
        {
            return RejectOutboundAction(
                "http2-trailers-before-message-head",
                "Trailers require an existing local final message head.");
        }

        HttpHeaders trailers = message.Trailers ?? message.Headers;
        Http2MessageResult encoded = messageCodec.EncodeHttp2Trailers(
            streamId, trailers);
        if (encoded.Status != HttpOperationStatus.Done)
        {
            return RejectEncodedMessage(encoded);
        }

        SetHeaderMessageAction(action, encoded, streamId);
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PrepareContentAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        Http2StreamSnapshot? stream = streamState.FindStream(streamId);
        bool directionValid = endpointRole == Http2EndpointRole.Client ?
            message.MessageKind == HttpMessageKind.Request :
            message.MessageKind == HttpMessageKind.Response;
        if (streamId <= 0 || stream == null || !stream.SentHeaders ||
            !directionValid || endpointRole == Http2EndpointRole.Server &&
                !ContainsOutboundFinalResponse(streamId) ||
            message.Content == null)
        {
            return RejectOutboundAction(
                "invalid-http2-content-action",
                "Content requires an active local request or final response stream.");
        }

        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Data;
        frame.StreamId = streamId;
        frame.EndStream = message.EndStream;
        frame.Data = message.Content;
        message.Status = HttpOperationStatus.Done;
        message.StreamId = streamId;
        action.Frame = frame;
        action.HasStreamId = true;
        action.StreamId = streamId;
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PreparePushAction(Http2Action action)
    {
        if (!ready || endpointRole != Http2EndpointRole.Server)
        {
            return RejectOutboundAction(
                "http2-push-role-or-state",
                "Server push requires a ready server connection.");
        }

        if (action.Message == null || action.Frame != null ||
            !action.HasStreamId || action.StreamId <= 0)
        {
            return RejectOutboundAction(
                "invalid-http2-push-action",
                "Push requires a request message and associated stream identifier.");
        }

        if (action.Message.Request != null)
        {
            Http2MessageResult encoded =
                messageCodec.EncodeHttp2RequestHeaders(
                    0, action.Message.Request, true);
            if (encoded.Status != HttpOperationStatus.Done)
            {
                return RejectEncodedMessage(encoded);
            }

            action.Message = encoded;
        }

        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.PushPromise;
        frame.StreamId = action.StreamId;
        frame.EndHeaders = true;
        action.Frame = frame;
        return CreateDoneConnectionResult();
    }

    internal Http2ConnectionResult PrepareResetAction(Http2Action action)
    {
        if (action.Frame != null || !action.HasStreamId ||
            action.StreamId <= 0 || !action.HasErrorCode)
        {
            return RejectOutboundAction(
                "invalid-http2-reset-action",
                "A reset requires a stream identifier, error code, and no prebuilt frame.");
        }

        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.RstStream;
        frame.StreamId = action.StreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Stream;
        frame.ErrorCode = action.ErrorCode;
        frame.ErrorCodeValue = action.ErrorCodeValue;
        frame.IsKnownErrorCode = action.IsKnownErrorCode;
        action.Frame = frame;
        return CreateDoneConnectionResult();
    }

    internal int ResolveOutboundActionStream(
        Http2Action action,
        Http2MessageResult message)
    {
        int streamId = action.HasStreamId ?
            action.StreamId : message.StreamId;
        if (action.HasStreamId && message.StreamId != 0 &&
            action.StreamId != message.StreamId)
        {
            return -1;
        }

        action.HasStreamId = streamId != 0;
        action.StreamId = streamId;
        message.StreamId = streamId;
        return streamId;
    }

    internal void SetHeaderMessageAction(
        Http2Action action,
        Http2MessageResult message,
        int streamId)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Headers;
        frame.StreamId = streamId;
        frame.EndHeaders = true;
        frame.EndStream = message.EndStream;
        action.Message = message;
        action.Frame = frame;
        action.HasStreamId = streamId != 0;
        action.StreamId = streamId;
    }

    internal void RecordOutboundMessageAction(Http2Action action)
    {
        if (action.Frame == null || action.Message == null)
        {
            return;
        }

        action.Message.StreamId = action.Frame.FrameType ==
            Http2FrameType.PushPromise ?
            action.Frame.PromisedStreamId : action.Frame.StreamId;
        if (action.Frame.FrameType == Http2FrameType.Headers &&
            action.Message.MessageKind == HttpMessageKind.Response &&
            !ContainsOutboundFinalResponse(action.Frame.StreamId))
        {
            outboundFinalResponseStreams.Add(action.Frame.StreamId);
        }
    }

    internal bool ContainsOutboundFinalResponse(int streamId)
    {
        int index = 0;
        while (index < outboundFinalResponseStreams.Count)
        {
            if (outboundFinalResponseStreams[index] == streamId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal Http2ConnectionResult RejectEncodedMessage(
        Http2MessageResult message)
    {
        return RejectOutboundAction(
            message.ErrorCodeName ?? "invalid-http2-message",
            message.ErrorMessage ??
                "The HTTP message cannot be encoded for HTTP/2.");
    }

    internal Http2ConnectionResult RejectOutboundAction(
        string errorCodeName,
        string errorMessage)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCodeName = errorCodeName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal Http2ConnectionResult CreateDoneConnectionResult()
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal Http2ConnectionResult Queue(Http2Action action)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        if (!initialized)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-connection-not-initialized";
            result.ErrorMessage =
                "ResetHttp2Connection must initialize the connection first.";
            return CompleteResult(result);
        }

        if (terminal)
        {
            result.Status = HttpOperationStatus.Terminal;
            result.ErrorCodeName = "http2-connection-terminal";
            result.ErrorMessage = "The HTTP/2 connection is terminal.";
            return CompleteResult(result);
        }

        if (action == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-action";
            result.ErrorMessage = "An HTTP/2 action is required.";
            return CompleteResult(result);
        }

        if (action.ActionKind == Http2ActionKind.ReleaseCapacity)
        {
            return Release(
                action.HasStreamId ? action.StreamId : 0,
                action.Capacity);
        }

        Http2ConnectionResult preparation = PrepareOutboundAction(action);
        if (preparation.Status != HttpOperationStatus.Done)
        {
            return CompleteResult(preparation);
        }

        bool automaticGracefulGoAway = false;
        if (action.ActionKind == Http2ActionKind.SendGoAway)
        {
            if (action.Frame == null)
            {
                if (!action.HasLastStreamId && !action.HasErrorCode)
                {
                    if (gracefulGoAwayStage >= 2)
                    {
                        result.Status = HttpOperationStatus.Rejected;
                        result.ErrorCodeName =
                            "http2-graceful-shutdown-complete";
                        result.ErrorMessage =
                            "The two-stage graceful GOAWAY sequence is complete.";
                        return CompleteResult(result);
                    }

                    automaticGracefulGoAway = true;
                    action.Frame = CreateGracefulGoAwayFrame(
                        action.OpaqueData);
                }
                else if (action.HasLastStreamId && action.HasErrorCode)
                {
                    action.Frame = CreateGoAwayFrame(action);
                }
                else
                {
                    result.Status = HttpOperationStatus.Rejected;
                    result.ErrorCodeName = "invalid-http2-goaway-action";
                    result.ErrorMessage =
                        "A GOAWAY action must provide both last-stream and error metadata.";
                    return CompleteResult(result);
                }
            }
            else if (action.Frame.FrameType != Http2FrameType.GoAway)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "invalid-http2-goaway-action";
                result.ErrorMessage =
                    "A send-GOAWAY action requires a GOAWAY frame.";
                return CompleteResult(result);
            }
        }

        if ((action.ActionKind != Http2ActionKind.SendFrame &&
            action.ActionKind != Http2ActionKind.SendGoAway &&
            action.ActionKind != Http2ActionKind.SendMessage &&
            action.ActionKind != Http2ActionKind.InitiatePush &&
            action.ActionKind != Http2ActionKind.ResetStream) ||
            action.Frame == null)
        {
            result.Status = HttpOperationStatus.Unsupported;
            result.ErrorCodeName = "http2-action-unsupported";
            result.ErrorMessage =
                "This outbound HTTP/2 connection action is not implemented yet.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.PriorityUpdate &&
            endpointRole != Http2EndpointRole.Client)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName =
                "http2-server-cannot-send-priority-update";
            result.ErrorMessage =
                "Only clients can send PRIORITY_UPDATE frames.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.PriorityUpdate &&
            !enablePriorityUpdate)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-priority-update-disabled";
            result.ErrorMessage =
                "PRIORITY_UPDATE processing is disabled by connection options.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.Origin &&
            endpointRole != Http2EndpointRole.Server)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-client-cannot-send-origin";
            result.ErrorMessage = "Only servers can send ORIGIN frames.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.Origin && !enableOrigin)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-origin-disabled";
            result.ErrorMessage =
                "ORIGIN processing is disabled by connection options.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.PushPromise)
        {
            Http2ConnectionResult pushValidation =
                ValidateOutboundPush(action);
            if (pushValidation.Status != HttpOperationStatus.Done)
            {
                return CompleteResult(pushValidation);
            }
        }

        Http2HeaderBlockResult sequence =
            headerBlockState.ValidateOutboundSequence(action);
        if (sequence.Status != HttpOperationStatus.Done)
        {
            result.Status = sequence.Status;
            result.ErrorCodeName = sequence.ErrorCodeName;
            result.ErrorMessage = sequence.ErrorMessage;
            return CompleteResult(result);
        }

        Http2ConnectionResult validation =
            streamState.ValidateLocalFrame(action);
        if (validation.Status != HttpOperationStatus.Done)
        {
            return CompleteResult(validation);
        }

        if (action.Frame.FrameType == Http2FrameType.PushPromise &&
            action.Message != null)
        {
            action.Message.StreamId = action.Frame.PromisedStreamId;
            action.Message.HasAssociatedStreamId = true;
            action.Message.AssociatedStreamId = action.Frame.StreamId;
        }

        Http2HeaderBlockResult prepared =
            headerBlockState.PrepareOutboundFrame(action);
        if (prepared.Status != HttpOperationStatus.Done)
        {
            result.Status = prepared.Status;
            result.ErrorCodeName = prepared.ErrorCodeName;
            result.ErrorMessage = prepared.ErrorMessage;
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.Priority ||
            action.Frame.FrameType == Http2FrameType.Headers &&
                action.Frame.HasPriority)
        {
            action.Frame.PriorityIgnored =
                PeerWillIgnoreLegacyPriority();
        }

        Http2FrameResult serialized =
            frameCodec.SerializeHttp2Frame(action.Frame);
        if (serialized.Status != HttpOperationStatus.Done)
        {
            headerBlockState.CancelPreparedOutboundFrame();
            result.Status = serialized.Status;
            result.ErrorCodeName = serialized.ErrorCode;
            result.ErrorMessage = serialized.ErrorMessage;
            return CompleteResult(result);
        }

        if (serialized.Frame != null &&
            serialized.Frame.FrameType == Http2FrameType.GoAway)
        {
            Http2ConnectionResult goAwayValidation =
                ValidateOutboundGoAway(serialized.Frame);
            if (goAwayValidation.Status != HttpOperationStatus.Done)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                return CompleteResult(goAwayValidation);
            }
        }

        Http2Settings? outboundSettings = null;
        if (action.Frame.FrameType == Http2FrameType.Settings)
        {
            if (action.Frame.Acknowledgement)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "http2-settings-ack-managed";
                result.ErrorMessage =
                    "SETTINGS acknowledgements are generated from received settings.";
                return CompleteResult(result);
            }

            if (serialized.Frame != null &&
                serialized.Frame.Settings != null)
            {
                outboundSettings = serialized.Frame.Settings;
            }

            if (outboundSettings == null)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "invalid-http2-settings-frame";
                result.ErrorMessage = "Outbound SETTINGS data is required.";
                return CompleteResult(result);
            }

            Http2ConnectionResult settingsValidation =
                ValidateLocalSettingsUpdate(outboundSettings);
            if (settingsValidation.Status != HttpOperationStatus.Done)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                return CompleteResult(settingsValidation);
            }
        }

        if (action.Frame.FrameType == Http2FrameType.Ping &&
            action.Frame.Acknowledgement)
        {
            headerBlockState.CancelPreparedOutboundFrame();
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-ping-ack-managed";
            result.ErrorMessage =
                "PING acknowledgements are generated from received pings.";
            return CompleteResult(result);
        }

        if (action.Frame.FrameType == Http2FrameType.Data)
        {
            if (serialized.Frame == null)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "invalid-http2-data-frame";
                result.ErrorMessage =
                    "Outbound DATA could not be normalized for flow control.";
                return CompleteResult(result);
            }

            Http2ConnectionResult reservation =
                flowControl.ReserveSendCapacity(
                    action, serialized.Frame, streamState);
            if (reservation.Status != HttpOperationStatus.Done)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                if (action.ActionKind == Http2ActionKind.SendMessage)
                {
                    action.Frame = null;
                }

                return CompleteResult(reservation);
            }
        }

        if (serialized.Frame != null &&
            serialized.Frame.FrameType == Http2FrameType.PriorityUpdate)
        {
            Http2ConnectionResult priorityUpdate =
                streamState.ApplyLocalPriorityUpdate(serialized.Frame);
            if (priorityUpdate.Status != HttpOperationStatus.Done)
            {
                headerBlockState.CancelPreparedOutboundFrame();
                return CompleteResult(priorityUpdate);
            }

            action.Frame.Priority = serialized.Frame.Priority;
        }

        if (serialized.Frame != null &&
            (serialized.Frame.FrameType == Http2FrameType.Priority ||
            serialized.Frame.FrameType == Http2FrameType.Headers &&
                serialized.Frame.HasPriority))
        {
            action.Frame.PriorityIgnored =
                serialized.Frame.PriorityIgnored;
        }

        bool initialOutboundHeaders = false;
        if (action.Frame.FrameType == Http2FrameType.Headers)
        {
            Http2StreamSnapshot? outboundStream =
                streamState.FindStream(action.Frame.StreamId);
            initialOutboundHeaders = outboundStream == null ||
                !outboundStream.SentHeaders;
        }

        Http2ConnectionResult applied =
            streamState.ApplyLocalFrame(action.Frame);
        if (applied.Status != HttpOperationStatus.Done)
        {
            headerBlockState.CancelPreparedOutboundFrame();
            return CompleteResult(applied);
        }

        RecordOutboundMessageAction(action);

        if (action.Frame.FrameType == Http2FrameType.PushPromise &&
            action.Message != null && action.Message.PushRequest != null)
        {
            streamState.ApplyPushRequestMetadata(
                action.Frame.PromisedStreamId,
                action.Message.PushRequest);
            HttpPriority? pushPriority =
                Http2PrioritySemantics.ReadPrioritySignal(
                    action.Message.Headers);
            if (pushPriority != null)
            {
                streamState.ApplyServerPrioritySignal(
                    action.Frame.PromisedStreamId, pushPriority);
            }
        }
        else if (action.Message != null)
        {
            streamState.CopyPushMetadataToMessage(
                action.Frame.StreamId,
                action.Message);
        }

        if (initialOutboundHeaders && action.Message != null)
        {
            HttpPriority? priority =
                Http2PrioritySemantics.ReadPrioritySignal(
                    action.Message.Headers);
            if (priority != null)
            {
                if (endpointRole == Http2EndpointRole.Client)
                {
                    streamState.ApplyClientPrioritySignal(
                        action.Frame.StreamId, priority);
                }
                else
                {
                    streamState.ApplyServerPrioritySignal(
                        action.Frame.StreamId, priority);
                }
            }
        }

        if (outboundSettings != null)
        {
            ApplyLocalSettingsUpdate(outboundSettings);
            pendingLocalSettings.Add(CopySettings(outboundSettings));
        }
        else if (action.Frame.FrameType == Http2FrameType.Ping &&
            serialized.Frame != null)
        {
            pendingLocalPings.Add(CopyBytes(serialized.Frame.OpaqueData));
        }

        else if (action.Frame.FrameType == Http2FrameType.GoAway &&
            serialized.Frame != null)
        {
            action.Frame = serialized.Frame;
            ApplySentGoAway(serialized.Frame);
            CopyGoAwayMetadataToAction(action, serialized.Frame);
            if (automaticGracefulGoAway)
            {
                gracefulGoAwayStage = gracefulGoAwayStage + 1;
            }
        }

        headerBlockState.ApplyOutboundFrame(action.Frame);
        result.Status = HttpOperationStatus.Done;
        result.Actions.Add(action);
        if (outboundSettings != null)
        {
            result.Actions.Add(CreateSettingsTimeoutAction(
                true, outboundSettings));
            flowControl.FlushAvailableCredits(result, streamState);
        }
        else if (action.Frame.FrameType == Http2FrameType.Ping &&
            serialized.Frame != null)
        {
            result.Actions.Add(CreatePingTimeoutAction(
                true, serialized.Frame.OpaqueData));
        }

        return CompleteResult(result);
    }

    internal Http2ConnectionResult Release(int streamId, int capacity)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        if (!initialized)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-connection-not-initialized";
            result.ErrorMessage =
                "ResetHttp2Connection must initialize the connection first.";
            return CompleteResult(result);
        }

        if (terminal)
        {
            result.Status = HttpOperationStatus.Terminal;
            result.ErrorCodeName = "http2-connection-terminal";
            result.ErrorMessage = "The HTTP/2 connection is terminal.";
            return CompleteResult(result);
        }

        return CompleteResult(flowControl.ReleaseCapacity(
            streamId, capacity, streamState));
    }

    internal Http2ConnectionSnapshot CreateSnapshot()
    {
        Http2ConnectionSnapshot snapshot = new Http2ConnectionSnapshot();
        snapshot.IsInitialized = initialized;
        snapshot.IsReady = ready;
        snapshot.EndpointRole = endpointRole;
        snapshot.LocalSettings = CopySettings(localSettings);
        snapshot.RemoteSettings = CopySettings(remoteSettings);
        streamState.CopyStateToSnapshot(snapshot);
        snapshot.CanCreateLocalRequestStream =
            initialized && ready && !terminal && !receivedGoAway &&
            endpointRole == Http2EndpointRole.Client &&
            streamState.nextLocalStreamId != 0 &&
            (long)streamState.activeLocalStreams +
                streamState.pendingPriorities.Count <
                streamState.maximumLocalConcurrentStreams;
        snapshot.BufferedInputLength = bufferedInput.Length;
        snapshot.MaximumBufferedSize = maximumBufferedSize;
        snapshot.PendingLocalSettingsCount = pendingLocalSettings.Count;
        snapshot.PendingLocalPingCount = pendingLocalPings.Count;
        snapshot.InitialLocalSettingsPending = initialLocalSettingsPending;
        snapshot.GracefulShutdownStage = gracefulGoAwayStage;
        snapshot.InboundHeaderBlockActive = headerBlockState.inboundActive;
        snapshot.InboundHeaderBlockStreamId =
            headerBlockState.inboundStreamId;
        snapshot.InboundHeaderBlockLength =
            headerBlockState.inboundBytes.Length;
        snapshot.OutboundHeaderBlockActive = headerBlockState.outboundActive;
        snapshot.OutboundHeaderBlockStreamId =
            headerBlockState.outboundStreamId;
        snapshot.OutboundHeaderBlockLength =
            headerBlockState.outboundCompressedSize;
        snapshot.LastProcessedRemoteStreamId =
            lastProcessedRemoteStreamId;
        flowControl.CopyFlowControlToSnapshot(snapshot);
        snapshot.SentPreface = sentPreface;
        snapshot.ReceivedPreface = receivedPreface;
        snapshot.ReceivedInitialSettings = receivedInitialSettings;
        snapshot.InitialSettingsAcknowledged = initialSettingsAcknowledged;
        snapshot.UnexpectedHttp1 = unexpectedHttp1;
        snapshot.HasUnexpectedHttpVersion = hasUnexpectedHttpVersion;
        snapshot.UnexpectedHttpVersion = unexpectedHttpVersion;
        snapshot.SentGoAway = sentGoAway;
        snapshot.ReceivedGoAway = receivedGoAway;
        snapshot.OriginSetInitialized = originSetInitialized;
        int originIndex = 0;
        while (originIndex < origins.Count)
        {
            snapshot.Origins.Add(origins[originIndex]);
            originIndex = originIndex + 1;
        }
        snapshot.HasSentLastStreamId = sentGoAway;
        snapshot.SentLastStreamId = sentLastStreamId;
        snapshot.HasSentGoAwayErrorCode = hasSentGoAwayErrorCode;
        snapshot.SentGoAwayErrorCode = sentGoAwayErrorCode;
        snapshot.SentGoAwayErrorCodeValue = sentGoAwayErrorCodeValue;
        snapshot.IsKnownSentGoAwayErrorCode =
            isKnownSentGoAwayErrorCode;
        snapshot.SentGoAwayDebugData = CopyBytes(sentGoAwayDebugData);
        snapshot.HasReceivedLastStreamId = receivedGoAway;
        snapshot.ReceivedLastStreamId = receivedLastStreamId;
        snapshot.HasReceivedGoAwayErrorCode = hasReceivedGoAwayErrorCode;
        snapshot.ReceivedGoAwayErrorCode = receivedGoAwayErrorCode;
        snapshot.ReceivedGoAwayErrorCodeValue =
            receivedGoAwayErrorCodeValue;
        snapshot.IsKnownReceivedGoAwayErrorCode =
            isKnownReceivedGoAwayErrorCode;
        snapshot.ReceivedGoAwayDebugData =
            CopyBytes(receivedGoAwayDebugData);
        snapshot.IsTerminal = terminal;
        return snapshot;
    }

    internal Http2OriginAdvice EvaluateOrigin(string origin)
    {
        Http2OriginAdvice advice = new Http2OriginAdvice();
        string evaluatedOrigin = origin ?? "";
        advice.Origin = evaluatedOrigin;
        if (!initialized)
        {
            advice.Status = HttpOperationStatus.Rejected;
            advice.Outcome = Http2OriginOutcome.NotApplicable;
            advice.ErrorCodeName = "http2-connection-not-initialized";
            advice.ErrorMessage =
                "ResetHttp2Connection must initialize the connection first.";
            return advice;
        }

        if (endpointRole != Http2EndpointRole.Client || !enableOrigin)
        {
            advice.Status = HttpOperationStatus.Unsupported;
            advice.Outcome = Http2OriginOutcome.NotApplicable;
            advice.ErrorCodeName = "http2-origin-advice-unavailable";
            advice.ErrorMessage =
                "Origin Set advice is available only to clients with ORIGIN processing enabled.";
            return advice;
        }

        if (!Http2FrameCodec.IsValidSerializedOrigin(evaluatedOrigin))
        {
            advice.Status = HttpOperationStatus.Rejected;
            advice.Outcome = Http2OriginOutcome.NotApplicable;
            advice.ErrorCodeName = "invalid-http2-origin";
            advice.ErrorMessage =
                "Origin advice requires a canonical ASCII HTTP origin.";
            return advice;
        }

        advice.Status = HttpOperationStatus.Done;
        advice.OriginSetInitialized = originSetInitialized;
        advice.RequiresExternalAuthorityValidation = true;
        advice.RequiresExternalConnectionSelection = true;
        if (!originSetInitialized)
        {
            advice.Outcome = Http2OriginOutcome.DefaultAuthorityRules;
            advice.OriginSetPermitsAuthority = true;
            advice.OriginSetPermitsCoalescing = true;
            return advice;
        }

        advice.OriginInSet = ContainsOrigin(evaluatedOrigin);
        if (advice.OriginInSet)
        {
            advice.Outcome = Http2OriginOutcome.OriginSetAllows;
            advice.OriginSetPermitsAuthority = true;
            advice.OriginSetPermitsCoalescing = true;
            return advice;
        }

        advice.Outcome = Http2OriginOutcome.OriginSetRejects;
        advice.RequiresExternalAuthorityValidation = false;
        advice.RequiresExternalConnectionSelection = false;
        return advice;
    }

    internal Http2ConnectionResult ValidateOptions(
        Http2ConnectionOptions options)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        if (options == null ||
            options.EndpointRole != Http2EndpointRole.Client &&
            options.EndpointRole != Http2EndpointRole.Server ||
            options.LocalSettings == null ||
            options.LocalSettings.Values == null ||
            options.EncoderOptions == null || options.DecoderOptions == null ||
            options.EncoderOptions.Limits == null ||
            options.DecoderOptions.Limits == null ||
            options.Limits == null ||
            options.Limits.MaximumFrameSize <
                Http2Constants.DefaultMaximumFrameSize ||
            options.Limits.MaximumFrameSize > Http2Constants.MaximumFrameSize ||
            options.Limits.MaximumBufferedSize < 0 ||
            options.Limits.MaximumCompressedBlockSize < 0 ||
            options.Limits.MaximumHeaderListSize < 0 ||
            options.Limits.MaximumFieldCount < 0 ||
            options.Limits.MaximumStringSize < 0 ||
            !HasValidHpackOptions(options.EncoderOptions) ||
            !HasValidHpackOptions(options.DecoderOptions) ||
            options.MaximumRetainedClosedStreams < 0 ||
            !HasValidInitialOrigin(options.InitialOrigin))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-connection-options";
            result.ErrorMessage =
                "HTTP/2 connection options contain an invalid role, setting, or limit.";
            return result;
        }

        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = options.LocalSettings;
        Http2FrameResult validation =
            new Http2FrameCodec().SerializeHttp2Frame(frame);
        if (validation.Status != HttpOperationStatus.Done)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-local-settings";
            result.ErrorMessage = validation.ErrorMessage;
            return result;
        }

        if (validation.Frame == null || validation.Frame.Settings == null ||
            !HasValidInitialLocalSettings(
                options,
                validation.Frame.Settings))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "invalid-http2-local-settings";
            result.ErrorMessage =
                "Initial HTTP/2 settings conflict with the endpoint role or configured limits.";
            return result;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal static bool HasValidInitialOrigin(string? origin)
    {
        return origin == null ||
            Http2FrameCodec.IsValidSerializedOrigin(origin);
    }

    internal bool HasValidInitialLocalSettings(
        Http2ConnectionOptions options,
        Http2Settings settings)
    {
        if (options.EndpointRole == Http2EndpointRole.Server &&
            settings.HasEnablePush && settings.EnablePush)
        {
            return false;
        }

        if (settings.HasHeaderTableSize &&
            settings.HeaderTableSize >
                options.DecoderOptions.MaximumDynamicTableSize)
        {
            return false;
        }

        if (settings.HasMaxFrameSize &&
            settings.MaxFrameSize > options.Limits.MaximumFrameSize)
        {
            return false;
        }

        bool connectEnabled = false;
        bool prioritySeen = false;
        bool priorityValue = false;
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier ==
                Http2SettingIdentifiers.EnableConnectProtocol)
            {
                if (connectEnabled && setting.Value == 0u)
                {
                    return false;
                }

                connectEnabled = setting.Value == 1u;
            }

            if (setting.Identifier ==
                Http2SettingIdentifiers.NoRfc7540Priorities)
            {
                bool nextPriorityValue = setting.Value == 1u;
                if (prioritySeen && nextPriorityValue != priorityValue)
                {
                    return false;
                }

                prioritySeen = true;
                priorityValue = nextPriorityValue;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HasValidHpackOptions(HpackOptions options)
    {
        return options.MaximumDynamicTableSize >= 0 &&
            options.DynamicTableSize >= 0 &&
            options.MaximumHeaderListSize >= 0 &&
            options.MaximumCompressedBlockSize >= 0 &&
            options.MaximumStringSize >= 0 &&
            options.MaximumHeaderCount >= 0 &&
            options.Limits != null &&
            options.Limits.MaximumFieldCount >= 0 &&
            options.Limits.MaximumHeaderListSize >= 0 &&
            options.Limits.MaximumCompressedBlockSize >= 0 &&
            options.Limits.MaximumStringSize >= 0;
    }

    internal void ProcessBufferedInput(Http2ConnectionResult result)
    {
        if (endpointRole == Http2EndpointRole.Server && !receivedClientMagic)
        {
            int compared = bufferedInput.Length;
            if (compared > Http2Constants.ClientConnectionPreface.Length)
            {
                compared = Http2Constants.ClientConnectionPreface.Length;
            }

            int index = 0;
            while (index < compared)
            {
                if (bufferedInput[index] !=
                    Http2Constants.ClientConnectionPreface[index])
                {
                    if (bufferedInput.Length >=
                        Http2Constants.ClientConnectionPreface.Length ||
                        ContainsSequence(bufferedInput, "\r\n"))
                    {
                        FailConnection(
                            result,
                            Http2ErrorCode.ProtocolError,
                            "invalid-http2-client-preface",
                            "The client connection preface does not match HTTP/2.",
                            true);
                    }
                    else
                    {
                        result.Status = HttpOperationStatus.NeedInput;
                    }

                    return;
                }

                index = index + 1;
            }

            if (bufferedInput.Length <
                Http2Constants.ClientConnectionPreface.Length)
            {
                result.Status = HttpOperationStatus.NeedInput;
                return;
            }

            RemoveBufferedPrefix(
                Http2Constants.ClientConnectionPreface.Length);
            receivedClientMagic = true;
        }

        while (bufferedInput.Length != 0 && !terminal)
        {
            Http2FrameResult parsed =
                frameCodec.ParseHttp2Frame(bufferedInput);
            if (parsed.Status == HttpOperationStatus.NeedInput)
            {
                result.Status = HttpOperationStatus.NeedInput;
                return;
            }

            if (parsed.Status != HttpOperationStatus.Done ||
                parsed.Frame == null)
            {
                Http2ErrorCode frameError = Http2ErrorCode.ProtocolError;
                if (receivedInitialSettings || parsed.Frame != null &&
                    parsed.Frame.FrameType == Http2FrameType.Settings)
                {
                    frameError = SelectFrameErrorCode(parsed);
                }

                FailConnection(
                    result,
                    frameError,
                    receivedInitialSettings ?
                        "invalid-http2-frame" :
                        "invalid-http2-connection-preface",
                    parsed.ErrorMessage ??
                        "The peer sent an invalid HTTP/2 connection preface frame.",
                    !receivedInitialSettings);
                return;
            }

            RemoveBufferedPrefix(parsed.Consumed);
            ProcessInboundFrame(result, parsed.Frame);
            if (result.Status != HttpOperationStatus.Done)
            {
                return;
            }
        }

        if (!terminal)
        {
            result.Status = ready ?
                HttpOperationStatus.Done : HttpOperationStatus.NeedInput;
        }
    }

    internal void ProcessInboundFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (endpointRole == Http2EndpointRole.Server && !receivedClientMagic)
        {
            FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "http2-client-preface-required",
                "A server must receive the client connection preface before frames.",
                false);
            return;
        }

        if (!receivedInitialSettings)
        {
            if (frame.FrameType != Http2FrameType.Settings ||
                frame.StreamId != 0 || frame.Acknowledgement ||
                frame.Settings == null || frame.Settings.Values == null)
            {
                FailConnection(
                    result,
                    Http2ErrorCode.ProtocolError,
                    "http2-initial-settings-required",
                    "The first peer frame must be a non-acknowledgement SETTINGS frame.",
                    false);
                return;
            }

            Http2FrameResult validation =
                NormalizeInboundSettingsFrame(frame);
            if (validation.Status != HttpOperationStatus.Done)
            {
                FailConnection(
                    result,
                    SelectSettingsErrorCode(validation),
                    "invalid-http2-initial-settings",
                    validation.ErrorMessage ??
                        "The peer initial SETTINGS frame is invalid.",
                    false);
                return;
            }

            receivedPreface = true;
            receivedInitialSettings = true;
            Http2Settings normalizedRemoteSettings = frame.Settings;
            if (validation.Frame != null && validation.Frame.Settings != null)
            {
                normalizedRemoteSettings = validation.Frame.Settings;
            }

            Http2ConnectionResult settingsValidation =
                ValidateRemoteSettingsUpdate(
                    normalizedRemoteSettings,
                    true);
            if (settingsValidation.Status != HttpOperationStatus.Done)
            {
                FailConnection(
                    result,
                    settingsValidation.ErrorCode,
                    settingsValidation.ErrorCodeName ??
                        "invalid-http2-initial-settings",
                    settingsValidation.ErrorMessage ??
                        "The peer initial settings are invalid.",
                    false);
                return;
            }

            ApplyRemoteSettingsUpdate(normalizedRemoteSettings);
            ready = true;
            result.Status = HttpOperationStatus.Done;
            result.Actions.Add(CreateSettingsAcknowledgement(remoteSettings));
            result.Events.Add(CreateSettingsEvent(frame, remoteSettings));
            result.Events.Add(CreateReadyEvent());
            return;
        }

        Http2HeaderBlockResult sequence =
            headerBlockState.ValidateInboundSequence(frame);
        if (sequence.Status != HttpOperationStatus.Done)
        {
            FailConnection(
                result,
                sequence.ErrorCode,
                sequence.ErrorCodeName ??
                    "http2-inbound-header-block-sequence",
                sequence.ErrorMessage ??
                    "The peer sent a non-contiguous HTTP/2 header block.",
                false);
            return;
        }

        if (frame.FrameType == Http2FrameType.Settings)
        {
            ProcessSettingsFrame(result, frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.Ping)
        {
            Http2FrameResult normalizedPing =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedPing.Status != HttpOperationStatus.Done ||
                normalizedPing.Frame == null)
            {
                FailConnection(
                    result,
                    SelectFrameErrorCode(normalizedPing),
                    normalizedPing.ErrorCode ??
                        "invalid-http2-ping-frame",
                    normalizedPing.ErrorMessage ??
                        "The peer sent an invalid PING frame.",
                    false);
                return;
            }

            ProcessPingFrame(result, normalizedPing.Frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.GoAway)
        {
            Http2FrameResult normalizedGoAway =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedGoAway.Status != HttpOperationStatus.Done ||
                normalizedGoAway.Frame == null)
            {
                FailConnection(
                    result,
                    SelectFrameErrorCode(normalizedGoAway),
                    normalizedGoAway.ErrorCode ??
                        "invalid-http2-goaway-frame",
                    normalizedGoAway.ErrorMessage ??
                        "The peer sent an invalid GOAWAY frame.",
                    false);
                return;
            }

            ProcessGoAwayFrame(result, normalizedGoAway.Frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            Http2FrameResult normalizedReset =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedReset.Status != HttpOperationStatus.Done ||
                normalizedReset.Frame == null)
            {
                FailConnection(
                    result,
                    SelectFrameErrorCode(normalizedReset),
                    normalizedReset.ErrorCode ??
                        "invalid-http2-rst-stream-frame",
                    normalizedReset.ErrorMessage ??
                        "The peer sent an invalid RST_STREAM frame.",
                    false);
                return;
            }

            frame = normalizedReset.Frame;
        }

        if (frame.FrameType == Http2FrameType.WindowUpdate)
        {
            Http2FrameResult normalizedWindow =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedWindow.Status != HttpOperationStatus.Done ||
                normalizedWindow.Frame == null)
            {
                if (frame.StreamId == 0)
                {
                    FailConnection(
                        result,
                        SelectFrameErrorCode(normalizedWindow),
                        normalizedWindow.ErrorCode ??
                            "invalid-http2-window-update",
                        normalizedWindow.ErrorMessage ??
                            "The peer sent an invalid WINDOW_UPDATE frame.",
                        false);
                }
                else
                {
                    FailStream(
                        result,
                        frame.StreamId,
                        Http2ErrorCode.ProtocolError,
                        normalizedWindow.ErrorCode ??
                            "invalid-http2-window-update",
                        normalizedWindow.ErrorMessage ??
                            "The peer sent an invalid WINDOW_UPDATE frame.");
                }

                return;
            }

            frame = normalizedWindow.Frame;
        }

        if (frame.FrameType == Http2FrameType.PriorityUpdate)
        {
            Http2FrameResult normalizedPriorityUpdate =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedPriorityUpdate.Status != HttpOperationStatus.Done ||
                normalizedPriorityUpdate.Frame == null)
            {
                FailConnection(
                    result,
                    Http2ErrorCode.ProtocolError,
                    normalizedPriorityUpdate.ErrorCode ??
                        "invalid-http2-priority-update",
                    normalizedPriorityUpdate.ErrorMessage ??
                        "The peer sent an invalid PRIORITY_UPDATE frame.",
                    false);
                return;
            }

            frame = normalizedPriorityUpdate.Frame;
            if (endpointRole != Http2EndpointRole.Server)
            {
                FailConnection(
                    result,
                    Http2ErrorCode.ProtocolError,
                    "http2-server-sent-priority-update",
                    "A server cannot send a PRIORITY_UPDATE frame.",
                    false);
                return;
            }
        }

        if (frame.FrameType == Http2FrameType.Origin)
        {
            ProcessOriginFrame(result, frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.PushPromise &&
            !acknowledgedLocalPushEnabled)
        {
            FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "http2-push-disabled",
                "The peer sent PUSH_PROMISE after push disablement was acknowledged.",
                false);
            return;
        }

        Http2StreamSnapshot? inboundDataStream = null;
        bool discardLateFrame = streamState.IsLateRemoteFrame(frame);
        bool discardAfterGoAway = IsAfterSentGoAway(frame);
        if (frame.FrameType == Http2FrameType.Data)
        {
            Http2FrameResult normalizedData =
                Http2FrameCodec.BuildHttp2Frame(
                    frame, frameCodec.configuredMaximumFrameSize);
            if (normalizedData.Status != HttpOperationStatus.Done ||
                normalizedData.Frame == null)
            {
                FailConnection(
                    result,
                    SelectFrameErrorCode(normalizedData),
                    normalizedData.ErrorCode ?? "invalid-http2-data-frame",
                    normalizedData.ErrorMessage ??
                        "The peer sent an invalid DATA frame.",
                    false);
                return;
            }

            frame = normalizedData.Frame;
            discardLateFrame = streamState.IsLateRemoteFrame(frame);
            discardAfterGoAway = IsAfterSentGoAway(frame);
            Http2ConnectionResult connectionFlow =
                flowControl.ConsumeConnectionData(frame);
            if (connectionFlow.Status != HttpOperationStatus.Done)
            {
                FailConnection(
                    result,
                    Http2ErrorCode.FlowControlError,
                    connectionFlow.ErrorCodeName ??
                        "http2-connection-flow-control-exceeded",
                    connectionFlow.ErrorMessage ??
                        "The peer exceeded the connection receive window.",
                    false);
                return;
            }

            inboundDataStream = streamState.FindStream(frame.StreamId);
            if (inboundDataStream != null &&
                (inboundDataStream.StreamState == Http2StreamState.Open ||
                inboundDataStream.StreamState ==
                    Http2StreamState.HalfClosedLocal))
            {
                Http2ConnectionResult streamFlow =
                    flowControl.ConsumeStreamData(
                        frame, inboundDataStream);
                if (streamFlow.Status != HttpOperationStatus.Done)
                {
                    FailStream(
                        result,
                        frame.StreamId,
                        Http2ErrorCode.FlowControlError,
                        streamFlow.ErrorCodeName ??
                            "http2-stream-flow-control-exceeded",
                        streamFlow.ErrorMessage ??
                            "The peer exceeded the stream receive window.");
                    flowControl.RegisterRejectedData(result, frame);
                    return;
                }
            }
        }

        if (discardAfterGoAway)
        {
            DiscardInboundFrame(result, frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.PriorityUpdate)
        {
            if (enablePriorityUpdate)
            {
                Http2ConnectionResult priorityUpdate =
                    streamState.ApplyRemotePriorityUpdate(frame);
                if (priorityUpdate.Status != HttpOperationStatus.Done)
                {
                    FailConnection(
                        result,
                        priorityUpdate.ErrorCode,
                        priorityUpdate.ErrorCodeName ??
                            "invalid-http2-priority-update",
                        priorityUpdate.ErrorMessage ??
                            "The peer sent an invalid PRIORITY_UPDATE frame.",
                        false);
                    return;
                }
            }

            Http2Event priorityEvent = new Http2Event();
            priorityEvent.EventKind = Http2EventKind.FrameReceived;
            priorityEvent.Frame = frame;
            result.Events.Add(priorityEvent);
            result.Status = HttpOperationStatus.Done;
            return;
        }

        bool initialInboundHeaders = false;
        if (frame.FrameType == Http2FrameType.Headers)
        {
            Http2StreamSnapshot? inboundHeaderStream =
                streamState.FindStream(frame.StreamId);
            initialInboundHeaders = inboundHeaderStream == null ||
                !inboundHeaderStream.ReceivedHeaders;
        }

        Http2ConnectionResult transition =
            streamState.ApplyRemoteFrame(frame);
        if (transition.Status != HttpOperationStatus.Done)
        {
            if (transition.ErrorScope == Http2ErrorScope.Stream)
            {
                if (transition.ErrorCode == Http2ErrorCode.RefusedStream &&
                    frame.FrameType == Http2FrameType.Headers)
                {
                    Http2HeaderBlockResult refusedHeaderBlock =
                        headerBlockState.ApplyInboundFrame(frame);
                    if (refusedHeaderBlock.Status !=
                        HttpOperationStatus.Done &&
                        refusedHeaderBlock.ErrorScope ==
                            Http2ErrorScope.Connection)
                    {
                        FailConnection(
                            result,
                            refusedHeaderBlock.ErrorCode,
                            refusedHeaderBlock.ErrorCodeName ??
                                "http2-hpack-error",
                            refusedHeaderBlock.ErrorMessage ??
                                "The peer sent an invalid HPACK header block.",
                            false);
                        return;
                    }

                }

                FailStream(
                    result,
                    frame.StreamId,
                    transition.ErrorCode,
                    transition.ErrorCodeName ?? "http2-stream-error",
                    transition.ErrorMessage ??
                        "The peer caused an HTTP/2 stream error.");
                if (frame.FrameType == Http2FrameType.Data)
                {
                    flowControl.RegisterRejectedData(result, frame);
                }

                return;
            }

            FailConnection(
                result,
                transition.HasErrorCode ?
                    transition.ErrorCode : Http2ErrorCode.ProtocolError,
                transition.ErrorCodeName ?? "invalid-http2-stream-id",
                transition.ErrorMessage ??
                    "The peer used an invalid HTTP/2 stream identifier.",
                false);
            return;
        }

        if (frame.FrameType == Http2FrameType.WindowUpdate)
        {
            Http2ConnectionResult windowUpdate =
                flowControl.ApplyRemoteWindowUpdate(frame, streamState);
            if (windowUpdate.Status != HttpOperationStatus.Done)
            {
                if (windowUpdate.ErrorScope == Http2ErrorScope.Stream)
                {
                    FailStream(
                        result,
                        frame.StreamId,
                        windowUpdate.ErrorCode,
                        windowUpdate.ErrorCodeName ??
                            "http2-stream-window-update-error",
                        windowUpdate.ErrorMessage ??
                            "The peer sent an invalid stream WINDOW_UPDATE.");
                }
                else
                {
                    FailConnection(
                        result,
                        windowUpdate.ErrorCode,
                        windowUpdate.ErrorCodeName ??
                            "http2-connection-window-update-error",
                        windowUpdate.ErrorMessage ??
                            "The peer sent an invalid connection WINDOW_UPDATE.",
                        false);
                }

                return;
            }
        }

        if (discardLateFrame)
        {
            DiscardInboundFrame(result, frame);
            return;
        }

        if (frame.FrameType == Http2FrameType.RstStream)
        {
            result.Events.Add(CreateStreamResetEvent(frame));
            result.Status = HttpOperationStatus.Done;
            return;
        }

        Http2HeaderBlockResult headerBlock =
            headerBlockState.ApplyInboundFrame(frame);
        if (headerBlock.Status != HttpOperationStatus.Done)
        {
            if (headerBlock.ErrorScope == Http2ErrorScope.Stream)
            {
                FailStream(
                    result,
                    frame.StreamId,
                    headerBlock.ErrorCode,
                    headerBlock.ErrorCodeName ?? "http2-header-list-error",
                    headerBlock.ErrorMessage ??
                        "The peer header list exceeds a configured limit.");
                return;
            }

            FailConnection(
                result,
                headerBlock.ErrorCode,
                headerBlock.ErrorCodeName ?? "http2-hpack-error",
                headerBlock.ErrorMessage ??
                    "The peer sent an invalid HPACK header block.",
                false);
            return;
        }

        if (headerBlock.Message != null &&
            headerBlock.Message.IsPushed &&
            headerBlock.Message.MessageKind == HttpMessageKind.Request)
        {
            Http2PushRequestResult pushRequest =
                Http2PushSemantics.ValidatePushRequest(
                    headerBlock.Message.Headers);
            if (pushRequest.Status != HttpOperationStatus.Done ||
                pushRequest.Request == null)
            {
                FailStream(
                    result,
                    headerBlock.Message.StreamId,
                    Http2ErrorCode.ProtocolError,
                    pushRequest.ErrorCodeName ??
                        "http2-invalid-push-request",
                    pushRequest.ErrorMessage ??
                        "The peer sent an invalid pushed request.");
                return;
            }

            headerBlock.Message.Request = pushRequest.Request;
            headerBlock.Message.PushRequest = pushRequest.Request;
            headerBlock.Message.IsPushCacheable = true;
            messageCodec.messageState.RegisterPromisedRequest(
                headerBlock.Message.StreamId,
                pushRequest.Request);
            streamState.ApplyPushRequestMetadata(
                headerBlock.Message.StreamId,
                pushRequest.Request);
            HttpPriority? pushPriority =
                Http2PrioritySemantics.ReadPrioritySignal(
                    headerBlock.Message.Headers);
            if (pushPriority != null)
            {
                streamState.ApplyServerPrioritySignal(
                    headerBlock.Message.StreamId, pushPriority);
            }
        }

        if (headerBlock.Message != null)
        {
            if (ShouldMapInboundHeaderMessage(headerBlock.Message))
            {
                Http2MessageResult mapped = MapInboundHeaderMessage(
                    headerBlock.Message);
                if (mapped.Status != HttpOperationStatus.Done)
                {
                    FailStream(
                        result,
                        mapped.StreamId,
                        mapped.ErrorCode,
                        mapped.ErrorCodeName ?? "invalid-http2-message",
                        mapped.ErrorMessage ??
                            "The peer sent an invalid HTTP/2 message field section.");
                    return;
                }

                headerBlock.Message = mapped;
            }

            streamState.CopyPushMetadataToMessage(
                headerBlock.Message.StreamId,
                headerBlock.Message);
        }

        if (frame.FrameType == Http2FrameType.Data &&
            HasMappedInboundMessage(frame.StreamId))
        {
            Http2MessageResult content = messageCodec.DecodeHttp2Content(
                frame.StreamId,
                endpointRole == Http2EndpointRole.Server ?
                    HttpMessageKind.Request : HttpMessageKind.Response,
                frame.Data,
                frame.EndStream);
            if (content.Status != HttpOperationStatus.Done)
            {
                FailStream(
                    result,
                    frame.StreamId,
                    content.ErrorCode,
                    content.ErrorCodeName ?? "invalid-http2-content",
                    content.ErrorMessage ??
                        "The peer sent invalid HTTP/2 message content.");
                flowControl.RegisterRejectedData(result, frame);
                return;
            }

            headerBlock.Message = content;
        }

        if (initialInboundHeaders && headerBlock.Message != null)
        {
            HttpPriority? priority =
                Http2PrioritySemantics.ReadPrioritySignal(
                    headerBlock.Message.Headers);
            if (priority != null)
            {
                if (endpointRole == Http2EndpointRole.Server)
                {
                    streamState.ApplyClientPrioritySignal(
                        headerBlock.Message.StreamId, priority);
                }
                else
                {
                    streamState.ApplyServerPrioritySignal(
                        headerBlock.Message.StreamId, priority);
                }
            }
        }

        if (frame.FrameType == Http2FrameType.Data &&
            inboundDataStream != null)
        {
            flowControl.RegisterDeliveredData(
                result,
                frame,
                inboundDataStream,
                streamState.localInitialWindowSize);
        }

        if (frameCodec.ignoreRfc7540Priorities &&
            (frame.FrameType == Http2FrameType.Priority ||
                frame.FrameType == Http2FrameType.Headers &&
                frame.HasPriority))
        {
            frame.PriorityIgnored = true;
        }

        if (frame.FrameType == Http2FrameType.Unknown &&
            !preserveUnknownFrames)
        {
            result.Status = HttpOperationStatus.Done;
            return;
        }

        Http2Event frameEvent = new Http2Event();
        frameEvent.EventKind = SelectInboundEventKind(
            frame, headerBlock.Message);
        frameEvent.Frame = frame;
        frameEvent.Message = headerBlock.Message;
        if (frameEvent.EventKind == Http2EventKind.PushPromised &&
            headerBlock.Message != null)
        {
            frameEvent.HasStreamId = true;
            frameEvent.StreamId = headerBlock.Message.StreamId;
            frameEvent.MessageKind = HttpMessageKind.Request;
            frameEvent.Request = headerBlock.Message.Request;
        }

        CopyMessageToEvent(frameEvent, headerBlock.Message);

        result.Events.Add(frameEvent);
        if (headerBlock.Message != null &&
            headerBlock.Message.IsComplete &&
            headerBlock.Message.MessageKind !=
                HttpMessageKind.InformationalResponse)
        {
            result.Events.Add(CreateStreamCompleteEvent(
                frame, headerBlock.Message));
        }

        RecordProcessedRemoteMessage(headerBlock.Message);
        result.Status = HttpOperationStatus.Done;
    }

    internal bool ShouldMapInboundHeaderMessage(
        Http2MessageResult decoded)
    {
        if (decoded.IsPushed || HasMappedInboundMessage(decoded.StreamId))
        {
            return true;
        }

        int index = 0;
        while (index < decoded.Headers.Fields.Count)
        {
            string name = decoded.Headers.Fields[index].Name;
            if (name.Length != 0 && name[0] == ':')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasMappedInboundMessage(int streamId)
    {
        if (endpointRole == Http2EndpointRole.Server)
        {
            return messageCodec.messageState.HasRequestHead(streamId);
        }

        return messageCodec.messageState.HasResponseHead(streamId);
    }

    internal Http2MessageResult MapInboundHeaderMessage(
        Http2MessageResult decoded)
    {
        if (decoded.IsPushed &&
            decoded.MessageKind == HttpMessageKind.Request)
        {
            return decoded;
        }

        if (endpointRole == Http2EndpointRole.Server)
        {
            if (messageCodec.messageState.HasRequestHead(decoded.StreamId))
            {
                return messageCodec.DecodeHttp2Trailers(
                    decoded.StreamId,
                    decoded.Headers,
                    decoded.EndStream);
            }

            bool extendedConnectEnabled = enableExtendedConnect &&
                localSettings.HasEnableConnectProtocol &&
                localSettings.EnableConnectProtocol;
            return messageCodec.DecodeHttp2RequestHeaders(
                decoded.StreamId,
                decoded.Headers,
                decoded.EndStream,
                extendedConnectEnabled);
        }

        if (messageCodec.messageState.HasFinalResponse(decoded.StreamId))
        {
            return messageCodec.DecodeHttp2Trailers(
                decoded.StreamId,
                decoded.Headers,
                decoded.EndStream);
        }

        return messageCodec.DecodeHttp2ResponseHeaders(
            decoded.StreamId,
            decoded.Headers,
            decoded.EndStream);
    }

    internal Http2EventKind SelectInboundEventKind(
        Http2Frame frame,
        Http2MessageResult? message)
    {
        if (message == null)
        {
            return Http2EventKind.FrameReceived;
        }

        if (message.IsPushed &&
            message.MessageKind == HttpMessageKind.Request)
        {
            return Http2EventKind.PushPromised;
        }

        if (frame.FrameType == Http2FrameType.Data)
        {
            return Http2EventKind.Content;
        }

        if (message.MessageKind == HttpMessageKind.Trailers)
        {
            return Http2EventKind.Trailers;
        }

        return Http2EventKind.MessageHead;
    }

    internal void CopyMessageToEvent(
        Http2Event target,
        Http2MessageResult? message)
    {
        if (message == null)
        {
            return;
        }

        target.HasStreamId = true;
        target.StreamId = message.StreamId;
        target.MessageKind = message.MessageKind;
        target.Request = message.Request;
        target.Response = message.Response;
        target.Content = message.Content;
        target.Trailers = message.Trailers;
    }

    internal Http2Event CreateStreamCompleteEvent(
        Http2Frame frame,
        Http2MessageResult message)
    {
        Http2Event completed = new Http2Event();
        completed.EventKind = Http2EventKind.StreamComplete;
        completed.Frame = frame;
        completed.Message = message;
        CopyMessageToEvent(completed, message);
        if (message.Request != null)
        {
            completed.MessageKind = HttpMessageKind.Request;
        }
        else if (message.Response != null)
        {
            completed.MessageKind = HttpMessageKind.Response;
        }

        return completed;
    }

    internal void ProcessOriginFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (endpointRole != Http2EndpointRole.Client || !enableOrigin ||
            frame.OriginIgnored || frame.StreamId != 0 ||
            (frame.Flags & 0x0f) != 0)
        {
            frame.OriginIgnored = true;
            AddOriginFrameEvent(result, frame);
            return;
        }

        Http2FrameResult normalized = Http2FrameCodec.BuildHttp2Frame(
            frame, frameCodec.configuredMaximumFrameSize);
        if (normalized.Status != HttpOperationStatus.Done ||
            normalized.Frame == null)
        {
            FailConnection(
                result,
                SelectFrameErrorCode(normalized),
                normalized.ErrorCode ?? "invalid-http2-origin-frame",
                normalized.ErrorMessage ??
                    "The peer sent an invalid ORIGIN frame.",
                false);
            return;
        }

        if (!originSetInitialized)
        {
            originSetInitialized = true;
            if (initialOrigin != null)
            {
                AddOrigin(initialOrigin);
            }
        }

        int index = 0;
        while (index < normalized.Frame.Origins.Count)
        {
            AddOrigin(normalized.Frame.Origins[index]);
            index = index + 1;
        }

        AddOriginFrameEvent(result, normalized.Frame);
    }

    internal void AddOriginFrameEvent(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        Http2Event frameEvent = new Http2Event();
        frameEvent.EventKind = Http2EventKind.FrameReceived;
        frameEvent.Frame = frame;
        result.Events.Add(frameEvent);
        result.Status = HttpOperationStatus.Done;
    }

    internal void AddOrigin(string origin)
    {
        if (!ContainsOrigin(origin))
        {
            origins.Add(origin);
        }
    }

    internal bool ContainsOrigin(string origin)
    {
        int index = 0;
        while (index < origins.Count)
        {
            if (origins[index] == origin)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void RecordProcessedRemoteMessage(Http2MessageResult? message)
    {
        if (message == null)
        {
            return;
        }

        int streamId = message.StreamId;
        if (streamId > lastProcessedRemoteStreamId && streamId > 0 &&
            !streamState.IsLocalIdentifier(streamId) &&
            streamState.FindStream(streamId) != null)
        {
            lastProcessedRemoteStreamId = streamId;
        }
    }

    internal void ProcessSettingsFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        Http2FrameResult normalized = NormalizeInboundSettingsFrame(frame);
        if (normalized.Status != HttpOperationStatus.Done ||
            normalized.Frame == null)
        {
            FailConnection(
                result,
                SelectSettingsErrorCode(normalized),
                normalized.ErrorCode ?? "invalid-http2-settings-frame",
                normalized.ErrorMessage ??
                    "The peer sent an invalid SETTINGS frame.",
                false);
            return;
        }

        if (normalized.Frame.Acknowledgement)
        {
            if (pendingLocalSettings.Count == 0)
            {
                FailConnection(
                    result,
                    Http2ErrorCode.ProtocolError,
                    "http2-unexpected-settings-ack",
                    "The peer acknowledged settings when none were outstanding.",
                    false);
                return;
            }

            Http2Settings acknowledged = pendingLocalSettings[0];
            pendingLocalSettings.RemoveAt(0);
            ApplyAcknowledgedLocalSettings(acknowledged);
            result.Actions.Add(CreateSettingsTimeoutAction(
                false, acknowledged));
            if (initialLocalSettingsPending)
            {
                initialLocalSettingsPending = false;
                initialSettingsAcknowledged = true;
            }

            Http2Event acknowledgementEvent = new Http2Event();
            acknowledgementEvent.EventKind = Http2EventKind.FrameReceived;
            acknowledgementEvent.Frame = frame;
            result.Events.Add(acknowledgementEvent);
            result.Status = HttpOperationStatus.Done;
            return;
        }

        if (normalized.Frame.Settings == null)
        {
            FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "invalid-http2-settings-frame",
                "A non-acknowledgement SETTINGS frame requires settings.",
                false);
            return;
        }

        Http2ConnectionResult validation = ValidateRemoteSettingsUpdate(
            normalized.Frame.Settings,
            false);
        if (validation.Status != HttpOperationStatus.Done)
        {
            FailConnection(
                result,
                validation.ErrorCode,
                validation.ErrorCodeName ?? "invalid-http2-settings-update",
                validation.ErrorMessage ??
                    "The peer settings update is invalid.",
                false);
            return;
        }

        ApplyRemoteSettingsUpdate(normalized.Frame.Settings);
        result.Actions.Add(CreateSettingsAcknowledgement(
            normalized.Frame.Settings));
        result.Events.Add(CreateSettingsEvent(
            frame, normalized.Frame.Settings));
        result.Status = HttpOperationStatus.Done;
    }

    internal void ProcessPingFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (!frame.Acknowledgement)
        {
            result.Actions.Add(CreatePingAcknowledgement(frame));
            Http2Event received = new Http2Event();
            received.EventKind = Http2EventKind.FrameReceived;
            received.Frame = frame;
            result.Events.Add(received);
            result.Status = HttpOperationStatus.Done;
            return;
        }

        int pendingIndex = FindPendingPing(frame.OpaqueData);
        if (pendingIndex < 0)
        {
            Http2Event unmatched = new Http2Event();
            unmatched.EventKind = Http2EventKind.FrameReceived;
            unmatched.Frame = frame;
            result.Events.Add(unmatched);
            result.Status = HttpOperationStatus.Done;
            return;
        }

        byte[] acknowledged = pendingLocalPings[pendingIndex];
        pendingLocalPings.RemoveAt(pendingIndex);
        result.Actions.Add(CreatePingTimeoutAction(false, acknowledged));
        Http2Event acknowledgement = new Http2Event();
        acknowledgement.EventKind = Http2EventKind.PingAcknowledged;
        acknowledgement.Frame = frame;
        acknowledgement.PingData = CopyBytes(acknowledged);
        result.Events.Add(acknowledgement);
        result.Status = HttpOperationStatus.Done;
    }

    internal void ProcessGoAwayFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        if (frame.LastStreamId != 0 &&
            !streamState.IsLocalIdentifier(frame.LastStreamId))
        {
            FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "http2-goaway-last-stream-parity",
                "GOAWAY last-stream identifiers must identify a locally initiated stream.",
                false);
            return;
        }

        if (receivedGoAway && frame.LastStreamId > receivedLastStreamId)
        {
            FailConnection(
                result,
                Http2ErrorCode.ProtocolError,
                "http2-goaway-last-stream-increased",
                "A peer cannot increase the last-stream identifier in a later GOAWAY frame.",
                false);
            return;
        }

        receivedGoAway = true;
        receivedLastStreamId = frame.LastStreamId;
        hasReceivedGoAwayErrorCode = frame.HasErrorCode;
        receivedGoAwayErrorCode = frame.ErrorCode;
        receivedGoAwayErrorCodeValue = frame.ErrorCodeValue;
        isKnownReceivedGoAwayErrorCode = frame.IsKnownErrorCode;
        receivedGoAwayDebugData = CopyBytes(frame.DebugData);
        streamState.ApplyReceivedGoAway(frame.LastStreamId);
        Http2Event received = new Http2Event();
        received.EventKind = Http2EventKind.FrameReceived;
        received.Frame = frame;
        received.HasErrorCode = frame.HasErrorCode;
        received.ErrorCode = frame.ErrorCode;
        received.ErrorCodeValue = frame.ErrorCodeValue;
        received.IsKnownErrorCode = frame.IsKnownErrorCode;
        received.ErrorScope = Http2ErrorScope.Connection;
        result.Events.Add(received);
        result.Status = HttpOperationStatus.Done;
    }

    internal bool IsAfterSentGoAway(Http2Frame frame)
    {
        if (discardedInboundHeaderActive &&
            frame.FrameType == Http2FrameType.Continuation &&
            frame.StreamId == discardedInboundHeaderStreamId)
        {
            return true;
        }

        if (!sentGoAway)
        {
            return false;
        }

        if (frame.FrameType == Http2FrameType.PushPromise &&
            frame.HasPromisedStreamId &&
            !streamState.IsLocalIdentifier(frame.PromisedStreamId))
        {
            return frame.PromisedStreamId > sentLastStreamId;
        }

        return frame.StreamId > sentLastStreamId &&
            !streamState.IsLocalIdentifier(frame.StreamId);
    }

    internal void DiscardInboundFrame(
        Http2ConnectionResult result,
        Http2Frame frame)
    {
        Http2HeaderBlockResult discardedHeaderBlock =
            headerBlockState.ApplyInboundFrame(frame);
        if ((frame.FrameType == Http2FrameType.Headers ||
            frame.FrameType == Http2FrameType.PushPromise) &&
            !frame.EndHeaders &&
            discardedHeaderBlock.Status == HttpOperationStatus.Done)
        {
            discardedInboundHeaderActive = true;
            discardedInboundHeaderStreamId = frame.StreamId;
        }
        else if (frame.FrameType == Http2FrameType.Continuation &&
            frame.EndHeaders)
        {
            discardedInboundHeaderActive = false;
            discardedInboundHeaderStreamId = 0;
        }

        if (discardedHeaderBlock.Status != HttpOperationStatus.Done &&
            discardedHeaderBlock.ErrorScope == Http2ErrorScope.Connection)
        {
            FailConnection(
                result,
                discardedHeaderBlock.ErrorCode,
                discardedHeaderBlock.ErrorCodeName ?? "http2-hpack-error",
                discardedHeaderBlock.ErrorMessage ??
                    "The peer sent an invalid HPACK header block.",
                false);
            return;
        }

        if (frame.FrameType == Http2FrameType.Data)
        {
            flowControl.RegisterRejectedData(result, frame);
        }

        result.Status = HttpOperationStatus.Done;
    }

    internal Http2FrameResult NormalizeInboundSettingsFrame(Http2Frame frame)
    {
        return Http2FrameCodec.BuildHttp2Frame(
            frame, frameCodec.configuredMaximumFrameSize);
    }

    internal Http2ConnectionResult ValidateRemoteSettingsUpdate(
        Http2Settings settings,
        bool initialSettings)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (!streamState.CanApplyRemoteWindowSettings(settings))
        {
            return RejectSettingsUpdate(
                result,
                Http2ErrorCode.FlowControlError,
                "http2-settings-window-overflow",
                "The peer initial-window change overflows a stream window.");
        }

        bool connectEnabled = remoteSettings.HasEnableConnectProtocol &&
            remoteSettings.EnableConnectProtocol;
        bool prioritySeen =
            remoteSettings.HasNoRfc7540Priorities;
        bool priorityValue = remoteSettings.NoRfc7540Priorities;
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier == Http2SettingIdentifiers.EnablePush &&
                endpointRole == Http2EndpointRole.Client &&
                setting.Value == 1u)
            {
                return RejectSettingsUpdate(
                    result,
                    Http2ErrorCode.ProtocolError,
                    "http2-server-enable-push",
                    "A server cannot enable push with SETTINGS_ENABLE_PUSH.");
            }

            if (setting.Identifier ==
                Http2SettingIdentifiers.EnableConnectProtocol)
            {
                if (connectEnabled && setting.Value == 0u)
                {
                    return RejectSettingsUpdate(
                        result,
                        Http2ErrorCode.ProtocolError,
                        "http2-connect-protocol-disabled",
                        "SETTINGS_ENABLE_CONNECT_PROTOCOL cannot be disabled after being enabled.");
                }

                connectEnabled = setting.Value == 1u;
            }

            if (setting.Identifier ==
                Http2SettingIdentifiers.NoRfc7540Priorities)
            {
                if (!initialSettings && !prioritySeen)
                {
                    return RejectSettingsUpdate(
                        result,
                        Http2ErrorCode.ProtocolError,
                        "http2-priority-setting-not-initial",
                        "SETTINGS_NO_RFC7540_PRIORITIES must appear in the initial SETTINGS frame.");
                }

                bool nextValue = setting.Value == 1u;
                if (prioritySeen && nextValue != priorityValue)
                {
                    return RejectSettingsUpdate(
                        result,
                        Http2ErrorCode.ProtocolError,
                        "http2-priority-setting-changed",
                        "SETTINGS_NO_RFC7540_PRIORITIES cannot change during a connection.");
                }

                prioritySeen = true;
                priorityValue = nextValue;
            }

            index = index + 1;
        }

        return result;
    }

    internal Http2ConnectionResult ValidateLocalSettingsUpdate(
        Http2Settings settings)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (!streamState.CanApplyLocalWindowSettings(settings))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-local-settings-window-overflow";
            result.ErrorMessage =
                "The local initial-window change overflows a stream window.";
            return result;
        }

        bool connectEnabled = localSettings.HasEnableConnectProtocol &&
            localSettings.EnableConnectProtocol;
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier == Http2SettingIdentifiers.EnablePush &&
                endpointRole == Http2EndpointRole.Server &&
                setting.Value == 1u)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "http2-server-enable-push";
                result.ErrorMessage =
                    "A server cannot send SETTINGS_ENABLE_PUSH with value 1.";
                return result;
            }

            if (setting.Identifier == Http2SettingIdentifiers.HeaderTableSize &&
                setting.Value >
                    headerBlockState.decoderOptions.MaximumDynamicTableSize)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "http2-local-header-table-limit";
                result.ErrorMessage =
                    "The advertised header-table size exceeds decoder capacity.";
                return result;
            }

            if (setting.Identifier == Http2SettingIdentifiers.MaxFrameSize &&
                setting.Value > configuredMaximumFrameSize)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCodeName = "http2-local-frame-size-limit";
                result.ErrorMessage =
                    "The advertised frame size exceeds configured capacity.";
                return result;
            }

            if (setting.Identifier ==
                Http2SettingIdentifiers.EnableConnectProtocol)
            {
                if (connectEnabled && setting.Value == 0u)
                {
                    result.Status = HttpOperationStatus.Rejected;
                    result.ErrorCodeName =
                        "http2-connect-protocol-disabled";
                    result.ErrorMessage =
                        "SETTINGS_ENABLE_CONNECT_PROTOCOL cannot be disabled after being enabled.";
                    return result;
                }

                connectEnabled = setting.Value == 1u;
            }

            if (setting.Identifier ==
                Http2SettingIdentifiers.NoRfc7540Priorities)
            {
                if (!localSettings.HasNoRfc7540Priorities)
                {
                    result.Status = HttpOperationStatus.Rejected;
                    result.ErrorCodeName =
                        "http2-priority-setting-not-initial";
                    result.ErrorMessage =
                        "SETTINGS_NO_RFC7540_PRIORITIES must appear in the initial SETTINGS frame.";
                    return result;
                }

                bool nextValue = setting.Value == 1u;
                if (nextValue != localSettings.NoRfc7540Priorities)
                {
                    result.Status = HttpOperationStatus.Rejected;
                    result.ErrorCodeName = "http2-priority-setting-changed";
                    result.ErrorMessage =
                        "SETTINGS_NO_RFC7540_PRIORITIES cannot change during a connection.";
                    return result;
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal void ApplyRemoteSettingsUpdate(Http2Settings settings)
    {
        streamState.ApplyRemoteWindowSettings(settings);
        streamState.ApplyRemoteConcurrencySettings(settings);
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier == Http2SettingIdentifiers.HeaderTableSize)
            {
                headerBlockState.ApplyRemoteHeaderTableSetting(setting.Value);
            }
            else if (setting.Identifier ==
                Http2SettingIdentifiers.MaxHeaderListSize)
            {
                headerBlockState.ApplyRemoteHeaderListSetting(setting.Value);
            }
            else if (setting.Identifier ==
                Http2SettingIdentifiers.MaxFrameSize)
            {
                frameCodec.peerMaximumFrameSize = (int)setting.Value;
            }

            index = index + 1;
        }

        MergeSettings(remoteSettings, settings);
        UpdatePriorityIgnoreState();
    }

    internal void ApplyLocalSettingsUpdate(Http2Settings settings)
    {
        streamState.ApplyLocalWindowSettings(settings);
        streamState.ApplyLocalConcurrencySettings(settings);
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier == Http2SettingIdentifiers.MaxFrameSize)
            {
                frameCodec.configuredMaximumFrameSize = (int)setting.Value;
            }

            index = index + 1;
        }

        MergeSettings(localSettings, settings);
        UpdatePriorityIgnoreState();
    }

    internal void ApplyAcknowledgedLocalSettings(Http2Settings settings)
    {
        int index = 0;
        while (index < settings.Values.Count)
        {
            Http2Setting setting = settings.Values[index];
            if (setting.Identifier == Http2SettingIdentifiers.HeaderTableSize)
            {
                headerBlockState.ApplyAcknowledgedLocalHeaderTableSetting(
                    setting.Value);
            }
            else if (setting.Identifier ==
                Http2SettingIdentifiers.EnablePush)
            {
                acknowledgedLocalPushEnabled = setting.Value == 1u;
            }

            index = index + 1;
        }
    }

    internal Http2ConnectionResult ValidateOutboundPush(Http2Action action)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (endpointRole != Http2EndpointRole.Server)
        {
            return result;
        }

        if (!allowServerPush)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-server-push-disabled";
            result.ErrorMessage =
                "Server push is disabled by the connection options.";
            return result;
        }

        if (remoteSettings.HasEnablePush && !remoteSettings.EnablePush)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-peer-disabled-push";
            result.ErrorMessage =
                "The peer SETTINGS_ENABLE_PUSH value disables server push.";
            return result;
        }

        if (action.Message == null)
        {
            return result;
        }

        Http2PushRequestResult validation =
            Http2PushSemantics.ValidatePushRequest(action.Message.Headers);
        if (validation.Status != HttpOperationStatus.Done ||
            validation.Request == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = validation.ErrorCodeName;
            result.ErrorMessage = validation.ErrorMessage;
            return result;
        }

        action.Message.MessageKind = HttpMessageKind.Request;
        action.Message.Request = validation.Request;
        action.Message.PushRequest = validation.Request;
        action.Message.IsPushed = true;
        action.Message.HasAssociatedStreamId = true;
        if (action.Frame != null)
        {
            action.Message.AssociatedStreamId = action.Frame.StreamId;
        }

        action.Message.IsPushCacheable = true;
        return result;
    }

    internal void UpdatePriorityIgnoreState()
    {
        frameCodec.ignoreRfc7540Priorities =
            endpointRole == Http2EndpointRole.Server &&
            (localSettings.HasNoRfc7540Priorities &&
                localSettings.NoRfc7540Priorities ||
            remoteSettings.HasNoRfc7540Priorities &&
                remoteSettings.NoRfc7540Priorities);
    }

    internal bool PeerWillIgnoreLegacyPriority()
    {
        return endpointRole == Http2EndpointRole.Client &&
            remoteSettings.HasNoRfc7540Priorities &&
            remoteSettings.NoRfc7540Priorities;
    }

    internal void MergeSettings(
        Http2Settings target,
        Http2Settings update)
    {
        int index = 0;
        while (index < update.Values.Count)
        {
            Http2Setting source = update.Values[index];
            Http2Setting setting = new Http2Setting();
            setting.Identifier = source.Identifier;
            setting.Value = source.Value;
            setting.IsKnown = source.IsKnown;
            setting.IsValid = source.IsValid;
            target.Values.Add(setting);
            Http2FrameCodec.ApplyHttp2Setting(target, setting);
            index = index + 1;
        }
    }

    internal Http2ConnectionResult RejectSettingsUpdate(
        Http2ConnectionResult result,
        Http2ErrorCode errorCode,
        string errorName,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorScope = Http2ErrorScope.Connection;
        result.HasErrorCode = true;
        result.ErrorCode = errorCode;
        result.ErrorCodeValue = (uint)errorCode;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal Http2ErrorCode SelectSettingsErrorCode(
        Http2FrameResult validation)
    {
        if (validation.ErrorCode == "http2-settings-ack-payload" ||
            validation.ErrorCode == "http2-settings-frame-size")
        {
            return Http2ErrorCode.FrameSizeError;
        }

        if (validation.Frame != null && validation.Frame.Settings != null)
        {
            int index = 0;
            while (index < validation.Frame.Settings.Values.Count)
            {
                Http2Setting setting =
                    validation.Frame.Settings.Values[index];
                if (setting.Identifier ==
                    Http2SettingIdentifiers.InitialWindowSize &&
                    !setting.IsValid)
                {
                    return Http2ErrorCode.FlowControlError;
                }

                index = index + 1;
            }
        }

        return Http2ErrorCode.ProtocolError;
    }

    internal Http2ErrorCode SelectFrameErrorCode(
        Http2FrameResult validation)
    {
        if (validation.Frame != null &&
            validation.Frame.FrameType == Http2FrameType.Settings)
        {
            return SelectSettingsErrorCode(validation);
        }

        string? errorCode = validation.ErrorCode;
        if (errorCode == "http2-frame-size-limit-exceeded" ||
            errorCode == "http2-priority-frame-size" ||
            errorCode == "http2-rst-stream-frame-size" ||
            errorCode == "http2-ping-frame-size" ||
            errorCode == "invalid-http2-ping-data" ||
            errorCode == "http2-goaway-frame-size" ||
            errorCode == "http2-window-update-frame-size" ||
            errorCode == "http2-priority-update-frame-size" ||
            errorCode == "http2-headers-priority-truncated" ||
            errorCode == "http2-push-promise-id-truncated")
        {
            return Http2ErrorCode.FrameSizeError;
        }

        return Http2ErrorCode.ProtocolError;
    }

    internal Http2ConnectionResult ValidateOutboundGoAway(Http2Frame frame)
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        result.Status = HttpOperationStatus.Done;
        if (frame.LastStreamId != 0 &&
            streamState.IsLocalIdentifier(frame.LastStreamId))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-local-goaway-last-stream-parity";
            result.ErrorMessage =
                "A sent GOAWAY last-stream identifier must identify a peer-initiated stream.";
            return result;
        }

        if (sentGoAway && frame.LastStreamId > sentLastStreamId)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCodeName = "http2-local-goaway-last-stream-increased";
            result.ErrorMessage =
                "A later GOAWAY cannot increase the sent last-stream identifier.";
        }

        return result;
    }

    internal void ApplySentGoAway(Http2Frame frame)
    {
        sentGoAway = true;
        sentLastStreamId = frame.LastStreamId;
        hasSentGoAwayErrorCode = frame.HasErrorCode;
        sentGoAwayErrorCode = frame.ErrorCode;
        sentGoAwayErrorCodeValue = frame.ErrorCodeValue;
        isKnownSentGoAwayErrorCode = frame.IsKnownErrorCode;
        sentGoAwayDebugData = CopyBytes(frame.DebugData);
        if (frame.ErrorCodeValue != (uint)Http2ErrorCode.NoError)
        {
            terminal = true;
            ready = false;
        }
    }

    internal Http2ConnectionResult FailStream(
        Http2ConnectionResult result,
        int streamId,
        Http2ErrorCode errorCode,
        string errorName,
        string errorMessage)
    {
        streamState.CloseForStreamError(streamId, errorCode);
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorScope = Http2ErrorScope.Stream;
        result.HasErrorCode = true;
        result.ErrorCode = errorCode;
        result.ErrorCodeValue = (uint)errorCode;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;

        Http2Frame resetFrame = new Http2Frame();
        resetFrame.FrameType = Http2FrameType.RstStream;
        resetFrame.StreamId = streamId;
        resetFrame.HasErrorCode = true;
        resetFrame.ErrorScope = Http2ErrorScope.Stream;
        resetFrame.ErrorCode = errorCode;
        resetFrame.ErrorCodeValue = (uint)errorCode;
        resetFrame.IsKnownErrorCode = true;
        Http2Action resetAction = new Http2Action();
        resetAction.ActionKind = Http2ActionKind.ResetStream;
        resetAction.Frame = resetFrame;
        resetAction.HasStreamId = true;
        resetAction.StreamId = streamId;
        resetAction.HasErrorCode = true;
        resetAction.ErrorCode = errorCode;
        resetAction.ErrorCodeValue = (uint)errorCode;
        resetAction.IsKnownErrorCode = true;
        result.Actions.Add(resetAction);

        Http2Event errorEvent = new Http2Event();
        errorEvent.EventKind = Http2EventKind.Error;
        errorEvent.HasStreamId = true;
        errorEvent.StreamId = streamId;
        errorEvent.ErrorScope = Http2ErrorScope.Stream;
        errorEvent.HasErrorCode = true;
        errorEvent.ErrorCode = errorCode;
        errorEvent.ErrorCodeValue = (uint)errorCode;
        errorEvent.IsKnownErrorCode = true;
        errorEvent.ErrorMessage = errorMessage;
        result.Events.Add(errorEvent);
        return CompleteResult(result);
    }

    internal void AppendBytes(byte[] bytes)
    {
        byte[] combined = new byte[bufferedInput.Length + bytes.Length];
        int index = 0;
        while (index < bufferedInput.Length)
        {
            combined[index] = bufferedInput[index];
            index = index + 1;
        }

        int sourceIndex = 0;
        while (sourceIndex < bytes.Length)
        {
            combined[bufferedInput.Length + sourceIndex] = bytes[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        bufferedInput = combined;
    }

    internal void RemoveBufferedPrefix(int count)
    {
        int remaining = bufferedInput.Length - count;
        byte[] next = new byte[remaining];
        int index = 0;
        while (index < remaining)
        {
            next[index] = bufferedInput[count + index];
            index = index + 1;
        }

        bufferedInput = next;
    }

    internal Http2ConnectionResult FailConnection(
        Http2ConnectionResult result,
        Http2ErrorCode errorCode,
        string errorName,
        string errorMessage,
        bool detectHttp1)
    {
        terminal = true;
        ready = false;
        if (detectHttp1)
        {
            DetectUnexpectedHttp1();
        }

        result.Status = HttpOperationStatus.Rejected;
        result.IsTerminal = true;
        result.ErrorScope = Http2ErrorScope.Connection;
        result.HasErrorCode = true;
        result.ErrorCode = errorCode;
        result.ErrorCodeValue = (uint)errorCode;
        result.IsKnownErrorCode = true;
        result.ErrorCodeName = errorName;
        result.ErrorMessage = errorMessage;
        result.Actions.Clear();
        if (!unexpectedHttp1)
        {
            Http2Action shutdown = CreateConnectionErrorAction(errorCode);
            result.Actions.Add(shutdown);
            if (shutdown.Frame != null)
            {
                ApplySentGoAway(shutdown.Frame);
            }
        }

        Http2Event errorEvent = new Http2Event();
        errorEvent.EventKind = Http2EventKind.Error;
        errorEvent.ErrorScope = Http2ErrorScope.Connection;
        errorEvent.HasErrorCode = true;
        errorEvent.ErrorCode = errorCode;
        errorEvent.ErrorCodeValue = (uint)errorCode;
        errorEvent.IsKnownErrorCode = true;
        errorEvent.ErrorMessage = errorMessage;
        result.Events.Add(errorEvent);
        return CompleteResult(result);
    }

    internal void DetectUnexpectedHttp1()
    {
        if (ContainsSequence(bufferedInput, "HTTP/1.1"))
        {
            unexpectedHttp1 = true;
            hasUnexpectedHttpVersion = true;
            unexpectedHttpVersion = HttpProtocolVersion.Http11;
            return;
        }

        if (ContainsSequence(bufferedInput, "HTTP/1.0"))
        {
            unexpectedHttp1 = true;
            hasUnexpectedHttpVersion = true;
            unexpectedHttpVersion = HttpProtocolVersion.Http10;
        }
    }

    internal bool ContainsSequence(byte[] bytes, string text)
    {
        if (text.Length > bytes.Length)
        {
            return false;
        }

        int start = 0;
        while (start <= bytes.Length - text.Length)
        {
            int index = 0;
            while (index < text.Length &&
                bytes[start + index] == (byte)text[index])
            {
                index = index + 1;
            }

            if (index == text.Length)
            {
                return true;
            }

            start = start + 1;
        }

        return false;
    }

    internal Http2Action CreateClientPrefaceAction()
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendConnectionPreface;
        action.Bytes = CopyBytes(Http2Constants.ClientConnectionPreface);
        return action;
    }

    internal Http2Action CreateInitialSettingsAction()
    {
        Http2Settings settings = CopySettings(localSettings);
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = settings;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        action.Settings = settings;
        return action;
    }

    internal Http2Action CreateSettingsAcknowledgement(Http2Settings settings)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Acknowledgement = true;
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.AcknowledgeSettings;
        action.Frame = frame;
        action.Settings = CopySettings(settings);
        return action;
    }

    internal Http2Action CreateSettingsTimeoutAction(
        bool start,
        Http2Settings settings)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = start ?
            Http2ActionKind.StartSettingsTimeout :
            Http2ActionKind.CancelSettingsTimeout;
        action.Settings = CopySettings(settings);
        return action;
    }

    internal Http2Action CreatePingAcknowledgement(Http2Frame received)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Ping;
        frame.Acknowledgement = true;
        frame.OpaqueData = CopyBytes(received.OpaqueData);
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.AcknowledgePing;
        action.Frame = frame;
        action.OpaqueData = CopyBytes(received.OpaqueData);
        return action;
    }

    internal Http2Action CreatePingTimeoutAction(
        bool start,
        byte[] opaqueData)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = start ?
            Http2ActionKind.StartPingTimeout :
            Http2ActionKind.CancelPingTimeout;
        action.OpaqueData = CopyBytes(opaqueData);
        return action;
    }

    internal Http2Frame CreateGracefulGoAwayFrame(byte[] debugData)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = gracefulGoAwayStage == 0 ?
            streamState.MaximumRemoteStreamIdentifier() :
            lastProcessedRemoteStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = Http2ErrorCode.NoError;
        frame.ErrorCodeValue = (uint)Http2ErrorCode.NoError;
        frame.IsKnownErrorCode = true;
        frame.DebugData = debugData;
        return frame;
    }

    internal Http2Action CreateConnectionErrorAction(
        Http2ErrorCode errorCode)
    {
        int lastStreamId = lastProcessedRemoteStreamId;
        if (sentGoAway && sentLastStreamId < lastStreamId)
        {
            lastStreamId = sentLastStreamId;
        }

        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = true;
        frame.LastStreamId = lastStreamId;
        frame.HasErrorCode = true;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = errorCode;
        frame.ErrorCodeValue = (uint)errorCode;
        frame.IsKnownErrorCode = true;

        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendGoAway;
        action.Frame = frame;
        action.HasLastStreamId = true;
        action.LastStreamId = lastStreamId;
        action.HasErrorCode = true;
        action.ErrorCode = errorCode;
        action.ErrorCodeValue = (uint)errorCode;
        action.IsKnownErrorCode = true;
        return action;
    }

    internal Http2Frame CreateGoAwayFrame(Http2Action action)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.GoAway;
        frame.HasLastStreamId = action.HasLastStreamId;
        frame.LastStreamId = action.LastStreamId;
        frame.HasErrorCode = action.HasErrorCode;
        frame.ErrorScope = Http2ErrorScope.Connection;
        frame.ErrorCode = action.ErrorCode;
        frame.ErrorCodeValue = action.ErrorCodeValue;
        frame.IsKnownErrorCode = action.IsKnownErrorCode;
        frame.DebugData = action.OpaqueData;
        return frame;
    }

    internal void CopyGoAwayMetadataToAction(
        Http2Action action,
        Http2Frame frame)
    {
        action.HasLastStreamId = frame.HasLastStreamId;
        action.LastStreamId = frame.LastStreamId;
        action.HasErrorCode = frame.HasErrorCode;
        action.ErrorCode = frame.ErrorCode;
        action.ErrorCodeValue = frame.ErrorCodeValue;
        action.IsKnownErrorCode = frame.IsKnownErrorCode;
        action.OpaqueData = CopyBytes(frame.DebugData);
    }

    internal Http2Event CreateStreamResetEvent(Http2Frame frame)
    {
        Http2Event http2Event = new Http2Event();
        http2Event.EventKind = Http2EventKind.StreamReset;
        http2Event.Frame = frame;
        http2Event.HasStreamId = true;
        http2Event.StreamId = frame.StreamId;
        http2Event.ErrorScope = Http2ErrorScope.Stream;
        http2Event.HasErrorCode = frame.HasErrorCode;
        http2Event.ErrorCode = frame.ErrorCode;
        http2Event.ErrorCodeValue = frame.ErrorCodeValue;
        http2Event.IsKnownErrorCode = frame.IsKnownErrorCode;
        return http2Event;
    }

    internal Http2Event CreateSettingsEvent(
        Http2Frame frame,
        Http2Settings settings)
    {
        Http2Event http2Event = new Http2Event();
        http2Event.EventKind = Http2EventKind.SettingsChanged;
        http2Event.Frame = frame;
        http2Event.Settings = CopySettings(settings);
        return http2Event;
    }

    internal Http2Event CreateReadyEvent()
    {
        Http2Event http2Event = new Http2Event();
        http2Event.EventKind = Http2EventKind.ConnectionReady;
        return http2Event;
    }

    internal Http2ConnectionResult CompleteResult(
        Http2ConnectionResult result)
    {
        flowControl.DiscardClosedStreamCredits(streamState);
        if (sentGoAway && gracefulGoAwayStage >= 2 &&
            streamState.activeLocalStreams == 0 &&
            streamState.activeRemoteStreams == 0)
        {
            terminal = true;
            ready = false;
        }

        result.IsReady = ready;
        result.IsTerminal = terminal;
        result.UnexpectedHttp1 = unexpectedHttp1;
        result.HasUnexpectedHttpVersion = hasUnexpectedHttpVersion;
        result.UnexpectedHttpVersion = unexpectedHttpVersion;
        result.Snapshot = CreateSnapshot();
        return result;
    }

    internal Http2Settings CopySettings(Http2Settings source)
    {
        Http2Settings copy = new Http2Settings();
        int index = 0;
        while (index < source.Values.Count)
        {
            Http2Setting sourceSetting = source.Values[index];
            Http2Setting setting = new Http2Setting();
            setting.Identifier = sourceSetting.Identifier;
            setting.Value = sourceSetting.Value;
            setting.IsKnown = sourceSetting.IsKnown;
            setting.IsValid = sourceSetting.IsValid;
            copy.Values.Add(setting);
            index = index + 1;
        }

        copy.HasHeaderTableSize = source.HasHeaderTableSize;
        copy.HeaderTableSize = source.HeaderTableSize;
        copy.HasEnablePush = source.HasEnablePush;
        copy.EnablePush = source.EnablePush;
        copy.HasMaxConcurrentStreams = source.HasMaxConcurrentStreams;
        copy.MaxConcurrentStreams = source.MaxConcurrentStreams;
        copy.HasInitialWindowSize = source.HasInitialWindowSize;
        copy.InitialWindowSize = source.InitialWindowSize;
        copy.HasMaxFrameSize = source.HasMaxFrameSize;
        copy.MaxFrameSize = source.MaxFrameSize;
        copy.HasMaxHeaderListSize = source.HasMaxHeaderListSize;
        copy.MaxHeaderListSize = source.MaxHeaderListSize;
        copy.HasEnableConnectProtocol = source.HasEnableConnectProtocol;
        copy.EnableConnectProtocol = source.EnableConnectProtocol;
        copy.HasNoRfc7540Priorities = source.HasNoRfc7540Priorities;
        copy.NoRfc7540Priorities = source.NoRfc7540Priorities;
        return copy;
    }

    internal HttpLimits CopyLimits(HttpLimits source)
    {
        HttpLimits copy = new HttpLimits();
        copy.MaximumStartLineSize = source.MaximumStartLineSize;
        copy.MaximumFieldLineSize = source.MaximumFieldLineSize;
        copy.MaximumFieldSectionSize = source.MaximumFieldSectionSize;
        copy.MaximumFieldCount = source.MaximumFieldCount;
        copy.MaximumFieldValueSize = source.MaximumFieldValueSize;
        copy.MaximumTrailerSectionSize = source.MaximumTrailerSectionSize;
        copy.MaximumTrailerCount = source.MaximumTrailerCount;
        copy.MaximumContentSize = source.MaximumContentSize;
        copy.MaximumChunkSize = source.MaximumChunkSize;
        copy.MaximumCompressedBlockSize =
            source.MaximumCompressedBlockSize;
        copy.MaximumHeaderListSize = source.MaximumHeaderListSize;
        copy.MaximumStringSize = source.MaximumStringSize;
        copy.MaximumFrameSize = source.MaximumFrameSize;
        copy.MaximumRangeCount = source.MaximumRangeCount;
        copy.MaximumParameterCount = source.MaximumParameterCount;
        copy.MaximumListMemberCount = source.MaximumListMemberCount;
        copy.MaximumBufferedSize = source.MaximumBufferedSize;
        return copy;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal int FindPendingPing(byte[] opaqueData)
    {
        int index = 0;
        while (index < pendingLocalPings.Count)
        {
            if (BytesEqual(pendingLocalPings[index], opaqueData))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
