using System.Collections.Generic;

namespace dns;

internal class DnsAddressCandidate
{
    internal byte[] Address = [];
    internal int AddressFamily;
    internal int QueryFamily;
    internal int Sequence;
    internal int Precedence;
    internal int Scope;
    internal DnsResolveResult Source = new DnsResolveResult();
}

internal class DnsAddressCollection
{
    internal List<DnsAddressCandidate> Candidates =
        new List<DnsAddressCandidate>();
    internal bool Ipv4Invalid;
    internal bool Ipv6Invalid;
}

internal static class AddressLookupOperations
{
    internal static DnsAddressResult LookupAddresses(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        ValidateRequest(request, service);
        DnsResolveRequest ipv4Request = CreateFamilyRequest(
            request,
            DnsRecordType.A,
            1);
        DnsResolveRequest ipv6Request = CreateFamilyRequest(
            request,
            DnsRecordType.Aaaa,
            28);
        DnsResolveResult ipv4 = service.Resolver.Resolve(ipv4Request);
        DnsResolveResult ipv6 = service.Resolver.Resolve(ipv6Request);
        if (ipv4 == null || ipv6 == null)
        {
            throw new DnsResolutionException(
                "The resolver returned no address result.");
        }

        DnsAddressCollection collection = new DnsAddressCollection();
        collection.Ipv4Invalid = !CollectFamily(
            collection.Candidates,
            ipv4,
            1,
            4,
            4);
        collection.Ipv6Invalid = !CollectFamily(
            collection.Candidates,
            ipv6,
            28,
            6,
            16);
        DnsAddressResult result = new DnsAddressResult();
        if (collection.Candidates.Count == 0)
        {
            DnsResolveStatus ipv4Status = EffectiveStatus(
                ipv4,
                collection.Ipv4Invalid);
            DnsResolveStatus ipv6Status = EffectiveStatus(
                ipv6,
                collection.Ipv6Invalid);
            result.Status = SelectEmptyStatus(ipv4Status, ipv6Status);
            DnsResolveResult source = SelectStatusSource(
                ipv4,
                ipv6,
                result.Status,
                ipv4Status,
                ipv6Status);
            CopyMetadata(result, source);
            return result;
        }

        SortCandidates(collection.Candidates);
        result.Status = DnsResolveStatus.Success;
        int index = 0;
        while (index < collection.Candidates.Count)
        {
            DnsAddressCandidate candidate = collection.Candidates[index];
            result.Addresses.Add(
                RecordSetOperations.CopyBytes(candidate.Address));
            result.AddressFamilies.Add(candidate.AddressFamily);
            index = index + 1;
        }

        DnsResolveResult primary = collection.Candidates[0].Source;
        CopyMetadata(result, primary);
        result.Status = DnsResolveStatus.Success;
        ApplyAggregateMetadata(result, collection.Candidates);
        return result;
    }

    static bool CollectFamily(
        List<DnsAddressCandidate> candidates,
        DnsResolveResult source,
        int recordTypeValue,
        int addressFamily,
        int addressLength)
    {
        if (source.Status != DnsResolveStatus.Success)
        {
            return true;
        }

        int initialCount = candidates.Count;
        bool valid = true;
        int setIndex = 0;
        while (setIndex < source.RecordSets.Count)
        {
            DnsResourceRecordSet set = source.RecordSets[setIndex];
            int setType = EffectiveValue(
                set.RecordTypeValue,
                (int)set.RecordType);
            if (setType == recordTypeValue)
            {
                if (set.Records.Count == 0)
                {
                    valid = false;
                }

                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    int recordType = EffectiveValue(
                        record.RecordTypeValue,
                        (int)record.RecordType);
                    if (recordType != recordTypeValue ||
                        record.Data == null ||
                        record.Data.Address == null ||
                        record.Data.Address.Address == null ||
                        record.Data.Address.Address.Length != addressLength)
                    {
                        valid = false;
                    }
                    else
                    {
                        AddCandidate(
                            candidates,
                            record.Data.Address.Address,
                            addressFamily,
                            source);
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        if (candidates.Count == initialCount)
        {
            valid = false;
        }

        return valid;
    }

    static void AddCandidate(
        List<DnsAddressCandidate> candidates,
        byte[] address,
        int addressFamily,
        DnsResolveResult source)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].AddressFamily == addressFamily &&
                BytesEqual(candidates[index].Address, address))
            {
                return;
            }

            index = index + 1;
        }

