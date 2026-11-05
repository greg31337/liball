using System;
using System.Collections.Generic;

namespace diameter;

internal class RealmRedirectProcessingResult
{
    internal bool Accepted;
    internal bool Rerouted;
    internal bool ForwardAnswer;
    internal DiameterRouteRequest? Request;
    internal DiameterRouteCandidate? Candidate;
    internal DiameterRedirectEntry? Entry;
    internal List<DiameterIdentity> Realms = new List<DiameterIdentity>();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class RealmRedirectAgent
{
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal DiameterIdentity LocalRealm = new DiameterIdentity();
    internal RouteTable Table;
    internal RedirectRouteCache Cache;
    internal IRealmRedirectPolicy? Policy;
    internal DiameterValidator Validator = new DiameterValidator();
    internal DiameterBaseAvpFactory AvpFactory =
        new DiameterBaseAvpFactory();

    internal RealmRedirectAgent(
        DiameterIdentity localHost,
        DiameterIdentity localRealm,
        RouteTable table,
        RedirectRouteCache cache,
        IRealmRedirectPolicy? policy)
    {
        DiameterValueResult host =
            ValueCodecs.NormalizeDiameterIdentity(localHost);
        DiameterValueResult realm =
            ValueCodecs.NormalizeDiameterRealm(localRealm);
        if (host.Status != DiameterCodecStatus.Success ||
            realm.Status != DiameterCodecStatus.Success ||
            table == null || cache == null)
        {
            throw new ArgumentException(
                "Realm redirection requires local identities and route storage.");
        }

        LocalHost.Value = host.Text;
        LocalRealm.Value = realm.Text;
        Table = table;
        Cache = cache;
        Policy = policy;
    }

    internal RedirectAnswerGenerationResult GenerateRealmRedirectAnswer(
        DiameterRouteDecision? decision)
    {
        if (decision == null || decision.SelectedCandidate == null ||
            !decision.Routed ||
            decision.Action != DiameterRouteAction.Redirect ||
            decision.AgentRole != DiameterAgentRole.Redirect ||
            decision.SelectedCandidate.Action != DiameterRouteAction.Redirect ||
            decision.SelectedCandidate.AgentRole !=
                DiameterAgentRole.Redirect ||
            decision.SelectedCandidate.Configuration.RedirectRealms.Count == 0)
        {
            return CreateRealmRedirectGenerationFailure(
                "realm-redirect-route-required");
        }

        DiameterRouteConfiguration configuration =
            decision.SelectedCandidate.Configuration;
        configuration.ValidateRouteConfiguration();
        if (configuration.RedirectInitialSessionRequestsOnly &&
            !decision.Request.InitialSessionRequest)
        {
            return CreateRealmRedirectGenerationFailure(
                "realm-redirect-established-session");
        }

        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = RouteTableCloner.CloneRouteMessage(
            decision.Request.Message);
        error.ResultCode = DiameterResultCodes.RealmRedirectIndication;
        DiameterMessage answer = Validator.BuildResultErrorAnswer(
            error,
            LocalHost,
            LocalRealm);
        int index = 0;
        while (index < configuration.RedirectRealms.Count)
        {
            answer.Avps.Add(AvpFactory.CreateIdentityAvp(
                DiameterAvpCodes.RedirectRealm,
                configuration.RedirectRealms[index]));
            index = index + 1;
        }

        if (configuration.RedirectUsage !=
            DiameterRedirectHostUsage.DoNotCache)
        {
            answer.Avps.Add(AvpFactory.CreateRedirectHostUsageAvp(
                configuration.RedirectUsage));
            answer.Avps.Add(AvpFactory.CreateRedirectMaxCacheTimeAvp(
                configuration.RedirectMaximumCacheTimeSeconds));
        }

        RedirectAnswerGenerationResult result =
            new RedirectAnswerGenerationResult();
        result.Generated = true;
        result.Answer = answer;
        result.Code = "realm-redirect-answer-generated";
        return result;
    }

    internal RealmRedirectProcessingResult ProcessRealmRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        bool sourceSecure,
        DateTimeOffset receivedAt,
        List<DiameterPeerSnapshot>? peers)
    {
        if (request == null || answer == null || sourcePeer == null ||
            peers == null)
        {
            return CreateRealmRedirectFailure(
                "realm-redirect-input-required",
                "The request, answer, source peer, and peers are required.");
        }

        request.ValidateRouteRequest();
        Table.ValidateRoutePeers(peers);
        DiameterValueResult normalizedSource =
            ValueCodecs.NormalizeDiameterIdentity(sourcePeer);
        if (normalizedSource.Status != DiameterCodecStatus.Success)
        {
            return CreateRealmRedirectFailure(
                "realm-redirect-source-invalid",
                "The redirecting peer identity is invalid.");
        }

        string headerFailure = Cache.ValidateRedirectAnswerHeader(
            request.Message,
            answer);
        if (headerFailure.Length > 0)
        {
            return CreateRealmRedirectFailure(
                "realm-redirect-answer-correlation-invalid",
                "The realm redirect answer does not match the request.");
        }

        RealmRedirectProcessingResult parsed = ParseRealmRedirectAnswer(
            answer);
        if (!parsed.Accepted)
        {
            return parsed;
        }

        uint usageValue = ReadOptionalUnsigned32(
            answer,
            DiameterAvpCodes.RedirectHostUsage);
        uint maximumSeconds = ReadOptionalUnsigned32(
            answer,
            DiameterAvpCodes.RedirectMaxCacheTime);
        bool cacheRequested = CountRealmRedirectAvps(
            answer,
            DiameterAvpCodes.RedirectHostUsage) == 1 &&
            usageValue != (uint)DiameterRedirectHostUsage.DoNotCache;
        int realmIndex = 0;
        while (realmIndex < parsed.Realms.Count)
        {
            DiameterIdentity redirectRealm = parsed.Realms[realmIndex];
            DiameterRouteRequest rewritten = RewriteRealmRedirectRequest(
                request,
                redirectRealm);
            RouteSelectionResult selection = Table.SelectStaticRoute(
                rewritten,
                peers);
            DiameterRouteCandidate? candidate = selection.SelectedCandidate;
            if (candidate != null && candidate.PeerIdentity != null &&
                (candidate.Action == DiameterRouteAction.Relay ||
                candidate.Action == DiameterRouteAction.Proxy))
            {
                DiameterRealmRedirectAuthorizationResult authorization =
                    AuthorizeRealmRedirect(
                        request,
                        normalizedSource.Text,
                        redirectRealm,
                        candidate,
                        sourceSecure);
                if (authorization.Authorized)
                {
                    DiameterRedirectEntry? entry = null;
                    if (cacheRequested)
                    {
                        entry = CacheRealmRedirect(
                            request,
                            normalizedSource.Text,
                            redirectRealm,
                            candidate,
                            (DiameterRedirectHostUsage)usageValue,
                            maximumSeconds,
                            sourceSecure,
                            receivedAt);
                    }

                    parsed.Rerouted = true;
                    parsed.Request = rewritten;
                    parsed.Candidate = RouteTableCloner.CloneRouteCandidate(
                        candidate);
                    parsed.Entry = entry;
                    parsed.Code = "realm-redirect-rerouted";
                    parsed.Message =
                        "The request was rerouted to an authorized alternate realm.";
                    return parsed;
                }
            }

            realmIndex = realmIndex + 1;
        }

        parsed.ForwardAnswer = true;
        parsed.Code = "realm-redirect-forward-answer";
        parsed.Message =
            "No authorized route to an alternate realm was available.";
        return parsed;
    }

