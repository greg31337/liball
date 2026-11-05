using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfConverterPipelineTests
{
    [Test]
    public void ConvertBytes_ComposesReaderResolverAndGlbWriter()
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/gltf-buffer;base64,AQIDBA==\",\"byteLength\":4}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}");
        GltfConversionOptions options = new GltfConversionOptions();
        options.InputFormat = GltfContainerFormat.Auto;
        options.OutputFormat = GltfContainerFormat.Glb;
        options.ResourceMode = GltfResourceMode.GlbBin;
        options.WriteOptions.Validate = false;

        GltfConversionResult result = new GltfConverter().ConvertBytes(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.PrimaryBytes.Take(4),
                Is.EqualTo(new byte[] { 0x67, 0x6C, 0x54, 0x46 }));
            Assert.That(result.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(result.Document.Container.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.Manifest, Has.Count.EqualTo(1));
            Assert.That(result.Manifest[0].Mode,
                Is.EqualTo(GltfResourceMode.GlbBin));
            Assert.That(options.ReadOptions.ResolveResources, Is.False);
        });
    }

    [Test]
    public void ConvertBytes_AppliesOptionalTransformBeforeWriting()
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"name\":\"before\"}]}");
        RenameNodeHook hook = new RenameNodeHook(
            "after",
            GltfOperationStatus.Success);
        GltfConversionOptions options = CreateJsonTransformOptions(hook);

        GltfConversionResult result = new GltfConverter().ConvertBytes(
            source,
            options);
        JsonObject json = JsonNode.Parse(result.PrimaryBytes)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(json["nodes"]![0]!["name"]!.GetValue<string>(),
                Is.EqualTo("after"));
            Assert.That(result.TransformReport, Is.Not.Null);
            Assert.That(result.TransformReport!.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.TransformReport.Steps, Has.Count.EqualTo(1));
            Assert.That(result.TransformReport.ChangedObjects,
                Is.EqualTo(new[] { "nodes/0" }));
            Assert.That(hook.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConvertBytes_StopsBeforeWritingWhenTransformFails()
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"name\":\"before\"}]}");
        RenameNodeHook hook = new RenameNodeHook(
            "failed",
            GltfOperationStatus.Failed);
        GltfConversionOptions options = CreateJsonTransformOptions(hook);

        GltfConversionResult result = new GltfConverter().ConvertBytes(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(result.PrimaryBytes, Is.Empty);
            Assert.That(result.PrimaryContentHash, Is.Null);
            Assert.That(result.TransformReport, Is.Not.Null);
            Assert.That(result.TransformReport!.Status,
                Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(result.Document, Is.Null);
            Assert.That(hook.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConvertBytes_ReturnsInputValidationFailureBeforeConversion()
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"mesh\":1}]}");
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.ReadOptions.Validate = true;
        options.WriteOptions.Validate = false;

        GltfConversionResult result = new GltfConverter().ConvertBytes(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.PrimaryBytes, Is.Empty);
            Assert.That(result.Diagnostics.Select(issue => issue.Code),
                Does.Contain(GltfIssueCodes.UnresolvedReference));
        });
    }

    [Test]
    public void ConvertDocument_AppliesRequestedInputValidation()
    {
        GltfDocument source = new GltfDocument();
        source.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        source.Nodes.Add(new GltfNode
        {
            HasMesh = true,
            Mesh = 1
        });
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.ReadOptions.Validate = true;
        options.WriteOptions.Validate = false;

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.PrimaryBytes, Is.Empty);
            Assert.That(result.Diagnostics.Select(issue => issue.Code),
                Does.Contain(GltfIssueCodes.UnresolvedReference));
            Assert.That(result.Document, Is.SameAs(source));
        });
    }

    [Test]
    public void ConvertFile_AppliesTransformBeforeTransactionalPublication()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "gltf-converter-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string inputPath = Path.Combine(directory, "input.gltf");
        string outputPath = Path.Combine(directory, "output.gltf");
        File.WriteAllText(
            inputPath,
            "{\"asset\":{\"version\":\"2.0\"},\"nodes\":[{\"name\":\"before\"}]}",
            new UTF8Encoding(false));
        RenameNodeHook hook = new RenameNodeHook(
            "published",
            GltfOperationStatus.Success);
        GltfConversionOptions options = CreateJsonTransformOptions(hook);

        try
        {
            GltfConversionResult result = new GltfConverter().ConvertFile(
                inputPath,
                outputPath,
                options);
            JsonObject json = JsonNode.Parse(
                File.ReadAllBytes(outputPath))!.AsObject();

            Assert.Multiple(() =>
            {
                Assert.That(result.Status,
                    Is.EqualTo(GltfOperationStatus.Success));
                Assert.That(result.PrimaryBytes, Is.Empty);
                Assert.That(result.PrimaryPath,
                    Is.EqualTo(Path.GetFullPath(outputPath)));
                Assert.That(result.TransformReport, Is.Not.Null);
                Assert.That(json["nodes"]![0]!["name"]!.GetValue<string>(),
                    Is.EqualTo("published"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(GltfExtensionNames.KhrTextureBasisu, GltfMimeTypes.Ktx2)]
    [TestCase(GltfExtensionNames.ExtTextureWebp, GltfMimeTypes.WebP)]
    public void ConvertBytes_PreservesOpaqueExtensionImagesWithoutCodecRoundTrip(
        string extension,
        string mimeType)
    {
        byte[] source = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"extensionsUsed\":[\""
            + extension
            + "\"],\"images\":[{\"uri\":\"data:"
            + mimeType
            + ";base64,qrvM\"}],\"textures\":[{\"extensions\":{\""
            + extension
            + "\":{\"source\":0}}}]}");
        byte[] original = (byte[])source.Clone();
        ImageCodecProbe codec = new ImageCodecProbe();
        GltfConversionOptions options = new GltfConversionOptions();
        options.OutputFormat = GltfContainerFormat.Json;
        options.ResourceMode = GltfResourceMode.Preserve;
        options.ReadOptions.ImageCodecs.Add(codec);
        options.WriteOptions.ImageCodecs.Add(codec);
        options.WriteOptions.Validate = false;

        GltfConversionResult result = new GltfConverter().ConvertBytes(
            source,
            options);
        JsonObject json = JsonNode.Parse(result.PrimaryBytes)!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(codec.Calls, Is.Zero);
            Assert.That(json["images"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:" + mimeType + ";base64,qrvM"));
            Assert.That(json["textures"]![0]!["extensions"]![
                extension], Is.Not.Null);
            Assert.That(source, Is.EqualTo(original));
            Assert.That(result.Decisions.Single(decision =>
                    decision.Kind == GltfConversionDecisionKind.Codec).Action,
                Is.EqualTo(GltfConversionDecisionAction.Available));
        });
    }

    private static GltfConversionOptions CreateJsonTransformOptions(
        IGltfTransformHook hook)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Json;
        result.OutputFormat = GltfContainerFormat.Json;
        result.WriteOptions.Validate = false;
        result.TransformPlan = new GltfTransformPlan
        {
            Steps =
            [
                new GltfTransformStep
                {
                    Kind = GltfTransformKind.Custom,
                    Name = "rename"
                }
            ]
        };
        result.TransformOptions.ValidateBefore = false;
        result.TransformOptions.ValidateAfter = false;
        result.TransformOptions.Hooks.Add(hook);
        return result;
    }

    private sealed class RenameNodeHook : IGltfTransformHook
    {
        private readonly string name;
        private readonly GltfOperationStatus status;

        public int Calls;

        public RenameNodeHook(string name, GltfOperationStatus status)
        {
            this.name = name;
            this.status = status;
        }

        public GltfTransformStepResult ApplyTransformHook(
            GltfTransformContext context)
        {
            Calls++;
            context.Document!.Nodes[0].Name = name;
            GltfTransformStepResult result = new GltfTransformStepResult();
            result.Status = status;
            result.ChangedObjects.Add("nodes/0");
            return result;
        }
    }

    private sealed class ImageCodecProbe : IGltfImageCodec
    {
        public int Calls;

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageInfo();
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }
    }
}
