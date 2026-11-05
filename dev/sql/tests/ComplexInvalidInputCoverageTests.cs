using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ComplexInvalidInputCoverageTests
{
    [Test]
    public void StatementParser_ReportsComplexInvalidSelectScriptDiagnostics()
    {
        const string text = "with recent(id) materialized (select 1) select * from recent; select * from items left orders; select * from items order name";

        SqlParseResult result = ParseScript(text);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Select));
        AssertDiagnosticCodesInOrder(result, "SQLQUERY023", "SQLQUERY009", "SQLQUERY017");
        Assert.That(result.Script.GetStatementAt(0).Query!.GetCommonTableExpressionCount(), Is.EqualTo(1));
        Assert.That(result.Script.GetStatementAt(1).Query!.GetSelectCoreAt(0).GetFromAt(0).GetJoinCount(), Is.EqualTo(1));
        Assert.That(result.Script.GetStatementAt(2).Query!.GetOrderByCount(), Is.EqualTo(1));
    }

    [Test]
    public void StatementParser_ReportsComplexInvalidDmlScriptDiagnostics()
    {
        const string text = "insert into audit (id, payload) values (1, json_object('name', 'a')) on conflict(id) update set payload = excluded.payload returning id; update audit indexed idx_audit_payload set payload = json_patch(payload, '{}') from (select id from audit) as s where s.id = audit.id; delete from audit indexed idx_audit_payload where payload is null returning";

        SqlParseResult result = ParseScript(text);
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        SqlUpdateStatement update = result.Script.GetStatementAt(1).Update!;
        SqlDeleteStatement delete = result.Script.GetStatementAt(2).Delete!;

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        AssertDiagnosticCodesInOrder(result, "SQLINSERT012", "SQLUPDATE011", "SQLDELETE007", "SQLDELETE006");
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(insert.GetUpsertClauseAt(0).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoUpdate));
        Assert.That(update.IndexedBy!.GetText(), Is.EqualTo("idx_audit_payload"));
        Assert.That(update.HasAssignments(), Is.True);
        Assert.That(delete.IndexedBy!.GetText(), Is.EqualTo("idx_audit_payload"));
        Assert.That(delete.HasReturning(), Is.True);
        Assert.That(delete.Returning!.HasResultColumns(), Is.False);
    }

    [Test]
    public void StatementParser_ReportsComplexInvalidSchemaScriptDiagnostics()
    {
        const string text = "create table invoices (subtotal numeric, tax numeric generated always (round(subtotal * 0.0825, 2)), constraint ck_invoice check (subtotal >= 0)) strict; create index idx_invoices_total on invoices; create trigger tr_invoices after update on invoices select 1; end;";

        SqlParseResult result = ParseScript(text);
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        SqlIndexDefinition index = result.Script.GetStatementAt(1).CreateIndex!;
        SqlTriggerDefinition trigger = result.Script.GetStatementAt(2).CreateTrigger!;

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Script.GetStatementCount(), Is.GreaterThanOrEqualTo(3));
        AssertDiagnosticCodesInOrder(result, "SQLDDL025", "SQLINDEX009", "SQLTRIGGER016");
        Assert.That(table.GetColumnAt(1).HasGeneratedExpression(), Is.True);
        Assert.That(table.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Check));
        Assert.That(index.GetKeyExpressionCount(), Is.EqualTo(0));
        Assert.That(trigger.Name.GetText(), Is.EqualTo("tr_invoices"));
    }

    static SqlParseResult ParseScript(string text)
    {
        return SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
    }

    static void AssertDiagnosticCodesInOrder(SqlParseResult result, params string[] expectedCodes)
    {
        int diagnosticIndex = 0;
        int expectedIndex = 0;

        while (expectedIndex < expectedCodes.Length)
        {
            string expectedCode = expectedCodes[expectedIndex];
            bool found = false;

            while (diagnosticIndex < result.Diagnostics.GetCount())
            {
                if (result.Diagnostics.GetAt(diagnosticIndex).Code == expectedCode)
                {
                    found = true;
                    diagnosticIndex = diagnosticIndex + 1;
                    break;
                }

                diagnosticIndex = diagnosticIndex + 1;
            }

            Assert.That(found, Is.True, "Expected diagnostic code " + expectedCode + " in order.");
            expectedIndex = expectedIndex + 1;
        }
    }
}
