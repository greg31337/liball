using System.Globalization;

namespace config;

/// <summary>Represents one configuration value together with information about the source that supplied it.</summary>
/// <remarks>Inspect <see cref="Kind"/> before reading a kind-specific field. <see cref="ConfigValues"/> creates valid values, and container values retain their nested structure.</remarks>
public class ConfigValue
{
    /// <summary>Identifies which value field contains the configured data.</summary>
    public ConfigValueKind Kind;
    /// <summary>Contains the text when <see cref="Kind"/> is <see cref="ConfigValueKind.String"/>, or null for other kinds.</summary>
    public string? StringValue;
    /// <summary>Contains the number when <see cref="Kind"/> is <see cref="ConfigValueKind.Number"/>.</summary>
    public decimal NumberValue;
    /// <summary>Contains the Boolean when <see cref="Kind"/> is <see cref="ConfigValueKind.Boolean"/>.</summary>
    public bool BooleanValue;
    /// <summary>Contains the array when <see cref="Kind"/> is <see cref="ConfigValueKind.Array"/>, or null for other kinds.</summary>
    public ConfigArray? ArrayValue;
    /// <summary>Contains the object when <see cref="Kind"/> is <see cref="ConfigValueKind.Object"/>, or null for other kinds.</summary>
    public ConfigMap? ObjectValue;
    /// <summary>Names the source that supplied the value, or contains null when no source is recorded.</summary>
    public string? SourceName;
    /// <summary>Records the one-based source precedence assigned during loading; larger ranks are later sources.</summary>
    public int PrecedenceRank;
    /// <summary>Records the key as it appeared in the source, or contains null when unavailable.</summary>
    public string? OriginalKey;
    /// <summary>Records the one-based source line, or zero when the parser cannot provide a location.</summary>
    public int LineNumber;
    /// <summary>Records the one-based source column, or zero when the parser cannot provide a location.</summary>
    public int ColumnNumber;

    internal ConfigValue()
    {
        Kind = ConfigValueKind.Missing;
        StringValue = null;
        NumberValue = 0m;
        BooleanValue = false;
        ArrayValue = null;
        ObjectValue = null;
        SourceName = null;
        PrecedenceRank = 0;
        OriginalKey = null;
        LineNumber = 0;
        ColumnNumber = 0;
    }

    /// <summary>Creates a value that represents an absent path.</summary>
    /// <returns>A new missing value.</returns>
    internal static ConfigValue Missing()
    {
        return new ConfigValue();
    }

