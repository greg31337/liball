using System;
using System.Collections.Generic;
using System.Numerics;

namespace gltf;

internal class PrimitiveBoundsEvaluation
{
    internal int Node = GltfConstants.AbsentIndex;
    internal int Mesh = GltfConstants.AbsentIndex;
    internal int Primitive = GltfConstants.AbsentIndex;
    internal GltfBounds Bounds = new GltfBounds();
}

internal class MeshBoundsEvaluation
{
    internal int Node = GltfConstants.AbsentIndex;
    internal int Mesh = GltfConstants.AbsentIndex;
    internal GltfBounds Bounds = new GltfBounds();
    internal List<PrimitiveBoundsEvaluation> Primitives = new List<PrimitiveBoundsEvaluation>();
}

internal class SceneBoundsEvaluation
{
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal GltfBounds Bounds = new GltfBounds();
    internal List<MeshBoundsEvaluation> Meshes = new List<MeshBoundsEvaluation>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class BoundsCalculator
{
    internal AccessorCodecEngine AccessorCodec = new AccessorCodecEngine();

    internal SceneBoundsEvaluation CalculateSceneBounds(
        GltfDocument document,
        List<int> nodeIndices,
        SceneTransformAnalysis transforms,
        bool includeMorphTargets)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (nodeIndices == null)
        {
            throw new ArgumentNullException("nodeIndices");
        }

        if (transforms == null)
        {
            throw new ArgumentNullException("transforms");
        }

        SceneBoundsEvaluation result = new SceneBoundsEvaluation();
        int position = 0;
        while (position < nodeIndices.Count)
        {
            int nodeIndex = nodeIndices[position];
            if (document.Nodes == null
                || nodeIndex < 0
                || nodeIndex >= document.Nodes.Count
                || document.Nodes[nodeIndex] == null
                || nodeIndex >= transforms.Nodes.Count
                || !transforms.Nodes[nodeIndex].HasWorldTransform)
            {
                AddBoundsIssue(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.UnresolvedReference,
                    $"Scene node index {nodeIndex} cannot be evaluated for bounds.",
                    $"/nodes/{nodeIndex}");
            }
            else
            {
                CalculateNodeBounds(
                    document,
                    nodeIndex,
                    transforms.Nodes[nodeIndex].WorldTransform,
                    includeMorphTargets,
                    result);
            }

            position++;
        }

        return result;
    }

