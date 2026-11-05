using System;
using System.Collections.Generic;

namespace diameter;

internal class RouteSelector
{
    internal DiameterIdentity LocalHost = new DiameterIdentity();
    internal DiameterIdentity LocalRealm = new DiameterIdentity();
    internal List<DiameterApplicationId> LocalApplications =
        new List<DiameterApplicationId>();
    internal bool LocalDeliveryEnabled;
    internal DiameterRoutingPriority DefaultRoutingPriority;
    internal RouteTable Table;
    internal UserNameRealmRouter RealmRouter;
    internal RouteRecordProcessor RouteRecords;
    internal RedirectRouteCache Redirects;
    internal RealmRedirectAgent RealmRedirects;
    internal TranslationAgent Translations;
    internal DiameterValidator Validator = new DiameterValidator();

    internal RouteSelector(
        DiameterNodeConfiguration configuration,
        RouteTable table)
    {
        if (configuration == null || table == null)
        {
            throw new ArgumentException(
                "A route selector requires node configuration and a route table.");
        }

        configuration.ValidateConfiguration();
        table.ConfigureRouteSelection(
            configuration.RoutePolicies,
            configuration.Limits.MaximumPendingRequests);
        LocalHost.Value = configuration.OriginHost.Value;
        LocalRealm.Value = configuration.OriginRealm.Value;
        DefaultRoutingPriority = configuration.DefaultRoutingPriority;
        RealmRouter = new UserNameRealmRouter(
            configuration.OriginRealm,
            configuration.RealmRouting);
        RouteRecords = new RouteRecordProcessor(
            configuration.OriginHost,
            configuration.Roles,
            configuration.RoutePathPolicy);
        Redirects = new RedirectRouteCache(
            configuration.OriginHost,
            table.Clock,
            configuration.Timers.RedirectCacheMaximumLifetimeMilliseconds,
            configuration.Limits.MaximumPendingRequests,
            configuration.RedirectHostPolicy);
        RealmRedirects = new RealmRedirectAgent(
            configuration.OriginHost,
            configuration.OriginRealm,
            table,
            Redirects,
            configuration.RealmRedirectPolicy);
        Translations = new TranslationAgent(
            configuration.OriginHost,
            configuration.OriginRealm,
            configuration.TranslationHandlers);
        LocalDeliveryEnabled = (configuration.Roles & DiameterNodeRole.Server) !=
            DiameterNodeRole.None;
        int applicationIndex = 0;
        while (applicationIndex < configuration.Applications.Count)
        {
            DiameterApplicationId source =
                configuration.Applications[applicationIndex];
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = source.Id;
            application.VendorId = source.VendorId;
            application.IsAccounting = source.IsAccounting;
            LocalApplications.Add(application);
            applicationIndex = applicationIndex + 1;
        }

        Table = table;
    }

