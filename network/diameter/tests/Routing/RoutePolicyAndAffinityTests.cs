namespace diameter.Tests;

public class RoutePolicyAndAffinityTests
{
    [Test]
    public void OrderedPoliciesScoreCandidatesFromImmutableSnapshots()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0L));
        ScoringRoutePolicy first = new ScoringRoutePolicy();
        first.PreferredRoute = "second";
        first.PreferredAdjustment = 5;
        first.OtherAdjustment = 1;
        first.MutateInputs = true;
        ScoringRoutePolicy second = new ScoringRoutePolicy();
        second.PreferredRoute = "second";
        second.PreferredAdjustment = 7;
        table.ConfigureRouteSelection(
            new List<IRoutePolicy> { first, second },
            8);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "first",
            "first.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "second",
            "second.example"));
        DiameterRouteRequest request = CreateRequest("session-original");
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("first.example", true));
        peers.Add(CreatePeer("second.example", true));

        RouteSelectionResult result = table.SelectStaticRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("second"));
            Assert.That(result.SelectedCandidate.PolicyScore, Is.EqualTo(12));
            Assert.That(result.Candidates[0].PolicyScore, Is.EqualTo(1));
            Assert.That(result.Candidates[1].PolicyScore, Is.EqualTo(12));
            Assert.That(first.CallCount, Is.EqualTo(2));
            Assert.That(second.CallCount, Is.EqualTo(2));
            Assert.That(second.ObservedSessionIds,
                Is.EqualTo(new[] { "session-original", "session-original" }));
            Assert.That(second.ObservedRouteIds,
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(request.SessionId, Is.EqualTo("session-original"));
            Assert.That(table.ReadStaticRouteSnapshot("second")!
                .Configuration.RouteIdentifier, Is.EqualTo("second"));
        });
    }

    [Test]
    public void TerminalPolicySafelyStopsLaterPoliciesPerCandidate()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0L));
        TerminalRoutePolicy terminal = new TerminalRoutePolicy();
        terminal.AcceptedRoute = "accepted";
        ScoringRoutePolicy later = new ScoringRoutePolicy();
        later.PreferredAdjustment = 100;
        later.OtherAdjustment = 100;
        table.ConfigureRouteSelection(
            new List<IRoutePolicy> { terminal, later },
            8);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "rejected",
            "rejected.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "accepted",
            "accepted.example"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("rejected.example", true));
        peers.Add(CreatePeer("accepted.example", true));

        RouteSelectionResult result = table.SelectStaticRoute(
            CreateRequest(string.Empty),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("accepted"));
            Assert.That(result.Candidates[0].PolicyAccepted, Is.False);
            Assert.That(result.Candidates[0].PolicyTerminal, Is.True);
            Assert.That(result.Candidates[0].Code,
                Is.EqualTo("policy-terminal-rejected"));
            Assert.That(result.Candidates[1].PolicyAccepted, Is.True);
            Assert.That(result.Candidates[1].PolicyTerminal, Is.True);
            Assert.That(later.CallCount, Is.Zero);
        });
    }

    [Test]
    public void InvalidPolicyResultRejectsCandidatesWithoutApplyingMutations()
    {
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0L));
        table.ConfigureRouteSelection(
            new List<IRoutePolicy> { new NullRoutePolicy() },
            8);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "route",
            "peer.example"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("peer.example", true));

        RouteSelectionResult result = table.SelectStaticRoute(
            CreateRequest(string.Empty),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedCandidate, Is.Null);
            Assert.That(result.Code, Is.EqualTo("route-unavailable"));
            Assert.That(result.Candidates[0].PolicyAccepted, Is.False);
            Assert.That(result.Candidates[0].PolicyTerminal, Is.True);
            Assert.That(result.Candidates[0].Code,
                Is.EqualTo("route-policy-result-invalid"));
        });
    }

    [Test]
    public void EqualScoresAndWeightsUseStableRegistrationOrder()
    {
        SequenceRouteRandom random = new SequenceRouteRandom(1L);
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            random);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "first",
            "first.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "second",
            "second.example"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("first.example", true));
        peers.Add(CreatePeer("second.example", true));

        RouteSelectionResult result = table.SelectStaticRoute(
            CreateRequest(string.Empty),
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("first"));
            Assert.That(random.Index, Is.Zero);
        });
    }

    [Test]
    public void SessionAffinitySticksRebindsAndYieldsToHostSpecificity()
    {
        SequenceRouteRandom random = new SequenceRouteRandom(1L, 0L);
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            random);
        table.ConfigureRouteSelection(new List<IRoutePolicy>(), 8);
        DiameterRouteConfiguration first = CreatePeerRoute(
            "first",
            "first.example");
        first.Weight = 1;
        DiameterRouteConfiguration second = CreatePeerRoute(
            "second",
            "second.example");
        second.Weight = 3;
        table.AddOrReplaceStaticRoute(first);
        table.AddOrReplaceStaticRoute(second);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        DiameterPeerSnapshot firstPeer = CreatePeer("first.example", true);
        DiameterPeerSnapshot secondPeer = CreatePeer("second.example", true);
        peers.Add(firstPeer);
        peers.Add(secondPeer);
        DiameterRouteRequest request = CreateRequest("session-one");

        RouteSelectionResult initial = table.SelectStaticRoute(request, peers);
        RouteSelectionResult sticky = table.SelectStaticRoute(request, peers);
        secondPeer.Available = false;
        RouteSelectionResult rebound = table.SelectStaticRoute(request, peers);
        secondPeer.Available = true;
        RouteSelectionResult reboundSticky = table.SelectStaticRoute(
            request,
            peers);
        DiameterRouteConfiguration host = CreatePeerRoute(
            "host",
            "host.example");
        host.DestinationHost = CreateIdentity("service.example");
        table.AddOrReplaceStaticRoute(host);
        peers.Add(CreatePeer("host.example", true));
        request.DestinationHost = CreateIdentity("service.example");
        RouteSelectionResult hostSpecific = table.SelectStaticRoute(
            request,
            peers);

        Assert.Multiple(() =>
        {
            Assert.That(initial.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("second"));
            Assert.That(sticky.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("second"));
            Assert.That(sticky.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.SessionAffinity));
            Assert.That(rebound.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("first"));
            Assert.That(reboundSticky.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("first"));
            Assert.That(reboundSticky.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.SessionAffinity));
            Assert.That(hostSpecific.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("host"));
            Assert.That(hostSpecific.SelectedCandidate.Source,
                Is.EqualTo(DiameterRouteSource.Static));
        });
    }

    private static DiameterRouteConfiguration CreatePeerRoute(
        string identifier,
        string peerIdentity)
    {
        DiameterRouteConfiguration result =
            new DiameterRouteConfiguration();
        result.RouteIdentifier = identifier;
        result.DestinationRealm.Value = "example.net";
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity(peerIdentity);
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterRouteRequest CreateRequest(string sessionId)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.ApplicationId = 4U;
        result.DestinationRealm = CreateIdentity("example.net");
        result.SessionId = sessionId;
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

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class ScoringRoutePolicy : IRoutePolicy
{
    internal string PreferredRoute = string.Empty;
    internal int PreferredAdjustment;
    internal int OtherAdjustment;
    internal bool MutateInputs;
    internal int CallCount;
    internal List<string> ObservedSessionIds = new List<string>();
    internal List<string> ObservedRouteIds = new List<string>();

    public DiameterRouteCandidate EvaluateDiameterRouteCandidate(
        DiameterRouteRequest? request,
        DiameterRouteCandidate? candidate)
    {
        Assert.That(request, Is.Not.Null);
        Assert.That(candidate, Is.Not.Null);
        string routeIdentifier = candidate!.Configuration.RouteIdentifier;
        ObservedSessionIds.Add(request!.SessionId);
        ObservedRouteIds.Add(routeIdentifier);
        CallCount = CallCount + 1;
        candidate.PolicyScore = candidate.PolicyScore +
            (routeIdentifier == PreferredRoute ?
                PreferredAdjustment : OtherAdjustment);
        candidate.PolicyAccepted = true;
        candidate.Code = "policy-scored";
        candidate.Message = "The policy scored the route.";
        if (MutateInputs)
        {
            request.SessionId = "mutated-session";
            candidate.Configuration.RouteIdentifier = "mutated-route";
        }

        return candidate;
    }
}

internal class TerminalRoutePolicy : IRoutePolicy
{
    internal string AcceptedRoute = string.Empty;

    public DiameterRouteCandidate EvaluateDiameterRouteCandidate(
        DiameterRouteRequest? request,
        DiameterRouteCandidate? candidate)
    {
        Assert.That(candidate, Is.Not.Null);
        bool accepted = candidate!.Configuration.RouteIdentifier ==
            AcceptedRoute;
        candidate.PolicyAccepted = accepted;
        candidate.PolicyTerminal = true;
        candidate.Code = accepted ?
            "policy-terminal-accepted" :
            "policy-terminal-rejected";
        candidate.Message = "The terminal policy completed evaluation.";
        return candidate;
    }
}

internal class NullRoutePolicy : IRoutePolicy
{
    public DiameterRouteCandidate EvaluateDiameterRouteCandidate(
        DiameterRouteRequest? request,
        DiameterRouteCandidate? candidate)
    {
        return null!;
    }
}
