using System.Collections.Generic;

namespace dns;

internal static class StubResolverOperations
{
    const uint FailureTimeToLive = 5;

    internal static DnsResolveResult Resolve(
        DnsResolveRequest request,
        DnsStubResolver resolver)
    {
        ValidateRequest(request, resolver);
        if (request.CancellationRequested)
        {
            return CreateStatusResult(DnsResolveStatus.Cancelled);
        }

        DnsResolveResult? inputPolicy =
            SpecialUseResolverOperations.ResolveInputName(request);
        if (inputPolicy != null)
        {
            return inputPolicy;
        }

        DnsResolverConfiguration configuration =
            ResolverConfigurationOperations.ValidateConfiguration(
                request.Options.Configuration);
        DnsCandidateSequence sequence =
            ResolverSearchOperations.CreateCandidateSequence(request);
        DnsClockValue startedAt = TransportTime.ReadClock();
        DnsClockValue deadline = TransportTime.CreateDeadlineAt(
            startedAt,
            request.Options.TotalTimeoutMilliseconds,
            false,
            new DnsClockValue());
        List<DnsResolveResult> results = new List<DnsResolveResult>();
        List<DnsResolveStatus> statuses = new List<DnsResolveStatus>();
        List<DnsQueryResult> queries = new List<DnsQueryResult>();
        List<DnsDiagnostic> diagnostics = new List<DnsDiagnostic>();
        int index = 0;
        while (index < sequence.Names.Count)
        {
            DnsResolveRequest candidateRequest = CreateCandidateRequest(
                request,
                sequence.Names[index]);
            DnsResolveResult? candidatePolicy =
                SpecialUseResolverOperations.ResolveCandidateName(
                    candidateRequest,
                    sequence.Names[index]);
            if (candidatePolicy != null)
            {
                candidatePolicy.Queries = queries;
                candidatePolicy.Diagnostics = diagnostics;
                return candidatePolicy;
            }

            DnsResolveResult result = ResolveCandidate(
                candidateRequest,
                configuration,
                deadline,
                resolver);
            AppendQueries(queries, result.Queries);
            AppendDiagnostics(diagnostics, result.Diagnostics);
            results.Add(result);
            statuses.Add(result.Status);
            if (!ResolverSearchOperations.ShouldContinueCandidateSearch(
                result.Status))
            {
                result.Queries = queries;
                result.Diagnostics = diagnostics;
                return result;
            }

            index = index + 1;
        }

        DnsResolveStatus selectedStatus =
            ResolverSearchOperations.SelectTerminalStatus(sequence, statuses);
        DnsResolveResult selected = SelectResult(
            sequence,
            results,
            selectedStatus);
        selected.Queries = queries;
        selected.Diagnostics = diagnostics;
        return selected;
    }

    static DnsResolveResult ResolveCandidate(
        DnsResolveRequest request,
        DnsResolverConfiguration configuration,
        DnsClockValue deadline,
        DnsStubResolver resolver)
    {
        return StubAliasOperations.ResolveCandidate(
            request,
            configuration,
            deadline,
            resolver);
    }

    internal static DnsResolveResult QuerySingleName(
        DnsResolveRequest request,
        DnsResolverConfiguration configuration,
        DnsClockValue deadline,
        DnsStubResolver resolver,
        int maximumAliasDepth)
    {
        int remaining = TransportTime.RemainingMilliseconds(deadline);
        if (remaining == 0)
        {
            return CreateStatusResult(DnsResolveStatus.TimedOut);
        }

        DnsQueryRequest queryRequest = CreateQueryRequest(
            request,
            configuration,
            remaining);
        DnsQueryResult queryResult = resolver.QueryClient.Query(queryRequest);
        DnsResolveResult result = CreateQueryResult(
            queryRequest,
            queryResult,
            request.Options,
            configuration,
            maximumAliasDepth);
        if (queryResult != null)
        {
            result.Queries.Add(queryResult);
            AppendDiagnostics(result.Diagnostics, queryResult.Diagnostics);
        }

        return result;
    }

    internal static DnsResolveResult? LookupCachedResult(
        DnsResolveRequest request,
        IDnsCache cache)
    {
        if (!request.Options.UseCache)
        {
            return null;
        }

        DnsCacheLookup lookup = cache.Lookup(
            request,
            TransportTime.ReadClock());
        if (lookup.Found && lookup.Entry != null)
        {
            return CreateCachedResult(lookup);
        }

        return null;
    }

