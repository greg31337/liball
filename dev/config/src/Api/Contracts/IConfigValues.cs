namespace config;

/// <summary>Defines creation, metadata, inspection, and copying operations for configuration values.</summary>
public interface IConfigValues
{
    /// <summary>Creates a missing value.</summary>
    ConfigValue Missing();
    /// <summary>Creates an explicit null value.</summary>
    ConfigValue Null();
    /// <summary>Creates a string value.</summary>
    ConfigValue String(string text);
    /// <summary>Creates a numeric value.</summary>
    ConfigValue Number(decimal number);
    /// <summary>Creates a Boolean value.</summary>
    ConfigValue Boolean(bool boolean);
    /// <summary>Creates an array value.</summary>
    ConfigValue Array(ConfigArray array);
    /// <summary>Creates an object value.</summary>
    ConfigValue Object(ConfigMap map);
    /// <summary>Attaches source metadata to a value.</summary>
    ConfigValue SetSource(ConfigValue value, string sourceName, int precedenceRank, string originalKey, int lineNumber, int columnNumber);
    /// <summary>Checks whether a value represents an absent path.</summary>
    bool IsMissing(ConfigValue value);
    /// <summary>Checks whether a value is an explicit null.</summary>
    bool IsNull(ConfigValue value);
    /// <summary>Checks whether source metadata is present.</summary>
    bool HasSource(ConfigValue value);
    /// <summary>Checks whether source location metadata is present.</summary>
    bool HasLocation(ConfigValue value);
    /// <summary>Creates a deep copy of a value.</summary>
    ConfigValue Copy(ConfigValue value);
    /// <summary>Formats a value for diagnostics.</summary>
    string ToDisplayString(ConfigValue value);
}
