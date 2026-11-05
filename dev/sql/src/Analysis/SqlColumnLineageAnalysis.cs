using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Maps query outputs and DML assignments to their contributing source columns.</summary>
internal static class SqlColumnLineageAnalysis
{
    internal static SqlColumnLineageResult Analyze(SqlScript script, SqlCatalog catalog)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        SqlColumnLineageContext context = new SqlColumnLineageContext(script, catalog);
        context.AnalyzeScript();
        return context.CreateResult();
    }
}

internal class SqlColumnLineageContext
{
    internal SqlScript Script;
    internal SqlCatalog Catalog;
    internal List<SqlColumnLineageEntry> Entries;
    internal SqlDiagnosticBag Diagnostics;
    internal List<SqlLineageQueryResult> QueryResults;

    internal SqlColumnLineageContext(SqlScript script, SqlCatalog catalog)
    {
        Script = script;
        Catalog = catalog;
        Entries = new List<SqlColumnLineageEntry>();
        Diagnostics = new SqlDiagnosticBag();
        QueryResults = new List<SqlLineageQueryResult>();
    }

    internal void AnalyzeScript()
    {
        int index = 0;
        while (index < Script.Statements.Length)
        {
            AnalyzeStatement(Script.Statements[index]);
            index = index + 1;
        }
    }

    internal void AnalyzeStatement(SqlStatement statement)
    {
        if (statement.Kind == SqlStatementKind.Select)
        {
            AnalyzeQuery(statement.Query, null);
            return;
        }

        if (statement.Kind == SqlStatementKind.Insert)
        {
            AnalyzeInsert(statement.Insert);
            return;
        }

        if (statement.Kind == SqlStatementKind.Update)
        {
            AnalyzeUpdate(statement.Update);
            return;
        }

        if (statement.Kind == SqlStatementKind.Delete)
        {
            AnalyzeDelete(statement.Delete);
            return;
        }

        if (statement.Kind == SqlStatementKind.Merge)
        {
            AnalyzeMerge(statement.Merge);
            return;
        }

        if (statement.Kind == SqlStatementKind.Explain && statement.Explain != null && statement.Explain.Statement != null)
        {
            AnalyzeStatement(statement.Explain.Statement);
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateTable && statement.CreateTable != null)
        {
            AnalyzeQuery(statement.CreateTable.Query, null);
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateView && statement.CreateView != null)
        {
            AnalyzeQuery(statement.CreateView.Query, null);
        }
    }

    internal SqlLineageQueryResult? AnalyzeQuery(SqlQuery? query, SqlLineageScope? outerScope)
    {
        if (query == null)
        {
            return null;
        }

        SqlLineageQueryResult? existing = FindQueryResult(query);
        if (existing != null)
        {
            return existing;
        }

        SqlLineageQueryResult result = new SqlLineageQueryResult(query);
        QueryResults.Add(result);
        SqlLineageScope queryScope = new SqlLineageScope(outerScope);
        RegisterCommonTableExpressions(query, queryScope);
        if (query.SelectCores.Length == 0)
        {
            return result;
        }

        SqlLineageScope coreScope = new SqlLineageScope(queryScope);
        AddTableReferences(coreScope, query.SelectCores[0].From);
        AnalyzeResultColumns(query.SelectCores[0].ResultColumns, query, coreScope, SqlColumnLineageTargetKind.QueryOutput, result.Outputs);

        int coreIndex = 1;
        while (coreIndex < query.SelectCores.Length)
        {
            SqlLineageScope compoundScope = new SqlLineageScope(queryScope);
            AddTableReferences(compoundScope, query.SelectCores[coreIndex].From);
            MergeCompoundOutputs(query.SelectCores[coreIndex], compoundScope, result.Outputs);
            coreIndex = coreIndex + 1;
        }

        UpdateQueryEntries(result);

        return result;
    }

