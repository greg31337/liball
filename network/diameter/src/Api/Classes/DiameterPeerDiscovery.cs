using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>Discovers configured peers first and admits expiring DNS-derived peers.</summary>
public class DiameterPeerDiscovery : IDiameterPeerDiscovery
{
    internal List<DiameterPeerConfiguration> StaticPeers =
        new List<DiameterPeerConfiguration>();
    internal Dictionary<string, DiameterPeerConfiguration> DynamicPeers =
        new Dictionary<string, DiameterPeerConfiguration>();
    internal List<string> DynamicPeerOrder = new List<string>();
    internal object DynamicPeerLock = new object();
    internal IDiameterClock Clock;
    internal IPeerAdmissionPolicy? AdmissionPolicy;
    internal DiameterDnsDiscovery DnsDiscovery;

    /// <summary>Initializes peer discovery with configured peers and DNS dependencies.</summary>
    /// <param name="staticPeers">The statically configured peers.</param>
    /// <param name="dnsResolver">The resolver used for dynamic discovery.</param>
    /// <param name="clock">The clock used for dynamic expiry.</param>
    /// <param name="admissionPolicy">The optional dynamic-peer admission policy.</param>
    public DiameterPeerDiscovery(
        List<DiameterPeerConfiguration> staticPeers,
        IDnsResolver dnsResolver,
        IDiameterClock clock,
        IPeerAdmissionPolicy? admissionPolicy)
    {
        if (staticPeers == null || dnsResolver == null || clock == null)
        {
            throw new ArgumentException(
                "Peer discovery requires static peers, a DNS resolver, and a clock.");
        }

        Clock = clock;
        AdmissionPolicy = admissionPolicy;
        int index = 0;
        while (index < staticPeers.Count)
        {
            DiameterPeerConfiguration peer = staticPeers[index];
            if (peer == null || peer.Source != DiameterPeerSource.Static)
            {
                throw new ArgumentException(
                    "Configured discovery peers must be non-null static peers.");
            }

            peer.ValidatePeerConfiguration();
            if (ContainsStaticPeerIdentity(peer.Identity.Value))
            {
                throw new ArgumentException(
                    "Configured discovery peer identities must be unique.");
            }

            StaticPeers.Add(ClonePeerConfiguration(peer));
            index = index + 1;
        }

        DnsDiscovery = new DiameterDnsDiscovery(
            dnsResolver,
            clock,
            new SystemDiscoveryRandom());
    }

    /// <summary>Discovers configured peers before admitted DNS-derived peers.</summary>
    /// <param name="request">The peer discovery requirements.</param>
    /// <param name="timeoutMilliseconds">The positive total discovery timeout.</param>
    /// <returns>The configured or dynamically admitted discovery outcome.</returns>
    public DiameterDiscoveryResult DiscoverDiameterPeers(
        DiameterDiscoveryRequest? request,
        int timeoutMilliseconds)
    {
        if (request == null)
        {
            throw new ArgumentException("A peer discovery request is required.");
        }

        request.ValidateDiscoveryRequest();
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("timeoutMilliseconds");
        }

        RemoveExpiredDynamicPeers();
        if (request.AllowStaticPeers)
        {
            DiameterDiscoveryResult staticResult = DiscoverStaticPeers(request);
            if (staticResult.Resolved)
            {
                return staticResult;
            }
        }

        if (request.AllowDnsDiscovery && request.AllowCachedResults)
        {
            DiameterDiscoveryResult dynamicResult = DiscoverCachedDynamicPeers(request);
            if (dynamicResult.Resolved)
            {
                return dynamicResult;
            }
        }

        DiameterDiscoveryResult dnsResult = DnsDiscovery.ResolveDnsDiscovery(
            request,
            timeoutMilliseconds);
        if (!dnsResult.Resolved)
        {
            return dnsResult;
        }

