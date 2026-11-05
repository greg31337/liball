using System;
using System.Globalization;

namespace uri;

internal static class IdnHostCore
{
    internal const string InvalidLabelMessage = "The host contains an invalid internationalized domain-name label.";
    internal const string MappingModeMessage = "The requested IDN mapping mode is not available for this operation.";
    internal const string DomainLengthMessage = "The mapped DNS host exceeds the maximum wire-format name length.";

    internal static UriTextResult ConvertHostToAscii(
        ReadOnlySpan<char> host,
        IriIdnMode mode,
        OperationLimits? limits)
    {
        return ConvertHost(host, mode, limits, true);
    }

    internal static UriTextResult ConvertHostToUnicode(
        ReadOnlySpan<char> host,
        IriIdnMode mode,
        OperationLimits? limits)
    {
        return ConvertHost(host, mode, limits, false);
    }

    internal static UriTextResult ConvertHost(
        ReadOnlySpan<char> host,
        IriIdnMode mode,
        OperationLimits? limits,
        bool toAscii)
    {
        if (limits == null)
        {
            return IriConverterCore.CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        if (mode != IriIdnMode.Platform)
        {
            return CreateIdnFailure(
                UriDiagnosticCode.InvalidInput,
                0,
                host.Length,
                MappingModeMessage);
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            host.Length,
            limits,
            DiagnosticDomain.Iri);
        if (inputLimit != null)
        {
            return IriConverterCore.CreateFailure(inputLimit);
        }

        UriDiagnostic? unicode = ValidateUnicodeInput(host);
        if (unicode != null)
        {
            return IriConverterCore.CreateFailure(unicode);
        }

        string normalized = NormalizeSeparators(host);
        UriDiagnostic? bidi = BidiValidator.ValidateIdnHost(
            normalized.AsSpan(),
            mode);
        if (bidi != null)
        {
            return IriConverterCore.CreateFailure(bidi);
        }

        if (normalized.Length == 0)
        {
            if (limits.MaxOutputCharacters < 0)
            {
                return IriConverterCore.CreateOutputFailure(0, 0);
            }

            return new UriTextResult(true, "", 0, null);
        }

        if (normalized == ".")
        {
            if (limits.MaxOutputCharacters < 1)
            {
                return IriConverterCore.CreateOutputFailure(0, 1);
            }

            return new UriTextResult(true, ".", 1, null);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        IdnMapping mapping = new IdnMapping();
        int labelOffset = 0;
        int asciiDomainLength = 0;
        while (labelOffset < normalized.Length)
        {
            int separatorOffset = FindSeparator(normalized.AsSpan(), labelOffset);
            int labelEnd = separatorOffset;
            if (labelEnd < 0)
            {
                labelEnd = normalized.Length;
            }

            int labelLength = labelEnd - labelOffset;
            if (labelLength == 0)
            {
                return CreateIdnFailure(
                    UriDiagnosticCode.InvalidIdnLabel,
                    labelOffset,
                    1,
                    InvalidLabelMessage);
            }

            string label = normalized.Substring(labelOffset, labelLength);
            UriDiagnostic? labelDiagnostic;
            if (toAscii)
            {
                labelDiagnostic = ValidateUnicodeLabel(label, labelOffset);
            }
            else
            {
                labelDiagnostic = ValidateAsciiLabel(label, labelOffset);
            }

            if (labelDiagnostic != null)
            {
                return IriConverterCore.CreateFailure(labelDiagnostic);
            }

            string mapped;
            string asciiLabel;
            if (toAscii)
            {
                mapped = mapping.GetAscii(label);
                asciiLabel = mapped;
                string unicodeRoundTrip = mapping.GetUnicode(asciiLabel);
                string asciiRoundTrip = mapping.GetAscii(unicodeRoundTrip);
                if (!string.Equals(asciiLabel, asciiRoundTrip, StringComparison.OrdinalIgnoreCase))
                {
                    return CreateIdnFailure(
                        UriDiagnosticCode.InvalidIdnLabel,
                        labelOffset,
                        labelLength,
                        InvalidLabelMessage);
                }
            }
            else
            {
                mapped = mapping.GetUnicode(label);
                asciiLabel = mapping.GetAscii(mapped);
                if (!string.Equals(label, asciiLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return CreateIdnFailure(
                        UriDiagnosticCode.InvalidIdnLabel,
                        labelOffset,
                        labelLength,
                        InvalidLabelMessage);
                }

                labelDiagnostic = ValidateUnicodeLabel(mapped, labelOffset);
                if (labelDiagnostic != null)
                {
                    return IriConverterCore.CreateFailure(labelDiagnostic);
                }
            }

            if (asciiLabel.Length == 0 || asciiLabel.Length > 63)
            {
                return CreateIdnFailure(
                    UriDiagnosticCode.InvalidIdnLabel,
                    labelOffset,
                    labelLength,
                    InvalidLabelMessage);
            }

            if (asciiDomainLength > 0)
            {
                asciiDomainLength = asciiDomainLength + 1;
                output.AppendCharacter('.');
            }

            asciiDomainLength = asciiDomainLength + asciiLabel.Length;
            output.AppendText(mapped);
            if (separatorOffset < 0)
            {
                labelOffset = normalized.Length;
            }
            else if (separatorOffset == normalized.Length - 1)
            {
                asciiDomainLength = asciiDomainLength + 1;
                output.AppendCharacter('.');
                labelOffset = normalized.Length;
            }
            else
            {
                labelOffset = separatorOffset + 1;
            }
        }

        bool trailingRoot = normalized[normalized.Length - 1] == '.';
        int maximumDomainLength = 253;
        if (trailingRoot)
        {
            maximumDomainLength = 254;
        }

        if (asciiDomainLength > maximumDomainLength)
        {
            return CreateIdnFailure(
                UriDiagnosticCode.InvalidIdnLabel,
                0,
                host.Length,
                DomainLengthMessage);
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return IriConverterCore.CreateOutputFailure(
                status.ErrorOffset,
                status.ErrorLength);
        }

        return new UriTextResult(true, output.GetText(), output.GetLength(), null);
    }

    internal static UriDiagnostic? ValidateUnicodeInput(ReadOnlySpan<char> text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.Iri,
                    UriComponent.Host,
                    scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static string NormalizeSeparators(ReadOnlySpan<char> text)
    {
        char[] normalized = new char[text.Length];
        int offset = 0;
        while (offset < text.Length)
        {
            char value = text[offset];
            if (value == '\u3002' || value == '\uFF0E' || value == '\uFF61')
            {
                normalized[offset] = '.';
            }
            else
            {
                normalized[offset] = value;
            }

            offset = offset + 1;
        }

        return new string(normalized);
    }

    internal static UriDiagnostic? ValidateUnicodeLabel(string label, int labelOffset)
    {
        if (label.Length == 0 || label[0] == '-' || label[label.Length - 1] == '-')
        {
            return CreateLabelDiagnostic(labelOffset, label.Length);
        }

        if (label.Length >= 4 &&
            label[2] == '-' &&
            label[3] == '-' &&
            !StartsWithAcePrefix(label))
        {
            return CreateLabelDiagnostic(labelOffset + 2, 2);
        }

        bool hasNonAscii = false;
        int scalarIndex = 0;
        int offset = 0;
        while (offset < label.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(label.AsSpan(), offset);
            int scalarValue = scalar.ScalarValue;
            if (scalarValue <= 127)
            {
                if (!IsAsciiLabelCharacter((char)scalarValue))
                {
                    return CreateLabelDiagnostic(labelOffset + offset, scalar.CodeUnitLength);
                }
            }
            else
            {
                hasNonAscii = true;
                if (!IsAllowedIdnScalar(label, offset, scalarIndex))
                {
                    return CreateLabelDiagnostic(labelOffset + offset, scalar.CodeUnitLength);
                }
            }

            scalarIndex = scalarIndex + 1;
            offset = offset + scalar.CodeUnitLength;
        }

        if (hasNonAscii && StartsWithAcePrefix(label))
        {
            return CreateLabelDiagnostic(labelOffset, label.Length);
        }

        if (!hasNonAscii)
        {
            return ValidateAsciiLabel(label, labelOffset);
        }

        string lengthForm = label.Normalize(System.Text.NormalizationForm.FormKC);
        if (hasNonAscii && MeasureMappedLabel(lengthForm) > 63)
        {
            return CreateLabelDiagnostic(labelOffset, label.Length);
        }

        return null;
    }

    internal static int MeasureMappedLabel(string label)
    {
        int offset = 0;
        while (offset < label.Length)
        {
            if (label[offset] > 127)
            {
                return MeasurePunycodeLabel(label);
            }

            offset = offset + 1;
        }

        return label.Length;
    }

    internal static UriDiagnostic? ValidateAsciiLabel(string label, int labelOffset)
    {
        if (label.Length == 0 || label.Length > 63 ||
            label[0] == '-' || label[label.Length - 1] == '-')
        {
            return CreateLabelDiagnostic(labelOffset, label.Length);
        }

        int offset = 0;
        while (offset < label.Length)
        {
            if (label[offset] > 127 || !IsAsciiLabelCharacter(label[offset]))
            {
                return CreateLabelDiagnostic(labelOffset + offset, 1);
            }

            offset = offset + 1;
        }

        if (label.Length >= 4 && label[2] == '-' && label[3] == '-')
        {
            if (!StartsWithAcePrefix(label) || label.Length < 6)
            {
                return CreateLabelDiagnostic(labelOffset + 2, label.Length - 2);
            }

            if (!IsValidAceLabel(label))
            {
                return CreateLabelDiagnostic(labelOffset, label.Length);
            }
        }

        return null;
    }

    internal static bool IsValidAceLabel(string label)
    {
        string payload = label.Substring(4);
        int[] scalars = new int[payload.Length];
        int scalarCount = 0;
        int delimiter = payload.LastIndexOf('-');
        int inputOffset = 0;
        if (delimiter >= 0)
        {
            while (inputOffset < delimiter)
            {
                char basic = payload[inputOffset];
                if (basic > 127 || !IsAsciiLabelCharacter(basic))
                {
                    return false;
                }

                scalars[scalarCount] = basic;
                scalarCount = scalarCount + 1;
                inputOffset = inputOffset + 1;
            }

            inputOffset = delimiter + 1;
        }

        int codePoint = 128;
        long insertion = 0;
        int bias = 72;
        while (inputOffset < payload.Length)
        {
            long previousInsertion = insertion;
            long weight = 1;
            int digitPosition = 36;
            bool complete = false;
            while (!complete)
            {
                if (inputOffset >= payload.Length)
                {
                    return false;
                }

                int digit = GetPunycodeDigit(payload[inputOffset]);
                inputOffset = inputOffset + 1;
                if (digit < 0 || digit > (long.MaxValue - insertion) / weight)
                {
                    return false;
                }

                insertion = insertion + digit * weight;
                int threshold = digitPosition - bias;
                if (digitPosition <= bias)
                {
                    threshold = 1;
                }
                else if (digitPosition >= bias + 26)
                {
                    threshold = 26;
                }

                if (digit < threshold)
                {
                    complete = true;
                }
                else
                {
                    int factor = 36 - threshold;
                    if (weight > long.MaxValue / factor)
                    {
                        return false;
                    }

                    weight = weight * factor;
                    digitPosition = digitPosition + 36;
                }
            }

            int nextCount = scalarCount + 1;
            bias = AdaptBias(
                insertion - previousInsertion,
                nextCount,
                previousInsertion == 0);
            long increment = insertion / nextCount;
            if (increment > 1114111 - codePoint)
            {
                return false;
            }

            codePoint = codePoint + (int)increment;
            int insertAt = (int)(insertion % nextCount);
            if (!UnicodeScalar.IsValid(codePoint))
            {
                return false;
            }

            int shift = scalarCount;
            while (shift > insertAt)
            {
                scalars[shift] = scalars[shift - 1];
                shift = shift - 1;
            }

            scalars[insertAt] = codePoint;
            scalarCount = nextCount;
            insertion = insertAt + 1;
        }

        if (scalarCount == 0)
        {
            return false;
        }

        int characterCount = 0;
        int index = 0;
        bool hasNonAscii = false;
        while (index < scalarCount)
        {
            characterCount = characterCount + UnicodeScalar.GetCodeUnitLength(scalars[index]);
            if (scalars[index] > 127)
            {
                hasNonAscii = true;
            }

            index = index + 1;
        }

        if (!hasNonAscii)
        {
            return false;
        }

        char[] characters = new char[characterCount];
        int characterOffset = 0;
        index = 0;
        while (index < scalarCount)
        {
            characterOffset = PercentDecoderCore.WriteScalar(
                scalars[index],
                characters,
                characterOffset);
            index = index + 1;
        }

        string decoded = new string(characters);
        return ValidateUnicodeLabel(decoded, 0) == null &&
            BidiValidator.ValidateIdnLabelText(decoded.AsSpan(), 0) == null;
    }

    internal static int GetPunycodeDigit(char value)
    {
        char folded = AsciiCase.FoldToLower(value);
        if (folded >= 'a' && folded <= 'z')
        {
            return folded - 'a';
        }

        if (value >= '0' && value <= '9')
        {
            return value - '0' + 26;
        }

        return -1;
    }

    internal static bool IsAllowedIdnScalar(string label, int offset, int scalarIndex)
    {
        UnicodeScalarResult scalar = UnicodeScalar.ReadAt(label.AsSpan(), offset);
        if (!IriGrammar.IsUcsCharacter(scalar.ScalarValue) ||
            IriConverterCore.IsBidiFormattingCharacter(scalar.ScalarValue))
        {
            return false;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(label, offset);
        bool mark = category == UnicodeCategory.NonSpacingMark ||
            category == UnicodeCategory.SpacingCombiningMark;
        if (scalarIndex == 0 && mark)
        {
            return false;
        }

        return category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.ModifierLetter ||
            category == UnicodeCategory.OtherLetter ||
            mark ||
            category == UnicodeCategory.DecimalDigitNumber;
    }

    internal static int MeasurePunycodeLabel(string label)
    {
        int[] scalars = new int[label.Length];
        int scalarCount = 0;
        int basicCount = 0;
        int offset = 0;
        while (offset < label.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(label.AsSpan(), offset);
            scalars[scalarCount] = scalar.ScalarValue;
            scalarCount = scalarCount + 1;
            if (scalar.ScalarValue < 128)
            {
                basicCount = basicCount + 1;
            }

            offset = offset + scalar.CodeUnitLength;
        }

        long length = 4 + basicCount;
        if (basicCount > 0)
        {
            length = length + 1;
        }

        int handled = basicCount;
        int codePoint = 128;
        long delta = 0;
        int bias = 72;
        while (handled < scalarCount)
        {
            int nextCodePoint = 1114111;
            int index = 0;
            while (index < scalarCount)
            {
                int value = scalars[index];
                if (value >= codePoint && value < nextCodePoint)
                {
                    nextCodePoint = value;
                }

                index = index + 1;
            }

            delta = delta + (long)(nextCodePoint - codePoint) * (handled + 1);
            codePoint = nextCodePoint;
            index = 0;
            while (index < scalarCount)
            {
                int value = scalars[index];
                if (value < codePoint)
                {
                    delta = delta + 1;
                }
                else if (value == codePoint)
                {
                    long quotient = delta;
                    int weight = 36;
                    bool complete = false;
                    while (!complete)
                    {
                        int threshold = weight - bias;
                        if (weight <= bias)
                        {
                            threshold = 1;
                        }
                        else if (weight >= bias + 26)
                        {
                            threshold = 26;
                        }

                        if (quotient < threshold)
                        {
                            complete = true;
                        }
                        else
                        {
                            length = length + 1;
                            quotient = (quotient - threshold) / (36 - threshold);
                            weight = weight + 36;
                        }
                    }

                    length = length + 1;
                    bias = AdaptBias(delta, handled + 1, handled == basicCount);
                    delta = 0;
                    handled = handled + 1;
                }

                index = index + 1;
            }

            delta = delta + 1;
            codePoint = codePoint + 1;
            if (length > int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)length;
    }

    internal static int AdaptBias(long delta, int pointCount, bool firstTime)
    {
        if (firstTime)
        {
            delta = delta / 700;
        }
        else
        {
            delta = delta / 2;
        }

        delta = delta + delta / pointCount;
        int adjustment = 0;
        while (delta > 455)
        {
            delta = delta / 35;
            adjustment = adjustment + 36;
        }

        return adjustment + (int)(36 * delta / (delta + 38));
    }

    internal static int FindSeparator(ReadOnlySpan<char> text, int startOffset)
    {
        int offset = startOffset;
        while (offset < text.Length)
        {
            if (text[offset] == '.')
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static bool IsAsciiLabelCharacter(char value)
    {
        return Ascii.IsAlphabetic(value) || Ascii.IsDigit(value) || value == '-';
    }

    internal static bool StartsWithAcePrefix(string label)
    {
        return label.Length >= 4 &&
            AsciiCase.FoldToLower(label[0]) == 'x' &&
            AsciiCase.FoldToLower(label[1]) == 'n' &&
            label[2] == '-' &&
            label[3] == '-';
    }

    internal static UriDiagnostic CreateLabelDiagnostic(int offset, int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidIdnLabel,
            UriComponent.Host,
            offset,
            length,
            InvalidLabelMessage);
    }

    internal static UriTextResult CreateIdnFailure(
        UriDiagnosticCode code,
        int offset,
        int length,
        string message)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            code,
            UriComponent.Host,
            offset,
            length,
            message);
        return IriConverterCore.CreateFailure(diagnostic);
    }
}
