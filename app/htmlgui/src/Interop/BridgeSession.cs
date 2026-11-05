using System;

namespace htmlgui;

internal class BridgeSessionOperation
{
    internal bool Success;
    internal bool Applied;
    internal bool TimedOut;
    internal string Code = string.Empty;
    internal UiTransportState State;
    internal UiBridgeEnvelope? Envelope;
    internal UiTransportCapabilities? Capabilities;
}

internal class BridgeSession
{
    internal const int MaximumIdentifierLength = 256;

    internal UiBridgeOptions? Options;
    internal UiTransportCapabilities? LocalCapabilities;
    internal CapabilityNegotiator Negotiator;
    internal BridgeValidator Validator;
    internal UiTransportState State;
    internal string Session = string.Empty;
    internal string Nonce = string.Empty;
    internal UiTransportCapabilities? NegotiatedCapabilities;
    internal int NextOutboundSequence;
    internal int LastInboundSequence;
    internal long LastActivityMilliseconds;
    internal long LastHeartbeatMilliseconds;

    internal BridgeSession(UiBridgeOptions? options, UiTransportCapabilities? localCapabilities)
    {
        Options = options;
        LocalCapabilities = localCapabilities;
        Negotiator = new CapabilityNegotiator();
        Validator = new BridgeValidator();
        State = UiTransportState.Stopped;
        LastInboundSequence = -1;
    }

    internal BridgeSessionOperation BeginBridgeHandshake(string? session, string? nonce, long nowMilliseconds)
    {
        if (State != UiTransportState.Stopped)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        if (!IsIdentifierValid(session) || !IsIdentifierValid(nonce))
        {
            return CreateSessionFailure("bridge.invalid-handshake-identity");
        }

        BridgeValidationResult optionResult = Validator.ValidateBridgeOptions(Options);
        if (!optionResult.Success)
        {
            return CreateSessionFailure(optionResult.Code);
        }

        CapabilityNegotiationResult localResult = Negotiator.ValidateBridgeCapabilities(LocalCapabilities);
        if (!localResult.Success)
        {
            return CreateSessionFailure(localResult.Code);
        }

        UiBridgeOptions checkedOptions = Options ?? new UiBridgeOptions();
        if (localResult.Capabilities.Version != checkedOptions.Version)
        {
            return CreateSessionFailure("bridge.local-version-mismatch");
        }

        Session = session ?? string.Empty;
        Nonce = nonce ?? string.Empty;
        LocalCapabilities = localResult.Capabilities;
        NegotiatedCapabilities = null;
        NextOutboundSequence = 0;
        LastInboundSequence = -1;
        LastActivityMilliseconds = nowMilliseconds;
        LastHeartbeatMilliseconds = nowMilliseconds;
        State = UiTransportState.Handshaking;
        UiBridgeEnvelope envelope = CreateSessionEnvelope(UiBridgeMessageKind.Initialize, Negotiator.CreateBridgeHandshakePayload(Nonce, localResult.Capabilities), checkedOptions.Version);
        return CreateSessionSuccess(true, envelope);
    }

