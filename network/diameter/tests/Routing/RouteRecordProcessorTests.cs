using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class RouteRecordProcessorTests
{
    [Test]
    public void LocalIdentityInRouteRecordReturnsLoopDetectedErrorAnswer()
    {
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(
                DiameterNodeRole.Server | DiameterNodeRole.Relay,
                null),
            CreateRouteTable());
        DiameterRouteRequest request = CreateRequest(
            "local.example",
            string.Empty);
        request.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.Message.HopByHopIdentifier = 0x01020304U;
        request.Message.EndToEndIdentifier = 0x05060708U;
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.RouteRecord,
            "NODE.LOCAL.EXAMPLE"));
        DiameterAvp proxyInfo = new DiameterAvp();
        proxyInfo.Code = DiameterAvpCodes.ProxyInfo;
        proxyInfo.Flags = DiameterAvpFlags.Mandatory;
        proxyInfo.Data = new byte[] { 1, 2, 3, 4 };
        request.Message.Avps.Add(proxyInfo);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Rejected, Is.True);
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.LoopDetected));
            Assert.That(decision.Code, Is.EqualTo("route-loop-detected"));
            Assert.That(decision.RoutedMessage, Is.Not.Null);
            Assert.That(decision.RoutedMessage!.Flags &
                DiameterMessageFlags.Request, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(decision.RoutedMessage.Flags &
                DiameterMessageFlags.Proxyable,
                Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(decision.RoutedMessage.Flags & DiameterMessageFlags.Error,
                Is.EqualTo(DiameterMessageFlags.Error));
            Assert.That(decision.RoutedMessage.HopByHopIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(decision.RoutedMessage.EndToEndIdentifier,
                Is.EqualTo(0x05060708U));
            Assert.That(ReadUnsigned32Avp(
                decision.RoutedMessage,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.LoopDetected));
            Assert.That(ReadTextAvp(
                decision.RoutedMessage,
                DiameterAvpCodes.OriginHost),
                Is.EqualTo("node.local.example"));
            Assert.That(CountAvps(
                decision.RoutedMessage,
                DiameterAvpCodes.ProxyInfo), Is.EqualTo(1));
            Assert.That(request.Message.Avps, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void RouteRecordsAvoidVisitedCandidatesAndPermitAlternateRoute()
    {
        RouteTable table = CreateRouteTable(0L);
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "visited",
            "remote.example",
            "visited.example"));
        table.AddOrReplaceStaticRoute(CreatePeerRoute(
            "alternate",
            "remote.example",
            "alternate.example"));
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Relay, null),
            table);
        DiameterRouteRequest request = CreateRequest(
            "remote.example",
            string.Empty);
        request.SourcePeer = CreateIdentity("ingress.example");
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.RouteRecord,
            "VISITED.EXAMPLE"));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("visited.example"));
        peers.Add(CreatePeer("alternate.example"));

        DiameterRouteDecision alternate = selector.SelectRoute(request, peers);

        table.RemoveStaticRoute("alternate");
        DiameterRouteDecision exhausted = selector.SelectRoute(request, peers);

        Assert.Multiple(() =>
        {
            Assert.That(alternate.Routed, Is.True);
            Assert.That(alternate.SelectedCandidate!.Configuration.RouteIdentifier,
                Is.EqualTo("alternate"));
            Assert.That(alternate.Candidates, Has.Count.EqualTo(2));
            Assert.That(alternate.Candidates[0].LoopFree, Is.False);
            Assert.That(alternate.Candidates[0].Code,
                Is.EqualTo("route-peer-excluded"));
            Assert.That(ReadRouteRecords(alternate.RoutedMessage!),
                Is.EqualTo(new[] { "visited.example", "ingress.example" }));
            Assert.That(ReadRouteRecords(request.Message),
                Is.EqualTo(new[] { "visited.example" }));
            Assert.That(exhausted.Routed, Is.False);
            Assert.That(exhausted.Rejected, Is.False);
            Assert.That(exhausted.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToDeliver));
            Assert.That(exhausted.Candidates[0].Code,
                Is.EqualTo("route-peer-excluded"));
        });
    }

    [TestCase(
        DiameterRouteAction.Relay,
        DiameterAgentRole.Relay,
        DiameterNodeRole.Relay)]
    [TestCase(
        DiameterRouteAction.Proxy,
        DiameterAgentRole.Proxy,
        DiameterNodeRole.Proxy)]
    public void ForwardingAppendsSourcePeerButLocalOriginAddsNothing(
        DiameterRouteAction action,
        DiameterAgentRole agentRole,
        DiameterNodeRole nodeRole)
    {
        RouteTable table = CreateRouteTable(0L, 0L);
        DiameterRouteConfiguration route = CreatePeerRoute(
            "relay",
            "remote.example",
            "egress.example");
        route.Action = action;
        route.AgentRole = agentRole;
        table.AddOrReplaceStaticRoute(route);
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(nodeRole, null),
            table);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreatePeer("egress.example"));
        DiameterRouteRequest received = CreateRequest(
            "remote.example",
            string.Empty);
        received.SourcePeer = CreateIdentity("SOURCE.EXAMPLE");
        DiameterRouteRequest local = CreateRequest(
            "remote.example",
            string.Empty);

        DiameterRouteDecision forwarded = selector.SelectRoute(received, peers);
        DiameterRouteDecision originated = selector.SelectRoute(local, peers);

        Assert.Multiple(() =>
        {
            Assert.That(ReadRouteRecords(forwarded.RoutedMessage!),
                Is.EqualTo(new[] { "source.example" }));
            Assert.That(FindAvp(
                forwarded.RoutedMessage!,
                DiameterAvpCodes.RouteRecord).Flags,
                Is.EqualTo(DiameterAvpFlags.Mandatory));
            Assert.That(ReadRouteRecords(forwarded.Request.Message), Is.Empty);
            Assert.That(ReadRouteRecords(received.Message), Is.Empty);
            Assert.That(ReadRouteRecords(originated.RoutedMessage!), Is.Empty);
        });
    }

    [Test]
    public void PathPolicyDenialReturnsAuthorizationRejectedAnswer()
    {
        RecordingRoutePathPolicy policy = new RecordingRoutePathPolicy();
        policy.Result.Authorized = false;
        policy.Result.Code = "path-denied";
        policy.Result.Message = "The traversed path is not authorized.";
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Server, policy),
            CreateRouteTable());
        DiameterRouteRequest request = CreateRequest(
            "local.example",
            string.Empty);
        request.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.RouteRecord,
            "first.example"));
        request.Message.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.RouteRecord,
            "SECOND.EXAMPLE"));

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(policy.CallCount, Is.EqualTo(1));
            Assert.That(policy.ObservedRequest, Is.Not.Null);
            Assert.That(policy.ObservedRequest!.Request,
                Is.Not.SameAs(request));
            Assert.That(policy.ObservedRequest.Request.Message,
                Is.Not.SameAs(request.Message));
            Assert.That(policy.ObservedRequest.LocalHost.Value,
                Is.EqualTo("node.local.example"));
            Assert.That(ReadIdentities(policy.ObservedRequest.TraversedPeers),
                Is.EqualTo(new[] { "first.example", "second.example" }));
            Assert.That(decision.Routed, Is.False);
            Assert.That(decision.Rejected, Is.True);
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.AuthorizationRejected));
            Assert.That(decision.Code, Is.EqualTo("path-denied"));
            Assert.That(decision.RoutedMessage, Is.Not.Null);
            Assert.That(decision.RoutedMessage!.Flags & DiameterMessageFlags.Error,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(ReadUnsigned32Avp(
                decision.RoutedMessage,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.AuthorizationRejected));
            Assert.That(request.ExcludedPeers, Is.Empty);
        });
    }

    [Test]
    public void InvalidRouteRecordReturnsInvalidAvpValueErrorAnswer()
    {
        RouteSelector selector = new RouteSelector(
            CreateNodeConfiguration(DiameterNodeRole.Relay, null),
            CreateRouteTable());
        DiameterRouteRequest request = CreateRequest(
            "remote.example",
            string.Empty);
        DiameterAvp invalid = new DiameterAvp();
        invalid.Code = DiameterAvpCodes.RouteRecord;
        invalid.Flags = DiameterAvpFlags.Mandatory;
        invalid.Data = new byte[] { 0xff };
        request.Message.Avps.Add(invalid);

        DiameterRouteDecision decision = selector.SelectRoute(
            request,
            new List<DiameterPeerSnapshot>());

        Assert.Multiple(() =>
        {
            Assert.That(decision.Rejected, Is.True);
            Assert.That(decision.Code, Is.EqualTo("route-record-invalid"));
            Assert.That(decision.ResultCode,
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
            Assert.That(ReadUnsigned32Avp(
                decision.RoutedMessage!,
                DiameterAvpCodes.ResultCode),
                Is.EqualTo(DiameterResultCodes.InvalidAvpValue));
        });
    }

    private static RouteTable CreateRouteTable(params long[] values)
    {
        return new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(values));
    }

    private static DiameterNodeConfiguration CreateNodeConfiguration(
        DiameterNodeRole roles,
        IRoutePathPolicy? policy)
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = roles;
        result.RoutePathPolicy = policy;
        result.Transport.EnableListener =
            (roles & DiameterNodeRole.Server) != DiameterNodeRole.None;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
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
        string host)
    {
        DiameterRouteRequest result = new DiameterRouteRequest();
        result.Message.Flags = DiameterMessageFlags.Request;
        result.Message.CommandCode = 300U;
        result.Message.ApplicationId = 4U;
        result.ApplicationId = 4U;
        result.DestinationRealm = CreateIdentity(realm);
        if (host.Length > 0)
        {
            result.DestinationHost = CreateIdentity(host);
        }

        return result;
    }

    private static DiameterPeerSnapshot CreatePeer(string identity)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity(identity);
        result.Available = true;
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

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return message.Avps[index];
            }

            index = index + 1;
        }

        throw new AssertionException("Expected AVP was not present.");
    }

    private static int CountAvps(DiameterMessage message, uint code)
    {
        int count = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static uint ReadUnsigned32Avp(DiameterMessage message, uint code)
    {
        return NetworkByteOrder.ReadUInt32(FindAvp(message, code).Data);
    }

    private static string ReadTextAvp(DiameterMessage message, uint code)
    {
        return Encoding.UTF8.GetString(FindAvp(message, code).Data);
    }

    private static List<string> ReadRouteRecords(DiameterMessage message)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == DiameterAvpCodes.RouteRecord && avp.VendorId == 0U)
            {
                result.Add(Encoding.UTF8.GetString(avp.Data).ToLowerInvariant());
            }

            index = index + 1;
        }

        return result;
    }

    private static List<string> ReadIdentities(List<DiameterIdentity> identities)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < identities.Count)
        {
            result.Add(identities[index].Value);
            index = index + 1;
        }

        return result;
    }
}

internal class RecordingRoutePathPolicy : IRoutePathPolicy
{
    internal DiameterRoutePathAuthorizationResult Result =
        new DiameterRoutePathAuthorizationResult();
    internal DiameterRoutePathAuthorizationRequest? ObservedRequest;
    internal int CallCount;

    public DiameterRoutePathAuthorizationResult AuthorizeDiameterRoutePath(
        DiameterRoutePathAuthorizationRequest? request)
    {
        CallCount = CallCount + 1;
        ObservedRequest = request;
        return Result;
    }
}
