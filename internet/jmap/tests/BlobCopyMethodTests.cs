using System.Text.Json;

namespace jmap.Tests;

public class BlobCopyMethodTests
{
    [Test]
    public async Task DispatchChecksAccountsAndMapsMixedBackendPartitions()
    {
        BlobCopyRequest request = Request("source", "target", "b1", "b2", "b3");
        request.UnknownProperties.Add("vendorMode", Json("\"fast\""));
        FakeBlobService blobs = new FakeBlobService();
        blobs.CopyResponse = Response("source", "target");
        blobs.CopyResponse.Copied = new Dictionary<string, string> { { "b1", "new1" } };
        blobs.CopyResponse.NotCopied = new Dictionary<string, JmapSetError>
        {
            { "b2", SetError(JmapErrorCodes.NotFound, "Missing source blob") },
            { "b3", SetError(JmapErrorCodes.OverQuota, "Target quota exceeded") }
        };
        blobs.CopyResponse.UnknownProperties.Add("vendorTrace", Json("7"));
        JmapRequestContext context = new JmapRequestContext();
        context.UserId = "user_1";
        BlobCopyMethod method = new BlobCopyMethod();

        JmapInvocation invocation = await method.ProcessInvocationAsync(
            Invocation(request, "copy-1"),
            Session(true, true, true, false),
            blobs,
            context,
            CancellationToken.None);
        BlobCopyResponse decoded = method.Codec.DecodeBlobCopyResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.BlobCopy));
            Assert.That(invocation.CallId, Is.EqualTo("copy-1"));
            Assert.That(decoded.FromAccountId, Is.EqualTo("source"));
            Assert.That(decoded.AccountId, Is.EqualTo("target"));
            Assert.That(decoded.Copied!["b1"], Is.EqualTo("new1"));
            Assert.That(decoded.NotCopied!["b2"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(decoded.NotCopied["b3"].Kind, Is.EqualTo(JmapSetErrorKind.OverQuota));
            Assert.That(decoded.UnknownProperties["vendorTrace"].GetInt32(), Is.EqualTo(7));
            Assert.That(blobs.CopyCalls, Is.EqualTo(1));
            Assert.That(blobs.LastCopyContext, Is.SameAs(context));
            Assert.That(blobs.LastCopyRequest!.UnknownProperties["vendorMode"].GetString(), Is.EqualTo("fast"));
        }
    }

    [Test]
    public void CrossAccountAccessCapabilityAndReadOnlyFailuresUseMethodErrors()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        BlobCopyRequest request = Request("source", "target", "b1");
        FakeBlobService blobs = new FakeBlobService();

        JmapInvocation missingSource = method.ProcessInvocation(
            Invocation(request, "c1"), Session(false, true, true, false), blobs, new JmapRequestContext(), CancellationToken.None);
        JmapInvocation missingTarget = method.ProcessInvocation(
            Invocation(request, "c2"), Session(true, false, true, false), blobs, new JmapRequestContext(), CancellationToken.None);
        JmapInvocation missingCore = method.ProcessInvocation(
            Invocation(request, "c3"), Session(true, true, false, false), blobs, new JmapRequestContext(), CancellationToken.None);
        JmapInvocation readOnly = method.ProcessInvocation(
            Invocation(request, "c4"), Session(true, true, true, true), blobs, new JmapRequestContext(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, missingSource).Kind, Is.EqualTo(JmapMethodErrorKind.FromAccountNotFound));
            Assert.That(Error(method, missingTarget).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, missingCore).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(Error(method, readOnly).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(blobs.CopyCalls, Is.Zero);
        }
    }

    [Test]
    public void InvalidArgumentsReturnInvalidArgumentsWithoutCallingBackend()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        FakeBlobService blobs = new FakeBlobService();
        BlobCopyRequest sameAccount = Request("source", "source", "b1");
        BlobCopyRequest duplicate = Request("source", "target", "b1", "b1");
        JmapInvocation wrongShape = new JmapInvocation();
        wrongShape.Name = JmapMethods.BlobCopy;
        wrongShape.CallId = "c3";
        wrongShape.Arguments = Json("{\"fromAccountId\":\"source\",\"accountId\":\"target\",\"blobIds\":[1]}");
        JmapSession session = Session(true, true, true, false);

        JmapInvocation same = method.ProcessInvocation(
            Invocation(sameAccount, "c1"), session, blobs, new JmapRequestContext(), CancellationToken.None);
        JmapInvocation duplicates = method.ProcessInvocation(
            Invocation(duplicate, "c2"), session, blobs, new JmapRequestContext(), CancellationToken.None);
        JmapInvocation malformed = method.ProcessInvocation(
            wrongShape, session, blobs, new JmapRequestContext(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, same).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, duplicates).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, malformed).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(blobs.CopyCalls, Is.Zero);
        }
    }

    [Test]
    public void EmptyBlobListProducesAValidNullPartitionResponse()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        BlobCopyRequest request = Request("source", "target");
        FakeBlobService blobs = new FakeBlobService();
        blobs.CopyResponse = Response("source", "target");

        JmapInvocation invocation = method.ProcessInvocation(
            Invocation(request, "empty"),
            Session(true, true, true, false),
            blobs,
            new JmapRequestContext(),
            CancellationToken.None);
        BlobCopyResponse response = method.Codec.DecodeBlobCopyResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Copied, Is.Null);
            Assert.That(response.NotCopied, Is.Null);
            Assert.That(blobs.CopyCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void BackendMustPartitionEveryBlobExactlyOnceWithAllowedCreateErrors()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        BlobCopyRequest request = Request("source", "target", "b1", "b2");
        FakeBlobService incomplete = new FakeBlobService();
        incomplete.CopyResponse = Response("source", "target");
        incomplete.CopyResponse.Copied = new Dictionary<string, string> { { "b1", "new1" } };
        FakeBlobService overlap = new FakeBlobService();
        overlap.CopyResponse = Response("source", "target");
        overlap.CopyResponse.Copied = new Dictionary<string, string> { { "b1", "new1" } };
        overlap.CopyResponse.NotCopied = new Dictionary<string, JmapSetError>
        {
            { "b1", SetError(JmapErrorCodes.NotFound, null) },
            { "b2", SetError(JmapErrorCodes.NotFound, null) }
        };
        FakeBlobService invalidError = new FakeBlobService();
        invalidError.CopyResponse = Response("source", "target");
        invalidError.CopyResponse.NotCopied = new Dictionary<string, JmapSetError>
        {
            { "b1", SetError(JmapErrorCodes.InvalidPatch, null) },
            { "b2", SetError(JmapErrorCodes.NotFound, null) }
        };
        JmapSession session = Session(true, true, true, false);

        JmapProtocolException incompleteError = Assert.Throws<JmapProtocolException>(() => method.ProcessInvocation(
            Invocation(request, "c1"), session, incomplete, new JmapRequestContext(), CancellationToken.None))!;
        JmapProtocolException overlapError = Assert.Throws<JmapProtocolException>(() => method.ProcessInvocation(
            Invocation(request, "c2"), session, overlap, new JmapRequestContext(), CancellationToken.None))!;
        JmapProtocolException typeError = Assert.Throws<JmapProtocolException>(() => method.ProcessInvocation(
            Invocation(request, "c3"), session, invalidError, new JmapRequestContext(), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(incompleteError.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(overlapError.Path, Is.EqualTo("notCopied"));
            Assert.That(typeError.Path, Is.EqualTo("notCopied/b1/type"));
        }
    }

    [Test]
    public void ClientMappingSelectsTypedResultAndValidatesCopiedIdsAndErrors()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        BlobCopyRequest request = Request("source", "target", "b1", "b2");
        JmapResponse wire = new JmapResponse();
        wire.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.EmailGet,
            CallId = "copy-1",
            Arguments = Json("{\"accountId\":\"target\",\"state\":\"s\",\"list\":[],\"notFound\":[]}")
        });
        wire.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.BlobCopy,
            CallId = "other",
            Arguments = Json("{\"fromAccountId\":\"source\",\"accountId\":\"target\",\"copied\":null,\"notCopied\":null}")
        });
        wire.MethodResponses.Add(new JmapInvocation
        {
            Name = JmapMethods.BlobCopy,
            CallId = "copy-1",
            Arguments = Json("{\"fromAccountId\":\"source\",\"accountId\":\"target\",\"copied\":{\"b1\":\"n1\"},\"notCopied\":{\"b2\":{\"type\":\"notFound\",\"vendor\":true}},\"trace\":9}")
        });

        BlobCopyResponse response = method.MapResponse(wire, "copy-1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Copied!["b1"], Is.EqualTo("n1"));
            Assert.That(response.NotCopied!["b2"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(response.NotCopied["b2"].UnknownProperties["vendor"].GetBoolean(), Is.True);
            Assert.That(response.UnknownProperties["trace"].GetInt32(), Is.EqualTo(9));
        }
    }

    [Test]
    public void ClientInvocationEncodingPreservesTypedArgumentsAndRejectsInvalidInputs()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        BlobCopyRequest request = Request("source", "target", "b1", "b2");
        request.UnknownProperties.Add("mode", Json("\"fast\""));

        JmapInvocation invocation = method.CreateInvocation(request, "copy-7");
        BlobCopyRequest decoded = method.Codec.DecodeBlobCopyRequest(invocation.Arguments);
        BlobCopyRequest sameAccount = Request("source", "source", "b1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.BlobCopy));
            Assert.That(invocation.CallId, Is.EqualTo("copy-7"));
            Assert.That(decoded.FromAccountId, Is.EqualTo("source"));
            Assert.That(decoded.AccountId, Is.EqualTo("target"));
            Assert.That(decoded.BlobIds, Is.EqualTo(new[] { "b1", "b2" }));
            Assert.That(decoded.UnknownProperties["mode"].GetString(), Is.EqualTo("fast"));
            Assert.That(() => method.CreateInvocation(sameAccount, "copy-8"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => method.CreateInvocation(request, ""), Throws.TypeOf<JmapValidationException>());
        }
    }

    [Test]
    public void ClientMappingConvertsCorrelatedMethodError()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        JmapResponse wire = new JmapResponse();
        wire.MethodResponses.Add(new JmapInvocation
        {
            Name = ResponseMapper.ErrorResponseName,
            CallId = "copy-1",
            Arguments = Json("{\"type\":\"fromAccountNotFound\",\"description\":\"Missing source\"}")
        });

        JmapMethodException error = Assert.Throws<JmapMethodException>(() => method.MapResponse(
            wire,
            "copy-1",
            Request("source", "target", "b1")))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.MethodName, Is.EqualTo(JmapMethods.BlobCopy));
            Assert.That(error.CallId, Is.EqualTo("copy-1"));
            Assert.That(error.ErrorToken, Is.EqualTo(JmapErrorCodes.FromAccountNotFound));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.FromAccountNotFound));
        }
    }

    [Test]
    public void CancellationBeforeAsyncDispatchDoesNotCallBackend()
    {
        BlobCopyMethod method = new BlobCopyMethod();
        FakeBlobService blobs = new FakeBlobService();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<JmapInvocation> task = method.ProcessInvocationAsync(
            Invocation(Request("source", "target", "b1"), "copy-1"),
            Session(true, true, true, false),
            blobs,
            new JmapRequestContext(),
            cancellation.Token);

        Assert.That(async () => await task, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(task.IsCanceled, Is.True);
            Assert.That(blobs.CopyCalls, Is.Zero);
        }
    }

    private static BlobCopyRequest Request(string source, string target, params string[] blobIds)
    {
        BlobCopyRequest request = new BlobCopyRequest();
        request.FromAccountId = source;
        request.AccountId = target;
        request.BlobIds.AddRange(blobIds);
        return request;
    }

    private static BlobCopyResponse Response(string source, string target)
    {
        BlobCopyResponse response = new BlobCopyResponse();
        response.FromAccountId = source;
        response.AccountId = target;
        return response;
    }

    private static JmapSetError SetError(string type, string? description)
    {
        JmapSetError error = new JmapSetError();
        error.Type = type;
        error.Description = description;
        return error;
    }

    private static JmapInvocation Invocation(BlobCopyRequest request, string callId)
    {
        JmapJsonReader reader = new JmapJsonReader(10000, 64);
        BlobPushJsonCodec codec = new BlobPushJsonCodec(reader);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        codec.EncodeBlobCopyRequest(request, writer);
        writer.DisposeWriter();
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.BlobCopy;
        invocation.Arguments = reader.ReadUtf8Memory(stream.ToArray());
        invocation.CallId = callId;
        return invocation;
    }

    private static JmapSession Session(bool source, bool target, bool core, bool targetReadOnly)
    {
        JmapSession session = new JmapSession();
        if (core)
        {
            JmapCapability capability = new JmapCapability();
            capability.Core = new JmapCoreCapabilities();
            session.Capabilities.Add(JmapCapabilities.Core, capability);
        }

        if (source)
        {
            session.Accounts.Add("source", new JmapAccount());
        }

        if (target)
        {
            JmapAccount account = new JmapAccount();
            account.IsReadOnly = targetReadOnly;
            session.Accounts.Add("target", account);
        }

        return session;
    }

    private static JmapMethodError Error(BlobCopyMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class FakeBlobService : IBlobService
    {
        public BlobCopyResponse CopyResponse = Response("source", "target");
        public BlobCopyRequest? LastCopyRequest;
        public JmapRequestContext? LastCopyContext;
        public int CopyCalls;

        public Task<BlobCopyResponse> CopyStoredBlobsAsync(
            BlobCopyRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            CopyCalls = CopyCalls + 1;
            LastCopyRequest = request;
            LastCopyContext = context;
            return Task.FromResult(CopyResponse);
        }

        public Task<BlobUploadResponse> StoreUploadedBlobAsync(
            BlobUploadRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new BlobUploadResponse());
        }

        public Task<List<JmapUnreferencedBlob>> ListUnreferencedBlobsAsync(
            string accountId,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<JmapUnreferencedBlob>());
        }

        public Task RefreshUnreferencedBlobAsync(
            string accountId,
            string blobId,
            DateTimeOffset retainUntilUtc,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task EvictUnreferencedBlobAsync(
            string accountId,
            string blobId,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<JmapDownloadContent?> OpenStoredBlobAsync(
            BlobDownloadRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<JmapDownloadContent?>(null);
        }
    }
}
