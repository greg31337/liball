namespace cookies;

/// <summary>
/// Specifies the severity of a cookie processing issue.
/// </summary>
public enum CookieParseSeverity
{
    /// <summary>
    /// Reports information that does not impair the operation.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Reports a recoverable or lossy condition.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Reports a condition that prevents the requested operation.
    /// </summary>
    Error = 2
}
