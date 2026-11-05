using System.Text;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class RequiredExtensionCapabilityTests
{
    [Test]
    public void Checker_DistinguishesEveryRequiredCapabilityKind()
    {
        RequiredExtensionCapabilityChecker checker =
            new RequiredExtensionCapabilityChecker();
        GltfDocument unknown = CreateRequiredDocument("ACME_unknown");
        GltfDocument semantic = CreateRequiredDocument(GltfExtensionNames.KhrTechniquesWebgl);
        GltfDocument resource = CreateRequiredDocument(GltfExtensionNames.KhrTextureBasisu);
        GltfDocument remap = CreateRequiredDocument(GltfExtensionNames.KhrMeshQuantization);

        List<GltfValidationIssue> metadataIssues = checker.CheckReadCapabilities(
            unknown,
            new List<IGltfExtensionHandler>(),
            new List<IGltfCompressionCodec>(),
            new List<IGltfImageCodec>());
        List<GltfValidationIssue> semanticIssues = checker.CheckEvaluationCapabilities(semantic);
        List<GltfValidationIssue> resourceIssues = checker.CheckReadCapabilities(
            resource,
            new List<IGltfExtensionHandler>(),
            new List<IGltfCompressionCodec>(),
            new List<IGltfImageCodec>());
        List<GltfValidationIssue> remapIssues = checker.CheckMutationCapabilities(remap);

        Assert.Multiple(() =>
        {
            AssertCapabilityIssue(
                metadataIssues,
                GltfIssueCodes.RequiredExtensionMetadataUnavailable);
            AssertCapabilityIssue(
                semanticIssues,
                GltfIssueCodes.RequiredExtensionSemanticsUnavailable);
            AssertCapabilityIssue(
                resourceIssues,
                GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable);
            AssertCapabilityIssue(
                remapIssues,
                GltfIssueCodes.RequiredExtensionRemapUnavailable);
        });
    }

    [Test]
    public void Checker_AcceptsRegisteredHandlersAndBothCodecCategories()
    {
        RequiredExtensionCapabilityChecker checker =
            new RequiredExtensionCapabilityChecker();
        GltfDocument custom = CreateRequiredDocument("ACME_custom");
        GltfDocument image = CreateRequiredDocument(GltfExtensionNames.KhrTextureBasisu);
        GltfDocument compression = CreateRequiredDocument(
            GltfExtensionNames.KhrDracoMeshCompression);
        List<IGltfExtensionHandler> handlers =
            new List<IGltfExtensionHandler> { new CapabilityExtensionHandler("ACME_custom") };
        List<IGltfImageCodec> imageCodecs =
            new List<IGltfImageCodec> { new CapabilityImageCodec() };
        List<IGltfCompressionCodec> compressionCodecs =
            new List<IGltfCompressionCodec> { new CapabilityCompressionCodec() };

        Assert.Multiple(() =>
        {
            Assert.That(checker.CheckReadCapabilities(
                custom,
                handlers,
                new List<IGltfCompressionCodec>(),
                new List<IGltfImageCodec>()), Is.Empty);
            Assert.That(checker.CheckReadCapabilities(
                image,
                new List<IGltfExtensionHandler>(),
                new List<IGltfCompressionCodec>(),
                imageCodecs), Is.Empty);
            Assert.That(checker.CheckReadCapabilities(
                compression,
                new List<IGltfExtensionHandler>(),
                compressionCodecs,
                new List<IGltfImageCodec>()), Is.Empty);
        });
    }

    [TestCase("ACME_unknown", GltfIssueCodes.RequiredExtensionMetadataUnavailable)]
    [TestCase(
        GltfExtensionNames.KhrTechniquesWebgl,
        GltfIssueCodes.RequiredExtensionSemanticsUnavailable)]
    [TestCase(
        GltfExtensionNames.KhrTextureBasisu,
        GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable)]
    [TestCase(
        GltfExtensionNames.ExtTextureWebp,
        GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable)]
    [TestCase(
        GltfExtensionNames.KhrDracoMeshCompression,
        GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable)]
    [TestCase(
        GltfExtensionNames.KhrMeshoptCompression,
        GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable)]
    [TestCase(
        GltfExtensionNames.ExtMeshoptCompression,
        GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable)]
    public void Reader_StopsBeforeUnsupportedRequiredExtensionProcessing(
        string extension,
        string issueCode)
    {
        string json = "{\"asset\":{\"version\":\"2.0\"},\"extensionsUsed\":[\""
            + extension
            + "\"],\"extensionsRequired\":[\""
            + extension
            + "\"]}";

        GltfReadResult result = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            AssertCapabilityIssue(result.Diagnostics, issueCode);
        });
    }

    [Test]
    public void Reader_AllowsARequiredExtensionImplementedByARegisteredHandler()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"ACME_custom\"],"
            + "\"extensionsRequired\":[\"ACME_custom\"],"
            + "\"extensions\":{\"ACME_custom\":{\"value\":1}}}";
        GltfReadOptions options = new GltfReadOptions();
        options.ExtensionHandlers.Add(new CapabilityExtensionHandler("ACME_custom"));

        GltfReadResult result = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code.StartsWith("REQUIRED_EXTENSION_", StringComparison.Ordinal)),
                Is.False);
        });
    }

    [Test]
    public void Reader_ChecksRequiredCapabilitiesBeforeResolvingResources()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"ACME_unknown\"],"
            + "\"extensionsRequired\":[\"ACME_unknown\"],"
            + "\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":4}]}";
        CapabilityResourceResolver resolver = new CapabilityResourceResolver();
        GltfReadOptions options = new GltfReadOptions
        {
            ResolveResources = true,
            ResourceResolver = resolver
        };

        GltfReadResult result = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(resolver.Calls, Is.Zero);
            AssertCapabilityIssue(
                result.Diagnostics,
                GltfIssueCodes.RequiredExtensionMetadataUnavailable);
        });
    }

    [Test]
    public void EvaluationEntryPoints_RejectMissingRequiredSemantics()
    {
        GltfDocument document = CreateRequiredDocument(
            GltfExtensionNames.KhrTechniquesWebgl);
        GltfScenePose scene = new GltfSceneEvaluator().EvaluateScene(
            document,
            new GltfSceneEvaluationOptions());
        GltfAnimationPose animation = new GltfAnimationEvaluator().SampleAnimation(
            document,
            new GltfAnimationEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(scene.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            AssertCapabilityIssue(
                scene.Diagnostics,
                GltfIssueCodes.RequiredExtensionSemanticsUnavailable);
            Assert.That(animation.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            AssertCapabilityIssue(
                animation.Diagnostics,
                GltfIssueCodes.RequiredExtensionSemanticsUnavailable);
        });
    }

    [Test]
    public void ConversionAndMutationEntryPoints_CheckCapabilitiesBeforeExecution()
    {
        GltfDocument semantic = CreateRequiredDocument(
            GltfExtensionNames.KhrTechniquesWebgl);
        GltfDocument remap = CreateRequiredDocument(GltfExtensionNames.KhrMeshQuantization);
        GltfEditOptions editOptions = new GltfEditOptions();
        GltfTransformOptions transformOptions = new GltfTransformOptions();
        GltfConversionOptions transformConversion = new GltfConversionOptions();
        transformConversion.TransformPlan = new GltfTransformPlan();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfConverter().ConvertDocument(
                    semantic,
                    new GltfConversionOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("no semantic implementation"));
            Assert.That(
                () => new GltfDocumentEditor().CompactDocument(remap, editOptions),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("cannot safely remap"));
            Assert.That(
                () => new GltfConverter().ConvertDocument(remap, transformConversion),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("cannot safely remap"));
            Assert.That(
                () => new GltfTransformer().TransformDocument(
                    remap,
                    new GltfTransformPlan(),
                    transformOptions),
                Throws.TypeOf<GltfUnsupportedFeatureException>()
                    .With.Message.Contains("cannot safely remap"));
        });
    }

    private static GltfDocument CreateRequiredDocument(string extension)
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add(extension);
        result.ExtensionsRequired.Add(extension);
        return result;
    }

    private static void AssertCapabilityIssue(
        List<GltfValidationIssue> issues,
        string issueCode)
    {
        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Code, Is.EqualTo(issueCode));
        Assert.That(issues[0].Severity, Is.EqualTo(GltfValidationSeverity.Error));
        Assert.That(issues[0].JsonPointer, Is.EqualTo("/extensionsRequired/0"));
    }

    private class CapabilityExtensionHandler : IGltfExtensionHandler
    {
        private readonly string name;

        public CapabilityExtensionHandler(string extensionName)
        {
            name = extensionName;
        }

        public GltfExtensionDescriptor DescribeExtension()
        {
            return new GltfExtensionDescriptor
            {
                Name = name,
                HasSemanticValidation = true,
                CanRemapReferences = true
            };
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            return CreateResult(context);
        }

        private static GltfExtensionResult CreateResult(GltfExtensionContext context)
        {
            return new GltfExtensionResult { Json = context.Json?.DeepClone() };
        }
    }

    private class CapabilityImageCodec : IGltfImageCodec
    {
        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            return new GltfImageInfo();
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            return new GltfImageResult();
        }
    }

    private class CapabilityCompressionCodec : IGltfCompressionCodec
    {
        public GltfCompressionResult DecodeGeometry(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeGeometry(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }

        public GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request)
        {
            return new GltfCompressionResult();
        }
    }

    private class CapabilityResourceResolver : IGltfResourceResolver
    {
        public int Calls;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Calls++;
            return new GltfResource { Bytes = new byte[4] };
        }
    }
}
