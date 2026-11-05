using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentRuleValidator
{
    internal SceneGraphEvaluator SceneGraphs = new SceneGraphEvaluator();
    internal ExtensionUsageCollector ExtensionUsage = new ExtensionUsageCollector();
    internal ExtensionRegistry Extensions = new ExtensionRegistry();
    internal MeshQuantization Quantization = new MeshQuantization();

    internal GltfValidationReport ValidateDocumentRules(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        ValidateAssetVersionRules(document, report);
        bool projectable = ValidateObjectGraph(document, report);
        ValidateDefaultSceneReference(document, report);
        ValidateAccessorReferences(document, report);
        ValidateAnimationReferences(document, report);
        ValidateBufferViewReferences(document, report);
        ValidateImageReferences(document, report);
        ValidateMaterialReferences(document, report);
        ValidateMeshReferences(document, report);
        ValidateNodeReferences(document, report);
        ValidateSkinReferences(document, report);
        ValidateTextureReferences(document, report);
        ValidateSceneGraphRules(document, report);
        ValidateExtensionDeclarations(document, report, projectable);
        return report;
    }

    internal void ValidateAssetVersionRules(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Asset == null)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UndefinedProperty,
                GltfValidationSeverity.Error,
                "The required asset object is missing.",
                "/asset");
            return;
        }

        if (document.Asset.Version == null)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UndefinedProperty,
                GltfValidationSeverity.Error,
                "The required asset version is missing.",
                "/asset/version");
        }

        int versionMajor = GetAssetVersionMajor(document.Asset.Version);
        int versionMinor = GetAssetVersionMinor(document.Asset.Version);
        if (versionMajor >= 0 && versionMajor != 2)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnknownAssetMajorVersion,
                GltfValidationSeverity.Error,
                $"Unknown glTF major asset version: {versionMajor}.",
                "/asset/version");
        }
        else if (versionMajor == 2 && versionMinor > 0)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnknownAssetMinorVersion,
                GltfValidationSeverity.Warning,
                $"Unknown glTF minor asset version: {versionMinor}.",
                "/asset/version");
        }

        string? minimumVersion = document.Asset.MinimumVersion;
        if (minimumVersion == null)
        {
            return;
        }

        int minimumMajor = GetAssetVersionMajor(minimumVersion);
        int minimumMinor = GetAssetVersionMinor(minimumVersion);
        if (minimumMajor >= 0 && minimumMajor != 2)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnknownAssetMajorVersion,
                GltfValidationSeverity.Error,
                $"Unknown minimum glTF major asset version: {minimumMajor}.",
                "/asset/minVersion");
        }
        else if (minimumMajor == 2 && minimumMinor > 0)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnknownAssetMinorVersion,
                GltfValidationSeverity.Warning,
                $"Unknown minimum glTF minor asset version: {minimumMinor}.",
                "/asset/minVersion");
        }

        if (versionMajor >= 0 && versionMinor >= 0
            && minimumMajor >= 0 && minimumMinor >= 0
            && (minimumMajor > versionMajor
                || (minimumMajor == versionMajor && minimumMinor > versionMinor)))
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.AssetMinVersionGreaterThanVersion,
                GltfValidationSeverity.Error,
                $"Asset minVersion '{minimumVersion}' is greater than version '{document.Asset.Version}'.",
                "/asset/minVersion");
        }
    }

    internal int GetAssetVersionMajor(string? version)
    {
        if (version == null || version.Length < 3)
        {
            return -1;
        }

        int separator = version.IndexOf('.');
        if (separator <= 0 || separator == version.Length - 1
            || version.IndexOf('.', separator + 1) >= 0)
        {
            return -1;
        }

        return ParseVersionPart(version, 0, separator);
    }

    internal int GetAssetVersionMinor(string? version)
    {
        if (version == null)
        {
            return -1;
        }

        int separator = version.IndexOf('.');
        if (separator <= 0 || separator == version.Length - 1
            || version.IndexOf('.', separator + 1) >= 0)
        {
            return -1;
        }

        return ParseVersionPart(version, separator + 1, version.Length);
    }

    internal int ParseVersionPart(string version, int start, int end)
    {
        int value = 0;
        int index = start;
        while (index < end)
        {
            char digit = version[index];
            if (digit < '0' || digit > '9')
            {
                return -1;
            }

            if (value > 214748364 || (value == 214748364 && digit > '7'))
            {
                return -1;
            }

            value = (value * 10) + digit - '0';
            index++;
        }

        return value;
    }

    internal bool ValidateObjectGraph(
        GltfDocument document,
        GltfValidationReport report)
    {
        bool valid = true;
        if (document.Asset == null)
        {
            valid = false;
        }

        if (document.Accessors == null)
        {
            AddMissingCollection(report, "/accessors");
            valid = false;
        }
        else
        {
            int index = 0;
            while (index < document.Accessors.Count)
            {
                GltfAccessor accessor = document.Accessors[index];
                if (accessor == null)
                {
                    AddNullObject(report, $"/accessors/{index}");
                    valid = false;
                }
                else if (accessor.Sparse != null
                    && (accessor.Sparse.Indices == null || accessor.Sparse.Values == null))
                {
                    if (accessor.Sparse.Indices == null)
                    {
                        AddNullObject(report, $"/accessors/{index}/sparse/indices");
                    }

                    if (accessor.Sparse.Values == null)
                    {
                        AddNullObject(report, $"/accessors/{index}/sparse/values");
                    }

                    valid = false;
                }

                index++;
            }
        }

        if (!ValidateAnimationObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateSimpleRootObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateMeshObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateSceneObjects(document, report))
        {
            valid = false;
        }

        return valid;
    }

    internal bool ValidateAnimationObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Animations == null)
        {
            AddMissingCollection(report, "/animations");
            return false;
        }

        bool valid = true;
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            string pointer = $"/animations/{animationIndex}";
            if (animation == null)
            {
                AddNullObject(report, pointer);
                valid = false;
                animationIndex++;
                continue;
            }

            if (animation.Channels == null)
            {
                AddMissingCollection(report, $"{pointer}/channels");
                valid = false;
            }
            else
            {
                int channelIndex = 0;
                while (channelIndex < animation.Channels.Count)
                {
                    GltfAnimationChannel channel = animation.Channels[channelIndex];
                    string channelPointer = $"{pointer}/channels/{channelIndex}";
                    if (channel == null)
                    {
                        AddNullObject(report, channelPointer);
                        valid = false;
                    }
                    else if (channel.Target == null)
                    {
                        AddNullObject(report, $"{channelPointer}/target");
                        valid = false;
                    }

                    channelIndex++;
                }
            }

            if (animation.Samplers == null)
            {
                AddMissingCollection(report, $"{pointer}/samplers");
                valid = false;
            }
            else
            {
                int samplerIndex = 0;
                while (samplerIndex < animation.Samplers.Count)
                {
                    if (animation.Samplers[samplerIndex] == null)
                    {
                        AddNullObject(report, $"{pointer}/samplers/{samplerIndex}");
                        valid = false;
                    }

                    samplerIndex++;
                }
            }

            animationIndex++;
        }

        return valid;
    }

    internal bool ValidateSimpleRootObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        bool valid = true;
        if (!ValidateBufferObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateBufferViewObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateCameraObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateImageObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateMaterialObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateSamplerObjects(document, report))
        {
            valid = false;
        }

        if (!ValidateTextureObjects(document, report))
        {
            valid = false;
        }

        return valid;
    }

    internal bool ValidateBufferObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Buffers == null)
        {
            AddMissingCollection(report, "/buffers");
            return false;
        }

        return ValidateBufferObjectItems(document.Buffers, report);
    }

    internal bool ValidateBufferObjectItems(
        List<GltfBuffer> objects,
        GltfValidationReport report)
    {
        bool valid = true;
        int index = 0;
        while (index < objects.Count)
        {
            if (objects[index] == null)
            {
                AddNullObject(report, $"/buffers/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateBufferViewObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.BufferViews == null)
        {
            AddMissingCollection(report, "/bufferViews");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.BufferViews.Count)
        {
            if (document.BufferViews[index] == null)
            {
                AddNullObject(report, $"/bufferViews/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateCameraObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Cameras == null)
        {
            AddMissingCollection(report, "/cameras");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.Cameras.Count)
        {
            GltfCamera camera = document.Cameras[index];
            string pointer = $"/cameras/{index}";
            if (camera == null)
            {
                AddNullObject(report, pointer);
                valid = false;
            }
            else if (camera.Type == GltfCameraType.Perspective && camera.Perspective == null)
            {
                AddNullObject(report, $"{pointer}/perspective");
                valid = false;
            }
            else if (camera.Type == GltfCameraType.Orthographic && camera.Orthographic == null)
            {
                AddNullObject(report, $"{pointer}/orthographic");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateImageObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Images == null)
        {
            AddMissingCollection(report, "/images");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.Images.Count)
        {
            if (document.Images[index] == null)
            {
                AddNullObject(report, $"/images/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateMaterialObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Materials == null)
        {
            AddMissingCollection(report, "/materials");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.Materials.Count)
        {
            if (document.Materials[index] == null)
            {
                AddNullObject(report, $"/materials/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateSamplerObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Samplers == null)
        {
            AddMissingCollection(report, "/samplers");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.Samplers.Count)
        {
            if (document.Samplers[index] == null)
            {
                AddNullObject(report, $"/samplers/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateTextureObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Textures == null)
        {
            AddMissingCollection(report, "/textures");
            return false;
        }

        bool valid = true;
        int index = 0;
        while (index < document.Textures.Count)
        {
            if (document.Textures[index] == null)
            {
                AddNullObject(report, $"/textures/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool ValidateMeshObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Meshes == null)
        {
            AddMissingCollection(report, "/meshes");
            return false;
        }

        bool valid = true;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            string meshPointer = $"/meshes/{meshIndex}";
            if (mesh == null)
            {
                AddNullObject(report, meshPointer);
                valid = false;
                meshIndex++;
                continue;
            }

            if (mesh.Primitives == null)
            {
                AddMissingCollection(report, $"{meshPointer}/primitives");
                valid = false;
                meshIndex++;
                continue;
            }

            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                string primitivePointer = $"{meshPointer}/primitives/{primitiveIndex}";
                if (primitive == null)
                {
                    AddNullObject(report, primitivePointer);
                    valid = false;
                    primitiveIndex++;
                    continue;
                }

                if (primitive.Attributes == null)
                {
                    AddMissingCollection(report, $"{primitivePointer}/attributes");
                    valid = false;
                }
                else
                {
                    int attributeIndex = 0;
                    while (attributeIndex < primitive.Attributes.Count)
                    {
                        if (primitive.Attributes[attributeIndex] == null)
                        {
                            AddNullObject(
                                report,
                                $"{primitivePointer}/attributes/{attributeIndex}");
                            valid = false;
                        }

                        attributeIndex++;
                    }
                }

                if (primitive.Targets == null)
                {
                    AddMissingCollection(report, $"{primitivePointer}/targets");
                    valid = false;
                }
                else
                {
                    int targetIndex = 0;
                    while (targetIndex < primitive.Targets.Count)
                    {
                        GltfMorphTarget target = primitive.Targets[targetIndex];
                        string targetPointer = $"{primitivePointer}/targets/{targetIndex}";
                        if (target == null)
                        {
                            AddNullObject(report, targetPointer);
                            valid = false;
                        }
                        else if (target.Attributes == null)
                        {
                            AddMissingCollection(report, $"{targetPointer}/attributes");
                            valid = false;
                        }
                        else
                        {
                            int attributeIndex = 0;
                            while (attributeIndex < target.Attributes.Count)
                            {
                                if (target.Attributes[attributeIndex] == null)
                                {
                                    AddNullObject(
                                        report,
                                        $"{targetPointer}/attributes/{attributeIndex}");
                                    valid = false;
                                }

                                attributeIndex++;
                            }
                        }

                        targetIndex++;
                    }
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        return valid;
    }

    internal bool ValidateSceneObjects(
        GltfDocument document,
        GltfValidationReport report)
    {
        bool valid = true;
        if (document.Nodes == null)
        {
            AddMissingCollection(report, "/nodes");
            valid = false;
        }
        else
        {
            int nodeIndex = 0;
            while (nodeIndex < document.Nodes.Count)
            {
                GltfNode node = document.Nodes[nodeIndex];
                if (node == null)
                {
                    AddNullObject(report, $"/nodes/{nodeIndex}");
                    valid = false;
                }
                else if (node.Children == null)
                {
                    AddMissingCollection(report, $"/nodes/{nodeIndex}/children");
                    valid = false;
                }

                nodeIndex++;
            }
        }

        if (document.Scenes == null)
        {
            AddMissingCollection(report, "/scenes");
            valid = false;
        }
        else
        {
            int sceneIndex = 0;
            while (sceneIndex < document.Scenes.Count)
            {
                GltfScene scene = document.Scenes[sceneIndex];
                if (scene == null)
                {
                    AddNullObject(report, $"/scenes/{sceneIndex}");
                    valid = false;
                }
                else if (scene.Nodes == null)
                {
                    AddMissingCollection(report, $"/scenes/{sceneIndex}/nodes");
                    valid = false;
                }

                sceneIndex++;
            }
        }

        if (document.Skins == null)
        {
            AddMissingCollection(report, "/skins");
            valid = false;
        }
        else
        {
            int skinIndex = 0;
            while (skinIndex < document.Skins.Count)
            {
                GltfSkin skin = document.Skins[skinIndex];
                if (skin == null)
                {
                    AddNullObject(report, $"/skins/{skinIndex}");
                    valid = false;
                }
                else if (skin.Joints == null)
                {
                    AddMissingCollection(report, $"/skins/{skinIndex}/joints");
                    valid = false;
                }

                skinIndex++;
            }
        }

        return valid;
    }

    internal void ValidateDefaultSceneReference(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.HasDefaultScene)
        {
            int sceneCount = document.Scenes == null ? 0 : document.Scenes.Count;
            ValidateReferenceIndex(
                report,
                document.DefaultScene,
                sceneCount,
                "/scene",
                "scene");
        }
    }

    internal void ValidateAccessorReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Accessors == null)
        {
            return;
        }

        int bufferViewCount = document.BufferViews == null ? 0 : document.BufferViews.Count;
        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            string pointer = $"/accessors/{accessorIndex}";
            if (accessor != null)
            {
                if (accessor.HasBufferView)
                {
                    ValidateReferenceIndex(
                        report,
                        accessor.BufferView,
                        bufferViewCount,
                        $"{pointer}/bufferView",
                        "bufferView");
                }

                if (accessor.Sparse != null)
                {
                    if (accessor.Sparse.Indices != null)
                    {
                        ValidateReferenceIndex(
                            report,
                            accessor.Sparse.Indices.BufferView,
                            bufferViewCount,
                            $"{pointer}/sparse/indices/bufferView",
                            "bufferView");
                    }

                    if (accessor.Sparse.Values != null)
                    {
                        ValidateReferenceIndex(
                            report,
                            accessor.Sparse.Values.BufferView,
                            bufferViewCount,
                            $"{pointer}/sparse/values/bufferView",
                            "bufferView");
                    }
                }
            }

            accessorIndex++;
        }
    }

    internal void ValidateAnimationReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Animations == null)
        {
            return;
        }

        int accessorCount = document.Accessors == null ? 0 : document.Accessors.Count;
        int nodeCount = document.Nodes == null ? 0 : document.Nodes.Count;
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            string pointer = $"/animations/{animationIndex}";
            if (animation != null)
            {
                if (animation.Samplers != null)
                {
                    int samplerIndex = 0;
                    while (samplerIndex < animation.Samplers.Count)
                    {
                        GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
                        if (sampler != null)
                        {
                            ValidateReferenceIndex(
                                report,
                                sampler.Input,
                                accessorCount,
                                $"{pointer}/samplers/{samplerIndex}/input",
                                "accessor");
                            ValidateReferenceIndex(
                                report,
                                sampler.Output,
                                accessorCount,
                                $"{pointer}/samplers/{samplerIndex}/output",
                                "accessor");
                        }

                        samplerIndex++;
                    }
                }

                if (animation.Channels != null)
                {
                    int channelIndex = 0;
                    while (channelIndex < animation.Channels.Count)
                    {
                        GltfAnimationChannel channel = animation.Channels[channelIndex];
                        if (channel != null)
                        {
                            int samplerCount = animation.Samplers == null
                                ? 0
                                : animation.Samplers.Count;
                            ValidateReferenceIndex(
                                report,
                                channel.Sampler,
                                samplerCount,
                                $"{pointer}/channels/{channelIndex}/sampler",
                                "animation sampler");
                            if (channel.Target != null && channel.Target.HasNode)
                            {
                                ValidateReferenceIndex(
                                    report,
                                    channel.Target.Node,
                                    nodeCount,
                                    $"{pointer}/channels/{channelIndex}/target/node",
                                    "node");
                            }
                        }

                        channelIndex++;
                    }
                }
            }

            animationIndex++;
        }
    }

    internal void ValidateBufferViewReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.BufferViews == null)
        {
            return;
        }

        int bufferCount = document.Buffers == null ? 0 : document.Buffers.Count;
        int index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView bufferView = document.BufferViews[index];
            if (bufferView != null)
            {
                ValidateReferenceIndex(
                    report,
                    bufferView.Buffer,
                    bufferCount,
                    $"/bufferViews/{index}/buffer",
                    "buffer");
            }

            index++;
        }
    }

    internal void ValidateImageReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Images == null)
        {
            return;
        }

        int bufferViewCount = document.BufferViews == null ? 0 : document.BufferViews.Count;
        int index = 0;
        while (index < document.Images.Count)
        {
            GltfImage image = document.Images[index];
            if (image != null && image.HasBufferView)
            {
                ValidateReferenceIndex(
                    report,
                    image.BufferView,
                    bufferViewCount,
                    $"/images/{index}/bufferView",
                    "bufferView");
            }

            index++;
        }
    }

    internal void ValidateMaterialReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Materials == null)
        {
            return;
        }

        int textureCount = document.Textures == null ? 0 : document.Textures.Count;
        int index = 0;
        while (index < document.Materials.Count)
        {
            GltfMaterial material = document.Materials[index];
            string pointer = $"/materials/{index}";
            if (material != null)
            {
                if (material.PbrMetallicRoughness != null)
                {
                    if (material.PbrMetallicRoughness.BaseColorTexture != null)
                    {
                        ValidateReferenceIndex(
                            report,
                            material.PbrMetallicRoughness.BaseColorTexture.Index,
                            textureCount,
                            $"{pointer}/pbrMetallicRoughness/baseColorTexture/index",
                            "texture");
                    }

                    if (material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                    {
                        ValidateReferenceIndex(
                            report,
                            material.PbrMetallicRoughness.MetallicRoughnessTexture.Index,
                            textureCount,
                            $"{pointer}/pbrMetallicRoughness/metallicRoughnessTexture/index",
                            "texture");
                    }
                }

                if (material.NormalTexture != null)
                {
                    ValidateReferenceIndex(
                        report,
                        material.NormalTexture.Index,
                        textureCount,
                        $"{pointer}/normalTexture/index",
                        "texture");
                }

                if (material.OcclusionTexture != null)
                {
                    ValidateReferenceIndex(
                        report,
                        material.OcclusionTexture.Index,
                        textureCount,
                        $"{pointer}/occlusionTexture/index",
                        "texture");
                }

                if (material.EmissiveTexture != null)
                {
                    ValidateReferenceIndex(
                        report,
                        material.EmissiveTexture.Index,
                        textureCount,
                        $"{pointer}/emissiveTexture/index",
                        "texture");
                }
            }

            index++;
        }
    }

    internal void ValidateMeshReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Meshes == null)
        {
            return;
        }

        int accessorCount = document.Accessors == null ? 0 : document.Accessors.Count;
        int materialCount = document.Materials == null ? 0 : document.Materials.Count;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh != null && mesh.Primitives != null)
            {
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                    string pointer = $"/meshes/{meshIndex}/primitives/{primitiveIndex}";
                    if (primitive != null)
                    {
                        if (primitive.HasIndices)
                        {
                            ValidateReferenceIndex(
                                report,
                                primitive.Indices,
                                accessorCount,
                                $"{pointer}/indices",
                                "accessor");
                        }

                        if (primitive.HasMaterial)
                        {
                            ValidateReferenceIndex(
                                report,
                                primitive.Material,
                                materialCount,
                                $"{pointer}/material",
                                "material");
                        }

                        ValidateAttributeReferences(
                            primitive.Attributes,
                            accessorCount,
                            $"{pointer}/attributes",
                            report);
                        if (primitive.Targets != null)
                        {
                            int targetIndex = 0;
                            while (targetIndex < primitive.Targets.Count)
                            {
                                GltfMorphTarget target = primitive.Targets[targetIndex];
                                if (target != null)
                                {
                                    ValidateAttributeReferences(
                                        target.Attributes,
                                        accessorCount,
                                        $"{pointer}/targets/{targetIndex}",
                                        report);
                                }

                                targetIndex++;
                            }
                        }
                    }

                    primitiveIndex++;
                }
            }

            meshIndex++;
        }
    }

    internal void ValidateAttributeReferences(
        List<GltfAttribute> attributes,
        int accessorCount,
        string pointer,
        GltfValidationReport report)
    {
        if (attributes == null)
        {
            return;
        }

        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            if (attribute != null)
            {
                string memberPointer = AppendPointerSegment(pointer, attribute.Semantic);
                ValidateReferenceIndex(
                    report,
                    attribute.Accessor,
                    accessorCount,
                    memberPointer,
                    "accessor");
            }

            index++;
        }
    }

    internal void ValidateNodeReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Nodes == null)
        {
            return;
        }

        int cameraCount = document.Cameras == null ? 0 : document.Cameras.Count;
        int skinCount = document.Skins == null ? 0 : document.Skins.Count;
        int meshCount = document.Meshes == null ? 0 : document.Meshes.Count;
        int index = 0;
        while (index < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[index];
            if (node != null)
            {
                if (node.HasCamera)
                {
                    ValidateReferenceIndex(
                        report,
                        node.Camera,
                        cameraCount,
                        $"/nodes/{index}/camera",
                        "camera");
                }

                if (node.HasSkin)
                {
                    ValidateReferenceIndex(
                        report,
                        node.Skin,
                        skinCount,
                        $"/nodes/{index}/skin",
                        "skin");
                }

                if (node.HasMesh)
                {
                    ValidateReferenceIndex(
                        report,
                        node.Mesh,
                        meshCount,
                        $"/nodes/{index}/mesh",
                        "mesh");
                }
            }

            index++;
        }
    }

    internal void ValidateSkinReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Skins == null)
        {
            return;
        }

        int accessorCount = document.Accessors == null ? 0 : document.Accessors.Count;
        int nodeCount = document.Nodes == null ? 0 : document.Nodes.Count;
        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            string pointer = $"/skins/{skinIndex}";
            if (skin != null)
            {
                if (skin.HasInverseBindMatrices)
                {
                    ValidateReferenceIndex(
                        report,
                        skin.InverseBindMatrices,
                        accessorCount,
                        $"{pointer}/inverseBindMatrices",
                        "accessor");
                }

                if (skin.HasSkeleton)
                {
                    ValidateReferenceIndex(
                        report,
                        skin.Skeleton,
                        nodeCount,
                        $"{pointer}/skeleton",
                        "node");
                }

                if (skin.Joints != null)
                {
                    int jointIndex = 0;
                    while (jointIndex < skin.Joints.Count)
                    {
                        ValidateReferenceIndex(
                            report,
                            skin.Joints[jointIndex],
                            nodeCount,
                            $"{pointer}/joints/{jointIndex}",
                            "node");
                        jointIndex++;
                    }
                }
            }

            skinIndex++;
        }
    }

    internal void ValidateTextureReferences(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Textures == null)
        {
            return;
        }

        int samplerCount = document.Samplers == null ? 0 : document.Samplers.Count;
        int imageCount = document.Images == null ? 0 : document.Images.Count;
        int index = 0;
        while (index < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[index];
            if (texture != null)
            {
                if (texture.HasSampler)
                {
                    ValidateReferenceIndex(
                        report,
                        texture.Sampler,
                        samplerCount,
                        $"/textures/{index}/sampler",
                        "sampler");
                }

                if (texture.HasSource)
                {
                    ValidateReferenceIndex(
                        report,
                        texture.Source,
                        imageCount,
                        $"/textures/{index}/source",
                        "image");
                }
            }

            index++;
        }
    }

    internal void ValidateSceneGraphRules(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (!CanAnalyzeSceneGraph(document))
        {
            return;
        }

        SceneGraphAnalysis analysis = SceneGraphs.AnalyzeDocument(document);
        int index = 0;
        while (index < analysis.Diagnostics.Count)
        {
            GltfValidationIssue issue = analysis.Diagnostics[index];
            report.Issues.Add(issue);
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                report.IsValid = false;
            }

            index++;
        }
    }

    internal bool CanAnalyzeSceneGraph(GltfDocument document)
    {
        if (document.Nodes == null || document.Scenes == null)
        {
            return false;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node == null || node.Children == null)
            {
                return false;
            }

            nodeIndex++;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfScene scene = document.Scenes[sceneIndex];
            if (scene == null || scene.Nodes == null)
            {
                return false;
            }

            sceneIndex++;
        }

        return true;
    }

    internal void ValidateExtensionDeclarations(
        GltfDocument document,
        GltfValidationReport report,
        bool projectable)
    {
        if (document.ExtensionsUsed == null)
        {
            AddMissingCollection(report, "/extensionsUsed");
        }
        else
        {
            ValidateExtensionNameList(document.ExtensionsUsed, "/extensionsUsed", report);
        }

        if (document.ExtensionsRequired == null)
        {
            AddMissingCollection(report, "/extensionsRequired");
        }
        else
        {
            ValidateExtensionNameList(
                document.ExtensionsRequired,
                "/extensionsRequired",
                report);
            int requiredIndex = 0;
            while (requiredIndex < document.ExtensionsRequired.Count)
            {
                string required = document.ExtensionsRequired[requiredIndex];
                if (required != null)
                {
                    if (document.ExtensionsUsed == null
                        || !document.ExtensionsUsed.Contains(required))
                    {
                        AddRuleIssue(
                            report,
                            GltfIssueCodes.UnusedExtensionRequired,
                            GltfValidationSeverity.Error,
                            $"Unused extension '{required}' cannot be required.",
                            $"/extensionsRequired/{requiredIndex}");
                    }

                    if (!IsKnownExtensionName(required))
                    {
                        AddRuleIssue(
                            report,
                            GltfIssueCodes.UnsupportedExtension,
                            GltfValidationSeverity.Warning,
                            $"The required extension '{required}' is unknown.",
                            $"/extensionsRequired/{requiredIndex}");
                    }
                }

                requiredIndex++;
            }
        }

        ValidateMeshQuantizationDeclarations(document, report);

        if (!projectable || document.ExtensionsUsed == null
            || document.ExtensionsRequired == null)
        {
            return;
        }

        List<EncounteredExtension> encountered = ExtensionUsage
            .CollectEncounteredExtensionLocations(document);
        int extensionIndex = 0;
        while (extensionIndex < encountered.Count)
        {
            EncounteredExtension extension = encountered[extensionIndex];
            if (!IsValidExtensionName(extension.Name))
            {
                AddRuleIssue(
                    report,
                    GltfIssueCodes.InvalidExtensionNameFormat,
                    GltfValidationSeverity.Warning,
                    $"Extension name '{extension.Name}' has invalid format.",
                    extension.JsonPointer);
            }

            if (!document.ExtensionsUsed.Contains(extension.Name))
            {
                AddRuleIssue(
                    report,
                    GltfIssueCodes.UndeclaredExtension,
                    GltfValidationSeverity.Error,
                    $"Extension '{extension.Name}' is not declared in extensionsUsed.",
                    extension.JsonPointer);
            }

            extensionIndex++;
        }
    }

    internal void ValidateMeshQuantizationDeclarations(
        GltfDocument document,
        GltfValidationReport report)
    {
        string? attributePointer = Quantization.FindFirstQuantizedAttribute(document);
        bool declaredUsed = document.ExtensionsUsed != null
            && document.ExtensionsUsed.Contains(GltfExtensionNames.KhrMeshQuantization);
        bool declaredRequired = document.ExtensionsRequired != null
            && document.ExtensionsRequired.Contains(GltfExtensionNames.KhrMeshQuantization);
        if (attributePointer != null && !declaredUsed)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UndeclaredExtension,
                GltfValidationSeverity.Error,
                "A quantized mesh attribute requires KHR_mesh_quantization in extensionsUsed.",
                attributePointer);
        }

        if ((attributePointer != null || declaredUsed) && !declaredRequired)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnsatisfiedDependency,
                GltfValidationSeverity.Error,
                "KHR_mesh_quantization must be listed in extensionsRequired because it has no fallback representation.",
                "/extensionsRequired");
        }
    }

    internal void ValidateExtensionNameList(
        List<string> names,
        string pointer,
        GltfValidationReport report)
    {
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            string itemPointer = $"{pointer}/{index}";
            if (name == null)
            {
                AddRuleIssue(
                    report,
                    GltfIssueCodes.TypeMismatch,
                    GltfValidationSeverity.Error,
                    "Extension names must be strings.",
                    itemPointer);
            }
            else
            {
                if (!IsValidExtensionName(name))
                {
                    AddRuleIssue(
                        report,
                        GltfIssueCodes.InvalidExtensionNameFormat,
                        GltfValidationSeverity.Warning,
                        $"Extension name '{name}' has invalid format.",
                        itemPointer);
                }

                int earlierIndex = 0;
                while (earlierIndex < index)
                {
                    if (names[earlierIndex] == name)
                    {
                        AddRuleIssue(
                            report,
                            GltfIssueCodes.DuplicateElements,
                            GltfValidationSeverity.Error,
                            $"Extension name '{name}' is duplicated.",
                            itemPointer);
                        break;
                    }

                    earlierIndex++;
                }
            }

            index++;
        }
    }

    internal bool IsValidExtensionName(string name)
    {
        int separator = name.IndexOf('_');
        if (separator <= 0 || separator == name.Length - 1)
        {
            return false;
        }

        int index = 0;
        while (index < separator)
        {
            char value = name[index];
            if (!IsUppercaseAsciiLetter(value)
                && (index == 0 || value < '0' || value > '9'))
            {
                return false;
            }

            index++;
        }

        index = separator + 1;
        bool previousUnderscore = false;
        while (index < name.Length)
        {
            char value = name[index];
            if (value == '_')
            {
                if (previousUnderscore
                    || index == separator + 1
                    || index == name.Length - 1)
                {
                    return false;
                }

                previousUnderscore = true;
            }
            else if (!IsUppercaseAsciiLetter(value)
                && !IsLowercaseAsciiLetter(value)
                && (value < '0' || value > '9'))
            {
                return false;
            }
            else
            {
                previousUnderscore = false;
            }

            index++;
        }

        return true;
    }

    internal bool IsUppercaseAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal bool IsLowercaseAsciiLetter(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal bool IsKnownExtensionName(string name)
    {
        return Extensions.FindDescriptor(name) != null;
    }

    internal void ValidateReferenceIndex(
        GltfValidationReport report,
        int value,
        int count,
        string pointer,
        string target)
    {
        if (value < 0 || value >= count)
        {
            AddRuleIssue(
                report,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Unresolved {target} reference: {value}.",
                pointer);
        }
    }

    internal string AppendPointerSegment(string pointer, string? segment)
    {
        if (segment == null)
        {
            return pointer;
        }

        return pointer + "/" + segment.Replace("~", "~0").Replace("/", "~1");
    }

    internal void AddMissingCollection(GltfValidationReport report, string pointer)
    {
        AddRuleIssue(
            report,
            GltfIssueCodes.ArrayTypeMismatch,
            GltfValidationSeverity.Error,
            "The object collection is missing.",
            pointer);
    }

    internal void AddNullObject(GltfValidationReport report, string pointer)
    {
        AddRuleIssue(
            report,
            GltfIssueCodes.TypeMismatch,
            GltfValidationSeverity.Error,
            "The object is null.",
            pointer);
    }

    internal void AddRuleIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
