using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class RoutingDataTests
{
    [Test]
    public void RoutingDataDefaultsOwnRequiredStorage()
    {
        DiameterRouteConfiguration configuration =
            new DiameterRouteConfiguration();
        DiameterRouteRequest request = new DiameterRouteRequest();
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        DiameterRouteDecision decision = new DiameterRouteDecision();
        DiameterRoutePathAuthorizationRequest pathRequest =
            new DiameterRoutePathAuthorizationRequest();
        DiameterRoutePathAuthorizationResult pathResult =
            new DiameterRoutePathAuthorizationResult();
        DiameterRouteSnapshot snapshot = new DiameterRouteSnapshot();
        DiameterRedirectEntry redirect = new DiameterRedirectEntry();
        DiameterRedirectHostAuthorizationRequest redirectRequest =
            new DiameterRedirectHostAuthorizationRequest();
        DiameterRedirectHostAuthorizationResult redirectResult =
            new DiameterRedirectHostAuthorizationResult();
        DiameterRealmRedirectAuthorizationRequest realmRedirectRequest =
            new DiameterRealmRedirectAuthorizationRequest();
        DiameterRealmRedirectAuthorizationResult realmRedirectResult =
            new DiameterRealmRedirectAuthorizationResult();
        DiameterTranslationResult translation =
            new DiameterTranslationResult();
        DiameterTranslationHandlerConfiguration translationHandler =
            new DiameterTranslationHandlerConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.RouteIdentifier, Is.Empty);
            Assert.That(configuration.DestinationRealm, Is.Not.Null);
            Assert.That(configuration.DestinationHost, Is.Null);
            Assert.That(configuration.PeerIdentity, Is.Null);
            Assert.That(configuration.RedirectHosts, Is.Empty);
            Assert.That(configuration.RedirectRealms, Is.Empty);
            Assert.That(configuration.RedirectUsage,
                Is.EqualTo(DiameterRedirectHostUsage.DoNotCache));
            Assert.That(configuration.RedirectMaximumCacheTimeSeconds,
                Is.Zero);
            Assert.That(configuration.RedirectInitialSessionRequestsOnly,
                Is.False);
            Assert.That(configuration.RedirectRequestsWithDestinationHost,
                Is.False);
            Assert.That(configuration.Action,
                Is.EqualTo(DiameterRouteAction.NoRoute));
            Assert.That(configuration.AgentRole,
                Is.EqualTo(DiameterAgentRole.None));
            Assert.That(configuration.Weight, Is.EqualTo(1));
            Assert.That(configuration.Enabled, Is.True);
            Assert.That(request.Message, Is.Not.Null);
            Assert.That(request.ExcludedPeers, Is.Empty);
            Assert.That(request.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority10));
            Assert.That(request.AllowRedirect, Is.True);
            Assert.That(request.AllowTranslation, Is.True);
            Assert.That(request.InitialSessionRequest, Is.True);
            Assert.That(candidate.Configuration, Is.Not.Null);
            Assert.That(candidate.Weight, Is.EqualTo(1));
            Assert.That(decision.Request, Is.Not.Null);
            Assert.That(decision.SelectedCandidate, Is.Null);
            Assert.That(decision.Candidates, Is.Empty);
            Assert.That(decision.RedirectEntries, Is.Empty);
            Assert.That(decision.TranslationResult, Is.Null);
            Assert.That(decision.RoutedMessage, Is.Null);
            Assert.That(pathRequest.Request, Is.Not.Null);
            Assert.That(pathRequest.LocalHost, Is.Not.Null);
            Assert.That(pathRequest.TraversedPeers, Is.Empty);
            Assert.That(pathResult.Authorized, Is.False);
            Assert.That(pathResult.Code, Is.Empty);
            Assert.That(pathResult.Message, Is.Empty);
            Assert.That(snapshot.Configuration, Is.Not.Null);
            Assert.That(redirect.Hosts, Is.Empty);
            Assert.That(redirect.RedirectingPeer, Is.Not.Null);
            Assert.That(redirectRequest.Request, Is.Not.Null);
            Assert.That(redirectRequest.RedirectingPeer, Is.Not.Null);
            Assert.That(redirectRequest.RedirectHost, Is.Not.Null);
            Assert.That(redirectRequest.SourceSecure, Is.False);
            Assert.That(redirectResult.Authorized, Is.False);
            Assert.That(redirectResult.Code, Is.Empty);
            Assert.That(redirectResult.Message, Is.Empty);
            Assert.That(redirect.RealmBased, Is.False);
            Assert.That(redirect.RedirectRealm, Is.Null);
            Assert.That(realmRedirectRequest.Request, Is.Not.Null);
            Assert.That(realmRedirectRequest.RedirectingPeer, Is.Not.Null);
            Assert.That(realmRedirectRequest.RedirectRealm, Is.Not.Null);
            Assert.That(realmRedirectRequest.Candidate, Is.Not.Null);
            Assert.That(realmRedirectRequest.SourceSecure, Is.False);
            Assert.That(realmRedirectResult.Authorized, Is.False);
            Assert.That(realmRedirectResult.Code, Is.Empty);
            Assert.That(realmRedirectResult.Message, Is.Empty);
            Assert.That(translation.SourceMessage, Is.Null);
            Assert.That(translation.TranslatedMessage, Is.Null);
            Assert.That(translation.TranslationState, Is.Empty);
            Assert.That(translation.Disposition,
                Is.EqualTo(DiameterTranslationDisposition.Untranslated));
            Assert.That(translationHandler.SourceApplicationId, Is.Zero);
            Assert.That(translationHandler.TargetApplicationId, Is.Zero);
            Assert.That(translationHandler.Handler, Is.Null);
            Assert.That(translationHandler.Enabled, Is.True);
        });
    }

    [Test]
    public void RouteConfigurationValidatesLocalAgentRedirectAndDefaultRoutes()
    {
        DiameterRouteConfiguration local =
            CreateRoutingTestConfiguration(
                "local-auth",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        DiameterRouteConfiguration relay =
            CreateRoutingTestConfiguration(
                "relay-auth",
                DiameterRouteAction.Relay,
                DiameterAgentRole.Relay);
        relay.PeerIdentity = CreateRoutingTestIdentity("peer.example");
        relay.Priority = 10;
        relay.Weight = 20;
        relay.RequireSecurePeer = true;
        DiameterRouteConfiguration proxy =
            CreateRoutingTestConfiguration(
                "proxy-auth",
                DiameterRouteAction.Proxy,
                DiameterAgentRole.Proxy);
        proxy.PeerIdentity = CreateRoutingTestIdentity("proxy.example");
        DiameterRouteConfiguration redirect =
            CreateRoutingTestConfiguration(
                "redirect-auth",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        redirect.RedirectHosts.Add(
            CreateRoutingTestUri("redirect.example", 3868));
        DiameterRouteConfiguration cachedRedirect =
            CreateRoutingTestConfiguration(
                "cached-redirect-auth",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        cachedRedirect.RedirectHosts.Add(
            CreateRoutingTestUri("cached-redirect.example", 3868));
        cachedRedirect.RedirectUsage =
            DiameterRedirectHostUsage.AllSession;
        cachedRedirect.RedirectMaximumCacheTimeSeconds = 60U;
        DiameterRouteConfiguration realmRedirect =
            CreateRoutingTestConfiguration(
                "realm-redirect-auth",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        realmRedirect.RedirectRealms.Add(
            CreateRoutingTestIdentity("alternate.example"));
        realmRedirect.RedirectUsage =
            DiameterRedirectHostUsage.RealmAndApplication;
        realmRedirect.RedirectMaximumCacheTimeSeconds = 60U;
        realmRedirect.RedirectInitialSessionRequestsOnly = true;
        realmRedirect.RedirectRequestsWithDestinationHost = true;
        DiameterRouteConfiguration translation =
            CreateRoutingTestConfiguration(
                "translate-auth",
                DiameterRouteAction.Translate,
                DiameterAgentRole.Translation);
        translation.TargetApplicationId = 9;
        DiameterRouteConfiguration fallback =
            CreateRoutingTestConfiguration(
                "fallback",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        fallback.DefaultRoute = true;
        fallback.DestinationRealm.Value = string.Empty;
        fallback.AnyApplication = true;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(local.ValidateRouteConfiguration);
            Assert.DoesNotThrow(relay.ValidateRouteConfiguration);
            Assert.DoesNotThrow(proxy.ValidateRouteConfiguration);
            Assert.DoesNotThrow(redirect.ValidateRouteConfiguration);
            Assert.DoesNotThrow(cachedRedirect.ValidateRouteConfiguration);
            Assert.DoesNotThrow(realmRedirect.ValidateRouteConfiguration);
            Assert.DoesNotThrow(translation.ValidateRouteConfiguration);
            Assert.DoesNotThrow(fallback.ValidateRouteConfiguration);
        });
    }

    [Test]
    public void RouteConfigurationRejectsInvalidShapeRoleTargetAndSelectionData()
    {
        DiameterRouteConfiguration missingIdentifier =
            CreateRoutingTestConfiguration(
                "valid",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        missingIdentifier.RouteIdentifier = " ";
        DiameterRouteConfiguration missingDestination =
            CreateRoutingTestConfiguration(
                "missing-destination",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        missingDestination.DestinationRealm.Value = string.Empty;
        DiameterRouteConfiguration constrainedDefault =
            CreateRoutingTestConfiguration(
                "constrained-default",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        constrainedDefault.DefaultRoute = true;
        DiameterRouteConfiguration wrongRole =
            CreateRoutingTestConfiguration(
                "wrong-role",
                DiameterRouteAction.Proxy,
                DiameterAgentRole.Relay);
        wrongRole.PeerIdentity = CreateRoutingTestIdentity("peer.example");
        DiameterRouteConfiguration missingPeer =
            CreateRoutingTestConfiguration(
                "missing-peer",
                DiameterRouteAction.Relay,
                DiameterAgentRole.Relay);
        DiameterRouteConfiguration missingRedirect =
            CreateRoutingTestConfiguration(
                "missing-redirect",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        DiameterRouteConfiguration invalidRedirect =
            CreateRoutingTestConfiguration(
                "invalid-redirect",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        invalidRedirect.RedirectHosts.Add(
            CreateRoutingTestUri("not a host", 3868));
        DiameterRouteConfiguration negativePriority =
            CreateRoutingTestConfiguration(
                "negative-priority",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        negativePriority.Priority = -1;
        DiameterRouteConfiguration zeroWeight =
            CreateRoutingTestConfiguration(
                "zero-weight",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);
        zeroWeight.Weight = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                missingIdentifier.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                missingDestination.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                constrainedDefault.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                wrongRole.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                missingPeer.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                missingRedirect.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                invalidRedirect.ValidateRouteConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                negativePriority.ValidateRouteConfiguration);
            Assert.Throws<ArgumentOutOfRangeException>(
                zeroWeight.ValidateRouteConfiguration);
        });
    }

    [Test]
    public void RouteRequestValidationAcceptsRoutingContextAndRetransmission()
    {
        DiameterRouteRequest request = CreateValidRoutingTestRequest();
        request.Message.Flags = request.Message.Flags |
            DiameterMessageFlags.PotentiallyRetransmitted;
        request.SourcePeer = CreateRoutingTestIdentity("source.example");
        request.OriginHost = CreateRoutingTestIdentity("origin.example");
        request.DestinationRealm =
            CreateRoutingTestIdentity("destination.example");
        request.DestinationHost =
            CreateRoutingTestIdentity("server.example");
        request.SessionId = "origin.example;1;2";
        request.UserName = "subscriber@destination.example";
        request.RoutingPriority = DiameterRoutingPriority.Priority3;
        request.RoutingPriorityPresent = true;
        request.ExcludedPeers.Add(
            CreateRoutingTestIdentity("first.example"));
        request.ExcludedPeers.Add(
            CreateRoutingTestIdentity("second.example"));
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 900;
        avp.Data = new byte[] { 1, 2 };
        request.Message.Avps.Add(avp);

        Assert.DoesNotThrow(request.ValidateRouteRequest);
    }

    [Test]
    public void RouteRequestValidationRejectsInvalidMessagesAndContext()
    {
        DiameterRouteRequest answer = CreateValidRoutingTestRequest();
        answer.Message.Flags = DiameterMessageFlags.None;
        DiameterRouteRequest application = CreateValidRoutingTestRequest();
        application.ApplicationId = 5;
        DiameterRouteRequest identity = CreateValidRoutingTestRequest();
        identity.SourcePeer = CreateRoutingTestIdentity("not a peer");
        DiameterRouteRequest duplicateExclusion =
            CreateValidRoutingTestRequest();
        duplicateExclusion.ExcludedPeers.Add(
            CreateRoutingTestIdentity("PEER.EXAMPLE"));
        duplicateExclusion.ExcludedPeers.Add(
            CreateRoutingTestIdentity("peer.example"));
        DiameterRouteRequest priority = CreateValidRoutingTestRequest();
        priority.RoutingPriority = (DiameterRoutingPriority)16;
        DiameterRouteRequest text = CreateValidRoutingTestRequest();
        text.SessionId = "invalid\0session";
        DiameterRouteRequest storage = CreateValidRoutingTestRequest();
        DiameterAvp avp = new DiameterAvp();
        avp.Data = null!;
        storage.Message.Avps.Add(avp);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(answer.ValidateRouteRequest);
            Assert.Throws<ArgumentException>(application.ValidateRouteRequest);
            Assert.Throws<ArgumentException>(identity.ValidateRouteRequest);
            Assert.Throws<ArgumentException>(
                duplicateExclusion.ValidateRouteRequest);
            Assert.Throws<ArgumentOutOfRangeException>(
                priority.ValidateRouteRequest);
            Assert.Throws<ArgumentException>(text.ValidateRouteRequest);
            Assert.Throws<ArgumentException>(storage.ValidateRouteRequest);
        });
    }

    [Test]
    public void RoutingContractsCarryDecisionsPoliciesRedirectsAndTranslations()
    {
        RoutingContractProbe routerProbe = new RoutingContractProbe();
        RoutePolicyContractProbe policyProbe =
            new RoutePolicyContractProbe();
        RoutePathPolicyContractProbe pathPolicyProbe =
            new RoutePathPolicyContractProbe();
        RedirectHostPolicyContractProbe redirectPolicyProbe =
            new RedirectHostPolicyContractProbe();
        RealmRedirectPolicyContractProbe realmRedirectPolicyProbe =
            new RealmRedirectPolicyContractProbe();
        TranslationContractProbe translationProbe =
            new TranslationContractProbe();
        DiameterRouteRequest request = CreateValidRoutingTestRequest();
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.Action = DiameterRouteAction.Relay;
        candidate.AgentRole = DiameterAgentRole.Relay;
        candidate.Source = DiameterRouteSource.Static;
        candidate.PolicyAccepted = true;
        routerProbe.Decision.Request = request;
        routerProbe.Decision.Action = DiameterRouteAction.Relay;
        routerProbe.Decision.SelectedCandidate = candidate;
        routerProbe.Decision.Candidates.Add(candidate);
        routerProbe.Decision.Routed = true;
        routerProbe.Decision.Code = "route-selected";
        DiameterRedirectEntry redirect = new DiameterRedirectEntry();
        redirect.Usage = DiameterRedirectHostUsage.AllSession;
        routerProbe.Redirects.Add(redirect);
        DiameterRouteSnapshot snapshot = new DiameterRouteSnapshot();
        snapshot.Source = DiameterRouteSource.Static;
        routerProbe.Snapshots.Add(snapshot);
        policyProbe.Result = candidate;
        pathPolicyProbe.Result.Authorized = true;
        pathPolicyProbe.Result.Code = "path-authorized";
        redirectPolicyProbe.Result.Authorized = true;
        redirectPolicyProbe.Result.Code = "redirect-authorized";
        realmRedirectPolicyProbe.Result.Authorized = true;
        realmRedirectPolicyProbe.Result.Code = "realm-redirect-authorized";
        translationProbe.RequestResult.Disposition =
            DiameterTranslationDisposition.Translated;
        translationProbe.RequestResult.TargetApplicationId = 9;
        translationProbe.AnswerResult.Disposition =
            DiameterTranslationDisposition.Translated;
        IDiameterRouter router = routerProbe;
        IRoutePolicy policy = policyProbe;
        IRoutePathPolicy pathPolicy = pathPolicyProbe;
        IRedirectHostPolicy redirectPolicy = redirectPolicyProbe;
        IRealmRedirectPolicy realmRedirectPolicy = realmRedirectPolicyProbe;
        ITranslationHandler translator = translationProbe;
        DateTimeOffset receivedAt = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);

        DiameterRouteDecision decision =
            router.SelectDiameterRoute(request);
        List<DiameterRedirectEntry> redirects =
            router.ProcessDiameterRedirectAnswer(
                request,
                request.Message,
                CreateRoutingTestIdentity("peer.example"),
                receivedAt);
        List<DiameterRouteSnapshot> snapshots =
            router.ReadDiameterRouteSnapshots();
        DiameterRouteCandidate evaluated =
            policy.EvaluateDiameterRouteCandidate(request, candidate);
        DiameterRoutePathAuthorizationRequest pathRequest =
            new DiameterRoutePathAuthorizationRequest();
        DiameterRoutePathAuthorizationResult pathResult =
            pathPolicy.AuthorizeDiameterRoutePath(pathRequest);
        DiameterRedirectHostAuthorizationRequest redirectRequest =
            new DiameterRedirectHostAuthorizationRequest();
        DiameterRedirectHostAuthorizationResult redirectResult =
            redirectPolicy.AuthorizeDiameterRedirectHost(redirectRequest);
        DiameterRealmRedirectAuthorizationRequest realmRedirectRequest =
            new DiameterRealmRedirectAuthorizationRequest();
        DiameterRealmRedirectAuthorizationResult realmRedirectResult =
            realmRedirectPolicy.AuthorizeDiameterRealmRedirect(
                realmRedirectRequest);
        DiameterTranslationResult translatedRequest =
            translator.TranslateDiameterRequest(request, 9);
        DiameterTranslationResult translatedAnswer =
            translator.TranslateDiameterAnswer(
                translatedRequest,
                request.Message);

        Assert.Multiple(() =>
        {
            Assert.That(decision, Is.SameAs(routerProbe.Decision));
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.SelectedCandidate, Is.SameAs(candidate));
            Assert.That(routerProbe.ObservedRequest, Is.SameAs(request));
            Assert.That(routerProbe.ObservedRedirectAnswer,
                Is.SameAs(request.Message));
            Assert.That(routerProbe.ObservedRedirectPeer!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(routerProbe.ObservedRedirectAt, Is.EqualTo(receivedAt));
            Assert.That(redirects, Is.SameAs(routerProbe.Redirects));
            Assert.That(snapshots, Is.SameAs(routerProbe.Snapshots));
            Assert.That(evaluated, Is.SameAs(candidate));
            Assert.That(policyProbe.ObservedRequest, Is.SameAs(request));
            Assert.That(policyProbe.ObservedCandidate, Is.SameAs(candidate));
            Assert.That(pathResult, Is.SameAs(pathPolicyProbe.Result));
            Assert.That(pathPolicyProbe.ObservedRequest,
                Is.SameAs(pathRequest));
            Assert.That(redirectResult,
                Is.SameAs(redirectPolicyProbe.Result));
            Assert.That(redirectPolicyProbe.ObservedRequest,
                Is.SameAs(redirectRequest));
            Assert.That(realmRedirectResult,
                Is.SameAs(realmRedirectPolicyProbe.Result));
            Assert.That(realmRedirectPolicyProbe.ObservedRequest,
                Is.SameAs(realmRedirectRequest));
            Assert.That(translatedRequest,
                Is.SameAs(translationProbe.RequestResult));
            Assert.That(translationProbe.TargetApplicationId, Is.EqualTo(9));
            Assert.That(translatedAnswer,
                Is.SameAs(translationProbe.AnswerResult));
            Assert.That(translationProbe.ObservedRequestTranslation,
                Is.SameAs(translatedRequest));
            Assert.That(translationProbe.ObservedAnswer,
                Is.SameAs(request.Message));
        });
    }

    [Test]
    public void PopulatedRoutingMetadataRetainsAllOutcomeDimensions()
    {
        DiameterRouteConfiguration configuration =
            CreateRoutingTestConfiguration(
                "route-1",
                DiameterRouteAction.Proxy,
                DiameterAgentRole.Proxy);
        configuration.PeerIdentity =
            CreateRoutingTestIdentity("peer.example");
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.Configuration = configuration;
        candidate.PeerIdentity = configuration.PeerIdentity;
        candidate.Action = DiameterRouteAction.Proxy;
        candidate.AgentRole = DiameterAgentRole.Proxy;
        candidate.Source = DiameterRouteSource.SessionAffinity;
        candidate.Priority = 2;
        candidate.Weight = 7;
        candidate.PolicyScore = 11;
        candidate.Order = 3;
        candidate.Available = true;
        candidate.ApplicationSupported = true;
        candidate.LoopFree = true;
        candidate.Secure = true;
        candidate.PolicyAccepted = true;
        candidate.PolicyTerminal = true;
        DiameterRedirectEntry redirect = new DiameterRedirectEntry();
        redirect.RedirectKey = "session|one";
        redirect.Usage = DiameterRedirectHostUsage.AllSession;
        redirect.Hosts.Add(CreateRoutingTestUri("redirect.example", 3869));
        redirect.DestinationRealm =
            CreateRoutingTestIdentity("realm.example");
        redirect.DestinationHost =
            CreateRoutingTestIdentity("server.example");
        redirect.ApplicationId = 4;
        redirect.SessionId = "session-one";
        redirect.UserName = "user@realm.example";
        redirect.RedirectingPeer =
            CreateRoutingTestIdentity("peer.example");
        redirect.MaximumCacheTimeSeconds = 60;
        redirect.Secure = true;
        redirect.Authorized = true;
        redirect.Cached = true;
        DiameterTranslationResult translation =
            new DiameterTranslationResult();
        translation.Disposition = DiameterTranslationDisposition.Translated;
        translation.SourceApplicationId = 4;
        translation.TargetApplicationId = 9;
        translation.TranslationState = new byte[] { 1, 2, 3 };
        DiameterRouteDecision decision = new DiameterRouteDecision();
        decision.Action = DiameterRouteAction.Proxy;
        decision.AgentRole = DiameterAgentRole.Proxy;
        decision.SelectedCandidate = candidate;
        decision.Candidates.Add(candidate);
        decision.RedirectEntries.Add(redirect);
        decision.TranslationResult = translation;
        decision.RoutedMessage = CreateValidRoutingTestRequest().Message;
        decision.Routed = true;
        decision.Code = "route-selected";
        DiameterRouteSnapshot snapshot = new DiameterRouteSnapshot();
        snapshot.Configuration = configuration;
        snapshot.PeerIdentity = configuration.PeerIdentity;
        snapshot.Source = DiameterRouteSource.Static;
        snapshot.Available = true;
        snapshot.ApplicationSupported = true;
        snapshot.Healthy = true;
        snapshot.SelectionCount = 10;
        snapshot.FailureCount = 2;

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Proxy));
            Assert.That(decision.SelectedCandidate!.PolicyScore,
                Is.EqualTo(11));
            Assert.That(decision.SelectedCandidate.PolicyTerminal, Is.True);
            Assert.That(decision.RedirectEntries[0].Usage,
                Is.EqualTo(DiameterRedirectHostUsage.AllSession));
            Assert.That(decision.RedirectEntries[0].Hosts[0].Port,
                Is.EqualTo(3869));
            Assert.That(decision.TranslationResult!.TranslationState,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(snapshot.Source,
                Is.EqualTo(DiameterRouteSource.Static));
            Assert.That(snapshot.SelectionCount, Is.EqualTo(10));
            Assert.That(snapshot.FailureCount, Is.EqualTo(2));
            Assert.That(snapshot.Healthy, Is.True);
        });
    }

    [Test]
    public void RouteConfigurationRejectsEveryRemainingShapeAndCacheBranch()
    {
        Func<DiameterRouteConfiguration> local = () =>
            CreateRoutingTestConfiguration(
                "local",
                DiameterRouteAction.Local,
                DiameterAgentRole.None);

        AssertEveryMutationIsRejected(
            local,
            value => value.ValidateRouteConfiguration(),
            value => value.DestinationRealm = null!,
            value => value.DestinationRealm.Value = null!,
            value => value.RedirectHosts = null!,
            value => value.RedirectRealms = null!,
            value => value.DestinationHost =
                CreateRoutingTestIdentity("bad host"),
            value => value.Action = DiameterRouteAction.NoRoute,
            value => value.Action = (DiameterRouteAction)7,
            value => value.AgentRole = (DiameterAgentRole)(-1),
            value => value.AgentRole = (DiameterAgentRole)6,
            value => value.TargetApplicationId = 9U,
            value => value.RedirectUsage =
                (DiameterRedirectHostUsage)(-1),
            value => value.RedirectUsage =
                (DiameterRedirectHostUsage)7,
            value =>
            {
                value.RedirectUsage = DiameterRedirectHostUsage.AllSession;
                value.RedirectMaximumCacheTimeSeconds = 60U;
            });

        AssertEveryMutationIsRejected(
            CreateRedirectRoute,
            value => value.ValidateRouteConfiguration(),
            value =>
            {
                value.RedirectRealms.Add(
                    CreateRoutingTestIdentity("alternate.example"));
            },
            value => value.RedirectInitialSessionRequestsOnly = true,
            value => value.RedirectRequestsWithDestinationHost = true,
            value => value.RedirectMaximumCacheTimeSeconds = 1U,
            value => value.RedirectHosts[0] = null!,
            value => value.RedirectHosts[0].Protocol = "aaa");

        DiameterRouteConfiguration translate =
            CreateRoutingTestConfiguration(
                "translate",
                DiameterRouteAction.Translate,
                DiameterAgentRole.Translation);
        translate.TargetApplicationId = translate.ApplicationId;
        DiameterRouteConfiguration cachedWithoutLifetime =
            CreateRedirectRoute();
        cachedWithoutLifetime.RedirectUsage =
            DiameterRedirectHostUsage.AllSession;
        DiameterRouteConfiguration mixedRedirect =
            CreateRedirectRoute();
        mixedRedirect.RedirectRealms.Add(
            CreateRoutingTestIdentity("alternate.example"));
        DiameterRouteConfiguration redirectRealmsOnLocal = local();
        redirectRealmsOnLocal.RedirectRealms.Add(
            CreateRoutingTestIdentity("alternate.example"));
        DiameterRouteConfiguration invalidSecondRealm =
            CreateRealmRedirectRoute();
        invalidSecondRealm.RedirectRealms.Add(
            CreateRoutingTestIdentity("bad realm"));
        DiameterRouteConfiguration duplicateRealm =
            CreateRealmRedirectRoute();
        duplicateRealm.RedirectRealms.Add(
            CreateRoutingTestIdentity("ALTERNATE.EXAMPLE"));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                translate.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                cachedWithoutLifetime.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                mixedRedirect.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                redirectRealmsOnLocal.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                invalidSecondRealm.ValidateRouteConfiguration);
            Assert.Throws<ArgumentException>(
                duplicateRealm.ValidateRouteConfiguration);
        });
    }

    [Test]
    public void RouteRequestRejectsEveryRemainingOwnedMessageBranch()
    {
        AssertEveryMutationIsRejected(
            CreateValidRoutingTestRequest,
            value => value.ValidateRouteRequest(),
            value => value.Message = null!,
            value => value.Message.Avps = null!,
            value => value.ExcludedPeers = null!,
            value => value.RoutingPriority =
                (DiameterRoutingPriority)(-1),
            value => value.Message.Avps.Add(null!),
            value => value.ExcludedPeers.Add(
                CreateRoutingTestIdentity("bad peer")),
            value =>
            {
                DiameterIdentity first =
                    CreateRoutingTestIdentity("first.example");
                DiameterIdentity invalid =
                    CreateRoutingTestIdentity("bad peer");
                value.ExcludedPeers.Add(first);
                value.ExcludedPeers.Add(invalid);
            });
    }

    private static DiameterRouteConfiguration CreateRoutingTestConfiguration(
        string identifier,
        DiameterRouteAction action,
        DiameterAgentRole role)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = "realm.example";
        result.ApplicationId = 4;
        result.Action = action;
        result.AgentRole = role;
        return result;
    }

    private static DiameterRouteConfiguration CreateRedirectRoute()
    {
        DiameterRouteConfiguration result =
            CreateRoutingTestConfiguration(
                "redirect",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        result.RedirectHosts.Add(
            CreateRoutingTestUri("redirect.example", 3868));
        return result;
    }

    private static DiameterRouteConfiguration CreateRealmRedirectRoute()
    {
        DiameterRouteConfiguration result =
            CreateRoutingTestConfiguration(
                "realm-redirect",
                DiameterRouteAction.Redirect,
                DiameterAgentRole.Redirect);
        result.RedirectRealms.Add(
            CreateRoutingTestIdentity("alternate.example"));
        return result;
    }

    private static DiameterRouteRequest CreateValidRoutingTestRequest()
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Message.CommandCode = 300;
        result.Message.ApplicationId = 4;
        result.ApplicationId = 4;
        return result;
    }

    private static DiameterIdentity CreateRoutingTestIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterUri CreateRoutingTestUri(string host, int port)
    {
        DiameterUri result = new DiameterUri();
        result.Host = CreateRoutingTestIdentity(host);
        result.Port = port;
        return result;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }
}

