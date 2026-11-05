using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlFormatterTests
{
    [Test]
    public void SqlFormatter_FormatFormatsSqlText()
    {
        string formatted = SqlFormatter.Format("select id from items; insert into items values (1)");

        Assert.That(formatted, Is.EqualTo("SELECT id FROM items;\nINSERT INTO items VALUES (1);"));
    }

    [Test]
    public void SqlFormatter_FormatUsesFormatOptions()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.CommaStyle = SqlCommaStyle.Leading;

        string formatted = SqlFormatter.Format("select id, name from items", options);

        Assert.That(formatted, Is.EqualTo("select id\n, name from items;"));
    }

    [Test]
    public void SqlFormatter_FormatScriptFormatsParsedScript()
    {
        SqlScript script = ParseScript("update items set name = 'a' where id = 1; delete from items where id = 2");

        string formatted = SqlFormatter.FormatScript(script);

        Assert.That(formatted, Is.EqualTo("UPDATE items SET name = 'a' WHERE id = 1;\nDELETE FROM items WHERE id = 2;"));
    }

    [Test]
    public void SqlFormatter_RejectsInvalidInputsAndParseFailures()
    {
        SqlFormatOptions invalidOptions = new SqlFormatOptions();
        invalidOptions.IndentText = null!;

        Assert.Throws<ArgumentNullException>(() => SqlFormatter.Format(null!));
        Assert.Throws<ArgumentNullException>(() => SqlFormatter.Format("select 1", null!));
        Assert.Throws<ArgumentNullException>(() => SqlFormatter.FormatScript(null!));
        Assert.Throws<ArgumentNullException>(() => SqlFormatter.FormatScript(SqlScript.CreateEmpty(), null!));
        Assert.Throws<ArgumentNullException>(() => SqlFormatter.Format("select 1", invalidOptions));
        Assert.Throws<FormatException>(() => SqlFormatter.Format("select from"));
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }
}