    static DnsQueryRequest CreateQueryRequest(
        DnsResolveRequest request,
        DnsResolverConfiguration configuration,
        int remainingMilliseconds)
    {
        List<DnsResolverServer> servers = OrderedServers(configuration);
        DnsQueryRequest result = new DnsQueryRequest();
        result.Question = CreateQuestion(request);
        int index = 0;
        while (index < servers.Count)
        {
            result.Servers.Add(CopyEndpoint(servers[index].Endpoint));
            index = index + 1;
        }

        DnsTransportKind transport = servers[0].Transport;
        if (configuration.UseTcp && transport == DnsTransportKind.Udp)
        {
            transport = DnsTransportKind.Tcp;
        }

        result.TransportOptions.Kind = transport;
        result.TransportOptions.AttemptTimeoutMilliseconds =
            configuration.TimeoutMilliseconds;
        result.TransportOptions.TotalTimeoutMilliseconds = remainingMilliseconds;
        result.TransportOptions.MaximumAttempts =
            MultiplyAttempts(configuration.Attempts, servers.Count);
        string? authenticationName = servers[0].AuthenticationName;
        result.TransportOptions.Tls.AuthenticationName = authenticationName;
        result.TransportOptions.Quic.AuthenticationName = authenticationName;
        if (transport == DnsTransportKind.Https &&
            authenticationName != null)
        {
            result.TransportOptions.Https.Uri =
                "https://" + authenticationName + "/dns-query";
        }

        result.RecursionDesired = true;
        result.UseEdns = configuration.UseEdns;
        result.RequestDnssec = request.Options.RequestDnssec;
        result.CheckingDisabled = request.Options.CheckingDisabled;
        result.AllowTcpFallback = transport == DnsTransportKind.Udp;
        result.AllowEdnsDowngrade = true;
        result.CancellationRequested = request.CancellationRequested;
        return result;
    }

    static DnsResolveResult CreateQueryResult(
        DnsQueryRequest request,
        DnsQueryResult queryResult,
        DnsResolverOptions options,
        DnsResolverConfiguration configuration,
        int maximumAliasDepth)
    {
        if (queryResult == null)
        {
            return ProtocolFailure("The query client returned no result.");
        }

        if (!queryResult.Success || queryResult.Response == null)
        {
            return CreateStatusResult(MapTransportStatus(queryResult.Status));
        }

        DnsDiagnostic? failure = ValidateResponse(
            request.Question,
            queryResult.Response);
        if (failure != null)
        {
            DnsResolveResult invalid = CreateStatusResult(
                DnsResolveStatus.ProtocolError);
            invalid.Response = queryResult.Response;
            invalid.Diagnostics.Add(failure);
            return invalid;
        }

        DnsResolveResult result = RecordSetOperations.ExtractAnswerChain(
            queryResult.Response,
            request.Question,
            maximumAliasDepth);
        if (!options.FollowAliases && result.AliasSteps.Count > 0)
        {
            StubAliasOperations.StopAfterFirstAlias(result);
        }

        int flags = MessageOperations.EffectiveFlags(
            queryResult.Response.Header);
        if (configuration.TrustAuthenticData &&
            (flags & (int)DnsHeaderFlag.AuthenticData) != 0)
        {
            result.SecurityStatus = DnsSecurityStatus.Secure;
        }

        return result;
    }

    static DnsDiagnostic? ValidateResponse(
        DnsQuestion question,
        DnsMessage response)
    {
        if (response.Header == null || response.Questions == null ||
            response.Answers == null || response.Authorities == null ||
            response.Additionals == null)
        {
            return ResponseDiagnostic(
                "response",
                "The query response is incomplete.");
        }

        int flags = MessageOperations.EffectiveFlags(response.Header);
        if ((flags & (int)DnsHeaderFlag.Response) == 0)
        {
            return ResponseDiagnostic(
                "response.header.flags",
                "The query result is not marked as a response.");
        }

        int opcode = DnsHeaderCodec.ResolveCode(
            response.Header.OpcodeValue,
            (int)response.Header.Opcode);
        if (opcode != (int)DnsOpcode.Query)
        {
            return ResponseDiagnostic(
                "response.header.opcode",
                "The response operation code does not match a standard query.");
        }

        List<DnsQuestion> expected = new List<DnsQuestion>();
        expected.Add(question);
        if (!MessageOperations.QuestionsEqual(expected, response.Questions))
        {
            return ResponseDiagnostic(
                "response.questions",
                "The response question does not match the resolver request.");
        }

        return null;
    }

