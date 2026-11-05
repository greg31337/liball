using System;
using System.Collections.Generic;

namespace sql;

internal class SqlLogicalPlanConversion
{
    internal static SqlLogicalPlanResult BuildPlan(SqlScript script, SqlCatalog catalog)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        SqlLogicalPlanContext context = new SqlLogicalPlanContext(script, catalog);
        SqlLogicalPlanNode root = context.BuildScript(script);
        return new SqlLogicalPlanResult(script, catalog, root, context.Diagnostics);
    }
}

internal class SqlLogicalPlanContext
{
    internal SqlScript Script;
    internal SqlCatalog Catalog;
    internal SqlDiagnosticBag Diagnostics;
    internal SqlSourceText SourceText;

    internal SqlLogicalPlanContext(SqlScript script, SqlCatalog catalog)
    {
        Script = script;
        Catalog = catalog;
        Diagnostics = new SqlDiagnosticBag();
        SourceText = new SqlSourceText(script.SourceText);
    }

    internal SqlLogicalPlanNode BuildScript(SqlScript script)
    {
        SqlLogicalPlanNode[] inputs = new SqlLogicalPlanNode[script.Statements.Length];
        bool complete = true;
        int index = 0;
        while (index < script.Statements.Length)
        {
            inputs[index] = BuildStatement(script.Statements[index]);
            if (!inputs[index].IsComplete)
            {
                complete = false;
            }

            index = index + 1;
        }

        return CreateNode(SqlLogicalPlanNodeKind.Script, inputs, script, null, new SqlExpression[0], script.Span, "SCRIPT", complete);
    }

    internal SqlLogicalPlanNode BuildStatement(SqlStatement statement)
    {
        if (statement.Kind == SqlStatementKind.Select && statement.Query != null)
        {
            return BuildQuery(statement.Query);
        }

        if (statement.Kind == SqlStatementKind.Insert && statement.Insert != null)
        {
            return BuildInsert(statement.Insert);
        }

        if (statement.Kind == SqlStatementKind.Update && statement.Update != null)
        {
            return BuildUpdate(statement.Update);
        }

        if (statement.Kind == SqlStatementKind.Delete && statement.Delete != null)
        {
            return BuildDelete(statement.Delete);
        }

        if (statement.Kind == SqlStatementKind.Merge && statement.Merge != null)
        {
            return BuildMerge(statement.Merge);
        }

        return CreateUnknown(statement, statement.Span, "Statement kind " + statement.Kind.ToString() + " has no relational logical-plan conversion.", "SQLPLAN002", null);
    }

    internal SqlLogicalPlanNode BuildQuery(SqlQuery query)
    {
        if (query.CommonTableExpressions.Length > 0)
        {
            return CreateUnknown(query, query.Span, "Common table expressions require scope-aware semantic planning.", "SQLPLAN003", null);
        }

        if (query.UnsupportedClauses.Length > 0)
        {
            return CreateUnknown(query, query.UnsupportedClauses[0].Span, "A preserved query clause has unknown relational semantics.", "SQLPLAN003", null);
        }

        SqlLogicalPlanNode current;
        if (query.SelectCores.Length == 0)
        {
            return CreateUnknown(query, query.Span, "Query has no select core.", "SQLPLAN003", null);
        }

        if (query.SelectCores.Length == 1)
        {
            current = BuildSelectCore(query.SelectCores[0]);
        }
        else
        {
            SqlLogicalPlanNode[] inputs = new SqlLogicalPlanNode[query.SelectCores.Length];
            bool complete = true;
            int index = 0;
            while (index < query.SelectCores.Length)
            {
                inputs[index] = BuildSelectCore(query.SelectCores[index]);
                if (!inputs[index].IsComplete)
                {
                    complete = false;
                }

                index = index + 1;
            }

            current = CreateNode(SqlLogicalPlanNodeKind.SetOperation, inputs, query, null, new SqlExpression[0], query.Span, GetCompoundDetail(query), complete);
        }

        if (query.OrderBy.Length > 0)
        {
            SqlExpression[] expressions = GetOrderingExpressions(query.OrderBy);
            current = CreateNode(SqlLogicalPlanNodeKind.Sort, new SqlLogicalPlanNode[] { current }, query, null, expressions, query.Span, "ORDER BY", current.IsComplete);
        }

        if (query.Limit != null || query.OffsetFetch != null)
        {
            SqlExpression[] expressions = GetLimitExpressions(query);
            current = CreateNode(SqlLogicalPlanNodeKind.Limit, new SqlLogicalPlanNode[] { current }, query, null, expressions, query.Span, "LIMIT", current.IsComplete);
        }

        return current;
    }

