using System.Collections.Generic;

namespace sql;

internal class SqlAstWalker
{
    internal List<SqlScript> Scripts;
    internal List<SqlStatement> Statements;
    internal List<SqlQuery> Queries;
    internal List<SqlSelectCore> SelectCores;
    internal List<SqlExpression> Expressions;
    internal List<SqlLiteral> Literals;
    internal List<SqlName> Names;
    internal List<SqlPragmaStatement> PragmaStatements;
    internal List<SqlAnalyzeStatement> AnalyzeStatements;
    internal List<SqlVacuumStatement> VacuumStatements;
    internal List<SqlAttachStatement> AttachStatements;
    internal List<SqlDetachStatement> DetachStatements;
    internal List<SqlReindexStatement> ReindexStatements;
    internal List<SqlTransactionStatement> TransactionStatements;
    internal List<SqlSavepointStatement> SavepointStatements;
    internal List<SqlReleaseStatement> ReleaseStatements;
    internal List<SqlExplainStatement> ExplainStatements;
    internal List<SqlTableReference> TableReferences;
    internal List<SqlJoin> Joins;
    internal List<SqlTableDefinition> TableDefinitions;
    internal List<SqlAlterTableDefinition> AlterTableDefinitions;
    internal List<SqlDropTableDefinition> DropTableDefinitions;
    internal List<SqlColumnDefinition> ColumnDefinitions;
    internal List<SqlIndexDefinition> IndexDefinitions;
    internal List<SqlDropIndexDefinition> DropIndexDefinitions;
    internal List<SqlViewDefinition> ViewDefinitions;
    internal List<SqlDropViewDefinition> DropViewDefinitions;
    internal List<SqlVirtualTableDefinition> VirtualTableDefinitions;
    internal List<SqlTriggerDefinition> TriggerDefinitions;
    internal List<SqlDropTriggerDefinition> DropTriggerDefinitions;
    internal List<SqlColumnConstraint> ColumnConstraints;
    internal List<SqlTableConstraint> TableConstraints;
    internal List<SqlForeignKeyReference> ForeignKeyReferences;
    internal List<SqlInsertStatement> InsertStatements;
    internal List<SqlUpdateStatement> UpdateStatements;
    internal List<SqlDeleteStatement> DeleteStatements;
    internal List<SqlMergeStatement> MergeStatements;
    internal List<SqlMergeWhenClause> MergeClauses;
    internal List<SqlAssignment> Assignments;
    internal List<SqlConflictTarget> ConflictTargets;
    internal List<SqlUpsertClause> UpsertClauses;
    internal List<SqlReturningClause> ReturningClauses;
    internal List<SqlUnsupportedSyntax> UnsupportedSyntax;

    internal SqlAstWalker()
    {
        Scripts = new List<SqlScript>();
        Statements = new List<SqlStatement>();
        Queries = new List<SqlQuery>();
        SelectCores = new List<SqlSelectCore>();
        Expressions = new List<SqlExpression>();
        Literals = new List<SqlLiteral>();
        Names = new List<SqlName>();
        PragmaStatements = new List<SqlPragmaStatement>();
        AnalyzeStatements = new List<SqlAnalyzeStatement>();
        VacuumStatements = new List<SqlVacuumStatement>();
        AttachStatements = new List<SqlAttachStatement>();
        DetachStatements = new List<SqlDetachStatement>();
        ReindexStatements = new List<SqlReindexStatement>();
        TransactionStatements = new List<SqlTransactionStatement>();
        SavepointStatements = new List<SqlSavepointStatement>();
        ReleaseStatements = new List<SqlReleaseStatement>();
        ExplainStatements = new List<SqlExplainStatement>();
        TableReferences = new List<SqlTableReference>();
        Joins = new List<SqlJoin>();
        TableDefinitions = new List<SqlTableDefinition>();
        AlterTableDefinitions = new List<SqlAlterTableDefinition>();
        DropTableDefinitions = new List<SqlDropTableDefinition>();
        ColumnDefinitions = new List<SqlColumnDefinition>();
        IndexDefinitions = new List<SqlIndexDefinition>();
        DropIndexDefinitions = new List<SqlDropIndexDefinition>();
        ViewDefinitions = new List<SqlViewDefinition>();
        DropViewDefinitions = new List<SqlDropViewDefinition>();
        VirtualTableDefinitions = new List<SqlVirtualTableDefinition>();
        TriggerDefinitions = new List<SqlTriggerDefinition>();
        DropTriggerDefinitions = new List<SqlDropTriggerDefinition>();
        ColumnConstraints = new List<SqlColumnConstraint>();
        TableConstraints = new List<SqlTableConstraint>();
        ForeignKeyReferences = new List<SqlForeignKeyReference>();
        InsertStatements = new List<SqlInsertStatement>();
        UpdateStatements = new List<SqlUpdateStatement>();
        DeleteStatements = new List<SqlDeleteStatement>();
        MergeStatements = new List<SqlMergeStatement>();
        MergeClauses = new List<SqlMergeWhenClause>();
        Assignments = new List<SqlAssignment>();
        ConflictTargets = new List<SqlConflictTarget>();
        UpsertClauses = new List<SqlUpsertClause>();
        ReturningClauses = new List<SqlReturningClause>();
        UnsupportedSyntax = new List<SqlUnsupportedSyntax>();
    }

