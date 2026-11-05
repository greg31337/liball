using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class CssSerializationResult
{
    internal string Css = string.Empty;
    internal string Hash = string.Empty;
    internal string[] Rules = Array.Empty<string>();
    internal int InputRuleCount;
}

internal class CssSerializer
{
    internal List<string> InternedRules;

    internal CssSerializer()
    {
        InternedRules = new List<string>();
    }

    internal string SerializeDeclarations(UiCssDeclaration[]? declarations)
    {
        if (declarations == null)
        {
            throw new ArgumentNullException("declarations");
        }

        List<UiCssDeclaration> ordered = new List<UiCssDeclaration>();
        int index = 0;
        while (index < declarations.Length)
        {
            UiCssDeclaration? declaration = declarations[index];
            if (declaration == null)
            {
                throw new ArgumentException("CSS declaration collections cannot contain null.", "declarations");
            }

            UiCssDeclaration? existing = FindDeclaration(ordered, declaration.Property);
            if (existing != null)
            {
                if (!DeclarationsMatch(existing, declaration))
                {
                    throw new ArgumentException("Conflicting declarations cannot use the same property in one declaration block.", "declarations");
                }
            }
            else
            {
                InsertDeclaration(ordered, declaration);
            }

            index++;
        }

        StringBuilder result = new StringBuilder();
        index = 0;
        while (index < ordered.Count)
        {
            UiCssDeclaration declaration = ordered[index];
            string value = CssEscaper.EscapeDeclarationValue(declaration.Property, declaration.Value, declaration.IsTrusted);
            result.Append(declaration.Property);
            result.Append(':');
            result.Append(value);
            if (declaration.Important)
            {
                result.Append("!important");
            }

            result.Append(';');
            index++;
        }

        return result.ToString();
    }

    internal UiCssDeclaration? FindDeclaration(List<UiCssDeclaration> declarations, string property)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (string.Equals(declarations[index].Property, property, StringComparison.Ordinal))
            {
                return declarations[index];
            }

