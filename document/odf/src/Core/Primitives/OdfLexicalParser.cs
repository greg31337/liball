namespace odf;

internal static class OdfLexicalParser
{
    internal static OdfTemporalValue ParseDate(string? lexicalValue)
    {
        OdfTemporalValue result = NewTemporalResult(lexicalValue);
        if (lexicalValue == null)
        {
            return result;
        }
        int coreLength = ParseTimeZone(result, lexicalValue);
        if (coreLength < 0)
        {
            return result;
        }
        string dateText = lexicalValue.Substring(0, coreLength);
        if (!ParseDateFields(result, dateText))
        {
            return result;
        }
        result.HasDate = true;
        result.IsValid = true;
        return result;
    }

    internal static OdfTemporalValue ParseTime(string? lexicalValue)
    {
        OdfTemporalValue result = NewTemporalResult(lexicalValue);
        if (lexicalValue == null)
        {
            return result;
        }
        int coreLength = ParseTimeZone(result, lexicalValue);
        if (coreLength < 0)
        {
            return result;
        }
        string timeText = lexicalValue.Substring(0, coreLength);
        if (!ParseTimeFields(result, timeText))
        {
            return result;
        }
        result.HasTime = true;
        result.IsValid = true;
        return result;
    }

    internal static OdfTemporalValue ParseDateTime(string? lexicalValue)
    {
        OdfTemporalValue result = NewTemporalResult(lexicalValue);
        if (lexicalValue == null)
        {
            return result;
        }
        int coreLength = ParseTimeZone(result, lexicalValue);
        if (coreLength < 0)
        {
            return result;
        }
        string core = lexicalValue.Substring(0, coreLength);
        int separator = core.IndexOf('T');
        if (separator <= 0 || separator != core.LastIndexOf('T') || separator == core.Length - 1)
        {
            return result;
        }
        if (!ParseDateFields(result, core.Substring(0, separator)) ||
            !ParseTimeFields(result, core.Substring(separator + 1)))
        {
            return result;
        }
        result.HasDate = true;
        result.HasTime = true;
        result.IsValid = true;
        return result;
    }

    internal static OdfDurationValue ParseDuration(string? lexicalValue)
    {
        OdfDurationValue result = NewDurationResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length < 3)
        {
            return result;
        }

        int index = 0;
        if (lexicalValue[index] == '-')
        {
            result.IsNegative = true;
            index = index + 1;
        }
        if (index >= lexicalValue.Length || lexicalValue[index] != 'P')
        {
            return result;
        }
        index = index + 1;

