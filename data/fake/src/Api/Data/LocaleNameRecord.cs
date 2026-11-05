namespace fake;

/// <summary>
/// Associates a locale-related code with a display name and its data source.
/// </summary>
public class LocaleNameRecord
{
    /// <summary>The canonical language, script, territory, currency, or time-zone code.</summary>
    public string Code = string.Empty;

    /// <summary>The display name associated with <see cref="Code"/>.</summary>
    public string Name = string.Empty;

    /// <summary>The canonical locale whose profile supplied the record.</summary>
    public string SourceLocale = string.Empty;

    /// <summary>Whether the source differs from the locale requested by the operation.</summary>
    public bool UsedFallback;

    /// <summary>The data version attached to the source profile.</summary>
    public string DataVersion = string.Empty;

    /// <summary>The provenance description attached to the source profile.</summary>
    public string Provenance = string.Empty;

    internal LocaleNameRecord()
    {
        Code = string.Empty;
        Name = string.Empty;
        SourceLocale = string.Empty;
        UsedFallback = false;
        DataVersion = string.Empty;
        Provenance = string.Empty;
    }
}