    static DnsResolveResult CreateCachedResult(DnsCacheLookup lookup)
    {
        DnsCacheEntry? entry = lookup.Entry;
        if (entry == null)
        {
            return ProtocolFailure(
                "A successful cache lookup did not contain an entry.");
        }

        DnsResolveResult result = new DnsResolveResult();
        result.Status = StatusFromCacheKind(entry.Kind);
        result.CanonicalName = RecordSetOperations.CopyName(entry.Name, false);
        result.Response = entry.Message;
        result.RecordSets = entry.RecordSets;
        result.SecurityStatus = entry.SecurityStatus;
        result.FromCache = true;
        result.Stale = lookup.Stale;
        if (lookup.Stale)
        {
            RecordSetOperations.SetResultTtl(result);
        }
        else if (lookup.HasRemainingTimeToLive)
        {
            result.HasTimeToLive = true;
            result.TimeToLive = lookup.RemainingTimeToLive;
        }

        return result;
    }

    internal static void StoreResult(
        DnsResolveRequest request,
        DnsResolveResult result,
        IDnsCache cache)
    {
        if (result.Status == DnsResolveStatus.Cancelled ||
            result.Status == DnsResolveStatus.Loop ||
            result.Status == DnsResolveStatus.DepthLimitExceeded)
        {
            return;
        }

        DnsCacheEntry entry = new DnsCacheEntry();
        entry.Name = RecordSetOperations.CopyName(request.Name, true);
        entry.RecordType = request.RecordType;
        entry.RecordTypeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        entry.Class = request.Class;
        entry.ClassValue = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        entry.Message = result.Response;
        entry.RecordSets = result.RecordSets;
        entry.SecurityStatus = result.SecurityStatus;
        entry.StoredAt = TransportTime.ReadClock();
        if (result.Status == DnsResolveStatus.Success)
        {
            entry.Kind = DnsCacheEntryKind.Positive;
            entry.Credibility = 4;
            if (result.HasTimeToLive)
            {
                entry.OriginalTimeToLive = result.TimeToLive;
            }

            entry.ExpiresAt = CreateExpiration(
                entry.StoredAt,
                entry.OriginalTimeToLive);
        }
        else if (result.Status == DnsResolveStatus.NameError ||
            result.Status == DnsResolveStatus.NoData)
        {
            entry.Kind = result.Status == DnsResolveStatus.NameError ?
                DnsCacheEntryKind.NegativeName :
                DnsCacheEntryKind.NegativeData;
            entry.Credibility = 4;
            entry.NegativeSoa = FindNegativeSoa(result.Response);
            if (entry.NegativeSoa == null)
            {
                return;
            }
        }
        else
        {
            entry.Kind = DnsCacheEntryKind.Failure;
            entry.Credibility = 0;
            entry.OriginalTimeToLive = FailureTimeToLive;
        }

        if (result.Response != null)
        {
            int flags = MessageOperations.EffectiveFlags(result.Response.Header);
            entry.Authoritative =
                (flags & (int)DnsHeaderFlag.AuthoritativeAnswer) != 0;
        }

        cache.Store(entry);
    }

    static DnsResourceRecord? FindNegativeSoa(DnsMessage? message)
    {
        if (message == null || message.Authorities == null)
        {
            return null;
        }

        int index = 0;
        while (index < message.Authorities.Count)
        {
            DnsResourceRecord record = message.Authorities[index];
            int typeValue = EffectiveValue(
                record.RecordTypeValue,
                (int)record.RecordType);
            if (typeValue == 6 && record.Data != null &&
                record.Data.Soa != null)
            {
                return record;
            }

            index = index + 1;
        }

        return null;
    }

    static List<DnsResolverServer> OrderedServers(
        DnsResolverConfiguration configuration)
    {
        List<DnsResolverServer> result = new List<DnsResolverServer>();
        int index = 0;
        while (index < configuration.Servers.Count)
        {
            DnsResolverServer server = configuration.Servers[index];
            if (server.Enabled)
            {
                int position = result.Count;
                while (position > 0 &&
                    result[position - 1].Priority > server.Priority)
                {
                    position = position - 1;
                }

                result.Insert(position, server);
            }

            index = index + 1;
        }

        return result;
    }

