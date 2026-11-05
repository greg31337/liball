using System;

namespace uri;

internal static class PercentNormalizerCore
{
    internal static PercentNormalizeResult NormalizeHexCase(
        string text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return NormalizeHexCaseSpan(
            text.AsSpan(),
            maximumOutputCharacters,
            component,
            baseOffset);
    }

    internal static PercentNormalizeResult NormalizeHexCaseSpan(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return Transform(
            text,
            maximumOutputCharacters,
            component,
            baseOffset,
            true,
            false);
    }

    internal static PercentNormalizeResult DecodeUnreserved(
        string text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return DecodeUnreservedSpan(
            text.AsSpan(),
            maximumOutputCharacters,
            component,
            baseOffset);
    }

    internal static PercentNormalizeResult DecodeUnreservedSpan(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return Transform(
            text,
            maximumOutputCharacters,
            component,
            baseOffset,
            false,
            true);
    }

    internal static PercentNormalizeResult Transform(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset,
        bool normalizeHexCase,
        bool decodeUnreserved)
    {
        UriDiagnostic? invalid = PercentScanner.ValidateFirstError(text, baseOffset, component);
        if (invalid != null)
        {
            return new PercentNormalizeResult(false, "", invalid);
        }

        int outputLength = MeasureOutput(text, decodeUnreserved);
        UriDiagnostic? limit = PercentEncoderCore.CreateOutputLimitDiagnostic(
            outputLength,
            maximumOutputCharacters);
        if (limit != null)
        {
            return new PercentNormalizeResult(false, "", limit);
        }

        char[] output = new char[outputLength];
        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < text.Length)
        {
            if (text[inputOffset] != '%')
            {
                output[outputOffset] = text[inputOffset];
                inputOffset = inputOffset + 1;
                outputOffset = outputOffset + 1;
                continue;
            }

            PercentTripletResult triplet = PercentScanner.ScanTriplet(
                text,
                inputOffset,
                baseOffset,
                component);
            if (decodeUnreserved && triplet.Value <= 127 && Ascii.IsUnreserved((char)triplet.Value))
            {
                output[outputOffset] = (char)triplet.Value;
                outputOffset = outputOffset + 1;
            }
            else
            {
                output[outputOffset] = '%';
                if (normalizeHexCase)
                {
                    output[outputOffset + 1] = Ascii.GetUpperHexCharacter(triplet.Value >> 4);
                    output[outputOffset + 2] = Ascii.GetUpperHexCharacter(triplet.Value & 15);
                }
                else
                {
                    output[outputOffset + 1] = text[inputOffset + 1];
                    output[outputOffset + 2] = text[inputOffset + 2];
                }

                outputOffset = outputOffset + 3;
            }

            inputOffset = triplet.NextOffset;
        }

        return new PercentNormalizeResult(true, new string(output), null);
    }

    internal static int MeasureOutput(ReadOnlySpan<char> text, bool decodeUnreserved)
    {
        int outputLength = 0;
        int inputOffset = 0;
        while (inputOffset < text.Length)
        {
            if (text[inputOffset] == '%')
            {
                int value = Ascii.GetHexByte(text[inputOffset + 1], text[inputOffset + 2]);
                if (decodeUnreserved && value <= 127 && Ascii.IsUnreserved((char)value))
                {
                    outputLength = outputLength + 1;
                }
                else
                {
                    outputLength = outputLength + 3;
                }

                inputOffset = inputOffset + 3;
            }
            else
            {
                outputLength = outputLength + 1;
                inputOffset = inputOffset + 1;
            }
        }

        return outputLength;
    }
}
