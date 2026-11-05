using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class RedirectRoutingTests
{
    [Test]
    public void RedirectAgentGeneratesCorrelatedOrderedProtocolErrorAnswer()
    {
        DiameterRouteRequest request = CreateRequest();
        request.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        DiameterAvp proxy = new DiameterAvp();
        proxy.Code = DiameterAvpCodes.ProxyInfo;
        proxy.Data = new byte[] { 1, 2, 3 };
        request.Message.Avps.Add(proxy);
        DiameterRouteConfiguration configuration =
            CreateRedirectConfiguration(
                DiameterRedirectHostUsage.RealmAndApplication,
                90U);
        configuration.RedirectHosts.Add(CreateUri(
            "first.redirect.example",
            false));
        configuration.RedirectHosts.Add(CreateUri(
            "second.redirect.example",
            true));
        DiameterRouteCandidate candidate = new DiameterRouteCandidate();
        candidate.Configuration = configuration;
        candidate.Action = DiameterRouteAction.Redirect;
        candidate.AgentRole = DiameterAgentRole.Redirect;
        DiameterRouteDecision decision = new DiameterRouteDecision();
        decision.Request = request;
        decision.SelectedCandidate = candidate;
        decision.Action = DiameterRouteAction.Redirect;
        decision.AgentRole = DiameterAgentRole.Redirect;
        decision.Routed = true;
        RedirectAgent agent = new RedirectAgent(
            CreateIdentity("node.local.example"),
            CreateIdentity("local.example"));

        RedirectAnswerGenerationResult generated =
            agent.GenerateRedirectAnswer(decision);
        DiameterMessage answer = generated.Answer!;

        Assert.Multiple(() =>
        {
            Assert.That(generated.Generated, Is.True);
            Assert.That(generated.Code, Is.EqualTo(
                "redirect-answer-generated"));
            Assert.That(answer.Flags, Is.EqualTo(
                DiameterMessageFlags.Proxyable |
                DiameterMessageFlags.Error));
            Assert.That(answer.CommandCode,
                Is.EqualTo(request.Message.CommandCode));
            Assert.That(answer.ApplicationId,
                Is.EqualTo(request.Message.ApplicationId));
            Assert.That(answer.HopByHopIdentifier,
                Is.EqualTo(request.Message.HopByHopIdentifier));
            Assert.That(answer.EndToEndIdentifier,
                Is.EqualTo(request.Message.EndToEndIdentifier));
            Assert.That(ReadUnsigned32(answer, DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.RedirectIndication));
            Assert.That(ReadTextValues(answer, DiameterAvpCodes.RedirectHost),
                Is.EqualTo(new string[]
                {
                    "aaa://first.redirect.example",
                    "aaas://second.redirect.example"
                }));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.RedirectHostUsage), Is.EqualTo(3U));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.RedirectMaxCacheTime), Is.EqualTo(90U));
            Assert.That(CountAvps(answer, DiameterAvpCodes.ProxyInfo),
                Is.EqualTo(1));
        });

        configuration.RedirectHosts[0].Host.Value = "changed.example";
        Assert.That(ReadTextValues(answer, DiameterAvpCodes.RedirectHost)[0],
            Is.EqualTo("aaa://first.redirect.example"));
    }

    [TestCase(DiameterRedirectHostUsage.DoNotCache)]
    [TestCase(DiameterRedirectHostUsage.AllSession)]
    [TestCase(DiameterRedirectHostUsage.AllRealm)]
    [TestCase(DiameterRedirectHostUsage.RealmAndApplication)]
    [TestCase(DiameterRedirectHostUsage.AllApplication)]
    [TestCase(DiameterRedirectHostUsage.AllHost)]
    [TestCase(DiameterRedirectHostUsage.AllUser)]
    public void EveryRedirectUsageScopeCanSelectAnEligiblePeer(
        DiameterRedirectHostUsage usage)
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 60000, null);
        DiameterRouteRequest request = CreateRequest();
        DiameterMessage answer = CreateRedirectAnswer(
            request.Message,
            CreateUri("target.example", false),
            usage,
            30U);

        RedirectProcessingResult processed = cache.ProcessRedirectAnswer(
            request,
            answer,
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("target.example", true, 4U));
        RedirectRouteSelectionResult selected =
            cache.SelectRedirectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(processed.Accepted, Is.True);
            Assert.That(processed.Entries, Has.Count.EqualTo(1));
            Assert.That(processed.Entries[0].Usage, Is.EqualTo(usage));
            Assert.That(processed.Entries[0].Cached,
                Is.EqualTo(usage != DiameterRedirectHostUsage.DoNotCache));
            Assert.That(selected.SelectedCandidate, Is.Not.Null);
            Assert.That(selected.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("target.example"));
            Assert.That(selected.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.Redirect));
            Assert.That(selected.Entry!.Usage, Is.EqualTo(usage));
        });

        if (usage == DiameterRedirectHostUsage.DoNotCache)
        {
            Assert.That(cache.SelectRedirectRoute(request, peers)
                .SelectedCandidate, Is.Null);
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
        }
    }

    [Test]
    public void RedirectCacheUsesRequiredScopePrecedence()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 60000, null);
        DiameterRouteRequest request = CreateRequest();
        DiameterRedirectHostUsage[] scopes = new DiameterRedirectHostUsage[]
        {
            DiameterRedirectHostUsage.AllHost,
            DiameterRedirectHostUsage.AllApplication,
            DiameterRedirectHostUsage.AllRealm,
            DiameterRedirectHostUsage.RealmAndApplication,
            DiameterRedirectHostUsage.AllUser,
            DiameterRedirectHostUsage.AllSession
        };
        int index = 0;
        while (index < scopes.Length)
        {
            DiameterUri target = CreateUri(
                "target-" + index.ToString() + ".example",
                false);
            RedirectProcessingResult processed = cache.ProcessRedirectAnswer(
                request,
                CreateRedirectAnswer(
                    request.Message,
                    target,
                    scopes[index],
                    30U),
                CreateIdentity("redirector.example"),
                true,
                clock.UtcTime);
            Assert.That(processed.Accepted, Is.True);
            index = index + 1;
        }

        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        index = 0;
        while (index < scopes.Length)
        {
            peers.Add(CreatePeer(
                "target-" + index.ToString() + ".example",
                true,
                4U));
            index = index + 1;
        }

        RedirectRouteSelectionResult selected =
            cache.SelectRedirectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Entry!.Usage,
                Is.EqualTo(DiameterRedirectHostUsage.AllSession));
            Assert.That(selected.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("target-5.example"));
        });
    }

    [Test]
    public void RedirectCacheCapsLifetimeExpiresEntriesAndReturnsOwnedState()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 2000, null);
        DiameterRouteRequest request = CreateRequest();

        RedirectProcessingResult processed = cache.ProcessRedirectAnswer(
            request,
            CreateRedirectAnswer(
                request.Message,
                CreateUri("target.example", false),
                DiameterRedirectHostUsage.AllSession,
                100U),
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);
        List<DiameterRedirectEntry> before = cache.ReadRedirectEntries();
        before[0].Hosts[0].Host.Value = "changed.example";
        List<DiameterRedirectEntry> owned = cache.ReadRedirectEntries();
        clock.AdvanceDiscoveryTime(3);

        Assert.Multiple(() =>
        {
            Assert.That(processed.Entries[0].ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-1)));
            Assert.That(owned[0].Hosts[0].Host.Value,
                Is.EqualTo("target.example"));
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
        });
    }

    [Test]
    public void RedirectCacheExpirySurvivesUtcClockRollback()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 2000, null);
        DiameterRouteRequest request = CreateRequest();
        RedirectProcessingResult processed = cache.ProcessRedirectAnswer(
            request,
            CreateRedirectAnswer(
                request.Message,
                CreateUri("target.example", false),
                DiameterRedirectHostUsage.AllSession,
                30U),
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);

        clock.UtcTime = clock.UtcTime.AddHours(-1);
        clock.MonotonicMilliseconds =
            clock.MonotonicMilliseconds + 3000L;

        Assert.Multiple(() =>
        {
            Assert.That(processed.Accepted, Is.True);
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
            Assert.That(cache.EntryMonotonicExpirations, Is.Empty);
        });
    }

    [Test]
    public void RedirectSecurityPolicyAndSecureTargetAreEnforced()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        SecureRedirectPolicy policy = new SecureRedirectPolicy();
        RedirectRouteCache cache = CreateCache(clock, 60000, policy);
        DiameterRouteRequest request = CreateRequest();
        DiameterMessage answer = CreateRedirectAnswer(
            request.Message,
            CreateUri("secure-target.example", true),
            DiameterRedirectHostUsage.AllSession,
            30U);

        RedirectProcessingResult denied = cache.ProcessRedirectAnswer(
            request,
            answer,
            CreateIdentity("redirector.example"),
            false,
            clock.UtcTime);
        RedirectProcessingResult accepted = cache.ProcessRedirectAnswer(
            request,
            answer,
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);
        List<DiameterPeerSnapshot> insecurePeers =
            new List<DiameterPeerSnapshot>();
        insecurePeers.Add(CreatePeer("secure-target.example", true, 4U));
        RedirectRouteSelectionResult insecure =
            cache.SelectRedirectRoute(request, insecurePeers);

        Assert.Multiple(() =>
        {
            Assert.That(denied.Accepted, Is.False);
            Assert.That(denied.Code, Is.EqualTo("redirect-source-insecure"));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(policy.Observed!.RedirectingPeer.Value,
                Is.EqualTo("redirector.example"));
            Assert.That(policy.Observed.SourceSecure, Is.True);
            Assert.That(policy.Observed.RedirectHost.Host.Value,
                Is.EqualTo("secure-target.example"));
            Assert.That(insecure.SelectedCandidate, Is.Null);
            Assert.That(insecure.Candidates[0].Code,
                Is.EqualTo("redirect-security-required"));
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
        });
    }

    [TestCase("node.local.example")]
    [TestCase("redirector.example")]
    [TestCase("ingress.example")]
    [TestCase("traversed.example")]
    public void RedirectLoopsAreRejectedBeforeCaching(string target)
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 60000, null);
        DiameterRouteRequest request = CreateRequest();
        request.ExcludedPeers.Add(CreateIdentity("traversed.example"));

        RedirectProcessingResult result = cache.ProcessRedirectAnswer(
            request,
            CreateRedirectAnswer(
                request.Message,
                CreateUri(target, false),
                DiameterRedirectHostUsage.AllSession,
                30U),
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code, Is.EqualTo("redirect-loop-detected"));
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
        });
    }

    [Test]
    public void RouteSelectorUsesCachedRedirectBeforeStaticRouting()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RouteTable table = new RouteTable(
            clock,
            new SequenceRouteRandom(new long[] { 0L }));
        DiameterRouteConfiguration route = new DiameterRouteConfiguration();
        route.RouteIdentifier = "static-route";
        route.DestinationRealm.Value = "remote.example";
        route.ApplicationId = 4U;
        route.PeerIdentity = CreateIdentity("static-peer.example");
        route.Action = DiameterRouteAction.Relay;
        route.AgentRole = DiameterAgentRole.Relay;
        table.AddOrReplaceStaticRoute(route);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(),
            table);
        DiameterRouteRequest request = CreateRequest();
        RedirectProcessingResult processed = selector.ProcessRedirectAnswer(
            request,
            CreateRedirectAnswer(
                request.Message,
                CreateUri("redirect-target.example", false),
                DiameterRedirectHostUsage.AllRealm,
                30U),
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("redirect-target.example", true, 4U));
        peers.Add(CreatePeer("static-peer.example", true, 4U));

        DiameterRouteDecision decision = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(processed.Accepted, Is.True);
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.Code, Is.EqualTo("route-redirect-cache"));
            Assert.That(decision.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("redirect-target.example"));
            Assert.That(decision.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.Redirect));
            Assert.That(decision.RedirectEntries, Has.Count.EqualTo(1));
            Assert.That(table.ReadStaticRouteSnapshot("static-route")!
                .SelectionCount, Is.Zero);
        });
    }

    [Test]
    public void InvalidRedirectAnswersAreNotAccepted()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RedirectRouteCache cache = CreateCache(clock, 60000, null);
        DiameterRouteRequest request = CreateRequest();
        DiameterMessage missingLifetime = CreateRedirectAnswer(
            request.Message,
            CreateUri("target.example", false),
            DiameterRedirectHostUsage.AllSession,
            30U);
        RemoveAvps(
            missingLifetime,
            DiameterAvpCodes.RedirectMaxCacheTime);
        DiameterMessage badCorrelation = CreateRedirectAnswer(
            request.Message,
            CreateUri("target.example", false),
            DiameterRedirectHostUsage.AllSession,
            30U);
        badCorrelation.HopByHopIdentifier = 999U;

        RedirectProcessingResult lifetime = cache.ProcessRedirectAnswer(
            request,
            missingLifetime,
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);
        RedirectProcessingResult correlation = cache.ProcessRedirectAnswer(
            request,
            badCorrelation,
            CreateIdentity("redirector.example"),
            true,
            clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(lifetime.Code,
                Is.EqualTo("redirect-cache-time-required"));
            Assert.That(correlation.Code,
                Is.EqualTo("redirect-answer-correlation-invalid"));
            Assert.That(cache.ReadRedirectEntries(), Is.Empty);
        });
    }

    private static RedirectRouteCache CreateCache(
        FakeDiscoveryClock clock,
        int maximumLifetimeMilliseconds,
        IRedirectHostPolicy? policy)
    {
        return new RedirectRouteCache(
            CreateIdentity("node.local.example"),
            clock,
            maximumLifetimeMilliseconds,
            16,
            policy);
    }

    private static DiameterRouteRequest CreateRequest()
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.Message.HopByHopIdentifier = 101U;
        result.Message.EndToEndIdentifier = 202U;
        result.SourcePeer = CreateIdentity("ingress.example");
        result.OriginHost = CreateIdentity("client.example");
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost = CreateIdentity("service.remote.example");
        result.ApplicationId = 4U;
        result.SessionId = "client.example;1";
        result.UserName = "alice@remote.example";
        return result;
    }

    private static DiameterMessage CreateRedirectAnswer(
        DiameterMessage request,
        DiameterUri target,
        DiameterRedirectHostUsage usage,
        uint maximumSeconds)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = (request.Flags & DiameterMessageFlags.Proxyable) |
            DiameterMessageFlags.Error;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        result.Avps.Add(CreateUnsigned32Avp(
            DiameterAvpCodes.ResultCode,
            DiameterResultCodes.RedirectIndication));
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        result.Avps.Add(factory.CreateRedirectHostAvp(target));
        result.Avps.Add(factory.CreateRedirectHostUsageAvp(usage));
        if (usage != DiameterRedirectHostUsage.DoNotCache)
        {
            result.Avps.Add(factory.CreateRedirectMaxCacheTimeAvp(
                maximumSeconds));
        }

        return result;
    }

    private static DiameterRouteConfiguration CreateRedirectConfiguration(
        DiameterRedirectHostUsage usage,
        uint maximumSeconds)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = "redirect-route";
        result.DestinationRealm.Value = "remote.example";
        result.ApplicationId = 4U;
        result.Action = DiameterRouteAction.Redirect;
        result.AgentRole = DiameterAgentRole.Redirect;
        result.RedirectUsage = usage;
        result.RedirectMaximumCacheTimeSeconds = maximumSeconds;
        return result;
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration()
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Client | DiameterNodeRole.Relay;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(
        string identity,
        bool available,
        uint applicationId)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = available;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        result.Capabilities.CommonApplications.Add(application);
        return result;
    }

    private static DiameterUri CreateUri(string host, bool secure)
    {
        DiameterUri result = new DiameterUri();
        result.Host = CreateIdentity(host);
        if (secure)
        {
            result.Scheme = "aaas";
            result.Port = DiameterProtocolConstants.SecurePort;
        }

        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DiameterAvp CreateUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(result.Data, value);
        return result;
    }

    private static uint ReadUnsigned32(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return NetworkByteOrder.ReadUInt32(message.Avps[index].Data);
            }

            index = index + 1;
        }

        throw new AssertionException("Expected Unsigned32 AVP was absent.");
    }

    private static List<string> ReadTextValues(
        DiameterMessage message,
        uint code)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                result.Add(Encoding.UTF8.GetString(
                    message.Avps[index].Data));
            }

            index = index + 1;
        }

        return result;
    }

    private static int CountAvps(DiameterMessage message, uint code)
    {
        return ReadTextValues(message, code).Count;
    }

    private static void RemoveAvps(DiameterMessage message, uint code)
    {
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            if (message.Avps[index].Code == code)
            {
                message.Avps.RemoveAt(index);
            }

            index = index - 1;
        }
    }
}

internal class SecureRedirectPolicy : IRedirectHostPolicy
{
    internal DiameterRedirectHostAuthorizationRequest? Observed;

    public DiameterRedirectHostAuthorizationResult AuthorizeDiameterRedirectHost(
        DiameterRedirectHostAuthorizationRequest? request)
    {
        Observed = request;
        DiameterRedirectHostAuthorizationResult result =
            new DiameterRedirectHostAuthorizationResult();
        result.Authorized = request != null && request.SourceSecure;
        result.Code = result.Authorized ?
            "redirect-authorized" : "redirect-source-insecure";
        result.Message = result.Authorized ?
            "The redirect is authorized." :
            "The redirect source must be secure.";
        return result;
    }
}
