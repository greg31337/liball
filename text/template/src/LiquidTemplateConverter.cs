using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Translates supported Liquid tags and filters into native syntax.
/// </summary>
internal static class LiquidTemplateConverter
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
        int index = 0;
        while (index < text.Length)
        {
            int outputStart = text.IndexOf("{{", index, StringComparison.Ordinal);
            int statementStart = text.IndexOf("{%", index, StringComparison.Ordinal);
            int start = ChooseNextTagStart(outputStart, statementStart);
            if (start < 0)
            {
                builder.Append(text.Substring(index));
                break;
            }

            builder.Append(text.Substring(index, start - index));
            if (start == outputStart)
            {
                index = ConvertOutputTag(builder, text, start);
            }
            else
            {
                index = ConvertStatementTag(builder, text, start);
            }
        }

        return builder.ToString();
    }

    internal static int ConvertOutputTag(StringBuilder builder, string text, int start)
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
        if (!IsValidLiquidValueExpression(expression))
        {
            AppendStatement(builder, "liquidInvalid", leftTrim, rightTrim);
            return end + 2;
        }

        AppendOutput(builder, NormalizeLiquidExpression(expression), leftTrim, rightTrim);
        return end + 2;
    }

    internal static int ConvertStatementTag(StringBuilder builder, string text, int start)
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
        if (statement == "comment")
        {
            int commentEnd = FindBlockEnd(text, end + 2, "endcomment");
            if (commentEnd >= 0)
            {
                return commentEnd;
            }

            AppendStatement(builder, "if false", leftTrim, rightTrim);
            return end + 2;
        }

        if (statement == "doc")
        {
            int docEnd = FindBlockEnd(text, end + 2, "enddoc");
            if (docEnd >= 0)
            {
                return docEnd;
            }

            AppendStatement(builder, "if false", leftTrim, rightTrim);
            return end + 2;
        }

        if (statement == "raw")
        {
            int rawEnd = FindBlockEnd(text, end + 2, "endraw");
            if (rawEnd >= 0)
            {
                builder.Append(text.Substring(start, rawEnd - start));
                return rawEnd;
            }

            AppendStatement(builder, "if false", leftTrim, rightTrim);
            return end + 2;
        }
        else if (StartsWithText(statement, "raw ") || StartsWithText(statement, "comment ") || StartsWithText(statement, "doc "))
        {
            AppendStatement(builder, "liquidInvalid", leftTrim, rightTrim);
        }

        else if (StartsWithText(statement, "liquid"))
        {
            AppendLiquidTagLines(builder, statement.Substring(6));
        }
        else if (StartsWithText(statement, "echo "))
        {
            string expression = statement.Substring(5).Trim();
            if (IsValidLiquidValueExpression(expression))
            {
                AppendOutput(builder, NormalizeLiquidExpression(expression), leftTrim, rightTrim);
            }
            else
            {
                AppendStatement(builder, "liquidInvalid", leftTrim, rightTrim);
            }
        }
        else
        {
            AppendStatement(builder, NormalizeLiquidStatement(statement), leftTrim, rightTrim);
        }

        return end + 2;
    }

    internal static int SkipCommentBlock(string text, int start)
    {
        int end = FindBlockEnd(text, start, "endcomment");
        if (end >= 0)
        {
            return end;
        }

        return start;
    }

    internal static int FindBlockEnd(string text, int start, string name)
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
            if (statement == name)
            {
                return tagEnd + 2;
            }

            index = tagEnd + 2;
        }

        return -1;
    }

    internal static void AppendLiquidTagLines(StringBuilder builder, string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split('\n');
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index].Trim();
            if (line != "")
            {
                if (StartsWithText(line, "echo "))
                {
                    string expression = line.Substring(5).Trim();
                    if (IsValidLiquidValueExpression(expression))
                    {
                        AppendOutput(builder, NormalizeLiquidExpression(expression), false, false);
                    }
                    else
                    {
                        AppendStatement(builder, "liquidInvalid", false, false);
                    }
                }
                else
                {
                    AppendStatement(builder, NormalizeLiquidStatement(line), false, false);
                }
            }

            index = index + 1;
        }
    }

    internal static string NormalizeLiquidStatement(string statement)
    {
        if (StartsWithText(statement, "elsif "))
        {
            return "elseif " + NormalizeLiquidExpression(statement.Substring(6).Trim());
        }

        if (StartsWithText(statement, "assign "))
        {
            return NormalizeAssignmentStatement(statement);
        }

        if (StartsWithText(statement, "capture "))
        {
            string name = statement.Substring(8).Trim();
            if (!IsLiquidArgumentName(name))
            {
                return "liquidInvalid";
            }

            return "capture " + name;
        }

        if (StartsWithText(statement, "increment ") || StartsWithText(statement, "decrement "))
        {
            int spaceIndex = statement.IndexOf(' ');
            string name = statement.Substring(spaceIndex + 1).Trim();
            if (!IsLiquidArgumentName(name))
            {
                return "liquidInvalid";
            }

            return statement.Substring(0, spaceIndex) + " " + name;
        }

        if (StartsWithText(statement, "if "))
        {
            return "if " + NormalizeLiquidExpression(statement.Substring(3).Trim());
        }

        if (StartsWithText(statement, "unless "))
        {
            return "unless " + NormalizeLiquidExpression(statement.Substring(7).Trim());
        }

        if (StartsWithText(statement, "case "))
        {
            return "case " + NormalizeLiquidExpression(statement.Substring(5).Trim());
        }

        if (StartsWithText(statement, "when "))
        {
            return "when " + NormalizeWhenArguments(statement.Substring(5).Trim());
        }

        if (StartsWithText(statement, "for "))
        {
            return NormalizeForStatement(statement);
        }

        if (StartsWithText(statement, "tablerow "))
        {
            return "liquidTableRow " + NormalizeLoopAttributes(statement.Substring(9).Trim());
        }

        if (statement == "endtablerow")
        {
            return "endliquidTableRow";
        }

        if (StartsWithText(statement, "cycle "))
        {
            return NormalizeCycleStatement(statement.Substring(6).Trim());
        }

        if (StartsWithText(statement, "#"))
        {
            return "liquidComment";
        }

        if (StartsWithText(statement, "render ") || StartsWithText(statement, "include "))
        {
            return NormalizeTemplateStatement(statement);
        }

        return statement;
    }

    internal static string NormalizeAssignmentStatement(string statement)
    {
        int equalsIndex = statement.IndexOf("=", StringComparison.Ordinal);
        if (equalsIndex < 0)
        {
            return "liquidInvalid";
        }

        string variableName = statement.Substring(7, equalsIndex - 7).Trim();
        string right = statement.Substring(equalsIndex + 1).Trim();
        if (!IsLiquidArgumentName(variableName) || !IsValidLiquidValueExpression(right))
        {
            return "liquidInvalid";
        }

        return "assign " + variableName + " = " + NormalizeLiquidExpression(right);
    }

    internal static bool IsValidLiquidValueExpression(string expression)
    {
        string trimmed = expression.Trim();
        if (trimmed == "")
        {
            return false;
        }

        List<string> filters = SplitTopLevel(trimmed, '|');
        if (!IsValidLiquidValue(filters[0].Trim()))
        {
            return false;
        }

        int index = 1;
        while (index < filters.Count)
        {
            string filter = filters[index].Trim();
            int colonIndex = FindTopLevelCharacter(filter, ':');
            string filterName = filter;
            if (colonIndex >= 0)
            {
                filterName = filter.Substring(0, colonIndex).Trim();
            }

            if (!IsLiquidArgumentName(filterName))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidLiquidValue(string value)
    {
        int depth = 0;
        int index = 0;
        char quote = '\0';
        while (index < value.Length)
        {
            char current = value[index];
            if (quote != '\0')
            {
                if (current == '\\' && index + 1 < value.Length)
                {
                    index = index + 2;
                    continue;
                }

                if (current == quote)
                {
                    quote = '\0';
                }
            }
            else if (current == '\'' || current == '"')
            {
                quote = current;
            }
            else if (current == '(' || current == '[')
            {
                depth = depth + 1;
            }
            else if (current == ')' || current == ']')
            {
                depth = depth - 1;
                if (depth < 0)
                {
                    return false;
                }
            }
            else if (char.IsWhiteSpace(current) && depth == 0 && !HasLiquidOperator(value))
            {
                return false;
            }
            else if (current == ':' || current == ',' || current == '?' || current == '{' || current == '}')
            {
                return false;
            }

            index = index + 1;
        }

        if (quote != '\0' || depth != 0)
        {
            return false;
        }

        char last = value[value.Length - 1];
        return last != '.' && last != '-' && last != '=' && last != '(' && last != '[';
    }

    internal static bool HasLiquidOperator(string value)
    {
        return value.IndexOf(" and ", StringComparison.Ordinal) >= 0
            || value.IndexOf(" or ", StringComparison.Ordinal) >= 0
            || value.IndexOf(" contains ", StringComparison.Ordinal) >= 0
            || value.IndexOf("==", StringComparison.Ordinal) >= 0
            || value.IndexOf("!=", StringComparison.Ordinal) >= 0
            || value.IndexOf(">", StringComparison.Ordinal) >= 0
            || value.IndexOf("<", StringComparison.Ordinal) >= 0;
    }

    internal static string NormalizeForStatement(string statement)
    {
        return "for " + NormalizeLoopAttributes(statement.Substring(4).Trim());
    }

    internal static string NormalizeTemplateStatement(string statement)
    {
        int firstSpace = statement.IndexOf(" ", StringComparison.Ordinal);
        string name = statement.Substring(0, firstSpace);
        string arguments = statement.Substring(firstSpace + 1).Trim();
        int withIndex = FindTopLevelText(arguments, " with ");
        int forIndex = FindTopLevelText(arguments, " for ");
        int modeIndex = withIndex;
        string mode = "with";
        if (modeIndex < 0 || (forIndex >= 0 && forIndex < modeIndex))
        {
            modeIndex = forIndex;
            mode = "for";
        }

        if (modeIndex < 0)
        {
            return name + " " + NormalizeTemplateArguments(arguments);
        }

        string templateName = arguments.Substring(0, modeIndex).Trim().TrimEnd(',');
        string remaining = arguments.Substring(modeIndex + mode.Length + 2).Trim();
        string model = remaining;
        string alias = "";
        string parameters = "";
        int commaIndex = FindTopLevelCharacter(remaining, ',');
        if (commaIndex >= 0)
        {
            model = remaining.Substring(0, commaIndex).Trim();
            parameters = remaining.Substring(commaIndex + 1).Trim();
        }

        int aliasIndex = FindTopLevelText(model, " as ");
        if (aliasIndex >= 0)
        {
            alias = model.Substring(aliasIndex + 4).Trim();
            model = model.Substring(0, aliasIndex).Trim();
        }

        if (alias == "")
        {
            alias = GetDefaultTemplateAlias(templateName);
        }

        string resultName = name;
        if (mode == "for")
        {
            if (name == "render")
            {
                resultName = "liquidRenderFor";
            }
            else
            {
                resultName = "liquidIncludeFor";
            }
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(resultName);
        builder.Append(" ");
        builder.Append(templateName);
        builder.Append(" ");
        builder.Append(NormalizeLiquidExpression(model));
        if (parameters != "")
        {
            builder.Append(", ");
            builder.Append(NormalizeTemplateParameters(parameters));
        }

        if (alias != "")
        {
            builder.Append(" as |");
            builder.Append(alias);
            builder.Append("|");
        }

        return builder.ToString();
    }

    internal static string NormalizeTemplateArguments(string arguments)
    {
        List<string> segments = SplitTopLevel(arguments, ',');
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segments.Count)
        {
            string segment = segments[index].Trim();
            if (index == 0)
            {
                int parameterIndex = FindTemplateParameterStart(segment);
                if (parameterIndex >= 0)
                {
                    builder.Append(segment.Substring(0, parameterIndex).Trim());
                    builder.Append(", ");
                    builder.Append(NormalizeTemplateParameter(segment.Substring(parameterIndex).Trim()));
                }
                else
                {
                    builder.Append(NormalizeLiquidExpression(segment));
                }
            }
            else
            {
                builder.Append(", ");
                builder.Append(NormalizeTemplateParameter(segment));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeTemplateParameters(string parameters)
    {
        List<string> segments = SplitTopLevel(parameters, ',');
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segments.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(NormalizeTemplateParameter(segments[index].Trim()));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeTemplateParameter(string parameter)
    {
        int colonIndex = FindTopLevelCharacter(parameter, ':');
        if (colonIndex > 0 && IsLiquidArgumentName(parameter.Substring(0, colonIndex).Trim()))
        {
            return parameter.Substring(0, colonIndex).Trim() + "=" + NormalizeLiquidExpression(parameter.Substring(colonIndex + 1).Trim());
        }

        return NormalizeLiquidExpression(parameter);
    }

    internal static int FindTemplateParameterStart(string text)
    {
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
            else if (char.IsWhiteSpace(current))
            {
                int candidate = index;
                while (candidate < text.Length && char.IsWhiteSpace(text[candidate]))
                {
                    candidate = candidate + 1;
                }

                int colon = text.IndexOf(':', candidate);
                if (colon > candidate && IsLiquidArgumentName(text.Substring(candidate, colon - candidate).Trim()))
                {
                    return candidate;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string NormalizeLoopAttributes(string text)
    {
        string result = NormalizeLoopRange(text);
        result = result.Replace("offset:continue", "offset \"__liquid_continue__\"");
        result = result.Replace(" limit:", " limit ");
        result = result.Replace(" offset:", " offset ");
        result = result.Replace(" cols:", " cols ");
        return result;
    }

    internal static string NormalizeLoopRange(string text)
    {
        int inIndex = FindTopLevelText(text, " in ");
        if (inIndex < 0)
        {
            return text;
        }

        int rangeStart = inIndex + 4;
        if (rangeStart >= text.Length || text[rangeStart] != '(')
        {
            return text;
        }

        int rangeEnd = text.IndexOf(')', rangeStart + 1);
        if (rangeEnd < 0 || text.Substring(rangeStart + 1, rangeEnd - rangeStart - 1).IndexOf("..", StringComparison.Ordinal) < 0)
        {
            return text;
        }

        return text.Substring(0, rangeStart) + text.Substring(rangeStart + 1, rangeEnd - rangeStart - 1) + text.Substring(rangeEnd + 1);
    }

    internal static string NormalizeCycleStatement(string arguments)
    {
        int colonIndex = FindTopLevelCharacter(arguments, ':');
        if (colonIndex >= 0)
        {
            string group = arguments.Substring(0, colonIndex).Trim();
            string values = arguments.Substring(colonIndex + 1).Trim();
            return "liquidCycleNamed " + NormalizeLiquidExpression(group) + ", " + NormalizeCycleValues(values);
        }

        return "liquidCycle " + NormalizeCycleValues(arguments);
    }

    internal static string NormalizeCycleValues(string arguments)
    {
        List<string> values = SplitTopLevel(arguments, ',');
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(NormalizeLiquidExpression(values[index].Trim()));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string GetDefaultTemplateAlias(string templateName)
    {
        string alias = templateName.Trim();
        if (alias.Length >= 2 && ((alias[0] == '\'' && alias[alias.Length - 1] == '\'') || (alias[0] == '"' && alias[alias.Length - 1] == '"')))
        {
            alias = alias.Substring(1, alias.Length - 2);
        }
        else
        {
            return "";
        }

        int slashIndex = alias.LastIndexOf('/');
        if (slashIndex >= 0)
        {
            alias = alias.Substring(slashIndex + 1);
        }

        int dotIndex = alias.LastIndexOf('.');
        if (dotIndex > 0)
        {
            alias = alias.Substring(0, dotIndex);
        }

        return alias;
    }

    internal static int FindTopLevelText(string text, string value)
    {
        int depth = 0;
        int index = 0;
        char quote = '\0';
        while (index <= text.Length - value.Length)
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
            else if (depth == 0 && text.Substring(index, value.Length) == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string NormalizeLiquidExpression(string expression)
    {
        List<string> segments = SplitTopLevel(expression, '|');
        if (segments.Count <= 1)
        {
            return NormalizeLiquidKeywords(expression);
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(NormalizeLiquidKeywords(segments[0].Trim()));
        int index = 1;
        while (index < segments.Count)
        {
            string segment = segments[index].Trim();
            int colonIndex = FindTopLevelCharacter(segment, ':');
            builder.Append(" | ");
            if (colonIndex >= 0)
            {
                builder.Append(segment.Substring(0, colonIndex).Trim());
                builder.Append("(");
                builder.Append(NormalizeLiquidFilterArguments(segment.Substring(colonIndex + 1).Trim()));
                builder.Append(")");
            }
            else
            {
                builder.Append(segment);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeLiquidFilterArguments(string arguments)
    {
        List<string> segments = SplitTopLevel(arguments, ',');
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < segments.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            string segment = segments[index].Trim();
            int colonIndex = FindTopLevelCharacter(segment, ':');
            if (colonIndex > 0 && IsLiquidArgumentName(segment.Substring(0, colonIndex).Trim()))
            {
                builder.Append(segment.Substring(0, colonIndex).Trim());
                builder.Append("=");
                builder.Append(NormalizeLiquidKeywords(segment.Substring(colonIndex + 1).Trim()));
            }
            else
            {
                builder.Append(NormalizeLiquidKeywords(segment));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string NormalizeWhenArguments(string arguments)
    {
        StringBuilder builder = new StringBuilder();
        string remaining = arguments;
        bool first = true;
        while (true)
        {
            int orIndex = FindTopLevelText(remaining, " or ");
            string value = remaining;
            if (orIndex >= 0)
            {
                value = remaining.Substring(0, orIndex);
            }

            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(NormalizeLiquidExpression(value.Trim()));
            first = false;
            if (orIndex < 0)
            {
                break;
            }

            remaining = remaining.Substring(orIndex + 4);
        }

        return builder.ToString();
    }

    internal static string NormalizeLiquidKeywords(string expression)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        char quote = '\0';
        while (index < expression.Length)
        {
            char current = expression[index];
            if (quote != '\0')
            {
                builder.Append(current);
                if (current == quote)
                {
                    quote = '\0';
                }

                index = index + 1;
            }
            else if (current == '\'' || current == '"')
            {
                quote = current;
                builder.Append(current);
                index = index + 1;
            }
            else if (char.IsLetter(current) || current == '_')
            {
                int start = index;
                index = index + 1;
                while (index < expression.Length && (char.IsLetterOrDigit(expression[index]) || expression[index] == '_'))
                {
                    index = index + 1;
                }

                string word = expression.Substring(start, index - start);
                if (word == "nil")
                {
                    builder.Append("null");
                }
                else
                {
                    builder.Append(word);
                }
            }
            else
            {
                builder.Append(current);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static bool IsLiquidArgumentName(string text)
    {
        if (text.Length == 0 || (!char.IsLetter(text[0]) && text[0] != '_'))
        {
            return false;
        }

        int index = 1;
        while (index < text.Length)
        {
            if (!char.IsLetterOrDigit(text[index]) && text[index] != '_')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static List<string> SplitTopLevel(string text, char separator)
    {
        List<string> segments = new List<string>();
        int depth = 0;
        int start = 0;
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
            else if (current == separator && depth == 0)
            {
                segments.Add(text.Substring(start, index - start));
                start = index + 1;
            }

            index = index + 1;
        }

        segments.Add(text.Substring(start));
        return segments;
    }

    internal static int FindTopLevelCharacter(string text, char value)
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
            else if (current == value && depth == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int ChooseNextTagStart(int outputStart, int statementStart)
    {
        if (outputStart < 0)
        {
            return statementStart;
        }

        if (statementStart < 0)
        {
            return outputStart;
        }

        if (outputStart < statementStart)
        {
            return outputStart;
        }

        return statementStart;
    }

    internal static void AppendOutput(StringBuilder builder, string expression, bool leftTrim, bool rightTrim)
    {
        if (leftTrim)
        {
            builder.Append("{{- ");
        }
        else
        {
            builder.Append("{{ ");
        }

        builder.Append(expression);
        if (rightTrim)
        {
            builder.Append(" -}}");
        }
        else
        {
            builder.Append(" }}");
        }
    }

    internal static void AppendStatement(StringBuilder builder, string statement, bool leftTrim, bool rightTrim)
    {
        if (leftTrim)
        {
            builder.Append("{%- ");
        }
        else
        {
            builder.Append("{% ");
        }

        builder.Append(statement);
        if (rightTrim)
        {
            builder.Append(" -%}");
        }
        else
        {
            builder.Append(" %}");
        }
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
