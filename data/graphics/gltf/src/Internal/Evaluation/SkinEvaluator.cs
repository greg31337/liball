using System;
using System.Collections.Generic;
using System.Numerics;

namespace gltf;

internal class SkinEvaluation
{
    internal int Skin = GltfConstants.AbsentIndex;
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal bool JointReferencesValid = true;
    internal bool HasCommonRoot;
    internal int CommonRoot = GltfConstants.AbsentIndex;
    internal bool SkeletonValid = true;
    internal bool SameScene = true;
    internal bool UsesDefaultInverseBindMatrices;
    internal bool HasPalette;
    internal List<int> Joints = new List<int>();
    internal List<int> ReferencingNodes = new List<int>();
    internal List<int> ReferencingScenes = new List<int>();
    internal List<int> CommonScenes = new List<int>();
    internal List<Matrix4x4> InverseBindMatrices = new List<Matrix4x4>();
    internal List<Matrix4x4> JointMatrices = new List<Matrix4x4>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class SkinInfluenceSet
{
    internal int Set;
    internal uint[] Joints = [];
    internal double[] Weights = [];
}

internal class SkinInfluenceValidation
{
    internal bool IsValid = true;
    internal int VertexCount;
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class SkinEvaluator
{
    internal SkinEvaluation EvaluateSkin(GltfDocument document, int skinIndex)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        SkinEvaluation result = new SkinEvaluation();
        result.Skin = skinIndex;
        if (document.Skins == null
            || skinIndex < 0
            || skinIndex >= document.Skins.Count)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Skin index {skinIndex} does not resolve to a skin.",
                $"/skins/{skinIndex}");
            return result;
        }

