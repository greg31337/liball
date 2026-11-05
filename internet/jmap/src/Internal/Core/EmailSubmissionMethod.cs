using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EmailSubmissionSetPreparation
{
    internal EmailSubmissionSetRequest Backend = new EmailSubmissionSetRequest();
    internal EmailSubmissionSetResponse Local = new EmailSubmissionSetResponse();
}

internal class EmailSubmissionSetDispatchResult
{
    internal EmailSubmissionSetResponse? SubmissionResponse;
    internal EmailSetRequest? EmailRequest;
    internal EmailSetResponse? EmailResponse;
    internal List<JmapInvocation> MethodResponses = new List<JmapInvocation>();
}

internal class EmailSubmissionCreateContext
{
    internal Dictionary<string, Email> Emails = new Dictionary<string, Email>(StringComparer.Ordinal);
    internal HashSet<string> MissingEmails = new HashSet<string>(StringComparer.Ordinal);
    internal Dictionary<string, Identity> Identities = new Dictionary<string, Identity>(StringComparer.Ordinal);
    internal HashSet<string> MissingIdentities = new HashSet<string>(StringComparer.Ordinal);
}

internal class EmailSubmissionMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal SubmissionValidator SubmissionValues = new SubmissionValidator();
    internal EmailSetValidator SetValues = new EmailSetValidator();
    internal EmailMethod Emails = new EmailMethod();
    internal IdentityMethod Identities = new IdentityMethod();
    internal SubmissionEnvelopeProcessor Envelopes = new SubmissionEnvelopeProcessor();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal EmailSubmissionJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal EmailSubmissionMethod()
    {
        Codec = new EmailSubmissionJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessGetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessGetInvocation(invocation, session, submissionService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessGetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSubmissionGet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsGetRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission/get arguments are invalid.", callId);
        }

        EmailSubmissionGetRequest request = Codec.DecodeSubmissionGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, submissionService, context, options);
        JmapMethodError? requestError = ValidateGetRequest(request, options.MaxObjectsInGet);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        EmailSubmissionGetRequest backendRequest = CompleteGetRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        EmailSubmissionGetResponse stored = submissionService
            .GetStoredEmailSubmissionsAsync(backendRequest, context, cancellationToken).GetAwaiter().GetResult();
        if (request.Ids == null && stored != null && stored.List != null &&
            stored.List.Count > options.MaxObjectsInGet)
        {
            return ErrorInvocation(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "Returning all EmailSubmissions would exceed the get object limit.", callId);
        }

        EmailSubmissionGetResponse response = NormalizeGetResponse(stored, backendRequest);
        return GetSuccessInvocation(response, request.Properties, callId);
    }

    internal Task<JmapInvocation> ProcessChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessChangesInvocation(invocation, session, submissionService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSubmissionChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsChangesRequestShape(invocation.Arguments))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission/changes arguments are invalid.", callId);
        }

        EmailSubmissionChangesRequest request = Codec.DecodeSubmissionChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, submissionService, context, options);
        JmapMethodError? requestError = ValidateChangesRequest(request);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailSubmissionChangesResponse response = submissionService
            .GetStoredEmailSubmissionChangesAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateChangesResponse(response, request);
        return ChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryInvocation(invocation, session, submissionService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessQueryInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSubmissionQuery);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission/query arguments are invalid.", callId);
        }

        EmailSubmissionQueryRequest request = Codec.DecodeSubmissionQueryRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, submissionService, context, options);
        JmapMethodError? requestError = ValidateQueryRequest(request, options.MaxJsonDepth);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailSubmissionQueryResponse response = submissionService
            .QueryStoredEmailSubmissionsAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateQueryResponse(response, request);
        return QuerySuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessQueryChangesInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessQueryChangesInvocation(invocation, session, submissionService, context, options,
                cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessQueryChangesInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSubmissionQueryChanges);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsQueryChangesRequestShape(invocation.Arguments, 1, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission/queryChanges arguments are invalid.", callId);
        }

        EmailSubmissionQueryChangesRequest request =
            Codec.DecodeSubmissionQueryChangesRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, submissionService, context, options);
        JmapMethodError? requestError = ValidateQueryChangesRequest(request, options.MaxJsonDepth);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        EmailSubmissionQueryChangesResponse response = submissionService
            .GetStoredEmailSubmissionQueryChangesAsync(request, context, cancellationToken)
            .GetAwaiter().GetResult();
        ValidateQueryChangesResponse(response, request);
        if (ExceedsMaximumQueryChanges(response, request))
        {
            return ErrorInvocation(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges,
                "The EmailSubmission query changes exceed maxChanges.", callId);
        }

        return QueryChangesSuccessInvocation(response, callId);
    }

    internal Task<JmapInvocation> ProcessSetInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        IEmailService emailService,
        IIdentityService identityService,
        IJmapClock? clock,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocation(invocation, session, submissionService, emailService,
                identityService, clock, context, options, cancellationToken), cancellationToken);
    }

    internal JmapInvocation ProcessSetInvocation(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        IEmailService emailService,
        IIdentityService identityService,
        IJmapClock? clock,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        EmailSubmissionSetDispatchResult result = ProcessSetInvocationResult(
            invocation, session, submissionService, emailService, identityService, clock,
            context, options, cancellationToken);
        return result.MethodResponses[0];
    }

    internal Task<EmailSubmissionSetDispatchResult> ProcessSetInvocationResultAsync(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        IEmailService emailService,
        IIdentityService identityService,
        IJmapClock? clock,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessSetInvocationResult(invocation, session, submissionService, emailService,
                identityService, clock, context, options, cancellationToken), cancellationToken);
    }

    internal EmailSubmissionSetDispatchResult ProcessSetInvocationResult(
        JmapInvocation invocation,
        JmapSession session,
        IEmailSubmissionService submissionService,
        IEmailService emailService,
        IIdentityService identityService,
        IJmapClock? clock,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation, JmapMethods.EmailSubmissionSet);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsSetRequestShape(invocation.Arguments))
        {
            return SingleSetDispatchResult(
                ErrorInvocation(JmapErrorCodes.InvalidArguments,
                    JmapMethodErrorKind.InvalidArguments,
                    "The EmailSubmission/set arguments are invalid.", callId));
        }

        EmailSubmissionSetRequest request = Codec.DecodeSubmissionSetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, submissionService, context, options);
        JmapMethodError? requestError = ValidateSetRequest(request, options.MaxObjectsInSet);
        if (requestError != null)
        {
            return SingleSetDispatchResult(
                ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId));
        }

        JmapMethodError? accountError = ValidateSubmissionAccount(session, request.AccountId);
        if (accountError != null)
        {
            return SingleSetDispatchResult(
                ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId));
        }

        string accountId = request.AccountId == null ? "" : request.AccountId;
        if (session.Accounts[accountId].IsReadOnly)
        {
            return SingleSetDispatchResult(
                ErrorInvocation(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly,
                    "The account is read-only.", callId));
        }

        EmailSubmissionGetRequest snapshotRequest = SetSnapshotRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        EmailSubmissionGetResponse stored = submissionService
            .GetStoredEmailSubmissionsAsync(snapshotRequest, context, cancellationToken)
            .GetAwaiter().GetResult();
        EmailSubmissionGetResponse snapshot = NormalizeGetResponse(stored, snapshotRequest);
        if (request.IfInState != null && request.IfInState != snapshot.State)
        {
            return SingleSetDispatchResult(
                ErrorInvocation(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch,
                    "The ifInState value does not match the current EmailSubmission state.", callId));
        }

        JmapSubmissionCapabilities capabilities = GetSubmissionCapabilities(session, request.AccountId);
        EmailSubmissionCreateContext createContext = LoadSubmissionCreateContext(
            request, emailService, identityService, context, cancellationToken);
        Dictionary<string, EmailSubmission> submissions =
            IndexSubmissions(snapshot.List, snapshotRequest.Properties);
        EmailSubmissionSetPreparation preparation = PrepareSetRequest(
            request, invocation.Arguments, submissions, snapshot.NotFound, snapshot.State,
            capabilities, createContext, clock);
        EmailSubmissionSetResponse response = preparation.Local;
        if (CountSetActions(preparation.Backend) != 0L)
        {
            cancellationToken.ThrowIfCancellationRequested();
            response = submissionService
                .SetStoredEmailSubmissionsAsync(preparation.Backend, context, cancellationToken)
                .GetAwaiter().GetResult();
            ValidateSetResponse(response, preparation.Backend);
            if (response.OldState != snapshot.State)
            {
                throw ProtocolFailure("EmailSubmission/set returned an unexpected oldState.",
                    JmapProperties.OldState, default(JsonElement));
            }

            MergeSetFailures(response, preparation.Local);
        }

        ValidateSetResponse(response, request);
        LocalizeUserVisibleSetErrors(response, context);
        EmailSubmissionSetDispatchResult result = new EmailSubmissionSetDispatchResult();
        result.SubmissionResponse = response;
        result.MethodResponses.Add(SetSuccessInvocation(response, callId));
        EmailSetRequest? emailRequest = BuildImplicitEmailSetRequest(request, response, submissions);
        if (emailRequest != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmailSetResponse emailResponse = emailService
                .SetStoredEmailsAsync(emailRequest, context, cancellationToken).GetAwaiter().GetResult();
            Emails.ValidateSetResponse(emailResponse, emailRequest);
            result.EmailRequest = emailRequest;
            result.EmailResponse = emailResponse;
            result.MethodResponses.Add(Emails.SetSuccessInvocation(emailResponse, callId));
        }

        return result;
    }

    internal void LocalizeUserVisibleSetErrors(
        EmailSubmissionSetResponse response,
        JmapRequestContext context)
    {
        if (Errors.Localizer == null || response.NotCreated == null)
        {
            return;
        }

        foreach (JmapSetError error in response.NotCreated.Values)
        {
            if (error.Type == JmapErrorCodes.ForbiddenToSend)
            {
                JmapSetError localized = Errors.CreateUserVisibleSetError(
                    JmapErrorCodes.ForbiddenToSend,
                    "submission.sendingBlocked",
                    error.Description == null ? "Sending is blocked." : error.Description,
                    context.AcceptLanguage);
                error.Description = localized.Description;
                error.Kind = localized.Kind;
            }
        }
    }

    internal JmapInvocation CreateGetInvocation(EmailSubmissionGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid EmailSubmission/get arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionGet, callId, stream.ToArray());
    }

    internal JmapInvocation CreateChangesInvocation(EmailSubmissionChangesRequest request, string callId)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure(
                "Valid EmailSubmission/changes arguments and a call identifier are required.", "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryInvocation(EmailSubmissionQueryRequest request, string callId)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid EmailSubmission/query arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionQueryRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionQuery, callId, stream.ToArray());
    }

    internal JmapInvocation CreateQueryChangesInvocation(
        EmailSubmissionQueryChangesRequest request,
        string callId)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure(
                "Valid EmailSubmission/queryChanges arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionQueryChangesRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation CreateSetInvocation(EmailSubmissionSetRequest request, string callId)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid EmailSubmission/set arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionSetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionSet, callId, stream.ToArray());
    }

    internal EmailSubmissionGetResponse MapGetResponse(
        JmapResponse response,
        string callId,
        EmailSubmissionGetRequest request)
    {
        JmapMethodError? error = ValidateGetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original EmailSubmission/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailSubmissionGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailSubmissionGet);
        ValidateGetProjection(arguments, request.Properties);
        EmailSubmissionGetResponse decoded = Codec.DecodeSubmissionGetResponse(arguments);
        ValidateClientGetResponse(decoded, request);
        return decoded;
    }

    internal EmailSubmissionChangesResponse MapChangesResponse(
        JmapResponse response,
        string callId,
        EmailSubmissionChangesRequest request)
    {
        JmapMethodError? error = ValidateChangesRequest(request);
        if (error != null)
        {
            throw ValidationFailure("Valid original EmailSubmission/changes arguments are required.",
                "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.EmailSubmissionChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.EmailSubmissionChanges);
        EmailSubmissionChangesResponse decoded = Codec.DecodeSubmissionChangesResponse(arguments);
        ValidateChangesResponse(decoded, request);
        return decoded;
    }

    internal EmailSubmissionQueryResponse MapQueryResponse(
        JmapResponse response,
        string callId,
        EmailSubmissionQueryRequest request)
    {
        JmapMethodError? error = ValidateQueryRequest(request, 64);
        if (error != null)
        {
            throw ValidationFailure("Valid original EmailSubmission/query arguments are required.",
                "request");
        }

        JmapCallResult result = Responses.FindTypedResult(
            response, callId, JmapMethods.EmailSubmissionQuery);
        JsonElement arguments = Responses.RequireSuccessfulArguments(
            result, JmapMethods.EmailSubmissionQuery);
        EmailSubmissionQueryResponse decoded = Codec.DecodeSubmissionQueryResponse(arguments);
        ValidateQueryResponse(decoded, request);
        return decoded;
    }

    internal EmailSubmissionQueryChangesResponse MapQueryChangesResponse(
        JmapResponse response,
        string callId,
        EmailSubmissionQueryChangesRequest request)
    {
        JmapMethodError? error = ValidateQueryChangesRequest(request, 64);
        if (error != null)
        {
            throw ValidationFailure(
                "Valid original EmailSubmission/queryChanges arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(
            response, callId, JmapMethods.EmailSubmissionQueryChanges);
        JsonElement arguments = Responses.RequireSuccessfulArguments(
            result, JmapMethods.EmailSubmissionQueryChanges);
        EmailSubmissionQueryChangesResponse decoded =
            Codec.DecodeSubmissionQueryChangesResponse(arguments);
        ValidateQueryChangesResponse(decoded, request);
        if (ExceedsMaximumQueryChanges(decoded, request))
        {
            throw ProtocolFailure(
                "EmailSubmission/queryChanges returned more changes than maxChanges permits.",
                "methodResponses/1", arguments);
        }

        return decoded;
    }

    internal EmailSubmissionSetResponse MapSetResponse(
        JmapResponse response,
        string callId,
        EmailSubmissionSetRequest request)
    {
        JmapMethodError? error = ValidateSetRequest(request, int.MaxValue);
        if (error != null)
        {
            throw ValidationFailure("Valid original EmailSubmission/set arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(
            response, callId, JmapMethods.EmailSubmissionSet);
        JsonElement arguments = Responses.RequireSuccessfulStateArguments(
            result, JmapMethods.EmailSubmissionSet, request.AccountId, request.IfInState);
        EmailSubmissionSetResponse decoded = Codec.DecodeSubmissionSetResponse(arguments);
        ValidateSetResponse(decoded, request);
        return decoded;
    }

    internal JmapMethodError? ValidateGetRequest(EmailSubmissionGetRequest? request, int maximumObjectsInGet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInGet < 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and get object limit are required.");
        }

        if (request.Ids != null)
        {
            if (request.Ids.Count > maximumObjectsInGet)
            {
                return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                    "The EmailSubmission/get request contains too many identifiers.");
            }

            foreach (string id in request.Ids)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "The ids argument contains an invalid EmailSubmission identifier.");
                }
            }
        }

        if (!ArePropertiesValid(request.Properties))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The properties argument contains an unsupported EmailSubmission property.");
        }

        return null;
    }

    internal JmapMethodError? ValidateChangesRequest(EmailSubmissionChangesRequest? request)
    {
        if (request == null || !Values.IsId(request.AccountId) || request.SinceState == null ||
            !Values.IsValidUnicode(request.SinceState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and sinceState are required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges ||
            request.MaxChanges != 0L && !request.HasMaxChanges ||
            request.HasMaxChanges && !request.IsMaxChangesNull &&
            (request.MaxChanges <= 0L || !Values.IsUnsignedInt(request.MaxChanges)))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxChanges must be null or a positive UnsignedInt with consistent presence flags.");
        }

        return null;
    }

    internal JmapMethodError? ValidateQueryRequest(
        EmailSubmissionQueryRequest? request,
        int maximumFilterDepth)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and filter depth limit are required.");
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

        if (request.CalculateTotal && !request.HasCalculateTotal)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The calculateTotal presence flag is inconsistent.");
        }

        JmapMethodError? filterError = ValidateSubmissionFilter(request.Filter, 1, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return ValidateSubmissionSort(request.Sort);
    }

    internal JmapMethodError? ValidateQueryChangesRequest(
        EmailSubmissionQueryChangesRequest? request,
        int maximumFilterDepth)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId and filter depth limit are required.");
        }

        if (request.SinceQueryState == null || !Values.IsValidUnicode(request.SinceQueryState))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid sinceQueryState is required.");
        }

        if (request.IsMaxChangesNull && !request.HasMaxChanges ||
            request.MaxChanges != 0L && !request.HasMaxChanges ||
            request.HasMaxChanges && !request.IsMaxChangesNull &&
            !Values.IsUnsignedInt(request.MaxChanges))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "maxChanges must be null or an UnsignedInt with consistent presence flags.");
        }

        if (request.UpToId != null && !Values.IsId(request.UpToId))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "upToId must be an Id.");
        }

        if (request.CalculateTotal && !request.HasCalculateTotal)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The calculateTotal presence flag is inconsistent.");
        }

        JmapMethodError? filterError = ValidateSubmissionFilter(request.Filter, 1, maximumFilterDepth);
        if (filterError != null)
        {
            return filterError;
        }

        return ValidateSubmissionSort(request.Sort);
    }

    internal JmapMethodError? ValidateSubmissionFilter(
        EmailSubmissionFilter? filter,
        int depth,
        int maximumDepth)
    {
        FilterTraversalBudget budget = new FilterTraversalBudget();
        return ValidateSubmissionFilterNode(filter, depth, maximumDepth, budget);
    }

    internal JmapMethodError? ValidateSubmissionFilterNode(
        EmailSubmissionFilter? filter,
        int depth,
        int maximumDepth,
        FilterTraversalBudget budget)
    {
        if (filter == null)
        {
            return null;
        }

        budget.Nodes = budget.Nodes + 1;
        if (budget.Nodes > SecurityLimits.MaximumFilterNodes)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter,
                JmapMethodErrorKind.UnsupportedFilter,
                "The EmailSubmission filter contains too many conditions.");
        }

        if (depth > maximumDepth)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                "The EmailSubmission filter exceeds the supported recursion depth.");
        }

        if (filter.UnknownProperties == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission filter contains invalid extension data.");
        }

        if (filter.UnknownProperties.Count != 0)
        {
            return MethodError(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter,
                "The EmailSubmission filter contains an unsupported property.");
        }

        if (filter.HasOperator)
        {
            if (!IsFilterOperator(filter.Operator) || filter.Conditions == null ||
                HasFilterConditionValues(filter))
            {
                return MethodError(JmapErrorCodes.InvalidArguments,
                    JmapMethodErrorKind.InvalidArguments,
                    "The EmailSubmission filter operator is malformed.");
            }

            if (filter.Operator == JmapFilterOperator.Not && filter.Conditions.Count != 1)
            {
                return MethodError(JmapErrorCodes.InvalidArguments,
                    JmapMethodErrorKind.InvalidArguments,
                    "A NOT EmailSubmission filter must contain exactly one condition.");
            }

            foreach (EmailSubmissionFilter condition in filter.Conditions)
            {
                if (condition == null)
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "An EmailSubmission filter operator contains an invalid condition.");
                }

                JmapMethodError? childError =
                    ValidateSubmissionFilterNode(
                        condition, depth + 1, maximumDepth, budget);
                if (childError != null)
                {
                    return childError;
                }
            }

            return null;
        }

        if (filter.Conditions != null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "An EmailSubmission filter condition cannot contain nested conditions.");
        }

        if (!AreIdFilterValuesValid(filter.IdentityIds) || !AreIdFilterValuesValid(filter.EmailIds) ||
            !AreIdFilterValuesValid(filter.ThreadIds))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "EmailSubmission identifier filters must contain only Id values.");
        }

        if (filter.HasUndoStatus && !IsUndoStatus(filter.UndoStatus) ||
            !filter.HasUndoStatus && filter.UndoStatus != JmapSubmissionUndoStatus.Pending)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The undoStatus filter is invalid or has inconsistent presence metadata.");
        }

        if (filter.Before != null && !Values.IsUtcDate(filter.Before) ||
            filter.After != null && !Values.IsUtcDate(filter.After))
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "before and after must be UTCDate values.");
        }

        return null;
    }

    internal bool HasFilterConditionValues(EmailSubmissionFilter filter)
    {
        return filter.IdentityIds != null || filter.EmailIds != null || filter.ThreadIds != null ||
            filter.HasUndoStatus || filter.UndoStatus != JmapSubmissionUndoStatus.Pending ||
            filter.Before != null || filter.After != null;
    }

    internal bool AreIdFilterValuesValid(List<string>? ids)
    {
        if (ids == null)
        {
            return true;
        }

        foreach (string id in ids)
        {
            if (!Values.IsId(id))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsFilterOperator(JmapFilterOperator value)
    {
        return value == JmapFilterOperator.And || value == JmapFilterOperator.Or ||
            value == JmapFilterOperator.Not;
    }

    internal JmapMethodError? ValidateSubmissionSort(List<EmailSubmissionComparator>? sort)
    {
        if (sort == null)
        {
            return null;
        }

        foreach (EmailSubmissionComparator comparator in sort)
        {
            if (comparator == null || comparator.Property == null || comparator.Property.Length == 0 ||
                !Values.IsValidUnicode(comparator.Property) || comparator.UnknownProperties == null ||
                comparator.IsAscending && !comparator.HasIsAscending)
            {
                return MethodError(JmapErrorCodes.InvalidArguments,
                    JmapMethodErrorKind.InvalidArguments,
                    "An EmailSubmission sort comparator is invalid.");
            }

            if (comparator.UnknownProperties.Count != 0 || comparator.Collation != null ||
                comparator.Property != JmapProperties.EmailId &&
                comparator.Property != JmapProperties.ThreadId &&
                comparator.Property != JmapProperties.SentAt)
            {
                return MethodError(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort,
                    "The EmailSubmission sort comparator is not supported.");
            }
        }

        return null;
    }

    internal JmapMethodError? ValidateSetRequest(
        EmailSubmissionSetRequest? request,
        int maximumObjectsInSet)
    {
        if (request == null || !Values.IsId(request.AccountId) || maximumObjectsInSet < 0 ||
            request.IfInState != null && !Values.IsValidUnicode(request.IfInState) ||
            request.UnknownProperties == null)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The EmailSubmission/set account, state, or object limit is invalid.");
        }

        if (CountSetActions(request) > maximumObjectsInSet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The EmailSubmission/set request contains too many object actions.");
        }

        if (request.Create != null)
        {
            foreach (string creationId in request.Create.Keys)
            {
                if (!Values.IsCreationId(creationId))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "The create map contains an invalid creation identifier.");
                }
            }
        }

        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (!Values.IsId(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "The update map contains an invalid EmailSubmission identifier.");
                }
            }
        }

        if (request.Destroy != null)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Destroy)
            {
                if (!Values.IsId(id) || !seen.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "The destroy list contains an invalid or duplicate EmailSubmission identifier.");
                }
            }
        }

        if (request.OnSuccessUpdateEmail != null)
        {
            foreach (KeyValuePair<string, JmapPatchObject> item in request.OnSuccessUpdateEmail)
            {
                if ((!Values.IsId(item.Key) && !Values.IsCreationIdReference(item.Key)) ||
                    !SetValues.IsPatchStructureValid(item.Value))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "onSuccessUpdateEmail contains an invalid submission reference or patch.");
                }
            }
        }

        if (request.OnSuccessDestroyEmail != null)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.OnSuccessDestroyEmail)
            {
                if ((!Values.IsId(id) && !Values.IsCreationIdReference(id)) || !seen.Add(id))
                {
                    return MethodError(JmapErrorCodes.InvalidArguments,
                        JmapMethodErrorKind.InvalidArguments,
                        "onSuccessDestroyEmail contains an invalid or duplicate submission reference.");
                }
            }
        }

        return null;
    }

    internal EmailSubmissionGetRequest SetSnapshotRequest(EmailSubmissionSetRequest request)
    {
        EmailSubmissionGetRequest result = new EmailSubmissionGetRequest();
        result.AccountId = request.AccountId;
        result.Ids = new List<string>();
        result.Properties = new List<string>
        {
            JmapProperties.Id, JmapProperties.EmailId, JmapProperties.UndoStatus
        };
        HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
        if (request.Update != null)
        {
            foreach (string id in request.Update.Keys)
            {
                if (added.Add(id))
                {
                    result.Ids.Add(id);
                }
            }
        }

        if (request.Destroy != null)
        {
            foreach (string id in request.Destroy)
            {
                if (added.Add(id))
                {
                    result.Ids.Add(id);
                }
            }
        }

        return result;
    }

    internal EmailSubmissionCreateContext LoadSubmissionCreateContext(
        EmailSubmissionSetRequest request,
        IEmailService emailService,
        IIdentityService identityService,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        EmailSubmissionCreateContext result = new EmailSubmissionCreateContext();
        if (request.Create == null)
        {
            return result;
        }

        EmailGetRequest emailRequest = new EmailGetRequest();
        emailRequest.AccountId = request.AccountId;
        emailRequest.Ids = new List<string>();
        emailRequest.Properties = new List<string>
        {
            JmapProperties.Id, JmapProperties.Headers, JmapProperties.Sender,
            JmapProperties.From, JmapProperties.To, JmapProperties.Cc, JmapProperties.Bcc
        };
        IdentityGetRequest identityRequest = new IdentityGetRequest();
        identityRequest.AccountId = request.AccountId;
        identityRequest.Ids = new List<string>();
        identityRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.Email };
        HashSet<string> emailIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> identityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (EmailSubmission submission in request.Create.Values)
        {
            if (submission != null && Values.IsId(submission.EmailId) &&
                submission.EmailId != null && emailIds.Add(submission.EmailId))
            {
                emailRequest.Ids.Add(submission.EmailId);
            }

            if (submission != null && Values.IsId(submission.IdentityId) &&
                submission.IdentityId != null && identityIds.Add(submission.IdentityId))
            {
                identityRequest.Ids.Add(submission.IdentityId);
            }
        }

        if (emailRequest.Ids.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EmailGetResponse storedEmails = emailService
                .GetStoredEmailsAsync(emailRequest, context, cancellationToken).GetAwaiter().GetResult();
            EmailGetResponse emails = Emails.NormalizeServerGetResponse(storedEmails, emailRequest);
            result.Emails = Emails.IndexEmails(emails.List);
            result.MissingEmails = new HashSet<string>(emails.NotFound, StringComparer.Ordinal);
        }

        if (identityRequest.Ids.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IdentityGetResponse storedIdentities = identityService
                .GetStoredIdentitiesAsync(identityRequest, context, cancellationToken).GetAwaiter().GetResult();
            IdentityGetResponse identities = Identities.NormalizeGetResponse(storedIdentities, identityRequest);
            result.Identities = Identities.IndexIdentities(identities.List, identityRequest.Properties);
            result.MissingIdentities = new HashSet<string>(identities.NotFound, StringComparer.Ordinal);
        }

        return result;
    }

    internal void RemoveRecipientAddressPaths(List<string> invalid)
    {
        int index = invalid.Count - 1;
        while (index >= 0)
        {
            string path = invalid[index];
            if (path == "envelope/rcptTo" ||
                path.StartsWith("envelope/rcptTo/", StringComparison.Ordinal) &&
                path.EndsWith("/email", StringComparison.Ordinal))
            {
                invalid.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal JmapSetError? ValidateSubmissionCreatePolicy(
        EmailSubmission submission,
        EmailSubmissionCreateContext context)
    {
        List<string> missing = new List<string>();
        if (submission.EmailId != null && Values.IsId(submission.EmailId) &&
            (context.MissingEmails.Contains(submission.EmailId) ||
                !context.Emails.ContainsKey(submission.EmailId)))
        {
            missing.Add(JmapProperties.EmailId);
        }

        if (submission.IdentityId != null && Values.IsId(submission.IdentityId) &&
            (context.MissingIdentities.Contains(submission.IdentityId) ||
                !context.Identities.ContainsKey(submission.IdentityId)))
        {
            missing.Add(JmapProperties.IdentityId);
        }

        if (missing.Count != 0)
        {
            return SetError(JmapErrorCodes.InvalidProperties,
                JmapSetErrorKind.InvalidProperties,
                "The referenced Email or Identity was not found.", missing);
        }

        if (submission.EmailId == null || submission.IdentityId == null ||
            !context.Emails.ContainsKey(submission.EmailId) ||
            !context.Identities.ContainsKey(submission.IdentityId))
        {
            return null;
        }

        Email email = context.Emails[submission.EmailId];
        Identity identity = context.Identities[submission.IdentityId];
        List<string> invalidEmail = ValidateSubmissionEmailHeaders(email);
        if (invalidEmail.Count != 0)
        {
            return SetError(JmapErrorCodes.InvalidEmail, JmapSetErrorKind.InvalidEmail,
                "The Email has invalid sender header fields.", invalidEmail);
        }

        if (!IsFromAllowedByIdentity(email, identity))
        {
            return SetError(JmapErrorCodes.ForbiddenFrom, JmapSetErrorKind.ForbiddenFrom,
                "The Email From header is not permitted by the Identity.", null);
        }

        EmailSubmissionEnvelope envelope;
        List<string> derivedInvalid = new List<string>();
        List<string> invalidRecipients;
        if (submission.Envelope == null)
        {
            SubmissionEnvelopeResult derived = Envelopes.DeriveEnvelope(email, identity);
            envelope = derived.Envelope;
            invalidRecipients = InvalidEmailRecipientAddresses(email);
            foreach (string path in derived.InvalidEmailProperties)
            {
                if (!IsRecipientEmailProperty(path))
                {
                    derivedInvalid.Add(path);
                }
            }

            if (derivedInvalid.Count != 0)
            {
                return SetError(JmapErrorCodes.InvalidEmail, JmapSetErrorKind.InvalidEmail,
                    "The Email cannot produce a valid submission envelope.",
                    UniquePaths(derivedInvalid));
            }
        }
        else
        {
            envelope = submission.Envelope;
            invalidRecipients = InvalidRecipientAddresses(envelope);
        }

        if (invalidRecipients.Count != 0)
        {
            JmapSetError error = SetError(JmapErrorCodes.InvalidRecipients,
                JmapSetErrorKind.InvalidRecipients,
                "The submission envelope contains invalid recipients.", null);
            error.InvalidRecipients = invalidRecipients;
            return error;
        }

        if (envelope.RcptTo == null || envelope.RcptTo.Count == 0)
        {
            return SetError(JmapErrorCodes.NoRecipients, JmapSetErrorKind.NoRecipients,
                "The submission envelope has no recipients.", null);
        }

        if (submission.Envelope != null && envelope.MailFrom != null &&
            envelope.MailFrom.Email != null && envelope.MailFrom.Email.Length != 0 &&
            identity.Email != null &&
            !Envelopes.IsAllowedByIdentity(envelope.MailFrom.Email, identity.Email))
        {
            return SetError(JmapErrorCodes.ForbiddenMailFrom,
                JmapSetErrorKind.ForbiddenMailFrom,
                "The envelope From address is not permitted by the Identity.", null);
        }

        return null;
    }

    internal List<string> ValidateSubmissionEmailHeaders(Email email)
    {
        List<string> invalid = new List<string>();
        if (Envelopes.CountHeaders(email.Headers, "From") > 1 || email.From == null ||
            email.From.Count == 0)
        {
            invalid.Add(JmapProperties.From);
        }
        else
        {
            foreach (EmailAddress address in email.From)
            {
                if (address == null || !SubmissionValues.IsSmtpMailbox(address.Email, false))
                {
                    invalid.Add(JmapProperties.From);
                }
            }
        }

        return UniquePaths(invalid);
    }

    internal bool IsFromAllowedByIdentity(Email email, Identity identity)
    {
        if (email.From == null || identity.Email == null ||
            !SubmissionValues.IsSmtpMailbox(identity.Email, false))
        {
            return false;
        }

        foreach (EmailAddress address in email.From)
        {
            if (address == null || address.Email == null ||
                !Envelopes.IsAllowedByIdentity(address.Email, identity.Email))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsRecipientEmailProperty(string path)
    {
        return path.StartsWith(JmapProperties.To + "/", StringComparison.Ordinal) ||
            path.StartsWith(JmapProperties.Cc + "/", StringComparison.Ordinal) ||
            path.StartsWith(JmapProperties.Bcc + "/", StringComparison.Ordinal);
    }

    internal List<string> InvalidRecipientAddresses(EmailSubmissionEnvelope envelope)
    {
        List<string> invalid = new List<string>();
        if (envelope.RcptTo == null)
        {
            return invalid;
        }

        foreach (EmailSubmissionAddress address in envelope.RcptTo)
        {
            if (address == null || !SubmissionValues.IsSmtpMailbox(address.Email, false))
            {
                string value = address == null || address.Email == null ? "" : address.Email;
                if (!invalid.Contains(value))
                {
                    invalid.Add(value);
                }
            }
        }

        return invalid;
    }

    internal List<string> InvalidEmailRecipientAddresses(Email email)
    {
        List<string> invalid = new List<string>();
        AddInvalidEmailRecipientAddresses(email.To, invalid);
        AddInvalidEmailRecipientAddresses(email.Cc, invalid);
        AddInvalidEmailRecipientAddresses(email.Bcc, invalid);
        return invalid;
    }

    internal void AddInvalidEmailRecipientAddresses(
        List<EmailAddress>? addresses,
        List<string> invalid)
    {
        if (addresses == null)
        {
            return;
        }

        foreach (EmailAddress address in addresses)
        {
            if (address == null || !SubmissionValues.IsSmtpMailbox(address.Email, false))
            {
                string value = address == null || address.Email == null ? "" : address.Email;
                if (!invalid.Contains(value))
                {
                    invalid.Add(value);
                }
            }
        }
    }

    internal void ValidateDelayedSend(
        EmailSubmissionEnvelope envelope,
        JmapSubmissionCapabilities capabilities,
        IJmapClock? clock,
        List<string> invalid)
    {
        if (envelope.MailFrom == null || envelope.MailFrom.Parameters == null)
        {
            return;
        }

        int holdCount = 0;
        foreach (KeyValuePair<string, JsonElement> parameter in envelope.MailFrom.Parameters)
        {
            bool holdFor = string.Equals(parameter.Key, "HOLDFOR", StringComparison.OrdinalIgnoreCase);
            bool holdUntil = string.Equals(parameter.Key, "HOLDUNTIL", StringComparison.OrdinalIgnoreCase);
            if (holdFor || holdUntil)
            {
                holdCount = holdCount + 1;
                string path = "envelope/mailFrom/parameters/" + parameter.Key;
                if (parameter.Value.ValueKind != JsonValueKind.String)
                {
                    invalid.Add(path);
                }
                else if (holdFor && !IsAllowedHoldFor(parameter.Value.GetString(), capabilities.MaxDelayedSend))
                {
                    invalid.Add(path);
                }
                else if (holdUntil && !IsAllowedHoldUntil(
                    parameter.Value.GetString(), capabilities.MaxDelayedSend, clock))
                {
                    invalid.Add(path);
                }
            }
        }

        if (holdCount > 1)
        {
            invalid.Add("envelope/mailFrom/parameters");
        }
    }

    internal bool IsAllowedHoldFor(string? value, long maximumDelay)
    {
        long seconds = ParsePositiveDecimal(value);
        return seconds > 0L && seconds <= 999999999L && seconds <= maximumDelay;
    }

    internal long ParsePositiveDecimal(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 9 || value[0] == '0')
        {
            return -1L;
        }

        long result = 0L;
        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return -1L;
            }

            result = result * 10L + character - '0';
        }

        return result;
    }

    internal bool IsAllowedHoldUntil(string? value, long maximumDelay, IJmapClock? clock)
    {
        long release = ParseFutureReleaseDate(value);
        DateTimeOffset current = clock == null ? DateTimeOffset.UtcNow : clock.GetCurrentUtcDateTime();
        long now = current.ToUniversalTime().ToUnixTimeSeconds();
        return release > now && release - now <= maximumDelay;
    }

    internal long ParseFutureReleaseDate(string? value)
    {
        if (value == null || value.Length != 15 || value[14] != 'Z')
        {
            return -1L;
        }

        int year = ParseFixedDigits(value, 0, 4);
        int month = ParseFixedDigits(value, 4, 2);
        int day = ParseFixedDigits(value, 6, 2);
        int hour = ParseFixedDigits(value, 8, 2);
        int minute = ParseFixedDigits(value, 10, 2);
        int second = ParseFixedDigits(value, 12, 2);
        if (year < 1 || month < 1 || month > 12 || hour < 0 || hour > 23 ||
            minute < 0 || minute > 59 || second < 0 || second > 59 ||
            day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return -1L;
        }

        DateTimeOffset parsed = new DateTimeOffset(
            year, month, day, hour, minute, second, TimeSpan.Zero);
        return parsed.ToUnixTimeSeconds();
    }

    internal int ParseFixedDigits(string value, int start, int count)
    {
        int result = 0;
        int index = start;
        while (index < start + count)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            result = result * 10 + character - '0';
            index = index + 1;
        }

        return result;
    }

    internal EmailSubmissionSetPreparation PrepareSetRequest(
        EmailSubmissionSetRequest request,
        JsonElement arguments,
        Dictionary<string, EmailSubmission> submissions,
        List<string> missing,
        string? state,
        JmapSubmissionCapabilities capabilities,
        EmailSubmissionCreateContext createContext,
        IJmapClock? clock)
    {
        EmailSubmissionSetPreparation result = new EmailSubmissionSetPreparation();
        result.Backend.AccountId = request.AccountId;
        result.Backend.IfInState = request.IfInState;
        CopyUnknown(request.UnknownProperties, result.Backend.UnknownProperties);
        result.Local.AccountId = request.AccountId;
        result.Local.OldState = state;
        result.Local.NewState = state;
        HashSet<string> notFound = new HashSet<string>(missing, StringComparer.Ordinal);
        PrepareSetCreates(request, arguments, result, capabilities, createContext, clock);
        PrepareSetUpdates(request, result, submissions, notFound);
        PrepareSetDestroys(request, result, submissions, notFound);
        return result;
    }

    internal void PrepareSetCreates(
        EmailSubmissionSetRequest request,
        JsonElement arguments,
        EmailSubmissionSetPreparation preparation,
        JmapSubmissionCapabilities capabilities,
        EmailSubmissionCreateContext createContext,
        IJmapClock? clock)
    {
        if (request.Create == null)
        {
            return;
        }

        preparation.Backend.Create = new Dictionary<string, EmailSubmission>();
        foreach (KeyValuePair<string, EmailSubmission> item in request.Create)
        {
            List<string> invalid = SubmissionValues.ValidateEmailSubmissionCreate(item.Value);
            AddRawSubmissionCreateInvalidProperties(arguments, item.Key, invalid);
            RemoveRecipientAddressPaths(invalid);
            if (item.Value != null && item.Value.Envelope != null)
            {
                SubmissionValues.ValidateEnvelopeExtensions(
                    item.Value.Envelope, capabilities, invalid);
                ValidateDelayedSend(item.Value.Envelope, capabilities, clock, invalid);
            }

            invalid = UniquePaths(invalid);
            if (invalid.Count != 0)
            {
                AddNotCreated(preparation.Local, item.Key,
                    SetError(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties,
                        "The EmailSubmission has invalid or immutable properties.", invalid));
            }
            else if (item.Value != null)
            {
                JmapSetError? policyError = ValidateSubmissionCreatePolicy(item.Value, createContext);
                if (policyError == null)
                {
                    preparation.Backend.Create.Add(item.Key, item.Value);
                }
                else
                {
                    AddNotCreated(preparation.Local, item.Key, policyError);
                }
            }
        }
    }

    internal void PrepareSetUpdates(
        EmailSubmissionSetRequest request,
        EmailSubmissionSetPreparation preparation,
        Dictionary<string, EmailSubmission> submissions,
        HashSet<string> missing)
    {
        if (request.Update == null)
        {
            return;
        }

        preparation.Backend.Update = new Dictionary<string, JmapPatchObject>();
        HashSet<string> destroyed = request.Destroy == null ?
            new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(request.Destroy, StringComparer.Ordinal);
        foreach (KeyValuePair<string, JmapPatchObject> item in request.Update)
        {
            if (destroyed.Contains(item.Key))
            {
                AddNotUpdated(preparation.Local, item.Key,
                    SetError(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy,
                        "The EmailSubmission will be destroyed by this request.", null));
            }
            else if (missing.Contains(item.Key) || !submissions.ContainsKey(item.Key))
            {
                AddNotUpdated(preparation.Local, item.Key,
                    SetError(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound,
                        "The EmailSubmission was not found.", null));
            }
            else if (!SetValues.IsPatchStructureValid(item.Value))
            {
                AddNotUpdated(preparation.Local, item.Key,
                    SetError(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch,
                        "The EmailSubmission patch is invalid.", null));
            }
            else
            {
                List<string> invalid =
                    SubmissionValues.ValidateEmailSubmissionWritablePatch(item.Value);
                invalid = UniquePaths(invalid);
                if (invalid.Count != 0)
                {
                    AddNotUpdated(preparation.Local, item.Key,
                        SetError(JmapErrorCodes.InvalidProperties,
                            JmapSetErrorKind.InvalidProperties,
                            "Only cancellation may update an EmailSubmission.", invalid));
                }
                else if (!submissions[item.Key].HasUndoStatus ||
                    submissions[item.Key].UndoStatus != JmapSubmissionUndoStatus.Pending)
                {
                    AddNotUpdated(preparation.Local, item.Key,
                        SetError(JmapErrorCodes.CannotUnsend,
                            JmapSetErrorKind.CannotUnsend,
                            "The EmailSubmission can no longer be canceled.", null));
                }
                else
                {
                    preparation.Backend.Update.Add(item.Key, item.Value);
                }
            }
        }
    }

    internal void PrepareSetDestroys(
        EmailSubmissionSetRequest request,
        EmailSubmissionSetPreparation preparation,
        Dictionary<string, EmailSubmission> submissions,
        HashSet<string> missing)
    {
        if (request.Destroy == null)
        {
            return;
        }

        preparation.Backend.Destroy = new List<string>();
        foreach (string id in request.Destroy)
        {
            if (missing.Contains(id) || !submissions.ContainsKey(id))
            {
                AddNotDestroyed(preparation.Local, id,
                    SetError(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound,
                        "The EmailSubmission was not found.", null));
            }
            else
            {
                preparation.Backend.Destroy.Add(id);
            }
        }
    }

    internal void AddRawSubmissionCreateInvalidProperties(
        JsonElement arguments,
        string creationId,
        List<string> invalid)
    {
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement submission = Codec.Values.GetMember(create, creationId);
        if (submission.ValueKind != JsonValueKind.Object)
        {
            invalid.Add("");
            return;
        }

        List<string> immutable = new List<string>
        {
            JmapProperties.Id, JmapProperties.ThreadId, JmapProperties.SendAt,
            JmapProperties.UndoStatus, JmapProperties.DeliveryStatus,
            JmapProperties.DsnBlobIds, JmapProperties.MdnBlobIds
        };
        foreach (string property in immutable)
        {
            if (Codec.Values.GetMember(submission, property).ValueKind != JsonValueKind.Undefined)
            {
                invalid.Add(property);
            }
        }
    }

    internal long CountSetActions(EmailSubmissionSetRequest request)
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

    internal EmailSetRequest? BuildImplicitEmailSetRequest(
        EmailSubmissionSetRequest request,
        EmailSubmissionSetResponse response,
        Dictionary<string, EmailSubmission> submissions)
    {
        Dictionary<string, string> emailIds =
            SuccessfulSubmissionEmailIds(request, response, submissions);
        EmailSetRequest result = new EmailSetRequest();
        result.AccountId = request.AccountId;
        if (request.OnSuccessUpdateEmail != null)
        {
            foreach (KeyValuePair<string, JmapPatchObject> item in request.OnSuccessUpdateEmail)
            {
                if (emailIds.ContainsKey(item.Key))
                {
                    AddImplicitEmailPatch(result, emailIds[item.Key], item.Value);
                }
            }
        }

        if (request.OnSuccessDestroyEmail != null)
        {
            HashSet<string> destroyed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string reference in request.OnSuccessDestroyEmail)
            {
                if (emailIds.ContainsKey(reference) && destroyed.Add(emailIds[reference]))
                {
                    if (result.Destroy == null)
                    {
                        result.Destroy = new List<string>();
                    }

                    result.Destroy.Add(emailIds[reference]);
                }
            }
        }

        if (CountImplicitEmailSetActions(result) == 0L)
        {
            return null;
        }

        return result;
    }

    internal Dictionary<string, string> SuccessfulSubmissionEmailIds(
        EmailSubmissionSetRequest request,
        EmailSubmissionSetResponse response,
        Dictionary<string, EmailSubmission> submissions)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (response.Created != null && request.Create != null)
        {
            foreach (KeyValuePair<string, EmailSubmission> item in response.Created)
            {
                if (request.Create.ContainsKey(item.Key))
                {
                    EmailSubmission created = request.Create[item.Key];
                    string? emailId = created == null ? null : created.EmailId;
                    if (!Values.IsId(emailId))
                    {
                        emailId = item.Value == null ? null : item.Value.EmailId;
                    }

                    if (Values.IsId(emailId) && emailId != null)
                    {
                        result["#" + item.Key] = emailId;
                    }
                }
            }
        }

        if (response.Updated != null)
        {
            foreach (string id in response.Updated.Keys)
            {
                AddStoredSubmissionEmailId(result, id, submissions);
            }
        }

        if (response.Destroyed != null)
        {
            foreach (string id in response.Destroyed)
            {
                AddStoredSubmissionEmailId(result, id, submissions);
            }
        }

        return result;
    }

    internal void AddStoredSubmissionEmailId(
        Dictionary<string, string> destination,
        string id,
        Dictionary<string, EmailSubmission> submissions)
    {
        if (submissions.ContainsKey(id))
        {
            string? emailId = submissions[id].EmailId;
            if (Values.IsId(emailId) && emailId != null)
            {
                destination[id] = emailId;
            }
        }
    }

    internal void AddImplicitEmailPatch(
        EmailSetRequest request,
        string emailId,
        JmapPatchObject source)
    {
        if (request.Update == null)
        {
            request.Update = new Dictionary<string, JmapPatchObject>();
        }

        if (!request.Update.ContainsKey(emailId))
        {
            request.Update[emailId] = new JmapPatchObject();
        }

        JmapPatchObject destination = request.Update[emailId];
        foreach (KeyValuePair<string, JsonElement> item in source.Values)
        {
            destination.Values[item.Key] = item.Value.Clone();
        }
    }

    internal long CountImplicitEmailSetActions(EmailSetRequest request)
    {
        long count = request.Update == null ? 0L : request.Update.Count;
        if (request.Destroy != null)
        {
            count = count + request.Destroy.Count;
        }

        return count;
    }

    internal EmailSubmissionGetRequest CompleteGetRequest(EmailSubmissionGetRequest request)
    {
        EmailSubmissionGetRequest result = new EmailSubmissionGetRequest();
        result.AccountId = request.AccountId;
        if (request.Ids != null)
        {
            result.Ids = new List<string>(request.Ids);
        }

        if (request.Properties != null)
        {
            result.Properties = EffectiveProperties(request.Properties);
        }

        CopyUnknown(request.UnknownProperties, result.UnknownProperties);
        return result;
    }

    internal EmailSubmissionGetResponse NormalizeGetResponse(
        EmailSubmissionGetResponse? stored,
        EmailSubmissionGetRequest request)
    {
        ValidateGetEnvelope(stored, request);
        if (stored == null)
        {
            throw ProtocolFailure("An EmailSubmission/get response is required.", "methodResponses/1",
                default(JsonElement));
        }

        Dictionary<string, EmailSubmission> records = IndexSubmissions(stored.List, request.Properties);
        ValidateGetPartition(stored, request, records);
        EmailSubmissionGetResponse result = new EmailSubmissionGetResponse();
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

    internal void ValidateClientGetResponse(
        EmailSubmissionGetResponse? response,
        EmailSubmissionGetRequest request)
    {
        ValidateGetEnvelope(response, request);
        if (response == null)
        {
            throw ProtocolFailure("An EmailSubmission/get response is required.", "methodResponses/1",
                default(JsonElement));
        }

        Dictionary<string, EmailSubmission> records = IndexSubmissions(response.List, request.Properties);
        ValidateGetPartition(response, request, records);
    }

    internal void ValidateGetEnvelope(
        EmailSubmissionGetResponse? response,
        EmailSubmissionGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.State == null ||
            !Values.IsValidUnicode(response.State) || response.List == null || response.NotFound == null ||
            response.UnknownProperties == null)
        {
            throw ProtocolFailure(
                "The EmailSubmission/get response has invalid account, state, or collection metadata.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal Dictionary<string, EmailSubmission> IndexSubmissions(
        List<EmailSubmission> submissions,
        List<string>? properties)
    {
        Dictionary<string, EmailSubmission> result =
            new Dictionary<string, EmailSubmission>(StringComparer.Ordinal);
        int index = 0;
        foreach (EmailSubmission submission in submissions)
        {
            List<string> invalid = ValidateProjectedSubmission(submission, properties);
            if (submission == null || submission.Id == null || invalid.Count != 0 ||
                result.ContainsKey(submission.Id))
            {
                throw ProtocolFailure("EmailSubmission/get returned an invalid or duplicate submission.",
                    JmapProperties.List + "/" + index, default(JsonElement));
            }

            result.Add(submission.Id, submission);
            index = index + 1;
        }

        return result;
    }

    internal List<string> ValidateProjectedSubmission(
        EmailSubmission? submission,
        List<string>? properties)
    {
        List<string> invalid = new List<string>();
        if (submission == null || !Values.IsId(submission.Id))
        {
            invalid.Add(JmapProperties.Id);
            return invalid;
        }

        HashSet<string> included =
            new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        if (included.Contains(JmapProperties.IdentityId) && !Values.IsId(submission.IdentityId))
        {
            invalid.Add(JmapProperties.IdentityId);
        }

        if (included.Contains(JmapProperties.EmailId) && !Values.IsId(submission.EmailId))
        {
            invalid.Add(JmapProperties.EmailId);
        }

        if (included.Contains(JmapProperties.ThreadId) && !Values.IsId(submission.ThreadId))
        {
            invalid.Add(JmapProperties.ThreadId);
        }

        if (included.Contains(JmapProperties.Envelope) && submission.Envelope != null)
        {
            ValidateStoredEnvelope(submission.Envelope, invalid);
        }

        if (included.Contains(JmapProperties.SendAt) && !Values.IsUtcDate(submission.SendAt))
        {
            invalid.Add(JmapProperties.SendAt);
        }

        if (included.Contains(JmapProperties.UndoStatus) &&
            (!submission.HasUndoStatus || !IsUndoStatus(submission.UndoStatus)))
        {
            invalid.Add(JmapProperties.UndoStatus);
        }

        if (included.Contains(JmapProperties.DeliveryStatus) && submission.DeliveryStatus != null)
        {
            ValidateDeliveryStatuses(submission.DeliveryStatus, invalid);
        }

        if (included.Contains(JmapProperties.DsnBlobIds))
        {
            ValidateBlobIds(submission.DsnBlobIds, JmapProperties.DsnBlobIds, invalid);
        }

        if (included.Contains(JmapProperties.MdnBlobIds))
        {
            ValidateBlobIds(submission.MdnBlobIds, JmapProperties.MdnBlobIds, invalid);
        }

        if (submission.UnknownProperties == null)
        {
            invalid.Add("unknownProperties");
        }

        return UniquePaths(invalid);
    }

    internal void ValidateStoredEnvelope(EmailSubmissionEnvelope envelope, List<string> invalid)
    {
        List<string> envelopeInvalid = new List<string>();
        SubmissionValues.ValidateEnvelope(envelope, envelopeInvalid);
        foreach (string path in envelopeInvalid)
        {
            invalid.Add(path);
        }

        if (envelope.UnknownProperties == null)
        {
            invalid.Add(JmapProperties.Envelope);
        }

        if (envelope.MailFrom != null && envelope.MailFrom.UnknownProperties == null)
        {
            invalid.Add(JmapProperties.Envelope + "/" + JmapProperties.MailFrom);
        }

        if (envelope.RcptTo == null)
        {
            return;
        }

        int index = 0;
        foreach (EmailSubmissionAddress address in envelope.RcptTo)
        {
            if (address != null && address.UnknownProperties == null)
            {
                invalid.Add(JmapProperties.Envelope + "/" + JmapProperties.RcptTo + "/" + index);
            }

            index = index + 1;
        }
    }

    internal void ValidateDeliveryStatuses(
        Dictionary<string, EmailDeliveryStatus> statuses,
        List<string> invalid)
    {
        foreach (KeyValuePair<string, EmailDeliveryStatus> item in statuses)
        {
            string path = JmapProperties.DeliveryStatus + "/" + item.Key;
            EmailDeliveryStatus status = item.Value;
            if (!SubmissionValues.IsSmtpMailbox(item.Key, false) || status == null ||
                status.SmtpReply == null || status.SmtpReply.Length == 0 ||
                !Values.IsValidUnicode(status.SmtpReply) || status.Delivered == null ||
                status.Delivered.Length == 0 || !Values.IsValidUnicode(status.Delivered) ||
                status.Displayed == null || status.Displayed.Length == 0 ||
                !Values.IsValidUnicode(status.Displayed) || status.UnknownProperties == null)
            {
                invalid.Add(path);
            }
        }
    }

    internal void ValidateBlobIds(List<string>? ids, string path, List<string> invalid)
    {
        if (ids == null)
        {
            invalid.Add(path);
            return;
        }

        int index = 0;
        foreach (string id in ids)
        {
            if (!Values.IsId(id))
            {
                invalid.Add(path + "/" + index);
            }

            index = index + 1;
        }
    }

    internal bool IsUndoStatus(JmapSubmissionUndoStatus value)
    {
        return value == JmapSubmissionUndoStatus.Pending || value == JmapSubmissionUndoStatus.Final ||
            value == JmapSubmissionUndoStatus.Canceled;
    }

    internal void ValidateGetPartition(
        EmailSubmissionGetResponse response,
        EmailSubmissionGetRequest request,
        Dictionary<string, EmailSubmission> records)
    {
        HashSet<string> notFound = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in response.NotFound)
        {
            if (!Values.IsId(id) || !notFound.Add(id) || records.ContainsKey(id))
            {
                throw ProtocolFailure("The EmailSubmission/get notFound partition is invalid.",
                    JmapProperties.NotFound, default(JsonElement));
            }
        }

        if (request.Ids == null)
        {
            if (notFound.Count != 0)
            {
                throw ProtocolFailure(
                    "EmailSubmission/get cannot return notFound when all submissions were requested.",
                    JmapProperties.NotFound, default(JsonElement));
            }

            return;
        }

        HashSet<string> requested = new HashSet<string>(request.Ids, StringComparer.Ordinal);
        foreach (string id in records.Keys)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("EmailSubmission/get returned an unrequested submission.",
                    JmapProperties.List, default(JsonElement));
            }
        }

        foreach (string id in notFound)
        {
            if (!requested.Contains(id))
            {
                throw ProtocolFailure("EmailSubmission/get returned an unrequested notFound identifier.",
                    JmapProperties.NotFound, default(JsonElement));
            }
        }

        if (records.Count + notFound.Count != requested.Count)
        {
            throw ProtocolFailure("EmailSubmission/get did not classify every requested identifier.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangesResponse(
        EmailSubmissionChangesResponse? response,
        EmailSubmissionChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId ||
            response.OldState != request.SinceState || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) || response.Created == null ||
            response.Updated == null || response.Destroyed == null || response.UnknownProperties == null)
        {
            throw ProtocolFailure(
                "The EmailSubmission/changes response has invalid account, state, or collections.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> classified = new HashSet<string>(StringComparer.Ordinal);
        ValidateChangeIds(response.Created, classified, JmapProperties.Created);
        ValidateChangeIds(response.Updated, classified, JmapProperties.Updated);
        ValidateChangeIds(response.Destroyed, classified, JmapProperties.Destroyed);
        long count = (long)response.Created.Count + response.Updated.Count + response.Destroyed.Count;
        if (request.HasMaxChanges && !request.IsMaxChangesNull && count > request.MaxChanges)
        {
            throw ProtocolFailure(
                "EmailSubmission/changes returned more identifiers than maxChanges permits.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal void ValidateChangeIds(List<string> ids, HashSet<string> classified, string path)
    {
        foreach (string id in ids)
        {
            if (!Values.IsId(id) || !classified.Add(id))
            {
                throw ProtocolFailure(
                    "An EmailSubmission change identifier is invalid or multiply classified.", path,
                    default(JsonElement));
            }
        }
    }

    internal void ValidateQueryResponse(
        EmailSubmissionQueryResponse? response,
        EmailSubmissionQueryRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.QueryState == null ||
            !Values.IsValidUnicode(response.QueryState) || !Values.IsUnsignedInt(response.Position) ||
            response.Ids == null || response.UnknownProperties == null)
        {
            throw ProtocolFailure(
                "The EmailSubmission/query response has invalid account, state, position, or results.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (string id in response.Ids)
        {
            if (!Values.IsId(id) || !ids.Add(id))
            {
                throw ProtocolFailure(
                    "The EmailSubmission/query result contains an invalid or duplicate identifier.",
                    JmapProperties.Ids + "/" + index, default(JsonElement));
            }

            index = index + 1;
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested ||
            response.HasTotal && !Values.IsUnsignedInt(response.Total))
        {
            throw ProtocolFailure("The EmailSubmission/query total metadata does not match the request.",
                JmapProperties.Total, default(JsonElement));
        }

        if (response.HasTotal && response.Ids.Count != 0 &&
            (response.Position > response.Total ||
                response.Ids.Count > response.Total - response.Position))
        {
            throw ProtocolFailure("The EmailSubmission/query window exceeds the reported total.",
                JmapProperties.Ids, default(JsonElement));
        }

        if (response.HasLimit)
        {
            if (!Values.IsUnsignedInt(response.Limit) || response.Ids.Count > response.Limit)
            {
                throw ProtocolFailure("The EmailSubmission/query enforced limit is invalid.",
                    JmapProperties.Limit, default(JsonElement));
            }

            if (request.HasLimit && !request.IsLimitNull && response.Limit >= request.Limit)
            {
                throw ProtocolFailure(
                    "The EmailSubmission/query response limit is not lower than the requested limit.",
                    JmapProperties.Limit, default(JsonElement));
            }
        }

        if (request.HasLimit && !request.IsLimitNull && response.Ids.Count > request.Limit)
        {
            throw ProtocolFailure("The EmailSubmission/query result exceeds the requested limit.",
                JmapProperties.Ids, default(JsonElement));
        }

        if (request.Anchor == null && (!request.HasPosition || request.Position >= 0L))
        {
            long expectedPosition = request.HasPosition ? request.Position : 0L;
            if (response.Position != expectedPosition)
            {
                throw ProtocolFailure(
                    "The EmailSubmission/query response position does not match the request.",
                    JmapProperties.Position, default(JsonElement));
            }
        }
    }

    internal void ValidateQueryChangesResponse(
        EmailSubmissionQueryChangesResponse? response,
        EmailSubmissionQueryChangesRequest request)
    {
        if (response == null || response.AccountId != request.AccountId ||
            response.OldQueryState != request.SinceQueryState || response.NewQueryState == null ||
            !Values.IsValidUnicode(response.NewQueryState) || response.Removed == null ||
            response.Added == null || response.UnknownProperties == null)
        {
            throw ProtocolFailure(
                "The EmailSubmission/queryChanges response has invalid account, state, or results.",
                "methodResponses/1", default(JsonElement));
        }

        bool totalRequested = request.HasCalculateTotal && request.CalculateTotal;
        if (response.HasTotal != totalRequested ||
            response.HasTotal && !Values.IsUnsignedInt(response.Total))
        {
            throw ProtocolFailure(
                "The EmailSubmission/queryChanges total metadata does not match the request.",
                JmapProperties.Total, default(JsonElement));
        }

        HashSet<string> removed = new HashSet<string>(StringComparer.Ordinal);
        int removedIndex = 0;
        foreach (string id in response.Removed)
        {
            if (!Values.IsId(id) || !removed.Add(id))
            {
                throw ProtocolFailure(
                    "The EmailSubmission/queryChanges removed list contains an invalid or duplicate id.",
                    JmapProperties.Removed + "/" + removedIndex, default(JsonElement));
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
                throw ProtocolFailure(
                    "The EmailSubmission/queryChanges added list is invalid or not index ordered.",
                    JmapProperties.Added + "/" + addedIndex, default(JsonElement));
            }

            if (response.HasTotal && item.Index >= response.Total)
            {
                throw ProtocolFailure(
                    "An EmailSubmission/queryChanges added index exceeds the reported total.",
                    JmapProperties.Added + "/" + addedIndex + "/" + JmapProperties.Index,
                    default(JsonElement));
            }

            previousIndex = item.Index;
            hasPreviousIndex = true;
            addedIndex = addedIndex + 1;
        }
    }

    internal bool ExceedsMaximumQueryChanges(
        EmailSubmissionQueryChangesResponse response,
        EmailSubmissionQueryChangesRequest request)
    {
        if (!request.HasMaxChanges || request.IsMaxChangesNull)
        {
            return false;
        }

        long count = (long)response.Removed.Count + response.Added.Count;
        return count > request.MaxChanges;
    }

    internal void ValidateSetResponse(
        EmailSubmissionSetResponse? response,
        EmailSubmissionSetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.NewState == null ||
            !Values.IsValidUnicode(response.NewState) ||
            response.OldState != null && !Values.IsValidUnicode(response.OldState) ||
            request.IfInState != null && response.OldState != request.IfInState ||
            response.UnknownProperties == null)
        {
            throw ProtocolFailure("The EmailSubmission/set response has invalid account or state metadata.",
                "methodResponses/1", default(JsonElement));
        }

        ValidateSetCreatePartitions(response, request.Create);
        ValidateSetUpdatePartitions(response, request.Update);
        ValidateSetDestroyPartitions(response, request.Destroy);
    }

    internal void ValidateSetCreatePartitions(
        EmailSubmissionSetResponse response,
        Dictionary<string, EmailSubmission>? requested)
    {
        HashSet<string> expected = requested == null ? new HashSet<string>(StringComparer.Ordinal) :
            new HashSet<string>(requested.Keys, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (response.Created != null)
        {
            foreach (KeyValuePair<string, EmailSubmission> item in response.Created)
            {
                if (!expected.Contains(item.Key) || !seen.Add(item.Key) ||
                    !IsValidCreatedSubmission(item.Value))
                {
                    throw ProtocolFailure("The EmailSubmission/set created map is invalid.",
                        JmapProperties.Created, default(JsonElement));
                }
            }
        }

        if (response.NotCreated != null)
        {
            ValidateSetErrors(response.NotCreated, expected, seen, JmapProperties.NotCreated);
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("EmailSubmission/set did not classify every create action.",
                JmapProperties.Created, default(JsonElement));
        }
    }

    internal void ValidateSetUpdatePartitions(
        EmailSubmissionSetResponse response,
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
                    !IsValidUpdatedSubmissionValue(item.Value))
                {
                    throw ProtocolFailure("The EmailSubmission/set updated map is invalid.",
                        JmapProperties.Updated, default(JsonElement));
                }
            }
        }

        if (response.NotUpdated != null)
        {
            ValidateSetErrors(response.NotUpdated, expected, seen, JmapProperties.NotUpdated);
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("EmailSubmission/set did not classify every update action.",
                JmapProperties.Updated, default(JsonElement));
        }
    }

    internal void ValidateSetDestroyPartitions(
        EmailSubmissionSetResponse response,
        List<string>? requested)
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
                    throw ProtocolFailure("The EmailSubmission/set destroyed list is invalid.",
                        JmapProperties.Destroyed, default(JsonElement));
                }
            }
        }

        if (response.NotDestroyed != null)
        {
            ValidateSetErrors(response.NotDestroyed, expected, seen, JmapProperties.NotDestroyed);
        }

        if (seen.Count != expected.Count)
        {
            throw ProtocolFailure("EmailSubmission/set did not classify every destroy action.",
                JmapProperties.Destroyed, default(JsonElement));
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
                throw ProtocolFailure("An EmailSubmission/set error partition is invalid.", path,
                    default(JsonElement));
            }

            JmapSetError normalized = Errors.NormalizeSetError(item.Value);
            if (!IsAllowedSubmissionSetError(normalized.Kind, path))
            {
                throw ProtocolFailure("The SetError type is not valid for this EmailSubmission action.",
                    path + "/" + item.Key + "/" + JmapProperties.Type,
                    default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.InvalidProperties &&
                normalized.Properties == null)
            {
                throw ProtocolFailure("An invalidProperties error must identify invalid properties.",
                    path + "/" + item.Key + "/" + JmapProperties.Properties,
                    default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.TooLarge && !normalized.HasMaxSize)
            {
                throw ProtocolFailure("A tooLarge submission error must identify maxSize.",
                    path + "/" + item.Key + "/" + JmapProperties.MaxSize,
                    default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.TooManyRecipients &&
                !normalized.HasMaxRecipients)
            {
                throw ProtocolFailure("A tooManyRecipients error must identify maxRecipients.",
                    path + "/" + item.Key + "/" + JmapProperties.MaxRecipients,
                    default(JsonElement));
            }

            if (normalized.Kind == JmapSetErrorKind.InvalidRecipients &&
                (normalized.InvalidRecipients == null || normalized.InvalidRecipients.Count == 0))
            {
                throw ProtocolFailure("An invalidRecipients error must identify invalid addresses.",
                    path + "/" + item.Key + "/" + JmapProperties.InvalidRecipients,
                    default(JsonElement));
            }

            item.Value.Kind = normalized.Kind;
        }
    }

    internal bool IsAllowedSubmissionSetError(JmapSetErrorKind kind, string path)
    {
        if (kind == JmapSetErrorKind.Unknown || kind == JmapSetErrorKind.Forbidden)
        {
            return true;
        }

        if (path == JmapProperties.NotCreated)
        {
            return kind == JmapSetErrorKind.OverQuota || kind == JmapSetErrorKind.TooLarge ||
                kind == JmapSetErrorKind.RateLimit || kind == JmapSetErrorKind.InvalidProperties ||
                kind == JmapSetErrorKind.InvalidEmail || kind == JmapSetErrorKind.TooManyRecipients ||
                kind == JmapSetErrorKind.NoRecipients || kind == JmapSetErrorKind.InvalidRecipients ||
                kind == JmapSetErrorKind.ForbiddenMailFrom || kind == JmapSetErrorKind.ForbiddenFrom ||
                kind == JmapSetErrorKind.ForbiddenToSend;
        }

        if (path == JmapProperties.NotUpdated)
        {
            return kind == JmapSetErrorKind.NotFound || kind == JmapSetErrorKind.InvalidPatch ||
                kind == JmapSetErrorKind.WillDestroy || kind == JmapSetErrorKind.InvalidProperties ||
                kind == JmapSetErrorKind.CannotUnsend;
        }

        return kind == JmapSetErrorKind.NotFound;
    }

    internal bool IsValidCreatedSubmission(EmailSubmission? submission)
    {
        if (submission == null || !Values.IsId(submission.Id) ||
            submission.UnknownProperties == null)
        {
            return false;
        }

        if (submission.IdentityId != null && !Values.IsId(submission.IdentityId) ||
            submission.EmailId != null && !Values.IsId(submission.EmailId) ||
            submission.ThreadId != null && !Values.IsId(submission.ThreadId) ||
            submission.SendAt != null && !Values.IsUtcDate(submission.SendAt) ||
            submission.HasUndoStatus && submission.UndoStatus != JmapSubmissionUndoStatus.Pending &&
                submission.UndoStatus != JmapSubmissionUndoStatus.Final ||
            !submission.HasUndoStatus && submission.UndoStatus != JmapSubmissionUndoStatus.Pending)
        {
            return false;
        }

        List<string> invalid = new List<string>();
        if (submission.Envelope != null)
        {
            ValidateStoredEnvelope(submission.Envelope, invalid);
        }

        if (submission.DeliveryStatus != null)
        {
            ValidateDeliveryStatuses(submission.DeliveryStatus, invalid);
        }

        if (submission.DsnBlobIds != null)
        {
            ValidateBlobIds(submission.DsnBlobIds, JmapProperties.DsnBlobIds, invalid);
        }

        if (submission.MdnBlobIds != null)
        {
            ValidateBlobIds(submission.MdnBlobIds, JmapProperties.MdnBlobIds, invalid);
        }

        return invalid.Count == 0;
    }

    internal bool IsValidUpdatedSubmissionValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement undo = Codec.Values.GetMember(value, JmapProperties.UndoStatus);
        return undo.ValueKind == JsonValueKind.Undefined ||
            undo.ValueKind == JsonValueKind.String && undo.GetString() == "canceled";
    }

    internal void MergeSetFailures(
        EmailSubmissionSetResponse target,
        EmailSubmissionSetResponse source)
    {
        MergeSetErrorMap(target, source.NotCreated, "create");
        MergeSetErrorMap(target, source.NotUpdated, "update");
        MergeSetErrorMap(target, source.NotDestroyed, "destroy");
    }

    internal void MergeSetErrorMap(
        EmailSubmissionSetResponse target,
        Dictionary<string, JmapSetError>? source,
        string action)
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

    internal void AddNotCreated(
        EmailSubmissionSetResponse response,
        string creationId,
        JmapSetError error)
    {
        if (response.NotCreated == null)
        {
            response.NotCreated = new Dictionary<string, JmapSetError>();
        }

        response.NotCreated.Add(creationId, error);
    }

    internal void AddNotUpdated(
        EmailSubmissionSetResponse response,
        string id,
        JmapSetError error)
    {
        if (response.NotUpdated == null)
        {
            response.NotUpdated = new Dictionary<string, JmapSetError>();
        }

        response.NotUpdated.Add(id, error);
    }

    internal void AddNotDestroyed(
        EmailSubmissionSetResponse response,
        string id,
        JmapSetError error)
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

    internal List<string> EffectiveProperties(List<string>? properties)
    {
        List<string> source = properties == null ? SubmissionProperties() : properties;
        List<string> result = new List<string> { JmapProperties.Id };
        foreach (string property in source)
        {
            if (!result.Contains(property))
            {
                result.Add(property);
            }
        }

        return result;
    }

    internal List<string> SubmissionProperties()
    {
        return new List<string>
        {
            JmapProperties.Id, JmapProperties.IdentityId, JmapProperties.EmailId, JmapProperties.ThreadId,
            JmapProperties.Envelope, JmapProperties.SendAt, JmapProperties.UndoStatus,
            JmapProperties.DeliveryStatus, JmapProperties.DsnBlobIds, JmapProperties.MdnBlobIds
        };
    }

    internal bool ArePropertiesValid(List<string>? properties)
    {
        if (properties == null)
        {
            return true;
        }

        List<string> known = SubmissionProperties();
        foreach (string property in properties)
        {
            if (property == null || !known.Contains(property))
            {
                return false;
            }
        }

        return true;
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

    internal void ValidateGetProjection(JsonElement arguments, List<string>? properties)
    {
        JsonElement list = Codec.Values.GetMember(arguments, JmapProperties.List);
        if (list.ValueKind != JsonValueKind.Array)
        {
            throw ProtocolFailure("The EmailSubmission/get result list is invalid.", JmapProperties.List,
                list);
        }

        HashSet<string> included =
            new HashSet<string>(EffectiveProperties(properties), StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement submission in list.EnumerateArray())
        {
            if (submission.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolFailure("An EmailSubmission/get result item is invalid.",
                    JmapProperties.List + "/" + index, submission);
            }

            foreach (JsonProperty property in submission.EnumerateObject())
            {
                if (SubmissionProperties().Contains(property.Name) && !included.Contains(property.Name))
                {
                    throw ProtocolFailure("EmailSubmission/get returned an unrequested property.",
                        JmapProperties.List + "/" + index + "/" + property.Name, submission);
                }
            }

            foreach (string property in included)
            {
                if (Codec.Values.GetMember(submission, property).ValueKind == JsonValueKind.Undefined)
                {
                    throw ProtocolFailure("EmailSubmission/get omitted a requested property.",
                        JmapProperties.List + "/" + index + "/" + property, submission);
                }
            }

            index = index + 1;
        }
    }

    internal JmapInvocation GetSuccessInvocation(
        EmailSubmissionGetResponse response,
        List<string>? properties,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteProjectedGetResponse(response, properties, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionGet, callId, stream.ToArray());
    }

    internal void WriteProjectedGetResponse(
        EmailSubmissionGetResponse response,
        List<string>? properties,
        JmapJsonWriter writer)
    {
        writer.WriteObjectStart();
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.AccountId, response.AccountId,
            "/accountId");
        Codec.Values.WriteRequiredStringMember(writer, JmapProperties.State, response.State, "/state");
        writer.WriteMemberName(JmapProperties.List);
        writer.WriteArrayStart();
        int index = 0;
        foreach (EmailSubmission submission in response.List)
        {
            WriteProjectedSubmission(submission, properties, writer, "/list/" + index);
            index = index + 1;
        }

        writer.WriteArrayEnd();
        writer.WriteMemberName(JmapProperties.NotFound);
        Codec.Values.EncodeStringArray(response.NotFound, writer, "/notFound");
        Codec.Values.WriteUnknownMembers(response.UnknownProperties, Codec.GetResponseNames(), writer);
        writer.WriteObjectEnd();
    }

    internal void WriteProjectedSubmission(
        EmailSubmission submission,
        List<string>? properties,
        JmapJsonWriter writer,
        string path)
    {
        writer.WriteObjectStart();
        foreach (string property in EffectiveProperties(properties))
        {
            if (property == JmapProperties.Id)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, submission.Id, path + "/id");
            }
            else if (property == JmapProperties.IdentityId)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, submission.IdentityId,
                    path + "/identityId");
            }
            else if (property == JmapProperties.EmailId)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, submission.EmailId,
                    path + "/emailId");
            }
            else if (property == JmapProperties.ThreadId)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, submission.ThreadId,
                    path + "/threadId");
            }
            else if (property == JmapProperties.Envelope)
            {
                writer.WriteMemberName(property);
                if (submission.Envelope == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    Codec.EncodeEnvelope(submission.Envelope, writer, path + "/envelope");
                }
            }
            else if (property == JmapProperties.SendAt)
            {
                Codec.Values.WriteRequiredStringMember(writer, property, submission.SendAt,
                    path + "/sendAt");
            }
            else if (property == JmapProperties.UndoStatus)
            {
                writer.WriteStringMember(property,
                    Codec.EncodeUndoStatus(submission.UndoStatus, path + "/undoStatus"), false);
            }
            else if (property == JmapProperties.DeliveryStatus)
            {
                if (submission.DeliveryStatus == null)
                {
                    writer.WriteMemberName(property);
                    writer.WriteNullValue();
                }
                else
                {
                    Codec.WriteOptionalDeliveryMap(property, submission.DeliveryStatus, writer,
                        path + "/deliveryStatus");
                }
            }
            else if (property == JmapProperties.DsnBlobIds)
            {
                WriteBlobIds(property, submission.DsnBlobIds, writer, path + "/dsnBlobIds");
            }
            else if (property == JmapProperties.MdnBlobIds)
            {
                WriteBlobIds(property, submission.MdnBlobIds, writer, path + "/mdnBlobIds");
            }
        }

        writer.WriteObjectEnd();
    }

    internal void WriteBlobIds(
        string property,
        List<string>? ids,
        JmapJsonWriter writer,
        string path)
    {
        if (ids == null)
        {
            throw ProtocolFailure("A projected EmailSubmission blob-id list is required.", path,
                default(JsonElement));
        }

        writer.WriteMemberName(property);
        Codec.Values.EncodeStringArray(ids, writer, path);
    }

    internal JmapInvocation ChangesSuccessInvocation(
        EmailSubmissionChangesResponse response,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionChanges, callId, stream.ToArray());
    }

    internal JmapInvocation QuerySuccessInvocation(
        EmailSubmissionQueryResponse response,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionQueryResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionQuery, callId, stream.ToArray());
    }

    internal JmapInvocation QueryChangesSuccessInvocation(
        EmailSubmissionQueryChangesResponse response,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionQueryChangesResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionQueryChanges, callId, stream.ToArray());
    }

    internal JmapInvocation SetSuccessInvocation(
        EmailSubmissionSetResponse response,
        string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSubmissionSetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.EmailSubmissionSet, callId, stream.ToArray());
    }

    internal EmailSubmissionSetDispatchResult SingleSetDispatchResult(JmapInvocation invocation)
    {
        EmailSubmissionSetDispatchResult result = new EmailSubmissionSetDispatchResult();
        result.MethodResponses.Add(invocation);
        return result;
    }

    internal JmapMethodError? ValidateSubmissionAccount(JmapSession session, string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        if (!session.Accounts.ContainsKey(id))
        {
            return MethodError(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound,
                "The account was not found or is not accessible.");
        }

        JmapAccount account = session.Accounts[id];
        if (account == null || account.AccountCapabilities == null)
        {
            throw ProtocolFailure("The Session contains invalid EmailSubmission account data.",
                "session/accounts/" + id, default(JsonElement));
        }

        if (!account.AccountCapabilities.ContainsKey(JmapCapabilities.Submission))
        {
            return MethodError(JmapErrorCodes.AccountNotSupportedByMethod,
                JmapMethodErrorKind.AccountNotSupportedByMethod,
                "The account does not support JMAP Submission.");
        }

        JmapCapability capability = account.AccountCapabilities[JmapCapabilities.Submission];
        if (capability == null || capability.Submission == null)
        {
            throw ProtocolFailure("The account contains invalid Submission capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return null;
    }

    internal JmapSubmissionCapabilities GetSubmissionCapabilities(
        JmapSession session,
        string? accountId)
    {
        string id = accountId == null ? "" : accountId;
        JmapCapability capability = session.Accounts[id].AccountCapabilities[JmapCapabilities.Submission];
        if (capability == null || capability.Submission == null)
        {
            throw ProtocolFailure("The account contains invalid Submission capability data.",
                "session/accounts/" + id, default(JsonElement));
        }

        return capability.Submission;
    }

    internal bool IsGetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement ids = Codec.Values.GetMember(arguments, JmapProperties.Ids);
        JsonElement properties = Codec.Values.GetMember(arguments, JmapProperties.Properties);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringArray(ids) &&
            IsOptionalStringArray(properties);
    }

    internal bool IsChangesRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement sinceState = Codec.Values.GetMember(arguments, JmapProperties.SinceState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        return accountId.ValueKind == JsonValueKind.String && sinceState.ValueKind == JsonValueKind.String &&
            (maximum.ValueKind == JsonValueKind.Undefined || maximum.ValueKind == JsonValueKind.Null ||
                IsUnsignedIntegerShape(maximum));
    }

    internal bool IsSetRequestShape(JsonElement arguments)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement state = Codec.Values.GetMember(arguments, JmapProperties.IfInState);
        JsonElement create = Codec.Values.GetMember(arguments, JmapProperties.Create);
        JsonElement update = Codec.Values.GetMember(arguments, JmapProperties.Update);
        JsonElement destroy = Codec.Values.GetMember(arguments, JmapProperties.Destroy);
        JsonElement updateEmail =
            Codec.Values.GetMember(arguments, JmapProperties.OnSuccessUpdateEmail);
        JsonElement destroyEmail =
            Codec.Values.GetMember(arguments, JmapProperties.OnSuccessDestroyEmail);
        return accountId.ValueKind == JsonValueKind.String && IsOptionalStringValue(state) &&
            IsOptionalObjectMap(create) && IsOptionalObjectMap(update) &&
            IsOptionalStringArray(destroy) && IsOptionalObjectMap(updateEmail) &&
            IsOptionalStringArray(destroyEmail);
    }

    internal bool IsOptionalObjectMap(JsonElement value)
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
        return accountId.ValueKind == JsonValueKind.String &&
            IsOptionalSubmissionFilterShape(filter, depth, maximumDepth) &&
            IsOptionalSubmissionComparatorArray(sort) && IsOptionalIntegerValue(position) &&
            IsOptionalStringValue(anchor) && IsOptionalIntegerValue(anchorOffset) &&
            IsOptionalNullableUnsignedIntegerValue(limit) && IsOptionalBooleanValue(calculate);
    }

    internal bool IsQueryChangesRequestShape(JsonElement arguments, int depth, int maximumDepth)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement sort = Codec.Values.GetMember(arguments, JmapProperties.Sort);
        JsonElement state = Codec.Values.GetMember(arguments, JmapProperties.SinceQueryState);
        JsonElement maximum = Codec.Values.GetMember(arguments, JmapProperties.MaxChanges);
        JsonElement upToId = Codec.Values.GetMember(arguments, JmapProperties.UpToId);
        JsonElement calculate = Codec.Values.GetMember(arguments, JmapProperties.CalculateTotal);
        return accountId.ValueKind == JsonValueKind.String && state.ValueKind == JsonValueKind.String &&
            IsOptionalSubmissionFilterShape(filter, depth, maximumDepth) &&
            IsOptionalSubmissionComparatorArray(sort) &&
            IsOptionalNullableUnsignedIntegerValue(maximum) && IsOptionalStringValue(upToId) &&
            IsOptionalBooleanValue(calculate);
    }

    internal bool IsOptionalSubmissionFilterShape(
        JsonElement value,
        int depth,
        int maximumDepth)
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
                !IsFilterOperatorToken(operatorValue.GetString()) ||
                conditions.ValueKind != JsonValueKind.Array || HasSubmissionConditionMember(value))
            {
                return false;
            }

            foreach (JsonElement condition in conditions.EnumerateArray())
            {
                if (condition.ValueKind != JsonValueKind.Object ||
                    !IsOptionalSubmissionFilterShape(condition, depth + 1, maximumDepth))
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

        JsonElement undo = Codec.Values.GetMember(value, JmapProperties.UndoStatus);
        return IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.IdentityIds)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.EmailIds)) &&
            IsOptionalStringArray(Codec.Values.GetMember(value, JmapProperties.ThreadIds)) &&
            IsOptionalUndoStatusToken(undo) &&
            IsOptionalStringValue(Codec.Values.GetMember(value, JmapProperties.Before)) &&
            IsOptionalStringValue(Codec.Values.GetMember(value, JmapProperties.After));
    }

    internal bool HasSubmissionConditionMember(JsonElement value)
    {
        return Codec.Values.GetMember(value, JmapProperties.IdentityIds).ValueKind !=
                JsonValueKind.Undefined ||
            Codec.Values.GetMember(value, JmapProperties.EmailIds).ValueKind != JsonValueKind.Undefined ||
            Codec.Values.GetMember(value, JmapProperties.ThreadIds).ValueKind != JsonValueKind.Undefined ||
            Codec.Values.GetMember(value, JmapProperties.UndoStatus).ValueKind != JsonValueKind.Undefined ||
            Codec.Values.GetMember(value, JmapProperties.Before).ValueKind != JsonValueKind.Undefined ||
            Codec.Values.GetMember(value, JmapProperties.After).ValueKind != JsonValueKind.Undefined;
    }

    internal bool IsOptionalSubmissionComparatorArray(JsonElement value)
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
            if (!IsSubmissionComparatorShape(comparator))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsSubmissionComparatorShape(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonElement property = Codec.Values.GetMember(value, JmapProperties.Property);
        JsonElement ascending = Codec.Values.GetMember(value, JmapProperties.IsAscending);
        JsonElement collation = Codec.Values.GetMember(value, JmapProperties.Collation);
        return property.ValueKind == JsonValueKind.String && IsOptionalBooleanValue(ascending) &&
            IsOptionalStringValue(collation);
    }

    internal bool IsFilterOperatorToken(string? value)
    {
        return value == "AND" || value == "OR" || value == "NOT";
    }

    internal bool IsOptionalUndoStatusToken(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        string? token = value.GetString();
        return token == "pending" || token == "final" || token == "canceled";
    }

    internal bool IsOptionalStringValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.String;
    }

    internal bool IsOptionalBooleanValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.True ||
            value.ValueKind == JsonValueKind.False;
    }

    internal bool IsOptionalIntegerValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || IsIntegerShape(value);
    }

    internal bool IsOptionalNullableUnsignedIntegerValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined || value.ValueKind == JsonValueKind.Null ||
            IsUnsignedIntegerShape(value);
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
            if (text[index] < '0' || text[index] > '9')
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
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateDispatchDependencies(
        JmapSession session,
        IEmailSubmissionService submissionService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || submissionService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("EmailSubmission dispatch dependencies are required.",
                "submissionService", default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation, string expectedName)
    {
        if (invocation == null)
        {
            throw ValidationFailure("An EmailSubmission invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != expectedName)
        {
            throw ValidationFailure("The invocation has the wrong EmailSubmission method name.",
                "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("EmailSubmission arguments must be a JSON object.",
                "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable EmailSubmission call identifier is required.",
                "methodCalls/0/2");
        }
    }

    internal JmapInvocation ErrorInvocation(
        string? type,
        JmapMethodErrorKind kind,
        string? description,
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
        return callId != null && callId.Length != 0 && Values.IsValidUnicode(callId);
    }

    internal void CopyUnknown(
        Dictionary<string, JsonElement> source,
        Dictionary<string, JsonElement> target)
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