    internal DiameterRouteDecision SelectRoute(
        DiameterRouteRequest request,
        List<DiameterPeerSnapshot> peers)
    {
        if (request == null)
        {
            throw new ArgumentException("A route request is required.");
        }

        request.ValidateRouteRequest();
        Table.ValidateRoutePeers(peers);
        DiameterRouteRequest working =
            RouteTableCloner.CloneRouteRequest(request);
        DrmpInspectionResult priority =
            DrmpProcessor.InspectRequestPriority(
                working.Message,
                DefaultRoutingPriority);
        if (!priority.Valid)
        {
            return CreateRouteErrorDecision(
                working,
                new List<DiameterRouteCandidate>(),
                DiameterResultCodes.InvalidAvpValue,
                priority.Code,
                priority.Message);
        }

        working.RoutingPriority = priority.Priority;
        working.RoutingPriorityPresent = priority.Present;
        RouteRecordInspectionResult routeRecords =
            RouteRecords.InspectRouteRecords(working);
        if (!routeRecords.Accepted)
        {
            uint resultCode = routeRecords.LoopDetected ?
                DiameterResultCodes.LoopDetected :
                DiameterResultCodes.InvalidAvpValue;
            return CreateRouteErrorDecision(
                working,
                new List<DiameterRouteCandidate>(),
                resultCode,
                routeRecords.Code,
                routeRecords.Message);
        }

        UserNameRealmRoutingResult realmRouting =
            RealmRouter.ProcessUserNameRealmRoute(working);
        if (!realmRouting.Accepted)
        {
            return CreateFailedRouteDecision(
                working,
                new List<DiameterRouteCandidate>(),
                DiameterResultCodes.InvalidAvpValue,
                realmRouting.Code,
                realmRouting.Message);
        }

        bool localHost = working.DestinationHost != null &&
            AreRouteIdentitiesEqual(working.DestinationHost, LocalHost);
        bool localRealm = working.DestinationRealm != null &&
            RealmRouter.IsConfiguredHomeRealm(working.DestinationRealm);
        bool localApplication = SupportsLocalApplication(working.ApplicationId);
        if (localRealm && LocalDeliveryEnabled && localApplication)
        {
            RouteSelectionResult configuredRealmRedirect =
                Table.SelectRealmRedirectRoute(working, peers);
            if (configuredRealmRedirect.SelectedCandidate != null)
            {
                return CreateSelectedRouteDecision(
                    working,
                    configuredRealmRedirect,
                    configuredRealmRedirect.SelectedCandidate);
            }
        }

        if (localHost && !realmRouting.Advanced)
        {
            if (LocalDeliveryEnabled && localApplication)
            {
                DiameterRoutePathAuthorizationResult authorization =
                    RouteRecords.AuthorizeRoutePath(
                        working,
                        routeRecords.RouteRecords);
                if (!authorization.Authorized)
                {
                    return CreateRouteErrorDecision(
                        working,
                        new List<DiameterRouteCandidate>(),
                        DiameterResultCodes.AuthorizationRejected,
                        authorization.Code,
                        authorization.Message);
                }

                return CreateLocalRouteDecision(working);
            }

            return CreateFailedRouteDecision(
                working,
                new List<DiameterRouteCandidate>(),
                DiameterResultCodes.ApplicationUnsupported,
                "route-local-application-unsupported",
                "The explicitly addressed local application is unavailable.");
        }

        if (working.DestinationHost == null && localRealm &&
            LocalDeliveryEnabled && localApplication)
        {
            DiameterRoutePathAuthorizationResult authorization =
                RouteRecords.AuthorizeRoutePath(
                    working,
                    routeRecords.RouteRecords);
            if (!authorization.Authorized)
            {
                return CreateRouteErrorDecision(
                    working,
                    new List<DiameterRouteCandidate>(),
                    DiameterResultCodes.AuthorizationRejected,
                    authorization.Code,
                    authorization.Message);
            }

            return CreateLocalRouteDecision(working);
        }

        RedirectRouteSelectionResult redirect =
            Redirects.SelectRedirectRoute(working, peers);
        if (redirect.SelectedCandidate != null && redirect.Entry != null)
        {
            return CreateRedirectRouteDecision(working, redirect);
        }

        RouteSelectionResult selection = Table.SelectStaticRoute(working, peers);
        DiameterRouteCandidate? selected = selection.SelectedCandidate;
        if (selected != null)
        {
            if (selected.Action == DiameterRouteAction.Local)
            {
                if (!LocalDeliveryEnabled || !localApplication)
                {
                    return CreateFailedRouteDecision(
                        working,
                        selection.Candidates,
                        DiameterResultCodes.ApplicationUnsupported,
                        "route-local-application-unsupported",
                        "The selected local application is unavailable.");
                }

                DiameterRoutePathAuthorizationResult authorization =
                    RouteRecords.AuthorizeRoutePath(
                        working,
                        routeRecords.RouteRecords);
                if (!authorization.Authorized)
                {
                    return CreateRouteErrorDecision(
                        working,
                        selection.Candidates,
                        DiameterResultCodes.AuthorizationRejected,
                        authorization.Code,
                        authorization.Message);
                }
            }

            return CreateSelectedRouteDecision(working, selection, selected);
        }

        if (localRealm && !localApplication &&
            selection.Candidates.Count == 0)
        {
            return CreateFailedRouteDecision(
                working,
                selection.Candidates,
                DiameterResultCodes.ApplicationUnsupported,
                "route-local-application-unsupported",
                "The destination realm is local but the application is unavailable.");
        }

        return CreateFailedRouteDecision(
            working,
            selection.Candidates,
            DiameterResultCodes.UnableToDeliver,
            "route-unable-to-deliver",
            "No usable route is available for the request.");
    }

