using System.Text.Json;

namespace jmap.Tests;

public class MailboxMethodTests
{
    [Test]
    public void GetDispatchOrdersRequestedIdsAndReturnsCompleteDefaultData()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.GetResponse.List.Add(StoredMailbox("m2", "Archive", 4));
        service.GetResponse.List.Add(StoredMailbox("m1", "Inbox", 9));
        service.GetResponse.NotFound.Add("missing");
        MailboxGetRequest request = new MailboxGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "m1", "missing", "m2", "m1" }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"),
            Session(true),
            service,
            new JmapRequestContext(),
            Options(10),
            CancellationToken.None);
        MailboxGetResponse response = method.Codec.DecodeMailboxGetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.MailboxGet));
            Assert.That(response.List.Select(mailbox => mailbox.Id), Is.EqualTo(new[] { "m1", "m2" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(response.List[0].Name, Is.EqualTo("Inbox"));
            Assert.That(response.List[0].TotalEmails, Is.EqualTo(9));
            Assert.That(response.List[0].MyRights, Is.Not.Null);
            Assert.That(response.List[0].UnknownProperties["vendorColor"].GetString(), Is.EqualTo("blue"));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.LastGetRequest!.Ids, Is.EqualTo(request.Ids));
        }
    }

    [Test]
    public void GetDispatchProjectsOnlyRequestedPropertiesAndAlwaysIncludesId()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.GetResponse.List.Add(StoredMailbox("m1", "Inbox", 0));
        MailboxGetRequest request = new MailboxGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.TotalEmails, JmapProperties.IsSubscribed }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g2"),
            Session(true),
            service,
            new JmapRequestContext(),
            Options(10),
            CancellationToken.None);
        JsonElement mailbox = invocation.Arguments.GetProperty(JmapProperties.List)[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mailbox.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.TotalEmails, JmapProperties.IsSubscribed }));
            Assert.That(mailbox.GetProperty(JmapProperties.TotalEmails).GetInt64(), Is.Zero);
            Assert.That(mailbox.GetProperty(JmapProperties.IsSubscribed).GetBoolean(), Is.False);
            Assert.That(service.LastGetRequest!.Properties, Is.Null);
            Assert.That(request.Properties, Is.EqualTo(new[] { JmapProperties.TotalEmails, JmapProperties.IsSubscribed }));
        }
    }

    [Test]
    public void GetRejectsInvalidArgumentsAndEnforcesAllObjectLimit()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        MailboxGetRequest unsupported = new MailboxGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { "future" }
        };
        MailboxGetRequest tooMany = new MailboxGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "m1", "m2" }
        };

        JmapInvocation invalid = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.MailboxGet, "g1", "{\"accountId\":\"account-1\",\"properties\":[\"future\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation oversizedIds = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.MailboxGet, "g2", EncodeGet(method, tooMany)),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);
        service.GetResponse.List.Add(StoredMailbox("m1", "One", 0));
        service.GetResponse.List.Add(StoredMailbox("m2", "Two", 0));
        JmapInvocation oversizedAll = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.MailboxGet, "g3", "{\"accountId\":\"account-1\"}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalid).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, oversizedIds).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, oversizedAll).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(unsupported.Properties, Is.EqualTo(new[] { "future" }));
        }
    }

    [Test]
    public void ReadMethodsReturnAccountErrorsBeforeCallingTheBackend()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        MailboxGetRequest request = new MailboxGetRequest { AccountId = "account-1" };

        JmapInvocation missing = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(false), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g2"), SessionWithoutMail(), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, missing).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, unsupported).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(service.GetCalls, Is.Zero);
        }
    }

    [Test]
    public void ChangesDispatchPreservesDisjointClassificationsAndCountProperties()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.ChangesResponse.Created.Add("created");
        service.ChangesResponse.Updated.Add("updated");
        service.ChangesResponse.Destroyed.Add("destroyed");
        service.ChangesResponse.UpdatedProperties = null;
        MailboxChangesRequest broad = ChangesRequest();

        JmapInvocation broadInvocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(broad, "c1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxChangesResponse broadResponse = method.Codec.DecodeMailboxChangesResponse(broadInvocation.Arguments);

        service.ChangesResponse.Created.Clear();
        service.ChangesResponse.Destroyed.Clear();
        service.ChangesResponse.UpdatedProperties = new List<string>
        {
            JmapProperties.TotalEmails,
            JmapProperties.UnreadThreads
        };
        JmapInvocation countInvocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxChangesResponse countResponse = method.Codec.DecodeMailboxChangesResponse(countInvocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(broadResponse.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(broadResponse.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(broadResponse.Destroyed, Is.EqualTo(new[] { "destroyed" }));
            Assert.That(broadResponse.UpdatedProperties, Is.Null);
            Assert.That(countResponse.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(countResponse.UpdatedProperties,
                Is.EqualTo(new[] { JmapProperties.TotalEmails, JmapProperties.UnreadThreads }));
            Assert.That(service.ChangesCalls, Is.EqualTo(2));
        }
    }

    [Test]
    public void ChangesRejectsInvalidArgumentsWithoutCallingTheBackend()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();

        JmapInvocation zero = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.MailboxChanges, "c1", "{\"accountId\":\"account-1\",\"sinceState\":\"s0\",\"maxChanges\":0}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation fraction = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.MailboxChanges, "c2", "{\"accountId\":\"account-1\",\"sinceState\":\"s0\",\"maxChanges\":1.5}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation missingState = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.MailboxChanges, "c3", "{\"accountId\":\"account-1\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, zero).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, fraction).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, missingState).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(service.ChangesCalls, Is.Zero);
        }
    }

    [Test]
    public void ChangesRejectsOverlappingClassificationsAndInvalidCountMetadata()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService overlap = new FakeMailboxService();
        overlap.ChangesResponse.Created.Add("m1");
        overlap.ChangesResponse.Updated.Add("m1");
        FakeMailboxService duplicateMetadata = new FakeMailboxService();
        duplicateMetadata.ChangesResponse.Updated.Add("m1");
        duplicateMetadata.ChangesResponse.UpdatedProperties = new List<string>
        {
            JmapProperties.TotalEmails,
            JmapProperties.TotalEmails
        };
        FakeMailboxService invalidProperty = new FakeMailboxService();
        invalidProperty.ChangesResponse.Updated.Add("m1");
        invalidProperty.ChangesResponse.UpdatedProperties = new List<string> { JmapProperties.Name };

        JmapProtocolException overlapError = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c1"), Session(true), overlap,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c2"), Session(true), duplicateMetadata,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException propertyError = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c3"), Session(true), invalidProperty,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(overlapError.Path, Is.EqualTo("updated"));
            Assert.That(duplicateError.Path, Is.EqualTo("updatedProperties"));
            Assert.That(propertyError.Path, Is.EqualTo("updatedProperties"));
        }
    }

    [Test]
    public void ChangesValidatesStatesAndMaximumAgainstTheBackendResponse()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService wrongState = new FakeMailboxService();
        wrongState.ChangesResponse.OldState = "other";
        FakeMailboxService tooMany = new FakeMailboxService();
        tooMany.ChangesResponse.Created.Add("m1");
        tooMany.ChangesResponse.Updated.Add("m2");
        MailboxChangesRequest limited = ChangesRequest();
        limited.HasMaxChanges = true;
        limited.MaxChanges = 1;

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(ChangesRequest(), "c1"), Session(true), wrongState,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException limitError = Assert.Throws<JmapProtocolException>(() => method.ProcessChangesInvocation(
            method.CreateChangesInvocation(limited, "c2"), Session(true), tooMany,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(limitError.Path, Is.EqualTo("methodResponses/1"));
        }
    }

    [Test]
    public void ClientMappingEnforcesProjectionAndMapsMethodErrors()
    {
        MailboxMethod method = new MailboxMethod();
        MailboxGetRequest request = new MailboxGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "m1" },
            Properties = new List<string> { JmapProperties.Name }
        };
        JmapResponse success = Response(
            JmapMethods.MailboxGet,
            "g1",
            "{\"accountId\":\"account-1\",\"state\":\"s1\",\"list\":[{\"id\":\"m1\",\"name\":\"Inbox\"}],\"notFound\":[]}");
        JmapResponse extra = Response(
            JmapMethods.MailboxGet,
            "g1",
            "{\"accountId\":\"account-1\",\"state\":\"s1\",\"list\":[{\"id\":\"m1\",\"name\":\"Inbox\",\"role\":\"inbox\"}],\"notFound\":[]}");
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "g1",
            "{\"type\":\"accountNotFound\",\"description\":\"gone\"}");

        MailboxGetResponse mapped = method.MapGetResponse(success, "g1", request);
        JmapProtocolException projectionError = Assert.Throws<JmapProtocolException>(
            () => method.MapGetResponse(extra, "g1", request))!;
        JmapMethodException methodError = Assert.Throws<JmapMethodException>(
            () => method.MapGetResponse(failed, "g1", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.List[0].Id, Is.EqualTo("m1"));
            Assert.That(mapped.List[0].Name, Is.EqualTo("Inbox"));
            Assert.That(projectionError.Path, Is.EqualTo("list/0/role"));
            Assert.That(methodError.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
        }
    }

    [Test]
    public void QueryDispatchPreservesRecursiveFiltersSortsAndTreeModes()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.QueryResponse.Position = 1;
        service.QueryResponse.Ids.Add("child");
        service.QueryResponse.HasTotal = true;
        service.QueryResponse.Total = 3;
        MailboxFilter topLevel = new MailboxFilter { HasParentId = true };
        MailboxFilter subscribed = new MailboxFilter { HasIsSubscribed = true, IsSubscribed = true };
        MailboxFilter either = new MailboxFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.Or,
            Conditions = new List<MailboxFilter> { topLevel, subscribed }
        };
        MailboxQueryRequest request = new MailboxQueryRequest
        {
            AccountId = "account-1",
            Filter = either,
            Sort = new List<JmapComparator>
            {
                new JmapComparator
                {
                    Property = JmapProperties.Name,
                    HasIsAscending = true,
                    IsAscending = false,
                    Collation = "i;unicode-casemap"
                },
                new JmapComparator { Property = JmapProperties.SortOrder }
            },
            HasPosition = true,
            Position = 1,
            HasLimit = true,
            Limit = 2,
            HasCalculateTotal = true,
            CalculateTotal = true,
            HasFilterAsTree = true,
            FilterAsTree = true,
            HasSortAsTree = true,
            SortAsTree = true
        };

        JmapInvocation invocation = method.ProcessQueryInvocation(
            method.CreateQueryInvocation(request, "q1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxQueryResponse response = method.Codec.DecodeMailboxQueryResponse(invocation.Arguments);
        MailboxQueryRequest received = service.LastQueryRequest!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.MailboxQuery));
            Assert.That(response.Ids, Is.EqualTo(new[] { "child" }));
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(received.Filter!.Operator, Is.EqualTo(JmapFilterOperator.Or));
            Assert.That(received.Filter.Conditions![0].HasParentId, Is.True);
            Assert.That(received.Filter.Conditions[0].ParentId, Is.Null);
            Assert.That(received.Filter.Conditions[1].IsSubscribed, Is.True);
            Assert.That(received.Sort![0].Property, Is.EqualTo(JmapProperties.Name));
            Assert.That(received.Sort[0].IsAscending, Is.False);
            Assert.That(received.Sort[0].Collation, Is.EqualTo("i;unicode-casemap"));
            Assert.That(received.FilterAsTree, Is.True);
            Assert.That(received.SortAsTree, Is.True);
            Assert.That(service.QueryCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryDefaultsBothTreeModesToFalse()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();

        method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q1", "{\"accountId\":\"account-1\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.LastQueryRequest!.HasFilterAsTree, Is.False);
            Assert.That(service.LastQueryRequest.FilterAsTree, Is.False);
            Assert.That(service.LastQueryRequest.HasSortAsTree, Is.False);
            Assert.That(service.LastQueryRequest.SortAsTree, Is.False);
        }
    }

    [Test]
    public void QueryClassifiesMalformedAndUnsupportedFiltersCorrectly()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        JmapSession session = Session(true);

        JmapInvocation invalidParent = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q1", "{\"accountId\":\"account-1\",\"filter\":{\"parentId\":\"bad/id\"}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation nullName = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q2", "{\"accountId\":\"account-1\",\"filter\":{\"name\":null}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation mixedOperator = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q3", "{\"accountId\":\"account-1\",\"filter\":{\"operator\":\"AND\",\"conditions\":[],\"role\":null}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q4", "{\"accountId\":\"account-1\",\"filter\":{\"vendorPinned\":true}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalidParent).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, nullName).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, mixedOperator).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, unsupported).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(service.QueryCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryEnforcesConfiguredFilterDepth()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        JmapServerOptions options = Options(10);
        options.MaxJsonDepth = 2;
        string arguments = "{\"accountId\":\"account-1\",\"filter\":{\"operator\":\"AND\",\"conditions\":[{\"operator\":\"OR\",\"conditions\":[{\"name\":\"work\"}]}]}}";

        JmapInvocation invocation = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q1", arguments), Session(true), service,
            new JmapRequestContext(), options, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invocation).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(service.QueryCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryValidatesMailboxSortPropertiesExtensionsAndCollations()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        JmapSession session = Session(true);
        JmapInvocation unsupportedProperty = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q1", "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"role\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupportedExtension = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q2", "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"name\",\"vendor\":true}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupportedCollation = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q3", "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"name\",\"collation\":\"i;octet\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation numericCollation = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q4", "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"sortOrder\",\"collation\":\"i;octet\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, unsupportedProperty).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, unsupportedExtension).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, unsupportedCollation).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(numericCollation.Name, Is.EqualTo(JmapMethods.MailboxQuery));
            Assert.That(service.QueryCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryRejectsMalformedWindowsBeforeDispatch()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        JmapInvocation fraction = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q1", "{\"accountId\":\"account-1\",\"position\":1.5}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation badAnchor = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q2", "{\"accountId\":\"account-1\",\"anchor\":\"bad/id\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation negativeLimit = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.MailboxQuery, "q3", "{\"accountId\":\"account-1\",\"limit\":-1}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, fraction).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, badAnchor).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, negativeLimit).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(service.QueryCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryRejectsInvalidBackendResultPartitionsAndMetadata()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService duplicate = new FakeMailboxService();
        duplicate.QueryResponse.Ids.Add("m1");
        duplicate.QueryResponse.Ids.Add("m1");
        FakeMailboxService unexpectedTotal = new FakeMailboxService();
        unexpectedTotal.QueryResponse.HasTotal = true;
        unexpectedTotal.QueryResponse.Total = 0;
        FakeMailboxService wrongPosition = new FakeMailboxService();
        wrongPosition.QueryResponse.Position = 2;

        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryInvocation(
            method.CreateQueryInvocation(QueryRequest(), "q1"), Session(true), duplicate,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException totalError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryInvocation(
            method.CreateQueryInvocation(QueryRequest(), "q2"), Session(true), unexpectedTotal,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException positionError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryInvocation(
            method.CreateQueryInvocation(QueryRequest(), "q3"), Session(true), wrongPosition,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(duplicateError.Path, Is.EqualTo("ids/1"));
            Assert.That(totalError.Path, Is.EqualTo("total"));
            Assert.That(positionError.Path, Is.EqualTo("position"));
        }
    }

    [Test]
    public void QueryValidatesRequestedAndServerAppliedLimits()
    {
        MailboxMethod method = new MailboxMethod();
        MailboxQueryRequest request = QueryRequest();
        request.HasLimit = true;
        request.Limit = 2;
        FakeMailboxService valid = new FakeMailboxService();
        valid.QueryResponse.Ids.Add("m1");
        valid.QueryResponse.HasLimit = true;
        valid.QueryResponse.Limit = 1;
        FakeMailboxService oversized = new FakeMailboxService();
        oversized.QueryResponse.Ids.Add("m1");
        oversized.QueryResponse.Ids.Add("m2");
        oversized.QueryResponse.Ids.Add("m3");

        JmapInvocation invocation = method.ProcessQueryInvocation(
            method.CreateQueryInvocation(request, "q1"), Session(true), valid,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxQueryResponse response = method.Codec.DecodeMailboxQueryResponse(invocation.Arguments);
        JmapProtocolException limitError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryInvocation(
            method.CreateQueryInvocation(request, "q2"), Session(true), oversized,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.HasLimit, Is.True);
            Assert.That(response.Limit, Is.EqualTo(1));
            Assert.That(limitError.Path, Is.EqualTo("ids"));
        }
    }

    [Test]
    public void QueryClientMappingReturnsTypedResultsAndMapsErrors()
    {
        MailboxMethod method = new MailboxMethod();
        MailboxQueryRequest request = QueryRequest();
        request.HasCalculateTotal = true;
        request.CalculateTotal = true;
        JmapResponse success = Response(
            JmapMethods.MailboxQuery,
            "q1",
            "{\"accountId\":\"account-1\",\"queryState\":\"qs1\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[\"m1\"],\"total\":1}");
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "q1",
            "{\"type\":\"unsupportedFilter\",\"description\":\"nope\"}");

        MailboxQueryResponse response = method.MapQueryResponse(success, "q1", request);
        JmapMethodException error = Assert.Throws<JmapMethodException>(
            () => method.MapQueryResponse(failed, "q1", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.QueryState, Is.EqualTo("qs1"));
            Assert.That(response.Ids, Is.EqualTo(new[] { "m1" }));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
        }
    }

    [Test]
    public void QueryChangesDispatchPreservesCompleteTreeQueryIdentityAndIndexedMoves()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.QueryChangesResponse.Removed.Add("moved");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "moved", Index = 0 });
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "new", Index = 2 });
        service.QueryChangesResponse.HasTotal = true;
        service.QueryChangesResponse.Total = 3;
        MailboxFilter filter = new MailboxFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.And,
            Conditions = new List<MailboxFilter>
            {
                new MailboxFilter { Name = "work" },
                new MailboxFilter { HasIsSubscribed = true, IsSubscribed = true }
            }
        };
        MailboxQueryChangesRequest request = new MailboxQueryChangesRequest
        {
            AccountId = "account-1",
            Filter = filter,
            Sort = new List<JmapComparator>
            {
                new JmapComparator { Property = JmapProperties.Name, Collation = "i;unicode-casemap" }
            },
            SinceQueryState = "q0",
            HasMaxChanges = true,
            MaxChanges = 3,
            UpToId = "cached",
            HasCalculateTotal = true,
            CalculateTotal = true,
            HasSortAsTree = true,
            SortAsTree = true,
            HasFilterAsTree = true,
            FilterAsTree = true
        };

        JmapInvocation invocation = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "qc1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxQueryChangesResponse response = method.Codec.DecodeMailboxQueryChangesResponse(invocation.Arguments);
        MailboxQueryChangesRequest received = service.LastQueryChangesRequest!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.MailboxQueryChanges));
            Assert.That(response.Removed, Is.EqualTo(new[] { "moved" }));
            Assert.That(response.Added.Select(item => item.Id), Is.EqualTo(new[] { "moved", "new" }));
            Assert.That(response.Added.Select(item => item.Index), Is.EqualTo(new long[] { 0, 2 }));
            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(received.Filter!.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(received.Sort![0].Collation, Is.EqualTo("i;unicode-casemap"));
            Assert.That(received.UpToId, Is.EqualTo("cached"));
            Assert.That(received.SortAsTree, Is.True);
            Assert.That(received.FilterAsTree, Is.True);
            Assert.That(received.UnknownProperties.ContainsKey(JmapProperties.SortAsTree), Is.False);
            Assert.That(received.UnknownProperties.ContainsKey(JmapProperties.FilterAsTree), Is.False);
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryChangesDefaultsTreeIdentityFlagsToFalse()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();

        method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "qc1", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.LastQueryChangesRequest!.HasSortAsTree, Is.False);
            Assert.That(service.LastQueryChangesRequest.SortAsTree, Is.False);
            Assert.That(service.LastQueryChangesRequest.HasFilterAsTree, Is.False);
            Assert.That(service.LastQueryChangesRequest.FilterAsTree, Is.False);
        }
    }

    [Test]
    public void QueryChangesRejectsInvalidCommonAndQueryIdentityArguments()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        JmapSession session = Session(true);
        JmapInvocation negativeMaximum = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "q1", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"maxChanges\":-1}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation badUpTo = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "q2", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"upToId\":\"bad/id\"}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupportedFilter = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "q3", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"filter\":{\"vendor\":true}}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupportedSort = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "q4", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"sort\":[{\"property\":\"role\"}]}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation malformedTree = method.ProcessQueryChangesInvocation(
            RawInvocation(JmapMethods.MailboxQueryChanges, "q5", "{\"accountId\":\"account-1\",\"sinceQueryState\":\"q0\",\"sortAsTree\":null}"),
            session, service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, negativeMaximum).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, badUpTo).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, unsupportedFilter).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(Error(method, unsupportedSort).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, malformedTree).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(service.QueryChangesCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryChangesMapsExcessBackendDeltaToTooManyChanges()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        service.QueryChangesResponse.Removed.Add("old");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "new", Index = 0 });
        MailboxQueryChangesRequest request = QueryChangesRequest();
        request.HasMaxChanges = true;
        request.MaxChanges = 1;

        JmapInvocation invocation = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "qc1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invocation).Kind, Is.EqualTo(JmapMethodErrorKind.TooManyChanges));
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void QueryChangesAllowsZeroForEmptyDeltaAndNullForUnlimitedDelta()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService empty = new FakeMailboxService();
        MailboxQueryChangesRequest zero = QueryChangesRequest();
        zero.HasMaxChanges = true;
        zero.MaxChanges = 0;
        FakeMailboxService changed = new FakeMailboxService();
        changed.QueryChangesResponse.Removed.Add("old");
        changed.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "new", Index = 0 });
        MailboxQueryChangesRequest unlimited = QueryChangesRequest();
        unlimited.HasMaxChanges = true;
        unlimited.IsMaxChangesNull = true;

        JmapInvocation emptyResult = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(zero, "q1"), Session(true), empty,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unlimitedResult = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(unlimited, "q2"), Session(true), changed,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(emptyResult.Name, Is.EqualTo(JmapMethods.MailboxQueryChanges));
            Assert.That(unlimitedResult.Name, Is.EqualTo(JmapMethods.MailboxQueryChanges));
        }
    }

    [Test]
    public void QueryChangesRejectsInvalidBackendStatesIdsAndIndexOrdering()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService wrongState = new FakeMailboxService();
        wrongState.QueryChangesResponse.OldQueryState = "other";
        FakeMailboxService duplicate = new FakeMailboxService();
        duplicate.QueryChangesResponse.Removed.Add("m1");
        duplicate.QueryChangesResponse.Removed.Add("m1");
        FakeMailboxService unsorted = new FakeMailboxService();
        unsorted.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "m1", Index = 2 });
        unsorted.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "m2", Index = 1 });

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(QueryChangesRequest(), "q1"), Session(true), wrongState,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(QueryChangesRequest(), "q2"), Session(true), duplicate,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException orderError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(QueryChangesRequest(), "q3"), Session(true), unsorted,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(duplicateError.Path, Is.EqualTo("removed/1"));
            Assert.That(orderError.Path, Is.EqualTo("added/1"));
        }
    }

    [Test]
    public void QueryChangesValidatesRequestedTotalsAndAddedIndexBounds()
    {
        MailboxMethod method = new MailboxMethod();
        MailboxQueryChangesRequest requested = QueryChangesRequest();
        requested.HasCalculateTotal = true;
        requested.CalculateTotal = true;
        FakeMailboxService missingTotal = new FakeMailboxService();
        FakeMailboxService badIndex = new FakeMailboxService();
        badIndex.QueryChangesResponse.HasTotal = true;
        badIndex.QueryChangesResponse.Total = 1;
        badIndex.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "m1", Index = 1 });

        JmapProtocolException totalError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(requested, "q1"), Session(true), missingTotal,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;
        JmapProtocolException indexError = Assert.Throws<JmapProtocolException>(() => method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(requested, "q2"), Session(true), badIndex,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(totalError.Path, Is.EqualTo("total"));
            Assert.That(indexError.Path, Is.EqualTo("added/0/index"));
        }
    }

    [Test]
    public void QueryChangesClientMappingHandlesTypedResultsAndStandardFailures()
    {
        MailboxMethod method = new MailboxMethod();
        MailboxQueryChangesRequest request = QueryChangesRequest();
        JmapResponse success = Response(
            JmapMethods.MailboxQueryChanges,
            "q1",
            "{\"accountId\":\"account-1\",\"oldQueryState\":\"q0\",\"newQueryState\":\"q1\",\"removed\":[\"m1\"],\"added\":[{\"id\":\"m1\",\"index\":2}]}" );
        JmapResponse cannotCalculate = Response(
            ResponseMapper.ErrorResponseName,
            "q1",
            "{\"type\":\"cannotCalculateChanges\",\"description\":\"expired\"}");

        MailboxQueryChangesResponse response = method.MapQueryChangesResponse(success, "q1", request);
        JmapMethodException error = Assert.Throws<JmapMethodException>(
            () => method.MapQueryChangesResponse(cannotCalculate, "q1", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Removed, Is.EqualTo(new[] { "m1" }));
            Assert.That(response.Added[0].Id, Is.EqualTo("m1"));
            Assert.That(response.Added[0].Index, Is.EqualTo(2));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
        }
    }

    [Test]
    public void SetDispatchesValidatedActionsAndAppliesCreateDefaults()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        Mailbox parent = StoredMailbox("parent", "Parent", 0);
        parent.MyRights!.MayCreateChild = true;
        Mailbox updated = StoredMailbox("update", "Before", 0);
        updated.MyRights!.MayRename = true;
        Mailbox destroyed = StoredMailbox("destroy", "Filled", 2);
        destroyed.MyRights!.MayDelete = true;
        service.GetResponse.List.AddRange(new[] { parent, updated, destroyed });
        Mailbox created = StoredMailbox("created-id", "Child", 0);
        created.ParentId = "parent";
        created.IsSubscribed = true;
        service.SetResponse.Created = new Dictionary<string, Mailbox> { ["c1"] = created };
        service.SetResponse.Updated = new Dictionary<string, JsonElement> { ["update"] = Json("null") };
        service.SetResponse.Destroyed = new List<string> { "destroy" };

        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(
                JmapMethods.MailboxSet,
                "s1",
                "{\"accountId\":\"account-1\",\"ifInState\":\"s1\",\"create\":{\"c1\":{\"name\":\"Child\",\"parentId\":\"parent\"}},\"update\":{\"update\":{\"name\":\"After\"}},\"destroy\":[\"destroy\"],\"onDestroyRemoveEmails\":true}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxSetResponse response = method.Codec.DecodeMailboxSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.MailboxSet));
            Assert.That(response.Created!["c1"].Id, Is.EqualTo("created-id"));
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { "update" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "destroy" }));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.EqualTo(1));
            Assert.That(service.LastSetRequest!.Create!["c1"].IsSubscribed, Is.True);
            Assert.That(service.LastSetRequest.Update!["update"].Values["name"].GetString(), Is.EqualTo("After"));
            Assert.That(service.LastSetRequest.Destroy, Is.EqualTo(new[] { "destroy" }));
            Assert.That(service.LastSetRequest.HasOnDestroyRemoveEmails, Is.True);
            Assert.That(service.LastSetRequest.OnDestroyRemoveEmails, Is.True);
        }
    }

    [Test]
    public void SetClassifiesCreateUpdateAndDestroyRightsFailuresAsForbidden()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        Mailbox parent = StoredMailbox("parent", "Parent", 0);
        Mailbox updated = StoredMailbox("update", "Before", 0);
        Mailbox destroyed = StoredMailbox("destroy", "Destroy", 0);
        service.GetResponse.List.AddRange(new[] { parent, updated, destroyed });
        MailboxSetRequest request = new MailboxSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Mailbox>
            {
                ["c1"] = new Mailbox { Name = "Child", ParentId = "parent" }
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["update"] = Patch("name", Json("\"After\""))
            },
            Destroy = new List<string> { "destroy" }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxSetResponse response = method.Codec.DecodeMailboxSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["c1"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.NotUpdated!["update"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.NotDestroyed!["destroy"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.OldState, Is.EqualTo("s1"));
            Assert.That(response.NewState, Is.EqualTo("s1"));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRejectsInvalidRolePlacementAndCyclicParentPerRecord()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        Mailbox parent = StoredMailbox("parent", "Parent", 0);
        parent.MyRights!.MayCreateChild = true;
        Mailbox moved = StoredMailbox("moved", "Moved", 0);
        moved.MyRights!.MayRename = true;
        moved.MyRights.MayCreateChild = true;
        service.GetResponse.List.AddRange(new[] { parent, moved });
        MailboxSetRequest request = new MailboxSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Mailbox>
            {
                ["c1"] = new Mailbox { Name = "Nested Inbox", ParentId = "parent", Role = JmapMailboxRoles.Inbox }
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["moved"] = Patch("parentId", Json("\"moved\""))
            }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxSetResponse response = method.Codec.DecodeMailboxSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["c1"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["c1"].Properties, Does.Contain(JmapProperties.ParentId));
            Assert.That(response.NotUpdated!["moved"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["moved"].Properties, Does.Contain(JmapProperties.ParentId));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetReturnsMailboxDestroyBlockersWithoutCallingMutationBackend()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        Mailbox parent = StoredMailbox("parent", "Parent", 0);
        parent.MyRights!.MayDelete = true;
        Mailbox child = StoredMailbox("child", "Child", 0);
        child.ParentId = "parent";
        Mailbox filled = StoredMailbox("filled", "Filled", 3);
        filled.MyRights!.MayDelete = true;
        service.GetResponse.List.AddRange(new[] { parent, child, filled });
        MailboxSetRequest request = new MailboxSetRequest
        {
            AccountId = "account-1",
            Destroy = new List<string> { "parent", "filled" }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        MailboxSetResponse response = method.Codec.DecodeMailboxSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotDestroyed!["parent"].Kind, Is.EqualTo(JmapSetErrorKind.MailboxHasChild));
            Assert.That(response.NotDestroyed["filled"].Kind, Is.EqualTo(JmapSetErrorKind.MailboxHasEmail));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetEnforcesObjectLimitReadOnlyAccountAndStatePrecondition()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        MailboxSetRequest request = new MailboxSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Mailbox> { ["c1"] = new Mailbox { Name = "One" } }
        };
        JmapServerOptions limitedOptions = Options(10);
        limitedOptions.MaxObjectsInSet = 0;
        JmapInvocation limited = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), limitedOptions, CancellationToken.None);
        JmapSession readOnlySession = Session(true);
        readOnlySession.Accounts["account-1"].IsReadOnly = true;
        JmapInvocation readOnly = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s2"), readOnlySession, service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        request.IfInState = "older";
        JmapInvocation mismatch = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s3"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, limited).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, readOnly).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, mismatch).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRejectsBackendResponsesThatDoNotPartitionEveryAction()
    {
        MailboxMethod method = new MailboxMethod();
        FakeMailboxService service = new FakeMailboxService();
        Mailbox mailbox = StoredMailbox("m1", "Before", 0);
        mailbox.MyRights!.MayRename = true;
        service.GetResponse.List.Add(mailbox);
        MailboxSetRequest request = new MailboxSetRequest
        {
            AccountId = "account-1",
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["m1"] = Patch("name", Json("\"After\""))
            }
        };

        JmapProtocolException error = Assert.Throws<JmapProtocolException>(() => method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None))!;

        Assert.That(error.Path, Is.EqualTo("updated"));
    }

    private static Mailbox StoredMailbox(string id, string name, long totalEmails)
    {
        Mailbox mailbox = new Mailbox();
        mailbox.Id = id;
        mailbox.Name = name;
        mailbox.TotalEmails = totalEmails;
        mailbox.UnreadEmails = totalEmails;
        mailbox.TotalThreads = totalEmails;
        mailbox.UnreadThreads = totalEmails;
        mailbox.MyRights = new MailboxRights();
        mailbox.UnknownProperties.Add("vendorColor", Json("\"blue\""));
        return mailbox;
    }

    private static JmapPatchObject Patch(string name, JsonElement value)
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(name, value);
        return patch;
    }

    private static MailboxChangesRequest ChangesRequest()
    {
        MailboxChangesRequest request = new MailboxChangesRequest();
        request.AccountId = "account-1";
        request.SinceState = "s0";
        return request;
    }

    private static MailboxQueryRequest QueryRequest()
    {
        MailboxQueryRequest request = new MailboxQueryRequest();
        request.AccountId = "account-1";
        return request;
    }

    private static MailboxQueryChangesRequest QueryChangesRequest()
    {
        MailboxQueryChangesRequest request = new MailboxQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "q0";
        return request;
    }

    private static JmapSession Session(bool includeAccount)
    {
        JmapSession session = new JmapSession();
        if (includeAccount)
        {
            JmapAccount account = new JmapAccount();
            JmapCapability capability = new JmapCapability();
            capability.Mail = new JmapMailCapabilities { MaxSizeMailboxName = 100, MayCreateTopLevelMailbox = true };
            account.AccountCapabilities.Add(JmapCapabilities.Mail, capability);
            session.Accounts.Add("account-1", account);
            JmapCapability coreCapability = new JmapCapability();
            coreCapability.Core = new JmapCoreCapabilities();
            coreCapability.Core.CollationAlgorithms.Add("i;unicode-casemap");
            session.Capabilities.Add(JmapCapabilities.Core, coreCapability);
        }

        return session;
    }

    private static JmapSession SessionWithoutMail()
    {
        JmapSession session = new JmapSession();
        session.Accounts.Add("account-1", new JmapAccount());
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

    private static string EncodeGet(MailboxMethod method, MailboxGetRequest request)
    {
        return method.CreateGetInvocation(request, "encoded").Arguments.GetRawText();
    }

    private static JmapMethodError Error(MailboxMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JmapResponse Response(string name, string callId, string arguments)
    {
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(RawInvocation(name, callId, arguments));
        return response;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeMailboxService : IMailboxService
    {
        public MailboxGetResponse GetResponse = new MailboxGetResponse
        {
            AccountId = "account-1",
            State = "s1"
        };

        public MailboxChangesResponse ChangesResponse = new MailboxChangesResponse
        {
            AccountId = "account-1",
            OldState = "s0",
            NewState = "s1"
        };

        public MailboxQueryResponse QueryResponse = new MailboxQueryResponse
        {
            AccountId = "account-1",
            QueryState = "q1"
        };

        public MailboxQueryChangesResponse QueryChangesResponse = new MailboxQueryChangesResponse
        {
            AccountId = "account-1",
            OldQueryState = "q0",
            NewQueryState = "q1"
        };

        public MailboxSetResponse SetResponse = new MailboxSetResponse
        {
            AccountId = "account-1",
            OldState = "s1",
            NewState = "s2"
        };

        public int GetCalls;
        public int ChangesCalls;
        public int QueryCalls;
        public int QueryChangesCalls;
        public int SetCalls;
        public MailboxGetRequest? LastGetRequest;
        public MailboxQueryRequest? LastQueryRequest;
        public MailboxQueryChangesRequest? LastQueryChangesRequest;
        public MailboxSetRequest? LastSetRequest;

        public Task<MailboxGetResponse> GetStoredMailboxesAsync(
            MailboxGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            return Task.FromResult(GetResponse);
        }

        public Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(
            MailboxChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ChangesCalls = ChangesCalls + 1;
            return Task.FromResult(ChangesResponse);
        }

        public Task<MailboxQueryResponse> QueryStoredMailboxesAsync(
            MailboxQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryCalls = QueryCalls + 1;
            LastQueryRequest = request;
            return Task.FromResult(QueryResponse);
        }

        public Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(
            MailboxQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryChangesCalls = QueryChangesCalls + 1;
            LastQueryChangesRequest = request;
            return Task.FromResult(QueryChangesResponse);
        }

        public Task<MailboxSetResponse> SetStoredMailboxesAsync(
            MailboxSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }
    }
}