    internal void CalculateNodeBounds(
        GltfDocument document,
        int nodeIndex,
        Matrix4x4 worldTransform,
        bool includeMorphTargets,
        SceneBoundsEvaluation result)
    {
        GltfNode node = document.Nodes[nodeIndex];
        if (!node.HasMesh)
        {
            return;
        }

        if (document.Meshes == null
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count
            || document.Meshes[node.Mesh] == null)
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                $"Node mesh index {node.Mesh} does not resolve to a mesh.",
                $"/nodes/{nodeIndex}/mesh");
            return;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
        MeshBoundsEvaluation meshResult = new MeshBoundsEvaluation();
        meshResult.Node = nodeIndex;
        meshResult.Mesh = node.Mesh;
        result.Meshes.Add(meshResult);
        if (mesh.Primitives == null)
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "The mesh primitive collection is missing.",
                $"/meshes/{node.Mesh}/primitives");
            return;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive == null)
            {
                AddBoundsIssue(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The mesh primitive is null.",
                    $"/meshes/{node.Mesh}/primitives/{primitiveIndex}");
            }
            else
            {
                PrimitiveBoundsEvaluation primitiveResult = CalculatePrimitiveBounds(
                    document,
                    node,
                    nodeIndex,
                    node.Mesh,
                    primitive,
                    primitiveIndex,
                    worldTransform,
                    includeMorphTargets,
                    result);
                meshResult.Primitives.Add(primitiveResult);
                IncludeBounds(meshResult.Bounds, primitiveResult.Bounds);
                IncludeBounds(result.Bounds, primitiveResult.Bounds);
            }

            primitiveIndex++;
        }
    }

    internal PrimitiveBoundsEvaluation CalculatePrimitiveBounds(
        GltfDocument document,
        GltfNode node,
        int nodeIndex,
        int meshIndex,
        GltfMeshPrimitive primitive,
        int primitiveIndex,
        Matrix4x4 worldTransform,
        bool includeMorphTargets,
        SceneBoundsEvaluation result)
    {
        PrimitiveBoundsEvaluation primitiveResult = new PrimitiveBoundsEvaluation();
        primitiveResult.Node = nodeIndex;
        primitiveResult.Mesh = meshIndex;
        primitiveResult.Primitive = primitiveIndex;
        string pointer = $"/meshes/{meshIndex}/primitives/{primitiveIndex}";
        int positionAccessor = FindPositionAccessor(primitive.Attributes);
        if (positionAccessor == GltfConstants.AbsentIndex)
        {
            return primitiveResult;
        }

        double[]? positions = ReadPositionValues(
            document,
            positionAccessor,
            result,
            $"{pointer}/attributes/POSITION");
        if (positions == null)
        {
            return primitiveResult;
        }

        int vertexCount = positions.Length / 3;
        uint[]? indices = null;
        if (primitive.HasIndices)
        {
            indices = ReadPrimitiveIndices(
                document,
                primitive.Indices,
                result,
                $"{pointer}/indices");
            if (indices == null)
            {
                return primitiveResult;
            }
        }

        List<double[]> morphPositions = new List<double[]>();
        double[] weights = [];
        if (includeMorphTargets)
        {
            weights = GetMorphWeights(
                document.Meshes[meshIndex],
                node,
                nodeIndex,
                meshIndex,
                primitive,
                result);
            PrepareMorphPositions(
                document,
                primitive,
                vertexCount,
                weights,
                morphPositions,
                result,
                pointer);
        }

        int elementCount = vertexCount;
        if (indices != null)
        {
            elementCount = indices.Length;
        }

        int elementIndex = 0;
        while (elementIndex < elementCount)
        {
            int vertexIndex = elementIndex;
            if (indices != null)
            {
                if ((long)indices[elementIndex] >= vertexCount)
                {
                    AddBoundsIssue(
                        result,
                        GltfOperationStatus.Invalid,
                        GltfIssueCodes.AccessorIndexOob,
                        $"Primitive index {indices[elementIndex]} is outside vertex count {vertexCount}.",
                        $"/accessors/{primitive.Indices}/{elementIndex}");
                    elementIndex++;
                    continue;
                }

                vertexIndex = (int)indices[elementIndex];
            }

            IncludeVertex(
                positions,
                vertexIndex,
                morphPositions,
                weights,
                worldTransform,
                primitiveResult.Bounds,
                result,
                pointer);
            elementIndex++;
        }

        return primitiveResult;
    }

    internal int FindPositionAccessor(List<GltfAttribute> attributes)
    {
        if (attributes == null)
        {
            return GltfConstants.AbsentIndex;
        }

        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            GltfAttribute attribute = attributes[attributeIndex];
            if (attribute != null && attribute.Semantic == "POSITION")
            {
                return attribute.Accessor;
            }

            attributeIndex++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal double[]? ReadPositionValues(
        GltfDocument document,
        int accessorIndex,
        SceneBoundsEvaluation result,
        string pointer)
    {
        if (document.Accessors == null
            || accessorIndex < 0
            || accessorIndex >= document.Accessors.Count
            || document.Accessors[accessorIndex] == null)
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                $"Position accessor index {accessorIndex} does not resolve.",
                pointer);
            return null;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.Type != GltfAccessorType.Vector3
            || accessor.ComponentType == GltfComponentType.UnsignedInt)
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "Position data must use a supported VEC3 accessor.",
                pointer);
            return null;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        request.ApplyNormalization = true;
        GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            CopyAccessorFailure(result, data, pointer);
            return null;
        }

        return data.FloatingValues;
    }

    internal uint[]? ReadPrimitiveIndices(
        GltfDocument document,
        int accessorIndex,
        SceneBoundsEvaluation result,
        string pointer)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData data = AccessorCodec.ReadIndexData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            CopyAccessorFailure(result, data, pointer);
            return null;
        }

        return data.Indices;
    }

    internal double[] GetMorphWeights(
        GltfMesh mesh,
        GltfNode node,
        int nodeIndex,
        int meshIndex,
        GltfMeshPrimitive primitive,
        SceneBoundsEvaluation result)
    {
        int targetCount = 0;
        if (primitive.Targets != null)
        {
            targetCount = primitive.Targets.Count;
        }

        double[] weights = new double[targetCount];
        double[]? source = node.Weights;
        string issueCode = GltfIssueCodes.NodeWeightsInvalid;
        string weightPointer = $"/nodes/{nodeIndex}/weights";
        if (source == null)
        {
            source = mesh.Weights;
            issueCode = GltfIssueCodes.MeshInvalidWeightsCount;
            weightPointer = $"/meshes/{meshIndex}/weights";
        }

        if (source == null)
        {
            return weights;
        }

        if (source.Length != targetCount)
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                issueCode,
                "Morph weights must match the primitive morph target count.",
                weightPointer);
            return weights;
        }

        int weightIndex = 0;
        while (weightIndex < source.Length)
        {
            if (!double.IsFinite(source[weightIndex]))
            {
                AddBoundsIssue(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.AccessorInvalidFloat,
                    "Morph weights must be finite.",
                    $"{weightPointer}/{weightIndex}");
            }
            else
            {
                weights[weightIndex] = source[weightIndex];
            }

            weightIndex++;
        }

        return weights;
    }

    internal void PrepareMorphPositions(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        int vertexCount,
        double[] weights,
        List<double[]> morphPositions,
        SceneBoundsEvaluation result,
        string pointer)
    {
        if (primitive.Targets == null)
        {
            return;
        }

        int targetIndex = 0;
        while (targetIndex < primitive.Targets.Count)
        {
            double[] values = [];
            GltfMorphTarget target = primitive.Targets[targetIndex];
            if (target != null && targetIndex < weights.Length && weights[targetIndex] != 0.0)
            {
                int accessorIndex = FindPositionAccessor(target.Attributes);
                if (accessorIndex != GltfConstants.AbsentIndex)
                {
                    double[]? decoded = ReadPositionValues(
                        document,
                        accessorIndex,
                        result,
                        $"{pointer}/targets/{targetIndex}/POSITION");
                    if (decoded != null)
                    {
                        if (decoded.Length != vertexCount * 3)
                        {
                            AddBoundsIssue(
                                result,
                                GltfOperationStatus.Invalid,
                                GltfIssueCodes.MeshPrimitiveMorphTargetInvalidAttributeCount,
                                "Morph position data must match the base vertex count.",
                                $"{pointer}/targets/{targetIndex}/POSITION");
                        }
                        else
                        {
                            values = decoded;
                        }
                    }
                }
            }

            morphPositions.Add(values);
            targetIndex++;
        }
    }

    internal void IncludeVertex(
        double[] positions,
        int vertexIndex,
        List<double[]> morphPositions,
        double[] weights,
        Matrix4x4 worldTransform,
        GltfBounds bounds,
        SceneBoundsEvaluation result,
        string pointer)
    {
        int offset = vertexIndex * 3;
        double x = positions[offset];
        double y = positions[offset + 1];
        double z = positions[offset + 2];
        int targetIndex = 0;
        while (targetIndex < morphPositions.Count)
        {
            double[] target = morphPositions[targetIndex];
            if (target.Length > 0 && targetIndex < weights.Length)
            {
                x += target[offset] * weights[targetIndex];
                y += target[offset + 1] * weights[targetIndex];
                z += target[offset + 2] * weights[targetIndex];
            }

            targetIndex++;
        }

        if (!IsRepresentableCoordinate(x)
            || !IsRepresentableCoordinate(y)
            || !IsRepresentableCoordinate(z))
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "A position is not finite or representable for bounds calculation.",
                pointer);
            return;
        }

        Vector3 transformed = Vector3.Transform(
            new Vector3((float)x, (float)y, (float)z),
            worldTransform);
        if (!float.IsFinite(transformed.X)
            || !float.IsFinite(transformed.Y)
            || !float.IsFinite(transformed.Z))
        {
            AddBoundsIssue(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "A transformed position is not finite.",
                pointer);
            return;
        }

        IncludePoint(bounds, transformed);
    }

    internal bool IsRepresentableCoordinate(double value)
    {
        return double.IsFinite(value) && Math.Abs(value) <= float.MaxValue;
    }

    internal void IncludePoint(GltfBounds bounds, Vector3 point)
    {
        if (bounds.IsEmpty)
        {
            bounds.IsEmpty = false;
            bounds.Minimum = point;
            bounds.Maximum = point;
            return;
        }

        bounds.Minimum = Vector3.Min(bounds.Minimum, point);
        bounds.Maximum = Vector3.Max(bounds.Maximum, point);
    }

    internal void IncludeBounds(GltfBounds destination, GltfBounds source)
    {
        if (source.IsEmpty)
        {
            return;
        }

        IncludePoint(destination, source.Minimum);
        IncludePoint(destination, source.Maximum);
    }

    internal void CopyAccessorFailure(
        SceneBoundsEvaluation result,
        GltfAccessorData data,
        string pointer)
    {
        GltfValidationIssue issue = data.Diagnostic ?? new GltfValidationIssue();
        if (data.Diagnostic == null)
        {
            issue.Code = GltfIssueCodes.OperationUnavailable;
            issue.Severity = GltfValidationSeverity.Error;
            issue.Message = "Accessor data is unavailable for bounds calculation.";
            issue.JsonPointer = pointer;
        }

        result.Diagnostics.Add(issue);
        result.IsValid = false;
        MergeBoundsStatus(result, data.Status);
    }

    internal void AddBoundsIssue(
        SceneBoundsEvaluation result,
        GltfOperationStatus status,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.IsValid = false;
        MergeBoundsStatus(result, status);
    }

    internal void MergeBoundsStatus(SceneBoundsEvaluation result, GltfOperationStatus status)
    {
        if (status == GltfOperationStatus.Invalid
            || status == GltfOperationStatus.Failed
            || result.Status == GltfOperationStatus.Success)
        {
            result.Status = status;
        }
    }
}
