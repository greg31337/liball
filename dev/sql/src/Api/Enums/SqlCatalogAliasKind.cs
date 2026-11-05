namespace sql;

/// <summary>Specifies the catalog object category named by an alias.</summary>
public enum SqlCatalogAliasKind
{
    /// <summary>The target category is unknown.</summary>
    Unknown = 0,
    /// <summary>The alias names a schema.</summary>
    Schema = 1,
    /// <summary>The alias names a table.</summary>
    Table = 2,
    /// <summary>The alias names a view.</summary>
    View = 3,
    /// <summary>The alias names a column.</summary>
    Column = 4,
    /// <summary>The alias names a common table expression.</summary>
    CommonTableExpression = 5
}
