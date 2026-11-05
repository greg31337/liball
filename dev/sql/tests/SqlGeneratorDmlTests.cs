using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorDmlTests
{
    [Test]
    public void SqlGenerator_GeneratesInsertValuesUpsertAndReturning()
    {
        string text = "insert or replace into main.items as target (id, name) values (1, 'a') on conflict(id) where active = 1 do update set name = excluded.name where target.name != excluded.name returning id, name";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("INSERT OR REPLACE INTO main.items AS target (id, name) VALUES (1, 'a') ON CONFLICT(id) WHERE active = 1 DO UPDATE SET name = excluded.name WHERE target.name <> excluded.name RETURNING id, name"));
    }

    [Test]
    public void SqlGenerator_GeneratesReplaceKeywordAndDefaultValues()
    {
        string replaceGenerated = GenerateStatement("replace into items values (1)");
        string defaultGenerated = GenerateStatement("insert into items default values returning *");

        Assert.That(replaceGenerated, Is.EqualTo("REPLACE INTO items VALUES (1)"));
        Assert.That(defaultGenerated, Is.EqualTo("INSERT INTO items DEFAULT VALUES RETURNING *"));
    }

    [Test]
    public void SqlGenerator_GeneratesInsertSelectSourceAndDoNothingUpsert()
    {
        string text = "insert into archive (id, name) select id, name from staging where active = true on conflict(id) do nothing";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("INSERT INTO archive (id, name) SELECT id, name FROM staging WHERE active = TRUE ON CONFLICT(id) DO NOTHING"));
    }

    [Test]
    public void SqlGenerator_GeneratesUpdateStatement()
    {
        string text = "update or fail items as i indexed by idx_items_name set name = s.name, updated_at = current_timestamp from staging as s where s.id = i.id returning id order by id desc limit 3 offset 1";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("UPDATE OR FAIL items AS i INDEXED BY idx_items_name SET name = s.name, updated_at = CURRENT_TIMESTAMP FROM staging AS s WHERE s.id = i.id RETURNING id ORDER BY id DESC LIMIT 3 OFFSET 1"));
    }

    [Test]
    public void SqlGenerator_GeneratesDeleteStatement()
    {
        string text = "delete from main.items as i not indexed where active = 0 returning id order by id desc limit 2";

        string generated = GenerateStatement(text);

        Assert.That(generated, Is.EqualTo("DELETE FROM main.items AS i NOT INDEXED WHERE active = 0 RETURNING id ORDER BY id DESC LIMIT 2"));
    }

    [Test]
    public void SqlGenerator_DmlStaticApisGenerateTargets()
    {
        SqlStatement insertStatement = ParseStatement("insert into items values (1)");
        SqlStatement updateStatement = ParseStatement("update items set name = 'a'");
        SqlStatement deleteStatement = ParseStatement("delete from items where id = 1");

        string insert = SqlGenerator.GenerateInsert(insertStatement.Insert!);
        string update = SqlGenerator.GenerateUpdate(updateStatement.Update!);
        string delete = SqlGenerator.GenerateDelete(deleteStatement.Delete!);

        Assert.That(insert, Is.EqualTo("INSERT INTO items VALUES (1)"));
        Assert.That(update, Is.EqualTo("UPDATE items SET name = 'a'"));
        Assert.That(delete, Is.EqualTo("DELETE FROM items WHERE id = 1"));
    }

    [Test]
    public void SqlGenerator_DmlApisRejectInvalidInputs()
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateStatement(null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateInsert(null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateUpdate(null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateDelete(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteStatement(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteInsertStatement(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteUpdateStatement(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteDeleteStatement(null!));
    }

    static string GenerateStatement(string text)
    {
        return SqlGenerator.GenerateStatement(ParseStatement(text));
    }

    static SqlStatement ParseStatement(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0);
    }
}
