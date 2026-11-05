using System;

namespace uri;

/// <summary>
/// Provides bounded RFC 6570 URI Template parsing.
/// </summary>
public class UriTemplateParser : IUriTemplateParser
{
    /// <summary>Initializes a stateless URI Template parser.</summary>
    public UriTemplateParser()
    {
    }

    /// <summary>Parses a URI Template and throws when parsing fails.</summary>
    /// <param name="text">The complete URI Template text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed reusable template model.</returns>
    /// <exception cref="UriTemplateSyntaxException">Syntax or a configured limit fails.</exception>
    public UriTemplate ParseTemplate(
        string text,
        UriTemplateParseOptions options)
    {
        UriTemplateParseResult result = TryParseTemplate(text, options);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Template;
    }

    /// <summary>Parses a URI Template without throwing for expected failure.</summary>
    /// <param name="text">The complete URI Template text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriTemplateParseResult TryParseTemplate(
        string text,
        UriTemplateParseOptions options)
    {
        if (text == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate));
        }

        return TryParseTemplateSpan(text.AsSpan(), options);
    }

    /// <summary>Parses a URI Template span without requiring a source string.</summary>
    /// <param name="text">The complete URI Template span.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsing outcome.</returns>
    public UriTemplateParseResult TryParseTemplateSpan(
        ReadOnlySpan<char> text,
        UriTemplateParseOptions options)
    {
        UriDiagnostic? precondition = CheckParsePreconditions(
            text.Length,
            options);
        if (precondition != null)
        {
            return CreateParseFailure(precondition);
        }

        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(text);
        if (!result.Success)
        {
            return result;
        }

        UriDiagnostic? modelLimit = CheckModelLimits(
            result.Template,
            options.Limits);
        if (modelLimit != null)
        {
            return CreateParseFailure(modelLimit);
        }

        return result;
    }

    internal UriDiagnostic? CheckParsePreconditions(
        int inputLength,
        UriTemplateParseOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.UriTemplate);
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            inputLength,
            options.Limits,
            DiagnosticDomain.UriTemplate);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        UriDiagnostic? expressionLimit = LimitGuard.CheckTemplateExpressions(
            0,
            options.Limits,
            0);
        if (expressionLimit != null)
        {
            return expressionLimit;
        }

        return LimitGuard.CheckTemplateVariables(0, options.Limits, 0);
    }

    internal UriDiagnostic? CheckModelLimits(
        UriTemplate template,
        OperationLimits limits)
    {
        int expressionCount = 0;
        int variableCount = 0;
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            if (part.Kind == UriTemplatePartKind.Expression)
            {
                expressionCount = expressionCount + 1;
                UriDiagnostic? expressionLimit =
                    LimitGuard.CheckTemplateExpressions(
                        expressionCount,
                        limits,
                        part.Range.Offset);
                if (expressionLimit != null)
                {
                    return expressionLimit;
                }

                int variableIndex = 0;
                while (variableIndex < part.Variables.Length)
                {
                    variableCount = variableCount + 1;
                    UriDiagnostic? variableLimit =
                        LimitGuard.CheckTemplateVariables(
                            variableCount,
                            limits,
                            part.Variables[variableIndex].Range.Offset);
                    if (variableLimit != null)
                    {
                        return variableLimit;
                    }

                    variableIndex = variableIndex + 1;
                }
            }

            partIndex = partIndex + 1;
        }

        return null;
    }

    internal UriTemplateParseResult CreateParseFailure(
        UriDiagnostic diagnostic)
    {
        return new UriTemplateParseResult(
            false,
            new UriTemplate(),
            diagnostic);
    }

    internal UriTemplateSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.UriTemplate);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriTemplateSyntaxException(failure);
    }
}
