using System;
using System.Collections.Generic;

namespace htmlgui;

internal class BridgeMessageFlowResult
{
    internal bool Success;
    internal bool Applied;
    internal bool Duplicate;
    internal bool Backpressured;
    internal bool TimedOut;
    internal string Code = string.Empty;
    internal UiBridgeEnvelope? Envelope;
    internal UiBridgeEnvelope[] Envelopes = Array.Empty<UiBridgeEnvelope>();
    internal UiBridgeEnvelope? Message;
    internal UiBridgeRequest? Request;
    internal UiBridgeResponse? Response;
    internal UiBridgeError? Error;
    internal string[] CorrelationIds = Array.Empty<string>();
}

internal class BridgePendingRequest
{
    internal string CorrelationId = string.Empty;
    internal long DeadlineMilliseconds;
    internal bool Inbound;
    internal UiBridgeMessageKind RequestKind;
}

internal class BridgeAcknowledgementEntry
{
    internal int Sequence;
}

internal class BridgeCompletedRequest
{
    internal string CorrelationId = string.Empty;
    internal bool Inbound;
    internal UiBridgeMessageKind Kind;
    internal UiBridgeResponse? Response;
    internal UiBridgeError? Error;
}

internal class BridgeAcknowledgementParseResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal int Sequence = -1;
}

internal class BridgeMessageFlow
{
    internal BridgeSession Session;
    internal List<BridgePendingRequest> PendingRequests;
    internal List<BridgeAcknowledgementEntry> PendingAcknowledgements;
    internal List<BridgeCompletedRequest> CompletedRequests;
    internal int LastAcknowledgedSequence;

    internal BridgeMessageFlow(BridgeSession session)
    {
        Session = session;
        PendingRequests = new List<BridgePendingRequest>();
        PendingAcknowledgements = new List<BridgeAcknowledgementEntry>();
        CompletedRequests = new List<BridgeCompletedRequest>();
        LastAcknowledgedSequence = -1;
    }

    internal BridgeMessageFlowResult SendBridgeNotification(UiBridgeMessageKind kind, UiValue? payload, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        if (!IsNotificationKind(kind))
        {
            return CreateFlowFailure("bridge.invalid-notification-kind");
        }

        if (PendingAcknowledgements.Count >= ReadFlowLimit())
        {
            BridgeMessageFlowResult backpressure = CreateFlowFailure("bridge.acknowledgement-backpressure");
            backpressure.Backpressured = true;
            return backpressure;
        }

        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        UiBridgeEnvelope candidate = CreateFlowCandidate(kind, null, payload, null, null, null);
        BridgeValidationResult validation = Session.Validator.ValidateBridgePayload(candidate, ReadMessageLimit());
        if (!validation.Success)
        {
            return CreateFlowFailure(validation.Code);
        }

        candidate.Payload = CopyOptionalFlowValue(payload);
        UiBridgeEnvelope envelope = CommitFlowEnvelope(candidate);
        PendingAcknowledgements.Add(new BridgeAcknowledgementEntry { Sequence = envelope.Sequence });
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = envelope;
        return result;
    }

    internal BridgeMessageFlowResult SendBridgeRequest(UiBridgeRequest? request, UiBridgeMessageKind kind, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        if (!IsRequestKind(kind) || request == null)
        {
            return CreateFlowFailure("bridge.invalid-request");
        }

        UiBridgeEnvelope sourceCandidate = CreateFlowCandidate(kind, request.CorrelationId, null, request, null, null);
        BridgeValidationResult sourceValidation = Session.Validator.ValidateBridgePayload(sourceCandidate, ReadMessageLimit());
        if (!sourceValidation.Success)
        {
            return CreateFlowFailure(sourceValidation.Code);
        }

        if (HasAnyCorrelation(request.CorrelationId))
        {
            BridgeMessageFlowResult duplicate = CreateFlowFailure("bridge.duplicate-correlation");
            duplicate.Duplicate = true;
            return duplicate;
        }

        if (PendingRequests.Count >= ReadFlowLimit())
        {
            BridgeMessageFlowResult backpressure = CreateFlowFailure("bridge.request-backpressure");
            backpressure.Backpressured = true;
            return backpressure;
        }

        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        UiBridgeRequest copiedRequest = CopyFlowRequest(request);
        UiBridgeEnvelope candidate = CreateFlowCandidate(kind, copiedRequest.CorrelationId, null, copiedRequest, null, null);
        UiBridgeEnvelope envelope = CommitFlowEnvelope(candidate);
        PendingRequests.Add(new BridgePendingRequest
        {
            CorrelationId = copiedRequest.CorrelationId,
            DeadlineMilliseconds = CreateRequestDeadline(nowMilliseconds, copiedRequest.TimeoutMilliseconds),
            Inbound = false,
            RequestKind = kind
        });
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = envelope;
        result.Request = copiedRequest;
        return result;
    }

