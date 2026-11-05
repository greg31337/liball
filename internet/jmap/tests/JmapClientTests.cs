using System.Text.Json;

namespace jmap.Tests;

public class JmapClientTests
{
    [Test]
    public void SessionAndTransportMetadataFailuresAreReportedAsProtocolErrors()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapClient client = new JmapClient(Options(), transport);
        JmapSession session = Session("session-1", false);
        JmapRequest request = PublicApiTestHelper.CoreEchoRequest("call-1", "{}");

        session.ApiUrl = null;
        JmapProtocolException missingApi = Assert.Throws<JmapProtocolException>(
            () => client.SendBatch(session, request, CancellationToken.None))!;
        Assert.That(missingApi.Path, Is.EqualTo("apiUrl"));

        session.ApiUrl = "https://jmap.example.test/api";
        transport.ReturnInvalidApiResponse = true;
        JmapProtocolException invalidResponse = Assert.Throws<JmapProtocolException>(
            () => client.SendBatch(session, request, CancellationToken.None))!;
        Assert.That(invalidResponse.Path, Is.EqualTo("response"));
        JmapInvocation nameless = new JmapInvocation
        {
            Arguments = Json("{}"),
            CallId = "call-1"
        };
        Assert.That(() => client.ExecuteInvocation(
            session, nameless, JmapCapabilities.Core, CancellationToken.None),
            Throws.TypeOf<JmapValidationException>());

        BlobUploadRequest upload = new BlobUploadRequest
        {
            AccountId = "account-1",
            MediaType = JmapMediaTypes.OctetStream,
            Content = new MemoryStream(new byte[] { 1 }),
            ContentLength = 1,
            HasContentLength = true
        };
        BlobUploadResponse validUpload = new BlobUploadResponse
        {
            AccountId = "account-1",
            BlobId = "blob-1",
            Type = JmapMediaTypes.OctetStream,
            Size = 1
        };
        client.ValidateUploadResponse(validUpload, upload);
        BlobUploadResponse invalidUpload = new BlobUploadResponse
        {
            AccountId = "different",
            BlobId = "blob-1",
            Type = JmapMediaTypes.OctetStream,
            Size = 1
        };
        JmapProtocolException uploadError = Assert.Throws<JmapProtocolException>(
            () => client.ValidateUploadResponse(invalidUpload, upload))!;
        Assert.That(uploadError.Path, Is.EqualTo("uploadResponse"));

        BlobDownloadResponse validDownload = new BlobDownloadResponse
        {
            Content = new MemoryStream(),
            MediaType = JmapMediaTypes.OctetStream
        };
        client.ValidateDownloadResponse(validDownload);
        BlobDownloadResponse invalidDownload = new BlobDownloadResponse
        {
            Content = new MemoryStream(),
            MediaType = "bad\ud800type"
        };
        JmapProtocolException downloadError = Assert.Throws<JmapProtocolException>(
            () => client.ValidateDownloadResponse(invalidDownload))!;
        Assert.That(downloadError.Path, Is.EqualTo("downloadResponse"));

        RecordingTransport uploadTransport = new RecordingTransport();
        JmapSession noUpload = Session("session-1", false);
        noUpload.UploadUrl = null;
        JmapClient uploadClient = new JmapClient(Options(), uploadTransport);
        uploadClient.Sessions.CachedSession = noUpload;
        JmapProtocolException missingUpload = Assert.ThrowsAsync<JmapProtocolException>(
            async () => await uploadClient.UploadBlobAsync(upload, CancellationToken.None))!;
        Assert.That(missingUpload.Path, Is.EqualTo("uploadUrl"));

