using System;
using System.Collections.Generic;

namespace sql;

internal class SqlAstDiff
{
    internal static SqlAstDiffResult CompareScripts(SqlScript originalScript, SqlScript modifiedScript)
    {
        if (originalScript == null)
        {
            throw new ArgumentNullException("originalScript");
        }

        if (modifiedScript == null)
        {
            throw new ArgumentNullException("modifiedScript");
        }

        SqlAstDiffNode originalRoot = BuildScript(originalScript);
        SqlAstDiffNode modifiedRoot = BuildScript(modifiedScript);
        List<SqlAstDiffEntry> entries = new List<SqlAstDiffEntry>();
        CompareNodes(originalRoot, modifiedRoot, "$", entries);
        return new SqlAstDiffResult(originalScript, modifiedScript, entries.ToArray());
    }

    internal static SqlAstDiffNode BuildScript(SqlScript script)
    {
        SqlAstDiffNode node = new SqlAstDiffNode("$", "SqlScript", script, script.Span, "");
        int index = 0;
        while (index < script.Statements.Length)
        {
            node.Children.Add(BuildStatement(script.Statements[index], "Statements[" + index.ToString() + "]"));
            index = index + 1;
        }

        return node;
    }

    internal static SqlAstDiffNode BuildStatement(SqlStatement statement, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlStatement", statement, statement.Span, statement.Kind.ToString());
        if (statement.Query != null)
        {
            node.Children.Add(BuildQuery(statement.Query, "Query"));
            return node;
        }

        string signature = SqlGenerator.GenerateStatement(statement);
        if (statement.Insert != null)
        {
            node.Children.Add(BuildOpaque("Insert", "SqlInsertStatement", statement.Insert, statement.Insert.Span, signature));
        }
        else if (statement.Update != null)
        {
            node.Children.Add(BuildOpaque("Update", "SqlUpdateStatement", statement.Update, statement.Update.Span, signature));
        }
        else if (statement.Delete != null)
        {
            node.Children.Add(BuildOpaque("Delete", "SqlDeleteStatement", statement.Delete, statement.Delete.Span, signature));
        }
        else if (statement.Merge != null)
        {
            node.Children.Add(BuildOpaque("Merge", "SqlMergeStatement", statement.Merge, statement.Merge.Span, signature));
        }
        else
        {
            object? payload = GetStatementPayload(statement);
            if (payload != null)
            {
                node.Children.Add(BuildOpaque("Payload", GetStatementPayloadType(statement), payload, statement.Span, signature));
            }
            else if (statement.SourceText.Length > 0)
            {
                node.ScalarSignature = node.ScalarSignature + ":" + statement.SourceText;
            }
        }

        return node;
    }

    internal static object? GetStatementPayload(SqlStatement statement)
    {
        if (statement.CreateTable != null) return statement.CreateTable;
        if (statement.AlterTable != null) return statement.AlterTable;
        if (statement.DropTable != null) return statement.DropTable;
        if (statement.CreateIndex != null) return statement.CreateIndex;
        if (statement.DropIndex != null) return statement.DropIndex;
        if (statement.CreateView != null) return statement.CreateView;
        if (statement.DropView != null) return statement.DropView;
        if (statement.CreateTrigger != null) return statement.CreateTrigger;
        if (statement.DropTrigger != null) return statement.DropTrigger;
        if (statement.CreateVirtualTable != null) return statement.CreateVirtualTable;
        if (statement.Pragma != null) return statement.Pragma;
        if (statement.Analyze != null) return statement.Analyze;
        if (statement.Vacuum != null) return statement.Vacuum;
        if (statement.Attach != null) return statement.Attach;
        if (statement.Detach != null) return statement.Detach;
        if (statement.Reindex != null) return statement.Reindex;
        if (statement.Transaction != null) return statement.Transaction;
        if (statement.Savepoint != null) return statement.Savepoint;
        if (statement.Release != null) return statement.Release;
        if (statement.Explain != null) return statement.Explain;
        return null;
    }

