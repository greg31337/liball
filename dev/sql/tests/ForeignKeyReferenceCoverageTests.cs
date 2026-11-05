using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ForeignKeyReferenceCoverageTests
{
    [Test]
    public void CreateTableParser_ParsesColumnForeignKeyReferenceClauses()
    {
        SqlTableDefinition table = ParseTable("create table child (parent_id integer references parents(id) on delete cascade on update set null match simple deferrable initially deferred not null)");
        SqlColumnDefinition parentId = table.GetColumnAt(0);
        SqlColumnConstraint referencesConstraint = parentId.GetConstraintAt(0);
        SqlForeignKeyReference reference = referencesConstraint.References!;

        Assert.That(parentId.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(referencesConstraint.Kind, Is.EqualTo(SqlConstraintKind.References));
        Assert.That(reference.TableName.GetText(), Is.EqualTo("parents"));
        Assert.That(reference.GetColumnNameAt(0).GetText(), Is.EqualTo("id"));
        Assert.That(reference.OnDeleteAction, Is.EqualTo("CASCADE"));
        Assert.That(reference.OnUpdateAction, Is.EqualTo("SET NULL"));
        Assert.That(reference.MatchName!.GetText(), Is.EqualTo("simple"));
        Assert.That(reference.IsDeferrable, Is.True);
        Assert.That(reference.IsInitiallyDeferred, Is.True);
        Assert.That(parentId.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.NotNull));
    }

    [Test]
    public void CreateTableParser_ParsesTableForeignKeyReferenceClauses()
    {
        SqlTableDefinition table = ParseTable("create table child (parent_id integer, backup_parent_id integer, foreign key(parent_id) references parents(id) on delete restrict on update no action, foreign key(backup_parent_id) references parents(id) on delete set default not deferrable initially immediate)");
        SqlTableConstraint parentKey = table.GetConstraintAt(0);
        SqlTableConstraint backupKey = table.GetConstraintAt(1);
        SqlForeignKeyReference parentReference = parentKey.References!;
        SqlForeignKeyReference backupReference = backupKey.References!;

        Assert.That(parentKey.GetColumnAt(0).GetText(), Is.EqualTo("parent_id"));
        Assert.That(parentReference.OnDeleteAction, Is.EqualTo("RESTRICT"));
        Assert.That(parentReference.OnUpdateAction, Is.EqualTo("NO ACTION"));
        Assert.That(parentReference.IsDeferrable, Is.False);
        Assert.That(parentReference.IsInitiallyDeferred, Is.False);
        Assert.That(backupKey.GetColumnAt(0).GetText(), Is.EqualTo("backup_parent_id"));
        Assert.That(backupReference.OnDeleteAction, Is.EqualTo("SET DEFAULT"));
        Assert.That(backupReference.HasOnUpdateAction(), Is.False);
        Assert.That(backupReference.IsDeferrable, Is.False);
        Assert.That(backupReference.IsInitiallyDeferred, Is.False);
    }

    [Test]
    public void SqlGenerator_GeneratesForeignKeyReferenceClauses()
    {
        const string text = "create table child (parent_id integer references parents(id) on delete cascade on update set null match simple deferrable initially deferred, foreign key(parent_id) references parents(id) on delete restrict on update no action)";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("CREATE TABLE child (parent_id integer REFERENCES parents(id) ON DELETE CASCADE ON UPDATE SET NULL MATCH simple DEFERRABLE INITIALLY DEFERRED, FOREIGN KEY (parent_id) REFERENCES parents(id) ON DELETE RESTRICT ON UPDATE NO ACTION)"));
    }

    static SqlTableDefinition ParseTable(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateTable!;
    }

    static string GenerateStatement(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return SqlGenerator.GenerateStatement(result.Script.GetStatementAt(0));
    }
}
