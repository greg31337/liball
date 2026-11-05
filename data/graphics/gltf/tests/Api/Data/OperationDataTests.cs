using System.Numerics;

namespace gltf.Tests.Api.Data;

[TestFixture]
public class OperationDataTests
{
    [Test]
    public void ContainerAndResourceData_HaveIndependentOwnedDefaults()
    {
        GltfContainerData firstContainer = new GltfContainerData();
        GltfContainerData secondContainer = new GltfContainerData();
        GltfResource firstResource = new GltfResource();
        GltfResourceRequest request = new GltfResourceRequest();
        GltfResourceRequest secondRequest = new GltfResourceRequest();
        GltfResourceSet firstSet = new GltfResourceSet();
        GltfResourceSet secondSet = new GltfResourceSet();
        GltfResourceManifestEntry manifestEntry = new GltfResourceManifestEntry();

        firstContainer.UnknownChunks.Add(new GltfGlbChunk());
        firstSet.Resources.Add(firstResource);
        firstSet.Cache["resource"] = firstResource;

        Assert.Multiple(() =>
        {
            Assert.That(firstContainer.Format, Is.EqualTo(GltfContainerFormat.Auto));
            Assert.That(firstContainer.JsonBytes, Is.Empty);
            Assert.That(firstContainer.HasBinChunk, Is.False);
            Assert.That(firstContainer.BinBytes, Is.Empty);
            Assert.That(firstContainer.TrailingBytes, Is.Empty);
            Assert.That(firstContainer.OriginalByteLength, Is.Zero);
            Assert.That(secondContainer.UnknownChunks, Is.Empty);
            Assert.That(firstContainer.UnknownChunks, Is.Not.SameAs(secondContainer.UnknownChunks));
            Assert.That(firstContainer.UnknownChunks[0].Data, Is.Empty);
            Assert.That(firstResource.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstResource.Kind, Is.EqualTo(GltfResourceKind.Other));
            Assert.That(firstResource.UriKind, Is.EqualTo(GltfUriKind.Absent));
            Assert.That(firstResource.ImageFormat, Is.EqualTo(GltfImageFormat.Unknown));
            Assert.That(firstResource.Bytes, Is.Empty);
            Assert.That(firstResource.MimeType, Is.Null);
            Assert.That(firstResource.DeclaredMimeType, Is.Null);
            Assert.That(firstResource.SourceMimeType, Is.Null);
            Assert.That(request.Kind, Is.EqualTo(GltfResourceKind.Other));
            Assert.That(request.MaximumByteLength, Is.EqualTo(GltfConstants.DefaultMaximumResourceByteLength));
            Assert.That(request.Policy.FileRoots, Is.Empty);
            Assert.That(request.Policy.AllowAbsolutePaths, Is.False);
            Assert.That(request.Policy.AllowParentPathSegments, Is.False);
            Assert.That(request.Policy, Is.Not.SameAs(secondRequest.Policy));
            Assert.That(secondSet.Resources, Is.Empty);
            Assert.That(secondSet.Cache, Is.Empty);
            Assert.That(firstSet.Resources, Is.Not.SameAs(secondSet.Resources));
            Assert.That(firstSet.Cache, Is.Not.SameAs(secondSet.Cache));
            Assert.That(manifestEntry.Kind, Is.EqualTo(GltfResourceKind.Other));
            Assert.That(manifestEntry.Mode, Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(manifestEntry.ByteLength, Is.Zero);
            Assert.That(manifestEntry.EffectiveUri, Is.Null);
            Assert.That(manifestEntry.Embedded, Is.False);
            Assert.That(manifestEntry.External, Is.False);
        });
    }

    [Test]
    public void ReadAndWriteOptions_ExposeSafeDeterministicDefaults()
    {
        GltfReadOptions firstRead = new GltfReadOptions();
        GltfReadOptions secondRead = new GltfReadOptions();
        GltfReadLimits limits = firstRead.Limits;
        GltfWriteOptions firstWrite = new GltfWriteOptions();
        GltfWriteOptions secondWrite = new GltfWriteOptions();

        firstRead.ExtensionHandlers.Add(null!);
        firstWrite.ImageCodecs.Add(null!);

        Assert.Multiple(() =>
        {
            Assert.That(firstRead.Format, Is.EqualTo(GltfContainerFormat.Auto));
            Assert.That(firstRead.DuplicateMemberPolicy, Is.EqualTo(GltfDuplicateMemberPolicy.Reject));
            Assert.That(firstRead.PreserveUnknownMembers, Is.True);
            Assert.That(firstRead.PreserveUnknownChunks, Is.True);
            Assert.That(firstRead.ResolveResources, Is.False);
            Assert.That(firstRead.Validate, Is.False);
            Assert.That(firstRead.LeaveStreamOpen, Is.True);
            Assert.That(firstRead.ResourceResolver, Is.Null);
            Assert.That(firstRead.ResourcePolicy.FileRoots, Is.Empty);
            Assert.That(firstRead.ResourcePolicy.AllowAbsolutePaths, Is.False);
            Assert.That(firstRead.ResourcePolicy.AllowParentPathSegments, Is.False);
            Assert.That(firstRead.ResourcePolicy, Is.Not.SameAs(secondRead.ResourcePolicy));
            Assert.That(limits.MaximumInputByteLength, Is.EqualTo(GltfConstants.DefaultMaximumInputByteLength));
            Assert.That(limits.MaximumJsonByteLength, Is.EqualTo(GltfConstants.DefaultMaximumJsonByteLength));
            Assert.That(limits.MaximumJsonDepth, Is.EqualTo(GltfConstants.DefaultMaximumJsonDepth));
            Assert.That(limits.MaximumJsonTokenCount, Is.EqualTo(GltfConstants.DefaultMaximumJsonTokenCount));
            Assert.That(limits.MaximumJsonStringByteLength, Is.EqualTo(GltfConstants.DefaultMaximumJsonStringByteLength));
            Assert.That(limits.MaximumJsonNumberByteLength, Is.EqualTo(GltfConstants.DefaultMaximumJsonNumberByteLength));
            Assert.That(limits.MaximumCollectionCount, Is.EqualTo(GltfConstants.DefaultMaximumCollectionCount));
            Assert.That(limits.MaximumDataUriByteLength, Is.EqualTo(GltfConstants.DefaultMaximumDataUriByteLength));
            Assert.That(limits.MaximumResourceByteLength, Is.EqualTo(GltfConstants.DefaultMaximumResourceByteLength));
            Assert.That(limits.MaximumTotalResourceByteLength, Is.EqualTo(GltfConstants.DefaultMaximumTotalResourceByteLength));
            Assert.That(secondRead.ExtensionHandlers, Is.Empty);
            Assert.That(firstRead.Limits, Is.Not.SameAs(secondRead.Limits));
            Assert.That(firstRead.ValidationOptions, Is.Not.SameAs(secondRead.ValidationOptions));
            Assert.That(firstWrite.Format, Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(firstWrite.ResourceMode, Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(firstWrite.Indented, Is.False);
            Assert.That(firstWrite.Deterministic, Is.True);
            Assert.That(firstWrite.PreserveExplicitDefaults, Is.True);
            Assert.That(firstWrite.PreserveUnknownMembers, Is.True);
            Assert.That(firstWrite.PreserveUnknownChunks, Is.True);
            Assert.That(firstWrite.Validate, Is.True);
            Assert.That(firstWrite.LeaveStreamOpen, Is.True);
            Assert.That(secondWrite.ImageCodecs, Is.Empty);
            Assert.That(firstWrite.ValidationOptions, Is.Not.SameAs(secondWrite.ValidationOptions));
        });
    }

    [Test]
    public void ReadAndWriteResults_OwnTheirMutableOutputState()
    {
        GltfReadResult firstRead = new GltfReadResult();
        GltfReadResult secondRead = new GltfReadResult();
        GltfWriteResult firstWrite = new GltfWriteResult();
        GltfWriteResult secondWrite = new GltfWriteResult();

        firstRead.Diagnostics.Add(new GltfValidationIssue());
        firstWrite.Manifest.Add(new GltfResourceManifestEntry());

        Assert.Multiple(() =>
        {
            Assert.That(firstRead.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstRead.Document, Is.Null);
            Assert.That(firstRead.Container, Is.Null);
            Assert.That(secondRead.Diagnostics, Is.Empty);
            Assert.That(firstRead.Resources, Is.Not.SameAs(secondRead.Resources));
            Assert.That(firstRead.Validation, Is.Not.SameAs(secondRead.Validation));
            Assert.That(firstWrite.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstWrite.PrimaryBytes, Is.Empty);
            Assert.That(firstWrite.JsonText, Is.Null);
            Assert.That(secondWrite.Manifest, Is.Empty);
            Assert.That(firstWrite.Resources, Is.Not.SameAs(secondWrite.Resources));
            Assert.That(firstWrite.Validation, Is.Not.SameAs(secondWrite.Validation));
            Assert.That(firstWrite.Diagnostics, Is.Not.SameAs(secondWrite.Diagnostics));
        });
    }

    [Test]
    public void ValidationData_HasUnambiguousEmptyReportDefaults()
    {
        GltfValidationOptions firstOptions = new GltfValidationOptions();
        GltfValidationOptions secondOptions = new GltfValidationOptions();
        GltfValidationIssue firstIssue = new GltfValidationIssue();
        GltfValidationIssue secondIssue = new GltfValidationIssue();
        GltfValidationReport firstReport = new GltfValidationReport();
        GltfValidationReport secondReport = new GltfValidationReport();
        GltfDocumentStatistics statistics = firstReport.Statistics;

        firstOptions.SuppressedIssueCodes.Add("TEST");
        firstIssue.RelatedJsonPointers.Add("/nodes/0");
        firstReport.Issues.Add(firstIssue);
        statistics.Extensions.Add("VENDOR_test");

        Assert.Multiple(() =>
        {
            Assert.That(firstOptions.ValidateStructure, Is.True);
            Assert.That(firstOptions.ValidateSemantics, Is.True);
            Assert.That(firstOptions.ValidateResources, Is.True);
            Assert.That(firstOptions.ValidateExtensions, Is.True);
            Assert.That(firstOptions.ImageCodecs, Is.Empty);
            Assert.That(firstOptions.ImageCodecs, Is.Not.SameAs(secondOptions.ImageCodecs));
            Assert.That(firstOptions.FailFast, Is.False);
            Assert.That(firstOptions.Strict, Is.False);
            Assert.That(firstOptions.MinimumSeverity, Is.EqualTo(GltfValidationSeverity.Information));
            Assert.That(firstOptions.MaximumIssueCount, Is.EqualTo(GltfConstants.DefaultMaximumIssueCount));
            Assert.That(secondOptions.SuppressedIssueCodes, Is.Empty);
            Assert.That(firstOptions.SeverityOverrides, Is.Not.SameAs(secondOptions.SeverityOverrides));
            Assert.That(firstIssue.Code, Is.Empty);
            Assert.That(firstIssue.Severity, Is.EqualTo(GltfValidationSeverity.Error));
            Assert.That(firstIssue.Message, Is.Empty);
            Assert.That(firstIssue.JsonPointer, Is.Empty);
            Assert.That(firstIssue.HasByteOffset, Is.False);
            Assert.That(firstIssue.ResourceUri, Is.Null);
            Assert.That(secondIssue.RelatedJsonPointers, Is.Empty);
            Assert.That(firstReport.IsValid, Is.True);
            Assert.That(firstReport.IssueLimitReached, Is.False);
            Assert.That(secondReport.Issues, Is.Empty);
            Assert.That(firstReport.Statistics, Is.Not.SameAs(secondReport.Statistics));
            Assert.That(statistics.AccessorCount, Is.Zero);
            Assert.That(statistics.VertexCount, Is.Zero);
            Assert.That(statistics.AnimationDuration, Is.Zero);
            Assert.That(statistics.Bounds, Is.Null);
        });
    }

    [Test]
    public void AccessorOperationData_DefaultsToBoundedEmptyValues()
    {
        GltfAccessorReadRequest readRequest = new GltfAccessorReadRequest();
        GltfAccessorData firstData = new GltfAccessorData();
        GltfAccessorData secondData = new GltfAccessorData();
        GltfAccessorWriteRequest writeRequest = new GltfAccessorWriteRequest();

        Assert.Multiple(() =>
        {
            Assert.That(readRequest.Document, Is.Null);
            Assert.That(readRequest.Accessor, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(readRequest.ApplyNormalization, Is.True);
            Assert.That(readRequest.ApplySparseValues, Is.True);
            Assert.That(readRequest.MaximumElementCount, Is.EqualTo(GltfConstants.DefaultMaximumCollectionCount));
            Assert.That(firstData.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstData.HasAccessor, Is.False);
            Assert.That(firstData.Accessor, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(firstData.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(firstData.Type, Is.EqualTo(GltfAccessorType.Scalar));
            Assert.That(firstData.Count, Is.Zero);
            Assert.That(firstData.ComponentCount, Is.Zero);
            Assert.That(firstData.FloatingValues, Is.Empty);
            Assert.That(firstData.SignedValues, Is.Empty);
            Assert.That(firstData.UnsignedValues, Is.Empty);
            Assert.That(firstData.Indices, Is.Empty);
            Assert.That(firstData.MatrixValues, Is.Empty);
            Assert.That(firstData.RawBytes, Is.Empty);
            Assert.That(firstData.Diagnostic, Is.Null);
            Assert.That(writeRequest.Document, Is.Null);
            Assert.That(writeRequest.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(writeRequest.Type, Is.EqualTo(GltfAccessorType.Scalar));
            Assert.That(writeRequest.AllowSparse, Is.False);
            Assert.That(writeRequest.SparseZeroTolerance, Is.Zero);
            Assert.That(writeRequest.CalculateBounds, Is.True);
            Assert.That(firstData, Is.Not.SameAs(secondData));
        });
    }

    [Test]
    public void SceneEvaluationData_DefaultsToIdentityAndEmptyBounds()
    {
        GltfSceneEvaluationOptions options = new GltfSceneEvaluationOptions();
        GltfScenePose firstPose = new GltfScenePose();
        GltfScenePose secondPose = new GltfScenePose();
        GltfNodePose firstNode = new GltfNodePose();
        GltfNodePose secondNode = new GltfNodePose();
        GltfProjectionResult projection = new GltfProjectionResult();

        firstPose.Nodes.Add(firstNode);
        firstNode.SkinMatrices.Add(Matrix4x4.Identity);

        Assert.Multiple(() =>
        {
            Assert.That(options.HasScene, Is.False);
            Assert.That(options.Scene, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(options.CalculateSkins, Is.True);
            Assert.That(options.CalculateCameras, Is.True);
            Assert.That(options.CalculateBounds, Is.True);
            Assert.That(options.IncludeMorphTargets, Is.False);
            Assert.That(firstPose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstPose.Scene, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(firstPose.HasBounds, Is.False);
            Assert.That(firstPose.Bounds.IsEmpty, Is.True);
            Assert.That(firstPose.Bounds.Minimum, Is.EqualTo(Vector3.Zero));
            Assert.That(firstPose.Bounds.Maximum, Is.EqualTo(Vector3.Zero));
            Assert.That(secondPose.Nodes, Is.Empty);
            Assert.That(firstPose.Bounds, Is.Not.SameAs(secondPose.Bounds));
            Assert.That(firstNode.Node, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(firstNode.LocalTransform, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(firstNode.WorldTransform, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(firstNode.NormalTransform, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(firstNode.Projection, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(secondNode.SkinMatrices, Is.Empty);
            Assert.That(projection.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(projection.Projection, Is.EqualTo(Matrix4x4.Identity));
            Assert.That(projection.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AnimationEvaluationData_OwnsSampledValueMaps()
    {
        GltfAnimationEvaluationOptions options = new GltfAnimationEvaluationOptions();
        GltfAnimationPose firstPose = new GltfAnimationPose();
        GltfAnimationPose secondPose = new GltfAnimationPose();

        firstPose.Translations[0] = new[] { 1.0, 2.0, 3.0 };
        firstPose.PointerValues["/materials/0"] = new[] { 0.5 };

        Assert.Multiple(() =>
        {
            Assert.That(options.Animation, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(options.Time, Is.Zero);
            Assert.That(options.WrapMode, Is.EqualTo(GltfAnimationWrapMode.Clamp));
            Assert.That(options.NormalizeRotations, Is.True);
            Assert.That(firstPose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstPose.Animation, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(firstPose.Time, Is.Zero);
            Assert.That(firstPose.Rotations, Is.Empty);
            Assert.That(firstPose.Scales, Is.Empty);
            Assert.That(firstPose.Weights, Is.Empty);
            Assert.That(firstPose.Diagnostics, Is.Empty);
            Assert.That(secondPose.Translations, Is.Empty);
            Assert.That(secondPose.PointerValues, Is.Empty);
            Assert.That(firstPose.Translations, Is.Not.SameAs(secondPose.Translations));
        });
    }

    [Test]
    public void ExtensionAndCodecData_HaveExplicitCapabilityDefaults()
    {
        GltfExtensionDescriptor descriptor = new GltfExtensionDescriptor();
        GltfExtensionContext context = new GltfExtensionContext();
        GltfExtensionResult extensionResult = new GltfExtensionResult();
        GltfCompressionRequest compressionRequest = new GltfCompressionRequest();
        GltfCompressionResult compressionResult = new GltfCompressionResult();
        GltfImageRequest imageRequest = new GltfImageRequest();
        GltfImageInfo imageInfo = new GltfImageInfo();
        GltfImageResult imageResult = new GltfImageResult();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Name, Is.Empty);
            Assert.That(descriptor.Status, Is.EqualTo(GltfExtensionStatus.Unknown));
            Assert.That(descriptor.ObjectLocations, Is.Empty);
            Assert.That(descriptor.Dependencies, Is.Empty);
            Assert.That(descriptor.Conflicts, Is.Empty);
            Assert.That(descriptor.SchemaResources, Is.Empty);
            Assert.That(descriptor.HasTypedModel, Is.False);
            Assert.That(descriptor.HasSemanticValidation, Is.False);
            Assert.That(descriptor.CanRemapReferences, Is.False);
            Assert.That(descriptor.AllowsFallback, Is.False);
            Assert.That(new GltfValidationOptions().ExtensionHandlers, Is.Empty);
            Assert.That(context.Name, Is.Empty);
            Assert.That(context.ObjectLocation, Is.Empty);
            Assert.That(context.HasObjectIndex, Is.False);
            Assert.That(context.ObjectIndex, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(context.HasSubObjectIndex, Is.False);
            Assert.That(context.SubObjectIndex, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(context.TypedData, Is.Null);
            Assert.That(context.Resources, Is.Not.SameAs(extensionResult.Resources));
            Assert.That(extensionResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(extensionResult.Descriptor, Is.Null);
            Assert.That(extensionResult.Json, Is.Null);
            Assert.That(extensionResult.TypedData, Is.Null);
            Assert.That(extensionResult.References, Is.Empty);
            Assert.That(compressionRequest.Codec, Is.Empty);
            Assert.That(compressionRequest.HasMesh, Is.False);
            Assert.That(compressionRequest.Mesh, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(compressionRequest.HasPrimitive, Is.False);
            Assert.That(compressionRequest.Primitive, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(compressionRequest.Bytes, Is.Empty);
            Assert.That(compressionRequest.Attributes, Is.Empty);
            Assert.That(compressionRequest.Draco, Is.Null);
            Assert.That(compressionRequest.Meshopt, Is.Null);
            Assert.That(compressionRequest.MaximumOutputByteLength, Is.EqualTo(GltfConstants.DefaultMaximumResourceByteLength));
            Assert.That(compressionResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(compressionResult.Bytes, Is.Empty);
            Assert.That(compressionResult.Attributes, Is.Empty);
            Assert.That(imageRequest.SourceFormat, Is.EqualTo(GltfImageFormat.Unknown));
            Assert.That(imageRequest.Codec, Is.Empty);
            Assert.That(imageRequest.Document, Is.Null);
            Assert.That(imageRequest.HasTexture, Is.False);
            Assert.That(imageRequest.Texture, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(imageRequest.HasImage, Is.False);
            Assert.That(imageRequest.Image, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(imageRequest.TargetFormat, Is.EqualTo(GltfImageFormat.Unknown));
            Assert.That(imageRequest.HasTargetWidth, Is.False);
            Assert.That(imageRequest.HasTargetHeight, Is.False);
            Assert.That(imageRequest.MaximumOutputByteLength, Is.EqualTo(GltfConstants.DefaultMaximumResourceByteLength));
            Assert.That(imageInfo.Format, Is.EqualTo(GltfImageFormat.Unknown));
            Assert.That(imageInfo.HasDimensions, Is.False);
            Assert.That(imageInfo.HasComponentCount, Is.False);
            Assert.That(imageInfo.HasBitDepth, Is.False);
            Assert.That(imageResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(imageResult.Info, Is.Not.SameAs(imageInfo));
            Assert.That(imageResult.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EditData_OwnsEveryIndexMapAndResultCollection()
    {
        GltfEditOptions options = new GltfEditOptions();
        GltfIndexRemap firstRemap = new GltfIndexRemap();
        GltfIndexRemap secondRemap = new GltfIndexRemap();
        GltfMergeResult firstResult = new GltfMergeResult();
        GltfMergeResult secondResult = new GltfMergeResult();

        firstRemap.Accessors[0] = 1;
        firstResult.SourceRemaps.Add(firstRemap);

        Assert.Multiple(() =>
        {
            Assert.That(options.CloneResourceBytes, Is.True);
            Assert.That(options.CloneJson, Is.True);
            Assert.That(options.RequireSafeUnknownExtensionRemap, Is.True);
            Assert.That(options.ValidateResult, Is.True);
            Assert.That(secondRemap.Accessors, Is.Empty);
            Assert.That(firstRemap.Animations, Is.Empty);
            Assert.That(firstRemap.Buffers, Is.Empty);
            Assert.That(firstRemap.BufferViews, Is.Empty);
            Assert.That(firstRemap.Cameras, Is.Empty);
            Assert.That(firstRemap.Images, Is.Empty);
            Assert.That(firstRemap.Materials, Is.Empty);
            Assert.That(firstRemap.Meshes, Is.Empty);
            Assert.That(firstRemap.Nodes, Is.Empty);
            Assert.That(firstRemap.Samplers, Is.Empty);
            Assert.That(firstRemap.Scenes, Is.Empty);
            Assert.That(firstRemap.Skins, Is.Empty);
            Assert.That(firstRemap.Textures, Is.Empty);
            Assert.That(firstRemap.XmpJsonLdPackets, Is.Empty);
            Assert.That(firstRemap.XmpJsonLdPackets, Is.Not.SameAs(
                secondRemap.XmpJsonLdPackets));
            Assert.That(firstResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstResult.Document, Is.Null);
            Assert.That(secondResult.SourceRemaps, Is.Empty);
            Assert.That(firstResult.SourceRemaps, Is.Not.SameAs(secondResult.SourceRemaps));
            Assert.That(firstResult.Diagnostics, Is.Not.SameAs(secondResult.Diagnostics));
        });
    }

    [Test]
    public void TransformData_HasTransactionalDefaultsAndIndependentState()
    {
        GltfTransformPlan firstPlan = new GltfTransformPlan();
        GltfTransformPlan secondPlan = new GltfTransformPlan();
        GltfTransformStep firstStep = new GltfTransformStep();
        GltfTransformStep secondStep = new GltfTransformStep();
        GltfTransformContext context = new GltfTransformContext();
        GltfTransformOptions options = new GltfTransformOptions();
        GltfTransformStepResult stepResult = new GltfTransformStepResult();
        GltfTransformExtensionChange extensionChange =
            new GltfTransformExtensionChange();
        GltfTransformReport report = new GltfTransformReport();

        firstPlan.Steps.Add(firstStep);
        firstStep.Settings["quality"] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(secondPlan.Steps, Is.Empty);
            Assert.That(firstStep.Kind, Is.EqualTo(GltfTransformKind.Custom));
            Assert.That(firstStep.Name, Is.Null);
            Assert.That(firstStep.Enabled, Is.True);
            Assert.That(secondStep.Settings, Is.Empty);
            Assert.That(firstStep.Settings, Is.Not.SameAs(secondStep.Settings));
            Assert.That(context.Document, Is.Null);
            Assert.That(context.Step, Is.Null);
            Assert.That(context.Resources, Is.Not.Null);
            Assert.That(context.Remap, Is.Not.Null);
            Assert.That(context.CompressionCodecs, Is.Empty);
            Assert.That(context.ImageCodecs, Is.Empty);
            Assert.That(context.Hooks, Is.Empty);
            Assert.That(context.Diagnostics, Is.Empty);
            Assert.That(options.CloneDocument, Is.True);
            Assert.That(options.ValidateBefore, Is.True);
            Assert.That(options.ValidateAfter, Is.True);
            Assert.That(options.RollbackOnFailure, Is.True);
            Assert.That(options.RequireSafeUnknownExtensions, Is.True);
            Assert.That(options.ValidationOptions, Is.Not.Null);
            Assert.That(stepResult.Step, Is.Null);
            Assert.That(stepResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(stepResult.ChangedObjects, Is.Empty);
            Assert.That(stepResult.ChangedResources, Is.Empty);
            Assert.That(stepResult.ExtensionChanges, Is.Empty);
            Assert.That(stepResult.Codecs, Is.Empty);
            Assert.That(stepResult.Reason, Is.Null);
            Assert.That(extensionChange.Name, Is.Empty);
            Assert.That(extensionChange.Required, Is.False);
            Assert.That(extensionChange.Added, Is.False);
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Document, Is.Null);
            Assert.That(report.Resources, Is.Not.Null);
            Assert.That(report.Steps, Is.Empty);
            Assert.That(report.ChangedObjects, Is.Empty);
            Assert.That(report.ChangedResources, Is.Empty);
            Assert.That(report.ExtensionChanges, Is.Empty);
            Assert.That(report.Codecs, Is.Empty);
            Assert.That(report.SkippedReasons, Is.Empty);
            Assert.That(report.OriginalByteLength, Is.Zero);
            Assert.That(report.ResultByteLength, Is.Zero);
            Assert.That(report.OriginalHash, Is.Null);
            Assert.That(report.ResultHash, Is.Null);
        });
    }

    [Test]
    public void InspectionAndConversionData_HaveCompleteOperationDefaults()
    {
        GltfInspectionOptions inspectionOptions = new GltfInspectionOptions();
        GltfDependency dependency = new GltfDependency();
        GltfInspectionResult firstInspection = new GltfInspectionResult();
        GltfInspectionResult secondInspection = new GltfInspectionResult();
        GltfConversionOptions firstConversion = new GltfConversionOptions();
        GltfConversionOptions secondConversion = new GltfConversionOptions();
        GltfConversionResult firstResult = new GltfConversionResult();
        GltfConversionResult secondResult = new GltfConversionResult();
        GltfConversionDecision decision = new GltfConversionDecision();

        firstInspection.Dependencies.Add(dependency);
        firstResult.Manifest.Add(new GltfResourceManifestEntry());
        firstResult.Decisions.Add(decision);

        Assert.Multiple(() =>
        {
            Assert.That(inspectionOptions.IncludeDependencies, Is.True);
            Assert.That(inspectionOptions.IncludeReverseDependencies, Is.True);
            Assert.That(inspectionOptions.CalculateBounds, Is.True);
            Assert.That(inspectionOptions.DecodeAccessors, Is.True);
            Assert.That(dependency.SourceCollection, Is.Empty);
            Assert.That(dependency.SourceIndex, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(dependency.SourceMember, Is.Empty);
            Assert.That(dependency.TargetCollection, Is.Empty);
            Assert.That(dependency.TargetIndex, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(firstInspection.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstInspection.Statistics, Is.Not.SameAs(secondInspection.Statistics));
            Assert.That(secondInspection.Dependencies, Is.Empty);
            Assert.That(firstInspection.ReverseDependencies, Is.Not.SameAs(secondInspection.ReverseDependencies));
            Assert.That(firstConversion.InputFormat, Is.EqualTo(GltfContainerFormat.Auto));
            Assert.That(firstConversion.OutputFormat, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(firstConversion.ResourceMode, Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(firstConversion.EmbedImages, Is.False);
            Assert.That(firstConversion.PartitionBuffers, Is.False);
            Assert.That(firstConversion.MaximumBufferByteLength,
                Is.EqualTo(GltfConstants.DefaultMaximumResourceByteLength));
            Assert.That(firstConversion.AtomicOutput, Is.True);
            Assert.That(firstConversion.OverwriteResources, Is.False);
            Assert.That(firstConversion.TransformPlan, Is.Null);
            Assert.That(firstConversion.ReadOptions, Is.Not.SameAs(secondConversion.ReadOptions));
            Assert.That(firstConversion.WriteOptions, Is.Not.SameAs(secondConversion.WriteOptions));
            Assert.That(firstConversion.TransformOptions, Is.Not.SameAs(secondConversion.TransformOptions));
            Assert.That(firstResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstResult.Document, Is.Null);
            Assert.That(firstResult.PrimaryBytes, Is.Empty);
            Assert.That(firstResult.PrimaryContentHash, Is.Null);
            Assert.That(firstResult.PrimaryPath, Is.Null);
            Assert.That(firstResult.TransformReport, Is.Null);
            Assert.That(secondResult.Manifest, Is.Empty);
            Assert.That(secondResult.Decisions, Is.Empty);
            Assert.That(decision.Kind,
                Is.EqualTo(GltfConversionDecisionKind.Extension));
            Assert.That(decision.Name, Is.Empty);
            Assert.That(decision.Action,
                Is.EqualTo(GltfConversionDecisionAction.Preserved));
            Assert.That(decision.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(firstResult.Resources, Is.Not.SameAs(secondResult.Resources));
            Assert.That(firstResult.Diagnostics, Is.Not.SameAs(secondResult.Diagnostics));
        });
    }
}
