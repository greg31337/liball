namespace yaml;

/// <summary>
/// Exposes standard YAML tag names and resolves scalar tags under a selected schema.
/// </summary>
internal static class YamlScalarResolver
{
    /// <summary>
    /// The expanded standard YAML tag URI for failsafe string values.
    /// </summary>
    internal const string FailsafeStringTag = YamlTags.String;
    /// <summary>
    /// The expanded standard YAML tag URI for null values.
    /// </summary>
    internal const string NullTag = YamlTags.Null;
    /// <summary>
    /// The expanded standard YAML tag URI for bool values.
    /// </summary>
    internal const string BoolTag = YamlTags.Bool;
    /// <summary>
    /// The expanded standard YAML tag URI for int values.
    /// </summary>
    internal const string IntTag = YamlTags.Int;
    /// <summary>
    /// The expanded standard YAML tag URI for float values.
    /// </summary>
    internal const string FloatTag = YamlTags.Float;
    /// <summary>
    /// The expanded standard YAML tag URI for sequence values.
    /// </summary>
    internal const string SequenceTag = YamlTags.Sequence;
    /// <summary>
    /// The expanded standard YAML tag URI for mapping values.
    /// </summary>
    internal const string MappingTag = YamlTags.Mapping;
    /// <summary>
    /// The expanded standard YAML tag URI for timestamp values.
    /// </summary>
    internal const string TimestampTag = YamlTags.Timestamp;
    /// <summary>
    /// The expanded standard YAML tag URI for binary values.
    /// </summary>
    internal const string BinaryTag = YamlTags.Binary;
    /// <summary>
    /// The expanded standard YAML tag URI for set values.
    /// </summary>
    internal const string SetTag = YamlTags.Set;
    /// <summary>
    /// The expanded standard YAML tag URI for ordered map values.
    /// </summary>
    internal const string OrderedMapTag = YamlTags.OrderedMap;
    /// <summary>
    /// The expanded standard YAML tag URI for pairs values.
    /// </summary>
    internal const string PairsTag = YamlTags.Pairs;
    /// <summary>
    /// The expanded standard YAML tag URI for merge values.
    /// </summary>
    internal const string MergeTag = YamlTags.Merge;

    /// <summary>
    /// Returns the tag selected for scalar text by custom registrations and the configured schema.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string ResolveScalarTag(string value, YamlScalarStyle style, YamlSchemaOptions? options)
    {
        if (!ShouldResolveScalars(options))
        {
            return "";
        }

        if (UsesFailsafeSchema(options))
        {
            return FailsafeStringTag;
        }

        string customTag = ResolveCustomScalarTag(value, style, options);
        if (customTag != "")
        {
            return customTag;
        }

        if (options != null && options.AllowTimestamp && IsTimestamp(value, style))
        {
            return TimestampTag;
        }

        if (UsesJsonSchema(options))
        {
            return ResolveJsonScalarTag(value, style);
        }

        if (UsesYaml11Schema(options))
        {
            return ResolveYaml11ScalarTag(value, style);
        }

        if (UsesCoreSchema(options))
        {
            return ResolveCoreScalarTag(value, style, options);
        }

        return FailsafeStringTag;
    }

    internal static string ResolveJsonScalarTag(string value, YamlScalarStyle style)
    {
        string text = value == null ? "" : value;
        if (style != YamlScalarStyle.Plain && style != YamlScalarStyle.Any)
        {
            return FailsafeStringTag;
        }

        if (text == "" || text == "null")
        {
            return NullTag;
        }

        if (text == "true" || text == "false")
        {
            return BoolTag;
        }

        if (IsJsonInteger(text))
        {
            return IntTag;
        }

        if (IsJsonFloat(text))
        {
            return FloatTag;
        }

        return FailsafeStringTag;
    }

    internal static string ResolveCoreScalarTag(string value, YamlScalarStyle style)
    {
        return ResolveCoreScalarTag(value, style, null);
    }

