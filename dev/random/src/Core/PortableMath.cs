using System;

namespace random;

internal class PortableMath
{
    internal const ulong DoubleSignMask = 0x8000000000000000UL;
    internal const ulong DoubleExponentMask = 0x7FF0000000000000UL;
    internal const ulong DoubleFractionMask = 0x000FFFFFFFFFFFFFUL;
    internal const ulong DoubleCanonicalNaN = 0x7FF8000000000000UL;
    internal const uint SingleSignMask = 0x80000000U;
    internal const uint SingleExponentMask = 0x7F800000U;
    internal const uint SingleFractionMask = 0x007FFFFFU;
    internal const uint SingleCanonicalNaN = 0x7FC00000U;
    internal const int LogErrorUlps = 4;
    internal const int ExpErrorUlps = 8;
    internal const int PowerErrorUlps = 32;
    internal const double TrigAbsoluteError = 2.0e-12;
    internal const double InverseTrigAbsoluteError = 2.0e-14;
    const double Ln2High = 6.93147180369123816490e-01;
    const double Ln2Low = 1.90821492927058770002e-10;
    const double InverseLn2 = 1.44269504088896340736e+00;
    const ulong SqrtTwoBits = 0x3FF6A09E667F3BCDUL;
    const double Pi = 3.14159265358979323846e+00;
    const double PiOver2 = 1.57079632679489661923e+00;
    const double PiOver4 = 7.85398163397448309616e-01;
    const double TwoPi = 6.28318530717958647693e+00;
    const double InversePiOver2 = 6.36619772367581343076e-01;
    const double PiOver2High = 1.57079632673412561417e+00;
    const double PiOver2Low = 6.07710050650619224932e-11;
    const double CodyWaiteLimit = 1048576.0;

    internal static ulong DoubleBits(double value)
    {
        return BitConverter.DoubleToUInt64Bits(value);
    }

    internal static double DoubleFromBits(ulong bits)
    {
        return BitConverter.UInt64BitsToDouble(bits);
    }

    internal static bool DoubleSignBit(double value)
    {
        return (DoubleBits(value) & DoubleSignMask) != 0UL;
    }

    internal static int DoubleExponentBits(double value)
    {
        return (int)((DoubleBits(value) & DoubleExponentMask) >> 52);
    }

    internal static ulong DoubleFractionBits(double value)
    {
        return DoubleBits(value) & DoubleFractionMask;
    }

    internal static double ComposeDouble(bool negative, int exponentBits, ulong fractionBits)
    {
        Guard.Argument(exponentBits >= 0 && exponentBits <= 0x7FF, "exponentBits", "The raw exponent must be between zero and 2047 inclusive.");
        Guard.Argument(fractionBits <= DoubleFractionMask, "fractionBits", "The fraction must fit in 52 bits.");
        ulong sign = negative ? DoubleSignMask : 0UL;
        ulong bits = sign | ((ulong)exponentBits << 52) | fractionBits;
        return DoubleFromBits(bits);
    }

    internal static uint SingleBits(float value)
    {
        return BitConverter.SingleToUInt32Bits(value);
    }

    internal static float SingleFromBits(uint bits)
    {
        return BitConverter.UInt32BitsToSingle(bits);
    }

    internal static bool SingleSignBit(float value)
    {
        return (SingleBits(value) & SingleSignMask) != 0U;
    }

    internal static int SingleExponentBits(float value)
    {
        return (int)((SingleBits(value) & SingleExponentMask) >> 23);
    }

    internal static uint SingleFractionBits(float value)
    {
        return SingleBits(value) & SingleFractionMask;
    }

    internal static float ComposeSingle(bool negative, int exponentBits, uint fractionBits)
    {
        Guard.Argument(exponentBits >= 0 && exponentBits <= 0xFF, "exponentBits", "The raw exponent must be between zero and 255 inclusive.");
        Guard.Argument(fractionBits <= SingleFractionMask, "fractionBits", "The fraction must fit in 23 bits.");
        uint sign = negative ? SingleSignMask : 0U;
        uint bits = sign | ((uint)exponentBits << 23) | fractionBits;
        return SingleFromBits(bits);
    }

    internal static double ScaleByPowerOfTwo(double value, int exponent)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        ulong sign = bits & DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        ulong fraction = magnitude & DoubleFractionMask;

