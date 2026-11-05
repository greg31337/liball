using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class RedirectAnswerGenerationResult
{
    internal bool Generated;
    internal DiameterMessage? Answer;
    internal string Code = string.Empty;
}

internal class RedirectProcessingResult
{
    internal bool Accepted;
    internal List<DiameterRedirectEntry> Entries =
        new List<DiameterRedirectEntry>();
    internal string Code = string.Empty;
}

internal class RedirectRouteSelectionResult
{
    internal DiameterRouteCandidate? SelectedCandidate;
    internal DiameterRedirectEntry? Entry;
    internal List<DiameterRouteCandidate> Candidates =
        new List<DiameterRouteCandidate>();
    internal string Code = string.Empty;
}

internal class RedirectAgent
{
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal DiameterIdentity LocalRealm = new DiameterIdentity();
    internal DiameterValidator Validator = new DiameterValidator();
    internal DiameterBaseAvpFactory AvpFactory =
        new DiameterBaseAvpFactory();

    internal RedirectAgent(
        DiameterIdentity localHost,
        DiameterIdentity localRealm)
    {
        DiameterValueResult host =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        DiameterValueResult realm =
            ValueCodecs.NormalizeDiameterRealm(localRealm);
        if (host.Status != DiameterCodecStatus.Success ||
            realm.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A redirect agent requires valid local identities.");
        }

        LocalHost.Value = host.Text;
        LocalRealm.Value = realm.Text;
    }

    internal RedirectAnswerGenerationResult GenerateRedirectAnswer(
        DiameterRouteDecision? decision)
    {
        if (decision == null || decision.Request == null ||
            decision.Request.Message == null ||
            decision.SelectedCandidate == null ||
            !decision.Routed ||
            decision.Action != DiameterRouteAction.Redirect ||
            decision.AgentRole != DiameterAgentRole.Redirect ||
            decision.SelectedCandidate.Action != DiameterRouteAction.Redirect ||
            decision.SelectedCandidate.AgentRole != DiameterAgentRole.Redirect)
        {
            return CreateRedirectAnswerFailure(
                "redirect-route-required");
        }

        DiameterRouteConfiguration configuration =
            decision.SelectedCandidate.Configuration;
        if (configuration.RedirectRealms.Count > 0)
        {
            return CreateRedirectAnswerFailure(
                "redirect-host-route-required");
        }

        configuration.ValidateRouteConfiguration();
        DiameterErrorAnswerRequest error =
            new DiameterErrorAnswerRequest();
        error.Request = RouteTableCloner.CloneRouteMessage(
            decision.Request.Message);
        error.ResultCode = DiameterResultCodes.RedirectIndication;
        DiameterMessage answer = Validator.BuildResultErrorAnswer(
            error,
            LocalHost,
            LocalRealm);
        int index = 0;
        while (index < configuration.RedirectHosts.Count)
        {
            answer.Avps.Add(AvpFactory.CreateRedirectHostAvp(
                configuration.RedirectHosts[index]));
            index = index + 1;
        }

        answer.Avps.Add(AvpFactory.CreateRedirectHostUsageAvp(
            configuration.RedirectUsage));
        if (configuration.RedirectUsage !=
            DiameterRedirectHostUsage.DoNotCache)
        {
            answer.Avps.Add(AvpFactory.CreateRedirectMaxCacheTimeAvp(
                configuration.RedirectMaximumCacheTimeSeconds));
        }

        RedirectAnswerGenerationResult result =
            new RedirectAnswerGenerationResult();
        result.Generated = true;
        result.Answer = answer;
        result.Code = "redirect-answer-generated";
        return result;
    }

    internal RedirectAnswerGenerationResult CreateRedirectAnswerFailure(
        string code)
    {
        RedirectAnswerGenerationResult result =
            new RedirectAnswerGenerationResult();
        result.Code = code;
        return result;
    }
}

