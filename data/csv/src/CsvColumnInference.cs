using System;
using System.Globalization;

namespace csv;

internal class CsvColumnInference
{
    internal bool SawValue;
    internal bool SawNull;
    internal bool SawNonNull;
    internal bool CanBoolean;
    internal bool CanInt32;
    internal bool CanInt64;
    internal bool CanDecimal;
    internal bool CanDouble;
    internal bool CanDateOnly;
    internal bool CanTimeOnly;
    internal bool CanDateTime;
    internal bool CanGuid;
    internal bool CanBinary;
    internal int SampledCount;
    internal int MissingCount;
    internal int NullCount;
    internal int EmptyCount;
    internal int NonNullCount;
    internal string FormatHint;
    internal bool HasFormatHint;
    internal bool HasFormatConflict;

    internal CsvColumnInference()
    {
        SawValue = false;
        SawNull = false;
        SawNonNull = false;
        CanBoolean = true;
        CanInt32 = true;
        CanInt64 = true;
        CanDecimal = true;
        CanDouble = true;
        CanDateOnly = true;
        CanTimeOnly = true;
        CanDateTime = true;
        CanGuid = true;
        CanBinary = true;
        SampledCount = 0;
        MissingCount = 0;
        NullCount = 0;
        EmptyCount = 0;
        NonNullCount = 0;
        FormatHint = "";
        HasFormatHint = false;
        HasFormatConflict = false;
    }

    internal void AddValue(string? value, bool isMissing, CsvInferenceOptions options)
    {
        SawValue = true;
        RecordValueCategory(value, isMissing, options);
        if (value == null || IsNullValue(value, isMissing, options))
        {
            SawNull = true;
            return;
        }

        SawNonNull = true;
        string text = value.Trim();
        RecordFormatHint(DetectFormatHint(text, options.ConversionOptions));
        CanBoolean = CanBoolean && IsBooleanText(text);
        CanInt32 = CanInt32 && IsInt32Text(text, options.ConversionOptions);
        CanInt64 = CanInt64 && IsInt64Text(text, options.ConversionOptions);
        CanDecimal = CanDecimal && IsDecimalText(text, options.ConversionOptions);
        CanDouble = CanDouble && IsDoubleText(text, options.ConversionOptions);
        CanDateOnly = CanDateOnly && IsDateOnlyText(text, options.ConversionOptions);
        CanTimeOnly = CanTimeOnly && IsTimeOnlyText(text, options.ConversionOptions);
        CanDateTime = CanDateTime && IsDateTimeText(text, options.ConversionOptions);
        CanGuid = CanGuid && CsvConverter.IsGuidText(text);
        CanBinary = CanBinary && IsBinaryText(text);
    }

    internal void RecordValueCategory(string? value, bool isMissing, CsvInferenceOptions options)
    {
        SampledCount = SampledCount + 1;
        if (isMissing)
        {
            MissingCount = MissingCount + 1;
            return;
        }

        if (value == null)
        {
            NullCount = NullCount + 1;
            return;
        }

        if (value.Length == 0)
        {
            EmptyCount = EmptyCount + 1;
            return;
        }

        if (options.ConversionOptions.HasNullString && value == options.ConversionOptions.NullString)
        {
            NullCount = NullCount + 1;
            return;
        }

        NonNullCount = NonNullCount + 1;
    }

    internal void RecordFormatHint(string hint)
    {
        if (hint.Length == 0)
        {
            return;
        }

        if (!HasFormatHint)
        {
            FormatHint = hint;
            HasFormatHint = true;
            return;
        }

        if (FormatHint != hint)
        {
            HasFormatConflict = true;
        }
    }

    internal string ResolveFormatHint(CsvTypeKind typeKind)
    {
        if (!HasFormatHint || HasFormatConflict)
        {
            return "";
        }

        if (typeKind == CsvTypeKind.Decimal || typeKind == CsvTypeKind.DateOnly || typeKind == CsvTypeKind.TimeOnly || typeKind == CsvTypeKind.DateTime || typeKind == CsvTypeKind.Guid || typeKind == CsvTypeKind.Binary)
        {
            return FormatHint;
        }

        return "";
    }

