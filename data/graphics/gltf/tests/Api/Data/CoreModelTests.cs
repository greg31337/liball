using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Data;

[TestFixture]
public class CoreModelTests
{
    [Test]
    public void Document_DefaultsToIndependentEmptyCoreCollections()
    {
        GltfDocument first = new GltfDocument();
        GltfDocument second = new GltfDocument();

        first.Nodes.Add(new GltfNode());
        first.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);

        Assert.Multiple(() =>
        {
            Assert.That(first.Asset.Version, Is.EqualTo("2.0"));
            Assert.That(first.HasDefaultScene, Is.False);
            Assert.That(first.DefaultScene, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(second.Accessors, Is.Empty);
            Assert.That(second.Animations, Is.Empty);
            Assert.That(second.Buffers, Is.Empty);
            Assert.That(second.BufferViews, Is.Empty);
            Assert.That(second.Cameras, Is.Empty);
            Assert.That(second.Images, Is.Empty);
            Assert.That(second.Materials, Is.Empty);
            Assert.That(second.Meshes, Is.Empty);
            Assert.That(second.Nodes, Is.Empty);
            Assert.That(second.Samplers, Is.Empty);
            Assert.That(second.Scenes, Is.Empty);
            Assert.That(second.Skins, Is.Empty);
            Assert.That(second.Textures, Is.Empty);
            Assert.That(second.ExtensionsUsed, Is.Empty);
            Assert.That(second.ExtensionsRequired, Is.Empty);
            Assert.That(first.Nodes, Is.Not.SameAs(second.Nodes));
            Assert.That(first.PropertyJson, Is.Not.SameAs(second.PropertyJson));
        });
    }

    [Test]
    public void PropertyJson_OwnsMutableExtensionExtrasAndUnknownContainers()
    {
        GltfPropertyJson first = new GltfPropertyJson();
        GltfPropertyJson second = new GltfPropertyJson();

        first.Extensions["VENDOR_test"] = new JsonObject { ["enabled"] = true };
        first.Extras = new JsonArray(1, 2, 3);
        first.UnknownMembers["future"] = JsonValue.Create("value");

        Assert.Multiple(() =>
        {
            Assert.That(first.Extensions, Has.Count.EqualTo(1));
            Assert.That(first.Extras, Is.TypeOf<JsonArray>());
            Assert.That(first.UnknownMembers, Has.Count.EqualTo(1));
            Assert.That(second.Extensions, Is.Empty);
            Assert.That(second.Extras, Is.Null);
            Assert.That(second.UnknownMembers, Is.Empty);
            Assert.That(first.Extensions, Is.Not.SameAs(second.Extensions));
            Assert.That(first.UnknownMembers, Is.Not.SameAs(second.UnknownMembers));
        });
    }

