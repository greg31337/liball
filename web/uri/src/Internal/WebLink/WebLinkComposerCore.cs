using System;

namespace uri;

internal static class WebLinkComposerCore
{
    internal const string InvalidModelMessage = "The Web Link model is inconsistent.";

    internal static UriTextResult ComposeLinkFieldValue(
        WebLinkSet? linkSet,
        OperationLimits? limits)
    {
        if (linkSet == null || limits == null || linkSet.Links == null)
        {
            return CreateCompositionFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        UriDiagnostic? linkLimit = LimitGuard.CheckWebLinks(
            0,
            limits,
            0);
        if (linkLimit != null)
        {
            return CreateCompositionFailure(linkLimit);
        }

        UriDiagnostic? parameterLimit = LimitGuard.CheckWebLinkParameters(
            0,
            limits,
            0);
        if (parameterLimit != null)
        {
            return CreateCompositionFailure(parameterLimit);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        UriDiagnostic? outputFailure = GetOutputFailure(output);
        if (outputFailure != null)
        {
            return CreateCompositionFailure(outputFailure);
        }

        int parameterCount = 0;
        int linkIndex = 0;
        while (linkIndex < linkSet.Links.Length)
        {
            WebLink? link = linkSet.Links[linkIndex];
            linkLimit = LimitGuard.CheckWebLinks(
                linkIndex + 1,
                limits,
                output.GetLength());
            if (linkLimit != null)
            {
                return CreateCompositionFailure(linkLimit);
            }

            if (link == null || link.Target == null ||
                link.Parameters == null)
            {
                return CreateCompositionFailure(CreateModelFailure(
                    UriComponent.WebLink,
                    output.GetLength()));
            }

            if (linkIndex > 0)
            {
                output.AppendText(", ");
            }

            int targetOffset = output.GetLength() + 1;
            UriTextResult target = PrepareTarget(
                link.Target,
                limits,
                targetOffset);
            if (!target.Success)
            {
                return CreateCompositionFailure(
                    target.Diagnostic ??
                        DiagnosticFactory.CreateInvalidInput(
                            DiagnosticDomain.WebLink));
            }

            output.AppendCharacter('<');
            output.AppendText(target.Text);
            output.AppendCharacter('>');
            outputFailure = GetOutputFailure(output);
            if (outputFailure != null)
            {
                return CreateCompositionFailure(outputFailure);
            }

            int parameterIndex = 0;
            while (parameterIndex < link.Parameters.Length)
            {
                WebLinkParameter? parameter =
                    link.Parameters[parameterIndex];
                parameterCount = parameterCount + 1;
                parameterLimit = LimitGuard.CheckWebLinkParameters(
                    parameterCount,
                    limits,
                    output.GetLength());
                if (parameterLimit != null)
                {
                    return CreateCompositionFailure(parameterLimit);
                }

                UriDiagnostic? parameterFailure = AppendParameter(
                    parameter,
                    limits,
                    output);
                if (parameterFailure != null)
                {
                    return CreateCompositionFailure(parameterFailure);
                }

                parameterIndex = parameterIndex + 1;
            }

            linkIndex = linkIndex + 1;
        }

        string text = output.GetText();
        return new UriTextResult(true, text, text.Length, null);
    }

    internal static UriMeasureResult MeasureLinkFieldValue(
        WebLinkSet? linkSet,
        OperationLimits? limits)
    {
        UriTextResult composition = ComposeLinkFieldValue(linkSet, limits);
        if (!composition.Success)
        {
            return new UriMeasureResult(
                false,
                0,
                composition.Diagnostic);
        }

        return new UriMeasureResult(true, composition.Length, null);
    }

    internal static UriTextResult PrepareTarget(
        string target,
        OperationLimits limits,
        int targetOffset)
    {
        UriTextResult converted =
            WebLinkIriCore.ConvertTargetIriToHttpUri(
                target,
                limits,
                targetOffset);
        if (!converted.Success)
        {
            return converted;
        }

        UriDiagnostic? targetLimit = LimitGuard.CheckComponentCharacters(
            converted.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkTarget,
            targetOffset);
        if (targetLimit != null)
        {
            return CreateCompositionFailure(targetLimit);
        }

        return converted;
    }

    internal static UriDiagnostic? AppendParameter(
        WebLinkParameter? parameter,
        OperationLimits limits,
        TextOutput output)
    {
        int parameterOffset = output.GetLength();
        if (parameter == null || parameter.Name == null ||
            parameter.Value == null || parameter.Language == null)
        {
            return CreateModelFailure(
                UriComponent.LinkParameter,
                parameterOffset);
        }

        int nameOffset = parameterOffset + 2;
        if (!IsToken(parameter.Name))
        {
            return CreateParameterFailure(nameOffset, 0);
        }

        UriDiagnostic? nameLimit = LimitGuard.CheckComponentCharacters(
            parameter.Name.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkParameter,
            nameOffset);
        if (nameLimit != null)
        {
            return nameLimit;
        }

        bool nameIsExtended = parameter.Name[
            parameter.Name.Length - 1] == '*';
        if (parameter.Extended != nameIsExtended ||
            (!parameter.HasValue &&
                (parameter.Value.Length > 0 ||
                 parameter.Language.Length > 0)))
        {
            return CreateModelFailure(
                UriComponent.LinkParameter,
                nameOffset);
        }

        output.AppendText("; ");
        output.AppendText(parameter.Name);
        if (!parameter.HasValue)
        {
            return GetOutputFailure(output);
        }

        output.AppendCharacter('=');
        UriDiagnostic? outputFailure = GetOutputFailure(output);
        if (outputFailure != null)
        {
            return outputFailure;
        }

        int valueOffset = output.GetLength();
        string serializedValue = parameter.Value;
        if (AsciiCase.EqualsIgnoreCase(parameter.Name, "anchor"))
        {
            UriTextResult convertedAnchor =
                WebLinkIriCore.ConvertContextIriToHttpUri(
                    parameter.Value,
                    limits,
                    valueOffset);
            if (!convertedAnchor.Success)
            {
                return convertedAnchor.Diagnostic ??
                    DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.WebLink);
            }

            serializedValue = convertedAnchor.Text;
        }

        UriDiagnostic? valueLimit = LimitGuard.CheckComponentCharacters(
            serializedValue.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkParameter,
            valueOffset);
        if (valueLimit != null)
        {
            return valueLimit;
        }

        if (parameter.Extended)
        {
            int remaining = limits.MaxOutputCharacters - valueOffset;
            PercentEncodeResult encoded =
                WebLinkExtendedValueCore.EncodeUtf8Value(
                    parameter.Value,
                    parameter.Language,
                    remaining);
            if (!encoded.Success)
            {
                return MapExtendedFailure(
                    encoded.Diagnostic,
                    valueOffset,
                    parameter.Language.Length);
            }

            output.AppendText(encoded.Text);
            return GetOutputFailure(output);
        }

        if (IsToken(serializedValue))
        {
            output.AppendText(serializedValue);
            return GetOutputFailure(output);
        }

        return AppendQuotedValue(serializedValue, valueOffset, output);
    }

    internal static UriDiagnostic? AppendQuotedValue(
        string value,
        int valueOffset,
        TextOutput output)
    {
        output.AppendCharacter('"');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '"' || character == '\\')
            {
                output.AppendCharacter('\\');
                output.AppendCharacter(character);
            }
            else if (HttpQuotedString.IsQuotedTextCharacter(character))
            {
                output.AppendCharacter(character);
            }
            else
            {
                return CreateParameterFailure(
                    valueOffset + 1 + index,
                    1);
            }

            UriDiagnostic? outputFailure = GetOutputFailure(output);
            if (outputFailure != null)
            {
                return outputFailure;
            }

            index = index + 1;
        }

