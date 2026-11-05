using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectTranspilationTests
{
    [Test]
    public void TranspileScript_RequotesIdentifiersLosslesslyForTargetDialect()
    {
        SqlScript script = Parse("select [display name] from [user table]", SqlDialectKind.SQLite);
        SqlTranspileOptions options = Options(SqlDialectKind.SQLite, SqlDialectKind.Ansi);
        ISqlAstTransformer transformer = new SqlAstTransformService();

        SqlTranspileResult result = transformer.TranspileScript(script, options);

        Assert.That(result.Sql, Is.EqualTo("SELECT \"display name\" FROM \"user table\";"));
        Assert.That(result.IsLossless, Is.True);
        Assert.That(result.Mappings.Length, Is.EqualTo(2));
        Assert.That(result.Mappings[0].Kind, Is.EqualTo(SqlTranspileMappingKind.IdentifierQuoting));
        Assert.That(result.Mappings[0].SourceText, Is.EqualTo("[display name]"));
        Assert.That(result.Mappings[0].TargetText, Is.EqualTo("\"display name\""));
        Assert.That(result.Mappings[0].IsLossless, Is.True);
        Assert.That(result.UnsupportedSyntax, Is.Empty);
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!.Name!.Parts[0].QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
        Assert.That(result.InputScript, Is.SameAs(script));
        Assert.That(script.SourceText, Is.EqualTo("select [display name] from [user table]"));
        Assert.That(script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!.Name!.Parts[0].QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.Bracket));
    }

    [Test]
    public void TranspileScript_RewritesPostgreSqlCastToPortableCastSyntax()
    {
        SqlScript script = Parse("select payload::jsonb from events", SqlDialectKind.PostgreSql);
        SqlTranspileOptions options = Options(SqlDialectKind.PostgreSql, SqlDialectKind.SQLite);

        SqlTranspileResult result = new SqlAstTransformService().TranspileScript(script, options);

        Assert.That(result.Sql, Is.EqualTo("SELECT CAST(payload AS jsonb) FROM events;"));
        Assert.That(result.IsLossless, Is.True);
        Assert.That(result.Mappings.Length, Is.EqualTo(1));
        Assert.That(result.Mappings[0].Kind, Is.EqualTo(SqlTranspileMappingKind.CastSyntax));
        Assert.That(result.Mappings[0].SourceText, Is.EqualTo("payload::jsonb"));
        Assert.That(result.Mappings[0].TargetText, Is.EqualTo("CAST(payload AS jsonb)"));
        Assert.That(result.UnsupportedSyntax, Is.Empty);
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
        Assert.That(result.Script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!.CastData!.SyntaxKind, Is.EqualTo(SqlCastSyntaxKind.CastFunction));
        Assert.That(script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!.CastData!.SyntaxKind, Is.EqualTo(SqlCastSyntaxKind.PostgreSql));
    }

    [Test]
    public void TranspileScript_MarksSyntaxWithoutLosslessTargetMapping()
    {
        SqlScript script = Parse("select array[1, 2] from events", SqlDialectKind.PostgreSql);
        SqlStatement originalStatement = script.Statements[0];
        SqlExpression originalExpression = originalStatement.Query!.SelectCores[0].ResultColumns[0].Expression!;
        SqlToken[] originalTokens = script.Tokens;
        SqlUnsupportedSyntax[] originalUnsupported = script.UnsupportedSyntax;
        SqlTranspileOptions options = Options(SqlDialectKind.PostgreSql, SqlDialectKind.SQLite);

        SqlTranspileResult result = new SqlAstTransformService().TranspileScript(script, options);

        Assert.That(result.IsLossless, Is.False);
        Assert.That(result.UnsupportedSyntax.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(ContainsDescription(result.UnsupportedSyntax, "array expression"), Is.True);
        Assert.That(result.UnsupportedSyntax[0].Kind, Is.EqualTo(SqlUnsupportedSyntaxKind.DialectFeature));
        Assert.That(result.UnsupportedSyntax[0].SourceText, Is.EqualTo("array[1, 2]"));
        Assert.That(result.Diagnostics.HasErrors(), Is.True);
        Assert.That(result.Diagnostics.Diagnostics.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.Diagnostics.Diagnostics[0].Span.Start, Is.EqualTo(result.UnsupportedSyntax[0].Span.Start));
        Assert.That(result.Script.UnsupportedSyntax.Length, Is.GreaterThanOrEqualTo(result.UnsupportedSyntax.Length));
        Assert.That(result.InputScript, Is.SameAs(script));
        Assert.That(script.Statements[0], Is.SameAs(originalStatement));
        Assert.That(script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression, Is.SameAs(originalExpression));
        Assert.That(script.Tokens, Is.SameAs(originalTokens));
        Assert.That(script.UnsupportedSyntax, Is.SameAs(originalUnsupported));
    }

    [Test]
    public void TranspileScript_PreservesExplicitUnsupportedSyntaxInOutput()
    {
        SqlScript script = SqlParser.ParseScript("mystery syntax;", SqlParseOptions.CreateSqlite()).Script;
        SqlUnsupportedSyntax original = script.UnsupportedSyntax[0];
        SqlTranspileOptions options = Options(SqlDialectKind.SQLite, SqlDialectKind.Ansi);

        SqlTranspileResult result = new SqlAstTransformService().TranspileScript(script, options);

        Assert.That(result.IsLossless, Is.False);
        Assert.That(result.UnsupportedSyntax, Is.Not.Empty);
        Assert.That(ContainsDescription(result.UnsupportedSyntax, "lossless mapping"), Is.True);
        Assert.That(result.Script.UnsupportedSyntax, Is.Not.Empty);
        Assert.That(result.Script.UnsupportedSyntax[0].SourceText, Is.EqualTo(original.SourceText));
        Assert.That(result.Diagnostics.HasErrors(), Is.True);
        Assert.That(script.UnsupportedSyntax[0], Is.SameAs(original));
    }

    [Test]
    public void TranspileScript_UsesFormattingAndTerminatorOptions()
    {
        SqlScript script = Parse("select id from items", SqlDialectKind.SQLite);
        SqlTranspileOptions options = Options(SqlDialectKind.SQLite, SqlDialectKind.Ansi);
        options.FormatOptions.KeywordCase = SqlKeywordCase.Lower;
        options.FormatOptions.IdentifierCase = SqlIdentifierCase.Upper;
        options.IncludeSemicolons = false;

        SqlTranspileResult result = new SqlAstTransformService().TranspileScript(script, options);

        Assert.That(result.Sql, Is.EqualTo("select ID from ITEMS"));
        Assert.That(result.IsLossless, Is.True);
        Assert.That(result.ReadDialect, Is.SameAs(options.ReadDialect));
        Assert.That(result.WriteDialect, Is.SameAs(options.WriteDialect));
    }

    [Test]
    public void TranspileScript_RejectsMissingInputsAndNestedOptions()
    {
        ISqlAstTransformer transformer = new SqlAstTransformService();
        SqlScript script = Parse("select 1", SqlDialectKind.SQLite);
        SqlTranspileOptions noReadDialect = new SqlTranspileOptions();
        noReadDialect.ReadDialect = null!;
        SqlTranspileOptions noWriteDialect = new SqlTranspileOptions();
        noWriteDialect.WriteDialect = null!;
        SqlTranspileOptions noFormatOptions = new SqlTranspileOptions();
        noFormatOptions.FormatOptions = null!;

        Assert.Throws<ArgumentNullException>(() => transformer.TranspileScript(null!, new SqlTranspileOptions()));
        Assert.Throws<ArgumentNullException>(() => transformer.TranspileScript(script, null!));
        Assert.Throws<ArgumentNullException>(() => transformer.TranspileScript(script, noReadDialect));
        Assert.Throws<ArgumentNullException>(() => transformer.TranspileScript(script, noWriteDialect));
        Assert.Throws<ArgumentNullException>(() => transformer.TranspileScript(script, noFormatOptions));
    }

    static SqlTranspileOptions Options(SqlDialectKind readKind, SqlDialectKind writeKind)
    {
        SqlTranspileOptions options = new SqlTranspileOptions();
        options.ReadDialect = SqlDialectCatalog.GetDialect(readKind).CreateOptions();
        options.WriteDialect = SqlDialectCatalog.GetDialect(writeKind).CreateOptions();
        return options;
    }

    static SqlScript Parse(string text, SqlDialectKind kind)
    {
        SqlDialectOptions dialect = SqlDialectCatalog.GetDialect(kind).CreateOptions();
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.Create(dialect));
        Assert.That(result.IsSuccess(), Is.True, DiagnosticText(result.Diagnostics));
        return result.Script;
    }

    static bool ContainsDescription(SqlUnsupportedSyntax[] syntax, string text)
    {
        int index = 0;
        while (index < syntax.Length)
        {
            if (syntax[index].Description.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string DiagnosticText(SqlDiagnosticBag diagnostics)
    {
        string text = "";
        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            text = text + diagnostics.Diagnostics[index].Message + "\n";
            index = index + 1;
        }

        return text;
    }
}
