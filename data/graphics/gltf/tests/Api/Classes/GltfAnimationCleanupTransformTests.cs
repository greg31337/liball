using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfAnimationCleanupTransformTests
{
    [Test]
    public void RemoveRedundantKeys_CollapsesExactLinearSegmentsAndPreservesSamples()
    {
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0, 3.0 },
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 2.0, 3.0,
                2.0, 4.0, 6.0,
                4.0, 8.0, 12.0
            });
        GltfAnimationSampler sourceSampler = source.Animations[0].Samplers[0];
        source.Accessors[sourceSampler.Output].Name = "translation samples";
        source.Accessors[sourceSampler.Output].PropertyJson.HasExtras = true;
        source.Accessors[sourceSampler.Output].PropertyJson.Extras =
            new JsonObject { ["owner"] = "animation" };
        double[][] expected = SampleTranslations(source, new[] { 0.5, 1.0, 1.5, 2.5 });

        GltfTransformReport report = Transform(source, null, true);
        GltfDocument result = report.Document!;
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];
        double[][] actual = SampleTranslations(result, new[] { 0.5, 1.0, 1.5, 2.5 });

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0, 3.0 }));
            Assert.That(ReadFloating(result, sampler.Output),
                Is.EqualTo(new double[]
                {
                    0.0, 0.0, 0.0,
                    2.0, 4.0, 6.0,
                    4.0, 8.0, 12.0
                }));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(result.Accessors[sampler.Output].Name,
                Is.EqualTo("translation samples"));
            Assert.That(result.Accessors[sampler.Output]
                .PropertyJson.Extras!["owner"]!.GetValue<string>(),
                Is.EqualTo("animation"));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "animations/0/samplers/0" }));
            Assert.That(source.Accessors[sourceSampler.Input].Count, Is.EqualTo(4));
            Assert.That(source.Accessors[sourceSampler.Output].Count, Is.EqualTo(4));
        });
    }

    [Test]
    public void RemoveRedundantKeys_AppliesConfiguredValueTolerance()
    {
        GltfDocument exact = CreateAnimationDocument(
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                0.0, 0.0, 0.0,
                1.001, 0.0, 0.0,
                2.0, 0.0, 0.0
            });
        GltfDocument tolerant = CreateAnimationDocument(
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                0.0, 0.0, 0.0,
                1.001, 0.0, 0.0,
                2.0, 0.0, 0.0
            });
        JsonObject settings = new JsonObject { ["tolerance"] = 0.002 };

        GltfDocument exactResult = Transform(exact, null, true).Document!;
        GltfDocument tolerantResult = Transform(tolerant, settings, true).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Accessors[
                exactResult.Animations[0].Samplers[0].Input].Count,
                Is.EqualTo(3));
            Assert.That(tolerantResult.Accessors[
                tolerantResult.Animations[0].Samplers[0].Input].Count,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void RemoveRedundantKeys_PreservesStepTransitionsAndClipDuration()
    {
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.Step,
            GltfAnimationPath.Scale,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0, 3.0 },
            new double[]
            {
                1.0, 1.0, 1.0,
                1.0, 1.0, 1.0,
                2.0, 2.0, 2.0,
                2.0, 2.0, 2.0
            });
        double[][] expected = SampleScales(
            source,
            new[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 });

        GltfDocument result = Transform(source, null, true).Document!;
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];
        double[][] actual = SampleScales(
            result,
            new[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 });

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0, 3.0 }));
            Assert.That(ReadFloating(result, sampler.Output),
                Is.EqualTo(new double[]
                {
                    1.0, 1.0, 1.0,
                    2.0, 2.0, 2.0,
                    2.0, 2.0, 2.0
                }));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(new GltfAnimationEvaluator().CalculateAnimationDuration(
                result,
                0),
                Is.EqualTo(3.0));
        });
    }

    [Test]
    public void RemoveRedundantKeys_CollapsesExactCubicSplineAndCopiesTriplets()
    {
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.CubicSpline,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                9.0, 9.0, 9.0,
                0.0, 0.0, 0.0,
                1.0, 2.0, 3.0,
                1.0, 2.0, 3.0,
                1.0, 2.0, 3.0,
                1.0, 2.0, 3.0,
                1.0, 2.0, 3.0,
                2.0, 4.0, 6.0,
                8.0, 8.0, 8.0
            });
        double[][] expected = SampleTranslations(
            source,
            new[] { 0.25, 0.5, 1.0, 1.5, 1.75 });

        GltfDocument result = Transform(source, null, true).Document!;
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];
        double[][] actual = SampleTranslations(
            result,
            new[] { 0.25, 0.5, 1.0, 1.5, 1.75 });

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0 }));
            Assert.That(ReadFloating(result, sampler.Output),
                Is.EqualTo(new double[]
                {
                    9.0, 9.0, 9.0,
                    0.0, 0.0, 0.0,
                    1.0, 2.0, 3.0,
                    1.0, 2.0, 3.0,
                    2.0, 4.0, 6.0,
                    8.0, 8.0, 8.0
                }));
            Assert.That(result.Accessors[sampler.Output].Count, Is.EqualTo(6));
            Assert.That(actual, Is.EqualTo(expected));
        });
    }

    [Test]
    public void RemoveRedundantKeys_KeepsCubicKeyWithDiscontinuousTangents()
    {
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.CubicSpline,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                0.0, 0.0, 0.0,
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                2.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                2.0, 0.0, 0.0,
                0.0, 0.0, 0.0
            });

        GltfTransformReport report = Transform(source, null, true);
        GltfAnimationSampler sampler = report.Document!.Animations[0].Samplers[0];

        Assert.Multiple(() =>
        {
            Assert.That(report.Steps[0].ChangedObjects, Is.Empty);
            Assert.That(report.Document.Accessors[sampler.Input].Count, Is.EqualTo(3));
            Assert.That(report.Document.Accessors[sampler.Output].Count, Is.EqualTo(9));
        });
    }

    [Test]
    public void RemoveRedundantKeys_UsesSphericalInterpolationForRotations()
    {
        double halfTurnComponent = System.Math.Sqrt(0.5);
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Rotation,
            GltfAccessorType.Vector4,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                0.0, 0.0, 0.0, 1.0,
                0.0, 0.0, halfTurnComponent, halfTurnComponent,
                0.0, 0.0, 1.0, 0.0
            });
        JsonObject settings = new JsonObject { ["valueTolerance"] = 0.000001 };

        GltfDocument result = Transform(source, settings, true).Document!;
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];
        GltfAnimationPose middle = SampleAnimation(result, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0 }));
            Assert.That(middle.Rotations[0][2],
                Is.EqualTo(halfTurnComponent).Within(0.000001));
            Assert.That(middle.Rotations[0][3],
                Is.EqualTo(halfTurnComponent).Within(0.000001));
        });
    }

    [Test]
    public void RemoveRedundantKeys_HandlesPackedMorphWeightValues()
    {
        GltfDocument source = new GltfDocument();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        int position = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        int targetIndex = 0;
        while (targetIndex < 2)
        {
            int displacement = CreateFloatingAccessor(
                source,
                GltfAccessorType.Vector3,
                new double[9]);
            GltfMorphTarget target = new GltfMorphTarget();
            target.Attributes.Add(new GltfAttribute
            {
                Semantic = "POSITION",
                Accessor = displacement
            });
            primitive.Targets.Add(target);
            targetIndex++;
        }
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        source.Meshes.Add(mesh);
        source.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 0 });
        int input = CreateFloatingAccessor(
            source,
            GltfAccessorType.Scalar,
            new double[] { 0.0, 1.0, 2.0 });
        int output = CreateFloatingAccessor(
            source,
            GltfAccessorType.Scalar,
            new double[]
            {
                0.0, 1.0,
                0.5, 0.5,
                1.0, 0.0
            });
        AddAnimation(
            source,
            input,
            output,
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Weights);

        GltfDocument result = Transform(source, null, true).Document!;
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0 }));
            Assert.That(ReadFloating(result, sampler.Output),
                Is.EqualTo(new double[] { 0.0, 1.0, 1.0, 0.0 }));
            Assert.That(result.Accessors[sampler.Output].Count, Is.EqualTo(4));
        });
    }

    [Test]
    public void RemoveRedundantKeys_RejectsInvalidToleranceSettings()
    {
        GltfDocument source = CreateAnimationDocument(
            GltfAnimationInterpolation.Linear,
            GltfAnimationPath.Translation,
            GltfAccessorType.Vector3,
            new double[] { 0.0, 1.0, 2.0 },
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                2.0, 0.0, 0.0
            });
        JsonObject settings = new JsonObject { ["valueTolerance"] = -1.0 };

        Assert.That(
            () => Transform(source, settings, false),
            Throws.ArgumentException.With.Message.Contains("valueTolerance"));
    }

    private static GltfDocument CreateAnimationDocument(
        GltfAnimationInterpolation interpolation,
        GltfAnimationPath path,
        GltfAccessorType outputType,
        double[] times,
        double[] values)
    {
        GltfDocument result = new GltfDocument();
        result.Nodes.Add(new GltfNode());
        int input = CreateFloatingAccessor(
            result,
            GltfAccessorType.Scalar,
            times);
        int output = CreateFloatingAccessor(result, outputType, values);
        AddAnimation(result, input, output, interpolation, path);
        return result;
    }

    private static void AddAnimation(
        GltfDocument document,
        int input,
        int output,
        GltfAnimationInterpolation interpolation,
        GltfAnimationPath path)
    {
        GltfAnimationSampler sampler = new GltfAnimationSampler
        {
            Input = input,
            Output = output,
            HasInterpolation = true,
            Interpolation = interpolation
        };
        GltfAnimationChannel channel = new GltfAnimationChannel
        {
            Sampler = 0,
            Target = new GltfAnimationTarget
            {
                HasNode = true,
                Node = 0,
                Path = path
            }
        };
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        double[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.Float,
            Type = type,
            FloatingValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        JsonObject? settings,
        bool validate)
    {
        GltfTransformStep step = new GltfTransformStep
        {
            Kind = GltfTransformKind.RemoveRedundantAnimationKeys
        };
        if (settings != null)
        {
            step.Settings = settings;
        }

        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan { Steps = [step] },
            new GltfTransformOptions
            {
                ValidateBefore = validate,
                ValidateAfter = validate
            });
    }

    private static double[] ReadFloating(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadFloatingValues(request).FloatingValues;
    }

    private static double[][] SampleTranslations(
        GltfDocument document,
        double[] times)
    {
        double[][] result = new double[times.Length][];
        int index = 0;
        while (index < times.Length)
        {
            result[index] = SampleAnimation(document, times[index]).Translations[0];
            index++;
        }

        return result;
    }

    private static double[][] SampleScales(GltfDocument document, double[] times)
    {
        double[][] result = new double[times.Length][];
        int index = 0;
        while (index < times.Length)
        {
            result[index] = SampleAnimation(document, times[index]).Scales[0];
            index++;
        }

        return result;
    }

    private static GltfAnimationPose SampleAnimation(
        GltfDocument document,
        double time)
    {
        GltfAnimationEvaluationOptions options = new GltfAnimationEvaluationOptions
        {
            Animation = 0,
            Time = time,
            WrapMode = GltfAnimationWrapMode.Clamp
        };
        return new GltfAnimationEvaluator().SampleAnimation(document, options);
    }
}
