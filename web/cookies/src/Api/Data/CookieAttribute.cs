namespace cookies;

/// <summary>
/// Represents a safe unrecognized Set-Cookie extension attribute.
/// </summary>
public class CookieAttribute
{
    /// <summary>
    /// The extension attribute name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates whether the extension included a value delimiter.
    /// </summary>
    public bool HasValue;

    /// <summary>
    /// The extension value when <see cref="HasValue"/> is true.
    /// </summary>
    public string Value = "";
}
