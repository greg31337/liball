using System.Net;

namespace diameter.Tests;

public class InProcessDiameterHarnessTests
{
    [Test]
    public void TwoNodeTransportControlsFragmentLossDelayDuplicateAndReconnect()
    {
        InProcessDiameterHarness harness = new InProcessDiameterHarness();
        harness.StartDiameterNodes();
        try
        {
            InProcessHarnessLink link = harness.ConnectFirstToSecond();
            harness.Network.MaximumFragmentSize = 3;

            DiameterTransportIoResult fragmentedSend = link.Outbound
                .SendDiameterConnectionBytes(
                    new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                    1000);
            byte[] fragmentOne = ReceiveBytes(link.Inbound, 3);
            byte[] fragmentTwo = ReceiveBytes(link.Inbound, 3);
            byte[] fragmentThree = ReceiveBytes(link.Inbound, 3);

            harness.Network.DropNextDelivery = true;
            DiameterTransportIoResult droppedSend = link.Outbound
                .SendDiameterConnectionBytes(new byte[] { 9 }, 1000);
            DiameterTransportIoResult droppedReceive = link.Inbound
                .ReceiveDiameterConnectionBytes(new byte[1], 5);

            harness.Network.MaximumFragmentSize = 0;
            harness.Network.DeliveryDelayMilliseconds = 100;
            link.Outbound.SendDiameterConnectionBytes(
                new byte[] { 10, 11 },
                1000);
            DiameterTransportIoResult earlyReceive = link.Inbound
                .ReceiveDiameterConnectionBytes(new byte[2], 5);
            harness.AdvanceDiameterTime(99);
            DiameterTransportIoResult stillEarlyReceive = link.Inbound
                .ReceiveDiameterConnectionBytes(new byte[2], 5);
            harness.AdvanceDiameterTime(1);
            byte[] delayed = ReceiveBytes(link.Inbound, 2);

            harness.Network.DeliveryDelayMilliseconds = 0;
            harness.Network.DuplicateNextDelivery = true;
            link.Outbound.SendDiameterConnectionBytes(
                new byte[] { 12, 13 },
                1000);
            byte[] duplicateOne = ReceiveBytes(link.Inbound, 2);
            byte[] duplicateTwo = ReceiveBytes(link.Inbound, 2);

            string firstConnectionId = link.Outbound
                .ReadDiameterConnectionMetadata().ConnectionId;
            link.Outbound.CloseDiameterConnection();
            DiameterTransportIoResult closedPeerReceive = link.Inbound
                .ReceiveDiameterConnectionBytes(new byte[1], 5);
            InProcessHarnessLink reconnected =
                harness.ConnectFirstToSecond();
            string secondConnectionId = reconnected.Outbound
                .ReadDiameterConnectionMetadata().ConnectionId;

            Assert.Multiple(() =>
            {
                Assert.That(fragmentedSend.Status,
                    Is.EqualTo(DiameterTransportStatus.Success));
                Assert.That(fragmentedSend.TransferredCount, Is.EqualTo(8));
                Assert.That(fragmentOne, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(fragmentTwo, Is.EqualTo(new byte[] { 4, 5, 6 }));
                Assert.That(fragmentThree, Is.EqualTo(new byte[] { 7, 8 }));
                Assert.That(droppedSend.Status,
                    Is.EqualTo(DiameterTransportStatus.Success));
                Assert.That(droppedReceive.Status,
                    Is.EqualTo(DiameterTransportStatus.TimedOut));
                Assert.That(earlyReceive.Status,
                    Is.EqualTo(DiameterTransportStatus.TimedOut));
                Assert.That(stillEarlyReceive.Status,
                    Is.EqualTo(DiameterTransportStatus.TimedOut));
                Assert.That(delayed, Is.EqualTo(new byte[] { 10, 11 }));
                Assert.That(duplicateOne,
                    Is.EqualTo(new byte[] { 12, 13 }));
                Assert.That(duplicateTwo,
                    Is.EqualTo(new byte[] { 12, 13 }));
                Assert.That(closedPeerReceive.Status,
                    Is.EqualTo(DiameterTransportStatus.Closed));
                Assert.That(secondConnectionId,
                    Is.Not.EqualTo(firstConnectionId));
                Assert.That(harness.Network.ConnectionCount, Is.EqualTo(2));
            });

            reconnected.Outbound.CloseDiameterConnection();
        }
        finally
        {
            harness.StopDiameterNodes();
        }
    }

    [Test]
    public void TwoNodeHarnessControlsClockDnsAndIsolatedStores()
    {
        InProcessDiameterHarness harness = new InProcessDiameterHarness();
        DateTimeOffset initialUtc = harness.Clock.ReadUtcTime();
        DiameterDnsQueryResult supplied = CreateDnsResult();
        harness.Dns.SetDiameterNaptrResult("example", supplied);
        harness.Dns.SetDiameterSrvResult("_diameter._tcp.example", supplied);
        harness.Dns.SetDiameterIpv4Result("peer.example", supplied);
        harness.Dns.SetDiameterIpv6Result("peer.example", supplied);
        supplied.NaptrRecords[0].Replacement = "mutated.example";
        supplied.SrvRecords[0].Target = "mutated.example";
        supplied.AddressRecords[0].Name = "mutated.example";

        DiameterDnsQueryResult naptr = harness.Dns
            .ResolveDiameterNaptrRecords("example", 1000);
        DiameterDnsQueryResult srv = harness.Dns
            .ResolveDiameterSrvRecords("_diameter._tcp.example", 1000);
        DiameterDnsQueryResult ipv4 = harness.Dns
            .ResolveDiameterIpv4Records("peer.example", 1000);
        DiameterDnsQueryResult ipv6 = harness.Dns
            .ResolveDiameterIpv6Records("peer.example", 1000);
        Assert.That(naptr.NaptrRecords[0].Replacement,
            Is.EqualTo("peer.example"));
        naptr.NaptrRecords[0].Replacement = "caller-change.example";
        DiameterDnsQueryResult retainedNaptr = harness.Dns
            .ResolveDiameterNaptrRecords("example", 1000);
        DiameterDnsQueryResult missing = harness.Dns
            .ResolveDiameterSrvRecords("missing.example", 1000);

        harness.AdvanceDiameterTime(1250);
        harness.StartDiameterNodes();
        try
        {
            NodeComposition firstComposition = harness.First.Node.Lifecycle
                .ReadDiameterNodeComposition();
            NodeComposition secondComposition = harness.Second.Node.Lifecycle
                .ReadDiameterNodeComposition();
            DiameterSessionConfiguration session =
                CreateSessionConfiguration();
            harness.First.Node.ReadDiameterSessionManager()
                .CreateDiameterSession(session);

            Assert.Multiple(() =>
            {
                Assert.That(harness.Clock.ReadUtcTime(),
                    Is.EqualTo(initialUtc.AddMilliseconds(1250)));
                Assert.That(harness.Clock.ReadMonotonicMilliseconds(),
                    Is.EqualTo(1250));
                Assert.That(retainedNaptr.NaptrRecords[0].Replacement,
                    Is.EqualTo("peer.example"));
                Assert.That(srv.SrvRecords[0].Target,
                    Is.EqualTo("peer.example"));
                Assert.That(ipv4.AddressRecords[0].Name,
                    Is.EqualTo("peer.example"));
                Assert.That(ipv6.AddressRecords[1].Address,
                    Is.EqualTo(IPAddress.Parse("2001:db8::1")));
                Assert.That(missing.ResponseCode, Is.EqualTo(3));
                Assert.That(missing.Successful, Is.False);
                Assert.That(firstComposition.Clock,
                    Is.SameAs(harness.Clock));
                Assert.That(secondComposition.Clock,
                    Is.SameAs(harness.Clock));
                Assert.That(firstComposition.SessionStore,
                    Is.SameAs(harness.First.SessionStore));
                Assert.That(secondComposition.SessionStore,
                    Is.SameAs(harness.Second.SessionStore));
                Assert.That(harness.First.SessionStore.LoadDiameterSession(
                    "first.example;harness"), Is.Not.Null);
                Assert.That(harness.Second.SessionStore.LoadDiameterSession(
                    "first.example;harness"), Is.Null);
                Assert.That(harness.First.AccountingStore,
                    Is.Not.SameAs(harness.Second.AccountingStore));
                Assert.That(harness.First.CreditControlStore,
                    Is.Not.SameAs(harness.Second.CreditControlStore));
                Assert.That(harness.First.DuplicateStore,
                    Is.Not.SameAs(harness.Second.DuplicateStore));
            });
        }
        finally
        {
            harness.StopDiameterNodes();
        }
    }

    private static byte[] ReceiveBytes(
        IDiameterConnection connection,
        int capacity)
    {
        byte[] buffer = new byte[capacity];
        DiameterTransportIoResult received = connection
            .ReceiveDiameterConnectionBytes(buffer, 1000);
        Assert.That(received.Status,
            Is.EqualTo(DiameterTransportStatus.Success));
        return buffer.AsSpan(0, received.TransferredCount).ToArray();
    }

    private static DiameterDnsQueryResult CreateDnsResult()
    {
        DiameterDnsQueryResult result = new DiameterDnsQueryResult();
        result.QueryName = "example";
        result.Successful = true;
        result.Authoritative = true;
        result.Code = "dns-success";
        DiameterNaptrRecord naptr = new DiameterNaptrRecord();
        naptr.Name = "example";
        naptr.Order = 10;
        naptr.Preference = 20;
        naptr.Flags = "S";
        naptr.Service = "AAA+D2T";
        naptr.Replacement = "peer.example";
        naptr.TimeToLiveSeconds = 60;
        result.NaptrRecords.Add(naptr);
        DiameterSrvRecord srv = new DiameterSrvRecord();
        srv.Name = "_diameter._tcp.example";
        srv.Priority = 10;
        srv.Weight = 20;
        srv.Port = DiameterProtocolConstants.Port;
        srv.Target = "peer.example";
        srv.TimeToLiveSeconds = 60;
        result.SrvRecords.Add(srv);
        DiameterAddressRecord ipv4 = new DiameterAddressRecord();
        ipv4.Name = "peer.example";
        ipv4.Address = IPAddress.Loopback;
        ipv4.TimeToLiveSeconds = 60;
        result.AddressRecords.Add(ipv4);
        DiameterAddressRecord ipv6 = new DiameterAddressRecord();
        ipv6.Name = "peer-v6.example";
        ipv6.Address = IPAddress.Parse("2001:db8::1");
        ipv6.TimeToLiveSeconds = 60;
        result.AddressRecords.Add(ipv6);
        return result;
    }

    private static DiameterSessionConfiguration CreateSessionConfiguration()
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = "first.example;harness";
        result.ApplicationId = 4;
        result.DestinationRealm = new DiameterIdentity();
        result.DestinationRealm.Value = "example";
        result.PeerIdentity = new DiameterIdentity();
        result.PeerIdentity.Value = "second.example";
        result.UserName = "harness@example";
        return result;
    }
}
