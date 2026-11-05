using System.Buffers.Binary;
using System.Text;

namespace gltf.Tests.Adversarial;

[TestFixture]
public class GltfAdversarialInputTests
{
    [Test]
    public void ReaderRejectsDeepAndHighTokenCountJsonAtConfiguredBoundaries()
    {
        string deepJson = "{\"asset\":{\"version\":\"2.0\"},\"extras\":"
            + new string('[', 128)
            + "0"
            + new string(']', 128)
            + "}";
        GltfReadOptions depthOptions = new GltfReadOptions();
        depthOptions.Limits.MaximumJsonDepth = 32;

        StringBuilder highTokenJson = new StringBuilder(
            "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[");
        int nodeIndex = 0;
        while (nodeIndex < 1024)
        {
            if (nodeIndex > 0)
            {
                highTokenJson.Append(',');
            }

            highTokenJson.Append("{}");
            nodeIndex++;
        }

        highTokenJson.Append("]}");
        GltfReadOptions tokenOptions = new GltfReadOptions();
        tokenOptions.Limits.MaximumJsonTokenCount = 128;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadBytes(
                    Encoding.UTF8.GetBytes(deepJson),
                    depthOptions),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.Contains("JSON depth exceeds the configured maximum 32"));
            Assert.That(
                () => new GltfReader().ReadBytes(
                    Encoding.UTF8.GetBytes(highTokenJson.ToString()),
                    tokenOptions),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.Contains("JSON token count exceeds the configured maximum 128"));
        });
    }

    [Test]
    public void AccessorDecodeRejectsComponentCountOverflowWithoutAllocatingOutput()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor accessor = new GltfAccessor();
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Type = GltfAccessorType.Vector4;
        accessor.Count = int.MaxValue;
        document.Accessors.Add(accessor);
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = 0;
        request.MaximumElementCount = int.MaxValue;

        GltfAccessorData result = new GltfAccessorCodec().ReadFloatingValues(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(GltfIssueCodes.AccessorTooLong));
            Assert.That(result.FloatingValues, Is.Empty);
        });
    }

    [Test]
    public void FileResolverRejectsPercentEncodedTraversalFromAFileBaseUri()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "gltf-adversarial-" + Guid.NewGuid().ToString("N"));
        string root = Path.Combine(directory, "root");
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(directory, "secret.bin"), [0x5A]);

        try
        {
            GltfResourceRequest denied = new GltfResourceRequest();
            denied.BaseUri = new Uri(Path.Combine(root, "model.gltf")).AbsoluteUri;
            denied.SourceUri = "%2e%2e%2fsecret.bin";
            denied.Policy.FileRoots.Add(root);
            GltfResourceRequest rootEscape = new GltfResourceRequest();
            rootEscape.BaseUri = denied.BaseUri;
            rootEscape.SourceUri = denied.SourceUri;
            rootEscape.Policy.FileRoots.Add(root);
            rootEscape.Policy.AllowParentPathSegments = true;
            FileGltfResourceResolver resolver = new FileGltfResourceResolver();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => resolver.ResolveResource(denied),
                    Throws.TypeOf<GltfResourceException>()
                        .With.Message.Contains("Parent path traversal"));
                Assert.That(
                    () => resolver.ResolveResource(rootEscape),
                    Throws.TypeOf<GltfResourceException>()
                        .With.Message.Contains("outside the configured file roots"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DataUriResolverRejectsBase64ExpansionBeforeReturningDecodedBytes()
    {
        byte[] decoded = new byte[32 * 1024];
        Array.Fill(decoded, (byte)0xA5);
        GltfResourceRequest request = new GltfResourceRequest();
        request.SourceUri = "data:application/octet-stream;base64,"
            + Convert.ToBase64String(decoded);
        request.MaximumByteLength = 16;

        Assert.That(
            () => new FileGltfResourceResolver().ResolveResource(request),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo(
                    "Data URI decoded length exceeds the configured maximum 24."));
    }

    [Test]
    public void ReaderRejectsOversizedGlbAndResourceDeclarationsWithoutLargeInputs()
    {
        byte[] glb = new byte[20];
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(0, 4), GltfConstants.GlbMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(
            glb.AsSpan(4, 4),
            (uint)GltfConstants.GlbVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(8, 4), (uint)glb.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(glb.AsSpan(12, 4), uint.MaxValue);
        BinaryPrimitives.WriteUInt32LittleEndian(
            glb.AsSpan(16, 4),
            GltfConstants.GlbJsonChunkType);

        GltfReadOptions resourceOptions = new GltfReadOptions();
        resourceOptions.Limits.MaximumResourceByteLength = 1024;
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"large.bin\",\"byteLength\":2147483647}]}");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadContainerBytes(glb, new GltfReadOptions()),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.Contains("exceeds the supported signed length"));
            Assert.That(
                () => new GltfReader().ReadBytes(json, resourceOptions),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.Contains("declared byteLength 2147483647"));
        });
    }

    [Test]
    public void SceneEvaluationTerminatesForADeepCyclicGraph()
    {
        const int nodeCount = 4096;
        GltfDocument document = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < nodeCount)
        {
            GltfNode node = new GltfNode();
            node.Children.Add((nodeIndex + 1) % nodeCount);
            document.Nodes.Add(node);
            nodeIndex++;
        }

        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        GltfSceneEvaluationOptions options = new GltfSceneEvaluationOptions();
        options.CalculateBounds = false;
        options.CalculateCameras = false;
        options.CalculateSkins = false;

        GltfScenePose result = new GltfSceneEvaluator().EvaluateScene(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Nodes, Has.Count.EqualTo(nodeCount));
            Assert.That(
                result.Diagnostics.Any(issue => issue.Code == GltfIssueCodes.NodeLoop),
                Is.True);
        });
    }

    [Test]
    public void ReaderAndValidatorRejectMalformedUtf8Sequences()
    {
        byte[][] invalidSequences =
        [
            [0xC0, 0xAF],
            [0xED, 0xA0, 0x80],
            [0xF4, 0x90, 0x80, 0x80],
            [0xE2, 0x82]
        ];

        Assert.Multiple(() =>
        {
            int sequenceIndex = 0;
            while (sequenceIndex < invalidSequences.Length)
            {
                byte[] json = WrapInJsonString(invalidSequences[sequenceIndex]);
                Assert.That(
                    () => new GltfReader().ReadBytes(json, new GltfReadOptions()),
                    Throws.TypeOf<GltfParseException>()
                        .With.Message.Contains("invalid UTF-8"));

                GltfValidationReport report = new GltfValidator().ValidateBytes(
                    json,
                    new GltfValidationOptions());
                Assert.That(report.IsValid, Is.False);
                Assert.That(report.Issues[0].Code, Is.EqualTo(GltfIssueCodes.InvalidJson));
                sequenceIndex++;
            }
        });
    }

    [Test]
    public void ValidatorRejectsEveryNonFiniteAccessorFloatEncoding()
    {
        byte[] bytes = new byte[12];
        WriteFloat(bytes, 0, float.NaN);
        WriteFloat(bytes, 4, float.PositiveInfinity);
        WriteFloat(bytes, 8, float.NegativeInfinity);
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = bytes.Length;
        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Type = GltfAccessorType.Scalar;
        accessor.Count = 3;
        document.Accessors.Add(accessor);

        GltfValidationReport report = new GltfValidator().ValidateDocument(
            document,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Code == GltfIssueCodes.AccessorInvalidFloat
                    && issue.JsonPointer == "/accessors/0"),
                Is.True);
        });
    }

    [Test]
    public void ValidatorStopsThePublicPipelineAtTheConfiguredIssueLimit()
    {
        GltfDocument document = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < 64)
        {
            GltfNode node = new GltfNode();
            node.Children.Add(1000 + nodeIndex);
            document.Nodes.Add(node);
            nodeIndex++;
        }

        GltfValidationOptions options = new GltfValidationOptions();
        options.MaximumIssueCount = 3;

        GltfValidationReport report = new GltfValidator().ValidateDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Count.EqualTo(3));
            Assert.That(report.IssueLimitReached, Is.True);
        });
    }

    private static byte[] WrapInJsonString(byte[] sequence)
    {
        byte[] prefix = Encoding.ASCII.GetBytes("{\"asset\":{\"version\":\"2.0\"},\"extras\":\"");
        byte[] suffix = Encoding.ASCII.GetBytes("\"}");
        byte[] result = new byte[prefix.Length + sequence.Length + suffix.Length];
        Array.Copy(prefix, 0, result, 0, prefix.Length);
        Array.Copy(sequence, 0, result, prefix.Length, sequence.Length);
        Array.Copy(suffix, 0, result, prefix.Length + sequence.Length, suffix.Length);
        return result;
    }

    private static void WriteFloat(byte[] bytes, int offset, float value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(
            bytes.AsSpan(offset, 4),
            BitConverter.SingleToInt32Bits(value));
    }
}
