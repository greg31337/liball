namespace countrycodes;

/// <summary>
/// Reports that a valid locale is not available in the compiled dataset.
/// </summary>
public class UnsupportedLocaleException : global::System.Exception
{
    /// <summary>
    /// The canonical locale that is not supported.
    /// </summary>
    public string Locale;

    /// <summary>
    /// Creates an unsupported-locale exception with stable locale context.
    /// </summary>
    /// <param name="locale">The canonical unsupported locale.</param>
    public UnsupportedLocaleException(string locale)
        : base($"Locale '{locale}' is not supported.")
    {
        Locale = locale;
    }
}