    internal BridgeSessionOperation AcceptBridgeInitialize(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        if (State != UiTransportState.Stopped)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        BridgeValidationResult optionResult = Validator.ValidateBridgeOptions(Options);
        if (!optionResult.Success)
        {
            return CreateSessionFailure(optionResult.Code);
        }

        CapabilityNegotiationResult localResult = Negotiator.ValidateBridgeCapabilities(LocalCapabilities);
        if (!localResult.Success)
        {
            return CreateSessionFailure(localResult.Code);
        }

        UiBridgeOptions checkedOptions = Options ?? new UiBridgeOptions();
        if (localResult.Capabilities.Version != checkedOptions.Version)
        {
            return CreateSessionFailure("bridge.local-version-mismatch");
        }

        BridgeValidationResult envelopeResult = ValidateHandshakeEnvelope(envelope, isMainFrame, null, -1);
        if (!envelopeResult.Success)
        {
            return CreateSessionFailure(envelopeResult.Code);
        }

        UiBridgeEnvelope checkedEnvelope = envelope ?? new UiBridgeEnvelope();
        if (checkedEnvelope.Kind != UiBridgeMessageKind.Initialize)
        {
            return CreateSessionFailure("bridge.unexpected-handshake-kind");
        }

        BridgeHandshakePayloadResult payloadResult = Negotiator.ParseBridgeHandshakePayload(checkedEnvelope.Payload);
        if (!payloadResult.Success)
        {
            return CreateSessionFailure(payloadResult.Code);
        }

        if (!IsIdentifierValid(payloadResult.Nonce) || payloadResult.Capabilities.Version != checkedEnvelope.Version)
        {
            return CreateSessionFailure("bridge.handshake-version-mismatch");
        }

        CapabilityNegotiationResult negotiation = Negotiator.NegotiateBridgeCapabilities(localResult.Capabilities, payloadResult.Capabilities);
        if (!negotiation.Success)
        {
            return CreateSessionFailure(negotiation.Code);
        }

        Session = checkedEnvelope.Session;
        Nonce = payloadResult.Nonce;
        LocalCapabilities = localResult.Capabilities;
        NegotiatedCapabilities = negotiation.Capabilities;
        NextOutboundSequence = 0;
        LastInboundSequence = checkedEnvelope.Sequence;
        LastActivityMilliseconds = nowMilliseconds;
        LastHeartbeatMilliseconds = nowMilliseconds;
        State = UiTransportState.Ready;
        UiBridgeEnvelope ready = CreateSessionEnvelope(UiBridgeMessageKind.Ready, Negotiator.CreateBridgeHandshakePayload(Nonce, negotiation.Capabilities), negotiation.Capabilities.Version);
        return CreateSessionSuccess(true, ready);
    }

    internal BridgeSessionOperation AcceptBridgeReady(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        if (State != UiTransportState.Handshaking)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        BridgeValidationResult envelopeResult = ValidateHandshakeEnvelope(envelope, isMainFrame, Session, LastInboundSequence);
        if (!envelopeResult.Success)
        {
            return CreateSessionFailure(envelopeResult.Code);
        }

        UiBridgeEnvelope checkedEnvelope = envelope ?? new UiBridgeEnvelope();
        if (checkedEnvelope.Kind != UiBridgeMessageKind.Ready)
        {
            return CreateSessionFailure("bridge.unexpected-handshake-kind");
        }

        BridgeHandshakePayloadResult payloadResult = Negotiator.ParseBridgeHandshakePayload(checkedEnvelope.Payload);
        if (!payloadResult.Success)
        {
            return CreateSessionFailure(payloadResult.Code);
        }

        if (!ValueComparer.TextEquals(payloadResult.Nonce, Nonce))
        {
            return CreateSessionFailure("bridge.nonce-mismatch");
        }

        if (payloadResult.Capabilities.Version != checkedEnvelope.Version)
        {
            return CreateSessionFailure("bridge.handshake-version-mismatch");
        }

        CapabilityNegotiationResult negotiation = Negotiator.NegotiateBridgeCapabilities(LocalCapabilities, payloadResult.Capabilities);
        if (!negotiation.Success)
        {
            return CreateSessionFailure(negotiation.Code);
        }

        if (negotiation.Capabilities.Version != checkedEnvelope.Version)
        {
            return CreateSessionFailure("bridge.handshake-version-mismatch");
        }

        NegotiatedCapabilities = negotiation.Capabilities;
        LastInboundSequence = checkedEnvelope.Sequence;
        LastActivityMilliseconds = nowMilliseconds;
        LastHeartbeatMilliseconds = nowMilliseconds;
        State = UiTransportState.Ready;
        return CreateSessionSuccess(true, null);
    }

    internal BridgeSessionOperation AcceptBridgeHeartbeat(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        if (State != UiTransportState.Ready && State != UiTransportState.Closing)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        BridgeValidationResult validation = ValidateActiveEnvelope(envelope, isMainFrame);
        if (!validation.Success)
        {
            return CreateSessionFailure(validation.Code);
        }

        UiBridgeEnvelope checkedEnvelope = envelope ?? new UiBridgeEnvelope();
        if (checkedEnvelope.Kind != UiBridgeMessageKind.Heartbeat || checkedEnvelope.Payload != null)
        {
            return CreateSessionFailure("bridge.unexpected-heartbeat");
        }

        LastInboundSequence = checkedEnvelope.Sequence;
        LastActivityMilliseconds = nowMilliseconds;
        return CreateSessionSuccess(true, null);
    }

