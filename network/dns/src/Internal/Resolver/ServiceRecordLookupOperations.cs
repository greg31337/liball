using System.Collections.Generic;

namespace dns;

internal static class ServiceRecordLookupOperations
{
    internal static DnsServiceResult LookupServiceRecords(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        LookupRecordOperations.ValidateRequest(request, service);
        DnsResolveRequest serviceRequest =
            LookupRecordOperations.CreateTypedRequest(
                request,
                DnsRecordType.Srv,
                33);
        DnsResolveResult resolved = service.Resolver.Resolve(serviceRequest);
        if (resolved == null)
        {
            throw new DnsResolutionException(
                "The resolver returned no service-record result.");
        }

        DnsServiceResult result = new DnsServiceResult();
        LookupRecordOperations.CopyServiceMetadata(result, resolved);
        if (resolved.Status != DnsResolveStatus.Success)
        {
            return result;
        }

        bool valid = CollectEndpoints(result.Endpoints, resolved);
        bool rootTarget = ContainsRootTarget(resolved);
        if (rootTarget)
        {
            if (result.Endpoints.Count != 1 || !valid)
            {
                result.Status = DnsResolveStatus.ProtocolError;
            }
            else
            {
                result.Status = DnsResolveStatus.NoData;
            }

            result.Endpoints.Clear();
            return result;
        }

        if (!valid || result.Endpoints.Count == 0)
        {
            result.Status = DnsResolveStatus.ProtocolError;
            result.Endpoints.Clear();
            return result;
        }

        List<DnsServiceEndpoint> ordered = OrderEndpoints(
            result.Endpoints,
            service.Entropy);
        result.Endpoints = ordered;
        ResolveEndpointAddresses(result, request, service);
        return result;
    }

