namespace sip;

internal static class SipRfc3263Resolver
{
    internal static SipResolutionResult Resolve(SipResolutionRequest? request,
        SipDnsOptions? options, ISipRandomSource? random, ISipMonotonicClock? clock)
    {
        if (request != null && request.UseEnum)
        {
            return SipEnumResolver.Resolve(request, options, random, clock);
        }
        SipDnsTarget target = CreateTarget(request);
        if (!target.Success || options == null || request == null || random == null || clock == null)
        {
            return CreateResult(SipResolutionStatus.Failed, target.ErrorMessage == null
                ? "A resolution request and DNS options are required." : target.ErrorMessage,
                request == null ? null : request.CorrelationId);
        }
        if (!ValidateOptions(options))
        {
            return CreateResult(SipResolutionStatus.Failed,
                "The DNS resolver options are invalid.", request.CorrelationId);
        }
        if (target.Domain == null)
        {
            return CreateResult(SipResolutionStatus.Failed,
                "A resolution domain is required.", request.CorrelationId);
        }

        if (IsNumericAddress(target.Domain))
        {
            SipTransportKind transport = target.Transport == SipTransportKind.Unknown
                ? DefaultTransport(target.Secure) : target.Transport;
            int port = target.ExplicitPort ? target.Port : DefaultPort(transport, target.Secure);
            SipServerCandidate candidate = CreateNumericCandidate(target.Domain, port,
                transport, target.Secure);
            SipResolutionResult numeric = CreateResult(SipResolutionStatus.Success,
                null, request.CorrelationId);
            numeric.Candidates = new SipServerCandidate[] { candidate };
            return numeric;
        }

        if (SipDnsWireCodec.NormalizeQueryName(target.Domain) == null)
        {
            return CreateResult(SipResolutionStatus.Failed,
                "The resolution domain is invalid.", request.CorrelationId);
        }

        SipDnsCandidateResult resolved;
        if (target.ExplicitPort)
        {
            SipTransportKind transport = target.Transport == SipTransportKind.Unknown
                ? DefaultTransport(target.Secure) : target.Transport;
            resolved = ResolveAddresses(target.Domain, transport, target.Port,
                target.Secure, 0, 0, -1, options, clock);
        }
        else if (target.ExplicitTransport)
        {
            string serviceName = CreateServiceName(target.Domain, target.Transport,
                target.Secure);
            resolved = ResolveService(serviceName, target.Transport, target.Secure,
                -1, options, random, clock);
            if (!resolved.FoundServiceRecords && resolved.Status == SipResolutionStatus.NoRecords)
            {
                resolved = ResolveAddresses(target.Domain, target.Transport,
                    DefaultPort(target.Transport, target.Secure), target.Secure,
                    0, 0, -1, options, clock);
            }
        }
        else
        {
            resolved = ResolveAutomatic(target.Domain, target.Secure, options, random, clock);
        }

        SipResolutionResult result = CreateResult(resolved.Status,
            resolved.ErrorMessage, request.CorrelationId);
        result.Candidates = resolved.Candidates;
        return result;
    }

    static SipDnsCandidateResult ResolveAutomatic(string domain, bool secure,
        SipDnsOptions options, ISipRandomSource random, ISipMonotonicClock clock)
    {
        SipDnsCandidateResult naptr = ResolveNaptr(domain, secure, options, random, clock);
        if (naptr.Candidates.Length > 0 || naptr.FoundServiceRecords)
        {
            return naptr;
        }

        System.Collections.Generic.List<SipServerCandidate> candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        bool foundService = false;
        SipTransportKind[] transports = secure
            ? new SipTransportKind[] { SipTransportKind.Tls }
            : new SipTransportKind[]
            {
                SipTransportKind.Udp, SipTransportKind.Tcp, SipTransportKind.Sctp
            };
        int index = 0;
        while (index < transports.Length)
        {
            SipTransportKind transport = transports[index];
            SipDnsCandidateResult service = ResolveService(
                CreateServiceName(domain, transport, secure), transport, secure, -1,
                options, random, clock);
            foundService = foundService || service.FoundServiceRecords;
            AppendCandidates(candidates, service.Candidates);
            index = index + 1;
        }
        if (candidates.Count > 0)
        {
            SipDnsCandidateResult result = new SipDnsCandidateResult();
            result.Status = SipResolutionStatus.Success;
            result.FoundServiceRecords = foundService;
            result.Candidates = candidates.ToArray();
            return result;
        }
        if (foundService)
        {
            SipDnsCandidateResult unavailable = new SipDnsCandidateResult();
            unavailable.Status = SipResolutionStatus.NoRecords;
            unavailable.FoundServiceRecords = true;
            unavailable.ErrorMessage = "The advertised SIP services have no reachable addresses.";
            return unavailable;
        }
        SipTransportKind fallback = DefaultTransport(secure);
        return ResolveAddresses(domain, fallback, DefaultPort(fallback, secure),
            secure, 0, 0, -1, options, clock);
    }

