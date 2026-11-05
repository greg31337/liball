namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfGeometryGenerationTransformTests
{
    [Test]
    public void GenerateBounds_ReplacesStaleScalarAndVectorBoundsWithoutChangingData()
    {
        GltfDocument source = new GltfDocument();
        int position = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                -2.0, 4.0, 1.0,
                3.0, -5.0, 8.0,
                0.0, 2.0, -7.0
            });
        int scalar = CreateFloatingAccessor(
            source,
            GltfAccessorType.Scalar,
            new double[] { 5.0, -1.0, 2.0 });
        source.Accessors[position].Minimum = new double[] { 0.0, 0.0, 0.0 };
        source.Accessors[position].Maximum = new double[] { 1.0, 1.0, 1.0 };
        source.Accessors[scalar].Minimum = null;
        source.Accessors[scalar].Maximum = null;
        byte[] positionBytes = ReadRaw(source, position);

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.GenerateBounds,
            false,
            true);
        GltfDocument result = report.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Accessors[position].Minimum,
                Is.EqualTo(new double[] { -2.0, -5.0, -7.0 }));
            Assert.That(result.Accessors[position].Maximum,
                Is.EqualTo(new double[] { 3.0, 4.0, 8.0 }));
            Assert.That(result.Accessors[scalar].Minimum,
                Is.EqualTo(new double[] { -1.0 }));
            Assert.That(result.Accessors[scalar].Maximum,
                Is.EqualTo(new double[] { 5.0 }));
            Assert.That(ReadRaw(result, position), Is.EqualTo(positionBytes));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "accessors/0", "accessors/1" }));
            Assert.That(source.Accessors[position].Minimum,
                Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
            Assert.That(source.Accessors[scalar].Minimum, Is.Null);
        });
    }

    [Test]
    public void GenerateBounds_DecodesSparsePositionStorageAndIsIdempotent()
    {
        GltfDocument source = new GltfDocument();
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = source,
            ComponentType = GltfComponentType.Float,
            Type = GltfAccessorType.Vector3,
            FloatingValues = new double[]
            {
                0.0, 0.0, 0.0,
                -3.0, 2.0, 6.0,
                0.0, 0.0, 0.0
            },
            AllowSparse = true
        };
        int position = new GltfAccessorCodec().CreateSparseAccessor(request).Accessor;
        source.Accessors[position].Minimum = null;
        source.Accessors[position].Maximum = null;

        GltfDocument first = Transform(
            source,
            GltfTransformKind.GenerateBounds,
            false,
            false).Document!;
        GltfTransformReport second = Transform(
            first,
            GltfTransformKind.GenerateBounds,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Accessors[position].Minimum,
                Is.EqualTo(new double[] { -3.0, 0.0, 0.0 }));
            Assert.That(first.Accessors[position].Maximum,
                Is.EqualTo(new double[] { 0.0, 2.0, 6.0 }));
            Assert.That(second.Steps[0].ChangedObjects, Is.Empty);
        });
    }

    [Test]
    public void GenerateNormals_UsesUnnormalizedFaceCrossProductsAsAreaWeights()
    {
        GltfDocument source = CreatePrimitiveDocument(
            GltfPrimitiveMode.Triangles,
            new double[]
            {
                0.0, 0.0, 0.0,
                2.0, 0.0, 0.0,
                0.0, 2.0, 0.0,
                0.0, 0.0, 1.0
            },
            new uint[] { 0, 1, 2, 0, 3, 1 });
        GltfMeshPrimitive sourcePrimitive = source.Meshes[0].Primitives[0];
        int oldNormal = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, -1.0,
                0.0, 0.0, -1.0,
                0.0, 0.0, -1.0,
                0.0, 0.0, -1.0
            });
        source.Accessors[oldNormal].Name = "generated normal target";
        SetArrayBufferTarget(source, oldNormal);
        sourcePrimitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "NORMAL",
            Accessor = oldNormal
        });

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.GenerateNormals,
            true,
            true);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        int normal = FindAttribute(primitive, "NORMAL");
        double[] values = ReadFloating(result, normal);
        double inverseLength = 1.0 / System.Math.Sqrt(20.0);

        Assert.Multiple(() =>
        {
            Assert.That(normal, Is.Not.EqualTo(oldNormal));
            Assert.That(result.Accessors[normal].ComponentType,
                Is.EqualTo(GltfComponentType.Float));
            Assert.That(result.Accessors[normal].Type,
                Is.EqualTo(GltfAccessorType.Vector3));
            Assert.That(result.Accessors[normal].Name,
                Is.EqualTo("generated normal target"));
            Assert.That(values[0], Is.EqualTo(0.0).Within(0.000001));
            Assert.That(values[1], Is.EqualTo(2.0 * inverseLength).Within(0.000001));
            Assert.That(values[2], Is.EqualTo(4.0 * inverseLength).Within(0.000001));
            Assert.That(values[6], Is.EqualTo(0.0).Within(0.000001));
            Assert.That(values[7], Is.EqualTo(0.0).Within(0.000001));
            Assert.That(values[8], Is.EqualTo(1.0).Within(0.000001));
            Assert.That(values[9], Is.EqualTo(0.0).Within(0.000001));
            Assert.That(values[10], Is.EqualTo(1.0).Within(0.000001));
            Assert.That(values[11], Is.EqualTo(0.0).Within(0.000001));
            Assert.That(result.BufferViews[result.Accessors[normal].BufferView].Target,
                Is.EqualTo(GltfBufferViewTarget.ArrayBuffer));
            Assert.That(report.Steps[0].Diagnostics, Is.Empty);
            Assert.That(FindAttribute(sourcePrimitive, "NORMAL"), Is.EqualTo(oldNormal));
        });
    }

    [TestCase(GltfPrimitiveMode.TriangleStrip)]
    [TestCase(GltfPrimitiveMode.TriangleFan)]
    public void GenerateNormals_UsesConsistentWindingForConnectedTriangleModes(
        GltfPrimitiveMode mode)
    {
        double[] positions;
        if (mode == GltfPrimitiveMode.TriangleStrip)
        {
            positions = new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                1.0, 1.0, 0.0
            };
        }
        else
        {
            positions = new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                1.0, 1.0, 0.0,
                0.0, 1.0, 0.0
            };
        }

        GltfDocument source = CreatePrimitiveDocument(
            mode,
            positions,
            new uint[] { 0, 1, 2, 3 });
        if (mode == GltfPrimitiveMode.TriangleFan)
        {
            GltfMeshPrimitive fanPrimitive = source.Meshes[0].Primitives[0];
            GltfBufferView indexView = source.BufferViews[
                source.Accessors[fanPrimitive.Indices].BufferView];
            indexView.HasTarget = false;
            indexView.Target = GltfBufferViewTarget.None;
            fanPrimitive.HasIndices = false;
            fanPrimitive.Indices = GltfConstants.AbsentIndex;
        }

        GltfDocument result = Transform(
            source,
            GltfTransformKind.GenerateNormals,
            true,
            true).Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        double[] normals = ReadFloating(result, FindAttribute(primitive, "NORMAL"));

        int offset = 0;
        while (offset < normals.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(normals[offset], Is.EqualTo(0.0).Within(0.000001));
                Assert.That(normals[offset + 1], Is.EqualTo(0.0).Within(0.000001));
                Assert.That(normals[offset + 2], Is.EqualTo(1.0).Within(0.000001));
            });
            offset += 3;
        }
    }

    [Test]
    public void GenerateNormals_ReportsDegenerateTrianglesAndUndefinedVertices()
    {
        GltfDocument source = CreatePrimitiveDocument(
            GltfPrimitiveMode.Triangles,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                2.0, 0.0, 0.0
            },
            new uint[] { 0, 1, 2, 0, 1, 3 });

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.GenerateNormals,
            true,
            true);
        GltfMeshPrimitive primitive = report.Document!.Meshes[0].Primitives[0];
        double[] normals = ReadFloating(
            report.Document,
            FindAttribute(primitive, "NORMAL"));
        GltfValidationIssue diagnostic = report.Steps[0].Diagnostics[0];

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code,
                Is.EqualTo(GltfIssueCodes.AccessorIndexTriangleDegenerate));
            Assert.That(diagnostic.Severity,
                Is.EqualTo(GltfValidationSeverity.Information));
            Assert.That(diagnostic.JsonPointer,
                Is.EqualTo("/meshes/0/primitives/0"));
            Assert.That(diagnostic.Message,
                Is.EqualTo("Normal generation encountered 1 degenerate triangles and 1 vertices without an accumulated normal."));
            Assert.That(normals[9], Is.EqualTo(0.0));
            Assert.That(normals[10], Is.EqualTo(0.0));
            Assert.That(normals[11], Is.EqualTo(1.0));
            Assert.That(report.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void GenerateNormals_LeavesLinesAndFullyDegenerateTrianglesUnchanged()
    {
        GltfDocument lines = CreatePrimitiveDocument(
            GltfPrimitiveMode.Lines,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0
            },
            new uint[] { 0, 1 });
        GltfDocument degenerate = CreatePrimitiveDocument(
            GltfPrimitiveMode.Triangles,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                2.0, 0.0, 0.0
            },
            new uint[] { 0, 1, 2 });

        GltfTransformReport lineReport = Transform(
            lines,
            GltfTransformKind.GenerateNormals,
            true,
            true);
        GltfTransformReport degenerateReport = Transform(
            degenerate,
            GltfTransformKind.GenerateNormals,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(lineReport.Steps[0].ChangedObjects, Is.Empty);
            Assert.That(FindAttribute(
                lineReport.Document!.Meshes[0].Primitives[0],
                "NORMAL"), Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(degenerateReport.Steps[0].ChangedObjects, Is.Empty);
            Assert.That(degenerateReport.Steps[0].Diagnostics, Has.Count.EqualTo(1));
            Assert.That(FindAttribute(
                degenerateReport.Document!.Meshes[0].Primitives[0],
                "NORMAL"), Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    private static GltfDocument CreatePrimitiveDocument(
        GltfPrimitiveMode mode,
        double[] positions,
        uint[] indices)
    {
        GltfDocument result = new GltfDocument();
        int position = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            positions);
        int indexAccessor = CreateIndexAccessor(result, indices);
        SetArrayBufferTarget(result, position);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = indexAccessor,
            HasMode = true,
            Mode = mode
        };
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

    private static int CreateIndexAccessor(GltfDocument document, uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedShort,
            Type = GltfAccessorType.Scalar,
            UnsignedValues = values
        };
        int accessor = new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
        GltfBufferView view = document.BufferViews[document.Accessors[accessor].BufferView];
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ElementArrayBuffer;
        return accessor;
    }

    private static void SetArrayBufferTarget(GltfDocument document, int accessor)
    {
        GltfBufferView view = document.BufferViews[document.Accessors[accessor].BufferView];
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ArrayBuffer;
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformKind kind,
        bool validateBefore,
        bool validateAfter)
    {
        GltfTransformStep step = new GltfTransformStep { Kind = kind };
        GltfTransformOptions options = new GltfTransformOptions
        {
            ValidateBefore = validateBefore,
            ValidateAfter = validateAfter
        };
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan { Steps = [step] },
            options);
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
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadFloatingValues(request).FloatingValues;
    }

    private static byte[] ReadRaw(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadRawElements(request).RawBytes;
    }
}
