using System;
using System.Collections.Generic;

namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class AnimationLayoutAnalyzerTests
{
    [Test]
    public void AnalyzeAnimation_BuildsLinearTranslationLayoutAndDuration()
    {
        GltfDocument document = CreateDocumentWithNode();
        int input = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0,
            1.0,
            2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 2.0, 3.0,
            2.0, 4.0, 6.0);
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        sampler.Input = input;
        sampler.Output = output;
        sampler.Interpolation = GltfAnimationInterpolation.CubicSpline;
        AddAnimation(document, sampler, CreateChannel(GltfAnimationPath.Translation));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasDuration, Is.True);
            Assert.That(result.Duration, Is.EqualTo(2.0));
            Assert.That(result.Samplers[0].Interpolation, Is.EqualTo(GltfAnimationInterpolation.Linear));
            Assert.That(result.Samplers[0].KeyTimes, Is.EqualTo(new[] { 0.0, 1.0, 2.0 }));
            Assert.That(result.Channels[0].ValueComponentCount, Is.EqualTo(3));
            Assert.That(result.Channels[0].ExpectedOutputElementCount, Is.EqualTo(3));
            Assert.That(result.Channels[0].ValueOffsets, Is.EqualTo(new int[] { 0, 3, 6 }));
            Assert.That(result.Channels[0].InTangentOffsets, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AnalyzeAnimation_MapsCubicQuaternionTripletsAndChecksOnlyValues()
    {
        GltfDocument document = CreateDocumentWithNode();
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 1.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector4,
            4.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0,
            0.0, 5.0, 0.0, 0.0,
            0.0, 0.0, 6.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 7.0);
        GltfAnimationSampler sampler = CreateSampler(input, output);
        sampler.HasInterpolation = true;
        sampler.Interpolation = GltfAnimationInterpolation.CubicSpline;
        AddAnimation(document, sampler, CreateChannel(GltfAnimationPath.Rotation));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Channels[0].CubicSpline, Is.True);
            Assert.That(result.Channels[0].ExpectedOutputElementCount, Is.EqualTo(6));
            Assert.That(result.Channels[0].InTangentOffsets, Is.EqualTo(new int[] { 0, 12 }));
            Assert.That(result.Channels[0].ValueOffsets, Is.EqualTo(new int[] { 4, 16 }));
            Assert.That(result.Channels[0].OutTangentOffsets, Is.EqualTo(new int[] { 8, 20 }));
            Assert.That(
                HasIssue(result.Diagnostics, GltfIssueCodes.AccessorAnimationSamplerOutputNonNormalizedQuaternion),
                Is.False);
        });
    }

    [Test]
    public void AnalyzeAnimation_MapsPackedCubicMorphWeights()
    {
        GltfDocument document = CreateDocumentWithNode();
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Targets.Add(new GltfMorphTarget());
        primitive.Targets.Add(new GltfMorphTarget());
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Nodes[0].HasMesh = true;
        document.Nodes[0].Mesh = 0;
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 2.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0, 0.0,
            0.25, 0.75,
            0.1, 0.2,
            0.3, 0.4,
            0.5, 0.5,
            0.0, 0.0);
        GltfAnimationSampler sampler = CreateSampler(input, output);
        sampler.HasInterpolation = true;
        sampler.Interpolation = GltfAnimationInterpolation.CubicSpline;
        AddAnimation(document, sampler, CreateChannel(GltfAnimationPath.Weights));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Channels[0].MorphTargetCount, Is.EqualTo(2));
            Assert.That(result.Channels[0].ValueComponentCount, Is.EqualTo(2));
            Assert.That(result.Channels[0].ExpectedOutputElementCount, Is.EqualTo(12));
            Assert.That(result.Channels[0].InTangentOffsets, Is.EqualTo(new int[] { 0, 6 }));
            Assert.That(result.Channels[0].ValueOffsets, Is.EqualTo(new int[] { 2, 8 }));
            Assert.That(result.Channels[0].OutTangentOffsets, Is.EqualTo(new int[] { 4, 10 }));
        });
    }

    [Test]
    public void AnalyzeAnimation_AcceptsNormalizedIntegerRotationOutput()
    {
        GltfDocument document = CreateDocumentWithNode();
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0, 1.0);
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest();
        request.Document = document;
        request.ComponentType = GltfComponentType.SignedShort;
        request.Type = GltfAccessorType.Vector4;
        request.Normalized = true;
        request.SignedValues = [0, 0, 0, 32767, 0, 0, 32767, 0];
        int output = new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
        AddAnimation(
            document,
            CreateSampler(input, output),
            CreateChannel(GltfAnimationPath.Rotation));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Samplers[0].OutputValues[3], Is.EqualTo(1.0));
            Assert.That(result.Samplers[0].OutputValues[6], Is.EqualTo(1.0));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void AnalyzeAnimation_ReportsInvalidTimesAndCubicKeyCount()
    {
        GltfDocument invalidTimes = CreateDocumentWithNode();
        int times = CreateFloatingAccessor(invalidTimes, GltfAccessorType.Scalar, -1.0, -1.0);
        int values = CreateFloatingAccessor(
            invalidTimes,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 1.0, 1.0);
        AddAnimation(
            invalidTimes,
            CreateSampler(times, values),
            CreateChannel(GltfAnimationPath.Scale));

        GltfDocument tooFew = CreateDocumentWithNode();
        int oneTime = CreateFloatingAccessor(tooFew, GltfAccessorType.Scalar, 0.0);
        int triplet = CreateFloatingAccessor(
            tooFew,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0,
            1.0, 1.0, 1.0,
            0.0, 0.0, 0.0);
        GltfAnimationSampler cubic = CreateSampler(oneTime, triplet);
        cubic.HasInterpolation = true;
        cubic.Interpolation = GltfAnimationInterpolation.CubicSpline;
        AddAnimation(tooFew, cubic, CreateChannel(GltfAnimationPath.Scale));
        AnimationLayoutAnalyzer analyzer = new AnimationLayoutAnalyzer();

        AnimationLayoutAnalysis timeResult = analyzer.AnalyzeAnimation(invalidTimes, 0);
        AnimationLayoutAnalysis countResult = analyzer.AnalyzeAnimation(tooFew, 0);

        Assert.Multiple(() =>
        {
            Assert.That(timeResult.IsValid, Is.False);
            Assert.That(HasIssue(timeResult.Diagnostics, GltfIssueCodes.AccessorAnimationInputNegative), Is.True);
            Assert.That(HasIssue(timeResult.Diagnostics, GltfIssueCodes.AccessorAnimationInputNonIncreasing), Is.True);
            Assert.That(countResult.IsValid, Is.False);
            Assert.That(
                HasIssue(countResult.Diagnostics, GltfIssueCodes.AnimationSamplerInputAccessorTooFewElements),
                Is.True);
        });
    }

    [Test]
    public void AnalyzeAnimation_ReportsOutputShapeCountAndQuaternionErrors()
    {
        GltfDocument shapeDocument = CreateDocumentWithNode();
        int shapeInput = CreateFloatingAccessor(shapeDocument, GltfAccessorType.Scalar, 0.0, 1.0);
        int wrongShape = CreateFloatingAccessor(
            shapeDocument,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 1.0);
        AddAnimation(
            shapeDocument,
            CreateSampler(shapeInput, wrongShape),
            CreateChannel(GltfAnimationPath.Translation));

        GltfDocument quaternionDocument = CreateDocumentWithNode();
        int quaternionInput = CreateFloatingAccessor(quaternionDocument, GltfAccessorType.Scalar, 0.0);
        int badQuaternion = CreateFloatingAccessor(
            quaternionDocument,
            GltfAccessorType.Vector4,
            0.0, 0.0, 0.0, 2.0);
        AddAnimation(
            quaternionDocument,
            CreateSampler(quaternionInput, badQuaternion),
            CreateChannel(GltfAnimationPath.Rotation));
        AnimationLayoutAnalyzer analyzer = new AnimationLayoutAnalyzer();

        AnimationLayoutAnalysis shapeResult = analyzer.AnalyzeAnimation(shapeDocument, 0);
        AnimationLayoutAnalysis quaternionResult = analyzer.AnalyzeAnimation(quaternionDocument, 0);

        Assert.Multiple(() =>
        {
            Assert.That(
                HasIssue(shapeResult.Diagnostics, GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat),
                Is.True);
            Assert.That(
                HasIssue(shapeResult.Diagnostics, GltfIssueCodes.AnimationSamplerOutputAccessorInvalidCount),
                Is.True);
            Assert.That(
                HasIssue(
                    quaternionResult.Diagnostics,
                    GltfIssueCodes.AccessorAnimationSamplerOutputNonNormalizedQuaternion),
                Is.True);
        });
    }

    [Test]
    public void AnalyzeAnimation_ReportsInvalidTargetsAndDuplicateTargets()
    {
        GltfDocument document = CreateDocumentWithNode();
        document.Nodes[0].HasMatrix = true;
        document.Nodes[0].HasSkin = true;
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0);
        int output = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            0.0, 0.0, 0.0);
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(CreateSampler(input, output));
        animation.Channels.Add(CreateChannel(GltfAnimationPath.Translation));
        animation.Channels.Add(CreateChannel(GltfAnimationPath.Translation));
        document.Animations.Add(animation);

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AnimationChannelTargetNodeMatrix), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AnimationChannelTargetNodeSkin), Is.True);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.AnimationDuplicateTargets), Is.True);
            Assert.That(
                FindIssue(result.Diagnostics, GltfIssueCodes.AnimationChannelTargetNodeSkin)!.Severity,
                Is.EqualTo(GltfValidationSeverity.Warning));
        });
    }

    [Test]
    public void AnalyzeAnimation_ReportsWeightsWithoutMorphsAndInterleavedData()
    {
        GltfDocument document = CreateDocumentWithNode();
        int input = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.0);
        int output = CreateFloatingAccessor(document, GltfAccessorType.Scalar, 0.5);
        document.BufferViews[document.Accessors[input].BufferView].HasByteStride = true;
        document.BufferViews[document.Accessors[input].BufferView].ByteStride = 4;
        AddAnimation(
            document,
            CreateSampler(input, output),
            CreateChannel(GltfAnimationPath.Weights));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                HasIssue(result.Diagnostics, GltfIssueCodes.AnimationChannelTargetNodeWeightsNoMorphs),
                Is.True);
            Assert.That(
                HasIssue(result.Diagnostics, GltfIssueCodes.AnimationSamplerAccessorWithBytestride),
                Is.True);
        });
    }

    [Test]
    public void AnalyzeAnimation_PropagatesUnavailableAccessorDataAndRejectsNullDocument()
    {
        GltfDocument document = CreateDocumentWithNode();
        GltfAccessor input = new GltfAccessor();
        input.Count = 1;
        input.Minimum = [0.0];
        input.Maximum = [0.0];
        document.Accessors.Add(input);
        GltfAccessor output = new GltfAccessor();
        output.Type = GltfAccessorType.Vector3;
        output.Count = 1;
        document.Accessors.Add(output);
        AddAnimation(
            document,
            CreateSampler(0, 1),
            CreateChannel(GltfAnimationPath.Translation));

        AnimationLayoutAnalysis result = new AnimationLayoutAnalyzer().AnalyzeAnimation(document, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Samplers[0].InputReady, Is.False);
            Assert.That(result.Samplers[0].OutputReady, Is.False);
            Assert.That(HasIssue(result.Diagnostics, GltfIssueCodes.UnresolvedReference), Is.True);
            Assert.That(
                () => new AnimationLayoutAnalyzer().AnalyzeAnimation(null!, 0),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
        });
    }

    private static GltfDocument CreateDocumentWithNode()
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode());
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

    private static bool HasIssue(List<GltfValidationIssue> issues, string code)
    {
        return FindIssue(issues, code) != null;
    }

    private static GltfValidationIssue? FindIssue(List<GltfValidationIssue> issues, string code)
    {
        int issueIndex = 0;
        while (issueIndex < issues.Count)
        {
            if (issues[issueIndex].Code == code)
            {
                return issues[issueIndex];
            }

            issueIndex++;
        }

        return null;
    }
}