    static SipDnsCandidateResult ResolveNaptr(string domain, bool secure,
        SipDnsOptions options, ISipRandomSource random, ISipMonotonicClock clock)
    {
        SipDnsClientResult queried = SipDnsClient.QueryRecords(domain,
            SipDnsRecordKind.Naptr, options, null);
        SipDnsCandidateResult result = new SipDnsCandidateResult();
        if (queried.Status != SipResolutionStatus.Success || queried.Response == null)
        {
            result.Status = queried.Status;
            result.ErrorMessage = queried.ErrorMessage;
            return result;
        }

        SipDnsWireRecord[] records = CopyUsableNaptrRecords(
            queried.Response.Records, domain, secure);
        SortNaptrRecords(records);
        System.Collections.Generic.List<SipServerCandidate> candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        int index = 0;
        int selectedOrder = records.Length == 0 ? -1 : records[0].NaptrOrder;
        while (index < records.Length && records[index].NaptrOrder == selectedOrder)
        {
            SipDnsWireRecord record = records[index];
            SipTransportKind transport = GetNaptrTransport(record.NaptrServices);
            bool recordSecure = IsSecureNaptrService(record.NaptrServices);
            SipDnsCandidateResult service = ResolveService(
                record.NaptrReplacement == null ? "." : record.NaptrReplacement,
                transport, recordSecure, record.TtlSeconds, options, random, clock);
            AppendCandidates(candidates, service.Candidates);
            result.FoundServiceRecords = true;
            index = index + 1;
        }
        result.Candidates = candidates.ToArray();
        result.Status = candidates.Count > 0 ? SipResolutionStatus.Success
            : SipResolutionStatus.NoRecords;
        if (records.Length > 0 && candidates.Count == 0)
        {
            result.ErrorMessage = "The NAPTR services have no reachable targets.";
        }
        return result;
    }

    static SipDnsCandidateResult ResolveService(string serviceName,
        SipTransportKind transport, bool secure, long parentTtl, SipDnsOptions options,
        ISipRandomSource random, ISipMonotonicClock clock)
    {
        SipDnsClientResult queried = SipDnsClient.QueryRecords(serviceName,
            SipDnsRecordKind.Srv, options, null);
        SipDnsCandidateResult result = new SipDnsCandidateResult();
        if (queried.Status != SipResolutionStatus.Success || queried.Response == null)
        {
            result.Status = queried.Status;
            result.ErrorMessage = queried.ErrorMessage;
            return result;
        }

        SipDnsWireRecord[] records = SipSrvOrdering.Order(
            CopySrvRecords(queried.Response.Records, serviceName), random);
        result.FoundServiceRecords = records.Length > 0;
        System.Collections.Generic.List<SipServerCandidate> candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        int index = 0;
        while (index < records.Length)
        {
            SipDnsWireRecord record = records[index];
            if (record.Target != null && record.Target != "." && record.Port > 0)
            {
                long serviceTtl = MinimumTtl(parentTtl, record.TtlSeconds);
                SipDnsCandidateResult addresses = ResolveAddresses(record.Target,
                    transport, record.Port, secure, record.Priority, record.Weight,
                    serviceTtl, options, clock);
                AppendCandidates(candidates, addresses.Candidates);
            }
            index = index + 1;
        }
        result.Candidates = candidates.ToArray();
        result.Status = candidates.Count > 0 ? SipResolutionStatus.Success
            : SipResolutionStatus.NoRecords;
        if (records.Length > 0 && candidates.Count == 0)
        {
            result.ErrorMessage = "The SRV records have no reachable target addresses.";
        }
        return result;
    }

