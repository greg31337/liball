using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XsltValueTemplateParser
{
    internal XsltStylesheetLoader Loader;

    internal XsltValueTemplateParser(XsltStylesheetLoader loader)
    {
        Loader = loader;
    }

    internal XsltValueTemplate ParseXsltValueTemplate(string value, XmlNode node, XsltValueTemplateKind kind, XsltStylesheetModule? module, string? location)
    {
        List<XsltValueTemplatePart> parts = new List<XsltValueTemplatePart>();
        StringBuilder fixedValue = new StringBuilder();
        int fixedStart = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '{' && index + 1 < value.Length && value[index + 1] == '{')
            {
                fixedValue.Append('{');
                index = index + 2;
            }
            else if (character == '}' && index + 1 < value.Length && value[index + 1] == '}')
            {
                fixedValue.Append('}');
                index = index + 2;
            }
            else if (character == '{')
            {
                AddXsltFixedValuePart(parts, fixedValue, fixedStart, index, value, node);
                int close = FindXsltValueTemplateExpressionEnd(value, index + 1);
                if (close < 0)
                {
                    throw CreateXsltValueTemplateError("A value template contains an unmatched opening brace.", XsltErrorCodes.XTSE0350, node, module, location, value, index);
                }

                string expression = value.Substring(index + 1, close - index - 1);
                if (IsXsltEmptyValueTemplateExpression(expression))
                {
                    expression = "";
                }

                SourceRange expressionRange = CreateXsltValueTemplateRange(node, value, index + 1, close);
                parts.Add(new XsltValueTemplatePart(XsltValueTemplatePartKind.Expression, expression, index + 1, close - index - 1, expressionRange));
                index = close + 1;
                fixedStart = index;
            }
            else if (character == '}')
            {
                throw CreateXsltValueTemplateError("A value template contains an unmatched closing brace.", XsltErrorCodes.XTSE0370, node, module, location, value, index);
            }
            else
            {
                fixedValue.Append(character);
                index = index + 1;
            }
        }

        AddXsltFixedValuePart(parts, fixedValue, fixedStart, value.Length, value, node);
        return new XsltValueTemplate(node, kind, value, parts.ToArray());
    }

    internal void AddXsltFixedValuePart(List<XsltValueTemplatePart> parts, StringBuilder fixedValue, int start, int end, string source, XmlNode node)
    {
        if (fixedValue.Length == 0)
        {
            return;
        }

        SourceRange range = CreateXsltValueTemplateRange(node, source, start, end);
        parts.Add(new XsltValueTemplatePart(XsltValueTemplatePartKind.Fixed, fixedValue.ToString(), start, end - start, range));
        fixedValue.Clear();
    }

    internal int FindXsltValueTemplateExpressionEnd(string value, int start)
    {
        char quote = '\0';
        int braceDepth = 0;
        int commentDepth = 0;
        int index = start;
        while (index < value.Length)
        {
            char character = value[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    if (index + 1 < value.Length && value[index + 1] == quote)
                    {
                        index = index + 2;
                        continue;
                    }

                    quote = '\0';
                }

                index = index + 1;
                continue;
            }

            if (commentDepth > 0)
            {
                if (character == '(' && index + 1 < value.Length && value[index + 1] == ':')
                {
                    commentDepth = commentDepth + 1;
                    index = index + 2;
                }
                else if (character == ':' && index + 1 < value.Length && value[index + 1] == ')')
                {
                    commentDepth = commentDepth - 1;
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }

                continue;
            }

            if (character == '(' && index + 1 < value.Length && value[index + 1] == ':')
            {
                commentDepth = 1;
                index = index + 2;
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
                index = index + 1;
            }
            else if (character == '{')
            {
                braceDepth = braceDepth + 1;
                index = index + 1;
            }
            else if (character == '}' && braceDepth > 0)
            {
                braceDepth = braceDepth - 1;
                index = index + 1;
            }
            else if (character == '}')
            {
                return index;
            }
            else
            {
                index = index + 1;
            }
        }

        return -1;
    }

    internal bool IsXsltEmptyValueTemplateExpression(string value)
    {
        int commentDepth = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (commentDepth > 0)
            {
                if (character == '(' && index + 1 < value.Length && value[index + 1] == ':')
                {
                    commentDepth = commentDepth + 1;
                    index = index + 2;
                }
                else if (character == ':' && index + 1 < value.Length && value[index + 1] == ')')
                {
                    commentDepth = commentDepth - 1;
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }

                continue;
            }

            if (character == '(' && index + 1 < value.Length && value[index + 1] == ':')
            {
                commentDepth = 1;
                index = index + 2;
            }
            else if (character == ' ' || character == '\t' || character == '\r' || character == '\n')
            {
                index = index + 1;
            }
            else
            {
                return false;
            }
        }

        return commentDepth == 0;
    }

    internal SourceRange CreateXsltValueTemplateRange(XmlNode node, string value, int start, int end)
    {
        SourceRange result = new SourceRange();
        result.Start = CreateXsltValueTemplateLocation(node, value, start);
        result.End = CreateXsltValueTemplateLocation(node, value, end);
        return result;
    }

    internal SourceLocation CreateXsltValueTemplateLocation(XmlNode node, string value, int offset)
    {
        SourceLocation? origin = node.Range == null ? null : node.Range.Start;
        SourceLocation result = new SourceLocation();
        result.SourceUri = origin == null ? null : origin.SourceUri;
        result.CharacterOffset = origin == null ? offset : origin.CharacterOffset + offset;
        result.ByteOffset = -1;
        result.Line = origin == null || origin.Line <= 0 ? 1 : origin.Line;
        result.Column = origin == null || origin.Column <= 0 ? 1 : origin.Column;
        int index = 0;
        while (index < offset && index < value.Length)
        {
            if (value[index] == '\n')
            {
                result.Line = result.Line + 1;
                result.Column = 1;
            }
            else
            {
                result.Column = result.Column + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal XsltStaticException CreateXsltValueTemplateError(string message, string code, XmlNode node, XsltStylesheetModule? module, string? location, string source, int offset)
    {
        XsltStaticException error = Loader.StaticProcessor.CreateXsltPreprocessingError(message, code, node, module, location);
        SourceRange range = CreateXsltValueTemplateRange(node, source, offset, offset + 1);
        if (error.Diagnostic != null)
        {
            error.Diagnostic.Range = range;
        }

        error.Location = range.Start;
        return error;
    }
}
