using System;
using System.Collections.Generic;

namespace odf;

internal class OdfReferenceIndex
{
    Dictionary<string, List<OdfNode>> references = new Dictionary<string, List<OdfNode>>(
        StringComparer.Ordinal);

    internal void Clear()
    {
        references.Clear();
    }

    internal void AddNode(OdfNode node)
    {
        foreach (OdfAttribute attribute in node.Attributes)
        {
            if (IsReferenceAttribute(attribute))
            {
                string target = GetTarget(attribute);
                if (target.Length > 0)
                {
                    AddTarget(target, node);
                }
            }
        }
    }

    internal OdfNode[] FindNodes(string target)
    {
        if (target == null || !references.ContainsKey(target))
        {
            return [];
        }
        return references[target].ToArray();
    }

    static bool IsReferenceAttribute(OdfAttribute attribute)
    {
        string localName = attribute.Name.LocalName;
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && localName == "href")
        {
            return true;
        }
        if (localName == "ref-name"
            || localName == "style-name"
            || localName == "start-shape"
            || localName == "end-shape"
            || localName == "shape-id"
            || localName == "target-id"
            || localName == "idref"
            || localName == "for"
            || localName == "control"
            || localName == "start-page")
        {
            return true;
        }
        return localName.EndsWith(
            "-style-name",
            StringComparison.Ordinal);
    }

    static string GetTarget(OdfAttribute attribute)
    {
        string value = attribute.Value.Trim();
        if (attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href")
        {
            int fragment = value.LastIndexOf('#');
            if (fragment >= 0 && fragment + 1 < value.Length)
            {
                return value.Substring(fragment + 1);
            }
        }
        return value;
    }

    void AddTarget(string target, OdfNode node)
    {
        if (!references.ContainsKey(target))
        {
            references.Add(target, new List<OdfNode>());
        }
        List<OdfNode> nodes = references[target];
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == node)
            {
                return;
            }
            index++;
        }
        nodes.Add(node);
    }
}
