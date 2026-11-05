using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlStatementSummaryTests
{
    [Test]
    public void SqlAnalyzer_SummarizesSelectReadTables()
    {
        SqlStatement statement = ParseFirst("select u.id from users as u join orders as o on o.user_id = u.id where exists (select 1 from payments as p where p.order_id = o.id)");

        SqlStatementSummary summary = SqlAnalyzer.GetStatementSummary(statement);

        Assert.That(summary.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(summary.HasUnknownEffects, Is.False);
        Assert.That(summary.HasWriteTables(), Is.False);
        AssertContainsName(summary.ReadTables, "users");
        AssertContainsName(summary.ReadTables, "orders");
        AssertContainsName(summary.ReadTables, "payments");
    }

    [Test]
    public void SqlAnalyzer_SummarizesDmlReadAndWriteTables()
    {
        SqlScript script = ParseScript("insert into audit(user_id,total) select id,total from users; update audit set total = total + 1 from adjustments where audit.user_id = adjustments.user_id; delete from audit where exists (select 1 from old_audit where old_audit.id = audit.id)");

        SqlStatementSummary insert = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(0));
        SqlStatementSummary update = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(1));
        SqlStatementSummary delete = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(2));

        AssertContainsName(insert.WriteTables, "audit");
        AssertContainsName(insert.ReadTables, "users");
        AssertContainsName(update.WriteTables, "audit");
        AssertContainsName(update.ReadTables, "adjustments");
        AssertContainsName(delete.WriteTables, "audit");
        AssertContainsName(delete.ReadTables, "old_audit");
        Assert.That(insert.HasUnknownEffects, Is.False);
        Assert.That(update.HasUnknownEffects, Is.False);
        Assert.That(delete.HasUnknownEffects, Is.False);
    }

    [Test]
    public void SqlAnalyzer_SummarizesDdlReadAndWriteTables()
    {
        SqlScript script = ParseScript("create table archive as select id from items; create view v_items as select id from items; drop table old_items");

        SqlStatementSummary createTable = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(0));
        SqlStatementSummary createView = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(1));
        SqlStatementSummary dropTable = SqlAnalyzer.GetStatementSummary(script.GetStatementAt(2));

        AssertContainsName(createTable.WriteTables, "archive");
        AssertContainsName(createTable.ReadTables, "items");
        AssertContainsName(createView.WriteTables, "v_items");
        AssertContainsName(createView.ReadTables, "items");
        AssertContainsName(dropTable.WriteTables, "old_items");
        Assert.That(dropTable.HasReadTables(), Is.False);
    }

    [Test]
    public void SqlAnalyzer_SummarizesExplainAndUnknownEffects()
    {
        SqlStatement explain = ParseFirst("explain query plan select id from items");
        SqlStatement pragma = ParseFirst("pragma cache_size = 1000");

        SqlStatementSummary explainSummary = SqlAnalyzer.GetStatementSummary(explain);
        SqlStatementSummary pragmaSummary = SqlAnalyzer.GetStatementSummary(pragma);
        SqlStatementSummary nullSummary = SqlAnalyzer.GetStatementSummary(null);

        Assert.That(explainSummary.Kind, Is.EqualTo(SqlStatementKind.Explain));
        AssertContainsName(explainSummary.ReadTables, "items");
        Assert.That(explainSummary.HasWriteTables(), Is.False);
        Assert.That(pragmaSummary.HasUnknownEffects, Is.True);
        Assert.That(nullSummary.Kind, Is.EqualTo(SqlStatementKind.Unknown));
        Assert.That(nullSummary.HasUnknownEffects, Is.True);
    }

    [Test]
    public void SqlStatementSummary_StoresTablesAndRejectsInvalidInputs()
    {
        SqlName read = Name("items", 0);
        SqlName write = Name("audit", 10);
        SqlStatementSummary summary = new SqlStatementSummary(SqlStatementKind.Insert, new SqlName[] { read }, new SqlName[] { write }, false);

        Assert.That(summary.HasReadTables(), Is.True);
        Assert.That(summary.GetReadTableCount(), Is.EqualTo(1));
        Assert.That(summary.GetReadTableAt(0), Is.SameAs(read));
        Assert.That(summary.HasWriteTables(), Is.True);
        Assert.That(summary.GetWriteTableCount(), Is.EqualTo(1));
        Assert.That(summary.GetWriteTableAt(0), Is.SameAs(write));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementSummary(SqlStatementKind.Select, null!, new SqlName[0], false));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementSummary(SqlStatementKind.Select, new SqlName[0], null!, false));
        Assert.Throws<ArgumentNullException>(() => new SqlStatementSummary(SqlStatementKind.Select, new SqlName[] { null! }, new SqlName[0], false));
    }

    static SqlStatement ParseFirst(string text)
    {
        return ParseScript(text).GetStatementAt(0);
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }

    static void AssertContainsName(SqlName[] names, string text)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].GetText() == text)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected name '" + text + "'.");
    }

    static SqlName Name(string text, int start)
    {
        SqlNamePart part = SqlNamePart.Create(text, text, new SqlTextSpan(start, text.Length), SqlNameQuoteStyle.None);
        return new SqlName(new SqlNamePart[] { part }, part.Span);
    }
}
