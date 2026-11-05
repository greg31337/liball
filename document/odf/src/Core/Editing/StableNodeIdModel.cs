using System;
using System.Collections.Generic;

namespace odf;

internal static class StableNodeIdModel
{
    internal static string EnsureStableNodeId(
        OdfDocument document,
        OdfNode node)
    {
        RequireAttachedElement(document, node);
        string existing = ReadStableNodeId(node);
        if (OdfNameRules.IsXmlId(existing)
            && IsUniqueStableNodeId(document, node, existing))
        {
            return existing;
        }
        int sequence = 1;
        string candidate = "odf-node-" + sequence.ToString();
        while (FindStableNode(document, candidate) != null)
        {
            sequence = sequence + 1;
            candidate = "odf-node-" + sequence.ToString();
        }
        AssignStableNodeId(document, node, candidate);
        return candidate;
    }

    internal static void AssignStableNodeId(
        OdfDocument document,
        OdfNode node,
        string stableId)
    {
        RequireAttachedElement(document, node);
        if (!OdfNameRules.IsXmlId(stableId))
        {
            throw new OdfEditException(
                "A stable node identifier must be a valid XML ID lexical value.");
        }
        OdfNode? existing = FindStableNode(document, stableId);
        if (existing != null
            && (existing != node
                || !IsUniqueStableNodeId(document, node, stableId)))
        {
            throw new OdfEditException(
                "A stable node identifier must be unique in the document.");
        }
        WriteStableNodeId(node, stableId);
    }

    internal static OdfNode? FindStableNode(
        OdfDocument document,
        string stableId)
    {
        if (document == null || stableId == null || stableId.Length == 0)
        {
            return null;
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? result = FindStableNodeInRoots(
                document.Parts[partIndex].Nodes,
                stableId);
            if (result != null)
            {
                return result;
            }
            partIndex = partIndex + 1;
        }
        return null;
    }

    internal static string ReadStableNodeId(OdfNode node)
    {
        if (node == null || node.Kind != OdfNodeKind.Element)
        {
            return "";
        }
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Collaboration,
            "node-id");
    }

    internal static void WriteStableNodeId(
        OdfNode node,
        string stableId)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Collaboration,
            "node-id",
            OdfNamespaces.CollaborationPrefix,
            stableId);
    }

    internal static OdfPart? FindOperationPart(
        OdfDocument document,
        string partPath)
    {
        if (document == null || partPath == null)
        {
            return null;
        }
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Path == partPath
                || part.IsFlatDocument && partPath.Length == 0)
            {
                return part;
            }
            index = index + 1;
        }
        return null;
    }

    internal static string FindNodePartPath(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (ContainsNodeInRoots(document.Parts[index].Nodes, node))
            {
                return document.Parts[index].Path;
            }
            index = index + 1;
        }
        return "";
    }

    static bool IsUniqueStableNodeId(
        OdfDocument document,
        OdfNode node,
        string stableId)
    {
        int found = 0;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            found = CountStableNodeId(
                document.Parts[partIndex].Nodes,
                stableId,
                found);
            partIndex = partIndex + 1;
        }
        return found == 1 && FindStableNode(document, stableId) == node;
    }

    static int CountStableNodeId(
        OdfNode[] roots,
        string stableId,
        int found)
    {
        int index = 0;
        while (index < roots.Length)
        {
            found = CountStableNodeIdInTree(
                roots[index], stableId, found);
            index = index + 1;
        }
        return found;
    }

    static int CountStableNodeIdInTree(
        OdfNode node,
        string stableId,
        int found)
    {
        if (ReadStableNodeId(node) == stableId)
        {
            found = found + 1;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            found = CountStableNodeIdInTree(
                node.Children[index], stableId, found);
            index = index + 1;
        }
        return found;
    }

    static OdfNode? FindStableNodeInRoots(
        OdfNode[] roots,
        string stableId)
    {
        int index = 0;
        while (index < roots.Length)
        {
            OdfNode? result = FindStableNodeInTree(roots[index], stableId);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindStableNodeInTree(
        OdfNode node,
        string stableId)
    {
        if (ReadStableNodeId(node) == stableId)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindStableNodeInTree(
                node.Children[index], stableId);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static bool ContainsNodeInRoots(OdfNode[] roots, OdfNode target)
    {
        int index = 0;
        while (index < roots.Length)
        {
            if (ContainsNodeInTree(roots[index], target))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsNodeInTree(OdfNode node, OdfNode target)
    {
        if (node == target)
        {
            return true;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (ContainsNodeInTree(node.Children[index], target))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RequireAttachedElement(
        OdfDocument document,
        OdfNode node)
    {
        if (document == null || node == null
            || node.Kind != OdfNodeKind.Element
            || FindAttachedNode(document, node) == null)
        {
            throw new OdfEditException(
                "Stable identifiers require an attached XML element.");
        }
    }

    static OdfNode? FindAttachedNode(OdfDocument document, OdfNode target)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            if (ContainsNodeInRoots(document.Parts[partIndex].Nodes, target))
            {
                return target;
            }
            partIndex = partIndex + 1;
        }
        return null;
    }
}
