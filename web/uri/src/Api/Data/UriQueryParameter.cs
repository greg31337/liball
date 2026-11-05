namespace uri;

/// <summary>
/// Represents one decoded ordered query parameter and its equals-sign state.
/// </summary>
public class UriQueryParameter
{
    /// <summary>The decoded parameter name.</summary>
    public string Name;

    /// <summary>Whether the original pair contains an equals sign and value.</summary>
    public bool HasValue;

    /// <summary>The decoded value; ignored when <see cref="HasValue"/> is false.</summary>
    public string Value;

    /// <summary>Initializes a decoded query parameter.</summary>
    /// <param name="name">The decoded parameter name.</param>
    /// <param name="hasValue">Whether the parameter has an equals sign and value.</param>
    /// <param name="value">The decoded parameter value.</param>
    public UriQueryParameter(
        string name,
        bool hasValue,
        string value)
    {
        Name = name;
        HasValue = hasValue;
        Value = value;
    }
}
