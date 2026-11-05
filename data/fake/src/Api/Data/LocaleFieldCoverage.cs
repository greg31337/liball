namespace fake;

/// <summary>
/// Describes the winning locale source for one field in a coverage report.
/// </summary>
public class LocaleFieldCoverage
{
    /// <summary>
    /// The provider name derived from the field's first dot-separated segment.
    /// </summary>
    public string Provider = string.Empty;

    /// <summary>
    /// The complete locale field name.
    /// </summary>
    public string Field = string.Empty;

    /// <summary>
    /// The canonical locale tag or <c>invariant</c> that supplies the field.
    /// </summary>
    public string SourceLocale = string.Empty;

    /// <summary>
    /// True only when the requested locale itself supplies the field.
    /// </summary>
    public bool Native;

    /// <summary>
    /// The data version attached to the winning field.
    /// </summary>
    public string DataVersion = string.Empty;

    /// <summary>
    /// The provenance description attached to the winning field.
    /// </summary>
    public string Provenance = string.Empty;

    internal LocaleFieldCoverage()
    {
        Provider = string.Empty;
        Field = string.Empty;
        SourceLocale = string.Empty;
        Native = false;
        DataVersion = string.Empty;
        Provenance = string.Empty;
    }
}
