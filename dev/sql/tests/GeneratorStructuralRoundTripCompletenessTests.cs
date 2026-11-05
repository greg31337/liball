using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class GeneratorStructuralRoundTripCompletenessTests
{
    [TestCase(SqlDialectKind.SQLite, "select case when a = 1 then cast(b as text) else 'x' end, exists(select 1), id in (1, 2), name not like 'x%' escape '!' from items")]
    [TestCase(SqlDialectKind.SQLite, "with recursive cte(id) as materialized (values (1)) select sum(id) filter (where id > 0) over win from cte window win as (order by id rows between unbounded preceding and current row exclude ties) union all select 0 order by 1 limit 5 offset 1")]
    [TestCase(SqlDialectKind.SQLite, "select * from items natural left outer join orders using(id), json_each(items.payload) as j, (values (1), (2)) as v(id)")]
    [TestCase(SqlDialectKind.PostgreSql, "select extract(year from created_at), payload::jsonb, array[1, 2][1], payload #>> '{name}' from events where payload @> expected")]
    [TestCase(SqlDialectKind.SqlServer, "select top (10) percent with ties id from items order by id offset 2 rows fetch next 3 rows only")]
    [TestCase(SqlDialectKind.SQLite, "insert or replace into items(id, name) values (1, 'a'), (2, 'b') on conflict(id) do update set name = excluded.name returning id")]
    [TestCase(SqlDialectKind.MySql, "insert into items(id, name) values (1, 'a') on duplicate key update name = 'b'")]
    [TestCase(SqlDialectKind.SQLite, "update or fail items indexed by idx_items set (name, active) = ('a', 1) from source where items.id = source.id returning items.id order by items.id limit 1")]
    [TestCase(SqlDialectKind.SQLite, "delete from items not indexed where id in (select id from old_items) returning id order by id limit 2")]
    [TestCase(SqlDialectKind.SqlServer, "merge into target t using source s on t.id = s.id when matched then update set name = s.name when not matched then insert(id, name) values(s.id, s.name)")]
    [TestCase(SqlDialectKind.SQLite, "create table items(id integer primary key autoincrement, parent_id integer references parents(id) on delete cascade deferrable initially deferred, value text collate nocase default 'x', generated_value integer generated always as (id + 1) stored, constraint uq unique(value) on conflict ignore, check(id > 0), foreign key(parent_id) references parents(id)) strict, without rowid")]
    [TestCase(SqlDialectKind.SQLite, "create unique index if not exists idx_items on items(lower(value) desc, id collate nocase) where id > 0")]
    [TestCase(SqlDialectKind.SQLite, "create temp view if not exists item_view(id) as select id from items")]
    [TestCase(SqlDialectKind.SQLite, "create virtual table if not exists docs using fts5(content, tokenize='porter')")]
    [TestCase(SqlDialectKind.SQLite, "create trigger tr_items after update of value on items for each row when new.value <> old.value begin insert into audit values(new.id); update stats set count = count + 1; end")]
    [TestCase(SqlDialectKind.SQLite, "alter table items rename column old_name to new_name; alter table items add column active integer default 1; drop table if exists old_items; drop index if exists old_idx; drop view if exists old_view; drop trigger if exists old_trigger")]
    [TestCase(SqlDialectKind.SQLite, "pragma main.cache_size = -2000; analyze main.items; vacuum main into 'backup.db'; attach database 'aux.db' as aux; detach database aux; reindex main.idx_items")]
    [TestCase(SqlDialectKind.SQLite, "begin immediate transaction; savepoint work; release savepoint work; rollback transaction to savepoint prior; commit transaction; end transaction")]
    [TestCase(SqlDialectKind.SQLite, "explain query plan select id from items; explain insert into items values (1)")]
    public void Generator_ParseGenerateParseIsStructurallyStableAcrossPublicNodeFamilies(SqlDialectKind kind, string source)
    {
        SqlParseOptions parseOptions = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(kind));
        SqlParseResult first = new SqlParserService().ParseScript(source, parseOptions);
        Assert.That(first.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(first.Diagnostics));

        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.WriteDialect = new SqlDialectCatalogService().CreateOptions(kind);
        string generated = new SqlGeneratorService().GenerateScriptWithOptions(first.Script, generateOptions);

        SqlParseResult second = new SqlParserService().ParseScript(generated, parseOptions);
        Assert.That(second.Diagnostics.HasErrors(), Is.False, generated + "\n" + FirstDiagnostic(second.Diagnostics));
        AssertSameScriptShape(first.Script, second.Script);

        string generatedAgain = new SqlGeneratorService().GenerateScriptWithOptions(second.Script, generateOptions);
        Assert.That(generatedAgain, Is.EqualTo(generated));
    }

    [TestCase(SqlDialectKind.SQLite, "(a + b) * c", "(a + b) * c")]
    [TestCase(SqlDialectKind.SQLite, "a + b * c", "a + b * c")]
    [TestCase(SqlDialectKind.SQLite, "a - (b - c)", "a - (b - c)")]
    [TestCase(SqlDialectKind.SQLite, "a / (b * c)", "a / (b * c)")]
    [TestCase(SqlDialectKind.SQLite, "(a or b) and c", "(a OR b) AND c")]
    [TestCase(SqlDialectKind.SQLite, "not (a or b)", "NOT (a OR b)")]
    [TestCase(SqlDialectKind.SQLite, "a = b between c and d", "a = b BETWEEN c AND d")]
    [TestCase(SqlDialectKind.SQLite, "a + b collate nocase || c", "a + b COLLATE nocase || c")]
    [TestCase(SqlDialectKind.PostgreSql, "payload #>> path #- key @> expected", "payload #>> path #- key @> expected")]
    public void Generator_PreservesPrecedenceWithOnlyRequiredOrExplicitParentheses(
        SqlDialectKind kind,
        string source,
        string expected)
    {
        SqlDialectOptions dialect = new SqlDialectCatalogService().CreateOptions(kind);
        SqlParseOptions parseOptions = SqlParseOptions.Create(dialect);
        SqlExpressionParseResult first = new SqlParserService().ParseExpression(source, parseOptions);
        Assert.That(first.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(first.Diagnostics));

        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.WriteDialect = dialect;
        string generated = new SqlGeneratorService().GenerateExpressionWithGenerateOptions(first.Expression, generateOptions);
        SqlExpressionParseResult second = new SqlParserService().ParseExpression(generated, parseOptions);

        Assert.That(generated, Is.EqualTo(expected));
        Assert.That(second.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(second.Diagnostics));
        Assert.That(
            new SqlGeneratorService().GenerateExpressionWithGenerateOptions(second.Expression, generateOptions),
            Is.EqualTo(generated));
    }

    [Test]
    public void Generator_CustomOperatorPrecedenceAndAssociativityRoundTripSafely()
    {
        SqlDialectOptions dialect = new SqlDialectOptions();
        dialect.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("**", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 75, true),
            new SqlOperatorPolicy("<=>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 35, false)
        };
        SqlParseOptions parseOptions = SqlParseOptions.Create(dialect);
        SqlExpressionParseResult first = new SqlParserService().ParseExpression("a <=> b + c ** d ** e", parseOptions);
        Assert.That(first.Diagnostics.HasErrors(), Is.False);

        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.WriteDialect = dialect;
        string generated = new SqlGeneratorService().GenerateExpressionWithGenerateOptions(first.Expression, generateOptions);
        SqlExpressionParseResult second = new SqlParserService().ParseExpression(generated, parseOptions);

        Assert.That(generated, Is.EqualTo("a <=> b + c ** (d ** e)"));
        Assert.That(second.Diagnostics.HasErrors(), Is.False);
        Assert.That(second.Expression.OperatorText, Is.EqualTo("<=>"));
        Assert.That(second.Expression.Right!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(second.Expression.Right.Right!.OperatorText, Is.EqualTo("**"));
        Assert.That(second.Expression.Right.Right.Right!.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
        Assert.That(second.Expression.Right.Right.Right.Operand!.OperatorText, Is.EqualTo("**"));
    }

    [Test]
    public void Generator_FullFidelityOptionsPreserveExactSourceBeforeNormalizedOptions()
    {
        const string source = "--lead\nSeLeCt  [Name] /*middle*/ FROM `Items`;;\n";
        SqlParseResult parsed = new SqlParserService().ParseScript(source, SqlParseOptions.CreateSqlite());
        Assert.That(parsed.Diagnostics.HasErrors(), Is.False);

        SqlGenerateOptions preserve = new SqlGenerateOptions();
        preserve.PreserveSourceText = true;
        preserve.IncludeSemicolons = false;
        preserve.FormatOptions.KeywordCase = SqlKeywordCase.Lower;
        preserve.FormatOptions.IdentifierCase = SqlIdentifierCase.Upper;
        preserve.FormatOptions.PreserveComments = false;

        SqlGenerateOptions normalized = preserve.Copy();
        normalized.PreserveSourceText = false;

        Assert.That(new SqlGeneratorService().GenerateScriptWithOptions(parsed.Script, preserve), Is.EqualTo(source));
        Assert.That(
            new SqlGeneratorService().GenerateScriptWithOptions(parsed.Script, normalized),
            Is.EqualTo("select \"Name\" from \"Items\""));
    }

    static void AssertSameScriptShape(SqlScript expected, SqlScript actual)
    {
        Assert.That(actual.Statements.Length, Is.EqualTo(expected.Statements.Length));
        int index = 0;
        while (index < expected.Statements.Length)
        {
            SqlStatement expectedStatement = expected.Statements[index];
            SqlStatement actualStatement = actual.Statements[index];
            Assert.That(actualStatement.Kind, Is.EqualTo(expectedStatement.Kind), "statement " + index);
            Assert.That(actualStatement.Query != null, Is.EqualTo(expectedStatement.Query != null), "query " + index);
            Assert.That(actualStatement.Insert != null, Is.EqualTo(expectedStatement.Insert != null), "insert " + index);
            Assert.That(actualStatement.Update != null, Is.EqualTo(expectedStatement.Update != null), "update " + index);
            Assert.That(actualStatement.Delete != null, Is.EqualTo(expectedStatement.Delete != null), "delete " + index);
            Assert.That(actualStatement.Merge != null, Is.EqualTo(expectedStatement.Merge != null), "merge " + index);
            if (expectedStatement.Query != null)
            {
                Assert.That(actualStatement.Query!.SelectCores.Length, Is.EqualTo(expectedStatement.Query.SelectCores.Length));
                Assert.That(actualStatement.Query.CommonTableExpressions.Length, Is.EqualTo(expectedStatement.Query.CommonTableExpressions.Length));
                Assert.That(actualStatement.Query.CompoundParts.Length, Is.EqualTo(expectedStatement.Query.CompoundParts.Length));
            }

            index = index + 1;
        }
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
