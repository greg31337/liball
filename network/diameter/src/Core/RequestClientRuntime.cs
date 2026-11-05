using System;
using System.Collections.Generic;

namespace diameter;

internal class RequestClientRouteContextResult
{
    internal bool Valid;
    internal DiameterRouteRequest Request = new DiameterRouteRequest();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class RequestClientDeliveryTarget
{
    internal bool Available;
    internal DiameterIdentity PeerIdentity = new DiameterIdentity();
    internal DiameterMessage Message = new DiameterMessage();
    internal DiameterSendStatus Status;
    internal string Code = string.Empty;
    internal string MessageText = string.Empty;
}

internal class RequestClientSendAttempt
{
    internal bool Delivered;
    internal int TransferredCount;
    internal DiameterTransportStatus Status;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class RequestClientFailoverAttempt
{
    internal bool Retried;
    internal bool Delivered;
    internal DiameterPendingRequestSnapshot? PendingRequest;
    internal DiameterIdentity? PeerIdentity;
    internal string Code = string.Empty;
}

internal class RequestClientRuntime
{
    internal IDiameterRouter Router;
    internal IDiameterPeerManager PeerManager;
    internal IDiameterMessageCodec Codec;
    internal IDiameterIdentifierSource IdentifierSource;
    internal IDiameterClock Clock;
    internal PendingRequestTable PendingRequests;
    internal AnswerCorrelator Answers;
    internal RequestFailoverController Failover;
    internal DiameterOverloadAdmissionController OverloadAdmission =
        new DiameterOverloadAdmissionController();

    internal RequestClientRuntime(
        DiameterNodeConfiguration configuration,
        IDiameterRouter router,
        IDiameterPeerManager peerManager,
        IDiameterMessageCodec codec,
        IDiameterIdentifierSource identifierSource,
        IDiameterClock clock)
    {
        if (configuration == null || router == null ||
            peerManager == null || codec == null ||
            identifierSource == null || clock == null)
        {
            throw new ArgumentException(
                "Request client configuration and dependencies are required.");
        }

        configuration.ValidateConfiguration();
        Router = router;
        PeerManager = peerManager;
        Codec = codec;
        IdentifierSource = identifierSource;
        Clock = clock;
        PendingRequests = new PendingRequestTable(
            clock,
            configuration.Limits.MaximumPendingRequests,
            configuration.Timers.DuplicateCacheLifetimeMilliseconds);
        Answers = new AnswerCorrelator(PendingRequests);
        Failover = new RequestFailoverController(
            PendingRequests,
            identifierSource);
    }

