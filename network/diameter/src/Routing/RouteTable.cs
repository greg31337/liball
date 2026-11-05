using System;
using System.Collections.Generic;

namespace diameter;

internal interface IRouteRandom
{
    long ReadRouteRandomValue(long maximumExclusive);
}

internal class SystemRouteRandom : IRouteRandom
{
    internal SystemRouteRandom()
    {
    }

    public long ReadRouteRandomValue(long maximumExclusive)
    {
        return Random.Shared.NextInt64(maximumExclusive);
    }
}

internal class RouteTableEntry
{
    internal DiameterRouteSnapshot Snapshot = new DiameterRouteSnapshot();
    internal int Order;
}

internal class RouteSelectionResult
{
    internal DiameterRouteCandidate? SelectedCandidate;
    internal List<DiameterRouteCandidate> Candidates =
        new List<DiameterRouteCandidate>();
    internal string Code = string.Empty;
}

internal class RouteTable
{
    internal Dictionary<string, RouteTableEntry> Entries =
        new Dictionary<string, RouteTableEntry>(StringComparer.OrdinalIgnoreCase);
    internal List<string> EntryOrder = new List<string>();
    internal object TableLock = new object();
    internal IDiameterClock Clock;
    internal IRouteRandom RandomSource;
    internal RoutePolicyEvaluator PolicyEvaluator =
        new RoutePolicyEvaluator(new List<IRoutePolicy>());
    internal RouteSessionAffinityStore Affinities =
        new RouteSessionAffinityStore(4096);
    internal DiameterLoadState? LoadState;
    internal int NextOrder;

    internal RouteTable(IDiameterClock clock, IRouteRandom randomSource)
    {
        if (clock == null || randomSource == null)
        {
            throw new ArgumentException(
                "A route table requires a clock and random source.");
        }

        Clock = clock;
        RandomSource = randomSource;
    }

    internal void ConfigureRouteSelection(
        List<IRoutePolicy> policies,
        int affinityCapacity)
    {
        RoutePolicyEvaluator evaluator =
            new RoutePolicyEvaluator(policies);
        RouteSessionAffinityStore affinities =
            new RouteSessionAffinityStore(affinityCapacity);
        lock (TableLock)
        {
            PolicyEvaluator = evaluator;
            Affinities = affinities;
        }
    }

    internal void ConfigureDiameterLoadRouting(
        DiameterLoadState? loadState)
    {
        lock (TableLock)
        {
            LoadState = loadState;
        }
    }

    internal DiameterRouteSnapshot AddOrReplaceStaticRoute(
        DiameterRouteConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException("A route configuration is required.");
        }