    internal DiameterRouteDecision CreateLocalRouteDecision(
        DiameterRouteRequest request)
    {
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.Configuration.RouteIdentifier =
            "local-application-" + request.ApplicationId.ToString();
        candidate.Configuration.DestinationRealm.Value = LocalRealm.Value;
        if (request.DestinationRealm != null)
        {
            candidate.Configuration.DestinationRealm.Value =
                request.DestinationRealm.Value;
        }
        candidate.Configuration.DestinationHost = new DiameterIdentity();
        candidate.Configuration.DestinationHost.Value = LocalHost.Value;
        candidate.Configuration.ApplicationId = request.ApplicationId;
        candidate.Configuration.Action = DiameterRouteAction.Local;
        candidate.Configuration.AgentRole = DiameterAgentRole.None;
        candidate.Action = DiameterRouteAction.Local;
        candidate.AgentRole = DiameterAgentRole.None;
        candidate.Source = DiameterRouteSource.Static;
        candidate.Weight = 1;
        candidate.Order = -1;
        candidate.Available = true;
        candidate.ApplicationSupported = true;
        candidate.LoopFree = true;
        candidate.Secure = true;
        candidate.PolicyAccepted = true;
        candidate.Code = "route-local-application";
        candidate.Message = "The request is eligible for local application delivery.";

        DiameterRouteDecision result = new DiameterRouteDecision();
        result.Request = RouteTableCloner.CloneRouteRequest(request);
        result.Action = DiameterRouteAction.Local;
        result.AgentRole = DiameterAgentRole.None;
        result.SelectedCandidate = candidate;
        result.Candidates.Add(candidate);
        result.RoutedMessage = RouteTableCloner.CloneRouteMessage(request.Message);
        result.Routed = true;
        result.Code = "route-local";
        result.Message = "The request was selected for local application delivery.";
        return result;
    }

    internal DiameterRouteDecision CreateRedirectRouteDecision(
        DiameterRouteRequest request,
        RedirectRouteSelectionResult selection)
    {
        if (selection.SelectedCandidate == null || selection.Entry == null)
        {
            throw new DiameterRoutingException(
                "A selected redirect route is required.");
        }

        DiameterRouteDecision result = new DiameterRouteDecision();
        DiameterRouteRequest routedRequest = request;
        if (selection.Entry.RealmBased)
        {
            if (selection.Entry.RedirectRealm == null)
            {
                throw new DiameterRoutingException(
                    "A cached realm redirect requires an alternate realm.");
            }

            routedRequest = RealmRedirects.RewriteRealmRedirectRequest(
                request,
                selection.Entry.RedirectRealm);
        }

        result.Request = RouteTableCloner.CloneRouteRequest(routedRequest);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        result.SelectedCandidate = selection.SelectedCandidate;
        int candidateIndex = 0;
        while (candidateIndex < selection.Candidates.Count)
        {
            result.Candidates.Add(selection.Candidates[candidateIndex]);
            candidateIndex = candidateIndex + 1;
        }

        result.RedirectEntries.Add(RouteTableCloner.CloneRedirectEntry(
            selection.Entry));
        result.RoutedMessage = RouteTableCloner.CloneRouteMessage(
            routedRequest.Message);
        RouteRecords.AppendForwardingRouteRecord(
            result.RoutedMessage,
            request.SourcePeer);
        result.Routed = true;
        result.Code = "route-redirect-cache";
        result.Message = "A cached redirect route was selected for the request.";
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
        return RealmRedirects.ProcessRealmRedirectAnswer(
            request,
            answer,
            sourcePeer,
            sourceSecure,
            receivedAt,
            peers);
    }

