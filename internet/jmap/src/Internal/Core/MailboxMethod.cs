using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class MailboxMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal MailboxValidator Validator = new MailboxValidator();
    internal PatchValidator Patches = new PatchValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal MailboxJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);
    internal MethodContext Access = new MethodContext();

    internal MailboxMethod()
    {
        Codec = new MailboxJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, mailboxService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.MailboxGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox/get arguments are invalid.", callId);
        }

        MailboxGetRequest request = Codec.DecodeMailboxGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, mailboxService, context, options);
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

        MailboxGetRequest backendRequest = CompleteGetRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        MailboxGetResponse stored = mailboxService.GetStoredMailboxesAsync(backendRequest, context, cancellationToken).GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null && stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "Returning all Mailboxes would exceed the get object limit.", callId);
        }

        JmapMailCapabilities capabilities = GetMailCapabilities(session, request.AccountId);
        MailboxGetResponse response = NormalizeServerGetResponse(stored, request, capabilities.MaxSizeMailboxName);
        return GetSuccessInvocation(response, request.Properties, callId);
    }

    internal Task<JmapInvocation> ProcessChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessChangesInvocation(invocation, session, mailboxService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.MailboxChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsChangesRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox/changes arguments are invalid.", callId);
        }

        MailboxChangesRequest request = Codec.DecodeMailboxChangesRequest(invocation.Arguments);
        JmapMethodError? requestError = ValidateChangesRequest(request);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        ValidateDispatchDependencies(session, mailboxService, context, options);
        JmapMethodError? accountError = ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        MailboxChangesResponse response = mailboxService.GetStoredMailboxChangesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateChangesResponse(response, request);
        return ChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryInvocation(invocation, session, mailboxService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessQueryInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.MailboxQuery);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox/query arguments are invalid.", callId);
        }

        MailboxQueryRequest request = Codec.DecodeMailboxQueryRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, mailboxService, context, options);
        JmapMethodError? requestError = ValidateQueryRequest(
            request,
            options.MaxJsonDepth,
            GetSupportedCollations(session),
            true);
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
        MailboxQueryResponse response = mailboxService.QueryStoredMailboxesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateQueryResponse(response, request);
        return QuerySuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryChangesInvocation(invocation, session, mailboxService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessQueryChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.MailboxQueryChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryChangesRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox/queryChanges arguments are invalid.", callId);
        }

        MailboxQueryChangesRequest request = Codec.DecodeMailboxQueryChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, mailboxService, context, options);
        JmapMethodError? requestError = ValidateQueryChangesRequest(
            request,
            options.MaxJsonDepth,
            GetSupportedCollations(session),
            true);
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
        MailboxQueryChangesResponse response = mailboxService.GetStoredMailboxQueryChangesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateQueryChangesResponse(response, request);
        if (ExceedsMaximumQueryChanges(response, request))
        {
            return ErrorInvocation(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges, "The Mailbox query changes exceed maxChanges.", callId);
        }

        return QueryChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, session, mailboxService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.MailboxSet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox/set arguments are invalid.", callId);
        }

        MailboxSetRequest request = Codec.DecodeMailboxSetRequest(invocation.Arguments);
        ApplyCreateDefaults(invocation.Arguments, request);
        ValidateDispatchDependencies(session, mailboxService, context, options);
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
            return ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly, "The account is read-only.", callId);
        }

        JmapMailCapabilities capabilities = GetMailCapabilities(session, request.AccountId);
        MailboxGetRequest snapshotRequest = new MailboxGetRequest();
        snapshotRequest.AccountId = request.AccountId;
        cancellationToken.ThrowIfCancellationRequested();
        MailboxGetResponse stored = mailboxService.GetStoredMailboxesAsync(snapshotRequest, context, cancellationToken).GetAwaiter().GetResult();
        MailboxGetResponse snapshot = NormalizeServerGetResponse(stored, snapshotRequest, capabilities.MaxSizeMailboxName);
        Dictionary<string, Mailbox> mailboxes = IndexCompleteMailboxes(snapshot.List, capabilities.MaxSizeMailboxName);
        if (Validator.ValidateHierarchy(mailboxes, capabilities).Count != 0)
        {
            throw ProtocolFailure("The mailbox backend returned an invalid hierarchy.", "mailboxService", default(JsonElement));
        }

        if (request.IfInState != null && request.IfInState != snapshot.State)
        {
            return ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch, "The ifInState value does not match the current Mailbox state.", callId);
        }

        MailboxSetResponse local = new MailboxSetResponse();
        local.AccountId = request.AccountId;
        local.OldState = snapshot.State;
        local.NewState = snapshot.State;
        MailboxSetRequest backend = PrepareSetRequest(request, local, mailboxes, capabilities);
        MailboxSetResponse response = local;
        if (CountSetActions(backend) != 0L)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = mailboxService.SetStoredMailboxesAsync(backend, context, cancellationToken).GetAwaiter().GetResult();
            ValidateSetResponse(response, backend, capabilities.MaxSizeMailboxName);
            MergeSetFailures(response, local);
        }

        ValidateSetResponse(response, request, capabilities.MaxSizeMailboxName);
        return SetSuccessInvocation(response, callId);
    }

    internal JmapInvocation CreateGetInvocation(MailboxGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Mailbox/get arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateChangesInvocation(MailboxChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Mailbox/changes arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryInvocation(MailboxQueryRequest request, string callId)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64, null, false);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Mailbox/query arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxQueryRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxQuery, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryChangesInvocation(MailboxQueryChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64, null, false);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Mailbox/queryChanges arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxQueryChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(MailboxSetRequest request, string callId)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid Mailbox/set arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxSetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxSet, callId, stream.ToArray());
    }

    internal MailboxGetResponse MapGetResponse(JmapResponse response, string callId, MailboxGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original Mailbox/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.MailboxGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.MailboxGet);
        ValidateGetProjection(arguments, request.Properties);
        MailboxGetResponse decoded = Codec.DecodeMailboxGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal MailboxChangesResponse MapChangesResponse(JmapResponse response, string callId, MailboxChangesRequest request)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null)
        {
            throw ValidationFailure("Valid original Mailbox/changes arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.MailboxChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.MailboxChanges);
        MailboxChangesResponse decoded = Codec.DecodeMailboxChangesResponse(arguments);
        ValidateChangesResponse(decoded, request);
        return decoded;
    }

    internal MailboxQueryResponse MapQueryResponse(JmapResponse response, string callId, MailboxQueryRequest request)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64, null, false);
        if (error != null)
        {
            throw ValidationFailure("Valid original Mailbox/query arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.MailboxQuery);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.MailboxQuery);
        MailboxQueryResponse decoded = Codec.DecodeMailboxQueryResponse(arguments);
        ValidateQueryResponse(decoded, request);
        return decoded;
    }

    internal MailboxQueryChangesResponse MapQueryChangesResponse(
        JmapResponse response,
        string callId,
        MailboxQueryChangesRequest request)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64, null, false);
        if (error != null)
        {
            throw ValidationFailure("Valid original Mailbox/queryChanges arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.MailboxQueryChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.MailboxQueryChanges);
        MailboxQueryChangesResponse decoded = Codec.DecodeMailboxQueryChangesResponse(arguments);
        ValidateQueryChangesResponse(decoded, request);
        if (ExceedsMaximumQueryChanges(decoded, request))
        {
            throw ProtocolFailure("Mailbox/queryChanges returned more changes than maxChanges permits.", "methodResponses/1", arguments);
        }

        return decoded;
    }

    internal MailboxSetResponse MapSetResponse(JmapResponse response, string callId, MailboxSetRequest request)
    {
        if (request == null)
        {
            throw ValidationFailure("Original Mailbox/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.MailboxSet);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.MailboxSet, request.AccountId, request.IfInState);
        MailboxSetResponse decoded = Codec.DecodeMailboxSetResponse(arguments);
        ValidateSetResponse(decoded, request, long.MaxValue);
        return decoded;
    }

    internal JmapMethodError? ValidateGetRequest(MailboxGetRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid get object limit is required.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The ids argument contains an invalid identifier.");
                }
            }
        }

        if (request.Properties != null)
        {
            HashSet<string> supported = MailboxPropertySet();
            foreach (string property in request.Properties)
            {
                if (property == null || !supported.Contains(property))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The properties argument contains an unsupported property.");
                }
            }
        }

        return null;
    }

    internal JmapMethodError? ValidateChangesRequest(MailboxChangesRequest? request)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.SinceState == null || !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid sinceState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "maxChanges must be null or a positive UnsignedInt.");
        }

        return null;
    }

    internal JmapMethodError? ValidateQueryRequest(
        MailboxQueryRequest? request,
        int maximumFilterDepth,
        List<string>? supportedCollations,
        bool enforceCollations)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid filter depth limit is required.");
        }

        if (request.HasPosition && !Values.IsInt(request.Position))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "position must be an Int.");
        }

        if (request.Anchor != null && !Values.IsId(request.Anchor))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "anchor must be an Id.");
        }

        if (request.HasAnchorOffset && !Values.IsInt(request.AnchorOffset))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "anchorOffset must be an Int.");
        }

        if (request.IsLimitNull && !request.HasLimit)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The limit presence flags are inconsistent.");
        }

        if (request.HasLimit && !request.IsLimitNull && !Values.IsUnsignedInt(request.Limit))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "limit must be null or an UnsignedInt.");
        }

        JmapMethodError? filterError = ValidateMailboxFilter(request.Filter, 1, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return ValidateMailboxSort(request.Sort, supportedCollations, enforceCollations);
    }

    internal JmapMethodError? ValidateQueryChangesRequest(
        MailboxQueryChangesRequest? request,
        int maximumFilterDepth,
        List<string>? supportedCollations,
        bool enforceCollations)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid filter depth limit is required.");
        }

        if (request.SinceQueryState == null || !Values.IsValidUnicode(request.SinceQueryState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid sinceQueryState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The maxChanges presence flags are inconsistent.");
        }

        if (request.HasMaxChanges && !request.IsMaxChangesNull && !Values.IsUnsignedInt(request.MaxChanges))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "maxChanges must be null or an UnsignedInt.");
        }

        if (request.UpToId != null && !Values.IsId(request.UpToId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "upToId must be an Id.");
        }

        JmapMethodError? filterError = ValidateMailboxFilter(request.Filter, 1, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return ValidateMailboxSort(request.Sort, supportedCollations, enforceCollations);
    }

    internal JmapMethodError? ValidateMailboxFilter(MailboxFilter? filter, int depth, int maximumDepth)
    {
        if (filter == null)
        {
            return null;
        }

        if (depth > maximumDepth)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter, "The Mailbox filter exceeds the supported recursion depth.");
        }

        if (filter.HasOperator)
        {
            if (!IsFilterOperator(filter.Operator) || filter.Conditions == null || filter.HasParentId || filter.Name != null ||
                filter.HasRole || filter.HasHasAnyRole || filter.HasIsSubscribed || filter.UnknownProperties == null)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The Mailbox filter operator is malformed.");
            }

            if (filter.UnknownProperties.Count != 0)
            {
                return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter, "The Mailbox filter contains an unsupported property.");
            }

            foreach (MailboxFilter condition in filter.Conditions)
            {
                if (condition == null)
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A Mailbox filter operator contains an invalid condition.");
                }

                JmapMethodError? childError = ValidateMailboxFilter(condition, depth + 1, maximumDepth);
                if (childError != null)
                {
                    return childError;
                }
            }

            return null;
        }

        if (filter.Conditions != null || filter.UnknownProperties == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A Mailbox filter condition cannot contain nested conditions.");
        }

        if (filter.UnknownProperties.Count != 0)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter, "The Mailbox filter contains an unsupported property.");
        }

        if (filter.HasParentId && filter.ParentId != null && !Values.IsId(filter.ParentId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The parentId filter must be null or an Id.");
        }

        if (filter.Name != null && !Values.IsValidUnicode(filter.Name))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The name filter must be a valid string.");
        }

        if (filter.HasRole && filter.Role != null && !Values.IsMailboxRole(filter.Role))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The role filter must be null or a valid Mailbox role.");
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(MailboxSetRequest? request, int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A valid accountId is required.");
        }

        if (request.IfInState != null && !Values.IsValidUnicode(request.IfInState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The ifInState value is not valid Unicode.");
        }

        if (CountSetActions(request) > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge, "The set request contains too many object actions.");
        }

        if (request.Create != null)
        {
            foreach (string creationId in request.Create.Keys)
            {
                if (!Values.IsCreationId(creationId))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The create map contains an invalid creation id.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The update map contains an invalid Mailbox id.");
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
                    return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "The destroy list contains an invalid or duplicate Mailbox id.");
                }
            }
        }

        return null;
    }

    internal long CountSetActions(MailboxSetRequest request)
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

    internal void ApplyCreateDefaults(JsonElement arguments, MailboxSetRequest request)
    {
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        if (create.ValueKind != JsonValueKind.Object || request.Create == null)
        {
            return;
        }

        foreach (JsonProperty property in create.EnumerateObject())
        {
            JsonElement subscribed = Codec.Values.GetMember(property.Value, JmapProperties.IsSubscribed);
            if (subscribed.ValueKind == JsonValueKind.Undefined && request.Create.ContainsKey(property.Name) && request.Create[property.Name] != null)
            {
                request.Create[property.Name].IsSubscribed = true;
            }
        }
    }

    internal MailboxSetRequest PrepareSetRequest(
        MailboxSetRequest request,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        JmapMailCapabilities capabilities)
    {
        MailboxSetRequest backend = new MailboxSetRequest();
        backend.AccountId = request.AccountId;
        backend.IfInState = request.IfInState;
        backend.OnDestroyRemoveEmails = request.OnDestroyRemoveEmails;
        backend.HasOnDestroyRemoveEmails = request.HasOnDestroyRemoveEmails;
        CopyUnknown(request.UnknownProperties, backend.UnknownProperties);
        Dictionary<string, Mailbox> created = new Dictionary<string, Mailbox>(StringComparer.Ordinal);
        PrepareSetCreates(request, backend, local, mailboxes, created, capabilities);
        HashSet<string> destroyed = SetDestroyIds(request.Destroy);
        PrepareSetUpdates(request, backend, local, mailboxes, created, destroyed, capabilities);
        PrepareSetDestroys(request, backend, local, mailboxes, created);
        return backend;
    }

    internal void PrepareSetCreates(
        MailboxSetRequest request,
        MailboxSetRequest backend,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        if (request.Create == null)
        {
            return;
        }

        HashSet<string> pending = new HashSet<string>(request.Create.Keys, StringComparer.Ordinal);
        bool progressed = true;
        while (pending.Count != 0 && progressed)
        {
            progressed = false;
            foreach (KeyValuePair<string, Mailbox> item in request.Create)
            {
                if (pending.Contains(item.Key) && !HasPendingCreateParent(item.Value, pending))
                {
                    PrepareSetCreate(item.Key, item.Value, backend, local, mailboxes, created, capabilities);
                    pending.Remove(item.Key);
                    progressed = true;
                }
            }
        }

        foreach (KeyValuePair<string, Mailbox> item in request.Create)
        {
            if (pending.Contains(item.Key))
            {
                AddNotCreated(local, item.Key, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The Mailbox create contains an unresolved or cyclic parentId.", new List<string> { JmapProperties.ParentId }));
            }
        }
    }

    internal bool HasPendingCreateParent(Mailbox? mailbox, HashSet<string> pending)
    {
        if (mailbox == null || mailbox.ParentId == null || !mailbox.ParentId.StartsWith("#", StringComparison.Ordinal))
        {
            return false;
        }

        string creationId = mailbox.ParentId.Substring(1);
        return pending.Contains(creationId);
    }

    internal void PrepareSetCreate(
        string creationId,
        Mailbox? mailbox,
        MailboxSetRequest backend,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        if (IsCreateForbidden(mailbox, mailboxes, created, capabilities))
        {
            AddNotCreated(local, creationId, SetError(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden, "The current user may not create the Mailbox at the requested location.", null));
            return;
        }

        List<string> invalid = Validator.ValidateCreateAgainstHierarchy(mailbox, mailboxes, created, capabilities);
        if (invalid.Count != 0 || mailbox == null)
        {
            AddNotCreated(local, creationId, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The Mailbox create contains invalid properties.", invalid));
            return;
        }

        if (backend.Create == null)
        {
            backend.Create = new Dictionary<string, Mailbox>();
        }

        backend.Create.Add(creationId, mailbox);
        created.Add(creationId, mailbox);
    }

    internal bool IsCreateForbidden(
        Mailbox? mailbox,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        if (mailbox == null)
        {
            return false;
        }

        if (mailbox.ParentId == null)
        {
            return !capabilities.MayCreateTopLevelMailbox;
        }

        if (mailbox.ParentId.StartsWith("#", StringComparison.Ordinal))
        {
            return false;
        }

        Mailbox? parent = Validator.FindParent(mailbox.ParentId, mailboxes, created);
        return parent != null &&
            !Access.HasMailboxRight(parent, MailboxRightKind.CreateChild);
    }

    internal void PrepareSetUpdates(
        MailboxSetRequest request,
        MailboxSetRequest backend,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        HashSet<string> destroyed,
        JmapMailCapabilities capabilities)
    {
        if (request.Update == null)
        {
            return;
        }

        foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
        {
            if (destroyed.Contains(item.Key))
            {
                AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy, "The update is skipped because the Mailbox is destroyed.", null));
            }
            else if (!mailboxes.ContainsKey(item.Key))
            {
                AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound, "The Mailbox was not found.", null));
            }
            else if (!IsPatchStructurallyValid(item.Value))
            {
                AddNotUpdated(local, item.Key, SetError(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch, "The update is not a valid PatchObject.", null));
            }
            else
            {
                PrepareSetUpdate(item.Key, item.Value, backend, local, mailboxes, created, capabilities);
            }
        }
    }

    internal void PrepareSetUpdate(
        string id,
        JmapPatchObject patch,
        MailboxSetRequest backend,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        List<string> invalid = Validator.ValidatePatchValues(patch, capabilities.MaxSizeMailboxName);
        if (invalid.Count != 0)
        {
            AddNotUpdated(local, id, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The Mailbox patch contains invalid properties.", invalid));
            return;
        }

        Mailbox original = mailboxes[id];
        Mailbox candidate = ApplyMailboxPatch(original, patch);
        if (IsUpdateForbidden(original, candidate, mailboxes, created, capabilities))
        {
            AddNotUpdated(local, id, SetError(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden, "The current user may not rename or move the Mailbox.", null));
            return;
        }

        invalid = Validator.ValidateUpdateAgainstHierarchy(id, candidate, mailboxes, created, capabilities);
        if (invalid.Count != 0)
        {
            AddNotUpdated(local, id, SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties, "The Mailbox update would create an invalid hierarchy.", invalid));
            return;
        }

        if (backend.Update == null)
        {
            backend.Update = new Dictionary<string, JmapPatchObject>();
        }

        backend.Update.Add(id, patch);
        mailboxes[id] = candidate;
    }

    internal bool IsPatchStructurallyValid(JmapPatchObject? patch)
    {
        if (patch == null || patch.Values == null || Patches.FindConflictingPaths(patch).Count != 0)
        {
            return false;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (!Patches.IsPatchPath(item.Key) || item.Value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }
        }

        return true;
    }

    internal Mailbox ApplyMailboxPatch(Mailbox original, JmapPatchObject patch)
    {
        Mailbox candidate = CloneMailbox(original);
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == JmapProperties.Name)
            {
                candidate.Name = item.Value.GetString();
            }
            else if (item.Key == JmapProperties.ParentId)
            {
                candidate.ParentId = item.Value.ValueKind == JsonValueKind.Null ? null : item.Value.GetString();
            }
            else if (item.Key == JmapProperties.Role)
            {
                candidate.Role = item.Value.ValueKind == JsonValueKind.Null ? null : item.Value.GetString();
            }
            else if (item.Key == JmapProperties.SortOrder)
            {
                candidate.SortOrder = item.Value.GetInt64();
            }
            else if (item.Key == JmapProperties.IsSubscribed)
            {
                candidate.IsSubscribed = item.Value.ValueKind == JsonValueKind.True;
            }
        }

        return candidate;
    }

    internal Mailbox CloneMailbox(Mailbox source)
    {
        Mailbox clone = new Mailbox();
        clone.Id = source.Id;
        clone.Name = source.Name;
        clone.ParentId = source.ParentId;
        clone.Role = source.Role;
        clone.SortOrder = source.SortOrder;
        clone.TotalEmails = source.TotalEmails;
        clone.UnreadEmails = source.UnreadEmails;
        clone.TotalThreads = source.TotalThreads;
        clone.UnreadThreads = source.UnreadThreads;
        clone.MyRights = source.MyRights;
        clone.IsSubscribed = source.IsSubscribed;
        CopyUnknown(source.UnknownProperties, clone.UnknownProperties);
        return clone;
    }

    internal bool IsUpdateForbidden(
        Mailbox original,
        Mailbox candidate,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        bool protectedChange = original.Name != candidate.Name || original.ParentId != candidate.ParentId || original.Role != candidate.Role;
        if (protectedChange &&
            !Access.HasMailboxRight(original, MailboxRightKind.Rename))
        {
            return true;
        }

        if (original.ParentId == candidate.ParentId)
        {
            return false;
        }

        if (candidate.ParentId == null)
        {
            return !capabilities.MayCreateTopLevelMailbox;
        }

        if (candidate.ParentId.StartsWith("#", StringComparison.Ordinal))
        {
            return false;
        }

        Mailbox? parent = Validator.FindParent(candidate.ParentId, mailboxes, created);
        return parent != null &&
            !Access.HasMailboxRight(parent, MailboxRightKind.CreateChild);
    }

    internal void PrepareSetDestroys(
        MailboxSetRequest request,
        MailboxSetRequest backend,
        MailboxSetResponse local,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        if (request.Destroy == null)
        {
            return;
        }

        foreach (string id in request.Destroy)
        {
            if (!mailboxes.ContainsKey(id))
            {
                AddNotDestroyed(local, id, SetError(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound, "The Mailbox was not found.", null));
            }
            else
            {
                Mailbox mailbox = mailboxes[id];
                if (!Access.HasMailboxRight(mailbox, MailboxRightKind.Delete))
                {
                    AddNotDestroyed(local, id, SetError(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden, "The current user may not delete the Mailbox.", null));
                }
                else if (HasMailboxChild(id, mailboxes, created))
                {
                    AddNotDestroyed(local, id, SetError(JmapErrorCodes.MailboxHasChild, JmapSetErrorKind.MailboxHasChild, "The Mailbox has a child Mailbox.", null));
                }
                else if (!request.OnDestroyRemoveEmails && mailbox.TotalEmails != 0L)
                {
                    AddNotDestroyed(local, id, SetError(JmapErrorCodes.MailboxHasEmail, JmapSetErrorKind.MailboxHasEmail, "The Mailbox contains Email objects.", null));
                }
                else
                {
                    if (backend.Destroy == null)
                    {
                        backend.Destroy = new List<string>();
                    }

                    backend.Destroy.Add(id);
                    mailboxes.Remove(id);
                }
            }
        }
    }

    internal bool HasMailboxChild(
        string id,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        foreach (Mailbox mailbox in mailboxes.Values)
        {
            if (mailbox != null && mailbox.ParentId == id)
            {
                return true;
            }
        }

        foreach (Mailbox mailbox in created.Values)
        {
            if (mailbox != null && mailbox.ParentId == id)
            {
                return true;
            }
        }

        return false;
    }

    internal HashSet<string> SetDestroyIds(List<string>? destroy)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (destroy != null)
        {
            foreach (string id in destroy)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    internal JmapMethodError? ValidateMailboxSort(
        List<JmapComparator>? sort,
        List<string>? supportedCollations,
        bool enforceCollations)
    {
        if (sort == null)
        {
            return null;
        }

        foreach (JmapComparator comparator in sort)
        {
            if (comparator == null || comparator.Property == null || comparator.Property.Length == 0 ||
                !Values.IsValidUnicode(comparator.Property) || comparator.UnknownProperties == null)
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments, "A Mailbox sort comparator is invalid.");
            }

            if (comparator.Property != JmapProperties.SortOrder && comparator.Property != JmapProperties.Name)
            {
                return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The Mailbox sort property is not supported.");
            }

            if (comparator.UnknownProperties.Count != 0)
            {
                return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The Mailbox comparator contains an unsupported property.");
            }

            if (comparator.Collation != null && comparator.Property == JmapProperties.Name)
            {
                if (comparator.Collation.Length == 0 || !Values.IsValidUnicode(comparator.Collation))
                {
                    return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The Mailbox collation is not supported.");
                }

                if (enforceCollations && (supportedCollations == null || !supportedCollations.Contains(comparator.Collation)))
                {
                    return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort, "The Mailbox collation is not supported.");
                }
            }
        }

        return null;
    }

    internal bool IsFilterOperator(JmapFilterOperator value)
    {
        return value == JmapFilterOperator.And || value == JmapFilterOperator.Or || value == JmapFilterOperator.Not;
    }

    internal MailboxGetRequest CompleteGetRequest(MailboxGetRequest request)
    {
        MailboxGetRequest result = new MailboxGetRequest();
        result.AccountId = request.AccountId;
        if (request.Ids != null)
        {
            result.Ids = new List<string>(request.Ids);
        }

        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal MailboxGetResponse NormalizeServerGetResponse(
        MailboxGetResponse? stored,
        MailboxGetRequest request,
        long maximumNameBytes)
    {
        ValidateGetEnvelope(stored, request);
        if (stored == null)
        {
            throw ProtocolFailure("A Mailbox/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Mailbox> records = IndexCompleteMailboxes(stored.List, maximumNameBytes);
        ValidateGetPartition(stored, request, records);
        MailboxGetResponse result = new MailboxGetResponse();
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

    internal void ValidateClientGetResponse(MailboxGetResponse? response, MailboxGetRequest request)
    {
        ValidateGetEnvelope(response, request);
        if (response == null)
        {
            throw ProtocolFailure("A Mailbox/get response is required.", "methodResponses/1", default(JsonElement));
        }

        Dictionary<string, Mailbox> records = IndexProjectedMailboxes(response.List, request.Properties);
        ValidateGetPartition(response, request, records);
    }

    internal void ValidateGetEnvelope(MailboxGetResponse? response, MailboxGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null || !Values.IsValidUnicode(response.State) ||
            response.List == null || response.NotFound == null)
        {
            throw ProtocolFailure("The Mailbox/get response has invalid account, state, or collection metadata.", "methodResponses/1", default(JsonElement));
        }
    }

    internal Dictionary<string, Mailbox> IndexCompleteMailboxes(List<Mailbox> mailboxes, long maximumNameBytes)
    {
        Dictionary<string, Mailbox> records = new Dictionary<string, Mailbox>(StringComparer.Ordinal);
        int index = 0;
        foreach (Mailbox mailbox in mailboxes)
        {
            List<string> invalid = Validator.ValidateStoredMailbox(mailbox, maximumNameBytes);
            if (mailbox == null || invalid.Count != 0 || mailbox.Id == null || records.ContainsKey(mailbox.Id))
            {
                throw ProtocolFailure("The Mailbox/get response contains an invalid or duplicate Mailbox.", "list/" + index, default(JsonElement));
            }

            records.Add(mailbox.Id, mailbox);
            index = index + 1;
        }

        return records;
    }

    internal Dictionary<string, Mailbox> IndexProjectedMailboxes(List<Mailbox> mailboxes, List<string>? properties)
    {
        Dictionary<string, Mailbox> records = new Dictionary<string, Mailbox>(StringComparer.Ordinal);
        int index = 0;
        foreach (Mailbox mailbox in mailboxes)
        {
            if (!IsValidProjectedMailbox(mailbox, properties) || mailbox.Id == null || records.ContainsKey(mailbox.Id))
            {
                throw ProtocolFailure("The Mailbox/get response contains an invalid or duplicate Mailbox.", "list/" + index, default(JsonElement));
            }

            records.Add(mailbox.Id, mailbox);
            index = index + 1;
        }

        return records;
    }

    internal bool IsValidProjectedMailbox(Mailbox? mailbox, List<string>? properties)
    {
        if (mailbox == null || !Values.IsId(mailbox.Id))
        {
            return false;
        }

        if (properties == null)
        {
            return Validator.ValidateStoredMailbox(mailbox, JmapProtocolLimits.MaximumInt).Count == 0;
        }

        HashSet<string> included = new HashSet<string>(properties, StringComparer.Ordinal);
        if (included.Contains(JmapProperties.Name) && !Validator.IsMailboxName(mailbox.Name, JmapProtocolLimits.MaximumInt))
        {
            return false;
        }

        if (included.Contains(JmapProperties.ParentId) && !Validator.IsParentId(mailbox.ParentId))
        {
            return false;
        }

        if (included.Contains(JmapProperties.Role) && mailbox.Role != null && !Values.IsMailboxRole(mailbox.Role))
        {
            return false;
        }

        if (included.Contains(JmapProperties.SortOrder) && (mailbox.SortOrder < 0L || mailbox.SortOrder > JmapProtocolLimits.MaximumMailboxSortOrder))
        {
            return false;
        }

        if (included.Contains(JmapProperties.TotalEmails) && !Values.IsUnsignedInt(mailbox.TotalEmails))
        {
            return false;
        }

        if (included.Contains(JmapProperties.UnreadEmails) && !Values.IsUnsignedInt(mailbox.UnreadEmails))
        {
            return false;
        }

        if (included.Contains(JmapProperties.TotalThreads) && !Values.IsUnsignedInt(mailbox.TotalThreads))
        {
            return false;
        }

        if (included.Contains(JmapProperties.UnreadThreads) && !Values.IsUnsignedInt(mailbox.UnreadThreads))
        {
            return false;
        }

        if (included.Contains(JmapProperties.MyRights) && mailbox.MyRights == null)
        {
            return false;
        }

        return true;
    }

    internal void ValidateGetPartition(
        MailboxGetResponse response,
        MailboxGetRequest request,
        Dictionary<string, Mailbox> records)
    {
        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !notFound.Add(id) || records.ContainsKey(id))
            {
                throw ProtocolFailure("The Mailbox/get notFound partition is invalid.", "notFound", default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (notFound.Count != 0)
            {
                throw ProtocolFailure("Mailbox/get cannot return notFound identifiers when all Mailboxes were requested.", "notFound", default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        foreach (string id in records.Keys)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Mailbox/get returned an unrequested Mailbox.", "list", default(JsonElement));
            }
        }

        foreach (string id in notFound)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("Mailbox/get returned an unrequested notFound identifier.", "notFound", default(JsonElement));
            }
        }

        if (records.Count + notFound.Count != requested.Count)
        {
            throw ProtocolFailure("Mailbox/get did not classify every requested identifier.", "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangesResponse(MailboxChangesResponse? response, MailboxChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.OldState != request.SinceState ||
            response.NewState == null || !Values.IsValidUnicode(response.NewState) || response.Created == null ||
            response.Updated == null || response.Destroyed == null)
        {
            throw ProtocolFailure("The Mailbox/changes response has invalid account, state, or collection metadata.", "methodResponses/1", default(JsonElement));
        }

        HashSet<string> classified = new HashSet<string>(StringComparer.Ordinal);
        ValidateChangeIds(response.Created, classified, "created");
        ValidateChangeIds(response.Updated, classified, "updated");
        ValidateChangeIds(response.Destroyed, classified, "destroyed");
        long changeCount = response.Created.Count + response.Updated.Count + response.Destroyed.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && changeCount > request.MaxChanges)
        {
            throw ProtocolFailure("Mailbox/changes returned more identifiers than maxChanges permits.", "methodResponses/1", default(JsonElement));
        }

        ValidateUpdatedProperties(response);
    }

    internal void ValidateChangeIds(List<string> ids, HashSet<string> classified, string path)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !classified.Add(id))
            {
                throw ProtocolFailure("A Mailbox change identifier is invalid or has multiple classifications.", path, default(JsonElement));
            }
        }
    }

    internal void ValidateUpdatedProperties(MailboxChangesResponse response)
    {
        if (response.UpdatedProperties == null)
        {
            return;
        }

        if (response.Updated.Count != 0 && response.UpdatedProperties.Count == 0)
        {
            throw ProtocolFailure("updatedProperties may only describe count-only Mailbox updates.", "updatedProperties", default(JsonElement));
        }

        HashSet<string> allowed = CountPropertySet();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string property in response.UpdatedProperties)
        {
            if (property == null || !allowed.Contains(property) || !seen.Add(property))
            {
                throw ProtocolFailure("Mailbox/changes contains an invalid changed property.", "updatedProperties", default(JsonElement));
            }
        }
    }

    internal void ValidateQueryResponse(MailboxQueryResponse? response, MailboxQueryRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.QueryState == null ||
            !Values.IsValidUnicode(response.QueryState) || !Values.IsUnsignedInt(response.Position) || response.Ids == null)
        {
            throw ProtocolFailure("The Mailbox/query response has invalid account, state, position, or result metadata.", "methodResponses/1", default(JsonElement));
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in response.Ids)
        {
            if (!Values.IsId(id) || !ids.Add(id))
            {
                throw ProtocolFailure("The Mailbox/query result contains an invalid or duplicate identifier.", "ids/" + index, default(JsonElement));
            }

            index = index + 1;
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested || (response.HasTotal && !Values.IsUnsignedInt(response.Total)))
        {
            throw ProtocolFailure("The Mailbox/query total metadata does not match the request.", "total", default(JsonElement));
        }

        if (response.HasTotal && response.Ids.Count != 0 &&
            (response.Position > response.Total || response.Ids.Count > response.Total - response.Position))
        {
            throw ProtocolFailure("The Mailbox/query result window exceeds the reported total.", "ids", default(JsonElement));
        }

        if (response.HasLimit)
        {
            if (!Values.IsUnsignedInt(response.Limit) || response.Ids.Count > response.Limit)
            {
                throw ProtocolFailure("The Mailbox/query enforced limit is invalid.", "limit", default(JsonElement));
            }

            if (request.HasLimit && !request.IsLimitNull && response.Limit >= request.Limit)
            {
                throw ProtocolFailure("The Mailbox/query response reports a limit that is not lower than the requested limit.", "limit", default(JsonElement));
            }
        }

        if (request.HasLimit && !request.IsLimitNull && response.Ids.Count > request.Limit)
        {
            throw ProtocolFailure("The Mailbox/query result exceeds the requested limit.", "ids", default(JsonElement));
        }

        if (request.Anchor == null && (!request.HasPosition || request.Position >= 0L))
        {
            long expectedPosition = request.HasPosition ? request.Position : 0L;
            if (response.Position != expectedPosition)
            {
                throw ProtocolFailure("The Mailbox/query response position does not match the requested window.", "position", default(JsonElement));
            }
        }
    }

    internal void ValidateQueryChangesResponse(
        MailboxQueryChangesResponse? response,
        MailboxQueryChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.OldQueryState != request.SinceQueryState ||
            response.NewQueryState == null || !Values.IsValidUnicode(response.NewQueryState) ||
            response.Removed == null || response.Added == null)
        {
            throw ProtocolFailure("The Mailbox/queryChanges response has invalid account, state, or collection metadata.", "methodResponses/1", default(JsonElement));
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested || (response.HasTotal && !Values.IsUnsignedInt(response.Total)))
        {
            throw ProtocolFailure("The Mailbox/queryChanges total metadata does not match the request.", "total", default(JsonElement));
        }

        HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal);
        int removedIndex = 0;
        foreach (string id in response.Removed)
        {
            if (!Values.IsId(id) || !removed.Add(id))
            {
                throw ProtocolFailure("The Mailbox/queryChanges removed list contains an invalid or duplicate identifier.", "removed/" + removedIndex, default(JsonElement));
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
                (hasPreviousIndex && item.Index < previousIndex) || item.Id == null || !added.Add(item.Id))
            {
                throw ProtocolFailure("The Mailbox/queryChanges added list is invalid or is not ordered by index.", "added/" + addedIndex, default(JsonElement));
            }

            if (response.HasTotal && item.Index >= response.Total)
            {
                throw ProtocolFailure("A Mailbox/queryChanges added index exceeds the reported total.", "added/" + addedIndex + "/index", default(JsonElement));
            }

            previousIndex = item.Index;
            hasPreviousIndex = true;
            addedIndex = addedIndex + 1;
        }
    }

    internal bool ExceedsMaximumQueryChanges(
        MailboxQueryChangesResponse response,
        MailboxQueryChangesRequest request)
    {
        if (!request.HasMaxChanges || request.IsMaxChangesNull)
        {
            return false;
        }

        long changeCount = (long)response.Removed.Count + response.Added.Count;
        return changeCount > request.MaxChanges;
    }

    internal void ValidateGetProjection(JsonElement arguments, List<string>? properties)
    {
        if (properties == null)
        {
            return;
        }

        HashSet<string> expected = new HashSet<string>(properties, StringComparer.Ordinal);
        expected.Add(JmapProperties.Id);
        JsonElement list = Codec.Values.GetMember(arguments, JmapProperties.List);
        int index = 0;
        foreach (JsonElement mailbox in list.EnumerateArray())
        {
            HashSet<string> actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in mailbox.EnumerateObject())
            {
                if (!expected.Contains(property.Name))
                {
                    throw ProtocolFailure("Mailbox/get returned a property that was not requested.", "list/" + index + "/" + property.Name, property.Value);
                }

                actual.Add(property.Name);
            }

            foreach (string property in expected)
            {
                if (!actual.Contains(property))
                {
                    throw ProtocolFailure("Mailbox/get omitted a requested property.", "list/" + index + "/" + property, mailbox);
                }
            }

            index = index + 1;
        }
    }

    internal JmapMethodError? ValidateMailAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound, "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null)
        {
            throw ProtocolFailure("The Session contains a null Mailbox account.", "session/accounts/" + id, default(JsonElement));
        }

        if (account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid account capability data.", "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Mail))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod, "The account does not support JMAP Mail.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Mail];
        if (capability == null || capability.Mail == null)
        {
            throw ProtocolFailure("The account contains invalid Mail capability data.", "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal JmapMailCapabilities GetMailCapabilities(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        JmapMailCapabilities? capabilities = session.Accounts[id].AccountCapabilities[JmapCapabilities.Mail].Mail;
        if (capabilities == null)
        {
            throw ProtocolFailure("The account contains invalid Mail capability data.", "session/accounts/" + id, default(JsonElement));
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

    internal void ValidateDispatchDependencies(
        JmapSession session,
        IMailboxService mailboxService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || mailboxService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("Mailbox dispatch dependencies are required.", "mailboxService", default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A Mailbox invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong Mailbox method name.", "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("Mailbox arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable Mailbox call identifier is required.", "methodCalls/0/2");
        }
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringArray(ids) && IsOptionalStringArray(properties);
    }

    internal bool IsChangesRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            (maximum.ValueKind == JsonValueKind.Undefined || maximum.ValueKind == JsonValueKind.Null || IsUnsignedIntegerShape(maximum));
    }

    internal bool IsQueryRequestShape(JsonElement arguments, int depth, int maximumDepth)
    {
        if (depth > maximumDepth)
        {
            return false;
        }

        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement sort = Codec.Values.GetMember(arguments, JmapProperties.Sort);
        JsonElement position = Codec.Values.GetMember(arguments, JmapProperties.Position);
        JsonElement anchor = Codec.Values.GetMember(arguments, JmapProperties.Anchor);
        JsonElement anchorOffset = Codec.Values.GetMember(arguments, JmapProperties.AnchorOffset);
        JsonElement limit = Codec.Values.GetMember(arguments, JmapProperties.Limit);
        JsonElement calculateTotal = Codec.Values.GetMember(arguments, JmapProperties.CalculateTotal);
        JsonElement sortAsTree = Codec.Values.GetMember(arguments, JmapProperties.SortAsTree);
        JsonElement filterAsTree = Codec.Values.GetMember(arguments, JmapProperties.FilterAsTree);
        if (accountId.ValueKind != JsonValueKind.String || !IsOptionalFilterShape(filter, depth, maximumDepth) ||
            !IsOptionalComparatorArray(sort) || !IsOptionalInteger(position) || !IsOptionalString(anchor) ||
            !IsOptionalInteger(anchorOffset) || !IsOptionalUnsignedInteger(limit) || !IsOptionalBoolean(calculateTotal) ||
            !IsOptionalBoolean(sortAsTree) || !IsOptionalBoolean(filterAsTree))
        {
            return false;
        }

        return true;
    }

    internal bool IsQueryChangesRequestShape(JsonElement arguments, int depth, int maximumDepth)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement sort = Codec.Values.GetMember(arguments, JmapProperties.Sort);
        JsonElement sinceQueryState = Codec.Values.GetMember(arguments, JmapProperties.SinceQueryState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        JsonElement upToId = Codec.Values.GetMember(arguments, JmapProperties.UpToId);
        JsonElement calculateTotal = Codec.Values.GetMember(arguments, JmapProperties.CalculateTotal);
        JsonElement sortAsTree = Codec.Values.GetMember(arguments, JmapProperties.SortAsTree);
        JsonElement filterAsTree = Codec.Values.GetMember(arguments, JmapProperties.FilterAsTree);
        if (accountId.ValueKind != JsonValueKind.String || sinceQueryState.ValueKind != JsonValueKind.String ||
            !IsOptionalFilterShape(filter, depth, maximumDepth) || !IsOptionalComparatorArray(sort) ||
            !IsOptionalUnsignedInteger(maximum) || !IsOptionalString(upToId) || !IsOptionalBoolean(calculateTotal) ||
            !IsOptionalBoolean(sortAsTree) || !IsOptionalBoolean(filterAsTree))
        {
            return false;
        }

        return true;
    }

    internal bool IsSetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ifInState = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement update = Codec.Values.GetMember(arguments, JmapProperties.Update);
        JsonElement destroy = Codec.Values.GetMember(arguments, JmapProperties.Destroy);
        JsonElement removeEmails = Codec.Values.GetMember(arguments, JmapProperties.OnDestroyRemoveEmails);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalString(ifInState) &&
            IsOptionalMailboxCreateMap(create) && IsOptionalPatchMap(update) && IsOptionalStringArray(destroy) &&
            IsOptionalBoolean(removeEmails);
    }

    internal bool IsOptionalMailboxCreateMap(JsonElement value)
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
            if (!IsMailboxCreateShape(property.Value))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsMailboxCreateShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement id = Codec.Values.GetMember(value, JmapProperties.Id);
        JsonElement name = Codec.Values.GetMember(value, JmapProperties.Name);
        JsonElement parentId = Codec.Values.GetMember(value, JmapProperties.ParentId);
        JsonElement role = Codec.Values.GetMember(value, JmapProperties.Role);
        JsonElement sortOrder = Codec.Values.GetMember(value, JmapProperties.SortOrder);
        JsonElement totalEmails = Codec.Values.GetMember(value, JmapProperties.TotalEmails);
        JsonElement unreadEmails = Codec.Values.GetMember(value, JmapProperties.UnreadEmails);
        JsonElement totalThreads = Codec.Values.GetMember(value, JmapProperties.TotalThreads);
        JsonElement unreadThreads = Codec.Values.GetMember(value, JmapProperties.UnreadThreads);
        JsonElement rights = Codec.Values.GetMember(value, JmapProperties.MyRights);
        JsonElement subscribed = Codec.Values.GetMember(value, JmapProperties.IsSubscribed);
        return IsOptionalString(id) && IsOptionalString(name) && IsOptionalString(parentId) && IsOptionalString(role) &&
            IsOptionalUnsignedInteger(sortOrder) && IsOptionalUnsignedInteger(totalEmails) &&
            IsOptionalUnsignedInteger(unreadEmails) && IsOptionalUnsignedInteger(totalThreads) &&
            IsOptionalUnsignedInteger(unreadThreads) && IsOptionalMailboxRightsShape(rights) && IsOptionalBoolean(subscribed);
    }

    internal bool IsOptionalMailboxRightsShape(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayReadItems)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayAddItems)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayRemoveItems)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MaySetSeen)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MaySetKeywords)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayCreateChild)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayRename)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MayDelete)) &&
            IsRequiredBoolean(Codec.Values.GetMember(value, JmapProperties.MaySubmit));
    }

    internal bool IsRequiredBoolean(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False;
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

    internal bool IsOptionalFilterShape(JsonElement value, int depth, int maximumDepth)
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
        JsonElement parentId = Codec.Values.GetMember(value, JmapProperties.ParentId);
        JsonElement name = Codec.Values.GetMember(value, JmapProperties.Name);
        JsonElement role = Codec.Values.GetMember(value, JmapProperties.Role);
        JsonElement hasAnyRole = Codec.Values.GetMember(value, JmapProperties.HasAnyRole);
        JsonElement isSubscribed = Codec.Values.GetMember(value, JmapProperties.IsSubscribed);
        if (operatorValue.ValueKind != JsonValueKind.Undefined)
        {
            if (operatorValue.ValueKind != JsonValueKind.String || !IsFilterOperatorToken(operatorValue.GetString()) ||
                conditions.ValueKind != JsonValueKind.Array || parentId.ValueKind != JsonValueKind.Undefined ||
                name.ValueKind != JsonValueKind.Undefined || role.ValueKind != JsonValueKind.Undefined ||
                hasAnyRole.ValueKind != JsonValueKind.Undefined || isSubscribed.ValueKind != JsonValueKind.Undefined)
            {
                return false;
            }

            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                if (condition.ValueKind != JsonValueKind.Object || !IsOptionalFilterShape(condition, depth + 1, maximumDepth))
                {
                    return false;
                }
            }

            return true;
        }

        if (conditions.ValueKind != JsonValueKind.Undefined || !IsOptionalString(parentId) ||
            (name.ValueKind != JsonValueKind.Undefined && name.ValueKind != JsonValueKind.String) ||
            !IsOptionalString(role) || !IsOptionalBoolean(hasAnyRole) || !IsOptionalBoolean(isSubscribed))
        {
            return false;
        }

        return true;
    }

    internal bool IsFilterOperatorToken(string? value)
    {
        return value == "AND" || value == "OR" || value == "NOT";
    }

    internal bool IsOptionalComparatorArray(JsonElement value)
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
            if (!IsComparatorShape(comparator))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsComparatorShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement property = Codec.Values.GetMember(value, JmapProperties.Property);
        JsonElement ascending = Codec.Values.GetMember(value, JmapProperties.IsAscending);
        JsonElement collation = Codec.Values.GetMember(value, JmapProperties.Collation);
        return property.ValueKind == JsonValueKind.String && IsOptionalBoolean(ascending) && IsOptionalString(collation);
    }

    internal bool IsOptionalBoolean(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False;
    }

    internal bool IsOptionalString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.String;
    }

    internal bool IsOptionalInteger(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || IsIntegerShape(value);
    }

    internal bool IsOptionalUnsignedInteger(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null || IsUnsignedIntegerShape(value);
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

    internal bool IsUnsignedIntegerShape(JsonElement value)
    {
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

    internal void ValidateSetResponse(MailboxSetResponse? response, MailboxSetRequest request, long maximumNameBytes)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            (response.OldState != null && !Values.IsValidUnicode(response.OldState)) ||
            (request.IfInState != null && response.OldState != request.IfInState))
        {
            throw ProtocolFailure("The Mailbox/set response has invalid account or state metadata.", "methodResponses/1", default(JsonElement));
        }

        ValidateSetCreatePartitions(response, request.Create, maximumNameBytes);
        ValidateSetUpdatePartitions(response, request.Update);
        ValidateSetDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateSetCreatePartitions(
        MailboxSetResponse response,
        Dictionary<string, Mailbox>? requested,
        long maximumNameBytes)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, Mailbox> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null ||
                    Validator.ValidateStoredMailbox(item.Value, maximumNameBytes).Count != 0)
                {
                    throw ProtocolFailure("The Mailbox/set created map is invalid.", "created", default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, "notCreated");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Mailbox/set response does not partition every create.", "created", default(JsonElement));
        }
    }

    internal void ValidateSetUpdatePartitions(
        MailboxSetResponse response,
        Dictionary<string, JmapPatchObject>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Updated != null)
        {
            foreach (KeyValuePair<string, JsonElement> item in response.Updated)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) ||
                    (item.Value.ValueKind != JsonValueKind.Null && item.Value.ValueKind != JsonValueKind.Object))
                {
                    throw ProtocolFailure("The Mailbox/set updated map is invalid.", "updated", default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, "notUpdated");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Mailbox/set response does not partition every update.", "updated", default(JsonElement));
        }
    }

    internal void ValidateSetDestroyPartitions(MailboxSetResponse response, List<string>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(requested, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                if (!expected.Contains(id) || !seen.Add(id))
                {
                    throw ProtocolFailure("The Mailbox/set destroyed list is invalid.", "destroyed", default(JsonElement));
                }
            }
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, "notDestroyed");
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("The Mailbox/set response does not partition every destroy.", "destroyed", default(JsonElement));
        }
    }

    internal void ValidateSetErrors(
        Dictionary<string, JmapSetError> errors,
        HashSet<string> expected,
        HashSet<string> seen,
        string path)
    {
        foreach (KeyValuePair<string, JmapSetError> item in errors)
        {
            if (!expected.Contains(item.Key) || !seen.Add(item.Key) || item.Value == null)
            {
                throw ProtocolFailure("A Mailbox/set error partition is invalid.", path, default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            item.Value.Kind = normalized.Kind;
        }
    }

    internal void MergeSetFailures(MailboxSetResponse target, MailboxSetResponse source)
    {
        if (source.NotCreated != null)
        {
            if (target.NotCreated == null)
            {
                target.NotCreated = new Dictionary<string, JmapSetError>();
            }

            foreach (KeyValuePair<string, JmapSetError> item in source.NotCreated)
            {
                target.NotCreated.Add(item.Key, item.Value);
            }
        }

        if (source.NotUpdated != null)
        {
            if (target.NotUpdated == null)
            {
                target.NotUpdated = new Dictionary<string, JmapSetError>();
            }

            foreach (KeyValuePair<string, JmapSetError> item in source.NotUpdated)
            {
                target.NotUpdated.Add(item.Key, item.Value);
            }
        }

        if (source.NotDestroyed != null)
        {
            if (target.NotDestroyed == null)
            {
                target.NotDestroyed = new Dictionary<string, JmapSetError>();
            }

            foreach (KeyValuePair<string, JmapSetError> item in source.NotDestroyed)
            {
                target.NotDestroyed.Add(item.Key, item.Value);
            }
        }
    }

    internal void AddNotCreated(MailboxSetResponse response, string creationId, JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(MailboxSetResponse response, string id, JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal void AddNotDestroyed(MailboxSetResponse response, string id, JmapSetError error)
    {
        if (response.NotDestroyed == null)
        {
            response.NotDestroyed = new Dictionary<string, JmapSetError>();
        }

        response.NotDestroyed.Add(id, error);
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

    internal HashSet<string> MailboxPropertySet()
    {
        return new HashSet<string>(Codec.MailboxNames(), StringComparer.Ordinal);
    }

    internal HashSet<string> CountPropertySet()
    {
        return new HashSet<string>(
            new List<string>
            {
                JmapProperties.TotalEmails,
                JmapProperties.UnreadEmails,
                JmapProperties.TotalThreads,
                JmapProperties.UnreadThreads
            },
            StringComparer.Ordinal);
    }

    internal JmapInvocation GetSuccessInvocation(MailboxGetResponse response, List<string>? properties, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeProjectedMailboxGetResponse(response, properties, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxGet, callId, stream.ToArray());
    }

    internal JmapInvocation ChangesSuccessInvocation(MailboxChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxChanges, callId, stream.ToArray());
    }

    internal JmapInvocation QuerySuccessInvocation(MailboxQueryResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxQueryResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxQuery, callId, stream.ToArray());
    }

    internal JmapInvocation QueryChangesSuccessInvocation(MailboxQueryChangesResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxQueryChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation SetSuccessInvocation(MailboxSetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeMailboxSetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.MailboxSet, callId, stream.ToArray());
    }

    internal JmapInvocation ErrorInvocation(string? type, JmapMethodErrorKind kind, string? description, string callId)
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
