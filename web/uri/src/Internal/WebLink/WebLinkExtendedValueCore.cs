using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkExtendedValueCore
{
    internal const string MalformedExtendedValueMessage = "The RFC 8187 extended parameter value is malformed.";
    internal const string UnsupportedCharacterSetMessage = "The RFC 8187 extended parameter uses an unsupported character set; UTF-8 is required.";
    internal const string InvalidExtendedUtf8Message = "The RFC 8187 extended parameter does not contain valid strict UTF-8.";
    internal const string RecoveredMalformedExtendedValueMessage = "Recovery retained an undecoded malformed RFC 8187 extended parameter value.";
    internal const string RecoveredUnsupportedCharacterSetMessage = "Recovery retained an undecoded RFC 8187 extended parameter that does not use UTF-8.";
    internal const string RecoveredInvalidExtendedUtf8Message = "Recovery retained an undecoded RFC 8187 extended parameter containing invalid UTF-8.";

    internal static UriDiagnostic? InterpretStrictExtendedValues(WebLink link)
    {
        UriDiagnostic[] diagnostics = InterpretExtendedValues(link, false);
        if (diagnostics.Length == 0)
        {
            return null;
        }

        return diagnostics[0];
    }

    internal static UriDiagnostic[] InterpretRecoveryExtendedValues(
        WebLink link)
    {
        return InterpretExtendedValues(link, true);
    }

    internal static UriDiagnostic[] InterpretExtendedValues(
        WebLink link,
        bool recovery)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title*") &&
                parameter.HasValue &&
                !parameter.Ignored)
            {
                UriDiagnostic? diagnostic = DecodeParameter(
                    link,
                    parameter,
                    recovery);
                if (diagnostic != null)
                {
                    diagnostics.Add(diagnostic);
                }
            }

            index = index + 1;
        }

        return diagnostics.ToArray();
    }

    internal static UriDiagnostic? DecodeParameter(
        WebLink link,
        WebLinkParameter parameter,
        bool recovery)
    {
        string rawValue = parameter.RawValue;
        if (parameter.WasQuoted)
        {
            return CreateExtendedDiagnostic(
                recovery,
                UriDiagnosticCode.InvalidExtendedParameter,
                WebLinkParameterValueCore.GetCharacterOffset(parameter, 0),
                GetSourceLength(parameter, 0, rawValue.Length),
                MalformedExtendedValueMessage,
                RecoveredMalformedExtendedValueMessage);
        }

        int firstQuote = rawValue.IndexOf('\'');
        if (firstQuote < 0)
        {
            return CreateMalformedDiagnostic(
                parameter,
                recovery,
                rawValue.Length,
                0);
        }

        if (firstQuote == 0)
        {
            return CreateMalformedDiagnostic(parameter, recovery, 0, 0);
        }

        int invalidCharset = FindInvalidCharsetCharacter(
            rawValue,
            firstQuote);
        if (invalidCharset >= 0)
        {
            return CreateMalformedDiagnostic(
                parameter,
                recovery,
                invalidCharset,
                1);
        }

        int secondQuote = rawValue.IndexOf('\'', firstQuote + 1);
        if (secondQuote < 0)
        {
            return CreateMalformedDiagnostic(
                parameter,
                recovery,
                rawValue.Length,
                0);
        }

        string charset = rawValue.Substring(0, firstQuote);
        string language = rawValue.Substring(
            firstQuote + 1,
            secondQuote - firstQuote - 1);
        parameter.CharacterSet = charset;
        parameter.Language = language;
        if (!AsciiCase.EqualsIgnoreCase(charset, "UTF-8"))
        {
            return CreateExtendedDiagnostic(
                recovery,
                UriDiagnosticCode.UnsupportedCharacterSet,
                WebLinkParameterValueCore.GetCharacterOffset(parameter, 0),
                GetSourceLength(parameter, 0, firstQuote),
                UnsupportedCharacterSetMessage,
                RecoveredUnsupportedCharacterSetMessage);
        }

        int valueOffset = secondQuote + 1;
        int invalidValue = FindInvalidValueCharacter(rawValue, valueOffset);
        if (invalidValue >= 0)
        {
            int invalidLength = 1;
            if (rawValue[invalidValue] == '%' &&
                rawValue.Length - invalidValue >= 3)
            {
                if (!Ascii.IsHexDigit(rawValue[invalidValue + 1]))
                {
                    invalidValue = invalidValue + 1;
                }
                else if (!Ascii.IsHexDigit(rawValue[invalidValue + 2]))
                {
                    invalidValue = invalidValue + 2;
                }
            }

            return CreateMalformedDiagnostic(
                parameter,
                recovery,
                invalidValue,
                invalidLength);
        }

        string encodedValue = rawValue.Substring(valueOffset);
        PercentByteDecodeResult decodedBytes = PercentDecoderCore.DecodeSpan(
            encodedValue.AsSpan(),
            encodedValue.Length,
            UriComponent.ExtendedParameter,
            0);
        if (!decodedBytes.Success)
        {
            int diagnosticOffset = valueOffset;
            int diagnosticLength = 0;
            if (decodedBytes.Diagnostic != null)
            {
                diagnosticOffset = valueOffset +
                    decodedBytes.Diagnostic.Range.Offset;
                diagnosticLength = decodedBytes.Diagnostic.Range.Length;
            }

            return CreateMalformedDiagnostic(
                parameter,
                recovery,
                diagnosticOffset,
                diagnosticLength);
        }

        Utf8DecodeResult decodedText = Utf8CodecCore.DecodeBytes(
            decodedBytes.Bytes);
        if (!decodedText.Success)
        {
            int sourceOffset = PercentDecoderCore.GetSourceOffset(
                encodedValue.AsSpan(),
                decodedText.ErrorOffset);
            int sourceEnd = PercentDecoderCore.GetSourceOffset(
                encodedValue.AsSpan(),
                decodedText.ErrorOffset + decodedText.ErrorLength);
            return CreateExtendedDiagnostic(
                recovery,
                UriDiagnosticCode.InvalidExtendedParameter,
                WebLinkParameterValueCore.GetCharacterOffset(
                    parameter,
                    valueOffset + sourceOffset),
                GetSourceLength(
                    parameter,
                    valueOffset + sourceOffset,
                    sourceEnd - sourceOffset),
                InvalidExtendedUtf8Message,
                RecoveredInvalidExtendedUtf8Message);
        }

        parameter.Value = decodedText.Text;
        UriDiagnostic? languageDiagnostic =
            WebLinkLanguageTagCore.ValidateExtendedLanguage(
                parameter,
                recovery);
        if (languageDiagnostic != null)
        {
            return languageDiagnostic;
        }

        if (AsciiCase.EqualsIgnoreCase(parameter.Name, "title*"))
        {
            link.HasTitleExtended = true;
            link.TitleExtended = decodedText.Text;
            link.TitleExtendedCharacterSet = charset;
            link.TitleExtendedLanguage = language;
            link.TitleExtendedRawValue = rawValue;
        }

        return null;
    }

    internal static PercentEncodeResult EncodeUtf8Value(
        string value,
        string language,
        int maximumOutputCharacters)
    {
        if (language.Length > 0)
        {
            WebLinkLanguageTagValidationResult validation =
                WebLinkLanguageTagCore.ValidateTag(language);
            if (!validation.Valid)
            {
                UriDiagnostic languageDiagnostic =
                    WebLinkLanguageTagCore.CreateLanguageDiagnostic(
                        false,
                        validation.ErrorOffset,
                        validation.ErrorLength);
                return new PercentEncodeResult(
                    false,
                    "",
                    languageDiagnostic);
            }
        }

        int prefixLength = 7;
        if (language.Length > int.MaxValue - prefixLength)
        {
            return CreateEncodingLimitFailure(maximumOutputCharacters);
        }

        prefixLength = prefixLength + language.Length;
        if (maximumOutputCharacters < prefixLength)
        {
            return CreateEncodingLimitFailure(maximumOutputCharacters);
        }

        PercentEncodeResult encoded = PercentEncoderCore.EncodeText(
            value,
            PercentEncodeSet.WebLinkExtendedValue,
            maximumOutputCharacters - prefixLength,
            UriComponent.ExtendedParameter,
            0);
        if (!encoded.Success)
        {
            return MapEncodingFailure(encoded, prefixLength);
        }

        return new PercentEncodeResult(
            true,
            $"UTF-8'{language}'{encoded.Text}",
            null);
    }

    internal static int FindInvalidCharsetCharacter(
        string rawValue,
        int charsetLength)
    {
        int index = 0;
        while (index < charsetLength)
        {
            char value = rawValue[index];
            if (!Ascii.IsAlphaNumeric(value) &&
                value != '!' &&
                value != '#' &&
                value != '$' &&
                value != '%' &&
                value != '&' &&
                value != '+' &&
                value != '-' &&
                value != '^' &&
                value != '_' &&
                value != '`' &&
                value != '{' &&
                value != '}' &&
                value != '~')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindInvalidValueCharacter(
        string rawValue,
        int valueOffset)
    {
        int index = valueOffset;
        while (index < rawValue.Length)
        {
            char value = rawValue[index];
            if (value == '%')
            {
                if (rawValue.Length - index < 3 ||
                    !Ascii.IsHexDigit(rawValue[index + 1]) ||
                    !Ascii.IsHexDigit(rawValue[index + 2]))
                {
                    return index;
                }

                index = index + 3;
                continue;
            }

            if (!PercentAllowlist.IsWebLinkExtendedValueCharacter(value))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int GetSourceLength(
        WebLinkParameter parameter,
        int valueOffset,
        int valueLength)
    {
        int startOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            valueOffset);
        int endOffset = WebLinkParameterValueCore.GetCharacterOffset(
            parameter,
            valueOffset + valueLength);
        return endOffset - startOffset;
    }

    internal static UriDiagnostic CreateMalformedDiagnostic(
        WebLinkParameter parameter,
        bool recovery,
        int valueOffset,
        int valueLength)
    {
        return CreateExtendedDiagnostic(
            recovery,
            UriDiagnosticCode.InvalidExtendedParameter,
            WebLinkParameterValueCore.GetCharacterOffset(
                parameter,
                valueOffset),
            GetSourceLength(parameter, valueOffset, valueLength),
            MalformedExtendedValueMessage,
            RecoveredMalformedExtendedValueMessage);
    }

    internal static UriDiagnostic CreateExtendedDiagnostic(
        bool recovery,
        UriDiagnosticCode code,
        int offset,
        int length,
        string errorMessage,
        string recoveryMessage)
    {
        if (recovery)
        {
            return DiagnosticFactory.CreateWarning(
                DiagnosticDomain.WebLink,
                code,
                UriComponent.ExtendedParameter,
                offset,
                length,
                recoveryMessage);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            code,
            UriComponent.ExtendedParameter,
            offset,
            length,
            errorMessage);
    }

    internal static PercentEncodeResult CreateEncodingLimitFailure(
        int maximumOutputCharacters)
    {
        int offset = maximumOutputCharacters;
        if (offset < 0)
        {
            offset = 0;
        }

        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            offset,
            0,
            LimitGuard.OutputLimitMessage);
        return new PercentEncodeResult(false, "", diagnostic);
    }

    internal static PercentEncodeResult MapEncodingFailure(
        PercentEncodeResult source,
        int prefixLength)
    {
        if (source.Diagnostic == null)
        {
            return source;
        }

        UriComponent component = UriComponent.ExtendedParameter;
        int offset = source.Diagnostic.Range.Offset;
        if (source.Diagnostic.Code == UriDiagnosticCode.LimitExceeded)
        {
            component = UriComponent.Output;
            offset = prefixLength + offset;
        }

        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            source.Diagnostic.Code,
            component,
            offset,
            source.Diagnostic.Range.Length,
            source.Diagnostic.Message);
        return new PercentEncodeResult(false, "", diagnostic);
    }
}
