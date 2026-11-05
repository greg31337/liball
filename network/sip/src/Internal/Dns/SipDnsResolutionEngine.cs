namespace sip;

internal class SipDnsResolutionEngine
{
    const int MaximumCacheEntries = 256;
    const int NegativeCacheMilliseconds = 30000;

    ISipRandomSource random;
    ISipMonotonicClock clock;
    object gate = new object();
    System.Collections.Generic.List<SipDnsCacheEntry> cache =
        new System.Collections.Generic.List<SipDnsCacheEntry>();
    System.Collections.Generic.List<string> failedCandidates =
        new System.Collections.Generic.List<string>();

    internal SipDnsResolutionEngine(ISipRandomSource randomSource,
        ISipMonotonicClock monotonicClock)
    {
        random = randomSource;
        clock = monotonicClock;
    }

    internal SipResolutionResult Resolve(SipResolutionRequest? request, SipDnsOptions? options)
    {
        string key = CreateCacheKey(request, options);
        if (options != null && options.EnableCache)
        {
            SipResolutionResult? cached = ReadCache(key,
                request == null ? null : request.CorrelationId);
            if (cached != null)
            {
                ApplyFailureOrder(cached);
                return cached;
            }
        }

        SipResolutionResult result = SipRfc3263Resolver.Resolve(request, options, random, clock);
        if (options != null && options.EnableCache)
        {
            StoreCache(key, result);
        }
        ApplyFailureOrder(result);
        return result;
    }

    internal void ReportFailure(SipServerCandidate? candidate)
    {
        string? key = CreateCandidateKey(candidate);
        if (key == null)
        {
            throw new System.ArgumentException("A complete DNS candidate is required.", "candidate");
        }
        lock (gate)
        {
            int existing = FindString(failedCandidates, key);
            if (existing >= 0)
            {
                failedCandidates.RemoveAt(existing);
            }
            failedCandidates.Add(key);
            if (failedCandidates.Count > MaximumCacheEntries)
            {
                failedCandidates.RemoveAt(0);
            }
        }
    }

    internal void Flush()
    {
        lock (gate)
        {
            cache.Clear();
            failedCandidates.Clear();
        }
    }

