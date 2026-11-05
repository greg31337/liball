using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides molecule metadata entry data and operations for molecular graphs.
/// </summary>
public class MoleculeMetadataEntry
{
    /// <summary>
    /// The key.
    /// </summary>
    public string Key;
    /// <summary>
    /// The value.
    /// </summary>
    public string Value;

    /// <summary>
    /// Creates an empty metadata entry.
    /// </summary>
    /// <returns>A metadata entry with an empty key and value.</returns>
    internal static MoleculeMetadataEntry CreateEmpty()
    {
        return new MoleculeMetadataEntry("", "");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeMetadataEntry"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public MoleculeMetadataEntry(string? key, string? value)
    {
        if (key == null)
        {
            Key = "";
        }
        else
        {
            Key = key;
        }

        if (value == null)
        {
            Value = "";
        }
        else
        {
            Value = value;
        }
    }
}

