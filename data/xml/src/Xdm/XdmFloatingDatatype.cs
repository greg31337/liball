using System;
using System.Globalization;
using System.Numerics;

namespace xml;

internal class XdmFloatingDatatype
{
    internal const int UnorderedComparison = 2;
    internal XdmTypeSystem TypeSystem;

    internal XdmFloatingDatatype()
    {
        TypeSystem = new XdmTypeSystem();
    }

    internal XdmAtomicValue ParseXdmFloating(string lexicalValue, XdmAtomicType targetType)
    {
        XdmNumericParseResult result = TryParseXdmFloating(lexicalValue, targetType);
        return new XdmNumericDatatype().RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmFloating(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmFloatingType(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        if (!IsValidXdmFloatingLexical(normalized))
        {
            return new XdmNumericDatatype().InvalidXdmNumericLexical(targetType == XdmAtomicType.Float ? "xs:float" : "xs:double");
        }

        double number;
        if (normalized == "INF" || normalized == "+INF")
        {
            number = double.PositiveInfinity;
        }
        else if (normalized == "-INF")
        {
            number = double.NegativeInfinity;
        }
        else if (normalized == "NaN")
        {
            number = double.NaN;
        }
        else if (targetType == XdmAtomicType.Float)
        {
            float parsed = float.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (parsed == 0f && normalized.Length > 0 && normalized[0] == '-')
            {
                parsed = BitConverter.Int32BitsToSingle(int.MinValue);
            }

            number = parsed;
        }
        else
        {
            number = double.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (number == 0d && normalized.Length > 0 && normalized[0] == '-')
            {
                number = BitConverter.Int64BitsToDouble(long.MinValue);
            }
        }

        return new XdmNumericParseResult(true, CreateXdmFloatingValue(number, targetType, lexicalValue), null, null);
    }

    internal bool IsValidXdmFloatingLexical(string lexicalValue)
    {
        if (lexicalValue == "INF" || lexicalValue == "+INF" || lexicalValue == "-INF" || lexicalValue == "NaN")
        {
            return true;
        }

        int index = 0;
        if (index < lexicalValue.Length && (lexicalValue[index] == '+' || lexicalValue[index] == '-'))
        {
            index = index + 1;
        }

        int mantissaDigits = 0;
        while (index < lexicalValue.Length && lexicalValue[index] >= '0' && lexicalValue[index] <= '9')
        {
            mantissaDigits = mantissaDigits + 1;
            index = index + 1;
        }

        if (index < lexicalValue.Length && lexicalValue[index] == '.')
        {
            index = index + 1;
            while (index < lexicalValue.Length && lexicalValue[index] >= '0' && lexicalValue[index] <= '9')
            {
                mantissaDigits = mantissaDigits + 1;
                index = index + 1;
            }
        }

        if (mantissaDigits == 0)
        {
            return false;
        }

        if (index < lexicalValue.Length && (lexicalValue[index] == 'E' || lexicalValue[index] == 'e'))
        {
            index = index + 1;
            if (index < lexicalValue.Length && (lexicalValue[index] == '+' || lexicalValue[index] == '-'))
            {
                index = index + 1;
            }

            int exponentStart = index;
            while (index < lexicalValue.Length && lexicalValue[index] >= '0' && lexicalValue[index] <= '9')
            {
                index = index + 1;
            }

            if (index == exponentStart)
            {
                return false;
            }
        }

        return index == lexicalValue.Length;
    }

    internal XdmAtomicValue CreateXdmFloatingValue(double source, XdmAtomicType type, string? lexicalValue)
    {
        RequireXdmFloatingType(type);
        double number = type == XdmAtomicType.Float ? (float)source : source;
        string canonical = CanonicalizeXdmFloating(number, type);
        string stringValue = FormatXdmFloatingStringValue(number, type);
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = type;
        value.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        value.CanonicalValue = canonical;
        value.StringValue = stringValue;
        value.FloatingValue = number;
        value.IsImmutable = true;
        return value;
    }

    internal string FormatXdmFloatingStringValue(double source, XdmAtomicType type)
    {
        RequireXdmFloatingType(type);
        double number = type == XdmAtomicType.Float ? (float)source : source;
        if (double.IsNaN(number))
        {
            return "NaN";
        }

        if (double.IsPositiveInfinity(number))
        {
            return "INF";
        }

        if (double.IsNegativeInfinity(number))
        {
            return "-INF";
        }

        if (number == 0d)
        {
            return IsNegativeXdmFloating(number, type) ? "-0" : "0";
        }

        double magnitude = Math.Abs(number);
        string roundTrip = type == XdmAtomicType.Float ? ((float)number).ToString("R", CultureInfo.InvariantCulture) : number.ToString("R", CultureInfo.InvariantCulture);
        if (magnitude >= 0.000001d && magnitude < 1000000d)
        {
            int exponentIndex = roundTrip.IndexOf('E');
            if (exponentIndex < 0)
            {
                exponentIndex = roundTrip.IndexOf('e');
            }

            if (exponentIndex < 0)
            {
                return roundTrip;
            }

            return ConvertXdmRoundTripToPlain(roundTrip);
        }

        return ConvertXdmRoundTripToScientific(roundTrip);
    }

    internal string ConvertXdmRoundTripToPlain(string roundTrip)
    {
        bool negative = roundTrip.Length > 0 && roundTrip[0] == '-';
        string unsigned = negative || roundTrip.Length > 0 && roundTrip[0] == '+' ? roundTrip.Substring(1) : roundTrip;
        int exponentIndex = unsigned.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = unsigned.IndexOf('e');
        }

        if (exponentIndex < 0)
        {
            return roundTrip;
        }

        int exponent = int.Parse(unsigned.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);
        string mantissa = unsigned.Substring(0, exponentIndex);
        int decimalIndex = mantissa.IndexOf('.');
        int decimalPosition = (decimalIndex < 0 ? mantissa.Length : decimalIndex) + exponent;
        string digits = decimalIndex < 0 ? mantissa : mantissa.Substring(0, decimalIndex) + mantissa.Substring(decimalIndex + 1);
        string result;
        if (decimalPosition <= 0)
        {
            result = "0." + new string('0', -decimalPosition) + digits;
        }
        else if (decimalPosition >= digits.Length)
        {
            result = digits + new string('0', decimalPosition - digits.Length);
        }
        else
        {
            result = digits.Substring(0, decimalPosition) + "." + digits.Substring(decimalPosition);
        }

        return negative ? "-" + result : result;
    }

    internal string CanonicalizeXdmFloating(double source, XdmAtomicType type)
    {
        RequireXdmFloatingType(type);
        double number = type == XdmAtomicType.Float ? (float)source : source;
        if (double.IsNaN(number))
        {
            return "NaN";
        }

        if (double.IsPositiveInfinity(number))
        {
            return "INF";
        }

        if (double.IsNegativeInfinity(number))
        {
            return "-INF";
        }

        if (number == 0d)
        {
            return IsNegativeXdmFloating(number, type) ? "-0.0E0" : "0.0E0";
        }

        string roundTrip = type == XdmAtomicType.Float ? ((float)number).ToString("R", CultureInfo.InvariantCulture) : number.ToString("R", CultureInfo.InvariantCulture);
        return ConvertXdmRoundTripToScientific(roundTrip);
    }

    internal string ConvertXdmRoundTripToScientific(string roundTrip)
    {
        bool negative = roundTrip.Length > 0 && roundTrip[0] == '-';
        string unsigned = negative || roundTrip.Length > 0 && roundTrip[0] == '+' ? roundTrip.Substring(1) : roundTrip;
        int exponentIndex = unsigned.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = unsigned.IndexOf('e');
        }

        int explicitExponent = 0;
        string mantissa = unsigned;
        if (exponentIndex >= 0)
        {
            explicitExponent = int.Parse(unsigned.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);
            mantissa = unsigned.Substring(0, exponentIndex);
        }

        int decimalIndex = mantissa.IndexOf('.');
        int decimalPosition = decimalIndex < 0 ? mantissa.Length : decimalIndex;
        string digits = decimalIndex < 0 ? mantissa : mantissa.Substring(0, decimalIndex) + mantissa.Substring(decimalIndex + 1);
        int leading = 0;
        while (leading < digits.Length && digits[leading] == '0')
        {
            leading = leading + 1;
        }

        int trailing = digits.Length - 1;
        while (trailing > leading && digits[trailing] == '0')
        {
            trailing = trailing - 1;
        }

        string significant = digits.Substring(leading, trailing - leading + 1);
        int scientificExponent = explicitExponent + decimalPosition - leading - 1;
        string fraction = significant.Length == 1 ? "0" : significant.Substring(1);
        string sign = negative ? "-" : "";
        return sign + significant[0] + "." + fraction + "E" + scientificExponent.ToString(CultureInfo.InvariantCulture);
    }

