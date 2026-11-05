namespace uri;

/// <summary>
/// Represents one ordered key/value member of an associative URI Template value.
/// </summary>
public class UriTemplateMapEntry
{
    /// <summary>The member key.</summary>
    public string Key;

    /// <summary>Whether the member has a defined value.</summary>
    public bool Defined;

    /// <summary>The member value.</summary>
    public string Value;

    /// <summary>Initializes an ordered associative member.</summary>
    /// <param name="key">The member key.</param>
    /// <param name="defined">Whether the member has a defined value.</param>
    /// <param name="value">The member value.</param>
    public UriTemplateMapEntry(string key, bool defined, string value)
    {
        Key = key;
        Defined = defined;
        Value = value;
    }
}
