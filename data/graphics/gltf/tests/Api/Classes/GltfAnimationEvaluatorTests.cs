using System;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfAnimationEvaluatorTests
{
    [Test]
    public void SampleAnimation_SamplesStepAtExactBetweenAndClampedTimes()
    {
        GltfDocument document = CreateDocument(1);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 1.0, 2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            10.0, 0.0, 0.0,
            20.0, 0.0, 0.0);
        GltfAnimationSampler sampler = CreateSampler(
            input,
            output,
            GltfAnimationInterpolation.Step);
        AddAnimation(document, [sampler], [CreateChannel(0, 0, GltfAnimationPath.Translation)]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();

        GltfAnimationPose before = Sample(evaluator, document, 0.0, GltfAnimationWrapMode.Clamp);
        GltfAnimationPose between = Sample(evaluator, document, 1.5, GltfAnimationWrapMode.Clamp);
        GltfAnimationPose exact = Sample(evaluator, document, 2.0, GltfAnimationWrapMode.Clamp);
        GltfAnimationPose after = Sample(evaluator, document, 4.0, GltfAnimationWrapMode.Clamp);

        Assert.Multiple(() =>
        {
            Assert.That(before.Time, Is.EqualTo(0.0));
            Assert.That(before.Translations[0], Is.EqualTo(new[] { 10.0, 0.0, 0.0 }));
            Assert.That(between.Translations[0], Is.EqualTo(new[] { 10.0, 0.0, 0.0 }));
            Assert.That(exact.Translations[0], Is.EqualTo(new[] { 20.0, 0.0, 0.0 }));
            Assert.That(after.Time, Is.EqualTo(2.0));
            Assert.That(after.Translations[0], Is.EqualTo(new[] { 20.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void SampleAnimation_LinearlyInterpolatesAndLoopsPositiveAndNegativeTimes()
    {
        GltfDocument document = CreateDocument(1);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            2.0, 4.0, 6.0);
        AddAnimation(
            document,
            [CreateSampler(input, output, GltfAnimationInterpolation.Linear)],
            [CreateChannel(0, 0, GltfAnimationPath.Translation)]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();

        GltfAnimationPose positive = Sample(evaluator, document, 2.5, GltfAnimationWrapMode.Loop);
        GltfAnimationPose negative = Sample(evaluator, document, -0.5, GltfAnimationWrapMode.Loop);

        Assert.Multiple(() =>
        {
            Assert.That(positive.Time, Is.EqualTo(0.5));
            Assert.That(positive.Translations[0], Is.EqualTo(new[] { 0.5, 1.0, 1.5 }));
            Assert.That(negative.Time, Is.EqualTo(1.5));
            Assert.That(negative.Translations[0], Is.EqualTo(new[] { 1.5, 3.0, 4.5 }));
        });
    }

    [Test]
    public void SampleAnimation_UsesLinearDefaultForScaleAtExactAndBetweenKeys()
    {
        GltfDocument document = CreateDocument(1);
        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0,
            1.0,
            2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            1.0, 1.0, 1.0,
            3.0, 5.0, 7.0,
            5.0, 9.0, 13.0);
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        sampler.Input = input;
        sampler.Output = output;
        sampler.Interpolation = GltfAnimationInterpolation.Step;
        AddAnimation(
            document,
            [sampler],
            [CreateChannel(0, 0, GltfAnimationPath.Scale)]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();

        GltfAnimationPose between = Sample(
            evaluator,
            document,
            0.5,
            GltfAnimationWrapMode.Clamp);
        GltfAnimationPose exact = Sample(
            evaluator,
            document,
            1.0,
            GltfAnimationWrapMode.Clamp);

        Assert.Multiple(() =>
        {
            Assert.That(between.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(between.Scales[0], Is.EqualTo(new[] { 2.0, 3.0, 4.0 }));
            Assert.That(exact.Scales[0], Is.EqualTo(new[] { 3.0, 5.0, 7.0 }));
            Assert.That(between.Animation, Is.Zero);
            Assert.That(between.Time, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void SampleAnimation_UsesQuaternionShortestPathAndOwnsResultArrays()
    {
        GltfDocument document = CreateDocument(1);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 1.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 1.0,
            0.0, 0.0, 0.0, -1.0);
        AddAnimation(
            document,
            [CreateSampler(input, output, GltfAnimationInterpolation.Linear)],
            [CreateChannel(0, 0, GltfAnimationPath.Rotation)]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();

        GltfAnimationPose first = Sample(evaluator, document, 0.5, GltfAnimationWrapMode.Clamp);
        first.Rotations[0][3] = 0.0;
        GltfAnimationPose second = Sample(evaluator, document, 0.5, GltfAnimationWrapMode.Clamp);

        Assert.Multiple(() =>
        {
            Assert.That(second.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(second.Rotations[0][0], Is.EqualTo(0.0).Within(0.0000001));
            Assert.That(second.Rotations[0][3], Is.EqualTo(1.0).Within(0.0000001));
            Assert.That(second.Rotations[0], Is.Not.SameAs(first.Rotations[0]));
        });
    }

    [Test]
    public void SampleAnimation_EvaluatesCubicHermiteWithTimeScalingAndOptionalNormalization()
    {
        GltfDocument document = CreateDocument(2);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 2.0);
        int translationOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0,
            0.0, 0.0, 0.0,
            0.0, 0.0, 0.0,
            0.0, 0.0, 0.0);
        int rotationOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0,
            0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 0.0);
        GltfAnimationSampler translation = CreateSampler(
            input,
            translationOutput,
            GltfAnimationInterpolation.CubicSpline);
        GltfAnimationSampler rotation = CreateSampler(
            input,
            rotationOutput,
            GltfAnimationInterpolation.CubicSpline);
        AddAnimation(
            document,
            [translation, rotation],
            [
                CreateChannel(0, 0, GltfAnimationPath.Translation),
                CreateChannel(1, 1, GltfAnimationPath.Rotation)
            ]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();
        GltfAnimationEvaluationOptions rawOptions = CreateOptions(
            1.0,
            GltfAnimationWrapMode.Clamp);
        rawOptions.NormalizeRotations = false;

        GltfAnimationPose raw = evaluator.SampleAnimation(document, rawOptions);
        GltfAnimationPose normalized = Sample(
            evaluator,
            document,
            1.0,
            GltfAnimationWrapMode.Clamp);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Translations[0][0], Is.EqualTo(0.25).Within(0.0000001));
            Assert.That(raw.Rotations[1], Is.EqualTo(new[] { 0.0, 0.0, 0.5, 0.5 }));
            Assert.That(normalized.Rotations[1][2], Is.EqualTo(Math.Sqrt(0.5)).Within(0.0000001));
            Assert.That(normalized.Rotations[1][3], Is.EqualTo(Math.Sqrt(0.5)).Within(0.0000001));
        });
    }

    [Test]
    public void SampleAnimation_InterpolatesPackedMorphWeights()
    {
        GltfDocument document = CreateDocument(1);
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Targets.Add(new GltfMorphTarget());
        primitive.Targets.Add(new GltfMorphTarget());
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 0;
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 1.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0, 1.0,
            1.0, 0.0);
        AddAnimation(
            document,
            [CreateSampler(input, output, GltfAnimationInterpolation.Linear)],
            [CreateChannel(0, 0, GltfAnimationPath.Weights)]);

        GltfAnimationPose result = Sample(
            new GltfAnimationEvaluator(),
            document,
            0.25,
            GltfAnimationWrapMode.Clamp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Weights[0], Is.EqualTo(new[] { 0.25, 0.75 }));
        });
    }

    [Test]
    public void SampleAndApplyAnimation_SupportsVectorAndScalarPointerTargets()
    {
        GltfDocument document = CreateDocument(1);
        document.Materials.Add(new GltfMaterial());
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrAnimationPointer);
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrAnimationPointer);
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 2.0);
        int scaleOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            1.0, 1.0, 1.0,
            3.0, 5.0, 7.0);
        int alphaOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.2,
            0.8);
        AddAnimation(
            document,
            [
                CreateSampler(input, scaleOutput, GltfAnimationInterpolation.Linear),
                CreateSampler(input, alphaOutput, GltfAnimationInterpolation.Linear)
            ],
            [
                CreatePointerChannel(0, "/nodes/0/scale"),
                CreatePointerChannel(1, "/materials/0/alphaCutoff")
            ]);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();

        GltfAnimationPose pose = Sample(
            evaluator,
            document,
            1.0,
            GltfAnimationWrapMode.Clamp);
        GltfSceneEvaluationOptions sceneOptions = new GltfSceneEvaluationOptions();
        sceneOptions.CalculateBounds = false;
        sceneOptions.CalculateCameras = false;
        sceneOptions.CalculateSkins = false;
        GltfScenePose applied = evaluator.ApplyAnimationPose(document, pose, sceneOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pose.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(pose.PointerValues["/nodes/0/scale"],
                Is.EqualTo(new[] { 2.0, 3.0, 4.0 }));
            Assert.That(pose.PointerValues["/materials/0/alphaCutoff"],
                Is.EqualTo(new[] { 0.5 }).Within(0.0000001));
            Assert.That(applied.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(applied.Nodes[0].LocalTransform.M11, Is.EqualTo(2.0f));
            Assert.That(applied.Nodes[0].LocalTransform.M22, Is.EqualTo(3.0f));
            Assert.That(applied.Nodes[0].LocalTransform.M33, Is.EqualTo(4.0f));
            Assert.That(document.Nodes[0].HasScale, Is.False);
            Assert.That(document.Materials[0].HasAlphaCutoff, Is.False);
            Assert.That(document.Materials[0].AlphaCutoff, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void ApplyAnimationPose_EvaluatesACloneWithoutMutatingSourceNodes()
    {
        GltfDocument document = CreateDocument(2);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 0.0, 0.0];
        document.Nodes[0].Children.Add(1);
        document.Nodes[1].HasTranslation = true;
        document.Nodes[1].Translation = [0.0, 2.0, 0.0];
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Targets.Add(new GltfMorphTarget());
        primitive.Targets.Add(new GltfMorphTarget());
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 0;
        document.Nodes[0].Weights = [1.0, 0.0];
        GltfAnimationPose pose = new GltfAnimationPose();
        pose.Translations[0] = [3.0, 0.0, 0.0];
        pose.Rotations[1] = [0.0, 0.0, Math.Sqrt(0.5), Math.Sqrt(0.5)];
        pose.Scales[1] = [2.0, 2.0, 2.0];
        pose.Weights[0] = [0.25, 0.75];

        GltfScenePose result = new GltfAnimationEvaluator().ApplyAnimationPose(
            document,
            pose,
            new GltfSceneEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Nodes, Has.Count.EqualTo(2));
            Assert.That(result.Nodes[0].LocalTransform.M41, Is.EqualTo(3.0f));
            Assert.That(result.Nodes[1].LocalTransform.M11, Is.Zero.Within(0.00001f));
            Assert.That(result.Nodes[1].LocalTransform.M12, Is.EqualTo(2.0f).Within(0.00001f));
            Assert.That(result.Nodes[1].LocalTransform.M21, Is.EqualTo(-2.0f).Within(0.00001f));
            Assert.That(result.Nodes[1].WorldTransform.M41, Is.EqualTo(3.0f));
            Assert.That(result.Nodes[1].WorldTransform.M42, Is.EqualTo(2.0f));
            Assert.That(document.Nodes[0].Translation, Is.EqualTo(new[] { 1.0, 0.0, 0.0 }));
            Assert.That(document.Nodes[0].Weights, Is.EqualTo(new[] { 1.0, 0.0 }));
            Assert.That(document.Nodes[1].HasRotation, Is.False);
            Assert.That(document.Nodes[0].HasScale, Is.False);
            Assert.That(document.Nodes[1].HasScale, Is.False);
        });
    }

    [Test]
    public void ApplyAnimationPose_RejectsMalformedValuesWithoutPublishingPartialScene()
    {
        GltfDocument document = CreateDocument(1);
        document.Nodes[0].HasTranslation = true;
        document.Nodes[0].Translation = [1.0, 2.0, 3.0];
        GltfAnimationPose pose = new GltfAnimationPose();
        pose.Translations[0] = [9.0, 8.0, 7.0];
        pose.Scales[0] = [2.0, 3.0];

        GltfScenePose result = new GltfAnimationEvaluator().ApplyAnimationPose(
            document,
            pose,
            new GltfSceneEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Nodes, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.ArrayLengthNotInList));
            Assert.That(result.Diagnostics[0].JsonPointer, Is.EqualTo("/nodes/0/scale"));
            Assert.That(document.Nodes[0].Translation, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(document.Nodes[0].HasScale, Is.False);
        });
    }

    [Test]
    public void CalculateAnimationDuration_UsesLatestSamplerEndTime()
    {
        GltfDocument document = CreateDocument(2);
        int shortInput = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 1.0);
        int longInput = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 3.0);
        int translationOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0);
        int scaleOutput = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            1.0, 1.0, 1.0,
            2.0, 2.0, 2.0);
        AddAnimation(
            document,
            [
                CreateSampler(shortInput, translationOutput, GltfAnimationInterpolation.Linear),
                CreateSampler(longInput, scaleOutput, GltfAnimationInterpolation.Linear)
            ],
            [
                CreateChannel(0, 0, GltfAnimationPath.Translation),
                CreateChannel(1, 1, GltfAnimationPath.Scale)
            ]);

        double result = new GltfAnimationEvaluator().CalculateAnimationDuration(document, 0);

        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void AnimationOperations_ReportInvalidInputsAndPropagateFailedPoses()
    {
        GltfDocument document = CreateDocument(1);
        GltfAnimationEvaluationOptions invalidTime = CreateOptions(
            double.NaN,
            GltfAnimationWrapMode.Clamp);
        GltfAnimationEvaluator evaluator = new GltfAnimationEvaluator();
        GltfAnimationPose failedPose = new GltfAnimationPose();
        failedPose.Status = GltfOperationStatus.Unavailable;
        GltfValidationIssue diagnostic = new GltfValidationIssue();
        diagnostic.Code = GltfIssueCodes.OperationUnavailable;
        failedPose.Diagnostics.Add(diagnostic);

        GltfAnimationPose sample = evaluator.SampleAnimation(document, invalidTime);
        GltfScenePose applied = evaluator.ApplyAnimationPose(
            document,
            failedPose,
            new GltfSceneEvaluationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(sample.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(sample.Diagnostics[0].Code, Is.EqualTo(GltfIssueCodes.ValueNotInRange));
            Assert.That(applied.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(applied.Diagnostics[0], Is.SameAs(diagnostic));
            Assert.That(
                () => evaluator.CalculateAnimationDuration(document, 0),
                Throws.TypeOf<GltfValidationException>());
            Assert.That(
                () => evaluator.SampleAnimation(null!, new GltfAnimationEvaluationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => evaluator.SampleAnimation(document, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => evaluator.ApplyAnimationPose(
                    null!,
                    new GltfAnimationPose(),
                    new GltfSceneEvaluationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => evaluator.ApplyAnimationPose(document, null!, new GltfSceneEvaluationOptions()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("pose"));
            Assert.That(
                () => evaluator.ApplyAnimationPose(document, new GltfAnimationPose(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => evaluator.CalculateAnimationDuration(null!, 0),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
        });
    }

    private static GltfDocument CreateDocument(int nodeCount)
    {
        GltfDocument result = new GltfDocument();
        int nodeIndex = 0;
        while (nodeIndex < nodeCount)
        {
            result.Nodes.Add(new GltfNode());
            nodeIndex++;
        }

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

    private static GltfAnimationSampler CreateSampler(
        int input,
        int output,
        GltfAnimationInterpolation interpolation)
    {
        GltfAnimationSampler result = new GltfAnimationSampler();
        result.Input = input;
        result.Output = output;
        result.HasInterpolation = true;
        result.Interpolation = interpolation;
        return result;
    }

    private static GltfAnimationChannel CreateChannel(
        int sampler,
        int node,
        GltfAnimationPath path)
    {
        GltfAnimationChannel result = new GltfAnimationChannel();
        result.Sampler = sampler;
        result.Target.HasNode = true;
        result.Target.Node = node;
        result.Target.Path = path;
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
        result.Target.Path = GltfAnimationPath.Pointer;
        result.Target.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrAnimationPointer] = typed;
        return result;
    }

    private static void AddAnimation(
        GltfDocument document,
        GltfAnimationSampler[] samplers,
        GltfAnimationChannel[] channels)
    {
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.AddRange(samplers);
        animation.Channels.AddRange(channels);
        document.Animations.Add(animation);
    }

    private static GltfAnimationEvaluationOptions CreateOptions(
        double time,
        GltfAnimationWrapMode wrapMode)
    {
        GltfAnimationEvaluationOptions result = new GltfAnimationEvaluationOptions();
        result.Animation = 0;
        result.Time = time;
        result.WrapMode = wrapMode;
        return result;
    }

    private static GltfAnimationPose Sample(
        GltfAnimationEvaluator evaluator,
        GltfDocument document,
        double time,
        GltfAnimationWrapMode wrapMode)
    {
        return evaluator.SampleAnimation(document, CreateOptions(time, wrapMode));
    }
}