    internal XdmAtomicValue PromoteXdmFloating(XdmAtomicValue value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        RequireXdmFloatingType(targetType);
        if (value.Type == XdmAtomicType.Double && targetType == XdmAtomicType.Float)
        {
            throw new ArgumentException("An xs:double value cannot be promoted to xs:float.", "targetType");
        }

        double promoted = ReadXdmFloatingValue(value, targetType);
        return CreateXdmFloatingValue(promoted, targetType, null);
    }

    internal double ReadXdmFloatingValue(XdmAtomicValue value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        RequireXdmFloatingType(targetType);
        if (value.Type == XdmAtomicType.Float)
        {
            float number = (float)value.FloatingValue;
            return targetType == XdmAtomicType.Float ? number : (double)number;
        }

        if (value.Type == XdmAtomicType.Double)
        {
            if (targetType != XdmAtomicType.Double)
            {
                throw new ArgumentException("An xs:double value cannot be promoted to xs:float.", "value");
            }

            return value.FloatingValue;
        }

        XdmNumericDatatype numeric = new XdmNumericDatatype();
        numeric.RequireXdmSupportedNumeric(value);
        string representation;
        if (TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            representation = numeric.ReadXdmIntegerValue(value).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            representation = numeric.CanonicalizeXdmDecimal(numeric.ReadXdmDecimalStorage(value));
        }

        if (targetType == XdmAtomicType.Float)
        {
            return float.Parse(representation, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return double.Parse(representation, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    internal XdmAtomicType CommonXdmFloatingType(XdmAtomicValue left, XdmAtomicValue right)
    {
        return left.Type == XdmAtomicType.Double || right.Type == XdmAtomicType.Double ? XdmAtomicType.Double : XdmAtomicType.Float;
    }

    internal int CompareXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        XdmAtomicType type = CommonXdmFloatingType(left, right);
        double leftNumber = ReadXdmFloatingValue(left, type);
        double rightNumber = ReadXdmFloatingValue(right, type);
        if (double.IsNaN(leftNumber) || double.IsNaN(rightNumber))
        {
            return UnorderedComparison;
        }

        if (leftNumber < rightNumber)
        {
            return -1;
        }

        if (leftNumber > rightNumber)
        {
            return 1;
        }

        return 0;
    }

    internal bool EqualXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmFloating(left, right) == 0;
    }

    internal bool LessThanXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmFloating(left, right) < 0;
    }

    internal bool GreaterThanXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmFloating(left, right) == 1;
    }

