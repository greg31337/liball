using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkRecoveryParserCore
{
    internal const string DiscardedRemainderMessage = "Recovery stopped at text that does not begin a Web Link value and discarded the field remainder.";
    internal const string TruncatedTargetMessage = "Recovery discarded a Web Link value whose target has no closing angle bracket.";
    internal const string InvalidTargetMessage = "Recovery discarded a Web Link value whose target is not a valid URI-reference.";
    internal const string MissingSeparatorMessage = "Recovery accepted adjacent Web Link values without the required comma separator.";
    internal const string InvalidParameterNameMessage = "Recovery retained a Web Link parameter whose name is not a valid HTTP token.";
    internal const string InvalidParameterValueMessage = "Recovery retained a Web Link parameter whose unquoted value is not a valid HTTP token.";
    internal const string InvalidQuotedContentMessage = "Recovery retained a quoted Web Link parameter containing invalid HTTP quoted text.";
    internal const string UnterminatedQuotedStringMessage = "Recovery retained a quoted Web Link parameter without a closing quote.";
    internal const string IncompleteQuotedPairMessage = "Recovery retained a quoted Web Link parameter ending with an incomplete escape.";

    internal static WebLinkParseResult ParseRecoveryFieldValueSpan(
        ReadOnlySpan<char> fieldValue,
        WebLinkParseOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateRecoveryOptions(options);
        if (optionFailure != null || options == null)
        {
            return WebLinkParserCore.CreateParseFailure(optionFailure);
        }

        UriDiagnostic? inputFailure = LimitGuard.CheckInputCharacters(
            fieldValue.Length,
            options.Limits,
            DiagnosticDomain.WebLink);
        if (inputFailure != null)
        {
            return WebLinkParserCore.CreateParseFailure(inputFailure);
        }

        WebLinkRecoveryFieldResult parsed = ParseRecoveryCombinedField(
            fieldValue,
            options.Limits,
            0);
        if (!parsed.Success)
        {
            return CreateRecoveryFailure(
                parsed.Diagnostic,
                parsed.Diagnostics);
        }

        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = parsed.Links;
        linkSet.OriginalFieldValues = new string[]
        {
            fieldValue.ToString()
        };
        linkSet.Diagnostics = parsed.Diagnostics;
        return CreateRecoverySuccess(linkSet, parsed.Diagnostics);
    }

    internal static WebLinkParseResult ParseRecoveryFieldValues(
        string[]? fieldValues,
        WebLinkParseOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateRecoveryOptions(options);
        if (optionFailure != null || options == null || fieldValues == null)
        {
            UriDiagnostic failure = optionFailure ??
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink);
            return WebLinkParserCore.CreateParseFailure(failure);
        }

        List<WebLink> links = new List<WebLink>();
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        string[] originals = new string[fieldValues.Length];
        int totalInputCharacters = 0;
        int fieldIndex = 0;
        while (fieldIndex < fieldValues.Length)
        {
            string? fieldValue = fieldValues[fieldIndex];
            if (fieldValue == null)
            {
                return CreateRecoveryFailure(
                    DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.WebLink),
                    diagnostics.ToArray());
            }

            UriDiagnostic? inputFailure =
                WebLinkParserCore.CheckCombinedInputLimit(
                    totalInputCharacters,
                    fieldValue.Length,
                    options.Limits);
            if (inputFailure != null)
            {
                return CreateRecoveryFailure(
                    inputFailure,
                    diagnostics.ToArray());
            }

            totalInputCharacters = totalInputCharacters +
                fieldValue.Length;
            WebLinkRecoveryFieldResult parsed =
                ParseRecoveryCombinedField(
                    fieldValue.AsSpan(),
                    options.Limits,
                    links.Count);
            AddDiagnostics(diagnostics, parsed.Diagnostics);
            if (!parsed.Success)
            {
                return CreateRecoveryFailure(
                    parsed.Diagnostic,
                    diagnostics.ToArray());
            }

            int linkIndex = 0;
            while (linkIndex < parsed.Links.Length)
            {
                links.Add(parsed.Links[linkIndex]);
                linkIndex = linkIndex + 1;
            }

            originals[fieldIndex] = fieldValue;
            fieldIndex = fieldIndex + 1;
        }

        UriDiagnostic[] warnings = diagnostics.ToArray();
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = links.ToArray();
        linkSet.OriginalFieldValues = originals;
        linkSet.Diagnostics = warnings;
        return CreateRecoverySuccess(linkSet, warnings);
    }

    internal static WebLinkRecoveryFieldResult ParseRecoveryCombinedField(
        ReadOnlySpan<char> fieldValue,
        OperationLimits limits,
        int initialLinkCount)
    {
        List<WebLink> links = new List<WebLink>();
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int parameterCount = 0;
        int offset = 0;
        bool requiresSeparator = false;
        WebLinkScanResult whitespace =
            WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
        offset = whitespace.NextOffset;

        while (offset < fieldValue.Length)
        {
            if (fieldValue[offset] == ',')
            {
                offset = offset + 1;
                requiresSeparator = false;
                whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                    fieldValue,
                    offset,
                    0);
                offset = whitespace.NextOffset;
                continue;
            }

            if (fieldValue[offset] != '<')
            {
                diagnostics.Add(CreateRecoveryWarning(
                    UriDiagnosticCode.InvalidWebLink,
                    UriComponent.WebLink,
                    offset,
                    fieldValue.Length - offset,
                    DiscardedRemainderMessage));
                offset = fieldValue.Length;
                continue;
            }

            if (requiresSeparator)
            {
                diagnostics.Add(CreateRecoveryWarning(
                    UriDiagnosticCode.InvalidWebLink,
                    UriComponent.WebLink,
                    offset,
                    0,
                    MissingSeparatorMessage));
            }

            WebLinkRecoveryLinkResult parsedLink =
                ParseRecoveryLinkValue(
                    fieldValue,
                    offset,
                    limits);
            AddDiagnostics(diagnostics, parsedLink.Diagnostics);
            if (!parsedLink.Success)
            {
                return new WebLinkRecoveryFieldResult(
                    false,
                    new WebLink[0],
                    diagnostics.ToArray(),
                    parsedLink.Diagnostic);
            }

            int parameterIndex = 0;
            while (parameterIndex < parsedLink.Link.Parameters.Length)
            {
                parameterCount = parameterCount + 1;
                UriDiagnostic? parameterLimit =
                    LimitGuard.CheckWebLinkParameters(
                        parameterCount,
                        limits,
                        parsedLink.Link.Parameters[parameterIndex]
                            .Range.Offset);
                if (parameterLimit != null)
                {
                    return new WebLinkRecoveryFieldResult(
                        false,
                        new WebLink[0],
                        diagnostics.ToArray(),
                        parameterLimit);
                }

                parameterIndex = parameterIndex + 1;
            }

            if (parsedLink.Included)
            {
                UriDiagnostic? linkLimit = LimitGuard.CheckWebLinks(
                    initialLinkCount + links.Count + 1,
                    limits,
                    parsedLink.Link.Range.Offset);
                if (linkLimit != null)
                {
                    return new WebLinkRecoveryFieldResult(
                        false,
                        new WebLink[0],
                        diagnostics.ToArray(),
                        linkLimit);
                }

                links.Add(parsedLink.Link);
            }

            offset = parsedLink.NextOffset;
            requiresSeparator = true;
            whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
            offset = whitespace.NextOffset;
        }

        return new WebLinkRecoveryFieldResult(
            true,
            links.ToArray(),
            diagnostics.ToArray(),
            null);
    }

    internal static WebLinkRecoveryLinkResult ParseRecoveryLinkValue(
        ReadOnlySpan<char> fieldValue,
        int startOffset,
        OperationLimits limits)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int targetEnd = startOffset + 1;
        while (targetEnd < fieldValue.Length &&
            fieldValue[targetEnd] != '>')
        {
            targetEnd = targetEnd + 1;
        }

        if (targetEnd == fieldValue.Length)
        {
            diagnostics.Add(CreateRecoveryWarning(
                UriDiagnosticCode.InvalidLinkTarget,
                UriComponent.LinkTarget,
                startOffset,
                fieldValue.Length - startOffset,
                TruncatedTargetMessage));
            WebLink truncated = new WebLink();
            truncated.Range = new TextRange(
                startOffset,
                fieldValue.Length - startOffset);
            return new WebLinkRecoveryLinkResult(
                true,
                false,
                truncated,
                fieldValue.Length,
                diagnostics.ToArray(),
                null);
        }

        string target = fieldValue.Slice(
            startOffset + 1,
            targetEnd - startOffset - 1).ToString();
        UriDiagnostic? targetLimit = LimitGuard.CheckComponentCharacters(
            target.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkTarget,
            startOffset + 1);
        if (targetLimit != null)
        {
            return CreateRecoveryLinkFailure(
                startOffset,
                diagnostics,
                targetLimit);
        }

        bool targetValid = true;
        UriParseResult targetValidation = UriParserCore.ParseReference(
            target.AsSpan());
        if (!targetValidation.Success)
        {
            targetValid = false;
            UriDiagnostic mapped = WebLinkParserCore.CreateTargetDiagnostic(
                startOffset + 1,
                targetValidation.Diagnostic);
            diagnostics.Add(CreateRecoveryWarning(
                mapped.Code,
                mapped.Component,
                mapped.Range.Offset,
                mapped.Range.Length,
                InvalidTargetMessage));
        }

        List<WebLinkParameter> parameters =
            new List<WebLinkParameter>();
        int offset = targetEnd + 1;
        int linkEndOffset = offset;
        bool scanningParameters = true;
        while (scanningParameters)
        {
            WebLinkScanResult whitespace =
                WebLinkGrammar.ScanOptionalWhitespace(
                    fieldValue,
                    offset,
                    0);
            offset = whitespace.NextOffset;
            if (offset >= fieldValue.Length ||
                fieldValue[offset] != ';')
            {
                scanningParameters = false;
                continue;
            }

            WebLinkRecoveryParameterResult parameter =
                ParseRecoveryParameter(fieldValue, offset, limits);
            AddDiagnostics(diagnostics, parameter.Diagnostics);
            if (!parameter.Success)
            {
                return CreateRecoveryLinkFailure(
                    startOffset,
                    diagnostics,
                    parameter.Diagnostic);
            }

            parameters.Add(parameter.Parameter);
            offset = parameter.NextOffset;
            linkEndOffset = offset;
        }

        WebLink link = new WebLink();
        link.Target = target;
        link.Parameters = parameters.ToArray();
        link.OriginalText = fieldValue.Slice(
            startOffset,
            linkEndOffset - startOffset).ToString();
        link.Range = new TextRange(
            startOffset,
            linkEndOffset - startOffset);
        UriDiagnostic[] duplicateDiagnostics =
            WebLinkDuplicateParameterCore.MarkIgnoredDuplicates(link);
        AddDiagnosticsInOffsetOrder(diagnostics, duplicateDiagnostics);
        UriDiagnostic[] extendedValueDiagnostics =
            WebLinkExtendedValueCore.InterpretRecoveryExtendedValues(link);
        AddDiagnosticsInOffsetOrder(
            diagnostics,
            extendedValueDiagnostics);
        UriDiagnostic[] relationDiagnostics =
            WebLinkRelationCore.InterpretRecoveryRelations(link);
        AddDiagnosticsInOffsetOrder(diagnostics, relationDiagnostics);
        UriDiagnostic[] attributeDiagnostics =
            WebLinkAttributeCore.InterpretRecoveryAttributes(link);
        AddDiagnosticsInOffsetOrder(diagnostics, attributeDiagnostics);
        return new WebLinkRecoveryLinkResult(
            true,
            targetValid,
            link,
            offset,
            diagnostics.ToArray(),
            null);
    }

    internal static WebLinkRecoveryParameterResult ParseRecoveryParameter(
        ReadOnlySpan<char> fieldValue,
        int startOffset,
        OperationLimits limits)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int offset = startOffset + 1;
        WebLinkScanResult whitespace =
            WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
        offset = whitespace.NextOffset;
        int nameOffset = offset;
        while (offset < fieldValue.Length &&
            !WebLinkGrammar.IsOptionalWhitespaceCharacter(
                fieldValue[offset]) &&
            fieldValue[offset] != '=' &&
            fieldValue[offset] != ';' &&
            fieldValue[offset] != ',')
        {
            offset = offset + 1;
        }

        string name = fieldValue.Slice(
            nameOffset,
            offset - nameOffset).ToString();
        int invalidNameOffset = FindInvalidTokenCharacter(name.AsSpan());
        if (name.Length == 0)
        {
            diagnostics.Add(CreateRecoveryWarning(
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter,
                nameOffset,
                0,
                InvalidParameterNameMessage));
        }
        else if (invalidNameOffset >= 0)
        {
            diagnostics.Add(CreateRecoveryWarning(
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter,
                nameOffset + invalidNameOffset,
                1,
                InvalidParameterNameMessage));
        }

        UriDiagnostic? nameLimit = LimitGuard.CheckComponentCharacters(
            name.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkParameter,
            nameOffset);
        if (nameLimit != null)
        {
            return CreateRecoveryParameterFailure(
                startOffset,
                diagnostics,
                nameLimit);
        }

        whitespace = WebLinkGrammar.ScanOptionalWhitespace(
            fieldValue,
            offset,
            0);
        offset = whitespace.NextOffset;
        bool hasValue = false;
        bool wasQuoted = false;
        string value = "";
        int valueOffset = offset;
        if (offset < fieldValue.Length && fieldValue[offset] == '=')
        {
            hasValue = true;
            offset = offset + 1;
            whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
            offset = whitespace.NextOffset;
            valueOffset = offset;
            if (offset < fieldValue.Length && fieldValue[offset] == '"')
            {
                wasQuoted = true;
                WebLinkRecoveryTextResult quoted =
                    ScanRecoveryQuotedString(fieldValue, offset);
                value = quoted.Value;
                offset = quoted.NextOffset;
                AddDiagnostics(diagnostics, quoted.Diagnostics);
            }
            else
            {
                int valueEnd = offset;
                while (valueEnd < fieldValue.Length &&
                    fieldValue[valueEnd] != ';' &&
                    fieldValue[valueEnd] != ',')
                {
                    valueEnd = valueEnd + 1;
                }

                value = fieldValue.Slice(
                    offset,
                    valueEnd - offset).ToString();
                int invalidValueOffset =
                    FindInvalidTokenCharacter(value.AsSpan());
                if (value.Length == 0)
                {
                    diagnostics.Add(CreateRecoveryWarning(
                        UriDiagnosticCode.InvalidLinkParameter,
                        UriComponent.LinkParameter,
                        valueOffset,
                        0,
                        InvalidParameterValueMessage));
                }
                else if (invalidValueOffset >= 0)
                {
                    diagnostics.Add(CreateRecoveryWarning(
                        UriDiagnosticCode.InvalidLinkParameter,
                        UriComponent.LinkParameter,
                        valueOffset + invalidValueOffset,
                        1,
                        InvalidParameterValueMessage));
                }

                offset = valueEnd;
            }

            UriDiagnostic? valueLimit =
                LimitGuard.CheckComponentCharacters(
                    value.Length,
                    limits,
                    DiagnosticDomain.WebLink,
                    UriComponent.LinkParameter,
                    valueOffset);
            if (valueLimit != null)
            {
                return CreateRecoveryParameterFailure(
                    startOffset,
                    diagnostics,
                    valueLimit);
            }
        }

        WebLinkParameter parameter = new WebLinkParameter();
        parameter.Name = name;
        parameter.HasValue = hasValue;
        parameter.Value = value;
        parameter.RawValue = value;
        parameter.WasQuoted = wasQuoted;
        parameter.Extended = name.Length > 0 &&
            name[name.Length - 1] == '*';
        parameter.OriginalText = fieldValue.Slice(
            startOffset,
            offset - startOffset).ToString();
        parameter.Range = new TextRange(
            startOffset,
            offset - startOffset);
        return new WebLinkRecoveryParameterResult(
            true,
            parameter,
            offset,
            diagnostics.ToArray(),
            null);
    }

    internal static WebLinkRecoveryTextResult ScanRecoveryQuotedString(
        ReadOnlySpan<char> fieldValue,
        int startOffset)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        TextOutput output = new TextOutput(
            fieldValue.Length - startOffset);
        int offset = startOffset + 1;
        bool invalidContentReported = false;
        while (offset < fieldValue.Length)
        {
            char value = fieldValue[offset];
            if (value == '\\')
            {
                if (offset + 1 >= fieldValue.Length)
                {
                    diagnostics.Add(CreateRecoveryWarning(
                        UriDiagnosticCode.InvalidLinkParameter,
                        UriComponent.LinkParameter,
                        offset,
                        1,
                        IncompleteQuotedPairMessage));
                    return new WebLinkRecoveryTextResult(
                        output.GetText(),
                        fieldValue.Length,
                        diagnostics.ToArray());
                }

                char escaped = fieldValue[offset + 1];
                if (!HttpQuotedString.IsQuotedPairCharacter(escaped) &&
                    !invalidContentReported)
                {
                    diagnostics.Add(CreateRecoveryWarning(
                        UriDiagnosticCode.InvalidLinkParameter,
                        UriComponent.LinkParameter,
                        offset + 1,
                        1,
                        InvalidQuotedContentMessage));
                    invalidContentReported = true;
                }

                output.AppendCharacter(escaped);
                offset = offset + 2;
                continue;
            }

            if (value == '"')
            {
                return new WebLinkRecoveryTextResult(
                    output.GetText(),
                    offset + 1,
                    diagnostics.ToArray());
            }

            if (!HttpQuotedString.IsQuotedTextCharacter(value) &&
                !invalidContentReported)
            {
                diagnostics.Add(CreateRecoveryWarning(
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriComponent.LinkParameter,
                    offset,
                    1,
                    InvalidQuotedContentMessage));
                invalidContentReported = true;
            }

            output.AppendCharacter(value);
            offset = offset + 1;
        }

        diagnostics.Add(CreateRecoveryWarning(
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            fieldValue.Length,
            0,
            UnterminatedQuotedStringMessage));
        return new WebLinkRecoveryTextResult(
            output.GetText(),
            fieldValue.Length,
            diagnostics.ToArray());
    }

    internal static int FindInvalidTokenCharacter(
        ReadOnlySpan<char> value)
    {
        int offset = 0;
        while (offset < value.Length)
        {
            if (!WebLinkGrammar.IsTokenCharacter(value[offset]))
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static UriDiagnostic? ValidateRecoveryOptions(
        WebLinkParseOptions? options)
    {
        if (options == null ||
            options.Limits == null ||
            options.Mode != WebLinkParseMode.Recovery)
        {
            return DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        }

        UriDiagnostic? diagnostic = LimitGuard.CheckInputCharacters(
            0,
            options.Limits,
            DiagnosticDomain.WebLink);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        diagnostic = LimitGuard.CheckComponentCharacters(
            0,
            options.Limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkTarget,
            0);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        diagnostic = LimitGuard.CheckWebLinks(
            0,
            options.Limits,
            0);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        return LimitGuard.CheckWebLinkParameters(
            0,
            options.Limits,
            0);
    }

    internal static UriDiagnostic CreateRecoveryWarning(
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateWarning(
            DiagnosticDomain.WebLink,
            code,
            component,
            offset,
            length,
            message);
    }

    internal static void AddDiagnostics(
        List<UriDiagnostic> destination,
        UriDiagnostic[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void AddDiagnosticsInOffsetOrder(
        List<UriDiagnostic> destination,
        UriDiagnostic[] source)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            UriDiagnostic diagnostic = source[sourceIndex];
            int destinationIndex = 0;
            while (destinationIndex < destination.Count &&
                destination[destinationIndex].Range.Offset <=
                    diagnostic.Range.Offset)
            {
                destinationIndex = destinationIndex + 1;
            }

            destination.Insert(destinationIndex, diagnostic);
            sourceIndex = sourceIndex + 1;
        }
    }

    internal static WebLinkRecoveryParameterResult
        CreateRecoveryParameterFailure(
            int startOffset,
            List<UriDiagnostic> diagnostics,
            UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkRecoveryParameterResult(
            false,
            new WebLinkParameter(),
            startOffset,
            diagnostics.ToArray(),
            failure);
    }

    internal static WebLinkRecoveryLinkResult CreateRecoveryLinkFailure(
        int startOffset,
        List<UriDiagnostic> diagnostics,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        WebLink link = new WebLink();
        link.Range = new TextRange(startOffset, 0);
        return new WebLinkRecoveryLinkResult(
            false,
            false,
            link,
            startOffset,
            diagnostics.ToArray(),
            failure);
    }

    internal static WebLinkParseResult CreateRecoverySuccess(
        WebLinkSet linkSet,
        UriDiagnostic[] diagnostics)
    {
        return new WebLinkParseResult(
            true,
            linkSet,
            diagnostics.Length > 0,
            null,
            diagnostics);
    }

    internal static WebLinkParseResult CreateRecoveryFailure(
        UriDiagnostic? diagnostic,
        UriDiagnostic[] warnings)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        UriDiagnostic[] diagnostics = new UriDiagnostic[
            warnings.Length + 1];
        int index = 0;
        while (index < warnings.Length)
        {
            diagnostics[index] = warnings[index];
            index = index + 1;
        }

        diagnostics[warnings.Length] = failure;
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Diagnostics = warnings;
        return new WebLinkParseResult(
            false,
            linkSet,
            warnings.Length > 0,
            failure,
            diagnostics);
    }
}
