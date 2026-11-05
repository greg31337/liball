using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class PublicCodecRoundTripTests
{
    [Test]
    public void SessionAndEveryCapabilityShapeRoundTripThroughThePublicCodec()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapCoreCapabilities core = new JmapCoreCapabilities
        {
            MaxSizeUpload = 50000000,
            MaxConcurrentUpload = 4,
            MaxSizeRequest = 10000000,
            MaxConcurrentRequests = 4,
            MaxCallsInRequest = 16,
            MaxObjectsInGet = 500,
            MaxObjectsInSet = 500
        };
        core.CollationAlgorithms.Add("i;unicode-casemap");
        core.UnknownProperties["vendorCore"] = Json("true");
        JmapMailCapabilities mail = new JmapMailCapabilities
        {
            HasMaxMailboxesPerEmail = true,
            IsMaxMailboxesPerEmailNull = true,
            HasMaxMailboxDepth = true,
            MaxMailboxDepth = 10,
            MaxSizeMailboxName = 255,
            MaxSizeAttachmentsPerEmail = 50000000,
            MayCreateTopLevelMailbox = true
        };
        mail.EmailQuerySortOptions.Add(JmapProperties.ReceivedAt);
        mail.UnknownProperties["vendorMail"] = Json("17");
        JmapSubmissionCapabilities submission = new JmapSubmissionCapabilities
        {
            MaxDelayedSend = 3600
        };
        submission.SubmissionExtensions["FUTURERELEASE"] = new List<string> { "T", "+" };
        submission.UnknownProperties["vendorSubmission"] = Json("null");
        JmapVacationResponseCapabilities vacation = new JmapVacationResponseCapabilities();
        vacation.UnknownProperties["vendorVacation"] = Json("{\"enabled\":true}");

        JmapSession session = new JmapSession
        {
            Username = "δοκιμή@example.test",
            ApiUrl = "https://jmap.example.test/api",
            DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}/{name}?type={type}",
            UploadUrl = "https://jmap.example.test/upload/{accountId}",
            EventSourceUrl = "https://jmap.example.test/events?types={types}",
            State = "session-1"
        };
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability { Core = core };
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability();
        session.Capabilities[JmapCapabilities.Submission] = new JmapCapability();
        session.Capabilities[JmapCapabilities.VacationResponse] = new JmapCapability();
        session.Capabilities["https://vendor.example/capability"] = new JmapCapability
        {
            UnknownProperties = { ["feature"] = Json("\"widgets\"") }
        };
        JmapAccount account = new JmapAccount
        {
            Name = "Pérsonal",
            IsPersonal = true,
            IsReadOnly = false
        };
        account.AccountCapabilities[JmapCapabilities.Mail] = new JmapCapability { Mail = mail };
        account.AccountCapabilities[JmapCapabilities.Submission] =
            new JmapCapability { Submission = submission };
        account.AccountCapabilities[JmapCapabilities.VacationResponse] =
            new JmapCapability { VacationResponse = vacation };
        account.UnknownProperties["vendorAccount"] = Json("[1,2]");
        session.Accounts["a1"] = account;
        session.PrimaryAccounts[JmapCapabilities.Mail] = "a1";
        session.UnknownProperties["vendorSession"] = Json("false");

        JmapSession decoded = codec.DecodeSessionBytes(codec.EncodeSessionBytes(session));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decoded.Username, Is.EqualTo("δοκιμή@example.test"));
            Assert.That(decoded.ApiUrl, Is.EqualTo(session.ApiUrl));
            Assert.That(decoded.DownloadUrl, Is.EqualTo(session.DownloadUrl));
            Assert.That(decoded.UploadUrl, Is.EqualTo(session.UploadUrl));
            Assert.That(decoded.EventSourceUrl, Is.EqualTo(session.EventSourceUrl));
            Assert.That(decoded.State, Is.EqualTo("session-1"));
            Assert.That(decoded.PrimaryAccounts[JmapCapabilities.Mail], Is.EqualTo("a1"));
            Assert.That(decoded.Capabilities[JmapCapabilities.Core].Core!.MaxObjectsInSet,
                Is.EqualTo(500));
            Assert.That(decoded.Capabilities[JmapCapabilities.Core].Core!.CollationAlgorithms,
                Is.EqualTo(new[] { "i;unicode-casemap" }));
            Assert.That(decoded.Capabilities[JmapCapabilities.Core].Core!
                .UnknownProperties["vendorCore"].GetBoolean(), Is.True);
            Assert.That(decoded.Capabilities["https://vendor.example/capability"]
                .UnknownProperties["feature"].GetString(), Is.EqualTo("widgets"));
            Assert.That(decoded.Accounts["a1"].Name, Is.EqualTo("Pérsonal"));
            Assert.That(decoded.Accounts["a1"].IsPersonal, Is.True);
            Assert.That(decoded.Accounts["a1"].IsReadOnly, Is.False);
            Assert.That(decoded.Accounts["a1"].AccountCapabilities[JmapCapabilities.Mail]
                .Mail!.IsMaxMailboxesPerEmailNull, Is.True);
            Assert.That(decoded.Accounts["a1"].AccountCapabilities[JmapCapabilities.Mail]
                .Mail!.MaxMailboxDepth, Is.EqualTo(10));
            Assert.That(decoded.Accounts["a1"].AccountCapabilities[JmapCapabilities.Submission]
                .Submission!.SubmissionExtensions["FUTURERELEASE"], Is.EqualTo(new[] { "T", "+" }));
            Assert.That(decoded.Accounts["a1"].AccountCapabilities[JmapCapabilities.VacationResponse]
                .VacationResponse!.UnknownProperties["vendorVacation"]
                .GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(decoded.Accounts["a1"].UnknownProperties["vendorAccount"].GetArrayLength(),
                Is.EqualTo(2));
            Assert.That(decoded.UnknownProperties["vendorSession"].GetBoolean(), Is.False);
        }
    }

    [Test]
    public void RfcExamplesInvocationTuplesUnicodeAndDynamicHeadersRoundTrip()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapRequest rfcRequest = codec.DecodeRequestBytes(
            Encoding.UTF8.GetBytes(RfcExampleFixtures.CoreEchoRequestJson));
        JmapResponse rfcResponse = codec.DecodeResponseBytes(
            Encoding.UTF8.GetBytes(RfcExampleFixtures.CoreEchoResponseJson));

        JmapRequest request = new JmapRequest
        {
            CreatedIds = new Dictionary<string, string> { ["draft"] = "e-1" }
        };
        request.Using.Add(JmapCapabilities.Core);
        request.Using.Add("https://vendor.example/capability");
        request.MethodCalls.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json(
                "{\"header:Subject:asText\":\"Grüße ☃\",\"header:X-List:asURLs:all\":[\"https://example.test/é\"],\"empty\":null}"),
            CallId = "c-α"
        });
        request.UnknownProperties["vendorRequest"] = Json("{\"enabled\":true}");

        byte[] encoded = codec.EncodeRequestBytes(request);
        JmapRequest decoded = codec.DecodeRequestMemory(encoded);
        JmapResponse response = new JmapResponse
        {
            CreatedIds = new Dictionary<string, string> { ["draft"] = "e-1" },
            SessionState = "state-3"
        };
        response.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = request.MethodCalls[0].Arguments,
            CallId = "c-α"
        });
        response.MethodResponses.Add(new JmapInvocation
        {
            Name = "error",
            Arguments = Json("{\"type\":\"vendorError\",\"detail\":\"échec\"}"),
            CallId = "c-β"
        });
        response.UnknownProperties["vendorResponse"] = Json("[\"kept\"]");
        JmapResponse decodedRichResponse = codec.DecodeResponseBytes(
            codec.EncodeResponseBytes(response));
        using JsonDocument wire = JsonDocument.Parse(encoded);
        JsonElement tuple = wire.RootElement.GetProperty(JmapProperties.MethodCalls)[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rfcRequest.MethodCalls[0].Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(rfcRequest.MethodCalls[0].Arguments.GetProperty("hello").GetString(),
                Is.EqualTo("world"));
            Assert.That(rfcResponse.SessionState, Is.EqualTo("state-2"));
            Assert.That(tuple.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(tuple.GetArrayLength(), Is.EqualTo(3));
            Assert.That(tuple[0].GetString(), Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(tuple[2].GetString(), Is.EqualTo("c-α"));
            Assert.That(decoded.Using[1], Is.EqualTo("https://vendor.example/capability"));
            Assert.That(decoded.CreatedIds!["draft"], Is.EqualTo("e-1"));
            Assert.That(decoded.MethodCalls[0].Arguments
                .GetProperty("header:Subject:asText").GetString(), Is.EqualTo("Grüße ☃"));
            Assert.That(decoded.MethodCalls[0].Arguments
                .GetProperty("header:X-List:asURLs:all")[0].GetString(),
                Is.EqualTo("https://example.test/é"));
            Assert.That(decoded.MethodCalls[0].Arguments.GetProperty("empty").ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(decoded.UnknownProperties["vendorRequest"]
                .GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(decodedRichResponse.CreatedIds!["draft"], Is.EqualTo("e-1"));
            Assert.That(decodedRichResponse.SessionState, Is.EqualTo("state-3"));
            Assert.That(decodedRichResponse.MethodResponses.Select(item => item.CallId),
                Is.EqualTo(new[] { "c-α", "c-β" }));
            Assert.That(decodedRichResponse.MethodResponses[1].Arguments
                .GetProperty(JmapProperties.Detail).GetString(), Is.EqualTo("échec"));
            Assert.That(decodedRichResponse.UnknownProperties["vendorResponse"][0].GetString(),
                Is.EqualTo("kept"));
        }
    }

    [Test]
    public async Task EveryStreamOperationHandlesFragmentedInputAndLeavesCallerStreamsOpen()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapSession session = PublicApiTestHelper.CoreSession("session-1");
        JmapRequest request = PublicApiTestHelper.CoreEchoRequest("c1", "{\"unicode\":\"雪\"}");
        JmapResponse response = new JmapResponse { SessionState = "session-2" };
        response.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json("{\"unicode\":\"雪\"}"),
            CallId = "c1"
        });
        JmapProblem problem = new JmapProblem
        {
            Type = JmapErrorCodes.NotRequestProblemType,
            Detail = "Malformed"
        };
        StateChange stateChange = new StateChange();
        stateChange.Changed["a1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "e2"
        };

        using MemoryStream sessionOutput = new MemoryStream();
        using MemoryStream requestOutput = new MemoryStream();
        using MemoryStream responseOutput = new MemoryStream();
        using MemoryStream problemOutput = new MemoryStream();
        using MemoryStream stateOutput = new MemoryStream();
        await codec.EncodeSessionStreamAsync(session, sessionOutput, CancellationToken.None);
        await codec.EncodeRequestStreamAsync(request, requestOutput, CancellationToken.None);
        await codec.EncodeResponseStreamAsync(response, responseOutput, CancellationToken.None);
        await codec.EncodeProblemStreamAsync(problem, problemOutput, CancellationToken.None);
        await codec.EncodeStateChangeStreamAsync(stateChange, stateOutput, CancellationToken.None);

        using FragmentedReadStream sessionInput = new FragmentedReadStream(sessionOutput.ToArray(), 1);
        using FragmentedReadStream requestInput = new FragmentedReadStream(requestOutput.ToArray(), 1);
        using FragmentedReadStream responseInput = new FragmentedReadStream(responseOutput.ToArray(), 1);
        using FragmentedReadStream problemInput = new FragmentedReadStream(problemOutput.ToArray(), 1);
        using FragmentedReadStream stateInput = new FragmentedReadStream(stateOutput.ToArray(), 1);
        JmapSession decodedSession = await codec.DecodeSessionStreamAsync(
            sessionInput, CancellationToken.None);
        JmapRequest decodedRequest = await codec.DecodeRequestStreamAsync(
            requestInput, CancellationToken.None);
        JmapResponse decodedResponse = await codec.DecodeResponseStreamAsync(
            responseInput, CancellationToken.None);
        JmapProblem decodedProblem = await codec.DecodeProblemStreamAsync(
            problemInput, CancellationToken.None);
        StateChange decodedState = await codec.DecodeStateChangeStreamAsync(
            stateInput, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decodedSession.State, Is.EqualTo("session-1"));
            Assert.That(decodedRequest.MethodCalls[0].Arguments.GetProperty("unicode").GetString(),
                Is.EqualTo("雪"));
            Assert.That(decodedResponse.SessionState, Is.EqualTo("session-2"));
            Assert.That(decodedProblem.Kind, Is.EqualTo(JmapProblemKind.NotRequest));
            Assert.That(decodedState.Changed["a1"][JmapDataTypes.Email], Is.EqualTo("e2"));
            Assert.That(sessionOutput.CanWrite, Is.True);
            Assert.That(requestOutput.CanWrite, Is.True);
            Assert.That(responseOutput.CanWrite, Is.True);
            Assert.That(problemOutput.CanWrite, Is.True);
            Assert.That(stateOutput.CanWrite, Is.True);
            Assert.That(sessionInput.CanRead, Is.True);
            Assert.That(requestInput.CanRead, Is.True);
            Assert.That(responseInput.CanRead, Is.True);
            Assert.That(problemInput.CanRead, Is.True);
            Assert.That(stateInput.CanRead, Is.True);
        }
    }

    [Test]
    public void OmittedDefaultsAndExplicitNullRemainDistinctAtEveryRequestBoundary()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapRequest protocolRequest = PublicApiTestHelper.CoreEchoRequest("c1", "{}");
        JmapResponse protocolResponse = new JmapResponse { SessionState = "s1" };
        JmapProblem problem = new JmapProblem { Type = JmapErrorCodes.NotRequestProblemType };

        JsonElement protocol = Parse(codec.EncodeRequestBytes(protocolRequest));
        JsonElement response = Parse(codec.EncodeResponseBytes(protocolResponse));
        JsonElement problemJson = Parse(codec.EncodeProblemBytes(problem));
        JsonElement get = codec.EncodeStandardGetRequest(new JmapGetRequest { AccountId = "a1" });
        JsonElement changes = codec.EncodeStandardChangesRequest(
            new JmapChangesRequest { AccountId = "a1", SinceState = "s1" });
        JsonElement changesNull = codec.EncodeStandardChangesRequest(new JmapChangesRequest
        {
            AccountId = "a1",
            SinceState = "s1",
            HasMaxChanges = true,
            IsMaxChangesNull = true
        });
        JsonElement set = codec.EncodeStandardSetRequest(new JmapSetRequest { AccountId = "a1" });
        JsonElement copy = codec.EncodeStandardCopyRequest(new JmapCopyRequest
        {
            FromAccountId = "a1",
            AccountId = "a2"
        });
        JsonElement query = codec.EncodeStandardQueryRequest(new JmapQueryRequest { AccountId = "a1" });
        JsonElement queryChanges = codec.EncodeStandardQueryChangesRequest(new JmapQueryChangesRequest
        {
            AccountId = "a1",
            SinceQueryState = "q1"
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(protocol.TryGetProperty(JmapProperties.CreatedIds, out _), Is.False);
            Assert.That(response.TryGetProperty(JmapProperties.CreatedIds, out _), Is.False);
            Assert.That(problemJson.TryGetProperty(JmapProperties.Status, out _), Is.False);
            Assert.That(get.TryGetProperty(JmapProperties.Ids, out _), Is.False);
            Assert.That(get.TryGetProperty(JmapProperties.Properties, out _), Is.False);
            Assert.That(changes.TryGetProperty(JmapProperties.MaxChanges, out _), Is.False);
            Assert.That(changesNull.GetProperty(JmapProperties.MaxChanges).ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(set.TryGetProperty(JmapProperties.Create, out _), Is.False);
            Assert.That(set.TryGetProperty(JmapProperties.Update, out _), Is.False);
            Assert.That(set.TryGetProperty(JmapProperties.Destroy, out _), Is.False);
            Assert.That(copy.TryGetProperty(JmapProperties.OnSuccessDestroyOriginal, out _), Is.False);
            Assert.That(query.TryGetProperty(JmapProperties.Position, out _), Is.False);
            Assert.That(query.TryGetProperty(JmapProperties.Limit, out _), Is.False);
            Assert.That(query.TryGetProperty(JmapProperties.CalculateTotal, out _), Is.False);
            Assert.That(queryChanges.TryGetProperty(JmapProperties.MaxChanges, out _), Is.False);
            Assert.That(queryChanges.TryGetProperty(JmapProperties.CalculateTotal, out _), Is.False);
        }
    }

    [TestCaseSource(nameof(MalformedIJsonCases))]
    public void PublicCodecRejectsMalformedIJson(string name, byte[] utf8)
    {
        IJmapCodec codec = new JmapJsonCodec();

        Assert.That(() => codec.DecodeRequestBytes(utf8),
            Throws.TypeOf<JmapProtocolException>(), name);
    }

    [Test]
    public void SafeIntegerBoundariesAreAcceptedAndValuesBeyondThemAreRejected()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapQueryRequest boundary = new JmapQueryRequest
        {
            AccountId = "a1",
            Position = JmapProtocolLimits.MinimumInt,
            HasPosition = true,
            AnchorOffset = JmapProtocolLimits.MaximumInt,
            HasAnchorOffset = true,
            Limit = JmapProtocolLimits.MaximumUnsignedInt,
            HasLimit = true
        };
        JsonElement encoded = codec.EncodeStandardQueryRequest(boundary);
        JmapQueryResponse decoded = codec.DecodeStandardQueryResponse(Json(
            "{\"accountId\":\"a1\",\"queryState\":\"q1\",\"canCalculateChanges\":false," +
            "\"position\":9007199254740991,\"ids\":[],\"total\":9007199254740991," +
            "\"limit\":9007199254740991}"));

        JmapQueryRequest tooLow = new JmapQueryRequest
        {
            AccountId = "a1",
            Position = JmapProtocolLimits.MinimumInt - 1L,
            HasPosition = true
        };
        JmapQueryRequest tooHigh = new JmapQueryRequest
        {
            AccountId = "a1",
            Limit = JmapProtocolLimits.MaximumUnsignedInt + 1L,
            HasLimit = true
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encoded.GetProperty(JmapProperties.Position).GetInt64(),
                Is.EqualTo(JmapProtocolLimits.MinimumInt));
            Assert.That(encoded.GetProperty(JmapProperties.AnchorOffset).GetInt64(),
                Is.EqualTo(JmapProtocolLimits.MaximumInt));
            Assert.That(encoded.GetProperty(JmapProperties.Limit).GetInt64(),
                Is.EqualTo(JmapProtocolLimits.MaximumUnsignedInt));
            Assert.That(decoded.Position, Is.EqualTo(JmapProtocolLimits.MaximumInt));
            Assert.That(decoded.Total, Is.EqualTo(JmapProtocolLimits.MaximumUnsignedInt));
            Assert.That(decoded.Limit, Is.EqualTo(JmapProtocolLimits.MaximumUnsignedInt));
            Assert.That(() => codec.EncodeStandardQueryRequest(tooLow),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.EncodeStandardQueryRequest(tooHigh),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.DecodeStandardQueryResponse(Json(
                "{\"accountId\":\"a1\",\"queryState\":\"q1\",\"canCalculateChanges\":false," +
                "\"position\":0,\"ids\":[],\"total\":9007199254740992}")),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void StandardMethodOperationsPreserveCompletePublicRequestAndResponseModels()
    {
        IJmapCodec codec = new JmapJsonCodec();

        JmapGetRequest getRequest = new JmapGetRequest
        {
            AccountId = "a1",
            Ids = new List<string> { "o1" },
            Properties = new List<string> { "id", "name" }
        };
        getRequest.UnknownProperties["vendorGet"] = Json("true");
        JsonElement get = codec.EncodeStandardGetRequest(getRequest);
        JmapGetResponse getResponse = codec.DecodeStandardGetResponse(Json(
            "{\"accountId\":\"a1\",\"state\":\"s1\",\"list\":[{\"id\":\"o1\"}]," +
            "\"notFound\":[\"missing\"],\"vendorGetResult\":17}"));

        JmapChangesRequest changesRequest = new JmapChangesRequest
        {
            AccountId = "a1",
            SinceState = "s1",
            MaxChanges = 7,
            HasMaxChanges = true
        };
        changesRequest.UnknownProperties["vendorChanges"] = Json("false");
        JsonElement changes = codec.EncodeStandardChangesRequest(changesRequest);
        JmapChangesResponse changesResponse = codec.DecodeStandardChangesResponse(Json(
            "{\"accountId\":\"a1\",\"oldState\":\"s1\",\"newState\":\"s2\"," +
            "\"hasMoreChanges\":true,\"created\":[\"c1\"],\"updated\":[\"u1\"]," +
            "\"destroyed\":[\"d1\"],\"vendorChangesResult\":null}"));

        JmapPatchObject patch = new JmapPatchObject();
        patch.Values["name"] = Json("\"updated\"");
        patch.Values["parentId"] = Json("null");
        JmapSetRequest setRequest = new JmapSetRequest
        {
            AccountId = "a1",
            IfInState = "s1",
            Create = new Dictionary<string, JsonElement> { ["new"] = Json("{\"name\":\"created\"}") },
            Update = new Dictionary<string, JmapPatchObject> { ["o1"] = patch },
            Destroy = new List<string> { "o2" }
        };
        setRequest.UnknownProperties["vendorSet"] = Json("[1]");
        JsonElement set = codec.EncodeStandardSetRequest(setRequest);
        JmapSetResponse setResponse = codec.DecodeStandardSetResponse(Json(
            "{\"accountId\":\"a1\",\"oldState\":\"s1\",\"newState\":\"s2\"," +
            "\"created\":{\"new\":{\"id\":\"o3\"}},\"updated\":{\"o1\":null}," +
            "\"destroyed\":[\"o2\"],\"notCreated\":{\"bad\":{\"type\":\"invalidProperties\",\"properties\":[\"name\"]}}," +
            "\"notUpdated\":{\"gone\":{\"type\":\"notFound\"}}," +
            "\"notDestroyed\":{\"locked\":{\"type\":\"forbidden\"}},\"vendorSetResult\":true}"));

        JmapCopyRequest copyRequest = new JmapCopyRequest
        {
            FromAccountId = "a1",
            IfFromInState = "source-1",
            AccountId = "a2",
            IfInState = "target-1",
            OnSuccessDestroyOriginal = true,
            HasOnSuccessDestroyOriginal = true,
            DestroyFromIfInState = "source-1"
        };
        copyRequest.Create["copy"] = Json("{\"id\":\"o1\",\"name\":\"copy\"}");
        copyRequest.UnknownProperties["vendorCopy"] = Json("17");
        JsonElement copy = codec.EncodeStandardCopyRequest(copyRequest);
        JmapCopyResponse copyResponse = codec.DecodeStandardCopyResponse(Json(
            "{\"fromAccountId\":\"a1\",\"accountId\":\"a2\",\"oldState\":\"target-1\"," +
            "\"newState\":\"target-2\",\"created\":{\"copy\":{\"id\":\"o9\"}}," +
            "\"notCreated\":{\"bad\":{\"type\":\"alreadyExists\",\"existingId\":\"o8\"}}," +
            "\"vendorCopyResult\":{\"ok\":true}}"));

        JmapFilterNode child = new JmapFilterNode();
        child.FilterProperties["name"] = Json("\"Inbox\"");
        JmapFilterNode filter = new JmapFilterNode
        {
            Operator = JmapFilterOperator.And,
            HasOperator = true,
            Conditions = new List<JmapFilterNode> { child }
        };
        JmapComparator comparator = new JmapComparator
        {
            Property = "name",
            IsAscending = false,
            HasIsAscending = true,
            Collation = "i;unicode-casemap"
        };
        comparator.UnknownProperties["vendorComparator"] = Json("\"value\"");
        JmapQueryRequest queryRequest = new JmapQueryRequest
        {
            AccountId = "a1",
            Filter = filter,
            Sort = new List<JmapComparator> { comparator },
            Position = -2,
            HasPosition = true,
            Anchor = "o1",
            AnchorOffset = 1,
            HasAnchorOffset = true,
            Limit = 10,
            HasLimit = true,
            CalculateTotal = true,
            HasCalculateTotal = true
        };
        queryRequest.UnknownProperties["vendorQuery"] = Json("true");
        JsonElement query = codec.EncodeStandardQueryRequest(queryRequest);
        JmapQueryResponse queryResponse = codec.DecodeStandardQueryResponse(Json(
            "{\"accountId\":\"a1\",\"queryState\":\"q1\",\"canCalculateChanges\":true," +
            "\"position\":2,\"ids\":[\"o1\",\"o2\"],\"total\":9,\"limit\":2," +
            "\"vendorQueryResult\":\"kept\"}"));

        JmapQueryChangesRequest queryChangesRequest = new JmapQueryChangesRequest
        {
            AccountId = "a1",
            Filter = filter,
            Sort = new List<JmapComparator> { comparator },
            SinceQueryState = "q1",
            MaxChanges = 5,
            HasMaxChanges = true,
            UpToId = "o9",
            CalculateTotal = true,
            HasCalculateTotal = true
        };
        queryChangesRequest.UnknownProperties["vendorQueryChanges"] = Json("null");
        JsonElement queryChanges = codec.EncodeStandardQueryChangesRequest(queryChangesRequest);
        JmapQueryChangesResponse queryChangesResponse = codec.DecodeStandardQueryChangesResponse(Json(
            "{\"accountId\":\"a1\",\"oldQueryState\":\"q1\",\"newQueryState\":\"q2\"," +
            "\"total\":8,\"removed\":[\"o1\"],\"added\":[{\"id\":\"o3\",\"index\":1," +
            "\"vendorAdded\":true}],\"vendorQueryChangesResult\":17}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(get.GetProperty(JmapProperties.Ids)[0].GetString(), Is.EqualTo("o1"));
            Assert.That(get.GetProperty("vendorGet").GetBoolean(), Is.True);
            Assert.That(getResponse.List[0].GetProperty(JmapProperties.Id).GetString(), Is.EqualTo("o1"));
            Assert.That(getResponse.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(getResponse.UnknownProperties["vendorGetResult"].GetInt32(), Is.EqualTo(17));

            Assert.That(changes.GetProperty(JmapProperties.MaxChanges).GetInt64(), Is.EqualTo(7));
            Assert.That(changes.GetProperty("vendorChanges").GetBoolean(), Is.False);
            Assert.That(changesResponse.HasMoreChanges, Is.True);
            Assert.That(changesResponse.Created, Is.EqualTo(new[] { "c1" }));
            Assert.That(changesResponse.Updated, Is.EqualTo(new[] { "u1" }));
            Assert.That(changesResponse.Destroyed, Is.EqualTo(new[] { "d1" }));
            Assert.That(changesResponse.UnknownProperties["vendorChangesResult"].ValueKind,
                Is.EqualTo(JsonValueKind.Null));

            Assert.That(set.GetProperty(JmapProperties.IfInState).GetString(), Is.EqualTo("s1"));
            Assert.That(set.GetProperty(JmapProperties.Create).GetProperty("new")
                .GetProperty(JmapProperties.Name).GetString(), Is.EqualTo("created"));
            Assert.That(set.GetProperty(JmapProperties.Update).GetProperty("o1")
                .GetProperty(JmapProperties.ParentId).ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(set.GetProperty(JmapProperties.Destroy)[0].GetString(), Is.EqualTo("o2"));
            Assert.That(setResponse.Created!["new"].GetProperty(JmapProperties.Id).GetString(),
                Is.EqualTo("o3"));
            Assert.That(setResponse.Updated!["o1"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(setResponse.NotCreated!["bad"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(setResponse.NotUpdated!["gone"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(setResponse.NotDestroyed!["locked"].Kind,
                Is.EqualTo(JmapSetErrorKind.Forbidden));
            Assert.That(setResponse.UnknownProperties["vendorSetResult"].GetBoolean(), Is.True);

            Assert.That(copy.GetProperty(JmapProperties.IfFromInState).GetString(), Is.EqualTo("source-1"));
            Assert.That(copy.GetProperty(JmapProperties.OnSuccessDestroyOriginal).GetBoolean(), Is.True);
            Assert.That(copy.GetProperty(JmapProperties.DestroyFromIfInState).GetString(), Is.EqualTo("source-1"));
            Assert.That(copyResponse.Created!["copy"].GetProperty(JmapProperties.Id).GetString(),
                Is.EqualTo("o9"));
            Assert.That(copyResponse.NotCreated!["bad"].ExistingId, Is.EqualTo("o8"));
            Assert.That(copyResponse.UnknownProperties["vendorCopyResult"]
                .GetProperty("ok").GetBoolean(), Is.True);

            Assert.That(query.GetProperty(JmapProperties.Filter)
                .GetProperty(JmapProperties.Conditions)[0].GetProperty(JmapProperties.Name).GetString(),
                Is.EqualTo("Inbox"));
            Assert.That(query.GetProperty(JmapProperties.Sort)[0]
                .GetProperty("vendorComparator").GetString(), Is.EqualTo("value"));
            Assert.That(query.GetProperty(JmapProperties.Position).GetInt64(), Is.EqualTo(-2));
            Assert.That(query.GetProperty(JmapProperties.CalculateTotal).GetBoolean(), Is.True);
            Assert.That(queryResponse.Ids, Is.EqualTo(new[] { "o1", "o2" }));
            Assert.That(queryResponse.HasTotal, Is.True);
            Assert.That(queryResponse.Total, Is.EqualTo(9));
            Assert.That(queryResponse.HasLimit, Is.True);
            Assert.That(queryResponse.Limit, Is.EqualTo(2));
            Assert.That(queryResponse.UnknownProperties["vendorQueryResult"].GetString(),
                Is.EqualTo("kept"));

            Assert.That(queryChanges.GetProperty(JmapProperties.SinceQueryState).GetString(),
                Is.EqualTo("q1"));
            Assert.That(queryChanges.GetProperty(JmapProperties.MaxChanges).GetInt64(), Is.EqualTo(5));
            Assert.That(queryChanges.GetProperty(JmapProperties.UpToId).GetString(), Is.EqualTo("o9"));
            Assert.That(queryChangesResponse.Total, Is.EqualTo(8));
            Assert.That(queryChangesResponse.Removed, Is.EqualTo(new[] { "o1" }));
            Assert.That(queryChangesResponse.Added[0].Id, Is.EqualTo("o3"));
            Assert.That(queryChangesResponse.Added[0].Index, Is.EqualTo(1));
            Assert.That(queryChangesResponse.Added[0].UnknownProperties["vendorAdded"].GetBoolean(),
                Is.True);
            Assert.That(queryChangesResponse.UnknownProperties["vendorQueryChangesResult"].GetInt32(),
                Is.EqualTo(17));
        }
    }

    [Test]
    public void ProblemErrorStatePushAndAddressDtosRoundTripEveryStructuredField()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapProblem problem = new JmapProblem
        {
            Type = "https://vendor.example/problem",
            Title = "Vendor problem",
            Status = 409,
            HasStatus = true,
            Detail = "Conflict",
            Instance = "urn:request:1",
            Limit = "vendorLimit"
        };
        problem.UnknownProperties["retryAfter"] = Json("30");
        JmapProblem decodedProblem = codec.DecodeProblemBytes(codec.EncodeProblemBytes(problem));

        JmapMethodError methodError = new JmapMethodError
        {
            Type = "vendorMethodError",
            Description = "Method failed"
        };
        methodError.UnknownProperties["code"] = Json("17");
        JmapMethodError decodedMethod = codec.DecodeMethodErrorBytes(
            codec.EncodeMethodErrorBytes(methodError));

        JmapSetError setError = new JmapSetError
        {
            Type = JmapErrorCodes.InvalidRecipients,
            Description = "Invalid recipients",
            Properties = new List<string> { JmapProperties.Envelope },
            ExistingId = "existing",
            MaxSize = 1000,
            HasMaxSize = true,
            MaxRecipients = 10,
            HasMaxRecipients = true,
            InvalidRecipients = new List<string> { "bad@example.test" },
            NotFound = new List<string> { "blob-1" }
        };
        setError.UnknownProperties["vendor"] = Json("true");
        JmapSetError decodedSet = codec.DecodeSetErrorBytes(codec.EncodeSetErrorBytes(setError));

        StateChange state = new StateChange();
        state.Changed["a1"] = new Dictionary<string, string> { ["VendorData"] = "v2" };
        state.UnknownProperties["vendorState"] = Json("{\"sequence\":2}");
        StateChange decodedState = codec.DecodeStateChangeBytes(codec.EncodeStateChangeBytes(state));

        PushVerification push = new PushVerification
        {
            PushSubscriptionId = "p1",
            VerificationCode = "secret"
        };
        push.UnknownProperties["vendorPush"] = Json("null");
        PushVerification decodedPush = codec.DecodePushVerificationBytes(
            codec.EncodePushVerificationBytes(push));

        List<EmailAddressGroup> groups = codec.DecodeEmailAddressGroups(Json(
            "[{\"name\":null,\"addresses\":[{\"name\":\"Zoë\",\"email\":\"zoë@example.test\"," +
            "\"vendorAddress\":true}],\"vendorGroup\":17}]"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decodedProblem.Type, Is.EqualTo("https://vendor.example/problem"));
            Assert.That(decodedProblem.Kind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(decodedProblem.Title, Is.EqualTo("Vendor problem"));
            Assert.That(decodedProblem.Status, Is.EqualTo(409));
            Assert.That(decodedProblem.HasStatus, Is.True);
            Assert.That(decodedProblem.Detail, Is.EqualTo("Conflict"));
            Assert.That(decodedProblem.Instance, Is.EqualTo("urn:request:1"));
            Assert.That(decodedProblem.Limit, Is.EqualTo("vendorLimit"));
            Assert.That(decodedProblem.UnknownProperties["retryAfter"].GetInt32(), Is.EqualTo(30));

            Assert.That(decodedMethod.Type, Is.EqualTo("vendorMethodError"));
            Assert.That(decodedMethod.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(decodedMethod.Description, Is.EqualTo("Method failed"));
            Assert.That(decodedMethod.UnknownProperties["code"].GetInt32(), Is.EqualTo(17));

            Assert.That(decodedSet.Kind, Is.EqualTo(JmapSetErrorKind.InvalidRecipients));
            Assert.That(decodedSet.Properties, Is.EqualTo(new[] { JmapProperties.Envelope }));
            Assert.That(decodedSet.ExistingId, Is.EqualTo("existing"));
            Assert.That(decodedSet.MaxSize, Is.EqualTo(1000));
            Assert.That(decodedSet.HasMaxSize, Is.True);
            Assert.That(decodedSet.MaxRecipients, Is.EqualTo(10));
            Assert.That(decodedSet.HasMaxRecipients, Is.True);
            Assert.That(decodedSet.InvalidRecipients, Is.EqualTo(new[] { "bad@example.test" }));
            Assert.That(decodedSet.NotFound, Is.EqualTo(new[] { "blob-1" }));
            Assert.That(decodedSet.UnknownProperties["vendor"].GetBoolean(), Is.True);

            Assert.That(decodedState.Type, Is.EqualTo("StateChange"));
            Assert.That(decodedState.Changed["a1"]["VendorData"], Is.EqualTo("v2"));
            Assert.That(decodedState.UnknownProperties["vendorState"]
                .GetProperty("sequence").GetInt32(), Is.EqualTo(2));
            Assert.That(decodedPush.Type, Is.EqualTo("PushVerification"));
            Assert.That(decodedPush.PushSubscriptionId, Is.EqualTo("p1"));
            Assert.That(decodedPush.VerificationCode, Is.EqualTo("secret"));
            Assert.That(decodedPush.UnknownProperties["vendorPush"].ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(groups[0].Name, Is.Null);
            Assert.That(groups[0].Addresses[0].Name, Is.EqualTo("Zoë"));
            Assert.That(groups[0].Addresses[0].Email, Is.EqualTo("zoë@example.test"));
            Assert.That(groups[0].Addresses[0].UnknownProperties["vendorAddress"].GetBoolean(), Is.True);
            Assert.That(groups[0].UnknownProperties["vendorGroup"].GetInt32(), Is.EqualTo(17));
        }
    }

    private static IEnumerable<TestCaseData> MalformedIJsonCases()
    {
        yield return new TestCaseData("invalid UTF-8", AdversarialFixtures.InvalidUtf8Json());
        yield return new TestCaseData("duplicate member", AdversarialFixtures.DuplicateMemberJson());
        yield return new TestCaseData("escaped noncharacter", Encoding.UTF8.GetBytes(
            "{\"using\":[],\"methodCalls\":[],\"x\":\"\\uFDD0\"}"));
        yield return new TestCaseData("unpaired surrogate", Encoding.UTF8.GetBytes(
            "{\"using\":[],\"methodCalls\":[],\"x\":\"\\uD800\"}"));
        yield return new TestCaseData("unsafe integer", Encoding.UTF8.GetBytes(
            "{\"using\":[],\"methodCalls\":[],\"x\":9007199254740992}"));
    }

    private static JsonElement Json(string json)
    {
        return PublicApiTestHelper.Json(json);
    }

    private static JsonElement Parse(byte[] utf8)
    {
        using JsonDocument document = JsonDocument.Parse(utf8);
        return document.RootElement.Clone();
    }
}
