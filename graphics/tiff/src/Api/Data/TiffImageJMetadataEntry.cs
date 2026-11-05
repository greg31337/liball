namespace tiff;

/// <summary>Represents image J metadata entry used by the public TIFF API.</summary>
public class TiffImageJMetadataEntry
{
    /// <summary>Contains the key.</summary>
    public string Key;
    /// <summary>Contains the value.</summary>
    public string Value;

    /// <summary>Creates a new instance of the TIFF Image J Metadata Entry class.</summary>
    public TiffImageJMetadataEntry()
    {
        Key = "";
        Value = "";
    }

    /// <summary>Creates a new instance of the TIFF Image J Metadata Entry class.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal static TiffImageJMetadataEntry Create(string key, string value)
    {
        TiffImageJMetadataEntry entry = new TiffImageJMetadataEntry();
        entry.Initialize(key, value);
        return entry;
    }

    internal void Initialize(string key, string value)
    {
        Key = NormalizeString(key);
        Value = NormalizeString(value);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF image J metadata entry.</returns>
    internal TiffImageJMetadataEntry Copy()
    {
        return TiffImageJMetadataEntry.Create(Key, Value);
    }

    internal static string NormalizeString(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