    internal SqlLogicalPlanNode BuildSelectCore(SqlSelectCore core)
    {
        if (core.UnsupportedClauses.Length > 0)
        {
            return CreateUnknown(core, core.UnsupportedClauses[0].Span, "A preserved select clause has unknown relational semantics.", "SQLPLAN003", null);
        }

        SqlLogicalPlanNode current = BuildSelectInput(core);
        if (core.Where != null)
        {
            current = CreateNode(SqlLogicalPlanNodeKind.Filter, new SqlLogicalPlanNode[] { current }, core, null, new SqlExpression[] { core.Where }, core.Where.Span, "WHERE", current.IsComplete);
        }

        if (core.GroupBy.Length > 0)
        {
            current = CreateNode(SqlLogicalPlanNodeKind.Aggregate, new SqlLogicalPlanNode[] { current }, core, null, core.GroupBy, core.Span, "GROUP BY", current.IsComplete);
        }

        if (core.Having != null)
        {
            current = CreateNode(SqlLogicalPlanNodeKind.Filter, new SqlLogicalPlanNode[] { current }, core, null, new SqlExpression[] { core.Having }, core.Having.Span, "HAVING", current.IsComplete);
        }

        List<SqlExpression> projections = new List<SqlExpression>();
        List<SqlLogicalPlanNode> projectInputs = new List<SqlLogicalPlanNode>();
        projectInputs.Add(current);
        bool complete = current.IsComplete;
        int index = 0;
        while (index < core.ResultColumns.Length)
        {
            SqlResultColumn column = core.ResultColumns[index];
            if (column.IsWildcard)
            {
                projectInputs.Add(CreateUnknown(column, column.Span, "Wildcard projection requires output-column expansion.", "SQLPLAN004", column.Qualifier));
                complete = false;
            }
            else if (column.Expression != null)
            {
                projections.Add(column.Expression);
            }
            else
            {
                projectInputs.Add(CreateUnknown(column, column.Span, "Result column has no expression.", "SQLPLAN004", column.Qualifier));
                complete = false;
            }

            index = index + 1;
        }

        string detail = core.IsDistinct ? "SELECT DISTINCT" : "SELECT";
        return CreateNode(SqlLogicalPlanNodeKind.Project, projectInputs.ToArray(), core, null, projections.ToArray(), core.Span, detail, complete);
    }

    internal SqlLogicalPlanNode BuildSelectInput(SqlSelectCore core)
    {
        if (core.IsValues)
        {
            return CreateNode(SqlLogicalPlanNodeKind.Values, new SqlLogicalPlanNode[0], core, null, FlattenRows(core.Values), core.Span, "VALUES", true);
        }

        if (core.From.Length == 0)
        {
            return CreateNode(SqlLogicalPlanNodeKind.Values, new SqlLogicalPlanNode[0], core, null, new SqlExpression[0], core.Span, "SINGLE ROW", true);
        }

        SqlLogicalPlanNode current = BuildTableReference(core.From[0]);
        int index = 1;
        while (index < core.From.Length)
        {
            SqlLogicalPlanNode right = BuildTableReference(core.From[index]);
            bool complete = current.IsComplete && right.IsComplete;
            current = CreateNode(SqlLogicalPlanNodeKind.Join, new SqlLogicalPlanNode[] { current, right }, core, null, new SqlExpression[0], core.Span, "CROSS", complete);
            index = index + 1;
        }

        return current;
    }