    internal static string GetStatementPayloadType(SqlStatement statement)
    {
        if (statement.CreateTable != null) return "SqlTableDefinition";
        if (statement.AlterTable != null) return "SqlAlterTableDefinition";
        if (statement.DropTable != null) return "SqlDropTableDefinition";
        if (statement.CreateIndex != null) return "SqlIndexDefinition";
        if (statement.DropIndex != null) return "SqlDropIndexDefinition";
        if (statement.CreateView != null) return "SqlViewDefinition";
        if (statement.DropView != null) return "SqlDropViewDefinition";
        if (statement.CreateTrigger != null) return "SqlTriggerDefinition";
        if (statement.DropTrigger != null) return "SqlDropTriggerDefinition";
        if (statement.CreateVirtualTable != null) return "SqlVirtualTableDefinition";
        if (statement.Pragma != null) return "SqlPragmaStatement";
        if (statement.Analyze != null) return "SqlAnalyzeStatement";
        if (statement.Vacuum != null) return "SqlVacuumStatement";
        if (statement.Attach != null) return "SqlAttachStatement";
        if (statement.Detach != null) return "SqlDetachStatement";
        if (statement.Reindex != null) return "SqlReindexStatement";
        if (statement.Transaction != null) return "SqlTransactionStatement";
        if (statement.Savepoint != null) return "SqlSavepointStatement";
        if (statement.Release != null) return "SqlReleaseStatement";
        return "SqlExplainStatement";
    }

    internal static SqlAstDiffNode BuildOpaque(string segment, string nodeType, object value, SqlTextSpan span, string signature)
    {
        return new SqlAstDiffNode(segment, nodeType, value, span, signature);
    }

