using System.Text;

namespace diameter.Tests;

public class UserNameRealmRoutingTests
{
    [Test]
    public void PlainUserNameDerivesMissingDestinationRealm()
    {
        RouteTable table = CreateRealmRouteTable(0);
        table.AddOrReplaceStaticRoute(CreateRealmPeerRoute(
            "home",
            "home.example",
            "home-peer.example",
            4U));
        RouteSelector selector = new RouteSelector(
            CreateRealmNode("local.example", 4U),
            table);
        DiameterRouteRequest request = CreateRealmRequest(
            string.Empty,
            "alice@home.example",
            4U);
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            request.UserName));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreateRealmPeer("home-peer.example", 4U));

        DiameterRouteDecision decision = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(request.DestinationRealm, Is.Null);
            Assert.That(decision.Routed, Is.True);
            Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("home"));
            Assert.That(decision.Request.DestinationRealm!.Value,
                Is.EqualTo("home.example"));
            Assert.That(ReadTextAvp(
                decision.RoutedMessage!,
                DiameterAvpCodes.DestinationRealm),
                Is.EqualTo("home.example"));
            Assert.That(ReadTextAvp(
                decision.RoutedMessage!,
                DiameterAvpCodes.UserName),
                Is.EqualTo("alice@home.example"));
        });
    }

    [Test]
    public void DecoratedUserNameAdvancesBeforeLocalApplicationDelivery()
    {
        RouteTable table = CreateRealmRouteTable(0);
        table.AddOrReplaceStaticRoute(CreateRealmPeerRoute(
            "next-realm",
            "x.example.com",
            "x-peer.example.com",
            4U));
        RouteSelector selector = new RouteSelector(
            CreateRealmNode("z.example.com", 4U),
            table);
        DiameterRouteRequest request = CreateRealmRequest(
            "z.example.com",
            "x.example.com!h.example.com!alice@z.example.com",
            4U);
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            request.UserName));
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            request.DestinationRealm!.Value));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreateRealmPeer("x-peer.example.com", 4U));

        DiameterRouteDecision decision = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(request.UserName,
                Is.EqualTo("x.example.com!h.example.com!alice@z.example.com"));
            Assert.That(request.DestinationRealm!.Value,
                Is.EqualTo("z.example.com"));
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Relay));
            Assert.That(decision.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("next-realm"));
            Assert.That(decision.Request.UserName,
                Is.EqualTo("h.example.com!alice@x.example.com"));
            Assert.That(decision.Request.DestinationRealm!.Value,
                Is.EqualTo("x.example.com"));
            Assert.That(ReadTextAvp(
                decision.RoutedMessage!,
                DiameterAvpCodes.UserName),
                Is.EqualTo("h.example.com!alice@x.example.com"));
            Assert.That(ReadTextAvp(
                decision.RoutedMessage!,
                DiameterAvpCodes.DestinationRealm),
                Is.EqualTo("x.example.com"));
        });
    }

    [Test]
    public void MultiHopDecoratedNaiAdvancesOneRealmPerRoutingPass()
    {
        RouteTable table = CreateRealmRouteTable(0, 0);
        table.AddOrReplaceStaticRoute(CreateRealmPeerRoute(
            "first-hop",
            "first.example",
            "first-peer.example",
            4U));
        table.AddOrReplaceStaticRoute(CreateRealmPeerRoute(
            "second-hop",
            "second.example",
            "second-peer.example",
            4U));
        DiameterNodeConfiguration configuration =
            CreateRealmNode("origin.example", 4U);
        configuration.RealmRouting.LocalRealms.Add(
            CreateRealmIdentity("first.example"));
        RouteSelector selector = new RouteSelector(configuration, table);
        DiameterRouteRequest request = CreateRealmRequest(
            "origin.example",
            "first.example!second.example!alice@origin.example",
            4U);
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            request.UserName));
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            request.DestinationRealm!.Value));
        List<DiameterPeerSnapshot> peers =
            new List<DiameterPeerSnapshot>();
        peers.Add(CreateRealmPeer("first-peer.example", 4U));
        peers.Add(CreateRealmPeer("second-peer.example", 4U));

        DiameterRouteDecision first = selector.SelectRoute(request, peers);
        DiameterRouteDecision second = selector.SelectRoute(
            first.Request,
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(first.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("first-hop"));
            Assert.That(first.Request.UserName,
                Is.EqualTo("second.example!alice@first.example"));
            Assert.That(first.Request.DestinationRealm!.Value,
                Is.EqualTo("first.example"));
            Assert.That(second.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("second-hop"));
            Assert.That(second.Request.UserName,
                Is.EqualTo("alice@second.example"));
            Assert.That(second.Request.DestinationRealm!.Value,
                Is.EqualTo("second.example"));
            Assert.That(ReadTextAvp(
                second.RoutedMessage!,
                DiameterAvpCodes.UserName),
                Is.EqualTo("alice@second.example"));
            Assert.That(request.UserName,
                Is.EqualTo(
                    "first.example!second.example!alice@origin.example"));
        });
    }

    [Test]
    public void FinalNonDecoratedHomeRealmUsesLocalApplication()
    {
        RouteSelector selector = new RouteSelector(
            CreateRealmNode("home.example", 4U),
            CreateRealmRouteTable(0));
        DiameterRouteRequest request = CreateRealmRequest(
            "HOME.EXAMPLE",
            "alice@home.example",
            4U);
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            request.UserName));

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(decision.Code, Is.EqualTo("route-local"));
            Assert.That(decision.Request.UserName,
                Is.EqualTo("alice@home.example"));
            Assert.That(decision.Request.DestinationRealm!.Value,
                Is.EqualTo("HOME.EXAMPLE"));
        });
    }

    [Test]
    public void AdditionalTransitAndHomeRealmsFollowConfiguredPolicy()
    {
        RouteTable table = CreateRealmRouteTable(0, 0);
        table.AddOrReplaceStaticRoute(CreateRealmPeerRoute(
            "next",
            "next.example",
            "next-peer.example",
            4U));
        table.AddOrReplaceStaticRoute(CreateDefaultRealmPeerRoute(
            "default",
            "default-peer.example"));
        DiameterNodeConfiguration configuration =
            CreateRealmNode("origin.example", 4U);
        configuration.RealmRouting.OriginRealmIsHome = false;
        configuration.RealmRouting.LocalRealms.Add(
            CreateRealmIdentity("transit.example"));
        configuration.RealmRouting.HomeRealms.Add(
            CreateRealmIdentity("home.example"));
        RouteSelector selector = new RouteSelector(configuration, table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreateRealmPeer("next-peer.example", 4U));
        peers.Add(CreateRealmPeer("default-peer.example", 4U));

        DiameterRouteDecision transit = selector.SelectRoute(
            CreateRealmRequest(
                "transit.example",
                "next.example!alice@transit.example",
                4U),
            peers);
        DiameterRouteDecision home = selector.SelectRoute(
            CreateRealmRequest(
                "home.example",
                "alice@home.example",
                4U),
            peers);
        DiameterRouteDecision origin = selector.SelectRoute(
            CreateRealmRequest(
                "origin.example",
                "alice@origin.example",
                4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(transit.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("next"));
            Assert.That(transit.Request.UserName,
                Is.EqualTo("alice@next.example"));
            Assert.That(ReadTextAvp(
                transit.RoutedMessage!,
                DiameterAvpCodes.UserName),
                Is.EqualTo("alice@next.example"));
            Assert.That(home.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(origin.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("default"));
            Assert.That(origin.Action, Is.EqualTo(DiameterRouteAction.Relay));
        });
    }

    [Test]
    public void DisabledUserNameRoutingLeavesDecoratedNameUnchanged()
    {
        DiameterNodeConfiguration configuration =
            CreateRealmNode("local.example", 4U);
        configuration.RealmRouting.EnableUserNameRouting = false;
        RouteSelector selector = new RouteSelector(
            configuration,
            CreateRealmRouteTable(0));
        DiameterRouteRequest request = CreateRealmRequest(
            "local.example",
            "next.example!alice@local.example",
            4U);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(decision.Request.UserName,
                Is.EqualTo("next.example!alice@local.example"));
            Assert.That(decision.Request.DestinationRealm!.Value,
                Is.EqualTo("local.example"));
        });
    }

    [TestCase("!home.example!alice@local.example")]
    [TestCase("next.example!!alice@local.example")]
    [TestCase("next.example!alice@other.example")]
    [TestCase("next.example!alice@local.example@other.example")]
    public void MalformedDecoratedUserNamesAreRejected(string userName)
    {
        RouteSelector selector = new RouteSelector(
            CreateRealmNode("local.example", 4U),
            CreateRealmRouteTable(0));

        DiameterRouteDecision decision = selector.SelectRoute(
            CreateRealmRequest("local.example", userName, 4U),
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.NoRoute));
            Assert.That(decision.Code,
                Does.StartWith("decorated-").Or.StartWith("user-name-"));
        });
    }

    [Test]
    public void PermissiveMalformedPolicyContinuesNormalRealmProcessing()
    {
        DiameterNodeConfiguration configuration =
            CreateRealmNode("local.example", 4U);
        configuration.RealmRouting.RejectMalformedUserNames = false;
        RouteSelector selector = new RouteSelector(
            configuration,
            CreateRealmRouteTable(0));
        DiameterRouteRequest request = CreateRealmRequest(
            "local.example",
            "next.example!alice@other.example",
            4U);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Action, Is.EqualTo(DiameterRouteAction.Local));
            Assert.That(decision.Request.UserName,
                Is.EqualTo("next.example!alice@other.example"));
            Assert.That(decision.Request.DestinationRealm!.Value,
                Is.EqualTo("local.example"));
        });
    }

    [Test]
    public void DuplicateOrInvalidUserNameAvpsAreRejected()
    {
        RouteSelector selector = new RouteSelector(
            CreateRealmNode("local.example", 4U),
            CreateRealmRouteTable(0));
        DiameterRouteRequest duplicate = CreateRealmRequest(
            "local.example",
            string.Empty,
            4U);
        duplicate.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            "alice@local.example"));
        duplicate.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.UserName,
            "alice@local.example"));
        DiameterRouteRequest invalid = CreateRealmRequest(
            "local.example",
            string.Empty,
            4U);
        DiameterAvp invalidAvp = new DiameterAvp();
        invalidAvp.Code = DiameterAvpCodes.UserName;
        invalidAvp.Data = new byte[] { 195, 40 };
        invalid.Message.Avps.Add(invalidAvp);

        DiameterRouteDecision duplicateResult = selector.SelectRoute(
            duplicate,
            new List<DiameterPeerSnapshot>());
        DiameterRouteDecision invalidResult = selector.SelectRoute(
            invalid,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(duplicateResult.Code,
                Is.EqualTo("user-name-avp-duplicate"));
            Assert.That(invalidResult.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(invalidResult.Code,
                Is.EqualTo("user-name-avp-invalid"));
        });
    }

    private static RouteTable CreateRealmRouteTable(params long[] values)
    {
        return new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(values));
    }

    private static DiameterNodeConfiguration CreateRealmNode(
        string realm,
        params uint[] applicationIds)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node." + realm;
        result.OriginRealm.Value = realm;
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Server | DiameterNodeRole.Relay;
        result.Transport.EnableListener = true;
        result.Security.AllowUnsecuredTransport = true;
        int index = 0;
        while (index < applicationIds.Length)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = applicationIds[index];
            result.Applications.Add(application);
            index = index + 1;
        }

        return result;
    }

    private static DiameterRouteConfiguration CreateRealmPeerRoute(
        string identifier,
        string realm,
        string peer,
        uint applicationId)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = applicationId;
        result.PeerIdentity = CreateRealmIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteConfiguration CreateDefaultRealmPeerRoute(
        string identifier,
        string peer)
    {
        DiameterRouteConfiguration result = CreateRealmPeerRoute(
            identifier,
            "temporary.example",
            peer,
            0U);
        result.DestinationRealm.Value = string.Empty;
        result.AnyApplication = true;
        result.DefaultRoute = true;
        return result;
    }

    private static DiameterRouteRequest CreateRealmRequest(
        string realm,
        string userName,
        uint applicationId)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = applicationId;
        result.ApplicationId = applicationId;
        if (realm.Length > 0)
        {
            result.DestinationRealm = CreateRealmIdentity(realm);
        }

        result.UserName = userName;
        return result;
    }

    private static DiameterPeerSnapshot CreateRealmPeer(
        string identity,
        params uint[] applicationIds)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateRealmIdentity(identity);
        result.Available = true;
        int index = 0;
        while (index < applicationIds.Length)
        {
            DiameterApplicationId application = new DiameterApplicationId();
            application.Id = applicationIds[index];
            result.Capabilities.CommonApplications.Add(application);
            index = index + 1;
        }

        return result;
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static string ReadTextAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return Encoding.UTF8.GetString(message.Avps[index].Data);
            }

            index = index + 1;
        }

        return string.Empty;
    }

    private static DiameterIdentity CreateRealmIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}
