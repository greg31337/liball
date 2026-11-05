using System;

namespace websocket;

internal class WebSocketConnectionInformationProvider
{
    WebSocketConnectionOptions AppliedOptions;
    WebSocketHandshakeResult OpeningHandshake;
    WebSocketConnectionStateMachine StateMachine;
    WebSocketSendScheduler SendScheduler;
    WebSocketEventQueue Events;
    WebSocketHeartbeatManager HeartbeatManager;
    WebSocketLocalCloseCoordinator LocalCloseCoordinator;
    WebSocketRemoteCloseHandler RemoteCloseHandler;
    long ReceivedFrameCount;
    long SentFrameCount;
    long ReceivedMessageCount;
    long SentMessageCount;

    internal WebSocketConnectionInformationProvider(
        WebSocketConnectionOptions options,
        WebSocketHandshakeResult? handshake,
        WebSocketConnectionStateMachine? stateMachine,
        WebSocketSendScheduler? sendScheduler,
        WebSocketEventQueue? events,
        WebSocketHeartbeatManager? heartbeatManager,
        WebSocketLocalCloseCoordinator? localCloseCoordinator,
        WebSocketRemoteCloseHandler? remoteCloseHandler)
    {
        WebSocketOptionValidator.ValidateConnectionOptions(options);
        if (handshake == null ||
            stateMachine == null ||
            sendScheduler == null ||
            events == null ||
            heartbeatManager == null ||
            localCloseCoordinator == null ||
            remoteCloseHandler == null ||
            stateMachine.Role != options.Role ||
            stateMachine.Transport != options.Transport)
        {
            throw new WebSocketStateException();
        }

        AppliedOptions = WebSocketConnectionSnapshotCopier.CopyOptions(
            options);
        OpeningHandshake =
            WebSocketConnectionSnapshotCopier.CopyHandshake(handshake);
        StateMachine = stateMachine;
        SendScheduler = sendScheduler;
        Events = events;
        HeartbeatManager = heartbeatManager;
        LocalCloseCoordinator = localCloseCoordinator;
        RemoteCloseHandler = remoteCloseHandler;
    }

    internal void RecordInboundProgress(
        int frameCount,
        int messageCount)
    {
        long frameTotal = AddCounter(
            ReceivedFrameCount,
            frameCount);
        long messageTotal = AddCounter(
            ReceivedMessageCount,
            messageCount);
        ReceivedFrameCount = frameTotal;
        ReceivedMessageCount = messageTotal;
    }

    internal void RecordOutboundProgress(
        int frameCount,
        int messageCount)
    {
        long frameTotal = AddCounter(SentFrameCount, frameCount);
        long messageTotal = AddCounter(SentMessageCount, messageCount);
        SentFrameCount = frameTotal;
        SentMessageCount = messageTotal;
    }

    internal WebSocketConnectionInfo GetConnectionInformation()
    {
        HeartbeatManager.SynchronizeHeartbeatAcknowledgement();
        WebSocketConnectionInfo information =
            StateMachine.GetConnectionInformation();
        information.Options =
            WebSocketConnectionSnapshotCopier.CopyOptions(AppliedOptions);
        information.DeliveryMode = AppliedOptions.DeliveryMode;
        information.HasSelectedSubprotocol =
            AppliedOptions.HasSelectedSubprotocol;
        information.SelectedSubprotocol =
            AppliedOptions.SelectedSubprotocol;
        information.Extensions =
            WebSocketConnectionSnapshotCopier.CopyNegotiations(
                AppliedOptions.NegotiatedExtensions);
        information.Handshake =
            WebSocketConnectionSnapshotCopier.CopyHandshake(
                OpeningHandshake);
        information.PendingOutputBytes =
            SendScheduler.GetPendingByteCount();
        information.PendingEventCount = Events.GetEventCount();
        information.ReceivedFrameCount = ReceivedFrameCount;
        information.SentFrameCount = SentFrameCount;
        information.ReceivedMessageCount = ReceivedMessageCount;
        information.SentMessageCount = SentMessageCount;
        information.HeartbeatPingOutstanding =
            HeartbeatManager.HasOutstandingHeartbeatPing();
        information.HasHeartbeatPingSentMilliseconds =
            HeartbeatManager.HasHeartbeatPingSentMilliseconds();
        information.HeartbeatPingSentMilliseconds =
            HeartbeatManager.GetHeartbeatPingSentMilliseconds();
        information.HasLastPingRoundTripMilliseconds =
            HeartbeatManager.HasHeartbeatRoundTripMilliseconds();
        information.LastPingRoundTripMilliseconds =
            HeartbeatManager.GetHeartbeatRoundTripMilliseconds();
        if (LocalCloseCoordinator.HasLocalCloseInfo())
        {
            information.HasLocalClose = true;
            information.LocalClose =
                LocalCloseCoordinator.GetLocalCloseInfo();
            information.LocalClose.Clean = information.CleanClose;
        }

        if (RemoteCloseHandler.HasRemoteCloseInfo())
        {
            information.HasRemoteClose = true;
            information.RemoteClose =
                RemoteCloseHandler.GetRemoteCloseInfo();
            information.RemoteClose.Clean = information.CleanClose;
        }

        return information;
    }

