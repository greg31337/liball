using System;

namespace html;

internal enum Iso2022JpState
{
    Ascii = 0,
    Roman = 1,
    Katakana = 2,
    Lead = 3
}

internal static class HtmlLegacyByteDecoder
{
    internal static HtmlDecodedInput Decode(ReadOnlySpan<byte> input, string encodingName, int start)
    {
        if (WebEncodingIndexes.GetSingleByteCodePoint(encodingName, 0) != -2)
        {
            return DecodeSingleByte(input, encodingName, start);
        }

        if (encodingName == "GBK" || encodingName == "gb18030")
        {
            return DecodeGb18030(input, encodingName, start);
        }

        if (encodingName == "Big5")
        {
            return DecodeBig5(input, start);
        }

        if (encodingName == "EUC-JP")
        {
            return DecodeEucJp(input, start);
        }

        if (encodingName == "ISO-2022-JP")
        {
            return DecodeIso2022Jp(input, start);
        }

        if (encodingName == "Shift_JIS")
        {
            return DecodeShiftJis(input, start);
        }

        if (encodingName == "EUC-KR")
        {
            return DecodeEucKr(input, start);
        }

        if (encodingName == "replacement")
        {
            return DecodeReplacement(input, start);
        }

        if (encodingName == "x-user-defined")
        {
            return DecodeUserDefined(input, start);
        }

        return Unsupported(input.Length, encodingName);
    }

    internal static HtmlDecodedInput DecodeSingleByte(ReadOnlySpan<byte> input, string encodingName, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder(encodingName, input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte value = input[position];
            if (value < 0x80)
            {
                builder.AppendScalar(value, position, position + 1);
            }
            else
            {
                int scalar = WebEncodingIndexes.GetSingleByteCodePoint(encodingName, value - 0x80);
                if (scalar < 0)
                {
                    builder.AppendError(position, position + 1);
                }
                else
                {
                    builder.AppendScalar(scalar, position, position + 1);
                }
            }

            position = position + 1;
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeGb18030(ReadOnlySpan<byte> input, string encodingName, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder(encodingName, input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first < 0x80)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first == 0x80)
            {
                builder.AppendScalar(0x20AC, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first < 0x81 || first > 0xFE)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
                continue;
            }

            if (position + 1 >= input.Length)
            {
                builder.AppendError(position, input.Length);
                position = input.Length;
                continue;
            }

            byte second = input[position + 1];
            if (second >= 0x30 && second <= 0x39)
            {
                if (position + 2 >= input.Length)
                {
                    builder.AppendError(position, input.Length);
                    position = input.Length;
                    continue;
                }

                byte third = input[position + 2];
                if (third < 0x81 || third > 0xFE)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    continue;
                }

                if (position + 3 >= input.Length)
                {
                    builder.AppendError(position, input.Length);
                    position = input.Length;
                    continue;
                }

                byte fourth = input[position + 3];
                if (fourth < 0x30 || fourth > 0x39)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    continue;
                }

                int pointer = (first - 0x81) * 12600 + (second - 0x30) * 1260
                    + (third - 0x81) * 10 + fourth - 0x30;
                int scalar = WebEncodingIndexes.GetGb18030RangeCodePoint(pointer);
                if (scalar < 0)
                {
                    builder.AppendError(position, position + 4);
                }
                else
                {
                    builder.AppendScalar(scalar, position, position + 4);
                }

                position = position + 4;
                continue;
            }

            int offset = second < 0x7F ? 0x40 : 0x41;
            int twoBytePointer = -1;
            if (second >= 0x40 && second <= 0x7E || second >= 0x80 && second <= 0xFE)
            {
                twoBytePointer = (first - 0x81) * 190 + second - offset;
            }

