using System;

namespace config;

internal class ConfigReader
{
    internal static ConfigReadResult ReadString(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsStringValue(value))
        {
            return Malformed(value, path, "value cannot be converted to string");
        }

        result.StringValue = ConfigConversions.StringValue(value);
        return result;
    }

    internal static ConfigReadResult ReadBool(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsBoolValue(value))
        {
            return Malformed(value, path, "value cannot be converted to Boolean");
        }

        result.BooleanValue = ConfigConversions.BoolValue(value, false);
        return result;
    }

    internal static ConfigReadResult ReadInt(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsIntValue(value))
        {
            return Malformed(value, path, "value cannot be converted to 32-bit integer");
        }

        result.IntValue = ConfigConversions.IntValue(value, 0);
        return result;
    }

    internal static ConfigReadResult ReadLong(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsLongValue(value))
        {
            return Malformed(value, path, "value cannot be converted to 64-bit integer");
        }

        result.LongValue = ConfigConversions.LongValue(value, 0L);
        return result;
    }

    internal static ConfigReadResult ReadDecimal(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsDecimalValue(value))
        {
            return Malformed(value, path, "value cannot be converted to decimal");
        }

        result.DecimalValue = ConfigConversions.DecimalValue(value, 0m);
        return result;
    }

    internal static ConfigReadResult ReadDouble(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsDoubleValue(value))
        {
            return Malformed(value, path, "value cannot be converted to double");
        }

        result.DoubleValue = ConfigConversions.DoubleValue(value, 0d);
        return result;
    }

    internal static ConfigReadResult ReadTimeSpan(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsTimeSpanValue(value))
        {
            return Malformed(value, path, "value cannot be converted to duration");
        }

        result.TimeSpanValue = ConfigConversions.TimeSpanValue(value, TimeSpan.Zero);
        return result;
    }

    internal static ConfigReadResult ReadByteSize(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsByteSizeValue(value))
        {
            return Malformed(value, path, "value cannot be converted to byte size");
        }

        result.ByteSizeValue = ConfigConversions.ByteSizeValue(value, 0L);
        return result;
    }

    internal static ConfigReadResult ReadStringArray(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsStringArrayValue(value))
        {
            return Malformed(value, path, "value cannot be converted to string array");
        }

        result.StringArrayValue = ConfigConversions.StringArrayValue(value);
        return result;
    }

    internal static ConfigReadResult ReadIntArray(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsIntArrayValue(value))
        {
            return Malformed(value, path, "value cannot be converted to 32-bit integer array");
        }

        result.IntArrayValue = ConfigConversions.IntArrayValue(value, new int[0]);
        return result;
    }

    internal static ConfigReadResult ReadLongArray(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsLongArrayValue(value))
        {
            return Malformed(value, path, "value cannot be converted to 64-bit integer array");
        }

        result.LongArrayValue = ConfigConversions.LongArrayValue(value, new long[0]);
        return result;
    }

    internal static ConfigReadResult ReadDecimalArray(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsDecimalArrayValue(value))
        {
            return Malformed(value, path, "value cannot be converted to decimal array");
        }

        result.DecimalArrayValue = ConfigConversions.DecimalArrayValue(value, new decimal[0]);
        return result;
    }

    internal static ConfigReadResult ReadDoubleArray(Config config, string path)
    {
        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsDoubleArrayValue(value))
        {
            return Malformed(value, path, "value cannot be converted to double array");
        }

        result.DoubleArrayValue = ConfigConversions.DoubleArrayValue(value, new double[0]);
        return result;
    }

    internal static ConfigReadResult ReadEnum(Config config, string path, Type enumType)
    {
        if (enumType == null)
        {
            return InvalidRequest(path, "enum type is null");
        }

        if (!enumType.IsEnum)
        {
            return InvalidRequest(path, "type is not an enum");
        }

        ConfigValue value = config.Value(path);
        ConfigReadResult result = Begin(value, path);
        if (result.Status != ConfigReadStatus.Success)
        {
            return result;
        }

        if (!ConfigConversions.IsEnumValue(value, enumType))
        {
            return Malformed(value, path, "value cannot be converted to " + enumType.FullName);
        }

        result.EnumValue = ConfigConversions.EnumValue(value, enumType, ConfigConversions.EnumDefault(enumType));
        return result;
    }

    internal static ConfigReadResult Begin(ConfigValue value, string path)
    {
        if (value.Kind == ConfigValueKind.Missing)
        {
            return Failure(ConfigReadStatus.Missing, value, path, "config value is missing");
        }

        if (value.Kind == ConfigValueKind.Null)
        {
            return Failure(ConfigReadStatus.Null, value, path, "config value is null");
        }

        return new ConfigReadResult();
    }

    internal static ConfigReadResult Malformed(ConfigValue value, string path, string message)
    {
        return Failure(ConfigReadStatus.Malformed, value, path, message);
    }

    internal static ConfigReadResult InvalidRequest(string path, string message)
    {
        return Failure(ConfigReadStatus.Malformed, ConfigValue.Missing(), path, message);
    }

    internal static ConfigReadResult Failure(ConfigReadStatus status, ConfigValue value, string path, string message)
    {
        ConfigReadResult result = new ConfigReadResult();
        string sourceName = "";
        if (value.SourceName != null)
        {
            sourceName = value.SourceName;
        }

        result.Status = status;
        result.Diagnostic = new ConfigError(sourceName, path, message, value.LineNumber, value.ColumnNumber);
        return result;
    }
}
