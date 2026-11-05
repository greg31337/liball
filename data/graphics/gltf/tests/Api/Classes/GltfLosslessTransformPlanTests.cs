using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfLosslessTransformPlanTests
{
    [Test]
    public void ObjectTransforms_ComposeInDeclaredOrderWithoutChangingSource()
    {
        GltfDocument source = CreateObjectTransformDocument();
        double[] originalPositions = ReadFloating(
            source,
            FindAttribute(source.Meshes[0].Primitives[0], "POSITION"));
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps =
            [
                CreateStep(GltfTransformKind.Prune),
                CreateStep(GltfTransformKind.Deduplicate),
                CreateStep(GltfTransformKind.Canonicalize),
                CreateStep(GltfTransformKind.Weld),
                CreateStep(GltfTransformKind.Unweld),
                CreateStep(GltfTransformKind.RemoveRedundantAnimationKeys)
            ]
        };

        GltfTransformReport report = Transform(source, plan);
        GltfDocument result = report.Document!;
        GltfMeshPrimitive primitive = result.Meshes[0].Primitives[0];
        GltfAnimationSampler sampler = result.Animations[0].Samplers[0];

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Steps.Select(item => item.Step!.Kind), Is.EqualTo(new[]
            {
                GltfTransformKind.Prune,
                GltfTransformKind.Deduplicate,
                GltfTransformKind.Canonicalize,
                GltfTransformKind.Weld,
                GltfTransformKind.Unweld,
                GltfTransformKind.RemoveRedundantAnimationKeys
            }));
            Assert.That(report.Steps.Select(item => item.Status),
                Is.All.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Steps.Select(item => item.ChangedObjects.Count),
                Is.All.GreaterThan(0));
            Assert.That(result.Materials, Is.Empty);
            Assert.That(result.Nodes.Select(node => node.Name),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(primitive.HasIndices, Is.False);
            Assert.That(ReadFloating(result, FindAttribute(primitive, "POSITION")),
                Is.EqualTo(originalPositions));
            Assert.That(ReadFloating(result, FindAttribute(primitive, "NORMAL")),
                Is.EqualTo(originalPositions));
            Assert.That(ReadFloating(result, sampler.Input),
                Is.EqualTo(new double[] { 0.0, 2.0 }));
            Assert.That(ReadFloating(result, sampler.Output),
                Is.EqualTo(new double[]
                {
                    0.0, 0.0, 0.0,
                    2.0, 0.0, 0.0
                }));
            Assert.That(source.Materials, Has.Count.EqualTo(1));
            Assert.That(source.Nodes.Select(node => node.Name),
                Is.EqualTo(new[] { "z", "a" }));
            Assert.That(source.Meshes[0].Primitives[0].HasIndices, Is.False);
            Assert.That(source.Accessors, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void StorageTransforms_RoundTripDataAcrossOneOrderedPlan()
    {
        GltfDocument source = new GltfDocument();
        int accessor = CreateUnsignedAccessor(source, new uint[] { 0, 7, 0, 9, 0 });
        AddBufferAndView(source, new byte[] { 10, 11, 12, 13 });
        GltfTransformStep partition = CreateStep(GltfTransformKind.PartitionBuffers);
        partition.Settings["maximumBufferByteLength"] = 12;
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps =
            [
                CreateStep(GltfTransformKind.DenseToSparse),
                CreateStep(GltfTransformKind.SparseToDense),
                CreateStep(GltfTransformKind.ConsolidateBuffers),
                partition,
                CreateStep(GltfTransformKind.PackBuffers),
                CreateStep(GltfTransformKind.EmbedResources),
                CreateStep(GltfTransformKind.ExtractResources)
            ]
        };

        GltfTransformReport report = Transform(source, plan);
        GltfDocument result = report.Document!;
        GltfBuffer resultBuffer = result.Buffers[0];

        Assert.Multiple(() =>
        {
            Assert.That(report.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Steps.Select(item => item.Step!.Kind), Is.EqualTo(new[]
            {
                GltfTransformKind.DenseToSparse,
                GltfTransformKind.SparseToDense,
                GltfTransformKind.ConsolidateBuffers,
                GltfTransformKind.PartitionBuffers,
                GltfTransformKind.PackBuffers,
                GltfTransformKind.EmbedResources,
                GltfTransformKind.ExtractResources
            }));
            Assert.That(report.Steps.Select(item => item.Status),
                Is.All.EqualTo(GltfOperationStatus.Success));
            Assert.That(report.Steps.Select(item =>
                item.ChangedObjects.Count + item.ChangedResources.Count),
                Is.All.GreaterThan(0));
            Assert.That(report.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "accessors/0" }));
            Assert.That(report.Steps[1].ChangedObjects,
                Is.EqualTo(new[] { "accessors/0" }));
            Assert.That(result.Accessors[accessor].Sparse, Is.Null);
            Assert.That(ReadUnsigned(result, accessor),
                Is.EqualTo(new uint[] { 0, 7, 0, 9, 0 }));
            Assert.That(result.Buffers, Has.Count.EqualTo(1));
            Assert.That(resultBuffer.HasResource, Is.True);
            Assert.That(resultBuffer.Uri, Is.EqualTo("buffer.bin"));
            Assert.That(result.Resources.Resources[resultBuffer.Resource].Embedded,
                Is.False);
            Assert.That(result.Resources.Resources[resultBuffer.Resource].Bytes,
                Is.EqualTo(resultBuffer.ResolvedBytes));
            Assert.That(source.Accessors[accessor].Sparse, Is.Null);
            Assert.That(source.Buffers, Has.Count.EqualTo(2));
            Assert.That(source.Buffers[0].HasResource, Is.False);
        });
    }

    private static GltfDocument CreateObjectTransformDocument()
    {
        GltfDocument result = new GltfDocument();
        int positions = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 0.0, 0.0
            });
        int normals = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                0.0, 0.0, 0.0
            });
        int times = CreateFloatingAccessor(
            result,
            GltfAccessorType.Scalar,
            new double[] { 0.0, 1.0, 2.0 });
        int values = CreateFloatingAccessor(
            result,
            GltfAccessorType.Vector3,
            new double[]
            {
                0.0, 0.0, 0.0,
                1.0, 0.0, 0.0,
                2.0, 0.0, 0.0
            });

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "POSITION",
            Accessor = positions
        });
        primitive.Attributes.Add(new GltfAttribute
        {
            Semantic = "NORMAL",
            Accessor = normals
        });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        result.Meshes.Add(mesh);
        result.Materials.Add(new GltfMaterial { Name = "unused" });

        result.Nodes.Add(new GltfNode
        {
            Name = "z",
            HasMesh = true,
            Mesh = 0
        });
        result.Nodes.Add(new GltfNode
        {
            Name = "a",
            HasMesh = true,
            Mesh = 0
        });
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        scene.Nodes.Add(1);
        result.Scenes.Add(scene);
        result.HasDefaultScene = true;
        result.DefaultScene = 0;

        GltfAnimationSampler sampler = new GltfAnimationSampler
        {
            Input = times,
            Output = values
        };
        GltfAnimationChannel channel = new GltfAnimationChannel { Sampler = 0 };
        channel.Target.HasNode = true;
        channel.Target.Node = 0;
        channel.Target.Path = GltfAnimationPath.Translation;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        animation.Channels.Add(channel);
        result.Animations.Add(animation);
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

    private static int CreateUnsignedAccessor(GltfDocument document, uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedShort,
            Type = GltfAccessorType.Scalar,
            UnsignedValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static int FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        return primitive.Attributes.Single(item => item.Semantic == semantic).Accessor;
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

    private static uint[] ReadUnsigned(GltfDocument document, int accessor)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest
        {
            Document = document,
            Accessor = accessor
        };
        return new GltfAccessorCodec().ReadUnsignedValues(request).UnsignedValues;
    }

    private static void AddBufferAndView(GltfDocument document, byte[] bytes)
    {
        int buffer = document.Buffers.Count;
        document.Buffers.Add(new GltfBuffer
        {
            ByteLength = bytes.Length,
            Resolved = true,
            ResolvedBytes = bytes
        });
        document.BufferViews.Add(new GltfBufferView
        {
            Buffer = buffer,
            ByteLength = bytes.Length
        });
    }

    private static GltfTransformStep CreateStep(GltfTransformKind kind)
    {
        return new GltfTransformStep { Kind = kind };
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformPlan plan)
    {
        return new GltfTransformer().TransformDocument(
            document,
            plan,
            new GltfTransformOptions
            {
                ValidateBefore = false,
                ValidateAfter = false
            });
    }
}
