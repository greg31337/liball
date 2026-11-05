using System;

namespace uri;

internal static class PercentEncoderCore
{
    internal static PercentEncodeResult EncodeBytes(
        ReadOnlySpan<byte> bytes,
        PercentEncodeSet encodeSet,
        int maximumOutputCharacters)
    {
        long measuredLength = MeasureEncodedBytes(bytes, encodeSet);
        UriDiagnostic? limit = CreateOutputLimitDiagnostic(measuredLength, maximumOutputCharacters);
        if (limit != null)
        {
            return new PercentEncodeResult(false, "", limit);
        }

        char[] output = new char[(int)measuredLength];
        int outputOffset = 0;
        int byteOffset = 0;
        while (byteOffset < bytes.Length)
        {
            byte value = bytes[byteOffset];
            if (value <= 127 && PercentAllowlist.IsAllowed((char)value, encodeSet))
            {
                output[outputOffset] = (char)value;
                outputOffset = outputOffset + 1;
            }
            else
            {
                outputOffset = WritePercentByte(value, output, outputOffset);
            }

            byteOffset = byteOffset + 1;
        }

        return new PercentEncodeResult(true, new string(output), null);
    }

    internal static PercentEncodeResult EncodeText(
        string text,
        PercentEncodeSet encodeSet,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return EncodeSpan(text.AsSpan(), encodeSet, maximumOutputCharacters, component, baseOffset);
    }

    internal static PercentEncodeResult EncodeSpan(
        ReadOnlySpan<char> text,
        PercentEncodeSet encodeSet,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        PercentTextMeasureResult measurement = MeasureEncodedText(text, encodeSet, component, baseOffset);
        if (!measurement.Success)
        {
            return new PercentEncodeResult(false, "", measurement.Diagnostic);
        }

        UriDiagnostic? limit = CreateOutputLimitDiagnostic(measurement.Length, maximumOutputCharacters);
        if (limit != null)
        {
            return new PercentEncodeResult(false, "", limit);
        }

        char[] output = new char[(int)measurement.Length];
        byte[] scalarBytes = new byte[4];
        int outputOffset = 0;
        int textOffset = 0;
        while (textOffset < text.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, textOffset);
            if (scalar.ScalarValue <= 127 &&
                PercentAllowlist.IsAllowed((char)scalar.ScalarValue, encodeSet))
            {
                output[outputOffset] = (char)scalar.ScalarValue;
                outputOffset = outputOffset + 1;
            }
            else
            {
                int byteCount = Utf8CodecCore.WriteScalar(scalar.ScalarValue, scalarBytes, 0);
                int byteOffset = 0;
                while (byteOffset < byteCount)
                {
                    outputOffset = WritePercentByte(scalarBytes[byteOffset], output, outputOffset);
                    byteOffset = byteOffset + 1;
                }
            }

            textOffset = textOffset + scalar.CodeUnitLength;
        }

        return new PercentEncodeResult(true, new string(output), null);
    }

    internal static long MeasureEncodedBytes(ReadOnlySpan<byte> bytes, PercentEncodeSet encodeSet)
    {
        long outputLength = 0;
        int offset = 0;
        while (offset < bytes.Length)
        {
            byte value = bytes[offset];
            if (value <= 127 && PercentAllowlist.IsAllowed((char)value, encodeSet))
            {
                outputLength = outputLength + 1;
            }
            else
            {
                outputLength = outputLength + 3;
            }

            offset = offset + 1;
        }

        return outputLength;
    }

    internal static PercentTextMeasureResult MeasureEncodedText(
        ReadOnlySpan<char> text,
        PercentEncodeSet encodeSet,
        UriComponent component,
        int baseOffset)
    {
        long outputLength = 0;
        int textOffset = 0;
        while (textOffset < text.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, textOffset);
            if (!scalar.Success)
            {
                UriDiagnostic invalidUnicode = DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.PercentEncoding,
                    component,
                    PercentScanner.GetAbsoluteOffset(baseOffset, scalar.ErrorOffset),
                    scalar.ErrorLength);
                return new PercentTextMeasureResult(false, 0, invalidUnicode);
            }

            if (scalar.ScalarValue <= 127 &&
                PercentAllowlist.IsAllowed((char)scalar.ScalarValue, encodeSet))
            {
                outputLength = outputLength + 1;
            }
            else
            {
                outputLength = outputLength + Utf8CodecCore.GetEncodedLength(scalar.ScalarValue) * 3;
            }

            textOffset = textOffset + scalar.CodeUnitLength;
        }

        return new PercentTextMeasureResult(true, outputLength, null);
    }

    internal static UriDiagnostic? CreateOutputLimitDiagnostic(long outputLength, int maximumOutputCharacters)
    {
        if (maximumOutputCharacters >= 0 && outputLength <= maximumOutputCharacters)
        {
            return null;
        }

        int errorOffset = 0;
        int errorLength = 0;
        if (maximumOutputCharacters >= 0)
        {
            errorOffset = maximumOutputCharacters;
            long excess = outputLength - maximumOutputCharacters;
            if (excess > int.MaxValue)
            {
                errorLength = int.MaxValue;
            }
            else
            {
                errorLength = (int)excess;
            }
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            errorOffset,
            errorLength,
            LimitGuard.OutputLimitMessage);
    }

    internal static int WritePercentByte(byte value, char[] output, int offset)
    {
        output[offset] = '%';
        output[offset + 1] = Ascii.GetUpperHexCharacter(value >> 4);
        output[offset + 2] = Ascii.GetUpperHexCharacter(value & 15);
        return offset + 3;
    }
}
