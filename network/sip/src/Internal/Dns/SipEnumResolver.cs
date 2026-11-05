namespace sip;

internal static class SipEnumResolver
{
    internal static SipResolutionResult Resolve(SipResolutionRequest? request,
        SipDnsOptions? options, ISipRandomSource? random, ISipMonotonicClock? clock)
    {
        if (request == null || options == null || random == null || clock == null)
        {
            return CreateResult(SipResolutionStatus.Failed,
                "A resolution request and DNS options are required.",
                request == null ? null : request.CorrelationId);
        }
        if (!options.EnableEnum)
        {
            return CreateResult(SipResolutionStatus.Failed,
                "ENUM resolution is disabled by resolver policy.", request.CorrelationId);
        }
        string? number = GetGlobalNumber(request);
        if (number == null)
        {
            return CreateResult(SipResolutionStatus.Failed,
                "ENUM resolution requires a valid global E.164 number.",
                request.CorrelationId);
        }

        string domain = CreateEnumDomain(number);
        SipDnsClientResult queried = SipDnsClient.QueryRecords(domain,
            SipDnsRecordKind.Naptr, options, null);
        if (queried.Status != SipResolutionStatus.Success || queried.Response == null)
        {
            return CreateResult(queried.Status, queried.ErrorMessage, request.CorrelationId);
        }

        bool secure = request.Secure || request.Uri != null
            && request.Uri.Scheme == SipUriScheme.Sips;
        SipDnsWireRecord[] records = CopyUsableRecords(
            queried.Response.Records, domain, secure);
        SortRecords(records);
        SipEnumMapping[] mappings = CreateMappings(records, number, secure);
        if (mappings.Length == 0)
        {
            return CreateResult(SipResolutionStatus.NoRecords,
                "The ENUM response contains no permitted SIP URI mappings.",
                request.CorrelationId);
        }

        System.Collections.Generic.List<SipServerCandidate> candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        SipResolutionStatus lastStatus = SipResolutionStatus.NoRecords;
        string? lastError = "The ENUM SIP URI mappings have no reachable targets.";
        int index = 0;
        while (index < mappings.Length)
        {
            SipEnumMapping mapping = mappings[index];
            SipResolutionRequest mappedRequest = new SipResolutionRequest();
            mappedRequest.Uri = mapping.Uri;
            mappedRequest.Transport = request.Transport;
            mappedRequest.Secure = secure;
            mappedRequest.UseEnum = false;
            mappedRequest.CorrelationId = request.CorrelationId;
            SipResolutionResult resolved = SipRfc3263Resolver.Resolve(
                mappedRequest, options, random, clock);
            if (resolved.Status == SipResolutionStatus.Success)
            {
                AppendCandidates(candidates, resolved.Candidates,
                    mapping.TtlSeconds, clock);
            }
            else
            {
                lastStatus = resolved.Status;
                lastError = resolved.ErrorMessage;
            }
            index = index + 1;
        }
        if (candidates.Count == 0)
        {
            return CreateResult(lastStatus, lastError, request.CorrelationId);
        }
        SipResolutionResult result = CreateResult(
            SipResolutionStatus.Success, null, request.CorrelationId);
        result.Candidates = candidates.ToArray();
        return result;
    }

    static string? GetGlobalNumber(SipResolutionRequest request)
    {
        string? source;
        if (request.Uri == null)
        {
            source = request.Domain;
        }
        else if (SipUriValidator.ValidateUriModel(request.Uri).Length != 0)
        {
            return null;
        }
        else if (request.Uri.Scheme == SipUriScheme.Tel)
        {
            source = request.Uri.User;
        }
        else if ((request.Uri.Scheme == SipUriScheme.Sip
                || request.Uri.Scheme == SipUriScheme.Sips)
            && HasPhoneUserParameter(request.Uri))
        {
            source = request.Uri.User;
        }
        else
        {
            return null;
        }
        if (source == null || source.Length < 2 || source[0] != '+')
        {
            return null;
        }
        System.Text.StringBuilder normalized = new System.Text.StringBuilder();
        normalized.Append('+');
        int index = 1;
        while (index < source.Length)
        {
            int character = source[index];
            if (character >= '0' && character <= '9')
            {
                normalized.Append((char)character);
            }
            else if (character != '-' && character != '.'
                && character != '(' && character != ')')
            {
                return null;
            }
            index = index + 1;
        }
        if (normalized.Length < 2 || normalized.Length > 16 || normalized[1] == '0')
        {
            return null;
        }
        return normalized.ToString();
    }

