using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorSelectTests
{
    [Test]
    public void SqlGenerator_GeneratesSelectClauses()
    {
        string text = "select distinct a as alpha, b + 1 beta from main.items as i where i.id = :id group by a having count(*) > 1 order by alpha desc limit 10 offset 5";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("SELECT DISTINCT a AS alpha, b + 1 AS beta FROM main.items AS i WHERE i.id = :id GROUP BY a HAVING count(*) > 1 ORDER BY alpha DESC LIMIT 10 OFFSET 5"));
    }

    [Test]
    public void SqlGenerator_GeneratesTableAliasesAndJoins()
    {
        string text = "select * from items i join orders o on i.id = o.item_id left outer join customers as c using (id)";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("SELECT * FROM items AS i JOIN orders AS o ON i.id = o.item_id LEFT OUTER JOIN customers AS c USING (id)"));
    }

    [Test]
    public void SqlGenerator_GeneratesCtesAndCompoundQueries()
    {
        string text = "with recursive nums(n) as (select 1 union all select n + 1 from nums limit 3) select n from nums union all select 0 order by n limit 5";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("WITH RECURSIVE nums(n) AS (SELECT 1 UNION ALL SELECT n + 1 FROM nums LIMIT 3) SELECT n FROM nums UNION ALL SELECT 0 ORDER BY n LIMIT 5"));
    }

    [Test]
    public void SqlGenerator_GeneratesValuesQueries()
    {
        string generated = Generate("values (1, 'a'), (2, 'b')");

        Assert.That(generated, Is.EqualTo("VALUES (1, 'a'), (2, 'b')"));
    }

    [Test]
    public void SqlGenerator_GeneratesSubqueryAndFunctionTableReferences()
    {
        string text = "select * from (select 1 as id) as s, json_each(s.id) j";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("SELECT * FROM (SELECT 1 AS id) AS s, json_each(s.id) AS j"));
    }

    [Test]
    public void SqlGenerator_GeneratesWindowClause()
    {
        string text = "select sum(amount) over win from items window win as (partition by category order by created)";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("SELECT sum(amount) OVER win FROM items WINDOW win AS (PARTITION BY category ORDER BY created)"));
    }

    [Test]
    public void SqlGenerator_QueryApisRejectInvalidInputs()
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        SqlSelectCore core = SqlSelectCore.Create(new SqlResultColumn[0], new SqlTextSpan(0, 0));

        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateQuery(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteQuery(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteSelectCore(null!));
        Assert.DoesNotThrow(() => generator.WriteSelectCore(core));
    }

    static string Generate(string text)
    {
        return SqlGenerator.GenerateQuery(ParseQuery(text));
    }

    static SqlQuery ParseQuery(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Query, Is.Not.Null);
        return statement.Query!;
    }
}
