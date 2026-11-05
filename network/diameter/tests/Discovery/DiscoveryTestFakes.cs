using System.Net;

namespace diameter.Tests;

internal class FakeDnsResolver : IDnsResolver
{
    internal Dictionary<string, DiameterDnsQueryResult> Responses =
        new Dictionary<string, DiameterDnsQueryResult>();
    internal Dictionary<string, int> QueryCounts = new Dictionary<string, int>();

    public DiameterDnsQueryResult ResolveDiameterNaptrRecords(
        string name,
        int timeoutMilliseconds)
    {
        return ResolveFakeDnsQuery(name, DnsMessageCodec.NaptrRecordType);
    }

    public DiameterDnsQueryResult ResolveDiameterSrvRecords(
        string name,
        int timeoutMilliseconds)
    {
        return ResolveFakeDnsQuery(name, DnsMessageCodec.SrvRecordType);
    }

    public DiameterDnsQueryResult ResolveDiameterIpv4Records(
        string name,
        int timeoutMilliseconds)
    {
        return ResolveFakeDnsQuery(name, DnsMessageCodec.Ipv4RecordType);
    }

    public DiameterDnsQueryResult ResolveDiameterIpv6Records(
        string name,
        int timeoutMilliseconds)
    {
        return ResolveFakeDnsQuery(name, DnsMessageCodec.Ipv6RecordType);
    }

    internal void SetFakeDnsResponse(
        string name,
        int recordType,
        DiameterDnsQueryResult result)
    {
        Responses[CreateFakeDnsKey(name, recordType)] = result;
    }

    internal int CountFakeDnsQueries(string name, int recordType)
    {
        string key = CreateFakeDnsKey(name, recordType);
        if (!QueryCounts.ContainsKey(key))
        {
            return 0;
        }

        return QueryCounts[key];
    }

    internal DiameterDnsQueryResult ResolveFakeDnsQuery(string name, int recordType)
    {
        string key = CreateFakeDnsKey(name, recordType);
        if (!QueryCounts.ContainsKey(key))
        {
            QueryCounts[key] = 0;
        }

        QueryCounts[key]++;
        if (Responses.ContainsKey(key))
        {
            return Responses[key];
        }

        DiameterDnsQueryResult result = new DiameterDnsQueryResult();
        result.QueryName = name;
        result.RecordType = recordType;
        result.Successful = true;
        result.Code = "dns-success";
        return result;
    }

    internal string CreateFakeDnsKey(string name, int recordType)
    {
        return name.TrimEnd('.').ToLowerInvariant() + "|" + recordType;
    }
}

internal class FakeDiscoveryClock : IDiameterClock
{
    internal DateTimeOffset UtcTime = new DateTimeOffset(
        2026, 8, 29, 18, 0, 0, TimeSpan.Zero);
    internal long MonotonicMilliseconds;

    public DateTimeOffset ReadUtcTime()
    {
        return UtcTime;
    }

    public long ReadMonotonicMilliseconds()
    {
        return MonotonicMilliseconds;
    }

    internal void AdvanceDiscoveryTime(int seconds)
    {
        UtcTime = UtcTime.AddSeconds(seconds);
        MonotonicMilliseconds += seconds * 1000L;
    }
}

internal class SequenceDiscoveryRandom : IDiscoveryRandom
{
    internal List<int> Values = new List<int>();
    internal int Index;

    internal SequenceDiscoveryRandom(params int[] values)
    {
        Values.AddRange(values);
    }

    public int ReadDiscoveryRandomValue(int maximumExclusive)
    {
        if (Values.Count == 0)
        {
            return 0;
        }

        int value = Values[Index % Values.Count];
        Index++;
        return value;
    }
}

internal class RecordingDiscoveryAdmissionPolicy : IPeerAdmissionPolicy
{
    internal List<DiameterPeerAdmissionRequest> Requests =
        new List<DiameterPeerAdmissionRequest>();
    internal string RejectedIdentity = string.Empty;

    public DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request)
    {
        Requests.Add(request);
        DiameterPeerAdmissionResult result = new DiameterPeerAdmissionResult();
        result.Admitted = !string.Equals(
            request.PeerIdentity.Value,
            RejectedIdentity,
            StringComparison.OrdinalIgnoreCase);
        if (result.Admitted)
        {
            result.Code = "admitted";
        }
        else
        {
            result.Code = "rejected";
        }

        return result;
    }
}

internal static class DiscoveryTestData
{
    internal static DiameterDiscoveryRequest CreateDiscoveryRequest(
        uint applicationId,
        DiameterTransportKind transport,
        DiameterSecurityMode security)
    {
        DiameterDiscoveryRequest request = new DiameterDiscoveryRequest();
        request.Realm.Value = "example";
        request.Application.Id = applicationId;
        request.Transport = transport;
        request.Security = security;
        return request;
    }

    internal static DiameterDnsQueryResult CreateSuccessfulDnsResult(
        string name,
        int recordType)
    {
        DiameterDnsQueryResult result = new DiameterDnsQueryResult();
        result.QueryName = name;
        result.RecordType = recordType;
        result.Successful = true;
        result.Code = "dns-success";
        return result;
    }

    internal static DiameterNaptrRecord CreateNaptrRecord(
        int order,
        int preference,
        string flags,
        string service,
        string replacement,
        uint timeToLive)
    {
        DiameterNaptrRecord record = new DiameterNaptrRecord();
        record.Name = "example";
        record.Order = order;
        record.Preference = preference;
        record.Flags = flags;
        record.Service = service;
        record.Replacement = replacement;
        record.TimeToLiveSeconds = timeToLive;
        return record;
    }

    internal static DiameterSrvRecord CreateSrvRecord(
        int priority,
        int weight,
        int port,
        string target,
        uint timeToLive)
    {
        DiameterSrvRecord record = new DiameterSrvRecord();
        record.Name = "_diameter._tcp.example";
        record.Priority = priority;
        record.Weight = weight;
        record.Port = port;
        record.Target = target;
        record.TimeToLiveSeconds = timeToLive;
        return record;
    }

    internal static DiameterAddressRecord CreateAddressRecord(
        string name,
        string address,
        uint timeToLive)
    {
        DiameterAddressRecord record = new DiameterAddressRecord();
        record.Name = name;
        record.Address = IPAddress.Parse(address);
        record.TimeToLiveSeconds = timeToLive;
        return record;
    }
}