            int twoByteScalar = WebEncodingIndexes.GetGb18030CodePoint(twoBytePointer);
            if (twoByteScalar >= 0)
            {
                builder.AppendScalar(twoByteScalar, position, position + 2);
                position = position + 2;
            }
            else if (second < 0x80)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
            }
            else
            {
                builder.AppendError(position, position + 2);
                position = position + 2;
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeBig5(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("Big5", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first < 0x80)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first < 0x81 || first > 0xFE)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
                continue;
            }

            if (position + 1 >= input.Length)
            {
                builder.AppendError(position, input.Length);
                position = input.Length;
                continue;
            }

            byte second = input[position + 1];
            int offset = second < 0x7F ? 0x40 : 0x62;
            int pointer = -1;
            if (second >= 0x40 && second <= 0x7E || second >= 0xA1 && second <= 0xFE)
            {
                pointer = (first - 0x81) * 157 + second - offset;
            }

            if (pointer == 1133)
            {
                builder.AppendTwoCharacters('\u00CA', '\u0304', position, position + 2);
                position = position + 2;
                continue;
            }

            if (pointer == 1135)
            {
                builder.AppendTwoCharacters('\u00CA', '\u030C', position, position + 2);
                position = position + 2;
                continue;
            }

            if (pointer == 1164)
            {
                builder.AppendTwoCharacters('\u00EA', '\u0304', position, position + 2);
                position = position + 2;
                continue;
            }

            if (pointer == 1166)
            {
                builder.AppendTwoCharacters('\u00EA', '\u030C', position, position + 2);
                position = position + 2;
                continue;
            }

            int scalar = WebEncodingIndexes.GetBig5CodePoint(pointer);
            if (scalar >= 0)
            {
                builder.AppendScalar(scalar, position, position + 2);
                position = position + 2;
            }
            else if (second < 0x80)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
            }
            else
            {
                builder.AppendError(position, position + 2);
                position = position + 2;
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeEucJp(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("EUC-JP", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first < 0x80)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first == 0x8E)
            {
                position = DecodeEucJpKatakana(input, builder, position);
                continue;
            }

            if (first == 0x8F)
            {
                position = DecodeEucJpJis0212(input, builder, position);
                continue;
            }

            if (first >= 0xA1 && first <= 0xFE)
            {
                position = DecodeEucJpPair(input, builder, position);
                continue;
            }

            builder.AppendError(position, position + 1);
            position = position + 1;
        }

        return builder.Finish();
    }

    internal static int DecodeEucJpKatakana(ReadOnlySpan<byte> input, HtmlDecodedInputBuilder builder, int position)
    {
        if (position + 1 >= input.Length)
        {
            builder.AppendError(position, input.Length);
            return input.Length;
        }

        byte second = input[position + 1];
        if (second >= 0xA1 && second <= 0xDF)
        {
            builder.AppendScalar(0xFF61 - 0xA1 + second, position, position + 2);
            return position + 2;
        }

        if (second < 0x80)
        {
            builder.AppendError(position, position + 1);
            return position + 1;
        }

        builder.AppendError(position, position + 2);
        return position + 2;
    }

    internal static int DecodeEucJpJis0212(ReadOnlySpan<byte> input, HtmlDecodedInputBuilder builder, int position)
    {
        if (position + 1 >= input.Length)
        {
            builder.AppendError(position, input.Length);
            return input.Length;
        }

        byte second = input[position + 1];
        if (second < 0xA1 || second > 0xFE)
        {
            if (second < 0x80)
            {
                builder.AppendError(position, position + 1);
                return position + 1;
            }

            builder.AppendError(position, position + 2);
            return position + 2;
        }

        if (position + 2 >= input.Length)
        {
            builder.AppendError(position, input.Length);
            return input.Length;
        }

        byte third = input[position + 2];
        if (third < 0xA1 || third > 0xFE)
        {
            if (third < 0x80)
            {
                builder.AppendError(position, position + 2);
                return position + 2;
            }

            builder.AppendError(position, position + 3);
            return position + 3;
        }

        int pointer = (second - 0xA1) * 94 + third - 0xA1;
        int scalar = WebEncodingIndexes.GetJis0212CodePoint(pointer);
        if (scalar < 0)
        {
            builder.AppendError(position, position + 3);
        }
        else
        {
            builder.AppendScalar(scalar, position, position + 3);
        }

        return position + 3;
    }

    internal static int DecodeEucJpPair(ReadOnlySpan<byte> input, HtmlDecodedInputBuilder builder, int position)
    {
        if (position + 1 >= input.Length)
        {
            builder.AppendError(position, input.Length);
            return input.Length;
        }

        byte first = input[position];
        byte second = input[position + 1];
        int scalar = -1;
        if (second >= 0xA1 && second <= 0xFE)
        {
            scalar = WebEncodingIndexes.GetJis0208CodePoint((first - 0xA1) * 94 + second - 0xA1);
        }

        if (scalar >= 0)
        {
            builder.AppendScalar(scalar, position, position + 2);
            return position + 2;
        }

        if (second < 0x80)
        {
            builder.AppendError(position, position + 1);
            return position + 1;
        }

        builder.AppendError(position, position + 2);
        return position + 2;
    }

    internal static HtmlDecodedInput DecodeShiftJis(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("Shift_JIS", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first <= 0x80)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first >= 0xA1 && first <= 0xDF)
            {
                builder.AppendScalar(0xFF61 - 0xA1 + first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (!(first >= 0x81 && first <= 0x9F || first >= 0xE0 && first <= 0xFC))
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
                continue;
            }

            if (position + 1 >= input.Length)
            {
                builder.AppendError(position, input.Length);
                position = input.Length;
                continue;
            }

            byte second = input[position + 1];
            int offset = second < 0x7F ? 0x40 : 0x41;
            int leadingOffset = first < 0xA0 ? 0x81 : 0xC1;
            int pointer = -1;
            if (second >= 0x40 && second <= 0x7E || second >= 0x80 && second <= 0xFC)
            {
                pointer = (first - leadingOffset) * 188 + second - offset;
            }

            int scalar = -1;
            if (pointer >= 8836 && pointer <= 10715)
            {
                scalar = 0xE000 - 8836 + pointer;
            }
            else
            {
                scalar = WebEncodingIndexes.GetJis0208CodePoint(pointer);
            }

            if (scalar >= 0)
            {
                builder.AppendScalar(scalar, position, position + 2);
                position = position + 2;
            }
            else if (second < 0x80)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
            }
            else
            {
                builder.AppendError(position, position + 2);
                position = position + 2;
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeEucKr(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("EUC-KR", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            byte first = input[position];
            if (first < 0x80)
            {
                builder.AppendScalar(first, position, position + 1);
                position = position + 1;
                continue;
            }

            if (first < 0x81 || first > 0xFE)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
                continue;
            }

            if (position + 1 >= input.Length)
            {
                builder.AppendError(position, input.Length);
                position = input.Length;
                continue;
            }

            byte second = input[position + 1];
            int pointer = -1;
            if (second >= 0x41 && second <= 0xFE)
            {
                pointer = (first - 0x81) * 190 + second - 0x41;
            }

            int scalar = WebEncodingIndexes.GetEucKrCodePoint(pointer);
            if (scalar >= 0)
            {
                builder.AppendScalar(scalar, position, position + 2);
                position = position + 2;
            }
            else if (second < 0x80)
            {
                builder.AppendError(position, position + 1);
                position = position + 1;
            }
            else
            {
                builder.AppendError(position, position + 2);
                position = position + 2;
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeIso2022Jp(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("ISO-2022-JP", input.Length, start);
        Iso2022JpState state = Iso2022JpState.Ascii;
        bool output = false;
        int position = start;
        while (position < input.Length)
        {
            byte value = input[position];
            if (value == 0x1B)
            {
                int escapeEnd = position + 3;
                if (position + 1 >= input.Length || input[position + 1] != 0x24 && input[position + 1] != 0x28
                    || position + 2 >= input.Length)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    output = false;
                    continue;
                }

                byte leading = input[position + 1];
                byte trailing = input[position + 2];
                Iso2022JpState nextState = state;
                bool valid = true;
                if (leading == 0x28 && trailing == 0x42)
                {
                    nextState = Iso2022JpState.Ascii;
                }
                else if (leading == 0x28 && trailing == 0x4A)
                {
                    nextState = Iso2022JpState.Roman;
                }
                else if (leading == 0x28 && trailing == 0x49)
                {
                    nextState = Iso2022JpState.Katakana;
                }
                else if (leading == 0x24 && (trailing == 0x40 || trailing == 0x42))
                {
                    nextState = Iso2022JpState.Lead;
                }
                else
                {
                    valid = false;
                }

                if (!valid)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    output = false;
                    continue;
                }

                state = nextState;
                if (output)
                {
                    builder.AppendError(position, escapeEnd);
                }
                else
                {
                    builder.SkipBytes(position, escapeEnd);
                }

                output = true;
                position = escapeEnd;
                continue;
            }

            if (state == Iso2022JpState.Lead)
            {
                if (value < 0x21 || value > 0x7E)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    output = false;
                    continue;
                }

                if (position + 1 >= input.Length)
                {
                    builder.AppendError(position, input.Length);
                    position = input.Length;
                    output = false;
                    continue;
                }

                byte trailing = input[position + 1];
                if (trailing == 0x1B)
                {
                    builder.AppendError(position, position + 1);
                    position = position + 1;
                    output = false;
                    continue;
                }

                int scalar = -1;
                if (trailing >= 0x21 && trailing <= 0x7E)
                {
                    scalar = WebEncodingIndexes.GetJis0208CodePoint((value - 0x21) * 94 + trailing - 0x21);
                }

                if (scalar < 0)
                {
                    builder.AppendError(position, position + 2);
                }
                else
                {
                    builder.AppendScalar(scalar, position, position + 2);
                }

                position = position + 2;
                output = false;
                continue;
            }

            int codePoint = -1;
            if (state == Iso2022JpState.Ascii)
            {
                if (value < 0x80 && value != 0x0E && value != 0x0F)
                {
                    codePoint = value;
                }
            }
            else if (state == Iso2022JpState.Roman)
            {
                if (value == 0x5C)
                {
                    codePoint = 0x00A5;
                }
                else if (value == 0x7E)
                {
                    codePoint = 0x203E;
                }
                else if (value < 0x80 && value != 0x0E && value != 0x0F)
                {
                    codePoint = value;
                }
            }
            else if (value >= 0x21 && value <= 0x5F)
            {
                codePoint = 0xFF61 - 0x21 + value;
            }

            if (codePoint < 0)
            {
                builder.AppendError(position, position + 1);
            }
            else
            {
                builder.AppendScalar(codePoint, position, position + 1);
            }

            position = position + 1;
            output = false;
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeReplacement(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("replacement", input.Length, start);
        if (start < input.Length)
        {
            builder.AppendError(start, start + 1);
            if (start + 1 < input.Length)
            {
                builder.SkipBytes(start + 1, input.Length);
            }
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput DecodeUserDefined(ReadOnlySpan<byte> input, int start)
    {
        HtmlDecodedInputBuilder builder = new HtmlDecodedInputBuilder("x-user-defined", input.Length, start);
        int position = start;
        while (position < input.Length)
        {
            int scalar = input[position];
            if (scalar >= 0x80)
            {
                scalar = 0xF780 + scalar - 0x80;
            }

            builder.AppendScalar(scalar, position, position + 1);
            position = position + 1;
        }

        return builder.Finish();
    }

    internal static HtmlDecodedInput Unsupported(int byteCount, string encodingName)
    {
        if (byteCount == int.MaxValue)
        {
            throw new InsufficientMemoryException("The byte-to-character map is too large.");
        }

        return new HtmlDecodedInput(false, encodingName, "", byteCount, new int[] { 0 },
            new int[byteCount + 1], new HtmlDecodingError[0]);
    }
}
