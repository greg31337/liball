using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class ProxyAgentTests
{
    [Test]
    public void ProxyRequestAppendsOwnedProtectedStateAfterExistingProxies()
    {
        ProxyAgentTestContext context = CreateProxyAgentTestContext();
        context.ProtectionKey[0] = 99;

        ProxyRequestProcessingResult result =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);
        List<DiameterAvp> proxies = ReadProxyInformationAvps(
            result.Message!);
        List<DiameterProxyInformation> values = DecodeProxyInformation(
            proxies);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code, Is.EqualTo("proxy-request-forwarded"));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.HopByHopIdentifier, Is.EqualTo(701U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(502U));
            Assert.That(proxies, Has.Count.EqualTo(3));
            Assert.That(values[0].ProxyHost.Value,
                Is.EqualTo("first.proxy.example"));
            Assert.That(values[1].ProxyHost.Value,
                Is.EqualTo("second.proxy.example"));
            Assert.That(values[2].ProxyHost.Value,
                Is.EqualTo("node.local.example"));
            Assert.That(values[2].ProxyState,
                Has.Length.EqualTo(ProxyAgent.ProxyStateLength));
            Assert.That(context.Agent.VerifyProtectedProxyState(
                values[2].ProxyState), Is.True);
            Assert.That(values[2].ProxyState,
                Is.Not.EqualTo(context.ProtectionKey));
            Assert.That(ReadProxyInformationAvps(
                context.Decision.RoutedMessage!), Has.Count.EqualTo(2));
            Assert.That(ReadProxyInformationAvps(
                context.Decision.Request.Message), Has.Count.EqualTo(2));
            Assert.That(result.Transaction!.Action,
                Is.EqualTo(DiameterRouteAction.Proxy));
            Assert.That(result.Transaction.AgentRole,
                Is.EqualTo(DiameterAgentRole.Proxy));
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.EqualTo(1));
            Assert.That(context.Agent.Transactions.ReadRelayTransactionCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ProxyForwardingPreservesRouteRecordPathMetadata()
    {
        ProxyAgentTestContext context = CreateProxyAgentTestContext();

        ProxyRequestProcessingResult request =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage answer = CreateProxyAnswer(
            request.Message!,
            false,
            DiameterResultCodes.Success);
        answer.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.RouteRecord,
            "answer-path.example"));
        ProxyAnswerProcessingResult forwarded =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(ReadTextAvps(
                context.Decision.RoutedMessage!,
                DiameterAvpCodes.RouteRecord),
                Is.EqualTo(new string[] { "source.example" }));
            Assert.That(ReadTextAvps(
                request.Message!,
                DiameterAvpCodes.RouteRecord),
                Is.EqualTo(new string[] { "source.example" }));
            Assert.That(ReadProxyInformationAvps(request.Message!),
                Has.Count.EqualTo(3));
            Assert.That(ReadTextAvps(
                forwarded.Message!,
                DiameterAvpCodes.RouteRecord),
                Is.EqualTo(new string[] { "answer-path.example" }));
            Assert.That(ReadProxyInformationAvps(forwarded.Message!),
                Has.Count.EqualTo(2));
        });
    }

    [TestCase(false, DiameterResultCodes.Success)]
    [TestCase(true, DiameterResultCodes.UnableToDeliver)]
    public void ProxyAnswerRemovesOnlyLocalLastStateAndTraversesErrors(
        bool error,
        uint resultCode)
    {
        ProxyAgentTestContext context = CreateProxyAgentTestContext();
        ProxyRequestProcessingResult request =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage answer = CreateProxyAnswer(
            request.Message!,
            error,
            resultCode);
        List<DiameterAvp> receivedProxies =
            ReadProxyInformationAvps(answer);

        ProxyAnswerProcessingResult result =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                answer);
        List<DiameterAvp> forwardedProxies =
            ReadProxyInformationAvps(result.Message!);
        List<DiameterProxyInformation> values =
            DecodeProxyInformation(forwardedProxies);

        Assert.Multiple(() =>
        {
            Assert.That(result.Correlated, Is.True);
            Assert.That(result.Forwarded, Is.True);
            Assert.That(result.Discarded, Is.False);
            Assert.That(result.Code, Is.EqualTo("proxy-answer-forwarded"));
            Assert.That(result.Message!.HopByHopIdentifier, Is.EqualTo(501U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(502U));
            Assert.That(result.Message.Flags, Is.EqualTo(answer.Flags));
            Assert.That(ReadUnsigned32Avp(
                result.Message,
                DiameterAvpCodes.ResultCode), Is.EqualTo(resultCode));
            Assert.That(forwardedProxies, Has.Count.EqualTo(2));
            Assert.That(values[0].ProxyHost.Value,
                Is.EqualTo("first.proxy.example"));
            Assert.That(values[0].ProxyState,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(values[1].ProxyHost.Value,
                Is.EqualTo("second.proxy.example"));
            Assert.That(values[1].ProxyState,
                Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(receivedProxies, Has.Count.EqualTo(3));
            Assert.That(ReadProxyInformationAvps(answer), Has.Count.EqualTo(3));
            Assert.That(result.DestinationConnection!.ConnectionId,
                Is.EqualTo("source-connection"));
            Assert.That(result.DestinationPeer!.Value,
                Is.EqualTo("source.example"));
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.Zero);
            Assert.That(context.Agent.Transactions.ReadRelayTransactionCount(),
                Is.Zero);
        });
    }

    [Test]
    public void ForgedMissingAndOutOfOrderProxyStateCannotConsumeTransaction()
    {
        ProxyAgentTestContext context = CreateProxyAgentTestContext();
        ProxyRequestProcessingResult request =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage valid = CreateProxyAnswer(
            request.Message!,
            false,
            DiameterResultCodes.Success);
        DiameterMessage forged = CloneMessage(valid);
        TamperLastProxyState(forged);
        DiameterMessage missing = CloneMessage(valid);
        RemoveLastProxyInformation(missing);
        DiameterMessage outOfOrder = CloneMessage(valid);
        outOfOrder.Avps.Add(CreateProxyInformationAvp(
            "later.proxy.example",
            new byte[] { 9, 8, 7 }));

        ProxyAnswerProcessingResult forgedResult =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                forged);
        ProxyAnswerProcessingResult missingResult =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                missing);
        ProxyAnswerProcessingResult orderedResult =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                outOfOrder);

        Assert.Multiple(() =>
        {
            Assert.That(forgedResult.Code,
                Is.EqualTo("proxy-state-invalid"));
            Assert.That(missingResult.Code,
                Is.EqualTo("proxy-information-not-local"));
            Assert.That(orderedResult.Code,
                Is.EqualTo("proxy-information-not-local"));
            Assert.That(forgedResult.Discarded, Is.True);
            Assert.That(missingResult.Discarded, Is.True);
            Assert.That(orderedResult.Discarded, Is.True);
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.EqualTo(1));
            Assert.That(context.Agent.Transactions.ReadRelayTransactionCount(),
                Is.EqualTo(1));
        });

        ProxyAnswerProcessingResult completed =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                valid);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Forwarded, Is.True);
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.Zero);
            Assert.That(context.Agent.Transactions.ReadRelayTransactionCount(),
                Is.Zero);
        });
    }

    [Test]
    public void WrongConnectionAndCorrelationDoNotReleaseProtectedState()
    {
        ProxyAgentTestContext context = CreateProxyAgentTestContext();
        ProxyRequestProcessingResult request =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);
        DiameterMessage valid = CreateProxyAnswer(
            request.Message!,
            false,
            DiameterResultCodes.Success);
        DiameterConnectionMetadata wrongConnection = CreateConnection(
            "different-connection",
            "destination.example",
            3869);
        DiameterMessage wrongEndToEnd = CloneMessage(valid);
        wrongEndToEnd.EndToEndIdentifier =
            wrongEndToEnd.EndToEndIdentifier + 1U;

        ProxyAnswerProcessingResult path =
            context.Agent.ForwardProxyAnswer(
                wrongConnection,
                valid);
        ProxyAnswerProcessingResult correlation =
            context.Agent.ForwardProxyAnswer(
                request.DestinationConnection,
                wrongEndToEnd);

        Assert.Multiple(() =>
        {
            Assert.That(path.Code, Is.EqualTo("proxy-answer-not-pending"));
            Assert.That(correlation.Code,
                Is.EqualTo("relay-answer-end-to-end-mismatch"));
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.EqualTo(1));
            Assert.That(context.Agent.Transactions.ReadRelayTransactionCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ProxyAgentRejectsWeakProtectionKeysAndNonProxyRoutes()
    {
        ProxyAgentIdentifierSource identifiers =
            new ProxyAgentIdentifierSource();
        DiameterIdentity localHost = CreateIdentity("node.local.example");
        Assert.Throws<ArgumentException>(() => new ProxyAgent(
            localHost,
            new byte[15],
            identifiers,
            1));
        ProxyAgentTestContext context = CreateProxyAgentTestContext();
        context.Decision.Action = DiameterRouteAction.Relay;

        ProxyRequestProcessingResult result =
            context.Agent.ForwardProxyRequest(
                context.Decision,
                context.SourceConnection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code, Is.EqualTo("proxy-route-required"));
            Assert.That(context.Agent.ReadProxyTransactionCount(), Is.Zero);
        });
    }

    private static ProxyAgentTestContext CreateProxyAgentTestContext()
    {
        ProxyAgentTestContext result = new ProxyAgentTestContext();
        result.ProtectionKey = new byte[32];
        int keyIndex = 0;
        while (keyIndex < result.ProtectionKey.Length)
        {
            result.ProtectionKey[keyIndex] = (byte)(keyIndex + 1);
            keyIndex = keyIndex + 1;
        }

        result.Identifiers = new ProxyAgentIdentifierSource();
        result.Identifiers.HopByHopIdentifier = 700U;
        result.Agent = new ProxyAgent(
            CreateIdentity("node.local.example"),
            result.ProtectionKey,
            result.Identifiers,
            2);
        result.SourceConnection = CreateConnection(
            "source-connection",
            "source.example",
            3868);
        RouteTable table = new RouteTable(
            new FakeDiscoveryClock(),
            new SequenceRouteRandom(0L));
        table.AddOrReplaceStaticRoute(CreateProxyRoute());
        RouteSelector selector = new RouteSelector(
            CreateProxyNodeConfiguration(),
            table);
        DiameterRouteRequest request = new DiameterRouteRequest();
        request.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        request.Message.CommandCode = 300U;
        request.Message.ApplicationId = 4U;
        request.Message.HopByHopIdentifier = 501U;
        request.Message.EndToEndIdentifier = 502U;
        request.ApplicationId = 4U;
        request.SourcePeer = CreateIdentity("source.example");
        request.DestinationRealm = CreateIdentity("remote.example");
        request.Message.Avps.Add(CreateProxyInformationAvp(
            "first.proxy.example",
            new byte[] { 1, 2 }));
        request.Message.Avps.Add(CreateProxyInformationAvp(
            "second.proxy.example",
            new byte[] { 3, 4 }));
        List<DiameterPeerSnapshot> peers = new List<DiameterPeerSnapshot>();
        peers.Add(CreateProxyPeer());
        result.Decision = selector.SelectRoute(request, peers);
        return result;
    }

    private static DiameterNodeConfiguration CreateProxyNodeConfiguration()
    {
        DiameterNodeConfiguration result = new DiameterNodeConfiguration();
        result.OriginHost.Value = "node.local.example";
        result.OriginRealm.Value = "local.example";
        result.ProductName = "diameter.Tests";
        result.Roles = DiameterNodeRole.Proxy;
        result.Security.AllowUnsecuredTransport = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Applications.Add(application);
        return result;
    }

    private static DiameterRouteConfiguration CreateProxyRoute()
    {
        DiameterRouteConfiguration result = new DiameterRouteConfiguration();
        result.RouteIdentifier = "proxy-route";
        result.DestinationRealm.Value = "remote.example";
        result.ApplicationId = 4U;
        result.PeerIdentity = CreateIdentity("destination.example");
        result.Action = DiameterRouteAction.Proxy;
        result.AgentRole = DiameterAgentRole.Proxy;
        return result;
    }

    private static DiameterPeerSnapshot CreateProxyPeer()
    {
        DiameterPeerSnapshot result = new DiameterPeerSnapshot();
        result.Configuration.Identity = CreateIdentity("destination.example");
        result.Available = true;
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = 4U;
        result.Capabilities.CommonApplications.Add(application);
        result.Connection = CreateConnection(
            "destination-connection",
            "destination.example",
            3869);
        return result;
    }

    private static DiameterConnectionMetadata CreateConnection(
        string connectionId,
        string remoteIdentity,
        int remotePort)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = connectionId;
        result.LocalEndpoint.Host = "node.local.example";
        result.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        result.LocalEndpoint.Port = 3868;
        result.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        result.RemoteEndpoint.Identity.Value = remoteIdentity;
        result.RemoteEndpoint.Host = remoteIdentity;
        result.RemoteEndpoint.Address = IPAddress.Parse("192.0.2.2");
        result.RemoteEndpoint.Port = remotePort;
        result.RemoteEndpoint.Transport = DiameterTransportKind.Tcp;
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Open = true;
        return result;
    }

    private static DiameterAvp CreateProxyInformationAvp(
        string proxyHost,
        byte[] proxyState)
    {
        DiameterProxyInformation information =
            new DiameterProxyInformation();
        information.ProxyHost.Value = proxyHost;
        information.ProxyState = proxyState;
        return new DiameterBaseAvpFactory().CreateProxyInfoAvp(information);
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static List<string> ReadTextAvps(
        DiameterMessage message,
        uint code)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf && avp.Code == code)
            {
                result.Add(Encoding.UTF8.GetString(avp.Data));
            }

            index = index + 1;
        }

        return result;
    }

    private static DiameterMessage CreateProxyAnswer(
        DiameterMessage request,
        bool error,
        uint resultCode)
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
        DiameterAvp resultAvp = new DiameterAvp();
        resultAvp.Code = DiameterAvpCodes.ResultCode;
        resultAvp.Flags = DiameterAvpFlags.Mandatory;
        resultAvp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(resultAvp.Data, resultCode);
        result.Avps.Add(resultAvp);
        int index = 0;
        while (index < request.Avps.Count)
        {
            DiameterAvp avp = request.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ProxyInfo)
            {
                result.Avps.Add(CloneAvp(avp));
            }

            index = index + 1;
        }

        DiameterAvp trailing = new DiameterAvp();
        trailing.Code = DiameterAvpCodes.ErrorMessage;
        trailing.Data = Encoding.UTF8.GetBytes("unchanged trailing AVP");
        result.Avps.Add(trailing);
        return result;
    }

    private static List<DiameterAvp> ReadProxyInformationAvps(
        DiameterMessage message)
    {
        List<DiameterAvp> result = new List<DiameterAvp>();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.VendorId == DiameterVendorIds.Ietf &&
                avp.Code == DiameterAvpCodes.ProxyInfo)
            {
                result.Add(avp);
            }

            index = index + 1;
        }

        return result;
    }

    private static List<DiameterProxyInformation> DecodeProxyInformation(
        List<DiameterAvp> avps)
    {
        List<DiameterProxyInformation> result =
            new List<DiameterProxyInformation>();
        int index = 0;
        while (index < avps.Count)
        {
            DiameterValueResult decoded =
                GroupedValueCodec.DecodeGroupedValue(
                    avps[index].Data,
                    null,
                    new DiameterLimitConfiguration());
            Assert.That(decoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            DiameterProxyInformation information =
                new DiameterProxyInformation();
            int childIndex = 0;
            while (childIndex < decoded.Avps.Count)
            {
                DiameterAvp child = decoded.Avps[childIndex];
                if (child.Code == DiameterAvpCodes.ProxyHost)
                {
                    information.ProxyHost.Value =
                        Encoding.UTF8.GetString(child.Data);
                }
                else if (child.Code == DiameterAvpCodes.ProxyState)
                {
                    information.ProxyState = new byte[child.Data.Length];
                    Array.Copy(
                        child.Data,
                        information.ProxyState,
                        child.Data.Length);
                }

                childIndex = childIndex + 1;
            }

            result.Add(information);
            index = index + 1;
        }

        return result;
    }

    private static void TamperLastProxyState(DiameterMessage message)
    {
        List<DiameterAvp> proxies = ReadProxyInformationAvps(message);
        DiameterAvp proxy = proxies[proxies.Count - 1];
        DiameterValueResult decoded =
            GroupedValueCodec.DecodeGroupedValue(
                proxy.Data,
                null,
                new DiameterLimitConfiguration());
        int index = 0;
        while (index < decoded.Avps.Count)
        {
            if (decoded.Avps[index].Code == DiameterAvpCodes.ProxyState)
            {
                decoded.Avps[index].Data[10] =
                    (byte)(decoded.Avps[index].Data[10] ^ 255);
            }

            index = index + 1;
        }

        DiameterValueResult measurement =
            GroupedValueCodec.MeasureGroupedValue(
                decoded.Avps,
                null,
                new DiameterLimitConfiguration());
        proxy.Data = new byte[measurement.RequiredCount];
        DiameterValueResult encoded =
            GroupedValueCodec.EncodeGroupedValue(
                proxy.Data,
                decoded.Avps,
                null,
                new DiameterLimitConfiguration());
        Assert.That(encoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
    }

    private static void RemoveLastProxyInformation(DiameterMessage message)
    {
        int index = message.Avps.Count - 1;
        while (index >= 0)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == DiameterAvpCodes.ProxyInfo)
            {
                message.Avps.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    private static uint ReadUnsigned32Avp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].Code == code)
            {
                return NetworkByteOrder.ReadUInt32(
                    message.Avps[index].Data);
            }

            index = index + 1;
        }

        throw new AssertionException("Expected Unsigned32 AVP was not present.");
    }

    private static DiameterAvp CloneAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.Flags = source.Flags;
        result.VendorId = source.VendorId;
        result.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, result.Data, source.Data.Length);
        return result;
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
}

internal class ProxyAgentTestContext
{
    internal byte[] ProtectionKey = new byte[0];
    internal ProxyAgentIdentifierSource Identifiers =
        new ProxyAgentIdentifierSource();
    internal ProxyAgent Agent = null!;
    internal DiameterRouteDecision Decision = new DiameterRouteDecision();
    internal DiameterConnectionMetadata SourceConnection =
        new DiameterConnectionMetadata();
}

internal class ProxyAgentIdentifierSource : IDiameterIdentifierSource
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
