using System.Text.Json;

namespace jmap.Tests;

public class VacationResponseMethodTests
{
    [Test]
    public void GetReturnsOnlyTheSingletonAndProjectsRequestedProperties()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.GetResponse.List[0].IsEnabled = true;
        service.GetResponse.List[0].TextBody = "Away";
        VacationResponseGetRequest request = new VacationResponseGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "missing", VacationResponseMethod.SingletonId,
                VacationResponseMethod.SingletonId },
            Properties = new List<string> { JmapProperties.IsEnabled, JmapProperties.TextBody }
        };
        request.UnknownProperties.Add("x-vendor", Json("true"));

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        VacationResponseGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.VacationResponseGet, "g1", invocation.Arguments.GetRawText()),
            "g1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[]
                {
                    JmapProperties.Id, JmapProperties.IsEnabled, JmapProperties.TextBody
                }));
            Assert.That(item.GetProperty(JmapProperties.Id).GetString(),
                Is.EqualTo(VacationResponseMethod.SingletonId));
            Assert.That(item.GetProperty(JmapProperties.IsEnabled).GetBoolean(), Is.True);
            Assert.That(item.GetProperty(JmapProperties.TextBody).GetString(), Is.EqualTo("Away"));
            Assert.That(mapped.List, Has.Count.EqualTo(1));
            Assert.That(mapped.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(service.LastGetRequest!.Ids, Is.Null);
            Assert.That(service.LastGetRequest.Properties,
                Is.EqualTo(new[]
                {
                    JmapProperties.Id, JmapProperties.IsEnabled, JmapProperties.TextBody
                }));
            Assert.That(service.LastGetRequest.UnknownProperties["x-vendor"].GetBoolean(), Is.True);
        }
    }

    [Test]
    public void GetReturnsEveryExplicitVacationFieldForTheSingleton()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.GetResponse.List[0] = new VacationResponse
        {
            Id = VacationResponseMethod.SingletonId,
            IsEnabled = true,
            FromDate = "2026-12-20T08:00:00Z",
            ToDate = "2027-01-03T18:00:00Z",
            Subject = "Out of office",
            TextBody = "I will reply in January.",
            HtmlBody = "<p>I will reply in <b>January</b>.</p>"
        };
        VacationResponseGetRequest request = new VacationResponseGetRequest { AccountId = "account-1" };

        VacationResponseGetResponse response = method.Codec.DecodeVacationGetResponse(
            method.ProcessGetInvocation(method.CreateGetInvocation(request, "g1"), Session(true, false),
                service, new JmapRequestContext(), Options(10, 10), CancellationToken.None).Arguments);
        VacationResponse vacation = response.List.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.State, Is.EqualTo("vacation-state"));
            Assert.That(vacation.Id, Is.EqualTo(VacationResponseMethod.SingletonId));
            Assert.That(vacation.IsEnabled, Is.True);
            Assert.That(vacation.FromDate, Is.EqualTo("2026-12-20T08:00:00Z"));
            Assert.That(vacation.ToDate, Is.EqualTo("2027-01-03T18:00:00Z"));
            Assert.That(vacation.Subject, Is.EqualTo("Out of office"));
            Assert.That(vacation.TextBody, Is.EqualTo("I will reply in January."));
            Assert.That(vacation.HtmlBody, Is.EqualTo("<p>I will reply in <b>January</b>.</p>"));
            Assert.That(service.LastGetRequest!.Ids, Is.Null);
            Assert.That(service.LastGetRequest.Properties, Is.EqualTo(method.VacationProperties()));
        }
    }

    [Test]
    public void GetEmitsDefaultsAndEnforcesLimitsAccountsAndStoredSingletonShape()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        VacationResponseGetRequest request = new VacationResponseGetRequest { AccountId = "account-1" };
        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];

        JmapInvocation invalidProperty = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.VacationResponseGet, "g2",
                "{\"accountId\":\"account-1\",\"properties\":[\"threadId\"]}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);
        JmapInvocation limit = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g3"), Session(true, false), service,
            new JmapRequestContext(), Options(0, 10), CancellationToken.None);
        JmapInvocation missing = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g4"), Session(false, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g5"), MailOnlySession(), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);

        service.GetResponse.List[0].Id = "other";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.VacationProperties()));
            Assert.That(item.GetProperty(JmapProperties.IsEnabled).GetBoolean(), Is.False);
            Assert.That(item.GetProperty(JmapProperties.FromDate).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(item.GetProperty(JmapProperties.ToDate).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(item.GetProperty(JmapProperties.Subject).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(item.GetProperty(JmapProperties.TextBody).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(item.GetProperty(JmapProperties.HtmlBody).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(Error(method, invalidProperty).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, missing).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, unsupported).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(() => method.ProcessGetInvocation(method.CreateGetInvocation(request, "g6"),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void SetClassifiesCreateDestroyAndNonSingletonUpdateLocally()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        string json = "{" +
            "\"accountId\":\"account-1\"," +
            "\"create\":{\"new\":{}}," +
            "\"update\":{\"missing\":{\"isEnabled\":true}}," +
            "\"destroy\":[\"singleton\",\"missing-destroy\"]}";

        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s1", json), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        VacationResponseSetResponse response = method.Codec.DecodeVacationSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.OldState, Is.EqualTo("vacation-state"));
            Assert.That(response.NewState, Is.EqualTo("vacation-state"));
            Assert.That(response.NotCreated!["new"].Kind, Is.EqualTo(JmapSetErrorKind.Singleton));
            Assert.That(response.NotUpdated!["missing"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(response.NotDestroyed!["singleton"].Kind, Is.EqualTo(JmapSetErrorKind.Singleton));
            Assert.That(response.NotDestroyed["missing-destroy"].Kind,
                Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetValidatesCompletePatchedIntervalAndUsesSnapshotState()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.GetResponse.List[0].FromDate = "2026-09-10T00:00:00Z";
        service.GetResponse.List[0].ToDate = "2026-09-20T00:00:00Z";
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            [VacationResponseMethod.SingletonId] = Json("null")
        };
        string json = "{" +
            "\"accountId\":\"account-1\"," +
            "\"update\":{\"singleton\":{" +
                "\"id\":\"singleton\"," +
                "\"isEnabled\":true," +
                "\"fromDate\":null," +
                "\"toDate\":\"2026-09-30T00:00:00Z\"," +
                "\"subject\":null," +
                "\"textBody\":\"Out of office\"}}}";
        VacationResponseSetRequest request = method.Codec.DecodeVacationSetRequest(Json(json));

        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s1", json), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        VacationResponseSetResponse response = method.MapSetResponse(
            Response(JmapMethods.VacationResponseSet, "s1", invocation.Arguments.GetRawText()),
            "s1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { VacationResponseMethod.SingletonId }));
            Assert.That(service.SetCalls, Is.EqualTo(1));
            Assert.That(service.LastSetRequest!.IfInState, Is.EqualTo("vacation-state"));
            Assert.That(service.LastSetRequest.Create, Is.Null);
            Assert.That(service.LastSetRequest.Destroy, Is.Null);
            Assert.That(service.LastSetRequest.Update![VacationResponseMethod.SingletonId].Values.Keys,
                Is.EquivalentTo(new[]
                {
                    JmapProperties.Id, JmapProperties.IsEnabled, JmapProperties.FromDate,
                    JmapProperties.ToDate, JmapProperties.Subject, JmapProperties.TextBody
                }));
        }
    }

    [Test]
    public void SetForwardsEveryMutableFieldForAnEnabledClosedInterval()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            [VacationResponseMethod.SingletonId] = Json("null")
        };
        string json = "{" +
            "\"accountId\":\"account-1\",\"ifInState\":\"vacation-state\"," +
            "\"update\":{\"singleton\":{" +
                "\"isEnabled\":true," +
                "\"fromDate\":\"2026-12-20T08:00:00Z\"," +
                "\"toDate\":\"2027-01-03T18:00:00Z\"," +
                "\"subject\":\"Out of office\"," +
                "\"textBody\":\"I will reply in January.\"," +
                "\"htmlBody\":\"<p>I will reply in <b>January</b>.</p>\"}}}";

        VacationResponseSetResponse response = method.Codec.DecodeVacationSetResponse(
            method.ProcessSetInvocation(RawInvocation(JmapMethods.VacationResponseSet, "s1", json),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None).Arguments);
        JmapPatchObject patch = service.LastSetRequest!.Update![VacationResponseMethod.SingletonId];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patch.Values.Keys, Is.EquivalentTo(new[]
            {
                JmapProperties.IsEnabled,
                JmapProperties.FromDate,
                JmapProperties.ToDate,
                JmapProperties.Subject,
                JmapProperties.TextBody,
                JmapProperties.HtmlBody
            }));
            Assert.That(patch.Values[JmapProperties.IsEnabled].GetBoolean(), Is.True);
            Assert.That(patch.Values[JmapProperties.FromDate].GetString(),
                Is.EqualTo("2026-12-20T08:00:00Z"));
            Assert.That(patch.Values[JmapProperties.ToDate].GetString(),
                Is.EqualTo("2027-01-03T18:00:00Z"));
            Assert.That(patch.Values[JmapProperties.Subject].GetString(), Is.EqualTo("Out of office"));
            Assert.That(patch.Values[JmapProperties.TextBody].GetString(),
                Is.EqualTo("I will reply in January."));
            Assert.That(patch.Values[JmapProperties.HtmlBody].GetString(),
                Is.EqualTo("<p>I will reply in <b>January</b>.</p>"));
            Assert.That(service.LastSetRequest.IfInState, Is.EqualTo("vacation-state"));
            Assert.That(response.OldState, Is.EqualTo("vacation-state"));
            Assert.That(response.NewState, Is.EqualTo("new-vacation-state"));
        }
    }

    [Test]
    public void SetAllowsDisablingAndResettingEveryOptionalFieldToItsOpenDefault()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.GetResponse.List[0] = new VacationResponse
        {
            Id = VacationResponseMethod.SingletonId,
            IsEnabled = true,
            FromDate = "2026-12-20T08:00:00Z",
            ToDate = "2027-01-03T18:00:00Z",
            Subject = "Out of office",
            TextBody = "Away",
            HtmlBody = "<p>Away</p>"
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            [VacationResponseMethod.SingletonId] = Json("null")
        };
        string json = "{" +
            "\"accountId\":\"account-1\",\"update\":{\"singleton\":{" +
            "\"isEnabled\":null,\"fromDate\":null,\"toDate\":null," +
            "\"subject\":null,\"textBody\":null,\"htmlBody\":null}}}";

        VacationResponseSetResponse response = method.Codec.DecodeVacationSetResponse(
            method.ProcessSetInvocation(RawInvocation(JmapMethods.VacationResponseSet, "s1", json),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None).Arguments);
        JmapPatchObject patch = service.LastSetRequest!.Update![VacationResponseMethod.SingletonId];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patch.Values.Keys, Has.Count.EqualTo(6));
            Assert.That(patch.Values.Values.All(value => value.ValueKind == JsonValueKind.Null), Is.True);
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { VacationResponseMethod.SingletonId }));
            Assert.That(service.SetCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void SetRejectsInvalidPatchesStateMismatchReadOnlyAndLimitsBeforeMutation()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();
        service.GetResponse.List[0].ToDate = "2026-09-20T00:00:00Z";
        JmapInvocation interval = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s1",
                "{\"accountId\":\"account-1\",\"update\":{\"singleton\":{" +
                "\"fromDate\":\"2026-09-21T00:00:00Z\"}}}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);
        JmapInvocation immutable = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s2",
                "{\"accountId\":\"account-1\",\"update\":{\"singleton\":{\"id\":null}}}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);
        JmapInvocation conflict = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s3",
                "{\"accountId\":\"account-1\",\"update\":{\"singleton\":{" +
                "\"subject\":null,\"subject/x\":null}}}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);
        JmapInvocation mismatch = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s4",
                "{\"accountId\":\"account-1\",\"ifInState\":\"wrong\"}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);
        JmapInvocation readOnly = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s5",
                "{\"accountId\":\"account-1\"}"), Session(true, true), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation limit = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s6",
                "{\"accountId\":\"account-1\",\"destroy\":[\"singleton\"]}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 0),
            CancellationToken.None);

        VacationResponseSetResponse intervalResponse = method.Codec.DecodeVacationSetResponse(interval.Arguments);
        VacationResponseSetResponse immutableResponse = method.Codec.DecodeVacationSetResponse(immutable.Arguments);
        VacationResponseSetResponse conflictResponse = method.Codec.DecodeVacationSetResponse(conflict.Arguments);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(intervalResponse.NotUpdated!["singleton"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(intervalResponse.NotUpdated["singleton"].Properties,
                Is.EquivalentTo(new[] { JmapProperties.FromDate, JmapProperties.ToDate }));
            Assert.That(immutableResponse.NotUpdated!["singleton"].Properties,
                Is.EqualTo(new[] { JmapProperties.Id }));
            Assert.That(conflictResponse.NotUpdated!["singleton"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidPatch));
            Assert.That(Error(method, mismatch).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, readOnly).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRequiresTheVacationResponseAccountCapabilityBeforeReadingState()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        FakeVacationResponseService service = Service();

        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.VacationResponseSet, "s1", "{\"accountId\":\"account-1\"}"),
            MailOnlySession(), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invocation).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(service.GetCalls, Is.Zero);
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetRejectsBackendSuccessForCreateOrDestroyAndUnexpectedOldState()
    {
        VacationResponseMethod method = new VacationResponseMethod();
        VacationResponseSetRequest update = new VacationResponseSetRequest
        {
            AccountId = "account-1",
            Update = new Dictionary<string, JmapPatchObject>
            {
                [VacationResponseMethod.SingletonId] = Patch(JmapProperties.IsEnabled, Json("true"))
            }
        };
        FakeVacationResponseService service = Service();
        service.SetResponse.OldState = "other-state";
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            [VacationResponseMethod.SingletonId] = Json("null")
        };

        VacationResponseSetRequest create = new VacationResponseSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, VacationResponse> { ["new"] = new VacationResponse() }
        };
        VacationResponseSetResponse invalidCreate = new VacationResponseSetResponse
        {
            AccountId = "account-1",
            OldState = "vacation-state",
            NewState = "new-state",
            Created = new Dictionary<string, VacationResponse>
            {
                ["new"] = new VacationResponse { Id = VacationResponseMethod.SingletonId }
            }
        };
        VacationResponseSetRequest destroy = new VacationResponseSetRequest
        {
            AccountId = "account-1",
            Destroy = new List<string> { VacationResponseMethod.SingletonId }
        };
        VacationResponseSetResponse invalidDestroy = new VacationResponseSetResponse
        {
            AccountId = "account-1",
            OldState = "vacation-state",
            NewState = "new-state",
            NotDestroyed = new Dictionary<string, JmapSetError>
            {
                [VacationResponseMethod.SingletonId] = new JmapSetError
                {
                    Type = JmapErrorCodes.NotFound,
                    Kind = JmapSetErrorKind.NotFound
                }
            }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => method.ProcessSetInvocation(method.CreateSetInvocation(update, "s1"),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => method.ValidateSetResponse(invalidCreate, create),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => method.ValidateSetResponse(invalidDestroy, destroy),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    private static FakeVacationResponseService Service()
    {
        FakeVacationResponseService service = new FakeVacationResponseService();
        service.GetResponse.List.Add(new VacationResponse
        {
            Id = VacationResponseMethod.SingletonId
        });
        return service;
    }

    private static JmapPatchObject Patch(string path, JsonElement value)
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(path, value);
        return patch;
    }

    private static JmapSession Session(bool includeAccount, bool readOnly)
    {
        JmapSession session = new JmapSession();
        if (includeAccount)
        {
            JmapAccount account = new JmapAccount();
            account.IsReadOnly = readOnly;
            JmapCapability vacation = new JmapCapability();
            vacation.VacationResponse = new JmapVacationResponseCapabilities();
            account.AccountCapabilities.Add(JmapCapabilities.VacationResponse, vacation);
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

    private static JmapServerOptions Options(int maximumObjectsInGet, int maximumObjectsInSet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInGet = maximumObjectsInGet;
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

    private static JmapMethodError Error(VacationResponseMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeVacationResponseService : IVacationResponseService
    {
        public VacationResponseGetResponse GetResponse = new VacationResponseGetResponse
        {
            AccountId = "account-1",
            State = "vacation-state"
        };

        public VacationResponseSetResponse SetResponse = new VacationResponseSetResponse
        {
            AccountId = "account-1",
            OldState = "vacation-state",
            NewState = "new-vacation-state"
        };

        public int GetCalls;
        public int SetCalls;
        public VacationResponseGetRequest? LastGetRequest;
        public VacationResponseSetRequest? LastSetRequest;

        public Task<VacationResponseGetResponse> GetStoredVacationResponseAsync(
            VacationResponseGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            return Task.FromResult(GetResponse);
        }

        public Task<VacationResponseSetResponse> SetStoredVacationResponseAsync(
            VacationResponseSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }
    }
}
