using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Converts the documented Scriban subset into native syntax.
/// </summary>
internal static class ScribanTemplateConverter
{
    /// <summary>
    /// Converts Scriban code blocks, output expressions, assignments, and control flow into native syntax.
    /// </summary>
    /// <param name="text">The source template.</param>
    /// <returns>The native template text.</returns>
    public static string ConvertToNative(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        List<string> blocks = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            builder.Append(text.Substring(index, start - index));
            if (start + 3 < text.Length && text.Substring(start, 4) == "{{##")
            {
                int commentEnd = text.IndexOf("##}}", start + 4, StringComparison.Ordinal);
                if (commentEnd < 0)
                {
                    return builder.ToString();
                }

                index = commentEnd + 4;
                continue;
            }

            int end = text.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                builder.Append(text.Substring(start));
                break;
            }

            string original = text.Substring(start + 2, end - start - 2);
            bool leftGreedy = HasOpeningMarker(original, '-');
            bool leftNonGreedy = HasOpeningMarker(original, '~');
            bool rightGreedy = HasClosingMarker(original, '-');
            bool rightNonGreedy = HasClosingMarker(original, '~');
            string content = TrimMarkers(original);
            if (IsScribanStatement(content))
            {
                string statement = NormalizeScribanStatement(content, blocks);
                AppendStatement(builder, statement, leftGreedy, leftNonGreedy, rightGreedy, rightNonGreedy);
            }
            else
            {
                AppendOutput(builder, content, leftGreedy, leftNonGreedy, rightGreedy, rightNonGreedy);
            }