        return AdmitDynamicDiscoveryPeers(dnsResult, request);
    }

    /// <summary>Invalidates cached DNS and dynamic peers for a realm.</summary>
    /// <param name="realm">The realm whose discovery state is invalidated.</param>
    public void InvalidateDiameterDiscoveryCache(DiameterIdentity realm)
    {
        if (realm == null || ValueCodecs.NormalizeDiameterRealm(realm).Status !=
            DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid discovery realm is required.");
        }

        DnsDiscovery.InvalidateDiscoveryCache(realm.Value);
        lock (DynamicPeerLock)
        {
            List<string> removals = new List<string>();
            foreach (string key in DynamicPeers.Keys)
            {
                DiameterPeerConfiguration peer = DynamicPeers[key];
                if (string.Equals(
                    peer.Realm.Value,
                    realm.Value,
                    StringComparison.OrdinalIgnoreCase))
                {
                    removals.Add(key);
                }
            }

            int index = 0;
            while (index < removals.Count)
            {
                DynamicPeers.Remove(removals[index]);
                DynamicPeerOrder.Remove(removals[index]);
                index = index + 1;
            }
        }
    }

    internal DiameterDiscoveryResult DiscoverStaticPeers(
        DiameterDiscoveryRequest request)
    {
        DiameterDiscoveryResult result = CreatePeerDiscoveryResult(request);
        int index = 0;
        while (index < StaticPeers.Count &&
            result.Endpoints.Count < request.MaximumResults)
        {
            DiameterPeerConfiguration peer = StaticPeers[index];
            AddMatchingPeerToDiscovery(result, peer, request, false);
            index = index + 1;
        }

        if (result.Endpoints.Count > 0)
        {
            result.Resolved = true;
            result.Code = "static-peer-discovery-success";
            result.Message = "Configured Diameter peers satisfy the discovery request.";
        }

        return result;
    }

    internal DiameterDiscoveryResult DiscoverCachedDynamicPeers(
        DiameterDiscoveryRequest request)
    {
        DiameterDiscoveryResult result = CreatePeerDiscoveryResult(request);
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (DynamicPeerLock)
        {
            int index = 0;
            while (index < DynamicPeerOrder.Count)
            {
                DiameterPeerConfiguration peer = DynamicPeers[DynamicPeerOrder[index]];
                if (peer.ExpiresAt > now &&
                    result.Endpoints.Count < request.MaximumResults)
                {
                    AddMatchingPeerToDiscovery(result, peer, request, true);
                }

                index = index + 1;
            }
        }

        if (result.Endpoints.Count > 0)
        {
            result.Resolved = true;
            result.FromCache = true;
            result.Code = "dynamic-peer-cache-success";
            result.Message = "Admitted dynamic Diameter peers satisfy the request.";
        }

        return result;
    }

    internal DiameterDiscoveryResult AdmitDynamicDiscoveryPeers(
        DiameterDiscoveryResult dnsResult,
        DiameterDiscoveryRequest request)
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        if (dnsResult.ExpiresAt == default(DateTimeOffset) ||
            dnsResult.ExpiresAt <= now)
        {
            dnsResult.Endpoints.Clear();
            dnsResult.Resolved = false;
            dnsResult.Negative = true;
            dnsResult.Code = "dynamic-peer-expired";
            dnsResult.Message = "The discovered peer records have already expired.";
            return dnsResult;
        }

        List<DiameterPeerConfiguration> candidates =
            CreateDynamicPeerCandidates(dnsResult, request);
        dnsResult.Endpoints.Clear();
        int admittedCount = 0;
        int index = 0;
        while (index < candidates.Count)
        {
            DiameterPeerConfiguration candidate = candidates[index];
            if (EvaluateDynamicPeerAdmission(candidate))
            {
                candidate.ValidatePeerConfiguration();
                StoreDynamicPeer(candidate);
                DiameterPeerConfiguration owned = ClonePeerConfiguration(candidate);
                dnsResult.Peers.Add(owned);
                AddPeerEndpointsToResult(dnsResult, owned, request.MaximumResults);
                admittedCount = admittedCount + 1;
            }

            index = index + 1;
        }

        if (admittedCount == 0 || dnsResult.Endpoints.Count == 0)
        {
            dnsResult.Resolved = false;
            dnsResult.Negative = true;
            dnsResult.Code = "dynamic-peer-admission-rejected";
            dnsResult.Message = "No dynamically discovered peer was admitted.";
        }
        else
        {
            dnsResult.Resolved = true;
            dnsResult.Negative = false;
            dnsResult.Code = "dynamic-peer-discovery-success";
            dnsResult.Message = "Dynamically discovered Diameter peers were admitted.";
        }

        return dnsResult;
    }

    internal List<DiameterPeerConfiguration> CreateDynamicPeerCandidates(
        DiameterDiscoveryResult dnsResult,
        DiameterDiscoveryRequest request)
    {
        List<DiameterPeerConfiguration> result =
            new List<DiameterPeerConfiguration>();
        int endpointIndex = 0;
        while (endpointIndex < dnsResult.Endpoints.Count)
        {
            DiameterEndpoint endpoint = dnsResult.Endpoints[endpointIndex];
            string identity = endpoint.Identity.Value;
            DiameterPeerConfiguration? peer = FindPeerCandidate(result, identity);
            if (peer == null)
            {
                peer = new DiameterPeerConfiguration();
                peer.Identity.Value = identity;
                peer.Realm.Value = request.Realm.Value;
                peer.Source = DiameterPeerSource.Dynamic;
                peer.ExpiresAt = dnsResult.ExpiresAt;
                peer.RequiredApplications.Add(CloneApplicationId(request.Application));
                result.Add(peer);
            }

            peer.Endpoints.Add(CloneEndpoint(endpoint));
            endpointIndex = endpointIndex + 1;
        }

        return result;
    }

    internal DiameterPeerConfiguration? FindPeerCandidate(
        List<DiameterPeerConfiguration> peers,
        string identity)
    {
        int index = 0;
        while (index < peers.Count)
        {
            if (string.Equals(
                peers[index].Identity.Value,
                identity,
                StringComparison.OrdinalIgnoreCase))
            {
                return peers[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool EvaluateDynamicPeerAdmission(DiameterPeerConfiguration peer)
    {
        if (AdmissionPolicy == null)
        {
            return true;
        }

        DiameterPeerAdmissionRequest request = new DiameterPeerAdmissionRequest();
        request.PeerIdentity.Value = peer.Identity.Value;
        request.PeerRealm.Value = peer.Realm.Value;
        request.Connection.Direction = DiameterConnectionDirection.Outbound;
        request.Connection.RemoteEndpoint = CloneEndpoint(peer.Endpoints[0]);
        request.Connection.Security.Mode = peer.Endpoints[0].Security;
        DiameterPeerAdmissionResult decision =
            AdmissionPolicy.EvaluatePeerAdmission(request);
        if (decision == null)
        {
            throw new DiameterPeerException(
                "The dynamic peer admission policy returned no decision.");
        }

        return decision.Admitted;
    }

    internal void StoreDynamicPeer(DiameterPeerConfiguration peer)
    {
        string key = peer.Identity.Value.ToLowerInvariant();
        lock (DynamicPeerLock)
        {
            if (!DynamicPeers.ContainsKey(key))
            {
                DynamicPeerOrder.Add(key);
            }

            DynamicPeers[key] = ClonePeerConfiguration(peer);
        }
    }

    internal void RemoveExpiredDynamicPeers()
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (DynamicPeerLock)
        {
            List<string> removals = new List<string>();
            foreach (string key in DynamicPeers.Keys)
            {
                if (DynamicPeers[key].ExpiresAt <= now)
                {
                    removals.Add(key);
                }
            }

            int index = 0;
            while (index < removals.Count)
            {
                DynamicPeers.Remove(removals[index]);
                DynamicPeerOrder.Remove(removals[index]);
                index = index + 1;
            }
        }
    }

    internal void AddMatchingPeerToDiscovery(
        DiameterDiscoveryResult result,
        DiameterPeerConfiguration peer,
        DiameterDiscoveryRequest request,
        bool dynamicPeer)
    {
        if (!peer.Enabled || !string.Equals(
            peer.Realm.Value,
            request.Realm.Value,
            StringComparison.OrdinalIgnoreCase) ||
            !PeerSupportsApplication(peer, request.Application))
        {
            return;
        }

        DiameterPeerConfiguration selected = ClonePeerConfiguration(peer);
        selected.Endpoints.Clear();
        int endpointIndex = 0;
        while (endpointIndex < peer.Endpoints.Count &&
            result.Endpoints.Count < request.MaximumResults)
        {
            DiameterEndpoint endpoint = peer.Endpoints[endpointIndex];
            if (endpoint.Transport == request.Transport &&
                endpoint.Security == request.Security)
            {
                DiameterEndpoint ownedEndpoint = CloneEndpoint(endpoint);
                selected.Endpoints.Add(ownedEndpoint);
                result.Endpoints.Add(CloneEndpoint(ownedEndpoint));
            }

            endpointIndex = endpointIndex + 1;
        }

        if (selected.Endpoints.Count > 0)
        {
            result.Peers.Add(selected);
            if (dynamicPeer &&
                (result.ExpiresAt == default(DateTimeOffset) ||
                selected.ExpiresAt < result.ExpiresAt))
            {
                result.ExpiresAt = selected.ExpiresAt;
            }
        }
    }

    internal bool PeerSupportsApplication(
        DiameterPeerConfiguration peer,
        DiameterApplicationId application)
    {
        if (peer.RequiredApplications.Count == 0)
        {
            return true;
        }

        int index = 0;
        while (index < peer.RequiredApplications.Count)
        {
            DiameterApplicationId candidate = peer.RequiredApplications[index];
            if (candidate.Id == application.Id &&
                candidate.VendorId == application.VendorId &&
                candidate.IsAccounting == application.IsAccounting)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddPeerEndpointsToResult(
        DiameterDiscoveryResult result,
        DiameterPeerConfiguration peer,
        int maximumResults)
    {
        int index = 0;
        while (index < peer.Endpoints.Count &&
            result.Endpoints.Count < maximumResults)
        {
            result.Endpoints.Add(CloneEndpoint(peer.Endpoints[index]));
            index = index + 1;
        }
    }

    internal DiameterDiscoveryResult CreatePeerDiscoveryResult(
        DiameterDiscoveryRequest request)
    {
        DiameterDiscoveryResult result = new DiameterDiscoveryResult();
        result.Request = DnsDiscovery.CloneDiscoveryRequest(request);
        return result;
    }

    internal bool ContainsStaticPeerIdentity(string identity)
    {
        int index = 0;
        while (index < StaticPeers.Count)
        {
            if (string.Equals(
                StaticPeers[index].Identity.Value,
                identity,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal DiameterPeerConfiguration ClonePeerConfiguration(
        DiameterPeerConfiguration source)
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = source.Identity.Value;
        result.Realm.Value = source.Realm.Value;
        result.Source = source.Source;
        result.Enabled = source.Enabled;
        result.ConnectAutomatically = source.ConnectAutomatically;
        result.AcceptInboundConnections = source.AcceptInboundConnections;
        result.ExpiresAt = source.ExpiresAt;
        int endpointIndex = 0;
        while (endpointIndex < source.Endpoints.Count)
        {
            result.Endpoints.Add(CloneEndpoint(source.Endpoints[endpointIndex]));
            endpointIndex = endpointIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < source.RequiredApplications.Count)
        {
            result.RequiredApplications.Add(
                CloneApplicationId(source.RequiredApplications[applicationIndex]));
            applicationIndex = applicationIndex + 1;
        }

        return result;
    }

    internal DiameterEndpoint CloneEndpoint(DiameterEndpoint source)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = source.Identity.Value;
        result.Host = source.Host;
        result.Address = source.Address;
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Security = source.Security;
        return result;
    }

    internal DiameterApplicationId CloneApplicationId(DiameterApplicationId source)
    {
        DiameterApplicationId result = new DiameterApplicationId();
        result.Id = source.Id;
        result.VendorId = source.VendorId;
        result.IsAccounting = source.IsAccounting;
        return result;
    }
}
