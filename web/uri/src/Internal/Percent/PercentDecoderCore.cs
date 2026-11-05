using System;

namespace uri;

internal static class PercentDecoderCore
{
    internal static PercentByteDecodeResult DecodeBytes(
        string text,
        int maximumOutputBytes,
        UriComponent component,
        int baseOffset)
    {
        return DecodeSpan(text.AsSpan(), maximumOutputBytes, component, baseOffset);
    }

    internal static PercentByteDecodeResult DecodeSpan(
        ReadOnlySpan<char> text,
        int maximumOutputBytes,
        UriComponent component,
        int baseOffset)
    {
        PercentByteMeasureResult measurement = MeasureDecodedBytes(text, component, baseOffset);
        if (!measurement.Success)
        {
            return new PercentByteDecodeResult(false, new byte[0], measurement.Diagnostic);
        }

        UriDiagnostic? limit = CreateOutputLimitDiagnostic(measurement.Length, maximumOutputBytes);
        if (limit != null)
        {
            return new PercentByteDecodeResult(false, new byte[0], limit);
        }

        byte[] output = new byte[measurement.Length];
        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < text.Length)
        {
            if (text[inputOffset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    inputOffset,
                    baseOffset,
                    component);
                output[outputOffset] = (byte)triplet.Value;
                inputOffset = triplet.NextOffset;
            }
            else
            {
                output[outputOffset] = (byte)text[inputOffset];
                inputOffset = inputOffset + 1;
            }

            outputOffset = outputOffset + 1;
        }

