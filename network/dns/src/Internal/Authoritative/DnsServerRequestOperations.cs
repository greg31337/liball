using System.Collections.Generic;

namespace dns;

internal class DnsServerRequestServices
{
    internal IDnsAuthoritativeService AuthoritativeService;
    internal IDnsUpdateService UpdateService;
    internal IDnsNotifyService NotifyService;
    internal IDnsTransferService TransferService;

    internal DnsServerRequestServices(IDnsZoneStore zoneStore)
    {
        this.AuthoritativeService = new DnsAuthoritativeService(zoneStore);
        this.UpdateService = new DnsUpdateService(zoneStore);
        this.NotifyService = new DnsNotifyService(
            zoneStore,
            new DnsQueryClient(),
            new DnsTransferClient());
        this.TransferService = new DnsTransferService(zoneStore);
    }
}

internal class DnsServerRequestContext
{
    internal DnsMessage Message = new DnsMessage();
    internal byte[] MessageBytes = [];
    internal DnsEndpoint ClientEndpoint = new DnsEndpoint();
    internal DnsEndpoint ServerEndpoint = new DnsEndpoint();
    internal DnsTransportKind Transport;
    internal bool CancellationRequested;
}

internal class DnsServerDispatchResult
{
    internal bool Failed;
    internal List<byte[]> Responses = new List<byte[]>();
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class DnsServerRequestOperations
{
    internal static DnsServerDispatchResult Dispatch(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        int opcode = DnsHeaderCodec.ResolveCode(
            context.Message.Header.OpcodeValue,
            (int)context.Message.Header.Opcode);
        if (opcode == (int)DnsOpcode.Update)
        {
            return DispatchUpdate(context, options, services);
        }

        if (opcode == (int)DnsOpcode.Notify)
        {
            return DispatchNotify(context, options, services);
        }

        if (opcode == (int)DnsOpcode.Query &&
            context.Message.Questions.Count == 1)
        {
            int typeValue = DnsHeaderCodec.ResolveCode(
                context.Message.Questions[0].RecordTypeValue,
                (int)context.Message.Questions[0].RecordType);
            if (typeValue == (int)DnsRecordType.Axfr)
            {
                return DispatchTransfer(
                    context,
                    options,
                    services,
                    DnsTransferKind.Axfr);
            }

            if (typeValue == (int)DnsRecordType.Ixfr)
            {
                return DispatchTransfer(
                    context,
                    options,
                    services,
                    DnsTransferKind.Ixfr);
            }
        }

        return DispatchQuery(context, options, services);
    }

    static DnsServerDispatchResult DispatchQuery(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        DnsTsigKey? key = FindTsigKey(context.Message, options.TsigKeys);
        DnsTsigResult? authentication = AuthenticateQuery(
            context.Message,
            key);
        if (authentication != null &&
            (!authentication.Success || !authentication.Authenticated))
        {
            DnsServerDispatchResult rejected = ProtocolResponse(
                context,
                DnsResponseCode.NotAuthoritative,
                false);
            CopyDiagnostics(
                authentication.Diagnostics,
                rejected.Diagnostics);
            return rejected;
        }

        DnsAuthoritativeRequest request = new DnsAuthoritativeRequest();
        request.Message = context.Message;
        request.MessageBytes = context.MessageBytes;
        request.ClientEndpoint = context.ClientEndpoint;
        request.ServerEndpoint = context.ServerEndpoint;
        request.Transport = context.Transport;
        request.Options = options.Authoritative;
        request.Tsig = authentication;
        request.ReceivedAt = TransportTime.ReadClock();
        request.CancellationRequested = context.CancellationRequested;
        DnsAuthoritativeResult answer =
            services.AuthoritativeService.AnswerAuthoritatively(request);
        DnsServerDispatchResult result = new DnsServerDispatchResult();
        CopyDiagnostics(answer.Diagnostics, result.Diagnostics);
        if (!answer.Success || answer.Response == null)
        {
            return FailureResponse(
                context,
                result.Diagnostics);
        }

        DnsMessage response = answer.Response;
        if (authentication != null && key != null)
        {
            DnsTsigResult signed = TsigOperations.Sign(
                response,
                key,
                authentication.Mac,
                TransportTime.ReadClock());
            CopyDiagnostics(signed.Diagnostics, result.Diagnostics);
            if (!signed.Success || signed.Message == null)
            {
                return FailureResponse(
                    context,
                    result.Diagnostics);
            }

            response = signed.Message;
        }

        if (!EncodeResponse(context, options, response, result))
        {
            return FailureResponse(
                context,
                result.Diagnostics);
        }

        return result;
    }

    static DnsServerDispatchResult DispatchUpdate(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        DnsUpdateRequest request = new DnsUpdateRequest();
        request.Message = context.Message;
        request.ClientEndpoint = context.ClientEndpoint;
        request.Authorization = options.Authoritative.Authorization;
        request.TsigKey = FindTsigKey(context.Message, options.TsigKeys);
        request.CancellationRequested = context.CancellationRequested;
        DnsUpdateResult update = services.UpdateService.ApplyUpdate(request);
        DnsServerDispatchResult result = new DnsServerDispatchResult();
        CopyDiagnostics(update.Diagnostics, result.Diagnostics);
        if (!update.Success || update.Response == null ||
            !EncodeResponse(context, options, update.Response, result))
        {
            return FailureResponse(
                context,
                result.Diagnostics);
        }

        return result;
    }

    static DnsServerDispatchResult DispatchNotify(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        DnsNotifyRequest request = new DnsNotifyRequest();
        request.Message = context.Message;
        request.Endpoint = context.ClientEndpoint;
        request.Authorization = options.Authoritative.Authorization;
        request.TsigKey = FindTsigKey(context.Message, options.TsigKeys);
        request.TransportOptions = CreateTransportOptions(
            context,
            options);
        request.CancellationRequested = context.CancellationRequested;
        DnsNotifyResult notify = services.NotifyService.ProcessNotify(
            request);
        DnsServerDispatchResult result = new DnsServerDispatchResult();
        CopyDiagnostics(notify.Diagnostics, result.Diagnostics);
        if (!notify.Success || notify.Response == null ||
            !EncodeResponse(context, options, notify.Response, result))
        {
            return FailureResponse(
                context,
                result.Diagnostics);
        }

        return result;
    }

    static DnsServerDispatchResult DispatchTransfer(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsServerRequestServices services,
        DnsTransferKind kind)
    {
        if (context.Transport != DnsTransportKind.Tcp)
        {
            return ProtocolResponse(
                context,
                DnsResponseCode.Refused,
                false);
        }

        DnsTransferRequest request = new DnsTransferRequest();
        request.Kind = kind;
        request.Endpoint = context.ClientEndpoint;
        request.Transport = context.Transport;
        request.TransportOptions = CreateTransportOptions(
            context,
            options);
        request.Message = context.Message;
        request.TsigKey = FindTsigKey(context.Message, options.TsigKeys);
        request.Authorization = options.Authoritative.Authorization;
        request.CancellationRequested = context.CancellationRequested;
        DnsTransferResult transfer =
            services.TransferService.ServeTransfer(request);
        DnsServerDispatchResult result = new DnsServerDispatchResult();
        CopyDiagnostics(transfer.Diagnostics, result.Diagnostics);
        int index = 0;
        while (index < transfer.Messages.Count)
        {
            if (!EncodeResponse(
                context,
                options,
                transfer.Messages[index],
                result))
            {
                return FailureResponse(
                    context,
                    result.Diagnostics);
            }

            index = index + 1;
        }

        if (result.Responses.Count == 0)
        {
            return FailureResponse(
                context,
                result.Diagnostics);
        }

        return result;
    }

    static DnsTransportOptions CreateTransportOptions(
        DnsServerRequestContext context,
        DnsServerOptions options)
    {
        DnsTransportOptions result = new DnsTransportOptions();
        result.Kind = context.Transport;
        result.AttemptTimeoutMilliseconds =
            options.RequestTimeoutMilliseconds;
        result.TotalTimeoutMilliseconds =
            options.RequestTimeoutMilliseconds;
        if (context.Transport == DnsTransportKind.Udp)
        {
            result.MaximumResponseLength =
                options.Authoritative.MaximumUdpResponseLength;
        }
        else
        {
            result.MaximumResponseLength =
                options.Authoritative.MaximumTcpResponseLength;
        }

        result.IdleConnectionTimeoutMilliseconds =
            options.TcpIdleTimeoutMilliseconds;
        result.ConnectionLifetimeMilliseconds =
            options.TcpConnectionLifetimeMilliseconds;
        return result;
    }

    static DnsTsigKey? FindTsigKey(
        DnsMessage message,
        List<DnsTsigKey> keys)
    {
        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success || !placement.HasTsig ||
            placement.Record == null)
        {
            return null;
        }

        int index = 0;
        while (index < keys.Count)
        {
            DnsTsigKey key = keys[index];
            if (key.Enabled && RecordSetOperations.NamesEqual(
                key.Name,
                placement.Record.Name))
            {
                return key;
            }

            index = index + 1;
        }

        return null;
    }

