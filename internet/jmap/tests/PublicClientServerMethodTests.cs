using System.Text;
using System.Text.Json;
using JmapThread = jmap.Thread;

namespace jmap.Tests;

public class PublicClientServerMethodTests
{
    private const string VendorCapability = "https://vendor.example.test/jmap/widgets";

    [Test]
    public async Task VendorExtensionsRoundTripThroughPublicClientAndServerWithoutRelaxingValidation()
    {
        JmapSession session = Session();
        JmapCapability globalVendor = new JmapCapability();
        globalVendor.UnknownProperties["maxWidgets"] = Json("23");
        JmapCapability accountVendor = new JmapCapability();
        accountVendor.UnknownProperties["tier"] = Json("\"gold\"");
        session.Capabilities[VendorCapability] = globalVendor;
        session.Accounts["account-1"].AccountCapabilities[VendorCapability] = accountVendor;
        session.PrimaryAccounts[VendorCapability] = "account-1";

        ScriptedMailboxService mailboxes = new ScriptedMailboxService();
        Mailbox mailbox = CompleteMailbox("inbox", "Inbox", null, JmapMailboxRoles.Inbox, 3);
        mailbox.UnknownProperties["vendorColor"] = Json("\"violet\"");
        mailbox.MyRights!.UnknownProperties["vendorAcl"] = Json("{\"share\":true}");
        mailboxes.GetResponse.List.Add(mailbox);
        mailboxes.GetResponse.UnknownProperties["vendorPage"] = Json("{\"cursor\":9}");
        VendorInteropHandler handler = new VendorInteropHandler();
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(session),
            MailboxService = mailboxes
        };
        services.VendorCapabilities.Add(VendorCapability);
        services.VendorMethods.Add(VendorRegistration("Widget/inspect", handler));
        services.VendorMethods.Add(VendorRegistration("Widget/fail", handler));
        ServerTransport transport = new ServerTransport(
            new JmapServer(new JmapServerOptions(), services));
        JmapClient implementation = new JmapClient(ClientOptions(), transport);
        IJmapClient client = implementation;

        JmapSession discovered = await client.DiscoverSessionAsync(
            ClientOptions().DiscoveryOptions, CancellationToken.None);
        MailboxGetRequest getRequest = new MailboxGetRequest { AccountId = "account-1" };
        getRequest.UnknownProperties["vendorProjection"] = Json("[\"color\",\"acl\"]");
        MailboxGetResponse typed = await implementation.GetMailboxesAsync(
            getRequest, CancellationToken.None);

        JmapRequest batch = new JmapRequest();
        batch.Using.AddRange(new[]
        {
            JmapCapabilities.Core, JmapCapabilities.Mail, VendorCapability
        });
        batch.UnknownProperties["vendorRequest"] = Json("{\"trace\":\"r-7\"}");
        batch.MethodCalls.Add(Invocation("Widget/inspect", "inspect", """
            {"payload":{"kind":"widget","values":[1,null,{"future":true}]}}
            """));
        batch.MethodCalls.Add(Invocation("Widget/fail", "vendor-error", "{}"));
        batch.MethodCalls.Add(Invocation(
            JmapMethods.MailboxGet, "invalid", "{\"accountId\":\"account-1\",\"ids\":\"bad\",\"vendorFlag\":true}"));
        batch.MethodCalls.Add(Invocation(
            JmapMethods.CoreEcho, "continued", "{\"afterError\":true}"));

        JmapResponse response = await client.ExecuteBatchAsync(batch, CancellationToken.None);
        JmapCallResult vendorError = client.FindCallResult(response, "vendor-error");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(discovered.Capabilities[VendorCapability]
                .UnknownProperties["maxWidgets"].GetInt32(), Is.EqualTo(23));
            Assert.That(discovered.Accounts["account-1"].AccountCapabilities[VendorCapability]
                .UnknownProperties["tier"].GetString(), Is.EqualTo("gold"));
            Assert.That(client.GetPrimaryAccountId(VendorCapability), Is.EqualTo("account-1"));
            Assert.That(mailboxes.LastGetRequest!.UnknownProperties["vendorProjection"][0]
                .GetString(), Is.EqualTo("color"));
            Assert.That(typed.List[0].UnknownProperties["vendorColor"].GetString(),
                Is.EqualTo("violet"));
            Assert.That(typed.List[0].MyRights!.UnknownProperties["vendorAcl"]
                .GetProperty("share").GetBoolean(), Is.True);
            Assert.That(typed.UnknownProperties["vendorPage"].GetProperty("cursor").GetInt32(),
                Is.EqualTo(9));
            Assert.That(handler.Arguments[0].GetProperty("payload").GetProperty("values")[2]
                .GetProperty("future").GetBoolean(), Is.True);
            Assert.That(response.MethodResponses[0].Arguments.GetProperty("vendorResult")
                .GetProperty("code").GetInt32(), Is.EqualTo(42));
            Assert.That(vendorError.Error!.Type, Is.EqualTo("vendorBusy"));
            Assert.That(vendorError.Error.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(vendorError.Error.UnknownProperties["retryAfter"].GetInt32(),
                Is.EqualTo(7));
            Assert.That(vendorError.Error.UnknownProperties["region"].GetString(),
                Is.EqualTo("west"));
            Assert.That(response.MethodResponses[2].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(response.MethodResponses[3].Arguments.GetProperty("afterError").GetBoolean(),
                Is.True);
            Assert.That(handler.MethodNames, Is.EqualTo(new[] { "Widget/inspect", "Widget/fail" }));
        }
    }