        bool inTime = false;
        bool hasPart = false;
        bool hasTimePart = false;
        int lastDateOrder = 0;
        int lastTimeOrder = 0;
        while (index < lexicalValue.Length)
        {
            if (lexicalValue[index] == 'T')
            {
                if (inTime || index == lexicalValue.Length - 1)
                {
                    return NewDurationResult(lexicalValue);
                }
                inTime = true;
                index = index + 1;
                continue;
            }

            int numberStart = index;
            while (index < lexicalValue.Length && lexicalValue[index] >= '0' && lexicalValue[index] <= '9')
            {
                index = index + 1;
            }
            if (index == numberStart)
            {
                return NewDurationResult(lexicalValue);
            }
            int wholeEnd = index;
            string fraction = "";
            if (index < lexicalValue.Length && lexicalValue[index] == '.')
            {
                index = index + 1;
                int fractionStart = index;
                while (index < lexicalValue.Length && lexicalValue[index] >= '0' && lexicalValue[index] <= '9')
                {
                    index = index + 1;
                }
                if (index == fractionStart)
                {
                    return NewDurationResult(lexicalValue);
                }
                fraction = lexicalValue.Substring(fractionStart, index - fractionStart);
            }
            if (index >= lexicalValue.Length)
            {
                return NewDurationResult(lexicalValue);
            }

            char designator = lexicalValue[index];
            global::System.Numerics.BigInteger number = ParseBigIntegerDigits(
                lexicalValue,
                numberStart,
                wholeEnd);
            if (!inTime)
            {
                if (fraction.Length != 0)
                {
                    return NewDurationResult(lexicalValue);
                }
                if (designator == 'Y' && lastDateOrder < 1)
                {
                    result.Years = number;
                    lastDateOrder = 1;
                }
                else if (designator == 'M' && lastDateOrder < 2)
                {
                    result.Months = number;
                    lastDateOrder = 2;
                }
                else if (designator == 'D' && lastDateOrder < 3)
                {
                    result.Days = number;
                    lastDateOrder = 3;
                }
                else
                {
                    return NewDurationResult(lexicalValue);
                }
            }
            else
            {
                if (designator == 'H' && lastTimeOrder < 1 && fraction.Length == 0)
                {
                    result.Hours = number;
                    lastTimeOrder = 1;
                }
                else if (designator == 'M' && lastTimeOrder < 2 && fraction.Length == 0)
                {
                    result.Minutes = number;
                    lastTimeOrder = 2;
                }
                else if (designator == 'S' && lastTimeOrder < 3)
                {
                    result.Seconds = number;
                    result.FractionalSecond = fraction;
                    lastTimeOrder = 3;
                }
                else
                {
                    return NewDurationResult(lexicalValue);
                }
                hasTimePart = true;
            }
            hasPart = true;
            index = index + 1;
        }