    static string CreateCacheKey(SipResolutionRequest? request, SipDnsOptions? options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (request == null)
        {
            builder.Append("request:null");
        }
        else
        {
            builder.Append("domain:");
            builder.Append(request.Domain == null ? "" : request.Domain.ToLowerInvariant());
            builder.Append("|uri:");
            builder.Append(FormatUri(request.Uri));
            builder.Append("|transport:");
            builder.Append(((int)request.Transport).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("|secure:");
            builder.Append(request.Secure ? "1" : "0");
            builder.Append("|enum:");
            builder.Append(request.UseEnum ? "1" : "0");
        }
        if (options != null)
        {
            builder.Append("|cname:");
            builder.Append(options.MaxCnameDepth.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("|records:");
            builder.Append(options.MaxRecordCount.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("|enum-enabled:");
            builder.Append(options.EnableEnum ? "1" : "0");
            if (options.NameServers != null)
            {
                int index = 0;
                while (index < options.NameServers.Length)
                {
                    SipEndpointAddress? server = options.NameServers[index];
                    builder.Append("|ns:");
                    if (server != null)
                    {
                        builder.Append(server.Host == null ? "" : server.Host.ToLowerInvariant());
                        builder.Append(':');
                        builder.Append(server.Port.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));
                    }
                    index = index + 1;
                }
            }
        }
        return builder.ToString();
    }

    static string FormatUri(SipUri? uri)
    {
        if (uri == null)
        {
            return "";
        }
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = true;
        options.PreserveOriginal = false;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipWriteResult formatted = SipUriFormatter.FormatUriModel(uri, options);
        return formatted.Success ? System.Text.Encoding.UTF8.GetString(formatted.Bytes) : "invalid";
    }

    SipResolutionResult? ReadCache(string key, string? correlationId)
    {
        long now = clock.GetMilliseconds();
        lock (gate)
        {
            int index = cache.Count - 1;
            while (index >= 0)
            {
                SipDnsCacheEntry entry = cache[index];
                if (entry.ExpiresAtMilliseconds <= now)
                {
                    cache.RemoveAt(index);
                }
                else if (entry.Key == key && entry.Result != null)
                {
                    return CopyResult(entry.Result, correlationId);
                }
                index = index - 1;
            }
        }
        return null;
    }

    void StoreCache(string key, SipResolutionResult result)
    {
        long expiry = GetCacheExpiry(result);
        long now = clock.GetMilliseconds();
        if (expiry <= now)
        {
            return;
        }
        SipDnsCacheEntry entry = new SipDnsCacheEntry();
        entry.Key = key;
        entry.Result = CopyResult(result, null);
        entry.ExpiresAtMilliseconds = expiry;
        lock (gate)
        {
            int existing = FindCacheEntry(key);
            if (existing >= 0)
            {
                cache.RemoveAt(existing);
            }
            cache.Add(entry);
            if (cache.Count > MaximumCacheEntries)
            {
                cache.RemoveAt(0);
            }
        }
    }

    long GetCacheExpiry(SipResolutionResult result)
    {
        long now = clock.GetMilliseconds();
        if (result.Status == SipResolutionStatus.NoRecords)
        {
            return now > long.MaxValue - NegativeCacheMilliseconds
                ? long.MaxValue : now + NegativeCacheMilliseconds;
        }
        if (result.Status != SipResolutionStatus.Success || result.Candidates == null
            || result.Candidates.Length == 0)
        {
            return -1;
        }
        long expiry = -1;
        int index = 0;
        while (index < result.Candidates.Length)
        {
            SipServerCandidate? candidate = result.Candidates[index];
            if (candidate != null && candidate.ExpiresAtMilliseconds >= 0
                && (expiry < 0 || candidate.ExpiresAtMilliseconds < expiry))
            {
                expiry = candidate.ExpiresAtMilliseconds;
            }
            index = index + 1;
        }
        return expiry;
    }

    void ApplyFailureOrder(SipResolutionResult result)
    {
        if (result.Candidates == null || result.Candidates.Length < 2)
        {
            return;
        }
        System.Collections.Generic.List<SipServerCandidate> available =
            new System.Collections.Generic.List<SipServerCandidate>();
        System.Collections.Generic.List<SipServerCandidate> failed =
            new System.Collections.Generic.List<SipServerCandidate>();
        lock (gate)
        {
            int index = 0;
            while (index < result.Candidates.Length)
            {
                SipServerCandidate candidate = result.Candidates[index];
                string? key = CreateCandidateKey(candidate);
                if (key != null && FindString(failedCandidates, key) >= 0)
                {
                    failed.Add(candidate);
                }
                else
                {
                    available.Add(candidate);
                }
                index = index + 1;
            }
        }
        AppendCandidateList(available, failed);
        result.Candidates = available.ToArray();
    }

    static string? CreateCandidateKey(SipServerCandidate? candidate)
    {
        if (candidate == null || candidate.Host == null || candidate.Host.Length == 0
            || candidate.Port <= 0 || candidate.Port > 65535
            || candidate.Transport == SipTransportKind.Unknown)
        {
            return null;
        }
        return candidate.Host.ToLowerInvariant() + ":"
            + candidate.Port.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":"
            + ((int)candidate.Transport).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    int FindCacheEntry(string key)
    {
        int index = 0;
        while (index < cache.Count)
        {
            if (cache[index].Key == key)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int FindString(System.Collections.Generic.List<string> values, string target)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static void AppendCandidateList(System.Collections.Generic.List<SipServerCandidate> destination,
        System.Collections.Generic.List<SipServerCandidate> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static SipResolutionResult CopyResult(SipResolutionResult source, string? correlationId)
    {
        SipResolutionResult copy = new SipResolutionResult();
        copy.Status = source.Status;
        copy.ErrorMessage = source.ErrorMessage;
        copy.CorrelationId = correlationId;
        if (source.Candidates == null)
        {
            copy.Candidates = new SipServerCandidate[0];
            return copy;
        }
        copy.Candidates = new SipServerCandidate[source.Candidates.Length];
        int index = 0;
        while (index < source.Candidates.Length)
        {
            SipServerCandidate item = new SipServerCandidate();
            SipServerCandidate? original = source.Candidates[index];
            if (original != null)
            {
                item.Host = original.Host;
                item.Port = original.Port;
                item.Transport = original.Transport;
                item.Priority = original.Priority;
                item.Weight = original.Weight;
                item.RecordKind = original.RecordKind;
                item.ExpiresAtMilliseconds = original.ExpiresAtMilliseconds;
                item.Secure = original.Secure;
            }
            copy.Candidates[index] = item;
            index = index + 1;
        }
        return copy;
    }
}
