using System;
using System.Collections.Generic;

namespace markdown;

internal class AbbreviationInlineProcessor
{
    internal static void Apply(MarkdownDocument document, MarkdownAbbreviationMap abbreviations)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (abbreviations == null)
        {
            throw new ArgumentNullException("abbreviations");
        }

        if (abbreviations.IsEmpty())
        {
            return;
        }

        ApplyNode(document.Root, abbreviations);
    }

    internal static void ApplyNode(MarkdownNode node, MarkdownAbbreviationMap abbreviations)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (abbreviations == null)
        {
            throw new ArgumentNullException("abbreviations");
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind == MarkdownNodeKind.Text)
            {
                ReplaceTextChild(node, index, child, abbreviations);
            }
            else if (ShouldProcessChildren(child))
            {
                ApplyNode(child, abbreviations);
            }

            index = index + 1;
        }
    }

    internal static bool ReplaceTextChild(MarkdownNode parent, int childIndex, MarkdownNode textNode, MarkdownAbbreviationMap abbreviations)
    {
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        if (textNode == null)
        {
            throw new ArgumentNullException("textNode");
        }

        if (abbreviations == null)
        {
            throw new ArgumentNullException("abbreviations");
        }

        List<MarkdownNode> replacement = CreateReplacementNodes(textNode.Literal, abbreviations);
        if (replacement.Count == 1 && replacement[0].Kind == MarkdownNodeKind.Text && replacement[0].Literal == textNode.Literal)
        {
            return false;
        }

        parent.RemoveChildAt(childIndex);
        int index = 0;
        while (index < replacement.Count)
        {
            parent.InsertChild(childIndex + index, replacement[index]);
            index = index + 1;
        }

        return true;
    }

    internal static List<MarkdownNode> CreateReplacementNodes(string text, MarkdownAbbreviationMap abbreviations)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (abbreviations == null)
        {
            throw new ArgumentNullException("abbreviations");
        }

        List<MarkdownNode> nodes = new List<MarkdownNode>();
        int index = 0;
        int textStart = 0;
        while (index < text.Length)
        {
            MarkdownNode? definition = FindDefinitionAt(text, index, abbreviations);
            if (definition == null)
            {
                index = index + 1;
                continue;
            }

            string label = definition.GetAttributeValue("label");
            if (index > textStart)
            {
                nodes.Add(CreateTextNode(text.Substring(textStart, index - textStart)));
            }

            nodes.Add(CreateAbbreviationNode(label, definition.GetAttributeValue("title")));
            index = index + label.Length;
            textStart = index;
        }

        if (textStart < text.Length)
        {
            nodes.Add(CreateTextNode(text.Substring(textStart)));
        }

        if (nodes.Count == 0)
        {
            nodes.Add(CreateTextNode(text));
        }

        return nodes;
    }

    internal static MarkdownNode? FindDefinitionAt(string text, int position, MarkdownAbbreviationMap abbreviations)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (position < 0 || position > text.Length)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        if (abbreviations == null)
        {
            throw new ArgumentNullException("abbreviations");
        }

        MarkdownNode[] definitions = abbreviations.GetDefinitions();
        int index = 0;
        while (index < definitions.Length)
        {
            MarkdownNode definition = definitions[index];
            string label = definition.GetAttributeValue("label");
            if (MatchesAt(text, position, label) && HasWordBoundaries(text, position, label))
            {
                return definition;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool MatchesAt(string text, int position, string label)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (position < 0 || position > text.Length)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        if (label.Length == 0 || label.Length > text.Length - position)
        {
            return false;
        }

        int index = 0;
        while (index < label.Length)
        {
            if (text[position + index] != label[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasWordBoundaries(string text, int position, string label)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (position < 0 || position > text.Length)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        if (label.Length == 0)
        {
            return false;
        }

        int end = position + label.Length;
        bool before = position == 0 || !char.IsLetterOrDigit(text[position - 1]);
        bool after = end >= text.Length || !char.IsLetterOrDigit(text[end]);
        return before && after;
    }

    internal static MarkdownNode CreateTextNode(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        node.SetLiteral(text);
        return node;
    }

    internal static MarkdownNode CreateAbbreviationNode(string label, string title)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        MarkdownNode abbreviation = MarkdownNode.Create(MarkdownNodeKind.Abbreviation);
        MarkdownNode text = CreateTextNode(label);
        abbreviation.SetLiteral(label);
        abbreviation.SetAttribute("title", title);
        abbreviation.AppendChild(text);
        return abbreviation;
    }

    internal static bool ShouldProcessChildren(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.Kind != MarkdownNodeKind.AbbreviationDefinition
            && node.Kind != MarkdownNodeKind.Abbreviation
            && node.Kind != MarkdownNodeKind.CodeSpan
            && node.Kind != MarkdownNodeKind.HtmlInline;
    }
}