        if (exponentBits == 0x7FF)
        {
            if (fraction != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return value;
        }

        if (magnitude == 0UL)
        {
            return value;
        }

        ulong significand;
        int power;
        if (exponentBits == 0)
        {
            significand = fraction;
            power = -1074;
        }
        else
        {
            significand = (1UL << 52) | fraction;
            power = exponentBits - 1023 - 52;
        }

        long scaledPower = (long)power + exponent;
        return ComposeScaledDouble(sign, significand, scaledPower);
    }

    internal static float ScaleByPowerOfTwo(float value, int exponent)
    {
        uint bits = SingleBits(value);
        uint magnitude = bits & ~SingleSignMask;
        if ((magnitude & SingleExponentMask) == SingleExponentMask && (magnitude & SingleFractionMask) != 0U)
        {
            return SingleFromBits(SingleCanonicalNaN);
        }

        double scaled = ScaleByPowerOfTwo((double)value, exponent);
        return (float)scaled;
    }

    internal static double RoundToNearestEven(double value)
    {
        ulong bits = DoubleBits(value);
        ulong sign = bits & DoubleSignMask;
        ulong magnitude = bits & ~DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        ulong fraction = magnitude & DoubleFractionMask;

        if (exponentBits == 0x7FF)
        {
            if (fraction != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return value;
        }

        if (exponentBits == 0)
        {
            return DoubleFromBits(sign);
        }

        int unbiasedExponent = exponentBits - 1023;
        if (unbiasedExponent < -1)
        {
            return DoubleFromBits(sign);
        }

        if (unbiasedExponent == -1)
        {
            if (fraction == 0UL)
            {
                return DoubleFromBits(sign);
            }

            return DoubleFromBits(sign | 0x3FF0000000000000UL);
        }

        if (unbiasedExponent >= 52)
        {
            return value;
        }

        int fractionalBitCount = 52 - unbiasedExponent;
        ulong mask = (1UL << fractionalBitCount) - 1UL;
        ulong remainder = magnitude & mask;
        if (remainder == 0UL)
        {
            return value;
        }

        ulong cleared = magnitude & ~mask;
        ulong halfway = 1UL << (fractionalBitCount - 1);
        ulong significand = (1UL << 52) | fraction;
        ulong integerLeastBit = (significand >> fractionalBitCount) & 1UL;
        if (remainder > halfway || remainder == halfway && integerLeastBit != 0UL)
        {
            cleared += 1UL << fractionalBitCount;
        }

        return DoubleFromBits(sign | cleared);
    }

    internal static float RoundToNearestEven(float value)
    {
        uint bits = SingleBits(value);
        uint sign = bits & SingleSignMask;
        uint magnitude = bits & ~SingleSignMask;
        int exponentBits = (int)(magnitude >> 23);
        uint fraction = magnitude & SingleFractionMask;

        if (exponentBits == 0xFF)
        {
            if (fraction != 0U)
            {
                return SingleFromBits(SingleCanonicalNaN);
            }

            return value;
        }

        if (exponentBits == 0)
        {
            return SingleFromBits(sign);
        }

        int unbiasedExponent = exponentBits - 127;
        if (unbiasedExponent < -1)
        {
            return SingleFromBits(sign);
        }

        if (unbiasedExponent == -1)
        {
            if (fraction == 0U)
            {
                return SingleFromBits(sign);
            }

            return SingleFromBits(sign | 0x3F800000U);
        }

        if (unbiasedExponent >= 23)
        {
            return value;
        }

        int fractionalBitCount = 23 - unbiasedExponent;
        uint mask = (1U << fractionalBitCount) - 1U;
        uint remainder = magnitude & mask;
        if (remainder == 0U)
        {
            return value;
        }

        uint cleared = magnitude & ~mask;
        uint halfway = 1U << (fractionalBitCount - 1);
        uint significand = (1U << 23) | fraction;
        uint integerLeastBit = (significand >> fractionalBitCount) & 1U;
        if (remainder > halfway || remainder == halfway && integerLeastBit != 0U)
        {
            cleared += 1U << fractionalBitCount;
        }

        return SingleFromBits(sign | cleared);
    }

    internal static double Sqrt(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        ulong fraction = magnitude & DoubleFractionMask;

        if (exponentBits == 0x7FF)
        {
            if (fraction != 0UL || (bits & DoubleSignMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return value;
        }

        if (magnitude == 0UL)
        {
            return value;
        }

        if ((bits & DoubleSignMask) != 0UL)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        ulong significand;
        int power;
        if (exponentBits == 0)
        {
            int shift = BitMath.LeadingZeroCount(fraction) - 11;
            significand = fraction << shift;
            power = -1074 - shift;
        }
        else
        {
            significand = (1UL << 52) | fraction;
            power = exponentBits - 1023 - 52;
        }

        if (power % 2 != 0)
        {
            significand <<= 1;
            power--;
        }

        UInt128 radicand = (UInt128)significand << 52;
        UInt128 root = IntegerSquareRoot(radicand);
        UInt128 remainder = radicand - root * root;
        if (remainder > root)
        {
            root++;
        }

        int resultExponent = power / 2 + 26;
        UInt128 overflowSignificand = (UInt128)1 << 53;
        if (root == overflowSignificand)
        {
            root >>= 1;
            resultExponent++;
        }

        ulong resultSignificand = (ulong)root;
        ulong resultFraction = resultSignificand & DoubleFractionMask;
        ulong resultExponentBits = (ulong)(resultExponent + 1023);
        return DoubleFromBits((resultExponentBits << 52) | resultFraction);
    }

    internal static float Sqrt(float value)
    {
        uint bits = SingleBits(value);
        uint magnitude = bits & ~SingleSignMask;
        if ((magnitude & SingleExponentMask) == SingleExponentMask && (magnitude & SingleFractionMask) != 0U)
        {
            return SingleFromBits(SingleCanonicalNaN);
        }

        if ((bits & SingleSignMask) != 0U && magnitude != 0U)
        {
            return SingleFromBits(SingleCanonicalNaN);
        }

        double result = Sqrt((double)value);
        return (float)result;
    }

    internal static double NextUp(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        if ((magnitude & DoubleExponentMask) == DoubleExponentMask && (magnitude & DoubleFractionMask) != 0UL)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (bits == DoubleExponentMask)
        {
            return value;
        }

        if (magnitude == 0UL)
        {
            return DoubleFromBits(1UL);
        }

        if ((bits & DoubleSignMask) == 0UL)
        {
            return DoubleFromBits(bits + 1UL);
        }

        return DoubleFromBits(bits - 1UL);
    }

    internal static double NextDown(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        if ((magnitude & DoubleExponentMask) == DoubleExponentMask && (magnitude & DoubleFractionMask) != 0UL)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (bits == (DoubleSignMask | DoubleExponentMask))
        {
            return value;
        }

        if (magnitude == 0UL)
        {
            return DoubleFromBits(DoubleSignMask | 1UL);
        }

        if ((bits & DoubleSignMask) == 0UL)
        {
            return DoubleFromBits(bits - 1UL);
        }

        return DoubleFromBits(bits + 1UL);
    }

    internal static float NextUp(float value)
    {
        uint bits = SingleBits(value);
        uint magnitude = bits & ~SingleSignMask;
        if ((magnitude & SingleExponentMask) == SingleExponentMask && (magnitude & SingleFractionMask) != 0U)
        {
            return SingleFromBits(SingleCanonicalNaN);
        }

        if (bits == SingleExponentMask)
        {
            return value;
        }

        if (magnitude == 0U)
        {
            return SingleFromBits(1U);
        }

        if ((bits & SingleSignMask) == 0U)
        {
            return SingleFromBits(bits + 1U);
        }

        return SingleFromBits(bits - 1U);
    }

    internal static float NextDown(float value)
    {
        uint bits = SingleBits(value);
        uint magnitude = bits & ~SingleSignMask;
        if ((magnitude & SingleExponentMask) == SingleExponentMask && (magnitude & SingleFractionMask) != 0U)
        {
            return SingleFromBits(SingleCanonicalNaN);
        }

        if (bits == (SingleSignMask | SingleExponentMask))
        {
            return value;
        }

        if (magnitude == 0U)
        {
            return SingleFromBits(SingleSignMask | 1U);
        }

        if ((bits & SingleSignMask) == 0U)
        {
            return SingleFromBits(bits - 1U);
        }

        return SingleFromBits(bits + 1U);
    }

    internal static double Log(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        ulong fraction = magnitude & DoubleFractionMask;

        if (exponentBits == 0x7FF)
        {
            if (fraction != 0UL || (bits & DoubleSignMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return value;
        }

        if (magnitude == 0UL)
        {
            return double.NegativeInfinity;
        }

        if ((bits & DoubleSignMask) != 0UL)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        int exponent;
        ulong normalizedFraction;
        if (exponentBits == 0)
        {
            int bitLength = 64 - BitMath.LeadingZeroCount(fraction);
            int shift = 53 - bitLength;
            ulong normalized = fraction << shift;
            normalizedFraction = normalized & DoubleFractionMask;
            exponent = bitLength - 1 - 1074;
        }
        else
        {
            normalizedFraction = fraction;
            exponent = exponentBits - 1023;
        }

        double mantissa = DoubleFromBits(0x3FF0000000000000UL | normalizedFraction);
        if (DoubleBits(mantissa) > SqrtTwoBits)
        {
            mantissa = mantissa * 0.5;
            exponent++;
        }

        double numerator = mantissa - 1.0;
        double denominator = mantissa + 1.0;
        double reduced = numerator / denominator;
        double series = LogAtanhSeries(reduced);
        double exponentValue = (double)exponent;
        double high = exponentValue * Ln2High;
        double low = exponentValue * Ln2Low;
        return high + (series + low);
    }

    internal static double Log1p(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        if ((magnitude & DoubleExponentMask) == DoubleExponentMask)
        {
            if ((magnitude & DoubleFractionMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            if ((bits & DoubleSignMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return value;
        }

        if (value == -1.0)
        {
            return double.NegativeInfinity;
        }

        if (value < -1.0)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (magnitude == 0UL)
        {
            return value;
        }

        if (value >= -0.5 && value <= 0.5)
        {
            double reduced = value / (2.0 + value);
            return LogAtanhSeries(reduced);
        }

        return Log(1.0 + value);
    }

    internal static double Exp(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        if ((magnitude & DoubleExponentMask) == DoubleExponentMask)
        {
            if ((magnitude & DoubleFractionMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            if ((bits & DoubleSignMask) != 0UL)
            {
                return 0.0;
            }

            return value;
        }

        if (magnitude == 0UL)
        {
            return 1.0;
        }

        if (value > 710.0)
        {
            return double.PositiveInfinity;
        }

        if (value < -746.0)
        {
            return 0.0;
        }

        double scaled = value * InverseLn2;
        double rounded = RoundToNearestEven(scaled);
        int exponent = (int)rounded;
        double exponentValue = (double)exponent;
        double reduced = value - exponentValue * Ln2High;
        reduced = reduced - exponentValue * Ln2Low;

        double term = 1.0;
        double sum = 1.0;
        double compensation = 0.0;
        int index = 1;
        while (index <= 24)
        {
            term = term * reduced;
            term = term / (double)index;
            double adjusted = term - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            index++;
        }

        return ScaleByPowerOfTwo(sum, exponent);
    }

    internal static double Expm1(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        if ((magnitude & DoubleExponentMask) == DoubleExponentMask)
        {
            if ((magnitude & DoubleFractionMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            if ((bits & DoubleSignMask) != 0UL)
            {
                return -1.0;
            }

            return value;
        }

        if (magnitude == 0UL)
        {
            return value;
        }

        if (value >= -0.5 && value <= 0.5)
        {
            double term = value;
            double sum = value;
            double compensation = 0.0;
            int index = 2;
            while (index <= 24)
            {
                term = term * value;
                term = term / (double)index;
                double adjusted = term - compensation;
                double next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                sum = next;
                index++;
            }

            return sum;
        }

        return Exp(value) - 1.0;
    }

    internal static double Power(double value, double exponent)
    {
        if (exponent == 0.0)
        {
            return 1.0;
        }

        if (value == 1.0)
        {
            return 1.0;
        }

        if (double.IsNaN(value) || double.IsNaN(exponent))
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        double absoluteValue = DoubleFromBits(DoubleBits(value) & ~DoubleSignMask);
        if (double.IsInfinity(exponent))
        {
            if (absoluteValue == 1.0)
            {
                return 1.0;
            }

            if (absoluteValue > 1.0)
            {
                return exponent > 0.0 ? double.PositiveInfinity : 0.0;
            }

            return exponent > 0.0 ? 0.0 : double.PositiveInfinity;
        }

        bool oddInteger = IsOddInteger(exponent);
        if (double.IsInfinity(value))
        {
            if (exponent > 0.0)
            {
                if (value < 0.0 && oddInteger)
                {
                    return double.NegativeInfinity;
                }

                return double.PositiveInfinity;
            }

            if (value < 0.0 && oddInteger)
            {
                return DoubleFromBits(DoubleSignMask);
            }

            return 0.0;
        }

        if (absoluteValue == 0.0)
        {
            bool negativeResult = (DoubleBits(value) & DoubleSignMask) != 0UL && oddInteger;
            if (exponent > 0.0)
            {
                return negativeResult ? DoubleFromBits(DoubleSignMask) : 0.0;
            }

            return negativeResult ? double.NegativeInfinity : double.PositiveInfinity;
        }

        bool negativeBase = value < 0.0;
        if (negativeBase && !IsInteger(exponent))
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (IsInteger(exponent) && exponent >= -9007199254740992.0 && exponent <= 9007199254740992.0)
        {
            return IntegerPower(value, (long)exponent);
        }

        double logarithm = Log(absoluteValue);
        double result = Exp(exponent * logarithm);
        if (negativeBase && oddInteger)
        {
            return -result;
        }

        return result;
    }

    internal static double LogAtanhSeries(double reduced)
    {
        double square = reduced * reduced;
        double term = reduced;
        double sum = reduced;
        double compensation = 0.0;
        int denominator = 3;
        while (denominator <= 65)
        {
            term = term * square;
            double addition = term / (double)denominator;
            double adjusted = addition - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            denominator += 2;
        }

        return 2.0 * sum;
    }

    internal static bool IsInteger(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        if (exponentBits == 0x7FF)
        {
            return false;
        }

        if (magnitude == 0UL)
        {
            return true;
        }

        int exponent = exponentBits - 1023;
        if (exponent < 0)
        {
            return false;
        }

        if (exponent >= 52)
        {
            return true;
        }

        int fractionalBits = 52 - exponent;
        ulong mask = (1UL << fractionalBits) - 1UL;
        return (magnitude & mask) == 0UL;
    }

    internal static bool IsOddInteger(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitude = bits & ~DoubleSignMask;
        int exponentBits = (int)(magnitude >> 52);
        if (exponentBits == 0 || exponentBits == 0x7FF)
        {
            return false;
        }

        int exponent = exponentBits - 1023;
        if (exponent < 0 || exponent > 52)
        {
            return false;
        }

        ulong significand = (1UL << 52) | (magnitude & DoubleFractionMask);
        int fractionalBits = 52 - exponent;
        if (fractionalBits > 0)
        {
            ulong mask = (1UL << fractionalBits) - 1UL;
            if ((significand & mask) != 0UL)
            {
                return false;
            }
        }

        return ((significand >> fractionalBits) & 1UL) != 0UL;
    }

    internal static double IntegerPower(double value, long exponent)
    {
        bool reciprocal = exponent < 0;
        ulong remaining = reciprocal ? (ulong)(-exponent) : (ulong)exponent;
        double result = 1.0;
        double factor = value;
        while (remaining != 0UL)
        {
            if ((remaining & 1UL) != 0UL)
            {
                result = result * factor;
            }

            remaining >>= 1;
            if (remaining != 0UL)
            {
                factor = factor * factor;
            }
        }

        if (reciprocal)
        {
            return 1.0 / result;
        }

        return result;
    }

    internal static double Sin(double value)
    {
        return Trig(value, false);
    }

    internal static double Cos(double value)
    {
        return Trig(value, true);
    }

    internal static double Tan(double value)
    {
        double sine = Sin(value);
        double cosine = Cos(value);
        return sine / cosine;
    }

    internal static double Atan(double value)
    {
        ulong bits = DoubleBits(value);
        ulong magnitudeBits = bits & ~DoubleSignMask;
        if ((magnitudeBits & DoubleExponentMask) == DoubleExponentMask)
        {
            if ((magnitudeBits & DoubleFractionMask) != 0UL)
            {
                return DoubleFromBits(DoubleCanonicalNaN);
            }

            return (bits & DoubleSignMask) != 0UL ? -PiOver2 : PiOver2;
        }

        if (magnitudeBits == 0UL)
        {
            return value;
        }

        bool negative = (bits & DoubleSignMask) != 0UL;
        double magnitude = DoubleFromBits(magnitudeBits);
        double reduced;
        double offset;
        int mode;
        if (magnitude > 2.41421356237309504880)
        {
            reduced = 1.0 / magnitude;
            offset = PiOver2;
            mode = 2;
        }
        else if (magnitude > 0.41421356237309504880)
        {
            reduced = (magnitude - 1.0) / (magnitude + 1.0);
            offset = PiOver4;
            mode = 1;
        }
        else
        {
            reduced = magnitude;
            offset = 0.0;
            mode = 0;
        }

        double kernel = AtanSeries(reduced);
        double result;
        if (mode == 2)
        {
            result = offset - kernel;
        }
        else
        {
            result = offset + kernel;
        }

        return negative ? -result : result;
    }

    internal static double Atan2(double y, double x)
    {
        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        ulong xBits = DoubleBits(x);
        ulong yBits = DoubleBits(y);
        ulong xMagnitudeBits = xBits & ~DoubleSignMask;
        ulong yMagnitudeBits = yBits & ~DoubleSignMask;
        bool xNegative = (xBits & DoubleSignMask) != 0UL;
        bool yNegative = (yBits & DoubleSignMask) != 0UL;

        if (yMagnitudeBits == 0UL)
        {
            if (xNegative)
            {
                return yNegative ? -Pi : Pi;
            }

            return y;
        }

        if (xMagnitudeBits == 0UL)
        {
            return yNegative ? -PiOver2 : PiOver2;
        }

        bool xInfinite = xMagnitudeBits == DoubleExponentMask;
        bool yInfinite = yMagnitudeBits == DoubleExponentMask;
        if (xInfinite && yInfinite)
        {
            double diagonal = xNegative ? 3.0 * PiOver4 : PiOver4;
            return yNegative ? -diagonal : diagonal;
        }

        if (yInfinite)
        {
            return yNegative ? -PiOver2 : PiOver2;
        }

        if (xInfinite)
        {
            if (xNegative)
            {
                return yNegative ? -Pi : Pi;
            }

            return yNegative ? DoubleFromBits(DoubleSignMask) : 0.0;
        }

        double xMagnitude = DoubleFromBits(xMagnitudeBits);
        double yMagnitude = DoubleFromBits(yMagnitudeBits);
        double angle;
        if (yMagnitude > xMagnitude)
        {
            angle = PiOver2 - Atan(xMagnitude / yMagnitude);
        }
        else
        {
            angle = Atan(yMagnitude / xMagnitude);
        }

        if (xNegative)
        {
            angle = Pi - angle;
        }

        return yNegative ? -angle : angle;
    }

    internal static double Asin(double value)
    {
        if (double.IsNaN(value))
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (value < -1.0 || value > 1.0)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (value == 0.0)
        {
            return value;
        }

        double complement = (1.0 - value) * (1.0 + value);
        return Atan2(value, Sqrt(complement));
    }

    internal static double Acos(double value)
    {
        if (double.IsNaN(value))
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (value < -1.0 || value > 1.0)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        double complement = (1.0 - value) * (1.0 + value);
        return Atan2(Sqrt(complement), value);
    }

    internal static double Trig(double value, bool cosine)
    {
        ulong bits = DoubleBits(value);
        ulong magnitudeBits = bits & ~DoubleSignMask;
        if ((magnitudeBits & DoubleExponentMask) == DoubleExponentMask)
        {
            return DoubleFromBits(DoubleCanonicalNaN);
        }

        if (magnitudeBits == 0UL)
        {
            return cosine ? 1.0 : value;
        }

        double working = value;
        double magnitude = DoubleFromBits(magnitudeBits);
        if (magnitude > CodyWaiteLimit)
        {
            working = value % TwoPi;
        }

        double quotient = working * InversePiOver2;
        int multiple = (int)RoundToNearestEven(quotient);
        double multipleValue = (double)multiple;
        double reduced = working - multipleValue * PiOver2High;
        reduced = reduced - multipleValue * PiOver2Low;
        int quadrant = multiple % 4;
        if (quadrant < 0)
        {
            quadrant += 4;
        }

        if (cosine)
        {
            switch (quadrant)
            {
                case 0:
                    return CosKernel(reduced);
                case 1:
                    return -SinKernel(reduced);
                case 2:
                    return -CosKernel(reduced);
                default:
                    return SinKernel(reduced);
            }
        }

        switch (quadrant)
        {
            case 0:
                return SinKernel(reduced);
            case 1:
                return CosKernel(reduced);
            case 2:
                return -SinKernel(reduced);
            default:
                return -CosKernel(reduced);
        }
    }

    internal static double SinKernel(double value)
    {
        double square = value * value;
        double polynomial = -8.2206352466243297169e-18;
        polynomial = 2.8114572543455207632e-15 + square * polynomial;
        polynomial = -7.6471637318198164759e-13 + square * polynomial;
        polynomial = 1.6059043836821614599e-10 + square * polynomial;
        polynomial = -2.5052108385441718775e-08 + square * polynomial;
        polynomial = 2.7557319223985892511e-06 + square * polynomial;
        polynomial = -1.9841269841269841253e-04 + square * polynomial;
        polynomial = 8.3333333333333328707e-03 + square * polynomial;
        polynomial = -1.6666666666666665741e-01 + square * polynomial;
        return value + value * square * polynomial;
    }

    internal static double CosKernel(double value)
    {
        double square = value * value;
        double polynomial = -1.5619206968586226462e-16;
        polynomial = 4.7794773323873852974e-14 + square * polynomial;
        polynomial = -1.1470745597729724714e-11 + square * polynomial;
        polynomial = 2.0876756987868098979e-09 + square * polynomial;
        polynomial = -2.7557319223985892511e-07 + square * polynomial;
        polynomial = 2.4801587301587301584e-05 + square * polynomial;
        polynomial = -1.3888888888888889419e-03 + square * polynomial;
        polynomial = 4.1666666666666664354e-02 + square * polynomial;
        polynomial = -5.0000000000000000000e-01 + square * polynomial;
        return 1.0 + square * polynomial;
    }

    internal static double AtanSeries(double value)
    {
        double square = value * value;
        double term = value;
        double sum = value;
        double compensation = 0.0;
        int denominator = 3;
        bool subtract = true;
        while (denominator <= 57)
        {
            term = term * square;
            double addition = term / (double)denominator;
            if (subtract)
            {
                addition = -addition;
            }

            double adjusted = addition - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            subtract = !subtract;
            denominator += 2;
        }

        return sum;
    }

    internal static UInt128 IntegerSquareRoot(UInt128 value)
    {
        UInt128 root = (UInt128)1 << 53;
        while (true)
        {
            UInt128 next = (root + value / root) >> 1;
            if (next >= root)
            {
                return root;
            }

            root = next;
        }
    }

    internal static double ComposeScaledDouble(ulong sign, ulong significand, long power)
    {
        int bitLength = 64 - BitMath.LeadingZeroCount(significand);
        long topExponent = power + bitLength - 1;
        if (topExponent > 1023)
        {
            return DoubleFromBits(sign | DoubleExponentMask);
        }

        if (topExponent >= -1022)
        {
            int shift = 53 - bitLength;
            ulong normalized = significand << shift;
            ulong fraction = normalized & DoubleFractionMask;
            ulong exponentBits = (ulong)(topExponent + 1023);
            return DoubleFromBits(sign | (exponentBits << 52) | fraction);
        }

        long unitPower = power + 1074;
        ulong subnormal;
        if (unitPower >= 0)
        {
            subnormal = significand << (int)unitPower;
        }
        else
        {
            long rightShift = -unitPower;
            if (rightShift > 64)
            {
                subnormal = 0UL;
            }
            else
            {
                subnormal = RoundShiftRight(significand, (int)rightShift);
            }
        }

        if (subnormal == 1UL << 52)
        {
            return DoubleFromBits(sign | (1UL << 52));
        }

        return DoubleFromBits(sign | subnormal);
    }

    internal static ulong RoundShiftRight(ulong value, int shift)
    {
        if (shift == 0)
        {
            return value;
        }

        if (shift > 64)
        {
            return 0UL;
        }

        if (shift == 64)
        {
            ulong halfway64 = 1UL << 63;
            return value > halfway64 ? 1UL : 0UL;
        }

        ulong quotient = value >> shift;
        ulong mask = (1UL << shift) - 1UL;
        ulong remainder = value & mask;
        ulong halfway = 1UL << (shift - 1);
        if (remainder > halfway || remainder == halfway && (quotient & 1UL) != 0UL)
        {
            quotient++;
        }

        return quotient;
    }
}
