namespace odf;

/// <summary>
/// Describes an inert database front-end data source.
/// </summary>
public class OdfDatabaseSource
{
    /// <summary>The selected resource, file, or server connection.</summary>
    public OdfDatabaseConnection? Connection;
    /// <summary>The optional login settings.</summary>
    public OdfDatabaseLogin? Login;
    /// <summary>The optional driver settings.</summary>
    public OdfDatabaseDriverSettings? DriverSettings;
    /// <summary>The application connection attributes.</summary>
    public OdfAttribute[] ApplicationConnectionAttributes = [];
    /// <summary>The included table-name filters.</summary>
    public string[] IncludedTableFilters = [];
    /// <summary>The excluded table-name filters.</summary>
    public string[] ExcludedTableFilters = [];
    /// <summary>The enabled table type filters.</summary>
    public string[] TableTypeFilters = [];
    /// <summary>The typed application data-source settings.</summary>
    public OdfDatabaseSetting[] Settings = [];
    /// <summary>The underlying data-source element.</summary>
    public OdfNode? Node;
}
