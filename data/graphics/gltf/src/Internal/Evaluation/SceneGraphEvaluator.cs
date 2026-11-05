using System;
using System.Collections.Generic;

namespace gltf;

internal class SceneGraphTraversal
{
    internal int Scene = GltfConstants.AbsentIndex;
    internal bool IsValid = true;
    internal List<int> Roots = new List<int>();
    internal List<int> Nodes = new List<int>();
}

internal class SceneGraphAnalysis
{
    internal bool IsValid = true;
    internal bool HasCycles;
    internal List<int> Parents = new List<int>();
    internal List<List<int>> ParentReferences = new List<List<int>>();
    internal List<List<int>> RootSceneReferences = new List<List<int>>();
    internal List<List<int>> NodeScenes = new List<List<int>>();
    internal List<bool> CycleNodes = new List<bool>();
    internal List<bool> InvalidNodes = new List<bool>();
    internal List<SceneGraphTraversal> SceneTraversals = new List<SceneGraphTraversal>();
    internal List<int> TraversalOrder = new List<int>();
    internal List<int> OrphanNodes = new List<int>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class SceneGraphEvaluator
{
    internal SceneGraphAnalysis AnalyzeDocument(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        SceneGraphAnalysis result = new SceneGraphAnalysis();
        if (document.Nodes == null)
        {
            AddSceneGraphIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                "The node collection is missing.",
                "/nodes");
            return result;
        }

        InitializeNodeMaps(result, document.Nodes.Count);
        BuildParentMaps(document, result);
        DetectNodeCycles(document, result);
        BuildSceneTraversals(document, result);
        BuildDocumentTraversal(document, result);
        CollectOrphanNodes(result);
        return result;
    }

    internal void InitializeNodeMaps(SceneGraphAnalysis result, int nodeCount)
    {
        int nodeIndex = 0;
        while (nodeIndex < nodeCount)
        {
            result.Parents.Add(GltfConstants.AbsentIndex);
            result.ParentReferences.Add(new List<int>());
            result.RootSceneReferences.Add(new List<int>());
            result.NodeScenes.Add(new List<int>());
            result.CycleNodes.Add(false);
            result.InvalidNodes.Add(false);
            nodeIndex++;
        }
    }

    internal void BuildParentMaps(GltfDocument document, SceneGraphAnalysis result)
    {
        int parentIndex = 0;
        while (parentIndex < document.Nodes.Count)
        {
            GltfNode parent = document.Nodes[parentIndex];
            string parentPointer = $"/nodes/{parentIndex}";
            if (parent == null)
            {
                result.InvalidNodes[parentIndex] = true;
                AddSceneGraphIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The node is null.",
                    parentPointer);
                parentIndex++;
                continue;
            }

            if (parent.Children == null)
            {
                result.InvalidNodes[parentIndex] = true;
                AddSceneGraphIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The node children collection is missing.",
                    $"{parentPointer}/children");
                parentIndex++;
                continue;
            }

            bool[] referencedChildren = new bool[document.Nodes.Count];
            int childPosition = 0;
            while (childPosition < parent.Children.Count)
            {
                int childIndex = parent.Children[childPosition];
                string childPointer = $"{parentPointer}/children/{childPosition}";
                if (!IsNodeIndexValid(document, childIndex))
                {
                    result.InvalidNodes[parentIndex] = true;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.UnresolvedReference,
                        $"Child node index {childIndex} does not resolve to a node.",
                        childPointer);
                    childPosition++;
                    continue;
                }

