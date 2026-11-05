using System;
using System.IO;

namespace markdown;

internal class TypographyProcessor
{
    internal static void Apply(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        ApplyNode(document.Root);
    }

    internal static void ApplyNode(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind == MarkdownNodeKind.Text)
        {
            node.SetLiteral(ApplyText(node.Literal));
            return;
        }

        if (!ShouldProcessChildren(node))
        {
            return;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            ApplyNode(node.GetChild(index));
            index = index + 1;
        }
    }

    internal static string ApplyText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringWriter writer = new StringWriter();
        bool doubleQuoteOpen = true;
        bool singleQuoteOpen = true;
        int index = 0;
        while (index < text.Length)
        {
            if (StartsWithIgnoreCaseAt(text, index, "(tm)"))
            {
                writer.Write("™");
                index = index + 4;
            }
            else if (StartsWithIgnoreCaseAt(text, index, "(c)"))
            {
                writer.Write("©");
                index = index + 3;
            }
            else if (StartsWithIgnoreCaseAt(text, index, "(r)"))
            {
                writer.Write("®");
                index = index + 3;
            }
            else if (StartsWithAt(text, index, "..."))
            {
                writer.Write("…");
                index = index + 3;
            }
            else if (StartsWithAt(text, index, "---"))
            {
                writer.Write("—");
                index = index + 3;
            }
            else if (StartsWithAt(text, index, "--"))
            {
                writer.Write("–");
                index = index + 2;
            }
            else if (text[index] == '"')
            {
                if (doubleQuoteOpen)
                {
                    writer.Write("“");
                }
                else
                {
                    writer.Write("”");
                }

                doubleQuoteOpen = !doubleQuoteOpen;
                index = index + 1;
            }
            else if (text[index] == '\'')
            {
                if (IsApostrophe(text, index))
                {
                    writer.Write("’");
                }
                else if (singleQuoteOpen)
                {
                    writer.Write("‘");
                    singleQuoteOpen = false;
                }
                else
                {
                    writer.Write("’");
                    singleQuoteOpen = true;
                }

                index = index + 1;
            }
            else
            {
                writer.Write(text[index]);
                index = index + 1;
            }
        }

        return writer.ToString();
    }

    internal static bool ShouldProcessChildren(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.Kind != MarkdownNodeKind.CodeSpan
            && node.Kind != MarkdownNodeKind.HtmlInline
            && node.Kind != MarkdownNodeKind.MathSpan
            && node.Kind != MarkdownNodeKind.MathBlock
            && node.Kind != MarkdownNodeKind.FencedCodeBlock
            && node.Kind != MarkdownNodeKind.IndentedCodeBlock
            && node.Kind != MarkdownNodeKind.HtmlBlock
            && node.Kind != MarkdownNodeKind.DiagramBlock
            && node.Kind != MarkdownNodeKind.FrontMatter
            && node.Kind != MarkdownNodeKind.Emoji;
    }

    internal static bool IsApostrophe(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (index == 0 || index >= text.Length - 1)
        {
            return false;
        }

        return char.IsLetterOrDigit(text[index - 1]) && char.IsLetterOrDigit(text[index + 1]);
    }

    internal static bool StartsWithAt(string text, int index, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (value.Length > text.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < value.Length)
        {
            if (text[index + offset] != value[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static bool StartsWithIgnoreCaseAt(string text, int index, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (value.Length > text.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < value.Length)
        {
            char left = char.ToLowerInvariant(text[index + offset]);
            char right = char.ToLowerInvariant(value[offset]);
            if (left != right)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }
}
