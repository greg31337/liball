using System;
using System.Collections.Generic;

namespace uri;

internal static class TemplateParserCore
{
    internal const string InvalidLiteralMessage = "The URI Template literal contains a character that is not allowed by RFC 6570.";
    internal const string UnmatchedClosingBraceMessage = "The URI Template contains an unmatched closing brace.";
    internal const string UnclosedExpressionMessage = "The URI Template expression is missing its closing brace.";
    internal const string NestedExpressionMessage = "URI Template expressions cannot be nested.";
    internal const string EmptyExpressionMessage = "The URI Template expression must contain at least one variable specification.";
    internal const string InvalidVariableMessage = "The URI Template variable name is invalid.";
    internal const string InvalidModifierMessage = "The URI Template variable modifier is invalid.";
    internal const string IncompatibleModifierMessage = "A URI Template variable cannot contain more than one modifier.";
    internal const string UnsupportedOperatorMessage = "The URI Template expression uses a reserved future operator.";

    internal static UriTemplateParseResult ParseTemplate(
        ReadOnlySpan<char> text)
    {
        UriTemplateParseResult structure = ScanTemplateStructure(text);
        if (!structure.Success)
        {
            return structure;
        }

        int partIndex = 0;
        while (partIndex < structure.Template.Parts.Length)
        {
            UriTemplatePart part = structure.Template.Parts[partIndex];
            if (part.Kind == UriTemplatePartKind.Expression)
            {
                UriDiagnostic? diagnostic = ParseExpression(text, part);
                if (diagnostic != null)
                {
                    return new UriTemplateParseResult(
                        false,
                        new UriTemplate(),
                        diagnostic);
                }
            }

            partIndex = partIndex + 1;
        }

        structure.Template.Level = TemplateInspectorCore.DetermineLevel(
            structure.Template);
        return structure;
    }

    internal static UriTemplateParseResult ScanTemplateStructure(
        ReadOnlySpan<char> text)
    {
        List<UriTemplatePart> parts = new List<UriTemplatePart>();
        int literalOffset = 0;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '{')
            {
                if (offset > literalOffset)
                {
                    parts.Add(CreateLiteralPart(
                        text.Slice(literalOffset, offset - literalOffset),
                        literalOffset));
                }

                int expressionOffset = offset;
                offset = offset + 1;
                bool closed = false;
                while (offset < text.Length && !closed)
                {
                    if (text[offset] == '{')
                    {
                        return CreateFailure(
                            UriDiagnosticCode.InvalidTemplateExpression,
                            offset,
                            1,
                            NestedExpressionMessage);
                    }

                    if (text[offset] == '}')
                    {
                        closed = true;
                    }
                    else
                    {
                        offset = offset + 1;
                    }
                }

                if (!closed)
                {
                    return CreateFailure(
                        UriDiagnosticCode.InvalidTemplateExpression,
                        expressionOffset,
                        text.Length - expressionOffset,
                        UnclosedExpressionMessage);
                }

                int expressionLength = offset - expressionOffset + 1;
                parts.Add(CreateExpressionPart(expressionOffset, expressionLength));
                offset = offset + 1;
                literalOffset = offset;
                continue;
            }

