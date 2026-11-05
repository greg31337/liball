using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstSearchTests
{
    [Test]
    public void Search_FindsTablesFromQueryTableReferences()
    {
        SqlName leftName = Name("users", 0);
        SqlName rightName = Name("orders", 10);
        SqlTableReference right = SqlTableReference.Create(SqlTableReferenceKind.Named, rightName.Span, rightName, null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlJoin join = new SqlJoin(SqlJoinKind.Inner, false, false, right, SqlJoinConstraintKind.None, null, new SqlName[0], new SqlTextSpan(8, 8));
        SqlTableReference left = SqlTableReference.Create(SqlTableReferenceKind.Named, new SqlTextSpan(0, 18), leftName, null, null, null, new SqlJoin[] { join }, null, new SqlName[0]);
        SqlSelectCore core = SqlSelectCore.Create(new SqlResultColumn[0], new SqlTextSpan(0, 18));
        core.From = new SqlTableReference[] { left };
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[] { core }, new SqlTextSpan(0, 18));

        SqlName[] tables = SqlAstSearch.FindTables(query);

        Assert.That(tables, Has.Length.EqualTo(2));
        Assert.That(tables[0], Is.SameAs(leftName));
        Assert.That(tables[1], Is.SameAs(rightName));
    }

    [Test]
    public void Search_FindsColumnsFromExpressionsAndSchemaNodes()
    {
        SqlName expressionName = Name("amount", 0);
        SqlExpression expression = SqlExpression.Create(SqlExpressionKind.Name, new SqlTextSpan(0, 6));
        expression.Name = expressionName;
        SqlName columnName = Name("id", 10);
        SqlColumnDefinition column = new SqlColumnDefinition(columnName, "integer", "INTEGER", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(10, 9));
        SqlName keyName = Name("parent_id", 25);
        SqlTableConstraint tableConstraint = new SqlTableConstraint(SqlConstraintKind.ForeignKey, null, new SqlName[] { keyName }, new SqlExpression[0], SqlConflictActionKind.None, null, new SqlTextSpan(20, 20));
        SqlTableDefinition table = new SqlTableDefinition(false, false, Name("items", 40), new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { tableConstraint }, null, false, false, new SqlTextSpan(10, 50));
        SqlResultColumn resultColumn = new SqlResultColumn(expression, false, null, null, expression.Span, new SqlToken[0]);
        SqlSelectCore core = SqlSelectCore.Create(new SqlResultColumn[] { resultColumn }, new SqlTextSpan(0, 6));
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[] { core }, new SqlTextSpan(0, 6));
        SqlStatement select = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 6));
        select.Query = query;
        SqlStatement create = SqlStatement.Create(SqlStatementKind.CreateTable, new SqlTextSpan(10, 50));
        create.CreateTable = table;
        SqlScript script = SqlScript.Create(new SqlStatement[] { select, create }, new SqlTextSpan(0, 60));

        SqlName[] columns = SqlAstSearch.FindColumns(script);

        Assert.That(columns, Has.Length.EqualTo(3));
        Assert.That(columns[0], Is.SameAs(expressionName));
        Assert.That(columns[1], Is.SameAs(columnName));
        Assert.That(columns[2], Is.SameAs(keyName));
    }

    [Test]
    public void Search_FindsParameterAndFunctionExpressions()
    {
        SqlExpression parameter = SqlExpression.Create(SqlExpressionKind.Parameter, new SqlTextSpan(6, 1));
        SqlExpression function = SqlExpression.Create(SqlExpressionKind.FunctionCall, new SqlTextSpan(0, 8), new SqlToken[0], new SqlExpression[] { parameter });
        SqlExpression wrapper = SqlExpression.Create(SqlExpressionKind.Parenthesized, new SqlTextSpan(0, 10));
        wrapper.Operand = function;

        SqlExpression[] parameters = SqlAstSearch.FindParameters(wrapper);
        SqlExpression[] functions = SqlAstSearch.FindFunctions(wrapper);

        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0], Is.SameAs(parameter));
        Assert.That(functions, Has.Length.EqualTo(1));
        Assert.That(functions[0], Is.SameAs(function));
    }

    [Test]
    public void Search_FindsSchemaTableTargets()
    {
        SqlName tableName = Name("items", 0);
        SqlName renamedTableName = Name("archived_items", 8);
        SqlName droppedTableName = Name("old_items", 22);
        SqlName indexTableName = Name("items", 15);
        SqlName virtualTableName = Name("docs", 25);
        SqlName triggerTarget = Name("items", 35);
        SqlTableDefinition table = new SqlTableDefinition(false, false, tableName, new SqlColumnDefinition[0], new SqlTableConstraint[0], null, false, false, new SqlTextSpan(0, 5));
        SqlAlterTableDefinition alter = new SqlAlterTableDefinition(tableName, SqlAlterTableActionKind.RenameTable, renamedTableName, null, null, null, new SqlTextSpan(0, 20));
        SqlDropTableDefinition drop = new SqlDropTableDefinition(false, droppedTableName, new SqlTextSpan(22, 9));
        SqlIndexDefinition index = new SqlIndexDefinition(false, false, Name("idx_items", 6), indexTableName, new SqlOrderingTerm[0], null, new SqlTextSpan(6, 14));
        SqlVirtualTableDefinition virtualTable = new SqlVirtualTableDefinition(false, virtualTableName, Name("fts5", 30), new SqlToken[0], new SqlTextSpan(25, 9));
        SqlTriggerDefinition trigger = new SqlTriggerDefinition(false, false, Name("tr_items", 40), SqlTriggerTiming.After, SqlTriggerEvent.Insert, new SqlName[0], triggerTarget, true, null, new SqlStatement[0], new SqlTextSpan(35, 15));
        SqlStatement tableStatement = SqlStatement.Create(SqlStatementKind.CreateTable, new SqlTextSpan(0, 0));
        SqlStatement alterStatement = SqlStatement.Create(SqlStatementKind.AlterTable, new SqlTextSpan(0, 0));
        SqlStatement dropStatement = SqlStatement.Create(SqlStatementKind.DropTable, new SqlTextSpan(0, 0));
        SqlStatement indexStatement = SqlStatement.Create(SqlStatementKind.CreateIndex, new SqlTextSpan(0, 0));
        SqlStatement virtualStatement = SqlStatement.Create(SqlStatementKind.CreateVirtualTable, new SqlTextSpan(0, 0));
        SqlStatement triggerStatement = SqlStatement.Create(SqlStatementKind.CreateTrigger, new SqlTextSpan(0, 0));
        tableStatement.CreateTable = table;
        alterStatement.AlterTable = alter;
        dropStatement.DropTable = drop;
        indexStatement.CreateIndex = index;
        virtualStatement.CreateVirtualTable = virtualTable;
        triggerStatement.CreateTrigger = trigger;
        SqlScript script = SqlScript.Create(new SqlStatement[] { tableStatement, alterStatement, dropStatement, indexStatement, virtualStatement, triggerStatement }, new SqlTextSpan(0, 60));

        SqlName[] tables = SqlAstSearch.FindTables(script);

        Assert.That(tables, Has.Length.EqualTo(7));
        Assert.That(tables[0], Is.SameAs(tableName));
        Assert.That(tables[1], Is.SameAs(tableName));
        Assert.That(tables[2], Is.SameAs(renamedTableName));
        Assert.That(tables[3], Is.SameAs(droppedTableName));
        Assert.That(tables[4], Is.SameAs(indexTableName));
        Assert.That(tables[5], Is.SameAs(virtualTableName));
        Assert.That(tables[6], Is.SameAs(triggerTarget));
    }

    [Test]
    public void Search_NullInputsReturnEmptyArrays()
    {
        Assert.That(SqlAstSearch.FindTables((SqlScript?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAstSearch.FindColumns((SqlScript?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAstSearch.FindParameters((SqlExpression?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAstSearch.FindFunctions((SqlExpression?)null), Has.Length.EqualTo(0));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
