using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class RealmRedirectRoutingTests
{
    [Test]
    public void LocalRealmRedirectRoutePrecedesApplicationDeliveryAndGeneratesAnswer()
    {
        RouteTable table = CreateRouteTable(0L, 0L);
        DiameterRouteConfiguration redirect = CreateRealmRedirectRoute();
        redirect.RedirectRealms.Add(CreateIdentity("first.alternate"));
        redirect.RedirectRealms.Add(CreateIdentity("second.alternate"));
        redirect.RedirectUsage =
            DiameterRedirectHostUsage.RealmAndApplication;
        redirect.RedirectMaximumCacheTimeSeconds = 60U;
        redirect.RedirectInitialSessionRequestsOnly = true;
        table.AddOrReplaceStaticRoute(redirect);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server),
            table);
        DiameterRouteRequest request = CreateRequest("local.example", false);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        RedirectAnswerGenerationResult generated =
            selector.RealmRedirects.GenerateRealmRedirectAnswer(decision);
        RedirectAnswerGenerationResult hostRedirect =
            new RedirectAgent(
                CreateIdentity("node.local.example"),
                CreateIdentity("local.example"))
            .GenerateRedirectAnswer(decision);
        DiameterMessage answer = generated.Answer!;

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action,
                Is.EqualTo(DiameterRouteAction.Redirect));
            Assert.That(decision.AgentRole,
                Is.EqualTo(DiameterAgentRole.Redirect));
            Assert.That(generated.Generated, Is.True);
            Assert.That(hostRedirect.Generated, Is.False);
            Assert.That(hostRedirect.Code,
                Is.EqualTo("redirect-host-route-required"));
            Assert.That(answer.Flags, Is.EqualTo(
                DiameterMessageFlags.Proxyable |
                DiameterMessageFlags.Error));
            Assert.That(answer.HopByHopIdentifier,
                Is.EqualTo(request.Message.HopByHopIdentifier));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.RealmRedirectIndication));
            Assert.That(ReadTextAvps(
                answer,
                DiameterAvpCodes.RedirectRealm), Is.EqualTo(
                new string[] { "first.alternate", "second.alternate" }));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.RedirectHostUsage), Is.EqualTo(3U));
            Assert.That(ReadUnsigned32(
                answer,
                DiameterAvpCodes.RedirectMaxCacheTime), Is.EqualTo(60U));
        });

        request.InitialSessionRequest = false;
        DiameterRouteDecision established = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        Assert.That(established.Action, Is.EqualTo(DiameterRouteAction.Local));
    }

    [Test]
    public void RealmRedirectTriesRealmsInOrderAndRewritesOwnedRequest()
    {
        RouteTable table = CreateRouteTable(0L);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "first-route",
            "first.alternate",
            "first-peer.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "second-route",
            "second.alternate",
            "second-peer.example"));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Client | DiameterNodeRole.Relay),
            table);
        DiameterRouteRequest request = CreateRequest("original.example", true);
        request.Message.Avps.Add(
            new DiameterBaseAvpFactory().CreateDestinationHostAvp(
                request.DestinationHost));
        request.Message.Avps.Add(
            new DiameterBaseAvpFactory().CreateDestinationRealmAvp(
                request.DestinationRealm));
        DiameterAvp retained = new DiameterAvp();
        retained.Code = 999U;
        retained.Data = new byte[] { 7 };
        request.Message.Avps.Add(retained);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("first-peer.example", false));
        peers.Add(CreatePeer("second-peer.example", true));

        RealmRedirectProcessingResult result =
            selector.ProcessRealmRedirectAnswer(
                request,
                CreateRealmRedirectAnswer(
                    request.Message,
                    new string[]
                    {
                        "first.alternate",
                        "second.alternate"
                    },
                    false,
                    0U),
                CreateIdentity("redirector.example"),
                true,
                table.Clock.ReadUtcTime(),
                peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Rerouted, Is.True);
            Assert.That(result.ForwardAnswer, Is.False);
            Assert.That(result.Candidate!.PeerIdentity!.Value,
                Is.EqualTo("second-peer.example"));
            Assert.That(result.Request!.DestinationRealm!.Value,
                Is.EqualTo("second.alternate"));
            Assert.That(result.Request.DestinationHost, Is.Null);
            Assert.That(ReadTextAvps(
                result.Request.Message,
                DiameterAvpCodes.DestinationRealm),
                Is.EqualTo(new string[] { "second.alternate" }));
            Assert.That(CountAvps(
                result.Request.Message,
                DiameterAvpCodes.DestinationHost), Is.Zero);
            Assert.That(CountAvps(result.Request.Message, 999U), Is.EqualTo(1));
            Assert.That(result.Entry, Is.Null);
            Assert.That(request.DestinationRealm!.Value,
                Is.EqualTo("original.example"));
            Assert.That(request.DestinationHost!.Value,
                Is.EqualTo("service.original.example"));
            Assert.That(CountAvps(
                request.Message,
                DiameterAvpCodes.DestinationHost), Is.EqualTo(1));
        });
    }

    [Test]
    public void RealmRedirectCacheReusesDiscoveredPeerAndExpiresWithBaseCache()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RouteTable table = new RouteTable(
            clock,
            new SequenceRouteRandom(new long[] { 0L, 0L }));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "alternate-route",
            "alternate.example",
            "alternate-peer.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "original-route",
            "original.example",
            "original-peer.example"));
        DiameterNodeConfiguration configuration = CreateNodeConfiguration(
            DiameterNodeRole.Client | DiameterNodeRole.Relay);
        configuration.Timers.RedirectCacheMaximumLifetimeMilliseconds = 2000;
        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest request = CreateRequest("original.example", true);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("alternate-peer.example", true));
        peers.Add(CreatePeer("original-peer.example", true));

        RealmRedirectProcessingResult processed =
            selector.ProcessRealmRedirectAnswer(
                request,
                CreateRealmRedirectAnswer(
                    request.Message,
                    new string[] { "alternate.example" },
                    true,
                    30U),
                CreateIdentity("redirector.example"),
                true,
                clock.UtcTime,
                peers);
        DiameterRouteDecision cached = selector.SelectRoute(request, peers);
        clock.AdvanceDiscoveryTime(3);
        DiameterRouteDecision expired = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(processed.Entry!.RealmBased, Is.True);
            Assert.That(processed.Entry.RedirectRealm!.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(processed.Entry.Usage,
                Is.EqualTo(DiameterRedirectHostUsage.RealmAndApplication));
            Assert.That(processed.Entry.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-1)));
            Assert.That(cached.Code, Is.EqualTo("route-redirect-cache"));
            Assert.That(cached.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("alternate-peer.example"));
            Assert.That(cached.Request.DestinationRealm!.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(cached.Request.DestinationHost, Is.Null);
            Assert.That(ReadTextAvps(
                cached.RoutedMessage!,
                DiameterAvpCodes.DestinationRealm),
                Is.EqualTo(new string[] { "alternate.example" }));
            Assert.That(expired.SelectedCandidate!.PeerIdentity!.Value,
                Is.EqualTo("original-peer.example"));
            Assert.That(selector.ReadRedirectEntries(), Is.Empty);
        });
    }

    [Test]
    public void RealmRedirectPolicyCanRequireSecureAuthorizedPaths()
    {
        RouteTable table = CreateRouteTable(0L, 0L);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "alternate-route",
            "alternate.example",
            "alternate-peer.example"));
        SecureRealmRedirectPolicy policy = new SecureRealmRedirectPolicy();
        DiameterNodeConfiguration configuration = CreateNodeConfiguration(
            DiameterNodeRole.Client | DiameterNodeRole.Relay);
        configuration.RealmRedirectPolicy = policy;
        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest request = CreateRequest("original.example", false);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("alternate-peer.example", true));
        DiameterMessage answer = CreateRealmRedirectAnswer(
            request.Message,
            new string[] { "alternate.example" },
            false,
            0U);

        RealmRedirectProcessingResult denied =
            selector.ProcessRealmRedirectAnswer(
                request,
                answer,
                CreateIdentity("REDIRECTOR.EXAMPLE"),
                false,
                table.Clock.ReadUtcTime(),
                peers);
        RealmRedirectProcessingResult accepted =
            selector.ProcessRealmRedirectAnswer(
                request,
                answer,
                CreateIdentity("REDIRECTOR.EXAMPLE"),
                true,
                table.Clock.ReadUtcTime(),
                peers);

        Assert.Multiple(() =>
        {
            Assert.That(denied.Accepted, Is.True);
            Assert.That(denied.Rerouted, Is.False);
            Assert.That(denied.ForwardAnswer, Is.True);
            Assert.That(accepted.Rerouted, Is.True);
            Assert.That(policy.Observed!.RedirectingPeer.Value,
                Is.EqualTo("redirector.example"));
            Assert.That(policy.Observed.RedirectRealm.Value,
                Is.EqualTo("alternate.example"));
            Assert.That(policy.Observed.Candidate.PeerIdentity!.Value,
                Is.EqualTo("alternate-peer.example"));
            Assert.That(policy.Observed.SourceSecure, Is.True);
        });
    }

    [Test]
    public void InvalidRealmRedirectAnswersAreRejected()
    {
        RouteTable table = CreateRouteTable(0L);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Client | DiameterNodeRole.Relay),
            table);
        DiameterRouteRequest request = CreateRequest("original.example", false);
        DiameterMessage missingRealm = CreateRealmRedirectAnswer(
            request.Message,
            new string[0],
            false,
            0U);
        DiameterMessage duplicateRealm = CreateRealmRedirectAnswer(
            request.Message,
            new string[] { "duplicate.example", "DUPLICATE.EXAMPLE." },
            false,
            0U);
        DiameterMessage missingMaximum = CreateRealmRedirectAnswer(
            request.Message,
            new string[] { "alternate.example" },
            true,
            30U);
        RemoveAvps(
            missingMaximum,
            DiameterAvpCodes.RedirectMaxCacheTime);

        RealmRedirectProcessingResult missing = ProcessInvalidAnswer(
            selector,
            request,
            missingRealm,
            table.Clock.ReadUtcTime());
        RealmRedirectProcessingResult duplicate = ProcessInvalidAnswer(
            selector,
            request,
            duplicateRealm,
            table.Clock.ReadUtcTime());
        RealmRedirectProcessingResult maximum = ProcessInvalidAnswer(
            selector,
            request,
            missingMaximum,
            table.Clock.ReadUtcTime());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code,
                Is.EqualTo("realm-redirect-result-invalid"));
            Assert.That(duplicate.Code,
                Is.EqualTo("realm-redirect-realm-invalid"));
            Assert.That(maximum.Code,
                Is.EqualTo("realm-redirect-cache-invalid"));
        });
    }

    [TestCase(false, 0U, false, 0U, true)]
    [TestCase(true, 0U, true, 30U, true)]
    [TestCase(true, 3U, true, 30U, true)]
    [TestCase(true, 0U, false, 0U, false)]
    [TestCase(true, 3U, false, 0U, false)]
    [TestCase(false, 0U, true, 30U, false)]
    [TestCase(true, 3U, true, 0U, false)]
    public void RealmRedirectCacheAvpsFollowRfc7075PairingRules(
        bool includeUsage,
        uint usage,
        bool includeMaximum,
        uint maximum,
        bool accepted)
    {
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Client | DiameterNodeRole.Relay),
            CreateRouteTable(0L));
        DiameterRouteRequest request = CreateRequest(
            "original.example",
            false);
        DiameterMessage answer = CreateRealmRedirectAnswer(
            request.Message,
            new string[] { "alternate.example" },
            false,
            0U);
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        if (includeUsage)
        {
            answer.Avps.Add(factory.CreateRedirectHostUsageAvp(
                (DiameterRedirectHostUsage)usage));
        }

        if (includeMaximum)
        {
            answer.Avps.Add(factory.CreateRedirectMaxCacheTimeAvp(maximum));
        }

        RealmRedirectProcessingResult result =
            selector.RealmRedirects.ParseRealmRedirectAnswer(answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.EqualTo(accepted));
            Assert.That(result.Code, Is.EqualTo(accepted ?
                "realm-redirect-accepted" :
                "realm-redirect-cache-invalid"));
        });
    }

    [Test]
    public void HostAddressedRealmRedirectRequiresExplicitServerPolicy()
    {
        RouteTable table = CreateRouteTable(0L);
        DiameterRouteConfiguration redirect = CreateRealmRedirectRoute();
        redirect.RedirectRealms.Add(CreateIdentity("alternate.example"));
        table.AddOrReplaceStaticRoute(redirect);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server),
            table);
        DiameterRouteRequest request = CreateRequest("local.example", true);
        request.DestinationHost = CreateIdentity("node.local.example");

        DiameterRouteDecision local = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());
        redirect.RedirectRequestsWithDestinationHost = true;
        table.AddOrReplaceStaticRoute(redirect);
        DiameterRouteDecision redirected = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(local.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(redirected.Action,
                Is.EqualTo(DiameterRouteAction.Redirect));
        });
    }

    private static RealmRedirectProcessingResult ProcessInvalidAnswer(
        RouteSelector selector,
        DiameterRouteRequest request,
        DiameterMessage answer,
        DateTimeOffset receivedAt)
    {
        return selector.ProcessRealmRedirectAnswer(
            request,
            answer,
            CreateIdentity("redirector.example"),
            true,
            receivedAt,
            new List<DiameterPeerSnapshot>());
    }

    private static RouteTable CreateRouteTable(params long[] values)
    {
        return new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(values));
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration(
        DiameterNodeRole roles)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = roles;
        result.Transport.EnableListener =
            (roles & DiameterNodeRole.Server) != DiameterNodeRole.None;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterRouteConfiguration CreateRealmRedirectRoute()
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = "realm-redirect";
        result.DestinationRealm.Value = "local.example";
        result.ApplicationId = 4U;
        result.Action = DiameterRouteAction.Redirect;
        result.AgentRole = DiameterAgentRole.Redirect;
        return result;
    }

    private static DiameterRouteConfiguration CreatePeerRoute(
        string identifier,
        string realm,
        string peer)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteRequest CreateRequest(
        string realm,
        bool withHost)
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
        result.DestinationRealm = CreateIdentity(realm);
        if (withHost)
        {
            result.DestinationHost = CreateIdentity(
                "service." + realm);
        }

        result.ApplicationId = 4U;
        result.SessionId = "client.example;1";
        result.UserName = "alice@" + realm;
        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(
        string identity,
        bool available)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = available;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Capabilities.CommonApplications.Add(application);
        return result;
    }

    private static DiameterMessage CreateRealmRedirectAnswer(
        DiameterMessage request,
        string[] realms,
        bool cache,
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
            DiameterResultCodes.RealmRedirectIndication));
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        int index = 0;
        while (index < realms.Length)
        {
            result.Avps.Add(factory.CreateIdentityAvp(
                DiameterAvpCodes.RedirectRealm,
                CreateIdentity(realms[index])));
            index = index + 1;
        }

        if (cache)
        {
            result.Avps.Add(factory.CreateRedirectHostUsageAvp(
                DiameterRedirectHostUsage.RealmAndApplication));
            result.Avps.Add(factory.CreateRedirectMaxCacheTimeAvp(
                maximumSeconds));
        }

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

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
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

    private static List<string> ReadTextAvps(
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
        int result = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                result = result + 1;
            }

            index = index + 1;
        }

        return result;
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

internal class SecureRealmRedirectPolicy : IRealmRedirectPolicy
{
    internal DiameterRealmRedirectAuthorizationRequest? Observed;

    public DiameterRealmRedirectAuthorizationResult AuthorizeDiameterRealmRedirect(
        DiameterRealmRedirectAuthorizationRequest? request)
    {
        Observed = request;
        DiameterRealmRedirectAuthorizationResult result =
            new DiameterRealmRedirectAuthorizationResult();
        result.Authorized = request != null && request.SourceSecure;
        result.Code = result.Authorized ?
            "realm-redirect-authorized" : "realm-redirect-source-insecure";
        result.Message = result.Authorized ?
            "The path is authorized." : "The source path is insecure.";
        return result;
    }
}