    internal static string ResolveCoreScalarTag(string value, YamlScalarStyle style, YamlSchemaOptions? options)
    {
        string text = value == null ? "" : value;
        if (style != YamlScalarStyle.Plain && style != YamlScalarStyle.Any)
        {
            return FailsafeStringTag;
        }

        if (IsCoreNull(text))
        {
            return NullTag;
        }

        if (IsCoreBool(text))
        {
            return BoolTag;
        }

        if (options != null && options.AllowYaml11Booleans && IsYaml11Bool(text))
        {
            return BoolTag;
        }

        if (IsCoreInteger(text))
        {
            return IntTag;
        }

        if (IsCoreFloat(text))
        {
            return FloatTag;
        }

        return FailsafeStringTag;
    }

    internal static string ResolveYaml11ScalarTag(string value, YamlScalarStyle style)
    {
        string text = value == null ? "" : value;
        if (style != YamlScalarStyle.Plain && style != YamlScalarStyle.Any)
        {
            return FailsafeStringTag;
        }

        if (IsYaml11Null(text))
        {
            return NullTag;
        }

        if (IsYaml11Bool(text))
        {
            return BoolTag;
        }

        if (IsYaml11Integer(text))
        {
            return IntTag;
        }

        if (IsYaml11Float(text))
        {
            return FloatTag;
        }

        return FailsafeStringTag;
    }

    internal static bool ShouldResolveScalars(YamlSchemaOptions? options)
    {
        return options == null || options.ResolveScalars;
    }

    internal static string ResolveCustomScalarTag(string value, YamlScalarStyle style, YamlSchemaOptions? options)
    {
        if (options == null || options.CustomTags == null)
        {
            return "";
        }

        return NormalizeStandardTag(options.CustomTags.ResolveScalarTag(value, style));
    }

    /// <summary>
    /// Expands a standard double-exclamation tag such as !!str to its full YAML tag URI.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string NormalizeStandardTag(string? tag)
    {
        if (tag == null || tag == "")
        {
            return "";
        }

        if (tag == "!!null" || tag == NullTag)
        {
            return NullTag;
        }

        if (tag == "!!bool" || tag == BoolTag)
        {
            return BoolTag;
        }

        if (tag == "!!int" || tag == IntTag)
        {
            return IntTag;
        }

        if (tag == "!!float" || tag == FloatTag)
        {
            return FloatTag;
        }

        if (tag == "!!str" || tag == FailsafeStringTag)
        {
            return FailsafeStringTag;
        }

        if (tag == "!!seq" || tag == SequenceTag)
        {
            return SequenceTag;
        }

        if (tag == "!!map" || tag == MappingTag)
        {
            return MappingTag;
        }

        if (tag == "!!timestamp" || tag == TimestampTag)
        {
            return TimestampTag;
        }

        if (tag == "!!binary" || tag == BinaryTag)
        {
            return BinaryTag;
        }

        if (tag == "!!set" || tag == SetTag)
        {
            return SetTag;
        }

        if (tag == "!!omap" || tag == OrderedMapTag)
        {
            return OrderedMapTag;
        }

        if (tag == "!!pairs" || tag == PairsTag)
        {
            return PairsTag;
        }

        if (tag == "!!merge" || tag == MergeTag)
        {
            return MergeTag;
        }

        return tag;
    }

    /// <summary>
    /// Determines whether a shorthand or expanded tag names a standard YAML type supported by the library.
    /// </summary>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal static bool IsStandardTag(string? tag)
    {
        string normalized = NormalizeStandardTag(tag);
        return normalized == NullTag
            || normalized == BoolTag
            || normalized == IntTag
            || normalized == FloatTag
            || normalized == FailsafeStringTag
            || normalized == SequenceTag
            || normalized == MappingTag
            || normalized == TimestampTag
            || normalized == BinaryTag
            || normalized == SetTag
            || normalized == OrderedMapTag
            || normalized == PairsTag
            || normalized == MergeTag;
    }

    internal static bool UsesFailsafeSchema(YamlSchemaOptions? options)
    {
        return options != null && options.Schema == YamlSchemaKind.Failsafe;
    }

