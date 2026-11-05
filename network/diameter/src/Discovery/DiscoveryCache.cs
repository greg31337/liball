using System;
using System.Collections.Generic;
using System.Globalization;

namespace diameter;

internal class DiscoveryCache
{
    internal Dictionary<string, DnsCacheEntry> Entries =
        new Dictionary<string, DnsCacheEntry>();
    internal object CacheLock = new object();
    internal IDiameterClock Clock;
    internal int NegativeLifetimeSeconds;

    internal DiscoveryCache(IDiameterClock clock, int negativeLifetimeSeconds)
    {
        if (clock == null)
        {
            throw new ArgumentException("A discovery cache requires a clock.");
        }

        if (negativeLifetimeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException("negativeLifetimeSeconds");
        }

        Clock = clock;
        NegativeLifetimeSeconds = negativeLifetimeSeconds;
    }

    internal DnsLookupResult ResolveCachedDnsQuery(
        IDnsResolver resolver,
        string name,
        int recordType,
        int timeoutMilliseconds,
        bool allowCachedResult)
    {
        if (resolver == null)
        {
            throw new ArgumentException("A DNS resolver is required.");
        }

        string normalizedName = DnsMessageCodec.NormalizeDnsQueryName(name);
        string key = CreateDnsCacheKey(normalizedName, recordType);
        DateTimeOffset now = Clock.ReadUtcTime();
        if (allowCachedResult)
        {
            lock (CacheLock)
            {
                if (Entries.ContainsKey(key))
                {
                    DnsCacheEntry entry = Entries[key];
                    if (entry.ExpiresAt > now)
                    {
                        DnsLookupResult cached = new DnsLookupResult();
                        cached.Result = CloneDnsQueryResult(entry.Result);
                        cached.FromCache = true;
                        cached.ExpiresAt = entry.ExpiresAt;
                        return cached;
                    }

                    Entries.Remove(key);
                }
            }
        }

        DiameterDnsQueryResult resolved = ResolveDnsRecordType(
            resolver,
            normalizedName,
            recordType,
            timeoutMilliseconds);
        DiameterDnsQueryResult owned = CloneDnsQueryResult(resolved);
        DateTimeOffset expiration = ReadDnsResultExpiration(
            owned,
            recordType,
            now);
        if (expiration > now && IsDnsResultCacheable(owned))
        {
            DnsCacheEntry entry = new DnsCacheEntry();
            entry.Result = CloneDnsQueryResult(owned);
            entry.ExpiresAt = expiration;
            lock (CacheLock)
            {
                Entries[key] = entry;
            }
        }

        DnsLookupResult lookup = new DnsLookupResult();
        lookup.Result = owned;
        lookup.ExpiresAt = expiration;
        return lookup;
    }

