using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteSyntaxDiagramStatementTests
{
    [TestCase("alter-table rename table", "alter table items rename to archived_items", SqlStatementKind.AlterTable)]
    [TestCase("alter-table rename column", "alter table items rename column old_name to new_name", SqlStatementKind.AlterTable)]
    [TestCase("alter-table rename column shorthand", "alter table items rename old_name to new_name", SqlStatementKind.AlterTable)]
    [TestCase("alter-table add column", "alter table items add column note text default 'x'", SqlStatementKind.AlterTable)]
    [TestCase("alter-table add column shorthand", "alter table items add note text", SqlStatementKind.AlterTable)]
    [TestCase("alter-table drop column", "alter table items drop column obsolete", SqlStatementKind.AlterTable)]
    [TestCase("alter-table drop column shorthand", "alter table items drop obsolete", SqlStatementKind.AlterTable)]
    [TestCase("analyze", "analyze", SqlStatementKind.Analyze)]
    [TestCase("analyze schema", "analyze main", SqlStatementKind.Analyze)]
    [TestCase("analyze qualified target", "analyze main.items", SqlStatementKind.Analyze)]
    [TestCase("attach", "attach 'archive.db' as archive", SqlStatementKind.Attach)]
    [TestCase("attach database", "attach database 'archive.db' as archive", SqlStatementKind.Attach)]
    [TestCase("begin", "begin", SqlStatementKind.Begin)]
    [TestCase("begin deferred transaction", "begin deferred transaction", SqlStatementKind.Begin)]
    [TestCase("begin immediate transaction", "begin immediate transaction", SqlStatementKind.Begin)]
    [TestCase("begin exclusive transaction", "begin exclusive transaction", SqlStatementKind.Begin)]
    [TestCase("commit", "commit", SqlStatementKind.Commit)]
    [TestCase("commit transaction", "commit transaction", SqlStatementKind.Commit)]
    [TestCase("end transaction", "end transaction", SqlStatementKind.End)]
    [TestCase("create index", "create index idx_items_name on items(name)", SqlStatementKind.CreateIndex)]
    [TestCase("create unique partial expression index", "create unique index if not exists idx_items_name on items(lower(name) collate nocase desc) where active = 1", SqlStatementKind.CreateIndex)]
    [TestCase("create table", "create table items(id integer primary key, name text)", SqlStatementKind.CreateTable)]
    [TestCase("create temporary table", "create temporary table if not exists temp.items(id)", SqlStatementKind.CreateTable)]
    [TestCase("create table as select", "create table archived as select * from items", SqlStatementKind.CreateTable)]
    [TestCase("create strict table without rowid", "create table items(id integer primary key) strict, without rowid", SqlStatementKind.CreateTable)]
    [TestCase("create trigger before insert", "create trigger before_items before insert on items begin select 1; end", SqlStatementKind.CreateTrigger)]
    [TestCase("create trigger after update of", "create trigger after_items after update of name, value on items for each row when new.value <> old.value begin update items set value = new.value; end", SqlStatementKind.CreateTrigger)]
    [TestCase("create temporary instead-of trigger", "create temp trigger view_delete instead of delete on item_view begin delete from items where id = old.id; end", SqlStatementKind.CreateTrigger)]
    [TestCase("create view", "create view item_view(id, name) as select id, name from items", SqlStatementKind.CreateView)]
    [TestCase("create temporary view", "create temp view if not exists temp_view as values (1)", SqlStatementKind.CreateView)]
    [TestCase("create virtual table", "create virtual table if not exists docs using fts5(title, body, tokenize='porter')", SqlStatementKind.CreateVirtualTable)]
    [TestCase("delete", "delete from items", SqlStatementKind.Delete)]
    [TestCase("delete qualified table", "delete from main.items as target indexed by idx_items where target.id = 1", SqlStatementKind.Delete)]
    [TestCase("delete returning", "delete from items where active = 0 returning id, name as removed_name", SqlStatementKind.Delete)]
    [TestCase("delete limited", "delete from items where active = 0 order by id desc limit 5 offset 2", SqlStatementKind.Delete)]
    [TestCase("detach", "detach archive", SqlStatementKind.Detach)]
    [TestCase("detach database", "detach database archive", SqlStatementKind.Detach)]
    [TestCase("drop index", "drop index if exists main.idx_items", SqlStatementKind.DropIndex)]
    [TestCase("drop table", "drop table if exists main.items", SqlStatementKind.DropTable)]
    [TestCase("drop trigger", "drop trigger if exists main.item_trigger", SqlStatementKind.DropTrigger)]
    [TestCase("drop view", "drop view if exists main.item_view", SqlStatementKind.DropView)]
    [TestCase("insert default values", "insert into items default values", SqlStatementKind.Insert)]
    [TestCase("insert values", "insert or rollback into items(id, name) values (1, 'one'), (2, 'two')", SqlStatementKind.Insert)]
    [TestCase("insert select", "insert or abort into items select * from staged_items", SqlStatementKind.Insert)]
    [TestCase("replace", "replace into items(id) values (1)", SqlStatementKind.Insert)]
    [TestCase("upsert do nothing", "insert into items values (1) on conflict do nothing", SqlStatementKind.Insert)]
    [TestCase("upsert do update", "insert into items values (1, 'x') on conflict(id collate binary asc) where id > 0 do update set name = excluded.name where items.name <> excluded.name", SqlStatementKind.Insert)]
    [TestCase("insert returning", "insert into items values (1) returning *, id as inserted_id", SqlStatementKind.Insert)]
    [TestCase("pragma", "pragma foreign_keys", SqlStatementKind.Pragma)]
    [TestCase("pragma qualified equals", "pragma main.cache_size = -2000", SqlStatementKind.Pragma)]
    [TestCase("pragma call", "pragma table_info(items)", SqlStatementKind.Pragma)]
    [TestCase("reindex", "reindex", SqlStatementKind.Reindex)]
    [TestCase("reindex qualified target", "reindex main.idx_items", SqlStatementKind.Reindex)]
    [TestCase("release", "release point_name", SqlStatementKind.Release)]
    [TestCase("release savepoint", "release savepoint point_name", SqlStatementKind.Release)]
    [TestCase("rollback", "rollback", SqlStatementKind.Rollback)]
    [TestCase("rollback transaction", "rollback transaction", SqlStatementKind.Rollback)]
    [TestCase("rollback to", "rollback to point_name", SqlStatementKind.Rollback)]
    [TestCase("rollback to savepoint", "rollback transaction to savepoint point_name", SqlStatementKind.Rollback)]
    [TestCase("savepoint", "savepoint point_name", SqlStatementKind.Savepoint)]
    [TestCase("select", "select all id from items", SqlStatementKind.Select)]
    [TestCase("values", "values (1, 'one'), (2, 'two')", SqlStatementKind.Select)]
    [TestCase("update", "update items set name = 'new' where id = 1", SqlStatementKind.Update)]
    [TestCase("update conflict action", "update or ignore items set name = 'new'", SqlStatementKind.Update)]
    [TestCase("update column name list", "update items set (name, value) = ('new', 2)", SqlStatementKind.Update)]
    [TestCase("update from returning", "update items as target not indexed set value = source.value from source where source.id = target.id returning target.id", SqlStatementKind.Update)]
    [TestCase("update limited", "update items set value = 1 order by id asc limit 5 offset 2", SqlStatementKind.Update)]
    [TestCase("vacuum", "vacuum", SqlStatementKind.Vacuum)]
    [TestCase("vacuum schema", "vacuum main", SqlStatementKind.Vacuum)]
    [TestCase("vacuum into", "vacuum main into 'backup.db'", SqlStatementKind.Vacuum)]
    [TestCase("explain", "explain select 1", SqlStatementKind.Explain)]
    [TestCase("explain query plan", "explain query plan select * from items", SqlStatementKind.Explain)]
    public void OfficialStatementDiagramForm_ParsesWithoutDiagnostics(string diagramForm, string text, SqlStatementKind expectedKind)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True, diagramForm + DiagnosticText(result));
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1), diagramForm);
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(expectedKind), diagramForm);
        Assert.That(result.Script.GetStatementAt(0).Span.Start, Is.EqualTo(0), diagramForm);
        Assert.That(result.Script.GetStatementAt(0).Span.GetEnd(), Is.EqualTo(text.Length), diagramForm);
    }

    [Test]
    public void SqlStatementListDiagram_PreservesSeparatorsAndEmptyStatements()
    {
        const string text = ";select 1;;pragma foreign_keys;";

        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True, DiagnosticText(result));
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(4));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Empty));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Empty));
        Assert.That(result.Script.GetStatementAt(3).Kind, Is.EqualTo(SqlStatementKind.Pragma));
        Assert.That(result.Script.GetStatementAt(3).Terminator, Is.Not.Null);
    }

    static string DiagnosticText(SqlParseResult result)
    {
        if (result.Diagnostics.GetCount() == 0)
        {
            return "";
        }

        SqlDiagnostic diagnostic = result.Diagnostics.GetAt(0);
        return ": " + diagnostic.Code + " " + diagnostic.Message;
    }
}
