using System;
using System.Globalization;

namespace csv;

/// <summary>
/// Converts CSV field text to and from common built-in .NET value types.
/// </summary>
internal static class CsvConverter
{
    /// <summary>
    /// Formats a value using column type and culture, including configured null fallback.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="column">Column metadata used for formatting.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static string ToStringValue(object? value, CsvColumn column, CsvConversionOptions options)
    {
        if (value == null)
        {
            return ResolveNullOutput(options);
        }

        string? typeName = value.GetType().FullName;
        if (typeName == "System.String")
        {
            string textValue = (string)value;
            if (options.IsNullValue(textValue))
            {
                return ResolveNullOutput(options);
            }
        }

        if (column.TypeKind == CsvTypeKind.Null)
        {
            return ResolveNullOutput(options);
        }

        if (!IsOutputTypeCompatible(typeName, column.TypeKind))
        {
            return ResolveInvalidOutput(options);
        }

        if (column.TypeKind == CsvTypeKind.Binary)
        {
            byte[] bytes = (byte[])value;
            return FormatByteArray(bytes, column.FormatHint);
        }

        string format = ResolveOutputFormat(column, options);
        CultureInfo culture = options.Culture;
        if (IsNumericType(column.TypeKind))
        {
            culture = CreateNumberCulture(options);
        }

        string formatted = FormatOutputValue(value, format, culture);
        return formatted;
    }

    internal static string ResolveNullOutput(CsvConversionOptions options)
    {
        if (options.UseDefaultForNull)
        {
            return options.DefaultValue;
        }

        return "";
    }

    internal static string ResolveInvalidOutput(CsvConversionOptions options)
    {
        if (options.UseDefaultForInvalid)
        {
            return options.DefaultValue;
        }

        return "";
    }

    internal static bool IsOutputTypeCompatible(string? typeName, CsvTypeKind typeKind)
    {
        if (typeKind == CsvTypeKind.Unknown || typeKind == CsvTypeKind.String)
        {
            return true;
        }

        if (typeKind == CsvTypeKind.Boolean)
        {
            return typeName == "System.Boolean";
        }

        if (typeKind == CsvTypeKind.Int32)
        {
            return typeName == "System.Int32";
        }

        if (typeKind == CsvTypeKind.Int64)
        {
            return typeName == "System.Int64" || typeName == "System.Int32";
        }

        if (typeKind == CsvTypeKind.Decimal)
        {
            return typeName == "System.Decimal" || typeName == "System.Int64" || typeName == "System.Int32";
        }

        if (typeKind == CsvTypeKind.Double)
        {
            return typeName == "System.Double" || typeName == "System.Single" || typeName == "System.Decimal" || typeName == "System.Int64" || typeName == "System.Int32";
        }

        if (typeKind == CsvTypeKind.DateTime)
        {
            return typeName == "System.DateTime";
        }

        if (typeKind == CsvTypeKind.DateOnly)
        {
            return typeName == "System.DateOnly";
        }

        if (typeKind == CsvTypeKind.TimeOnly)
        {
            return typeName == "System.TimeOnly";
        }

        if (typeKind == CsvTypeKind.Guid)
        {
            return typeName == "System.Guid";
        }

        if (typeKind == CsvTypeKind.Binary)
        {
            return typeName == "System.Byte[]";
        }

        return false;
    }

    internal static bool IsNumericType(CsvTypeKind typeKind)
    {
        if (typeKind == CsvTypeKind.Int32 || typeKind == CsvTypeKind.Int64 || typeKind == CsvTypeKind.Decimal || typeKind == CsvTypeKind.Double)
        {
            return true;
        }

        return false;
    }

    internal static CultureInfo CreateNumberCulture(CsvConversionOptions options)
    {
        CultureInfo culture = (CultureInfo)options.Culture.Clone();
        if (options.DecimalSeparator.Length > 0)
        {
            culture.NumberFormat.NumberDecimalSeparator = options.DecimalSeparator;
        }

        culture.NumberFormat.NumberGroupSeparator = options.ThousandsSeparator;
        return culture;
    }