        output.AppendCharacter('"');
        return GetOutputFailure(output);
    }

    internal static bool IsToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!WebLinkGrammar.IsTokenCharacter(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static UriDiagnostic MapExtendedFailure(
        UriDiagnostic? diagnostic,
        int valueOffset,
        int languageLength)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        }

        int prefixOffset = valueOffset;
        if (diagnostic.Component == UriComponent.LanguageTag)
        {
            prefixOffset = valueOffset + 6;
        }
        else if (diagnostic.Component == UriComponent.ExtendedParameter)
        {
            prefixOffset = valueOffset + 7 + languageLength;
        }

        int offset = prefixOffset;
        if (diagnostic.Range.Offset <= int.MaxValue - prefixOffset)
        {
            offset = prefixOffset + diagnostic.Range.Offset;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            diagnostic.Code,
            diagnostic.Component,
            offset,
            diagnostic.Range.Length,
            diagnostic.Message);
    }

    internal static UriDiagnostic? GetOutputFailure(TextOutput output)
    {
        TextOutputResult status = output.GetStatus();
        if (status.Success)
        {
            return null;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            status.ErrorOffset,
            status.ErrorLength,
            LimitGuard.OutputLimitMessage);
    }

    internal static UriDiagnostic CreateParameterFailure(
        int offset,
        int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            offset,
            length,
            WebLinkParserCore.InvalidParameterMessage);
    }

    internal static UriDiagnostic CreateModelFailure(
        UriComponent component,
        int offset)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidWebLink,
            component,
            offset,
            0,
            InvalidModelMessage);
    }

    internal static UriTextResult CreateCompositionFailure(
        UriDiagnostic diagnostic)
    {
        return new UriTextResult(false, "", 0, diagnostic);
    }
}
