using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Translates supported Jinja and Django constructs into native syntax.
/// </summary>
internal static class JinjaTemplateConverter
{
    /// <summary>
    /// Converts Jinja constructs to equivalent native syntax.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string ConvertToNative(string text)
    {
        return ConvertDialectToNative(text, false);
    }

    /// <summary>
    /// Converts Django template constructs to equivalent native syntax.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string ConvertDjangoToNative(string text)
    {
        return ConvertDialectToNative(text, true);
    }

    internal static string ConvertDialectToNative(string text, bool django)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        Dictionary<string, string> importAliases = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> filterDepths = new List<string>();
        List<string> setDepths = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            int outputStart = text.IndexOf("{{", index, StringComparison.Ordinal);
            int statementStart = text.IndexOf("{%", index, StringComparison.Ordinal);
            int next = SelectNextTag(outputStart, statementStart);
            if (next < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            builder.Append(text.Substring(index, next - index));
            if (next == outputStart)
            {
                index = ConvertOutputTag(builder, text, next, django, importAliases);
            }
            else
            {
                index = ConvertStatementTagForDialect(builder, text, next, django, importAliases, filterDepths, setDepths);
            }
        }

        return builder.ToString();
    }

    internal static int SelectNextTag(int outputStart, int statementStart)
    {
        if (outputStart < 0)
        {
            return statementStart;
        }

        if (statementStart < 0 || outputStart < statementStart)
        {
            return outputStart;
        }

        return statementStart;
    }

    internal static int ConvertOutputTag(StringBuilder builder, string text, int start, bool django, Dictionary<string, string> importAliases)
    {
        int end = text.IndexOf("}}", start + 2, StringComparison.Ordinal);
        if (end < 0)
        {
            builder.Append(text.Substring(start));
            return text.Length;
        }

        string content = text.Substring(start + 2, end - start - 2);
        bool leftTrim = HasLeftTrim(content);
        bool rightTrim = HasRightTrim(content);
        string expression = TrimTagContent(content);
        if (django)
        {
            expression = NormalizeDjangoFilters(expression);
        }

        expression = NormalizeExpression(expression, django, importAliases);
        builder.Append(leftTrim ? "{{- " : "{{ ");
        builder.Append(expression);
        builder.Append(rightTrim ? " -}}" : " }}");
        return end + 2;
    }

    internal static int ConvertStatementTag(StringBuilder builder, string text, int start)
    {
        Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> depths = new List<string>();
        List<string> setDepths = new List<string>();
        return ConvertStatementTagForDialect(builder, text, start, false, aliases, depths, setDepths);
    }

    internal static int ConvertStatementTagForDialect(StringBuilder builder, string text, int start, bool django, Dictionary<string, string> importAliases, List<string> filterDepths, List<string> setDepths)
    {
        int end = text.IndexOf("%}", start + 2, StringComparison.Ordinal);
        if (end < 0)
        {
            builder.Append(text.Substring(start));
            return text.Length;
        }

        string content = text.Substring(start + 2, end - start - 2);
        bool leftTrim = HasLeftTrim(content);
        bool rightTrim = HasRightTrim(content);
        string statement = TrimTagContent(content);
        if ((!django && statement == "raw") || (django && StartsWithText(statement, "verbatim")))
        {
            string endName = django ? "endverbatim" : "endraw";
            int rawEndStart = FindBlockEndStart(text, end + 2, endName);
            if (rawEndStart >= 0)
            {
                int rawEnd = text.IndexOf("%}", rawEndStart + 2, StringComparison.Ordinal);
                AppendStatement(builder, "raw", leftTrim, false);
                builder.Append(text.Substring(end + 2, rawEndStart - end - 2));
                AppendStatement(builder, "endraw", false, rightTrim);
                return rawEnd + 2;
            }
        }

        if (django && StartsWithText(statement, "comment"))
        {
            int commentEndStart = FindBlockEndStart(text, end + 2, "endcomment");
            if (commentEndStart >= 0)
            {
                int commentEnd = text.IndexOf("%}", commentEndStart + 2, StringComparison.Ordinal);
                return commentEnd + 2;
            }
        }

        if (StartsWithText(statement, "filter "))
        {
            string filters = statement.Substring(7).Trim();
            if (django)
            {
                filters = NormalizeDjangoFilters(filters);
            }

            string captureName = "__profile_filter_" + start.ToString();
            AppendStatement(builder, "capture " + captureName, leftTrim, rightTrim);
            filterDepths.Add(captureName + "\n" + filters);
            return end + 2;
        }

        if (statement == "endfilter" && filterDepths.Count > 0)
        {
            int depthIndex = filterDepths.Count - 1;
            string filterState = filterDepths[depthIndex];
            filterDepths.RemoveAt(depthIndex);
            int separatorIndex = filterState.IndexOf('\n');
            string captureName = filterState.Substring(0, separatorIndex);
            string filters = filterState.Substring(separatorIndex + 1);
            AppendStatement(builder, "endcapture", leftTrim, false);
            builder.Append("{{ ");
            builder.Append(captureName);
            builder.Append('|');
            builder.Append(filters);
            builder.Append(rightTrim ? " -}}" : " }}");

            return end + 2;
        }

        if (!django && StartsWithText(statement, "set ") && FindTopLevelCharacter(statement, '=') < 0)
        {
            string assignment = statement.Substring(4).Trim();
            int pipeIndex = FindTopLevelCharacter(assignment, '|');
            string name = pipeIndex < 0 ? assignment : assignment.Substring(0, pipeIndex).Trim();
            string filters = pipeIndex < 0 ? "" : assignment.Substring(pipeIndex + 1).Trim();
            AppendStatement(builder, "capture " + name, leftTrim, rightTrim);
            setDepths.Add(name + "\n" + filters);
            return end + 2;
        }

        if (!django && statement == "endset" && setDepths.Count > 0)
        {
            int depthIndex = setDepths.Count - 1;
            string setState = setDepths[depthIndex];
            setDepths.RemoveAt(depthIndex);
            int separatorIndex = setState.IndexOf('\n');
            string name = setState.Substring(0, separatorIndex);
            string filters = setState.Substring(separatorIndex + 1);
            AppendStatement(builder, "endcapture", leftTrim, filters == "" && rightTrim);
            if (filters != "")
            {
                AppendStatement(builder, "set " + name + " = " + name + "|" + filters, false, rightTrim);
            }

            return end + 2;
        }

        if (django && StartsWithText(statement, "with "))
        {
            AppendDjangoWith(builder, statement.Substring(5).Trim(), leftTrim, rightTrim, importAliases);
            return end + 2;
        }

        statement = NormalizeStatement(statement, django, importAliases);
        AppendStatement(builder, statement, leftTrim, rightTrim);
        return end + 2;
    }

    internal static void AppendDjangoWith(StringBuilder builder, string arguments, bool leftTrim, bool rightTrim, Dictionary<string, string> importAliases)
    {
        AppendStatement(builder, "with true", leftTrim, false);
        int asIndex = FindWordOutsideStrings(arguments, "as");
        if (asIndex >= 0)
        {
            string value = arguments.Substring(0, asIndex).Trim();
            string name = arguments.Substring(asIndex + 2).Trim();
            AppendStatement(builder, "set " + name + " = " + NormalizeExpression(value, true, importAliases), false, rightTrim);
            return;
        }

        List<string> assignments = SplitTopLevelWhitespace(arguments);
        int index = 0;
        while (index < assignments.Count)
        {
            string assignment = assignments[index];
            int equalsIndex = FindTopLevelCharacter(assignment, '=');
            if (equalsIndex > 0)
            {
                string name = assignment.Substring(0, equalsIndex).Trim();
                string value = assignment.Substring(equalsIndex + 1).Trim();
                AppendStatement(builder, "set " + name + " = " + NormalizeExpression(value, true, importAliases), false, index + 1 == assignments.Count && rightTrim);
            }

            index = index + 1;
        }
    }

    internal static string NormalizeStatement(string statement, bool django, Dictionary<string, string> importAliases)
    {
        if (StartsWithText(statement, "elif "))
        {
            statement = "elseif " + statement.Substring(5).Trim();
        }

        if (!django && StartsWithText(statement, "set ") && FindTopLevelCharacter(statement, '=') < 0)
        {
            return "capture " + statement.Substring(4).Trim();
        }

        if (!django && statement == "endset")
        {
            return "endcapture";
        }

        if (StartsWithText(statement, "autoescape "))
        {
            return "jinjaAutoescape " + NormalizeAutoescapeArgument(statement.Substring(11).Trim());
        }

        if (statement == "endautoescape")
        {
            return "endjinjaAutoescape";
        }

        if (StartsWithText(statement, "endblock "))
        {
            return "endblock";
        }

        if (StartsWithText(statement, "macro "))
        {
            statement = NormalizeMacroDeclaration(statement);
        }

        if (StartsWithText(statement, "include "))
        {
            statement = NormalizeIncludeStatement(statement, django);
        }

        if (!django && StartsWithText(statement, "import "))
        {
            statement = NormalizeImportStatement(statement, importAliases);
        }
        else if (!django && StartsWithText(statement, "from "))
        {
            statement = NormalizeFromImportStatement(statement, importAliases);
        }

        if (django)
        {
            statement = NormalizeDjangoFilters(statement);
        }

        return NormalizeExpression(statement, django, importAliases);
    }

    internal static string NormalizeMacroDeclaration(string statement)
    {
        string declaration = statement.Substring(6).Trim();
        int openIndex = declaration.IndexOf('(');
        if (openIndex <= 0 || declaration[declaration.Length - 1] != ')')
        {
            return statement;
        }

        string name = declaration.Substring(0, openIndex).Trim();
        string parameters = declaration.Substring(openIndex + 1, declaration.Length - openIndex - 2).Trim();
        return parameters == "" ? "macro " + name : "macro " + name + " " + parameters;
    }

    internal static string NormalizeIncludeStatement(string statement, bool django)
    {
        string arguments = statement.Substring(8).Trim();
        if (!django)
        {
            arguments = RemoveOutsideStringText(arguments, "ignore missing");
            if (FindOutsideStringText(arguments, "without context") >= 0)
            {
                arguments = RemoveOutsideStringText(arguments, "without context");
                return "render " + arguments.Trim() + " {}";
            }

            arguments = RemoveOutsideStringText(arguments, "with context");
            return "include " + arguments.Trim();
        }

        bool isolated = FindWordOutsideStrings(arguments, "only") >= 0;
        arguments = RemoveOutsideStringText(arguments, "only").Trim();
        int withIndex = FindWordOutsideStrings(arguments, "with");
        string templateName = arguments;
        string namedArguments = "";
        if (withIndex >= 0)
        {
            templateName = arguments.Substring(0, withIndex).Trim();
            namedArguments = arguments.Substring(withIndex + 4).Trim();
        }

        string result = isolated ? "render " + templateName + " {}" : "include " + templateName;
        return namedArguments == "" ? result : result + " " + namedArguments;
    }

    internal static string NormalizeImportStatement(string statement, Dictionary<string, string> importAliases)
    {
        string arguments = statement.Substring(7).Trim();
        arguments = RemoveOutsideStringText(arguments, "without context").Trim();
        arguments = RemoveOutsideStringText(arguments, "with context").Trim();
        int asIndex = FindWordOutsideStrings(arguments, "as");
        if (asIndex < 0)
        {
            return statement;
        }

        string alias = arguments.Substring(asIndex + 2).Trim();
        importAliases[alias] = "";
        return "import " + arguments.Substring(0, asIndex).Trim();
    }

    internal static string NormalizeFromImportStatement(string statement, Dictionary<string, string> importAliases)
    {
        string arguments = statement.Substring(5).Trim();
        int importIndex = FindWordOutsideStrings(arguments, "import");
        if (importIndex < 0)
        {
            return statement;
        }

        string templateName = arguments.Substring(0, importIndex).Trim();
        List<string> names = SplitTopLevel(arguments.Substring(importIndex + 6).Trim(), ',');
        int index = 0;
        while (index < names.Count)
        {
            string item = names[index].Trim();
            int asIndex = FindWordOutsideStrings(item, "as");
            if (asIndex >= 0)
            {
                importAliases[item.Substring(asIndex + 2).Trim()] = item.Substring(0, asIndex).Trim();
            }

            index = index + 1;
        }

        return "import " + templateName;
    }

    internal static string NormalizeExpression(string text, bool django, Dictionary<string, string> importAliases)
    {
        string result = ReplaceWordOutsideStrings(text, "True", "true");
        result = ReplaceWordOutsideStrings(result, "False", "false");
        result = ReplaceWordOutsideStrings(result, "None", "null");
        if (!django)
        {
            result = ReplaceWordOutsideStrings(result, "none", "null");
            result = ReplaceCharacterOutsideStrings(result, '~', '+');
        }
        else
        {
            result = ReplaceOutsideStrings(result, "block.super", "super()");
        }

        foreach (KeyValuePair<string, string> pair in importAliases)
        {
            result = pair.Value == "" ? ReplaceOutsideStrings(result, pair.Key + ".", "") : ReplaceWordOutsideStrings(result, pair.Key, pair.Value);
        }

        return result;
    }

    internal static string NormalizeDjangoFilters(string text)
    {
        List<string> segments = SplitTopLevel(text, '|');
        if (segments.Count < 2)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(segments[0]);
        int index = 1;
        while (index < segments.Count)
        {
            string segment = segments[index].Trim();
            int colonIndex = FindTopLevelCharacter(segment, ':');
            builder.Append('|');
            if (colonIndex < 0)
            {
                builder.Append(segment);
            }
            else
            {
                builder.Append(segment.Substring(0, colonIndex).Trim());
                builder.Append('(');
                builder.Append(segment.Substring(colonIndex + 1).Trim());
                builder.Append(')');
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static List<string> SplitTopLevel(string text, char separator)
    {
        List<string> parts = new List<string>();
        int depth = 0;
        char quote = '\0';
        int start = 0;
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0')
            {
                if (value == '(' || value == '[' || value == '{')
                {
                    depth = depth + 1;
                }
                else if (value == ')' || value == ']' || value == '}')
                {
                    depth = depth - 1;
                }
                else if (value == separator && depth == 0)
                {
                    parts.Add(text.Substring(start, index - start));
                    start = index + 1;
                }
            }

            index = index + 1;
        }

        parts.Add(text.Substring(start));
        return parts;
    }

    internal static List<string> SplitTopLevelWhitespace(string text)
    {
        List<string> parts = new List<string>();
        int depth = 0;
        char quote = '\0';
        int start = 0;
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0')
            {
                if (value == '(' || value == '[' || value == '{')
                {
                    depth = depth + 1;
                }
                else if (value == ')' || value == ']' || value == '}')
                {
                    depth = depth - 1;
                }
                else if (char.IsWhiteSpace(value) && depth == 0)
                {
                    if (index > start)
                    {
                        parts.Add(text.Substring(start, index - start));
                    }

                    start = index + 1;
                }
            }

            index = index + 1;
        }

        if (start < text.Length)
        {
            parts.Add(text.Substring(start));
        }

        return parts;
    }

    internal static int FindTopLevelCharacter(string text, char target)
    {
        int depth = 0;
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0')
            {
                if (value == target && depth == 0)
                {
                    return index;
                }

                if (value == '(' || value == '[' || value == '{')
                {
                    depth = depth + 1;
                }
                else if (value == ')' || value == ']' || value == '}')
                {
                    depth = depth - 1;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static char UpdateQuote(string text, int index, char quote)
    {
        char value = text[index];
        if (quote == '\0')
        {
            return value == '\'' || value == '"' ? value : quote;
        }

        if (value == quote && (index == 0 || text[index - 1] != '\\'))
        {
            return '\0';
        }

        return quote;
    }

    internal static int FindWordOutsideStrings(string text, string word)
    {
        char quote = '\0';
        int index = 0;
        while (index + word.Length <= text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0' && text.Substring(index, word.Length) == word && IsWordBoundary(text, index - 1) && IsWordBoundary(text, index + word.Length))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsWordBoundary(string text, int index)
    {
        return index < 0 || index >= text.Length || (!char.IsLetterOrDigit(text[index]) && text[index] != '_');
    }

    internal static string RemoveOutsideStringText(string text, string value)
    {
        int index = FindOutsideStringText(text, value);
        return index < 0 ? text : text.Substring(0, index) + text.Substring(index + value.Length);
    }

    internal static int FindOutsideStringText(string text, string value)
    {
        char quote = '\0';
        int index = 0;
        while (index + value.Length <= text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0' && text.Substring(index, value.Length) == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string ReplaceWordOutsideStrings(string text, string oldValue, string newValue)
    {
        StringBuilder builder = new StringBuilder();
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0' && index + oldValue.Length <= text.Length && text.Substring(index, oldValue.Length) == oldValue && IsWordBoundary(text, index - 1) && IsWordBoundary(text, index + oldValue.Length))
            {
                builder.Append(newValue);
                index = index + oldValue.Length;
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static string ReplaceOutsideStrings(string text, string oldValue, string newValue)
    {
        StringBuilder builder = new StringBuilder();
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            if (quote == '\0' && index + oldValue.Length <= text.Length && text.Substring(index, oldValue.Length) == oldValue)
            {
                builder.Append(newValue);
                index = index + oldValue.Length;
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static string ReplaceCharacterOutsideStrings(string text, char oldValue, char newValue)
    {
        StringBuilder builder = new StringBuilder();
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            quote = UpdateQuote(text, index, quote);
            builder.Append(quote == '\0' && text[index] == oldValue ? newValue : text[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeJinjaStatement(string statement)
    {
        Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        return NormalizeStatement(statement, false, aliases);
    }

    internal static string NormalizeAutoescapeArgument(string text)
    {
        if (text == "false" || text == "off" || text == "0")
        {
            return "false";
        }

        if (text == "true" || text == "on" || text == "1")
        {
            return "true";
        }

        return text;
    }

    internal static int FindBlockEnd(string text, int start, string name)
    {
        int blockStart = FindBlockEndStart(text, start, name);
        if (blockStart < 0)
        {
            return -1;
        }

        int end = text.IndexOf("%}", blockStart + 2, StringComparison.Ordinal);
        return end < 0 ? -1 : end + 2;
    }

    internal static int FindBlockEndStart(string text, int start, string name)
    {
        int index = start;
        while (index < text.Length)
        {
            int tagStart = text.IndexOf("{%", index, StringComparison.Ordinal);
            if (tagStart < 0)
            {
                return -1;
            }

            int tagEnd = text.IndexOf("%}", tagStart + 2, StringComparison.Ordinal);
            if (tagEnd < 0)
            {
                return -1;
            }

            string statement = TrimTagContent(text.Substring(tagStart + 2, tagEnd - tagStart - 2));
            if (statement == name || StartsWithText(statement, name + " "))
            {
                return tagStart;
            }

            index = tagEnd + 2;
        }

        return -1;
    }

    internal static void AppendStatement(StringBuilder builder, string statement, bool leftTrim, bool rightTrim)
    {
        builder.Append(leftTrim ? "{%- " : "{% ");
        builder.Append(statement);
        builder.Append(rightTrim ? " -%}" : " %}");
    }

    internal static bool HasLeftTrim(string content)
    {
        string trimmed = content.TrimStart();
        return trimmed.Length > 0 && trimmed[0] == '-';
    }

    internal static bool HasRightTrim(string content)
    {
        string trimmed = content.TrimEnd();
        return trimmed.Length > 0 && trimmed[trimmed.Length - 1] == '-';
    }

    internal static string TrimTagContent(string content)
    {
        string trimmed = content.Trim();
        if (trimmed.Length > 0 && trimmed[0] == '-')
        {
            trimmed = trimmed.Substring(1).TrimStart();
        }

        if (trimmed.Length > 0 && trimmed[trimmed.Length - 1] == '-')
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
        }

        return trimmed;
    }

    internal static bool StartsWithText(string text, string value)
    {
        return text.Length >= value.Length && text.Substring(0, value.Length) == value;
    }
}
