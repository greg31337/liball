using System;
using System.Collections.Generic;

namespace dns;

internal static class QueryClientOperations
{
    internal static DnsQueryResult Query(
        DnsQueryRequest request,
        DnsQueryClient client)
    {
        ValidateRequest(request, client);
        DnsClockValue startedAt = TransportTime.ReadClock();
        DnsQueryResult result = new DnsQueryResult();
        CreateServerStates(request, result);
        if (request.CancellationRequested)
        {
            result.Status = DnsTransportStatus.Cancelled;
            return CompleteQueryResult(result, startedAt);
        }

        DnsMessage initialQuery = CreateQueryMessage(request);
        byte[] initialMac = [];
        if (request.TsigKey != null)
        {
            DnsTsigResult signing = TsigOperations.Sign(
                initialQuery,
                request.TsigKey,
                [],
                startedAt);
            result.Tsig = signing;
            if (!signing.Success || signing.Message == null)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                CopyDiagnostics(signing.Diagnostics, result.Diagnostics);
                return CompleteQueryResult(result, startedAt);
            }

            initialQuery = signing.Message;
            initialMac = signing.Mac;
        }

        DnsEncodeResult initialEncoding = EncodeQuery(initialQuery);
        if (!initialEncoding.Success)
        {
            result.Status = DnsTransportStatus.ProtocolError;
            CopyDiagnostics(initialEncoding.Diagnostics, result.Diagnostics);
            return CompleteQueryResult(result, startedAt);
        }

        DnsClockValue totalDeadline = TransportTime.CreateDeadlineAt(
            startedAt,
            request.TransportOptions.TotalTimeoutMilliseconds,
            false,
            new DnsClockValue());
        DnsTransportKind kind = request.TransportOptions.Kind;
        if (kind != DnsTransportKind.Udp && kind != DnsTransportKind.Tcp &&
            kind != DnsTransportKind.Tls && kind != DnsTransportKind.Https &&
            kind != DnsTransportKind.Quic)
        {
            result.Status = DnsTransportStatus.Unsupported;
            return CompleteQueryResult(result, startedAt);
        }