    internal void Clear()
    {
        Scripts.Clear();
        Statements.Clear();
        Queries.Clear();
        SelectCores.Clear();
        Expressions.Clear();
        Literals.Clear();
        Names.Clear();
        PragmaStatements.Clear();
        AnalyzeStatements.Clear();
        VacuumStatements.Clear();
        AttachStatements.Clear();
        DetachStatements.Clear();
        ReindexStatements.Clear();
        TransactionStatements.Clear();
        SavepointStatements.Clear();
        ReleaseStatements.Clear();
        ExplainStatements.Clear();
        TableReferences.Clear();
        Joins.Clear();
        TableDefinitions.Clear();
        AlterTableDefinitions.Clear();
        DropTableDefinitions.Clear();
        ColumnDefinitions.Clear();
        IndexDefinitions.Clear();
        DropIndexDefinitions.Clear();
        ViewDefinitions.Clear();
        DropViewDefinitions.Clear();
        VirtualTableDefinitions.Clear();
        TriggerDefinitions.Clear();
        DropTriggerDefinitions.Clear();
        ColumnConstraints.Clear();
        TableConstraints.Clear();
        ForeignKeyReferences.Clear();
        InsertStatements.Clear();
        UpdateStatements.Clear();
        DeleteStatements.Clear();
        MergeStatements.Clear();
        MergeClauses.Clear();
        Assignments.Clear();
        ConflictTargets.Clear();
        UpsertClauses.Clear();
        ReturningClauses.Clear();
        UnsupportedSyntax.Clear();
    }

    internal void WalkScript(SqlScript? script)
    {
        if (script == null)
        {
            return;
        }

        Scripts.Add(script);
        int index = 0;
        while (index < script.Statements.Length)
        {
            WalkStatement(script.Statements[index]);
            index = index + 1;
        }
    }

    internal void WalkStatement(SqlStatement? statement)
    {
        if (statement == null)
        {
            return;
        }

        Statements.Add(statement);
        int unsupportedIndex = 0;
        while (unsupportedIndex < statement.UnsupportedSyntax.Length)
        {
            UnsupportedSyntax.Add(statement.UnsupportedSyntax[unsupportedIndex]);
            unsupportedIndex = unsupportedIndex + 1;
        }

        switch (statement.Kind)
        {
            case SqlStatementKind.Select:
                WalkQuery(statement.Query);
                break;
            case SqlStatementKind.Explain:
                if (statement.Explain != null)
                {
                    WalkExplainStatement(statement.Explain);
                }
                else
                {
                    WalkQuery(statement.Query);
                }

                break;
            case SqlStatementKind.Pragma:
                WalkPragmaStatement(statement.Pragma);
                break;
            case SqlStatementKind.Analyze:
                WalkAnalyzeStatement(statement.Analyze);
                break;
            case SqlStatementKind.Vacuum:
                WalkVacuumStatement(statement.Vacuum);
                break;
            case SqlStatementKind.Attach:
                WalkAttachStatement(statement.Attach);
                break;
            case SqlStatementKind.Detach:
                WalkDetachStatement(statement.Detach);
                break;
            case SqlStatementKind.Reindex:
                WalkReindexStatement(statement.Reindex);
                break;
            case SqlStatementKind.Begin:
            case SqlStatementKind.Commit:
            case SqlStatementKind.End:
            case SqlStatementKind.Rollback:
                WalkTransactionStatement(statement.Transaction);
                break;
            case SqlStatementKind.Savepoint:
                WalkSavepointStatement(statement.Savepoint);
                break;
            case SqlStatementKind.Release:
                WalkReleaseStatement(statement.Release);
                break;
            case SqlStatementKind.Insert:
                WalkInsertStatement(statement.Insert);
                break;
            case SqlStatementKind.Update:
                WalkUpdateStatement(statement.Update);
                break;
            case SqlStatementKind.Delete:
                WalkDeleteStatement(statement.Delete);
                break;
            case SqlStatementKind.Merge:
                WalkMergeStatement(statement.Merge);
                break;
            case SqlStatementKind.CreateTable:
                WalkTableDefinition(statement.CreateTable);
                break;
            case SqlStatementKind.AlterTable:
                WalkAlterTableDefinition(statement.AlterTable);
                break;
            case SqlStatementKind.DropTable:
                WalkDropTableDefinition(statement.DropTable);
                break;
            case SqlStatementKind.CreateIndex:
                WalkIndexDefinition(statement.CreateIndex);
                break;
            case SqlStatementKind.DropIndex:
                WalkDropIndexDefinition(statement.DropIndex);
                break;
            case SqlStatementKind.CreateView:
                WalkViewDefinition(statement.CreateView);
                break;
            case SqlStatementKind.DropView:
                WalkDropViewDefinition(statement.DropView);
                break;
            case SqlStatementKind.CreateTrigger:
                WalkTriggerDefinition(statement.CreateTrigger);
                break;
            case SqlStatementKind.DropTrigger:
                WalkDropTriggerDefinition(statement.DropTrigger);
                break;
            case SqlStatementKind.CreateVirtualTable:
                WalkVirtualTableDefinition(statement.CreateVirtualTable);
                break;
            default:
                WalkQuery(statement.Query);
                WalkInsertStatement(statement.Insert);
                WalkUpdateStatement(statement.Update);
                WalkDeleteStatement(statement.Delete);
                WalkMergeStatement(statement.Merge);
                WalkTableDefinition(statement.CreateTable);
                WalkAlterTableDefinition(statement.AlterTable);
                WalkDropTableDefinition(statement.DropTable);
                WalkIndexDefinition(statement.CreateIndex);
                WalkDropIndexDefinition(statement.DropIndex);
                WalkViewDefinition(statement.CreateView);
                WalkDropViewDefinition(statement.DropView);
                WalkTriggerDefinition(statement.CreateTrigger);
                WalkDropTriggerDefinition(statement.DropTrigger);
                WalkVirtualTableDefinition(statement.CreateVirtualTable);
                WalkPragmaStatement(statement.Pragma);
                WalkAnalyzeStatement(statement.Analyze);
                WalkVacuumStatement(statement.Vacuum);
                WalkAttachStatement(statement.Attach);
                WalkDetachStatement(statement.Detach);
                WalkReindexStatement(statement.Reindex);
                WalkTransactionStatement(statement.Transaction);
                WalkSavepointStatement(statement.Savepoint);
                WalkReleaseStatement(statement.Release);
                WalkExplainStatement(statement.Explain);
                break;
        }
    }

