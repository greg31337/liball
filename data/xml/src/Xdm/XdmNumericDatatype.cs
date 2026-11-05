using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XdmNumericDatatype
{
    internal const int MaximumNumericDigits = 10000;
    internal const int DivisionScale = 18;
    internal XdmTypeSystem TypeSystem;
    internal BigInteger DecimalStorageMaximum;
    internal BigInteger UnsignedLongMaximum;

    internal XdmNumericDatatype()
    {
        TypeSystem = new XdmTypeSystem();
        DecimalStorageMaximum = BigInteger.Parse("79228162514264337593543950335", CultureInfo.InvariantCulture);
        UnsignedLongMaximum = BigInteger.Parse("18446744073709551615", CultureInfo.InvariantCulture);
    }

    internal XdmAtomicValue ParseXdmBoolean(string lexicalValue)
    {
        XdmNumericParseResult result = TryParseXdmBoolean(lexicalValue);
        return RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmBoolean(string lexicalValue)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        bool valid = normalized == "true" || normalized == "1" || normalized == "false" || normalized == "0";
        if (!valid)
        {
            return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for xs:boolean.");
        }

        bool boolean = normalized == "true" || normalized == "1";
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = XdmAtomicType.Boolean;
        value.LexicalValue = lexicalValue;
        value.CanonicalValue = boolean ? "true" : "false";
        value.StringValue = value.CanonicalValue;
        value.BooleanValue = boolean;
        value.IsImmutable = true;
        return new XdmNumericParseResult(true, value, null, null);
    }

    internal XdmAtomicValue ParseXdmDecimal(string lexicalValue)
    {
        XdmNumericParseResult result = TryParseXdmDecimal(lexicalValue);
        return RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmDecimal(string lexicalValue)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        int index = 0;
        bool negative = false;
        if (index < normalized.Length && (normalized[index] == '+' || normalized[index] == '-'))
        {
            negative = normalized[index] == '-';
            index = index + 1;
        }

        StringBuilder digits = new StringBuilder(normalized.Length);
        bool decimalPoint = false;
        int digitCount = 0;
        int scale = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character >= '0' && character <= '9')
            {
                digits.Append(character);
                digitCount = digitCount + 1;
                if (decimalPoint)
                {
                    scale = scale + 1;
                }
            }
            else if (character == '.' && !decimalPoint)
            {
                decimalPoint = true;
            }
            else
            {
                return InvalidXdmNumericLexical("xs:decimal");
            }

            index = index + 1;
        }

        if (digitCount == 0 || digitCount > MaximumNumericDigits)
        {
            return digitCount > MaximumNumericDigits ? OverflowXdmNumericLexical("xs:decimal") : InvalidXdmNumericLexical("xs:decimal");
        }

        BigInteger coefficient = BigInteger.Parse(digits.ToString(), CultureInfo.InvariantCulture);
        if (negative)
        {
            coefficient = -coefficient;
        }

        XdmDecimalStorage storage = NormalizeXdmDecimalStorage(new XdmDecimalStorage(coefficient, scale));
        return new XdmNumericParseResult(true, CreateXdmDecimalValue(storage, lexicalValue), null, null);
    }

    internal XdmAtomicValue ParseXdmInteger(string lexicalValue, XdmAtomicType targetType)
    {
        XdmNumericParseResult result = TryParseXdmInteger(lexicalValue, targetType);
        return RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmInteger(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmIntegerDatatype(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        int index = 0;
        bool negative = false;
        if (index < normalized.Length && (normalized[index] == '+' || normalized[index] == '-'))
        {
            negative = normalized[index] == '-';
            index = index + 1;
        }

        int digitStart = index;
        while (index < normalized.Length && normalized[index] >= '0' && normalized[index] <= '9')
        {
            index = index + 1;
        }

        int digitCount = index - digitStart;
        if (digitCount == 0 || index != normalized.Length)
        {
            return InvalidXdmNumericLexical(GetXdmNumericTypeDisplayName(targetType));
        }

        if (digitCount > MaximumNumericDigits)
        {
            return OverflowXdmNumericLexical(GetXdmNumericTypeDisplayName(targetType));
        }

        BigInteger integer = BigInteger.Parse(normalized.Substring(digitStart), CultureInfo.InvariantCulture);
        if (negative)
        {
            integer = -integer;
        }

        if (!IsWithinXdmIntegerBounds(integer, targetType))
        {
            string typeName = GetXdmNumericTypeDisplayName(targetType);
            return new XdmNumericParseResult(false, null, XPathErrorCodes.FOCA0002, "The value is outside the range of " + typeName + ".");
        }

        return new XdmNumericParseResult(true, CreateXdmIntegerValue(integer, targetType, lexicalValue), null, null);
    }

    internal int CompareXdmBoolean(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmBooleanValue(left);
        RequireXdmBooleanValue(right);
        if (left.BooleanValue == right.BooleanValue)
        {
            return 0;
        }

        return left.BooleanValue ? 1 : -1;
    }

    internal int CompareXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmSupportedNumeric(left);
        RequireXdmSupportedNumeric(right);
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().CompareXdmFloating(left, right);
        }

        XdmDecimalStorage leftValue = ReadXdmDecimalStorage(left);
        XdmDecimalStorage rightValue = ReadXdmDecimalStorage(right);
        int scale = leftValue.Scale > rightValue.Scale ? leftValue.Scale : rightValue.Scale;
        BigInteger leftCoefficient = ScaleXdmDecimalCoefficient(leftValue, scale);
        BigInteger rightCoefficient = ScaleXdmDecimalCoefficient(rightValue, scale);
        return leftCoefficient.CompareTo(rightCoefficient);
    }

    internal bool EqualXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmNumeric(left, right) == 0;
    }

    internal bool LessThanXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmNumeric(left, right) == -1;
    }

    internal bool GreaterThanXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        return CompareXdmNumeric(left, right) == 1;
    }

    internal XdmAtomicValue PromoteXdmNumeric(XdmAtomicValue value, XdmAtomicType targetType)
    {
        RequireXdmSupportedNumeric(value);
        if (IsXdmFloatingValue(value) || targetType == XdmAtomicType.Float || targetType == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().PromoteXdmFloating(value, targetType);
        }

        if (targetType == XdmAtomicType.Decimal)
        {
            return CreateXdmDecimalValue(ReadXdmDecimalStorage(value), null);
        }

        if (targetType == XdmAtomicType.Integer && TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            return CreateXdmIntegerValue(ReadXdmIntegerValue(value), XdmAtomicType.Integer, null);
        }

        throw new ArgumentException("The requested promotion is not supported by the decimal/integer numeric core.", "targetType");
    }

    internal XdmAtomicValue AddXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().AddXdmFloating(left, right);
        }

        return CalculateXdmNumeric(left, right, 0);
    }

    internal XdmAtomicValue SubtractXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().SubtractXdmFloating(left, right);
        }

        return CalculateXdmNumeric(left, right, 1);
    }

    internal XdmAtomicValue MultiplyXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().MultiplyXdmFloating(left, right);
        }

        return CalculateXdmNumeric(left, right, 2);
    }

    internal XdmAtomicValue DivideXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmSupportedNumeric(left);
        RequireXdmSupportedNumeric(right);
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().DivideXdmFloating(left, right);
        }

        XdmDecimalStorage dividend = ReadXdmDecimalStorage(left);
        XdmDecimalStorage divisor = ReadXdmDecimalStorage(right);
        if (divisor.Coefficient == BigInteger.Zero)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0001, "Division by zero is not defined.");
        }

        int exponent = divisor.Scale - dividend.Scale + DivisionScale;
        BigInteger numerator = dividend.Coefficient;
        BigInteger denominator = divisor.Coefficient;
        if (exponent >= 0)
        {
            numerator = numerator * PowerOfTen(exponent);
        }
        else
        {
            denominator = denominator * PowerOfTen(-exponent);
        }

        BigInteger coefficient = DivideXdmHalfToEven(numerator, denominator);
        return CreateXdmDecimalValue(new XdmDecimalStorage(coefficient, DivisionScale), null);
    }

    internal XdmAtomicValue IntegerDivideXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmSupportedNumeric(left);
        RequireXdmSupportedNumeric(right);
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().IntegerDivideXdmFloating(left, right);
        }

        XdmDecimalStorage dividend = ReadXdmDecimalStorage(left);
        XdmDecimalStorage divisor = ReadXdmDecimalStorage(right);
        if (divisor.Coefficient == BigInteger.Zero)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0001, "Integer division by zero is not defined.");
        }

        int scale = dividend.Scale > divisor.Scale ? dividend.Scale : divisor.Scale;
        BigInteger numerator = ScaleXdmDecimalCoefficient(dividend, scale);
        BigInteger denominator = ScaleXdmDecimalCoefficient(divisor, scale);
        BigInteger quotient = numerator / denominator;
        return CreateXdmIntegerValue(quotient, XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue ModuloXdmNumeric(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmSupportedNumeric(left);
        RequireXdmSupportedNumeric(right);
        if (IsXdmFloatingValue(left) || IsXdmFloatingValue(right))
        {
            return new XdmFloatingDatatype().ModuloXdmFloating(left, right);
        }

        bool integerResult = TypeSystem.IsXdmAtomicSubtypeOf(left.Type, XdmAtomicType.Integer) && TypeSystem.IsXdmAtomicSubtypeOf(right.Type, XdmAtomicType.Integer);
        XdmDecimalStorage dividend = ReadXdmDecimalStorage(left);
        XdmDecimalStorage divisor = ReadXdmDecimalStorage(right);
        if (divisor.Coefficient == BigInteger.Zero)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0001, "Modulo by zero is not defined.");
        }

        int scale = dividend.Scale > divisor.Scale ? dividend.Scale : divisor.Scale;
        BigInteger numerator = ScaleXdmDecimalCoefficient(dividend, scale);
        BigInteger denominator = ScaleXdmDecimalCoefficient(divisor, scale);
        BigInteger remainder = numerator % denominator;
        if (integerResult)
        {
            return CreateXdmIntegerValue(remainder, XdmAtomicType.Integer, null);
        }

        return CreateXdmDecimalValue(new XdmDecimalStorage(remainder, scale), null);
    }

    internal XdmAtomicValue NegateXdmNumeric(XdmAtomicValue value)
    {
        RequireXdmSupportedNumeric(value);
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().NegateXdmFloating(value);
        }

        if (TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            return CreateXdmIntegerValue(-ReadXdmIntegerValue(value), XdmAtomicType.Integer, null);
        }

        XdmDecimalStorage decimalValue = ReadXdmDecimalStorage(value);
        return CreateXdmDecimalValue(new XdmDecimalStorage(-decimalValue.Coefficient, decimalValue.Scale), null);
    }

    internal XdmAtomicValue FloorXdmNumeric(XdmAtomicValue value)
    {
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().FloorXdmFloating(value);
        }

        return ApplyXdmRounding(value, 0, XdmRoundingMode.Floor);
    }

    internal XdmAtomicValue CeilingXdmNumeric(XdmAtomicValue value)
    {
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().CeilingXdmFloating(value);
        }

        return ApplyXdmRounding(value, 0, XdmRoundingMode.Ceiling);
    }

    internal XdmAtomicValue RoundXdmNumeric(XdmAtomicValue value, int precision)
    {
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().RoundXdmFloating(value, precision);
        }

        return ApplyXdmRounding(value, precision, XdmRoundingMode.HalfTowardPositiveInfinity);
    }

    internal XdmAtomicValue RoundHalfToEvenXdmNumeric(XdmAtomicValue value, int precision)
    {
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().RoundHalfToEvenXdmFloating(value, precision);
        }

        return ApplyXdmRounding(value, precision, XdmRoundingMode.HalfToEven);
    }

    internal XdmAtomicValue PositiveXdmNumeric(XdmAtomicValue value)
    {
        RequireXdmSupportedNumeric(value);
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().PositiveXdmFloating(value);
        }

        if (TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            return CreateXdmIntegerValue(ReadXdmIntegerValue(value), XdmAtomicType.Integer, null);
        }

        return CreateXdmDecimalValue(ReadXdmDecimalStorage(value), null);
    }

    internal XdmAtomicValue AbsoluteXdmNumeric(XdmAtomicValue value)
    {
        RequireXdmSupportedNumeric(value);
        if (IsXdmFloatingValue(value))
        {
            return new XdmFloatingDatatype().AbsoluteXdmFloating(value);
        }

        if (TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            return CreateXdmIntegerValue(BigInteger.Abs(ReadXdmIntegerValue(value)), XdmAtomicType.Integer, null);
        }

        XdmDecimalStorage storage = ReadXdmDecimalStorage(value);
        return CreateXdmDecimalValue(new XdmDecimalStorage(BigInteger.Abs(storage.Coefficient), storage.Scale), null);
    }

    internal XdmAtomicValue ApplyXdmRounding(XdmAtomicValue value, int precision, XdmRoundingMode mode)
    {
        RequireXdmSupportedNumeric(value);
        bool integer = TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer);
        XdmDecimalStorage storage = ReadXdmDecimalStorage(value);
        if (precision >= storage.Scale)
        {
            return integer ? CreateXdmIntegerValue(storage.Coefficient, XdmAtomicType.Integer, null) : CreateXdmDecimalValue(storage, null);
        }

        if (precision < -MaximumNumericDigits)
        {
            return integer ? CreateXdmIntegerValue(BigInteger.Zero, XdmAtomicType.Integer, null) : CreateXdmDecimalValue(new XdmDecimalStorage(BigInteger.Zero, 0), null);
        }

        int removedDigits = storage.Scale - precision;
        if (removedDigits > MaximumNumericDigits)
        {
            return integer ? CreateXdmIntegerValue(BigInteger.Zero, XdmAtomicType.Integer, null) : CreateXdmDecimalValue(new XdmDecimalStorage(BigInteger.Zero, 0), null);
        }

        BigInteger divisor = PowerOfTen(removedDigits);
        BigInteger quotient = storage.Coefficient / divisor;
        BigInteger remainder = storage.Coefficient % divisor;
        quotient = AdjustXdmRoundedQuotient(quotient, remainder, divisor, mode);
        XdmDecimalStorage rounded;
        if (precision >= 0)
        {
            rounded = new XdmDecimalStorage(quotient, precision);
        }
        else
        {
            rounded = new XdmDecimalStorage(quotient * PowerOfTen(-precision), 0);
        }

        return integer ? CreateXdmIntegerValue(rounded.Coefficient, XdmAtomicType.Integer, null) : CreateXdmDecimalValue(rounded, null);
    }

    internal XdmAtomicValue CalculateXdmNumeric(XdmAtomicValue left, XdmAtomicValue right, int operation)
    {
        RequireXdmSupportedNumeric(left);
        RequireXdmSupportedNumeric(right);
        bool integers = TypeSystem.IsXdmAtomicSubtypeOf(left.Type, XdmAtomicType.Integer) && TypeSystem.IsXdmAtomicSubtypeOf(right.Type, XdmAtomicType.Integer);
        if (integers)
        {
            BigInteger leftInteger = ReadXdmIntegerValue(left);
            BigInteger rightInteger = ReadXdmIntegerValue(right);
            BigInteger result = operation == 0 ? leftInteger + rightInteger : operation == 1 ? leftInteger - rightInteger : leftInteger * rightInteger;
            CheckXdmNumericMagnitude(result, 0);
            return CreateXdmIntegerValue(result, XdmAtomicType.Integer, null);
        }

        XdmDecimalStorage leftDecimal = ReadXdmDecimalStorage(left);
        XdmDecimalStorage rightDecimal = ReadXdmDecimalStorage(right);
        XdmDecimalStorage decimalResult;
        if (operation == 2)
        {
            decimalResult = new XdmDecimalStorage(leftDecimal.Coefficient * rightDecimal.Coefficient, leftDecimal.Scale + rightDecimal.Scale);
        }
        else
        {
            int scale = leftDecimal.Scale > rightDecimal.Scale ? leftDecimal.Scale : rightDecimal.Scale;
            BigInteger leftCoefficient = ScaleXdmDecimalCoefficient(leftDecimal, scale);
            BigInteger rightCoefficient = ScaleXdmDecimalCoefficient(rightDecimal, scale);
            decimalResult = new XdmDecimalStorage(operation == 0 ? leftCoefficient + rightCoefficient : leftCoefficient - rightCoefficient, scale);
        }

        CheckXdmNumericMagnitude(decimalResult.Coefficient, decimalResult.Scale);
        return CreateXdmDecimalValue(decimalResult, null);
    }

    internal XdmAtomicValue CreateXdmIntegerValue(BigInteger integer, XdmAtomicType type, string? lexicalValue)
    {
        CheckXdmNumericMagnitude(integer, 0);
        XdmAtomicValue value = new XdmAtomicValue();
        string canonical = integer.ToString(CultureInfo.InvariantCulture);
        value.Type = type;
        value.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        value.CanonicalValue = canonical;
        value.StringValue = canonical;
        value.ExtendedValue = new XdmIntegerStorage(integer);
        if (integer >= new BigInteger(long.MinValue) && integer <= new BigInteger(long.MaxValue))
        {
            value.IntegerValue = (long)integer;
        }

        value.IsImmutable = true;
        return value;
    }

    internal XdmAtomicValue CreateXdmDecimalValue(XdmDecimalStorage source, string? lexicalValue)
    {
        XdmDecimalStorage storage = NormalizeXdmDecimalStorage(new XdmDecimalStorage(source.Coefficient, source.Scale));
        CheckXdmNumericMagnitude(storage.Coefficient, storage.Scale);
        string canonical = CanonicalizeXdmDecimal(storage);
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = XdmAtomicType.Decimal;
        value.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        value.CanonicalValue = canonical;
        value.StringValue = canonical;
        value.ExtendedValue = storage;
        BigInteger absolute = BigInteger.Abs(storage.Coefficient);
        if (absolute <= DecimalStorageMaximum && storage.Scale <= 28)
        {
            decimal decimalValue = (decimal)storage.Coefficient;
            int index = 0;
            while (index < storage.Scale)
            {
                decimalValue = decimalValue / 10m;
                index = index + 1;
            }

            value.DecimalValue = decimalValue;
        }

        value.IsImmutable = true;
        return value;
    }

    internal string CanonicalizeXdmDecimal(XdmDecimalStorage source)
    {
        XdmDecimalStorage storage = NormalizeXdmDecimalStorage(new XdmDecimalStorage(source.Coefficient, source.Scale));
        bool negative = storage.Coefficient < BigInteger.Zero;
        string digits = BigInteger.Abs(storage.Coefficient).ToString(CultureInfo.InvariantCulture);
        string result;
        if (storage.Scale == 0)
        {
            result = digits;
        }
        else if (digits.Length <= storage.Scale)
        {
            result = "0." + new string('0', storage.Scale - digits.Length) + digits;
        }
        else
        {
            int point = digits.Length - storage.Scale;
            result = digits.Substring(0, point) + "." + digits.Substring(point);
        }

        return negative && storage.Coefficient != BigInteger.Zero ? "-" + result : result;
    }

    internal XdmDecimalStorage NormalizeXdmDecimalStorage(XdmDecimalStorage storage)
    {
        if (storage.Scale < 0)
        {
            throw new ArgumentOutOfRangeException("storage");
        }

        if (storage.Coefficient == BigInteger.Zero)
        {
            storage.Scale = 0;
            return storage;
        }

        while (storage.Scale > 0 && storage.Coefficient % 10 == BigInteger.Zero)
        {
            storage.Coefficient = storage.Coefficient / 10;
            storage.Scale = storage.Scale - 1;
        }

        return storage;
    }

    internal BigInteger ReadXdmIntegerValue(XdmAtomicValue value)
    {
        RequireXdmIntegerValue(value);
        if (value.ExtendedValue != null)
        {
            XdmIntegerStorage storage = (XdmIntegerStorage)value.ExtendedValue;
            return storage.Value;
        }

        if (value.CanonicalValue != null)
        {
            return BigInteger.Parse(value.CanonicalValue, CultureInfo.InvariantCulture);
        }

        return new BigInteger(value.IntegerValue);
    }

    internal XdmDecimalStorage ReadXdmDecimalStorage(XdmAtomicValue value)
    {
        RequireXdmSupportedNumeric(value);
        if (IsXdmFloatingValue(value))
        {
            throw new ArgumentException("A floating-point value does not use exact decimal storage.", "value");
        }

        if (TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            return new XdmDecimalStorage(ReadXdmIntegerValue(value), 0);
        }

        if (value.ExtendedValue != null)
        {
            XdmDecimalStorage storage = (XdmDecimalStorage)value.ExtendedValue;
            return new XdmDecimalStorage(storage.Coefficient, storage.Scale);
        }

        string? representation = value.CanonicalValue == null ? value.StringValue : value.CanonicalValue;
        if (representation != null)
        {
            XdmAtomicValue parsed = ParseXdmDecimal(representation);
            object? parsedStorage = parsed.ExtendedValue;
            if (parsedStorage == null)
            {
                throw new InvalidOperationException("Parsed decimal storage is missing.");
            }

            return (XdmDecimalStorage)parsedStorage;
        }

        XdmAtomicValue fallback = ParseXdmDecimal(value.DecimalValue.ToString(CultureInfo.InvariantCulture));
        object? fallbackStorage = fallback.ExtendedValue;
        if (fallbackStorage == null)
        {
            throw new InvalidOperationException("Parsed decimal storage is missing.");
        }

        return (XdmDecimalStorage)fallbackStorage;
    }

    internal BigInteger ScaleXdmDecimalCoefficient(XdmDecimalStorage value, int scale)
    {
        int difference = scale - value.Scale;
        return difference == 0 ? value.Coefficient : value.Coefficient * PowerOfTen(difference);
    }

    internal BigInteger DivideXdmHalfToEven(BigInteger numerator, BigInteger denominator)
    {
        if (denominator < BigInteger.Zero)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger quotient = numerator / denominator;
        BigInteger remainder = numerator % denominator;
        BigInteger comparison = BigInteger.Abs(remainder) * 2;
        if (comparison > denominator || comparison == denominator && !quotient.IsEven)
        {
            quotient = quotient + (numerator < BigInteger.Zero ? -BigInteger.One : BigInteger.One);
        }

        return quotient;
    }

    internal BigInteger AdjustXdmRoundedQuotient(BigInteger quotient, BigInteger remainder, BigInteger divisor, XdmRoundingMode mode)
    {
        if (remainder == BigInteger.Zero)
        {
            return quotient;
        }

        bool negative = remainder < BigInteger.Zero;
        if (mode == XdmRoundingMode.Floor)
        {
            return negative ? quotient - BigInteger.One : quotient;
        }

        if (mode == XdmRoundingMode.Ceiling)
        {
            return negative ? quotient : quotient + BigInteger.One;
        }

        BigInteger comparison = BigInteger.Abs(remainder) * 2;
        bool increment = comparison > divisor;
        if (comparison == divisor)
        {
            increment = mode == XdmRoundingMode.HalfToEven ? !quotient.IsEven : !negative;
        }

        if (!increment)
        {
            return quotient;
        }

        return quotient + (negative ? -BigInteger.One : BigInteger.One);
    }

    internal BigInteger PowerOfTen(int exponent)
    {
        if (exponent < 0 || exponent > MaximumNumericDigits + DivisionScale)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0002, "The numeric scale exceeds the supported range.");
        }

        return BigInteger.Pow(new BigInteger(10), exponent);
    }

    internal void CheckXdmNumericMagnitude(BigInteger coefficient, int scale)
    {
        if (scale < 0 || scale > MaximumNumericDigits)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0002, "The decimal scale exceeds the supported range.");
        }

        string digits = BigInteger.Abs(coefficient).ToString(CultureInfo.InvariantCulture);
        if (digits.Length > MaximumNumericDigits)
        {
            throw CreateXdmNumericError(XPathErrorCodes.FOAR0002, "The numeric result exceeds the supported precision.");
        }
    }

    internal bool IsWithinXdmIntegerBounds(BigInteger value, XdmAtomicType type)
    {
        if (type == XdmAtomicType.Integer)
        {
            return true;
        }

        if (type == XdmAtomicType.NonPositiveInteger) return value <= BigInteger.Zero;
        if (type == XdmAtomicType.NegativeInteger) return value < BigInteger.Zero;
        if (type == XdmAtomicType.Long) return value >= new BigInteger(long.MinValue) && value <= new BigInteger(long.MaxValue);
        if (type == XdmAtomicType.Int) return value >= new BigInteger(int.MinValue) && value <= new BigInteger(int.MaxValue);
        if (type == XdmAtomicType.Short) return value >= new BigInteger(short.MinValue) && value <= new BigInteger(short.MaxValue);
        if (type == XdmAtomicType.Byte) return value >= new BigInteger(sbyte.MinValue) && value <= new BigInteger(sbyte.MaxValue);
        if (type == XdmAtomicType.NonNegativeInteger) return value >= BigInteger.Zero;
        if (type == XdmAtomicType.UnsignedLong) return value >= BigInteger.Zero && value <= UnsignedLongMaximum;
        if (type == XdmAtomicType.UnsignedInt) return value >= BigInteger.Zero && value <= new BigInteger(uint.MaxValue);
        if (type == XdmAtomicType.UnsignedShort) return value >= BigInteger.Zero && value <= new BigInteger(ushort.MaxValue);
        if (type == XdmAtomicType.UnsignedByte) return value >= BigInteger.Zero && value <= new BigInteger(byte.MaxValue);
        return value > BigInteger.Zero;
    }

    internal string GetXdmNumericTypeDisplayName(XdmAtomicType type)
    {
        string? qualifiedName = TypeSystem.GetXdmAtomicTypeDefinition(type).Name.QualifiedName;
        return qualifiedName == null ? type.ToString() : qualifiedName;
    }

    internal void RequireXdmSupportedNumeric(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (value.Type != XdmAtomicType.Decimal && value.Type != XdmAtomicType.Float && value.Type != XdmAtomicType.Double && !TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            throw new ArgumentException("The value is not numeric.", "value");
        }
    }

    internal bool IsXdmFloatingValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        return value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double;
    }

    internal void RequireXdmIntegerValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (!TypeSystem.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            throw new ArgumentException("The value is not derived from xs:integer.", "value");
        }
    }

    internal void RequireXdmBooleanValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (value.Type != XdmAtomicType.Boolean)
        {
            throw new ArgumentException("The value is not an xs:boolean.", "value");
        }
    }

    internal void RequireXdmIntegerDatatype(XdmAtomicType type)
    {
        if (!TypeSystem.IsXdmAtomicSubtypeOf(type, XdmAtomicType.Integer))
        {
            throw new ArgumentException("The requested type is not derived from xs:integer.", "targetType");
        }
    }

    internal XdmAtomicValue RequireXdmNumericParseResult(XdmNumericParseResult result)
    {
        if (result.Success && result.Value != null)
        {
            return result.Value;
        }

        string code = result.ErrorCode == null ? XPathErrorCodes.FORG0001 : result.ErrorCode;
        string message = result.ErrorMessage == null ? "The numeric lexical value is invalid." : result.ErrorMessage;
        throw CreateXdmNumericError(code, message);
    }

    internal XdmNumericParseResult InvalidXdmNumericLexical(string typeName)
    {
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for " + typeName + ".");
    }

    internal XdmNumericParseResult OverflowXdmNumericLexical(string typeName)
    {
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FOCA0002, "The lexical value exceeds the supported range of " + typeName + ".");
    }

    internal XPathDynamicException CreateXdmNumericError(string code, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