        int startIndex = SelectStartingServer(client, request.Servers.Count);
        int serverStep = 0;
        DnsTransportResult? terminal = null;
        while (result.Attempts.Count <
            request.TransportOptions.MaximumAttempts)
        {
            if (request.CancellationRequested)
            {
                result.Status = DnsTransportStatus.Cancelled;
                return CompleteQueryResult(result, startedAt);
            }

            if (TransportTime.RemainingMilliseconds(totalDeadline) == 0)
            {
                result.Status = DnsTransportStatus.TimedOut;
                return CompleteQueryResult(result, startedAt);
            }

            int serverIndex = (startIndex + serverStep) % request.Servers.Count;
            DnsEndpoint endpoint = request.Servers[serverIndex];
            DnsServerState serverState = result.ServerStates[serverIndex];
            DnsMessage activeQuery = initialQuery;
            byte[] activeBytes = initialEncoding.Bytes;
            byte[] activeMac = initialMac;
            bool retrySameServer = true;
            while (retrySameServer && result.Attempts.Count <
                request.TransportOptions.MaximumAttempts)
            {
                retrySameServer = false;
                DnsTransportResult attempt = ExecuteTransport(
                    client,
                    request,
                    endpoint,
                    activeQuery,
                    activeBytes,
                    totalDeadline,
                    kind,
                    false);
                if (request.TsigKey != null &&
                    attempt.Status == DnsTransportStatus.Success &&
                    attempt.ResponseMessage != null)
                {
                    result.Tsig = AuthenticateResponse(
                        attempt,
                        request.TsigKey,
                        activeMac);
                }

                AddAttempt(result, serverState, attempt);
                terminal = attempt;

                if (ShouldFallback(request, attempt) &&
                    result.Attempts.Count <
                        request.TransportOptions.MaximumAttempts &&
                    TransportTime.RemainingMilliseconds(totalDeadline) > 0)
                {
                    DnsTransportResult fallback = ExecuteTransport(
                        client,
                        request,
                        endpoint,
                        activeQuery,
                        activeBytes,
                        totalDeadline,
                        DnsTransportKind.Tcp,
                        true);
                    if (request.TsigKey != null &&
                        fallback.Status == DnsTransportStatus.Success &&
                        fallback.ResponseMessage != null)
                    {
                        result.Tsig = AuthenticateResponse(
                            fallback,
                            request.TsigKey,
                            activeMac);
                    }

                    AddAttempt(result, serverState, fallback);
                    result.UsedFallback = true;
                    terminal = fallback;
                }

                DnsMessage? metadataResponse = terminal.ResponseMessage;
                if (request.TsigKey != null &&
                    (result.Tsig == null || !result.Tsig.Authenticated ||
                     result.Tsig.ErrorValue != 0))
                {
                    metadataResponse = null;
                }

                EdnsResponseMetadata metadata = EdnsOperations.InspectResponse(
                    activeQuery,
                    metadataResponse,
                    request.AllowEdnsDowngrade,
                    EdnsIsRequired(request, activeQuery));
                EdnsOperations.ApplyResponseMetadata(
                    metadata, result, serverState, false);
                if (metadata.ShouldRetryWithoutEdns &&
                    result.Attempts.Count <
                        request.TransportOptions.MaximumAttempts &&
                    TransportTime.RemainingMilliseconds(totalDeadline) > 0)
                {
                    if (request.TsigKey != null)
                    {
                        activeQuery = RemoveTsig(activeQuery);
                    }

                    activeQuery = EdnsOperations.PrepareWithoutEdns(activeQuery);
                    if (request.TsigKey != null)
                    {
                        DnsTsigResult signing = TsigOperations.Sign(
                            activeQuery,
                            request.TsigKey,
                            [],
                            TransportTime.ReadClock());
                        result.Tsig = signing;
                        if (!signing.Success || signing.Message == null)
                        {
                            result.Status = DnsTransportStatus.ProtocolError;
                            CopyDiagnostics(
                                signing.Diagnostics,
                                result.Diagnostics);
                            return CompleteQueryResult(result, startedAt);
                        }

                        activeQuery = signing.Message;
                        activeMac = signing.Mac;
                    }

                    DnsEncodeResult downgradedEncoding = EncodeQuery(activeQuery);
                    if (!downgradedEncoding.Success)
                    {
                        result.Status = DnsTransportStatus.ProtocolError;
                        CopyDiagnostics(
                            downgradedEncoding.Diagnostics,
                            result.Diagnostics);
                        return CompleteQueryResult(result, startedAt);
                    }

                    activeBytes = downgradedEncoding.Bytes;
                    EdnsOperations.ApplyResponseMetadata(
                        metadata, result, serverState, true);
                    retrySameServer = true;
                }
            }

            if (terminal != null && !retrySameServer &&
                !IsRetryableFailure(terminal, activeQuery))
            {
                ApplyTerminalAttempt(result, terminal);
                return CompleteQueryResult(result, startedAt);
            }

            serverStep = serverStep + 1;
        }

        if (terminal != null)
        {
            ApplyTerminalAttempt(result, terminal);
        }
        else
        {
            result.Status = DnsTransportStatus.TimedOut;
        }