    internal static DnsResolveRequest CreateCandidateRequest(
        DnsResolveRequest source,
        DnsName name)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = RecordSetOperations.CopyName(name, false);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = EffectiveValue(
            source.RecordTypeValue,
            (int)source.RecordType);
        result.Class = source.Class;
        result.ClassValue = EffectiveValue(
            source.ClassValue,
            (int)source.Class);
        result.Options = source.Options;
        result.ClientSubnet = source.ClientSubnet;
        result.CancellationRequested = source.CancellationRequested;
        return result;
    }

    static DnsQuestion CreateQuestion(DnsResolveRequest request)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = RecordSetOperations.CopyName(request.Name, false);
        result.RecordType = request.RecordType;
        result.RecordTypeValue = EffectiveValue(
            request.RecordTypeValue,
            (int)request.RecordType);
        result.Class = request.Class;
        result.ClassValue = EffectiveValue(
            request.ClassValue,
            (int)request.Class);
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.Address = RecordSetOperations.CopyBytes(source.Address);
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        return result;
    }

    static DnsResolveResult SelectResult(
        DnsCandidateSequence sequence,
        List<DnsResolveResult> results,
        DnsResolveStatus status)
    {
        if (sequence.AbsoluteFirst)
        {
            return results[0];
        }

        int index = 0;
        while (index < results.Count)
        {
            if (results[index].Status == status)
            {
                return results[index];
            }

            index = index + 1;
        }

        return results[results.Count - 1];
    }

    static DnsResolveStatus MapTransportStatus(DnsTransportStatus status)
    {
        if (status == DnsTransportStatus.TimedOut)
        {
            return DnsResolveStatus.TimedOut;
        }

        if (status == DnsTransportStatus.Cancelled)
        {
            return DnsResolveStatus.Cancelled;
        }

        if (status == DnsTransportStatus.ProtocolError ||
            status == DnsTransportStatus.ResponseTooLarge)
        {
            return DnsResolveStatus.ProtocolError;
        }

        if (status == DnsTransportStatus.Unsupported)
        {
            return DnsResolveStatus.Unsupported;
        }

        return DnsResolveStatus.ServerFailure;
    }

    static DnsResolveStatus StatusFromCacheKind(DnsCacheEntryKind kind)
    {
        if (kind == DnsCacheEntryKind.Positive)
        {
            return DnsResolveStatus.Success;
        }

        if (kind == DnsCacheEntryKind.NegativeName)
        {
            return DnsResolveStatus.NameError;
        }

        if (kind == DnsCacheEntryKind.NegativeData)
        {
            return DnsResolveStatus.NoData;
        }

        return DnsResolveStatus.ServerFailure;
    }

    static DnsResolveResult ProtocolFailure(string reason)
    {
        DnsResolveResult result = CreateStatusResult(
            DnsResolveStatus.ProtocolError);
        result.Diagnostics.Add(ResponseDiagnostic("queryResult", reason));
        return result;
    }

    internal static DnsResolveResult CreateStatusResult(DnsResolveStatus status)
    {
        DnsResolveResult result = new DnsResolveResult();
        result.Status = status;
        return result;
    }

    static DnsDiagnostic ResponseDiagnostic(string field, string reason)
    {
        DnsDiagnostic result = new DnsDiagnostic();
        result.Code = DnsDiagnosticCode.ResponseMismatch;
        result.Field = field;
        result.Reason = reason;
        return result;
    }

    static DnsClockValue CreateExpiration(
        DnsClockValue storedAt,
        uint timeToLive)
    {
        long milliseconds = (long)timeToLive * 1000;
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = AddSaturated(
            storedAt.UnixTimeMilliseconds,
            milliseconds);
        result.MonotonicMilliseconds = AddSaturated(
            storedAt.MonotonicMilliseconds,
            milliseconds);
        return result;
    }

    static long AddSaturated(long value, long amount)
    {
        if (value > long.MaxValue - amount)
        {
            return long.MaxValue;
        }

        return value + amount;
    }

    static int MultiplyAttempts(int attempts, int serverCount)
    {
        if (attempts > int.MaxValue / serverCount)
        {
            return int.MaxValue;
        }

        return attempts * serverCount;
    }

    static int EffectiveValue(int rawValue, int recognizedValue)
    {
        if (rawValue == 0 && recognizedValue != 0)
        {
            return recognizedValue;
        }

        return rawValue;
    }

    internal static void AppendQueries(
        List<DnsQueryResult> destination,
        List<DnsQueryResult> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void AppendDiagnostics(
        List<DnsDiagnostic> destination,
        List<DnsDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void ValidateRequest(
        DnsResolveRequest request,
        DnsStubResolver resolver)
    {
        if (request == null || resolver == null ||
            resolver.QueryClient == null || resolver.Cache == null)
        {
            throw new DnsResolutionException(
                "A resolver request and its services are required.");
        }

        if (request.Options == null ||
            request.Options.Configuration == null)
        {
            throw new DnsResolutionException(
                "Resolver options and configuration are required.");
        }

        if (request.Options.MaximumAliasDepth < 0)
        {
            throw new DnsResolutionException(
                "The maximum alias depth cannot be negative.");
        }

        if (request.Options.TotalTimeoutMilliseconds < 1)
        {
            throw new DnsResolutionException(
                "The total resolver timeout must be positive.");
        }
    }
}