    internal static string ResolveOutputFormat(CsvColumn column, CsvConversionOptions options)
    {
        if (column.FormatHint.Length > 0)
        {
            return column.FormatHint;
        }

        if ((column.TypeKind == CsvTypeKind.DateTime || column.TypeKind == CsvTypeKind.DateOnly || column.TypeKind == CsvTypeKind.TimeOnly) && options.DateTimeFormats.Length > 0)
        {
            return options.DateTimeFormats[0];
        }

        if (column.TypeKind == CsvTypeKind.DateTime)
        {
            return "yyyy-MM-ddTHH:mm:ss";
        }

        if (column.TypeKind == CsvTypeKind.DateOnly)
        {
            return "yyyy-MM-dd";
        }

        if (column.TypeKind == CsvTypeKind.TimeOnly)
        {
            return "HH:mm:ss";
        }

        if (column.TypeKind == CsvTypeKind.Guid)
        {
            return "D";
        }

        if (column.TypeKind == CsvTypeKind.Binary)
        {
            return "X2";
        }

        return "";
    }

    internal static string FormatOutputValue(object value, string format, CultureInfo culture)
    {
        if (format.Length == 0)
        {
            return string.Format(culture, "{0}", value);
        }

        string compositeFormat = "{0:" + format + "}";
        return string.Format(culture, compositeFormat, value);
    }

    internal static string FormatByteArray(byte[] bytes, string formatHint)
    {
        string format = formatHint;
        if (format != "x2")
        {
            format = "X2";
        }

        string text = "";
        int index = 0;
        while (index < bytes.Length)
        {
            text = text + bytes[index].ToString(format, CultureInfo.InvariantCulture);
            index = index + 1;
        }

        return text;
    }

    /// <summary>
    /// Converts trimmed true, false, 1, or 0 text to a Boolean value.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted Boolean; invalid text resolves to the configured fallback or <c>false</c>.</returns>
    internal static bool ToBoolean(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string normalized = text.Trim().ToUpperInvariant();
        if (normalized == "TRUE" || normalized == "T" || normalized == "YES" || normalized == "Y" || normalized == "1")
        {
            return true;
        }

        if (normalized == "FALSE" || normalized == "F" || normalized == "NO" || normalized == "N" || normalized == "0")
        {
            return false;
        }

        string fallback = ResolveInvalidInput(options);
        if (fallback != text)
        {
            return ToBoolean(fallback, CreateInvalidSafeOptions(options));
        }

        return false;
    }

    /// <summary>
    /// Converts normalized integer text to a signed 32-bit value.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted integer; invalid or out-of-range text resolves to the configured fallback or zero.</returns>
    internal static int ToInt32(string? value, CsvConversionOptions options)
    {
        long number = ToInt64(value, options);
        if (number > 2147483647)
        {
            return ToInvalidInt32(options);
        }

        if (number < -2147483648)
        {
            return ToInvalidInt32(options);
        }

        return (int)number;
    }

    /// <summary>
    /// Converts normalized integer text to a signed 64-bit value.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted integer; invalid or out-of-range text resolves to the configured fallback or zero.</returns>
    internal static long ToInt64(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string normalized = options.NormalizeNumberString(text.Trim());
        if (!IsIntegerText(normalized))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToInt64(fallback, CreateInvalidSafeOptions(options));
            }

