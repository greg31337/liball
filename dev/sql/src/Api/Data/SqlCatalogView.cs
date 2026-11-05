using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Represents a qualified catalog view and its exposed columns.</summary>
public class SqlCatalogView
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The unqualified view name.</summary>
    public string Name;
    /// <summary>The columns exposed by the view.</summary>
    public List<SqlCatalogColumn> Columns;
    /// <summary>The optional parsed query defining the view.</summary>
    public SqlQuery? Query;
    /// <summary>The aliases associated with the view.</summary>
    public List<SqlCatalogAlias> Aliases;

    /// <summary>Initializes a catalog view.</summary>
    /// <param name="name">The unqualified view name.</param>
    public SqlCatalogView(string name)
    {
        SqlCatalog.ValidateName(name, "name");
        CatalogName = null;
        SchemaName = null;
        Name = name;
        Columns = new List<SqlCatalogColumn>();
        Query = null;
        Aliases = new List<SqlCatalogAlias>();
    }

    /// <summary>Adds a column and fills omitted qualification from this view.</summary>
    /// <param name="column">The column to add.</param>
    /// <exception cref="ArgumentNullException">column is <c>null</c>.</exception>
    internal void AddColumn(SqlCatalogColumn column)
    {
        if (column == null)
        {
            throw new ArgumentNullException("column");
        }

        if (column.CatalogName == null)
        {
            column.CatalogName = CatalogName;
        }

        if (column.SchemaName == null)
        {
            column.SchemaName = SchemaName;
        }

        if (column.RelationName == null)
        {
            column.RelationName = Name;
        }

        Columns.Add(column);
    }

    /// <summary>Finds a column exposed by this view.</summary>
    /// <param name="name">The column name.</param>
    /// <param name="caseSensitiveNames">Whether matching is case-sensitive.</param>
    /// <returns>The matching column, or <c>null</c>.</returns>
    internal SqlCatalogColumn? FindColumn(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Columns.Count)
        {
            if (SqlCatalogSchema.NamesEqual(Columns[index].Name, name, caseSensitiveNames))
            {
                return Columns[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds an alias to this view.</summary>
    /// <param name="alias">The alias to add.</param>
    /// <exception cref="ArgumentNullException">alias is <c>null</c>.</exception>
    internal void AddAlias(SqlCatalogAlias alias)
    {
        if (alias == null)
        {
            throw new ArgumentNullException("alias");
        }

        Aliases.Add(alias);
    }

    internal void ApplyQualification(string? catalogName, string schemaName)
    {
        if (CatalogName == null)
        {
            CatalogName = catalogName;
        }

        if (SchemaName == null)
        {
            SchemaName = schemaName;
        }

        int index = 0;
        while (index < Columns.Count)
        {
            if (Columns[index].CatalogName == null)
            {
                Columns[index].CatalogName = CatalogName;
            }

            if (Columns[index].SchemaName == null)
            {
                Columns[index].SchemaName = SchemaName;
            }

            if (Columns[index].RelationName == null)
            {
                Columns[index].RelationName = Name;
            }

            index = index + 1;
        }
    }
}
