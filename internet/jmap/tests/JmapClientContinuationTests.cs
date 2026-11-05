using System.Text.Json;

namespace jmap.Tests;

public class JmapClientContinuationTests
{
    [Test]
    public async Task EmailChangesPagesAdvanceStateAndCollapseRepeatedClassifications()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"s0\",\"newState\":\"s1\"," +
            "\"hasMoreChanges\":true,\"created\":[\"a\"],\"updated\":[\"b\"]," +
            "\"destroyed\":[\"c\"],\"vendorFirst\":1}");
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"s1\",\"newState\":\"s2\"," +
            "\"hasMoreChanges\":true,\"created\":[\"d\"],\"updated\":[\"a\"]," +
            "\"destroyed\":[\"b\"]}");
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"s2\",\"newState\":\"s3\"," +
            "\"hasMoreChanges\":false,\"created\":[\"e\"],\"updated\":[\"d\"]," +
            "\"destroyed\":[\"a\"],\"vendorFinal\":true}");
        JmapClient client = Client(transport);
        EmailChangesRequest request = new EmailChangesRequest
        {
            SinceState = "s0",
            HasMaxChanges = true,
            MaxChanges = 3
        };
        request.UnknownProperties["vendorRequest"] = Json("true");

        EmailChangesResponse result = await client.GetAllEmailChangesAsync(
            request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.OldState, Is.EqualTo("s0"));
            Assert.That(result.NewState, Is.EqualTo("s3"));
            Assert.That(result.HasMoreChanges, Is.False);
            Assert.That(result.Created, Is.EqualTo(new[] { "d", "e" }));
            Assert.That(result.Updated, Is.Empty);
            Assert.That(result.Destroyed, Is.EqualTo(new[] { "b", "c" }));
            Assert.That(result.UnknownProperties["vendorFirst"].GetInt32(), Is.EqualTo(1));
            Assert.That(result.UnknownProperties["vendorFinal"].GetBoolean(), Is.True);
            Assert.That(request.AccountId, Is.Null);
            Assert.That(request.SinceState, Is.EqualTo("s0"));
            Assert.That(transport.RequestArguments.Select(value =>
                    value.GetProperty("sinceState").GetString()),
                Is.EqualTo(new[] { "s0", "s1", "s2" }));
            Assert.That(transport.RequestArguments.All(value =>
                value.GetProperty("maxChanges").GetInt64() == 3), Is.True);
            Assert.That(transport.RequestArguments.All(value =>
                value.GetProperty("vendorRequest").GetBoolean()), Is.True);
        }
    }

    [Test]
    public async Task EveryChangesFamilyUsesTheSharedContinuationAndMailboxMetadataIsCombined()
    {
        ScriptedTransport transport = new ScriptedTransport();
        EnqueueTwoPages(transport, "m0", "m1", "m2",
            ",\"updatedProperties\":[\"totalEmails\"]",
            ",\"updatedProperties\":[\"unreadEmails\"]");
        EnqueueTwoPages(transport, "t0", "t1", "t2", "", "");
        EnqueueTwoPages(transport, "i0", "i1", "i2", "", "");
        EnqueueTwoPages(transport, "u0", "u1", "u2", "", "");
        JmapClient client = Client(transport);

        MailboxChangesResponse mailboxes = await client.GetAllMailboxChangesAsync(
            new MailboxChangesRequest { SinceState = "m0" }, CancellationToken.None);
        ThreadChangesResponse threads = await client.GetAllThreadChangesAsync(
            new ThreadChangesRequest { SinceState = "t0" }, CancellationToken.None);
        IdentityChangesResponse identities = await client.GetAllIdentityChangesAsync(
            new IdentityChangesRequest { SinceState = "i0" }, CancellationToken.None);
        EmailSubmissionChangesResponse submissions =
            await client.GetAllEmailSubmissionChangesAsync(
                new EmailSubmissionChangesRequest { SinceState = "u0" },
                CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mailboxes.Created, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(mailboxes.Updated, Is.Empty);
            Assert.That(mailboxes.UpdatedProperties,
                Is.EqualTo(new[] { "totalEmails", "unreadEmails" }));
            Assert.That(threads.NewState, Is.EqualTo("t2"));
            Assert.That(threads.Created, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(identities.NewState, Is.EqualTo("i2"));
            Assert.That(identities.Created, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(submissions.NewState, Is.EqualTo("u2"));
            Assert.That(submissions.Created, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(transport.MethodNames, Is.EqualTo(new[]
            {
                JmapMethods.MailboxChanges, JmapMethods.MailboxChanges,
                JmapMethods.ThreadChanges, JmapMethods.ThreadChanges,
                JmapMethods.IdentityChanges, JmapMethods.IdentityChanges,
                JmapMethods.EmailSubmissionChanges, JmapMethods.EmailSubmissionChanges
            }));
        }
    }

    [Test]
    public void QueryContinuationsApplyMovesInOrderAndReturnDetachedSnapshots()
    {
        JmapClient client = Client(new ScriptedTransport());
        JmapQuerySnapshot original = Snapshot();
        MailboxQueryChangesResponse mailboxChanges = new MailboxQueryChangesResponse
        {
            AccountId = "account-1",
            OldQueryState = "q0",
            NewQueryState = "q1"
        };
        mailboxChanges.Removed.Add("b");
        mailboxChanges.Removed.Add("d");
        mailboxChanges.Added.Add(new JmapAddedItem { Id = "d", Index = 0 });
        mailboxChanges.Added.Add(new JmapAddedItem { Id = "e", Index = 2 });

        JmapQuerySnapshot mailbox = client.ContinueMailboxQuery(original, mailboxChanges);
        EmailQueryChangesResponse emailChanges = new EmailQueryChangesResponse
        {
            AccountId = "account-1",
            OldQueryState = "q1",
            NewQueryState = "q2"
        };
        emailChanges.Removed.Add("a");
        emailChanges.Added.Add(new JmapAddedItem { Id = "a", Index = 3 });
        JmapQuerySnapshot email = client.ContinueEmailQuery(mailbox, emailChanges);
        EmailSubmissionQueryChangesResponse submissionChanges =
            new EmailSubmissionQueryChangesResponse
            {
                AccountId = "account-1",
                OldQueryState = "q2",
                NewQueryState = "q3"
            };
        submissionChanges.Removed.Add("e");
        JmapQuerySnapshot submission = client.ContinueEmailSubmissionQuery(
            email, submissionChanges);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(original.Ids, Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(original.QueryState, Is.EqualTo("q0"));
            Assert.That(mailbox.Ids, Is.EqualTo(new[] { "d", "a", "e", "c" }));
            Assert.That(mailbox.QueryState, Is.EqualTo("q1"));
            Assert.That(email.Ids, Is.EqualTo(new[] { "d", "e", "c", "a" }));
            Assert.That(submission.Ids, Is.EqualTo(new[] { "d", "c", "a" }));
            Assert.That(submission.QueryState, Is.EqualTo("q3"));
            Assert.That(submission.Ids.Distinct().Count(), Is.EqualTo(submission.Ids.Count));
        }
    }

    [Test]
    public void QueryContinuationsRejectStateMismatchMissingRemovalAndDuplicateInsertion()
    {
        JmapClient client = Client(new ScriptedTransport());
        JmapQuerySnapshot snapshot = Snapshot();
        EmailQueryChangesResponse wrongState = QueryChanges("other", "q1");
        EmailQueryChangesResponse missingRemoval = QueryChanges("q0", "q1");
        missingRemoval.Removed.Add("absent");
        EmailQueryChangesResponse duplicate = QueryChanges("q0", "q1");
        duplicate.Added.Add(new JmapAddedItem { Id = "a", Index = 0 });

        Assert.That(() => client.ContinueEmailQuery(snapshot, wrongState),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => client.ContinueEmailQuery(snapshot, missingRemoval),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => client.ContinueEmailQuery(snapshot, duplicate),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void ChangesContinuationRejectsANonAdvancingPage()
    {
        ScriptedTransport transport = new ScriptedTransport();
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"s0\",\"newState\":\"s0\"," +
            "\"hasMoreChanges\":true,\"created\":[\"a\"],\"updated\":[],\"destroyed\":[]}");
        JmapClient client = Client(transport);

        Assert.That(async () => await client.GetAllEmailChangesAsync(
                new EmailChangesRequest { SinceState = "s0" }, CancellationToken.None),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(transport.MethodNames, Has.Count.EqualTo(1));
    }

    private static void EnqueueTwoPages(
        ScriptedTransport transport,
        string oldState,
        string middleState,
        string newState,
        string firstExtra,
        string secondExtra)
    {
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"" + oldState +
            "\",\"newState\":\"" + middleState +
            "\",\"hasMoreChanges\":true,\"created\":[\"first\"],\"updated\":[]," +
            "\"destroyed\":[]" + firstExtra + "}");
        transport.Responses.Enqueue(
            "{\"accountId\":\"account-1\",\"oldState\":\"" + middleState +
            "\",\"newState\":\"" + newState +
            "\",\"hasMoreChanges\":false,\"created\":[\"second\"]," +
            "\"updated\":[\"first\"],\"destroyed\":[]" + secondExtra + "}");
    }

    private static EmailQueryChangesResponse QueryChanges(string oldState, string newState)
    {
        return new EmailQueryChangesResponse
        {
            AccountId = "account-1",
            OldQueryState = oldState,
            NewQueryState = newState
        };
    }

    private static JmapQuerySnapshot Snapshot()
    {
        JmapQuerySnapshot snapshot = new JmapQuerySnapshot();
        snapshot.AccountId = "account-1";
        snapshot.QueryState = "q0";
        snapshot.Ids.AddRange(new[] { "a", "b", "c", "d" });
        return snapshot;
    }

    private static JmapClient Client(ScriptedTransport transport)
    {
        transport.Sessions.Enqueue(Task.FromResult(Session()));
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = "https://jmap.example.test/session";
        return new JmapClient(options, transport);
    }

    private static JmapSession Session()
    {
        JmapSession session = new JmapSession();
        session.Username = "user@example.test";
        session.ApiUrl = "https://jmap.example.test/api";
        session.DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}/{name}";
        session.UploadUrl = "https://jmap.example.test/upload/{accountId}";
        session.EventSourceUrl = "https://jmap.example.test/events";
        session.State = "session-state";
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability
        {
            Core = new JmapCoreCapabilities()
        };
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability();
        session.Capabilities[JmapCapabilities.Submission] = new JmapCapability();
        JmapAccount account = new JmapAccount();
        account.Name = "Account";
        account.AccountCapabilities[JmapCapabilities.Mail] = new JmapCapability
        {
            Mail = new JmapMailCapabilities()
        };
        account.AccountCapabilities[JmapCapabilities.Submission] = new JmapCapability
        {
            Submission = new JmapSubmissionCapabilities()
        };
        session.Accounts["account-1"] = account;
        session.PrimaryAccounts[JmapCapabilities.Mail] = "account-1";
        session.PrimaryAccounts[JmapCapabilities.Submission] = "account-1";
        return session;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class ScriptedTransport : IJmapTransport
    {
        public Queue<Task<JmapSession>> Sessions = new Queue<Task<JmapSession>>();
        public Queue<string> Responses = new Queue<string>();
        public List<string?> MethodNames = new List<string?>();
        public List<JsonElement> RequestArguments = new List<JsonElement>();

        public Task<JmapSession> GetTransportSessionAsync(
            string sessionUrl,
            CancellationToken cancellationToken)
        {
            return Sessions.Dequeue();
        }

        public Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            JmapInvocation call = request.MethodCalls[0];
            MethodNames.Add(call.Name);
            RequestArguments.Add(call.Arguments.Clone());
            JmapInvocation responseCall = new JmapInvocation();
            responseCall.Name = call.Name;
            responseCall.CallId = call.CallId;
            responseCall.Arguments = Json(Responses.Dequeue());
            JmapResponse response = new JmapResponse();
            response.MethodResponses.Add(responseCall);
            response.SessionState = "session-state";
            return Task.FromResult(response);
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
