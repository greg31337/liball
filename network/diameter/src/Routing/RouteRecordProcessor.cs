using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

internal class RouteRecordInspectionResult
{
    internal bool Accepted = true;
    internal bool LoopDetected;
    internal List<DiameterIdentity> RouteRecords =
        new List<DiameterIdentity>();
    internal string Code = "route-records-accepted";
    internal string Message = string.Empty;
}

internal class RouteRecordProcessor
{
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal bool AgentLoopDetectionEnabled;
    internal IRoutePathPolicy? PathPolicy;

    internal RouteRecordProcessor(
        DiameterIdentity localHost,
        DiameterNodeRole roles,
        IRoutePathPolicy? pathPolicy)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "Route-Record processing requires a valid local host.");
        }

        LocalHost.Value = normalized.Text;
        AgentLoopDetectionEnabled =
            (roles & DiameterNodeRole.Relay) != DiameterNodeRole.None ||
            (roles & DiameterNodeRole.Proxy) != DiameterNodeRole.None;
        PathPolicy = pathPolicy;
    }

    internal RouteRecordInspectionResult InspectRouteRecords(
        DiameterRouteRequest request)
    {
        RouteRecordInspectionResult result =
            new RouteRecordInspectionResult();
        int index = 0;
        while (index < request.Message.Avps.Count)
        {
            DiameterAvp avp = request.Message.Avps[index];
            if (avp.Code == DiameterAvpCodes.RouteRecord && avp.VendorId == 0U)
            {
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success ||
                    decoded.Identity == null)
                {
                    result.Accepted = false;
                    result.Code = "route-record-invalid";
                    result.Message =
                        "A Route-Record AVP contains an invalid peer identity.";
                    return result;
                }

                DiameterIdentity identity = new DiameterIdentity();
                identity.Value = decoded.Identity.Value;
                result.RouteRecords.Add(identity);
                if (AgentLoopDetectionEnabled &&
                    ValueCodecs.AreDiameterIdentitiesEqual(identity, LocalHost))
                {
                    result.Accepted = false;
                    result.LoopDetected = true;
                    result.Code = "route-loop-detected";
                    result.Message =
                        "The local host is already present in Route-Record.";
                    return result;
                }

                AddPredictiveRouteExclusion(request, identity);
            }

            index = index + 1;
        }

        return result;
    }

    internal void AddPredictiveRouteExclusion(
        DiameterRouteRequest request,
        DiameterIdentity identity)
    {
        int index = 0;
        while (index < request.ExcludedPeers.Count)
        {
            if (ValueCodecs.AreDiameterIdentitiesEqual(
                request.ExcludedPeers[index],
                identity))
            {
                return;
            }

            index = index + 1;
        }

        DiameterIdentity excluded = new DiameterIdentity();
        excluded.Value = identity.Value;
        request.ExcludedPeers.Add(excluded);
    }

    internal DiameterRoutePathAuthorizationResult AuthorizeRoutePath(
        DiameterRouteRequest request,
        List<DiameterIdentity> routeRecords)
    {
        DiameterRoutePathAuthorizationResult accepted =
            new DiameterRoutePathAuthorizationResult();
        accepted.Authorized = true;
        accepted.Code = "route-path-authorized";
        accepted.Message = "No route path policy rejected local delivery.";
        if (PathPolicy == null)
        {
            return accepted;
        }

        DiameterRoutePathAuthorizationRequest policyRequest =
            new DiameterRoutePathAuthorizationRequest();
        policyRequest.Request = RouteTableCloner.CloneRouteRequest(request);
        policyRequest.LocalHost.Value = LocalHost.Value;
        int index = 0;
        while (index < routeRecords.Count)
        {
            DiameterIdentity identity = new DiameterIdentity();
            identity.Value = routeRecords[index].Value;
            policyRequest.TraversedPeers.Add(identity);
            index = index + 1;
        }

        DiameterRoutePathAuthorizationResult result =
            PathPolicy.AuthorizeDiameterRoutePath(policyRequest);
        if (result == null || result.Code == null || result.Message == null)
        {
            throw new DiameterRoutingException(
                "The route path policy returned an invalid decision.");
        }

        DiameterRoutePathAuthorizationResult owned =
            new DiameterRoutePathAuthorizationResult();
        owned.Authorized = result.Authorized;
        owned.Code = result.Code;
        owned.Message = result.Message;
        return owned;
    }

    internal void AppendForwardingRouteRecord(
        DiameterMessage message,
        DiameterIdentity? sourcePeer)
    {
        if (sourcePeer == null)
        {
            return;
        }

        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new DiameterRoutingException(
                "A forwarded request requires a valid source peer identity.");
        }

        DiameterAvp routeRecord = new DiameterAvp();
        routeRecord.Code = DiameterAvpCodes.RouteRecord;
        routeRecord.Flags = DiameterAvpFlags.Mandatory;
        routeRecord.Data = Encoding.UTF8.GetBytes(normalized.Text);
        message.Avps.Add(routeRecord);
    }
}