    static SipDnsCandidateResult ResolveAddresses(string domain,
        SipTransportKind transport, int port, bool secure, int priority, int weight,
        long parentTtl, SipDnsOptions options, ISipMonotonicClock clock)
    {
        SipDnsClientResult ipv4 = SipDnsClient.QueryRecords(domain,
            SipDnsRecordKind.A, options, null);
        SipDnsClientResult ipv6 = SipDnsClient.QueryRecords(domain,
            SipDnsRecordKind.Aaaa, options, null);
        System.Collections.Generic.List<SipServerCandidate> ipv4Candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        System.Collections.Generic.List<SipServerCandidate> ipv6Candidates =
            new System.Collections.Generic.List<SipServerCandidate>();
        AppendAddressCandidates(ipv4Candidates, ipv4, domain, SipDnsRecordKind.A,
            transport, port, secure, priority, weight, parentTtl, clock);
        AppendAddressCandidates(ipv6Candidates, ipv6, domain, SipDnsRecordKind.Aaaa,
            transport, port, secure, priority, weight, parentTtl, clock);
        System.Collections.Generic.List<SipServerCandidate> candidates =
            InterleaveCandidates(ipv4Candidates, ipv6Candidates);

        SipDnsCandidateResult result = new SipDnsCandidateResult();
        result.Candidates = candidates.ToArray();
        if (candidates.Count > 0)
        {
            result.Status = SipResolutionStatus.Success;
        }
        else if (ipv4.Status == SipResolutionStatus.Timeout
            || ipv6.Status == SipResolutionStatus.Timeout)
        {
            result.Status = SipResolutionStatus.Timeout;
            result.ErrorMessage = "Address resolution timed out.";
        }
        else if (ipv4.Status == SipResolutionStatus.Failed
            || ipv6.Status == SipResolutionStatus.Failed)
        {
            result.Status = SipResolutionStatus.Failed;
            result.ErrorMessage = "Address resolution failed.";
        }
        else
        {
            result.Status = SipResolutionStatus.NoRecords;
            result.ErrorMessage = "No IPv4 or IPv6 address records were found.";
        }
        return result;
    }

    static void AppendAddressCandidates(
        System.Collections.Generic.List<SipServerCandidate> candidates,
        SipDnsClientResult queried, string domain, SipDnsRecordKind kind,
        SipTransportKind transport, int port, bool secure, int priority,
        int weight, long parentTtl, ISipMonotonicClock clock)
    {
        if (queried.Status != SipResolutionStatus.Success || queried.Response == null)
        {
            return;
        }
        int index = 0;
        while (index < queried.Response.Records.Length)
        {
            SipDnsWireRecord? record = queried.Response.Records[index];
            if (record != null && record.Section == 0
                && record.Kind == kind && record.Address != null
                && AddressOwnerBelongsToChain(
                    queried.Response.Records, domain, record.Name))
            {
                SipServerCandidate candidate = new SipServerCandidate();
                candidate.Host = record.Address;
                candidate.Port = port;
                candidate.Transport = transport;
                candidate.Priority = priority;
                candidate.Weight = weight;
                candidate.RecordKind = kind;
                candidate.Secure = secure || IsSecureTransport(transport);
                candidate.ExpiresAtMilliseconds = ExpiryFromTtl(
                    MinimumTtl(parentTtl, record.TtlSeconds), clock);
                candidates.Add(candidate);
            }
            index = index + 1;
        }
    }