    internal BridgeMessageFlowResult ReceiveBridgeMessage(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        if (envelope == null)
        {
            return CreateFlowFailure("bridge.null-envelope");
        }

        if (envelope.Sequence < 0)
        {
            return CreateFlowFailure("bridge.invalid-sequence");
        }

        if (envelope.Sequence <= Session.LastInboundSequence)
        {
            BridgeValidationResult duplicateValidation = ValidateDuplicateEnvelope(envelope, isMainFrame);
            if (!duplicateValidation.Success)
            {
                return CreateFlowFailure(duplicateValidation.Code);
            }

            return AcceptDuplicateEnvelope(envelope, nowMilliseconds);
        }

        if (Session.LastInboundSequence == int.MaxValue || envelope.Sequence != Session.LastInboundSequence + 1)
        {
            BridgeValidationResult gapValidation = Session.ValidateActiveEnvelope(envelope, isMainFrame);
            if (!gapValidation.Success)
            {
                return CreateFlowFailure(gapValidation.Code);
            }

            return CreateFlowFailure("bridge.message-out-of-order");
        }

        BridgeValidationResult validation = Session.ValidateActiveEnvelope(envelope, isMainFrame);
        if (!validation.Success)
        {
            return CreateFlowFailure(validation.Code);
        }

        UiBridgeMessageKind messageKind = envelope.Kind;
        switch (messageKind)
        {
            case UiBridgeMessageKind.RenderSnapshot:
            case UiBridgeMessageKind.DomPatch:
            case UiBridgeMessageKind.Event:
            case UiBridgeMessageKind.Command:
            case UiBridgeMessageKind.StateMutation:
            case UiBridgeMessageKind.Validation:
            case UiBridgeMessageKind.Navigation:
            case UiBridgeMessageKind.Focus:
            case UiBridgeMessageKind.Measurement:
            case UiBridgeMessageKind.VirtualizationRange:
            case UiBridgeMessageKind.VirtualizationResult:
            case UiBridgeMessageKind.Lifecycle:
            case UiBridgeMessageKind.Log:
            case UiBridgeMessageKind.Diagnostic:
                return AcceptInboundNotification(envelope, nowMilliseconds);
            case UiBridgeMessageKind.Request:
            case UiBridgeMessageKind.HostCall:
                return AcceptInboundRequest(envelope, nowMilliseconds);
            case UiBridgeMessageKind.Response:
            case UiBridgeMessageKind.HostResult:
                return AcceptInboundResponse(envelope, nowMilliseconds);
            case UiBridgeMessageKind.Error:
                return AcceptInboundError(envelope, nowMilliseconds);
            case UiBridgeMessageKind.Cancel:
                return AcceptInboundCancel(envelope, nowMilliseconds);
            case UiBridgeMessageKind.Acknowledgement:
                return AcceptInboundAcknowledgement(envelope, nowMilliseconds);
            default:
                return CreateFlowFailure("bridge.unexpected-message-kind");
        }
    }

