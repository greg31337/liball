using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XdmDurationDatatype
{
    internal const int MaximumDurationDigits = 10000;
    internal XdmTypeSystem TypeSystem;

    internal XdmDurationDatatype()
    {
        TypeSystem = new XdmTypeSystem();
    }

    internal XdmAtomicValue ParseXdmDuration(string lexicalValue, XdmAtomicType targetType)
    {
        XdmNumericParseResult result = TryParseXdmDuration(lexicalValue, targetType);
        return new XdmNumericDatatype().RequireXdmNumericParseResult(result);
    }

    internal XdmNumericParseResult TryParseXdmDuration(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmDurationType(targetType);
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        XdmDurationParserState state = new XdmDurationParserState(normalized);
        if (state.Index < normalized.Length && normalized[state.Index] == '-')
        {
            state.Negative = true;
            state.Index = state.Index + 1;
        }

        if (state.Index >= normalized.Length || normalized[state.Index] != 'P')
        {
            return InvalidXdmDurationLexical(targetType);
        }

        state.Index = state.Index + 1;
        while (state.Index < normalized.Length)
        {
            if (normalized[state.Index] == 'T')
            {
                if (state.InTime)
                {
                    return InvalidXdmDurationLexical(targetType);
                }

                state.InTime = true;
                state.Index = state.Index + 1;
                continue;
            }

            if (!ReadXdmDurationComponent(state))
            {
                return InvalidXdmDurationLexical(targetType);
            }
        }

        if (!state.HasComponent || state.InTime && !state.HasTimeComponent || !IsXdmDurationLexicalAllowedForType(state, targetType))
        {
            return InvalidXdmDurationLexical(targetType);
        }

        if (state.DigitCount > MaximumDurationDigits)
        {
            return OverflowXdmDurationLexical(targetType);
        }

        BigInteger years = ParseXdmDurationIntegerComponent(state.Years);
        BigInteger dateMonths = ParseXdmDurationIntegerComponent(state.DateMonths);
        BigInteger days = ParseXdmDurationIntegerComponent(state.Days);
        BigInteger hours = ParseXdmDurationIntegerComponent(state.Hours);
        BigInteger minutes = ParseXdmDurationIntegerComponent(state.TimeMinutes);
        XdmDecimalStorage seconds = ParseXdmDurationSecondComponent(state.Seconds);
        BigInteger months = years * 12 + dateMonths;
        BigInteger wholeSeconds = days * 86400 + hours * 3600 + minutes * 60;
        BigInteger scaleFactor = new XdmNumericDatatype().PowerOfTen(seconds.Scale);
        BigInteger secondsCoefficient = wholeSeconds * scaleFactor + seconds.Coefficient;
        if (state.Negative)
        {
            months = -months;
            secondsCoefficient = -secondsCoefficient;
        }

        XdmDurationStorage storage = NormalizeXdmDurationStorage(new XdmDurationStorage(months, secondsCoefficient, seconds.Scale));
        if (!IsXdmDurationMagnitudeSupported(storage))
        {
            return OverflowXdmDurationLexical(targetType);
        }

        return new XdmNumericParseResult(true, CreateXdmDurationValue(storage, targetType, lexicalValue), null, null);
    }

    internal bool ReadXdmDurationComponent(XdmDurationParserState state)
    {
        int start = state.Index;
        while (state.Index < state.Source.Length && state.Source[state.Index] >= '0' && state.Source[state.Index] <= '9')
        {
            state.DigitCount = state.DigitCount + 1;
            state.Index = state.Index + 1;
        }

        if (state.Index == start)
        {
            return false;
        }

        bool fractional = false;
        if (state.Index < state.Source.Length && state.Source[state.Index] == '.')
        {
            fractional = true;
            state.Index = state.Index + 1;
            int fractionStart = state.Index;
            while (state.Index < state.Source.Length && state.Source[state.Index] >= '0' && state.Source[state.Index] <= '9')
            {
                state.DigitCount = state.DigitCount + 1;
                state.Index = state.Index + 1;
            }

            if (state.Index == fractionStart)
            {
                return false;
            }
        }

        if (state.Index >= state.Source.Length)
        {
            return false;
        }

        string number = state.Source.Substring(start, state.Index - start);
        char designator = state.Source[state.Index];
        state.Index = state.Index + 1;
        if (!state.InTime)
        {
            if (fractional)
            {
                return false;
            }

            if (designator == 'Y' && state.LastDateOrder < 1)
            {
                state.HasYear = true;
                state.Years = number;
                state.LastDateOrder = 1;
            }
            else if (designator == 'M' && state.LastDateOrder < 2)
            {
                state.HasDateMonth = true;
                state.DateMonths = number;
                state.LastDateOrder = 2;
            }
            else if (designator == 'D' && state.LastDateOrder < 3)
            {
                state.HasDay = true;
                state.Days = number;
                state.LastDateOrder = 3;
            }
            else
            {
                return false;
            }
        }
        else if (designator == 'H' && !fractional && state.LastTimeOrder < 1)
        {
            state.HasHour = true;
            state.Hours = number;
            state.LastTimeOrder = 1;
        }
        else if (designator == 'M' && !fractional && state.LastTimeOrder < 2)
        {
            state.HasTimeMinute = true;
            state.TimeMinutes = number;
            state.LastTimeOrder = 2;
        }
        else if (designator == 'S' && state.LastTimeOrder < 3)
        {
            state.HasSecond = true;
            state.Seconds = number;
            state.LastTimeOrder = 3;
        }
        else
        {
            return false;
        }

        state.HasComponent = true;
        if (state.InTime)
        {
            state.HasTimeComponent = true;
        }

        return true;
    }

    internal bool IsXdmDurationLexicalAllowedForType(XdmDurationParserState state, XdmAtomicType type)
    {
        if (type == XdmAtomicType.YearMonthDuration)
        {
            return !state.HasDay && !state.InTime && (state.HasYear || state.HasDateMonth);
        }

        if (type == XdmAtomicType.DayTimeDuration)
        {
            return !state.HasYear && !state.HasDateMonth && (state.HasDay || state.HasTimeComponent);
        }

        return true;
    }

    internal BigInteger ParseXdmDurationIntegerComponent(string? component)
    {
        return component == null ? BigInteger.Zero : BigInteger.Parse(component, CultureInfo.InvariantCulture);
    }

    internal XdmDecimalStorage ParseXdmDurationSecondComponent(string? component)
    {
        if (component == null)
        {
            return new XdmDecimalStorage(BigInteger.Zero, 0);
        }

        int point = component.IndexOf('.');
        if (point < 0)
        {
            return new XdmDecimalStorage(BigInteger.Parse(component, CultureInfo.InvariantCulture), 0);
        }

        string digits = component.Substring(0, point) + component.Substring(point + 1);
        int scale = component.Length - point - 1;
        return new XdmNumericDatatype().NormalizeXdmDecimalStorage(new XdmDecimalStorage(BigInteger.Parse(digits, CultureInfo.InvariantCulture), scale));
    }

    internal XdmAtomicValue CreateXdmDurationValue(XdmDurationStorage source, XdmAtomicType type, string? lexicalValue)
    {
        RequireXdmDurationType(type);
        XdmDurationStorage storage = NormalizeXdmDurationStorage(new XdmDurationStorage(source.Months, source.SecondsCoefficient, source.SecondsScale));
        if (type == XdmAtomicType.YearMonthDuration && storage.SecondsCoefficient != BigInteger.Zero)
        {
            throw new ArgumentException("An xs:yearMonthDuration cannot contain seconds.", "source");
        }

        if (type == XdmAtomicType.DayTimeDuration && storage.Months != BigInteger.Zero)
        {
            throw new ArgumentException("An xs:dayTimeDuration cannot contain months.", "source");
        }

        if (storage.Months < BigInteger.Zero && storage.SecondsCoefficient > BigInteger.Zero || storage.Months > BigInteger.Zero && storage.SecondsCoefficient < BigInteger.Zero)
        {
            throw new ArgumentException("Duration month and second components must have the same sign.", "source");
        }

        CheckXdmDurationMagnitude(storage);
        string canonical = CanonicalizeXdmDuration(storage, type);
        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = type;
        value.LexicalValue = lexicalValue == null ? canonical : lexicalValue;
        value.CanonicalValue = canonical;
        value.StringValue = canonical;
        value.ExtendedValue = storage;
        value.IsImmutable = true;
        return value;
    }

    internal XdmDurationStorage NormalizeXdmDurationStorage(XdmDurationStorage storage)
    {
        XdmDecimalStorage seconds = new XdmNumericDatatype().NormalizeXdmDecimalStorage(new XdmDecimalStorage(storage.SecondsCoefficient, storage.SecondsScale));
        storage.SecondsCoefficient = seconds.Coefficient;
        storage.SecondsScale = seconds.Scale;
        return storage;
    }

    internal string CanonicalizeXdmDuration(XdmDurationStorage source, XdmAtomicType type)
    {
        XdmDurationStorage storage = NormalizeXdmDurationStorage(new XdmDurationStorage(source.Months, source.SecondsCoefficient, source.SecondsScale));
        bool negative = storage.Months < BigInteger.Zero || storage.SecondsCoefficient < BigInteger.Zero;
        BigInteger months = BigInteger.Abs(storage.Months);
        BigInteger secondsCoefficient = BigInteger.Abs(storage.SecondsCoefficient);
        BigInteger years = months / 12;
        BigInteger monthComponent = months % 12;
        BigInteger scaleFactor = new XdmNumericDatatype().PowerOfTen(storage.SecondsScale);
        BigInteger wholeSeconds = secondsCoefficient / scaleFactor;
        BigInteger fraction = secondsCoefficient % scaleFactor;
        BigInteger days = wholeSeconds / 86400;
        BigInteger dayRemainder = wholeSeconds % 86400;
        BigInteger hours = dayRemainder / 3600;
        BigInteger hourRemainder = dayRemainder % 3600;
        BigInteger minutes = hourRemainder / 60;
        BigInteger secondComponent = hourRemainder % 60;
        StringBuilder result = new StringBuilder();
        if (negative)
        {
            result.Append('-');
        }

        result.Append('P');
        if (years != BigInteger.Zero)
        {
            result.Append(years.ToString(CultureInfo.InvariantCulture));
            result.Append('Y');
        }

        if (monthComponent != BigInteger.Zero)
        {
            result.Append(monthComponent.ToString(CultureInfo.InvariantCulture));
            result.Append('M');
        }

        if (days != BigInteger.Zero)
        {
            result.Append(days.ToString(CultureInfo.InvariantCulture));
            result.Append('D');
        }

        bool hasTime = hours != BigInteger.Zero || minutes != BigInteger.Zero || secondComponent != BigInteger.Zero || fraction != BigInteger.Zero;
        if (hasTime)
        {
            result.Append('T');
            if (hours != BigInteger.Zero)
            {
                result.Append(hours.ToString(CultureInfo.InvariantCulture));
                result.Append('H');
            }

            if (minutes != BigInteger.Zero)
            {
                result.Append(minutes.ToString(CultureInfo.InvariantCulture));
                result.Append('M');
            }

            if (secondComponent != BigInteger.Zero || fraction != BigInteger.Zero)
            {
                BigInteger componentCoefficient = secondComponent * scaleFactor + fraction;
                string seconds = new XdmNumericDatatype().CanonicalizeXdmDecimal(new XdmDecimalStorage(componentCoefficient, storage.SecondsScale));
                result.Append(seconds);
                result.Append('S');
            }
        }

        if (months == BigInteger.Zero && secondsCoefficient == BigInteger.Zero)
        {
            return type == XdmAtomicType.YearMonthDuration ? "P0M" : "PT0S";
        }

        return result.ToString();
    }

    internal XdmDurationStorage ReadXdmDurationStorage(XdmAtomicValue value)
    {
        RequireXdmDurationValue(value);
        object? extended = value.ExtendedValue;
        if (extended != null)
        {
            XdmDurationStorage storage = (XdmDurationStorage)extended;
            return new XdmDurationStorage(storage.Months, storage.SecondsCoefficient, storage.SecondsScale);
        }

        string? representation = value.CanonicalValue == null ? value.StringValue : value.CanonicalValue;
        if (representation == null)
        {
            representation = value.LexicalValue;
        }

        if (representation == null)
        {
            throw new ArgumentException("The duration value has no representation.", "value");
        }

        XdmAtomicValue parsed = ParseXdmDuration(representation, value.Type);
        object? parsedStorage = parsed.ExtendedValue;
        if (parsedStorage == null)
        {
            throw new InvalidOperationException("Parsed duration storage is missing.");
        }

        return (XdmDurationStorage)parsedStorage;
    }

    internal bool EqualXdmDuration(XdmAtomicValue left, XdmAtomicValue right)
    {
        XdmDurationStorage leftStorage = ReadXdmDurationStorage(left);
        XdmDurationStorage rightStorage = ReadXdmDurationStorage(right);
        return leftStorage.Months == rightStorage.Months && CompareXdmDurationSeconds(leftStorage, rightStorage) == 0;
    }

    internal int CompareXdmDuration(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmDurationValue(left);
        RequireXdmDurationValue(right);
        if (left.Type == XdmAtomicType.YearMonthDuration && right.Type == XdmAtomicType.YearMonthDuration)
        {
            return ReadXdmDurationStorage(left).Months.CompareTo(ReadXdmDurationStorage(right).Months);
        }

        if (left.Type == XdmAtomicType.DayTimeDuration && right.Type == XdmAtomicType.DayTimeDuration)
        {
            return CompareXdmDurationSeconds(ReadXdmDurationStorage(left), ReadXdmDurationStorage(right));
        }

        throw new ArgumentException("Ordering is defined only between durations of the same ordered subtype.", "right");
    }

    internal int CompareXdmDurationSeconds(XdmDurationStorage left, XdmDurationStorage right)
    {
        int scale = left.SecondsScale > right.SecondsScale ? left.SecondsScale : right.SecondsScale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger leftCoefficient = numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(left.SecondsCoefficient, left.SecondsScale), scale);
        BigInteger rightCoefficient = numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(right.SecondsCoefficient, right.SecondsScale), scale);
        return leftCoefficient.CompareTo(rightCoefficient);
    }

    internal XdmAtomicValue YearsFromXdmDuration(XdmAtomicValue value)
    {
        BigInteger months = ReadXdmDurationStorage(value).Months;
        return new XdmNumericDatatype().CreateXdmIntegerValue(months / 12, XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue MonthsFromXdmDuration(XdmAtomicValue value)
    {
        BigInteger months = ReadXdmDurationStorage(value).Months;
        return new XdmNumericDatatype().CreateXdmIntegerValue(months % 12, XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue DaysFromXdmDuration(XdmAtomicValue value)
    {
        return CreateXdmDurationIntegerComponent(value, 86400, 0);
    }

    internal XdmAtomicValue HoursFromXdmDuration(XdmAtomicValue value)
    {
        return CreateXdmDurationIntegerComponent(value, 3600, 24);
    }

    internal XdmAtomicValue MinutesFromXdmDuration(XdmAtomicValue value)
    {
        return CreateXdmDurationIntegerComponent(value, 60, 60);
    }

    internal XdmAtomicValue SecondsFromXdmDuration(XdmAtomicValue value)
    {
        XdmDurationStorage storage = ReadXdmDurationStorage(value);
        BigInteger factor = new XdmNumericDatatype().PowerOfTen(storage.SecondsScale);
        BigInteger modulus = factor * 60;
        BigInteger component = storage.SecondsCoefficient % modulus;
        return new XdmNumericDatatype().CreateXdmDecimalValue(new XdmDecimalStorage(component, storage.SecondsScale), null);
    }

    internal XdmAtomicValue CreateXdmDurationIntegerComponent(XdmAtomicValue value, int unitSeconds, int modulus)
    {
        XdmDurationStorage storage = ReadXdmDurationStorage(value);
        BigInteger factor = new XdmNumericDatatype().PowerOfTen(storage.SecondsScale);
        BigInteger wholeSeconds = storage.SecondsCoefficient / factor;
        BigInteger component = wholeSeconds / unitSeconds;
        if (modulus > 0)
        {
            component = component % modulus;
        }

        return new XdmNumericDatatype().CreateXdmIntegerValue(component, XdmAtomicType.Integer, null);
    }

    internal XdmAtomicValue AddXdmDurations(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireSameXdmDurationArithmeticType(left, right);
        XdmDurationStorage leftStorage = ReadXdmDurationStorage(left);
        XdmDurationStorage rightStorage = ReadXdmDurationStorage(right);
        XdmDurationStorage result = AddXdmDurationStorage(leftStorage, rightStorage, false);
        return CreateXdmDurationValue(result, left.Type, null);
    }

    internal XdmAtomicValue SubtractXdmDurations(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireSameXdmDurationArithmeticType(left, right);
        XdmDurationStorage leftStorage = ReadXdmDurationStorage(left);
        XdmDurationStorage rightStorage = ReadXdmDurationStorage(right);
        XdmDurationStorage result = AddXdmDurationStorage(leftStorage, rightStorage, true);
        return CreateXdmDurationValue(result, left.Type, null);
    }

    internal XdmDurationStorage AddXdmDurationStorage(XdmDurationStorage left, XdmDurationStorage right, bool subtract)
    {
        int scale = left.SecondsScale > right.SecondsScale ? left.SecondsScale : right.SecondsScale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger leftSeconds = numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(left.SecondsCoefficient, left.SecondsScale), scale);
        BigInteger rightSeconds = numeric.ScaleXdmDecimalCoefficient(new XdmDecimalStorage(right.SecondsCoefficient, right.SecondsScale), scale);
        BigInteger months = subtract ? left.Months - right.Months : left.Months + right.Months;
        BigInteger seconds = subtract ? leftSeconds - rightSeconds : leftSeconds + rightSeconds;
        return NormalizeXdmDurationStorage(new XdmDurationStorage(months, seconds, scale));
    }

    internal XdmAtomicValue NegateXdmDuration(XdmAtomicValue value)
    {
        XdmDurationStorage storage = ReadXdmDurationStorage(value);
        return CreateXdmDurationValue(new XdmDurationStorage(-storage.Months, -storage.SecondsCoefficient, storage.SecondsScale), value.Type, null);
    }

    internal XdmAtomicValue MultiplyXdmDuration(XdmAtomicValue duration, XdmAtomicValue factor)
    {
        RequireXdmDurationArithmeticValue(duration);
        double number = ReadXdmDurationNumericFactor(factor);
        if (double.IsNaN(number))
        {
            throw CreateXdmDurationError(XPathErrorCodes.FOCA0005, "A duration cannot be multiplied by NaN.");
        }

        if (double.IsInfinity(number))
        {
            throw CreateXdmDurationError(XPathErrorCodes.FODT0002, "The duration multiplication overflowed.");
        }

        if (number == 0d)
        {
            return CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, BigInteger.Zero, 0), duration.Type, null);
        }

        XdmDurationStorage storage = ReadXdmDurationStorage(duration);
        XdmDecimalStorage numericFactor = ConvertXdmDurationFactorToDecimal(number);
        if (duration.Type == XdmAtomicType.YearMonthDuration)
        {
            XdmDecimalStorage product = NormalizeXdmDurationDecimal(new XdmDecimalStorage(storage.Months * numericFactor.Coefficient, numericFactor.Scale));
            BigInteger months = RoundXdmDurationDecimalToInteger(product);
            return CreateXdmDurationValue(new XdmDurationStorage(months, BigInteger.Zero, 0), duration.Type, null);
        }

        XdmDecimalStorage seconds = NormalizeXdmDurationDecimal(new XdmDecimalStorage(storage.SecondsCoefficient * numericFactor.Coefficient, storage.SecondsScale + numericFactor.Scale));
        return CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, seconds.Coefficient, seconds.Scale), duration.Type, null);
    }

    internal XdmAtomicValue DivideXdmDuration(XdmAtomicValue duration, XdmAtomicValue divisor)
    {
        RequireXdmDurationArithmeticValue(duration);
        double number = ReadXdmDurationNumericFactor(divisor);
        if (double.IsNaN(number))
        {
            throw CreateXdmDurationError(XPathErrorCodes.FOCA0005, "A duration cannot be divided by NaN.");
        }

        if (double.IsInfinity(number))
        {
            return CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, BigInteger.Zero, 0), duration.Type, null);
        }

        if (number == 0d)
        {
            throw CreateXdmDurationError(XPathErrorCodes.FODT0002, "Duration division by zero overflowed.");
        }

        XdmDurationStorage storage = ReadXdmDurationStorage(duration);
        XdmDecimalStorage numericDivisor = ConvertXdmDurationFactorToDecimal(number);
        if (duration.Type == XdmAtomicType.YearMonthDuration)
        {
            XdmDecimalStorage quotient = DivideXdmDurationDecimal(new XdmDecimalStorage(storage.Months, 0), numericDivisor, true);
            BigInteger months = RoundXdmDurationDecimalToInteger(quotient);
            return CreateXdmDurationValue(new XdmDurationStorage(months, BigInteger.Zero, 0), duration.Type, null);
        }

        XdmDecimalStorage seconds = DivideXdmDurationDecimal(new XdmDecimalStorage(storage.SecondsCoefficient, storage.SecondsScale), numericDivisor, true);
        return CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, seconds.Coefficient, seconds.Scale), duration.Type, null);
    }

    internal XdmAtomicValue DivideXdmDurationByDuration(XdmAtomicValue dividend, XdmAtomicValue divisor)
    {
        RequireSameXdmDurationArithmeticType(dividend, divisor);
        XdmDurationStorage left = ReadXdmDurationStorage(dividend);
        XdmDurationStorage right = ReadXdmDurationStorage(divisor);
        XdmDecimalStorage leftNumber = dividend.Type == XdmAtomicType.YearMonthDuration ? new XdmDecimalStorage(left.Months, 0) : new XdmDecimalStorage(left.SecondsCoefficient, left.SecondsScale);
        XdmDecimalStorage rightNumber = divisor.Type == XdmAtomicType.YearMonthDuration ? new XdmDecimalStorage(right.Months, 0) : new XdmDecimalStorage(right.SecondsCoefficient, right.SecondsScale);
        XdmDecimalStorage ratio = DivideXdmDurationDecimal(leftNumber, rightNumber, false);
        return new XdmNumericDatatype().CreateXdmDecimalValue(ratio, null);
    }

    internal XdmAtomicValue RoundXdmDayTimeDuration(XdmAtomicValue value, int precision)
    {
        RequireXdmDurationValue(value);
        if (value.Type != XdmAtomicType.DayTimeDuration)
        {
            throw new ArgumentException("Only xs:dayTimeDuration has a roundable second length.", "value");
        }

        XdmDurationStorage storage = ReadXdmDurationStorage(value);
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmAtomicValue seconds = numeric.CreateXdmDecimalValue(new XdmDecimalStorage(storage.SecondsCoefficient, storage.SecondsScale), null);
        XdmAtomicValue rounded = numeric.RoundHalfToEvenXdmNumeric(seconds, precision);
        XdmDecimalStorage roundedStorage = numeric.ReadXdmDecimalStorage(rounded);
        return CreateXdmDurationValue(new XdmDurationStorage(BigInteger.Zero, roundedStorage.Coefficient, roundedStorage.Scale), value.Type, null);
    }

    internal double ReadXdmDurationNumericFactor(XdmAtomicValue value)
    {
        return new XdmFloatingDatatype().ReadXdmFloatingValue(value, XdmAtomicType.Double);
    }

    internal XdmDecimalStorage ConvertXdmDurationFactorToDecimal(double number)
    {
        string canonical = new XdmFloatingDatatype().CanonicalizeXdmFloating(number, XdmAtomicType.Double);
        bool negative = canonical[0] == '-';
        int start = negative ? 1 : 0;
        int exponentIndex = canonical.IndexOf('E');
        string mantissa = canonical.Substring(start, exponentIndex - start);
        int point = mantissa.IndexOf('.');
        string digits = mantissa.Substring(0, point) + mantissa.Substring(point + 1);
        int exponent = int.Parse(canonical.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);
        int scale = mantissa.Length - point - 1 - exponent;
        BigInteger coefficient = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        if (scale < 0)
        {
            coefficient = coefficient * new XdmNumericDatatype().PowerOfTen(-scale);
            scale = 0;
        }

        if (negative)
        {
            coefficient = -coefficient;
        }

        return NormalizeXdmDurationDecimal(new XdmDecimalStorage(coefficient, scale));
    }

    internal XdmDecimalStorage DivideXdmDurationDecimal(XdmDecimalStorage dividend, XdmDecimalStorage divisor, bool durationResult)
    {
        if (divisor.Coefficient == BigInteger.Zero)
        {
            string code = durationResult ? XPathErrorCodes.FODT0002 : XPathErrorCodes.FOAR0001;
            throw CreateXdmDurationError(code, "Division by a zero duration value is not defined.");
        }

        int exponent = divisor.Scale - dividend.Scale + XdmNumericDatatype.DivisionScale;
        BigInteger numerator = dividend.Coefficient;
        BigInteger denominator = divisor.Coefficient;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        if (exponent >= 0)
        {
            numerator = numerator * numeric.PowerOfTen(exponent);
        }
        else
        {
            denominator = denominator * numeric.PowerOfTen(-exponent);
        }

        BigInteger coefficient = numeric.DivideXdmHalfToEven(numerator, denominator);
        XdmDecimalStorage result = NormalizeXdmDurationDecimal(new XdmDecimalStorage(coefficient, XdmNumericDatatype.DivisionScale));
        if (!IsXdmDurationDecimalMagnitudeSupported(result))
        {
            throw CreateXdmDurationError(XPathErrorCodes.FODT0002, "The duration division result overflowed.");
        }

        return result;
    }

    internal BigInteger RoundXdmDurationDecimalToInteger(XdmDecimalStorage value)
    {
        if (value.Scale == 0)
        {
            return value.Coefficient;
        }

        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger divisor = numeric.PowerOfTen(value.Scale);
        BigInteger quotient = value.Coefficient / divisor;
        BigInteger remainder = value.Coefficient % divisor;
        return numeric.AdjustXdmRoundedQuotient(quotient, remainder, divisor, XdmRoundingMode.HalfTowardPositiveInfinity);
    }

    internal XdmDecimalStorage NormalizeXdmDurationDecimal(XdmDecimalStorage value)
    {
        return new XdmNumericDatatype().NormalizeXdmDecimalStorage(value);
    }

    internal bool IsXdmDurationMagnitudeSupported(XdmDurationStorage storage)
    {
        return BigInteger.Abs(storage.Months).ToString(CultureInfo.InvariantCulture).Length <= MaximumDurationDigits && IsXdmDurationDecimalMagnitudeSupported(new XdmDecimalStorage(storage.SecondsCoefficient, storage.SecondsScale));
    }

    internal bool IsXdmDurationDecimalMagnitudeSupported(XdmDecimalStorage value)
    {
        return value.Scale >= 0 && value.Scale <= MaximumDurationDigits && BigInteger.Abs(value.Coefficient).ToString(CultureInfo.InvariantCulture).Length <= MaximumDurationDigits;
    }

    internal void CheckXdmDurationMagnitude(XdmDurationStorage storage)
    {
        if (!IsXdmDurationMagnitudeSupported(storage))
        {
            throw CreateXdmDurationError(XPathErrorCodes.FODT0002, "The duration exceeds the supported precision.");
        }
    }

    internal XdmNumericParseResult InvalidXdmDurationLexical(XdmAtomicType type)
    {
        string typeName = type == XdmAtomicType.Duration ? "xs:duration" : type == XdmAtomicType.YearMonthDuration ? "xs:yearMonthDuration" : "xs:dayTimeDuration";
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FORG0001, "The lexical value is not valid for " + typeName + ".");
    }

    internal XdmNumericParseResult OverflowXdmDurationLexical(XdmAtomicType type)
    {
        string typeName = type == XdmAtomicType.Duration ? "xs:duration" : type == XdmAtomicType.YearMonthDuration ? "xs:yearMonthDuration" : "xs:dayTimeDuration";
        return new XdmNumericParseResult(false, null, XPathErrorCodes.FOCA0002, "The lexical value exceeds the supported range of " + typeName + ".");
    }

    internal XPathDynamicException CreateXdmDurationError(string code, string message)
    {
        return new XdmNumericDatatype().CreateXdmNumericError(code, message);
    }

    internal void RequireXdmDurationType(XdmAtomicType type)
    {
        if (!TypeSystem.IsXdmDurationDerivedAtomicType(type))
        {
            throw new ArgumentException("The requested type is not a duration type.", "type");
        }
    }

    internal void RequireXdmDurationValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        RequireXdmDurationType(value.Type);
    }

    internal void RequireXdmDurationArithmeticValue(XdmAtomicValue value)
    {
        RequireXdmDurationValue(value);
        if (value.Type != XdmAtomicType.YearMonthDuration && value.Type != XdmAtomicType.DayTimeDuration)
        {
            throw new ArgumentException("Arithmetic is defined only for ordered duration subtypes.", "value");
        }
    }

    internal void RequireSameXdmDurationArithmeticType(XdmAtomicValue left, XdmAtomicValue right)
    {
        RequireXdmDurationArithmeticValue(left);
        RequireXdmDurationArithmeticValue(right);
        if (left.Type != right.Type)
        {
            throw new ArgumentException("Duration arithmetic requires operands of the same subtype.", "right");
        }
    }
}
