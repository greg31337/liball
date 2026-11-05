using System.Numerics;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class ValidationStatisticsCollectorTests
{
    [Test]
    public void PopulateValidationStatistics_CollectsDocumentGeometryAnimationResourcesAndBounds()
    {
        GltfDocument document = new GltfDocument();
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            -1.0, 2.0, 3.0,
            4.0, -2.0, 8.0,
            0.0, 1.0, 5.0);
        int times = CreateFloatingAccessor(
            document,
            GltfAccessorType.Scalar,
            0.0,
            2.0);
        GltfAccessor sparse = new GltfAccessor();
        sparse.Sparse = new GltfAccessorSparse();
        document.Accessors.Add(sparse);
        GltfBufferView interleaved = new GltfBufferView();
        interleaved.HasByteStride = true;
        interleaved.ByteStride = 16;
        document.BufferViews.Add(interleaved);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAttribute position = new GltfAttribute();
        position.Semantic = "POSITION";
        position.Accessor = positions;
        primitive.Attributes.Add(position);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfNode node = new GltfNode();
        node.HasMesh = true;
        node.Mesh = 0;
        document.Nodes.Add(node);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        sampler.Input = times;
        sampler.Output = positions;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        document.Animations.Add(animation);
        GltfBuffer unresolved = new GltfBuffer();
        unresolved.Uri = "unused.bin";
        unresolved.ByteLength = 5;
        document.Buffers.Add(unresolved);
        GltfImage image = new GltfImage();
        image.Resolved = true;
        image.ResolvedBytes = [1, 2, 3, 4];
        document.Images.Add(image);
        document.ExtensionsUsed.Add("EXT_zeta");
        node.PropertyJson.Extensions["EXT_alpha"] = new JsonObject();
        GltfValidationReport report = new GltfValidationReport();
        report.Issues.Add(new GltfValidationIssue());

        new ValidationStatisticsCollector().PopulateValidationStatistics(
            document,
            report);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Statistics.AccessorCount, Is.EqualTo(3));
            Assert.That(report.Statistics.AnimationCount, Is.EqualTo(1));
            Assert.That(report.Statistics.BufferCount, Is.EqualTo(3));
            Assert.That(report.Statistics.BufferViewCount, Is.EqualTo(3));
            Assert.That(report.Statistics.ImageCount, Is.EqualTo(1));
            Assert.That(report.Statistics.MeshCount, Is.EqualTo(1));
            Assert.That(report.Statistics.NodeCount, Is.EqualTo(1));
            Assert.That(report.Statistics.SceneCount, Is.EqualTo(1));
            Assert.That(report.Statistics.PrimitiveCount, Is.EqualTo(1));
            Assert.That(report.Statistics.VertexCount, Is.EqualTo(3));
            Assert.That(report.Statistics.TriangleCount, Is.EqualTo(1));
            Assert.That(report.Statistics.DrawCallCount, Is.EqualTo(1));
            Assert.That(report.Statistics.AnimationKeyframeCount, Is.EqualTo(2));
            Assert.That(report.Statistics.AnimationDuration, Is.EqualTo(2.0));
            Assert.That(report.Statistics.BufferByteLength, Is.EqualTo(49));
            Assert.That(report.Statistics.ImageByteLength, Is.EqualTo(4));
            Assert.That(report.Statistics.SparseAccessorCount, Is.EqualTo(1));
            Assert.That(report.Statistics.InterleavedBufferViewCount, Is.EqualTo(1));
            Assert.That(report.Statistics.UnresolvedResourceCount, Is.EqualTo(1));
            Assert.That(
                report.Statistics.Extensions,
                Is.EqualTo(new[] { "EXT_alpha", "EXT_zeta" }));
            Assert.That(report.Statistics.Bounds, Is.Not.Null);
            Assert.That(
                report.Statistics.Bounds!.Minimum,
                Is.EqualTo(new Vector3(-1.0f, -2.0f, 3.0f)));
            Assert.That(
                report.Statistics.Bounds.Maximum,
                Is.EqualTo(new Vector3(4.0f, 2.0f, 8.0f)));
        });
    }

    [Test]
    public void CollectDocumentStatistics_ReturnsIndependentStatistics()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add("EXT_example");
        ValidationStatisticsCollector collector = new ValidationStatisticsCollector();

        GltfDocumentStatistics first = collector.CollectDocumentStatistics(document);
        GltfDocumentStatistics second = collector.CollectDocumentStatistics(document);
        first.Extensions.Add("EXT_changed");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(second.Extensions, Is.EqualTo(new[] { "EXT_example" }));
            Assert.That(document.ExtensionsUsed, Is.EqualTo(new[] { "EXT_example" }));
        });
    }

    [Test]
    public void CollectDocumentStatistics_ReturnsPartialCountsForInvalidCollectionState()
    {
        GltfDocument document = new GltfDocument();
        document.Meshes.Add(new GltfMesh());
        document.Nodes.Add(new GltfNode());
        GltfBuffer unresolved = new GltfBuffer();
        unresolved.ByteLength = 12;
        document.Buffers.Add(unresolved);
        document.Images = null!;
        document.Scenes = null!;
        document.ExtensionsRequired.Add("EXT_required");

        GltfDocumentStatistics statistics = new ValidationStatisticsCollector()
            .CollectDocumentStatistics(document);

        Assert.Multiple(() =>
        {
            Assert.That(statistics.MeshCount, Is.EqualTo(1));
            Assert.That(statistics.NodeCount, Is.EqualTo(1));
            Assert.That(statistics.ImageCount, Is.Zero);
            Assert.That(statistics.SceneCount, Is.Zero);
            Assert.That(statistics.BufferByteLength, Is.EqualTo(12));
            Assert.That(statistics.UnresolvedResourceCount, Is.EqualTo(1));
            Assert.That(statistics.Extensions, Is.EqualTo(new[] { "EXT_required" }));
            Assert.That(statistics.Bounds, Is.Null);
        });
    }

    [Test]
    public void ValidationStatisticsCollector_RejectsNullArguments()
    {
        ValidationStatisticsCollector collector = new ValidationStatisticsCollector();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => collector.CollectDocumentStatistics(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => collector.PopulateValidationStatistics(
                    new GltfDocument(),
                    null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("report"));
        });
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
        GltfAccessorData result = new GltfAccessorCodec().CreateDenseAccessor(request);
        Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
        return result.Accessor;
    }
}
