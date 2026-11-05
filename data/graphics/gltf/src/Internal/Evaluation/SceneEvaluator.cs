using System;
using System.Collections.Generic;

namespace gltf;

internal class SceneSelection
{
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal int Scene = GltfConstants.AbsentIndex;
    internal List<int> Nodes = new List<int>();
    internal List<bool> ActiveNodes = new List<bool>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class SceneEvaluator
{
    internal TransformMath TransformMath = new TransformMath();
    internal CameraProjectionCalculator ProjectionCalculator = new CameraProjectionCalculator();
    internal SkinEvaluator SkinEvaluator = new SkinEvaluator();
    internal BoundsCalculator BoundsCalculator = new BoundsCalculator();
    internal SceneExtensionEvaluator ExtensionEvaluator = new SceneExtensionEvaluator();

    internal GltfScenePose EvaluateDocumentScene(
        GltfDocument document,
        GltfSceneEvaluationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfScenePose result = new GltfScenePose();
        SceneTransformAnalysis transforms = TransformMath.EvaluateDocumentTransforms(document);
        CopyTransformIssues(result, transforms);
        if (!transforms.IsValid)
        {
            result.Status = GltfOperationStatus.Invalid;
        }

        SceneSelection selection = SelectScene(document, options, transforms.Graph);
        result.Scene = selection.Scene;
        CopySelectionIssues(result, selection);
        MergeSceneStatus(result, selection.Status);
        PopulateNodePoses(result, transforms);
        ExtensionEvaluator.PopulateSceneExtensionMetadata(
            document,
            result,
            transforms.Graph);
        if (!selection.IsValid)
        {
            return result;
        }

        if (options.CalculateCameras)
        {
            CalculateNodeProjections(document, result, selection);
        }

        if (options.CalculateSkins)
        {
            CalculateNodeSkins(document, result, selection);
        }

        if (options.CalculateBounds)
        {
            SceneBoundsEvaluation bounds = BoundsCalculator.CalculateSceneBounds(
                document,
                selection.Nodes,
                transforms,
                options.IncludeMorphTargets);
            result.HasBounds = bounds.Status == GltfOperationStatus.Success;
            result.Bounds = bounds.Bounds;
            CopyBoundsIssues(result, bounds);
            MergeSceneStatus(result, bounds.Status);
        }

        return result;
    }

    internal SceneBoundsEvaluation CalculateDocumentSceneBounds(
        GltfDocument document,
        GltfSceneEvaluationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        SceneTransformAnalysis transforms = TransformMath.EvaluateDocumentTransforms(document);
        SceneSelection selection = SelectScene(document, options, transforms.Graph);
        SceneBoundsEvaluation result = new SceneBoundsEvaluation();
        if (!transforms.IsValid)
        {
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
        }

        int issueIndex = 0;
        while (issueIndex < transforms.Diagnostics.Count)
        {
            result.Diagnostics.Add(transforms.Diagnostics[issueIndex]);
            issueIndex++;
        }

        issueIndex = 0;
        while (issueIndex < selection.Diagnostics.Count)
        {
            result.Diagnostics.Add(selection.Diagnostics[issueIndex]);
            issueIndex++;
        }

        if (!selection.IsValid)
        {
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
            return result;
        }

        SceneBoundsEvaluation calculated = BoundsCalculator.CalculateSceneBounds(
            document,
            selection.Nodes,
            transforms,
            options.IncludeMorphTargets);
        result.Bounds = calculated.Bounds;
        result.Meshes = calculated.Meshes;
        issueIndex = 0;
        while (issueIndex < calculated.Diagnostics.Count)
        {
            result.Diagnostics.Add(calculated.Diagnostics[issueIndex]);
            issueIndex++;
        }

        if (calculated.Status != GltfOperationStatus.Success)
        {
            result.IsValid = false;
            if (calculated.Status == GltfOperationStatus.Invalid
                || calculated.Status == GltfOperationStatus.Failed
                || result.Status == GltfOperationStatus.Success)
            {
                result.Status = calculated.Status;
            }
        }

        return result;
    }

    internal SceneSelection SelectScene(
        GltfDocument document,
        GltfSceneEvaluationOptions options,
        SceneGraphAnalysis graph)
    {
        SceneSelection result = new SceneSelection();
        int nodeIndex = 0;
        while (nodeIndex < graph.Parents.Count)
        {
            result.ActiveNodes.Add(false);
            nodeIndex++;
        }

        int sceneIndex = GltfConstants.AbsentIndex;
        if (options.HasScene)
        {
            sceneIndex = options.Scene;
        }
        else if (document.HasDefaultScene)
        {
            sceneIndex = document.DefaultScene;
        }
        else if (document.Scenes != null && document.Scenes.Count > 0)
        {
            sceneIndex = 0;
        }

        result.Scene = sceneIndex;
        if (sceneIndex == GltfConstants.AbsentIndex)
        {
            int traversalPosition = 0;
            while (traversalPosition < graph.TraversalOrder.Count)
            {
                AddSelectedNode(result, graph.TraversalOrder[traversalPosition]);
                traversalPosition++;
            }

            return result;
        }

        if (document.Scenes == null
            || sceneIndex < 0
            || sceneIndex >= document.Scenes.Count
            || sceneIndex >= graph.SceneTraversals.Count
            || document.Scenes[sceneIndex] == null)
        {
            AddSelectionIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                $"Scene index {sceneIndex} does not resolve to a scene.",
                $"/scenes/{sceneIndex}");
            return result;
        }

        SceneGraphTraversal traversal = graph.SceneTraversals[sceneIndex];
        if (!traversal.IsValid)
        {
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
        }

        int position = 0;
        while (position < traversal.Nodes.Count)
        {
            AddSelectedNode(result, traversal.Nodes[position]);
            position++;
        }

        return result;
    }

