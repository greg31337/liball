using System.Collections.Generic;

namespace dns;

internal static class LookupRecordOperations
{
    internal static DnsResolveResult LookupPointer(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        ValidateRequest(request, service);
        DnsResolveRequest pointerRequest = CreateTypedRequest(
            request,
            DnsRecordType.Ptr,
            12);
        DnsResolveResult result = service.Resolver.Resolve(pointerRequest);
        if (result == null)
        {
            throw new DnsResolutionException(
                "The resolver returned no pointer result.");
        }

        return result;
    }

    internal static DnsMailResult LookupMail(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        ValidateRequest(request, service);
        DnsResolveResult resolved = service.Resolver.Resolve(
            CreateTypedRequest(request, DnsRecordType.Mx, 15));
        if (resolved == null)
        {
            throw new DnsResolutionException(
                "The resolver returned no mail result.");
        }

        DnsMailResult result = new DnsMailResult();
        CopyMailMetadata(result, resolved);
        if (resolved.Status != DnsResolveStatus.Success)
        {
            return result;
        }

        bool valid = CollectMailExchanges(result.Exchanges, resolved);
        if (!valid || result.Exchanges.Count == 0)
        {
            result.Status = DnsResolveStatus.ProtocolError;
            result.Exchanges.Clear();
            return result;
        }

        SortMailExchanges(result.Exchanges);
        return result;
    }

    internal static DnsServiceResult LookupText(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        ValidateRequest(request, service);
        DnsResolveResult resolved = service.Resolver.Resolve(
            CreateTypedRequest(request, DnsRecordType.Txt, 16));
        if (resolved == null)
        {
            throw new DnsResolutionException(
                "The resolver returned no text result.");
        }

        DnsServiceResult result = new DnsServiceResult();
        CopyServiceMetadata(result, resolved);
        if (resolved.Status != DnsResolveStatus.Success)
        {
            return result;
        }

        bool valid = CollectTextValues(result.TextValues, resolved);
        if (!valid || result.TextValues.Count == 0)
        {
            result.Status = DnsResolveStatus.ProtocolError;
            result.TextValues.Clear();
        }

        return result;
    }

    internal static DnsServiceResult LookupServices(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        ValidateRequest(request, service);
        int typeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        if (typeValue == 16)
        {
            return LookupText(request, service);
        }

        if (typeValue == 33)
        {
            return ServiceRecordLookupOperations.LookupServiceRecords(
                request,
                service);
        }

        if (typeValue == 64 || typeValue == 65)
        {
            return service.LookupServiceBindings(request);
        }

        throw new DnsResolutionException(
            "A combined service lookup requires TXT, SRV, SVCB, or HTTPS.");
    }

    internal static DnsResolveRequest CreateTypedRequest(
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

    internal static void CopyServiceMetadata(
        DnsServiceResult destination,
        DnsResolveResult source)
    {
        destination.Status = source.Status;
        destination.CanonicalName = source.CanonicalName;
        destination.Response = source.Response;
        destination.SecurityStatus = source.SecurityStatus;
        destination.HasTimeToLive = source.HasTimeToLive;
        destination.TimeToLive = source.TimeToLive;
    }

    internal static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }

    static bool CollectMailExchanges(
        List<DnsPreferenceNameData> exchanges,
        DnsResolveResult resolved)
    {
        bool valid = true;
        int setIndex = 0;
        while (setIndex < resolved.RecordSets.Count)
        {
            DnsResourceRecordSet set = resolved.RecordSets[setIndex];
            if (EffectiveValue(set.RecordTypeValue, (int)set.RecordType) == 15)
            {
                if (set.Records.Count == 0)
                {
                    valid = false;
                }

                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (EffectiveValue(
                            record.RecordTypeValue,
                            (int)record.RecordType) != 15 ||
                        record.Data == null ||
                        record.Data.PreferenceName == null ||
                        record.Data.PreferenceName.Preference < 0 ||
                        record.Data.PreferenceName.Preference > 65535 ||
                        record.Data.PreferenceName.Name == null ||
                        !record.Data.PreferenceName.Name.IsAbsolute)
                    {
                        valid = false;
                    }
                    else
                    {
                        DnsPreferenceNameData exchange =
                            new DnsPreferenceNameData();
                        exchange.Preference =
                            record.Data.PreferenceName.Preference;
                        exchange.Name = RecordSetOperations.CopyName(
                            record.Data.PreferenceName.Name,
                            false);
                        exchanges.Add(exchange);
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        return valid;
    }

    static void SortMailExchanges(List<DnsPreferenceNameData> exchanges)
    {
        int index = 1;
        while (index < exchanges.Count)
        {
            int position = index;
            while (position > 0 &&
                exchanges[position].Preference <
                    exchanges[position - 1].Preference)
            {
                DnsPreferenceNameData swap = exchanges[position - 1];
                exchanges[position - 1] = exchanges[position];
                exchanges[position] = swap;
                position = position - 1;
            }

            index = index + 1;
        }
    }

    static bool CollectTextValues(
        List<byte[]> values,
        DnsResolveResult resolved)
    {
        bool valid = true;
        int setIndex = 0;
        while (setIndex < resolved.RecordSets.Count)
        {
            DnsResourceRecordSet set = resolved.RecordSets[setIndex];
            if (EffectiveValue(set.RecordTypeValue, (int)set.RecordType) == 16)
            {
                if (set.Records.Count == 0)
                {
                    valid = false;
                }

                int recordIndex = 0;
                while (recordIndex < set.Records.Count)
                {
                    DnsResourceRecord record = set.Records[recordIndex];
                    if (EffectiveValue(
                            record.RecordTypeValue,
                            (int)record.RecordType) != 16 ||
                        record.Data == null ||
                        record.Data.CharacterStrings == null ||
                        record.Data.CharacterStrings.Strings == null ||
                        record.Data.CharacterStrings.Strings.Count == 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        int stringIndex = 0;
                        while (stringIndex <
                            record.Data.CharacterStrings.Strings.Count)
                        {
                            byte[] text = record.Data.CharacterStrings
                                .Strings[stringIndex];
                            if (text == null || text.Length > 255)
                            {
                                valid = false;
                            }
                            else
                            {
                                values.Add(
                                    RecordSetOperations.CopyBytes(text));
                            }

                            stringIndex = stringIndex + 1;
                        }
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            setIndex = setIndex + 1;
        }

        return valid;
    }

    static void CopyMailMetadata(
        DnsMailResult destination,
        DnsResolveResult source)
    {
        destination.Status = source.Status;
        destination.CanonicalName = source.CanonicalName;
        destination.Response = source.Response;
        destination.SecurityStatus = source.SecurityStatus;
        destination.HasTimeToLive = source.HasTimeToLive;
        destination.TimeToLive = source.TimeToLive;
    }

    internal static void ValidateRequest(
        DnsResolveRequest request,
        DnsLookupService service)
    {
        if (request == null || service == null ||
            service.Resolver == null || service.Entropy == null)
        {
            throw new DnsResolutionException(
                "A lookup request, resolver, and entropy source are required.");
        }

        if (request.Name == null || request.Options == null)
        {
            throw new DnsResolutionException(
                "A lookup name and resolver options are required.");
        }
    }
}
