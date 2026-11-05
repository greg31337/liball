using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AlterTableParserTests
{
    [Test]
    public void StatementParser_AttachesAlterTableRenameTableSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("alter table main.items rename to archived_items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.AlterTable));
        Assert.That(statement.AlterTable, Is.Not.Null);
        SqlAlterTableDefinition alter = statement.AlterTable!;
        Assert.That(alter.ActionKind, Is.EqualTo(SqlAlterTableActionKind.RenameTable));
        Assert.That(alter.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(alter.NewTableName!.GetText(), Is.EqualTo("archived_items"));
        Assert.That(alter.HasNewTableName(), Is.True);
    }

    [Test]
    public void AlterTableParser_ParsesRenameColumnForms()
    {
        SqlAlterTableDefinition explicitColumn = ParseAlter("alter table items rename column old_name to new_name");
        SqlAlterTableDefinition implicitColumn = ParseAlter("alter table items rename old_name to new_name");

        Assert.That(explicitColumn.ActionKind, Is.EqualTo(SqlAlterTableActionKind.RenameColumn));
        Assert.That(explicitColumn.ColumnName!.GetText(), Is.EqualTo("old_name"));
        Assert.That(explicitColumn.NewColumnName!.GetText(), Is.EqualTo("new_name"));
        Assert.That(explicitColumn.HasColumnName(), Is.True);
        Assert.That(explicitColumn.HasNewColumnName(), Is.True);
        Assert.That(implicitColumn.ActionKind, Is.EqualTo(SqlAlterTableActionKind.RenameColumn));
        Assert.That(implicitColumn.ColumnName!.GetText(), Is.EqualTo("old_name"));
        Assert.That(implicitColumn.NewColumnName!.GetText(), Is.EqualTo("new_name"));
    }

    [Test]
    public void AlterTableParser_ParsesAddAndDropColumn()
    {
        SqlAlterTableDefinition add = ParseAlter("alter table items add column created_at text not null default current_timestamp");
        SqlAlterTableDefinition drop = ParseAlter("alter table items drop column old_name");

        Assert.That(add.ActionKind, Is.EqualTo(SqlAlterTableActionKind.AddColumn));
        Assert.That(add.HasColumnDefinition(), Is.True);
        Assert.That(add.ColumnDefinition!.Name.GetText(), Is.EqualTo("created_at"));
        Assert.That(add.ColumnDefinition.TypeName, Is.EqualTo("text"));
        Assert.That(add.ColumnDefinition.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(add.ColumnDefinition.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.NotNull));
        Assert.That(add.ColumnDefinition.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.Default));
        Assert.That(drop.ActionKind, Is.EqualTo(SqlAlterTableActionKind.DropColumn));
        Assert.That(drop.ColumnName!.GetText(), Is.EqualTo("old_name"));
    }

    [Test]
    public void AlterTableParser_ReportsMissingActionAndRenameTo()
    {
        SqlParseResult missingAction = SqlStatementParser.ParseScript("alter table items", SqlParseOptions.CreateSqlite());
        SqlParseResult missingTo = SqlStatementParser.ParseScript("alter table items rename column old_name new_name", SqlParseOptions.CreateSqlite());

        Assert.That(missingAction.IsSuccess(), Is.False);
        Assert.That(missingAction.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLALTER004"));
        Assert.That(missingTo.IsSuccess(), Is.False);
        Assert.That(missingTo.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLALTER007"));
    }

    [Test]
    public void AlterTableParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlAlterTableParser(null!));
    }

    static SqlAlterTableDefinition ParseAlter(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).AlterTable!;
    }
}
