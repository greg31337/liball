using System.Collections.Generic;

namespace dns;

internal class DnsAuthorizationDecision
{
    internal bool Valid = true;
    internal bool Authorized;
    internal bool RateLimited;
    internal bool Slipped;
    internal string Reason = string.Empty;
}

internal class DnsAuthorizationRateEntry
{
    internal int AddressFamily;
    internal byte[] Address = [];
    internal long WindowStarted;
    internal int ResponseCount;
    internal int LimitedCount;
}

internal class DnsAuthorizationRateState
{
    internal List<DnsAuthorizationRateEntry> Entries =
        new List<DnsAuthorizationRateEntry>();
}

internal static class AuthorizationOperations
{
    const int QueryOperation = 0;
    const int TransferOperation = 1;
    const int UpdateOperation = 2;
    const int NotifyOperation = 3;

    internal static DnsAuthorizationDecision AuthorizeQuery(
        DnsAuthorizationPolicy policy,
        DnsEndpoint client,
        DnsName zone,
        DnsTsigResult? tsig,
        DnsClockValue now,
        DnsAuthorizationRateState rateState)
    {
        bool authenticated = tsig != null && tsig.Success &&
            tsig.Authenticated && tsig.KeyName != null;
        DnsName? keyName = null;
        if (authenticated && tsig != null)
        {
            keyName = tsig.KeyName;
        }

        return Authorize(
            policy,
            client,
            zone,
            keyName,
            authenticated,
            QueryOperation,
            now,
            rateState);
    }

    internal static DnsAuthorizationDecision AuthorizeTransfer(
        DnsTransferRequest request,
        DnsClockValue now,
        DnsAuthorizationRateState rateState)
    {
        bool authenticated = request.TsigKey != null &&
            request.TsigKey.Enabled;
        DnsName? keyName = null;
        if (authenticated && request.TsigKey != null)
        {
            keyName = request.TsigKey.Name;
        }

        return Authorize(
            request.Authorization,
            request.Endpoint,
            request.Zone,
            keyName,
            authenticated,
            TransferOperation,
            now,
            rateState);
    }

    internal static DnsAuthorizationDecision AuthorizeUpdate(
        DnsUpdateRequest request,
        DnsClockValue now,
        DnsAuthorizationRateState rateState)
    {
        bool authenticated = request.TsigKey != null &&
            request.TsigKey.Enabled;
        DnsName? keyName = null;
        if (authenticated && request.TsigKey != null)
        {
            keyName = request.TsigKey.Name;
        }

        return Authorize(
            request.Authorization,
            request.ClientEndpoint,
            request.Zone,
            keyName,
            authenticated,
            UpdateOperation,
            now,
            rateState);
    }

    internal static DnsAuthorizationDecision AuthorizeNotify(
        DnsNotifyRequest request,
        DnsClockValue now,
        DnsAuthorizationRateState rateState)
    {
        bool authenticated = request.TsigKey != null &&
            request.TsigKey.Enabled;
        DnsName? keyName = null;
        if (authenticated && request.TsigKey != null)
        {
            keyName = request.TsigKey.Name;
        }

        return Authorize(
            request.Authorization,
            request.Endpoint,
            request.Zone,
            keyName,
            authenticated,
            NotifyOperation,
            now,
            rateState);
    }

    static DnsAuthorizationDecision Authorize(
        DnsAuthorizationPolicy policy,
        DnsEndpoint client,
        DnsName zone,
        DnsName? keyName,
        bool authenticated,
        int operation,
        DnsClockValue now,
        DnsAuthorizationRateState rateState)
    {
        DnsAuthorizationDecision result = new DnsAuthorizationDecision();
        if (!ValidatePolicy(policy))
        {
            result.Valid = false;
            result.Reason = "The authorization policy is malformed.";
            return result;
        }

        if (!OperationAllowed(policy, operation))
        {
            result.Reason = "The requested DNS operation is disabled by policy.";
            return result;
        }

        if (!ZoneAllowed(policy, zone))
        {
            result.Reason = "The requested DNS zone is not allowed by policy.";
            return result;
        }

        if (!ClientAllowed(policy, client))
        {
            result.Reason = "The client network is not allowed by policy.";
            return result;
        }

        if (RequiresTsig(policy, operation) &&
            (!authenticated || keyName == null ||
             !TsigKeyAllowed(policy, keyName)))
        {
            result.Reason =
                "The operation requires an allowed authenticated TSIG key.";
            return result;
        }

        result.Authorized = true;
        ApplyRateLimit(policy, client, now, rateState, result);
        return result;
    }

