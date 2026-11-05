using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class MethodDispatchResult
{
    internal List<JmapInvocation> Responses = new List<JmapInvocation>();
}

internal class RequestDispatcher
{
    internal JmapServerOptions Options;
    internal JmapServerServices Services;
    internal IPushVerificationSender? VerificationSender;
    internal MethodRegistry Registry;
    internal RequestValidator Validator;
    internal ResultReferenceResolver ResultReferences = new ResultReferenceResolver();
    internal MethodContext MethodContexts = new MethodContext();
    internal CoreEchoMethod Echo = new CoreEchoMethod();
    internal BlobCopyMethod Blobs = new BlobCopyMethod();
    internal PushSubscriptionMethod Push = new PushSubscriptionMethod();
    internal MailboxMethod Mailboxes = new MailboxMethod();
    internal ThreadMethod Threads = new ThreadMethod();
    internal EmailMethod Emails = new EmailMethod();
    internal SearchSnippetMethod Snippets = new SearchSnippetMethod();
    internal IdentityMethod Identities = new IdentityMethod();
    internal EmailSubmissionMethod Submissions = new EmailSubmissionMethod();
    internal VacationResponseMethod Vacations = new VacationResponseMethod();
    internal ErrorFactory Errors;
    internal JmapJsonReader Reader = new JmapJsonReader(
        JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal ProtocolJsonCodec Protocol;
    internal ValueValidator Values = new ValueValidator();
    internal SecurityErrorRedactor Redactor = new SecurityErrorRedactor();

    internal RequestDispatcher(
        JmapServerOptions options,
        JmapServerServices services,
        IPushVerificationSender? verificationSender)
    {
        if (options == null || services == null)
        {
            throw ValidationFailure("Server options and services are required.", "dispatcher");
        }

        Options = options;
        Services = services;
        VerificationSender = verificationSender;
        Registry = new MethodRegistry(services);
        Validator = new RequestValidator(
            options, Registry.GetSupportedCapabilities(), Registry.GetMethodCapabilities());
        Errors = new ErrorFactory(services.StringLocalizer);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapResponse> DispatchRequestAsync(
        JmapRequest request,
        JmapSession session,
        JmapRequestContext context,
        long requestBytes,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => DispatchRequest(request, session, context, requestBytes, cancellationToken),
            cancellationToken);
    }

    internal JmapResponse DispatchRequest(
        JmapRequest request,
        JmapSession session,
        JmapRequestContext context,
        long requestBytes,
        CancellationToken cancellationToken)
    {
        RequestValidationResult validation = ValidateDispatchRequest(
            request, session, context, requestBytes);
        if (!validation.IsValid)
        {
            throw new JmapValidationException(
                validation.Detail == null ? "The JMAP request is invalid." : validation.Detail,
                validation.InvalidPaths,
                default(JsonElement));
        }

        CreationIdResolver creationIds = new CreationIdResolver(request.CreatedIds);
        JmapResponse response = new JmapResponse();
        int index = 0;
        foreach (JmapInvocation source in request.MethodCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (validation.MethodErrors.ContainsKey(index))
            {
                response.MethodResponses.Add(ErrorInvocation(
                    validation.MethodErrors[index], source.CallId));
                index = index + 1;
                continue;
            }

            JmapInvocation? prepared = PrepareInvocation(
                source, response.MethodResponses, creationIds, response);
            if (prepared == null)
            {
                index = index + 1;
                continue;
            }

            RegisteredJmapMethod? registered = Registry.FindMethod(prepared.Name);
            if (registered == null)
            {
                response.MethodResponses.Add(ErrorInvocation(
                    Errors.CreateMethodError(
                        JmapErrorCodes.UnknownMethod,
                        "The requested method is not registered.",
                        false),
                    prepared.CallId));
                index = index + 1;
                continue;
            }

            Task<MethodDispatchResult> processing = DispatchMethodAsync(
                prepared, registered, session, context, cancellationToken);
            MethodDispatchResult result = AwaitDispatch(
                processing, prepared.CallId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            AppendResponses(response, result, prepared.CallId, creationIds);
            index = index + 1;
        }

        response.CreatedIds = creationIds.GetCarryOutCreatedIds();
        response.SessionState = session.State;
        return response;
    }

    internal RequestValidationResult ValidateDispatchRequest(
        JmapRequest? request,
        JmapSession? session,
        JmapRequestContext? context,
        long requestBytes)
    {
        if (session == null || session.State == null || !Values.IsValidUnicode(session.State))
        {
            throw ValidationFailure("A Session with a valid state is required.", "session/state");
        }

        if (context == null)
        {
            throw ValidationFailure("A request context is required.", "context");
        }

        return Validator.ValidateRequest(request, requestBytes);
    }

    internal JmapInvocation? PrepareInvocation(
        JmapInvocation source,
        List<JmapInvocation> previousResponses,
        CreationIdResolver creationIds,
        JmapResponse response)
    {
        JsonElement arguments = source.Arguments;
        if (HasResultReference(arguments))
        {
            ResultReferenceResolution reference = ResultReferences.ResolveArguments(
                arguments, previousResponses);
            if (!reference.IsSuccess || reference.Error != null)
            {
                response.MethodResponses.Add(ErrorInvocation(reference.Error, source.CallId));
                return null;
            }

            arguments = reference.Arguments;
        }

        if (creationIds.ContainsInvocationCreationIdReference(arguments))
        {
            CreationIdResolution creation = creationIds.ResolveInvocationData(arguments);
            if (creation.IsSuccess)
            {
                arguments = creation.Data;
            }
        }

        JmapInvocation prepared = new JmapInvocation();
        prepared.Name = source.Name;
        prepared.Arguments = arguments;
        prepared.CallId = source.CallId;
        return prepared;
    }

    internal bool HasResultReference(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in arguments.EnumerateObject())
        {
            if (property.Name.Length != 0 && property.Name[0] == '#')
            {
                return true;
            }
        }

        return false;
    }

    internal Task<MethodDispatchResult> DispatchMethodAsync(
        JmapInvocation invocation,
        RegisteredJmapMethod registered,
        JmapSession session,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => DispatchMethod(invocation, registered, session, context, cancellationToken),
            cancellationToken);
    }