    internal static bool UsesJsonSchema(YamlSchemaOptions? options)
    {
        return options != null && options.Schema == YamlSchemaKind.Json;
    }

    internal static bool UsesCoreSchema(YamlSchemaOptions? options)
    {
        return options == null || options.Schema == YamlSchemaKind.Core;
    }

    internal static bool UsesYaml11Schema(YamlSchemaOptions? options)
    {
        return options != null && options.Schema == YamlSchemaKind.Yaml11;
    }

    internal static bool IsCoreNull(string value)
    {
        return value == ""
            || value == "~"
            || value == "null"
            || value == "Null"
            || value == "NULL";
    }

    internal static bool IsCoreBool(string value)
    {
        return value == "true"
            || value == "True"
            || value == "TRUE"
            || value == "false"
            || value == "False"
            || value == "FALSE";
    }

    internal static bool IsYaml11Null(string value)
    {
        return IsCoreNull(value);
    }

    internal static bool IsYaml11Bool(string value)
    {
        return value == "y"
            || value == "Y"
            || value == "yes"
            || value == "Yes"
            || value == "YES"
            || value == "n"
            || value == "N"
            || value == "no"
            || value == "No"
            || value == "NO"
            || value == "true"
            || value == "True"
            || value == "TRUE"
            || value == "false"
            || value == "False"
            || value == "FALSE"
            || value == "on"
            || value == "On"
            || value == "ON"
            || value == "off"
            || value == "Off"
            || value == "OFF";
    }

    internal static bool IsYaml11Integer(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        int index = SkipSign(value, 0);
        if (index >= value.Length)
        {
            return false;
        }

        if (StartsWithAt(value, index, "0b") || StartsWithAt(value, index, "0B"))
        {
            return HasDigitsForBase(value, index + 2, 2);
        }

        if (StartsWithAt(value, index, "0x") || StartsWithAt(value, index, "0X"))
        {
            return HasDigitsForBase(value, index + 2, 16);
        }

        return HasDigitsForBase(value, index, 10);
    }

    internal static bool IsYaml11Float(string value)
    {
        if (IsCoreInfinity(value) || IsCoreNan(value))
        {
            return true;
        }

        return IsCoreDecimalFloat(value == null ? "" : value.Replace("_", ""));
    }

    internal static bool IsTimestamp(string value, YamlScalarStyle style)
    {
        if (style != YamlScalarStyle.Plain && style != YamlScalarStyle.Any)
        {
            return false;
        }

        if (value == null || value.Length < 10)
        {
            return false;
        }

        if (!IsTimestampDate(value, 0))
        {
            return false;
        }

        if (value.Length == 10)
        {
            return true;
        }

        if (value[10] != 'T' && value[10] != 't' && value[10] != ' ')
        {
            return false;
        }

        return IsTimestampTime(value, 11);
    }

    internal static bool IsTimestampDate(string value, int start)
    {
        if (value == null || start < 0 || start > value.Length - 10)
        {
            return false;
        }

        if (!IsDigit(value[start])
            || !IsDigit(value[start + 1])
            || !IsDigit(value[start + 2])
            || !IsDigit(value[start + 3])
            || value[start + 4] != '-'
            || !IsDigit(value[start + 5])
            || !IsDigit(value[start + 6])
            || value[start + 7] != '-'
            || !IsDigit(value[start + 8])
            || !IsDigit(value[start + 9]))
        {
            return false;
        }

        int year = ParseFourDigits(value, start);
        int month = ParseTwoDigits(value, start + 5);
        int day = ParseTwoDigits(value, start + 8);
        if (month < 1 || month > 12)
        {
            return false;
        }

        int maxDay = DaysInMonth(year, month);
        return day >= 1 && day <= maxDay;
    }

