using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FormatterCompletionTests
{
    [Test]
    public void Formatter_AppliesIdentifierCaseAndWriteDialectQuoting()
    {
        SqlFormatOptions options = CreateOptions(SqlDialectKind.SQLite, SqlDialectKind.PostgreSql);
        options.KeywordCase = SqlKeywordCase.Upper;
        options.IdentifierCase = SqlIdentifierCase.Lower;

        string formatted = SqlFormatter.Format("select [Mixed Name] as MixedAlias from `Some Table`", options);

        Assert.That(formatted, Is.EqualTo("SELECT \"Mixed Name\" AS mixedalias FROM \"Some Table\";"));
    }

    [Test]
    public void Formatter_AddsTrailingSelectCommaOnlyForSupportingDialect()
    {
        SqlFormatOptions bigQuery = CreateOptions(SqlDialectKind.SQLite, SqlDialectKind.BigQuery);
        bigQuery.TrailingCommaPolicy = SqlTrailingCommaPolicy.WhenSupported;
        SqlFormatOptions sqlite = CreateOptions(SqlDialectKind.SQLite, SqlDialectKind.SQLite);
        sqlite.TrailingCommaPolicy = SqlTrailingCommaPolicy.WhenSupported;

        string bigQuerySql = SqlFormatter.Format("select id, name from items", bigQuery);
        string sqliteSql = SqlFormatter.Format("select id, name from items", sqlite);

        Assert.That(bigQuerySql, Is.EqualTo("SELECT id, name, FROM items;"));
        Assert.That(sqliteSql, Is.EqualTo("SELECT id, name FROM items;"));
    }

    [Test]
    public void Formatter_WrapsGeneratedSqlAtPreferredLineWidth()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.TargetLineWidth = 24;

        string formatted = SqlFormatter.Format("select identifier, display_name, created_at from customer_records where active = true order by display_name", options);
        string[] lines = formatted.Split('\n');

        Assert.That(lines.Length, Is.GreaterThan(1));
        int index = 0;
        while (index < lines.Length)
        {
            Assert.That(lines[index].Length, Is.LessThanOrEqualTo(24), "line " + index.ToString());
            index = index + 1;
        }
    }

    [Test]
    public void Formatter_ProvidesCompactAndMinimalModes()
    {
        const string text = "select id, name from items where id = 1; update items set name = 'a' where id = 1";
        SqlFormatOptions compact = new SqlFormatOptions();
        compact.Mode = SqlFormatMode.Compact;
        SqlFormatOptions minimal = new SqlFormatOptions();
        minimal.Mode = SqlFormatMode.Minimal;

        string compactSql = SqlFormatter.Format(text, compact);
        string minimalSql = SqlFormatter.Format(text, minimal);

        Assert.That(compactSql, Is.EqualTo("SELECT id, name FROM items WHERE id = 1; UPDATE items SET name = 'a' WHERE id = 1;"));
        Assert.That(minimalSql, Is.EqualTo("SELECT id,name FROM items WHERE id=1;UPDATE items SET name='a' WHERE id=1;"));
    }

    [Test]
    public void Formatter_RoundTripModePreservesCommentsLayoutAndTerminators()
    {
        const string text = "-- lead\nSeLeCt  [Name] /* middle */ FROM `Items`;;\n";
        SqlFormatOptions options = new SqlFormatOptions();
        options.Mode = SqlFormatMode.RoundTrip;
        options.KeywordCase = SqlKeywordCase.Lower;
        options.IdentifierCase = SqlIdentifierCase.Upper;
        options.PreserveComments = true;

        string formatted = SqlFormatter.Format(text, options);

        Assert.That(formatted, Is.EqualTo(text));
    }

    [Test]
    public void Formatter_UsesExplicitReadDialectForRawSql()
    {
        SqlFormatOptions options = CreateOptions(SqlDialectKind.PostgreSql, SqlDialectKind.PostgreSql);

        string formatted = SqlFormatter.Format("select $$hello$$ as Message", options);

        Assert.That(formatted, Is.EqualTo("SELECT $$hello$$ AS Message;"));
        options.ReadDialect = SqlDialectCatalog.CreateOptions(SqlDialectKind.SQLite);
        Assert.Throws<FormatException>(() => SqlFormatter.Format("select $$hello$$ as Message", options));
    }

    [Test]
    public void FormatOptions_CopyAndValidationCoverExpandedSettings()
    {
        SqlFormatOptions options = CreateOptions(SqlDialectKind.PostgreSql, SqlDialectKind.BigQuery);
        options.IdentifierCase = SqlIdentifierCase.Upper;
        options.TrailingCommaPolicy = SqlTrailingCommaPolicy.WhenSupported;
        options.Mode = SqlFormatMode.Compact;
        options.TargetLineWidth = 72;

        SqlFormatOptions copy = options.Copy();

        Assert.That(copy.IdentifierCase, Is.EqualTo(SqlIdentifierCase.Upper));
        Assert.That(copy.TrailingCommaPolicy, Is.EqualTo(SqlTrailingCommaPolicy.WhenSupported));
        Assert.That(copy.Mode, Is.EqualTo(SqlFormatMode.Compact));
        Assert.That(copy.TargetLineWidth, Is.EqualTo(72));
        Assert.That(copy.ReadDialect.Kind, Is.EqualTo(SqlDialectKind.PostgreSql));
        Assert.That(copy.WriteDialect.Kind, Is.EqualTo(SqlDialectKind.BigQuery));

        options.TargetLineWidth = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        options = new SqlFormatOptions();
        options.ReadDialect = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());
        options = new SqlFormatOptions();
        options.WriteDialect = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());
    }

    [Test]
    public void FormatterService_FormatsExpressionQueryAndStatementTrees()
    {
        SqlParseResult result = SqlParser.ParseScript("select value + 1 from items", SqlParseOptions.CreateSqlite());
        SqlStatement statement = result.Script.Statements[0];
        SqlQuery query = statement.Query!;
        SqlExpression expression = query.SelectCores[0].ResultColumns[0].Expression!;
        SqlFormatOptions options = new SqlFormatOptions();
        options.IdentifierCase = SqlIdentifierCase.Upper;
        ISqlFormatter formatter = new SqlFormatterService(new SqlParserService());

        Assert.That(formatter.FormatExpression(expression, options), Is.EqualTo("VALUE + 1"));
        Assert.That(formatter.FormatQuery(query, options), Is.EqualTo("SELECT VALUE + 1 FROM ITEMS"));
        Assert.That(formatter.FormatStatement(statement, options), Is.EqualTo("SELECT VALUE + 1 FROM ITEMS"));
    }

    static SqlFormatOptions CreateOptions(SqlDialectKind readKind, SqlDialectKind writeKind)
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.ReadDialect = SqlDialectCatalog.CreateOptions(readKind);
        options.WriteDialect = SqlDialectCatalog.CreateOptions(writeKind);
        return options;
    }
}