            index = end + 2;
        }

        return builder.ToString();
    }

    internal static bool IsScribanStatement(string content)
    {
        return StartsWithWord(content, "if")
            || StartsWithWord(content, "else")
            || StartsWithWord(content, "for")
            || StartsWithWord(content, "while")
            || StartsWithWord(content, "case")
            || StartsWithWord(content, "when")
            || StartsWithWord(content, "capture")
            || StartsWithWord(content, "with")
            || StartsWithWord(content, "import")
            || StartsWithWord(content, "include")
            || StartsWithWord(content, "ret")
            || content == "break"
            || content == "continue"
            || content == "end"
            || FindAssignment(content) >= 0;
    }

    internal static string NormalizeScribanStatement(string content, List<string> blocks)
    {
        if (content == "end")
        {
            if (blocks.Count == 0)
            {
                return "end";
            }

            int index = blocks.Count - 1;
            string name = blocks[index];
            blocks.RemoveAt(index);
            return "end" + name;
        }

        if (StartsWithWord(content, "else if"))
        {
            return "elseif " + content.Substring(7).Trim();
        }

        if (content == "else" || StartsWithWord(content, "when"))
        {
            return content;
        }

        string blockName = GetScribanBlockName(content);
        if (blockName != "")
        {
            blocks.Add(blockName);
            return content;
        }

        if (StartsWithWord(content, "ret"))
        {
            return "return" + content.Substring(3);
        }

        int assignment = FindAssignment(content);
        if (assignment >= 0)
        {
            return "set " + content;
        }

        return content;
    }

    internal static string GetScribanBlockName(string content)
    {
        if (StartsWithWord(content, "if"))
        {
            return "if";
        }

        if (StartsWithWord(content, "for"))
        {
            return "for";
        }

        if (StartsWithWord(content, "while"))
        {
            return "while";
        }

        if (StartsWithWord(content, "case"))
        {
            return "case";
        }

        if (StartsWithWord(content, "capture"))
        {
            return "capture";
        }

        if (StartsWithWord(content, "with"))
        {
            return "with";
        }

        return "";
    }

    internal static int FindAssignment(string text)
    {
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0' && text[index] == '=')
            {
                char previous = index > 0 ? text[index - 1] : '\0';
                char next = index + 1 < text.Length ? text[index + 1] : '\0';
                if (previous != '=' && previous != '!' && previous != '<' && previous != '>' && next != '=')
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void AppendStatement(StringBuilder builder, string content, bool leftGreedy, bool leftNonGreedy, bool rightGreedy, bool rightNonGreedy)
    {
        builder.Append("{%" + GetMarker(leftGreedy, leftNonGreedy) + " " + content + " " + GetMarker(rightGreedy, rightNonGreedy) + "%}");
    }

    internal static void AppendOutput(StringBuilder builder, string content, bool leftGreedy, bool leftNonGreedy, bool rightGreedy, bool rightNonGreedy)
    {
        builder.Append("{{" + GetMarker(leftGreedy, leftNonGreedy) + " " + content + " " + GetMarker(rightGreedy, rightNonGreedy) + "}}");
    }

    internal static string GetMarker(bool greedy, bool nonGreedy)
    {
        if (greedy)
        {
            return "-";
        }

        return nonGreedy ? "~" : "";
    }

    internal static bool HasOpeningMarker(string text, char marker)
    {
        string trimmed = text.TrimStart();
        return trimmed.Length > 0 && trimmed[0] == marker;
    }

    internal static bool HasClosingMarker(string text, char marker)
    {
        string trimmed = text.TrimEnd();
        return trimmed.Length > 0 && trimmed[trimmed.Length - 1] == marker;
    }

    internal static string TrimMarkers(string text)
    {
        string result = text.Trim();
        if (result.Length > 0 && (result[0] == '-' || result[0] == '~'))
        {
            result = result.Substring(1).TrimStart();
        }

        if (result.Length > 0 && (result[result.Length - 1] == '-' || result[result.Length - 1] == '~'))
        {
            result = result.Substring(0, result.Length - 1).TrimEnd();
        }

        return result;
    }

    internal static bool StartsWithWord(string text, string word)
    {
        return text == word || (text.Length > word.Length && text.Substring(0, word.Length) == word && char.IsWhiteSpace(text[word.Length]));
    }

    internal static char UpdateQuote(string text, int index, char quote)
    {
        char value = text[index];
        if (quote == '\0')
        {
            return value == '\'' || value == '"' || value == '`' ? value : quote;
        }

        if (value == quote && (index == 0 || text[index - 1] != '\\'))
        {
            return '\0';
        }

        return quote;
    }
}

/// <summary>
/// Converts the documented Go text/template subset into native syntax.
/// </summary>
internal static class GoTextTemplateConverter
{
    /// <summary>
    /// Converts Go actions, dot paths, variables, ranges, conditions, and template calls into native syntax.
    /// </summary>
    /// <param name="text">The source template.</param>
    /// <returns>The native template text.</returns>
    public static string ConvertToNative(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        List<string> blocks = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            builder.Append(text.Substring(index, start - index));
            int end = text.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                builder.Append(text.Substring(start));
                break;
            }

            string original = text.Substring(start + 2, end - start - 2);
            bool leftTrim = ScribanTemplateConverter.HasOpeningMarker(original, '-');
            bool rightTrim = ScribanTemplateConverter.HasClosingMarker(original, '-');
            string content = ScribanTemplateConverter.TrimMarkers(original);
            if (content.Length >= 4 && content.Substring(0, 2) == "/*" && content.Substring(content.Length - 2) == "*/")
            {
                index = end + 2;
                continue;
            }

            string statement = NormalizeGoStatement(content, blocks);
            if (statement != "")
            {
                AppendGoStatement(builder, statement, leftTrim, rightTrim);
            }
            else
            {
                AppendGoOutput(builder, NormalizeGoExpression(content), leftTrim, rightTrim);
            }

            index = end + 2;
        }

        return builder.ToString();
    }

    internal static string NormalizeGoStatement(string content, List<string> blocks)
    {
        if (content == "end")
        {
            if (blocks.Count == 0)
            {
                return "end";
            }

            int index = blocks.Count - 1;
            string name = blocks[index];
            blocks.RemoveAt(index);
            return "end" + name;
        }

        if (content == "else")
        {
            return "else";
        }

        if (ScribanTemplateConverter.StartsWithWord(content, "else if"))
        {
            return "elseif " + NormalizeGoExpression(content.Substring(7).Trim());
        }

        if (ScribanTemplateConverter.StartsWithWord(content, "if"))
        {
            blocks.Add("if");
            return "if " + NormalizeGoExpression(content.Substring(2).Trim());
        }

        if (ScribanTemplateConverter.StartsWithWord(content, "range"))
        {
            blocks.Add("for");
            return NormalizeGoRange(content.Substring(5).Trim());
        }

        if (ScribanTemplateConverter.StartsWithWord(content, "with"))
        {
            blocks.Add("with");
            return NormalizeGoWith(content.Substring(4).Trim());
        }

        if (content == "break" || content == "continue")
        {
            return content;
        }

        if (ScribanTemplateConverter.StartsWithWord(content, "template"))
        {
            string arguments = content.Substring(8).Trim();
            return "include " + NormalizeGoExpression(arguments);
        }

        int declaration = content.IndexOf(":=", StringComparison.Ordinal);
        int assignment = ScribanTemplateConverter.FindAssignment(content);
        if (declaration >= 0)
        {
            string name = content.Substring(0, declaration).Trim().TrimStart('$');
            return "set " + name + " = " + NormalizeGoExpression(content.Substring(declaration + 2).Trim());
        }

        if (assignment >= 0)
        {
            string name = content.Substring(0, assignment).Trim().TrimStart('$');
            return "set " + name + " = " + NormalizeGoExpression(content.Substring(assignment + 1).Trim());
        }

        return "";
    }

    internal static string NormalizeGoRange(string content)
    {
        int declaration = content.IndexOf(":=", StringComparison.Ordinal);
        if (declaration < 0)
        {
            return "for item in " + NormalizeGoExpression(content);
        }

        string names = content.Substring(0, declaration).Replace("$", "").Trim();
        string iterable = NormalizeGoExpression(content.Substring(declaration + 2).Trim());
        return "for " + names + " in " + iterable;
    }

    internal static string NormalizeGoWith(string content)
    {
        int declaration = content.IndexOf(":=", StringComparison.Ordinal);
        if (declaration < 0)
        {
            return "with " + NormalizeGoExpression(content);
        }

        string name = content.Substring(0, declaration).Trim().TrimStart('$');
        string value = NormalizeGoExpression(content.Substring(declaration + 2).Trim());
        return "with " + value + " as |" + name + "|";
    }

    internal static string NormalizeGoExpression(string content)
    {
        string result = content.Trim();
        result = ReplaceGoRawString(result);
        result = ReplaceGoVariablesAndDot(result);
        result = ReplaceGoWord(result, "nil", "null");
        if (ScribanTemplateConverter.StartsWithWord(result, "len"))
        {
            return result.Substring(3).Trim() + "|length";
        }

        string[] comparisons = new string[] { "eq", "ne", "lt", "le", "gt", "ge" };
        string[] operators = new string[] { "==", "!=", "<", "<=", ">", ">=" };
        int index = 0;
        while (index < comparisons.Length)
        {
            if (ScribanTemplateConverter.StartsWithWord(result, comparisons[index]))
            {
                List<string> values = SplitGoArguments(result.Substring(comparisons[index].Length).Trim());
                if (values.Count == 2)
                {
                    return values[0] + " " + operators[index] + " " + values[1];
                }
            }

            index = index + 1;
        }

        if (ScribanTemplateConverter.StartsWithWord(result, "not"))
        {
            return "not " + result.Substring(3).Trim();
        }

        return result;
    }

    internal static string ReplaceGoVariablesAndDot(string text)
    {
        StringBuilder builder = new StringBuilder();
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            quote = ScribanTemplateConverter.UpdateQuote(text, index, quote);
            char value = text[index];
            if (quote == '\0' && value == '$')
            {
                index = index + 1;
                continue;
            }

            if (quote == '\0' && value == '.')
            {
                bool member = index + 1 < text.Length && (char.IsLetter(text[index + 1]) || text[index + 1] == '_');
                if (!member)
                {
                    builder.Append("this");
                }

                index = index + 1;
                continue;
            }

            builder.Append(value);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string ReplaceGoRawString(string text)
    {
        if (text.Length >= 2 && text[0] == '`' && text[text.Length - 1] == '`')
        {
            return "\"" + text.Substring(1, text.Length - 2).Replace("\"", "\\\"") + "\"";
        }

        return text;
    }

    internal static string ReplaceGoWord(string text, string oldValue, string newValue)
    {
        return text == oldValue ? newValue : text;
    }

    internal static List<string> SplitGoArguments(string text)
    {
        List<string> values = new List<string>();
        char quote = '\0';
        int start = 0;
        int index = 0;
        while (index < text.Length)
        {
            quote = ScribanTemplateConverter.UpdateQuote(text, index, quote);
            if (quote == '\0' && char.IsWhiteSpace(text[index]))
            {
                if (index > start)
                {
                    values.Add(text.Substring(start, index - start));
                }

                start = index + 1;
            }

            index = index + 1;
        }

        if (start < text.Length)
        {
            values.Add(text.Substring(start));
        }

        return values;
    }

    internal static void AppendGoStatement(StringBuilder builder, string content, bool leftTrim, bool rightTrim)
    {
        builder.Append(leftTrim ? "{%- " : "{% ");
        builder.Append(content);
        builder.Append(rightTrim ? " -%}" : " %}");
    }

    internal static void AppendGoOutput(StringBuilder builder, string content, bool leftTrim, bool rightTrim)
    {
        builder.Append(leftTrim ? "{{- " : "{{ ");
        builder.Append(content);
        builder.Append(rightTrim ? " -}}" : " }}");
    }
}

