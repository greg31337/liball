using System;
using System.Globalization;
using System.Numerics;

namespace cbor;

internal class CddlNumberResult
{
    internal bool IsValid;
    internal CddlLiteralKind LiteralKind = CddlLiteralKind.None;
    internal BigInteger IntegerValue;
    internal bool HasIntegerValue;
    internal double FloatValue;
    internal bool HasFloatValue;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal CddlSourceSpan ErrorSpan = new CddlSourceSpan();
}

internal class CddlNumberParser
{
    internal CddlToken Token;
    internal string Raw;
    internal int Index;
    internal bool Negative;
    internal BigInteger ParsedExponent;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorRelativeOffset;
    internal int ErrorLength = 1;

    internal CddlNumberParser(CddlToken token)
    {
        Token = token;
        Raw = token.Text;
    }

    internal CddlNumberResult ParseNumber()
    {
        if (Raw.Length == 0)
        {
            SetFailure(
                "CDDL_NUMBER_DIGIT_REQUIRED",
                "A CDDL number requires at least one digit.",
                0,
                0
            );
            return CreateFailureResult();
        }
        if (Raw[Index] == '-')
        {
            Negative = true;
            Index = Index + 1;
        }
        if (Index >= Raw.Length)
        {
            SetFailure(
                "CDDL_NUMBER_DIGIT_REQUIRED",
                "A sign must be followed by a CDDL number.",
                Index,
                0
            );
            return CreateFailureResult();
        }
        if (Index + 1 < Raw.Length && Raw[Index] == '0'
            && (Raw[Index + 1] == 'x' || Raw[Index + 1] == 'X'))
        {
            return ParseHexNumber();
        }
        if (Index + 1 < Raw.Length && Raw[Index] == '0'
            && (Raw[Index + 1] == 'b' || Raw[Index + 1] == 'B'))
        {
            return ParseBinaryInteger();
        }
        return ParseDecimalNumber();
    }

    internal CddlNumberResult ParseHexNumber()
    {
        Index = Index + 2;
        int integerStart = Index;
        BigInteger significand = BigInteger.Zero;
        while (Index < Raw.Length && HexValue(Raw[Index]) >= 0)
        {
            significand = significand * 16 + HexValue(Raw[Index]);
            Index = Index + 1;
        }
        if (Index == integerStart)
        {
            SetFailure(
                "CDDL_NUMBER_DIGIT_REQUIRED",
                "A hexadecimal CDDL number requires at least one hexadecimal digit.",
                Index,
                0
            );
            return CreateFailureResult();
        }
        int fractionalDigits = 0;
        bool hasFraction = false;
        if (Index < Raw.Length && Raw[Index] == '.')
        {
            hasFraction = true;
            Index = Index + 1;
            int fractionStart = Index;
            while (Index < Raw.Length && HexValue(Raw[Index]) >= 0)
            {
                significand = significand * 16 + HexValue(Raw[Index]);
                fractionalDigits = fractionalDigits + 1;
                Index = Index + 1;
            }
            if (Index == fractionStart)
            {
                SetFailure(
                    "CDDL_NUMBER_FRACTION_REQUIRED",
                    "A hexadecimal fraction requires at least one hexadecimal digit.",
                    Index,
                    0
                );
                return CreateFailureResult();
            }
        }
        bool hasExponent = Index < Raw.Length
            && (Raw[Index] == 'p' || Raw[Index] == 'P');
        if (!hasExponent)
        {
            if (hasFraction)
            {
                SetFailure(
                    "CDDL_NUMBER_HEX_EXPONENT_REQUIRED",
                    "A hexadecimal floating-point literal requires a binary exponent.",
                    Index,
                    0
                );
                return CreateFailureResult();
            }
            if (Index != Raw.Length)
            {
                SetInvalidCharacterFailure();
                return CreateFailureResult();
            }
            return CreateIntegerResult(significand);
        }
        Index = Index + 1;
        if (!ReadDecimalExponent())
        {
            return CreateFailureResult();
        }
        if (Index != Raw.Length)
        {
            SetInvalidCharacterFailure();
            return CreateFailureResult();
        }
        BigInteger binaryExponent = ParsedExponent - fractionalDigits * 4;
        return CreateHexFloatResult(significand, binaryExponent);
    }

    internal CddlNumberResult ParseBinaryInteger()
    {
        Index = Index + 2;
        int digitsStart = Index;
        BigInteger magnitude = BigInteger.Zero;
        while (Index < Raw.Length
            && (Raw[Index] == '0' || Raw[Index] == '1'))
        {
            magnitude = magnitude * 2 + (Raw[Index] - '0');
            Index = Index + 1;
        }
        if (Index == digitsStart)
        {
            SetFailure(
                "CDDL_NUMBER_DIGIT_REQUIRED",
                "A binary CDDL integer requires at least one binary digit.",
                Index,
                0
            );
            return CreateFailureResult();
        }
        if (Index != Raw.Length)
        {
            SetInvalidCharacterFailure();
            return CreateFailureResult();
        }
        return CreateIntegerResult(magnitude);
    }

