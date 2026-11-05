using System;
using System.Collections.Generic;

namespace iso20022;

internal class PlaceAndNameParser
{
    internal static PlaceAndNameParseResult ParseExpression(string expression)
    {
        PlaceAndNameParseResult result = new PlaceAndNameParseResult();
        string text = NamespaceService.TrimXmlWhitespace(expression);
        if (text.Length == 0)
        {
            result.ErrorMessage = "The PlaceAndName expression is empty.";
            return result;
        }
        if (!ValidateStructure(text, result))
        {
            return result;
        }

        result.Absolute = text[0] == '/';
        int start = result.Absolute ? 1 : 0;
        if (start == text.Length)
        {
            return Unsupported(result, "The document-node-only XPath is outside the supported element-path subset.");
        }
        if (text.Contains("///") || text.EndsWith("//"))
        {
            result.ErrorMessage = "The PlaceAndName expression contains an incomplete path step.";
            return result;
        }
        if (text.Contains("//"))
        {
            return Unsupported(result, "The descendant axis is outside the supported child-path subset.");
        }

        List<string> segments = SplitSegments(text, start, result);
        if (result.ErrorMessage.Length > 0)
        {
            return result;
        }
        int index = 0;
        while (index < segments.Count)
        {
            PlaceAndNameStep? step = ParseStep(segments[index], result);
            if (step == null)
            {
                return result;
            }
            result.Steps.Add(step);
            index++;
        }
        result.Success = true;
        result.Supported = true;
        return result;
    }