    static bool HasPhoneUserParameter(SipUri uri)
    {
        if (uri.Parameters == null)
        {
            return false;
        }
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && parameter.HasValue
                && SipAscii.EqualsIgnoreCase(parameter.Name, "user")
                && SipAscii.EqualsIgnoreCase(parameter.Value, "phone"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string CreateEnumDomain(string number)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = number.Length - 1;
        while (index > 0)
        {
            builder.Append(number[index]);
            builder.Append('.');
            index = index - 1;
        }
        builder.Append("e164.arpa");
        return builder.ToString();
    }

    static SipDnsWireRecord[] CopyUsableRecords(SipDnsWireRecord[] source,
        string domain, bool secure)
    {
        System.Collections.Generic.List<SipDnsWireRecord> records =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int index = 0;
        while (index < source.Length)
        {
            SipDnsWireRecord? record = source[index];
            if (record != null && record.Section == 0
                && record.Kind == SipDnsRecordKind.Naptr
                && SipAscii.EqualsIgnoreCase(record.Name, domain)
                && SipAscii.EqualsIgnoreCase(record.NaptrFlags, "u")
                && record.NaptrRegexp != null && record.NaptrRegexp.Length > 0
                && record.NaptrReplacement == "."
                && IsPermittedService(record.NaptrServices, secure))
            {
                records.Add(record);
            }
            index = index + 1;
        }
        return records.ToArray();
    }

    static bool IsPermittedService(string? service, bool secure)
    {
        if (SipAscii.EqualsIgnoreCase(service, "E2U+SIPS"))
        {
            return true;
        }
        return !secure && SipAscii.EqualsIgnoreCase(service, "E2U+SIP");
    }

    static void SortRecords(SipDnsWireRecord[] records)
    {
        int index = 1;
        while (index < records.Length)
        {
            SipDnsWireRecord current = records[index];
            int insertion = index;
            while (insertion > 0 && CompareRecords(current, records[insertion - 1]) < 0)
            {
                records[insertion] = records[insertion - 1];
                insertion = insertion - 1;
            }
            records[insertion] = current;
            index = index + 1;
        }
    }

    static int CompareRecords(SipDnsWireRecord left, SipDnsWireRecord right)
    {
        if (left.NaptrOrder != right.NaptrOrder)
        {
            return left.NaptrOrder < right.NaptrOrder ? -1 : 1;
        }
        if (left.NaptrPreference != right.NaptrPreference)
        {
            return left.NaptrPreference < right.NaptrPreference ? -1 : 1;
        }
        return 0;
    }

    static SipEnumMapping[] CreateMappings(SipDnsWireRecord[] records,
        string number, bool secure)
    {
        System.Collections.Generic.List<SipEnumMapping> mappings =
            new System.Collections.Generic.List<SipEnumMapping>();
        int index = 0;
        while (index < records.Length && mappings.Count == 0)
        {
            int selectedOrder = records[index].NaptrOrder;
            while (index < records.Length && records[index].NaptrOrder == selectedOrder)
            {
                SipDnsWireRecord record = records[index];
                SipEnumRegexpResult expanded = SipEnumRegexp.Apply(
                    record.NaptrRegexp, number, SipDefaults.MaxUriBytes);
                SipUri? uri = ParseMappedUri(expanded.Output, record.NaptrServices, secure);
                if (expanded.Success && uri != null && !ContainsUri(mappings, uri))
                {
                    SipEnumMapping mapping = new SipEnumMapping();
                    mapping.Uri = uri;
                    mapping.TtlSeconds = record.TtlSeconds;
                    mappings.Add(mapping);
                }
                index = index + 1;
            }
        }
        return mappings.ToArray();
    }

    static SipUri? ParseMappedUri(string? text, string? service, bool secure)
    {
        if (text == null || text.Length == 0 || text.Length > SipDefaults.MaxUriBytes
            || !IsAsciiUri(text))
        {
            return null;
        }
        SipParseOptions options = new SipParseOptions();
        options.Strict = true;
        options.PreserveOriginal = false;
        options.MaxUriBytes = SipDefaults.MaxUriBytes;
        SipParseResult parsed = SipUriParser.ParseUriBytes(
            System.Text.Encoding.ASCII.GetBytes(text), options);
        SipUri? uri = parsed.Uri;
        if (parsed.Status != SipParseStatus.Complete || uri == null
            || uri.Password != null || uri.Headers == null || uri.Headers.Length != 0
            || SipUriValidator.ValidateUriModel(uri).Length != 0)
        {
            return null;
        }
        if (SipAscii.EqualsIgnoreCase(service, "E2U+SIPS"))
        {
            return uri.Scheme == SipUriScheme.Sips ? uri : null;
        }
        if (secure)
        {
            return null;
        }
        return uri.Scheme == SipUriScheme.Sip ? uri : null;
    }

    static bool IsAsciiUri(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 33 || value[index] > 126)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ContainsUri(System.Collections.Generic.List<SipEnumMapping> mappings,
        SipUri uri)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (SipUriComparison.CompareUriModels(mappings[index].Uri, uri))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AppendCandidates(System.Collections.Generic.List<SipServerCandidate> destination,
        SipServerCandidate[] source, long ttlSeconds, ISipMonotonicClock clock)
    {
        long enumExpiry = ExpiryFromTtl(ttlSeconds, clock);
        int index = 0;
        while (index < source.Length)
        {
            SipServerCandidate? candidate = source[index];
            if (candidate != null && !ContainsCandidate(destination, candidate))
            {
                if (enumExpiry >= 0 && (candidate.ExpiresAtMilliseconds < 0
                    || enumExpiry < candidate.ExpiresAtMilliseconds))
                {
                    candidate.ExpiresAtMilliseconds = enumExpiry;
                }
                destination.Add(candidate);
            }
            index = index + 1;
        }
    }

    static bool ContainsCandidate(
        System.Collections.Generic.List<SipServerCandidate> candidates,
        SipServerCandidate target)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            SipServerCandidate candidate = candidates[index];
            if (SipAscii.EqualsIgnoreCase(candidate.Host, target.Host)
                && candidate.Port == target.Port && candidate.Transport == target.Transport)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static long ExpiryFromTtl(long ttl, ISipMonotonicClock clock)
    {
        if (ttl < 0)
        {
            return -1;
        }
        long now = clock.GetMilliseconds();
        if (ttl > (long.MaxValue - now) / 1000)
        {
            return long.MaxValue;
        }
        return now + ttl * 1000;
    }

    static SipResolutionResult CreateResult(SipResolutionStatus status,
        string? errorMessage, string? correlationId)
    {
        SipResolutionResult result = new SipResolutionResult();
        result.Status = status;
        result.ErrorMessage = errorMessage;
        result.CorrelationId = correlationId;
        return result;
    }
}