    internal RealmRedirectProcessingResult ParseRealmRedirectAnswer(
        DiameterMessage answer)
    {
        RealmRedirectProcessingResult result =
            new RealmRedirectProcessingResult();
        uint resultCode = 0U;
        int resultCount = 0;
        int usageCount = 0;
        int maximumCount = 0;
        uint usageValue = 0U;
        uint maximumSeconds = 0U;
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
                    return CreateRealmRedirectFailure(
                        "realm-redirect-result-invalid",
                        "The realm redirect Result-Code is malformed.");
                }

                resultCode = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectRealm)
            {
                DiameterValueResult decoded =
                    ValueCodecs.DecodeDiameterIdentity(avp.Data);
                DiameterValueResult normalized =
                    ValueCodecs.NormalizeDiameterRealm(decoded.Identity);
                if (decoded.Status != DiameterCodecStatus.Success ||
                    normalized.Status != DiameterCodecStatus.Success ||
                    ContainsRealmRedirect(result.Realms, normalized.Text))
                {
                    return CreateRealmRedirectFailure(
                        "realm-redirect-realm-invalid",
                        "Redirect-Realm values must be valid and distinct.");
                }

                DiameterIdentity realm = new DiameterIdentity();
                realm.Value = normalized.Text;
                result.Realms.Add(realm);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectHostUsage)
            {
                usageCount = usageCount + 1;
                if (avp.Data.Length != 4)
                {
                    return CreateRealmRedirectFailure(
                        "realm-redirect-cache-invalid",
                        "Redirect-Host-Usage is malformed.");
                }

                usageValue = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectMaxCacheTime)
            {
                maximumCount = maximumCount + 1;
                if (avp.Data.Length != 4)
                {
                    return CreateRealmRedirectFailure(
                        "realm-redirect-cache-invalid",
                        "Redirect-Max-Cache-Time is malformed.");
                }

                maximumSeconds = NetworkByteOrder.ReadUInt32(avp.Data);
            }
            else if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.RedirectHost)
            {
                return CreateRealmRedirectFailure(
                    "realm-redirect-host-not-allowed",
                    "Realm redirect answers cannot contain Redirect-Host.");
            }

            index = index + 1;
        }

        if (resultCount != 1 ||
            resultCode != DiameterResultCodes.RealmRedirectIndication ||
            result.Realms.Count == 0)
        {
            return CreateRealmRedirectFailure(
                "realm-redirect-result-invalid",
                "Result-Code 3011 requires at least one Redirect-Realm.");
        }

        if (usageCount > 1 || maximumCount > 1 ||
            usageValue > (uint)DiameterRedirectHostUsage.AllUser ||
            (usageCount == 0 && maximumCount != 0) ||
            (usageCount == 1 &&
            (maximumCount != 1 || maximumSeconds == 0U)))
        {
            return CreateRealmRedirectFailure(
                "realm-redirect-cache-invalid",
                "Realm redirect cache AVPs are inconsistent.");
        }

        result.Accepted = true;
        result.Code = "realm-redirect-accepted";
        result.Message = "The realm redirect indication is valid.";
        return result;
    }

    internal DiameterRouteRequest RewriteRealmRedirectRequest(
        DiameterRouteRequest request,
        DiameterIdentity redirectRealm)
    {
        DiameterRouteRequest result =
            RouteTableCloner.CloneRouteRequest(request);
        result.DestinationHost = null;
        result.DestinationRealm = RouteTableCloner.CloneRouteIdentity(
            redirectRealm);
        int index = result.Message.Avps.Count - 1;
        while (index >= 0)
        {
            DiameterAvp avp = result.Message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                (avp.Code == DiameterAvpCodes.DestinationHost ||
                avp.Code == DiameterAvpCodes.DestinationRealm))
            {
                result.Message.Avps.RemoveAt(index);
            }

            index = index - 1;
        }

        result.Message.Avps.Add(AvpFactory.CreateDestinationRealmAvp(
            redirectRealm));
        return result;
    }

    internal DiameterRealmRedirectAuthorizationResult AuthorizeRealmRedirect(
        DiameterRouteRequest request,
        string sourcePeer,
        DiameterIdentity realm,
        DiameterRouteCandidate candidate,
        bool sourceSecure)
    {
        DiameterRealmRedirectAuthorizationResult accepted =
            new DiameterRealmRedirectAuthorizationResult();
        accepted.Authorized = true;
        accepted.Code = "realm-redirect-authorized";
        accepted.Message = "The discovered alternate realm route is authorized.";
        if (Policy == null)
        {
            return accepted;
        }

        DiameterRealmRedirectAuthorizationRequest policyRequest =
            new DiameterRealmRedirectAuthorizationRequest();
        policyRequest.Request = RouteTableCloner.CloneRouteRequest(request);
        policyRequest.RedirectingPeer.Value = sourcePeer;
        policyRequest.RedirectRealm.Value = realm.Value;
        policyRequest.Candidate = RouteTableCloner.CloneRouteCandidate(
            candidate);
        policyRequest.SourceSecure = sourceSecure;
        DiameterRealmRedirectAuthorizationResult decision =
            Policy.AuthorizeDiameterRealmRedirect(policyRequest);
        if (decision == null || decision.Code == null ||
            decision.Message == null)
        {
            throw new DiameterRoutingException(
                "The realm redirect policy returned an invalid decision.");
        }

        DiameterRealmRedirectAuthorizationResult result =
            new DiameterRealmRedirectAuthorizationResult();
        result.Authorized = decision.Authorized;
        result.Code = decision.Code;
        result.Message = decision.Message;
        return result;
    }

    internal DiameterRedirectEntry? CacheRealmRedirect(
        DiameterRouteRequest request,
        string sourcePeer,
        DiameterIdentity realm,
        DiameterRouteCandidate candidate,
        DiameterRedirectHostUsage usage,
        uint maximumSeconds,
        bool sourceSecure,
        DateTimeOffset receivedAt)
    {
        if (candidate.PeerIdentity == null)
        {
            return null;
        }

        DiameterUri host = new DiameterUri();
        host.Host.Value = candidate.PeerIdentity.Value;
        if (candidate.Peer != null &&
            Cache.IsRedirectPeerSecure(candidate.Peer))
        {
            host.Scheme = "aaas";
            host.Port = DiameterProtocolConstants.SecurePort;
        }

        List<DiameterUri> hosts = new List<DiameterUri>();
        hosts.Add(host);
        DiameterRedirectEntry? entry = Cache.CreateRedirectEntry(
            request,
            hosts,
            sourcePeer,
            usage,
            maximumSeconds,
            sourceSecure,
            receivedAt);
        if (entry == null || entry.ExpiresAt <= Cache.Clock.ReadUtcTime())
        {
            return null;
        }

        entry.RealmBased = true;
        entry.RedirectRealm = RouteTableCloner.CloneRouteIdentity(realm);
        entry.Cached = true;
        entry.Code = "realm-redirect-cached";
        Cache.StoreRedirectEntry(entry);
        return RouteTableCloner.CloneRedirectEntry(entry);
    }

    internal bool ContainsRealmRedirect(
        List<DiameterIdentity> realms,
        string value)
    {
        int index = 0;
        while (index < realms.Count)
        {
            if (realms[index].Value == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal uint ReadOptionalUnsigned32(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == code && avp.Data.Length == 4)
            {
                return NetworkByteOrder.ReadUInt32(avp.Data);
            }

            index = index + 1;
        }

        return 0U;
    }

    internal int CountRealmRedirectAvps(DiameterMessage message, uint code)
    {
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal RedirectAnswerGenerationResult CreateRealmRedirectGenerationFailure(
        string code)
    {
        RedirectAnswerGenerationResult result =
            new RedirectAnswerGenerationResult();
        result.Code = code;
        return result;
    }

    internal RealmRedirectProcessingResult CreateRealmRedirectFailure(
        string code,
        string message)
    {
        RealmRedirectProcessingResult result =
            new RealmRedirectProcessingResult();
        result.Code = code;
        result.Message = message;
        return result;
    }
}
