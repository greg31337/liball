using System;
using System.Collections.Generic;
using System.IO;

namespace config;

/// <summary>Provides an immutable snapshot facade for merged configuration values.</summary>
/// <remarks>Reads resolve aliases and substitutions and return copies of mutable value containers. A snapshot does not change after it is built.</remarks>
public class Config : IConfig
{
    internal ConfigValue Root;
    internal ConfigDiagnostics DiagnosticData;
    internal ConfigAliasTable Aliases;
    internal string Delimiter;
    internal ConfigCaseMode CaseMode;
    internal ConfigResolutionOptions ResolutionOptions;

    internal Config(ConfigValue root, ConfigDiagnostics diagnostics, ConfigAliasTable aliases, string delimiter, ConfigCaseMode caseMode, ConfigResolutionOptions resolutionOptions)
    {
        Root = root.Copy();
        DiagnosticData = diagnostics.Copy();
        Aliases = aliases.Copy();
        Delimiter = delimiter;
        CaseMode = caseMode;
        ResolutionOptions = resolutionOptions.Copy();
    }

    internal void AddResolutionDiagnostics()
    {
        ConfigResolver.AddDiagnostics(this, DiagnosticData);
    }

    /// <summary>Starts a builder whose sources are applied in registration order.</summary>
    /// <returns>A new builder with default merge and path options.</returns>
    internal static ConfigBuilder CreateBuilder()
    {
        return new ConfigBuilder();
    }

    /// <summary>Checks whether a path exists before substitution resolution.</summary>
    /// <param name="path">The hierarchical configuration path.</param>
    /// <returns>True for any present value, including an explicit null.</returns>
    public bool Has(string path)
    {
        ConfigValue value = RawValue(path);
        return !value.IsMissing();
    }

    /// <summary>Reads a value after resolving aliases and substitutions.</summary>
    /// <param name="path">The hierarchical configuration path.</param>
    /// <returns>A copied value, or a missing value when the path cannot be resolved.</returns>
    public ConfigValue Value(string path)
    {
        return ConfigResolver.ResolvePath(this, path);
    }

    internal ConfigValue RawValue(string path)
    {
        string normalizedPath = ConfigPath.Normalize(path, Delimiter);
        if (normalizedPath.Length == 0)
        {
            return Root.Copy();
        }

        if (Root.Kind != ConfigValueKind.Object)
        {
            return ConfigValue.Missing();
        }

        if (Root.ObjectValue == null)
        {
            return ConfigValue.Missing();
        }

        return Root.ObjectValue.GetPathWithAliases(path, Delimiter, Aliases).Copy();
    }

    /// <summary>Reads an object section after resolving substitutions.</summary>
    /// <param name="path">The object path to read.</param>
    /// <returns>A deep copy of the object, or an empty map when the path is missing or is not an object.</returns>
    public ConfigMap Section(string path)
    {
        ConfigValue value = Value(path);
        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            return value.ObjectValue.Copy();
        }

