using System.Text;
using System.Text.Json.Nodes;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfTestInfrastructureIntegrationTests
{
    [Test]
    public void FixturesExerciseReaderWriterAndOwnedOutputComparison()
    {
        using TemporaryResourceWorkspace workspace =
            new TemporaryResourceWorkspace();
        GltfDocument document = new GltfTestDocumentBuilder()
            .AddResolvedBuffer("resources/data.bin", [1, 2, 3, 4])
            .Build();
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Json;
        writeOptions.ResourceMode = GltfResourceMode.Preserve;
        writeOptions.Validate = false;
        GltfWriter writer = new GltfWriter();
        GltfWriteResult first = writer.WriteBytes(document, writeOptions);
        GltfWriteResult second = writer.WriteBytes(document, writeOptions);
        string inputPath = workspace.WriteBytes("scene.gltf", first.PrimaryBytes);
        workspace.WriteBytes("resources/data.bin", [1, 2, 3, 4]);
        GltfReadOptions fileOptions = new GltfReadOptions();
        fileOptions.ResolveResources = true;
        GltfReadResult fileResult = new GltfReader().ReadFile(
            inputPath,
            fileOptions);
        using HostileTestStream stream = new HostileTestStream(
            first.PrimaryBytes,
            true,
            false,
            false,
            2,
            int.MaxValue);
        GltfReadResult streamResult = new GltfReader().ReadStream(
            stream,
            new GltfReadOptions());
        DeterministicByteArrayComparer byteComparer =
            new DeterministicByteArrayComparer();

        DeterministicOutputComparer.AssertWriteResultsEqual(first, second);
        Assert.Multiple(() =>
        {
            Assert.That(fileResult.Document!.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(streamResult.Document, Is.Not.Null);
            Assert.That(stream.SeekAttempted, Is.False);
            Assert.That(stream.ReadByteLength,
                Is.EqualTo(first.PrimaryBytes.Length));
            Assert.That(byteComparer.Equals(
                first.Resources.Resources[0].Bytes,
                second.Resources.Resources[0].Bytes), Is.True);
            Assert.That(byteComparer.GetHashCode(
                    first.Resources.Resources[0].Bytes),
                Is.EqualTo(byteComparer.GetHashCode(
                    second.Resources.Resources[0].Bytes)));
        });
    }

    [Test]
    public void TestDoublesDriveResolverCodecExtensionAndTransformPipelines()
    {
        TestResourceResolver resolver = new TestResourceResolver();
        resolver.Add(
            "buffer.bin",
            GltfResourceKind.Buffer,
            GltfMimeTypes.GltfBuffer,
            [1, 2, 3, 4]);
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":4}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}");
        GltfConversionOptions conversionOptions = new GltfConversionOptions();
        conversionOptions.OutputFormat = GltfContainerFormat.Glb;
        conversionOptions.ResourceMode = GltfResourceMode.GlbBin;
        conversionOptions.ReadOptions.ResourceResolver = resolver;
        conversionOptions.WriteOptions.Validate = false;
        GltfConversionResult converted = new GltfConverter().ConvertBytes(
            json,
            conversionOptions);
        GltfConversionResult repeatedConversion = new GltfConverter().ConvertBytes(
            json,
            conversionOptions);

        TestCodec codec = new TestCodec();
        codec.CompressionResult.Bytes = [9, 8, 7];
        GltfCompressionResult decoded = new CompressionCodecDispatcher()
            .DecodeGeometry(
                new GltfCompressionRequest(),
                new List<IGltfCompressionCodec> { codec });

        TestExtensionHandler extension =
            new TestExtensionHandler("EXT_test_support");
        GltfDocument extensionDocument = new GltfTestDocumentBuilder()
            .AddExtension(
                "EXT_test_support",
                new JsonObject { ["enabled"] = true },
                false)
            .Build();
        GltfWriteOptions extensionOptions = new GltfWriteOptions();
        extensionOptions.Validate = false;
        extensionOptions.ExtensionHandlers.Add(extension);
        GltfWriteResult written = new GltfWriter().WriteBytes(
            extensionDocument,
            extensionOptions);

        TestTransformHook hook = new TestTransformHook();
        hook.NodeName = "changed";
        hook.Codec = "test-codec";
        GltfTransformPlan plan = new GltfTransformPlan();
        plan.Steps.Add(new GltfTransformStep
        {
            Kind = GltfTransformKind.Custom,
            Name = "test"
        });
        GltfTransformOptions transformOptions = new GltfTransformOptions();
        transformOptions.ValidateBefore = false;
        transformOptions.ValidateAfter = false;
        transformOptions.Hooks.Add(hook);
        GltfTransformReport transformed = new GltfTransformer()
            .TransformDocument(
                new GltfTestDocumentBuilder().AddNode("original").Build(),
                plan,
                transformOptions);

        DeterministicOutputComparer.AssertConversionResultsEqual(
            converted,
            repeatedConversion);
        Assert.Multiple(() =>
        {
            Assert.That(converted.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(resolver.Requests, Has.Count.EqualTo(2));
            Assert.That(converted.Document!.Container!.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(codec.Calls, Is.EqualTo(new[] { "DecodeGeometry" }));
            Assert.That(written.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(extension.Calls,
                Does.Contain("WriteExtension"));
            Assert.That(transformed.Document!.Nodes[0].Name,
                Is.EqualTo("changed"));
            Assert.That(transformed.Codecs,
                Is.EqualTo(new[] { "test-codec" }));
            Assert.That(hook.LastContext, Is.Not.Null);
        });
    }

    [Test]
    public void HostileStreamsExerciseBoundedReadAndWriteFailures()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"}}");
        using HostileTestStream failingInput = new HostileTestStream(
            json,
            true,
            false,
            false,
            1,
            int.MaxValue);
        failingInput.FailReadsAfterByteLength = 3;
        using HostileTestStream failingOutput = new HostileTestStream(
            [],
            false,
            true,
            false,
            int.MaxValue,
            int.MaxValue);
        failingOutput.FailWritesAfterByteLength = 0;
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Validate = false;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadStream(
                    failingInput,
                    new GltfReadOptions()),
                Throws.TypeOf<IOException>().With.Message.EqualTo(
                    "Configured hostile read failure."));
            Assert.That(
                () => new GltfWriter().WriteStream(
                    new GltfTestDocumentBuilder().Build(),
                    failingOutput,
                    writeOptions),
                Throws.TypeOf<IOException>().With.Message.EqualTo(
                    "Configured hostile write failure."));
        });
    }

    [Test]
    public void KhronosCorpusAdapterFeedsVendoredAssetsToContainerReader()
    {
        List<KhronosAssetCase> assets = KhronosAssetCorpus.EnumerateAssets();
        GltfReader reader = new GltfReader();
        List<string> parsed = new List<string>();
        foreach (KhronosAssetCase asset in assets)
        {
            GltfReadOptions options = new GltfReadOptions();
            options.Format = asset.Format;
            GltfReadResult result = reader.ReadContainerBytes(
                KhronosAssetCorpus.ReadBytes(asset),
                options);
            Assert.That(result.Container, Is.Not.Null, asset.RelativePath);
            parsed.Add(asset.RelativePath);
        }

        Assert.Multiple(() =>
        {
            Assert.That(assets, Is.Not.Empty);
            Assert.That(parsed, Is.EqualTo(parsed.OrderBy(
                value => value,
                StringComparer.Ordinal)));
            Assert.That(assets.All(asset =>
                asset.RelativePath.StartsWith(
                    "specs/khronos-gltf/",
                    StringComparison.Ordinal)), Is.True);
        });
    }
}
