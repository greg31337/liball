using System;
using System.Collections.Generic;

namespace odf;

internal class OdfNodeIndex
{
    OdfNode[] roots;
    Dictionary<string, List<OdfNode>> ids = new Dictionary<string, List<OdfNode>>(
        StringComparer.Ordinal);
    Dictionary<string, List<OdfNode>> qualifiedNames = new Dictionary<string, List<OdfNode>>(
        StringComparer.Ordinal);
    Dictionary<string, List<OdfNode>> styleNames = new Dictionary<string, List<OdfNode>>(
        StringComparer.Ordinal);
    OdfReferenceIndex references = new OdfReferenceIndex();
    bool valid;
    int revision;

    internal OdfNodeIndex(OdfNode[] roots)
    {
        if (roots == null)
        {
            throw new OdfXmlException("XML index roots are required.");
        }
        this.roots = roots;
    }

    internal void Invalidate()
    {
        valid = false;
    }

    internal void UpdateRoots(OdfNode[] newRoots)
    {
        if (newRoots == null)
        {
            throw new OdfXmlException("XML index roots are required.");
        }
        roots = newRoots;
        valid = false;
    }

    internal OdfNode[] FindNodesByXmlId(string xmlId)
    {
        EnsureBuilt();
        return FindResults(ids, xmlId);
    }

    internal OdfNode[] FindNodesByQualifiedName(OdfQualifiedName name)
    {
        if (!OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfXmlException("A valid expanded XML name is required for lookup.");
        }
        EnsureBuilt();
        return FindResults(
            qualifiedNames,
            ExpandedKey(name.NamespaceName, name.LocalName));
    }

    internal OdfNode[] FindNodesByStyleName(string styleName)
    {
        EnsureBuilt();
        return FindResults(styleNames, styleName);
    }

    internal OdfNode[] FindNodesReferringTo(string target)
    {
        EnsureBuilt();
        return references.FindNodes(target);
    }

    internal int GetRevision()
    {
        EnsureBuilt();
        return revision;
    }

    void EnsureBuilt()
    {
        if (valid)
        {
            return;
        }
        Rebuild();
    }

    void Rebuild()
    {
        ids.Clear();
        qualifiedNames.Clear();
        styleNames.Clear();
        references.Clear();
        HashSet<OdfNode> visited = new HashSet<OdfNode>();
        foreach (OdfNode root in roots)
        {
            VisitNode(root, visited);
        }
        revision++;
        valid = true;
    }

    void VisitNode(
        OdfNode node,
        HashSet<OdfNode> visited)
    {
        if (node == null || visited.Contains(node))
        {
            return;
        }
        visited.Add(node);
        if (node.Kind == OdfNodeKind.Element && node.Name != null)
        {
            AddToIndex(
                qualifiedNames,
                ExpandedKey(node.Name.NamespaceName, node.Name.LocalName),
                node);
            string xmlId = GetXmlId(node);
            if (xmlId.Length > 0)
            {
                AddToIndex(ids, xmlId, node);
            }
            string styleName = GetStyleName(node);
            if (styleName.Length > 0)
            {
                AddToIndex(styleNames, styleName, node);
            }
            references.AddNode(node);
        }
        foreach (OdfNode child in node.Children)
        {
            VisitNode(child, visited);
        }
    }

    static string GetXmlId(OdfNode node)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                && attribute.Name.LocalName == "id")
            {
                return attribute.Value;
            }
        }
        return node.XmlId;
    }

    static string GetStyleName(OdfNode node)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Style
                && attribute.Name.LocalName == "name")
            {
                return attribute.Value;
            }
        }
        return "";
    }

    static void AddToIndex(
        Dictionary<string, List<OdfNode>> index,
        string key,
        OdfNode node)
    {
        if (!index.ContainsKey(key))
        {
            index.Add(key, new List<OdfNode>());
        }
        index[key].Add(node);
    }

    static string ExpandedKey(
        string namespaceName,
        string localName)
    {
        return namespaceName + "\u001F" + localName;
    }

    static OdfNode[] FindResults(
        Dictionary<string, List<OdfNode>> index,
        string key)
    {
        if (key == null || !index.ContainsKey(key))
        {
            return [];
        }
        return index[key].ToArray();
    }
}
