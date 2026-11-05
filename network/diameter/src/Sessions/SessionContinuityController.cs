using System;
using System.Collections.Generic;

namespace diameter;

internal class SessionContinuityResult
{
    internal bool Accepted;
    internal bool Changed;
    internal bool RetryWithoutDestinationHost;
    internal bool AllowService;
    internal bool DisconnectRequired;
    internal bool FailoverRequired;
    internal bool Released;
    internal bool RestartInferred;
    internal DiameterSessionState PreviousState;
    internal DiameterSessionRecord? Session;
    internal DiameterMessage? Message;
    internal string Code = string.Empty;
    internal string Description = string.Empty;
}

internal class SessionContinuityBatchResult
{
    internal int MatchedSessions;
    internal int ChangedSessions;
    internal int ReleasedSessions;
    internal List<SessionContinuityResult> Results =
        new List<SessionContinuityResult>();
    internal string Code = string.Empty;
}

internal class SessionContinuityPolicy
{
    internal bool Valid;
    internal uint SessionBinding;
    internal int SessionServerFailover;
    internal bool OriginStateIdentifierPresent;
    internal uint OriginStateIdentifier;
    internal string Code = string.Empty;
}

internal class SessionContinuityController : IPeerRestartNotificationSink
{
    internal SessionTable Table;
    internal RouteSessionAffinityStore Affinities;
    internal IDiameterClock Clock;
    internal DiameterBaseAvpFactory AvpFactory =
        new DiameterBaseAvpFactory();
    internal SessionContinuityBatchResult LastRestartResult =
        new SessionContinuityBatchResult();

    internal SessionContinuityController(
        SessionTable table,
        RouteSessionAffinityStore affinities,
        IDiameterClock clock)
    {
        if (table == null || affinities == null || clock == null)
        {
            throw new ArgumentException(
                "Session storage, affinities, and a clock are required.");
        }

        Table = table;
        Affinities = affinities;
        Clock = clock;
    }

