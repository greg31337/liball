using System.Text.Json;

namespace jmap.Tests;

public class IdentityMethodTests
{
    [Test]
    public void GetAppliesDefaultsAndProjectsOnlyRequestedPropertiesWithId()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        service.GetResponse.List.Add(new Identity
        {
            Id = "i1",
            Email = "alice@example.test",
            MayDelete = false
        });
        IdentityGetRequest defaults = new IdentityGetRequest { AccountId = "account-1" };

        JmapInvocation defaultInvocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(defaults, "g1"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JsonElement defaultItem = defaultInvocation.Arguments.GetProperty(JmapProperties.List)[0];

        service.GetResponse = new IdentityGetResponse
        {
            AccountId = "account-1",
            State = "identity-state",
            List = new List<Identity> { new Identity { Id = "i2" } },
            NotFound = new List<string> { "missing" }
        };
        IdentityGetRequest projected = new IdentityGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "missing", "i2", "i2" },
            Properties = new List<string> { JmapProperties.Name }
        };
        JmapInvocation projectedInvocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(projected, "g2"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JsonElement projectedItem = projectedInvocation.Arguments.GetProperty(JmapProperties.List)[0];
        IdentityGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.IdentityGet, "g2", projectedInvocation.Arguments.GetRawText()), "g2", projected);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(defaultItem.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.IdentityProperties()));
            Assert.That(defaultItem.GetProperty(JmapProperties.Name).GetString(), Is.EqualTo(""));
            Assert.That(defaultItem.GetProperty(JmapProperties.ReplyTo).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(defaultItem.GetProperty(JmapProperties.Bcc).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(defaultItem.GetProperty(JmapProperties.TextSignature).GetString(), Is.EqualTo(""));
            Assert.That(defaultItem.GetProperty(JmapProperties.HtmlSignature).GetString(), Is.EqualTo(""));
            Assert.That(projectedItem.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Name }));
            Assert.That(projectedItem.GetProperty(JmapProperties.Name).GetString(), Is.EqualTo(""));
            Assert.That(mapped.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(mapped.List.Select(identity => identity.Id), Is.EqualTo(new[] { "i2" }));
            Assert.That(service.LastGetRequest!.Properties,
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Name }));
        }
    }

    [Test]
    public void GetReturnsEveryIdentityFieldWithExplicitAddressAndSignatureValues()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        service.GetResponse.List.Add(new Identity
        {
            Id = "i1",
            Name = "Alice Example",
            Email = "*@example.test",
            ReplyTo = new List<EmailAddress>
            {
                new EmailAddress { Name = "Replies", Email = "reply@example.test" }
            },
            Bcc = new List<EmailAddress>
            {
                new EmailAddress { Name = null, Email = "archive@example.test" }
            },
            TextSignature = "Thanks,\nAlice",
            HtmlSignature = "<p>Thanks,<br>Alice</p>",
            MayDelete = true
        });
        IdentityGetRequest request = new IdentityGetRequest { AccountId = "account-1" };

        IdentityGetResponse response = method.Codec.DecodeIdentityGetResponse(method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None).Arguments);
        Identity identity = response.List[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.State, Is.EqualTo("identity-state"));
            Assert.That(identity.Id, Is.EqualTo("i1"));
            Assert.That(identity.Name, Is.EqualTo("Alice Example"));
            Assert.That(identity.Email, Is.EqualTo("*@example.test"));
            Assert.That(identity.ReplyTo![0].Name, Is.EqualTo("Replies"));
            Assert.That(identity.ReplyTo[0].Email, Is.EqualTo("reply@example.test"));
            Assert.That(identity.Bcc![0].Name, Is.Null);
            Assert.That(identity.Bcc[0].Email, Is.EqualTo("archive@example.test"));
            Assert.That(identity.TextSignature, Is.EqualTo("Thanks,\nAlice"));
            Assert.That(identity.HtmlSignature, Is.EqualTo("<p>Thanks,<br>Alice</p>"));
            Assert.That(identity.MayDelete, Is.True);
            Assert.That(service.LastGetRequest!.Properties, Is.Null);
        }
    }

    [Test]
    public void GetRejectsBadPropertiesAccountsLimitsAndInvalidBackendPartitions()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        JmapInvocation property = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.IdentityGet, "g1",
                "{\"accountId\":\"account-1\",\"properties\":[\"threadId\"]}"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation limit = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.IdentityGet, "g2",
                "{\"accountId\":\"account-1\",\"ids\":[\"i1\",\"i2\"]}"),
            Session(true, false), service, new JmapRequestContext(), Options(1, 10), CancellationToken.None);
        JmapInvocation missing = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.IdentityGet, "g3", "{\"accountId\":\"account-1\"}"),
            Session(false, false), service, new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.IdentityGet, "g4", "{\"accountId\":\"account-1\"}"),
            MailOnlySession(), service, new JmapRequestContext(), Options(10, 10), CancellationToken.None);

        service.GetResponse.List.Add(new Identity { Id = "other", Email = "other@example.test" });
        IdentityGetRequest requested = new IdentityGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "i1" }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, property).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, missing).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, unsupported).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(service.GetCalls, Is.Zero);
            Assert.That(() => method.ProcessGetInvocation(method.CreateGetInvocation(requested, "g5"),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void ChangesAndSetRequireTheSubmissionAccountCapability()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        JmapInvocation changes = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.IdentityChanges, "c1",
                "{\"accountId\":\"account-1\",\"sinceState\":\"identity-state\"}"),
            MailOnlySession(), service, new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation set = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.IdentitySet, "s1", "{\"accountId\":\"account-1\"}"),
            MailOnlySession(), service, new JmapRequestContext(), Options(10, 10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, changes).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(Error(method, set).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(service.GetCalls, Is.Zero);
            Assert.That(service.ChangesCalls, Is.Zero);
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void ChangesValidatesStatePartitionsAndMaximumChanges()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        service.ChangesResponse.Created.Add("created");
        service.ChangesResponse.Updated.Add("updated");
        service.ChangesResponse.Destroyed.Add("destroyed");
        service.ChangesResponse.HasMoreChanges = true;
        IdentityChangesRequest request = new IdentityChangesRequest
        {
            AccountId = "account-1",
            SinceState = "old-state",
            MaxChanges = 3,
            HasMaxChanges = true
        };

        JmapInvocation invocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(request, "c1"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        IdentityChangesResponse decoded = method.Codec.DecodeIdentityChangesResponse(invocation.Arguments);
        IdentityChangesResponse mapped = method.MapChangesResponse(
            Response(JmapMethods.IdentityChanges, "c1", invocation.Arguments.GetRawText()), "c1", request);

        service.ChangesResponse.Updated.Add("created");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(decoded.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(decoded.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(decoded.Destroyed, Is.EqualTo(new[] { "destroyed" }));
            Assert.That(decoded.OldState, Is.EqualTo("old-state"));
            Assert.That(decoded.HasMoreChanges, Is.True);
            Assert.That(mapped.NewState, Is.EqualTo("new-state"));
            Assert.That(service.ChangesCalls, Is.EqualTo(1));
            Assert.That(service.LastChangesRequest!.SinceState, Is.EqualTo("old-state"));
            Assert.That(service.LastChangesRequest.MaxChanges, Is.EqualTo(3));
            Assert.That(service.LastChangesRequest.HasMaxChanges, Is.True);
            Assert.That(() => method.ProcessChangesInvocation(method.CreateChangesInvocation(request, "c2"),
                Session(true, false), service, new JmapRequestContext(), Options(10, 10),
                CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void SetAppliesDefaultsAndPartitionsImmutableProtectedMissingAndForbiddenFromActions()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = SetService();
        string json = "{" +
            "\"accountId\":\"account-1\",\"ifInState\":\"identity-state\"," +
            "\"create\":{" +
                "\"good\":{\"email\":\"good@example.test\"}," +
                "\"denied\":{\"email\":\"denied@example.test\",\"name\":\"Denied\"}," +
                "\"invalid\":{\"email\":\"bad@example.test\",\"mayDelete\":false}} ," +
            "\"update\":{" +
                "\"editable\":{\"name\":\"New name\"}," +
                "\"immutable\":{\"email\":\"changed@example.test\"}," +
                "\"both\":{\"name\":\"ignored\"}," +
                "\"missing-update\":{\"name\":\"missing\"}}," +
            "\"destroy\":[\"protected\",\"deletable\",\"both\",\"missing-destroy\"]}";

        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.IdentitySet, "s1", json), Session(true, false), service,
            new JmapRequestContext(), Options(10, 20), CancellationToken.None);
        IdentitySetResponse response = method.Codec.DecodeIdentitySetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!.Keys, Is.EqualTo(new[] { "good" }));
            Assert.That(response.NotCreated!["denied"].Kind, Is.EqualTo(JmapSetErrorKind.ForbiddenFrom));
            Assert.That(response.NotCreated["invalid"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["invalid"].Properties, Does.Contain(JmapProperties.MayDelete));
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { "editable" }));
            Assert.That(response.NotUpdated!["immutable"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["immutable"].Properties, Does.Contain(JmapProperties.Email));
            Assert.That(response.NotUpdated["both"].Kind, Is.EqualTo(JmapSetErrorKind.WillDestroy));
            Assert.That(response.NotUpdated["missing-update"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "deletable", "both" }));
            Assert.That(response.NotDestroyed!["protected"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.NotDestroyed["missing-destroy"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(service.SetCalls, Is.EqualTo(1));
            Assert.That(service.LastSetRequest!.Create!.Keys, Is.EquivalentTo(new[] { "good", "denied" }));
            Assert.That(service.LastSetRequest.Create["good"].Name, Is.EqualTo(""));
            Assert.That(service.LastSetRequest.Create["good"].TextSignature, Is.EqualTo(""));
            Assert.That(service.LastSetRequest.Create["good"].HtmlSignature, Is.EqualTo(""));
            Assert.That(service.LastSetRequest.Update!.Keys, Is.EqualTo(new[] { "editable" }));
            Assert.That(service.LastSetRequest.Destroy, Is.EqualTo(new[] { "deletable", "both" }));
        }
    }

    [Test]
    public void SetForwardsEveryWritableFieldAndReturnsTheNewCollectionState()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        service.GetResponse.List.Add(new Identity { Id = "editable", MayDelete = false });
        service.SetResponse.Created = new Dictionary<string, Identity>
        {
            ["new"] = new Identity { Id = "created", Email = "alice@example.test", MayDelete = true }
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["editable"] = Json("null")
        };
        string json = "{" +
            "\"accountId\":\"account-1\",\"ifInState\":\"identity-state\"," +
            "\"create\":{\"new\":{" +
                "\"name\":\"Alice\",\"email\":\"alice@example.test\"," +
                "\"replyTo\":[{\"name\":\"Reply Desk\",\"email\":\"reply@example.test\"}]," +
                "\"bcc\":[{\"name\":null,\"email\":\"archive@example.test\"}]," +
                "\"textSignature\":\"Regards\",\"htmlSignature\":\"<b>Regards</b>\"}}," +
            "\"update\":{\"editable\":{" +
                "\"name\":\"Updated\"," +
                "\"replyTo\":[{\"name\":null,\"email\":\"new-reply@example.test\"}]," +
                "\"bcc\":null,\"textSignature\":\"Updated text\"," +
                "\"htmlSignature\":\"<i>Updated HTML</i>\"}}}";

        IdentitySetResponse response = method.Codec.DecodeIdentitySetResponse(method.ProcessSetInvocation(
            RawInvocation(JmapMethods.IdentitySet, "s1", json), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None).Arguments);
        Identity created = service.LastSetRequest!.Create!["new"];
        JmapPatchObject update = service.LastSetRequest.Update!["editable"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Name, Is.EqualTo("Alice"));
            Assert.That(created.Email, Is.EqualTo("alice@example.test"));
            Assert.That(created.ReplyTo![0].Name, Is.EqualTo("Reply Desk"));
            Assert.That(created.ReplyTo[0].Email, Is.EqualTo("reply@example.test"));
            Assert.That(created.Bcc![0].Name, Is.Null);
            Assert.That(created.Bcc[0].Email, Is.EqualTo("archive@example.test"));
            Assert.That(created.TextSignature, Is.EqualTo("Regards"));
            Assert.That(created.HtmlSignature, Is.EqualTo("<b>Regards</b>"));
            Assert.That(created.MayDelete, Is.False);
            Assert.That(update.Values.Keys, Is.EquivalentTo(new[]
            {
                JmapProperties.Name,
                JmapProperties.ReplyTo,
                JmapProperties.Bcc,
                JmapProperties.TextSignature,
                JmapProperties.HtmlSignature
            }));
            Assert.That(update.Values[JmapProperties.ReplyTo][0].GetProperty(JmapProperties.Email).GetString(),
                Is.EqualTo("new-reply@example.test"));
            Assert.That(update.Values[JmapProperties.Bcc].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.OldState, Is.EqualTo("identity-state"));
            Assert.That(response.NewState, Is.EqualTo("identity-state-2"));
            Assert.That(response.Created!["new"].Id, Is.EqualTo("created"));
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { "editable" }));
        }
    }

    [Test]
    public void SetRejectsReadOnlyStateMismatchAndActionLimitBeforeMutation()
    {
        IdentityMethod method = new IdentityMethod();
        FakeIdentityService service = new FakeIdentityService();
        IdentitySetRequest request = new IdentitySetRequest
        {
            AccountId = "account-1",
            IfInState = "wrong-state",
            Create = new Dictionary<string, Identity>
            {
                ["create"] = new Identity { Email = "a@example.test" }
            }
        };
        JmapInvocation readOnly = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s1"), Session(true, true), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation mismatch = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s2"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 10), CancellationToken.None);
        JmapInvocation limit = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s3"), Session(true, false), service,
            new JmapRequestContext(), Options(10, 0), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, readOnly).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, mismatch).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, limit).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.Zero);
            Assert.That(request.Create!["create"].Name, Is.EqualTo(""));
            Assert.That(request.Create["create"].TextSignature, Is.EqualTo(""));
            Assert.That(request.Create["create"].HtmlSignature, Is.EqualTo(""));
        }
    }

    [Test]
    public void SetRejectsMalformedBackendPartitionsAndInvalidForbiddenFromPlacement()
    {
        IdentityMethod method = new IdentityMethod();
        IdentitySetRequest request = new IdentitySetRequest
        {
            AccountId = "account-1",
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["editable"] = Patch(JmapProperties.Name, Json("\"New\""))
            }
        };
        FakeIdentityService service = new FakeIdentityService();
        service.GetResponse.List.Add(new Identity { Id = "editable", MayDelete = true });
        service.SetResponse.Updated = null;
        service.SetResponse.NotUpdated = new Dictionary<string, JmapSetError>
        {
            ["editable"] = new JmapSetError
            {
                Type = JmapErrorCodes.ForbiddenFrom,
                Kind = JmapSetErrorKind.ForbiddenFrom
            }
        };

        Assert.That(() => method.ProcessSetInvocation(method.CreateSetInvocation(request, "s1"),
            Session(true, false), service, new JmapRequestContext(), Options(10, 10),
            CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
    }

    private static FakeIdentityService SetService()
    {
        FakeIdentityService service = new FakeIdentityService();
        service.GetResponse.List.Add(new Identity { Id = "editable" });
        service.GetResponse.List.Add(new Identity { Id = "immutable" });
        service.GetResponse.List.Add(new Identity { Id = "both", MayDelete = true });
        service.GetResponse.List.Add(new Identity { Id = "protected" });
        service.GetResponse.List.Add(new Identity { Id = "deletable", MayDelete = true });
        service.GetResponse.NotFound.Add("missing-update");
        service.GetResponse.NotFound.Add("missing-destroy");
        service.SetResponse.Created = new Dictionary<string, Identity>
        {
            ["good"] = new Identity { Id = "created-good" }
        };
        service.SetResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["denied"] = new JmapSetError
            {
                Type = JmapErrorCodes.ForbiddenFrom,
                Kind = JmapSetErrorKind.ForbiddenFrom
            }
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["editable"] = Json("null")
        };
        service.SetResponse.Destroyed = new List<string> { "deletable", "both" };
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
            JmapCapability submission = new JmapCapability();
            submission.Submission = new JmapSubmissionCapabilities();
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

    private static JmapMethodError Error(IdentityMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeIdentityService : IIdentityService
    {
        public IdentityGetResponse GetResponse = new IdentityGetResponse
        {
            AccountId = "account-1",
            State = "identity-state"
        };

        public IdentityChangesResponse ChangesResponse = new IdentityChangesResponse
        {
            AccountId = "account-1",
            OldState = "old-state",
            NewState = "new-state"
        };

        public IdentitySetResponse SetResponse = new IdentitySetResponse
        {
            AccountId = "account-1",
            OldState = "identity-state",
            NewState = "identity-state-2"
        };

        public int GetCalls;
        public int ChangesCalls;
        public int SetCalls;
        public IdentityGetRequest? LastGetRequest;
        public IdentityChangesRequest? LastChangesRequest;
        public IdentitySetRequest? LastSetRequest;

        public Task<IdentityGetResponse> GetStoredIdentitiesAsync(
            IdentityGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            return Task.FromResult(GetResponse);
        }

        public Task<IdentityChangesResponse> GetStoredIdentityChangesAsync(
            IdentityChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ChangesCalls = ChangesCalls + 1;
            LastChangesRequest = request;
            return Task.FromResult(ChangesResponse);
        }

        public Task<IdentitySetResponse> SetStoredIdentitiesAsync(
            IdentitySetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }
    }
}
