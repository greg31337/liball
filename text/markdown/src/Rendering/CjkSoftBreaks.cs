using System;
using System.IO;

namespace markdown;

internal class CjkSoftBreaks
{
    internal static bool ShouldCollapse(MarkdownPipeline pipeline, MarkdownOptions options, MarkdownNode softBreak)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (softBreak == null)
        {
            throw new ArgumentNullException("softBreak");
        }

        if (softBreak.Kind != MarkdownNodeKind.SoftLineBreak)
        {
            throw new ArgumentException("Node kind must be a soft line break.");
        }

        if (!pipeline.GetFeatures().CjkSoftBreaks || options.SoftBreakMode != MarkdownSoftBreakMode.Cjk)
        {
            return false;
        }

        char before = GetPreviousCharacter(softBreak);
        char after = GetNextCharacter(softBreak);
        return IsCjkCharacter(before) && IsCjkCharacter(after);
    }

    internal static char GetPreviousCharacter(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? sibling = node.GetPreviousSibling();
        while (sibling != null)
        {
            string text = GetPlainText(sibling);
            int index = text.Length - 1;
            while (index >= 0)
            {
                if (text[index] != '\n' && text[index] != '\r')
                {
                    return text[index];
                }

                index = index - 1;
            }

            sibling = sibling.GetPreviousSibling();
        }

        return '\0';
    }

    internal static char GetNextCharacter(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownNode? sibling = node.GetNextSibling();
        while (sibling != null)
        {
            string text = GetPlainText(sibling);
            int index = 0;
            while (index < text.Length)
            {
                if (text[index] != '\n' && text[index] != '\r')
                {
                    return text[index];
                }

                index = index + 1;
            }

            sibling = sibling.GetNextSibling();
        }

        return '\0';
    }

    internal static string GetPlainText(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind == MarkdownNodeKind.Text
            || node.Kind == MarkdownNodeKind.CodeSpan
            || node.Kind == MarkdownNodeKind.MathSpan
            || node.Kind == MarkdownNodeKind.Emoji)
        {
            string replacement = node.GetAttributeValue("replacement");
            if (replacement.Length > 0)
            {
                return replacement;
            }

            return node.Literal;
        }

        if (node.Kind == MarkdownNodeKind.SoftLineBreak || node.Kind == MarkdownNodeKind.HardLineBreak)
        {
            return "\n";
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < node.GetChildCount())
        {
            writer.Write(GetPlainText(node.GetChild(index)));
            index = index + 1;
        }

        return writer.ToString();
    }

    internal static bool IsCjkCharacter(char character)
    {
        int code = character;
        return (code >= 0x3040 && code <= 0x30FF)
            || (code >= 0x3400 && code <= 0x9FFF)
            || (code >= 0xF900 && code <= 0xFAFF)
            || (code >= 0xAC00 && code <= 0xD7AF)
            || (code >= 0xFF00 && code <= 0xFFEF);
    }
}
