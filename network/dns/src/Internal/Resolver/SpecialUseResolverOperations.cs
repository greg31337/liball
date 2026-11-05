using System.Collections.Generic;

namespace dns;

internal static class SpecialUseResolverOperations
{
    internal static DnsResolveResult? ResolveInputName(
        DnsResolveRequest request)
    {
        ValidatePolicies(request.Options.SpecialUsePolicies);
        RawDnsNameResult validated = RawDnsName.ValidateName(request.Name);
        if (!validated.Success || validated.Name == null)
        {
            throw new DnsResolutionException(
                "The resolver request name is invalid.");
        }

        DnsName absoluteName = RecordSetOperations.CopyName(
            validated.Name,
            false);
        absoluteName.IsAbsolute = true;
        return ResolveCandidateName(request, absoluteName);
    }

    internal static DnsResolveResult? ResolveCandidateName(
        DnsResolveRequest request,
        DnsName candidate)
    {
        DnsResolveResult? custom = ResolveCustomPolicy(request, candidate);
        if (custom != null)
        {
            return custom;
        }

        if (request.Options.RejectOnionNames &&
            HasSuffix(candidate, CreateOnionName()))
        {
            return CreateTerminalResult(
                candidate,
                DnsResolveStatus.NameError,
                DnsSecurityStatus.Insecure);
        }

        if (request.Options.HandleLocalhostNames &&
            HasSuffix(candidate, CreateLocalhostName()))
        {
            return CreateLocalhostResult(request, candidate);
        }

        return null;
    }

    static void ValidatePolicies(List<DnsSpecialUsePolicy> policies)
    {
        if (policies == null)
        {
            throw new DnsResolutionException(
                "Special-use policies are required.");
        }

        int index = 0;
        while (index < policies.Count)
        {
            DnsSpecialUsePolicy policy = policies[index];
            if (policy == null || policy.Suffix == null ||
                policy.RecordSets == null)
            {
                throw new DnsResolutionException(
                    "Each special-use policy requires a suffix and record sets.");
            }

            RawDnsNameResult suffix = RawDnsName.ValidateName(policy.Suffix);
            int status = (int)policy.Status;
            if (!suffix.Success || suffix.Name == null ||
                !suffix.Name.IsAbsolute)
            {
                throw new DnsResolutionException(
                    "A special-use policy suffix must be an absolute DNS name.");
            }

            if (status < (int)DnsResolveStatus.Success ||
                status > (int)DnsResolveStatus.Unsupported)
            {
                throw new DnsResolutionException(
                    "A special-use policy status is not recognized.");
            }

            index = index + 1;
        }
    }

    static DnsResolveResult? ResolveCustomPolicy(
        DnsResolveRequest request,
        DnsName candidate)
    {
        int index = 0;
        while (index < request.Options.SpecialUsePolicies.Count)
        {
            DnsSpecialUsePolicy policy =
                request.Options.SpecialUsePolicies[index];
            if (PolicyMatches(policy, candidate))
            {
                return CreatePolicyResult(request, candidate, policy);
            }

            index = index + 1;
        }

        return null;
    }

    static bool PolicyMatches(
        DnsSpecialUsePolicy policy,
        DnsName candidate)
    {
        DnsNameOperationResult equal = DnsNameOperations.NamesEqual(
            candidate,
            policy.Suffix);
        if (!equal.Success)
        {
            throw new DnsResolutionException(
                "A special-use policy contains an invalid DNS name.");
        }

        if (equal.BooleanValue)
        {
            return true;
        }

        if (!policy.ApplyToSubdomains)
        {
            return false;
        }

        return HasSuffix(candidate, policy.Suffix);
    }

    static bool HasSuffix(DnsName name, DnsName suffix)
    {
        DnsNameOperationResult result = DnsNameOperations.HasNameSuffix(
            name,
            suffix);
        if (!result.Success)
        {
            throw new DnsResolutionException(
                "Special-use policy matching requires valid DNS names.");
        }

        return result.BooleanValue;
    }

    static DnsResolveResult CreatePolicyResult(
        DnsResolveRequest request,
        DnsName candidate,
        DnsSpecialUsePolicy policy)
    {
        if (policy.Status != DnsResolveStatus.Success)
        {
            return CreateTerminalResult(
                candidate,
                policy.Status,
                policy.SecurityStatus);
        }

        DnsResolveResult result = CreateTerminalResult(
            candidate,
            DnsResolveStatus.Success,
            policy.SecurityStatus);
        CopyPolicyRecordSets(result.RecordSets, request, candidate, policy);
        if (result.RecordSets.Count == 0)
        {
            result.Status = DnsResolveStatus.NoData;
            return result;
        }

        RecordSetOperations.SetResultTtl(result);
        return result;
    }

