using System;

namespace htmlgui;

/// <summary>Coordinates validated bridge sessions over an injected host message transport.</summary>
public class UiWebViewBridge : IUiWebViewBridge
{
    internal UiBridgeOptions Options;
    internal IUiMessageTransport Transport;
    internal IUiBridgeMessageHandler Handler;
    internal BridgeSession Session;
    internal BridgeMessageFlow Flow;
    internal UpdateBatchCore Updates;
    internal BridgeReconnectCore Reconnect;
    internal BridgeSession? PendingReconnectSession;
    internal int PendingReconnectRevision;

    /// <summary>Initializes a bridge using injected transport and inbound-message callbacks.</summary>
    /// <param name="options">The bridge validation and resource options.</param>
    /// <param name="transport">The host-specific message transport.</param>
    /// <param name="handler">The validated inbound-message callback.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an injected dependency is null.</exception>
    public UiWebViewBridge(UiBridgeOptions? options, IUiMessageTransport? transport, IUiBridgeMessageHandler? handler)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (transport == null)
        {
            throw new ArgumentNullException("transport");
        }

        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        Options = CopyPublicBridgeOptions(options);
        Transport = transport;
        Handler = handler;
        Session = new BridgeSession(Options, null);
        Flow = new BridgeMessageFlow(Session);
        Updates = new UpdateBatchCore();
        Reconnect = new BridgeReconnectCore(Session, Updates);
    }

    /// <summary>Starts the transport and sends the bridge initialization handshake.</summary>
    public UiResult StartUiBridge(string? session, string? nonce, long nowMilliseconds)
    {
        if (PendingReconnectSession != null)
        {
            return CreatePublicBridgeFailure("bridge.reconnect-in-progress", "A replacement bridge handshake is already in progress.", "/bridge/session");
        }

        Session.LocalCapabilities = CopyPublicBridgeCapabilities(Transport.GetUiMessageTransportCapabilities());
        BridgeSessionOperation handshake = Session.BeginBridgeHandshake(session, nonce, nowMilliseconds);
        if (!handshake.Success || handshake.Envelope == null)
        {
            return CreatePublicBridgeCoreFailure(handshake.Code);
        }

        UiResult started = ReadPublicTransportResult(Transport.StartUiMessageTransport(), "bridge.transport-start-failed");
        if (!started.Success)
        {
            Session.State = UiTransportState.Stopped;
            return started;
        }

        UiResult sent = SendPublicBridgeEnvelope(handshake.Envelope);
        if (!sent.Success)
        {
            return sent;
        }

        Updates.InitializeUpdateHistory(CreatePublicEmptySnapshot(Session.Session), UpdateBatchCore.DefaultMaximumHistoryCount);
        return CreatePublicBridgeSuccess();
    }

    /// <summary>Sends one correlated request.</summary>
    public UiResult SendUiBridgeRequest(UiBridgeRequest? request, long nowMilliseconds)
    {
        BridgeMessageFlowResult flow = Flow.SendBridgeRequest(request, UiBridgeMessageKind.Request, nowMilliseconds);
        if (!flow.Success || flow.Envelope == null)
        {
            return CreatePublicBridgeCoreFailure(flow.Code);
        }

        return SendPublicBridgeEnvelope(flow.Envelope);
    }

    /// <summary>Sends one notification that requires acknowledgement.</summary>
    public UiResult SendUiBridgeNotification(UiBridgeMessageKind kind, UiValue? payload, long nowMilliseconds)
    {
        BridgeMessageFlowResult flow = Flow.SendBridgeNotification(kind, payload, nowMilliseconds);
        if (!flow.Success || flow.Envelope == null)
        {
            return CreatePublicBridgeCoreFailure(flow.Code);
        }

        return SendPublicBridgeEnvelope(flow.Envelope);
    }

    /// <summary>Validates and dispatches one inbound message.</summary>
    public UiResult ReceiveUiBridgeMessage(UiBridgeEnvelope? message, bool isMainFrame, long nowMilliseconds)
    {
        BridgeSession? pending = PendingReconnectSession;
        if (pending != null)
        {
            return AcceptPublicReconnectReady(pending, message, isMainFrame, nowMilliseconds);
        }

        if (Session.State == UiTransportState.Handshaking)
        {
            BridgeSessionOperation ready = Session.AcceptBridgeReady(message, isMainFrame, nowMilliseconds);
            if (!ready.Success)
            {
                return CreatePublicBridgeCoreFailure(ready.Code);
            }

            return CreatePublicBridgeSuccess();
        }

        if (message != null && message.Kind == UiBridgeMessageKind.Heartbeat)
        {
            BridgeSessionOperation heartbeat = Session.AcceptBridgeHeartbeat(message, isMainFrame, nowMilliseconds);
            return CreatePublicBridgeSessionResult(heartbeat);
        }

        if (message != null && message.Kind == UiBridgeMessageKind.Shutdown)
        {
            return AcceptPublicBridgeShutdown(message, isMainFrame, nowMilliseconds);
        }

        BridgeMessageFlowResult flow = Flow.ReceiveBridgeMessage(message, isMainFrame, nowMilliseconds);
        return DispatchPublicBridgeFlow(flow, nowMilliseconds);
    }

    /// <summary>Sends an explicit acknowledgement for one received sequence.</summary>
    public UiResult AcknowledgeUiBridgeMessage(int sequence, long nowMilliseconds)
    {
        if (Session.State != UiTransportState.Ready)
        {
            return CreatePublicBridgeCoreFailure("bridge.invalid-state");
        }

        if (!Session.IsActiveTimestampValid(nowMilliseconds))
        {
            return CreatePublicBridgeCoreFailure("bridge.invalid-time");
        }

        if (sequence < 0 || sequence > Session.LastInboundSequence)
        {
            return CreatePublicBridgeFailure("bridge.invalid-acknowledgement", "The acknowledged sequence has not been received.", "/bridge/sequence");
        }

        UiBridgeEnvelope acknowledgement = Flow.CreateAcknowledgementEnvelope(sequence);
        return SendPublicBridgeEnvelope(acknowledgement);
    }

    /// <summary>Sweeps request, heartbeat, and session timeouts.</summary>
    public UiResult SweepUiBridgeTimeouts(long nowMilliseconds)
    {
        BridgeMessageFlowResult requests = Flow.SweepBridgeRequestTimeouts(nowMilliseconds);
        if (!requests.Success)
        {
            return CreatePublicBridgeCoreFailure(requests.Code);
        }

        UiResult requestSend = SendPublicBridgeEnvelopes(requests.Envelopes);
        if (!requestSend.Success)
        {
            return requestSend;
        }

        BridgeSessionOperation sessionResult = Session.SweepBridgeTimeout(nowMilliseconds);
        if (!sessionResult.Success)
        {
            return CreatePublicBridgeCoreFailure(sessionResult.Code);
        }

        if (sessionResult.Envelope != null)
        {
            return SendPublicBridgeEnvelope(sessionResult.Envelope);
        }

        if (sessionResult.State == UiTransportState.Stopped)
        {
            return ReadPublicTransportResult(Transport.StopUiMessageTransport(), "bridge.transport-stop-failed");
        }

        return CreatePublicBridgeSuccess();
    }

    /// <summary>Restarts the transport and begins a new session handshake.</summary>
    public UiResult ReconnectUiBridge(string? session, string? nonce, int lastAcknowledgedRevision, long nowMilliseconds)
    {
        if (PendingReconnectSession != null)
        {
            return CreatePublicBridgeFailure("bridge.reconnect-in-progress", "A replacement bridge handshake is already in progress.", "/bridge/session");
        }

        if (lastAcknowledgedRevision < 0)
        {
            return CreatePublicBridgeCoreFailure("bridge.reconnect-invalid-revision");
        }

        if (!Session.IsIdentifierValid(session))
        {
            return CreatePublicBridgeCoreFailure("bridge.reconnect-invalid-session");
        }

        if (ValueComparer.TextEquals(session, Session.Session) || Reconnect.IsRetiredBridgeSession(session))
        {
            return CreatePublicBridgeCoreFailure("bridge.reconnect-session-reused");
        }

        DomSnapshot? currentSnapshot = Updates.CurrentSnapshot;
        if (currentSnapshot == null)
        {
            return CreatePublicBridgeCoreFailure("bridge.reconnect-missing-snapshot");
        }

        if (lastAcknowledgedRevision > currentSnapshot.Revision)
        {
            return CreatePublicBridgeCoreFailure("bridge.reconnect-future-revision");
        }

        BridgeReconnectResult reconnecting = Reconnect.BeginBridgeReconnect();
        if (!reconnecting.Success)
        {
            return CreatePublicBridgeCoreFailure(reconnecting.Code);
        }

        UiResult stopped = ReadPublicTransportResult(Transport.StopUiMessageTransport(), "bridge.transport-stop-failed");
        if (!stopped.Success)
        {
            return stopped;
        }

        UiTransportCapabilities capabilities = CopyPublicBridgeCapabilities(Transport.GetUiMessageTransportCapabilities());
        BridgeSession replacement = new BridgeSession(Options, capabilities);
        BridgeSessionOperation handshake = replacement.BeginBridgeHandshake(session, nonce, nowMilliseconds);
        if (!handshake.Success || handshake.Envelope == null)
        {
            return CreatePublicBridgeCoreFailure(handshake.Code);
        }

        UiResult started = ReadPublicTransportResult(Transport.StartUiMessageTransport(), "bridge.transport-start-failed");
        if (!started.Success)
        {
            return started;
        }

        UiResult sent = SendPublicBridgeEnvelope(handshake.Envelope);
        if (!sent.Success)
        {
            return sent;
        }

        PendingReconnectSession = replacement;
        PendingReconnectRevision = lastAcknowledgedRevision;
        return CreatePublicBridgeSuccess();
    }

    /// <summary>Begins an orderly bridge shutdown.</summary>
    public UiResult ShutdownUiBridge(long nowMilliseconds)
    {
        BridgeSessionOperation shutdown = Session.BeginBridgeShutdown(nowMilliseconds);
        if (!shutdown.Success || shutdown.Envelope == null)
        {
            return CreatePublicBridgeCoreFailure(shutdown.Code);
        }

        return SendPublicBridgeEnvelope(shutdown.Envelope);
    }

    /// <summary>Reads the negotiated or currently available transport capabilities.</summary>
    public UiTransportCapabilities GetUiBridgeCapabilities()
    {
        if (Session.NegotiatedCapabilities != null)
        {
            return CopyPublicBridgeCapabilities(Session.NegotiatedCapabilities);
        }

        return CopyPublicBridgeCapabilities(Transport.GetUiMessageTransportCapabilities());
    }

    internal UiResult AcceptPublicReconnectReady(BridgeSession pending, UiBridgeEnvelope? message, bool isMainFrame, long nowMilliseconds)
    {
        BridgeSessionOperation ready = pending.AcceptBridgeReady(message, isMainFrame, nowMilliseconds);
        if (!ready.Success)
        {
            return CreatePublicBridgeCoreFailure(ready.Code);
        }

        BridgeReconnectResult resync = Reconnect.CompleteBridgeReconnect(pending, PendingReconnectRevision);
        if (!resync.Success)
        {
            return CreatePublicBridgeCoreFailure(resync.Code);
        }

        Session = pending;
        Flow = new BridgeMessageFlow(Session);
        PendingReconnectSession = null;
        PendingReconnectRevision = 0;
        return CreatePublicBridgeSuccess();
    }

    internal UiResult AcceptPublicBridgeShutdown(UiBridgeEnvelope message, bool isMainFrame, long nowMilliseconds)
    {
        BridgeSessionOperation shutdown = Session.AcceptBridgeShutdown(message, isMainFrame, nowMilliseconds);
        if (!shutdown.Success)
        {
            return CreatePublicBridgeCoreFailure(shutdown.Code);
        }

        if (shutdown.Envelope != null)
        {
            UiResult sent = SendPublicBridgeEnvelope(shutdown.Envelope);
            if (!sent.Success)
            {
                return sent;
            }

            BridgeSessionOperation completed = Session.CompleteBridgeShutdown();
            if (!completed.Success)
            {
                return CreatePublicBridgeCoreFailure(completed.Code);
            }
        }

        return ReadPublicTransportResult(Transport.StopUiMessageTransport(), "bridge.transport-stop-failed");
    }

    internal UiResult DispatchPublicBridgeFlow(BridgeMessageFlowResult flow, long nowMilliseconds)
    {
        if (flow.Envelope != null)
        {
            UiResult sent = SendPublicBridgeEnvelope(flow.Envelope);
            if (!sent.Success)
            {
                return sent;
            }
        }

        if (!flow.Success)
        {
            return CreatePublicBridgeCoreFailure(flow.Code);
        }

        if (flow.Duplicate || flow.Message == null)
        {
            return CreatePublicBridgeSuccess();
        }

        UiBridgeEnvelope callbackMessage = CopyPublicBridgeEnvelope(flow.Message);
        UiResult callback = ReadPublicHandlerResult(Handler.HandleUiBridgeMessage(callbackMessage));
        if (flow.Request != null)
        {
            string correlationId = flow.Request.CorrelationId;
            BridgeMessageFlowResult completion;
            if (callback.Success)
            {
                completion = Flow.CompleteIncomingRequest(correlationId, callback.Value, nowMilliseconds);
            }
            else
            {
                completion = Flow.FailIncomingRequest(correlationId, CreatePublicHandlerError(correlationId, callback), nowMilliseconds);
            }

            if (!completion.Success || completion.Envelope == null)
            {
                return CreatePublicBridgeCoreFailure(completion.Code);
            }

            UiResult completed = SendPublicBridgeEnvelope(completion.Envelope);
            if (!completed.Success)
            {
                return completed;
            }
        }

        return callback;
    }

    internal UiResult SendPublicBridgeEnvelopes(UiBridgeEnvelope[]? envelopes)
    {
        UiBridgeEnvelope[] messages = envelopes ?? Array.Empty<UiBridgeEnvelope>();
        int index = 0;
        while (index < messages.Length)
        {
            UiBridgeEnvelope? message = messages[index];
            if (message == null)
            {
                return CreatePublicBridgeFailure("bridge.null-envelope", "A bridge envelope is required.", "/bridge/message");
            }

            UiResult sent = SendPublicBridgeEnvelope(message);
            if (!sent.Success)
            {
                return sent;
            }

            index++;
        }

        return CreatePublicBridgeSuccess();
    }

    internal UiResult SendPublicBridgeEnvelope(UiBridgeEnvelope message)
    {
        UiBridgeEnvelope copied = CopyPublicBridgeEnvelope(message);
        UiResult sent = ReadPublicTransportResult(Transport.SendUiMessage(copied), "bridge.transport-send-failed");
        if (!sent.Success)
        {
            BridgeSession active = ReadPublicActiveSession();
            active.State = UiTransportState.Faulted;
        }

        return sent;
    }

    internal BridgeSession ReadPublicActiveSession()
    {
        return PendingReconnectSession ?? Session;
    }

    internal UiResult ReadPublicHandlerResult(UiResult? result)
    {
        if (result == null)
        {
            return CreatePublicBridgeFailure("bridge.null-handler-result", "The bridge message handler returned no result.", "/bridge/handler");
        }

        return CopyPublicBridgeResult(result);
    }

    internal UiResult ReadPublicTransportResult(UiResult? result, string code)
    {
        if (result == null)
        {
            return CreatePublicBridgeFailure(code, "The message transport returned no result.", "/bridge/transport");
        }

        return CopyPublicBridgeResult(result);
    }

    internal UiResult CreatePublicBridgeSessionResult(BridgeSessionOperation operation)
    {
        if (!operation.Success)
        {
            return CreatePublicBridgeCoreFailure(operation.Code);
        }

        return CreatePublicBridgeSuccess();
    }

    internal UiResult CreatePublicBridgeCoreFailure(string code)
    {
        return CreatePublicBridgeFailure(code, "The bridge operation failed.", "/bridge");
    }

    internal UiResult CreatePublicBridgeFailure(string code, string message, string location)
    {
        return new UiResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic
                {
                    Severity = UiDiagnosticSeverity.Error,
                    Code = code,
                    Message = message,
                    Location = location
                }
            }
        };
    }

    internal UiResult CreatePublicBridgeSuccess()
    {
        return new UiResult { Success = true };
    }

    internal UiBridgeError CreatePublicHandlerError(string correlationId, UiResult callback)
    {
        UiDiagnostic? diagnostic = callback.Diagnostics.Length == 0 ? null : callback.Diagnostics[0];
        return new UiBridgeError
        {
            Code = diagnostic == null || string.IsNullOrWhiteSpace(diagnostic.Code) ? "bridge.handler-failed" : diagnostic.Code,
            Message = diagnostic == null || string.IsNullOrWhiteSpace(diagnostic.Message) ? "The bridge message handler rejected the request." : diagnostic.Message,
            CorrelationId = correlationId,
            IsRetryable = false
        };
    }

    internal DomSnapshot CreatePublicEmptySnapshot(string session)
    {
        return new DomSnapshot
        {
            SessionId = session,
            RootSeed = "bridge",
            Revision = 0,
            Roots = Array.Empty<DomNode>(),
            ContentHash = StableHash.HashParts(new string[] { "bridge", session, "0" })
        };
    }

    internal UiBridgeOptions CopyPublicBridgeOptions(UiBridgeOptions source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The bridge options copy could not be created.");
        }

        return (UiBridgeOptions)copied;
    }

    internal UiTransportCapabilities CopyPublicBridgeCapabilities(UiTransportCapabilities? source)
    {
        if (source == null)
        {
            return new UiTransportCapabilities();
        }

        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The bridge capability copy could not be created.");
        }

        return (UiTransportCapabilities)copied;
    }

    internal UiBridgeEnvelope CopyPublicBridgeEnvelope(UiBridgeEnvelope source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The bridge envelope copy could not be created.");
        }

        return (UiBridgeEnvelope)copied;
    }

    internal UiResult CopyPublicBridgeResult(UiResult source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The bridge result copy could not be created.");
        }

        return (UiResult)copied;
    }
}