    internal void ResetConnectionInformation()
    {
        ReceivedFrameCount = 0;
        SentFrameCount = 0;
        ReceivedMessageCount = 0;
        SentMessageCount = 0;
    }

    static long AddCounter(long current, int increment)
    {
        if (increment < 0 || current > long.MaxValue - increment)
        {
            throw new WebSocketStateException();
        }

        return current + increment;
    }
}

internal static class WebSocketConnectionSnapshotCopier
{
    internal static WebSocketConnectionOptions CopyOptions(
        WebSocketConnectionOptions source)
    {
        WebSocketConnectionOptions copy =
            new WebSocketConnectionOptions();
        copy.Role = source.Role;
        copy.Transport = source.Transport;
        copy.DeliveryMode = source.DeliveryMode;
        copy.HasSelectedSubprotocol = source.HasSelectedSubprotocol;
        copy.SelectedSubprotocol = source.SelectedSubprotocol;
        copy.Extensions = CopyExtensionHandlers(source.Extensions);
        copy.NegotiatedExtensions =
            CopyNegotiations(source.NegotiatedExtensions);
        copy.HasPerMessageDeflate = source.HasPerMessageDeflate;
        copy.PerMessageDeflate =
            CopyPerMessageDeflate(source.PerMessageDeflate);
        copy.MaximumFramePayloadLength =
            source.MaximumFramePayloadLength;
        copy.MaximumMessagePayloadLength =
            source.MaximumMessagePayloadLength;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        copy.MaximumFragmentCount = source.MaximumFragmentCount;
        copy.MaximumBufferedInputBytes =
            source.MaximumBufferedInputBytes;
        copy.MaximumBufferedOutputBytes =
            source.MaximumBufferedOutputBytes;
        copy.MaximumQueuedEvents = source.MaximumQueuedEvents;
        copy.AutomaticFramePayloadLength =
            source.AutomaticFramePayloadLength;
        copy.AutomaticPong = source.AutomaticPong;
        copy.AutomaticCloseResponse = source.AutomaticCloseResponse;
        copy.CloseTimeoutMilliseconds =
            source.CloseTimeoutMilliseconds;
        copy.Heartbeat = CopyHeartbeat(source.Heartbeat);
        copy.HasMaskKeySource = source.HasMaskKeySource;
        copy.MaskKeySource = source.MaskKeySource;
        return copy;
    }

    internal static WebSocketHandshakeResult CopyHandshake(
        WebSocketHandshakeResult source)
    {
        WebSocketHandshakeResult copy =
            new WebSocketHandshakeResult();
        copy.Outcome = source.Outcome;
        copy.Transport = source.Transport;
        copy.HasRequest = source.HasRequest;
        copy.Request = CopyRequest(source.Request);
        copy.HasResponse = source.HasResponse;
        copy.Response = CopyResponse(source.Response);
        copy.HasSelectedSubprotocol = source.HasSelectedSubprotocol;
        copy.SelectedSubprotocol = source.SelectedSubprotocol;
        copy.Extensions = CopyNegotiations(source.Extensions);
        copy.HasClientKey = source.HasClientKey;
        copy.ClientKey = source.ClientKey;
        copy.HasError = source.HasError;
        copy.Error = CopyError(source.Error);
        return copy;
    }

