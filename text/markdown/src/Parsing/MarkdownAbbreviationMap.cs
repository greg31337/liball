using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownAbbreviationMap
{
    internal List<MarkdownNode> Definitions;

    internal MarkdownAbbreviationMap()
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

        if (definition.Kind != MarkdownNodeKind.AbbreviationDefinition)
        {
            throw new ArgumentException("Node kind must be an abbreviation definition.");
        }

        string label = definition.GetAttributeValue("label");
        if (label.Length == 0)
        {
            return false;
        }

        if (Find(label) != null)
        {
            return false;
        }

        InsertByDescendingLabelLength(definition);
        return true;
    }

    internal MarkdownNode? Find(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        int index = 0;
        while (index < Definitions.Count)
        {
            MarkdownNode definition = Definitions[index];
            if (definition.GetAttributeValue("label") == label)
            {
                return definition;
            }

            index = index + 1;
        }

        return null;
    }

    internal MarkdownNode[] GetDefinitions()
    {
        return Definitions.ToArray();
    }

    internal void Clear()
    {
        Definitions.Clear();
    }

    internal void InsertByDescendingLabelLength(MarkdownNode definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException("definition");
        }

        int definitionLength = definition.GetAttributeValue("label").Length;
        int index = 0;
        while (index < Definitions.Count)
        {
            if (definitionLength > Definitions[index].GetAttributeValue("label").Length)
            {
                Definitions.Insert(index, definition);
                return;
            }

            index = index + 1;
        }

        Definitions.Add(definition);
    }
}