        GltfSkin skin = document.Skins[skinIndex];
        if (skin == null)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The selected skin is null.",
                $"/skins/{skinIndex}");
            return result;
        }

        SceneGraphAnalysis graph = new SceneGraphEvaluator().AnalyzeDocument(document);
        if (document.Nodes == null)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The node collection is missing.",
                "/nodes");
            return result;
        }

        ValidateJointReferences(document, skin, result, graph);
        if (result.JointReferencesValid)
        {
            FindCommonJointRoot(result, graph);
            CollectCommonJointScenes(result, graph);
        }

        ValidateSkeletonReference(document, skin, result, graph);
        AnalyzeSkinSceneMembership(document, result, graph);
        ReadInverseBindMatrices(document, skin, result);
        CalculateJointPalette(document, result);
        return result;
    }

    internal void ValidateJointReferences(
        GltfDocument document,
        GltfSkin skin,
        SkinEvaluation result,
        SceneGraphAnalysis graph)
    {
        string pointer = $"/skins/{result.Skin}/joints";
        if (skin.Joints == null || skin.Joints.Count == 0)
        {
            result.JointReferencesValid = false;
            AddSkinIssue(
                result,
                GltfIssueCodes.EmptyEntity,
                GltfValidationSeverity.Error,
                "A skin must contain at least one joint.",
                pointer);
            return;
        }

        bool[] referencedNodes = new bool[document.Nodes.Count];
        int jointPosition = 0;
        while (jointPosition < skin.Joints.Count)
        {
            int jointIndex = skin.Joints[jointPosition];
            result.Joints.Add(jointIndex);
            string jointPointer = $"{pointer}/{jointPosition}";
            if (jointIndex < 0 || jointIndex >= document.Nodes.Count)
            {
                result.JointReferencesValid = false;
                AddSkinIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    GltfValidationSeverity.Error,
                    $"Joint node index {jointIndex} does not resolve to a node.",
                    jointPointer);
            }
            else if (document.Nodes[jointIndex] == null)
            {
                result.JointReferencesValid = false;
                AddSkinIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    GltfValidationSeverity.Error,
                    $"Joint node index {jointIndex} resolves to a null node.",
                    jointPointer);
            }
            else if (referencedNodes[jointIndex])
            {
                result.JointReferencesValid = false;
                AddSkinIssue(
                    result,
                    GltfIssueCodes.DuplicateElements,
                    GltfValidationSeverity.Error,
                    $"Joint node index {jointIndex} is listed more than once.",
                    jointPointer);
            }
            else if (HasCycleInAncestorPath(jointIndex, graph)
                || HasMultipleParentInAncestorPath(jointIndex, graph))
            {
                result.JointReferencesValid = false;
                AddSkinIssue(
                    result,
                    HasCycleInAncestorPath(jointIndex, graph)
                        ? GltfIssueCodes.NodeLoop
                        : GltfIssueCodes.NodeParentOverride,
                    GltfValidationSeverity.Error,
                    $"Joint node {jointIndex} does not belong to a strict node tree.",
                    jointPointer);
            }

            if (jointIndex >= 0 && jointIndex < document.Nodes.Count)
            {
                referencedNodes[jointIndex] = true;
            }

            jointPosition++;
        }
    }

    internal bool HasCycleInAncestorPath(int nodeIndex, SceneGraphAnalysis graph)
    {
        int current = nodeIndex;
        int steps = 0;
        while (current != GltfConstants.AbsentIndex
            && steps <= graph.Parents.Count)
        {
            if (graph.CycleNodes[current])
            {
                return true;
            }

            current = graph.Parents[current];
            steps++;
        }

        return current != GltfConstants.AbsentIndex;
    }

    internal bool HasMultipleParentInAncestorPath(
        int nodeIndex,
        SceneGraphAnalysis graph)
    {
        int current = nodeIndex;
        int steps = 0;
        while (current != GltfConstants.AbsentIndex
            && steps <= graph.Parents.Count)
        {
            if (graph.ParentReferences[current].Count > 1)
            {
                return true;
            }

            current = graph.Parents[current];
            steps++;
        }

        return false;
    }

    internal void FindCommonJointRoot(SkinEvaluation result, SceneGraphAnalysis graph)
    {
        int candidate = result.Joints[0];
        int steps = 0;
        while (candidate != GltfConstants.AbsentIndex
            && steps <= graph.Parents.Count)
        {
            if (IsAncestorOfAllJoints(candidate, result.Joints, graph))
            {
                result.HasCommonRoot = true;
                result.CommonRoot = candidate;
                return;
            }

            candidate = graph.Parents[candidate];
            steps++;
        }

        AddSkinIssue(
            result,
            GltfIssueCodes.SkinNoCommonRoot,
            GltfValidationSeverity.Error,
            "The skin joints do not have a common root.",
            $"/skins/{result.Skin}/joints");
    }

    internal bool IsAncestorOfAllJoints(
        int candidate,
        List<int> joints,
        SceneGraphAnalysis graph)
    {
        int jointPosition = 0;
        while (jointPosition < joints.Count)
        {
            if (!IsNodeAncestor(candidate, joints[jointPosition], graph))
            {
                return false;
            }

            jointPosition++;
        }

        return true;
    }

    internal bool IsNodeAncestor(
        int candidate,
        int nodeIndex,
        SceneGraphAnalysis graph)
    {
        int current = nodeIndex;
        int steps = 0;
        while (current != GltfConstants.AbsentIndex
            && steps <= graph.Parents.Count)
        {
            if (current == candidate)
            {
                return true;
            }

            if (graph.ParentReferences[current].Count > 1)
            {
                return false;
            }

            current = graph.Parents[current];
            steps++;
        }

        return false;
    }

    internal void CollectCommonJointScenes(
        SkinEvaluation result,
        SceneGraphAnalysis graph)
    {
        int sceneIndex = 0;
        while (sceneIndex < graph.SceneTraversals.Count)
        {
            bool containsAllJoints = true;
            int jointPosition = 0;
            while (jointPosition < result.Joints.Count)
            {
                if (!graph.NodeScenes[result.Joints[jointPosition]].Contains(sceneIndex))
                {
                    containsAllJoints = false;
                }

                jointPosition++;
            }

            if (containsAllJoints)
            {
                result.CommonScenes.Add(sceneIndex);
            }

            sceneIndex++;
        }
    }

    internal void ValidateSkeletonReference(
        GltfDocument document,
        GltfSkin skin,
        SkinEvaluation result,
        SceneGraphAnalysis graph)
    {
        if (!skin.HasSkeleton)
        {
            return;
        }

        if (!result.HasCommonRoot)
        {
            result.SkeletonValid = false;
            return;
        }

        if (skin.Skeleton < 0
            || skin.Skeleton >= document.Nodes.Count
            || document.Nodes[skin.Skeleton] == null)
        {
            result.SkeletonValid = false;
            AddSkinIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Skeleton node index {skin.Skeleton} does not resolve to a node.",
                $"/skins/{result.Skin}/skeleton");
            return;
        }

        if (!IsNodeAncestor(skin.Skeleton, result.CommonRoot, graph))
        {
            result.SkeletonValid = false;
            AddSkinIssue(
                result,
                GltfIssueCodes.SkinSkeletonInvalid,
                GltfValidationSeverity.Error,
                "The skeleton node is not the common root or one of its ancestors.",
                $"/skins/{result.Skin}/skeleton");
        }
    }

    internal void AnalyzeSkinSceneMembership(
        GltfDocument document,
        SkinEvaluation result,
        SceneGraphAnalysis graph)
    {
        bool[] referencedScenes = new bool[graph.SceneTraversals.Count];
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node != null && node.HasSkin && node.Skin == result.Skin)
            {
                result.ReferencingNodes.Add(nodeIndex);
                int scenePosition = 0;
                while (scenePosition < graph.NodeScenes[nodeIndex].Count)
                {
                    int sceneIndex = graph.NodeScenes[nodeIndex][scenePosition];
                    referencedScenes[sceneIndex] = true;

                    if (result.JointReferencesValid
                        && !DoesSceneContainAllJoints(sceneIndex, result.Joints, graph))
                    {
                        result.SameScene = false;
                        AddSkinIssue(
                            result,
                            GltfIssueCodes.NodeSkinNoScene,
                            GltfValidationSeverity.Error,
                            $"Scene {sceneIndex} contains skinned node {nodeIndex} but not every joint.",
                            $"/nodes/{nodeIndex}/skin");
                    }

                    scenePosition++;
                }
            }

            nodeIndex++;
        }

        int referencedSceneIndex = 0;
        while (referencedSceneIndex < referencedScenes.Length)
        {
            if (referencedScenes[referencedSceneIndex])
            {
                result.ReferencingScenes.Add(referencedSceneIndex);
            }

            referencedSceneIndex++;
        }
    }

    internal bool DoesSceneContainAllJoints(
        int sceneIndex,
        List<int> joints,
        SceneGraphAnalysis graph)
    {
        int jointPosition = 0;
        while (jointPosition < joints.Count)
        {
            if (!graph.NodeScenes[joints[jointPosition]].Contains(sceneIndex))
            {
                return false;
            }

            jointPosition++;
        }

        return true;
    }

    internal void ReadInverseBindMatrices(
        GltfDocument document,
        GltfSkin skin,
        SkinEvaluation result)
    {
        if (!skin.HasInverseBindMatrices)
        {
            result.UsesDefaultInverseBindMatrices = true;
            int jointPosition = 0;
            while (jointPosition < result.Joints.Count)
            {
                result.InverseBindMatrices.Add(Matrix4x4.Identity);
                jointPosition++;
            }

            return;
        }

        string pointer = $"/skins/{result.Skin}/inverseBindMatrices";
        if (document.Accessors == null
            || skin.InverseBindMatrices < 0
            || skin.InverseBindMatrices >= document.Accessors.Count
            || document.Accessors[skin.InverseBindMatrices] == null)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "The inverse bind matrices accessor does not resolve.",
                pointer);
            return;
        }

        GltfAccessor accessor = document.Accessors[skin.InverseBindMatrices];
        if (accessor.ComponentType != GltfComponentType.Float
            || accessor.Type != GltfAccessorType.Matrix4)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.SkinIbmInvalidFormat,
                GltfValidationSeverity.Error,
                "The inverse bind accessor must be FLOAT MAT4 with at least one element per joint.",
                pointer);
            return;
        }

        if (accessor.Count < result.Joints.Count)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.InvalidIbmAccessorCount,
                GltfValidationSeverity.Error,
                $"The inverse bind accessor contains {accessor.Count} matrices for {result.Joints.Count} joints.",
                pointer);
            return;
        }

        if (accessor.HasBufferView
            && document.BufferViews != null
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count
            && document.BufferViews[accessor.BufferView] != null
            && document.BufferViews[accessor.BufferView].HasByteStride)
        {
            AddSkinIssue(
                result,
                GltfIssueCodes.SkinIbmAccessorWithBytestride,
                GltfValidationSeverity.Error,
                "The inverse bind matrices buffer view must not define byteStride.",
                pointer);
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = skin.InverseBindMatrices;
        GltfAccessorData data = new GltfAccessorCodec().ReadMatrices(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            if (result.Status == GltfOperationStatus.Success)
            {
                result.Status = data.Status;
            }

            result.IsValid = false;
            if (data.Diagnostic != null)
            {
                result.Diagnostics.Add(data.Diagnostic);
            }

            return;
        }

        CoordinateConverter matrices = new CoordinateConverter();
        int jointIndex = 0;
        while (jointIndex < result.Joints.Count)
        {
            Matrix4x4 matrix = matrices.ReadMatrixAt(data.MatrixValues, jointIndex * 16);
            result.InverseBindMatrices.Add(matrix);
            if (!IsValidInverseBindMatrix(matrix))
            {
                AddSkinIssue(
                    result,
                    GltfIssueCodes.AccessorInvalidIbm,
                    GltfValidationSeverity.Error,
                    $"Inverse bind matrix {jointIndex} is not a finite affine matrix.",
                    $"/accessors/{skin.InverseBindMatrices}/{jointIndex}");
            }

            jointIndex++;
        }
    }

    internal bool IsValidInverseBindMatrix(Matrix4x4 matrix)
    {
        return new TransformMath().IsFiniteMatrix(matrix)
            && matrix.M14 == 0.0f
            && matrix.M24 == 0.0f
            && matrix.M34 == 0.0f
            && matrix.M44 == 1.0f;
    }

    internal void CalculateJointPalette(GltfDocument document, SkinEvaluation result)
    {
        if (result.InverseBindMatrices.Count != result.Joints.Count)
        {
            return;
        }

        SceneTransformAnalysis transforms = new TransformMath().EvaluateDocumentTransforms(document);
        bool complete = true;
        int jointPosition = 0;
        while (jointPosition < result.Joints.Count)
        {
            int jointIndex = result.Joints[jointPosition];
            if (jointIndex < 0
                || jointIndex >= transforms.Nodes.Count
                || !transforms.Nodes[jointIndex].HasWorldTransform)
            {
                complete = false;
                result.JointMatrices.Add(Matrix4x4.Identity);
                AddSkinIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    GltfValidationSeverity.Error,
                    $"The world transform for joint node {jointIndex} is unavailable.",
                    $"/skins/{result.Skin}/joints/{jointPosition}");
                jointPosition++;
                continue;
            }

            NodeTransformEvaluation jointTransform = transforms.Nodes[jointIndex];
            Matrix4x4 matrix = result.InverseBindMatrices[jointPosition]
                * jointTransform.WorldTransform;
            if (!new TransformMath().IsFiniteMatrix(matrix))
            {
                complete = false;
                result.JointMatrices.Add(Matrix4x4.Identity);
                AddSkinIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    GltfValidationSeverity.Error,
                    $"The palette matrix for joint node {jointIndex} is not finite.",
                    $"/skins/{result.Skin}/joints/{jointPosition}");
            }
            else
            {
                result.JointMatrices.Add(matrix);
            }

            int diagnosticIndex = 0;
            while (diagnosticIndex < jointTransform.Diagnostics.Count)
            {
                GltfValidationIssue diagnostic = jointTransform.Diagnostics[diagnosticIndex];
                result.Diagnostics.Add(diagnostic);
                if (diagnostic.Severity == GltfValidationSeverity.Error)
                {
                    result.IsValid = false;
                    result.Status = GltfOperationStatus.Invalid;
                }

                diagnosticIndex++;
            }

            jointPosition++;
        }

        result.HasPalette = complete;
    }

    internal SkinInfluenceValidation ValidateInfluences(
        List<SkinInfluenceSet> sets,
        int jointCount,
        int skinIndex)
    {
        if (sets == null)
        {
            throw new ArgumentNullException("sets");
        }

        SkinInfluenceValidation result = new SkinInfluenceValidation();
        if (jointCount <= 0)
        {
            AddInfluenceIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "The skin joint count must be positive.",
                $"/skins/{skinIndex}/joints");
            return result;
        }

        if (!ValidateInfluenceSetLayouts(sets, result, skinIndex))
        {
            return result;
        }

        int vertexIndex = 0;
        while (vertexIndex < result.VertexCount)
        {
            ValidateVertexInfluences(
                sets,
                result,
                jointCount,
                skinIndex,
                vertexIndex);
            vertexIndex++;
        }

        return result;
    }

    internal bool ValidateInfluenceSetLayouts(
        List<SkinInfluenceSet> sets,
        SkinInfluenceValidation result,
        int skinIndex)
    {
        if (sets.Count == 0)
        {
            AddInfluenceIssue(
                result,
                GltfIssueCodes.EmptyEntity,
                GltfValidationSeverity.Error,
                "At least one joint and weight influence set is required.",
                $"/skins/{skinIndex}/influences");
            return false;
        }

        int expectedValueCount = -1;
        int setPosition = 0;
        while (setPosition < sets.Count)
        {
            SkinInfluenceSet set = sets[setPosition];
            string pointer = $"/skins/{skinIndex}/influences/{setPosition}";
            if (set == null || set.Joints == null || set.Weights == null)
            {
                AddInfluenceIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The influence set is incomplete.",
                    pointer);
                return false;
            }

            if (set.Joints.Length != set.Weights.Length
                || set.Joints.Length % 4 != 0)
            {
                AddInfluenceIssue(
                    result,
                    GltfIssueCodes.MeshPrimitiveJointsWeightsMismatch,
                    GltfValidationSeverity.Error,
                    "Joint and weight arrays must have equal VEC4 layouts.",
                    pointer);
                return false;
            }

            if (expectedValueCount < 0)
            {
                expectedValueCount = set.Joints.Length;
                result.VertexCount = expectedValueCount / 4;
            }
            else if (set.Joints.Length != expectedValueCount)
            {
                AddInfluenceIssue(
                    result,
                    GltfIssueCodes.MeshPrimitiveUnequalAccessorCount,
                    GltfValidationSeverity.Error,
                    "Every influence set must contain the same vertex count.",
                    pointer);
                return false;
            }

            setPosition++;
        }

        return true;
    }

    internal void ValidateVertexInfluences(
        List<SkinInfluenceSet> sets,
        SkinInfluenceValidation result,
        int jointCount,
        int skinIndex,
        int vertexIndex)
    {
        List<uint> usedJoints = new List<uint>();
        double weightSum = 0.0;
        int nonZeroWeightCount = 0;
        int setPosition = 0;
        while (setPosition < sets.Count)
        {
            SkinInfluenceSet set = sets[setPosition];
            int componentIndex = 0;
            while (componentIndex < 4)
            {
                int valueIndex = vertexIndex * 4 + componentIndex;
                uint joint = set.Joints[valueIndex];
                double weight = set.Weights[valueIndex];
                string pointer = $"/skins/{skinIndex}/influences/{set.Set}/{vertexIndex}/{componentIndex}";
                if (!double.IsFinite(weight))
                {
                    AddInfluenceIssue(
                        result,
                        GltfIssueCodes.AccessorInvalidFloat,
                        GltfValidationSeverity.Error,
                        "A skin weight is not finite.",
                        pointer);
                }
                else
                {
                    weightSum += weight;
                    if (weight < 0.0)
                    {
                        AddInfluenceIssue(
                            result,
                            GltfIssueCodes.AccessorWeightsNegative,
                            GltfValidationSeverity.Error,
                            $"Skin weight {weight} is negative.",
                            pointer);
                    }

                    if (weight != 0.0)
                    {
                        nonZeroWeightCount++;
                        if (usedJoints.Contains(joint))
                        {
                            AddInfluenceIssue(
                                result,
                                GltfIssueCodes.AccessorJointsIndexDuplicate,
                                GltfValidationSeverity.Error,
                                $"Joint index {joint} has more than one non-zero influence.",
                                pointer);
                        }
                        else
                        {
                            usedJoints.Add(joint);
                        }
                    }
                    else if (joint != 0U)
                    {
                        AddInfluenceIssue(
                            result,
                            GltfIssueCodes.AccessorJointsUsedZeroWeight,
                            GltfValidationSeverity.Warning,
                            $"Non-zero joint index {joint} is paired with a zero weight.",
                            pointer);
                    }
                }

                if (joint >= (uint)jointCount)
                {
                    AddInfluenceIssue(
                        result,
                        GltfIssueCodes.AccessorJointsIndexOob,
                        GltfValidationSeverity.Error,
                        $"Joint index {joint} exceeds the maximum index {jointCount - 1}.",
                        pointer);
                }

                componentIndex++;
            }

            setPosition++;
        }

        double tolerance = 0.0000002 * nonZeroWeightCount;
        if (!double.IsFinite(weightSum) || Math.Abs(weightSum - 1.0) > tolerance)
        {
            AddInfluenceIssue(
                result,
                GltfIssueCodes.AccessorWeightsNonNormalized,
                GltfValidationSeverity.Error,
                $"The skin weights for vertex {vertexIndex} sum to {weightSum}.",
                $"/skins/{skinIndex}/influences/{vertexIndex}");
        }
    }

    internal void AddSkinIssue(
        SkinEvaluation result,
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
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            result.IsValid = false;
            if (result.Status == GltfOperationStatus.Success)
            {
                result.Status = GltfOperationStatus.Invalid;
            }
        }
    }

    internal void AddInfluenceIssue(
        SkinInfluenceValidation result,
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
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            result.IsValid = false;
        }
    }
}