/// <summary>
/// Converts a bounded Razor-inspired text-generation subset into native syntax.
/// </summary>
internal static class RazorInspiredTemplateConverter
{
    /// <summary>
    /// Converts explicit and implicit expressions, raw HTML, simple code-block assignments, conditionals, and foreach loops.
    /// </summary>
    /// <param name="text">The source template.</param>
    /// <returns>The native template text.</returns>
    public static string ConvertToNative(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        List<string> blocks = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '}' && blocks.Count > 0)
            {
                index = ConvertRazorBlockEnd(builder, text, index, blocks);
                continue;
            }

            if (text[index] != '@')
            {
                builder.Append(text[index]);
                index = index + 1;
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == '@')
            {
                builder.Append('@');
                index = index + 2;
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == '*')
            {
                int commentEnd = text.IndexOf("*@", index + 2, StringComparison.Ordinal);
                index = commentEnd < 0 ? text.Length : commentEnd + 2;
                continue;
            }

            if (StartsAt(text, index, "@Html.Raw("))
            {
                int open = index + 9;
                int close = FindMatching(text, open, '(', ')');
                if (close < 0)
                {
                    builder.Append(text.Substring(index));
                    break;
                }

                builder.Append("{{ ");
                builder.Append(text.Substring(open + 1, close - open - 1));
                builder.Append("|safe }}");
                index = close + 1;
                continue;
            }

