using System.Numerics;
using System.Text;

namespace cbor;

internal class CborTokenWritePlan
{
    internal int MajorType;
    internal ulong Argument;
    internal int Required;
    internal byte[]? BytePayload;
    internal string? TextPayload;
    internal CborFloatWidth FloatWidth;
    internal ulong FloatBits;
    internal bool IsFloat;
    internal bool IsStart;
    internal bool IsEnd;
    internal bool IsChunk;
    internal bool IsIndefinite;
    internal bool WritesBreak;
    internal bool CountsAsItem;
    internal bool UsesRequestedHeadWidth;
    internal CborTokenKind TokenKind;
    internal CborLengthKind LengthKind;
    internal int RequestedHeadArgumentWidth;
    internal CborRepresentation? Representation;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal static CborTokenWritePlan Create(CborToken token)
    {
        CborTokenWritePlan plan = new CborTokenWritePlan();
        if (token == null)
        {
            plan.Fail(
                "CBOR_TOKEN_MISSING",
                "A token is required for token output."
            );
            return plan;
        }

        switch (token.Kind)
        {
            case CborTokenKind.UnsignedInteger:
                plan.CreateUnsignedInteger(token.IntegerValue);
                break;
            case CborTokenKind.NegativeInteger:
                plan.CreateNegativeInteger(token.IntegerValue);
                break;
            case CborTokenKind.ByteString:
                plan.CreateByteString(token);
                break;
            case CborTokenKind.TextString:
                plan.CreateTextString(token);
                break;
            case CborTokenKind.StartByteString:
                plan.CreateIndefiniteStringStart(token, 2);
                break;
            case CborTokenKind.ByteStringChunk:
                plan.CreateByteStringChunk(token);
                break;
            case CborTokenKind.EndByteString:
                plan.CreateContainerEnd(token);
                break;
            case CborTokenKind.StartTextString:
                plan.CreateIndefiniteStringStart(token, 3);
                break;
            case CborTokenKind.TextStringChunk:
                plan.CreateTextStringChunk(token);
                break;
            case CborTokenKind.EndTextString:
                plan.CreateContainerEnd(token);
                break;
            case CborTokenKind.SimpleValue:
                plan.CreateSimpleValue(token.SimpleValue);
                break;
            case CborTokenKind.Float:
                plan.CreateFloat(token);
                break;
            case CborTokenKind.StartArray:
                plan.CreateContainerStart(token, 4);
                break;
            case CborTokenKind.EndArray:
                plan.CreateContainerEnd(token);
                break;
            case CborTokenKind.StartMap:
                plan.CreateContainerStart(token, 5);
                break;
            case CborTokenKind.EndMap:
                plan.CreateContainerEnd(token);
                break;
            case CborTokenKind.Tag:
                plan.CreateTag(token);
                break;
            default:
                plan.Fail(
                    "CBOR_TOKEN_KIND_UNSUPPORTED",
                    "The token kind is not a scalar token supported by this writer stage."
                );
                break;
        }
        plan.TokenKind = token.Kind;
        plan.LengthKind = token.LengthKind;
        plan.Representation = token.Representation;
        if (plan.ErrorCode == null && !plan.IsEnd && !plan.IsChunk)
        {
            plan.CountsAsItem = true;
        }
        return plan;
    }

    internal void ApplyRepresentation()
    {
        if (Representation == null || ErrorCode != null || IsFloat || IsEnd)
        {
            return;
        }
        int width = Representation.HeadArgumentWidth;
        if (IsIndefinite)
        {
            if (width != 0)
            {
                Fail(
                    "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
                    "An indefinite-length head cannot have an argument width."
                );
            }
            return;
        }
        bool validWidth = width == 0
            || width == 1
            || width == 2
            || width == 4
            || width == 8;
        bool fits = (width == 0 && Argument < 24)
            || (width == 1 && Argument <= byte.MaxValue)
            || (width == 2 && Argument <= ushort.MaxValue)
            || (width == 4 && Argument <= uint.MaxValue)
            || width == 8;
        if (!validWidth || !fits)
        {
            Fail(
                "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
                "The retained argument width cannot represent the current value."
            );
            return;
        }
        if (TokenKind == CborTokenKind.SimpleValue
            && width == 1
            && Argument < 32)
        {
            Fail(
                "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
                "Simple values below 32 cannot use an extended argument head."
            );
            return;
        }
        int currentHeadSize = CborBinaryPrimitives.GetHeadSize(Argument);
        int payloadSize = Required - currentHeadSize;
        if (payloadSize > int.MaxValue - width - 1)
        {
            Fail(
                "CBOR_OUTPUT_SIZE_OUT_OF_RANGE",
                "The retained head width makes the encoded value too large."
            );
            return;
        }
        Required = payloadSize + width + 1;
        UsesRequestedHeadWidth = true;
        RequestedHeadArgumentWidth = width;
    }

