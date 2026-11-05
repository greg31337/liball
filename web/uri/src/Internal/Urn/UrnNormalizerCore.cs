using System;

namespace uri;

internal static class UrnNormalizerCore
{
    internal static UriTextResult NormalizeUrn(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? precondition =
            ValidateCompleteUrnPreconditions(urn, limits);
        if (precondition != null || urn == null || limits == null)
        {
            return CreateFailure(precondition);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        AppendNormalizedAssignedName(urn, output);
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
            return CreateFailure(CreateOutputDiagnostic(status));
        }

        string text = output.GetText();
        UrnParseResult validation = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);
        if (!validation.Success)
        {
            return CreateFailure(validation.Diagnostic);
        }

        return new UriTextResult(true, text, text.Length, null);
    }

    internal static UriTextResult NormalizeAssignedName(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? precondition =
            ValidateAssignedNamePreconditions(urn, limits);
        if (precondition != null || urn == null || limits == null)
        {
            return CreateFailure(precondition);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        AppendNormalizedAssignedName(urn, output);
        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return CreateFailure(CreateOutputDiagnostic(status));
        }

        string text = output.GetText();
        UrnAssignedNameParseResult validation =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);
        if (!validation.Success || validation.NextOffset != text.Length)
        {
            return CreateFailure(validation.Diagnostic);
        }

        return new UriTextResult(true, text, text.Length, null);
    }

    internal static void AppendNormalizedAssignedName(
        Urn urn,
        TextOutput output)
    {
        AppendLowercaseAscii(urn.Scheme, output);
        output.AppendCharacter(':');
        AppendLowercaseAscii(urn.NamespaceIdentifier, output);
        output.AppendCharacter(':');
        AppendNormalizedNamespaceSpecificString(
            urn.NamespaceSpecificString,
            output);
    }

    internal static void AppendLowercaseAscii(
        string text,
        TextOutput output)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            output.AppendCharacter(AsciiCase.FoldToLower(text[offset]));
            offset = offset + 1;
        }
    }

    internal static void AppendNormalizedNamespaceSpecificString(
        string text,
        TextOutput output)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%' &&
                offset + 2 < text.Length &&
                Ascii.IsHexDigit(text[offset + 1]) &&
                Ascii.IsHexDigit(text[offset + 2]))
            {
                output.AppendCharacter('%');
                output.AppendCharacter(AsciiCase.FoldToUpper(
                    text[offset + 1]));
                output.AppendCharacter(AsciiCase.FoldToUpper(
                    text[offset + 2]));
                offset = offset + 3;
                continue;
            }

            output.AppendCharacter(text[offset]);
            offset = offset + 1;
        }
    }

    internal static UriDiagnostic? ValidateCompleteUrnPreconditions(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? assignedName = ValidateAssignedNamePreconditions(
            urn,
            limits);
        if (assignedName != null)
        {
            return assignedName;
        }

        if (urn == null ||
            urn.RComponent == null ||
            urn.QComponent == null ||
            urn.FComponent == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return null;
    }

    internal static UriDiagnostic? ValidateAssignedNamePreconditions(
        Urn? urn,
        OperationLimits? limits)
    {
        if (urn == null || limits == null ||
            urn.Scheme == null ||
            urn.NamespaceIdentifier == null ||
            urn.NamespaceSpecificString == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return null;
    }

    internal static UriTextResult CreateFailure(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        return new UriTextResult(false, "", 0, failure);
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
}
