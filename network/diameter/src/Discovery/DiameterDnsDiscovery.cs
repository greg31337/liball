using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

internal class DiameterDnsDiscovery
{
    internal const int NegativeCacheLifetimeSeconds = 60;

    internal IDnsResolver Resolver;
    internal IDiameterClock Clock;
    internal DiscoveryCache Cache;
    internal SrvSelector Selector;
    internal Dictionary<string, List<string>> RealmDependencies =
        new Dictionary<string, List<string>>();
    internal object DependencyLock = new object();

    internal DiameterDnsDiscovery(
        IDnsResolver resolver,
        IDiameterClock clock,
        IDiscoveryRandom randomSource)
    {
        if (resolver == null || clock == null || randomSource == null)
        {
            throw new ArgumentException(
                "DNS discovery requires a resolver, clock, and random source.");
        }

        Resolver = resolver;
        Clock = clock;
        Cache = new DiscoveryCache(clock, NegativeCacheLifetimeSeconds);
        Selector = new SrvSelector(randomSource);
    }

    internal DiameterDiscoveryResult ResolveDnsDiscovery(
        DiameterDiscoveryRequest request,
        int timeoutMilliseconds)
    {
        if (request == null)
        {
            throw new ArgumentException("A discovery request is required.");
        }

        request.ValidateDiscoveryRequest();
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("timeoutMilliseconds");
        }

        DiameterDiscoveryResult result = new DiameterDiscoveryResult();
        result.Request = CloneDiscoveryRequest(request);
        if (!request.AllowDnsDiscovery)
        {
            SetNegativeDiscoveryResult(
                result,
                "dns-discovery-disabled",
                "DNS discovery is disabled for this request.");
            return result;
        }

        long deadline = Clock.ReadMonotonicMilliseconds() + timeoutMilliseconds;
        DnsLookupResult naptrLookup = ResolveDiscoveryDnsQuery(
            request.Realm.Value,
            DnsMessageCodec.NaptrRecordType,
            request,
            deadline);
        ApplyDnsLookupMetadata(result, naptrLookup);
        AddNaptrRecordsToDiscovery(result, naptrLookup.Result.NaptrRecords);
        if (!naptrLookup.Result.Successful && naptrLookup.Result.ResponseCode != 3)
        {
            SetNegativeDiscoveryResult(
                result,
                "dns-naptr-query-failed",
                "The Diameter NAPTR query did not complete successfully.");
            return result;
        }

        if (naptrLookup.Result.NaptrRecords.Count == 0)
        {
            string fallbackName = SnaptrMatcher.CreateDiameterSrvFallbackName(request);
            if (!string.IsNullOrEmpty(fallbackName))
            {
                ResolveSrvTarget(fallbackName, request, deadline, result);
            }
        }
        else
        {
            List<DiameterNaptrRecord> matches =
                SnaptrMatcher.SelectCompatibleNaptrRecords(
                    naptrLookup.Result.NaptrRecords,
                    request);
            if (matches.Count == 0)
            {
                SetNegativeDiscoveryResult(
                    result,
                    "dns-naptr-no-compatible-service",
                    "No NAPTR record supports the requested application and transport.");
                return result;
            }

            ResolveNaptrTargets(matches, request, deadline, result);
        }

        if (result.Endpoints.Count > 0)
        {
            result.Resolved = true;
            result.Negative = false;
            result.Code = "dns-discovery-success";
            result.Message = "Diameter DNS discovery returned usable endpoints.";
        }
        else
        {
            SetNegativeDiscoveryResult(
                result,
                "dns-discovery-empty",
                "Diameter DNS discovery returned no usable endpoints.");
        }

