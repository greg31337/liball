using System.Text.Json;

namespace jmap.Tests;

public class PublicReferenceAndPatchTests
{
    private const string VendorCapability = "https://example.test/capability/references";

    [Test]
    public async Task BuilderCodecAndServerResolveEscapedAndWildcardReferencesInOrder()
    {
        JmapSession session = PublicApiTestHelper.CoreSession("session-state");
        IJmapServer server = CoreServer(session);
        IJmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddRequestCapability(JmapCapabilities.Core);
        builder.AddTypedMethodCall(
            JmapMethods.CoreEcho,
            Json("{\"a/b\":{\"m~n\":\"escaped\"},\"groups\":[" +
                "{\"ids\":[\"b\",\"a\"]},{\"ids\":[]},{\"ids\":[\"c\"]}]}"),
            new List<string> { JmapCapabilities.Core },
            "seed");

        JmapRawMethodCall projected = RawCall(JmapMethods.CoreEcho, "projected", "{\"keep\":true}");
        projected.ResultReferences.Add(
            "#escaped",
            builder.CreateResultReference("seed", JmapMethods.CoreEcho, "/a~1b/m~0n"));
        projected.ResultReferences.Add(
            "#ids",
            builder.CreateResultReference("seed", JmapMethods.CoreEcho, "/groups/*/ids"));
        builder.AddRawMethodCall(projected);

        JmapRawMethodCall invalid = RawCall(JmapMethods.CoreEcho, "invalid", "{}");
        invalid.ResultReferences.Add(
            "#missing",
            builder.CreateResultReference("seed", JmapMethods.CoreEcho, "/groups/*/missing"));
        builder.AddRawMethodCall(invalid);
        builder.AddTypedMethodCall(
            JmapMethods.CoreEcho,
            Json("{\"continued\":true}"),
            new List<string> { JmapCapabilities.Core },
            "last");

        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = "https://jmap.example.test/session";
        JmapClient client = new JmapClient(options, new ServerTransport(server, session));
        JmapResponse response = await client.ExecuteBatchAsync(
            builder.BuildJmapRequest(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.CallId),
                Is.EqualTo(new[] { "seed", "projected", "invalid", "last" }));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("escaped").GetString(),
                Is.EqualTo("escaped"));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("ids")
                .EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(new[] { "b", "a", "c" }));
            Assert.That(response.MethodResponses[1].Arguments.GetProperty("keep").GetBoolean(),
                Is.True);
            Assert.That(response.MethodResponses[2].Name, Is.EqualTo("error"));
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidResultReference));
            Assert.That(response.MethodResponses[3].Arguments.GetProperty("continued").GetBoolean(),
                Is.True);
        }
    }

    [Test]
    public async Task CreatedIdsResolveValuesMapKeysAndPatchPathTokensAcrossCalls()
    {
        RecordingVendorHandler handler = new RecordingVendorHandler();
        IJmapServer server = VendorServer(handler);
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddRequestCapability(JmapCapabilities.Core);
        builder.AddRequestCapability(VendorCapability);
        builder.AddRequestCreatedId("carry", "C1");
        JmapRawMethodCall create = RawCall("Vendor/create", "create", "{}");
        create.RequiredCapabilities.Add(VendorCapability);
        builder.AddRawMethodCall(create);
        JmapRawMethodCall use = RawCall(
            "Vendor/use",
            "use",
            "{\"carried\":\"#carry\",\"nested\":[\"#made\"]," +
                "\"idMap\":{\"#made\":true},\"update\":{" +
                "\"record-1\":{\"mailboxIds/#made\":true," +
                "\"related\":[\"#carry\"]}}}");
        use.RequiredCapabilities.Add(VendorCapability);
        builder.AddRawMethodCall(use);

        JmapResponse response = await ProcessAsync(server, builder.BuildJmapRequest());
        JsonElement resolved = handler.Arguments[1];
        JsonElement patch = resolved.GetProperty("update").GetProperty("record-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handler.MethodNames, Is.EqualTo(new[] { "Vendor/create", "Vendor/use" }));
            Assert.That(resolved.GetProperty("carried").GetString(), Is.EqualTo("C1"));
            Assert.That(resolved.GetProperty("nested")[0].GetString(), Is.EqualTo("M1"));
            Assert.That(resolved.GetProperty("idMap").GetProperty("M1").GetBoolean(), Is.True);
            Assert.That(patch.GetProperty("mailboxIds/M1").GetBoolean(), Is.True);
            Assert.That(patch.GetProperty("related")[0].GetString(), Is.EqualTo("C1"));
            Assert.That(response.CreatedIds, Is.EqualTo(new Dictionary<string, string>
            {
                ["carry"] = "C1",
                ["made"] = "M1"
            }));
        }
    }

    [Test]
    public async Task EmptyAndOverlappingPatchPathsFailBeforeTheBackendWhileValidUpdatesPass()
    {
        RecordingMailboxService mailboxes = new RecordingMailboxService();
        IJmapServer server = MailServer(mailboxes);
        JmapRequest invalid = Request(
            JmapMethods.MailboxSet,
            "invalid",
            "{\"accountId\":\"account-1\",\"update\":{" +
                "\"m1\":{\"\":\"bad\"}," +
                "\"m2\":{\"name\":\"Parent\",\"name/child\":\"bad\"}}}",
            JmapCapabilities.Mail);

        JmapResponse rejected = await ProcessAsync(server, invalid);
        JsonElement failures = rejected.MethodResponses[0].Arguments.GetProperty("notUpdated");
        JmapRequest valid = Request(
            JmapMethods.MailboxSet,
            "valid",
            "{\"accountId\":\"account-1\",\"update\":{" +
                "\"m1\":{\"name\":\"After\"}}}",
            JmapCapabilities.Mail);
        JmapResponse accepted = await ProcessAsync(server, valid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures.GetProperty("m1").GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidPatch));
            Assert.That(failures.GetProperty("m2").GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidPatch));
            Assert.That(mailboxes.SetCalls, Is.EqualTo(1));
            Assert.That(mailboxes.LastSetRequest!.Update!["m1"].Values["name"].GetString(),
                Is.EqualTo("After"));
            Assert.That(accepted.MethodResponses[0].Arguments.GetProperty("updated")
                .GetProperty("m1").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    private static IJmapServer CoreServer(JmapSession session)
    {
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(session)
        };
        return new JmapServer(new JmapServerOptions(), services);
    }

    private static IJmapServer VendorServer(RecordingVendorHandler handler)
    {
        JmapSession session = PublicApiTestHelper.CoreSession("session-state");
        session.Capabilities.Add(VendorCapability, new JmapCapability());
        JmapVendorMethodRegistration create = new JmapVendorMethodRegistration
        {
            MethodName = "Vendor/create",
            Handler = handler
        };
        create.RequiredCapabilities.Add(VendorCapability);
        JmapVendorMethodRegistration use = new JmapVendorMethodRegistration
        {
            MethodName = "Vendor/use",
            Handler = handler
        };
        use.RequiredCapabilities.Add(VendorCapability);
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(session)
        };
        services.VendorCapabilities.Add(VendorCapability);
        services.VendorMethods.Add(create);
        services.VendorMethods.Add(use);
        return new JmapServer(new JmapServerOptions(), services);
    }

    private static IJmapServer MailServer(RecordingMailboxService mailboxes)
    {
        JmapSession session = PublicApiTestHelper.CoreSession("session-state");
        JmapMailCapabilities mail = new JmapMailCapabilities
        {
            MaxSizeMailboxName = 255,
            MayCreateTopLevelMailbox = true
        };
        session.Accounts["account-1"].AccountCapabilities.Add(
            JmapCapabilities.Mail, new JmapCapability { Mail = mail });
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(session),
            MailboxService = mailboxes
        };
        return new JmapServer(new JmapServerOptions(), services);
    }

    private static JmapRequest Request(
        string methodName,
        string callId,
        string arguments,
        string capability)
    {
        JmapRequestBuilder builder = new JmapRequestBuilder();
        builder.AddTypedMethodCall(
            methodName,
            Json(arguments),
            new List<string> { JmapCapabilities.Core, capability },
            callId);
        return builder.BuildJmapRequest();
    }

    private static JmapRawMethodCall RawCall(string name, string callId, string arguments)
    {
        return new JmapRawMethodCall
        {
            Name = name,
            CallId = callId,
            Arguments = Json(arguments)
        };
    }

    private static async Task<JmapResponse> ProcessAsync(IJmapServer server, JmapRequest request)
    {
        IJmapCodec codec = new JmapJsonCodec();
        byte[] body = codec.EncodeRequestBytes(request);
        JmapEndpointRequest endpoint = new JmapEndpointRequest
        {
            Context = PublicApiTestHelper.AuthenticatedContext(),
            Method = "POST",
            ContentType = JmapMediaTypes.Json,
            Body = new MemoryStream(body, false),
            ContentLength = body.Length,
            HasContentLength = true
        };
        JmapEndpointResponse response = await server.ProcessApiEndpointAsync(
            endpoint, CancellationToken.None);
        Assert.That(response.StatusCode, Is.EqualTo(200));
        return codec.DecodeResponseMemory(response.BodyBytes!);
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class RecordingVendorHandler : IJmapVendorMethodHandler
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
            JmapInvocation response = new JmapInvocation
            {
                Name = invocation.Name,
                CallId = invocation.CallId,
                Arguments = invocation.Name == "Vendor/create"
                    ? Json("{\"created\":{\"made\":{\"id\":\"M1\"}}}")
                    : invocation.Arguments.Clone()
            };
            return Task.FromResult(response);
        }
    }

    private sealed class RecordingMailboxService : IMailboxService
    {
        public int SetCalls;
        public MailboxSetRequest? LastSetRequest;

        public Task<MailboxGetResponse> GetStoredMailboxesAsync(
            MailboxGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            MailboxGetResponse response = new MailboxGetResponse
            {
                AccountId = "account-1",
                State = "mailbox-state"
            };
            response.List.Add(Mailbox("m1", "Before"));
            response.List.Add(Mailbox("m2", "Second"));
            return Task.FromResult(response);
        }

        public Task<MailboxSetResponse> SetStoredMailboxesAsync(
            MailboxSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            MailboxSetResponse response = new MailboxSetResponse
            {
                AccountId = "account-1",
                OldState = "mailbox-state",
                NewState = "mailbox-state-2",
                Updated = new Dictionary<string, JsonElement>()
            };
            foreach (string id in request.Update!.Keys)
            {
                response.Updated.Add(id, Json("null"));
            }

            return Task.FromResult(response);
        }

        public Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(
            MailboxChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<MailboxQueryResponse> QueryStoredMailboxesAsync(
            MailboxQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(
            MailboxQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private static Mailbox Mailbox(string id, string name)
        {
            return new Mailbox
            {
                Id = id,
                Name = name,
                MyRights = new MailboxRights { MayRename = true }
            };
        }
    }

    private sealed class ServerTransport : IJmapTransport
    {
        public IJmapServer Server;
        public JmapSession Session;

        public ServerTransport(IJmapServer server, JmapSession session)
        {
            Server = server;
            Session = session;
        }

        public Task<JmapSession> GetTransportSessionAsync(
            string sessionUrl,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Session);
        }

        public Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            return ProcessAsync(Server, request);
        }

        public Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
