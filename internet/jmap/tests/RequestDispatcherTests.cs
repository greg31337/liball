using System.Text.Json;

namespace jmap.Tests;

public class RequestDispatcherTests
{
    private const string VendorCapability = "https://vendor.example/dispatch";

    [Test]
    public async Task DispatchesInOrderAndCarriesReferencesCreatedIdsErrorsAndSessionState()
    {
        ScriptedVendorHandler handler = new ScriptedVendorHandler();
        JmapServerServices services = VendorServices(handler,
            "Vendor/create", "Vendor/use", "Vendor/fail");
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), services, null);
        JmapRequest request = Request(JmapCapabilities.Core, VendorCapability);
        request.CreatedIds = new Dictionary<string, string> { ["carry"] = "C1" };
        request.MethodCalls.Add(Invocation("Vendor/create", "c1", "{\"number\":1.2300e+4}"));
        request.MethodCalls.Add(Invocation("Vendor/use", "c2", """
            {"#selected":{"resultOf":"c1","name":"Vendor/create","path":"/ids"}}
            """));
        request.MethodCalls.Add(Invocation("Vendor/fail", "c3", "{}"));
        request.MethodCalls.Add(Invocation(
            "Vendor/use", "c4", "{\"created\":\"#draft\",\"carried\":\"#carry\"}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.CoreEcho, "c5", "{\"afterFailure\":true}"));

        JmapResponse response = await dispatcher.DispatchRequestAsync(
            request, Session("session-7"), new JmapRequestContext(), 100L,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name), Is.EqualTo(new[]
            {
                "Vendor/create", "Vendor/use", "error", "Vendor/use", JmapMethods.CoreEcho
            }));
            Assert.That(response.MethodResponses.Select(value => value.CallId), Is.EqualTo(new[]
            {
                "c1", "c2", "c3", "c4", "c5"
            }));
            Assert.That(handler.MethodNames, Is.EqualTo(new[]
            {
                "Vendor/create", "Vendor/use", "Vendor/fail", "Vendor/use"
            }));
            Assert.That(handler.Arguments[0].GetRawText(),
                Is.EqualTo("{\"number\":1.2300e+4}"));
            Assert.That(handler.Arguments[1].GetProperty("selected")[0].GetString(),
                Is.EqualTo("M1"));
            Assert.That(handler.Arguments[3].GetProperty("created").GetString(), Is.EqualTo("M1"));
            Assert.That(handler.Arguments[3].GetProperty("carried").GetString(), Is.EqualTo("C1"));
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.ServerFail));
            Assert.That(response.MethodResponses[2].Arguments.GetRawText(),
                Does.Not.Contain("backend"));
            Assert.That(response.MethodResponses[4].Arguments.GetProperty("afterFailure").GetBoolean(),
                Is.True);
            Assert.That(response.CreatedIds, Is.EqualTo(new Dictionary<string, string>
            {
                ["carry"] = "C1",
                ["draft"] = "M1"
            }));
            Assert.That(response.SessionState, Is.EqualTo("session-7"));
        }
    }

    [Test]
    public void InvalidResultReferenceAndMissingCapabilityErrorsDoNotStopLaterCalls()
    {
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), new JmapServerServices(), null);
        JmapRequest request = Request(JmapCapabilities.Core);
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c1", "{\"value\":1}"));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c2", """
            {"#value":{"resultOf":"c1","name":"Core/echo","path":"/missing"}}
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.EmailGet, "c3", "{}"));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c4", "{\"done\":true}"));

        JmapResponse response = dispatcher.DispatchRequest(
            request, Session("state"), new JmapRequestContext(), 100L,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name), Is.EqualTo(new[]
            {
                JmapMethods.CoreEcho, "error", "error", JmapMethods.CoreEcho
            }));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidResultReference));
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.UnknownMethod));
            Assert.That(response.MethodResponses[3].Arguments.GetProperty("done").GetBoolean(),
                Is.True);
            Assert.That(response.CreatedIds, Is.Null);
        }
    }

    [Test]
    public void DispatchValidationUsesTheMeasuredRequestByteLimit()
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxRequestBytes = 10L;
        RequestDispatcher dispatcher = new RequestDispatcher(
            options, new JmapServerServices(), null);
        JmapRequest request = Request(JmapCapabilities.Core);
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c1", "{}"));

        RequestValidationResult result = dispatcher.ValidateDispatchRequest(
            request, Session("state"), new JmapRequestContext(), 11L);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ProblemKind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(result.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
        }
    }

    [Test]
    public async Task CancellationStopsDispatchBeforeTheNextMethod()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelingVendorHandler handler = new CancelingVendorHandler(cancellation);
        JmapServerServices services = VendorServices(handler, "Vendor/cancel", "Vendor/after");
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), services, null);
        JmapRequest request = Request(JmapCapabilities.Core, VendorCapability);
        request.MethodCalls.Add(Invocation("Vendor/cancel", "c1", "{}"));
        request.MethodCalls.Add(Invocation("Vendor/after", "c2", "{}"));

        Assert.That(async () => await dispatcher.DispatchRequestAsync(
            request, Session("state"), new JmapRequestContext(), 100L,
            cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(handler.MethodNames, Is.EqualTo(new[] { "Vendor/cancel" }));
    }

    [Test]
    public void SubmissionSetImplicitEmailResponseStaysAdjacentToItsPrimaryResponse()
    {
        DispatchSubmissionService submissions = new DispatchSubmissionService();
        DispatchEmailService emails = new DispatchEmailService();
        JmapServerServices services = new JmapServerServices();
        services.EmailSubmissionService = submissions;
        services.EmailService = emails;
        services.IdentityService = new DispatchIdentityService();
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), services, null);
        JmapRequest request = Request(JmapCapabilities.Core, JmapCapabilities.Submission);
        request.MethodCalls.Add(Invocation(JmapMethods.EmailSubmissionSet, "s1", """
            {
              "accountId":"account-1",
              "update":{"submission-1":{"undoStatus":"canceled"}},
              "onSuccessUpdateEmail":{"submission-1":{"keywords/$flagged":true}}
            }
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.CoreEcho, "c2", "{\"next\":true}"));

        JmapResponse response = dispatcher.DispatchRequest(
            request, SubmissionSession(), new JmapRequestContext(), 100L,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name), Is.EqualTo(new[]
            {
                JmapMethods.EmailSubmissionSet, JmapMethods.EmailSet, JmapMethods.CoreEcho
            }));
            Assert.That(response.MethodResponses.Select(value => value.CallId),
                Is.EqualTo(new[] { "s1", "s1", "c2" }));
            Assert.That(submissions.SetCalls, Is.EqualTo(1));
            Assert.That(emails.SetCalls, Is.EqualTo(1));
            Assert.That(emails.LastSetRequest!.Update!.Keys, Is.EqualTo(new[] { "email-1" }));
            Assert.That(emails.LastSetRequest.Update["email-1"].Values["keywords/$flagged"]
                .GetBoolean(), Is.True);
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("next").GetBoolean(),
                Is.True);
        }
    }

    [Test]
    public void ReadOnlyAccountFailureIsIsolatedBeforeAnyBackendAccess()
    {
        DispatchEmailService emails = new DispatchEmailService();
        JmapServerServices services = new JmapServerServices();
        services.EmailService = emails;
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), services, null);
        JmapRequest request = Request(JmapCapabilities.Core, JmapCapabilities.Mail);
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailSet, "s1", "{\"accountId\":\"account-1\"}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.CoreEcho, "c2", "{\"continued\":true}"));

        JmapResponse response = dispatcher.DispatchRequest(
            request, MailSession(true), new JmapRequestContext(), 100L,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name),
                Is.EqualTo(new[] { "error", JmapMethods.CoreEcho }));
            Assert.That(response.MethodResponses[0].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.AccountReadOnly));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("continued").GetBoolean(),
                Is.True);
            Assert.That(emails.GetCalls, Is.Zero);
            Assert.That(emails.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void InvalidBackendEnvelopeBecomesServerFailAndLaterCallsContinue()
    {
        DispatchEmailService emails = new DispatchEmailService();
        emails.GetResponse = new EmailGetResponse
        {
            AccountId = "other-account",
            State = "email-state"
        };
        JmapServerServices services = new JmapServerServices();
        services.EmailService = emails;
        RequestDispatcher dispatcher = new RequestDispatcher(
            new JmapServerOptions(), services, null);
        JmapRequest request = Request(JmapCapabilities.Core, JmapCapabilities.Mail);
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet, "g1", "{\"accountId\":\"account-1\",\"ids\":[]}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.CoreEcho, "c2", "{\"continued\":true}"));

        JmapResponse response = dispatcher.DispatchRequest(
            request, MailSession(false), new JmapRequestContext(), 100L,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name),
                Is.EqualTo(new[] { "error", JmapMethods.CoreEcho }));
            Assert.That(response.MethodResponses[0].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.ServerFail));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("continued").GetBoolean(),
                Is.True);
            Assert.That(emails.GetCalls, Is.EqualTo(1));
        }
    }

    private static JmapRequest Request(params string[] capabilities)
    {
        JmapRequest request = new JmapRequest();
        request.Using.AddRange(capabilities);
        return request;
    }

    private static JmapSession Session(string state)
    {
        JmapSession session = new JmapSession();
        session.State = state;
        return session;
    }

    private static JmapSession SubmissionSession()
    {
        JmapSession session = Session("session-state");
        JmapAccount account = new JmapAccount();
        JmapCapability capability = new JmapCapability();
        capability.Submission = new JmapSubmissionCapabilities();
        account.AccountCapabilities.Add(JmapCapabilities.Submission, capability);
        session.Accounts.Add("account-1", account);
        return session;
    }

    private static JmapSession MailSession(bool readOnly)
    {
        JmapSession session = Session("session-state");
        JmapAccount account = new JmapAccount();
        account.IsReadOnly = readOnly;
        JmapCapability capability = new JmapCapability();
        capability.Mail = new JmapMailCapabilities();
        account.AccountCapabilities.Add(JmapCapabilities.Mail, capability);
        session.Accounts.Add("account-1", account);
        return session;
    }

    private static JmapServerServices VendorServices(
        IJmapVendorMethodHandler handler,
        params string[] methodNames)
    {
        JmapServerServices services = new JmapServerServices();
        services.VendorCapabilities.Add(VendorCapability);
        foreach (string methodName in methodNames)
        {
            JmapVendorMethodRegistration registration = new JmapVendorMethodRegistration();
            registration.MethodName = methodName;
            registration.RequiredCapabilities.Add(VendorCapability);
            registration.Handler = handler;
            services.VendorMethods.Add(registration);
        }

        return services;
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class ScriptedVendorHandler : IJmapVendorMethodHandler
    {
        public List<string> MethodNames = new List<string>();
        public List<JsonElement> Arguments = new List<JsonElement>();

        public Task<JmapInvocation> ProcessVendorMethodAsync(
            JmapInvocation invocation,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            MethodNames.Add(invocation.Name!);
            Arguments.Add(invocation.Arguments.Clone());
            if (invocation.Name == "Vendor/fail")
            {
                return Task.FromException<JmapInvocation>(new InvalidOperationException("backend"));
            }

            JmapInvocation response = new JmapInvocation();
            response.Name = invocation.Name;
            response.CallId = invocation.CallId;
            response.Arguments = invocation.Name == "Vendor/create"
                ? Json("{\"created\":{\"draft\":{\"id\":\"M1\"}},\"ids\":[\"M1\"]}")
                : invocation.Arguments.Clone();
            return Task.FromResult(response);
        }
    }

    private class CancelingVendorHandler : IJmapVendorMethodHandler
    {
        public CancellationTokenSource Cancellation;
        public List<string> MethodNames = new List<string>();

        public CancelingVendorHandler(CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
        }

        public Task<JmapInvocation> ProcessVendorMethodAsync(
            JmapInvocation invocation,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            MethodNames.Add(invocation.Name!);
            Cancellation.Cancel();
            return Task.FromCanceled<JmapInvocation>(cancellationToken);
        }
    }

    private class DispatchSubmissionService : IEmailSubmissionService
    {
        public int SetCalls;

        public Task<EmailSubmissionGetResponse> GetStoredEmailSubmissionsAsync(
            EmailSubmissionGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            EmailSubmissionGetResponse response = new EmailSubmissionGetResponse();
            response.AccountId = "account-1";
            response.State = "submission-state";
            response.List.Add(new EmailSubmission
            {
                Id = "submission-1",
                EmailId = "email-1",
                UndoStatus = JmapSubmissionUndoStatus.Pending,
                HasUndoStatus = true
            });
            return Task.FromResult(response);
        }

        public Task<EmailSubmissionChangesResponse> GetStoredEmailSubmissionChangesAsync(
            EmailSubmissionChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailSubmissionChangesResponse());
        }

        public Task<EmailSubmissionQueryResponse> QueryStoredEmailSubmissionsAsync(
            EmailSubmissionQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailSubmissionQueryResponse());
        }

        public Task<EmailSubmissionQueryChangesResponse> GetStoredEmailSubmissionQueryChangesAsync(
            EmailSubmissionQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailSubmissionQueryChangesResponse());
        }

        public Task<EmailSubmissionSetResponse> SetStoredEmailSubmissionsAsync(
            EmailSubmissionSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            EmailSubmissionSetResponse response = new EmailSubmissionSetResponse();
            response.AccountId = "account-1";
            response.OldState = "submission-state";
            response.NewState = "submission-state-2";
            response.Updated = new Dictionary<string, JsonElement>
            {
                ["submission-1"] = Json("{\"undoStatus\":\"canceled\"}")
            };
            return Task.FromResult(response);
        }
    }

    private class DispatchEmailService : IEmailService
    {
        public EmailGetResponse? GetResponse;
        public int GetCalls;
        public int SetCalls;
        public EmailSetRequest? LastSetRequest;

        public Task<EmailGetResponse> GetStoredEmailsAsync(
            EmailGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            if (GetResponse != null)
            {
                return Task.FromResult(GetResponse);
            }

            return Task.FromResult(new EmailGetResponse());
        }

        public Task<EmailChangesResponse> GetStoredEmailChangesAsync(
            EmailChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailChangesResponse());
        }

        public Task<EmailQueryResponse> QueryStoredEmailsAsync(
            EmailQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailQueryResponse());
        }

        public Task<EmailQueryChangesResponse> GetStoredEmailQueryChangesAsync(
            EmailQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailQueryChangesResponse());
        }

        public Task<EmailSetResponse> SetStoredEmailsAsync(
            EmailSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            EmailSetResponse response = new EmailSetResponse();
            response.AccountId = "account-1";
            response.OldState = "email-state";
            response.NewState = "email-state-2";
            response.Updated = new Dictionary<string, JsonElement>
            {
                ["email-1"] = Json("null")
            };
            return Task.FromResult(response);
        }

        public Task<EmailCopyResponse> CopyStoredEmailsAsync(
            EmailCopyRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailCopyResponse());
        }

        public Task<EmailImportResponse> ImportStoredEmailsAsync(
            EmailImportRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailImportResponse());
        }

        public Task<EmailParseResponse> ParseStoredEmailBlobsAsync(
            EmailParseRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmailParseResponse());
        }
    }

    private class DispatchIdentityService : IIdentityService
    {
        public Task<IdentityGetResponse> GetStoredIdentitiesAsync(
            IdentityGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdentityGetResponse());
        }

        public Task<IdentityChangesResponse> GetStoredIdentityChangesAsync(
            IdentityChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdentityChangesResponse());
        }

        public Task<IdentitySetResponse> SetStoredIdentitiesAsync(
            IdentitySetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new IdentitySetResponse());
        }
    }
}
