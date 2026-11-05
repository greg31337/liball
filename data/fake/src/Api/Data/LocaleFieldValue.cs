namespace fake;

/// <summary>
/// Associates one locale field with a concrete value supplied to locale resolution.
/// </summary>
public class LocaleFieldValue
{
    /// <summary>
    /// Identifies the final non-cultural fallback source.
    /// </summary>
    public const string InvariantLocale = "invariant";

    /// <summary>
    /// Identifies values supplied directly by a caller.
    /// </summary>
    public const string CallerProvenance = "caller-supplied";

    /// <summary>
    /// The BCP 47 source locale or <c>invariant</c>.
    /// </summary>
    public string Locale = string.Empty;

    /// <summary>
    /// The field name within the source locale.
    /// </summary>
    public string Field = string.Empty;

    /// <summary>
    /// The field value returned when this source is selected.
    /// </summary>
    public object Value = new object();

    /// <summary>
    /// The data version for this field, or null to use the version selected by <see cref="FakeOptions"/>.
    /// </summary>
    public string? DataVersion;

    /// <summary>
    /// The source or authorship description for this field.
    /// </summary>
    public string Provenance = string.Empty;

    /// <summary>
    /// Initializes a locale field value.
    /// </summary>
    /// <param name="locale">The BCP 47 source locale or <c>invariant</c>.</param>
    /// <param name="field">The nonempty field name.</param>
    /// <param name="value">The non-null field value.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a string argument is empty or whitespace.</exception>
    public LocaleFieldValue(string locale, string field, object value)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        Guard.NotNullOrWhiteSpace(field, "field");
        Guard.NotNull(value, "value");
        Locale = locale;
        Field = field;
        Value = value;
        DataVersion = null;
        Provenance = CallerProvenance;
    }

    /// <summary>
    /// Creates a locale field value with explicit version and provenance metadata.
    /// </summary>
    /// <param name="locale">The BCP 47 source locale or <c>invariant</c>.</param>
    /// <param name="field">The nonempty field name.</param>
    /// <param name="value">The non-null field value.</param>
    /// <param name="dataVersion">The nonempty data version.</param>
    /// <param name="provenance">The nonempty source or authorship description.</param>
    /// <returns>A locale field value containing the supplied metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a string argument is empty or whitespace.</exception>
    internal static LocaleFieldValue WithMetadata(
        string locale,
        string field,
        object value,
        string dataVersion,
        string provenance)
    {
        Guard.NotNullOrWhiteSpace(locale, "locale");
        Guard.NotNullOrWhiteSpace(field, "field");
        Guard.NotNull(value, "value");
        Guard.NotNullOrWhiteSpace(dataVersion, "dataVersion");
        Guard.NotNullOrWhiteSpace(provenance, "provenance");
        LocaleFieldValue result = new LocaleFieldValue(locale, field, value);
        result.DataVersion = dataVersion;
        result.Provenance = provenance;
        return result;
    }
}