    [Test]
    public async Task MultiCallBatchCarriesReferencesImplicitResponsesStatesAndPartialErrorsEndToEnd()
    {
        JmapSession session = ScenarioSession();
        FakeJmapStatePublisher externalPublisher = new FakeJmapStatePublisher();
        ScenarioServices scenario = new ScenarioServices(session);
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(session),
            MailboxService = scenario,
            EmailService = scenario,
            IdentityService = scenario,
            EmailSubmissionService = scenario,
            StatePublisher = externalPublisher,
            Clock = new FakeJmapClock()
        };
        IJmapServer server = new JmapServer(new JmapServerOptions(), services);
        scenario.Publisher = services.StatePublisher!;
        IJmapClient client = new JmapClient(ClientOptions(), new ServerTransport(server));
        JmapRequest request = new JmapRequest();
        request.Using.AddRange(new[]
        {
            JmapCapabilities.Core, JmapCapabilities.Mail, JmapCapabilities.Submission
        });
        request.CreatedIds = new Dictionary<string, string>();
        request.MethodCalls.Add(Invocation(JmapMethods.MailboxSet, "m-set", """
            {"accountId":"account-1","create":{
              "folder":{"name":"Projects"},
              "invalid":{"name":""}
            }}
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.MailboxGet, "m-get", """
            {"accountId":"account-1","ids":["#folder"]}
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.MailboxGet, "m-back", """
            {"accountId":"account-1",
             "#ids":{"resultOf":"m-get","name":"Mailbox/get","path":"/list/*/id"}}
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.EmailSet, "e-set", """
            {"accountId":"account-1","create":{"message":{
              "mailboxIds":{"#folder":true},"keywords":{"$draft":true},
              "from":[{"name":null,"email":"sender@example.test"}],
              "to":[{"name":null,"email":"recipient@example.test"}],
              "subject":"End-to-end batch",
              "textBody":[{"partId":"text","type":"text/plain"}],
              "bodyValues":{"text":{"value":"Hello."}}
            }}}
            """));
        request.MethodCalls.Add(Invocation(JmapMethods.EmailSubmissionSet, "s-set", """
            {"accountId":"account-1","create":{"send":{
              "identityId":"identity-1","emailId":"#message"
            }},"update":{"submission-existing":{"undoStatus":"canceled"}},
            "onSuccessUpdateEmail":{
              "submission-existing":{"keywords/$draft":null,"keywords/$seen":true}
            }}
            """));
        request.MethodCalls.Add(Invocation(
            JmapMethods.EmailGet, "bad", "{\"accountId\":\"account-1\",\"ids\":\"wrong-shape\"}"));
        request.MethodCalls.Add(Invocation(
            JmapMethods.CoreEcho, "after", "{\"continued\":true}"));

        JmapResponse response = await client.ExecuteBatchAsync(request, CancellationToken.None);
        JsonElement mailboxSet = response.MethodResponses[0].Arguments;
        JsonElement mailboxGet = response.MethodResponses[1].Arguments;
        JsonElement emailSet = response.MethodResponses[3].Arguments;
        JsonElement submissionSet = response.MethodResponses[4].Arguments;
        JsonElement implicitEmailSet = response.MethodResponses[5].Arguments;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.MethodResponses.Select(value => value.Name), Is.EqualTo(new[]
            {
                JmapMethods.MailboxSet, JmapMethods.MailboxGet, JmapMethods.MailboxGet,
                JmapMethods.EmailSet, JmapMethods.EmailSubmissionSet, JmapMethods.EmailSet, "error",
                JmapMethods.CoreEcho
            }));
            Assert.That(response.MethodResponses.Select(value => value.CallId), Is.EqualTo(new[]
            {
                "m-set", "m-get", "m-back", "e-set", "s-set", "s-set", "bad", "after"
            }));
            Assert.That(response.CreatedIds, Is.EqualTo(new Dictionary<string, string>
            {
                ["folder"] = "mailbox-projects",
                ["message"] = "email-1",
                ["send"] = "submission-1"
            }));
            Assert.That(mailboxSet.GetProperty("notCreated").GetProperty("invalid")
                .GetProperty("type").GetString(), Is.EqualTo(JmapErrorCodes.InvalidProperties));
            Assert.That(mailboxGet.GetProperty("list")[0].GetProperty("id").GetString(),
                Is.EqualTo("mailbox-projects"));
            Assert.That(scenario.EmailCreateMailboxId, Is.EqualTo("mailbox-projects"));
            Assert.That(scenario.SubmissionEmailId, Is.EqualTo("email-1"));
            Assert.That(emailSet.GetProperty("newState").GetString(), Is.EqualTo("email-2"));
            Assert.That(submissionSet.GetProperty("newState").GetString(),
                Is.EqualTo("submission-2"));
            Assert.That(implicitEmailSet.GetProperty("updated").TryGetProperty(
                "email-1", out JsonElement unused), Is.True);
            Assert.That(scenario.ImplicitEmailPatch!.Values["keywords/$seen"].GetBoolean(), Is.True);
            Assert.That(response.MethodResponses[6].Arguments.GetProperty("type").GetString(),
                Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(response.MethodResponses[7].Arguments.GetProperty("continued").GetBoolean(),
                Is.True);
            Assert.That(response.SessionState, Is.EqualTo("session-after"));
            Assert.That(externalPublisher.Publications.Select(value =>
                value.StateChange.Changed["account-1"].Single().Key), Is.EqualTo(new[]
            {
                JmapDataTypes.Mailbox, JmapDataTypes.Email,
                JmapDataTypes.EmailSubmission, JmapDataTypes.Email
            }));
        }
    }

    [Test]
    public async Task ClientAndServerRoundTripEveryStandardMethodShape()
    {
        RoundTripMailboxService mailboxService = new RoundTripMailboxService();
        FakeJmapBlobService blobService = new FakeJmapBlobService();
        blobService.CopyResponse = new BlobCopyResponse
        {
            FromAccountId = "source",
            AccountId = "account-1",
            Copied = new Dictionary<string, string> { ["blob-1"] = "copied-1" },
            NotCopied = new Dictionary<string, JmapSetError>
            {
                ["missing"] = new JmapSetError { Type = JmapErrorCodes.NotFound }
            }
        };
        IJmapServer server = Server(mailboxService, blobService, new JmapServerOptions());
        ServerTransport transport = new ServerTransport(server);
        JmapClient implementation = new JmapClient(ClientOptions(), transport);
        IJmapClient client = implementation;
        IMailboxClient mailboxes = implementation;
        IBlobClient blobs = implementation;

        JsonElement echoed = await client.ExecuteEchoAsync(
            Json("{\"answer\":42}"), CancellationToken.None);
        MailboxGetResponse get = await mailboxes.GetMailboxesAsync(
            new MailboxGetRequest(), CancellationToken.None);
        MailboxChangesResponse changes = await mailboxes.GetMailboxChangesAsync(
            new MailboxChangesRequest
            {
                SinceState = "mailbox-0",
                HasMaxChanges = true,
                MaxChanges = 3
            },
            CancellationToken.None);
        MailboxQueryResponse query = await mailboxes.QueryMailboxesAsync(
            new MailboxQueryRequest
            {
                HasPosition = true,
                Position = 1,
                HasLimit = true,
                Limit = 2,
                HasCalculateTotal = true,
                CalculateTotal = true
            },
            CancellationToken.None);
        MailboxQueryChangesResponse queryChanges = await mailboxes.GetMailboxQueryChangesAsync(
            new MailboxQueryChangesRequest
            {
                SinceQueryState = "query-0",
                HasMaxChanges = true,
                MaxChanges = 2,
                HasCalculateTotal = true,
                CalculateTotal = true
            },
            CancellationToken.None);
        MailboxSetResponse set = await mailboxes.SetMailboxesAsync(
            new MailboxSetRequest
            {
                Destroy = new List<string> { "missing-mailbox" }
            },
            CancellationToken.None);
        BlobCopyRequest copyRequest = new BlobCopyRequest
        {
            FromAccountId = "source",
            AccountId = "account-1"
        };
        copyRequest.BlobIds.Add("blob-1");
        copyRequest.BlobIds.Add("missing");
        BlobCopyResponse copy = await blobs.CopyBlobAsync(copyRequest, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(echoed.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(get.AccountId, Is.EqualTo("account-1"));
            Assert.That(get.List, Is.Empty);
            Assert.That(changes.OldState, Is.EqualTo("mailbox-0"));
            Assert.That(changes.NewState, Is.EqualTo("mailbox-1"));
            Assert.That(changes.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(changes.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(changes.Destroyed, Is.EqualTo(new[] { "destroyed" }));
            Assert.That(query.Position, Is.EqualTo(1));
            Assert.That(query.Ids, Is.EqualTo(new[] { "mailbox-2", "mailbox-3" }));
            Assert.That(query.Total, Is.EqualTo(4));
            Assert.That(queryChanges.Removed, Is.EqualTo(new[] { "mailbox-1" }));
            Assert.That(queryChanges.Added.Select(item => item.Id), Is.EqualTo(new[] { "mailbox-3" }));
            Assert.That(queryChanges.Added[0].Index, Is.EqualTo(1));
            Assert.That(set.NotDestroyed!["missing-mailbox"].Kind,
                Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(copy.Copied!["blob-1"], Is.EqualTo("copied-1"));
            Assert.That(copy.NotCopied!["missing"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(mailboxService.Calls, Is.EqualTo(new[]
            {
                JmapMethods.MailboxGet,
                JmapMethods.MailboxChanges,
                JmapMethods.MailboxQuery,
                JmapMethods.MailboxQueryChanges,
                JmapMethods.MailboxGet
            }));
            Assert.That(transport.ApiRequests.Select(request => request.MethodCalls[0].Name),
                Is.EqualTo(new[]
                {
                    JmapMethods.CoreEcho,
                    JmapMethods.MailboxGet,
                    JmapMethods.MailboxChanges,
                    JmapMethods.MailboxQuery,
                    JmapMethods.MailboxQueryChanges,
                    JmapMethods.MailboxSet,
                    JmapMethods.BlobCopy
                }));
        }
    }

    [Test]
    public void ServerLimitsAndStateErrorsReachTypedClientCallers()
    {
        RoundTripMailboxService mailboxService = new RoundTripMailboxService();
        JmapServerOptions serverOptions = new JmapServerOptions
        {
            MaxObjectsInGet = 1
        };
        ServerTransport transport = new ServerTransport(
            Server(mailboxService, new FakeJmapBlobService(), serverOptions));
        JmapClient implementation = new JmapClient(ClientOptions(), transport);
        IMailboxClient client = implementation;
        MailboxGetRequest getRequest = new MailboxGetRequest
        {
            Ids = new List<string> { "mailbox-1", "mailbox-2" }
        };
        MailboxSetRequest setRequest = new MailboxSetRequest
        {
            IfInState = "stale-state"
        };

        JmapMethodException limit = Assert.ThrowsAsync<JmapMethodException>(async () =>
            await client.GetMailboxesAsync(getRequest, CancellationToken.None))!;
        JmapStateMismatchException state = Assert.ThrowsAsync<JmapStateMismatchException>(async () =>
            await client.SetMailboxesAsync(setRequest, CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(limit.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(limit.MethodName, Is.EqualTo(JmapMethods.MailboxGet));
            Assert.That(state.ExpectedState, Is.EqualTo("stale-state"));
            Assert.That(state.MethodName, Is.EqualTo(JmapMethods.MailboxSet));
            Assert.That(mailboxService.Calls, Is.EqualTo(new[] { JmapMethods.MailboxGet }));
        }
    }

    [Test]
    public async Task BlobClientRoundTripsStreamingEndpointsAndNotFoundProblemsThroughServer()
    {
        FakeJmapBlobService blobService = new FakeJmapBlobService
        {
            UploadResponse = new BlobUploadResponse
            {
                AccountId = "account-1",
                BlobId = "empty-avif",
                Type = "image/avif; profile=hdr",
                Size = 0
            },
            DownloadContent = new JmapDownloadContent
            {
                Content = new MemoryStream(new byte[] { 0, 1, 2, 3 }),
                MediaType = "image/avif",
                FileName = "image.avif",
                Size = 4,
                HasSize = true,
                CacheLifetimeSeconds = 7200
            }
        };
        blobService.UnreferencedBlobs.Add(new JmapUnreferencedBlob
        {
            AccountId = "account-1",
            BlobId = "empty-avif",
            Size = 0,
            UploadedAtUtc = new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero)
        });
        ServerTransport transport = new ServerTransport(
            Server(new RoundTripMailboxService(), blobService, new JmapServerOptions()));
        IBlobClient client = new JmapClient(ClientOptions(), transport);
        BlobUploadRequest uploadRequest = new BlobUploadRequest
        {
            MediaType = "image/avif; profile=hdr",
            Content = new NonSeekableBlobStream(Array.Empty<byte>()),
            HasContentLength = false
        };

        BlobUploadResponse upload = await client.UploadBlobAsync(
            uploadRequest, CancellationToken.None);
        BlobDownloadRequest downloadRequest = new BlobDownloadRequest
        {
            BlobId = "empty-avif",
            Type = "image/avif",
            Name = "image.avif"
        };
        BlobDownloadResponse download = await client.DownloadBlobAsync(
            downloadRequest, CancellationToken.None);
        using MemoryStream downloaded = new MemoryStream();
        await download.Content!.CopyToAsync(downloaded);

        blobService.DownloadContent = null;
        JmapRequestException missing = Assert.ThrowsAsync<JmapRequestException>(async () =>
            await client.DownloadBlobAsync(
                new BlobDownloadRequest
                {
                    BlobId = "missing",
                    Type = JmapMediaTypes.OctetStream,
                    Name = "missing.bin"
                },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.AccountId, Is.EqualTo("account-1"));
            Assert.That(upload.BlobId, Is.EqualTo("empty-avif"));
            Assert.That(upload.Type, Is.EqualTo("image/avif; profile=hdr"));
            Assert.That(upload.Size, Is.Zero);
            Assert.That(uploadRequest.AccountId, Is.EqualTo("account-1"));
            Assert.That(blobService.RefreshedBlobIds, Is.EqualTo(new[] { "empty-avif" }));
            Assert.That(downloaded.ToArray(), Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
            Assert.That(download.MediaType, Is.EqualTo("image/avif"));
            Assert.That(download.FileName, Is.EqualTo("image.avif"));
            Assert.That(download.CacheControl, Is.EqualTo("private, immutable, max-age=7200"));
            Assert.That(missing.HttpStatus, Is.EqualTo(404));
            Assert.That(transport.UploadRequests, Is.EqualTo(1));
            Assert.That(transport.DownloadRequests, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task MailboxClientGetsEveryFieldAndCountOnlyChangesThroughServer()
    {
        ScriptedMailboxService mailboxService = new ScriptedMailboxService();
        Mailbox inbox = CompleteMailbox("inbox", "Inbox", null, JmapMailboxRoles.Inbox, 10);
        inbox.SortOrder = 5;
        inbox.UnreadEmails = 4;
        inbox.TotalThreads = 7;
        inbox.UnreadThreads = 3;
        inbox.IsSubscribed = true;
        inbox.MyRights = Rights(true, false, true, false, true, true, false, false, true);
        inbox.UnknownProperties["vendorColor"] = Json("\"blue\"");
        inbox.MyRights.UnknownProperties["vendorAcl"] = Json("7");
        Mailbox project = CompleteMailbox("project", "Project", "inbox", null, 0);
        mailboxService.GetResponse.List.AddRange(new[] { inbox, project });
        mailboxService.ChangesResponse.HasMoreChanges = true;
        mailboxService.ChangesResponse.Created.Add("new-box");
        mailboxService.ChangesResponse.Updated.Add("project");
        mailboxService.ChangesResponse.Destroyed.Add("old-box");
        mailboxService.ChangesResponse.UpdatedProperties = new List<string>
        {
            JmapProperties.TotalEmails,
            JmapProperties.UnreadEmails,
            JmapProperties.TotalThreads,
            JmapProperties.UnreadThreads
        };
        IMailboxClient client = MailboxClient(mailboxService);

        MailboxGetResponse get = await client.GetMailboxesAsync(
            new MailboxGetRequest
            {
                Ids = new List<string> { "project", "missing", "inbox", "project" }
            },
            CancellationToken.None);
        MailboxGetResponse projected = await client.GetMailboxesAsync(
            new MailboxGetRequest
            {
                Ids = new List<string> { "inbox" },
                Properties = new List<string>
                {
                    JmapProperties.Name,
                    JmapProperties.IsSubscribed
                }
            },
            CancellationToken.None);
        MailboxChangesResponse changes = await client.GetMailboxChangesAsync(
            new MailboxChangesRequest
            {
                SinceState = "mailbox-0",
                HasMaxChanges = true,
                MaxChanges = 3
            },
            CancellationToken.None);
        Mailbox returned = get.List[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(get.AccountId, Is.EqualTo("account-1"));
            Assert.That(get.State, Is.EqualTo("mailbox-1"));
            Assert.That(get.List.Select(item => item.Id), Is.EqualTo(new[] { "project", "inbox" }));
            Assert.That(get.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(returned.Name, Is.EqualTo("Inbox"));
            Assert.That(returned.ParentId, Is.Null);
            Assert.That(returned.Role, Is.EqualTo(JmapMailboxRoles.Inbox));
            Assert.That(returned.SortOrder, Is.EqualTo(5));
            Assert.That(returned.TotalEmails, Is.EqualTo(10));
            Assert.That(returned.UnreadEmails, Is.EqualTo(4));
            Assert.That(returned.TotalThreads, Is.EqualTo(7));
            Assert.That(returned.UnreadThreads, Is.EqualTo(3));
            Assert.That(returned.IsSubscribed, Is.True);
            Assert.That(returned.HasIsSubscribed, Is.True);
            Assert.That(returned.MyRights!.MayReadItems, Is.True);
            Assert.That(returned.MyRights.MayAddItems, Is.False);
            Assert.That(returned.MyRights.MayRemoveItems, Is.True);
            Assert.That(returned.MyRights.MaySetSeen, Is.False);
            Assert.That(returned.MyRights.MaySetKeywords, Is.True);
            Assert.That(returned.MyRights.MayCreateChild, Is.True);
            Assert.That(returned.MyRights.MayRename, Is.False);
            Assert.That(returned.MyRights.MayDelete, Is.False);
            Assert.That(returned.MyRights.MaySubmit, Is.True);
            Assert.That(returned.UnknownProperties["vendorColor"].GetString(), Is.EqualTo("blue"));
            Assert.That(returned.MyRights.UnknownProperties["vendorAcl"].GetInt32(), Is.EqualTo(7));
            Assert.That(projected.List[0].Id, Is.EqualTo("inbox"));
            Assert.That(projected.List[0].Name, Is.EqualTo("Inbox"));
            Assert.That(projected.List[0].Role, Is.Null);
            Assert.That(projected.List[0].TotalEmails, Is.Zero);
            Assert.That(projected.List[0].MyRights, Is.Null);
            Assert.That(projected.List[0].IsSubscribed, Is.True);
            Assert.That(changes.OldState, Is.EqualTo("mailbox-0"));
            Assert.That(changes.NewState, Is.EqualTo("mailbox-1"));
            Assert.That(changes.HasMoreChanges, Is.True);
            Assert.That(changes.Created, Is.EqualTo(new[] { "new-box" }));
            Assert.That(changes.Updated, Is.EqualTo(new[] { "project" }));
            Assert.That(changes.Destroyed, Is.EqualTo(new[] { "old-box" }));
            Assert.That(changes.UpdatedProperties, Is.EqualTo(new[]
            {
                JmapProperties.TotalEmails,
                JmapProperties.UnreadEmails,
                JmapProperties.TotalThreads,
                JmapProperties.UnreadThreads
            }));
            Assert.That(mailboxService.LastGetRequest!.Properties, Is.Null,
                "the backend receives complete records so the server can safely project them");
            Assert.That(mailboxService.LastChangesRequest!.AccountId, Is.EqualTo("account-1"));
            Assert.That(mailboxService.LastChangesRequest.MaxChanges, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task MailboxTreeQueryAndQueryChangesPreserveCompletePublicIdentity()
    {
        ScriptedMailboxService mailboxService = new ScriptedMailboxService();
        mailboxService.QueryResponse.QueryState = "query-1";
        mailboxService.QueryResponse.CanCalculateChanges = true;
        mailboxService.QueryResponse.Position = 1;
        mailboxService.QueryResponse.Ids.Add("project");
        mailboxService.QueryResponse.Ids.Add("archive");
        mailboxService.QueryResponse.HasTotal = true;
        mailboxService.QueryResponse.Total = 4;
        mailboxService.QueryResponse.HasLimit = true;
        mailboxService.QueryResponse.Limit = 2;
        mailboxService.QueryChangesResponse.OldQueryState = "query-1";
        mailboxService.QueryChangesResponse.NewQueryState = "query-2";
        mailboxService.QueryChangesResponse.HasTotal = true;
        mailboxService.QueryChangesResponse.Total = 3;
        mailboxService.QueryChangesResponse.Removed.Add("project");
        mailboxService.QueryChangesResponse.Added.Add(
            new JmapAddedItem { Id = "project", Index = 2 });
        IMailboxClient client = MailboxClient(mailboxService);
        MailboxFilter condition = new MailboxFilter
        {
            HasParentId = true,
            ParentId = "inbox",
            Name = "Pro",
            HasRole = true,
            Role = null,
            HasHasAnyRole = true,
            HasAnyRole = false,
            HasIsSubscribed = true,
            IsSubscribed = true
        };
        MailboxFilter filter = new MailboxFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.And,
            Conditions = new List<MailboxFilter> { condition }
        };
        List<JmapComparator> sort = new List<JmapComparator>
        {
            new JmapComparator
            {
                Property = JmapProperties.Name,
                HasIsAscending = true,
                IsAscending = false,
                Collation = "i;unicode-casemap"
            },
            new JmapComparator
            {
                Property = JmapProperties.SortOrder,
                HasIsAscending = true,
                IsAscending = true
            }
        };

        MailboxQueryResponse query = await client.QueryMailboxesAsync(
            new MailboxQueryRequest
            {
                Filter = filter,
                Sort = sort,
                HasPosition = true,
                Position = 1,
                Anchor = "inbox",
                HasAnchorOffset = true,
                AnchorOffset = -1,
                HasLimit = true,
                Limit = 3,
                HasCalculateTotal = true,
                CalculateTotal = true,
                HasSortAsTree = true,
                SortAsTree = true,
                HasFilterAsTree = true,
                FilterAsTree = true
            },
            CancellationToken.None);
        MailboxQueryChangesResponse changes = await client.GetMailboxQueryChangesAsync(
            new MailboxQueryChangesRequest
            {
                Filter = filter,
                Sort = sort,
                SinceQueryState = "query-1",
                HasMaxChanges = true,
                MaxChanges = 2,
                UpToId = "archive",
                HasCalculateTotal = true,
                CalculateTotal = true,
                HasSortAsTree = true,
                SortAsTree = true,
                HasFilterAsTree = true,
                FilterAsTree = true
            },
            CancellationToken.None);
        JmapQuerySnapshot continued = client.ContinueMailboxQuery(
            new JmapQuerySnapshot
            {
                AccountId = "account-1",
                QueryState = "query-1",
                Ids = new List<string> { "inbox", "project", "archive" }
            },
            changes);
        MailboxQueryRequest received = mailboxService.LastQueryRequest!;
        MailboxQueryChangesRequest receivedChanges = mailboxService.LastQueryChangesRequest!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(query.QueryState, Is.EqualTo("query-1"));
            Assert.That(query.CanCalculateChanges, Is.True);
            Assert.That(query.Position, Is.EqualTo(1));
            Assert.That(query.Ids, Is.EqualTo(new[] { "project", "archive" }));
            Assert.That(query.HasTotal, Is.True);
            Assert.That(query.Total, Is.EqualTo(4));
            Assert.That(query.HasLimit, Is.True);
            Assert.That(query.Limit, Is.EqualTo(2));
            Assert.That(received.Filter!.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(received.Filter.Conditions![0].ParentId, Is.EqualTo("inbox"));
            Assert.That(received.Filter.Conditions[0].HasRole, Is.True);
            Assert.That(received.Filter.Conditions[0].Role, Is.Null);
            Assert.That(received.Filter.Conditions[0].HasAnyRole, Is.False);
            Assert.That(received.Filter.Conditions[0].IsSubscribed, Is.True);
            Assert.That(received.Sort![0].Collation, Is.EqualTo("i;unicode-casemap"));
            Assert.That(received.Sort[0].IsAscending, Is.False);
            Assert.That(received.Sort[1].Property, Is.EqualTo(JmapProperties.SortOrder));
            Assert.That(received.Anchor, Is.EqualTo("inbox"));
            Assert.That(received.AnchorOffset, Is.EqualTo(-1));
            Assert.That(received.SortAsTree, Is.True);
            Assert.That(received.FilterAsTree, Is.True);
            Assert.That(changes.Removed, Is.EqualTo(new[] { "project" }));
            Assert.That(changes.Added[0].Id, Is.EqualTo("project"));
            Assert.That(changes.Added[0].Index, Is.EqualTo(2));
            Assert.That(receivedChanges.UpToId, Is.EqualTo("archive"));
            Assert.That(receivedChanges.MaxChanges, Is.EqualTo(2));
            Assert.That(receivedChanges.SortAsTree, Is.True);
            Assert.That(receivedChanges.FilterAsTree, Is.True);
            Assert.That(continued.QueryState, Is.EqualTo("query-2"));
            Assert.That(continued.Ids, Is.EqualTo(new[] { "inbox", "archive", "project" }));
        }
    }

    [Test]
    public async Task MailboxSetAppliesSubscriptionsRightsHierarchyAndBothDestroyModes()
    {
        ScriptedMailboxService mailboxService = new ScriptedMailboxService();
        Mailbox inbox = CompleteMailbox("inbox", "Inbox", null, JmapMailboxRoles.Inbox, 0);
        inbox.MyRights!.MayCreateChild = true;
        inbox.MyRights.MayDelete = true;
        Mailbox project = CompleteMailbox("project", "Project", "inbox", null, 0);
        project.MyRights!.MayRename = true;
        Mailbox filled = CompleteMailbox("filled", "Filled", null, null, 2);
        filled.MyRights!.MayDelete = true;
        Mailbox empty = CompleteMailbox("empty", "Empty", null, null, 0);
        empty.MyRights!.MayDelete = true;
        mailboxService.GetResponse.List.AddRange(new[] { inbox, project, filled, empty });
        Mailbox createdMailbox = CompleteMailbox("created-id", "New Child", "inbox", null, 0);
        createdMailbox.SortOrder = 7;
        createdMailbox.IsSubscribed = true;
        mailboxService.SetResponse.Created = new Dictionary<string, Mailbox>
        {
            ["child"] = createdMailbox,
            ["unsubscribed"] = CompleteMailbox(
                "unsubscribed-id", "Muted Child", "inbox", null, 0)
        };
        mailboxService.SetResponse.Created["unsubscribed"].HasIsSubscribed = true;
        mailboxService.SetResponse.Updated = new Dictionary<string, JsonElement>
        {
            ["project"] = Json("null")
        };
        mailboxService.SetResponse.Destroyed = new List<string> { "filled", "empty" };
        IMailboxClient client = MailboxClient(mailboxService);
        JmapPatchObject update = new JmapPatchObject();
        update.Values[JmapProperties.Name] = Json("\"Renamed Project\"");
        update.Values[JmapProperties.IsSubscribed] = Json("false");

        MailboxSetResponse response = await client.SetMailboxesAsync(
            new MailboxSetRequest
            {
                IfInState = "mailbox-1",
                Create = new Dictionary<string, Mailbox>
                {
                    ["child"] = new Mailbox
                    {
                        Name = "New Child",
                        ParentId = "inbox",
                        SortOrder = 7
                    },
                    ["unsubscribed"] = new Mailbox
                    {
                        Name = "Muted Child",
                        ParentId = "inbox",
                        HasIsSubscribed = true,
                        IsSubscribed = false
                    }
                },
                Update = new Dictionary<string, JmapPatchObject>
                {
                    ["project"] = update
                },
                Destroy = new List<string> { "filled", "empty" },
                HasOnDestroyRemoveEmails = true,
                OnDestroyRemoveEmails = true
            },
            CancellationToken.None);
        MailboxSetRequest received = mailboxService.LastSetRequest!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.AccountId, Is.EqualTo("account-1"));
            Assert.That(response.OldState, Is.EqualTo("mailbox-1"));
            Assert.That(response.NewState, Is.EqualTo("mailbox-2"));
            Assert.That(response.Created!["child"].Id, Is.EqualTo("created-id"));
            Assert.That(response.Created["child"].IsSubscribed, Is.True);
            Assert.That(response.Created["unsubscribed"].IsSubscribed, Is.False);
            Assert.That(response.Updated!.Keys, Is.EqualTo(new[] { "project" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "filled", "empty" }));
            Assert.That(received.Create!["child"].IsSubscribed, Is.True,
                "new Mailboxes default to subscribed when the field is omitted");
            Assert.That(received.Create["unsubscribed"].IsSubscribed, Is.False,
                "an explicit false subscription value must be preserved");
            Assert.That(received.Create["unsubscribed"].HasIsSubscribed, Is.True);
            Assert.That(received.Create["child"].ParentId, Is.EqualTo("inbox"));
            Assert.That(received.Create["child"].SortOrder, Is.EqualTo(7));
            Assert.That(received.Update!["project"].Values[JmapProperties.Name].GetString(),
                Is.EqualTo("Renamed Project"));
            Assert.That(received.Update["project"].Values[JmapProperties.IsSubscribed].GetBoolean(),
                Is.False);
            Assert.That(received.OnDestroyRemoveEmails, Is.True);
            Assert.That(received.HasOnDestroyRemoveEmails, Is.True);
        }
    }

    [Test]
    public async Task MailboxSetReturnsRightsCycleDepthAndMailboxSpecificErrors()
    {
        ScriptedMailboxService mailboxService = new ScriptedMailboxService();
        Mailbox inbox = CompleteMailbox("inbox", "Inbox", null, JmapMailboxRoles.Inbox, 0);
        inbox.MyRights!.MayCreateChild = true;
        inbox.MyRights.MayDelete = true;
        Mailbox project = CompleteMailbox("project", "Project", "inbox", null, 0);
        project.MyRights!.MayRename = true;
        Mailbox grandchild = CompleteMailbox("grandchild", "Grandchild", "project", null, 0);
        grandchild.MyRights!.MayCreateChild = true;
        Mailbox locked = CompleteMailbox("locked", "Locked", null, null, 0);
        Mailbox filled = CompleteMailbox("filled", "Filled", null, null, 2);
        filled.MyRights!.MayDelete = true;
        mailboxService.GetResponse.List.AddRange(
            new[] { inbox, project, grandchild, locked, filled });
        IMailboxClient client = MailboxClient(mailboxService);
        JmapPatchObject cycle = new JmapPatchObject();
        cycle.Values[JmapProperties.ParentId] = Json("\"grandchild\"");
        JmapPatchObject deniedRename = new JmapPatchObject();
        deniedRename.Values[JmapProperties.Name] = Json("\"Still Locked\"");

        MailboxSetResponse response = await client.SetMailboxesAsync(
            new MailboxSetRequest
            {
                Create = new Dictionary<string, Mailbox>
                {
                    ["nestedInbox"] = new Mailbox
                    {
                        Name = "Nested Inbox",
                        ParentId = "inbox",
                        Role = JmapMailboxRoles.Inbox
                    },
                    ["deniedChild"] = new Mailbox
                    {
                        Name = "Denied Child",
                        ParentId = "locked"
                    },
                    ["tooDeep"] = new Mailbox
                    {
                        Name = "Too Deep",
                        ParentId = "grandchild"
                    }
                },
                Update = new Dictionary<string, JmapPatchObject>
                {
                    ["project"] = cycle,
                    ["locked"] = deniedRename
                },
                Destroy = new List<string> { "inbox", "filled" }
            },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["nestedInbox"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["nestedInbox"].Properties,
                Does.Contain(JmapProperties.ParentId));
            Assert.That(response.NotCreated["deniedChild"].Kind,
                Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.NotCreated["tooDeep"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated!["project"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["project"].Properties,
                Does.Contain(JmapProperties.ParentId));
            Assert.That(response.NotUpdated["locked"].Kind,
                Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(response.NotDestroyed!["inbox"].Kind,
                Is.EqualTo(JmapSetErrorKind.MailboxHasChild));
            Assert.That(response.NotDestroyed["filled"].Kind,
                Is.EqualTo(JmapSetErrorKind.MailboxHasEmail));
            Assert.That(response.OldState, Is.EqualTo("mailbox-1"));
            Assert.That(response.NewState, Is.EqualTo("mailbox-1"));
            Assert.That(mailboxService.SetCalls, Is.Zero);
        }
    }

    [Test]
    public async Task ThreadGetPreservesMultiEmailOrderRequestedOrderProjectionAndNotFound()
    {
        ScriptedThreadService threadService = new ScriptedThreadService();
        JmapThread first = CompleteThread("thread-1", "email-1", "email-2", "email-3");
        first.UnknownProperties["vendorColor"] = Json("\"blue\"");
        JmapThread second = CompleteThread("thread-2", "email-4", "email-5");
        threadService.GetResponse.List.AddRange(new[] { second, first });
        IThreadClient client = ThreadClient(threadService);

        ThreadGetResponse response = await client.GetThreadsAsync(
            new ThreadGetRequest
            {
                Ids = new List<string>
                {
                    "thread-1", "missing-thread", "thread-2", "thread-1"
                }
            },
            CancellationToken.None);
        ThreadGetResponse projected = await client.GetThreadsAsync(
            new ThreadGetRequest
            {
                Ids = new List<string> { "thread-1" },
                Properties = new List<string> { JmapProperties.Id }
            },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.AccountId, Is.EqualTo("account-1"));
            Assert.That(response.State, Is.EqualTo("thread-2"));
            Assert.That(response.List.Select(item => item.Id),
                Is.EqualTo(new[] { "thread-1", "thread-2" }));
            Assert.That(response.List[0].EmailIds,
                Is.EqualTo(new[] { "email-1", "email-2", "email-3" }));
            Assert.That(response.List[1].EmailIds,
                Is.EqualTo(new[] { "email-4", "email-5" }));
            Assert.That(response.List[0].UnknownProperties["vendorColor"].GetString(),
                Is.EqualTo("blue"));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "missing-thread" }));
            Assert.That(projected.List[0].Id, Is.EqualTo("thread-1"));
            Assert.That(projected.List[0].EmailIds, Is.Empty);
            Assert.That(threadService.GetRequests, Has.Count.EqualTo(2));
            Assert.That(threadService.GetRequests.All(request => request.Properties == null),
                Is.True, "the server must request complete records before projecting them");
        }
    }

    [Test]
    public async Task ThreadChangesAggregateMembershipAndOrderMutationsAcrossPages()
    {
        ScriptedThreadService threadService = new ScriptedThreadService();
        ThreadChangesResponse firstPage = new ThreadChangesResponse
        {
            AccountId = "account-1",
            OldState = "thread-0",
            NewState = "thread-1",
            HasMoreChanges = true
        };
        firstPage.Created.AddRange(new[] { "created-thread", "transient-thread" });
        firstPage.Updated.AddRange(
            new[] { "membership-mutated-thread", "reordered-thread" });
        ThreadChangesResponse secondPage = new ThreadChangesResponse
        {
            AccountId = "account-1",
            OldState = "thread-1",
            NewState = "thread-2"
        };
        secondPage.Updated.AddRange(
            new[] { "created-thread", "membership-mutated-thread", "reordered-thread" });
        secondPage.Destroyed.AddRange(new[] { "transient-thread", "removed-thread" });
        threadService.ChangePages["thread-0"] = firstPage;
        threadService.ChangePages["thread-1"] = secondPage;
        IThreadClient client = ThreadClient(threadService);

        ThreadChangesResponse response = await client.GetAllThreadChangesAsync(
            new ThreadChangesRequest
            {
                SinceState = "thread-0",
                HasMaxChanges = true,
                MaxChanges = 5
            },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.AccountId, Is.EqualTo("account-1"));
            Assert.That(response.OldState, Is.EqualTo("thread-0"));
            Assert.That(response.NewState, Is.EqualTo("thread-2"));
            Assert.That(response.HasMoreChanges, Is.False);
            Assert.That(response.Created, Is.EqualTo(new[] { "created-thread" }));
            Assert.That(response.Updated,
                Is.EqualTo(new[] { "membership-mutated-thread", "reordered-thread" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "removed-thread" }));
            Assert.That(threadService.ChangesRequests.Select(request => request.SinceState),
                Is.EqualTo(new[] { "thread-0", "thread-1" }));
            Assert.That(threadService.ChangesRequests.All(request =>
                request.HasMaxChanges && request.MaxChanges == 5), Is.True);
        }
    }

    [Test]
    public void ThreadChangesMapsStaleStateErrorsThroughPublicClient()
    {
        ScriptedThreadService threadService = new ScriptedThreadService();
        ServerTransport transport = new ServerTransport(ThreadServer(threadService));
        JmapResponse staleState = new JmapResponse { SessionState = "session-state" };
        staleState.MethodResponses.Add(new JmapInvocation
        {
            Name = "error",
            CallId = "c0",
            Arguments = Json(
                "{\"type\":\"cannotCalculateChanges\",\"description\":\"state too old\"}")
        });
        transport.ApiResponses.Enqueue(staleState);
        IThreadClient client = new JmapClient(ClientOptions(), transport);

        JmapMethodException error = Assert.ThrowsAsync<JmapMethodException>(async () =>
            await client.GetThreadChangesAsync(
                new ThreadChangesRequest { SinceState = "expired-state" },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.MethodName, Is.EqualTo(JmapMethods.ThreadChanges));
            Assert.That(error.ErrorToken, Is.EqualTo(JmapErrorCodes.CannotCalculateChanges));
            Assert.That(error.Error!.Kind,
                Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
            Assert.That(error.Error.Description, Is.EqualTo("state too old"));
            Assert.That(threadService.ChangesRequests, Is.Empty);
        }
    }

    private static IJmapServer Server(
        IMailboxService mailboxService,
        FakeJmapBlobService blobService,
        JmapServerOptions options)
    {
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(Session()),
            MailboxService = mailboxService,
            BlobService = blobService
        };
        return new JmapServer(options, services);
    }

    private static IMailboxClient MailboxClient(IMailboxService mailboxService)
    {
        IJmapServer server = Server(
            mailboxService, new FakeJmapBlobService(), new JmapServerOptions());
        return new JmapClient(ClientOptions(), new ServerTransport(server));
    }

    private static IJmapServer ThreadServer(IThreadService threadService)
    {
        JmapServerServices services = new JmapServerServices
        {
            SessionService = new FakeJmapSessionService(Session()),
            ThreadService = threadService
        };
        return new JmapServer(new JmapServerOptions(), services);
    }

    private static IThreadClient ThreadClient(IThreadService threadService)
    {
        return new JmapClient(ClientOptions(), new ServerTransport(ThreadServer(threadService)));
    }

    private static JmapThread CompleteThread(string id, params string[] emailIds)
    {
        JmapThread thread = new JmapThread { Id = id };
        thread.EmailIds.AddRange(emailIds);
        return thread;
    }

    private static Mailbox CompleteMailbox(
        string id,
        string name,
        string? parentId,
        string? role,
        long totalEmails)
    {
        return new Mailbox
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            Role = role,
            TotalEmails = totalEmails,
            UnreadEmails = totalEmails,
            TotalThreads = totalEmails,
            UnreadThreads = totalEmails,
            MyRights = new MailboxRights()
        };
    }

    private static MailboxRights Rights(
        bool read,
        bool add,
        bool remove,
        bool seen,
        bool keywords,
        bool child,
        bool rename,
        bool delete,
        bool submit)
    {
        return new MailboxRights
        {
            MayReadItems = read,
            MayAddItems = add,
            MayRemoveItems = remove,
            MaySetSeen = seen,
            MaySetKeywords = keywords,
            MayCreateChild = child,
            MayRename = rename,
            MayDelete = delete,
            MaySubmit = submit
        };
    }

    private static JmapClientOptions ClientOptions()
    {
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = "https://jmap.example.test/session";
        return options;
    }

    private static JmapSession Session()
    {
        JmapSession session = PublicApiTestHelper.CoreSession("session-state");
        session.Capabilities[JmapCapabilities.Core].Core!.CollationAlgorithms.Add(
            "i;unicode-casemap");
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability();
        JmapCapability mail = new JmapCapability
        {
            Mail = new JmapMailCapabilities
            {
                HasMaxMailboxDepth = true,
                MaxMailboxDepth = 3,
                MaxSizeMailboxName = 255,
                MaxSizeAttachmentsPerEmail = 1000000,
                MayCreateTopLevelMailbox = true
            }
        };
        session.Accounts["account-1"].AccountCapabilities[JmapCapabilities.Mail] = mail;
        JmapAccount source = new JmapAccount
        {
            Name = "Source"
        };
        source.AccountCapabilities[JmapCapabilities.Mail] = mail;
        session.Accounts["source"] = source;
        session.PrimaryAccounts[JmapCapabilities.Mail] = "account-1";
        return session;
    }

    private static JmapSession ScenarioSession()
    {
        JmapSession session = Session();
        session.State = "session-before";
        session.Capabilities[JmapCapabilities.Submission] = new JmapCapability();
        session.Accounts["account-1"].AccountCapabilities[JmapCapabilities.Submission] =
            new JmapCapability { Submission = new JmapSubmissionCapabilities() };
        session.PrimaryAccounts[JmapCapabilities.Submission] = "account-1";
        return session;
    }

    private static JmapInvocation Invocation(string name, string callId, string arguments)
    {
        return new JmapInvocation
        {
            Name = name,
            CallId = callId,
            Arguments = Json(arguments)
        };
    }

    private static JmapVendorMethodRegistration VendorRegistration(
        string methodName, IJmapVendorMethodHandler handler)
    {
        JmapVendorMethodRegistration registration = new JmapVendorMethodRegistration
        {
            MethodName = methodName,
            Handler = handler
        };
        registration.RequiredCapabilities.Add(VendorCapability);
        return registration;
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class ServerTransport : IJmapTransport
    {
        public IJmapServer Server;
        public IJmapCodec Codec = new JmapJsonCodec();
        public List<JmapRequest> ApiRequests = new List<JmapRequest>();
        public Queue<JmapResponse> ApiResponses = new Queue<JmapResponse>();
        public int UploadRequests;
        public int DownloadRequests;

        public ServerTransport(IJmapServer server)
        {
            Server = server;
        }

        public async Task<JmapSession> GetTransportSessionAsync(
            string sessionUrl,
            CancellationToken cancellationToken)
        {
            JmapEndpointRequest request = new JmapEndpointRequest
            {
                Method = "GET",
                Context = PublicApiTestHelper.AuthenticatedContext()
            };
            JmapEndpointResponse response = await Server.ProcessSessionEndpointAsync(
                request, cancellationToken);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            return Codec.DecodeSessionMemory(response.BodyBytes!);
        }

        public async Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            ApiRequests.Add(request);
            if (ApiResponses.Count != 0)
            {
                return ApiResponses.Dequeue();
            }

            byte[] body = Codec.EncodeRequestBytes(request);
            JmapEndpointRequest endpoint = new JmapEndpointRequest
            {
                Method = "POST",
                ContentType = JmapMediaTypes.Json,
                Body = new MemoryStream(body, false),
                ContentLength = body.Length,
                HasContentLength = true,
                Context = PublicApiTestHelper.AuthenticatedContext()
            };
            JmapEndpointResponse response = await Server.ProcessApiEndpointAsync(
                endpoint, cancellationToken);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            return Codec.DecodeResponseMemory(response.BodyBytes!);
        }

        public async Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            UploadRequests = UploadRequests + 1;
            JmapEndpointRequest endpoint = new JmapEndpointRequest
            {
                Method = "POST",
                ContentType = request.MediaType,
                Body = request.Content,
                ContentLength = request.ContentLength,
                HasContentLength = request.HasContentLength,
                Context = PublicApiTestHelper.AuthenticatedContext()
            };
            endpoint.RouteValues[JmapProperties.AccountId] = request.AccountId!;
            JmapEndpointResponse response = await Server.ProcessUploadEndpointAsync(
                endpoint, cancellationToken);
            RequireSuccess(response);
            using JsonDocument document = JsonDocument.Parse(response.BodyBytes!);
            JsonElement value = document.RootElement;
            return new BlobUploadResponse
            {
                AccountId = value.GetProperty(JmapProperties.AccountId).GetString(),
                BlobId = value.GetProperty(JmapProperties.BlobId).GetString(),
                Type = value.GetProperty(JmapProperties.Type).GetString(),
                Size = value.GetProperty(JmapProperties.Size).GetInt64()
            };
        }

        public async Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            DownloadRequests = DownloadRequests + 1;
            JmapEndpointRequest endpoint = new JmapEndpointRequest
            {
                Method = "GET",
                Context = PublicApiTestHelper.AuthenticatedContext()
            };
            endpoint.RouteValues[JmapProperties.AccountId] = request.AccountId!;
            endpoint.RouteValues[JmapProperties.BlobId] = request.BlobId!;
            endpoint.QueryParameters[JmapProperties.Type] = request.Type!;
            endpoint.QueryParameters[JmapProperties.Name] = request.Name!;
            JmapEndpointResponse response = await Server.ProcessDownloadEndpointAsync(
                endpoint, cancellationToken);
            RequireSuccess(response);
            return new BlobDownloadResponse
            {
                Content = response.BodyStream,
                MediaType = response.ContentType,
                FileName = request.Name,
                ContentLength = response.ContentLength,
                HasContentLength = response.HasContentLength,
                CacheControl = response.Headers.ContainsKey("Cache-Control") ?
                    response.Headers["Cache-Control"] : null
            };
        }

        public Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private void RequireSuccess(JmapEndpointResponse response)
        {
            if (response.StatusCode >= 200 && response.StatusCode <= 299)
            {
                return;
            }

            byte[] body = response.BodyBytes == null ? Array.Empty<byte>() : response.BodyBytes;
            JmapProblem problem = Codec.DecodeProblemMemory(body);
            using JsonDocument document = JsonDocument.Parse(body);
            throw new JmapRequestException(
                problem.Detail == null ? "The endpoint request failed." : problem.Detail,
                response.StatusCode,
                problem.Type,
                problem,
                document.RootElement.Clone(),
                body,
                response.StatusCode == 408 || response.StatusCode == 429 ||
                    response.StatusCode >= 500);
        }
    }

    private sealed class NonSeekableBlobStream : MemoryStream
    {
        public NonSeekableBlobStream(byte[] bytes) : base(bytes)
        {
        }

        public override bool CanSeek => false;
    }

    private sealed class ScriptedMailboxService : IMailboxService
    {
        public MailboxGetResponse GetResponse = new MailboxGetResponse
        {
            AccountId = "account-1",
            State = "mailbox-1"
        };
        public MailboxChangesResponse ChangesResponse = new MailboxChangesResponse
        {
            AccountId = "account-1",
            OldState = "mailbox-0",
            NewState = "mailbox-1"
        };
        public MailboxQueryResponse QueryResponse = new MailboxQueryResponse
        {
            AccountId = "account-1",
            QueryState = "query-0"
        };
        public MailboxQueryChangesResponse QueryChangesResponse =
            new MailboxQueryChangesResponse
            {
                AccountId = "account-1",
                OldQueryState = "query-0",
                NewQueryState = "query-1"
            };
        public MailboxSetResponse SetResponse = new MailboxSetResponse
        {
            AccountId = "account-1",
            OldState = "mailbox-1",
            NewState = "mailbox-2"
        };
        public MailboxGetRequest? LastGetRequest;
        public MailboxChangesRequest? LastChangesRequest;
        public MailboxQueryRequest? LastQueryRequest;
        public MailboxQueryChangesRequest? LastQueryChangesRequest;
        public MailboxSetRequest? LastSetRequest;
        public int SetCalls;

        public Task<MailboxGetResponse> GetStoredMailboxesAsync(
            MailboxGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastGetRequest = request;
            if (request.Ids == null)
            {
                return Task.FromResult(GetResponse);
            }

            MailboxGetResponse response = new MailboxGetResponse
            {
                AccountId = GetResponse.AccountId,
                State = GetResponse.State
            };
            Dictionary<string, Mailbox> records = GetResponse.List.ToDictionary(
                mailbox => mailbox.Id!, StringComparer.Ordinal);
            HashSet<string> handled = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Ids)
            {
                if (handled.Add(id))
                {
                    if (records.ContainsKey(id))
                    {
                        response.List.Add(records[id]);
                    }
                    else
                    {
                        response.NotFound.Add(id);
                    }
                }
            }

            return Task.FromResult(response);
        }

        public Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(
            MailboxChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastChangesRequest = request;
            return Task.FromResult(ChangesResponse);
        }

        public Task<MailboxQueryResponse> QueryStoredMailboxesAsync(
            MailboxQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastQueryRequest = request;
            return Task.FromResult(QueryResponse);
        }

        public Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(
            MailboxQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastQueryChangesRequest = request;
            return Task.FromResult(QueryChangesResponse);
        }

        public Task<MailboxSetResponse> SetStoredMailboxesAsync(
            MailboxSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }
    }

    private sealed class ScriptedThreadService : IThreadService
    {
        public ThreadGetResponse GetResponse = new ThreadGetResponse
        {
            AccountId = "account-1",
            State = "thread-2"
        };
        public Dictionary<string, ThreadChangesResponse> ChangePages =
            new Dictionary<string, ThreadChangesResponse>(StringComparer.Ordinal);
        public List<ThreadGetRequest> GetRequests = new List<ThreadGetRequest>();
        public List<ThreadChangesRequest> ChangesRequests =
            new List<ThreadChangesRequest>();

        public Task<ThreadGetResponse> GetStoredThreadsAsync(
            ThreadGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetRequests.Add(request);
            if (request.Ids == null)
            {
                return Task.FromResult(GetResponse);
            }

            ThreadGetResponse response = new ThreadGetResponse
            {
                AccountId = GetResponse.AccountId,
                State = GetResponse.State
            };
            Dictionary<string, JmapThread> records = GetResponse.List.ToDictionary(
                thread => thread.Id!, StringComparer.Ordinal);
            HashSet<string> handled = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in request.Ids)
            {
                if (handled.Add(id))
                {
                    if (records.ContainsKey(id))
                    {
                        response.List.Add(records[id]);
                    }
                    else
                    {
                        response.NotFound.Add(id);
                    }
                }
            }

            return Task.FromResult(response);
        }

        public Task<ThreadChangesResponse> GetStoredThreadChangesAsync(
            ThreadChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ChangesRequests.Add(request);
            return Task.FromResult(ChangePages[request.SinceState!]);
        }
    }

    private sealed class RoundTripMailboxService : IMailboxService
    {
        public List<string> Calls = new List<string>();

        public Task<MailboxGetResponse> GetStoredMailboxesAsync(
            MailboxGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls.Add(JmapMethods.MailboxGet);
            MailboxGetResponse response = new MailboxGetResponse
            {
                AccountId = request.AccountId,
                State = "mailbox-1"
            };
            if (request.Ids != null)
            {
                response.NotFound.AddRange(request.Ids);
            }

            return Task.FromResult(response);
        }

        public Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(
            MailboxChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls.Add(JmapMethods.MailboxChanges);
            MailboxChangesResponse response = new MailboxChangesResponse
            {
                AccountId = request.AccountId,
                OldState = request.SinceState,
                NewState = "mailbox-1"
            };
            response.Created.Add("created");
            response.Updated.Add("updated");
            response.Destroyed.Add("destroyed");
            return Task.FromResult(response);
        }

        public Task<MailboxQueryResponse> QueryStoredMailboxesAsync(
            MailboxQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls.Add(JmapMethods.MailboxQuery);
            MailboxQueryResponse response = new MailboxQueryResponse
            {
                AccountId = request.AccountId,
                QueryState = "query-1",
                CanCalculateChanges = true,
                Position = 1,
                HasTotal = true,
                Total = 4
            };
            response.Ids.Add("mailbox-2");
            response.Ids.Add("mailbox-3");
            return Task.FromResult(response);
        }

        public Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(
            MailboxQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls.Add(JmapMethods.MailboxQueryChanges);
            MailboxQueryChangesResponse response = new MailboxQueryChangesResponse
            {
                AccountId = request.AccountId,
                OldQueryState = request.SinceQueryState,
                NewQueryState = "query-1",
                HasTotal = true,
                Total = 3
            };
            response.Removed.Add("mailbox-1");
            response.Added.Add(new JmapAddedItem { Id = "mailbox-3", Index = 1 });
            return Task.FromResult(response);
        }

        public Task<MailboxSetResponse> SetStoredMailboxesAsync(
            MailboxSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls.Add(JmapMethods.MailboxSet);
            return Task.FromResult(new MailboxSetResponse
            {
                AccountId = request.AccountId,
                OldState = "mailbox-1",
                NewState = "mailbox-2"
            });
        }
    }

    private sealed class ScenarioServices : IMailboxService, IEmailService,
        IIdentityService, IEmailSubmissionService
    {
        public JmapSession Session;
        public IJmapStatePublisher? Publisher;
        public Mailbox? StoredMailbox;
        public Email? StoredEmail;
        public string? EmailCreateMailboxId;
        public string? SubmissionEmailId;
        public JmapPatchObject? ImplicitEmailPatch;
        public string MailboxState = "mailbox-1";
        public string EmailState = "email-1";
        public string SubmissionState = "submission-1";

        public ScenarioServices(JmapSession session)
        {
            Session = session;
        }

        public Task<MailboxGetResponse> GetStoredMailboxesAsync(
            MailboxGetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            MailboxGetResponse response = new MailboxGetResponse
            {
                AccountId = request.AccountId,
                State = MailboxState
            };
            if (StoredMailbox != null &&
                (request.Ids == null || request.Ids.Contains(StoredMailbox.Id!)))
            {
                response.List.Add(StoredMailbox);
            }
            if (request.Ids != null)
            {
                response.NotFound.AddRange(request.Ids.Where(id =>
                    StoredMailbox == null || id != StoredMailbox.Id));
            }
            return Task.FromResult(response);
        }

        public async Task<MailboxSetResponse> SetStoredMailboxesAsync(
            MailboxSetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Mailbox created = request.Create!["folder"];
            StoredMailbox = new Mailbox
            {
                Id = "mailbox-projects",
                Name = created.Name,
                ParentId = created.ParentId,
                SortOrder = created.SortOrder,
                MyRights = FullRights()
            };
            string oldState = MailboxState;
            MailboxState = "mailbox-2";
            Session.State = "session-mailbox";
            await Publish(JmapDataTypes.Mailbox, MailboxState, cancellationToken);
            return new MailboxSetResponse
            {
                AccountId = request.AccountId,
                OldState = oldState,
                NewState = MailboxState,
                Created = new Dictionary<string, Mailbox>
                {
                    ["folder"] = StoredMailbox
                }
            };
        }

        public Task<MailboxChangesResponse> GetStoredMailboxChangesAsync(
            MailboxChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MailboxQueryResponse> QueryStoredMailboxesAsync(
            MailboxQueryRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MailboxQueryChangesResponse> GetStoredMailboxQueryChangesAsync(
            MailboxQueryChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailGetResponse> GetStoredEmailsAsync(
            EmailGetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            EmailGetResponse response = new EmailGetResponse
            {
                AccountId = request.AccountId,
                State = EmailState
            };
            if (StoredEmail != null && request.Ids != null && request.Ids.Contains(StoredEmail.Id!))
            {
                response.List.Add(StoredEmail);
            }
            if (request.Ids != null)
            {
                response.NotFound.AddRange(request.Ids.Where(id =>
                    StoredEmail == null || id != StoredEmail.Id));
            }
            return Task.FromResult(response);
        }

        public async Task<EmailSetResponse> SetStoredEmailsAsync(
            EmailSetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            string oldState = EmailState;
            EmailSetResponse response = new EmailSetResponse { AccountId = request.AccountId };
            if (request.Create != null)
            {
                Email incoming = request.Create["message"];
                EmailCreateMailboxId = incoming.MailboxIds!.Keys.Single();
                StoredEmail = incoming;
                StoredEmail.Id = "email-1";
                EmailState = "email-2";
                response.Created = new Dictionary<string, Email>
                {
                    ["message"] = new Email
                    {
                        Id = "email-1", BlobId = "blob-1", ThreadId = "thread-1", Size = 42
                    }
                };
            }
            else
            {
                ImplicitEmailPatch = request.Update!["email-1"];
                EmailState = "email-3";
                response.Updated = new Dictionary<string, JsonElement>
                {
                    ["email-1"] = Json("null")
                };
            }
            response.OldState = oldState;
            response.NewState = EmailState;
            Session.State = EmailState == "email-3" ? "session-after" : "session-email";
            await Publish(JmapDataTypes.Email, EmailState, cancellationToken);
            return response;
        }

        public Task<EmailChangesResponse> GetStoredEmailChangesAsync(
            EmailChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailQueryResponse> QueryStoredEmailsAsync(
            EmailQueryRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailQueryChangesResponse> GetStoredEmailQueryChangesAsync(
            EmailQueryChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailCopyResponse> CopyStoredEmailsAsync(
            EmailCopyRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailImportResponse> ImportStoredEmailsAsync(
            EmailImportRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailParseResponse> ParseStoredEmailBlobsAsync(
            EmailParseRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityGetResponse> GetStoredIdentitiesAsync(
            IdentityGetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            IdentityGetResponse response = new IdentityGetResponse
            {
                AccountId = request.AccountId,
                State = "identity-1"
            };
            response.List.Add(new Identity
            {
                Id = "identity-1",
                Email = "sender@example.test"
            });
            return Task.FromResult(response);
        }

        public Task<IdentityChangesResponse> GetStoredIdentityChangesAsync(
            IdentityChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentitySetResponse> SetStoredIdentitiesAsync(
            IdentitySetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailSubmissionGetResponse> GetStoredEmailSubmissionsAsync(
            EmailSubmissionGetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            EmailSubmissionGetResponse response = new EmailSubmissionGetResponse
            {
                AccountId = request.AccountId,
                State = SubmissionState
            };
            if (request.Ids != null && request.Ids.Contains("submission-existing"))
            {
                response.List.Add(new EmailSubmission
                {
                    Id = "submission-existing",
                    EmailId = "email-1",
                    UndoStatus = JmapSubmissionUndoStatus.Pending,
                    HasUndoStatus = true
                });
            }
            return Task.FromResult(response);
        }

        public async Task<EmailSubmissionSetResponse> SetStoredEmailSubmissionsAsync(
            EmailSubmissionSetRequest request, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SubmissionEmailId = request.Create!["send"].EmailId;
            string oldState = SubmissionState;
            SubmissionState = "submission-2";
            Session.State = "session-submission";
            await Publish(JmapDataTypes.EmailSubmission, SubmissionState, cancellationToken);
            return new EmailSubmissionSetResponse
            {
                AccountId = request.AccountId,
                OldState = oldState,
                NewState = SubmissionState,
                Created = new Dictionary<string, EmailSubmission>
                {
                    ["send"] = new EmailSubmission
                    {
                        Id = "submission-1",
                        SendAt = "2026-09-10T12:00:00Z",
                        UndoStatus = JmapSubmissionUndoStatus.Final,
                        HasUndoStatus = true
                    }
                },
                Updated = new Dictionary<string, JsonElement>
                {
                    ["submission-existing"] = Json("null")
                }
            };
        }

        public Task<EmailSubmissionChangesResponse> GetStoredEmailSubmissionChangesAsync(
            EmailSubmissionChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailSubmissionQueryResponse> QueryStoredEmailSubmissionsAsync(
            EmailSubmissionQueryRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EmailSubmissionQueryChangesResponse> GetStoredEmailSubmissionQueryChangesAsync(
            EmailSubmissionQueryChangesRequest request, JmapRequestContext context,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        private async Task Publish(
            string dataType, string state, CancellationToken cancellationToken)
        {
            JmapStatePublication publication = new JmapStatePublication
            {
                EventId = state
            };
            publication.StateChange.Changed["account-1"] = new Dictionary<string, string>
            {
                [dataType] = state
            };
            await Publisher!.PublishStateChangeAsync(publication, cancellationToken);
        }

        private static MailboxRights FullRights()
        {
            return new MailboxRights
            {
                MayReadItems = true,
                MayAddItems = true,
                MayRemoveItems = true,
                MaySetSeen = true,
                MaySetKeywords = true,
                MayCreateChild = true,
                MayRename = true,
                MayDelete = true,
                MaySubmit = true
            };
        }
    }

    private sealed class VendorInteropHandler : IJmapVendorMethodHandler
    {
        public List<string> MethodNames = new List<string>();
        public List<JsonElement> Arguments = new List<JsonElement>();

        public Task<JmapInvocation> ProcessVendorMethodAsync(
            JmapInvocation invocation, JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MethodNames.Add(invocation.Name!);
            Arguments.Add(invocation.Arguments.Clone());
            if (invocation.Name == "Widget/fail")
            {
                return Task.FromResult(new JmapInvocation
                {
                    Name = "error",
                    CallId = invocation.CallId,
                    Arguments = Json("""
                        {"type":"vendorBusy","description":"Try another region.",
                         "retryAfter":7,"region":"west"}
                        """)
                });
            }

            return Task.FromResult(new JmapInvocation
            {
                Name = invocation.Name,
                CallId = invocation.CallId,
                Arguments = Json("""
                    {"accepted":true,"vendorResult":{"code":42,"format":"vNext"}}
                    """)
            });
        }
    }
}