    static bool AddressOwnerBelongsToChain(SipDnsWireRecord[] records,
        string domain, string? owner)
    {
        if (owner == null)
        {
            return false;
        }
        string current = domain;
        int remaining = records.Length + 1;
        while (remaining > 0)
        {
            if (SipAscii.EqualsIgnoreCase(current, owner))
            {
                return true;
            }
            string? next = FindChainTarget(records, current);
            if (next == null)
            {
                return false;
            }
            current = next;
            remaining = remaining - 1;
        }
        return false;
    }

    static string? FindChainTarget(SipDnsWireRecord[] records, string owner)
    {
        int index = 0;
        while (index < records.Length)
        {
            SipDnsWireRecord? record = records[index];
            if (record != null && record.Section == 0
                && record.Kind == SipDnsRecordKind.Cname
                && record.Target != null
                && SipAscii.EqualsIgnoreCase(record.Name, owner))
            {
                return record.Target;
            }
            index = index + 1;
        }
        return null;
    }

    static SipDnsTarget CreateTarget(SipResolutionRequest? request)
    {
        SipDnsTarget target = new SipDnsTarget();
        if (request == null)
        {
            target.ErrorMessage = "A resolution request is required.";
            return target;
        }
        SipUri? uri = request.Uri;
        if (uri != null && uri.Scheme != SipUriScheme.Sip && uri.Scheme != SipUriScheme.Sips)
        {
            target.ErrorMessage = "Only SIP and SIPS URIs can be resolved as SIP targets.";
            return target;
        }
        string? domain = uri == null ? request.Domain : GetUriTarget(uri);
        if (domain == null || domain.Length == 0)
        {
            target.ErrorMessage = "A resolution domain is required.";
            return target;
        }
        domain = RemoveAddressBrackets(domain);
        target.Domain = NormalizeDomain(domain);
        target.Secure = request.Secure || uri != null && uri.Scheme == SipUriScheme.Sips;
        target.Port = uri == null ? -1 : uri.Port;
        target.ExplicitPort = target.Port >= 0;
        if (target.ExplicitPort && (target.Port <= 0 || target.Port > 65535))
        {
            target.ErrorMessage = "The explicit target port is invalid.";
            return target;
        }

        SipTransportKind uriTransport = GetUriTransport(uri);
        SipTransportKind requestedTransport = request.Transport;
        if (requestedTransport == SipTransportKind.Custom
            || uriTransport == SipTransportKind.Custom)
        {
            target.ErrorMessage = "The requested transport is not supported by RFC 3263 resolution.";
            return target;
        }
        requestedTransport = NormalizeSecureTransport(requestedTransport, target.Secure);
        uriTransport = NormalizeSecureTransport(uriTransport, target.Secure);
        if (requestedTransport != SipTransportKind.Unknown
            && uriTransport != SipTransportKind.Unknown
            && requestedTransport != uriTransport)
        {
            target.ErrorMessage = "The URI and resolution request select different transports.";
            return target;
        }
        target.Transport = requestedTransport != SipTransportKind.Unknown
            ? requestedTransport : uriTransport;
        target.ExplicitTransport = target.Transport != SipTransportKind.Unknown;
        if (target.Secure && !CanBeSecure(target.Transport))
        {
            target.ErrorMessage = "A secure target cannot use the selected transport.";
            return target;
        }
        target.Success = true;
        return target;
    }

