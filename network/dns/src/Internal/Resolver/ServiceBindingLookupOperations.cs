using System.Collections.Generic;

namespace dns;

internal class DnsServiceBindingCandidate
{
    internal DnsName Owner = new DnsName();
    internal DnsSvcbData Binding = new DnsSvcbData();
    internal DnsSvcbData ParsedBinding = new DnsSvcbData();
}

internal class DnsServiceBindingCollection
{
    internal List<DnsServiceBindingCandidate> Aliases =
        new List<DnsServiceBindingCandidate>();
    internal List<DnsServiceBindingCandidate> Services =
        new List<DnsServiceBindingCandidate>();
    internal bool Invalid;
    internal bool ServiceInvalid;
    internal int MatchingRecordCount;
}

internal class DnsServiceBindingState
{
    internal DnsName CurrentName = new DnsName();
    internal List<DnsName> VisitedNames = new List<DnsName>();
    internal int AliasCount;
    internal DnsMessage? Response;
    internal DnsSecurityStatus SecurityStatus;
    internal bool HasSecurityStatus;
    internal bool HasTimeToLive;
    internal uint TimeToLive;
}

internal static class ServiceBindingLookupOperations
{
    internal static DnsServiceResult LookupServiceBindings(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        LookupRecordOperations.ValidateRequest(request, service);
        if (request.Options.MaximumAliasDepth < 0)
        {
            throw new DnsResolutionException(
                "The maximum alias depth cannot be negative.");
        }

        int typeValue = ReadBindingType(request);
        DnsRecordType recordType = typeValue == 65 ?
            DnsRecordType.Https : DnsRecordType.Svcb;
        DnsServiceBindingState state = CreateState(request.Name);
        while (true)
        {
            DnsResolveRequest bindingRequest =
                LookupRecordOperations.CreateTypedRequest(
                    request,
                    recordType,
                    typeValue);
            bindingRequest.Name = RecordSetOperations.CopyName(
                state.CurrentName,
                false);
            DnsResolveResult resolved = service.Resolver.Resolve(
                bindingRequest);
            if (resolved == null)
            {
                throw new DnsResolutionException(
                    "The resolver returned no service-binding result.");
            }

            AccumulateResolution(state, resolved);
            if (resolved.Status != DnsResolveStatus.Success)
            {
                return CreateResult(state, resolved.Status);
            }

            DnsServiceBindingCollection collection = CollectBindings(
                resolved,
                typeValue);
            if (collection.Invalid || collection.MatchingRecordCount == 0)
            {
                return CreateResult(state, DnsResolveStatus.ProtocolError);
            }

            if (collection.Aliases.Count > 0)
            {
                DnsServiceBindingCandidate alias = SelectAlias(
                    collection.Aliases,
                    service.Entropy);
                if (alias.Binding.TargetName.Labels.Count == 0)
                {
                    return CreateResult(state, DnsResolveStatus.NoData);
                }

                DnsResolveStatus aliasStatus = ApplyAlias(
                    state,
                    alias.Binding.TargetName,
                    request.Options.MaximumAliasDepth);
                if (aliasStatus != DnsResolveStatus.Success)
                {
                    return CreateResult(state, aliasStatus);
                }

                continue;
            }

            if (collection.ServiceInvalid)
            {
                return CreateResult(state, DnsResolveStatus.ProtocolError);
            }

            DnsServiceResult result = CreateResult(
                state,
                DnsResolveStatus.Success);
            AddCompatibleEndpoints(result, collection.Services, typeValue);
            if (result.Endpoints.Count == 0)
            {
                result.Status = DnsResolveStatus.NoData;
                return result;
            }

            SortEndpoints(result.Endpoints);
            ResolveEndpointAddresses(result, request, service);
            return result;
        }
    }

    static int ReadBindingType(DnsResolveRequest request)
    {
        int typeValue = LookupRecordOperations.EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        if (typeValue == 65)
        {
            return 65;
        }

        return 64;
    }

