using System;
using System.Collections.Generic;
using System.Globalization;

namespace config;

internal class ConfigConversions
{
    internal static string StringValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.String)
        {
            if (value.StringValue == null)
            {
                return "";
            }

            return value.StringValue;
        }

        if (value.Kind == ConfigValueKind.Number)
        {
            return value.NumberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return "true";
            }

            return "false";
        }

        if (value.Kind == ConfigValueKind.Array || value.Kind == ConfigValueKind.Object)
        {
            return value.ToDisplayString();
        }

        return "";
    }

    internal static bool BoolValue(ConfigValue value, bool fallback)
    {
        if (value.Kind == ConfigValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.Kind == ConfigValueKind.Number)
        {
            return value.NumberValue != 0m;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            string text = value.StringValue.Trim().ToLowerInvariant();
            if (text == "true" || text == "1" || text == "yes" || text == "y" || text == "on")
            {
                return true;
            }

            if (text == "false" || text == "0" || text == "no" || text == "n" || text == "off")
            {
                return false;
            }
        }

        return fallback;
    }

    internal static int IntValue(ConfigValue value, int fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            if (IsWholeDecimal(value.NumberValue) && value.NumberValue >= int.MinValue && value.NumberValue <= int.MaxValue)
            {
                return (int)value.NumberValue;
            }
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            int parsed = 0;
            if (int.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    internal static long LongValue(ConfigValue value, long fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            if (IsWholeDecimal(value.NumberValue) && value.NumberValue >= long.MinValue && value.NumberValue <= long.MaxValue)
            {
                return (long)value.NumberValue;
            }
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            long parsed = 0L;
            if (long.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    internal static decimal DecimalValue(ConfigValue value, decimal fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return value.NumberValue;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            decimal parsed = 0m;
            if (decimal.TryParse(value.StringValue.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return 1m;
            }

            return 0m;
        }

        return fallback;
    }

    internal static double DoubleValue(ConfigValue value, double fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return decimal.ToDouble(value.NumberValue);
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            double parsed = 0d;
            if (double.TryParse(value.StringValue.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
        }

        if (value.Kind == ConfigValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return 1d;
            }

            return 0d;
        }

        return fallback;
    }

    internal static TimeSpan TimeSpanValue(ConfigValue value, TimeSpan fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return TimeSpanFromDecimalMilliseconds(value.NumberValue, fallback);
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            TimeSpan parsed = TimeSpan.Zero;
            string text = value.StringValue.Trim();
            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return DurationWithSuffix(text, fallback);
        }

        return fallback;
    }

    internal static long ByteSizeValue(ConfigValue value, long fallback)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return LongFromDecimal(value.NumberValue, fallback);
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            return ByteSizeText(value.StringValue.Trim(), fallback);
        }

        return fallback;
    }

    internal static string[] StringArrayValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            string[] result = new string[value.ArrayValue.Count()];

            int index = 0;
            while (index < value.ArrayValue.Count())
            {
                result[index] = StringValue(value.ArrayValue.Get(index));
                index = index + 1;
            }

            return result;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            return SplitStringArray(value.StringValue);
        }

        if (value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean)
        {
            return new string[] { StringValue(value) };
        }

        return new string[0];
    }

    internal static int[] IntArrayValue(ConfigValue value, int[] fallback)
    {
        if (!IsNumericArrayInput(value))
        {
            return CopyIntArray(fallback);
        }

        ConfigArray items = NumericItems(value);
        int[] result = new int[items.Count()];
        int index = 0;
        while (index < items.Count())
        {
            ConfigValue item = items.Get(index);
            if (!IsIntValue(item))
            {
                return CopyIntArray(fallback);
            }

            result[index] = IntValue(item, 0);
            index = index + 1;
        }

        return result;
    }

    internal static long[] LongArrayValue(ConfigValue value, long[] fallback)
    {
        if (!IsNumericArrayInput(value))
        {
            return CopyLongArray(fallback);
        }

        ConfigArray items = NumericItems(value);
        long[] result = new long[items.Count()];
        int index = 0;
        while (index < items.Count())
        {
            ConfigValue item = items.Get(index);
            if (!IsLongValue(item))
            {
                return CopyLongArray(fallback);
            }

            result[index] = LongValue(item, 0L);
            index = index + 1;
        }

        return result;
    }

    internal static decimal[] DecimalArrayValue(ConfigValue value, decimal[] fallback)
    {
        if (!IsNumericArrayInput(value))
        {
            return CopyDecimalArray(fallback);
        }

        ConfigArray items = NumericItems(value);
        decimal[] result = new decimal[items.Count()];
        int index = 0;
        while (index < items.Count())
        {
            ConfigValue item = items.Get(index);
            if (!IsDecimalValue(item))
            {
                return CopyDecimalArray(fallback);
            }

            result[index] = DecimalValue(item, 0m);
            index = index + 1;
        }

        return result;
    }

    internal static double[] DoubleArrayValue(ConfigValue value, double[] fallback)
    {
        if (!IsNumericArrayInput(value))
        {
            return CopyDoubleArray(fallback);
        }

        ConfigArray items = NumericItems(value);
        double[] result = new double[items.Count()];
        int index = 0;
        while (index < items.Count())
        {
            ConfigValue item = items.Get(index);
            if (!IsDoubleValue(item))
            {
                return CopyDoubleArray(fallback);
            }

            result[index] = DoubleValue(item, 0d);
            index = index + 1;
        }

        return result;
    }

    internal static object EnumValue(ConfigValue value, Type enumType, object fallback)
    {
        if (value.Kind != ConfigValueKind.String || value.StringValue == null)
        {
            return fallback;
        }

        string text = value.StringValue.Trim();
        string[] names = Enum.GetNames(enumType);
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(names[index], text, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse(enumType, names[index]);
            }

            index = index + 1;
        }

        return fallback;
    }

    internal static object EnumDefault(Type enumType)
    {
        return Enum.ToObject(enumType, 0);
    }

    internal static bool IsNumericArrayInput(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            return true;
        }

        return value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean;
    }

    internal static ConfigArray NumericItems(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            return value.ArrayValue.Copy();
        }

        ConfigArray items = new ConfigArray();
        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            string[] parts = SplitStringArray(value.StringValue);
            int index = 0;
            while (index < parts.Length)
            {
                items.Add(ConfigValue.String(parts[index]));
                index = index + 1;
            }

            return items;
        }

        if (value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean)
        {
            items.Add(value.Copy());
        }

        return items;
    }

    internal static bool IsIntValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return IsWholeDecimal(value.NumberValue) && value.NumberValue >= int.MinValue && value.NumberValue <= int.MaxValue;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            int parsed = 0;
            return int.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool IsLongValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number)
        {
            return IsWholeDecimal(value.NumberValue) && value.NumberValue >= long.MinValue && value.NumberValue <= long.MaxValue;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            long parsed = 0L;
            return long.TryParse(value.StringValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool IsDecimalValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            decimal parsed = 0m;
            return decimal.TryParse(value.StringValue.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool IsDoubleValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            double parsed = 0d;
            return double.TryParse(value.StringValue.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed);
        }

        return false;
    }

    internal static bool IsStringValue(ConfigValue value)
    {
        return value.Kind != ConfigValueKind.Missing && value.Kind != ConfigValueKind.Null;
    }

    internal static bool IsBoolValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Boolean || value.Kind == ConfigValueKind.Number)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            string text = value.StringValue.Trim().ToLowerInvariant();
            return text == "true" || text == "1" || text == "yes" || text == "y" || text == "on" || text == "false" || text == "0" || text == "no" || text == "n" || text == "off";
        }

        return false;
    }

    internal static bool IsTimeSpanValue(ConfigValue value)
    {
        TimeSpan first = TimeSpanValue(value, TimeSpan.MinValue);
        TimeSpan second = TimeSpanValue(value, TimeSpan.MaxValue);
        return first == second;
    }

    internal static bool IsByteSizeValue(ConfigValue value)
    {
        long first = ByteSizeValue(value, long.MinValue);
        long second = ByteSizeValue(value, long.MaxValue);
        return first == second;
    }

    internal static bool IsStringArrayValue(ConfigValue value)
    {
        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            return true;
        }

        if (value.Kind == ConfigValueKind.String && value.StringValue != null)
        {
            return true;
        }

        return value.Kind == ConfigValueKind.Number || value.Kind == ConfigValueKind.Boolean;
    }

    internal static bool IsIntArrayValue(ConfigValue value)
    {
        if (!IsNumericArrayInput(value))
        {
            return false;
        }

        ConfigArray items = NumericItems(value);
        int index = 0;
        while (index < items.Count())
        {
            if (!IsIntValue(items.Get(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsLongArrayValue(ConfigValue value)
    {
        if (!IsNumericArrayInput(value))
        {
            return false;
        }

        ConfigArray items = NumericItems(value);
        int index = 0;
        while (index < items.Count())
        {
            if (!IsLongValue(items.Get(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDecimalArrayValue(ConfigValue value)
    {
        if (!IsNumericArrayInput(value))
        {
            return false;
        }

        ConfigArray items = NumericItems(value);
        int index = 0;
        while (index < items.Count())
        {
            if (!IsDecimalValue(items.Get(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDoubleArrayValue(ConfigValue value)
    {
        if (!IsNumericArrayInput(value))
        {
            return false;
        }

        ConfigArray items = NumericItems(value);
        int index = 0;
        while (index < items.Count())
        {
            if (!IsDoubleValue(items.Get(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsEnumValue(ConfigValue value, Type enumType)
    {
        if (value.Kind != ConfigValueKind.String || value.StringValue == null)
        {
            return false;
        }

        string text = value.StringValue.Trim();
        string[] names = Enum.GetNames(enumType);
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(names[index], text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsWholeDecimal(decimal value)
    {
        return decimal.Truncate(value) == value;
    }

    internal static TimeSpan DurationWithSuffix(string text, TimeSpan fallback)
    {
        string lower = text.ToLowerInvariant();
        if (lower.EndsWith("ms", StringComparison.Ordinal))
        {
            return DurationNumber(text, 2, "ms", fallback);
        }

        if (lower.EndsWith("s", StringComparison.Ordinal))
        {
            return DurationNumber(text, 1, "s", fallback);
        }

        if (lower.EndsWith("m", StringComparison.Ordinal))
        {
            return DurationNumber(text, 1, "m", fallback);
        }

        if (lower.EndsWith("h", StringComparison.Ordinal))
        {
            return DurationNumber(text, 1, "h", fallback);
        }

        if (lower.EndsWith("d", StringComparison.Ordinal))
        {
            return DurationNumber(text, 1, "d", fallback);
        }

        return fallback;
    }

    internal static TimeSpan DurationNumber(string text, int suffixLength, string suffix, TimeSpan fallback)
    {
        string numberText = text.Substring(0, text.Length - suffixLength).Trim();
        double number = 0d;
        if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return fallback;
        }

        if (suffix == "ms")
        {
            return TimeSpanFromDouble(number, TimeSpan.TicksPerMillisecond, fallback);
        }

        if (suffix == "s")
        {
            return TimeSpanFromDouble(number, TimeSpan.TicksPerSecond, fallback);
        }

        if (suffix == "m")
        {
            return TimeSpanFromDouble(number, TimeSpan.TicksPerMinute, fallback);
        }

        if (suffix == "h")
        {
            return TimeSpanFromDouble(number, TimeSpan.TicksPerHour, fallback);
        }

        if (suffix == "d")
        {
            return TimeSpanFromDouble(number, TimeSpan.TicksPerDay, fallback);
        }

        return fallback;
    }

    internal static TimeSpan TimeSpanFromDecimalMilliseconds(decimal value, TimeSpan fallback)
    {
        decimal minimum = (decimal)long.MinValue / TimeSpan.TicksPerMillisecond;
        decimal maximum = (decimal)long.MaxValue / TimeSpan.TicksPerMillisecond;
        if (value < minimum || value > maximum)
        {
            return fallback;
        }

        decimal ticks = value * TimeSpan.TicksPerMillisecond;
        return TimeSpan.FromTicks((long)ticks);
    }

    internal static TimeSpan TimeSpanFromDouble(double value, double ticksPerUnit, TimeSpan fallback)
    {
        double ticks = value * ticksPerUnit;
        if (double.IsNaN(ticks) || double.IsInfinity(ticks))
        {
            return fallback;
        }

        if (ticks < -9223372036854775808d || ticks >= 9223372036854775808d)
        {
            return fallback;
        }

        return TimeSpan.FromTicks((long)ticks);
    }

    internal static long ByteSizeText(string text, long fallback)
    {
        string lower = text.ToLowerInvariant();
        if (lower.EndsWith("kb", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 2, 1024d, fallback);
        }

        if (lower.EndsWith("mb", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 2, 1024d * 1024d, fallback);
        }

        if (lower.EndsWith("gb", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 2, 1024d * 1024d * 1024d, fallback);
        }

        if (lower.EndsWith("tb", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 2, 1024d * 1024d * 1024d * 1024d, fallback);
        }

        if (lower.EndsWith("b", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 1, 1d, fallback);
        }

        if (lower.EndsWith("k", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 1, 1024d, fallback);
        }

        if (lower.EndsWith("m", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 1, 1024d * 1024d, fallback);
        }

        if (lower.EndsWith("g", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 1, 1024d * 1024d * 1024d, fallback);
        }

        if (lower.EndsWith("t", StringComparison.Ordinal))
        {
            return ByteSizeNumber(text, 1, 1024d * 1024d * 1024d * 1024d, fallback);
        }

        decimal decimalBytes = 0m;
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimalBytes))
        {
            return LongFromDecimal(decimalBytes, fallback);
        }

        double bytes = 0d;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out bytes))
        {
            return LongFromDouble(bytes, fallback);
        }

        return fallback;
    }

    internal static long ByteSizeNumber(string text, int suffixLength, double multiplier, long fallback)
    {
        string numberText = text.Substring(0, text.Length - suffixLength).Trim();
        double number = 0d;
        if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return fallback;
        }

        return LongFromDouble(number * multiplier, fallback);
    }

    internal static long LongFromDouble(double value, long fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        if (value < -9223372036854775808d || value >= 9223372036854775808d)
        {
            return fallback;
        }

        return (long)value;
    }

    internal static long LongFromDecimal(decimal value, long fallback)
    {
        if (value < long.MinValue || value > long.MaxValue)
        {
            return fallback;
        }

        return (long)value;
    }

    internal static string[] SplitStringArray(string text)
    {
        string[] parts = text.Split(',');
        List<string> values = new List<string>();

        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index].Trim();
            if (part.Length > 0)
            {
                values.Add(part);
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    internal static string[] CopyStringArray(string[] values)
    {
        string[] copy = new string[values.Length];

        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int[] CopyIntArray(int[] values)
    {
        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static long[] CopyLongArray(long[] values)
    {
        long[] copy = new long[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static decimal[] CopyDecimalArray(decimal[] values)
    {
        decimal[] copy = new decimal[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        double[] copy = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