    internal DiameterSendResult SendRequest(
        DiameterSendRequest request,
        IDiameterAnswerHandler? handler)
    {
        request.ValidateSendRequest();
        DiameterSendRequest owned =
            PendingRequests.ClonePendingSendRequest(request);
        owned.Request.HopByHopIdentifier =
            IdentifierSource.CreateHopByHopIdentifier();
        owned.Request.EndToEndIdentifier =
            IdentifierSource.CreateEndToEndIdentifier();
        RequestClientDeliveryTarget target =
            SelectInitialDeliveryTarget(owned);
        if (!target.Available)
        {
            return CreateSendFailure(
                target.Status,
                target.Code,
                target.MessageText);
        }

        DiameterEncodeResult encoded =
            Codec.EncodeMessageToArray(target.Message);
        if (encoded == null ||
            encoded.Status != DiameterCodecStatus.Success ||
            encoded.Data == null || encoded.WrittenCount <= 0 ||
            encoded.WrittenCount != encoded.Data.Length)
        {
            return CreateSendFailure(
                DiameterSendStatus.Failed,
                "request-encoding-failed",
                "The outbound Diameter request could not be encoded.");
        }

        PendingRequestTableResult registration =
            PendingRequests.RegisterPendingRequest(
                owned,
                target.PeerIdentity,
                target.Message,
                handler,
                false);
        if (!registration.Accepted ||
            registration.PendingRequest == null)
        {
            DiameterSendStatus status = registration.CapacityReached ?
                DiameterSendStatus.PendingLimitReached :
                DiameterSendStatus.Failed;
            return CreateSendFailure(
                status,
                registration.Code,
                "The request could not enter pending tracking.");
        }

        DiameterPendingRequestSnapshot pending =
            registration.PendingRequest;
        RequestClientSendAttempt attempt = SendEncodedRequest(
            target.PeerIdentity,
            encoded.Data,
            owned.TimeoutMilliseconds);
        bool uncertainDelivery = attempt.TransferredCount > 0;
        PendingRequestTableResult delivery =
            PendingRequests.RecordPendingRequestDelivery(
                pending.RequestIdentifier,
                pending.HopByHopIdentifier,
                attempt.Delivered || uncertainDelivery);
        if (attempt.Delivered && delivery.PendingRequest != null)
        {
            return CreateAcceptedSendResult(
                delivery.PendingRequest,
                target.PeerIdentity);
        }

        if (uncertainDelivery)
        {
            PendingRequestTableResult latest =
                PendingRequests.ReadPendingRequestByIdentifier(
                    pending.RequestIdentifier);
            if (latest.PendingRequest != null)
            {
                RequestClientFailoverAttempt retry =
                    AttemptPendingRequestFailover(
                        latest.PendingRequest);
                if (retry.Delivered && retry.PendingRequest != null &&
                    retry.PeerIdentity != null)
                {
                    return CreateAcceptedSendResult(
                        retry.PendingRequest,
                        retry.PeerIdentity);
                }
            }
        }

        bool peerFailed = attempt.Status == DiameterTransportStatus.Closed ||
            attempt.Status == DiameterTransportStatus.EndOfStream ||
            attempt.Status == DiameterTransportStatus.Failed ||
            attempt.Status == DiameterTransportStatus.TimedOut;
        PendingRequestTableResult failed =
            PendingRequests.FailPendingRequest(
                pending.RequestIdentifier,
                peerFailed,
                ReadSendFailureCode(attempt));
        DispatchPendingOutcome(failed);
        return CreateSendFailure(
            MapTransportSendStatus(attempt.Status),
            ReadSendFailureCode(attempt),
            ReadSendFailureMessage(attempt));
    }

    internal void ConfigureDiameterOverloadAdmission(
        IDiameterOverloadManager? manager)
    {
        OverloadAdmission.ConfigureDiameterOverloadAdmission(manager);
    }

    internal DiameterAnswerResult CancelRequest(long requestIdentifier)
    {
        PendingRequestTableResult cancelled =
            PendingRequests.CancelPendingRequest(requestIdentifier);
        if (cancelled.AnswerResult == null)
        {
            DiameterAnswerResult missing = new DiameterAnswerResult();
            missing.RequestIdentifier = requestIdentifier;
            missing.Code = "request-not-pending";
            missing.Message = "The request is not pending.";
            return missing;
        }

        DiameterAnswerResult result = CloneAnswerResult(
            cancelled.AnswerResult);
        result.Message = "The pending request was cancelled.";
        DispatchPendingOutcome(cancelled);
        return result;
    }

    internal List<DiameterPendingRequestSnapshot> ReadPendingRequests()
    {
        return PendingRequests.ReadPendingRequestSnapshots();
    }

    internal DiameterAnswerResult HandleReceivedAnswer(
        DiameterIdentity? peerIdentity,
        DiameterMessage? answer)
    {
        DiameterValueResult peer =
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        if (peer.Status != DiameterCodecStatus.Success || answer == null)
        {
            DiameterAnswerResult invalid = new DiameterAnswerResult();
            invalid.Code = "answer-input-invalid";
            invalid.Message =
                "A valid answer peer and Diameter answer are required.";
            return invalid;
        }

        AnswerCorrelationResult correlation =
            Answers.CorrelateDiameterAnswer(peer.Text, answer);
        if (correlation.AnswerResult == null)
        {
            DiameterAnswerResult rejected = new DiameterAnswerResult();
            rejected.Code = correlation.Code;
            rejected.Message =
                "The Diameter answer was not correlated to a pending request.";
            return rejected;
        }

        DiameterAnswerResult result = CloneAnswerResult(
            correlation.AnswerResult);
        DispatchAnswerHandler(
            correlation.Handler,
            correlation.AnswerResult);
        return result;
    }