    static bool CollectEndpoints(
        List<DnsServiceEndpoint> endpoints,
        DnsResolveResult resolved)
    {
        bool valid = true;
        int setIndex = 0;
        while (setIndex < resolved.RecordSets.Count)
        {
            DnsResourceRecordSet set = resolved.RecordSets[setIndex];
            if (LookupRecordOperations.EffectiveValue(
                    set.RecordTypeValue,
                    (int)set.RecordType) == 33)
            {
                if (set.Records.Count == 0)
                {
                    valid = false;
                }

                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (!TryAddEndpoint(endpoints, record, resolved))
                    {
                        valid = false;
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        return valid;
    }

    static bool TryAddEndpoint(
        List<DnsServiceEndpoint> endpoints,
        DnsResourceRecord record,
        DnsResolveResult resolved)
    {
        if (LookupRecordOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType) != 33 ||
            record.Data == null || record.Data.Srv == null ||
            record.Data.Srv.Priority < 0 ||
            record.Data.Srv.Priority > 65535 ||
            record.Data.Srv.Weight < 0 ||
            record.Data.Srv.Weight > 65535 ||
            record.Data.Srv.Port < 0 || record.Data.Srv.Port > 65535 ||
            record.Data.Srv.Target == null ||
            !record.Data.Srv.Target.IsAbsolute)
        {
            return false;
        }

        DnsServiceEndpoint endpoint = new DnsServiceEndpoint();
        endpoint.Priority = record.Data.Srv.Priority;
        endpoint.Weight = record.Data.Srv.Weight;
        endpoint.Port = record.Data.Srv.Port;
        endpoint.Target = RecordSetOperations.CopyName(
            record.Data.Srv.Target,
            false);
        endpoint.SecurityStatus = resolved.SecurityStatus;
        endpoint.HasTimeToLive = resolved.HasTimeToLive;
        endpoint.TimeToLive = resolved.TimeToLive;
        endpoints.Add(endpoint);
        return true;
    }

    static bool ContainsRootTarget(DnsResolveResult resolved)
    {
        int setIndex = 0;
        while (setIndex < resolved.RecordSets.Count)
        {
            DnsResourceRecordSet set = resolved.RecordSets[setIndex];
            int recordIndex = 0;
            while (recordIndex < set.Records.Count)
            {
                DnsResourceRecord record = set.Records[recordIndex];
                if (record.Data != null && record.Data.Srv != null &&
                    record.Data.Srv.Target != null &&
                    record.Data.Srv.Target.IsAbsolute &&
                    record.Data.Srv.Target.Labels.Count == 0)
                {
                    return true;
                }

                recordIndex = recordIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        return false;
    }

    static List<DnsServiceEndpoint> OrderEndpoints(
        List<DnsServiceEndpoint> endpoints,
        IDnsLookupEntropy entropy)
    {
        List<DnsServiceEndpoint> priorityOrder =
            new List<DnsServiceEndpoint>();
        int index = 0;
        while (index < endpoints.Count)
        {
            int position = priorityOrder.Count;
            while (position > 0 &&
                priorityOrder[position - 1].Priority >
                    endpoints[index].Priority)
            {
                position = position - 1;
            }

            priorityOrder.Insert(position, endpoints[index]);
            index = index + 1;
        }

        List<DnsServiceEndpoint> result = new List<DnsServiceEndpoint>();
        index = 0;
        while (index < priorityOrder.Count)
        {
            int end = index + 1;
            while (end < priorityOrder.Count &&
                priorityOrder[end].Priority == priorityOrder[index].Priority)
            {
                end = end + 1;
            }

            List<DnsServiceEndpoint> remaining = CreateWeightOrder(
                priorityOrder,
                index,
                end);
            while (remaining.Count > 0)
            {
                int selected = SelectWeightedIndex(remaining, entropy);
                result.Add(remaining[selected]);
                remaining.RemoveAt(selected);
            }

            index = end;
        }

        return result;
    }

    static List<DnsServiceEndpoint> CreateWeightOrder(
        List<DnsServiceEndpoint> endpoints,
        int start,
        int end)
    {
        List<DnsServiceEndpoint> result = new List<DnsServiceEndpoint>();
        int index = start;
        while (index < end)
        {
            if (endpoints[index].Weight == 0)
            {
                result.Add(endpoints[index]);
            }

            index = index + 1;
        }

        index = start;
        while (index < end)
        {
            if (endpoints[index].Weight != 0)
            {
                result.Add(endpoints[index]);
            }

            index = index + 1;
        }

        return result;
    }

    static int SelectWeightedIndex(
        List<DnsServiceEndpoint> endpoints,
        IDnsLookupEntropy entropy)
    {
        long total = 0;
        int index = 0;
        while (index < endpoints.Count)
        {
            total = total + endpoints[index].Weight;
            index = index + 1;
        }

        if (total >= int.MaxValue)
        {
            throw new DnsResolutionException(
                "The service-record weight sum is too large.");
        }

        int maximumExclusive = (int)total + 1;
        int selectedValue = entropy.NextIndex(maximumExclusive);
        if (selectedValue < 0 || selectedValue >= maximumExclusive)
        {
            throw new DnsResolutionException(
                "The lookup entropy source returned an out-of-range value.");
        }

        long running = 0;
        index = 0;
        while (index < endpoints.Count)
        {
            running = running + endpoints[index].Weight;
            if (running >= selectedValue)
            {
                return index;
            }

            index = index + 1;
        }

        return endpoints.Count - 1;
    }

    static void ResolveEndpointAddresses(
        DnsServiceResult result,
        DnsResolveRequest request,
        DnsLookupService service)
    {
        int index = 0;
        while (index < result.Endpoints.Count)
        {
            DnsServiceEndpoint endpoint = result.Endpoints[index];
            DnsResolveRequest addressRequest =
                LookupRecordOperations.CreateTypedRequest(
                    request,
                    DnsRecordType.A,
                    1);
            addressRequest.Name = RecordSetOperations.CopyName(
                endpoint.Target,
                false);
            DnsAddressResult addresses = service.LookupAddresses(
                addressRequest);
            int addressIndex = 0;
            while (addressIndex < addresses.Addresses.Count)
            {
                endpoint.Addresses.Add(RecordSetOperations.CopyBytes(
                    addresses.Addresses[addressIndex]));
                endpoint.AddressFamilies.Add(
                    addresses.AddressFamilies[addressIndex]);
                addressIndex = addressIndex + 1;
            }

            if (addresses.Status == DnsResolveStatus.Success)
            {
                endpoint.SecurityStatus = WorseSecurityStatus(
                    endpoint.SecurityStatus,
                    addresses.SecurityStatus);
                MergeEndpointTimeToLive(endpoint, addresses);
                result.SecurityStatus = WorseSecurityStatus(
                    result.SecurityStatus,
                    addresses.SecurityStatus);
                MergeResultTimeToLive(result, addresses);
            }

            index = index + 1;
        }
    }

    static DnsSecurityStatus WorseSecurityStatus(
        DnsSecurityStatus left,
        DnsSecurityStatus right)
    {
        if (SecurityRank(right) > SecurityRank(left))
        {
            return right;
        }

        return left;
    }

    static int SecurityRank(DnsSecurityStatus status)
    {
        if (status == DnsSecurityStatus.Bogus)
        {
            return 5;
        }

        if (status == DnsSecurityStatus.UnsupportedAlgorithm)
        {
            return 4;
        }

        if (status == DnsSecurityStatus.Indeterminate)
        {
            return 3;
        }

        if (status == DnsSecurityStatus.Insecure)
        {
            return 2;
        }

        if (status == DnsSecurityStatus.Secure)
        {
            return 1;
        }

        return 3;
    }

    static void MergeEndpointTimeToLive(
        DnsServiceEndpoint endpoint,
        DnsAddressResult addresses)
    {
        if (addresses.HasTimeToLive &&
            (!endpoint.HasTimeToLive ||
                addresses.TimeToLive < endpoint.TimeToLive))
        {
            endpoint.HasTimeToLive = true;
            endpoint.TimeToLive = addresses.TimeToLive;
        }
    }

    static void MergeResultTimeToLive(
        DnsServiceResult result,
        DnsAddressResult addresses)
    {
        if (addresses.HasTimeToLive &&
            (!result.HasTimeToLive ||
                addresses.TimeToLive < result.TimeToLive))
        {
            result.HasTimeToLive = true;
            result.TimeToLive = addresses.TimeToLive;
        }
    }
}
