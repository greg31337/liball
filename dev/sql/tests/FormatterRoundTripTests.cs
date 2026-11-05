using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FormatterRoundTripTests
{
    [TestCase("with active_items as (select id,name from items where active=1) select id,name from active_items where id in (select item_id from orders) order by name limit 10")]
    [TestCase("insert into items(id,name) values (1,'a'),(2,'b') on conflict(id) do update set name=excluded.name returning id,name; update items set name='c' where id=1 returning *; delete from items where id in (select id from old_items) returning id")]
    [TestCase("create table if not exists items(id integer primary key, name text not null, price real default 0) strict; create index idx_items_name on items(name collate nocase desc) where name is not null; pragma cache_size=-2000; vacuum main into 'backup.db'")]
    [TestCase("begin immediate transaction; savepoint sp1; release savepoint sp1; rollback transaction; attach database 'extra.db' as aux; detach database aux")]
    public void SqlFormatter_ParseFormatParseRoundTripsWithDefaultOptions(string sql)
    {
        AssertRoundTrip(sql, SqlFormatOptions.CreateDefault());
    }

    [Test]
    public void SqlFormatter_ParseFormatParseRoundTripsWithFormattingOptions()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.CommaStyle = SqlCommaStyle.Leading;

        AssertRoundTrip("select id,name,price from items where id in (select item_id from orders) order by name,id", options);
    }

    static void AssertRoundTrip(string sql, SqlFormatOptions options)
    {
        SqlParseResult first = SqlParser.ParseScript(sql, SqlParseOptions.CreateSqlite());
        Assert.That(first.IsSuccess(), Is.True, "initial parse");

        string formatted = SqlFormatter.Format(sql, options);
        SqlParseResult second = SqlParser.ParseScript(formatted, SqlParseOptions.CreateSqlite());
        Assert.That(second.IsSuccess(), Is.True, "formatted parse");

        Assert.That(second.Script.GetStatementCount(), Is.EqualTo(first.Script.GetStatementCount()));
        AssertStatementKinds(first.Script, second.Script);

        string formattedAgain = SqlFormatter.Format(formatted, options);
        Assert.That(formattedAgain, Is.EqualTo(formatted));
        Assert.That(SqlGenerator.GenerateScript(second.Script), Is.EqualTo(SqlGenerator.GenerateScript(first.Script)));
    }

    static void AssertStatementKinds(SqlScript expected, SqlScript actual)
    {
        int index = 0;
        while (index < expected.GetStatementCount())
        {
            Assert.That(actual.GetStatementAt(index).Kind, Is.EqualTo(expected.GetStatementAt(index).Kind), "statement " + index.ToString());
            index = index + 1;
        }
    }
}