    internal static List<string> SplitSegments(
        string text,
        int start,
        PlaceAndNameParseResult result)
    {
        List<string> segments = new List<string>();
        int bracketDepth = 0;
        int segmentStart = start;
        int index = start;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '[')
            {
                bracketDepth++;
            }
            else if (value == ']')
            {
                bracketDepth--;
                if (bracketDepth < 0)
                {
                    result.ErrorMessage = "The PlaceAndName expression contains an unmatched closing bracket.";
                    return segments;
                }
            }
            else if (value == '/' && bracketDepth == 0)
            {
                if (index == segmentStart)
                {
                    result.ErrorMessage = "The PlaceAndName expression contains an empty path step.";
                    return segments;
                }
                segments.Add(text.Substring(segmentStart, index - segmentStart));
                segmentStart = index + 1;
            }
            index++;
        }
        if (bracketDepth != 0)
        {
            result.ErrorMessage = "The PlaceAndName expression contains an unmatched opening bracket.";
            return segments;
        }
        if (segmentStart == text.Length)
        {
            result.ErrorMessage = "The PlaceAndName expression ends with an empty path step.";
            return segments;
        }
        segments.Add(text.Substring(segmentStart));
        return segments;
    }

    internal static PlaceAndNameStep? ParseStep(
        string source,
        PlaceAndNameParseResult result)
    {
        string text = NamespaceService.TrimXmlWhitespace(source);
        if (text.Length == 0)
        {
            result.ErrorMessage = "The PlaceAndName expression contains an empty path step.";
            return null;
        }
        if (ContainsUnsupportedSyntax(text))
        {
            Unsupported(result, "The XPath uses syntax outside the supported qualified child-path subset.");
            return null;
        }

        string name = text;
        bool hasPosition = false;
        int position = 0;
        int opening = text.IndexOf('[');
        if (opening >= 0)
        {
            int closing = text.IndexOf(']');
            if (closing != text.Length - 1 || opening == 0)
            {
                Unsupported(result, "Only one trailing positive positional predicate is supported per path step.");
                return null;
            }
            name = text.Substring(0, opening);
            string digits = text.Substring(opening + 1, closing - opening - 1);
            if (!ContainsOnlyDigits(digits))
            {
                Unsupported(result, "Only a positive integer positional predicate is supported.");
                return null;
            }
            position = ReadPosition(digits);
            if (position <= 0)
            {
                Unsupported(result, "Only a positive 32-bit integer positional predicate is supported.");
                return null;
            }
            hasPosition = true;
        }

        int colon = name.IndexOf(':');
        string prefix = string.Empty;
        string localName = name;
        if (colon >= 0)
        {
            if (colon == 0 || colon == name.Length - 1 ||
                name.IndexOf(':', colon + 1) >= 0)
            {
                result.ErrorMessage = "A PlaceAndName path step contains an invalid qualified name.";
                return null;
            }
            prefix = name.Substring(0, colon);
            localName = name.Substring(colon + 1);
        }
        if (!XmlNameLexical.IsNcName(localName.AsSpan()) ||
            (prefix.Length > 0 && !XmlNameLexical.IsNcName(prefix.AsSpan())))
        {
            if (LooksLikeUnsupportedExpression(name))
            {
                Unsupported(result, "The XPath step is outside the supported element-name subset.");
                return null;
            }
            result.ErrorMessage = "A PlaceAndName path step contains an invalid XML name.";
            return null;
        }

        PlaceAndNameStep step = new PlaceAndNameStep();
        step.Prefix = prefix;
        step.LocalName = localName;
        step.HasPosition = hasPosition;
        step.Position = position;
        return step;
    }

    internal static bool ContainsUnsupportedSyntax(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '@' || value == '*' || value == '(' || value == ')' ||
                value == '|' || value == '=' || value == '\'' || value == '"' ||
                value == '$' || value == '+' || value == '<' || value == '>' ||
                value == ',' || value == '!' || char.IsWhiteSpace(value))
            {
                return true;
            }
            index++;
        }
        return text == "." || text == ".." || text.Contains("::");
    }

    internal static bool ValidateStructure(
        string text,
        PlaceAndNameParseResult result)
    {
        int bracketDepth = 0;
        int parenthesisDepth = 0;
        char quote = '\0';
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (quote != '\0')
            {
                if (value == quote)
                {
                    if (index + 1 < text.Length && text[index + 1] == quote)
                    {
                        index++;
                    }
                    else
                    {
                        quote = '\0';
                    }
                }
            }
            else if (value == '\'' || value == '"')
            {
                quote = value;
            }
            else if (value == '[')
            {
                bracketDepth++;
            }
            else if (value == ']')
            {
                bracketDepth--;
                if (bracketDepth < 0)
                {
                    result.ErrorMessage = "The PlaceAndName expression contains an unmatched closing bracket.";
                    return false;
                }
            }
            else if (value == '(')
            {
                parenthesisDepth++;
            }
            else if (value == ')')
            {
                parenthesisDepth--;
                if (parenthesisDepth < 0)
                {
                    result.ErrorMessage = "The PlaceAndName expression contains an unmatched closing parenthesis.";
                    return false;
                }
            }
            index++;
        }
        if (quote != '\0')
        {
            result.ErrorMessage = "The PlaceAndName expression contains an unterminated string literal.";
            return false;
        }
        if (bracketDepth != 0)
        {
            result.ErrorMessage = "The PlaceAndName expression contains an unmatched opening bracket.";
            return false;
        }
        if (parenthesisDepth != 0)
        {
            result.ErrorMessage = "The PlaceAndName expression contains an unmatched opening parenthesis.";
            return false;
        }
        return true;
    }

    internal static bool LooksLikeUnsupportedExpression(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        if (text[0] >= '0' && text[0] <= '9')
        {
            return true;
        }
        return text.Length > 1 && text[0] == '-' &&
            text[1] >= '0' && text[1] <= '9';
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static int ReadPosition(string text)
    {
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return -1;
            }
            value = value * 10 + digit;
            index++;
        }
        return value;
    }

    internal static PlaceAndNameParseResult Unsupported(
        PlaceAndNameParseResult result,
        string message)
    {
        result.Success = true;
        result.Supported = false;
        result.ErrorMessage = message;
        return result;
    }
}
