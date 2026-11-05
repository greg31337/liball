using System.Text;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfPublicApiFailureMatrixTests
{
    [Test]
    public void PublicEntryPointsRejectNullArgumentsAtTheirBoundaries()
    {
        GltfDocument document = new GltfDocument();
        GltfReadOptions readOptions = new GltfReadOptions();
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        GltfValidationOptions validationOptions = new GltfValidationOptions();
        GltfConversionOptions conversionOptions = new GltfConversionOptions();
        GltfSceneEvaluationOptions sceneOptions = new GltfSceneEvaluationOptions();
        GltfAnimationPose animationPose = new GltfAnimationPose();
        GltfExtensionContext extensionContext = new GltfExtensionContext();
        GltfIndexRemap remap = new GltfIndexRemap();
        GltfTransformPlan transformPlan = new GltfTransformPlan();
        GltfTransformOptions transformOptions = new GltfTransformOptions();
        GltfContainerData container = CreateJsonContainer();
        using MemoryStream stream = new MemoryStream();

        GltfReader reader = new GltfReader();
        AssertArgumentNull(() => reader.ReadFile(null!, readOptions), "path");
        AssertArgumentNull(() => reader.ReadFile("unused", null!), "options");
        AssertArgumentNull(() => reader.ReadStream(null!, readOptions), "stream");
        AssertArgumentNull(() => reader.ReadStream(stream, null!), "options");
        AssertArgumentNull(() => reader.ReadContainerStream(null!, readOptions), "stream");
        AssertArgumentNull(() => reader.ReadContainerStream(stream, null!), "options");
        AssertArgumentNull(() => reader.ReadBytes(null!, readOptions), "bytes");
        AssertArgumentNull(() => reader.ReadBytes([], null!), "options");
        AssertArgumentNull(() => reader.ReadContainerBytes(null!, readOptions), "bytes");
        AssertArgumentNull(() => reader.ReadContainerBytes([], null!), "options");
        AssertArgumentNull(() => reader.ReadContainer(null!, readOptions), "container");
        AssertArgumentNull(() => reader.ReadContainer(container, null!), "options");

        GltfWriter writer = new GltfWriter();
        AssertArgumentNull(() => writer.WriteFile(document, null!, writeOptions), "path");
        AssertArgumentNull(() => writer.WriteFile(null!, "unused", writeOptions), "document");
        AssertArgumentNull(() => writer.WriteStream(document, null!, writeOptions), "stream");
        AssertArgumentNull(() => writer.WriteStream(document, stream, null!), "options");
        AssertArgumentNull(() => writer.WriteContainerStream(container, null!, writeOptions), "stream");
        AssertArgumentNull(() => writer.WriteContainerStream(container, stream, null!), "options");
        AssertArgumentNull(() => writer.WriteContainerStream(null!, stream, writeOptions), "container");
        AssertArgumentNull(() => writer.WriteBytes(null!, writeOptions), "document");
        AssertArgumentNull(() => writer.WriteBytes(document, null!), "options");
        AssertArgumentNull(() => writer.WriteContainerBytes(null!, writeOptions), "container");
        AssertArgumentNull(() => writer.WriteContainerBytes(container, null!), "options");
        AssertArgumentNull(() => writer.WriteJsonText(null!, writeOptions), "document");
        AssertArgumentNull(() => writer.WriteJsonText(document, null!), "options");

        GltfValidator validator = new GltfValidator();
        AssertArgumentNull(() => validator.ValidateDocument(null!, validationOptions), "document");
        AssertArgumentNull(() => validator.ValidateDocument(document, null!), "options");
        AssertArgumentNull(() => validator.ValidateFile(null!, validationOptions), "path");
        AssertArgumentNull(() => validator.ValidateFile("missing", null!), "options");
        AssertArgumentNull(() => validator.ValidateBytes(null!, validationOptions), "bytes");
        AssertArgumentNull(() => validator.ValidateBytes([], null!), "options");
        AssertArgumentNull(() => validator.ValidateContainer(null!, validationOptions), "container");
        AssertArgumentNull(() => validator.ValidateContainer(container, null!), "options");

        GltfConverter converter = new GltfConverter();
        AssertArgumentNull(() => converter.ConvertFile(null!, "output", conversionOptions), "inputPath");
        AssertArgumentNull(() => converter.ConvertFile("input", null!, conversionOptions), "outputPath");
        AssertArgumentNull(() => converter.ConvertFile("input", "output", null!), "options");
        AssertArgumentNull(() => converter.ConvertStream(null!, stream, conversionOptions), "input");
        AssertArgumentNull(() => converter.ConvertStream(stream, null!, conversionOptions), "output");
        AssertArgumentNull(() => converter.ConvertStream(stream, stream, null!), "options");
        AssertArgumentNull(() => converter.ConvertBytes(null!, conversionOptions), "bytes");
        AssertArgumentNull(() => converter.ConvertBytes([], null!), "options");
        AssertArgumentNull(() => converter.ConvertDocument(null!, conversionOptions), "document");
        AssertArgumentNull(() => converter.ConvertDocument(document, null!), "options");

        GltfAnimationEvaluator animations = new GltfAnimationEvaluator();
        AssertArgumentNull(() => animations.SampleAnimation(null!, new GltfAnimationEvaluationOptions()), "document");
        AssertArgumentNull(() => animations.SampleAnimation(document, null!), "options");
        AssertArgumentNull(() => animations.ApplyAnimationPose(null!, animationPose, sceneOptions), "document");
        AssertArgumentNull(() => animations.ApplyAnimationPose(document, null!, sceneOptions), "pose");
        AssertArgumentNull(() => animations.ApplyAnimationPose(document, animationPose, null!), "options");
        AssertArgumentNull(() => animations.CalculateAnimationDuration(null!, 0), "document");

        GltfSceneEvaluator scenes = new GltfSceneEvaluator();
        AssertArgumentNull(() => scenes.EvaluateScene(null!, sceneOptions), "document");
        AssertArgumentNull(() => scenes.EvaluateScene(document, null!), "options");
        AssertArgumentNull(() => scenes.CalculateBounds(null!, sceneOptions), "document");
        AssertArgumentNull(() => scenes.CalculateBounds(document, null!), "options");
        AssertArgumentNull(() => scenes.CalculateProjection(null!, 0), "document");

        GltfTransformer transformer = new GltfTransformer();
        AssertArgumentNull(() => transformer.TransformDocument(null!, transformPlan, transformOptions), "document");
        AssertArgumentNull(() => transformer.TransformDocument(document, null!, transformOptions), "plan");
        AssertArgumentNull(() => transformer.TransformDocument(document, transformPlan, null!), "options");

        GltfExtensionHandler extension = new GltfExtensionHandler();
        AssertArgumentNull(() => extension.ReadExtension(null!), "context");
        AssertArgumentNull(() => extension.WriteExtension(null!), "context");
        AssertArgumentNull(() => extension.ValidateExtension(null!), "context");
        AssertArgumentNull(() => extension.RemapExtensionReferences(null!, remap), "context");
        AssertArgumentNull(() => extension.RemapExtensionReferences(extensionContext, null!), "remap");
    }

    [Test]
    public void ConverterAndTransformerRejectInvalidNestedOptionsAndStreams()
    {
        GltfConverter converter = new GltfConverter();
        GltfDocument document = new GltfDocument();
        byte[] json = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}");

        GltfConversionOptions noReadOptions = new GltfConversionOptions();
        noReadOptions.ReadOptions = null!;
        AssertArgument(() => converter.ConvertFile("input", "output", noReadOptions), "options");
        using MemoryStream readable = new MemoryStream(json);
        using MemoryStream writable = new MemoryStream();
        AssertArgument(() => converter.ConvertStream(readable, writable, noReadOptions), "options");

        GltfConversionOptions noWriteOptions = new GltfConversionOptions();
        noWriteOptions.WriteOptions = null!;
        AssertArgument(() => converter.ConvertFile("input", "output", noWriteOptions), "options");
        AssertArgument(() => converter.ConvertStream(readable, writable, noWriteOptions), "options");

        GltfConversionOptions noLimits = new GltfConversionOptions();
        noLimits.ReadOptions.Limits = null!;
        AssertArgument(() => converter.ConvertBytes(json, noLimits), "options");
        GltfConversionOptions noPolicy = new GltfConversionOptions();
        noPolicy.ReadOptions.ResourcePolicy = null!;
        AssertArgument(() => converter.ConvertDocument(document, noPolicy), "options");
        GltfConversionOptions noReadValidation = new GltfConversionOptions();
        noReadValidation.ReadOptions.ValidationOptions = null!;
        AssertArgument(() => converter.ConvertDocument(document, noReadValidation), "options");
        GltfConversionOptions noWriteValidation = new GltfConversionOptions();
        noWriteValidation.WriteOptions.ValidationOptions = null!;
        AssertArgument(() => converter.ConvertDocument(document, noWriteValidation), "options");
        GltfConversionOptions noTransformOptions = new GltfConversionOptions();
        noTransformOptions.TransformPlan = new GltfTransformPlan();
        noTransformOptions.TransformOptions = null!;
        AssertArgument(() => converter.ConvertDocument(document, noTransformOptions), "options");

        GltfConversionOptions invalidInputFormat = CreateConversionOptions();
        invalidInputFormat.InputFormat = (GltfContainerFormat)999;
        Assert.That(
            () => converter.ConvertDocument(document, invalidInputFormat),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("options"));
        GltfConversionOptions invalidOutputFormat = CreateConversionOptions();
        invalidOutputFormat.OutputFormat = (GltfContainerFormat)999;
        Assert.That(
            () => converter.ConvertDocument(document, invalidOutputFormat),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("options"));

        using HostileTestStream unreadable = new HostileTestStream(
            [],
            false,
            false,
            false,
            int.MaxValue,
            int.MaxValue);
        using HostileTestStream nonwritable = new HostileTestStream(
            [],
            true,
            false,
            false,
            int.MaxValue,
            int.MaxValue);
        AssertArgument(
            () => converter.ConvertStream(unreadable, writable, CreateConversionOptions()),
            "input");
        AssertArgument(
            () => converter.ConvertStream(readable, nonwritable, CreateConversionOptions()),
            "output");

        GltfTransformer transformer = new GltfTransformer();
        GltfTransformPlan noSteps = new GltfTransformPlan();
        noSteps.Steps = null!;
        AssertArgument(
            () => transformer.TransformDocument(document, noSteps, new GltfTransformOptions()),
            "plan");
        AssertInvalidTransformOptions(transformer, document, options => options.ValidationOptions = null!);
        AssertInvalidTransformOptions(
            transformer,
            document,
            options => options.ValidationOptions.ExtensionHandlers = null!);
        AssertInvalidTransformOptions(transformer, document, options => options.Hooks = null!);
        AssertInvalidTransformOptions(transformer, document, options => options.CompressionCodecs = null!);
        AssertInvalidTransformOptions(transformer, document, options => options.ImageCodecs = null!);
    }

    [Test]
    public void UnsupportedRequiredExtensionsUseUnavailableResultsOrStrictExceptions()
    {
        GltfDocument document = CreateUnsupportedDocument();
        GltfAnimationEvaluator animations = new GltfAnimationEvaluator();
        GltfSceneEvaluator scenes = new GltfSceneEvaluator();

        GltfScenePose animated = animations.ApplyAnimationPose(
            document,
            new GltfAnimationPose(),
            new GltfSceneEvaluationOptions());
        GltfProjectionResult projection = scenes.CalculateProjection(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(animated.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(
                animated.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.RequiredExtensionMetadataUnavailable));
            Assert.That(projection.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(
                projection.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.RequiredExtensionMetadataUnavailable));
            Assert.That(
                () => animations.CalculateAnimationDuration(document, 0),
                Throws.TypeOf<GltfUnsupportedFeatureException>());
            Assert.That(
                () => scenes.CalculateBounds(document, new GltfSceneEvaluationOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>());
            Assert.That(
                () => new GltfConverter().ConvertDocument(
                    document,
                    CreateConversionOptions()),
                Throws.TypeOf<GltfUnsupportedFeatureException>());
        });
    }

    [Test]
    public void ValidatorStrictAndCollectingModesCoverFilesBytesContainersAndDocuments()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        byte[] invalidJson = Encoding.UTF8.GetBytes("{\"asset\":{}}");
        string invalidPath = workspace.WriteBytes("invalid.gltf", invalidJson);
        string missingPath = workspace.GetPath("missing.gltf");
        string oversizedPath = workspace.GetPath("oversized.gltf");
        using (FileStream stream = new FileStream(
            oversizedPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            stream.SetLength((long)GltfConstants.DefaultMaximumInputByteLength + 1L);
        }

        GltfContainerData invalidContainer = new GltfContainerData();
        invalidContainer.Format = GltfContainerFormat.Json;
        invalidContainer.JsonBytes = invalidJson;
        GltfDocument invalidDocument = new GltfDocument();
        invalidDocument.Asset.Version = "";
        GltfValidationOptions strict = new GltfValidationOptions();
        strict.Strict = true;
        GltfValidator validator = new GltfValidator();

        GltfValidationReport missing = validator.ValidateFile(
            missingPath,
            new GltfValidationOptions());
        GltfValidationReport oversized = validator.ValidateFile(
            oversizedPath,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsValid, Is.False);
            Assert.That(missing.Issues[0].Code, Is.EqualTo(GltfIssueCodes.IoError));
            Assert.That(oversized.IsValid, Is.False);
            Assert.That(
                oversized.Issues[0].Code,
                Is.EqualTo(GltfIssueCodes.InputLimitExceeded));
            Assert.That(
                () => validator.ValidateFile(invalidPath, strict),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => validator.ValidateFile(missingPath, strict),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => validator.ValidateBytes(invalidJson, strict),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => validator.ValidateContainer(invalidContainer, strict),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => validator.ValidateDocument(invalidDocument, strict),
                Throws.TypeOf<GltfValidationException>());
        });
    }

    [Test]
    public void WriterAndConverterHonorStrictValidationAndValidPreflightModes()
    {
        GltfDocument invalidDocument = new GltfDocument();
        invalidDocument.Asset.Version = "";
        GltfWriteOptions strictWrite = new GltfWriteOptions();
        strictWrite.Validate = true;
        strictWrite.ValidationOptions.Strict = true;

        GltfConversionOptions strictConversion = CreateConversionOptions();
        strictConversion.ReadOptions.Validate = true;
        strictConversion.ReadOptions.ValidationOptions.Strict = true;

        GltfConversionOptions validConversion = CreateConversionOptions();
        validConversion.ReadOptions.Validate = true;
        GltfConversionResult valid = new GltfConverter().ConvertDocument(
            new GltfDocument(),
            validConversion);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfWriter().WriteBytes(invalidDocument, strictWrite),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => new GltfConverter().ConvertDocument(
                    invalidDocument,
                    strictConversion),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(valid.Status, Is.EqualTo(GltfOperationStatus.Success));
        });
    }

    [Test]
    public void MissingResourcesAndCodecsUseTheirDocumentedFailureMechanisms()
    {
        byte[] missingResourceJson = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"buffers\":[{\"uri\":\"missing.bin\",\"byteLength\":1}]}");
        TestResourceResolver resolver = new TestResourceResolver();
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;
        readOptions.ResourceResolver = resolver;
        GltfConversionOptions conversionOptions = CreateConversionOptions();
        conversionOptions.ReadOptions.ResolveResources = true;
        conversionOptions.ReadOptions.ResourceResolver = resolver;

        GltfResourceRequest networkRequest = new GltfResourceRequest();
        networkRequest.SourceUri = "https://example.invalid/missing.bin";
        GltfResource unavailableResource = new FileGltfResourceResolver()
            .ResolveResource(networkRequest);

        GltfImageRequest imageRequest = new GltfImageRequest();
        imageRequest.Bytes = [1, 2, 3];
        imageRequest.SourceFormat = GltfImageFormat.Custom;
        GltfImageResult image = new GltfImageCodec().DecodeImage(imageRequest);

        WriterRecordingValidator recordingValidator = new WriterRecordingValidator();
        recordingValidator.Report.IsValid = false;
        GltfWriter writer = new GltfWriter();
        writer.Validator = recordingValidator;
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Validate = true;
        using MemoryStream output = new MemoryStream();
        GltfWriteResult write = writer.WriteStream(
            new GltfDocument(),
            output,
            writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfReader().ReadBytes(missingResourceJson, readOptions),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.Contains("status Unavailable"));
            Assert.That(
                () => new GltfConverter().ConvertBytes(
                    missingResourceJson,
                    conversionOptions),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.Contains("status Unavailable"));
            Assert.That(
                unavailableResource.Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(image.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(image.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.ImageCodecUnavailable));
            Assert.That(write.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void ImageCodecRejectsEveryInvalidOperationRequestShape()
    {
        GltfImageCodec codec = new GltfImageCodec();
        AssertArgumentNull(() => codec.InspectImage(null!), "request");
        AssertArgumentNull(() => codec.DecodeImage(null!), "request");
        AssertArgumentNull(() => codec.EncodeImage(null!), "request");
        AssertArgumentNull(() => codec.TranscodeImage(null!), "request");

        GltfImageRequest nullBytes = new GltfImageRequest();
        nullBytes.Bytes = null!;
        AssertArgument(() => codec.DecodeImage(nullBytes), "request");
        GltfImageRequest negativeLimit = new GltfImageRequest();
        negativeLimit.MaximumOutputByteLength = -1;
        Assert.That(
            () => codec.EncodeImage(negativeLimit),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("request"));
        GltfImageRequest invalidSource = new GltfImageRequest();
        invalidSource.SourceFormat = (GltfImageFormat)999;
        Assert.That(
            () => codec.TranscodeImage(invalidSource),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        GltfImageRequest invalidTarget = new GltfImageRequest();
        invalidTarget.TargetFormat = (GltfImageFormat)999;
        Assert.That(
            () => codec.TranscodeImage(invalidTarget),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        GltfImageRequest invalidWidth = new GltfImageRequest();
        invalidWidth.HasTargetWidth = true;
        Assert.That(
            () => codec.EncodeImage(invalidWidth),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        GltfImageRequest invalidHeight = new GltfImageRequest();
        invalidHeight.HasTargetHeight = true;
        Assert.That(
            () => codec.EncodeImage(invalidHeight),
            Throws.TypeOf<ArgumentOutOfRangeException>());

        GltfImageRequest heightChange = new GltfImageRequest();
        heightChange.Bytes = CreatePngBytes();
        heightChange.TargetFormat = GltfImageFormat.Png;
        heightChange.HasTargetHeight = true;
        heightChange.TargetHeight = 4;
        Assert.That(
            codec.TranscodeImage(heightChange).Status,
            Is.EqualTo(GltfOperationStatus.Unavailable));
    }

    [Test]
    public void WriterRejectsNonWritableStreamsForDocumentAndContainerOperations()
    {
        using HostileTestStream stream = new HostileTestStream(
            [],
            true,
            false,
            false,
            int.MaxValue,
            int.MaxValue);
        GltfWriter writer = new GltfWriter();

        AssertArgument(
            () => writer.WriteStream(
                new GltfDocument(),
                stream,
                new GltfWriteOptions()),
            "stream");
        AssertArgument(
            () => writer.WriteContainerStream(
                CreateJsonContainer(),
                stream,
                new GltfWriteOptions()),
            "stream");
    }

    private static GltfConversionOptions CreateConversionOptions()
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Json;
        result.OutputFormat = GltfContainerFormat.Json;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static GltfContainerData CreateJsonContainer()
    {
        GltfContainerData result = new GltfContainerData();
        result.Format = GltfContainerFormat.Json;
        result.JsonBytes = Encoding.UTF8.GetBytes("{}");
        return result;
    }

    private static GltfDocument CreateUnsupportedDocument()
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add("FUTURE_required_feature");
        result.ExtensionsRequired.Add("FUTURE_required_feature");
        return result;
    }

    private static byte[] CreatePngBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0x00, 0x00, 0x00, 0x00
        ];
    }

    private static void AssertInvalidTransformOptions(
        GltfTransformer transformer,
        GltfDocument document,
        Action<GltfTransformOptions> mutate)
    {
        GltfTransformOptions options = new GltfTransformOptions();
        mutate(options);
        AssertArgument(
            () => transformer.TransformDocument(
                document,
                new GltfTransformPlan(),
                options),
            "options");
    }

    private static void AssertArgumentNull(TestDelegate action, string parameterName)
    {
        Assert.That(
            action,
            Throws.TypeOf<ArgumentNullException>()
                .With.Property("ParamName").EqualTo(parameterName));
    }

    private static void AssertArgument(TestDelegate action, string parameterName)
    {
        Assert.That(
            action,
            Throws.TypeOf<ArgumentException>()
                .With.Property("ParamName").EqualTo(parameterName));
    }
}