        RecordingTransport downloadTransport = new RecordingTransport();
        JmapSession noDownload = Session("session-1", false);
        noDownload.DownloadUrl = null;
        JmapClient downloadClient = new JmapClient(Options(), downloadTransport);
        downloadClient.Sessions.CachedSession = noDownload;
        BlobDownloadRequest download = new BlobDownloadRequest
        {
            AccountId = "account-1",
            BlobId = "blob-1",
            Type = JmapMediaTypes.OctetStream,
            Name = "file.bin"
        };
        JmapProtocolException missingDownload = Assert.ThrowsAsync<JmapProtocolException>(
            async () => await downloadClient.DownloadBlobAsync(download, CancellationToken.None))!;
        Assert.That(missingDownload.Path, Is.EqualTo("downloadUrl"));
    }

    [Test]
    public void PublicOperationsRejectMissingRequestsBeforeUsingTheTransport()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        JmapClient client = new JmapClient(Options(), transport);
        Assert.That(client.GetPrimaryAccountId(JmapCapabilities.Mail), Is.Null);
        List<Func<Task>> calls = new List<Func<Task>>
        {
            () => client.ExecuteBatchAsync(null!, CancellationToken.None),
            () => client.ExecuteRawCallAsync(null!, CancellationToken.None),
            () => client.UploadBlobAsync(null!, CancellationToken.None),
            () => client.DownloadBlobAsync(null!, CancellationToken.None),
            () => client.CopyBlobAsync(null!, CancellationToken.None),
            () => client.GetPushSubscriptionsAsync(null!, CancellationToken.None),
            () => client.SetPushSubscriptionsAsync(null!, CancellationToken.None),
            () => client.GetMailboxesAsync(null!, CancellationToken.None),
            () => client.GetMailboxChangesAsync(null!, CancellationToken.None),
            () => client.GetAllMailboxChangesAsync(null!, CancellationToken.None),
            () => client.QueryMailboxesAsync(null!, CancellationToken.None),
            () => client.GetMailboxQueryChangesAsync(null!, CancellationToken.None),
            () => client.SetMailboxesAsync(null!, CancellationToken.None),
            () => client.GetThreadsAsync(null!, CancellationToken.None),
            () => client.GetThreadChangesAsync(null!, CancellationToken.None),
            () => client.GetAllThreadChangesAsync(null!, CancellationToken.None),
            () => client.GetEmailsAsync(null!, CancellationToken.None),
            () => client.GetEmailChangesAsync(null!, CancellationToken.None),
            () => client.GetAllEmailChangesAsync(null!, CancellationToken.None),
            () => client.QueryEmailsAsync(null!, CancellationToken.None),
            () => client.GetEmailQueryChangesAsync(null!, CancellationToken.None),
            () => client.SetEmailsAsync(null!, CancellationToken.None),
            () => client.CopyEmailsAsync(null!, CancellationToken.None),
            () => client.ImportEmailsAsync(null!, CancellationToken.None),
            () => client.ParseEmailsAsync(null!, CancellationToken.None),
            () => client.GetSearchSnippetsAsync(null!, CancellationToken.None),
            () => client.GetIdentitiesAsync(null!, CancellationToken.None),
            () => client.GetIdentityChangesAsync(null!, CancellationToken.None),
            () => client.GetAllIdentityChangesAsync(null!, CancellationToken.None),
            () => client.SetIdentitiesAsync(null!, CancellationToken.None),
            () => client.GetEmailSubmissionsAsync(null!, CancellationToken.None),
            () => client.GetEmailSubmissionChangesAsync(null!, CancellationToken.None),
            () => client.GetAllEmailSubmissionChangesAsync(null!, CancellationToken.None),
            () => client.QueryEmailSubmissionsAsync(null!, CancellationToken.None),
            () => client.GetEmailSubmissionQueryChangesAsync(null!, CancellationToken.None),
            () => client.SetEmailSubmissionsAsync(null!, CancellationToken.None),
            () => client.GetVacationResponseAsync(null!, CancellationToken.None),
            () => client.SetVacationResponseAsync(null!, CancellationToken.None)
        };

        foreach (Func<Task> call in calls)
        {
            Assert.That(async () => await call(), Throws.TypeOf<JmapValidationException>());
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => client.ContinueMailboxQuery(
                new JmapQuerySnapshot(), null!), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => client.ContinueEmailQuery(
                new JmapQuerySnapshot(), null!), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => client.ContinueEmailSubmissionQuery(
                new JmapQuerySnapshot(), null!), Throws.TypeOf<JmapValidationException>());
            Assert.That(async () => await client.ExecuteEchoAsync(
                Json("null"), CancellationToken.None), Throws.TypeOf<JmapValidationException>());
            Assert.That(transport.SessionUrls, Is.EqualTo(new[]
            {
                "https://jmap.example.test/session"
            }));
            Assert.That(transport.ApiRequests, Is.Empty);
        }
    }

    [Test]
    public async Task SessionRawBatchAndEchoOperationsUseCacheAndRefreshAfterStateChange()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-2", false)));
        transport.ResponseStates.Enqueue("session-2");
        transport.ResponseStates.Enqueue("session-2");
        transport.ResponseStates.Enqueue("session-2");
        JmapClientOptions options = Options();
        JmapClient client = new JmapClient(options, transport);

        Assert.That(client.GetSession(), Is.Null);
        JmapSession discovered = await client.DiscoverSessionAsync(
            options.DiscoveryOptions, CancellationToken.None);
        JsonElement echoed = await client.ExecuteEchoAsync(Json("{\"hello\":\"world\"}"),
            CancellationToken.None);
        JmapSession? stale = client.GetSession();

        JmapRawMethodCall raw = new JmapRawMethodCall
        {
            Name = "Vendor/ping",
            Arguments = Json("{\"value\":7}")
        };
        JmapCallResult rawResult = await client.ExecuteRawCallAsync(raw, CancellationToken.None);

        JmapRequest batch = new JmapRequest();
        batch.Using.Add(JmapCapabilities.Core);
        batch.MethodCalls.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json("{\"batch\":true}"),
            CallId = "batch-1"
        });
        JmapResponse batchResponse = await client.ExecuteBatchAsync(batch, CancellationToken.None);
        JmapCallResult selected = client.FindCallResult(batchResponse, "batch-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(client.GetClientOptions(), Is.SameAs(options));
            Assert.That(discovered.State, Is.EqualTo("session-1"));
            Assert.That(stale!.State, Is.EqualTo("session-1"));
            Assert.That(client.GetSession()!.State, Is.EqualTo("session-2"));
            Assert.That(client.GetPrimaryAccountId(JmapCapabilities.Mail), Is.EqualTo("account-1"));
            Assert.That(echoed.GetProperty("hello").GetString(), Is.EqualTo("world"));
            Assert.That(rawResult.Name, Is.EqualTo("Vendor/ping"));
            Assert.That(rawResult.Arguments.GetProperty("value").GetInt32(), Is.EqualTo(7));
            Assert.That(selected.Arguments.GetProperty("batch").GetBoolean(), Is.True);
            Assert.That(transport.SessionUrls, Has.Count.EqualTo(2));
            Assert.That(transport.ApiRequests, Has.Count.EqualTo(3));
            Assert.That(transport.ApiRequests[0].Using, Is.EqualTo(new[] { JmapCapabilities.Core }));
            Assert.That(transport.ApiRequests[1].Using, Is.EqualTo(new[] { JmapCapabilities.Core }));
        }
    }

    [Test]
    public async Task UsernameDiscoveryUsesLocatedUrlAndRefreshesThatResolvedEndpoint()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-2", false)));
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.Username = "user@example.test";
        RecordingLocator locator = new RecordingLocator();
        options.ServiceLocator = locator;
        JmapClient client = new JmapClient(options, transport);

        JmapSession discovered = await client.DiscoverSessionAsync(
            options.DiscoveryOptions, CancellationToken.None);
        JmapSession refreshed = await client.RefreshSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(discovered.State, Is.EqualTo("session-1"));
            Assert.That(refreshed.State, Is.EqualTo("session-2"));
            Assert.That(locator.Usernames, Is.EqualTo(new[] { "user@example.test" }));
            Assert.That(transport.SessionUrls, Is.EqualTo(new[]
            {
                "https://located.example.test/.well-known/jmap",
                "https://located.example.test/.well-known/jmap"
            }));
        }
    }

    [Test]
    public void ConfiguredServiceLocatorReceivesCancellationBeforeSessionTransport()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.Username = "user@example.test";
        CanceledLocator locator = new CanceledLocator();
        options.ServiceLocator = locator;
        JmapClient client = new JmapClient(options, transport);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<JmapSession> discovery = client.DiscoverSessionAsync(
            options.DiscoveryOptions, cancellation.Token);

        Assert.That(async () => await discovery, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(discovery.IsCanceled, Is.True);
            Assert.That(locator.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(transport.SessionUrls, Is.Empty);
            Assert.That(client.GetSession(), Is.Null);
        }
    }

    [Test]
    public async Task SessionValidationRejectsInvalidLimitsAndAcceptsUnknownCapabilities()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapSession invalid = Session("invalid", false);
        invalid.Capabilities[JmapCapabilities.Core].Core!.MaxObjectsInGet = -1;
        transport.SessionResults.Enqueue(Task.FromResult(invalid));
        JmapClient client = new JmapClient(Options(), transport);

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await client.RefreshSessionAsync(CancellationToken.None))!;

        string vendorCapability = "https://vendor.example/capability";
        JmapSession valid = Session("valid", false);
        valid.Capabilities[JmapCapabilities.Core].Core!.MaxObjectsInGet = 37;
        JmapCapability vendorServer = new JmapCapability();
        vendorServer.UnknownProperties["maxWidgets"] = Json("19");
        valid.Capabilities[vendorCapability] = vendorServer;
        JmapCapability vendorAccount = new JmapCapability();
        vendorAccount.UnknownProperties["enabled"] = Json("true");
        valid.Accounts["account-1"].AccountCapabilities[vendorCapability] = vendorAccount;
        valid.PrimaryAccounts[vendorCapability] = "account-1";
        transport.SessionResults.Enqueue(Task.FromResult(valid));

        JmapSession discovered = await client.RefreshSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties,
                Does.Contain("capabilities/urn:ietf:params:jmap:core/maxObjectsInGet"));
            Assert.That(discovered, Is.SameAs(valid));
            Assert.That(client.GetSession(), Is.SameAs(valid));
            Assert.That(discovered.Capabilities[JmapCapabilities.Core].Core!.MaxObjectsInGet,
                Is.EqualTo(37));
            Assert.That(discovered.Capabilities[vendorCapability]
                .UnknownProperties["maxWidgets"].GetInt32(), Is.EqualTo(19));
            Assert.That(client.GetPrimaryAccountId(vendorCapability), Is.EqualTo("account-1"));
        }
    }

    [Test]
    public async Task TypedMethodsSelectAccountsRouteCapabilitiesAndMapResponses()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ResponseArguments[JmapMethods.MailboxGet] =
            "{\"accountId\":\"account-1\",\"state\":\"m1\",\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.ThreadGet] =
            "{\"accountId\":\"account-1\",\"state\":\"t1\",\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.EmailQuery] =
            "{\"accountId\":\"account-1\",\"queryState\":\"q1\",\"canCalculateChanges\":true," +
            "\"position\":0,\"ids\":[]}";
        transport.ResponseArguments[JmapMethods.SearchSnippetGet] =
            "{\"accountId\":\"account-1\",\"list\":[],\"notFound\":null}";
        transport.ResponseArguments[JmapMethods.IdentityGet] =
            "{\"accountId\":\"account-1\",\"state\":\"i1\",\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.EmailSubmissionQuery] =
            "{\"accountId\":\"account-1\",\"queryState\":\"sq1\"," +
            "\"canCalculateChanges\":true,\"position\":0,\"ids\":[]}";
        transport.ResponseArguments[JmapMethods.VacationResponseGet] =
            "{\"accountId\":\"account-1\",\"state\":\"v1\"," +
            "\"list\":[{\"id\":\"singleton\"}],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.PushSubscriptionGet] =
            "{\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.BlobCopy] =
            "{\"fromAccountId\":\"source\",\"accountId\":\"account-1\"," +
            "\"copied\":{\"blob-1\":\"blob-2\"}}";
        JmapClient client = new JmapClient(Options(), transport);

        MailboxGetRequest mailboxRequest = new MailboxGetRequest();
        MailboxGetResponse mailboxes = await client.GetMailboxesAsync(mailboxRequest, CancellationToken.None);
        ThreadGetRequest threadRequest = new ThreadGetRequest();
        ThreadGetResponse threads = await client.GetThreadsAsync(threadRequest, CancellationToken.None);
        EmailQueryRequest emailRequest = new EmailQueryRequest();
        EmailQueryResponse emails = await client.QueryEmailsAsync(emailRequest, CancellationToken.None);
        SearchSnippetGetRequest snippetRequest = new SearchSnippetGetRequest();
        SearchSnippetGetResponse snippets = await client.GetSearchSnippetsAsync(
            snippetRequest, CancellationToken.None);
        IdentityGetRequest identityRequest = new IdentityGetRequest();
        IdentityGetResponse identities = await client.GetIdentitiesAsync(
            identityRequest, CancellationToken.None);
        EmailSubmissionQueryRequest submissionRequest = new EmailSubmissionQueryRequest();
        EmailSubmissionQueryResponse submissions = await client.QueryEmailSubmissionsAsync(
            submissionRequest, CancellationToken.None);
        VacationResponseGetRequest vacationRequest = new VacationResponseGetRequest
        {
            Properties = new List<string> { JmapProperties.Id }
        };
        VacationResponseGetResponse vacation = await client.GetVacationResponseAsync(
            vacationRequest, CancellationToken.None);
        PushSubscriptionGetResponse pushes = await client.GetPushSubscriptionsAsync(
            new PushSubscriptionGetRequest(), CancellationToken.None);
        BlobCopyRequest blobRequest = new BlobCopyRequest
        {
            FromAccountId = "source"
        };
        blobRequest.BlobIds.Add("blob-1");
        BlobCopyResponse blobs = await client.CopyBlobAsync(blobRequest, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mailboxes.State, Is.EqualTo("m1"));
            Assert.That(threads.State, Is.EqualTo("t1"));
            Assert.That(emails.QueryState, Is.EqualTo("q1"));
            Assert.That(snippets.List, Is.Empty);
            Assert.That(identities.State, Is.EqualTo("i1"));
            Assert.That(submissions.QueryState, Is.EqualTo("sq1"));
            Assert.That(vacation.List[0].Id, Is.EqualTo("singleton"));
            Assert.That(pushes.List, Is.Empty);
            Assert.That(blobs.Copied!["blob-1"], Is.EqualTo("blob-2"));
            Assert.That(mailboxRequest.AccountId, Is.EqualTo("account-1"));
            Assert.That(identityRequest.AccountId, Is.EqualTo("account-1"));
            Assert.That(vacationRequest.AccountId, Is.EqualTo("account-1"));
            Assert.That(blobRequest.AccountId, Is.EqualTo("account-1"));
            Assert.That(transport.ApiRequests, Has.Count.EqualTo(9));
            Assert.That(transport.ApiRequests[0].Using,
                Is.EqualTo(new[] { JmapCapabilities.Core, JmapCapabilities.Mail }));
            Assert.That(transport.ApiRequests[4].Using,
                Is.EqualTo(new[] { JmapCapabilities.Core, JmapCapabilities.Submission }));
            Assert.That(transport.ApiRequests[6].Using,
                Is.EqualTo(new[] { JmapCapabilities.Core, JmapCapabilities.VacationResponse }));
            Assert.That(transport.ApiRequests[7].Using,
                Is.EqualTo(new[] { JmapCapabilities.Core }));
            Assert.That(transport.ApiRequests[8].Using,
                Is.EqualTo(new[] { JmapCapabilities.Core }));
        }
    }

    [Test]
    public async Task RemainingTypedOperationsMapSuccessfulEmptyResponses()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ResponseArguments[JmapMethods.EmailGet] =
            "{\"accountId\":\"account-1\",\"state\":\"e1\",\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.EmailQueryChanges] =
            "{\"accountId\":\"account-1\",\"oldQueryState\":\"eq0\",\"newQueryState\":\"eq1\"," +
            "\"removed\":[],\"added\":[]}";
        transport.ResponseArguments[JmapMethods.EmailCopy] =
            "{\"fromAccountId\":\"source\",\"accountId\":\"account-1\"," +
            "\"oldState\":\"e0\",\"newState\":\"e1\"}";
        transport.ResponseArguments[JmapMethods.EmailImport] =
            "{\"accountId\":\"account-1\",\"oldState\":\"e0\",\"newState\":\"e1\"}";
        transport.ResponseArguments[JmapMethods.EmailParse] =
            "{\"accountId\":\"account-1\",\"parsed\":{},\"notParsable\":[]}";
        transport.ResponseArguments[JmapMethods.IdentitySet] =
            "{\"accountId\":\"account-1\",\"oldState\":\"i0\",\"newState\":\"i1\"}";
        transport.ResponseArguments[JmapMethods.EmailSubmissionGet] =
            "{\"accountId\":\"account-1\",\"state\":\"s1\",\"list\":[],\"notFound\":[]}";
        transport.ResponseArguments[JmapMethods.EmailSubmissionQueryChanges] =
            "{\"accountId\":\"account-1\",\"oldQueryState\":\"sq0\",\"newQueryState\":\"sq1\"," +
            "\"removed\":[],\"added\":[]}";
        transport.ResponseArguments[JmapMethods.EmailSubmissionSet] =
            "{\"accountId\":\"account-1\",\"oldState\":\"s0\",\"newState\":\"s1\"}";
        transport.ResponseArguments[JmapMethods.VacationResponseSet] =
            "{\"accountId\":\"account-1\",\"oldState\":\"v0\",\"newState\":\"v1\"}";
        JmapClient client = new JmapClient(Options(), transport);

        EmailGetResponse emails = await client.GetEmailsAsync(
            new EmailGetRequest(), CancellationToken.None);
        EmailQueryChangesResponse emailChanges = await client.GetEmailQueryChangesAsync(
            new EmailQueryChangesRequest { SinceQueryState = "eq0" }, CancellationToken.None);
        EmailCopyResponse copies = await client.CopyEmailsAsync(
            new EmailCopyRequest { FromAccountId = "source" }, CancellationToken.None);
        EmailImportResponse imports = await client.ImportEmailsAsync(
            new EmailImportRequest(), CancellationToken.None);
        EmailParseResponse parsed = await client.ParseEmailsAsync(
            new EmailParseRequest(), CancellationToken.None);
        IdentitySetResponse identities = await client.SetIdentitiesAsync(
            new IdentitySetRequest(), CancellationToken.None);
        EmailSubmissionGetResponse submissions = await client.GetEmailSubmissionsAsync(
            new EmailSubmissionGetRequest(), CancellationToken.None);
        EmailSubmissionQueryChangesResponse submissionChanges =
            await client.GetEmailSubmissionQueryChangesAsync(
                new EmailSubmissionQueryChangesRequest { SinceQueryState = "sq0" },
                CancellationToken.None);
        EmailSubmissionSetResponse submissionSet = await client.SetEmailSubmissionsAsync(
            new EmailSubmissionSetRequest(), CancellationToken.None);
        VacationResponseSetResponse vacation = await client.SetVacationResponseAsync(
            new VacationResponseSetRequest(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(emails.State, Is.EqualTo("e1"));
            Assert.That(emailChanges.NewQueryState, Is.EqualTo("eq1"));
            Assert.That(copies.NewState, Is.EqualTo("e1"));
            Assert.That(imports.NewState, Is.EqualTo("e1"));
            Assert.That(parsed.Parsed, Is.Empty);
            Assert.That(identities.NewState, Is.EqualTo("i1"));
            Assert.That(submissions.State, Is.EqualTo("s1"));
            Assert.That(submissionChanges.NewQueryState, Is.EqualTo("sq1"));
            Assert.That(submissionSet.NewState, Is.EqualTo("s1"));
            Assert.That(vacation.NewState, Is.EqualTo("v1"));
        }
    }

    [Test]
    public async Task BinaryOperationsUseSessionTemplatesAndSelectedAccount()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        JmapClient client = new JmapClient(Options(), transport);
        MemoryStream uploadContent = new MemoryStream(new byte[] { 1, 2, 3 });
        BlobUploadRequest upload = new BlobUploadRequest
        {
            MediaType = "application/octet-stream",
            Content = uploadContent,
            ContentLength = 3,
            HasContentLength = true
        };
        BlobDownloadRequest download = new BlobDownloadRequest
        {
            BlobId = "blob-1",
            Type = "text/plain",
            Name = "file.txt"
        };

        BlobUploadResponse uploaded = await client.UploadBlobAsync(upload, CancellationToken.None);
        BlobDownloadResponse downloaded = await client.DownloadBlobAsync(download, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.AccountId, Is.EqualTo("account-1"));
            Assert.That(download.AccountId, Is.EqualTo("account-1"));
            Assert.That(uploaded.BlobId, Is.EqualTo("uploaded-blob"));
            Assert.That(downloaded.MediaType, Is.EqualTo("text/plain"));
            Assert.That(transport.UploadUrls, Is.EqualTo(new[]
            {
                "https://jmap.example.test/upload/{accountId}"
            }));
            Assert.That(transport.DownloadUrls, Is.EqualTo(new[]
            {
                "https://jmap.example.test/download/{accountId}/{blobId}/{name}?type={type}"
            }));
        }
    }

    [Test]
    public async Task ReadOnlyAndUnsupportedAccountsFailBeforeApiOrUploadTransport()
    {
        RecordingTransport readOnlyTransport = new RecordingTransport();
        readOnlyTransport.SessionResults.Enqueue(Task.FromResult(Session("session-1", true)));
        JmapClient readOnlyClient = new JmapClient(Options(), readOnlyTransport);
        await readOnlyClient.RefreshSessionAsync(CancellationToken.None);

        JmapCapabilityException setError = Assert.ThrowsAsync<JmapCapabilityException>(async () =>
            await readOnlyClient.SetEmailsAsync(new EmailSetRequest(), CancellationToken.None))!;
        JmapCapabilityException uploadError = Assert.ThrowsAsync<JmapCapabilityException>(async () =>
            await readOnlyClient.UploadBlobAsync(new BlobUploadRequest(), CancellationToken.None))!;

        RecordingTransport unsupportedTransport = new RecordingTransport();
        JmapSession unsupported = Session("session-1", false);
        unsupported.Accounts["account-1"].AccountCapabilities.Remove(JmapCapabilities.Submission);
        unsupportedTransport.SessionResults.Enqueue(Task.FromResult(unsupported));
        JmapClient unsupportedClient = new JmapClient(Options(), unsupportedTransport);
        await unsupportedClient.RefreshSessionAsync(CancellationToken.None);
        JmapCapabilityException unsupportedError = Assert.ThrowsAsync<JmapCapabilityException>(async () =>
            await unsupportedClient.GetIdentitiesAsync(
                new IdentityGetRequest { AccountId = "account-1" }, CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(setError.AccountId, Is.EqualTo("account-1"));
            Assert.That(uploadError.AccountId, Is.EqualTo("account-1"));
            Assert.That(unsupportedError.Capability, Is.EqualTo(JmapCapabilities.Submission));
            Assert.That(readOnlyTransport.ApiRequests, Is.Empty);
            Assert.That(readOnlyTransport.UploadUrls, Is.Empty);
            Assert.That(unsupportedTransport.ApiRequests, Is.Empty);
        }
    }

    [Test]
    public void TypedMethodErrorsAreConvertedWithMethodAndCallContext()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ErrorMethods.Add(JmapMethods.EmailGet);
        JmapClient client = new JmapClient(Options(), transport);

        JmapMethodException error = Assert.ThrowsAsync<JmapMethodException>(async () =>
            await client.GetEmailsAsync(new EmailGetRequest(), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.MethodName, Is.EqualTo(JmapMethods.EmailGet));
            Assert.That(error.CallId, Is.EqualTo(JmapClient.SingleCallId));
            Assert.That(error.ErrorToken, Is.EqualTo(JmapErrorCodes.ServerUnavailable));
            Assert.That(error.IsRetryable, Is.True);
        }
    }

    [Test]
    public void StateMismatchErrorsRetainMutationRequestAndServerStateContext()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ErrorArguments[JmapMethods.EmailSet] =
            "{\"type\":\"stateMismatch\",\"description\":\"The state changed.\"," +
            "\"newState\":\"email-state-2\",\"vendorCode\":\"S9\"}";
        JmapClient client = new JmapClient(Options(), transport);
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            IfInState = "email-state-1"
        };

        JmapStateMismatchException error = Assert.ThrowsAsync<JmapStateMismatchException>(async () =>
            await client.SetEmailsAsync(request, CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.MethodName, Is.EqualTo(JmapMethods.EmailSet));
            Assert.That(error.CallId, Is.EqualTo(JmapClient.SingleCallId));
            Assert.That(error.AccountId, Is.EqualTo("account-1"));
            Assert.That(error.ExpectedState, Is.EqualTo("email-state-1"));
            Assert.That(error.ActualState, Is.EqualTo("email-state-2"));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(error.Error.UnknownProperties["vendorCode"].GetString(), Is.EqualTo("S9"));
            Assert.That(error.StructuredData.GetProperty("newState").GetString(),
                Is.EqualTo("email-state-2"));
            Assert.That(error.IsRetryable, Is.True);
        }
    }

    [Test]
    public void UnknownMethodErrorsRemainInspectableAndAreNotRetryable()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ErrorArguments[JmapMethods.EmailGet] =
            "{\"type\":\"vendorFailure\",\"description\":\"Vendor failure.\"," +
            "\"vendorCode\":27}";
        JmapClient client = new JmapClient(Options(), transport);

        JmapMethodException error = Assert.ThrowsAsync<JmapMethodException>(async () =>
            await client.GetEmailsAsync(new EmailGetRequest(), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.ErrorToken, Is.EqualTo("vendorFailure"));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(error.Error.Description, Is.EqualTo("Vendor failure."));
            Assert.That(error.Error.UnknownProperties["vendorCode"].GetInt32(), Is.EqualTo(27));
            Assert.That(error.StructuredData.GetProperty("vendorCode").GetInt32(), Is.EqualTo(27));
            Assert.That(error.IsRetryable, Is.False);
        }
    }

    [Test]
    public async Task PerRecordErrorsAreReturnedWithoutBecomingMethodExceptions()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ResponseArguments[JmapMethods.EmailSet] =
            "{\"accountId\":\"account-1\",\"oldState\":\"email-state-1\"," +
            "\"newState\":\"email-state-1\",\"notDestroyed\":{" +
            "\"email-1\":{\"type\":\"vendorRecordError\",\"description\":\"No.\"," +
            "\"vendorCode\":41}}}";
        JmapClient client = new JmapClient(Options(), transport);
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Destroy = new List<string> { "email-1" }
        };

        EmailSetResponse response = await client.SetEmailsAsync(request, CancellationToken.None);
        JmapSetError error = response.NotDestroyed!["email-1"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo("vendorRecordError"));
            Assert.That(error.Kind, Is.EqualTo(JmapSetErrorKind.Unknown));
            Assert.That(error.Description, Is.EqualTo("No."));
            Assert.That(error.UnknownProperties["vendorCode"].GetInt32(), Is.EqualTo(41));
        }
    }

    [Test]
    public void RemainingTypedOperationsBuildRequestsBeforeMappingMethodErrors()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(Session("session-1", false)));
        transport.ErrorAllMethods = true;
        JmapClient client = new JmapClient(Options(), transport);

        AssertMethodFailure(client.GetMailboxChangesAsync(
            new MailboxChangesRequest { SinceState = "m0" }, CancellationToken.None));
        AssertMethodFailure(client.QueryMailboxesAsync(
            new MailboxQueryRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetMailboxQueryChangesAsync(
            new MailboxQueryChangesRequest { SinceQueryState = "mq0" }, CancellationToken.None));
        AssertMethodFailure(client.SetMailboxesAsync(
            new MailboxSetRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetThreadChangesAsync(
            new ThreadChangesRequest { SinceState = "t0" }, CancellationToken.None));
        AssertMethodFailure(client.GetEmailsAsync(
            new EmailGetRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetEmailChangesAsync(
            new EmailChangesRequest { SinceState = "e0" }, CancellationToken.None));
        AssertMethodFailure(client.GetEmailQueryChangesAsync(
            new EmailQueryChangesRequest { SinceQueryState = "eq0" }, CancellationToken.None));
        AssertMethodFailure(client.SetEmailsAsync(
            new EmailSetRequest(), CancellationToken.None));
        AssertMethodFailure(client.CopyEmailsAsync(
            new EmailCopyRequest { FromAccountId = "source" }, CancellationToken.None));
        AssertMethodFailure(client.ImportEmailsAsync(
            new EmailImportRequest(), CancellationToken.None));
        AssertMethodFailure(client.ParseEmailsAsync(
            new EmailParseRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetIdentityChangesAsync(
            new IdentityChangesRequest { SinceState = "i0" }, CancellationToken.None));
        AssertMethodFailure(client.SetIdentitiesAsync(
            new IdentitySetRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetEmailSubmissionsAsync(
            new EmailSubmissionGetRequest(), CancellationToken.None));
        AssertMethodFailure(client.GetEmailSubmissionChangesAsync(
            new EmailSubmissionChangesRequest { SinceState = "s0" }, CancellationToken.None));
        AssertMethodFailure(client.GetEmailSubmissionQueryChangesAsync(
            new EmailSubmissionQueryChangesRequest { SinceQueryState = "sq0" },
            CancellationToken.None));
        AssertMethodFailure(client.SetEmailSubmissionsAsync(
            new EmailSubmissionSetRequest(), CancellationToken.None));
        AssertMethodFailure(client.SetVacationResponseAsync(
            new VacationResponseSetRequest(), CancellationToken.None));
        AssertMethodFailure(client.SetPushSubscriptionsAsync(
            new PushSubscriptionSetRequest(), CancellationToken.None));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transport.ApiRequests, Has.Count.EqualTo(20));
            Assert.That(transport.ApiRequests.Select(request => request.MethodCalls[0].Name),
                Is.EqualTo(new[]
                {
                    JmapMethods.MailboxChanges, JmapMethods.MailboxQuery,
                    JmapMethods.MailboxQueryChanges, JmapMethods.MailboxSet,
                    JmapMethods.ThreadChanges, JmapMethods.EmailGet, JmapMethods.EmailChanges,
                    JmapMethods.EmailQueryChanges, JmapMethods.EmailSet, JmapMethods.EmailCopy,
                    JmapMethods.EmailImport, JmapMethods.EmailParse, JmapMethods.IdentityChanges,
                    JmapMethods.IdentitySet, JmapMethods.EmailSubmissionGet,
                    JmapMethods.EmailSubmissionChanges, JmapMethods.EmailSubmissionQueryChanges,
                    JmapMethods.EmailSubmissionSet, JmapMethods.VacationResponseSet,
                    JmapMethods.PushSubscriptionSet
                }));
        }
    }

    private static void AssertMethodFailure(Task operation)
    {
        Assert.That(async () => await operation, Throws.TypeOf<JmapMethodException>());
    }

    private static JmapClientOptions Options()
    {
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = "https://jmap.example.test/session";
        return options;
    }

    private static JmapSession Session(string state, bool readOnly)
    {
        JmapSession session = new JmapSession();
        session.Username = "user@example.test";
        session.ApiUrl = "https://jmap.example.test/api";
        session.DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}/{name}?type={type}";
        session.UploadUrl = "https://jmap.example.test/upload/{accountId}";
        session.EventSourceUrl = "https://jmap.example.test/events?types={types}";
        session.State = state;
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability
        {
            Core = new JmapCoreCapabilities()
        };
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability();
        session.Capabilities[JmapCapabilities.Submission] = new JmapCapability();
        session.Capabilities[JmapCapabilities.VacationResponse] = new JmapCapability();

        JmapAccount account = Account(readOnly);
        session.Accounts["account-1"] = account;
        session.Accounts["source"] = Account(false);
        session.PrimaryAccounts[JmapCapabilities.Mail] = "account-1";
        session.PrimaryAccounts[JmapCapabilities.Submission] = "account-1";
        session.PrimaryAccounts[JmapCapabilities.VacationResponse] = "account-1";
        return session;
    }

    private static JmapAccount Account(bool readOnly)
    {
        JmapAccount account = new JmapAccount();
        account.Name = "Account";
        account.IsReadOnly = readOnly;
        account.AccountCapabilities[JmapCapabilities.Mail] = new JmapCapability
        {
            Mail = new JmapMailCapabilities()
        };
        account.AccountCapabilities[JmapCapabilities.Submission] = new JmapCapability
        {
            Submission = new JmapSubmissionCapabilities()
        };
        account.AccountCapabilities[JmapCapabilities.VacationResponse] = new JmapCapability
        {
            VacationResponse = new JmapVacationResponseCapabilities()
        };
        return account;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class RecordingTransport : IJmapTransport
    {
        public Queue<Task<JmapSession>> SessionResults = new Queue<Task<JmapSession>>();
        public Queue<string> ResponseStates = new Queue<string>();
        public Dictionary<string, string> ResponseArguments = new Dictionary<string, string>();
        public Dictionary<string, string> ErrorArguments = new Dictionary<string, string>();
        public HashSet<string> ErrorMethods = new HashSet<string>();
        public bool ErrorAllMethods;
        public List<string> SessionUrls = new List<string>();
        public List<JmapRequest> ApiRequests = new List<JmapRequest>();
        public List<string> UploadUrls = new List<string>();
        public List<string> DownloadUrls = new List<string>();
        public bool ReturnInvalidApiResponse;

        public Task<JmapSession> GetTransportSessionAsync(
            string sessionUrl,
            CancellationToken cancellationToken)
        {
            SessionUrls.Add(sessionUrl);
            return SessionResults.Dequeue();
        }

        public Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            ApiRequests.Add(request);
            if (ReturnInvalidApiResponse)
            {
                return Task.FromResult<JmapResponse>(null!);
            }

            JmapInvocation call = request.MethodCalls[0];
            JmapInvocation invocation = new JmapInvocation();
            invocation.CallId = call.CallId;
            if (ErrorAllMethods || call.Name != null &&
                (ErrorMethods.Contains(call.Name) || ErrorArguments.ContainsKey(call.Name)))
            {
                invocation.Name = ResponseMapper.ErrorResponseName;
                if (call.Name != null && ErrorArguments.ContainsKey(call.Name))
                {
                    invocation.Arguments = Json(ErrorArguments[call.Name]);
                }
                else
                {
                    invocation.Arguments = Json("{\"type\":\"serverUnavailable\"}");
                }
            }
            else
            {
                invocation.Name = call.Name;
                if (call.Name == JmapMethods.CoreEcho ||
                    call.Name != null && !ResponseArguments.ContainsKey(call.Name))
                {
                    invocation.Arguments = call.Arguments.Clone();
                }
                else
                {
                    invocation.Arguments = Json(ResponseArguments[call.Name == null ? "" : call.Name]);
                }
            }

            JmapResponse response = new JmapResponse();
            response.MethodResponses.Add(invocation);
            response.SessionState = ResponseStates.Count == 0 ? "session-1" : ResponseStates.Dequeue();
            return Task.FromResult(response);
        }

        public Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            UploadUrls.Add(uploadUrl);
            BlobUploadResponse response = new BlobUploadResponse();
            response.AccountId = request.AccountId;
            response.BlobId = "uploaded-blob";
            response.Type = request.MediaType;
            response.Size = request.ContentLength;
            return Task.FromResult(response);
        }

        public Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            DownloadUrls.Add(downloadUrl);
            BlobDownloadResponse response = new BlobDownloadResponse();
            response.Content = new MemoryStream(new byte[] { 1, 2, 3 });
            response.MediaType = request.Type;
            response.FileName = request.Name;
            response.ContentLength = 3;
            response.HasContentLength = true;
            return Task.FromResult(response);
        }

        public Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private class RecordingLocator : IJmapServiceLocator
    {
        public List<string?> Usernames = new List<string?>();

        public Task<string> LocateSessionUrlAsync(
            JmapDiscoveryOptions options,
            CancellationToken cancellationToken)
        {
            Usernames.Add(options.Username);
            return Task.FromResult("https://located.example.test/.well-known/jmap");
        }
    }

    private class CanceledLocator : IJmapServiceLocator
    {
        public CancellationToken CancellationToken;

        public Task<string> LocateSessionUrlAsync(
            JmapDiscoveryOptions options,
            CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return Task.FromCanceled<string>(cancellationToken);
        }
    }
}
