using System;
using System.Collections.Generic;
using System.IO;

namespace markdown;

internal class HeadingAttributeParser
{
    internal static bool TryApplyHeadingAttributes(MarkdownNode heading, MarkdownPipeline pipeline)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (heading.Kind != MarkdownNodeKind.Heading)
        {
            throw new ArgumentException("Node kind must be a heading.");
        }

        if (!pipeline.GetFeatures().Attributes)
        {
            return false;
        }

        int start = FindTrailingAttributeStart(heading.Literal);
        if (start < 0)
        {
            return false;
        }

        int end = FindTrailingAttributeEnd(heading.Literal);
        string text = heading.Literal.Substring(start + 1, end - start - 1);
        List<string> tokens = TokenizeAttributeBlock(text);
        if (!AreAttributeTokens(tokens))
        {
            return false;
        }

        int index = 0;
        while (index < tokens.Count)
        {
            ApplyAttributeToken(heading, tokens[index]);
            index = index + 1;
        }

        heading.SetLiteral(heading.Literal.Substring(0, start).TrimEnd());
        heading.SetAttribute("explicitAttributes", "true");
        return true;
    }

    internal static int FindTrailingAttributeStart(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int end = FindTrailingAttributeEnd(text);
        if (end < 0)
        {
            return -1;
        }

        int index = end - 1;
        while (index >= 0)
        {
            if (text[index] == '{')
            {
                if (index > 0 && !MarkdownChar.IsWhitespace(text[index - 1]))
                {
                    return -1;
                }

                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal static int FindTrailingAttributeEnd(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = text.Length;
        while (index > 0 && MarkdownChar.IsWhitespace(text[index - 1]))
        {
            index = index - 1;
        }

        if (index == 0 || text[index - 1] != '}')
        {
            return -1;
        }

        return index - 1;
    }

    internal static List<string> TokenizeAttributeBlock(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        List<string> tokens = new List<string>();
        StringWriter writer = new StringWriter();
        bool inQuote = false;
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (inQuote)
            {
                if (character == quote)
                {
                    inQuote = false;
                }
                else
                {
                    writer.Write(character);
                }
            }
            else if (character == '"' || character == '\'')
            {
                inQuote = true;
                quote = character;
            }
            else if (MarkdownChar.IsWhitespace(character))
            {
                AddToken(tokens, writer);
            }
            else
            {
                writer.Write(character);
            }

            index = index + 1;
        }

        AddToken(tokens, writer);
        return tokens;
    }

    internal static void AddToken(List<string> tokens, StringWriter writer)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        string token = writer.ToString();
        if (token.Length > 0)
        {
            tokens.Add(token);
            writer.GetStringBuilder().Clear();
        }
    }

    internal static bool AreAttributeTokens(List<string> tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (tokens.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < tokens.Count)
        {
            if (!IsAttributeToken(tokens[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAttributeToken(string token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.Length < 2)
        {
            return false;
        }

        if (token[0] == '#' || token[0] == '.')
        {
            return token.Length > 1;
        }

        int equals = token.IndexOf('=');
        return equals > 0 && IsAttributeName(token.Substring(0, equals));
    }

    internal static bool IsAttributeName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0 || !IsAttributeNameStart(name[0]))
        {
            return false;
        }

        int index = 1;
        while (index < name.Length)
        {
            char character = name[index];
            if (!IsAttributeNameStart(character)
                && (character < '0' || character > '9')
                && character != '-'
                && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAttributeNameStart(char character)
    {
        return (character >= 'a' && character <= 'z')
            || (character >= 'A' && character <= 'Z')
            || character == '_'
            || character == ':';
    }

    internal static void ApplyAttributeToken(MarkdownNode heading, string token)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token[0] == '#')
        {
            heading.SetAttribute("id", token.Substring(1));
            RecordAttachedAttribute(heading, "id", false);
            return;
        }

        if (token[0] == '.')
        {
            AddClass(heading, token.Substring(1));
            RecordAttachedAttribute(heading, "class", false);
            return;
        }

        int equals = token.IndexOf('=');
        if (equals > 0)
        {
            string name = token.Substring(0, equals);
            heading.SetAttribute(name, token.Substring(equals + 1));
            RecordAttachedAttribute(heading, name, false);
        }
    }

    internal static void RecordAttachedAttribute(MarkdownNode node, string name, bool isBlock)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        MarkdownAttachedAttributeData attached = node.Data.AttachedAttributes;
        if (!attached.Names.Contains(name))
        {
            attached.Names.Add(name);
        }
        attached.IsBlock = isBlock;
    }

    internal static void AddClass(MarkdownNode heading, string className)
    {
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (className == null)
        {
            throw new ArgumentNullException("className");
        }

        if (className.Length == 0)
        {
            return;
        }

        string existing = heading.GetAttributeValue("class");
        if (existing.Length == 0)
        {
            heading.SetAttribute("class", className);
        }
        else
        {
            heading.SetAttribute("class", existing + " " + className);
        }
    }
}
