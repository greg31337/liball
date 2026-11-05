using System;
using System.Collections.Generic;

namespace gltf;

internal class MeshAttributeValidationState
{
    internal bool HasVertexCount;
    internal int VertexCount;
    internal bool HasPosition;
    internal bool HasNormal;
    internal bool HasTangent;
    internal List<int> WeightAccessors = new List<int>();
}

internal class MeshPrimitiveValidator
{
    internal AttributeSemanticParser SemanticParser = new AttributeSemanticParser();
    internal AccessorCodecEngine AccessorCodec = new AccessorCodecEngine();
    internal AccessorLayout AccessorLayout = new AccessorLayout();
    internal MeshQuantization Quantization = new MeshQuantization();
    internal JsonPointerBuilder PointerBuilder = new JsonPointerBuilder();

    internal GltfValidationReport ValidateDocumentMeshes(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        if (document.Meshes == null)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The mesh collection is missing.",
                "/meshes");
            return report;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            string meshPointer = $"/meshes/{meshIndex}";
            if (mesh == null)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The mesh is null.",
                    meshPointer);
            }
            else
            {
                ValidateMesh(document, mesh, meshPointer, report);
            }

            meshIndex++;
        }

        return report;
    }

    internal void ValidateMesh(
        GltfDocument document,
        GltfMesh mesh,
        string meshPointer,
        GltfValidationReport report)
    {
        if (mesh.Primitives == null)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The mesh primitive collection is missing.",
                $"{meshPointer}/primitives");
            return;
        }

        int expectedTargetCount = -1;
        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            string primitivePointer = $"{meshPointer}/primitives/{primitiveIndex}";
            if (primitive == null)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The mesh primitive is null.",
                    primitivePointer);
            }
            else
            {
                int targetCount = 0;
                if (primitive.Targets != null)
                {
                    targetCount = primitive.Targets.Count;
                }

                if (expectedTargetCount < 0)
                {
                    expectedTargetCount = targetCount;
                }
                else if (targetCount != expectedTargetCount)
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitivesUnequalTargetsCount,
                        GltfValidationSeverity.Error,
                        "All primitives in a mesh must have the same number of morph targets.",
                        $"{primitivePointer}/targets");
                }

                ValidatePrimitive(document, primitive, primitivePointer, report);
            }

            primitiveIndex++;
        }

        if (mesh.Weights != null
            && expectedTargetCount >= 0
            && mesh.Weights.Length != expectedTargetCount)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshInvalidWeightsCount,
                GltfValidationSeverity.Error,
                "The mesh weights count must equal its morph target count.",
                $"{meshPointer}/weights");
        }
    }

    internal void ValidatePrimitive(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer,
        GltfValidationReport report)
    {
        ValidateMaterialReference(document, primitive, primitivePointer, report);
        MeshAttributeValidationState attributes = ValidateBaseAttributes(
            document,
            primitive,
            primitivePointer,
            report);

        int drawCount = attributes.VertexCount;
        bool hasDrawCount = attributes.HasVertexCount;
        if (primitive.HasIndices)
        {
            GltfAccessor? indices = ResolveMeshAccessor(
                document,
                primitive.Indices,
                $"{primitivePointer}/indices",
                report);
            if (indices != null)
            {
                drawCount = indices.Count;
                hasDrawCount = true;
                if (ValidateIndexAccessorFormat(
                    document,
                    indices,
                    primitivePointer,
                    report))
                {
                    ValidateIndexValues(
                        document,
                        indices,
                        primitive.Indices,
                        attributes,
                        primitive.Mode,
                        report);
                }
            }
        }

        if (hasDrawCount)
        {
            ValidateTopologyCount(
                primitive.Mode,
                drawCount,
                primitivePointer,
                report);
        }

        ValidateMorphTargets(document, primitive, primitivePointer, report);
    }

    internal void ValidateMaterialReference(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer,
        GltfValidationReport report)
    {
        if (!primitive.HasMaterial)
        {
            return;
        }

        if (document.Materials == null
            || primitive.Material < 0
            || primitive.Material >= document.Materials.Count
            || document.Materials[primitive.Material] == null)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "The primitive material index does not resolve to a material.",
                $"{primitivePointer}/material");
        }
    }

    internal MeshAttributeValidationState ValidateBaseAttributes(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer,
        GltfValidationReport report)
    {
        MeshAttributeValidationState state = new MeshAttributeValidationState();
        if (primitive.Attributes == null || primitive.Attributes.Count == 0)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                GltfValidationSeverity.Error,
                "A mesh primitive must define at least one attribute.",
                $"{primitivePointer}/attributes");
            return state;
        }

        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            string attributePointer = $"{primitivePointer}/attributes/{attributeIndex}";
            if (attribute == null || attribute.Semantic == null)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                    GltfValidationSeverity.Error,
                    "The primitive attribute is null or incomplete.",
                    attributePointer);
                attributeIndex++;
                continue;
            }

            attributePointer = PointerBuilder.AppendProperty(
                $"{primitivePointer}/attributes",
                attribute.Semantic);
            AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                attribute.Semantic);
            if (!semantic.Valid || HasDuplicateAttribute(primitive.Attributes, attributeIndex))
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                    GltfValidationSeverity.Error,
                    "The primitive attribute semantic is invalid or duplicated.",
                    attributePointer);
            }

            GltfAccessor? accessor = ResolveMeshAccessor(
                document,
                attribute.Accessor,
                attributePointer,
                report);
            if (accessor != null)
            {
                if (!state.HasVertexCount)
                {
                    state.HasVertexCount = true;
                    state.VertexCount = accessor.Count;
                }
                else if (accessor.Count != state.VertexCount)
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveUnequalAccessorCount,
                        GltfValidationSeverity.Error,
                        "All primitive attribute accessors must have the same count.",
                        attributePointer);
                }

                if (accessor.ComponentType == GltfComponentType.UnsignedInt)
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveAttributesAccessorUnsignedInt,
                        GltfValidationSeverity.Error,
                        "Mesh attributes cannot use unsigned 32-bit integer components.",
                        attributePointer);
                }

                ValidateVertexAttributeAlignment(
                    document,
                    accessor,
                    attributePointer,
                    report);

                if (semantic.Valid)
                {
                    TrackBaseSemantic(state, semantic, attribute.Accessor);
                    if (!ValidateBaseAttributeFormat(document, semantic, accessor))
                    {
                        AddMeshIssue(
                            report,
                            GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                            GltfValidationSeverity.Error,
                            "The accessor format is invalid for the attribute semantic.",
                            attributePointer);
                    }
                    else
                    {
                        ValidateBaseAttributeValues(
                            document,
                            semantic,
                            accessor,
                            attribute.Accessor,
                            report);
                    }

                    if (semantic.Kind == AttributeSemanticKind.Position
                        && (accessor.Minimum == null || accessor.Maximum == null))
                    {
                        AddMeshIssue(
                            report,
                            GltfIssueCodes.MeshPrimitivePositionAccessorWithoutBounds,
                            GltfValidationSeverity.Error,
                            "The POSITION accessor must define minimum and maximum bounds.",
                            attributePointer);
                    }
                }
            }

            attributeIndex++;
        }

        ValidateSemanticContinuity(
            primitive.Attributes,
            AttributeSemanticKind.TextureCoordinate,
            primitivePointer,
            report);
        ValidateSemanticContinuity(
            primitive.Attributes,
            AttributeSemanticKind.Color,
            primitivePointer,
            report);
        ValidateSemanticContinuity(
            primitive.Attributes,
            AttributeSemanticKind.Joints,
            primitivePointer,
            report);
        ValidateSemanticContinuity(
            primitive.Attributes,
            AttributeSemanticKind.Weights,
            primitivePointer,
            report);
        ValidateJointWeightPairing(primitive, primitivePointer, report);
        ValidateWeightValues(document, state, report);

        if (!state.HasPosition)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveNoPosition,
                GltfValidationSeverity.Warning,
                "The primitive does not define a POSITION attribute.",
                $"{primitivePointer}/attributes");
        }

        if (state.HasTangent && !state.HasNormal)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveTangentWithoutNormal,
                GltfValidationSeverity.Warning,
                "The primitive defines TANGENT without NORMAL.",
                $"{primitivePointer}/attributes");
        }

        return state;
    }

    internal bool HasDuplicateAttribute(
        List<GltfAttribute> attributes,
        int attributeIndex)
    {
        GltfAttribute attribute = attributes[attributeIndex];
        int index = 0;
        while (index < attributeIndex)
        {
            GltfAttribute previous = attributes[index];
            if (previous != null
                && previous.Semantic != null
                && previous.Semantic == attribute.Semantic)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void TrackBaseSemantic(
        MeshAttributeValidationState state,
        AttributeSemantic semantic,
        int accessorIndex)
    {
        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
                state.HasPosition = true;
                break;
            case AttributeSemanticKind.Normal:
                state.HasNormal = true;
                break;
            case AttributeSemanticKind.Tangent:
                state.HasTangent = true;
                break;
            case AttributeSemanticKind.Weights:
                state.WeightAccessors.Add(accessorIndex);
                break;
        }
    }

    internal bool ValidateBaseAttributeFormat(
        GltfDocument document,
        AttributeSemantic semantic,
        GltfAccessor accessor)
    {
        bool quantized = UsesMeshQuantization(document);
        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
                return accessor.Type == GltfAccessorType.Vector3
                    && (IsFloatFormat(accessor)
                        || (quantized && IsQuantizedPositionFormat(accessor)));
            case AttributeSemanticKind.Normal:
                return accessor.Type == GltfAccessorType.Vector3
                    && (IsFloatFormat(accessor)
                        || (quantized && IsQuantizedDirectionFormat(accessor)));
            case AttributeSemanticKind.Tangent:
                return accessor.Type == GltfAccessorType.Vector4
                    && (IsFloatFormat(accessor)
                        || (quantized && IsQuantizedDirectionFormat(accessor)));
            case AttributeSemanticKind.TextureCoordinate:
                return accessor.Type == GltfAccessorType.Vector2
                    && (IsFloatFormat(accessor)
                        || IsUnsignedNormalizedFormat(accessor)
                        || (quantized && IsQuantizedTextureCoordinateFormat(accessor)));
            case AttributeSemanticKind.Color:
                return (accessor.Type == GltfAccessorType.Vector3
                        || accessor.Type == GltfAccessorType.Vector4)
                    && (IsFloatFormat(accessor)
                        || IsUnsignedNormalizedFormat(accessor));
            case AttributeSemanticKind.Joints:
                return accessor.Type == GltfAccessorType.Vector4
                    && !accessor.Normalized
                    && (accessor.ComponentType == GltfComponentType.UnsignedByte
                        || accessor.ComponentType == GltfComponentType.UnsignedShort);
            case AttributeSemanticKind.Weights:
                return accessor.Type == GltfAccessorType.Vector4
                    && (IsFloatFormat(accessor)
                        || IsUnsignedNormalizedFormat(accessor));
            case AttributeSemanticKind.Custom:
            case AttributeSemanticKind.Extension:
                return IsVertexShape(accessor.Type);
            default:
                return false;
        }
    }

    internal bool IsFloatFormat(GltfAccessor accessor)
    {
        return accessor.ComponentType == GltfComponentType.Float
            && !accessor.Normalized;
    }

    internal bool IsUnsignedNormalizedFormat(GltfAccessor accessor)
    {
        return accessor.Normalized
            && (accessor.ComponentType == GltfComponentType.UnsignedByte
                || accessor.ComponentType == GltfComponentType.UnsignedShort);
    }

    internal bool IsQuantizedPositionFormat(GltfAccessor accessor)
    {
        return accessor.ComponentType == GltfComponentType.SignedByte
            || accessor.ComponentType == GltfComponentType.UnsignedByte
            || accessor.ComponentType == GltfComponentType.SignedShort
            || accessor.ComponentType == GltfComponentType.UnsignedShort;
    }

    internal bool IsQuantizedDirectionFormat(GltfAccessor accessor)
    {
        return accessor.Normalized
            && (accessor.ComponentType == GltfComponentType.SignedByte
                || accessor.ComponentType == GltfComponentType.SignedShort);
    }

    internal bool IsQuantizedTextureCoordinateFormat(GltfAccessor accessor)
    {
        return accessor.ComponentType == GltfComponentType.SignedByte
            || accessor.ComponentType == GltfComponentType.SignedShort
            || (!accessor.Normalized
                && (accessor.ComponentType == GltfComponentType.UnsignedByte
                    || accessor.ComponentType == GltfComponentType.UnsignedShort));
    }

    internal bool IsVertexShape(GltfAccessorType type)
    {
        return type == GltfAccessorType.Scalar
            || type == GltfAccessorType.Vector2
            || type == GltfAccessorType.Vector3
            || type == GltfAccessorType.Vector4;
    }

    internal bool UsesMeshQuantization(GltfDocument document)
    {
        return Quantization.IsEnabled(document);
    }

    internal void ValidateSemanticContinuity(
        List<GltfAttribute> attributes,
        AttributeSemanticKind kind,
        string primitivePointer,
        GltfValidationReport report)
    {
        int count = 0;
        int maximum = -1;
        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            if (attribute != null && attribute.Semantic != null)
            {
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (semantic.Valid && semantic.Kind == kind && semantic.HasSetIndex)
                {
                    count++;
                    if (semantic.SetIndex > maximum)
                    {
                        maximum = semantic.SetIndex;
                    }
                }
            }

            index++;
        }

        if (maximum >= 0 && count != maximum + 1)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveIndexedSemanticContinuity,
                GltfValidationSeverity.Error,
                $"Indexed attribute semantics must start at zero and be continuous; expected {maximum + 1}, found {count}.",
                $"{primitivePointer}/attributes");
        }
    }

    internal void ValidateJointWeightPairing(
        GltfMeshPrimitive primitive,
        string primitivePointer,
        GltfValidationReport report)
    {
        int jointCount = CountSemanticKind(primitive.Attributes, AttributeSemanticKind.Joints);
        int weightCount = CountSemanticKind(primitive.Attributes, AttributeSemanticKind.Weights);
        if (jointCount != weightCount)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch,
                GltfValidationSeverity.Error,
                $"The primitive defines {jointCount} JOINTS sets and {weightCount} WEIGHTS sets.",
                $"{primitivePointer}/attributes");
        }
    }

    internal int CountSemanticKind(
        List<GltfAttribute> attributes,
        AttributeSemanticKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            if (attribute != null && attribute.Semantic != null)
            {
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (semantic.Valid && semantic.Kind == kind)
                {
                    count++;
                }
            }

            index++;
        }

        return count;
    }

    internal void ValidateVertexAttributeAlignment(
        GltfDocument document,
        GltfAccessor accessor,
        string pointer,
        GltfValidationReport report)
    {
        if (!accessor.HasBufferView
            || document.BufferViews == null
            || accessor.BufferView < 0
            || accessor.BufferView >= document.BufferViews.Count)
        {
            return;
        }

        GltfBufferView view = document.BufferViews[accessor.BufferView];
        if (view == null)
        {
            return;
        }

        if (!AccessorLayout.IsKnownComponentType(accessor.ComponentType)
            || !AccessorLayout.IsKnownAccessorType(accessor.Type))
        {
            return;
        }

        long totalOffset = (long)view.ByteOffset + accessor.ByteOffset;
        int elementStride = 0;
        if (view.HasByteStride)
        {
            elementStride = view.ByteStride;
        }
        else
        {
            elementStride = AccessorLayout.CalculateElementLayout(
                accessor.ComponentType,
                accessor.Type).ElementByteLength;
        }

        if (totalOffset < 0
            || totalOffset % 4L != 0L
            || (accessor.Count > 1 && elementStride % 4 != 0))
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveAccessorUnaligned,
                GltfValidationSeverity.Error,
                $"Vertex attribute data starts at byte offset {totalOffset} with element stride {elementStride}; every element must be aligned to four bytes.",
                pointer);
        }
    }

    internal void ValidateBaseAttributeValues(
        GltfDocument document,
        AttributeSemantic semantic,
        GltfAccessor accessor,
        int accessorIndex,
        GltfValidationReport report)
    {
        if (semantic.Kind != AttributeSemanticKind.Normal
            && semantic.Kind != AttributeSemanticKind.Tangent
            && semantic.Kind != AttributeSemanticKind.Color)
        {
            return;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            return;
        }

        if (semantic.Kind == AttributeSemanticKind.Normal)
        {
            ValidateUnitVectorData(data.FloatingValues, 3, accessorIndex, report);
        }
        else if (semantic.Kind == AttributeSemanticKind.Tangent)
        {
            ValidateTangentData(data.FloatingValues, accessorIndex, report);
        }
        else
        {
            ValidateColorData(data.FloatingValues, accessorIndex, report);
        }
    }

    internal void ValidateUnitVectorData(
        double[] values,
        int componentStride,
        int accessorIndex,
        GltfValidationReport report)
    {
        int offset = 0;
        while (offset + 2 < values.Length)
        {
            double x = values[offset];
            double y = values[offset + 1];
            double z = values[offset + 2];
            double length = Math.Sqrt(x * x + y * y + z * z);
            if (!double.IsNaN(length)
                && !double.IsInfinity(length)
                && Math.Abs(length - 1.0) > 0.02)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorVector3NonUnit,
                    GltfValidationSeverity.Error,
                    $"Vector at element {offset / componentStride} has length {length} instead of unit length.",
                    $"/accessors/{accessorIndex}");
            }

            offset += componentStride;
        }
    }

    internal void ValidateTangentData(
        double[] values,
        int accessorIndex,
        GltfValidationReport report)
    {
        ValidateUnitVectorData(values, 4, accessorIndex, report);
        int offset = 0;
        while (offset + 3 < values.Length)
        {
            double sign = values[offset + 3];
            if (sign != -1.0 && sign != 1.0)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorInvalidSign,
                    GltfValidationSeverity.Error,
                    $"Tangent at element {offset / 4} has sign {sign}; it must be -1 or 1.",
                    $"/accessors/{accessorIndex}");
            }

            offset += 4;
        }
    }

    internal void ValidateColorData(
        double[] values,
        int accessorIndex,
        GltfValidationReport report)
    {
        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!double.IsNaN(value) && (value < 0.0 || value > 1.0))
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorNonClamped,
                    GltfValidationSeverity.Error,
                    $"Color component {index} has value {value} outside the 0 to 1 range.",
                    $"/accessors/{accessorIndex}");
            }

            index++;
        }
    }

    internal void ValidateWeightValues(
        GltfDocument document,
        MeshAttributeValidationState state,
        GltfValidationReport report)
    {
        if (state.WeightAccessors.Count == 0 || !state.HasVertexCount)
        {
            return;
        }

        double[] sums = new double[state.VertexCount];
        bool complete = true;
        int setIndex = 0;
        while (setIndex < state.WeightAccessors.Count)
        {
            int accessorIndex = state.WeightAccessors[setIndex];
            GltfAccessorReadRequest request = new GltfAccessorReadRequest();
            request.Document = document;
            request.Accessor = accessorIndex;
            GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
            if (data.Status != GltfOperationStatus.Success
                || data.FloatingValues.Length != state.VertexCount * 4)
            {
                complete = false;
            }
            else
            {
                int componentIndex = 0;
                while (componentIndex < data.FloatingValues.Length)
                {
                    double value = data.FloatingValues[componentIndex];
                    if (!double.IsNaN(value) && value < 0.0)
                    {
                        AddMeshIssue(
                            report,
                            GltfIssueCodes.AccessorWeightsNegative,
                            GltfValidationSeverity.Error,
                            $"Weight component {componentIndex} has negative value {value}.",
                            $"/accessors/{accessorIndex}");
                    }

                    sums[componentIndex / 4] += value;
                    componentIndex++;
                }
            }

            setIndex++;
        }

        if (!complete)
        {
            return;
        }

        int vertexIndex = 0;
        while (vertexIndex < sums.Length)
        {
            double sum = sums[vertexIndex];
            if (!double.IsNaN(sum)
                && !double.IsInfinity(sum)
                && Math.Abs(sum - 1.0) > 0.0001)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorWeightsNonNormalized,
                    GltfValidationSeverity.Error,
                    $"Weights for vertex {vertexIndex} have sum {sum} instead of 1.",
                    $"/accessors/{state.WeightAccessors[0]}");
            }

            vertexIndex++;
        }
    }

    internal bool ValidateIndexAccessorFormat(
        GltfDocument document,
        GltfAccessor accessor,
        string primitivePointer,
        GltfValidationReport report)
    {
        bool valid = accessor.Type == GltfAccessorType.Scalar
            && !accessor.Normalized
            && (accessor.ComponentType == GltfComponentType.UnsignedByte
                || accessor.ComponentType == GltfComponentType.UnsignedShort
                || accessor.ComponentType == GltfComponentType.UnsignedInt);
        if (!valid)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveIndicesAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "Indices require a non-normalized scalar unsigned integer accessor.",
                $"{primitivePointer}/indices");
            return false;
        }

        if (accessor.HasBufferView
            && document.BufferViews != null
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[accessor.BufferView];
            if (view != null && view.HasByteStride)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveIndicesAccessorWithBytestride,
                    GltfValidationSeverity.Error,
                    "An indices accessor buffer view cannot define byteStride.",
                    $"/bufferViews/{accessor.BufferView}/byteStride");
            }
        }

        return true;
    }

    internal void ValidateIndexValues(
        GltfDocument document,
        GltfAccessor accessor,
        int accessorIndex,
        MeshAttributeValidationState attributes,
        GltfPrimitiveMode mode,
        GltfValidationReport report)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData data = AccessorCodec.ReadIndexData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            return;
        }

        uint restart = uint.MaxValue;
        if (accessor.ComponentType == GltfComponentType.UnsignedByte)
        {
            restart = byte.MaxValue;
        }
        else if (accessor.ComponentType == GltfComponentType.UnsignedShort)
        {
            restart = ushort.MaxValue;
        }

        int index = 0;
        while (index < data.Indices.Length)
        {
            uint value = data.Indices[index];
            string pointer = $"/accessors/{accessorIndex}";
            if (value == restart)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorIndexPrimitiveRestart,
                    GltfValidationSeverity.Error,
                    $"Index {index} uses the prohibited primitive restart value {value}.",
                    pointer);
            }
            else if (attributes.HasVertexCount
                && (long)value >= attributes.VertexCount)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.AccessorIndexOob,
                    GltfValidationSeverity.Error,
                    $"Index {index} value {value} is outside vertex count {attributes.VertexCount}.",
                    pointer);
            }

            index++;
        }

        ValidateDegenerateTriangles(data.Indices, mode, accessorIndex, report);
    }

    internal void ValidateDegenerateTriangles(
        uint[] indices,
        GltfPrimitiveMode mode,
        int accessorIndex,
        GltfValidationReport report)
    {
        int degenerateCount = 0;
        if (mode == GltfPrimitiveMode.Triangles)
        {
            int index = 0;
            while (index + 2 < indices.Length)
            {
                if (indices[index] == indices[index + 1]
                    || indices[index] == indices[index + 2]
                    || indices[index + 1] == indices[index + 2])
                {
                    degenerateCount++;
                }

                index += 3;
            }
        }
        else if (mode == GltfPrimitiveMode.TriangleStrip)
        {
            int index = 0;
            while (index + 2 < indices.Length)
            {
                if (indices[index] == indices[index + 1]
                    || indices[index] == indices[index + 2]
                    || indices[index + 1] == indices[index + 2])
                {
                    degenerateCount++;
                }

                index++;
            }
        }
        else if (mode == GltfPrimitiveMode.TriangleFan)
        {
            int index = 1;
            while (index + 1 < indices.Length)
            {
                if (indices[0] == indices[index]
                    || indices[0] == indices[index + 1]
                    || indices[index] == indices[index + 1])
                {
                    degenerateCount++;
                }

                index++;
            }
        }

        if (degenerateCount > 0)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.AccessorIndexTriangleDegenerate,
                GltfValidationSeverity.Information,
                $"The indices accessor contains {degenerateCount} degenerate triangles.",
                $"/accessors/{accessorIndex}");
        }
    }

    internal void ValidateTopologyCount(
        GltfPrimitiveMode mode,
        int drawCount,
        string primitivePointer,
        GltfValidationReport report)
    {
        bool valid;
        switch (mode)
        {
            case GltfPrimitiveMode.Points:
                valid = drawCount > 0;
                break;
            case GltfPrimitiveMode.Lines:
                valid = drawCount > 0 && drawCount % 2 == 0;
                break;
            case GltfPrimitiveMode.LineLoop:
            case GltfPrimitiveMode.LineStrip:
                valid = drawCount >= 2;
                break;
            case GltfPrimitiveMode.Triangles:
                valid = drawCount > 0 && drawCount % 3 == 0;
                break;
            case GltfPrimitiveMode.TriangleStrip:
            case GltfPrimitiveMode.TriangleFan:
                valid = drawCount >= 3;
                break;
            default:
                valid = false;
                break;
        }

        if (!valid)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.MeshPrimitiveIncompatibleMode,
                GltfValidationSeverity.Warning,
                $"Draw count {drawCount} is incompatible with primitive mode {mode}.",
                $"{primitivePointer}/mode");
        }
    }

    internal void ValidateMorphTargets(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer,
        GltfValidationReport report)
    {
        if (primitive.Targets == null)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The primitive morph target collection is missing.",
                $"{primitivePointer}/targets");
            return;
        }

        int targetIndex = 0;
        while (targetIndex < primitive.Targets.Count)
        {
            GltfMorphTarget target = primitive.Targets[targetIndex];
            string targetPointer = $"{primitivePointer}/targets/{targetIndex}";
            if (target == null || target.Attributes == null)
            {
                AddMeshIssue(
                    report,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The morph target is null or incomplete.",
                    targetPointer);
                targetIndex++;
                continue;
            }

            int attributeIndex = 0;
            while (attributeIndex < target.Attributes.Count)
            {
                GltfAttribute attribute = target.Attributes[attributeIndex];
                string pointer = $"{targetPointer}/{attributeIndex}";
                if (attribute == null || attribute.Semantic == null)
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                        GltfValidationSeverity.Error,
                        "The morph target attribute is null or incomplete.",
                        pointer);
                    attributeIndex++;
                    continue;
                }

                pointer = PointerBuilder.AppendProperty(targetPointer, attribute.Semantic);
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (!semantic.Valid || HasDuplicateAttribute(target.Attributes, attributeIndex))
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                        GltfValidationSeverity.Error,
                        "The morph target attribute semantic is invalid or duplicated.",
                        pointer);
                }

                GltfAttribute? baseAttribute = FindBaseAttribute(
                    primitive,
                    attribute.Semantic);
                if (baseAttribute == null)
                {
                    AddMeshIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveMorphTargetNoBaseAccessor,
                        GltfValidationSeverity.Error,
                        "The primitive does not define a matching base attribute.",
                        pointer);
                }

                GltfAccessor? targetAccessor = ResolveMeshAccessor(
                    document,
                    attribute.Accessor,
                    pointer,
                    report);
                GltfAccessor? baseAccessor = null;
                if (baseAttribute != null)
                {
                    baseAccessor = ResolveAccessorWithoutIssue(
                        document,
                        baseAttribute.Accessor);
                }

                if (targetAccessor != null)
                {
                    ValidateVertexAttributeAlignment(
                        document,
                        targetAccessor,
                        pointer,
                        report);
                }

                if (targetAccessor != null && baseAccessor != null)
                {
                    if (targetAccessor.Count != baseAccessor.Count)
                    {
                        AddMeshIssue(
                            report,
                            GltfIssueCodes.MeshPrimitiveMorphTargetInvalidAttributeCount,
                            GltfValidationSeverity.Error,
                            "The morph target accessor count differs from its base accessor.",
                            pointer);
                    }

                    if (semantic.Valid
                        && !ValidateMorphAttributeFormat(
                            document,
                            semantic,
                            targetAccessor,
                            baseAccessor))
                    {
                        AddMeshIssue(
                            report,
                            GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                            GltfValidationSeverity.Error,
                            "The accessor format is invalid for the morph target semantic.",
                            pointer);
                    }
                }

                attributeIndex++;
            }

            targetIndex++;
        }
    }

    internal bool ValidateMorphAttributeFormat(
        GltfDocument document,
        AttributeSemantic semantic,
        GltfAccessor accessor,
        GltfAccessor baseAccessor)
    {
        bool quantized = UsesMeshQuantization(document);
        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
                return accessor.Type == GltfAccessorType.Vector3
                    && (IsFloatFormat(accessor)
                        || (quantized && IsQuantizedMorphPositionFormat(accessor)));
            case AttributeSemanticKind.Normal:
            case AttributeSemanticKind.Tangent:
                return accessor.Type == GltfAccessorType.Vector3
                    && (IsFloatFormat(accessor)
                        || (quantized && IsQuantizedDirectionFormat(accessor)));
            case AttributeSemanticKind.TextureCoordinate:
                return quantized
                    && accessor.Type == GltfAccessorType.Vector2
                    && !accessor.Normalized
                    && (accessor.ComponentType == GltfComponentType.SignedByte
                        || accessor.ComponentType == GltfComponentType.SignedShort);
            case AttributeSemanticKind.Color:
            case AttributeSemanticKind.Joints:
            case AttributeSemanticKind.Weights:
                return false;
            case AttributeSemanticKind.Custom:
            case AttributeSemanticKind.Extension:
                return accessor.Type == baseAccessor.Type
                    && accessor.ComponentType == baseAccessor.ComponentType
                    && accessor.Normalized == baseAccessor.Normalized;
            default:
                return false;
        }
    }

    internal bool IsQuantizedMorphPositionFormat(GltfAccessor accessor)
    {
        return accessor.ComponentType == GltfComponentType.SignedByte
            || accessor.ComponentType == GltfComponentType.SignedShort;
    }

    internal GltfAttribute? FindBaseAttribute(
        GltfMeshPrimitive primitive,
        string semantic)
    {
        if (primitive.Attributes == null)
        {
            return null;
        }

        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute != null && attribute.Semantic == semantic)
            {
                return attribute;
            }

            index++;
        }

        return null;
    }

    internal GltfAccessor? ResolveMeshAccessor(
        GltfDocument document,
        int accessorIndex,
        string pointer,
        GltfValidationReport report)
    {
        GltfAccessor? accessor = ResolveAccessorWithoutIssue(document, accessorIndex);
        if (accessor == null)
        {
            AddMeshIssue(
                report,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "The accessor index does not resolve to an accessor.",
                pointer);
        }

        return accessor;
    }

    internal GltfAccessor? ResolveAccessorWithoutIssue(
        GltfDocument document,
        int accessorIndex)
    {
        if (document.Accessors == null
            || accessorIndex < 0
            || accessorIndex >= document.Accessors.Count)
        {
            return null;
        }

        return document.Accessors[accessorIndex];
    }

    internal void AddMeshIssue(
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
