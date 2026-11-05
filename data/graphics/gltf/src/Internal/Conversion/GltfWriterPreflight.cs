namespace gltf;

internal class GltfWriterPreflight
{
    internal AccessorLayout AccessorLayout = new AccessorLayout();
    internal SparseAccessorDecoder SparseDecoder = new SparseAccessorDecoder();

    internal void ValidateDocument(GltfDocument document)
    {
        ValidateDocumentCollections(document);
        if (document.HasDefaultScene)
        {
            ValidateReference(document.DefaultScene, document.Scenes.Count, "/scene");
        }

        ValidateAccessors(document);
        ValidateAnimations(document);
        ValidateBuffers(document);
        ValidateBufferViews(document);
        ValidateCameras(document);
        ValidateImages(document);
        ValidateMaterials(document);
        ValidateMeshes(document);
        ValidateNodes(document);
        ValidateScenes(document);
        ValidateSkins(document);
        ValidateTextures(document);
    }

    internal void ValidateDocumentCollections(GltfDocument document)
    {
        if (document.Asset == null
            || document.Accessors == null
            || document.Animations == null
            || document.Buffers == null
            || document.BufferViews == null
            || document.Cameras == null
            || document.Images == null
            || document.Materials == null
            || document.Meshes == null
            || document.Nodes == null
            || document.Samplers == null
            || document.Scenes == null
            || document.Skins == null
            || document.Textures == null
            || document.ExtensionsUsed == null
            || document.ExtensionsRequired == null)
        {
            throw new GltfWriteException("The document contains a null required object or collection.");
        }
    }

    internal void ValidateAccessors(GltfDocument document)
    {
        int index = 0;
        while (index < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[index];
            string pointer = $"/accessors/{index}";
            if (accessor == null)
            {
                throw new GltfWriteException($"Object {pointer} is null.");
            }

            ValidateNonNegative(accessor.Count, $"{pointer}/count");
            int byteOffset = 0;
            if (accessor.HasByteOffset)
            {
                ValidateNonNegative(accessor.ByteOffset, $"{pointer}/byteOffset");
                byteOffset = accessor.ByteOffset;
            }

            GltfBufferView? bufferView = null;
            if (accessor.HasBufferView)
            {
                ValidateReference(accessor.BufferView, document.BufferViews.Count, $"{pointer}/bufferView");
                bufferView = document.BufferViews[accessor.BufferView];
            }

            ValidateAccessorRange(accessor, bufferView, byteOffset, pointer);

            if (accessor.Maximum != null)
            {
                ValidateFiniteArray(accessor.Maximum, $"{pointer}/max");
            }

            if (accessor.Minimum != null)
            {
                ValidateFiniteArray(accessor.Minimum, $"{pointer}/min");
            }

            if (accessor.Sparse != null)
            {
                ValidateAccessorSparse(accessor, accessor.Sparse, document, pointer);
            }

            index++;
        }
    }

    internal void ValidateAccessorRange(
        GltfAccessor accessor,
        GltfBufferView? bufferView,
        int byteOffset,
        string pointer)
    {
        AccessorElementLayout elementLayout = GetAccessorElementLayout(
            accessor.ComponentType,
            accessor.Type,
            pointer);
        if (!AccessorLayout.IsComponentOffsetAligned(elementLayout, byteOffset))
        {
            throw new GltfWriteException(
                $"Accessor byte offset {pointer}/byteOffset value {byteOffset} is not aligned to component byte length {elementLayout.ComponentByteLength}.");
        }

        BufferViewByteRange? bufferViewRange = null;
        if (bufferView != null)
        {
            ValidateBufferViewLayout(
                bufferView,
                $"/bufferViews/{accessor.BufferView}");
            bufferViewRange = AccessorLayout.CalculateBufferViewRange(bufferView);
        }

        AccessorByteRange range = AccessorLayout.CalculateAccessorByteRange(
            accessor,
            bufferViewRange);
        long absoluteByteOffset = range.FirstElementBufferByteOffset;
        if (absoluteByteOffset >= 0
            && !AccessorLayout.IsElementOffsetAligned(elementLayout, absoluteByteOffset))
        {
            throw new GltfWriteException(
                $"Accessor element {pointer} starts at absolute byte offset {absoluteByteOffset}, which is not aligned to {elementLayout.ElementByteAlignment} bytes.");
        }

        if (bufferViewRange != null
            && bufferViewRange.Target == GltfBufferViewTarget.ArrayBuffer
            && absoluteByteOffset % 4L != 0L)
        {
            throw new GltfWriteException(
                $"Vertex accessor element {pointer} starts at absolute byte offset {absoluteByteOffset}, which is not aligned to 4 bytes.");
        }

        if (bufferViewRange != null
            && bufferViewRange.HasByteStride
            && bufferViewRange.ByteStride < elementLayout.ElementByteLength)
        {
            throw new GltfWriteException(
                $"Buffer view /bufferViews/{accessor.BufferView} byteStride {bufferViewRange.ByteStride} is smaller than accessor element byte length {elementLayout.ElementByteLength}.");
        }

        if (bufferViewRange != null
            && range.AccessorByteEnd > bufferViewRange.ByteLength)
        {
            throw new GltfWriteException(
                $"Accessor byte range {pointer} ends at {range.AccessorByteEnd}, beyond buffer view {accessor.BufferView} byteLength {bufferViewRange.ByteLength}.");
        }

        ValidateByteRange(byteOffset, range.AccessorByteLength, pointer);
    }

