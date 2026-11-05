using System;

namespace uri;

internal static class QueryCodecCore
{
    internal static UriQueryParseCoreResult ParseQuery(
        string? query,
        UriQueryOptions? options)
    {
        char pairDelimiter = GetPairDelimiter(options);
        UriDiagnostic? inputFailure = GetParseInputDiagnostic(query, options);
        if (inputFailure != null)
        {
            return CreateParseFailure(pairDelimiter, inputFailure);
        }

        if (query == null || options == null)
        {
            return CreateParseFailure(
                pairDelimiter,
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriDiagnostic? invalidQuery = UriValidatorCore.ValidateQueryOrFragmentValue(
            query.AsSpan(),
            UriComponent.Query);
        if (invalidQuery != null)
        {
            return CreateParseFailure(pairDelimiter, invalidQuery);
        }

        int itemCount = CountQueryItems(query.AsSpan(), pairDelimiter);
        UriDiagnostic? itemLimit = LimitGuard.CheckQueryItems(
            itemCount,
            options.Limits,
            DiagnosticDomain.Uri,
            0);
        if (itemLimit != null)
        {
            return CreateParseFailure(pairDelimiter, itemLimit);
        }

        UriQueryParameterValue[] parameters = new UriQueryParameterValue[itemCount];
        int parameterOffset = 0;
        int itemStart = 0;
        while (parameterOffset < itemCount)
        {
            int itemEnd = FindPairDelimiter(
                query.AsSpan(),
                itemStart,
                pairDelimiter);
            if (itemEnd < 0)
            {
                itemEnd = query.Length;
            }

            int equalsOffset = FindEquals(query.AsSpan(), itemStart, itemEnd);
            int nameEnd = itemEnd;
            bool hasValue = false;
            int valueStart = itemEnd;
            if (equalsOffset >= 0)
            {
                nameEnd = equalsOffset;
                hasValue = true;
                valueStart = equalsOffset + 1;
            }

            PercentTextDecodeResult name = DecodeQueryParameterText(
                query.AsSpan(itemStart, nameEnd - itemStart),
                options.Limits.MaxComponentCharacters,
                itemStart,
                options.FormUrlEncoded);
            if (!name.Success)
            {
                return CreateParseFailure(pairDelimiter, name.Diagnostic);
            }

            string value = "";
            if (hasValue)
            {
                PercentTextDecodeResult decodedValue = DecodeQueryParameterText(
                    query.AsSpan(valueStart, itemEnd - valueStart),
                    options.Limits.MaxComponentCharacters,
                    valueStart,
                    options.FormUrlEncoded);
                if (!decodedValue.Success)
                {
                    return CreateParseFailure(pairDelimiter, decodedValue.Diagnostic);
                }

                value = decodedValue.Text;
            }

            parameters[parameterOffset] = new UriQueryParameterValue(
                name.Text,
                hasValue,
                value);
            parameterOffset = parameterOffset + 1;
            itemStart = itemEnd + 1;
        }

        return new UriQueryParseCoreResult(
            true,
            parameters,
            pairDelimiter,
            null);
    }

    internal static UriQueryComposeCoreResult ComposeQuery(
        UriQueryParameterValue[]? parameters,
        UriQueryOptions? options)
    {
        char pairDelimiter = GetPairDelimiter(options);
        UriDiagnostic? inputFailure = GetComposeInputDiagnostic(
            parameters,
            options);
        if (inputFailure != null)
        {
            return CreateComposeFailure(pairDelimiter, inputFailure);
        }

        if (parameters == null || options == null)
        {
            return CreateComposeFailure(
                pairDelimiter,
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        string[] encodedNames = new string[parameters.Length];
        string[] encodedValues = new string[parameters.Length];
        long outputLength = 0;
        int parameterOffset = 0;
        while (parameterOffset < parameters.Length)
        {
            UriQueryParameterValue parameter = parameters[parameterOffset];
            PercentEncodeResult encodedName = EncodeQueryParameterText(
                parameter.Name,
                pairDelimiter,
                true,
                0,
                options.FormUrlEncoded);
            if (!encodedName.Success)
            {
                return CreateComposeFailure(pairDelimiter, encodedName.Diagnostic);
            }

            encodedNames[parameterOffset] = encodedName.Text;
            encodedValues[parameterOffset] = "";
            outputLength = outputLength + encodedName.Text.Length;
            if (parameter.HasValue)
            {
                PercentEncodeResult encodedValue = EncodeQueryParameterText(
                    parameter.Value,
                    pairDelimiter,
                    false,
                    0,
                    options.FormUrlEncoded);
                if (!encodedValue.Success)
                {
                    return CreateComposeFailure(pairDelimiter, encodedValue.Diagnostic);
                }

                encodedValues[parameterOffset] = encodedValue.Text;
                outputLength = outputLength + 1 + encodedValue.Text.Length;
            }

            if (parameterOffset > 0)
            {
                outputLength = outputLength + 1;
            }

            if (outputLength > int.MaxValue)
            {
                return CreateComposeFailure(
                    pairDelimiter,
                    DiagnosticFactory.CreateLimitExceeded(
                        DiagnosticDomain.Uri,
                        UriComponent.Output,
                        0,
                        0));
            }

            parameterOffset = parameterOffset + 1;
        }

        UriDiagnostic? outputLimit = LimitGuard.CheckOutputCharacters(
            (int)outputLength,
            options.Limits,
            DiagnosticDomain.Uri);
        if (outputLimit != null)
        {
            return CreateComposeFailure(pairDelimiter, outputLimit);
        }

        char[] output = new char[(int)outputLength];
        int outputOffset = 0;
        parameterOffset = 0;
        while (parameterOffset < parameters.Length)
        {
            if (parameterOffset > 0)
            {
                output[outputOffset] = pairDelimiter;
                outputOffset = outputOffset + 1;
            }

            outputOffset = CopyText(
                encodedNames[parameterOffset],
                output,
                outputOffset);
            if (parameters[parameterOffset].HasValue)
            {
                output[outputOffset] = '=';
                outputOffset = outputOffset + 1;
                outputOffset = CopyText(
                    encodedValues[parameterOffset],
                    output,
                    outputOffset);
            }

            parameterOffset = parameterOffset + 1;
        }

        return new UriQueryComposeCoreResult(
            true,
            new string(output),
            output.Length,
            pairDelimiter,
            null);
    }

    internal static UriQueryParameterValue[] AddQueryParameter(
        UriQueryParameterValue[] parameters,
        UriQueryParameterValue parameter)
    {
        UriQueryParameterValue[] result = new UriQueryParameterValue[parameters.Length + 1];
        CopyQueryParameters(parameters, result, 0);
        result[parameters.Length] = parameter;
        return result;
    }

    internal static UriQueryParameterValue[] ReplaceQueryParameters(
        UriQueryParameterValue[] parameters,
        string name,
        UriQueryParameterValue[] replacements)
    {
        int matchCount = CountQueryParameterNames(parameters, name);
        UriQueryParameterValue[] result = new UriQueryParameterValue[
            parameters.Length - matchCount + replacements.Length];
        int inputOffset = 0;
        int outputOffset = 0;
        bool inserted = false;
        while (inputOffset < parameters.Length)
        {
            UriQueryParameterValue parameter = parameters[inputOffset];
            if (string.CompareOrdinal(parameter.Name, name) == 0)
            {
                if (!inserted)
                {
                    outputOffset = CopyQueryParameters(
                        replacements,
                        result,
                        outputOffset);
                    inserted = true;
                }
            }
            else
            {
                result[outputOffset] = parameter;
                outputOffset = outputOffset + 1;
            }

            inputOffset = inputOffset + 1;
        }

        if (!inserted)
        {
            CopyQueryParameters(replacements, result, outputOffset);
        }

        return result;
    }

    internal static UriQueryParameterValue[] RemoveQueryParameters(
        UriQueryParameterValue[] parameters,
        string name)
    {
        return ReplaceQueryParameters(
            parameters,
            name,
            new UriQueryParameterValue[0]);
    }

    internal static UriQueryParameterValue[] SortQueryParameters(
        UriQueryParameterValue[] parameters)
    {
        UriQueryParameterValue[] result = new UriQueryParameterValue[parameters.Length];
        CopyQueryParameters(parameters, result, 0);
        int inputOffset = 1;
        while (inputOffset < result.Length)
        {
            UriQueryParameterValue current = result[inputOffset];
            int insertionOffset = inputOffset;
            while (insertionOffset > 0 &&
                CompareQueryParameters(current, result[insertionOffset - 1]) < 0)
            {
                result[insertionOffset] = result[insertionOffset - 1];
                insertionOffset = insertionOffset - 1;
            }

            result[insertionOffset] = current;
            inputOffset = inputOffset + 1;
        }

        return result;
    }

    internal static UriQueryParameterValue[] DeduplicateQueryParameters(
        UriQueryParameterValue[] parameters)
    {
        int uniqueCount = 0;
        int inputOffset = 0;
        while (inputOffset < parameters.Length)
        {
            if (!HasEqualQueryParameterBefore(parameters, inputOffset))
            {
                uniqueCount = uniqueCount + 1;
            }

            inputOffset = inputOffset + 1;
        }

        UriQueryParameterValue[] result = new UriQueryParameterValue[uniqueCount];
        int outputOffset = 0;
        inputOffset = 0;
        while (inputOffset < parameters.Length)
        {
            if (!HasEqualQueryParameterBefore(parameters, inputOffset))
            {
                result[outputOffset] = parameters[inputOffset];
                outputOffset = outputOffset + 1;
            }

            inputOffset = inputOffset + 1;
        }

        return result;
    }

    internal static int CopyQueryParameters(
        UriQueryParameterValue[] source,
        UriQueryParameterValue[] destination,
        int destinationOffset)
    {
        int sourceOffset = 0;
        while (sourceOffset < source.Length)
        {
            destination[destinationOffset] = source[sourceOffset];
            destinationOffset = destinationOffset + 1;
            sourceOffset = sourceOffset + 1;
        }

        return destinationOffset;
    }

    internal static int CountQueryParameterNames(
        UriQueryParameterValue[] parameters,
        string name)
    {
        int count = 0;
        int offset = 0;
        while (offset < parameters.Length)
        {
            if (string.CompareOrdinal(parameters[offset].Name, name) == 0)
            {
                count = count + 1;
            }

            offset = offset + 1;
        }

        return count;
    }

    internal static int CompareQueryParameters(
        UriQueryParameterValue left,
        UriQueryParameterValue right)
    {
        int nameComparison = string.CompareOrdinal(left.Name, right.Name);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        if (!left.HasValue && right.HasValue)
        {
            return -1;
        }

        if (left.HasValue && !right.HasValue)
        {
            return 1;
        }

        if (!left.HasValue)
        {
            return 0;
        }

        return string.CompareOrdinal(left.Value, right.Value);
    }

    internal static bool HasEqualQueryParameterBefore(
        UriQueryParameterValue[] parameters,
        int parameterOffset)
    {
        int offset = 0;
        while (offset < parameterOffset)
        {
            if (AreQueryParametersEqual(
                parameters[offset],
                parameters[parameterOffset]))
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static bool AreQueryParametersEqual(
        UriQueryParameterValue left,
        UriQueryParameterValue right)
    {
        if (string.CompareOrdinal(left.Name, right.Name) != 0 ||
            left.HasValue != right.HasValue)
        {
            return false;
        }

        if (!left.HasValue)
        {
            return true;
        }

        return string.CompareOrdinal(left.Value, right.Value) == 0;
    }

    internal static PercentTextDecodeResult DecodeQueryParameterText(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        int baseOffset,
        bool formUrlEncoded)
    {
        if (!formUrlEncoded)
        {
            return PercentDecoderCore.DecodeTextSpanStrict(
                text,
                maximumOutputCharacters,
                UriComponent.Query,
                baseOffset);
        }

        bool hasPlus = false;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '+')
            {
                hasPlus = true;
                break;
            }

            offset = offset + 1;
        }

        if (!hasPlus)
        {
            return PercentDecoderCore.DecodeTextSpanStrict(
                text,
                maximumOutputCharacters,
                UriComponent.Query,
                baseOffset);
        }

        char[] translated = new char[text.Length];
        offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '+')
            {
                translated[offset] = ' ';
            }
            else
            {
                translated[offset] = text[offset];
            }

            offset = offset + 1;
        }

        return PercentDecoderCore.DecodeTextSpanStrict(
            translated.AsSpan(),
            maximumOutputCharacters,
            UriComponent.Query,
            baseOffset);
    }

    internal static PercentEncodeResult EncodeQueryParameterText(
        string? text,
        char pairDelimiter,
        bool encodeEquals,
        int baseOffset,
        bool formUrlEncoded)
    {
        PercentEncodeResult plain = EncodePlainParameterText(
            text,
            pairDelimiter,
            encodeEquals,
            baseOffset);
        if (!plain.Success || !formUrlEncoded)
        {
            return plain;
        }

        return TransformFormEncodedText(plain.Text);
    }

    internal static PercentEncodeResult TransformFormEncodedText(string text)
    {
        long outputLength = text.Length;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '+')
            {
                outputLength = outputLength + 2;
                offset = offset + 1;
            }
            else if (offset + 2 < text.Length &&
                text[offset] == '%' &&
                text[offset + 1] == '2' &&
                text[offset + 2] == '0')
            {
                outputLength = outputLength - 2;
                offset = offset + 3;
            }
            else
            {
                offset = offset + 1;
            }
        }

        if (outputLength > int.MaxValue)
        {
            return new PercentEncodeResult(
                false,
                "",
                DiagnosticFactory.CreateLimitExceeded(
                    DiagnosticDomain.Uri,
                    UriComponent.Output,
                    0,
                    0));
        }

        char[] output = new char[(int)outputLength];
        int outputOffset = 0;
        offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '+')
            {
                outputOffset = PercentEncoderCore.WritePercentByte(
                    (byte)'+',
                    output,
                    outputOffset);
                offset = offset + 1;
            }
            else if (offset + 2 < text.Length &&
                text[offset] == '%' &&
                text[offset + 1] == '2' &&
                text[offset + 2] == '0')
            {
                output[outputOffset] = '+';
                outputOffset = outputOffset + 1;
                offset = offset + 3;
            }
            else
            {
                output[outputOffset] = text[offset];
                outputOffset = outputOffset + 1;
                offset = offset + 1;
            }
        }

        return new PercentEncodeResult(true, new string(output), null);
    }

    internal static PercentEncodeResult EncodePlainParameterText(
        string? text,
        char pairDelimiter,
        bool encodeEquals,
        int baseOffset)
    {
        if (text == null || !IsSupportedPairDelimiter(pairDelimiter))
        {
            return new PercentEncodeResult(
                false,
                "",
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        PercentEncodeResult queryEncoded = PercentEncoderCore.EncodeText(
            text,
            PercentEncodeSet.Query,
            int.MaxValue,
            UriComponent.Query,
            baseOffset);
        if (!queryEncoded.Success)
        {
            return queryEncoded;
        }

        int structuralCount = 0;
        int offset = 0;
        while (offset < queryEncoded.Text.Length)
        {
            char value = queryEncoded.Text[offset];
            if (value == pairDelimiter || (encodeEquals && value == '='))
            {
                structuralCount = structuralCount + 1;
            }

            offset = offset + 1;
        }

        if (structuralCount == 0)
        {
            return queryEncoded;
        }

        if (structuralCount > (int.MaxValue - queryEncoded.Text.Length) / 2)
        {
            return new PercentEncodeResult(
                false,
                "",
                DiagnosticFactory.CreateLimitExceeded(
                    DiagnosticDomain.Uri,
                    UriComponent.Output,
                    0,
                    0));
        }

        char[] encoded = new char[queryEncoded.Text.Length + structuralCount * 2];
        int outputOffset = 0;
        offset = 0;
        while (offset < queryEncoded.Text.Length)
        {
            char value = queryEncoded.Text[offset];
            if (value == pairDelimiter || (encodeEquals && value == '='))
            {
                outputOffset = PercentEncoderCore.WritePercentByte(
                    (byte)value,
                    encoded,
                    outputOffset);
            }
            else
            {
                encoded[outputOffset] = value;
                outputOffset = outputOffset + 1;
            }

            offset = offset + 1;
        }

        return new PercentEncodeResult(true, new string(encoded), null);
    }

    internal static UriDiagnostic? GetComposeInputDiagnostic(
        UriQueryParameterValue[]? parameters,
        UriQueryOptions? options)
    {
        if (parameters == null ||
            options == null ||
            options.Limits == null ||
            !IsSupportedPairDelimiter(options.PairDelimiter))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        UriDiagnostic? itemLimit = LimitGuard.CheckQueryItems(
            parameters.Length,
            options.Limits,
            DiagnosticDomain.Uri,
            0);
        if (itemLimit != null)
        {
            return itemLimit;
        }

        long inputLength = 0;
        int offset = 0;
        while (offset < parameters.Length)
        {
            UriQueryParameterValue? parameter = parameters[offset];
            if (parameter == null ||
                parameter.Name == null ||
                parameter.Value == null)
            {
                return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
            }

            UriDiagnostic? nameLimit = LimitGuard.CheckComponentCharacters(
                parameter.Name.Length,
                options.Limits,
                DiagnosticDomain.Uri,
                UriComponent.Query,
                0);
            if (nameLimit != null)
            {
                return nameLimit;
            }

            UriDiagnostic? valueLimit = LimitGuard.CheckComponentCharacters(
                parameter.Value.Length,
                options.Limits,
                DiagnosticDomain.Uri,
                UriComponent.Query,
                0);
            if (valueLimit != null)
            {
                return valueLimit;
            }

            inputLength = inputLength + parameter.Name.Length;
            if (parameter.HasValue)
            {
                inputLength = inputLength + 1 + parameter.Value.Length;
            }

            if (offset > 0)
            {
                inputLength = inputLength + 1;
            }

            if (inputLength > int.MaxValue)
            {
                return DiagnosticFactory.CreateLimitExceeded(
                    DiagnosticDomain.Uri,
                    UriComponent.Input,
                    0,
                    0);
            }

            offset = offset + 1;
        }

        return LimitGuard.CheckInputCharacters(
            (int)inputLength,
            options.Limits,
            DiagnosticDomain.Uri);
    }

    internal static int CopyText(
        string text,
        char[] output,
        int outputOffset)
    {
        int textOffset = 0;
        while (textOffset < text.Length)
        {
            output[outputOffset] = text[textOffset];
            outputOffset = outputOffset + 1;
            textOffset = textOffset + 1;
        }

        return outputOffset;
    }

    internal static int CountQueryItems(
        ReadOnlySpan<char> query,
        char pairDelimiter)
    {
        if (query.Length == 0)
        {
            return 0;
        }

        int count = 1;
        int offset = 0;
        while (offset < query.Length)
        {
            if (query[offset] == pairDelimiter)
            {
                count = count + 1;
            }

            offset = offset + 1;
        }

        return count;
    }

    internal static int FindPairDelimiter(
        ReadOnlySpan<char> query,
        int startOffset,
        char pairDelimiter)
    {
        int offset = startOffset;
        while (offset < query.Length)
        {
            if (query[offset] == pairDelimiter)
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static int FindEquals(
        ReadOnlySpan<char> query,
        int startOffset,
        int endOffset)
    {
        int offset = startOffset;
        while (offset < endOffset)
        {
            if (query[offset] == '=')
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static UriDiagnostic? GetParseInputDiagnostic(
        string? query,
        UriQueryOptions? options)
    {
        if (query == null ||
            options == null ||
            options.Limits == null ||
            !IsSupportedPairDelimiter(options.PairDelimiter))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            query.Length,
            options.Limits,
            DiagnosticDomain.Uri);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        return LimitGuard.CheckComponentCharacters(
            query.Length,
            options.Limits,
            DiagnosticDomain.Uri,
            UriComponent.Query,
            0);
    }

    internal static char GetPairDelimiter(UriQueryOptions? options)
    {
        if (options == null)
        {
            return '&';
        }

        return options.PairDelimiter;
    }

    internal static bool IsSupportedPairDelimiter(char pairDelimiter)
    {
        return pairDelimiter == '&' || pairDelimiter == ';';
    }

    internal static UriQueryParseCoreResult CreateParseFailure(
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriQueryParseCoreResult(
            false,
            new UriQueryParameterValue[0],
            pairDelimiter,
            failure);
    }

    internal static UriQueryComposeCoreResult CreateComposeFailure(
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriQueryComposeCoreResult(
            false,
            "",
            0,
            pairDelimiter,
            failure);
    }
}
