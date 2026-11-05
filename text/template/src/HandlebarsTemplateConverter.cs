using System;
using System.Collections.Generic;
using System.Text;

namespace template;

internal class HandlebarsTagFrame
{
    internal string Kind;
    internal string Name;

    internal HandlebarsTagFrame(string kind, string name)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Kind = kind;
        Name = name;
    }
}

/// <summary>
/// Translates supported Handlebars expressions, helpers, blocks, and partials into native syntax.
/// </summary>
internal static class HandlebarsTemplateConverter
{
    /// <summary>
    /// Converts supported source-dialect constructs to equivalent native syntax and leaves unsupported text intact.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string ConvertToNative(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        List<HandlebarsTagFrame> stack = new List<HandlebarsTagFrame>();
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            if (StartsWithAt(text, start, "{{{{"))
            {
                builder.Append(text.Substring(index, start - index));
                index = ConvertRawBlock(builder, text, start);
                continue;
            }

            bool triple = StartsWithAt(text, start, "{{{");
            string endDelimiter = "}}";
            int contentStart = start + 2;
            if (triple)
            {
                endDelimiter = "}}}";
                contentStart = start + 3;
            }

            int end = text.IndexOf(endDelimiter, contentStart, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException("Unclosed Handlebars tag.");
            }

            int precedingSlashes = CountPrecedingBackslashes(text, start);
            if (precedingSlashes % 2 == 1)
            {
                builder.Append(text.Substring(index, start - index));
                builder.Append(text.Substring(start, end - start + endDelimiter.Length));
                index = end + endDelimiter.Length;
                continue;
            }

            builder.Append(text.Substring(index, start - index));
            string tagContent = text.Substring(contentStart, end - contentStart);
            bool leftTrim = HasHandlebarsLeftTrim(tagContent);
            bool rightTrim = HasHandlebarsRightTrim(tagContent);
            string tagText = TrimHandlebarsTag(tagContent);
            int convertedStart = builder.Length;
            bool converted = ConvertTag(builder, tagText, triple, stack);
            if (converted)
            {
                ApplyNativeTrimMarkers(builder, convertedStart, leftTrim, rightTrim);
            }

            if (!converted)
            {
                builder.Append(text.Substring(start, end - start + endDelimiter.Length));
            }

