using System;

namespace uri;

internal static class TemplateComposerCore
{
    internal const string InvalidModelMessage = "The URI Template model is inconsistent.";

    internal static UriTextResult ComposeTemplate(
        UriTemplate? template,
        OperationLimits? limits)
    {
        return ComposeModel(template, limits, false);
    }

    internal static UriTextResult CanonicalizePercentCase(
        UriTemplate? template,
        OperationLimits? limits)
    {
        return ComposeModel(template, limits, true);
    }

    internal static UriTextResult ComposeModel(
        UriTemplate? template,
        OperationLimits? limits,
        bool canonicalizePercentCase)
    {
        if (template == null || limits == null || template.Parts == null)
        {
            return CreateFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate));
        }

        UriDiagnostic? expressionLimit = LimitGuard.CheckTemplateExpressions(
            0,
            limits,
            0);
        if (expressionLimit != null)
        {
            return CreateFailure(expressionLimit);
        }

        UriDiagnostic? variableLimit = LimitGuard.CheckTemplateVariables(
            0,
            limits,
            0);
        if (variableLimit != null)
        {
            return CreateFailure(variableLimit);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return CreateFailure(CreateOutputDiagnostic(status));
        }

        int expressionCount = 0;
        int variableCount = 0;
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart? part = template.Parts[partIndex];
            if (part == null)
            {
                return CreateFailure(CreateModelDiagnostic(
                    UriComponent.Template,
                    output.GetLength()));
            }

            if (part.Kind == UriTemplatePartKind.Literal)
            {
                UriDiagnostic? literalDiagnostic = ValidateLiteral(
                    part.Literal,
                    output.GetLength());
                if (literalDiagnostic != null)
                {
                    return CreateFailure(literalDiagnostic);
                }

                AppendText(
                    part.Literal,
                    canonicalizePercentCase,
                    output);
            }
            else if (part.Kind == UriTemplatePartKind.Expression)
            {
                expressionCount = expressionCount + 1;
                expressionLimit = LimitGuard.CheckTemplateExpressions(
                    expressionCount,
                    limits,
                    output.GetLength());
                if (expressionLimit != null)
                {
                    return CreateFailure(expressionLimit);
                }

                if (part.Variables == null || part.Variables.Length == 0)
                {
                    return CreateFailure(DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidTemplateExpression,
                        UriComponent.TemplateExpression,
                        output.GetLength(),
                        0,
                        TemplateParserCore.EmptyExpressionMessage));
                }

                if (!IsDefinedOperator(part.Operator))
                {
                    return CreateFailure(CreateModelDiagnostic(
                        UriComponent.TemplateExpression,
                        output.GetLength()));
                }

                if (part.Variables.Length > int.MaxValue - variableCount)
                {
                    return CreateFailure(DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.LimitExceeded,
                        UriComponent.TemplateVariable,
                        output.GetLength(),
                        0,
                        LimitGuard.CollectionLimitMessage));
                }

                variableCount = variableCount + part.Variables.Length;
                variableLimit = LimitGuard.CheckTemplateVariables(
                    variableCount,
                    limits,
                    output.GetLength());
                if (variableLimit != null)
                {
                    return CreateFailure(variableLimit);
                }

                output.AppendCharacter('{');
                char operatorCharacter = GetOperatorCharacter(part.Operator);
                if (operatorCharacter != '\0')
                {
                    output.AppendCharacter(operatorCharacter);
                }

                int variableIndex = 0;
                while (variableIndex < part.Variables.Length)
                {
                    if (variableIndex > 0)
                    {
                        output.AppendCharacter(',');
                    }

                    UriTemplateVariable? variable =
                        part.Variables[variableIndex];
                    UriDiagnostic? variableDiagnostic = ValidateVariable(
                        variable,
                        output.GetLength());
                    if (variableDiagnostic != null)
                    {
                        return CreateFailure(variableDiagnostic);
                    }

                    if (variable == null)
                    {
                        return CreateFailure(CreateModelDiagnostic(
                            UriComponent.TemplateVariable,
                            output.GetLength()));
                    }

                    AppendText(
                        variable.Name,
                        canonicalizePercentCase,
                        output);
                    if (variable.HasPrefix)
                    {
                        output.AppendCharacter(':');
                        output.AppendText(variable.PrefixLength.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if (variable.Explode)
                    {
                        output.AppendCharacter('*');
                    }

                    variableIndex = variableIndex + 1;
                }

                output.AppendCharacter('}');
            }
            else
            {
                return CreateFailure(CreateModelDiagnostic(
                    UriComponent.Template,
                    output.GetLength()));
            }

            status = output.GetStatus();
            if (!status.Success)
            {
                return CreateFailure(CreateOutputDiagnostic(status));
            }

            partIndex = partIndex + 1;
        }

        string text = output.GetText();
        return new UriTextResult(true, text, text.Length, null);
    }

    internal static UriDiagnostic? ValidateLiteral(
        string? literal,
        int baseOffset)
    {
        if (literal == null)
        {
            return CreateModelDiagnostic(
                UriComponent.TemplateLiteral,
                baseOffset);
        }

        ReadOnlySpan<char> text = literal.AsSpan();
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    baseOffset,
                    UriComponent.TemplateLiteral);
                if (!triplet.Success)
                {
                    int errorOffset = baseOffset + offset;
                    int errorLength = 1;
                    if (triplet.Diagnostic != null)
                    {
                        errorOffset = triplet.Diagnostic.Range.Offset;
                        errorLength = triplet.Diagnostic.Range.Length;
                    }

                    return DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidPercentEncoding,
                        UriComponent.TemplateLiteral,
                        errorOffset,
                        errorLength,
                        DiagnosticFactory.InvalidPercentEncodingMessage);
                }

                offset = triplet.NextOffset;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.UriTemplate,
                    UriComponent.TemplateLiteral,
                    baseOffset + scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            if (!TemplateGrammar.IsLiteralScalar(scalar.ScalarValue))
            {
                return DiagnosticFactory.CreateError(
                    DiagnosticDomain.UriTemplate,
                    UriDiagnosticCode.InvalidTemplate,
                    UriComponent.TemplateLiteral,
                    baseOffset + offset,
                    scalar.CodeUnitLength,
                    TemplateParserCore.InvalidLiteralMessage);
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static UriDiagnostic? ValidateVariable(
        UriTemplateVariable? variable,
        int baseOffset)
    {
        if (variable == null || variable.Name == null ||
            variable.Name.Length == 0)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidTemplateVariable,
                UriComponent.TemplateVariable,
                baseOffset,
                0,
                TemplateParserCore.InvalidVariableMessage);
        }

        ReadOnlySpan<char> name = variable.Name.AsSpan();
        bool segmentHasCharacter = false;
        int offset = 0;
        while (offset < name.Length)
        {
            char value = name[offset];
            if (value == '.')
            {
                if (!segmentHasCharacter || offset == name.Length - 1)
                {
                    return CreateVariableDiagnostic(baseOffset + offset);
                }

                segmentHasCharacter = false;
                offset = offset + 1;
                continue;
            }

            if (value == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    name,
                    offset,
                    baseOffset,
                    UriComponent.TemplateVariable);
                if (!triplet.Success)
                {
                    int errorOffset = baseOffset + offset;
                    int errorLength = 1;
                    if (triplet.Diagnostic != null)
                    {
                        errorOffset = triplet.Diagnostic.Range.Offset;
                        errorLength = triplet.Diagnostic.Range.Length;
                    }

                    return DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidPercentEncoding,
                        UriComponent.TemplateVariable,
                        errorOffset,
                        errorLength,
                        DiagnosticFactory.InvalidPercentEncodingMessage);
                }

                segmentHasCharacter = true;
                offset = triplet.NextOffset;
                continue;
            }

            if (!Ascii.IsAlphaNumeric(value) && value != '_')
            {
                return CreateVariableDiagnostic(baseOffset + offset);
            }

            segmentHasCharacter = true;
            offset = offset + 1;
        }

        if (!segmentHasCharacter)
        {
            return CreateVariableDiagnostic(baseOffset + name.Length - 1);
        }

        if (variable.HasPrefix && variable.Explode)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidTemplateModifier,
                UriComponent.TemplateVariable,
                baseOffset + name.Length,
                0,
                TemplateParserCore.IncompatibleModifierMessage);
        }

        if (variable.HasPrefix &&
            (variable.PrefixLength < 1 || variable.PrefixLength > 9999))
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidTemplateModifier,
                UriComponent.TemplateVariable,
                baseOffset + name.Length,
                0,
                TemplateParserCore.InvalidModifierMessage);
        }

        return null;
    }

    internal static void AppendText(
        string text,
        bool canonicalizePercentCase,
        TextOutput output)
    {
        if (!canonicalizePercentCase)
        {
            output.AppendText(text);
            return;
        }

        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                int value = Ascii.GetHexByte(
                    text[offset + 1],
                    text[offset + 2]);
                output.AppendCharacter('%');
                output.AppendCharacter(Ascii.GetUpperHexCharacter(value >> 4));
                output.AppendCharacter(Ascii.GetUpperHexCharacter(value & 15));
                offset = offset + 3;
            }
            else
            {
                output.AppendCharacter(text[offset]);
                offset = offset + 1;
            }
        }
    }

    internal static bool IsDefinedOperator(UriTemplateOperator value)
    {
        return value == UriTemplateOperator.Simple ||
            value == UriTemplateOperator.Reserved ||
            value == UriTemplateOperator.Fragment ||
            value == UriTemplateOperator.Label ||
            value == UriTemplateOperator.PathSegment ||
            value == UriTemplateOperator.PathParameter ||
            value == UriTemplateOperator.Query ||
            value == UriTemplateOperator.QueryContinuation;
    }

    internal static char GetOperatorCharacter(UriTemplateOperator value)
    {
        switch (value)
        {
            case UriTemplateOperator.Reserved:
                return '+';
            case UriTemplateOperator.Fragment:
                return '#';
            case UriTemplateOperator.Label:
                return '.';
            case UriTemplateOperator.PathSegment:
                return '/';
            case UriTemplateOperator.PathParameter:
                return ';';
            case UriTemplateOperator.Query:
                return '?';
            case UriTemplateOperator.QueryContinuation:
                return '&';
            default:
                return '\0';
        }
    }

    internal static UriDiagnostic CreateVariableDiagnostic(int offset)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.InvalidTemplateVariable,
            UriComponent.TemplateVariable,
            offset,
            1,
            TemplateParserCore.InvalidVariableMessage);
    }

    internal static UriDiagnostic CreateModelDiagnostic(
        UriComponent component,
        int offset)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.InvalidTemplate,
            component,
            offset,
            0,
            InvalidModelMessage);
    }

    internal static UriDiagnostic CreateOutputDiagnostic(
        TextOutputResult status)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            status.ErrorOffset,
            status.ErrorLength,
            LimitGuard.OutputLimitMessage);
    }

    internal static UriTextResult CreateFailure(UriDiagnostic diagnostic)
    {
        return new UriTextResult(false, "", 0, diagnostic);
    }
}
