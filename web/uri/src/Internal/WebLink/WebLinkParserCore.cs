using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkParserCore
{
    internal const string InvalidFieldMessage = "The Web Link field contains text outside a valid link-value or list delimiter.";
    internal const string InvalidTargetMessage = "The Web Link target is not a valid URI-reference.";
    internal const string InvalidParameterMessage = "The Web Link parameter is incomplete or has an invalid value.";

    internal static WebLinkParseResult ParseStrictFieldValueSpan(
        ReadOnlySpan<char> fieldValue,
        WebLinkParseOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateStrictOptions(options);
        if (optionFailure != null || options == null)
        {
            return CreateParseFailure(optionFailure);
        }

        UriDiagnostic? inputFailure = LimitGuard.CheckInputCharacters(
            fieldValue.Length,
            options.Limits,
            DiagnosticDomain.WebLink);
        if (inputFailure != null)
        {
            return CreateParseFailure(inputFailure);
        }

        WebLinkFieldParseResult parsed = ParseStrictCombinedField(
            fieldValue,
            options.Limits,
            0);
        if (!parsed.Success)
        {
            return CreateParseFailure(parsed.Diagnostic);
        }

        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = parsed.Links;
        linkSet.OriginalFieldValues = new string[]
        {
            fieldValue.ToString()
        };
        linkSet.Diagnostics = parsed.Diagnostics;
        return CreateParseSuccess(linkSet, parsed.Diagnostics);
    }

    internal static WebLinkParseResult ParseStrictFieldValues(
        string[]? fieldValues,
        WebLinkParseOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateStrictOptions(options);
        if (optionFailure != null || options == null || fieldValues == null)
        {
            UriDiagnostic failure = optionFailure ??
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink);
            return CreateParseFailure(failure);
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
                return CreateParseFailure(
                    DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.WebLink));
            }

            UriDiagnostic? inputFailure = CheckCombinedInputLimit(
                totalInputCharacters,
                fieldValue.Length,
                options.Limits);
            if (inputFailure != null)
            {
                return CreateParseFailure(inputFailure);
            }

            totalInputCharacters = totalInputCharacters +
                fieldValue.Length;
            WebLinkFieldParseResult parsed = ParseStrictCombinedField(
                fieldValue.AsSpan(),
                options.Limits,
                links.Count);
            if (!parsed.Success)
            {
                return CreateParseFailure(parsed.Diagnostic);
            }

            AddStrictDiagnostics(diagnostics, parsed.Diagnostics);

            int linkIndex = 0;
            while (linkIndex < parsed.Links.Length)
            {
                links.Add(parsed.Links[linkIndex]);
                linkIndex = linkIndex + 1;
            }

            originals[fieldIndex] = fieldValue;
            fieldIndex = fieldIndex + 1;
        }

        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = links.ToArray();
        linkSet.OriginalFieldValues = originals;
        UriDiagnostic[] warnings = diagnostics.ToArray();
        linkSet.Diagnostics = warnings;
        return CreateParseSuccess(linkSet, warnings);
    }

    internal static WebLinkFieldParseResult ParseStrictCombinedField(
        ReadOnlySpan<char> fieldValue,
        OperationLimits limits,
        int initialLinkCount)
    {
        List<WebLink> links = new List<WebLink>();
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int parameterCount = 0;
        int offset = 0;
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
                WebLinkScanResult comma = WebLinkGrammar.ScanComma(
                    fieldValue,
                    offset,
                    0);
                offset = comma.NextOffset;
                whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                    fieldValue,
                    offset,
                    0);
                offset = whitespace.NextOffset;
                continue;
            }

            if (fieldValue[offset] != '<')
            {
                return CreateFieldFailure(CreateFieldDiagnostic(
                    offset,
                    1,
                    InvalidFieldMessage));
            }

            WebLinkValueParseResult parsedLink = ParseStrictLinkValue(
                fieldValue,
                offset,
                limits);
            if (!parsedLink.Success)
            {
                return CreateFieldFailure(parsedLink.Diagnostic);
            }

            AddStrictDiagnostics(diagnostics, parsedLink.Diagnostics);

            UriDiagnostic? linkLimit = LimitGuard.CheckWebLinks(
                initialLinkCount + links.Count + 1,
                limits,
                parsedLink.Link.Range.Offset);
            if (linkLimit != null)
            {
                return CreateFieldFailure(linkLimit);
            }

            int parameterIndex = 0;
            while (parameterIndex < parsedLink.Link.Parameters.Length)
            {
                WebLinkParameter parameter =
                    parsedLink.Link.Parameters[parameterIndex];
                parameterCount = parameterCount + 1;
                UriDiagnostic? parameterLimit =
                    LimitGuard.CheckWebLinkParameters(
                        parameterCount,
                        limits,
                        parameter.Range.Offset);
                if (parameterLimit != null)
                {
                    return CreateFieldFailure(parameterLimit);
                }

                parameterIndex = parameterIndex + 1;
            }

            links.Add(parsedLink.Link);
            offset = parsedLink.NextOffset;
            if (offset == fieldValue.Length)
            {
                break;
            }

            if (fieldValue[offset] != ',')
            {
                return CreateFieldFailure(CreateFieldDiagnostic(
                    offset,
                    1,
                    InvalidFieldMessage));
            }

            WebLinkScanResult separator = WebLinkGrammar.ScanComma(
                fieldValue,
                offset,
                0);
            offset = separator.NextOffset;
            whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
            offset = whitespace.NextOffset;
        }

        WebLinkFieldParseResult result = new WebLinkFieldParseResult(
            true,
            links.ToArray(),
            null);
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal static WebLinkValueParseResult ParseStrictLinkValue(
        ReadOnlySpan<char> fieldValue,
        int startOffset,
        OperationLimits limits)
    {
        WebLinkScanResult target = WebLinkGrammar.ScanAngleTarget(
            fieldValue,
            startOffset,
            0);
        if (!target.Success)
        {
            return CreateLinkFailure(startOffset, target.Diagnostic);
        }

        UriDiagnostic? targetLimit = LimitGuard.CheckComponentCharacters(
            target.Value.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkTarget,
            startOffset + 1);
        if (targetLimit != null)
        {
            return CreateLinkFailure(startOffset, targetLimit);
        }

        UriParseResult targetValidation = UriParserCore.ParseReference(
            target.Value.AsSpan());
        if (!targetValidation.Success)
        {
            return CreateLinkFailure(
                startOffset,
                CreateTargetDiagnostic(
                    startOffset + 1,
                    targetValidation.Diagnostic));
        }

        List<WebLinkParameter> parameters =
            new List<WebLinkParameter>();
        int offset = target.NextOffset;
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

            WebLinkParameterParseResult parameter =
                ParseStrictParameter(fieldValue, offset, limits);
            if (!parameter.Success)
            {
                return CreateLinkFailure(
                    startOffset,
                    parameter.Diagnostic);
            }

            parameters.Add(parameter.Parameter);
            offset = parameter.NextOffset;
            linkEndOffset = offset;
        }

        WebLink link = new WebLink();
        link.Target = target.Value;
        link.Parameters = parameters.ToArray();
        link.OriginalText = fieldValue.Slice(
            startOffset,
            linkEndOffset - startOffset).ToString();
        link.Range = new TextRange(
            startOffset,
            linkEndOffset - startOffset);
        UriDiagnostic[] duplicateDiagnostics =
            WebLinkDuplicateParameterCore.MarkIgnoredDuplicates(link);
        UriDiagnostic? extendedValueFailure =
            WebLinkExtendedValueCore.InterpretStrictExtendedValues(link);
        if (extendedValueFailure != null)
        {
            return CreateLinkFailure(startOffset, extendedValueFailure);
        }

        UriDiagnostic? relationFailure =
            WebLinkRelationCore.InterpretStrictRelations(link);
        if (relationFailure != null)
        {
            return CreateLinkFailure(startOffset, relationFailure);
        }

        UriDiagnostic? attributeFailure =
            WebLinkAttributeCore.InterpretStrictAttributes(link);
        if (attributeFailure != null)
        {
            return CreateLinkFailure(startOffset, attributeFailure);
        }

        WebLinkValueParseResult result = new WebLinkValueParseResult(
            true,
            link,
            offset,
            null);
        result.Diagnostics = duplicateDiagnostics;
        return result;
    }

    internal static WebLinkParameterParseResult ParseStrictParameter(
        ReadOnlySpan<char> fieldValue,
        int startOffset,
        OperationLimits limits)
    {
        WebLinkScanResult semicolon = WebLinkGrammar.ScanSemicolon(
            fieldValue,
            startOffset,
            0);
        if (!semicolon.Success)
        {
            return CreateParameterFailure(
                startOffset,
                semicolon.Diagnostic);
        }

        WebLinkScanResult whitespace =
            WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                semicolon.NextOffset,
                0);
        WebLinkScanResult name = WebLinkGrammar.ScanToken(
            fieldValue,
            whitespace.NextOffset,
            0);
        if (!name.Success)
        {
            return CreateParameterFailure(startOffset, name.Diagnostic);
        }

        UriDiagnostic? nameLimit = LimitGuard.CheckComponentCharacters(
            name.Value.Length,
            limits,
            DiagnosticDomain.WebLink,
            UriComponent.LinkParameter,
            name.StartOffset);
        if (nameLimit != null)
        {
            return CreateParameterFailure(startOffset, nameLimit);
        }

        int offset = name.NextOffset;
        whitespace = WebLinkGrammar.ScanOptionalWhitespace(
            fieldValue,
            offset,
            0);
        offset = whitespace.NextOffset;
        bool hasValue = false;
        bool wasQuoted = false;
        string value = "";
        if (offset < fieldValue.Length && fieldValue[offset] == '=')
        {
            hasValue = true;
            offset = offset + 1;
            whitespace = WebLinkGrammar.ScanOptionalWhitespace(
                fieldValue,
                offset,
                0);
            offset = whitespace.NextOffset;
            if (offset >= fieldValue.Length)
            {
                return CreateParameterFailure(
                    startOffset,
                    CreateParameterDiagnostic(
                        offset,
                        0,
                        InvalidParameterMessage));
            }

            WebLinkScanResult parsedValue;
            if (fieldValue[offset] == '"')
            {
                wasQuoted = true;
                parsedValue = HttpQuotedString.ScanQuotedString(
                    fieldValue,
                    offset,
                    0);
            }
            else
            {
                parsedValue = WebLinkGrammar.ScanToken(
                    fieldValue,
                    offset,
                    0);
            }

            if (!parsedValue.Success)
            {
                return CreateParameterFailure(
                    startOffset,
                    parsedValue.Diagnostic);
            }

            value = parsedValue.Value;
            UriDiagnostic? valueLimit =
                LimitGuard.CheckComponentCharacters(
                    value.Length,
                    limits,
                    DiagnosticDomain.WebLink,
                    UriComponent.LinkParameter,
                    parsedValue.StartOffset);
            if (valueLimit != null)
            {
                return CreateParameterFailure(startOffset, valueLimit);
            }

            offset = parsedValue.NextOffset;
        }

        WebLinkParameter parameter = new WebLinkParameter();
        parameter.Name = name.Value;
        parameter.HasValue = hasValue;
        parameter.Value = value;
        parameter.RawValue = value;
        parameter.WasQuoted = wasQuoted;
        parameter.Extended = name.Value.Length > 0 &&
            name.Value[name.Value.Length - 1] == '*';
        parameter.OriginalText = fieldValue.Slice(
            startOffset,
            offset - startOffset).ToString();
        parameter.Range = new TextRange(
            startOffset,
            offset - startOffset);
        return new WebLinkParameterParseResult(
            true,
            parameter,
            offset,
            null);
    }

    internal static UriDiagnostic? ValidateStrictOptions(
        WebLinkParseOptions? options)
    {
        if (options == null ||
            options.Limits == null ||
            options.Mode != WebLinkParseMode.Strict)
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

    internal static UriDiagnostic? CheckCombinedInputLimit(
        int currentLength,
        int nextLength,
        OperationLimits limits)
    {
        int maximum = limits.MaxInputCharacters;
        if (maximum >= 0 &&
            currentLength <= maximum &&
            nextLength <= maximum - currentLength)
        {
            return null;
        }

        int offset = 0;
        int length = 0;
        if (maximum >= 0 && currentLength <= maximum)
        {
            offset = maximum - currentLength;
            if (nextLength > offset)
            {
                length = nextLength - offset;
            }
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            offset,
            length,
            LimitGuard.InputLimitMessage);
    }

    internal static UriDiagnostic CreateTargetDiagnostic(
        int targetOffset,
        UriDiagnostic? source)
    {
        int offset = targetOffset;
        int length = 0;
        if (source != null)
        {
            offset = UriParserCore.GetAbsoluteOffset(
                targetOffset,
                source.Range.Offset);
            length = source.Range.Length;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            offset,
            length,
            InvalidTargetMessage);
    }

    internal static UriDiagnostic CreateFieldDiagnostic(
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            offset,
            length,
            message);
    }

    internal static UriDiagnostic CreateParameterDiagnostic(
        int offset,
        int length,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            offset,
            length,
            message);
    }

    internal static WebLinkParameterParseResult CreateParameterFailure(
        int startOffset,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkParameterParseResult(
            false,
            new WebLinkParameter(),
            startOffset,
            failure);
    }

    internal static WebLinkValueParseResult CreateLinkFailure(
        int startOffset,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkValueParseResult(
            false,
            new WebLink(),
            startOffset,
            failure);
    }

    internal static WebLinkFieldParseResult CreateFieldFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkFieldParseResult(
            false,
            new WebLink[0],
            failure);
    }

    internal static void AddStrictDiagnostics(
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

    internal static WebLinkParseResult CreateParseSuccess(
        WebLinkSet linkSet,
        UriDiagnostic[] diagnostics)
    {
        return new WebLinkParseResult(
            true,
            linkSet,
            false,
            null,
            diagnostics);
    }

    internal static WebLinkParseResult CreateParseFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = diagnostic ??
            DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        return new WebLinkParseResult(
            false,
            new WebLinkSet(),
            false,
            failure,
            new UriDiagnostic[] { failure });
    }
}
