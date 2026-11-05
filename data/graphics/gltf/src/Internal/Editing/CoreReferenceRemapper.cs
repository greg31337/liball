using System.Collections.Generic;

namespace gltf;

internal class CoreReferenceRemapper
{
    internal void RemapDocumentReferences(GltfDocument document, GltfIndexRemap remap)
    {
        if (document.HasDefaultScene)
        {
            document.DefaultScene = RemapIndex(document.DefaultScene, remap.Scenes);
        }

        int index = 0;
        while (index < document.Accessors.Count)
        {
            RemapAccessorReferences(document.Accessors[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Animations.Count)
        {
            RemapAnimationReferences(document.Animations[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[index];
            if (buffer.HasResource)
            {
                buffer.Resource = RemapIndex(buffer.Resource, remap.Resources);
            }

            index++;
        }

        index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[index];
            view.Buffer = RemapIndex(view.Buffer, remap.Buffers);
            index++;
        }

        index = 0;
        while (index < document.Images.Count)
        {
            GltfImage image = document.Images[index];
            if (image.HasBufferView)
            {
                image.BufferView = RemapIndex(image.BufferView, remap.BufferViews);
            }

            if (image.HasResource)
            {
                image.Resource = RemapIndex(image.Resource, remap.Resources);
            }

            index++;
        }

        index = 0;
        while (index < document.Materials.Count)
        {
            RemapMaterialReferences(document.Materials[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Meshes.Count)
        {
            RemapMeshReferences(document.Meshes[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Nodes.Count)
        {
            RemapNodeReferences(document.Nodes[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Scenes.Count)
        {
            RemapIntegerList(document.Scenes[index].Nodes, remap.Nodes);
            index++;
        }

        index = 0;
        while (index < document.Skins.Count)
        {
            RemapSkinReferences(document.Skins[index], remap);
            index++;
        }

        index = 0;
        while (index < document.Textures.Count)
        {
            RemapTextureReferences(document.Textures[index], remap);
            index++;
        }
    }

    internal void RemapAccessorReferences(GltfAccessor accessor, GltfIndexRemap remap)
    {
        if (accessor.HasBufferView)
        {
            accessor.BufferView = RemapIndex(accessor.BufferView, remap.BufferViews);
        }

        if (accessor.Sparse != null)
        {
            accessor.Sparse.Indices.BufferView = RemapIndex(
                accessor.Sparse.Indices.BufferView,
                remap.BufferViews);
            accessor.Sparse.Values.BufferView = RemapIndex(
                accessor.Sparse.Values.BufferView,
                remap.BufferViews);
        }
    }

    internal void RemapAnimationReferences(GltfAnimation animation, GltfIndexRemap remap)
    {
        int index = 0;
        while (index < animation.Samplers.Count)
        {
            GltfAnimationSampler sampler = animation.Samplers[index];
            sampler.Input = RemapIndex(sampler.Input, remap.Accessors);
            sampler.Output = RemapIndex(sampler.Output, remap.Accessors);
            index++;
        }

        index = 0;
        while (index < animation.Channels.Count)
        {
            GltfAnimationTarget target = animation.Channels[index].Target;
            if (target.HasNode)
            {
                target.Node = RemapIndex(target.Node, remap.Nodes);
            }

            index++;
        }
    }

    internal void RemapMaterialReferences(GltfMaterial material, GltfIndexRemap remap)
    {
        if (material.PbrMetallicRoughness != null)
        {
            RemapTextureInfoReference(
                material.PbrMetallicRoughness.BaseColorTexture,
                remap);
            RemapTextureInfoReference(
                material.PbrMetallicRoughness.MetallicRoughnessTexture,
                remap);
        }

        RemapNormalTextureReference(material.NormalTexture, remap);
        RemapOcclusionTextureReference(material.OcclusionTexture, remap);
        RemapTextureInfoReference(material.EmissiveTexture, remap);
    }

    internal void RemapTextureInfoReference(GltfTextureInfo? texture, GltfIndexRemap remap)
    {
        if (texture != null)
        {
            texture.Index = RemapIndex(texture.Index, remap.Textures);
        }
    }

    internal void RemapNormalTextureReference(
        GltfNormalTextureInfo? texture,
        GltfIndexRemap remap)
    {
        if (texture != null)
        {
            texture.Index = RemapIndex(texture.Index, remap.Textures);
        }
    }

    internal void RemapOcclusionTextureReference(
        GltfOcclusionTextureInfo? texture,
        GltfIndexRemap remap)
    {
        if (texture != null)
        {
            texture.Index = RemapIndex(texture.Index, remap.Textures);
        }
    }

    internal void RemapMeshReferences(GltfMesh mesh, GltfIndexRemap remap)
    {
        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            RemapAttributeReferences(primitive.Attributes, remap);
            if (primitive.HasIndices)
            {
                primitive.Indices = RemapIndex(primitive.Indices, remap.Accessors);
            }

            if (primitive.HasMaterial)
            {
                primitive.Material = RemapIndex(primitive.Material, remap.Materials);
            }

            int targetIndex = 0;
            while (targetIndex < primitive.Targets.Count)
            {
                RemapAttributeReferences(primitive.Targets[targetIndex].Attributes, remap);
                targetIndex++;
            }

            primitiveIndex++;
        }
    }

    internal void RemapAttributeReferences(
        List<GltfAttribute> attributes,
        GltfIndexRemap remap)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            attributes[index].Accessor = RemapIndex(
                attributes[index].Accessor,
                remap.Accessors);
            index++;
        }
    }

    internal void RemapNodeReferences(GltfNode node, GltfIndexRemap remap)
    {
        RemapIntegerList(node.Children, remap.Nodes);
        if (node.HasCamera)
        {
            node.Camera = RemapIndex(node.Camera, remap.Cameras);
        }

        if (node.HasSkin)
        {
            node.Skin = RemapIndex(node.Skin, remap.Skins);
        }

        if (node.HasMesh)
        {
            node.Mesh = RemapIndex(node.Mesh, remap.Meshes);
        }
    }

    internal void RemapSkinReferences(GltfSkin skin, GltfIndexRemap remap)
    {
        if (skin.HasInverseBindMatrices)
        {
            skin.InverseBindMatrices = RemapIndex(
                skin.InverseBindMatrices,
                remap.Accessors);
        }

        if (skin.HasSkeleton)
        {
            skin.Skeleton = RemapIndex(skin.Skeleton, remap.Nodes);
        }

        RemapIntegerList(skin.Joints, remap.Nodes);
    }

    internal void RemapTextureReferences(GltfTexture texture, GltfIndexRemap remap)
    {
        if (texture.HasSampler)
        {
            texture.Sampler = RemapIndex(texture.Sampler, remap.Samplers);
        }

        if (texture.HasSource)
        {
            texture.Source = RemapIndex(texture.Source, remap.Images);
        }

        if (texture.HasSelectedSource)
        {
            texture.SelectedSource = RemapIndex(texture.SelectedSource, remap.Images);
        }
    }

    internal void RemapIntegerList(List<int> values, Dictionary<int, int> remap)
    {
        int index = 0;
        while (index < values.Count)
        {
            values[index] = RemapIndex(values[index], remap);
            index++;
        }
    }

    internal int RemapIndex(int value, Dictionary<int, int> remap)
    {
        if (remap.ContainsKey(value))
        {
            return remap[value];
        }

        return value;
    }
}
