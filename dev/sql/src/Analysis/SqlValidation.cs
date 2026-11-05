using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Validates parsed SQL against a catalog and reports semantic diagnostics without changing the syntax tree.</summary>
/// <remarks>Normal SQL syntax problems are represented as diagnostics where the operation returns a result; argument and configuration errors are reported as exceptions.</remarks>
internal static class SqlValidation
{
    internal static SqlValidationResult Validate(SqlScript script, SqlCatalog catalog)
    {
        return Validate(script, catalog, SqlValidationOptions.CreateDefault());
    }

    internal static SqlValidationResult Validate(SqlScript script, SqlCatalog catalog, SqlValidationOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        SqlValidationContext context = new SqlValidationContext(catalog, options.Copy(), diagnostics);
        int index = 0;
        while (index < script.Statements.Length)
        {
            context.ValidateStatement(script.Statements[index]);
            index = index + 1;
        }

        return new SqlValidationResult(script, catalog, options, diagnostics);
    }
}

internal class SqlValidationContext
{
    internal SqlCatalog Catalog;
    internal SqlValidationOptions Options;
    internal SqlDiagnosticBag Diagnostics;

    internal SqlValidationContext(SqlCatalog catalog, SqlValidationOptions options, SqlDiagnosticBag diagnostics)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        Catalog = catalog;
        Options = options;
        Diagnostics = diagnostics;
    }

    internal void ValidateStatement(SqlStatement? statement)
    {
        if (statement == null)
        {
            return;
        }

        if (statement.Kind == SqlStatementKind.Select)
        {
            ValidateQuery(statement.Query);
            return;
        }

        if (statement.Kind == SqlStatementKind.Explain)
        {
            ValidateExplain(statement);
            return;
        }

        if (statement.Kind == SqlStatementKind.Insert)
        {
            ValidateInsert(statement.Insert);
            return;
        }

        if (statement.Kind == SqlStatementKind.Update)
        {
            ValidateUpdate(statement.Update);
            return;
        }

        if (statement.Kind == SqlStatementKind.Delete)
        {
            ValidateDelete(statement.Delete);
            return;
        }

        if (statement.Kind == SqlStatementKind.Merge)
        {
            ValidateMerge(statement.Merge);
            return;
        }

        ValidateSchemaStatement(statement);
    }

    internal void ValidateQuery(SqlQuery? query)
    {
        ValidateQueryInScope(query, null);
    }

    internal void ValidateQueryInScope(SqlQuery? query, SqlValidationScope? outerScope)
    {
        if (query == null)
        {
            return;
        }

        SqlValidationScope queryScope = new SqlValidationScope(outerScope);
        int index = 0;
        if (query.IsRecursive)
        {
            RegisterCommonTableExpressions(query, queryScope);
            while (index < query.CommonTableExpressions.Length)
            {
                ValidateQueryInScope(query.CommonTableExpressions[index].Query, queryScope);
                index = index + 1;
            }
        }
        else
        {
            while (index < query.CommonTableExpressions.Length)
            {
                ValidateQueryInScope(query.CommonTableExpressions[index].Query, queryScope);
                RegisterSingleCommonTableExpression(query.CommonTableExpressions[index], queryScope);
                index = index + 1;
            }
        }

        SqlValidationScope? firstCoreScope = null;
        index = 0;
        while (index < query.SelectCores.Length)
        {
            SqlValidationScope coreScope = ValidateSelectCore(query.SelectCores[index], queryScope);
            if (firstCoreScope == null)
            {
                firstCoreScope = coreScope;
            }

            index = index + 1;
        }

        SqlValidationScope tailScope = firstCoreScope == null ? queryScope : firstCoreScope;
        ValidateOrderingTerms(query.OrderBy, tailScope);
        if (query.Limit != null)
        {
            ValidateExpression(query.Limit.LimitExpression, tailScope);
            ValidateExpression(query.Limit.OffsetExpression, tailScope);
        }

        if (query.OffsetFetch != null)
        {
            ValidateExpression(query.OffsetFetch.Offset, tailScope);
            ValidateExpression(query.OffsetFetch.Fetch, tailScope);
        }
    }

    internal void RegisterCommonTableExpressions(SqlQuery query, SqlValidationScope scope)
    {
        int index = 0;
        while (index < query.CommonTableExpressions.Length)
        {
            RegisterSingleCommonTableExpression(query.CommonTableExpressions[index], scope);
            index = index + 1;
        }
    }

    internal void RegisterSingleCommonTableExpression(SqlCommonTableExpression cte, SqlValidationScope scope)
    {
        if (FindLocalCte(scope, cte.Name) != null && Options.ReportDuplicateAliases)
        {
            AddDuplicateAlias(cte.Name);
        }

        scope.CommonTableExpressions.Add(new SqlValidationCteBinding(cte.Name, CreateCteColumnSet(cte)));
    }

    internal SqlValidationCteBinding? FindLocalCte(SqlValidationScope scope, SqlName name)
    {
        string text = name.GetLastPart().Text;
        int index = 0;
        while (index < scope.CommonTableExpressions.Count)
        {
            if (NamesEqual(scope.CommonTableExpressions[index].Name.GetLastPart().Text, text))
            {
                return scope.CommonTableExpressions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal SqlValidationCteBinding? FindVisibleCte(SqlValidationScope scope, SqlName name)
    {
        string text = name.GetLastPart().Text;
        SqlValidationScope? current = scope;
        while (current != null)
        {
            int index = 0;
            while (index < current.CommonTableExpressions.Count)
            {
                if (NamesEqual(current.CommonTableExpressions[index].Name.GetLastPart().Text, text))
                {
                    return current.CommonTableExpressions[index];
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal SqlValidationColumnSet CreateCteColumnSet(SqlCommonTableExpression cte)
    {
        if (cte.ColumnNames.Length > 0)
        {
            string[] names = new string[cte.ColumnNames.Length];
            int index = 0;
            while (index < cte.ColumnNames.Length)
            {
                names[index] = cte.ColumnNames[index].GetLastPart().Text;
                index = index + 1;
            }

            return new SqlValidationColumnSet(names, true);
        }

        return GetQueryOutputColumns(cte.Query);
    }

    internal SqlValidationColumnSet GetQueryOutputColumns(SqlQuery query)
    {
        if (query.SelectCores.Length == 0)
        {
            return new SqlValidationColumnSet(new string[0], false);
        }

        SqlResultColumn[] resultColumns = query.SelectCores[0].ResultColumns;
        string[] names = new string[resultColumns.Length];
        bool known = true;
        int index = 0;
        while (index < resultColumns.Length)
        {
            SqlResultColumn resultColumn = resultColumns[index];
            if (resultColumn.Alias != null)
            {
                names[index] = resultColumn.Alias.GetLastPart().Text;
            }
            else if (!resultColumn.IsWildcard && resultColumn.Expression != null && resultColumn.Expression.Kind == SqlExpressionKind.Name && resultColumn.Expression.Name != null)
            {
                names[index] = resultColumn.Expression.Name.GetLastPart().Text;
            }
            else
            {
                names[index] = "";
                known = false;
            }

            index = index + 1;
        }

        return new SqlValidationColumnSet(names, known);
    }

    internal SqlValidationScope ValidateSelectCore(SqlSelectCore core, SqlValidationScope outerScope)
    {
        SqlValidationScope scope = new SqlValidationScope(outerScope);
        int index = 0;
        while (index < core.From.Length)
        {
            AddTableReference(scope, core.From[index]);
            index = index + 1;
        }

        index = 0;
        while (index < core.ResultColumns.Length)
        {
            ValidateResultColumn(core.ResultColumns[index], scope);
            index = index + 1;
        }

        index = 0;
        while (index < core.Values.Length)
        {
            ValidateExpressionArray(core.Values[index], scope);
            index = index + 1;
        }

        ValidateExpression(core.Where, scope);
        ValidateExpressionArray(core.GroupBy, scope);
        ValidateExpression(core.Having, scope);
        if (core.Top != null)
        {
            ValidateExpression(core.Top.Quantity, scope);
        }

        index = 0;
        while (index < core.Windows.Length)
        {
            ValidateWindowDefinition(core.Windows[index], scope);
            index = index + 1;
        }

        AddResultAliases(core.ResultColumns, scope);
        return scope;
    }

    internal void AddResultAliases(SqlResultColumn[] resultColumns, SqlValidationScope scope)
    {
        int index = 0;
        while (index < resultColumns.Length)
        {
            SqlName? alias = resultColumns[index].Alias;
            if (alias != null)
            {
                scope.OutputAliases.Add(alias.GetLastPart().Text);
            }

            index = index + 1;
        }
    }

    internal void ValidateResultColumn(SqlResultColumn resultColumn, SqlValidationScope scope)
    {
        if (resultColumn.IsWildcard)
        {
            ValidateWildcardQualifier(resultColumn.Qualifier, scope);
        }
        else
        {
            ValidateExpression(resultColumn.Expression, scope);
        }
    }

    internal void AddTableReference(SqlValidationScope scope, SqlTableReference table)
    {
        int firstBindingIndex = scope.Bindings.Count;
        if (table.Kind == SqlTableReferenceKind.Named && table.Name != null)
        {
            AddNamedTableBinding(scope, table);
        }
        else if (table.Kind == SqlTableReferenceKind.Subquery)
        {
            bool isLateral = table.SourceData != null && table.SourceData.IsLateral;
            SqlValidationScope? queryOuter = isLateral ? scope : scope.Parent;
            ValidateQueryInScope(table.Query, queryOuter);
            SqlValidationColumnSet columns = table.Query == null ? new SqlValidationColumnSet(new string[0], false) : GetQueryOutputColumns(table.Query);
            AddDerivedTableBinding(scope, table, CreateTableAliasColumnSet(table, columns));
        }
        else if (table.Kind == SqlTableReferenceKind.Values)
        {
            AddDerivedTableBinding(scope, table, CreateTableAliasColumnSet(table, new SqlValidationColumnSet(new string[0], false)));
            if (table.SourceData != null)
            {
                int rowIndex = 0;
                while (rowIndex < table.SourceData.Values.Length)
                {
                    ValidateExpressionArray(table.SourceData.Values[rowIndex], scope);
                    rowIndex = rowIndex + 1;
                }
            }
        }
        else if (table.Kind == SqlTableReferenceKind.Function)
        {
            ValidateExpression(table.FunctionExpression, scope);
            AddDerivedTableBinding(scope, table, CreateTableAliasColumnSet(table, new SqlValidationColumnSet(new string[0], false)));
        }
        else if (table.Kind == SqlTableReferenceKind.Parenthesized && table.Inner != null)
        {
            AddTableReference(scope, table.Inner);
            ApplyParenthesizedAlias(scope, firstBindingIndex, table);
        }

        int index = 0;
        while (index < table.Joins.Length)
        {
            AddTableReference(scope, table.Joins[index].Right);
            ValidateExpression(table.Joins[index].OnExpression, scope);
            index = index + 1;
        }
    }

    internal void ApplyParenthesizedAlias(SqlValidationScope scope, int firstBindingIndex, SqlTableReference table)
    {
        if (table.Alias == null)
        {
            return;
        }

        int index = firstBindingIndex;
        while (index < scope.Bindings.Count)
        {
            scope.Bindings[index].Alias = table.Alias;
            if (table.ColumnAliases.Length > 0)
            {
                scope.Bindings[index].Columns = CreateTableAliasColumnSet(table, scope.Bindings[index].Columns);
            }

            index = index + 1;
        }
    }

    internal void AddNamedTableBinding(SqlValidationScope scope, SqlTableReference table)
    {
        SqlName? tableName = table.Name;
        if (tableName == null)
        {
            return;
        }

        SqlValidationCteBinding? cte = FindVisibleCte(scope, tableName);
        if (cte != null)
        {
            AddBinding(scope, new SqlValidationTableBinding(tableName, table.Alias, null, null, CreateTableAliasColumnSet(table, cte.Columns)));
            return;
        }

        SqlCatalogTable? catalogTable = Catalog.FindTableByName(tableName);
        SqlCatalogView? catalogView = null;
        if (catalogTable == null)
        {
            catalogView = Catalog.FindViewByName(tableName);
        }

        if (catalogTable == null && catalogView == null)
        {
            AddUnknownTable(tableName);
        }

        SqlValidationColumnSet columns = CreateTableAliasColumnSet(table, CreateCatalogColumnSet(catalogTable, catalogView));
        AddBinding(scope, new SqlValidationTableBinding(tableName, table.Alias, catalogTable, catalogView, columns));
    }

    internal SqlValidationColumnSet CreateCatalogColumnSet(SqlCatalogTable? table, SqlCatalogView? view)
    {
        if (table != null)
        {
            string[] tableColumns = new string[table.Columns.Count];
            int tableIndex = 0;
            while (tableIndex < table.Columns.Count)
            {
                tableColumns[tableIndex] = table.Columns[tableIndex].Name;
                tableIndex = tableIndex + 1;
            }

            return new SqlValidationColumnSet(tableColumns, true);
        }

        if (view != null)
        {
            string[] viewColumns = new string[view.Columns.Count];
            int viewIndex = 0;
            while (viewIndex < view.Columns.Count)
            {
                viewColumns[viewIndex] = view.Columns[viewIndex].Name;
                viewIndex = viewIndex + 1;
            }

            return new SqlValidationColumnSet(viewColumns, true);
        }

        return new SqlValidationColumnSet(new string[0], false);
    }

    internal SqlValidationColumnSet CreateTableAliasColumnSet(SqlTableReference table, SqlValidationColumnSet fallback)
    {
        if (table.ColumnAliases.Length == 0)
        {
            return fallback;
        }

        string[] names = new string[table.ColumnAliases.Length];
        int index = 0;
        while (index < table.ColumnAliases.Length)
        {
            names[index] = table.ColumnAliases[index].GetLastPart().Text;
            index = index + 1;
        }

        return new SqlValidationColumnSet(names, true);
    }

    internal void AddDerivedTableBinding(SqlValidationScope scope, SqlTableReference table, SqlValidationColumnSet columns)
    {
        AddBinding(scope, new SqlValidationTableBinding(null, table.Alias, null, null, columns));
    }

    internal void AddBinding(SqlValidationScope scope, SqlValidationTableBinding binding)
    {
        string? qualifier = binding.GetQualifierText();
        if (qualifier != null && Options.ReportDuplicateAliases)
        {
            int index = 0;
            while (index < scope.Bindings.Count)
            {
                string? existingQualifier = scope.Bindings[index].GetQualifierText();
                if (existingQualifier != null && NamesEqual(existingQualifier, qualifier))
                {
                    SqlName? diagnosticName = binding.Alias == null ? binding.Name : binding.Alias;
                    if (diagnosticName != null)
                    {
                        AddDuplicateAlias(diagnosticName);
                    }

                    break;
                }

                index = index + 1;
            }
        }

        scope.Bindings.Add(binding);
    }

    internal void ValidateInsert(SqlInsertStatement? insert)
    {
        if (insert == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(insert.TableName);
        ValidateCatalogColumnNames(insert.ColumnNames, table);
        SqlValidationScope targetScope = CreateSingleTableScope(insert.TableName, insert.Alias, table);
        int index = 0;
        while (index < insert.Values.Length)
        {
            ValidateExpressionArray(insert.Values[index], targetScope);
            index = index + 1;
        }

        ValidateQueryInScope(insert.SourceQuery, null);
        index = 0;
        while (index < insert.UpsertClauses.Length)
        {
            ValidateUpsert(insert.UpsertClauses[index], table, targetScope);
            index = index + 1;
        }

        ValidateReturning(insert.Returning, targetScope);
    }

    internal void ValidateUpdate(SqlUpdateStatement? update)
    {
        if (update == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(update.TableName);
        ValidateCatalogColumnNames(update.AssignmentColumns, table);
        ValidateIndexReference(update.IndexedBy, table);
        SqlValidationScope scope = CreateSingleTableScope(update.TableName, update.Alias, table);
        int index = 0;
        while (index < update.From.Length)
        {
            AddTableReference(scope, update.From[index]);
            index = index + 1;
        }

        ValidateExpressionArray(update.AssignmentExpressions, scope);
        ValidateExpression(update.Where, scope);
        ValidateReturning(update.Returning, scope);
        ValidateOrderingTerms(update.OrderBy, scope);
        if (update.Limit != null)
        {
            ValidateExpression(update.Limit.LimitExpression, scope);
            ValidateExpression(update.Limit.OffsetExpression, scope);
        }
    }

    internal void ValidateDelete(SqlDeleteStatement? delete)
    {
        if (delete == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(delete.TableName);
        ValidateIndexReference(delete.IndexedBy, table);
        SqlValidationScope scope = CreateSingleTableScope(delete.TableName, delete.Alias, table);
        ValidateExpression(delete.Where, scope);
        ValidateReturning(delete.Returning, scope);
        ValidateOrderingTerms(delete.OrderBy, scope);
        if (delete.Limit != null)
        {
            ValidateExpression(delete.Limit.LimitExpression, scope);
            ValidateExpression(delete.Limit.OffsetExpression, scope);
        }
    }

    internal void ValidateMerge(SqlMergeStatement? merge)
    {
        if (merge == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(merge.TableName);
        SqlValidationScope scope = CreateSingleTableScope(merge.TableName, merge.Alias, table);
        AddTableReference(scope, merge.Source);
        ValidateExpression(merge.Condition, scope);
        int clauseIndex = 0;
        while (clauseIndex < merge.Clauses.Length)
        {
            SqlMergeWhenClause clause = merge.Clauses[clauseIndex];
            ValidateExpression(clause.Condition, scope);
            ValidateCatalogColumnNames(clause.InsertColumns, table);
            ValidateExpressionArray(clause.InsertValues, scope);
            int assignmentIndex = 0;
            while (assignmentIndex < clause.Assignments.Length)
            {
                ValidateCatalogColumnNames(clause.Assignments[assignmentIndex].Columns, table);
                ValidateExpression(clause.Assignments[assignmentIndex].Value, scope);
                assignmentIndex = assignmentIndex + 1;
            }

            clauseIndex = clauseIndex + 1;
        }

        ValidateReturning(merge.Returning, scope);
    }

    internal void ValidateExplain(SqlStatement statement)
    {
        if (statement.Explain != null)
        {
            ValidateStatement(statement.Explain.Statement);
        }
    }

    internal void ValidateSchemaStatement(SqlStatement statement)
    {
        if (statement.Kind == SqlStatementKind.CreateTable)
        {
            ValidateCreateTable(statement.CreateTable);
            return;
        }

        if (statement.Kind == SqlStatementKind.AlterTable)
        {
            ValidateAlterTable(statement.AlterTable);
            return;
        }

        if (statement.Kind == SqlStatementKind.DropTable)
        {
            ValidateDropTable(statement.DropTable);
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateIndex)
        {
            ValidateCreateIndex(statement.CreateIndex);
            return;
        }

        if (statement.Kind == SqlStatementKind.DropIndex)
        {
            ValidateDropIndex(statement.DropIndex);
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateView)
        {
            if (statement.CreateView != null)
            {
                ValidateQuery(statement.CreateView.Query);
            }

            return;
        }

        if (statement.Kind == SqlStatementKind.DropView)
        {
            ValidateDropView(statement.DropView);
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateTrigger)
        {
            ValidateCreateTrigger(statement.CreateTrigger);
        }
    }

    internal void ValidateCreateTable(SqlTableDefinition? definition)
    {
        if (definition == null)
        {
            return;
        }

        SqlCatalogTable localTable = new SqlCatalogTable(definition.Name.GetLastPart().Text);
        int columnIndex = 0;
        while (columnIndex < definition.Columns.Length)
        {
            SqlColumnDefinition column = definition.Columns[columnIndex];
            localTable.Columns.Add(new SqlCatalogColumn(column.Name.GetLastPart().Text, column.TypeName));
            columnIndex = columnIndex + 1;
        }

        SqlValidationScope scope = CreateSingleTableScope(definition.Name, null, localTable);
        SqlValidationScope defaultScope = new SqlValidationScope(null);
        columnIndex = 0;
        while (columnIndex < definition.Columns.Length)
        {
            SqlColumnDefinition column = definition.Columns[columnIndex];
            ValidateExpression(column.DefaultExpression, defaultScope);
            ValidateExpression(column.GeneratedExpression, scope);
            int constraintIndex = 0;
            while (constraintIndex < column.Constraints.Length)
            {
                ValidateColumnConstraint(column.Constraints[constraintIndex], scope);
                constraintIndex = constraintIndex + 1;
            }

            columnIndex = columnIndex + 1;
        }

        int tableConstraintIndex = 0;
        while (tableConstraintIndex < definition.Constraints.Length)
        {
            ValidateTableConstraint(definition.Constraints[tableConstraintIndex], localTable, scope);
            tableConstraintIndex = tableConstraintIndex + 1;
        }

        ValidateQuery(definition.Query);
    }

    internal void ValidateColumnConstraint(SqlColumnConstraint constraint, SqlValidationScope scope)
    {
        ValidateExpression(constraint.Expression, scope);
        ValidateCollationName(constraint.CollationName);
        ValidateForeignKeyReference(constraint.References);
    }

    internal void ValidateTableConstraint(SqlTableConstraint constraint, SqlCatalogTable table, SqlValidationScope scope)
    {
        ValidateCatalogColumnNames(constraint.Columns, table);
        ValidateOrderingTerms(constraint.IndexedColumns, scope);
        ValidateExpressionArray(constraint.Expressions, scope);
        ValidateForeignKeyReference(constraint.References);
    }

    internal void ValidateForeignKeyReference(SqlForeignKeyReference? reference)
    {
        if (reference == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(reference.TableName);
        ValidateCatalogColumnNames(reference.ColumnNames, table);
    }

    internal void ValidateAlterTable(SqlAlterTableDefinition? definition)
    {
        if (definition == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(definition.TableName);
        if (definition.ColumnName != null && table != null)
        {
            ValidateCatalogColumnNames(new SqlName[] { definition.ColumnName }, table);
        }

        if (definition.ColumnDefinition != null)
        {
            SqlValidationScope scope = CreateSingleTableScope(definition.TableName, null, table);
            ValidateExpression(definition.ColumnDefinition.DefaultExpression, scope);
            ValidateExpression(definition.ColumnDefinition.GeneratedExpression, scope);
            int index = 0;
            while (index < definition.ColumnDefinition.Constraints.Length)
            {
                ValidateColumnConstraint(definition.ColumnDefinition.Constraints[index], scope);
                index = index + 1;
            }
        }
    }

    internal void ValidateDropTable(SqlDropTableDefinition? definition)
    {
        if (definition == null || definition.IfExists)
        {
            return;
        }

        ValidateTargetTable(definition.Name);
    }

    internal void ValidateCreateIndex(SqlIndexDefinition? indexDefinition)
    {
        if (indexDefinition == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(indexDefinition.TableName);
        SqlValidationScope scope = CreateSingleTableScope(indexDefinition.TableName, null, table);
        ValidateOrderingTerms(indexDefinition.KeyExpressions, scope);
        ValidateExpression(indexDefinition.WhereExpression, scope);
    }

    internal void ValidateDropIndex(SqlDropIndexDefinition? definition)
    {
        if (definition == null || definition.IfExists || !Options.ValidateIndexNames)
        {
            return;
        }

        if (!CatalogContainsIndex(definition.Name, null))
        {
            AddUnknownIndex(definition.Name);
        }
    }

    internal void ValidateDropView(SqlDropViewDefinition? definition)
    {
        if (definition == null || definition.IfExists)
        {
            return;
        }

        if (Catalog.FindViewByName(definition.Name) == null)
        {
            AddDiagnostic("SQLVAL009", "Unknown view '" + definition.Name.GetText() + "'.", definition.Name.Span);
        }
    }

    internal void ValidateCreateTrigger(SqlTriggerDefinition? trigger)
    {
        if (trigger == null)
        {
            return;
        }

        SqlCatalogTable? table = ValidateTargetTable(trigger.TargetName);
        ValidateCatalogColumnNames(trigger.UpdateColumns, table);
        SqlValidationScope scope = CreateSingleTableScope(trigger.TargetName, null, table);
        ValidateExpression(trigger.WhenExpression, scope);
        int index = 0;
        while (index < trigger.Body.Length)
        {
            ValidateStatement(trigger.Body[index]);
            index = index + 1;
        }
    }

    internal void ValidateUpsert(SqlUpsertClause clause, SqlCatalogTable? table, SqlValidationScope scope)
    {
        ValidateOrderingTerms(clause.ConflictTargets, scope);
        ValidateCatalogColumnNames(clause.UpdateColumns, table);
        ValidateExpressionArray(clause.UpdateExpressions, scope);
        ValidateExpression(clause.ConflictWhere, scope);
        ValidateExpression(clause.UpdateWhere, scope);
    }

    internal void ValidateReturning(SqlReturningClause? returning, SqlValidationScope scope)
    {
        if (returning == null)
        {
            return;
        }

        int index = 0;
        while (index < returning.ResultColumns.Length)
        {
            ValidateResultColumn(returning.ResultColumns[index], scope);
            index = index + 1;
        }
    }

    internal void ValidateExpressionArray(SqlExpression[] expressions, SqlValidationScope scope)
    {
        int index = 0;
        while (index < expressions.Length)
        {
            ValidateExpression(expressions[index], scope);
            index = index + 1;
        }
    }

    internal void ValidateExpression(SqlExpression? expression, SqlValidationScope scope)
    {
        if (expression == null)
        {
            return;
        }

        if (expression.Kind == SqlExpressionKind.Name)
        {
            ValidateColumnName(expression.Name, scope);
            return;
        }

        if (expression.Kind == SqlExpressionKind.FunctionCall)
        {
            ValidateFunctionExpression(expression);
        }

        if (expression.Kind == SqlExpressionKind.Collate)
        {
            SqlName? collation = expression.Name;
            if (expression.CollateData != null)
            {
                collation = expression.CollateData.Collation;
            }

            ValidateCollationName(collation);
        }

        ValidateExpression(expression.Operand, scope);
        ValidateExpression(expression.Left, scope);
        ValidateExpression(expression.Right, scope);
        ValidateExpression(expression.Alternate, scope);
        ValidateExpressionArray(expression.Arguments, scope);
        if (!ExpressionChildOwnsQuery(expression))
        {
            ValidateQueryInScope(expression.Query, scope);
        }

        ValidateOrderingTerms(expression.OrderBy, scope);
        ValidateExpression(expression.Filter, scope);
        if (expression.WindowDefinition != null)
        {
            ValidateWindowDefinition(expression.WindowDefinition, scope);
        }
    }

    internal bool ExpressionChildOwnsQuery(SqlExpression expression)
    {
        SqlQuery? query = expression.Query;
        if (query == null)
        {
            return false;
        }

        if (expression.Operand != null && expression.Operand.Query == query)
        {
            return true;
        }

        if (expression.Left != null && expression.Left.Query == query)
        {
            return true;
        }

        if (expression.Right != null && expression.Right.Query == query)
        {
            return true;
        }

        if (expression.Alternate != null && expression.Alternate.Query == query)
        {
            return true;
        }

        int index = 0;
        while (index < expression.Arguments.Length)
        {
            if (expression.Arguments[index].Query == query)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateFunctionExpression(SqlExpression expression)
    {
        if (!Options.ValidateFunctionNames)
        {
            return;
        }

        SqlName? name = expression.Name;
        int argumentCount = expression.Arguments.Length;
        if (expression.FunctionData != null)
        {
            name = expression.FunctionData.Name;
            argumentCount = expression.FunctionData.Arguments.Length;
        }

        if (name != null && Catalog.FindFunctionByName(name, argumentCount) == null)
        {
            AddDiagnostic("SQLVAL006", "Unknown function '" + name.GetText() + "' for " + argumentCount + " arguments.", name.Span);
        }
    }

    internal void ValidateCollationName(SqlName? name)
    {
        if (!Options.ValidateCollationNames || name == null)
        {
            return;
        }

        if (Catalog.FindCollationByName(name) == null)
        {
            AddDiagnostic("SQLVAL007", "Unknown collation '" + name.GetText() + "'.", name.Span);
        }
    }

    internal void ValidateOrderingTerms(SqlOrderingTerm[] orderBy, SqlValidationScope scope)
    {
        int index = 0;
        while (index < orderBy.Length)
        {
            ValidateOrderingTerm(orderBy[index], scope);
            index = index + 1;
        }
    }

    internal void ValidateOrderingTerm(SqlOrderingTerm term, SqlValidationScope scope)
    {
        ValidateExpression(term.Expression, scope);
        ValidateCollationName(term.Collation);
    }

    internal void ValidateWindowDefinition(SqlWindowDefinition window, SqlValidationScope scope)
    {
        ValidateExpressionArray(window.PartitionBy, scope);
        ValidateOrderingTerms(window.OrderBy, scope);
        if (window.Frame != null)
        {
            ValidateExpression(window.Frame.Start.Expression, scope);
            if (window.Frame.End != null)
            {
                ValidateExpression(window.Frame.End.Expression, scope);
            }
        }
    }

    internal void ValidateColumnName(SqlName? name, SqlValidationScope scope)
    {
        if (!Options.ValidateColumnNames || name == null || name.IsEmpty())
        {
            return;
        }

        if (name.GetPartCount() > 1)
        {
            ValidateQualifiedColumn(name, scope);
            return;
        }

        ValidateUnqualifiedColumn(name, scope);
    }

    internal void ValidateQualifiedColumn(SqlName name, SqlValidationScope scope)
    {
        string columnName = name.GetLastPart().Text;
        SqlValidationScope? current = scope;
        while (current != null)
        {
            int index = 0;
            while (index < current.Bindings.Count)
            {
                SqlValidationTableBinding binding = current.Bindings[index];
                if (binding.MatchesColumnQualifier(name, Catalog.CaseSensitiveNames))
                {
                    if (binding.Columns.IsKnown && !ColumnSetContains(binding.Columns, columnName))
                    {
                        AddUnknownColumn(name);
                    }

                    return;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        AddUnknownTableQualifier(name);
    }

    internal void ValidateUnqualifiedColumn(SqlName name, SqlValidationScope scope)
    {
        string columnName = name.GetLastPart().Text;
        bool sawKnownBinding = false;
        SqlValidationScope? current = scope;
        while (current != null)
        {
            int aliasIndex = 0;
            while (aliasIndex < current.OutputAliases.Count)
            {
                if (NamesEqual(current.OutputAliases[aliasIndex], columnName))
                {
                    return;
                }

                aliasIndex = aliasIndex + 1;
            }

            int matchCount = 0;
            bool hasUnknownBinding = false;
            int bindingIndex = 0;
            while (bindingIndex < current.Bindings.Count)
            {
                SqlValidationTableBinding binding = current.Bindings[bindingIndex];
                if (binding.Columns.IsKnown)
                {
                    sawKnownBinding = true;
                    if (ColumnSetContains(binding.Columns, columnName))
                    {
                        matchCount = matchCount + 1;
                    }
                }
                else
                {
                    hasUnknownBinding = true;
                }

                bindingIndex = bindingIndex + 1;
            }

            if (matchCount > 1)
            {
                if (Options.ReportAmbiguousColumns)
                {
                    AddAmbiguousColumn(name);
                }

                return;
            }

            if (matchCount == 1 || hasUnknownBinding)
            {
                return;
            }

            current = current.Parent;
        }

        if (sawKnownBinding)
        {
            AddUnknownColumn(name);
        }
    }

    internal bool ColumnSetContains(SqlValidationColumnSet columns, string name)
    {
        int index = 0;
        while (index < columns.Names.Length)
        {
            if (NamesEqual(columns.Names[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateWildcardQualifier(SqlName? qualifier, SqlValidationScope scope)
    {
        if (qualifier == null)
        {
            return;
        }

        SqlValidationScope? current = scope;
        while (current != null)
        {
            int index = 0;
            while (index < current.Bindings.Count)
            {
                if (current.Bindings[index].MatchesWildcardQualifier(qualifier, Catalog.CaseSensitiveNames))
                {
                    return;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        AddUnknownTableQualifier(qualifier);
    }

    internal SqlCatalogTable? ValidateTargetTable(SqlName name)
    {
        SqlCatalogTable? table = Catalog.FindTableByName(name);
        if (table == null && Options.ValidateTableNames)
        {
            AddUnknownTable(name);
        }

        return table;
    }

    internal void ValidateCatalogColumnNames(SqlName[] names, SqlCatalogTable? table)
    {
        if (!Options.ValidateColumnNames || table == null)
        {
            return;
        }

        int index = 0;
        while (index < names.Length)
        {
            if (table.FindColumn(names[index].GetLastPart().Text, Catalog.CaseSensitiveNames) == null)
            {
                AddUnknownColumn(names[index]);
            }

            index = index + 1;
        }
    }

    internal SqlValidationScope CreateSingleTableScope(SqlName tableName, SqlName? alias, SqlCatalogTable? table)
    {
        SqlValidationScope scope = new SqlValidationScope(null);
        AddBinding(scope, new SqlValidationTableBinding(tableName, alias, table, null, CreateCatalogColumnSet(table, null)));
        return scope;
    }

    internal void ValidateIndexReference(SqlName? indexName, SqlCatalogTable? table)
    {
        if (!Options.ValidateIndexNames || indexName == null || table == null)
        {
            return;
        }

        if (!CatalogContainsIndex(indexName, table))
        {
            AddUnknownIndex(indexName);
        }
    }

    internal bool CatalogContainsIndex(SqlName indexName, SqlCatalogTable? table)
    {
        string name = indexName.GetLastPart().Text;
        int schemaIndex = 0;
        while (schemaIndex < Catalog.Schemas.Count)
        {
            SqlCatalogSchema schema = Catalog.Schemas[schemaIndex];
            bool schemaMatches = indexName.GetPartCount() < 2 || NamesEqual(schema.Name, indexName.GetPartAt(indexName.GetPartCount() - 2).Text);
            bool catalogMatches = true;
            if (indexName.GetPartCount() > 2)
            {
                catalogMatches = schema.CatalogName != null && NamesEqual(schema.CatalogName, indexName.GetPartAt(indexName.GetPartCount() - 3).Text);
            }

            if (schemaMatches && catalogMatches)
            {
                SqlCatalogIndex? schemaIndexEntry = schema.FindIndex(name, Catalog.CaseSensitiveNames);
                if (schemaIndexEntry != null && (table == null || NamesEqual(schemaIndexEntry.TableName, table.Name)))
                {
                    return true;
                }
            }

            schemaIndex = schemaIndex + 1;
        }

        if (table != null)
        {
            int tableIndex = 0;
            while (tableIndex < table.Indexes.Count)
            {
                if (NamesEqual(table.Indexes[tableIndex].Name, name))
                {
                    return true;
                }

                tableIndex = tableIndex + 1;
            }
        }

        return false;
    }

    internal bool NamesEqual(string left, string right)
    {
        if (Catalog.CaseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal void AddUnknownTable(SqlName name)
    {
        if (Options.ValidateTableNames)
        {
            AddDiagnostic("SQLVAL001", "Unknown table '" + name.GetText() + "'.", name.Span);
        }
    }

    internal void AddUnknownColumn(SqlName name)
    {
        AddDiagnostic("SQLVAL002", "Unknown column '" + name.GetText() + "'.", name.Span);
    }

    internal void AddUnknownTableQualifier(SqlName name)
    {
        AddDiagnostic("SQLVAL003", "Unknown table qualifier '" + name.GetText() + "'.", name.Span);
    }

    internal void AddAmbiguousColumn(SqlName name)
    {
        AddDiagnostic("SQLVAL004", "Ambiguous column '" + name.GetText() + "'.", name.Span);
    }

    internal void AddDuplicateAlias(SqlName name)
    {
        AddDiagnostic("SQLVAL005", "Duplicate alias '" + name.GetText() + "'.", name.Span);
    }

    internal void AddUnknownIndex(SqlName name)
    {
        AddDiagnostic("SQLVAL008", "Unknown index '" + name.GetText() + "'.", name.Span);
    }

    internal void AddDiagnostic(string code, string message, SqlTextSpan span)
    {
        Diagnostics.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Error, code, message, span, null);
    }
}

internal class SqlValidationScope
{
    internal SqlValidationScope? Parent;
    internal List<SqlValidationTableBinding> Bindings;
    internal List<SqlValidationCteBinding> CommonTableExpressions;
    internal List<string> OutputAliases;

    internal SqlValidationScope(SqlValidationScope? parent)
    {
        Parent = parent;
        Bindings = new List<SqlValidationTableBinding>();
        CommonTableExpressions = new List<SqlValidationCteBinding>();
        OutputAliases = new List<string>();
    }
}

internal class SqlValidationColumnSet
{
    internal string[] Names;
    internal bool IsKnown;

    internal SqlValidationColumnSet(string[] names, bool isKnown)
    {
        if (names == null)
        {
            throw new ArgumentNullException("names");
        }

        Names = names;
        IsKnown = isKnown;
    }
}

internal class SqlValidationCteBinding
{
    internal SqlName Name;
    internal SqlValidationColumnSet Columns;

    internal SqlValidationCteBinding(SqlName name, SqlValidationColumnSet columns)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (columns == null)
        {
            throw new ArgumentNullException("columns");
        }

        Name = name;
        Columns = columns;
    }
}

internal class SqlValidationTableBinding
{
    internal SqlName? Name;
    internal SqlName? Alias;
    internal SqlCatalogTable? Table;
    internal SqlCatalogView? View;
    internal SqlValidationColumnSet Columns;

    internal SqlValidationTableBinding(SqlName? name, SqlName? alias, SqlCatalogTable? table, SqlCatalogView? view, SqlValidationColumnSet columns)
    {
        if (columns == null)
        {
            throw new ArgumentNullException("columns");
        }

        Name = name;
        Alias = alias;
        Table = table;
        View = view;
        Columns = columns;
    }

    internal string? GetQualifierText()
    {
        if (Alias != null)
        {
            return Alias.GetLastPart().Text;
        }

        if (Name != null)
        {
            return Name.GetLastPart().Text;
        }

        return null;
    }

    internal bool MatchesColumnQualifier(SqlName columnName, bool caseSensitiveNames)
    {
        if (columnName.GetPartCount() < 2)
        {
            return false;
        }

        if (Alias != null)
        {
            return columnName.GetPartCount() == 2 && NamesMatch(Alias.GetLastPart().Text, columnName.GetPartAt(columnName.GetPartCount() - 2).Text, caseSensitiveNames);
        }

        if (Name == null || !NamesMatch(Name.GetLastPart().Text, columnName.GetPartAt(columnName.GetPartCount() - 2).Text, caseSensitiveNames))
        {
            return false;
        }

        if (columnName.GetPartCount() > 2)
        {
            string? schemaName = null;
            if (Name.GetPartCount() > 1)
            {
                schemaName = Name.GetPartAt(Name.GetPartCount() - 2).Text;
            }
            else if (Table != null)
            {
                schemaName = Table.SchemaName;
            }
            else if (View != null)
            {
                schemaName = View.SchemaName;
            }

            if (schemaName == null || !NamesMatch(schemaName, columnName.GetPartAt(columnName.GetPartCount() - 3).Text, caseSensitiveNames))
            {
                return false;
            }
        }

        if (columnName.GetPartCount() > 3)
        {
            string? catalogName = null;
            if (Name.GetPartCount() > 2)
            {
                catalogName = Name.GetPartAt(Name.GetPartCount() - 3).Text;
            }
            else if (Table != null)
            {
                catalogName = Table.CatalogName;
            }
            else if (View != null)
            {
                catalogName = View.CatalogName;
            }

            if (catalogName == null || !NamesMatch(catalogName, columnName.GetPartAt(columnName.GetPartCount() - 4).Text, caseSensitiveNames))
            {
                return false;
            }
        }

        return true;
    }

    internal bool MatchesWildcardQualifier(SqlName qualifier, bool caseSensitiveNames)
    {
        if (Alias != null)
        {
            return qualifier.GetPartCount() == 1 && NamesMatch(Alias.GetLastPart().Text, qualifier.GetLastPart().Text, caseSensitiveNames);
        }

        if (Name == null || !NamesMatch(Name.GetLastPart().Text, qualifier.GetLastPart().Text, caseSensitiveNames))
        {
            return false;
        }

        if (qualifier.GetPartCount() > 1)
        {
            string? schemaName = null;
            if (Name.GetPartCount() > 1)
            {
                schemaName = Name.GetPartAt(Name.GetPartCount() - 2).Text;
            }
            else if (Table != null)
            {
                schemaName = Table.SchemaName;
            }
            else if (View != null)
            {
                schemaName = View.SchemaName;
            }

            if (schemaName == null || !NamesMatch(schemaName, qualifier.GetPartAt(qualifier.GetPartCount() - 2).Text, caseSensitiveNames))
            {
                return false;
            }
        }

        return true;
    }

    internal bool NamesMatch(string left, string right, bool caseSensitiveNames)
    {
        if (caseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
