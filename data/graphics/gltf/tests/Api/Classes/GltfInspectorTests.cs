using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfInspectorTests
{
    [Test]
    public void InspectDocument_CountsDefinitionsGeometryInstancesAndLayouts()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor positions = new GltfAccessor();
        positions.Count = 6;
        positions.Type = GltfAccessorType.Vector3;
        document.Accessors.Add(positions);
        GltfAccessor indices = new GltfAccessor();
        indices.Count = 6;
        indices.ComponentType = GltfComponentType.UnsignedShort;
        document.Accessors.Add(indices);
        GltfAccessor stripPositions = new GltfAccessor();
        stripPositions.Count = 4;
        stripPositions.Type = GltfAccessorType.Vector3;
        stripPositions.Sparse = new GltfAccessorSparse();
        document.Accessors.Add(stripPositions);
        GltfBufferView interleaved = new GltfBufferView();
        interleaved.HasByteStride = true;
        interleaved.ByteStride = 24;
        document.BufferViews.Add(interleaved);

        GltfMeshPrimitive triangles = CreatePrimitive(0, GltfPrimitiveMode.Triangles);
        triangles.HasIndices = true;
        triangles.Indices = 1;
        GltfMeshPrimitive strip = CreatePrimitive(2, GltfPrimitiveMode.TriangleStrip);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(triangles);
        mesh.Primitives.Add(strip);
        document.Meshes.Add(mesh);
        document.Nodes.Add(CreateMeshNode(0));
        document.Nodes.Add(CreateMeshNode(0));
        document.Materials.Add(new GltfMaterial());
        document.Cameras.Add(new GltfCamera());
        document.Samplers.Add(new GltfSampler());
        document.Scenes.Add(new GltfScene());
        document.Skins.Add(new GltfSkin());
        document.Textures.Add(new GltfTexture());

        GltfInspectionResult result = new GltfInspector().InspectDocument(
            document,
            CreateMetadataOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Statistics.AccessorCount, Is.EqualTo(3));
            Assert.That(result.Statistics.BufferViewCount, Is.EqualTo(1));
            Assert.That(result.Statistics.CameraCount, Is.EqualTo(1));
            Assert.That(result.Statistics.MaterialCount, Is.EqualTo(1));
            Assert.That(result.Statistics.MeshCount, Is.EqualTo(1));
            Assert.That(result.Statistics.NodeCount, Is.EqualTo(2));
            Assert.That(result.Statistics.SamplerCount, Is.EqualTo(1));
            Assert.That(result.Statistics.SceneCount, Is.EqualTo(1));
            Assert.That(result.Statistics.SkinCount, Is.EqualTo(1));
            Assert.That(result.Statistics.TextureCount, Is.EqualTo(1));
            Assert.That(result.Statistics.PrimitiveCount, Is.EqualTo(2));
            Assert.That(result.Statistics.VertexCount, Is.EqualTo(10));
            Assert.That(result.Statistics.TriangleCount, Is.EqualTo(4));
            Assert.That(result.Statistics.DrawCallCount, Is.EqualTo(4));
            Assert.That(result.Statistics.SparseAccessorCount, Is.EqualTo(1));
            Assert.That(result.Statistics.InterleavedBufferViewCount, Is.EqualTo(1));
            Assert.That(result.Statistics.Bounds, Is.Null);
        });
    }

    [Test]
    public void InspectDocument_ComputesExactAndMetadataAnimationStatistics()
    {
        GltfDocument exactDocument = new GltfDocument();
        int input = CreateFloatingAccessor(exactDocument, GltfAccessorType.Scalar, 0.0, 2.0, 5.0);
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        sampler.Input = input;
        sampler.Output = input;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(sampler);
        exactDocument.Animations.Add(animation);
        GltfInspectionOptions exactOptions = CreateMetadataOptions();
        exactOptions.DecodeAccessors = true;

        GltfInspectionResult exact = new GltfInspector().InspectDocument(
            exactDocument,
            exactOptions);

        GltfDocument metadataDocument = new GltfDocument();
        GltfAccessor metadataInput = new GltfAccessor();
        metadataInput.Count = 4;
        metadataInput.Maximum = [7.5];
        metadataDocument.Accessors.Add(metadataInput);
        GltfAnimationSampler metadataSampler = new GltfAnimationSampler();
        metadataSampler.Input = 0;
        GltfAnimation metadataAnimation = new GltfAnimation();
        metadataAnimation.Samplers.Add(metadataSampler);
        metadataDocument.Animations.Add(metadataAnimation);
        GltfInspectionResult metadata = new GltfInspector().InspectDocument(
            metadataDocument,
            CreateMetadataOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(exact.Statistics.AnimationCount, Is.EqualTo(1));
            Assert.That(exact.Statistics.AnimationKeyframeCount, Is.EqualTo(3));
            Assert.That(exact.Statistics.AnimationDuration, Is.EqualTo(5.0));
            Assert.That(metadata.Statistics.AnimationKeyframeCount, Is.EqualTo(4));
            Assert.That(metadata.Statistics.AnimationDuration, Is.EqualTo(7.5));
        });
    }

    [Test]
    public void EnumerateDependencies_ReturnsStableForwardAndReverseEdges()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(new GltfBuffer());
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        document.BufferViews.Add(view);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        document.Accessors.Add(accessor);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(CreatePrimitive(0, GltfPrimitiveMode.Points));
        document.Meshes.Add(mesh);
        document.Nodes.Add(CreateMeshNode(0));
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        document.HasDefaultScene = true;
        document.DefaultScene = 0;
        GltfInspectionOptions options = CreateMetadataOptions();
        options.IncludeDependencies = true;
        options.IncludeReverseDependencies = true;
        GltfInspector inspector = new GltfInspector();

        var forward = inspector.EnumerateDependencies(document, options);
        GltfInspectionResult inspection = inspector.InspectDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Select(edge => edge.SourceMember), Is.EqualTo(new[]
            {
                "scene", "bufferView", "buffer", "primitives/0/attributes/POSITION",
                "mesh", "nodes/0"
            }));
            Assert.That(inspection.Dependencies.Select(edge => edge.SourceMember),
                Is.EqualTo(forward.Select(edge => edge.SourceMember)));
            Assert.That(inspection.ReverseDependencies, Has.Count.EqualTo(forward.Count));
            Assert.That(inspection.ReverseDependencies[1].SourceCollection, Is.EqualTo("bufferViews"));
            Assert.That(inspection.ReverseDependencies[1].SourceIndex, Is.EqualTo(0));
            Assert.That(inspection.ReverseDependencies[1].TargetCollection, Is.EqualTo("accessors"));
            Assert.That(inspection.ReverseDependencies[1].TargetIndex, Is.EqualTo(0));
            Assert.That(inspection.ReverseDependencies[1].SourceMember, Is.EqualTo("bufferView"));
        });

        options.IncludeDependencies = false;
        Assert.That(inspector.EnumerateDependencies(document, options), Is.Empty);
        GltfInspectionResult reverseOnly = inspector.InspectDocument(document, options);
        Assert.Multiple(() =>
        {
            Assert.That(reverseOnly.Dependencies, Is.Empty);
            Assert.That(reverseOnly.ReverseDependencies, Has.Count.EqualTo(forward.Count));
        });
    }

    [Test]
    public void Inspector_ReturnsExactComprehensiveDependenciesAndStatistics()
    {
        GltfDocument document = CreateComprehensiveDocument();
        GltfInspectionOptions options = CreateMetadataOptions();
        options.IncludeDependencies = true;
        options.IncludeReverseDependencies = true;
        GltfInspector inspector = new GltfInspector();

        var dependencies = inspector.EnumerateDependencies(document, options);
        GltfInspectionResult result = inspector.InspectDocument(document, options);
        string[] expectedDependencies =
        {
            "document[-1].scene -> scenes[0]",
            "accessors[0].bufferView -> bufferViews[0]",
            "accessors[1].bufferView -> bufferViews[1]",
            "accessors[1].sparse/indices/bufferView -> bufferViews[1]",
            "accessors[1].sparse/values/bufferView -> bufferViews[2]",
            "animations[0].samplers/0/input -> accessors[2]",
            "animations[0].samplers/0/output -> accessors[3]",
            "animations[0].channels/0/sampler -> animations/0/samplers[0]",
            "animations[0].channels/0/target/node -> nodes[1]",
            "bufferViews[0].buffer -> buffers[0]",
            "bufferViews[1].buffer -> buffers[1]",
            "bufferViews[2].buffer -> buffers[0]",
            "buffers[0].resource -> resources[0]",
            "buffers[1].resource -> resources[1]",
            "images[0].bufferView -> bufferViews[2]",
            "images[0].resource -> resources[0]",
            "images[1].resource -> resources[1]",
            "meshes[0].primitives/0/indices -> accessors[1]",
            "meshes[0].primitives/0/material -> materials[0]",
            "meshes[0].primitives/0/attributes/POSITION -> accessors[0]",
            "meshes[0].primitives/0/attributes/NORMAL -> accessors[3]",
            "meshes[0].primitives/0/targets/0/POSITION -> accessors[3]",
            "nodes[0].children/0 -> nodes[1]",
            "nodes[0].mesh -> meshes[0]",
            "nodes[0].camera -> cameras[0]",
            "nodes[0].skin -> skins[0]",
            "scenes[0].nodes/0 -> nodes[0]",
            "skins[0].inverseBindMatrices -> accessors[3]",
            "skins[0].skeleton -> nodes[0]",
            "skins[0].joints/0 -> nodes[1]",
            "textures[0].sampler -> samplers[0]",
            "textures[0].source -> images[0]",
            "textures[0].selectedSource -> images[1]",
            "textures[0].extensions/KHR_texture_basisu/source -> images[1]"
        };

        Assert.Multiple(() =>
        {
            Assert.That(DescribeDependencies(dependencies), Is.EqualTo(expectedDependencies));
            Assert.That(DescribeDependencies(result.Dependencies), Is.EqualTo(expectedDependencies));
            Assert.That(DescribeReverseDependencies(result.ReverseDependencies),
                Is.EqualTo(expectedDependencies));
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Statistics.AccessorCount, Is.EqualTo(4));
            Assert.That(result.Statistics.AnimationCount, Is.EqualTo(1));
            Assert.That(result.Statistics.BufferCount, Is.EqualTo(2));
            Assert.That(result.Statistics.BufferViewCount, Is.EqualTo(3));
            Assert.That(result.Statistics.CameraCount, Is.EqualTo(1));
            Assert.That(result.Statistics.ImageCount, Is.EqualTo(2));
            Assert.That(result.Statistics.MaterialCount, Is.EqualTo(1));
            Assert.That(result.Statistics.MeshCount, Is.EqualTo(1));
            Assert.That(result.Statistics.NodeCount, Is.EqualTo(2));
            Assert.That(result.Statistics.SamplerCount, Is.EqualTo(1));
            Assert.That(result.Statistics.SceneCount, Is.EqualTo(1));
            Assert.That(result.Statistics.SkinCount, Is.EqualTo(1));
            Assert.That(result.Statistics.TextureCount, Is.EqualTo(1));
            Assert.That(result.Statistics.PrimitiveCount, Is.EqualTo(1));
            Assert.That(result.Statistics.VertexCount, Is.EqualTo(6));
            Assert.That(result.Statistics.TriangleCount, Is.EqualTo(2));
            Assert.That(result.Statistics.DrawCallCount, Is.EqualTo(1));
            Assert.That(result.Statistics.AnimationKeyframeCount, Is.EqualTo(3));
            Assert.That(result.Statistics.AnimationDuration, Is.EqualTo(4.0));
            Assert.That(result.Statistics.BufferByteLength, Is.EqualTo(32));
            Assert.That(result.Statistics.ImageByteLength, Is.EqualTo(4));
            Assert.That(result.Statistics.SparseAccessorCount, Is.EqualTo(1));
            Assert.That(result.Statistics.InterleavedBufferViewCount, Is.EqualTo(1));
            Assert.That(result.Statistics.UnresolvedResourceCount, Is.EqualTo(2));
            Assert.That(result.Statistics.Extensions,
                Is.EqualTo(new[] { "EXT_unrecognized", "KHR_texture_basisu" }));
            Assert.That(result.Statistics.Bounds, Is.Null);
        });
    }

    [Test]
    public void InspectDocument_CalculatesWorldBoundsWithoutMutatingTheDocument()
    {
        GltfDocument document = new GltfDocument();
        int positions = CreateFloatingAccessor(
            document,
            GltfAccessorType.Vector3,
            -1.0, 2.0, 3.0,
            4.0, -2.0, 8.0);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(CreatePrimitive(positions, GltfPrimitiveMode.Points));
        document.Meshes.Add(mesh);
        GltfNode node = CreateMeshNode(0);
        node.HasTranslation = true;
        node.Translation = [10.0, 1.0, -3.0];
        document.Nodes.Add(node);
        int accessorCount = document.Accessors.Count;
        int bufferViewCount = document.BufferViews.Count;
        int bufferCount = document.Buffers.Count;
        byte[] originalBytes = (byte[])document.Buffers[0].ResolvedBytes.Clone();
        double[] originalTranslation = (double[])node.Translation.Clone();
        GltfInspectionOptions options = CreateMetadataOptions();
        options.CalculateBounds = true;

        GltfInspectionResult result = new GltfInspector().InspectDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Statistics.Bounds, Is.Not.Null);
            Assert.That(result.Statistics.Bounds!.Minimum, Is.EqualTo(new Vector3(9.0f, -1.0f, 0.0f)));
            Assert.That(result.Statistics.Bounds.Maximum, Is.EqualTo(new Vector3(14.0f, 3.0f, 5.0f)));
            Assert.That(document.Accessors, Has.Count.EqualTo(accessorCount));
            Assert.That(document.BufferViews, Has.Count.EqualTo(bufferViewCount));
            Assert.That(document.Buffers, Has.Count.EqualTo(bufferCount));
            Assert.That(document.Buffers[0].ResolvedBytes, Is.EqualTo(originalBytes));
            Assert.That(node.Translation, Is.EqualTo(originalTranslation));
        });
    }

    [Test]
    public void InspectDocument_ReportsResourceBytesUnresolvedResourcesAndEncounteredExtensions()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer resolvedBuffer = new GltfBuffer();
        resolvedBuffer.ByteLength = 10;
        resolvedBuffer.Resolved = true;
        resolvedBuffer.ResolvedBytes = new byte[10];
        document.Buffers.Add(resolvedBuffer);
        GltfBuffer unresolvedBuffer = new GltfBuffer();
        unresolvedBuffer.ByteLength = 20;
        document.Buffers.Add(unresolvedBuffer);
        GltfImage resolvedImage = new GltfImage();
        resolvedImage.Resolved = true;
        resolvedImage.ResolvedBytes = new byte[] { 1, 2, 3, 4 };
        document.Images.Add(resolvedImage);
        document.Images.Add(new GltfImage());
        document.ExtensionsUsed.Add("EXT_zeta");
        document.ExtensionsRequired.Add("EXT_alpha");
        GltfNode node = new GltfNode();
        JsonObject payload = new JsonObject { ["enabled"] = true };
        node.PropertyJson.Extensions.Add("EXT_middle", payload);
        document.Nodes.Add(node);

        GltfInspectionResult result = new GltfInspector().InspectDocument(
            document,
            CreateMetadataOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Statistics.BufferCount, Is.EqualTo(2));
            Assert.That(result.Statistics.ImageCount, Is.EqualTo(2));
            Assert.That(result.Statistics.BufferByteLength, Is.EqualTo(30));
            Assert.That(result.Statistics.ImageByteLength, Is.EqualTo(4));
            Assert.That(result.Statistics.UnresolvedResourceCount, Is.EqualTo(2));
            Assert.That(result.Statistics.Extensions,
                Is.EqualTo(new[] { "EXT_alpha", "EXT_middle", "EXT_zeta" }));
            Assert.That(node.PropertyJson.Extensions["EXT_middle"], Is.SameAs(payload));
        });
    }

    [Test]
    public void Inspector_RejectsNullInputs()
    {
        GltfInspector inspector = new GltfInspector();
        GltfInspectionOptions options = new GltfInspectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspector.InspectDocument(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(() => inspector.InspectDocument(new GltfDocument(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => inspector.EnumerateDependencies(null!, options),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
            Assert.That(() => inspector.EnumerateDependencies(new GltfDocument(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
        });
    }

    private static GltfInspectionOptions CreateMetadataOptions()
    {
        GltfInspectionOptions options = new GltfInspectionOptions();
        options.IncludeDependencies = false;
        options.IncludeReverseDependencies = false;
        options.CalculateBounds = false;
        options.DecodeAccessors = false;
        return options;
    }

    private static GltfDocument CreateComprehensiveDocument()
    {
        GltfDocument document = new GltfDocument();
        document.Resources.Resources.Add(new GltfResource());
        document.Resources.Resources.Add(new GltfResource());
        document.ExtensionsUsed.Add("EXT_unrecognized");

        GltfBuffer resolvedBuffer = new GltfBuffer();
        resolvedBuffer.ByteLength = 12;
        resolvedBuffer.Resolved = true;
        resolvedBuffer.ResolvedBytes = new byte[12];
        resolvedBuffer.HasResource = true;
        resolvedBuffer.Resource = 0;
        document.Buffers.Add(resolvedBuffer);
        GltfBuffer unresolvedBuffer = new GltfBuffer();
        unresolvedBuffer.ByteLength = 20;
        unresolvedBuffer.HasResource = true;
        unresolvedBuffer.Resource = 1;
        document.Buffers.Add(unresolvedBuffer);

        GltfBufferView interleaved = new GltfBufferView();
        interleaved.Buffer = 0;
        interleaved.HasByteStride = true;
        interleaved.ByteStride = 24;
        document.BufferViews.Add(interleaved);
        GltfBufferView sparseIndices = new GltfBufferView();
        sparseIndices.Buffer = 1;
        document.BufferViews.Add(sparseIndices);
        GltfBufferView sparseValues = new GltfBufferView();
        sparseValues.Buffer = 0;
        document.BufferViews.Add(sparseValues);

        GltfAccessor positions = new GltfAccessor();
        positions.HasBufferView = true;
        positions.BufferView = 0;
        positions.Count = 6;
        positions.Type = GltfAccessorType.Vector3;
        document.Accessors.Add(positions);
        GltfAccessor indices = new GltfAccessor();
        indices.HasBufferView = true;
        indices.BufferView = 1;
        indices.Count = 6;
        indices.ComponentType = GltfComponentType.UnsignedShort;
        indices.Sparse = new GltfAccessorSparse();
        indices.Sparse.Count = 1;
        indices.Sparse.Indices.BufferView = 1;
        indices.Sparse.Values.BufferView = 2;
        document.Accessors.Add(indices);
        GltfAccessor times = new GltfAccessor();
        times.Count = 3;
        times.Maximum = [4.0];
        document.Accessors.Add(times);
        GltfAccessor values = new GltfAccessor();
        values.Count = 3;
        values.Type = GltfAccessorType.Vector3;
        document.Accessors.Add(values);

        GltfAnimationSampler animationSampler = new GltfAnimationSampler();
        animationSampler.Input = 2;
        animationSampler.Output = 3;
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Sampler = 0;
        channel.Target.HasNode = true;
        channel.Target.Node = 1;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(animationSampler);
        animation.Channels.Add(channel);
        document.Animations.Add(animation);

        GltfMeshPrimitive primitive = CreatePrimitive(0, GltfPrimitiveMode.Triangles);
        primitive.HasIndices = true;
        primitive.Indices = 1;
        primitive.HasMaterial = true;
        primitive.Material = 0;
        GltfAttribute normal = new GltfAttribute();
        normal.Semantic = "NORMAL";
        normal.Accessor = 3;
        primitive.Attributes.Add(normal);
        GltfMorphTarget target = new GltfMorphTarget();
        GltfAttribute targetPosition = new GltfAttribute();
        targetPosition.Semantic = "POSITION";
        targetPosition.Accessor = 3;
        target.Attributes.Add(targetPosition);
        primitive.Targets.Add(target);
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        document.Materials.Add(new GltfMaterial());
        document.Cameras.Add(new GltfCamera());

        GltfNode parent = CreateMeshNode(0);
        parent.HasCamera = true;
        parent.Camera = 0;
        parent.HasSkin = true;
        parent.Skin = 0;
        parent.Children.Add(1);
        document.Nodes.Add(parent);
        document.Nodes.Add(new GltfNode());
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        document.HasDefaultScene = true;
        document.DefaultScene = 0;

        GltfSkin skin = new GltfSkin();
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 3;
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        skin.Joints.Add(1);
        document.Skins.Add(skin);

        GltfImage resolvedImage = new GltfImage();
        resolvedImage.HasBufferView = true;
        resolvedImage.BufferView = 2;
        resolvedImage.HasResource = true;
        resolvedImage.Resource = 0;
        resolvedImage.Resolved = true;
        resolvedImage.ResolvedBytes = [1, 2, 3, 4];
        document.Images.Add(resolvedImage);
        GltfImage unresolvedImage = new GltfImage();
        unresolvedImage.HasResource = true;
        unresolvedImage.Resource = 1;
        document.Images.Add(unresolvedImage);
        document.Samplers.Add(new GltfSampler());
        GltfTexture texture = new GltfTexture();
        texture.HasSampler = true;
        texture.Sampler = 0;
        texture.HasSource = true;
        texture.Source = 0;
        texture.HasSelectedSource = true;
        texture.SelectedSource = 1;
        GltfTypedExtensionData typedExtension = new GltfTypedExtensionData();
        typedExtension.TextureBasisu = new GltfTextureBasisuExtension();
        typedExtension.TextureBasisu.HasSource = true;
        typedExtension.TextureBasisu.Source = 1;
        texture.PropertyJson.TypedExtensions[GltfExtensionNames.KhrTextureBasisu]
            = typedExtension;
        document.Textures.Add(texture);
        return document;
    }

    private static string[] DescribeDependencies(
        System.Collections.Generic.List<GltfDependency> dependencies)
    {
        return dependencies.Select(edge =>
            $"{edge.SourceCollection}[{edge.SourceIndex}].{edge.SourceMember} -> "
            + $"{edge.TargetCollection}[{edge.TargetIndex}]").ToArray();
    }

    private static string[] DescribeReverseDependencies(
        System.Collections.Generic.List<GltfDependency> dependencies)
    {
        return dependencies.Select(edge =>
            $"{edge.TargetCollection}[{edge.TargetIndex}].{edge.SourceMember} -> "
            + $"{edge.SourceCollection}[{edge.SourceIndex}]").ToArray();
    }

    private static GltfMeshPrimitive CreatePrimitive(
        int positionAccessor,
        GltfPrimitiveMode mode)
    {
        GltfAttribute position = new GltfAttribute();
        position.Semantic = "POSITION";
        position.Accessor = positionAccessor;
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Attributes.Add(position);
        primitive.Mode = mode;
        return primitive;
    }

    private static GltfNode CreateMeshNode(int mesh)
    {
        GltfNode node = new GltfNode();
        node.HasMesh = true;
        node.Mesh = mesh;
        return node;
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
