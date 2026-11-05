using System.Numerics;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfSceneSkinAnimationSemanticValidationTests
{
    [Test]
    public void ValidateDocument_AcceptsIntegratedSceneAndUsesColumnMajorCoordinatesForBounds()
    {
        GltfDocument document = CreateIntegratedDocument();

        GltfValidationReport report = Validate(document);
        GltfSceneEvaluationOptions options = new GltfSceneEvaluationOptions();
        options.CalculateCameras = false;
        GltfScenePose pose = new GltfSceneEvaluator().EvaluateScene(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True, FormatIssues(report));
            AssertNoIssue(report, GltfIssueCodes.NodeLoop);
            AssertNoIssue(report, GltfIssueCodes.NodeParentOverride);
            AssertNoIssue(report, GltfIssueCodes.SceneNonRootNode);
            AssertNoIssue(report, GltfIssueCodes.InvalidIbmAccessorCount);
            AssertNoIssue(report, GltfIssueCodes.AccessorJointsIndexOob);
            AssertNoIssue(report, GltfIssueCodes.AnimationSamplerOutputAccessorInvalidCount);
            Assert.That(pose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(pose.Nodes[1].WorldTransform.M41, Is.EqualTo(1.0f));
            Assert.That(pose.Nodes[1].WorldTransform.M42, Is.EqualTo(2.0f));
            Assert.That(pose.Nodes[1].WorldTransform.M43, Is.EqualTo(3.0f));
            Assert.That(pose.Nodes[2].SkinMatrices, Has.Count.EqualTo(2));
            Assert.That(pose.Bounds.IsEmpty, Is.False);
            Assert.That(pose.Bounds.Minimum, Is.EqualTo(new Vector3(4.0f, -4.0f, 7.0f)));
            Assert.That(pose.Bounds.Maximum, Is.EqualTo(new Vector3(7.0f, -1.0f, 14.0f)));
        });
    }

    [Test]
    public void ValidateDocument_ReportsHierarchyAndTransformConflictsTogether()
    {
        GltfDocument document = new GltfDocument();
        GltfNode first = new GltfNode();
        first.HasMatrix = true;
        first.HasTranslation = true;
        first.Children.Add(1);
        GltfNode second = new GltfNode();
        second.Children.Add(0);
        GltfNode third = new GltfNode();
        third.HasRotation = true;
        third.Rotation = new double[] { 0.0, 0.0, 0.0, 2.0 };
        third.Children.Add(1);
        document.Nodes.Add(first);
        document.Nodes.Add(second);
        document.Nodes.Add(third);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(1);
        document.Scenes.Add(scene);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.NodeLoop,
                GltfValidationSeverity.Error,
                "/nodes/1/children/0");
            AssertIssue(
                report,
                GltfIssueCodes.NodeParentOverride,
                GltfValidationSeverity.Error,
                "/nodes/2/children/0");
            AssertIssue(
                report,
                GltfIssueCodes.SceneNonRootNode,
                GltfValidationSeverity.Error,
                "/scenes/0/nodes/0");
            AssertIssue(
                report,
                GltfIssueCodes.NodeMatrixTrs,
                GltfValidationSeverity.Error,
                "/nodes/0");
            AssertIssue(
                report,
                GltfIssueCodes.NodeMatrixDefault,
                GltfValidationSeverity.Information,
                "/nodes/0/matrix");
            AssertIssue(
                report,
                GltfIssueCodes.RotationNonUnit,
                GltfValidationSeverity.Error,
                "/nodes/2/rotation");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocument_CrossChecksSkinHierarchySceneAndInverseBindMatrices()
    {
        GltfDocument document = CreateIntegratedDocument();
        GltfNode parent = new GltfNode();
        parent.HasTranslation = true;
        parent.Translation = new double[] { 2.0, 0.0, 0.0 };
        parent.Children.Add(2);
        document.Nodes.Add(parent);
        document.Scenes[0].Nodes.Clear();
        document.Scenes[0].Nodes.Add(3);
        GltfSkin skin = document.Skins[0];
        skin.HasSkeleton = true;
        skin.Skeleton = 3;
        skin.InverseBindMatrices = CreateFloatingAccessor(
            document,
            GltfAccessorType.Matrix4,
            IdentityMatrix());

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.SkinSkeletonInvalid,
                GltfValidationSeverity.Error,
                "/skins/0/skeleton");
            AssertIssue(
                report,
                GltfIssueCodes.NodeSkinNoScene,
                GltfValidationSeverity.Error,
                "/nodes/2/skin");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidIbmAccessorCount,
                GltfValidationSeverity.Error,
                "/skins/0/inverseBindMatrices");
            AssertIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshLocalTransforms,
                GltfValidationSeverity.Warning,
                "/nodes/2");
            AssertIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshNonRoot,
                GltfValidationSeverity.Information,
                "/nodes/2");
            AssertIssue(
                report,
                GltfIssueCodes.NodeSkinnedMeshParentTransforms,
                GltfValidationSeverity.Warning,
                "/nodes/2");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocument_DecodesJointInfluencesAgainstReferencedSkin()
    {
        GltfDocument document = CreateIntegratedDocument();
        GltfMeshPrimitive primitive = document.Meshes[0].Primitives[0];
        FindAttribute(primitive, "JOINTS_0").Accessor = CreateUnsignedAccessor(
            document,
            GltfAccessorType.Vector4,
            0U, 0U, 3U, 1U,
            0U, 1U, 0U, 0U,
            0U, 1U, 0U, 0U);
        FindAttribute(primitive, "WEIGHTS_0").Accessor = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.5, 0.25, 0.25, 0.0,
            0.5, 0.5, 0.0, 0.0,
            0.5, 0.5, 0.0, 0.0);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorJointsIndexDuplicate,
                GltfValidationSeverity.Error,
                "/skins/0/influences/0/0/1");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorJointsIndexOob,
                GltfValidationSeverity.Error,
                "/skins/0/influences/0/0/2");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorJointsUsedZeroWeight,
                GltfValidationSeverity.Warning,
                "/skins/0/influences/0/0/3");
            AssertNoIssue(report, GltfIssueCodes.AccessorWeightsNonNormalized);
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocument_CrossChecksAnimationInputsOutputsAndTargetNodes()
    {
        GltfDocument document = CreateIntegratedDocument();
        GltfAnimation animation = document.Animations[0];
        animation.Samplers[0].Input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            -1.0,
            -1.0);
        animation.Samplers[0].Output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 1.0);
        animation.Channels[0].Target.Node = 2;
        int rotationInput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0);
        int rotationOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 2.0);
        GltfAnimationSampler rotationSampler = new GltfAnimationSampler();
        rotationSampler.Input = rotationInput;
        rotationSampler.Output = rotationOutput;
        animation.Samplers.Add(rotationSampler);
        GltfAnimationChannel rotationChannel = new GltfAnimationChannel();
        rotationChannel.Sampler = 1;
        rotationChannel.Target.HasNode = true;
        rotationChannel.Target.Node = 1;
        rotationChannel.Target.Path = GltfAnimationPath.Rotation;
        animation.Channels.Add(rotationChannel);

        GltfValidationReport report = Validate(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.AccessorAnimationInputNegative,
                GltfValidationSeverity.Error,
                "/accessors/6/0");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorAnimationInputNonIncreasing,
                GltfValidationSeverity.Error,
                "/accessors/6/1");
            AssertIssue(
                report,
                GltfIssueCodes.AnimationChannelTargetNodeMatrix,
                GltfValidationSeverity.Error,
                "/animations/0/channels/0/target/node");
            AssertIssue(
                report,
                GltfIssueCodes.AnimationChannelTargetNodeSkin,
                GltfValidationSeverity.Warning,
                "/animations/0/channels/0/target/node");
            AssertIssue(
                report,
                GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "/accessors/7");
            AssertIssue(
                report,
                GltfIssueCodes.AnimationSamplerOutputAccessorInvalidCount,
                GltfValidationSeverity.Error,
                "/accessors/7/count");
            AssertIssue(
                report,
                GltfIssueCodes.AccessorAnimationSamplerOutputNonNormalizedQuaternion,
                GltfValidationSeverity.Error,
                "/accessors/9/0");
            Assert.That(report.IsValid, Is.False);
        });
    }

    private static GltfDocument CreateIntegratedDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfNode jointRoot = new GltfNode();
        jointRoot.HasTranslation = true;
        jointRoot.Translation = new double[] { 1.0, 2.0, 3.0 };
        jointRoot.Children.Add(1);
        document.Nodes.Add(jointRoot);
        document.Nodes.Add(new GltfNode());
        GltfNode skinnedNode = new GltfNode();
        skinnedNode.HasMesh = true;
        skinnedNode.Mesh = 0;
        skinnedNode.HasSkin = true;
        skinnedNode.Skin = 0;
        skinnedNode.HasMatrix = true;
        skinnedNode.Matrix = TranslationMatrix(5.0, -2.0, 10.0);
        document.Nodes.Add(skinnedNode);

        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            -1.0, -2.0, -3.0,
            2.0, 1.0, 4.0,
            0.0, 0.0, 0.0);
        int joints = CreateUnsignedAccessor(
            document,
            GltfAccessorType.Vector4,
            0U, 1U, 0U, 0U,
            0U, 1U, 0U, 0U,
            0U, 1U, 0U, 0U);
        int weights = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.5, 0.5, 0.0, 0.0,
            0.5, 0.5, 0.0, 0.0,
            0.5, 0.5, 0.0, 0.0);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        AddAttribute(primitive, "POSITION", positions);
        AddAttribute(primitive, "JOINTS_0", joints);
        AddAttribute(primitive, "WEIGHTS_0", weights);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfSkin skin = new GltfSkin();
        skin.Joints.Add(0);
        skin.Joints.Add(1);
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = CreateFloatingAccessor(
            document,
            GltfAccessorType.Matrix4,
            TwoIdentityMatrices());
        document.Skins.Add(skin);

        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        scene.Nodes.Add(2);
        document.Scenes.Add(scene);
        document.HasDefaultScene = true;
        document.DefaultScene = 0;

        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0,
            1.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 2.0, 3.0);
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        sampler.Input = input;
        sampler.Output = output;
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Sampler = 0;
        channel.Target.HasNode = true;
        channel.Target.Node = 1;
        channel.Target.Path = GltfAnimationPath.Translation;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
        return document;
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        params double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.Float;
        request.Type = type;
        request.FloatingValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int CreateUnsignedAccessor(
        GltfDocument document,
        GltfAccessorType type,
        params uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.UnsignedByte;
        request.Type = type;
        request.UnsignedValues = values;
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static void AddAttribute(
        GltfMeshPrimitive primitive,
        string semantic,
        int accessor)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = accessor;
        primitive.Attributes.Add(attribute);
    }

    private static GltfAttribute FindAttribute(
        GltfMeshPrimitive primitive,
        string semantic)
    {
        return primitive.Attributes.Single(attribute => attribute.Semantic == semantic);
    }

    private static double[] IdentityMatrix()
    {
        return new double[]
        {
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        };
    }

    private static double[] TwoIdentityMatrices()
    {
        double[] identity = IdentityMatrix();
        double[] result = new double[32];
        identity.CopyTo(result, 0);
        identity.CopyTo(result, 16);
        return result;
    }

    private static double[] TranslationMatrix(double x, double y, double z)
    {
        double[] result = IdentityMatrix();
        result[12] = x;
        result[13] = y;
        result[14] = z;
        return result;
    }

    private static GltfValidationReport Validate(GltfDocument document)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateResources = false;
        options.ValidateExtensions = false;
        return new GltfValidator().ValidateDocument(document, options);
    }

    private static void AssertNoIssue(GltfValidationReport report, string code)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code),
            Is.False,
            FormatIssues(report));
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code
                && issue.Severity == severity
                && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {severity} {code} at '{pointer}', got: {FormatIssues(report)}");
    }

    private static string FormatIssues(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(
                issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