    internal void ValidateAccessorSparse(
        GltfAccessor accessor,
        GltfAccessorSparse sparse,
        GltfDocument document,
        string pointer)
    {
        if (sparse.Indices == null || sparse.Values == null)
        {
            throw new GltfWriteException($"Object {pointer}/sparse contains a null required object.");
        }

        ValidateNonNegative(sparse.Count, $"{pointer}/sparse/count");
        if (sparse.Count < 1 || sparse.Count > accessor.Count)
        {
            throw new GltfWriteException(
                $"Sparse count {pointer}/sparse/count value {sparse.Count} must be positive and no greater than accessor count {accessor.Count}.");
        }

        ValidateReference(
            sparse.Indices.BufferView,
            document.BufferViews.Count,
            $"{pointer}/sparse/indices/bufferView");
        ValidateReference(
            sparse.Values.BufferView,
            document.BufferViews.Count,
            $"{pointer}/sparse/values/bufferView");
        GltfBufferView indicesBufferView = document.BufferViews[sparse.Indices.BufferView];
        GltfBufferView valuesBufferView = document.BufferViews[sparse.Values.BufferView];
        if (indicesBufferView == null || valuesBufferView == null)
        {
            throw new GltfWriteException(
                $"Object {pointer}/sparse references a null buffer view.");
        }

        ValidateSparsePayloadBufferView(
            indicesBufferView,
            $"{pointer}/sparse/indices");
        ValidateSparsePayloadBufferView(
            valuesBufferView,
            $"{pointer}/sparse/values");
        int indicesByteOffset = 0;
        if (sparse.Indices.HasByteOffset)
        {
            ValidateNonNegative(sparse.Indices.ByteOffset, $"{pointer}/sparse/indices/byteOffset");
            indicesByteOffset = sparse.Indices.ByteOffset;
        }

        int valuesByteOffset = 0;
        if (sparse.Values.HasByteOffset)
        {
            ValidateNonNegative(sparse.Values.ByteOffset, $"{pointer}/sparse/values/byteOffset");
            valuesByteOffset = sparse.Values.ByteOffset;
        }

        if (sparse.Indices.ComponentType != GltfComponentType.UnsignedByte
            && sparse.Indices.ComponentType != GltfComponentType.UnsignedShort
            && sparse.Indices.ComponentType != GltfComponentType.UnsignedInt)
        {
            throw new GltfWriteException(
                $"Sparse index component type {(int)sparse.Indices.ComponentType} at {pointer}/sparse/indices/componentType must be unsigned byte, unsigned short, or unsigned int.");
        }

        int indexByteLength = GetComponentByteLength(
            sparse.Indices.ComponentType,
            $"{pointer}/sparse/indices/componentType");
        AccessorElementLayout elementLayout = GetAccessorElementLayout(
            accessor.ComponentType,
            accessor.Type,
            pointer);
        if (indicesByteOffset % indexByteLength != 0)
        {
            throw new GltfWriteException(
                $"Sparse index byte offset {pointer}/sparse/indices/byteOffset value {indicesByteOffset} is not aligned to component byte length {indexByteLength}.");
        }

        long indicesAbsoluteOffset = indicesByteOffset;
        if (indicesBufferView.HasByteOffset)
        {
            indicesAbsoluteOffset += indicesBufferView.ByteOffset;
        }

        if (indicesAbsoluteOffset >= 0
            && indicesAbsoluteOffset % indexByteLength != 0)
        {
            throw new GltfWriteException(
                $"Sparse indices {pointer}/sparse/indices start at absolute byte offset {indicesAbsoluteOffset}, which is not aligned to {indexByteLength} bytes.");
        }

        if (!AccessorLayout.IsComponentOffsetAligned(elementLayout, valuesByteOffset))
        {
            throw new GltfWriteException(
                $"Sparse value byte offset {pointer}/sparse/values/byteOffset value {valuesByteOffset} is not aligned to component byte length {elementLayout.ComponentByteLength}.");
        }

        long valuesAbsoluteOffset = valuesByteOffset;
        if (valuesBufferView.HasByteOffset)
        {
            valuesAbsoluteOffset += valuesBufferView.ByteOffset;
        }

        if (valuesAbsoluteOffset >= 0
            && !AccessorLayout.IsElementOffsetAligned(
                elementLayout,
                valuesAbsoluteOffset))
        {
            throw new GltfWriteException(
                $"Sparse values {pointer}/sparse/values start at absolute byte offset {valuesAbsoluteOffset}, which is not aligned to {elementLayout.ElementByteAlignment} bytes.");
        }

        long indicesByteLength = (long)sparse.Count * indexByteLength;
        long valuesByteLength = (long)sparse.Count
            * elementLayout.ElementByteLength;
        ValidateByteRange(
            indicesByteOffset,
            indicesByteLength,
            $"{pointer}/sparse/indices");
        ValidateByteRange(
            valuesByteOffset,
            valuesByteLength,
            $"{pointer}/sparse/values");
        long indicesEnd = indicesByteOffset + indicesByteLength;
        if (indicesBufferView.ByteLength >= 0
            && indicesEnd > indicesBufferView.ByteLength)
        {
            throw new GltfWriteException(
                $"Sparse index byte range {pointer}/sparse/indices ends at {indicesEnd}, beyond buffer view {sparse.Indices.BufferView} byteLength {indicesBufferView.ByteLength}.");
        }

        long valuesEnd = valuesByteOffset + valuesByteLength;
        if (valuesBufferView.ByteLength >= 0
            && valuesEnd > valuesBufferView.ByteLength)
        {
            throw new GltfWriteException(
                $"Sparse value byte range {pointer}/sparse/values ends at {valuesEnd}, beyond buffer view {sparse.Values.BufferView} byteLength {valuesBufferView.ByteLength}.");
        }

        ValidateResolvedSparseIndices(
            accessor,
            sparse,
            indicesBufferView,
            document,
            pointer);
    }

