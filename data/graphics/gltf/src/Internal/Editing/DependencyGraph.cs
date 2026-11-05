using System;
using System.Collections.Generic;

namespace gltf;

internal class DependencyGraph
{
    internal ExtensionDependencyEnumerator ExtensionDependencies =
        new ExtensionDependencyEnumerator();

    internal List<GltfDependency> EnumerateDocumentDependencies(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<GltfDependency> result = EnumerateCoreDependencies(document);
        List<GltfDependency> extensionDependencies =
            ExtensionDependencies.EnumerateExtensionDependencies(document);
        int extensionIndex = 0;
        while (extensionIndex < extensionDependencies.Count)
        {
            result.Add(extensionDependencies[extensionIndex]);
            extensionIndex++;
        }

        return result;
    }

    internal List<GltfDependency> EnumerateCoreDependencies(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<GltfDependency> result = new List<GltfDependency>();
        AddRootDependencies(document, result);
        AddAccessorDependencies(document, result);
        AddAnimationDependencies(document, result);
        AddBufferViewDependencies(document, result);
        AddBufferDependencies(document, result);
        AddImageDependencies(document, result);
        AddMaterialDependencies(document, result);
        AddMeshDependencies(document, result);
        AddNodeDependencies(document, result);
        AddSceneDependencies(document, result);
        AddSkinDependencies(document, result);
        AddTextureDependencies(document, result);
        return result;
    }

    internal List<GltfDependency> ReverseDocumentDependencies(
        List<GltfDependency> dependencies)
    {
        if (dependencies == null)
        {
            throw new ArgumentNullException("dependencies");
        }

        List<GltfDependency> result = new List<GltfDependency>();
        int dependencyIndex = 0;
        while (dependencyIndex < dependencies.Count)
        {
            GltfDependency dependency = dependencies[dependencyIndex];
            GltfDependency reverse = new GltfDependency();
            reverse.SourceCollection = dependency.TargetCollection;
            reverse.SourceIndex = dependency.TargetIndex;
            reverse.SourceMember = dependency.SourceMember;
            reverse.TargetCollection = dependency.SourceCollection;
            reverse.TargetIndex = dependency.SourceIndex;
            result.Add(reverse);
            dependencyIndex++;
        }

        return result;
    }

