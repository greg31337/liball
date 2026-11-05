using System;

namespace uri;

internal static class UrnModelConverterCore
{
    internal static UriParseResult ConvertUrnToUriReference(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? precondition = ValidateUrnModelPreconditions(
            urn,
            limits);
        if (precondition != null || urn == null || limits == null)
        {
            return CreateUriFailure(precondition);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        output.AppendText(urn.Scheme);
        output.AppendCharacter(':');
        output.AppendText(urn.NamespaceIdentifier);
        output.AppendCharacter(':');
        output.AppendText(urn.NamespaceSpecificString);
        if (urn.HasRComponent)
        {
            output.AppendText("?+");
            output.AppendText(urn.RComponent);
        }

        if (urn.HasQComponent)
        {
            output.AppendText("?=");
            output.AppendText(urn.QComponent);
        }

        if (urn.HasFComponent)
        {
            output.AppendCharacter('#');
            output.AppendText(urn.FComponent);
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return CreateUriFailure(CreateOutputDiagnostic(status));
        }

        string text = output.GetText();
        UrnParseResult validation = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);
        if (!validation.Success)
        {
            return CreateUriFailure(validation.Diagnostic);
        }

        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        if (!result.Success)
        {
            return CreateUriFailure(ConvertDiagnostic(result.Diagnostic));
        }

        return result;
    }

    internal static UrnParseResult ConvertUriReferenceToUrn(
        UriReference? reference,
        UrnValidationMode validationMode,
        OperationLimits? limits)
    {
        if (reference == null || limits == null)
        {
            return UrnParserCore.CreateUrnFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        UriCompositionResult composition =
            UriComposerCore.RecomposeParsedReference(reference, limits);
        if (!composition.Success)
        {
            return UrnParserCore.CreateUrnFailure(
                ConvertDiagnostic(composition.Diagnostic));
        }

        return UrnParserCore.ParseUrn(
            composition.Text.AsSpan(),
            validationMode);
    }

    internal static UriDiagnostic? ValidateUrnModelPreconditions(
        Urn? urn,
        OperationLimits? limits)
    {
        if (urn == null || limits == null ||
            urn.Scheme == null ||
            urn.NamespaceIdentifier == null ||
            urn.NamespaceSpecificString == null ||
            urn.RComponent == null ||
            urn.QComponent == null ||
            urn.FComponent == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return null;
    }

    internal static UriParseResult CreateUriFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UriParseResult(false, new UriReference(), failure);
    }

    internal static UriDiagnostic CreateOutputDiagnostic(
        TextOutputResult status)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            status.ErrorOffset,
            status.ErrorLength,
            LimitGuard.OutputLimitMessage);
    }

    internal static UriDiagnostic ConvertDiagnostic(
        UriDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            diagnostic.Code,
            diagnostic.Component,
            diagnostic.Range.Offset,
            diagnostic.Range.Length,
            diagnostic.Message);
    }
}