    static DnsServiceBindingState CreateState(DnsName name)
    {
        DnsServiceBindingState result = new DnsServiceBindingState();
        result.CurrentName = RecordSetOperations.CopyName(name, false);
        result.VisitedNames.Add(RecordSetOperations.CopyName(name, true));
        return result;
    }

    static void AccumulateResolution(
        DnsServiceBindingState state,
        DnsResolveResult resolved)
    {
        state.Response = resolved.Response;
        if (resolved.CanonicalName != null)
        {
            state.CurrentName = RecordSetOperations.CopyName(
                resolved.CanonicalName,
                false);
            if (!ContainsName(state.VisitedNames, state.CurrentName))
            {
                state.VisitedNames.Add(RecordSetOperations.CopyName(
                    state.CurrentName,
                    true));
            }
        }

        if (!state.HasSecurityStatus)
        {
            state.SecurityStatus = resolved.SecurityStatus;
            state.HasSecurityStatus = true;
        }
        else
        {
            state.SecurityStatus = WorseSecurityStatus(
                state.SecurityStatus,
                resolved.SecurityStatus);
        }

        MergeStateTimeToLive(
            state,
            resolved.HasTimeToLive,
            resolved.TimeToLive);
    }

    static DnsServiceBindingCollection CollectBindings(
        DnsResolveResult resolved,
        int typeValue)
    {
        DnsServiceBindingCollection result =
            new DnsServiceBindingCollection();
        int setIndex = 0;
        while (setIndex < resolved.RecordSets.Count)
        {
            DnsResourceRecordSet set = resolved.RecordSets[setIndex];
            int setType = LookupRecordOperations.EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (setType == typeValue)
            {
                if (set.Records == null || set.Records.Count == 0 ||
                    set.Name == null || !set.Name.IsAbsolute)
                {
                    result.Invalid = true;
                }
                else
                {
                    CollectSetBindings(result, set, typeValue);
                }
            }

            setIndex = setIndex + 1;
        }

        return result;
    }

    static void CollectSetBindings(
        DnsServiceBindingCollection destination,
        DnsResourceRecordSet set,
        int typeValue)
    {
        int recordIndex = 0;
        while (recordIndex < set.Records.Count)
        {
            DnsResourceRecord record = set.Records[recordIndex];
            destination.MatchingRecordCount =
                destination.MatchingRecordCount + 1;
            int recordType = LookupRecordOperations.EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (recordType != typeValue || record.Data == null ||
                record.Data.Svcb == null ||
                record.Data.Svcb.Priority < 0 ||
                record.Data.Svcb.Priority > 65535 ||
                record.Data.Svcb.TargetName == null ||
                !record.Data.Svcb.TargetName.IsAbsolute)
            {
                destination.Invalid = true;
            }
            else
            {
                DnsServiceBindingCandidate candidate =
                    new DnsServiceBindingCandidate();
                candidate.Owner = RecordSetOperations.CopyName(
                    set.Name,
                    false);
                candidate.Binding = record.Data.Svcb;
                if (candidate.Binding.Priority == 0)
                {
                    candidate.ParsedBinding = candidate.Binding;
                    destination.Aliases.Add(candidate);
                }
                else
                {
                    DnsSvcbData? parsed = ParseServiceBinding(
                        candidate.Binding,
                        typeValue);
                    if (parsed == null)
                    {
                        destination.ServiceInvalid = true;
                    }
                    else
                    {
                        candidate.ParsedBinding = parsed;
                        destination.Services.Add(candidate);
                    }
                }
            }

            recordIndex = recordIndex + 1;
        }
    }

