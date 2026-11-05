using System.Net;
using System.Text;

namespace jmap.Tests;

public class JmapDnsServiceLocatorTests
{
    [Test]
    public async Task DirectHttpsUrlBypassesDnsAndIsReturnedUnchanged()
    {
        FakeDnsQueryClient queries = new FakeDnsQueryClient();
        JmapDnsServiceLocator locator = Locator(queries);
        JmapDiscoveryOptions options = new JmapDiscoveryOptions
        {
            SessionUrl = "https://jmap.example.test/custom?tenant=one"
        };

        string result = await locator.LocateSessionUrlAsync(options, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(options.SessionUrl));
            Assert.That(queries.CallCount, Is.Zero);
        }
    }

    [Test]
    public void DirectUrlAndDiscoveryInputsAreValidatedBeforeLookup()
    {
        JmapDnsServiceLocator locator = new JmapDnsServiceLocator();
        JmapDiscoveryOptions insecure = new JmapDiscoveryOptions { SessionUrl = "http://example.test/session" };
        JmapDiscoveryOptions fragmented = new JmapDiscoveryOptions { SessionUrl = "https://example.test/session#fragment" };
        JmapDiscoveryOptions missingDomain = new JmapDiscoveryOptions { Username = "local-only" };
        JmapDiscoveryOptions invalidTimeout = new JmapDiscoveryOptions
        {
            Username = "user@example.test",
            DnsTimeoutMilliseconds = 0
        };

        JmapValidationException insecureError = Assert.Throws<JmapValidationException>(
            () => locator.LocateSessionUrlAsync(insecure, CancellationToken.None))!;
        JmapValidationException fragmentError = Assert.Throws<JmapValidationException>(
            () => locator.LocateSessionUrlAsync(fragmented, CancellationToken.None))!;
        JmapValidationException usernameError = Assert.Throws<JmapValidationException>(
            () => locator.LocateSessionUrlAsync(missingDomain, CancellationToken.None))!;
        JmapValidationException timeoutError = Assert.Throws<JmapValidationException>(
            () => locator.LocateSessionUrlAsync(invalidTimeout, CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(insecureError.InvalidProperties, Is.EqualTo(new[] { "sessionUrl" }));
            Assert.That(fragmentError.InvalidProperties, Is.EqualTo(new[] { "sessionUrl" }));
            Assert.That(usernameError.InvalidProperties, Is.EqualTo(new[] { "username" }));
            Assert.That(timeoutError.InvalidProperties, Is.EqualTo(new[] { "dnsTimeoutMilliseconds" }));
        }
    }

    [Test]
    public void DiscoveryRejectsMissingAndMalformedDomainsAndEndpoints()
    {
        JmapDnsServiceLocator locator = new JmapDnsServiceLocator();
        string oversizedDomain = string.Join(".", Enumerable.Repeat("abcdefghij", 24));
        string oversizedIdnaDomain = string.Join(".", Enumerable.Repeat(new string('\u00fc', 49), 5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => locator.LocateSessionUrlAsync(null!, CancellationToken.None),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.RequireDirectSessionUrl(new JmapDiscoveryOptions()),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.RequireUsernameDomain(null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.RequireUsernameDomain("user@\ud800.example"),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(locator.RequireUsernameDomain("user@Example.Test."),
                Is.EqualTo("example.test"));
            Assert.That(() => locator.RequireUsernameDomain("user@."),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.RequireUsernameDomain("user@" + oversizedDomain),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.RequireUsernameDomain("user@" + oversizedIdnaDomain),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => locator.BuildSessionUrl("", 443),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => locator.BuildSessionUrl("bad host", 443),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => locator.BuildSessionUrl("example.test", -1),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => locator.BuildSessionUrl("example.test", 65536),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void DnsFailureCodesAndMixedRootTargetsAreRejected()
    {
        string queryName = "_jmap._tcp.example.test";
        FakeDnsQueryClient failedQueries = new FakeDnsQueryClient
        {
            Response = Response(queryName, 2)
        };
        FakeDnsQueryClient mixedQueries = new FakeDnsQueryClient
        {
            Response = Response(
                queryName, 0,
                new SrvValue(0, 0, 443, "."),
                new SrvValue(0, 0, 443, "jmap.example.test"))
        };

        JmapTransportException dnsFailure = Assert.ThrowsAsync<JmapTransportException>(async () =>
            await Locator(failedQueries).LocateSessionUrlAsync(
                new JmapDiscoveryOptions { Username = "user@example.test" },
                CancellationToken.None))!;
        JmapProtocolException mixedRoot = Assert.ThrowsAsync<JmapProtocolException>(async () =>
            await Locator(mixedQueries).LocateSessionUrlAsync(
                new JmapDiscoveryOptions { Username = "user@example.test" },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dnsFailure.IsRetryable, Is.True);
            Assert.That(dnsFailure.Endpoint, Is.EqualTo(queryName));
            Assert.That(mixedRoot.Path, Is.EqualTo("dns"));
        }
    }

    [Test]
    public async Task MissingSrvRecordUsesIdnaDomainWellKnownFallback()
    {
        FakeDnsQueryClient queries = new FakeDnsQueryClient();
        queries.Response = Response("_jmap._tcp.xn--bcher-kva.example", 3);
        JmapDnsServiceLocator locator = Locator(queries);
        JmapDiscoveryOptions options = new JmapDiscoveryOptions
        {
            Username = "reader@b\u00fccher.example",
            DnsTimeoutMilliseconds = 4321
        };
        CancellationTokenSource cancellation = new CancellationTokenSource();

        string result = await locator.LocateSessionUrlAsync(options, cancellation.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo("https://xn--bcher-kva.example/.well-known/jmap"));
            Assert.That(queries.QueryName, Is.EqualTo("_jmap._tcp.xn--bcher-kva.example"));
            Assert.That(queries.TimeoutMilliseconds, Is.EqualTo(4321));
            Assert.That(queries.CancellationToken, Is.EqualTo(cancellation.Token));
        }
    }

    [Test]
    public async Task LowestPrioritySrvTargetProducesHttpsUrlWithAdvertisedPort()
    {
        string queryName = "_jmap._tcp.example.test";
        FakeDnsQueryClient queries = new FakeDnsQueryClient();
        queries.Response = Response(
            queryName,
            0,
            new SrvValue(20, 100, 443, "later.example.test"),
            new SrvValue(5, 0, 8443, "preferred.example.test"));
        JmapDnsServiceLocator locator = Locator(queries);

        string result = await locator.LocateSessionUrlAsync(
            new JmapDiscoveryOptions { Username = "user@example.test" },
            CancellationToken.None);

        Assert.That(result, Is.EqualTo("https://preferred.example.test:8443/.well-known/jmap"));
    }

    [Test]
    public void SrvWeightSelectionGivesZeroWeightItsSpecifiedSmallChance()
    {
        DnsSrvRecord zero = Record(0, 0, 443, "zero.example.test");
        DnsSrvRecord weighted = Record(0, 10, 443, "weighted.example.test");
        DnsSrvRecord lowerPreference = Record(1, 65535, 443, "later.example.test");
        List<DnsSrvRecord> records = new List<DnsSrvRecord> { weighted, lowerPreference, zero };
        DnsSrvSelector selector = new DnsSrvSelector();
        selector.Randomizer = new FixedRandom(0);

        DnsSrvRecord first = selector.SelectRecord(records)!;
        selector.Randomizer = new FixedRandom(1);
        DnsSrvRecord second = selector.SelectRecord(records)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.SameAs(zero));
            Assert.That(second, Is.SameAs(weighted));
        }
    }

    [Test]
    public void ParserExpandsNamesContainingCompressionPointers()
    {
        List<byte> bytes = new List<byte>();
        AddName(bytes, "example.test");
        int compressedOffset = bytes.Count;
        bytes.Add(4);
        bytes.AddRange(Encoding.ASCII.GetBytes("jmap"));
        bytes.Add(192);
        bytes.Add(0);
        DnsSrvParser parser = new DnsSrvParser();

        DnsNameResult result = parser.ReadName(bytes.ToArray(), compressedOffset);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo("jmap.example.test"));
            Assert.That(result.NextOffset, Is.EqualTo(bytes.Count));
        }
    }

    [Test]
    public void ParserRejectsCyclicOrForwardCompressionPointers()
    {
        byte[] message = new byte[14];
        message[2] = 128;
        message[7] = 1;
        message[12] = 192;
        message[13] = 12;

        JmapProtocolException error = Assert.Throws<JmapProtocolException>(
            () => new DnsSrvParser().ParseSrvResponse(message))!;

        Assert.That(error.Path, Is.EqualTo("dns"));
    }

    [Test]
    public void SoleRootSrvTargetReportsServiceUnavailableInsteadOfFallingBack()
    {
        string queryName = "_jmap._tcp.example.test";
        FakeDnsQueryClient queries = new FakeDnsQueryClient();
        queries.Response = Response(queryName, 0, new SrvValue(0, 0, 0, "."));
        JmapDnsServiceLocator locator = Locator(queries);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await locator.LocateSessionUrlAsync(
                new JmapDiscoveryOptions { Username = "user@example.test" },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Endpoint, Is.EqualTo(queryName));
            Assert.That(error.IsRetryable, Is.False);
        }
    }

    [Test]
    public async Task TruncatedUdpResponseIsRetriedOverTcpWithinOriginalTimeout()
    {
        FakeDnsPacketExchange exchange = new FakeDnsPacketExchange();
        exchange.UdpResponse = Header(0, 33536);
        exchange.TcpResponse = Header(0, 33152);
        DnsQueryClient queryClient = new DnsQueryClient();
        queryClient.DnsServerOverride = IPAddress.Loopback;
        queryClient.Exchange = exchange;

        byte[] result = await queryClient.QuerySrvAsync(
            "_jmap._tcp.example.test",
            2000,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[2] & 2, Is.Zero);
            Assert.That(exchange.UdpCalls, Is.EqualTo(1));
            Assert.That(exchange.TcpCalls, Is.EqualTo(1));
            Assert.That(exchange.TcpTimeoutMilliseconds, Is.GreaterThan(0).And.LessThanOrEqualTo(2000));
            Assert.That(exchange.QueryType, Is.EqualTo(33));
            Assert.That(exchange.QueryClass, Is.EqualTo(1));
        }
    }

    [Test]
    public void CanceledDnsLookupPropagatesCancellation()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        DnsQueryClient queryClient = new DnsQueryClient();
        queryClient.DnsServerOverride = IPAddress.Loopback;
        queryClient.Exchange = new FakeDnsPacketExchange();

        Task<byte[]> query = queryClient.QuerySrvAsync("_jmap._tcp.example.test", 2000, cancellation.Token);

        Assert.That(async () => await query, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void LocatorReturnsCanceledTaskWhenDomainLookupIsCanceled()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        FakeDnsQueryClient queries = new FakeDnsQueryClient();
        queries.Cancel = true;
        JmapDnsServiceLocator locator = Locator(queries);

        Task<string> lookup = locator.LocateSessionUrlAsync(
            new JmapDiscoveryOptions { Username = "user@example.test" },
            cancellation.Token);

        Assert.That(async () => await lookup, Throws.InstanceOf<OperationCanceledException>());
        Assert.That(lookup.IsCanceled, Is.True);
    }

    private static JmapDnsServiceLocator Locator(FakeDnsQueryClient queries)
    {
        JmapDnsServiceLocator locator = new JmapDnsServiceLocator();
        locator.Queries = queries;
        return locator;
    }

    private static DnsSrvRecord Record(int priority, int weight, int port, string target)
    {
        return new DnsSrvRecord
        {
            Priority = priority,
            Weight = weight,
            Port = port,
            Target = target
        };
    }

    private static byte[] Response(string queryName, int responseCode, params SrvValue[] records)
    {
        List<byte> bytes = new List<byte>();
        AddUnsignedShort(bytes, 1234);
        AddUnsignedShort(bytes, 33152 | responseCode);
        AddUnsignedShort(bytes, 1);
        AddUnsignedShort(bytes, records.Length);
        AddUnsignedShort(bytes, 0);
        AddUnsignedShort(bytes, 0);
        AddName(bytes, queryName);
        AddUnsignedShort(bytes, 33);
        AddUnsignedShort(bytes, 1);
        foreach (SrvValue record in records)
        {
            bytes.Add(192);
            bytes.Add(12);
            AddUnsignedShort(bytes, 33);
            AddUnsignedShort(bytes, 1);
            bytes.AddRange(new byte[] { 0, 0, 0, 60 });
            List<byte> data = new List<byte>();
            AddUnsignedShort(data, record.Priority);
            AddUnsignedShort(data, record.Weight);
            AddUnsignedShort(data, record.Port);
            AddName(data, record.Target);
            AddUnsignedShort(bytes, data.Count);
            bytes.AddRange(data);
        }

        return bytes.ToArray();
    }

    private static byte[] Header(int identifier, int flags)
    {
        List<byte> bytes = new List<byte>();
        AddUnsignedShort(bytes, identifier);
        AddUnsignedShort(bytes, flags);
        AddUnsignedShort(bytes, 0);
        AddUnsignedShort(bytes, 0);
        AddUnsignedShort(bytes, 0);
        AddUnsignedShort(bytes, 0);
        return bytes.ToArray();
    }

    private static void AddName(List<byte> bytes, string name)
    {
        if (name == ".")
        {
            bytes.Add(0);
            return;
        }

        foreach (string label in name.Split('.'))
        {
            byte[] encoded = Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)encoded.Length);
            bytes.AddRange(encoded);
        }

        bytes.Add(0);
    }

    private static void AddUnsignedShort(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    private sealed class SrvValue
    {
        public int Priority;
        public int Weight;
        public int Port;
        public string Target;

        public SrvValue(int priority, int weight, int port, string target)
        {
            Priority = priority;
            Weight = weight;
            Port = port;
            Target = target;
        }
    }

    private sealed class FixedRandom : Random
    {
        private readonly long value;

        public FixedRandom(long value)
        {
            this.value = value;
        }

        public override long NextInt64(long maxValue)
        {
            return value;
        }
    }

    private sealed class FakeDnsQueryClient : IDnsQueryClient
    {
        public byte[] Response = Array.Empty<byte>();
        public string? QueryName;
        public int TimeoutMilliseconds;
        public CancellationToken CancellationToken;
        public int CallCount;
        public bool Cancel;

        public Task<byte[]> QuerySrvAsync(
            string queryName,
            int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            CallCount++;
            QueryName = queryName;
            TimeoutMilliseconds = timeoutMilliseconds;
            CancellationToken = cancellationToken;
            if (Cancel)
            {
                return Task.FromCanceled<byte[]>(cancellationToken);
            }

            return Task.FromResult(Response);
        }
    }

    private sealed class FakeDnsPacketExchange : IDnsPacketExchange
    {
        public byte[] UdpResponse = Array.Empty<byte>();
        public byte[] TcpResponse = Array.Empty<byte>();
        public int UdpCalls;
        public int TcpCalls;
        public int TcpTimeoutMilliseconds;
        public int QueryType;
        public int QueryClass;

        public byte[] ExchangeUdp(
            byte[] request,
            IPEndPoint server,
            int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            UdpCalls++;
            ReadQuestionTypeAndClass(request);
            return WithRequestIdentifier(UdpResponse, request);
        }

        public byte[] ExchangeTcp(
            byte[] request,
            IPEndPoint server,
            int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            TcpCalls++;
            TcpTimeoutMilliseconds = timeoutMilliseconds;
            return WithRequestIdentifier(TcpResponse, request);
        }

        private void ReadQuestionTypeAndClass(byte[] request)
        {
            int offset = 12;
            while (request[offset] != 0)
            {
                offset += request[offset] + 1;
            }

            offset++;
            QueryType = (request[offset] << 8) | request[offset + 1];
            QueryClass = (request[offset + 2] << 8) | request[offset + 3];
        }

        private static byte[] WithRequestIdentifier(byte[] template, byte[] request)
        {
            byte[] response = (byte[])template.Clone();
            response[0] = request[0];
            response[1] = request[1];
            return response;
        }
    }
}
