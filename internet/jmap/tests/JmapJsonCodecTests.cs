using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class JmapJsonCodecTests
{
    [Test]
    public async Task SessionSupportsBytesMemoryAndOpenStreams()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapSession session = MinimalSession();

        byte[] bytes = codec.EncodeSessionBytes(session);
        JmapSession fromBytes = codec.DecodeSessionBytes(bytes);
        JmapSession fromMemory = codec.DecodeSessionMemory(bytes.AsMemory());
        using MemoryStream encodedStream = new MemoryStream();
        await codec.EncodeSessionStreamAsync(session, encodedStream, CancellationToken.None);
        encodedStream.Position = 0;
        JmapSession fromStream = await codec.DecodeSessionStreamAsync(encodedStream, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fromBytes.Username, Is.EqualTo("user@example.test"));
            Assert.That(fromMemory.State, Is.EqualTo("session-1"));
            Assert.That(fromStream.ApiUrl, Is.EqualTo("https://jmap.example.test/api"));
            Assert.That(encodedStream.CanRead, Is.True);
            Assert.That(encodedStream.CanWrite, Is.True);
            Assert.That(encodedStream.Length, Is.EqualTo(bytes.Length));
        }
    }

    [Test]
    public async Task RequestAndResponseRoundTripEveryPublicTransportShape()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapRequest request = new JmapRequest();
        request.Using.Add(JmapCapabilities.Core);
        request.MethodCalls.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json("{\"answer\":42}"),
            CallId = "c1"
        });
        request.CreatedIds = new Dictionary<string, string> { ["draft"] = "email-1" };

        byte[] requestBytes = codec.EncodeRequestBytes(request);
        JmapRequest fromBytes = codec.DecodeRequestBytes(requestBytes);
        JmapRequest fromMemory = codec.DecodeRequestMemory(requestBytes.AsMemory());
        using FragmentedReadStream requestStream = new FragmentedReadStream(requestBytes, 1);
        JmapRequest fromStream = await codec.DecodeRequestStreamAsync(requestStream, CancellationToken.None);

        JmapResponse response = new JmapResponse { SessionState = "session-2" };
        response.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.CoreEcho,
            Arguments = Json("{\"answer\":42}"),
            CallId = "c1"
        });
        byte[] responseBytes = codec.EncodeResponseBytes(response);
        JmapResponse responseFromBytes = codec.DecodeResponseBytes(responseBytes);
        JmapResponse responseFromMemory = codec.DecodeResponseMemory(responseBytes.AsMemory());
        using MemoryStream responseStream = new MemoryStream();
        await codec.EncodeResponseStreamAsync(response, responseStream, CancellationToken.None);
        responseStream.Position = 0;
        JmapResponse responseFromStream = await codec.DecodeResponseStreamAsync(responseStream, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fromBytes.MethodCalls[0].Arguments.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(fromMemory.CreatedIds!["draft"], Is.EqualTo("email-1"));
            Assert.That(fromStream.MethodCalls[0].CallId, Is.EqualTo("c1"));
            Assert.That(requestStream.CanRead, Is.True);
            Assert.That(responseFromBytes.SessionState, Is.EqualTo("session-2"));
            Assert.That(responseFromMemory.MethodResponses[0].Name, Is.EqualTo(JmapMethods.CoreEcho));
            Assert.That(responseFromStream.MethodResponses[0].Arguments.GetProperty("answer").GetInt32(), Is.EqualTo(42));
            Assert.That(responseStream.CanWrite, Is.True);
        }
    }

    [Test]
    public async Task ProblemDetailsSupportAllInputFormsAndKeepUnknownDataDetached()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapProblem problem = new JmapProblem
        {
            Type = JmapErrorCodes.LimitProblemType,
            Title = "Limit exceeded",
            Status = 400,
            HasStatus = true,
            Limit = JmapProperties.MaxSizeRequest
        };
        problem.UnknownProperties["retry"] = Json("30");

        byte[] bytes = codec.EncodeProblemBytes(problem);
        JmapProblem fromBytes = codec.DecodeProblemBytes(bytes);
        JmapProblem fromMemory = codec.DecodeProblemMemory(bytes.AsMemory());
        using MemoryStream stream = new MemoryStream();
        await codec.EncodeProblemStreamAsync(problem, stream, CancellationToken.None);
        stream.Position = 0;
        JmapProblem fromStream = await codec.DecodeProblemStreamAsync(stream, CancellationToken.None);
        Array.Fill(bytes, (byte)32);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fromBytes.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(fromMemory.UnknownProperties["retry"].GetInt32(), Is.EqualTo(30));
            Assert.That(fromStream.HasStatus, Is.True);
            Assert.That(fromStream.Status, Is.EqualTo(400));
            Assert.That(stream.CanRead, Is.True);
        }
    }

    [Test]
    public void ErrorObjectsUseThePublicByteFacade()
    {
        IJmapCodec codec = new JmapJsonCodec();
        JmapMethodError methodError = new JmapMethodError
        {
            Type = JmapErrorCodes.AccountReadOnly,
            Description = "Read only"
        };
        JmapSetError setError = new JmapSetError
        {
            Type = JmapErrorCodes.InvalidProperties,
            Properties = new List<string> { "emailId", "identityId" }
        };

        JmapMethodError decodedMethod = codec.DecodeMethodErrorBytes(codec.EncodeMethodErrorBytes(methodError));
        JmapSetError decodedSet = codec.DecodeSetErrorBytes(codec.EncodeSetErrorBytes(setError));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decodedMethod.Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(decodedMethod.Description, Is.EqualTo("Read only"));
            Assert.That(decodedSet.Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(decodedSet.Properties, Is.EqualTo(new[] { "emailId", "identityId" }));
        }
    }

    [Test]
    public async Task StateChangeSupportsBytesMemoryAndOpenStreams()
    {
        IJmapCodec codec = new JmapJsonCodec();
        StateChange change = new StateChange();
        change.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "email-state",
            [JmapDataTypes.Mailbox] = "mailbox-state"
        };

        byte[] bytes = codec.EncodeStateChangeBytes(change);
        StateChange fromBytes = codec.DecodeStateChangeBytes(bytes);
        StateChange fromMemory = codec.DecodeStateChangeMemory(bytes.AsMemory());
        using MemoryStream stream = new MemoryStream();
        await codec.EncodeStateChangeStreamAsync(change, stream, CancellationToken.None);
        stream.Position = 0;
        StateChange fromStream = await codec.DecodeStateChangeStreamAsync(stream, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fromBytes.Type, Is.EqualTo("StateChange"));
            Assert.That(fromMemory.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-state"));
            Assert.That(fromStream.Changed["account-1"][JmapDataTypes.Mailbox], Is.EqualTo("mailbox-state"));
            Assert.That(stream.CanWrite, Is.True);
        }
    }

    [Test]
    public void StandardMethodFacadeEncodesRequestsAndDecodesResponses()
    {
        IJmapCodec codec = new JmapJsonCodec();

        JsonElement get = codec.EncodeStandardGetRequest(new JmapGetRequest { AccountId = "a" });
        JmapGetResponse getResponse = codec.DecodeStandardGetResponse(Json("{\"accountId\":\"a\",\"state\":\"s\",\"list\":[],\"notFound\":[]}"));

        JsonElement changes = codec.EncodeStandardChangesRequest(new JmapChangesRequest { AccountId = "a", SinceState = "s" });
        JmapChangesResponse changesResponse = codec.DecodeStandardChangesResponse(Json("{\"accountId\":\"a\",\"oldState\":\"s1\",\"newState\":\"s2\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[],\"destroyed\":[]}"));

        JsonElement set = codec.EncodeStandardSetRequest(new JmapSetRequest { AccountId = "a" });
        JmapSetResponse setResponse = codec.DecodeStandardSetResponse(Json("{\"accountId\":\"a\",\"oldState\":\"s1\",\"newState\":\"s2\"}"));

        JmapCopyRequest copyRequest = new JmapCopyRequest { FromAccountId = "a", AccountId = "b" };
        JsonElement copy = codec.EncodeStandardCopyRequest(copyRequest);
        JmapCopyResponse copyResponse = codec.DecodeStandardCopyResponse(Json("{\"fromAccountId\":\"a\",\"accountId\":\"b\",\"oldState\":\"s1\",\"newState\":\"s2\"}"));

        JsonElement query = codec.EncodeStandardQueryRequest(new JmapQueryRequest { AccountId = "a" });
        JmapQueryResponse queryResponse = codec.DecodeStandardQueryResponse(Json("{\"accountId\":\"a\",\"queryState\":\"q1\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[]}"));

        JsonElement queryChanges = codec.EncodeStandardQueryChangesRequest(new JmapQueryChangesRequest { AccountId = "a", SinceQueryState = "q1" });
        JmapQueryChangesResponse queryChangesResponse = codec.DecodeStandardQueryChangesResponse(Json("{\"accountId\":\"a\",\"oldQueryState\":\"q1\",\"newQueryState\":\"q2\",\"removed\":[],\"added\":[]}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(get.GetProperty("accountId").GetString(), Is.EqualTo("a"));
            Assert.That(getResponse.State, Is.EqualTo("s"));
            Assert.That(changes.GetProperty("sinceState").GetString(), Is.EqualTo("s"));
            Assert.That(changesResponse.NewState, Is.EqualTo("s2"));
            Assert.That(set.GetProperty("accountId").GetString(), Is.EqualTo("a"));
            Assert.That(setResponse.OldState, Is.EqualTo("s1"));
            Assert.That(copy.GetProperty("fromAccountId").GetString(), Is.EqualTo("a"));
            Assert.That(copyResponse.AccountId, Is.EqualTo("b"));
            Assert.That(query.GetProperty("accountId").GetString(), Is.EqualTo("a"));
            Assert.That(queryResponse.CanCalculateChanges, Is.True);
            Assert.That(queryChanges.GetProperty("sinceQueryState").GetString(), Is.EqualTo("q1"));
            Assert.That(queryChangesResponse.NewQueryState, Is.EqualTo("q2"));
        }
    }

    [Test]
    public void PushVerificationAddressGroupsAndClassifiersAreExposed()
    {
        IJmapCodec codec = new JmapJsonCodec();
        PushVerification verification = new PushVerification
        {
            PushSubscriptionId = "push-1",
            VerificationCode = "secret"
        };
        PushVerification decoded = codec.DecodePushVerificationBytes(codec.EncodePushVerificationBytes(verification));
        List<EmailAddressGroup> groups = codec.DecodeEmailAddressGroups(Json("[{\"name\":\"Team\",\"addresses\":[{\"name\":\"A\",\"email\":\"a@example.test\"}]}]"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(decoded.Type, Is.EqualTo("PushVerification"));
            Assert.That(decoded.PushSubscriptionId, Is.EqualTo("push-1"));
            Assert.That(groups[0].Addresses[0].Email, Is.EqualTo("a@example.test"));
            Assert.That(codec.ClassifyProblemType(JmapErrorCodes.NotJsonProblemType), Is.EqualTo(JmapProblemKind.NotJson));
            Assert.That(codec.ClassifyMethodErrorType(JmapErrorCodes.ServerFail), Is.EqualTo(JmapMethodErrorKind.ServerFail));
            Assert.That(codec.ClassifySetErrorType(JmapErrorCodes.TooLarge), Is.EqualTo(JmapSetErrorKind.TooLarge));
            Assert.That(codec.ClassifyFilterOperator("AND"), Is.EqualTo(JmapFilterOperator.And));
            Assert.That(codec.ClassifyHeaderForm("asGroupedAddresses"), Is.EqualTo(JmapHeaderForm.GroupedAddresses));
            Assert.That(codec.ClassifyEventName("state"), Is.EqualTo(JmapEventKind.State));
            Assert.That(codec.ClassifyEventName("future"), Is.EqualTo(JmapEventKind.Unknown));
            Assert.That(codec.ClassifySubmissionUndoStatus("canceled"), Is.EqualTo(JmapSubmissionUndoStatus.Canceled));
        }
    }

    [Test]
    public void PublicFacadePreservesReaderValidationAndEncoderValidation()
    {
        IJmapCodec codec = new JmapJsonCodec();
        byte[] duplicate = AdversarialFixtures.DuplicateMemberJson();
        JmapResponse missingState = new JmapResponse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => codec.DecodeRequestBytes(duplicate), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => codec.EncodeResponseBytes(missingState), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.ClassifyFilterOperator("XOR"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => codec.ClassifyHeaderForm("asFuture"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.ClassifySubmissionUndoStatus("recalled"), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void CanceledStreamOperationsDoNotReadOrWrite()
    {
        IJmapCodec codec = new JmapJsonCodec();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using MemoryStream input = new MemoryStream(codec.EncodeSessionBytes(MinimalSession()));
        using MemoryStream output = new MemoryStream();

        Task<JmapSession> read = codec.DecodeSessionStreamAsync(input, cancellation.Token);
        Task write = codec.EncodeSessionStreamAsync(MinimalSession(), output, cancellation.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(async () => await read, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(async () => await write, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(input.Position, Is.Zero);
            Assert.That(output.Length, Is.Zero);
        }
    }

    private static JmapSession MinimalSession()
    {
        return new JmapSession
        {
            Username = "user@example.test",
            ApiUrl = "https://jmap.example.test/api",
            DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}",
            UploadUrl = "https://jmap.example.test/upload/{accountId}",
            EventSourceUrl = "https://jmap.example.test/events/{types}",
            State = "session-1"
        };
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
