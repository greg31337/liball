using System;
using System.Collections.Generic;
using System.Text;

namespace template;

internal class MustacheTagFrame
{
    internal string Kind;
    internal string Name;

    internal MustacheTagFrame(string kind, string name)
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

internal class MustacheDelimiterState
{
    internal string StartDelimiter;
    internal string EndDelimiter;

    internal MustacheDelimiterState()
    {
        StartDelimiter = "{{";
        EndDelimiter = "}}";
    }
}

/// <summary>
/// Translates supported Mustache constructs into native template syntax.
/// </summary>
internal static class MustacheTemplateConverter
{
    /// <summary>
    /// Converts supported source-dialect constructs to equivalent native syntax and leaves unsupported text intact.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string ConvertParentBlocksToNative(string text)
    {
        return ConvertToNative(text);
    }

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
        List<MustacheTagFrame> stack = new List<MustacheTagFrame>();
        MustacheDelimiterState delimiters = new MustacheDelimiterState();
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf(delimiters.StartDelimiter, index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            bool triple = delimiters.StartDelimiter == "{{" && StartsWithAt(text, start, "{{{");
            string currentEndDelimiter = delimiters.EndDelimiter;
            int contentStart = start + delimiters.StartDelimiter.Length;
            if (triple)
            {
                currentEndDelimiter = "}}}";
                contentStart = start + 3;
            }

            int end = text.IndexOf(currentEndDelimiter, contentStart, StringComparison.Ordinal);
            if (end < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            builder.Append(text.Substring(index, start - index));
            string tagText = text.Substring(contentStart, end - contentStart).Trim();
            bool converted = false;
            if (IsDelimiterChangeTag(tagText))
            {
                ApplyDelimiterChange(tagText, delimiters);
                converted = true;
            }
            else
            {
                converted = ConvertTag(builder, tagText, triple, stack);
            }

            if (!converted)
            {
                builder.Append(text.Substring(start, end - start + currentEndDelimiter.Length));
            }

            index = end + currentEndDelimiter.Length;
        }

        return builder.ToString();
    }

    internal static bool ConvertTag(StringBuilder builder, string tagText, bool rawOutput, List<MustacheTagFrame> stack)
    {
        if (tagText.Length == 0)
        {
            return false;
        }

        if (tagText[0] == '!')
        {
            return true;
        }

        if (tagText[0] == '#')
        {
            string sectionName = NormalizeExpression(tagText.Substring(1).Trim());
            if (sectionName == "")
            {
                return false;
            }

            builder.Append("{% mustacheSection ");
            builder.Append(sectionName);
            builder.Append(" %}");
            stack.Add(new MustacheTagFrame("mustacheSection", sectionName));
            return true;
        }

        if (tagText[0] == '^')
        {
            string sectionName = NormalizeExpression(tagText.Substring(1).Trim());
            if (sectionName == "")
            {
                return false;
            }

            builder.Append("{% mustacheInverted ");
            builder.Append(sectionName);
            builder.Append(" %}");
            stack.Add(new MustacheTagFrame("mustacheInverted", sectionName));
            return true;
        }

        if (tagText[0] == '>')
        {
            string partialName = tagText.Substring(1).Trim();
            if (partialName == "")
            {
                return false;
            }

            builder.Append("{% partial ");
            AppendNameArgument(builder, partialName);
            builder.Append(" %}");
            return true;
        }

        if (tagText[0] == '<')
        {
            string parentName = tagText.Substring(1).Trim();
            if (parentName == "")
            {
                return false;
            }

            builder.Append("{% extends ");
            AppendNameArgument(builder, parentName);
            builder.Append(" %}");
            stack.Add(new MustacheTagFrame("parent", parentName));
            return true;
        }

        if (tagText[0] == '$')
        {
            string blockName = tagText.Substring(1).Trim();
            if (blockName == "")
            {
                return false;
            }

            builder.Append("{% block ");
            AppendNameArgument(builder, blockName);
            builder.Append(" %}");
            stack.Add(new MustacheTagFrame("block", blockName));
            return true;
        }

        if (tagText[0] == '/')
        {
            string closeName = tagText.Substring(1).Trim();
            int stackIndex = stack.Count - 1;
            if (stackIndex >= 0)
            {
                MustacheTagFrame frame = stack[stackIndex];
                stack.RemoveAt(stackIndex);
                if (frame.Kind == "parent")
                {
                    return true;
                }

                if (frame.Kind == "block")
                {
                    builder.Append("{% endblock %}");
                    return true;
                }

                builder.Append("{% end");
                builder.Append(frame.Kind);
                builder.Append(" %}");
                return true;
            }
        }

        if (tagText[0] == '&')
        {
            string expression = NormalizeExpression(tagText.Substring(1).Trim());
            if (expression == "")
            {
                return false;
            }

            AppendOutput(builder, expression, true);
            return true;
        }

        AppendOutput(builder, NormalizeExpression(tagText), rawOutput);
        return true;
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

    internal static void AppendNameArgument(StringBuilder builder, string name)
    {
        if (IsDynamicName(name))
        {
            builder.Append(NormalizeExpression(name.Substring(1).Trim()));
        }
        else
        {
            AppendQuoted(builder, name);
        }
    }

    internal static bool IsDynamicName(string name)
    {
        return name.Length > 1 && name[0] == '*';
    }

    internal static string NormalizeExpression(string expression)
    {
        if (expression == ".")
        {
            return "this";
        }

        return expression;
    }

    internal static bool IsDelimiterChangeTag(string tagText)
    {
        return tagText.Length >= 2 && tagText[0] == '=' && tagText[tagText.Length - 1] == '=';
    }

    internal static void ApplyDelimiterChange(string tagText, MustacheDelimiterState delimiters)
    {
        string delimiterText = tagText.Substring(1, tagText.Length - 2).Trim();
        int split = delimiterText.IndexOf(" ", StringComparison.Ordinal);
        if (split < 0)
        {
            return;
        }

        string nextStart = delimiterText.Substring(0, split).Trim();
        string nextEnd = delimiterText.Substring(split + 1).Trim();
        if (nextStart == "" || nextEnd == "")
        {
            return;
        }

        delimiters.StartDelimiter = nextStart;
        delimiters.EndDelimiter = nextEnd;
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