internal class RedirectRouteCache
{
    internal object CacheLock = new object();
    internal Dictionary<string, DiameterRedirectEntry> Entries =
        new Dictionary<string, DiameterRedirectEntry>(
            StringComparer.Ordinal);
    internal Dictionary<string, long> EntryMonotonicExpirations =
        new Dictionary<string, long>(StringComparer.Ordinal);
    internal List<string> EntryOrder = new List<string>();
    internal List<DiameterRedirectEntry> ImmediateEntries =
        new List<DiameterRedirectEntry>();
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal IDiameterClock Clock;
    internal int MaximumLifetimeMilliseconds;
    internal int MaximumEntries;
    internal IRedirectHostPolicy? HostPolicy;

    internal RedirectRouteCache(
        DiameterIdentity localHost,
        IDiameterClock clock,
        int maximumLifetimeMilliseconds,
        int maximumEntries,
        IRedirectHostPolicy? hostPolicy)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        if (normalized.Status != DiameterCodecStatus.Success ||
            clock == null)
        {
            throw new ArgumentException(
                "A redirect cache requires a local host and clock.");
        }

        if (maximumLifetimeMilliseconds <= 0 || maximumEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "Redirect cache bounds");
        }

        LocalHost.Value = normalized.Text;
        Clock = clock;
        MaximumLifetimeMilliseconds = maximumLifetimeMilliseconds;
        MaximumEntries = maximumEntries;
        HostPolicy = hostPolicy;
    }

    internal RedirectProcessingResult ProcessRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        bool sourceSecure,
        DateTimeOffset receivedAt)
    {
        if (request == null || answer == null || sourcePeer == null)
        {
            return CreateRedirectProcessingFailure(
                "redirect-input-required");
        }

        request.ValidateRouteRequest();
        DiameterValueResult normalizedSource =
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer);
        if (normalizedSource.Status != DiameterCodecStatus.Success)
        {
            return CreateRedirectProcessingFailure(
                "redirect-source-invalid");
        }

        string headerFailure = ValidateRedirectAnswerHeader(
            request.Message,
            answer);
        if (headerFailure.Length > 0)
        {
            return CreateRedirectProcessingFailure(headerFailure);
        }

        uint resultCode = 0U;
        int resultCount = 0;
        int usageCount = 0;
        int maximumCount = 0;
        uint usageValue = 0U;
        uint maximumSeconds = 0U;
        List<DiameterUri> hosts = new List<DiameterUri>();
        List<string> hostTexts = new List<string>();
        int index = 0;
        while (index < answer.Avps.Count)
        {
            DiameterAvp avp = answer.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ResultCode)
            {
                resultCount = resultCount + 1;
                if (avp.Data.Length != 4)
                {
                    return CreateRedirectProcessingFailure(
                        "redirect-result-invalid");
                }

                resultCode = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectHostUsage)
            {
                usageCount = usageCount + 1;
                if (avp.Data.Length != 4)
                {
                    return CreateRedirectProcessingFailure(
                        "redirect-usage-invalid");
                }

                usageValue = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectMaxCacheTime)
            {
                maximumCount = maximumCount + 1;
                if (avp.Data.Length != 4)
                {
                    return CreateRedirectProcessingFailure(
                        "redirect-cache-time-invalid");
                }

                maximumSeconds = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectHost)
            {
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterUri(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success ||
                    decoded.Uri == null || decoded.Uri.Protocol != "diameter")
                {
                    return CreateRedirectProcessingFailure(
                        "redirect-host-invalid");
                }

                int duplicateIndex = 0;
                while (duplicateIndex < hostTexts.Count)
                {
                    if (hostTexts[duplicateIndex] == decoded.Text)
                    {
                        return CreateRedirectProcessingFailure(
                            "redirect-host-duplicate");
                    }

                    duplicateIndex = duplicateIndex + 1;
                }

                hosts.Add(RouteTableCloner.CloneRouteUri(decoded.Uri));
                hostTexts.Add(decoded.Text);
            }

            index = index + 1;
        }

        if (resultCount != 1 ||
            resultCode != DiameterResultCodes.RedirectIndication)
        {
            return CreateRedirectProcessingFailure(
                "redirect-result-invalid");
        }

        if (hosts.Count == 0 || usageCount > 1 || maximumCount > 1 ||
            usageValue > (uint)DiameterRedirectHostUsage.AllUser)
        {
            return CreateRedirectProcessingFailure(
                "redirect-avp-selection-invalid");
        }

        DiameterRedirectHostUsage usage =
            (DiameterRedirectHostUsage)usageValue;
        if (usage != DiameterRedirectHostUsage.DoNotCache &&
            (maximumCount != 1 || maximumSeconds == 0U))
        {
            return CreateRedirectProcessingFailure(
                "redirect-cache-time-required");
        }

        DiameterIdentity normalizedSourceIdentity = new DiameterIdentity();
        normalizedSourceIdentity.Value = normalizedSource.Text;
        index = 0;
        while (index < hosts.Count)
        {
            if (IsRedirectLoop(
                request,
                normalizedSource.Text,
                hosts[index].Host))
            {
                return CreateRedirectProcessingFailure(
                    "redirect-loop-detected");
            }

            DiameterRedirectHostAuthorizationResult authorization =
                AuthorizeRedirectHost(
                    request,
                    normalizedSourceIdentity,
                    hosts[index],
                    sourceSecure);
            if (!authorization.Authorized)
            {
                string authorizationCode = authorization.Code;
                if (authorizationCode.Length == 0)
                {
                    authorizationCode = "redirect-host-rejected";
                }

                return CreateRedirectProcessingFailure(
                    authorizationCode);
            }

            index = index + 1;
        }

        DiameterRedirectEntry? entry = CreateRedirectEntry(
            request,
            hosts,
            normalizedSource.Text,
            usage,
            maximumSeconds,
            sourceSecure,
            receivedAt);
        if (entry == null)
        {
            return CreateRedirectProcessingFailure(
                "redirect-scope-context-missing");
        }

        if (usage != DiameterRedirectHostUsage.DoNotCache &&
            entry.ExpiresAt > Clock.ReadUtcTime())
        {
            entry.Cached = true;
            entry.Code = "redirect-cached";
            StoreRedirectEntry(entry);
        }
        else if (usage == DiameterRedirectHostUsage.DoNotCache)
        {
            StoreImmediateRedirectEntry(entry);
        }

        RedirectProcessingResult result = new RedirectProcessingResult();
        result.Accepted = true;
        result.Entries.Add(RouteTableCloner.CloneRedirectEntry(entry));
        result.Code = entry.Cached ?
            "redirect-cached" : "redirect-accepted-not-cached";
        return result;
    }

    internal RedirectRouteSelectionResult SelectRedirectRoute(
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers)
    {
        RedirectRouteSelectionResult result =
            new RedirectRouteSelectionResult();
        if (!request.AllowRedirect)
        {
            result.Code = "redirect-routing-disabled";
            return result;
        }

        lock (CacheLock)
        {
            PurgeExpiredRedirectEntries();
            int immediateIndex = 0;
            while (immediateIndex < ImmediateEntries.Count)
            {
                DiameterRedirectEntry immediate =
                    ImmediateEntries[immediateIndex];
                if (immediate.RedirectKey == ReadImmediateRedirectKey(request))
                {
                    ImmediateEntries.RemoveAt(immediateIndex);
                    DiameterRouteCandidate? immediateCandidate =
                        BuildRedirectCandidates(
                            immediate,
                            request,
                            peers,
                            result.Candidates);
                    if (immediateCandidate != null)
                    {
                        result.SelectedCandidate = immediateCandidate;
                        result.Entry = RouteTableCloner.CloneRedirectEntry(
                            immediate);
                        result.Code = "redirect-route-selected";
                        return result;
                    }

                    result.Code = "redirect-host-unreachable";
                    return result;
                }

                immediateIndex = immediateIndex + 1;
            }

            int precedence = 1;
            while (precedence <= 6)
            {
                int index = 0;
                while (index < EntryOrder.Count)
                {
                    string key = EntryOrder[index];
                    DiameterRedirectEntry entry = Entries[key];
                    if (ReadRedirectPrecedence(entry.Usage) == precedence &&
                        MatchesRedirectEntry(entry, request))
                    {
                        DiameterRouteCandidate? selected =
                            BuildRedirectCandidates(
                                entry,
                                request,
                                peers,
                                result.Candidates);
                        if (selected != null)
                        {
                            result.SelectedCandidate = selected;
                            result.Entry =
                                RouteTableCloner.CloneRedirectEntry(entry);
                            result.Code = "redirect-route-selected";
                            return result;
                        }

                        Entries.Remove(key);
                        EntryMonotonicExpirations.Remove(key);
                        EntryOrder.RemoveAt(index);
                    }
                    else
                    {
                        index = index + 1;
                    }
                }

                precedence = precedence + 1;
            }
        }

        result.Code = result.Candidates.Count == 0 ?
            "redirect-route-not-found" : "redirect-host-unreachable";
        return result;
    }

    internal List<DiameterRedirectEntry> ReadRedirectEntries()
    {
        List<DiameterRedirectEntry> result =
            new List<DiameterRedirectEntry>();
        lock (CacheLock)
        {
            PurgeExpiredRedirectEntries();
            int index = 0;
            while (index < EntryOrder.Count)
            {
                result.Add(RouteTableCloner.CloneRedirectEntry(
                    Entries[EntryOrder[index]]));
                index = index + 1;
            }
        }

        return result;
    }

    internal string ValidateRedirectAnswerHeader(
        DiameterMessage request,
        DiameterMessage answer)
    {
        if ((answer.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None ||
            (answer.Flags & DiameterMessageFlags.Error) ==
                DiameterMessageFlags.None ||
            (answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None ||
            answer.Version != request.Version ||
            answer.CommandCode != request.CommandCode ||
            answer.ApplicationId != request.ApplicationId ||
            answer.HopByHopIdentifier != request.HopByHopIdentifier ||
            answer.EndToEndIdentifier != request.EndToEndIdentifier ||
            (answer.Flags & DiameterMessageFlags.Proxyable) !=
                (request.Flags & DiameterMessageFlags.Proxyable))
        {
            return "redirect-answer-correlation-invalid";
        }

        return string.Empty;
    }

    internal DiameterRedirectHostAuthorizationResult AuthorizeRedirectHost(
        DiameterRouteRequest request,
        DiameterIdentity sourcePeer,
        DiameterUri host,
        bool sourceSecure)
    {
        DiameterRedirectHostAuthorizationResult accepted =
            new DiameterRedirectHostAuthorizationResult();
        accepted.Authorized = true;
        accepted.Code = "redirect-host-authorized";
        accepted.Message = "The redirect host passed built-in validation.";
        if (HostPolicy == null)
        {
            return accepted;
        }

        DiameterRedirectHostAuthorizationRequest policyRequest =
            new DiameterRedirectHostAuthorizationRequest();
        policyRequest.Request = RouteTableCloner.CloneRouteRequest(request);
        policyRequest.RedirectingPeer.Value = sourcePeer.Value;
        policyRequest.RedirectHost = RouteTableCloner.CloneRouteUri(host);
        policyRequest.SourceSecure = sourceSecure;
        DiameterRedirectHostAuthorizationResult decision =
            HostPolicy.AuthorizeDiameterRedirectHost(policyRequest);
        if (decision == null || decision.Code == null ||
            decision.Message == null)
        {
            throw new DiameterRoutingException(
                "The redirect host policy returned an invalid decision.");
        }

        DiameterRedirectHostAuthorizationResult result =
            new DiameterRedirectHostAuthorizationResult();
        result.Authorized = decision.Authorized;
        result.Code = decision.Code;
        result.Message = decision.Message;
        return result;
    }

    internal bool IsRedirectLoop(
        DiameterRouteRequest request,
        string normalizedSourcePeer,
        DiameterIdentity target)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(target);
        if (normalized.Status != DiameterCodecStatus.Success ||
            normalized.Text == LocalHost.Value ||
            normalized.Text == normalizedSourcePeer)
        {
            return true;
        }

        if (request.SourcePeer != null &&
            ValueCodecs.AreDiameterIdentitiesEqual(
                request.SourcePeer,
                target))
        {
            return true;
        }

        int index = 0;
        while (index < request.ExcludedPeers.Count)
        {
            if (ValueCodecs.AreDiameterIdentitiesEqual(
                request.ExcludedPeers[index],
                target))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal DiameterRedirectEntry? CreateRedirectEntry(
        DiameterRouteRequest request,
        List<DiameterUri> hosts,
        string normalizedSourcePeer,
        DiameterRedirectHostUsage usage,
        uint maximumSeconds,
        bool sourceSecure,
        DateTimeOffset receivedAt)
    {
        DiameterRedirectEntry result = new DiameterRedirectEntry();
        result.Usage = usage;
        int index = 0;
        while (index < hosts.Count)
        {
            result.Hosts.Add(RouteTableCloner.CloneRouteUri(hosts[index]));
            index = index + 1;
        }

        result.DestinationRealm = RouteTableCloner.CloneRouteIdentity(
            request.DestinationRealm);
        result.DestinationHost = RouteTableCloner.CloneRouteIdentity(
            request.DestinationHost);
        result.ApplicationId = request.ApplicationId;
        result.SessionId = request.SessionId;
        result.UserName = request.UserName;
        result.RedirectingPeer.Value = normalizedSourcePeer;
        result.MaximumCacheTimeSeconds = maximumSeconds;
        result.CreatedAt = receivedAt;
        result.Secure = sourceSecure;
        result.Authorized = true;
        result.Code = "redirect-accepted";
        string scope = ReadRedirectScopeKey(result);
        if (scope.Length == 0)
        {
            return null;
        }

        result.RedirectKey = ((int)usage).ToString(
            CultureInfo.InvariantCulture) + "|" + scope;
        if (usage == DiameterRedirectHostUsage.DoNotCache)
        {
            result.RedirectKey = ReadImmediateRedirectKey(request);
        }
        long requestedMilliseconds = (long)maximumSeconds * 1000L;
        long lifetimeMilliseconds = requestedMilliseconds;
        if (lifetimeMilliseconds > MaximumLifetimeMilliseconds)
        {
            lifetimeMilliseconds = MaximumLifetimeMilliseconds;
        }

        result.ExpiresAt = receivedAt.AddMilliseconds(
            lifetimeMilliseconds);
        return result;
    }

    internal string ReadRedirectScopeKey(DiameterRedirectEntry entry)
    {
        if (entry.Usage == DiameterRedirectHostUsage.DoNotCache)
        {
            return "once";
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllSession)
        {
            return entry.SessionId.Length == 0 ?
                string.Empty : "session|" + entry.SessionId;
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllUser)
        {
            return entry.UserName.Length == 0 ?
                string.Empty : "user|" + entry.UserName;
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllApplication)
        {
            return "application|" + entry.ApplicationId.ToString(
                CultureInfo.InvariantCulture);
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllHost)
        {
            if (entry.DestinationHost == null)
            {
                return string.Empty;
            }

            return "host|" + ValueCodecs.NormalizeDiameterIdentity(
                entry.DestinationHost).Text;
        }

        if (entry.DestinationRealm == null)
        {
            return string.Empty;
        }

        string realm = ValueCodecs.NormalizeDiameterRealm(
            entry.DestinationRealm).Text;
        if (entry.Usage == DiameterRedirectHostUsage.AllRealm)
        {
            return "realm|" + realm;
        }

        return "realm-application|" + realm + "|" +
            entry.ApplicationId.ToString(CultureInfo.InvariantCulture);
    }

    internal void StoreRedirectEntry(DiameterRedirectEntry entry)
    {
        lock (CacheLock)
        {
            PurgeExpiredRedirectEntries();
            if (!Entries.ContainsKey(entry.RedirectKey))
            {
                while (EntryOrder.Count >= MaximumEntries)
                {
                    string oldest = EntryOrder[0];
                    EntryOrder.RemoveAt(0);
                    Entries.Remove(oldest);
                    EntryMonotonicExpirations.Remove(oldest);
                }

                EntryOrder.Add(entry.RedirectKey);
            }

            Entries[entry.RedirectKey] =
                RouteTableCloner.CloneRedirectEntry(entry);
            EntryMonotonicExpirations[entry.RedirectKey] =
                CreateRedirectMonotonicExpiration(entry);
        }
    }

    internal void StoreImmediateRedirectEntry(DiameterRedirectEntry entry)
    {
        lock (CacheLock)
        {
            while (ImmediateEntries.Count >= MaximumEntries)
            {
                ImmediateEntries.RemoveAt(0);
            }

            ImmediateEntries.Add(
                RouteTableCloner.CloneRedirectEntry(entry));
        }
    }

    internal string ReadImmediateRedirectKey(DiameterRouteRequest request)
    {
        return "once|" + request.Message.HopByHopIdentifier.ToString(
            CultureInfo.InvariantCulture) + "|" +
            request.Message.EndToEndIdentifier.ToString(
                CultureInfo.InvariantCulture);
    }

    internal void PurgeExpiredRedirectEntries()
    {
        DateTimeOffset now = Clock.ReadUtcTime();
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        int index = 0;
        while (index < EntryOrder.Count)
        {
            string key = EntryOrder[index];
            if (!Entries.ContainsKey(key) ||
                !EntryMonotonicExpirations.ContainsKey(key) ||
                Entries[key].ExpiresAt <= now ||
                EntryMonotonicExpirations[key] <= monotonicNow)
            {
                Entries.Remove(key);
                EntryMonotonicExpirations.Remove(key);
                EntryOrder.RemoveAt(index);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal long CreateRedirectMonotonicExpiration(
        DiameterRedirectEntry entry)
    {
        long monotonicNow = Clock.ReadMonotonicMilliseconds();
        double remainingValue =
            (entry.ExpiresAt - Clock.ReadUtcTime()).TotalMilliseconds;
        if (remainingValue <= 0.0)
        {
            return monotonicNow;
        }

        long remainingMilliseconds = (long)remainingValue;
        if (remainingMilliseconds > long.MaxValue - monotonicNow)
        {
            return long.MaxValue;
        }

        return monotonicNow + remainingMilliseconds;
    }

    internal bool MatchesRedirectEntry(
        DiameterRedirectEntry entry,
        DiameterRouteRequest request)
    {
        if (entry.Usage == DiameterRedirectHostUsage.AllSession)
        {
            return entry.SessionId == request.SessionId;
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllUser)
        {
            return entry.UserName == request.UserName;
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllApplication)
        {
            return entry.ApplicationId == request.ApplicationId;
        }

        if (entry.Usage == DiameterRedirectHostUsage.AllHost)
        {
            return entry.DestinationHost != null &&
                request.DestinationHost != null &&
                ValueCodecs.AreDiameterIdentitiesEqual(
                    entry.DestinationHost,
                    request.DestinationHost);
        }

        bool realm = entry.DestinationRealm != null &&
            request.DestinationRealm != null &&
            ValueCodecs.AreDiameterIdentitiesEqual(
                entry.DestinationRealm,
                request.DestinationRealm);
        if (entry.Usage == DiameterRedirectHostUsage.AllRealm)
        {
            return realm;
        }

        return realm && entry.ApplicationId == request.ApplicationId;
    }

    internal int ReadRedirectPrecedence(DiameterRedirectHostUsage usage)
    {
        if (usage == DiameterRedirectHostUsage.AllSession)
        {
            return 1;
        }

        if (usage == DiameterRedirectHostUsage.AllUser)
        {
            return 2;
        }

        if (usage == DiameterRedirectHostUsage.RealmAndApplication)
        {
            return 3;
        }

        if (usage == DiameterRedirectHostUsage.AllRealm)
        {
            return 4;
        }

        if (usage == DiameterRedirectHostUsage.AllApplication)
        {
            return 5;
        }

        return 6;
    }

    internal DiameterRouteCandidate? BuildRedirectCandidates(
        DiameterRedirectEntry entry,
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers,
        List<DiameterRouteCandidate> candidates)
    {
        DiameterRouteCandidate? selected = null;
        int hostIndex = 0;
        while (hostIndex < entry.Hosts.Count)
        {
            DiameterUri host = entry.Hosts[hostIndex];
            DiameterRouteCandidate candidate =
                new DiameterRouteCandidate();
            candidate.Configuration.RouteIdentifier =
                "redirect-cache-" + entry.RedirectKey;
            candidate.Configuration.ApplicationId = request.ApplicationId;
            candidate.Configuration.PeerIdentity =
                RouteTableCloner.CloneRouteIdentity(host.Host);
            candidate.Configuration.Action = DiameterRouteAction.Relay;
            candidate.Configuration.AgentRole = DiameterAgentRole.Relay;
            candidate.Configuration.RequireSecurePeer = host.Scheme == "aaas";
            candidate.PeerIdentity =
                RouteTableCloner.CloneRouteIdentity(host.Host);
            candidate.Action = DiameterRouteAction.Relay;
            candidate.AgentRole = DiameterAgentRole.Relay;
            candidate.Source = DiameterRouteSource.Redirect;
            candidate.Weight = 1;
            candidate.Order = hostIndex;
            candidate.LoopFree = !IsRedirectLoop(
                request,
                entry.RedirectingPeer.Value,
                host.Host);
            DiameterPeerSnapshot? peer = FindRedirectPeer(
                host.Host,
                peers);
            if (peer != null)
            {
                candidate.Peer =
                    PeerTableCloner.ClonePeerTableSnapshot(peer);
            }

            candidate.Available = peer != null && peer.Available;
            candidate.ApplicationSupported = peer != null &&
                SupportsRedirectApplication(peer, request.ApplicationId);
            candidate.Secure = host.Scheme != "aaas" ||
                (peer != null && IsRedirectPeerSecure(peer));
            candidate.PolicyAccepted = entry.Authorized;
            candidate.Code = "redirect-route-eligible";
            candidate.Message = "The cached redirect target is eligible.";
            if (!candidate.LoopFree)
            {
                candidate.Code = "redirect-route-loop";
                candidate.Message = "The cached redirect target creates a loop.";
            }
            else if (!candidate.Available)
            {
                candidate.Code = "redirect-route-unavailable";
                candidate.Message = "The cached redirect target is unavailable.";
            }
            else if (!candidate.ApplicationSupported)
            {
                candidate.Code = "redirect-application-unsupported";
                candidate.Message = "The redirect target does not support the application.";
            }
            else if (!candidate.Secure)
            {
                candidate.Code = "redirect-security-required";
                candidate.Message = "The redirect target requires a secure connection.";
            }

            candidates.Add(candidate);
            if (selected == null && candidate.LoopFree &&
                candidate.Available && candidate.ApplicationSupported &&
                candidate.Secure && candidate.PolicyAccepted)
            {
                selected = candidate;
            }

            hostIndex = hostIndex + 1;
        }

        return selected;
    }

    internal DiameterPeerSnapshot? FindRedirectPeer(
        DiameterIdentity identity,
        List<DiameterPeerSnapshot> peers)
    {
        int index = 0;
        while (index < peers.Count)
        {
            if (ValueCodecs.AreDiameterIdentitiesEqual(
                identity,
                peers[index].Configuration.Identity))
            {
                return peers[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool SupportsRedirectApplication(
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

    internal bool IsRedirectPeerSecure(DiameterPeerSnapshot peer)
    {
        if (peer.Connection != null &&
            peer.Connection.Security.Mode != DiameterSecurityMode.None)
        {
            return true;
        }

        return peer.CurrentEndpoint != null &&
            peer.CurrentEndpoint.Security != DiameterSecurityMode.None;
    }

    internal RedirectProcessingResult CreateRedirectProcessingFailure(
        string code)
    {
        RedirectProcessingResult result = new RedirectProcessingResult();
        result.Code = code;
        return result;
    }
}
