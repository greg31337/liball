using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlAnalyzerTests
{
    [Test]
    public void SqlAnalyzer_FindsTablesColumnsParametersAndFunctions()
    {
        SqlScript script = ParseScript("select u.id, count(o.id) from users as u join orders as o on o.user_id = u.id where u.id = ? and exists (select 1 from payments as p where p.order_id = o.id); insert into audit(user_id,total) values(:userId,total_changes()); update audit set total = total + 1 where user_id = @userId");

        SqlName[] tables = SqlAnalyzer.FindTables(script);
        SqlName[] columns = SqlAnalyzer.FindColumns(script);
        SqlExpression[] parameters = SqlAnalyzer.FindParameters(script);
        SqlExpression[] functions = SqlAnalyzer.FindFunctions(script);

        AssertContainsName(tables, "users");
        AssertContainsName(tables, "orders");
        AssertContainsName(tables, "payments");
        AssertContainsName(tables, "audit");
        AssertContainsName(columns, "u.id");
        AssertContainsName(columns, "o.user_id");
        AssertContainsName(columns, "p.order_id");
        AssertContainsName(columns, "user_id");
        AssertContainsName(columns, "total");
        AssertContainsParameter(parameters, "?");
        AssertContainsParameter(parameters, ":userId");
        AssertContainsParameter(parameters, "@userId");
        AssertContainsFunction(functions, "count");
        AssertContainsFunction(functions, "total_changes");
    }

    [Test]
    public void SqlAnalyzer_SearchesQueryAndExpressionInputs()
    {
        SqlScript script = ParseScript("select id from items where exists (select 1 from audit where audit.item_id = items.id) and id = ?");
        SqlQuery query = script.GetStatementAt(0).Query!;
        SqlExpression where = query.GetSelectCoreAt(0).Where!;

        SqlName[] queryTables = SqlAnalyzer.FindTables(query);
        SqlName[] expressionTables = SqlAnalyzer.FindTables(where);
        SqlExpression[] queryParameters = SqlAnalyzer.FindParameters(query);
        SqlName[] expressionColumns = SqlAnalyzer.FindColumns(where);

        AssertContainsName(queryTables, "items");
        AssertContainsName(queryTables, "audit");
        AssertContainsName(expressionTables, "audit");
        AssertContainsParameter(queryParameters, "?");
        AssertContainsName(expressionColumns, "audit.item_id");
        AssertContainsName(expressionColumns, "items.id");
    }

    [Test]
    public void SqlAnalyzer_NullInputsReturnEmptyArrays()
    {
        Assert.That(SqlAnalyzer.FindTables((SqlScript?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAnalyzer.FindTables((SqlExpression?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAnalyzer.FindColumns((SqlQuery?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAnalyzer.FindParameters((SqlQuery?)null), Has.Length.EqualTo(0));
        Assert.That(SqlAnalyzer.FindFunctions((SqlQuery?)null), Has.Length.EqualTo(0));
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }

    static void AssertContainsName(SqlName[] names, string text)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index].GetText() == text)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected name '" + text + "'.");
    }

    static void AssertContainsParameter(SqlExpression[] parameters, string text)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            string? value = parameters[index].Value as string;
            if (value == text)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected parameter '" + text + "'.");
    }

    static void AssertContainsFunction(SqlExpression[] functions, string text)
    {
        int index = 0;
        while (index < functions.Length)
        {
            if (functions[index].Name != null && functions[index].Name!.GetText() == text)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected function '" + text + "'.");
    }
}
