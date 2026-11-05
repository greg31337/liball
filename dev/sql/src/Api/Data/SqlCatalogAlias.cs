namespace sql;

/// <summary>Represents an alternate name for a schema, relation, column, or common table expression.</summary>
public class SqlCatalogAlias
{
    /// <summary>The alias name visible in its scope.</summary>
    public string Name;
    /// <summary>The category of object named by the alias.</summary>
    public SqlCatalogAliasKind Kind;
    /// <summary>The optional target catalog name.</summary>
    public string? TargetCatalogName;
    /// <summary>The optional target schema name.</summary>
    public string? TargetSchemaName;
    /// <summary>The target relation, schema, or common table expression name.</summary>
    public string TargetName;
    /// <summary>The optional target column name.</summary>
    public string? TargetColumnName;

    /// <summary>Initializes an alias for a catalog object.</summary>
    /// <param name="name">The alias name.</param>
    /// <param name="kind">The target object category.</param>
    /// <param name="targetName">The target object's unqualified name.</param>
    public SqlCatalogAlias(string name, SqlCatalogAliasKind kind, string targetName)
    {
        SqlCatalog.ValidateName(name, "name");
        SqlCatalog.ValidateName(targetName, "targetName");
        Name = name;
        Kind = kind;
        TargetCatalogName = null;
        TargetSchemaName = null;
        TargetName = targetName;
        TargetColumnName = null;
    }
}