            index = end + endDelimiter.Length;
        }

        if (stack.Count > 0)
        {
            throw new InvalidOperationException("Unclosed Handlebars block '" + stack[stack.Count - 1].Name + "'.");
        }

        return builder.ToString();
    }

    internal static int ConvertRawBlock(StringBuilder builder, string text, int start)
    {
        int openEnd = text.IndexOf("}}}}", start + 4, StringComparison.Ordinal);
        if (openEnd < 0)
        {
            throw new InvalidOperationException("Unclosed Handlebars raw block tag.");
        }

        string opening = text.Substring(start + 4, openEnd - start - 4).Trim();
        List<string> parts = SplitTopLevelArguments(opening);
        if (parts.Count == 0)
        {
            throw new InvalidOperationException("Handlebars raw block helper name is required.");
        }

        string name = parts[0];
        string closeTag = "{{{{/" + name + "}}}}";
        int contentStart = openEnd + 4;
        int closeStart = text.IndexOf(closeTag, contentStart, StringComparison.Ordinal);
        if (closeStart < 0)
        {
            throw new InvalidOperationException("Unclosed Handlebars raw block '" + name + "'.");
        }

        string content = text.Substring(contentStart, closeStart - contentStart);
        if (name == "raw" && parts.Count == 1)
        {
            builder.Append("{% raw %}");
            builder.Append(content);
            builder.Append("{% endraw %}");
        }
        else
        {
            builder.Append("{% handlebarsBlock ");
            builder.Append(ConvertExpression(opening));
            builder.Append(" %}{% raw %}");
            builder.Append(content);
            builder.Append("{% endraw %}{% endhandlebarsBlock %}");
        }

        return closeStart + closeTag.Length;
    }

    internal static int CountPrecedingBackslashes(string text, int start)
    {
        int count = 0;
        int index = start - 1;
        while (index >= 0 && text[index] == '\\')
        {
            count = count + 1;
            index = index - 1;
        }

        return count;
    }

    internal static bool ConvertTag(StringBuilder builder, string tagText, bool rawOutput, List<HandlebarsTagFrame> stack)
    {
        if (tagText == "")
        {
            return false;
        }

        if (tagText[0] == '!')
        {
            builder.Append("{# #}");
            return true;
        }

        if (tagText == "else")
        {
            builder.Append("{% else %}");
            return true;
        }

        if (StartsWithText(tagText, "else if "))
        {
            builder.Append("{% elseif ");
            builder.Append(ConvertConditionalExpression(tagText.Substring(8).Trim()));
            builder.Append(" %}");
            return true;
        }

        if (StartsWithText(tagText, "#*inline "))
        {
            string partialName = tagText.Substring(9).Trim();
            builder.Append("{% handlebarsInlinePartial ");
            AppendNameArgument(builder, partialName);
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("handlebarsInlinePartial", "inline"));
            return true;
        }

        if (StartsWithText(tagText, "#>"))
        {
            string partialName = tagText.Substring(2).Trim();
            builder.Append("{% handlebarsPartialBlock ");
            AppendPartialInvocation(builder, partialName);
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("handlebarsPartialBlock", GetFirstSegment(partialName)));
            return true;
        }

        if (tagText[0] == '#')
        {
            return ConvertBlockStart(builder, tagText.Substring(1).Trim(), stack);
        }

        if (tagText[0] == '^')
        {
            string invertedText = tagText.Substring(1).Trim();
            builder.Append("{% unless ");
            builder.Append(ConvertExpression(invertedText));
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("unless", GetFirstSegment(invertedText)));
            return true;
        }

        if (tagText[0] == '/')
        {
            return ConvertBlockEnd(builder, tagText.Substring(1).Trim(), stack);
        }

        if (tagText[0] == '>')
        {
            return ConvertPartial(builder, tagText.Substring(1).Trim());
        }

        if (tagText[0] == '&')
        {
            AppendOutput(builder, NormalizeExpression(tagText.Substring(1).Trim()), true);
            return true;
        }

        AppendOutput(builder, ConvertExpression(tagText), rawOutput);
        return true;
    }

    internal static bool ConvertBlockStart(StringBuilder builder, string tagText, List<HandlebarsTagFrame> stack)
    {
        if (StartsWithText(tagText, "if "))
        {
            builder.Append("{% if ");
            builder.Append(ConvertConditionalExpression(tagText.Substring(3).Trim()));
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("if", "if"));
            return true;
        }

        if (StartsWithText(tagText, "unless "))
        {
            builder.Append("{% unless ");
            builder.Append(ConvertConditionalExpression(tagText.Substring(7).Trim()));
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("unless", "unless"));
            return true;
        }

        if (StartsWithText(tagText, "with "))
        {
            builder.Append("{% with ");
            builder.Append(NormalizeExpression(tagText.Substring(5).Trim()));
            builder.Append(" %}");
            stack.Add(new HandlebarsTagFrame("with", "with"));
            return true;
        }

        if (StartsWithText(tagText, "each "))
        {
            ConvertEachBlockStart(builder, tagText.Substring(5).Trim());
            stack.Add(new HandlebarsTagFrame("handlebarsEach", "each"));
            return true;
        }

        string expression = ConvertExpression(tagText);
        if (tagText == "empty")
        {
            expression = "this[\"empty\"]";
        }

        builder.Append("{% handlebarsBlock ");
        builder.Append(expression);
        builder.Append(" %}");
        stack.Add(new HandlebarsTagFrame("handlebarsBlock", GetFirstSegment(tagText)));
        return true;
    }

    internal static bool ConvertBlockEnd(StringBuilder builder, string closeName, List<HandlebarsTagFrame> stack)
    {
        int stackIndex = stack.Count - 1;
        if (stackIndex < 0)
        {
            throw new InvalidOperationException("Unexpected Handlebars closing tag '" + closeName + "'.");
        }

        HandlebarsTagFrame frame = stack[stackIndex];
        if (frame.Name != closeName)
        {
            throw new InvalidOperationException("Handlebars closing tag '" + closeName + "' does not match '" + frame.Name + "'.");
        }

        stack.RemoveAt(stackIndex);
        builder.Append("{% end");
        builder.Append(frame.Kind);
        builder.Append(" %}");
        return true;
    }

    internal static string ConvertConditionalExpression(string text)
    {
        List<string> parts = SplitTopLevelArguments(text);
        if (parts.Count == 0)
        {
            return "false";
        }

        string expression = NormalizeExpression(parts[0]);
        int index = 1;
        while (index < parts.Count)
        {
            if (parts[index] == "includeZero=true")
            {
                return "(" + expression + ") or (" + expression + " == 0)";
            }

            index = index + 1;
        }

        return expression;
    }

    internal static void ConvertEachBlockStart(StringBuilder builder, string text)
    {
        string expression = text;
        string itemAlias = "";
        string indexAlias = "";
        int asIndex = text.IndexOf(" as |", StringComparison.Ordinal);
        if (asIndex >= 0)
        {
            expression = text.Substring(0, asIndex).Trim();
            string aliases = text.Substring(asIndex + 5).Trim();
            if (aliases.Length > 0 && aliases[aliases.Length - 1] == '|')
            {
                aliases = aliases.Substring(0, aliases.Length - 1).Trim();
            }

            List<string> aliasParts = SplitTopLevelArguments(aliases);
            if (aliasParts.Count > 0)
            {
                itemAlias = aliasParts[0];
            }

            if (aliasParts.Count > 1)
            {
                indexAlias = aliasParts[1];
            }
        }

        builder.Append("{% handlebarsEach ");
        builder.Append(NormalizeExpression(expression));
        if (itemAlias != "")
        {
            builder.Append(" ");
            AppendQuoted(builder, itemAlias);
        }

        if (indexAlias != "")
        {
            builder.Append(" ");
            AppendQuoted(builder, indexAlias);
        }

        builder.Append(" %}");
    }

    internal static bool ConvertPartial(StringBuilder builder, string partialName)
    {
        if (partialName == "")
        {
            return false;
        }

        if (partialName == "@partial-block")
        {
            builder.Append("{% handlebarsPartialBlockBody %}");
            return true;
        }

        builder.Append("{% partial ");
        AppendPartialInvocation(builder, partialName);
        builder.Append(" %}");
        return true;
    }

    internal static void AppendPartialInvocation(StringBuilder builder, string text)
    {
        List<string> parts = SplitTopLevelArguments(text);
        if (parts.Count == 0)
        {
            return;
        }

        AppendNameArgument(builder, parts[0]);
        int index = 1;
        while (index < parts.Count)
        {
            string part = parts[index];
            int equalsIndex = FindTopLevelEquals(part);
            builder.Append(" ");
            if (equalsIndex > 0)
            {
                builder.Append(part.Substring(0, equalsIndex));
                builder.Append("=");
                builder.Append(NormalizeExpression(part.Substring(equalsIndex + 1)));
            }
            else
            {
                builder.Append(NormalizeExpression(part));
            }

            index = index + 1;
        }
    }

    internal static void AppendOutput(StringBuilder builder, string expression, bool raw)
    {
        if (expression == "")
        {
            return;
        }

        builder.Append("{{ ");
        builder.Append(expression);
        if (raw)
        {
            builder.Append(" | safe");
        }

        builder.Append(" }}");
    }

    internal static string ConvertExpression(string expression)
    {
        string normalized = NormalizeExpression(expression);
        List<string> parts = SplitTopLevelArguments(normalized);
        if (parts.Count <= 1)
        {
            return normalized;
        }

        string name = parts[0];
        if (!IsSimpleIdentifier(name))
        {
            return normalized;
        }

        StringBuilder builder = new StringBuilder();
        StringBuilder hashBuilder = new StringBuilder();
        int argumentCount = 0;
        int hashCount = 0;
        builder.Append(name);
        builder.Append("(");
        int index = 1;
        while (index < parts.Count)
        {
            string part = parts[index];
            int equalsIndex = FindTopLevelEquals(part);
            if (equalsIndex > 0)
            {
                if (hashCount > 0)
                {
                    hashBuilder.Append(", ");
                }

                AppendQuoted(hashBuilder, part.Substring(0, equalsIndex));
                hashBuilder.Append(": ");
                hashBuilder.Append(NormalizeExpression(part.Substring(equalsIndex + 1)));
                hashCount = hashCount + 1;
            }
            else
            {
                if (argumentCount > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(NormalizeExpression(part));
                argumentCount = argumentCount + 1;
            }

            index = index + 1;
        }

        if (hashCount > 0)
        {
            if (argumentCount > 0)
            {
                builder.Append(", ");
            }

            builder.Append("{");
            builder.Append(hashBuilder.ToString());
            builder.Append("}");
        }

        builder.Append(")");
        return builder.ToString();
    }

    internal static string NormalizeExpression(string expression)
    {
        string normalized = expression.Trim();
        if (normalized == ".")
        {
            return "this";
        }

        if (normalized == "@partial-block")
        {
            return "@partialBlock";
        }

        if (normalized == "@root")
        {
            return "$";
        }

        if (StartsWithText(normalized, "@root.") || StartsWithText(normalized, "@root/"))
        {
            return "$" + normalized.Substring(5).Replace('/', '.');
        }

        if (StartsWithText(normalized, "@../"))
        {
            string parentDataName = normalized.Substring(4);
            if (parentDataName != "")
            {
                return "@parent" + char.ToUpperInvariant(parentDataName[0]) + parentDataName.Substring(1);
            }
        }

        if (StartsWithText(normalized, "./"))
        {
            normalized = "this." + normalized.Substring(2);
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < normalized.Length)
        {
            char current = normalized[index];
            if (current == '/' && (index == 0 || normalized[index - 1] != '.'))
            {
                builder.Append(".");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void AppendNameArgument(StringBuilder builder, string name)
    {
        if (IsDynamicName(name))
        {
            builder.Append(NormalizeDynamicName(name));
        }
        else if (IsQuotedName(name))
        {
            builder.Append(name);
        }
        else
        {
            AppendQuoted(builder, name);
        }
    }

    internal static bool IsDynamicName(string name)
    {
        return name.Length > 1 && name[0] == '(';
    }

    internal static bool IsQuotedName(string name)
    {
        if (name.Length < 2)
        {
            return false;
        }

        if (name[0] == '"' && name[name.Length - 1] == '"')
        {
            return true;
        }

        if (name[0] == '\'' && name[name.Length - 1] == '\'')
        {
            return true;
        }

        return false;
    }

    internal static string NormalizeDynamicName(string name)
    {
        if (name.Length >= 2 && name[0] == '(' && name[name.Length - 1] == ')')
        {
            return NormalizeExpression(name.Substring(1, name.Length - 2).Trim());
        }

        return NormalizeExpression(name);
    }

    internal static List<string> SplitTopLevelArguments(string text)
    {
        List<string> parts = new List<string>();
        int depth = 0;
        int start = -1;
        int index = 0;
        char quote = '\0';
        while (index < text.Length)
        {
            char current = text[index];
            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }
            }
            else if (current == '\'' || current == '"')
            {
                quote = current;
            }
            else if (current == '(' || current == '[' || current == '{')
            {
                depth = depth + 1;
            }
            else if (current == ')' || current == ']' || current == '}')
            {
                depth = depth - 1;
            }

            if (char.IsWhiteSpace(current) && depth == 0 && quote == '\0')
            {
                if (start >= 0)
                {
                    parts.Add(text.Substring(start, index - start));
                    start = -1;
                }
            }
            else if (start < 0)
            {
                start = index;
            }

            index = index + 1;
        }

        if (start >= 0)
        {
            parts.Add(text.Substring(start));
        }

        return parts;
    }

    internal static int FindTopLevelEquals(string text)
    {
        int depth = 0;
        int index = 0;
        char quote = '\0';
        while (index < text.Length)
        {
            char current = text[index];
            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }
            }
            else if (current == '\'' || current == '"')
            {
                quote = current;
            }
            else if (current == '(' || current == '[' || current == '{')
            {
                depth = depth + 1;
            }
            else if (current == ')' || current == ']' || current == '}')
            {
                depth = depth - 1;
            }
            else if (current == '=' && depth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string GetFirstSegment(string text)
    {
        List<string> parts = SplitTopLevelArguments(text);
        if (parts.Count == 0)
        {
            return "";
        }

        return parts[0];
    }

    internal static bool IsSimpleIdentifier(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        if (!IsIdentifierStart(text[0]))
        {
            return false;
        }

        int index = 1;
        while (index < text.Length)
        {
            if (!IsIdentifierPart(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    internal static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    internal static string TrimHandlebarsTag(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length > 0 && trimmed[0] == '~')
        {
            trimmed = trimmed.Substring(1).TrimStart();
        }

        if (trimmed.Length > 0 && trimmed[trimmed.Length - 1] == '~')
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
        }

        return trimmed;
    }

    internal static bool HasHandlebarsLeftTrim(string text)
    {
        string trimmed = text.TrimStart();
        return trimmed.Length > 0 && trimmed[0] == '~';
    }

    internal static bool HasHandlebarsRightTrim(string text)
    {
        string trimmed = text.TrimEnd();
        return trimmed.Length > 0 && trimmed[trimmed.Length - 1] == '~';
    }

    internal static void ApplyNativeTrimMarkers(StringBuilder builder, int start, bool leftTrim, bool rightTrim)
    {
        if (builder.Length <= start)
        {
            return;
        }

        if (leftTrim)
        {
            builder.Insert(start + 2, '-');
        }

        if (rightTrim)
        {
            builder.Insert(builder.Length - 2, '-');
        }
    }

    internal static bool StartsWithText(string text, string value)
    {
        return text.Length >= value.Length && text.Substring(0, value.Length) == value;
    }

    internal static bool StartsWithAt(string text, int index, string value)
    {
        if (index < 0 || index > text.Length || value.Length > text.Length - index)
        {
            return false;
        }

        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (text[index + valueIndex] != value[valueIndex])
            {
                return false;
            }

            valueIndex = valueIndex + 1;
        }

        return true;
    }

    internal static void AppendQuoted(StringBuilder builder, string text)
    {
        builder.Append("\"");
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\\' || current == '"')
            {
                builder.Append("\\");
            }

            builder.Append(current);
            index = index + 1;
        }

        builder.Append("\"");
    }
}
