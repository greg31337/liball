using System;
using System.Globalization;

namespace csv;

internal static class CsvConversionResults
{
    internal static CsvConversionResult ConvertString(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.String);
        if (CanConvert(result))
        {
            CompleteValue(result, row, fieldIndex);
        }

        return result;
    }

    internal static CsvConversionResult ConvertBoolean(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Boolean);
        while (CanConvert(result))
        {
            string normalized = result.TextValue.Trim().ToUpperInvariant();
            if (normalized == "TRUE" || normalized == "T" || normalized == "YES" || normalized == "Y" || normalized == "1")
            {
                result.BooleanValue = true;
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (normalized == "FALSE" || normalized == "F" || normalized == "NO" || normalized == "N" || normalized == "0")
            {
                result.BooleanValue = false;
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (!UseInvalidDefault(result, CsvConversionStatus.Invalid, options))
            {
                CompleteFailure(result, CsvConversionStatus.Invalid, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertInt32(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Int32);
        while (CanConvert(result))
        {
            string normalized = options.NormalizeNumberString(result.TextValue.Trim());
            CsvConversionStatus failure = CsvConversionStatus.Success;
            if (!CsvConverter.IsIntegerText(normalized))
            {
                failure = CsvConversionStatus.Invalid;
            }
            else if (!CsvConverter.IsInt64RangeText(normalized))
            {
                failure = CsvConversionStatus.OutOfRange;
            }
            else
            {
                long number = long.Parse(normalized, CultureInfo.InvariantCulture);
                if (number < -2147483648 || number > 2147483647)
                {
                    failure = CsvConversionStatus.OutOfRange;
                }
                else
                {
                    result.Int32Value = (int)number;
                    CompleteValue(result, row, fieldIndex);
                    return result;
                }
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertInt64(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Int64);
        while (CanConvert(result))
        {
            string normalized = options.NormalizeNumberString(result.TextValue.Trim());
            CsvConversionStatus failure = CsvConversionStatus.Success;
            if (!CsvConverter.IsIntegerText(normalized))
            {
                failure = CsvConversionStatus.Invalid;
            }
            else if (!CsvConverter.IsInt64RangeText(normalized))
            {
                failure = CsvConversionStatus.OutOfRange;
            }
            else
            {
                result.Int64Value = long.Parse(normalized, CultureInfo.InvariantCulture);
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertDecimal(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Decimal);
        while (CanConvert(result))
        {
            string normalized = options.NormalizeNumberString(result.TextValue.Trim());
            CsvConversionStatus failure = CsvConversionStatus.Success;
            if (!CsvConverter.IsDecimalText(normalized))
            {
                failure = CsvConversionStatus.Invalid;
            }
            else if (!IsDecimalInRange(normalized))
            {
                failure = CsvConversionStatus.OutOfRange;
            }
            else
            {
                result.DecimalValue = decimal.Parse(normalized, CultureInfo.InvariantCulture);
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertDouble(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Double);
        while (CanConvert(result))
        {
            string normalized = options.NormalizeNumberString(result.TextValue.Trim());
            CsvConversionStatus failure = CsvConversionStatus.Success;
            if (!CsvConverter.IsDoubleText(normalized))
            {
                failure = CsvConversionStatus.Invalid;
            }
            else if (!IsDoubleExponentInRange(normalized))
            {
                failure = CsvConversionStatus.OutOfRange;
            }
            else
            {
                double number = double.Parse(normalized, CultureInfo.InvariantCulture);
                if (double.IsInfinity(number))
                {
                    failure = CsvConversionStatus.OutOfRange;
                }
                else
                {
                    result.DoubleValue = number;
                    CompleteValue(result, row, fieldIndex);
                    return result;
                }
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertDateTime(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.DateTime);
        CsvConversionOptions safeOptions = CreateSafeOptions(options);
        while (CanConvert(result))
        {
            string text = result.TextValue.Trim();
            DateTime value = CsvConverter.ToDateTime(text, safeOptions);
            if (value != default(DateTime) || (IsMinimumDateTime(text) && HasDateTimeShape(text, options)))
            {
                result.DateTimeValue = value;
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            CsvConversionStatus failure = CsvConversionStatus.Invalid;
            if (HasDateTimeShape(text, options))
            {
                failure = CsvConversionStatus.OutOfRange;
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertDateOnly(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.DateOnly);
        CsvConversionOptions safeOptions = CreateSafeOptions(options);
        while (CanConvert(result))
        {
            string text = result.TextValue.Trim();
            DateOnly value = CsvConverter.ToDateOnly(text, safeOptions);
            if (value != default(DateOnly) || (IsMinimumDateTime(text) && HasDateTimeShape(text, options)))
            {
                result.DateOnlyValue = value;
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            CsvConversionStatus failure = CsvConversionStatus.Invalid;
            if (HasDateTimeShape(text, options))
            {
                failure = CsvConversionStatus.OutOfRange;
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertTimeOnly(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.TimeOnly);
        CsvConversionOptions safeOptions = CreateSafeOptions(options);
        while (CanConvert(result))
        {
            string text = result.TextValue.Trim();
            TimeOnly value = CsvConverter.ToTimeOnly(text, safeOptions);
            if (value != default(TimeOnly) || IsMidnight(text))
            {
                result.TimeOnlyValue = value;
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            CsvConversionStatus failure = CsvConversionStatus.Invalid;
            if (HasTimeShape(text, options))
            {
                failure = CsvConversionStatus.OutOfRange;
            }

            if (!UseInvalidDefault(result, failure, options))
            {
                CompleteFailure(result, failure, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertGuid(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Guid);
        while (CanConvert(result))
        {
            string text = result.TextValue.Trim();
            if (CsvConverter.IsGuidText(text))
            {
                result.GuidValue = new Guid(text);
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (!UseInvalidDefault(result, CsvConversionStatus.Invalid, options))
            {
                CompleteFailure(result, CsvConversionStatus.Invalid, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult ConvertByteArray(CsvRow row, int fieldIndex, CsvConversionOptions options)
    {
        CsvConversionResult result = PrepareResult(row, fieldIndex, options, CsvTypeKind.Binary);
        while (CanConvert(result))
        {
            string compact = CsvConverter.CompactHexText(result.TextValue);
            if (CsvConverter.IsHexByteText(compact))
            {
                result.ByteArrayValue = CsvConverter.ToByteArray(result.TextValue, CreateSafeOptions(options));
                CompleteValue(result, row, fieldIndex);
                return result;
            }

            if (!UseInvalidDefault(result, CsvConversionStatus.Invalid, options))
            {
                CompleteFailure(result, CsvConversionStatus.Invalid, row, fieldIndex);
                return result;
            }
        }

        return result;
    }

    internal static CsvConversionResult PrepareResult(CsvRow row, int fieldIndex, CsvConversionOptions options, CsvTypeKind typeKind)
    {
        CsvConversionResult result = new CsvConversionResult();
        result.Status = CsvConversionStatus.Success;
        result.SourceStatus = CsvConversionStatus.Success;
        result.TypeKind = typeKind;

        if (!row.HasField(fieldIndex))
        {
            SetInputState(result, CsvConversionStatus.Missing, options.UseDefaultForNull, options.DefaultValue, row, fieldIndex);
            return result;
        }

        string? value = row.GetField(fieldIndex);
        if (value == null)
        {
            SetInputState(result, CsvConversionStatus.Null, options.UseDefaultForNull, options.DefaultValue, row, fieldIndex);
            return result;
        }

        if (value.Length == 0)
        {
            bool useDefault = options.EmptyStringIsNull && options.UseDefaultForNull;
            SetInputState(result, CsvConversionStatus.Empty, useDefault, options.DefaultValue, row, fieldIndex);
            return result;
        }

        if (options.HasNullString && value == options.NullString)
        {
            SetInputState(result, CsvConversionStatus.Null, options.UseDefaultForNull, options.DefaultValue, row, fieldIndex);
            return result;
        }

        result.TextValue = value;
        return result;
    }

    internal static void SetInputState(CsvConversionResult result, CsvConversionStatus status, bool useDefault, string defaultValue, CsvRow row, int fieldIndex)
    {
        result.SourceStatus = status;
        if (useDefault)
        {
            result.Status = CsvConversionStatus.Defaulted;
            result.TextValue = defaultValue;
            return;
        }

        result.Status = status;
        AddStateDiagnostic(result, status, row, fieldIndex);
    }

    internal static bool CanConvert(CsvConversionResult result)
    {
        if (result.Status == CsvConversionStatus.Success || result.Status == CsvConversionStatus.Defaulted)
        {
            return true;
        }

        return false;
    }

    internal static bool UseInvalidDefault(CsvConversionResult result, CsvConversionStatus failure, CsvConversionOptions options)
    {
        if (result.Status == CsvConversionStatus.Success && options.UseDefaultForInvalid)
        {
            result.SourceStatus = failure;
            result.Status = CsvConversionStatus.Defaulted;
            result.TextValue = options.DefaultValue;
            return true;
        }

        return false;
    }

    internal static void CompleteValue(CsvConversionResult result, CsvRow row, int fieldIndex)
    {
        result.HasValue = true;
        if (result.Status == CsvConversionStatus.Defaulted)
        {
            AddDefaultDiagnostic(result, row, fieldIndex);
        }
    }

    internal static void CompleteFailure(CsvConversionResult result, CsvConversionStatus failure, CsvRow row, int fieldIndex)
    {
        result.Status = failure;
        result.HasValue = false;
        if (result.SourceStatus == CsvConversionStatus.Success)
        {
            result.SourceStatus = failure;
        }

        AddFailureDiagnostic(result, failure, row, fieldIndex);
    }

    internal static void AddStateDiagnostic(CsvConversionResult result, CsvConversionStatus status, CsvRow row, int fieldIndex)
    {
        string code = "ConversionNull";
        string message = "Field contains a null value.";
        if (status == CsvConversionStatus.Missing)
        {
            code = "ConversionMissing";
            message = "Field is missing from the row.";
        }
        else if (status == CsvConversionStatus.Empty)
        {
            code = "ConversionEmpty";
            message = "Field contains empty text.";
        }

        CsvDiagnostic diagnostic = CreateDiagnostic(code, message, row, fieldIndex);
        diagnostic.IsError = false;
        result.Diagnostics.Items.Add(diagnostic);
    }

    internal static void AddDefaultDiagnostic(CsvConversionResult result, CsvRow row, int fieldIndex)
    {
        string message = "Configured default was converted for the field input.";
        CsvDiagnostic diagnostic = CreateDiagnostic("ConversionDefaulted", message, row, fieldIndex);
        diagnostic.IsError = false;
        result.Diagnostics.Items.Add(diagnostic);
    }

    internal static void AddFailureDiagnostic(CsvConversionResult result, CsvConversionStatus failure, CsvRow row, int fieldIndex)
    {
        string code = "ConversionInvalid";
        string message = "Field text is invalid for the requested target type.";
        if (failure == CsvConversionStatus.OutOfRange)
        {
            code = "ConversionOutOfRange";
            message = "Field value is outside the requested target type range.";
        }

        CsvDiagnostic diagnostic = CreateDiagnostic(code, message, row, fieldIndex);
        result.Diagnostics.Items.Add(diagnostic);
    }

    internal static CsvDiagnostic CreateDiagnostic(string code, string message, CsvRow row, int fieldIndex)
    {
        int column = 0;
        if (fieldIndex >= 0 && fieldIndex < 2147483647)
        {
            column = fieldIndex + 1;
        }
        else if (fieldIndex == 2147483647)
        {
            column = 2147483647;
        }

        CsvDiagnostic diagnostic = CsvDiagnostic.CreateConverterError(code, message, row.RowNumber, column, row.Line, row.CharacterOffset, row.RawText);
        diagnostic.ByteOffset = row.ByteOffset;
        return diagnostic;
    }

    internal static CsvConversionOptions CreateSafeOptions(CsvConversionOptions options)
    {
        CsvConversionOptions safeOptions = options.Clone();
        safeOptions.UseDefaultForNull = false;
        safeOptions.UseDefaultForInvalid = false;
        return safeOptions;
    }

    internal static bool IsDecimalInRange(string value)
    {
        string unsigned = value;
        if (unsigned[0] == '-' || unsigned[0] == '+')
        {
            unsigned = unsigned.Substring(1);
        }

        int point = FindDecimalPoint(unsigned);
        string integerPart = unsigned;
        string fractionalPart = "";
        if (point >= 0)
        {
            integerPart = unsigned.Substring(0, point);
            fractionalPart = unsigned.Substring(point + 1);
        }

        if (integerPart.Length == 0)
        {
            integerPart = "0";
        }

        integerPart = CsvConverter.TrimIntegerLeadingZeros(integerPart);
        fractionalPart = TrimFractionalZeros(fractionalPart);
        if (fractionalPart.Length > 28)
        {
            return false;
        }

        if (integerPart.Length < 29)
        {
            return true;
        }

        if (integerPart.Length > 29)
        {
            return false;
        }

        string maximum = "79228162514264337593543950335";
        int comparison = string.CompareOrdinal(integerPart, maximum);
        if (comparison < 0)
        {
            return true;
        }

        if (comparison > 0)
        {
            return false;
        }

        return fractionalPart.Length == 0;
    }

    internal static int FindDecimalPoint(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '.')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string TrimFractionalZeros(string value)
    {
        int length = value.Length;
        while (length > 0 && value[length - 1] == '0')
        {
            length = length - 1;
        }

        return value.Substring(0, length);
    }

    internal static bool IsDoubleExponentInRange(string value)
    {
        int exponentIndex = CsvConverter.FindExponentIndex(value);
        if (exponentIndex < 0)
        {
            return true;
        }

        string exponent = value.Substring(exponentIndex + 1);
        if (exponent[0] == '-' || exponent[0] == '+')
        {
            exponent = exponent.Substring(1);
        }

        exponent = CsvConverter.TrimIntegerLeadingZeros(exponent);
        if (exponent.Length > 4)
        {
            return false;
        }

        return true;
    }

    internal static bool HasDateTimeShape(string value, CsvConversionOptions options)
    {
        if (HasIsoDateShape(value) || HasIsoDateTimeShape(value))
        {
            return true;
        }

        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            if (format == "yyyyMMdd" && HasCompactDateShape(value))
            {
                return true;
            }

            if (format == "dd/MM/yyyy" && HasSlashDateShape(value))
            {
                return true;
            }

            if (format == "yyyy-MM-dd" && HasIsoDateShape(value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasIsoDateShape(string value)
    {
        if (value.Length != 10 || value[4] != '-' || value[7] != '-')
        {
            return false;
        }

        return HasDigitsExceptSeparators(value, 4, 7, -1, -1, -1);
    }

    internal static bool HasIsoDateTimeShape(string value)
    {
        if (value.Length != 19)
        {
            return false;
        }

        if (value[4] != '-' || value[7] != '-' || (value[10] != 'T' && value[10] != ' ') || value[13] != ':' || value[16] != ':')
        {
            return false;
        }

        return HasDigitsExceptSeparators(value, 4, 7, 10, 13, 16);
    }

    internal static bool HasCompactDateShape(string value)
    {
        if (value.Length != 8)
        {
            return false;
        }

        return HasDigitsExceptSeparators(value, -1, -1, -1, -1, -1);
    }

    internal static bool HasSlashDateShape(string value)
    {
        if (value.Length != 10 || value[2] != '/' || value[5] != '/')
        {
            return false;
        }

        return HasDigitsExceptSeparators(value, 2, 5, -1, -1, -1);
    }

    internal static bool HasDigitsExceptSeparators(string value, int first, int second, int third, int fourth, int fifth)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (index != first && index != second && index != third && index != fourth && index != fifth && !CsvConverter.IsDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasTimeShape(string value, CsvConversionOptions options)
    {
        if (HasClockShape(value))
        {
            return true;
        }

        int index = 0;
        while (index < options.DateTimeFormats.Length)
        {
            string format = options.DateTimeFormats[index];
            if ((format == "HH:mm" || format == "HH:mm:ss") && HasClockShape(value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasClockShape(string value)
    {
        if (value.Length != 5 && value.Length != 8)
        {
            return false;
        }

        if (value[2] != ':')
        {
            return false;
        }

        if (value.Length == 8 && value[5] != ':')
        {
            return false;
        }

        return HasDigitsExceptSeparators(value, 2, 5, -1, -1, -1);
    }

    internal static bool IsMinimumDateTime(string value)
    {
        if (value == "0001-01-01" || value == "0001-01-01T00:00:00" || value == "0001-01-01 00:00:00" || value == "00010101" || value == "01/01/0001")
        {
            return true;
        }

        return false;
    }

    internal static bool IsMidnight(string value)
    {
        if (value == "00:00" || value == "00:00:00")
        {
            return true;
        }

        return false;
    }
}