    internal SqlLogicalPlanNode BuildTableReference(SqlTableReference table)
    {
        SqlLogicalPlanNode current;
        if (table.Kind == SqlTableReferenceKind.Named && table.Name != null)
        {
            current = BuildRelationScan(table, table.Name, table.Span);
        }
        else if (table.Kind == SqlTableReferenceKind.Subquery && table.Query != null)
        {
            current = BuildQuery(table.Query);
        }
        else if (table.Kind == SqlTableReferenceKind.Parenthesized && table.Inner != null)
        {
            current = BuildTableReference(table.Inner);
        }
        else if (table.Kind == SqlTableReferenceKind.Values && table.SourceData != null)
        {
            current = CreateNode(SqlLogicalPlanNodeKind.Values, new SqlLogicalPlanNode[0], table, null, FlattenRows(table.SourceData.Values), table.Span, "VALUES", true);
        }
        else
        {
            current = CreateUnknown(table, table.Span, "Table source kind " + table.Kind.ToString() + " has unknown relational semantics.", "SQLPLAN003", table.Name);
        }

        int index = 0;
        while (index < table.Joins.Length)
        {
            SqlJoin join = table.Joins[index];
            SqlLogicalPlanNode right = BuildTableReference(join.Right);
            SqlExpression[] expressions = join.OnExpression == null ? new SqlExpression[0] : new SqlExpression[] { join.OnExpression };
            bool complete = current.IsComplete && right.IsComplete;
            current = CreateNode(SqlLogicalPlanNodeKind.Join, new SqlLogicalPlanNode[] { current, right }, join, null, expressions, join.Span, join.Kind.ToString(), complete);
            index = index + 1;
        }

        return current;
    }

    internal SqlLogicalPlanNode BuildRelationScan(object sourceNode, SqlName relationName, SqlTextSpan span)
    {
        if (Catalog.FindTableByName(relationName) != null || Catalog.FindViewByName(relationName) != null)
        {
            return CreateNode(SqlLogicalPlanNodeKind.Scan, new SqlLogicalPlanNode[0], sourceNode, relationName, new SqlExpression[0], span, relationName.GetText(), true);
        }

        return CreateUnknown(sourceNode, span, "Relation " + relationName.GetText() + " could not be resolved in the catalog.", "SQLPLAN001", relationName);
    }

    internal SqlLogicalPlanNode BuildInsert(SqlInsertStatement insert)
    {
        SqlLogicalPlanNode target = BuildRelationScan(insert, insert.TableName, insert.Span);
        SqlLogicalPlanNode source;
        if (insert.SourceKind == SqlInsertSourceKind.Select && insert.SourceQuery != null)
        {
            source = BuildQuery(insert.SourceQuery);
        }
        else if (insert.SourceKind == SqlInsertSourceKind.Values || insert.SourceKind == SqlInsertSourceKind.DefaultValues)
        {
            source = CreateNode(SqlLogicalPlanNodeKind.Values, new SqlLogicalPlanNode[0], insert, null, FlattenRows(insert.Values), insert.Span, insert.SourceKind.ToString(), true);
        }
        else
        {
            source = CreateUnknown(insert, insert.Span, "INSERT source could not be represented safely.", "SQLPLAN003", null);
        }

        bool complete = target.IsComplete && source.IsComplete;
        return CreateNode(SqlLogicalPlanNodeKind.Insert, new SqlLogicalPlanNode[] { target, source }, insert, insert.TableName, new SqlExpression[0], insert.Span, "INSERT", complete);
    }

    internal SqlLogicalPlanNode BuildUpdate(SqlUpdateStatement update)
    {
        List<SqlLogicalPlanNode> inputs = new List<SqlLogicalPlanNode>();
        inputs.Add(BuildRelationScan(update, update.TableName, update.Span));
        int index = 0;
        while (index < update.From.Length)
        {
            inputs.Add(BuildTableReference(update.From[index]));
            index = index + 1;
        }

        List<SqlExpression> expressions = new List<SqlExpression>();
        if (update.Assignments.Length > 0)
        {
            index = 0;
            while (index < update.Assignments.Length)
            {
                expressions.Add(update.Assignments[index].Value);
                index = index + 1;
            }
        }
        else
        {
            AddExpressions(expressions, update.AssignmentExpressions);
        }

        if (update.Where != null) expressions.Add(update.Where);
        return CreateNode(SqlLogicalPlanNodeKind.Update, inputs.ToArray(), update, update.TableName, expressions.ToArray(), update.Span, "UPDATE", InputsComplete(inputs));
    }

    internal SqlLogicalPlanNode BuildDelete(SqlDeleteStatement delete)
    {
        SqlLogicalPlanNode target = BuildRelationScan(delete, delete.TableName, delete.Span);
        SqlExpression[] expressions = delete.Where == null ? new SqlExpression[0] : new SqlExpression[] { delete.Where };
        return CreateNode(SqlLogicalPlanNodeKind.Delete, new SqlLogicalPlanNode[] { target }, delete, delete.TableName, expressions, delete.Span, "DELETE", target.IsComplete);
    }

