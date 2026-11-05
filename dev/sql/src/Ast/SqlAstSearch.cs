using System.Collections.Generic;

namespace sql;

internal class SqlAstSearch
{
    internal static SqlName[] FindTables(SqlScript? script)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        return FindTablesFromWalker(walker);
    }

    internal static SqlName[] FindTables(SqlStatement? statement)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkStatement(statement);
        return FindTablesFromWalker(walker);
    }

    internal static SqlName[] FindTables(SqlQuery? query)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkQuery(query);
        return FindTablesFromWalker(walker);
    }

    internal static SqlName[] FindTables(SqlExpression? expression)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkExpression(expression);
        return FindTablesFromWalker(walker);
    }

    internal static SqlName[] FindColumns(SqlScript? script)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        return FindColumnsFromWalker(walker);
    }

    internal static SqlName[] FindColumns(SqlStatement? statement)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkStatement(statement);
        return FindColumnsFromWalker(walker);
    }

    internal static SqlName[] FindColumns(SqlQuery? query)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkQuery(query);
        return FindColumnsFromWalker(walker);
    }

    internal static SqlName[] FindColumns(SqlExpression? expression)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkExpression(expression);
        return FindColumnsFromWalker(walker);
    }

    internal static SqlExpression[] FindParameters(SqlScript? script)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        return FindParametersFromWalker(walker);
    }

    internal static SqlExpression[] FindParameters(SqlStatement? statement)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkStatement(statement);
        return FindParametersFromWalker(walker);
    }

    internal static SqlExpression[] FindParameters(SqlQuery? query)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkQuery(query);
        return FindParametersFromWalker(walker);
    }

    internal static SqlExpression[] FindParameters(SqlExpression? expression)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkExpression(expression);
        return FindParametersFromWalker(walker);
    }

    internal static SqlExpression[] FindFunctions(SqlScript? script)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        return FindFunctionsFromWalker(walker);
    }

    internal static SqlExpression[] FindFunctions(SqlStatement? statement)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkStatement(statement);
        return FindFunctionsFromWalker(walker);
    }

    internal static SqlExpression[] FindFunctions(SqlQuery? query)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkQuery(query);
        return FindFunctionsFromWalker(walker);
    }

    internal static SqlExpression[] FindFunctions(SqlExpression? expression)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkExpression(expression);
        return FindFunctionsFromWalker(walker);
    }

    internal static SqlName[] FindTablesFromWalker(SqlAstWalker walker)
    {
        List<SqlName> tables = new List<SqlName>();
        int index = 0;
        while (index < walker.TableReferences.Count)
        {
            SqlTableReference tableReference = walker.TableReferences[index];
            if (tableReference.Kind == SqlTableReferenceKind.Named && tableReference.Name != null)
            {
                tables.Add(tableReference.Name);
            }

            index = index + 1;
        }

        index = 0;
        while (index < walker.TableDefinitions.Count)
        {
            tables.Add(walker.TableDefinitions[index].Name);
            index = index + 1;
        }

        index = 0;
        while (index < walker.AlterTableDefinitions.Count)
        {
            tables.Add(walker.AlterTableDefinitions[index].TableName);
            SqlName? newTableName = walker.AlterTableDefinitions[index].NewTableName;
            if (newTableName != null)
            {
                tables.Add(newTableName);
            }

            index = index + 1;
        }

        index = 0;
        while (index < walker.DropTableDefinitions.Count)
        {
            tables.Add(walker.DropTableDefinitions[index].Name);
            index = index + 1;
        }

        index = 0;
        while (index < walker.IndexDefinitions.Count)
        {
            tables.Add(walker.IndexDefinitions[index].TableName);
            index = index + 1;
        }

        index = 0;
        while (index < walker.VirtualTableDefinitions.Count)
        {
            tables.Add(walker.VirtualTableDefinitions[index].Name);
            index = index + 1;
        }

        index = 0;
        while (index < walker.TriggerDefinitions.Count)
        {
            tables.Add(walker.TriggerDefinitions[index].TargetName);
            index = index + 1;
        }

        index = 0;
        while (index < walker.InsertStatements.Count)
        {
            tables.Add(walker.InsertStatements[index].TableName);
            index = index + 1;
        }

        index = 0;
        while (index < walker.UpdateStatements.Count)
        {
            tables.Add(walker.UpdateStatements[index].TableName);
            index = index + 1;
        }

        index = 0;
        while (index < walker.DeleteStatements.Count)
        {
            tables.Add(walker.DeleteStatements[index].TableName);
            index = index + 1;
        }

        index = 0;
        while (index < walker.MergeStatements.Count)
        {
            tables.Add(walker.MergeStatements[index].TableName);
            index = index + 1;
        }

        return tables.ToArray();
    }

    internal static SqlName[] FindColumnsFromWalker(SqlAstWalker walker)
    {
        List<SqlName> columns = new List<SqlName>();
        int index = 0;
        while (index < walker.Expressions.Count)
        {
            SqlExpression expression = walker.Expressions[index];
            if (expression.Kind == SqlExpressionKind.Name && expression.Name != null)
            {
                columns.Add(expression.Name);
            }

            index = index + 1;
        }

        index = 0;
        while (index < walker.ColumnDefinitions.Count)
        {
            columns.Add(walker.ColumnDefinitions[index].Name);
            index = index + 1;
        }

        index = 0;
        while (index < walker.AlterTableDefinitions.Count)
        {
            SqlName? columnName = walker.AlterTableDefinitions[index].ColumnName;
            if (columnName != null)
            {
                columns.Add(columnName);
            }

            SqlName? newColumnName = walker.AlterTableDefinitions[index].NewColumnName;
            if (newColumnName != null)
            {
                columns.Add(newColumnName);
            }

            index = index + 1;
        }

        index = 0;
        while (index < walker.ViewDefinitions.Count)
        {
            AddNames(columns, walker.ViewDefinitions[index].ColumnNames);
            index = index + 1;
        }

        index = 0;
        while (index < walker.TriggerDefinitions.Count)
        {
            AddNames(columns, walker.TriggerDefinitions[index].UpdateColumns);
            index = index + 1;
        }

        index = 0;
        while (index < walker.TableConstraints.Count)
        {
            AddNames(columns, walker.TableConstraints[index].Columns);
            index = index + 1;
        }

        index = 0;
        while (index < walker.ForeignKeyReferences.Count)
        {
            AddNames(columns, walker.ForeignKeyReferences[index].ColumnNames);
            index = index + 1;
        }

        index = 0;
        while (index < walker.InsertStatements.Count)
        {
            AddNames(columns, walker.InsertStatements[index].ColumnNames);
            index = index + 1;
        }

        index = 0;
        while (index < walker.UpdateStatements.Count)
        {
            AddNames(columns, walker.UpdateStatements[index].AssignmentColumns);
            index = index + 1;
        }

        index = 0;
        while (index < walker.UpsertClauses.Count)
        {
            AddUpsertColumns(columns, walker.UpsertClauses[index]);
            index = index + 1;
        }

        index = 0;
        while (index < walker.MergeClauses.Count)
        {
            AddMergeColumns(columns, walker.MergeClauses[index]);
            index = index + 1;
        }

        return columns.ToArray();
    }

    internal static void AddMergeColumns(List<SqlName> columns, SqlMergeWhenClause clause)
    {
        int index = 0;
        while (index < clause.Assignments.Length)
        {
            AddNames(columns, clause.Assignments[index].Columns);
            index = index + 1;
        }

        AddNames(columns, clause.InsertColumns);
    }

    internal static void AddUpsertColumns(List<SqlName> columns, SqlUpsertClause clause)
    {
        int index = 0;
        while (index < clause.ConflictTargets.Length)
        {
            AddExpressionName(columns, clause.ConflictTargets[index].Expression);
            index = index + 1;
        }

        AddNames(columns, clause.UpdateColumns);
    }

    internal static void AddExpressionName(List<SqlName> columns, SqlExpression expression)
    {
        if (expression == null)
        {
            return;
        }

        if (expression.Kind == SqlExpressionKind.Name && expression.Name != null)
        {
            columns.Add(expression.Name);
        }
    }

    internal static SqlExpression[] FindParametersFromWalker(SqlAstWalker walker)
    {
        List<SqlExpression> parameters = new List<SqlExpression>();
        int index = 0;
        while (index < walker.Expressions.Count)
        {
            SqlExpression expression = walker.Expressions[index];
            if (expression.Kind == SqlExpressionKind.Parameter)
            {
                parameters.Add(expression);
            }

            index = index + 1;
        }

        return parameters.ToArray();
    }

    internal static SqlExpression[] FindFunctionsFromWalker(SqlAstWalker walker)
    {
        List<SqlExpression> functions = new List<SqlExpression>();
        int index = 0;
        while (index < walker.Expressions.Count)
        {
            SqlExpression expression = walker.Expressions[index];
            if (expression.Kind == SqlExpressionKind.FunctionCall)
            {
                functions.Add(expression);
            }

            index = index + 1;
        }

        return functions.ToArray();
    }

    internal static void AddNames(List<SqlName> target, SqlName[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            target.Add(names[index]);
            index = index + 1;
        }
    }
}