        return new PercentByteDecodeResult(true, output, null);
    }

    internal static PercentTextDecodeResult DecodeTextStrict(
        string text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return DecodeTextSpanStrict(
            text.AsSpan(),
            maximumOutputCharacters,
            component,
            baseOffset);
    }

    internal static PercentTextDecodeResult DecodeTextSpanStrict(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        PercentByteDecodeResult byteResult = DecodeSpan(
            text,
            text.Length,
            component,
            baseOffset);
        if (!byteResult.Success)
        {
            return new PercentTextDecodeResult(false, "", false, byteResult.Diagnostic);
        }

        Utf8DecodeResult textResult = Utf8CodecCore.DecodeBytes(byteResult.Bytes);
        if (!textResult.Success)
        {
            UriDiagnostic invalidUtf8 = CreateInvalidUtf8Diagnostic(
                text,
                textResult.ErrorOffset,
                textResult.ErrorLength,
                component,
                baseOffset);
            return new PercentTextDecodeResult(false, "", false, invalidUtf8);
        }

        UriDiagnostic? limit = PercentEncoderCore.CreateOutputLimitDiagnostic(
            textResult.Text.Length,
            maximumOutputCharacters);
        if (limit != null)
        {
            return new PercentTextDecodeResult(false, "", false, limit);
        }

        return new PercentTextDecodeResult(true, textResult.Text, false, null);
    }

    internal static PercentTextDecodeResult DecodeTextWithReplacement(
        string text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return DecodeTextSpanWithReplacement(
            text.AsSpan(),
            maximumOutputCharacters,
            component,
            baseOffset);
    }

    internal static PercentTextDecodeResult DecodeTextSpanWithReplacement(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        PercentByteDecodeResult byteResult = DecodeSpan(
            text,
            text.Length,
            component,
            baseOffset);
        if (!byteResult.Success)
        {
            return new PercentTextDecodeResult(false, "", false, byteResult.Diagnostic);
        }

        PercentReplacementMeasureResult measurement = MeasureTextWithReplacement(byteResult.Bytes);
        UriDiagnostic? limit = PercentEncoderCore.CreateOutputLimitDiagnostic(
            measurement.Length,
            maximumOutputCharacters);
        if (limit != null)
        {
            return new PercentTextDecodeResult(false, "", measurement.UsedReplacement, limit);
        }

        char[] output = new char[(int)measurement.Length];
        int byteOffset = 0;
        int outputOffset = 0;
        bool usedReplacement = false;
        while (byteOffset < byteResult.Bytes.Length)
        {
            Utf8ScalarResult scalar = Utf8CodecCore.DecodeScalar(byteResult.Bytes, byteOffset);
            if (scalar.Success)
            {
                outputOffset = WriteScalar(scalar.ScalarValue, output, outputOffset);
                byteOffset = byteOffset + scalar.ByteLength;
            }
            else
            {
                output[outputOffset] = (char)65533;
                outputOffset = outputOffset + 1;
                byteOffset = byteOffset + GetInvalidSequenceLength(byteResult.Bytes, byteOffset);
                usedReplacement = true;
            }
        }

        return new PercentTextDecodeResult(true, new string(output), usedReplacement, null);
    }

    internal static PercentByteMeasureResult MeasureDecodedBytes(
        ReadOnlySpan<char> text,
        UriComponent component,
        int baseOffset)
    {
        int inputOffset = 0;
        int outputLength = 0;
        while (inputOffset < text.Length)
        {
            char value = text[inputOffset];
            if (value == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    inputOffset,
                    baseOffset,
                    component);
                if (!triplet.Success)
                {
                    return new PercentByteMeasureResult(false, 0, triplet.Diagnostic);
                }

                inputOffset = triplet.NextOffset;
            }
            else
            {
                if (value > 127)
                {
                    UriDiagnostic invalidCharacter = DiagnosticFactory.CreateInvalidCharacter(
                        DiagnosticDomain.PercentEncoding,
                        component,
                        PercentScanner.GetAbsoluteOffset(baseOffset, inputOffset));
                    return new PercentByteMeasureResult(false, 0, invalidCharacter);
                }

                inputOffset = inputOffset + 1;
            }

            outputLength = outputLength + 1;
        }

        return new PercentByteMeasureResult(true, outputLength, null);
    }

    internal static PercentReplacementMeasureResult MeasureTextWithReplacement(ReadOnlySpan<byte> bytes)
    {
        long outputLength = 0;
        bool usedReplacement = false;
        int byteOffset = 0;
        while (byteOffset < bytes.Length)
        {
            Utf8ScalarResult scalar = Utf8CodecCore.DecodeScalar(bytes, byteOffset);
            if (scalar.Success)
            {
                outputLength = outputLength + UnicodeScalar.GetCodeUnitLength(scalar.ScalarValue);
                byteOffset = byteOffset + scalar.ByteLength;
            }
            else
            {
                outputLength = outputLength + 1;
                byteOffset = byteOffset + GetInvalidSequenceLength(bytes, byteOffset);
                usedReplacement = true;
            }
        }

        return new PercentReplacementMeasureResult(outputLength, usedReplacement);
    }

    internal static int GetInvalidSequenceLength(ReadOnlySpan<byte> bytes, int offset)
    {
        byte first = bytes[offset];
        int expectedLength = 1;
        if (first >= 194 && first <= 223)
        {
            expectedLength = 2;
        }
        else if (first >= 224 && first <= 239)
        {
            expectedLength = 3;
        }
        else if (first >= 240 && first <= 244)
        {
            expectedLength = 4;
        }

        if (expectedLength == 1)
        {
            return 1;
        }

        int length = 1;
        while (length < expectedLength && offset + length < bytes.Length)
        {
            byte value = bytes[offset + length];
            if (!Utf8CodecCore.IsContinuationByte(value))
            {
                return length;
            }

            if (length == 1)
            {
                if (first == 224 && value < 160)
                {
                    return 1;
                }

                if (first == 237 && value >= 160)
                {
                    return 1;
                }

                if (first == 240 && value < 144)
                {
                    return 1;
                }

                if (first == 244 && value > 143)
                {
                    return 1;
                }
            }

            length = length + 1;
        }

        return length;
    }

    internal static UriDiagnostic CreateInvalidUtf8Diagnostic(
        ReadOnlySpan<char> text,
        int errorByteOffset,
        int errorByteLength,
        UriComponent component,
        int baseOffset)
    {
        int sourceOffset = GetSourceOffset(text, errorByteOffset);
        int sourceEnd = GetSourceOffset(text, errorByteOffset + errorByteLength);
        return DiagnosticFactory.CreateInvalidUtf8(
            component,
            PercentScanner.GetAbsoluteOffset(baseOffset, sourceOffset),
            sourceEnd - sourceOffset);
    }

    internal static int GetSourceOffset(ReadOnlySpan<char> text, int byteOffset)
    {
        int inputOffset = 0;
        int currentByteOffset = 0;
        while (inputOffset < text.Length && currentByteOffset < byteOffset)
        {
            if (text[inputOffset] == '%')
            {
                inputOffset = inputOffset + 3;
            }
            else
            {
                inputOffset = inputOffset + 1;
            }

            currentByteOffset = currentByteOffset + 1;
        }

        return inputOffset;
    }

    internal static int WriteScalar(int scalarValue, char[] output, int offset)
    {
        if (scalarValue <= 65535)
        {
            output[offset] = (char)scalarValue;
            return offset + 1;
        }

        int supplementaryValue = scalarValue - 65536;
        output[offset] = (char)(55296 + supplementaryValue / 1024);
        output[offset + 1] = (char)(56320 + supplementaryValue % 1024);
        return offset + 2;
    }

    internal static UriDiagnostic? CreateOutputLimitDiagnostic(
        int outputLength,
        int maximumOutputBytes)
    {
        if (maximumOutputBytes >= 0 && outputLength <= maximumOutputBytes)
        {
            return null;
        }

        int errorOffset = 0;
        int errorLength = 0;
        if (maximumOutputBytes >= 0)
        {
            errorOffset = maximumOutputBytes;
            errorLength = outputLength - maximumOutputBytes;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            errorOffset,
            errorLength,
            LimitGuard.OutputLimitMessage);
    }
}
