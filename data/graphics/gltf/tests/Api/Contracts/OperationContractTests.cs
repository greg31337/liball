using System.Collections.Generic;
using System.IO;

namespace gltf.Tests.Api.Contracts;

[TestFixture]
public class OperationContractTests
{
    [Test]
    public void IoContracts_DispatchEveryFileStreamByteAndModelWorkflow()
    {
        ContractProbe probe = new ContractProbe();
        IGltfReader reader = probe;
        IGltfWriter writer = probe;
        IGltfConverter converter = probe;
        GltfReadOptions readOptions = new GltfReadOptions();
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        GltfConversionOptions conversionOptions = new GltfConversionOptions();
        GltfDocument document = new GltfDocument();
        GltfContainerData container = new GltfContainerData();
        byte[] bytes = [1, 2, 3];
        using MemoryStream input = new MemoryStream(bytes);
        using MemoryStream output = new MemoryStream();

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadFile("input.gltf", readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(reader.ReadStream(input, readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(reader.ReadContainerStream(input, readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(reader.ReadBytes(bytes, readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(reader.ReadContainerBytes(bytes, readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(reader.ReadContainer(container, readOptions), Is.SameAs(probe.ReadResult));
            Assert.That(writer.WriteFile(document, "output.glb", writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(writer.WriteStream(document, output, writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(writer.WriteContainerStream(container, output, writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(writer.WriteBytes(document, writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(writer.WriteContainerBytes(container, writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(writer.WriteJsonText(document, writeOptions), Is.SameAs(probe.WriteResult));
            Assert.That(converter.ConvertFile("input.gltf", "output.glb", conversionOptions), Is.SameAs(probe.ConversionResult));
            Assert.That(converter.ConvertStream(input, output, conversionOptions), Is.SameAs(probe.ConversionResult));
            Assert.That(converter.ConvertBytes(bytes, conversionOptions), Is.SameAs(probe.ConversionResult));
            Assert.That(converter.ConvertDocument(document, conversionOptions), Is.SameAs(probe.ConversionResult));
        });

        Assert.That(probe.CallCount, Is.EqualTo(16));
    }

    [Test]
    public void ValidationAndResolutionContracts_DispatchResultBasedOperations()
    {
        ContractProbe probe = new ContractProbe();
        IGltfValidator validator = probe;
        IGltfResourceResolver resolver = probe;
        GltfValidationOptions options = new GltfValidationOptions();
        GltfDocument document = new GltfDocument();
        GltfContainerData container = new GltfContainerData();

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateDocument(document, options), Is.SameAs(probe.ValidationReport));
            Assert.That(validator.ValidateFile("input.gltf", options), Is.SameAs(probe.ValidationReport));
            Assert.That(validator.ValidateBytes([1, 2, 3], options), Is.SameAs(probe.ValidationReport));
            Assert.That(validator.ValidateContainer(container, options), Is.SameAs(probe.ValidationReport));
            Assert.That(resolver.ResolveResource(new GltfResourceRequest()), Is.SameAs(probe.Resource));
        });

        Assert.That(probe.CallCount, Is.EqualTo(5));
    }

    [Test]
    public void AccessorAndEvaluationContracts_DispatchDistinctTypedOperations()
    {
        ContractProbe probe = new ContractProbe();
        IGltfAccessorCodec accessorCodec = probe;
        IGltfSceneEvaluator sceneEvaluator = probe;
        IGltfAnimationEvaluator animationEvaluator = probe;
        GltfAccessorReadRequest readRequest = new GltfAccessorReadRequest();
        GltfAccessorWriteRequest writeRequest = new GltfAccessorWriteRequest();
        GltfDocument document = new GltfDocument();
        GltfSceneEvaluationOptions sceneOptions = new GltfSceneEvaluationOptions();
        GltfAnimationEvaluationOptions animationOptions = new GltfAnimationEvaluationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(accessorCodec.ReadFloatingValues(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.ReadSignedValues(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.ReadUnsignedValues(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.ReadIndices(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.ReadMatrices(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.ReadRawElements(readRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.CreateDenseAccessor(writeRequest), Is.SameAs(probe.AccessorData));
            Assert.That(accessorCodec.CreateSparseAccessor(writeRequest), Is.SameAs(probe.AccessorData));
            Assert.That(sceneEvaluator.EvaluateScene(document, sceneOptions), Is.SameAs(probe.ScenePose));
            Assert.That(sceneEvaluator.CalculateBounds(document, sceneOptions), Is.SameAs(probe.Bounds));
            Assert.That(sceneEvaluator.CalculateProjection(document, 0), Is.SameAs(probe.ProjectionResult));
            Assert.That(animationEvaluator.SampleAnimation(document, animationOptions), Is.SameAs(probe.AnimationPose));
            Assert.That(animationEvaluator.ApplyAnimationPose(document, probe.AnimationPose, sceneOptions), Is.SameAs(probe.ScenePose));
            Assert.That(animationEvaluator.CalculateAnimationDuration(document, 0), Is.EqualTo(2.5));
        });

        Assert.That(probe.CallCount, Is.EqualTo(14));
    }

    [Test]
    public void ExtensionAndCodecContracts_DispatchAllCapabilityOperations()
    {
        ContractProbe probe = new ContractProbe();
        IGltfExtensionHandler extensionHandler = probe;
        IGltfCompressionCodec compressionCodec = probe;
        IGltfImageCodec imageCodec = probe;
        GltfExtensionContext extensionContext = new GltfExtensionContext();
        GltfCompressionRequest compressionRequest = new GltfCompressionRequest();
        GltfImageRequest imageRequest = new GltfImageRequest();

        Assert.Multiple(() =>
        {
            Assert.That(extensionHandler.DescribeExtension(), Is.SameAs(probe.ExtensionDescriptor));
            Assert.That(extensionHandler.ReadExtension(extensionContext), Is.SameAs(probe.ExtensionResult));
            Assert.That(extensionHandler.WriteExtension(extensionContext), Is.SameAs(probe.ExtensionResult));
            Assert.That(extensionHandler.ValidateExtension(extensionContext), Is.SameAs(probe.ExtensionResult));
            Assert.That(extensionHandler.RemapExtensionReferences(extensionContext, new GltfIndexRemap()), Is.SameAs(probe.ExtensionResult));
            Assert.That(compressionCodec.DecodeGeometry(compressionRequest), Is.SameAs(probe.CompressionResult));
            Assert.That(compressionCodec.EncodeGeometry(compressionRequest), Is.SameAs(probe.CompressionResult));
            Assert.That(compressionCodec.DecodeMeshoptData(compressionRequest), Is.SameAs(probe.CompressionResult));
            Assert.That(compressionCodec.EncodeMeshoptData(compressionRequest), Is.SameAs(probe.CompressionResult));
            Assert.That(imageCodec.InspectImage(imageRequest), Is.SameAs(probe.ImageInfo));
            Assert.That(imageCodec.DecodeImage(imageRequest), Is.SameAs(probe.ImageResult));
            Assert.That(imageCodec.EncodeImage(imageRequest), Is.SameAs(probe.ImageResult));
            Assert.That(imageCodec.TranscodeImage(imageRequest), Is.SameAs(probe.ImageResult));
        });

        Assert.That(probe.CallCount, Is.EqualTo(13));
    }

    [Test]
    public void EditingTransformAndInspectionContracts_ComposeOnOneImplementation()
    {
        ContractProbe probe = new ContractProbe();
        IGltfDocumentEditor editor = probe;
        IGltfTransformer transformer = probe;
        IGltfTransformHook transformHook = probe;
        IGltfInspector inspector = probe;
        GltfDocument document = new GltfDocument();
        GltfEditOptions editOptions = new GltfEditOptions();
        GltfInspectionOptions inspectionOptions = new GltfInspectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(editor.CreateDocument(), Is.SameAs(probe.Document));
            Assert.That(editor.CloneDocument(document, editOptions), Is.SameAs(probe.Document));
            Assert.That(editor.MergeDocument([document], editOptions), Is.SameAs(probe.MergeResult));
            Assert.That(editor.CompactDocument(document, editOptions), Is.SameAs(probe.MergeResult));
            Assert.That(editor.RebuildReferences(document, editOptions), Is.SameAs(probe.MergeResult));
            Assert.That(transformer.TransformDocument(document, new GltfTransformPlan(), new GltfTransformOptions()), Is.SameAs(probe.TransformReport));
            Assert.That(transformHook.ApplyTransformHook(new GltfTransformContext()), Is.SameAs(probe.TransformStepResult));
            Assert.That(inspector.InspectDocument(document, inspectionOptions), Is.SameAs(probe.InspectionResult));
            Assert.That(inspector.EnumerateDependencies(document, inspectionOptions), Is.SameAs(probe.Dependencies));
        });

        Assert.That(probe.CallCount, Is.EqualTo(9));
    }

    private class ContractProbe : IGltfReader, IGltfWriter, IGltfResourceResolver, IGltfValidator,
        IGltfAccessorCodec, IGltfSceneEvaluator, IGltfAnimationEvaluator, IGltfExtensionHandler,
        IGltfCompressionCodec, IGltfImageCodec, IGltfDocumentEditor, IGltfTransformer,
        IGltfTransformHook, IGltfInspector, IGltfConverter
    {
        public int CallCount;
        public GltfReadResult ReadResult = new GltfReadResult();
        public GltfWriteResult WriteResult = new GltfWriteResult();
        public GltfResource Resource = new GltfResource();
        public GltfValidationReport ValidationReport = new GltfValidationReport();
        public GltfAccessorData AccessorData = new GltfAccessorData();
        public GltfScenePose ScenePose = new GltfScenePose();
        public GltfBounds Bounds = new GltfBounds();
        public GltfProjectionResult ProjectionResult = new GltfProjectionResult();
        public GltfAnimationPose AnimationPose = new GltfAnimationPose();
        public GltfExtensionDescriptor ExtensionDescriptor = new GltfExtensionDescriptor();
        public GltfExtensionResult ExtensionResult = new GltfExtensionResult();
        public GltfCompressionResult CompressionResult = new GltfCompressionResult();
        public GltfImageInfo ImageInfo = new GltfImageInfo();
        public GltfImageResult ImageResult = new GltfImageResult();
        public GltfDocument Document = new GltfDocument();
        public GltfMergeResult MergeResult = new GltfMergeResult();
        public GltfTransformReport TransformReport = new GltfTransformReport();
        public GltfTransformStepResult TransformStepResult = new GltfTransformStepResult();
        public GltfInspectionResult InspectionResult = new GltfInspectionResult();
        public List<GltfDependency> Dependencies = new List<GltfDependency>();
        public GltfConversionResult ConversionResult = new GltfConversionResult();

        public GltfReadResult ReadFile(string path, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfReadResult ReadStream(Stream stream, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfReadResult ReadContainerStream(Stream stream, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfReadResult ReadBytes(byte[] bytes, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfReadResult ReadContainerBytes(byte[] bytes, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfReadResult ReadContainer(GltfContainerData container, GltfReadOptions options)
        {
            CallCount++;
            return ReadResult;
        }

        public GltfWriteResult WriteFile(GltfDocument document, string path, GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfWriteResult WriteStream(GltfDocument document, Stream stream, GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfWriteResult WriteContainerStream(
            GltfContainerData container,
            Stream stream,
            GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfWriteResult WriteBytes(GltfDocument document, GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfWriteResult WriteContainerBytes(GltfContainerData container, GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfWriteResult WriteJsonText(GltfDocument document, GltfWriteOptions options)
        {
            CallCount++;
            return WriteResult;
        }

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            return Resource;
        }

        public GltfValidationReport ValidateDocument(GltfDocument document, GltfValidationOptions options)
        {
            CallCount++;
            return ValidationReport;
        }

        public GltfValidationReport ValidateFile(string path, GltfValidationOptions options)
        {
            CallCount++;
            return ValidationReport;
        }

        public GltfValidationReport ValidateBytes(byte[] bytes, GltfValidationOptions options)
        {
            CallCount++;
            return ValidationReport;
        }

        public GltfValidationReport ValidateContainer(GltfContainerData container, GltfValidationOptions options)
        {
            CallCount++;
            return ValidationReport;
        }

        public GltfAccessorData ReadFloatingValues(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData ReadSignedValues(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData ReadUnsignedValues(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData ReadIndices(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData ReadMatrices(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData ReadRawElements(GltfAccessorReadRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData CreateDenseAccessor(GltfAccessorWriteRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfAccessorData CreateSparseAccessor(GltfAccessorWriteRequest request)
        {
            CallCount++;
            return AccessorData;
        }

        public GltfScenePose EvaluateScene(GltfDocument document, GltfSceneEvaluationOptions options)
        {
            CallCount++;
            return ScenePose;
        }

        public GltfBounds CalculateBounds(GltfDocument document, GltfSceneEvaluationOptions options)
        {
            CallCount++;
            return Bounds;
        }

        public GltfProjectionResult CalculateProjection(GltfDocument document, int camera)
        {
            CallCount++;
            return ProjectionResult;
        }

        public GltfAnimationPose SampleAnimation(GltfDocument document, GltfAnimationEvaluationOptions options)
        {
            CallCount++;
            return AnimationPose;
        }

        public GltfScenePose ApplyAnimationPose(GltfDocument document, GltfAnimationPose pose, GltfSceneEvaluationOptions options)
        {
            CallCount++;
            return ScenePose;
        }

        public double CalculateAnimationDuration(GltfDocument document, int animation)
        {
            CallCount++;
            return 2.5;
        }

        public GltfExtensionDescriptor DescribeExtension()
        {
            CallCount++;
            return ExtensionDescriptor;
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            CallCount++;
            return ExtensionResult;
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            CallCount++;
            return ExtensionResult;
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            CallCount++;
            return ExtensionResult;
        }

        public GltfExtensionResult RemapExtensionReferences(GltfExtensionContext context, GltfIndexRemap remap)
        {
            CallCount++;
            return ExtensionResult;
        }

        public GltfCompressionResult DecodeGeometry(GltfCompressionRequest request)
        {
            CallCount++;
            return CompressionResult;
        }

        public GltfCompressionResult EncodeGeometry(GltfCompressionRequest request)
        {
            CallCount++;
            return CompressionResult;
        }

        public GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request)
        {
            CallCount++;
            return CompressionResult;
        }

        public GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request)
        {
            CallCount++;
            return CompressionResult;
        }

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            CallCount++;
            return ImageInfo;
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            CallCount++;
            return ImageResult;
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            CallCount++;
            return ImageResult;
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            CallCount++;
            return ImageResult;
        }

        public GltfDocument CreateDocument()
        {
            CallCount++;
            return Document;
        }

        public GltfDocument CloneDocument(GltfDocument document, GltfEditOptions options)
        {
            CallCount++;
            return Document;
        }

        public GltfMergeResult MergeDocument(GltfDocument[] documents, GltfEditOptions options)
        {
            CallCount++;
            return MergeResult;
        }

        public GltfMergeResult CompactDocument(GltfDocument document, GltfEditOptions options)
        {
            CallCount++;
            return MergeResult;
        }

        public GltfMergeResult RebuildReferences(GltfDocument document, GltfEditOptions options)
        {
            CallCount++;
            return MergeResult;
        }

        public GltfTransformReport TransformDocument(GltfDocument document, GltfTransformPlan plan, GltfTransformOptions options)
        {
            CallCount++;
            return TransformReport;
        }

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            CallCount++;
            return TransformStepResult;
        }

        public GltfInspectionResult InspectDocument(GltfDocument document, GltfInspectionOptions options)
        {
            CallCount++;
            return InspectionResult;
        }

        public List<GltfDependency> EnumerateDependencies(GltfDocument document, GltfInspectionOptions options)
        {
            CallCount++;
            return Dependencies;
        }

        public GltfConversionResult ConvertFile(string inputPath, string outputPath, GltfConversionOptions options)
        {
            CallCount++;
            return ConversionResult;
        }

        public GltfConversionResult ConvertStream(Stream input, Stream output, GltfConversionOptions options)
        {
            CallCount++;
            return ConversionResult;
        }

        public GltfConversionResult ConvertBytes(byte[] bytes, GltfConversionOptions options)
        {
            CallCount++;
            return ConversionResult;
        }

        public GltfConversionResult ConvertDocument(GltfDocument document, GltfConversionOptions options)
        {
            CallCount++;
            return ConversionResult;
        }
    }
}
