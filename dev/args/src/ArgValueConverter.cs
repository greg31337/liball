using System.Globalization;
using System.Collections.Generic;
using System.IO;

namespace args;

internal class ArgValueConverter
{
    internal Dictionary<string, System.Func<string, ArgConvertResult?>> CustomConverters;
    internal CultureInfo Culture;

    internal ArgValueConverter()
    {
        CustomConverters = new Dictionary<string, System.Func<string, ArgConvertResult?>>();
        Culture = CultureInfo.InvariantCulture;
    }

    internal ArgValueConverter UseCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new System.ArgumentNullException("culture");
        }

        Culture = culture;
        return this;
    }

    internal ArgValueConverter Register(string typeName, System.Func<string, ArgConvertResult?> converter)
    {
        if (typeName == null)
        {
            throw new System.ArgumentNullException("typeName");
        }

        if (converter == null)
        {
            throw new System.ArgumentNullException("converter");
        }

        CustomConverters[typeName] = converter;
        return this;
    }

    internal bool HasCustom(string typeName)
    {
        return CustomConverters.ContainsKey(typeName);
    }

    internal ArgConvertResult ConvertCustom(string typeName, string value)
    {
        if (!CustomConverters.ContainsKey(typeName))
        {
            return Failure(typeName, value);
        }

        ArgConvertResult? result = CustomConverters[typeName](value);
        return NormalizeCustomResult(result, typeName, value);
    }

    internal ArgConvertResult NormalizeCustomResult(ArgConvertResult? result, string typeName, string value)
    {
        if (result == null)
        {
            return Failure(typeName, value);
        }

        if (result.ExpectedType.Length == 0)
        {
            result.ExpectedType = typeName;
        }

        if (result.ActualValue.Length == 0)
        {
            result.ActualValue = value;
        }

        if (!result.Success && result.Error.Length == 0)
        {
            result.Error = "Expected " + result.ExpectedType + " value but got '" + result.ActualValue + "'.";
        }

        return result;
    }

    internal ArgConvertResult Success(object value, string expectedType, string actualValue)
    {
        return ArgConvertResult.Detailed(true, value, "", expectedType, actualValue);
    }

    internal ArgConvertResult Failure(string expectedType, string actualValue)
    {
        string error = "Expected " + expectedType + " value but got '" + actualValue + "'.";
        return ArgConvertResult.Detailed(false, null, error, expectedType, actualValue);
    }

    internal ArgConvertResult ConvertBool(string value)
    {
        if (value == "1")
        {
            return Success(true, "bool", value);
        }

        if (value == "0")
        {
            return Success(false, "bool", value);
        }

        if (string.Equals(value, "yes", System.StringComparison.OrdinalIgnoreCase))
        {
            return Success(true, "bool", value);
        }

        if (string.Equals(value, "no", System.StringComparison.OrdinalIgnoreCase))
        {
            return Success(false, "bool", value);
        }

        if (string.Equals(value, "on", System.StringComparison.OrdinalIgnoreCase))
        {
            return Success(true, "bool", value);
        }

        if (string.Equals(value, "off", System.StringComparison.OrdinalIgnoreCase))
        {
            return Success(false, "bool", value);
        }

        bool result = false;
        if (bool.TryParse(value, out result))
        {
            return Success(result, "bool", value);
        }

        return Failure("bool", value);
    }

    internal bool Bool(string value)
    {
        ArgConvertResult conversion = ConvertBool(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (bool)conversion.Value;
        }

        return false;
    }

    internal ArgConvertResult ConvertInt(string value)
    {
        int result = 0;
        if (int.TryParse(value, NumberStyles.Integer, Culture, out result))
        {
            return Success(result, "int", value);
        }

        return Failure("int", value);
    }

    internal int Int(string value)
    {
        ArgConvertResult conversion = ConvertInt(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (int)conversion.Value;
        }

        return 0;
    }

    internal ArgConvertResult ConvertLong(string value)
    {
        long result = 0;
        if (long.TryParse(value, NumberStyles.Integer, Culture, out result))
        {
            return Success(result, "long", value);
        }

        return Failure("long", value);
    }

    internal long Long(string value)
    {
        ArgConvertResult conversion = ConvertLong(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (long)conversion.Value;
        }

        return 0;
    }

    internal ArgConvertResult ConvertDouble(string value)
    {
        double result = 0;
        if (double.TryParse(value, NumberStyles.Float, Culture, out result))
        {
            return Success(result, "double", value);
        }

        return Failure("double", value);
    }

    internal double Double(string value)
    {
        ArgConvertResult conversion = ConvertDouble(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (double)conversion.Value;
        }

        return 0;
    }

    internal ArgConvertResult ConvertDecimal(string value)
    {
        decimal result = 0;
        if (decimal.TryParse(value, NumberStyles.Number, Culture, out result))
        {
            return Success(result, "decimal", value);
        }

        return Failure("decimal", value);
    }

    internal decimal Decimal(string value)
    {
        ArgConvertResult conversion = ConvertDecimal(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (decimal)conversion.Value;
        }

        return 0;
    }

    internal ArgConvertResult ConvertDateTime(string value)
    {
        System.DateTime result = new System.DateTime();
        if (System.DateTime.TryParse(value, Culture, DateTimeStyles.None, out result))
        {
            return Success(result, "DateTime", value);
        }

        return Failure("DateTime", value);
    }

    internal System.DateTime DateTime(string value)
    {
        ArgConvertResult conversion = ConvertDateTime(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (System.DateTime)conversion.Value;
        }

        return new System.DateTime();
    }

    internal ArgConvertResult ConvertTimeSpan(string value)
    {
        System.TimeSpan result = new System.TimeSpan();
        if (System.TimeSpan.TryParse(value, Culture, out result))
        {
            return Success(result, "TimeSpan", value);
        }

        return Failure("TimeSpan", value);
    }

    internal System.TimeSpan TimeSpan(string value)
    {
        ArgConvertResult conversion = ConvertTimeSpan(value);
        if (conversion.Success && conversion.Value != null)
        {
            return (System.TimeSpan)conversion.Value;
        }

        return new System.TimeSpan();
    }

    internal ArgConvertResult ConvertFileInfo(string value)
    {
        return Success(FileInfo(value), "FileInfo", value);
    }

    internal FileInfo FileInfo(string value)
    {
        if (value.Length == 0)
        {
            return new FileInfo(".");
        }

        return new FileInfo(value);
    }

    internal ArgConvertResult ConvertDirectoryInfo(string value)
    {
        return Success(DirectoryInfo(value), "DirectoryInfo", value);
    }

    internal DirectoryInfo DirectoryInfo(string value)
    {
        if (value.Length == 0)
        {
            return new DirectoryInfo(".");
        }

        return new DirectoryInfo(value);
    }

    internal ArgConvertResult ConvertEnumValue(string value, System.Type enumType)
    {
        if (enumType == null)
        {
            throw new System.ArgumentNullException("enumType");
        }

        string expectedType = "enum " + enumType.Name;
        object? result = null;
        if (System.Enum.TryParse(enumType, value, true, out result))
        {
            return Success(result, expectedType, value);
        }

        return Failure(expectedType, value);
    }

    internal object? EnumValue(string value, System.Type enumType)
    {
        ArgConvertResult conversion = ConvertEnumValue(value, enumType);
        return conversion.Value;
    }
}