    internal BridgeSessionOperation SweepBridgeTimeout(long nowMilliseconds)
    {
        if (State != UiTransportState.Ready && State != UiTransportState.Closing)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        UiBridgeOptions checkedOptions = Options ?? new UiBridgeOptions();
        long elapsedActivity = nowMilliseconds - LastActivityMilliseconds;
        if (elapsedActivity >= checkedOptions.TimeoutMilliseconds)
        {
            if (State == UiTransportState.Closing)
            {
                State = UiTransportState.Stopped;
                BridgeSessionOperation closeResult = CreateSessionSuccess(true, null);
                closeResult.TimedOut = true;
                closeResult.Code = "bridge.shutdown-timeout";
                return closeResult;
            }

            State = UiTransportState.Faulted;
            BridgeSessionOperation timeoutResult = CreateSessionFailure("bridge.session-timeout");
            timeoutResult.TimedOut = true;
            return timeoutResult;
        }

        if (State == UiTransportState.Ready)
        {
            int heartbeatInterval = Math.Max(1, checkedOptions.TimeoutMilliseconds / 2);
            if (nowMilliseconds - LastHeartbeatMilliseconds >= heartbeatInterval)
            {
                if (NextOutboundSequence == int.MaxValue)
                {
                    State = UiTransportState.Faulted;
                    return CreateSessionFailure("bridge.sequence-exhausted");
                }

                LastHeartbeatMilliseconds = nowMilliseconds;
                int version = ReadNegotiatedVersion();
                UiBridgeEnvelope heartbeat = CreateSessionEnvelope(UiBridgeMessageKind.Heartbeat, null, version);
                return CreateSessionSuccess(true, heartbeat);
            }
        }

        return CreateSessionSuccess(false, null);
    }

    internal BridgeSessionOperation BeginBridgeShutdown(long nowMilliseconds)
    {
        if (State != UiTransportState.Ready)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        if (NextOutboundSequence == int.MaxValue)
        {
            State = UiTransportState.Faulted;
            return CreateSessionFailure("bridge.sequence-exhausted");
        }

        State = UiTransportState.Closing;
        LastActivityMilliseconds = nowMilliseconds;
        UiBridgeEnvelope shutdown = CreateSessionEnvelope(UiBridgeMessageKind.Shutdown, null, ReadNegotiatedVersion());
        return CreateSessionSuccess(true, shutdown);
    }

    internal BridgeSessionOperation AcceptBridgeShutdown(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        if (State != UiTransportState.Ready && State != UiTransportState.Closing)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        if (!IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateSessionFailure("bridge.invalid-time");
        }

        BridgeValidationResult validation = ValidateActiveEnvelope(envelope, isMainFrame);
        if (!validation.Success)
        {
            return CreateSessionFailure(validation.Code);
        }

        UiBridgeEnvelope checkedEnvelope = envelope ?? new UiBridgeEnvelope();
        if (checkedEnvelope.Kind != UiBridgeMessageKind.Shutdown || checkedEnvelope.Payload != null)
        {
            return CreateSessionFailure("bridge.unexpected-shutdown");
        }

        bool wasClosing = State == UiTransportState.Closing;
        LastInboundSequence = checkedEnvelope.Sequence;
        LastActivityMilliseconds = nowMilliseconds;
        if (wasClosing)
        {
            State = UiTransportState.Stopped;
            return CreateSessionSuccess(true, null);
        }

        if (NextOutboundSequence == int.MaxValue)
        {
            State = UiTransportState.Faulted;
            return CreateSessionFailure("bridge.sequence-exhausted");
        }

        State = UiTransportState.Closing;
        UiBridgeEnvelope response = CreateSessionEnvelope(UiBridgeMessageKind.Shutdown, null, ReadNegotiatedVersion());
        return CreateSessionSuccess(true, response);
    }

    internal BridgeSessionOperation CompleteBridgeShutdown()
    {
        if (State != UiTransportState.Closing)
        {
            return CreateSessionFailure("bridge.invalid-state");
        }

        State = UiTransportState.Stopped;
        return CreateSessionSuccess(true, null);
    }

