using System;

namespace cbor;

internal static class CborHalfPrecision
{
    internal static CborFloatValue ReadHalfPrecision(ReadOnlySpan<byte> source)
    {
        ushort bits = CborBinaryPrimitives.ReadUInt16BigEndian(source);
        return CreateHalfFloat(bits);
    }

    internal static CborFloatValue ReadSinglePrecision(ReadOnlySpan<byte> source)
    {
        uint bits = CborBinaryPrimitives.ReadUInt32BigEndian(source);
        return CreateSingleFloat(bits);
    }

    internal static CborFloatValue ReadDoublePrecision(ReadOnlySpan<byte> source)
    {
        ulong bits = CborBinaryPrimitives.ReadUInt64BigEndian(source);
        return CreateDoubleFloat(bits);
    }

    internal static CborFloatValue CreateHalfFloat(ushort bits)
    {
        CborFloatValue result = new CborFloatValue();
        result.Value = ConvertHalfBitsToDouble(bits);
        result.Width = CborFloatWidth.Half;
        result.RawBits = bits;
        result.IsNaN = IsHalfNaN(bits);
        result.IsSignalingNaN = IsHalfSignalingNaN(bits);
        return result;
    }

    internal static CborFloatValue CreateSingleFloat(uint bits)
    {
        CborFloatValue result = new CborFloatValue();
        result.Value = ConvertSingleBitsToDouble(bits);
        result.Width = CborFloatWidth.Single;
        result.RawBits = bits;
        result.IsNaN = IsSingleNaN(bits);
        result.IsSignalingNaN = IsSingleSignalingNaN(bits);
        return result;
    }

    internal static CborFloatValue CreateDoubleFloat(ulong bits)
    {
        CborFloatValue result = new CborFloatValue();
        result.Value = ConvertDoubleBitsToDouble(bits);
        result.Width = CborFloatWidth.Double;
        result.RawBits = bits;
        result.IsNaN = IsDoubleNaN(bits);
        result.IsSignalingNaN = IsDoubleSignalingNaN(bits);
        return result;
    }

    internal static double ConvertHalfBitsToDouble(ushort bits)
    {
        Half value = BitConverter.UInt16BitsToHalf(bits);
        return (double)value;
    }

    internal static double ConvertSingleBitsToDouble(uint bits)
    {
        float value = BitConverter.UInt32BitsToSingle(bits);
        return value;
    }

    internal static double ConvertDoubleBitsToDouble(ulong bits)
    {
        return BitConverter.UInt64BitsToDouble(bits);
    }

    internal static ushort ConvertDoubleToHalfBits(double value)
    {
        Half halfValue = (Half)value;
        return BitConverter.HalfToUInt16Bits(halfValue);
    }

    internal static uint ConvertDoubleToSingleBits(double value)
    {
        float singleValue = (float)value;
        return BitConverter.SingleToUInt32Bits(singleValue);
    }

    internal static ulong ConvertDoubleToDoubleBits(double value)
    {
        return BitConverter.DoubleToUInt64Bits(value);
    }

    internal static bool IsExactlyRepresentableAsHalf(double value)
    {
        if (double.IsNaN(value))
        {
            return false;
        }
        ushort bits = ConvertDoubleToHalfBits(value);
        double restored = ConvertHalfBitsToDouble(bits);
        return ConvertDoubleToDoubleBits(value) == ConvertDoubleToDoubleBits(restored);
    }

    internal static bool IsExactlyRepresentableAsSingle(double value)
    {
        if (double.IsNaN(value))
        {
            return false;
        }
        uint bits = ConvertDoubleToSingleBits(value);
        double restored = ConvertSingleBitsToDouble(bits);
        return ConvertDoubleToDoubleBits(value) == ConvertDoubleToDoubleBits(restored);
    }

    internal static bool IsHalfNaN(ushort bits)
    {
        uint exponent = (uint)bits & 0x7c00U;
        uint fraction = (uint)bits & 0x03ffU;
        return exponent == 0x7c00U && fraction != 0;
    }

    internal static bool IsHalfSignalingNaN(ushort bits)
    {
        return IsHalfNaN(bits) && ((uint)bits & 0x0200U) == 0;
    }

    internal static bool IsSingleNaN(uint bits)
    {
        uint exponent = bits & 0x7f800000U;
        uint fraction = bits & 0x007fffffU;
        return exponent == 0x7f800000U && fraction != 0;
    }

    internal static bool IsSingleSignalingNaN(uint bits)
    {
        return IsSingleNaN(bits) && (bits & 0x00400000U) == 0;
    }

    internal static bool IsDoubleNaN(ulong bits)
    {
        ulong exponent = bits & 0x7ff0000000000000UL;
        ulong fraction = bits & 0x000fffffffffffffUL;
        return exponent == 0x7ff0000000000000UL && fraction != 0;
    }

    internal static bool IsDoubleSignalingNaN(ulong bits)
    {
        return IsDoubleNaN(bits) && (bits & 0x0008000000000000UL) == 0;
    }

    internal static int WriteFloatRepresentation(Span<byte> destination, CborFloatValue value)
    {
        switch (value.Width)
        {
            case CborFloatWidth.Half:
                if (value.RawBits > ushort.MaxValue)
                {
                    throw new CborEncodeException(
                        "CBOR_FLOAT_BITS_OUT_OF_RANGE",
                        "The retained bits do not fit the selected half-precision width.",
                        -1,
                        "$"
                    );
                }
                CborBinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)value.RawBits);
                return 2;
            case CborFloatWidth.Single:
                if (value.RawBits > uint.MaxValue)
                {
                    throw new CborEncodeException(
                        "CBOR_FLOAT_BITS_OUT_OF_RANGE",
                        "The retained bits do not fit the selected single-precision width.",
                        -1,
                        "$"
                    );
                }
                CborBinaryPrimitives.WriteUInt32BigEndian(destination, (uint)value.RawBits);
                return 4;
            case CborFloatWidth.Double:
                CborBinaryPrimitives.WriteUInt64BigEndian(destination, value.RawBits);
                return 8;
            default:
                throw new CborEncodeException(
                    "CBOR_FLOAT_WIDTH_INVALID",
                    "A retained floating-point representation requires a 16-, 32-, or 64-bit width.",
                    -1,
                    "$"
                );
        }
    }
}