            if (StartsAt(text, index, "@if"))
            {
                index = ConvertRazorConditionStart(builder, text, index, blocks);
                continue;
            }

            if (StartsAt(text, index, "@foreach"))
            {
                index = ConvertRazorForeachStart(builder, text, index, blocks);
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == '(')
            {
                int close = FindMatching(text, index + 1, '(', ')');
                if (close < 0)
                {
                    builder.Append(text.Substring(index));
                    break;
                }

                builder.Append("{{ ");
                builder.Append(text.Substring(index + 2, close - index - 2));
                builder.Append(" }}");
                index = close + 1;
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == '{')
            {
                int close = FindMatching(text, index + 1, '{', '}');
                if (close < 0)
                {
                    builder.Append(text.Substring(index));
                    break;
                }

                AppendRazorAssignments(builder, text.Substring(index + 2, close - index - 2));
                index = close + 1;
                continue;
            }

            int expressionEnd = FindImplicitExpressionEnd(text, index + 1);
            if (expressionEnd == index + 1 || (index > 0 && char.IsLetterOrDigit(text[index - 1])))
            {
                builder.Append('@');
                index = index + 1;
                continue;
            }

            builder.Append("{{ ");
            builder.Append(text.Substring(index + 1, expressionEnd - index - 1));
            builder.Append(" }}");
            index = expressionEnd;
        }

        return builder.ToString();
    }

    internal static int ConvertRazorConditionStart(StringBuilder builder, string text, int start, List<string> blocks)
    {
        int open = text.IndexOf('(', start + 3);
        int close = open < 0 ? -1 : FindMatching(text, open, '(', ')');
        if (close < 0)
        {
            builder.Append('@');
            return start + 1;
        }

        int brace = SkipWhitespace(text, close + 1);
        if (brace >= text.Length || text[brace] != '{')
        {
            builder.Append('@');
            return start + 1;
        }

        builder.Append("{% if ");
        builder.Append(text.Substring(open + 1, close - open - 1).Trim());
        builder.Append(" %}");
        blocks.Add("if");
        return brace + 1;
    }

    internal static int ConvertRazorForeachStart(StringBuilder builder, string text, int start, List<string> blocks)
    {
        int open = text.IndexOf('(', start + 8);
        int close = open < 0 ? -1 : FindMatching(text, open, '(', ')');
        if (close < 0)
        {
            builder.Append('@');
            return start + 1;
        }

        int brace = SkipWhitespace(text, close + 1);
        if (brace >= text.Length || text[brace] != '{')
        {
            builder.Append('@');
            return start + 1;
        }

        string clause = text.Substring(open + 1, close - open - 1).Trim();
        if (ScribanTemplateConverter.StartsWithWord(clause, "var"))
        {
            clause = clause.Substring(3).Trim();
        }

        builder.Append("{% for ");
        builder.Append(clause);
        builder.Append(" %}");
        blocks.Add("for");
        return brace + 1;
    }

    internal static int ConvertRazorBlockEnd(StringBuilder builder, string text, int start, List<string> blocks)
    {
        int next = SkipWhitespace(text, start + 1);
        if (StartsAt(text, next, "else if"))
        {
            int open = text.IndexOf('(', next + 7);
            int close = open < 0 ? -1 : FindMatching(text, open, '(', ')');
            int brace = close < 0 ? -1 : SkipWhitespace(text, close + 1);
            if (brace >= 0 && brace < text.Length && text[brace] == '{')
            {
                builder.Append("{% elseif ");
                builder.Append(text.Substring(open + 1, close - open - 1).Trim());
                builder.Append(" %}");
                return brace + 1;
            }
        }

        if (StartsAt(text, next, "else"))
        {
            int brace = SkipWhitespace(text, next + 4);
            if (brace < text.Length && text[brace] == '{')
            {
                builder.Append("{% else %}");
                return brace + 1;
            }
        }

        int blockIndex = blocks.Count - 1;
        string name = blocks[blockIndex];
        blocks.RemoveAt(blockIndex);
        builder.Append("{% end");
        builder.Append(name);
        builder.Append(" %}");
        return start + 1;
    }

    internal static void AppendRazorAssignments(StringBuilder builder, string code)
    {
        string[] statements = code.Split(';');
        int index = 0;
        while (index < statements.Length)
        {
            string statement = statements[index].Trim();
            if (ScribanTemplateConverter.StartsWithWord(statement, "var"))
            {
                statement = statement.Substring(3).Trim();
            }

            int assignment = ScribanTemplateConverter.FindAssignment(statement);
            if (assignment > 0)
            {
                builder.Append("{% set ");
                builder.Append(statement);
                builder.Append(" %}");
            }

            index = index + 1;
        }
    }

    internal static int FindImplicitExpressionEnd(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            char value = text[index];
            if (char.IsLetterOrDigit(value) || value == '_' || value == '.')
            {
                index = index + 1;
                continue;
            }

            if (value == '(')
            {
                int close = FindMatching(text, index, '(', ')');
                return close < 0 ? index : close + 1;
            }

            break;
        }

        return index;
    }

    internal static int FindMatching(string text, int openIndex, char open, char close)
    {
        int depth = 0;
        char quote = '\0';
        int index = openIndex;
        while (index < text.Length)
        {
            quote = ScribanTemplateConverter.UpdateQuote(text, index, quote);
            if (quote == '\0')
            {
                if (text[index] == open)
                {
                    depth = depth + 1;
                }
                else if (text[index] == close)
                {
                    depth = depth - 1;
                    if (depth == 0)
                    {
                        return index;
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool StartsAt(string text, int index, string value)
    {
        return index >= 0 && index + value.Length <= text.Length && text.Substring(index, value.Length) == value;
    }
}
