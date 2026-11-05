using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ComplexValidInputCoverageTests
{
    [Test]
    public void StatementParser_ParsesComplexAnalyticCteQuery()
    {
        const string text = "with recent(id, category, amount) as materialized (select id, category, amount from orders where created_at >= date('now', '-30 days')), ranked as not materialized (select id, category, amount, sum(amount) filter (where amount > 0) over (partition by category order by id rows between 2 preceding and current row exclude no others) as rolling_amount, case when amount > (select avg(amount) from recent) then 'high' else 'normal' end as bucket from recent) select r.category, count(*) as order_count, max(r.rolling_amount) as max_rolling from ranked as r left join (select category from ranked group by category having count(*) > 1) as many using (category) where exists (select 1 from recent as x where x.id = r.id) group by r.category having count(*) > 0 order by max_rolling desc nulls last, r.category collate nocase asc limit 10 offset 5";

        SqlParseResult result = ParseScript(text);
        SqlQuery query = result.Script.GetStatementAt(0).Query!;
        SqlCommonTableExpression recent = query.GetCommonTableExpressionAt(0);
        SqlCommonTableExpression ranked = query.GetCommonTableExpressionAt(1);
        SqlSelectCore rankedCore = ranked.Query.GetSelectCoreAt(0);
        SqlSelectCore mainCore = query.GetSelectCoreAt(0);
        SqlExpression rollingAmount = ((SqlResultColumn)rankedCore.GetResultColumnAt(3)).Expression!;
        SqlExpression bucket = ((SqlResultColumn)rankedCore.GetResultColumnAt(4)).Expression!;
        SqlJoin join = mainCore.GetFromAt(0).GetJoinAt(0);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Diagnostics.GetCount(), Is.EqualTo(0));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(2));
        Assert.That(recent.IsMaterialized(), Is.True);
        Assert.That(recent.GetColumnNameCount(), Is.EqualTo(3));
        Assert.That(ranked.IsNotMaterialized(), Is.True);
        Assert.That(rollingAmount.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(rollingAmount.Operand!.Filter, Is.Not.Null);
        Assert.That(rollingAmount.WindowDefinition!.Frame!.Exclusion, Is.EqualTo(SqlWindowFrameExclusionKind.NoOthers));
        Assert.That(bucket.Kind, Is.EqualTo(SqlExpressionKind.Case));
        Assert.That(bucket.GetArgumentAt(0).Right!.Kind, Is.EqualTo(SqlExpressionKind.Subquery));
        Assert.That(mainCore.GetResultColumnCount(), Is.EqualTo(3));
        Assert.That(mainCore.GetFromCount(), Is.EqualTo(1));
        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Left));
        Assert.That(join.Right.Kind, Is.EqualTo(SqlTableReferenceKind.Subquery));
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.Using));
        Assert.That(mainCore.HasWhere(), Is.True);
        Assert.That(mainCore.GetGroupByCount(), Is.EqualTo(1));
        Assert.That(mainCore.HasHaving(), Is.True);
        Assert.That(query.GetOrderByCount(), Is.EqualTo(2));
        Assert.That(query.GetOrderByAt(0).NullOrdering, Is.EqualTo(SqlNullOrdering.Last));
        Assert.That(query.GetOrderByAt(1).HasCollation(), Is.True);
        Assert.That(query.HasLimit(), Is.True);
    }

    [Test]
    public void StatementParser_ParsesComplexValidDmlScript()
    {
        const string text = "insert into audit (id, payload, score) values (1, json_object('name', 'a'), case when 1 then 10 else 0 end), (2, json_object('name', 'b'), coalesce(null, 0)) on conflict(id) where score > 0 do update set payload = excluded.payload, score = excluded.score where audit.score <> excluded.score on conflict do nothing returning id, payload as saved_payload; update audit as a not indexed set score = score + 1 from (select id from audit where score < 10) as s where s.id = a.id returning a.id, score; delete from audit indexed by idx_audit_score where score in (select score from audit where score < 0) returning *";

        SqlParseResult result = ParseScript(text);
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        SqlUpdateStatement update = result.Script.GetStatementAt(1).Update!;
        SqlDeleteStatement delete = result.Script.GetStatementAt(2).Delete!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Diagnostics.GetCount(), Is.EqualTo(0));
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Values));
        Assert.That(insert.GetValueRowCount(), Is.EqualTo(2));
        Assert.That(insert.GetValueRowAt(0)[1].Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(insert.GetValueRowAt(0)[2].Kind, Is.EqualTo(SqlExpressionKind.Case));
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(2));
        Assert.That(insert.GetUpsertClauseAt(0).HasConflictWhere(), Is.True);
        Assert.That(insert.GetUpsertClauseAt(0).GetUpdateAssignmentCount(), Is.EqualTo(2));
        Assert.That(insert.GetUpsertClauseAt(1).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
        Assert.That(insert.Returning!.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(update.Alias!.GetText(), Is.EqualTo("a"));
        Assert.That(update.IsNotIndexed, Is.True);
        Assert.That(update.GetFromAt(0).Kind, Is.EqualTo(SqlTableReferenceKind.Subquery));
        Assert.That(update.GetAssignmentExpressionAt(0).Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(update.Returning!.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(delete.IndexedBy!.GetText(), Is.EqualTo("idx_audit_score"));
        Assert.That(delete.Where!.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(delete.Where.Query, Is.Not.Null);
        Assert.That(((SqlResultColumn)delete.Returning!.GetResultColumnAt(0)).IsWildcard, Is.True);
    }

    [Test]
    public void StatementParser_ParsesComplexValidSchemaAndUtilityScript()
    {
        const string text = "create table if not exists invoices (id integer primary key, customer_id integer not null references customers(id), subtotal numeric default 0 check (subtotal >= 0), tax numeric generated always as (round(subtotal * 0.0825, 2)) stored, total numeric generated always as (subtotal + tax) virtual, constraint uq_invoice unique (customer_id, id) on conflict abort) strict; create unique index if not exists idx_invoices_customer_total on invoices (customer_id asc, total desc) where total > 0; create view if not exists invoice_rollups(customer_id, invoice_count, grand_total) as select customer_id, count(*), sum(total) from invoices group by customer_id having sum(total) > 0; analyze main.invoices; reindex idx_invoices_customer_total; vacuum main into 'backup.db'";

        SqlParseResult result = ParseScript(text);
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        SqlIndexDefinition index = result.Script.GetStatementAt(1).CreateIndex!;
        SqlViewDefinition view = result.Script.GetStatementAt(2).CreateView!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Diagnostics.GetCount(), Is.EqualTo(0));
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(6));
        Assert.That(table.IsStrict, Is.True);
        Assert.That(table.GetColumnCount(), Is.EqualTo(5));
        Assert.That(table.GetColumnAt(3).GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Stored));
        Assert.That(table.GetColumnAt(4).GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
        Assert.That(table.GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Abort));
        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.GetKeyExpressionCount(), Is.EqualTo(2));
        Assert.That(index.HasWhereExpression(), Is.True);
        Assert.That(view.GetColumnNameCount(), Is.EqualTo(3));
        Assert.That(view.Query.GetSelectCoreAt(0).HasHaving(), Is.True);
        Assert.That(result.Script.GetStatementAt(3).Kind, Is.EqualTo(SqlStatementKind.Analyze));
        Assert.That(result.Script.GetStatementAt(4).Kind, Is.EqualTo(SqlStatementKind.Reindex));
        Assert.That(result.Script.GetStatementAt(5).Kind, Is.EqualTo(SqlStatementKind.Vacuum));
    }

    static SqlParseResult ParseScript(string text)
    {
        return SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
    }
}