    internal List<DiameterAnswerResult> ExpireRequests()
    {
        List<PendingRequestTableResult> expired =
            PendingRequests.ExpirePendingRequests();
        List<DiameterAnswerResult> result =
            new List<DiameterAnswerResult>();
        int index = 0;
        while (index < expired.Count)
        {
            DiameterAnswerResult? expiredAnswer =
                expired[index].AnswerResult;
            if (expiredAnswer != null)
            {
                DiameterAnswerResult answer = CloneAnswerResult(
                    expiredAnswer);
                answer.Message = "The pending request timed out.";
                result.Add(answer);
                DispatchPendingOutcome(expired[index]);
            }

            index = index + 1;
        }

        return result;
    }

    internal List<DiameterAnswerResult> HandlePeerFailure(
        DiameterIdentity? peerIdentity)
    {
        DiameterValueResult failedPeer =
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        List<DiameterAnswerResult> results =
            new List<DiameterAnswerResult>();
        if (failedPeer.Status != DiameterCodecStatus.Success)
        {
            return results;
        }

        List<DiameterPendingRequestSnapshot> snapshots =
            PendingRequests.ReadPendingRequestSnapshots();
        int index = 0;
        while (index < snapshots.Count)
        {
            DiameterPendingRequestSnapshot pending = snapshots[index];
            DiameterValueResult pendingPeer =
                ValueCodecs.NormalizeDiameterIdentity(
                    pending.PeerIdentity);
            if (pendingPeer.Status == DiameterCodecStatus.Success &&
                pendingPeer.Text == failedPeer.Text)
            {
                RequestClientFailoverAttempt retry =
                    AttemptPendingRequestFailover(pending);
                if (!retry.Delivered)
                {
                    PendingRequestTableResult failed =
                        PendingRequests.FailPendingRequest(
                            pending.RequestIdentifier,
                            true,
                            "request-peer-failed");
                    if (failed.AnswerResult != null)
                    {
                        DiameterAnswerResult answer = CloneAnswerResult(
                            failed.AnswerResult);
                        answer.Message =
                            "The request peer failed before an answer arrived.";
                        results.Add(answer);
                    }

                    DispatchPendingOutcome(failed);
                }
            }

            index = index + 1;
        }

        return results;
    }

    internal RequestClientDeliveryTarget SelectInitialDeliveryTarget(
        DiameterSendRequest request)
    {
        if (request.PreferredPeer != null)
        {
            DiameterPeerSnapshot? peer =
                PeerManager.ReadDiameterPeerSnapshot(
                    request.PreferredPeer);
            if (peer == null || !peer.Available ||
                !SupportsPeerApplication(
                    peer,
                    request.Request.ApplicationId))
            {
                return CreateUnavailableTarget(
                    DiameterSendStatus.PeerUnavailable,
                    "preferred-peer-unavailable",
                    "The preferred peer is not available for the application.");
            }

            RequestClientDeliveryTarget preferred =
                new RequestClientDeliveryTarget();
            preferred.Available = true;
            preferred.PeerIdentity = CloneIdentity(
                peer.Configuration.Identity);
            preferred.Message = CloneMessage(request.Request);
            preferred.Status = DiameterSendStatus.Accepted;
            preferred.Code = "preferred-peer-selected";
            return ApplyPreferredPeerOverloadAdmission(
                request,
                preferred);
        }

        RequestClientRouteContextResult context =
            BuildRouteRequest(request.Request);
        if (!context.Valid)
        {
            return CreateUnavailableTarget(
                DiameterSendStatus.Failed,
                context.Code,
                context.Message);
        }

        return SelectAdmittedRouteTarget(
            context.Request,
            request.CorrelationText);
    }

