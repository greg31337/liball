using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorFormattingOptionsTests
{
    [Test]
    public void SqlGenerator_AppliesKeywordCaseOption()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        SqlStatement statement = ParseStatement("select id from items where active = true order by id desc");

        string generated = SqlGenerator.GenerateStatement(statement, options);

        Assert.That(generated, Is.EqualTo("select id from items where active = true order by id desc"));
    }

    [Test]
    public void SqlGenerator_AppliesLeadingCommaStyle()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.CommaStyle = SqlCommaStyle.Leading;
        SqlStatement statement = ParseStatement("select id, name, price from items order by name, id");

        string generated = SqlGenerator.GenerateStatement(statement, options);

        Assert.That(generated, Is.EqualTo("SELECT id\n, name\n, price FROM items ORDER BY name\n, id"));
    }

    [Test]
    public void SqlGenerator_PreservesCommentTokensWhenRequested()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.PreserveComments = true;
        SqlStatement statement = ParseStatement("select /* keep */ id from items");

        string generated = SqlGenerator.GenerateStatement(statement, options);

        Assert.That(generated, Is.EqualTo("/* keep */\nSELECT id FROM items"));
    }

    [Test]
    public void SqlGenerator_OptionsFactoryUsesCopiedOptions()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        SqlGenerator generator = SqlGenerator.Create(options);
        options.KeywordCase = SqlKeywordCase.Upper;

        generator.WriteStatement(ParseStatement("select id from items"));

        Assert.That(generator.ToSql(), Is.EqualTo("select id from items"));
    }

    static SqlStatement ParseStatement(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0);
    }
}