    internal void ValidateSparsePayloadBufferView(
        GltfBufferView bufferView,
        string pointer)
    {
        if (bufferView.HasByteStride)
        {
            throw new GltfWriteException(
                $"Sparse payload buffer view referenced by {pointer} must not define byteStride.");
        }

        if (bufferView.HasTarget)
        {
            throw new GltfWriteException(
                $"Sparse payload buffer view referenced by {pointer} must not define target.");
        }
    }

    internal void ValidateResolvedSparseIndices(
        GltfAccessor accessor,
        GltfAccessorSparse sparse,
        GltfBufferView indicesBufferView,
        GltfDocument document,
        string pointer)
    {
        if (indicesBufferView.Buffer < 0
            || indicesBufferView.Buffer >= document.Buffers.Count)
        {
            return;
        }

        GltfBuffer buffer = document.Buffers[indicesBufferView.Buffer];
        if (buffer == null || !buffer.Resolved || buffer.ResolvedBytes == null)
        {
            return;
        }

        BufferViewByteRange bufferViewRange = AccessorLayout.CalculateBufferViewRange(
            indicesBufferView);
        AccessorByteRange indicesRange = SparseDecoder.CalculateSparseIndicesRange(
            sparse,
            bufferViewRange);
        if (indicesRange.FirstElementBufferByteOffset < 0
            || indicesRange.BufferByteEnd > buffer.ResolvedBytes.Length)
        {
            return;
        }

        uint[] indices = SparseDecoder.Decoder.DecodeIndices(
            buffer.ResolvedBytes,
            indicesRange);
        uint previous = 0U;
        int index = 0;
        while (index < indices.Length)
        {
            uint value = indices[index];
            if ((long)value >= accessor.Count)
            {
                throw new GltfWriteException(
                    $"Sparse index {value} at {pointer}/sparse/indices/{index} is outside accessor count {accessor.Count}.");
            }

            if (index > 0 && value <= previous)
            {
                throw new GltfWriteException(
                    $"Sparse index {value} at {pointer}/sparse/indices/{index} is not strictly greater than {previous}.");
            }

            previous = value;
            index++;
        }
    }

