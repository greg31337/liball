using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorExpressionTests
{
    [TestCase("main.[select]", "main.[select]")]
    [TestCase(":id", ":id")]
    [TestCase("X'53514C'", "X'53514C'")]
    [TestCase("items.*", "items.*")]
    public void SqlGenerator_GeneratesPrimaryExpressions(string text, string expected)
    {
        Assert.That(Generate(text), Is.EqualTo(expected));
    }

    [TestCase("1 + 2 * 3", "1 + 2 * 3")]
    [TestCase("(1 + 2) * 3", "(1 + 2) * 3")]
    [TestCase("not active and archived = 0", "NOT active AND archived = 0")]
    [TestCase("a - (b - c)", "a - (b - c)")]
    public void SqlGenerator_GeneratesUnaryBinaryAndParenthesizedExpressions(string text, string expected)
    {
        Assert.That(Generate(text), Is.EqualTo(expected));
    }

    [Test]
    public void SqlGenerator_GeneratesFunctionCallOptions()
    {
        string text = "sum(distinct amount order by created desc) filter (where active = 1)";

        string generated = Generate(text);

        Assert.That(generated, Is.EqualTo("sum(DISTINCT amount ORDER BY created DESC) FILTER (WHERE active = 1)"));
    }

    [TestCase("cast(total as decimal(10, 2))", "CAST(total AS decimal(10, 2))")]
    [TestCase("case status when 'a' then 1 else 0 end", "CASE status WHEN 'a' THEN 1 ELSE 0 END")]
    [TestCase("(1, name, :id)", "(1, name, :id)")]
    public void SqlGenerator_GeneratesComplexPrimaryExpressions(string text, string expected)
    {
        Assert.That(Generate(text), Is.EqualTo(expected));
    }

    [TestCase("value is not null", "value IS NOT NULL")]
    [TestCase("value not between 1 and 2", "value NOT BETWEEN 1 AND 2")]
    [TestCase("id in (1, 2, 3)", "id IN (1, 2, 3)")]
    [TestCase("id in (select id from old_items)", "id IN (SELECT id FROM old_items)")]
    [TestCase("name not like 'a%' escape '!'", "name NOT LIKE 'a%' ESCAPE '!'")]
    [TestCase("name collate nocase", "name COLLATE nocase")]
    public void SqlGenerator_GeneratesSpecialOperators(string text, string expected)
    {
        Assert.That(Generate(text), Is.EqualTo(expected));
    }

    [Test]
    public void SqlGenerator_GeneratesWindowExpressions()
    {
        string inlineText = "sum(amount) over (partition by category order by created rows between unbounded preceding and current row)";
        string namedText = "row_number() over win";

        string inlineGenerated = Generate(inlineText);
        string namedGenerated = Generate(namedText);

        Assert.That(inlineGenerated, Is.EqualTo("sum(amount) OVER (PARTITION BY category ORDER BY created ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)"));
        Assert.That(namedGenerated, Is.EqualTo("row_number() OVER win"));
    }

    [Test]
    public void SqlGenerator_InstanceWritesIntoProvidedWriter()
    {
        SqlWriter writer = SqlWriter.CreateDefault();
        SqlGenerator generator = SqlGenerator.Create(writer);
        SqlExpression expression = Parse("1 + 2");

        generator.WriteExpression(expression);

        Assert.That(generator.ToSql(), Is.EqualTo("1 + 2"));
        Assert.That(writer.ToSql(), Is.EqualTo("1 + 2"));
    }

    [Test]
    public void SqlGenerator_RejectsInvalidInputs()
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlGenerator.Create((SqlWriter)null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.Create((SqlFormatOptions)null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateExpression(null!));
        Assert.Throws<ArgumentNullException>(() => generator.WriteExpression(null!));
        Assert.Throws<ArgumentNullException>(() => SqlPrecedence.GetExpressionPrecedence(null!));
    }

    static string Generate(string text)
    {
        return SqlGenerator.GenerateExpression(Parse(text));
    }

    static SqlExpression Parse(string text)
    {
        SqlExpressionParseResult result = SqlParser.ParseExpression(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Expression;
    }
}
