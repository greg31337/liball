using System;

namespace uri;

internal static class IriNormalizerCore
{
    internal const string NormalizationRequiredMessage = "The IRI component is not in Unicode Normalization Form C.";
    internal const string InvalidNormalizationModeMessage = "The requested Unicode normalization mode is invalid.";

    internal static UriTextResult NormalizeIri(
        ReadOnlySpan<char> text,
        UnicodeNormalizationMode mode,
        OperationLimits? limits)
    {
        if (limits == null)
        {
            return IriConverterCore.CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        if (mode != UnicodeNormalizationMode.Preserve &&
            mode != UnicodeNormalizationMode.NormalizeNfc &&
            mode != UnicodeNormalizationMode.DiagnoseNfc)
        {
            return CreateModeFailure();
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            text.Length,
            limits,
            DiagnosticDomain.Iri);
        if (inputLimit != null)
        {
            return IriConverterCore.CreateFailure(inputLimit);
        }

        IriParseResult parsed = IriParserCore.ParseReference(text);
        if (!parsed.Success)
        {
            return IriConverterCore.CreateFailure(parsed.Diagnostic);
        }

        if (mode == UnicodeNormalizationMode.DiagnoseNfc)
        {
            UriDiagnostic? diagnostic = FindNormalizationDiagnostic(parsed.Reference);
            if (diagnostic != null)
            {
                return IriConverterCore.CreateFailure(diagnostic);
            }
        }

        if (mode == UnicodeNormalizationMode.NormalizeNfc)
        {
            return WriteNormalizedReference(
                parsed.Reference,
                limits.MaxOutputCharacters);
        }

        UriDiagnostic? outputLimit = LimitGuard.CheckOutputCharacters(
            text.Length,
            limits,
            DiagnosticDomain.Iri);
        if (outputLimit != null)
        {
            return IriConverterCore.CreateFailure(outputLimit);
        }

        return new UriTextResult(true, text.ToString(), text.Length, null);
    }

    internal static UriTextResult WriteNormalizedReference(
        IriReference reference,
        int maximumOutputCharacters)
    {
        TextOutput output = new TextOutput(maximumOutputCharacters);
        if (reference.HasScheme)
        {
            output.AppendText(reference.Scheme);
            output.AppendCharacter(':');
        }

        if (reference.HasAuthority)
        {
            output.AppendText("//");
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                AppendNfcComponent(authority.UserInfo, output);
                output.AppendCharacter('@');
            }

            bool ipLiteral = UriComposerCore.IsIpLiteral(authority.HostKind);
            if (ipLiteral)
            {
                output.AppendCharacter('[');
            }

            if (authority.HostKind == UriHostKind.RegisteredName)
            {
                AppendNfcComponent(authority.Host, output);
            }
            else
            {
                output.AppendText(authority.Host);
            }

            if (ipLiteral)
            {
                output.AppendCharacter(']');
            }

            if (authority.HasPort)
            {
                output.AppendCharacter(':');
                output.AppendText(authority.Port);
            }
        }

        AppendNfcComponent(reference.Path, output);
        if (reference.HasQuery)
        {
            output.AppendCharacter('?');
            AppendNfcComponent(reference.Query, output);
        }

        if (reference.HasFragment)
        {
            output.AppendCharacter('#');
            AppendNfcComponent(reference.Fragment, output);
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return IriConverterCore.CreateOutputFailure(
                status.ErrorOffset,
                status.ErrorLength);
        }

        string normalizedText = output.GetText();
        IriParseResult parsed = IriParserCore.ParseReference(
            normalizedText.AsSpan());
        if (!parsed.Success)
        {
            return IriConverterCore.CreateFailure(parsed.Diagnostic);
        }

        return new UriTextResult(
            true,
            normalizedText,
            normalizedText.Length,
            null);
    }

    internal static void AppendNfcComponent(string text, TextOutput output)
    {
        string normalized = text.Normalize(System.Text.NormalizationForm.FormC);
        output.AppendText(normalized);
    }

    internal static UriDiagnostic? FindNormalizationDiagnostic(IriReference reference)
    {
        int offset = 0;
        if (reference.HasScheme)
        {
            offset = reference.Scheme.Length + 1;
        }

        if (reference.HasAuthority)
        {
            offset = offset + 2;
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                if (!authority.UserInfo.IsNormalized(System.Text.NormalizationForm.FormC))
                {
                    return CreateNormalizationDiagnostic(
                        UriComponent.UserInfo,
                        offset,
                        authority.UserInfo.Length);
                }

                offset = offset + authority.UserInfo.Length + 1;
            }

            bool ipLiteral = UriComposerCore.IsIpLiteral(authority.HostKind);
            if (ipLiteral)
            {
                offset = offset + 1;
            }

            if (authority.HostKind == UriHostKind.RegisteredName &&
                !authority.Host.IsNormalized(System.Text.NormalizationForm.FormC))
            {
                return CreateNormalizationDiagnostic(
                    UriComponent.Host,
                    offset,
                    authority.Host.Length);
            }

            offset = offset + authority.Host.Length;
            if (ipLiteral)
            {
                offset = offset + 1;
            }

            if (authority.HasPort)
            {
                offset = offset + authority.Port.Length + 1;
            }
        }

        if (!reference.Path.IsNormalized(System.Text.NormalizationForm.FormC))
        {
            return CreateNormalizationDiagnostic(
                UriComponent.Path,
                offset,
                reference.Path.Length);
        }

        offset = offset + reference.Path.Length;
        if (reference.HasQuery)
        {
            offset = offset + 1;
            if (!reference.Query.IsNormalized(System.Text.NormalizationForm.FormC))
            {
                return CreateNormalizationDiagnostic(
                    UriComponent.Query,
                    offset,
                    reference.Query.Length);
            }

            offset = offset + reference.Query.Length;
        }

        if (reference.HasFragment)
        {
            offset = offset + 1;
            if (!reference.Fragment.IsNormalized(System.Text.NormalizationForm.FormC))
            {
                return CreateNormalizationDiagnostic(
                    UriComponent.Fragment,
                    offset,
                    reference.Fragment.Length);
            }
        }

        return null;
    }

    internal static UriDiagnostic CreateNormalizationDiagnostic(
        UriComponent component,
        int offset,
        int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.NormalizationRequired,
            component,
            offset,
            length,
            NormalizationRequiredMessage);
    }

    internal static UriTextResult CreateModeFailure()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            InvalidNormalizationModeMessage);
        return IriConverterCore.CreateFailure(diagnostic);
    }
}