    internal BridgeMessageFlowResult CompleteIncomingRequest(string? correlationId, UiValue? value, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        BridgePendingRequest? pending = FindPendingRequest(correlationId, true);
        if (pending == null)
        {
            return CreateFlowFailure("bridge.unknown-correlation");
        }

        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        UiBridgeResponse response = new UiBridgeResponse
        {
            CorrelationId = pending.CorrelationId,
            Success = true,
            Value = value
        };
        UiBridgeMessageKind responseKind = ReadResponseKind(pending.RequestKind);
        UiBridgeEnvelope candidate = CreateFlowCandidate(responseKind, pending.CorrelationId, null, null, response, null);
        BridgeValidationResult validation = Session.Validator.ValidateBridgePayload(candidate, ReadMessageLimit());
        if (!validation.Success)
        {
            return CreateFlowFailure(validation.Code);
        }

        response.Value = CopyOptionalFlowValue(value);
        candidate.Response = response;
        UiBridgeEnvelope envelope = CommitFlowEnvelope(candidate);
        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, true, responseKind, response, null);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = envelope;
        result.Response = response;
        return result;
    }

    internal BridgeMessageFlowResult FailIncomingRequest(string? correlationId, UiBridgeError? error, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        BridgePendingRequest? pending = FindPendingRequest(correlationId, true);
        if (pending == null)
        {
            return CreateFlowFailure("bridge.unknown-correlation");
        }

        if (error == null)
        {
            return CreateFlowFailure("bridge.invalid-error");
        }

        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        UiBridgeError copiedError = CopyFlowError(error);
        UiBridgeEnvelope candidate = CreateFlowCandidate(UiBridgeMessageKind.Error, pending.CorrelationId, null, null, null, copiedError);
        BridgeValidationResult validation = Session.Validator.ValidateBridgePayload(candidate, ReadMessageLimit());
        if (!validation.Success)
        {
            return CreateFlowFailure(validation.Code);
        }

        UiBridgeEnvelope envelope = CommitFlowEnvelope(candidate);
        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, true, UiBridgeMessageKind.Error, null, copiedError);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = envelope;
        result.Error = copiedError;
        return result;
    }

    internal BridgeMessageFlowResult CancelOutgoingRequest(string? correlationId, long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        BridgePendingRequest? pending = FindPendingRequest(correlationId, false);
        if (pending == null)
        {
            BridgeCompletedRequest? completed = FindCompletedRequest(correlationId, false);
            if (completed != null && completed.Kind == UiBridgeMessageKind.Cancel)
            {
                BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
                duplicate.Duplicate = true;
                return duplicate;
            }

            return CreateFlowFailure("bridge.unknown-correlation");
        }

        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        UiBridgeEnvelope candidate = CreateFlowCandidate(UiBridgeMessageKind.Cancel, pending.CorrelationId, null, null, null, null);
        BridgeValidationResult validation = Session.Validator.ValidateBridgePayload(candidate, ReadMessageLimit());
        if (!validation.Success)
        {
            return CreateFlowFailure(validation.Code);
        }

        UiBridgeEnvelope envelope = CommitFlowEnvelope(candidate);
        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, false, UiBridgeMessageKind.Cancel, null, null);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = envelope;
        return result;
    }

    internal BridgeMessageFlowResult SweepBridgeRequestTimeouts(long nowMilliseconds)
    {
        BridgeMessageFlowResult readiness = ValidateFlowReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        int expiredCount = CountExpiredRequests(nowMilliseconds);
        if (expiredCount == 0)
        {
            return CreateFlowSuccess(false);
        }

        if (!CanCreateFlowEnvelopes(expiredCount))
        {
            return FaultFlowSequence();
        }

        UiBridgeEnvelope[] envelopes = new UiBridgeEnvelope[expiredCount];
        string[] correlations = new string[expiredCount];
        int outputIndex = 0;
        int pendingIndex = 0;
        while (pendingIndex < PendingRequests.Count)
        {
            BridgePendingRequest pending = PendingRequests[pendingIndex];
            if (nowMilliseconds >= pending.DeadlineMilliseconds)
            {
                UiBridgeEnvelope candidate;
                if (pending.Inbound)
                {
                    UiBridgeError error = new UiBridgeError
                    {
                        Code = "bridge.request-timeout",
                        Message = "The bridge request timed out.",
                        CorrelationId = pending.CorrelationId,
                        IsRetryable = true
                    };
                    candidate = CreateFlowCandidate(UiBridgeMessageKind.Error, pending.CorrelationId, null, null, null, error);
                    AddCompletedRequest(pending.CorrelationId, true, UiBridgeMessageKind.Error, null, error);
                }
                else
                {
                    candidate = CreateFlowCandidate(UiBridgeMessageKind.Cancel, pending.CorrelationId, null, null, null, null);
                    AddCompletedRequest(pending.CorrelationId, false, UiBridgeMessageKind.Cancel, null, null);
                }

                envelopes[outputIndex] = CommitFlowEnvelope(candidate);
                correlations[outputIndex] = pending.CorrelationId;
                PendingRequests.RemoveAt(pendingIndex);
                outputIndex++;
            }
            else
            {
                pendingIndex++;
            }
        }

        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.TimedOut = true;
        result.Envelopes = envelopes;
        result.CorrelationIds = correlations;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundNotification(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        if (!CanCreateFlowEnvelopes(1))
        {
            return FaultFlowSequence();
        }

        CommitInboundEnvelope(envelope, nowMilliseconds);
        UiBridgeEnvelope acknowledgement = CreateAcknowledgementEnvelope(envelope.Sequence);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Envelope = acknowledgement;
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundRequest(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        UiBridgeRequest request = envelope.Request ?? new UiBridgeRequest();
        BridgePendingRequest? duplicatePending = FindPendingRequest(request.CorrelationId, true);
        BridgeCompletedRequest? duplicateCompleted = FindCompletedRequest(request.CorrelationId, true);
        if (duplicatePending != null || duplicateCompleted != null)
        {
            CommitInboundEnvelope(envelope, nowMilliseconds);
            BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
            duplicate.Duplicate = true;
            duplicate.Request = request;
            duplicate.Message = envelope;
            if (duplicateCompleted != null)
            {
                UiBridgeEnvelope? replay = ReplayCompletedRequest(duplicateCompleted);
                if (replay == null)
                {
                    return FaultFlowSequence();
                }

                duplicate.Envelope = replay;
            }

            return duplicate;
        }

        if (HasAnyCorrelation(request.CorrelationId))
        {
            return CreateFlowFailure("bridge.duplicate-correlation");
        }

        if (PendingRequests.Count >= ReadFlowLimit())
        {
            if (!CanCreateFlowEnvelopes(1))
            {
                return FaultFlowSequence();
            }

            CommitInboundEnvelope(envelope, nowMilliseconds);
            UiBridgeError error = new UiBridgeError
            {
                Code = "bridge.request-backpressure",
                Message = "The bridge request limit has been reached.",
                CorrelationId = request.CorrelationId,
                IsRetryable = true
            };
            UiBridgeEnvelope response = CommitFlowEnvelope(CreateFlowCandidate(UiBridgeMessageKind.Error, request.CorrelationId, null, null, null, error));
            AddCompletedRequest(request.CorrelationId, true, UiBridgeMessageKind.Error, null, error);
            BridgeMessageFlowResult backpressure = CreateFlowFailure("bridge.request-backpressure");
            backpressure.Applied = true;
            backpressure.Backpressured = true;
            backpressure.Envelope = response;
            backpressure.Error = error;
            backpressure.Message = envelope;
            return backpressure;
        }

        PendingRequests.Add(new BridgePendingRequest
        {
            CorrelationId = request.CorrelationId,
            DeadlineMilliseconds = CreateRequestDeadline(nowMilliseconds, request.TimeoutMilliseconds),
            Inbound = true,
            RequestKind = envelope.Kind
        });
        CommitInboundEnvelope(envelope, nowMilliseconds);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Request = request;
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundResponse(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        UiBridgeResponse response = envelope.Response ?? new UiBridgeResponse();
        BridgePendingRequest? pending = FindPendingRequest(response.CorrelationId, false);
        if (pending == null)
        {
            BridgeCompletedRequest? completed = FindCompletedRequest(response.CorrelationId, false);
            if (completed != null)
            {
                CommitInboundEnvelope(envelope, nowMilliseconds);
                BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
                duplicate.Duplicate = true;
                duplicate.Response = response;
                duplicate.Message = envelope;
                return duplicate;
            }

            return CreateFlowFailure("bridge.unknown-correlation");
        }

        if (ReadResponseKind(pending.RequestKind) != envelope.Kind)
        {
            return CreateFlowFailure("bridge.response-kind-mismatch");
        }

        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, false, envelope.Kind, response, null);
        CommitInboundEnvelope(envelope, nowMilliseconds);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Response = response;
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundError(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        UiBridgeError error = envelope.Error ?? new UiBridgeError();
        BridgePendingRequest? pending = FindPendingRequest(envelope.CorrelationId, false);
        if (pending == null)
        {
            BridgeCompletedRequest? completed = FindCompletedRequest(envelope.CorrelationId, false);
            if (completed != null)
            {
                CommitInboundEnvelope(envelope, nowMilliseconds);
                BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
                duplicate.Duplicate = true;
                duplicate.Error = error;
                duplicate.Message = envelope;
                return duplicate;
            }

            return CreateFlowFailure("bridge.unknown-correlation");
        }

        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, false, UiBridgeMessageKind.Error, null, error);
        CommitInboundEnvelope(envelope, nowMilliseconds);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Error = error;
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundCancel(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        BridgePendingRequest? pending = FindPendingRequest(envelope.CorrelationId, true);
        if (pending == null)
        {
            BridgeCompletedRequest? completed = FindCompletedRequest(envelope.CorrelationId, true);
            if (completed != null)
            {
                CommitInboundEnvelope(envelope, nowMilliseconds);
                BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
                duplicate.Duplicate = true;
                duplicate.Message = envelope;
                return duplicate;
            }

            return CreateFlowFailure("bridge.unknown-correlation");
        }

        PendingRequests.Remove(pending);
        AddCompletedRequest(pending.CorrelationId, true, UiBridgeMessageKind.Cancel, null, null);
        CommitInboundEnvelope(envelope, nowMilliseconds);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptInboundAcknowledgement(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        BridgeAcknowledgementParseResult parsed = ParseAcknowledgementPayload(envelope.Payload);
        if (!parsed.Success)
        {
            return CreateFlowFailure(parsed.Code);
        }

        if (parsed.Sequence <= LastAcknowledgedSequence)
        {
            CommitInboundEnvelope(envelope, nowMilliseconds);
            BridgeMessageFlowResult duplicate = CreateFlowSuccess(false);
            duplicate.Duplicate = true;
            duplicate.Message = envelope;
            return duplicate;
        }

        if (!HasPendingAcknowledgement(parsed.Sequence))
        {
            return CreateFlowFailure("bridge.unknown-acknowledgement");
        }

        int index = PendingAcknowledgements.Count - 1;
        while (index >= 0)
        {
            if (PendingAcknowledgements[index].Sequence <= parsed.Sequence)
            {
                PendingAcknowledgements.RemoveAt(index);
            }

            index--;
        }

        LastAcknowledgedSequence = parsed.Sequence;
        CommitInboundEnvelope(envelope, nowMilliseconds);
        BridgeMessageFlowResult result = CreateFlowSuccess(true);
        result.Message = envelope;
        return result;
    }

    internal BridgeMessageFlowResult AcceptDuplicateEnvelope(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        Session.LastActivityMilliseconds = nowMilliseconds;
        BridgeMessageFlowResult result = CreateFlowSuccess(false);
        result.Duplicate = true;
        result.Message = envelope;
        if (IsNotificationKind(envelope.Kind))
        {
            if (!CanCreateFlowEnvelopes(1))
            {
                return FaultFlowSequence();
            }

            result.Envelope = CreateAcknowledgementEnvelope(envelope.Sequence);
        }
        else if (IsRequestKind(envelope.Kind) && envelope.CorrelationId != null)
        {
            BridgeCompletedRequest? completed = FindCompletedRequest(envelope.CorrelationId, true);
            if (completed != null)
            {
                UiBridgeEnvelope? replay = ReplayCompletedRequest(completed);
                if (replay == null)
                {
                    return FaultFlowSequence();
                }

                result.Envelope = replay;
            }
        }

        return result;
    }

    internal BridgeValidationResult ValidateDuplicateEnvelope(UiBridgeEnvelope envelope, bool isMainFrame)
    {
        UiBridgeOptions? options = Session.Options;
        if (options == null)
        {
            return Session.Validator.CreateBridgeValidationFailure("bridge.null-options", "Bridge options are required.");
        }

        UiBridgeOptions validationOptions = new UiBridgeOptions
        {
            Version = Session.ReadNegotiatedVersion(),
            Origin = options.Origin,
            RequireMainFrame = options.RequireMainFrame,
            MaximumMessageLength = options.MaximumMessageLength,
            MaximumPendingRequests = options.MaximumPendingRequests,
            TimeoutMilliseconds = options.TimeoutMilliseconds
        };
        int priorSequence = envelope.Sequence == 0 ? -1 : envelope.Sequence - 1;
        return Session.Validator.ValidateBridgeEnvelope(envelope, validationOptions, Session.Session, isMainFrame, priorSequence);
    }

    internal BridgeAcknowledgementParseResult ParseAcknowledgementPayload(UiValue? payload)
    {
        if (payload == null || payload.Kind != UiValueKind.Object || payload.ObjectValue == null)
        {
            return CreateAcknowledgementFailure("bridge.invalid-acknowledgement");
        }

        UiValueEntry[] entries = payload.ObjectValue;
        if (entries.Length != 1 || entries[0] == null || !ValueComparer.TextEquals(entries[0].Key, BridgeProtocolNames.Acknowledgement))
        {
            return CreateAcknowledgementFailure("bridge.invalid-acknowledgement");
        }

        UiValue value = entries[0].Value;
        if (value == null || value.Kind != UiValueKind.Integer || value.IntegerValue < 0 || value.IntegerValue > int.MaxValue)
        {
            return CreateAcknowledgementFailure("bridge.invalid-acknowledgement");
        }

        return new BridgeAcknowledgementParseResult { Success = true, Sequence = (int)value.IntegerValue };
    }

    internal UiBridgeEnvelope CreateAcknowledgementEnvelope(int acknowledgedSequence)
    {
        UiValue payload = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                new UiValueEntry
                {
                    Key = BridgeProtocolNames.Acknowledgement,
                    Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = acknowledgedSequence }
                }
            }
        };
        return CommitFlowEnvelope(CreateFlowCandidate(UiBridgeMessageKind.Acknowledgement, null, payload, null, null, null));
    }

    internal UiBridgeEnvelope? ReplayCompletedRequest(BridgeCompletedRequest completed)
    {
        if (!CanCreateFlowEnvelopes(1))
        {
            return null;
        }

        UiBridgeResponse? response = CopyOptionalFlowResponse(completed.Response);
        UiBridgeError? error = CopyOptionalFlowError(completed.Error);
        return CommitFlowEnvelope(CreateFlowCandidate(completed.Kind, completed.CorrelationId, null, null, response, error));
    }

    internal UiBridgeEnvelope CreateFlowCandidate(UiBridgeMessageKind kind, string? correlationId, UiValue? payload, UiBridgeRequest? request, UiBridgeResponse? response, UiBridgeError? error)
    {
        return new UiBridgeEnvelope
        {
            Version = Session.ReadNegotiatedVersion(),
            Session = Session.Session,
            Sequence = Session.NextOutboundSequence,
            CorrelationId = correlationId,
            Origin = Session.ReadLocalOrigin(),
            Kind = kind,
            Payload = payload,
            Request = request,
            Response = response,
            Error = error
        };
    }

    internal UiBridgeEnvelope CommitFlowEnvelope(UiBridgeEnvelope envelope)
    {
        Session.NextOutboundSequence++;
        return envelope;
    }

    internal void CommitInboundEnvelope(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        Session.LastInboundSequence = envelope.Sequence;
        Session.LastActivityMilliseconds = nowMilliseconds;
    }

    internal BridgeMessageFlowResult ValidateFlowReadiness(long nowMilliseconds)
    {
        if (Session == null || Session.State != UiTransportState.Ready)
        {
            return CreateFlowFailure("bridge.invalid-state");
        }

        if (!Session.IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateFlowFailure("bridge.invalid-time");
        }

        return CreateFlowSuccess(false);
    }

    internal bool IsNotificationKind(UiBridgeMessageKind kind)
    {
        switch (kind)
        {
            case UiBridgeMessageKind.RenderSnapshot:
            case UiBridgeMessageKind.DomPatch:
            case UiBridgeMessageKind.Event:
            case UiBridgeMessageKind.Command:
            case UiBridgeMessageKind.StateMutation:
            case UiBridgeMessageKind.Validation:
            case UiBridgeMessageKind.Navigation:
            case UiBridgeMessageKind.Focus:
            case UiBridgeMessageKind.Measurement:
            case UiBridgeMessageKind.VirtualizationRange:
            case UiBridgeMessageKind.VirtualizationResult:
            case UiBridgeMessageKind.Lifecycle:
            case UiBridgeMessageKind.Log:
            case UiBridgeMessageKind.Diagnostic:
                return true;
            default:
                return false;
        }
    }

    internal bool IsRequestKind(UiBridgeMessageKind kind)
    {
        return kind == UiBridgeMessageKind.Request || kind == UiBridgeMessageKind.HostCall;
    }

    internal UiBridgeMessageKind ReadResponseKind(UiBridgeMessageKind requestKind)
    {
        return requestKind == UiBridgeMessageKind.HostCall ? UiBridgeMessageKind.HostResult : UiBridgeMessageKind.Response;
    }

    internal bool HasAnyCorrelation(string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return false;
        }

        int pendingIndex = 0;
        while (pendingIndex < PendingRequests.Count)
        {
            if (ValueComparer.TextEquals(PendingRequests[pendingIndex].CorrelationId, correlationId))
            {
                return true;
            }

            pendingIndex++;
        }

        int completedIndex = 0;
        while (completedIndex < CompletedRequests.Count)
        {
            if (ValueComparer.TextEquals(CompletedRequests[completedIndex].CorrelationId, correlationId))
            {
                return true;
            }

            completedIndex++;
        }

        return false;
    }

    internal BridgePendingRequest? FindPendingRequest(string? correlationId, bool inbound)
    {
        int index = 0;
        while (index < PendingRequests.Count)
        {
            BridgePendingRequest pending = PendingRequests[index];
            if (pending.Inbound == inbound && ValueComparer.TextEquals(pending.CorrelationId, correlationId))
            {
                return pending;
            }

            index++;
        }

        return null;
    }

    internal BridgeCompletedRequest? FindCompletedRequest(string? correlationId, bool inbound)
    {
        int index = 0;
        while (index < CompletedRequests.Count)
        {
            BridgeCompletedRequest completed = CompletedRequests[index];
            if (completed.Inbound == inbound && ValueComparer.TextEquals(completed.CorrelationId, correlationId))
            {
                return completed;
            }

            index++;
        }

        return null;
    }

    internal void AddCompletedRequest(string correlationId, bool inbound, UiBridgeMessageKind kind, UiBridgeResponse? response, UiBridgeError? error)
    {
        int limit = ReadFlowLimit();
        if (CompletedRequests.Count >= limit)
        {
            CompletedRequests.RemoveAt(0);
        }

        CompletedRequests.Add(new BridgeCompletedRequest
        {
            CorrelationId = correlationId,
            Inbound = inbound,
            Kind = kind,
            Response = CopyOptionalFlowResponse(response),
            Error = CopyOptionalFlowError(error)
        });
    }

    internal bool HasPendingAcknowledgement(int sequence)
    {
        int index = 0;
        while (index < PendingAcknowledgements.Count)
        {
            if (PendingAcknowledgements[index].Sequence == sequence)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal int CountExpiredRequests(long nowMilliseconds)
    {
        int count = 0;
        int index = 0;
        while (index < PendingRequests.Count)
        {
            if (nowMilliseconds >= PendingRequests[index].DeadlineMilliseconds)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal long CreateRequestDeadline(long nowMilliseconds, int timeoutMilliseconds)
    {
        int effectiveTimeout = timeoutMilliseconds;
        if (effectiveTimeout == 0)
        {
            UiBridgeOptions? options = Session.Options;
            effectiveTimeout = options == null ? 1 : options.TimeoutMilliseconds;
        }

        if (nowMilliseconds > long.MaxValue - effectiveTimeout)
        {
            return long.MaxValue;
        }

        return nowMilliseconds + effectiveTimeout;
    }

    internal int ReadFlowLimit()
    {
        UiBridgeOptions? options = Session.Options;
        return options == null ? 1 : options.MaximumPendingRequests;
    }

    internal int ReadMessageLimit()
    {
        UiBridgeOptions? options = Session.Options;
        return options == null ? 1 : options.MaximumMessageLength;
    }

    internal bool CanCreateFlowEnvelopes(int count)
    {
        return count >= 0 && Session.NextOutboundSequence <= int.MaxValue - count;
    }

    internal BridgeMessageFlowResult FaultFlowSequence()
    {
        Session.State = UiTransportState.Faulted;
        return CreateFlowFailure("bridge.sequence-exhausted");
    }

    internal UiBridgeRequest CopyFlowRequest(UiBridgeRequest source)
    {
        UiValueEntry[] sourceArguments = source.Arguments ?? Array.Empty<UiValueEntry>();
        UiValueEntry[] arguments = new UiValueEntry[sourceArguments.Length];
        int index = 0;
        while (index < sourceArguments.Length)
        {
            UiValueEntry sourceEntry = sourceArguments[index];
            arguments[index] = new UiValueEntry
            {
                Key = sourceEntry.Key,
                Value = ValueJsonCodec.CloneNode(sourceEntry.Value)
            };
            index++;
        }

        return new UiBridgeRequest
        {
            CorrelationId = source.CorrelationId,
            Operation = source.Operation,
            Arguments = arguments,
            TimeoutMilliseconds = source.TimeoutMilliseconds
        };
    }

    internal UiBridgeResponse? CopyOptionalFlowResponse(UiBridgeResponse? source)
    {
        if (source == null)
        {
            return null;
        }

        return new UiBridgeResponse
        {
            CorrelationId = source.CorrelationId,
            Success = source.Success,
            Value = CopyOptionalFlowValue(source.Value),
            Error = CopyOptionalFlowError(source.Error)
        };
    }

    internal UiBridgeError CopyFlowError(UiBridgeError source)
    {
        return new UiBridgeError
        {
            Code = source.Code,
            Message = source.Message,
            CorrelationId = source.CorrelationId,
            IsRetryable = source.IsRetryable
        };
    }

    internal UiBridgeError? CopyOptionalFlowError(UiBridgeError? source)
    {
        return source == null ? null : CopyFlowError(source);
    }

    internal UiValue? CopyOptionalFlowValue(UiValue? source)
    {
        return source == null ? null : ValueJsonCodec.CloneNode(source);
    }

    internal BridgeAcknowledgementParseResult CreateAcknowledgementFailure(string code)
    {
        return new BridgeAcknowledgementParseResult { Code = code };
    }

    internal BridgeMessageFlowResult CreateFlowSuccess(bool applied)
    {
        return new BridgeMessageFlowResult { Success = true, Applied = applied };
    }

    internal BridgeMessageFlowResult CreateFlowFailure(string code)
    {
        return new BridgeMessageFlowResult { Code = code };
    }
}