internal class RoutingContractProbe : IDiameterRouter
{
    internal DiameterRouteDecision Decision = new DiameterRouteDecision();
    internal List<DiameterRedirectEntry> Redirects =
        new List<DiameterRedirectEntry>();
    internal List<DiameterRouteSnapshot> Snapshots =
        new List<DiameterRouteSnapshot>();
    internal DiameterRouteRequest? ObservedRequest;
    internal DiameterMessage? ObservedRedirectAnswer;
    internal DiameterIdentity? ObservedRedirectPeer;
    internal DateTimeOffset ObservedRedirectAt;

    public DiameterRouteDecision SelectDiameterRoute(
        DiameterRouteRequest? request)
    {
        ObservedRequest = request;
        return Decision;
    }

    public List<DiameterRedirectEntry> ProcessDiameterRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        ObservedRequest = request;
        ObservedRedirectAnswer = answer;
        ObservedRedirectPeer = sourcePeer;
        ObservedRedirectAt = receivedAt;
        return Redirects;
    }

    public List<DiameterRouteSnapshot> ReadDiameterRouteSnapshots()
    {
        return Snapshots;
    }
}

internal class RoutePolicyContractProbe : IRoutePolicy
{
    internal DiameterRouteCandidate Result = new DiameterRouteCandidate();
    internal DiameterRouteRequest? ObservedRequest;
    internal DiameterRouteCandidate? ObservedCandidate;