    internal RequestClientFailoverAttempt AttemptPendingRequestFailover(
        DiameterPendingRequestSnapshot pending)
    {
        RequestClientFailoverAttempt result =
            new RequestClientFailoverAttempt();
        if (!pending.SendRequest.AllowFailover ||
            !pending.SendRequest.AllowRetransmission ||
            !pending.Delivered)
        {
            result.Code = "request-failover-ineligible";
            return result;
        }

        RequestClientRouteContextResult context =
            BuildRouteRequest(pending.OutboundMessage);
        if (!context.Valid)
        {
            result.Code = context.Code;
            return result;
        }

        context.Request.DestinationHost = null;
        context.Request.ExcludedPeers.Add(CloneIdentity(
            pending.PeerIdentity));
        RequestClientDeliveryTarget target =
            SelectAdmittedRouteTarget(
                context.Request,
                pending.SendRequest.CorrelationText);
        if (!target.Available)
        {
            result.Code = target.Code.Length == 0 ?
                "request-alternate-route-unavailable" : target.Code;
            return result;
        }

        RequestFailoverPolicy policy = new RequestFailoverPolicy();
        policy.RemoveDestinationHost = true;
        policy.RoutedMessage = target.Message;
        RequestFailoverResult failover =
            Failover.FailoverPendingRequest(
                pending.RequestIdentifier,
                target.PeerIdentity,
                policy);
        if (!failover.Retried || failover.Message == null ||
            failover.PeerIdentity == null ||
            failover.PendingRequest == null)
        {
            result.Code = failover.Code;
            return result;
        }

        result.Retried = true;
        DiameterEncodeResult encoded =
            Codec.EncodeMessageToArray(failover.Message);
        if (encoded == null ||
            encoded.Status != DiameterCodecStatus.Success ||
            encoded.Data == null || encoded.WrittenCount <= 0 ||
            encoded.WrittenCount != encoded.Data.Length)
        {
            result.Code = "request-retransmission-encoding-failed";
            return result;
        }

        int timeout = ReadRemainingTimeout(
            failover.PendingRequest);
        if (timeout <= 0)
        {
            result.Code = "request-deadline-expired";
            return result;
        }

        RequestClientSendAttempt sent = SendEncodedRequest(
            failover.PeerIdentity,
            encoded.Data,
            timeout);
        PendingRequestTableResult delivery =
            PendingRequests.RecordPendingRequestDelivery(
                failover.PendingRequest.RequestIdentifier,
                failover.PendingRequest.HopByHopIdentifier,
                sent.Delivered || sent.TransferredCount > 0);
        result.Delivered = sent.Delivered;
        result.PendingRequest = delivery.PendingRequest;
        result.PeerIdentity = CloneIdentity(failover.PeerIdentity);
        result.Code = sent.Delivered ?
            "request-failover-delivered" :
            ReadSendFailureCode(sent);
        return result;
    }

    internal RequestClientDeliveryTarget ApplyPreferredPeerOverloadAdmission(
        DiameterSendRequest request,
        RequestClientDeliveryTarget preferred)
    {
        if (OverloadAdmission.Manager == null ||
            OverloadAdmission.IsDiameterWatchdog(request.Request))
        {
            return preferred;
        }

        RequestClientRouteContextResult context =
            BuildRouteRequest(request.Request);
        if (!context.Valid)
        {
            return CreateUnavailableTarget(
                DiameterSendStatus.Failed,
                context.Code,
                context.Message);
        }

        DiameterRouteCandidate candidate =
            CreatePreferredPeerRouteCandidate(preferred.PeerIdentity);
        DiameterAdmissionDecision admission =
            OverloadAdmission.EvaluateDiameterRouteAdmission(
                context.Request,
                candidate,
                request.AllowFailover,
                request.CorrelationText);
        if (admission.Admitted)
        {
            return preferred;
        }

        if (admission.Action == DiameterAdmissionAction.Divert &&
            request.AllowFailover)
        {
            context.Request.DestinationHost = null;
            context.Request.ExcludedPeers.Add(CloneIdentity(
                preferred.PeerIdentity));
            return SelectAdmittedRouteTarget(
                context.Request,
                request.CorrelationText);
        }

        return CreateOverloadRejectedTarget(admission);
    }

