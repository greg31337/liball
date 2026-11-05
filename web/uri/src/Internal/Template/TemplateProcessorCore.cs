using System;

namespace uri;

internal static class TemplateProcessorCore
{
    internal static UriParseResult ExpandToUri(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriParseOptions parseOptions)
    {
        UriTemplateExpansionResult expansion =
            TemplateExpanderCore.ExpandResult(
                template,
                bindings,
                expansionOptions);
        if (!expansion.Success)
        {
            return CreateParseFailure(expansion.Diagnostic);
        }

        if (parseOptions == null || parseOptions.Limits == null)
        {
            return CreateParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            expansion.Length,
            parseOptions.Limits,
            DiagnosticDomain.Uri);
        if (inputLimit != null)
        {
            return CreateParseFailure(inputLimit);
        }

        return UriParserCore.ParseReference(expansion.Text.AsSpan());
    }

    internal static UriTextResult ExpandAndResolve(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        string baseUri,
        UriResolutionOptions resolutionOptions)
    {
        UriTemplateExpansionResult expansion =
            TemplateExpanderCore.ExpandResult(
                template,
                bindings,
                expansionOptions);
        if (!expansion.Success)
        {
            return CreateTextFailure(expansion.Diagnostic);
        }

        UriResolutionCoreResult resolution =
            UriResolverCore.ResolveReferenceText(
                baseUri,
                expansion.Text,
                resolutionOptions);
        if (!resolution.Success)
        {
            return CreateTextFailure(resolution.Diagnostic);
        }

        return new UriTextResult(
            true,
            resolution.Text,
            resolution.Length,
            null);
    }

    internal static UriTextResult ExpandAndNormalize(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriNormalizationOptions normalizationOptions)
    {
        UriTemplateExpansionResult expansion =
            TemplateExpanderCore.ExpandResult(
                template,
                bindings,
                expansionOptions);
        if (!expansion.Success)
        {
            return CreateTextFailure(expansion.Diagnostic);
        }

        if (normalizationOptions == null ||
            normalizationOptions.Limits == null ||
            (normalizationOptions.Profile != null &&
                !UriNormalizerCore.IsValidSchemeProfile(
                    normalizationOptions.Profile)))
        {
            return CreateTextFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = normalizationOptions.Limits;
        UriValidationResult validation = UriValidatorCore.ValidateReference(
            expansion.Text.AsSpan(),
            validationOptions);
        if (!validation.Success)
        {
            return CreateTextFailure(validation.Diagnostic);
        }

        UriReference normalized;
        if (normalizationOptions.Profile == null)
        {
            normalized = UriNormalizerCore.NormalizeSyntaxReference(
                validation.Reference);
        }
        else
        {
            normalized = UriNormalizerCore.NormalizeWithSchemeProfile(
                validation.Reference,
                normalizationOptions.Profile);
        }

        UriCompositionResult composition =
            UriComposerCore.RecomposeParsedReference(
                normalized,
                normalizationOptions.Limits);
        if (!composition.Success)
        {
            return CreateTextFailure(composition.Diagnostic);
        }

        return new UriTextResult(
            true,
            composition.Text,
            composition.Length,
            null);
    }

    internal static UriParseResult CreateParseFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriParseResult(false, new UriReference(), failure);
    }

    internal static UriTextResult CreateTextFailure(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriTextResult(false, "", 0, failure);
    }
}
