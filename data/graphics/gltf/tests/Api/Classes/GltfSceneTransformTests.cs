using System.Numerics;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfSceneTransformTests
{
    [Test]
    public void ApplyNodeTransform_ChangesOnlySafeSelectedNodes()
    {
        GltfDocument source = new GltfDocument();
        source.Nodes.Add(new GltfNode
        {
            HasTranslation = true,
            Translation = [1.0, 0.0, 0.0]
        });
        source.Nodes.Add(new GltfNode
        {
            HasTranslation = true,
            Translation = [5.0, 0.0, 0.0]
        });
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Target.HasNode = true;
        channel.Target.Node = 1;
        GltfAnimation animation = new GltfAnimation();
        animation.Channels.Add(channel);
        source.Animations.Add(animation);
        JsonObject settings = new JsonObject
        {
            ["nodes"] = new JsonArray(0, 1),
            ["matrix"] = MatrixJson(Matrix4x4.CreateTranslation(0.0f, 2.0f, 0.0f))
        };

        GltfDocument result = Transform(
            source,
            GltfTransformKind.ApplyNodeTransform,
            settings,
            false).Document!;
        Matrix4x4 applied = ReadNodeMatrix(result.Nodes[0]);

        Assert.Multiple(() =>
        {
            Assert.That(applied.M41, Is.EqualTo(1.0f));
            Assert.That(applied.M42, Is.EqualTo(2.0f));
            Assert.That(result.Nodes[1].HasMatrix, Is.False);
            Assert.That(result.Nodes[1].Translation,
                Is.EqualTo(new double[] { 5.0, 0.0, 0.0 }));
            Assert.That(source.Nodes[0].HasMatrix, Is.False);
        });
    }

    [Test]
    public void BakeNodeTransforms_MovesGeometryAndClearsTheLocalTransform()
    {
        GltfDocument source = CreateGeometryDocument(true);
        source.Nodes[0].HasTranslation = true;
        source.Nodes[0].Translation = [2.0, 3.0, 0.0];

        GltfDocument result = Transform(
            source,
            GltfTransformKind.BakeNodeTransforms,
            null,
            true).Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(new double[]
                {
                    2, 3, 0,
                    3, 3, 0,
                    2, 4, 0
                }));
            Assert.That(result.Nodes[0].HasMatrix, Is.False);
            Assert.That(result.Nodes[0].HasTranslation, Is.False);
            Assert.That(source.Nodes[0].HasTranslation, Is.True);
        });
    }

    [Test]
    public void BakeNodeTransforms_SeparatesSharedAccessorsForDifferentTransforms()
    {
        GltfDocument source = new GltfDocument();
        int positions = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0
            });
        int meshIndex = 0;
        while (meshIndex < 2)
        {
            GltfMeshPrimitive primitive = new GltfMeshPrimitive();
            primitive.Attributes.Add(new GltfAttribute
            {
                Semantic = "POSITION",
                Accessor = positions
            });
            GltfMesh mesh = new GltfMesh();
            mesh.Primitives.Add(primitive);
            source.Meshes.Add(mesh);
            source.Nodes.Add(new GltfNode
            {
                HasMesh = true,
                Mesh = meshIndex,
                HasTranslation = true,
                Translation = [meshIndex == 0 ? 2.0 : 10.0, 0.0, 0.0]
            });
            meshIndex++;
        }

        GltfScene scene = new GltfScene { Nodes = [0, 1] };
        source.Scenes.Add(scene);

        GltfDocument result = Transform(
            source,
            GltfTransformKind.BakeNodeTransforms,
            null,
            true).Document!;
        int first = FindAttribute(result.Meshes[0].Primitives[0], "POSITION");
        int second = FindAttribute(result.Meshes[1].Primitives[0], "POSITION");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(ReadFloating(result, first), Is.EqualTo(new double[]
            {
                2.0, 0.0, 0.0,
                3.0, 0.0, 0.0,
                2.0, 1.0, 0.0
            }));
            Assert.That(ReadFloating(result, second), Is.EqualTo(new double[]
            {
                10.0, 0.0, 0.0,
                11.0, 0.0, 0.0,
                10.0, 1.0, 0.0
            }));
            Assert.That(result.Nodes.Select(node => node.HasTranslation),
                Is.All.False);
            Assert.That(source.Accessors, Has.Count.EqualTo(1));
            Assert.That(source.Nodes[0].HasTranslation, Is.True);
            Assert.That(source.Nodes[1].HasTranslation, Is.True);
        });
    }

    [Test]
    public void FlattenHierarchy_PromotesDescendantsWithTheirWorldTransforms()
    {
        GltfDocument source = new GltfDocument();
        GltfNode root = new GltfNode
        {
            HasTranslation = true,
            Translation = [1.0, 0.0, 0.0]
        };
        root.Children.Add(1);
        source.Nodes.Add(root);
        source.Nodes.Add(new GltfNode
        {
            HasTranslation = true,
            Translation = [0.0, 2.0, 0.0]
        });
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        source.Scenes.Add(scene);

        GltfDocument result = Transform(
            source,
            GltfTransformKind.FlattenHierarchy,
            null,
            false).Document!;
        Matrix4x4 child = ReadNodeMatrix(result.Nodes[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Scenes[0].Nodes, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Nodes[0].Children, Is.Empty);
            Assert.That(child.M41, Is.EqualTo(1.0f));
            Assert.That(child.M42, Is.EqualTo(2.0f));
        });
    }

    [Test]
    public void CenterAndGroundScene_TranslateRootsFromEvaluatedBounds()
    {
        GltfDocument centeredSource = CreateOffsetGeometryDocument();
        GltfDocument groundedSource = CreateOffsetGeometryDocument();

        GltfDocument centered = Transform(
            centeredSource,
            GltfTransformKind.CenterScene,
            null,
            true).Document!;
        GltfDocument grounded = Transform(
            groundedSource,
            GltfTransformKind.GroundScene,
            null,
            true).Document!;
        Matrix4x4 center = ReadNodeMatrix(centered.Nodes[0]);
        Matrix4x4 ground = ReadNodeMatrix(grounded.Nodes[0]);

        Assert.Multiple(() =>
        {
            Assert.That(center.M41, Is.EqualTo(-3.0f));
            Assert.That(center.M42, Is.EqualTo(-2.0f));
            Assert.That(ground.M41, Is.Zero);
            Assert.That(ground.M42, Is.EqualTo(-1.0f));
        });
    }

    [Test]
    public void FlipWinding_ReversesTrianglesAndTangentHandedness()
    {
        GltfDocument source = CreateGeometryDocument(true);

        GltfDocument result = Transform(
            source,
            GltfTransformKind.FlipWinding,
            null,
            true).Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        double[] tangents = ReadFloating(result, FindAttribute(primitive, "TANGENT"));

        Assert.Multiple(() =>
        {
            Assert.That(ReadIndices(result, primitive.Indices),
                Is.EqualTo(new uint[] { 0, 2, 1 }));
            Assert.That(new[] { tangents[3], tangents[7], tangents[11] },
                Is.EqualTo(new double[] { -1, -1, -1 }));
        });
    }

    [Test]
    public void ConvertUnits_UpdatesGeometryNodesAnimationSkinsAndCameras()
    {
        GltfDocument source = CreateGeometryDocument(false);
        source.Nodes[0].HasTranslation = true;
        source.Nodes[0].Translation = [1.0, 2.0, 3.0];
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera
        {
            VerticalFieldOfView = 0.75,
            NearPlane = 0.1,
            HasFarPlane = true,
            FarPlane = 100.0
        };
        source.Cameras.Add(new GltfCamera
        {
            Type = GltfCameraType.Perspective,
            Perspective = perspective
        });
        int time = CreateFloatingAccessor(
            source,
            GltfAccessorType.Scalar,
            new double[] { 0.0 });
        int output = CreateFloatingAccessor(
            source,
            GltfAccessorType.Vector3,
            new double[] { 1.0, 2.0, 3.0 });
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(new GltfAnimationSampler
        {
            Input = time,
            Output = output
        });
        GltfAnimationChannel channel = new GltfAnimationChannel { Sampler = 0 };
        channel.Target.HasNode = true;
        channel.Target.Node = 0;
        channel.Target.Path = GltfAnimationPath.Translation;
        animation.Channels.Add(channel);
        source.Animations.Add(animation);
        int inverseBind = CreateMatrixAccessor(
            source,
            new CoordinateConverter().WriteColumnMajorMatrix(
                Matrix4x4.CreateTranslation(-1.0f, 0.0f, 0.0f)));
        source.Skins.Add(new GltfSkin
        {
            HasInverseBindMatrices = true,
            InverseBindMatrices = inverseBind
        });
        JsonObject settings = new JsonObject { ["scale"] = 10.0 };

        GltfDocument result = Transform(
            source,
            GltfTransformKind.ConvertUnits,
            settings,
            false).Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        GltfAnimation resultAnimation = result.Animations[0];
        int animationOutput = resultAnimation.Samplers[
            resultAnimation.Channels[0].Sampler].Output;
        Matrix4x4 convertedBind = new CoordinateConverter().ReadMatrixAt(
            ReadMatrices(result, result.Skins[0].InverseBindMatrices),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(new double[] { 0, 0, 0, 10, 0, 0, 0, 10, 0 }));
            Assert.That(result.Nodes[0].Translation,
                Is.EqualTo(new double[] { 10, 20, 30 }));
            Assert.That(ReadFloating(result, animationOutput),
                Is.EqualTo(new double[] { 10, 20, 30 }));
            Assert.That(convertedBind.M41, Is.EqualTo(-10.0f));
            Assert.That(result.Cameras[0].Perspective!.NearPlane, Is.EqualTo(1.0));
            Assert.That(result.Cameras[0].Perspective!.FarPlane, Is.EqualTo(1000.0));
        });
    }

    [Test]
    public void ConvertAxes_TransformsVectorsAndMaintainsFrontFacingTriangles()
    {
        GltfDocument source = CreateGeometryDocument(true);
        source.Nodes[0].HasTranslation = true;
        source.Nodes[0].Translation = [1.0, 2.0, 3.0];
        JsonObject settings = new JsonObject
        {
            ["axisBasis"] = new JsonArray(
                -1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0)
        };

        GltfDocument result = Transform(
            source,
            GltfTransformKind.ConvertAxes,
            settings,
            true).Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        double[] tangents = ReadFloating(result, FindAttribute(primitive, "TANGENT"));

        Assert.Multiple(() =>
        {
            Assert.That(ReadFloating(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(new double[] { 0, 0, 0, -1, 0, 0, 0, 1, 0 }));
            Assert.That(ReadIndices(result, primitive.Indices),
                Is.EqualTo(new uint[] { 0, 2, 1 }));
            Assert.That(tangents[0], Is.EqualTo(-1.0));
            Assert.That(tangents[3], Is.EqualTo(-1.0));
            Assert.That(result.Nodes[0].Translation,
                Is.EqualTo(new double[] { -1, 2, 3 }));
        });
    }

    [Test]
    public void UnsafeHierarchyAndCoordinateOperations_AreSkippedTransactionally()
    {
        GltfDocument flattenSource = new GltfDocument();
        flattenSource.Nodes.Add(new GltfNode());
        flattenSource.Scenes.Add(new GltfScene { Nodes = [0] });
        flattenSource.Animations.Add(new GltfAnimation());

        GltfTransformReport flattened = Transform(
            flattenSource,
            GltfTransformKind.FlattenHierarchy,
            null,
            false);

        GltfDocument coordinateSource = CreateGeometryDocument(false);
        GltfAnimation pointerAnimation = new GltfAnimation();
        GltfAnimationChannel pointerChannel = new GltfAnimationChannel();
        pointerChannel.Target.Path = GltfAnimationPath.Pointer;
        pointerAnimation.Channels.Add(pointerChannel);
        coordinateSource.Animations.Add(pointerAnimation);
        GltfTransformReport converted = Transform(
            coordinateSource,
            GltfTransformKind.ConvertUnits,
            new JsonObject { ["scale"] = 2.0 },
            false);
        GltfDocument convertedDocument = converted.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(flattened.Steps[0].Status,
                Is.EqualTo(GltfOperationStatus.Skipped));
            Assert.That(flattened.Document!.Nodes[0].HasMatrix, Is.False);
            Assert.That(converted.Steps[0].Status,
                Is.EqualTo(GltfOperationStatus.Skipped));
            Assert.That(ReadFloating(
                convertedDocument,
                FindAttribute(convertedDocument.Meshes[0].Primitives[0], "POSITION")),
                Is.EqualTo(new double[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }));
        });
    }

    private static GltfDocument CreateGeometryDocument(bool tangent)
    {
        GltfDocument result = new GltfDocument();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            HasIndices = true,
            Indices = CreateIndexAccessor(result, new uint[] { 0, 1, 2 }),
            HasMode = true,
            Mode = GltfPrimitiveMode.Triangles
        };
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = CreateFloatingAccessor(
                result,
                GltfAccessorType.Vector3,
                new double[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 })
        });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "NORMAL",
            Accessor = CreateFloatingAccessor(
                result,
                GltfAccessorType.Vector3,
                new double[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 })
        });
        if (tangent)
        {
            primitive.Attributes.Add(new GltfAttribute
            {
                Semantic = "TANGENT",
                Accessor = CreateFloatingAccessor(
                    result,
                    GltfAccessorType.Vector4,
                    new double[]
                    {
                        1, 0, 0, 1,
                        1, 0, 0, 1,
                        1, 0, 0, 1
                    })
            });
        }

        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        result.Nodes.Add(new GltfNode { HasMesh = true, Mesh = 0 });
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        result.Scenes.Add(scene);
        result.HasDefaultScene = true;
        result.DefaultScene = 0;
        return result;
    }

    private static GltfDocument CreateOffsetGeometryDocument()
    {
        GltfDocument result = CreateGeometryDocument(false);
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        primitive.Attributes[0].Accessor = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[] { 2, 1, 0, 4, 1, 0, 2, 3, 0 });
        return result;
    }

    private static JsonArray MatrixJson(Matrix4x4 matrix)
    {
        double[] values = new CoordinateConverter().WriteColumnMajorMatrix(matrix);
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < values.Length)
        {
            result.Add(values[index]);
            index++;
        }

        return result;
    }

    private static Matrix4x4 ReadNodeMatrix(GltfNode node)
    {
        return new TransformMath().EvaluateLocalTransform(node, 0).LocalTransform;
    }

    private static int CreateFloatingAccessor(
        GltfDocument document,
        GltfAccessorType type,
        double[] values)
    {
        return new GltfAccessorCodec().CreateDenseAccessor(
            new GltfAccessorWriteRequest
            {
                Document = document,
                ComponentType = GltfComponentType.Float,
                Type = type,
                FloatingValues = values
            }).Accessor;
    }

    private static int CreateMatrixAccessor(GltfDocument document, double[] values)
    {
        return new GltfAccessorCodec().CreateDenseAccessor(
            new GltfAccessorWriteRequest
            {
                Document = document,
                ComponentType = GltfComponentType.Float,
                Type = GltfAccessorType.Matrix4,
                FloatingValues = values
            }).Accessor;
    }

    private static int CreateIndexAccessor(GltfDocument document, uint[] values)
    {
        int accessor = new GltfAccessorCodec().CreateDenseAccessor(
            new GltfAccessorWriteRequest
            {
                Document = document,
                ComponentType = GltfComponentType.UnsignedShort,
                Type = GltfAccessorType.Scalar,
                UnsignedValues = values
            }).Accessor;
        GltfBufferView view = document.BufferViews[document.Accessors[accessor].BufferView];
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ElementArrayBuffer;
        return accessor;
    }

    private static int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        return primitive.Attributes.Single(attribute => attribute.Semantic == semantic)
            .Accessor;
    }

    private static double[] ReadFloating(GltfDocument document, int accessor)
    {
        return new GltfAccessorCodec().ReadFloatingValues(
            new GltfAccessorReadRequest
            {
                Document = document,
                Accessor = accessor,
                ApplyNormalization = true
            }).FloatingValues;
    }

    private static double[] ReadMatrices(GltfDocument document, int accessor)
    {
        return new GltfAccessorCodec().ReadMatrices(
            new GltfAccessorReadRequest
            {
                Document = document,
                Accessor = accessor
            }).MatrixValues;
    }

    private static uint[] ReadIndices(GltfDocument document, int accessor)
    {
        return new GltfAccessorCodec().ReadIndices(
            new GltfAccessorReadRequest
            {
                Document = document,
                Accessor = accessor
            }).Indices;
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
}