    static SipTransportKind GetUriTransport(SipUri? uri)
    {
        if (uri == null || uri.Parameters == null)
        {
            return SipTransportKind.Unknown;
        }
        int index = 0;
        while (index < uri.Parameters.Length)
        {
            SipUriParameter? parameter = uri.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "transport"))
            {
                string? value = parameter.Value;
                if (SipAscii.EqualsIgnoreCase(value, "udp")) return SipTransportKind.Udp;
                if (SipAscii.EqualsIgnoreCase(value, "tcp")) return SipTransportKind.Tcp;
                if (SipAscii.EqualsIgnoreCase(value, "tls")) return SipTransportKind.Tls;
                if (SipAscii.EqualsIgnoreCase(value, "sctp")) return SipTransportKind.Sctp;
                if (SipAscii.EqualsIgnoreCase(value, "ws")) return SipTransportKind.WebSocket;
                if (SipAscii.EqualsIgnoreCase(value, "wss")) return SipTransportKind.SecureWebSocket;
                if (SipAscii.EqualsIgnoreCase(value, "dtls")) return SipTransportKind.Dtls;
                return SipTransportKind.Custom;
            }
            index = index + 1;
        }
        return SipTransportKind.Unknown;
    }

    static string? GetUriTarget(SipUri uri)
    {
        if (uri.Parameters != null)
        {
            int index = 0;
            while (index < uri.Parameters.Length)
            {
                SipUriParameter? parameter = uri.Parameters[index];
                if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "maddr")
                    && parameter.HasValue && parameter.Value != null)
                {
                    return parameter.Value;
                }
                index = index + 1;
            }
        }
        return uri.Host;
    }

    static SipDnsWireRecord[] CopyUsableNaptrRecords(SipDnsWireRecord[] source,
        string domain, bool secure)
    {
        System.Collections.Generic.List<SipDnsWireRecord> records =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int index = 0;
        while (index < source.Length)
        {
            SipDnsWireRecord? record = source[index];
            if (record != null && record.Section == 0 && record.Kind == SipDnsRecordKind.Naptr
                && SipAscii.EqualsIgnoreCase(record.Name, domain)
                && SipAscii.EqualsIgnoreCase(record.NaptrFlags, "s")
                && (record.NaptrRegexp == null || record.NaptrRegexp.Length == 0)
                && record.NaptrReplacement != null && record.NaptrReplacement != "."
                && GetNaptrTransport(record.NaptrServices) != SipTransportKind.Unknown
                && (!secure || IsSecureNaptrService(record.NaptrServices)))
            {
                records.Add(record);
            }
            index = index + 1;
        }
        return records.ToArray();
    }

    static SipDnsWireRecord[] CopySrvRecords(SipDnsWireRecord[] source, string serviceName)
    {
        System.Collections.Generic.List<SipDnsWireRecord> records =
            new System.Collections.Generic.List<SipDnsWireRecord>();
        int index = 0;
        while (index < source.Length)
        {
            SipDnsWireRecord? record = source[index];
            if (record != null && record.Section == 0 && record.Kind == SipDnsRecordKind.Srv
                && SipAscii.EqualsIgnoreCase(record.Name, serviceName))
            {
                records.Add(record);
            }
            index = index + 1;
        }
        return records.ToArray();
    }

    static void SortNaptrRecords(SipDnsWireRecord[] records)
    {
        int index = 1;
        while (index < records.Length)
        {
            SipDnsWireRecord current = records[index];
            int insertion = index;
            while (insertion > 0 && CompareNaptr(current, records[insertion - 1]) < 0)
            {
                records[insertion] = records[insertion - 1];
                insertion = insertion - 1;
            }
            records[insertion] = current;
            index = index + 1;
        }
    }

    static int CompareNaptr(SipDnsWireRecord left, SipDnsWireRecord right)
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

    static SipTransportKind GetNaptrTransport(string? service)
    {
        if (SipAscii.EqualsIgnoreCase(service, "SIP+D2U")) return SipTransportKind.Udp;
        if (SipAscii.EqualsIgnoreCase(service, "SIP+D2T")) return SipTransportKind.Tcp;
        if (SipAscii.EqualsIgnoreCase(service, "SIPS+D2T")) return SipTransportKind.Tls;
        if (SipAscii.EqualsIgnoreCase(service, "SIP+D2S")
            || SipAscii.EqualsIgnoreCase(service, "SIPS+D2S")) return SipTransportKind.Sctp;
        return SipTransportKind.Unknown;
    }

    static bool IsSecureNaptrService(string? service)
    {
        return service != null && service.Length >= 5
            && SipAscii.EqualsIgnoreCase(service.Substring(0, 5), "SIPS+");
    }

    static string CreateServiceName(string domain, SipTransportKind transport, bool secure)
    {
        string prefix;
        if (transport == SipTransportKind.Udp) prefix = secure ? "_sips._udp." : "_sip._udp.";
        else if (transport == SipTransportKind.Sctp) prefix = secure ? "_sips._sctp." : "_sip._sctp.";
        else if (transport == SipTransportKind.WebSocket) prefix = "_sip._ws.";
        else if (transport == SipTransportKind.SecureWebSocket) prefix = "_sips._ws.";
        else prefix = secure || transport == SipTransportKind.Tls ? "_sips._tcp." : "_sip._tcp.";
        return prefix + domain;
    }

    static SipTransportKind NormalizeSecureTransport(SipTransportKind transport, bool secure)
    {
        if (secure && transport == SipTransportKind.Tcp)
        {
            return SipTransportKind.Tls;
        }
        if (secure && transport == SipTransportKind.WebSocket)
        {
            return SipTransportKind.SecureWebSocket;
        }
        return transport;
    }

    static bool CanBeSecure(SipTransportKind transport)
    {
        return transport == SipTransportKind.Unknown || transport == SipTransportKind.Tcp
            || transport == SipTransportKind.Tls || transport == SipTransportKind.Sctp
            || transport == SipTransportKind.SecureWebSocket || transport == SipTransportKind.Dtls;
    }

    static bool IsSecureTransport(SipTransportKind transport)
    {
        return transport == SipTransportKind.Tls || transport == SipTransportKind.SecureWebSocket
            || transport == SipTransportKind.Dtls;
    }

    static SipTransportKind DefaultTransport(bool secure)
    {
        return secure ? SipTransportKind.Tls : SipTransportKind.Udp;
    }

    static int DefaultPort(SipTransportKind transport, bool secure)
    {
        return secure || IsSecureTransport(transport)
            ? SipTransportNames.SipsPort : SipTransportNames.SipPort;
    }

    static bool IsNumericAddress(string domain)
    {
        return SipUriSyntax.IsIpv4Address(domain) || SipUriSyntax.IsIpv6Address(domain);
    }

    static string RemoveAddressBrackets(string domain)
    {
        if (domain.Length > 1 && domain[0] == '[' && domain[domain.Length - 1] == ']')
        {
            return domain.Substring(1, domain.Length - 2);
        }
        return domain;
    }

    static string NormalizeDomain(string domain)
    {
        string normalized = domain;
        if (normalized.Length > 1 && normalized[normalized.Length - 1] == '.')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        return normalized.ToLowerInvariant();
    }

    static SipServerCandidate CreateNumericCandidate(string address, int port,
        SipTransportKind transport, bool secure)
    {
        SipServerCandidate candidate = new SipServerCandidate();
        candidate.Host = address;
        candidate.Port = port;
        candidate.Transport = transport;
        candidate.RecordKind = SipUriSyntax.IsIpv6Address(address)
            ? SipDnsRecordKind.Aaaa : SipDnsRecordKind.A;
        candidate.ExpiresAtMilliseconds = -1;
        candidate.Secure = secure || IsSecureTransport(transport);
        return candidate;
    }

    static long MinimumTtl(long left, long right)
    {
        if (left < 0) return right;
        if (right < 0) return left;
        return left < right ? left : right;
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

    static System.Collections.Generic.List<SipServerCandidate> InterleaveCandidates(
        System.Collections.Generic.List<SipServerCandidate> ipv4,
        System.Collections.Generic.List<SipServerCandidate> ipv6)
    {
        System.Collections.Generic.List<SipServerCandidate> result =
            new System.Collections.Generic.List<SipServerCandidate>();
        int index = 0;
        while (index < ipv4.Count || index < ipv6.Count)
        {
            if (index < ipv4.Count)
            {
                result.Add(ipv4[index]);
            }
            if (index < ipv6.Count)
            {
                result.Add(ipv6[index]);
            }
            index = index + 1;
        }
        return result;
    }

    static void AppendCandidates(System.Collections.Generic.List<SipServerCandidate> destination,
        SipServerCandidate[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static bool ValidateOptions(SipDnsOptions options)
    {
        return options.NameServers != null && options.TimeoutMilliseconds > 0
            && options.MaxAttempts > 0 && options.MaxRecordCount >= 0
            && options.MaxCnameDepth >= 0;
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
