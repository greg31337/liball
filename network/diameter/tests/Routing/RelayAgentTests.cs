using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class RelayAgentTests
{
    [Test]
    public void ForwardingRequestSavesExactPathAndReplacesOnlyHopIdentifier()
    {
        RelayAgentTestContext context = CreateRelayAgentTestContext(
            "source-connection",
            "destination-connection",
            41U,
            51U);
        context.Identifiers.HopByHopIdentifier = 700U;

        RelayRequestProcessingResult result =
            context.Agent.ForwardRelayRequest(
                context.Decision,
                context.SourceConnection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("relay-request-forwarded"));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.HopByHopIdentifier, Is.EqualTo(701U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(51U));
            Assert.That(result.Message.Flags,
                Is.EqualTo(context.Decision.RoutedMessage!.Flags));
            Assert.That(result.Message.CommandCode,
                Is.EqualTo(context.Decision.RoutedMessage.CommandCode));
            Assert.That(result.Message.ApplicationId,
                Is.EqualTo(context.Decision.RoutedMessage.ApplicationId));
            Assert.That(result.Message.Avps, Has.Count.EqualTo(2));
            Assert.That(result.Message.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(ReadTextAvp(
                result.Message,
                DiameterAvpCodes.RouteRecord),
                Is.EqualTo("source.example"));
            Assert.That(context.Decision.RoutedMessage.HopByHopIdentifier,
                Is.EqualTo(41U));
            Assert.That(context.Decision.Request.Message.HopByHopIdentifier,
                Is.EqualTo(41U));
            Assert.That(context.Decision.Request.Message.Avps,
                Has.Count.EqualTo(1));
            Assert.That(result.DestinationConnection!.ConnectionId,
                Is.EqualTo("destination-connection"));
            Assert.That(result.DestinationPeer!.Value,
                Is.EqualTo("destination.example"));
            Assert.That(result.Transaction, Is.Not.Null);
            Assert.That(result.Transaction!.SourceConnection.ConnectionId,
                Is.EqualTo("source-connection"));
            Assert.That(result.Transaction.DestinationConnection.ConnectionId,
                Is.EqualTo("destination-connection"));
            Assert.That(result.Transaction.SourceHopByHopIdentifier,
                Is.EqualTo(41U));
            Assert.That(result.Transaction.DestinationHopByHopIdentifier,
                Is.EqualTo(701U));
            Assert.That(result.Transaction.EndToEndIdentifier,
                Is.EqualTo(51U));
            Assert.That(context.Agent.ReadRelayTransactionCount(), Is.EqualTo(1));
        });

        context.SourceConnection.ConnectionId = "changed-source";
        context.Decision.RoutedMessage!.Avps[0].Data[0] = 9;
        Assert.Multiple(() =>
        {
            Assert.That(result.Transaction!.SourceConnection.ConnectionId,
                Is.EqualTo("source-connection"));
            Assert.That(result.Message!.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void CorrelatedAnswerRestoresUpstreamHopAndPreservesEverythingElse()
    {
        RelayAgentTestContext context = CreateRelayAgentTestContext(
            "upstream-connection",
            "downstream-connection",
            101U,
            202U);
        context.Identifiers.HopByHopIdentifier = 900U;
        RelayRequestProcessingResult request =
            context.Agent.ForwardRelayRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage answer = CreateRelayAnswer(
            request.Message!,
            DiameterResultCodes.UnableToDeliver,
            true);
        DiameterAvp opaque = new DiameterAvp();
        opaque.Code = 9876U;
        opaque.Flags = DiameterAvpFlags.None;
        opaque.Data = new byte[] { 7, 6, 5 };
        answer.Avps.Add(opaque);

        RelayAnswerProcessingResult result =
            context.Agent.ForwardRelayAnswer(
                request.DestinationConnection,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Correlated, Is.True);
            Assert.That(result.Forwarded, Is.True);
            Assert.That(result.Discarded, Is.False);
            Assert.That(result.Code, Is.EqualTo("relay-answer-forwarded"));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.HopByHopIdentifier, Is.EqualTo(101U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(202U));
            Assert.That(result.Message.Flags, Is.EqualTo(answer.Flags));
            Assert.That(result.Message.CommandCode, Is.EqualTo(answer.CommandCode));
            Assert.That(result.Message.ApplicationId,
                Is.EqualTo(answer.ApplicationId));
            Assert.That(result.Message.Avps, Has.Count.EqualTo(2));
            Assert.That(result.Message.Avps[0].Data,
                Is.EqualTo(answer.Avps[0].Data));
            Assert.That(result.Message.Avps[1].Code, Is.EqualTo(9876U));
            Assert.That(result.Message.Avps[1].Data,
                Is.EqualTo(new byte[] { 7, 6, 5 }));
            Assert.That(answer.HopByHopIdentifier, Is.EqualTo(901U));
            Assert.That(answer.EndToEndIdentifier, Is.EqualTo(202U));
            Assert.That(result.DestinationConnection!.ConnectionId,
                Is.EqualTo("upstream-connection"));
            Assert.That(result.DestinationPeer!.Value,
                Is.EqualTo("source.example"));
            Assert.That(result.Transaction!.ForwardedRequest.HopByHopIdentifier,
                Is.EqualTo(901U));
            Assert.That(context.Agent.ReadRelayTransactionCount(), Is.Zero);
        });
    }

    [Test]
    public void CreditControlPrivacyPolicyPreservesRelayRoutingSemantics()
    {
        RelayAgentTestContext context = CreateRelayAgentTestContext(
            "privacy-upstream",
            "privacy-downstream",
            301U,
            302U);
        context.Identifiers.HopByHopIdentifier = 800U;
        context.Decision.Request.Message.CommandCode =
            DiameterCommandCodes.CreditControl;
        context.Decision.RoutedMessage!.CommandCode =
            DiameterCommandCodes.CreditControl;
        AddRelayPrivacyAvps(context.Decision.Request.Message);
        AddRelayPrivacyAvps(context.Decision.RoutedMessage);
        CreditControlPrivacyPolicy policy = new CreditControlPrivacyPolicy();
        policy.Action = DiameterCreditControlPrivacyAction.Remove;
        context.Agent.CreditControlPrivacyPolicy = policy;

        RelayRequestProcessingResult result =
            context.Agent.ForwardRelayRequest(
                context.Decision,
                context.SourceConnection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(FindRelayAvp(
                result.Message!,
                DiameterAvpCodes.SessionId), Is.Not.Null);
            Assert.That(FindRelayAvp(
                result.Message!,
                DiameterAvpCodes.SubscriptionId), Is.Not.Null);
            Assert.That(FindRelayAvp(
                result.Message!,
                DiameterAvpCodes.ServiceContextId), Is.Not.Null);
            Assert.That(FindRelayAvp(
                result.Message!,
                DiameterAvpCodes.UserEquipmentInfo), Is.Null);
            Assert.That(ReadTextAvp(
                result.Message!,
                DiameterAvpCodes.RouteRecord),
                Is.EqualTo("source.example"));
            Assert.That(result.Message!.EndToEndIdentifier,
                Is.EqualTo(302U));
            Assert.That(context.Agent.ReadRelayTransactionCount(),
                Is.EqualTo(1));
            Assert.That(FindRelayAvp(
                context.Decision.RoutedMessage,
                DiameterAvpCodes.UserEquipmentInfo), Is.Not.Null);
        });
    }

    [Test]
    public void AnswerMismatchesAreDiscardedWithoutConsumingTransaction()
    {
        RelayAgentTestContext context = CreateRelayAgentTestContext(
            "upstream",
            "downstream",
            301U,
            302U);
        context.Identifiers.HopByHopIdentifier = 1000U;
        RelayRequestProcessingResult request =
            context.Agent.ForwardRelayRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage valid = CreateRelayAnswer(
            request.Message!,
            DiameterResultCodes.Success,
            false);
        DiameterConnectionMetadata wrongPath = CreateRelayConnection(
            "different-downstream",
            "node.local.example",
            "destination.example",
            3868);
        DiameterConnectionMetadata changedPath =
            context.Agent.CloneRelayConnection(
                request.DestinationConnection!);
        changedPath.RemoteEndpoint.Port = 3870;
        DiameterMessage wrongHop = CloneMessage(valid);
        wrongHop.HopByHopIdentifier = wrongHop.HopByHopIdentifier + 1U;
        DiameterMessage wrongEnd = CloneMessage(valid);
        wrongEnd.EndToEndIdentifier = wrongEnd.EndToEndIdentifier + 1U;
        DiameterMessage wrongProxy = CloneMessage(valid);
        wrongProxy.Flags = wrongProxy.Flags & ~DiameterMessageFlags.Proxyable;

        RelayAnswerProcessingResult path = context.Agent.ForwardRelayAnswer(
            wrongPath,
            valid);
        RelayAnswerProcessingResult hop = context.Agent.ForwardRelayAnswer(
            request.DestinationConnection,
            wrongHop);
        RelayAnswerProcessingResult metadata =
            context.Agent.ForwardRelayAnswer(
                changedPath,
                valid);
        RelayAnswerProcessingResult end = context.Agent.ForwardRelayAnswer(
            request.DestinationConnection,
            wrongEnd);
        RelayAnswerProcessingResult proxy = context.Agent.ForwardRelayAnswer(
            request.DestinationConnection,
            wrongProxy);

        Assert.Multiple(() =>
        {
            Assert.That(path.Code, Is.EqualTo("relay-answer-path-mismatch"));
            Assert.That(hop.Code,
                Is.EqualTo("relay-answer-hop-by-hop-mismatch"));
            Assert.That(metadata.Code,
                Is.EqualTo("relay-answer-path-mismatch"));
            Assert.That(end.Code,
                Is.EqualTo("relay-answer-end-to-end-mismatch"));
            Assert.That(proxy.Code,
                Is.EqualTo("relay-answer-proxyable-bit-mismatch"));
            Assert.That(path.Discarded, Is.True);
            Assert.That(hop.Discarded, Is.True);
            Assert.That(metadata.Discarded, Is.True);
            Assert.That(end.Discarded, Is.True);
            Assert.That(proxy.Discarded, Is.True);
            Assert.That(context.Agent.ReadRelayTransactionCount(), Is.EqualTo(1));
        });

        RelayAnswerProcessingResult completed =
            context.Agent.ForwardRelayAnswer(
                request.DestinationConnection,
                valid);
        RelayAnswerProcessingResult duplicate =
            context.Agent.ForwardRelayAnswer(
                request.DestinationConnection,
                valid);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Forwarded, Is.True);
            Assert.That(duplicate.Discarded, Is.True);
            Assert.That(duplicate.Code,
                Is.EqualTo("relay-answer-not-pending"));
            Assert.That(context.Agent.ReadRelayTransactionCount(), Is.Zero);
        });
    }

    [Test]
    public void SameInboundHopOnDifferentConnectionsReturnsToExactSourcePath()
    {
        RelayAgentIdentifierSource identifiers =
            new RelayAgentIdentifierSource();
        identifiers.HopByHopIdentifier = 1100U;
        RelayAgent agent = new RelayAgent(identifiers, 2);
        RelayAgentTestContext first = CreateRelayAgentTestContext(
            "upstream-one",
            "shared-downstream",
            77U,
            401U);
        RelayAgentTestContext second = CreateRelayAgentTestContext(
            "upstream-two",
            "shared-downstream",
            77U,
            402U);

        RelayRequestProcessingResult firstRequest = agent.ForwardRelayRequest(
            first.Decision,
            first.SourceConnection);
        RelayRequestProcessingResult secondRequest = agent.ForwardRelayRequest(
            second.Decision,
            second.SourceConnection);
        RelayAnswerProcessingResult secondAnswer = agent.ForwardRelayAnswer(
            secondRequest.DestinationConnection,
            CreateRelayAnswer(
                secondRequest.Message!,
                DiameterResultCodes.Success,
                false));
        RelayAnswerProcessingResult firstAnswer = agent.ForwardRelayAnswer(
            firstRequest.DestinationConnection,
            CreateRelayAnswer(
                firstRequest.Message!,
                DiameterResultCodes.Success,
                false));

        Assert.Multiple(() =>
        {
            Assert.That(firstRequest.Message!.HopByHopIdentifier,
                Is.EqualTo(1101U));
            Assert.That(secondRequest.Message!.HopByHopIdentifier,
                Is.EqualTo(1102U));
            Assert.That(firstRequest.Message.EndToEndIdentifier,
                Is.EqualTo(401U));
            Assert.That(secondRequest.Message.EndToEndIdentifier,
                Is.EqualTo(402U));
            Assert.That(secondAnswer.DestinationConnection!.ConnectionId,
                Is.EqualTo("upstream-two"));
            Assert.That(firstAnswer.DestinationConnection!.ConnectionId,
                Is.EqualTo("upstream-one"));
            Assert.That(secondAnswer.Message!.HopByHopIdentifier,
                Is.EqualTo(77U));
            Assert.That(firstAnswer.Message!.HopByHopIdentifier,
                Is.EqualTo(77U));
            Assert.That(agent.ReadRelayTransactionCount(), Is.Zero);
        });
    }

    [Test]
    public void CapacityAndIdentifierCollisionDoNotCreatePartialState()
    {
        RelayAgentTestContext full = CreateRelayAgentTestContext(
            "source-one",
            "destination",
            10U,
            20U);
        full.Identifiers.HopByHopIdentifier = 500U;
        RelayRequestProcessingResult accepted =
            full.Agent.ForwardRelayRequest(
                full.Decision,
                full.SourceConnection);
        RelayAgentTestContext second = CreateRelayAgentTestContext(
            "source-two",
            "destination",
            11U,
            21U);
        RelayRequestProcessingResult capacity =
            full.Agent.ForwardRelayRequest(
                second.Decision,
                second.SourceConnection);
        RelayAgentTestContext collision = CreateRelayAgentTestContext(
            "source-three",
            "destination-three",
            42U,
            22U);
        collision.Identifiers.HopByHopIdentifier = 41U;
        RelayRequestProcessingResult unavailable =
            collision.Agent.ForwardRelayRequest(
                collision.Decision,
                collision.SourceConnection);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(capacity.Accepted, Is.False);
            Assert.That(capacity.Code,
                Is.EqualTo("relay-transaction-limit-reached"));
            Assert.That(full.Agent.ReadRelayTransactionCount(), Is.EqualTo(1));
            Assert.That(unavailable.Accepted, Is.False);
            Assert.That(unavailable.Code,
                Is.EqualTo("relay-hop-by-hop-identifier-unavailable"));
            Assert.That(collision.Agent.ReadRelayTransactionCount(), Is.Zero);
        });
    }

    [Test]
    public void RelayStoresRequestPriorityAndLetsAnswerDrmpOverrideIt()
    {
        RelayAgentTestContext inheritedContext =
            CreateRelayAgentTestContext(
                "priority-upstream-one",
                "priority-downstream-one",
                61U,
                71U);
        inheritedContext.Identifiers.HopByHopIdentifier = 1200U;
        inheritedContext.Decision.Request.RoutingPriority =
            DiameterRoutingPriority.Priority8;
        inheritedContext.Decision.Request.RoutingPriorityPresent = true;
        RelayRequestProcessingResult inheritedRequest =
            inheritedContext.Agent.ForwardRelayRequest(
                inheritedContext.Decision,
                inheritedContext.SourceConnection);
        RelayAnswerProcessingResult inheritedAnswer =
            inheritedContext.Agent.ForwardRelayAnswer(
                inheritedRequest.DestinationConnection,
                CreateRelayAnswer(
                    inheritedRequest.Message!,
                    DiameterResultCodes.Success,
                    false));

        RelayAgentTestContext explicitContext =
            CreateRelayAgentTestContext(
                "priority-upstream-two",
                "priority-downstream-two",
                62U,
                72U);
        explicitContext.Identifiers.HopByHopIdentifier = 1300U;
        explicitContext.Decision.Request.RoutingPriority =
            DiameterRoutingPriority.Priority8;
        explicitContext.Decision.Request.RoutingPriorityPresent = true;
        RelayRequestProcessingResult explicitRequest =
            explicitContext.Agent.ForwardRelayRequest(
                explicitContext.Decision,
                explicitContext.SourceConnection);
        DiameterMessage explicitMessage = CreateRelayAnswer(
            explicitRequest.Message!,
            DiameterResultCodes.Success,
            false);
        DiameterAvp answerDrmp = new DiameterAvp();
        answerDrmp.Code = DiameterAvpCodes.Drmp;
        answerDrmp.Data = new byte[4];
        NetworkByteOrder.WriteInt32(
            answerDrmp.Data,
            (int)DiameterRoutingPriority.Priority1);
        explicitMessage.Avps.Add(answerDrmp);
        RelayAnswerProcessingResult explicitAnswer =
            explicitContext.Agent.ForwardRelayAnswer(
                explicitRequest.DestinationConnection,
                explicitMessage);

        Assert.Multiple(() =>
        {
            Assert.That(inheritedRequest.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority8));
            Assert.That(inheritedRequest.RoutingPriorityPresent, Is.True);
            Assert.That(inheritedAnswer.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority8));
            Assert.That(inheritedAnswer.RoutingPriorityPresent, Is.False);
            Assert.That(explicitAnswer.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority1));
            Assert.That(explicitAnswer.RoutingPriorityPresent, Is.True);
            Assert.That(explicitAnswer.Message!.Avps[^1].Code,
                Is.EqualTo(DiameterAvpCodes.Drmp));
            Assert.That(explicitAnswer.Transaction!.RoutingPriority,
                Is.EqualTo(DiameterRoutingPriority.Priority8));
        });
    }

    private static RelayAgentTestContext CreateRelayAgentTestContext(
        string sourceConnectionId,
        string destinationConnectionId,
        uint hopByHopIdentifier,
        uint endToEndIdentifier)
    {
        RelayAgentTestContext result = new RelayAgentTestContext();
        result.Identifiers = new RelayAgentIdentifierSource();
        result.Agent = new RelayAgent(result.Identifiers, 1);
        result.SourceConnection = CreateRelayConnection(
            sourceConnectionId,
            "node.local.example",
            "source.example",
            3868);
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0L));
        table.AddOrReplaceStaticRoute(CreateRelayRoute());
        RouteSelector selector = new RouteSelector(
            CreateRelayNodeConfiguration(),
            table);
        DiameterRouteRequest request = new DiameterRouteRequest();
        request.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.Message.CommandCode = 300U;
        request.Message.ApplicationId = 4U;
        request.Message.HopByHopIdentifier = hopByHopIdentifier;
        request.Message.EndToEndIdentifier = endToEndIdentifier;
        request.ApplicationId = 4U;
        request.SourcePeer = CreateIdentity("source.example");
        request.DestinationRealm = CreateIdentity("remote.example");
        DiameterAvp opaque = new DiameterAvp();
        opaque.Code = 9999U;
        opaque.Flags = DiameterAvpFlags.None;
        opaque.Data = new byte[] { 1, 2, 3 };
        request.Message.Avps.Add(opaque);
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreateRelayPeer(destinationConnectionId));
        result.Decision = selector.SelectRoute(request, peers);
        return result;
    }

    private static DiameterNodeConfiguration CreateRelayNodeConfiguration()
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Relay;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterRouteConfiguration CreateRelayRoute()
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = "relay-route";
        result.DestinationRealm.Value = "remote.example";
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity("destination.example");
        result.Action = DiameterRouteAction.Relay;
        result.AgentRole = DiameterAgentRole.Relay;
        return result;
    }

    private static DiameterPeerSnapshot CreateRelayPeer(string connectionId)
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity("destination.example");
        result.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Capabilities.CommonApplications.Add(application);
        result.Connection = CreateRelayConnection(
            connectionId,
            "node.local.example",
            "destination.example",
            3869);
        return result;
    }

    private static DiameterConnectionMetadata CreateRelayConnection(
        string connectionId,
        string localHost,
        string remoteHost,
        int remotePort)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = connectionId;
        result.LocalEndpoint.Host = localHost;
        result.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        result.LocalEndpoint.Port = 3868;
        result.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        result.RemoteEndpoint.Host = remoteHost;
        result.RemoteEndpoint.Address = IPAddress.Parse("192.0.2.2");
        result.RemoteEndpoint.Port = remotePort;
        result.RemoteEndpoint.Transport = DiameterTransportKind.Tcp;
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Open = true;
        return result;
    }

    private static DiameterMessage CreateRelayAnswer(
        DiameterMessage request,
        uint resultCode,
        bool error)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        if (error)
        {
            result.Flags = result.Flags | DiameterMessageFlags.Error;
        }

        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        DiameterAvp resultCodeAvp = new DiameterAvp();
        resultCodeAvp.Code = DiameterAvpCodes.ResultCode;
        resultCodeAvp.Flags = DiameterAvpFlags.Mandatory;
        resultCodeAvp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(resultCodeAvp.Data, resultCode);
        result.Avps.Add(resultCodeAvp);
        return result;
    }

    private static void AddRelayPrivacyAvps(DiameterMessage message)
    {
        DiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        message.Avps.Add(factory.CreateTextAvp(
            DiameterAvpCodes.SessionId,
            "privacy-client.example;relay;1"));
        message.Avps.Add(factory.CreateTextAvp(
            DiameterAvpCodes.ServiceContextId,
            "example.privacy"));
        List<DiameterAvp> subscriptionChildren = new List<DiameterAvp>();
        DiameterAvp subscriptionType = CreditControlCommandCodec
            .CreateCreditControlInteger32Avp(
                DiameterAvpCodes.SubscriptionIdType,
                (int)DiameterSubscriptionIdType.EndUserImsi);
        subscriptionType.Flags = DiameterAvpFlags.None;
        subscriptionChildren.Add(subscriptionType);
        DiameterAvp subscriptionData = factory.CreateTextAvp(
            DiameterAvpCodes.SubscriptionIdData,
            "001010123456789");
        subscriptionData.Flags = DiameterAvpFlags.None;
        subscriptionChildren.Add(subscriptionData);
        DiameterAvp subscription = CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.SubscriptionId,
                subscriptionChildren);
        subscription.Flags = DiameterAvpFlags.None;
        message.Avps.Add(subscription);
        List<DiameterAvp> equipmentChildren = new List<DiameterAvp>();
        DiameterAvp equipmentType = CreditControlCommandCodec
            .CreateCreditControlInteger32Avp(
                DiameterAvpCodes.UserEquipmentInfoType,
                (int)DiameterUserEquipmentInfoType.Imeisv);
        equipmentType.Flags = DiameterAvpFlags.None;
        equipmentChildren.Add(equipmentType);
        DiameterAvp equipmentValue = new DiameterAvp();
        equipmentValue.Code = DiameterAvpCodes.UserEquipmentInfoValue;
        equipmentValue.Data = new byte[] { 8, 6, 7, 5, 3, 0, 9 };
        equipmentChildren.Add(equipmentValue);
        DiameterAvp equipment = CreditControlCommandCodec
            .CreateCreditControlGroupedAvp(
                DiameterAvpCodes.UserEquipmentInfo,
                equipmentChildren);
        equipment.Flags = DiameterAvpFlags.None;
        message.Avps.Add(equipment);
    }

    private static DiameterAvp? FindRelayAvp(
        DiameterMessage message,
        uint code)
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

        return null;
    }

    private static DiameterMessage CloneMessage(DiameterMessage source)
    {
        return RouteTableCloner.CloneRouteMessage(source);
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
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

        throw new AssertionException("Expected text AVP was not present.");
    }
}

internal class RelayAgentTestContext
{
    internal RelayAgentIdentifierSource Identifiers =
        new RelayAgentIdentifierSource();
    internal RelayAgent Agent = null!;
    internal DiameterRouteDecision Decision = new DiameterRouteDecision();
    internal DiameterConnectionMetadata SourceConnection =
        new DiameterConnectionMetadata();
}

internal class RelayAgentIdentifierSource : IDiameterIdentifierSource
{
    internal uint HopByHopIdentifier;

    public uint CreateHopByHopIdentifier()
    {
        HopByHopIdentifier = HopByHopIdentifier + 1U;
        return HopByHopIdentifier;
    }

    public uint CreateEndToEndIdentifier()
    {
        throw new NotSupportedException();
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        throw new NotSupportedException();
    }

    public uint CreateOriginStateIdentifier()
    {
        throw new NotSupportedException();
    }
}
