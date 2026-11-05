using System;

namespace uri;

internal static class WebLinkIriCore
{
    internal const string InvalidTargetIriMessage = "The Web Link target is not a valid IRI-reference.";
    internal const string InvalidContextIriMessage = "The Web Link context is not a valid IRI-reference.";

    internal static UriTextResult ConvertTargetIriToHttpUri(
        string? text,
        OperationLimits? limits,
        int baseOffset)
    {
        return ConvertIriReference(
            text,
            limits,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            baseOffset,
            InvalidTargetIriMessage);
    }

    internal static UriTextResult ConvertContextIriToHttpUri(
        string? text,
        OperationLimits? limits,
        int baseOffset)
    {
        return ConvertIriReference(
            text,
            limits,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkContext,
            baseOffset,
            InvalidContextIriMessage);
    }

    internal static UriTextResult ConvertTargetUriToIriView(
        string? text,
        OperationLimits? limits)
    {
        return ConvertUriReference(
            text,
            limits,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            InvalidTargetIriMessage);
    }

    internal static UriTextResult ConvertContextUriToIriView(
        string? text,
        OperationLimits? limits)
    {
        return ConvertUriReference(
            text,
            limits,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.LinkContext,
            InvalidContextIriMessage);
    }

    internal static UriTextResult ConvertIriReference(
        string? text,
        OperationLimits? limits,
        UriDiagnosticCode invalidCode,
        UriComponent component,
        int baseOffset,
        string invalidMessage)
    {
        if (text == null || limits == null)
        {
            return CreateConversionFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        UriTextResult converted = IriConverterCore.ConvertIriToUri(
            text.AsSpan(),
            limits);
        return MapConversionResult(
            converted,
            invalidCode,
            component,
            baseOffset,
            invalidMessage);
    }

    internal static UriTextResult ConvertUriReference(
        string? text,
        OperationLimits? limits,
        UriDiagnosticCode invalidCode,
        UriComponent component,
        string invalidMessage)
    {
        if (text == null || limits == null)
        {
            return CreateConversionFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        UriTextResult converted = IriConverterCore.ConvertUriToIri(
            text.AsSpan(),
            limits);
        return MapConversionResult(
            converted,
            invalidCode,
            component,
            0,
            invalidMessage);
    }

    internal static UriTextResult MapConversionResult(
        UriTextResult converted,
        UriDiagnosticCode invalidCode,
        UriComponent component,
        int baseOffset,
        string invalidMessage)
    {
        if (converted.Success)
        {
            return converted;
        }

        UriDiagnostic? source = converted.Diagnostic;
        if (source == null)
        {
            return CreateConversionFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        UriDiagnosticCode code = invalidCode;
        UriComponent mappedComponent = component;
        string message = invalidMessage;
        if (source.Code == UriDiagnosticCode.LimitExceeded ||
            source.Code == UriDiagnosticCode.InvalidUnicode)
        {
            code = source.Code;
            message = source.Message;
            if (source.Component == UriComponent.Output)
            {
                mappedComponent = UriComponent.Output;
            }
        }

        int offset = baseOffset;
        if (source.Range.Offset <= int.MaxValue - baseOffset)
        {
            offset = baseOffset + source.Range.Offset;
        }

        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            code,
            mappedComponent,
            offset,
            source.Range.Length,
            message);
        return CreateConversionFailure(diagnostic);
    }

    internal static UriTextResult CreateConversionFailure(
        UriDiagnostic diagnostic)
    {
        return new UriTextResult(false, "", 0, diagnostic);
    }
}
