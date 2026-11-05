using System;
using System.Collections.Generic;

namespace gltf;

internal class AccessorUsageInfo
{
    internal bool VertexAttribute;
    internal bool PrimitiveIndices;
    internal bool Animation;
    internal bool InverseBindMatrices;
}

internal class BufferViewUsageInfo
{
    internal bool VertexAttribute;
    internal bool PrimitiveIndices;
    internal bool Animation;
    internal bool InverseBindMatrices;
    internal bool Image;
    internal bool Sparse;
    internal bool OtherAccessor;
    internal List<int> VertexAccessors = new List<int>();
}

internal class BufferAccessorValidator
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorDecoder Decoder = new AccessorDecoder();
    internal AccessorBoundsCalculator Bounds = new AccessorBoundsCalculator();

    internal GltfValidationReport ValidateBufferAccessorRules(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        ValidateBufferRules(document, report);
        List<AccessorUsageInfo> accessorUsage = CreateAccessorUsageMap(document);
        List<BufferViewUsageInfo> viewUsage = CreateBufferViewUsageMap(
            document,
            accessorUsage);
        ValidateBufferViewRules(document, viewUsage, report);
        ValidateAccessorRules(document, accessorUsage, report);
        return report;
    }

    internal void ValidateBufferRules(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Buffers == null)
        {
            return;
        }

        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            string pointer = $"/buffers/{bufferIndex}";
            if (buffer != null)
            {
                if (buffer.Resolved)
                {
                    if (buffer.ResolvedBytes == null)
                    {
                        AddBufferAccessorIssue(
                            report,
                            GltfIssueCodes.ResourceUnavailable,
                            GltfValidationSeverity.Error,
                            "The resolved buffer bytes are missing.",
                            pointer);
                    }
                    else if (buffer.ResolvedBytes.Length < buffer.ByteLength)
                    {
                        AddBufferAccessorIssue(
                            report,
                            GltfIssueCodes.BufferByteLengthMismatch,
                            GltfValidationSeverity.Error,
                            $"Resolved buffer length {buffer.ResolvedBytes.Length} is less than declared byteLength {buffer.ByteLength}.",
                            $"{pointer}/byteLength");
                    }
                }
                else if (buffer.Uri == null)
                {
                    if (document.Container == null
                        || document.Container.Format != GltfContainerFormat.Glb
                        || !document.Container.HasBinChunk)
                    {
                        AddBufferAccessorIssue(
                            report,
                            GltfIssueCodes.BufferMissingGlbData,
                            GltfValidationSeverity.Error,
                            "The URI-less buffer has no resolved GLB binary data.",
                            pointer);
                    }
                    else
                    {
                        AddBufferAccessorIssue(
                            report,
                            GltfIssueCodes.ResourceUnavailable,
                            GltfValidationSeverity.Warning,
                            "The GLB binary data has not been bound to the buffer.",
                            pointer);
                    }
                }
                else
                {
                    AddBufferAccessorIssue(
                        report,
                        GltfIssueCodes.ResourceUnavailable,
                        GltfValidationSeverity.Information,
                        "The external buffer resource has not been resolved.",
                        pointer);
                }

                ValidateGlbBufferLength(document, buffer, bufferIndex, report);
            }

            bufferIndex++;
        }
    }

    internal void ValidateGlbBufferLength(
        GltfDocument document,
        GltfBuffer buffer,
        int bufferIndex,
        GltfValidationReport report)
    {
        if (bufferIndex != 0 || buffer.Uri != null || document.Container == null
            || document.Container.Format != GltfContainerFormat.Glb
            || !document.Container.HasBinChunk
            || document.Container.BinBytes == null)
        {
            return;
        }

        int actualLength = document.Container.BinBytes.Length;
        if (actualLength < buffer.ByteLength)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferByteLengthMismatch,
                GltfValidationSeverity.Error,
                $"GLB binary chunk length {actualLength} is less than declared byteLength {buffer.ByteLength}.",
                $"/buffers/{bufferIndex}/byteLength");
        }
        else if ((long)actualLength > (long)buffer.ByteLength + 3L)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferGlbChunkTooBig,
                GltfValidationSeverity.Warning,
                $"GLB binary chunk has {actualLength - buffer.ByteLength} excess bytes.",
                $"/buffers/{bufferIndex}/byteLength");
        }
    }

    internal List<AccessorUsageInfo> CreateAccessorUsageMap(GltfDocument document)
    {
        List<AccessorUsageInfo> result = new List<AccessorUsageInfo>();
        int accessorCount = document.Accessors == null ? 0 : document.Accessors.Count;
        int accessorIndex = 0;
        while (accessorIndex < accessorCount)
        {
            result.Add(new AccessorUsageInfo());
            accessorIndex++;
        }

        MarkMeshAccessorUsage(document, result);
        MarkAnimationAccessorUsage(document, result);
        MarkSkinAccessorUsage(document, result);
        return result;
    }

    internal void MarkMeshAccessorUsage(
        GltfDocument document,
        List<AccessorUsageInfo> usage)
    {
        if (document.Meshes == null)
        {
            return;
        }

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
                    if (primitive != null)
                    {
                        if (primitive.HasIndices)
                        {
                            MarkPrimitiveIndexUsage(usage, primitive.Indices);
                        }

                        MarkAttributeAccessorUsage(usage, primitive.Attributes);
                        if (primitive.Targets != null)
                        {
                            int targetIndex = 0;
                            while (targetIndex < primitive.Targets.Count)
                            {
                                GltfMorphTarget target = primitive.Targets[targetIndex];
                                if (target != null)
                                {
                                    MarkAttributeAccessorUsage(usage, target.Attributes);
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

    internal void MarkAttributeAccessorUsage(
        List<AccessorUsageInfo> usage,
        List<GltfAttribute> attributes)
    {
        if (attributes == null)
        {
            return;
        }

        int index = 0;
        while (index < attributes.Count)
        {
            GltfAttribute attribute = attributes[index];
            if (attribute != null && attribute.Accessor >= 0
                && attribute.Accessor < usage.Count)
            {
                usage[attribute.Accessor].VertexAttribute = true;
            }

            index++;
        }
    }

    internal void MarkPrimitiveIndexUsage(
        List<AccessorUsageInfo> usage,
        int accessorIndex)
    {
        if (accessorIndex >= 0 && accessorIndex < usage.Count)
        {
            usage[accessorIndex].PrimitiveIndices = true;
        }
    }

    internal void MarkAnimationAccessorUsage(
        GltfDocument document,
        List<AccessorUsageInfo> usage)
    {
        if (document.Animations == null)
        {
            return;
        }

        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            if (animation != null && animation.Samplers != null)
            {
                int samplerIndex = 0;
                while (samplerIndex < animation.Samplers.Count)
                {
                    GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
                    if (sampler != null)
                    {
                        MarkAnimationIndexUsage(usage, sampler.Input);
                        MarkAnimationIndexUsage(usage, sampler.Output);
                    }

                    samplerIndex++;
                }
            }

            animationIndex++;
        }
    }

    internal void MarkAnimationIndexUsage(
        List<AccessorUsageInfo> usage,
        int accessorIndex)
    {
        if (accessorIndex >= 0 && accessorIndex < usage.Count)
        {
            usage[accessorIndex].Animation = true;
        }
    }

    internal void MarkSkinAccessorUsage(
        GltfDocument document,
        List<AccessorUsageInfo> usage)
    {
        if (document.Skins == null)
        {
            return;
        }

        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            if (skin != null && skin.HasInverseBindMatrices
                && skin.InverseBindMatrices >= 0
                && skin.InverseBindMatrices < usage.Count)
            {
                usage[skin.InverseBindMatrices].InverseBindMatrices = true;
            }

            skinIndex++;
        }
    }

    internal List<BufferViewUsageInfo> CreateBufferViewUsageMap(
        GltfDocument document,
        List<AccessorUsageInfo> accessorUsage)
    {
        List<BufferViewUsageInfo> result = new List<BufferViewUsageInfo>();
        int viewCount = document.BufferViews == null ? 0 : document.BufferViews.Count;
        int viewIndex = 0;
        while (viewIndex < viewCount)
        {
            result.Add(new BufferViewUsageInfo());
            viewIndex++;
        }

        if (document.Accessors != null)
        {
            int accessorIndex = 0;
            while (accessorIndex < document.Accessors.Count)
            {
                GltfAccessor accessor = document.Accessors[accessorIndex];
                if (accessor != null)
                {
                    if (accessor.HasBufferView && accessor.BufferView >= 0
                        && accessor.BufferView < result.Count)
                    {
                        ApplyAccessorUsageToView(
                            result[accessor.BufferView],
                            accessorUsage[accessorIndex],
                            accessorIndex);
                    }

                    if (accessor.Sparse != null)
                    {
                        if (accessor.Sparse.Indices != null)
                        {
                            MarkSparseViewUsage(
                                result,
                                accessor.Sparse.Indices.BufferView);
                        }

                        if (accessor.Sparse.Values != null)
                        {
                            MarkSparseViewUsage(
                                result,
                                accessor.Sparse.Values.BufferView);
                        }
                    }
                }

                accessorIndex++;
            }
        }

        MarkImageViewUsage(document, result);
        return result;
    }

    internal void ApplyAccessorUsageToView(
        BufferViewUsageInfo view,
        AccessorUsageInfo accessor,
        int accessorIndex)
    {
        view.VertexAttribute = view.VertexAttribute || accessor.VertexAttribute;
        view.PrimitiveIndices = view.PrimitiveIndices || accessor.PrimitiveIndices;
        view.Animation = view.Animation || accessor.Animation;
        view.InverseBindMatrices = view.InverseBindMatrices
            || accessor.InverseBindMatrices;
        if (!accessor.VertexAttribute && !accessor.PrimitiveIndices
            && !accessor.Animation && !accessor.InverseBindMatrices)
        {
            view.OtherAccessor = true;
        }

        if (accessor.VertexAttribute && !view.VertexAccessors.Contains(accessorIndex))
        {
            view.VertexAccessors.Add(accessorIndex);
        }
    }

    internal void MarkSparseViewUsage(
        List<BufferViewUsageInfo> usage,
        int viewIndex)
    {
        if (viewIndex >= 0 && viewIndex < usage.Count)
        {
            usage[viewIndex].Sparse = true;
        }
    }

    internal void MarkImageViewUsage(
        GltfDocument document,
        List<BufferViewUsageInfo> usage)
    {
        if (document.Images == null)
        {
            return;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image != null && image.HasBufferView
                && image.BufferView >= 0 && image.BufferView < usage.Count)
            {
                usage[image.BufferView].Image = true;
            }

            imageIndex++;
        }
    }

    internal void ValidateBufferViewRules(
        GltfDocument document,
        List<BufferViewUsageInfo> usage,
        GltfValidationReport report)
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
                ValidateBufferViewRange(document, view, viewIndex, report);
                ValidateBufferViewStride(view, viewIndex, usage[viewIndex], report);
                ValidateBufferViewTarget(view, viewIndex, usage[viewIndex], report);
                ValidateBufferViewUsage(view, viewIndex, usage[viewIndex], report);
            }

            viewIndex++;
        }

        ValidateBufferViewOverlapRules(document, usage, report);
    }

    internal void ValidateBufferViewRange(
        GltfDocument document,
        GltfBufferView view,
        int viewIndex,
        GltfValidationReport report)
    {
        if (view.Buffer < 0 || document.Buffers == null
            || view.Buffer >= document.Buffers.Count)
        {
            return;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        if (buffer == null)
        {
            return;
        }

        long byteOffset = view.HasByteOffset ? view.ByteOffset : 0;
        long byteEnd = byteOffset + view.ByteLength;
        if (byteOffset < 0 || view.ByteLength < 0 || byteEnd < byteOffset
            || byteEnd > buffer.ByteLength)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTooLong,
                GltfValidationSeverity.Error,
                "The bufferView does not fit its declared buffer.",
                $"/bufferViews/{viewIndex}");
            return;
        }

        if (buffer.Resolved && buffer.ResolvedBytes != null
            && byteEnd > buffer.ResolvedBytes.Length)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTooLong,
                GltfValidationSeverity.Error,
                "The bufferView does not fit the resolved buffer bytes.",
                $"/bufferViews/{viewIndex}");
        }
    }

    internal void ValidateBufferViewStride(
        GltfBufferView view,
        int viewIndex,
        BufferViewUsageInfo usage,
        GltfValidationReport report)
    {
        if (!view.HasByteStride)
        {
            if (usage.VertexAccessors.Count > 1)
            {
                AddBufferAccessorIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveAccessorWithoutBytestride,
                    GltfValidationSeverity.Error,
                    "A bufferView shared by vertex attributes must define byteStride.",
                    $"/bufferViews/{viewIndex}");
            }

            return;
        }

        if (view.ByteStride < 4 || view.ByteStride > 252
            || view.ByteStride % 4 != 0)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewInvalidByteStride,
                GltfValidationSeverity.Error,
                $"Invalid bufferView byteStride {view.ByteStride}.",
                $"/bufferViews/{viewIndex}/byteStride");
        }

        if (view.ByteStride > view.ByteLength)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTooBigByteStride,
                GltfValidationSeverity.Error,
                $"BufferView byteStride {view.ByteStride} exceeds byteLength {view.ByteLength}.",
                $"/bufferViews/{viewIndex}/byteStride");
        }

        if (usage.Image)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.ImageBufferViewWithBytestride,
                GltfValidationSeverity.Error,
                "Image bufferViews must not define byteStride.",
                $"/bufferViews/{viewIndex}/byteStride");
        }
        else if (!usage.VertexAttribute || CountBufferViewUsageKinds(usage) > 1)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewInvalidByteStride,
                GltfValidationSeverity.Error,
                "Only vertex attribute bufferViews may define byteStride.",
                $"/bufferViews/{viewIndex}/byteStride");
        }
    }

    internal void ValidateBufferViewTarget(
        GltfBufferView view,
        int viewIndex,
        BufferViewUsageInfo usage,
        GltfValidationReport report)
    {
        string pointer = $"/bufferViews/{viewIndex}/target";
        if (view.HasTarget && (view.Target == GltfBufferViewTarget.None
            || !Layout.IsKnownBufferViewTarget(view.Target)))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                $"Invalid GL bufferView target {(int)view.Target}.",
                pointer);
            return;
        }

        int kindCount = CountBufferViewUsageKinds(usage);
        if (kindCount != 1)
        {
            return;
        }

        GltfBufferViewTarget expected = GltfBufferViewTarget.None;
        if (usage.VertexAttribute)
        {
            expected = GltfBufferViewTarget.ArrayBuffer;
        }
        else if (usage.PrimitiveIndices)
        {
            expected = GltfBufferViewTarget.ElementArrayBuffer;
        }

        if (expected == GltfBufferViewTarget.None)
        {
            if (view.HasTarget)
            {
                AddBufferAccessorIssue(
                    report,
                    GltfIssueCodes.BufferViewTargetOverride,
                    GltfValidationSeverity.Error,
                    "The declared target does not match the bufferView usage.",
                    pointer);
            }

            return;
        }

        if (!view.HasTarget)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTargetMissing,
                GltfValidationSeverity.Information,
                "The vertex or index bufferView should define target.",
                $"/bufferViews/{viewIndex}");
        }
        else if (view.Target != expected)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                GltfValidationSeverity.Error,
                $"The declared target {(int)view.Target} does not match the bufferView usage.",
                pointer);
        }
    }

    internal void ValidateBufferViewUsage(
        GltfBufferView view,
        int viewIndex,
        BufferViewUsageInfo usage,
        GltfValidationReport report)
    {
        if (CountBufferViewUsageKinds(usage) > 1)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                GltfValidationSeverity.Error,
                "The bufferView is used for incompatible data kinds.",
                $"/bufferViews/{viewIndex}");
        }

        if (usage.Sparse && (view.HasByteStride || view.HasTarget))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewInvalidByteStride,
                GltfValidationSeverity.Error,
                "Sparse accessor bufferViews must not define byteStride or target.",
                $"/bufferViews/{viewIndex}");
        }
    }

    internal int CountBufferViewUsageKinds(BufferViewUsageInfo usage)
    {
        int result = 0;
        if (usage.VertexAttribute)
        {
            result++;
        }

        if (usage.PrimitiveIndices)
        {
            result++;
        }

        if (usage.Animation)
        {
            result++;
        }

        if (usage.InverseBindMatrices)
        {
            result++;
        }

        if (usage.Image)
        {
            result++;
        }

        if (usage.Sparse)
        {
            result++;
        }

        if (usage.OtherAccessor)
        {
            result++;
        }

        return result;
    }

    internal int GetExclusiveBufferViewUsage(BufferViewUsageInfo usage)
    {
        if (CountBufferViewUsageKinds(usage) != 1)
        {
            return 0;
        }

        if (usage.VertexAttribute)
        {
            return 1;
        }

        if (usage.PrimitiveIndices)
        {
            return 2;
        }

        if (usage.Animation)
        {
            return 3;
        }

        if (usage.InverseBindMatrices)
        {
            return 4;
        }

        if (usage.Image)
        {
            return 5;
        }

        if (usage.Sparse)
        {
            return 6;
        }

        return 7;
    }

    internal void ValidateBufferViewOverlapRules(
        GltfDocument document,
        List<BufferViewUsageInfo> usage,
        GltfValidationReport report)
    {
        if (document.BufferViews == null)
        {
            return;
        }

        int rightIndex = 0;
        while (rightIndex < document.BufferViews.Count)
        {
            GltfBufferView right = document.BufferViews[rightIndex];
            int rightUsage = GetExclusiveBufferViewUsage(usage[rightIndex]);
            if (right != null && rightUsage > 0 && right.ByteLength > 0)
            {
                int leftIndex = 0;
                while (leftIndex < rightIndex)
                {
                    GltfBufferView left = document.BufferViews[leftIndex];
                    int leftUsage = GetExclusiveBufferViewUsage(usage[leftIndex]);
                    if (left != null && left.Buffer == right.Buffer
                        && leftUsage > 0 && leftUsage != rightUsage
                        && BufferViewRangesOverlap(left, right))
                    {
                        GltfValidationIssue issue = AddBufferAccessorIssue(
                            report,
                            GltfIssueCodes.BufferViewTargetOverride,
                            GltfValidationSeverity.Error,
                            $"The bufferView overlaps bufferView {leftIndex} used for a different data kind.",
                            $"/bufferViews/{rightIndex}");
                        issue.RelatedJsonPointers.Add($"/bufferViews/{leftIndex}");
                    }

                    leftIndex++;
                }
            }

            rightIndex++;
        }
    }

    internal bool BufferViewRangesOverlap(GltfBufferView left, GltfBufferView right)
    {
        long leftStart = left.HasByteOffset ? left.ByteOffset : 0;
        long rightStart = right.HasByteOffset ? right.ByteOffset : 0;
        long leftEnd = leftStart + left.ByteLength;
        long rightEnd = rightStart + right.ByteLength;
        return leftStart < rightEnd && rightStart < leftEnd;
    }

    internal void ValidateAccessorRules(
        GltfDocument document,
        List<AccessorUsageInfo> usage,
        GltfValidationReport report)
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
                string pointer = $"/accessors/{accessorIndex}";
                bool typeValid = ValidateAccessorTypeRules(
                    accessor,
                    pointer,
                    report);
                ValidateAccessorUsageRules(
                    usage[accessorIndex],
                    pointer,
                    report);
                if (typeValid)
                {
                    bool denseValid = ValidateDenseAccessorLayout(
                        document,
                        accessor,
                        accessorIndex,
                        report);
                    bool sparseValid = ValidateSparseAccessorLayout(
                        document,
                        accessor,
                        accessorIndex,
                        report);
                    ValidateAccessorBoundsAndValues(
                        document,
                        accessor,
                        accessorIndex,
                        denseValid,
                        sparseValid,
                        report);
                }
            }

            accessorIndex++;
        }
    }

    internal bool ValidateAccessorTypeRules(
        GltfAccessor accessor,
        string pointer,
        GltfValidationReport report)
    {
        bool valid = true;
        if (!Layout.IsKnownComponentType(accessor.ComponentType))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                $"Unknown accessor component type {(int)accessor.ComponentType}.",
                $"{pointer}/componentType");
            valid = false;
        }

        if (!Layout.IsKnownAccessorType(accessor.Type))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Error,
                $"Unknown accessor element type {(int)accessor.Type}.",
                $"{pointer}/type");
            valid = false;
        }

        if (accessor.Normalized
            && accessor.ComponentType != GltfComponentType.SignedByte
            && accessor.ComponentType != GltfComponentType.UnsignedByte
            && accessor.ComponentType != GltfComponentType.SignedShort
            && accessor.ComponentType != GltfComponentType.UnsignedShort)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorNormalizedInvalid,
                GltfValidationSeverity.Error,
                "Only byte and short integer accessors may be normalized.",
                $"{pointer}/normalized");
            valid = false;
        }

        return valid;
    }

    internal void ValidateAccessorUsageRules(
        AccessorUsageInfo usage,
        string pointer,
        GltfValidationReport report)
    {
        int kinds = 0;
        if (usage.VertexAttribute)
        {
            kinds++;
        }

        if (usage.PrimitiveIndices)
        {
            kinds++;
        }

        if (usage.Animation)
        {
            kinds++;
        }

        if (usage.InverseBindMatrices)
        {
            kinds++;
        }

        if (kinds > 1)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorUsageOverride,
                GltfValidationSeverity.Error,
                "The accessor is used for incompatible data kinds.",
                pointer);
        }
    }

    internal bool ValidateDenseAccessorLayout(
        GltfDocument document,
        GltfAccessor accessor,
        int accessorIndex,
        GltfValidationReport report)
    {
        if (!accessor.HasBufferView)
        {
            return true;
        }

        GltfBufferView? view = GetValidBufferView(document, accessor.BufferView);
        if (view == null || accessor.Count < 0)
        {
            return false;
        }

        BufferViewByteRange viewRange = Layout.CalculateBufferViewRange(view);
        AccessorByteRange range = Layout.CalculateAccessorByteRange(accessor, viewRange);
        string pointer = $"/accessors/{accessorIndex}";
        bool valid = ValidateAccessorByteRangeRules(
            range,
            view,
            pointer,
            report);
        return valid;
    }

    internal bool ValidateAccessorByteRangeRules(
        AccessorByteRange range,
        GltfBufferView view,
        string pointer,
        GltfValidationReport report)
    {
        bool valid = true;
        if (!Layout.IsComponentOffsetAligned(
                range.ElementLayout,
                range.AccessorByteOffset))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorOffsetAlignment,
                GltfValidationSeverity.Error,
                $"Accessor byteOffset {range.AccessorByteOffset} is not aligned to component size {range.ElementLayout.ComponentByteLength}.",
                $"{pointer}/byteOffset");
            valid = false;
        }

        if (range.FirstElementBufferByteOffset < 0
            || range.FirstElementBufferByteOffset
                % range.ElementLayout.ComponentByteLength != 0)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorTotalOffsetAlignment,
                GltfValidationSeverity.Error,
                $"Accessor total byte offset {range.FirstElementBufferByteOffset} is not aligned to component size {range.ElementLayout.ComponentByteLength}.",
                pointer);
            valid = false;
        }

        if (range.ElementLayout.ColumnCount > 1
            && !Layout.IsElementOffsetAligned(
                range.ElementLayout,
                range.FirstElementBufferByteOffset))
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorMatrixAlignment,
                GltfValidationSeverity.Error,
                "Matrix accessor columns are not aligned to four-byte boundaries.",
                pointer);
            valid = false;
        }

        if (range.ElementByteStride < range.ElementLayout.ElementByteLength)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorSmallBytestride,
                GltfValidationSeverity.Error,
                $"Accessor element length {range.ElementLayout.ElementByteLength} exceeds byteStride {range.ElementByteStride}.",
                pointer);
            valid = false;
        }

        if (view.HasByteStride
            && view.ByteStride % range.ElementLayout.ComponentByteLength != 0)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorOffsetAlignment,
                GltfValidationSeverity.Error,
                "BufferView byteStride is not aligned to the accessor component size.",
                pointer);
            valid = false;
        }

        if (range.AccessorByteEnd < range.AccessorByteOffset
            || range.AccessorByteEnd > view.ByteLength)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorTooLong,
                GltfValidationSeverity.Error,
                "The accessor byte range exceeds its bufferView.",
                pointer);
            valid = false;
        }

        return valid;
    }

    internal bool ValidateSparseAccessorLayout(
        GltfDocument document,
        GltfAccessor accessor,
        int accessorIndex,
        GltfValidationReport report)
    {
        GltfAccessorSparse? sparse = accessor.Sparse;
        if (sparse == null)
        {
            return true;
        }

        string pointer = $"/accessors/{accessorIndex}/sparse";
        bool valid = true;
        if (sparse.Count < 1 || sparse.Count > accessor.Count)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorSparseCountOutOfRange,
                GltfValidationSeverity.Error,
                $"Sparse count {sparse.Count} is outside accessor count {accessor.Count}.",
                $"{pointer}/count");
            valid = false;
        }

        if (sparse.Indices == null || sparse.Values == null)
        {
            return false;
        }

        if (sparse.Indices.ComponentType != GltfComponentType.UnsignedByte
            && sparse.Indices.ComponentType != GltfComponentType.UnsignedShort
            && sparse.Indices.ComponentType != GltfComponentType.UnsignedInt)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                "Sparse indices require an unsigned integer component type.",
                $"{pointer}/indices/componentType");
            valid = false;
        }

        bool indicesValid = false;
        bool valuesValid = false;
        if (sparse.Count >= 0
            && (sparse.Indices.ComponentType == GltfComponentType.UnsignedByte
                || sparse.Indices.ComponentType == GltfComponentType.UnsignedShort
                || sparse.Indices.ComponentType == GltfComponentType.UnsignedInt))
        {
            GltfAccessor indices = CreateSparseIndicesAccessor(sparse);
            indicesValid = ValidateSparsePayloadLayout(
                document,
                indices,
                sparse.Indices.BufferView,
                $"{pointer}/indices",
                report);
        }

        if (sparse.Count >= 0)
        {
            GltfAccessor values = CreateSparseValuesAccessor(accessor, sparse);
            valuesValid = ValidateSparsePayloadLayout(
                document,
                values,
                sparse.Values.BufferView,
                $"{pointer}/values",
                report);
        }

        if (indicesValid && valuesValid
            && SparsePayloadRangesOverlap(document, accessor, sparse))
        {
            GltfValidationIssue issue = AddBufferAccessorIssue(
                report,
                GltfIssueCodes.BufferViewTargetOverride,
                GltfValidationSeverity.Error,
                "Sparse indices and values byte ranges overlap.",
                $"{pointer}/values/bufferView");
            issue.RelatedJsonPointers.Add($"{pointer}/indices/bufferView");
            valid = false;
        }

        if (valid && indicesValid && valuesValid)
        {
            if (!ValidateSparseIndexValues(document, accessor, accessorIndex, report))
            {
                valid = false;
            }
        }
        else
        {
            valid = false;
        }

        return valid;
    }

    internal GltfAccessor CreateSparseIndicesAccessor(GltfAccessorSparse sparse)
    {
        GltfAccessor result = new GltfAccessor();
        result.HasBufferView = true;
        result.BufferView = sparse.Indices.BufferView;
        result.HasByteOffset = sparse.Indices.HasByteOffset;
        result.ByteOffset = sparse.Indices.ByteOffset;
        result.ComponentType = sparse.Indices.ComponentType;
        result.Count = sparse.Count;
        result.Type = GltfAccessorType.Scalar;
        return result;
    }

    internal GltfAccessor CreateSparseValuesAccessor(
        GltfAccessor accessor,
        GltfAccessorSparse sparse)
    {
        GltfAccessor result = new GltfAccessor();
        result.HasBufferView = true;
        result.BufferView = sparse.Values.BufferView;
        result.HasByteOffset = sparse.Values.HasByteOffset;
        result.ByteOffset = sparse.Values.ByteOffset;
        result.ComponentType = accessor.ComponentType;
        result.Count = sparse.Count;
        result.Type = accessor.Type;
        return result;
    }

    internal bool ValidateSparsePayloadLayout(
        GltfDocument document,
        GltfAccessor payload,
        int viewIndex,
        string pointer,
        GltfValidationReport report)
    {
        GltfBufferView? view = GetValidBufferView(document, viewIndex);
        if (view == null)
        {
            return false;
        }

        AccessorByteRange range = Layout.CalculateAccessorByteRange(
            payload,
            Layout.CalculateBufferViewRange(view));
        bool valid = ValidateAccessorByteRangeRules(range, view, pointer, report);
        if (view.HasByteStride || view.HasTarget)
        {
            valid = false;
        }

        return valid;
    }

    internal bool SparsePayloadRangesOverlap(
        GltfDocument document,
        GltfAccessor accessor,
        GltfAccessorSparse sparse)
    {
        GltfBufferView? indicesView = GetValidBufferView(
            document,
            sparse.Indices.BufferView);
        GltfBufferView? valuesView = GetValidBufferView(
            document,
            sparse.Values.BufferView);
        if (indicesView == null || valuesView == null
            || indicesView.Buffer != valuesView.Buffer)
        {
            return false;
        }

        GltfAccessor indices = CreateSparseIndicesAccessor(sparse);
        AccessorByteRange indicesRange = Layout.CalculateAccessorByteRange(
            indices,
            Layout.CalculateBufferViewRange(indicesView));
        GltfAccessor values = CreateSparseValuesAccessor(accessor, sparse);
        AccessorByteRange valuesRange = Layout.CalculateAccessorByteRange(
            values,
            Layout.CalculateBufferViewRange(valuesView));
        long indicesStart = indicesRange.FirstElementBufferByteOffset;
        long indicesEnd = indicesRange.BufferByteEnd;
        long valuesStart = valuesRange.FirstElementBufferByteOffset;
        long valuesEnd = valuesRange.BufferByteEnd;
        return indicesStart < valuesEnd && valuesStart < indicesEnd;
    }

    internal bool ValidateSparseIndexValues(
        GltfDocument document,
        GltfAccessor accessor,
        int accessorIndex,
        GltfValidationReport report)
    {
        GltfAccessorSparse? sparse = accessor.Sparse;
        if (sparse == null)
        {
            return false;
        }

        GltfAccessor indices = CreateSparseIndicesAccessor(sparse);
        if (!IsAccessorPayloadAvailable(document, indices))
        {
            return false;
        }

        GltfBufferView view = document.BufferViews[indices.BufferView];
        GltfBuffer buffer = document.Buffers[view.Buffer];
        AccessorByteRange range = Layout.CalculateAccessorByteRange(
            indices,
            Layout.CalculateBufferViewRange(view));
        uint[] values = Decoder.DecodeUnsignedValues(buffer.ResolvedBytes, range);
        bool valid = true;
        uint previous = 0U;
        int index = 0;
        while (index < values.Length)
        {
            uint value = values[index];
            if ((long)value >= accessor.Count)
            {
                AddBufferAccessorIssue(
                    report,
                    GltfIssueCodes.AccessorSparseIndexOob,
                    GltfValidationSeverity.Error,
                    $"Sparse index {value} is outside accessor count {accessor.Count}.",
                    $"/accessors/{accessorIndex}/sparse/indices");
                valid = false;
            }

            if (index > 0 && value <= previous)
            {
                AddBufferAccessorIssue(
                    report,
                    GltfIssueCodes.AccessorSparseIndicesNonIncreasing,
                    GltfValidationSeverity.Error,
                    $"Sparse index {value} is not greater than {previous}.",
                    $"/accessors/{accessorIndex}/sparse/indices");
                valid = false;
            }

            previous = value;
            index++;
        }

        return valid;
    }

    internal void ValidateAccessorBoundsAndValues(
        GltfDocument document,
        GltfAccessor accessor,
        int accessorIndex,
        bool denseValid,
        bool sparseValid,
        GltfValidationReport report)
    {
        int componentCount = Layout.GetComponentCount(accessor.Type);
        string pointer = $"/accessors/{accessorIndex}";
        bool declarationsValid = ValidateDeclaredAccessorBounds(
            accessor,
            componentCount,
            pointer,
            report);
        if (!denseValid || !sparseValid
            || !IsAccessorStorageAvailable(document, accessor))
        {
            return;
        }

        AccessorBoundsSource source = Bounds.CreateBoundsSource(document, accessor);
        if (accessor.ComponentType == GltfComponentType.Float)
        {
            double[] values = Decoder.DecodeFloatingValues(source.Bytes, source.Range);
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                if (!double.IsFinite(values[valueIndex]))
                {
                    int elementIndex = valueIndex / componentCount;
                    int componentIndex = valueIndex % componentCount;
                    AddBufferAccessorIssue(
                        report,
                        GltfIssueCodes.AccessorInvalidFloat,
                        GltfValidationSeverity.Error,
                        $"Accessor element {elementIndex} component {componentIndex} is not finite.",
                        pointer);
                    break;
                }

                valueIndex++;
            }
        }

        if (!declarationsValid || accessor.Type == GltfAccessorType.Matrix2
            || accessor.Type == GltfAccessorType.Matrix3
            || accessor.Type == GltfAccessorType.Matrix4)
        {
            return;
        }

        AccessorBoundsVerification verification = Bounds.VerifyAccessorBounds(
            document,
            accessorIndex);
        if (accessor.Minimum != null && !verification.MinimumMatches)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorMinMismatch,
                GltfValidationSeverity.Error,
                "Declared accessor minimum does not match the stored values.",
                $"{pointer}/min");
        }

        if (accessor.Maximum != null && !verification.MaximumMatches)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.AccessorMaxMismatch,
                GltfValidationSeverity.Error,
                "Declared accessor maximum does not match the stored values.",
                $"{pointer}/max");
        }
    }

    internal bool ValidateDeclaredAccessorBounds(
        GltfAccessor accessor,
        int componentCount,
        string pointer,
        GltfValidationReport report)
    {
        bool valid = true;
        if (accessor.Minimum != null && accessor.Minimum.Length != componentCount)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                $"Accessor min length {accessor.Minimum.Length} does not match component count {componentCount}.",
                $"{pointer}/min");
            valid = false;
        }

        if (accessor.Maximum != null && accessor.Maximum.Length != componentCount)
        {
            AddBufferAccessorIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                $"Accessor max length {accessor.Maximum.Length} does not match component count {componentCount}.",
                $"{pointer}/max");
            valid = false;
        }

        if (accessor.Minimum != null && accessor.Minimum.Length == componentCount
            && !ValidateBoundArrayValues(
                accessor.Minimum,
                accessor.ComponentType,
                $"{pointer}/min",
                report))
        {
            valid = false;
        }

        if (accessor.Maximum != null && accessor.Maximum.Length == componentCount
            && !ValidateBoundArrayValues(
                accessor.Maximum,
                accessor.ComponentType,
                $"{pointer}/max",
                report))
        {
            valid = false;
        }

        if (accessor.Minimum != null && accessor.Maximum != null
            && accessor.Minimum.Length == componentCount
            && accessor.Maximum.Length == componentCount)
        {
            int index = 0;
            while (index < componentCount)
            {
                double minimum = Bounds.NormalizeDeclaredBound(
                    accessor.Minimum[index],
                    accessor.ComponentType);
                double maximum = Bounds.NormalizeDeclaredBound(
                    accessor.Maximum[index],
                    accessor.ComponentType);
                if (double.IsFinite(minimum) && double.IsFinite(maximum)
                    && minimum > maximum)
                {
                    AddBufferAccessorIssue(
                        report,
                        GltfIssueCodes.ValueNotInRange,
                        GltfValidationSeverity.Error,
                        $"Accessor min component {index} exceeds max.",
                        $"{pointer}/min/{index}");
                    valid = false;
                }

                index++;
            }
        }

        return valid;
    }

    internal bool ValidateBoundArrayValues(
        double[] values,
        GltfComponentType componentType,
        string pointer,
        GltfValidationReport report)
    {
        bool valid = true;
        int index = 0;
        while (index < values.Length)
        {
            double value = Bounds.NormalizeDeclaredBound(values[index], componentType);
            if (!Bounds.IsDeclaredBoundValueValid(value, componentType))
            {
                AddBufferAccessorIssue(
                    report,
                    GltfIssueCodes.ValueNotInRange,
                    GltfValidationSeverity.Error,
                    "Accessor bounds must be finite values representable by the accessor component type.",
                    $"{pointer}/{index}");
                valid = false;
            }

            index++;
        }

        return valid;
    }

    internal bool IsAccessorStorageAvailable(
        GltfDocument document,
        GltfAccessor accessor)
    {
        if (accessor.Count < 0)
        {
            return false;
        }

        AccessorElementLayout element = Layout.CalculateElementLayout(
            accessor.ComponentType,
            accessor.Type);
        if ((long)accessor.Count * element.ElementByteLength > int.MaxValue)
        {
            return false;
        }

        if (accessor.HasBufferView && !IsAccessorPayloadAvailable(document, accessor))
        {
            return false;
        }

        if (accessor.Sparse == null)
        {
            return accessor.HasBufferView;
        }

        GltfAccessorSparse sparse = accessor.Sparse;
        if (sparse.Indices == null || sparse.Values == null
            || sparse.Count < 1 || sparse.Count > accessor.Count)
        {
            return false;
        }

        GltfAccessor indices = CreateSparseIndicesAccessor(sparse);
        GltfAccessor values = CreateSparseValuesAccessor(accessor, sparse);
        return IsAccessorPayloadAvailable(document, indices)
            && IsAccessorPayloadAvailable(document, values);
    }

    internal bool IsAccessorPayloadAvailable(
        GltfDocument document,
        GltfAccessor payload)
    {
        GltfBufferView? view = GetValidBufferView(document, payload.BufferView);
        if (view == null || view.Buffer < 0 || document.Buffers == null
            || view.Buffer >= document.Buffers.Count)
        {
            return false;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        if (buffer == null || !buffer.Resolved || buffer.ResolvedBytes == null)
        {
            return false;
        }

        AccessorByteRange range = Layout.CalculateAccessorByteRange(
            payload,
            Layout.CalculateBufferViewRange(view));
        return range.AccessorByteEnd >= range.AccessorByteOffset
            && range.AccessorByteEnd <= view.ByteLength
            && range.FirstElementBufferByteOffset >= 0
            && range.BufferByteEnd >= range.FirstElementBufferByteOffset
            && range.BufferByteEnd <= buffer.ResolvedBytes.Length;
    }

    internal GltfBufferView? GetValidBufferView(
        GltfDocument document,
        int viewIndex)
    {
        if (document.BufferViews == null || viewIndex < 0
            || viewIndex >= document.BufferViews.Count)
        {
            return null;
        }

        return document.BufferViews[viewIndex];
    }

    internal GltfValidationIssue AddBufferAccessorIssue(
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

        return issue;
    }
}
