namespace cookies;

/// <summary>
/// Represents one ordered name and value pair from a Cookie request field.
/// </summary>
public class CookiePair
{
    /// <summary>
    /// The cookie name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The cookie value.
    /// </summary>
    public string Value = "";

    /// <summary>
    /// Indicates that compatible parsing observed an allowed quoted value.
    /// </summary>
    public bool IsQuoted;
}