    internal RequestClientDeliveryTarget SelectAdmittedRouteTarget(
        DiameterRouteRequest request,
        string correlationText)
    {
        bool diverted = false;
        int remainingSelections =
            PeerManager.ReadDiameterPeerSnapshots().Count + 1;
        while (remainingSelections > 0)
        {
            DiameterRouteDecision decision =
                Router.SelectDiameterRoute(request);
            if (!decision.Routed || decision.SelectedCandidate == null ||
                decision.SelectedCandidate.PeerIdentity == null ||
                decision.RoutedMessage == null)
            {
                if (diverted)
                {
                    return CreateUnavailableTarget(
                        DiameterSendStatus.Overloaded,
                        "overload-diversion-exhausted",
                        "Every usable route candidate was rejected by overload admission.");
                }

                DiameterSendStatus status = decision.ResultCode ==
                    DiameterResultCodes.TooBusy ?
                    DiameterSendStatus.Overloaded :
                    DiameterSendStatus.NoRoute;
                return CreateUnavailableTarget(
                    status,
                    decision.Code,
                    decision.Message);
            }

            DiameterAdmissionDecision admission =
                OverloadAdmission.EvaluateDiameterRouteAdmission(
                    decision.Request,
                    decision.SelectedCandidate,
                    true,
                    correlationText);
            if (admission.Admitted)
            {
                RequestClientDeliveryTarget target =
                    new RequestClientDeliveryTarget();
                target.Available = true;
                target.PeerIdentity = CloneIdentity(
                    decision.SelectedCandidate.PeerIdentity);
                target.Message = CloneMessage(decision.RoutedMessage);
                target.Status = DiameterSendStatus.Accepted;
                target.Code = "request-route-selected";
                return target;
            }

            if (admission.Action != DiameterAdmissionAction.Divert)
            {
                return CreateOverloadRejectedTarget(admission);
            }

            DiameterIdentity excluded = decision.SelectedCandidate.PeerIdentity;
            if (ContainsExcludedDiameterPeer(request, excluded))
            {
                return CreateUnavailableTarget(
                    DiameterSendStatus.Overloaded,
                    "overload-diversion-repeated",
                    "Overload diversion selected an already excluded peer.");
            }

            request.ExcludedPeers.Add(CloneIdentity(excluded));
            diverted = true;
            remainingSelections = remainingSelections - 1;
        }

        return CreateUnavailableTarget(
            DiameterSendStatus.Overloaded,
            "overload-diversion-exhausted",
            "Every usable route candidate was rejected by overload admission.");
    }

    internal DiameterRouteCandidate CreatePreferredPeerRouteCandidate(
        DiameterIdentity peerIdentity)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.PeerIdentity = CloneIdentity(peerIdentity);
        result.Available = true;
        result.ApplicationSupported = true;
        result.LoopFree = true;
        result.Secure = true;
        result.PolicyAccepted = true;
        result.Code = "preferred-peer-candidate";
        result.Message =
            "The caller-selected preferred peer is the current route candidate.";
        return result;
    }

    internal bool ContainsExcludedDiameterPeer(
        DiameterRouteRequest request,
        DiameterIdentity peerIdentity)
    {
        int index = 0;
        while (index < request.ExcludedPeers.Count)
        {
            if (ValueCodecs.AreDiameterIdentitiesEqual(
                request.ExcludedPeers[index],
                peerIdentity))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal RequestClientDeliveryTarget CreateOverloadRejectedTarget(
        DiameterAdmissionDecision decision)
    {
        string code = decision.Code.Length == 0 ?
            "overload-admission-rejected" : decision.Code;
        string message = decision.Message.Length == 0 ?
            "Overload admission rejected the outbound request." :
            decision.Message;
        return CreateUnavailableTarget(
            DiameterSendStatus.Overloaded,
            code,
            message);
    }

    internal RequestClientRouteContextResult BuildRouteRequest(
        DiameterMessage message)
    {
        RequestClientRouteContextResult result =
            new RequestClientRouteContextResult();
        if (message == null || message.Avps == null)
        {
            result.Code = "request-routing-storage-invalid";
            result.Message = "The request does not contain valid AVP storage.";
            return result;
        }

        result.Request.Message = CloneMessage(message);
        result.Request.ApplicationId = message.ApplicationId;
        result.Request.ReceivedAt = Clock.ReadUtcTime();
        int destinationRealmCount = 0;
        int destinationHostCount = 0;
        int originHostCount = 0;
        int sessionCount = 0;
        int userCount = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                result.Code = "request-routing-storage-invalid";
                result.Message =
                    "The request contains invalid AVP storage.";
                return result;
            }

            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationRealm)
            {
                destinationRealmCount = destinationRealmCount + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return CreateInvalidRouteContext(
                        "request-destination-realm-invalid");
                }

                result.Request.DestinationRealm = new DiameterIdentity();
                result.Request.DestinationRealm.Value = decoded.Text;
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationHost)
            {
                destinationHostCount = destinationHostCount + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return CreateInvalidRouteContext(
                        "request-destination-host-invalid");
                }

                result.Request.DestinationHost = new DiameterIdentity();
                result.Request.DestinationHost.Value = decoded.Text;
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.OriginHost)
            {
                originHostCount = originHostCount + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return CreateInvalidRouteContext(
                        "request-origin-host-invalid");
                }