    internal static WebSocketExtensionNegotiation[] CopyNegotiations(
        WebSocketExtensionNegotiation[] source)
    {
        WebSocketExtensionNegotiation[] copy =
            new WebSocketExtensionNegotiation[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == null)
            {
                throw new WebSocketStateException();
            }

            copy[index] = WebSocketExtensionNegotiator.CopyNegotiation(
                source[index]);
            index = index + 1;
        }

        return copy;
    }

    static IWebSocketExtension[] CopyExtensionHandlers(
        IWebSocketExtension[] source)
    {
        IWebSocketExtension[] copy =
            new IWebSocketExtension[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static PerMessageDeflateOptions CopyPerMessageDeflate(
        PerMessageDeflateOptions source)
    {
        PerMessageDeflateOptions copy =
            new PerMessageDeflateOptions();
        copy.OfferServerNoContextTakeover =
            source.OfferServerNoContextTakeover;
        copy.RequireServerNoContextTakeover =
            source.RequireServerNoContextTakeover;
        copy.OfferClientNoContextTakeover =
            source.OfferClientNoContextTakeover;
        copy.RequireClientNoContextTakeover =
            source.RequireClientNoContextTakeover;
        copy.OfferServerMaxWindowBits =
            source.OfferServerMaxWindowBits;
        copy.HasServerMaxWindowBits = source.HasServerMaxWindowBits;
        copy.ServerMaxWindowBits = source.ServerMaxWindowBits;
        copy.OfferClientMaxWindowBits =
            source.OfferClientMaxWindowBits;
        copy.HasClientMaxWindowBits = source.HasClientMaxWindowBits;
        copy.ClientMaxWindowBits = source.ClientMaxWindowBits;
        copy.CompressionThreshold = source.CompressionThreshold;
        copy.MaximumDecompressedMessagePayloadLength =
            source.MaximumDecompressedMessagePayloadLength;
        return copy;
    }

    static WebSocketHeartbeatOptions CopyHeartbeat(
        WebSocketHeartbeatOptions source)
    {
        WebSocketHeartbeatOptions copy =
            new WebSocketHeartbeatOptions();
        copy.Enabled = source.Enabled;
        copy.IdleIntervalMilliseconds =
            source.IdleIntervalMilliseconds;
        copy.PongTimeoutMilliseconds =
            source.PongTimeoutMilliseconds;
        copy.TokenLength = source.TokenLength;
        return copy;
    }

    static WebSocketHttpRequest CopyRequest(
        WebSocketHttpRequest source)
    {
        WebSocketHttpRequest copy = new WebSocketHttpRequest();
        copy.Transport = source.Transport;
        copy.Method = source.Method;
        copy.HasRequestTarget = source.HasRequestTarget;
        copy.RequestTarget = source.RequestTarget;
        copy.HasHttpVersion = source.HasHttpVersion;
        copy.HttpVersion = source.HttpVersion;
        copy.HasScheme = source.HasScheme;
        copy.Scheme = source.Scheme;
        copy.HasAuthority = source.HasAuthority;
        copy.Authority = source.Authority;
        copy.HasPath = source.HasPath;
        copy.Path = source.Path;
        copy.HasProtocol = source.HasProtocol;
        copy.Protocol = source.Protocol;
        copy.Fields = CopyHeaderFields(source.Fields);
        return copy;
    }

    static WebSocketHttpResponse CopyResponse(
        WebSocketHttpResponse source)
    {
        WebSocketHttpResponse copy = new WebSocketHttpResponse();
        copy.Transport = source.Transport;
        copy.StatusCode = source.StatusCode;
        copy.HasReasonPhrase = source.HasReasonPhrase;
        copy.ReasonPhrase = source.ReasonPhrase;
        copy.Fields = CopyHeaderFields(source.Fields);
        copy.HasBody = source.HasBody;
        copy.Body = CopyBytes(source.Body);
        return copy;
    }

    static WebSocketHeaderField[] CopyHeaderFields(
        WebSocketHeaderField[] source)
    {
        WebSocketHeaderField[] copy =
            new WebSocketHeaderField[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            WebSocketHeaderField field =
                new WebSocketHeaderField();
            field.Name = source[index].Name;
            field.Value = source[index].Value;
            copy[index] = field;
            index = index + 1;
        }

        return copy;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        source.AsSpan().CopyTo(copy);
        return copy;
    }

    static WebSocketError CopyError(WebSocketError source)
    {
        return WebSocketFrameCodec.CopyError(source);
    }
}
