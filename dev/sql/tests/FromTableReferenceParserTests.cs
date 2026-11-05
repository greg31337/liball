using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FromTableReferenceParserTests
{
    [Test]
    public void QueryParser_ParsesNamedTableReference()
    {
        SqlSelectCore core = ParseCore("select * from main.items");

        Assert.That(core.HasFrom(), Is.True);
        Assert.That(core.GetFromCount(), Is.EqualTo(1));
        SqlTableReference table = core.GetFromAt(0);
        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Named));
        Assert.That(table.Name, Is.Not.Null);
        Assert.That(table.Name!.GetText(), Is.EqualTo("main.items"));
        Assert.That(table.HasAlias(), Is.False);
    }

    [Test]
    public void QueryParser_ParsesTableAliasAndColumnAliases()
    {
        SqlSelectCore core = ParseCore("select * from items as i(id, name)");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Named));
        Assert.That(table.HasAlias(), Is.True);
        Assert.That(table.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(table.HasColumnAliases(), Is.True);
        Assert.That(table.GetColumnAliasCount(), Is.EqualTo(2));
        Assert.That(table.GetColumnAliasAt(0).GetText(), Is.EqualTo("id"));
        Assert.That(table.GetColumnAliasAt(1).GetText(), Is.EqualTo("name"));
    }

    [Test]
    public void QueryParser_ParsesImplicitTableAlias()
    {
        SqlSelectCore core = ParseCore("select * from items i");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.HasAlias(), Is.True);
        Assert.That(table.Alias!.GetText(), Is.EqualTo("i"));
    }

    [Test]
    public void QueryParser_ParsesMultipleTableReferences()
    {
        SqlSelectCore core = ParseCore("select * from items i, orders as o");

        Assert.That(core.GetFromCount(), Is.EqualTo(2));
        Assert.That(core.GetFromAt(0).Name!.GetText(), Is.EqualTo("items"));
        Assert.That(core.GetFromAt(0).Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(core.GetFromAt(1).Name!.GetText(), Is.EqualTo("orders"));
        Assert.That(core.GetFromAt(1).Alias!.GetText(), Is.EqualTo("o"));
    }

    [Test]
    public void QueryParser_ParsesTableValuedFunctionReference()
    {
        SqlSelectCore core = ParseCore("select * from json_each(items.data) as j");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Function));
        Assert.That(table.FunctionExpression, Is.Not.Null);
        Assert.That(table.FunctionExpression!.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(table.HasAlias(), Is.True);
        Assert.That(table.Alias!.GetText(), Is.EqualTo("j"));
    }

    [Test]
    public void QueryParser_ParsesSubqueryTableReference()
    {
        SqlSelectCore core = ParseCore("select * from (select 1) as s");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Subquery));
        Assert.That(table.Query, Is.Not.Null);
        Assert.That(table.Query!.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(table.HasAlias(), Is.True);
        Assert.That(table.Alias!.GetText(), Is.EqualTo("s"));
    }

    [Test]
    public void QueryParser_ParsesParenthesizedTableReference()
    {
        SqlSelectCore core = ParseCore("select * from (items) as i");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Parenthesized));
        Assert.That(table.Inner, Is.Not.Null);
        Assert.That(table.Inner!.Kind, Is.EqualTo(SqlTableReferenceKind.Named));
        Assert.That(table.Inner.Name!.GetText(), Is.EqualTo("items"));
        Assert.That(table.Alias!.GetText(), Is.EqualTo("i"));
    }

    [Test]
    public void QueryParser_ReportsMissingTableReferenceAndAliasDiagnostics()
    {
        SqlParserContext missingTableContext = Context("select * from where id = 1");
        SqlQueryParser missingTableParser = new SqlQueryParser(missingTableContext);
        SqlParserContext missingAliasContext = Context("select * from items as where id = 1");
        SqlQueryParser missingAliasParser = new SqlQueryParser(missingAliasContext);

        SqlQuery missingTable = missingTableParser.ParseQuery();
        SqlQuery missingAlias = missingAliasParser.ParseQuery();

        Assert.That(missingTable.GetSelectCoreAt(0).GetFromCount(), Is.EqualTo(0));
        Assert.That(missingTableContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY004"));
        Assert.That(missingAlias.GetSelectCoreAt(0).GetFromCount(), Is.EqualTo(1));
        Assert.That(missingAliasContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY008"));
    }

    static SqlSelectCore ParseCore(string text)
    {
        SqlQueryParser parser = new SqlQueryParser(Context(text));
        return parser.ParseQuery().GetSelectCoreAt(0);
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
