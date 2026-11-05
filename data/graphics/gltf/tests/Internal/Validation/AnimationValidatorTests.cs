using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class AnimationValidatorTests
{
    [Test]
    public void ValidateDocumentAnimationRules_AcceptsValidLinearAnimation()
    {
        GltfDocument document = CreateDocumentWithNode();
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
        AddAnimation(
            document,
            CreateSampler(input, output),
            CreateChannel(GltfAnimationPath.Translation));

        GltfValidationReport report = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_ReportsReferencesAndInterpolation()
    {
        GltfDocument document = CreateDocumentWithNode();
        GltfAnimationSampler sampler = CreateSampler(4, 5);
        sampler.HasInterpolation = true;
        sampler.Interpolation = (GltfAnimationInterpolation)99;
        GltfAnimationChannel channel = CreateChannel(GltfAnimationPath.Translation);
        channel.Sampler = 2;
        AddAnimation(document, sampler, channel);

        GltfValidationReport report = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.UnresolvedReference, "/animations/0/samplers/0/input");
            AssertIssue(report, GltfIssueCodes.UnresolvedReference, "/animations/0/samplers/0/output");
            AssertIssue(report, GltfIssueCodes.ValueNotInList, "/animations/0/samplers/0/interpolation");
            AssertIssue(report, GltfIssueCodes.UnresolvedReference, "/animations/0/channels/0/sampler");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_ReportsTimeOutputAndTargetViolations()
    {
        GltfDocument document = CreateDocumentWithNode();
        document.Nodes[0].HasMatrix = true;
        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            -1.0,
            -1.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 1.0);
        AddAnimation(
            document,
            CreateSampler(input, output),
            CreateChannel(GltfAnimationPath.Translation));

        GltfValidationReport report = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.AccessorAnimationInputNegative, "/accessors/0/0");
            AssertIssue(report, GltfIssueCodes.AccessorAnimationInputNonIncreasing, "/accessors/0/1");
            AssertIssue(report, GltfIssueCodes.AnimationChannelTargetNodeMatrix, "/animations/0/channels/0/target/node");
            AssertIssue(report, GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat, "/accessors/1");
            AssertIssue(report, GltfIssueCodes.AnimationSamplerOutputAccessorInvalidCount, "/accessors/1/count");
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_ValidatesCubicWeightsAndRotationValues()
    {
        GltfDocument weightsDocument = CreateDocumentWithMorphTargets(2);
        int weightsInput = CreateFloatingAccessor(
            weightsDocument,
            GltfAccessorType.Scalar,
            0.0,
            1.0);
        int weightsOutput = CreateFloatingAccessor(
            weightsDocument,
            GltfAccessorType.Scalar,
            0.0, 0.0,
            0.2, 0.8,
            0.0, 0.0,
            0.0, 0.0,
            0.6, 0.4,
            0.0, 0.0);
        GltfAnimationSampler weightsSampler = CreateSampler(weightsInput, weightsOutput);
        weightsSampler.HasInterpolation = true;
        weightsSampler.Interpolation = GltfAnimationInterpolation.CubicSpline;
        AddAnimation(
            weightsDocument,
            weightsSampler,
            CreateChannel(GltfAnimationPath.Weights));

        GltfDocument rotationDocument = CreateDocumentWithNode();
        int rotationInput = CreateFloatingAccessor(
            rotationDocument,
            GltfAccessorType.Scalar,
            0.0);
        int rotationOutput = CreateFloatingAccessor(
            rotationDocument,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 2.0);
        AddAnimation(
            rotationDocument,
            CreateSampler(rotationInput, rotationOutput),
            CreateChannel(GltfAnimationPath.Rotation));

        AnimationValidator validator = new AnimationValidator();
        GltfValidationReport weightsReport = validator
            .ValidateDocumentAnimationRules(weightsDocument);
        GltfValidationReport rotationReport = validator
            .ValidateDocumentAnimationRules(rotationDocument);

        Assert.Multiple(() =>
        {
            Assert.That(weightsReport.IsValid, Is.True);
            Assert.That(weightsReport.Issues, Is.Empty);
            AssertIssue(
                rotationReport,
                GltfIssueCodes.AccessorAnimationSamplerOutputNonNormalizedQuaternion,
                "/accessors/1/0");
            Assert.That(rotationReport.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_EnforcesPointerTargetPairing()
    {
        GltfDocument document = CreateDocumentWithNode();
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0);
        int output = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 1.0);
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(CreateSampler(input, output));
        GltfAnimationChannel missingExtension = CreateChannel(GltfAnimationPath.Pointer);
        missingExtension.Target.HasNode = false;
        animation.Channels.Add(missingExtension);
        GltfAnimationChannel invalidExtensionTarget = CreateChannel(GltfAnimationPath.Scale);
        invalidExtensionTarget.Target.PropertyJson.Extensions[
            GltfExtensionNames.KhrAnimationPointer] = new JsonObject();
        animation.Channels.Add(invalidExtensionTarget);
        document.Animations.Add(animation);

        GltfValidationReport report = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.UndefinedProperty,
                "/animations/0/channels/0/target/extensions/KHR_animation_pointer");
            AssertIssue(
                report,
                GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetNode,
                "/animations/0/channels/1/target/node");
            AssertIssue(
                report,
                GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetPath,
                "/animations/0/channels/1/target/path");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_DoesNotWarnForWeightTargetOnSkinnedNode()
    {
        GltfDocument document = CreateDocumentWithMorphTargets(1);
        document.Nodes[0].HasSkin = true;
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0);
        int output = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.5);
        AddAnimation(
            document,
            CreateSampler(input, output),
            CreateChannel(GltfAnimationPath.Weights));

        GltfValidationReport report = new AnimationValidator()
            .ValidateDocumentAnimationRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(
                FindIssue(report, GltfIssueCodes.AnimationChannelTargetNodeSkin),
                Is.Null);
        });
    }

    [Test]
    public void ValidateDocumentAnimationRules_RejectsNullDocument()
    {
        Assert.That(
            () => new AnimationValidator().ValidateDocumentAnimationRules(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
    }

    private static GltfDocument CreateDocumentWithNode()
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode());
        return result;
    }

    private static GltfDocument CreateDocumentWithMorphTargets(int count)
    {
        GltfDocument result = CreateDocumentWithNode();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        int targetIndex = 0;
        while (targetIndex < count)
        {
            primitive.Targets.Add(new GltfMorphTarget());
            targetIndex++;
        }

        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        result.Nodes[0].HasMesh = true;
        result.Nodes[0].Mesh = 0;
        return result;
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

    private static GltfAnimationSampler CreateSampler(int input, int output)
    {
        GltfAnimationSampler result = new GltfAnimationSampler();
        result.Input = input;
        result.Output = output;
        return result;
    }

    private static GltfAnimationChannel CreateChannel(GltfAnimationPath path)
    {
        GltfAnimationChannel result = new GltfAnimationChannel();
        result.Sampler = 0;
        result.Target.HasNode = true;
        result.Target.Node = 0;
        result.Target.Path = path;
        return result;
    }

    private static void AddAnimation(
        GltfDocument document,
        GltfAnimationSampler sampler,
        GltfAnimationChannel channel)
    {
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        int index = 0;
        while (index < report.Issues.Count)
        {
            GltfValidationIssue issue = report.Issues[index];
            if (issue.Code == code && issue.JsonPointer == pointer)
            {
                return;
            }

            index++;
        }

        Assert.Fail($"Expected issue {code} at {pointer}.");
    }

    private static GltfValidationIssue? FindIssue(
        GltfValidationReport report,
        string code)
    {
        int index = 0;
        while (index < report.Issues.Count)
        {
            if (report.Issues[index].Code == code)
            {
                return report.Issues[index];
            }

            index++;
        }

        return null;
    }
}