    internal void AddRootDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.HasDefaultScene)
        {
            AddDependency(result, "document", -1, "scene", "scenes", document.DefaultScene);
        }
    }

    internal void AddAccessorDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Accessors == null)
        {
            return;
        }

        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor != null)
            {
                if (accessor.HasBufferView)
                {
                    AddDependency(
                        result,
                        "accessors",
                        accessorIndex,
                        "bufferView",
                        "bufferViews",
                        accessor.BufferView);
                }

                if (accessor.Sparse != null)
                {
                    AddDependency(
                        result,
                        "accessors",
                        accessorIndex,
                        "sparse/indices/bufferView",
                        "bufferViews",
                        accessor.Sparse.Indices.BufferView);
                    AddDependency(
                        result,
                        "accessors",
                        accessorIndex,
                        "sparse/values/bufferView",
                        "bufferViews",
                        accessor.Sparse.Values.BufferView);
                }
            }

            accessorIndex++;
        }
    }

    internal void AddAnimationDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Animations == null)
        {
            return;
        }

        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            if (animation != null)
            {
                int samplerIndex = 0;
                while (animation.Samplers != null
                    && samplerIndex < animation.Samplers.Count)
                {
                    GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
                    if (sampler != null)
                    {
                        AddDependency(
                            result,
                            "animations",
                            animationIndex,
                            $"samplers/{samplerIndex}/input",
                            "accessors",
                            sampler.Input);
                        AddDependency(
                            result,
                            "animations",
                            animationIndex,
                            $"samplers/{samplerIndex}/output",
                            "accessors",
                            sampler.Output);
                    }

                    samplerIndex++;
                }

                int channelIndex = 0;
                while (animation.Channels != null
                    && channelIndex < animation.Channels.Count)
                {
                    GltfAnimationChannel channel = animation.Channels[channelIndex];
                    if (channel != null)
                    {
                        AddDependency(
                            result,
                            "animations",
                            animationIndex,
                            $"channels/{channelIndex}/sampler",
                            $"animations/{animationIndex}/samplers",
                            channel.Sampler);
                        if (channel.Target != null && channel.Target.HasNode)
                        {
                            AddDependency(
                                result,
                                "animations",
                                animationIndex,
                                $"channels/{channelIndex}/target/node",
                                "nodes",
                                channel.Target.Node);
                        }
                    }

                    channelIndex++;
                }
            }

            animationIndex++;
        }
    }

    internal void AddBufferViewDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.BufferViews == null)
        {
            return;
        }

        int viewIndex = 0;
        while (viewIndex < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[viewIndex];
            if (view != null)
            {
                AddDependency(
                    result,
                    "bufferViews",
                    viewIndex,
                    "buffer",
                    "buffers",
                    view.Buffer);
            }

            viewIndex++;
        }
    }

    internal void AddBufferDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Buffers == null)
        {
            return;
        }

        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer != null && buffer.HasResource)
            {
                AddDependency(
                    result,
                    "buffers",
                    bufferIndex,
                    "resource",
                    "resources",
                    buffer.Resource);
            }

            bufferIndex++;
        }
    }

    internal void AddImageDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Images == null)
        {
            return;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image != null)
            {
                if (image.HasBufferView)
                {
                    AddDependency(
                        result,
                        "images",
                        imageIndex,
                        "bufferView",
                        "bufferViews",
                        image.BufferView);
                }

                if (image.HasResource)
                {
                    AddDependency(
                        result,
                        "images",
                        imageIndex,
                        "resource",
                        "resources",
                        image.Resource);
                }
            }

            imageIndex++;
        }
    }

    internal void AddMaterialDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Materials == null)
        {
            return;
        }

        int materialIndex = 0;
        while (materialIndex < document.Materials.Count)
        {
            GltfMaterial material = document.Materials[materialIndex];
            if (material != null)
            {
                if (material.PbrMetallicRoughness != null)
                {
                    AddTextureInfoEdge(
                        result,
                        materialIndex,
                        "pbrMetallicRoughness/baseColorTexture",
                        material.PbrMetallicRoughness.BaseColorTexture);
                    AddTextureInfoEdge(
                        result,
                        materialIndex,
                        "pbrMetallicRoughness/metallicRoughnessTexture",
                        material.PbrMetallicRoughness.MetallicRoughnessTexture);
                }

                AddNormalTextureEdge(
                    result,
                    materialIndex,
                    "normalTexture",
                    material.NormalTexture);
                AddOcclusionTextureEdge(
                    result,
                    materialIndex,
                    "occlusionTexture",
                    material.OcclusionTexture);
                AddTextureInfoEdge(
                    result,
                    materialIndex,
                    "emissiveTexture",
                    material.EmissiveTexture);
            }

            materialIndex++;
        }
    }

    internal void AddTextureInfoEdge(
        List<GltfDependency> result,
        int materialIndex,
        string member,
        GltfTextureInfo? texture)
    {
        if (texture != null)
        {
            AddDependency(result, "materials", materialIndex, member, "textures", texture.Index);
        }
    }

    internal void AddNormalTextureEdge(
        List<GltfDependency> result,
        int materialIndex,
        string member,
        GltfNormalTextureInfo? texture)
    {
        if (texture != null)
        {
            AddDependency(result, "materials", materialIndex, member, "textures", texture.Index);
        }
    }

    internal void AddOcclusionTextureEdge(
        List<GltfDependency> result,
        int materialIndex,
        string member,
        GltfOcclusionTextureInfo? texture)
    {
        if (texture != null)
        {
            AddDependency(result, "materials", materialIndex, member, "textures", texture.Index);
        }
    }

    internal void AddMeshDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Meshes == null)
        {
            return;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (mesh != null
                && mesh.Primitives != null
                && primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                if (primitive != null)
                {
                    if (primitive.HasIndices)
                    {
                        AddDependency(
                            result,
                            "meshes",
                            meshIndex,
                            $"primitives/{primitiveIndex}/indices",
                            "accessors",
                            primitive.Indices);
                    }

                    if (primitive.HasMaterial)
                    {
                        AddDependency(
                            result,
                            "meshes",
                            meshIndex,
                            $"primitives/{primitiveIndex}/material",
                            "materials",
                            primitive.Material);
                    }

                    AddAttributeDependencies(
                        result,
                        meshIndex,
                        primitiveIndex,
                        primitive.Attributes,
                        "attributes");
                    int targetIndex = 0;
                    while (primitive.Targets != null
                        && targetIndex < primitive.Targets.Count)
                    {
                        GltfMorphTarget target = primitive.Targets[targetIndex];
                        if (target != null)
                        {
                            AddAttributeDependencies(
                                result,
                                meshIndex,
                                primitiveIndex,
                                target.Attributes,
                                $"targets/{targetIndex}");
                        }

                        targetIndex++;
                    }
                }

                primitiveIndex++;
            }

            meshIndex++;
        }
    }

    internal void AddAttributeDependencies(
        List<GltfDependency> result,
        int meshIndex,
        int primitiveIndex,
        List<GltfAttribute> attributes,
        string member)
    {
        if (attributes == null)
        {
            return;
        }

        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            GltfAttribute attribute = attributes[attributeIndex];
            if (attribute != null)
            {
                AddDependency(
                    result,
                    "meshes",
                    meshIndex,
                    $"primitives/{primitiveIndex}/{member}/{attribute.Semantic}",
                    "accessors",
                    attribute.Accessor);
            }

            attributeIndex++;
        }
    }

    internal void AddNodeDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Nodes == null)
        {
            return;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node != null)
            {
                int childPosition = 0;
                while (node.Children != null && childPosition < node.Children.Count)
                {
                    AddDependency(
                        result,
                        "nodes",
                        nodeIndex,
                        $"children/{childPosition}",
                        "nodes",
                        node.Children[childPosition]);
                    childPosition++;
                }

                if (node.HasMesh)
                {
                    AddDependency(result, "nodes", nodeIndex, "mesh", "meshes", node.Mesh);
                }

                if (node.HasCamera)
                {
                    AddDependency(result, "nodes", nodeIndex, "camera", "cameras", node.Camera);
                }

                if (node.HasSkin)
                {
                    AddDependency(result, "nodes", nodeIndex, "skin", "skins", node.Skin);
                }
            }

            nodeIndex++;
        }
    }

    internal void AddSceneDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Scenes == null)
        {
            return;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfScene scene = document.Scenes[sceneIndex];
            int rootPosition = 0;
            while (scene != null && scene.Nodes != null && rootPosition < scene.Nodes.Count)
            {
                AddDependency(
                    result,
                    "scenes",
                    sceneIndex,
                    $"nodes/{rootPosition}",
                    "nodes",
                    scene.Nodes[rootPosition]);
                rootPosition++;
            }

            sceneIndex++;
        }
    }

    internal void AddSkinDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Skins == null)
        {
            return;
        }

        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            if (skin != null)
            {
                if (skin.HasInverseBindMatrices)
                {
                    AddDependency(
                        result,
                        "skins",
                        skinIndex,
                        "inverseBindMatrices",
                        "accessors",
                        skin.InverseBindMatrices);
                }

                if (skin.HasSkeleton)
                {
                    AddDependency(
                        result,
                        "skins",
                        skinIndex,
                        "skeleton",
                        "nodes",
                        skin.Skeleton);
                }

                int jointPosition = 0;
                while (skin.Joints != null && jointPosition < skin.Joints.Count)
                {
                    AddDependency(
                        result,
                        "skins",
                        skinIndex,
                        $"joints/{jointPosition}",
                        "nodes",
                        skin.Joints[jointPosition]);
                    jointPosition++;
                }
            }

            skinIndex++;
        }
    }

    internal void AddTextureDependencies(
        GltfDocument document,
        List<GltfDependency> result)
    {
        if (document.Textures == null)
        {
            return;
        }

        int textureIndex = 0;
        while (textureIndex < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[textureIndex];
            if (texture != null)
            {
                if (texture.HasSampler)
                {
                    AddDependency(
                        result,
                        "textures",
                        textureIndex,
                        "sampler",
                        "samplers",
                        texture.Sampler);
                }

                if (texture.HasSource)
                {
                    AddDependency(
                        result,
                        "textures",
                        textureIndex,
                        "source",
                        "images",
                        texture.Source);
                }

                if (texture.HasSelectedSource)
                {
                    AddDependency(
                        result,
                        "textures",
                        textureIndex,
                        "selectedSource",
                        "images",
                        texture.SelectedSource);
                }
            }

            textureIndex++;
        }
    }

    internal void AddDependency(
        List<GltfDependency> result,
        string sourceCollection,
        int sourceIndex,
        string sourceMember,
        string targetCollection,
        int targetIndex)
    {
        GltfDependency dependency = new GltfDependency();
        dependency.SourceCollection = sourceCollection;
        dependency.SourceIndex = sourceIndex;
        dependency.SourceMember = sourceMember;
        dependency.TargetCollection = targetCollection;
        dependency.TargetIndex = targetIndex;
        result.Add(dependency);
    }
}