    [Test]
    public void Node_ExposesSchemaDefaultsWithoutClaimingPresence()
    {
        GltfNode node = new GltfNode();

        Assert.Multiple(() =>
        {
            Assert.That(node.HasCamera, Is.False);
            Assert.That(node.Camera, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(node.HasSkin, Is.False);
            Assert.That(node.Skin, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(node.HasMesh, Is.False);
            Assert.That(node.Mesh, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(node.HasMatrix, Is.False);
            Assert.That(node.Matrix, Is.EqualTo(new double[]
            {
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, 1.0
            }));
            Assert.That(node.HasRotation, Is.False);
            Assert.That(node.Rotation, Is.EqualTo(new[] { 0.0, 0.0, 0.0, 1.0 }));
            Assert.That(node.HasScale, Is.False);
            Assert.That(node.Scale, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(node.HasTranslation, Is.False);
            Assert.That(node.Translation, Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
            Assert.That(node.Weights, Is.Null);
            Assert.That(node.Children, Is.Empty);
        });
    }

    [Test]
    public void MeshAndAccessor_DefaultsMatchCoreSchema()
    {
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAccessor accessor = new GltfAccessor();
        GltfAccessorSparse sparse = new GltfAccessorSparse();

        Assert.Multiple(() =>
        {
            Assert.That(mesh.Primitives, Is.Empty);
            Assert.That(mesh.Weights, Is.Null);
            Assert.That(primitive.Attributes, Is.Empty);
            Assert.That(primitive.HasIndices, Is.False);
            Assert.That(primitive.Indices, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(primitive.HasMaterial, Is.False);
            Assert.That(primitive.Material, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(primitive.HasMode, Is.False);
            Assert.That(primitive.Mode, Is.EqualTo(GltfPrimitiveMode.Triangles));
            Assert.That(primitive.Targets, Is.Empty);
            Assert.That(accessor.HasBufferView, Is.False);
            Assert.That(accessor.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(accessor.HasByteOffset, Is.False);
            Assert.That(accessor.ByteOffset, Is.Zero);
            Assert.That(accessor.ComponentType, Is.EqualTo(GltfComponentType.Float));
            Assert.That(accessor.HasNormalized, Is.False);
            Assert.That(accessor.Normalized, Is.False);
            Assert.That(accessor.Type, Is.EqualTo(GltfAccessorType.Scalar));
            Assert.That(accessor.Maximum, Is.Null);
            Assert.That(accessor.Minimum, Is.Null);
            Assert.That(accessor.Sparse, Is.Null);
            Assert.That(sparse.Indices.ComponentType, Is.EqualTo(GltfComponentType.UnsignedShort));
            Assert.That(sparse.Indices.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(sparse.Values.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    [Test]
    public void BufferTextureAndSampler_DefaultsPreserveOptionalPresence()
    {
        GltfBuffer buffer = new GltfBuffer();
        GltfBufferView bufferView = new GltfBufferView();
        GltfTexture texture = new GltfTexture();
        GltfSampler sampler = new GltfSampler();
        GltfImage image = new GltfImage();

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Uri, Is.Null);
            Assert.That(buffer.ByteLength, Is.Zero);
            Assert.That(buffer.Resolved, Is.False);
            Assert.That(buffer.ResolvedBytes, Is.Empty);
            Assert.That(buffer.HasResource, Is.False);
            Assert.That(buffer.Resource, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(buffer.PendingResourceRequest, Is.Null);
            Assert.That(bufferView.Buffer, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(bufferView.HasByteOffset, Is.False);
            Assert.That(bufferView.ByteOffset, Is.Zero);
            Assert.That(bufferView.HasByteStride, Is.False);
            Assert.That(bufferView.HasTarget, Is.False);
            Assert.That(bufferView.Target, Is.EqualTo(GltfBufferViewTarget.None));
            Assert.That(texture.HasSampler, Is.False);
            Assert.That(texture.Sampler, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(texture.HasSource, Is.False);
            Assert.That(texture.Source, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(texture.HasSelectedSource, Is.False);
            Assert.That(texture.SelectedSource, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(texture.DecodedImage, Is.Null);
            Assert.That(sampler.HasMagnificationFilter, Is.False);
            Assert.That(sampler.HasMinificationFilter, Is.False);
            Assert.That(sampler.HasWrapS, Is.False);
            Assert.That(sampler.WrapS, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(sampler.HasWrapT, Is.False);
            Assert.That(sampler.WrapT, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(image.Uri, Is.Null);
            Assert.That(image.MimeType, Is.Null);
            Assert.That(image.HasBufferView, Is.False);
            Assert.That(image.BufferView, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(image.Resolved, Is.False);
            Assert.That(image.ResolvedBytes, Is.Empty);
            Assert.That(image.HasResource, Is.False);
            Assert.That(image.Resource, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(image.EffectiveUri, Is.Null);
            Assert.That(image.ResolvedMimeType, Is.Null);
            Assert.That(image.PendingResourceRequest, Is.Null);
        });
    }

    [Test]
    public void MaterialDefaults_MatchCoreSchema()
    {
        GltfMaterial material = new GltfMaterial();
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        GltfTextureInfo textureInfo = new GltfTextureInfo();
        GltfNormalTextureInfo normal = new GltfNormalTextureInfo();
        GltfOcclusionTextureInfo occlusion = new GltfOcclusionTextureInfo();

        Assert.Multiple(() =>
        {
            Assert.That(material.PbrMetallicRoughness, Is.Null);
            Assert.That(material.EmissiveFactor, Is.EqualTo(new[] { 0.0, 0.0, 0.0 }));
            Assert.That(material.HasEmissiveFactor, Is.False);
            Assert.That(material.AlphaMode, Is.EqualTo(GltfAlphaMode.Opaque));
            Assert.That(material.HasAlphaMode, Is.False);
            Assert.That(material.AlphaCutoff, Is.EqualTo(0.5));
            Assert.That(material.HasAlphaCutoff, Is.False);
            Assert.That(material.DoubleSided, Is.False);
            Assert.That(material.HasDoubleSided, Is.False);
            Assert.That(pbr.BaseColorFactor, Is.EqualTo(new[] { 1.0, 1.0, 1.0, 1.0 }));
            Assert.That(pbr.HasBaseColorFactor, Is.False);
            Assert.That(pbr.MetallicFactor, Is.EqualTo(1.0));
            Assert.That(pbr.HasMetallicFactor, Is.False);
            Assert.That(pbr.RoughnessFactor, Is.EqualTo(1.0));
            Assert.That(pbr.HasRoughnessFactor, Is.False);
            Assert.That(textureInfo.Index, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(textureInfo.TextureCoordinate, Is.Zero);
            Assert.That(textureInfo.HasTextureCoordinate, Is.False);
            Assert.That(normal.Scale, Is.EqualTo(1.0));
            Assert.That(normal.HasScale, Is.False);
            Assert.That(occlusion.Strength, Is.EqualTo(1.0));
            Assert.That(occlusion.HasStrength, Is.False);
        });
    }

    [Test]
    public void CameraSkinAndAnimation_DefaultsPreserveRequiredAndOptionalShape()
    {
        GltfCamera camera = new GltfCamera();
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera();
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        GltfSkin skin = new GltfSkin();
        GltfAnimation animation = new GltfAnimation();
        GltfAnimationSampler sampler = new GltfAnimationSampler();
        GltfAnimationChannel channel = new GltfAnimationChannel();

        Assert.Multiple(() =>
        {
            Assert.That(camera.Type, Is.EqualTo(GltfCameraType.Perspective));
            Assert.That(camera.Perspective, Is.Null);
            Assert.That(camera.Orthographic, Is.Null);
            Assert.That(perspective.HasAspectRatio, Is.False);
            Assert.That(perspective.HasFarPlane, Is.False);
            Assert.That(orthographic.NearPlane, Is.Zero);
            Assert.That(skin.HasInverseBindMatrices, Is.False);
            Assert.That(skin.InverseBindMatrices, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(skin.HasSkeleton, Is.False);
            Assert.That(skin.Skeleton, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(skin.Joints, Is.Empty);
            Assert.That(animation.Channels, Is.Empty);
            Assert.That(animation.Samplers, Is.Empty);
            Assert.That(sampler.Input, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(sampler.HasInterpolation, Is.False);
            Assert.That(sampler.Interpolation, Is.EqualTo(GltfAnimationInterpolation.Linear));
            Assert.That(sampler.Output, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(channel.Sampler, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(channel.Target.HasNode, Is.False);
            Assert.That(channel.Target.Node, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(channel.Target.Path, Is.EqualTo(GltfAnimationPath.Translation));
        });
    }

    [Test]
    public void CoreGraphFields_AreMutableAndRetainObjectIdentity()
    {
        GltfDocument document = new GltfDocument();
        GltfScene scene = new GltfScene();
        GltfNode node = new GltfNode();
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfMorphTarget target = new GltfMorphTarget();
        GltfAttribute attribute = new GltfAttribute();

        attribute.Semantic = GltfSemantics.Position;
        attribute.Accessor = 0;
        target.Attributes.Add(attribute);
        primitive.Attributes.Add(attribute);
        primitive.Targets.Add(target);
        mesh.Primitives.Add(primitive);
        node.HasMesh = true;
        node.Mesh = 0;
        scene.Nodes.Add(0);
        document.Accessors.Add(new GltfAccessor());
        document.Meshes.Add(mesh);
        document.Nodes.Add(node);
        document.Scenes.Add(scene);
        document.HasDefaultScene = true;
        document.DefaultScene = 0;

        Assert.Multiple(() =>
        {
            Assert.That(document.Meshes[0], Is.SameAs(mesh));
            Assert.That(document.Meshes[0].Primitives[0], Is.SameAs(primitive));
            Assert.That(primitive.Targets[0], Is.SameAs(target));
            Assert.That(primitive.Attributes[0], Is.SameAs(attribute));
            Assert.That(document.Nodes[0].Mesh, Is.Zero);
            Assert.That(document.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
            Assert.That(document.DefaultScene, Is.Zero);
        });
    }
}