    internal void ValidateAnimations(GltfDocument document)
    {
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            string pointer = $"/animations/{animationIndex}";
            if (animation == null || animation.Channels == null || animation.Samplers == null)
            {
                throw new GltfWriteException($"Object {pointer} is null or incomplete.");
            }

            int samplerIndex = 0;
            while (samplerIndex < animation.Samplers.Count)
            {
                GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
                if (sampler == null)
                {
                    throw new GltfWriteException($"Object {pointer}/samplers/{samplerIndex} is null.");
                }

                ValidateReference(
                    sampler.Input,
                    document.Accessors.Count,
                    $"{pointer}/samplers/{samplerIndex}/input");
                ValidateReference(
                    sampler.Output,
                    document.Accessors.Count,
                    $"{pointer}/samplers/{samplerIndex}/output");
                samplerIndex++;
            }

            int channelIndex = 0;
            while (channelIndex < animation.Channels.Count)
            {
                GltfAnimationChannel channel = animation.Channels[channelIndex];
                string channelPointer = $"{pointer}/channels/{channelIndex}";
                if (channel == null || channel.Target == null)
                {
                    throw new GltfWriteException($"Object {channelPointer} is null or incomplete.");
                }

                ValidateReference(channel.Sampler, animation.Samplers.Count, $"{channelPointer}/sampler");
                if (channel.Target.HasNode)
                {
                    ValidateReference(channel.Target.Node, document.Nodes.Count, $"{channelPointer}/target/node");
                }

                channelIndex++;
            }

            animationIndex++;
        }
    }

    internal void ValidateBuffers(GltfDocument document)
    {
        int index = 0;
        while (index < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[index];
            if (buffer == null)
            {
                throw new GltfWriteException($"Object /buffers/{index} is null.");
            }

            ValidateNonNegative(buffer.ByteLength, $"/buffers/{index}/byteLength");
            index++;
        }
    }

    internal void ValidateBufferViews(GltfDocument document)
    {
        int index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView bufferView = document.BufferViews[index];
            string pointer = $"/bufferViews/{index}";
            if (bufferView == null)
            {
                throw new GltfWriteException($"Object {pointer} is null.");
            }

            ValidateReference(bufferView.Buffer, document.Buffers.Count, $"{pointer}/buffer");
            int byteOffset = 0;
            if (bufferView.HasByteOffset)
            {
                ValidateNonNegative(bufferView.ByteOffset, $"{pointer}/byteOffset");
                byteOffset = bufferView.ByteOffset;
            }

            ValidateNonNegative(bufferView.ByteLength, $"{pointer}/byteLength");
            ValidateBufferViewLayout(bufferView, pointer);
            ValidateByteRange(byteOffset, bufferView.ByteLength, pointer);
            BufferViewByteRange range = AccessorLayout.CalculateBufferViewRange(bufferView);
            GltfBuffer buffer = document.Buffers[bufferView.Buffer];
            if (buffer != null && range.ByteEnd > buffer.ByteLength)
            {
                throw new GltfWriteException(
                    $"Buffer view byte range {pointer} ends at {range.ByteEnd}, beyond buffer {bufferView.Buffer} byteLength {buffer.ByteLength}.");
            }

            index++;
        }
    }

    internal void ValidateBufferViewLayout(GltfBufferView bufferView, string pointer)
    {
        if (bufferView.HasTarget
            && !AccessorLayout.IsKnownBufferViewTarget(bufferView.Target))
        {
            throw new GltfWriteException(
                $"Unknown buffer view target {(int)bufferView.Target} at {pointer}/target.");
        }

        if (bufferView.HasByteStride)
        {
            ValidateNonNegative(bufferView.ByteStride, $"{pointer}/byteStride");
            if (bufferView.ByteStride < 4 || bufferView.ByteStride > 252)
            {
                throw new GltfWriteException(
                    $"Buffer view {pointer} byteStride {bufferView.ByteStride} is outside the supported range 4 through 252.");
            }

            if (bufferView.ByteStride % 4 != 0)
            {
                throw new GltfWriteException(
                    $"Buffer view {pointer} byteStride {bufferView.ByteStride} is not a multiple of 4.");
            }

            if (bufferView.HasTarget
                && bufferView.Target == GltfBufferViewTarget.ElementArrayBuffer)
            {
                throw new GltfWriteException(
                    $"Element array buffer view {pointer} must not define byteStride.");
            }
        }
    }

    internal void ValidateCameras(GltfDocument document)
    {
        int index = 0;
        while (index < document.Cameras.Count)
        {
            GltfCamera camera = document.Cameras[index];
            string pointer = $"/cameras/{index}";
            if (camera == null)
            {
                throw new GltfWriteException($"Object {pointer} is null.");
            }

            if (camera.Perspective != null)
            {
                if (camera.Perspective.HasAspectRatio)
                {
                    ValidateFinite(camera.Perspective.AspectRatio, $"{pointer}/perspective/aspectRatio");
                }

                ValidateFinite(camera.Perspective.VerticalFieldOfView, $"{pointer}/perspective/yfov");
                if (camera.Perspective.HasFarPlane)
                {
                    ValidateFinite(camera.Perspective.FarPlane, $"{pointer}/perspective/zfar");
                }

                ValidateFinite(camera.Perspective.NearPlane, $"{pointer}/perspective/znear");
            }

            if (camera.Orthographic != null)
            {
                ValidateFinite(camera.Orthographic.HorizontalMagnification, $"{pointer}/orthographic/xmag");
                ValidateFinite(camera.Orthographic.VerticalMagnification, $"{pointer}/orthographic/ymag");
                ValidateFinite(camera.Orthographic.FarPlane, $"{pointer}/orthographic/zfar");
                ValidateFinite(camera.Orthographic.NearPlane, $"{pointer}/orthographic/znear");
            }

            index++;
        }
    }

    internal void ValidateImages(GltfDocument document)
    {
        int index = 0;
        while (index < document.Images.Count)
        {
            GltfImage image = document.Images[index];
            if (image == null)
            {
                throw new GltfWriteException($"Object /images/{index} is null.");
            }

            if (image.HasBufferView)
            {
                ValidateReference(image.BufferView, document.BufferViews.Count, $"/images/{index}/bufferView");
            }

            index++;
        }
    }

    internal void ValidateMaterials(GltfDocument document)
    {
        int index = 0;
        while (index < document.Materials.Count)
        {
            GltfMaterial material = document.Materials[index];
            string pointer = $"/materials/{index}";
            if (material == null)
            {
                throw new GltfWriteException($"Object {pointer} is null.");
            }

            if (material.HasEmissiveFactor)
            {
                ValidateFiniteArray(material.EmissiveFactor, $"{pointer}/emissiveFactor");
            }

            if (material.HasAlphaCutoff)
            {
                ValidateFinite(material.AlphaCutoff, $"{pointer}/alphaCutoff");
            }

            if (material.PbrMetallicRoughness != null)
            {
                ValidatePbrMaterial(material.PbrMetallicRoughness, document.Textures.Count, pointer);
            }

            if (material.NormalTexture != null)
            {
                ValidateReference(material.NormalTexture.Index, document.Textures.Count, $"{pointer}/normalTexture/index");
                if (material.NormalTexture.HasScale)
                {
                    ValidateFinite(material.NormalTexture.Scale, $"{pointer}/normalTexture/scale");
                }
            }

            if (material.OcclusionTexture != null)
            {
                ValidateReference(material.OcclusionTexture.Index, document.Textures.Count, $"{pointer}/occlusionTexture/index");
                if (material.OcclusionTexture.HasStrength)
                {
                    ValidateFinite(material.OcclusionTexture.Strength, $"{pointer}/occlusionTexture/strength");
                }
            }

            if (material.EmissiveTexture != null)
            {
                ValidateReference(material.EmissiveTexture.Index, document.Textures.Count, $"{pointer}/emissiveTexture/index");
            }

            index++;
        }
    }

    internal void ValidatePbrMaterial(
        GltfPbrMetallicRoughness pbr,
        int textureCount,
        string materialPointer)
    {
        string pointer = $"{materialPointer}/pbrMetallicRoughness";
        if (pbr.HasBaseColorFactor)
        {
            ValidateFiniteArray(pbr.BaseColorFactor, $"{pointer}/baseColorFactor");
        }

        if (pbr.HasMetallicFactor)
        {
            ValidateFinite(pbr.MetallicFactor, $"{pointer}/metallicFactor");
        }

        if (pbr.HasRoughnessFactor)
        {
            ValidateFinite(pbr.RoughnessFactor, $"{pointer}/roughnessFactor");
        }

        if (pbr.BaseColorTexture != null)
        {
            ValidateReference(pbr.BaseColorTexture.Index, textureCount, $"{pointer}/baseColorTexture/index");
        }

        if (pbr.MetallicRoughnessTexture != null)
        {
            ValidateReference(
                pbr.MetallicRoughnessTexture.Index,
                textureCount,
                $"{pointer}/metallicRoughnessTexture/index");
        }
    }

    internal void ValidateMeshes(GltfDocument document)
    {
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            string pointer = $"/meshes/{meshIndex}";
            if (mesh == null || mesh.Primitives == null)
            {
                throw new GltfWriteException($"Object {pointer} is null or incomplete.");
            }

            if (mesh.Weights != null)
            {
                ValidateFiniteArray(mesh.Weights, $"{pointer}/weights");
            }

            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                string primitivePointer = $"{pointer}/primitives/{primitiveIndex}";
                if (primitive == null || primitive.Attributes == null || primitive.Targets == null)
                {
                    throw new GltfWriteException($"Object {primitivePointer} is null or incomplete.");
                }

                ValidateAttributes(primitive.Attributes, document.Accessors.Count, $"{primitivePointer}/attributes");
                if (primitive.HasIndices)
                {
                    ValidateReference(primitive.Indices, document.Accessors.Count, $"{primitivePointer}/indices");
                }

                if (primitive.HasMaterial)
                {
                    ValidateReference(primitive.Material, document.Materials.Count, $"{primitivePointer}/material");
                }

                int targetIndex = 0;
                while (targetIndex < primitive.Targets.Count)
                {
                    GltfMorphTarget target = primitive.Targets[targetIndex];
                    if (target == null || target.Attributes == null)
                    {
                        throw new GltfWriteException($"Object {primitivePointer}/targets/{targetIndex} is null or incomplete.");
                    }

                    ValidateAttributes(
                        target.Attributes,
                        document.Accessors.Count,
                        $"{primitivePointer}/targets/{targetIndex}");
                    targetIndex++;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }
    }

    internal void ValidateAttributes(
        System.Collections.Generic.List<GltfAttribute> attributes,
        int accessorCount,
        string pointer)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            if (attribute == null)
            {
                throw new GltfWriteException($"Object {pointer}/{index} is null.");
            }

            ValidateReference(attribute.Accessor, accessorCount, $"{pointer}/{attribute.Semantic}");
            index++;
        }
    }

    internal void ValidateNodes(GltfDocument document)
    {
        int index = 0;
        while (index < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[index];
            string pointer = $"/nodes/{index}";
            if (node == null || node.Children == null)
            {
                throw new GltfWriteException($"Object {pointer} is null or incomplete.");
            }

            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                ValidateReference(node.Children[childIndex], document.Nodes.Count, $"{pointer}/children/{childIndex}");
                childIndex++;
            }

            if (node.HasCamera)
            {
                ValidateReference(node.Camera, document.Cameras.Count, $"{pointer}/camera");
            }

            if (node.HasSkin)
            {
                ValidateReference(node.Skin, document.Skins.Count, $"{pointer}/skin");
            }

            if (node.HasMesh)
            {
                ValidateReference(node.Mesh, document.Meshes.Count, $"{pointer}/mesh");
            }

            if (node.HasMatrix)
            {
                ValidateFiniteArray(node.Matrix, $"{pointer}/matrix");
            }

            if (node.HasRotation)
            {
                ValidateFiniteArray(node.Rotation, $"{pointer}/rotation");
            }

            if (node.HasScale)
            {
                ValidateFiniteArray(node.Scale, $"{pointer}/scale");
            }

            if (node.HasTranslation)
            {
                ValidateFiniteArray(node.Translation, $"{pointer}/translation");
            }

            if (node.Weights != null)
            {
                ValidateFiniteArray(node.Weights, $"{pointer}/weights");
            }

            index++;
        }
    }

    internal void ValidateScenes(GltfDocument document)
    {
        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            GltfScene scene = document.Scenes[sceneIndex];
            string pointer = $"/scenes/{sceneIndex}";
            if (scene == null || scene.Nodes == null)
            {
                throw new GltfWriteException($"Object {pointer} is null or incomplete.");
            }

            int nodeIndex = 0;
            while (nodeIndex < scene.Nodes.Count)
            {
                ValidateReference(scene.Nodes[nodeIndex], document.Nodes.Count, $"{pointer}/nodes/{nodeIndex}");
                nodeIndex++;
            }

            sceneIndex++;
        }
    }

    internal void ValidateSkins(GltfDocument document)
    {
        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            string pointer = $"/skins/{skinIndex}";
            if (skin == null || skin.Joints == null)
            {
                throw new GltfWriteException($"Object {pointer} is null or incomplete.");
            }

            if (skin.HasInverseBindMatrices)
            {
                ValidateReference(
                    skin.InverseBindMatrices,
                    document.Accessors.Count,
                    $"{pointer}/inverseBindMatrices");
            }

            if (skin.HasSkeleton)
            {
                ValidateReference(skin.Skeleton, document.Nodes.Count, $"{pointer}/skeleton");
            }

            int jointIndex = 0;
            while (jointIndex < skin.Joints.Count)
            {
                ValidateReference(skin.Joints[jointIndex], document.Nodes.Count, $"{pointer}/joints/{jointIndex}");
                jointIndex++;
            }

            skinIndex++;
        }
    }

    internal void ValidateTextures(GltfDocument document)
    {
        int index = 0;
        while (index < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[index];
            string pointer = $"/textures/{index}";
            if (texture == null)
            {
                throw new GltfWriteException($"Object {pointer} is null.");
            }

            if (texture.HasSampler)
            {
                ValidateReference(texture.Sampler, document.Samplers.Count, $"{pointer}/sampler");
            }

            if (texture.HasSource)
            {
                ValidateReference(texture.Source, document.Images.Count, $"{pointer}/source");
            }

            index++;
        }
    }

    internal int GetAccessorElementByteLength(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        string pointer)
    {
        return GetAccessorElementLayout(
            componentType,
            accessorType,
            pointer).ElementByteLength;
    }

    internal AccessorElementLayout GetAccessorElementLayout(
        GltfComponentType componentType,
        GltfAccessorType accessorType,
        string pointer)
    {
        if (!AccessorLayout.IsKnownComponentType(componentType))
        {
            throw new GltfWriteException(
                $"Unknown accessor component type {(int)componentType} at {pointer}/componentType.");
        }

        if (!AccessorLayout.IsKnownAccessorType(accessorType))
        {
            throw new GltfWriteException(
                $"Unknown accessor type {(int)accessorType} at {pointer}/type.");
        }

        return AccessorLayout.CalculateElementLayout(
            componentType,
            accessorType);
    }

    internal int GetComponentByteLength(GltfComponentType componentType, string pointer)
    {
        if (!AccessorLayout.IsKnownComponentType(componentType))
        {
            throw new GltfWriteException(
                $"Unknown accessor component type {(int)componentType} at {pointer}.");
        }

        return AccessorLayout.GetComponentByteLength(componentType);
    }

    internal void ValidateReference(int value, int count, string pointer)
    {
        if (value < 0 || value >= count)
        {
            throw new GltfWriteException(
                $"Reference {pointer} index {value} is outside the target collection of length {count}.");
        }
    }

    internal void ValidateFiniteArray(double[] values, string pointer)
    {
        if (values == null)
        {
            throw new GltfWriteException($"Numeric array {pointer} is null.");
        }

        int index = 0;
        while (index < values.Length)
        {
            ValidateFinite(values[index], $"{pointer}/{index}");
            index++;
        }
    }

    internal void ValidateFinite(double value, string pointer)
    {
        if (!FiniteNumber.IsFiniteDouble(value))
        {
            throw new GltfWriteException($"Numeric value {pointer} must be finite.");
        }
    }

    internal void ValidateNonNegative(int value, string pointer)
    {
        if (value < 0)
        {
            throw new GltfWriteException($"Numeric value {pointer} must not be negative.");
        }
    }

    internal void ValidateByteRange(long offset, long byteLength, string pointer)
    {
        if (offset < 0L || byteLength < 0L)
        {
            throw new GltfWriteException($"Byte range {pointer} must not be negative.");
        }

        long end = offset + byteLength;
        if (end > int.MaxValue)
        {
            throw new GltfWriteException(
                $"Byte range {pointer} exceeds the supported signed byte length.");
        }
    }
}
