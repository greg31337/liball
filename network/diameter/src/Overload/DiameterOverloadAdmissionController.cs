using System;

namespace diameter;

internal class DiameterOverloadAdmissionController
{
    internal IDiameterOverloadManager? Manager;

    internal DiameterOverloadAdmissionController()
    {
    }

    internal void ConfigureDiameterOverloadAdmission(
        IDiameterOverloadManager? manager)
    {
        Manager = manager;
    }

    internal DiameterAdmissionDecision EvaluateDiameterRouteAdmission(
        DiameterRouteRequest request,
        DiameterRouteCandidate? candidate,
        bool allowDiversion,
        string correlationText)
    {
        DiameterAdmissionRequest admission = new DiameterAdmissionRequest();
        admission.Request = RouteTableCloner.CloneRouteRequest(request);
        if (candidate != null)
        {
            admission.Candidate =
                RouteTableCloner.CloneRouteCandidate(candidate);
        }

        admission.Scope = DiameterOverloadScope.Application;
        admission.AllowDiversion = allowDiversion;
        admission.AllowThrottling = true;
        admission.EvaluatedAt = request.ReceivedAt;
        admission.CorrelationText = correlationText;
        if (Manager == null || IsDiameterWatchdog(request.Message))
        {
            return CreateDiameterAdmissionBypass(admission);
        }

        DiameterAdmissionDecision? decision =
            Manager.EvaluateDiameterAdmission(admission);
        if (decision == null)
        {
            throw new DiameterValidationException(
                "The overload manager returned no admission decision.");
        }

        decision.ValidateAdmissionDecision();
        return decision;
    }

    internal DiameterAdmissionDecision EvaluateDiameterApplicationAdmission(
        DiameterRequestContext context)
    {
        DiameterRouteRequest route = CreateDiameterApplicationRoute(context);
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.PeerIdentity = CloneDiameterIdentity(context.PeerIdentity);
        candidate.Available = true;
        candidate.ApplicationSupported = true;
        candidate.LoopFree = true;
        candidate.Secure = true;
        candidate.PolicyAccepted = true;
        return EvaluateDiameterRouteAdmission(
            route,
            candidate,
            false,
            context.RouteIdentifier);
    }

    internal DiameterRouteRequest CreateDiameterApplicationRoute(
        DiameterRequestContext context)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message = RouteTableCloner.CloneRouteMessage(context.Request);
        result.SourcePeer = CloneDiameterIdentity(context.PeerIdentity);
        result.ApplicationId = context.Request.ApplicationId;
        result.SessionId = context.SessionId;
        result.ReceivedAt = context.ReceivedAt;
        int index = 0;
        while (index < context.Request.Avps.Count)
        {
            DiameterAvp avp = context.Request.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.OriginHost)
            {
                result.OriginHost = DecodeDiameterAdmissionIdentity(avp);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationHost)
            {
                result.DestinationHost = DecodeDiameterAdmissionIdentity(avp);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.DestinationRealm)
            {
                result.DestinationRealm = DecodeDiameterAdmissionIdentity(avp);
            }

            index = index + 1;
        }

        return result;
    }

    internal DiameterIdentity? DecodeDiameterAdmissionIdentity(
        DiameterAvp avp)
    {
        DiameterValueResult decoded =
            ValueCodecs.DecodeDiameterIdentity(avp.Data);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return null;
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = decoded.Text;
        return result;
    }

    internal bool IsDiameterWatchdog(DiameterMessage message)
    {
        return message.CommandCode == DiameterCommandCodes.DeviceWatchdog &&
            message.ApplicationId == DiameterApplicationIds.Common;
    }

    internal DiameterAdmissionDecision CreateDiameterAdmissionBypass(
        DiameterAdmissionRequest request)
    {
        DiameterAdmissionDecision result = new DiameterAdmissionDecision();
        result.Request = DiameterOverloadState.CloneDiameterAdmissionRequest(
            request);
        result.Code = IsDiameterWatchdog(request.Request.Message) ?
            "overload-watchdog-bypass" :
            "overload-admission-unconfigured";
        result.Message = IsDiameterWatchdog(request.Request.Message) ?
            "Diameter watchdog traffic bypasses overload admission." :
            "No overload manager is configured for this request path.";
        return result;
    }

    internal DiameterIdentity CloneDiameterIdentity(DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }
}