    internal RedirectProcessingResult ProcessRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        bool sourceSecure,
        DateTimeOffset receivedAt)
    {
        return Redirects.ProcessRedirectAnswer(
            request,
            answer,
            sourcePeer,
            sourceSecure,
            receivedAt);
    }

    internal List<DiameterRedirectEntry> ReadRedirectEntries()
    {
        return Redirects.ReadRedirectEntries();
    }

    internal DiameterRouteDecision CreateRouteErrorDecision(
        DiameterRouteRequest request,
        List<DiameterRouteCandidate> candidates,
        uint resultCode,
        string code,
        string message)
    {
        DiameterRouteDecision result = CreateFailedRouteDecision(
            request,
            candidates,
            resultCode,
            code,
            message);
        result.Rejected = true;
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = RouteTableCloner.CloneRouteMessage(request.Message);
        error.ResultCode = resultCode;
        error.ErrorMessage = message;
        result.RoutedMessage = Validator.BuildResultErrorAnswer(
            error,
            LocalHost,
            LocalRealm);
        return result;
    }

    internal DiameterRouteDecision CreateSelectedRouteDecision(
        DiameterRouteRequest request,
        RouteSelectionResult selection,
        DiameterRouteCandidate selected)
    {
        DiameterRouteDecision result = new DiameterRouteDecision();
        result.Request = RouteTableCloner.CloneRouteRequest(request);
        result.Action = selected.Action;
        result.AgentRole = selected.AgentRole;
        result.SelectedCandidate = selected;
        int candidateIndex = 0;
        while (candidateIndex < selection.Candidates.Count)
        {
            result.Candidates.Add(selection.Candidates[candidateIndex]);
            candidateIndex = candidateIndex + 1;
        }

        if (selected.Action == DiameterRouteAction.Reject)
        {
            result.ResultCode = DiameterResultCodes.UnableToDeliver;
            result.Rejected = true;
            result.Code = "route-rejected";
            result.Message = "The selected static route rejects the request.";
            return result;
        }

        if (selected.Action == DiameterRouteAction.Translate)
        {
            TranslationRequestDispatchResult dispatch =
                Translations.DispatchTranslationRequest(result);
            result.TranslationResult =
                RouteTableCloner.CloneTranslationResult(
                    dispatch.Translation);
            result.RoutedMessage = dispatch.Message;
            result.ResultCode = dispatch.Translation.ResultCode;
            result.Routed = dispatch.Routed;
            result.Rejected = dispatch.Error;
            result.Code = dispatch.Code;
            result.Message = dispatch.MessageText;
            return result;
        }

        result.RoutedMessage = RouteTableCloner.CloneRouteMessage(request.Message);
        if (selected.Action == DiameterRouteAction.Relay ||
            selected.Action == DiameterRouteAction.Proxy)
        {
            RouteRecords.AppendForwardingRouteRecord(
                result.RoutedMessage,
                request.SourcePeer);
        }

        result.Routed = true;
        result.Code = selected.Action == DiameterRouteAction.Local ?
            "route-local" : "route-selected";
        result.Message = selected.Action == DiameterRouteAction.Local ?
            "The request was selected for local application delivery." :
            "A static route was selected for the request.";
        return result;
    }

    internal DiameterRouteDecision CreateFailedRouteDecision(
        DiameterRouteRequest request,
        List<DiameterRouteCandidate> candidates,
        uint resultCode,
        string code,
        string message)
    {
        DiameterRouteDecision result = new DiameterRouteDecision();
        result.Request = RouteTableCloner.CloneRouteRequest(request);
        result.Action = DiameterRouteAction.NoRoute;
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            result.Candidates.Add(candidates[candidateIndex]);
            candidateIndex = candidateIndex + 1;
        }

        result.ResultCode = resultCode;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal bool SupportsLocalApplication(uint applicationId)
    {
        if (applicationId == DiameterApplicationIds.Relay)
        {
            return false;
        }

        int index = 0;
        while (index < LocalApplications.Count)
        {
            if (LocalApplications[index].Id == applicationId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AreRouteIdentitiesEqual(
        DiameterIdentity first,
        DiameterIdentity second)
    {
        return ValueCodecs.NormalizeDiameterIdentity(first).Text ==
            ValueCodecs.NormalizeDiameterIdentity(second).Text;
    }

}
