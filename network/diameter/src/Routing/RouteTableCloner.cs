using System;

namespace diameter;

internal static class RouteTableCloner
{
    internal static DiameterMessage CloneRouteMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int avpIndex = 0;
        while (avpIndex < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[avpIndex];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = new byte[sourceAvp.Data.Length];
            Array.Copy(sourceAvp.Data, avp.Data, sourceAvp.Data.Length);
            result.Avps.Add(avp);
            avpIndex = avpIndex + 1;
        }

        return result;
    }

    internal static DiameterRouteRequest CloneRouteRequest(
        DiameterRouteRequest source)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message = CloneRouteMessage(source.Message);
        result.SourcePeer = CloneRouteIdentity(source.SourcePeer);
        result.OriginHost = CloneRouteIdentity(source.OriginHost);
        result.DestinationRealm = CloneRouteIdentity(source.DestinationRealm);
        result.DestinationHost = CloneRouteIdentity(source.DestinationHost);
        result.ApplicationId = source.ApplicationId;
        result.SessionId = source.SessionId;
        result.UserName = source.UserName;
        int peerIndex = 0;
        while (peerIndex < source.ExcludedPeers.Count)
        {
            DiameterIdentity peer = new DiameterIdentity();
            peer.Value = source.ExcludedPeers[peerIndex].Value;
            result.ExcludedPeers.Add(peer);
            peerIndex = peerIndex + 1;
        }

        result.RoutingPriority = source.RoutingPriority;
        result.RoutingPriorityPresent = source.RoutingPriorityPresent;
        result.AllowRedirect = source.AllowRedirect;
        result.AllowTranslation = source.AllowTranslation;
        result.InitialSessionRequest = source.InitialSessionRequest;
        result.ReceivedAt = source.ReceivedAt;
        return result;
    }

    internal static DiameterRouteConfiguration CloneRouteConfiguration(
        DiameterRouteConfiguration source)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = source.RouteIdentifier;
        result.DestinationRealm.Value = source.DestinationRealm.Value;
        if (source.DestinationHost != null)
        {
            result.DestinationHost = new DiameterIdentity();
            result.DestinationHost.Value = source.DestinationHost.Value;
        }

        result.ApplicationId = source.ApplicationId;
        result.AnyApplication = source.AnyApplication;
        if (source.PeerIdentity != null)
        {
            result.PeerIdentity = new DiameterIdentity();
            result.PeerIdentity.Value = source.PeerIdentity.Value;
        }

        result.Action = source.Action;
        result.AgentRole = source.AgentRole;
        result.Priority = source.Priority;
        result.Weight = source.Weight;
        result.TargetApplicationId = source.TargetApplicationId;
        result.RedirectUsage = source.RedirectUsage;
        result.RedirectMaximumCacheTimeSeconds =
            source.RedirectMaximumCacheTimeSeconds;
        int redirectIndex = 0;
        while (redirectIndex < source.RedirectHosts.Count)
        {
            DiameterUri sourceHost = source.RedirectHosts[redirectIndex];
            DiameterUri host = new DiameterUri();
            host.Scheme = sourceHost.Scheme;
            host.Host.Value = sourceHost.Host.Value;
            host.Port = sourceHost.Port;
            host.Transport = sourceHost.Transport;
            host.Protocol = sourceHost.Protocol;
            result.RedirectHosts.Add(host);
            redirectIndex = redirectIndex + 1;
        }

        redirectIndex = 0;
        while (redirectIndex < source.RedirectRealms.Count)
        {
            DiameterIdentity redirectRealm = new DiameterIdentity();
            redirectRealm.Value = source.RedirectRealms[redirectIndex].Value;
            result.RedirectRealms.Add(redirectRealm);
            redirectIndex = redirectIndex + 1;
        }

        result.RedirectInitialSessionRequestsOnly =
            source.RedirectInitialSessionRequestsOnly;
        result.RedirectRequestsWithDestinationHost =
            source.RedirectRequestsWithDestinationHost;

        result.DefaultRoute = source.DefaultRoute;
        result.RequireSecurePeer = source.RequireSecurePeer;
        result.Enabled = source.Enabled;
        return result;
    }

    internal static DiameterRouteSnapshot CloneRouteSnapshot(
        DiameterRouteSnapshot source)
    {
        DiameterRouteSnapshot result = new DiameterRouteSnapshot();
        result.Configuration = CloneRouteConfiguration(source.Configuration);
        if (source.PeerIdentity != null)
        {
            result.PeerIdentity = new DiameterIdentity();
            result.PeerIdentity.Value = source.PeerIdentity.Value;
        }

        result.Source = source.Source;
        result.Available = source.Available;
        result.ApplicationSupported = source.ApplicationSupported;
        result.Healthy = source.Healthy;
        result.SelectionCount = source.SelectionCount;
        result.FailureCount = source.FailureCount;
        result.CreatedAt = source.CreatedAt;
        result.UpdatedAt = source.UpdatedAt;
        result.LastSelectedAt = source.LastSelectedAt;
        result.Code = source.Code;
        return result;
    }

    internal static DiameterIdentity? CloneRouteIdentity(
        DiameterIdentity? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal static DiameterUri CloneRouteUri(DiameterUri source)
    {
        DiameterUri result = new DiameterUri();
        result.Scheme = source.Scheme;
        result.Host.Value = source.Host.Value;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Protocol = source.Protocol;
        return result;
    }

    internal static DiameterRedirectEntry CloneRedirectEntry(
        DiameterRedirectEntry source)
    {
        DiameterRedirectEntry result = new DiameterRedirectEntry();
        result.RedirectKey = source.RedirectKey;
        result.Usage = source.Usage;
        int index = 0;
        while (index < source.Hosts.Count)
        {
            result.Hosts.Add(CloneRouteUri(source.Hosts[index]));
            index = index + 1;
        }

        result.DestinationRealm = CloneRouteIdentity(
            source.DestinationRealm);
        result.DestinationHost = CloneRouteIdentity(
            source.DestinationHost);
        result.ApplicationId = source.ApplicationId;
        result.SessionId = source.SessionId;
        result.UserName = source.UserName;
        result.RedirectingPeer.Value = source.RedirectingPeer.Value;
        result.MaximumCacheTimeSeconds = source.MaximumCacheTimeSeconds;
        result.CreatedAt = source.CreatedAt;
        result.ExpiresAt = source.ExpiresAt;
        result.Secure = source.Secure;
        result.Authorized = source.Authorized;
        result.Cached = source.Cached;
        result.Code = source.Code;
        result.RealmBased = source.RealmBased;
        result.RedirectRealm = CloneRouteIdentity(source.RedirectRealm);
        return result;
    }

    internal static DiameterRouteCandidate CloneRouteCandidate(
        DiameterRouteCandidate source)
    {
        DiameterRouteCandidate result = new DiameterRouteCandidate();
        result.Configuration = CloneRouteConfiguration(source.Configuration);
        result.PeerIdentity = CloneRouteIdentity(source.PeerIdentity);
        if (source.Peer != null)
        {
            result.Peer = PeerTableCloner.ClonePeerTableSnapshot(source.Peer);
        }

        result.Action = source.Action;
        result.AgentRole = source.AgentRole;
        result.Source = source.Source;
        result.Priority = source.Priority;
        result.Weight = source.Weight;
        result.PolicyScore = source.PolicyScore;
        result.Order = source.Order;
        result.Available = source.Available;
        result.ApplicationSupported = source.ApplicationSupported;
        result.LoopFree = source.LoopFree;
        result.Secure = source.Secure;
        result.PolicyAccepted = source.PolicyAccepted;
        result.PolicyTerminal = source.PolicyTerminal;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal static DiameterTranslationResult CloneTranslationResult(
        DiameterTranslationResult source)
    {
        DiameterTranslationResult result = new DiameterTranslationResult();
        result.Disposition = source.Disposition;
        if (source.SourceMessage != null)
        {
            result.SourceMessage = CloneRouteMessage(source.SourceMessage);
        }

        if (source.TranslatedMessage != null)
        {
            result.TranslatedMessage = CloneRouteMessage(
                source.TranslatedMessage);
        }

        result.SourceApplicationId = source.SourceApplicationId;
        result.TargetApplicationId = source.TargetApplicationId;
        result.TranslationState = new byte[source.TranslationState.Length];
        Array.Copy(
            source.TranslationState,
            result.TranslationState,
            source.TranslationState.Length);
        result.ResultCode = source.ResultCode;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }
}
