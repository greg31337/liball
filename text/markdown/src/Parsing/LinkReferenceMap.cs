using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownLinkReferenceMap
{
    internal List<MarkdownNode> Definitions;

    internal MarkdownLinkReferenceMap()
    {
        Definitions = new List<MarkdownNode>();
    }

    internal int GetCount()
    {
        return Definitions.Count;
    }

    internal bool IsEmpty()
    {
        return Definitions.Count == 0;
    }

    internal bool Add(MarkdownNode definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException("definition");
        }

        if (definition.Kind != MarkdownNodeKind.LinkReferenceDefinition)
        {
            throw new ArgumentException("Node kind must be a link reference definition.");
        }

        string normalizedLabel = definition.GetAttributeValue("normalizedLabel");
        if (normalizedLabel.Length == 0)
        {
            normalizedLabel = NormalizeLabel(definition.GetAttributeValue("label"));
            if (normalizedLabel.Length == 0)
            {
                return false;
            }

            definition.SetAttribute("normalizedLabel", normalizedLabel);
        }

        if (FindNormalized(normalizedLabel) != null)
        {
            return false;
        }

        Definitions.Add(definition);
        return true;
    }

    internal MarkdownNode? Find(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        string normalizedLabel = NormalizeLabel(label);
        if (normalizedLabel.Length == 0)
        {
            return null;
        }

        return FindNormalized(normalizedLabel);
    }

    internal MarkdownNode? FindNormalized(string normalizedLabel)
    {
        if (normalizedLabel == null)
        {
            throw new ArgumentNullException("normalizedLabel");
        }

        int index = 0;
        while (index < Definitions.Count)
        {
            MarkdownNode definition = Definitions[index];
            if (definition.GetAttributeValue("normalizedLabel") == normalizedLabel)
            {
                return definition;
            }

            index = index + 1;
        }

        return null;
    }

    internal bool Contains(string label)
    {
        return Find(label) != null;
    }

    internal MarkdownNode[] GetDefinitions()
    {
        return Definitions.ToArray();
    }

    internal void Clear()
    {
        Definitions.Clear();
    }

    internal static string NormalizeLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return CommonMarkBlockRules.NormalizeLinkReferenceLabel(label);
    }
}