    static DnsTsigResult? AuthenticateQuery(
        DnsMessage message,
        DnsTsigKey? key)
    {
        TsigPlacementResult placement =
            TransactionRecordDataCodec.InspectTsigPlacement(message);
        if (!placement.Success || !placement.HasTsig)
        {
            return null;
        }

        if (key == null)
        {
            DnsTsigResult missing = new DnsTsigResult();
            missing.Success = true;
            missing.Error = DnsTsigError.BadKey;
            missing.ErrorValue = (int)DnsTsigError.BadKey;
            AddDiagnostic(
                missing.Diagnostics,
                DnsDiagnosticCode.ResponseMismatch,
                "request.tsigKey",
                "No verification key is available for the signed query.");
            return missing;
        }

        return TsigOperations.Verify(
            message,
            key,
            [],
            TransportTime.ReadClock());
    }

    static bool EncodeResponse(
        DnsServerRequestContext context,
        DnsServerOptions options,
        DnsMessage response,
        DnsServerDispatchResult result)
    {
        DnsEncodeOptions encodeOptions = new DnsEncodeOptions();
        if (context.Transport == DnsTransportKind.Udp)
        {
            encodeOptions.MaximumMessageLength =
                options.Authoritative.MaximumUdpResponseLength;
        }
        else
        {
            encodeOptions.MaximumMessageLength =
                options.Authoritative.MaximumTcpResponseLength;
        }

        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            response,
            encodeOptions);
        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
        if (!encoded.Success)
        {
            return false;
        }

