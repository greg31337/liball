using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Stores schemas, tables, columns, indexes, functions, and collations used during semantic validation.</summary>
public class SqlCatalog
{
    /// <summary>The optional catalog name used to qualify contained schemas.</summary>
    public string? DefaultCatalogName;
    /// <summary>The schema name used for unqualified table lookups.</summary>
    public string DefaultSchemaName;
    /// <summary>Controls whether catalog name matching is case-sensitive.</summary>
    public bool CaseSensitiveNames;
    /// <summary>The schemas associated with this value.</summary>
    public List<SqlCatalogSchema> Schemas;
    /// <summary>The functions associated with this value.</summary>
    public List<SqlCatalogFunction> Functions;
    /// <summary>The collations associated with this value.</summary>
    public List<SqlCatalogCollation> Collations;
    /// <summary>The catalog-level aliases associated with this value.</summary>
    public List<SqlCatalogAlias> Aliases;

    /// <summary>Initializes a catalog with default name-matching settings.</summary>
    public SqlCatalog()
    {
        DefaultCatalogName = null;
        DefaultSchemaName = "main";
        CaseSensitiveNames = false;
        Schemas = new List<SqlCatalogSchema>();
        Functions = new List<SqlCatalogFunction>();
        Collations = new List<SqlCatalogCollation>();
        Aliases = new List<SqlCatalogAlias>();
        AddSchema(new SqlCatalogSchema(DefaultSchemaName));
    }

    /// <summary>Creates a catalog with default name-matching settings.</summary>
    /// <returns>The resulting catalog.</returns>
    internal static SqlCatalog CreateDefault()
    {
        return new SqlCatalog();
    }

    /// <summary>Creates a catalog with explicit name-matching settings.</summary>
    /// <param name="caseSensitiveNames">The case sensitive names to use.</param>
    /// <param name="defaultSchemaName">The default schema name to use.</param>
    /// <returns>The resulting catalog.</returns>
    internal static SqlCatalog Create(bool caseSensitiveNames, string defaultSchemaName)
    {
        ValidateName(defaultSchemaName, "defaultSchemaName");
        SqlCatalog catalog = new SqlCatalog();
        catalog.DefaultSchemaName = defaultSchemaName;
        catalog.CaseSensitiveNames = caseSensitiveNames;
        catalog.Schemas.Clear();
        catalog.AddSchema(new SqlCatalogSchema(defaultSchemaName));
        return catalog;
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="schema">The schema to use.</param>
    /// <exception cref="ArgumentNullException">schema is <c>null</c>.</exception>
    internal void AddSchema(SqlCatalogSchema schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("schema");
        }

        if (schema.CatalogName == null)
        {
            schema.CatalogName = DefaultCatalogName;
        }

        schema.ApplyQualification();
        Schemas.Add(schema);
    }