            if (text[offset] == '}')
            {
                return CreateFailure(
                    UriDiagnosticCode.InvalidTemplateExpression,
                    offset,
                    1,
                    UnmatchedClosingBraceMessage);
            }

            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    0,
                    UriComponent.TemplateLiteral);
                if (!triplet.Success)
                {
                    return CreatePercentFailure(triplet.Diagnostic, offset);
                }

                offset = triplet.NextOffset;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.UriTemplate,
                    UriComponent.TemplateLiteral,
                    scalar.ErrorOffset,
                    scalar.ErrorLength);
                return new UriTemplateParseResult(
                    false,
                    new UriTemplate(),
                    diagnostic);
            }

            if (!TemplateGrammar.IsLiteralScalar(scalar.ScalarValue))
            {
                UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
                    DiagnosticDomain.UriTemplate,
                    UriDiagnosticCode.InvalidTemplate,
                    UriComponent.TemplateLiteral,
                    offset,
                    scalar.CodeUnitLength,
                    InvalidLiteralMessage);
                return new UriTemplateParseResult(
                    false,
                    new UriTemplate(),
                    diagnostic);
            }

            offset = offset + scalar.CodeUnitLength;
        }

        if (literalOffset < text.Length)
        {
            parts.Add(CreateLiteralPart(
                text.Slice(literalOffset),
                literalOffset));
        }

        UriTemplate template = new UriTemplate();
        template.Parts = parts.ToArray();
        template.OriginalText = text.ToString();
        return new UriTemplateParseResult(true, template, null);
    }

    internal static UriTemplatePart CreateLiteralPart(
        ReadOnlySpan<char> text,
        int offset)
    {
        UriTemplatePart part = new UriTemplatePart();
        part.Literal = text.ToString();
        part.Range = new TextRange(offset, text.Length);
        return part;
    }

    internal static UriTemplatePart CreateExpressionPart(int offset, int length)
    {
        UriTemplatePart part = new UriTemplatePart();
        part.Kind = UriTemplatePartKind.Expression;
        part.Range = new TextRange(offset, length);
        return part;
    }

    internal static UriDiagnostic? ParseExpression(
        ReadOnlySpan<char> templateText,
        UriTemplatePart part)
    {
        int expressionOffset = part.Range.Offset + 1;
        int expressionLength = part.Range.Length - 2;
        ReadOnlySpan<char> expression = templateText.Slice(
            expressionOffset,
            expressionLength);
        if (expression.Length == 0)
        {
            return CreateExpressionDiagnostic(
                UriDiagnosticCode.InvalidTemplateExpression,
                expressionOffset,
                0,
                EmptyExpressionMessage);
        }

        int offset = 0;
        char first = expression[0];
        if (IsReservedFutureOperator(first))
        {
            return CreateExpressionDiagnostic(
                UriDiagnosticCode.UnsupportedTemplateOperator,
                expressionOffset,
                1,
                UnsupportedOperatorMessage);
        }

        if (IsDefinedOperator(first))
        {
            part.Operator = GetDefinedOperator(first);
            offset = 1;
        }

        if (offset == expression.Length)
        {
            return CreateExpressionDiagnostic(
                UriDiagnosticCode.InvalidTemplateExpression,
                expressionOffset + offset,
                0,
                EmptyExpressionMessage);
        }

        List<UriTemplateVariable> variables = new List<UriTemplateVariable>();
        while (offset < expression.Length)
        {
            int variableOffset = offset;
            bool hasVariableCharacter = false;
            bool followsDot = false;
            int dotOffset = -1;
            while (offset < expression.Length &&
                expression[offset] != ',' &&
                expression[offset] != ':' &&
                expression[offset] != '*')
            {
                char value = expression[offset];
                if (value == '.')
                {
                    if (!hasVariableCharacter || followsDot)
                    {
                        return CreateVariableDiagnostic(
                            UriDiagnosticCode.InvalidTemplateVariable,
                            expressionOffset + offset,
                            1,
                            InvalidVariableMessage);
                    }

                    followsDot = true;
                    dotOffset = offset;
                    offset = offset + 1;
                    continue;
                }

                if (value == '%')
                {
                    PercentTripletResult triplet = PercentScanner.ScanTriplet(
                        expression,
                        offset,
                        expressionOffset,
                        UriComponent.TemplateVariable);
                    if (!triplet.Success)
                    {
                        return CreateVariablePercentDiagnostic(
                            triplet.Diagnostic,
                            expressionOffset + offset);
                    }

                    hasVariableCharacter = true;
                    followsDot = false;
                    offset = triplet.NextOffset;
                    continue;
                }

                if (Ascii.IsAlphaNumeric(value) || value == '_')
                {
                    hasVariableCharacter = true;
                    followsDot = false;
                    offset = offset + 1;
                    continue;
                }

                return CreateVariableDiagnostic(
                    UriDiagnosticCode.InvalidTemplateVariable,
                    expressionOffset + offset,
                    1,
                    InvalidVariableMessage);
            }

            if (!hasVariableCharacter)
            {
                return CreateVariableDiagnostic(
                    UriDiagnosticCode.InvalidTemplateVariable,
                    expressionOffset + variableOffset,
                    1,
                    InvalidVariableMessage);
            }

            if (followsDot)
            {
                return CreateVariableDiagnostic(
                    UriDiagnosticCode.InvalidTemplateVariable,
                    expressionOffset + dotOffset,
                    1,
                    InvalidVariableMessage);
            }

            int nameLength = offset - variableOffset;
            UriTemplateVariable variable = new UriTemplateVariable();
            variable.Name = expression.Slice(
                variableOffset,
                nameLength).ToString();

            if (offset < expression.Length && expression[offset] == ':')
            {
                int modifierOffset = offset;
                offset = offset + 1;
                if (offset == expression.Length || expression[offset] == ',')
                {
                    return CreateVariableDiagnostic(
                        UriDiagnosticCode.InvalidTemplateModifier,
                        expressionOffset + modifierOffset,
                        1,
                        InvalidModifierMessage);
                }

                if (expression[offset] < '1' || expression[offset] > '9')
                {
                    return CreateVariableDiagnostic(
                        UriDiagnosticCode.InvalidTemplateModifier,
                        expressionOffset + offset,
                        1,
                        InvalidModifierMessage);
                }

                int prefixLength = 0;
                int digitCount = 0;
                while (offset < expression.Length &&
                    expression[offset] >= '0' &&
                    expression[offset] <= '9')
                {
                    if (digitCount == 4)
                    {
                        return CreateVariableDiagnostic(
                            UriDiagnosticCode.InvalidTemplateModifier,
                            expressionOffset + offset,
                            1,
                            InvalidModifierMessage);
                    }

                    prefixLength = prefixLength * 10 + expression[offset] - '0';
                    digitCount = digitCount + 1;
                    offset = offset + 1;
                }

                variable.HasPrefix = true;
                variable.PrefixLength = prefixLength;
            }
            else if (offset < expression.Length && expression[offset] == '*')
            {
                variable.Explode = true;
                offset = offset + 1;
            }

            if (offset < expression.Length && expression[offset] != ',')
            {
                string message = InvalidModifierMessage;
                if (expression[offset] == ':' || expression[offset] == '*')
                {
                    message = IncompatibleModifierMessage;
                }

                return CreateVariableDiagnostic(
                    UriDiagnosticCode.InvalidTemplateModifier,
                    expressionOffset + offset,
                    1,
                    message);
            }

            variable.Range = new TextRange(
                expressionOffset + variableOffset,
                offset - variableOffset);
            variables.Add(variable);

            if (offset < expression.Length)
            {
                int commaOffset = offset;
                offset = offset + 1;
                if (offset == expression.Length)
                {
                    return CreateVariableDiagnostic(
                        UriDiagnosticCode.InvalidTemplateVariable,
                        expressionOffset + commaOffset,
                        1,
                        InvalidVariableMessage);
                }

                if (expression[offset] == ',')
                {
                    return CreateVariableDiagnostic(
                        UriDiagnosticCode.InvalidTemplateVariable,
                        expressionOffset + offset,
                        1,
                        InvalidVariableMessage);
                }
            }
        }

        part.Variables = variables.ToArray();
        return null;
    }

    internal static bool IsDefinedOperator(char value)
    {
        return value == '+' ||
            value == '#' ||
            value == '.' ||
            value == '/' ||
            value == ';' ||
            value == '?' ||
            value == '&';
    }

    internal static UriTemplateOperator GetDefinedOperator(char value)
    {
        if (value == '+')
        {
            return UriTemplateOperator.Reserved;
        }

        if (value == '#')
        {
            return UriTemplateOperator.Fragment;
        }

        if (value == '.')
        {
            return UriTemplateOperator.Label;
        }

        if (value == '/')
        {
            return UriTemplateOperator.PathSegment;
        }

        if (value == ';')
        {
            return UriTemplateOperator.PathParameter;
        }

        if (value == '?')
        {
            return UriTemplateOperator.Query;
        }

        if (value == '&')
        {
            return UriTemplateOperator.QueryContinuation;
        }

        return UriTemplateOperator.Simple;
    }

    internal static bool IsReservedFutureOperator(char value)
    {
        return value == '=' ||
            value == ',' ||
            value == '!' ||
            value == '@' ||
            value == '|';
    }

    internal static UriDiagnostic CreateExpressionDiagnostic(
        UriDiagnosticCode code,
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            code,
            UriComponent.TemplateExpression,
            offset,
            length,
            message);
    }

    internal static UriDiagnostic CreateVariableDiagnostic(
        UriDiagnosticCode code,
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            code,
            UriComponent.TemplateVariable,
            offset,
            length,
            message);
    }

    internal static UriDiagnostic CreateVariablePercentDiagnostic(
        UriDiagnostic? source,
        int fallbackOffset)
    {
        int offset = fallbackOffset;
        int length = 1;
        if (source != null)
        {
            offset = source.Range.Offset;
            length = source.Range.Length;
        }

        return CreateVariableDiagnostic(
            UriDiagnosticCode.InvalidPercentEncoding,
            offset,
            length,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    internal static UriTemplateParseResult CreateFailure(
        UriDiagnosticCode code,
        int offset,
        int length,
        string message)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            code,
            UriComponent.TemplateExpression,
            offset,
            length,
            message);
        return new UriTemplateParseResult(false, new UriTemplate(), diagnostic);
    }

    internal static UriTemplateParseResult CreatePercentFailure(
        UriDiagnostic? source,
        int fallbackOffset)
    {
        int offset = fallbackOffset;
        int length = 1;
        if (source != null)
        {
            offset = source.Range.Offset;
            length = source.Range.Length;
        }

        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.TemplateLiteral,
            offset,
            length,
            DiagnosticFactory.InvalidPercentEncodingMessage);
        return new UriTemplateParseResult(false, new UriTemplate(), diagnostic);
    }
}