        DnsAddressCandidate candidate = new DnsAddressCandidate();
        candidate.Address = RecordSetOperations.CopyBytes(address);
        candidate.AddressFamily = addressFamily;
        candidate.QueryFamily = addressFamily;
        candidate.Sequence = candidates.Count;
        candidate.Precedence = ReadPrecedence(address, addressFamily);
        candidate.Scope = ReadScope(address, addressFamily);
        candidate.Source = source;
        candidates.Add(candidate);
    }

    static void SortCandidates(List<DnsAddressCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            int position = index;
            while (position > 0 && CompareCandidates(
                candidates[position],
                candidates[position - 1]) < 0)
            {
                DnsAddressCandidate swap = candidates[position - 1];
                candidates[position - 1] = candidates[position];
                candidates[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }
    }

    static int CompareCandidates(
        DnsAddressCandidate left,
        DnsAddressCandidate right)
    {
        if (left.Precedence > right.Precedence)
        {
            return -1;
        }

        if (left.Precedence < right.Precedence)
        {
            return 1;
        }

        if (left.Scope < right.Scope)
        {
            return -1;
        }

        if (left.Scope > right.Scope)
        {
            return 1;
        }

        if (left.Sequence < right.Sequence)
        {
            return -1;
        }

        if (left.Sequence > right.Sequence)
        {
            return 1;
        }

        return 0;
    }

    static int ReadPrecedence(byte[] address, int addressFamily)
    {
        if (addressFamily == 4)
        {
            return 35;
        }

        if (IsIpv6Loopback(address))
        {
            return 50;
        }

        if (IsIpv4Mapped(address))
        {
            return 35;
        }

        if (address[0] == 32 && address[1] == 2)
        {
            return 30;
        }

        if (address[0] == 32 && address[1] == 1 &&
            address[2] == 0 && address[3] == 0)
        {
            return 5;
        }

        if ((address[0] & 254) == 252)
        {
            return 3;
        }

        if (FirstBytesZero(address, 12))
        {
            return 1;
        }

        if (address[0] == 254 && (address[1] & 192) == 192)
        {
            return 1;
        }

        if (address[0] == 63 && address[1] == 254)
        {
            return 1;
        }

        return 40;
    }

    static int ReadScope(byte[] address, int addressFamily)
    {
        if (addressFamily == 4)
        {
            if (address[0] == 127 ||
                (address[0] == 169 && address[1] == 254))
            {
                return 2;
            }

            return 14;
        }

        if (address[0] == 255)
        {
            return address[1] & 15;
        }

        if (IsIpv6Loopback(address) ||
            (address[0] == 254 && (address[1] & 192) == 128))
        {
            return 2;
        }

        if (address[0] == 254 && (address[1] & 192) == 192)
        {
            return 5;
        }

        return 14;
    }

    static bool IsIpv6Loopback(byte[] address)
    {
        return FirstBytesZero(address, 15) && address[15] == 1;
    }

    static bool IsIpv4Mapped(byte[] address)
    {
        return FirstBytesZero(address, 10) &&
            address[10] == 255 && address[11] == 255;
    }

    static bool FirstBytesZero(byte[] address, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (address[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void ApplyAggregateMetadata(
        DnsAddressResult result,
        List<DnsAddressCandidate> candidates)
    {
        bool usedIpv4 = false;
        bool usedIpv6 = false;
        int index = 0;
        while (index < candidates.Count)
        {
            DnsAddressCandidate candidate = candidates[index];
            bool newSource = candidate.QueryFamily == 4 && !usedIpv4;
            if (candidate.QueryFamily == 6 && !usedIpv6)
            {
                newSource = true;
            }

            if (newSource)
            {
                if (candidate.QueryFamily == 4)
                {
                    usedIpv4 = true;
                }
                else
                {
                    usedIpv6 = true;
                }

                MergeSecurityStatus(result, candidate.Source.SecurityStatus);
                MergeTimeToLive(
                    result,
                    candidate.Source.HasTimeToLive,
                    candidate.Source.TimeToLive);
            }

            index = index + 1;
        }
    }

    static void CopyMetadata(
        DnsAddressResult destination,
        DnsResolveResult source)
    {
        destination.CanonicalName = source.CanonicalName;
        destination.Response = source.Response;
        destination.SecurityStatus = source.SecurityStatus;
        destination.HasTimeToLive = source.HasTimeToLive;
        destination.TimeToLive = source.TimeToLive;
        destination.AliasSteps = new List<DnsAliasStep>();
        int index = 0;
        while (index < source.AliasSteps.Count)
        {
            destination.AliasSteps.Add(source.AliasSteps[index]);
            index = index + 1;
        }
    }

    static void MergeSecurityStatus(
        DnsAddressResult result,
        DnsSecurityStatus status)
    {
        if (SecurityRank(status) > SecurityRank(result.SecurityStatus))
        {
            result.SecurityStatus = status;
        }
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

    static void MergeTimeToLive(
        DnsAddressResult result,
        bool hasTimeToLive,
        uint timeToLive)
    {
        if (hasTimeToLive &&
            (!result.HasTimeToLive || timeToLive < result.TimeToLive))
        {
            result.HasTimeToLive = true;
            result.TimeToLive = timeToLive;
        }
    }

    static DnsResolveStatus EffectiveStatus(
        DnsResolveResult result,
        bool invalid)
    {
        if (invalid && result.Status == DnsResolveStatus.Success)
        {
            return DnsResolveStatus.ProtocolError;
        }

        return result.Status;
    }

    static DnsResolveStatus SelectEmptyStatus(
        DnsResolveStatus ipv4,
        DnsResolveStatus ipv6)
    {
        if (IsNegativeStatus(ipv4) && IsNegativeStatus(ipv6))
        {
            if (ipv4 == DnsResolveStatus.NoData ||
                ipv6 == DnsResolveStatus.NoData)
            {
                return DnsResolveStatus.NoData;
            }

            return DnsResolveStatus.NameError;
        }

        if (ipv4 == DnsResolveStatus.Cancelled ||
            ipv6 == DnsResolveStatus.Cancelled)
        {
            return DnsResolveStatus.Cancelled;
        }

        if (ipv4 == DnsResolveStatus.TimedOut ||
            ipv6 == DnsResolveStatus.TimedOut)
        {
            return DnsResolveStatus.TimedOut;
        }

        if (ipv4 == DnsResolveStatus.ProtocolError ||
            ipv6 == DnsResolveStatus.ProtocolError)
        {
            return DnsResolveStatus.ProtocolError;
        }

        if (ipv4 == DnsResolveStatus.Loop ||
            ipv6 == DnsResolveStatus.Loop)
        {
            return DnsResolveStatus.Loop;
        }

        if (ipv4 == DnsResolveStatus.DepthLimitExceeded ||
            ipv6 == DnsResolveStatus.DepthLimitExceeded)
        {
            return DnsResolveStatus.DepthLimitExceeded;
        }

        if (ipv4 == DnsResolveStatus.Refused ||
            ipv6 == DnsResolveStatus.Refused)
        {
            return DnsResolveStatus.Refused;
        }

        if (ipv4 == DnsResolveStatus.ServerFailure ||
            ipv6 == DnsResolveStatus.ServerFailure)
        {
            return DnsResolveStatus.ServerFailure;
        }

        if (ipv4 == DnsResolveStatus.Unsupported ||
            ipv6 == DnsResolveStatus.Unsupported)
        {
            return DnsResolveStatus.Unsupported;
        }

        if (IsNegativeStatus(ipv4))
        {
            return ipv6;
        }

        return ipv4;
    }

    static DnsResolveResult SelectStatusSource(
        DnsResolveResult ipv4,
        DnsResolveResult ipv6,
        DnsResolveStatus selected,
        DnsResolveStatus ipv4Status,
        DnsResolveStatus ipv6Status)
    {
        if (ipv4Status == selected)
        {
            return ipv4;
        }

        if (ipv6Status == selected)
        {
            return ipv6;
        }

        if (IsNegativeStatus(ipv4Status) &&
            IsNegativeStatus(ipv6Status) &&
            selected == DnsResolveStatus.NoData)
        {
            if (ipv4Status == DnsResolveStatus.NoData)
            {
                return ipv4;
            }

            return ipv6;
        }

        return ipv4;
    }

    static bool IsNegativeStatus(DnsResolveStatus status)
    {
        return status == DnsResolveStatus.NoData ||
            status == DnsResolveStatus.NameError;
    }

    static DnsResolveRequest CreateFamilyRequest(
        DnsResolveRequest source,
        DnsRecordType recordType,
        int recordTypeValue)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = RecordSetOperations.CopyName(source.Name, false);
        result.RecordType = recordType;
        result.RecordTypeValue = recordTypeValue;
        result.Class = source.Class;
        result.ClassValue = EffectiveValue(
            source.ClassValue,
            (int)source.Class);
        result.Options = source.Options;
        result.ClientSubnet = source.ClientSubnet;
        result.CancellationRequested = source.CancellationRequested;
        return result;
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }

    static void ValidateRequest(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        if (request == null || service == null || service.Resolver == null)
        {
            throw new DnsResolutionException(
                "An address request and resolver are required.");
        }

        if (request.Name == null || request.Options == null)
        {
            throw new DnsResolutionException(
                "An address name and resolver options are required.");
        }
    }
}