        return ConfigMap.Create(CaseMode);
    }

    /// <summary>Lists direct child keys below an object path.</summary>
    /// <param name="path">The object path, or an empty string for the root.</param>
    /// <returns>Sorted child keys, or an empty array when the path is not an object.</returns>
    public string[] Children(string path)
    {
        if (Root.Kind != ConfigValueKind.Object)
        {
            return new string[0];
        }

        if (Root.ObjectValue == null)
        {
            return new string[0];
        }

        return Root.ObjectValue.ChildKeysWithAliases(path, Delimiter, Aliases);
    }

    /// <summary>Lists the direct keys of the root object.</summary>
    /// <returns>Sorted root keys, or an empty array for a non-object root.</returns>
    public string[] Keys()
    {
        return Children("");
    }

    /// <summary>Gets diagnostics collected while loading and merging this snapshot.</summary>
    /// <returns>A copy that callers may inspect without changing the snapshot.</returns>
    public ConfigDiagnostics Diagnostics()
    {
        return DiagnosticData.Copy();
    }

    /// <summary>Serializes the complete merged snapshot as JSON.</summary>
    /// <returns>JSON text with object keys emitted in deterministic order.</returns>
    public string ToJson()
    {
        return ConfigExporter.ToJson(this);
    }

    /// <summary>Serializes the merged snapshot as JSON while masking secret-like paths.</summary>
    /// <param name="redactor">The path matcher and replacement text to apply.</param>
    /// <returns>Redacted JSON text with deterministic object-key order.</returns>
    public string ToRedactedJson(ConfigRedactor redactor)
    {
        return ConfigExporter.ToJson(this, redactor);
    }

    /// <summary>Flattens leaf values into path and text pairs.</summary>
    /// <returns>A new dictionary keyed with this snapshot's delimiter.</returns>
    public Dictionary<string, string> ToFlatMap()
    {
        return ConfigExporter.ToFlatMap(this);
    }

    /// <summary>Flattens leaf values while masking values at secret-like paths.</summary>
    /// <param name="redactor">The path matcher and replacement text to apply.</param>
    /// <returns>A new redacted dictionary keyed with this snapshot's delimiter.</returns>
    public Dictionary<string, string> ToRedactedFlatMap(ConfigRedactor redactor)
    {
        return ConfigExporter.ToFlatMap(this, redactor);
    }

    /// <summary>Writes JSON to a new file and refuses to replace an existing file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="IOException">Thrown when the destination already exists or cannot be written.</exception>
    public void WriteJson(string path)
    {
        ConfigExporter.WriteJsonFile(this, path);
    }

    /// <summary>Writes JSON to a file, replacing its previous contents when it exists.</summary>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="IOException">Thrown when the destination cannot be written.</exception>
    public void OverwriteJson(string path)
    {
        ConfigExporter.OverwriteJsonFile(this, path);
    }

    /// <summary>Writes flattened key-value text to a new file and refuses to replace an existing file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="IOException">Thrown when the destination already exists or cannot be written.</exception>
    public void WriteFlat(string path)
    {
        ConfigExporter.WriteFlatFile(this, path);
    }

    /// <summary>Writes flattened key-value text to a file, replacing its previous contents when it exists.</summary>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="IOException">Thrown when the destination cannot be written.</exception>
    public void OverwriteFlat(string path)
    {
        ConfigExporter.OverwriteFlatFile(this, path);
    }

    /// <summary>Creates an instance and fills its public fields from the root object.</summary>
    /// <param name="targetType">A bindable type with a public parameterless constructor.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the target cannot be created or a configured value cannot be converted.</exception>
    public object Bind(Type targetType)
    {
        return ConfigBinder.Bind(this, targetType);
    }

    /// <summary>Creates an instance and fills mapped public fields from the root object.</summary>
    /// <param name="targetType">A bindable type with a public parameterless constructor.</param>
    /// <param name="fieldMap">Explicit configuration paths for selected target fields.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the target cannot be created or a configured value cannot be converted.</exception>
    public object BindMapped(Type targetType, ConfigFieldMap fieldMap)
    {
        return ConfigBinder.Bind(this, targetType, fieldMap);
    }

    /// <summary>Creates an instance and fills its public fields from one configuration section.</summary>
    /// <param name="path">The section used as the binding root.</param>
    /// <param name="targetType">A bindable type with a public parameterless constructor.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the target cannot be created or a configured value cannot be converted.</exception>
    public object BindSection(string path, Type targetType)
    {
        return ConfigBinder.Bind(this, path, targetType);
    }

    /// <summary>Creates an instance and fills mapped public fields from one configuration section.</summary>
    /// <param name="path">The section used as the binding root.</param>
    /// <param name="targetType">A bindable type with a public parameterless constructor.</param>
    /// <param name="fieldMap">Explicit paths interpreted relative to the section.</param>
    /// <returns>The populated instance.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the target cannot be created or a configured value cannot be converted.</exception>
    public object BindMappedSection(string path, Type targetType, ConfigFieldMap fieldMap)
    {
        return ConfigBinder.Bind(this, path, targetType, fieldMap);
    }

    /// <summary>Fills an existing object's public fields from the root object.</summary>
    /// <param name="target">The instance to populate. Missing and null values leave existing fields unchanged.</param>
    /// <exception cref="ConfigConversionException">Thrown when a configured value cannot be converted to its field type.</exception>
    public void BindInstance(object target)
    {
        ConfigBinder.Bind(this, target);
    }

    /// <summary>Fills mapped public fields on an existing object from the root object.</summary>
    /// <param name="target">The instance to populate.</param>
    /// <param name="fieldMap">Explicit configuration paths for selected target fields.</param>
    /// <exception cref="ConfigConversionException">Thrown when a configured value cannot be converted to its field type.</exception>
    public void BindMappedInstance(object target, ConfigFieldMap fieldMap)
    {
        ConfigBinder.Bind(this, target, fieldMap);
    }

    /// <summary>Fills an existing object's public fields from one configuration section.</summary>
    /// <param name="path">The section used as the binding root.</param>
    /// <param name="target">The instance to populate.</param>
    /// <exception cref="ConfigConversionException">Thrown when a configured value cannot be converted to its field type.</exception>
    public void BindSectionInstance(string path, object target)
    {
        ConfigBinder.Bind(this, path, target);
    }

    /// <summary>Fills mapped public fields on an existing object from one configuration section.</summary>
    /// <param name="path">The section used as the binding root.</param>
    /// <param name="target">The instance to populate.</param>
    /// <param name="fieldMap">Explicit paths interpreted relative to the section.</param>
    /// <exception cref="ConfigConversionException">Thrown when a configured value cannot be converted to its field type.</exception>
    public void BindMappedSectionInstance(string path, object target, ConfigFieldMap fieldMap)
    {
        ConfigBinder.Bind(this, path, target, fieldMap);
    }

    /// <summary>Reads a required value after alias and substitution resolution.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The resolved value, including an explicit null.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the path is missing.</exception>
    public ConfigValue Require(string path)
    {
        ConfigValue value = Value(path);
        if (value.IsMissing())
        {
            throw ConfigExceptionFactory.ConversionFromValue("missing required config value: " + path, path, value);
        }

        return value;
    }

    /// <summary>Reads a value as invariant text.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted text, or an empty string when the value is missing, null, or not scalar.</returns>
    public string GetString(string path)
    {
        return ConfigConversions.StringValue(Value(path));
    }

    /// <summary>Reads a value as invariant text, using a fallback only for missing or null values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when the path is missing or null.</param>
    /// <returns>The converted text or the supplied fallback.</returns>
    public string GetStringOrDefault(string path, string fallback)
    {
        ConfigValue value = Value(path);
        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            return fallback;
        }

        return ConfigConversions.StringValue(value);
    }

    /// <summary>Reads a Boolean, accepting Boolean, numeric, and common textual forms.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted value, or false when conversion fails.</returns>
    public bool GetBool(string path)
    {
        return ConfigConversions.BoolValue(Value(path), false);
    }

    /// <summary>Reads a Boolean and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted value or the fallback.</returns>
    public bool GetBoolOrDefault(string path, bool fallback)
    {
        return ConfigConversions.BoolValue(Value(path), fallback);
    }

    /// <summary>Reads a 32-bit integer using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted value, or zero when conversion fails.</returns>
    public int GetInt(string path)
    {
        return ConfigConversions.IntValue(Value(path), 0);
    }

    /// <summary>Reads a 32-bit integer and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted value or the fallback.</returns>
    public int GetIntOrDefault(string path, int fallback)
    {
        return ConfigConversions.IntValue(Value(path), fallback);
    }

    /// <summary>Reads a 64-bit integer using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted value, or zero when conversion fails.</returns>
    public long GetLong(string path)
    {
        return ConfigConversions.LongValue(Value(path), 0L);
    }

    /// <summary>Reads a 64-bit integer and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted value or the fallback.</returns>
    public long GetLongOrDefault(string path, long fallback)
    {
        return ConfigConversions.LongValue(Value(path), fallback);
    }

    /// <summary>Reads a decimal number using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted value, or zero when conversion fails.</returns>
    public decimal GetDecimal(string path)
    {
        return ConfigConversions.DecimalValue(Value(path), 0m);
    }

    /// <summary>Reads a decimal number and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted value or the fallback.</returns>
    public decimal GetDecimalOrDefault(string path, decimal fallback)
    {
        return ConfigConversions.DecimalValue(Value(path), fallback);
    }

    /// <summary>Reads a double-precision number using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted value, or zero when conversion fails.</returns>
    public double GetDouble(string path)
    {
        return ConfigConversions.DoubleValue(Value(path), 0d);
    }

    /// <summary>Reads a double-precision number and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted value or the fallback.</returns>
    public double GetDoubleOrDefault(string path, double fallback)
    {
        return ConfigConversions.DoubleValue(Value(path), fallback);
    }

    /// <summary>Reads a duration from a number of ticks, a standard time span, or a value ending in <c>ms</c>, <c>s</c>, <c>m</c>, <c>h</c>, or <c>d</c>.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted duration, or <see cref="TimeSpan.Zero"/> when conversion fails.</returns>
    public TimeSpan GetTimeSpan(string path)
    {
        return ConfigConversions.TimeSpanValue(Value(path), TimeSpan.Zero);
    }

    /// <summary>Reads a duration and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted duration or the fallback.</returns>
    public TimeSpan GetTimeSpanOrDefault(string path, TimeSpan fallback)
    {
        return ConfigConversions.TimeSpanValue(Value(path), fallback);
    }

    /// <summary>Reads a byte count from a number or a value with a decimal or binary size suffix.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted byte count, or zero when conversion fails.</returns>
    public long GetByteSize(string path)
    {
        return ConfigConversions.ByteSizeValue(Value(path), 0L);
    }

    /// <summary>Reads a byte count and returns a fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The value returned when conversion is not possible.</param>
    /// <returns>The converted byte count or the fallback.</returns>
    public long GetByteSizeOrDefault(string path, long fallback)
    {
        return ConfigConversions.ByteSizeValue(Value(path), fallback);
    }

    /// <summary>Reads an array as text, or splits a scalar string on commas.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>Converted item text, or an empty array when conversion is not possible.</returns>
    public string[] GetStringArray(string path)
    {
        return ConfigConversions.StringArrayValue(Value(path));
    }

    /// <summary>Reads a string array and returns a copy of the fallback when conversion is not possible.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The items to copy when the value is missing, null, an object, or otherwise unsupported.</param>
    /// <returns>Converted item text or a copy of the fallback.</returns>
    public string[] GetStringArrayOrDefault(string path, string[] fallback)
    {
        ConfigValue value = Value(path);
        if (value.Kind == ConfigValueKind.Missing || value.Kind == ConfigValueKind.Null)
        {
            return ConfigConversions.CopyStringArray(fallback);
        }

        if (value.Kind != ConfigValueKind.Array && value.Kind != ConfigValueKind.String && value.Kind != ConfigValueKind.Number && value.Kind != ConfigValueKind.Boolean)
        {
            return ConfigConversions.CopyStringArray(fallback);
        }

        return ConfigConversions.StringArrayValue(value);
    }

    /// <summary>Reads 32-bit integers from an array, comma-delimited string, or scalar using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted values, or an empty array when any item cannot be converted.</returns>
    public int[] GetIntArray(string path)
    {
        return ConfigConversions.IntArrayValue(Value(path), new int[0]);
    }

    /// <summary>Reads a 32-bit integer array and returns a copy of the fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The values to copy when conversion is not possible.</param>
    /// <returns>The converted values or an independent fallback copy.</returns>
    public int[] GetIntArrayOrDefault(string path, int[] fallback)
    {
        return ConfigConversions.IntArrayValue(Value(path), fallback);
    }

    /// <summary>Reads 64-bit integers from an array, comma-delimited string, or scalar using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted values, or an empty array when any item cannot be converted.</returns>
    public long[] GetLongArray(string path)
    {
        return ConfigConversions.LongArrayValue(Value(path), new long[0]);
    }

    /// <summary>Reads a 64-bit integer array and returns a copy of the fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The values to copy when conversion is not possible.</param>
    /// <returns>The converted values or an independent fallback copy.</returns>
    public long[] GetLongArrayOrDefault(string path, long[] fallback)
    {
        return ConfigConversions.LongArrayValue(Value(path), fallback);
    }

    /// <summary>Reads decimal numbers from an array, comma-delimited string, or scalar using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted values, or an empty array when any item cannot be converted.</returns>
    public decimal[] GetDecimalArray(string path)
    {
        return ConfigConversions.DecimalArrayValue(Value(path), new decimal[0]);
    }

    /// <summary>Reads a decimal array and returns a copy of the fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The values to copy when conversion is not possible.</param>
    /// <returns>The converted values or an independent fallback copy.</returns>
    public decimal[] GetDecimalArrayOrDefault(string path, decimal[] fallback)
    {
        return ConfigConversions.DecimalArrayValue(Value(path), fallback);
    }

    /// <summary>Reads double-precision numbers from an array, comma-delimited string, or scalar using invariant conversion.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The converted values, or an empty array when any item cannot be converted.</returns>
    public double[] GetDoubleArray(string path)
    {
        return ConfigConversions.DoubleArrayValue(Value(path), new double[0]);
    }

    /// <summary>Reads a double-precision array and returns a copy of the fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="fallback">The values to copy when conversion is not possible.</param>
    /// <returns>The converted values or an independent fallback copy.</returns>
    public double[] GetDoubleArrayOrDefault(string path, double[] fallback)
    {
        return ConfigConversions.DoubleArrayValue(Value(path), fallback);
    }

    /// <summary>Reads an enum member by name using a runtime enum type.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="enumType">The enum type to read.</param>
    /// <returns>The boxed matching member, or the boxed zero value when conversion fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the type is not an enum.</exception>
    public object GetEnum(string path, Type enumType)
    {
        RequireEnumType(enumType);
        object fallback = ConfigConversions.EnumDefault(enumType);
        return ConfigConversions.EnumValue(Value(path), enumType, fallback);
    }

    /// <summary>Reads an enum member by name and returns a supplied fallback when conversion fails.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="enumType">The enum type to read.</param>
    /// <param name="fallback">A boxed member of the supplied enum type.</param>
    /// <returns>The boxed matching member or the fallback.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the type is not an enum or the fallback has a different type.</exception>
    public object GetEnumOrDefault(string path, Type enumType, object fallback)
    {
        RequireEnumType(enumType);
        RequireEnumFallback(enumType, fallback);
        return ConfigConversions.EnumValue(Value(path), enumType, fallback);
    }

    /// <summary>Reads a string without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadString(string path)
    {
        return ConfigReader.ReadString(this, path);
    }

    /// <summary>Reads a Boolean without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadBool(string path)
    {
        return ConfigReader.ReadBool(this, path);
    }

    /// <summary>Reads a 32-bit integer without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadInt(string path)
    {
        return ConfigReader.ReadInt(this, path);
    }

    /// <summary>Reads a 64-bit integer without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadLong(string path)
    {
        return ConfigReader.ReadLong(this, path);
    }

    /// <summary>Reads a decimal without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadDecimal(string path)
    {
        return ConfigReader.ReadDecimal(this, path);
    }

    /// <summary>Reads a double-precision number without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadDouble(string path)
    {
        return ConfigReader.ReadDouble(this, path);
    }

    /// <summary>Reads a duration without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadTimeSpan(string path)
    {
        return ConfigReader.ReadTimeSpan(this, path);
    }

    /// <summary>Reads a byte size without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadByteSize(string path)
    {
        return ConfigReader.ReadByteSize(this, path);
    }

    /// <summary>Reads a string array without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadStringArray(string path)
    {
        return ConfigReader.ReadStringArray(this, path);
    }

    /// <summary>Reads a 32-bit integer array without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadIntArray(string path)
    {
        return ConfigReader.ReadIntArray(this, path);
    }

    /// <summary>Reads a 64-bit integer array without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadLongArray(string path)
    {
        return ConfigReader.ReadLongArray(this, path);
    }

    /// <summary>Reads a decimal array without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadDecimalArray(string path)
    {
        return ConfigReader.ReadDecimalArray(this, path);
    }

    /// <summary>Reads a double-precision array without throwing for missing, null, or malformed values.</summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The explicit read outcome, diagnostic, and converted value.</returns>
    public ConfigReadResult ReadDoubleArray(string path)
    {
        return ConfigReader.ReadDoubleArray(this, path);
    }

    /// <summary>Reads an enum by runtime type without throwing for missing, null, malformed, or invalid-type requests.</summary>
    /// <param name="path">The configuration path.</param>
    /// <param name="enumType">The enum type to read.</param>
    /// <returns>The explicit read outcome, diagnostic, and boxed converted value.</returns>
    public ConfigReadResult ReadEnum(string path, Type enumType)
    {
        return ConfigReader.ReadEnum(this, path, enumType);
    }

    /// <summary>Requires a path and converts its value to invariant text.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted text.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public string RequireString(string path)
    {
        ConfigReadResult result = ReadString(path);
        RequireReadSuccess(result, path);
        return result.StringValue;
    }

    /// <summary>Requires a path and reads its value as a Boolean.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public bool RequireBool(string path)
    {
        ConfigReadResult result = ReadBool(path);
        RequireReadSuccess(result, path);
        return result.BooleanValue;
    }

    /// <summary>Requires a path and reads its value as a 32-bit integer.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public int RequireInt(string path)
    {
        ConfigReadResult result = ReadInt(path);
        RequireReadSuccess(result, path);
        return result.IntValue;
    }

    /// <summary>Requires a path and reads its value as a 64-bit integer.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public long RequireLong(string path)
    {
        ConfigReadResult result = ReadLong(path);
        RequireReadSuccess(result, path);
        return result.LongValue;
    }

    /// <summary>Requires a path and reads its value as a decimal number.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public decimal RequireDecimal(string path)
    {
        ConfigReadResult result = ReadDecimal(path);
        RequireReadSuccess(result, path);
        return result.DecimalValue;
    }

    /// <summary>Requires a path and reads its value as a double-precision number.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public double RequireDouble(string path)
    {
        ConfigReadResult result = ReadDouble(path);
        RequireReadSuccess(result, path);
        return result.DoubleValue;
    }

    /// <summary>Requires a path and reads its value as a duration.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted duration.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public TimeSpan RequireTimeSpan(string path)
    {
        ConfigReadResult result = ReadTimeSpan(path);
        RequireReadSuccess(result, path);
        return result.TimeSpanValue;
    }

    /// <summary>Requires a path and reads its value as a byte count.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted byte count.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public long RequireByteSize(string path)
    {
        ConfigReadResult result = ReadByteSize(path);
        RequireReadSuccess(result, path);
        return result.ByteSizeValue;
    }

    /// <summary>Requires a path and reads its value as a string array.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted items.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public string[] RequireStringArray(string path)
    {
        ConfigReadResult result = ReadStringArray(path);
        RequireReadSuccess(result, path);
        return result.StringArrayValue;
    }

    /// <summary>Requires a path and reads its value as a 32-bit integer array.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public int[] RequireIntArray(string path)
    {
        ConfigReadResult result = ReadIntArray(path);
        RequireReadSuccess(result, path);
        return result.IntArrayValue;
    }

    /// <summary>Requires a path and reads its value as a 64-bit integer array.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public long[] RequireLongArray(string path)
    {
        ConfigReadResult result = ReadLongArray(path);
        RequireReadSuccess(result, path);
        return result.LongArrayValue;
    }

    /// <summary>Requires a path and reads its value as a decimal array.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public decimal[] RequireDecimalArray(string path)
    {
        ConfigReadResult result = ReadDecimalArray(path);
        RequireReadSuccess(result, path);
        return result.DecimalArrayValue;
    }

    /// <summary>Requires a path and reads its value as a double-precision array.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    public double[] RequireDoubleArray(string path)
    {
        ConfigReadResult result = ReadDoubleArray(path);
        RequireReadSuccess(result, path);
        return result.DoubleArrayValue;
    }

    /// <summary>Requires a path and reads an enum member by name using a runtime enum type.</summary>
    /// <param name="path">The required configuration path.</param>
    /// <param name="enumType">The enum type to read.</param>
    /// <returns>The boxed matching member.</returns>
    /// <exception cref="ConfigConversionException">Thrown when the value is missing, null, or cannot be converted.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the type is not an enum.</exception>
    public object RequireEnum(string path, Type enumType)
    {
        RequireEnumType(enumType);
        ConfigReadResult result = ReadEnum(path, enumType);
        RequireReadSuccess(result, path);
        if (result.EnumValue == null)
        {
            throw ConfigExceptionFactory.ConversionFromDiagnostic("invalid required config value: " + path, result.Diagnostic);
        }

        return result.EnumValue;
    }

    internal static void RequireReadSuccess(ConfigReadResult result, string path)
    {
        if (result.Status == ConfigReadStatus.Success)
        {
            return;
        }

        if (result.Status == ConfigReadStatus.Missing)
        {
            throw ConfigExceptionFactory.ConversionFromDiagnostic("missing required config value: " + path, result.Diagnostic);
        }

        if (result.Status == ConfigReadStatus.Null)
        {
            throw ConfigExceptionFactory.ConversionFromDiagnostic("null required config value: " + path, result.Diagnostic);
        }

        throw ConfigExceptionFactory.ConversionFromDiagnostic("invalid required config value: " + path + ": " + result.Diagnostic.Message, result.Diagnostic);
    }

    internal static void RequireEnumType(Type enumType)
    {
        if (enumType == null)
        {
            throw new ArgumentNullException("enumType");
        }

        if (!enumType.IsEnum)
        {
            throw new ArgumentException("type must be an enum", "enumType");
        }
    }

    internal static void RequireEnumFallback(Type enumType, object fallback)
    {
        if (!enumType.IsInstanceOfType(fallback))
        {
            throw new ArgumentException("fallback must be a member of the supplied enum type", "fallback");
        }
    }
}