    internal BridgeValidationResult ValidateHandshakeEnvelope(UiBridgeEnvelope? envelope, bool isMainFrame, string? expectedSession, int lastSequence)
    {
        if (envelope == null)
        {
            return Validator.CreateBridgeValidationFailure("bridge.null-envelope", "A bridge envelope is required.");
        }

        UiBridgeOptions? configuredOptions = Options;
        if (configuredOptions == null)
        {
            return Validator.CreateBridgeValidationFailure("bridge.null-options", "Bridge options are required.");
        }

        if (envelope.Version > configuredOptions.Version)
        {
            return Validator.CreateBridgeValidationFailure("bridge.version-mismatch", "The remote bridge version is newer than the configured version.");
        }

        UiBridgeOptions validationOptions = new UiBridgeOptions
        {
            Version = envelope.Version,
            Origin = configuredOptions.Origin,
            RequireMainFrame = configuredOptions.RequireMainFrame,
            MaximumMessageLength = configuredOptions.MaximumMessageLength,
            MaximumPendingRequests = configuredOptions.MaximumPendingRequests,
            TimeoutMilliseconds = configuredOptions.TimeoutMilliseconds
        };
        return Validator.ValidateBridgeEnvelope(envelope, validationOptions, expectedSession, isMainFrame, lastSequence);
    }

    internal BridgeValidationResult ValidateActiveEnvelope(UiBridgeEnvelope? envelope, bool isMainFrame)
    {
        UiBridgeOptions? configuredOptions = Options;
        if (configuredOptions == null)
        {
            return Validator.CreateBridgeValidationFailure("bridge.null-options", "Bridge options are required.");
        }

        UiBridgeOptions validationOptions = new UiBridgeOptions
        {
            Version = ReadNegotiatedVersion(),
            Origin = configuredOptions.Origin,
            RequireMainFrame = configuredOptions.RequireMainFrame,
            MaximumMessageLength = configuredOptions.MaximumMessageLength,
            MaximumPendingRequests = configuredOptions.MaximumPendingRequests,
            TimeoutMilliseconds = configuredOptions.TimeoutMilliseconds
        };
        return Validator.ValidateBridgeEnvelope(envelope, validationOptions, Session, isMainFrame, LastInboundSequence);
    }

    internal UiBridgeEnvelope CreateSessionEnvelope(UiBridgeMessageKind kind, UiValue? payload, int version)
    {
        UiBridgeEnvelope envelope = new UiBridgeEnvelope
        {
            Version = version,
            Session = Session,
            Sequence = NextOutboundSequence,
            Origin = ReadLocalOrigin(),
            Kind = kind,
            Payload = payload
        };
        NextOutboundSequence++;
        return envelope;
    }

    internal string ReadLocalOrigin()
    {
        UiBridgeOptions? configuredOptions = Options;
        if (configuredOptions != null && ValueComparer.TextEquals(configuredOptions.Origin, BridgeProtocolNames.BrowserOrigin))
        {
            return BridgeProtocolNames.HostOrigin;
        }

        return BridgeProtocolNames.BrowserOrigin;
    }

    internal int ReadNegotiatedVersion()
    {
        if (NegotiatedCapabilities != null)
        {
            return NegotiatedCapabilities.Version;
        }

        UiBridgeOptions? configuredOptions = Options;
        return configuredOptions == null ? BridgeProtocolVersions.Current : configuredOptions.Version;
    }

    internal bool IsIdentifierValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumIdentifierLength;
    }

    internal bool IsTimestampValid(long nowMilliseconds)
    {
        return nowMilliseconds >= 0;
    }

    internal bool IsActiveTimestampValid(long nowMilliseconds)
    {
        return nowMilliseconds >= 0 && nowMilliseconds >= LastActivityMilliseconds && nowMilliseconds >= LastHeartbeatMilliseconds;
    }

    internal BridgeSessionOperation CreateSessionSuccess(bool applied, UiBridgeEnvelope? envelope)
    {
        return new BridgeSessionOperation
        {
            Success = true,
            Applied = applied,
            State = State,
            Envelope = envelope,
            Capabilities = NegotiatedCapabilities
        };
    }

    internal BridgeSessionOperation CreateSessionFailure(string code)
    {
        return new BridgeSessionOperation
        {
            Code = code,
            State = State,
            Capabilities = NegotiatedCapabilities
        };
    }
}