    internal CddlNumberResult ParseDecimalNumber()
    {
        int digitsStart = Index;
        while (Index < Raw.Length && IsDecimalDigit(Raw[Index]))
        {
            Index = Index + 1;
        }
        if (Index == digitsStart)
        {
            SetFailure(
                "CDDL_NUMBER_DIGIT_REQUIRED",
                "A decimal CDDL number requires at least one digit.",
                Index,
                0
            );
            return CreateFailureResult();
        }
        if (Raw[digitsStart] == '0' && Index - digitsStart > 1)
        {
            SetFailure(
                "CDDL_NUMBER_DECIMAL_LEADING_ZERO",
                "A decimal CDDL number cannot contain leading zeroes.",
                digitsStart + 1,
                1
            );
            return CreateFailureResult();
        }
        bool hasFraction = false;
        bool hasExponent = false;
        if (Index < Raw.Length && Raw[Index] == '.')
        {
            hasFraction = true;
            Index = Index + 1;
            int fractionStart = Index;
            while (Index < Raw.Length && IsDecimalDigit(Raw[Index]))
            {
                Index = Index + 1;
            }
            if (Index == fractionStart)
            {
                SetFailure(
                    "CDDL_NUMBER_FRACTION_REQUIRED",
                    "A decimal fraction requires at least one digit.",
                    Index,
                    0
                );
                return CreateFailureResult();
            }
        }
        if (Index < Raw.Length && (Raw[Index] == 'e' || Raw[Index] == 'E'))
        {
            hasExponent = true;
            Index = Index + 1;
            if (!SkipDecimalExponent())
            {
                return CreateFailureResult();
            }
        }
        if (Index != Raw.Length)
        {
            SetInvalidCharacterFailure();
            return CreateFailureResult();
        }
        if (hasFraction || hasExponent)
        {
            return CreateDecimalFloatResult();
        }
        BigInteger magnitude = BigInteger.Zero;
        int valueIndex = digitsStart;
        while (valueIndex < Index)
        {
            magnitude = magnitude * 10 + (Raw[valueIndex] - '0');
            valueIndex = valueIndex + 1;
        }
        return CreateIntegerResult(magnitude);
    }

    internal bool ReadDecimalExponent()
    {
        bool exponentNegative = false;
        if (Index < Raw.Length && (Raw[Index] == '+' || Raw[Index] == '-'))
        {
            exponentNegative = Raw[Index] == '-';
            Index = Index + 1;
        }
        int exponentStart = Index;
        BigInteger exponent = BigInteger.Zero;
        while (Index < Raw.Length && IsDecimalDigit(Raw[Index]))
        {
            exponent = exponent * 10 + (Raw[Index] - '0');
            Index = Index + 1;
        }
        if (Index == exponentStart)
        {
            SetFailure(
                "CDDL_NUMBER_EXPONENT_REQUIRED",
                "A floating-point exponent requires at least one decimal digit.",
                Index,
                0
            );
            return false;
        }
        ParsedExponent = exponentNegative ? -exponent : exponent;
        return true;
    }

    internal bool SkipDecimalExponent()
    {
        if (Index < Raw.Length && (Raw[Index] == '+' || Raw[Index] == '-'))
        {
            Index = Index + 1;
        }
        int exponentStart = Index;
        while (Index < Raw.Length && IsDecimalDigit(Raw[Index]))
        {
            Index = Index + 1;
        }
        if (Index == exponentStart)
        {
            SetFailure(
                "CDDL_NUMBER_EXPONENT_REQUIRED",
                "A floating-point exponent requires at least one decimal digit.",
                Index,
                0
            );
            return false;
        }
        return true;
    }

    internal CddlNumberResult CreateIntegerResult(BigInteger magnitude)
    {
        CddlNumberResult result = new CddlNumberResult();
        result.IsValid = true;
        result.LiteralKind = Negative
            ? CddlLiteralKind.NegativeInteger
            : CddlLiteralKind.UnsignedInteger;
        result.IntegerValue = Negative ? -magnitude : magnitude;
        result.HasIntegerValue = true;
        return result;
    }

    internal CddlNumberResult CreateDecimalFloatResult()
    {
        double value = double.Parse(
            Raw,
            NumberStyles.Float,
            CultureInfo.InvariantCulture
        );
        if (double.IsInfinity(value))
        {
            SetFailure(
                "CDDL_NUMBER_OVERFLOW",
                "The decimal floating-point literal exceeds the binary64 range.",
                0,
                Raw.Length
            );
            return CreateFailureResult();
        }
        CddlNumberResult result = new CddlNumberResult();
        result.IsValid = true;
        result.LiteralKind = CddlLiteralKind.DecimalFloat;
        result.FloatValue = value;
        result.HasFloatValue = true;
        return result;
    }