    internal SqlLogicalPlanNode BuildMerge(SqlMergeStatement merge)
    {
        SqlLogicalPlanNode target = BuildRelationScan(merge, merge.TableName, merge.Span);
        SqlLogicalPlanNode source = BuildTableReference(merge.Source);
        List<SqlExpression> expressions = new List<SqlExpression>();
        expressions.Add(merge.Condition);
        int clauseIndex = 0;
        while (clauseIndex < merge.Clauses.Length)
        {
            SqlMergeWhenClause clause = merge.Clauses[clauseIndex];
            if (clause.Condition != null) expressions.Add(clause.Condition);
            int assignmentIndex = 0;
            while (assignmentIndex < clause.Assignments.Length)
            {
                expressions.Add(clause.Assignments[assignmentIndex].Value);
                assignmentIndex = assignmentIndex + 1;
            }

            AddExpressions(expressions, clause.InsertValues);
            clauseIndex = clauseIndex + 1;
        }

        bool complete = target.IsComplete && source.IsComplete;
        return CreateNode(SqlLogicalPlanNodeKind.Merge, new SqlLogicalPlanNode[] { target, source }, merge, merge.TableName, expressions.ToArray(), merge.Span, "MERGE", complete);
    }

    internal SqlLogicalPlanNode CreateUnknown(object sourceNode, SqlTextSpan span, string message, string code, SqlName? relationName)
    {
        Diagnostics.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Warning, code, message, span, SourceText);
        return CreateNode(SqlLogicalPlanNodeKind.Unknown, new SqlLogicalPlanNode[0], sourceNode, relationName, new SqlExpression[0], span, message, false);
    }

    internal SqlLogicalPlanNode CreateNode(SqlLogicalPlanNodeKind kind, SqlLogicalPlanNode[] inputs, object sourceNode, SqlName? relationName, SqlExpression[] expressions, SqlTextSpan span, string detail, bool complete)
    {
        return new SqlLogicalPlanNode(kind, inputs, sourceNode, relationName, expressions, span, detail, complete);
    }

    internal string GetCompoundDetail(SqlQuery query)
    {
        string detail = "";
        int index = 0;
        while (index < query.CompoundOperators.Length)
        {
            if (index > 0) detail = detail + ",";
            detail = detail + query.CompoundOperators[index].ToString();
            index = index + 1;
        }

        return detail;
    }

    internal SqlExpression[] GetOrderingExpressions(SqlOrderingTerm[] terms)
    {
        SqlExpression[] expressions = new SqlExpression[terms.Length];
        int index = 0;
        while (index < terms.Length)
        {
            expressions[index] = terms[index].Expression;
            index = index + 1;
        }

        return expressions;
    }

    internal SqlExpression[] GetLimitExpressions(SqlQuery query)
    {
        List<SqlExpression> expressions = new List<SqlExpression>();
        if (query.Limit != null)
        {
            expressions.Add(query.Limit.LimitExpression);
            if (query.Limit.OffsetExpression != null) expressions.Add(query.Limit.OffsetExpression);
        }

        if (query.OffsetFetch != null)
        {
            if (query.OffsetFetch.Offset != null) expressions.Add(query.OffsetFetch.Offset);
            if (query.OffsetFetch.Fetch != null) expressions.Add(query.OffsetFetch.Fetch);
        }

        return expressions.ToArray();
    }

    internal SqlExpression[] FlattenRows(SqlExpression[][] rows)
    {
        List<SqlExpression> expressions = new List<SqlExpression>();
        int index = 0;
        while (index < rows.Length)
        {
            AddExpressions(expressions, rows[index]);
            index = index + 1;
        }

        return expressions.ToArray();
    }

    internal void AddExpressions(List<SqlExpression> target, SqlExpression[] additions)
    {
        int index = 0;
        while (index < additions.Length)
        {
            target.Add(additions[index]);
            index = index + 1;
        }
    }

    internal bool InputsComplete(List<SqlLogicalPlanNode> inputs)
    {
        int index = 0;
        while (index < inputs.Count)
        {
            if (!inputs[index].IsComplete)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
