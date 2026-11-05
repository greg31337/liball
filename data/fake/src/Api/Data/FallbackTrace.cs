namespace fake;

/// <summary>
/// Records how one locale field was resolved and which source supplied it.
/// </summary>
public class FallbackTrace
{
    /// <summary>
    /// The canonical requested locale tag.
    /// </summary>
    public string RequestedLocale = string.Empty;

    /// <summary>
    /// The requested field name.
    /// </summary>
    public string Field = string.Empty;

    /// <summary>
    /// The canonical locale source that supplied the value, or <c>invariant</c>.
    /// </summary>
    public string SourceLocale = string.Empty;

    /// <summary>
    /// The locales examined in deterministic resolution order through the winning source.
    /// </summary>
    public string[] AttemptedLocales = new string[0];

    /// <summary>
    /// Whether the winning source differs from the exact requested locale.
    /// </summary>
    public bool UsedFallback;

    /// <summary>
    /// The resolved field value.
    /// </summary>
    public object Value;

    /// <summary>
    /// The data version of the winning field source.
    /// </summary>
    public string DataVersion = string.Empty;

    /// <summary>
    /// The provenance description of the winning field source.
    /// </summary>
    public string Provenance = string.Empty;

    internal FallbackTrace()
    {
        RequestedLocale = string.Empty;
        Field = string.Empty;
        SourceLocale = string.Empty;
        AttemptedLocales = new string[0];
        UsedFallback = false;
        Value = string.Empty;
        DataVersion = string.Empty;
        Provenance = string.Empty;
    }
}
