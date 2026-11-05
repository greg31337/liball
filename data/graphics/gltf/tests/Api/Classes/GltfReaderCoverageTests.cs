using System.Text;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfReaderCoverageTests
{
    private static readonly byte[] ValidJson = Encoding.UTF8.GetBytes(
        "{\"asset\":{\"version\":\"2.0\",\"generator\":\"reader coverage\"}}");

    [Test]
    public void ReaderInterface_ExecutesEveryDocumentAndContainerEntryPoint()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        string path = workspace.WriteBytes("model.gltf", ValidJson);
        IGltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();
        using MemoryStream documentStream = new MemoryStream(ValidJson);
        using MemoryStream containerStream = new MemoryStream(ValidJson);
        GltfContainerData container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json,
            JsonBytes = ValidJson.ToArray(),
            OriginalByteLength = ValidJson.Length
        };

        GltfReadResult fileResult = reader.ReadFile(path, options);
        GltfReadResult streamResult = reader.ReadStream(documentStream, options);
        GltfReadResult byteResult = reader.ReadBytes(ValidJson, options);
        GltfReadResult suppliedResult = reader.ReadContainer(container, options);
        GltfReadResult rawStreamResult = reader.ReadContainerStream(
            containerStream,
            options);
        GltfReadResult rawByteResult = reader.ReadContainerBytes(ValidJson, options);

        Assert.Multiple(() =>
        {
            AssertDocumentResult(fileResult);
            AssertDocumentResult(streamResult);
            AssertDocumentResult(byteResult);
            AssertDocumentResult(suppliedResult);
            AssertContainerResult(rawStreamResult);
            AssertContainerResult(rawByteResult);
            Assert.That(suppliedResult.Container, Is.SameAs(container));
        });
    }

    [Test]
    public void ReadBytes_AcceptsOneBomAndUnicodeUtf8ButRejectsInvalidUtf8()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\",\"generator\":\"Créateur 🚀\"},"
            + "\"nodes\":[{\"name\":\"模型\"}]}");
        byte[] withBom = new byte[json.Length + 3];
        withBom[0] = 0xEF;
        withBom[1] = 0xBB;
        withBom[2] = 0xBF;
        Array.Copy(json, 0, withBom, 3, json.Length);
        byte[] malformedUtf8 =
        [
            0x7B, 0x22, 0x61, 0x22, 0x3A, 0x22,
            0xC0, 0x80,
            0x22, 0x7D
        ];

        GltfReadResult result = new GltfReader().ReadBytes(
            withBom,
            new GltfReadOptions());
        withBom[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Document!.Asset.Generator,
                Is.EqualTo("Créateur 🚀"));
            Assert.That(result.Document.Nodes[0].Name, Is.EqualTo("模型"));
            Assert.That(result.Container!.JsonBytes[0], Is.EqualTo(0xEF));
            Assert.That(result.Container.JsonBytes[1], Is.EqualTo(0xBB));
            Assert.That(result.Container.JsonBytes[2], Is.EqualTo(0xBF));
            Assert.That(
                () => new GltfReader().ReadBytes(
                    malformedUtf8,
                    new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "JSON string contains invalid UTF-8 at byte 6 and JSON pointer '/a'."));
            Assert.That(
                () => new GltfReader().ReadBytes(
                    Encoding.Unicode.GetBytes(
                        "{\"asset\":{\"version\":\"2.0\"}}"),
                    new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>());
        });
    }

    [Test]
    public void DocumentEntryPoints_RejectMalformedJsonWhileRawReadsRetainIt()
    {
        byte[] malformed = Encoding.UTF8.GetBytes("{\"asset\":{");
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        string path = workspace.WriteBytes("malformed.gltf", malformed);
        GltfReader reader = new GltfReader();
        GltfContainerData container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json,
            JsonBytes = malformed.ToArray(),
            OriginalByteLength = malformed.Length
        };
        using MemoryStream stream = new MemoryStream(malformed);

        GltfReadResult rawBytes = reader.ReadContainerBytes(
            malformed,
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rawBytes.Container!.JsonBytes, Is.EqualTo(malformed));
            Assert.That(rawBytes.Document, Is.Null);
            Assert.That(
                () => reader.ReadBytes(malformed, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>());
            Assert.That(
                () => reader.ReadStream(stream, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>());
            Assert.That(
                () => reader.ReadContainer(container, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>());
            Assert.That(
                () => reader.ReadFile(path, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>());
        });
    }

    [Test]
    public void ReadStream_HonorsCallerOwnershipOnSuccessAndParseFailure()
    {
        HostileTestStream keptSuccess = CreateReadStream(ValidJson);
        HostileTestStream ownedSuccess = CreateReadStream(ValidJson);
        HostileTestStream keptFailure = CreateReadStream(
            Encoding.UTF8.GetBytes("{\"asset\":{"));
        HostileTestStream ownedFailure = CreateReadStream(
            Encoding.UTF8.GetBytes("{\"asset\":{"));

        new GltfReader().ReadStream(
            keptSuccess,
            new GltfReadOptions { LeaveStreamOpen = true });
        new GltfReader().ReadStream(
            ownedSuccess,
            new GltfReadOptions { LeaveStreamOpen = false });

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadStream(
                    keptFailure,
                    new GltfReadOptions { LeaveStreamOpen = true }),
                Throws.TypeOf<GltfParseException>());
            Assert.That(
                () => new GltfReader().ReadStream(
                    ownedFailure,
                    new GltfReadOptions { LeaveStreamOpen = false }),
                Throws.TypeOf<GltfParseException>());
            Assert.That(keptSuccess.Disposed, Is.False);
            Assert.That(ownedSuccess.Disposed, Is.True);
            Assert.That(keptFailure.Disposed, Is.False);
            Assert.That(ownedFailure.Disposed, Is.True);
            Assert.That(keptSuccess.SeekAttempted, Is.False);
            Assert.That(ownedSuccess.SeekAttempted, Is.False);
            Assert.That(keptFailure.SeekAttempted, Is.False);
            Assert.That(ownedFailure.SeekAttempted, Is.False);
        });

        keptSuccess.Dispose();
        keptFailure.Dispose();
    }

    [Test]
    public void ReadBytes_EnforcesEveryPrimaryJsonAndModelLimit()
    {
        GltfReadOptions input = new GltfReadOptions();
        input.Limits.MaximumInputByteLength = ValidJson.Length - 1;
        GltfReadOptions json = new GltfReadOptions();
        json.Limits.MaximumJsonByteLength = ValidJson.Length - 1;
        GltfReadOptions depth = new GltfReadOptions();
        depth.Limits.MaximumJsonDepth = 1;
        GltfReadOptions tokens = new GltfReadOptions();
        tokens.Limits.MaximumJsonTokenCount = 1;
        GltfReadOptions strings = new GltfReadOptions();
        strings.Limits.MaximumJsonStringByteLength = 1;
        GltfReadOptions numbers = new GltfReadOptions();
        numbers.Limits.MaximumJsonNumberByteLength = 3;
        GltfReadOptions collections = new GltfReadOptions();
        collections.Limits.MaximumCollectionCount = 1;

        Assert.Multiple(() =>
        {
            AssertReadParseFailure(ValidJson, input, "Input length");
            AssertReadParseFailure(ValidJson, json, "JSON content length");
            AssertReadParseFailure(ValidJson, depth, "JSON depth");
            AssertReadParseFailure(ValidJson, tokens, "JSON token count");
            AssertReadParseFailure(
                Encoding.UTF8.GetBytes("{\"é\":0}"),
                strings,
                "JSON string byte length");
            AssertReadParseFailure(
                Encoding.UTF8.GetBytes("{\"a\":1234}"),
                numbers,
                "JSON number byte length");
            AssertReadParseFailure(
                Encoding.UTF8.GetBytes(
                    "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{},{}]}"),
                collections,
                "JSON collection count");
        });
    }

    [Test]
    public void ReadBytes_LeavesRequestsPendingOrResolvesOwnedDataByMode()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"data:application/gltf-buffer;base64,AQID\",\"byteLength\":3}],"
            + "\"images\":[{\"uri\":\"data:image/png;base64,BAU=\"}]}");
        GltfReadOptions lazyOptions = new GltfReadOptions();
        GltfReadOptions resolvedOptions = new GltfReadOptions
        {
            ResolveResources = true
        };

        GltfReadResult lazy = new GltfReader().ReadBytes(json, lazyOptions);
        GltfReadResult resolved = new GltfReader().ReadBytes(json, resolvedOptions);
        GltfBuffer lazyBuffer = lazy.Document!.Buffers[0];
        GltfImage lazyImage = lazy.Document.Images[0];
        GltfBuffer resolvedBuffer = resolved.Document!.Buffers[0];
        GltfImage resolvedImage = resolved.Document.Images[0];
        resolved.Resources.Resources[0].Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(lazy.Resources.Resources, Is.Empty);
            Assert.That(lazyBuffer.Resolved, Is.False);
            Assert.That(lazyBuffer.PendingResourceRequest, Is.Not.Null);
            Assert.That(lazyImage.Resolved, Is.False);
            Assert.That(lazyImage.PendingResourceRequest, Is.Not.Null);
            Assert.That(resolved.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(resolvedBuffer.Resolved, Is.True);
            Assert.That(resolvedBuffer.ResolvedBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(resolvedBuffer.ResolvedBytes[0], Is.EqualTo(1));
            Assert.That(resolvedBuffer.PendingResourceRequest, Is.Null);
            Assert.That(resolvedImage.Resolved, Is.True);
            Assert.That(resolvedImage.ResolvedBytes, Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(resolvedImage.PendingResourceRequest, Is.Null);
        });
    }

    [Test]
    public void ReadBytes_AppliesRejectKeepFirstAndKeepLastDuplicatePolicies()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\",\"generator\":\"first\","
            + "\"generator\":\"last\"}}");
        GltfReadOptions keepFirst = new GltfReadOptions
        {
            DuplicateMemberPolicy = GltfDuplicateMemberPolicy.KeepFirst
        };
        GltfReadOptions keepLast = new GltfReadOptions
        {
            DuplicateMemberPolicy = GltfDuplicateMemberPolicy.KeepLast
        };

        GltfReadResult first = new GltfReader().ReadBytes(json, keepFirst);
        GltfReadResult last = new GltfReader().ReadBytes(json, keepLast);

        Assert.Multiple(() =>
        {
            Assert.That(first.Document!.Asset.Generator, Is.EqualTo("first"));
            Assert.That(last.Document!.Asset.Generator, Is.EqualTo("last"));
            Assert.That(
                () => new GltfReader().ReadBytes(json, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.Contains("Duplicate JSON object member 'generator'"));
        });
    }

    [Test]
    public void ReadBytes_CollectsValidationOrThrowsInStrictMode()
    {
        byte[] invalid = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"nodes\":[{\"mesh\":4},{\"camera\":3}]}");
        GltfReadOptions collecting = new GltfReadOptions { Validate = true };
        GltfReadOptions strict = new GltfReadOptions { Validate = true };
        strict.ValidationOptions.Strict = true;
        GltfReadOptions disabled = new GltfReadOptions { Validate = false };

        GltfReadResult collected = new GltfReader().ReadBytes(invalid, collecting);
        GltfReadResult unvalidated = new GltfReader().ReadBytes(invalid, disabled);

        Assert.Multiple(() =>
        {
            Assert.That(collected.Document, Is.Not.Null);
            Assert.That(collected.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(collected.Validation.IsValid, Is.False);
            Assert.That(collected.Validation.Issues, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(collected.Validation.Issues.Select(issue => issue.Code),
                Does.Contain(GltfIssueCodes.UnresolvedReference));
            Assert.That(unvalidated.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(unvalidated.Validation.Issues, Is.Empty);
            Assert.That(
                () => new GltfReader().ReadBytes(invalid, strict),
                Throws.TypeOf<GltfValidationException>()
                    .With.Message.Contains(GltfIssueCodes.UnresolvedReference));
        });
    }

    private static HostileTestStream CreateReadStream(byte[] bytes)
    {
        return new HostileTestStream(
            bytes,
            true,
            false,
            false,
            3,
            int.MaxValue);
    }

    private static void AssertReadParseFailure(
        byte[] bytes,
        GltfReadOptions options,
        string message)
    {
        Assert.That(
            () => new GltfReader().ReadBytes(bytes, options),
            Throws.TypeOf<GltfParseException>().With.Message.Contains(message));
    }

    private static void AssertDocumentResult(GltfReadResult result)
    {
        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Document!.Asset.Generator, Is.EqualTo("reader coverage"));
        Assert.That(result.Container, Is.SameAs(result.Document.Container));
    }

    private static void AssertContainerResult(GltfReadResult result)
    {
        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(result.Document, Is.Null);
        Assert.That(result.Container, Is.Not.Null);
        Assert.That(result.Container!.Format, Is.EqualTo(GltfContainerFormat.Json));
        Assert.That(result.Container.JsonBytes, Is.EqualTo(ValidJson));
    }
}
