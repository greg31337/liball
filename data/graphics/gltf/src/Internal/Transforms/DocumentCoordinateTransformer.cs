using System.Collections.Generic;

namespace gltf;

internal class DocumentCoordinateTransformer
{
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal CoordinateConverter Converter = new CoordinateConverter();
    internal DocumentVertexTransformer Vertices = new DocumentVertexTransformer();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal List<AccessorTransformCacheEntry> Cache =
        new List<AccessorTransformCacheEntry>();
    internal List<int> SourceBufferViews = new List<int>();

    internal bool ConvertDocumentCoordinates(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        if (!CanConvertCoordinateDocument(document))
        {
            result.Status = GltfOperationStatus.Skipped;
            result.Reason = "Coordinate conversion skipped unsafe extension or pointer-animation state.";
            return false;
        }

        Cache.Clear();
        SourceBufferViews.Clear();
        bool changed = ConvertGeometryAccessors(document, conversion, result);
        changed = ConvertNodeTransforms(document, conversion, result) || changed;
        changed = ConvertAnimationAccessors(document, conversion, result) || changed;
        changed = ConvertSkinAccessors(document, conversion, result) || changed;
        changed = ConvertCameraValues(document, conversion, result) || changed;
        if (conversion.ReversesOrientation)
        {
            changed = FlipDocumentWinding(document, result, true) || changed;
        }

        if (changed)
        {
            Targets.ClearUnusedTargets(document, result, SourceBufferViews);
        }

        return changed;
    }

    internal bool FlipDocumentWinding(
        GltfDocument document,
        GltfTransformStepResult result,
        bool tangentAlreadyConverted)
    {
        if (!tangentAlreadyConverted)
        {
            SourceBufferViews.Clear();
        }

        Vertices.SourceBufferViews.Clear();
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                if (IsTriangleTopology(primitive.Mode)
                    && Vertices.IsCompatiblePrimitive(primitive))
                {
                    PrimitiveVertexData? data = Vertices.ReadPrimitiveData(
                        document,
                        primitive);
                    if (data != null)
                    {
                        uint[] converted = Converter.ConvertPrimitiveIndices(
                            data.Indices,
                            primitive.Mode,
                            Converter.CreateAxisUnitConversion(
                                System.Numerics.Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f),
                                1.0));
                        Vertices.RecordSourceBufferViews(document, data);
                        int sourceAccessor = data.HasIndices
                            ? data.IndexAccessor
                            : GltfConstants.AbsentIndex;
                        GltfComponentType componentType = data.HasIndices
                            ? data.IndexComponentType
                            : Vertices.SelectIndexComponentType(data.VertexCount);
                        primitive.Indices = Vertices.WriteIndexAccessor(
                            document,
                            converted,
                            componentType,
                            sourceAccessor);
                        primitive.HasIndices = true;
                        if (!tangentAlreadyConverted)
                        {
                            FlipPrimitiveTangent(document, primitive);
                        }

                        result.ChangedObjects.Add(
                            $"meshes/{meshIndex}/primitives/{primitiveIndex}");
                        changed = true;
                    }
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        if (changed)
        {
            int viewIndex = 0;
            while (viewIndex < Vertices.SourceBufferViews.Count)
            {
                int view = Vertices.SourceBufferViews[viewIndex];
                if (!SourceBufferViews.Contains(view))
                {
                    SourceBufferViews.Add(view);
                }

                viewIndex++;
            }

            Targets.ClearUnusedTargets(document, result, SourceBufferViews);
        }

        return changed;
    }

    internal bool CanConvertCoordinateDocument(GltfDocument document)
    {
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfPropertyJson property = document.Nodes[nodeIndex].PropertyJson;
            if (property.Extensions.Count > 0
                || property.TypedExtensions.ContainsKey(
                    GltfExtensionNames.ExtMeshGpuInstancing))
            {
                return false;
            }

            nodeIndex++;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                if (!Vertices.IsCompatiblePrimitive(primitive))
                {
                    return false;
                }

                if (Vertices.ReadPrimitiveData(document, primitive) == null)
                {
                    return false;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            int channelIndex = 0;
            while (channelIndex < animation.Channels.Count)
            {
                if (animation.Channels[channelIndex].Target.Path
                    == GltfAnimationPath.Pointer)
                {
                    return false;
                }

                channelIndex++;
            }

            animationIndex++;
        }

        return true;
    }