    static DnsSvcbData? ParseServiceBinding(
        DnsSvcbData binding,
        int typeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.Svcb = binding;
        DnsEncodeResult encoded = ResourceRecordCodec.EncodeRecordData(
            typeValue,
            data,
            new DnsEncodeOptions());
        if (!encoded.Success)
        {
            return null;
        }

        DnsDecodeResult decoded = SvcbRecordDataCodec.DecodeRecordData(
            encoded.Bytes,
            0,
            encoded.Bytes.Length);
        if (!decoded.Success || decoded.RecordData == null ||
            decoded.RecordData.Svcb == null)
        {
            return null;
        }

        return decoded.RecordData.Svcb;
    }

    static DnsServiceBindingCandidate SelectAlias(
        List<DnsServiceBindingCandidate> aliases,
        IDnsLookupEntropy entropy)
    {
        if (aliases.Count == 1)
        {
            return aliases[0];
        }

        int selected = entropy.NextIndex(aliases.Count);
        if (selected < 0 || selected >= aliases.Count)
        {
            throw new DnsResolutionException(
                "The lookup entropy source returned an out-of-range value.");
        }

        return aliases[selected];
    }

    static DnsResolveStatus ApplyAlias(
        DnsServiceBindingState state,
        DnsName target,
        int maximumAliasDepth)
    {
        if (state.AliasCount >= maximumAliasDepth)
        {
            return DnsResolveStatus.DepthLimitExceeded;
        }

        if (ContainsName(state.VisitedNames, target))
        {
            return DnsResolveStatus.Loop;
        }

        state.AliasCount = state.AliasCount + 1;
        state.CurrentName = RecordSetOperations.CopyName(target, false);
        state.VisitedNames.Add(RecordSetOperations.CopyName(target, true));
        return DnsResolveStatus.Success;
    }

