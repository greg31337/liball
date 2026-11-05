namespace diameter.Tests;

public class RouteTableTests
{
    [Test]
    public void RoutesAreOwnedOrderedReplaceableAndRemovable()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RouteTable table = new RouteTable(clock, new SequenceRouteRandom(0));
        DiameterRouteConfiguration first = CreatePeerRoute(
            "route-one",
            "example.net",
            "peer-one.example.net",
            4U,
            10,
            1);

        DiameterRouteSnapshot added = table.AddOrReplaceStaticRoute(first);
        first.DestinationRealm.Value = "mutated.example";
        first.PeerIdentity!.Value = "mutated.example";
        added.Configuration.RouteIdentifier = "mutated-result";
        clock.AdvanceDiscoveryTime(5);
        DiameterRouteConfiguration replacement = CreatePeerRoute(
            "ROUTE-ONE",
            "replacement.example",
            "peer-two.example.net",
            4U,
            5,
            2);
        DiameterRouteSnapshot replaced =
            table.AddOrReplaceStaticRoute(replacement);
        DiameterRouteConfiguration second = CreateLocalRoute(
            "route-two",
            "example.net",
            4U);
        table.AddOrReplaceStaticRoute(second);

        DiameterRouteSnapshot? stored = table.ReadStaticRouteSnapshot("route-one");
        List<DiameterRouteSnapshot> all = table.ReadAllStaticRouteSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Configuration.RouteIdentifier,
                Is.EqualTo("ROUTE-ONE"));
            Assert.That(stored.Configuration.DestinationRealm.Value,
                Is.EqualTo("replacement.example"));
            Assert.That(stored.PeerIdentity!.Value,
                Is.EqualTo("peer-two.example.net"));
            Assert.That(replaced.CreatedAt, Is.EqualTo(added.CreatedAt));
            Assert.That(replaced.UpdatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(all, Has.Count.EqualTo(2));
            Assert.That(all[0].Configuration.RouteIdentifier,
                Is.EqualTo("ROUTE-ONE"));
            Assert.That(all[1].Configuration.RouteIdentifier,
                Is.EqualTo("route-two"));
        });

        all[0].Configuration.DestinationRealm.Value = "changed.example";
        Assert.That(table.ReadStaticRouteSnapshot("route-one")!
            .Configuration.DestinationRealm.Value,
            Is.EqualTo("replacement.example"));
        Assert.That(table.RemoveStaticRoute("Route-Two"), Is.True);
        Assert.That(table.RemoveStaticRoute("route-two"), Is.False);
        Assert.That(table.ReadStaticRouteSnapshot("route-two"), Is.Null);
    }

    [Test]
    public void SelectionUsesHostThenRealmThenDefaultPrecedence()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0, 0, 0, 0));
        DiameterRouteConfiguration host = CreatePeerRoute(
            "host",
            "example.net",
            "host-peer.example.net",
            4U,
            20,
            1);
        host.DestinationHost = CreateIdentity("service.example.net");
        table.AddOrReplaceStaticRoute(host);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "realm",
            "example.net",
            "realm-peer.example.net",
            4U,
            0,
            1));
        table.AddOrReplaceStaticRoute(CreateDefaultPeerRoute(
            "default",
            "default-peer.example.net",
            1U));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("host-peer.example.net", true, 4U));
        peers.Add(CreatePeer("realm-peer.example.net", true, 4U));
        peers.Add(CreatePeer("default-peer.example.net", true, 4U, 5U));

        DiameterRouteRequest request = CreateRequest(
            "example.net",
            "service.example.net",
            4U);
        RouteSelectionResult hostResult = table.SelectStaticRoute(request, peers);
        request.DestinationHost = null;
        RouteSelectionResult realmResult = table.SelectStaticRoute(request, peers);
        request.DestinationRealm = CreateIdentity("elsewhere.example");
        RouteSelectionResult defaultResult = table.SelectStaticRoute(request, peers);
        request.ApplicationId = 5U;
        request.Message.ApplicationId = 5U;
        RouteSelectionResult anyApplicationResult =
            table.SelectStaticRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(hostResult.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("host"));
            Assert.That(realmResult.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("realm"));
            Assert.That(defaultResult.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("default"));
            Assert.That(defaultResult.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.Default));
            Assert.That(anyApplicationResult.SelectedCandidate!.Configuration
                .RouteIdentifier, Is.EqualTo("default"));
        });
    }

    [Test]
    public void SelectionUsesLowestEligiblePriorityAndConfiguredWeights()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(1));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "unavailable-low",
            "example.net",
            "down.example.net",
            4U,
            0,
            10));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "weight-one",
            "example.net",
            "one.example.net",
            4U,
            5,
            1));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "weight-three",
            "example.net",
            "three.example.net",
            4U,
            5,
            3));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "later-priority",
            "example.net",
            "later.example.net",
            4U,
            6,
            100));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("down.example.net", false, 4U));
        peers.Add(CreatePeer("one.example.net", true, 4U));
        peers.Add(CreatePeer("three.example.net", true, 4U));
        peers.Add(CreatePeer("later.example.net", true, 4U));

        RouteSelectionResult result = table.SelectStaticRoute(
            CreateRequest("example.net", string.Empty, 4U),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("route-selected"));
            Assert.That(result.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("weight-three"));
            Assert.That(result.Candidates[0].Code,
                Is.EqualTo("route-peer-unhealthy"));
            Assert.That(result.Candidates[1].Priority, Is.EqualTo(5));
            Assert.That(result.Candidates[2].Weight, Is.EqualTo(3));
        });
    }

    [Test]
    public void PeerCapabilityHealthLoopAndSecurityFiltersFallBackSafely()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0));
        DiameterRouteConfiguration insecure = CreatePeerRoute(
            "insecure",
            "example.net",
            "insecure.example.net",
            4U,
            0,
            1);
        insecure.RequireSecurePeer = true;
        table.AddOrReplaceStaticRoute(insecure);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "down",
            "example.net",
            "down.example.net",
            4U,
            0,
            1));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "unsupported",
            "example.net",
            "unsupported.example.net",
            4U,
            0,
            1));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "excluded",
            "example.net",
            "excluded.example.net",
            4U,
            0,
            1));
        table.AddOrReplaceStaticRoute(CreateDefaultLocalRoute("fallback"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("insecure.example.net", true, 4U));
        peers.Add(CreatePeer("down.example.net", false, 4U));
        peers.Add(CreatePeer("unsupported.example.net", true, 9U));
        peers.Add(CreatePeer("excluded.example.net", true, 4U));
        DiameterRouteRequest request = CreateRequest(
            "example.net",
            string.Empty,
            4U);
        request.ExcludedPeers.Add(CreateIdentity("EXCLUDED.EXAMPLE.NET"));

        RouteSelectionResult result = table.SelectStaticRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("fallback"));
            Assert.That(result.Candidates, Has.Count.EqualTo(5));
            Assert.That(result.Candidates[0].Code,
                Is.EqualTo("route-security-required"));
            Assert.That(result.Candidates[1].Code,
                Is.EqualTo("route-peer-unhealthy"));
            Assert.That(result.Candidates[2].Code,
                Is.EqualTo("route-application-unsupported"));
            Assert.That(result.Candidates[3].Code,
                Is.EqualTo("route-peer-excluded"));
            Assert.That(result.Candidates[4].Code,
                Is.EqualTo("route-eligible"));
        });
    }

    [Test]
    public void RefreshSelectionAndFailureMaintainOwnedOperationalSnapshots()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RouteTable table = new RouteTable(clock, new SequenceRouteRandom(0));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "route",
            "example.net",
            "peer.example.net",
            4U,
            0,
            1));
        DiameterRouteSnapshot initial = table.ReadStaticRouteSnapshot("route")!;
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        DiameterPeerSnapshot peer = CreatePeer("peer.example.net", true, 4U);
        peer.CurrentEndpoint = new DiameterEndpoint();
        peer.CurrentEndpoint.Security = DiameterSecurityMode.Tls;
        peers.Add(peer);
        clock.AdvanceDiscoveryTime(2);
        List<DiameterRouteSnapshot> refreshed =
            table.RefreshStaticRouteSnapshots(peers);
        RouteSelectionResult selected = table.SelectStaticRoute(
            CreateRequest("example.net", string.Empty, 4U),
            peers);
        clock.AdvanceDiscoveryTime(3);
        DiameterRouteSnapshot failed = table.RecordStaticRouteFailure("route");

        Assert.Multiple(() =>
        {
            Assert.That(initial.Available, Is.False);
            Assert.That(initial.Code, Is.EqualTo("route-awaiting-peer-state"));
            Assert.That(refreshed[0].Available, Is.True);
            Assert.That(refreshed[0].Healthy, Is.True);
            Assert.That(refreshed[0].ApplicationSupported, Is.True);
            Assert.That(refreshed[0].Code, Is.EqualTo("route-available"));
            Assert.That(selected.SelectedCandidate!.Peer,
                Is.Not.SameAs(peer));
            Assert.That(failed.SelectionCount, Is.EqualTo(1));
            Assert.That(failed.FailureCount, Is.EqualTo(1));
            Assert.That(failed.LastSelectedAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-3)));
            Assert.That(failed.UpdatedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(failed.Code, Is.EqualTo("route-delivery-failed"));
        });

        refreshed[0].Configuration.PeerIdentity!.Value = "changed.example";
        selected.SelectedCandidate!.Peer!.Configuration.Identity.Value =
            "changed-peer.example";
        DiameterRouteSnapshot stored = table.ReadStaticRouteSnapshot("route")!;
        Assert.That(stored.Configuration.PeerIdentity!.Value,
            Is.EqualTo("peer.example.net"));
    }

    [Test]
    public void ConcurrentReplacementAndReadsKeepCompleteSnapshots()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0));
        table.AddOrReplaceStaticRoute(CreateLocalRoute(
            "route",
            "example.net",
            4U));

        Parallel.For(0, 100, index =>
        {
            DiameterRouteConfiguration route = CreateLocalRoute(
                "route",
                index % 2 == 0 ? "example.net" : "other.example",
                4U);
            route.Priority = index;
            table.AddOrReplaceStaticRoute(route);
            DiameterRouteSnapshot snapshot =
                table.ReadStaticRouteSnapshot("ROUTE")!;
            Assert.That(snapshot.Configuration.DestinationRealm.Value,
                Is.Not.Empty);
            Assert.That(snapshot.Configuration.Priority, Is.GreaterThanOrEqualTo(0));
        });

        Assert.That(table.ReadAllStaticRouteSnapshots(), Has.Count.EqualTo(1));
    }

    private static DiameterRouteConfiguration CreatePeerRoute(
        string identifier,
        string realm,
        string peer,
        uint applicationId,
        int priority,
        int weight)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = applicationId;
        result.PeerIdentity = CreateIdentity(peer);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        result.Priority = priority;
        result.Weight = weight;
        return result;
    }

    private static DiameterRouteConfiguration CreateDefaultPeerRoute(
        string identifier,
        string peer,
        uint applicationId)
    {
        DiameterRouteConfiguration result = CreatePeerRoute(
            identifier,
            "temporary.example",
            peer,
            applicationId,
            0,
            1);
        result.DestinationRealm.Value = string.Empty;
        result.AnyApplication = true;
        result.DefaultRoute = true;
        return result;
    }

    private static DiameterRouteConfiguration CreateLocalRoute(
        string identifier,
        string realm,
        uint applicationId)
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = realm;
        result.ApplicationId = applicationId;
        result.Action = DiameterRouteAction.Local;
        result.AgentRole = DiameterAgentRole.None;
        return result;
    }

    private static DiameterRouteConfiguration CreateDefaultLocalRoute(
        string identifier)
    {
        DiameterRouteConfiguration result = CreateLocalRoute(
            identifier,
            "temporary.example",
            0U);
        result.DestinationRealm.Value = string.Empty;
        result.AnyApplication = true;
        result.DefaultRoute = true;
        return result;
    }

    private static DiameterRouteRequest CreateRequest(
        string realm,
        string host,
        uint applicationId)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = applicationId;
        result.ApplicationId = applicationId;
        result.DestinationRealm = CreateIdentity(realm);
        if (host.Length > 0)
        {
            result.DestinationHost = CreateIdentity(host);
        }

        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(
        string identity,
        bool available,
        params uint[] applicationIds)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = available;
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

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class SequenceRouteRandom : IRouteRandom
{
    internal List<long> Values = new List<long>();
    internal int Index;

    internal SequenceRouteRandom(params long[] values)
    {
        Values.AddRange(values);
    }

    public long ReadRouteRandomValue(long maximumExclusive)
    {
        if (Index >= Values.Count)
        {
            throw new InvalidOperationException("No route random value remains.");
        }

        long result = Values[Index];
        Index = Index + 1;
        return result;
    }
}
