using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstNormalizationTests
{
    [Test]
    public void CanonicalizeScript_ProducesStableTextAndFreshEquivalentTrees()
    {
        SqlScript first = Parse("select Foo, Bar from Items where Foo = 1");
        SqlScript second = Parse("  SELECT foo,bar\nFROM items WHERE foo=1; ");
        ISqlAstTransformer transformer = new SqlAstTransformService();
        SqlCanonicalizationOptions options = new SqlCanonicalizationOptions();

        SqlCanonicalizationResult firstResult = transformer.CanonicalizeScript(first, options);
        SqlCanonicalizationResult secondResult = transformer.CanonicalizeScript(second, options);

        Assert.That(firstResult.CanonicalText, Is.EqualTo("SELECT foo,bar FROM items WHERE foo=1;"));
        Assert.That(secondResult.CanonicalText, Is.EqualTo(firstResult.CanonicalText));
        Assert.That(firstResult.Script, Is.Not.SameAs(first));
        Assert.That(firstResult.InputScript, Is.SameAs(first));
        Assert.That(firstResult.Script.Statements[0].Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(firstResult.Script.SourceText, Is.EqualTo(firstResult.CanonicalText));
        Assert.That(firstResult.Markers, Is.Empty);
        Assert.That(firstResult.Diagnostics.HasErrors(), Is.False);
        Assert.That(first.SourceText, Is.EqualTo("select Foo, Bar from Items where Foo = 1"));
    }

    [Test]
    public void CanonicalizeScript_IsIdempotent()
    {
        ISqlAstTransformer transformer = new SqlAstTransformService();
        SqlCanonicalizationOptions options = new SqlCanonicalizationOptions();
        SqlCanonicalizationResult first = transformer.CanonicalizeScript(Parse("select A from T where A in (1, 2)"), options);

        SqlCanonicalizationResult second = transformer.CanonicalizeScript(first.Script, options);

        Assert.That(second.CanonicalText, Is.EqualTo(first.CanonicalText));
        Assert.That(second.Markers, Is.Empty);
        Assert.That(second.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void NormalizeScript_AppliesExplicitCaseCommentAndTerminatorOptions()
    {
        SqlScript script = Parse("select /* keep */ item from inventory;");
        SqlNormalizationOptions options = new SqlNormalizationOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.IdentifierCase = SqlIdentifierCase.Upper;
        options.Mode = SqlFormatMode.Compact;
        options.PreserveComments = true;
        options.IncludeSemicolons = false;

        SqlNormalizationResult result = new SqlAstTransformService().NormalizeScript(script, options);

        Assert.That(result.NormalizedText, Is.EqualTo("/* keep */ select ITEM from INVENTORY"));
        Assert.That(result.Script.SourceText, Is.EqualTo(result.NormalizedText));
        Assert.That(result.Script.Tokens, Is.Not.SameAs(script.Tokens));
        Assert.That(result.Markers, Is.Empty);
        Assert.That(result.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void CanonicalizeScript_PreservesExplicitUnsupportedMarkers()
    {
        SqlScript script = Parse("mystery syntax;");
        SqlUnsupportedSyntax originalMarker = script.UnsupportedSyntax[0];

        SqlCanonicalizationResult result = new SqlAstTransformService().CanonicalizeScript(script, new SqlCanonicalizationOptions());

        Assert.That(result.Markers.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.Markers[0].Kind, Is.EqualTo(SqlNormalizationMarkerKind.UnsupportedSyntax));
        Assert.That(result.Markers[0].SourceText, Is.EqualTo("mystery syntax;"));
        Assert.That(result.Markers[0].UnsupportedSyntax, Is.SameAs(originalMarker));
        Assert.That(result.Script.UnsupportedSyntax.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.Script.UnsupportedSyntax[0].SourceText, Is.EqualTo(originalMarker.SourceText));
        Assert.That(result.InputScript.UnsupportedSyntax[0], Is.SameAs(originalMarker));
    }

    [Test]
    public void NormalizeScript_PreservesInputTreeAndReportsUnknownSyntaxMarkers()
    {
        SqlScript script = Parse("select #; mystery syntax;");
        SqlStatement firstStatement = script.Statements[0];
        SqlStatement secondStatement = script.Statements[1];
        SqlExpression originalExpression = firstStatement.Query!.SelectCores[0].ResultColumns[0].Expression!;
        SqlToken[] originalTokens = script.Tokens;
        SqlUnsupportedSyntax[] originalUnsupported = script.UnsupportedSyntax;

        SqlNormalizationResult result = new SqlAstTransformService().NormalizeScript(script, new SqlNormalizationOptions());

        Assert.That(result.InputScript, Is.SameAs(script));
        Assert.That(result.Script, Is.Not.SameAs(script));
        Assert.That(HasMarker(result.Markers, SqlNormalizationMarkerKind.UnknownExpression), Is.True);
        Assert.That(HasMarker(result.Markers, SqlNormalizationMarkerKind.UnsupportedSyntax), Is.True);
        Assert.That(script.Statements[0], Is.SameAs(firstStatement));
        Assert.That(script.Statements[1], Is.SameAs(secondStatement));
        Assert.That(script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression, Is.SameAs(originalExpression));
        Assert.That(script.Tokens, Is.SameAs(originalTokens));
        Assert.That(script.UnsupportedSyntax, Is.SameAs(originalUnsupported));
    }

    [Test]
    public void CanonicalizeScript_HonorsCaseCommentAndTerminatorOptions()
    {
        SqlScript script = Parse("-- keep\nSELECT MixedName FROM MixedTable;\n-- tail");
        SqlCanonicalizationOptions options = new SqlCanonicalizationOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.IdentifierCase = SqlIdentifierCase.Upper;
        options.PreserveComments = true;
        options.IncludeSemicolons = false;

        SqlCanonicalizationResult result = new SqlAstTransformService().CanonicalizeScript(script, options);

        Assert.That(result.CanonicalText, Is.EqualTo("-- keep\nselect MIXEDNAME from MIXEDTABLE\n-- tail"));
        Assert.That(result.Script.SourceText, Is.EqualTo(result.CanonicalText));
        Assert.That(script.SourceText, Is.EqualTo("-- keep\nSELECT MixedName FROM MixedTable;\n-- tail"));
    }

    [Test]
    public void TransformOperations_RejectInvalidInputsAndOptions()
    {
        ISqlAstTransformer transformer = new SqlAstTransformService();
        SqlScript script = Parse("select 1");
        SqlNormalizationOptions normalizationOptions = new SqlNormalizationOptions();
        normalizationOptions.Mode = (SqlFormatMode)99;
        SqlCanonicalizationOptions canonicalizationOptions = new SqlCanonicalizationOptions();
        canonicalizationOptions.Dialect = null!;

        Assert.Throws<ArgumentNullException>(() => transformer.NormalizeScript(null!, new SqlNormalizationOptions()));
        Assert.Throws<ArgumentNullException>(() => transformer.NormalizeScript(script, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => transformer.NormalizeScript(script, normalizationOptions));
        Assert.Throws<ArgumentNullException>(() => transformer.CanonicalizeScript(script, canonicalizationOptions));
    }

    static SqlScript Parse(string text)
    {
        return SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite()).Script;
    }

    static bool HasMarker(SqlNormalizationMarker[] markers, SqlNormalizationMarkerKind kind)
    {
        int index = 0;
        while (index < markers.Length)
        {
            if (markers[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