            return 0;
        }

        if (!IsInt64RangeText(normalized))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToInt64(fallback, CreateInvalidSafeOptions(options));
            }

            return 0;
        }

        long number = long.Parse(normalized, CultureInfo.InvariantCulture);
        return number;
    }

    /// <summary>
    /// Converts culture-normalized text to a decimal value.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static decimal ToDecimal(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string normalized = options.NormalizeNumberString(text.Trim());
        if (!IsDecimalText(normalized) || !CsvConversionResults.IsDecimalInRange(normalized))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToDecimal(fallback, CreateInvalidSafeOptions(options));
            }

            return 0m;
        }

        decimal number = decimal.Parse(normalized, CultureInfo.InvariantCulture);
        return number;
    }

    /// <summary>
    /// Converts culture-normalized text, including exponents, to a double.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static double ToDouble(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string normalized = options.NormalizeNumberString(text.Trim());
        if (!IsDoubleText(normalized) || !CsvConversionResults.IsDoubleExponentInRange(normalized))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToDouble(fallback, CreateInvalidSafeOptions(options));
            }

            return 0d;
        }

        double number = double.Parse(normalized, CultureInfo.InvariantCulture);
        if (double.IsInfinity(number))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToDouble(fallback, CreateInvalidSafeOptions(options));
            }

            return 0d;
        }

        return number;
    }

    /// <summary>
    /// Converts text using configured exact formats and supported common forms.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static DateTime ToDateTime(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string trimmed = text.Trim();
        if (options.HasDateTimeFormats())
        {
            DateTime formatted = ToDateTimeWithFormats(trimmed, options);
            if (formatted != default(DateTime))
            {
                return formatted;
            }
        }

        DateTime iso = ToIsoDateTime(trimmed);
        if (iso != default(DateTime))
        {
            return iso;
        }

        string fallback = ResolveInvalidInput(options);
        if (fallback != text)
        {
            return ToDateTime(fallback, CreateInvalidSafeOptions(options));
        }

        return default(DateTime);
    }

    /// <summary>
    /// Converts text to a calendar date.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static DateOnly ToDateOnly(string? value, CsvConversionOptions options)
    {
        DateTime dateTime = ToDateTime(value, options);
        if (dateTime == default(DateTime))
        {
            return default(DateOnly);
        }

        DateOnly dateOnly = DateOnly.FromDateTime(dateTime);
        return dateOnly;
    }

    /// <summary>
    /// Converts text to a clock time.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static TimeOnly ToTimeOnly(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string trimmed = text.Trim();
        if (options.HasDateTimeFormats())
        {
            TimeOnly formatted = ToTimeOnlyWithFormats(trimmed, options);
            if (formatted != default(TimeOnly))
            {
                return formatted;
            }
        }

        TimeOnly timeOnly = ToClockTime(trimmed);
        if (timeOnly != default(TimeOnly))
        {
            return timeOnly;
        }

        string fallback = ResolveInvalidInput(options);
        if (fallback != text)
        {
            return ToTimeOnly(fallback, CreateInvalidSafeOptions(options));
        }

        return default(TimeOnly);
    }

    /// <summary>
    /// Converts standard GUID text.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static Guid ToGuid(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string trimmed = text.Trim();
        if (IsGuidText(trimmed))
        {
            Guid guid = new Guid(trimmed);
            return guid;
        }

        string fallback = ResolveInvalidInput(options);
        if (fallback != text)
        {
            return ToGuid(fallback, CreateInvalidSafeOptions(options));
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Converts hexadecimal text, ignoring spaces and hyphens, into bytes.
    /// </summary>
    /// <param name="value">The value or text to process.</param>
    /// <param name="options">Conversion settings.</param>
    /// <returns>The converted value.</returns>
    internal static byte[] ToByteArray(string? value, CsvConversionOptions options)
    {
        string text = ResolveInput(value, options);
        string compact = CompactHexText(text);
        if (!IsHexByteText(compact))
        {
            string fallback = ResolveInvalidInput(options);
            if (fallback != text)
            {
                return ToByteArray(fallback, CreateInvalidSafeOptions(options));
            }

            return new byte[0];
        }

        byte[] bytes = new byte[compact.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            int high = HexValue(compact[index * 2]);
            int low = HexValue(compact[index * 2 + 1]);
            bytes[index] = (byte)(high * 16 + low);
            index = index + 1;
        }

        return bytes;
    }

    internal static string ResolveInput(string? value, CsvConversionOptions options)
    {
        if (value == null || options.IsNullValue(value))
        {
            if (options.UseDefaultForNull)
            {
                return options.DefaultValue;
            }

            return "";
        }

        return value;
    }

    internal static string ResolveInvalidInput(CsvConversionOptions options)
    {
        if (options.UseDefaultForInvalid)
        {
            return options.DefaultValue;
        }

        return "";
    }

    internal static CsvConversionOptions CreateInvalidSafeOptions(CsvConversionOptions options)
    {
        CsvConversionOptions clone = options.Clone();
        clone.UseDefaultForInvalid = false;
        return clone;
    }

    internal static int ToInvalidInt32(CsvConversionOptions options)
    {
        string fallback = ResolveInvalidInput(options);
        if (fallback.Length > 0)
        {
            return ToInt32(fallback, CreateInvalidSafeOptions(options));
        }

        return 0;
    }

    internal static bool IsIntegerText(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        if (value[0] == '-' || value[0] == '+')
        {
            if (value.Length == 1)
            {
                return false;
            }

            index = 1;
        }

        while (index < value.Length)
        {
            if (!IsDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsInt64RangeText(string value)
    {
        bool negative = false;
        int index = 0;
        if (value[0] == '-')
        {
            negative = true;
            index = 1;
        }
        else if (value[0] == '+')
        {
            index = 1;
        }

        string digits = TrimIntegerLeadingZeros(value.Substring(index));
        if (negative)
        {
            return IsIntegerMagnitudeWithinLimit(digits, "9223372036854775808");
        }

        return IsIntegerMagnitudeWithinLimit(digits, "9223372036854775807");
    }

    internal static string TrimIntegerLeadingZeros(string value)
    {
        int index = 0;
        while (index < value.Length - 1 && value[index] == '0')
        {
            index = index + 1;
        }

        string trimmed = value.Substring(index);
        return trimmed;
    }

    internal static bool IsIntegerMagnitudeWithinLimit(string digits, string limit)
    {
        if (digits.Length < limit.Length)
        {
            return true;
        }

        if (digits.Length > limit.Length)
        {
            return false;
        }

        int index = 0;
        while (index < digits.Length)
        {
            if (digits[index] < limit[index])
            {
                return true;
            }

            if (digits[index] > limit[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDecimalText(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        bool sawDigit = false;
        bool sawDot = false;
        if (value[0] == '-' || value[0] == '+')
        {
            if (value.Length == 1)
            {
                return false;
            }

            index = 1;
        }

        while (index < value.Length)
        {
            char character = value[index];
            if (IsDigit(character))
            {
                sawDigit = true;
            }
            else if (character == '.' && !sawDot)
            {
                sawDot = true;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return sawDigit;
    }

    internal static bool IsDoubleText(string value)
    {
        int exponentIndex = FindExponentIndex(value);
        if (exponentIndex < 0)
        {
            bool decimalText = IsDecimalText(value);
            return decimalText;
        }

        if (exponentIndex == 0 || exponentIndex == value.Length - 1)
        {
            return false;
        }

        string mantissa = value.Substring(0, exponentIndex);
        string exponent = value.Substring(exponentIndex + 1);
        if (!IsDecimalText(mantissa))
        {
            return false;
        }

        if (!IsIntegerText(exponent))
        {
            return false;
        }

        return true;
    }

    internal static int FindExponentIndex(string value)
    {
        int found = -1;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 'e' || character == 'E')
            {
                if (found >= 0)
                {
                    return -1;
                }

                found = index;
            }

            index = index + 1;
        }

        return found;
    }

    internal static bool IsDigit(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return true;
        }

        return false;
    }

    internal static DateTime ToDateTimeWithFormats(string value, CsvConversionOptions options)
    {
        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            DateTime dateTime = ToDateTimeWithFormat(value, format);
            if (dateTime != default(DateTime))
            {
                return dateTime;
            }

            index = index + 1;
        }

        return default(DateTime);
    }

    internal static DateTime ToDateTimeWithFormat(string value, string format)
    {
        if (format == "yyyy-MM-dd")
        {
            return ToIsoDate(value);
        }

        if (format == "yyyyMMdd")
        {
            return ToCompactDate(value);
        }

        if (format == "dd/MM/yyyy")
        {
            return ToSlashDate(value);
        }

        return default(DateTime);
    }

    internal static TimeOnly ToTimeOnlyWithFormats(string value, CsvConversionOptions options)
    {
        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            TimeOnly timeOnly = ToTimeOnlyWithFormat(value, format);
            if (timeOnly != default(TimeOnly))
            {
                return timeOnly;
            }

            index = index + 1;
        }

        return default(TimeOnly);
    }

    internal static TimeOnly ToTimeOnlyWithFormat(string value, string format)
    {
        if (format == "HH:mm")
        {
            return ToClockTime(value);
        }

        if (format == "HH:mm:ss")
        {
            return ToClockTime(value);
        }

        return default(TimeOnly);
    }

    internal static TimeOnly ToClockTime(string value)
    {
        if (value.Length != 5 && value.Length != 8)
        {
            return default(TimeOnly);
        }

        if (value[2] != ':')
        {
            return default(TimeOnly);
        }

        int hour = ParseTwoDigits(value, 0);
        int minute = ParseTwoDigits(value, 3);
        int second = 0;
        if (value.Length == 8)
        {
            if (value[5] != ':')
            {
                return default(TimeOnly);
            }

            second = ParseTwoDigits(value, 6);
        }

        return CreateTime(hour, minute, second);
    }

    internal static TimeOnly CreateTime(int hour, int minute, int second)
    {
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            return default(TimeOnly);
        }

        TimeOnly timeOnly = new TimeOnly(hour, minute, second);
        return timeOnly;
    }

    internal static DateTime ToIsoDateTime(string value)
    {
        if (value.Length == 10)
        {
            return ToIsoDate(value);
        }

        if (value.Length == 19)
        {
            DateTime date = ToIsoDate(value.Substring(0, 10));
            if (date == default(DateTime))
            {
                return default(DateTime);
            }

            char separator = value[10];
            if (separator != 'T' && separator != ' ')
            {
                return default(DateTime);
            }

            if (value[13] != ':' || value[16] != ':')
            {
                return default(DateTime);
            }

            int hour = ParseTwoDigits(value, 11);
            int minute = ParseTwoDigits(value, 14);
            int second = ParseTwoDigits(value, 17);
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                return default(DateTime);
            }

            DateTime dateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, second);
            return dateTime;
        }

        return default(DateTime);
    }

    internal static DateTime ToIsoDate(string value)
    {
        if (value.Length != 10)
        {
            return default(DateTime);
        }

        if (value[4] != '-' || value[7] != '-')
        {
            return default(DateTime);
        }

        int year = ParseFourDigits(value, 0);
        int month = ParseTwoDigits(value, 5);
        int day = ParseTwoDigits(value, 8);
        return CreateDate(year, month, day);
    }

    internal static DateTime ToCompactDate(string value)
    {
        if (value.Length != 8)
        {
            return default(DateTime);
        }

        int year = ParseFourDigits(value, 0);
        int month = ParseTwoDigits(value, 4);
        int day = ParseTwoDigits(value, 6);
        return CreateDate(year, month, day);
    }

    internal static DateTime ToSlashDate(string value)
    {
        if (value.Length != 10)
        {
            return default(DateTime);
        }

        if (value[2] != '/' || value[5] != '/')
        {
            return default(DateTime);
        }

        int day = ParseTwoDigits(value, 0);
        int month = ParseTwoDigits(value, 3);
        int year = ParseFourDigits(value, 6);
        return CreateDate(year, month, day);
    }

    internal static DateTime CreateDate(int year, int month, int day)
    {
        if (year < 1 || month < 1 || month > 12 || day < 1)
        {
            return default(DateTime);
        }

        int maxDay = DaysInMonth(year, month);
        if (day > maxDay)
        {
            return default(DateTime);
        }

        DateTime date = new DateTime(year, month, day);
        return date;
    }

    internal static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            if (IsLeapYear(year))
            {
                return 29;
            }

            return 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    internal static bool IsLeapYear(int year)
    {
        if (year % 400 == 0)
        {
            return true;
        }

        if (year % 100 == 0)
        {
            return false;
        }

        if (year % 4 == 0)
        {
            return true;
        }

        return false;
    }

    internal static int ParseTwoDigits(string value, int start)
    {
        if (start < 0 || start > value.Length - 2)
        {
            return -1;
        }

        if (!IsDigit(value[start]) || !IsDigit(value[start + 1]))
        {
            return -1;
        }

        int number = (value[start] - '0') * 10 + (value[start + 1] - '0');
        return number;
    }

    internal static int ParseFourDigits(string value, int start)
    {
        if (start < 0 || start > value.Length - 4)
        {
            return -1;
        }

        if (!IsDigit(value[start]) || !IsDigit(value[start + 1]) || !IsDigit(value[start + 2]) || !IsDigit(value[start + 3]))
        {
            return -1;
        }

        int number = (value[start] - '0') * 1000 + (value[start + 1] - '0') * 100 + (value[start + 2] - '0') * 10 + (value[start + 3] - '0');
        return number;
    }

    internal static bool IsGuidText(string value)
    {
        if (value.Length != 36)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (index == 8 || index == 13 || index == 18 || index == 23)
            {
                if (character != '-')
                {
                    return false;
                }
            }
            else if (!IsHexDigit(character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsHexByteText(string value)
    {
        if (value.Length == 0)
        {
            return true;
        }

        if (value.Length % 2 != 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsHexDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string CompactHexText(string value)
    {
        string compact = "";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '-' && character != ':')
            {
                compact = compact + character;
            }

            index = index + 1;
        }

        return compact;
    }

    internal static bool IsHexDigit(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return true;
        }

        if (character >= 'a' && character <= 'f')
        {
            return true;
        }

        if (character >= 'A' && character <= 'F')
        {
            return true;
        }

        return false;
    }

    internal static int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return 0;
    }
}
