using System.Collections.Generic;
using System.Text;

namespace config;

/// <summary>Stores an ordered collection of configuration values.</summary>
public class ConfigArray : IConfigArray
{
    internal List<ConfigValue> Values;

    /// <summary>Creates an empty configuration array.</summary>
    public ConfigArray()
    {
        Values = new List<ConfigValue>();
    }

    /// <summary>Adds a value at the end of the array.</summary>
    /// <param name="value">The value to add.</param>
    public void Add(ConfigValue value)
    {
        Values.Add(value);
    }

    /// <summary>Reads an item without throwing for an out-of-range index.</summary>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The stored value, or a missing value when the index is outside the array.</returns>
    public ConfigValue Get(int index)
    {
        if (index < 0)
        {
            return ConfigValue.Missing();
        }

        if (index >= Values.Count)
        {
            return ConfigValue.Missing();
        }

        return Values[index];
    }

    /// <summary>Gets the number of values in the array.</summary>
    /// <returns>The item count.</returns>
    public int Count()
    {
        return Values.Count;
    }

    /// <summary>Copies the item references into a new .NET array.</summary>
    /// <returns>A new array containing the current values in order.</returns>
    public ConfigValue[] ToArray()
    {
        return Values.ToArray();
    }

    /// <summary>Creates a deep copy of this array and its nested values.</summary>
    /// <returns>An independent configuration array.</returns>
    public ConfigArray Copy()
    {
        ConfigArray array = new ConfigArray();

        int index = 0;
        while (index < Values.Count)
        {
            array.Add(Values[index].Copy());
            index = index + 1;
        }

        return array;
    }

    /// <summary>Formats the array as compact, deterministic diagnostic text.</summary>
    /// <returns>A bracketed list of item display values.</returns>
    public string ToDisplayString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("[");

        int index = 0;
        while (index < Values.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Values[index].ToDisplayString());
            index = index + 1;
        }

        builder.Append("]");
        return builder.ToString();
    }
}
