using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EmailCopyPreparation
{
    internal EmailCopyRequest Backend = new EmailCopyRequest();
    internal EmailCopyResponse Local = new EmailCopyResponse();
    internal Dictionary<string, string> DuplicateOf = new Dictionary<string, string>();
}

internal class EmailMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal EmailMethodJsonCodec Codec;
    internal EmailQueryJsonCodec QueryCodec;
    internal ProtocolJsonCodec Protocol;
    internal HeaderPropertySelector Headers = new HeaderPropertySelector();
    internal BodyPartMapper BodyParts = new BodyPartMapper();
    internal EmailFilterEvaluator Filters = new EmailFilterEvaluator();
    internal EmailQuerySorter Sorter = new EmailQuerySorter();
    internal EmailSetValidator SetValues = new EmailSetValidator();
    internal MimeMessageWriter MimeWriter = new MimeMessageWriter();
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal EmailMethod()
    {
        Codec = new EmailMethodJsonCodec(Reader);
        QueryCodec = new EmailQueryJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/get arguments are invalid.", callId);
        }

        EmailGetRequest request = Codec.DecodeEmailGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateGetRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        EmailGetRequest backendRequest = CompleteGetRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        EmailGetResponse stored = emailService.GetStoredEmailsAsync(backendRequest, context, cancellationToken)
            .GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null &&
            stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "Returning all Emails would exceed the get object limit.", callId);
        }

        EmailGetResponse response = NormalizeServerGetResponse(stored, request);
        return GetSuccessInvocation(response, request, callId);
    }

    internal Task<JmapInvocation> ProcessChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessChangesInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsChangesRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/changes arguments are invalid.", callId);
        }

        EmailChangesRequest request = Codec.DecodeEmailChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateChangesRequest(request);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailChangesResponse response = emailService.GetStoredEmailChangesAsync(request, context, cancellationToken)
            .GetAwaiter().GetResult();
        ValidateChangesResponse(response, request);
        return ChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessQueryInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailQuery);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/query arguments are invalid.", callId);
        }

        EmailQueryRequest request = QueryCodec.DecodeEmailQueryRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateQueryRequest(request, options.MaxJsonDepth, null, null);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        JmapMailCapabilities mail = GetMailCapabilities(session, request.AccountId);
        requestError = Sorter.ValidateComparators(request.Sort, mail.EmailQuerySortOptions,
            GetSupportedCollations(session));
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailQueryResponse response = emailService.QueryStoredEmailsAsync(request, context, cancellationToken)
            .GetAwaiter().GetResult();
        ValidateQueryResponse(response, request);
        return QuerySuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryChangesInvocation(invocation, session, emailService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessQueryChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailQueryChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryChangesRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/queryChanges arguments are invalid.", callId);
        }

        EmailQueryChangesRequest request = QueryCodec.DecodeEmailQueryChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateQueryChangesRequest(request, options.MaxJsonDepth, null, null);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        JmapMailCapabilities mail = GetMailCapabilities(session, request.AccountId);
        requestError = Sorter.ValidateComparators(request.Sort, mail.EmailQuerySortOptions,
            GetSupportedCollations(session));
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailQueryChangesResponse response = emailService.GetStoredEmailQueryChangesAsync(
            request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateQueryChangesResponse(response, request);
        if (ExceedsMaximumQueryChanges(response, request))
        {
            return ErrorInvocation(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges,
                "The Email query changes exceed maxChanges.", callId);
        }

        return QueryChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/set arguments are invalid.", callId);
        }

        EmailSetRequest request = Codec.DecodeEmailSetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateSetRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (session.Accounts[accountId].IsReadOnly)
        {
            return ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly,
                "The account is read-only.", callId);
        }

        EmailGetRequest snapshotRequest = new EmailGetRequest();
        snapshotRequest.AccountId = request.AccountId;
        snapshotRequest.Ids = new List<string>();
        snapshotRequest.Properties = new List<string> { JmapProperties.Id };
        cancellationToken.ThrowIfCancellationRequested();
        EmailGetResponse stored = emailService.GetStoredEmailsAsync(snapshotRequest, context, cancellationToken)
            .GetAwaiter().GetResult();
        EmailGetResponse snapshot = NormalizeServerGetResponse(stored, snapshotRequest);
        if (request.IfInState != null && request.IfInState != snapshot.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifInState value does not match the current Email state.", callId);
        }

        EmailSetResponse local = new EmailSetResponse();
        local.AccountId = request.AccountId;
        local.OldState = snapshot.State;
        local.NewState = snapshot.State;
        EmailSetRequest backend = PrepareSetRequest(request, local, GetMailCapabilities(session, request.AccountId));
        EmailSetResponse response = local;
        if (CountSetActions(backend) != 0L)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = emailService.SetStoredEmailsAsync(backend, context, cancellationToken)
                .GetAwaiter().GetResult();
            ValidateSetResponse(response, backend);
            MergeSetFailures(response, local);
        }

        ValidateSetResponse(response, request);
        return SetSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessCopyInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessCopyInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessCopyInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailCopy);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsCopyRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/copy arguments are invalid.", callId);
        }

        EmailCopyRequest request = Codec.DecodeEmailCopyRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateCopyRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? sourceError = ValidateSourceMailAccount(session, request.FromAccountId);
        if (sourceError != null)
        {
            return ErrorInvocation(sourceError.Type, sourceError.Kind, sourceError.Description, callId);
        }

        JmapMethodError? targetError = ValidateMailAccount(session, request.AccountId);
        if (targetError != null)
        {
            return ErrorInvocation(targetError.Type, targetError.Kind, targetError.Description, callId);
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (session.Accounts[accountId].IsReadOnly)
        {
            return ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly,
                "The destination account is read-only.", callId);
        }

        EmailGetResponse source = ReadEmailState(request.FromAccountId, emailService, context, cancellationToken);
        EmailGetResponse target = ReadEmailState(request.AccountId, emailService, context, cancellationToken);
        if (request.IfFromInState != null && request.IfFromInState != source.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifFromInState value does not match the current source Email state.", callId);
        }

        if (request.IfInState != null && request.IfInState != target.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifInState value does not match the current destination Email state.", callId);
        }

        EmailCopyPreparation preparation = PrepareCopyRequest(
            request, target.State, GetMailCapabilities(session, request.AccountId));
        EmailCopyResponse response = preparation.Local;
        if (preparation.Backend.Create.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = emailService.CopyStoredEmailsAsync(preparation.Backend, context, cancellationToken)
                .GetAwaiter().GetResult();
            ValidateCopyResponse(response, preparation.Backend);
            if (response.OldState != target.State)
            {
                throw ProtocolFailure("The Email/copy backend used an unexpected destination state.",
                    JmapProperties.OldState, default(JsonElement));
            }

            CompleteDuplicateCopies(response, preparation.DuplicateOf);
            MergeCopyFailures(response, preparation.Local);
        }

        ValidateCopyResponse(response, request);
        return CopySuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessImportInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessImportInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessImportInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailImport);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsImportRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/import arguments are invalid.", callId);
        }

        EmailImportRequest request = Codec.DecodeEmailImportDispatchRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateImportRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (session.Accounts[accountId].IsReadOnly)
        {
            return ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly,
                "The account is read-only.", callId);
        }

        EmailGetResponse snapshot = ReadEmailState(request.AccountId, emailService, context, cancellationToken);
        if (request.IfInState != null && request.IfInState != snapshot.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                "The ifInState value does not match the current Email state.", callId);
        }

        EmailImportResponse local = new EmailImportResponse();
        local.AccountId = request.AccountId;
        local.OldState = snapshot.State;
        local.NewState = snapshot.State;
        EmailImportRequest backend = PrepareImportRequest(
            request, local, GetMailCapabilities(session, request.AccountId));
        EmailImportResponse response = local;
        if (backend.Emails.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = emailService.ImportStoredEmailsAsync(backend, context, cancellationToken)
                .GetAwaiter().GetResult();
            ValidateImportResponse(response, backend);
            if (response.OldState != snapshot.State)
            {
                throw ProtocolFailure("The Email/import backend used an unexpected Email state.",
                    JmapProperties.OldState, default(JsonElement));
            }

            MergeImportFailures(response, local);
        }

        ValidateImportResponse(response, request);
        return ImportSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessParseInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessParseInvocation(invocation, session, emailService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessParseInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailService emailService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailParse);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsParseRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/parse arguments are invalid.", callId);
        }

        EmailParseRequest request = Codec.DecodeEmailParseRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, emailService, context, options);
        JmapMethodError? requestError = ValidateParseRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        EmailParseRequest backend = CompleteParseRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        EmailParseResponse response = emailService.ParseStoredEmailBlobsAsync(backend, context, cancellationToken)
            .GetAwaiter().GetResult();
        ValidateParseResponse(response, request);
        return ParseSuccessInvocation(response, request, callId);
    }

    internal JmapInvocation CreateGetInvocation(EmailGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/get arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateChangesInvocation(EmailChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/changes arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryInvocation(EmailQueryRequest request, string callId)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64, null, null);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/query arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        QueryCodec.EncodeEmailQueryRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailQuery, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryChangesInvocation(EmailQueryChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64, null, null);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/queryChanges arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        QueryCodec.EncodeEmailQueryChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(EmailSetRequest request, string callId)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/set arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailSetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateCopyInvocation(EmailCopyRequest request, string callId)
    {
        JmapMethodError? error = ValidateCopyRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/copy arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailCopyRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailCopy, callId, stream.ToArray());
    }

    internal JmapInvocation CreateImportInvocation(EmailImportRequest request, string callId)
    {
        JmapMethodError? error = ValidateImportRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/import arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailImportRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailImport, callId, stream.ToArray());
    }

    internal JmapInvocation CreateParseInvocation(EmailParseRequest request, string callId)
    {
        JmapMethodError? error = ValidateParseRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Email/parse arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailParseRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailParse, callId, stream.ToArray());
    }

    internal EmailGetResponse MapGetResponse(JmapResponse response, string callId, EmailGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailGet);
        ValidateGetProjection(arguments, request);
        EmailGetResponse decoded = Codec.DecodeEmailGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal EmailChangesResponse MapChangesResponse(
        JmapResponse response,
        string callId,
        EmailChangesRequest request)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/changes arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailChanges);
        EmailChangesResponse decoded = Codec.DecodeEmailChangesResponse(arguments);
        ValidateChangesResponse(decoded, request);
        return decoded;
    }

    internal EmailQueryResponse MapQueryResponse(
        JmapResponse response,
        string callId,
        EmailQueryRequest request)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64, null, null);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/query arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailQuery);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailQuery);
        EmailQueryResponse decoded = QueryCodec.DecodeEmailQueryResponse(arguments);
        ValidateQueryResponse(decoded, request);
        return decoded;
    }

    internal EmailQueryChangesResponse MapQueryChangesResponse(
        JmapResponse response,
        string callId,
        EmailQueryChangesRequest request)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64, null, null);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/queryChanges arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailQueryChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailQueryChanges);
        EmailQueryChangesResponse decoded = QueryCodec.DecodeEmailQueryChangesResponse(arguments);
        ValidateQueryChangesResponse(decoded, request);
        if (ExceedsMaximumQueryChanges(decoded, request))
        {
            throw ProtocolFailure("Email/queryChanges returned more changes than maxChanges permits.",
                "methodResponses/1", arguments);
        }

        return decoded;
    }

    internal EmailSetResponse MapSetResponse(JmapResponse response, string callId, EmailSetRequest request)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailSet);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.EmailSet, request.AccountId, request.IfInState);
        ValidateSetCreatedWireProperties(arguments);
        EmailSetResponse decoded = Codec.DecodeEmailSetResponse(arguments);
        ValidateSetResponse(decoded, request);
        return decoded;
    }

    internal EmailCopyResponse MapCopyResponse(JmapResponse response, string callId, EmailCopyRequest request)
    {
        JmapMethodError? error = ValidateCopyRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/copy arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailCopy);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.EmailCopy, request.AccountId, request.IfInState);
        ValidateCopyCreatedWireProperties(arguments);
        EmailCopyResponse decoded = Codec.DecodeEmailCopyResponse(arguments);
        ValidateCopyResponse(decoded, request);
        return decoded;
    }

    internal EmailImportResponse MapImportResponse(
        JmapResponse response,
        string callId,
        EmailImportRequest request)
    {
        JmapMethodError? error = ValidateImportRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/import arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailImport);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.EmailImport, request.AccountId, request.IfInState);
        ValidateImportCreatedWireProperties(arguments);
        EmailImportResponse decoded = Codec.DecodeEmailImportResponse(arguments);
        ValidateImportResponse(decoded, request);
        return decoded;
    }

    internal EmailParseResponse MapParseResponse(
        JmapResponse response,
        string callId,
        EmailParseRequest request)
    {
        JmapMethodError? error = ValidateParseRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Email/parse arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailParse);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailParse);
        ValidateParseProjection(arguments, request);
        EmailParseResponse decoded = Codec.DecodeEmailParseResponse(arguments);
        ValidateParseResponse(decoded, request);
        return decoded;
    }

    internal void ValidateSetCreatedWireProperties(JsonElement arguments)
    {
        JsonElement created = Codec.Values.GetMember(arguments, JmapProperties.Created);
        if (created.ValueKind == JsonValueKind.Undefined || created.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (created.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The Email/set created map is invalid.", JmapProperties.Created, created);
        }

        foreach (JsonProperty item in created.EnumerateObject())
        {
            JsonElement email = item.Value;
            if (email.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(email, JmapProperties.Id).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.BlobId).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.ThreadId).ValueKind != JsonValueKind.String ||
                !IsRequiredUnsignedInteger(Codec.Values.GetMember(email, JmapProperties.Size)))
            {
                throw ProtocolFailure("A created Email must contain id, blobId, threadId, and size.",
                    JmapProperties.Created + "/" + item.Name, email);
            }
        }
    }

    internal bool IsRequiredUnsignedInteger(JsonElement value)
    {
        return value.ValueKind != JsonValueKind.Undefined && IsOptionalUnsignedInteger(value);
    }

    internal JmapMethodError? ValidateGetRequest(EmailGetRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid get object limit is required.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                    "The get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The ids argument contains an invalid identifier.");
                }
            }
        }

        if (!AreEmailPropertiesValid(request.Properties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The properties argument contains an unsupported property or header form.");
        }

        if (!AreBodyPropertiesValid(request.BodyProperties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The bodyProperties argument contains an unsupported property or header form.");
        }

        if (request.FetchTextBodyValues && !request.HasFetchTextBodyValues ||
            request.FetchHtmlBodyValues && !request.HasFetchHtmlBodyValues ||
            request.FetchAllBodyValues && !request.HasFetchAllBodyValues ||
            request.MaxBodyValueBytes != 0L && !request.HasMaxBodyValueBytes)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/get option presence flags are inconsistent.");
        }

        if (request.HasMaxBodyValueBytes && !Values.IsUnsignedInt(request.MaxBodyValueBytes))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxBodyValueBytes must be an UnsignedInt.");
        }

        return null;
    }

    internal JmapMethodError? ValidateChangesRequest(EmailChangesRequest? request)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (request.SinceState == null || !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid sinceState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxChanges must be null or a positive UnsignedInt.");
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(EmailSetRequest? request, int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (maximumObjectsInSet < 0 || request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/set limit or ifInState value is invalid.");
        }

        if (CountSetActions(request) > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The set request contains too many object actions.");
        }

        if (request.Create != null)
        {
            foreach (string creationId in request.Create.Keys)
            {
                if (!Values.IsCreationId(creationId))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The create map contains an invalid creation id.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The update map contains an invalid Email id.");
                }
            }
        }

        if (request.Destroy != null)
        {
            HashSet<string> destroyed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Destroy)
            {
                if (!Values.IsId(id) || !destroyed.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                        "The destroy list contains an invalid or duplicate Email id.");
                }
            }
        }

        return null;
    }

    internal long CountSetActions(EmailSetRequest request)
    {
        long count = 0L;
        if (request.Create != null)
        {
            count = count + request.Create.Count;
        }

        if (request.Update != null)
        {
            count = count + request.Update.Count;
        }

        if (request.Destroy != null)
        {
            count = count + request.Destroy.Count;
        }

        return count;
    }

    internal JmapMethodError? ValidateCopyRequest(EmailCopyRequest? request, int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.FromAccountId) || !Values.IsId(request.AccountId) ||
            request.FromAccountId == request.AccountId)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "Distinct valid source and destination account ids are required.");
        }

        if (maximumObjectsInSet < 0 || request.Create == null || request.Create.Count > maximumObjectsInSet)
        {
            return MethodError(request.Create != null && maximumObjectsInSet >= 0 ? JmapErrorCodes.RequestTooLarge :
                JmapErrorCodes.InvalidArguments, request.Create != null && maximumObjectsInSet >= 0 ?
                JmapMethodErrorKind.RequestTooLarge : JmapMethodErrorKind.InvalidArguments,
                "The Email/copy create map or object limit is invalid.");
        }

        if (request.IfFromInState != null && !Values.IsValidUnicode(request.IfFromInState) ||
            request.IfInState != null && !Values.IsValidUnicode(request.IfInState) ||
            request.DestroyFromIfInState != null && !Values.IsValidUnicode(request.DestroyFromIfInState) ||
            request.OnSuccessDestroyOriginal && !request.HasOnSuccessDestroyOriginal)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "An Email/copy state or boolean presence flag is invalid.");
        }

        foreach (string creationId in request.Create.Keys)
        {
            if (!Values.IsCreationId(creationId))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The copy create map contains an invalid creation id.");
            }
        }

        return null;
    }

    internal EmailGetResponse ReadEmailState(
        string? accountId,
        IEmailService emailService,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        EmailGetRequest request = new EmailGetRequest();
        request.AccountId = accountId;
        request.Ids = new List<string>();
        request.Properties = new List<string> { JmapProperties.Id };
        cancellationToken.ThrowIfCancellationRequested();
        EmailGetResponse stored = emailService.GetStoredEmailsAsync(request, context, cancellationToken)
            .GetAwaiter().GetResult();
        return NormalizeServerGetResponse(stored, request);
    }

    internal EmailCopyPreparation PrepareCopyRequest(
        EmailCopyRequest request,
        string? targetState,
        JmapMailCapabilities capabilities)
    {
        EmailCopyPreparation result = new EmailCopyPreparation();
        result.Backend.FromAccountId = request.FromAccountId;
        result.Backend.IfFromInState = request.IfFromInState;
        result.Backend.AccountId = request.AccountId;
        result.Backend.IfInState = request.IfInState;
        result.Backend.OnSuccessDestroyOriginal = request.OnSuccessDestroyOriginal;
        result.Backend.HasOnSuccessDestroyOriginal = request.HasOnSuccessDestroyOriginal;
        result.Backend.DestroyFromIfInState = request.DestroyFromIfInState;
        CopyUnknown(request.UnknownProperties, result.Backend.UnknownProperties);
        result.Local.FromAccountId = request.FromAccountId;
        result.Local.AccountId = request.AccountId;
        result.Local.OldState = targetState;
        result.Local.NewState = targetState;
        Dictionary<string, string> firstBySource = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, Email> item in request.Create)
        {
            List<string> invalid = ValidateCopyEmail(item.Value);
            if (invalid.Count != 0)
            {
                AddCopyFailure(result.Local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                    JmapSetErrorKind.InvalidProperties, "The Email copy has invalid properties.", invalid));
            }
            else if (capabilities.HasMaxMailboxesPerEmail && !capabilities.IsMaxMailboxesPerEmailNull &&
                item.Value.MailboxIds != null && item.Value.MailboxIds.Count > capabilities.MaxMailboxesPerEmail)
            {
                AddCopyFailure(result.Local, item.Key, SetError(JmapErrorCodes.TooManyMailboxes,
                    JmapSetErrorKind.TooManyMailboxes, "The copied Email belongs to too many Mailboxes.", null));
            }
            else
            {
                string sourceId = item.Value.Id == null ? "" : item.Value.Id;
                if (firstBySource.ContainsKey(sourceId))
                {
                    result.DuplicateOf.Add(item.Key, firstBySource[sourceId]);
                }
                else
                {
                    firstBySource.Add(sourceId, item.Key);
                    result.Backend.Create.Add(item.Key, item.Value);
                }
            }
        }

        return result;
    }

    internal List<string> ValidateCopyEmail(Email? email)
    {
        List<string> invalid = SetValues.EmailValues.ValidateCopy(email);
        if (email != null && email.Keywords != null)
        {
            SetValues.ValidateKeywordRules(email.Keywords, JmapProperties.Keywords, invalid);
        }

        return UniquePaths(invalid);
    }

    internal JmapMethodError? ValidateImportRequest(EmailImportRequest? request, int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (maximumObjectsInSet < 0 || request.Emails == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid Email/import object limit and emails map are required.");
        }

        if (request.Emails.Count > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The import request contains too many Emails.");
        }

        if (request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The ifInState value is not valid Unicode.");
        }

        foreach (string creationId in request.Emails.Keys)
        {
            if (!Values.IsCreationId(creationId))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The emails map contains an invalid creation id.");
            }
        }

        return null;
    }

    internal EmailImportRequest PrepareImportRequest(
        EmailImportRequest request,
        EmailImportResponse local,
        JmapMailCapabilities capabilities)
    {
        EmailImportRequest backend = new EmailImportRequest();
        backend.AccountId = request.AccountId;
        backend.IfInState = request.IfInState;
        CopyUnknown(request.UnknownProperties, backend.UnknownProperties);
        foreach (KeyValuePair<string, EmailImport> item in request.Emails)
        {
            List<string> invalid = ValidateImportEmail(item.Value);
            if (invalid.Count != 0)
            {
                AddImportFailure(local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                    JmapSetErrorKind.InvalidProperties, "The Email import has invalid properties.", invalid));
            }
            else if (capabilities.HasMaxMailboxesPerEmail && !capabilities.IsMaxMailboxesPerEmailNull &&
                item.Value.MailboxIds.Count > capabilities.MaxMailboxesPerEmail)
            {
                AddImportFailure(local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                    JmapSetErrorKind.InvalidProperties, "The Email import has too many Mailboxes.",
                    new List<string> { JmapProperties.MailboxIds }));
            }
            else
            {
                backend.Emails.Add(item.Key, item.Value);
            }
        }

        return backend;
    }

    internal List<string> ValidateImportEmail(EmailImport? emailImport)
    {
        List<string> invalid = SetValues.EmailValues.ValidateImport(emailImport);
        if (emailImport != null)
        {
            SetValues.ValidateKeywordRules(emailImport.Keywords, JmapProperties.Keywords, invalid);
        }

        return UniquePaths(invalid);
    }

    internal JmapMethodError? ValidateParseRequest(EmailParseRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId) || request.BlobIds == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and blobIds array are required.");
        }

        if (maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid parse object limit is required.");
        }

        if (request.BlobIds.Count > maximumObjectsInGet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The parse request contains too many blob identifiers.");
        }

        foreach (string blobId in request.BlobIds)
        {
            if (!Values.IsId(blobId))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The blobIds argument contains an invalid identifier.");
            }
        }

        if (!AreEmailPropertiesValid(request.Properties) || !AreBodyPropertiesValid(request.BodyProperties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/parse property selection is invalid.");
        }

        if (request.FetchTextBodyValues && !request.HasFetchTextBodyValues ||
            request.FetchHtmlBodyValues && !request.HasFetchHtmlBodyValues ||
            request.FetchAllBodyValues && !request.HasFetchAllBodyValues ||
            request.MaxBodyValueBytes != 0L && !request.HasMaxBodyValueBytes)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/parse option presence flags are inconsistent.");
        }

        if (request.HasMaxBodyValueBytes && !Values.IsUnsignedInt(request.MaxBodyValueBytes))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxBodyValueBytes must be an UnsignedInt.");
        }

        return null;
    }

    internal EmailParseRequest CompleteParseRequest(EmailParseRequest request)
    {
        EmailGetRequest projection = ParseProjectionRequest(request);
        EmailGetRequest completed = CompleteGetRequest(projection);
        EmailParseRequest result = new EmailParseRequest();
        result.AccountId = request.AccountId;
        result.BlobIds = new List<string>(request.BlobIds);
        result.Properties = completed.Properties;
        if (result.Properties != null && !EffectiveParseProperties(request.Properties).Contains(JmapProperties.Id))
        {
            result.Properties.Remove(JmapProperties.Id);
        }

        result.BodyProperties = completed.BodyProperties;
        result.FetchTextBodyValues = completed.FetchTextBodyValues;
        result.HasFetchTextBodyValues = completed.HasFetchTextBodyValues;
        result.FetchHtmlBodyValues = completed.FetchHtmlBodyValues;
        result.HasFetchHtmlBodyValues = completed.HasFetchHtmlBodyValues;
        result.FetchAllBodyValues = completed.FetchAllBodyValues;
        result.HasFetchAllBodyValues = completed.HasFetchAllBodyValues;
        result.MaxBodyValueBytes = completed.MaxBodyValueBytes;
        result.HasMaxBodyValueBytes = completed.HasMaxBodyValueBytes;
        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal EmailGetRequest ParseProjectionRequest(EmailParseRequest request)
    {
        EmailGetRequest result = new EmailGetRequest();
        result.AccountId = request.AccountId;
        result.Properties = EffectiveParseProperties(request.Properties);
        result.BodyProperties = request.BodyProperties == null ? null : new List<string>(request.BodyProperties);
        result.FetchTextBodyValues = request.FetchTextBodyValues;
        result.HasFetchTextBodyValues = request.HasFetchTextBodyValues;
        result.FetchHtmlBodyValues = request.FetchHtmlBodyValues;
        result.HasFetchHtmlBodyValues = request.HasFetchHtmlBodyValues;
        result.FetchAllBodyValues = request.FetchAllBodyValues;
        result.HasFetchAllBodyValues = request.HasFetchAllBodyValues;
        result.MaxBodyValueBytes = request.MaxBodyValueBytes;
        result.HasMaxBodyValueBytes = request.HasMaxBodyValueBytes;
        return result;
    }

    internal List<string> EffectiveParseProperties(List<string>? properties)
    {
        List<string> source = properties == null ? DefaultParseProperties() : properties;
        List<string> result = new List<string>();
        foreach (string property in source)
        {
            AddUniqueValue(property, result);
        }

        return result;
    }

    internal List<string> DefaultParseProperties()
    {
        return new List<string>
        {
            JmapProperties.MessageId, JmapProperties.InReplyTo, JmapProperties.References,
            JmapProperties.Sender, JmapProperties.From, JmapProperties.To, JmapProperties.Cc,
            JmapProperties.Bcc, JmapProperties.ReplyTo, JmapProperties.Subject, JmapProperties.SentAt,
            JmapProperties.HasAttachment, JmapProperties.Preview, JmapProperties.BodyValues,
            JmapProperties.TextBody, JmapProperties.HtmlBody, JmapProperties.Attachments
        };
    }

    internal EmailSetRequest PrepareSetRequest(
        EmailSetRequest request,
        EmailSetResponse local,
        JmapMailCapabilities capabilities)
    {
        EmailSetRequest backend = new EmailSetRequest();
        backend.AccountId = request.AccountId;
        backend.IfInState = request.IfInState;
        CopyUnknown(request.UnknownProperties, backend.UnknownProperties);
        if (request.Create != null)
        {
            backend.Create = new Dictionary<string, Email>();
            foreach (KeyValuePair<string, Email> item in request.Create)
            {
                List<string> invalid = ValidateDraftCreate(item.Value);
                if (invalid.Count != 0)
                {
                    AddNotCreated(local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                        JmapSetErrorKind.InvalidProperties, "The draft Email has invalid properties.", invalid));
                }
                else if (capabilities.HasMaxMailboxesPerEmail && !capabilities.IsMaxMailboxesPerEmailNull &&
                    item.Value.MailboxIds != null && item.Value.MailboxIds.Count > capabilities.MaxMailboxesPerEmail)
                {
                    AddNotCreated(local, item.Key, SetError(JmapErrorCodes.TooManyMailboxes,
                        JmapSetErrorKind.TooManyMailboxes, "The draft belongs to too many Mailboxes.", null));
                }
                else if (KnownAttachmentSize(item.Value, capabilities) >
                    capabilities.MaxSizeAttachmentsPerEmail)
                {
                    AddNotCreated(local, item.Key, SetError(JmapErrorCodes.TooLarge,
                        JmapSetErrorKind.TooLarge, "The draft attachments exceed the account limit.", null));
                }
                else
                {
                    backend.Create.Add(item.Key, item.Value);
                }
            }
        }

        HashSet<string> destroyed = request.Destroy == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(request.Destroy, StringComparer.Ordinal);
        if (request.Update != null)
        {
            backend.Update = new Dictionary<string, JmapPatchObject>();
            foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
            {
                if (destroyed.Contains(item.Key))
                {
                    AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.WillDestroy,
                        JmapSetErrorKind.WillDestroy, "The Email will be destroyed in this request.", null));
                }
                else if (!SetValues.IsPatchStructureValid(item.Value))
                {
                    AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.InvalidPatch,
                        JmapSetErrorKind.InvalidPatch, "The Email patch is invalid.", null));
                }
                else
                {
                    List<string> invalid = ValidateEmailPatch(item.Value);
                    if (invalid.Count != 0)
                    {
                        AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.InvalidProperties,
                            JmapSetErrorKind.InvalidProperties, "The Email patch has invalid properties.", invalid));
                    }
                    else
                    {
                        backend.Update.Add(item.Key, item.Value);
                    }
                }
            }
        }

        if (request.Destroy != null)
        {
            backend.Destroy = new List<string>(request.Destroy);
        }

        return backend;
    }

    internal long KnownAttachmentSize(Email email, JmapMailCapabilities capabilities)
    {
        EmailSetValidationContext context = new EmailSetValidationContext();
        context.Capabilities = capabilities;
        return SetValues.AttachmentSize(email, context);
    }

    internal List<string> ValidateDraftCreate(Email? email)
    {
        List<string> invalid = SetValues.EmailValues.ValidateCreate(email);
        if (email == null)
        {
            return invalid;
        }

        MimeWriter.ValidateWriterValues(email, invalid);
        if (email.Keywords != null)
        {
            SetValues.ValidateKeywordRules(email.Keywords, JmapProperties.Keywords, invalid);
        }

        return UniquePaths(invalid);
    }

    internal List<string> ValidateEmailPatch(JmapPatchObject patch)
    {
        List<string> invalid = SetValues.EmailValues.ValidateWritablePatch(patch);
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == JmapProperties.MailboxIds)
            {
                ValidateBooleanMapPatch(item.Value, false, item.Key, invalid);
            }
            else if (item.Key == JmapProperties.Keywords)
            {
                if (item.Value.ValueKind != JsonValueKind.Null)
                {
                    ValidateBooleanMapPatch(item.Value, true, item.Key, invalid);
                }
            }
            else if (SetValues.IsSingleChildPath(item.Key, JmapProperties.MailboxIds + "/"))
            {
                string key = SetValues.DecodeChildKey(item.Key, JmapProperties.MailboxIds + "/");
                if ((!Values.IsId(key) && !Values.IsCreationIdReference(key)) || !IsPatchBooleanValue(item.Value))
                {
                    invalid.Add(item.Key);
                }
            }
            else if (SetValues.IsSingleChildPath(item.Key, JmapProperties.Keywords + "/"))
            {
                string key = SetValues.DecodeChildKey(item.Key, JmapProperties.Keywords + "/");
                if (!Values.IsKeyword(key) || key == "$recent" || !IsPatchBooleanValue(item.Value))
                {
                    invalid.Add(item.Key);
                }
            }
        }

        return UniquePaths(invalid);
    }

    internal void ValidateBooleanMapPatch(JsonElement value, bool keywords, string path, List<string> invalid)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            invalid.Add(path);
            return;
        }

        int count = 0;
        bool junk = false;
        bool notJunk = false;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            bool valid = keywords ? Values.IsKeyword(property.Name) && property.Name != "$recent" :
                Values.IsId(property.Name) || Values.IsCreationIdReference(property.Name);
            if (!valid || property.Value.ValueKind != JsonValueKind.True)
            {
                invalid.Add(path + "/" + SetValues.Pointers.EscapeToken(property.Name));
            }

            junk = junk || property.Name == JmapMailKeywords.Junk;
            notJunk = notJunk || property.Name == JmapMailKeywords.NotJunk;
            count = count + 1;
        }

        if (!keywords && count == 0)
        {
            invalid.Add(path);
        }

        if (keywords && junk && notJunk)
        {
            invalid.Add(path + "/" + JmapMailKeywords.Junk);
            invalid.Add(path + "/" + JmapMailKeywords.NotJunk);
        }
    }

    internal bool IsPatchBooleanValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.Null;
    }

    internal List<string> UniquePaths(List<string> paths)
    {
        List<string> result = new List<string>();
        foreach (string path in paths)
        {
            if (!result.Contains(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    internal JmapMethodError? ValidateQueryRequest(
        EmailQueryRequest? request,
        int maximumFilterDepth,
        List<string>? advertisedSortProperties,
        List<string>? supportedCollations)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid filter depth limit is required.");
        }

        if (request.Position != 0L && !request.HasPosition ||
            request.HasPosition && !Values.IsInt(request.Position))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "position must be an Int with consistent presence flags.");
        }

        if (request.Anchor != null && !Values.IsId(request.Anchor))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "anchor must be an Id.");
        }

        if (request.AnchorOffset != 0L && !request.HasAnchorOffset ||
            request.HasAnchorOffset && !Values.IsInt(request.AnchorOffset))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "anchorOffset must be an Int with consistent presence flags.");
        }

        if (request.IsLimitNull && !request.HasLimit || request.Limit != 0L && !request.HasLimit ||
            request.HasLimit && !request.IsLimitNull && !Values.IsUnsignedInt(request.Limit))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "limit must be null or an UnsignedInt with consistent presence flags.");
        }

        if (request.CalculateTotal && !request.HasCalculateTotal ||
            request.CollapseThreads && !request.HasCollapseThreads)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/query boolean presence flags are inconsistent.");
        }

        JmapMethodError? filterError = Filters.ValidateFilter(request.Filter, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return Sorter.ValidateComparators(request.Sort, advertisedSortProperties, supportedCollations);
    }

    internal JmapMethodError? ValidateQueryChangesRequest(
        EmailQueryChangesRequest? request,
        int maximumFilterDepth,
        List<string>? advertisedSortProperties,
        List<string>? supportedCollations)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId is required.");
        }

        if (maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid filter depth limit is required.");
        }

        if (request.SinceQueryState == null || !Values.IsValidUnicode(request.SinceQueryState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid sinceQueryState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges ||
            request.MaxChanges != 0L && !request.HasMaxChanges ||
            request.HasMaxChanges && !request.IsMaxChangesNull && !Values.IsUnsignedInt(request.MaxChanges))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxChanges must be null or an UnsignedInt with consistent presence flags.");
        }

        if (request.UpToId != null && !Values.IsId(request.UpToId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "upToId must be an Id.");
        }

        if (request.CalculateTotal && !request.HasCalculateTotal ||
            request.CollapseThreads && !request.HasCollapseThreads)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The Email/queryChanges boolean presence flags are inconsistent.");
        }

        JmapMethodError? filterError = Filters.ValidateFilter(request.Filter, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return Sorter.ValidateComparators(request.Sort, advertisedSortProperties, supportedCollations);
    }

    internal bool AreEmailPropertiesValid(List<string>? properties)
    {
        if (properties == null)
        {
            return true;
        }

        HashSet<string> supported = EmailPropertySet();
        foreach (string property in properties)
        {
            if (property == null)
            {
                return false;
            }

            if (!supported.Contains(property) && Headers.ParsePropertyForValidation(property) == null)
            {
                return false;
            }
        }

        return true;
    }

    internal bool AreBodyPropertiesValid(List<string>? properties)
    {
        if (properties == null)
        {
            return true;
        }

        HashSet<string> supported = BodyPropertySet();
        foreach (string property in properties)
        {
            if (property == null)
            {
                return false;
            }

            if (!supported.Contains(property) && Headers.ParsePropertyForValidation(property) == null)
            {
                return false;
            }
        }

        return true;
    }

    internal EmailGetRequest CompleteGetRequest(EmailGetRequest request)
    {
        EmailGetRequest result = new EmailGetRequest();
        result.AccountId = request.AccountId;
        if (request.Ids != null)
        {
            result.Ids = new List<string>(request.Ids);
        }

        List<string> requested = EffectiveEmailProperties(request.Properties);
        result.Properties = new List<string>();
        AddUniqueValue(JmapProperties.Id, result.Properties);
        foreach (string property in requested)
        {
            if (Headers.ParsePropertyForValidation(property) != null)
            {
                AddUniqueValue(JmapProperties.Headers, result.Properties);
            }
            else
            {
                AddUniqueValue(property, result.Properties);
            }
        }

        bool bodyValues = requested.Contains(JmapProperties.BodyValues);
        bool fetchText = bodyValues && request.FetchTextBodyValues;
        bool fetchHtml = bodyValues && request.FetchHtmlBodyValues;
        bool fetchAll = bodyValues && request.FetchAllBodyValues;
        if (fetchAll)
        {
            fetchText = false;
            fetchHtml = false;
            AddUniqueValue(JmapProperties.BodyStructure, result.Properties);
        }
        else
        {
            if (fetchText)
            {
                AddUniqueValue(JmapProperties.TextBody, result.Properties);
            }

            if (fetchHtml)
            {
                AddUniqueValue(JmapProperties.HtmlBody, result.Properties);
            }
        }

        if (HasBodyPartProjection(result.Properties))
        {
            result.BodyProperties = EffectiveBodyProperties(request.BodyProperties);
            AddUniqueValue(JmapProperties.PartId, result.BodyProperties);
            AddUniqueValue(JmapProperties.Type, result.BodyProperties);
            AddUniqueValue(JmapProperties.SubParts, result.BodyProperties);
            if (ContainsDynamicHeaderProperty(result.BodyProperties))
            {
                AddUniqueValue(JmapProperties.Headers, result.BodyProperties);
            }
        }

        result.FetchTextBodyValues = fetchText;
        result.HasFetchTextBodyValues = fetchText;
        result.FetchHtmlBodyValues = fetchHtml;
        result.HasFetchHtmlBodyValues = fetchHtml;
        result.FetchAllBodyValues = fetchAll;
        result.HasFetchAllBodyValues = fetchAll;
        result.MaxBodyValueBytes = bodyValues ? request.MaxBodyValueBytes : 0L;
        result.HasMaxBodyValueBytes = bodyValues && request.HasMaxBodyValueBytes;
        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal EmailGetResponse NormalizeServerGetResponse(EmailGetResponse? stored, EmailGetRequest request)
    {
        ValidateGetEnvelope(stored, request);
        if (stored == null)
        {
            throw ProtocolFailure("An Email/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Email> records = IndexEmails(stored.List);
        ValidateGetPartition(stored, request, records);
        EmailGetResponse result = new EmailGetResponse();
        result.AccountId = stored.AccountId;
        result.State = stored.State;
        CopyUnknown(stored.UnknownProperties, result.UnknownProperties);
        if (request.Ids == null)
        {
            result.List.AddRange(stored.List);
            return result;
        }

        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in request.Ids)
        {
            if (added.Add(id))
            {
                if (records.ContainsKey(id))
                {
                    result.List.Add(records[id]);
                }
                else
                {
                    result.NotFound.Add(id);
                }
            }
        }

        return result;
    }

    internal void ValidateClientGetResponse(EmailGetResponse? response, EmailGetRequest request)
    {
        ValidateGetEnvelope(response, request);
        if (response == null)
        {
            throw ProtocolFailure("An Email/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Email> records = IndexEmails(response.List);
        ValidateGetPartition(response, request, records);
    }

    internal void ValidateChangesResponse(EmailChangesResponse? response, EmailChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.OldState != request.SinceState ||
            response.NewState == null || !Values.IsValidUnicode(response.NewState) || response.Created == null ||
            response.Updated == null || response.Destroyed == null)
        {
            throw ProtocolFailure("The Email/changes response has invalid account, state, or collection metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> classified = new HashSet<string>(StringComparer.Ordinal);
        ValidateChangeIds(response.Created, classified, JmapProperties.Created);
        ValidateChangeIds(response.Updated, classified, JmapProperties.Updated);
        ValidateChangeIds(response.Destroyed, classified, JmapProperties.Destroyed);
        long changeCount = response.Created.Count + response.Updated.Count + response.Destroyed.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && changeCount > request.MaxChanges)
        {
            throw ProtocolFailure("Email/changes returned more identifiers than maxChanges permits.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangeIds(List<string> ids, HashSet<string> classified, string path)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !classified.Add(id))
            {
                throw ProtocolFailure("An Email change identifier is invalid or has multiple classifications.",
                    path, default(JsonElement));
            }
        }
    }

    internal void ValidateQueryResponse(EmailQueryResponse? response, EmailQueryRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.QueryState == null ||
            !Values.IsValidUnicode(response.QueryState) || !Values.IsUnsignedInt(response.Position) ||
            response.Ids == null)
        {
            throw ProtocolFailure("The Email/query response has invalid account, state, position, or result metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in response.Ids)
        {
            if (!Values.IsId(id) || !ids.Add(id))
            {
                throw ProtocolFailure("The Email/query result contains an invalid or duplicate identifier.",
                    "ids/" + index, default(JsonElement));
            }

            index = index + 1;
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested || response.HasTotal && !Values.IsUnsignedInt(response.Total))
        {
            throw ProtocolFailure("The Email/query total metadata does not match the request.", "total",
                default(JsonElement));
        }

        if (response.HasTotal && response.Ids.Count != 0 &&
            (response.Position > response.Total || response.Ids.Count > response.Total - response.Position))
        {
            throw ProtocolFailure("The Email/query result window exceeds the reported total.", "ids",
                default(JsonElement));
        }

        if (response.HasLimit)
        {
            if (!Values.IsUnsignedInt(response.Limit) || response.Ids.Count > response.Limit)
            {
                throw ProtocolFailure("The Email/query enforced limit is invalid.", "limit",
                    default(JsonElement));
            }

            if (request.HasLimit && !request.IsLimitNull && response.Limit >= request.Limit)
            {
                throw ProtocolFailure("The Email/query response reports a limit that is not lower than requested.",
                    "limit", default(JsonElement));
            }
        }

        if (request.HasLimit && !request.IsLimitNull && response.Ids.Count > request.Limit)
        {
            throw ProtocolFailure("The Email/query result exceeds the requested limit.", "ids",
                default(JsonElement));
        }

        if (request.Anchor == null && (!request.HasPosition || request.Position >= 0L))
        {
            long expectedPosition = request.HasPosition ? request.Position : 0L;
            if (response.Position != expectedPosition)
            {
                throw ProtocolFailure("The Email/query response position does not match the requested window.",
                    "position", default(JsonElement));
            }
        }
    }

    internal void ValidateQueryChangesResponse(
        EmailQueryChangesResponse? response,
        EmailQueryChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId ||
            response.OldQueryState != request.SinceQueryState || response.NewQueryState == null ||
            !Values.IsValidUnicode(response.NewQueryState) || response.Removed == null || response.Added == null)
        {
            throw ProtocolFailure("The Email/queryChanges response has invalid account, state, or collection metadata.",
                "methodResponses/1", default(JsonElement));
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested || response.HasTotal && !Values.IsUnsignedInt(response.Total))
        {
            throw ProtocolFailure("The Email/queryChanges total metadata does not match the request.", "total",
                default(JsonElement));
        }

        HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal);
        int removedIndex = 0;
        foreach (string id in response.Removed)
        {
            if (!Values.IsId(id) || !removed.Add(id))
            {
                throw ProtocolFailure("The Email/queryChanges removed list contains an invalid or duplicate id.",
                    "removed/" + removedIndex, default(JsonElement));
            }

            removedIndex = removedIndex + 1;
        }

        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        long previousIndex = 0L;
        bool hasPreviousIndex = false;
        int addedIndex = 0;
        foreach (JmapAddedItem item in response.Added)
        {
            if (item == null || !Values.IsId(item.Id) || !Values.IsUnsignedInt(item.Index) ||
                hasPreviousIndex && item.Index < previousIndex || item.Id == null || !added.Add(item.Id))
            {
                throw ProtocolFailure("The Email/queryChanges added list is invalid or not ordered by index.",
                    "added/" + addedIndex, default(JsonElement));
            }

            if (response.HasTotal && item.Index >= response.Total)
            {
                throw ProtocolFailure("An Email/queryChanges added index exceeds the reported total.",
                    "added/" + addedIndex + "/index", default(JsonElement));
            }

            previousIndex = item.Index;
            hasPreviousIndex = true;
            addedIndex = addedIndex + 1;
        }
    }

    internal bool ExceedsMaximumQueryChanges(
        EmailQueryChangesResponse response,
        EmailQueryChangesRequest request)
    {
        if (!request.HasMaxChanges || request.IsMaxChangesNull)
        {
            return false;
        }

        long count = (long)response.Removed.Count + response.Added.Count;
        return count > request.MaxChanges;
    }

    internal void ValidateSetResponse(EmailSetResponse? response, EmailSetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState)
        {
            throw ProtocolFailure("The Email/set response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        ValidateSetCreatePartitions(response, request.Create);
        ValidateSetUpdatePartitions(response, request.Update);
        ValidateSetDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateCopyResponse(EmailCopyResponse? response, EmailCopyRequest request)
    {
        if (response == null || response.FromAccountId != request.FromAccountId ||
            response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState)
        {
            throw ProtocolFailure("The Email/copy response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> expected = new HashSet<string>(request.Create.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, Email> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || !IsValidSetCreatedEmail(item.Value))
                {
                    throw ProtocolFailure("The Email/copy created map is invalid.", JmapProperties.Created,
                        default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            foreach (KeyValuePair<string, JmapSetError> item in response.NotCreated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
                {
                    throw ProtocolFailure("The Email/copy notCreated map is invalid.", JmapProperties.NotCreated,
                        default(JsonElement));
                }

                ValidateCopySetError(item.Value, item.Key);
            }
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/copy response does not partition every create.",
                JmapProperties.Created, default(JsonElement));
        }
    }

    internal void ValidateCopySetError(JmapSetError error, string creationId)
    {
        JmapSetError normalized = Errors.NormalizeSetError(error);
        bool allowed = normalized.Kind == JmapSetErrorKind.Forbidden ||
            normalized.Kind == JmapSetErrorKind.OverQuota || normalized.Kind == JmapSetErrorKind.TooLarge ||
            normalized.Kind == JmapSetErrorKind.RateLimit || normalized.Kind == JmapSetErrorKind.NotFound ||
            normalized.Kind == JmapSetErrorKind.InvalidProperties ||
            normalized.Kind == JmapSetErrorKind.AlreadyExists ||
            normalized.Kind == JmapSetErrorKind.TooManyKeywords ||
            normalized.Kind == JmapSetErrorKind.TooManyMailboxes ||
            normalized.Kind == JmapSetErrorKind.Unknown;
        if (!allowed)
        {
            throw ProtocolFailure("Email/copy returned an invalid SetError type.",
                JmapProperties.NotCreated + "/" + creationId + "/type", default(JsonElement));
        }

        if (normalized.Kind == JmapSetErrorKind.InvalidProperties && normalized.Properties == null)
        {
            throw ProtocolFailure("An invalidProperties copy error must identify the invalid properties.",
                JmapProperties.NotCreated + "/" + creationId + "/properties", default(JsonElement));
        }

        if (normalized.Kind == JmapSetErrorKind.AlreadyExists && !Values.IsId(normalized.ExistingId))
        {
            throw ProtocolFailure("An alreadyExists copy error must identify the existing Email.",
                JmapProperties.NotCreated + "/" + creationId + "/existingId", default(JsonElement));
        }

        error.Kind = normalized.Kind;
    }

    internal void CompleteDuplicateCopies(
        EmailCopyResponse response,
        Dictionary<string, string> duplicateOf)
    {
        foreach (KeyValuePair<string, string> duplicate in duplicateOf)
        {
            if (response.Created != null && response.Created.ContainsKey(duplicate.Value))
            {
                Email created = response.Created[duplicate.Value];
                JmapSetError error = SetError(JmapErrorCodes.AlreadyExists, JmapSetErrorKind.AlreadyExists,
                    "The source Email was already copied by this request.", null);
                error.ExistingId = created.Id;
                AddCopyFailure(response, duplicate.Key, error);
            }
            else if (response.NotCreated != null && response.NotCreated.ContainsKey(duplicate.Value))
            {
                JmapSetError primary = Errors.NormalizeSetError(response.NotCreated[duplicate.Value]);
                AddCopyFailure(response, duplicate.Key, primary);
            }
            else
            {
                throw ProtocolFailure("A duplicate Email copy has no primary result.",
                    JmapProperties.NotCreated + "/" + duplicate.Key, default(JsonElement));
            }
        }
    }

    internal void MergeCopyFailures(EmailCopyResponse target, EmailCopyResponse source)
    {
        if (source.NotCreated == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JmapSetError> item in source.NotCreated)
        {
            AddCopyFailure(target, item.Key, item.Value);
        }
    }

    internal void AddCopyFailure(EmailCopyResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void ValidateCopyCreatedWireProperties(JsonElement arguments)
    {
        JsonElement created = Codec.Values.GetMember(arguments, JmapProperties.Created);
        if (created.ValueKind == JsonValueKind.Undefined || created.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (created.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The Email/copy created map is invalid.", JmapProperties.Created, created);
        }

        foreach (JsonProperty item in created.EnumerateObject())
        {
            JsonElement email = item.Value;
            if (email.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(email, JmapProperties.Id).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.BlobId).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.ThreadId).ValueKind != JsonValueKind.String ||
                !IsRequiredUnsignedInteger(Codec.Values.GetMember(email, JmapProperties.Size)))
            {
                throw ProtocolFailure("A copied Email must contain id, blobId, threadId, and size.",
                    JmapProperties.Created + "/" + item.Name, email);
            }
        }
    }

    internal void ValidateImportResponse(EmailImportResponse? response, EmailImportRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState)
        {
            throw ProtocolFailure("The Email/import response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> expected = new HashSet<string>(request.Emails.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, Email> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || !IsValidSetCreatedEmail(item.Value))
                {
                    throw ProtocolFailure("The Email/import created map is invalid.", JmapProperties.Created,
                        default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            foreach (KeyValuePair<string, JmapSetError> item in response.NotCreated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
                {
                    throw ProtocolFailure("The Email/import notCreated map is invalid.",
                        JmapProperties.NotCreated, default(JsonElement));
                }

                ValidateImportSetError(item.Value, item.Key);
            }
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/import response does not partition every requested Email.",
                JmapProperties.Created, default(JsonElement));
        }
    }

    internal void ValidateImportSetError(JmapSetError error, string creationId)
    {
        JmapSetError normalized = Errors.NormalizeSetError(error);
        bool allowed = normalized.Kind == JmapSetErrorKind.InvalidProperties ||
            normalized.Kind == JmapSetErrorKind.OverQuota || normalized.Kind == JmapSetErrorKind.InvalidEmail ||
            normalized.Kind == JmapSetErrorKind.AlreadyExists || normalized.Kind == JmapSetErrorKind.Unknown;
        if (!allowed)
        {
            throw ProtocolFailure("Email/import returned an invalid SetError type.",
                JmapProperties.NotCreated + "/" + creationId + "/type", default(JsonElement));
        }

        if (normalized.Kind == JmapSetErrorKind.InvalidProperties && normalized.Properties == null)
        {
            throw ProtocolFailure("An invalidProperties import error must identify the invalid properties.",
                JmapProperties.NotCreated + "/" + creationId + "/properties", default(JsonElement));
        }

        if (normalized.Kind == JmapSetErrorKind.AlreadyExists && !Values.IsId(normalized.ExistingId))
        {
            throw ProtocolFailure("An alreadyExists import error must identify the existing Email.",
                JmapProperties.NotCreated + "/" + creationId + "/existingId", default(JsonElement));
        }

        error.Kind = normalized.Kind;
    }

    internal void MergeImportFailures(EmailImportResponse target, EmailImportResponse source)
    {
        if (source.NotCreated == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JmapSetError> item in source.NotCreated)
        {
            AddImportFailure(target, item.Key, item.Value);
        }
    }

    internal void AddImportFailure(EmailImportResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void ValidateImportCreatedWireProperties(JsonElement arguments)
    {
        JsonElement created = Codec.Values.GetMember(arguments, JmapProperties.Created);
        if (created.ValueKind == JsonValueKind.Undefined || created.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (created.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The Email/import created map is invalid.", JmapProperties.Created, created);
        }

        foreach (JsonProperty item in created.EnumerateObject())
        {
            JsonElement email = item.Value;
            if (email.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(email, JmapProperties.Id).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.BlobId).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(email, JmapProperties.ThreadId).ValueKind != JsonValueKind.String ||
                !IsRequiredUnsignedInteger(Codec.Values.GetMember(email, JmapProperties.Size)))
            {
                throw ProtocolFailure("An imported Email must contain id, blobId, threadId, and size.",
                    JmapProperties.Created + "/" + item.Name, email);
            }
        }
    }

    internal void ValidateParseResponse(EmailParseResponse? response, EmailParseRequest request)
    {
        if (response == null || response.AccountId != request.AccountId)
        {
            throw ProtocolFailure("The Email/parse response has invalid account metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> expected = new HashSet<string>(request.BlobIds, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Parsed != null)
        {
            foreach (KeyValuePair<string, Email> item in response.Parsed)
            {
                Email email = item.Value;
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || email == null ||
                    email.Id != null || email.MailboxIds != null || email.Keywords != null ||
                    email.ReceivedAt != null)
                {
                    throw ProtocolFailure("The Email/parse parsed map contains invalid or stored metadata.",
                        JmapProperties.Parsed + "/" + item.Key, default(JsonElement));
                }
            }
        }

        ValidateParseFailureIds(response.NotParsable, expected, seen, JmapProperties.NotParsable);
        ValidateParseFailureIds(response.NotFound, expected, seen, JmapProperties.NotFound);
        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/parse response does not partition every requested blob.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateParseFailureIds(
        List<string>? values,
        HashSet<string> expected,
        HashSet<string> seen,
        string path)
    {
        if (values == null)
        {
            return;
        }

        foreach (string id in values)
        {
            if (!Values.IsId(id) || !expected.Contains(id) || !seen.Add(id))
            {
                throw ProtocolFailure("An Email/parse failure partition is invalid.", path,
                    default(JsonElement));
            }
        }
    }

    internal void ValidateParseProjection(JsonElement arguments, EmailParseRequest request)
    {
        JsonElement parsed = Codec.Values.GetMember(arguments, JmapProperties.Parsed);
        if (parsed.ValueKind == JsonValueKind.Undefined || parsed.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (parsed.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("The Email/parse parsed map is invalid.", JmapProperties.Parsed, parsed);
        }

        List<string> includedList = EffectiveParseProperties(request.Properties);
        HashSet<string> included = new HashSet<string>(includedList, StringComparer.Ordinal);
        foreach (JsonProperty item in parsed.EnumerateObject())
        {
            JsonElement email = item.Value;
            if (email.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("A parsed Email must be an object.",
                    JmapProperties.Parsed + "/" + item.Name, email);
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(email, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("Email/parse omitted a requested property.",
                        JmapProperties.Parsed + "/" + item.Name + "/" + property, email);
                }
            }

            foreach (JsonProperty property in email.EnumerateObject())
            {
                if ((EmailPropertySet().Contains(property.Name) ||
                    Headers.ParsePropertyForValidation(property.Name) != null) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("Email/parse returned an unrequested property.",
                        JmapProperties.Parsed + "/" + item.Name + "/" + property.Name, email);
                }
            }

            ValidateParseNullMetadata(email, included, item.Name);
            ValidateParseProjectedBodyMembers(email, request, item.Name);
        }
    }

    internal void ValidateParseProjectedBodyMembers(
        JsonElement email,
        EmailParseRequest request,
        string blobId)
    {
        List<string> names = new List<string>
        {
            JmapProperties.BodyStructure, JmapProperties.TextBody, JmapProperties.HtmlBody,
            JmapProperties.Attachments
        };
        foreach (string name in names)
        {
            JsonElement value = Codec.Values.GetMember(email, name);
            string path = JmapProperties.Parsed + "/" + blobId + "/" + name;
            if (value.ValueKind == JsonValueKind.Object)
            {
                ValidateProjectedBodyPart(value, request.BodyProperties, path);
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement part in value.EnumerateArray())
                {
                    ValidateProjectedBodyPart(part, request.BodyProperties, path + "/" + index);
                    index = index + 1;
                }
            }
        }
    }

    internal void ValidateParseNullMetadata(JsonElement email, HashSet<string> included, string blobId)
    {
        List<string> nullableMetadata = new List<string>
        {
            JmapProperties.Id, JmapProperties.MailboxIds, JmapProperties.Keywords, JmapProperties.ReceivedAt
        };
        foreach (string property in nullableMetadata)
        {
            if (included.Contains(property) &&
                Codec.Values.GetMember(email, property).ValueKind != JsonValueKind.Null)
            {
                throw ProtocolFailure("Parsed Email store metadata must be null.",
                    JmapProperties.Parsed + "/" + blobId + "/" + property, email);
            }
        }
    }

    internal void ValidateSetCreatePartitions(EmailSetResponse response, Dictionary<string, Email>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, Email> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || !IsValidSetCreatedEmail(item.Value))
                {
                    throw ProtocolFailure("The Email/set created map is invalid.", "created", default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, JmapProperties.NotCreated, "create");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/set response does not partition every create.", "created",
                default(JsonElement));
        }
    }

    internal void ValidateSetUpdatePartitions(
        EmailSetResponse response,
        Dictionary<string, JmapPatchObject>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Updated != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in response.Updated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) ||
                    item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.Object)
                {
                    throw ProtocolFailure("The Email/set updated map is invalid.", "updated", default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, JmapProperties.NotUpdated, "update");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/set response does not partition every update.", "updated",
                default(JsonElement));
        }
    }

    internal void ValidateSetDestroyPartitions(EmailSetResponse response, List<string>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                if (!expected.Contains(id) || !seen.Add(id))
                {
                    throw ProtocolFailure("The Email/set destroyed list is invalid.", "destroyed",
                        default(JsonElement));
                }
            }
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, JmapProperties.NotDestroyed, "destroy");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Email/set response does not partition every destroy.", "destroyed",
                default(JsonElement));
        }
    }

    internal void ValidateSetErrors(
        Dictionary<string, JmapSetError> errors,
        HashSet<string> expected,
        HashSet<string> seen,
        string path,
        string action)
    {
        foreach (KeyValuePair<string, JmapSetError> item in errors)
        {
            if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
            {
                throw ProtocolFailure("An Email/set error partition is invalid.", path, default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            if (!IsAllowedEmailSetError(normalized.Kind, action))
            {
                throw ProtocolFailure("Email/set returned a SetError that is invalid for this action.",
                    path + "/" + item.Key + "/type", default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.BlobNotFound &&
                (normalized.NotFound == null || normalized.NotFound.Count == 0))
            {
                throw ProtocolFailure("A blobNotFound SetError must identify the missing blobs.",
                    path + "/" + item.Key + "/notFound", default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.InvalidProperties && normalized.Properties == null)
            {
                throw ProtocolFailure("An invalidProperties SetError must identify the invalid properties.",
                    path + "/" + item.Key + "/properties", default(JsonElement));
            }

            item.Value.Kind = normalized.Kind;
        }
    }

    internal bool IsAllowedEmailSetError(JmapSetErrorKind kind, string action)
    {
        if (kind == JmapSetErrorKind.Unknown)
        {
            return true;
        }

        if (action == "create")
        {
            return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.OverQuota ||
                kind == JmapSetErrorKind.TooLarge || kind == JmapSetErrorKind.RateLimit ||
                kind == JmapSetErrorKind.InvalidProperties || kind == JmapSetErrorKind.BlobNotFound ||
                kind == JmapSetErrorKind.TooManyKeywords || kind == JmapSetErrorKind.TooManyMailboxes;
        }

        if (action == "update")
        {
            return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.NotFound ||
                kind == JmapSetErrorKind.InvalidPatch || kind == JmapSetErrorKind.WillDestroy ||
                kind == JmapSetErrorKind.InvalidProperties || kind == JmapSetErrorKind.TooManyKeywords ||
                kind == JmapSetErrorKind.TooManyMailboxes;
        }

        return kind == JmapSetErrorKind.Forbidden || kind == JmapSetErrorKind.NotFound;
    }

    internal bool IsValidSetCreatedEmail(Email? email)
    {
        return email != null && Values.IsId(email.Id) && Values.IsId(email.BlobId) &&
            Values.IsId(email.ThreadId) && Values.IsUnsignedInt(email.Size) && email.MailboxIds == null &&
            email.Keywords == null && email.ReceivedAt == null && email.Headers == null &&
            email.HeaderProperties != null && email.HeaderProperties.Count == 0 && email.MessageId == null &&
            email.InReplyTo == null && email.References == null && email.Sender == null && email.From == null &&
            email.To == null && email.Cc == null && email.Bcc == null && email.ReplyTo == null &&
            email.Subject == null && email.SentAt == null && email.BodyStructure == null && email.BodyValues == null &&
            email.TextBody == null && email.HtmlBody == null && email.Attachments == null && !email.HasAttachment &&
            email.Preview == null;
    }

    internal void MergeSetFailures(EmailSetResponse target, EmailSetResponse source)
    {
        MergeSetErrorMap(target, source.NotCreated, "create");
        MergeSetErrorMap(target, source.NotUpdated, "update");
        MergeSetErrorMap(target, source.NotDestroyed, "destroy");
    }

    internal void MergeSetErrorMap(EmailSetResponse target, Dictionary<string, JmapSetError>? source, string action)
    {
        if (source == null)
        {
            return;
        }

        Dictionary<string, JmapSetError>? destination = action == "create" ? target.NotCreated :
            action == "update" ? target.NotUpdated : target.NotDestroyed;
        if (destination == null)
        {
            destination = new Dictionary<string, JmapSetError>();
            if (action == "create")
            {
                target.NotCreated = destination;
            }
            else if (action == "update")
            {
                target.NotUpdated = destination;
            }
            else
            {
                target.NotDestroyed = destination;
            }
        }

        foreach (KeyValuePair<string, JmapSetError> item in source)
        {
            destination.Add(item.Key, item.Value);
        }
    }

    internal void AddNotCreated(EmailSetResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(EmailSetResponse response, string id, JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal JmapSetError SetError(
        string type,
        JmapSetErrorKind kind,
        string description,
        List<string>? properties)
    {
        JmapSetError error = new JmapSetError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        error.Properties = properties;
        return error;
    }

    internal void ValidateGetEnvelope(EmailGetResponse? response, EmailGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null)
        {
            throw ProtocolFailure("The Email/get response has invalid account, state, or collection metadata.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal Dictionary<string, Email> IndexEmails(List<Email> emails)
    {
        Dictionary<string, Email> records = new Dictionary<string, Email>(StringComparer.Ordinal);
        int index = 0;
        foreach (Email email in emails)
        {
            if (email == null || !Values.IsId(email.Id) || email.Id == null || records.ContainsKey(email.Id))
            {
                throw ProtocolFailure("The Email/get response contains an invalid or duplicate Email.",
                    "list/" + index, default(JsonElement));
            }

            records.Add(email.Id, email);
            index = index + 1;
        }

        return records;
    }

    internal void ValidateGetPartition(
        EmailGetResponse response,
        EmailGetRequest request,
        Dictionary<string, Email> records)
    {
        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !notFound.Add(id) || records.ContainsKey(id))
            {
                throw ProtocolFailure("The Email/get notFound partition is invalid.", "notFound",
                    default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (notFound.Count != 0)
            {
                throw ProtocolFailure("Email/get cannot return notFound identifiers when all Emails were requested.",
                    "notFound", default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        foreach (string id in records.Keys)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Email/get returned an unrequested Email.", "list", default(JsonElement));
            }
        }

        foreach (string id in notFound)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Email/get returned an unrequested notFound identifier.", "notFound",
                    default(JsonElement));
            }
        }

        if (records.Count + notFound.Count != requested.Count)
        {
            throw ProtocolFailure("Email/get did not classify every requested identifier.", "methodResponses/1",
                default(JsonElement));
        }
    }

    internal void ValidateGetProjection(JsonElement arguments, EmailGetRequest request)
    {
        JsonElement list = Codec.Values.GetMember(arguments, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw ProtocolFailure("The Email/get result list is invalid.", "list", list);
        }

        List<string> includedList = EffectiveEmailProperties(request.Properties);
        AddUniqueValue(JmapProperties.Id, includedList);
        HashSet<string> included = new HashSet<string>(includedList, StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement email in list.EnumerateArray())
        {
            if (email.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("An Email/get result item is invalid.", "list/" + index, email);
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(email, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("Email/get omitted a requested property.",
                        "list/" + index + "/" + property, email);
                }
            }

            foreach (JsonProperty property in email.EnumerateObject())
            {
                if ((EmailPropertySet().Contains(property.Name) ||
                    Headers.ParsePropertyForValidation(property.Name) != null) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("Email/get returned an unrequested property.",
                        "list/" + index + "/" + property.Name, email);
                }
            }

            ValidateProjectedBodyMembers(email, request, index);
            index = index + 1;
        }
    }

    internal void ValidateProjectedBodyMembers(JsonElement email, EmailGetRequest request, int emailIndex)
    {
        List<string> names = new List<string>
        {
            JmapProperties.BodyStructure,
            JmapProperties.TextBody,
            JmapProperties.HtmlBody,
            JmapProperties.Attachments
        };
        foreach (string name in names)
        {
            JsonElement value = Codec.Values.GetMember(email, name);
            if (value.ValueKind == JsonValueKind.Object)
            {
                ValidateProjectedBodyPart(value, request.BodyProperties, "list/" + emailIndex + "/" + name);
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                int partIndex = 0;
                foreach (JsonElement part in value.EnumerateArray())
                {
                    ValidateProjectedBodyPart(part, request.BodyProperties,
                        "list/" + emailIndex + "/" + name + "/" + partIndex);
                    partIndex = partIndex + 1;
                }
            }
        }
    }

    internal void ValidateProjectedBodyPart(JsonElement part, List<string>? bodyProperties, string path)
    {
        if (part.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolFailure("An EmailBodyPart projection is invalid.", path, part);
        }

        List<string> requested = EffectiveBodyProperties(bodyProperties);
        HashSet<string> included = new HashSet<string>(requested, StringComparer.Ordinal);
        foreach (JsonProperty property in part.EnumerateObject())
        {
            bool structural = property.Name == JmapProperties.SubParts;
            if (!structural && (BodyPropertySet().Contains(property.Name) ||
                Headers.ParsePropertyForValidation(property.Name) != null) && !included.Contains(property.Name))
            {
                throw ProtocolFailure("Email/get returned an unrequested EmailBodyPart property.",
                    path + "/" + property.Name, part);
            }
        }

        foreach (string property in included)
        {
            if (Codec.Values.GetMember(part, property).ValueKind == JsonValueKind.Undefined)
            {
                throw ProtocolFailure("Email/get omitted a requested EmailBodyPart property.",
                    path + "/" + property, part);
            }
        }

        JsonElement children = Codec.Values.GetMember(part, JmapProperties.SubParts);
        if (children.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement child in children.EnumerateArray())
            {
                ValidateProjectedBodyPart(child, bodyProperties, path + "/subParts/" + index);
                index = index + 1;
            }
        }
    }

    internal JmapInvocation GetSuccessInvocation(EmailGetResponse response, EmailGetRequest request, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteProjectedGetResponse(response, request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailGet, callId, stream.ToArray());
    }

    internal JmapInvocation ChangesSuccessInvocation(EmailChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailChanges, callId, stream.ToArray());
    }

    internal JmapInvocation QuerySuccessInvocation(EmailQueryResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        QueryCodec.EncodeEmailQueryResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailQuery, callId, stream.ToArray());
    }

    internal JmapInvocation QueryChangesSuccessInvocation(EmailQueryChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        QueryCodec.EncodeEmailQueryChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation SetSuccessInvocation(EmailSetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailSetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSet, callId, stream.ToArray());
    }

    internal JmapInvocation CopySuccessInvocation(EmailCopyResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailCopyResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailCopy, callId, stream.ToArray());
    }

    internal JmapInvocation ImportSuccessInvocation(EmailImportResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeEmailImportResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailImport, callId, stream.ToArray());
    }

    internal JmapInvocation ParseSuccessInvocation(
        EmailParseResponse response,
        EmailParseRequest request,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteProjectedParseResponse(response, request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailParse, callId, stream.ToArray());
    }

    internal void WriteProjectedParseResponse(
        EmailParseResponse response,
        EmailParseRequest request,
        JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, response.AccountId, "/accountId");
        if (response.Parsed != null)
        {
            writer.WriteMemberName(JmapProperties.Parsed);
            writer.WriteObjectStart();
            foreach (string blobId in Codec.Values.SortNames(response.Parsed.Keys))
            {
                writer.WriteMemberName(blobId);
                WriteProjectedParsedEmail(response.Parsed[blobId], request, writer,
                    "/parsed/" + blobId);
            }

            writer.WriteObjectEnd();
        }

        Codec.Optional.WriteOptionalStringArray(
            JmapProperties.NotParsable, response.NotParsable, writer, "/notParsable");
        Codec.Optional.WriteOptionalStringArray(JmapProperties.NotFound, response.NotFound, writer, "/notFound");
        Codec.Values.WriteUnknownMembers(response.UnknownProperties, Codec.EmailParseResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void WriteProjectedParsedEmail(
        Email email,
        EmailParseRequest request,
        JmapJsonWriter writer,
        string path)
    {
        if (email == null)
        {
            throw ProtocolFailure("A parsed Email cannot be null.", path, default(JsonElement));
        }

        EmailGetRequest projection = ParseProjectionRequest(request);
        writer.WriteObjectStart();
        foreach (string property in EffectiveParseProperties(request.Properties))
        {
            if (property == JmapProperties.Id || property == JmapProperties.MailboxIds ||
                property == JmapProperties.Keywords || property == JmapProperties.ReceivedAt)
            {
                writer.WriteMemberName(property);
                writer.WriteNullValue();
            }
            else if (property == JmapProperties.ThreadId)
            {
                writer.WriteStringMember(property, email.ThreadId, true);
            }
            else
            {
                WriteEmailProperty(email, property, projection, writer, path);
            }
        }

        writer.WriteObjectEnd();
    }

    internal void WriteProjectedGetResponse(EmailGetResponse response, EmailGetRequest request, JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, response.AccountId, "/accountId");
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.State, response.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (Email email in response.List)
        {
            WriteProjectedEmail(email, request, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Codec.Values.EncodeStringArray(response.NotFound, writer, "/notFound");
        Codec.Values.WriteUnknownMembers(response.UnknownProperties,
            new List<string> { JmapProperties.AccountId, JmapProperties.State, JmapProperties.List,
                JmapProperties.NotFound }, writer);
        writer.WriteObjectEnd();
    }

    internal void WriteProjectedEmail(Email email, EmailGetRequest request, JmapJsonWriter writer, string path)
    {
        if (email == null)
        {
            throw ProtocolFailure("An Email/get record cannot be null.", path, default(JsonElement));
        }

        List<string> properties = EffectiveEmailProperties(request.Properties);
        AddUniqueValue(JmapProperties.Id, properties);
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.Id, email.Id, path + "/id");
        foreach (string property in properties)
        {
            if (property != JmapProperties.Id)
            {
                WriteEmailProperty(email, property, request, writer, path);
            }
        }

        writer.WriteObjectEnd();
    }

    internal void WriteEmailProperty(Email email, string property, EmailGetRequest request,
        JmapJsonWriter writer, string path)
    {
        if (property == JmapProperties.BlobId)
        {
            Codec.Values.WriteRequiredStringMember(writer, property, email.BlobId, path + "/blobId");
        }
        else if (property == JmapProperties.ThreadId)
        {
            Codec.Values.WriteRequiredStringMember(writer, property, email.ThreadId, path + "/threadId");
        }
        else if (property == JmapProperties.MailboxIds)
        {
            WriteRequiredBooleanMap(email.MailboxIds, property, writer, path + "/mailboxIds");
        }
        else if (property == JmapProperties.Keywords)
        {
            WriteRequiredBooleanMap(email.Keywords, property, writer, path + "/keywords");
        }
        else if (property == JmapProperties.Size)
        {
            Codec.Values.WriteUnsignedIntegerMember(writer, property, email.Size, path + "/size");
        }
        else if (property == JmapProperties.ReceivedAt)
        {
            Codec.Values.WriteRequiredStringMember(writer, property, email.ReceivedAt, path + "/receivedAt");
        }
        else if (property == JmapProperties.Headers)
        {
            WriteRequiredHeaders(email.Headers, property, writer, path + "/headers");
        }
        else if (property == JmapProperties.MessageId)
        {
            WriteNullableStringArray(property, email.MessageId, writer, path + "/messageId");
        }
        else if (property == JmapProperties.InReplyTo)
        {
            WriteNullableStringArray(property, email.InReplyTo, writer, path + "/inReplyTo");
        }
        else if (property == JmapProperties.References)
        {
            WriteNullableStringArray(property, email.References, writer, path + "/references");
        }
        else if (property == JmapProperties.Sender)
        {
            WriteNullableAddresses(property, email.Sender, writer, path + "/sender");
        }
        else if (property == JmapProperties.From)
        {
            WriteNullableAddresses(property, email.From, writer, path + "/from");
        }
        else if (property == JmapProperties.To)
        {
            WriteNullableAddresses(property, email.To, writer, path + "/to");
        }
        else if (property == JmapProperties.Cc)
        {
            WriteNullableAddresses(property, email.Cc, writer, path + "/cc");
        }
        else if (property == JmapProperties.Bcc)
        {
            WriteNullableAddresses(property, email.Bcc, writer, path + "/bcc");
        }
        else if (property == JmapProperties.ReplyTo)
        {
            WriteNullableAddresses(property, email.ReplyTo, writer, path + "/replyTo");
        }
        else if (property == JmapProperties.Subject)
        {
            writer.WriteStringMember(property, email.Subject, true);
        }
        else if (property == JmapProperties.SentAt)
        {
            writer.WriteStringMember(property, email.SentAt, true);
        }
        else if (property == JmapProperties.BodyStructure)
        {
            WriteRequiredBodyStructure(email.BodyStructure, request.BodyProperties, writer,
                path + "/bodyStructure");
        }
        else if (property == JmapProperties.BodyValues)
        {
            WriteSelectedBodyValues(email, request, writer, path + "/bodyValues");
        }
        else if (property == JmapProperties.TextBody)
        {
            WriteRequiredBodyPartList(email.TextBody, property, request.BodyProperties, writer, path + "/textBody");
        }
        else if (property == JmapProperties.HtmlBody)
        {
            WriteRequiredBodyPartList(email.HtmlBody, property, request.BodyProperties, writer, path + "/htmlBody");
        }
        else if (property == JmapProperties.Attachments)
        {
            WriteRequiredBodyPartList(email.Attachments, property, request.BodyProperties, writer,
                path + "/attachments");
        }
        else if (property == JmapProperties.HasAttachment)
        {
            writer.WriteBooleanMember(property, email.HasAttachment, true);
        }
        else if (property == JmapProperties.Preview)
        {
            Codec.Values.WriteRequiredStringMember(writer, property, email.Preview, path + "/preview");
        }
        else if (Headers.ParsePropertyForValidation(property) != null)
        {
            if (email.Headers == null)
            {
                throw ProtocolFailure("Dynamic Email headers require source headers.", path + "/" + property,
                    default(JsonElement));
            }

            writer.WriteElementMember(property, Headers.SelectProperty(email.Headers, property));
        }
    }

    internal void WriteProjectedBodyPart(EmailBodyPart part, List<string>? bodyProperties,
        JmapJsonWriter writer, string path)
    {
        if (part == null)
        {
            throw ProtocolFailure("An EmailBodyPart cannot be null.", path, default(JsonElement));
        }

        List<string> properties = EffectiveBodyProperties(bodyProperties);
        writer.WriteObjectStart();
        foreach (string property in properties)
        {
            WriteBodyPartProperty(part, property, writer, path);
        }

        if (part.SubParts != null)
        {
            writer.WriteMemberName(JmapProperties.SubParts);
            writer.WriteArrayStart();
            int index = 0;
            foreach (EmailBodyPart child in part.SubParts)
            {
                WriteProjectedBodyPart(child, bodyProperties, writer, path + "/subParts/" + index);
                index = index + 1;
            }

            writer.WriteArrayEnd();
        }
        else if (properties.Contains(JmapProperties.SubParts))
        {
            writer.WriteMemberName(JmapProperties.SubParts);
            writer.WriteNullValue();
        }

        writer.WriteObjectEnd();
    }

    internal void WriteBodyPartProperty(EmailBodyPart part, string property, JmapJsonWriter writer, string path)
    {
        if (property == JmapProperties.PartId)
        {
            writer.WriteStringMember(property, part.PartId, true);
        }
        else if (property == JmapProperties.BlobId)
        {
            writer.WriteStringMember(property, part.BlobId, true);
        }
        else if (property == JmapProperties.Size)
        {
            Codec.Values.WriteUnsignedIntegerMember(writer, property, part.Size, path + "/size");
        }
        else if (property == JmapProperties.Headers)
        {
            WriteRequiredHeaders(part.Headers, property, writer, path + "/headers");
        }
        else if (property == JmapProperties.Name)
        {
            writer.WriteStringMember(property, part.Name, true);
        }
        else if (property == JmapProperties.Type)
        {
            Codec.Values.WriteRequiredStringMember(writer, property, part.Type, path + "/type");
        }
        else if (property == JmapProperties.Charset)
        {
            writer.WriteStringMember(property, part.Charset, true);
        }
        else if (property == JmapProperties.Disposition)
        {
            writer.WriteStringMember(property, part.Disposition, true);
        }
        else if (property == JmapProperties.Cid)
        {
            writer.WriteStringMember(property, part.Cid, true);
        }
        else if (property == JmapProperties.Language)
        {
            WriteNullableStringArray(property, part.Language, writer, path + "/language");
        }
        else if (property == JmapProperties.Location)
        {
            writer.WriteStringMember(property, part.Location, true);
        }
        else if (property == JmapProperties.SubParts)
        {
        }
        else if (Headers.ParsePropertyForValidation(property) != null)
        {
            if (part.Headers == null)
            {
                throw ProtocolFailure("Dynamic EmailBodyPart headers require source headers.",
                    path + "/" + property, default(JsonElement));
            }

            writer.WriteElementMember(property, Headers.SelectProperty(part.Headers, property));
        }
    }

    internal void WriteSelectedBodyValues(Email email, EmailGetRequest request, JmapJsonWriter writer, string path)
    {
        Dictionary<string, string> selectedTypes = SelectBodyValueTypes(email, request);
        writer.WriteMemberName(JmapProperties.BodyValues);
        writer.WriteObjectStart();
        foreach (string partId in Codec.Values.SortNames(selectedTypes.Keys))
        {
            if (email.BodyValues == null || !email.BodyValues.ContainsKey(partId) ||
                email.BodyValues[partId] == null || email.BodyValues[partId].Value == null)
            {
                throw ProtocolFailure("Email/get could not supply a selected body value.", path + "/" + partId,
                    default(JsonElement));
            }

            EmailBodyValue source = email.BodyValues[partId];
            string sourceValue = source.Value == null ? "" : source.Value;
            BodyTextTruncationResult truncated = BodyParts.TruncateUtf8(sourceValue, request.MaxBodyValueBytes,
                string.Equals(selectedTypes[partId], "text/html", StringComparison.OrdinalIgnoreCase));
            writer.WriteMemberName(partId);
            writer.WriteObjectStart();
            Codec.Values.WriteRequiredStringMember(writer, JmapProperties.Value, truncated.Value,
                path + "/" + partId + "/value");
            writer.WriteBooleanMember(JmapProperties.IsEncodingProblem, source.IsEncodingProblem, true);
            writer.WriteBooleanMember(JmapProperties.IsTruncated, source.IsTruncated || truncated.IsTruncated, true);
            writer.WriteObjectEnd();
        }

        writer.WriteObjectEnd();
    }

    internal Dictionary<string, string> SelectBodyValueTypes(Email email, EmailGetRequest request)
    {
        Dictionary<string, string> selected = new Dictionary<string, string>(StringComparer.Ordinal);
        if (request.FetchAllBodyValues)
        {
            if (email.BodyStructure == null)
            {
                throw ProtocolFailure("fetchAllBodyValues requires a projected bodyStructure.",
                    "bodyStructure", default(JsonElement));
            }

            CollectTextPartTypes(email.BodyStructure, selected);
            return selected;
        }

        if (request.FetchTextBodyValues)
        {
            if (email.TextBody == null)
            {
                throw ProtocolFailure("fetchTextBodyValues requires a projected textBody.",
                    "textBody", default(JsonElement));
            }

            CollectTextPartListTypes(email.TextBody, selected);
        }

        if (request.FetchHtmlBodyValues)
        {
            if (email.HtmlBody == null)
            {
                throw ProtocolFailure("fetchHTMLBodyValues requires a projected htmlBody.",
                    "htmlBody", default(JsonElement));
            }

            CollectTextPartListTypes(email.HtmlBody, selected);
        }

        return selected;
    }

    internal void CollectTextPartTypes(EmailBodyPart? part, Dictionary<string, string> selected)
    {
        if (part == null)
        {
            return;
        }

        if (part.Type != null && part.Type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
            part.PartId != null)
        {
            selected[part.PartId] = part.Type;
        }

        if (part.SubParts != null)
        {
            foreach (EmailBodyPart child in part.SubParts)
            {
                CollectTextPartTypes(child, selected);
            }
        }
    }

    internal void CollectTextPartListTypes(List<EmailBodyPart>? parts, Dictionary<string, string> selected)
    {
        if (parts == null)
        {
            return;
        }

        foreach (EmailBodyPart part in parts)
        {
            CollectTextPartTypes(part, selected);
        }
    }

    internal void WriteRequiredBooleanMap(Dictionary<string, bool>? value, string name,
        JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw ProtocolFailure("A required Email map is unavailable.", path, default(JsonElement));
        }

        writer.WriteMemberName(name);
        Codec.Emails.EncodeBooleanMap(value, writer, path);
    }

    internal void WriteRequiredHeaders(List<EmailHeader>? value, string name, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw ProtocolFailure("A required Email header list is unavailable.", path, default(JsonElement));
        }

        writer.WriteMemberName(name);
        Codec.Emails.EncodeHeaderList(value, writer, path);
    }

    internal void WriteNullableStringArray(string name, List<string>? value, JmapJsonWriter writer, string path)
    {
        writer.WriteMemberName(name);
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            Codec.Values.EncodeStringArray(value, writer, path);
        }
    }

    internal void WriteNullableAddresses(string name, List<EmailAddress>? value, JmapJsonWriter writer, string path)
    {
        writer.WriteMemberName(name);
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            Codec.Emails.EncodeAddressList(value, writer, path);
        }
    }

    internal void WriteRequiredBodyStructure(EmailBodyPart? value, List<string>? bodyProperties,
        JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw ProtocolFailure("A required Email body structure is unavailable.", path, default(JsonElement));
        }

        writer.WriteMemberName(JmapProperties.BodyStructure);
        WriteProjectedBodyPart(value, bodyProperties, writer, path);
    }

    internal void WriteRequiredBodyPartList(List<EmailBodyPart>? value, string name,
        List<string>? bodyProperties, JmapJsonWriter writer, string path)
    {
        if (value == null)
        {
            throw ProtocolFailure("A required Email body-part list is unavailable.", path, default(JsonElement));
        }

        writer.WriteMemberName(name);
        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailBodyPart part in value)
        {
            WriteProjectedBodyPart(part, bodyProperties, writer, path + "/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
    }

    internal List<string> EffectiveEmailProperties(List<string>? properties)
    {
        List<string> source = properties == null ? DefaultEmailProperties() : properties;
        List<string> result = new List<string>();
        foreach (string property in source)
        {
            AddUniqueValue(property, result);
        }

        return result;
    }

    internal List<string> EffectiveBodyProperties(List<string>? properties)
    {
        List<string> source = properties == null ? DefaultBodyProperties() : properties;
        List<string> result = new List<string>();
        foreach (string property in source)
        {
            AddUniqueValue(property, result);
        }

        return result;
    }

    internal List<string> DefaultEmailProperties()
    {
        return new List<string>
        {
            JmapProperties.Id, JmapProperties.BlobId, JmapProperties.ThreadId, JmapProperties.MailboxIds,
            JmapProperties.Keywords, JmapProperties.Size, JmapProperties.ReceivedAt, JmapProperties.MessageId,
            JmapProperties.InReplyTo, JmapProperties.References, JmapProperties.Sender, JmapProperties.From,
            JmapProperties.To, JmapProperties.Cc, JmapProperties.Bcc, JmapProperties.ReplyTo,
            JmapProperties.Subject, JmapProperties.SentAt, JmapProperties.HasAttachment, JmapProperties.Preview,
            JmapProperties.BodyValues, JmapProperties.TextBody, JmapProperties.HtmlBody,
            JmapProperties.Attachments
        };
    }

    internal List<string> DefaultBodyProperties()
    {
        return new List<string>
        {
            JmapProperties.PartId, JmapProperties.BlobId, JmapProperties.Size, JmapProperties.Name,
            JmapProperties.Type, JmapProperties.Charset, JmapProperties.Disposition, JmapProperties.Cid,
            JmapProperties.Language, JmapProperties.Location
        };
    }

    internal HashSet<string> EmailPropertySet()
    {
        return new HashSet<string>(Codec.Emails.EmailNames(), StringComparer.Ordinal);
    }

    internal HashSet<string> BodyPropertySet()
    {
        return new HashSet<string>(Codec.Emails.BodyPartNames(), StringComparer.Ordinal);
    }

    internal bool ContainsDynamicHeaderProperty(List<string> properties)
    {
        foreach (string property in properties)
        {
            if (Headers.ParsePropertyForValidation(property) != null)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasBodyPartProjection(List<string> properties)
    {
        return properties.Contains(JmapProperties.BodyStructure) || properties.Contains(JmapProperties.TextBody) ||
            properties.Contains(JmapProperties.HtmlBody) || properties.Contains(JmapProperties.Attachments);
    }

    internal void AddUniqueValue(string value, List<string> values)
    {
        if (!values.Contains(value))
        {
            values.Add(value);
        }
    }

    internal JmapMethodError? ValidateMailAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound,
                "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null)
        {
            throw ProtocolFailure("The Session contains a null Email account.", "session/accounts/" + id,
                default(JsonElement));
        }

        if (account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid account capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Mail))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod,
                JmapMethodErrorKind.AccountNotSupportedByMethod, "The account does not support JMAP Mail.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Mail];
        if (capability == null || capability.Mail == null)
        {
            throw ProtocolFailure("The account contains invalid Mail capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal JmapMethodError? ValidateSourceMailAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.FromAccountNotFound, JmapMethodErrorKind.FromAccountNotFound,
                "The source account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null)
        {
            throw ProtocolFailure("The Session contains a null source Email account.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid source account capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Mail))
        {
            return MethodError(JmapErrorCodes.FromAccountNotSupportedByMethod,
                JmapMethodErrorKind.FromAccountNotSupportedByMethod,
                "The source account does not support JMAP Mail.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Mail];
        if (capability == null || capability.Mail == null)
        {
            throw ProtocolFailure("The source account contains invalid Mail capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal JmapMailCapabilities GetMailCapabilities(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        JmapMailCapabilities? capabilities = session.Accounts[id].AccountCapabilities[JmapCapabilities.Mail].Mail;
        if (capabilities == null)
        {
            throw ProtocolFailure("The account contains invalid Mail capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return capabilities;
    }

    internal List<string> GetSupportedCollations(JmapSession session)
    {
        if (session.Capabilities.ContainsKey(JmapCapabilities.Core))
        {
            JmapCapability capability = session.Capabilities[JmapCapabilities.Core];
            if (capability != null && capability.Core != null && capability.Core.CollationAlgorithms != null)
            {
                return capability.Core.CollationAlgorithms;
            }
        }

        return new List<string>();
    }

    internal void ValidateDispatchDependencies(JmapSession session, IEmailService emailService,
        JmapRequestContext context, JmapServerOptions options)
    {
        if (session == null || emailService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("Email dispatch dependencies are required.", "emailService",
                default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("An Email invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong Email method name.", "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Email arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable Email call identifier is required.", "methodCalls/0/2");
        }
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        JsonElement bodyProperties = Codec.Values.GetMember(arguments, JmapProperties.BodyProperties);
        JsonElement text = Codec.Values.GetMember(arguments, JmapProperties.FetchTextBodyValues);
        JsonElement html = Codec.Values.GetMember(arguments, JmapProperties.FetchHtmlBodyValues);
        JsonElement all = Codec.Values.GetMember(arguments, JmapProperties.FetchAllBodyValues);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxBodyValueBytes);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringArray(ids) &&
            IsOptionalStringArray(properties) && IsOptionalStringArray(bodyProperties) && IsOptionalBoolean(text) &&
            IsOptionalBoolean(html) && IsOptionalBoolean(all) && IsOptionalUnsignedInteger(maximum);
    }

    internal bool IsChangesRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            (maximum.ValueKind == JsonValueKind.Undefined || maximum.ValueKind == JsonValueKind.Null ||
                IsOptionalUnsignedInteger(maximum));
    }

    internal bool IsQueryRequestShape(JsonElement arguments, int depth, int maximumDepth)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement sort = Codec.Values.GetMember(arguments, JmapProperties.Sort);
        JsonElement position = Codec.Values.GetMember(arguments, JmapProperties.Position);
        JsonElement anchor = Codec.Values.GetMember(arguments, JmapProperties.Anchor);
        JsonElement anchorOffset = Codec.Values.GetMember(arguments, JmapProperties.AnchorOffset);
        JsonElement limit = Codec.Values.GetMember(arguments, JmapProperties.Limit);
        JsonElement calculate = Codec.Values.GetMember(arguments, JmapProperties.CalculateTotal);
        JsonElement collapse = Codec.Values.GetMember(arguments, JmapProperties.CollapseThreads);
        return accountId.ValueKind == JsonValueKind.String &&
            IsOptionalEmailFilterShape(filter, depth, maximumDepth) && IsOptionalEmailComparatorArray(sort) &&
            IsOptionalInteger(position) && IsOptionalString(anchor) && IsOptionalInteger(anchorOffset) &&
            IsOptionalNullableUnsignedInteger(limit) && IsOptionalBoolean(calculate) &&
            IsOptionalBoolean(collapse);
    }

    internal bool IsQueryChangesRequestShape(JsonElement arguments, int depth, int maximumDepth)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement sort = Codec.Values.GetMember(arguments, JmapProperties.Sort);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceQueryState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        JsonElement upToId = Codec.Values.GetMember(arguments, JmapProperties.UpToId);
        JsonElement calculate = Codec.Values.GetMember(arguments, JmapProperties.CalculateTotal);
        JsonElement collapse = Codec.Values.GetMember(arguments, JmapProperties.CollapseThreads);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            IsOptionalEmailFilterShape(filter, depth, maximumDepth) && IsOptionalEmailComparatorArray(sort) &&
            IsOptionalNullableUnsignedInteger(maximum) && IsOptionalString(upToId) &&
            IsOptionalBoolean(calculate) && IsOptionalBoolean(collapse);
    }

    internal bool IsSetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ifInState = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement update = Codec.Values.GetMember(arguments, JmapProperties.Update);
        JsonElement destroy = Codec.Values.GetMember(arguments, JmapProperties.Destroy);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalString(ifInState) &&
            IsOptionalEmailCreateMap(create) && IsOptionalPatchMap(update) && IsOptionalStringArray(destroy);
    }

    internal bool IsCopyRequestShape(JsonElement arguments)
    {
        JsonElement fromAccountId = Codec.Values.GetMember(arguments, JmapProperties.FromAccountId);
        JsonElement ifFromInState = Codec.Values.GetMember(arguments, JmapProperties.IfFromInState);
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ifInState = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement destroyOriginal = Codec.Values.GetMember(arguments, JmapProperties.OnSuccessDestroyOriginal);
        JsonElement destroyState = Codec.Values.GetMember(arguments, JmapProperties.DestroyFromIfInState);
        return fromAccountId.ValueKind == JsonValueKind.String && accountId.ValueKind == JsonValueKind.String &&
            IsOptionalString(ifFromInState) && IsOptionalString(ifInState) && IsEmailCopyMap(create) &&
            IsOptionalBoolean(destroyOriginal) && IsOptionalString(destroyState);
    }

    internal bool IsEmailCopyMap(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!IsEmailCreateShape(property.Value, 1, 64))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsImportRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ifInState = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement emails = Codec.Values.GetMember(arguments, JmapProperties.Emails);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalString(ifInState) &&
            emails.ValueKind == JsonValueKind.Object;
    }

    internal bool IsParseRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement blobIds = Codec.Values.GetMember(arguments, JmapProperties.BlobIds);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        JsonElement bodyProperties = Codec.Values.GetMember(arguments, JmapProperties.BodyProperties);
        JsonElement text = Codec.Values.GetMember(arguments, JmapProperties.FetchTextBodyValues);
        JsonElement html = Codec.Values.GetMember(arguments, JmapProperties.FetchHtmlBodyValues);
        JsonElement all = Codec.Values.GetMember(arguments, JmapProperties.FetchAllBodyValues);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxBodyValueBytes);
        return accountId.ValueKind == JsonValueKind.String && blobIds.ValueKind == JsonValueKind.Array &&
            IsOptionalStringArray(properties) && IsOptionalStringArray(bodyProperties) &&
            IsOptionalBoolean(text) && IsOptionalBoolean(html) && IsOptionalBoolean(all) &&
            IsOptionalUnsignedInteger(maximum) && IsStringArray(blobIds);
    }

    internal bool IsStringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalEmailCreateMap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!IsEmailCreateShape(property.Value, 1, 64))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsEmailCreateShape(JsonElement value, int depth, int maximumDepth)
    {
        if (value.ValueKind != JsonValueKind.Object || depth > maximumDepth)
        {
            return false;
        }

        return IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Id)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.BlobId)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.ThreadId)) &&
            IsOptionalBooleanMap(Codec.Values.GetMember(value, JmapProperties.MailboxIds)) &&
            IsOptionalBooleanMap(Codec.Values.GetMember(value, JmapProperties.Keywords)) &&
            IsOptionalUnsignedInteger(Codec.Values.GetMember(value, JmapProperties.Size)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.ReceivedAt)) &&
            IsOptionalHeaderArray(Codec.Values.GetMember(value, JmapProperties.Headers)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.MessageId)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.InReplyTo)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.References)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.Sender)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.From)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.To)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.Cc)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.Bcc)) &&
            IsOptionalAddressArray(Codec.Values.GetMember(value, JmapProperties.ReplyTo)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Subject)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.SentAt)) &&
            IsOptionalBodyPartObject(Codec.Values.GetMember(value, JmapProperties.BodyStructure), depth,
                maximumDepth) &&
            IsOptionalBodyValueMap(Codec.Values.GetMember(value, JmapProperties.BodyValues)) &&
            IsOptionalBodyPartArray(Codec.Values.GetMember(value, JmapProperties.TextBody), depth, maximumDepth) &&
            IsOptionalBodyPartArray(Codec.Values.GetMember(value, JmapProperties.HtmlBody), depth, maximumDepth) &&
            IsOptionalBodyPartArray(Codec.Values.GetMember(value, JmapProperties.Attachments), depth,
                maximumDepth) &&
            IsOptionalBoolean(Codec.Values.GetMember(value, JmapProperties.HasAttachment)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Preview));
    }

    internal bool IsOptionalBooleanMap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.True && property.Value.ValueKind != JsonValueKind.False)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalHeaderArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(item, JmapProperties.Name).ValueKind != JsonValueKind.String ||
                Codec.Values.GetMember(item, JmapProperties.Value).ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalAddressArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !IsRequiredNullableString(Codec.Values.GetMember(item, JmapProperties.Name)) ||
                !IsRequiredNullableString(Codec.Values.GetMember(item, JmapProperties.Email)))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsRequiredNullableString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String || value.ValueKind == JsonValueKind.Null;
    }

    internal bool IsOptionalBodyPartObject(JsonElement value, int depth, int maximumDepth)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null ||
            IsBodyPartShape(value, depth + 1, maximumDepth);
    }

    internal bool IsOptionalBodyPartArray(JsonElement value, int depth, int maximumDepth)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (!IsBodyPartShape(item, depth + 1, maximumDepth))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsBodyPartShape(JsonElement value, int depth, int maximumDepth)
    {
        if (value.ValueKind != JsonValueKind.Object || depth > maximumDepth)
        {
            return false;
        }

        return IsOptionalString(Codec.Values.GetMember(value, JmapProperties.PartId)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.BlobId)) &&
            IsOptionalUnsignedInteger(Codec.Values.GetMember(value, JmapProperties.Size)) &&
            IsOptionalHeaderArray(Codec.Values.GetMember(value, JmapProperties.Headers)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Name)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Type)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Charset)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Disposition)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Cid)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.Language)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Location)) &&
            IsOptionalBodyPartArray(Codec.Values.GetMember(value, JmapProperties.SubParts), depth, maximumDepth);
    }

    internal bool IsOptionalBodyValueMap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            JsonElement bodyValue = property.Value;
            if (bodyValue.ValueKind != JsonValueKind.Object ||
                Codec.Values.GetMember(bodyValue, JmapProperties.Value).ValueKind != JsonValueKind.String ||
                !IsOptionalBoolean(Codec.Values.GetMember(bodyValue, JmapProperties.IsEncodingProblem)) ||
                !IsOptionalBoolean(Codec.Values.GetMember(bodyValue, JmapProperties.IsTruncated)))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalPatchMap(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalEmailFilterShape(JsonElement value, int depth, int maximumDepth)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object || depth > maximumDepth)
        {
            return false;
        }

        JsonElement operatorValue = Codec.Values.GetMember(value, JmapProperties.Operator);
        JsonElement conditions = Codec.Values.GetMember(value, JmapProperties.Conditions);
        if (operatorValue.ValueKind != JsonValueKind.Undefined)
        {
            if (operatorValue.ValueKind != JsonValueKind.String ||
                !IsFilterOperatorToken(operatorValue.GetString()) || conditions.ValueKind != JsonValueKind.Array ||
                HasEmailConditionMember(value))
            {
                return false;
            }

            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                if (condition.ValueKind != JsonValueKind.Object ||
                    !IsOptionalEmailFilterShape(condition, depth + 1, maximumDepth))
                {
                    return false;
                }
            }

            return true;
        }

        if (conditions.ValueKind != JsonValueKind.Undefined)
        {
            return false;
        }

        return IsOptionalString(Codec.Values.GetMember(value, JmapProperties.InMailbox)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.InMailboxOtherThan)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Before)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.After)) &&
            IsOptionalUnsignedInteger(Codec.Values.GetMember(value, JmapProperties.MinSize)) &&
            IsOptionalUnsignedInteger(Codec.Values.GetMember(value, JmapProperties.MaxSize)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.AllInThreadHaveKeyword)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.SomeInThreadHaveKeyword)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.NoneInThreadHaveKeyword)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.HasKeyword)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.NotKeyword)) &&
            IsOptionalBoolean(Codec.Values.GetMember(value, JmapProperties.HasAttachment)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Text)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.From)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.To)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Cc)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Bcc)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Subject)) &&
            IsOptionalString(Codec.Values.GetMember(value, JmapProperties.Body)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.Header));
    }

    internal bool HasEmailConditionMember(JsonElement value)
    {
        List<string> names = new List<string>
        {
            JmapProperties.InMailbox, JmapProperties.InMailboxOtherThan, JmapProperties.Before,
            JmapProperties.After, JmapProperties.MinSize, JmapProperties.MaxSize,
            JmapProperties.AllInThreadHaveKeyword, JmapProperties.SomeInThreadHaveKeyword,
            JmapProperties.NoneInThreadHaveKeyword, JmapProperties.HasKeyword, JmapProperties.NotKeyword,
            JmapProperties.HasAttachment, JmapProperties.Text, JmapProperties.From, JmapProperties.To,
            JmapProperties.Cc, JmapProperties.Bcc, JmapProperties.Subject, JmapProperties.Body,
            JmapProperties.Header
        };
        foreach (string name in names)
        {
            if (Codec.Values.GetMember(value, name).ValueKind != JsonValueKind.Undefined)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsOptionalEmailComparatorArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement comparator in value.EnumerateArray())
        {
            if (!IsEmailComparatorShape(comparator))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsEmailComparatorShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement property = Codec.Values.GetMember(value, JmapProperties.Property);
        JsonElement keyword = Codec.Values.GetMember(value, "keyword");
        JsonElement ascending = Codec.Values.GetMember(value, JmapProperties.IsAscending);
        JsonElement collation = Codec.Values.GetMember(value, JmapProperties.Collation);
        return property.ValueKind == JsonValueKind.String && IsOptionalString(keyword) &&
            IsOptionalBoolean(ascending) && IsOptionalString(collation);
    }

    internal bool IsFilterOperatorToken(string? value)
    {
        return value == "AND" || value == "OR" || value == "NOT";
    }

    internal bool IsOptionalString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.String;
    }

    internal bool IsOptionalInteger(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || IsIntegerShape(value);
    }

    internal bool IsOptionalNullableUnsignedInteger(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Null || IsOptionalUnsignedInteger(value);
    }

    internal bool IsIntegerShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        string text = value.GetRawText();
        if (text.Length == 0 || text.Length > 17)
        {
            return false;
        }

        int index = text[0] == '-' ? 1 : 0;
        if (index == text.Length)
        {
            return false;
        }

        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsOptionalStringArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsOptionalBoolean(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.True ||
            value.ValueKind == JsonValueKind.False;
    }

    internal bool IsOptionalUnsignedInteger(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        string text = value.GetRawText();
        if (text.Length == 0 || text.Length > 16)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JmapInvocation ErrorInvocation(string? type, JmapMethodErrorKind kind, string? description,
        string callId)
    {
        JmapMethodError error = Errors.CreateMethodError(type, description, false);
        error.Kind = kind;
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(ResponseMapper.ErrorResponseName, callId, stream.ToArray());
    }

    internal JmapInvocation InvocationFromBytes(string name, string callId, byte[] bytes)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Reader.ReadUtf8Memory(bytes);
        return invocation;
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal bool IsValidCallId(string? callId)
    {
        return callId != null && callId.Length > 0 && Values.IsValidUnicode(callId);
    }

    internal void CopyUnknown(Dictionary<string, JsonElement> source, Dictionary<string, JsonElement> target)
    {
        foreach (KeyValuePair<string, JsonElement> item in source)
        {
            target.Add(item.Key, item.Value.Clone());
        }
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
