namespace countrycodes;

/// <summary>
/// Holds a localized or categorized display name.
/// </summary>
public class LocalizedName
{
    /// <summary>
    /// The canonical locale tag, or an empty string for a language-neutral name.
    /// </summary>
    public string Locale;

    /// <summary>
    /// The display value in NFC form.
    /// </summary>
    public string Value;

    /// <summary>
    /// The role of the name.
    /// </summary>
    public CountryNameKind Kind;

    /// <summary>
    /// The source identifier for the name.
    /// </summary>
    public string Provenance;

    /// <summary>
    /// Creates a detached localized name.
    /// </summary>
    /// <param name="locale">The canonical locale tag, or an empty string.</param>
    /// <param name="value">The display value.</param>
    /// <param name="kind">The role of the name.</param>
    /// <param name="provenance">The source identifier for the name.</param>
    public LocalizedName(
        string locale,
        string value,
        CountryNameKind kind,
        string provenance)
    {
        Locale = locale;
        Value = value;
        Kind = kind;
        Provenance = provenance;
    }

    internal static LocalizedName[] CopyArray(LocalizedName[] values)
    {
        LocalizedName[] copies = new LocalizedName[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            LocalizedName value = values[index];
            copies[index] = new LocalizedName(
                value.Locale,
                value.Value,
                value.Kind,
                value.Provenance);
            index += 1;
        }

        return copies;
    }
}
