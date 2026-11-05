using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstWalkerTests
{
    [Test]
    public void Walker_WalksScriptStatementsQueriesExpressionsTablesAndJoins()
    {
        SqlLiteral literal = SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(7, 1), 1L);
        SqlExpression resultExpression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(7, 1));
        resultExpression.Literal = literal;
        SqlResultColumn result = new SqlResultColumn(resultExpression, false, null, null, resultExpression.Span, new SqlToken[0]);
        SqlName leftName = Name("items", 14);
        SqlName rightName = Name("orders", 25);
        SqlTableReference right = SqlTableReference.Create(SqlTableReferenceKind.Named, rightName.Span, rightName, null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlExpression on = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(35, 10));
        SqlJoin join = new SqlJoin(SqlJoinKind.Inner, false, false, right, SqlJoinConstraintKind.On, on, new SqlName[0], new SqlTextSpan(19, 26));
        SqlTableReference from = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(14, 31), leftName, null, null, null, new SqlJoin[] { join }, null, new SqlName[0]);
        SqlSelectCore core = SqlSelectCore.Create(false, false, new SqlResultColumn[] { result }, new SqlTableReference[] { from }, null, new SqlExpression[0], null, new SqlWindowDefinition[0], new SqlTextSpan(0, 45));
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[] { core }, new SqlTextSpan(0, 45));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 45));
        statement.Query = query;
        SqlScript script = SqlScript.Create(new SqlStatement[] { statement }, new SqlTextSpan(0, 45));
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkScript(script);

        Assert.That(walker.Scripts, Has.Count.EqualTo(1));
        Assert.That(walker.Statements, Has.Count.EqualTo(1));
        Assert.That(walker.Queries, Has.Count.EqualTo(1));
        Assert.That(walker.SelectCores, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Has.Count.EqualTo(2));
        Assert.That(walker.Literals, Has.Count.EqualTo(1));
        Assert.That(walker.TableReferences, Has.Count.EqualTo(2));
        Assert.That(walker.Joins, Has.Count.EqualTo(1));
        Assert.That(walker.Names, Does.Contain(leftName));
        Assert.That(walker.Names, Does.Contain(rightName));
    }

    [Test]
    public void Walker_WalksSchemaPayloadsFromStatements()
    {
        SqlForeignKeyReference reference = new SqlForeignKeyReference(Name("parents", 50), new SqlName[] { Name("id", 58) }, "", "", null, false, false, new SqlTextSpan(50, 10));
        SqlColumnConstraint columnConstraint = new SqlColumnConstraint(SqlConstraintKind.References, null, null, null, SqlConflictActionKind.None, reference, SqlGeneratedColumnKind.None, false, new SqlTextSpan(40, 20));
        SqlColumnDefinition column = new SqlColumnDefinition(Name("parent_id", 14), "integer", "INTEGER", new SqlColumnConstraint[] { columnConstraint }, null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(14, 26));
        SqlExpression check = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(65, 8));
        SqlTableConstraint tableConstraint = new SqlTableConstraint(SqlConstraintKind.Check, Name("ck_items", 62), new SqlName[0], new SqlExpression[] { check }, SqlConflictActionKind.None, null, new SqlTextSpan(62, 14));
        SqlTableDefinition table = new SqlTableDefinition(false, true, Name("items", 0), new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { tableConstraint }, null, false, true, new SqlTextSpan(0, 80));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.CreateTable, new SqlTextSpan(0, 80));
        statement.CreateTable = table;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.Statements, Has.Count.EqualTo(1));
        Assert.That(walker.TableDefinitions, Has.Count.EqualTo(1));
        Assert.That(walker.ColumnDefinitions, Has.Count.EqualTo(1));
        Assert.That(walker.ColumnConstraints, Has.Count.EqualTo(1));
        Assert.That(walker.TableConstraints, Has.Count.EqualTo(1));
        Assert.That(walker.ForeignKeyReferences, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Does.Contain(check));
        Assert.That(walker.Names, Does.Contain(table.Name));
        Assert.That(walker.Names, Does.Contain(column.Name));
    }

    [Test]
    public void Walker_UsesExpressionKindSwitchesForCommonPayloads()
    {
        SqlExpression left = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        left.Name = Name("a", 0);
        SqlExpression right = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(4, 1));
        right.Literal = SqlLiteral.Create(SqlLiteralKind.Integer, "1", new SqlTextSpan(4, 1), 1L);
        SqlExpression binary = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(0, 5));
        binary.Left = left;
        binary.Right = right;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkExpression(binary);

        Assert.That(walker.Expressions, Has.Count.EqualTo(3));
        Assert.That(walker.Names, Has.Count.EqualTo(1));
        Assert.That(walker.Literals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Walker_WalksFunctionCallAggregateOptions()
    {
        SqlExpression argument = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(4, 1));
        SqlExpression orderExpression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(15, 1));
        SqlExpression filter = SqlExpression.Create(SqlExpressionKind.Binary, new SqlTextSpan(30, 5));
        SqlOrderingTerm order = new SqlOrderingTerm(orderExpression, null, SqlOrderingDirection.Desc, SqlNullOrdering.None, new SqlTextSpan(15, 6));
        SqlExpression function = SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(0, 35), new SqlToken[0], new SqlExpression[] { argument });
        function.OrderBy = new SqlOrderingTerm[] { order };
        function.Filter = filter;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkExpression(function);

        Assert.That(walker.Expressions, Does.Contain(function));
        Assert.That(walker.Expressions, Does.Contain(argument));
        Assert.That(walker.Expressions, Does.Contain(orderExpression));
        Assert.That(walker.Expressions, Does.Contain(filter));
    }

    [Test]
    public void Walker_WalksWindowExpressionDefinition()
    {
        SqlExpression function = SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(0, 8));
        SqlExpression partition = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(20, 1));
        SqlExpression orderExpression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(32, 1));
        SqlOrderingTerm order = new SqlOrderingTerm(orderExpression, null, SqlOrderingDirection.Asc, SqlNullOrdering.None, new SqlTextSpan(32, 1));
        SqlWindowDefinition definition = new SqlWindowDefinition(null, null, new SqlExpression[] { partition }, new SqlOrderingTerm[] { order }, null, new SqlTextSpan(10, 28));
        SqlExpression window = SqlExpression.Create(SqlExpressionKind.Window, new SqlTextSpan(0, 38));
        window.Operand = function;
        window.WindowDefinition = definition;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkExpression(window);

        Assert.That(walker.Expressions, Does.Contain(window));
        Assert.That(walker.Expressions, Does.Contain(function));
        Assert.That(walker.Expressions, Does.Contain(partition));
        Assert.That(walker.Expressions, Does.Contain(orderExpression));
    }

    [Test]
    public void Walker_WalksCollateExpressionName()
    {
        SqlExpression operand = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        SqlName collation = Name("nocase", 10);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Collate, new SqlTextSpan(0, 16));
        expression.Operand = operand;
        expression.Name = collation;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkExpression(expression);

        Assert.That(walker.Expressions, Does.Contain(operand));
        Assert.That(walker.Names, Does.Contain(collation));
    }

    [Test]
    public void Walker_WalksResultColumnPayload()
    {
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 1));
        SqlName alias = Name("alias", 5);
        SqlResultColumn column = new SqlResultColumn(expression, false, null, alias, new SqlTextSpan(0, 10), new SqlToken[0]);
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkPayloadObject(column);

        Assert.That(walker.Expressions, Does.Contain(expression));
        Assert.That(walker.Names, Does.Contain(alias));
    }

    [Test]
    public void Walker_ClearResetsCollectedNodesAndNullInputsAreIgnored()
    {
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkName(Name("x", 0));
        walker.WalkExpression(null);
        walker.WalkQuery(null);
        walker.WalkPayloadObject(null);

        Assert.That(walker.Names, Has.Count.EqualTo(1));

        walker.Clear();

        Assert.That(walker.Names, Has.Count.EqualTo(0));
        Assert.That(walker.Expressions, Has.Count.EqualTo(0));
        Assert.That(walker.Statements, Has.Count.EqualTo(0));
    }

    [Test]
    public void Walker_WalksAnalyzeStatementPayload()
    {
        SqlName name = Name("items", 8);
        SqlAnalyzeStatement analyze = new SqlAnalyzeStatement(name, new SqlTextSpan(0, 13));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Analyze, analyze.Span);
        statement.Analyze = analyze;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.AnalyzeStatements, Does.Contain(analyze));
        Assert.That(walker.Names, Does.Contain(name));
    }

    [Test]
    public void Walker_WalksVacuumStatementPayload()
    {
        SqlName name = Name("main", 7);
        SqlExpression intoExpression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(17, 11));
        SqlVacuumStatement vacuum = new SqlVacuumStatement(name, intoExpression, new SqlTextSpan(0, 28));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Vacuum, vacuum.Span);
        statement.Vacuum = vacuum;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.VacuumStatements, Does.Contain(vacuum));
        Assert.That(walker.Names, Does.Contain(name));
        Assert.That(walker.Expressions, Does.Contain(intoExpression));
    }

    [Test]
    public void Walker_WalksAttachAndDetachStatementPayloads()
    {
        SqlExpression databaseExpression = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(7, 10));
        SqlName attachSchema = Name("aux", 21);
        SqlName detachSchema = Name("aux", 7);
        SqlAttachStatement attach = new SqlAttachStatement(databaseExpression, attachSchema, new SqlTextSpan(0, 24));
        SqlDetachStatement detach = new SqlDetachStatement(detachSchema, new SqlTextSpan(0, 10));
        SqlStatement attachStatement = SqlStatement.Create(SqlStatementKind.Attach, attach.Span);
        SqlStatement detachStatement = SqlStatement.Create(SqlStatementKind.Detach, detach.Span);
        SqlAstWalker walker = new SqlAstWalker();
        attachStatement.Attach = attach;
        detachStatement.Detach = detach;

        walker.WalkStatement(attachStatement);
        walker.WalkStatement(detachStatement);

        Assert.That(walker.AttachStatements, Does.Contain(attach));
        Assert.That(walker.DetachStatements, Does.Contain(detach));
        Assert.That(walker.Expressions, Does.Contain(databaseExpression));
        Assert.That(walker.Names, Does.Contain(attachSchema));
        Assert.That(walker.Names, Does.Contain(detachSchema));
    }

    [Test]
    public void Walker_WalksReindexStatementPayload()
    {
        SqlName name = Name("idx_items_name", 8);
        SqlReindexStatement reindex = new SqlReindexStatement(name, new SqlTextSpan(0, 22));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Reindex, reindex.Span);
        statement.Reindex = reindex;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.ReindexStatements, Does.Contain(reindex));
        Assert.That(walker.Names, Does.Contain(name));
    }

    [Test]
    public void Walker_WalksTransactionStatementPayload()
    {
        SqlName savepointName = Name("sp1", 12);
        SqlTransactionStatement transaction = new SqlTransactionStatement(SqlTransactionActionKind.Rollback, SqlTransactionMode.None, false, true, savepointName, new SqlTextSpan(0, 15));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Rollback, transaction.Span);
        statement.Transaction = transaction;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.TransactionStatements, Does.Contain(transaction));
        Assert.That(walker.Names, Does.Contain(savepointName));
    }

    [Test]
    public void Walker_WalksSavepointAndReleaseStatementPayloads()
    {
        SqlName savepointName = Name("sp1", 10);
        SqlName releaseName = Name("sp1", 18);
        SqlSavepointStatement savepoint = new SqlSavepointStatement(savepointName, new SqlTextSpan(0, 13));
        SqlReleaseStatement release = new SqlReleaseStatement(true, releaseName, new SqlTextSpan(0, 21));
        SqlStatement savepointStatement = SqlStatement.Create(SqlStatementKind.Savepoint, savepoint.Span);
        SqlStatement releaseStatement = SqlStatement.Create(SqlStatementKind.Release, release.Span);
        SqlAstWalker walker = new SqlAstWalker();
        savepointStatement.Savepoint = savepoint;
        releaseStatement.Release = release;

        walker.WalkStatement(savepointStatement);
        walker.WalkStatement(releaseStatement);

        Assert.That(walker.SavepointStatements, Does.Contain(savepoint));
        Assert.That(walker.ReleaseStatements, Does.Contain(release));
        Assert.That(walker.Names, Does.Contain(savepointName));
        Assert.That(walker.Names, Does.Contain(releaseName));
    }

    [Test]
    public void Walker_WalksExplainStatementPayload()
    {
        SqlStatement inner = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(8, 8));
        SqlExplainStatement explain = new SqlExplainStatement(false, inner, new SqlTextSpan(0, 16));
        SqlStatement statement = SqlStatement.Create(SqlStatementKind.Explain, explain.Span);
        statement.Explain = explain;
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(walker.ExplainStatements, Does.Contain(explain));
        Assert.That(walker.Statements, Does.Contain(inner));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
