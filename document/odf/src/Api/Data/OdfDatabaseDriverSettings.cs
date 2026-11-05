namespace odf;

/// <summary>
/// Describes inert database driver and application connection settings.
/// </summary>
public class OdfDatabaseDriverSettings
{
    /// <summary>The preserved deleted-row visibility flag.</summary>
    public string ShowDeleted = "";
    /// <summary>Opaque system driver settings.</summary>
    public string SystemDriverSettings = "";
    /// <summary>The directory-service base distinguished name.</summary>
    public string BaseDn = "";
    /// <summary>The preserved first-row-header flag.</summary>
    public string IsFirstRowHeaderLine = "";
    /// <summary>The preserved named-parameter substitution flag.</summary>
    public string ParameterNameSubstitution = "";
    /// <summary>The statement used to create an automatic value.</summary>
    public string AdditionalColumnStatement = "";
    /// <summary>The statement used to retrieve an inserted row.</summary>
    public string RowRetrievingStatement = "";
    /// <summary>The field delimiter.</summary>
    public string FieldDelimiter = "";
    /// <summary>The string delimiter.</summary>
    public string StringDelimiter = "";
    /// <summary>The decimal delimiter.</summary>
    public string DecimalDelimiter = "";
    /// <summary>The thousands delimiter.</summary>
    public string ThousandsDelimiter = "";
    /// <summary>The driver character encoding.</summary>
    public string Encoding = "";
    /// <summary>The underlying driver-settings element.</summary>
    public OdfNode? Node;
    internal string OriginalShowDeleted = "";
    internal string OriginalSystemDriverSettings = "";
    internal string OriginalBaseDn = "";
    internal string OriginalIsFirstRowHeaderLine = "";
    internal string OriginalParameterNameSubstitution = "";
    internal string OriginalAdditionalColumnStatement = "";
    internal string OriginalRowRetrievingStatement = "";
    internal string OriginalFieldDelimiter = "";
    internal string OriginalStringDelimiter = "";
    internal string OriginalDecimalDelimiter = "";
    internal string OriginalThousandsDelimiter = "";
    internal string OriginalEncoding = "";
}