    internal void InvalidateDnsName(string name)
    {
        string normalizedName = DnsMessageCodec.NormalizeDnsQueryName(name);
        string prefix = normalizedName.ToLowerInvariant() + "|";
        lock (CacheLock)
        {
            List<string> removals = new List<string>();
            foreach (string key in Entries.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    removals.Add(key);
                }
            }

            int index = 0;
            while (index < removals.Count)
            {
                Entries.Remove(removals[index]);
                index = index + 1;
            }
        }
    }

    internal void InvalidateAllDnsEntries()
    {
        lock (CacheLock)
        {
            Entries.Clear();
        }
    }

    internal DiameterDnsQueryResult ResolveDnsRecordType(
        IDnsResolver resolver,
        string name,
        int recordType,
        int timeoutMilliseconds)
    {
        if (recordType == DnsMessageCodec.NaptrRecordType)
        {
            return resolver.ResolveDiameterNaptrRecords(name, timeoutMilliseconds);
        }

        if (recordType == DnsMessageCodec.SrvRecordType)
        {
            return resolver.ResolveDiameterSrvRecords(name, timeoutMilliseconds);
        }

        if (recordType == DnsMessageCodec.Ipv4RecordType)
        {
            return resolver.ResolveDiameterIpv4Records(name, timeoutMilliseconds);
        }

        if (recordType == DnsMessageCodec.Ipv6RecordType)
        {
            return resolver.ResolveDiameterIpv6Records(name, timeoutMilliseconds);
        }

        throw new ArgumentOutOfRangeException("recordType");
    }

    internal DateTimeOffset ReadDnsResultExpiration(
        DiameterDnsQueryResult result,
        int recordType,
        DateTimeOffset now)
    {
        DateTimeOffset expiration = default(DateTimeOffset);
        if (recordType == DnsMessageCodec.NaptrRecordType)
        {
            int index = 0;
            while (index < result.NaptrRecords.Count)
            {
                DateTimeOffset recordExpiration = ReadNaptrRecordExpiration(
                    result.NaptrRecords[index],
                    now);
                expiration = SelectEarlierDnsExpiration(expiration, recordExpiration);
                index = index + 1;
            }
        }
        else if (recordType == DnsMessageCodec.SrvRecordType)
        {
            int index = 0;
            while (index < result.SrvRecords.Count)
            {
                DateTimeOffset recordExpiration = ReadSrvRecordExpiration(
                    result.SrvRecords[index],
                    now);
                expiration = SelectEarlierDnsExpiration(expiration, recordExpiration);
                index = index + 1;
            }
        }
        else if (recordType == DnsMessageCodec.Ipv4RecordType ||
            recordType == DnsMessageCodec.Ipv6RecordType)
        {
            int index = 0;
            while (index < result.AddressRecords.Count)
            {
                DateTimeOffset recordExpiration = ReadAddressRecordExpiration(
                    result.AddressRecords[index],
                    now);
                expiration = SelectEarlierDnsExpiration(expiration, recordExpiration);
                index = index + 1;
            }
        }

        if (expiration == default(DateTimeOffset) &&
            ((result.Successful && !HasDnsRecords(result, recordType)) ||
            result.ResponseCode == 3))
        {
            if (result.NegativeTimeToLiveSeconds > 0)
            {
                expiration = now.AddSeconds(result.NegativeTimeToLiveSeconds);
            }
            else
            {
                expiration = now.AddSeconds(NegativeLifetimeSeconds);
            }
        }

        return expiration;
    }

    internal bool IsDnsResultCacheable(DiameterDnsQueryResult result)
    {
        return result.ResponseCode == 3 || result.Successful;
    }

    internal bool HasDnsRecords(DiameterDnsQueryResult result, int recordType)
    {
        if (recordType == DnsMessageCodec.NaptrRecordType)
        {
            return result.NaptrRecords.Count > 0;
        }

        if (recordType == DnsMessageCodec.SrvRecordType)
        {
            return result.SrvRecords.Count > 0;
        }

        return result.AddressRecords.Count > 0;
    }

    internal DateTimeOffset ReadNaptrRecordExpiration(
        DiameterNaptrRecord record,
        DateTimeOffset now)
    {
        if (record.ExpiresAt != default(DateTimeOffset))
        {
            return record.ExpiresAt;
        }

        return now.AddSeconds(record.TimeToLiveSeconds);
    }

    internal DateTimeOffset ReadSrvRecordExpiration(
        DiameterSrvRecord record,
        DateTimeOffset now)
    {
        if (record.ExpiresAt != default(DateTimeOffset))
        {
            return record.ExpiresAt;
        }

        return now.AddSeconds(record.TimeToLiveSeconds);
    }

    internal DateTimeOffset ReadAddressRecordExpiration(
        DiameterAddressRecord record,
        DateTimeOffset now)
    {
        if (record.ExpiresAt != default(DateTimeOffset))
        {
            return record.ExpiresAt;
        }

        return now.AddSeconds(record.TimeToLiveSeconds);
    }

    internal DateTimeOffset SelectEarlierDnsExpiration(
        DateTimeOffset current,
        DateTimeOffset candidate)
    {
        if (candidate == default(DateTimeOffset))
        {
            return current;
        }

        if (current == default(DateTimeOffset) || candidate < current)
        {
            return candidate;
        }

        return current;
    }

    internal string CreateDnsCacheKey(string name, int recordType)
    {
        return name.ToLowerInvariant() + "|" +
            recordType.ToString(CultureInfo.InvariantCulture);
    }

    internal DiameterDnsQueryResult CloneDnsQueryResult(
        DiameterDnsQueryResult source)
    {
        if (source == null || source.NaptrRecords == null ||
            source.SrvRecords == null || source.AddressRecords == null)
        {
            throw new DiameterPeerException("The DNS resolver returned invalid result data.");
        }

        DiameterDnsQueryResult result = new DiameterDnsQueryResult();
        result.QueryName = source.QueryName;
        result.RecordType = source.RecordType;
        result.ResponseCode = source.ResponseCode;
        result.Successful = source.Successful;
        result.Authoritative = source.Authoritative;
        result.RecursionAvailable = source.RecursionAvailable;
        result.UsedTcpFallback = source.UsedTcpFallback;
        result.NegativeTimeToLiveSeconds = source.NegativeTimeToLiveSeconds;
        result.Code = source.Code;
        result.Message = source.Message;

        int naptrIndex = 0;
        while (naptrIndex < source.NaptrRecords.Count)
        {
            result.NaptrRecords.Add(CloneNaptrRecord(source.NaptrRecords[naptrIndex]));
            naptrIndex = naptrIndex + 1;
        }

        int srvIndex = 0;
        while (srvIndex < source.SrvRecords.Count)
        {
            result.SrvRecords.Add(CloneSrvRecord(source.SrvRecords[srvIndex]));
            srvIndex = srvIndex + 1;
        }

        int addressIndex = 0;
        while (addressIndex < source.AddressRecords.Count)
        {
            result.AddressRecords.Add(
                CloneAddressRecord(source.AddressRecords[addressIndex]));
            addressIndex = addressIndex + 1;
        }

        return result;
    }

    internal DiameterNaptrRecord CloneNaptrRecord(DiameterNaptrRecord source)
    {
        if (source == null)
        {
            throw new DiameterPeerException("The DNS result contains a null NAPTR record.");
        }

        DiameterNaptrRecord result = new DiameterNaptrRecord();
        result.Name = source.Name;
        result.Order = source.Order;
        result.Preference = source.Preference;
        result.Flags = source.Flags;
        result.Service = source.Service;
        result.RegularExpression = source.RegularExpression;
        result.Replacement = source.Replacement;
        result.TimeToLiveSeconds = source.TimeToLiveSeconds;
        result.ExpiresAt = source.ExpiresAt;
        return result;
    }

    internal DiameterSrvRecord CloneSrvRecord(DiameterSrvRecord source)
    {
        if (source == null)
        {
            throw new DiameterPeerException("The DNS result contains a null SRV record.");
        }

        DiameterSrvRecord result = new DiameterSrvRecord();
        result.Name = source.Name;
        result.Priority = source.Priority;
        result.Weight = source.Weight;
        result.Port = source.Port;
        result.Target = source.Target;
        result.TimeToLiveSeconds = source.TimeToLiveSeconds;
        result.ExpiresAt = source.ExpiresAt;
        return result;
    }

    internal DiameterAddressRecord CloneAddressRecord(DiameterAddressRecord source)
    {
        if (source == null)
        {
            throw new DiameterPeerException("The DNS result contains a null address record.");
        }

        DiameterAddressRecord result = new DiameterAddressRecord();
        result.Name = source.Name;
        result.Address = source.Address;
        result.TimeToLiveSeconds = source.TimeToLiveSeconds;
        result.ExpiresAt = source.ExpiresAt;
        return result;
    }
}

internal class DnsCacheEntry
{
    internal DiameterDnsQueryResult Result = new DiameterDnsQueryResult();
    internal DateTimeOffset ExpiresAt;
}

internal class DnsLookupResult
{
    internal DiameterDnsQueryResult Result = new DiameterDnsQueryResult();
    internal bool FromCache;
    internal DateTimeOffset ExpiresAt;
}
