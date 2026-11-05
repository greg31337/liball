using System;
using System.Collections.Generic;

namespace config;

/// <summary>Defines access to an immutable merged configuration snapshot.</summary>
public interface IConfig
{
    /// <summary>Checks whether a path exists.</summary>
    bool Has(string path);
    /// <summary>Reads a configuration value.</summary>
    ConfigValue Value(string path);
    /// <summary>Reads an object section.</summary>
    ConfigMap Section(string path);
    /// <summary>Lists direct child keys below a path.</summary>
    string[] Children(string path);
    /// <summary>Lists direct root keys.</summary>
    string[] Keys();
    /// <summary>Gets build diagnostics.</summary>
    ConfigDiagnostics Diagnostics();
    /// <summary>Serializes the snapshot as JSON.</summary>
    string ToJson();
    /// <summary>Serializes the snapshot as redacted JSON.</summary>
    string ToRedactedJson(ConfigRedactor redactor);
    /// <summary>Flattens leaf values.</summary>
    Dictionary<string, string> ToFlatMap();
    /// <summary>Flattens and redacts leaf values.</summary>
    Dictionary<string, string> ToRedactedFlatMap(ConfigRedactor redactor);
    /// <summary>Writes JSON to a new file.</summary>
    void WriteJson(string path);
    /// <summary>Writes JSON, replacing an existing file.</summary>
    void OverwriteJson(string path);
    /// <summary>Writes flat text to a new file.</summary>
    void WriteFlat(string path);
    /// <summary>Writes flat text, replacing an existing file.</summary>
    void OverwriteFlat(string path);
    /// <summary>Creates and binds an instance.</summary>
    object Bind(Type targetType);
    /// <summary>Creates and binds an instance with field mappings.</summary>
    object BindMapped(Type targetType, ConfigFieldMap fieldMap);
    /// <summary>Creates and binds an instance from a section.</summary>
    object BindSection(string path, Type targetType);
    /// <summary>Creates and binds a mapped instance from a section.</summary>
    object BindMappedSection(string path, Type targetType, ConfigFieldMap fieldMap);
    /// <summary>Binds an existing instance.</summary>
    void BindInstance(object target);
    /// <summary>Binds an existing instance with field mappings.</summary>
    void BindMappedInstance(object target, ConfigFieldMap fieldMap);
    /// <summary>Binds an existing instance from a section.</summary>
    void BindSectionInstance(string path, object target);
    /// <summary>Binds a mapped existing instance from a section.</summary>
    void BindMappedSectionInstance(string path, object target, ConfigFieldMap fieldMap);
    /// <summary>Requires a present, non-null value.</summary>
    ConfigValue Require(string path);
    /// <summary>Gets a string.</summary>
    string GetString(string path);
    /// <summary>Gets a string or a fallback.</summary>
    string GetStringOrDefault(string path, string fallback);
    /// <summary>Gets a Boolean.</summary>
    bool GetBool(string path);
    /// <summary>Gets a Boolean or a fallback.</summary>
    bool GetBoolOrDefault(string path, bool fallback);
    /// <summary>Gets a 32-bit integer.</summary>
    int GetInt(string path);
    /// <summary>Gets a 32-bit integer or a fallback.</summary>
    int GetIntOrDefault(string path, int fallback);
    /// <summary>Gets a 64-bit integer.</summary>
    long GetLong(string path);
    /// <summary>Gets a 64-bit integer or a fallback.</summary>
    long GetLongOrDefault(string path, long fallback);
    /// <summary>Gets a decimal number.</summary>
    decimal GetDecimal(string path);
    /// <summary>Gets a decimal number or a fallback.</summary>
    decimal GetDecimalOrDefault(string path, decimal fallback);
    /// <summary>Gets a double-precision number.</summary>
    double GetDouble(string path);
    /// <summary>Gets a double-precision number or a fallback.</summary>
    double GetDoubleOrDefault(string path, double fallback);
    /// <summary>Gets a duration.</summary>
    TimeSpan GetTimeSpan(string path);
    /// <summary>Gets a duration or a fallback.</summary>
    TimeSpan GetTimeSpanOrDefault(string path, TimeSpan fallback);
    /// <summary>Gets a byte size.</summary>
    long GetByteSize(string path);
    /// <summary>Gets a byte size or a fallback.</summary>
    long GetByteSizeOrDefault(string path, long fallback);
    /// <summary>Gets a string array.</summary>
    string[] GetStringArray(string path);
    /// <summary>Gets a string array or a fallback.</summary>
    string[] GetStringArrayOrDefault(string path, string[] fallback);
    /// <summary>Gets a 32-bit integer array.</summary>
    int[] GetIntArray(string path);
    /// <summary>Gets a 32-bit integer array or a fallback.</summary>
    int[] GetIntArrayOrDefault(string path, int[] fallback);
    /// <summary>Gets a 64-bit integer array.</summary>
    long[] GetLongArray(string path);
    /// <summary>Gets a 64-bit integer array or a fallback.</summary>
    long[] GetLongArrayOrDefault(string path, long[] fallback);
    /// <summary>Gets a decimal array.</summary>
    decimal[] GetDecimalArray(string path);
    /// <summary>Gets a decimal array or a fallback.</summary>
    decimal[] GetDecimalArrayOrDefault(string path, decimal[] fallback);
    /// <summary>Gets a double-precision array.</summary>
    double[] GetDoubleArray(string path);
    /// <summary>Gets a double-precision array or a fallback.</summary>
    double[] GetDoubleArrayOrDefault(string path, double[] fallback);
    /// <summary>Gets an enum member by name using a runtime enum type.</summary>
    object GetEnum(string path, Type enumType);
    /// <summary>Gets an enum member by name or a fallback.</summary>
    object GetEnumOrDefault(string path, Type enumType, object fallback);
    /// <summary>Reads a string with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadString(string path);
    /// <summary>Reads a Boolean with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadBool(string path);
    /// <summary>Reads a 32-bit integer with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadInt(string path);
    /// <summary>Reads a 64-bit integer with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadLong(string path);
    /// <summary>Reads a decimal with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadDecimal(string path);
    /// <summary>Reads a double-precision number with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadDouble(string path);
    /// <summary>Reads a duration with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadTimeSpan(string path);
    /// <summary>Reads a byte size with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadByteSize(string path);
    /// <summary>Reads a string array with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadStringArray(string path);
    /// <summary>Reads a 32-bit integer array with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadIntArray(string path);
    /// <summary>Reads a 64-bit integer array with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadLongArray(string path);
    /// <summary>Reads a decimal array with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadDecimalArray(string path);
    /// <summary>Reads a double-precision array with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadDoubleArray(string path);
    /// <summary>Reads an enum with an explicit status and diagnostic.</summary>
    ConfigReadResult ReadEnum(string path, Type enumType);
    /// <summary>Requires a string.</summary>
    string RequireString(string path);
    /// <summary>Requires a Boolean.</summary>
    bool RequireBool(string path);
    /// <summary>Requires a 32-bit integer.</summary>
    int RequireInt(string path);
    /// <summary>Requires a 64-bit integer.</summary>
    long RequireLong(string path);
    /// <summary>Requires a decimal number.</summary>
    decimal RequireDecimal(string path);
    /// <summary>Requires a double-precision number.</summary>
    double RequireDouble(string path);
    /// <summary>Requires a duration.</summary>
    TimeSpan RequireTimeSpan(string path);
    /// <summary>Requires a byte size.</summary>
    long RequireByteSize(string path);
    /// <summary>Requires a string array.</summary>
    string[] RequireStringArray(string path);
    /// <summary>Requires a 32-bit integer array.</summary>
    int[] RequireIntArray(string path);
    /// <summary>Requires a 64-bit integer array.</summary>
    long[] RequireLongArray(string path);
    /// <summary>Requires a decimal array.</summary>
    decimal[] RequireDecimalArray(string path);
    /// <summary>Requires a double-precision array.</summary>
    double[] RequireDoubleArray(string path);
    /// <summary>Requires an enum member by name using a runtime enum type.</summary>
    object RequireEnum(string path, Type enumType);
}