            index++;
        }

        return null;
    }

    internal bool DeclarationsMatch(UiCssDeclaration left, UiCssDeclaration right)
    {
        return string.Equals(left.Property, right.Property, StringComparison.Ordinal)
            && string.Equals(left.Value, right.Value, StringComparison.Ordinal)
            && left.Important == right.Important
            && left.IsTrusted == right.IsTrusted;
    }

    internal void InsertDeclaration(List<UiCssDeclaration> declarations, UiCssDeclaration declaration)
    {
        int index = declarations.Count;
        declarations.Add(declaration);
        while (index > 0 && string.CompareOrdinal(declarations[index - 1].Property, declaration.Property) > 0)
        {
            declarations[index] = declarations[index - 1];
            index--;
        }

        declarations[index] = declaration;
    }

    internal string SerializeRule(UiCssRule? rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        string selector = CssEscaper.EscapeSelector(rule.Selector);
        string declarations = SerializeDeclarations(rule.Declarations);
        string serialized = selector + "{" + declarations + "}";
        if (rule.Condition != null)
        {
            serialized = SerializeCondition(rule.Condition, serialized);
        }

        if (rule.Layer != null)
        {
            if (string.IsNullOrWhiteSpace(rule.Layer))
            {
                throw new ArgumentException("CSS layer names cannot be empty.", "rule");
            }

            serialized = "@layer " + EscapeLayerName(rule.Layer) + "{" + serialized + "}";
        }

        return InternRule(serialized);
    }

    internal string SerializeCondition(UiCssCondition condition, string nestedRule)
    {
        if (condition == null)
        {
            throw new ArgumentNullException("condition");
        }

        string expression = condition.Expression.Trim();
        if (expression.Length == 0 || !IsSafeConditionExpression(expression))
        {
            throw new ArgumentException("CSS condition expressions must be non-empty, balanced, and free of rule delimiters.", "condition");
        }

        switch (condition.Kind)
        {
            case "media":
                if (condition.ContainerName != null)
                {
                    throw new ArgumentException("Media conditions cannot name a container.", "condition");
                }

                return "@media " + expression + "{" + nestedRule + "}";
            case "supports":
                if (condition.ContainerName != null)
                {
                    throw new ArgumentException("Supports conditions cannot name a container.", "condition");
                }

                return "@supports " + expression + "{" + nestedRule + "}";
            case "container":
                string prefix = string.Empty;
                if (condition.ContainerName != null)
                {
                    if (string.IsNullOrWhiteSpace(condition.ContainerName))
                    {
                        throw new ArgumentException("Container condition names cannot be empty.", "condition");
                    }

                    prefix = CssEscaper.EscapeIdentifier(condition.ContainerName) + " ";
                }

                return "@container " + prefix + expression + "{" + nestedRule + "}";
            default:
                throw new ArgumentException("The CSS condition kind is not supported.", "condition");
        }
    }

    internal bool IsSafeConditionExpression(string expression)
    {
        int parenthesisDepth = 0;
        int squareDepth = 0;
        char quote = '\0';
        int index = 0;
        while (index < expression.Length)
        {
            char character = expression[index];
            if (character == '\0' || character < ' ' || character == '\u007f' || character == '<' || character == '{' || character == '}' || character == ';' || character == '@')
            {
                return false;
            }

            if (character == '/' && index + 1 < expression.Length && expression[index + 1] == '*')
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= expression.Length || !char.IsLowSurrogate(expression[index + 1]))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                return false;
            }

            if (quote != '\0')
            {
                if (character == '\\')
                {
                    if (!CssEscaper.HasSafeEscapeFollower(expression, index))
                    {
                        return false;
                    }

                    index++;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '"' || character == '\'')
            {
                quote = character;
            }
            else if (character == '(')
            {
                parenthesisDepth++;
            }
            else if (character == ')')
            {
                if (parenthesisDepth == 0)
                {
                    return false;
                }

                parenthesisDepth--;
            }
            else if (character == '[')
            {
                squareDepth++;
            }
            else if (character == ']')
            {
                if (squareDepth == 0)
                {
                    return false;
                }

                squareDepth--;
            }

            index++;
        }

        return quote == '\0' && parenthesisDepth == 0 && squareDepth == 0;
    }

    internal string EscapeLayerName(string layer)
    {
        string[] segments = layer.Split('.');
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index].Length == 0)
            {
                throw new ArgumentException("CSS layer name segments cannot be empty.", "layer");
            }

            if (index > 0)
            {
                result.Append('.');
            }

            result.Append(CssEscaper.EscapeIdentifier(segments[index]));
            index++;
        }

        return result.ToString();
    }

    internal string InternRule(string rule)
    {
        int index = 0;
        while (index < InternedRules.Count)
        {
            if (string.Equals(InternedRules[index], rule, StringComparison.Ordinal))
            {
                return InternedRules[index];
            }

            index++;
        }

        InternedRules.Add(rule);
        return rule;
    }

    internal int GetInternedRuleCount()
    {
        return InternedRules.Count;
    }

    internal CssSerializationResult SerializeStylesheet(UiCssRule[]? rules)
    {
        if (rules == null)
        {
            throw new ArgumentNullException("rules");
        }

        List<string> ordered = new List<string>();
        int index = 0;
        while (index < rules.Length)
        {
            UiCssRule? rule = rules[index];
            if (rule == null)
            {
                throw new ArgumentException("CSS rule collections cannot contain null.", "rules");
            }

            string serialized = SerializeRule(rule);
            InsertUniqueRule(ordered, serialized);
            index++;
        }

        StringBuilder css = new StringBuilder();
        index = 0;
        while (index < ordered.Count)
        {
            css.Append(ordered[index]);
            index++;
        }

        CssSerializationResult result = new CssSerializationResult();
        result.Css = css.ToString();
        result.Hash = StableHash.HashText(result.Css);
        result.Rules = ordered.ToArray();
        result.InputRuleCount = rules.Length;
        return result;
    }

    internal void InsertUniqueRule(List<string> rules, string rule)
    {
        int index = 0;
        while (index < rules.Count)
        {
            int comparison = string.CompareOrdinal(rules[index], rule);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                rules.Insert(index, rule);
                return;
            }

            index++;
        }

        rules.Add(rule);
    }

    internal UiCssDeclaration[] CreateCustomPropertyDeclarations(UiValueEntry[]? tokens, string? prefix)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        string checkedPrefix = Guard.ArgumentNotWhiteSpace(prefix, "prefix");
        List<UiValueEntry> ordered = new List<UiValueEntry>();
        int index = 0;
        while (index < tokens.Length)
        {
            UiValueEntry? token = tokens[index];
            if (token == null || string.IsNullOrWhiteSpace(token.Key) || token.Value == null)
            {
                throw new ArgumentException("Custom-property tokens require non-empty keys and values.", "tokens");
            }

            UiValueEntry? existing = FindToken(ordered, token.Key);
            if (existing != null)
            {
                if (!ValueJsonCodec.CompareNodes(existing.Value, token.Value))
                {
                    throw new ArgumentException("Conflicting custom-property tokens cannot use the same key.", "tokens");
                }
            }
            else
            {
                InsertToken(ordered, token);
            }

            index++;
        }

        UiCssDeclaration[] result = new UiCssDeclaration[ordered.Count];
        index = 0;
        while (index < ordered.Count)
        {
            UiValueEntry token = ordered[index];
            result[index] = new UiCssDeclaration
            {
                Property = CreateCustomPropertyName(checkedPrefix, token.Key),
                Value = FormatCustomPropertyValue(token.Value),
                IsTrusted = true
            };
            index++;
        }

        return result;
    }

    internal UiValueEntry? FindToken(List<UiValueEntry> tokens, string key)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            if (string.Equals(tokens[index].Key, key, StringComparison.Ordinal))
            {
                return tokens[index];
            }

            index++;
        }

        return null;
    }

    internal void InsertToken(List<UiValueEntry> tokens, UiValueEntry token)
    {
        int index = tokens.Count;
        tokens.Add(token);
        while (index > 0 && string.CompareOrdinal(tokens[index - 1].Key, token.Key) > 0)
        {
            tokens[index] = tokens[index - 1];
            index--;
        }

        tokens[index] = token;
    }

    internal string CreateCustomPropertyName(string prefix, string tokenKey)
    {
        string prefixPart = StableId.NormalizeKind(Guard.ArgumentNotWhiteSpace(prefix, "prefix"));
        string keyPart = StableId.NormalizeKind(Guard.ArgumentNotWhiteSpace(tokenKey, "tokenKey"));
        string hash = StableHash.HashParts(new string[] { prefix, tokenKey });
        return "--" + prefixPart + "-" + keyPart + "-" + hash.Substring(0, 12);
    }

    internal string FormatCustomPropertyValue(UiValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        switch (value.Kind)
        {
            case UiValueKind.Null:
                return "initial";
            case UiValueKind.Boolean:
                return InvariantFormatter.FormatBoolean(value.BooleanValue);
            case UiValueKind.Integer:
                return InvariantFormatter.FormatInt64(value.IntegerValue);
            case UiValueKind.FloatingPoint:
                return InvariantFormatter.FormatDouble(value.FloatingPointValue);
            case UiValueKind.Decimal:
                return InvariantFormatter.FormatDecimal(value.DecimalValue);
            case UiValueKind.String:
                if (value.StringValue == null)
                {
                    throw new ArgumentException("String theme tokens require a value.", "value");
                }

                return value.StringValue;
            case UiValueKind.DateTime:
                if (value.DateTimeValue == null)
                {
                    throw new ArgumentException("Date-time theme tokens require a value.", "value");
                }

                return CssEscaper.EscapeString(value.DateTimeValue);
            case UiValueKind.Binary:
            case UiValueKind.Array:
            case UiValueKind.Object:
                throw new ArgumentException("Binary and aggregate values cannot be serialized as CSS custom properties.", "value");
            default:
                throw new ArgumentOutOfRangeException("value", value.Kind, "The theme token value kind is not supported.");
        }
    }
}
