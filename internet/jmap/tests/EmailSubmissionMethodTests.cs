using System.Text.Json;

namespace jmap.Tests;

public class EmailSubmissionMethodTests
{
    [Test]
    public void GetReturnsEverySubmissionFieldAndMapsTheTypedResponse()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.GetResponse.List.Add(FullSubmission("submission-1", "yes"));
        EmailSubmissionGetRequest request = new EmailSubmissionGetRequest { AccountId = "account-1" };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        EmailSubmissionGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.EmailSubmissionGet, "g1", invocation.Arguments.GetRawText()), "g1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.SubmissionProperties()));
            Assert.That(item.GetProperty(JmapProperties.Envelope)
                .GetProperty(JmapProperties.MailFrom).GetProperty(JmapProperties.Email).GetString(),
                Is.EqualTo("sender@example.test"));
            Assert.That(item.GetProperty(JmapProperties.DeliveryStatus)
                .GetProperty("recipient@example.test").GetProperty(JmapProperties.Delivered).GetString(),
                Is.EqualTo("yes"));
            Assert.That(mapped.List.Single().DsnBlobIds, Is.EqualTo(new[] { "dsn-1", "dsn-2" }));
            Assert.That(mapped.List.Single().MdnBlobIds, Is.EqualTo(new[] { "mdn-1" }));
            Assert.That(mapped.List.Single().HasUndoStatus, Is.True);
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.LastGetRequest?.Properties, Is.Null);
        }
    }

    [Test]
    public void GetProjectsPropertiesOrdersRequestedIdsAndReadsEachBackendSnapshot()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.GetResponse.List.Add(FullSubmission("submission-2", "queued"));
        service.GetResponse.List.Add(FullSubmission("submission-1", "yes"));
        service.GetResponse.NotFound.Add("missing");
        EmailSubmissionGetRequest request = new EmailSubmissionGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "missing", "submission-1", "submission-2", "submission-1" },
            Properties = new List<string>
            {
                JmapProperties.DeliveryStatus,
                JmapProperties.DsnBlobIds
            }
        };

        JmapInvocation first = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement firstItem = first.Arguments.GetProperty(JmapProperties.List)[0];

        service.GetResponse = new EmailSubmissionGetResponse
        {
            AccountId = "account-1",
            State = "submission-state-2",
            List = new List<EmailSubmission> { FullSubmission("submission-1", "no") },
            NotFound = new List<string> { "missing", "submission-2" }
        };
        JmapInvocation second = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Arguments.GetProperty(JmapProperties.List)
                .EnumerateArray().Select(value => value.GetProperty(JmapProperties.Id).GetString()),
                Is.EqualTo(new[] { "submission-1", "submission-2" }));
            Assert.That(first.Arguments.GetProperty(JmapProperties.NotFound)[0].GetString(),
                Is.EqualTo("missing"));
            Assert.That(firstItem.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[]
            {
                JmapProperties.Id,
                JmapProperties.DeliveryStatus,
                JmapProperties.DsnBlobIds
            }));
            Assert.That(service.LastGetRequest?.Properties, Is.EqualTo(new[]
            {
                JmapProperties.Id,
                JmapProperties.DeliveryStatus,
                JmapProperties.DsnBlobIds
            }));
            Assert.That(second.Arguments.GetProperty(JmapProperties.State).GetString(),
                Is.EqualTo("submission-state-2"));
            Assert.That(second.Arguments.GetProperty(JmapProperties.List)[0]
                .GetProperty(JmapProperties.DeliveryStatus).GetProperty("recipient@example.test")
                .GetProperty(JmapProperties.Delivered).GetString(), Is.EqualTo("no"));
            Assert.That(service.GetCalls, Is.EqualTo(2));
        }
    }

    [Test]
    public void ChangesPreservesPagedPersistentStateAndValidatesPartitions()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.ChangesResponse.Created.Add("created");
        service.ChangesResponse.Updated.Add("updated");
        service.ChangesResponse.Destroyed.Add("destroyed");
        service.ChangesResponse.HasMoreChanges = true;
        EmailSubmissionChangesRequest request = new EmailSubmissionChangesRequest
        {
            AccountId = "account-1",
            SinceState = "old-state",
            MaxChanges = 3,
            HasMaxChanges = true
        };

        JmapInvocation invocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(request, "c1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSubmissionChangesResponse decoded = method.Codec.DecodeSubmissionChangesResponse(
            invocation.Arguments);
        EmailSubmissionChangesResponse mapped = method.MapChangesResponse(
            Response(JmapMethods.EmailSubmissionChanges, "c1", invocation.Arguments.GetRawText()),
            "c1", request);

        service.ChangesResponse.Updated.Add("created");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(decoded.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(decoded.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(decoded.Destroyed, Is.EqualTo(new[] { "destroyed" }));
            Assert.That(decoded.HasMoreChanges, Is.True);
            Assert.That(mapped.NewState, Is.EqualTo("new-state"));
            Assert.That(service.LastChangesRequest?.SinceState, Is.EqualTo("old-state"));
            Assert.That(service.LastChangesRequest?.MaxChanges, Is.EqualTo(3));
            Assert.That(() => method.ProcessChangesInvocation(method.CreateChangesInvocation(request, "c2"),
                Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void GetRejectsInvalidArgumentsAccountsLimitsAndBackendRecords()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        JmapInvocation property = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailSubmissionGet, "g1",
                "{\"accountId\":\"account-1\",\"properties\":[\"subject\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation limit = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailSubmissionGet, "g2",
                "{\"accountId\":\"account-1\",\"ids\":[\"one\",\"two\"]}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);
        JmapInvocation missing = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailSubmissionGet, "g3", "{\"accountId\":\"account-1\"}"),
            Session(false), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailSubmissionGet, "g4", "{\"accountId\":\"account-1\"}"),
            MailOnlySession(), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        service.GetResponse.List.Add(FullSubmission("submission-1", "yes"));
        service.GetResponse.List[0].DsnBlobIds = new List<string> { "bad/id" };
        EmailSubmissionGetRequest request = new EmailSubmissionGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "submission-1" }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, property).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, missing).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, unsupported).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(service.GetCalls, Is.Zero);
            Assert.That(() => method.ProcessGetInvocation(method.CreateGetInvocation(request, "g5"),
                Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void GetAllAppliesObjectLimitAfterReadingThePersistentCollection()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.GetResponse.List.Add(FullSubmission("submission-1", "yes"));
        service.GetResponse.List.Add(FullSubmission("submission-2", "queued"));
        EmailSubmissionGetRequest request = new EmailSubmissionGetRequest { AccountId = "account-1" };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(1), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invocation).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.GetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryDispatchesEveryFilterSortAndPaginationFieldAndMapsState()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.QueryResponse.QueryState = "query-state-2";
        service.QueryResponse.CanCalculateChanges = true;
        service.QueryResponse.Position = 4;
        service.QueryResponse.Ids.Add("submission-1");
        service.QueryResponse.Ids.Add("submission-2");
        service.QueryResponse.HasTotal = true;
        service.QueryResponse.Total = 6;
        JmapInvocation invocation = method.ProcessQueryInvocation(
            RawInvocation(
                JmapMethods.EmailSubmissionQuery,
                "q1",
                "{\"accountId\":\"account-1\",\"filter\":{\"operator\":\"AND\",\"conditions\":[{\"identityIds\":[\"identity-1\"],\"emailIds\":[\"email-1\"],\"threadIds\":[\"thread-1\"],\"undoStatus\":\"final\",\"before\":\"2026-09-12T00:00:00Z\",\"after\":\"2026-09-10T00:00:00Z\"},{\"operator\":\"OR\",\"conditions\":[{\"emailIds\":[\"email-2\"]},{\"operator\":\"NOT\",\"conditions\":[{\"undoStatus\":\"canceled\"}]}]}]},\"sort\":[{\"property\":\"emailId\",\"isAscending\":true},{\"property\":\"threadId\",\"isAscending\":false},{\"property\":\"sentAt\",\"isAscending\":true}],\"position\":-1,\"anchor\":\"submission-3\",\"anchorOffset\":-2,\"limit\":2,\"calculateTotal\":true}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSubmissionQueryResponse mapped = method.MapQueryResponse(
            Response(JmapMethods.EmailSubmissionQuery, "q1", invocation.Arguments.GetRawText()),
            "q1", service.LastQueryRequest!);
        EmailSubmissionQueryRequest received = service.LastQueryRequest!;
        EmailSubmissionFilter condition = received.Filter!.Conditions![0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.EmailSubmissionQuery));
            Assert.That(received.Filter.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(received.Filter.Conditions![1].Operator, Is.EqualTo(JmapFilterOperator.Or));
            Assert.That(received.Filter.Conditions[1].Conditions![1].Operator,
                Is.EqualTo(JmapFilterOperator.Not));
            Assert.That(condition.IdentityIds, Is.EqualTo(new[] { "identity-1" }));
            Assert.That(condition.EmailIds, Is.EqualTo(new[] { "email-1" }));
            Assert.That(condition.ThreadIds, Is.EqualTo(new[] { "thread-1" }));
            Assert.That(condition.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Final));
            Assert.That(condition.Before, Is.EqualTo("2026-09-12T00:00:00Z"));
            Assert.That(condition.After, Is.EqualTo("2026-09-10T00:00:00Z"));
            Assert.That(received.Sort!.Select(value => value.Property),
                Is.EqualTo(new[] { JmapProperties.EmailId, JmapProperties.ThreadId,
                    JmapProperties.SentAt }));
            Assert.That(received.Sort!.Select(value => value.IsAscending),
                Is.EqualTo(new[] { true, false, true }));
            Assert.That(received.Position, Is.EqualTo(-1));
            Assert.That(received.Anchor, Is.EqualTo("submission-3"));
            Assert.That(received.AnchorOffset, Is.EqualTo(-2));
            Assert.That(received.Limit, Is.EqualTo(2));
            Assert.That(received.CalculateTotal, Is.True);
            Assert.That(mapped.QueryState, Is.EqualTo("query-state-2"));
            Assert.That(mapped.CanCalculateChanges, Is.True);
            Assert.That(mapped.Ids, Is.EqualTo(new[] { "submission-1", "submission-2" }));
            Assert.That(mapped.Total, Is.EqualTo(6));
            Assert.That(service.QueryCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryRejectsMalformedUnsupportedAndOutOfRangeArguments()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        JmapSession session = Session(true);
        JmapInvocation unknownFilter = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q1",
                "{\"accountId\":\"account-1\",\"filter\":{\"vendorFlag\":true}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unknownSort = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q2",
                "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"sendAt\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation collation = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q3",
                "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"emailId\",\"collation\":\"i;ascii-casemap\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation undo = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q4",
                "{\"accountId\":\"account-1\",\"filter\":{\"undoStatus\":\"queued\"}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation not = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q5",
                "{\"accountId\":\"account-1\",\"filter\":{\"operator\":\"NOT\",\"conditions\":[]}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation date = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q6",
                "{\"accountId\":\"account-1\",\"filter\":{\"before\":\"tomorrow\"}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation fraction = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q7",
                "{\"accountId\":\"account-1\",\"position\":1.5}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation negativeLimit = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q8",
                "{\"accountId\":\"account-1\",\"limit\":-1}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation badId = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q9",
                "{\"accountId\":\"account-1\",\"filter\":{\"identityIds\":[\"bad/id\"]}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapServerOptions shallow = Options(10);
        shallow.MaxJsonDepth = 1;
        JmapInvocation deep = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQuery, "q10",
                "{\"accountId\":\"account-1\",\"filter\":{\"operator\":\"AND\",\"conditions\":[{}]}}"),
            session, service, new JmapRequestContext(), shallow, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, unknownFilter).Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(Error(method, unknownSort).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, collation).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, undo).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, not).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, date).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, fraction).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, negativeLimit).Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, badId).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, deep).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(service.QueryCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryRejectsInvalidBackendWindowTotalLimitAndIdentifiers()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionQueryRequest request = new EmailSubmissionQueryRequest
        {
            AccountId = "account-1",
            Limit = 2,
            HasLimit = true
        };
        FakeEmailSubmissionService duplicate = new FakeEmailSubmissionService();
        duplicate.QueryResponse.Ids.Add("same");
        duplicate.QueryResponse.Ids.Add("same");
        FakeEmailSubmissionService wrongPosition = new FakeEmailSubmissionService();
        wrongPosition.QueryResponse.Position = 1;
        FakeEmailSubmissionService unexpectedTotal = new FakeEmailSubmissionService();
        unexpectedTotal.QueryResponse.HasTotal = true;
        unexpectedTotal.QueryResponse.Total = 0;
        FakeEmailSubmissionService badServerLimit = new FakeEmailSubmissionService();
        badServerLimit.QueryResponse.HasLimit = true;
        badServerLimit.QueryResponse.Limit = 2;

        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryInvocation(method.CreateQueryInvocation(request, "q1"), Session(true),
                duplicate, new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException positionError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryInvocation(method.CreateQueryInvocation(request, "q2"), Session(true),
                wrongPosition, new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException totalError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryInvocation(method.CreateQueryInvocation(request, "q3"), Session(true),
                unexpectedTotal, new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException limitError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryInvocation(method.CreateQueryInvocation(request, "q4"), Session(true),
                badServerLimit, new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(duplicateError.Path, Is.EqualTo("ids/1"));
            Assert.That(positionError.Path, Is.EqualTo(JmapProperties.Position));
            Assert.That(totalError.Path, Is.EqualTo(JmapProperties.Total));
            Assert.That(limitError.Path, Is.EqualTo(JmapProperties.Limit));
        }
    }

    [Test]
    public void QueryChangesPreservesQueryIdentityStatesTotalsAndIndexedMoves()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.QueryChangesResponse.Removed.Add("moved");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "moved", Index = 0 });
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "new", Index = 2 });
        service.QueryChangesResponse.HasTotal = true;
        service.QueryChangesResponse.Total = 3;
        EmailSubmissionQueryChangesRequest request = new EmailSubmissionQueryChangesRequest
        {
            AccountId = "account-1",
            Filter = new EmailSubmissionFilter
            {
                EmailIds = new List<string> { "email-1" },
                HasUndoStatus = true,
                UndoStatus = JmapSubmissionUndoStatus.Pending
            },
            Sort = new List<EmailSubmissionComparator>
            {
                new EmailSubmissionComparator
                {
                    Property = JmapProperties.SentAt,
                    IsAscending = false,
                    HasIsAscending = true
                }
            },
            SinceQueryState = "q0",
            MaxChanges = 3,
            HasMaxChanges = true,
            UpToId = "cached",
            CalculateTotal = true,
            HasCalculateTotal = true
        };

        JmapInvocation invocation = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "qc1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSubmissionQueryChangesResponse mapped = method.MapQueryChangesResponse(
            Response(JmapMethods.EmailSubmissionQueryChanges, "qc1", invocation.Arguments.GetRawText()),
            "qc1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.EmailSubmissionQueryChanges));
            Assert.That(service.LastQueryChangesRequest!.SinceQueryState, Is.EqualTo("q0"));
            Assert.That(service.LastQueryChangesRequest.MaxChanges, Is.EqualTo(3));
            Assert.That(service.LastQueryChangesRequest.UpToId, Is.EqualTo("cached"));
            Assert.That(service.LastQueryChangesRequest.Filter!.EmailIds,
                Is.EqualTo(new[] { "email-1" }));
            Assert.That(service.LastQueryChangesRequest.Sort![0].Property,
                Is.EqualTo(JmapProperties.SentAt));
            Assert.That(mapped.OldQueryState, Is.EqualTo("q0"));
            Assert.That(mapped.NewQueryState, Is.EqualTo("q1"));
            Assert.That(mapped.Removed, Is.EqualTo(new[] { "moved" }));
            Assert.That(mapped.Added.Select(value => value.Id), Is.EqualTo(new[] { "moved", "new" }));
            Assert.That(mapped.Added.Select(value => value.Index), Is.EqualTo(new long[] { 0, 2 }));
            Assert.That(mapped.Total, Is.EqualTo(3));
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryChangesReturnsTooManyChangesAndRejectsInvalidArguments()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.QueryChangesResponse.Removed.Add("old");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "new", Index = 0 });
        EmailSubmissionQueryChangesRequest request = new EmailSubmissionQueryChangesRequest
        {
            AccountId = "account-1",
            SinceQueryState = "q0",
            MaxChanges = 1,
            HasMaxChanges = true
        };
        JmapInvocation excessive = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "q1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        FakeEmailSubmissionService invalid = new FakeEmailSubmissionService();
        JmapInvocation maximum = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQueryChanges, "q2",
                "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"maxChanges\":-1}"),
            Session(true), invalid, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation upTo = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.EmailSubmissionQueryChanges, "q3",
                "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"upToId\":\"bad/id\"}"),
            Session(true), invalid, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, excessive).Kind, Is.EqualTo(JmapMethodErrorKind.TooManyChanges));
            Assert.That(Error(method, maximum).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, upTo).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
            Assert.That(invalid.QueryChangesCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryChangesRejectsInvalidBackendStatesTotalsIdsAndIndexOrdering()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionQueryChangesRequest request = new EmailSubmissionQueryChangesRequest
        {
            AccountId = "account-1",
            SinceQueryState = "q0",
            CalculateTotal = true,
            HasCalculateTotal = true
        };
        FakeEmailSubmissionService wrongState = new FakeEmailSubmissionService();
        wrongState.QueryChangesResponse.OldQueryState = "other";
        wrongState.QueryChangesResponse.HasTotal = true;
        wrongState.QueryChangesResponse.Total = 2;
        FakeEmailSubmissionService duplicate = new FakeEmailSubmissionService();
        duplicate.QueryChangesResponse.HasTotal = true;
        duplicate.QueryChangesResponse.Total = 2;
        duplicate.QueryChangesResponse.Removed.Add("same");
        duplicate.QueryChangesResponse.Removed.Add("same");
        FakeEmailSubmissionService unordered = new FakeEmailSubmissionService();
        unordered.QueryChangesResponse.HasTotal = true;
        unordered.QueryChangesResponse.Total = 3;
        unordered.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "first", Index = 2 });
        unordered.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "second", Index = 1 });
        FakeEmailSubmissionService missingTotal = new FakeEmailSubmissionService();

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(method.CreateQueryChangesInvocation(request, "q1"),
                Session(true), wrongState, new JmapRequestContext(), Options(10),
                CancellationToken.None))!;
        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(method.CreateQueryChangesInvocation(request, "q2"),
                Session(true), duplicate, new JmapRequestContext(), Options(10),
                CancellationToken.None))!;
        JmapProtocolException orderError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(method.CreateQueryChangesInvocation(request, "q3"),
                Session(true), unordered, new JmapRequestContext(), Options(10),
                CancellationToken.None))!;
        JmapProtocolException totalError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(method.CreateQueryChangesInvocation(request, "q4"),
                Session(true), missingTotal, new JmapRequestContext(), Options(10),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(duplicateError.Path, Is.EqualTo("removed/1"));
            Assert.That(orderError.Path, Is.EqualTo("added/1"));
            Assert.That(totalError.Path, Is.EqualTo(JmapProperties.Total));
        }
    }

    [Test]
    public void SetCreatesImmediateAndDelayedSubmissionsCancelsAndDestroysWithoutCancelingDelivery()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService service = new FakeEmailSubmissionService();
        service.GetResponse.List.Add(StoredSubmission("cancel-pending", JmapSubmissionUndoStatus.Pending));
        service.GetResponse.List.Add(StoredSubmission("immutable", JmapSubmissionUndoStatus.Final));
        service.GetResponse.List.Add(StoredSubmission("invalid-transition", JmapSubmissionUndoStatus.Pending));
        service.GetResponse.List.Add(StoredSubmission("both", JmapSubmissionUndoStatus.Pending));
        service.GetResponse.List.Add(StoredSubmission("destroy-pending", JmapSubmissionUndoStatus.Pending));
        service.GetResponse.List.Add(StoredSubmission("destroy-final", JmapSubmissionUndoStatus.Final));
        service.GetResponse.NotFound.Add("missing-update");
        service.GetResponse.NotFound.Add("missing-destroy");
        service.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["immediate"] = CreatedSubmission(
                "submission-immediate", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Final),
            ["delayed"] = CreatedSubmission(
                "submission-delayed", "2026-09-11T12:01:00Z", JmapSubmissionUndoStatus.Pending)
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["cancel-pending"] = Json("{\"undoStatus\":\"canceled\"}")
        };
        service.SetResponse.Destroyed = new List<string>
        {
            "destroy-pending", "destroy-final", "both"
        };
        string json = "{" +
            "\"accountId\":\"account-1\",\"ifInState\":\"submission-state\"," +
            "\"create\":{" +
                "\"immediate\":{\"identityId\":\"identity-1\",\"emailId\":\"email-1\"}," +
                "\"delayed\":{\"identityId\":\"identity-1\",\"emailId\":\"email-2\"," +
                    "\"envelope\":{\"mailFrom\":{\"email\":\"sender@example.test\"," +
                        "\"parameters\":{\"HOLDUNTIL\":\"20260911120100Z\"}}," +
                        "\"rcptTo\":[{\"email\":\"recipient@example.test\"}]}}," +
                "\"immutable-create\":{\"identityId\":\"identity-1\",\"emailId\":\"email-3\"," +
                    "\"sendAt\":null}}," +
            "\"update\":{" +
                "\"cancel-pending\":{\"undoStatus\":\"canceled\"}," +
                "\"immutable\":{\"emailId\":\"email-other\"}," +
                "\"invalid-transition\":{\"undoStatus\":\"final\"}," +
                "\"both\":{\"undoStatus\":\"canceled\"}," +
                "\"missing-update\":{\"undoStatus\":\"canceled\"}}," +
            "\"destroy\":[\"destroy-pending\",\"destroy-final\",\"both\",\"missing-destroy\"]}";
        JmapInvocation requestInvocation =
            RawInvocation(JmapMethods.EmailSubmissionSet, "s1", json);
        EmailSubmissionSetRequest original =
            method.Codec.DecodeSubmissionSetRequest(requestInvocation.Arguments);

        JmapInvocation invocation = method.ProcessSetInvocation(
            requestInvocation, Session(true), service, new FakeEmailService(),
            new FakeIdentityService(), new FixedClock(), new JmapRequestContext(), SetOptions(20),
            CancellationToken.None);
        EmailSubmissionSetResponse response = method.Codec.DecodeSubmissionSetResponse(invocation.Arguments);
        EmailSubmissionSetResponse mapped = method.MapSetResponse(
            Response(JmapMethods.EmailSubmissionSet, "s1", invocation.Arguments.GetRawText()),
            "s1", original);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!["immediate"].UndoStatus,
                Is.EqualTo(JmapSubmissionUndoStatus.Final));
            Assert.That(response.Created["delayed"].UndoStatus,
                Is.EqualTo(JmapSubmissionUndoStatus.Pending));
            Assert.That(response.Updated!["cancel-pending"].GetProperty(JmapProperties.UndoStatus)
                .GetString(), Is.EqualTo("canceled"));
            Assert.That(response.Destroyed,
                Is.EqualTo(new[] { "destroy-pending", "destroy-final", "both" }));
            Assert.That(response.NotCreated!["immutable-create"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["immutable-create"].Properties,
                Does.Contain(JmapProperties.SendAt));
            Assert.That(response.NotUpdated!["immutable"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["immutable"].Properties,
                Does.Contain(JmapProperties.EmailId));
            Assert.That(response.NotUpdated["invalid-transition"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["invalid-transition"].Properties,
                Does.Contain(JmapProperties.UndoStatus));
            Assert.That(response.NotUpdated["both"].Kind, Is.EqualTo(JmapSetErrorKind.WillDestroy));
            Assert.That(response.NotUpdated["missing-update"].Kind,
                Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(response.NotDestroyed!["missing-destroy"].Kind,
                Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(service.LastSetRequest!.Create!.Keys,
                Is.EquivalentTo(new[] { "immediate", "delayed" }));
            Assert.That(service.LastSetRequest.Create["delayed"].Envelope!.MailFrom!.Parameters!
                ["HOLDUNTIL"].GetString(), Is.EqualTo("20260911120100Z"));
            Assert.That(service.LastSetRequest.Update!.Keys, Is.EqualTo(new[] { "cancel-pending" }));
            Assert.That(service.LastSetRequest.Destroy,
                Is.EqualTo(new[] { "destroy-pending", "destroy-final", "both" }));
            Assert.That(mapped.NewState, Is.EqualTo("submission-state-2"));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void SetValidatesDerivedSenderAndAllRecipientHeadersThroughTheCreateFlow()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        submissions.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["derived"] = CreatedSubmission(
                "submission-derived", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Final)
        };
        FakeEmailService emails = new FakeEmailService
        {
            GetResponse = new EmailGetResponse
            {
                AccountId = "account-1",
                State = "email-state",
                List = new List<Email>
                {
                    new Email
                    {
                        Id = "email-derived",
                        Sender = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "sender@example.test" }
                        },
                        From = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "alias@example.test" }
                        },
                        To = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "to@example.test" }
                        },
                        Cc = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "cc@example.test" }
                        },
                        Bcc = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "bcc@example.test" }
                        }
                    },
                    new Email
                    {
                        Id = "email-without-recipients",
                        From = new List<EmailAddress>
                        {
                            new EmailAddress { Email = "alias@example.test" }
                        }
                    }
                }
            }
        };
        FakeIdentityService identities = new FakeIdentityService
        {
            GetResponse = new IdentityGetResponse
            {
                AccountId = "account-1",
                State = "identity-state",
                List = new List<Identity>
                {
                    new Identity { Id = "identity-1", Email = "*@example.test" }
                }
            }
        };
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["derived"] = Submission("identity-1", "email-derived", null),
                ["no-recipients"] = Submission("identity-1", "email-without-recipients", null)
            }
        };

        EmailSubmissionSetResponse response = method.Codec.DecodeSubmissionSetResponse(
            method.ProcessSetInvocation(method.CreateSetInvocation(request, "s1"), Session(true),
                submissions, emails, identities, new FixedClock(), new JmapRequestContext(),
                SetOptions(10), CancellationToken.None).Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!.Keys, Is.EqualTo(new[] { "derived" }));
            Assert.That(response.NotCreated!["no-recipients"].Kind,
                Is.EqualTo(JmapSetErrorKind.NoRecipients));
            Assert.That(submissions.LastSetRequest!.Create!.Keys, Is.EqualTo(new[] { "derived" }));
            Assert.That(submissions.LastSetRequest.Create["derived"].Envelope, Is.Null);
            Assert.That(emails.LastGetRequest!.Properties, Is.EqualTo(new[]
            {
                JmapProperties.Id,
                JmapProperties.Headers,
                JmapProperties.Sender,
                JmapProperties.From,
                JmapProperties.To,
                JmapProperties.Cc,
                JmapProperties.Bcc
            }));
            Assert.That(identities.LastGetRequest!.Properties,
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Email }));
        }
    }

    [Test]
    public void SetAcceptsAdvertisedStandardAndVendorEnvelopeParametersAndRejectsOthers()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        submissions.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["accepted"] = CreatedSubmission(
                "submission-accepted", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Final)
        };
        EmailSubmissionAddress sender = new EmailSubmissionAddress
        {
            Email = "sender@example.test",
            Parameters = new Dictionary<string, JsonElement>
            {
                ["RET"] = Json("\"FULL\""),
                ["ENVID"] = Json("\"tracking-1\""),
                ["BODY"] = Json("\"8BITMIME\""),
                ["BY"] = Json("\"120;R\""),
                ["SMTPUTF8"] = Json("null"),
                ["X-ROUTE"] = Json("\"east\"")
            }
        };
        EmailSubmissionAddress recipient = new EmailSubmissionAddress
        {
            Email = "recipient@example.test",
            Parameters = new Dictionary<string, JsonElement>
            {
                ["NOTIFY"] = Json("\"FAILURE,DELAY\""),
                ["ORCPT"] = Json("\"rfc822;recipient@example.test\"")
            }
        };
        EmailSubmissionEnvelope acceptedEnvelope = new EmailSubmissionEnvelope { MailFrom = sender };
        acceptedEnvelope.RcptTo.Add(recipient);
        EmailSubmissionEnvelope rejectedEnvelope = Envelope(
            "sender@example.test", "recipient@example.test", "UNADVERTISED", "value");
        rejectedEnvelope.RcptTo[0].Parameters = new Dictionary<string, JsonElement>
        {
            ["RET"] = Json("\"FULL\"")
        };
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["accepted"] = Submission("identity-1", "email-1", acceptedEnvelope),
                ["rejected"] = Submission("identity-1", "email-2", rejectedEnvelope)
            }
        };
        JmapSession session = Session(true);
        JmapSubmissionCapabilities capabilities = session.Accounts["account-1"].AccountCapabilities[
            JmapCapabilities.Submission].Submission!;
        capabilities.SubmissionExtensions["DSN"] = new List<string>();
        capabilities.SubmissionExtensions["8BITMIME"] = new List<string>();
        capabilities.SubmissionExtensions["DELIVERBY"] = new List<string>();
        capabilities.SubmissionExtensions["SMTPUTF8"] = new List<string>();
        capabilities.SubmissionExtensions["X-ROUTE"] = new List<string> { "east", "west" };

        EmailSubmissionSetResponse response = method.Codec.DecodeSubmissionSetResponse(
            method.ProcessSetInvocation(method.CreateSetInvocation(request, "s1"), session, submissions,
                new FakeEmailService(), new FakeIdentityService(), new FixedClock(),
                new JmapRequestContext(), SetOptions(10), CancellationToken.None).Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!.Keys, Is.EqualTo(new[] { "accepted" }));
            Assert.That(response.NotCreated!["rejected"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["rejected"].Properties, Is.EquivalentTo(new[]
            {
                "envelope/mailFrom/parameters/UNADVERTISED",
                "envelope/rcptTo/0/parameters/RET"
            }));
            Assert.That(submissions.LastSetRequest!.Create!.Keys, Is.EqualTo(new[] { "accepted" }));
            Assert.That(submissions.LastSetRequest.Create["accepted"].Envelope!.MailFrom!.Parameters!.Keys,
                Is.EquivalentTo(new[] { "RET", "ENVID", "BODY", "BY", "SMTPUTF8", "X-ROUTE" }));
            Assert.That(submissions.LastSetRequest.Create["accepted"].Envelope!.RcptTo[0].Parameters!.Keys,
                Is.EquivalentTo(new[] { "NOTIFY", "ORCPT" }));
        }
    }

    [Test]
    public void SetRejectsReadOnlyStateMismatchMalformedRequestsAndActionLimitsBeforeMutation()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            IfInState = "wrong-state",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["create"] = new EmailSubmission
                {
                    IdentityId = "identity-1",
                    EmailId = "email-1"
                }
            }
        };
        FakeEmailSubmissionService readOnlyService = new FakeEmailSubmissionService();
        JmapSession readOnlySession = Session(true);
        readOnlySession.Accounts["account-1"].IsReadOnly = true;
        JmapInvocation readOnly = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), readOnlySession, readOnlyService,
            new FakeEmailService(), new FakeIdentityService(), null,
            new JmapRequestContext(), SetOptions(10), CancellationToken.None);
        FakeEmailSubmissionService mismatchService = new FakeEmailSubmissionService();
        JmapInvocation mismatch = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s2"), Session(true), mismatchService,
            new FakeEmailService(), new FakeIdentityService(), null,
            new JmapRequestContext(), SetOptions(10), CancellationToken.None);
        FakeEmailSubmissionService limitService = new FakeEmailSubmissionService();
        JmapInvocation limit = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s3"), Session(true), limitService,
            new FakeEmailService(), new FakeIdentityService(), null,
            new JmapRequestContext(), SetOptions(0), CancellationToken.None);
        FakeEmailSubmissionService malformedService = new FakeEmailSubmissionService();
        JmapInvocation malformed = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.EmailSubmissionSet, "s4",
                "{\"accountId\":\"account-1\",\"create\":[]}"),
            Session(true), malformedService, new FakeEmailService(), new FakeIdentityService(),
            null, new JmapRequestContext(), SetOptions(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, readOnly).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, mismatch).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, malformed).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(readOnlyService.GetCalls, Is.Zero);
            Assert.That(mismatchService.GetCalls, Is.EqualTo(1));
            Assert.That(readOnlyService.SetCalls + mismatchService.SetCalls + limitService.SetCalls +
                malformedService.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRejectsMalformedBackendStatesCreatedTransitionsAndPartitions()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["create"] = new EmailSubmission
                {
                    IdentityId = "identity-1",
                    EmailId = "email-1"
                }
            }
        };
        FakeEmailSubmissionService state = new FakeEmailSubmissionService();
        state.SetResponse.OldState = "other-state";
        state.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["create"] = CreatedSubmission(
                "submission-1", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Final)
        };
        FakeEmailSubmissionService transition = new FakeEmailSubmissionService();
        transition.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["create"] = CreatedSubmission(
                "submission-1", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Canceled)
        };
        FakeEmailSubmissionService partition = new FakeEmailSubmissionService();

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessSetInvocation(method.CreateSetInvocation(request, "s1"), Session(true), state,
                new FakeEmailService(), new FakeIdentityService(), null,
                new JmapRequestContext(), SetOptions(10), CancellationToken.None))!;
        JmapProtocolException transitionError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessSetInvocation(method.CreateSetInvocation(request, "s2"), Session(true),
                transition, new FakeEmailService(), new FakeIdentityService(), null,
                new JmapRequestContext(), SetOptions(10), CancellationToken.None))!;
        JmapProtocolException partitionError = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessSetInvocation(method.CreateSetInvocation(request, "s3"), Session(true),
                partition, new FakeEmailService(), new FakeIdentityService(), null,
                new JmapRequestContext(), SetOptions(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo(JmapProperties.OldState));
            Assert.That(transitionError.Path, Is.EqualTo(JmapProperties.Created));
            Assert.That(partitionError.Path, Is.EqualTo(JmapProperties.Created));
        }
    }

    [Test]
    public void SetRunsOneImplicitEmailSetForSuccessfulActionsAfterTheSubmissionResponse()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        submissions.GetResponse.List.Add(StoredSubmission(
            "update-success", JmapSubmissionUndoStatus.Pending, "email-update"));
        submissions.GetResponse.List.Add(StoredSubmission(
            "update-fail", JmapSubmissionUndoStatus.Pending, "email-update-fail"));
        submissions.GetResponse.List.Add(StoredSubmission(
            "destroy-success", JmapSubmissionUndoStatus.Final, "email-destroy"));
        submissions.GetResponse.List.Add(StoredSubmission(
            "destroy-fail", JmapSubmissionUndoStatus.Final, "email-destroy-fail"));
        submissions.SetResponse.Created = new Dictionary<string, EmailSubmission>
        {
            ["create-success"] = CreatedSubmission(
                "submission-created", "2026-09-11T12:00:00Z", JmapSubmissionUndoStatus.Final)
        };
        submissions.SetResponse.Created["create-success"].EmailId = "email-created";
        submissions.SetResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["create-fail"] = SetFailure(JmapErrorCodes.Forbidden)
        };
        submissions.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["update-success"] = Json("{\"undoStatus\":\"canceled\"}")
        };
        submissions.SetResponse.NotUpdated = new Dictionary<string, JmapSetError>
        {
            ["update-fail"] = SetFailure(JmapErrorCodes.CannotUnsend)
        };
        submissions.SetResponse.Destroyed = new List<string> { "destroy-success" };
        submissions.SetResponse.NotDestroyed = new Dictionary<string, JmapSetError>
        {
            ["destroy-fail"] = SetFailure(JmapErrorCodes.Forbidden)
        };
        FakeEmailService emails = new FakeEmailService();
        emails.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["email-created"] = Json("null"),
            ["email-update"] = Json("null")
        };
        emails.SetResponse.Destroyed = new List<string> { "email-destroy" };
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["create-success"] = new EmailSubmission
                {
                    IdentityId = "identity-1",
                    EmailId = "#draft"
                },
                ["create-fail"] = new EmailSubmission
                {
                    IdentityId = "identity-1",
                    EmailId = "email-created-fail"
                }
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["update-success"] = Patch(JmapProperties.UndoStatus, Json("\"canceled\"")),
                ["update-fail"] = Patch(JmapProperties.UndoStatus, Json("\"canceled\""))
            },
            Destroy = new List<string> { "destroy-success", "destroy-fail" },
            OnSuccessUpdateEmail = new Dictionary<string, JmapPatchObject>
            {
                ["#create-success"] = Patch("keywords/$draft", Json("null")),
                ["#create-fail"] = Patch(JmapProperties.Subject, Json("\"ignored\"")),
                ["update-success"] = Patch("keywords/$flagged", Json("true")),
                ["update-fail"] = Patch(JmapProperties.Subject, Json("\"ignored\""))
            },
            OnSuccessDestroyEmail = new List<string> { "destroy-success", "destroy-fail" }
        };

        EmailSubmissionSetDispatchResult result = method.ProcessSetInvocationResult(
            method.CreateSetInvocation(request, "s1"), Session(true), submissions, emails,
            new FakeIdentityService(), null, new JmapRequestContext(), SetOptions(20),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.MethodResponses.Select(value => value.Name), Is.EqualTo(new[]
            {
                JmapMethods.EmailSubmissionSet,
                JmapMethods.EmailSet
            }));
            Assert.That(result.MethodResponses.Select(value => value.CallId),
                Is.EqualTo(new[] { "s1", "s1" }));
            Assert.That(result.MethodResponses[0].Arguments.GetProperty(JmapProperties.Created)
                .TryGetProperty("create-success", out JsonElement _), Is.True);
            Assert.That(result.MethodResponses[1].Arguments.GetProperty(JmapProperties.Updated)
                .TryGetProperty("email-created", out JsonElement _), Is.True);
            Assert.That(result.EmailRequest!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.EmailRequest.IfInState, Is.Null);
            Assert.That(result.EmailRequest.Update!.Keys,
                Is.EquivalentTo(new[] { "email-created", "email-update" }));
            Assert.That(result.EmailRequest.Update["email-created"].Values["keywords/$draft"]
                .ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(result.EmailRequest.Update["email-update"].Values["keywords/$flagged"]
                .GetBoolean(), Is.True);
            Assert.That(result.EmailRequest.Destroy, Is.EqualTo(new[] { "email-destroy" }));
            Assert.That(result.EmailResponse, Is.SameAs(emails.SetResponse));
            Assert.That(emails.SetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void SetOmitsTheImplicitResponseWhenNoReferencedSubmissionActionSucceeds()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        submissions.SetResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["failed"] = SetFailure(JmapErrorCodes.Forbidden)
        };
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["failed"] = new EmailSubmission
                {
                    IdentityId = "identity-1",
                    EmailId = "email-1"
                }
            },
            OnSuccessUpdateEmail = new Dictionary<string, JmapPatchObject>
            {
                ["#failed"] = Patch(JmapProperties.Subject, Json("\"ignored\""))
            },
            OnSuccessDestroyEmail = new List<string> { "unrequested-submission" }
        };
        FakeEmailService emails = new FakeEmailService();

        EmailSubmissionSetDispatchResult result = method.ProcessSetInvocationResult(
            method.CreateSetInvocation(request, "s1"), Session(true), submissions, emails,
            new FakeIdentityService(), null, new JmapRequestContext(), SetOptions(10),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.MethodResponses, Has.Count.EqualTo(1));
            Assert.That(result.MethodResponses[0].Name, Is.EqualTo(JmapMethods.EmailSubmissionSet));
            Assert.That(result.EmailRequest, Is.Null);
            Assert.That(result.EmailResponse, Is.Null);
            Assert.That(emails.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRejectsInvalidImplicitEmailReferencesAndPatchesBeforeMutation()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService badReferenceService = new FakeEmailSubmissionService();
        EmailSubmissionSetDispatchResult badReference = method.ProcessSetInvocationResult(
            RawInvocation(JmapMethods.EmailSubmissionSet, "s1",
                "{\"accountId\":\"account-1\",\"onSuccessUpdateEmail\":{\"bad/id\":{\"subject\":\"x\"}}}"),
            Session(true), badReferenceService, new FakeEmailService(), new FakeIdentityService(),
            null, new JmapRequestContext(), SetOptions(10), CancellationToken.None);
        FakeEmailSubmissionService badPatchService = new FakeEmailSubmissionService();
        EmailSubmissionSetDispatchResult badPatch = method.ProcessSetInvocationResult(
            RawInvocation(JmapMethods.EmailSubmissionSet, "s2",
                "{\"accountId\":\"account-1\",\"onSuccessUpdateEmail\":{\"submission-1\":{\"keywords\":{},\"keywords/$draft\":null}}}"),
            Session(true), badPatchService, new FakeEmailService(), new FakeIdentityService(),
            null, new JmapRequestContext(), SetOptions(10), CancellationToken.None);
        FakeEmailSubmissionService duplicateService = new FakeEmailSubmissionService();
        EmailSubmissionSetDispatchResult duplicate = method.ProcessSetInvocationResult(
            RawInvocation(JmapMethods.EmailSubmissionSet, "s3",
                "{\"accountId\":\"account-1\",\"onSuccessDestroyEmail\":[\"submission-1\",\"submission-1\"]}"),
            Session(true), duplicateService, new FakeEmailService(), new FakeIdentityService(),
            null, new JmapRequestContext(), SetOptions(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, badReference.MethodResponses[0]).Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, badPatch.MethodResponses[0]).Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, duplicate.MethodResponses[0]).Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(badReferenceService.GetCalls + badPatchService.GetCalls +
                duplicateService.GetCalls, Is.Zero);
            Assert.That(badReferenceService.SetCalls + badPatchService.SetCalls +
                duplicateService.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetClassifiesMissingReferencesSenderRecipientsAndDelayedSendBeforeMutation()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        FakeEmailService emails = new FakeEmailService();
        emails.GetResponse = new EmailGetResponse
        {
            AccountId = "account-1",
            State = "email-state",
            List = new List<Email>
            {
                new Email { Id = "invalid-email" },
                SendableEmail("forbidden-from", "other@example.test", "recipient@example.test"),
                SendableEmail("invalid-recipient", "sender@example.test", "recipient@example.test"),
                SendableEmail("no-recipient", "sender@example.test", "recipient@example.test"),
                SendableEmail("forbidden-mail-from", "sender@example.test", "recipient@example.test"),
                SendableEmail("delayed-for", "sender@example.test", "recipient@example.test"),
                SendableEmail("delayed-until", "sender@example.test", "recipient@example.test")
            },
            NotFound = new List<string> { "missing-email" }
        };
        FakeIdentityService identities = new FakeIdentityService();
        identities.GetResponse = new IdentityGetResponse
        {
            AccountId = "account-1",
            State = "identity-state",
            List = new List<Identity>
            {
                new Identity { Id = "identity-1", Email = "sender@example.test" }
            },
            NotFound = new List<string> { "missing-identity" }
        };
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["missing-email"] = Submission("identity-1", "missing-email", null),
                ["missing-identity"] = Submission("missing-identity", "invalid-recipient", null),
                ["invalid-email"] = Submission("identity-1", "invalid-email", null),
                ["forbidden-from"] = Submission("identity-1", "forbidden-from", null),
                ["invalid-recipient"] = Submission("identity-1", "invalid-recipient",
                    Envelope("sender@example.test", "not-an-address", null, null)),
                ["no-recipient"] = Submission("identity-1", "no-recipient",
                    Envelope("sender@example.test", null, null, null)),
                ["forbidden-mail-from"] = Submission("identity-1", "forbidden-mail-from",
                    Envelope("other@example.test", "recipient@example.test", null, null)),
                ["delayed-for"] = Submission("identity-1", "delayed-for",
                    Envelope("sender@example.test", "recipient@example.test", "HOLDFOR", "3601")),
                ["delayed-until"] = Submission("identity-1", "delayed-until",
                    Envelope("sender@example.test", "recipient@example.test", "HOLDUNTIL",
                        "20260911130001Z"))
            }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), submissions, emails,
            identities, new FixedClock(), new JmapRequestContext(), SetOptions(20),
            CancellationToken.None);
        EmailSubmissionSetResponse response = method.Codec.DecodeSubmissionSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["missing-email"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["missing-email"].Properties,
                Is.EqualTo(new[] { JmapProperties.EmailId }));
            Assert.That(response.NotCreated["missing-identity"].Properties,
                Is.EqualTo(new[] { JmapProperties.IdentityId }));
            Assert.That(response.NotCreated["invalid-email"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidEmail));
            Assert.That(response.NotCreated["invalid-email"].Properties,
                Is.EqualTo(new[] { JmapProperties.From }));
            Assert.That(response.NotCreated["forbidden-from"].Kind,
                Is.EqualTo(JmapSetErrorKind.ForbiddenFrom));
            Assert.That(response.NotCreated["invalid-recipient"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidRecipients));
            Assert.That(response.NotCreated["invalid-recipient"].InvalidRecipients,
                Is.EqualTo(new[] { "not-an-address" }));
            Assert.That(response.NotCreated["no-recipient"].Kind,
                Is.EqualTo(JmapSetErrorKind.NoRecipients));
            Assert.That(response.NotCreated["forbidden-mail-from"].Kind,
                Is.EqualTo(JmapSetErrorKind.ForbiddenMailFrom));
            Assert.That(response.NotCreated["delayed-for"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["delayed-for"].Properties,
                Does.Contain("envelope/mailFrom/parameters/HOLDFOR"));
            Assert.That(response.NotCreated["delayed-until"].Properties,
                Does.Contain("envelope/mailFrom/parameters/HOLDUNTIL"));
            Assert.That(submissions.SetCalls, Is.Zero);
            Assert.That(emails.GetCalls, Is.EqualTo(1));
            Assert.That(identities.GetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void SetReturnsCannotUnsendWithoutCallingTheBackendWhenSubmissionIsFinal()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        FakeEmailSubmissionService submissions = new FakeEmailSubmissionService();
        submissions.GetResponse.List.Add(StoredSubmission(
            "submission-final", JmapSubmissionUndoStatus.Final));
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["submission-final"] = Patch(JmapProperties.UndoStatus, Json("\"canceled\""))
            }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), submissions,
            new FakeEmailService(), new FakeIdentityService(), null,
            new JmapRequestContext(), SetOptions(10), CancellationToken.None);
        EmailSubmissionSetResponse response = method.Codec.DecodeSubmissionSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotUpdated!["submission-final"].Kind,
                Is.EqualTo(JmapSetErrorKind.CannotUnsend));
            Assert.That(submissions.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetAcceptsEveryStandardAndSubmissionSpecificErrorInItsDefinedPartition()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionSetRequest request = SetErrorRequest();
        EmailSubmissionSetResponse response = new EmailSubmissionSetResponse
        {
            AccountId = "account-1",
            NewState = "new-state",
            NotCreated = new Dictionary<string, JmapSetError>
            {
                ["forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["quota"] = SetFailure(JmapErrorCodes.OverQuota),
                ["large"] = SetFailureWithMaxSize(1000L),
                ["rate"] = SetFailure(JmapErrorCodes.RateLimit),
                ["properties"] = SetFailureWithProperties(JmapErrorCodes.InvalidProperties, "emailId"),
                ["email"] = SetFailureWithProperties(JmapErrorCodes.InvalidEmail, "from"),
                ["many"] = SetFailureWithMaxRecipients(50L),
                ["none"] = SetFailure(JmapErrorCodes.NoRecipients),
                ["recipients"] = SetFailureWithRecipients("bad"),
                ["mail-from"] = SetFailure(JmapErrorCodes.ForbiddenMailFrom),
                ["from"] = SetFailure(JmapErrorCodes.ForbiddenFrom),
                ["send"] = new JmapSetError
                {
                    Type = JmapErrorCodes.ForbiddenToSend,
                    Description = "Sending is temporarily disabled."
                }
            },
            NotUpdated = new Dictionary<string, JmapSetError>
            {
                ["update-forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["update-missing"] = SetFailure(JmapErrorCodes.NotFound),
                ["update-patch"] = SetFailure(JmapErrorCodes.InvalidPatch),
                ["update-destroy"] = SetFailure(JmapErrorCodes.WillDestroy),
                ["update-properties"] = SetFailureWithProperties(
                    JmapErrorCodes.InvalidProperties, JmapProperties.UndoStatus),
                ["update-unsend"] = SetFailure(JmapErrorCodes.CannotUnsend)
            },
            NotDestroyed = new Dictionary<string, JmapSetError>
            {
                ["destroy-forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["destroy-missing"] = SetFailure(JmapErrorCodes.NotFound)
            }
        };

        Assert.DoesNotThrow(() => method.ValidateSetResponse(response, request));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated["email"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidEmail));
            Assert.That(response.NotCreated["many"].Kind,
                Is.EqualTo(JmapSetErrorKind.TooManyRecipients));
            Assert.That(response.NotUpdated["update-unsend"].Kind,
                Is.EqualTo(JmapSetErrorKind.CannotUnsend));
        }
    }

    [Test]
    public void SetRejectsMissingSubmissionErrorDetailsAndErrorsInWrongPartitions()
    {
        EmailSubmissionMethod method = new EmailSubmissionMethod();
        EmailSubmissionSetRequest createRequest = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["item"] = Submission("identity-1", "email-1", null)
            }
        };

        JmapProtocolException missingSize = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(FailedCreate("item", SetFailure(JmapErrorCodes.TooLarge)),
                createRequest))!;
        JmapProtocolException missingMaximum = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(FailedCreate(
                "item", SetFailure(JmapErrorCodes.TooManyRecipients)), createRequest))!;
        JmapProtocolException missingRecipients = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(FailedCreate(
                "item", SetFailure(JmapErrorCodes.InvalidRecipients)), createRequest))!;
        JmapProtocolException wrongPartition = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(FailedCreate(
                "item", SetFailure(JmapErrorCodes.CannotUnsend)), createRequest))!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingSize.Path, Does.EndWith(JmapProperties.MaxSize));
            Assert.That(missingMaximum.Path, Does.EndWith(JmapProperties.MaxRecipients));
            Assert.That(missingRecipients.Path, Does.EndWith(JmapProperties.InvalidRecipients));
            Assert.That(wrongPartition.Path, Does.EndWith(JmapProperties.Type));
        }
    }

    private static JmapPatchObject Patch(string path, JsonElement value)
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values[path] = value;
        return patch;
    }

    private static JmapSetError SetFailure(string type)
    {
        return new JmapSetError { Type = type };
    }

    private static JmapSetError SetFailureWithProperties(string type, string property)
    {
        return new JmapSetError
        {
            Type = type,
            Properties = new List<string> { property }
        };
    }

    private static JmapSetError SetFailureWithMaxSize(long maximum)
    {
        return new JmapSetError
        {
            Type = JmapErrorCodes.TooLarge,
            MaxSize = maximum,
            HasMaxSize = true
        };
    }

    private static JmapSetError SetFailureWithMaxRecipients(long maximum)
    {
        return new JmapSetError
        {
            Type = JmapErrorCodes.TooManyRecipients,
            MaxRecipients = maximum,
            HasMaxRecipients = true
        };
    }

    private static JmapSetError SetFailureWithRecipients(string recipient)
    {
        return new JmapSetError
        {
            Type = JmapErrorCodes.InvalidRecipients,
            InvalidRecipients = new List<string> { recipient }
        };
    }

    private static EmailSubmissionSetResponse FailedCreate(string creationId, JmapSetError error)
    {
        return new EmailSubmissionSetResponse
        {
            AccountId = "account-1",
            NewState = "new-state",
            NotCreated = new Dictionary<string, JmapSetError> { [creationId] = error }
        };
    }

    private static EmailSubmission Submission(
        string identityId,
        string emailId,
        EmailSubmissionEnvelope? envelope)
    {
        return new EmailSubmission
        {
            IdentityId = identityId,
            EmailId = emailId,
            Envelope = envelope
        };
    }

    private static EmailSubmissionEnvelope Envelope(
        string mailFrom,
        string? recipient,
        string? parameter,
        string? value)
    {
        EmailSubmissionAddress from = new EmailSubmissionAddress { Email = mailFrom };
        if (parameter != null)
        {
            from.Parameters = new Dictionary<string, JsonElement>
            {
                [parameter] = value == null ? Json("null") : Json("\"" + value + "\"")
            };
        }

        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope { MailFrom = from };
        if (recipient != null)
        {
            envelope.RcptTo.Add(new EmailSubmissionAddress { Email = recipient });
        }

        return envelope;
    }

    private static EmailSubmissionSetRequest SetErrorRequest()
    {
        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, EmailSubmission>(),
            Update = new Dictionary<string, JmapPatchObject>(),
            Destroy = new List<string> { "destroy-forbidden", "destroy-missing" }
        };
        string[] creates =
        {
            "forbidden", "quota", "large", "rate", "properties", "email", "many",
            "none", "recipients", "mail-from", "from", "send"
        };
        foreach (string key in creates)
        {
            request.Create[key] = Submission("identity-1", "email-1", null);
        }

        string[] updates =
        {
            "update-forbidden", "update-missing", "update-patch", "update-destroy",
            "update-properties", "update-unsend"
        };
        foreach (string key in updates)
        {
            request.Update[key] = Patch(JmapProperties.UndoStatus, Json("\"canceled\""));
        }

        return request;
    }

    private static EmailSubmission StoredSubmission(string id, JmapSubmissionUndoStatus status)
    {
        return StoredSubmission(id, status, "email-" + id);
    }

    private static EmailSubmission StoredSubmission(
        string id,
        JmapSubmissionUndoStatus status,
        string emailId)
    {
        return new EmailSubmission
        {
            Id = id,
            EmailId = emailId,
            UndoStatus = status,
            HasUndoStatus = true
        };
    }

    private static EmailSubmission CreatedSubmission(
        string id,
        string sendAt,
        JmapSubmissionUndoStatus status)
    {
        return new EmailSubmission
        {
            Id = id,
            SendAt = sendAt,
            UndoStatus = status,
            HasUndoStatus = true
        };
    }

    private static EmailSubmission FullSubmission(string id, string delivered)
    {
        EmailSubmissionAddress mailFrom = new EmailSubmissionAddress
        {
            Email = "sender@example.test",
            Parameters = new Dictionary<string, JsonElement>
            {
                ["SMTPUTF8"] = Json("null")
            }
        };
        EmailSubmissionAddress recipient = new EmailSubmissionAddress
        {
            Email = "recipient@example.test",
            Parameters = new Dictionary<string, JsonElement>
            {
                ["NOTIFY"] = Json("\"FAILURE\"")
            }
        };
        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope { MailFrom = mailFrom };
        envelope.RcptTo.Add(recipient);
        EmailDeliveryStatus status = new EmailDeliveryStatus
        {
            SmtpReply = "250 2.0.0 accepted",
            Delivered = delivered,
            Displayed = "unknown"
        };
        EmailSubmission submission = new EmailSubmission
        {
            Id = id,
            IdentityId = "identity-1",
            EmailId = "email-1",
            ThreadId = "thread-1",
            Envelope = envelope,
            SendAt = "2026-09-11T12:00:00Z",
            UndoStatus = JmapSubmissionUndoStatus.Final,
            HasUndoStatus = true,
            DeliveryStatus = new Dictionary<string, EmailDeliveryStatus>(),
            DsnBlobIds = new List<string> { "dsn-1", "dsn-2" },
            MdnBlobIds = new List<string> { "mdn-1" }
        };
        submission.DeliveryStatus["recipient@example.test"] = status;
        return submission;
    }

    private static JmapSession Session(bool includeAccount)
    {
        JmapSession session = new JmapSession();
        if (includeAccount)
        {
            JmapAccount account = new JmapAccount();
            JmapCapability submission = new JmapCapability();
            submission.Submission = new JmapSubmissionCapabilities();
            submission.Submission.MaxDelayedSend = 3600L;
            submission.Submission.SubmissionExtensions["FUTURERELEASE"] =
                new List<string> { "T", "+" };
            account.AccountCapabilities.Add(JmapCapabilities.Submission, submission);
            session.Accounts.Add("account-1", account);
        }

        return session;
    }

    private static JmapSession MailOnlySession()
    {
        JmapSession session = new JmapSession();
        JmapAccount account = new JmapAccount();
        JmapCapability mail = new JmapCapability();
        mail.Mail = new JmapMailCapabilities();
        account.AccountCapabilities.Add(JmapCapabilities.Mail, mail);
        session.Accounts.Add("account-1", account);
        return session;
    }

    private static JmapServerOptions Options(int maximumObjectsInGet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInGet = maximumObjectsInGet;
        return options;
    }

    private static JmapServerOptions SetOptions(int maximumObjectsInSet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInSet = maximumObjectsInSet;
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

    private static JmapMethodError Error(EmailSubmissionMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeEmailSubmissionService : IEmailSubmissionService
    {
        public EmailSubmissionGetResponse GetResponse = new EmailSubmissionGetResponse
        {
            AccountId = "account-1",
            State = "submission-state"
        };

        public EmailSubmissionChangesResponse ChangesResponse = new EmailSubmissionChangesResponse
        {
            AccountId = "account-1",
            OldState = "old-state",
            NewState = "new-state"
        };

        public EmailSubmissionQueryResponse QueryResponse = new EmailSubmissionQueryResponse
        {
            AccountId = "account-1",
            QueryState = "query-state"
        };

        public EmailSubmissionQueryChangesResponse QueryChangesResponse =
            new EmailSubmissionQueryChangesResponse
            {
                AccountId = "account-1",
                OldQueryState = "q0",
                NewQueryState = "q1"
            };

        public EmailSubmissionSetResponse SetResponse = new EmailSubmissionSetResponse
        {
            AccountId = "account-1",
            OldState = "submission-state",
            NewState = "submission-state-2"
        };

        public int GetCalls;
        public int ChangesCalls;
        public int QueryCalls;
        public int QueryChangesCalls;
        public int SetCalls;
        public EmailSubmissionGetRequest? LastGetRequest;
        public EmailSubmissionChangesRequest? LastChangesRequest;
        public EmailSubmissionQueryRequest? LastQueryRequest;
        public EmailSubmissionQueryChangesRequest? LastQueryChangesRequest;
        public EmailSubmissionSetRequest? LastSetRequest;

        public Task<EmailSubmissionGetResponse> GetStoredEmailSubmissionsAsync(
            EmailSubmissionGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            return Task.FromResult(GetResponse);
        }

        public Task<EmailSubmissionChangesResponse> GetStoredEmailSubmissionChangesAsync(
            EmailSubmissionChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ChangesCalls = ChangesCalls + 1;
            LastChangesRequest = request;
            return Task.FromResult(ChangesResponse);
        }

        public Task<EmailSubmissionQueryResponse> QueryStoredEmailSubmissionsAsync(
            EmailSubmissionQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryCalls = QueryCalls + 1;
            LastQueryRequest = request;
            return Task.FromResult(QueryResponse);
        }

        public Task<EmailSubmissionQueryChangesResponse> GetStoredEmailSubmissionQueryChangesAsync(
            EmailSubmissionQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryChangesCalls = QueryChangesCalls + 1;
            LastQueryChangesRequest = request;
            return Task.FromResult(QueryChangesResponse);
        }

        public Task<EmailSubmissionSetResponse> SetStoredEmailSubmissionsAsync(
            EmailSubmissionSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }
    }

    private class FakeEmailService : IEmailService
    {
        public EmailGetResponse? GetResponse;
        public EmailSetResponse SetResponse = new EmailSetResponse
        {
            AccountId = "account-1",
            OldState = "email-state",
            NewState = "email-state-2"
        };

        public int SetCalls;
        public int GetCalls;
        public EmailGetRequest? LastGetRequest;
        public EmailSetRequest? LastSetRequest;

        public Task<EmailGetResponse> GetStoredEmailsAsync(
            EmailGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            if (GetResponse != null)
            {
                return Task.FromResult(GetResponse);
            }

            EmailGetResponse response = new EmailGetResponse
            {
                AccountId = request.AccountId,
                State = "email-state"
            };
            if (request.Ids != null)
            {
                foreach (string id in request.Ids)
                {
                    response.List.Add(SendableEmail(id, "sender@example.test", "recipient@example.test"));
                }
            }

            return Task.FromResult(response);
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
            return Task.FromResult(SetResponse);
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

    private class FakeIdentityService : IIdentityService
    {
        public IdentityGetResponse? GetResponse;
        public int GetCalls;
        public IdentityGetRequest? LastGetRequest;

        public Task<IdentityGetResponse> GetStoredIdentitiesAsync(
            IdentityGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            if (GetResponse != null)
            {
                return Task.FromResult(GetResponse);
            }

            IdentityGetResponse response = new IdentityGetResponse
            {
                AccountId = request.AccountId,
                State = "identity-state"
            };
            if (request.Ids != null)
            {
                foreach (string id in request.Ids)
                {
                    response.List.Add(new Identity
                    {
                        Id = id,
                        Email = "sender@example.test"
                    });
                }
            }

            return Task.FromResult(response);
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

    private class FixedClock : IJmapClock
    {
        public DateTimeOffset GetCurrentUtcDateTime()
        {
            return new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
        }
    }

    private static Email SendableEmail(string id, string from, string recipient)
    {
        return new Email
        {
            Id = id,
            From = new List<EmailAddress> { new EmailAddress { Email = from } },
            To = new List<EmailAddress> { new EmailAddress { Email = recipient } }
        };
    }
}