    internal CddlNumberResult CreateHexFloatResult(
        BigInteger significand,
        BigInteger binaryExponent)
    {
        if (significand == BigInteger.Zero)
        {
            return CreateFloatResult(Negative ? -0.0 : 0.0);
        }
        int bitLength = BitLength(significand);
        BigInteger highestExponent = binaryExponent + bitLength - 1;
        if (highestExponent > 1023)
        {
            SetFloatOverflow();
            return CreateFailureResult();
        }
        double value;
        if (highestExponent >= -1022)
        {
            int shift = bitLength - 53;
            BigInteger rounded;
            if (shift > 0)
            {
                rounded = RoundRight(significand, shift);
            }
            else
            {
                rounded = significand << -shift;
            }
            BigInteger scale = binaryExponent + shift;
            if (BitLength(rounded) > 53)
            {
                rounded = rounded >> 1;
                scale = scale + 1;
                highestExponent = highestExponent + 1;
                if (highestExponent > 1023)
                {
                    SetFloatOverflow();
                    return CreateFailureResult();
                }
            }
            if (scale < int.MinValue || scale > int.MaxValue)
            {
                SetFloatOverflow();
                return CreateFailureResult();
            }
            value = Math.ScaleB((double)rounded, (int)scale);
        }
        else
        {
            BigInteger unitShift = binaryExponent + 1074;
            BigInteger units;
            if (unitShift >= BigInteger.Zero)
            {
                if (unitShift > int.MaxValue)
                {
                    SetFloatOverflow();
                    return CreateFailureResult();
                }
                units = significand << (int)unitShift;
            }
            else
            {
                BigInteger rightShift = -unitShift;
                if (rightShift > bitLength + 1)
                {
                    units = BigInteger.Zero;
                }
                else
                {
                    units = RoundRight(significand, (int)rightShift);
                }
            }
            value = Math.ScaleB((double)units, -1074);
        }
        if (double.IsInfinity(value))
        {
            SetFloatOverflow();
            return CreateFailureResult();
        }
        if (Negative)
        {
            value = -value;
        }
        return CreateFloatResult(value);
    }

    internal CddlNumberResult CreateFloatResult(double value)
    {
        CddlNumberResult result = new CddlNumberResult();
        result.IsValid = true;
        result.LiteralKind = CddlLiteralKind.HexadecimalFloat;
        result.FloatValue = value;
        result.HasFloatValue = true;
        return result;
    }

    internal BigInteger RoundRight(BigInteger value, int shift)
    {
        if (shift <= 0)
        {
            return value;
        }
        BigInteger quotient = value >> shift;
        BigInteger remainder = value - (quotient << shift);
        BigInteger halfway = BigInteger.One << (shift - 1);
        if (remainder > halfway
            || (remainder == halfway && !quotient.IsEven))
        {
            quotient = quotient + 1;
        }
        return quotient;
    }

    internal int BitLength(BigInteger value)
    {
        byte[] bytes = value.ToByteArray(true, true);
        if (bytes.Length == 0)
        {
            return 0;
        }
        int leadingBits = 8;
        int mask = 0x80;
        while ((bytes[0] & mask) == 0)
        {
            leadingBits = leadingBits - 1;
            mask = mask >> 1;
        }
        return (bytes.Length - 1) * 8 + leadingBits;
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal void SetInvalidCharacterFailure()
    {
        SetFailure(
            "CDDL_NUMBER_CHARACTER_INVALID",
            "The numeric literal contains a character outside its grammar.",
            Index,
            Index < Raw.Length ? 1 : 0
        );
    }

    internal void SetFloatOverflow()
    {
        SetFailure(
            "CDDL_NUMBER_OVERFLOW",
            "The hexadecimal floating-point literal exceeds the binary64 range.",
            0,
            Raw.Length
        );
    }

    internal void SetFailure(
        string code,
        string message,
        int relativeOffset,
        int length)
    {
        if (ErrorCode != null)
        {
            return;
        }
        ErrorCode = code;
        ErrorMessage = message;
        ErrorRelativeOffset = relativeOffset;
        ErrorLength = length < 0 ? 0 : length;
    }

    internal CddlNumberResult CreateFailureResult()
    {
        CddlNumberResult result = new CddlNumberResult();
        result.ErrorCode = ErrorCode ?? "CDDL_NUMBER_INVALID";
        result.ErrorMessage = ErrorMessage ?? "The CDDL number is invalid.";
        result.ErrorSpan = CreateErrorSpan();
        return result;
    }

    internal CddlSourceSpan CreateErrorSpan()
    {
        int relative = ErrorRelativeOffset;
        if (relative < 0)
        {
            relative = 0;
        }
        if (relative > Raw.Length)
        {
            relative = Raw.Length;
        }
        int length = ErrorLength;
        if (length > Raw.Length - relative)
        {
            length = Raw.Length - relative;
        }
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = Token.Span.SourceName;
        span.Start = Token.Span.Start + relative;
        span.Length = length;
        span.StartLine = Token.Span.StartLine;
        span.StartColumn = Token.Span.StartColumn + relative;
        span.EndLine = Token.Span.StartLine;
        span.EndColumn = span.StartColumn + length;
        return span;
    }
}