    internal void WalkQuery(SqlQuery? query)
    {
        if (query == null)
        {
            return;
        }

        Queries.Add(query);
        int index = 0;
        while (index < query.CommonTableExpressions.Length)
        {
            WalkCommonTableExpression(query.CommonTableExpressions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < query.SelectCores.Length)
        {
            WalkSelectCore(query.SelectCores[index]);
            index = index + 1;
        }

        index = 0;
        while (index < query.OrderBy.Length)
        {
            WalkOrderingTerm(query.OrderBy[index]);
            index = index + 1;
        }

        WalkLimitClause(query.Limit);
        if (query.OffsetFetch != null)
        {
            WalkExpression(query.OffsetFetch.Offset);
            WalkExpression(query.OffsetFetch.Fetch);
        }
    }

    internal void WalkCommonTableExpression(SqlCommonTableExpression? commonTableExpression)
    {
        if (commonTableExpression == null)
        {
            return;
        }

        WalkName(commonTableExpression.Name);
        int index = 0;
        while (index < commonTableExpression.ColumnNames.Length)
        {
            WalkName(commonTableExpression.ColumnNames[index]);
            index = index + 1;
        }

        WalkQuery(commonTableExpression.Query);
    }

    internal void WalkSelectCore(SqlSelectCore? selectCore)
    {
        if (selectCore == null)
        {
            return;
        }

        SelectCores.Add(selectCore);
        if (selectCore.Top != null)
        {
            WalkExpression(selectCore.Top.Quantity);
        }

        int index = 0;
        while (index < selectCore.ResultColumns.Length)
        {
            WalkResultColumn(selectCore.ResultColumns[index]);
            index = index + 1;
        }

        index = 0;
        while (index < selectCore.Values.Length)
        {
            WalkExpressionArray(selectCore.Values[index]);
            index = index + 1;
        }

        index = 0;
        while (index < selectCore.From.Length)
        {
            WalkTableReference(selectCore.From[index]);
            index = index + 1;
        }

        WalkExpression(selectCore.Where);
        index = 0;
        while (index < selectCore.GroupBy.Length)
        {
            WalkExpression(selectCore.GroupBy[index]);
            index = index + 1;
        }

        WalkExpression(selectCore.Having);
        index = 0;
        while (index < selectCore.Windows.Length)
        {
            WalkWindowDefinition(selectCore.Windows[index]);
            index = index + 1;
        }
    }

    internal void WalkExpression(SqlExpression? expression)
    {
        if (expression == null)
        {
            return;
        }

        Expressions.Add(expression);
        int index = 0;
        switch (expression.Kind)
        {
            case SqlExpressionKind.Name:
                WalkName(expression.Name);
                break;
            case SqlExpressionKind.Literal:
                WalkLiteral(expression.Literal);
                break;
            case SqlExpressionKind.Unary:
            case SqlExpressionKind.Parenthesized:
                WalkExpression(expression.Operand);
                break;
            case SqlExpressionKind.Collate:
                WalkExpression(expression.Operand);
                WalkName(expression.Name);
                break;
            case SqlExpressionKind.Binary:
            case SqlExpressionKind.JsonOperator:
            case SqlExpressionKind.Is:
            case SqlExpressionKind.Distinctness:
            case SqlExpressionKind.Like:
                WalkExpression(expression.Left);
                WalkExpression(expression.Right);
                WalkExpression(expression.Alternate);
                break;
            case SqlExpressionKind.FunctionCall:
                WalkExpressionArray(expression.Arguments);
                index = 0;
                while (index < expression.OrderBy.Length)
                {
                    WalkOrderingTerm(expression.OrderBy[index]);
                    index = index + 1;
                }

                WalkExpression(expression.Filter);
                break;
            case SqlExpressionKind.RowValue:
                WalkExpressionArray(expression.Arguments);
                break;
            case SqlExpressionKind.Cast:
                WalkExpression(expression.Operand);
                break;
            case SqlExpressionKind.Extract:
                WalkExpression(expression.Operand);
                break;
            case SqlExpressionKind.Array:
                WalkExpressionArray(expression.Arguments);
                WalkQuery(expression.Query);
                break;
            case SqlExpressionKind.Subscript:
                WalkExpression(expression.Operand);
                WalkExpression(expression.Left);
                WalkExpression(expression.Right);
                break;
            case SqlExpressionKind.Exists:
            case SqlExpressionKind.Subquery:
                WalkQuery(expression.Query);
                break;
            case SqlExpressionKind.Between:
            case SqlExpressionKind.In:
            case SqlExpressionKind.Case:
                WalkExpression(expression.Operand);
                WalkExpression(expression.Left);
                WalkExpression(expression.Right);
                WalkExpression(expression.Alternate);
                WalkExpressionArray(expression.Arguments);
                WalkQuery(expression.Query);
                break;
            case SqlExpressionKind.Window:
                WalkExpression(expression.Operand);
                WalkWindowDefinition(expression.WindowDefinition);
                break;
            default:
                WalkExpression(expression.Operand);
                WalkExpression(expression.Left);
                WalkExpression(expression.Right);
                WalkExpression(expression.Alternate);
                WalkExpressionArray(expression.Arguments);
                WalkName(expression.Name);
                WalkLiteral(expression.Literal);
                WalkQuery(expression.Query);
                break;
        }
    }

    internal void WalkExpressionObject(object? value)
    {
        SqlExpression? expression = value as SqlExpression;
        if (expression != null)
        {
            WalkExpression(expression);
            return;
        }

        SqlPragmaStatement? pragmaStatement = value as SqlPragmaStatement;
        if (pragmaStatement != null)
        {
            WalkPragmaStatement(pragmaStatement);
        }
    }

    internal void WalkPragmaStatement(SqlPragmaStatement? pragmaStatement)
    {
        if (pragmaStatement == null)
        {
            return;
        }

        PragmaStatements.Add(pragmaStatement);
        WalkName(pragmaStatement.Name);
        WalkExpression(pragmaStatement.Value);
    }

    internal void WalkAnalyzeStatement(SqlAnalyzeStatement? analyzeStatement)
    {
        if (analyzeStatement == null)
        {
            return;
        }

        AnalyzeStatements.Add(analyzeStatement);
        WalkName(analyzeStatement.TargetName);
    }

    internal void WalkVacuumStatement(SqlVacuumStatement? vacuumStatement)
    {
        if (vacuumStatement == null)
        {
            return;
        }

        VacuumStatements.Add(vacuumStatement);
        WalkName(vacuumStatement.DatabaseName);
        WalkExpression(vacuumStatement.IntoExpression);
    }

    internal void WalkAttachStatement(SqlAttachStatement? attachStatement)
    {
        if (attachStatement == null)
        {
            return;
        }

        AttachStatements.Add(attachStatement);
        WalkExpression(attachStatement.DatabaseExpression);
        WalkName(attachStatement.SchemaName);
    }

    internal void WalkDetachStatement(SqlDetachStatement? detachStatement)
    {
        if (detachStatement == null)
        {
            return;
        }

        DetachStatements.Add(detachStatement);
        WalkName(detachStatement.SchemaName);
    }

    internal void WalkReindexStatement(SqlReindexStatement? reindexStatement)
    {
        if (reindexStatement == null)
        {
            return;
        }

        ReindexStatements.Add(reindexStatement);
        WalkName(reindexStatement.TargetName);
    }

    internal void WalkTransactionStatement(SqlTransactionStatement? transactionStatement)
    {
        if (transactionStatement == null)
        {
            return;
        }

        TransactionStatements.Add(transactionStatement);
        if (transactionStatement.RollbackTarget != null)
        {
            WalkName(transactionStatement.RollbackTarget.Name);
        }
        else
        {
            WalkName(transactionStatement.SavepointName);
        }
    }

    internal void WalkSavepointStatement(SqlSavepointStatement? savepointStatement)
    {
        if (savepointStatement == null)
        {
            return;
        }

        SavepointStatements.Add(savepointStatement);
        WalkName(savepointStatement.Name);
    }

    internal void WalkReleaseStatement(SqlReleaseStatement? releaseStatement)
    {
        if (releaseStatement == null)
        {
            return;
        }

        ReleaseStatements.Add(releaseStatement);
        WalkName(releaseStatement.Name);
    }

    internal void WalkExplainStatement(SqlExplainStatement? explainStatement)
    {
        if (explainStatement == null)
        {
            return;
        }

        ExplainStatements.Add(explainStatement);
        WalkStatement(explainStatement.Statement);
    }

    internal void WalkLiteral(SqlLiteral? literal)
    {
        if (literal == null)
        {
            return;
        }

        Literals.Add(literal);
    }

    internal void WalkName(SqlName? name)
    {
        if (name == null)
        {
            return;
        }

        Names.Add(name);
    }

    internal void WalkTableReference(SqlTableReference? tableReference)
    {
        if (tableReference == null)
        {
            return;
        }

        TableReferences.Add(tableReference);
        switch (tableReference.Kind)
        {
            case SqlTableReferenceKind.Named:
                WalkName(tableReference.Name);
                break;
            case SqlTableReferenceKind.Subquery:
                WalkQuery(tableReference.Query);
                break;
            case SqlTableReferenceKind.Function:
                WalkExpression(tableReference.FunctionExpression);
                break;
            case SqlTableReferenceKind.Parenthesized:
                WalkTableReference(tableReference.Inner);
                break;
            default:
                WalkName(tableReference.Name);
                WalkQuery(tableReference.Query);
                WalkExpression(tableReference.FunctionExpression);
                WalkTableReference(tableReference.Inner);
                break;
        }

        int index = 0;
        while (index < tableReference.Joins.Length)
        {
            WalkJoin(tableReference.Joins[index]);
            index = index + 1;
        }

        WalkName(tableReference.Alias);
        index = 0;
        while (index < tableReference.ColumnAliases.Length)
        {
            WalkName(tableReference.ColumnAliases[index]);
            index = index + 1;
        }
    }

    internal void WalkJoin(SqlJoin? join)
    {
        if (join == null)
        {
            return;
        }

        Joins.Add(join);
        WalkTableReference(join.Right);
        switch (join.ConstraintKind)
        {
            case SqlJoinConstraintKind.On:
                WalkExpression(join.OnExpression);
                break;
            case SqlJoinConstraintKind.Using:
                WalkNameArray(join.UsingColumns);
                break;
            default:
                WalkExpression(join.OnExpression);
                WalkNameArray(join.UsingColumns);
                break;
        }
    }

    internal void WalkOrderingTerm(SqlOrderingTerm? orderingTerm)
    {
        if (orderingTerm == null)
        {
            return;
        }

        WalkExpression(orderingTerm.Expression);
        WalkName(orderingTerm.Collation);
    }

    internal void WalkLimitClause(SqlLimitClause? limitClause)
    {
        if (limitClause == null)
        {
            return;
        }

        WalkExpression(limitClause.LimitExpression);
        WalkExpression(limitClause.OffsetExpression);
    }

    internal void WalkWindowDefinition(SqlWindowDefinition? windowDefinition)
    {
        if (windowDefinition == null)
        {
            return;
        }

        WalkName(windowDefinition.Name);
        WalkName(windowDefinition.BaseWindowName);
        WalkExpressionArray(windowDefinition.PartitionBy);
        int index = 0;
        while (index < windowDefinition.OrderBy.Length)
        {
            WalkOrderingTerm(windowDefinition.OrderBy[index]);
            index = index + 1;
        }

        WalkWindowFrame(windowDefinition.Frame);
    }

    internal void WalkWindowFrame(SqlWindowFrame? windowFrame)
    {
        if (windowFrame == null)
        {
            return;
        }

        WalkWindowFrameBoundary(windowFrame.Start);
        WalkWindowFrameBoundary(windowFrame.End);
    }

    internal void WalkWindowFrameBoundary(SqlWindowFrameBoundary? boundary)
    {
        if (boundary == null)
        {
            return;
        }

        WalkExpression(boundary.Expression);
    }

    internal void WalkTableDefinition(SqlTableDefinition? tableDefinition)
    {
        if (tableDefinition == null)
        {
            return;
        }

        TableDefinitions.Add(tableDefinition);
        WalkName(tableDefinition.Name);
        int index = 0;
        while (index < tableDefinition.Columns.Length)
        {
            WalkColumnDefinition(tableDefinition.Columns[index]);
            index = index + 1;
        }

        index = 0;
        while (index < tableDefinition.Constraints.Length)
        {
            WalkTableConstraint(tableDefinition.Constraints[index]);
            index = index + 1;
        }

        WalkQuery(tableDefinition.Query);
    }

    internal void WalkAlterTableDefinition(SqlAlterTableDefinition? alterTableDefinition)
    {
        if (alterTableDefinition == null)
        {
            return;
        }

        AlterTableDefinitions.Add(alterTableDefinition);
        WalkName(alterTableDefinition.TableName);
        WalkName(alterTableDefinition.NewTableName);
        WalkName(alterTableDefinition.ColumnName);
        WalkName(alterTableDefinition.NewColumnName);
        WalkColumnDefinition(alterTableDefinition.ColumnDefinition);
    }

    internal void WalkDropTableDefinition(SqlDropTableDefinition? dropTableDefinition)
    {
        if (dropTableDefinition == null)
        {
            return;
        }

        DropTableDefinitions.Add(dropTableDefinition);
        WalkName(dropTableDefinition.Name);
    }

    internal void WalkColumnDefinition(SqlColumnDefinition? columnDefinition)
    {
        if (columnDefinition == null)
        {
            return;
        }

        ColumnDefinitions.Add(columnDefinition);
        WalkName(columnDefinition.Name);
        int index = 0;
        while (index < columnDefinition.Constraints.Length)
        {
            WalkColumnConstraint(columnDefinition.Constraints[index]);
            index = index + 1;
        }

        WalkExpression(columnDefinition.DefaultExpression);
        WalkExpression(columnDefinition.GeneratedExpression);
    }

    internal void WalkIndexDefinition(SqlIndexDefinition? indexDefinition)
    {
        if (indexDefinition == null)
        {
            return;
        }

        IndexDefinitions.Add(indexDefinition);
        WalkName(indexDefinition.Name);
        WalkName(indexDefinition.TableName);
        int index = 0;
        while (index < indexDefinition.KeyExpressions.Length)
        {
            WalkOrderingTerm(indexDefinition.KeyExpressions[index]);
            index = index + 1;
        }

        WalkExpression(indexDefinition.WhereExpression);
    }

    internal void WalkDropIndexDefinition(SqlDropIndexDefinition? dropIndexDefinition)
    {
        if (dropIndexDefinition == null)
        {
            return;
        }

        DropIndexDefinitions.Add(dropIndexDefinition);
        WalkName(dropIndexDefinition.Name);
    }

    internal void WalkViewDefinition(SqlViewDefinition? viewDefinition)
    {
        if (viewDefinition == null)
        {
            return;
        }

        ViewDefinitions.Add(viewDefinition);
        WalkName(viewDefinition.Name);
        WalkNameArray(viewDefinition.ColumnNames);
        WalkQuery(viewDefinition.Query);
    }

    internal void WalkDropViewDefinition(SqlDropViewDefinition? dropViewDefinition)
    {
        if (dropViewDefinition == null)
        {
            return;
        }

        DropViewDefinitions.Add(dropViewDefinition);
        WalkName(dropViewDefinition.Name);
    }

    internal void WalkVirtualTableDefinition(SqlVirtualTableDefinition? virtualTableDefinition)
    {
        if (virtualTableDefinition == null)
        {
            return;
        }

        VirtualTableDefinitions.Add(virtualTableDefinition);
        WalkName(virtualTableDefinition.Name);
        WalkName(virtualTableDefinition.ModuleName);
    }

    internal void WalkTriggerDefinition(SqlTriggerDefinition? triggerDefinition)
    {
        if (triggerDefinition == null)
        {
            return;
        }

        TriggerDefinitions.Add(triggerDefinition);
        WalkName(triggerDefinition.Name);
        WalkNameArray(triggerDefinition.UpdateColumns);
        WalkName(triggerDefinition.TargetName);
        WalkExpression(triggerDefinition.WhenExpression);
        int index = 0;
        while (index < triggerDefinition.Body.Length)
        {
            WalkStatement(triggerDefinition.Body[index]);
            index = index + 1;
        }
    }

    internal void WalkDropTriggerDefinition(SqlDropTriggerDefinition? dropTriggerDefinition)
    {
        if (dropTriggerDefinition == null)
        {
            return;
        }

        DropTriggerDefinitions.Add(dropTriggerDefinition);
        WalkName(dropTriggerDefinition.Name);
    }

    internal void WalkColumnConstraint(SqlColumnConstraint? columnConstraint)
    {
        if (columnConstraint == null)
        {
            return;
        }

        ColumnConstraints.Add(columnConstraint);
        WalkName(columnConstraint.Name);
        WalkExpression(columnConstraint.Expression);
        WalkName(columnConstraint.CollationName);
        WalkForeignKeyReference(columnConstraint.References);
    }

    internal void WalkTableConstraint(SqlTableConstraint? tableConstraint)
    {
        if (tableConstraint == null)
        {
            return;
        }

        TableConstraints.Add(tableConstraint);
        WalkName(tableConstraint.Name);
        if (tableConstraint.IndexedColumns.Length > 0)
        {
            int index = 0;
            while (index < tableConstraint.IndexedColumns.Length)
            {
                WalkOrderingTerm(tableConstraint.IndexedColumns[index]);
                index = index + 1;
            }
        }
        else
        {
            WalkNameArray(tableConstraint.Columns);
        }

        WalkExpressionArray(tableConstraint.Expressions);
        WalkForeignKeyReference(tableConstraint.References);
    }

    internal void WalkForeignKeyReference(SqlForeignKeyReference? foreignKeyReference)
    {
        if (foreignKeyReference == null)
        {
            return;
        }

        ForeignKeyReferences.Add(foreignKeyReference);
        WalkName(foreignKeyReference.TableName);
        WalkNameArray(foreignKeyReference.ColumnNames);
        if (foreignKeyReference.Clauses.Length > 0)
        {
            int index = 0;
            while (index < foreignKeyReference.Clauses.Length)
            {
                WalkName(foreignKeyReference.Clauses[index].MatchName);
                index = index + 1;
            }
        }
        else
        {
            WalkName(foreignKeyReference.MatchName);
        }
    }

    internal void WalkPayloadObject(object? value)
    {
        SqlResultColumn? resultColumn = value as SqlResultColumn;
        if (resultColumn != null)
        {
            WalkResultColumn(resultColumn);
            return;
        }

        SqlExpression? expression = value as SqlExpression;
        if (expression != null)
        {
            WalkExpression(expression);
            return;
        }

        SqlLiteral? literal = value as SqlLiteral;
        if (literal != null)
        {
            WalkLiteral(literal);
            return;
        }

        SqlName? name = value as SqlName;
        if (name != null)
        {
            WalkName(name);
            return;
        }

        SqlQuery? query = value as SqlQuery;
        if (query != null)
        {
            WalkQuery(query);
            return;
        }

        SqlTableReference? tableReference = value as SqlTableReference;
        if (tableReference != null)
        {
            WalkTableReference(tableReference);
            return;
        }

        SqlTableDefinition? tableDefinition = value as SqlTableDefinition;
        if (tableDefinition != null)
        {
            WalkTableDefinition(tableDefinition);
            return;
        }

        SqlAlterTableDefinition? alterTableDefinition = value as SqlAlterTableDefinition;
        if (alterTableDefinition != null)
        {
            WalkAlterTableDefinition(alterTableDefinition);
            return;
        }

        SqlDropTableDefinition? dropTableDefinition = value as SqlDropTableDefinition;
        if (dropTableDefinition != null)
        {
            WalkDropTableDefinition(dropTableDefinition);
            return;
        }

        SqlColumnDefinition? columnDefinition = value as SqlColumnDefinition;
        if (columnDefinition != null)
        {
            WalkColumnDefinition(columnDefinition);
            return;
        }

        SqlIndexDefinition? indexDefinition = value as SqlIndexDefinition;
        if (indexDefinition != null)
        {
            WalkIndexDefinition(indexDefinition);
            return;
        }

        SqlDropIndexDefinition? dropIndexDefinition = value as SqlDropIndexDefinition;
        if (dropIndexDefinition != null)
        {
            WalkDropIndexDefinition(dropIndexDefinition);
            return;
        }

        SqlViewDefinition? viewDefinition = value as SqlViewDefinition;
        if (viewDefinition != null)
        {
            WalkViewDefinition(viewDefinition);
            return;
        }

        SqlDropViewDefinition? dropViewDefinition = value as SqlDropViewDefinition;
        if (dropViewDefinition != null)
        {
            WalkDropViewDefinition(dropViewDefinition);
            return;
        }

        SqlVirtualTableDefinition? virtualTableDefinition = value as SqlVirtualTableDefinition;
        if (virtualTableDefinition != null)
        {
            WalkVirtualTableDefinition(virtualTableDefinition);
            return;
        }

        SqlTriggerDefinition? triggerDefinition = value as SqlTriggerDefinition;
        if (triggerDefinition != null)
        {
            WalkTriggerDefinition(triggerDefinition);
            return;
        }

        SqlDropTriggerDefinition? dropTriggerDefinition = value as SqlDropTriggerDefinition;
        if (dropTriggerDefinition != null)
        {
            WalkDropTriggerDefinition(dropTriggerDefinition);
            return;
        }

        SqlAnalyzeStatement? analyzeStatement = value as SqlAnalyzeStatement;
        if (analyzeStatement != null)
        {
            WalkAnalyzeStatement(analyzeStatement);
            return;
        }

        SqlVacuumStatement? vacuumStatement = value as SqlVacuumStatement;
        if (vacuumStatement != null)
        {
            WalkVacuumStatement(vacuumStatement);
            return;
        }

        SqlAttachStatement? attachStatement = value as SqlAttachStatement;
        if (attachStatement != null)
        {
            WalkAttachStatement(attachStatement);
            return;
        }

        SqlDetachStatement? detachStatement = value as SqlDetachStatement;
        if (detachStatement != null)
        {
            WalkDetachStatement(detachStatement);
            return;
        }

        SqlReindexStatement? reindexStatement = value as SqlReindexStatement;
        if (reindexStatement != null)
        {
            WalkReindexStatement(reindexStatement);
            return;
        }

        SqlTransactionStatement? transactionStatement = value as SqlTransactionStatement;
        if (transactionStatement != null)
        {
            WalkTransactionStatement(transactionStatement);
            return;
        }

        SqlSavepointStatement? savepointStatement = value as SqlSavepointStatement;
        if (savepointStatement != null)
        {
            WalkSavepointStatement(savepointStatement);
            return;
        }

        SqlReleaseStatement? releaseStatement = value as SqlReleaseStatement;
        if (releaseStatement != null)
        {
            WalkReleaseStatement(releaseStatement);
            return;
        }

        SqlExplainStatement? explainStatement = value as SqlExplainStatement;
        if (explainStatement != null)
        {
            WalkExplainStatement(explainStatement);
            return;
        }

        SqlColumnConstraint? columnConstraint = value as SqlColumnConstraint;
        if (columnConstraint != null)
        {
            WalkColumnConstraint(columnConstraint);
            return;
        }

        SqlTableConstraint? tableConstraint = value as SqlTableConstraint;
        if (tableConstraint != null)
        {
            WalkTableConstraint(tableConstraint);
            return;
        }

        SqlForeignKeyReference? foreignKeyReference = value as SqlForeignKeyReference;
        if (foreignKeyReference != null)
        {
            WalkForeignKeyReference(foreignKeyReference);
            return;
        }

        SqlInsertStatement? insertStatement = value as SqlInsertStatement;
        if (insertStatement != null)
        {
            WalkInsertStatement(insertStatement);
            return;
        }

        SqlUpdateStatement? updateStatement = value as SqlUpdateStatement;
        if (updateStatement != null)
        {
            WalkUpdateStatement(updateStatement);
            return;
        }

        SqlDeleteStatement? deleteStatement = value as SqlDeleteStatement;
        if (deleteStatement != null)
        {
            WalkDeleteStatement(deleteStatement);
            return;
        }

        SqlMergeStatement? mergeStatement = value as SqlMergeStatement;
        if (mergeStatement != null)
        {
            WalkMergeStatement(mergeStatement);
            return;
        }

        SqlMergeWhenClause? mergeClause = value as SqlMergeWhenClause;
        if (mergeClause != null)
        {
            WalkMergeWhenClause(mergeClause);
            return;
        }

        SqlAssignment? assignment = value as SqlAssignment;
        if (assignment != null)
        {
            WalkAssignment(assignment);
            return;
        }

        SqlConflictTarget? conflictTarget = value as SqlConflictTarget;
        if (conflictTarget != null)
        {
            WalkConflictTarget(conflictTarget);
            return;
        }

        SqlUpsertClause? upsertClause = value as SqlUpsertClause;
        if (upsertClause != null)
        {
            WalkUpsertClause(upsertClause);
            return;
        }

        SqlReturningClause? returningClause = value as SqlReturningClause;
        if (returningClause != null)
        {
            WalkReturningClause(returningClause);
            return;
        }
    }

    internal void WalkInsertStatement(SqlInsertStatement? insertStatement)
    {
        if (insertStatement == null)
        {
            return;
        }

        InsertStatements.Add(insertStatement);
        WalkName(insertStatement.TableName);
        WalkName(insertStatement.Alias);
        WalkNameArray(insertStatement.ColumnNames);
        int index = 0;
        while (index < insertStatement.Values.Length)
        {
            WalkExpressionArray(insertStatement.Values[index]);
            index = index + 1;
        }

        WalkQuery(insertStatement.SourceQuery);
        index = 0;
        while (index < insertStatement.UpsertClauses.Length)
        {
            WalkUpsertClause(insertStatement.UpsertClauses[index]);
            index = index + 1;
        }

        WalkReturningClause(insertStatement.Returning);
    }

    internal void WalkUpsertClause(SqlUpsertClause? upsertClause)
    {
        if (upsertClause == null)
        {
            return;
        }

        UpsertClauses.Add(upsertClause);
        int index = 0;
        if (upsertClause.ConflictTarget != null)
        {
            WalkConflictTarget(upsertClause.ConflictTarget);
        }
        else
        {
            while (index < upsertClause.ConflictTargets.Length)
            {
                WalkOrderingTerm(upsertClause.ConflictTargets[index]);
                index = index + 1;
            }

            WalkExpression(upsertClause.ConflictWhere);
        }
        if (upsertClause.Assignments.Length > 0)
        {
            WalkAssignmentArray(upsertClause.Assignments);
        }
        else
        {
            WalkNameArray(upsertClause.UpdateColumns);
            WalkExpressionArray(upsertClause.UpdateExpressions);
        }
        WalkExpression(upsertClause.UpdateWhere);
    }

    internal void WalkReturningClause(SqlReturningClause? returningClause)
    {
        if (returningClause == null)
        {
            return;
        }

        ReturningClauses.Add(returningClause);
        int index = 0;
        while (index < returningClause.ResultColumns.Length)
        {
            WalkResultColumn(returningClause.ResultColumns[index]);
            index = index + 1;
        }
    }

    internal void WalkUpdateStatement(SqlUpdateStatement? updateStatement)
    {
        if (updateStatement == null)
        {
            return;
        }

        UpdateStatements.Add(updateStatement);
        WalkName(updateStatement.TableName);
        WalkName(updateStatement.Alias);
        WalkName(updateStatement.IndexedBy);
        if (updateStatement.Assignments.Length > 0)
        {
            WalkAssignmentArray(updateStatement.Assignments);
        }
        else
        {
            WalkNameArray(updateStatement.AssignmentColumns);
            WalkExpressionArray(updateStatement.AssignmentExpressions);
        }
        int index = 0;
        while (index < updateStatement.From.Length)
        {
            WalkTableReference(updateStatement.From[index]);
            index = index + 1;
        }

        WalkExpression(updateStatement.Where);
        WalkReturningClause(updateStatement.Returning);
        index = 0;
        while (index < updateStatement.OrderBy.Length)
        {
            WalkOrderingTerm(updateStatement.OrderBy[index]);
            index = index + 1;
        }

        WalkLimitClause(updateStatement.Limit);
    }

    internal void WalkDeleteStatement(SqlDeleteStatement? deleteStatement)
    {
        if (deleteStatement == null)
        {
            return;
        }

        DeleteStatements.Add(deleteStatement);
        WalkName(deleteStatement.TableName);
        WalkName(deleteStatement.Alias);
        WalkName(deleteStatement.IndexedBy);
        WalkExpression(deleteStatement.Where);
        WalkReturningClause(deleteStatement.Returning);
        int index = 0;
        while (index < deleteStatement.OrderBy.Length)
        {
            WalkOrderingTerm(deleteStatement.OrderBy[index]);
            index = index + 1;
        }

        WalkLimitClause(deleteStatement.Limit);
    }

    internal void WalkMergeStatement(SqlMergeStatement? mergeStatement)
    {
        if (mergeStatement == null)
        {
            return;
        }

        MergeStatements.Add(mergeStatement);
        WalkName(mergeStatement.TableName);
        WalkName(mergeStatement.Alias);
        WalkTableReference(mergeStatement.Source);
        WalkExpression(mergeStatement.Condition);
        int index = 0;
        while (index < mergeStatement.Clauses.Length)
        {
            WalkMergeWhenClause(mergeStatement.Clauses[index]);
            index = index + 1;
        }

        WalkReturningClause(mergeStatement.Returning);
    }

    internal void WalkMergeWhenClause(SqlMergeWhenClause? clause)
    {
        if (clause == null)
        {
            return;
        }

        MergeClauses.Add(clause);
        WalkExpression(clause.Condition);
        WalkAssignmentArray(clause.Assignments);
        WalkNameArray(clause.InsertColumns);
        WalkExpressionArray(clause.InsertValues);
    }

    internal void WalkAssignmentArray(SqlAssignment[] assignments)
    {
        int index = 0;
        while (index < assignments.Length)
        {
            WalkAssignment(assignments[index]);
            index = index + 1;
        }
    }

    internal void WalkAssignment(SqlAssignment? assignment)
    {
        if (assignment == null)
        {
            return;
        }

        Assignments.Add(assignment);
        WalkNameArray(assignment.Columns);
        WalkExpression(assignment.Value);
    }

    internal void WalkConflictTarget(SqlConflictTarget? target)
    {
        if (target == null)
        {
            return;
        }

        ConflictTargets.Add(target);
        int index = 0;
        while (index < target.IndexedColumns.Length)
        {
            WalkOrderingTerm(target.IndexedColumns[index]);
            index = index + 1;
        }

        WalkName(target.ConstraintName);
        WalkExpression(target.Predicate);
    }

    internal void WalkResultColumn(SqlResultColumn? resultColumn)
    {
        if (resultColumn == null)
        {
            return;
        }

        WalkExpression(resultColumn.Expression);
        WalkName(resultColumn.Qualifier);
        WalkName(resultColumn.Alias);
    }

    internal void WalkExpressionArray(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            return;
        }

        int index = 0;
        while (index < expressions.Length)
        {
            WalkExpression(expressions[index]);
            index = index + 1;
        }
    }

    internal void WalkNameArray(SqlName[] names)
    {
        if (names == null)
        {
            return;
        }

        int index = 0;
        while (index < names.Length)
        {
            WalkName(names[index]);
            index = index + 1;
        }
    }
}
