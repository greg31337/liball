using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FullFidelityAstTests
{
    [Test]
    public void ParseScript_PopulatesTriviaTerminatorsTokenRangesAndQuotedTokens()
    {
        const string text = "-- lead\nSELECT [Name], 'x' /* inner */ FROM `Items`;; -- tail\n";
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        SqlScript script = result.Script;
        SqlStatement select = script.GetStatementAt(0);
        SqlStatement empty = script.GetStatementAt(1);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(script.SourceText, Is.EqualTo(text));
        Assert.That(script.Tokens, Is.SameAs(result.Tokens));
        Assert.That(script.TokenRange.Start, Is.EqualTo(0));
        Assert.That(script.TokenRange.Length, Is.EqualTo(result.Tokens.Length - 1));
        Assert.That(script.LeadingTrivia, Has.Length.EqualTo(2));
        Assert.That(script.LeadingTrivia[0].Kind, Is.EqualTo(SqlTriviaKind.LineComment));
        Assert.That(script.TrailingTrivia, Has.Length.EqualTo(3));
        Assert.That(script.TrailingTrivia[0].Text, Is.EqualTo(" "));
        Assert.That(script.TrailingTrivia[1].Kind, Is.EqualTo(SqlTriviaKind.LineComment));
        Assert.That(select.LeadingTrivia, Is.SameAs(script.LeadingTrivia));
        Assert.That(select.Terminator, Is.Not.Null);
        Assert.That(select.Terminator!.Text, Is.EqualTo(";"));
        Assert.That(select.SourceText, Is.EqualTo("SELECT [Name], 'x' /* inner */ FROM `Items`;"));
        Assert.That(select.QuotedTokens, Has.Length.EqualTo(3));
        Assert.That(select.QuotedTokens[0].IdentifierQuoteStyle, Is.EqualTo(SqlNameQuoteStyle.Bracket));
        Assert.That(select.QuotedTokens[1].StringQuoteStyle, Is.EqualTo(SqlStringQuoteStyle.SingleQuote));
        Assert.That(select.QuotedTokens[2].IdentifierQuoteStyle, Is.EqualTo(SqlNameQuoteStyle.Backtick));
        Assert.That(select.TokenRange.Start, Is.GreaterThan(0));
        Assert.That(empty.Kind, Is.EqualTo(SqlStatementKind.Empty));
        Assert.That(empty.Terminator, Is.Not.Null);
        Assert.That(empty.TokenRange.Length, Is.EqualTo(1));
        Assert.That(empty.TrailingTrivia, Is.SameAs(script.TrailingTrivia));
    }

    [Test]
    public void GenerateScript_PreservedSourceRetainsCommentsEmptyStatementsAndTerminators()
    {
        const string text = " /* first */ SELECT \"Mixed Name\" FROM [Some Table];;\n-- last\nPRAGMA cache_size=-10";
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        SqlGenerateOptions options = new SqlGenerateOptions();
        options.PreserveSourceText = true;
        options.IncludeSemicolons = false;
        options.FormatOptions.KeywordCase = SqlKeywordCase.Upper;

        string generated = SqlGenerator.GenerateScript(result.Script, options);
        SqlGenerateOptions copy = options.Copy();

        Assert.That(generated, Is.EqualTo(text));
        Assert.That(copy.PreserveSourceText, Is.True);
        Assert.That(copy.IncludeSemicolons, Is.False);
    }

    [Test]
    public void ParseScript_CreatesUnknownAndDialectUnsupportedMarkers()
    {
        SqlParseResult unknownResult = SqlParser.ParseScript("frobnicate [item];", SqlParseOptions.CreateSqlite());
        SqlStatement unknown = unknownResult.Script.GetStatementAt(0);
        SqlParseResult dialectResult = SqlParser.ParseScript("insert into items values (1) returning id", new SqlParseOptions());
        SqlStatement dialectStatement = dialectResult.Script.GetStatementAt(0);
        SqlParseResult skippedResult = SqlParser.ParseScript("select #", SqlParseOptions.CreateSqlite());
        SqlStatement skippedStatement = skippedResult.Script.GetStatementAt(0);

        Assert.That(unknown.UnsupportedSyntax, Has.Length.EqualTo(1));
        Assert.That(unknown.UnsupportedSyntax[0].Kind, Is.EqualTo(SqlUnsupportedSyntaxKind.UnknownStatement));
        Assert.That(unknown.UnsupportedSyntax[0].SourceText, Is.EqualTo("frobnicate [item];"));
        Assert.That(unknownResult.Script.UnsupportedSyntax, Is.EqualTo(unknown.UnsupportedSyntax));
        Assert.That(dialectResult.IsSuccess(), Is.False);
        Assert.That(dialectStatement.UnsupportedSyntax, Has.Some.Matches<SqlUnsupportedSyntax>(marker => marker.Kind == SqlUnsupportedSyntaxKind.DialectFeature));
        Assert.That(skippedResult.IsSuccess(), Is.False);
        Assert.That(skippedStatement.UnsupportedSyntax, Has.Some.Matches<SqlUnsupportedSyntax>(marker => marker.Kind == SqlUnsupportedSyntaxKind.SkippedText && marker.SourceText == "#"));
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkStatement(dialectStatement);
        Assert.That(walker.UnsupportedSyntax, Is.EqualTo(dialectStatement.UnsupportedSyntax));
    }

    [Test]
    public void Lexer_BindsStandaloneTriviaToAdjacentTokens()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("select /* gap */ value");
        SqlToken select = tokens[0];
        SqlToken value = tokens[4];

        Assert.That(select.TrailingTrivia, Has.Length.EqualTo(3));
        Assert.That(select.TrailingTrivia[1].Kind, Is.EqualTo(SqlTriviaKind.BlockComment));
        Assert.That(value.LeadingTrivia, Is.SameAs(select.TrailingTrivia));
        Assert.That(value.LeadingTrivia[1].Text, Is.EqualTo("/* gap */"));
    }
}
