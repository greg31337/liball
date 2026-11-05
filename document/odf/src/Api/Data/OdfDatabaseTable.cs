namespace odf;

/// <summary>
/// Describes an inert database table representation or schema definition.
/// </summary>
public class OdfDatabaseTable
{
    /// <summary>Whether this item is a schema table definition.</summary>
    public bool IsDefinition;
    /// <summary>The table name.</summary>
    public string Name = "";
    /// <summary>The catalog name.</summary>
    public string CatalogName = "";
    /// <summary>The schema name.</summary>
    public string SchemaName = "";
    /// <summary>The schema table type token.</summary>
    public string Type = "";
    /// <summary>The human-readable title.</summary>
    public string Title = "";
    /// <summary>The human-readable description.</summary>
    public string Description = "";
    /// <summary>The optional order statement.</summary>
    public string OrderStatement = "";
    /// <summary>The optional filter statement.</summary>
    public string FilterStatement = "";
    /// <summary>The preserved column elements.</summary>
    public OdfNode[] Columns = [];
    /// <summary>The preserved key elements.</summary>
    public OdfNode[] Keys = [];
    /// <summary>The preserved index elements.</summary>
    public OdfNode[] Indices = [];
    /// <summary>The underlying table element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalCatalogName = "";
    internal string OriginalSchemaName = "";
    internal string OriginalType = "";
    internal string OriginalTitle = "";
    internal string OriginalDescription = "";
    internal string OriginalOrderStatement = "";
    internal string OriginalFilterStatement = "";
}