        return CompleteQueryResult(result, startedAt);
    }

    static void ValidateRequest(
        DnsQueryRequest request,
        DnsQueryClient client)
    {
        if (request == null || client == null)
        {
            throw new ArgumentNullException("request");
        }

        if (request.Question == null || request.Servers == null ||
            request.TransportOptions == null)
        {
            throw new ArgumentException(
                "A complete DNS query request is required.");
        }

        if (request.Servers.Count == 0)
        {
            throw new ArgumentException(
                "At least one DNS server endpoint is required.");
        }

        int serverIndex = 0;
        while (serverIndex < request.Servers.Count)
        {
            if (request.Servers[serverIndex] == null)
            {
                throw new ArgumentException(
                    "Every DNS server endpoint is required.");
            }

            serverIndex = serverIndex + 1;
        }

        if (request.TransportOptions.AttemptTimeoutMilliseconds < 0 ||
            request.TransportOptions.TotalTimeoutMilliseconds < 0 ||
            request.TransportOptions.MaximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException("request.TransportOptions");
        }
    }

    static void CreateServerStates(
        DnsQueryRequest request,
        DnsQueryResult result)
    {
        int index = 0;
        while (index < request.Servers.Count)
        {
            DnsServerState state = new DnsServerState();
            state.Endpoint = CopyEndpoint(request.Servers[index]);
            state.PreferredTransport = request.TransportOptions.Kind;
            result.ServerStates.Add(state);
            index = index + 1;
        }
    }

    static int SelectStartingServer(DnsQueryClient client, int serverCount)
    {
        int start = client.NextServerOffset;
        if (start < 0)
        {
            start = TransportEntropy.NextInt32(serverCount);
        }

        start = start % serverCount;
        client.NextServerOffset = (start + 1) % serverCount;
        return start;
    }

    static DnsMessage CreateQueryMessage(DnsQueryRequest request)
    {
        if (request.Message != null)
        {
            return MessageOperations.CloneMessage(request.Message);
        }

        DnsMessage message = new DnsMessage();
        message.Header.Identifier = TransportEntropy.NextIdentifier();
        DnsHeaderFlag flags = DnsHeaderFlag.None;
        if (request.RecursionDesired)
        {
            flags = flags | DnsHeaderFlag.RecursionDesired;
        }

        if (request.CheckingDisabled)
        {
            flags = flags | DnsHeaderFlag.CheckingDisabled;
        }

        message.Header.Flags = flags;
        message.Header.FlagsValue = (int)flags;
        message.Questions.Add(CopyQuestion(request.Question));
        if (request.UseEdns)
        {
            return EdnsOperations.PrepareRequest(
                message,
                DnsProtocolLimits.DefaultEdnsUdpPayloadSize,
                request.RequestDnssec);
        }

        return message;
    }

    static DnsQuestion CopyQuestion(DnsQuestion source)
    {
        DnsQuestion result = new DnsQuestion();
        result.Name = CopyName(source.Name);
        result.RecordType = source.RecordType;
        result.RecordTypeValue = source.RecordTypeValue;
        result.Class = source.Class;
        result.ClassValue = source.ClassValue;
        return result;
    }

    static DnsName CopyName(DnsName source)
    {
        if (source == null)
        {
            throw new ArgumentException("A DNS query name is required.");
        }

        DnsName result = new DnsName();
        result.IsAbsolute = source.IsAbsolute;
        int index = 0;
        while (index < source.Labels.Count)
        {
            byte[] label = source.Labels[index];
            byte[] copiedLabel = new byte[label.Length];
            Array.Copy(label, copiedLabel, label.Length);
            result.Labels.Add(copiedLabel);
            index = index + 1;
        }

        return result;
    }

    static DnsEncodeResult EncodeQuery(DnsMessage query)
    {
        DnsEncodeOptions encodeOptions = new DnsEncodeOptions();
        encodeOptions.MaximumMessageLength =
            DnsProtocolLimits.MaximumTcpMessageLength;
        return DnsMessageEncoder.EncodeMessage(query, encodeOptions);
    }

    static DnsTsigResult AuthenticateResponse(
        DnsTransportResult attempt,
        DnsTsigKey key,
        byte[] requestMac)
    {
        DnsMessage? response = attempt.ResponseMessage;
        if (response == null)
        {
            DnsTsigResult missing = new DnsTsigResult();
            DnsDiagnostic diagnostic = new DnsDiagnostic();
            diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
            diagnostic.Field = "tsig";
            diagnostic.Reason =
                "A DNS response is required for TSIG authentication.";
            missing.Diagnostics.Add(diagnostic);
            attempt.Status = DnsTransportStatus.ProtocolError;
            attempt.Diagnostics.Add(diagnostic);
            return missing;
        }

        DnsClockValue now = attempt.CompletedAt;
        if (now.UnixTimeMilliseconds <= 0)
        {
            now = TransportTime.ReadClock();
        }

        DnsTsigResult result = TsigOperations.Verify(
            response,
            key,
            requestMac,
            now);
        if (!result.Success || !result.Authenticated ||
            result.ErrorValue != 0)
        {
            attempt.Status = DnsTransportStatus.ProtocolError;
            CopyDiagnostics(result.Diagnostics, attempt.Diagnostics);
            if (result.Diagnostics.Count == 0)
            {
                DnsDiagnostic diagnostic = new DnsDiagnostic();
                diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
                diagnostic.Field = "tsig";
                diagnostic.Reason =
                    "The DNS response did not pass TSIG authentication.";
                attempt.Diagnostics.Add(diagnostic);
            }
        }

        return result;
    }

    static DnsMessage RemoveTsig(DnsMessage message)
    {
        DnsMessage result = MessageOperations.CloneMessage(message);
        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(result);
        if (placement.Success && placement.HasTsig &&
            result.Additionals.Count > 0)
        {
            result.Additionals.RemoveAt(result.Additionals.Count - 1);
            result.Header.AdditionalCount = result.Additionals.Count;
        }

        return result;
    }

    static DnsTransportResult ExecuteTransport(
        DnsQueryClient client,
        DnsQueryRequest request,
        DnsEndpoint endpoint,
        DnsMessage query,
        byte[] queryBytes,
        DnsClockValue totalDeadline,
        DnsTransportKind kind,
        bool fallback)
    {
        DnsTransportRequest transportRequest = CreateTransportRequest(
            request,
            endpoint,
            query,
            queryBytes,
            totalDeadline,
            kind);
        DnsTransportResult result;
        if (kind == DnsTransportKind.Tcp)
        {
            result = ExecuteTcp(client, transportRequest);
        }
        else if (kind == DnsTransportKind.Tls)
        {
            result = ExecuteTls(client, transportRequest);
        }
        else if (kind == DnsTransportKind.Https)
        {
            result = ExecuteHttps(client, transportRequest);
        }
        else if (kind == DnsTransportKind.Quic)
        {
            result = ExecuteQuic(client, transportRequest);
        }
        else
        {
            result = ExecuteUdp(client, transportRequest);
        }

        if (fallback)
        {
            result.UsedFallback = true;
            result.FallbackFrom = DnsTransportKind.Udp;
        }

        return result;
    }

    static DnsTransportResult ExecuteUdp(
        DnsQueryClient client,
        DnsTransportRequest request)
    {
        if (client.UdpTransport != null)
        {
            return client.UdpTransport.Exchange(request);
        }

        DnsUdpTransport transport = new DnsUdpTransport();
        DnsTransportResult result = transport.Exchange(request);
        transport.CloseTransport();
        return result;
    }

    static DnsTransportResult ExecuteTcp(
        DnsQueryClient client,
        DnsTransportRequest request)
    {
        if (client.TcpTransport != null)
        {
            return client.TcpTransport.Exchange(request);
        }

        DnsTcpTransport transport = new DnsTcpTransport();
        DnsTransportResult result = transport.Exchange(request);
        transport.CloseTransport();
        return result;
    }

    static DnsTransportResult ExecuteTls(
        DnsQueryClient client,
        DnsTransportRequest request)
    {
        if (client.TlsTransport != null)
        {
            return client.TlsTransport.Exchange(request);
        }

        DnsTlsTransport transport = new DnsTlsTransport();
        DnsTransportResult result = transport.Exchange(request);
        transport.CloseTransport();
        return result;
    }

    static DnsTransportResult ExecuteHttps(
        DnsQueryClient client,
        DnsTransportRequest request)
    {
        if (client.HttpsTransport != null)
        {
            return client.HttpsTransport.Exchange(request);
        }

        DnsHttpsTransport transport = new DnsHttpsTransport();
        DnsTransportResult result = transport.Exchange(request);
        transport.CloseTransport();
        return result;
    }

    static DnsTransportResult ExecuteQuic(
        DnsQueryClient client,
        DnsTransportRequest request)
    {
        if (client.QuicTransport != null)
        {
            return client.QuicTransport.Exchange(request);
        }

        DnsQuicTransport transport = new DnsQuicTransport();
        DnsTransportResult result = transport.Exchange(request);
        transport.CloseTransport();
        return result;
    }

    static DnsTransportRequest CreateTransportRequest(
        DnsQueryRequest request,
        DnsEndpoint endpoint,
        DnsMessage query,
        byte[] queryBytes,
        DnsClockValue totalDeadline,
        DnsTransportKind kind)
    {
        DnsTransportRequest result = new DnsTransportRequest();
        result.Endpoint = endpoint;
        result.Options = CopyTransportOptions(request.TransportOptions);
        result.Options.Kind = kind;
        result.QueryBytes = queryBytes;
        result.QueryMessage = query;
        result.HasDeadline = true;
        result.Deadline = totalDeadline;
        result.CancellationRequested = request.CancellationRequested;
        return result;
    }

    static DnsTransportOptions CopyTransportOptions(
        DnsTransportOptions source)
    {
        DnsTransportOptions result = new DnsTransportOptions();
        result.Kind = source.Kind;
        result.AttemptTimeoutMilliseconds = source.AttemptTimeoutMilliseconds;
        result.TotalTimeoutMilliseconds = source.TotalTimeoutMilliseconds;
        result.MaximumAttempts = source.MaximumAttempts;
        result.MaximumResponseLength = source.MaximumResponseLength;
        result.ConnectUdpSocket = source.ConnectUdpSocket;
        result.ReuseConnections = source.ReuseConnections;
        result.IdleConnectionTimeoutMilliseconds =
            source.IdleConnectionTimeoutMilliseconds;
        result.ConnectionLifetimeMilliseconds =
            source.ConnectionLifetimeMilliseconds;
        result.Tls = CopyTlsOptions(source.Tls);
        result.Https = CopyHttpsOptions(source.Https);
        result.Quic = CopyQuicOptions(source.Quic);
        return result;
    }

    static DnsTlsOptions CopyTlsOptions(DnsTlsOptions source)
    {
        DnsTlsOptions result = new DnsTlsOptions();
        result.AuthenticationName = source.AuthenticationName;
        result.ValidateCertificate = source.ValidateCertificate;
        result.CheckCertificateRevocation = source.CheckCertificateRevocation;
        result.ReuseConnection = source.ReuseConnection;
        return result;
    }

    static DnsHttpsOptions CopyHttpsOptions(DnsHttpsOptions source)
    {
        DnsHttpsOptions result = new DnsHttpsOptions();
        result.Uri = source.Uri;
        result.UseGet = source.UseGet;
        result.MediaType = source.MediaType;
        result.BootstrapEndpoint = source.BootstrapEndpoint;
        result.AllowRedirects = source.AllowRedirects;
        result.MaximumRedirects = source.MaximumRedirects;
        return result;
    }

    static DnsQuicOptions CopyQuicOptions(DnsQuicOptions source)
    {
        DnsQuicOptions result = new DnsQuicOptions();
        result.AuthenticationName = source.AuthenticationName;
        result.ValidateCertificate = source.ValidateCertificate;
        result.AllowZeroRoundTripData = source.AllowZeroRoundTripData;
        result.IdleTimeoutMilliseconds = source.IdleTimeoutMilliseconds;
        return result;
    }

    static bool ShouldFallback(
        DnsQueryRequest request,
        DnsTransportResult udp)
    {
        if (!request.AllowTcpFallback ||
            udp.Transport != DnsTransportKind.Udp)
        {
            return false;
        }

        if (udp.Status == DnsTransportStatus.ResponseTooLarge)
        {
            return true;
        }

        return udp.Status == DnsTransportStatus.Success && udp.Truncated;
    }

    static bool EdnsIsRequired(
        DnsQueryRequest request,
        DnsMessage query)
    {
        if (request.RequestDnssec)
        {
            return true;
        }

        DnsResourceRecord? opt = MessageOperations.FindOptRecord(query);
        if (opt == null || opt.Data == null || opt.Data.Edns == null)
        {
            return false;
        }

        int flags = EdnsRecordDataCodec.ReadEffectiveFlags(opt.Data.Edns);
        return (flags & (int)DnsEdnsFlag.DnssecAnswerOk) != 0 ||
            opt.Data.Edns.Options.Count > 0;
    }

    static bool IsRetryableFailure(
        DnsTransportResult attempt,
        DnsMessage query)
    {
        if (attempt.Status == DnsTransportStatus.TimedOut ||
            attempt.Status == DnsTransportStatus.NetworkError ||
            attempt.Status == DnsTransportStatus.ProtocolError ||
            attempt.Status == DnsTransportStatus.ResponseTooLarge ||
            attempt.Status == DnsTransportStatus.Closed)
        {
            return true;
        }

        if (attempt.Status != DnsTransportStatus.Success ||
            attempt.ResponseMessage == null)
        {
            return false;
        }

        int responseCode = MessageOperations.ReadEffectiveResponseCode(
            attempt.ResponseMessage);
        if (responseCode == (int)DnsResponseCode.ServerFailure ||
            responseCode == (int)DnsResponseCode.NotImplemented ||
            responseCode == (int)DnsResponseCode.Refused)
        {
            return true;
        }

        return responseCode == (int)DnsResponseCode.FormatError &&
            MessageOperations.FindOptRecord(query) != null;
    }

    static void AddAttempt(
        DnsQueryResult result,
        DnsServerState state,
        DnsTransportResult attempt)
    {
        result.Attempts.Add(attempt);
        CopyDiagnostics(attempt.Diagnostics, result.Diagnostics);
        state.AttemptCount = state.AttemptCount + 1;
        if (attempt.CompletedAt.MonotonicMilliseconds == 0)
        {
            state.LastAttempt = TransportTime.ReadClock();
        }
        else
        {
            state.LastAttempt = attempt.CompletedAt;
        }

        if (attempt.Status == DnsTransportStatus.Success)
        {
            state.HasSuccessfulExchange = true;
            state.ConsecutiveFailures = 0;
            state.LastSuccess = state.LastAttempt;
            state.PreferredTransport = attempt.Transport;
        }
        else
        {
            state.ConsecutiveFailures = state.ConsecutiveFailures + 1;
        }
    }

    static void ApplyTerminalAttempt(
        DnsQueryResult result,
        DnsTransportResult attempt)
    {
        result.Status = attempt.Status;
        result.Success = attempt.Status == DnsTransportStatus.Success;
        result.Response = attempt.ResponseMessage;
        result.ResponseBytes = attempt.ResponseBytes;
    }

    static DnsQueryResult CompleteQueryResult(
        DnsQueryResult result,
        DnsClockValue startedAt)
    {
        DnsClockValue completedAt = TransportTime.ReadClock();
        result.ElapsedMilliseconds = TransportTime.ElapsedMilliseconds(
            startedAt, completedAt);
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        result.Address = new byte[source.Address.Length];
        Array.Copy(source.Address, result.Address, source.Address.Length);
        return result;
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
}
