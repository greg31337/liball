using System;
using System.Collections.Generic;
using System.IO;

namespace markdown;

/// <summary>
/// Creates deterministic, URL-friendly identifiers from heading text.
/// </summary>
internal class HeadingIdGenerator
{
    internal static void Apply(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<string> usedIds = new List<string>();
        ApplyNode(document.Root, usedIds);
    }

    internal static void ApplyNode(MarkdownNode node, List<string> usedIds)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (node.Kind == MarkdownNodeKind.Heading)
        {
            ApplyHeading(node, usedIds);
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            ApplyNode(node.GetChild(index), usedIds);
            index = index + 1;
        }
    }

    internal static void ApplyHeading(MarkdownNode heading, List<string> usedIds)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (heading.Kind != MarkdownNodeKind.Heading)
        {
            throw new ArgumentException("Node kind must be a heading.");
        }

        string existingId = heading.GetAttributeValue("id");
        if (existingId.Length > 0)
        {
            ReserveHeadingId(usedIds, existingId);
            return;
        }

        string baseId = GenerateHeadingId(GetHeadingText(heading));
        string id = MakeUniqueHeadingId(usedIds, baseId);
        heading.SetAttribute("id", id);
    }

    internal static string GetHeadingText(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        switch (node.Kind)
        {
            case MarkdownNodeKind.Text:
            case MarkdownNodeKind.CodeSpan:
            case MarkdownNodeKind.HtmlInline:
                return node.Literal;
            case MarkdownNodeKind.SoftLineBreak:
            case MarkdownNodeKind.HardLineBreak:
                return " ";
            case MarkdownNodeKind.FootnoteReference:
                return node.GetAttributeValue("label");
            default:
                break;
        }

        if (node.GetChildCount() == 0)
        {
            return node.Literal;
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < node.GetChildCount())
        {
            writer.Write(GetHeadingText(node.GetChild(index)));
            index = index + 1;
        }

        return writer.ToString();
    }

    /// <summary>
    /// Converts heading text to a lowercase, hyphen-separated identifier.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string GenerateHeadingId(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringWriter writer = new StringWriter();
        bool lastWasDash = false;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (char.IsLetterOrDigit(character))
            {
                writer.Write(char.ToLowerInvariant(character));
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                writer.Write("-");
                lastWasDash = true;
            }

            index = index + 1;
        }

        string id = writer.ToString().Trim('-');
        if (id.Length == 0)
        {
            return "section";
        }

        return id;
    }

    internal static string MakeUniqueHeadingId(List<string> usedIds, string baseId)
    {
        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (baseId == null)
        {
            throw new ArgumentNullException("baseId");
        }

        string id = baseId;
        if (id.Length == 0)
        {
            id = "section";
        }

        int suffix = 1;
        string candidate = id;
        while (ContainsHeadingId(usedIds, candidate))
        {
            candidate = id + "-" + suffix.ToString();
            suffix = suffix + 1;
        }

        usedIds.Add(candidate);
        return candidate;
    }

    internal static void ReserveHeadingId(List<string> usedIds, string id)
    {
        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id.Length > 0 && !ContainsHeadingId(usedIds, id))
        {
            usedIds.Add(id);
        }
    }

    internal static bool ContainsHeadingId(List<string> usedIds, string id)
    {
        if (usedIds == null)
        {
            throw new ArgumentNullException("usedIds");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        int index = 0;
        while (index < usedIds.Count)
        {
            if (usedIds[index] == id)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
