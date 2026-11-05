using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SchemaAstTests
{
    [Test]
    public void TableDefinition_StoresColumnsConstraintsOptionsAndQuery()
    {
        SqlName tableName = Name("items", 0);
        SqlColumnDefinition column = Column("id", 14);
        SqlTableConstraint constraint = TableConstraint();
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(40, 8));
        SqlTextSpan span = new SqlTextSpan(0, 48);
        SqlTableDefinition table = new SqlTableDefinition(true, true, tableName, new SqlColumnDefinition[] { column }, new SqlTableConstraint[] { constraint }, query, true, true, span);

        Assert.That(table.IsTemporary, Is.True);
        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name, Is.SameAs(tableName));
        Assert.That(table.HasColumns(), Is.True);
        Assert.That(table.GetColumnAt(0), Is.SameAs(column));
        Assert.That(table.HasConstraints(), Is.True);
        Assert.That(table.GetConstraintAt(0), Is.SameAs(constraint));
        Assert.That(table.HasQuery(), Is.True);
        Assert.That(table.Query, Is.SameAs(query));
        Assert.That(table.WithoutRowId, Is.True);
        Assert.That(table.IsStrict, Is.True);
        Assert.That(table.Span, Is.SameAs(span));
    }

    [Test]
    public void ColumnDefinition_StoresTypeConstraintsDefaultAndGeneratedExpression()
    {
        SqlName name = Name("total", 0);
        SqlColumnConstraint constraint = ColumnConstraint();
        SqlExpression defaultExpression = Expression(SqlExpressionKind.Literal, 15, 1);
        SqlExpression generatedExpression = Expression(SqlExpressionKind.Binary, 25, 5);
        SqlColumnDefinition column = new SqlColumnDefinition(name, "integer", "INTEGER", new SqlColumnConstraint[] { constraint }, defaultExpression, generatedExpression, SqlGeneratedColumnKind.Stored, new SqlTextSpan(0, 30));

        Assert.That(column.Name, Is.SameAs(name));
        Assert.That(column.TypeName, Is.EqualTo("integer"));
        Assert.That(column.SourceTypeName, Is.EqualTo("INTEGER"));
        Assert.That(column.HasTypeName(), Is.True);
        Assert.That(column.HasConstraints(), Is.True);
        Assert.That(column.GetConstraintAt(0), Is.SameAs(constraint));
        Assert.That(column.HasDefaultExpression(), Is.True);
        Assert.That(column.DefaultExpression, Is.SameAs(defaultExpression));
        Assert.That(column.HasGeneratedExpression(), Is.True);
        Assert.That(column.GeneratedExpression, Is.SameAs(generatedExpression));
        Assert.That(column.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Stored));
    }

    [Test]
    public void AlterTableDefinition_StoresActionNamesAndColumnDefinition()
    {
        SqlName tableName = Name("items", 0);
        SqlName newTableName = Name("archived_items", 20);
        SqlName columnName = Name("old_name", 40);
        SqlName newColumnName = Name("new_name", 50);
        SqlColumnDefinition columnDefinition = Column("created_at", 60);
        SqlTextSpan span = new SqlTextSpan(0, 70);
        SqlAlterTableDefinition alter = new SqlAlterTableDefinition(tableName, SqlAlterTableActionKind.AddColumn, newTableName, columnName, newColumnName, columnDefinition, span);

        Assert.That(alter.TableName, Is.SameAs(tableName));
        Assert.That(alter.ActionKind, Is.EqualTo(SqlAlterTableActionKind.AddColumn));
        Assert.That(alter.HasNewTableName(), Is.True);
        Assert.That(alter.NewTableName, Is.SameAs(newTableName));
        Assert.That(alter.HasColumnName(), Is.True);
        Assert.That(alter.ColumnName, Is.SameAs(columnName));
        Assert.That(alter.HasNewColumnName(), Is.True);
        Assert.That(alter.NewColumnName, Is.SameAs(newColumnName));
        Assert.That(alter.HasColumnDefinition(), Is.True);
        Assert.That(alter.ColumnDefinition, Is.SameAs(columnDefinition));
        Assert.That(alter.Span, Is.SameAs(span));
    }

    [Test]
    public void DropTableDefinition_StoresIfExistsNameAndSpan()
    {
        SqlName name = Name("items", 0);
        SqlTextSpan span = new SqlTextSpan(0, 16);
        SqlDropTableDefinition drop = new SqlDropTableDefinition(true, name, span);

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name, Is.SameAs(name));
        Assert.That(drop.Span, Is.SameAs(span));
    }

    [Test]
    public void DropIndexDefinition_StoresIfExistsNameAndSpan()
    {
        SqlName name = Name("idx_items", 0);
        SqlTextSpan span = new SqlTextSpan(0, 21);
        SqlDropIndexDefinition drop = new SqlDropIndexDefinition(true, name, span);

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name, Is.SameAs(name));
        Assert.That(drop.Span, Is.SameAs(span));
    }

    [Test]
    public void DropViewDefinition_StoresIfExistsNameAndSpan()
    {
        SqlName name = Name("v_items", 0);
        SqlTextSpan span = new SqlTextSpan(0, 20);
        SqlDropViewDefinition drop = new SqlDropViewDefinition(true, name, span);

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name, Is.SameAs(name));
        Assert.That(drop.Span, Is.SameAs(span));
    }

    [Test]
    public void DropTriggerDefinition_StoresIfExistsNameAndSpan()
    {
        SqlName name = Name("tr_items", 0);
        SqlTextSpan span = new SqlTextSpan(0, 23);
        SqlDropTriggerDefinition drop = new SqlDropTriggerDefinition(true, name, span);

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name, Is.SameAs(name));
        Assert.That(drop.Span, Is.SameAs(span));
    }

    [Test]
    public void IndexDefinition_StoresTableKeysAndPartialPredicate()
    {
        SqlName indexName = Name("idx_items_id", 0);
        SqlName tableName = Name("items", 20);
        SqlOrderingTerm key = new SqlOrderingTerm(Expression(SqlExpressionKind.Name, 26, 2), null, SqlOrderingDirection.Asc, SqlNullOrdering.None, new SqlTextSpan(26, 2));
        SqlExpression where = Expression(SqlExpressionKind.Binary, 40, 8);
        SqlIndexDefinition index = new SqlIndexDefinition(true, true, indexName, tableName, new SqlOrderingTerm[] { key }, where, new SqlTextSpan(0, 48));

        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.IfNotExists, Is.True);
        Assert.That(index.Name, Is.SameAs(indexName));
        Assert.That(index.TableName, Is.SameAs(tableName));
        Assert.That(index.GetKeyExpressionCount(), Is.EqualTo(1));
        Assert.That(index.GetKeyExpressionAt(0), Is.SameAs(key));
        Assert.That(index.HasWhereExpression(), Is.True);
        Assert.That(index.WhereExpression, Is.SameAs(where));
    }

    [Test]
    public void ViewDefinition_StoresNameColumnsAndQuery()
    {
        SqlName viewName = Name("v_items", 0);
        SqlName columnName = Name("id", 10);
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(20, 8));
        SqlViewDefinition view = new SqlViewDefinition(true, true, viewName, new SqlName[] { columnName }, query, new SqlTextSpan(0, 28));

        Assert.That(view.IsTemporary, Is.True);
        Assert.That(view.IfNotExists, Is.True);
        Assert.That(view.Name, Is.SameAs(viewName));
        Assert.That(view.HasColumnNames(), Is.True);
        Assert.That(view.GetColumnNameAt(0), Is.SameAs(columnName));
        Assert.That(view.Query, Is.SameAs(query));
    }

    [Test]
    public void VirtualTableDefinition_StoresModuleAndRawArguments()
    {
        SqlName tableName = Name("docs", 0);
        SqlName moduleName = Name("fts5", 12);
        SqlToken argument = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(17, 7), "content");
        SqlVirtualTableDefinition table = new SqlVirtualTableDefinition(true, tableName, moduleName, new SqlToken[] { argument }, new SqlTextSpan(0, 24));

        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name, Is.SameAs(tableName));
        Assert.That(table.ModuleName, Is.SameAs(moduleName));
        Assert.That(table.HasModuleArguments(), Is.True);
        Assert.That(table.GetModuleArgumentCount(), Is.EqualTo(1));
        Assert.That(table.GetModuleArgumentAt(0), Is.SameAs(argument));
    }

    [Test]
    public void TriggerDefinition_StoresTriggerShapeTargetWhenAndBody()
    {
        SqlName name = Name("tr_items_ai", 0);
        SqlName updateColumn = Name("total", 15);
        SqlName targetName = Name("items", 25);
        SqlExpression when = Expression(SqlExpressionKind.Binary, 35, 8);
        SqlStatement body = SqlStatement.Create(SqlStatementKind.Insert, new SqlTextSpan(50, 10));
        SqlTextSpan span = new SqlTextSpan(0, 60);
        SqlTriggerDefinition trigger = new SqlTriggerDefinition(true, true, name, SqlTriggerTiming.After, SqlTriggerEvent.Update, new SqlName[] { updateColumn }, targetName, true, when, new SqlStatement[] { body }, span);

        Assert.That(trigger.IsTemporary, Is.True);
        Assert.That(trigger.IfNotExists, Is.True);
        Assert.That(trigger.Name, Is.SameAs(name));
        Assert.That(trigger.Timing, Is.EqualTo(SqlTriggerTiming.After));
        Assert.That(trigger.Event, Is.EqualTo(SqlTriggerEvent.Update));
        Assert.That(trigger.HasUpdateColumns(), Is.True);
        Assert.That(trigger.GetUpdateColumnAt(0), Is.SameAs(updateColumn));
        Assert.That(trigger.TargetName, Is.SameAs(targetName));
        Assert.That(trigger.ForEachRow, Is.True);
        Assert.That(trigger.HasWhenExpression(), Is.True);
        Assert.That(trigger.WhenExpression, Is.SameAs(when));
        Assert.That(trigger.HasBody(), Is.True);
        Assert.That(trigger.GetBodyStatementAt(0), Is.SameAs(body));
        Assert.That(trigger.Span, Is.SameAs(span));
    }

    [Test]
    public void SchemaAst_EnumValuesAreStable()
    {
        Assert.That((int)SqlGeneratedColumnKind.None, Is.EqualTo(0));
        Assert.That((int)SqlGeneratedColumnKind.Virtual, Is.EqualTo(1));
        Assert.That((int)SqlGeneratedColumnKind.Stored, Is.EqualTo(2));
        Assert.That((int)SqlAlterTableActionKind.Unknown, Is.EqualTo(0));
        Assert.That((int)SqlAlterTableActionKind.RenameTable, Is.EqualTo(1));
        Assert.That((int)SqlAlterTableActionKind.DropColumn, Is.EqualTo(4));
        Assert.That((int)SqlTriggerTiming.Before, Is.EqualTo(1));
        Assert.That((int)SqlTriggerTiming.InsteadOf, Is.EqualTo(3));
        Assert.That((int)SqlTriggerEvent.Insert, Is.EqualTo(1));
        Assert.That((int)SqlTriggerEvent.Delete, Is.EqualTo(3));
    }

    [Test]
    public void SchemaAst_RejectsNullInputs()
    {
        SqlName name = Name("x", 0);
        SqlColumnDefinition column = Column("c", 2);
        SqlQuery query = SqlQuery.Create(new SqlSelectCore[0], new SqlTextSpan(0, 0));
        SqlOrderingTerm key = new SqlOrderingTerm(Expression(SqlExpressionKind.Name, 0, 1), null, SqlOrderingDirection.None, SqlNullOrdering.None, new SqlTextSpan(0, 1));
        SqlStatement body = SqlStatement.Create(SqlStatementKind.Select, new SqlTextSpan(0, 0));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, null!, new SqlColumnDefinition[0], new SqlTableConstraint[0], null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, null!, new SqlTableConstraint[0], null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, new SqlColumnDefinition[] { null! }, new SqlTableConstraint[0], null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTableDefinition(false, false, name, new SqlColumnDefinition[] { column }, null!, null, false, false, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlAlterTableDefinition(null!, SqlAlterTableActionKind.Unknown, null, null, null, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlAlterTableDefinition(name, SqlAlterTableActionKind.Unknown, null, null, null, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDropTableDefinition(false, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlDropTableDefinition(false, name, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDropIndexDefinition(false, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlDropIndexDefinition(false, name, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDropViewDefinition(false, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlDropViewDefinition(false, name, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDropTriggerDefinition(false, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlDropTriggerDefinition(false, name, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(null!, "", "", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(name, null!, "", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(name, "", null!, new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlColumnDefinition(name, "", "", null!, null, null, SqlGeneratedColumnKind.None, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlIndexDefinition(false, false, null!, name, new SqlOrderingTerm[] { key }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlIndexDefinition(false, false, name, null!, new SqlOrderingTerm[] { key }, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlIndexDefinition(false, false, name, name, null!, null, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlViewDefinition(false, false, null!, new SqlName[0], query, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlViewDefinition(false, false, name, null!, query, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlViewDefinition(false, false, name, new SqlName[0], null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableDefinition(false, null!, name, new SqlToken[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableDefinition(false, name, null!, new SqlToken[0], new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlVirtualTableDefinition(false, name, name, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, null!, SqlTriggerTiming.Unknown, SqlTriggerEvent.Unknown, new SqlName[0], name, false, null, new SqlStatement[] { body }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Unknown, SqlTriggerEvent.Unknown, null!, name, false, null, new SqlStatement[] { body }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Unknown, SqlTriggerEvent.Unknown, new SqlName[0], null!, false, null, new SqlStatement[] { body }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlTriggerDefinition(false, false, name, SqlTriggerTiming.Unknown, SqlTriggerEvent.Unknown, new SqlName[0], name, false, null, null!, new SqlTextSpan(0, 0)));
    }

    static SqlColumnDefinition Column(string text, int start)
    {
        SqlName name = Name(text, start);
        return new SqlColumnDefinition(name, "", "", new SqlColumnConstraint[0], null, null, SqlGeneratedColumnKind.None, name.Span);
    }

    static SqlColumnConstraint ColumnConstraint()
    {
        return new SqlColumnConstraint(SqlConstraintKind.NotNull, null, null, null, SqlConflictActionKind.None, null, SqlGeneratedColumnKind.None, false, new SqlTextSpan(0, 0));
    }

    static SqlTableConstraint TableConstraint()
    {
        return new SqlTableConstraint(SqlConstraintKind.PrimaryKey, null, new SqlName[0], new SqlExpression[0], SqlConflictActionKind.None, null, new SqlTextSpan(0, 0));
    }

    static SqlExpression Expression(SqlExpressionKind kind, int start, int length)
    {
        return SqlExpression.Create(kind, new SqlTextSpan(start, length));
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