    internal bool ConvertGeometryAccessors(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                int attributeIndex = 0;
                while (attributeIndex < primitive.Attributes.Count)
                {
                    GltfAttribute attribute = primitive.Attributes[attributeIndex];
                    int kind = GetGeometryTransformKind(attribute.Semantic, false);
                    if (kind != 0)
                    {
                        attribute.Accessor = TransformCoordinateAccessor(
                            document,
                            attribute.Accessor,
                            kind,
                            conversion);
                        changed = true;
                    }

                    attributeIndex++;
                }

                int targetIndex = 0;
                while (targetIndex < primitive.Targets.Count)
                {
                    GltfMorphTarget target = primitive.Targets[targetIndex];
                    attributeIndex = 0;
                    while (attributeIndex < target.Attributes.Count)
                    {
                        GltfAttribute attribute = target.Attributes[attributeIndex];
                        int kind = GetGeometryTransformKind(attribute.Semantic, true);
                        if (kind != 0)
                        {
                            attribute.Accessor = TransformCoordinateAccessor(
                                document,
                                attribute.Accessor,
                                kind,
                                conversion);
                            changed = true;
                        }

                        attributeIndex++;
                    }

                    targetIndex++;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        if (changed)
        {
            result.ChangedObjects.Add("meshes");
        }

        return changed;
    }

    internal int GetGeometryTransformKind(string semantic, bool morphTarget)
    {
        if (semantic == "POSITION")
        {
            return 1;
        }

        if (semantic == "NORMAL" || (morphTarget && semantic == "TANGENT"))
        {
            return 2;
        }

        if (!morphTarget && semantic == "TANGENT")
        {
            return 3;
        }

        return 0;
    }

    internal int TransformCoordinateAccessor(
        GltfDocument document,
        int accessorIndex,
        int kind,
        CoordinateConversion conversion)
    {
        int cached = FindCachedAccessor(accessorIndex, kind);
        if (cached != GltfConstants.AbsentIndex)
        {
            return cached;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        request.ApplyNormalization = true;
        GltfAccessorData decoded = Accessors.ReadFloatingData(request);
        if (decoded.Status != GltfOperationStatus.Success)
        {
            throw new GltfUnsupportedFeatureException(
                $"Accessor {accessorIndex} cannot be converted as coordinate data.");
        }

        double[] values = decoded.FloatingValues;
        if (kind == 1)
        {
            values = Converter.ConvertPositionValues(values, conversion);
        }
        else if (kind == 2)
        {
            values = Converter.ConvertNormalValues(values, conversion);
        }
        else
        {
            values = Converter.ConvertTangentValues(values, conversion);
        }

        int created = WriteFloatingAccessor(document, accessorIndex, values, accessor.Type);
        AddCachedAccessor(accessorIndex, kind, created);
        return created;
    }

    internal bool ConvertNodeTransforms(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            bool nodeChanged = false;
            if (node.HasMatrix)
            {
                node.Matrix = Converter.ConvertNodeMatrixValues(node.Matrix, conversion);
                changed = true;
                nodeChanged = true;
            }
            else
            {
                if (node.HasTranslation)
                {
                    node.Translation = Converter.ConvertPositionValues(
                        node.Translation,
                        conversion);
                    changed = true;
                    nodeChanged = true;
                }

                if (node.HasRotation)
                {
                    node.Rotation = Converter.ConvertRotationAnimationValues(
                        node.Rotation,
                        conversion);
                    changed = true;
                    nodeChanged = true;
                }

                if (node.HasScale)
                {
                    node.Scale = Converter.ConvertScaleAnimationValues(
                        node.Scale,
                        conversion);
                    changed = true;
                    nodeChanged = true;
                }
            }

            if (nodeChanged)
            {
                result.ChangedObjects.Add($"nodes/{nodeIndex}");
            }

            nodeIndex++;
        }

        return changed;
    }

    internal bool ConvertAnimationAccessors(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            int channelIndex = 0;
            while (channelIndex < animation.Channels.Count)
            {
                GltfAnimationChannel channel = animation.Channels[channelIndex];
                if (channel.Sampler < 0 || channel.Sampler >= animation.Samplers.Count)
                {
                    throw new GltfUnsupportedFeatureException(
                        "Animation conversion requires valid sampler references.");
                }

                GltfAnimationSampler source = animation.Samplers[channel.Sampler];
                GltfAccessor accessor = document.Accessors[source.Output];
                GltfAccessorReadRequest request = new GltfAccessorReadRequest();
                request.Document = document;
                request.Accessor = source.Output;
                request.ApplyNormalization = true;
                GltfAccessorData decoded = Accessors.ReadFloatingData(request);
                if (decoded.Status != GltfOperationStatus.Success)
                {
                    throw new GltfUnsupportedFeatureException(
                        "Animation output accessor cannot be converted.");
                }

                double[] values = Converter.ConvertAnimationValues(
                    channel.Target.Path,
                    decoded.FloatingValues,
                    conversion);
                int output = WriteFloatingAccessor(
                    document,
                    source.Output,
                    values,
                    accessor.Type);
                GltfAnimationSampler replacement = CloneAnimationSampler(source);
                replacement.Output = output;
                channel.Sampler = animation.Samplers.Count;
                animation.Samplers.Add(replacement);
                result.ChangedObjects.Add(
                    $"animations/{animationIndex}/channels/{channelIndex}");
                changed = true;
                channelIndex++;
            }

            animationIndex++;
        }

        return changed;
    }