    /// <summary>Creates an explicitly configured null value.</summary>
    /// <returns>A new null value.</returns>
    internal static ConfigValue Null()
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.Null;
        return value;
    }

    /// <summary>Creates a configuration string.</summary>
    /// <param name="text">The text to store.</param>
    /// <returns>A new string value.</returns>
    internal static ConfigValue String(string text)
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.String;
        value.StringValue = text;
        return value;
    }

    /// <summary>Creates a configuration number.</summary>
    /// <param name="number">The decimal value to store.</param>
    /// <returns>A new numeric value.</returns>
    internal static ConfigValue Number(decimal number)
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.Number;
        value.NumberValue = number;
        return value;
    }

    /// <summary>Creates a configuration Boolean.</summary>
    /// <param name="boolean">The true or false value to store.</param>
    /// <returns>A new Boolean value.</returns>
    internal static ConfigValue Boolean(bool boolean)
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.Boolean;
        value.BooleanValue = boolean;
        return value;
    }

    /// <summary>Creates a configuration array backed by the supplied container.</summary>
    /// <param name="array">The ordered values to store.</param>
    /// <returns>A new array value.</returns>
    internal static ConfigValue Array(ConfigArray array)
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.Array;
        value.ArrayValue = array;
        return value;
    }

    /// <summary>Creates a configuration object backed by the supplied map.</summary>
    /// <param name="map">The named values to store.</param>
    /// <returns>A new object value.</returns>
    internal static ConfigValue Object(ConfigMap map)
    {
        ConfigValue value = new ConfigValue();
        value.Kind = ConfigValueKind.Object;
        value.ObjectValue = map;
        return value;
    }

    /// <summary>Checks whether this value represents an absent path.</summary>
    /// <returns>True for <see cref="ConfigValueKind.Missing"/>; otherwise false.</returns>
    internal bool IsMissing()
    {
        return Kind == ConfigValueKind.Missing;
    }

    /// <summary>Checks whether this value is an explicit null.</summary>
    /// <returns>True for <see cref="ConfigValueKind.Null"/>; otherwise false.</returns>
    internal bool IsNull()
    {
        return Kind == ConfigValueKind.Null;
    }

    /// <summary>Attaches source metadata to this value.</summary>
    /// <param name="sourceName">The source name shown in diagnostics.</param>
    /// <param name="precedenceRank">The one-based source position supplied to <see cref="IConfigSource.Load"/>.</param>
    /// <param name="originalKey">The key as it appeared in the source, or an empty string when unavailable.</param>
    /// <param name="lineNumber">The one-based line number, or zero when unavailable.</param>
    /// <param name="columnNumber">The one-based column number, or zero when unavailable.</param>
    /// <returns>This value for fluent construction.</returns>
    internal ConfigValue SetSource(string sourceName, int precedenceRank, string originalKey, int lineNumber, int columnNumber)
    {
        SourceName = sourceName;
        PrecedenceRank = precedenceRank;
        OriginalKey = originalKey;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        return this;
    }

    /// <summary>Checks whether source metadata includes a nonempty source name.</summary>
    /// <returns>True when a source name is available.</returns>
    internal bool HasSource()
    {
        if (SourceName == null)
        {
            return false;
        }

        return SourceName.Length > 0;
    }

    /// <summary>Checks whether both a positive line and column were recorded.</summary>
    /// <returns>True when a complete source location is available.</returns>
    internal bool HasLocation()
    {
        if (LineNumber <= 0)
        {
            return false;
        }

        return ColumnNumber > 0;
    }

    internal void CopyMetadataTo(ConfigValue value)
    {
        value.SourceName = SourceName;
        value.PrecedenceRank = PrecedenceRank;
        value.OriginalKey = OriginalKey;
        value.LineNumber = LineNumber;
        value.ColumnNumber = ColumnNumber;
    }

    /// <summary>Creates a deep copy, including nested values and source metadata.</summary>
    /// <returns>An independent copy of this value.</returns>
    internal ConfigValue Copy()
    {
        ConfigValue value = new ConfigValue();
        value.Kind = Kind;
        value.StringValue = StringValue;
        value.NumberValue = NumberValue;
        value.BooleanValue = BooleanValue;

        if (ArrayValue != null)
        {
            value.ArrayValue = ArrayValue.Copy();
        }

        if (ObjectValue != null)
        {
            value.ObjectValue = ObjectValue.Copy();
        }

        CopyMetadataTo(value);
        return value;
    }

    /// <summary>Formats the value as compact, deterministic text intended for diagnostics.</summary>
    /// <returns>A representation of the value, including markers for missing or invalid state.</returns>
    internal string ToDisplayString()
    {
        if (Kind == ConfigValueKind.Missing)
        {
            return "(missing)";
        }

        if (Kind == ConfigValueKind.Null)
        {
            return "null";
        }

        if (Kind == ConfigValueKind.String)
        {
            return "\"" + StringValue + "\"";
        }

        if (Kind == ConfigValueKind.Number)
        {
            return NumberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (Kind == ConfigValueKind.Boolean)
        {
            if (BooleanValue)
            {
                return "true";
            }

            return "false";
        }

        if (Kind == ConfigValueKind.Array && ArrayValue != null)
        {
            return ArrayValue.ToDisplayString();
        }

        if (Kind == ConfigValueKind.Object && ObjectValue != null)
        {
            return ObjectValue.ToDisplayString();
        }

        return "(invalid)";
    }
}
