namespace odf;

/// <summary>
/// Describes an OpenDocument database front end without executing database operations.
/// </summary>
public class OdfDatabaseDocument
{
    /// <summary>The document data source.</summary>
    public OdfDatabaseSource Source = new OdfDatabaseSource();
    /// <summary>The queries and query collections.</summary>
    public OdfDatabaseQuery[] Queries = [];
    /// <summary>The table representations and schema definitions.</summary>
    public OdfDatabaseTable[] Tables = [];
    /// <summary>The form components and collections.</summary>
    public OdfDatabaseComponent[] Forms = [];
    /// <summary>The report components and collections.</summary>
    public OdfDatabaseComponent[] Reports = [];
    /// <summary>The underlying office:database element.</summary>
    public OdfNode? Node;
}