    internal GltfAnimationSampler CloneAnimationSampler(GltfAnimationSampler source)
    {
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        return (GltfAnimationSampler)(Cloner.CloneValue(source)
            ?? new GltfAnimationSampler());
    }

    internal bool ConvertSkinAccessors(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            if (skin.HasInverseBindMatrices)
            {
                int sourceIndex = skin.InverseBindMatrices;
                GltfAccessor source = document.Accessors[sourceIndex];
                GltfAccessorReadRequest request = new GltfAccessorReadRequest();
                request.Document = document;
                request.Accessor = sourceIndex;
                GltfAccessorData decoded = Accessors.ReadMatrixData(request);
                if (decoded.Status != GltfOperationStatus.Success)
                {
                    throw new GltfUnsupportedFeatureException(
                        "Inverse-bind accessor cannot be converted.");
                }

                double[] values = Converter.ConvertInverseBindMatrices(
                    decoded.MatrixValues,
                    conversion);
                AccessorEncodingResult encoding = Encoder.EncodeMatrixAccessor(
                    values,
                    source.ComponentType,
                    source.Type,
                    source.Normalized);
                AccessorCreationResult created = Encoder.CreateAccessorStorage(
                    document,
                    encoding,
                    false);
                Vertices.CopyAccessorMetadata(
                    source,
                    document.Accessors[created.Accessor]);
                RecordSourceView(document, sourceIndex);
                skin.InverseBindMatrices = created.Accessor;
                result.ChangedObjects.Add($"skins/{skinIndex}");
                changed = true;
            }

            skinIndex++;
        }

        return changed;
    }

    internal bool ConvertCameraValues(
        GltfDocument document,
        CoordinateConversion conversion,
        GltfTransformStepResult result)
    {
        if (conversion.UnitScale == 1.0)
        {
            return false;
        }

        int cameraIndex = 0;
        while (cameraIndex < document.Cameras.Count)
        {
            GltfCamera camera = document.Cameras[cameraIndex];
            if (camera.Perspective != null)
            {
                Converter.ConvertPerspectiveCameraParameters(
                    camera.Perspective,
                    conversion);
            }

            if (camera.Orthographic != null)
            {
                Converter.ConvertOrthographicCameraParameters(
                    camera.Orthographic,
                    conversion);
            }

            result.ChangedObjects.Add($"cameras/{cameraIndex}");
            cameraIndex++;
        }

        return document.Cameras.Count > 0;
    }

    internal void FlipPrimitiveTangent(
        GltfDocument document,
        GltfMeshPrimitive primitive)
    {
        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute.Semantic == "TANGENT")
            {
                int sourceIndex = attribute.Accessor;
                GltfAccessor source = document.Accessors[sourceIndex];
                GltfAccessorReadRequest request = new GltfAccessorReadRequest();
                request.Document = document;
                request.Accessor = sourceIndex;
                request.ApplyNormalization = true;
                GltfAccessorData decoded = Accessors.ReadFloatingData(request);
                if (decoded.Status != GltfOperationStatus.Success)
                {
                    throw new GltfUnsupportedFeatureException(
                        "Tangent accessor cannot be flipped.");
                }

                double[] values = decoded.FloatingValues;
                int valueIndex = 3;
                while (valueIndex < values.Length)
                {
                    values[valueIndex] = -values[valueIndex];
                    valueIndex += 4;
                }

                attribute.Accessor = WriteFloatingAccessor(
                    document,
                    sourceIndex,
                    values,
                    source.Type);
            }

            attributeIndex++;
        }
    }

    internal bool IsTriangleTopology(GltfPrimitiveMode mode)
    {
        return mode == GltfPrimitiveMode.Triangles
            || mode == GltfPrimitiveMode.TriangleStrip
            || mode == GltfPrimitiveMode.TriangleFan;
    }

    internal int WriteFloatingAccessor(
        GltfDocument document,
        int sourceIndex,
        double[] values,
        GltfAccessorType type)
    {
        GltfAccessor source = document.Accessors[sourceIndex];
        AccessorEncodingResult encoding = Encoder.EncodeFloatingAccessor(
            values,
            source.ComponentType,
            type,
            source.Normalized);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        Vertices.CopyAccessorMetadata(source, document.Accessors[created.Accessor]);
        RecordSourceView(document, sourceIndex);
        return created.Accessor;
    }

    internal int FindCachedAccessor(int source, int kind)
    {
        int index = 0;
        while (index < Cache.Count)
        {
            if (Cache[index].Source == source && Cache[index].Kind == kind)
            {
                return Cache[index].Result;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal void AddCachedAccessor(int source, int kind, int result)
    {
        AccessorTransformCacheEntry entry = new AccessorTransformCacheEntry();
        entry.Source = source;
        entry.Kind = kind;
        entry.Result = result;
        Cache.Add(entry);
    }

    internal void RecordSourceView(GltfDocument document, int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.HasBufferView
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count
            && !SourceBufferViews.Contains(accessor.BufferView))
        {
            SourceBufferViews.Add(accessor.BufferView);
        }
    }
}
