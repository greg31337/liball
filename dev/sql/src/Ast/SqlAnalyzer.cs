using System.Collections.Generic;

namespace sql;

/// <summary>Provides read-only searches and high-level read/write summaries for SQL syntax trees.</summary>
/// <remarks>Normal SQL syntax problems are represented as diagnostics where the operation returns a result; argument and configuration errors are reported as exceptions.</remarks>
internal static class SqlAnalyzer
{
    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindTables(SqlScript? script)
    {
        return SqlAstSearch.FindTables(script);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindTables(SqlStatement? statement)
    {
        return SqlAstSearch.FindTables(statement);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindTables(SqlQuery? query)
    {
        return SqlAstSearch.FindTables(query);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindTables(SqlExpression? expression)
    {
        return SqlAstSearch.FindTables(expression);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindColumns(SqlScript? script)
    {
        return SqlAstSearch.FindColumns(script);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindColumns(SqlStatement? statement)
    {
        return SqlAstSearch.FindColumns(statement);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindColumns(SqlQuery? query)
    {
        return SqlAstSearch.FindColumns(query);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlName[] FindColumns(SqlExpression? expression)
    {
        return SqlAstSearch.FindColumns(expression);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindParameters(SqlScript? script)
    {
        return SqlAstSearch.FindParameters(script);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindParameters(SqlStatement? statement)
    {
        return SqlAstSearch.FindParameters(statement);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindParameters(SqlQuery? query)
    {
        return SqlAstSearch.FindParameters(query);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindParameters(SqlExpression? expression)
    {
        return SqlAstSearch.FindParameters(expression);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindFunctions(SqlScript? script)
    {
        return SqlAstSearch.FindFunctions(script);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindFunctions(SqlStatement? statement)
    {
        return SqlAstSearch.FindFunctions(statement);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindFunctions(SqlQuery? query)
    {
        return SqlAstSearch.FindFunctions(query);
    }

    /// <summary>Searches the supplied tree recursively and returns matching nodes in encounter order.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <returns>The matching nodes in encounter order.</returns>
    internal static SqlExpression[] FindFunctions(SqlExpression? expression)
    {
        return SqlAstSearch.FindFunctions(expression);
    }

    /// <summary>Builds a high-level summary of the statement's table reads and writes.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The resulting statement summary.</returns>
    internal static SqlStatementSummary GetStatementSummary(SqlStatement? statement)
    {
        if (statement == null)
        {
            return CreateSummary(SqlStatementKind.Unknown, EmptyNames(), EmptyNames(), true);
        }

        if (statement.Kind == SqlStatementKind.Select)
        {
            return CreateSummary(statement.Kind, UniqueNames(SqlAstSearch.FindTables(statement)), EmptyNames(), false);
        }

        if (statement.Kind == SqlStatementKind.Explain)
        {
            return SummarizeExplain(statement);
        }

        if (statement.Kind == SqlStatementKind.Insert)
        {
            SqlInsertStatement? insert = statement.Insert;
            if (insert == null)
            {
                return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
            }

            SqlName[] writes = UniqueSingleName(insert.TableName);
            return CreateSummary(statement.Kind, ReadTablesWithoutWrites(statement, writes), writes, false);
        }

        if (statement.Kind == SqlStatementKind.Update)
        {
            SqlUpdateStatement? update = statement.Update;
            if (update == null)
            {
                return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
            }

            SqlName[] writes = UniqueSingleName(update.TableName);
            return CreateSummary(statement.Kind, ReadTablesWithoutWrites(statement, writes), writes, false);
        }

        if (statement.Kind == SqlStatementKind.Delete)
        {
            SqlDeleteStatement? delete = statement.Delete;
            if (delete == null)
            {
                return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
            }

            SqlName[] writes = UniqueSingleName(delete.TableName);
            return CreateSummary(statement.Kind, ReadTablesWithoutWrites(statement, writes), writes, false);
        }

        if (statement.Kind == SqlStatementKind.Merge)
        {
            SqlMergeStatement? merge = statement.Merge;
            if (merge == null)
            {
                return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
            }

            SqlName[] writes = UniqueSingleName(merge.TableName);
            return CreateSummary(statement.Kind, ReadTablesWithoutWrites(statement, writes), writes, false);
        }

        if (statement.Kind == SqlStatementKind.CreateTable)
        {
            SqlTableDefinition? table = statement.CreateTable;
            return SummarizeSchemaStatement(statement, table == null ? null : table.Name);
        }

        if (statement.Kind == SqlStatementKind.CreateIndex)
        {
            SqlIndexDefinition? index = statement.CreateIndex;
            return SummarizeSchemaStatement(statement, index == null ? null : index.TableName);
        }

        if (statement.Kind == SqlStatementKind.AlterTable)
        {
            SqlAlterTableDefinition? alter = statement.AlterTable;
            return SummarizeSchemaStatement(statement, alter == null ? null : alter.TableName);
        }

        if (statement.Kind == SqlStatementKind.DropTable)
        {
            SqlDropTableDefinition? drop = statement.DropTable;
            return SummarizeSchemaStatement(statement, drop == null ? null : drop.Name);
        }

        if (statement.Kind == SqlStatementKind.CreateView)
        {
            SqlViewDefinition? view = statement.CreateView;
            return SummarizeSchemaStatement(statement, view == null ? null : view.Name);
        }

        if (statement.Kind == SqlStatementKind.DropView)
        {
            SqlDropViewDefinition? drop = statement.DropView;
            return SummarizeSchemaStatement(statement, drop == null ? null : drop.Name);
        }

        if (statement.Kind == SqlStatementKind.CreateVirtualTable)
        {
            SqlVirtualTableDefinition? table = statement.CreateVirtualTable;
            return SummarizeSchemaStatement(statement, table == null ? null : table.Name);
        }

        if (statement.Kind == SqlStatementKind.CreateTrigger)
        {
            SqlTriggerDefinition? trigger = statement.CreateTrigger;
            return SummarizeSchemaStatement(statement, trigger == null ? null : trigger.TargetName);
        }

        return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), IsUnknownEffectStatement(statement.Kind));
    }

    internal static SqlStatementSummary SummarizeExplain(SqlStatement statement)
    {
        SqlExplainStatement? explain = statement.Explain;
        if (explain == null || explain.Statement == null)
        {
            return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
        }

        SqlStatementSummary inner = GetStatementSummary(explain.Statement);
        return CreateSummary(statement.Kind, inner.ReadTables, EmptyNames(), inner.HasUnknownEffects);
    }

    internal static SqlStatementSummary SummarizeSchemaStatement(SqlStatement statement, SqlName? writeName)
    {
        if (writeName == null)
        {
            return CreateSummary(statement.Kind, EmptyNames(), EmptyNames(), true);
        }

        SqlName[] writes = UniqueSingleName(writeName);
        return CreateSummary(statement.Kind, ReadTablesWithoutWrites(statement, writes), writes, false);
    }

    internal static SqlName[] ReadTablesWithoutWrites(SqlStatement statement, SqlName[] writes)
    {
        SqlName[] allTables = SqlAstSearch.FindTables(statement);
        List<SqlName> reads = new List<SqlName>();
        int index = 0;
        while (index < allTables.Length)
        {
            if (!ContainsNameText(writes, allTables[index].GetText()))
            {
                AddUniqueName(reads, allTables[index]);
            }

            index = index + 1;
        }

        return reads.ToArray();
    }

    internal static SqlStatementSummary CreateSummary(SqlStatementKind kind, SqlName[] reads, SqlName[] writes, bool hasUnknownEffects)
    {
        return new SqlStatementSummary(kind, UniqueNames(reads), UniqueNames(writes), hasUnknownEffects);
    }

    internal static SqlName[] UniqueSingleName(SqlName name)
    {
        return new SqlName[] { name };
    }

    internal static SqlName[] UniqueNames(SqlName[] names)
    {
        List<SqlName> result = new List<SqlName>();
        int index = 0;
        while (index < names.Length)
        {
            AddUniqueName(result, names[index]);
            index = index + 1;
        }

        return result.ToArray();
    }

    internal static void AddUniqueName(List<SqlName> names, SqlName name)
    {
        if (!ContainsNameText(names, name.GetText()))
        {
            names.Add(name);
        }
    }

    internal static bool ContainsNameText(List<SqlName> names, string text)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetText() == text)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsNameText(SqlName[] names, string text)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].GetText() == text)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static SqlName[] EmptyNames()
    {
        return new SqlName[0];
    }

    internal static bool IsUnknownEffectStatement(SqlStatementKind kind)
    {
        return kind == SqlStatementKind.Unknown || kind == SqlStatementKind.Pragma;
    }
}