        configuration.ValidateRouteConfiguration();
        DiameterRouteConfiguration owned =
            RouteTableCloner.CloneRouteConfiguration(configuration);
        string key = ReadRouteKey(configuration.RouteIdentifier);
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (TableLock)
        {
            if (Entries.ContainsKey(key))
            {
                RouteTableEntry existing = Entries[key];
                existing.Snapshot.Configuration = owned;
                existing.Snapshot.PeerIdentity = CloneRoutePeerIdentity(
                    owned.PeerIdentity);
                existing.Snapshot.Source = ReadRouteSource(owned);
                existing.Snapshot.Available = ReadNonPeerRouteAvailability(owned);
                existing.Snapshot.ApplicationSupported =
                    !IsPeerRouteConfiguration(owned);
                existing.Snapshot.Healthy = ReadNonPeerRouteAvailability(owned);
                existing.Snapshot.UpdatedAt = now;
                existing.Snapshot.Code = owned.Enabled ?
                    "route-awaiting-peer-state" : "route-disabled";
                if (!IsPeerRouteConfiguration(owned) && owned.Enabled)
                {
                    existing.Snapshot.Code = "route-available";
                }

                return RouteTableCloner.CloneRouteSnapshot(existing.Snapshot);
            }

            RouteTableEntry entry = new RouteTableEntry();
            entry.Order = NextOrder;
            NextOrder = NextOrder + 1;
            entry.Snapshot.Configuration = owned;
            entry.Snapshot.PeerIdentity = CloneRoutePeerIdentity(owned.PeerIdentity);
            entry.Snapshot.Source = ReadRouteSource(owned);
            entry.Snapshot.Available = ReadNonPeerRouteAvailability(owned);
            entry.Snapshot.ApplicationSupported =
                !IsPeerRouteConfiguration(owned);
            entry.Snapshot.Healthy = ReadNonPeerRouteAvailability(owned);
            entry.Snapshot.CreatedAt = now;
            entry.Snapshot.UpdatedAt = now;
            entry.Snapshot.Code = owned.Enabled ?
                "route-awaiting-peer-state" : "route-disabled";
            if (!IsPeerRouteConfiguration(owned) && owned.Enabled)
            {
                entry.Snapshot.Code = "route-available";
            }

            Entries[key] = entry;
            EntryOrder.Add(key);
            return RouteTableCloner.CloneRouteSnapshot(entry.Snapshot);
        }
    }

    internal bool RemoveStaticRoute(string routeIdentifier)
    {
        string key = ReadRouteKey(routeIdentifier);
        lock (TableLock)
        {
            if (!Entries.ContainsKey(key))
            {
                return false;
            }

            Entries.Remove(key);
            EntryOrder.Remove(key);
            Affinities.RemoveRouteAffinities(routeIdentifier);
            return true;
        }
    }

    internal DiameterRouteSnapshot? ReadStaticRouteSnapshot(
        string routeIdentifier)
    {
        string key = ReadRouteKey(routeIdentifier);
        lock (TableLock)
        {
            if (!Entries.ContainsKey(key))
            {
                return null;
            }

            return RouteTableCloner.CloneRouteSnapshot(Entries[key].Snapshot);
        }
    }

    internal List<DiameterRouteSnapshot> ReadAllStaticRouteSnapshots()
    {
        List<DiameterRouteSnapshot> result = new List<DiameterRouteSnapshot>();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                result.Add(RouteTableCloner.CloneRouteSnapshot(
                    Entries[EntryOrder[index]].Snapshot));
                index = index + 1;
            }
        }

        return result;
    }

    internal List<DiameterRouteSnapshot> RefreshStaticRouteSnapshots(
        List<DiameterPeerSnapshot> peers)
    {
        ValidateRoutePeers(peers);
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                RouteTableEntry entry = Entries[EntryOrder[index]];
                RefreshRouteEntry(entry, peers, now);
                index = index + 1;
            }

            return ReadAllStaticRouteSnapshots();
        }
    }

    internal RouteSelectionResult SelectStaticRoute(
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers)
    {
        if (request == null)
        {
            throw new ArgumentException("A route request is required.");
        }

        request.ValidateRouteRequest();
        ValidateRoutePeers(peers);
        RouteSelectionResult result = new RouteSelectionResult();
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                RouteTableEntry entry = Entries[EntryOrder[index]];
                if (entry.Snapshot.Configuration.RedirectRealms.Count == 0 &&
                    MatchesStaticRoute(entry.Snapshot.Configuration, request))
                {
                    DiameterRouteCandidate candidate = BuildRouteCandidate(
                        entry,
                        request,
                        peers);
                    result.Candidates.Add(candidate);
                    UpdateRouteEntryFromCandidate(entry, candidate, now);
                }

                index = index + 1;
            }

            DiameterRouteCandidate? selected = SelectEligibleRouteCandidate(
                result.Candidates,
                request);
            if (selected == null)
            {
                result.Code = result.Candidates.Count == 0 ?
                    "route-not-configured" : "route-unavailable";
                return result;
            }

            result.SelectedCandidate = selected;
            result.Code = "route-selected";
            RouteTableEntry selectedEntry = Entries[
                ReadRouteKey(selected.Configuration.RouteIdentifier)];
            selectedEntry.Snapshot.SelectionCount =
                selectedEntry.Snapshot.SelectionCount + 1L;
            selectedEntry.Snapshot.LastSelectedAt = now;
            selectedEntry.Snapshot.UpdatedAt = now;
            return result;
        }
    }

    internal RouteSelectionResult SelectRealmRedirectRoute(
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers)
    {
        request.ValidateRouteRequest();
        ValidateRoutePeers(peers);
        RouteSelectionResult result = new RouteSelectionResult();
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (TableLock)
        {
            int index = 0;
            while (index < EntryOrder.Count)
            {
                RouteTableEntry entry = Entries[EntryOrder[index]];
                DiameterRouteConfiguration configuration =
                    entry.Snapshot.Configuration;
                if (configuration.Action == DiameterRouteAction.Redirect &&
                    configuration.RedirectRealms.Count > 0 &&
                    MatchesStaticRoute(configuration, request))
                {
                    DiameterRouteCandidate candidate = BuildRouteCandidate(
                        entry,
                        request,
                        peers);
                    result.Candidates.Add(candidate);
                    UpdateRouteEntryFromCandidate(entry, candidate, now);
                }

                index = index + 1;
            }

            DiameterRouteCandidate? selected = SelectEligibleRouteCandidate(
                result.Candidates,
                request);
            if (selected == null)
            {
                result.Code = result.Candidates.Count == 0 ?
                    "realm-redirect-not-configured" :
                    "realm-redirect-unavailable";
                return result;
            }

            result.SelectedCandidate = selected;
            result.Code = "realm-redirect-selected";
            RouteTableEntry selectedEntry = Entries[
                ReadRouteKey(selected.Configuration.RouteIdentifier)];
            selectedEntry.Snapshot.SelectionCount =
                selectedEntry.Snapshot.SelectionCount + 1L;
            selectedEntry.Snapshot.LastSelectedAt = now;
            selectedEntry.Snapshot.UpdatedAt = now;
            return result;
        }
    }

    internal DiameterRouteSnapshot RecordStaticRouteFailure(
        string routeIdentifier)
    {
        string key = ReadRouteKey(routeIdentifier);
        DateTimeOffset now = Clock.ReadUtcTime();
        lock (TableLock)
        {
            if (!Entries.ContainsKey(key))
            {
                throw new DiameterRoutingException(
                    "The failed route is not present in the static route table.");
            }

            DiameterRouteSnapshot snapshot = Entries[key].Snapshot;
            snapshot.FailureCount = snapshot.FailureCount + 1L;
            snapshot.UpdatedAt = now;
            snapshot.Code = "route-delivery-failed";
            return RouteTableCloner.CloneRouteSnapshot(snapshot);
        }
    }

    internal void RefreshRouteEntry(
        RouteTableEntry entry,
        List<DiameterPeerSnapshot> peers,
        DateTimeOffset now)
    {
        DiameterRouteConfiguration configuration = entry.Snapshot.Configuration;
        bool peerRoute = IsPeerRouteConfiguration(configuration);
        DiameterPeerSnapshot? peer = FindRoutePeer(
            configuration.PeerIdentity,
            peers);
        bool healthy = !peerRoute || (peer != null && peer.Available);
        bool applicationSupported = !peerRoute ||
            (peer != null && SupportsConfiguredRouteApplication(
                peer,
                configuration));
        bool secure = !configuration.RequireSecurePeer ||
            (peer != null && IsRoutePeerSecure(peer));
        entry.Snapshot.PeerIdentity = CloneRoutePeerIdentity(
            configuration.PeerIdentity);
        entry.Snapshot.Healthy = configuration.Enabled && healthy;
        entry.Snapshot.ApplicationSupported = applicationSupported;
        entry.Snapshot.Available = configuration.Enabled && healthy &&
            applicationSupported && secure;
        entry.Snapshot.UpdatedAt = now;
        entry.Snapshot.Code = ReadRouteSnapshotCode(
            configuration,
            peer,
            healthy,
            applicationSupported,
            secure);
    }

    internal DiameterRouteCandidate BuildRouteCandidate(
        RouteTableEntry entry,
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers)
    {
        DiameterRouteConfiguration configuration = entry.Snapshot.Configuration;
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.Configuration =
            RouteTableCloner.CloneRouteConfiguration(configuration);
        candidate.PeerIdentity = CloneRoutePeerIdentity(configuration.PeerIdentity);
        candidate.Action = configuration.Action;
        candidate.AgentRole = configuration.AgentRole;
        candidate.Source = ReadRouteSource(configuration);
        candidate.Priority = configuration.Priority;
        candidate.Weight = configuration.Weight;
        candidate.Order = entry.Order;
        candidate.LoopFree = true;
        candidate.Secure = true;
        candidate.PolicyAccepted = true;
        bool peerRoute = IsPeerRouteConfiguration(configuration);
        DiameterPeerSnapshot? peer = FindRoutePeer(
            configuration.PeerIdentity,
            peers);
        if (peer != null)
        {
            candidate.Peer = PeerTableCloner.ClonePeerTableSnapshot(peer);
        }

        candidate.Available = configuration.Enabled &&
            (!peerRoute || (peer != null && peer.Available));
        candidate.ApplicationSupported = !peerRoute ||
            (peer != null && SupportsRouteApplication(peer, request.ApplicationId));
        candidate.LoopFree = !peerRoute || IsRoutePeerLoopFree(
            configuration.PeerIdentity,
            request);
        candidate.Secure = !configuration.RequireSecurePeer ||
            (peer != null && IsRoutePeerSecure(peer));
        candidate.Code = "route-eligible";
        candidate.Message = "The route is eligible for selection.";
        if (!configuration.Enabled)
        {
            candidate.Code = "route-disabled";
            candidate.Message = "The route is disabled.";
        }
        else if (peerRoute && peer == null)
        {
            candidate.Code = "route-peer-not-found";
            candidate.Message = "The configured route peer was not found.";
        }
        else if (!candidate.Available)
        {
            candidate.Code = "route-peer-unhealthy";
            candidate.Message = "The configured route peer is not available.";
        }
        else if (!candidate.ApplicationSupported)
        {
            candidate.Code = "route-application-unsupported";
            candidate.Message = "The route peer does not support the application.";
        }
        else if (!candidate.LoopFree)
        {
            candidate.Code = "route-peer-excluded";
            candidate.Message = "The route peer is excluded for this request.";
        }
        else if (!candidate.Secure)
        {
            candidate.Code = "route-security-required";
            candidate.Message = "The route requires a secured peer connection.";
        }
        else if (configuration.Action == DiameterRouteAction.Redirect &&
            !request.AllowRedirect)
        {
            candidate.PolicyAccepted = false;
            candidate.Code = "route-redirect-disabled";
            candidate.Message = "Redirect routing is disabled for this request.";
        }
        else if (configuration.RedirectRealms.Count > 0 &&
            configuration.RedirectInitialSessionRequestsOnly &&
            !request.InitialSessionRequest)
        {
            candidate.PolicyAccepted = false;
            candidate.Code = "realm-redirect-established-session";
            candidate.Message = "Realm redirection is limited to new sessions.";
        }
        else if (configuration.RedirectRealms.Count > 0 &&
            request.DestinationHost != null &&
            !configuration.RedirectRequestsWithDestinationHost)
        {
            candidate.PolicyAccepted = false;
            candidate.Code = "realm-redirect-host-addressed";
            candidate.Message =
                "Realm redirection is disabled for host-addressed requests.";
        }
        else if (configuration.Action == DiameterRouteAction.Translate &&
            !request.AllowTranslation)
        {
            candidate.PolicyAccepted = false;
            candidate.Code = "route-translation-disabled";
            candidate.Message = "Translation routing is disabled for this request.";
        }

        return PolicyEvaluator.EvaluateRouteCandidate(
            request,
            candidate);
    }

    internal DiameterRouteCandidate? SelectEligibleRouteCandidate(
        List<DiameterRouteCandidate> candidates,
        DiameterRouteRequest request)
    {
        int bestSpecificity = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            DiameterRouteCandidate candidate = candidates[index];
            if (IsEligibleRouteCandidate(candidate))
            {
                int specificity = ReadRouteSpecificity(
                    candidate.Configuration,
                    request);
                if (specificity > bestSpecificity)
                {
                    bestSpecificity = specificity;
                }
            }

            index = index + 1;
        }

        if (bestSpecificity < 0)
        {
            return null;
        }

        string affinityRoute =
            Affinities.ReadAffinityRoute(request.SessionId);
        if (affinityRoute.Length > 0)
        {
            index = 0;
            while (index < candidates.Count)
            {
                DiameterRouteCandidate candidate = candidates[index];
                if (IsEligibleRouteCandidate(candidate) &&
                    ReadRouteSpecificity(
                        candidate.Configuration,
                        request) == bestSpecificity &&
                    string.Equals(
                        candidate.Configuration.RouteIdentifier,
                        affinityRoute,
                        StringComparison.OrdinalIgnoreCase))
                {
                    candidate.Source = DiameterRouteSource.SessionAffinity;
                    return candidate;
                }

                index = index + 1;
            }

            Affinities.RemoveAffinityRoute(request.SessionId);
        }

        int bestPriority = int.MaxValue;
        int bestPolicyScore = int.MinValue;
        index = 0;
        while (index < candidates.Count)
        {
            DiameterRouteCandidate candidate = candidates[index];
            if (IsEligibleRouteCandidate(candidate) &&
                ReadRouteSpecificity(
                    candidate.Configuration,
                    request) == bestSpecificity &&
                (candidate.Priority < bestPriority ||
                (candidate.Priority == bestPriority &&
                candidate.PolicyScore > bestPolicyScore)))
            {
                bestPriority = candidate.Priority;
                bestPolicyScore = candidate.PolicyScore;
            }

            index = index + 1;
        }

        List<DiameterRouteCandidate> group =
            new List<DiameterRouteCandidate>();
        index = 0;
        while (index < candidates.Count)
        {
            DiameterRouteCandidate candidate = candidates[index];
            if (IsEligibleRouteCandidate(candidate) &&
                ReadRouteSpecificity(candidate.Configuration, request) ==
                    bestSpecificity &&
                candidate.Priority == bestPriority &&
                candidate.PolicyScore == bestPolicyScore)
            {
                group.Add(candidate);
            }

            index = index + 1;
        }

        List<long> selectionWeights = new List<long>();
        bool loadApplied = false;
        if (LoadState != null)
        {
            DiameterLoadScoringResult loadScores =
                LoadState.ScoreDiameterRouteCandidates(group, true);
            loadApplied = loadScores.LoadApplied;
            index = 0;
            while (index < loadScores.Scores.Count)
            {
                selectionWeights.Add(
                    loadScores.Scores[index].SelectionWeight);
                index = index + 1;
            }
        }
        else
        {
            index = 0;
            while (index < group.Count)
            {
                selectionWeights.Add(group[index].Weight);
                index = index + 1;
            }
        }

        long totalWeight = 0L;
        bool equalWeights = true;
        long firstWeight = -1L;
        index = 0;
        while (index < selectionWeights.Count)
        {
            long weight = selectionWeights[index];
            if (firstWeight < 0L)
            {
                firstWeight = weight;
            }
            else if (weight != firstWeight)
            {
                equalWeights = false;
            }

            if (weight < 0L || long.MaxValue - totalWeight < weight)
            {
                throw new DiameterRoutingException(
                    "The route weight total is too large.");
            }

            totalWeight = totalWeight + weight;
            index = index + 1;
        }

        DiameterRouteCandidate selectedCandidate;
        if (group.Count == 1 || !loadApplied && equalWeights)
        {
            selectedCandidate = group[0];
            index = 1;
            while (index < group.Count)
            {
                if (group[index].Order < selectedCandidate.Order)
                {
                    selectedCandidate = group[index];
                }

                index = index + 1;
            }

            Affinities.StoreAffinityRoute(
                request.SessionId,
                selectedCandidate.Configuration.RouteIdentifier);
            return selectedCandidate;
        }

        long selection = RandomSource.ReadRouteRandomValue(totalWeight);
        if (selection < 0L || selection >= totalWeight)
        {
            throw new DiameterRoutingException(
                "The route random source returned an invalid value.");
        }

        long runningWeight = 0L;
        index = 0;
        while (index < group.Count)
        {
            runningWeight = runningWeight + selectionWeights[index];
            if (selection < runningWeight)
            {
                selectedCandidate = group[index];
                Affinities.StoreAffinityRoute(
                    request.SessionId,
                    selectedCandidate.Configuration.RouteIdentifier);
                return selectedCandidate;
            }

            index = index + 1;
        }

        selectedCandidate = group[group.Count - 1];
        Affinities.StoreAffinityRoute(
            request.SessionId,
            selectedCandidate.Configuration.RouteIdentifier);
        return selectedCandidate;
    }

    internal void UpdateRouteEntryFromCandidate(
        RouteTableEntry entry,
        DiameterRouteCandidate candidate,
        DateTimeOffset now)
    {
        entry.Snapshot.PeerIdentity = CloneRoutePeerIdentity(
            candidate.PeerIdentity);
        entry.Snapshot.Available = candidate.Available &&
            candidate.ApplicationSupported && candidate.Secure;
        entry.Snapshot.ApplicationSupported = candidate.ApplicationSupported;
        entry.Snapshot.Healthy = candidate.Available;
        entry.Snapshot.UpdatedAt = now;
        entry.Snapshot.Code = candidate.Code;
        if (entry.Snapshot.Available)
        {
            entry.Snapshot.Code = "route-available";
        }
    }

    internal bool MatchesStaticRoute(
        DiameterRouteConfiguration configuration,
        DiameterRouteRequest request)
    {
        if (!configuration.AnyApplication &&
            configuration.ApplicationId != request.ApplicationId)
        {
            return false;
        }

        if (configuration.DefaultRoute)
        {
            return true;
        }

        if (configuration.DestinationHost != null)
        {
            if (request.DestinationHost == null ||
                ReadNormalizedRouteIdentity(configuration.DestinationHost) !=
                    ReadNormalizedRouteIdentity(request.DestinationHost))
            {
                return false;
            }

            if (configuration.DestinationRealm.Value.Length == 0)
            {
                return true;
            }
        }

        return request.DestinationRealm != null &&
            ReadNormalizedRouteRealm(configuration.DestinationRealm) ==
                ReadNormalizedRouteRealm(request.DestinationRealm);
    }

    internal int ReadRouteSpecificity(
        DiameterRouteConfiguration configuration,
        DiameterRouteRequest request)
    {
        if (configuration.DefaultRoute)
        {
            return 0;
        }

        if (configuration.DestinationHost != null &&
            request.DestinationHost != null)
        {
            return 2;
        }

        return 1;
    }

    internal bool SupportsConfiguredRouteApplication(
        DiameterPeerSnapshot peer,
        DiameterRouteConfiguration configuration)
    {
        if (configuration.AnyApplication)
        {
            return peer.Capabilities.CommonApplications.Count > 0;
        }

        return SupportsRouteApplication(peer, configuration.ApplicationId);
    }

    internal bool SupportsRouteApplication(
        DiameterPeerSnapshot peer,
        uint applicationId)
    {
        int index = 0;
        while (index < peer.Capabilities.CommonApplications.Count)
        {
            uint supported = peer.Capabilities.CommonApplications[index].Id;
            if (supported == applicationId ||
                supported == DiameterApplicationIds.Relay)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsRoutePeerLoopFree(
        DiameterIdentity? peerIdentity,
        DiameterRouteRequest request)
    {
        if (peerIdentity == null)
        {
            return false;
        }

        string identity = ReadNormalizedRouteIdentity(peerIdentity);
        if (request.SourcePeer != null &&
            identity == ReadNormalizedRouteIdentity(request.SourcePeer))
        {
            return false;
        }

        int index = 0;
        while (index < request.ExcludedPeers.Count)
        {
            if (identity == ReadNormalizedRouteIdentity(
                request.ExcludedPeers[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsRoutePeerSecure(DiameterPeerSnapshot peer)
    {
        if (peer.Connection != null && peer.Connection.Security != null &&
            peer.Connection.Security.Mode != DiameterSecurityMode.None)
        {
            return true;
        }

        return peer.CurrentEndpoint != null &&
            peer.CurrentEndpoint.Security != DiameterSecurityMode.None;
    }

    internal DiameterPeerSnapshot? FindRoutePeer(
        DiameterIdentity? identity,
        List<DiameterPeerSnapshot> peers)
    {
        if (identity == null)
        {
            return null;
        }

        string expected = ReadNormalizedRouteIdentity(identity);
        int index = 0;
        while (index < peers.Count)
        {
            DiameterPeerSnapshot peer = peers[index];
            if (ReadNormalizedRouteIdentity(peer.Configuration.Identity) == expected)
            {
                return peer;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsEligibleRouteCandidate(DiameterRouteCandidate candidate)
    {
        return candidate.Available && candidate.ApplicationSupported &&
            candidate.LoopFree && candidate.Secure && candidate.PolicyAccepted;
    }

    internal bool IsPeerRouteConfiguration(
        DiameterRouteConfiguration configuration)
    {
        return configuration.Action == DiameterRouteAction.Relay ||
            configuration.Action == DiameterRouteAction.Proxy;
    }

    internal bool ReadNonPeerRouteAvailability(
        DiameterRouteConfiguration configuration)
    {
        return configuration.Enabled &&
            !IsPeerRouteConfiguration(configuration);
    }

    internal DiameterRouteSource ReadRouteSource(
        DiameterRouteConfiguration configuration)
    {
        if (configuration.DefaultRoute)
        {
            return DiameterRouteSource.Default;
        }

        return DiameterRouteSource.Static;
    }

    internal string ReadRouteSnapshotCode(
        DiameterRouteConfiguration configuration,
        DiameterPeerSnapshot? peer,
        bool healthy,
        bool applicationSupported,
        bool secure)
    {
        if (!configuration.Enabled)
        {
            return "route-disabled";
        }

        if (IsPeerRouteConfiguration(configuration) && peer == null)
        {
            return "route-peer-not-found";
        }

        if (!healthy)
        {
            return "route-peer-unhealthy";
        }

        if (!applicationSupported)
        {
            return "route-application-unsupported";
        }

        if (!secure)
        {
            return "route-security-required";
        }

        return "route-available";
    }

    internal void ValidateRoutePeers(List<DiameterPeerSnapshot> peers)
    {
        if (peers == null)
        {
            throw new ArgumentException("Route peer snapshots are required.");
        }

        int index = 0;
        while (index < peers.Count)
        {
            DiameterPeerSnapshot peer = peers[index];
            if (peer == null || peer.Configuration == null ||
                peer.Configuration.Identity == null || peer.Capabilities == null ||
                peer.Capabilities.CommonApplications == null ||
                ValueCodecs.NormalizeDiameterIdentity(
                    peer.Configuration.Identity).Status !=
                    DiameterCodecStatus.Success)
            {
                throw new ArgumentException(
                    "Route peer snapshots must contain valid owned state.");
            }

            int applicationIndex = 0;
            while (applicationIndex <
                peer.Capabilities.CommonApplications.Count)
            {
                if (peer.Capabilities.CommonApplications[applicationIndex] == null)
                {
                    throw new ArgumentException(
                        "Route peer applications must not contain null values.");
                }

                applicationIndex = applicationIndex + 1;
            }

            int comparisonIndex = index + 1;
            while (comparisonIndex < peers.Count)
            {
                DiameterPeerSnapshot comparison = peers[comparisonIndex];
                if (comparison == null || comparison.Configuration == null ||
                    comparison.Configuration.Identity == null ||
                    ValueCodecs.NormalizeDiameterIdentity(
                        comparison.Configuration.Identity).Status !=
                        DiameterCodecStatus.Success)
                {
                    throw new ArgumentException(
                        "Route peer snapshots must contain valid owned state.");
                }

                if (ReadNormalizedRouteIdentity(peer.Configuration.Identity) ==
                    ReadNormalizedRouteIdentity(
                        comparison.Configuration.Identity))
                {
                    throw new ArgumentException(
                        "Route peer snapshots must have distinct identities.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }

    internal DiameterIdentity? CloneRoutePeerIdentity(DiameterIdentity? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal string ReadRouteKey(string routeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(routeIdentifier) ||
            ValueCodecs.FindInvalidUtf16Index(routeIdentifier) >= 0)
        {
            throw new ArgumentException("A valid route identifier is required.");
        }

        return routeIdentifier.Trim();
    }

    internal string ReadNormalizedRouteIdentity(DiameterIdentity identity)
    {
        return ValueCodecs.NormalizeDiameterIdentity(identity).Text;
    }

    internal string ReadNormalizedRouteRealm(DiameterIdentity identity)
    {
        return ValueCodecs.NormalizeDiameterRealm(identity).Text;
    }
}
