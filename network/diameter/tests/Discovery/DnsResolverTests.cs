using System.IO;
using System.Net;
using System.Net.Sockets;

namespace diameter.Tests;

public class DnsResolverTests
{
    [Test]
    public void ResolverRejectsMissingServerAddress()
    {
        Assert.Throws<ArgumentException>(() =>
            new DiameterDnsResolver(null!, 53));
    }

    [Test]
    public void ResolverQueriesEverySupportedRecordTypeAndIgnoresWrongTransaction()
    {
        using UdpClient server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        server.Client.ReceiveTimeout = 5000;
        int port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;

        Task serverTask = Task.Run(() =>
        {
            int queryIndex = 0;
            while (queryIndex < 4)
            {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] query = server.Receive(ref remote);
                int recordType = DnsTestMessages.ReadQueryType(query);
                byte[] record = DnsTestMessages.BuildRecord(
                    recordType,
                    60,
                    DnsTestMessages.BuildRecordData(recordType));
                byte[] response = DnsTestMessages.BuildResponse(query, 0x8180, record);
                if (queryIndex == 0)
                {
                    byte[] unrelated = (byte[])response.Clone();
                    unrelated[1] = (byte)(unrelated[1] + 1);
                    server.Send(unrelated, unrelated.Length, remote);
                }

                server.Send(response, response.Length, remote);
                queryIndex++;
            }
        });

