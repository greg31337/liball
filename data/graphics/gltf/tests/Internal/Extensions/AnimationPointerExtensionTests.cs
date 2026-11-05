using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class AnimationPointerExtensionTests
{
    [Test]
    public void ReaderAndWriter_PreserveTypedPointerAndUnknownData()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"materials\":[{\"pbrMetallicRoughness\":{}}],"
            + "\"animations\":[{\"channels\":[{\"sampler\":0,\"target\":{"
            + "\"path\":\"pointer\",\"extensions\":{\"KHR_animation_pointer\":{"
            + "\"pointer\":\"/materials/0/pbrMetallicRoughness/baseColorFactor\","
            + "\"future\":true}}}}],\"samplers\":[{\"input\":0,\"output\":1}]}]}";

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfAnimationTarget target = read.Document!.Animations[0].Channels[0].Target;
        GltfAnimationPointerExtension value = target.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrAnimationPointer].AnimationPointer!;
        CreateFloatingAccessor(read.Document, GltfAccessorType.Scalar, 0.0);
        CreateFloatingAccessor(
            read.Document,
            GltfAccessorType.Vector4,
            1.0, 1.0, 1.0, 1.0);
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        JsonNode written = JsonNode.Parse(new GltfWriter().WriteJsonText(
            read.Document,
            options).JsonText!)!;
        JsonNode payload = written["animations"]![0]!["channels"]![0]!["target"]![
            "extensions"]![GltfExtensionNames.KhrAnimationPointer]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(value.Pointer, Is.EqualTo(
                "/materials/0/pbrMetallicRoughness/baseColorFactor"));
            Assert.That(value.PropertyJson.UnknownMembers["future"]!.GetValue<bool>(),
                Is.True);
            Assert.That(payload["pointer"]!.GetValue<string>(), Is.EqualTo(value.Pointer));
            Assert.That(payload["future"]!.GetValue<bool>(), Is.True);
        });
    }

    [Test]
    public void Resolver_ParsesRfc6901EscapesAndRejectsInvalidEscapes()
    {
        AnimationPointerResolver resolver = new AnimationPointerResolver();

        AnimationPointerTarget valid = resolver.ParsePointer("/extras/a~1b/~0key");
        AnimationPointerTarget invalid = resolver.ParsePointer("/nodes/0/a~2b");

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Segments, Is.EqualTo(new[] { "extras", "a/b", "~key" }));
            Assert.That(resolver.EncodePointer(valid.Segments),
                Is.EqualTo("/extras/a~1b/~0key"));
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.IssueCode, Is.EqualTo(GltfIssueCodes.InvalidJson));
        });
    }

    [Test]
    public void Resolver_ResolvesTextureTransformVectorShape()
    {
        GltfDocument document = new GltfDocument();
        GltfMaterial material = new GltfMaterial();
        material.NormalTexture = new GltfNormalTextureInfo();
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.TextureTransform = new GltfTextureTransformExtension();
        material.NormalTexture.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrTextureTransform] = typed;
        document.Materials.Add(material);

        AnimationPointerTarget result = new AnimationPointerResolver().ResolvePointer(
            document,
            "/materials/0/normalTexture/extensions/KHR_texture_transform/offset");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ValueType, Is.EqualTo(AnimationPointerValueType.Float2));
            Assert.That(result.ComponentCount, Is.EqualTo(2));
            Assert.That(result.TargetCollection, Is.EqualTo("materials"));
            Assert.That(result.TargetIndex, Is.Zero);
        });
    }

    [Test]
    public void SampleAnimation_ResolvesShapeAndStoresPointerValues()
    {
        GltfDocument document = new GltfDocument();
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        document.Materials.Add(material);
        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0, 2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.2, 0.4, 0.6,
            1.0, 0.8, 0.6, 0.4);
        const string pointer = "/materials/0/pbrMetallicRoughness/baseColorFactor";
        AddPointerAnimation(document, input, output, pointer);

        GltfAnimationEvaluationOptions options = new GltfAnimationEvaluationOptions();
        options.Animation = 0;
        options.Time = 1.0;
        GltfAnimationPose result = new GltfAnimationEvaluator().SampleAnimation(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.PointerValues[pointer][0], Is.EqualTo(0.5).Within(0.000001));
            Assert.That(result.PointerValues[pointer][1], Is.EqualTo(0.5).Within(0.000001));
            Assert.That(result.PointerValues[pointer][2], Is.EqualTo(0.5).Within(0.000001));
            Assert.That(result.PointerValues[pointer][3], Is.EqualTo(0.5).Within(0.000001));
            Assert.That(result.Translations, Is.Empty);
        });
    }

    [Test]
    public void BooleanPointer_RequiresUnsignedByteStepSampling()
    {
        GltfDocument document = new GltfDocument();
        GltfNode node = new GltfNode();
        GltfTypedExtensionData visibilityTyped = new GltfTypedExtensionData();
        visibilityTyped.NodeVisibility = new GltfNodeVisibilityExtension();
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrNodeVisibility] =
            visibilityTyped;
        document.Nodes.Add(node);
        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0, 1.0);
        int output = CreateUnsignedByteAccessor(document, 0U, 1U);
        const string pointer = "/nodes/0/extensions/KHR_node_visibility/visible";
        AddPointerAnimation(document, input, output, pointer);
        document.Animations[0].Samplers[0].Interpolation = GltfAnimationInterpolation.Step;
        GltfAnimationEvaluationOptions options = new GltfAnimationEvaluationOptions();
        options.Animation = 0;
        options.Time = 0.5;

        GltfAnimationPose sampled = new GltfAnimationEvaluator().SampleAnimation(
            document,
            options);
        document.Animations[0].Samplers[0].Interpolation = GltfAnimationInterpolation.Linear;
        AnimationLayoutAnalysis invalid = new AnimationLayoutAnalyzer().AnalyzeAnimation(
            document,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(sampled.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(sampled.PointerValues[pointer], Is.EqualTo(new[] { 0.0 }));
            Assert.That(invalid.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat
                && issue.JsonPointer.EndsWith("/interpolation", StringComparison.Ordinal)),
                Is.True);
        });
    }

    [Test]
    public void ApplyAnimationPose_AppliesNodePointerWithoutMutatingDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfNode node = new GltfNode();
        node.HasTranslation = true;
        node.Translation = [1.0, 0.0, 0.0];
        document.Nodes.Add(node);
        GltfAnimationPose pose = new GltfAnimationPose();
        pose.PointerValues["/nodes/0/translation"] = [3.0, 4.0, 5.0];

        GltfScenePose result = new GltfAnimationEvaluator().ApplyAnimationPose(
            document,
            pose,
            new GltfSceneEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Nodes[0].LocalTransform.M41, Is.EqualTo(3.0f));
            Assert.That(result.Nodes[0].LocalTransform.M42, Is.EqualTo(4.0f));
            Assert.That(result.Nodes[0].LocalTransform.M43, Is.EqualTo(5.0f));
            Assert.That(document.Nodes[0].Translation,
                Is.EqualTo(new[] { 1.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void Validation_RejectsUnresolvedShapesAndOverlappingTargets()
    {
        GltfDocument unresolved = new GltfDocument();
        int unresolvedInput = CreateFloatingAccessor(
            unresolved,
            GltfAccessorType.Scalar,
            0.0);
        int unresolvedOutput = CreateFloatingAccessor(
            unresolved,
            GltfAccessorType.Vector3,
            1.0, 2.0, 3.0);
        AddPointerAnimation(
            unresolved,
            unresolvedInput,
            unresolvedOutput,
            "/nodes/9/translation");

        GltfDocument duplicate = CreateMorphDocument();
        int duplicateInput = CreateFloatingAccessor(
            duplicate,
            GltfAccessorType.Scalar,
            0.0);
        int duplicateOutput = CreateFloatingAccessor(
            duplicate,
            GltfAccessorType.Scalar,
            0.5);
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(CreateSampler(duplicateInput, duplicateOutput));
        animation.Channels.Add(CreatePointerChannel(0, "/nodes/0/weights"));
        animation.Channels.Add(CreatePointerChannel(0, "/nodes/0/weights/0"));
        duplicate.Animations.Add(animation);

        AnimationLayoutAnalysis unresolvedResult = new AnimationLayoutAnalyzer()
            .AnalyzeAnimation(unresolved, 0);
        AnimationLayoutAnalysis duplicateResult = new AnimationLayoutAnalyzer()
            .AnalyzeAnimation(duplicate, 0);

        Assert.Multiple(() =>
        {
            Assert.That(unresolvedResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnresolvedReference), Is.True);
            Assert.That(duplicateResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.AnimationDuplicateTargets), Is.True);
            Assert.That(unresolvedResult.IsValid, Is.False);
            Assert.That(duplicateResult.IsValid, Is.False);
        });
    }

    [Test]
    public void Validation_RejectsOutputShapeMismatchForResolvedPointer()
    {
        GltfDocument document = new GltfDocument();
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        document.Materials.Add(material);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            1.0, 1.0, 1.0);
        AddPointerAnimation(
            document,
            input,
            output,
            "/materials/0/pbrMetallicRoughness/baseColorFactor");

        GltfValidationReport result = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat),
                Is.True);
            Assert.That(result.Issues.Any(issue =>
                issue.Code == GltfIssueCodes.UndefinedProperty
                && issue.JsonPointer.EndsWith(
                    "/extensions/KHR_animation_pointer",
                    StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void Handler_TracksAndRemapsPointerRootReferenceWithoutMutatingInput()
    {
        GltfAnimationPointerExtension value = new GltfAnimationPointerExtension();
        value.Pointer = "/nodes/2/translation";
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.AnimationPointer = value;
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = GltfExtensionNames.KhrAnimationPointer;
        context.ObjectLocation = "animation.channel.target";
        context.HasObjectIndex = true;
        context.ObjectIndex = 1;
        context.TypedData = typed;
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Nodes[2] = 7;

        GltfExtensionResult result = new GltfExtensionHandler()
            .RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Json!["pointer"]!.GetValue<string>(),
                Is.EqualTo("/nodes/7/translation"));
            Assert.That(value.Pointer, Is.EqualTo("/nodes/2/translation"));
            Assert.That(result.References, Has.Count.EqualTo(1));
            Assert.That(result.References[0].SourceCollection, Is.EqualTo("animations"));
            Assert.That(result.References[0].SourceIndex, Is.EqualTo(1));
            Assert.That(result.References[0].TargetCollection, Is.EqualTo("nodes"));
            Assert.That(result.References[0].TargetIndex, Is.EqualTo(7));
        });
    }

    private static GltfDocument CreateMorphDocument()
    {
        GltfDocument result = new GltfDocument();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Targets.Add(new GltfMorphTarget());
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        GltfNode node = new GltfNode();
        node.HasMesh = true;
        node.Mesh = 0;
        result.Nodes.Add(node);
        return result;
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        params double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.Type = type;
        request.FloatingValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int CreateUnsignedByteAccessor(
        GltfDocument document,
        params uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.UnsignedByte;
        request.Type = GltfAccessorType.Scalar;
        request.UnsignedValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static GltfAnimationSampler CreateSampler(int input, int output)
    {
        GltfAnimationSampler result = new GltfAnimationSampler();
        result.Input = input;
        result.Output = output;
        result.HasInterpolation = true;
        result.Interpolation = GltfAnimationInterpolation.Linear;
        return result;
    }

    private static GltfAnimationChannel CreatePointerChannel(int sampler, string pointer)
    {
        GltfAnimationPointerExtension extension = new GltfAnimationPointerExtension();
        extension.Pointer = pointer;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.AnimationPointer = extension;
        GltfAnimationChannel result = new GltfAnimationChannel();
        result.Sampler = sampler;
        result.Target.HasNode = false;
        result.Target.Path = GltfAnimationPath.Pointer;
        result.Target.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrAnimationPointer] = typed;
        return result;
    }

    private static void AddPointerAnimation(
        GltfDocument document,
        int input,
        int output,
        string pointer)
    {
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(CreateSampler(input, output));
        animation.Channels.Add(CreatePointerChannel(0, pointer));
        document.Animations.Add(animation);
    }
}
