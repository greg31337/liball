namespace config;

/// <summary>Defines operations on an ordered collection of configuration values.</summary>
public interface IConfigArray
{
    /// <summary>Adds a value at the end of the array.</summary>
    void Add(ConfigValue value);
    /// <summary>Reads an item without throwing for an invalid index.</summary>
    ConfigValue Get(int index);
    /// <summary>Gets the number of stored values.</summary>
    int Count();
    /// <summary>Copies the values into a new array.</summary>
    ConfigValue[] ToArray();
    /// <summary>Creates a deep copy.</summary>
    ConfigArray Copy();
    /// <summary>Formats the array for diagnostics.</summary>
    string ToDisplayString();
}
