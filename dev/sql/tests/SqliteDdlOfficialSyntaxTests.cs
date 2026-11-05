using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteDdlOfficialSyntaxTests
{
    [Test]
    public void SQLiteDdlSyntax_ParsesCreateTableShape()
    {
        SqlParseResult result = ParseScript("create temporary table if not exists main.items (id integer primary key on conflict replace autoincrement, name text not null on conflict fail unique on conflict ignore, total integer generated always as (id + 1) stored, parent_id integer references parents(id), constraint ck_items check (id > 0), foreign key(parent_id) references parents(id)) strict, without rowid");
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;

        Assert.That(table.IsTemporary, Is.True);
        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name.GetText(), Is.EqualTo("main.items"));
        Assert.That(table.GetColumnCount(), Is.EqualTo(4));
        Assert.That(table.GetColumnAt(0).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(table.GetColumnAt(1).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Fail));
        Assert.That(table.GetColumnAt(2).GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Stored));
        Assert.That(table.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(table.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Check));
        Assert.That(table.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.ForeignKey));
        Assert.That(table.IsStrict, Is.True);
        Assert.That(table.WithoutRowId, Is.True);
    }

    [Test]
    public void SQLiteDdlSyntax_ParsesCreateTableAsAlterAndDropShape()
    {
        SqlParseResult result = ParseScript("create table archived_items as select id, name from items where active = 0; alter table archived_items add column copied_at text default current_timestamp; drop table if exists old_items");

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        SqlAlterTableDefinition alter = result.Script.GetStatementAt(1).AlterTable!;
        SqlDropTableDefinition drop = result.Script.GetStatementAt(2).DropTable!;
        Assert.That(table.HasQuery(), Is.True);
        Assert.That(table.Query!.GetSelectCoreAt(0).HasWhere(), Is.True);
        Assert.That(alter.ActionKind, Is.EqualTo(SqlAlterTableActionKind.AddColumn));
        Assert.That(alter.ColumnDefinition!.Name.GetText(), Is.EqualTo("copied_at"));
        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("old_items"));
    }

    [Test]
    public void SQLiteDdlSyntax_ParsesCreateAndDropIndexShape()
    {
        SqlParseResult result = ParseScript("create unique index if not exists idx_items_name on items (name collate nocase desc, lower(name)) where active = 1; drop index if exists idx_items_name");
        SqlIndexDefinition index = result.Script.GetStatementAt(0).CreateIndex!;
        SqlDropIndexDefinition drop = result.Script.GetStatementAt(1).DropIndex!;

        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.IfNotExists, Is.True);
        Assert.That(index.GetKeyExpressionCount(), Is.EqualTo(2));
        Assert.That(index.GetKeyExpressionAt(0).Collation!.GetText(), Is.EqualTo("nocase"));
        Assert.That(index.GetKeyExpressionAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(index.HasWhereExpression(), Is.True);
        Assert.That(drop.IfExists, Is.True);
    }

    [Test]
    public void SQLiteDdlSyntax_ParsesCreateAndDropViewShape()
    {
        SqlParseResult result = ParseScript("create temp view if not exists v_items(id, name) as select id, name from items; drop view if exists v_items");
        SqlViewDefinition view = result.Script.GetStatementAt(0).CreateView!;
        SqlDropViewDefinition drop = result.Script.GetStatementAt(1).DropView!;

        Assert.That(view.IsTemporary, Is.True);
        Assert.That(view.IfNotExists, Is.True);
        Assert.That(view.GetColumnNameCount(), Is.EqualTo(2));
        Assert.That(view.Query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("v_items"));
    }

    [Test]
    public void SQLiteDdlSyntax_ParsesCreateAndDropTriggerShape()
    {
        SqlParseResult result = ParseScript("create trigger if not exists tr_items after update of name on items for each row when new.name <> old.name begin insert into audit values (new.name); update items set changed = 1 where id = new.id; end; drop trigger if exists tr_items");
        SqlTriggerDefinition trigger = result.Script.GetStatementAt(0).CreateTrigger!;
        SqlDropTriggerDefinition drop = result.Script.GetStatementAt(1).DropTrigger!;

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(2));
        Assert.That(trigger.IfNotExists, Is.True);
        Assert.That(trigger.Timing, Is.EqualTo(SqlTriggerTiming.After));
        Assert.That(trigger.Event, Is.EqualTo(SqlTriggerEvent.Update));
        Assert.That(trigger.GetUpdateColumnAt(0).GetText(), Is.EqualTo("name"));
        Assert.That(trigger.ForEachRow, Is.True);
        Assert.That(trigger.HasWhenExpression(), Is.True);
        Assert.That(trigger.GetBodyStatementCount(), Is.EqualTo(2));
        Assert.That(trigger.GetBodyStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(trigger.GetBodyStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Update));
        Assert.That(drop.IfExists, Is.True);
    }

    [Test]
    public void SQLiteDdlSyntax_ParsesCreateVirtualTableShape()
    {
        SqlParseResult result = ParseScript("create virtual table if not exists docs using fts5(content,tokenize='porter')");
        SqlVirtualTableDefinition table = result.Script.GetStatementAt(0).CreateVirtualTable!;

        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name.GetText(), Is.EqualTo("docs"));
        Assert.That(table.ModuleName.GetText(), Is.EqualTo("fts5"));
        Assert.That(table.GetModuleArgumentAt(0).Text, Is.EqualTo("content"));
        Assert.That(table.GetModuleArgumentAt(2).Text, Is.EqualTo("tokenize"));
        Assert.That(table.GetModuleArgumentAt(4).Text, Is.EqualTo("'porter'"));
    }

    static SqlParseResult ParseScript(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result;
    }
}