    static bool ContainsName(List<DnsName> names, DnsName candidate)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (RecordSetOperations.NamesEqual(names[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static DnsServiceResult CreateResult(
        DnsServiceBindingState state,
        DnsResolveStatus status)
    {
        DnsServiceResult result = new DnsServiceResult();
        result.Status = status;
        result.CanonicalName = RecordSetOperations.CopyName(
            state.CurrentName,
            false);
        result.Response = state.Response;
        result.SecurityStatus = state.SecurityStatus;
        result.HasTimeToLive = state.HasTimeToLive;
        result.TimeToLive = state.TimeToLive;
        return result;
    }

    static void AddCompatibleEndpoints(
        DnsServiceResult result,
        List<DnsServiceBindingCandidate> candidates,
        int typeValue)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            DnsServiceBindingCandidate candidate = candidates[index];
            if (MandatoryKeysAreSupported(candidate.ParsedBinding))
            {
                DnsServiceEndpoint endpoint = new DnsServiceEndpoint();
                endpoint.Priority = candidate.Binding.Priority;
                endpoint.Weight = 0;
                endpoint.Port = ReadPort(candidate.ParsedBinding, typeValue);
                if (candidate.Binding.TargetName.Labels.Count == 0)
                {
                    endpoint.Target = RecordSetOperations.CopyName(
                        candidate.Owner,
                        false);
                }
                else
                {
                    endpoint.Target = RecordSetOperations.CopyName(
                        candidate.Binding.TargetName,
                        false);
                }

                endpoint.ServiceBinding = candidate.Binding;
                endpoint.SecurityStatus = result.SecurityStatus;
                endpoint.HasTimeToLive = result.HasTimeToLive;
                endpoint.TimeToLive = result.TimeToLive;
                result.Endpoints.Add(endpoint);
            }

            index = index + 1;
        }
    }

    static bool MandatoryKeysAreSupported(DnsSvcbData binding)
    {
        DnsSvcbParameter? mandatory = FindParameter(binding.Parameters, 0);
        if (mandatory == null)
        {
            return true;
        }

        int index = 0;
        while (index < mandatory.MandatoryKeyValues.Count)
        {
            int key = mandatory.MandatoryKeyValues[index];
            if (key < 1 || key > 8)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int ReadPort(DnsSvcbData binding, int typeValue)
    {
        DnsSvcbParameter? parameter = FindParameter(binding.Parameters, 3);
        if (parameter != null)
        {
            return parameter.Port;
        }

        if (typeValue == 65)
        {
            return 443;
        }

        return 0;
    }

    static DnsSvcbParameter? FindParameter(
        List<DnsSvcbParameter> parameters,
        int key)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            DnsSvcbParameter parameter = parameters[index];
            int parameterKey = LookupRecordOperations.EffectiveValue(
                parameter.KeyValue,
                (int)parameter.Key);
            if (parameterKey == key)
            {
                return parameter;
            }

            index = index + 1;
        }

        return null;
    }

    static void SortEndpoints(List<DnsServiceEndpoint> endpoints)
    {
        int index = 1;
        while (index < endpoints.Count)
        {
            int position = index;
            while (position > 0 &&
                endpoints[position].Priority <
                    endpoints[position - 1].Priority)
            {
                DnsServiceEndpoint swap = endpoints[position - 1];
                endpoints[position - 1] = endpoints[position];
                endpoints[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }
    }

    static void ResolveEndpointAddresses(
        DnsServiceResult result,
        DnsResolveRequest request,
        DnsLookupService service)
    {
        int endpointIndex = 0;
        while (endpointIndex < result.Endpoints.Count)
        {
            DnsServiceEndpoint endpoint = result.Endpoints[endpointIndex];
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
            if (addresses.Status == DnsResolveStatus.Success)
            {
                CopyResolvedAddresses(endpoint, addresses);
                endpoint.SecurityStatus = WorseSecurityStatus(
                    endpoint.SecurityStatus,
                    addresses.SecurityStatus);
                result.SecurityStatus = WorseSecurityStatus(
                    result.SecurityStatus,
                    addresses.SecurityStatus);
                MergeEndpointTimeToLive(endpoint, addresses);
                MergeResultTimeToLive(result, addresses);
            }
            else
            {
                CopyAddressHints(endpoint);
            }

            endpointIndex = endpointIndex + 1;
        }
    }

    static void CopyResolvedAddresses(
        DnsServiceEndpoint endpoint,
        DnsAddressResult addresses)
    {
        int index = 0;
        while (index < addresses.Addresses.Count)
        {
            endpoint.Addresses.Add(RecordSetOperations.CopyBytes(
                addresses.Addresses[index]));
            endpoint.AddressFamilies.Add(addresses.AddressFamilies[index]);
            index = index + 1;
        }
    }

    static void CopyAddressHints(DnsServiceEndpoint endpoint)
    {
        if (endpoint.ServiceBinding == null)
        {
            return;
        }

        DnsSvcbData? parsed = ParseServiceBinding(
            endpoint.ServiceBinding,
            64);
        if (parsed == null)
        {
            return;
        }

        DnsSvcbParameter? ipv4 = FindParameter(parsed.Parameters, 4);
        if (ipv4 != null)
        {
            CopyHintList(endpoint, ipv4.Ipv4Hints, 4);
        }

        DnsSvcbParameter? ipv6 = FindParameter(parsed.Parameters, 6);
        if (ipv6 != null)
        {
            CopyHintList(endpoint, ipv6.Ipv6Hints, 6);
        }
    }

    static void CopyHintList(
        DnsServiceEndpoint endpoint,
        List<byte[]> hints,
        int addressFamily)
    {
        int index = 0;
        while (index < hints.Count)
        {
            endpoint.Addresses.Add(RecordSetOperations.CopyBytes(hints[index]));
            endpoint.AddressFamilies.Add(addressFamily);
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

    static void MergeStateTimeToLive(
        DnsServiceBindingState state,
        bool hasTimeToLive,
        uint timeToLive)
    {
        if (hasTimeToLive &&
            (!state.HasTimeToLive || timeToLive < state.TimeToLive))
        {
            state.HasTimeToLive = true;
            state.TimeToLive = timeToLive;
        }
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