    static bool ValidatePolicy(DnsAuthorizationPolicy policy)
    {
        if (policy == null || policy.AllowedClientAddresses == null ||
            policy.AllowedClientAddressFamilies == null ||
            policy.AllowedClientPrefixLengths == null ||
            policy.DeniedClientAddresses == null ||
            policy.DeniedClientAddressFamilies == null ||
            policy.DeniedClientPrefixLengths == null ||
            policy.AllowedTsigKeyNames == null ||
            policy.AllowedZones == null ||
            policy.RateLimitResponses < 0 ||
            policy.RateLimitWindowMilliseconds <= 0 ||
            policy.RateLimitSlip < 0 ||
            !NetworkRulesValid(
                policy.AllowedClientAddresses,
                policy.AllowedClientAddressFamilies,
                policy.AllowedClientPrefixLengths) ||
            !NetworkRulesValid(
                policy.DeniedClientAddresses,
                policy.DeniedClientAddressFamilies,
                policy.DeniedClientPrefixLengths))
        {
            return false;
        }

        int index = 0;
        while (index < policy.AllowedTsigKeyNames.Count)
        {
            if (!AbsoluteNameValid(policy.AllowedTsigKeyNames[index]))
            {
                return false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < policy.AllowedZones.Count)
        {
            if (!AbsoluteNameValid(policy.AllowedZones[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool NetworkRulesValid(
        List<byte[]> addresses,
        List<int> families,
        List<int> prefixLengths)
    {
        if (addresses.Count != families.Count ||
            addresses.Count != prefixLengths.Count)
        {
            return false;
        }

        int index = 0;
        while (index < addresses.Count)
        {
            byte[] address = addresses[index];
            int family = families[index];
            int maximumPrefix = 0;
            if (family == 4 && address != null && address.Length == 4)
            {
                maximumPrefix = 32;
            }
            else if (family == 6 && address != null &&
                address.Length == 16)
            {
                maximumPrefix = 128;
            }
            else
            {
                return false;
            }

            if (prefixLengths[index] < 0 ||
                prefixLengths[index] > maximumPrefix)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AbsoluteNameValid(DnsName name)
    {
        if (name == null)
        {
            return false;
        }

        RawDnsNameResult validated = RawDnsName.ValidateName(name);
        return validated.Success && validated.Name != null &&
            validated.Name.IsAbsolute;
    }

    static bool OperationAllowed(
        DnsAuthorizationPolicy policy,
        int operation)
    {
        switch (operation)
        {
            case QueryOperation:
                return policy.AllowQueries;
            case TransferOperation:
                return policy.AllowTransfers;
            case UpdateOperation:
                return policy.AllowUpdates;
            case NotifyOperation:
                return policy.AllowNotify;
            default:
                return false;
        }
    }

    static bool RequiresTsig(
        DnsAuthorizationPolicy policy,
        int operation)
    {
        switch (operation)
        {
            case TransferOperation:
                return policy.RequireTsigForTransfers;
            case UpdateOperation:
                return policy.RequireTsigForUpdates;
            case NotifyOperation:
                return policy.RequireTsigForNotify;
            default:
                return false;
        }
    }

    static bool ZoneAllowed(
        DnsAuthorizationPolicy policy,
        DnsName zone)
    {
        if (!AbsoluteNameValid(zone))
        {
            return false;
        }

        if (policy.AllowedZones.Count == 0)
        {
            return true;
        }

        int index = 0;
        while (index < policy.AllowedZones.Count)
        {
            if (RecordSetOperations.NamesEqual(
                    policy.AllowedZones[index],
                    zone))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ClientAllowed(
        DnsAuthorizationPolicy policy,
        DnsEndpoint client)
    {
        bool hasRules = policy.AllowedClientAddresses.Count > 0 ||
            policy.DeniedClientAddresses.Count > 0;
        if (!hasRules)
        {
            return true;
        }

        if (!ClientAddressValid(client))
        {
            return false;
        }

        if (MatchesRules(
                client,
                policy.DeniedClientAddresses,
                policy.DeniedClientAddressFamilies,
                policy.DeniedClientPrefixLengths))
        {
            return false;
        }

        return policy.AllowedClientAddresses.Count == 0 ||
            MatchesRules(
                client,
                policy.AllowedClientAddresses,
                policy.AllowedClientAddressFamilies,
                policy.AllowedClientPrefixLengths);
    }

    static bool MatchesRules(
        DnsEndpoint client,
        List<byte[]> addresses,
        List<int> families,
        List<int> prefixLengths)
    {
        int index = 0;
        while (index < addresses.Count)
        {
            if (client.AddressFamily == families[index] &&
                PrefixMatches(
                    client.Address,
                    addresses[index],
                    prefixLengths[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool PrefixMatches(
        byte[] address,
        byte[] network,
        int prefixLength)
    {
        if (address.Length != network.Length)
        {
            return false;
        }

        int completeBytes = prefixLength / 8;
        int index = 0;
        while (index < completeBytes)
        {
            if (address[index] != network[index])
            {
                return false;
            }

            index = index + 1;
        }

        int remainingBits = prefixLength % 8;
        if (remainingBits == 0)
        {
            return true;
        }

        int mask = (255 << (8 - remainingBits)) & 255;
        return (address[completeBytes] & mask) ==
            (network[completeBytes] & mask);
    }

    static bool ClientAddressValid(DnsEndpoint client)
    {
        return client != null && client.HasAddress &&
            ((client.AddressFamily == 4 && client.Address != null &&
              client.Address.Length == 4) ||
             (client.AddressFamily == 6 && client.Address != null &&
              client.Address.Length == 16));
    }

    static bool TsigKeyAllowed(
        DnsAuthorizationPolicy policy,
        DnsName keyName)
    {
        if (policy.AllowedTsigKeyNames.Count == 0)
        {
            return true;
        }

        int index = 0;
        while (index < policy.AllowedTsigKeyNames.Count)
        {
            if (RecordSetOperations.NamesEqual(
                    policy.AllowedTsigKeyNames[index],
                    keyName))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void ApplyRateLimit(
        DnsAuthorizationPolicy policy,
        DnsEndpoint client,
        DnsClockValue now,
        DnsAuthorizationRateState rateState,
        DnsAuthorizationDecision result)
    {
        if (policy.RateLimitResponses == 0)
        {
            return;
        }

        if (!ClientAddressValid(client))
        {
            result.Authorized = false;
            result.Reason =
                "Rate limiting requires a valid numeric client address.";
            return;
        }

        lock (rateState.Entries)
        {
            RemoveExpiredEntries(
                rateState,
                now.MonotonicMilliseconds,
                policy.RateLimitWindowMilliseconds);
            DnsAuthorizationRateEntry? entry = FindRateEntry(
                rateState,
                client);
            if (entry == null)
            {
                entry = new DnsAuthorizationRateEntry();
                entry.AddressFamily = client.AddressFamily;
                entry.Address = RecordSetOperations.CopyBytes(
                    client.Address);
                entry.WindowStarted = now.MonotonicMilliseconds;
                rateState.Entries.Add(entry);
            }

            if (entry.ResponseCount < policy.RateLimitResponses)
            {
                entry.ResponseCount = entry.ResponseCount + 1;
                return;
            }

            entry.LimitedCount = entry.LimitedCount + 1;
            result.RateLimited = true;
            result.Authorized = false;
            result.Reason = "The client response rate limit was exceeded.";
            if (policy.RateLimitSlip > 0 &&
                entry.LimitedCount % policy.RateLimitSlip == 0)
            {
                result.Authorized = true;
                result.Slipped = true;
            }
        }
    }

    static void RemoveExpiredEntries(
        DnsAuthorizationRateState rateState,
        long now,
        int windowLength)
    {
        int index = rateState.Entries.Count - 1;
        while (index >= 0)
        {
            DnsAuthorizationRateEntry entry = rateState.Entries[index];
            long elapsed = now - entry.WindowStarted;
            if (elapsed < 0 || elapsed >= windowLength)
            {
                rateState.Entries.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    static DnsAuthorizationRateEntry? FindRateEntry(
        DnsAuthorizationRateState rateState,
        DnsEndpoint client)
    {
        int index = 0;
        while (index < rateState.Entries.Count)
        {
            DnsAuthorizationRateEntry entry = rateState.Entries[index];
            if (entry.AddressFamily == client.AddressFamily &&
                RecordSetOperations.BytesEqual(
                    entry.Address,
                    client.Address))
            {
                return entry;
            }

            index = index + 1;
        }

        return null;
    }
}