    internal void CreateContainerStart(CborToken token, int majorType)
    {
        if (token.LengthKind == CborLengthKind.Definite)
        {
            MajorType = majorType;
            Argument = token.Length;
            Required = CborBinaryPrimitives.GetHeadSize(token.Length);
            IsStart = true;
            return;
        }
        if (token.LengthKind != CborLengthKind.Indefinite)
        {
            Fail(
                "CBOR_CONTAINER_LENGTH_KIND_INVALID",
                "A container start token must declare a definite or indefinite length."
            );
            return;
        }
        MajorType = majorType;
        Required = 1;
        IsStart = true;
        IsIndefinite = true;
    }

    internal void CreateContainerEnd(CborToken token)
    {
        IsEnd = true;
    }

    internal void CreateIndefiniteStringStart(CborToken token, int majorType)
    {
        if (token.LengthKind != CborLengthKind.Indefinite)
        {
            Fail(
                "CBOR_STRING_LENGTH_KIND_INVALID",
                "An indefinite string start token must declare an indefinite length."
            );
            return;
        }
        MajorType = majorType;
        Required = 1;
        IsStart = true;
        IsIndefinite = true;
    }

    internal void CreateByteStringChunk(CborToken token)
    {
        CreateByteString(token);
        if (ErrorCode == null)
        {
            IsChunk = true;
        }
    }

    internal void CreateTextStringChunk(CborToken token)
    {
        CreateTextString(token);
        if (ErrorCode == null)
        {
            IsChunk = true;
        }
    }

    internal void CreateTag(CborToken token)
    {
        MajorType = 6;
        Argument = token.TagNumber;
        Required = CborBinaryPrimitives.GetHeadSize(token.TagNumber);
        IsStart = true;
    }

    internal void CreateUnsignedInteger(BigInteger value)
    {
        if (value < BigInteger.Zero || value > new BigInteger(ulong.MaxValue))
        {
            Fail(
                "CBOR_UNSIGNED_INTEGER_OUT_OF_RANGE",
                "An unsigned CBOR integer must be between zero and 2^64 minus one."
            );
            return;
        }
        MajorType = 0;
        Argument = (ulong)value;
        Required = CborBinaryPrimitives.GetHeadSize(Argument);
    }

    internal void CreateNegativeInteger(BigInteger value)
    {
        BigInteger minimum = new BigInteger(-1) - new BigInteger(ulong.MaxValue);
        if (value < minimum || value >= BigInteger.Zero)
        {
            Fail(
                "CBOR_NEGATIVE_INTEGER_OUT_OF_RANGE",
                "A negative CBOR integer must be between -2^64 and negative one."
            );
            return;
        }
        MajorType = 1;
        Argument = (ulong)(new BigInteger(-1) - value);
        Required = CborBinaryPrimitives.GetHeadSize(Argument);
    }

    internal void CreateByteString(CborToken token)
    {
        if (token.LengthKind != CborLengthKind.Definite)
        {
            Fail(
                "CBOR_STRING_LENGTH_KIND_INVALID",
                "A complete byte string token must have a definite length."
            );
            return;
        }
        if (token.ByteStringValue == null)
        {
            Fail(
                "CBOR_BYTE_STRING_VALUE_MISSING",
                "A byte string token requires a byte payload."
            );
            return;
        }
        if (token.Length != (ulong)token.ByteStringValue.Length)
        {
            Fail(
                "CBOR_BYTE_STRING_LENGTH_MISMATCH",
                "The byte string token length does not match its payload."
            );
            return;
        }
        int headSize = CborBinaryPrimitives.GetHeadSize(token.Length);
        if (token.ByteStringValue.Length > int.MaxValue - headSize)
        {
            Fail(
                "CBOR_OUTPUT_SIZE_OUT_OF_RANGE",
                "The encoded byte string size exceeds the supported result range."
            );
            return;
        }
        MajorType = 2;
        Argument = token.Length;
        Required = headSize + token.ByteStringValue.Length;
        BytePayload = token.ByteStringValue;
    }

