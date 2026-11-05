using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Conservatively infers SQLite affinity for catalog and syntax-tree values.</summary>
internal static class SqliteTypeAffinityAnalysis
{
    internal static SqliteTypeAffinityResult AnalyzeScript(SqlScript script, SqlCatalog catalog)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        SqlColumnLineageResult lineage = SqlColumnLineageAnalysis.Analyze(script, catalog);
        SqliteTypeAffinityContext context = new SqliteTypeAffinityContext(catalog, lineage);
        return context.AnalyzeScriptNodes(script);
    }

    internal static SqliteTypeAffinityKind GetLiteralAffinity(SqlLiteral literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        return SqliteTypeAffinityContext.GetLiteralAffinityCore(literal);
    }

    internal static SqliteTypeAffinityKind GetColumnAffinity(SqlCatalogColumn column)
    {
        if (column == null)
        {
            throw new ArgumentNullException("column");
        }

        return SqliteTypeAffinityContext.GetColumnAffinityCore(column);
    }

    internal static SqliteTypeAffinityKind InferExpression(SqlExpression expression, SqlCatalog catalog)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        return new SqliteTypeAffinityContext(catalog, null).InferExpressionCore(expression);
    }

    internal static SqliteTypeAffinityKind InferResultColumn(SqlResultColumn resultColumn, SqlCatalog catalog)
    {
        if (resultColumn == null)
        {
            throw new ArgumentNullException("resultColumn");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        if (resultColumn.IsWildcard || resultColumn.Expression == null)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        return new SqliteTypeAffinityContext(catalog, null).InferExpressionCore(resultColumn.Expression);
    }

    internal static SqliteTypeAffinityKind InferAssignment(SqlAssignment assignment, SqlCatalog catalog)
    {
        if (assignment == null)
        {
            throw new ArgumentNullException("assignment");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        return new SqliteTypeAffinityContext(catalog, null).InferExpressionCore(assignment.Value);
    }
}

internal class SqliteTypeAffinityContext
{
    internal SqlCatalog Catalog;
    internal SqlColumnLineageResult? Lineage;
    internal List<SqliteTypeAffinityEntry> Entries;
    internal List<SqlCatalogColumn> AddedColumns;

    internal SqliteTypeAffinityContext(SqlCatalog catalog, SqlColumnLineageResult? lineage)
    {
        Catalog = catalog;
        Lineage = lineage;
        Entries = new List<SqliteTypeAffinityEntry>();
        AddedColumns = new List<SqlCatalogColumn>();
    }

    internal SqliteTypeAffinityResult AnalyzeScriptNodes(SqlScript script)
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        AddLiteralEntries(walker.Literals);
        AddExpressionEntries(walker.Expressions);
        AddLineageEntries();
        return new SqliteTypeAffinityResult(script, Catalog, Entries.ToArray());
    }

    internal void AddLiteralEntries(List<SqlLiteral> literals)
    {
        int index = 0;
        while (index < literals.Count)
        {
            SqlLiteral literal = literals[index];
            Entries.Add(new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.Literal, GetLiteralAffinityCore(literal), SqliteTypeAffinityKind.Unknown, literal, null, null, null, null, null));
            index = index + 1;
        }
    }

    internal void AddExpressionEntries(List<SqlExpression> expressions)
    {
        int index = 0;
        while (index < expressions.Count)
        {
            SqlExpression expression = expressions[index];
            Entries.Add(new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.Expression, InferExpressionCore(expression), SqliteTypeAffinityKind.Unknown, null, null, expression, null, null, null));
            index = index + 1;
        }
    }

    internal void AddLineageEntries()
    {
        if (Lineage == null)
        {
            return;
        }

        int lineageIndex = 0;
        while (lineageIndex < Lineage.Entries.Length)
        {
            SqlColumnLineageEntry lineage = Lineage.Entries[lineageIndex];
            AddLineageSourceColumns(lineage);
            SqliteTypeAffinityKind affinity = InferLineageAffinity(lineage);
            if (lineage.ResultColumn != null)
            {
                Entries.Add(new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.ResultColumn, affinity, SqliteTypeAffinityKind.Unknown, null, null, lineage.Expression, lineage.ResultColumn, null, lineage));
            }
            else
            {
                SqliteTypeAffinityKind targetAffinity = GetTargetAffinity(lineage);
                Entries.Add(new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.Assignment, affinity, targetAffinity, null, null, lineage.Expression, null, lineage.Assignment, lineage));
            }

            lineageIndex = lineageIndex + 1;
        }
    }

    internal void AddLineageSourceColumns(SqlColumnLineageEntry lineage)
    {
        int index = 0;
        while (index < lineage.Sources.Length)
        {
            SqlCatalogColumn? column = lineage.Sources[index].Column;
            if (column != null)
            {
                AddCatalogColumnEntry(column);
            }

            index = index + 1;
        }

        if (lineage.TargetTable != null)
        {
            SqlCatalogColumn? targetColumn = FindTargetColumn(lineage);
            if (targetColumn != null)
            {
                AddCatalogColumnEntry(targetColumn);
            }
        }
    }

    internal void AddCatalogColumnEntry(SqlCatalogColumn column)
    {
        int index = 0;
        while (index < AddedColumns.Count)
        {
            if (AddedColumns[index] == column)
            {
                return;
            }

            index = index + 1;
        }

        AddedColumns.Add(column);
        Entries.Add(new SqliteTypeAffinityEntry(SqliteTypeAffinityTargetKind.CatalogColumn, GetColumnAffinityCore(column), SqliteTypeAffinityKind.Unknown, null, column, null, null, null, null));
    }

    internal SqliteTypeAffinityKind InferLineageAffinity(SqlColumnLineageEntry lineage)
    {
        if (lineage.Expression != null)
        {
            return InferExpressionCore(lineage.Expression);
        }

        return GetSourcesAffinity(lineage.Sources);
    }

    internal SqliteTypeAffinityKind GetTargetAffinity(SqlColumnLineageEntry lineage)
    {
        SqlCatalogColumn? column = FindTargetColumn(lineage);
        if (column == null)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        return GetColumnAffinityCore(column);
    }

    internal SqlCatalogColumn? FindTargetColumn(SqlColumnLineageEntry lineage)
    {
        if (lineage.TargetTable == null || lineage.OutputName.Length == 0)
        {
            return null;
        }

        return lineage.TargetTable.FindColumn(lineage.OutputName, Catalog.CaseSensitiveNames);
    }

    internal SqliteTypeAffinityKind InferExpressionCore(SqlExpression expression)
    {
        if (expression.Kind == SqlExpressionKind.Literal && expression.Literal != null)
        {
            return GetLiteralAffinityCore(expression.Literal);
        }

        if (expression.Kind == SqlExpressionKind.Name && expression.Name != null)
        {
            return ResolveNameAffinity(expression.Name);
        }

        if (expression.Kind == SqlExpressionKind.Parameter || expression.Kind == SqlExpressionKind.Unknown || expression.Kind == SqlExpressionKind.Wildcard || expression.Kind == SqlExpressionKind.Array || expression.Kind == SqlExpressionKind.RowValue)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        if (expression.Kind == SqlExpressionKind.Parenthesized || expression.Kind == SqlExpressionKind.Collate || expression.Kind == SqlExpressionKind.Window)
        {
            return expression.Operand == null ? SqliteTypeAffinityKind.Unknown : InferExpressionCore(expression.Operand);
        }

        if (expression.Kind == SqlExpressionKind.Cast)
        {
            string? typeName = expression.CastData == null ? expression.TypeName : expression.CastData.TypeName;
            return typeName == null ? SqliteTypeAffinityKind.Unknown : DeriveDeclaredTypeAffinity(typeName);
        }

        if (expression.Kind == SqlExpressionKind.FunctionCall)
        {
            return InferFunctionAffinity(expression);
        }

        if (expression.Kind == SqlExpressionKind.Unary)
        {
            if (expression.UnaryOperator == SqlUnaryOperatorKind.Not)
            {
                return SqliteTypeAffinityKind.Integer;
            }

            return SqliteTypeAffinityKind.Numeric;
        }

        if (expression.Kind == SqlExpressionKind.Binary || expression.Kind == SqlExpressionKind.JsonOperator)
        {
            return InferBinaryAffinity(expression);
        }

        if (expression.Kind == SqlExpressionKind.Is || expression.Kind == SqlExpressionKind.Distinctness || expression.Kind == SqlExpressionKind.Like || expression.Kind == SqlExpressionKind.Between || expression.Kind == SqlExpressionKind.In || expression.Kind == SqlExpressionKind.Exists)
        {
            return SqliteTypeAffinityKind.Integer;
        }

        if (expression.Kind == SqlExpressionKind.Case)
        {
            return GetMatchingArgumentAffinity(expression.Arguments);
        }

        if (expression.Kind == SqlExpressionKind.Subquery && expression.Query != null)
        {
            return InferQueryOutputAffinity(expression.Query);
        }

        if (expression.Kind == SqlExpressionKind.Subscript)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        if (expression.Kind == SqlExpressionKind.Extract)
        {
            return SqliteTypeAffinityKind.Numeric;
        }

        return SqliteTypeAffinityKind.Unknown;
    }

    internal SqliteTypeAffinityKind InferBinaryAffinity(SqlExpression expression)
    {
        if (expression.BinaryOperator == SqlBinaryOperatorKind.Concat)
        {
            return SqliteTypeAffinityKind.Text;
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Equal || expression.BinaryOperator == SqlBinaryOperatorKind.NotEqual || expression.BinaryOperator == SqlBinaryOperatorKind.Less || expression.BinaryOperator == SqlBinaryOperatorKind.LessOrEqual || expression.BinaryOperator == SqlBinaryOperatorKind.Greater || expression.BinaryOperator == SqlBinaryOperatorKind.GreaterOrEqual || expression.BinaryOperator == SqlBinaryOperatorKind.And || expression.BinaryOperator == SqlBinaryOperatorKind.Or)
        {
            return SqliteTypeAffinityKind.Integer;
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Add || expression.BinaryOperator == SqlBinaryOperatorKind.Subtract || expression.BinaryOperator == SqlBinaryOperatorKind.Multiply || expression.BinaryOperator == SqlBinaryOperatorKind.Divide || expression.BinaryOperator == SqlBinaryOperatorKind.Modulo || expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseAnd || expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseOr || expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseXor || expression.BinaryOperator == SqlBinaryOperatorKind.ShiftLeft || expression.BinaryOperator == SqlBinaryOperatorKind.ShiftRight)
        {
            return SqliteTypeAffinityKind.Numeric;
        }

        return SqliteTypeAffinityKind.Unknown;
    }

    internal SqliteTypeAffinityKind InferFunctionAffinity(SqlExpression expression)
    {
        SqlName? functionName = expression.FunctionData == null ? expression.Name : expression.FunctionData.Name;
        if (functionName == null)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        SqlExpression[] arguments = expression.FunctionData == null ? expression.Arguments : expression.FunctionData.Arguments;
        SqlCatalogFunction? function = Catalog.FindFunctionByName(functionName, arguments.Length);
        if (function != null)
        {
            if (function.ReturnAffinity != SqliteTypeAffinityKind.Unknown)
            {
                return function.ReturnAffinity;
            }

            if (function.ReturnTypeName.Length > 0)
            {
                return DeriveDeclaredTypeAffinity(function.ReturnTypeName);
            }
        }

        string name = functionName.GetLastPart().Text;
        if (NameEquals(name, "count") || NameEquals(name, "length") || NameEquals(name, "instr") || NameEquals(name, "changes") || NameEquals(name, "total_changes") || NameEquals(name, "last_insert_rowid") || NameEquals(name, "random"))
        {
            return SqliteTypeAffinityKind.Integer;
        }

        if (NameEquals(name, "avg") || NameEquals(name, "total") || NameEquals(name, "julianday"))
        {
            return SqliteTypeAffinityKind.Real;
        }

        if (NameEquals(name, "lower") || NameEquals(name, "upper") || NameEquals(name, "trim") || NameEquals(name, "ltrim") || NameEquals(name, "rtrim") || NameEquals(name, "substr") || NameEquals(name, "substring") || NameEquals(name, "printf") || NameEquals(name, "format") || NameEquals(name, "quote") || NameEquals(name, "hex") || NameEquals(name, "replace") || NameEquals(name, "date") || NameEquals(name, "time") || NameEquals(name, "datetime") || NameEquals(name, "strftime"))
        {
            return SqliteTypeAffinityKind.Text;
        }

        if (NameEquals(name, "coalesce") || NameEquals(name, "ifnull") || NameEquals(name, "nullif") || NameEquals(name, "min") || NameEquals(name, "max"))
        {
            return GetMatchingArgumentAffinity(arguments);
        }

        if (NameEquals(name, "abs") || NameEquals(name, "round"))
        {
            return SqliteTypeAffinityKind.Numeric;
        }

        return SqliteTypeAffinityKind.Unknown;
    }

    internal SqliteTypeAffinityKind GetMatchingArgumentAffinity(SqlExpression[] arguments)
    {
        SqliteTypeAffinityKind affinity = SqliteTypeAffinityKind.Unknown;
        int index = 0;
        while (index < arguments.Length)
        {
            SqliteTypeAffinityKind current = InferExpressionCore(arguments[index]);
            if (current != SqliteTypeAffinityKind.Unknown)
            {
                if (affinity == SqliteTypeAffinityKind.Unknown)
                {
                    affinity = current;
                }
                else if (affinity != current)
                {
                    return SqliteTypeAffinityKind.Unknown;
                }
            }
            else
            {
                return SqliteTypeAffinityKind.Unknown;
            }

            index = index + 1;
        }

        return affinity;
    }

    internal SqliteTypeAffinityKind InferQueryOutputAffinity(SqlQuery query)
    {
        if (Lineage != null)
        {
            int index = 0;
            while (index < Lineage.Entries.Length)
            {
                SqlColumnLineageEntry entry = Lineage.Entries[index];
                if (entry.Query == query && entry.TargetKind == SqlColumnLineageTargetKind.QueryOutput)
                {
                    return InferLineageAffinity(entry);
                }

                index = index + 1;
            }
        }

        if (query.SelectCores.Length == 0 || query.SelectCores[0].ResultColumns.Length == 0)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        SqlResultColumn column = query.SelectCores[0].ResultColumns[0];
        if (column.Expression == null)
        {
            return SqliteTypeAffinityKind.Unknown;
        }

        return InferExpressionCore(column.Expression);
    }

    internal SqliteTypeAffinityKind ResolveNameAffinity(SqlName name)
    {
        if (Lineage != null)
        {
            List<SqlColumnLineageSource> sources = new List<SqlColumnLineageSource>();
            int entryIndex = 0;
            while (entryIndex < Lineage.Entries.Length)
            {
                int sourceIndex = 0;
                while (sourceIndex < Lineage.Entries[entryIndex].Sources.Length)
                {
                    SqlColumnLineageSource source = Lineage.Entries[entryIndex].Sources[sourceIndex];
                    if (source.Reference == name)
                    {
                        AddUniqueSource(sources, source);
                    }

                    sourceIndex = sourceIndex + 1;
                }

                entryIndex = entryIndex + 1;
            }

            if (sources.Count > 0)
            {
                return GetSourceListAffinity(sources);
            }
        }

        return ResolveCatalogNameAffinity(name);
    }

    internal SqliteTypeAffinityKind ResolveCatalogNameAffinity(SqlName name)
    {
        string columnName = name.GetLastPart().Text;
        string? relationName = name.Parts.Length > 1 ? name.Parts[name.Parts.Length - 2].Text : null;
        SqliteTypeAffinityKind affinity = SqliteTypeAffinityKind.Unknown;
        int matches = 0;
        int schemaIndex = 0;
        while (schemaIndex < Catalog.Schemas.Count)
        {
            SqlCatalogSchema schema = Catalog.Schemas[schemaIndex];
            int tableIndex = 0;
            while (tableIndex < schema.Tables.Count)
            {
                SqlCatalogTable table = schema.Tables[tableIndex];
                if (relationName == null || NameEquals(table.Name, relationName))
                {
                    SqlCatalogColumn? column = table.FindColumn(columnName, Catalog.CaseSensitiveNames);
                    if (column != null)
                    {
                        SqliteTypeAffinityKind current = GetColumnAffinityCore(column);
                        if (matches > 0 && current != affinity)
                        {
                            return SqliteTypeAffinityKind.Unknown;
                        }

                        affinity = current;
                        matches = matches + 1;
                    }
                }

                tableIndex = tableIndex + 1;
            }

            int viewIndex = 0;
            while (viewIndex < schema.Views.Count)
            {
                SqlCatalogView view = schema.Views[viewIndex];
                if (relationName == null || NameEquals(view.Name, relationName))
                {
                    SqlCatalogColumn? column = view.FindColumn(columnName, Catalog.CaseSensitiveNames);
                    if (column != null)
                    {
                        SqliteTypeAffinityKind current = GetColumnAffinityCore(column);
                        if (matches > 0 && current != affinity)
                        {
                            return SqliteTypeAffinityKind.Unknown;
                        }

                        affinity = current;
                        matches = matches + 1;
                    }
                }

                viewIndex = viewIndex + 1;
            }

            schemaIndex = schemaIndex + 1;
        }

        return matches == 0 ? SqliteTypeAffinityKind.Unknown : affinity;
    }

    internal SqliteTypeAffinityKind GetSourcesAffinity(SqlColumnLineageSource[] sources)
    {
        List<SqlColumnLineageSource> list = new List<SqlColumnLineageSource>(sources);
        return GetSourceListAffinity(list);
    }

    internal SqliteTypeAffinityKind GetSourceListAffinity(List<SqlColumnLineageSource> sources)
    {
        SqliteTypeAffinityKind affinity = SqliteTypeAffinityKind.Unknown;
        int index = 0;
        while (index < sources.Count)
        {
            SqlCatalogColumn? column = sources[index].Column;
            if (column == null)
            {
                return SqliteTypeAffinityKind.Unknown;
            }

            SqliteTypeAffinityKind current = GetColumnAffinityCore(column);
            if (current == SqliteTypeAffinityKind.Unknown)
            {
                return SqliteTypeAffinityKind.Unknown;
            }

            if (affinity == SqliteTypeAffinityKind.Unknown)
            {
                affinity = current;
            }
            else if (affinity != current)
            {
                return SqliteTypeAffinityKind.Unknown;
            }

            index = index + 1;
        }

        return affinity;
    }

    internal void AddUniqueSource(List<SqlColumnLineageSource> sources, SqlColumnLineageSource source)
    {
        int index = 0;
        while (index < sources.Count)
        {
            if (sources[index].Table == source.Table && sources[index].View == source.View && sources[index].Column == source.Column)
            {
                return;
            }

            index = index + 1;
        }

        sources.Add(source);
    }

    internal bool NameEquals(string left, string right)
    {
        if (Catalog.CaseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static SqliteTypeAffinityKind GetLiteralAffinityCore(SqlLiteral literal)
    {
        if (literal.Kind == SqlLiteralKind.Integer || literal.Kind == SqlLiteralKind.Boolean)
        {
            return SqliteTypeAffinityKind.Integer;
        }

        if (literal.Kind == SqlLiteralKind.Decimal)
        {
            return SqliteTypeAffinityKind.Real;
        }

        if (literal.Kind == SqlLiteralKind.String || literal.Kind == SqlLiteralKind.CurrentDate || literal.Kind == SqlLiteralKind.CurrentTime || literal.Kind == SqlLiteralKind.CurrentTimestamp)
        {
            return SqliteTypeAffinityKind.Text;
        }

        if (literal.Kind == SqlLiteralKind.Blob)
        {
            return SqliteTypeAffinityKind.Blob;
        }

        return SqliteTypeAffinityKind.Unknown;
    }

    internal static SqliteTypeAffinityKind GetColumnAffinityCore(SqlCatalogColumn column)
    {
        if (column.Affinity != SqliteTypeAffinityKind.Unknown)
        {
            return column.Affinity;
        }

        return DeriveDeclaredTypeAffinity(column.TypeName);
    }

    internal static SqliteTypeAffinityKind DeriveDeclaredTypeAffinity(string typeName)
    {
        string upper = typeName.ToUpperInvariant();
        if (upper.Contains("INT", StringComparison.Ordinal))
        {
            return SqliteTypeAffinityKind.Integer;
        }

        if (upper.Contains("CHAR", StringComparison.Ordinal) || upper.Contains("CLOB", StringComparison.Ordinal) || upper.Contains("TEXT", StringComparison.Ordinal))
        {
            return SqliteTypeAffinityKind.Text;
        }

        if (upper.Length == 0 || upper.Contains("BLOB", StringComparison.Ordinal))
        {
            return SqliteTypeAffinityKind.Blob;
        }

        if (upper.Contains("REAL", StringComparison.Ordinal) || upper.Contains("FLOA", StringComparison.Ordinal) || upper.Contains("DOUB", StringComparison.Ordinal))
        {
            return SqliteTypeAffinityKind.Real;
        }

        return SqliteTypeAffinityKind.Numeric;
    }
}