    internal void AddSelectedNode(SceneSelection result, int nodeIndex)
    {
        if (nodeIndex >= 0
            && nodeIndex < result.ActiveNodes.Count
            && !result.ActiveNodes[nodeIndex])
        {
            result.ActiveNodes[nodeIndex] = true;
            result.Nodes.Add(nodeIndex);
        }
    }

    internal void PopulateNodePoses(
        GltfScenePose result,
        SceneTransformAnalysis transforms)
    {
        int nodeIndex = 0;
        while (nodeIndex < transforms.Nodes.Count)
        {
            NodeTransformEvaluation transform = transforms.Nodes[nodeIndex];
            GltfNodePose nodePose = new GltfNodePose();
            nodePose.Node = nodeIndex;
            nodePose.LocalTransform = transform.LocalTransform;
            nodePose.WorldTransform = transform.WorldTransform;
            nodePose.HasNormalTransform = transform.HasNormalTransform;
            nodePose.NormalTransform = transform.NormalTransform;
            result.Nodes.Add(nodePose);
            nodeIndex++;
        }
    }

    internal void CalculateNodeProjections(
        GltfDocument document,
        GltfScenePose result,
        SceneSelection selection)
    {
        int nodeIndex = 0;
        while (nodeIndex < selection.ActiveNodes.Count)
        {
            if (selection.ActiveNodes[nodeIndex]
                && document.Nodes != null
                && nodeIndex < document.Nodes.Count
                && document.Nodes[nodeIndex] != null
                && document.Nodes[nodeIndex].HasCamera)
            {
                GltfProjectionResult projection = ProjectionCalculator.CalculateDocumentProjection(
                    document,
                    document.Nodes[nodeIndex].Camera);
                CopyProjectionIssues(result, projection);
                MergeSceneStatus(result, projection.Status);
                if (projection.Status == GltfOperationStatus.Success
                    && nodeIndex < result.Nodes.Count)
                {
                    result.Nodes[nodeIndex].HasProjection = true;
                    result.Nodes[nodeIndex].Projection = projection.Projection;
                }
            }

            nodeIndex++;
        }
    }

    internal void CalculateNodeSkins(
        GltfDocument document,
        GltfScenePose result,
        SceneSelection selection)
    {
        int nodeIndex = 0;
        while (nodeIndex < selection.ActiveNodes.Count)
        {
            if (selection.ActiveNodes[nodeIndex]
                && document.Nodes != null
                && nodeIndex < document.Nodes.Count
                && document.Nodes[nodeIndex] != null
                && document.Nodes[nodeIndex].HasSkin)
            {
                SkinEvaluation skin = SkinEvaluator.EvaluateSkin(
                    document,
                    document.Nodes[nodeIndex].Skin);
                CopySkinIssues(result, skin);
                MergeSceneStatus(result, skin.Status);
                if (skin.HasPalette && nodeIndex < result.Nodes.Count)
                {
                    int matrixIndex = 0;
                    while (matrixIndex < skin.JointMatrices.Count)
                    {
                        result.Nodes[nodeIndex].SkinMatrices.Add(
                            skin.JointMatrices[matrixIndex]);
                        matrixIndex++;
                    }
                }
            }

            nodeIndex++;
        }
    }

    internal void CopyTransformIssues(
        GltfScenePose result,
        SceneTransformAnalysis transforms)
    {
        int issueIndex = 0;
        while (issueIndex < transforms.Diagnostics.Count)
        {
            result.Diagnostics.Add(transforms.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void CopySelectionIssues(GltfScenePose result, SceneSelection selection)
    {
        int issueIndex = 0;
        while (issueIndex < selection.Diagnostics.Count)
        {
            result.Diagnostics.Add(selection.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void CopyProjectionIssues(
        GltfScenePose result,
        GltfProjectionResult projection)
    {
        int issueIndex = 0;
        while (issueIndex < projection.Diagnostics.Count)
        {
            result.Diagnostics.Add(projection.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void CopySkinIssues(GltfScenePose result, SkinEvaluation skin)
    {
        int issueIndex = 0;
        while (issueIndex < skin.Diagnostics.Count)
        {
            result.Diagnostics.Add(skin.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void CopyBoundsIssues(GltfScenePose result, SceneBoundsEvaluation bounds)
    {
        int issueIndex = 0;
        while (issueIndex < bounds.Diagnostics.Count)
        {
            result.Diagnostics.Add(bounds.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void MergeSceneStatus(GltfScenePose result, GltfOperationStatus status)
    {
        if (status == GltfOperationStatus.Invalid
            || status == GltfOperationStatus.Failed
            || result.Status == GltfOperationStatus.Success)
        {
            result.Status = status;
        }
    }

    internal void AddSelectionIssue(
        SceneSelection result,
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
        result.Status = GltfOperationStatus.Invalid;
    }
}
