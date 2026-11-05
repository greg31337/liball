using System.Text.Json;
using JmapThread = jmap.Thread;

namespace jmap.Tests;

public class ThreadMethodTests
{
    [Test]
    public void GetReturnsRequestedOrderAndPreservesRealEmailOrder()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService service = new FakeThreadService();
        service.GetResponse.List.Add(StoredThread("t2", "e4"));
        service.GetResponse.List.Add(StoredThread("t1", "e1", "e2", "e3"));
        service.GetResponse.NotFound.Add("missing");
        ThreadGetRequest request = new ThreadGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "t1", "missing", "t2", "t1" }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        ThreadGetResponse response = method.Codec.DecodeThreadGetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.ThreadGet));
            Assert.That(response.List.Select(thread => thread.Id), Is.EqualTo(new[] { "t1", "t2" }));
            Assert.That(response.List[0].EmailIds, Is.EqualTo(new[] { "e1", "e2", "e3" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.LastGetRequest!.Ids, Is.EqualTo(request.Ids));
            Assert.That(service.LastGetRequest.Properties, Is.Null);
        }
    }

    [Test]
    public void GetProjectsIdWithoutFabricatingEmailMembership()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService service = new FakeThreadService();
        service.GetResponse.List.Add(StoredThread("t1", "e1", "e2"));
        ThreadGetRequest request = new ThreadGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.Id }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        ThreadGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.ThreadGet, "g1", invocation.Arguments.GetRawText()), "g1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { JmapProperties.Id }));
            Assert.That(mapped.List[0].Id, Is.EqualTo("t1"));
            Assert.That(mapped.List[0].EmailIds, Is.Empty);
        }
    }

    [Test]
    public void GetEnforcesArgumentsAccountsAndObjectLimits()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService service = new FakeThreadService();
        JmapInvocation invalidProperty = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.ThreadGet, "g1", "{\"accountId\":\"account-1\",\"properties\":[\"subject\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation tooManyIds = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.ThreadGet, "g2", "{\"accountId\":\"account-1\",\"ids\":[\"t1\",\"t2\"]}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);
        JmapInvocation missingAccount = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.ThreadGet, "g3", "{\"accountId\":\"account-1\"}"),
            Session(false), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        service.GetResponse.List.Add(StoredThread("t1", "e1"));
        service.GetResponse.List.Add(StoredThread("t2", "e2"));
        JmapInvocation tooManyAll = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.ThreadGet, "g4", "{\"accountId\":\"account-1\"}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalidProperty).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, tooManyIds).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, tooManyAll).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.GetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void GetRejectsDuplicateOrOverlappingBackendMembership()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService service = new FakeThreadService();
        service.GetResponse.List.Add(StoredThread("t1", "e1", "e2"));
        service.GetResponse.List.Add(StoredThread("t2", "e2", "e3"));

        JmapProtocolException error = Assert.Throws<JmapProtocolException>(() => method.ProcessGetInvocation(
            RawInvocation(JmapMethods.ThreadGet, "g1", "{\"accountId\":\"account-1\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None))!;

        Assert.That(error.Path, Is.EqualTo("list/1"));
    }

    [Test]
    public void ChangesPreservesBackendMembershipAndOrderClassifications()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService service = new FakeThreadService();
        service.ChangesResponse.NewState = "partial-state";
        service.ChangesResponse.HasMoreChanges = true;
        service.ChangesResponse.Created.Add("created-thread");
        service.ChangesResponse.Updated.Add("membership-or-order-changed");
        service.ChangesResponse.Destroyed.Add("destroyed-thread");
        ThreadChangesRequest request = new ThreadChangesRequest
        {
            AccountId = "account-1",
            SinceState = "old-state",
            MaxChanges = 3,
            HasMaxChanges = true
        };

        JmapInvocation invocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(request, "c1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        ThreadChangesResponse response = method.Codec.DecodeThreadChangesResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created, Is.EqualTo(new[] { "created-thread" }));
            Assert.That(response.Updated, Is.EqualTo(new[] { "membership-or-order-changed" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "destroyed-thread" }));
            Assert.That(response.HasMoreChanges, Is.True);
            Assert.That(response.NewState, Is.EqualTo("partial-state"));
            Assert.That(service.LastChangesRequest!.SinceState, Is.EqualTo("old-state"));
            Assert.That(service.LastChangesRequest.MaxChanges, Is.EqualTo(3));
            Assert.That(service.LastChangesRequest.HasMaxChanges, Is.True);
        }
    }

    [Test]
    public void ChangesRejectsInvalidLimitsAndBackendClassifications()
    {
        ThreadMethod method = new ThreadMethod();
        FakeThreadService invalidService = new FakeThreadService();
        JmapInvocation invalid = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.ThreadChanges, "c1", "{\"accountId\":\"account-1\",\"sinceState\":\"old-state\",\"maxChanges\":0}"),
            Session(true), invalidService, new JmapRequestContext(), Options(10), CancellationToken.None);
        FakeThreadService duplicateService = new FakeThreadService();
        duplicateService.ChangesResponse.Created.Add("t1");
        duplicateService.ChangesResponse.Updated.Add("t1");
        JmapProtocolException duplicate = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c2"), Session(true), duplicateService,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        FakeThreadService oversizedService = new FakeThreadService();
        oversizedService.ChangesResponse.Created.Add("t1");
        oversizedService.ChangesResponse.Updated.Add("t2");
        ThreadChangesRequest limited = ChangesRequest();
        limited.HasMaxChanges = true;
        limited.MaxChanges = 1;
        JmapProtocolException oversized = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(limited, "c3"), Session(true), oversizedService,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalid).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(invalidService.ChangesCalls, Is.Zero);
            Assert.That(duplicate.Path, Is.EqualTo("updated"));
            Assert.That(oversized.Path, Is.EqualTo("methodResponses/1"));
        }
    }

    [Test]
    public void ClientMappingHandlesThreadResultsAndStandardChangeFailures()
    {
        ThreadMethod method = new ThreadMethod();
        ThreadGetRequest getRequest = new ThreadGetRequest { AccountId = "account-1" };
        JmapResponse getWire = Response(
            JmapMethods.ThreadGet,
            "g1",
            "{\"accountId\":\"account-1\",\"state\":\"s1\",\"list\":[{\"id\":\"t1\",\"emailIds\":[\"e1\",\"e2\"]}],\"notFound\":[]}");
        ThreadGetResponse getResponse = method.MapGetResponse(getWire, "g1", getRequest);
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "c1",
            "{\"type\":\"cannotCalculateChanges\",\"description\":\"expired\"}");
        JmapMethodException error = Assert.Throws<JmapMethodException>(
            () => method.MapChangesResponse(failed, "c1", ChangesRequest()))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getResponse.List[0].EmailIds, Is.EqualTo(new[] { "e1", "e2" }));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
        }
    }

    private static JmapThread StoredThread(string id, params string[] emailIds)
    {
        JmapThread thread = new JmapThread();
        thread.Id = id;
        thread.EmailIds.AddRange(emailIds);
        return thread;
    }

    private static ThreadChangesRequest ChangesRequest()
    {
        ThreadChangesRequest request = new ThreadChangesRequest();
        request.AccountId = "account-1";
        request.SinceState = "old-state";
        return request;
    }

    private static JmapSession Session(bool includeAccount)
    {
        JmapSession session = new JmapSession();
        if (includeAccount)
        {
            JmapAccount account = new JmapAccount();
            JmapCapability mail = new JmapCapability();
            mail.Mail = new JmapMailCapabilities();
            account.AccountCapabilities.Add(JmapCapabilities.Mail, mail);
            session.Accounts.Add("account-1", account);
        }

        return session;
    }

    private static JmapServerOptions Options(int maximumObjectsInGet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInGet = maximumObjectsInGet;
        return options;
    }

    private static JmapInvocation RawInvocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JmapResponse Response(string name, string callId, string arguments)
    {
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(RawInvocation(name, callId, arguments));
        return response;
    }

    private static JmapMethodError Error(ThreadMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeThreadService : IThreadService
    {
        public ThreadGetResponse GetResponse = new ThreadGetResponse
        {
            AccountId = "account-1",
            State = "thread-state"
        };

        public ThreadChangesResponse ChangesResponse = new ThreadChangesResponse
        {
            AccountId = "account-1",
            OldState = "old-state",
            NewState = "new-state"
        };

        public int GetCalls;
        public int ChangesCalls;
        public ThreadGetRequest? LastGetRequest;
        public ThreadChangesRequest? LastChangesRequest;

        public Task<ThreadGetResponse> GetStoredThreadsAsync(
            ThreadGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            return Task.FromResult(GetResponse);
        }

        public Task<ThreadChangesResponse> GetStoredThreadChangesAsync(
            ThreadChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ChangesCalls = ChangesCalls + 1;
            LastChangesRequest = request;
            return Task.FromResult(ChangesResponse);
        }
    }
}