        if (!hasPart || (inTime && !hasTimePart))
        {
            return NewDurationResult(lexicalValue);
        }
        result.IsValid = true;
        return result;
    }

    internal static OdfUnitValue ParseLength(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, true, false, false, false);
    }

    internal static OdfUnitValue ParseNonNegativeLength(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, false, false, false, false);
    }

    internal static OdfUnitValue ParsePositiveLength(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, false, true, false, false);
    }

    internal static OdfUnitValue ParsePixelLength(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, false, false, true, false);
    }

    internal static OdfUnitValue ParseMeasurement(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, true, false, false, true);
    }

    internal static OdfUnitValue ParseCoordinate(string? lexicalValue)
    {
        return ParseLengthCore(lexicalValue, true, false, false, false);
    }

    internal static OdfUnitValue ParseAngle(string? lexicalValue)
    {
        OdfUnitValue result = NewUnitResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }

        OdfUnitKind unit = OdfUnitKind.Unitless;
        int unitLength = 0;
        if (lexicalValue.EndsWith("grad", global::System.StringComparison.Ordinal))
        {
            unit = OdfUnitKind.Grad;
            unitLength = 4;
        }
        else if (lexicalValue.EndsWith("deg", global::System.StringComparison.Ordinal))
        {
            unit = OdfUnitKind.Degree;
            unitLength = 3;
        }
        else if (lexicalValue.EndsWith("rad", global::System.StringComparison.Ordinal))
        {
            unit = OdfUnitKind.Radian;
            unitLength = 3;
        }

        string numericText = lexicalValue.Substring(0, lexicalValue.Length - unitLength);
        OdfLexicalValue numeric = ParseFloat(numericText);
        if (!numeric.IsValid || !double.IsFinite(numeric.FloatValue))
        {
            return result;
        }

        result.NumericValue = numeric.FloatValue;
        result.UnitKind = unit;
        result.IsValid = true;
        return result;
    }

    internal static OdfPointValue ParsePoint(string? lexicalValue)
    {
        OdfPointValue result = NewPointResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }

        int comma = lexicalValue.IndexOf(',');
        if (comma <= 0 || comma != lexicalValue.LastIndexOf(',') || comma == lexicalValue.Length - 1)
        {
            return result;
        }
        string xText = lexicalValue.Substring(0, comma);
        string yText = lexicalValue.Substring(comma + 1);
        if (xText[0] == '+' || yText[0] == '+')
        {
            return result;
        }
        OdfLexicalValue x = ParseInteger(xText);
        OdfLexicalValue y = ParseInteger(yText);
        if (!x.IsValid || !y.IsValid)
        {
            return result;
        }

        result.X = x.IntegerValue;
        result.Y = y.IntegerValue;
        result.IsValid = true;
        return result;
    }

    internal static OdfVectorValue ParseVector(string? lexicalValue)
    {
        OdfVectorValue result = NewVectorResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length < 5 || lexicalValue[0] != '(' || lexicalValue[lexicalValue.Length - 1] != ')')
        {
            return result;
        }

        string content = lexicalValue.Substring(1, lexicalValue.Length - 2);
        string[] values = content.Split([' ', '\t', '\r', '\n'], global::System.StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 3 || values[0][0] == '+' || values[1][0] == '+' || values[2][0] == '+')
        {
            return result;
        }
        OdfLexicalValue x = ParseDecimal(values[0]);
        OdfLexicalValue y = ParseDecimal(values[1]);
        OdfLexicalValue z = ParseDecimal(values[2]);
        if (!x.IsValid || !y.IsValid || !z.IsValid)
        {
            return result;
        }

        result.X = (double)x.DecimalValue;
        result.Y = (double)y.DecimalValue;
        result.Z = (double)z.DecimalValue;
        result.IsValid = true;
        return result;
    }

    internal static OdfViewBoxValue ParseViewBox(string? lexicalValue)
    {
        OdfViewBoxValue result = NewViewBoxResult(lexicalValue);
        if (lexicalValue == null)
        {
            return result;
        }

        string[] values = lexicalValue.Split([' ', '\t', '\r', '\n'], global::System.StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 4)
        {
            return result;
        }
        OdfLexicalValue left = ParseInteger(values[0]);
        OdfLexicalValue top = ParseInteger(values[1]);
        OdfLexicalValue width = ParseInteger(values[2]);
        OdfLexicalValue height = ParseInteger(values[3]);
        if (!left.IsValid || !top.IsValid || !width.IsValid || !height.IsValid)
        {
            return result;
        }

        result.Left = left.IntegerValue;
        result.Top = top.IntegerValue;
        result.Width = width.IntegerValue;
        result.Height = height.IntegerValue;
        result.IsValid = true;
        return result;
    }

    internal static OdfLexicalValue ParseBoolean(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == "true" || lexicalValue == "1")
        {
            result.BooleanValue = true;
            result.IsValid = true;
        }
        else if (lexicalValue == "false" || lexicalValue == "0")
        {
            result.BooleanValue = false;
            result.IsValid = true;
        }
        return result;
    }

    internal static OdfLexicalValue ParseInteger(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }

        bool negative = false;
        int index = 0;
        if (lexicalValue[index] == '+' || lexicalValue[index] == '-')
        {
            negative = lexicalValue[index] == '-';
            index = index + 1;
        }
        if (index == lexicalValue.Length)
        {
            return result;
        }

        long limit = negative ? long.MinValue : -long.MaxValue;
        long multiplicationLimit = limit / 10;
        long value = 0;
        while (index < lexicalValue.Length)
        {
            int digit = lexicalValue[index] - '0';
            if (digit < 0 || digit > 9)
            {
                return result;
            }
            if (value < multiplicationLimit)
            {
                return result;
            }
            value = value * 10;
            if (value < limit + digit)
            {
                return result;
            }
            value = value - digit;
            index = index + 1;
        }

        result.IntegerValue = negative ? value : -value;
        result.IsValid = true;
        return result;
    }

    internal static OdfLexicalValue ParseDecimal(string? lexicalValue)
    {
        if (lexicalValue == null)
        {
            return NewResult(null);
        }
        return ParseDecimalText(lexicalValue);
    }

    internal static OdfLexicalValue ParseFloat(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length == 0)
        {
            return result;
        }
        if (lexicalValue == "INF")
        {
            result.FloatValue = double.PositiveInfinity;
            result.IsValid = true;
            return result;
        }
        if (lexicalValue == "-INF")
        {
            result.FloatValue = double.NegativeInfinity;
            result.IsValid = true;
            return result;
        }
        if (lexicalValue == "NaN")
        {
            result.FloatValue = double.NaN;
            result.IsValid = true;
            return result;
        }

        int index = 0;
        if (lexicalValue[index] == '+' || lexicalValue[index] == '-')
        {
            index = index + 1;
        }
        if (index == lexicalValue.Length)
        {
            return result;
        }

        bool hasDigit = false;
        bool hasDecimalPoint = false;
        while (index < lexicalValue.Length && lexicalValue[index] != 'e' && lexicalValue[index] != 'E')
        {
            char character = lexicalValue[index];
            if (character == '.')
            {
                if (hasDecimalPoint)
                {
                    return result;
                }
                hasDecimalPoint = true;
            }
            else
            {
                int digit = character - '0';
                if (digit < 0 || digit > 9)
                {
                    return result;
                }
                hasDigit = true;
            }
            index = index + 1;
        }
        if (!hasDigit)
        {
            return result;
        }

        if (index < lexicalValue.Length)
        {
            index = index + 1;
            if (index < lexicalValue.Length && (lexicalValue[index] == '+' || lexicalValue[index] == '-'))
            {
                index = index + 1;
            }
            if (index == lexicalValue.Length)
            {
                return result;
            }
            while (index < lexicalValue.Length)
            {
                int digit = lexicalValue[index] - '0';
                if (digit < 0 || digit > 9)
                {
                    return result;
                }
                index = index + 1;
            }
        }

        double value = double.Parse(
            lexicalValue,
            global::System.Globalization.NumberStyles.Float,
            global::System.Globalization.CultureInfo.InvariantCulture);
        result.FloatValue = value;
        result.IsValid = true;
        return result;
    }

    internal static OdfLexicalValue ParsePercentage(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length < 2 || lexicalValue[lexicalValue.Length - 1] != '%')
        {
            return result;
        }
        OdfLexicalValue number = ParseDecimalText(lexicalValue.Substring(0, lexicalValue.Length - 1));
        if (!number.IsValid)
        {
            return result;
        }
        result.DecimalValue = number.DecimalValue;
        result.IsValid = true;
        return result;
    }

    internal static OdfLexicalValue ParseBase64(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == null)
        {
            return result;
        }

        int characterCount = 0;
        int sourceIndex = 0;
        while (sourceIndex < lexicalValue.Length)
        {
            if (!IsXmlWhitespace(lexicalValue[sourceIndex]))
            {
                characterCount = characterCount + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        if (characterCount == 0)
        {
            result.IsValid = true;
            return result;
        }
        if (characterCount % 4 != 0)
        {
            return result;
        }

        char[] characters = new char[characterCount];
        sourceIndex = 0;
        int characterIndex = 0;
        while (sourceIndex < lexicalValue.Length)
        {
            if (!IsXmlWhitespace(lexicalValue[sourceIndex]))
            {
                characters[characterIndex] = lexicalValue[sourceIndex];
                characterIndex = characterIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }

        int padding = 0;
        if (characters[characterCount - 1] == '=')
        {
            padding = padding + 1;
        }
        if (characters[characterCount - 2] == '=')
        {
            padding = padding + 1;
        }
        byte[] bytes = new byte[characterCount / 4 * 3 - padding];
        int group = 0;
        int byteIndex = 0;
        while (group < characterCount)
        {
            bool finalGroup = group + 4 == characterCount;
            int first = Base64Digit(characters[group]);
            int second = Base64Digit(characters[group + 1]);
            int third = characters[group + 2] == '=' ? 0 : Base64Digit(characters[group + 2]);
            int fourth = characters[group + 3] == '=' ? 0 : Base64Digit(characters[group + 3]);
            if (first < 0 || second < 0 || third < 0 || fourth < 0)
            {
                return result;
            }
            if (!finalGroup && (characters[group + 2] == '=' || characters[group + 3] == '='))
            {
                return result;
            }
            if (characters[group + 2] == '=' && characters[group + 3] != '=')
            {
                return result;
            }
            if (characters[group + 2] == '=' && (second & 0x0F) != 0)
            {
                return result;
            }
            if (characters[group + 3] == '=' && characters[group + 2] != '=' && (third & 0x03) != 0)
            {
                return result;
            }

            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)(first << 2 | second >> 4);
                byteIndex = byteIndex + 1;
            }
            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)((second & 0x0F) << 4 | third >> 2);
                byteIndex = byteIndex + 1;
            }
            if (byteIndex < bytes.Length)
            {
                bytes[byteIndex] = (byte)((third & 0x03) << 6 | fourth);
                byteIndex = byteIndex + 1;
            }
            group = group + 4;
        }

        result.BinaryValue = bytes;
        result.IsValid = true;
        return result;
    }

    internal static OdfLexicalValue ParseColor(string? lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length != 7 || lexicalValue[0] != '#')
        {
            return result;
        }

        uint value = 0;
        int index = 1;
        while (index < lexicalValue.Length)
        {
            int digit = HexDigit(lexicalValue[index]);
            if (digit < 0)
            {
                return result;
            }
            value = value << 4 | (uint)digit;
            index = index + 1;
        }
        result.ColorValue = value;
        result.IsValid = true;
        return result;
    }

    static OdfLexicalValue ParseDecimalText(string lexicalValue)
    {
        OdfLexicalValue result = NewResult(lexicalValue);
        if (lexicalValue.Length == 0)
        {
            return result;
        }

        bool negative = false;
        int start = 0;
        if (lexicalValue[0] == '+' || lexicalValue[0] == '-')
        {
            negative = lexicalValue[0] == '-';
            start = 1;
        }
        if (start == lexicalValue.Length)
        {
            return result;
        }

        int decimalPoint = -1;
        bool hasDigit = false;
        int index = start;
        while (index < lexicalValue.Length)
        {
            char character = lexicalValue[index];
            if (character == '.')
            {
                if (decimalPoint >= 0)
                {
                    return result;
                }
                decimalPoint = index;
            }
            else if (character >= '0' && character <= '9')
            {
                hasDigit = true;
            }
            else
            {
                return result;
            }
            index = index + 1;
        }
        if (!hasDigit)
        {
            return result;
        }

        int significantEnd = lexicalValue.Length;
        int scale = 0;
        if (decimalPoint >= 0)
        {
            while (significantEnd > decimalPoint + 1 && lexicalValue[significantEnd - 1] == '0')
            {
                significantEnd = significantEnd - 1;
            }
            scale = significantEnd - decimalPoint - 1;
            if (scale > 28)
            {
                return result;
            }
        }

        decimal value = 0;
        index = start;
        while (index < significantEnd)
        {
            if (index == decimalPoint)
            {
                index = index + 1;
                continue;
            }
            int digit = lexicalValue[index] - '0';
            if (value > (decimal.MaxValue - digit) / 10)
            {
                return result;
            }
            value = value * 10 + digit;
            index = index + 1;
        }
        index = 0;
        while (index < scale)
        {
            value = value / 10;
            index = index + 1;
        }

        result.DecimalValue = negative ? -value : value;
        result.IsValid = true;
        return result;
    }

    static OdfUnitValue ParseLengthCore(
        string? lexicalValue,
        bool allowNegative,
        bool requirePositive,
        bool pixelOnly,
        bool allowRelative)
    {
        OdfUnitValue result = NewUnitResult(lexicalValue);
        if (lexicalValue == null || lexicalValue.Length < 2)
        {
            return result;
        }

        OdfUnitKind unit = MeasurementUnit(lexicalValue);
        if (unit == OdfUnitKind.Unknown)
        {
            return result;
        }
        if (pixelOnly && unit != OdfUnitKind.Pixel)
        {
            return result;
        }
        if (!allowRelative && (unit == OdfUnitKind.Em || unit == OdfUnitKind.Ex || unit == OdfUnitKind.Percentage))
        {
            return result;
        }

        int suffixLength = UnitSuffixLength(unit);
        string numericText = lexicalValue.Substring(0, lexicalValue.Length - suffixLength);
        if (numericText.Length == 0 || numericText[0] == '+')
        {
            return result;
        }
        OdfLexicalValue numeric = ParseDecimalText(numericText);
        if (!numeric.IsValid)
        {
            return result;
        }
        if (!allowNegative && (numeric.DecimalValue < 0 || numericText[0] == '-'))
        {
            return result;
        }
        if (requirePositive && numeric.DecimalValue <= 0)
        {
            return result;
        }

        result.NumericValue = (double)numeric.DecimalValue;
        result.UnitKind = unit;
        result.IsValid = true;
        return result;
    }

    static bool ParseDateFields(OdfTemporalValue result, string value)
    {
        if (value.Length < 10)
        {
            return false;
        }
        int yearStart = value[0] == '-' ? 1 : 0;
        int yearEnd = value.IndexOf('-', yearStart);
        if (yearEnd < 0 || yearEnd - yearStart < 4 || value.Length != yearEnd + 6)
        {
            return false;
        }
        if (yearEnd - yearStart > 4 && value[yearStart] == '0')
        {
            return false;
        }
        if (!AllDigits(value, yearStart, yearEnd) ||
            value[yearEnd + 3] != '-' ||
            !AllDigits(value, yearEnd + 1, yearEnd + 3) ||
            !AllDigits(value, yearEnd + 4, yearEnd + 6))
        {
            return false;
        }

        global::System.Numerics.BigInteger year = ParseBigIntegerDigits(value, yearStart, yearEnd);
        if (year == 0)
        {
            return false;
        }
        if (yearStart == 1)
        {
            year = -year;
        }
        int month = TwoDigits(value, yearEnd + 1);
        int day = TwoDigits(value, yearEnd + 4);
        if (month < 1 || month > 12 || day < 1 || day > TemporalDaysInMonth(year, month))
        {
            return false;
        }

        result.Year = year;
        result.Month = month;
        result.Day = day;
        return true;
    }

    static bool ParseTimeFields(OdfTemporalValue result, string value)
    {
        if (value.Length < 8 || value[2] != ':' || value[5] != ':' ||
            !AllDigits(value, 0, 2) ||
            !AllDigits(value, 3, 5) ||
            !AllDigits(value, 6, 8))
        {
            return false;
        }
        string fraction = "";
        if (value.Length > 8)
        {
            if (value[8] != '.' || value.Length == 9 || !AllDigits(value, 9, value.Length))
            {
                return false;
            }
            fraction = value.Substring(9);
        }

        int hour = TwoDigits(value, 0);
        int minute = TwoDigits(value, 3);
        int second = TwoDigits(value, 6);
        if (hour > 24 || minute > 59 || second > 60)
        {
            return false;
        }
        if (hour == 24 && (minute != 0 || second != 0 || !IsZeroDigits(fraction)))
        {
            return false;
        }

        result.Hour = hour;
        result.Minute = minute;
        result.Second = second;
        result.FractionalSecond = fraction;
        return true;
    }

    static int ParseTimeZone(OdfTemporalValue result, string value)
    {
        if (value.EndsWith("Z", global::System.StringComparison.Ordinal))
        {
            result.HasTimeZone = true;
            return value.Length - 1;
        }
        if (value.Length >= 6)
        {
            int start = value.Length - 6;
            char sign = value[start];
            if ((sign == '+' || sign == '-') && value[start + 3] == ':')
            {
                if (!AllDigits(value, start + 1, start + 3) || !AllDigits(value, start + 4, start + 6))
                {
                    return -1;
                }
                int hour = TwoDigits(value, start + 1);
                int minute = TwoDigits(value, start + 4);
                if (hour > 14 || minute > 59 || (hour == 14 && minute != 0))
                {
                    return -1;
                }
                int offset = hour * 60 + minute;
                result.TimeZoneMinutes = sign == '-' ? -offset : offset;
                result.HasTimeZone = true;
                return start;
            }
        }
        return value.Length;
    }

    static bool AllDigits(string value, int start, int end)
    {
        if (start >= end)
        {
            return false;
        }
        int index = start;
        while (index < end)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsZeroDigits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '0')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static int TwoDigits(string value, int start)
    {
        return (value[start] - '0') * 10 + value[start + 1] - '0';
    }

    static global::System.Numerics.BigInteger ParseBigIntegerDigits(string value, int start, int end)
    {
        global::System.Numerics.BigInteger result = 0;
        int index = start;
        while (index < end)
        {
            result = result * 10 + value[index] - '0';
            index = index + 1;
        }
        return result;
    }

    static int TemporalDaysInMonth(global::System.Numerics.BigInteger year, int month)
    {
        if (month == 2)
        {
            return IsTemporalLeapYear(year) ? 29 : 28;
        }
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }
        return 31;
    }

    static bool IsTemporalLeapYear(global::System.Numerics.BigInteger year)
    {
        global::System.Numerics.BigInteger calendarYear = year < 0 ? year + 1 : year;
        return calendarYear % 400 == 0 || (calendarYear % 4 == 0 && calendarYear % 100 != 0);
    }

    static OdfUnitKind MeasurementUnit(string lexicalValue)
    {
        if (lexicalValue.EndsWith("mm", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Millimetre;
        }
        if (lexicalValue.EndsWith("cm", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Centimetre;
        }
        if (lexicalValue.EndsWith("in", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Inch;
        }
        if (lexicalValue.EndsWith("pt", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Point;
        }
        if (lexicalValue.EndsWith("pc", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Pica;
        }
        if (lexicalValue.EndsWith("px", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Pixel;
        }
        if (lexicalValue.EndsWith("em", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Em;
        }
        if (lexicalValue.EndsWith("ex", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Ex;
        }
        if (lexicalValue.EndsWith("%", global::System.StringComparison.Ordinal))
        {
            return OdfUnitKind.Percentage;
        }
        return OdfUnitKind.Unknown;
    }

    static int UnitSuffixLength(OdfUnitKind unit)
    {
        if (unit == OdfUnitKind.Percentage)
        {
            return 1;
        }
        return 2;
    }

    static OdfUnitValue NewUnitResult(string? lexicalValue)
    {
        OdfUnitValue result = new OdfUnitValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfTemporalValue NewTemporalResult(string? lexicalValue)
    {
        OdfTemporalValue result = new OdfTemporalValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfDurationValue NewDurationResult(string? lexicalValue)
    {
        OdfDurationValue result = new OdfDurationValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfPointValue NewPointResult(string? lexicalValue)
    {
        OdfPointValue result = new OdfPointValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfVectorValue NewVectorResult(string? lexicalValue)
    {
        OdfVectorValue result = new OdfVectorValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfViewBoxValue NewViewBoxResult(string? lexicalValue)
    {
        OdfViewBoxValue result = new OdfViewBoxValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static OdfLexicalValue NewResult(string? lexicalValue)
    {
        OdfLexicalValue result = new OdfLexicalValue();
        result.LexicalValue = lexicalValue ?? "";
        return result;
    }

    static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static int Base64Digit(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        if (value == '+')
        {
            return 62;
        }
        if (value == '/')
        {
            return 63;
        }
        return -1;
    }

    static int HexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        return -1;
    }
}