        IDnsResolver resolver = new DiameterDnsResolver(IPAddress.Loopback, port);
        DiameterDnsQueryResult naptr = resolver.ResolveDiameterNaptrRecords(
            "example",
            3000);
        DiameterDnsQueryResult srv = resolver.ResolveDiameterSrvRecords(
            "_diameter._tcp.example",
            3000);
        DiameterDnsQueryResult ipv4 = resolver.ResolveDiameterIpv4Records(
            "peer.example",
            3000);
        DiameterDnsQueryResult ipv6 = resolver.ResolveDiameterIpv6Records(
            "peer.example",
            3000);
        serverTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(naptr.Successful, Is.True);
            Assert.That(naptr.NaptrRecords, Has.Count.EqualTo(1));
            Assert.That(naptr.NaptrRecords[0].Service, Is.EqualTo("AAA+D2T"));
            Assert.That(srv.Successful, Is.True);
            Assert.That(srv.SrvRecords, Has.Count.EqualTo(1));
            Assert.That(srv.SrvRecords[0].Target, Is.EqualTo("peer.example"));
            Assert.That(ipv4.Successful, Is.True);
            Assert.That(ipv4.AddressRecords[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.10")));
            Assert.That(ipv6.Successful, Is.True);
            Assert.That(ipv6.AddressRecords[0].Address,
                Is.EqualTo(IPAddress.Parse("2001:db8::10")));
            Assert.That(naptr.UsedTcpFallback, Is.False);
            Assert.That(srv.UsedTcpFallback, Is.False);
        });
    }

    [Test]
    public void TruncatedUdpResponseFallsBackToLengthPrefixedTcp()
    {
        TcpListener tcpServer = new TcpListener(IPAddress.Loopback, 0);
        tcpServer.Start();
        int port = ((IPEndPoint)tcpServer.LocalEndpoint).Port;
        using UdpClient udpServer = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        udpServer.Client.ReceiveTimeout = 5000;

        Task udpTask = Task.Run(() =>
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            byte[] query = udpServer.Receive(ref remote);
            byte[] truncated = DnsTestMessages.BuildResponse(query, 0x8380);
            udpServer.Send(truncated, truncated.Length, remote);
        });

        Task tcpTask = Task.Run(() =>
        {
            using TcpClient client = tcpServer.AcceptTcpClient();
            using NetworkStream stream = client.GetStream();
            byte[] lengthBytes = ReadExact(stream, 2);
            int queryLength = (lengthBytes[0] << 8) | lengthBytes[1];
            byte[] query = ReadExact(stream, queryLength);
            byte[] record = DnsTestMessages.BuildRecord(
                DnsMessageCodec.Ipv4RecordType,
                90,
                new byte[] { 198, 51, 100, 7 });
            byte[] response = DnsTestMessages.BuildResponse(query, 0x8180, record);
            byte[] frame = new byte[response.Length + 2];
            frame[0] = (byte)(response.Length >> 8);
            frame[1] = (byte)response.Length;
            Buffer.BlockCopy(response, 0, frame, 2, response.Length);
            stream.Write(frame, 0, 1);
            stream.Write(frame, 1, frame.Length - 1);
        });

        IDnsResolver resolver = new DiameterDnsResolver(IPAddress.Loopback, port);
        DiameterDnsQueryResult result = resolver.ResolveDiameterIpv4Records(
            "peer.example",
            3000);
        udpTask.GetAwaiter().GetResult();
        tcpTask.GetAwaiter().GetResult();
        tcpServer.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True);
            Assert.That(result.UsedTcpFallback, Is.True);
            Assert.That(result.AddressRecords, Has.Count.EqualTo(1));
            Assert.That(result.AddressRecords[0].Address,
                Is.EqualTo(IPAddress.Parse("198.51.100.7")));
            Assert.That(result.AddressRecords[0].TimeToLiveSeconds, Is.EqualTo(90U));
        });
    }

    [Test]
    public void TcpFallbackRejectsInvalidLengthPrefix()
    {
        TcpListener tcpServer = new TcpListener(IPAddress.Loopback, 0);
        tcpServer.Start();
        int port = ((IPEndPoint)tcpServer.LocalEndpoint).Port;
        using UdpClient udpServer = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        udpServer.Client.ReceiveTimeout = 5000;

        Task udpTask = Task.Run(() =>
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            byte[] query = udpServer.Receive(ref remote);
            byte[] truncated = DnsTestMessages.BuildResponse(query, 0x8380);
            udpServer.Send(truncated, truncated.Length, remote);
        });

        Task tcpTask = Task.Run(() =>
        {
            using TcpClient client = tcpServer.AcceptTcpClient();
            using NetworkStream stream = client.GetStream();
            byte[] lengthBytes = ReadExact(stream, 2);
            int queryLength = (lengthBytes[0] << 8) | lengthBytes[1];
            ReadExact(stream, queryLength);
            stream.Write(new byte[] { 0, 11 }, 0, 2);
        });

        IDnsResolver resolver = new DiameterDnsResolver(IPAddress.Loopback, port);

        Assert.Throws<DiameterCodecException>(() =>
            resolver.ResolveDiameterIpv4Records("peer.example", 3000));
        udpTask.GetAwaiter().GetResult();
        tcpTask.GetAwaiter().GetResult();
        tcpServer.Stop();
    }

    [Test]
    public void ResolverValidatesConstructionQueriesAndTimeouts()
    {
        using UdpClient silentServer = new UdpClient(
            new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)silentServer.Client.LocalEndPoint!).Port;
        DiameterDnsResolver resolver = new DiameterDnsResolver(IPAddress.Loopback, port);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DiameterDnsResolver(IPAddress.Loopback, 0));
            Assert.Throws<ArgumentException>(() =>
                resolver.ResolveDiameterIpv4Records("", 100));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                resolver.ResolveDiameterIpv4Records("example", 0));
            Assert.Throws<DiameterPeerException>(() =>
                resolver.ResolveDiameterIpv4Records("example", 50));
        });
    }

    private static byte[] ReadExact(NetworkStream stream, int count)
    {
        byte[] result = new byte[count];
        int offset = 0;
        while (offset < result.Length)
        {
            int received = stream.Read(result, offset, result.Length - offset);
            if (received == 0)
            {
                throw new EndOfStreamException();
            }

            offset += received;
        }

        return result;
    }
}
