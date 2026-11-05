using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteSyntaxDiagramExpressionQueryTests
{
    [TestCase("literal-value null", "null")]
    [TestCase("literal-value boolean", "true")]
    [TestCase("literal-value current date", "current_date")]
    [TestCase("literal-value current time", "current_time")]
    [TestCase("literal-value current timestamp", "current_timestamp")]
    [TestCase("numeric-literal integer", "123")]
    [TestCase("numeric-literal decimal", ".125")]
    [TestCase("numeric-literal exponent", "1.25e-3")]
    [TestCase("numeric-literal hexadecimal", "0xCAFE")]
    [TestCase("numeric-literal digit separators", "1_000.25_0e1_0")]
    [TestCase("hexadecimal digit separators", "0xCA_FE")]
    [TestCase("string literal", "'it''s sqlite'")]
    [TestCase("blob literal", "X'53514C'")]
    [TestCase("bind-parameter anonymous", "?")]
    [TestCase("bind-parameter numbered", "?123")]
    [TestCase("bind-parameter colon", ":item_id")]
    [TestCase("bind-parameter at", "@item_id")]
    [TestCase("bind-parameter dollar", "$item_id")]
    [TestCase("bind-parameter Tcl namespace", "$item::detail(value)")]
    [TestCase("qualified column name", "main.items.item_id")]
    [TestCase("unary operators", "not ~-+1")]
    [TestCase("arithmetic operators", "1 + 2 * 3 / 4 % 5 - 6")]
    [TestCase("concatenation operator", "'a' || 'b'")]
    [TestCase("comparison operators", "1 = 1 and 2 == 2 and 3 <> 4 and 4 != 5 and 5 <= 6 and 7 >= 6")]
    [TestCase("bitwise operators", "1 | 2 & 3 << 1 >> 1")]
    [TestCase("json operators", "payload -> 'name' ->> 'first'")]
    [TestCase("collation", "name collate nocase")]
    [TestCase("is predicate", "value is null")]
    [TestCase("is-not predicate", "value is not false")]
    [TestCase("is-distinct predicate", "left_value is distinct from right_value")]
    [TestCase("is-not-distinct predicate", "left_value is not distinct from right_value")]
    [TestCase("postfix isnull", "value isnull")]
    [TestCase("postfix notnull", "value notnull")]
    [TestCase("postfix not null", "value not null")]
    [TestCase("between", "value between 1 and 10")]
    [TestCase("not between", "value not between 1 and 10")]
    [TestCase("in list", "value in (1, 2, 3)")]
    [TestCase("in empty list", "value in ()")]
    [TestCase("not in subquery", "value not in (select id from items)")]
    [TestCase("in table", "value in lookup_table")]
    [TestCase("in table function", "value in json_each(payload)")]
    [TestCase("like escape", "name like 'a!_%' escape '!'")]
    [TestCase("not glob", "name not glob 'tmp*'")]
    [TestCase("regexp", "name regexp '^[a-z]+$'")]
    [TestCase("match", "body match 'sqlite'")]
    [TestCase("exists", "exists (select 1 from items)")]
    [TestCase("searched case", "case when active then 'yes' else 'no' end")]
    [TestCase("simple case", "case status when 1 then 'yes' when 0 then 'no' end")]
    [TestCase("cast", "cast(value as unsigned big int(10, 2))")]
    [TestCase("ordinary function", "coalesce(null, 1, 2)")]
    [TestCase("aggregate invocation", "group_concat(distinct name order by name collate nocase desc)")]
    [TestCase("filter clause", "sum(value) filter (where value > 0)")]
    [TestCase("named over clause", "row_number() over result_window")]
    [TestCase("inline over clause", "sum(value) over (partition by category order by id rows between 2 preceding and current row exclude ties)")]
    [TestCase("parenthesized expression", "(1 + 2)")]
    [TestCase("row value", "(1, 2, 3)")]
    [TestCase("scalar subquery", "(select max(id) from items)")]
    [TestCase("raise ignore", "raise(ignore)")]
    [TestCase("raise action", "raise(abort, 'invalid item')")]
    public void OfficialExpressionDiagramForm_ParsesWithoutDiagnostics(string diagramForm, string text)
    {
        SqlExpressionParseResult result = new SqlParserService().ParseExpression(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True, diagramForm + DiagnosticText(result.Diagnostics));
        Assert.That(result.Expression.Kind, Is.Not.EqualTo(SqlExpressionKind.Unknown), diagramForm);
        Assert.That(result.Expression.Span.Start, Is.EqualTo(0), diagramForm);
        Assert.That(result.Expression.Span.GetEnd(), Is.EqualTo(text.Length), diagramForm);
    }

    [TestCase("simple-select-stmt", "select 1")]
    [TestCase("factored-select-stmt", "select 1 union all select 2 order by 1 limit 1")]
    [TestCase("select-core distinct", "select distinct id as item_id, items.* from items")]
    [TestCase("select-core all", "select all id item_id from items")]
    [TestCase("values select core", "values (1, 'one'), (2, 'two')")]
    [TestCase("compound union", "select 1 union select 2")]
    [TestCase("compound union all", "select 1 union all select 2")]
    [TestCase("compound intersect", "select 1 intersect select 2")]
    [TestCase("compound except", "select 1 except select 2")]
    [TestCase("ordinary cte", "with first(value) as (values (1)) select value from first")]
    [TestCase("materialized cte", "with first as materialized (select 1) select * from first")]
    [TestCase("not-materialized cte", "with first as not materialized (select 1) select * from first")]
    [TestCase("recursive cte", "with recursive counter(value) as (values (1) union all select value + 1 from counter where value < 3) select * from counter")]
    [TestCase("named table", "select * from main.items as target")]
    [TestCase("table-valued function", "select * from json_each('[1,2]') as each_value")]
    [TestCase("subquery table", "select * from (select 1 as id) as source(id)")]
    [TestCase("values table", "select * from (values (1), (2)) as source(value)")]
    [TestCase("parenthesized joined table", "select * from (items join details using (id)) as combined")]
    [TestCase("comma join", "select * from items, details")]
    [TestCase("cross join", "select * from items cross join details")]
    [TestCase("inner join on", "select * from items inner join details on details.id = items.id")]
    [TestCase("left outer join", "select * from items left outer join details using (id)")]
    [TestCase("right join", "select * from items right join details on true")]
    [TestCase("full outer join", "select * from items full outer join details on true")]
    [TestCase("natural join", "select * from items natural join details")]
    [TestCase("flexible SQLite join modifiers", "select * from items natural outer left join details using (id)")]
    [TestCase("where group having", "select category, count(*) from items where active group by category having count(*) > 1")]
    [TestCase("ordering term", "select * from items order by name collate nocase asc nulls first, id desc nulls last")]
    [TestCase("limit", "select * from items limit 10")]
    [TestCase("limit offset", "select * from items limit 10 offset 5")]
    [TestCase("limit comma", "select * from items limit 5, 10")]
    [TestCase("named window", "select sum(value) over result_window from items window result_window as (partition by category order by id)")]
    [TestCase("rows frame", "select sum(value) over (rows unbounded preceding exclude current row) from items")]
    [TestCase("range frame", "select sum(value) over (range between current row and unbounded following exclude group) from items")]
    [TestCase("groups frame", "select sum(value) over (groups between 1 preceding and 1 following exclude no others) from items")]
    public void OfficialQueryDiagramForm_ParsesWithoutDiagnostics(string diagramForm, string text)
    {
        SqlQueryParseResult result = new SqlParserService().ParseQuery(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True, diagramForm + DiagnosticText(result.Diagnostics));
        Assert.That(result.Query.GetSelectCoreCount(), Is.GreaterThan(0), diagramForm);
        Assert.That(result.Query.Span.Start, Is.EqualTo(0), diagramForm);
        Assert.That(result.Query.Span.GetEnd(), Is.EqualTo(text.Length), diagramForm);
    }

    [Test]
    public void PostfixNullPredicates_PreserveTypedNullRelationshipAndNormalizeGeneration()
    {
        SqlExpressionParseResult isNullResult = new SqlParserService().ParseExpression("value isnull", SqlParseOptions.CreateSqlite());
        SqlExpressionParseResult notNullResult = new SqlParserService().ParseExpression("value notnull", SqlParseOptions.CreateSqlite());
        SqlExpressionParseResult spacedResult = new SqlParserService().ParseExpression("value not null", SqlParseOptions.CreateSqlite());

        Assert.That(isNullResult.Expression.Kind, Is.EqualTo(SqlExpressionKind.Is));
        Assert.That(isNullResult.Expression.Right!.Literal!.Kind, Is.EqualTo(SqlLiteralKind.Null));
        Assert.That(notNullResult.Expression.Right!.Literal!.Kind, Is.EqualTo(SqlLiteralKind.Null));
        Assert.That(spacedResult.Expression.Right!.Literal!.Kind, Is.EqualTo(SqlLiteralKind.Null));
        Assert.That(SqlGenerator.GenerateExpression(isNullResult.Expression), Is.EqualTo("value IS NULL"));
        Assert.That(SqlGenerator.GenerateExpression(notNullResult.Expression), Is.EqualTo("value IS NOT NULL"));
        Assert.That(SqlGenerator.GenerateExpression(spacedResult.Expression), Is.EqualTo("value IS NOT NULL"));
    }

    [Test]
    public void SQLiteLexicalExtensions_PreserveDigitSeparatorsAndTclParameterName()
    {
        SqlExpressionParseResult numeric = new SqlParserService().ParseExpression("1_000.25_0e1_0", SqlParseOptions.CreateSqlite());
        SqlExpressionParseResult parameter = new SqlParserService().ParseExpression("$item::detail(value)", SqlParseOptions.CreateSqlite());

        Assert.That(numeric.Expression.Literal!.SourceText, Is.EqualTo("1_000.25_0e1_0"));
        Assert.That(numeric.Tokens[0].Text, Is.EqualTo("1_000.25_0e1_0"));
        Assert.That(parameter.Expression.Kind, Is.EqualTo(SqlExpressionKind.Parameter));
        Assert.That(parameter.Expression.Value, Is.EqualTo("$item::detail(value)"));
        Assert.That(parameter.Tokens[0].ParameterStyle, Is.EqualTo(SqlParameterStyle.DollarName));
        Assert.That(parameter.Tokens[0].Value, Is.EqualTo("item::detail(value)"));
    }

    static string DiagnosticText(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.GetCount() == 0)
        {
            return "";
        }

        SqlDiagnostic diagnostic = diagnostics.GetAt(0);
        return ": " + diagnostic.Code + " " + diagnostic.Message;
    }
}
