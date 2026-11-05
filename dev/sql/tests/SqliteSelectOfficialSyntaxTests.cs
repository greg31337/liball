using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteSelectOfficialSyntaxTests
{
    [Test]
    public void SQLiteSelectSyntax_ParsesDistinctAggregateQueryShape()
    {
        SqlQuery query = ParseQuery("select distinct item.category as category, count(*) as total from items where item.active = 1 group by item.category having count(*) > 1 order by total desc limit 5 offset 10");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.IsDistinct, Is.True);
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(((SqlResultColumn)core.GetResultColumnAt(0)).Alias!.GetText(), Is.EqualTo("category"));
        Assert.That(core.HasFrom(), Is.True);
        Assert.That(core.HasWhere(), Is.True);
        Assert.That(core.GetGroupByCount(), Is.EqualTo(1));
        Assert.That(core.HasHaving(), Is.True);
        Assert.That(query.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(query.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Limit!.HasOffset(), Is.True);
    }

    [Test]
    public void SQLiteSelectSyntax_ParsesTableOrSubqueryForms()
    {
        SqlQuery query = ParseQuery("select t.*, j.value from (select 1 as id) as t, json_each(t.id) as j");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(((SqlResultColumn)core.GetResultColumnAt(0)).IsWildcard, Is.True);
        Assert.That(((SqlResultColumn)core.GetResultColumnAt(0)).Qualifier!.GetText(), Is.EqualTo("t"));
        Assert.That(core.GetFromCount(), Is.EqualTo(2));
        Assert.That(core.GetFromAt(0).Kind, Is.EqualTo(SqlTableReferenceKind.Subquery));
        Assert.That(core.GetFromAt(0).Alias!.GetText(), Is.EqualTo("t"));
        Assert.That(core.GetFromAt(1).Kind, Is.EqualTo(SqlTableReferenceKind.Function));
        Assert.That(core.GetFromAt(1).Alias!.GetText(), Is.EqualTo("j"));
    }

    [Test]
    public void SQLiteSelectSyntax_ParsesFlexibleJoinOperatorShape()
    {
        SqlQuery query = ParseQuery("select * from items natural left outer join orders using (id)");
        SqlJoin join = query.GetSelectCoreAt(0).GetFromAt(0).GetJoinAt(0);

        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Left));
        Assert.That(join.IsNatural, Is.True);
        Assert.That(join.IsOuter, Is.True);
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.Using));
        Assert.That(join.GetUsingColumnAt(0).GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void SQLiteSelectSyntax_ParsesRecursiveCteWithValuesAndCompoundSelect()
    {
        SqlQuery query = ParseQuery("with recursive cte(n) as (values (1) union all select n + 1 from cte limit 3) select n from cte order by n");
        SqlCommonTableExpression cte = query.GetCommonTableExpressionAt(0);

        Assert.That(query.IsRecursive, Is.True);
        Assert.That(cte.GetColumnNameAt(0).GetText(), Is.EqualTo("n"));
        Assert.That(cte.Query.GetSelectCoreAt(0).IsValues, Is.True);
        Assert.That(cte.Query.IsCompound(), Is.True);
        Assert.That(cte.Query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(cte.Query.HasLimit(), Is.True);
        Assert.That(query.GetOrderByCount(), Is.EqualTo(1));
    }

    [Test]
    public void SQLiteSelectSyntax_ParsesWindowAndFilterClauses()
    {
        SqlQuery query = ParseQuery("select sum(amount) filter (where amount > 0) over win from items window win as (partition by category order by created rows between unbounded preceding and current row)");
        SqlSelectCore core = query.GetSelectCoreAt(0);
        SqlExpression expression = ((SqlResultColumn)core.GetResultColumnAt(0)).Expression!;

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.Operand.Filter, Is.Not.Null);
        Assert.That(core.GetWindowCount(), Is.EqualTo(1));
        Assert.That(core.GetWindowAt(0).Name!.GetText(), Is.EqualTo("win"));
        Assert.That(core.GetWindowAt(0).Frame, Is.Not.Null);
        Assert.That(core.GetWindowAt(0).Frame!.End, Is.Not.Null);
    }

    static SqlQuery ParseQuery(string text)
    {
        SqlQueryParser parser = new SqlQueryParser(Context(text));
        return parser.ParseQuery();
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