    internal SqlLineageQueryResult? FindQueryResult(SqlQuery query)
    {
        int index = 0;
        while (index < QueryResults.Count)
        {
            if (QueryResults[index].Query == query)
            {
                return QueryResults[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void RegisterCommonTableExpressions(SqlQuery query, SqlLineageScope scope)
    {
        int index = 0;
        if (query.IsRecursive)
        {
            while (index < query.CommonTableExpressions.Length)
            {
                SqlCommonTableExpression recursiveCte = query.CommonTableExpressions[index];
                scope.CommonTableExpressions.Add(CreateUnknownCteBinding(recursiveCte));
                index = index + 1;
            }
        }

        index = 0;
        while (index < query.CommonTableExpressions.Length)
        {
            SqlCommonTableExpression cte = query.CommonTableExpressions[index];
            SqlLineageQueryResult? queryResult = AnalyzeQuery(cte.Query, scope);
            SqlLineageBinding binding = CreateOutputBinding(cte.Name, null, queryResult, cte.ColumnNames);
            if (query.IsRecursive)
            {
                scope.CommonTableExpressions[index] = binding;
            }
            else
            {
                scope.CommonTableExpressions.Add(binding);
            }

            index = index + 1;
        }
    }

    internal SqlLineageBinding CreateUnknownCteBinding(SqlCommonTableExpression cte)
    {
        SqlLineageBinding binding = new SqlLineageBinding(cte.Name, null);
        if (cte.ColumnNames.Length == 0)
        {
            binding.ColumnsKnown = false;
            return binding;
        }

        int index = 0;
        while (index < cte.ColumnNames.Length)
        {
            binding.Columns.Add(new SqlLineageColumnBinding(cte.ColumnNames[index].GetLastPart().Text, SqlColumnLineageOriginKind.Unknown, new SqlColumnLineageSource[0]));
            index = index + 1;
        }

        return binding;
    }

    internal void MergeCompoundOutputs(SqlSelectCore core, SqlLineageScope scope, List<SqlLineageOutput> outputs)
    {
        List<SqlLineageOutput> next = new List<SqlLineageOutput>();
        AnalyzeResultColumnsWithoutEntries(core.ResultColumns, scope, next);
        int index = 0;
        while (index < outputs.Count && index < next.Count)
        {
            MergeSources(outputs[index].Sources, next[index].Sources);
            if (outputs[index].OriginKind != SqlColumnLineageOriginKind.Unknown && outputs[index].OriginKind != SqlColumnLineageOriginKind.Ambiguous && next[index].OriginKind != SqlColumnLineageOriginKind.Unknown && next[index].OriginKind != SqlColumnLineageOriginKind.Ambiguous)
            {
                outputs[index].OriginKind = SqlColumnLineageOriginKind.Expression;
            }

            if (next[index].OriginKind == SqlColumnLineageOriginKind.Unknown)
            {
                outputs[index].OriginKind = SqlColumnLineageOriginKind.Unknown;
            }

            if (next[index].OriginKind == SqlColumnLineageOriginKind.Ambiguous)
            {
                outputs[index].OriginKind = SqlColumnLineageOriginKind.Ambiguous;
            }

            index = index + 1;
        }
    }

    internal void UpdateQueryEntries(SqlLineageQueryResult result)
    {
        int outputIndex = 0;
        int entryIndex = 0;
        while (entryIndex < Entries.Count && outputIndex < result.Outputs.Count)
        {
            SqlColumnLineageEntry entry = Entries[entryIndex];
            if (entry.TargetKind == SqlColumnLineageTargetKind.QueryOutput && entry.Query == result.Query)
            {
                SqlLineageOutput output = result.Outputs[outputIndex];
                bool becameUncertain = entry.OriginKind != output.OriginKind && (output.OriginKind == SqlColumnLineageOriginKind.Unknown || output.OriginKind == SqlColumnLineageOriginKind.Ambiguous);
                entry.OriginKind = output.OriginKind;
                entry.Sources = output.Sources.ToArray();
                if (becameUncertain)
                {
                    AddOriginDiagnostic(entry.OriginKind, entry.OutputName, entry.Span);
                }

                outputIndex = outputIndex + 1;
            }

            entryIndex = entryIndex + 1;
        }
    }

    internal void AnalyzeResultColumns(SqlResultColumn[] columns, SqlQuery? query, SqlLineageScope scope, SqlColumnLineageTargetKind targetKind, List<SqlLineageOutput>? outputs)
    {
        int index = 0;
        while (index < columns.Length)
        {
            SqlResultColumn column = columns[index];
            if (column.IsWildcard)
            {
                AnalyzeWildcard(column, query, scope, targetKind, outputs);
            }
            else
            {
                SqlLineageValue value = AnalyzeExpressionValue(column.Expression, scope);
                string outputName = GetOutputName(column, index);
                SqlLineageOutput output = new SqlLineageOutput(outputName, column.Expression, column, value.OriginKind, value.Sources, column.Span);
                if (outputs != null)
                {
                    outputs.Add(output);
                }

                AddEntry(targetKind, value.OriginKind, outputName, null, null, column, null, column.Expression, query, value.Sources, column.Span);
            }

            index = index + 1;
        }
    }

    internal void AnalyzeResultColumnsWithoutEntries(SqlResultColumn[] columns, SqlLineageScope scope, List<SqlLineageOutput> outputs)
    {
        int index = 0;
        while (index < columns.Length)
        {
            SqlResultColumn column = columns[index];
            if (column.IsWildcard)
            {
                ExpandWildcardOutputs(column, scope, outputs);
            }
            else
            {
                SqlLineageValue value = AnalyzeExpressionValue(column.Expression, scope);
                outputs.Add(new SqlLineageOutput(GetOutputName(column, index), column.Expression, column, value.OriginKind, value.Sources, column.Span));
            }

            index = index + 1;
        }
    }

    internal void AnalyzeWildcard(SqlResultColumn column, SqlQuery? query, SqlLineageScope scope, SqlColumnLineageTargetKind targetKind, List<SqlLineageOutput>? outputs)
    {
        List<SqlLineageOutput> expanded = new List<SqlLineageOutput>();
        ExpandWildcardOutputs(column, scope, expanded);
        int index = 0;
        while (index < expanded.Count)
        {
            SqlLineageOutput output = expanded[index];
            if (outputs != null)
            {
                outputs.Add(output);
            }

            AddEntry(targetKind, output.OriginKind, output.Name, null, null, column, null, null, query, output.Sources.ToArray(), column.Span);
            index = index + 1;
        }
    }

    internal void ExpandWildcardOutputs(SqlResultColumn column, SqlLineageScope scope, List<SqlLineageOutput> outputs)
    {
        int bindingIndex = 0;
        while (bindingIndex < scope.Bindings.Count)
        {
            SqlLineageBinding binding = scope.Bindings[bindingIndex];
            if (column.Qualifier == null || BindingMatchesQualifier(binding, column.Qualifier))
            {
                int columnIndex = 0;
                while (columnIndex < binding.Columns.Count)
                {
                    SqlLineageColumnBinding sourceColumn = binding.Columns[columnIndex];
                    outputs.Add(new SqlLineageOutput(sourceColumn.Name, null, column, sourceColumn.OriginKind, CopySources(sourceColumn.Sources), column.Span));
                    columnIndex = columnIndex + 1;
                }
            }

            bindingIndex = bindingIndex + 1;
        }

        if (outputs.Count == 0)
        {
            SqlColumnLineageSource unresolved = new SqlColumnLineageSource(column.Qualifier, null, null, null);
            outputs.Add(new SqlLineageOutput("", null, column, SqlColumnLineageOriginKind.Unknown, new SqlColumnLineageSource[] { unresolved }, column.Span));
        }
    }

    internal string GetOutputName(SqlResultColumn column, int index)
    {
        if (column.Alias != null)
        {
            return column.Alias.GetLastPart().Text;
        }

        if (column.Expression != null && column.Expression.Kind == SqlExpressionKind.Name && column.Expression.Name != null)
        {
            return column.Expression.Name.GetLastPart().Text;
        }

        return "column" + (index + 1).ToString();
    }

    internal void AnalyzeInsert(SqlInsertStatement? insert)
    {
        if (insert == null)
        {
            return;
        }

        SqlCatalogTable? targetTable = Catalog.FindTableByName(insert.TableName);
        SqlLineageScope targetScope = CreateTargetScope(insert.TableName, insert.Alias, targetTable);
        if (insert.SourceQuery != null)
        {
            SqlLineageQueryResult? sourceResult = AnalyzeQuery(insert.SourceQuery, null);
            if (sourceResult != null)
            {
                int index = 0;
                while (index < sourceResult.Outputs.Count)
                {
                    SqlName? targetColumn = GetInsertTargetColumn(insert, index);
                    SqlLineageOutput output = sourceResult.Outputs[index];
                    SqlTextSpan span = targetColumn == null ? output.Span : targetColumn.Span;
                    AddEntry(SqlColumnLineageTargetKind.InsertAssignment, output.OriginKind, GetTargetName(targetColumn, targetTable, index), targetColumn, targetTable, null, null, output.Expression, null, output.Sources.ToArray(), span);
                    index = index + 1;
                }
            }
        }

        int rowIndex = 0;
        while (rowIndex < insert.Values.Length)
        {
            int valueIndex = 0;
            while (valueIndex < insert.Values[rowIndex].Length)
            {
                SqlExpression expression = insert.Values[rowIndex][valueIndex];
                SqlLineageValue value = AnalyzeExpressionValue(expression, null);
                SqlName? targetColumn = GetInsertTargetColumn(insert, valueIndex);
                SqlTextSpan span = targetColumn == null ? expression.Span : targetColumn.Span;
                AddEntry(SqlColumnLineageTargetKind.InsertAssignment, value.OriginKind, GetTargetName(targetColumn, targetTable, valueIndex), targetColumn, targetTable, null, null, expression, null, value.Sources, span);
                valueIndex = valueIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        AnalyzeUpsertAssignments(insert.UpsertClauses, targetScope, targetTable);
        AnalyzeReturning(insert.Returning, targetScope);
    }

    internal SqlName? GetInsertTargetColumn(SqlInsertStatement insert, int index)
    {
        if (index < insert.ColumnNames.Length)
        {
            return insert.ColumnNames[index];
        }

        return null;
    }

    internal string GetTargetName(SqlName? targetColumn, SqlCatalogTable? targetTable, int index)
    {
        if (targetColumn != null)
        {
            return targetColumn.GetLastPart().Text;
        }

        if (targetTable != null && index < targetTable.Columns.Count)
        {
            return targetTable.Columns[index].Name;
        }

        return "";
    }

    internal void AnalyzeUpdate(SqlUpdateStatement? update)
    {
        if (update == null)
        {
            return;
        }

        SqlCatalogTable? targetTable = Catalog.FindTableByName(update.TableName);
        SqlLineageScope scope = CreateTargetScope(update.TableName, update.Alias, targetTable);
        AddTableReferences(scope, update.From);
        AnalyzeAssignments(update.Assignments, scope, targetTable, SqlColumnLineageTargetKind.UpdateAssignment);
        AnalyzeReturning(update.Returning, scope);
    }

    internal void AnalyzeDelete(SqlDeleteStatement? delete)
    {
        if (delete == null)
        {
            return;
        }

        SqlCatalogTable? targetTable = Catalog.FindTableByName(delete.TableName);
        SqlLineageScope scope = CreateTargetScope(delete.TableName, delete.Alias, targetTable);
        AnalyzeReturning(delete.Returning, scope);
    }

    internal void AnalyzeMerge(SqlMergeStatement? merge)
    {
        if (merge == null)
        {
            return;
        }

        SqlCatalogTable? targetTable = Catalog.FindTableByName(merge.TableName);
        SqlLineageScope scope = CreateTargetScope(merge.TableName, merge.Alias, targetTable);
        AddTableReference(scope, merge.Source);
        int clauseIndex = 0;
        while (clauseIndex < merge.Clauses.Length)
        {
            SqlMergeWhenClause clause = merge.Clauses[clauseIndex];
            AnalyzeAssignments(clause.Assignments, scope, targetTable, SqlColumnLineageTargetKind.MergeAssignment);
            int insertIndex = 0;
            while (insertIndex < clause.InsertValues.Length)
            {
                SqlExpression expression = clause.InsertValues[insertIndex];
                SqlLineageValue value = AnalyzeExpressionValue(expression, scope);
                SqlName? targetColumn = insertIndex < clause.InsertColumns.Length ? clause.InsertColumns[insertIndex] : null;
                SqlTextSpan span = targetColumn == null ? expression.Span : targetColumn.Span;
                AddEntry(SqlColumnLineageTargetKind.MergeAssignment, value.OriginKind, GetTargetName(targetColumn, targetTable, insertIndex), targetColumn, targetTable, null, null, expression, null, value.Sources, span);
                insertIndex = insertIndex + 1;
            }

            clauseIndex = clauseIndex + 1;
        }

        AnalyzeReturning(merge.Returning, scope);
    }

    internal void AnalyzeUpsertAssignments(SqlUpsertClause[] clauses, SqlLineageScope scope, SqlCatalogTable? targetTable)
    {
        int index = 0;
        while (index < clauses.Length)
        {
            AnalyzeAssignments(clauses[index].Assignments, scope, targetTable, SqlColumnLineageTargetKind.UpsertAssignment);
            index = index + 1;
        }
    }

    internal void AnalyzeAssignments(SqlAssignment[] assignments, SqlLineageScope scope, SqlCatalogTable? targetTable, SqlColumnLineageTargetKind targetKind)
    {
        int assignmentIndex = 0;
        while (assignmentIndex < assignments.Length)
        {
            SqlAssignment assignment = assignments[assignmentIndex];
            int columnIndex = 0;
            while (columnIndex < assignment.Columns.Length)
            {
                SqlExpression expression = GetAssignmentExpression(assignment, columnIndex);
                SqlLineageValue value = AnalyzeExpressionValue(expression, scope);
                SqlName targetColumn = assignment.Columns[columnIndex];
                AddEntry(targetKind, value.OriginKind, targetColumn.GetLastPart().Text, targetColumn, targetTable, null, assignment, expression, null, value.Sources, assignment.Span);
                columnIndex = columnIndex + 1;
            }

            assignmentIndex = assignmentIndex + 1;
        }
    }

    internal SqlExpression GetAssignmentExpression(SqlAssignment assignment, int columnIndex)
    {
        if (assignment.Columns.Length > 1 && assignment.Value.Kind == SqlExpressionKind.RowValue && columnIndex < assignment.Value.Arguments.Length)
        {
            return assignment.Value.Arguments[columnIndex];
        }

        return assignment.Value;
    }

    internal void AnalyzeReturning(SqlReturningClause? returning, SqlLineageScope scope)
    {
        if (returning == null)
        {
            return;
        }

        AnalyzeResultColumns(returning.ResultColumns, null, scope, SqlColumnLineageTargetKind.ReturningOutput, null);
    }

    internal SqlLineageScope CreateTargetScope(SqlName name, SqlName? alias, SqlCatalogTable? table)
    {
        SqlLineageScope scope = new SqlLineageScope(null);
        scope.Bindings.Add(CreateCatalogBinding(name, alias, table, null));
        return scope;
    }

    internal void AddTableReferences(SqlLineageScope scope, SqlTableReference[] references)
    {
        int index = 0;
        while (index < references.Length)
        {
            AddTableReference(scope, references[index]);
            index = index + 1;
        }
    }

    internal void AddTableReference(SqlLineageScope scope, SqlTableReference reference)
    {
        if (reference.Kind == SqlTableReferenceKind.Parenthesized && reference.Inner != null)
        {
            AddTableReference(scope, reference.Inner);
        }
        else if (reference.Kind == SqlTableReferenceKind.Subquery && reference.Query != null)
        {
            SqlLineageScope? queryParent = reference.SourceData != null && reference.SourceData.IsLateral ? scope : scope.Parent;
            SqlLineageQueryResult? queryResult = AnalyzeQuery(reference.Query, queryParent);
            scope.Bindings.Add(CreateOutputBinding(reference.Name, reference.Alias, queryResult, reference.ColumnAliases));
        }
        else if (reference.Kind == SqlTableReferenceKind.Named && reference.Name != null)
        {
            SqlLineageBinding? cte = FindCommonTableExpression(scope, reference.Name);
            if (cte != null)
            {
                scope.Bindings.Add(CopyBinding(cte, reference.Name, reference.Alias, reference.ColumnAliases));
            }
            else
            {
                SqlCatalogTable? table = Catalog.FindTableByName(reference.Name);
                SqlCatalogView? view = table == null ? Catalog.FindViewByName(reference.Name) : null;
                scope.Bindings.Add(CreateCatalogBinding(reference.Name, reference.Alias, table, view));
            }
        }
        else
        {
            SqlLineageBinding unknown = new SqlLineageBinding(reference.Name, reference.Alias);
            unknown.ColumnsKnown = false;
            AddColumnAliases(unknown, reference.ColumnAliases);
            scope.Bindings.Add(unknown);
        }

        int joinIndex = 0;
        while (joinIndex < reference.Joins.Length)
        {
            AddTableReference(scope, reference.Joins[joinIndex].Right);
            joinIndex = joinIndex + 1;
        }
    }

    internal SqlLineageBinding? FindCommonTableExpression(SqlLineageScope scope, SqlName name)
    {
        if (name.Parts.Length != 1)
        {
            return null;
        }

        SqlLineageScope? current = scope;
        while (current != null)
        {
            int index = 0;
            while (index < current.CommonTableExpressions.Count)
            {
                SqlLineageBinding binding = current.CommonTableExpressions[index];
                if (binding.Name != null && NamesEqual(binding.Name.GetLastPart().Text, name.GetLastPart().Text))
                {
                    return binding;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal SqlLineageBinding CreateCatalogBinding(SqlName? name, SqlName? alias, SqlCatalogTable? table, SqlCatalogView? view)
    {
        SqlLineageBinding binding = new SqlLineageBinding(name, alias);
        binding.Table = table;
        binding.View = view;
        if (table != null)
        {
            int index = 0;
            while (index < table.Columns.Count)
            {
                SqlCatalogColumn column = table.Columns[index];
                SqlColumnLineageSource source = new SqlColumnLineageSource(null, table, null, column);
                binding.Columns.Add(new SqlLineageColumnBinding(column.Name, SqlColumnLineageOriginKind.DirectColumn, new SqlColumnLineageSource[] { source }));
                index = index + 1;
            }
        }
        else if (view != null)
        {
            int index = 0;
            while (index < view.Columns.Count)
            {
                SqlCatalogColumn column = view.Columns[index];
                SqlColumnLineageSource source = new SqlColumnLineageSource(null, null, view, column);
                binding.Columns.Add(new SqlLineageColumnBinding(column.Name, SqlColumnLineageOriginKind.DirectColumn, new SqlColumnLineageSource[] { source }));
                index = index + 1;
            }
        }
        else
        {
            binding.ColumnsKnown = false;
        }

        return binding;
    }

    internal SqlLineageBinding CreateOutputBinding(SqlName? name, SqlName? alias, SqlLineageQueryResult? result, SqlName[] columnAliases)
    {
        SqlLineageBinding binding = new SqlLineageBinding(name, alias);
        if (result == null)
        {
            binding.ColumnsKnown = false;
        }
        else
        {
            int index = 0;
            while (index < result.Outputs.Count)
            {
                SqlLineageOutput output = result.Outputs[index];
                string columnName = index < columnAliases.Length ? columnAliases[index].GetLastPart().Text : output.Name;
                binding.Columns.Add(new SqlLineageColumnBinding(columnName, output.OriginKind, output.Sources.ToArray()));
                index = index + 1;
            }
        }

        return binding;
    }

    internal SqlLineageBinding CopyBinding(SqlLineageBinding source, SqlName? name, SqlName? alias, SqlName[] columnAliases)
    {
        SqlLineageBinding binding = new SqlLineageBinding(name, alias);
        binding.ColumnsKnown = source.ColumnsKnown;
        int index = 0;
        while (index < source.Columns.Count)
        {
            SqlLineageColumnBinding column = source.Columns[index];
            string columnName = index < columnAliases.Length ? columnAliases[index].GetLastPart().Text : column.Name;
            binding.Columns.Add(new SqlLineageColumnBinding(columnName, column.OriginKind, CopySources(column.Sources)));
            index = index + 1;
        }

        return binding;
    }

    internal void AddColumnAliases(SqlLineageBinding binding, SqlName[] aliases)
    {
        int index = 0;
        while (index < aliases.Length)
        {
            binding.Columns.Add(new SqlLineageColumnBinding(aliases[index].GetLastPart().Text, SqlColumnLineageOriginKind.Unknown, new SqlColumnLineageSource[0]));
            index = index + 1;
        }
    }

    internal SqlLineageValue AnalyzeExpressionValue(SqlExpression? expression, SqlLineageScope? scope)
    {
        if (expression == null)
        {
            return new SqlLineageValue(SqlColumnLineageOriginKind.Unknown, new SqlColumnLineageSource[0]);
        }

        SqlLineageCollector collector = new SqlLineageCollector();
        CollectExpressionSources(expression, scope, collector);
        SqlColumnLineageOriginKind origin = SqlColumnLineageOriginKind.Expression;
        if (collector.HasAmbiguous)
        {
            origin = SqlColumnLineageOriginKind.Ambiguous;
        }
        else if (collector.HasUnknown)
        {
            origin = SqlColumnLineageOriginKind.Unknown;
        }
        else if (expression.Kind == SqlExpressionKind.Name)
        {
            origin = collector.DirectOrigin;
        }

        return new SqlLineageValue(origin, collector.Sources.ToArray());
    }

    internal void CollectExpressionSources(SqlExpression? expression, SqlLineageScope? scope, SqlLineageCollector collector)
    {
        if (expression == null || collector.HasVisited(expression))
        {
            return;
        }

        collector.Visited.Add(expression);
        if (expression.Kind == SqlExpressionKind.Name && expression.Name != null)
        {
            SqlLineageResolution resolution = ResolveColumn(expression.Name, scope);
            MergeSources(collector.Sources, resolution.Sources);
            collector.HasUnknown = collector.HasUnknown || resolution.IsUnknown;
            collector.HasAmbiguous = collector.HasAmbiguous || resolution.IsAmbiguous;
            collector.DirectOrigin = resolution.OriginKind;
            return;
        }

        if ((expression.Kind == SqlExpressionKind.Subquery || expression.Kind == SqlExpressionKind.Exists) && expression.Query != null)
        {
            SqlLineageQueryResult? queryResult = AnalyzeQuery(expression.Query, scope);
            if (queryResult == null || queryResult.Outputs.Count == 0)
            {
                collector.HasUnknown = true;
            }
            else
            {
                MergeSources(collector.Sources, queryResult.Outputs[0].Sources);
                collector.HasUnknown = collector.HasUnknown || queryResult.Outputs[0].OriginKind == SqlColumnLineageOriginKind.Unknown;
                collector.HasAmbiguous = collector.HasAmbiguous || queryResult.Outputs[0].OriginKind == SqlColumnLineageOriginKind.Ambiguous;
            }

            return;
        }

        CollectExpressionSources(expression.Operand, scope, collector);
        CollectExpressionSources(expression.Left, scope, collector);
        CollectExpressionSources(expression.Right, scope, collector);
        CollectExpressionSources(expression.Alternate, scope, collector);
        int index = 0;
        while (index < expression.Arguments.Length)
        {
            CollectExpressionSources(expression.Arguments[index], scope, collector);
            index = index + 1;
        }

        CollectExpressionSources(expression.Filter, scope, collector);
        index = 0;
        while (index < expression.OrderBy.Length)
        {
            CollectExpressionSources(expression.OrderBy[index].Expression, scope, collector);
            index = index + 1;
        }

        if (expression.ArrayData != null && expression.ArrayData.Query != null)
        {
            SqlLineageQueryResult? arrayQuery = AnalyzeQuery(expression.ArrayData.Query, scope);
            if (arrayQuery != null && arrayQuery.Outputs.Count > 0)
            {
                MergeSources(collector.Sources, arrayQuery.Outputs[0].Sources);
            }
        }
    }

    internal SqlLineageResolution ResolveColumn(SqlName name, SqlLineageScope? scope)
    {
        SqlLineageResolution result = new SqlLineageResolution();
        if (scope == null)
        {
            result.IsUnknown = true;
            result.Sources.Add(new SqlColumnLineageSource(name, null, null, null));
            return result;
        }

        SqlLineageScope? current = scope;
        while (current != null)
        {
            int matchCount = 0;
            bool unknownBinding = false;
            int bindingIndex = 0;
            while (bindingIndex < current.Bindings.Count)
            {
                SqlLineageBinding binding = current.Bindings[bindingIndex];
                bool qualifierMatches = name.Parts.Length == 1 || BindingMatchesColumnQualifier(binding, name);
                if (qualifierMatches)
                {
                    SqlLineageColumnBinding? column = FindBindingColumn(binding, name.GetLastPart().Text);
                    if (column != null)
                    {
                        matchCount = matchCount + 1;
                        AddResolvedSources(result.Sources, column.Sources, name);
                        result.OriginKind = column.OriginKind;
                    }
                    else if (!binding.ColumnsKnown)
                    {
                        unknownBinding = true;
                    }
                }

                bindingIndex = bindingIndex + 1;
            }

            if (matchCount > 1)
            {
                result.IsAmbiguous = true;
                result.OriginKind = SqlColumnLineageOriginKind.Ambiguous;
                return result;
            }

            if (matchCount == 1)
            {
                return result;
            }

            if (unknownBinding)
            {
                result.IsUnknown = true;
                result.Sources.Add(new SqlColumnLineageSource(name, null, null, null));
                return result;
            }

            current = current.Parent;
        }

        result.IsUnknown = true;
        result.Sources.Add(new SqlColumnLineageSource(name, null, null, null));
        return result;
    }

    internal void AddResolvedSources(List<SqlColumnLineageSource> target, SqlColumnLineageSource[] sources, SqlName reference)
    {
        int index = 0;
        while (index < sources.Length)
        {
            SqlColumnLineageSource source = sources[index];
            SqlColumnLineageSource resolved = new SqlColumnLineageSource(reference, source.Table, source.View, source.Column);
            AddSource(target, resolved);
            index = index + 1;
        }
    }

    internal SqlLineageColumnBinding? FindBindingColumn(SqlLineageBinding binding, string name)
    {
        int index = 0;
        while (index < binding.Columns.Count)
        {
            if (NamesEqual(binding.Columns[index].Name, name))
            {
                return binding.Columns[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool BindingMatchesColumnQualifier(SqlLineageBinding binding, SqlName columnName)
    {
        if (columnName.Parts.Length < 2)
        {
            return false;
        }

        string qualifier = columnName.Parts[columnName.Parts.Length - 2].Text;
        return BindingMatchesText(binding, qualifier);
    }

    internal bool BindingMatchesQualifier(SqlLineageBinding binding, SqlName qualifier)
    {
        return BindingMatchesText(binding, qualifier.GetLastPart().Text);
    }

    internal bool BindingMatchesText(SqlLineageBinding binding, string text)
    {
        if (binding.Alias != null)
        {
            return NamesEqual(binding.Alias.GetLastPart().Text, text);
        }

        return binding.Name != null && NamesEqual(binding.Name.GetLastPart().Text, text);
    }

    internal bool NamesEqual(string left, string right)
    {
        if (Catalog.CaseSensitiveNames)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal void AddEntry(SqlColumnLineageTargetKind targetKind, SqlColumnLineageOriginKind originKind, string outputName, SqlName? targetColumn, SqlCatalogTable? targetTable, SqlResultColumn? resultColumn, SqlAssignment? assignment, SqlExpression? expression, SqlQuery? query, SqlColumnLineageSource[] sources, SqlTextSpan span)
    {
        SqlColumnLineageEntry entry = new SqlColumnLineageEntry(targetKind, originKind, outputName, targetColumn, targetTable, resultColumn, assignment, expression, query, sources, span);
        Entries.Add(entry);
        AddOriginDiagnostic(originKind, outputName, span);
    }

    internal void AddOriginDiagnostic(SqlColumnLineageOriginKind originKind, string outputName, SqlTextSpan span)
    {
        if (originKind == SqlColumnLineageOriginKind.Unknown)
        {
            Diagnostics.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Warning, "SQLLINEAGE001", "Column lineage for '" + outputName + "' is unknown.", span, null);
        }
        else if (originKind == SqlColumnLineageOriginKind.Ambiguous)
        {
            Diagnostics.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Warning, "SQLLINEAGE002", "Column lineage for '" + outputName + "' is ambiguous.", span, null);
        }
    }

    internal void AddSource(List<SqlColumnLineageSource> sources, SqlColumnLineageSource source)
    {
        int index = 0;
        while (index < sources.Count)
        {
            bool sameResolvedColumn = source.Column != null && sources[index].Table == source.Table && sources[index].View == source.View && sources[index].Column == source.Column;
            bool sameUnknownReference = source.Column == null && sources[index].Column == null && sources[index].Reference == source.Reference;
            if (sameResolvedColumn || sameUnknownReference)
            {
                return;
            }

            index = index + 1;
        }

        sources.Add(source);
    }

    internal void MergeSources(List<SqlColumnLineageSource> target, List<SqlColumnLineageSource> sources)
    {
        int index = 0;
        while (index < sources.Count)
        {
            AddSource(target, sources[index]);
            index = index + 1;
        }
    }

    internal SqlColumnLineageSource[] CopySources(SqlColumnLineageSource[] sources)
    {
        SqlColumnLineageSource[] copy = new SqlColumnLineageSource[sources.Length];
        Array.Copy(sources, copy, sources.Length);
        return copy;
    }

    internal SqlColumnLineageResult CreateResult()
    {
        return new SqlColumnLineageResult(Script, Catalog, Entries.ToArray(), Diagnostics);
    }
}

internal class SqlLineageScope
{
    internal SqlLineageScope? Parent;
    internal List<SqlLineageBinding> Bindings;
    internal List<SqlLineageBinding> CommonTableExpressions;

    internal SqlLineageScope(SqlLineageScope? parent)
    {
        Parent = parent;
        Bindings = new List<SqlLineageBinding>();
        CommonTableExpressions = new List<SqlLineageBinding>();
    }
}

internal class SqlLineageBinding
{
    internal SqlName? Name;
    internal SqlName? Alias;
    internal SqlCatalogTable? Table;
    internal SqlCatalogView? View;
    internal List<SqlLineageColumnBinding> Columns;
    internal bool ColumnsKnown;

    internal SqlLineageBinding(SqlName? name, SqlName? alias)
    {
        Name = name;
        Alias = alias;
        Table = null;
        View = null;
        Columns = new List<SqlLineageColumnBinding>();
        ColumnsKnown = true;
    }
}

internal class SqlLineageColumnBinding
{
    internal string Name;
    internal SqlColumnLineageOriginKind OriginKind;
    internal SqlColumnLineageSource[] Sources;

    internal SqlLineageColumnBinding(string name, SqlColumnLineageOriginKind originKind, SqlColumnLineageSource[] sources)
    {
        Name = name;
        OriginKind = originKind;
        Sources = sources;
    }
}

internal class SqlLineageOutput
{
    internal string Name;
    internal SqlExpression? Expression;
    internal SqlResultColumn ResultColumn;
    internal SqlColumnLineageOriginKind OriginKind;
    internal List<SqlColumnLineageSource> Sources;
    internal SqlTextSpan Span;

    internal SqlLineageOutput(string name, SqlExpression? expression, SqlResultColumn resultColumn, SqlColumnLineageOriginKind originKind, SqlColumnLineageSource[] sources, SqlTextSpan span)
    {
        Name = name;
        Expression = expression;
        ResultColumn = resultColumn;
        OriginKind = originKind;
        Sources = new List<SqlColumnLineageSource>(sources);
        Span = span;
    }
}

internal class SqlLineageQueryResult
{
    internal SqlQuery Query;
    internal List<SqlLineageOutput> Outputs;

    internal SqlLineageQueryResult(SqlQuery query)
    {
        Query = query;
        Outputs = new List<SqlLineageOutput>();
    }
}

internal class SqlLineageValue
{
    internal SqlColumnLineageOriginKind OriginKind;
    internal SqlColumnLineageSource[] Sources;

    internal SqlLineageValue(SqlColumnLineageOriginKind originKind, SqlColumnLineageSource[] sources)
    {
        OriginKind = originKind;
        Sources = sources;
    }
}

internal class SqlLineageResolution
{
    internal SqlColumnLineageOriginKind OriginKind;
    internal List<SqlColumnLineageSource> Sources;
    internal bool IsUnknown;
    internal bool IsAmbiguous;

    internal SqlLineageResolution()
    {
        OriginKind = SqlColumnLineageOriginKind.DirectColumn;
        Sources = new List<SqlColumnLineageSource>();
        IsUnknown = false;
        IsAmbiguous = false;
    }
}

internal class SqlLineageCollector
{
    internal List<SqlColumnLineageSource> Sources;
    internal List<SqlExpression> Visited;
    internal bool HasUnknown;
    internal bool HasAmbiguous;
    internal SqlColumnLineageOriginKind DirectOrigin;

    internal SqlLineageCollector()
    {
        Sources = new List<SqlColumnLineageSource>();
        Visited = new List<SqlExpression>();
        HasUnknown = false;
        HasAmbiguous = false;
        DirectOrigin = SqlColumnLineageOriginKind.DirectColumn;
    }

    internal bool HasVisited(SqlExpression expression)
    {
        int index = 0;
        while (index < Visited.Count)
        {
            if (Visited[index] == expression)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