    public DiameterRouteCandidate EvaluateDiameterRouteCandidate(
        DiameterRouteRequest? request,
        DiameterRouteCandidate? candidate)
    {
        ObservedRequest = request;
        ObservedCandidate = candidate;
        return Result;
    }
}

internal class RoutePathPolicyContractProbe : IRoutePathPolicy
{
    internal DiameterRoutePathAuthorizationResult Result =
        new DiameterRoutePathAuthorizationResult();
    internal DiameterRoutePathAuthorizationRequest? ObservedRequest;

    public DiameterRoutePathAuthorizationResult AuthorizeDiameterRoutePath(
        DiameterRoutePathAuthorizationRequest? request)
    {
        ObservedRequest = request;
        return Result;
    }
}

internal class RedirectHostPolicyContractProbe : IRedirectHostPolicy
{
    internal DiameterRedirectHostAuthorizationResult Result =
        new DiameterRedirectHostAuthorizationResult();
    internal DiameterRedirectHostAuthorizationRequest? ObservedRequest;

    public DiameterRedirectHostAuthorizationResult AuthorizeDiameterRedirectHost(
        DiameterRedirectHostAuthorizationRequest? request)
    {
        ObservedRequest = request;
        return Result;
    }
}

internal class RealmRedirectPolicyContractProbe : IRealmRedirectPolicy
{
    internal DiameterRealmRedirectAuthorizationResult Result =
        new DiameterRealmRedirectAuthorizationResult();
    internal DiameterRealmRedirectAuthorizationRequest? ObservedRequest;

    public DiameterRealmRedirectAuthorizationResult AuthorizeDiameterRealmRedirect(
        DiameterRealmRedirectAuthorizationRequest? request)
    {
        ObservedRequest = request;
        return Result;
    }
}

internal class TranslationContractProbe : ITranslationHandler
{
    internal DiameterTranslationResult RequestResult =
        new DiameterTranslationResult();
    internal DiameterTranslationResult AnswerResult =
        new DiameterTranslationResult();
    internal DiameterRouteRequest? ObservedRequest;
    internal uint TargetApplicationId;
    internal DiameterTranslationResult? ObservedRequestTranslation;
    internal DiameterMessage? ObservedAnswer;

    public DiameterTranslationResult TranslateDiameterRequest(
        DiameterRouteRequest? request,
        uint targetApplicationId)
    {
        ObservedRequest = request;
        TargetApplicationId = targetApplicationId;
        return RequestResult;
    }

    public DiameterTranslationResult TranslateDiameterAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer)
    {
        ObservedRequestTranslation = requestTranslation;
        ObservedAnswer = answer;
        return AnswerResult;
    }
}