    static void CopyPolicyRecordSets(
        List<DnsResourceRecordSet> destination,
        DnsResolveRequest request,
        DnsName candidate,
        DnsSpecialUsePolicy policy)
    {
        int requestedType = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        int requestedClass = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        int index = 0;
        while (index < policy.RecordSets.Count)
        {
            DnsResourceRecordSet source = policy.RecordSets[index];
            if (source == null || source.Records == null)
            {
                throw new DnsResolutionException(
                    "A special-use policy record set is incomplete.");
            }

            int setType = EffectiveValue(
                source.RecordTypeValue,
                (int)source.RecordType);
            int setClass = EffectiveValue(
                source.ClassValue,
                (int)source.Class);
            if ((requestedType == 255 || requestedType == setType) &&
                requestedClass == setClass)
            {
                destination.Add(CopyPolicyRecordSet(
                    source,
                    candidate,
                    setType,
                    setClass));
            }

            index = index + 1;
        }
    }

    static DnsResourceRecordSet CopyPolicyRecordSet(
        DnsResourceRecordSet source,
        DnsName candidate,
        int setType,
        int setClass)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(candidate, false);
        result.RecordType = (DnsRecordType)setType;
        result.RecordTypeValue = setType;
        result.Class = (DnsClass)setClass;
        result.ClassValue = setClass;
        result.TimeToLive = source.TimeToLive;
        int index = 0;
        while (index < source.Records.Count)
        {
            DnsResourceRecord record = source.Records[index];
            if (record == null ||
                EffectiveValue(
                    record.RecordTypeValue,
                    (int)record.RecordType) != setType ||
                EffectiveValue(
                    record.ClassValue,
                    (int)record.Class) != setClass)
            {
                throw new DnsResolutionException(
                    "A special-use policy record does not match its record set.");
            }

            DnsResourceRecord copy = RecordSetOperations.CopyRecord(
                record,
                candidate);
            copy.RecordType = result.RecordType;
            copy.RecordTypeValue = setType;
            copy.Class = result.Class;
            copy.ClassValue = setClass;
            result.Records.Add(copy);
            if (record.TimeToLive < result.TimeToLive)
            {
                result.TimeToLive = record.TimeToLive;
            }

            index = index + 1;
        }

        if (result.Records.Count == 0)
        {
            throw new DnsResolutionException(
                "A successful special-use policy record set cannot be empty.");
        }

        return result;
    }

    static DnsResolveResult CreateLocalhostResult(
        DnsResolveRequest request,
        DnsName candidate)
    {
        DnsResolveResult result = CreateTerminalResult(
            candidate,
            DnsResolveStatus.NoData,
            DnsSecurityStatus.Insecure);
        int classValue = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        if (classValue != 1)
        {
            return result;
        }

        int typeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        if (typeValue == 1 || typeValue == 255)
        {
            result.RecordSets.Add(CreateIpv4Set(candidate));
        }

        if (typeValue == 28 || typeValue == 255)
        {
            result.RecordSets.Add(CreateIpv6Set(candidate));
        }

        if (result.RecordSets.Count > 0)
        {
            result.Status = DnsResolveStatus.Success;
            RecordSetOperations.SetResultTtl(result);
        }

        return result;
    }

    static DnsResourceRecordSet CreateIpv4Set(DnsName owner)
    {
        DnsAddressData address = new DnsAddressData();
        address.Address = [127, 0, 0, 1];
        return CreateAddressSet(owner, DnsRecordType.A, 1, address);
    }

    static DnsResourceRecordSet CreateIpv6Set(DnsName owner)
    {
        DnsAddressData address = new DnsAddressData();
        address.Address = [0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1];
        return CreateAddressSet(owner, DnsRecordType.Aaaa, 28, address);
    }

    static DnsResourceRecordSet CreateAddressSet(
        DnsName owner,
        DnsRecordType recordType,
        int recordTypeValue,
        DnsAddressData address)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = RecordSetOperations.CopyName(owner, false);
        record.RecordType = recordType;
        record.RecordTypeValue = recordTypeValue;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 0;
        record.Data = new DnsRecordData();
        record.Data.Address = address;
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = RecordSetOperations.CopyName(owner, false);
        result.RecordType = recordType;
        result.RecordTypeValue = recordTypeValue;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 0;
        result.Records.Add(record);
        return result;
    }

    static DnsResolveResult CreateTerminalResult(
        DnsName name,
        DnsResolveStatus status,
        DnsSecurityStatus securityStatus)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = status;
        result.CanonicalName = RecordSetOperations.CopyName(name, false);
        result.SecurityStatus = securityStatus;
        return result;
    }

    static DnsName CreateOnionName()
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        result.Labels.Add([111, 110, 105, 111, 110]);
        return result;
    }

    static DnsName CreateLocalhostName()
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        result.Labels.Add([108, 111, 99, 97, 108, 104, 111, 115, 116]);
        return result;
    }

    static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }
}