    internal MethodDispatchResult DispatchMethod(
        JmapInvocation invocation,
        RegisteredJmapMethod registered,
        JmapSession session,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        JmapMethodError? accessError = MethodContexts.ValidateBeforeDispatch(
            invocation, registered, session);
        if (accessError != null)
        {
            return SingleResponse(ErrorInvocation(accessError, invocation.CallId));
        }

        if (registered.VendorHandler != null)
        {
            JmapInvocation vendor = Registry.ProcessVendorMethodAsync(
                invocation, context, cancellationToken).GetAwaiter().GetResult();
            return SingleResponse(vendor);
        }

        MethodDispatchResult result = DispatchStandardMethod(
            invocation, session, context, cancellationToken);
        MethodContexts.ValidateAfterDispatch(invocation, registered, result);
        return result;
    }

    internal MethodDispatchResult DispatchStandardMethod(
        JmapInvocation invocation,
        JmapSession session,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        string methodName = invocation.Name == null ? "" : invocation.Name;
        switch (methodName)
        {
            case JmapMethods.CoreEcho:
                return SingleResponse(Echo.ProcessInvocation(invocation));
            case JmapMethods.BlobCopy:
                return SingleResponse(Blobs.ProcessInvocation(
                    invocation, session, RequireBlobService(), context, cancellationToken));
            case JmapMethods.PushSubscriptionGet:
                return SingleResponse(Push.ProcessGetInvocation(
                    invocation, RequirePushService(), context, Options, Services.Clock,
                    cancellationToken));
            case JmapMethods.PushSubscriptionSet:
                return SingleResponse(Push.ProcessSetInvocation(
                    invocation, RequirePushService(), RequireVerificationSender(), context, Options,
                    Services.PushUrlPolicy, Services.Clock, cancellationToken));
            case JmapMethods.MailboxGet:
                return SingleResponse(Mailboxes.ProcessGetInvocation(
                    invocation, session, RequireMailboxService(), context, Options,
                    cancellationToken));
            case JmapMethods.MailboxChanges:
                return SingleResponse(Mailboxes.ProcessChangesInvocation(
                    invocation, session, RequireMailboxService(), context, Options,
                    cancellationToken));
            case JmapMethods.MailboxQuery:
                return SingleResponse(Mailboxes.ProcessQueryInvocation(
                    invocation, session, RequireMailboxService(), context, Options,
                    cancellationToken));
            case JmapMethods.MailboxQueryChanges:
                return SingleResponse(Mailboxes.ProcessQueryChangesInvocation(
                    invocation, session, RequireMailboxService(), context, Options,
                    cancellationToken));
            case JmapMethods.MailboxSet:
                return SingleResponse(Mailboxes.ProcessSetInvocation(
                    invocation, session, RequireMailboxService(), context, Options,
                    cancellationToken));
            case JmapMethods.ThreadGet:
                return SingleResponse(Threads.ProcessGetInvocation(
                    invocation, session, RequireThreadService(), context, Options,
                    cancellationToken));
            case JmapMethods.ThreadChanges:
                return SingleResponse(Threads.ProcessChangesInvocation(
                    invocation, session, RequireThreadService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailGet:
                return SingleResponse(Emails.ProcessGetInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailChanges:
                return SingleResponse(Emails.ProcessChangesInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailQuery:
                return SingleResponse(Emails.ProcessQueryInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailQueryChanges:
                return SingleResponse(Emails.ProcessQueryChangesInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSet:
                return SingleResponse(Emails.ProcessSetInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailCopy:
                return SingleResponse(Emails.ProcessCopyInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailImport:
                return SingleResponse(Emails.ProcessImportInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailParse:
                return SingleResponse(Emails.ProcessParseInvocation(
                    invocation, session, RequireEmailService(), context, Options,
                    cancellationToken));
            case JmapMethods.SearchSnippetGet:
                return SingleResponse(Snippets.ProcessInvocation(
                    invocation, session, RequireSearchSnippetService(), context, Options,
                    cancellationToken));
            case JmapMethods.IdentityGet:
                return SingleResponse(Identities.ProcessGetInvocation(
                    invocation, session, RequireIdentityService(), context, Options,
                    cancellationToken));
            case JmapMethods.IdentityChanges:
                return SingleResponse(Identities.ProcessChangesInvocation(
                    invocation, session, RequireIdentityService(), context, Options,
                    cancellationToken));
            case JmapMethods.IdentitySet:
                return SingleResponse(Identities.ProcessSetInvocation(
                    invocation, session, RequireIdentityService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSubmissionGet:
                return SingleResponse(Submissions.ProcessGetInvocation(
                    invocation, session, RequireEmailSubmissionService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSubmissionChanges:
                return SingleResponse(Submissions.ProcessChangesInvocation(
                    invocation, session, RequireEmailSubmissionService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSubmissionQuery:
                return SingleResponse(Submissions.ProcessQueryInvocation(
                    invocation, session, RequireEmailSubmissionService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSubmissionQueryChanges:
                return SingleResponse(Submissions.ProcessQueryChangesInvocation(
                    invocation, session, RequireEmailSubmissionService(), context, Options,
                    cancellationToken));
            case JmapMethods.EmailSubmissionSet:
                EmailSubmissionSetDispatchResult submission =
                    Submissions.ProcessSetInvocationResult(
                        invocation, session, RequireEmailSubmissionService(),
                        RequireEmailService(), RequireIdentityService(), Services.Clock,
                        context, Options, cancellationToken);
                MethodDispatchResult submissionResult = new MethodDispatchResult();
                submissionResult.Responses = submission.MethodResponses;
                return submissionResult;
            case JmapMethods.VacationResponseGet:
                return SingleResponse(Vacations.ProcessGetInvocation(
                    invocation, session, RequireVacationResponseService(), context, Options,
                    cancellationToken));
            case JmapMethods.VacationResponseSet:
                return SingleResponse(Vacations.ProcessSetInvocation(
                    invocation, session, RequireVacationResponseService(), context, Options,
                    cancellationToken));
            default:
                return SingleResponse(ErrorInvocation(
                    Errors.CreateMethodError(
                        JmapErrorCodes.UnknownMethod, "The method is not implemented.", false),
                    invocation.CallId));
        }
    }

    internal MethodDispatchResult AwaitDispatch(
        Task<MethodDispatchResult> processing,
        string? callId,
        CancellationToken cancellationToken)
    {
        Task.WhenAny(processing).GetAwaiter().GetResult();
        if (processing.IsCanceled)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SingleResponse(ErrorInvocation(
                Errors.CreateMethodError(
                    JmapErrorCodes.ServerFail, "The method was canceled by its backend.", false),
                callId));
        }

        if (processing.IsFaulted)
        {
            return SingleResponse(ErrorInvocation(
                Errors.CreateMethodError(
                    JmapErrorCodes.ServerFail,
                    Redactor.RedactMethodFailure(processing.Exception), false),
                callId));
        }

        MethodDispatchResult? result = processing.Result;
        if (result == null || !ValidateMethodResponses(result, callId))
        {
            return SingleResponse(ErrorInvocation(
                Errors.CreateMethodError(
                    JmapErrorCodes.ServerFail, "The method returned an invalid response.", false),
                callId));
        }

        return result;
    }

    internal bool ValidateMethodResponses(MethodDispatchResult result, string? callId)
    {
        if (result.Responses == null || result.Responses.Count == 0)
        {
            return false;
        }

        foreach (JmapInvocation response in result.Responses)
        {
            if (response == null)
            {
                return false;
            }

            bool validName = response.Name == ResponseMapper.ErrorResponseName ||
                Values.IsMethodName(response.Name);
            if (!validName || response.CallId != callId ||
                response.Arguments.ValueKind != JsonValueKind.Object)
            {
                return false;
            }
        }

        return true;
    }

    internal MethodDispatchResult SingleResponse(JmapInvocation response)
    {
        MethodDispatchResult result = new MethodDispatchResult();
        result.Responses.Add(response);
        return result;
    }

    internal void AppendResponses(
        JmapResponse response,
        MethodDispatchResult result,
        string? callId,
        CreationIdResolver creationIds)
    {
        if (!ValidateMethodResponses(result, callId))
        {
            response.MethodResponses.Add(ErrorInvocation(
                Errors.CreateMethodError(
                    JmapErrorCodes.ServerFail, "The method returned an invalid response.", false),
                callId));
            return;
        }

        foreach (JmapInvocation invocation in result.Responses)
        {
            response.MethodResponses.Add(invocation);
            creationIds.AccumulateResponseCreatedIds(invocation);
        }
    }

    internal JmapInvocation ErrorInvocation(JmapMethodError? error, string? callId)
    {
        JmapMethodError normalized = Errors.NormalizeMethodError(error, false);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(normalized, writer);
        writer.DisposeWriter();
        JmapInvocation response = new JmapInvocation();
        response.Name = ResponseMapper.ErrorResponseName;
        response.Arguments = Reader.ReadUtf8Memory(stream.ToArray());
        response.CallId = callId;
        return response;
    }

    internal IBlobService RequireBlobService()
    {
        if (Services.BlobService == null)
        {
            throw ValidationFailure("The Blob service is not configured.", "services/blobService");
        }

        return Services.BlobService;
    }

    internal IPushService RequirePushService()
    {
        if (Services.PushService == null)
        {
            throw ValidationFailure("The Push service is not configured.", "services/pushService");
        }

        return Services.PushService;
    }

    internal IPushVerificationSender RequireVerificationSender()
    {
        if (VerificationSender == null)
        {
            throw ValidationFailure("The push verification sender is not configured.", "services/pushVerificationSender");
        }

        return VerificationSender;
    }

    internal IMailboxService RequireMailboxService()
    {
        if (Services.MailboxService == null)
        {
            throw ValidationFailure("The Mailbox service is not configured.", "services/mailboxService");
        }

        return Services.MailboxService;
    }

    internal IThreadService RequireThreadService()
    {
        if (Services.ThreadService == null)
        {
            throw ValidationFailure("The Thread service is not configured.", "services/threadService");
        }

        return Services.ThreadService;
    }

    internal IEmailService RequireEmailService()
    {
        if (Services.EmailService == null)
        {
            throw ValidationFailure("The Email service is not configured.", "services/emailService");
        }

        return Services.EmailService;
    }

    internal ISearchSnippetService RequireSearchSnippetService()
    {
        if (Services.SearchSnippetService == null)
        {
            throw ValidationFailure("The search snippet service is not configured.", "services/searchSnippetService");
        }

        return Services.SearchSnippetService;
    }

    internal IIdentityService RequireIdentityService()
    {
        if (Services.IdentityService == null)
        {
            throw ValidationFailure("The Identity service is not configured.", "services/identityService");
        }

        return Services.IdentityService;
    }

    internal IEmailSubmissionService RequireEmailSubmissionService()
    {
        if (Services.EmailSubmissionService == null)
        {
            throw ValidationFailure("The EmailSubmission service is not configured.", "services/emailSubmissionService");
        }

        return Services.EmailSubmissionService;
    }

    internal IVacationResponseService RequireVacationResponseService()
    {
        if (Services.VacationResponseService == null)
        {
            throw ValidationFailure("The VacationResponse service is not configured.", "services/vacationResponseService");
        }

        return Services.VacationResponseService;
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }
}
