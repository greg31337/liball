using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorDdlTests
{
    [Test]
    public void SqlGenerator_GeneratesCreateTableWithConstraintsAndOptions()
    {
        string text = "create temp table if not exists main.items (id integer constraint pk_items primary key on conflict replace autoincrement, name text not null on conflict fail unique on conflict ignore, qty integer check (qty > 0) default 1, parent_id integer references parents(id), constraint ck_items check (id > 0), foreign key(parent_id) references parents(id)) strict, without rowid";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("CREATE TEMP TABLE IF NOT EXISTS main.items (id integer CONSTRAINT pk_items PRIMARY KEY ON CONFLICT REPLACE AUTOINCREMENT, name text NOT NULL ON CONFLICT FAIL UNIQUE ON CONFLICT IGNORE, qty integer CHECK (qty > 0) DEFAULT 1, parent_id integer REFERENCES parents(id), CONSTRAINT ck_items CHECK (id > 0), FOREIGN KEY (parent_id) REFERENCES parents(id)) STRICT, WITHOUT ROWID"));
    }

    [Test]
    public void SqlGenerator_GeneratesCreateTableAsSelect()
    {
        string generated = GenerateStatement("create table archived_items as select id, name from items where active = 0");

        Assert.That(generated, Is.EqualTo("CREATE TABLE archived_items AS SELECT id, name FROM items WHERE active = 0"));
    }

    [Test]
    public void SqlGenerator_GeneratesCreateIndexViewAndVirtualTable()
    {
        string index = GenerateStatement("create unique index if not exists idx_items_name on items (name collate nocase desc, lower(title)) where active = 1");
        string view = GenerateStatement("create temp view if not exists main.v_items (item_id, item_name) as select id, name from items");
        string virtualTable = GenerateStatement("create virtual table if not exists docs using fts5(content,tokenize='porter')");

        Assert.That(index, Is.EqualTo("CREATE UNIQUE INDEX IF NOT EXISTS idx_items_name ON items (name COLLATE nocase DESC, lower(title)) WHERE active = 1"));
        Assert.That(view, Is.EqualTo("CREATE TEMP VIEW IF NOT EXISTS main.v_items (item_id, item_name) AS SELECT id, name FROM items"));
        Assert.That(virtualTable, Is.EqualTo("CREATE VIRTUAL TABLE IF NOT EXISTS docs USING fts5(content,tokenize='porter')"));
    }

    [Test]
    public void SqlGenerator_GeneratesAlterTableActions()
    {
        string renameTable = GenerateStatement("alter table main.items rename to archived_items");
        string renameColumn = GenerateStatement("alter table items rename column old_name to new_name");
        string addColumn = GenerateStatement("alter table items add column created_at text not null default current_timestamp");
        string dropColumn = GenerateStatement("alter table items drop column old_name");

        Assert.That(renameTable, Is.EqualTo("ALTER TABLE main.items RENAME TO archived_items"));
        Assert.That(renameColumn, Is.EqualTo("ALTER TABLE items RENAME COLUMN old_name TO new_name"));
        Assert.That(addColumn, Is.EqualTo("ALTER TABLE items ADD COLUMN created_at text NOT NULL DEFAULT CURRENT_TIMESTAMP"));
        Assert.That(dropColumn, Is.EqualTo("ALTER TABLE items DROP COLUMN old_name"));
    }

    [Test]
    public void SqlGenerator_GeneratesDropStatements()
    {
        string table = GenerateStatement("drop table if exists items");
        string index = GenerateStatement("drop index if exists idx_items_name");
        string view = GenerateStatement("drop view if exists v_items");
        string trigger = GenerateStatement("drop trigger if exists tr_items");

        Assert.That(table, Is.EqualTo("DROP TABLE IF EXISTS items"));
        Assert.That(index, Is.EqualTo("DROP INDEX IF EXISTS idx_items_name"));
        Assert.That(view, Is.EqualTo("DROP VIEW IF EXISTS v_items"));
        Assert.That(trigger, Is.EqualTo("DROP TRIGGER IF EXISTS tr_items"));
    }

    [Test]
    public void SqlGenerator_GeneratesCreateTrigger()
    {
        string text = "create temp trigger if not exists tr_items after update of name, price on items for each row when new.price > old.price begin select 1; update audit set value = 1; insert into audit values (1); delete from audit where value = 0; end;";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("CREATE TEMP TRIGGER IF NOT EXISTS tr_items AFTER UPDATE OF name, price ON items FOR EACH ROW WHEN new.price > old.price BEGIN SELECT 1; UPDATE audit SET value = 1; INSERT INTO audit VALUES (1); DELETE FROM audit WHERE value = 0; END"));
    }

    [Test]
    public void SqlGenerator_DdlApisRejectInvalidInputs()
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        SqlStatement createTable = SqlStatement.Create(SqlStatementKind.CreateTable, new SqlTextSpan(0, 12));

        Assert.Throws<ArgumentNullException>(() => generator.WriteCreateTable(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteAlterTable(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDropTable(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteCreateIndex(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDropIndex(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteCreateView(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDropView(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteCreateVirtualTable(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteCreateTrigger(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDropTrigger(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteStatement(createTable));
    }

    static string GenerateStatement(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return SqlGenerator.GenerateStatement(result.Script.GetStatementAt(0));
    }
}
