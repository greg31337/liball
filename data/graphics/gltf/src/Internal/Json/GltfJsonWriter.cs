using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfJsonWriter
{
    internal GltfWriteOptions Options = new GltfWriteOptions();
    internal DeterministicJsonWriter FreeFormWriter = new DeterministicJsonWriter();
    internal ExtensionHandlerDispatcher ExtensionDispatcher = new ExtensionHandlerDispatcher();
    internal ArchivedExtensionPolicy ArchivedExtensions = new ArchivedExtensionPolicy();
    internal GltfDocument? Document;
    internal GltfResourceSet ExtensionResources = new GltfResourceSet();
    internal List<GltfValidationIssue> ExtensionDiagnostics = new List<GltfValidationIssue>();
    internal GltfOperationStatus ExtensionStatus = GltfOperationStatus.Success;
    internal bool DispatchExtensionWrites = true;

    internal GltfWriteResult WriteOutput(
        GltfDocument document,
        GltfWriteOptions options,
        bool includeJsonText)
    {
        JsonObject json = WriteDocument(document, options);
        string text = FreeFormWriter.WriteFormattedValue(json, options.Indented, false);
        UTF8Encoding encoding = new UTF8Encoding(false, true);
        byte[] bytes = encoding.GetBytes(text);
        GltfWriteResult result = new GltfWriteResult();
        result.PrimaryBytes = bytes;
        ApplyExtensionOutput(result);
        if (includeJsonText)
        {
            result.JsonText = encoding.GetString(bytes);
        }

        return result;
    }

    internal GltfWriteResult WriteResourceOutput(
        GltfDocument document,
        GltfWriteOptions options,
        bool includeJsonText,
        ResourceWritePlan resourcePlan)
    {
        if (resourcePlan == null)
        {
            throw new ArgumentNullException("resourcePlan");
        }

        JsonObject json = WriteDocument(document, options);
        DispatchExtensionWrites = false;
        ApplyResourcePlan(json, document, resourcePlan);
        string text = FreeFormWriter.WriteFormattedValue(json, options.Indented, false);
        UTF8Encoding encoding = new UTF8Encoding(false, true);
        byte[] bytes = encoding.GetBytes(text);
        GltfWriteResult result = new GltfWriteResult();
        result.PrimaryBytes = bytes;
        ApplyExtensionOutput(result);
        if (includeJsonText)
        {
            result.JsonText = encoding.GetString(bytes);
        }

        return result;
    }

    internal void ApplyResourcePlan(
        JsonObject json,
        GltfDocument document,
        ResourceWritePlan resourcePlan)
    {
        if (resourcePlan.Mode == GltfResourceMode.BufferView)
        {
            ApplyBufferViewImagePlan(json, document, resourcePlan);
            return;
        }

        if (resourcePlan.Mode != GltfResourceMode.External
            && resourcePlan.Mode != GltfResourceMode.DataUri)
        {
            return;
        }

        if (document.Buffers.Count > 0)
        {
            JsonArray buffers = new JsonArray();
            int bufferIndex = 0;
            while (bufferIndex < document.Buffers.Count)
            {
                GltfBuffer source = document.Buffers[bufferIndex];
                JsonObject output = WriteBuffer(source);
                if (source.HasResource)
                {
                    output["uri"] = JsonValue.Create(
                        GetPlannedResourceUri(resourcePlan, source.Resource));
                }

                buffers.Add(output);
                bufferIndex++;
            }

            json["buffers"] = buffers;
        }

        if (document.Images.Count > 0)
        {
            JsonArray images = new JsonArray();
            int imageIndex = 0;
            while (imageIndex < document.Images.Count)
            {
                GltfImage source = document.Images[imageIndex];
                JsonObject output = WriteImage(source);
                if (source.HasResource)
                {
                    output["uri"] = JsonValue.Create(
                        GetPlannedResourceUri(resourcePlan, source.Resource));
                    output.Remove("bufferView");
                }

                images.Add(output);
                imageIndex++;
            }

            json["images"] = images;
        }
    }

    internal void ApplyBufferViewImagePlan(
        JsonObject json,
        GltfDocument document,
        ResourceWritePlan resourcePlan)
    {
        ImageBufferViewPackPlan? imagePack = resourcePlan.ImagePack;
        if (imagePack == null)
        {
            throw new GltfWriteException("The image bufferView plan is missing.");
        }

        if (imagePack.Images.Count == 0)
        {
            return;
        }

        JsonArray buffers = new JsonArray();
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            buffers.Add(WriteBuffer(document.Buffers[bufferIndex]));
            bufferIndex++;
        }

        JsonObject packedBuffer = new JsonObject();
        packedBuffer["uri"] = JsonValue.Create(GetPackedBufferUri(resourcePlan));
        packedBuffer["byteLength"] = JsonValue.Create(imagePack.BufferBytes.Length);
        buffers.Add(packedBuffer);
        SetOrderedBufferArray(json, buffers);

        JsonArray bufferViews = new JsonArray();
        int bufferViewIndex = 0;
        while (bufferViewIndex < document.BufferViews.Count)
        {
            bufferViews.Add(WriteBufferView(document.BufferViews[bufferViewIndex]));
            bufferViewIndex++;
        }

        int packedViewIndex = 0;
        while (packedViewIndex < imagePack.ByteOffsets.Count)
        {
            JsonObject packedView = new JsonObject();
            packedView["buffer"] = JsonValue.Create(document.Buffers.Count);
            if (imagePack.ByteOffsets[packedViewIndex] > 0)
            {
                packedView["byteOffset"] = JsonValue.Create(
                    imagePack.ByteOffsets[packedViewIndex]);
            }

            packedView["byteLength"] = JsonValue.Create(
                imagePack.ByteLengths[packedViewIndex]);
            bufferViews.Add(packedView);
            packedViewIndex++;
        }

        SetOrderedBufferViewArray(json, bufferViews);

        JsonArray images = new JsonArray();
        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage source = document.Images[imageIndex];
            JsonObject output = WriteImage(source);
            if (source.HasResource)
            {
                int resourceIndex = source.Resource;
                if (resourceIndex < 0
                    || resourceIndex >= imagePack.ResourceBufferViews.Count)
                {
                    throw new GltfWriteException(
                        $"Image {imageIndex} resource index {resourceIndex} is outside the image packing plan.");
                }

                int localViewIndex = imagePack.ResourceBufferViews[resourceIndex];
                if (localViewIndex < 0)
                {
                    throw new GltfWriteException(
                        $"Image {imageIndex} resource {resourceIndex} is not an image resource.");
                }

                output.Remove("uri");
                output["mimeType"] = JsonValue.Create(imagePack.MimeTypes[localViewIndex]);
                output["bufferView"] = JsonValue.Create(
                    document.BufferViews.Count + localViewIndex);
            }

            images.Add(output);
            imageIndex++;
        }

        json["images"] = images;
    }

    internal string GetPackedBufferUri(ResourceWritePlan resourcePlan)
    {
        int index = resourcePlan.Resources.Resources.Count - 1;
        if (index < 0)
        {
            throw new GltfWriteException("The image packing plan has no packed buffer resource.");
        }

        string? uri = resourcePlan.Resources.Resources[index].EffectiveUri;
        if (uri == null)
        {
            throw new GltfWriteException("The packed image buffer has no output URI.");
        }

        return uri;
    }

    internal void SetOrderedBufferArray(JsonObject json, JsonArray buffers)
    {
        if (json.ContainsKey("buffers"))
        {
            json["buffers"] = buffers;
            return;
        }

        int index = 1;
        if (json.ContainsKey("accessors"))
        {
            index++;
        }

        if (json.ContainsKey("animations"))
        {
            index++;
        }

        json.Insert(index, "buffers", buffers);
    }

    internal void SetOrderedBufferViewArray(JsonObject json, JsonArray bufferViews)
    {
        if (json.ContainsKey("bufferViews"))
        {
            json["bufferViews"] = bufferViews;
            return;
        }

        int index = 1;
        if (json.ContainsKey("accessors"))
        {
            index++;
        }

        if (json.ContainsKey("animations"))
        {
            index++;
        }

        index++;
        json.Insert(index, "bufferViews", bufferViews);
    }

    internal string GetPlannedResourceUri(ResourceWritePlan resourcePlan, int sourceResourceIndex)
    {
        if (sourceResourceIndex < 0
            || sourceResourceIndex >= resourcePlan.SourceResourceUris.Count)
        {
            throw new GltfWriteException(
                $"Resource index {sourceResourceIndex} is outside the planned resource collection.");
        }

        string? uri = resourcePlan.SourceResourceUris[sourceResourceIndex];
        if (uri == null)
        {
            throw new GltfWriteException(
                $"Planned resource {sourceResourceIndex} has no output URI.");
        }

        return uri;
    }

    internal JsonObject WriteDocument(GltfDocument document, GltfWriteOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Options = options;
        Document = document;
        ExtensionResources = new GltfResourceSet();
        ExtensionDiagnostics = new List<GltfValidationIssue>();
        ExtensionStatus = GltfOperationStatus.Success;
        DispatchExtensionWrites = true;
        JsonObject result = new JsonObject();
        result["asset"] = WriteAsset(document.Asset);
        if (document.Accessors.Count > 0)
        {
            result["accessors"] = WriteAccessorCollection(document.Accessors);
        }

        if (document.Animations.Count > 0)
        {
            result["animations"] = WriteAnimationCollection(document.Animations);
        }

        if (document.Buffers.Count > 0)
        {
            result["buffers"] = WriteBufferCollection(document.Buffers);
        }

        if (document.BufferViews.Count > 0)
        {
            result["bufferViews"] = WriteBufferViewCollection(document.BufferViews);
        }

        if (document.Cameras.Count > 0)
        {
            result["cameras"] = WriteCameraCollection(document.Cameras);
        }

        if (document.Images.Count > 0)
        {
            result["images"] = WriteImageCollection(document.Images);
        }

        if (document.Materials.Count > 0)
        {
            result["materials"] = WriteMaterialCollection(document.Materials);
        }

        if (document.Meshes.Count > 0)
        {
            result["meshes"] = WriteMeshCollection(document.Meshes);
        }

        if (document.Nodes.Count > 0)
        {
            result["nodes"] = WriteNodeCollection(document.Nodes);
        }

        if (document.Samplers.Count > 0)
        {
            result["samplers"] = WriteSamplerCollection(document.Samplers);
        }

        if (document.Scenes.Count > 0)
        {
            result["scenes"] = WriteSceneCollection(document.Scenes);
        }

        if (document.Skins.Count > 0)
        {
            result["skins"] = WriteSkinCollection(document.Skins);
        }

        if (document.Textures.Count > 0)
        {
            result["textures"] = WriteTextureCollection(document.Textures);
        }

        if (document.HasDefaultScene)
        {
            result["scene"] = JsonValue.Create(document.DefaultScene);
        }

        if (document.ExtensionsUsed.Count > 0)
        {
            result["extensionsUsed"] = WriteStringList(document.ExtensionsUsed);
        }

        if (document.ExtensionsRequired.Count > 0)
        {
            result["extensionsRequired"] = WriteRequiredExtensionList(document.ExtensionsRequired);
        }

        AppendPropertyJson(result, document.PropertyJson, "glTF");
        return result;
    }

    internal JsonObject WriteAsset(GltfAsset source)
    {
        JsonObject result = new JsonObject();
        result["version"] = JsonValue.Create(source.Version);
        if (source.MinimumVersion != null)
        {
            result["minVersion"] = JsonValue.Create(source.MinimumVersion);
        }

        if (source.Generator != null)
        {
            result["generator"] = JsonValue.Create(source.Generator);
        }

        if (source.Copyright != null)
        {
            result["copyright"] = JsonValue.Create(source.Copyright);
        }

        AppendPropertyJson(result, source.PropertyJson, "asset");
        return result;
    }

    internal JsonArray WriteAccessorCollection(List<GltfAccessor> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteAccessor(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteAccessor(GltfAccessor source)
    {
        JsonObject result = new JsonObject();
        if (source.HasBufferView)
        {
            result["bufferView"] = JsonValue.Create(source.BufferView);
        }

        if (ShouldWriteIntDefault(source.HasByteOffset, source.ByteOffset, 0))
        {
            result["byteOffset"] = JsonValue.Create(source.ByteOffset);
        }

        result["componentType"] = JsonValue.Create((int)source.ComponentType);
        if (ShouldWriteBoolDefault(source.HasNormalized, source.Normalized, false))
        {
            result["normalized"] = JsonValue.Create(source.Normalized);
        }

        result["count"] = JsonValue.Create(source.Count);
        result["type"] = JsonValue.Create(GetAccessorTypeName(source.Type));
        if (source.Maximum != null)
        {
            result["max"] = WriteDoubleArray(source.Maximum);
        }

        if (source.Minimum != null)
        {
            result["min"] = WriteMinimumArray(source.Minimum);
        }

        if (source.Sparse != null)
        {
            result["sparse"] = WriteAccessorSparse(source.Sparse);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "accessor");
        return result;
    }

    internal JsonObject WriteAccessorSparse(GltfAccessorSparse source)
    {
        JsonObject result = new JsonObject();
        result["count"] = JsonValue.Create(source.Count);
        result["indices"] = WriteAccessorSparseIndices(source.Indices);
        result["values"] = WriteAccessorSparseValues(source.Values);
        AppendPropertyJson(result, source.PropertyJson, "accessor.sparse");
        return result;
    }

    internal JsonObject WriteAccessorSparseIndices(GltfAccessorSparseIndices source)
    {
        JsonObject result = new JsonObject();
        result["bufferView"] = JsonValue.Create(source.BufferView);
        if (ShouldWriteIntDefault(source.HasByteOffset, source.ByteOffset, 0))
        {
            result["byteOffset"] = JsonValue.Create(source.ByteOffset);
        }

        result["componentType"] = JsonValue.Create((int)source.ComponentType);
        AppendPropertyJson(result, source.PropertyJson, "accessor.sparse.indices");
        return result;
    }

    internal JsonObject WriteAccessorSparseValues(GltfAccessorSparseValues source)
    {
        JsonObject result = new JsonObject();
        result["bufferView"] = JsonValue.Create(source.BufferView);
        if (ShouldWriteIntDefault(source.HasByteOffset, source.ByteOffset, 0))
        {
            result["byteOffset"] = JsonValue.Create(source.ByteOffset);
        }

        AppendPropertyJson(result, source.PropertyJson, "accessor.sparse.values");
        return result;
    }

    internal JsonArray WriteAnimationCollection(List<GltfAnimation> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteAnimation(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteAnimation(GltfAnimation source)
    {
        JsonObject result = new JsonObject();
        result["channels"] = WriteAnimationChannelCollection(source.Channels);
        result["samplers"] = WriteAnimationSamplerCollection(source.Samplers);
        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "animation");
        return result;
    }

    internal JsonArray WriteAnimationChannelCollection(List<GltfAnimationChannel> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteAnimationChannel(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteAnimationChannel(GltfAnimationChannel source)
    {
        JsonObject result = new JsonObject();
        result["sampler"] = JsonValue.Create(source.Sampler);
        result["target"] = WriteAnimationTarget(source.Target);
        AppendPropertyJson(result, source.PropertyJson, "animation.channel");
        return result;
    }

    internal JsonObject WriteAnimationTarget(GltfAnimationTarget source)
    {
        JsonObject result = new JsonObject();
        if (source.HasNode)
        {
            result["node"] = JsonValue.Create(source.Node);
        }

        result["path"] = JsonValue.Create(GetAnimationPathName(source.Path));
        AppendPropertyJson(result, source.PropertyJson, "animation.channel.target");
        return result;
    }

    internal JsonArray WriteAnimationSamplerCollection(List<GltfAnimationSampler> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteAnimationSampler(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteAnimationSampler(GltfAnimationSampler source)
    {
        JsonObject result = new JsonObject();
        result["input"] = JsonValue.Create(source.Input);
        if (ShouldWriteIntDefault(source.HasInterpolation, (int)source.Interpolation, (int)GltfAnimationInterpolation.Linear))
        {
            result["interpolation"] = JsonValue.Create(GetAnimationInterpolationName(source.Interpolation));
        }

        result["output"] = JsonValue.Create(source.Output);
        AppendPropertyJson(result, source.PropertyJson, "animation.sampler");
        return result;
    }

    internal JsonArray WriteBufferCollection(List<GltfBuffer> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteBuffer(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteBuffer(GltfBuffer source)
    {
        JsonObject result = new JsonObject();
        if (source.Uri != null)
        {
            result["uri"] = JsonValue.Create(source.Uri);
        }

        result["byteLength"] = JsonValue.Create(source.ByteLength);
        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "buffer");
        return result;
    }

    internal JsonArray WriteBufferViewCollection(List<GltfBufferView> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteBufferView(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteBufferView(GltfBufferView source)
    {
        JsonObject result = new JsonObject();
        result["buffer"] = JsonValue.Create(source.Buffer);
        if (ShouldWriteIntDefault(source.HasByteOffset, source.ByteOffset, 0))
        {
            result["byteOffset"] = JsonValue.Create(source.ByteOffset);
        }

        result["byteLength"] = JsonValue.Create(source.ByteLength);
        if (source.HasByteStride)
        {
            result["byteStride"] = JsonValue.Create(source.ByteStride);
        }

        if (source.HasTarget)
        {
            result["target"] = JsonValue.Create((int)source.Target);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "bufferView");
        return result;
    }

    internal JsonArray WriteCameraCollection(List<GltfCamera> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteCamera(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteCamera(GltfCamera source)
    {
        JsonObject result = new JsonObject();
        result["type"] = JsonValue.Create(GetCameraTypeName(source.Type));
        if (source.Perspective != null)
        {
            result["perspective"] = WritePerspectiveCamera(source.Perspective);
        }

        if (source.Orthographic != null)
        {
            result["orthographic"] = WriteOrthographicCamera(source.Orthographic);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "camera");
        return result;
    }

    internal JsonObject WritePerspectiveCamera(GltfPerspectiveCamera source)
    {
        JsonObject result = new JsonObject();
        if (source.HasAspectRatio)
        {
            result["aspectRatio"] = JsonValue.Create(source.AspectRatio);
        }

        result["yfov"] = JsonValue.Create(source.VerticalFieldOfView);
        if (source.HasFarPlane)
        {
            result["zfar"] = JsonValue.Create(source.FarPlane);
        }

        result["znear"] = JsonValue.Create(source.NearPlane);
        AppendPropertyJson(result, source.PropertyJson, "camera.perspective");
        return result;
    }

    internal JsonObject WriteOrthographicCamera(GltfOrthographicCamera source)
    {
        JsonObject result = new JsonObject();
        result["xmag"] = JsonValue.Create(source.HorizontalMagnification);
        result["ymag"] = JsonValue.Create(source.VerticalMagnification);
        result["zfar"] = JsonValue.Create(source.FarPlane);
        result["znear"] = JsonValue.Create(source.NearPlane);
        AppendPropertyJson(result, source.PropertyJson, "camera.orthographic");
        return result;
    }

    internal JsonArray WriteImageCollection(List<GltfImage> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteImage(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteImage(GltfImage source)
    {
        JsonObject result = new JsonObject();
        if (source.Uri != null)
        {
            result["uri"] = JsonValue.Create(source.Uri);
        }

        if (source.MimeType != null)
        {
            result["mimeType"] = JsonValue.Create(source.MimeType);
        }

        if (source.HasBufferView)
        {
            result["bufferView"] = JsonValue.Create(source.BufferView);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "image");
        return result;
    }

    internal JsonArray WriteMaterialCollection(List<GltfMaterial> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteMaterial(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteMaterial(GltfMaterial source)
    {
        JsonObject result = new JsonObject();
        if (source.PbrMetallicRoughness != null)
        {
            result["pbrMetallicRoughness"] = WritePbrMetallicRoughness(source.PbrMetallicRoughness);
        }

        if (source.NormalTexture != null)
        {
            result["normalTexture"] = WriteNormalTextureInfo(source.NormalTexture);
        }

        if (source.OcclusionTexture != null)
        {
            result["occlusionTexture"] = WriteOcclusionTextureInfo(source.OcclusionTexture);
        }

        if (source.EmissiveTexture != null)
        {
            result["emissiveTexture"] = WriteTextureInfo(source.EmissiveTexture);
        }

        if (ShouldWriteDoubleArrayDefault(source.HasEmissiveFactor, source.EmissiveFactor, [0.0, 0.0, 0.0]))
        {
            result["emissiveFactor"] = WriteEmissiveFactor(source.EmissiveFactor);
        }

        if (ShouldWriteIntDefault(source.HasAlphaMode, (int)source.AlphaMode, (int)GltfAlphaMode.Opaque))
        {
            result["alphaMode"] = JsonValue.Create(GetAlphaModeName(source.AlphaMode));
        }

        if (ShouldWriteDoubleDefault(source.HasAlphaCutoff, source.AlphaCutoff, 0.5))
        {
            result["alphaCutoff"] = JsonValue.Create(source.AlphaCutoff);
        }

        if (ShouldWriteBoolDefault(source.HasDoubleSided, source.DoubleSided, false))
        {
            result["doubleSided"] = JsonValue.Create(source.DoubleSided);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "material");
        return result;
    }

    internal JsonObject WritePbrMetallicRoughness(GltfPbrMetallicRoughness source)
    {
        JsonObject result = new JsonObject();
        if (ShouldWriteDoubleArrayDefault(source.HasBaseColorFactor, source.BaseColorFactor, [1.0, 1.0, 1.0, 1.0]))
        {
            result["baseColorFactor"] = WriteBaseColorFactor(source.BaseColorFactor);
        }

        if (source.BaseColorTexture != null)
        {
            result["baseColorTexture"] = WriteBaseColorTextureInfo(source.BaseColorTexture);
        }

        if (ShouldWriteDoubleDefault(source.HasMetallicFactor, source.MetallicFactor, 1.0))
        {
            result["metallicFactor"] = JsonValue.Create(source.MetallicFactor);
        }

        if (ShouldWriteDoubleDefault(source.HasRoughnessFactor, source.RoughnessFactor, 1.0))
        {
            result["roughnessFactor"] = JsonValue.Create(source.RoughnessFactor);
        }

        if (source.MetallicRoughnessTexture != null)
        {
            result["metallicRoughnessTexture"] = WriteMetallicRoughnessTextureInfo(source.MetallicRoughnessTexture);
        }

        AppendPropertyJson(result, source.PropertyJson, "material.pbrMetallicRoughness");
        return result;
    }

    internal JsonObject WriteTextureInfo(GltfTextureInfo source)
    {
        JsonObject result = new JsonObject();
        result["index"] = JsonValue.Create(source.Index);
        if (ShouldWriteIntDefault(source.HasTextureCoordinate, source.TextureCoordinate, 0))
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        AppendPropertyJson(result, source.PropertyJson, "textureInfo");
        return result;
    }

    internal JsonObject WriteBaseColorTextureInfo(GltfTextureInfo source)
    {
        return WriteTextureInfo(source);
    }

    internal JsonObject WriteMetallicRoughnessTextureInfo(GltfTextureInfo source)
    {
        return WriteTextureInfo(source);
    }

    internal JsonObject WriteNormalTextureInfo(GltfNormalTextureInfo source)
    {
        JsonObject result = new JsonObject();
        result["index"] = JsonValue.Create(source.Index);
        if (ShouldWriteIntDefault(source.HasTextureCoordinate, source.TextureCoordinate, 0))
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        if (ShouldWriteDoubleDefault(source.HasScale, source.Scale, 1.0))
        {
            result["scale"] = JsonValue.Create(source.Scale);
        }

        AppendPropertyJson(result, source.PropertyJson, "textureInfo");
        return result;
    }

    internal JsonObject WriteOcclusionTextureInfo(GltfOcclusionTextureInfo source)
    {
        JsonObject result = new JsonObject();
        result["index"] = JsonValue.Create(source.Index);
        if (ShouldWriteIntDefault(source.HasTextureCoordinate, source.TextureCoordinate, 0))
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        if (ShouldWriteDoubleDefault(source.HasStrength, source.Strength, 1.0))
        {
            result["strength"] = JsonValue.Create(source.Strength);
        }

        AppendPropertyJson(result, source.PropertyJson, "textureInfo");
        return result;
    }

    internal JsonArray WriteMeshCollection(List<GltfMesh> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteMesh(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteMesh(GltfMesh source)
    {
        JsonObject result = new JsonObject();
        result["primitives"] = WriteMeshPrimitiveCollection(source.Primitives);
        if (source.Weights != null)
        {
            result["weights"] = WriteMeshWeightArray(source.Weights);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "mesh");
        return result;
    }

    internal JsonArray WriteMeshPrimitiveCollection(List<GltfMeshPrimitive> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteMeshPrimitive(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteMeshPrimitive(GltfMeshPrimitive source)
    {
        JsonObject result = new JsonObject();
        result["attributes"] = WriteAttributeDictionary(source.Attributes);
        if (source.HasIndices)
        {
            result["indices"] = JsonValue.Create(source.Indices);
        }

        if (source.HasMaterial)
        {
            result["material"] = JsonValue.Create(source.Material);
        }

        if (ShouldWriteIntDefault(source.HasMode, (int)source.Mode, (int)GltfPrimitiveMode.Triangles))
        {
            result["mode"] = JsonValue.Create((int)source.Mode);
        }

        if (source.Targets.Count > 0)
        {
            result["targets"] = WriteMorphTargetCollection(source.Targets);
        }

        AppendPropertyJson(result, source.PropertyJson, "mesh.primitive");
        return result;
    }

    internal JsonObject WriteAttributeDictionary(List<GltfAttribute> source)
    {
        List<GltfAttribute> attributes = new List<GltfAttribute>(source);
        attributes.Sort((left, right) => StringComparer.Ordinal.Compare(left.Semantic, right.Semantic));
        JsonObject result = new JsonObject();
        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            result[attribute.Semantic] = JsonValue.Create(attribute.Accessor);
            index++;
        }

        return result;
    }

    internal JsonArray WriteMorphTargetCollection(List<GltfMorphTarget> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteMorphTarget(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteMorphTarget(GltfMorphTarget source)
    {
        return WriteAttributeDictionary(source.Attributes);
    }

    internal JsonArray WriteNodeCollection(List<GltfNode> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteNode(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteNode(GltfNode source)
    {
        JsonObject result = new JsonObject();
        if (source.Children.Count > 0)
        {
            result["children"] = WriteIntList(source.Children);
        }

        if (source.HasCamera)
        {
            result["camera"] = JsonValue.Create(source.Camera);
        }

        if (source.HasSkin)
        {
            result["skin"] = JsonValue.Create(source.Skin);
        }

        if (source.HasMesh)
        {
            result["mesh"] = JsonValue.Create(source.Mesh);
        }

        if (ShouldWriteDoubleArrayDefault(source.HasMatrix, source.Matrix,
            [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]))
        {
            result["matrix"] = WriteNodeMatrix(source.Matrix);
        }

        if (ShouldWriteDoubleArrayDefault(source.HasRotation, source.Rotation, [0.0, 0.0, 0.0, 1.0]))
        {
            result["rotation"] = WriteNodeRotation(source.Rotation);
        }

        if (ShouldWriteDoubleArrayDefault(source.HasScale, source.Scale, [1.0, 1.0, 1.0]))
        {
            result["scale"] = WriteNodeScale(source.Scale);
        }

        if (ShouldWriteDoubleArrayDefault(source.HasTranslation, source.Translation, [0.0, 0.0, 0.0]))
        {
            result["translation"] = WriteNodeTranslation(source.Translation);
        }

        if (source.Weights != null)
        {
            result["weights"] = WriteNodeWeightArray(source.Weights);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "node");
        return result;
    }

    internal JsonArray WriteSamplerCollection(List<GltfSampler> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteSampler(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteSampler(GltfSampler source)
    {
        JsonObject result = new JsonObject();
        if (source.HasMagnificationFilter)
        {
            result["magFilter"] = JsonValue.Create((int)source.MagnificationFilter);
        }

        if (source.HasMinificationFilter)
        {
            result["minFilter"] = JsonValue.Create((int)source.MinificationFilter);
        }

        if (ShouldWriteIntDefault(source.HasWrapS, (int)source.WrapS, (int)GltfSamplerWrap.Repeat))
        {
            result["wrapS"] = JsonValue.Create((int)source.WrapS);
        }

        if (ShouldWriteIntDefault(source.HasWrapT, (int)source.WrapT, (int)GltfSamplerWrap.Repeat))
        {
            result["wrapT"] = JsonValue.Create((int)source.WrapT);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "sampler");
        return result;
    }

    internal JsonArray WriteSceneCollection(List<GltfScene> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteScene(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteScene(GltfScene source)
    {
        JsonObject result = new JsonObject();
        if (source.Nodes.Count > 0)
        {
            result["nodes"] = WriteIntList(source.Nodes);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "scene");
        return result;
    }

    internal JsonArray WriteSkinCollection(List<GltfSkin> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteSkin(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteSkin(GltfSkin source)
    {
        JsonObject result = new JsonObject();
        if (source.HasInverseBindMatrices)
        {
            result["inverseBindMatrices"] = JsonValue.Create(source.InverseBindMatrices);
        }

        if (source.HasSkeleton)
        {
            result["skeleton"] = JsonValue.Create(source.Skeleton);
        }

        result["joints"] = WriteIntList(source.Joints);
        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "skin");
        return result;
    }

    internal JsonArray WriteTextureCollection(List<GltfTexture> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(WriteTexture(source[index]));
            index++;
        }

        return result;
    }

    internal JsonObject WriteTexture(GltfTexture source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSampler)
        {
            result["sampler"] = JsonValue.Create(source.Sampler);
        }

        if (source.HasSource)
        {
            result["source"] = JsonValue.Create(source.Source);
        }

        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        AppendPropertyJson(result, source.PropertyJson, "texture");
        return result;
    }

    internal JsonArray WriteStringList(List<string> source)
    {
        JsonArray result = new JsonArray();
        List<string> values = new List<string>();
        int index = 0;
        while (index < source.Count)
        {
            values.Add(source[index]);
            index++;
        }

        if (Options.Deterministic)
        {
            values.Sort(StringComparer.Ordinal);
        }

        index = 0;
        while (index < values.Count)
        {
            result.Add(JsonValue.Create(values[index]));
            index++;
        }

        return result;
    }

    internal JsonArray WriteRequiredExtensionList(List<string> source)
    {
        return WriteStringList(source);
    }

    internal JsonArray WriteIntList(List<int> source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(JsonValue.Create(source[index]));
            index++;
        }

        return result;
    }

    internal JsonArray WriteDoubleArray(double[] source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (index < source.Length)
        {
            result.Add(JsonValue.Create(source[index]));
            index++;
        }

        return result;
    }

    internal JsonArray WriteMinimumArray(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteEmissiveFactor(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteBaseColorFactor(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteMeshWeightArray(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteNodeMatrix(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteNodeRotation(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteNodeScale(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteNodeTranslation(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal JsonArray WriteNodeWeightArray(double[] source)
    {
        return WriteDoubleArray(source);
    }

    internal void AppendPropertyJson(
        JsonObject result,
        GltfPropertyJson source,
        string objectLocation)
    {
        if (source.Extensions.Count > 0 || source.TypedExtensions.Count > 0)
        {
            JsonObject extensions = new JsonObject();
            List<string> names = GetPropertyExtensionNames(source);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                JsonNode? value = null;
                if (source.Extensions.ContainsKey(name))
                {
                    value = source.Extensions[name];
                }

                if (DispatchExtensionWrites
                    && (Options.ExtensionHandlers.Count > 0
                        || source.TypedExtensions.ContainsKey(name)
                        || ArchivedExtensions.IsArchivedExtension(name)))
                {
                    value = WriteHandledExtension(source, name, value, objectLocation);
                }

                extensions[name] = PrepareFreeFormValue(value);
                index++;
            }

            result["extensions"] = extensions;
        }

        if (source.HasExtras || source.Extras != null)
        {
            result["extras"] = PrepareFreeFormValue(source.Extras);
        }

        if (Options.PreserveUnknownMembers)
        {
            List<string> names = GetSortedKeys(source.UnknownMembers);
            int index = 0;
            while (index < names.Count)
            {
                string name = names[index];
                if (!result.ContainsKey(name) && name != "extensions" && name != "extras")
                {
                    result[name] = PrepareFreeFormValue(source.UnknownMembers[name]);
                }

                index++;
            }
        }
    }

    internal List<string> GetPropertyExtensionNames(GltfPropertyJson source)
    {
        List<string> result = GetSortedKeys(source.Extensions);
        foreach (KeyValuePair<string, GltfTypedExtensionData> member in source.TypedExtensions)
        {
            if (!result.Contains(member.Key))
            {
                result.Add(member.Key);
            }
        }

        if (Options.Deterministic)
        {
            result.Sort(StringComparer.Ordinal);
        }

        return result;
    }

    internal JsonNode? WriteHandledExtension(
        GltfPropertyJson source,
        string name,
        JsonNode? json,
        string objectLocation)
    {
        GltfExtensionContext context = new GltfExtensionContext();
        context.Document = Document;
        context.Name = name;
        context.ObjectLocation = objectLocation;
        context.Json = JsonValueCloner.CloneNode(json);
        if (Document != null)
        {
            context.Resources = Document.Resources;
        }

        if (source.TypedExtensions.ContainsKey(name))
        {
            context.TypedData = source.TypedExtensions[name];
        }

        GltfExtensionResult handled = ExtensionDispatcher.DispatchWriteExtension(
            context,
            Options.ExtensionHandlers);
        AppendExtensionResources(handled.Resources);
        AppendExtensionDiagnostics(handled.Diagnostics);
        if ((int)handled.Status > (int)ExtensionStatus)
        {
            ExtensionStatus = handled.Status;
        }

        if (handled.Status == GltfOperationStatus.Success && handled.Json != null)
        {
            return handled.Json;
        }

        return json;
    }

    internal void AppendExtensionResources(GltfResourceSet source)
    {
        int index = 0;
        while (index < source.Resources.Count)
        {
            GltfResource resource = source.Resources[index];
            ExtensionResources.Resources.Add(resource);
            if (resource.CacheKey != null
                && !ExtensionResources.Cache.ContainsKey(resource.CacheKey))
            {
                ExtensionResources.Cache[resource.CacheKey] = resource;
            }

            index++;
        }
    }

    internal void AppendExtensionDiagnostics(List<GltfValidationIssue> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            ExtensionDiagnostics.Add(source[index]);
            index++;
        }
    }

    internal void ApplyExtensionOutput(GltfWriteResult result)
    {
        result.Status = ExtensionStatus;
        int index = 0;
        while (index < ExtensionDiagnostics.Count)
        {
            result.Diagnostics.Add(ExtensionDiagnostics[index]);
            index++;
        }
    }

    internal JsonNode? PrepareFreeFormValue(JsonNode? source)
    {
        if (Options.Deterministic)
        {
            return FreeFormWriter.OrderValue(source);
        }

        return JsonValueCloner.CloneNode(source);
    }

    internal List<string> GetSortedKeys(Dictionary<string, JsonNode?> source)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            result.Add(member.Key);
        }

        if (Options.Deterministic)
        {
            result.Sort(StringComparer.Ordinal);
        }

        return result;
    }

    internal bool ShouldWriteIntDefault(bool wasPresent, int value, int defaultValue)
    {
        return wasPresent && (Options.PreserveExplicitDefaults || value != defaultValue);
    }

    internal bool ShouldWriteBoolDefault(bool wasPresent, bool value, bool defaultValue)
    {
        return wasPresent && (Options.PreserveExplicitDefaults || value != defaultValue);
    }

    internal bool ShouldWriteDoubleDefault(bool wasPresent, double value, double defaultValue)
    {
        return wasPresent && (Options.PreserveExplicitDefaults || value != defaultValue);
    }

    internal bool ShouldWriteDoubleArrayDefault(bool wasPresent, double[] value, double[] defaultValue)
    {
        if (!wasPresent)
        {
            return false;
        }

        if (Options.PreserveExplicitDefaults || value.Length != defaultValue.Length)
        {
            return true;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != defaultValue[index])
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal string GetAccessorTypeName(GltfAccessorType value)
    {
        switch (value)
        {
            case GltfAccessorType.Scalar:
                return "SCALAR";
            case GltfAccessorType.Vector2:
                return "VEC2";
            case GltfAccessorType.Vector3:
                return "VEC3";
            case GltfAccessorType.Vector4:
                return "VEC4";
            case GltfAccessorType.Matrix2:
                return "MAT2";
            case GltfAccessorType.Matrix3:
                return "MAT3";
            case GltfAccessorType.Matrix4:
                return "MAT4";
            default:
                throw new GltfWriteException($"Unknown accessor type {(int)value}.");
        }
    }

    internal string GetAnimationInterpolationName(GltfAnimationInterpolation value)
    {
        switch (value)
        {
            case GltfAnimationInterpolation.Linear:
                return "LINEAR";
            case GltfAnimationInterpolation.Step:
                return "STEP";
            case GltfAnimationInterpolation.CubicSpline:
                return "CUBICSPLINE";
            default:
                throw new GltfWriteException($"Unknown animation interpolation {(int)value}.");
        }
    }

    internal string GetAnimationPathName(GltfAnimationPath value)
    {
        switch (value)
        {
            case GltfAnimationPath.Translation:
                return "translation";
            case GltfAnimationPath.Rotation:
                return "rotation";
            case GltfAnimationPath.Scale:
                return "scale";
            case GltfAnimationPath.Weights:
                return "weights";
            case GltfAnimationPath.Pointer:
                return "pointer";
            default:
                throw new GltfWriteException($"Unknown animation path {(int)value}.");
        }
    }

    internal string GetCameraTypeName(GltfCameraType value)
    {
        switch (value)
        {
            case GltfCameraType.Perspective:
                return "perspective";
            case GltfCameraType.Orthographic:
                return "orthographic";
            default:
                throw new GltfWriteException($"Unknown camera type {(int)value}.");
        }
    }

    internal string GetAlphaModeName(GltfAlphaMode value)
    {
        switch (value)
        {
            case GltfAlphaMode.Opaque:
                return "OPAQUE";
            case GltfAlphaMode.Mask:
                return "MASK";
            case GltfAlphaMode.Blend:
                return "BLEND";
            default:
                throw new GltfWriteException($"Unknown alpha mode {(int)value}.");
        }
    }
}
