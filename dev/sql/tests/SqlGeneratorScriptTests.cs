using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlGeneratorScriptTests
{
    [Test]
    public void SqlGenerator_GeneratesScriptsWithSemicolonsByDefault()
    {
        SqlScript script = ParseScript("select id from items; pragma cache_size = 1000");

        string generated = SqlGenerator.GenerateScript(script);

        Assert.That(generated, Is.EqualTo("SELECT id FROM items;\nPRAGMA cache_size = 1000;"));
    }

    [Test]
    public void SqlGenerator_GenerateScriptUsesFormatOptions()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        SqlScript script = ParseScript("select id from items");

        string generated = SqlGenerator.GenerateScript(script, options);

        Assert.That(generated, Is.EqualTo("select id from items;"));
    }

    [Test]
    public void SqlGenerator_GenerateScriptUsesGenerateOptions()
    {
        SqlGenerateOptions options = new SqlGenerateOptions();
        options.IncludeSemicolons = false;
        options.FormatOptions.KeywordCase = SqlKeywordCase.Lower;
        SqlScript script = ParseScript("select id from items; select name from items");

        string generated = SqlGenerator.GenerateScript(script, options);

        Assert.That(generated, Is.EqualTo("select id from items\nselect name from items"));
    }

    [Test]
    public void SqlGenerateOptions_CopyPreservesValues()
    {
        SqlGenerateOptions options = new SqlGenerateOptions();
        options.IncludeSemicolons = false;
        options.FormatOptions.KeywordCase = SqlKeywordCase.Lower;

        SqlGenerateOptions copy = options.Copy();

        Assert.That(copy.IncludeSemicolons, Is.False);
        Assert.That(copy.FormatOptions.KeywordCase, Is.EqualTo(SqlKeywordCase.Lower));
    }

    [Test]
    public void SqlGenerator_ScriptApisRejectInvalidInputs()
    {
        SqlGenerateOptions invalidOptions = new SqlGenerateOptions();
        invalidOptions.FormatOptions = null!;
        SqlGenerator generator = SqlGenerator.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateScript(null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateScript(SqlScript.CreateEmpty(), (SqlFormatOptions)null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateScript(SqlScript.CreateEmpty(), (SqlGenerateOptions)null!));
        Assert.Throws<ArgumentNullException>(() => SqlGenerator.GenerateScript(SqlScript.CreateEmpty(), invalidOptions));
        Assert.Throws<ArgumentNullException>(() => generator.WriteScript(null!, new SqlGenerateOptions()));
        Assert.Throws<ArgumentNullException>(() => generator.WriteScript(SqlScript.CreateEmpty(), null!));
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }
}