        result.Responses.Add(encoded.Bytes);
        return true;
    }

    static DnsServerDispatchResult ProtocolResponse(
        DnsServerRequestContext context,
        DnsResponseCode responseCode,
        bool failed)
    {
        DnsServerDispatchResult result = new DnsServerDispatchResult();
        result.Failed = failed;
        DnsMessage response = CreateProtocolResponse(
            context.Message,
            responseCode);
        DnsEncodeOptions options = new DnsEncodeOptions();
        options.MaximumMessageLength = DnsProtocolLimits.MaximumTcpMessageLength;
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            response,
            options);
        CopyDiagnostics(encoded.Diagnostics, result.Diagnostics);
        if (encoded.Success)
        {
            result.Responses.Add(encoded.Bytes);
        }

        return result;
    }

    static DnsServerDispatchResult FailureResponse(
        DnsServerRequestContext context,
        List<DnsDiagnostic> diagnostics)
    {
        DnsServerDispatchResult result = ProtocolResponse(
            context,
            DnsResponseCode.ServerFailure,
            true);
        CopyDiagnostics(diagnostics, result.Diagnostics);
        return result;
    }

    static DnsMessage CreateProtocolResponse(
        DnsMessage request,
        DnsResponseCode responseCode)
    {
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = request.Header.Identifier;
        result.Header.Opcode = request.Header.Opcode;
        result.Header.OpcodeValue = request.Header.OpcodeValue;
        result.Header.ResponseCode = responseCode;
        result.Header.ResponseCodeValue = (int)responseCode;
        result.Header.Flags = DnsHeaderFlag.Response;
        result.Header.FlagsValue = (int)DnsHeaderFlag.Response;
        int requestFlags = MessageOperations.EffectiveFlags(request.Header);
        if ((requestFlags & (int)DnsHeaderFlag.RecursionDesired) != 0)
        {
            result.Header.Flags = result.Header.Flags |
                DnsHeaderFlag.RecursionDesired;
            result.Header.FlagsValue = (int)result.Header.Flags;
        }

        if (request.Questions.Count == 1)
        {
            DnsMessage copied = MessageOperations.CloneMessage(request);
            result.Questions.Add(copied.Questions[0]);
            result.Header.QuestionCount = 1;
        }

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

    static void AddDiagnostic(
        List<DnsDiagnostic> diagnostics,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        diagnostics.Add(diagnostic);
    }
}
