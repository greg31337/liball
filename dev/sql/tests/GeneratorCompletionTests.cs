using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class GeneratorCompletionTests
{
    [Test]
    public void Generator_UsesTypedExpressionPayloadsWithoutLegacyMirrorFields()
    {
        SqlExpression function = ParseExpression("sum(distinct amount order by created) filter (where active)", SqlDialectKind.SQLite);
        function.Name = null;
        function.Arguments = new SqlExpression[0];
        function.OrderBy = new SqlOrderingTerm[0];
        function.Filter = null;

        SqlExpression pattern = ParseExpression("name not like 'a%' escape '!'", SqlDialectKind.SQLite);
        pattern.Left = null;
        pattern.Right = null;
        pattern.Alternate = null;
        pattern.OperatorText = null;
        pattern.OperatorToken = null;
        pattern.Tokens = new SqlToken[0];

        SqlExpression inList = ParseExpression("id not in (1, 2)", SqlDialectKind.SQLite);
        inList.Operand = null;
        inList.Arguments = new SqlExpression[0];
        inList.Query = null;
        inList.Tokens = new SqlToken[0];

        SqlExpression collate = ParseExpression("name collate nocase", SqlDialectKind.SQLite);
        collate.Operand = null;
        collate.Name = null;

        SqlExpression cast = ParseExpression("cast(value as decimal(10, 2))", SqlDialectKind.SQLite);
        cast.Operand = null;
        cast.TypeName = null;

        SqlExpression json = ParseExpression("payload ->> 'name'", SqlDialectKind.PostgreSql);
        json.Left = null;
        json.Right = null;
        json.OperatorText = null;

        SqlExpression wildcard = ParseExpression("items.*", SqlDialectKind.SQLite);
        wildcard.Name = null;

        Assert.That(SqlGenerator.GenerateExpression(function), Is.EqualTo("sum(DISTINCT amount ORDER BY created) FILTER (WHERE active)"));
        Assert.That(SqlGenerator.GenerateExpression(pattern), Is.EqualTo("name NOT LIKE 'a%' ESCAPE '!'"));
        Assert.That(SqlGenerator.GenerateExpression(inList), Is.EqualTo("id NOT IN (1, 2)"));
        Assert.That(SqlGenerator.GenerateExpression(collate), Is.EqualTo("name COLLATE nocase"));
        Assert.That(SqlGenerator.GenerateExpression(cast), Is.EqualTo("CAST(value AS decimal(10, 2))"));
        Assert.That(SqlGenerator.GenerateExpression(json), Is.EqualTo("payload ->> 'name'"));
        Assert.That(SqlGenerator.GenerateExpression(wildcard), Is.EqualTo("items.*"));
    }

    [Test]
    public void Generator_UsesTypedCompoundAndTableSourcePayloads()
    {
        SqlQuery compound = ParseQuery("select 1 union all select 2", SqlDialectKind.SQLite);
        compound.CompoundOperators = new SqlCompoundOperatorKind[0];

        SqlQuery functionQuery = ParseQuery("select * from json_each(payload) as j", SqlDialectKind.SQLite);
        functionQuery.SelectCores[0].From[0].FunctionExpression = null;

        SqlQuery valuesQuery = ParseQuery("select * from (values (1), (2)) as v(x)", SqlDialectKind.Generic);
        valuesQuery.SelectCores[0].From[0].Query = null;

        Assert.That(SqlGenerator.GenerateQuery(compound), Is.EqualTo("SELECT 1 UNION ALL SELECT 2"));
        Assert.That(SqlGenerator.GenerateQuery(functionQuery), Is.EqualTo("SELECT * FROM json_each(payload) AS j"));
        Assert.That(SqlGenerator.GenerateQuery(valuesQuery), Is.EqualTo("SELECT * FROM (VALUES (1), (2)) AS v(x)"));
    }

    [Test]
    public void Generator_UsesTypedDdlPayloadsWhenLegacyFieldsAreEmpty()
    {
        SqlStatement tableStatement = ParseStatement("create table items (value decimal(10, 2) default 1, doubled integer generated always as (value * 2) stored)", SqlDialectKind.SQLite);
        SqlColumnDefinition value = tableStatement.CreateTable!.Columns[0];
        SqlColumnDefinition doubled = tableStatement.CreateTable.Columns[1];
        value.SourceTypeName = "";
        value.TypeName = "";
        value.Constraints = new SqlColumnConstraint[0];
        doubled.SourceTypeName = "";
        doubled.TypeName = "";
        doubled.Constraints = new SqlColumnConstraint[0];

        SqlStatement indexStatement = ParseStatement("create index idx_items on items (lower(value) desc) where value > 0", SqlDialectKind.SQLite);
        indexStatement.CreateIndex!.KeyExpressions = new SqlOrderingTerm[0];
        indexStatement.CreateIndex.WhereExpression = null;

        Assert.That(SqlGenerator.GenerateStatement(tableStatement), Is.EqualTo("CREATE TABLE items (value decimal(10, 2) DEFAULT 1, doubled integer GENERATED ALWAYS AS (value * 2) STORED)"));
        Assert.That(SqlGenerator.GenerateStatement(indexStatement), Is.EqualTo("CREATE INDEX idx_items ON items (lower(value) DESC) WHERE value > 0"));
    }

    [Test]
    public void Generator_PublicApiCoversMergeAndUnsupportedSyntax()
    {
        SqlStatement mergeStatement = ParseStatement("merge into target t using source s on t.id = s.id when matched then delete", SqlDialectKind.SqlServer);
        SqlParseResult unknownResult = ParseScript("frobnicate item;", SqlDialectKind.SQLite);
        SqlStatement unknownStatement = unknownResult.Script.Statements[0];
        ISqlGenerator generator = new SqlGeneratorService();

        Assert.That(generator.GenerateMerge(mergeStatement.Merge!), Is.EqualTo("MERGE INTO target AS t USING source AS s ON t.id = s.id WHEN MATCHED THEN DELETE"));
        Assert.That(generator.GenerateUnsupportedSyntax(unknownStatement.UnsupportedSyntax[0]), Is.EqualTo("frobnicate item;"));
        Assert.That(generator.GenerateStatement(unknownStatement), Is.EqualTo("frobnicate item"));
        Assert.That(generator.GenerateScript(unknownResult.Script), Is.EqualTo("frobnicate item;"));
    }

    [Test]
    public void GenerateOptions_SelectWriteDialectIndependentlyFromParseDialect()
    {
        SqlParseResult result = ParseScript("select value from items", SqlDialectKind.SQLite);
        SqlGenerateOptions options = new SqlGenerateOptions();
        options.WriteDialect = SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql);
        ISqlGenerator service = new SqlGeneratorService();

        string statementSql = service.GenerateStatementWithGenerateOptions(result.Script.Statements[0], options);
        string querySql = service.GenerateQueryWithGenerateOptions(result.Script.Statements[0].Query!, options);
        string expressionSql = service.GenerateExpressionWithGenerateOptions(result.Script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!, options);
        SqlGenerateOptions copy = options.Copy();
        SqlGenerator internalGenerator = SqlGenerator.CreateForGeneration(options);

        Assert.That(result.Options.Dialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(options.WriteDialect.Kind, Is.EqualTo(SqlDialectKind.PostgreSql));
        Assert.That(copy.WriteDialect, Is.SameAs(options.WriteDialect));
        Assert.That(internalGenerator.WriteDialect.Kind, Is.EqualTo(SqlDialectKind.PostgreSql));
        Assert.That(statementSql, Is.EqualTo("SELECT value FROM items"));
        Assert.That(querySql, Is.EqualTo("SELECT value FROM items"));
        Assert.That(expressionSql, Is.EqualTo("value"));

        options.WriteDialect = null!;
        Assert.Throws<ArgumentNullException>(() => service.GenerateStatementWithGenerateOptions(result.Script.Statements[0], options));
    }

    static SqlExpression ParseExpression(string text, SqlDialectKind kind)
    {
        SqlParseOptions options = SqlParseOptions.Create(SqlDialectCatalog.CreateOptions(kind));
        SqlExpressionParseResult result = SqlParser.ParseExpression(text, options);
        Assert.That(result.IsSuccess(), Is.True, FirstDiagnostic(result.Diagnostics));
        return result.Expression;
    }

    static SqlQuery ParseQuery(string text, SqlDialectKind kind)
    {
        return ParseStatement(text, kind).Query!;
    }

    static SqlStatement ParseStatement(string text, SqlDialectKind kind)
    {
        return ParseScript(text, kind).Script.Statements[0];
    }

    static SqlParseResult ParseScript(string text, SqlDialectKind kind)
    {
        SqlParseOptions options = SqlParseOptions.Create(SqlDialectCatalog.CreateOptions(kind));
        SqlParseResult result = SqlParser.ParseScript(text, options);
        if (result.Script.Statements.Length == 0)
        {
            Assert.Fail(FirstDiagnostic(result.Diagnostics));
        }

        return result;
    }

    static string FirstDiagnostic(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.IsEmpty())
        {
            return "";
        }

        return diagnostics.GetAt(0).Code + ": " + diagnostics.GetAt(0).Message;
    }
}
