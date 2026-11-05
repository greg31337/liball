namespace config;

/// <summary>Creates and operates on field-based configuration values.</summary>
public class ConfigValues : IConfigValues
{
    /// <summary>Creates a configuration value service.</summary>
    public ConfigValues()
    {
    }

    /// <summary>Creates a missing value.</summary>
    /// <returns>A new missing value.</returns>
    public ConfigValue Missing()
    {
        return ConfigValue.Missing();
    }

    /// <summary>Creates an explicit null value.</summary>
    /// <returns>A new null value.</returns>
    public ConfigValue Null()
    {
        return ConfigValue.Null();
    }

    /// <summary>Creates a string value.</summary>
    /// <param name="text">The text to store.</param>
    /// <returns>A new string value.</returns>
    public ConfigValue String(string text)
    {
        return ConfigValue.String(text);
    }

    /// <summary>Creates a numeric value.</summary>
    /// <param name="number">The number to store.</param>
    /// <returns>A new numeric value.</returns>
    public ConfigValue Number(decimal number)
    {
        return ConfigValue.Number(number);
    }

    /// <summary>Creates a Boolean value.</summary>
    /// <param name="boolean">The Boolean to store.</param>
    /// <returns>A new Boolean value.</returns>
    public ConfigValue Boolean(bool boolean)
    {
        return ConfigValue.Boolean(boolean);
    }

    /// <summary>Creates an array value.</summary>
    /// <param name="array">The array to store.</param>
    /// <returns>A new array value.</returns>
    public ConfigValue Array(ConfigArray array)
    {
        return ConfigValue.Array(array);
    }

    /// <summary>Creates an object value.</summary>
    /// <param name="map">The map to store.</param>
    /// <returns>A new object value.</returns>
    public ConfigValue Object(ConfigMap map)
    {
        return ConfigValue.Object(map);
    }

    /// <summary>Attaches source metadata to a value.</summary>
    /// <param name="value">The value to update.</param>
    /// <param name="sourceName">The source name.</param>
    /// <param name="precedenceRank">The one-based source rank.</param>
    /// <param name="originalKey">The original source key.</param>
    /// <param name="lineNumber">The one-based source line, or zero.</param>
    /// <param name="columnNumber">The one-based source column, or zero.</param>
    /// <returns>The supplied value.</returns>
    public ConfigValue SetSource(ConfigValue value, string sourceName, int precedenceRank, string originalKey, int lineNumber, int columnNumber)
    {
        return value.SetSource(sourceName, precedenceRank, originalKey, lineNumber, columnNumber);
    }

    /// <summary>Checks whether a value represents an absent path.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>True when the value is missing.</returns>
    public bool IsMissing(ConfigValue value)
    {
        return value.IsMissing();
    }

    /// <summary>Checks whether a value is an explicit null.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>True when the value is null.</returns>
    public bool IsNull(ConfigValue value)
    {
        return value.IsNull();
    }

    /// <summary>Checks whether source metadata is present.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>True when a source name is available.</returns>
    public bool HasSource(ConfigValue value)
    {
        return value.HasSource();
    }

    /// <summary>Checks whether source location metadata is present.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>True when line and column values are available.</returns>
    public bool HasLocation(ConfigValue value)
    {
        return value.HasLocation();
    }

    /// <summary>Creates a deep copy of a value.</summary>
    /// <param name="value">The value to copy.</param>
    /// <returns>An independent copy.</returns>
    public ConfigValue Copy(ConfigValue value)
    {
        return value.Copy();
    }

    /// <summary>Formats a value for diagnostics.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A compact diagnostic representation.</returns>
    public string ToDisplayString(ConfigValue value)
    {
        return value.ToDisplayString();
    }
}