    internal XdmAtomicValue AddXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CalculateXdmFloating(left, right, 0);
    }

    internal XdmAtomicValue SubtractXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CalculateXdmFloating(left, right, 1);
    }

    internal XdmAtomicValue MultiplyXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CalculateXdmFloating(left, right, 2);
    }

    internal XdmAtomicValue DivideXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CalculateXdmFloating(left, right, 3);
    }

    internal XdmAtomicValue ModuloXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CalculateXdmFloating(left, right, 4);
    }

    internal XdmAtomicValue CalculateXdmFloating(XdmAtomicValue left, XdmAtomicValue right, int operation)
    {
        XdmAtomicType type = CommonXdmFloatingType(left, right);
        double result;
        if (type == XdmAtomicType.Float)
        {
            float leftNumber = (float)ReadXdmFloatingValue(left, type);
            float rightNumber = (float)ReadXdmFloatingValue(right, type);
            float floatResult = operation == 0 ? leftNumber + rightNumber : operation == 1 ? leftNumber - rightNumber : operation == 2 ? leftNumber * rightNumber : operation == 3 ? leftNumber / rightNumber : leftNumber % rightNumber;
            result = floatResult;
        }
        else
        {
            double leftNumber = ReadXdmFloatingValue(left, type);
            double rightNumber = ReadXdmFloatingValue(right, type);
            result = operation == 0 ? leftNumber + rightNumber : operation == 1 ? leftNumber - rightNumber : operation == 2 ? leftNumber * rightNumber : operation == 3 ? leftNumber / rightNumber : leftNumber % rightNumber;
        }

        return CreateXdmFloatingValue(result, type, null);
    }

    internal XdmAtomicValue IntegerDivideXdmFloating(XdmAtomicValue left, XdmAtomicValue right)
    {
        XdmAtomicType type = CommonXdmFloatingType(left, right);
        double dividend = ReadXdmFloatingValue(left, type);
        double divisor = ReadXdmFloatingValue(right, type);
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        if (divisor == 0d)
        {
            throw numeric.CreateXdmNumericError(XPathErrorCodes.FOAR0001, "Integer division by zero is not defined.");
        }

        if (double.IsNaN(dividend) || double.IsNaN(divisor) || double.IsInfinity(dividend))
        {
            throw numeric.CreateXdmNumericError(XPathErrorCodes.FOAR0002, "The floating-point operands do not have a finite integer quotient.");
        }

        if (double.IsInfinity(divisor))
        {
            return numeric.CreateXdmIntegerValue(BigInteger.Zero, XdmAtomicType.Integer, null);
        }

        double quotient = type == XdmAtomicType.Float ? (float)dividend / (float)divisor : dividend / divisor;
        if (double.IsInfinity(quotient) || double.IsNaN(quotient))
        {
            throw numeric.CreateXdmNumericError(XPathErrorCodes.FOAR0002, "The floating-point integer quotient overflowed.");
        }

        return numeric.CreateXdmIntegerValue(new BigInteger(quotient), XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue PositiveXdmFloating(XdmAtomicValue value)
    {
        RequireXdmFloatingValue(value);
        return CreateXdmFloatingValue(value.FloatingValue, value.Type, null);
    }

    internal XdmAtomicValue NegateXdmFloating(XdmAtomicValue value)
    {
        RequireXdmFloatingValue(value);
        double number = value.Type == XdmAtomicType.Float ? -(float)value.FloatingValue : -value.FloatingValue;
        return CreateXdmFloatingValue(number, value.Type, null);
    }

    internal XdmAtomicValue AbsoluteXdmFloating(XdmAtomicValue value)
    {
        RequireXdmFloatingValue(value);
        double number = value.Type == XdmAtomicType.Float ? MathF.Abs((float)value.FloatingValue) : Math.Abs(value.FloatingValue);
        return CreateXdmFloatingValue(number, value.Type, null);
    }

    internal XdmAtomicValue FloorXdmFloating(XdmAtomicValue value)
    {
        RequireXdmFloatingValue(value);
        double number = value.Type == XdmAtomicType.Float ? MathF.Floor((float)value.FloatingValue) : Math.Floor(value.FloatingValue);
        return CreateXdmFloatingValue(number, value.Type, null);
    }

    internal XdmAtomicValue CeilingXdmFloating(XdmAtomicValue value)
    {
        RequireXdmFloatingValue(value);
        double number = value.Type == XdmAtomicType.Float ? MathF.Ceiling((float)value.FloatingValue) : Math.Ceiling(value.FloatingValue);
        return CreateXdmFloatingValue(number, value.Type, null);
    }

    internal XdmAtomicValue RoundXdmFloating(XdmAtomicValue value, int precision)
    {
        return ApplyXdmFloatingRounding(value, precision, XdmRoundingMode.HalfTowardPositiveInfinity);
    }

    internal XdmAtomicValue RoundHalfToEvenXdmFloating(XdmAtomicValue value, int precision)
    {
        return ApplyXdmFloatingRounding(value, precision, XdmRoundingMode.HalfToEven);
    }

    internal XdmAtomicValue ApplyXdmFloatingRounding(XdmAtomicValue value, int precision, XdmRoundingMode mode)
    {
        RequireXdmFloatingValue(value);
        double source = value.Type == XdmAtomicType.Float ? (float)value.FloatingValue : value.FloatingValue;
        if (source == 0d || double.IsNaN(source) || double.IsInfinity(source))
        {
            return CreateXdmFloatingValue(source, value.Type, null);
        }

        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmDecimalStorage exact = ConvertXdmFloatingToExactDecimal(source, value.Type);
        XdmAtomicValue decimalValue = numeric.CreateXdmDecimalValue(exact, null);
        XdmAtomicValue rounded = numeric.ApplyXdmRounding(decimalValue, precision, mode);
        string representation = numeric.CanonicalizeXdmDecimal(numeric.ReadXdmDecimalStorage(rounded));
        double result = value.Type == XdmAtomicType.Float ? float.Parse(representation, NumberStyles.Float, CultureInfo.InvariantCulture) : double.Parse(representation, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (result == 0d && IsNegativeXdmFloating(source, value.Type))
        {
            result = value.Type == XdmAtomicType.Float ? BitConverter.Int32BitsToSingle(int.MinValue) : BitConverter.Int64BitsToDouble(long.MinValue);
        }

        return CreateXdmFloatingValue(result, value.Type, null);
    }

    internal XdmDecimalStorage ConvertXdmFloatingToExactDecimal(double source, XdmAtomicType type)
    {
        RequireXdmFloatingType(type);
        BigInteger mantissa;
        int binaryExponent;
        bool negative;
        if (type == XdmAtomicType.Float)
        {
            int bits = BitConverter.SingleToInt32Bits((float)source);
            negative = bits < 0;
            int exponentBits = bits >> 23 & 0xff;
            int fractionBits = bits & 0x7fffff;
            mantissa = exponentBits == 0 ? new BigInteger(fractionBits) : new BigInteger(0x800000 | fractionBits);
            binaryExponent = exponentBits == 0 ? -149 : exponentBits - 127 - 23;
        }
        else
        {
            long signedBits = BitConverter.DoubleToInt64Bits(source);
            negative = signedBits < 0;
            ulong bits = (ulong)(signedBits & long.MaxValue);
            int exponentBits = (int)(bits >> 52 & 0x7ff);
            ulong fractionBits = bits & 0xfffffffffffff;
            mantissa = exponentBits == 0 ? new BigInteger(fractionBits) : new BigInteger(0x10000000000000 | fractionBits);
            binaryExponent = exponentBits == 0 ? -1074 : exponentBits - 1023 - 52;
        }

        XdmDecimalStorage storage;
        if (binaryExponent >= 0)
        {
            storage = new XdmDecimalStorage(mantissa * BigInteger.Pow(new BigInteger(2), binaryExponent), 0);
        }
        else
        {
            int scale = -binaryExponent;
            storage = new XdmDecimalStorage(mantissa * BigInteger.Pow(new BigInteger(5), scale), scale);
        }

        if (negative)
        {
            storage.Coefficient = -storage.Coefficient;
        }

        return new XdmNumericDatatype().NormalizeXdmDecimalStorage(storage);
    }

    internal bool IsNegativeXdmFloating(double source, XdmAtomicType type)
    {
        return type == XdmAtomicType.Float ? BitConverter.SingleToInt32Bits((float)source) < 0 : BitConverter.DoubleToInt64Bits(source) < 0;
    }

    internal void RequireXdmFloatingType(XdmAtomicType type)
    {
        if (type != XdmAtomicType.Float && type != XdmAtomicType.Double)
        {
            throw new ArgumentException("The requested type is not xs:float or xs:double.", "type");
        }
    }

    internal void RequireXdmFloatingValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (value.Type != XdmAtomicType.Float && value.Type != XdmAtomicType.Double)
        {
            throw new ArgumentException("The value is not xs:float or xs:double.", "value");
        }
    }
}