                if (referencedChildren[childIndex])
                {
                    result.InvalidNodes[parentIndex] = true;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.DuplicateElements,
                        $"Child node index {childIndex} is listed more than once.",
                        childPointer);
                    childPosition++;
                    continue;
                }

                referencedChildren[childIndex] = true;
                result.ParentReferences[childIndex].Add(parentIndex);
                if (result.Parents[childIndex] == GltfConstants.AbsentIndex)
                {
                    result.Parents[childIndex] = parentIndex;
                }
                else
                {
                    result.InvalidNodes[childIndex] = true;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.NodeParentOverride,
                        $"Node {childIndex} has more than one parent.",
                        childPointer);
                }

                childPosition++;
            }

            parentIndex++;
        }
    }

    internal void DetectNodeCycles(GltfDocument document, SceneGraphAnalysis result)
    {
        int[] states = new int[document.Nodes.Count];
        int startIndex = 0;
        while (startIndex < document.Nodes.Count)
        {
            if (states[startIndex] == 0)
            {
                DetectCyclesFromNode(document, result, states, startIndex);
            }

            startIndex++;
        }
    }

    internal void DetectCyclesFromNode(
        GltfDocument document,
        SceneGraphAnalysis result,
        int[] states,
        int startIndex)
    {
        List<int> nodeStack = new List<int>();
        List<int> childPositionStack = new List<int>();
        nodeStack.Add(startIndex);
        childPositionStack.Add(0);
        while (nodeStack.Count > 0)
        {
            int stackIndex = nodeStack.Count - 1;
            int nodeIndex = nodeStack[stackIndex];
            if (states[nodeIndex] == 0)
            {
                states[nodeIndex] = 1;
            }

            GltfNode node = document.Nodes[nodeIndex];
            if (node == null || node.Children == null)
            {
                states[nodeIndex] = 2;
                nodeStack.RemoveAt(stackIndex);
                childPositionStack.RemoveAt(stackIndex);
                continue;
            }

            int childPosition = childPositionStack[stackIndex];
            if (childPosition >= node.Children.Count)
            {
                states[nodeIndex] = 2;
                nodeStack.RemoveAt(stackIndex);
                childPositionStack.RemoveAt(stackIndex);
                continue;
            }

            childPositionStack[stackIndex] = childPosition + 1;
            int childIndex = node.Children[childPosition];
            if (!IsNodeIndexValid(document, childIndex)
                || HasEarlierChildReference(node.Children, childPosition, childIndex))
            {
                continue;
            }

            if (states[childIndex] == 0)
            {
                nodeStack.Add(childIndex);
                childPositionStack.Add(0);
            }
            else if (states[childIndex] == 1)
            {
                result.HasCycles = true;
                MarkCycleNodes(result, nodeStack, childIndex);
                AddSceneGraphIssue(
                    result,
                    GltfIssueCodes.NodeLoop,
                    $"Child node index {childIndex} closes a node hierarchy cycle.",
                    $"/nodes/{nodeIndex}/children/{childPosition}");
            }
        }
    }

    internal bool HasEarlierChildReference(
        List<int> children,
        int childPosition,
        int childIndex)
    {
        int earlierPosition = 0;
        while (earlierPosition < childPosition)
        {
            if (children[earlierPosition] == childIndex)
            {
                return true;
            }

            earlierPosition++;
        }

        return false;
    }

    internal void MarkCycleNodes(
        SceneGraphAnalysis result,
        List<int> nodeStack,
        int firstCycleNode)
    {
        int firstPosition = nodeStack.Count - 1;
        while (firstPosition >= 0 && nodeStack[firstPosition] != firstCycleNode)
        {
            firstPosition--;
        }

        while (firstPosition < nodeStack.Count)
        {
            result.CycleNodes[nodeStack[firstPosition]] = true;
            firstPosition++;
        }
    }

    internal void BuildSceneTraversals(GltfDocument document, SceneGraphAnalysis result)
    {
        if (document.Scenes == null)
        {
            AddSceneGraphIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                "The scene collection is missing.",
                "/scenes");
            return;
        }

        int sceneIndex = 0;
        while (sceneIndex < document.Scenes.Count)
        {
            SceneGraphTraversal traversal = new SceneGraphTraversal();
            traversal.Scene = sceneIndex;
            result.SceneTraversals.Add(traversal);
            GltfScene scene = document.Scenes[sceneIndex];
            string scenePointer = $"/scenes/{sceneIndex}";
            if (scene == null)
            {
                traversal.IsValid = false;
                AddSceneGraphIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The scene is null.",
                    scenePointer);
                sceneIndex++;
                continue;
            }

            if (scene.Nodes == null)
            {
                traversal.IsValid = false;
                AddSceneGraphIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The scene root node collection is missing.",
                    $"{scenePointer}/nodes");
                sceneIndex++;
                continue;
            }

            bool[] visitedNodes = new bool[document.Nodes.Count];
            bool[] referencedRoots = new bool[document.Nodes.Count];
            int rootPosition = 0;
            while (rootPosition < scene.Nodes.Count)
            {
                int rootIndex = scene.Nodes[rootPosition];
                string rootPointer = $"{scenePointer}/nodes/{rootPosition}";
                if (!IsNodeIndexValid(document, rootIndex))
                {
                    traversal.IsValid = false;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.UnresolvedReference,
                        $"Root node index {rootIndex} does not resolve to a node.",
                        rootPointer);
                    rootPosition++;
                    continue;
                }

                if (referencedRoots[rootIndex])
                {
                    traversal.IsValid = false;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.DuplicateElements,
                        $"Root node index {rootIndex} is listed more than once.",
                        rootPointer);
                    rootPosition++;
                    continue;
                }

                referencedRoots[rootIndex] = true;
                traversal.Roots.Add(rootIndex);
                result.RootSceneReferences[rootIndex].Add(sceneIndex);
                if (result.ParentReferences[rootIndex].Count > 0)
                {
                    traversal.IsValid = false;
                    AddSceneGraphIssue(
                        result,
                        GltfIssueCodes.SceneNonRootNode,
                        $"Node {rootIndex} is not a root node.",
                        rootPointer);
                }

                TraverseSceneSubtree(
                    document,
                    result,
                    traversal,
                    visitedNodes,
                    rootIndex);
                rootPosition++;
            }

            MarkTraversalGraphValidity(document, result, traversal);
            sceneIndex++;
        }
    }

    internal void TraverseSceneSubtree(
        GltfDocument document,
        SceneGraphAnalysis result,
        SceneGraphTraversal traversal,
        bool[] visitedNodes,
        int rootIndex)
    {
        List<int> stack = new List<int>();
        stack.Add(rootIndex);
        while (stack.Count > 0)
        {
            int stackIndex = stack.Count - 1;
            int nodeIndex = stack[stackIndex];
            stack.RemoveAt(stackIndex);
            if (visitedNodes[nodeIndex])
            {
                continue;
            }

            visitedNodes[nodeIndex] = true;
            traversal.Nodes.Add(nodeIndex);
            result.NodeScenes[nodeIndex].Add(traversal.Scene);
            GltfNode node = document.Nodes[nodeIndex];
            if (node == null || node.Children == null)
            {
                continue;
            }

            int childPosition = node.Children.Count - 1;
            while (childPosition >= 0)
            {
                int childIndex = node.Children[childPosition];
                if (IsNodeIndexValid(document, childIndex))
                {
                    stack.Add(childIndex);
                }

                childPosition--;
            }
        }
    }

    internal void MarkTraversalGraphValidity(
        GltfDocument document,
        SceneGraphAnalysis result,
        SceneGraphTraversal traversal)
    {
        int position = 0;
        while (position < traversal.Nodes.Count)
        {
            int nodeIndex = traversal.Nodes[position];
            if (document.Nodes[nodeIndex] == null
                || result.InvalidNodes[nodeIndex]
                || result.CycleNodes[nodeIndex]
                || result.ParentReferences[nodeIndex].Count > 1)
            {
                traversal.IsValid = false;
            }

            position++;
        }
    }

    internal void BuildDocumentTraversal(GltfDocument document, SceneGraphAnalysis result)
    {
        bool[] visitedNodes = new bool[document.Nodes.Count];
        int scenePosition = 0;
        while (scenePosition < result.SceneTraversals.Count)
        {
            SceneGraphTraversal traversal = result.SceneTraversals[scenePosition];
            int nodePosition = 0;
            while (nodePosition < traversal.Nodes.Count)
            {
                int nodeIndex = traversal.Nodes[nodePosition];
                if (!visitedNodes[nodeIndex])
                {
                    visitedNodes[nodeIndex] = true;
                    result.TraversalOrder.Add(nodeIndex);
                }

                nodePosition++;
            }

            scenePosition++;
        }

        int rootIndex = 0;
        while (rootIndex < document.Nodes.Count)
        {
            if (result.ParentReferences[rootIndex].Count == 0)
            {
                AppendDocumentSubtree(document, visitedNodes, result.TraversalOrder, rootIndex);
            }

            rootIndex++;
        }

        int remainingIndex = 0;
        while (remainingIndex < document.Nodes.Count)
        {
            AppendDocumentSubtree(document, visitedNodes, result.TraversalOrder, remainingIndex);
            remainingIndex++;
        }
    }

    internal void AppendDocumentSubtree(
        GltfDocument document,
        bool[] visitedNodes,
        List<int> traversalOrder,
        int rootIndex)
    {
        if (visitedNodes[rootIndex])
        {
            return;
        }

        List<int> stack = new List<int>();
        stack.Add(rootIndex);
        while (stack.Count > 0)
        {
            int stackIndex = stack.Count - 1;
            int nodeIndex = stack[stackIndex];
            stack.RemoveAt(stackIndex);
            if (visitedNodes[nodeIndex])
            {
                continue;
            }

            visitedNodes[nodeIndex] = true;
            traversalOrder.Add(nodeIndex);
            GltfNode node = document.Nodes[nodeIndex];
            if (node == null || node.Children == null)
            {
                continue;
            }

            int childPosition = node.Children.Count - 1;
            while (childPosition >= 0)
            {
                int childIndex = node.Children[childPosition];
                if (IsNodeIndexValid(document, childIndex))
                {
                    stack.Add(childIndex);
                }

                childPosition--;
            }
        }
    }

    internal void CollectOrphanNodes(SceneGraphAnalysis result)
    {
        int nodeIndex = 0;
        while (nodeIndex < result.NodeScenes.Count)
        {
            if (result.NodeScenes[nodeIndex].Count == 0)
            {
                result.OrphanNodes.Add(nodeIndex);
            }

            nodeIndex++;
        }
    }

    internal bool IsNodeIndexValid(GltfDocument document, int nodeIndex)
    {
        return nodeIndex >= 0 && nodeIndex < document.Nodes.Count;
    }

    internal void AddSceneGraphIssue(
        SceneGraphAnalysis result,
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
    }
}