    internal static string DetectFormatHint(string value, CsvConversionOptions options)
    {
        string dateTimeFormat = DetectDateTimeFormat(value);
        if (dateTimeFormat.Length > 0)
        {
            return dateTimeFormat;
        }

        string dateFormat = DetectDateOnlyFormat(value, options);
        if (dateFormat.Length > 0)
        {
            return dateFormat;
        }

        string timeFormat = DetectTimeOnlyFormat(value, options);
        if (timeFormat.Length > 0)
        {
            return timeFormat;
        }

        if (CsvConverter.IsGuidText(value))
        {
            return "D";
        }

        if (IsBinaryText(value))
        {
            return "X2";
        }

        if (IsDecimalText(value, options))
        {
            return DetectDecimalFormat(value, options);
        }

        return "";
    }

    internal static string DetectDateOnlyFormat(string value, CsvConversionOptions options)
    {
        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            DateTime date = CsvConverter.ToDateTimeWithFormat(value, format);
            if (date != default(DateTime))
            {
                return format;
            }

            index = index + 1;
        }

        if (CsvConverter.ToIsoDate(value) != default(DateTime))
        {
            return "yyyy-MM-dd";
        }

        return "";
    }

    internal static string DetectTimeOnlyFormat(string value, CsvConversionOptions options)
    {
        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            TimeOnly time = CsvConverter.ToTimeOnlyWithFormat(value, format);
            if (time != default(TimeOnly))
            {
                return format;
            }

            index = index + 1;
        }

        if (CsvConverter.ToClockTime(value) != default(TimeOnly))
        {
            if (value.Length == 5)
            {
                return "HH:mm";
            }

            if (value.Length == 8)
            {
                return "HH:mm:ss";
            }
        }

        return "";
    }

    internal static string DetectDateTimeFormat(string value)
    {
        if (value.Length != 19)
        {
            return "";
        }

        if (CsvConverter.ToIsoDateTime(value) == default(DateTime))
        {
            return "";
        }

        if (value[10] == 'T')
        {
            return "yyyy-MM-ddTHH:mm:ss";
        }

        if (value[10] == ' ')
        {
            return "yyyy-MM-dd HH:mm:ss";
        }

        return "";
    }

    internal static string DetectDecimalFormat(string value, CsvConversionOptions options)
    {
        string normalized = options.NormalizeNumberString(value);
        int decimalIndex = normalized.IndexOf('.');
        int decimalPlaces = 0;
        if (decimalIndex >= 0)
        {
            decimalPlaces = normalized.Length - decimalIndex - 1;
        }

        string format = "0";
        if (options.ThousandsSeparator.Length > 0 && value.Contains(options.ThousandsSeparator))
        {
            format = "#,##0";
        }

        if (decimalPlaces > 0)
        {
            format = format + "." + new string('0', decimalPlaces);
        }

        return format;
    }

    internal bool ResolveNullable(CsvInferenceOptions options)
    {
        if (!options.InferNullability)
        {
            return true;
        }

        if (SawNull)
        {
            return true;
        }

        if (!SawValue)
        {
            return true;
        }

        return false;
    }

    internal CsvTypeKind ResolveType(CsvInferenceOptions options)
    {
        if (!options.InferTypes)
        {
            return CsvTypeKind.Unknown;
        }

        if (!SawNonNull)
        {
            if (SawValue)
            {
                return CsvTypeKind.Null;
            }

            return CsvTypeKind.Unknown;
        }

        if (CanBoolean)
        {
            return CsvTypeKind.Boolean;
        }

        if (CanInt32)
        {
            return CsvTypeKind.Int32;
        }

        if (CanInt64)
        {
            return CsvTypeKind.Int64;
        }

        if (CanDecimal)
        {
            return CsvTypeKind.Decimal;
        }

        if (CanDouble)
        {
            return CsvTypeKind.Double;
        }

        if (CanDateOnly)
        {
            return CsvTypeKind.DateOnly;
        }

        if (CanTimeOnly)
        {
            return CsvTypeKind.TimeOnly;
        }

        if (CanDateTime)
        {
            return CsvTypeKind.DateTime;
        }

        if (CanGuid)
        {
            return CsvTypeKind.Guid;
        }

        if (CanBinary)
        {
            return CsvTypeKind.Binary;
        }

        return CsvTypeKind.String;
    }

    internal static bool IsNullValue(string? value, bool isMissing, CsvInferenceOptions options)
    {
        if (isMissing)
        {
            return true;
        }

        if (value == null)
        {
            return true;
        }

        if (options.TreatEmptyAsNull && value.Length == 0)
        {
            return true;
        }

        if (options.ConversionOptions.IsNullValue(value))
        {
            return true;
        }

        return false;
    }

    internal static bool IsBooleanText(string value)
    {
        string normalized = value.ToUpperInvariant();
        if (normalized == "TRUE" || normalized == "T" || normalized == "YES" || normalized == "Y" || normalized == "1")
        {
            return true;
        }

        if (normalized == "FALSE" || normalized == "F" || normalized == "NO" || normalized == "N" || normalized == "0")
        {
            return true;
        }

        return false;
    }

    internal static bool IsInt32Text(string value, CsvConversionOptions options)
    {
        string normalized = options.NormalizeNumberString(value);
        if (!CsvConverter.IsIntegerText(normalized))
        {
            return false;
        }

        bool fits = FitsSignedRange(normalized, "2147483647", "2147483648");
        return fits;
    }

    internal static bool IsInt64Text(string value, CsvConversionOptions options)
    {
        string normalized = options.NormalizeNumberString(value);
        if (!CsvConverter.IsIntegerText(normalized))
        {
            return false;
        }

        bool fits = FitsSignedRange(normalized, "9223372036854775807", "9223372036854775808");
        return fits;
    }

    internal static bool FitsSignedRange(string value, string positiveLimit, string negativeLimit)
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

        string digits = TrimLeadingZeros(value.Substring(index));
        if (negative)
        {
            return IsMagnitudeWithinLimit(digits, negativeLimit);
        }

        return IsMagnitudeWithinLimit(digits, positiveLimit);
    }

    internal static string TrimLeadingZeros(string value)
    {
        int index = 0;
        while (index < value.Length - 1 && value[index] == '0')
        {
            index = index + 1;
        }

        string trimmed = value.Substring(index);
        return trimmed;
    }

    internal static bool IsMagnitudeWithinLimit(string digits, string limit)
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

    internal static bool IsDecimalText(string value, CsvConversionOptions options)
    {
        string normalized = options.NormalizeNumberString(value);
        bool isDecimal = CsvConverter.IsDecimalText(normalized);
        return isDecimal;
    }

    internal static bool IsDoubleText(string value, CsvConversionOptions options)
    {
        string normalized = options.NormalizeNumberString(value);
        bool isDouble = CsvConverter.IsDoubleText(normalized);
        return isDouble;
    }

    internal static bool IsDateOnlyText(string value, CsvConversionOptions options)
    {
        if (options.HasDateTimeFormats())
        {
            int index = 0;
            while (index < options.DateTimeFormats.Length)
            {
                string format = options.DateTimeFormats[index];
                if (format == "yyyy-MM-dd" || format == "yyyyMMdd" || format == "dd/MM/yyyy")
                {
                    DateTime formatted = CsvConverter.ToDateTimeWithFormat(value, format);
                    if (formatted != default(DateTime))
                    {
                        return true;
                    }
                }

                index = index + 1;
            }
        }

        DateTime iso = CsvConverter.ToIsoDate(value);
        if (iso != default(DateTime))
        {
            return true;
        }

        return false;
    }

    internal static bool IsTimeOnlyText(string value, CsvConversionOptions options)
    {
        if (options.HasDateTimeFormats())
        {
            int index = 0;
            while (index < options.DateTimeFormats.Length)
            {
                string format = options.DateTimeFormats[index];
                if (format == "HH:mm" || format == "HH:mm:ss")
                {
                    TimeOnly formatted = CsvConverter.ToTimeOnlyWithFormat(value, format);
                    if (formatted != default(TimeOnly))
                    {
                        return true;
                    }
                }

                index = index + 1;
            }
        }

        TimeOnly timeOnly = CsvConverter.ToClockTime(value);
        if (timeOnly != default(TimeOnly))
        {
            return true;
        }

        return false;
    }

    internal static bool IsDateTimeText(string value, CsvConversionOptions options)
    {
        if (options.HasDateTimeFormats())
        {
            DateTime formatted = CsvConverter.ToDateTimeWithFormats(value, options);
            if (formatted != default(DateTime))
            {
                return true;
            }
        }

        DateTime iso = CsvConverter.ToIsoDateTime(value);
        if (iso != default(DateTime))
        {
            return true;
        }

        return false;
    }

    internal static bool IsBinaryText(string value)
    {
        string compact = CsvConverter.CompactHexText(value);
        if (compact.Length == 0)
        {
            return false;
        }

        bool isBinary = CsvConverter.IsHexByteText(compact);
        return isBinary;
    }
}