    internal void CreateTextString(CborToken token)
    {
        if (token.LengthKind != CborLengthKind.Definite)
        {
            Fail(
                "CBOR_STRING_LENGTH_KIND_INVALID",
                "A complete text string token must have a definite length."
            );
            return;
        }
        if (token.TextStringValue == null)
        {
            Fail(
                "CBOR_TEXT_STRING_VALUE_MISSING",
                "A text string token requires a text payload."
            );
            return;
        }
        if (!IsWellFormedUtf16(token.TextStringValue))
        {
            Fail(
                "CBOR_TEXT_STRING_INVALID_UTF16",
                "The text string contains an unpaired UTF-16 surrogate."
            );
            return;
        }
        int byteLength = Encoding.UTF8.GetByteCount(token.TextStringValue);
        if (token.Length != (ulong)byteLength)
        {
            Fail(
                "CBOR_TEXT_STRING_LENGTH_MISMATCH",
                "The text string token length does not match its UTF-8 payload."
            );
            return;
        }
        int headSize = CborBinaryPrimitives.GetHeadSize(token.Length);
        if (byteLength > int.MaxValue - headSize)
        {
            Fail(
                "CBOR_OUTPUT_SIZE_OUT_OF_RANGE",
                "The encoded text string size exceeds the supported result range."
            );
            return;
        }
        MajorType = 3;
        Argument = token.Length;
        Required = headSize + byteLength;
        TextPayload = token.TextStringValue;
    }

    internal void CreateSimpleValue(byte value)
    {
        if (value >= 24 && value < 32)
        {
            Fail(
                "CBOR_SIMPLE_VALUE_RESERVED",
                "Simple values 24 through 31 cannot be encoded as data items."
            );
            return;
        }
        MajorType = 7;
        Argument = value;
        Required = CborBinaryPrimitives.GetHeadSize(value);
    }

    internal void CreateFloat(CborToken token)
    {
        if (token.FloatValue == null)
        {
            Fail(
                "CBOR_FLOAT_VALUE_MISSING",
                "A floating-point token requires a floating-point value."
            );
            return;
        }
        if (token.FloatWidth != CborFloatWidth.Half
            && token.FloatWidth != CborFloatWidth.Single
            && token.FloatWidth != CborFloatWidth.Double)
        {
            Fail(
                "CBOR_FLOAT_WIDTH_INVALID",
                "A floating-point token requires a 16-, 32-, or 64-bit width."
            );
            return;
        }
        if (token.FloatValue.Width != CborFloatWidth.None
            && token.FloatValue.Width != token.FloatWidth)
        {
            Fail(
                "CBOR_FLOAT_WIDTH_MISMATCH",
                "The token and floating-point value widths do not match."
            );
            return;
        }

        FloatWidth = token.FloatWidth;
        IsFloat = true;
        if (token.FloatValue.Width == CborFloatWidth.None)
        {
            CreateConvertedFloatBits(token.FloatValue.Value);
            return;
        }

        FloatBits = token.FloatValue.RawBits;
        if (FloatWidth == CborFloatWidth.Half)
        {
            if (FloatBits > ushort.MaxValue)
            {
                Fail(
                    "CBOR_FLOAT_BITS_OUT_OF_RANGE",
                    "The floating-point bits do not fit the selected half-precision width."
                );
                return;
            }
            Required = 3;
            return;
        }
        if (FloatWidth == CborFloatWidth.Single)
        {
            if (FloatBits > uint.MaxValue)
            {
                Fail(
                    "CBOR_FLOAT_BITS_OUT_OF_RANGE",
                    "The floating-point bits do not fit the selected single-precision width."
                );
                return;
            }
            Required = 5;
            return;
        }
        Required = 9;
    }

    internal void CreateConvertedFloatBits(double value)
    {
        if (FloatWidth == CborFloatWidth.Half)
        {
            FloatBits = CborHalfPrecision.ConvertDoubleToHalfBits(value);
            Required = 3;
            return;
        }
        if (FloatWidth == CborFloatWidth.Single)
        {
            FloatBits = CborHalfPrecision.ConvertDoubleToSingleBits(value);
            Required = 5;
            return;
        }
        FloatBits = CborHalfPrecision.ConvertDoubleToDoubleBits(value);
        Required = 9;
    }

    internal static bool IsWellFormedUtf16(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                index = index + 2;
                continue;
            }
            if (char.IsLowSurrogate(current))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal void Fail(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