    internal static SqlAstDiffNode BuildQuery(SqlQuery query, string segment)
    {
        string scalar = query.IsRecursive.ToString();
        int operatorIndex = 0;
        while (operatorIndex < query.CompoundOperators.Length)
        {
            scalar = scalar + ":" + query.CompoundOperators[operatorIndex].ToString();
            operatorIndex = operatorIndex + 1;
        }

        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlQuery", query, query.Span, scalar);
        int index = 0;
        while (index < query.CommonTableExpressions.Length)
        {
            node.Children.Add(BuildCommonTableExpression(query.CommonTableExpressions[index], "CommonTableExpressions[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < query.SelectCores.Length)
        {
            node.Children.Add(BuildSelectCore(query.SelectCores[index], "SelectCores[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < query.OrderBy.Length)
        {
            node.Children.Add(BuildOrderingTerm(query.OrderBy[index], "OrderBy[" + index.ToString() + "]"));
            index = index + 1;
        }

        if (query.Limit != null)
        {
            node.Children.Add(BuildLimit(query.Limit, "Limit"));
        }

        if (query.OffsetFetch != null)
        {
            string offsetScalar = query.OffsetFetch.Direction.ToString() + ":" + query.OffsetFetch.WithTies.ToString();
            SqlAstDiffNode offsetNode = BuildOpaque("OffsetFetch", "SqlOffsetFetchClause", query.OffsetFetch, query.OffsetFetch.Span, offsetScalar);
            AddExpressionChild(offsetNode, query.OffsetFetch.Offset, "Offset");
            AddExpressionChild(offsetNode, query.OffsetFetch.Fetch, "Fetch");
            node.Children.Add(offsetNode);
        }

        return node;
    }

    internal static SqlAstDiffNode BuildCommonTableExpression(SqlCommonTableExpression expression, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlCommonTableExpression", expression, expression.Span, expression.Materialization.ToString());
        node.Children.Add(BuildName(expression.Name, "Name"));
        int index = 0;
        while (index < expression.ColumnNames.Length)
        {
            node.Children.Add(BuildName(expression.ColumnNames[index], "ColumnNames[" + index.ToString() + "]"));
            index = index + 1;
        }

        node.Children.Add(BuildQuery(expression.Query, "Query"));
        return node;
    }

    internal static SqlAstDiffNode BuildSelectCore(SqlSelectCore core, string segment)
    {
        string scalar = core.IsDistinct.ToString() + ":" + core.IsAll.ToString() + ":" + core.IsValues.ToString();
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlSelectCore", core, core.Span, scalar);
        if (core.Top != null)
        {
            string topScalar = core.Top.IsPercent.ToString() + ":" + core.Top.WithTies.ToString() + ":" + core.Top.IsParenthesized.ToString();
            SqlAstDiffNode top = BuildOpaque("Top", "SqlTopClause", core.Top, core.Top.Span, topScalar);
            top.Children.Add(BuildExpression(core.Top.Quantity, "Quantity"));
            node.Children.Add(top);
        }

        int index = 0;
        while (index < core.ResultColumns.Length)
        {
            node.Children.Add(BuildResultColumn(core.ResultColumns[index], "ResultColumns[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < core.Values.Length)
        {
            int valueIndex = 0;
            while (valueIndex < core.Values[index].Length)
            {
                node.Children.Add(BuildExpression(core.Values[index][valueIndex], "Values[" + index.ToString() + "][" + valueIndex.ToString() + "]"));
                valueIndex = valueIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < core.From.Length)
        {
            node.Children.Add(BuildTableReference(core.From[index], "From[" + index.ToString() + "]"));
            index = index + 1;
        }

        AddExpressionChild(node, core.Where, "Where");
        index = 0;
        while (index < core.GroupBy.Length)
        {
            node.Children.Add(BuildExpression(core.GroupBy[index], "GroupBy[" + index.ToString() + "]"));
            index = index + 1;
        }

        AddExpressionChild(node, core.Having, "Having");
        index = 0;
        while (index < core.Windows.Length)
        {
            node.Children.Add(BuildWindowDefinition(core.Windows[index], "Windows[" + index.ToString() + "]"));
            index = index + 1;
        }

        return node;
    }

    internal static SqlAstDiffNode BuildResultColumn(SqlResultColumn column, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlResultColumn", column, column.Span, column.IsWildcard.ToString());
        AddExpressionChild(node, column.Expression, "Expression");
        AddNameChild(node, column.Qualifier, "Qualifier");
        AddNameChild(node, column.Alias, "Alias");
        return node;
    }

    internal static SqlAstDiffNode BuildTableReference(SqlTableReference table, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlTableReference", table, table.Span, table.Kind.ToString());
        AddNameChild(node, table.Name, "Name");
        if (table.Query != null) node.Children.Add(BuildQuery(table.Query, "Query"));
        AddExpressionChild(node, table.FunctionExpression, "FunctionExpression");
        if (table.Inner != null) node.Children.Add(BuildTableReference(table.Inner, "Inner"));
        AddNameChild(node, table.Alias, "Alias");
        int index = 0;
        while (index < table.ColumnAliases.Length)
        {
            node.Children.Add(BuildName(table.ColumnAliases[index], "ColumnAliases[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < table.Joins.Length)
        {
            node.Children.Add(BuildJoin(table.Joins[index], "Joins[" + index.ToString() + "]"));
            index = index + 1;
        }

        return node;
    }

    internal static SqlAstDiffNode BuildJoin(SqlJoin join, string segment)
    {
        string scalar = join.Kind.ToString() + ":" + join.IsNatural.ToString() + ":" + join.IsOuter.ToString() + ":" + join.ConstraintKind.ToString();
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlJoin", join, join.Span, scalar);
        node.Children.Add(BuildTableReference(join.Right, "Right"));
        AddExpressionChild(node, join.OnExpression, "OnExpression");
        int index = 0;
        while (index < join.UsingColumns.Length)
        {
            node.Children.Add(BuildName(join.UsingColumns[index], "UsingColumns[" + index.ToString() + "]"));
            index = index + 1;
        }

        return node;
    }

    internal static SqlAstDiffNode BuildExpression(SqlExpression expression, string segment)
    {
        string scalar = expression.Kind.ToString() + ":" + expression.UnaryOperator.ToString() + ":" + expression.BinaryOperator.ToString() + ":" + (expression.OperatorText ?? "") + ":" + (expression.TypeName ?? "") + ":" + (expression.Value ?? "") + ":" + expression.IsDistinct.ToString();
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlExpression", expression, expression.Span, scalar);
        if (expression.Kind == SqlExpressionKind.Name) AddNameChild(node, expression.Name, "Name");
        if (expression.Kind == SqlExpressionKind.FunctionCall) AddNameChild(node, expression.Name, "Name");
        if (expression.Kind == SqlExpressionKind.Wildcard) AddNameChild(node, expression.Name, "Qualifier");
        if (expression.Kind == SqlExpressionKind.Literal && expression.Literal != null) node.Children.Add(BuildLiteral(expression.Literal, "Literal"));
        if (expression.Kind == SqlExpressionKind.Unary || expression.Kind == SqlExpressionKind.Parenthesized || expression.Kind == SqlExpressionKind.Collate || expression.Kind == SqlExpressionKind.Cast || expression.Kind == SqlExpressionKind.Extract || expression.Kind == SqlExpressionKind.Subscript || expression.Kind == SqlExpressionKind.Between || expression.Kind == SqlExpressionKind.In || expression.Kind == SqlExpressionKind.Case || expression.Kind == SqlExpressionKind.Window)
        {
            AddExpressionChild(node, expression.Operand, "Operand");
        }

        if (expression.Kind == SqlExpressionKind.Binary || expression.Kind == SqlExpressionKind.JsonOperator || expression.Kind == SqlExpressionKind.Is || expression.Kind == SqlExpressionKind.Distinctness || expression.Kind == SqlExpressionKind.Like || expression.Kind == SqlExpressionKind.Subscript || expression.Kind == SqlExpressionKind.Between || expression.Kind == SqlExpressionKind.In || expression.Kind == SqlExpressionKind.Case)
        {
            AddExpressionChild(node, expression.Left, "Left");
            AddExpressionChild(node, expression.Right, "Right");
            AddExpressionChild(node, expression.Alternate, "Alternate");
        }

        int index = 0;
        while (index < expression.Arguments.Length)
        {
            node.Children.Add(BuildExpression(expression.Arguments[index], "Arguments[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < expression.OrderBy.Length)
        {
            node.Children.Add(BuildOrderingTerm(expression.OrderBy[index], "OrderBy[" + index.ToString() + "]"));
            index = index + 1;
        }

        if (expression.Query != null) node.Children.Add(BuildQuery(expression.Query, "Query"));
        AddExpressionChild(node, expression.Filter, "Filter");
        AddNameChild(node, expression.Kind == SqlExpressionKind.Collate ? expression.Name : null, "Collation");
        if (expression.WindowDefinition != null) node.Children.Add(BuildWindowDefinition(expression.WindowDefinition, "WindowDefinition"));
        return node;
    }

    internal static SqlAstDiffNode BuildLiteral(SqlLiteral literal, string segment)
    {
        return new SqlAstDiffNode(segment, "SqlLiteral", literal, literal.Span, literal.Kind.ToString() + ":" + literal.SourceText);
    }

    internal static SqlAstDiffNode BuildName(SqlName name, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlName", name, name.Span, "");
        int index = 0;
        while (index < name.Parts.Length)
        {
            SqlNamePart part = name.Parts[index];
            string scalar = part.Text + ":" + part.QuoteStyle.ToString();
            node.Children.Add(new SqlAstDiffNode("Parts[" + index.ToString() + "]", "SqlNamePart", part, part.Span, scalar));
            index = index + 1;
        }

        return node;
    }

    internal static SqlAstDiffNode BuildOrderingTerm(SqlOrderingTerm term, string segment)
    {
        string scalar = term.Direction.ToString() + ":" + term.NullOrdering.ToString();
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlOrderingTerm", term, term.Span, scalar);
        node.Children.Add(BuildExpression(term.Expression, "Expression"));
        AddNameChild(node, term.Collation, "Collation");
        return node;
    }

    internal static SqlAstDiffNode BuildLimit(SqlLimitClause limit, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlLimitClause", limit, limit.Span, "");
        node.Children.Add(BuildExpression(limit.LimitExpression, "LimitExpression"));
        AddExpressionChild(node, limit.OffsetExpression, "OffsetExpression");
        return node;
    }

    internal static SqlAstDiffNode BuildWindowDefinition(SqlWindowDefinition window, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlWindowDefinition", window, window.Span, "");
        AddNameChild(node, window.Name, "Name");
        AddNameChild(node, window.BaseWindowName, "BaseWindowName");
        int index = 0;
        while (index < window.PartitionBy.Length)
        {
            node.Children.Add(BuildExpression(window.PartitionBy[index], "PartitionBy[" + index.ToString() + "]"));
            index = index + 1;
        }

        index = 0;
        while (index < window.OrderBy.Length)
        {
            node.Children.Add(BuildOrderingTerm(window.OrderBy[index], "OrderBy[" + index.ToString() + "]"));
            index = index + 1;
        }

        if (window.Frame != null)
        {
            node.Children.Add(BuildWindowFrame(window.Frame, "Frame"));
        }

        return node;
    }

    internal static SqlAstDiffNode BuildWindowFrame(SqlWindowFrame frame, string segment)
    {
        string scalar = frame.Kind.ToString() + ":" + frame.Exclusion.ToString();
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlWindowFrame", frame, frame.Span, scalar);
        node.Children.Add(BuildWindowBoundary(frame.Start, "Start"));
        if (frame.End != null)
        {
            node.Children.Add(BuildWindowBoundary(frame.End, "End"));
        }

        return node;
    }

    internal static SqlAstDiffNode BuildWindowBoundary(SqlWindowFrameBoundary boundary, string segment)
    {
        SqlAstDiffNode node = new SqlAstDiffNode(segment, "SqlWindowFrameBoundary", boundary, boundary.Span, boundary.Kind.ToString());
        AddExpressionChild(node, boundary.Expression, "Expression");
        return node;
    }

    internal static void AddExpressionChild(SqlAstDiffNode node, SqlExpression? expression, string segment)
    {
        if (expression != null)
        {
            node.Children.Add(BuildExpression(expression, segment));
        }
    }

    internal static void AddNameChild(SqlAstDiffNode node, SqlName? name, string segment)
    {
        if (name != null)
        {
            node.Children.Add(BuildName(name, segment));
        }
    }

    internal static void CompareNodes(SqlAstDiffNode? original, SqlAstDiffNode? modified, string path, List<SqlAstDiffEntry> entries)
    {
        if (original == null && modified == null)
        {
            return;
        }

        if (original == null && modified != null)
        {
            entries.Add(new SqlAstDiffEntry(SqlAstDiffKind.Added, path, modified.NodeType, null, modified.Node, null, modified.Span));
            AddDescendantEntries(modified, path, SqlAstDiffKind.Added, false, entries);
            return;
        }

        if (modified == null && original != null)
        {
            entries.Add(new SqlAstDiffEntry(SqlAstDiffKind.Removed, path, original.NodeType, original.Node, null, original.Span, null));
            AddDescendantEntries(original, path, SqlAstDiffKind.Removed, true, entries);
            return;
        }

        if (original == null || modified == null)
        {
            return;
        }

        bool unchanged = original.NodeType == modified.NodeType && original.GetStructuralSignature() == modified.GetStructuralSignature();
        string nodeType = original.NodeType == modified.NodeType ? original.NodeType : original.NodeType + "/" + modified.NodeType;
        entries.Add(new SqlAstDiffEntry(unchanged ? SqlAstDiffKind.Unchanged : SqlAstDiffKind.Replaced, path, nodeType, original.Node, modified.Node, original.Span, modified.Span));

        int index = 0;
        while (index < original.Children.Count)
        {
            SqlAstDiffNode originalChild = original.Children[index];
            SqlAstDiffNode? modifiedChild = modified.FindChild(originalChild.Segment);
            CompareNodes(originalChild, modifiedChild, path + "." + originalChild.Segment, entries);
            index = index + 1;
        }

        index = 0;
        while (index < modified.Children.Count)
        {
            SqlAstDiffNode modifiedChild = modified.Children[index];
            if (original.FindChild(modifiedChild.Segment) == null)
            {
                CompareNodes(null, modifiedChild, path + "." + modifiedChild.Segment, entries);
            }

            index = index + 1;
        }
    }

    internal static void AddDescendantEntries(SqlAstDiffNode node, string path, SqlAstDiffKind kind, bool useOriginal, List<SqlAstDiffEntry> entries)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            SqlAstDiffNode child = node.Children[index];
            string childPath = path + "." + child.Segment;
            object? originalNode = useOriginal ? child.Node : null;
            object? modifiedNode = useOriginal ? null : child.Node;
            SqlTextSpan? originalSpan = useOriginal ? child.Span : null;
            SqlTextSpan? modifiedSpan = useOriginal ? null : child.Span;
            entries.Add(new SqlAstDiffEntry(kind, childPath, child.NodeType, originalNode, modifiedNode, originalSpan, modifiedSpan));
            AddDescendantEntries(child, childPath, kind, useOriginal, entries);
            index = index + 1;
        }
    }
}
