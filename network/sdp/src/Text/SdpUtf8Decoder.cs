using System;
using System.Text;

namespace sdp;

internal static class SdpUtf8Decoder
{
    internal static readonly Encoding Utf8 = new UTF8Encoding(false, false);

    internal static SdpUtf8DecodeResult DecodeUtf8(ReadOnlySpan<byte> input, bool allowByteOrderMark)
    {
        return ProcessUtf8(input, allowByteOrderMark, true);
    }

    internal static SdpUtf8DecodeResult ValidateUtf8(ReadOnlySpan<byte> input, bool allowByteOrderMark)
    {
        return ProcessUtf8(input, allowByteOrderMark, false);
    }

    internal static SdpUtf8DecodeResult ProcessUtf8(ReadOnlySpan<byte> input, bool allowByteOrderMark, bool decodeText)
    {
        SdpUtf8DecodeResult result = new SdpUtf8DecodeResult();
        int byteOffset = 0;
        int characterOffset = 0;
        if (input.Length >= 3 && input[0] == 239 && input[1] == 187 && input[2] == 191)
        {
            result.HasByteOrderMark = true;
            if (!allowByteOrderMark)
            {
                result.ByteOrderMarkNotAllowed = true;
                result.ByteOffset = 0;
                result.CharacterOffset = 0;
                result.DiagnosticCode = SdpDiagnosticCodes.ParseByteOrderMarkNotAllowed;
                return result;
            }
            byteOffset = 3;
        }

        int textStartOffset = byteOffset;
        while (byteOffset < input.Length)
        {
            byte first = input[byteOffset];
            if (first <= 127)
            {
                if (first == 0)
                {
                    return CreateNulResult(result, byteOffset, characterOffset);
                }
                if (IsDisallowedAsciiControl(first))
                {
                    return CreateControlResult(result, byteOffset, characterOffset);
                }
                byteOffset = byteOffset + 1;
                characterOffset = characterOffset + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (byteOffset + 1 >= input.Length || !IsContinuationByte(input[byteOffset + 1]))
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                if (first == 194 && input[byteOffset + 1] >= 128 && input[byteOffset + 1] <= 159)
                {
                    return CreateControlResult(result, byteOffset, characterOffset);
                }
                byteOffset = byteOffset + 2;
                characterOffset = characterOffset + 1;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (byteOffset + 2 >= input.Length)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                byte second = input[byteOffset + 1];
                byte third = input[byteOffset + 2];
                if (!IsContinuationByte(second) || !IsContinuationByte(third))
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                if (first == 224 && second < 160)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                if (first == 237 && second > 159)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                byteOffset = byteOffset + 3;
                characterOffset = characterOffset + 1;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (byteOffset + 3 >= input.Length)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                byte second = input[byteOffset + 1];
                byte third = input[byteOffset + 2];
                byte fourth = input[byteOffset + 3];
                if (!IsContinuationByte(second) || !IsContinuationByte(third) || !IsContinuationByte(fourth))
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                if (first == 240 && second < 144)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                if (first == 244 && second > 143)
                {
                    return CreateInvalidResult(result, byteOffset, characterOffset);
                }
                byteOffset = byteOffset + 4;
                characterOffset = characterOffset + 2;
                continue;
            }

            return CreateInvalidResult(result, byteOffset, characterOffset);
        }

        result.Success = true;
        result.BytesConsumed = input.Length;
        result.CharacterCount = characterOffset;
        if (decodeText)
        {
            result.Text = Utf8.GetString(input.Slice(textStartOffset));
        }
        return result;
    }

    internal static bool IsContinuationByte(byte value)
    {
        return value >= 128 && value <= 191;
    }

    internal static bool IsDisallowedAsciiControl(byte value)
    {
        if (value == 10 || value == 13)
        {
            return false;
        }
        return value < 32 || value == 127;
    }

    internal static SdpUtf8DecodeResult CreateInvalidResult(SdpUtf8DecodeResult result, int byteOffset, int characterOffset)
    {
        result.InvalidUtf8 = true;
        result.ByteOffset = byteOffset;
        result.CharacterOffset = characterOffset;
        result.BytesConsumed = byteOffset;
        result.CharacterCount = characterOffset;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseInvalidUtf8;
        return result;
    }

    internal static SdpUtf8DecodeResult CreateNulResult(SdpUtf8DecodeResult result, int byteOffset, int characterOffset)
    {
        result.NulCharacterNotAllowed = true;
        result.ByteOffset = byteOffset;
        result.CharacterOffset = characterOffset;
        result.BytesConsumed = byteOffset;
        result.CharacterCount = characterOffset;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseNulCharacterNotAllowed;
        return result;
    }

    internal static SdpUtf8DecodeResult CreateControlResult(SdpUtf8DecodeResult result, int byteOffset, int characterOffset)
    {
        result.ControlCharacterNotAllowed = true;
        result.ByteOffset = byteOffset;
        result.CharacterOffset = characterOffset;
        result.BytesConsumed = byteOffset;
        result.CharacterCount = characterOffset;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseControlCharacterNotAllowed;
        return result;
    }
}
