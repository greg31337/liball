namespace sip.Tests;

public class SipDnsOrderingAndCacheTests
{
    [Test]
    public void OrdersSrvPriorityGroupsUsingDeterministicWeightDraws()
    {
        SipDnsWireRecord zero = SrvRecord("zero.example", 10, 0);
        SipDnsWireRecord light = SrvRecord("light.example", 10, 1);
        SipDnsWireRecord heavy = SrvRecord("heavy.example", 10, 3);
        SipDnsWireRecord later = SrvRecord("later.example", 20, 65535);
        SequenceRandomSource random = new SequenceRandomSource(new byte[] { 2, 0, 0, 0 });

        SipDnsWireRecord[] ordered = SipSrvOrdering.Order(
            new SipDnsWireRecord[] { later, light, zero, heavy }, random);

        Assert.That(ordered.Select(record => record.Target), Is.EqualTo(new string[]
        {
            "heavy.example", "zero.example", "light.example", "later.example"
        }));
    }

    [Test]
    public void InterleavesIpv4AndIpv6CandidatesAndUsesInjectedClockForTtls()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, 2, (query, name, type) => type == 1
            ? AddressResponse(query, 20, new byte[][]
            {
                new byte[] { 192, 0, 2, 1 }, new byte[] { 192, 0, 2, 2 }
            })
            : AddressResponse(query, 30, new byte[][]
            {
                Ipv6(1), Ipv6(2)
            }));
        SipManualMonotonicClock clock = new SipManualMonotonicClock(1000);
        SipDnsResolver resolver = Resolver(clock);

        SipResolutionResult result = resolver.ResolveTarget(
            ExplicitPortRequest("families.example", 5090), Options(LocalPort(server)));
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "192.0.2.1", "2001:db8::1", "192.0.2.2", "2001:db8::2" }));
            Assert.That(result.Candidates.Select(candidate => candidate.RecordKind),
                Is.EqualTo(new SipDnsRecordKind[]
                {
                    SipDnsRecordKind.A, SipDnsRecordKind.Aaaa,
                    SipDnsRecordKind.A, SipDnsRecordKind.Aaaa
                }));
            Assert.That(result.Candidates[0].ExpiresAtMilliseconds, Is.EqualTo(21000));
            Assert.That(result.Candidates[1].ExpiresAtMilliseconds, Is.EqualTo(31000));
        });
    }

    [Test]
    public void CachesPositiveResultsUntilTtlAndFlushesDefensiveCopies()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        int queryCount = 0;
        Task responder = Serve(server, 6, (query, name, type) =>
        {
            System.Threading.Interlocked.Increment(ref queryCount);
            return type == 1 ? AddressResponse(query, 10,
                new byte[][] { new byte[] { 198, 51, 100, 10 } }) : EmptyResponse(query);
        });
        SipManualMonotonicClock clock = new SipManualMonotonicClock(5000);
        SipDnsResolver resolver = Resolver(clock);
        SipDnsOptions options = Options(LocalPort(server));
        SipResolutionRequest firstRequest = ExplicitPortRequest("cache.example", 5070);
        firstRequest.CorrelationId = "first";

        SipResolutionResult first = resolver.ResolveTarget(firstRequest, options);
        first.Candidates[0].Host = "mutated.example";
        SipResolutionRequest cachedRequest = ExplicitPortRequest("cache.example", 5070);
        cachedRequest.CorrelationId = "cached";
        SipResolutionResult cached = resolver.ResolveTarget(cachedRequest, options);
        clock.AdvanceMilliseconds(10001);
        SipResolutionResult expired = resolver.ResolveTarget(
            ExplicitPortRequest("cache.example", 5070), options);
        resolver.FlushResolverCache();
        SipResolutionResult flushed = resolver.ResolveTarget(
            ExplicitPortRequest("cache.example", 5070), options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(cached.Candidates.Single().Host, Is.EqualTo("198.51.100.10"));
            Assert.That(cached.CorrelationId, Is.EqualTo("cached"));
            Assert.That(expired.Candidates.Single().Host, Is.EqualTo("198.51.100.10"));
            Assert.That(flushed.Candidates.Single().Host, Is.EqualTo("198.51.100.10"));
            Assert.That(queryCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void CachesNegativeResultsForBoundedLifetime()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        int queryCount = 0;
        Task responder = Serve(server, 4, (query, name, type) =>
        {
            System.Threading.Interlocked.Increment(ref queryCount);
            return EmptyResponse(query);
        });
        SipManualMonotonicClock clock = new SipManualMonotonicClock(100);
        SipDnsResolver resolver = Resolver(clock);
        SipDnsOptions options = Options(LocalPort(server));

        SipResolutionResult first = resolver.ResolveTarget(
            ExplicitPortRequest("absent.example", 5060), options);
        SipResolutionResult cached = resolver.ResolveTarget(
            ExplicitPortRequest("absent.example", 5060), options);
        clock.AdvanceMilliseconds(30001);
        SipResolutionResult expired = resolver.ResolveTarget(
            ExplicitPortRequest("absent.example", 5060), options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(SipResolutionStatus.NoRecords));
            Assert.That(cached.Status, Is.EqualTo(SipResolutionStatus.NoRecords));
            Assert.That(expired.Status, Is.EqualTo(SipResolutionStatus.NoRecords));
            Assert.That(queryCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void ReportedFailuresMoveBehindCachedAlternativesUntilFlush()
    {
        using System.Net.Sockets.UdpClient server = CreateServer();
        Task responder = Serve(server, 4, (query, name, type) => type == 1
            ? AddressResponse(query, 60, new byte[][]
            {
                new byte[] { 203, 0, 113, 1 }, new byte[] { 203, 0, 113, 2 }
            }) : EmptyResponse(query));
        SipDnsResolver resolver = Resolver(new SipManualMonotonicClock(1000));
        SipDnsOptions options = Options(LocalPort(server));

        SipResolutionResult first = resolver.ResolveTarget(
            ExplicitPortRequest("failover.example", 5060), options);
        resolver.ReportCandidateFailure(first.Candidates[0]);
        SipResolutionResult reordered = resolver.ResolveTarget(
            ExplicitPortRequest("failover.example", 5060), options);
        resolver.FlushResolverCache();
        SipResolutionResult reset = resolver.ResolveTarget(
            ExplicitPortRequest("failover.example", 5060), options);
        responder.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "203.0.113.1", "203.0.113.2" }));
            Assert.That(reordered.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "203.0.113.2", "203.0.113.1" }));
            Assert.That(reset.Candidates.Select(candidate => candidate.Host),
                Is.EqualTo(new string[] { "203.0.113.1", "203.0.113.2" }));
        });
    }

    static SipDnsResolver Resolver(SipManualMonotonicClock clock)
    {
        SipDnsResolver resolver = new SipDnsResolver();
        resolver.ConfigureResolutionSources(
            new SequenceRandomSource(new byte[] { 0 }), clock);
        return resolver;
    }

    static SipDnsWireRecord SrvRecord(string target, int priority, int weight)
    {
        SipDnsWireRecord record = new SipDnsWireRecord();
        record.Kind = SipDnsRecordKind.Srv;
        record.Target = target;
        record.Priority = priority;
        record.Weight = weight;
        return record;
    }

    static SipResolutionRequest ExplicitPortRequest(string domain, int port)
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.Host = domain;
        uri.Port = port;
        SipResolutionRequest request = new SipResolutionRequest();
        request.Uri = uri;
        request.Transport = SipTransportKind.Udp;
        return request;
    }

    static SipDnsOptions Options(int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = "127.0.0.1";
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Udp;
        SipDnsOptions options = new SipDnsOptions();
        options.NameServers = new SipEndpointAddress[] { endpoint };
        options.TimeoutMilliseconds = 1000;
        options.MaxAttempts = 1;
        options.MaxRecordCount = 16;
        options.EnableCache = true;
        return options;
    }

    static Task Serve(System.Net.Sockets.UdpClient server, int count,
        System.Func<byte[], string, int, byte[]> responseFactory)
    {
        return Task.Run(() =>
        {
            int index = 0;
            while (index < count)
            {
                System.Net.IPEndPoint remote = new System.Net.IPEndPoint(
                    System.Net.IPAddress.Loopback, 0);
                byte[] query = server.Receive(ref remote);
                byte[] response = responseFactory(query, QuestionName(query), QuestionType(query));
                server.Send(response, response.Length, remote);
                index++;
            }
        });
    }

    static System.Net.Sockets.UdpClient CreateServer()
    {
        return new System.Net.Sockets.UdpClient(
            new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
    }

    static int LocalPort(System.Net.Sockets.UdpClient server)
    {
        return ((System.Net.IPEndPoint)server.Client.LocalEndPoint!).Port;
    }

    static byte[] EmptyResponse(byte[] query)
    {
        return StartResponse(query, 0).ToArray();
    }

    static byte[] AddressResponse(byte[] query, uint ttl, byte[][] addresses)
    {
        List<byte> response = StartResponse(query, addresses.Length);
        int type = QuestionType(query);
        int index = 0;
        while (index < addresses.Length)
        {
            AddPointer(response, 12);
            AddUInt16(response, type);
            AddUInt16(response, 1);
            AddUInt32(response, ttl);
            AddUInt16(response, addresses[index].Length);
            response.AddRange(addresses[index]);
            index++;
        }
        return response.ToArray();
    }

    static List<byte> StartResponse(byte[] query, int answerCount)
    {
        List<byte> response = new List<byte>();
        response.Add(query[0]);
        response.Add(query[1]);
        AddUInt16(response, 0x8180);
        AddUInt16(response, 1);
        AddUInt16(response, answerCount);
        AddUInt16(response, 0);
        AddUInt16(response, 0);
        response.AddRange(query.Skip(12));
        return response;
    }

    static string QuestionName(byte[] query)
    {
        List<string> labels = new List<string>();
        int offset = 12;
        while (query[offset] != 0)
        {
            int length = query[offset++];
            labels.Add(System.Text.Encoding.ASCII.GetString(query, offset, length));
            offset += length;
        }
        return string.Join('.', labels);
    }

    static int QuestionType(byte[] query)
    {
        int offset = 12;
        while (query[offset] != 0)
        {
            offset += query[offset] + 1;
        }
        offset++;
        return (query[offset] << 8) | query[offset + 1];
    }

    static byte[] Ipv6(int last)
    {
        return new byte[]
        {
            0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, (byte)last
        };
    }

    static void AddPointer(List<byte> bytes, int offset)
    {
        bytes.Add((byte)(0xc0 | offset >> 8));
        bytes.Add((byte)offset);
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    class SequenceRandomSource : ISipRandomSource
    {
        byte[] values;
        int index;

        internal SequenceRandomSource(byte[] source)
        {
            values = source;
        }

        public void FillBytes(byte[] bytes)
        {
            System.Array.Clear(bytes);
            bytes[bytes.Length - 1] = values[index % values.Length];
            index++;
        }
    }
}