    internal SessionContinuityResult ApplySessionAuthorizationPolicy(
        string? sessionId,
        DiameterMessage? answer,
        string? routeIdentifier,
        DiameterIdentity? peerIdentity)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        if (record.Configuration.Role != DiameterSessionRole.Client ||
            answer == null || answer.Avps == null ||
            (answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            !ContainsContinuitySessionIdentifier(answer, record.SessionId))
        {
            throw new ArgumentException(
                "A client authorization answer for the session is required.");
        }

        string route = ValidateContinuityRouteIdentifier(routeIdentifier);
        DiameterIdentity peer = ValidateContinuityPeerIdentity(peerIdentity);
        SessionContinuityPolicy policy = ReadSessionContinuityPolicy(
            answer,
            peer);
        if (!policy.Valid)
        {
            throw new ArgumentException(
                "The authorization answer contains invalid continuity policy.");
        }

        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        record.Configuration.SessionBinding = policy.SessionBinding;
        record.Configuration.SessionServerFailover =
            policy.SessionServerFailover;
        record.RouteIdentifier = route;
        record.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(peer);
        if (policy.OriginStateIdentifierPresent)
        {
            record.PeerOriginStateIdentifier =
                policy.OriginStateIdentifier;
            record.PeerOriginStateIdentifierPresent = true;
        }
        record.UpdatedAt = now;
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        Affinities.StoreAffinityRoute(saved.SessionId, route);
        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            "session-continuity-policy-applied");
        result.Description =
            "Session binding, failover, origin state, and route affinity were retained.";
        return result;
    }

    internal SessionContinuityResult RebindSessionRoute(
        string? sessionId,
        string? routeIdentifier,
        DiameterIdentity? peerIdentity)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        string route = ValidateContinuityRouteIdentifier(routeIdentifier);
        DiameterIdentity peer = ValidateContinuityPeerIdentity(peerIdentity);
        DiameterSessionState previous = record.State;
        record.RouteIdentifier = route;
        record.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(peer);
        record.UpdatedAt = Clock.ReadUtcTime();
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        Affinities.StoreAffinityRoute(saved.SessionId, route);
        return CreateContinuityResult(
            saved,
            previous,
            true,
            "session-route-rebound");
    }

    internal SessionContinuityResult PrepareSessionReAuthRequest(
        string? sessionId,
        DiameterMessage? request)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        return PrepareBoundSessionRequest(
            record,
            request,
            1U,
            0U,
            "session-reauth-request-prepared");
    }

    internal SessionContinuityResult PrepareSessionTerminationRequest(
        string? sessionId,
        DiameterMessage? request)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        return PrepareBoundSessionRequest(
            record,
            request,
            2U,
            DiameterCommandCodes.SessionTermination,
            "session-termination-request-prepared");
    }

    internal SessionContinuityResult PrepareSessionAccountingRequest(
        string? sessionId,
        DiameterMessage? request)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        return PrepareBoundSessionRequest(
            record,
            request,
            4U,
            DiameterCommandCodes.Accounting,
            "session-accounting-request-prepared");
    }

    internal SessionContinuityResult HandleSessionReAuthDeliveryFailure(
        string? sessionId,
        bool retryAlreadyAttempted)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        if (record.Configuration.Role != DiameterSessionRole.Client ||
            record.State != DiameterSessionState.Reauthorizing)
        {
            throw new DiameterSessionException(
                "A client re-authorization request is not pending.");
        }

        int policy = ReadEffectiveSessionFailoverPolicy(record);
        if ((policy == 1 || policy == 3) && !retryAlreadyAttempted)
        {
            return PrepareSessionFailoverRetry(
                record,
                "session-reauth-failover-retry");
        }

        if (policy == 2 || (policy == 3 && retryAlreadyAttempted))
        {
            DiameterSessionState previous = record.State;
            DateTimeOffset now = Clock.ReadUtcTime();
            record.State = DiameterSessionState.Open;
            record.StateChangedAt = now;
            record.UpdatedAt = now;
            record.PendingRequestIdentifier = 0;
            record.ReauthorizationRequested = false;
            DiameterSessionRecord saved = Table.SaveSessionRecord(
                record,
                record.Version);
            SessionContinuityResult allowed = CreateContinuityResult(
                saved,
                previous,
                true,
                "session-reauth-failure-service-allowed");
            allowed.AllowService = true;
            return allowed;
        }

        return FailContinuitySession(
            record,
            "session-reauth-failure-service-refused");
    }

    internal SessionContinuityResult HandleSessionTerminationDeliveryFailure(
        string? sessionId,
        bool retryAlreadyAttempted)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        if (record.Configuration.Role != DiameterSessionRole.Client ||
            record.State != DiameterSessionState.Terminating)
        {
            throw new DiameterSessionException(
                "A client termination request is not pending.");
        }

        int policy = ReadEffectiveSessionFailoverPolicy(record);
        if ((policy == 1 || policy == 3) && !retryAlreadyAttempted)
        {
            return PrepareSessionFailoverRetry(
                record,
                "session-termination-failover-retry");
        }

        return ReleaseSessionRecord(
            record,
            record.TerminationCausePresent ?
                record.TerminationCause :
                DiameterTerminationCause.LinkBroken,
            "session-termination-delivery-failed");
    }

    internal SessionContinuityBatchResult ProcessSessionPeerLoss(
        DiameterIdentity? peerIdentity)
    {
        DiameterIdentity peer = ValidateContinuityPeerIdentity(peerIdentity);
        string peerKey = NormalizeContinuityIdentity(peer);
        List<DiameterSessionRecord> sessions = Table.ReadSessionRecords();
        SessionContinuityBatchResult result =
            new SessionContinuityBatchResult();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterSessionRecord record = sessions[index];
            if (MatchesContinuityPeer(record, peerKey))
            {
                result.MatchedSessions = result.MatchedSessions + 1;
                SessionContinuityResult changed;
                if (record.Configuration.Role ==
                        DiameterSessionRole.Client &&
                    record.State == DiameterSessionState.Reauthorizing)
                {
                    changed = HandleSessionReAuthDeliveryFailure(
                        record.SessionId,
                        false);
                }
                else if (record.Configuration.Role ==
                        DiameterSessionRole.Client &&
                    record.State == DiameterSessionState.Terminating)
                {
                    changed = HandleSessionTerminationDeliveryFailure(
                        record.SessionId,
                        false);
                }
                else
                {
                    changed = ClearSessionAffinityForPeerLoss(record);
                }

                result.Results.Add(changed);
                if (changed.Changed)
                {
                    result.ChangedSessions = result.ChangedSessions + 1;
                }

                if (changed.Released)
                {
                    result.ReleasedSessions = result.ReleasedSessions + 1;
                }
            }

            index = index + 1;
        }

        result.Code = result.MatchedSessions > 0 ?
            "session-peer-loss-processed" :
            "session-peer-loss-no-match";
        return result;
    }

    internal SessionContinuityBatchResult ProcessSessionPeerRestart(
        DiameterPeerEvent? peerEvent)
    {
        ValidateContinuityRestartEvent(peerEvent);
        if (peerEvent == null)
        {
            throw new ArgumentException("A peer restart event is required.");
        }

        string peerKey = NormalizeContinuityIdentity(
            peerEvent.Peer.Configuration.Identity);
        List<DiameterSessionRecord> sessions = Table.ReadSessionRecords();
        SessionContinuityBatchResult result =
            new SessionContinuityBatchResult();
        int index = 0;
        while (index < sessions.Count)
        {
            DiameterSessionRecord record = sessions[index];
            if (MatchesContinuityPeer(record, peerKey))
            {
                result.MatchedSessions = result.MatchedSessions + 1;
                bool inferred = record.PeerOriginStateIdentifierPresent &&
                    record.PeerOriginStateIdentifier > 0U &&
                    record.PeerOriginStateIdentifier <
                        peerEvent.CurrentOriginStateId;
                SessionContinuityResult changed;
                if (inferred && record.Configuration.Role ==
                    DiameterSessionRole.Server)
                {
                    changed = ReleaseSessionRecord(
                        record,
                        DiameterTerminationCause.LinkBroken,
                        "session-peer-restart-inferred-termination");
                    changed.RestartInferred = true;
                }
                else
                {
                    changed = RecordSessionPeerRestart(
                        record,
                        peerEvent.CurrentOriginStateId,
                        inferred);
                }

                result.Results.Add(changed);
                if (changed.Changed)
                {
                    result.ChangedSessions = result.ChangedSessions + 1;
                }

                if (changed.Released)
                {
                    result.ReleasedSessions = result.ReleasedSessions + 1;
                }
            }

            index = index + 1;
        }

        result.Code = result.MatchedSessions > 0 ?
            "session-peer-restart-processed" :
            "session-peer-restart-no-match";
        return result;
    }

    /// <summary>Applies one peer restart notification to retained sessions.</summary>
    /// <param name="peerEvent">The observed peer restart.</param>
    public void HandlePeerRestart(DiameterPeerEvent peerEvent)
    {
        LastRestartResult = ProcessSessionPeerRestart(peerEvent);
    }

    internal SessionContinuityResult ReleaseSessionState(
        string? sessionId,
        DiameterTerminationCause cause)
    {
        DiameterSessionRecord record = ReadRequiredContinuitySession(
            sessionId);
        int causeValue = (int)cause;
        if (causeValue < (int)DiameterTerminationCause.Logout ||
            causeValue > (int)DiameterTerminationCause.SessionTimeout)
        {
            throw new ArgumentOutOfRangeException("cause");
        }

        return ReleaseSessionRecord(
            record,
            cause,
            "session-explicitly-released");
    }

    internal SessionContinuityResult PrepareBoundSessionRequest(
        DiameterSessionRecord record,
        DiameterMessage? request,
        uint bindingBit,
        uint requiredCommandCode,
        string code)
    {
        if (record.Configuration.Role != DiameterSessionRole.Client ||
            request == null || request.Avps == null ||
            (request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (requiredCommandCode != 0U &&
            request.CommandCode != requiredCommandCode) ||
            !ContainsContinuitySessionIdentifier(
                request,
                record.SessionId))
        {
            throw new ArgumentException(
                "A client request correlated to the session is required.");
        }

        DiameterMessage? ownedPrepared =
            SessionRecordCloner.CloneOptionalSessionMessage(request);
        if (ownedPrepared == null)
        {
            throw new ArgumentException(
                "A client request correlated to the session is required.");
        }

        DiameterMessage prepared = ownedPrepared;
        bool omitDestination =
            (record.Configuration.SessionBinding & bindingBit) != 0U;
        RemoveContinuityDestinationHosts(prepared);
        if (!omitDestination)
        {
            if (record.PeerIdentity == null)
            {
                if (bindingBit != 4U)
                {
                    throw new DiameterSessionException(
                        "The bound authorization server is not known.");
                }
            }
            else
            {
                prepared.Avps.Add(
                    AvpFactory.CreateDestinationHostAvp(
                        record.PeerIdentity));
            }
        }

        SessionContinuityResult result = CreateContinuityResult(
            record,
            record.State,
            false,
            code);
        result.Message = prepared;
        result.Description = omitDestination ?
            "Session binding omitted Destination-Host." :
            "The bound authorization server was addressed directly.";
        return result;
    }

    internal SessionContinuityResult PrepareSessionFailoverRetry(
        DiameterSessionRecord record,
        string code)
    {
        if (record.LastRequest == null)
        {
            throw new DiameterSessionException(
                "The failed session request is not retained.");
        }

        DiameterSessionState previous = record.State;
        DiameterMessage? ownedRetry =
            SessionRecordCloner.CloneOptionalSessionMessage(
                record.LastRequest);
        if (ownedRetry == null)
        {
            throw new DiameterSessionException(
                "The failed session request is not retained.");
        }

        DiameterMessage retry = ownedRetry;
        RemoveContinuityDestinationHosts(retry);
        Affinities.RemoveAffinityRoute(record.SessionId);
        record.RouteIdentifier = string.Empty;
        record.UpdatedAt = Clock.ReadUtcTime();
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            code);
        result.RetryWithoutDestinationHost = true;
        result.FailoverRequired = true;
        result.Message = retry;
        return result;
    }

    internal SessionContinuityResult ClearSessionAffinityForPeerLoss(
        DiameterSessionRecord record)
    {
        DiameterSessionState previous = record.State;
        Affinities.RemoveAffinityRoute(record.SessionId);
        record.RouteIdentifier = string.Empty;
        record.UpdatedAt = Clock.ReadUtcTime();
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            "session-peer-loss-affinity-cleared");
        result.FailoverRequired =
            record.Configuration.AllowFailover &&
            record.Configuration.SessionServerFailover != 0;
        return result;
    }

    internal SessionContinuityResult RecordSessionPeerRestart(
        DiameterSessionRecord record,
        uint originStateIdentifier,
        bool inferred)
    {
        DiameterSessionState previous = record.State;
        Affinities.RemoveAffinityRoute(record.SessionId);
        record.RouteIdentifier = string.Empty;
        record.PeerOriginStateIdentifier = originStateIdentifier;
        record.PeerOriginStateIdentifierPresent =
            originStateIdentifier != 0U;
        record.UpdatedAt = Clock.ReadUtcTime();
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            inferred ?
                "session-peer-restart-observed" :
                "session-peer-origin-state-recorded");
        result.RestartInferred = inferred;
        result.FailoverRequired = inferred &&
            record.Configuration.Role == DiameterSessionRole.Client &&
            record.Configuration.AllowFailover;
        return result;
    }

    internal SessionContinuityResult FailContinuitySession(
        DiameterSessionRecord record,
        string code)
    {
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        Affinities.RemoveAffinityRoute(record.SessionId);
        record.State = DiameterSessionState.Failed;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.TerminationCause = DiameterTerminationCause.LinkBroken;
        record.TerminationCausePresent = true;
        ClearContinuityTimers(record);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            code);
        result.DisconnectRequired = true;
        return result;
    }

    internal SessionContinuityResult ReleaseSessionRecord(
        DiameterSessionRecord record,
        DiameterTerminationCause cause,
        string code)
    {
        DiameterSessionState previous = record.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        Affinities.RemoveAffinityRoute(record.SessionId);
        record.State = DiameterSessionState.Closed;
        record.StateChangedAt = now;
        record.UpdatedAt = now;
        record.PendingRequestIdentifier = 0;
        record.TerminationCause = cause;
        record.TerminationCausePresent = true;
        record.ReleaseRequested = true;
        record.ReauthorizationRequested = false;
        ClearContinuityTimers(record);
        DiameterSessionRecord saved = Table.SaveSessionRecord(
            record,
            record.Version);
        if (!Table.RemoveSessionRecord(saved.SessionId, saved.Version))
        {
            throw new DiameterSessionException(
                "The released session could not be removed.");
        }

        SessionContinuityResult result = CreateContinuityResult(
            saved,
            previous,
            true,
            code);
        result.Released = true;
        result.DisconnectRequired =
            record.Configuration.Role == DiameterSessionRole.Client;
        return result;
    }

    internal SessionContinuityPolicy ReadSessionContinuityPolicy(
        DiameterMessage answer,
        DiameterIdentity peerIdentity)
    {
        SessionContinuityPolicy result = new SessionContinuityPolicy();
        int bindingCount = 0;
        int failoverCount = 0;
        int originStateCount = 0;
        int originHostCount = 0;
        string originHost = string.Empty;
        bool malformed = false;
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf)
            {
                if ((avp.Code == DiameterAvpCodes.SessionBinding ||
                    avp.Code ==
                        DiameterAvpCodes.SessionServerFailover ||
                    avp.Code == DiameterAvpCodes.OriginStateId) &&
                    (avp.Data == null || avp.Data.Length != 4))
                {
                    malformed = true;
                }

                if (avp.Code == DiameterAvpCodes.SessionBinding)
                {
                    bindingCount = bindingCount + 1;
                    result.SessionBinding =
                        ReadContinuityUnsigned32(avp);
                }
                else if (avp.Code ==
                    DiameterAvpCodes.SessionServerFailover)
                {
                    failoverCount = failoverCount + 1;
                    result.SessionServerFailover = (int)
                        ReadContinuityUnsigned32(avp);
                }
                else if (avp.Code == DiameterAvpCodes.OriginStateId)
                {
                    originStateCount = originStateCount + 1;
                    result.OriginStateIdentifier =
                        ReadContinuityUnsigned32(avp);
                    result.OriginStateIdentifierPresent = true;
                }
                else if (avp.Code == DiameterAvpCodes.OriginHost)
                {
                    originHostCount = originHostCount + 1;
                    DiameterValueResult decoded =
                        ValueCodecs.DecodeDiameterIdentity(avp.Data);
                    if (decoded.Status != DiameterCodecStatus.Success)
                    {
                        malformed = true;
                    }
                    else
                    {
                        originHost = decoded.Text;
                    }
                }
            }

            index = index + 1;
        }

        if (malformed || bindingCount > 1 || failoverCount > 1 ||
            originStateCount > 1 || result.SessionBinding > 7U ||
            result.SessionServerFailover < 0 ||
            result.SessionServerFailover > 3 ||
            (failoverCount == 1 && bindingCount == 1 &&
            result.SessionBinding == 7U) ||
            (originStateCount == 1 &&
            (originHostCount != 1 || originHost !=
                NormalizeContinuityIdentity(peerIdentity))))
        {
            result.Code = "session-continuity-policy-invalid";
            return result;
        }

        result.Valid = true;
        result.Code = "session-continuity-policy-valid";
        return result;
    }

    internal uint ReadContinuityUnsigned32(DiameterAvp avp)
    {
        if (avp.Data == null || avp.Data.Length != 4)
        {
            return uint.MaxValue;
        }

        return NetworkByteOrder.ReadUInt32(avp.Data);
    }

    internal int ReadEffectiveSessionFailoverPolicy(
        DiameterSessionRecord record)
    {
        if (!record.Configuration.AllowFailover)
        {
            return 0;
        }

        return record.Configuration.SessionServerFailover;
    }

    internal DiameterSessionRecord ReadRequiredContinuitySession(
        string? sessionId)
    {
        DiameterSessionRecord? record = Table.ReadSessionRecord(sessionId);
        if (record == null || record.Configuration.Kind !=
            DiameterSessionKind.StatefulAuthorization)
        {
            throw new DiameterSessionException(
                "A retained stateful authorization session was not found.");
        }

        return record;
    }

    internal string ValidateContinuityRouteIdentifier(
        string? routeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(routeIdentifier) ||
            ValueCodecs.FindInvalidUtf16Index(routeIdentifier) >= 0)
        {
            throw new ArgumentException(
                "A valid route identifier is required.");
        }

        return routeIdentifier;
    }

    internal DiameterIdentity ValidateContinuityPeerIdentity(
        DiameterIdentity? peerIdentity)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid session peer identity is required.");
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = normalized.Text;
        return result;
    }

    internal string NormalizeContinuityIdentity(
        DiameterIdentity identity)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid Diameter identity is required.");
        }

        return normalized.Text;
    }

    internal bool MatchesContinuityPeer(
        DiameterSessionRecord record,
        string peerKey)
    {
        if (record.PeerIdentity == null)
        {
            return false;
        }

        return NormalizeContinuityIdentity(record.PeerIdentity) == peerKey;
    }

    internal void ValidateContinuityRestartEvent(
        DiameterPeerEvent? peerEvent)
    {
        if (peerEvent == null ||
            peerEvent.Kind != DiameterPeerEventKind.RestartDetected ||
            peerEvent.Peer == null ||
            peerEvent.Peer.Configuration == null ||
            peerEvent.Peer.Configuration.Identity == null ||
            peerEvent.CurrentOriginStateId == 0U ||
            peerEvent.CurrentOriginStateId <=
                peerEvent.PreviousOriginStateId ||
            ValueCodecs.NormalizeDiameterIdentity(
                peerEvent.Peer.Configuration.Identity).Status !=
                    DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid peer restart event is required.");
        }
    }

    internal bool ContainsContinuitySessionIdentifier(
        DiameterMessage message,
        string sessionId)
    {
        int count = 0;
        string value = string.Empty;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.SessionId)
            {
                count = count + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return false;
                }

                value = decoded.Text;
            }

            index = index + 1;
        }

        return count == 1 && value == sessionId;
    }

    internal void RemoveContinuityDestinationHosts(
        DiameterMessage message)
    {
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationHost)
            {
                message.Avps.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal void ClearContinuityTimers(DiameterSessionRecord record)
    {
        record.AuthorizationDeadlinePresent = false;
        record.GraceDeadlinePresent = false;
        record.SessionDeadlinePresent = false;
        record.MultiRoundDeadlinePresent = false;
    }

    internal SessionContinuityResult CreateContinuityResult(
        DiameterSessionRecord record,
        DiameterSessionState previous,
        bool changed,
        string code)
    {
        SessionContinuityResult result = new SessionContinuityResult();
        result.Accepted = true;
        result.Changed = changed;
        result.PreviousState = previous;
        result.Session = SessionRecordCloner.CloneSessionRecord(record);
        result.Code = code;
        result.Description =
            "The session continuity operation completed.";
        return result;
    }
}
