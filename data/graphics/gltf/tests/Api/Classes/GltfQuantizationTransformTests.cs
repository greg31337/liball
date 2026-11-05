using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfQuantizationTransformTests
{
    [Test]
    public void Quantize_SelectsSmallestLegalNormalizedFormatsAndAddsDeclarations()
    {
        double diagonal = System.Math.Sqrt(0.5);
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        GltfMeshPrimitive sourcePrimitive = source.Meshes[0].Primitives[0];
        int normal = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                diagonal, diagonal, 0.0,
                diagonal, diagonal, 0.0,
                diagonal, diagonal, 0.0
            });
        int color = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector4,
            new double[]
            {
                0.0, 0.5, 1.0, 1.0,
                0.0, 0.5, 1.0, 1.0,
                0.0, 0.5, 1.0, 1.0
            });
        sourcePrimitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "NORMAL",
            Accessor = normal
        });
        sourcePrimitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "COLOR_0",
            Accessor = color
        });
        JsonObject settings = new JsonObject { ["tolerance"] = 0.004 };

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.Quantize,
            settings,
            true);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        GltfAccessor quantizedPosition = result.Accessors[
            FindAttribute(primitive, "POSITION")];
        GltfAccessor quantizedNormal = result.Accessors[
            FindAttribute(primitive, "NORMAL")];
        GltfAccessor quantizedColor = result.Accessors[
            FindAttribute(primitive, "COLOR_0")];

        Assert.Multiple(() =>
        {
            Assert.That(quantizedPosition.ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(quantizedPosition.Normalized, Is.True);
            Assert.That(quantizedNormal.ComponentType,
                Is.EqualTo(GltfComponentType.SignedByte));
            Assert.That(quantizedNormal.Normalized, Is.True);
            Assert.That(quantizedColor.ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(quantizedColor.Normalized, Is.True);
            Assert.That(result.BufferViews[quantizedPosition.BufferView].ByteStride,
                Is.EqualTo(4));
            Assert.That(result.BufferViews[quantizedPosition.BufferView].HasByteStride,
                Is.True);
            Assert.That(result.BufferViews[quantizedNormal.BufferView].ByteStride,
                Is.EqualTo(4));
            Assert.That(result.ExtensionsUsed,
                Does.Contain(GltfExtensionNames.KhrMeshQuantization));
            Assert.That(result.ExtensionsRequired,
                Does.Contain(GltfExtensionNames.KhrMeshQuantization));
            Assert.That(MaximumError(
                ReadFloating(result, FindAttribute(primitive, "POSITION")),
                new double[]
                {
                    0.0, 0.0, 0.0,
                    0.5, 0.0, 0.0,
                    0.0, 1.0, 0.0
                }), Is.LessThanOrEqualTo(0.004));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[]
                {
                    "meshes/0/primitives/0",
                    "extensionsUsed",
                    "extensionsRequired"
                }));
            Assert.That(source.Accessors[
                FindAttribute(sourcePrimitive, "POSITION")].ComponentType,
                Is.EqualTo(GltfComponentType.Float));
        });
    }

    [Test]
    public void Quantize_UsesSixteenBitsWhenEightBitsExceedTolerance()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        JsonObject settings = new JsonObject { ["tolerance"] = 0.0001 };

        GltfDocument result = Transform(
            source,
            GltfTransformKind.Quantize,
            settings,
            true).Document!;
        GltfAccessor position = result.Accessors[FindAttribute(
            result.Meshes[0].Primitives[0],
            "POSITION")];

        Assert.Multiple(() =>
        {
            Assert.That(position.ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(position.Normalized, Is.True);
            Assert.That(result.BufferViews[position.BufferView].ByteStride,
                Is.EqualTo(8));
            Assert.That(result.BufferViews[position.BufferView].HasByteStride,
                Is.True);
        });
    }

    [Test]
    public void Quantize_LeavesAccessorFloatingWhenNoEncodingMeetsErrorLimit()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        JsonObject settings = new JsonObject { ["tolerance"] = 0.0 };

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.Quantize,
            settings,
            true);
        GltfAccessor position = report.Document!.Accessors[FindAttribute(
            report.Document.Meshes[0].Primitives[0],
            "POSITION")];

        Assert.Multiple(() =>
        {
            Assert.That(position.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(report.Document.ExtensionsUsed, Is.Empty);
            Assert.That(report.Document.ExtensionsRequired, Is.Empty);
            Assert.That(report.Steps[0].ChangedObjects, Is.Empty);
        });
    }

    [Test]
    public void Quantize_UsesUnnormalizedIntegersForExactValuesOutsideUnitRange()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                10.0, 0.0, 0.0,
                20.0, 0.0, 0.0,
                10.0, 30.0, 0.0
            });

        GltfDocument result = Transform(
            source,
            GltfTransformKind.Quantize,
            new JsonObject { ["tolerance"] = 0.0 },
            true).Document!;
        GltfAccessor position = result.Accessors[FindAttribute(
            result.Meshes[0].Primitives[0],
            "POSITION")];

        Assert.Multiple(() =>
        {
            Assert.That(position.ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(position.Normalized, Is.False);
            Assert.That(ReadFloating(result, FindAttribute(
                result.Meshes[0].Primitives[0],
                "POSITION")), Is.EqualTo(new double[]
                {
                    10.0, 0.0, 0.0,
                    20.0, 0.0, 0.0,
                    10.0, 30.0, 0.0
                }));
        });
    }

    [Test]
    public void Dequantize_RestoresFloatAttributesAndRemovesMarkerDeclarations()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        GltfDocument quantized = Transform(
            source,
            GltfTransformKind.Quantize,
            new JsonObject { ["tolerance"] = 0.004 },
            true).Document!;
        double[] expected = ReadFloating(
            quantized,
            FindAttribute(quantized.Meshes[0].Primitives[0], "POSITION"));

        GltfTransformReport report = Transform(
            quantized,
            GltfTransformKind.Dequantize,
            null,
            true);
        GltfDocument result = report.Document!;
        GltfAccessor position = result.Accessors[FindAttribute(
            result.Meshes[0].Primitives[0],
            "POSITION")];

        Assert.Multiple(() =>
        {
            Assert.That(position.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(position.Normalized, Is.False);
            Assert.That(ReadFloating(result, FindAttribute(
                result.Meshes[0].Primitives[0],
                "POSITION")), Is.EqualTo(expected).Within(0.0000001));
            Assert.That(result.ExtensionsUsed,
                Does.Not.Contain(GltfExtensionNames.KhrMeshQuantization));
            Assert.That(result.ExtensionsRequired,
                Does.Not.Contain(GltfExtensionNames.KhrMeshQuantization));
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
        });
    }

    [Test]
    public void Quantize_CoreColorEncodingDoesNotRequireMeshQuantizationMarker()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
        });
        GltfMeshPrimitive primitive = source.Meshes[0].Primitives[0];
        int color = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector4,
            new double[]
            {
                0.0, 0.0, 1.0, 1.0,
                0.0, 1.0, 1.0, 1.0,
                1.0, 0.0, 1.0, 1.0
            });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "COLOR_0",
            Accessor = color
        });

        GltfDocument result = Transform(
            source,
            GltfTransformKind.Quantize,
            new JsonObject { ["tolerance"] = 0.0 },
            true).Document!;
        GltfAccessor quantizedColor = result.Accessors[FindAttribute(
            result.Meshes[0].Primitives[0],
            "COLOR_0")];

        Assert.Multiple(() =>
        {
            Assert.That(quantizedColor.ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedByte));
            Assert.That(quantizedColor.Normalized, Is.True);
            Assert.That(result.ExtensionsUsed, Is.Empty);
            Assert.That(result.ExtensionsRequired, Is.Empty);
        });
    }

    [Test]
    public void Quantize_SelectsWeightEncodingThatPreservesNormalizedSums()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                0.5, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        GltfMeshPrimitive primitive = source.Meshes[0].Primitives[0];
        int joints = CreateUnsignedAccessor(
            source,
            GltfAccessorType.Vector4,
            new uint[12]);
        int weights = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector4,
            new double[]
            {
                0.1, 0.2, 0.3, 0.4,
                0.1, 0.2, 0.3, 0.4,
                0.1, 0.2, 0.3, 0.4
            });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "JOINTS_0",
            Accessor = joints
        });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "WEIGHTS_0",
            Accessor = weights
        });

        GltfDocument result = Transform(
            source,
            GltfTransformKind.Quantize,
            new JsonObject { ["tolerance"] = 0.004 },
            true).Document!;
        int resultWeights = FindAttribute(
            result.Meshes[0].Primitives[0],
            "WEIGHTS_0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Accessors[resultWeights].ComponentType,
                Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(result.Accessors[resultWeights].Normalized, Is.True);
            Assert.That(ReadFloating(result, resultWeights).Chunk(4)
                .Select(values => values.Sum()),
                Has.All.EqualTo(1.0).Within(0.0001));
        });
    }

    [Test]
    public void Quantize_RejectsInvalidTolerance()
    {
        GltfDocument source = CreateTriangleDocument(
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0
            });

        Assert.That(
            () => Transform(
                source,
                GltfTransformKind.Quantize,
                new JsonObject { ["tolerance"] = -1.0 },
                false),
            Throws.ArgumentException.With.Message.Contains("tolerance"));
    }

    private static GltfDocument CreateTriangleDocument(double[] positions)
    {
        GltfDocument result = new GltfDocument();
        int position = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            positions);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = position
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        return result;
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

    private static int CreateUnsignedAccessor(
        GltfDocument document,
        GltfAccessorType type,
        uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedByte,
            Type = type,
            UnsignedValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformKind kind,
        JsonObject? settings,
        bool validate)
    {
        GltfTransformStep step = new GltfTransformStep { Kind = kind };
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

    private static int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            if (primitive.Attributes[index].Semantic == semantic)
            {
                return primitive.Attributes[index].Accessor;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    private static double[] ReadFloating(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor,
            ApplyNormalization = true
        };
        return new GltfAccessorCodec().ReadFloatingValues(request).FloatingValues;
    }

    private static double MaximumError(double[] actual, double[] expected)
    {
        double result = 0.0;
        int index = 0;
        while (index < actual.Length)
        {
            double error = System.Math.Abs(actual[index] - expected[index]);
            if (error > result)
            {
                result = error;
            }

            index++;
        }

        return result;
    }
}