    internal static bool IsTimestampTime(string value, int start)
    {
        if (value == null || start < 0 || start > value.Length - 8)
        {
            return false;
        }

        if (!IsDigit(value[start])
            || !IsDigit(value[start + 1])
            || value[start + 2] != ':'
            || !IsDigit(value[start + 3])
            || !IsDigit(value[start + 4])
            || value[start + 5] != ':'
            || !IsDigit(value[start + 6])
            || !IsDigit(value[start + 7]))
        {
            return false;
        }

        int hour = ParseTwoDigits(value, start);
        int minute = ParseTwoDigits(value, start + 3);
        int second = ParseTwoDigits(value, start + 6);
        if (hour > 23 || minute > 59 || second > 59)
        {
            return false;
        }

        int index = start + 8;
        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            int firstDigit = index;
            while (index < value.Length && IsDigit(value[index]))
            {
                index = index + 1;
            }

            if (index == firstDigit)
            {
                return false;
            }
        }

        if (index == value.Length)
        {
            return true;
        }

        if ((value[index] == 'Z' || value[index] == 'z') && index + 1 == value.Length)
        {
            return true;
        }

        if (value[index] == '+' || value[index] == '-')
        {
            return IsTimestampOffset(value, index + 1);
        }

        return false;
    }

    internal static bool IsTimestampOffset(string value, int start)
    {
        if (value == null || start < 0 || start > value.Length || value.Length - start != 5)
        {
            return false;
        }

        if (!IsDigit(value[start])
            || !IsDigit(value[start + 1])
            || value[start + 2] != ':'
            || !IsDigit(value[start + 3])
            || !IsDigit(value[start + 4]))
        {
            return false;
        }

        int hour = ParseTwoDigits(value, start);
        int minute = ParseTwoDigits(value, start + 3);
        return hour <= 23 && minute <= 59;
    }

    internal static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            return IsLeapYear(year) ? 29 : 28;
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

        return year % 4 == 0;
    }

    internal static int ParseTwoDigits(string value, int start)
    {
        return (value[start] - '0') * 10 + value[start + 1] - '0';
    }

    internal static int ParseFourDigits(string value, int start)
    {
        return (value[start] - '0') * 1000
            + (value[start + 1] - '0') * 100
            + (value[start + 2] - '0') * 10
            + value[start + 3] - '0';
    }

    internal static bool IsCoreInteger(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        int index = 0;
        if (value[index] == '+' || value[index] == '-')
        {
            index = index + 1;
        }

        if (index >= value.Length)
        {
            return false;
        }

        if (StartsWithAt(value, index, "0x") || StartsWithAt(value, index, "0X"))
        {
            index = index + 2;
            if (index >= value.Length)
            {
                return false;
            }

            while (index < value.Length)
            {
                if (!IsHexadecimalDigit(value[index]))
                {
                    return false;
                }

                index = index + 1;
            }

            return true;
        }

        if (StartsWithAt(value, index, "0o") || StartsWithAt(value, index, "0O"))
        {
            index = index + 2;
            if (index >= value.Length)
            {
                return false;
            }

            while (index < value.Length)
            {
                if (value[index] < '0' || value[index] > '7')
                {
                    return false;
                }

                index = index + 1;
            }

            return true;
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

    internal static bool IsHexadecimalDigit(char value)
    {
        return IsDigit(value)
            || value >= 'a' && value <= 'f'
            || value >= 'A' && value <= 'F';
    }

    internal static bool IsCoreFloat(string value)
    {
        if (IsCoreInfinity(value) || IsCoreNan(value))
        {
            return true;
        }

        return IsCoreDecimalFloat(value);
    }

    internal static bool IsCoreInfinity(string value)
    {
        return value == ".inf"
            || value == ".Inf"
            || value == ".INF"
            || value == "+.inf"
            || value == "+.Inf"
            || value == "+.INF"
            || value == "-.inf"
            || value == "-.Inf"
            || value == "-.INF";
    }

    internal static bool IsCoreNan(string value)
    {
        return value == ".nan" || value == ".NaN" || value == ".NAN";
    }

    internal static bool IsCoreDecimalFloat(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        int index = 0;
        if (value[index] == '+' || value[index] == '-')
        {
            index = index + 1;
        }

        int digitsBeforeDot = 0;
        while (index < value.Length && IsDigit(value[index]))
        {
            digitsBeforeDot = digitsBeforeDot + 1;
            index = index + 1;
        }

        bool hasDot = false;
        int digitsAfterDot = 0;
        if (index < value.Length && value[index] == '.')
        {
            hasDot = true;
            index = index + 1;
            while (index < value.Length && IsDigit(value[index]))
            {
                digitsAfterDot = digitsAfterDot + 1;
                index = index + 1;
            }
        }

        bool hasExponent = false;
        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            hasExponent = true;
            index = index + 1;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                index = index + 1;
            }

            int exponentDigits = 0;
            while (index < value.Length && IsDigit(value[index]))
            {
                exponentDigits = exponentDigits + 1;
                index = index + 1;
            }

            if (exponentDigits == 0)
            {
                return false;
            }
        }

        if (index != value.Length)
        {
            return false;
        }

        if (digitsBeforeDot == 0 && digitsAfterDot == 0)
        {
            return false;
        }

        return hasDot || hasExponent;
    }

    internal static bool IsJsonInteger(string value)
    {
        if (value == null)
        {
            return false;
        }

        int end = ScanJsonIntegerPart(value, 0);
        return end == value.Length;
    }

    internal static bool IsJsonFloat(string value)
    {
        int index = ScanJsonIntegerPart(value, 0);
        if (index < 0)
        {
            return false;
        }

        bool hasFloatPart = false;
        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            int firstDigit = index;
            while (index < value.Length && IsDigit(value[index]))
            {
                index = index + 1;
            }

            if (index == firstDigit)
            {
                return false;
            }

            hasFloatPart = true;
        }

        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            index = index + 1;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                index = index + 1;
            }

            int firstExponentDigit = index;
            while (index < value.Length && IsDigit(value[index]))
            {
                index = index + 1;
            }

            if (index == firstExponentDigit)
            {
                return false;
            }

            hasFloatPart = true;
        }

        return hasFloatPart && index == value.Length;
    }

    internal static int ScanJsonIntegerPart(string value, int start)
    {
        if (value == null || value == "")
        {
            return -1;
        }

        int index = start;
        if (index < value.Length && value[index] == '-')
        {
            index = index + 1;
        }

        if (index >= value.Length)
        {
            return -1;
        }

        if (value[index] == '0')
        {
            return index + 1;
        }

        if (!IsDigitOneToNine(value[index]))
        {
            return -1;
        }

        index = index + 1;
        while (index < value.Length && IsDigit(value[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static int SkipSign(string value, int start)
    {
        int index = start;
        if (index < value.Length && (value[index] == '+' || value[index] == '-'))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool StartsWithAt(string value, int index, string prefix)
    {
        if (value == null || prefix == null)
        {
            return false;
        }

        if (index < 0 || index > value.Length || prefix.Length > value.Length - index)
        {
            return false;
        }

        int offset = 0;
        while (offset < prefix.Length)
        {
            if (value[index + offset] != prefix[offset])
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static bool HasDigitsForBase(string value, int start, int numberBase)
    {
        if (start >= value.Length)
        {
            return false;
        }

        bool hasDigit = false;
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == '_')
            {
                index = index + 1;
                continue;
            }

            if (!IsDigitForBase(value[index], numberBase))
            {
                return false;
            }

            hasDigit = true;
            index = index + 1;
        }

        return hasDigit;
    }

    internal static bool IsDigitForBase(char value, int numberBase)
    {
        if (numberBase == 2)
        {
            return value == '0' || value == '1';
        }

        if (numberBase == 10)
        {
            return IsDigit(value);
        }

        if (numberBase == 16)
        {
            return IsDigit(value)
                || (value >= 'a' && value <= 'f')
                || (value >= 'A' && value <= 'F');
        }

        return false;
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsDigitOneToNine(char value)
    {
        return value >= '1' && value <= '9';
    }
}
