using System;
using System.Text;

namespace htmlgui;

internal static class CssEscaper
{
    internal static string EscapeIdentifier(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("CSS identifiers cannot be empty.", "value");
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0')
            {
                result.Append('\uFFFD');
            }
            else if (char.IsHighSurrogate(character))
            {
                if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    result.Append(character);
                    index++;
                    result.Append(value[index]);
                }
                else
                {
                    result.Append('\uFFFD');
                }
            }
            else if (char.IsLowSurrogate(character))
            {
                result.Append('\uFFFD');
            }
            else if (IsIdentifierControl(character) || IsIdentifierLeadingDigit(value, index, character))
            {
                AppendHexEscape(result, character);
            }
            else if (IsIdentifierCharacter(character))
            {
                result.Append(character);
            }
            else
            {
                result.Append('\\');
                result.Append(character);
            }

            index++;
        }

        if (value.Length == 1 && value[0] == '-')
        {
            return "\\-";
        }

        return result.ToString();
    }

    internal static string EscapeString(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder();
        result.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0')
            {
                result.Append('\uFFFD');
            }
            else if (char.IsHighSurrogate(character))
            {
                if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    result.Append(character);
                    index++;
                    result.Append(value[index]);
                }
                else
                {
                    result.Append('\uFFFD');
                }
            }
            else if (char.IsLowSurrogate(character))
            {
                result.Append('\uFFFD');
            }
            else if (character == '"' || character == '\\')
            {
                result.Append('\\');
                result.Append(character);
            }
            else if (character < ' ' || character == '\u007f' || character == '<' || character == '>')
            {
                AppendHexEscape(result, character);
            }
            else
            {
                result.Append(character);
            }

            index++;
        }

        result.Append('"');
        return result.ToString();
    }

    internal static string EscapeUrl(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0 || !IsSafeUrl(value))
        {
            throw new ArgumentException("CSS URLs must be non-empty and use an allowed URL scheme.", "value");
        }

        return "url(" + EscapeString(value) + ")";
    }

    internal static string EscapeSelector(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string selector = value.Trim();
        if (selector.Length == 0 || !IsSafeSelector(selector))
        {
            throw new ArgumentException("The CSS selector is empty, malformed, or contains an unsafe delimiter.", "value");
        }

        return selector;
    }

    internal static string EscapeDeclarationValue(string? property, string? value, bool isTrusted)
    {
        if (property == null)
        {
            throw new ArgumentNullException("property");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!IsValidPropertyName(property))
        {
            throw new ArgumentException("CSS property names must be canonical identifiers.", "property");
        }

        if (!isTrusted && !IsAllowedUntrustedProperty(property))
        {
            throw new ArgumentException("The CSS property is not allowed for an untrusted declaration.", "property");
        }

        string token = value.Trim();
        if (token.Length == 0 || !IsSafeDeclarationToken(token, isTrusted))
        {
            throw new ArgumentException("The CSS declaration value is empty, malformed, or contains an unsafe token.", "value");
        }

        return EscapeStyleMarkupCharacters(token);
    }

    internal static bool IsAllowedUntrustedProperty(string? property)
    {
        if (!IsValidPropertyName(property))
        {
            return false;
        }

        switch (property)
        {
            case CssProperties.Direction:
            case CssProperties.Display:
            case CssProperties.Visibility:
            case CssProperties.Position:
            case CssProperties.Inset:
            case CssProperties.Top:
            case CssProperties.Right:
            case CssProperties.Bottom:
            case CssProperties.Left:
            case CssProperties.ZIndex:
            case CssProperties.Width:
            case CssProperties.Height:
            case CssProperties.MinimumWidth:
            case CssProperties.MinimumHeight:
            case CssProperties.MaximumWidth:
            case CssProperties.MaximumHeight:
            case CssProperties.AspectRatio:
            case CssProperties.BoxSizing:
            case CssProperties.Margin:
            case CssProperties.Padding:
            case CssProperties.Border:
            case CssProperties.BorderRadius:
            case CssProperties.BackgroundColor:
            case CssProperties.Color:
            case CssProperties.Opacity:
            case CssProperties.BoxShadow:
            case CssProperties.Overflow:
            case CssProperties.OverflowX:
            case CssProperties.OverflowY:
            case CssProperties.Contain:
            case CssProperties.ContainerName:
            case CssProperties.ContainerType:
            case CssProperties.FlexDirection:
            case CssProperties.FlexWrap:
            case CssProperties.FlexGrow:
            case CssProperties.FlexShrink:
            case CssProperties.FlexBasis:
            case CssProperties.AlignItems:
            case CssProperties.AlignSelf:
            case CssProperties.AlignContent:
            case CssProperties.JustifyContent:
            case CssProperties.JustifySelf:
            case CssProperties.Gap:
            case CssProperties.RowGap:
            case CssProperties.ColumnGap:
            case CssProperties.Order:
            case CssProperties.GridTemplateColumns:
            case CssProperties.GridTemplateRows:
            case CssProperties.GridTemplateAreas:
            case CssProperties.GridColumn:
            case CssProperties.GridRow:
            case CssProperties.GridArea:
            case CssProperties.FontFamily:
            case CssProperties.FontSize:
            case CssProperties.FontStyle:
            case CssProperties.FontWeight:
            case CssProperties.LineHeight:
            case CssProperties.LetterSpacing:
            case CssProperties.TextAlign:
            case CssProperties.TextDecoration:
            case CssProperties.WhiteSpace:
            case CssProperties.WordBreak:
            case CssProperties.PointerEvents:
            case CssProperties.UserSelect:
            case CssProperties.Transform:
            case CssProperties.TransformOrigin:
            case CssProperties.Outline:
            case CssProperties.OutlineOffset:
            case CssProperties.ScrollBehavior:
            case CssProperties.ScrollMargin:
            case CssProperties.ScrollPadding:
            case "inset-block-start":
            case "inset-inline-end":
            case "inset-block-end":
            case "inset-inline-start":
            case "margin-block-start":
            case "margin-inline-end":
            case "margin-block-end":
            case "margin-inline-start":
            case "padding-block-start":
            case "padding-inline-end":
            case "padding-block-end":
            case "padding-inline-start":
            case "resize":
                return true;
            default:
                return false;
        }
    }

    internal static bool IsValidPropertyName(string? property)
    {
        if (property == null || property.Length == 0)
        {
            return false;
        }

        bool isCustom = property.Length > 2 && property[0] == '-' && property[1] == '-';
        int index = isCustom ? 2 : 0;
        if (!isCustom && property[0] == '-')
        {
            if (property.Length == 1 || !IsLowerAsciiLetter(property[1]))
            {
                return false;
            }

            index = 1;
        }
        else if (!isCustom && !IsLowerAsciiLetter(property[0]))
        {
            return false;
        }

        while (index < property.Length)
        {
            char character = property[index];
            bool isDigit = character >= '0' && character <= '9';
            if (!IsLowerAsciiLetter(character) && !isDigit && character != '-' && character != '_')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsSafeUrl(string value)
    {
        return HtmlSecurityPolicy.IsSafeResourceUrl(value);
    }

    internal static bool IsSafeSelector(string selector)
    {
        int squareDepth = 0;
        int parenthesisDepth = 0;
        char quote = '\0';
        bool hasToken = false;
        bool requiresToken = false;
        int index = 0;
        while (index < selector.Length)
        {
            char character = selector[index];
            if (character == '\0' || character < ' ' && character != '\t' || character == '\u007f' || character == '<' || character == '{' || character == '}' || character == ';' || character == '@')
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= selector.Length || !char.IsLowSurrogate(selector[index + 1]))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                return false;
            }

            if (character == '/' && index + 1 < selector.Length && selector[index + 1] == '*')
            {
                return false;
            }

            if (quote != '\0')
            {
                if (character == '\\')
                {
                    if (!HasSafeEscapeFollower(selector, index))
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
                if (squareDepth == 0 && parenthesisDepth == 0)
                {
                    return false;
                }

                quote = character;
            }
            else if (character == '\\')
            {
                if (!HasSafeEscapeFollower(selector, index))
                {
                    return false;
                }

                index++;
                hasToken = true;
                requiresToken = false;
            }
            else if (character == '[')
            {
                if (squareDepth != 0)
                {
                    return false;
                }

                squareDepth++;
                hasToken = true;
                requiresToken = false;
            }
            else if (character == ']')
            {
                if (squareDepth == 0)
                {
                    return false;
                }

                squareDepth--;
            }
            else if (character == '(')
            {
                parenthesisDepth++;
                hasToken = true;
                requiresToken = false;
            }
            else if (character == ')')
            {
                if (parenthesisDepth == 0)
                {
                    return false;
                }

                parenthesisDepth--;
            }
            else if (squareDepth == 0 && parenthesisDepth == 0 && character == ',')
            {
                if (!hasToken || requiresToken)
                {
                    return false;
                }

                hasToken = false;
                requiresToken = true;
            }
            else if (squareDepth == 0 && parenthesisDepth == 0 && (character == '>' || character == '+' || character == '~'))
            {
                if (!hasToken || requiresToken)
                {
                    return false;
                }

                requiresToken = true;
            }
            else if (!char.IsWhiteSpace(character))
            {
                hasToken = true;
                requiresToken = false;
            }

            index++;
        }

        return quote == '\0' && squareDepth == 0 && parenthesisDepth == 0 && hasToken && !requiresToken;
    }

    internal static bool IsSafeDeclarationToken(string value, bool isTrusted)
    {
        int parenthesisDepth = 0;
        int squareDepth = 0;
        char quote = '\0';
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character < ' ' || character == '\u007f' || character == ';' || character == '{' || character == '}')
            {
                return false;
            }

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                return false;
            }

            if (!isTrusted && character == '\\')
            {
                return false;
            }

            if (quote != '\0')
            {
                if (character == '\\')
                {
                    if (!HasSafeEscapeFollower(value, index))
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

        if (quote != '\0' || parenthesisDepth != 0 || squareDepth != 0 || value.Contains("/*", StringComparison.Ordinal) || value.Contains("*/", StringComparison.Ordinal))
        {
            return false;
        }

        if (!isTrusted && (value.Contains("url(", StringComparison.OrdinalIgnoreCase)
            || value.Contains("expression(", StringComparison.OrdinalIgnoreCase)
            || value.Contains("@import", StringComparison.OrdinalIgnoreCase)
            || value.Contains("-moz-binding", StringComparison.OrdinalIgnoreCase)
            || value.Contains("!important", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    internal static bool HasSafeEscapeFollower(string value, int escapeIndex)
    {
        int followerIndex = escapeIndex + 1;
        if (followerIndex >= value.Length)
        {
            return false;
        }

        char follower = value[followerIndex];
        if (follower == '\0' || follower < ' ' || follower == '\u007f' || char.IsSurrogate(follower))
        {
            return false;
        }

        return true;
    }

    internal static string EscapeStyleMarkupCharacters(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '<' || character == '>')
            {
                AppendHexEscape(result, character);
            }
            else
            {
                result.Append(character);
            }

            index++;
        }

        return result.ToString();
    }

    internal static void AppendHexEscape(StringBuilder result, char character)
    {
        result.Append('\\');
        result.Append(((int)character).ToString("x", System.Globalization.CultureInfo.InvariantCulture));
        result.Append(' ');
    }

    internal static bool IsIdentifierControl(char character)
    {
        return character <= ' ' || character == '\u007f';
    }

    internal static bool IsIdentifierLeadingDigit(string value, int index, char character)
    {
        bool isDigit = character >= '0' && character <= '9';
        return isDigit && (index == 0 || index == 1 && value[0] == '-');
    }

    internal static bool IsIdentifierCharacter(char character)
    {
        bool isUpper = character >= 'A' && character <= 'Z';
        bool isLower = character >= 'a' && character <= 'z';
        bool isDigit = character >= '0' && character <= '9';
        return isUpper || isLower || isDigit || character == '-' || character == '_' || character >= '\u0080';
    }

    internal static bool IsLowerAsciiLetter(char character)
    {
        return character >= 'a' && character <= 'z';
    }
}
