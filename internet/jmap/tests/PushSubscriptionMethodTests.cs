using System.Text.Json;

namespace jmap.Tests;

public class PushSubscriptionMethodTests
{
    [Test]
    public void GetDispatchOrdersIdsProjectsPropertiesAndRemovesPrivateData()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        service.GetResponse.List.Add(StoredSubscription("p2", "device-2"));
        service.GetResponse.List.Add(StoredSubscription("p1", "device-1"));
        PushSubscriptionGetRequest request = new PushSubscriptionGetRequest
        {
            Ids = new List<string> { "p1", "missing", "p2", "p1" },
            Properties = new List<string> { JmapProperties.DeviceClientId }
        };
        JmapRequestContext context = Context();

        JmapInvocation invocation = method.ProcessGetInvocation(
            GetInvocation(method, request, "get-1"), service, context, Options(), Clock(), CancellationToken.None);
        PushSubscriptionGetResponse response = method.Codec.DecodePushGetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.PushSubscriptionGet));
            Assert.That(response.List.Select(item => item.Id), Is.EqualTo(new[] { "p1", "p2" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(response.List[0].DeviceClientId, Is.EqualTo("device-1"));
            Assert.That(response.List[0].Expires, Is.Null);
            Assert.That(response.List[0].Url, Is.Null);
            Assert.That(response.List[0].Keys, Is.Null);
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.LastContext, Is.SameAs(context));
        }
    }

    [Test]
    public void GetRejectsPrivatePropertiesWithoutCallingBackend()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        PushSubscriptionGetRequest url = new PushSubscriptionGetRequest { Properties = new List<string> { JmapProperties.Url } };
        PushSubscriptionGetRequest keys = new PushSubscriptionGetRequest { Properties = new List<string> { JmapProperties.Keys } };

        JmapInvocation urlResult = method.ProcessGetInvocation(
            GetInvocation(method, url, "g1"), service, Context(), Options(), Clock(), CancellationToken.None);
        JmapInvocation keysResult = method.ProcessGetInvocation(
            GetInvocation(method, keys, "g2"), service, Context(), Options(), Clock(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, urlResult).Kind, Is.EqualTo(JmapMethodErrorKind.Forbidden));
            Assert.That(Error(method, keysResult).Kind, Is.EqualTo(JmapMethodErrorKind.Forbidden));
            Assert.That(service.GetCalls, Is.Zero);
        }
    }

    [Test]
    public void CreateCommitsBeforeImmediateVerificationAndSupportsCallbackConfirmation()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        JmapRequestContext context = Context();
        JmapServerOptions options = Options();
        FixedClock clock = Clock();
        RaceVerificationSender sender = new RaceVerificationSender();
        sender.Method = method;
        sender.Service = service;
        sender.Context = context;
        sender.Options = options;
        sender.Clock = clock;
        PushSubscriptionSetRequest request = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["c1"] = ValidSubscription() }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            SetInvocation(method, request, "set-1"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSetResponse response = method.Codec.DecodePushSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!["c1"].Id, Is.EqualTo("push-c1"));
            Assert.That(response.Created["c1"].Url, Is.Null);
            Assert.That(response.Created["c1"].Keys, Is.Null);
            Assert.That(response.Created["c1"].VerificationCode, Is.Null);
            Assert.That(sender.Calls, Is.EqualTo(1));
            Assert.That(sender.BackendWasCommitted, Is.True);
            Assert.That(sender.NotificationAllowedBeforeConfirmation, Is.False);
            Assert.That(sender.NotificationAllowedAfterConfirmation, Is.True);
            Assert.That(sender.Verification!.Type, Is.EqualTo("PushVerification"));
            Assert.That(sender.Verification.PushSubscriptionId, Is.EqualTo("push-c1"));
            Assert.That(sender.Verification.VerificationCode, Has.Length.EqualTo(43));
            Assert.That(service.SetCalls, Is.EqualTo(2));
            Assert.That(service.ReceivedVerificationCode, Is.EqualTo(sender.Verification.VerificationCode));
        }
    }

    [Test]
    public void IncorrectVerificationIsRejectedLocallyAndCannotEnableNotifications()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        RecordingVerificationSender sender = new RecordingVerificationSender();
        JmapRequestContext context = Context();
        JmapServerOptions options = Options();
        FixedClock clock = Clock();
        PushSubscriptionSetRequest create = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["c1"] = ValidSubscription() }
        };
        method.ProcessSetInvocation(SetInvocation(method, create, "s1"), service, sender, context, options, null, clock, CancellationToken.None);
        int callsAfterCreate = service.SetCalls;
        PushSubscriptionSetRequest update = VerificationUpdate("push-c1", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        JmapInvocation invocation = method.ProcessSetInvocation(
            SetInvocation(method, update, "s2"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSetResponse response = method.Codec.DecodePushSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotUpdated!["push-c1"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["push-c1"].Properties, Is.EqualTo(new[] { JmapProperties.VerificationCode }));
            Assert.That(service.SetCalls, Is.EqualTo(callsAfterCreate));
            Assert.That(method.CanSendStateChange("push-c1", context, clock), Is.False);
        }
    }

    [Test]
    public void SetAppliesExpiryBoundsAndReturnsPerRecordValidationFailures()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        RecordingVerificationSender sender = new RecordingVerificationSender();
        JmapRequestContext context = Context();
        JmapServerOptions options = Options();
        FixedClock clock = Clock();
        PushSubscription invalid = ValidSubscription();
        invalid.Url = "http://push.example.test/events";
        PushSubscriptionSetRequest request = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription>
            {
                ["good"] = ValidSubscription(),
                ["bad"] = invalid
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["push-immutable"] = Patch((JmapProperties.Url, "\"https://other.example\""))
            }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            SetInvocation(method, request, "set-1"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSetResponse response = method.Codec.DecodePushSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!.Keys, Is.EqualTo(new[] { "good" }));
            Assert.That(response.NotCreated!["bad"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["bad"].Properties, Is.EqualTo(new[] { JmapProperties.Url }));
            Assert.That(response.NotUpdated!["push-immutable"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["push-immutable"].Properties, Is.EqualTo(new[] { JmapProperties.Url }));
            Assert.That(service.LastSetRequest!.Create!.Keys, Is.EqualTo(new[] { "good" }));
            Assert.That(service.LastSetRequest.Create["good"].Expires, Is.EqualTo("2026-09-17T12:00:00Z"));
            Assert.That(sender.Calls, Is.EqualTo(1));
        }
    }

    [Test]
    public void SuccessfulExpiryAndTypeUpdateChangesTrackedSubscriptionWithoutReverification()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        RecordingVerificationSender sender = new RecordingVerificationSender();
        JmapRequestContext context = Context();
        JmapServerOptions options = Options();
        FixedClock clock = Clock();
        PushSubscriptionSetRequest create = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["c1"] = ValidSubscription() }
        };
        method.ProcessSetInvocation(SetInvocation(method, create, "s1"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSetRequest update = new PushSubscriptionSetRequest
        {
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["push-c1"] = Patch(
                    (JmapProperties.Expires, "\"2030-01-01T00:00:00Z\""),
                    (JmapProperties.Types, "[\"Email\",\"https://vendor.example/Todo\"]"))
            }
        };

        method.ProcessSetInvocation(SetInvocation(method, update, "s2"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSecurityState state = method.SecurityStates["push-c1"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.LastSetRequest!.Update!["push-c1"].Values[JmapProperties.Expires].GetString(), Is.EqualTo("2026-09-17T12:00:00Z"));
            Assert.That(state.Subscription!.Expires, Is.EqualTo("2026-09-17T12:00:00Z"));
            Assert.That(state.Subscription.Types, Is.EqualTo(new[] { JmapDataTypes.Email, "https://vendor.example/Todo" }));
            Assert.That(sender.Calls, Is.EqualTo(1));
            Assert.That(method.CanSendStateChange("push-c1", context, clock), Is.False);
        }
    }

    [Test]
    public void DestroyErasesTrackedSecretsAndDisablesDelivery()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        RecordingVerificationSender sender = new RecordingVerificationSender();
        JmapRequestContext context = Context();
        JmapServerOptions options = Options();
        FixedClock clock = Clock();
        PushSubscriptionSetRequest create = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["c1"] = ValidSubscription() }
        };
        method.ProcessSetInvocation(SetInvocation(method, create, "s1"), service, sender, context, options, null, clock, CancellationToken.None);
        PushSubscriptionSecurityState state = method.SecurityStates["push-c1"];
        byte[] key = state.Auth!;
        PushSubscriptionSetRequest destroy = new PushSubscriptionSetRequest { Destroy = new List<string> { "push-c1" } };

        method.ProcessSetInvocation(SetInvocation(method, destroy, "s2"), service, sender, context, options, null, clock, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.SecurityStates.ContainsKey("push-c1"), Is.False);
            Assert.That(key, Has.All.EqualTo(0));
            Assert.That(state.Subscription, Is.Null);
            Assert.That(method.CanSendStateChange("push-c1", context, clock), Is.False);
        }
    }

    [Test]
    public void ClientInvocationAndResponseMappingPreserveTypedPushOperations()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        PushSubscriptionGetRequest get = new PushSubscriptionGetRequest { Ids = new List<string> { "push-1" } };
        PushSubscriptionSetRequest set = new PushSubscriptionSetRequest
        {
            Create = new Dictionary<string, PushSubscription> { ["c1"] = ValidSubscription() }
        };

        JmapInvocation getInvocation = method.CreateGetInvocation(get, "g1");
        JmapInvocation setInvocation = method.CreateSetInvocation(set, "s1");
        JmapResponse getWire = Response(JmapMethods.PushSubscriptionGet, "g1", "{\"list\":[{\"id\":\"push-1\",\"deviceClientId\":\"device-hash\"}],\"notFound\":[]}");
        JmapResponse setWire = Response(JmapMethods.PushSubscriptionSet, "s1", "{\"created\":{\"c1\":{\"id\":\"push-1\",\"expires\":\"2026-09-17T12:00:00Z\"}}}");

        PushSubscriptionGetResponse getResult = method.MapGetResponse(getWire, "g1", get);
        PushSubscriptionSetResponse setResult = method.MapSetResponse(setWire, "s1", set);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getInvocation.Name, Is.EqualTo(JmapMethods.PushSubscriptionGet));
            Assert.That(method.Codec.DecodePushGetRequest(getInvocation.Arguments).Ids, Is.EqualTo(new[] { "push-1" }));
            Assert.That(setInvocation.Name, Is.EqualTo(JmapMethods.PushSubscriptionSet));
            Assert.That(method.Codec.DecodePushSetRequest(setInvocation.Arguments).Create!.ContainsKey("c1"), Is.True);
            Assert.That(getResult.List[0].Id, Is.EqualTo("push-1"));
            Assert.That(setResult.Created!["c1"].Id, Is.EqualTo("push-1"));
        }
    }

    [Test]
    public void ClientMappingRejectsPrivateGetDataAndConvertsMethodErrors()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        PushSubscriptionGetRequest request = new PushSubscriptionGetRequest { Ids = new List<string> { "push-1" } };
        JmapResponse leaked = Response(
            JmapMethods.PushSubscriptionGet,
            "g1",
            "{\"list\":[{\"id\":\"push-1\",\"url\":\"https://private.example\"}],\"notFound\":[]}");
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "g2",
            "{\"type\":\"forbidden\",\"description\":\"Private property\"}");

        Assert.That(() => method.MapGetResponse(leaked, "g1", request), Throws.TypeOf<JmapProtocolException>());
        JmapMethodException error = Assert.Throws<JmapMethodException>(() => method.MapGetResponse(failed, "g2", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.MethodName, Is.EqualTo(JmapMethods.PushSubscriptionGet));
            Assert.That(error.ErrorToken, Is.EqualTo(JmapErrorCodes.Forbidden));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.Forbidden));
        }
    }

    [Test]
    public void LimitsAndInactiveCredentialsFailBeforeBackendDispatch()
    {
        PushSubscriptionMethod method = new PushSubscriptionMethod();
        FakePushService service = new FakePushService();
        JmapServerOptions options = Options();
        options.MaxObjectsInGet = 1;
        PushSubscriptionGetRequest tooMany = new PushSubscriptionGetRequest { Ids = new List<string> { "p1", "p2" } };
        JmapRequestContext revoked = Context();
        revoked.CredentialIsRevoked = true;

        JmapInvocation limited = method.ProcessGetInvocation(
            GetInvocation(method, tooMany, "g1"), service, Context(), options, Clock(), CancellationToken.None);
        JmapInvocation forbidden = method.ProcessGetInvocation(
            GetInvocation(method, new PushSubscriptionGetRequest(), "g2"), service, revoked, options, Clock(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, limited).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, forbidden).Kind, Is.EqualTo(JmapMethodErrorKind.Forbidden));
            Assert.That(service.GetCalls, Is.Zero);
        }
    }

    private static PushSubscription ValidSubscription()
    {
        return new PushSubscription
        {
            DeviceClientId = "device-hash",
            Url = "https://push.example.test/events",
            Keys = new PushSubscriptionKeys
            {
                P256dh = Base64Url(Convert.FromHexString("046B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C2964FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5")),
                Auth = Base64Url(new byte[16])
            }
        };
    }

    private static PushSubscription StoredSubscription(string id, string device)
    {
        PushSubscription subscription = ValidSubscription();
        subscription.Id = id;
        subscription.DeviceClientId = device;
        subscription.VerificationCode = "verified-code";
        subscription.Expires = "2026-09-17T12:00:00Z";
        subscription.UnknownProperties["vendor"] = Json("true");
        return subscription;
    }

    private static PushSubscriptionSetRequest VerificationUpdate(string id, string code)
    {
        return new PushSubscriptionSetRequest
        {
            Update = new Dictionary<string, JmapPatchObject>
            {
                [id] = Patch((JmapProperties.VerificationCode, JsonSerializer.Serialize(code)))
            }
        };
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in values)
        {
            patch.Values[path] = Json(json);
        }

        return patch;
    }

    private static JmapInvocation GetInvocation(PushSubscriptionMethod method, PushSubscriptionGetRequest request, string callId)
    {
        return method.CreateGetInvocation(request, callId);
    }

    private static JmapInvocation SetInvocation(PushSubscriptionMethod method, PushSubscriptionSetRequest request, string callId)
    {
        return method.CreateSetInvocation(request, callId);
    }

    private static JmapResponse Response(string name, string callId, string arguments)
    {
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(new JmapInvocation { Name = name, CallId = callId, Arguments = Json(arguments) });
        return response;
    }

    private static JmapMethodError Error(PushSubscriptionMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JmapRequestContext Context()
    {
        return new JmapRequestContext { UserId = "user-1", CredentialId = "credential-1" };
    }

    private static JmapServerOptions Options()
    {
        return new JmapServerOptions { MaxObjectsInGet = 10, MaxObjectsInSet = 10 };
    }

    private static FixedClock Clock()
    {
        return new FixedClock();
    }

    private static string Base64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private class FixedClock : IJmapClock
    {
        public DateTimeOffset Now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset GetCurrentUtcDateTime()
        {
            return Now;
        }
    }

    private class RecordingVerificationSender : IPushVerificationSender
    {
        public int Calls;
        public PushVerification? Verification;

        public Task SendPushVerificationAsync(PushSubscription subscription, PushVerification verification, JmapRequestContext context, CancellationToken cancellationToken)
        {
            Calls = Calls + 1;
            Verification = verification;
            return Task.CompletedTask;
        }
    }

    private class RaceVerificationSender : IPushVerificationSender
    {
        public PushSubscriptionMethod? Method;
        public FakePushService? Service;
        public JmapRequestContext? Context;
        public JmapServerOptions? Options;
        public FixedClock? Clock;
        public int Calls;
        public bool BackendWasCommitted;
        public bool NotificationAllowedBeforeConfirmation;
        public bool NotificationAllowedAfterConfirmation;
        public PushVerification? Verification;

        public Task SendPushVerificationAsync(PushSubscription subscription, PushVerification verification, JmapRequestContext context, CancellationToken cancellationToken)
        {
            Calls = Calls + 1;
            Verification = verification;
            BackendWasCommitted = Service!.StoredIds.Contains(verification.PushSubscriptionId!);
            NotificationAllowedBeforeConfirmation = Method!.CanSendStateChange(verification.PushSubscriptionId, Context, Clock);
            PushSubscriptionSetRequest confirm = VerificationUpdate(verification.PushSubscriptionId!, verification.VerificationCode!);
            Method.ProcessSetInvocation(
                SetInvocation(Method, confirm, "confirm"),
                Service,
                this,
                Context!,
                Options!,
                null,
                Clock,
                cancellationToken);
            NotificationAllowedAfterConfirmation = Method.CanSendStateChange(verification.PushSubscriptionId, Context, Clock);
            return Task.CompletedTask;
        }
    }

    private class FakePushService : IPushService
    {
        public PushSubscriptionGetResponse GetResponse = new PushSubscriptionGetResponse();
        public PushSubscriptionGetRequest? LastGetRequest;
        public PushSubscriptionSetRequest? LastSetRequest;
        public JmapRequestContext? LastContext;
        public HashSet<string> StoredIds = new HashSet<string>(StringComparer.Ordinal);
        public int GetCalls;
        public int SetCalls;
        public string? ReceivedVerificationCode;

        public Task<PushSubscriptionGetResponse> GetStoredPushSubscriptionsAsync(PushSubscriptionGetRequest request, JmapRequestContext context, CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            LastContext = context;
            return Task.FromResult(GetResponse);
        }

        public Task<PushSubscriptionSetResponse> SetStoredPushSubscriptionsAsync(PushSubscriptionSetRequest request, JmapRequestContext context, CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            LastContext = context;
            PushSubscriptionSetResponse response = new PushSubscriptionSetResponse();
            if (request.Create != null)
            {
                response.Created = new Dictionary<string, PushSubscription>();
                foreach (KeyValuePair<string, PushSubscription> item in request.Create)
                {
                    string id = "push-" + item.Key;
                    StoredIds.Add(id);
                    ReceivedVerificationCode = item.Value.VerificationCode;
                    response.Created.Add(item.Key, new PushSubscription { Id = id, Expires = item.Value.Expires });
                }
            }

            if (request.Update != null)
            {
                response.Updated = new Dictionary<string, JsonElement>();
                foreach (string id in request.Update.Keys)
                {
                    if (StoredIds.Contains(id))
                    {
                        response.Updated.Add(id, Json("null"));
                    }
                    else
                    {
                        if (response.NotUpdated == null)
                        {
                            response.NotUpdated = new Dictionary<string, JmapSetError>();
                        }

                        response.NotUpdated.Add(id, new JmapSetError { Type = JmapErrorCodes.NotFound });
                    }
                }

                if (response.Updated.Count == 0)
                {
                    response.Updated = null;
                }
            }

            if (request.Destroy != null)
            {
                response.Destroyed = new List<string>();
                foreach (string id in request.Destroy)
                {
                    if (StoredIds.Remove(id))
                    {
                        response.Destroyed.Add(id);
                    }
                    else
                    {
                        if (response.NotDestroyed == null)
                        {
                            response.NotDestroyed = new Dictionary<string, JmapSetError>();
                        }

                        response.NotDestroyed.Add(id, new JmapSetError { Type = JmapErrorCodes.NotFound });
                    }
                }

                if (response.Destroyed.Count == 0)
                {
                    response.Destroyed = null;
                }
            }

            return Task.FromResult(response);
        }
    }
}