    /// <summary>Creates, stores, and returns a schema.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <returns>The resulting catalog schema.</returns>
    internal SqlCatalogSchema CreateSchema(string name)
    {
        SqlCatalogSchema schema = new SqlCatalogSchema(name);
        schema.CatalogName = DefaultCatalogName;
        AddSchema(schema);
        return schema;
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <returns>The resulting catalog schema.</returns>
    internal SqlCatalogSchema? FindSchema(string? name)
    {
        string schemaName = name == null || name.Length == 0 ? DefaultSchemaName : name;
        int index = 0;
        while (index < Schemas.Count)
        {
            if (NamesEqual(Schemas[index].Name, schemaName))
            {
                return Schemas[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Finds a schema with optional catalog qualification.</summary>
    /// <param name="name">The qualified or unqualified schema name.</param>
    /// <returns>The matching schema, or <c>null</c>.</returns>
    internal SqlCatalogSchema? FindSchemaByName(SqlName? name)
    {
        if (name == null || name.IsEmpty())
        {
            return null;
        }

        SqlCatalogSchema? schema = FindSchema(name.GetLastPart().Text);
        if (schema == null)
        {
            return null;
        }

        if (name.GetPartCount() > 1)
        {
            string catalogName = name.GetPartAt(name.GetPartCount() - 2).Text;
            if (schema.CatalogName == null || !NamesEqual(schema.CatalogName, catalogName))
            {
                return null;
            }
        }

        return schema;
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <returns>The resulting catalog table.</returns>
    internal SqlCatalogTable? FindTable(string tableName)
    {
        ValidateName(tableName, "tableName");
        SqlCatalogSchema? schema = FindSchema(DefaultSchemaName);
        if (schema == null)
        {
            return null;
        }

        return schema.FindTable(tableName, CaseSensitiveNames);
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <returns>The resulting catalog table.</returns>
    internal SqlCatalogTable? FindTableByName(SqlName? name)
    {
        if (name == null || name.IsEmpty())
        {
            return null;
        }

        string tableName = name.GetLastPart().Text;
        string? schemaName = null;
        if (name.GetPartCount() > 1)
        {
            schemaName = name.GetPartAt(name.GetPartCount() - 2).Text;
        }

        SqlCatalogSchema? schema = FindSchema(schemaName);
        if (schema == null)
        {
            return null;
        }

        if (name.GetPartCount() > 2)
        {
            string catalogName = name.GetPartAt(name.GetPartCount() - 3).Text;
            if (schema.CatalogName == null || !NamesEqual(schema.CatalogName, catalogName))
            {
                return null;
            }
        }

        return schema.FindTable(tableName, CaseSensitiveNames);
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="function">The function to use.</param>
    /// <exception cref="ArgumentNullException">function is <c>null</c>.</exception>
    internal void AddFunction(SqlCatalogFunction function)
    {
        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        Functions.Add(function);
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="argumentCount">The argument count to use.</param>
    /// <returns>The resulting catalog function.</returns>
    internal SqlCatalogFunction? FindFunction(string name, int argumentCount)
    {
        ValidateName(name, "name");
        int index = 0;
        while (index < Functions.Count)
        {
            if (Functions[index].Matches(name, argumentCount, CaseSensitiveNames))
            {
                return Functions[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="collation">The collation to use.</param>
    /// <exception cref="ArgumentNullException">collation is <c>null</c>.</exception>
    internal void AddCollation(SqlCatalogCollation collation)
    {
        if (collation == null)
        {
            throw new ArgumentNullException("collation");
        }

        Collations.Add(collation);
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <returns>The resulting catalog collation.</returns>
    internal SqlCatalogCollation? FindCollation(string name)
    {
        ValidateName(name, "name");
        int index = 0;
        while (index < Collations.Count)
        {
            if (NamesEqual(Collations[index].Name, name))
            {
                return Collations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds a catalog-level alias.</summary>
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

    /// <summary>Finds a catalog-level alias by name.</summary>
    /// <param name="name">The alias name to find.</param>
    /// <returns>The matching alias, or <c>null</c>.</returns>
    internal SqlCatalogAlias? FindAlias(string name)
    {
        ValidateName(name, "name");
        int index = 0;
        while (index < Aliases.Count)
        {
            if (NamesEqual(Aliases[index].Name, name))
            {
                return Aliases[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Finds a qualified view name.</summary>
    /// <param name="name">The qualified or unqualified view name.</param>
    /// <returns>The matching view, or <c>null</c>.</returns>
    internal SqlCatalogView? FindViewByName(SqlName? name)
    {
        if (name == null || name.IsEmpty())
        {
            return null;
        }

        string viewName = name.GetLastPart().Text;
        string? schemaName = null;
        if (name.GetPartCount() > 1)
        {
            schemaName = name.GetPartAt(name.GetPartCount() - 2).Text;
        }

        SqlCatalogSchema? schema = FindSchema(schemaName);
        if (schema == null)
        {
            return null;
        }

        if (name.GetPartCount() > 2)
        {
            string catalogName = name.GetPartAt(name.GetPartCount() - 3).Text;
            if (schema.CatalogName == null || !NamesEqual(schema.CatalogName, catalogName))
            {
                return null;
            }
        }

        return schema.FindView(viewName, CaseSensitiveNames);
    }

    /// <summary>Finds a qualified function name and compatible arity.</summary>
    /// <param name="name">The qualified or unqualified function name.</param>
    /// <param name="argumentCount">The supplied argument count.</param>
    /// <returns>The matching function, or <c>null</c>.</returns>
    internal SqlCatalogFunction? FindFunctionByName(SqlName? name, int argumentCount)
    {
        if (name == null || name.IsEmpty())
        {
            return null;
        }

        string functionName = name.GetLastPart().Text;
        if (name.GetPartCount() > 1)
        {
            string schemaName = name.GetPartAt(name.GetPartCount() - 2).Text;
            SqlCatalogSchema? schema = FindSchema(schemaName);
            if (schema == null)
            {
                return null;
            }

            if (name.GetPartCount() > 2)
            {
                string catalogName = name.GetPartAt(name.GetPartCount() - 3).Text;
                if (schema.CatalogName == null || !NamesEqual(schema.CatalogName, catalogName))
                {
                    return null;
                }
            }

            return schema.FindFunction(functionName, argumentCount, CaseSensitiveNames);
        }

        SqlCatalogSchema? defaultSchema = FindSchema(DefaultSchemaName);
        if (defaultSchema != null)
        {
            SqlCatalogFunction? schemaFunction = defaultSchema.FindFunction(functionName, argumentCount, CaseSensitiveNames);
            if (schemaFunction != null)
            {
                return schemaFunction;
            }
        }

        return FindFunction(functionName, argumentCount);
    }

    /// <summary>Finds a qualified collation name.</summary>
    /// <param name="name">The qualified or unqualified collation name.</param>
    /// <returns>The matching collation, or <c>null</c>.</returns>
    internal SqlCatalogCollation? FindCollationByName(SqlName? name)
    {
        if (name == null || name.IsEmpty())
        {
            return null;
        }

        string collationName = name.GetLastPart().Text;
        if (name.GetPartCount() > 1)
        {
            string schemaName = name.GetPartAt(name.GetPartCount() - 2).Text;
            SqlCatalogSchema? schema = FindSchema(schemaName);
            if (schema == null)
            {
                return null;
            }

            if (name.GetPartCount() > 2)
            {
                string catalogName = name.GetPartAt(name.GetPartCount() - 3).Text;
                if (schema.CatalogName == null || !NamesEqual(schema.CatalogName, catalogName))
                {
                    return null;
                }
            }

            return schema.FindCollation(collationName, CaseSensitiveNames);
        }

        SqlCatalogSchema? defaultSchema = FindSchema(DefaultSchemaName);
        if (defaultSchema != null)
        {
            SqlCatalogCollation? schemaCollation = defaultSchema.FindCollation(collationName, CaseSensitiveNames);
            if (schemaCollation != null)
            {
                return schemaCollation;
            }
        }

        return FindCollation(collationName);
    }

    internal bool NamesEqual(string left, string right)
    {
        if (CaseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Checks that a catalog name is neither <c>null</c> nor empty.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="parameterName">The parameter name to use.</param>
    internal static void ValidateName(string name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (name.Length == 0 || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(parameterName + " must not be empty or whitespace");
        }
    }
}

/// <summary>Represents a catalog schema used by the public SQL model.</summary>
public class SqlCatalogSchema
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The name associated with this value.</summary>
    public string Name;
    /// <summary>The tables associated with this value.</summary>
    public List<SqlCatalogTable> Tables;
    /// <summary>The indexes associated with this value.</summary>
    public List<SqlCatalogIndex> Indexes;
    /// <summary>The views associated with this schema.</summary>
    public List<SqlCatalogView> Views;
    /// <summary>The functions associated with this schema.</summary>
    public List<SqlCatalogFunction> Functions;
    /// <summary>The collations associated with this schema.</summary>
    public List<SqlCatalogCollation> Collations;
    /// <summary>The aliases associated with this schema.</summary>
    public List<SqlCatalogAlias> Aliases;

    /// <summary>Initializes a new instance of <see cref="SqlCatalogSchema"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    public SqlCatalogSchema(string name)
    {
        SqlCatalog.ValidateName(name, "name");
        CatalogName = null;
        Name = name;
        Tables = new List<SqlCatalogTable>();
        Indexes = new List<SqlCatalogIndex>();
        Views = new List<SqlCatalogView>();
        Functions = new List<SqlCatalogFunction>();
        Collations = new List<SqlCatalogCollation>();
        Aliases = new List<SqlCatalogAlias>();
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="table">The table to use.</param>
    /// <exception cref="ArgumentNullException">table is <c>null</c>.</exception>
    internal void AddTable(SqlCatalogTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        table.ApplyQualification(CatalogName, Name);
        Tables.Add(table);
    }

    /// <summary>Creates, stores, and returns a table.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <returns>The resulting catalog table.</returns>
    internal SqlCatalogTable CreateTable(string name)
    {
        SqlCatalogTable table = new SqlCatalogTable(name);
        AddTable(table);
        return table;
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="caseSensitiveNames">The case sensitive names to use.</param>
    /// <returns>The resulting catalog table.</returns>
    internal SqlCatalogTable? FindTable(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Tables.Count)
        {
            if (NamesEqual(Tables[index].Name, name, caseSensitiveNames))
            {
                return Tables[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds a view and fills omitted qualification from this schema.</summary>
    /// <param name="view">The view to add.</param>
    /// <exception cref="ArgumentNullException">view is <c>null</c>.</exception>
    internal void AddView(SqlCatalogView view)
    {
        if (view == null)
        {
            throw new ArgumentNullException("view");
        }

        view.ApplyQualification(CatalogName, Name);
        Views.Add(view);
    }

    /// <summary>Creates, stores, and returns a view.</summary>
    /// <param name="name">The view name.</param>
    /// <returns>The resulting view.</returns>
    internal SqlCatalogView CreateView(string name)
    {
        SqlCatalogView view = new SqlCatalogView(name);
        AddView(view);
        return view;
    }

    /// <summary>Finds a view in this schema.</summary>
    /// <param name="name">The view name.</param>
    /// <param name="caseSensitiveNames">Whether matching is case-sensitive.</param>
    /// <returns>The matching view, or <c>null</c>.</returns>
    internal SqlCatalogView? FindView(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Views.Count)
        {
            if (NamesEqual(Views[index].Name, name, caseSensitiveNames))
            {
                return Views[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <exception cref="ArgumentNullException">index is <c>null</c>.</exception>
    internal void AddIndex(SqlCatalogIndex index)
    {
        if (index == null)
        {
            throw new ArgumentNullException("index");
        }

        if (index.CatalogName == null)
        {
            index.CatalogName = CatalogName;
        }

        if (index.SchemaName == null)
        {
            index.SchemaName = Name;
        }

        Indexes.Add(index);
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="caseSensitiveNames">The case sensitive names to use.</param>
    /// <returns>The resulting catalog index.</returns>
    internal SqlCatalogIndex? FindIndex(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Indexes.Count)
        {
            if (NamesEqual(Indexes[index].Name, name, caseSensitiveNames))
            {
                return Indexes[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds a function and fills omitted qualification from this schema.</summary>
    /// <param name="function">The function to add.</param>
    /// <exception cref="ArgumentNullException">function is <c>null</c>.</exception>
    internal void AddFunction(SqlCatalogFunction function)
    {
        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        if (function.CatalogName == null)
        {
            function.CatalogName = CatalogName;
        }

        if (function.SchemaName == null)
        {
            function.SchemaName = Name;
        }

        Functions.Add(function);
    }

    /// <summary>Finds a function in this schema.</summary>
    /// <param name="name">The function name.</param>
    /// <param name="argumentCount">The supplied argument count.</param>
    /// <param name="caseSensitiveNames">Whether matching is case-sensitive.</param>
    /// <returns>The matching function, or <c>null</c>.</returns>
    internal SqlCatalogFunction? FindFunction(string name, int argumentCount, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Functions.Count)
        {
            if (Functions[index].Matches(name, argumentCount, caseSensitiveNames))
            {
                return Functions[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds a collation and fills omitted qualification from this schema.</summary>
    /// <param name="collation">The collation to add.</param>
    /// <exception cref="ArgumentNullException">collation is <c>null</c>.</exception>
    internal void AddCollation(SqlCatalogCollation collation)
    {
        if (collation == null)
        {
            throw new ArgumentNullException("collation");
        }

        if (collation.CatalogName == null)
        {
            collation.CatalogName = CatalogName;
        }

        if (collation.SchemaName == null)
        {
            collation.SchemaName = Name;
        }

        Collations.Add(collation);
    }

    /// <summary>Finds a collation in this schema.</summary>
    /// <param name="name">The collation name.</param>
    /// <param name="caseSensitiveNames">Whether matching is case-sensitive.</param>
    /// <returns>The matching collation, or <c>null</c>.</returns>
    internal SqlCatalogCollation? FindCollation(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Collations.Count)
        {
            if (NamesEqual(Collations[index].Name, name, caseSensitiveNames))
            {
                return Collations[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds an alias to this schema.</summary>
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

    /// <summary>Finds an alias in this schema.</summary>
    /// <param name="name">The alias name.</param>
    /// <param name="caseSensitiveNames">Whether matching is case-sensitive.</param>
    /// <returns>The matching alias, or <c>null</c>.</returns>
    internal SqlCatalogAlias? FindAlias(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Aliases.Count)
        {
            if (NamesEqual(Aliases[index].Name, name, caseSensitiveNames))
            {
                return Aliases[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void ApplyQualification()
    {
        int tableIndex = 0;
        while (tableIndex < Tables.Count)
        {
            Tables[tableIndex].ApplyQualification(CatalogName, Name);
            tableIndex = tableIndex + 1;
        }

        int viewIndex = 0;
        while (viewIndex < Views.Count)
        {
            Views[viewIndex].ApplyQualification(CatalogName, Name);
            viewIndex = viewIndex + 1;
        }

        int indexIndex = 0;
        while (indexIndex < Indexes.Count)
        {
            if (Indexes[indexIndex].CatalogName == null)
            {
                Indexes[indexIndex].CatalogName = CatalogName;
            }

            if (Indexes[indexIndex].SchemaName == null)
            {
                Indexes[indexIndex].SchemaName = Name;
            }

            indexIndex = indexIndex + 1;
        }

        int functionIndex = 0;
        while (functionIndex < Functions.Count)
        {
            if (Functions[functionIndex].CatalogName == null)
            {
                Functions[functionIndex].CatalogName = CatalogName;
            }

            if (Functions[functionIndex].SchemaName == null)
            {
                Functions[functionIndex].SchemaName = Name;
            }

            functionIndex = functionIndex + 1;
        }

        int collationIndex = 0;
        while (collationIndex < Collations.Count)
        {
            if (Collations[collationIndex].CatalogName == null)
            {
                Collations[collationIndex].CatalogName = CatalogName;
            }

            if (Collations[collationIndex].SchemaName == null)
            {
                Collations[collationIndex].SchemaName = Name;
            }

            collationIndex = collationIndex + 1;
        }
    }

    internal static bool NamesEqual(string left, string right, bool caseSensitiveNames)
    {
        if (caseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Represents a catalog table used by the public SQL model.</summary>
public class SqlCatalogTable
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The name associated with this value.</summary>
    public string Name;
    /// <summary>Indicates whether view.</summary>
    public bool IsView;
    /// <summary>The columns associated with this value.</summary>
    public List<SqlCatalogColumn> Columns;
    /// <summary>The indexes associated directly with this table.</summary>
    public List<SqlCatalogIndex> Indexes;
    /// <summary>The aliases associated with this table.</summary>
    public List<SqlCatalogAlias> Aliases;

    /// <summary>Initializes a catalog table.</summary>
    /// <param name="name">The name to use or locate.</param>
    public SqlCatalogTable(string name)
    {
        SqlCatalog.ValidateName(name, "name");
        CatalogName = null;
        SchemaName = null;
        Name = name;
        IsView = false;
        Columns = new List<SqlCatalogColumn>();
        Indexes = new List<SqlCatalogIndex>();
        Aliases = new List<SqlCatalogAlias>();
    }

    /// <summary>Creates a catalog table.</summary>
    /// <param name="name">The table name to use.</param>
    /// <returns>The resulting catalog table.</returns>
    internal static SqlCatalogTable Create(string name)
    {
        return new SqlCatalogTable(name);
    }

    /// <summary>Creates a catalog table or view.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="isView">The is view to use.</param>
    /// <returns>The resulting catalog table.</returns>
    internal static SqlCatalogTable Create(string name, bool isView)
    {
        SqlCatalogTable table = new SqlCatalogTable(name);
        table.IsView = isView;
        return table;
    }

    /// <summary>Adds the supplied value after validating it.</summary>
    /// <param name="column">The column to use.</param>
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

    /// <summary>Creates, stores, and returns a column.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="typeName">The type name to use.</param>
    /// <returns>The resulting catalog column.</returns>
    internal SqlCatalogColumn CreateColumn(string name, string typeName)
    {
        SqlCatalogColumn column = new SqlCatalogColumn(name, typeName);
        AddColumn(column);
        return column;
    }

    /// <summary>Searches this catalog and returns the first matching entry, or <c>null</c> when none exists.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="caseSensitiveNames">The case sensitive names to use.</param>
    /// <returns>The resulting catalog column.</returns>
    internal SqlCatalogColumn? FindColumn(string name, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        int index = 0;
        while (index < Columns.Count)
        {
            if (NamesEqual(Columns[index].Name, name, caseSensitiveNames))
            {
                return Columns[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Adds an index and fills omitted qualification from this table.</summary>
    /// <param name="index">The index to add.</param>
    /// <exception cref="ArgumentNullException">index is <c>null</c>.</exception>
    internal void AddTableIndex(SqlCatalogIndex index)
    {
        if (index == null)
        {
            throw new ArgumentNullException("index");
        }

        if (index.CatalogName == null)
        {
            index.CatalogName = CatalogName;
        }

        if (index.SchemaName == null)
        {
            index.SchemaName = SchemaName;
        }

        Indexes.Add(index);
    }

    /// <summary>Adds an alias to this table.</summary>
    /// <param name="alias">The alias to add.</param>
    /// <exception cref="ArgumentNullException">alias is <c>null</c>.</exception>
    internal void AddTableAlias(SqlCatalogAlias alias)
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

        int columnIndex = 0;
        while (columnIndex < Columns.Count)
        {
            if (Columns[columnIndex].CatalogName == null)
            {
                Columns[columnIndex].CatalogName = CatalogName;
            }

            if (Columns[columnIndex].SchemaName == null)
            {
                Columns[columnIndex].SchemaName = SchemaName;
            }

            if (Columns[columnIndex].RelationName == null)
            {
                Columns[columnIndex].RelationName = Name;
            }

            columnIndex = columnIndex + 1;
        }

        int indexIndex = 0;
        while (indexIndex < Indexes.Count)
        {
            if (Indexes[indexIndex].CatalogName == null)
            {
                Indexes[indexIndex].CatalogName = CatalogName;
            }

            if (Indexes[indexIndex].SchemaName == null)
            {
                Indexes[indexIndex].SchemaName = SchemaName;
            }

            indexIndex = indexIndex + 1;
        }
    }

    internal static bool NamesEqual(string left, string right, bool caseSensitiveNames)
    {
        if (caseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Represents a catalog column node in the SQL syntax tree.</summary>
public class SqlCatalogColumn
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The optional containing table or view name.</summary>
    public string? RelationName;
    /// <summary>The name associated with this value.</summary>
    public string Name;
    /// <summary>The type name associated with this value.</summary>
    public string TypeName;
    /// <summary>Indicates whether nullable.</summary>
    public bool IsNullable;
    /// <summary>Indicates whether primary key.</summary>
    public bool IsPrimaryKey;
    /// <summary>The SQLite type affinity derived from or assigned to this column.</summary>
    public SqliteTypeAffinityKind Affinity;
    /// <summary>The optional collation name associated with this column.</summary>
    public string? CollationName;
    /// <summary>The aliases associated with this column.</summary>
    public List<SqlCatalogAlias> Aliases;

    /// <summary>Initializes a nullable non-key catalog column.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="typeName">The type name to use.</param>
    /// <exception cref="ArgumentNullException">typeName is <c>null</c>.</exception>
    public SqlCatalogColumn(string name, string typeName)
    {
        SqlCatalog.ValidateName(name, "name");
        if (typeName == null)
        {
            throw new ArgumentNullException("typeName");
        }

        CatalogName = null;
        SchemaName = null;
        RelationName = null;
        Name = name;
        TypeName = typeName;
        IsNullable = true;
        IsPrimaryKey = false;
        Affinity = SqliteTypeAffinityKind.Unknown;
        CollationName = null;
        Aliases = new List<SqlCatalogAlias>();
    }

    /// <summary>Creates a nullable non-key catalog column.</summary>
    /// <param name="name">The column name to use.</param>
    /// <param name="typeName">The type name to use.</param>
    /// <returns>The resulting catalog column.</returns>
    internal static SqlCatalogColumn Create(string name, string typeName)
    {
        return new SqlCatalogColumn(name, typeName);
    }

    /// <summary>Creates a catalog column with explicit nullability and key metadata.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="typeName">The type name to use.</param>
    /// <param name="isNullable">The is nullable to use.</param>
    /// <param name="isPrimaryKey">The is primary key to use.</param>
    /// <exception cref="ArgumentNullException">typeName is <c>null</c>.</exception>
    /// <returns>The resulting catalog column.</returns>
    internal static SqlCatalogColumn Create(string name, string typeName, bool isNullable, bool isPrimaryKey)
    {
        SqlCatalogColumn column = new SqlCatalogColumn(name, typeName);
        column.IsNullable = isNullable;
        column.IsPrimaryKey = isPrimaryKey;
        return column;
    }
}

/// <summary>Represents a catalog index used by the public SQL model.</summary>
public class SqlCatalogIndex
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The name associated with this value.</summary>
    public string Name;
    /// <summary>The table name associated with this value.</summary>
    public string TableName;
    /// <summary>The column names in source or insertion order.</summary>
    public string[] ColumnNames;
    /// <summary>Indicates whether this index enforces uniqueness.</summary>
    public bool IsUnique;

    /// <summary>Initializes a new instance of <see cref="SqlCatalogIndex"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    public SqlCatalogIndex(string name, string tableName, string[] columnNames)
    {
        SqlCatalog.ValidateName(name, "name");
        SqlCatalog.ValidateName(tableName, "tableName");
        ValidateNames(columnNames, "columnNames");
        CatalogName = null;
        SchemaName = null;
        Name = name;
        TableName = tableName;
        ColumnNames = columnNames;
        IsUnique = false;
    }

    internal static void ValidateNames(string[] names, string parameterName)
    {
        if (names == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < names.Length)
        {
            SqlCatalog.ValidateName(names[index], parameterName);
            index = index + 1;
        }
    }
}

/// <summary>Represents a catalog function used by the public SQL model.</summary>
public class SqlCatalogFunction
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The name associated with this value.</summary>
    public string Name;
    /// <summary>The argument count associated with this value.</summary>
    public int ArgumentCount;
    /// <summary>Indicates whether aggregate.</summary>
    public bool IsAggregate;
    /// <summary>The declared return type name, or an empty string when unknown.</summary>
    public string ReturnTypeName;
    /// <summary>The SQLite affinity of the returned value.</summary>
    public SqliteTypeAffinityKind ReturnAffinity;

    /// <summary>Initializes a new instance of <see cref="SqlCatalogFunction"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="argumentCount">The argument count to use.</param>
    /// <param name="isAggregate">The is aggregate to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">argumentCount is outside the supported range or otherwise invalid.</exception>
    public SqlCatalogFunction(string name, int argumentCount, bool isAggregate)
    {
        SqlCatalog.ValidateName(name, "name");
        if (argumentCount < -1)
        {
            throw new ArgumentOutOfRangeException("argumentCount");
        }

        CatalogName = null;
        SchemaName = null;
        Name = name;
        ArgumentCount = argumentCount;
        IsAggregate = isAggregate;
        ReturnTypeName = "";
        ReturnAffinity = SqliteTypeAffinityKind.Unknown;
    }

    /// <summary>Returns whether the supplied name matches this function's name and arity.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="argumentCount">The argument count to use.</param>
    /// <param name="caseSensitiveNames">The case sensitive names to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Matches(string name, int argumentCount, bool caseSensitiveNames)
    {
        SqlCatalog.ValidateName(name, "name");
        if (ArgumentCount != -1 && ArgumentCount != argumentCount)
        {
            return false;
        }

        if (caseSensitiveNames)
        {
            return string.Equals(Name, name, StringComparison.Ordinal);
        }

        return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Represents a catalog collation used by the public SQL model.</summary>
public class SqlCatalogCollation
{
    /// <summary>The optional containing catalog name.</summary>
    public string? CatalogName;
    /// <summary>The optional containing schema name.</summary>
    public string? SchemaName;
    /// <summary>The name associated with this value.</summary>
    public string Name;

    /// <summary>Initializes a new instance of <see cref="SqlCatalogCollation"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    public SqlCatalogCollation(string name)
    {
        SqlCatalog.ValidateName(name, "name");
        CatalogName = null;
        SchemaName = null;
        Name = name;
    }
}