                result.Request.OriginHost = new DiameterIdentity();
                result.Request.OriginHost.Value = decoded.Text;
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.SessionId)
            {
                sessionCount = sessionCount + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return CreateInvalidRouteContext(
                        "request-session-id-invalid");
                }

                result.Request.SessionId = decoded.Text;
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.UserName)
            {
                userCount = userCount + 1;
                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success)
                {
                    return CreateInvalidRouteContext(
                        "request-user-name-invalid");
                }

                result.Request.UserName = decoded.Text;
            }

            index = index + 1;
        }

        if (destinationRealmCount > 1 || destinationHostCount > 1 ||
            originHostCount > 1 || sessionCount > 1 || userCount > 1)
        {
            return CreateInvalidRouteContext(
                "request-routing-avp-duplicate");
        }

        result.Valid = true;
        result.Code = "request-route-context-created";
        return result;
    }

    internal RequestClientSendAttempt SendEncodedRequest(
        DiameterIdentity peerIdentity,
        byte[] data,
        int timeoutMilliseconds)
    {
        RequestClientSendAttempt result =
            new RequestClientSendAttempt();
        long started = Clock.ReadMonotonicMilliseconds();
        int transferred = 0;
        while (transferred < data.Length)
        {
            int remainingTime = ReadRemainingDuration(
                started,
                timeoutMilliseconds);
            if (remainingTime <= 0)
            {
                result.Status = DiameterTransportStatus.TimedOut;
                result.Code = "request-send-timeout";
                result.Message =
                    "The request send operation exceeded its timeout.";
                result.TransferredCount = transferred;
                return result;
            }

            int remainingBytes = data.Length - transferred;
            byte[] chunk = new byte[remainingBytes];
            Array.Copy(data, transferred, chunk, 0, remainingBytes);
            DiameterTransportIoResult progress =
                PeerManager.SendDiameterPeerBytes(
                    peerIdentity,
                    chunk,
                    remainingTime);
            if (progress == null || progress.TransferredCount < 0 ||
                progress.TransferredCount > remainingBytes)
            {
                result.Status = DiameterTransportStatus.Failed;
                result.Code = "request-send-count-invalid";
                result.Message =
                    "The peer transport returned an invalid send count.";
                result.TransferredCount = transferred;
                return result;
            }

            transferred = transferred + progress.TransferredCount;
            if (progress.Status != DiameterTransportStatus.Success)
            {
                result.Status = progress.Status;
                result.Code = progress.Code;
                result.Message = progress.Message;
                result.TransferredCount = transferred;
                return result;
            }

            if (progress.TransferredCount == 0)
            {
                result.Status = DiameterTransportStatus.Failed;
                result.Code = "request-send-no-progress";
                result.Message =
                    "The peer transport completed without send progress.";
                result.TransferredCount = transferred;
                return result;
            }
        }

        result.Delivered = true;
        result.Status = DiameterTransportStatus.Success;
        result.Code = "request-delivered";
        result.Message = "The request was delivered to the peer transport.";
        result.TransferredCount = transferred;
        return result;
    }

    internal int ReadRemainingDuration(
        long started,
        int timeoutMilliseconds)
    {
        long elapsed = Clock.ReadMonotonicMilliseconds() - started;
        if (elapsed < 0L)
        {
            elapsed = 0L;
        }

        long remaining = timeoutMilliseconds - elapsed;
        if (remaining <= 0L)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining;
    }

    internal int ReadRemainingTimeout(
        DiameterPendingRequestSnapshot pending)
    {
        TimeSpan remaining = pending.DeadlineAt - Clock.ReadUtcTime();
        if (remaining.TotalMilliseconds <= 0.0)
        {
            return 0;
        }

        if (remaining.TotalMilliseconds > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining.TotalMilliseconds;
    }

    internal bool SupportsPeerApplication(
        DiameterPeerSnapshot peer,
        uint applicationId)
    {
        int index = 0;
        while (index < peer.Capabilities.CommonApplications.Count)
        {
            uint supported =
                peer.Capabilities.CommonApplications[index].Id;
            if (supported == applicationId ||
                supported == DiameterApplicationIds.Relay)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal DiameterSendStatus MapTransportSendStatus(
        DiameterTransportStatus status)
    {
        if (status == DiameterTransportStatus.QueueFull)
        {
            return DiameterSendStatus.QueueFull;
        }

        if (status == DiameterTransportStatus.Closed ||
            status == DiameterTransportStatus.EndOfStream)
        {
            return DiameterSendStatus.Closed;
        }

        return DiameterSendStatus.Failed;
    }

    internal string ReadSendFailureCode(RequestClientSendAttempt attempt)
    {
        if (attempt.Code.Length > 0)
        {
            return attempt.Code;
        }

        return "request-send-failed";
    }

    internal string ReadSendFailureMessage(RequestClientSendAttempt attempt)
    {
        if (attempt.Message.Length > 0)
        {
            return attempt.Message;
        }

        return "The request could not be delivered to the selected peer.";
    }

    internal DiameterSendResult CreateAcceptedSendResult(
        DiameterPendingRequestSnapshot pending,
        DiameterIdentity peerIdentity)
    {
        DiameterSendResult result = new DiameterSendResult();
        result.Status = DiameterSendStatus.Accepted;
        result.PendingRequest =
            PendingRequests.ClonePendingRequestSnapshot(pending);
        result.PeerIdentity = CloneIdentity(peerIdentity);
        result.Accepted = true;
        result.Delivered = true;
        result.Code = "request-accepted";
        result.Message =
            "The request was delivered and entered pending tracking.";
        return result;
    }

    internal DiameterSendResult CreateSendFailure(
        DiameterSendStatus status,
        string code,
        string message)
    {
        DiameterSendResult result = new DiameterSendResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal RequestClientDeliveryTarget CreateUnavailableTarget(
        DiameterSendStatus status,
        string code,
        string message)
    {
        RequestClientDeliveryTarget result =
            new RequestClientDeliveryTarget();
        result.Status = status;
        result.Code = code;
        result.MessageText = message;
        return result;
    }

    internal RequestClientRouteContextResult CreateInvalidRouteContext(
        string code)
    {
        RequestClientRouteContextResult result =
            new RequestClientRouteContextResult();
        result.Code = code;
        result.Message =
            "The request contains invalid routing AVP information.";
        return result;
    }

    internal void DispatchPendingOutcome(PendingRequestTableResult outcome)
    {
        if (outcome.AnswerResult != null)
        {
            DispatchAnswerHandler(
                outcome.Handler,
                outcome.AnswerResult);
        }
    }

    internal void DispatchAnswerHandler(
        IDiameterAnswerHandler? handler,
        DiameterAnswerResult result)
    {
        if (handler != null)
        {
            handler.HandleDiameterAnswer(CloneAnswerResult(result));
        }
    }

    internal DiameterAnswerResult CloneAnswerResult(
        DiameterAnswerResult source)
    {
        DiameterAnswerResult result = new DiameterAnswerResult();
        result.RequestIdentifier = source.RequestIdentifier;
        if (source.PendingRequest != null)
        {
            result.PendingRequest =
                PendingRequests.ClonePendingRequestSnapshot(
                    source.PendingRequest);
        }

        if (source.Answer != null)
        {
            result.Answer = CloneMessage(source.Answer);
        }

        if (source.PeerIdentity != null)
        {
            result.PeerIdentity = CloneIdentity(source.PeerIdentity);
        }

        result.AnswerReceived = source.AnswerReceived;
        result.Correlated = source.Correlated;
        result.Successful = source.Successful;
        result.ErrorAnswer = source.ErrorAnswer;
        result.TimedOut = source.TimedOut;
        result.Cancelled = source.Cancelled;
        result.PeerFailed = source.PeerFailed;
        result.Late = source.Late;
        result.ResultCode = source.ResultCode;
        if (source.ExperimentalResult != null)
        {
            result.ExperimentalResult = new DiameterExperimentalResult();
            result.ExperimentalResult.VendorId =
                source.ExperimentalResult.VendorId;
            result.ExperimentalResult.ResultCode =
                source.ExperimentalResult.ResultCode;
        }

        result.ResultClass = source.ResultClass;
        result.ProtocolError = source.ProtocolError;
        result.ApplicationError = source.ApplicationError;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal DiameterMessage CloneMessage(DiameterMessage source)
    {
        return PendingRequests.ClonePendingMessage(source);
    }

    internal DiameterIdentity CloneIdentity(DiameterIdentity source)
    {
        return PendingRequests.ClonePendingIdentity(source);
    }
}