        return result;
    }

    internal void InvalidateDiscoveryCache(string realm)
    {
        string normalizedRealm = DnsMessageCodec.NormalizeDnsQueryName(realm);
        string key = normalizedRealm.ToLowerInvariant();
        List<string> names = new List<string>();
        lock (DependencyLock)
        {
            if (RealmDependencies.ContainsKey(key))
            {
                names.AddRange(RealmDependencies[key]);
                RealmDependencies.Remove(key);
            }
        }

        if (names.Count == 0)
        {
            names.Add(normalizedRealm);
        }

        int index = 0;
        while (index < names.Count)
        {
            Cache.InvalidateDnsName(names[index]);
            index = index + 1;
        }
    }

    internal void ResolveNaptrTargets(
        List<DiameterNaptrRecord> matches,
        DiameterDiscoveryRequest request,
        long deadline,
        DiameterDiscoveryResult result)
    {
        int index = 0;
        while (index < matches.Count && result.Endpoints.Count == 0)
        {
            int order = matches[index].Order;
            while (index < matches.Count && matches[index].Order == order)
            {
                DiameterNaptrRecord record = matches[index];
                int resolution = SnaptrMatcher.ReadNaptrResolution(record.Flags);
                if (resolution == SnaptrMatcher.ServiceResolution)
                {
                    ResolveSrvTarget(record.Replacement, request, deadline, result);
                }
                else if (resolution == SnaptrMatcher.AddressResolution)
                {
                    int port = ReadDefaultDiscoveryPort(request.Security);
                    ResolveAddressTarget(
                        record.Replacement,
                        port,
                        request,
                        deadline,
                        result);
                }

                index = index + 1;
            }
        }
    }

    internal void ResolveSrvTarget(
        string serviceName,
        DiameterDiscoveryRequest request,
        long deadline,
        DiameterDiscoveryResult result)
    {
        DnsLookupResult lookup = ResolveDiscoveryDnsQuery(
            serviceName,
            DnsMessageCodec.SrvRecordType,
            request,
            deadline);
        ApplyDnsLookupMetadata(result, lookup);
        AddSrvRecordsToDiscovery(result, lookup.Result.SrvRecords);
        if (!lookup.Result.Successful)
        {
            return;
        }

        List<DiameterSrvRecord> ordered = Selector.OrderSrvRecords(
            lookup.Result.SrvRecords);
        int index = 0;
        while (index < ordered.Count &&
            result.Endpoints.Count < request.MaximumResults)
        {
            DiameterSrvRecord record = ordered[index];
            ResolveAddressTarget(
                record.Target,
                record.Port,
                request,
                deadline,
                result);
            index = index + 1;
        }
    }

    internal void ResolveAddressTarget(
        string host,
        int port,
        DiameterDiscoveryRequest request,
        long deadline,
        DiameterDiscoveryResult result)
    {
        DnsLookupResult ipv4Lookup = ResolveDiscoveryDnsQuery(
            host,
            DnsMessageCodec.Ipv4RecordType,
            request,
            deadline);
        ApplyDnsLookupMetadata(result, ipv4Lookup);
        AddAddressRecordsToDiscovery(result, ipv4Lookup.Result.AddressRecords);
        AddAddressEndpoints(
            ipv4Lookup.Result,
            host,
            port,
            request,
            result);
        if (result.Endpoints.Count >= request.MaximumResults)
        {
            return;
        }

        DnsLookupResult ipv6Lookup = ResolveDiscoveryDnsQuery(
            host,
            DnsMessageCodec.Ipv6RecordType,
            request,
            deadline);
        ApplyDnsLookupMetadata(result, ipv6Lookup);
        AddAddressRecordsToDiscovery(result, ipv6Lookup.Result.AddressRecords);
        AddAddressEndpoints(
            ipv6Lookup.Result,
            host,
            port,
            request,
            result);
    }

    internal DnsLookupResult ResolveDiscoveryDnsQuery(
        string name,
        int recordType,
        DiameterDiscoveryRequest request,
        long deadline)
    {
        RecordDiscoveryDependency(request.Realm.Value, name);
        int remaining = ReadRemainingDiscoveryMilliseconds(deadline);
        if (remaining <= 0)
        {
            throw new DiameterPeerException("Diameter DNS discovery timed out.");
        }

        return Cache.ResolveCachedDnsQuery(
            Resolver,
            name,
            recordType,
            remaining,
            request.AllowCachedResults);
    }

    internal void RecordDiscoveryDependency(string realm, string name)
    {
        string realmKey = DnsMessageCodec.NormalizeDnsQueryName(realm).ToLowerInvariant();
        string normalizedName = DnsMessageCodec.NormalizeDnsQueryName(name);
        lock (DependencyLock)
        {
            if (!RealmDependencies.ContainsKey(realmKey))
            {
                RealmDependencies[realmKey] = new List<string>();
            }

            List<string> names = RealmDependencies[realmKey];
            int index = 0;
            while (index < names.Count)
            {
                if (string.Equals(
                    names[index],
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                index = index + 1;
            }

            names.Add(normalizedName);
        }
    }

    internal void AddAddressEndpoints(
        DiameterDnsQueryResult lookup,
        string host,
        int port,
        DiameterDiscoveryRequest request,
        DiameterDiscoveryResult result)
    {
        if (!lookup.Successful)
        {
            return;
        }

        int index = 0;
        while (index < lookup.AddressRecords.Count &&
            result.Endpoints.Count < request.MaximumResults)
        {
            DiameterAddressRecord addressRecord = lookup.AddressRecords[index];
            if (addressRecord != null && addressRecord.Address != null &&
                !ContainsDiscoveryEndpoint(
                    result.Endpoints,
                    addressRecord.Address,
                    port,
                    request.Transport,
                    request.Security))
            {
                DiameterEndpoint endpoint = new DiameterEndpoint();
                endpoint.Identity.Value = DnsMessageCodec.NormalizeDnsQueryName(host);
                endpoint.Host = endpoint.Identity.Value;
                endpoint.Address = addressRecord.Address;
                endpoint.Port = port;
                endpoint.Transport = request.Transport;
                endpoint.Security = request.Security;
                result.Endpoints.Add(endpoint);
            }

            index = index + 1;
        }
    }

    internal bool ContainsDiscoveryEndpoint(
        List<DiameterEndpoint> endpoints,
        IPAddress address,
        int port,
        DiameterTransportKind transport,
        DiameterSecurityMode security)
    {
        int index = 0;
        while (index < endpoints.Count)
        {
            DiameterEndpoint endpoint = endpoints[index];
            if (endpoint.Address != null && endpoint.Address.Equals(address) &&
                endpoint.Port == port && endpoint.Transport == transport &&
                endpoint.Security == security)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ApplyDnsLookupMetadata(
        DiameterDiscoveryResult result,
        DnsLookupResult lookup)
    {
        if (lookup.FromCache)
        {
            result.FromCache = true;
        }

        if (lookup.ExpiresAt != default(DateTimeOffset) &&
            (result.ExpiresAt == default(DateTimeOffset) ||
            lookup.ExpiresAt < result.ExpiresAt))
        {
            result.ExpiresAt = lookup.ExpiresAt;
        }
    }

    internal void AddNaptrRecordsToDiscovery(
        DiameterDiscoveryResult result,
        List<DiameterNaptrRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            result.NaptrRecords.Add(Cache.CloneNaptrRecord(records[index]));
            index = index + 1;
        }
    }

    internal void AddSrvRecordsToDiscovery(
        DiameterDiscoveryResult result,
        List<DiameterSrvRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            result.SrvRecords.Add(Cache.CloneSrvRecord(records[index]));
            index = index + 1;
        }
    }

    internal void AddAddressRecordsToDiscovery(
        DiameterDiscoveryResult result,
        List<DiameterAddressRecord> records)
    {
        int index = 0;
        while (index < records.Count)
        {
            result.AddressRecords.Add(Cache.CloneAddressRecord(records[index]));
            index = index + 1;
        }
    }

    internal DiameterDiscoveryRequest CloneDiscoveryRequest(
        DiameterDiscoveryRequest source)
    {
        DiameterDiscoveryRequest result = new DiameterDiscoveryRequest();
        result.Realm.Value = source.Realm.Value;
        result.Application.Id = source.Application.Id;
        result.Application.VendorId = source.Application.VendorId;
        result.Application.IsAccounting = source.Application.IsAccounting;
        result.Transport = source.Transport;
        result.Security = source.Security;
        result.AllowStaticPeers = source.AllowStaticPeers;
        result.AllowDnsDiscovery = source.AllowDnsDiscovery;
        result.AllowCachedResults = source.AllowCachedResults;
        result.MaximumResults = source.MaximumResults;
        return result;
    }

    internal int ReadDefaultDiscoveryPort(DiameterSecurityMode security)
    {
        if (security == DiameterSecurityMode.Tls ||
            security == DiameterSecurityMode.Dtls)
        {
            return DiameterProtocolConstants.SecurePort;
        }

        return DiameterProtocolConstants.Port;
    }

    internal int ReadRemainingDiscoveryMilliseconds(long deadline)
    {
        long remaining = deadline - Clock.ReadMonotonicMilliseconds();
        if (remaining <= 0)
        {
            return 0;
        }

        if (remaining > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)remaining;
    }

    internal void SetNegativeDiscoveryResult(
        DiameterDiscoveryResult result,
        string code,
        string message)
    {
        result.Resolved = false;
        result.Negative = true;
        result.Code = code;
        result.Message = message;
    }
}
